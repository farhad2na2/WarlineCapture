using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class UnitTransportValidationTests
{
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;

    [TearDown]
    public void TearDown()
    {
        if (_blockerCounts.IsCreated)
            _blockerCounts.Dispose();
        if (_blocked.IsCreated)
            _blocked.Dispose();
        if (_occupied.IsCreated)
            _occupied.Dispose();
        if (_friendlyPassFactionIds.IsCreated)
            _friendlyPassFactionIds.Dispose();
    }

    [Test]
    public void GroundPersonnelTransport_BoardsSoldierLikeApc()
    {
        using var world = new World("GroundPersonnelTransport_BoardsSoldierLikeApc");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 12, 12);

        Entity transport = CreateTransport(em, new int2(5, 5), air: false, airborne: false);
        Entity passenger = CreatePassenger(em, new int2(6, 5), transport, new int2(6, 5));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length);
        Assert.AreEqual(passenger, passengers[0].Passenger);
        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsTrue(em.HasComponent<Disabled>(passenger));
    }

    [Test]
    public void AirTransport_DoesNotBoardSoldierUntilLanded()
    {
        using var world = new World("AirTransport_DoesNotBoardSoldierUntilLanded");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 12, 12);

        Entity transport = CreateTransport(em, new int2(5, 5), air: true, airborne: true);
        Entity passenger = CreatePassenger(em, new int2(6, 5), transport, new int2(6, 5));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));

        UnitAirComponent landedState = em.GetComponentData<UnitAirComponent>(transport);
        landedState.Airborne = 0;
        landedState.ReturningHome = 0;
        em.SetComponentData(transport, landedState);
        em.SetComponentData(transport, LocalTransform.FromPosition(new float3(5.5f, 0f, 5.5f)));

        world.SetTime(new TimeData(2d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsTrue(em.HasComponent<Disabled>(passenger));
    }

    [Test]
    public void AirTransport_BoardsWhenLandedOnRaisedHelipad()
    {
        using var world = new World("AirTransport_BoardsWhenLandedOnRaisedHelipad");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 16, 16);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        airState.HomeInitialized = 1;
        airState.HomePosition = new float3(8.5f, 0f, 8.5f);
        airState.Airborne = 0;
        em.SetComponentData(transport, airState);
        em.SetComponentData(transport, LocalTransform.FromPosition(new float3(8.5f, 2.25f, 8.5f)));
        em.SetComponentData(transport, new LocalToWorld { Value = float4x4.Translate(new float3(8.5f, 2.25f, 8.5f)) });

        Entity passenger = CreatePassenger(em, new int2(9, 8), transport, new int2(9, 8));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "A helicopter visibly landed on a raised helipad should accept nearby soldiers.");
        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsTrue(em.HasComponent<Disabled>(passenger));
    }

    [Test]
    public void AirTransport_DoesNotBoardAtOldWideClearanceDistance()
    {
        using var world = new World("AirTransport_DoesNotBoardAtOldWideClearanceDistance");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        Entity passenger = CreatePassenger(em, new int2(13, 8), transport, new int2(13, 8));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "A soldier outside the tightened helicopter boarding clearance must keep walking instead of boarding from far away.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
    }

    [Test]
    public void AirTransport_DoesNotBoardWhenStoppedOneCellShortOfCloseGoal()
    {
        using var world = new World("AirTransport_DoesNotBoardWhenStoppedOneCellShortOfCloseGoal");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        em.SetComponentData(transport, new UnitFootprint { Size = new int2(1, 1) });
        Entity passenger = CreatePassenger(em, new int2(10, 8), transport, new int2(9, 8));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "A helicopter passenger must reach the close boarding goal instead of boarding from the old two-cell fallback distance.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
    }

    [Test]
    public void AirTransport_DoesNotBoardAtFarEdgeOfLargeHelicopterFootprint()
    {
        using var world = new World("AirTransport_DoesNotBoardAtFarEdgeOfLargeHelicopterFootprint");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 40, 40);

        Entity transport = CreateTransport(em, new int2(20, 20), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        em.SetComponentData(transport, new UnitFootprint { Size = new int2(17, 21) });
        Entity passenger = CreatePassenger(em, new int2(28, 30), transport, new int2(28, 30));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "A soldier at the far edge of a large helicopter footprint must not board until it reaches the compact center boarding area.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
    }

    [Test]
    public void AirTransportPickup_ClickingFlyingHelicopterCommandsLandingNearPassengerBeforeBoarding()
    {
        using var world = new World("AirTransportPickup_ClickingFlyingHelicopterCommandsLandingNearPassengerBeforeBoarding");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransport(em, new int2(4, 4), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        em.SetComponentData(transport, new UnitFootprint { Size = new int2(1, 1) });
        UnitAirComponent visuallyFlyingAirState = em.GetComponentData<UnitAirComponent>(transport);
        visuallyFlyingAirState.Airborne = 0;
        visuallyFlyingAirState.HomeInitialized = 1;
        visuallyFlyingAirState.HomePosition = new float3(4.5f, 0f, 4.5f);
        em.SetComponentData(transport, visuallyFlyingAirState);
        em.SetComponentData(transport, LocalTransform.FromPosition(new float3(4.5f, 8f, 4.5f)));
        Entity passenger = CreatePassenger(em, new int2(16, 16), transport, new int2(16, 16));
        Entity gridEntity = GetGridEntity(em);
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        DynamicOccupancyComponent occupancyData = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);

        using EntityQuery liveUnitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>());
        using NativeArray<Entity> liveEntities = liveUnitQuery.ToEntityArray(Allocator.Temp);
        using NativeArray<UnitGrid> liveGrids = liveUnitQuery.ToComponentDataArray<UnitGrid>(Allocator.Temp);
        using NativeArray<UnitFootprint> liveFootprints = liveUnitQuery.ToComponentDataArray<UnitFootprint>(Allocator.Temp);

        var airPickupSystem = new UnitTransportAirPickupSystem();
        bool prepared = airPickupSystem.TryPrepareAirTransportPickupForBoarding(
            em,
            transport,
            grid,
            walkable.AsNativeArray(),
            blockerData.Blocked,
            blockerData.FriendlyPassFactionIds,
            occupancyData.Occupied,
            em.GetComponentData<UnitGrid>(transport).Cell,
            em.GetComponentData<UnitFootprint>(transport).Size,
            new List<Entity> { passenger },
            1,
            liveEntities,
            liveGrids,
            liveFootprints,
            new UnitMoveOrderSystem(),
            out int2 pickupCell);

        Assert.IsTrue(prepared, "Clicking a flying transport helicopter with a selected soldier should command a pickup landing.");
        Assert.AreNotEqual(em.GetComponentData<UnitGrid>(passenger).Cell, pickupCell, "The helicopter must land on a free nearby cell, not on top of the soldier.");
        Assert.LessOrEqual(
            math.max(math.abs(pickupCell.x - em.GetComponentData<UnitGrid>(passenger).Cell.x), math.abs(pickupCell.y - em.GetComponentData<UnitGrid>(passenger).Cell.y)),
            10,
            "The pickup landing should stay near the selected soldier.");
        Assert.IsTrue(em.HasComponent<UnitTarget>(transport));
        Assert.AreEqual(pickupCell, em.GetComponentData<UnitTarget>(transport).Cell);
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        Assert.AreEqual(1, airState.Airborne, "A helicopter that is physically above the ground must be marked airborne for pickup landing, even if stale flags said grounded.");
        Assert.AreEqual(pickupCell, airState.HomeCell);

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "The soldier must not board while the helicopter is still airborne and moving to the pickup landing.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
    }

    [Test]
    public void AirTransportPickup_FindingLandingCellDoesNotInvalidateGridArrays()
    {
        using var world = new World("AirTransportPickup_FindingLandingCellDoesNotInvalidateGridArrays");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransport(em, new int2(4, 4), air: true, airborne: true, "Unit_Veh_Helicopter_Transport");
        em.SetComponentData(transport, new UnitFootprint { Size = new int2(1, 1) });
        Entity passenger = CreatePassenger(em, new int2(16, 16), transport, new int2(16, 16));
        Entity gridEntity = GetGridEntity(em);
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        DynamicBuffer<GridWalkable> walkableBuffer = em.GetBuffer<GridWalkable>(gridEntity);
        NativeArray<GridWalkable> walkable = walkableBuffer.AsNativeArray();
        DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        DynamicOccupancyComponent occupancyData = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);

        using EntityQuery liveUnitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>());
        using NativeArray<Entity> liveEntities = liveUnitQuery.ToEntityArray(Allocator.Temp);
        using NativeArray<UnitGrid> liveGrids = liveUnitQuery.ToComponentDataArray<UnitGrid>(Allocator.Temp);
        using NativeArray<UnitFootprint> liveFootprints = liveUnitQuery.ToComponentDataArray<UnitFootprint>(Allocator.Temp);

        var airPickupSystem = new UnitTransportAirPickupSystem();
        bool found = airPickupSystem.TryFindAirTransportPickupForBoarding(
            em,
            transport,
            grid,
            walkable,
            blockerData.Blocked,
            blockerData.FriendlyPassFactionIds,
            occupancyData.Occupied,
            em.GetComponentData<UnitGrid>(transport).Cell,
            em.GetComponentData<UnitFootprint>(transport).Size,
            new List<Entity> { passenger },
            1,
            liveEntities,
            liveGrids,
            liveFootprints,
            out _);

        Assert.IsTrue(found, "Finding an airborne pickup landing cell should succeed.");
        Assert.IsFalse(em.HasComponent<UnitTarget>(transport), "Finding the pickup cell must not make structural ECS changes while grid NativeArrays are still in use.");
        Assert.AreEqual(1, walkable[GridUtils.CellToIndex(new int2(0, 0), grid.Width)].Value, "The held GridWalkable NativeArray should remain valid after pickup-cell search.");
    }

    [Test]
    public void AirTransport_DoesNotBoardWhenAirFlagsGroundedButModelStillFlying()
    {
        using var world = new World("AirTransport_DoesNotBoardWhenAirFlagsGroundedButModelStillFlying");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 16, 16);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        airState.HomeInitialized = 1;
        airState.HomePosition = new float3(8.5f, 0f, 8.5f);
        airState.Airborne = 0;
        airState.TakeoffRolling = 0;
        airState.LandingRolling = 0;
        em.SetComponentData(transport, airState);
        em.SetComponentData(transport, LocalTransform.FromPosition(new float3(8.5f, 8f, 8.5f)));

        Entity passenger = CreatePassenger(em, new int2(9, 8), transport, new int2(9, 8));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "A soldier must not board while the helicopter model is still visibly flying, even if stale air flags say grounded.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
    }

    [Test]
    public void AirTransport_BoardsAllPassengersThatReachedCloseHelicopterGoals()
    {
        using var world = new World("AirTransport_BoardsAllPassengersThatReachedCloseHelicopterGoals");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 20, 20);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        Entity passengerA = CreatePassenger(em, new int2(7, 8), transport, new int2(7, 8));
        Entity passengerB = CreatePassenger(em, new int2(9, 8), transport, new int2(9, 8));
        Entity passengerC = CreatePassenger(em, new int2(8, 7), transport, new int2(8, 7));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(3, passengers.Length, "Every passenger that reached a valid close helicopter boarding goal should board in the same update.");
        Assert.IsTrue(TransportPassengerBufferContains(passengers, passengerA));
        Assert.IsTrue(TransportPassengerBufferContains(passengers, passengerB));
        Assert.IsTrue(TransportPassengerBufferContains(passengers, passengerC));
        Assert.IsTrue(em.HasComponent<Disabled>(passengerA));
        Assert.IsTrue(em.HasComponent<Disabled>(passengerB));
        Assert.IsTrue(em.HasComponent<Disabled>(passengerC));
    }

    [Test]
    public void Transport_DoesNotBoardPassengerThatOnlyReachedFarBoardingGoal()
    {
        using var world = new World("Transport_DoesNotBoardPassengerThatOnlyReachedFarBoardingGoal");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransport(em, new int2(5, 5), air: false, airborne: false);
        Entity passenger = CreatePassenger(em, new int2(20, 5), transport, new int2(20, 5));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "A passenger must not board just because it reached a stale/far boarding goal.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger), "The order should remain active until the passenger actually reaches the transport clearance.");
    }

    [Test]
    public void HelicopterRopeDisembark_ReleasesPassengersOneAtATime()
    {
        using var world = new World("HelicopterRopeDisembark_ReleasesPassengersOneAtATime");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 16, 16);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: true);
        Entity passengerA = CreateLoadedPassenger(em, transport);
        Entity passengerB = CreateLoadedPassenger(em, transport);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = passengerA });
        passengers.Add(new UnitTransportPassengerElement { Passenger = passengerB });
        em.AddComponentData(transport, new UnitTransportRopeDisembarkRequest
        {
            ReferenceCell = new int2(8, 8),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.8f
        });

        SystemHandle disembarkSystem = world.CreateSystem<UnitTransportRopeDisembarkSystem>();
        SystemHandle dropSystem = world.CreateSystem<UnitTransportRopeDropSystem>();
        SystemHandle disperseSystem = world.CreateSystem<UnitTransportRopeDisperseSystem>();

        world.SetTime(new TimeData(1d, 0.1f));
        disembarkSystem.Update(world.Unmanaged);

        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length, "Only one passenger should leave per rope interval.");
        Assert.IsFalse(em.HasComponent<Disabled>(passengerA));
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passengerA));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDropComponent>(passengerA));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeLandingClearance>(passengerA));
        Assert.IsTrue(em.HasComponent<Disabled>(passengerB));
        float3 firstStart = em.GetComponentData<LocalTransform>(passengerA).Position;
        Assert.Greater(firstStart.y, 1f);

        world.SetTime(new TimeData(1.4d, 0.1f));
        disembarkSystem.Update(world.Unmanaged);
        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length, "The second passenger must wait for the configured drop interval.");

        world.SetTime(new TimeData(2d, 0.1f));
        disembarkSystem.Update(world.Unmanaged);
        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length, "The second passenger must not start descending before the first passenger has reached the ground.");
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDropComponent>(passengerB));

        UnitTransportRopeDropComponent dropState = em.GetComponentData<UnitTransportRopeDropComponent>(passengerA);
        world.SetTime(new TimeData(dropState.StartedAt + dropState.DurationSeconds + 0.1f, 0.1f));
        dropSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDropComponent>(passengerA));
        Assert.That(em.GetComponentData<LocalTransform>(passengerA).Position.y, Is.EqualTo(dropState.EndPosition.y).Within(0.001f));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisperseComponent>(passengerA), "A passenger should receive a direct free-cell disperse after reaching the ground.");
        Assert.IsFalse(em.HasComponent<UnitTarget>(passengerA), "Rope exit disperse must not depend on pathfinding in the tight landing area.");
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(passengerA));
        Assert.AreEqual(1, em.GetComponentData<UnitMoveVisualComponent>(passengerA).IsMoving);
        UnitTransportRopeDisperseComponent passengerADisperse = em.GetComponentData<UnitTransportRopeDisperseComponent>(passengerA);
        int2 passengerATarget = passengerADisperse.EndCell;
        Assert.AreNotEqual(em.GetComponentData<UnitGrid>(passengerA).Cell, passengerATarget);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeLandingClearance>(passengerA), "Starting the move-away should immediately free the rope landing slot for the next passenger.");
        Assert.LessOrEqual(
            math.max(
                math.abs(passengerATarget.x - em.GetComponentData<UnitGrid>(passengerA).Cell.x),
                math.abs(passengerATarget.y - em.GetComponentData<UnitGrid>(passengerA).Cell.y)),
            12,
            "The post-rope move-away target should remain near the landing point.");

        world.SetTime(new TimeData(2.6d, 0.1f));
        disembarkSystem.Update(world.Unmanaged);
        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(0, passengers.Length, "The second passenger should start once the previous passenger has started moving away, without waiting for the full disperse move.");
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "The helicopter should keep the rope request active while the second passenger is descending.");
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisperseComponent>(passengerA), "The first passenger can still be moving away while the second starts descending.");
        Assert.IsFalse(em.HasComponent<Disabled>(passengerB));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDropComponent>(passengerB));

        world.SetTime(new TimeData(passengerADisperse.StartedAt + passengerADisperse.DurationSeconds + 0.1f, 0.1f));
        disperseSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisperseComponent>(passengerA));
        Assert.IsFalse(em.HasComponent<UnitTransportRopeLandingClearance>(passengerA));
        Assert.AreEqual(passengerATarget, em.GetComponentData<UnitGrid>(passengerA).Cell);

        UnitTransportRopeDropComponent passengerBDropState = em.GetComponentData<UnitTransportRopeDropComponent>(passengerB);
        world.SetTime(new TimeData(passengerBDropState.StartedAt + passengerBDropState.DurationSeconds + 0.1f, 0.1f));
        dropSystem.Update(world.Unmanaged);
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisperseComponent>(passengerB), "Each passenger should receive a direct free-cell disperse after landing.");
        UnitTransportRopeDisperseComponent passengerBDisperse = em.GetComponentData<UnitTransportRopeDisperseComponent>(passengerB);
        int2 passengerBTarget = passengerBDisperse.EndCell;
        Assert.AreNotEqual(passengerATarget, passengerBTarget, "Consecutive rope exits should target different free cells instead of stacking on one target.");
        Assert.IsFalse(em.HasComponent<UnitTransportRopeLandingClearance>(passengerB), "The final passenger should also free the rope landing slot as soon as it starts moving away.");

        world.SetTime(new TimeData(passengerBDisperse.StartedAt + passengerBDisperse.DurationSeconds + 0.1f, 0.1f));
        disperseSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeLandingClearance>(passengerB));
        Assert.AreEqual(passengerBTarget, em.GetComponentData<UnitGrid>(passengerB).Cell);

        world.SetTime(new TimeData(3.0d, 0.1f));
        disembarkSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "The helicopter may finish disembark only after the last passenger leaves the rope landing cell.");
    }

    [Test]
    public void HelicopterRopeDisembark_DropsStraightDownFromVisualModelCenter()
    {
        using var world = new World("HelicopterRopeDisembark_DropsStraightDownFromVisualModelCenter");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: true);
        LocalTransform transportTransform = em.GetComponentData<LocalTransform>(transport);
        em.AddComponentData(transport, new UnitModelLocalTransform
        {
            Position = new float3(3f, 0f, -2f),
            Rotation = quaternion.identity,
            Scale = 1f
        });

        Entity passenger = CreateLoadedPassenger(em, transport);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = passenger });
        em.AddComponentData(transport, new UnitTransportRopeDisembarkRequest
        {
            ReferenceCell = new int2(12, 8),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.8f
        });

        SystemHandle disembarkSystem = world.CreateSystem<UnitTransportRopeDisembarkSystem>();
        SystemHandle dropSystem = world.CreateSystem<UnitTransportRopeDropSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        disembarkSystem.Update(world.Unmanaged);

        UnitTransportRopeDropComponent dropState = em.GetComponentData<UnitTransportRopeDropComponent>(passenger);
        float3 expectedAnchor = transportTransform.Position + new float3(3f, 0f, -2f);
        Assert.That(dropState.StartPosition.x, Is.EqualTo(expectedAnchor.x).Within(0.001f), "Rope drop must start from the helicopter visual center X, not the side/drop cell.");
        Assert.That(dropState.StartPosition.z, Is.EqualTo(expectedAnchor.z).Within(0.001f), "Rope drop must start from the helicopter visual center Z, not the side/drop cell.");
        Assert.That(dropState.EndPosition.x, Is.EqualTo(dropState.StartPosition.x).Within(0.001f), "Rope drop must stay vertical in X.");
        Assert.That(dropState.EndPosition.z, Is.EqualTo(dropState.StartPosition.z).Within(0.001f), "Rope drop must stay vertical in Z.");
        Assert.That(dropState.EndPosition.y, Is.LessThan(dropState.StartPosition.y), "Rope drop must descend to the ground.");

        world.SetTime(new TimeData(dropState.StartedAt + dropState.DurationSeconds + 0.1f, 0.1f));
        dropSystem.Update(world.Unmanaged);
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisperseComponent>(passenger));
        UnitTransportRopeDisperseComponent disperseState = em.GetComponentData<UnitTransportRopeDisperseComponent>(passenger);
        int2 disperseTarget = disperseState.EndCell;
        int2 landingCell = GridUtils.WorldToCell(em.GetComponentData<GridConfig>(GetGridEntity(em)), dropState.EndPosition);
        Assert.AreNotEqual(landingCell, disperseTarget, "The post-landing move target should give the passenger somewhere to move away from the rope.");
        Assert.LessOrEqual(
            math.max(math.abs(disperseTarget.x - landingCell.x), math.abs(disperseTarget.y - landingCell.y)),
            12,
            "The post-landing move target should stay near the rope landing cell.");
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(passenger));
        Assert.AreEqual(1, em.GetComponentData<UnitMoveVisualComponent>(passenger).IsMoving);
    }

    [Test]
    public void HelicopterRopeDisembark_TenPassengersDisperseToDistinctFreeCells()
    {
        using var world = new World("HelicopterRopeDisembark_TenPassengersDisperseToDistinctFreeCells");
        EntityManager em = world.EntityManager;
        const int width = 40;
        CreateGrid(em, width, 40);

        Entity transport = CreateTransport(em, new int2(20, 20), air: true, airborne: true);
        Entity[] passengersToDrop = new Entity[10];
        for (int i = 0; i < passengersToDrop.Length; i++)
            passengersToDrop[i] = CreateLoadedPassenger(em, transport);

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        for (int i = 0; i < passengersToDrop.Length; i++)
            passengers.Add(new UnitTransportPassengerElement { Passenger = passengersToDrop[i] });

        em.AddComponentData(transport, new UnitTransportRopeDisembarkRequest
        {
            ReferenceCell = new int2(20, 20),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f
        });

        SystemHandle disembarkSystem = world.CreateSystem<UnitTransportRopeDisembarkSystem>();
        SystemHandle dropSystem = world.CreateSystem<UnitTransportRopeDropSystem>();
        SystemHandle disperseSystem = world.CreateSystem<UnitTransportRopeDisperseSystem>();
        HashSet<int> disperseTargets = new();
        double time = 1d;

        for (int i = 0; i < passengersToDrop.Length; i++)
        {
            Entity passenger = passengersToDrop[i];
            world.SetTime(new TimeData(time, 0.1f));
            disembarkSystem.Update(world.Unmanaged);
            Assert.IsTrue(em.HasComponent<UnitTransportRopeDropComponent>(passenger), $"Passenger {i} should start a rope drop.");

            UnitTransportRopeDropComponent dropState = em.GetComponentData<UnitTransportRopeDropComponent>(passenger);
            time = dropState.StartedAt + dropState.DurationSeconds + 0.1f;
            world.SetTime(new TimeData(time, 0.1f));
            dropSystem.Update(world.Unmanaged);

            Assert.IsTrue(em.HasComponent<UnitTransportRopeDisperseComponent>(passenger), $"Passenger {i} should start moving away after landing.");
            UnitTransportRopeDisperseComponent disperseState = em.GetComponentData<UnitTransportRopeDisperseComponent>(passenger);
            Assert.IsTrue(
                disperseTargets.Add(GridUtils.CellToIndex(disperseState.EndCell, width)),
                $"Passenger {i} should get a unique free disperse cell instead of stacking on another exited soldier.");
            Assert.AreEqual(1, em.GetComponentData<UnitMoveVisualComponent>(passenger).IsMoving, $"Passenger {i} should use the run/move visual while dispersing.");

            time = disperseState.StartedAt + disperseState.DurationSeconds + 0.1f;
            world.SetTime(new TimeData(time, 0.1f));
            disperseSystem.Update(world.Unmanaged);

            Assert.IsFalse(em.HasComponent<UnitTransportRopeLandingClearance>(passenger), $"Passenger {i} should clear the rope landing cell after moving away.");
            Assert.AreEqual(disperseState.EndCell, em.GetComponentData<UnitGrid>(passenger).Cell);
            time += 0.1d;
        }

        world.SetTime(new TimeData(time, 0.1f));
        disembarkSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport));
    }

    [Test]
    public void FocusedTransportExitButton_StartsRopeDisembarkWithoutLosingPassenger()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World("FocusedTransportExitButton_StartsRopeDisembarkWithoutLosingPassenger");
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;
        CreateGrid(em, 16, 16);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        Entity passenger = CreateLoadedPassenger(em, transport);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = passenger });

        try
        {
            Entity queue = em.CreateEntity();
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(queue);
            em.AddBuffer<RtsSelectionCommandResultElement>(queue);
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(queue);
            DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(queue);
            requests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
                TargetEntity = transport,
                HasTargetEntity = 1
            });

            var transportRequestSystem = new SelectionTransportCommandRequestSystem();
            transportRequestSystem.ProcessPendingRequests(
                em,
                queue,
                requests,
                results,
                new TransportBoardingCommandSystem(),
                new UnitTransportCapacitySystem(),
                new UnitTransportBoardingQuerySystem(),
                new UnitTransportBoardingRuleSystem(),
                new UnitTransportApproachCellSystem(),
                new UnitTransportAirPickupSystem(),
                new UnitTransportRopeDisembarkCommandSystem(),
                new UnitMoveOrderSystem(),
                new SelectionStateSystem(),
                TryGetNoClickedUnit,
                TryGetNoClickedCell);

            Assert.IsTrue(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "Exit button should start the rope disembark flow for transport helicopters.");
            Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "Passenger must remain in the helicopter buffer until the rope system drops it.");

            SystemHandle disembarkSystem = world.CreateSystem<UnitTransportRopeDisembarkSystem>();
            world.SetTime(new TimeData(1d, 0.1f));
            disembarkSystem.Update(world.Unmanaged);

            Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
            Assert.IsFalse(em.HasComponent<Disabled>(passenger));
            Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
            Assert.IsTrue(em.HasComponent<UnitTransportRopeDropComponent>(passenger));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void SelectionFallback_FindsNearbyTransportHelicopterWhenHelipadCellWasClicked()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World("SelectionFallback_FindsNearbyTransportHelicopterWhenHelipadCellWasClicked");
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;
        CreateGrid(em, 16, 16);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        airState.HomeInitialized = 1;
        airState.HomePosition = new float3(8.5f, 0f, 8.5f);
        airState.Airborne = 0;
        em.SetComponentData(transport, airState);
        em.SetComponentData(transport, LocalTransform.FromPosition(new float3(8.5f, 2.25f, 8.5f)));
        em.SetComponentData(transport, new LocalToWorld { Value = float4x4.Translate(new float3(8.5f, 2.25f, 8.5f)) });
        try
        {
            var transportCommandSystem = new TransportBoardingCommandSystem();
            bool found = transportCommandSystem.IsBoardablePlayerTransportClick(
                em,
                Vector2.zero,
                new UnitTransportBoardingRuleSystem(),
                new UnitTransportBoardingQuerySystem(),
                TryGetNoClickedUnit,
                TryGetNearbyHelipadCell);

            Assert.IsTrue(found, "Clicking the helipad/ground beside the landed transport helicopter should still resolve the boardable helicopter.");
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    private static bool TryGetNoClickedUnit(Vector2 screenPosition, EntityManager em, out Entity entity)
    {
        entity = Entity.Null;
        return false;
    }

    private static bool TryGetNoClickedCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        cell = default;
        worldPoint = default;
        return false;
    }

    private static bool TryGetNearbyHelipadCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        cell = new int2(10, 8);
        worldPoint = default;
        return true;
    }

    private void CreateGrid(EntityManager em, int width, int height)
    {
        int gridSize = width * height;
        _blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        _blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = _blockerCounts,
            Blocked = _blocked,
            FriendlyPassFactionIds = _friendlyPassFactionIds
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = _occupied
        });

        DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = new GridWalkable { Value = 1 };
    }

    private static Entity CreateTransport(EntityManager em, int2 cell, bool air, bool airborne, string sourcePrefabKey = null)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitTransportCapacity),
            typeof(LocalToWorld),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = 0 });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(entity, new UnitTransportCapacity { SoldierCapacity = 10 });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, airborne ? 8f : 0f, cell.y + 0.5f)));
        em.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(new float3(cell.x + 0.5f, airborne ? 8f : 0f, cell.y + 0.5f)) });
        em.AddBuffer<UnitTransportPassengerElement>(entity);
        if (!string.IsNullOrWhiteSpace(sourcePrefabKey))
            em.AddComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(sourcePrefabKey) });

        if (air)
        {
            em.AddComponentData(entity, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 5f });
            em.AddComponentData(entity, new UnitAirComponent
            {
                HomePosition = new float3(cell.x + 0.5f, 0f, cell.y + 0.5f),
                HomeCell = cell,
                HomeInitialized = 1,
                Airborne = (byte)(airborne ? 1 : 0)
            });
        }

        return entity;
    }

    private static Entity CreatePassenger(EntityManager em, int2 cell, Entity transport, int2 boardingGoal)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitTransportBoardingTarget),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = 0 });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitTransportBoardingTarget { Transport = transport, Goal = boardingGoal });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        em.AddBuffer<UnitTransportHiddenVisualScale>(entity);
        return entity;
    }

    private static Entity CreateLoadedPassenger(EntityManager em, Entity transport)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitTransportPassenger),
            typeof(UnitMoveVisualComponent),
            typeof(LocalTransform),
            typeof(Disabled));
        em.SetComponentData(entity, new Faction { Id = 0 });
        em.SetComponentData(entity, new UnitGrid { Cell = new int2(0, 0) });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitTransportPassenger { Transport = transport });
        em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        em.SetComponentData(entity, LocalTransform.FromPosition(float3.zero));
        em.AddBuffer<UnitTransportHiddenVisualScale>(entity);
        return entity;
    }

    private static bool TransportPassengerBufferContains(DynamicBuffer<UnitTransportPassengerElement> passengers, Entity passenger)
    {
        for (int i = 0; i < passengers.Length; i++)
        {
            if (passengers[i].Passenger == passenger)
                return true;
        }

        return false;
    }

    private static Entity GetGridEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        return query.GetSingletonEntity();
    }
}

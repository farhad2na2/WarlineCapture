#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class GameSceneTransportBoardingPlayModeTests
{
    private const string TransportHelicopterName = "Unit_Veh_Helicopter_Transport";
    private const string TransportPlaneName = "Unit_Veh_Plane_Transport";
    private const string SoldierName = "Unit_Chr_Soldier_Male_01";
    private const string VehicleName = "Unit_Veh_Tank_USA";

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
    public void DeterministicHelicopterBoardingCommand_QueuesAndBoardsSelectedSoldier()
    {
        using var world = new World("DeterministicHelicopterBoardingCommand_QueuesAndBoardsSelectedSoldier");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransportHelicopter(em, new int2(8, 8), airborne: false);
        Entity passenger = CreateSoldier(em, new int2(13, 8));
        em.AddComponent<SelectedUnitTag>(passenger);

        var commandSystem = new TransportBoardingCommandSystem();
        TransportBoardingCommandSystem.Result result = commandSystem.TryRequestBoardTransportOrderToClickedUnit(
            em,
            Vector2.zero,
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateCompositionSystemHelper(),
            (Vector2 unusedScreenPosition, EntityManager unusedEntityManager, out Entity clicked) =>
            {
                clicked = transport;
                return true;
            },
            TryGetNoClickedCell);

        Assert.IsTrue(result.Accepted, "Clicking a boardable transport helicopter with a selected soldier must queue a boarding order.");
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger), "Selected soldier must receive a boarding target from the command boundary.");

        UnitTransportBoardingTarget target = em.GetComponentData<UnitTransportBoardingTarget>(passenger);
        Assert.AreEqual(transport, target.Transport);
        MoveUnitToCell(em, passenger, target.Goal);

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passenger), "Soldier should board after reaching the helicopter boarding goal.");
        Assert.IsTrue(em.HasComponent<Disabled>(passenger), "Boarded soldier should be hidden while inside the helicopter.");
        Assert.IsTrue(TransportPassengerBufferContains(em, transport, passenger), "Helicopter passenger buffer should contain the boarded soldier.");
    }

    [Test]
    public void DeterministicHelicopterBoardThenExitCommand_BoardsAndDisembarksSameSoldier()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World("DeterministicHelicopterBoardThenExitCommand_BoardsAndDisembarksSameSoldier");
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransportHelicopter(em, new int2(8, 8), airborne: false);
        Entity passenger = CreateSoldier(em, new int2(13, 8));
        em.AddComponent<SelectedUnitTag>(passenger);

        try
        {
            var commandSystem = new TransportBoardingCommandSystem();
            TransportBoardingCommandSystem.Result result = commandSystem.TryRequestBoardTransportOrderToClickedUnit(
                em,
                Vector2.zero,
                new UnitTransportAirPickupSystem(),
                new UnitMoveOrderSystem(),
                new SelectionStateCompositionSystemHelper(),
                (Vector2 unusedScreenPosition, EntityManager unusedEntityManager, out Entity clicked) =>
                {
                    clicked = transport;
                    return true;
                },
                TryGetNoClickedCell);

            Assert.IsTrue(result.Accepted, "Board command should accept a selected soldier and landed transport.");
            UnitTransportBoardingTarget target = em.GetComponentData<UnitTransportBoardingTarget>(passenger);
            MoveUnitToCell(em, passenger, target.Goal);

            SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
            world.SetTime(new TimeData(1d, 0.1f));
            boardingSystem.Update(world.Unmanaged);

            AssertPassengerBoarded(em, transport, passenger);

            Assert.IsTrue(RequestDisembarkTransportForTest(world, em, transport), "Transport exit command should start rope disembark for the boarded passenger.");
            Assert.IsTrue(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "Boarded transport should receive a rope disembark request.");

            SystemHandle disembarkSystem = world.CreateSystem<UnitTransportRopeDisembarkSystem>();
            SystemHandle dropSystem = world.CreateSystem<UnitTransportRopeDropSystem>();
            SystemHandle disperseSystem = world.CreateSystem<UnitTransportRopeDisperseSystem>();

            world.SetTime(new TimeData(2d, 0.1f));
            disembarkSystem.Update(world.Unmanaged);

            AssertPassengerStartedDrop(em, passenger);
            CompleteDropAndDisperse(world, dropSystem, disperseSystem, em, passenger);

            world.SetTime(new TimeData(5d, 0.1f));
            disembarkSystem.Update(world.Unmanaged);

            Assert.IsFalse(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "Rope disembark should clear after the boarded passenger exits.");
            AssertPassengerFinishedExit(em, passenger);
            Assert.IsFalse(TransportPassengerBufferContains(em, transport, passenger), "Transport passenger buffer should no longer contain the exited soldier.");
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void DeterministicHelicopterExitCommand_DropsAndDispersesPassengers()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World("DeterministicHelicopterExitCommand_DropsAndDispersesPassengers");
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransportHelicopter(em, new int2(10, 10), airborne: false);
        Entity passengerA = CreateLoadedPassenger(em, transport);
        Entity passengerB = CreateLoadedPassenger(em, transport);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = passengerA });
        passengers.Add(new UnitTransportPassengerElement { Passenger = passengerB });

        try
        {
            Assert.IsTrue(RequestDisembarkTransportForTest(world, em, transport), "Transport exit command must start rope disembark for the helicopter.");
            Assert.IsTrue(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "Rope disembark request should be attached to the helicopter.");

            SystemHandle disembarkSystem = world.CreateSystem<UnitTransportRopeDisembarkSystem>();
            SystemHandle dropSystem = world.CreateSystem<UnitTransportRopeDropSystem>();
            SystemHandle disperseSystem = world.CreateSystem<UnitTransportRopeDisperseSystem>();

            world.SetTime(new TimeData(1d, 0.1f));
            disembarkSystem.Update(world.Unmanaged);

            AssertPassengerStartedDrop(em, passengerA);
            Assert.IsTrue(em.HasComponent<Disabled>(passengerB), "Second passenger should wait for the first rope interval.");

            CompleteDropAndDisperse(world, dropSystem, disperseSystem, em, passengerA);

            world.SetTime(new TimeData(3d, 0.1f));
            disembarkSystem.Update(world.Unmanaged);

            AssertPassengerStartedDrop(em, passengerB);
            CompleteDropAndDisperse(world, dropSystem, disperseSystem, em, passengerB);

            world.SetTime(new TimeData(5d, 0.1f));
            disembarkSystem.Update(world.Unmanaged);

            Assert.IsFalse(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "Rope disembark should finish after all passengers leave and clear the landing point.");
            AssertPassengerFinishedExit(em, passengerA);
            AssertPassengerFinishedExit(em, passengerB);
            Assert.AreNotEqual(em.GetComponentData<UnitGrid>(passengerA).Cell, em.GetComponentData<UnitGrid>(passengerB).Cell, "Exited passengers should disperse to different cells.");
            Assert.IsFalse(TransportPassengerBufferContains(em, transport, passengerA));
            Assert.IsFalse(TransportPassengerBufferContains(em, transport, passengerB));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void DeterministicTransportPlaneBoardingCommand_BoardsSelectedSoldierThroughRearRamp()
    {
        using var world = new World("DeterministicTransportPlaneBoardingCommand_BoardsSelectedSoldierThroughRearRamp");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransportPlane(em, new int2(12, 12), airborne: false);
        Entity passenger = CreateSoldier(em, new int2(17, 12));
        em.AddComponent<SelectedUnitTag>(passenger);

        var commandSystem = new TransportBoardingCommandSystem();
        TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToTransport(
            em,
            transport,
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateCompositionSystemHelper());

        Assert.IsTrue(result.Accepted, "Selected soldier should be ordered into the landed transport plane.");
        UnitTransportBoardingTarget target = em.GetComponentData<UnitTransportBoardingTarget>(passenger);
        Assert.AreEqual(transport, target.Transport);
        Assert.AreEqual(UnitTransportPassengerKind.Soldier, target.PassengerKind);
        Assert.AreEqual(new int2(12, 7), target.Goal, "Soldier should path to the rear-ramp approach cell.");

        SystemHandle doorSystem = world.CreateSystem<UnitTransportPlaneDoorSystem>();
        world.SetTime(new TimeData(0.25d, 0.25f));
        doorSystem.Update(world.Unmanaged);
        Assert.AreEqual(1, em.GetComponentData<UnitTransportPlaneDoorState>(transport).TargetOpen, "Rear ramp should open while boarding is pending.");

        MoveUnitToCell(em, passenger, target.Goal);
        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        AssertPassengerBoarded(em, transport, passenger);
        Assert.IsFalse(em.HasComponent<UnitTransportCargoPassenger>(passenger), "Soldier passenger should not be tagged as cargo.");

        world.SetTime(new TimeData(1.2d, 0.2f));
        doorSystem.Update(world.Unmanaged);
        Assert.AreEqual(0, em.GetComponentData<UnitTransportPlaneDoorState>(transport).TargetOpen, "Rear ramp should close after boarding completes.");
    }

    [Test]
    public void DeterministicTransportPlaneBoardingCommand_BoardsSelectedVehicleThroughRearRamp()
    {
        using var world = new World("DeterministicTransportPlaneBoardingCommand_BoardsSelectedVehicleThroughRearRamp");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransportPlane(em, new int2(12, 12), airborne: false);
        Entity vehicle = CreateVehicle(em, new int2(17, 12));
        em.AddComponent<SelectedUnitTag>(vehicle);

        var commandSystem = new TransportBoardingCommandSystem();
        TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToTransport(
            em,
            transport,
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateCompositionSystemHelper());

        Assert.IsTrue(result.Accepted, "Selected vehicle should be ordered into the landed transport plane.");
        UnitTransportBoardingTarget target = em.GetComponentData<UnitTransportBoardingTarget>(vehicle);
        Assert.AreEqual(transport, target.Transport);
        Assert.AreEqual(UnitTransportPassengerKind.Vehicle, target.PassengerKind);
        Assert.AreEqual(new int2(12, 7), target.Goal, "Vehicle should drive to the rear-ramp approach cell.");

        SystemHandle doorSystem = world.CreateSystem<UnitTransportPlaneDoorSystem>();
        world.SetTime(new TimeData(0.25d, 0.25f));
        doorSystem.Update(world.Unmanaged);
        Assert.AreEqual(1, em.GetComponentData<UnitTransportPlaneDoorState>(transport).TargetOpen, "Rear ramp should open while cargo boarding is pending.");

        MoveUnitToCell(em, vehicle, target.Goal);
        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        AssertPassengerBoarded(em, transport, vehicle);
        Assert.IsTrue(em.HasComponent<UnitTransportCargoPassenger>(vehicle), "Boarded vehicle should be tracked as cargo.");
        UnitTransportCargoPassenger cargo = em.GetComponentData<UnitTransportCargoPassenger>(vehicle);
        Assert.AreEqual(UnitTransportPassengerKind.Vehicle, cargo.PassengerKind);

        world.SetTime(new TimeData(1.2d, 0.2f));
        doorSystem.Update(world.Unmanaged);
        Assert.AreEqual(0, em.GetComponentData<UnitTransportPlaneDoorState>(transport).TargetOpen, "Rear ramp should close after cargo boarding completes.");
    }

    [Test]
    public void DeterministicTransportPlaneExitCommand_UnloadsPassengersThroughRearRamp()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World("DeterministicTransportPlaneExitCommand_UnloadsPassengersThroughRearRamp");
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransportPlane(em, new int2(12, 12), airborne: false);
        Entity soldier = CreateLoadedPassenger(em, transport);
        Entity vehicle = CreateLoadedVehiclePassenger(em, transport);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = soldier });
        passengers.Add(new UnitTransportPassengerElement { Passenger = vehicle });

        try
        {
            Assert.IsTrue(RequestDisembarkTransportForTest(world, em, transport), "Landed plane exit command should start rear-ramp unload.");
            Assert.IsTrue(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport), "Rear door should be explicitly held open during ramp unload.");
            passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            Assert.AreEqual(0, passengers.Length);

            AssertPassengerFinishedRampUnload(em, soldier, new int2(12, 7), UnitTransportPassengerKind.Soldier);
            AssertPassengerFinishedRampUnload(em, vehicle, new int2(12, 7), UnitTransportPassengerKind.Vehicle);
            Assert.IsTrue(em.HasComponent<UnitTarget>(soldier), "Unloaded soldier should get a visible walk-out target.");
            Assert.IsTrue(em.HasComponent<UnitTarget>(vehicle), "Unloaded vehicle should get a visible rollout target.");

            SystemHandle doorSystem = world.CreateSystem<UnitTransportPlaneDoorSystem>();
            world.SetTime(new TimeData(0.4d, 0.25f));
            doorSystem.Update(world.Unmanaged);
            Assert.AreEqual(1, em.GetComponentData<UnitTransportPlaneDoorState>(transport).TargetOpen, "Rear ramp should stay open during landed unload.");
            Assert.IsTrue(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport));

            world.SetTime(new TimeData(3.2d, 3f));
            doorSystem.Update(world.Unmanaged);
            Assert.IsFalse(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport));
            world.SetTime(new TimeData(3.3d, 0.1f));
            doorSystem.Update(world.Unmanaged);
            Assert.AreEqual(0, em.GetComponentData<UnitTransportPlaneDoorState>(transport).TargetOpen, "Rear ramp should close after landed unload hold expires.");
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void DeterministicTransportPlaneAirdrop_DropsSoldierWithParachuteToGround()
    {
        using var world = new World("DeterministicTransportPlaneAirdrop_DropsSoldierWithParachuteToGround");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransportPlane(em, new int2(12, 12), airborne: true);
        AddAirdropVisualPrefabs(em, transport);
        Entity soldier = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = soldier });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 1,
            SoldierDropCount = 1,
            DropMode = UnitTransportAirdropMode.SoldierOnly,
            PassReady = 1
        });

        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitTransportParachuteDropComponent>(soldier), "Airdropped soldier should descend under parachute state.");
        UnitTransportParachuteDropComponent drop = em.GetComponentData<UnitTransportParachuteDropComponent>(soldier);
        AssertVisualTracksPassenger(em, soldier, drop.VisualEntity, 2.2f);

        world.SetTime(new TimeData(drop.StartedAt + drop.DurationSeconds + 0.1f, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        AssertPassengerLandedFromAirdrop(em, soldier, new int2(15, 15));
        Assert.IsTrue(em.Exists(drop.VisualEntity), "Parachute visual should linger briefly after touchdown.");
    }

    [Test]
    public void DeterministicTransportPlaneAirdrop_DropsVehicleWithEmergencyRigToGround()
    {
        using var world = new World("DeterministicTransportPlaneAirdrop_DropsVehicleWithEmergencyRigToGround");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransportPlane(em, new int2(12, 12), airborne: true);
        AddAirdropVisualPrefabs(em, transport);
        Entity vehicle = CreateLoadedVehiclePassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = vehicle });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 1,
            VehicleDropCount = 1,
            DropMode = UnitTransportAirdropMode.VehicleOnly,
            PassReady = 1
        });

        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitTransportCargoDropComponent>(vehicle), "Airdropped vehicle should descend under cargo-drop state.");
        UnitTransportCargoDropComponent drop = em.GetComponentData<UnitTransportCargoDropComponent>(vehicle);
        AssertVisualTracksPassenger(em, vehicle, drop.VisualEntity, 1.6f);

        world.SetTime(new TimeData(drop.StartedAt + drop.DurationSeconds + 0.1f, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        AssertPassengerLandedFromAirdrop(em, vehicle, new int2(15, 15));
        Assert.IsTrue(em.Exists(drop.VisualEntity), "Emergency-drop visual should linger briefly after touchdown.");
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

    private static Entity CreateTransportHelicopter(EntityManager em, int2 cell, bool airborne)
    {
        float3 position = new(cell.x + 0.5f, airborne ? 8f : 0f, cell.y + 0.5f);
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitTransportCapacity),
            typeof(UnitSourcePrefabKey),
            typeof(UnitAirMovement),
            typeof(UnitAirComponent),
            typeof(LocalTransform),
            typeof(LocalToWorld));
        em.SetName(entity, TransportHelicopterName);
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(entity, new UnitTransportCapacity { SoldierCapacity = 10 });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(TransportHelicopterName) });
        em.SetComponentData(entity, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 5f });
        em.SetComponentData(entity, new UnitAirComponent
        {
            HomePosition = new float3(cell.x + 0.5f, 0f, cell.y + 0.5f),
            HomeCell = cell,
            HomeInitialized = 1,
            Airborne = (byte)(airborne ? 1 : 0)
        });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
        em.AddBuffer<UnitTransportPassengerElement>(entity);
        return entity;
    }

    private static Entity CreateTransportPlane(EntityManager em, int2 cell, bool airborne)
    {
        float3 position = new(cell.x + 0.5f, airborne ? 55f : 0f, cell.y + 0.5f);
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitTransportCapacity),
            typeof(UnitTransportCargoCapacity),
            typeof(UnitSourcePrefabKey),
            typeof(UnitAirMovement),
            typeof(UnitAirComponent),
            typeof(UnitTransportPlaneDoorReference),
            typeof(UnitTransportPlaneDoorState),
            typeof(LocalTransform),
            typeof(LocalToWorld));
        em.SetName(entity, TransportPlaneName);
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(5, 5) });
        em.SetComponentData(entity, new UnitTransportCapacity { SoldierCapacity = 24 });
        em.SetComponentData(entity, new UnitTransportCargoCapacity
        {
            SoldierCapacity = 24,
            VehicleCapacity = 2,
            CargoWeightCapacity = 0
        });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(TransportPlaneName) });
        em.SetComponentData(entity, new UnitAirMovement { CruiseHeight = 55f, RunwayTaxiSpeed = 8f });
        em.SetComponentData(entity, new UnitAirComponent
        {
            HomePosition = new float3(cell.x + 0.5f, 0f, cell.y + 0.5f),
            HomeCell = cell,
            HomeInitialized = 1,
            Airborne = (byte)(airborne ? 1 : 0),
            UsesRunway = 1
        });
        em.SetComponentData(entity, new UnitTransportPlaneDoorReference
        {
            DoorEntity = Entity.Null,
            ClosedLocalRotation = quaternion.identity,
            OpenLocalRotation = quaternion.identity,
            OpenSeconds = 1.1f,
            CloseSeconds = 0.9f,
            DoorLocalPosition = new float3(0f, 0f, -4f),
            InteriorLocalPosition = new float3(0f, 1.45f, 4f),
            ApproachLocalPosition = new float3(0f, 0f, -5f),
            RolloutLocalPosition = new float3(0f, 0f, -5f)
        });
        em.SetComponentData(entity, new UnitTransportPlaneDoorState());
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
        em.AddBuffer<UnitTransportPassengerElement>(entity);
        return entity;
    }

    private static Entity CreateSoldier(EntityManager em, int2 cell)
    {
        float3 position = new(cell.x + 0.5f, 0f, cell.y + 0.5f);
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitMoveVisualComponent),
            typeof(UnitSourcePrefabKey),
            typeof(LocalTransform),
            typeof(LocalToWorld));
        em.SetName(entity, SoldierName);
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(SoldierName) });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
        em.AddBuffer<UnitTransportHiddenVisualScale>(entity);
        return entity;
    }

    private static Entity CreateVehicle(EntityManager em, int2 cell)
    {
        float3 position = new(cell.x + 0.5f, 0f, cell.y + 0.5f);
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitMoveVisualComponent),
            typeof(UnitSourcePrefabKey),
            typeof(LocalTransform),
            typeof(LocalToWorld));
        em.SetName(entity, VehicleName);
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(entity, new UnitMove { Speed = 7f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 1 });
        em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(VehicleName) });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
        em.AddBuffer<UnitTransportHiddenVisualScale>(entity);
        return entity;
    }

    private static Entity CreateLoadedPassenger(EntityManager em, Entity transport)
    {
        Entity passenger = CreateSoldier(em, new int2(0, 0));
        em.AddComponentData(passenger, new UnitTransportPassenger { Transport = transport });
        em.AddComponent<Disabled>(passenger);
        return passenger;
    }

    private static Entity CreateLoadedVehiclePassenger(EntityManager em, Entity transport)
    {
        Entity passenger = CreateVehicle(em, new int2(0, 0));
        em.AddComponentData(passenger, new UnitTransportPassenger { Transport = transport });
        em.AddComponentData(passenger, new UnitTransportCargoPassenger
        {
            Transport = transport,
            PassengerKind = UnitTransportPassengerKind.Vehicle,
            CargoWeight = 9
        });
        em.AddComponent<Disabled>(passenger);
        return passenger;
    }

    private static void AddAirdropVisualPrefabs(EntityManager em, Entity transport)
    {
        Entity parachutePrefab = CreateAirdropVisualPrefab(em, "ParachuteVisual");
        Entity cargoPrefab = CreateAirdropVisualPrefab(em, "CargoVisual");
        em.AddComponentData(transport, new UnitTransportAirdropVisualPrefabs
        {
            SoldierParachuteVisualPrefab = parachutePrefab,
            VehicleEmergencyDropVisualPrefab = cargoPrefab
        });
    }

    private static Entity CreateAirdropVisualPrefab(EntityManager em, string name)
    {
        Entity entity = em.CreateEntity(typeof(Prefab), typeof(LocalTransform));
        em.SetName(entity, name);
        em.SetComponentData(entity, LocalTransform.FromPosition(float3.zero));
        return entity;
    }

    private static void MoveUnitToCell(EntityManager em, Entity entity, int2 cell)
    {
        GridConfig grid = GetGrid(em);
        float3 position = GridUtils.CellToWorldCenter(grid, cell);
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
        RemoveIfPresent<UnitTarget>(em, entity);
        RemoveIfPresent<UnitPathRequest>(em, entity);
        RemoveIfPresent<UnitPathFollow>(em, entity);
        RemoveIfPresent<UnitPathRange>(em, entity);
        RemoveIfPresent<ManualMoveOrderTag>(em, entity);
    }

    private static bool RequestDisembarkTransportForTest(World world, EntityManager em, Entity transport)
    {
        Entity queue = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(queue);
        em.AddBuffer<RtsSelectionCommandResultElement>(queue);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(queue);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);

        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(queue);
        return results.Length > 0 && results[results.Length - 1].Accepted != 0;
    }

    private static void AssertPassengerBoarded(EntityManager em, Entity transport, Entity passenger)
    {
        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passenger), "Passenger should be tracked as inside the transport.");
        Assert.IsTrue(em.HasComponent<Disabled>(passenger), "Boarded passenger should be hidden while inside the transport.");
        Assert.IsTrue(TransportPassengerBufferContains(em, transport, passenger), "Transport passenger buffer should contain the boarded passenger.");
    }

    private static void AssertPassengerFinishedRampUnload(EntityManager em, Entity passenger, int2 rampCell, byte passengerKind)
    {
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportCargoPassenger>(passenger));
        UnitGrid grid = em.GetComponentData<UnitGrid>(passenger);
        Assert.LessOrEqual(math.abs(grid.Cell.x - rampCell.x), 3, "Ramp unload should place passengers near the rear ramp lane.");
        Assert.LessOrEqual(grid.Cell.y, rampCell.y, "Ramp unload should place passengers behind the plane.");
        Assert.AreEqual(passengerKind == UnitTransportPassengerKind.Vehicle ? 3 : 1, em.GetComponentData<UnitFootprint>(passenger).Size.x);
    }

    private static void AssertPassengerLandedFromAirdrop(EntityManager em, Entity passenger, int2 expectedLandingCell)
    {
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportCargoPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportParachuteDropComponent>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportCargoDropComponent>(passenger));
        Assert.AreEqual(expectedLandingCell, em.GetComponentData<UnitGrid>(passenger).Cell);
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(passenger).Position.y, 0.001f);
        Assert.IsTrue(em.HasComponent<UnitTransportAirdropSettleComponent>(passenger), "Landed passenger should begin the short settle/rollout motion.");
    }

    private static void AssertVisualTracksPassenger(EntityManager em, Entity passenger, Entity visual, float expectedHeightOffset)
    {
        Assert.AreNotEqual(Entity.Null, visual);
        Assert.IsTrue(em.Exists(visual));
        Assert.IsTrue(em.HasComponent<LocalTransform>(visual));
        LocalTransform passengerTransform = em.GetComponentData<LocalTransform>(passenger);
        LocalTransform visualTransform = em.GetComponentData<LocalTransform>(visual);
        Assert.AreEqual(passengerTransform.Position.x, visualTransform.Position.x, 0.001f);
        Assert.AreEqual(passengerTransform.Position.z, visualTransform.Position.z, 0.001f);
        Assert.AreEqual(passengerTransform.Position.y + expectedHeightOffset, visualTransform.Position.y, 0.001f);
        Assert.AreEqual(1f, visualTransform.Scale, 0.001f);
    }

    private static void AssertPassengerStartedDrop(EntityManager em, Entity passenger)
    {
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDropComponent>(passenger), "Passenger should be in rope drop state.");
        Assert.IsTrue(em.HasComponent<UnitTransportRopeLandingClearance>(passenger), "Passenger should reserve the rope landing point while descending.");
    }

    private static void CompleteDropAndDisperse(
        World world,
        SystemHandle dropSystem,
        SystemHandle disperseSystem,
        EntityManager em,
        Entity passenger)
    {
        UnitTransportRopeDropComponent drop = em.GetComponentData<UnitTransportRopeDropComponent>(passenger);
        world.SetTime(new TimeData(drop.StartedAt + drop.DurationSeconds + 0.1f, 0.1f));
        dropSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDropComponent>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisperseComponent>(passenger), "Passenger should disperse after reaching the ground.");

        UnitTransportRopeDisperseComponent disperse = em.GetComponentData<UnitTransportRopeDisperseComponent>(passenger);
        world.SetTime(new TimeData(disperse.StartedAt + disperse.DurationSeconds + 0.1f, 0.1f));
        disperseSystem.Update(world.Unmanaged);
    }

    private static void AssertPassengerFinishedExit(EntityManager em, Entity passenger)
    {
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDropComponent>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisperseComponent>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportRopeLandingClearance>(passenger));
    }

    private static bool TransportPassengerBufferContains(EntityManager em, Entity transport, Entity passenger)
    {
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        for (int i = 0; i < passengers.Length; i++)
        {
            if (passengers[i].Passenger == passenger)
                return true;
        }

        return false;
    }

    private static GridConfig GetGrid(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        return em.GetComponentData<GridConfig>(query.GetSingletonEntity());
    }

    private static bool TryGetNoClickedUnit(Vector2 _, EntityManager __, out Entity entity)
    {
        entity = Entity.Null;
        return false;
    }

    private static bool TryGetNoClickedCell(Vector2 _, EntityManager __, out int2 cell, out Vector3 worldPoint)
    {
        cell = default;
        worldPoint = default;
        return false;
    }

    private static void RemoveIfPresent<T>(EntityManager em, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            em.RemoveComponent<T>(entity);
    }
}
#endif

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
    private const string SoldierName = "Unit_Chr_Soldier_Male_01";

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
        TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToClickedUnit(
            em,
            Vector2.zero,
            new UnitTransportBoardingQuerySystem(),
            new UnitTransportBoardingRuleSystem(),
            new UnitTransportApproachCellSystem(),
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateSystem(),
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
            Assert.IsTrue(RequestDisembarkTransportForTest(em, transport), "Transport exit command must start rope disembark for the helicopter.");
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
        em.SetComponentData(entity, new Faction { Id = 0 });
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
        em.SetComponentData(entity, new Faction { Id = 0 });
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

    private static Entity CreateLoadedPassenger(EntityManager em, Entity transport)
    {
        Entity passenger = CreateSoldier(em, new int2(0, 0));
        em.AddComponentData(passenger, new UnitTransportPassenger { Transport = transport });
        em.AddComponent<Disabled>(passenger);
        return passenger;
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

    private static bool RequestDisembarkTransportForTest(EntityManager em, Entity transport)
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

        bool processed = new SelectionTransportCommandRequestSystem().ProcessPendingRequests(
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

        results = em.GetBuffer<RtsSelectionCommandResultElement>(queue);
        return processed && results.Length > 0 && results[results.Length - 1].Accepted != 0;
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

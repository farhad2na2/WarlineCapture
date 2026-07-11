using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class Aph807TransportBoardingFlowPlayModeTests
{
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;
    private NativeList<int2> _pathPool;

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
        if (_pathPool.IsCreated)
            _pathPool.Dispose();
    }

    [Test]
    public void Tb001_ProductionInputGateway_BoardsThenDisembarksSelectedSoldier()
    {
        Assert.IsTrue(
            TransportBoardingScenarioCatalog.TryGetScenario(
                TransportBoardingScenarioCatalog.Tb001GroundVehicleBoardExitId,
                out TransportBoardingScenarioDescriptor scenario));
        Assert.AreEqual(TransportBoardingScenarioExitMode.Ground, scenario.ExitMode);

        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World($"APH-807_{scenario.ScenarioId}");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            EntityManager em = world.EntityManager;
            CreateGrid(em, 18, 18);
            Entity transport = CreateTransport(em, new int2(8, 8));
            Entity passenger = CreateSelectedPassenger(em, new int2(13, 8));
            var gateway = new RtsSelectionInputCompositionSystemHelper();
            SystemHandle commandSystem = world.CreateSystem<TransportBoardingCommandSystem>();

            Assert.IsTrue(gateway.QueueBoardTransportCommandRequest(transport, default, frame: 10));
            world.SetTime(new TimeData(0.1d, 0.1f));
            commandSystem.Update(world.Unmanaged);

            DynamicBuffer<RtsSelectionCommandResultElement> results = GetCommandResults(gateway);
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(1, results[0].Accepted);
            Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));

            UnitTransportBoardingTarget target = em.GetComponentData<UnitTransportBoardingTarget>(passenger);
            MovePassengerToCell(em, passenger, target.Goal);
            SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
            world.SetTime(new TimeData(1d, 0.1f));
            boardingSystem.Update(world.Unmanaged);

            Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passenger));
            Assert.IsTrue(em.HasComponent<Disabled>(passenger));
            Assert.AreEqual(passenger, em.GetBuffer<UnitTransportPassengerElement>(transport)[0].Passenger);

            Assert.IsTrue(gateway.QueueDisembarkTransportCommandRequest(transport, frame: 20));
            world.SetTime(new TimeData(2d, 0.1f));
            commandSystem.Update(world.Unmanaged);

            results = GetCommandResults(gateway);
            Assert.AreEqual(2, results.Length);
            Assert.AreEqual(1, results[1].Accepted);
            Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
            Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
            Assert.IsFalse(em.HasComponent<Disabled>(passenger));
            Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(passenger));
            Assert.AreNotEqual(
                em.GetComponentData<UnitGrid>(transport).Cell,
                em.GetComponentData<UnitGrid>(passenger).Cell);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    private static DynamicBuffer<RtsSelectionCommandResultElement> GetCommandResults(
        RtsSelectionInputCompositionSystemHelper gateway)
    {
        Assert.IsTrue(gateway.TryGetCommandBuffers(out _, out _, out DynamicBuffer<RtsSelectionCommandResultElement> results));
        return results;
    }

    private void CreateGrid(EntityManager em, int width, int height)
    {
        int gridSize = width * height;
        _blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        _blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
        _pathPool = new NativeList<int2>(1024, Allocator.Persistent);
        for (int i = 0; i < gridSize; i++)
            _friendlyPassFactionIds[i] = byte.MaxValue;

        Entity grid = em.CreateEntity(
            typeof(GridConfig),
            typeof(DynamicBlockerComponent),
            typeof(DynamicOccupancyComponent),
            typeof(PathPoolComponent));
        em.SetComponentData(grid, new GridConfig
        {
            Width = width,
            Height = height,
            CellSize = 1f,
            Origin = float3.zero
        });
        em.SetComponentData(grid, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = _blockerCounts,
            Blocked = _blocked,
            FriendlyPassFactionIds = _friendlyPassFactionIds
        });
        em.SetComponentData(grid, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = _occupied
        });
        em.SetComponentData(grid, new PathPoolComponent { Cells = _pathPool });

        em.AddBuffer<GridWalkable>(grid);
        em.AddBuffer<GridRoad>(grid);
        em.AddBuffer<GridRoadSidewalk>(grid);
        em.AddBuffer<GridRoadDirt>(grid);
        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(grid);
        DynamicBuffer<GridRoad> roads = em.GetBuffer<GridRoad>(grid);
        DynamicBuffer<GridRoadSidewalk> sidewalks = em.GetBuffer<GridRoadSidewalk>(grid);
        DynamicBuffer<GridRoadDirt> dirtRoads = em.GetBuffer<GridRoadDirt>(grid);
        walkable.ResizeUninitialized(gridSize);
        roads.ResizeUninitialized(gridSize);
        sidewalks.ResizeUninitialized(gridSize);
        dirtRoads.ResizeUninitialized(gridSize);
        for (int i = 0; i < gridSize; i++)
        {
            walkable[i] = new GridWalkable { Value = 1 };
            roads[i] = new GridRoad { Value = 0 };
            sidewalks[i] = new GridRoadSidewalk { Value = 0 };
            dirtRoads[i] = new GridRoadDirt { Value = 0 };
        }
    }

    private static Entity CreateTransport(EntityManager em, int2 cell)
    {
        Entity transport = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitTransportCapacity),
            typeof(UnitSourcePrefabKey),
            typeof(LocalToWorld),
            typeof(LocalTransform));
        float3 position = new(cell.x + 0.5f, 0f, cell.y + 0.5f);
        em.SetComponentData(transport, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(transport, new UnitGrid { Cell = cell });
        em.SetComponentData(transport, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 10 });
        em.SetComponentData(transport, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_APC_01") });
        em.SetComponentData(transport, LocalTransform.FromPosition(position));
        em.SetComponentData(transport, new LocalToWorld { Value = float4x4.Translate(position) });
        em.AddBuffer<UnitTransportPassengerElement>(transport);
        return transport;
    }

    private static Entity CreateSelectedPassenger(EntityManager em, int2 cell)
    {
        Entity passenger = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitMoveVisualComponent),
            typeof(SelectedUnitTag),
            typeof(LocalTransform),
            typeof(LocalToWorld));
        float3 position = new(cell.x + 0.5f, 0f, cell.y + 0.5f);
        em.SetComponentData(passenger, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(passenger, new UnitGrid { Cell = cell });
        em.SetComponentData(passenger, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(passenger, new UnitMove
        {
            Speed = 4f,
            WalkSpeed = 1.5f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        em.SetComponentData(passenger, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
        em.SetComponentData(passenger, new UnitMoveVisualComponent());
        em.SetComponentData(passenger, LocalTransform.FromPosition(position));
        em.SetComponentData(passenger, new LocalToWorld { Value = float4x4.Translate(position) });
        em.AddBuffer<UnitTransportHiddenVisualScale>(passenger);
        return passenger;
    }

    private static void MovePassengerToCell(EntityManager em, Entity passenger, int2 cell)
    {
        float3 position = new(cell.x + 0.5f, 0f, cell.y + 0.5f);
        em.SetComponentData(passenger, new UnitGrid { Cell = cell });
        em.SetComponentData(passenger, LocalTransform.FromPosition(position));
        em.SetComponentData(passenger, new LocalToWorld { Value = float4x4.Translate(position) });
    }
}

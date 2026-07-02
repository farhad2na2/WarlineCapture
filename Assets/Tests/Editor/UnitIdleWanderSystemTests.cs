using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;

public sealed class UnitIdleWanderSystemTests
{
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(nameof(IdleWander_NonFactionUnitIssuesPathRequest), test => test.IdleWander_NonFactionUnitIssuesPathRequest());
            RunCase(nameof(IdleWander_FactionOwnedUnitStaysIdle), test => test.IdleWander_FactionOwnedUnitStaysIdle());
            UnityEngine.Debug.Log("[UnitIdleWanderFocusedValidation] result=Passed tests=2");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError("[UnitIdleWanderFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(string name, System.Action<UnitIdleWanderSystemTests> action)
    {
        var tests = new UnitIdleWanderSystemTests();
        try
        {
            action(tests);
            UnityEngine.Debug.Log($"[UnitIdleWanderFocusedValidation] passed={name}");
        }
        finally
        {
            tests.TearDown();
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (_friendlyPassFactionIds.IsCreated)
            _friendlyPassFactionIds.Dispose();
        if (_occupied.IsCreated)
            _occupied.Dispose();
        if (_blocked.IsCreated)
            _blocked.Dispose();
        if (_blockerCounts.IsCreated)
            _blockerCounts.Dispose();
    }

    [Test]
    public void IdleWander_NonFactionUnitIssuesPathRequest()
    {
        using var world = new World("UnitIdleWanderSystemTests_NonFaction");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);
        Entity unit = CreateIdleWanderUnit(em, addFaction: false);

        SystemHandle system = world.CreateSystem<UnitIdleWanderSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitTarget>(unit));
        Assert.IsTrue(em.HasComponent<UnitPathRequest>(unit));
        Assert.IsTrue(em.HasComponent<AutoWanderMoveTag>(unit));
        Assert.AreNotEqual(new int2(10, 10), em.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(0.75f, em.GetComponentData<UnitIdleWanderComponent>(unit).RetrySeconds, 0.001f);
    }

    [Test]
    public void IdleWander_FactionOwnedUnitStaysIdle()
    {
        using var world = new World("UnitIdleWanderSystemTests_FactionOwned");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);
        Entity unit = CreateIdleWanderUnit(em, addFaction: true);

        SystemHandle system = world.CreateSystem<UnitIdleWanderSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(em.HasComponent<AutoWanderMoveTag>(unit));
    }

    private Entity CreateIdleWanderUnit(EntityManager em, bool addFaction)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMovementBehavior),
            typeof(UnitMoveVisualComponent),
            typeof(UnitAnimationSettings),
            typeof(UnitIdleWanderComponent),
            typeof(UnitHealth));

        em.SetComponentData(entity, new UnitGrid { Cell = new int2(10, 10) });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 1, UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 12f });
        em.SetComponentData(entity, new UnitAnimationSettings
        {
            IdleDelayMinSeconds = 0f,
            IdleDelayMaxSeconds = 0f,
            IdleWanderDistanceMin = 3f,
            IdleWanderDistanceMax = 4f
        });
        em.SetComponentData(entity, new UnitIdleWanderComponent
        {
            RandomState = 7,
            RetrySeconds = 0f,
            CurrentIdleDelaySeconds = 0f
        });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });

        if (addFaction)
            em.AddComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });

        return entity;
    }

    private void CreateGrid(EntityManager em, int width, int height)
    {
        int gridSize = width * height;
        _blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        _blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
        for (int i = 0; i < _friendlyPassFactionIds.Length; i++)
            _friendlyPassFactionIds[i] = byte.MaxValue;

        Entity gridEntity = em.CreateEntity(
            typeof(GridConfig),
            typeof(DynamicBlockerComponent),
            typeof(DynamicOccupancyComponent),
            typeof(GridWalkable));
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

        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < gridSize; i++)
            walkable[i] = new GridWalkable { Value = 1 };
    }
}
#endif

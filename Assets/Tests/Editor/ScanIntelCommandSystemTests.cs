using Game.Tactical.Contracts;
using Game.Components;
using Game.Runtime;

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class ScanIntelCommandSystemTests
{
    private World _world;
    private EntityManager _entityManager;

    public static void RunFocusedValidation()
    {
        var tests = new ScanIntelCommandSystemTests();
        try
        {
            tests.SetUp();
            tests.TryIssueScan_RevealsHostileTargetsAndWritesIntelFeed();
            tests.TearDown();

            tests.SetUp();
            tests.TryIssueScan_RejectsWhenClickedCellCannotResolve();
            UnityEngine.Debug.Log("[ScanIntelCommandFocusedValidation] result=Passed tests=2");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[ScanIntelCommandFocusedValidation] result=Failed");
            throw;
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _world = new World("ScanIntelCommandSystemTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void TryIssueScan_RevealsHostileTargetsAndWritesIntelFeed()
    {
        Entity gridEntity = CreateGrid(64, 64);
        Entity hostile = CreateUnit(FactionIdentity.EnemyFactionId, new int2(14, 14));
        Entity friendly = CreateUnit(FactionIdentity.PlayerFactionId, new int2(15, 15));
        Entity farHostile = CreateUnit(FactionIdentity.EnemyFactionId, new int2(40, 40));
        using EntityQuery gridQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        var scanSystem = new ScanIntelCommandSystem();

        ScanIntelCommandSystem.Result result = scanSystem.TryIssueScan(
            _entityManager,
            new Vector2(100f, 100f),
            requestId: 7,
            frame: 42,
            gridQuery,
            (Vector2 screenPosition, EntityManager entityManager, out int2 cell, out Vector3 worldPoint) =>
            {
                cell = new int2(12, 12);
                worldPoint = new Vector3(12.5f, 0f, 12.5f);
                return true;
            });

        Assert.IsTrue(result.CommandResult.Accepted);
        Assert.AreEqual(new int2(12, 12), result.CenterCell);
        Assert.AreEqual(1, result.RevealedCount);
        Assert.IsTrue(_entityManager.HasComponent<ScanIntelRevealedTag>(hostile));
        Assert.IsTrue(_entityManager.HasComponent<ScanIntelLastSeen>(hostile));
        Assert.IsFalse(_entityManager.HasComponent<ScanIntelRevealedTag>(friendly));
        Assert.IsFalse(_entityManager.HasComponent<ScanIntelRevealedTag>(farHostile));

        using EntityQuery feedQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<ScanIntelFeedQueueTag>(),
            ComponentType.ReadOnly<ScanIntelFeedEntry>());
        Entity feedEntity = feedQuery.GetSingletonEntity();
        DynamicBuffer<ScanIntelFeedEntry> feed = _entityManager.GetBuffer<ScanIntelFeedEntry>(feedEntity);
        Assert.AreEqual(1, feed.Length);
        Assert.AreEqual(7, feed[0].RequestId);
        Assert.AreEqual(42, feed[0].Frame);
        Assert.AreEqual(1, feed[0].RevealedCount);

        Assert.IsTrue(_entityManager.Exists(gridEntity));
    }

    [Test]
    public void TryIssueScan_RejectsWhenClickedCellCannotResolve()
    {
        CreateGrid(16, 16);
        using EntityQuery gridQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        var scanSystem = new ScanIntelCommandSystem();

        ScanIntelCommandSystem.Result result = scanSystem.TryIssueScan(
            _entityManager,
            new Vector2(100f, 100f),
            requestId: 1,
            frame: 1,
            gridQuery,
            (Vector2 screenPosition, EntityManager entityManager, out int2 cell, out Vector3 worldPoint) =>
            {
                cell = default;
                worldPoint = default;
                return false;
            });

        Assert.IsFalse(result.CommandResult.Accepted);
        Assert.AreEqual(TacticalCommandReasonCode.TargetOutOfBounds, result.CommandResult.ReasonCode);
    }

    private Entity CreateGrid(int width, int height)
    {
        Entity entity = _entityManager.CreateEntity(typeof(GridConfig));
        _entityManager.SetComponentData(entity, new GridConfig
        {
            Width = width,
            Height = height,
            CellSize = 1f,
            Origin = float3.zero
        });
        return entity;
    }

    private Entity CreateUnit(byte factionId, int2 cell)
    {
        Entity entity = _entityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(LocalTransform));
        _entityManager.SetComponentData(entity, new Faction { Id = factionId });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = cell });
        _entityManager.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        _entityManager.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        return entity;
    }
}
#endif

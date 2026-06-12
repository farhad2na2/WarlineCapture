#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class SelectionStateSystemTests
{
    private World _world;
    private EntityManager _entityManager;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.FocusedUnit_CanBeSetAndCleared());
            RunCase(test => test.CacheSelectedMoveEntity_KeepsOnlyPlayerMoveUnits());
            RunCase(test => test.VisibleUnitSelection_IgnoresPlayerBuildingsWithoutUnitMove());
            RunCase(test => test.FocusedUnitLifecycle_ClearCurrentSelection_RemovesSelectedTagsAndClearsCache());
            RunCase(test => test.FocusedUnitLifecycle_RefreshFocusedUnit_FocusesSingleSelectedPlayerUnit());
            UnityEngine.Debug.Log("[SelectionStateFocusedValidation] result=Passed tests=5");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[SelectionStateFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunCase(System.Action<SelectionStateSystemTests> testCase)
    {
        var tests = new SelectionStateSystemTests();
        try
        {
            tests.SetUp();
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _world = new World("SelectionStateSystemTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void FocusedUnit_CanBeSetAndCleared()
    {
        var selectionState = new SelectionStateSystem();
        Entity unit = _entityManager.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitMove));

        selectionState.SetFocusedUnit(unit);
        Assert.AreEqual(unit, selectionState.FocusedUnit);

        selectionState.ClearFocusedUnit();
        Assert.AreEqual(Entity.Null, selectionState.FocusedUnit);
    }

    [Test]
    public void CacheSelectedMoveEntity_KeepsOnlyPlayerMoveUnits()
    {
        var selectionState = new SelectionStateSystem();
        Entity playerUnit = CreateMoveUnit(FactionIdentitySystem.PlayerFactionId);
        Entity enemyUnit = CreateMoveUnit(FactionIdentitySystem.EnemyFactionId);
        Entity passengerUnit = CreateMoveUnit(FactionIdentitySystem.PlayerFactionId);
        _entityManager.AddComponentData(passengerUnit, new UnitTransportPassenger { Transport = Entity.Null });

        selectionState.CacheSelectedMoveEntity(_entityManager, playerUnit);
        selectionState.CacheSelectedMoveEntity(_entityManager, playerUnit);
        selectionState.CacheSelectedMoveEntity(_entityManager, enemyUnit);
        selectionState.CacheSelectedMoveEntity(_entityManager, passengerUnit);

        Assert.AreEqual(1, selectionState.CachedSelectedMoveEntities.Count);
        Assert.AreEqual(playerUnit, selectionState.CachedSelectedMoveEntities[0]);
    }

    [Test]
    public void VisibleUnitSelection_IgnoresPlayerBuildingsWithoutUnitMove()
    {
        GameObject cameraObject = new("VisibleUnitSelectionCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.pixelRect = new Rect(0f, 0f, 100f, 100f);
        camera.transform.position = new Vector3(0f, 0f, -10f);

        try
        {
            Entity movableUnit = CreateVisibleEntity(hasMove: true, new float3(-1f, 0f, 0f));
            Entity buildingLikeEntity = CreateVisibleEntity(hasMove: false, new float3(1f, 0f, 0f));
            var system = new VisibleUnitSelectionSystem();
            var selected = new System.Collections.Generic.List<Entity>();
            Rect screenRect = new(0f, 0f, 100f, 100f);
            var querySystem = new SelectionUiQuerySystem();

            Assert.IsTrue(system.HasVisiblePlayerUnits(
                _entityManager,
                camera,
                querySystem,
                screenRect,
                VisibleUnitSelectionSystem.Filter.All));
            Assert.IsFalse(system.HasVisiblePlayerUnits(
                _entityManager,
                camera,
                querySystem,
                screenRect,
                VisibleUnitSelectionSystem.Filter.Vehicles));

            int selectedCount = system.CollectVisiblePlayerUnits(
                _entityManager,
                camera,
                querySystem,
                screenRect,
                VisibleUnitSelectionSystem.Filter.All,
                selected);

            Assert.AreEqual(1, selectedCount);
            Assert.AreEqual(movableUnit, selected[0]);
            Assert.IsFalse(selected.Contains(buildingLikeEntity));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void FocusedUnitLifecycle_ClearCurrentSelection_RemovesSelectedTagsAndClearsCache()
    {
        var selectionState = new SelectionStateSystem();
        var lifecycle = new FocusedUnitLifecycleSystem();
        Entity unitA = CreateMoveUnit(FactionIdentitySystem.PlayerFactionId);
        Entity unitB = CreateMoveUnit(FactionIdentitySystem.PlayerFactionId);
        _entityManager.AddComponent<SelectedUnitTag>(unitA);
        _entityManager.AddComponent<SelectedUnitTag>(unitB);
        selectionState.CacheSelectedMoveEntity(_entityManager, unitA);
        selectionState.CacheSelectedMoveEntity(_entityManager, unitB);

        bool hudCleared = false;
        string diagnostic = null;
        lifecycle.ClearCurrentSelection(
            _entityManager,
            selectionState,
            "UnitTest",
            message => diagnostic = message,
            () => hudCleared = true);

        Assert.IsFalse(_entityManager.HasComponent<SelectedUnitTag>(unitA));
        Assert.IsFalse(_entityManager.HasComponent<SelectedUnitTag>(unitB));
        Assert.AreEqual(0, selectionState.CachedSelectedMoveEntities.Count);
        Assert.IsTrue(hudCleared);
        StringAssert.Contains("selected=2", diagnostic);
    }

    [Test]
    public void FocusedUnitLifecycle_RefreshFocusedUnit_FocusesSingleSelectedPlayerUnit()
    {
        var selectionState = new SelectionStateSystem();
        var lifecycle = new FocusedUnitLifecycleSystem();
        Entity unit = CreateMoveUnit(FactionIdentitySystem.PlayerFactionId);
        _entityManager.AddComponent<SelectedUnitTag>(unit);

        Entity applied = Entity.Null;
        bool result = lifecycle.RefreshFocusedUnit(
            _entityManager,
            selectionState,
            (_, entity) => applied = entity);

        Assert.IsTrue(result);
        Assert.AreEqual(unit, selectionState.FocusedUnit);
        Assert.AreEqual(unit, applied);
    }

    private Entity CreateMoveUnit(byte factionId)
    {
        Entity entity = _entityManager.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitMove));
        _entityManager.SetComponentData(entity, new Faction { Id = factionId });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = int2.zero });
        _entityManager.SetComponentData(entity, new UnitMove { Speed = 1f });
        return entity;
    }

    private Entity CreateVisibleEntity(bool hasMove, float3 position)
    {
        Entity entity = hasMove
            ? _entityManager.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitMove), typeof(LocalToWorld))
            : _entityManager.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(LocalToWorld));
        _entityManager.SetComponentData(entity, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = int2.zero });
        _entityManager.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
        if (hasMove)
            _entityManager.SetComponentData(entity, new UnitMove { Speed = 1f });
        return entity;
    }
}
#endif

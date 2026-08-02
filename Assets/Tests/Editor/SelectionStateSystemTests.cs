using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class SelectionStateCompositionSystemHelperTests
{
    private World _world;
    private EntityManager _entityManager;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.FocusedUnit_CanBeSetAndCleared());
            RunCase(test => test.SelectionVersion_ChangesOnlyWhenSelectionStateChanges());
            RunCase(test => test.CacheSelectedMoveEntity_KeepsOnlyPlayerMoveUnits());
            RunCase(test => test.VisibleUnitSelection_IgnoresPlayerBuildingsWithoutUnitMove());
            RunCase(test => test.VisibleUnitSelection_UsesSourcePrefixBeforeMovementFallback());
            RunCase(test => test.VisibleUnitSelection_RebindsAfterWorldReplacement());
            RunCase(test => test.VisibleUnitSelectionCandidateSystem_PublishesCandidateSnapshot());
            RunCase(test => test.FocusedUnitLifecycle_ClearCurrentSelection_RemovesSelectedTagsAndClearsCache());
            RunCase(test => test.FocusedUnitLifecycle_RefreshFocusedUnit_FocusesSingleSelectedPlayerUnit());
            RunCase(test => test.FocusedUnitLifecycle_RefreshFocusedUnit_RebindsAfterWorldReplacement());
            RunCase(test => test.FocusedUnitLifecycle_RejectsEnemyFocusWithoutClearingCurrentSelection());
            RunCase(test => test.FocusedUnitLifecycle_FocusesCanonicalOperationMapBuildingWithoutMovementAuthority());
            RunCase(test => test.FocusedUnitLifecycle_FocusAirUnitClearsAccidentalSelectionMoveThroughRequest());
            UnityEngine.Debug.Log("[SelectionStateFocusedValidation] result=Passed tests=13");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[SelectionStateFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunCase(System.Action<SelectionStateCompositionSystemHelperTests> testCase)
    {
        var tests = new SelectionStateCompositionSystemHelperTests();
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
        _world = new World("SelectionStateCompositionSystemHelperTests");
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
        var selectionState = new SelectionStateCompositionSystemHelper();
        Entity unit = _entityManager.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitMove));

        selectionState.SetFocusedUnit(unit);
        Assert.AreEqual(unit, selectionState.FocusedUnit);

        selectionState.ClearFocusedUnit();
        Assert.AreEqual(Entity.Null, selectionState.FocusedUnit);
    }

    [Test]
    public void SelectionVersion_ChangesOnlyWhenSelectionStateChanges()
    {
        var selectionState = new SelectionStateCompositionSystemHelper();
        Entity unit = CreateMoveUnit(FactionIdentity.PlayerFactionId);

        Assert.AreEqual(0, selectionState.SelectionVersion);

        selectionState.SetFocusedUnit(unit);
        Assert.AreEqual(1, selectionState.SelectionVersion);

        selectionState.SetFocusedUnit(unit);
        Assert.AreEqual(1, selectionState.SelectionVersion);

        selectionState.CacheSelectedMoveEntity(_entityManager, unit);
        Assert.AreEqual(2, selectionState.SelectionVersion);

        selectionState.CacheSelectedMoveEntity(_entityManager, unit);
        Assert.AreEqual(2, selectionState.SelectionVersion);

        selectionState.ClearSelectedMoveCache();
        Assert.AreEqual(3, selectionState.SelectionVersion);

        selectionState.ClearSelectedMoveCache();
        Assert.AreEqual(3, selectionState.SelectionVersion);

        selectionState.ClearFocusedUnit();
        Assert.AreEqual(4, selectionState.SelectionVersion);
    }

    [Test]
    public void CacheSelectedMoveEntity_KeepsOnlyPlayerMoveUnits()
    {
        var selectionState = new SelectionStateCompositionSystemHelper();
        Entity playerUnit = CreateMoveUnit(FactionIdentity.PlayerFactionId);
        Entity enemyUnit = CreateMoveUnit(FactionIdentity.EnemyFactionId);
        Entity passengerUnit = CreateMoveUnit(FactionIdentity.PlayerFactionId);
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
            var system = new VisibleUnitSelectionCameraSystemHelper();
            var selected = new System.Collections.Generic.List<Entity>();
            Rect screenRect = new(0f, 0f, 100f, 100f);
            var lookup = new SelectionUiReadModelLookup();

            Assert.IsTrue(system.HasVisiblePlayerUnits(
                _entityManager,
                camera,
                lookup,
                screenRect,
                VisibleUnitSelectionCameraSystemHelper.Filter.All));
            Assert.IsFalse(system.HasVisiblePlayerUnits(
                _entityManager,
                camera,
                lookup,
                screenRect,
                VisibleUnitSelectionCameraSystemHelper.Filter.Vehicles));

            int selectedCount = system.CollectVisiblePlayerUnits(
                _entityManager,
                camera,
                lookup,
                screenRect,
                VisibleUnitSelectionCameraSystemHelper.Filter.All,
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
    public void VisibleUnitSelection_UsesSourcePrefixBeforeMovementFallback()
    {
        GameObject cameraObject = new("VisibleUnitSelectionPrefixCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.pixelRect = new Rect(0f, 0f, 100f, 100f);
        camera.transform.position = new Vector3(0f, 0f, -10f);

        try
        {
            Entity sourceVehicle = CreateVisibleEntity(hasMove: true, new float3(-1f, 0f, 0f));
            _entityManager.AddComponentData(sourceVehicle, new UnitSourcePrefabKey
            {
                Value = new Unity.Collections.FixedString64Bytes("Unit_Veh_Test_APC")
            });

            Entity sourceCharacterWithVehicleFallback = CreateVisibleEntity(hasMove: true, new float3(1f, 0f, 0f));
            _entityManager.AddComponentData(sourceCharacterWithVehicleFallback, new UnitSourcePrefabKey
            {
                Value = new Unity.Collections.FixedString64Bytes("Unit_Chr_Test_Soldier")
            });
            _entityManager.AddComponentData(sourceCharacterWithVehicleFallback, new UnitFootprint { Size = new int2(2, 2) });
            _entityManager.AddComponentData(sourceCharacterWithVehicleFallback, new UnitMovementBehavior { UsesVehicleMotion = 1 });

            var system = new VisibleUnitSelectionCameraSystemHelper();
            var selected = new System.Collections.Generic.List<Entity>();
            Rect screenRect = new(0f, 0f, 100f, 100f);
            var lookup = new SelectionUiReadModelLookup();

            int vehicleCount = system.CollectVisiblePlayerUnits(
                _entityManager,
                camera,
                lookup,
                screenRect,
                VisibleUnitSelectionCameraSystemHelper.Filter.Vehicles,
                selected);

            Assert.AreEqual(1, vehicleCount);
            Assert.AreEqual(sourceVehicle, selected[0]);

            int soldierCount = system.CollectVisiblePlayerUnits(
                _entityManager,
                camera,
                lookup,
                screenRect,
                VisibleUnitSelectionCameraSystemHelper.Filter.Soldiers,
                selected);

            Assert.AreEqual(1, soldierCount);
            Assert.AreEqual(sourceCharacterWithVehicleFallback, selected[0]);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void VisibleUnitSelection_RebindsAfterWorldReplacement()
    {
        GameObject cameraObject = new("VisibleUnitSelectionReplacementCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.pixelRect = new Rect(0f, 0f, 100f, 100f);
        camera.transform.position = new Vector3(0f, 0f, -10f);
        var system = new VisibleUnitSelectionCameraSystemHelper();
        var selected = new System.Collections.Generic.List<Entity>();
        Rect screenRect = new(0f, 0f, 100f, 100f);
        var lookup = new SelectionUiReadModelLookup();

        try
        {
            Entity firstUnit = CreateVisibleEntity(hasMove: true, float3.zero);
            Assert.AreEqual(1, system.CollectVisiblePlayerUnits(
                _entityManager,
                camera,
                lookup,
                screenRect,
                VisibleUnitSelectionCameraSystemHelper.Filter.All,
                selected));
            Assert.AreEqual(firstUnit, selected[0]);

            _world.Dispose();
            _world = new World("SelectionStateCompositionSystemHelperTests-VisibleReplacement");
            _entityManager = _world.EntityManager;
            Entity replacementUnit = CreateVisibleEntity(hasMove: true, new float3(1f, 0f, 0f));

            Assert.AreEqual(1, system.CollectVisiblePlayerUnits(
                _entityManager,
                camera,
                lookup,
                screenRect,
                VisibleUnitSelectionCameraSystemHelper.Filter.All,
                selected));
            Assert.AreEqual(replacementUnit, selected[0]);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void VisibleUnitSelectionCandidateSystem_PublishesCandidateSnapshot()
    {
        Entity sourceVehicle = CreateVisibleEntity(hasMove: true, new float3(-1f, 0f, 0f));
        _entityManager.AddComponentData(sourceVehicle, new UnitSourcePrefabKey
        {
            Value = new Unity.Collections.FixedString64Bytes("Unit_Veh_Test_APC")
        });

        Entity sourceSoldier = CreateVisibleEntity(hasMove: true, new float3(1f, 0f, 0f));
        _entityManager.AddComponentData(sourceSoldier, new UnitSourcePrefabKey
        {
            Value = new Unity.Collections.FixedString64Bytes("Unit_Chr_Test_Soldier")
        });

        SystemHandle candidateSystem = _world.CreateSystem<VisibleUnitSelectionCandidateSystem>();
        candidateSystem.Update(_world.Unmanaged);

        EntityQuery snapshotQuery = _entityManager.CreateEntityQuery(
            typeof(VisibleUnitSelectionCandidateSnapshot),
            typeof(VisibleUnitSelectionCandidateElement));
        Assert.AreEqual(1, snapshotQuery.CalculateEntityCount());

        Entity snapshot = snapshotQuery.GetSingletonEntity();
        DynamicBuffer<VisibleUnitSelectionCandidateElement> candidates =
            _entityManager.GetBuffer<VisibleUnitSelectionCandidateElement>(snapshot);
        Assert.AreEqual(2, candidates.Length);

        bool foundVehicle = false;
        bool foundSoldier = false;
        for (int i = 0; i < candidates.Length; i++)
        {
            VisibleUnitSelectionCandidateElement candidate = candidates[i];
            if (candidate.Entity == sourceVehicle && candidate.IsVehicle == 1)
                foundVehicle = true;
            if (candidate.Entity == sourceSoldier && candidate.IsVehicle == 0)
                foundSoldier = true;
        }

        Assert.IsTrue(foundVehicle);
        Assert.IsTrue(foundSoldier);
    }

    [Test]
    public void FocusedUnitLifecycle_ClearCurrentSelection_RemovesSelectedTagsAndClearsCache()
    {
        var selectionState = new SelectionStateCompositionSystemHelper();
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        Entity unitA = CreateMoveUnit(FactionIdentity.PlayerFactionId);
        Entity unitB = CreateMoveUnit(FactionIdentity.PlayerFactionId);
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
        var selectionState = new SelectionStateCompositionSystemHelper();
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        Entity unit = CreateMoveUnit(FactionIdentity.PlayerFactionId);
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

    [Test]
    public void FocusedUnitLifecycle_RefreshFocusedUnit_RebindsAfterWorldReplacement()
    {
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        var firstSelectionState = new SelectionStateCompositionSystemHelper();
        Entity firstUnit = CreateMoveUnit(FactionIdentity.PlayerFactionId);
        _entityManager.AddComponent<SelectedUnitTag>(firstUnit);

        Assert.IsTrue(lifecycle.RefreshFocusedUnit(_entityManager, firstSelectionState, null));
        Assert.AreEqual(firstUnit, firstSelectionState.FocusedUnit);

        _world.Dispose();
        _world = new World("SelectionStateCompositionSystemHelperTests-Replacement");
        _entityManager = _world.EntityManager;

        var replacementSelectionState = new SelectionStateCompositionSystemHelper();
        Entity replacementUnit = CreateMoveUnit(FactionIdentity.PlayerFactionId);
        _entityManager.AddComponent<SelectedUnitTag>(replacementUnit);
        Entity applied = Entity.Null;

        Assert.IsTrue(lifecycle.RefreshFocusedUnit(
            _entityManager,
            replacementSelectionState,
            (_, entity) => applied = entity));
        Assert.AreEqual(replacementUnit, replacementSelectionState.FocusedUnit);
        Assert.AreEqual(replacementUnit, applied);
    }

    [Test]
    public void FocusedUnitLifecycle_RejectsEnemyFocusWithoutClearingCurrentSelection()
    {
        var selectionState = new SelectionStateCompositionSystemHelper();
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        Entity playerUnit = CreateMoveUnit(FactionIdentity.PlayerFactionId);
        Entity enemyUnit = CreateMoveUnit(FactionIdentity.EnemyFactionId);
        _entityManager.AddComponent<SelectedUnitTag>(playerUnit);
        selectionState.SetFocusedUnit(playerUnit);
        selectionState.CacheSelectedMoveEntity(_entityManager, playerUnit);

        bool hudCleared = false;
        Entity applied = Entity.Null;
        bool focused = lifecycle.FocusUnitEntity(
            _entityManager,
            enemyUnit,
            selectionState,
            "EnemyClick",
            "EnemyClick",
            _ => { },
            null,
            () => hudCleared = true,
            (_, entity) => applied = entity);

        Assert.IsFalse(focused);
        Assert.IsTrue(_entityManager.HasComponent<SelectedUnitTag>(playerUnit));
        Assert.IsFalse(_entityManager.HasComponent<SelectedUnitTag>(enemyUnit));
        Assert.AreEqual(playerUnit, selectionState.FocusedUnit);
        Assert.AreEqual(1, selectionState.CachedSelectedMoveEntities.Count);
        Assert.AreEqual(playerUnit, selectionState.CachedSelectedMoveEntities[0]);
        Assert.IsFalse(hudCleared);
        Assert.AreEqual(Entity.Null, applied);
    }

    [Test]
    public void FocusedUnitLifecycle_FocusesCanonicalOperationMapBuildingWithoutMovementAuthority()
    {
        var selectionState = new SelectionStateCompositionSystemHelper();
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        Entity building = _entityManager.CreateEntity(
            typeof(OperationMapBuildingComponent),
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(StaticGridBlocker));
        _entityManager.SetComponentData(building, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(building, new UnitGrid { Cell = new int2(4, 6) });
        _entityManager.SetComponentData(building, new UnitFootprint { Size = new int2(3, 2) });
        _entityManager.SetComponentData(building, new UnitHealth { Current = 100, Max = 100 });

        Entity applied = Entity.Null;
        bool focused = lifecycle.FocusUnitEntity(
            _entityManager,
            building,
            selectionState,
            "CanonicalBuildingClick",
            "CanonicalBuildingClick",
            _ => { },
            null,
            () => { },
            (_, entity) => applied = entity);

        Assert.IsTrue(focused);
        Assert.AreEqual(building, selectionState.FocusedUnit);
        Assert.AreEqual(building, applied);
        Assert.IsTrue(_entityManager.HasComponent<SelectedUnitTag>(building));
        Assert.IsFalse(_entityManager.HasComponent<UnitMove>(building));
        Assert.AreEqual(0, selectionState.CachedSelectedMoveEntities.Count);
    }

    [Test]
    public void FocusedUnitLifecycle_FocusAirUnitClearsAccidentalSelectionMoveThroughRequest()
    {
        var selectionState = new SelectionStateCompositionSystemHelper();
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        Entity unit = CreateMoveUnit(FactionIdentity.PlayerFactionId);
        _entityManager.AddComponent<UnitAirMovement>(unit);
        _entityManager.AddComponent<ManualMoveOrderTag>(unit);
        _entityManager.AddComponentData(unit, new UnitTarget { Cell = new int2(1, 0) });
        _entityManager.AddComponentData(unit, new UnitPathRequest { Goal = new int2(1, 0) });
        _entityManager.AddComponentData(unit, new UnitPathFollow { PathIndex = 1 });
        _entityManager.AddComponentData(unit, new UnitPathRange { Start = 0, Length = 2 });

        Entity applied = Entity.Null;
        bool focused = lifecycle.FocusUnitEntity(
            _entityManager,
            unit,
            selectionState,
            "UnitTest",
            "UnitTest",
            _ => { },
            null,
            () => { },
            (_, entity) => applied = entity);

        Assert.IsTrue(focused);
        Assert.AreEqual(unit, selectionState.FocusedUnit);
        Assert.AreEqual(unit, applied);
        Assert.IsFalse(_entityManager.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRange>(unit));
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
        _entityManager.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = int2.zero });
        _entityManager.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
        if (hasMove)
            _entityManager.SetComponentData(entity, new UnitMove { Speed = 1f });
        return entity;
    }
}
#endif

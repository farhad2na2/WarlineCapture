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
        Entity playerUnit = CreateMoveUnit(0);
        Entity enemyUnit = CreateMoveUnit(1);
        Entity passengerUnit = CreateMoveUnit(0);
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

            int selectedCount = system.CollectVisiblePlayerUnits(
                _entityManager,
                camera,
                new SelectionUiQuerySystem(),
                new Rect(0f, 0f, 100f, 100f),
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

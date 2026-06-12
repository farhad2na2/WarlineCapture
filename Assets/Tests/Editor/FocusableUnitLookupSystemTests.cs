#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class FocusableUnitLookupSystemTests
{
    private World _world;
    private EntityManager _entityManager;

    [SetUp]
    public void SetUp()
    {
        _world = new World("FocusableUnitLookupSystemTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void LookupRefreshesWhenGridOrFootprintChangesWithoutCountChange()
    {
        GameObject cameraObject = new("FocusableUnitLookupCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10f;
        camera.pixelRect = new Rect(0f, 0f, 100f, 100f);
        camera.transform.position = new Vector3(0f, 0f, -10f);

        try
        {
            CreateGrid(32, 32);
            Entity unit = CreateFocusableUnit(new int2(4, 4), new int2(1, 1));
            var lookup = new FocusableUnitLookupSystem();

            Assert.IsTrue(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(4, 4),
                new Vector2(50f, 50f),
                out Entity focusedBeforeMove));
            Assert.AreEqual(unit, focusedBeforeMove);

            _entityManager.SetComponentData(unit, new UnitGrid { Cell = new int2(8, 4) });
            _entityManager.SetComponentData(unit, new LocalToWorld { Value = float4x4.Translate(new float3(8f, 0f, 4f)) });

            Assert.IsFalse(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(4, 4),
                new Vector2(50f, 50f),
                out _));
            Assert.IsTrue(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(8, 4),
                new Vector2(50f, 50f),
                out Entity focusedAfterMove));
            Assert.AreEqual(unit, focusedAfterMove);

            Assert.IsFalse(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(11, 4),
                new Vector2(50f, 50f),
                out _));

            _entityManager.SetComponentData(unit, new UnitFootprint { Size = new int2(4, 1) });

            Assert.IsTrue(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(11, 4),
                new Vector2(50f, 50f),
                out Entity focusedAfterFootprintChange));
            Assert.AreEqual(unit, focusedAfterFootprintChange);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    private void CreateGrid(int width, int height)
    {
        Entity grid = _entityManager.CreateEntity(typeof(GridConfig));
        _entityManager.SetComponentData(grid, new GridConfig
        {
            Width = width,
            Height = height,
            CellSize = 1f,
            Origin = float3.zero
        });
    }

    private Entity CreateFocusableUnit(int2 cell, int2 footprint)
    {
        Entity entity = _entityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitFootprint),
            typeof(LocalToWorld));
        _entityManager.SetComponentData(entity, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = cell });
        _entityManager.SetComponentData(entity, new UnitMove { Speed = 1f });
        _entityManager.SetComponentData(entity, new UnitFootprint { Size = footprint });
        _entityManager.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(new float3(cell.x, 0f, cell.y)) });
        return entity;
    }
}
#endif

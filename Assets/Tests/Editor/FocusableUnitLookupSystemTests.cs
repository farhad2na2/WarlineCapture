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

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.LookupRefreshesWhenGridOrFootprintChangesWithoutCountChange());
            RunCase(test => test.ScreenDistanceFallback_SkipsActiveTransitAirUnits());
            RunCase(test => test.ScreenDistanceFallback_UsesVisualHitboxForLargeAircraft());
            UnityEngine.Debug.Log("[FocusableUnitLookupFocusedValidation] result=Passed tests=3");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[FocusableUnitLookupFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunCase(System.Action<FocusableUnitLookupSystemTests> testCase)
    {
        var tests = new FocusableUnitLookupSystemTests();
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

    [Test]
    public void ScreenDistanceFallback_SkipsActiveTransitAirUnits()
    {
        GameObject cameraObject = new("FocusableUnitScreenDistanceCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10f;
        camera.pixelRect = new Rect(0f, 0f, 100f, 100f);
        camera.transform.position = new Vector3(0f, 0f, -10f);

        try
        {
            CreateGrid(32, 32);
            Entity activeTransit = CreateFocusableUnit(new int2(4, 4), new int2(1, 1));
            _entityManager.AddComponent<UnitSpawnTransitTag>(activeTransit);
            _entityManager.AddComponentData(activeTransit, new UnitAirComponent { Airborne = 1 });

            Entity groundedIdleTransit = CreateFocusableUnit(new int2(5, 4), new int2(1, 1));
            _entityManager.AddComponent<UnitSpawnTransitTag>(groundedIdleTransit);
            _entityManager.AddComponentData(groundedIdleTransit, new UnitAirComponent());

            var lookup = new FocusableUnitLookupSystem();
            Assert.IsTrue(lookup.TryGetClickedUnitEntityByScreenDistance(
                _entityManager,
                camera,
                new Vector2(50f, 50f),
                1000f,
                out Entity focused));
            Assert.AreEqual(groundedIdleTransit, focused);

            _entityManager.SetComponentData(activeTransit, new UnitAirComponent());

            Assert.IsTrue(lookup.TryGetClickedUnitEntityByScreenDistance(
                _entityManager,
                camera,
                new Vector2(50f, 50f),
                1000f,
                out focused));
            Assert.AreEqual(activeTransit, focused);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void ScreenDistanceFallback_UsesVisualHitboxForLargeAircraft()
    {
        GameObject cameraObject = new("FocusableUnitLargeAircraftCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10f;
        camera.pixelRect = new Rect(0f, 0f, 100f, 100f);
        camera.transform.position = new Vector3(0f, 0f, -10f);

        try
        {
            CreateGrid(32, 32);
            Entity aircraft = CreateFocusableUnit(new int2(0, 0), new int2(1, 1));
            _entityManager.AddComponentData(aircraft, new UnitSelectionHitbox
            {
                Center = float3.zero,
                Extents = new float3(7f, 1f, 1f)
            });

            var lookup = new FocusableUnitLookupSystem();
            Assert.IsTrue(lookup.TryGetClickedUnitEntityByScreenDistance(
                _entityManager,
                camera,
                new Vector2(80f, 50f),
                10f,
                out Entity focused));
            Assert.AreEqual(aircraft, focused);
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
        _entityManager.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = cell });
        _entityManager.SetComponentData(entity, new UnitMove { Speed = 1f });
        _entityManager.SetComponentData(entity, new UnitFootprint { Size = footprint });
        _entityManager.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(new float3(cell.x, 0f, cell.y)) });
        return entity;
    }
}
#endif

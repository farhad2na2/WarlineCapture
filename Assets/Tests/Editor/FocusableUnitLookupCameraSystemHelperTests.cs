using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class FocusableUnitLookupCameraSystemHelperTests
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
            RunCase(test => test.GridLookup_UsesAirSelectionHitboxPaddingForLargeMapAircraft());
            RunCase(test => test.GridLookup_RebindsAfterWorldReplacement());
            RunCase(test => test.GridLookup_SelectsCanonicalOperationMapBuildingWithoutUnitMove());
            RunCase(test => test.GridLookup_RejectsDestroyedCanonicalOperationMapBuilding());
            UnityEngine.Debug.Log("[FocusableUnitLookupFocusedValidation] result=Passed tests=7");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[FocusableUnitLookupFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunCase(System.Action<FocusableUnitLookupCameraSystemHelperTests> testCase)
    {
        var tests = new FocusableUnitLookupCameraSystemHelperTests();
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
        _world = new World("FocusableUnitLookupCameraSystemHelperTests");
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
            var lookup = new FocusableUnitLookupCameraSystemHelper();

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

            var lookup = new FocusableUnitLookupCameraSystemHelper();
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

            var lookup = new FocusableUnitLookupCameraSystemHelper();
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

    [Test]
    public void GridLookup_UsesAirSelectionHitboxPaddingForLargeMapAircraft()
    {
        GameObject cameraObject = new("FocusableUnitMapAircraftCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 16f;
        camera.pixelRect = new Rect(0f, 0f, 100f, 100f);
        camera.transform.position = new Vector3(0f, 0f, -10f);

        try
        {
            CreateGrid(64, 64);
            Entity aircraft = CreateFocusableUnit(new int2(0, 0), new int2(1, 1));
            _entityManager.AddComponentData(aircraft, new UnitAirMovement());
            _entityManager.AddComponentData(aircraft, new UnitSelectionHitbox
            {
                Center = float3.zero,
                Extents = new float3(12f, 1f, 1f)
            });

            Vector3 screen = camera.WorldToScreenPoint(new Vector3(12f, 0f, 0f));
            var lookup = new FocusableUnitLookupCameraSystemHelper();
            Assert.IsTrue(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(12, 0),
                new Vector2(screen.x, screen.y),
                out Entity focused));
            Assert.AreEqual(aircraft, focused);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void GridLookup_RebindsAfterWorldReplacement()
    {
        GameObject cameraObject = new("FocusableUnitReplacementWorldCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 16f;
        camera.pixelRect = new Rect(0f, 0f, 100f, 100f);
        camera.transform.position = new Vector3(0f, 0f, -10f);
        var lookup = new FocusableUnitLookupCameraSystemHelper();

        try
        {
            CreateGrid(32, 32);
            CreateFocusableUnit(new int2(4, 4), new int2(1, 1));
            Assert.IsTrue(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(4, 4),
                new Vector2(50f, 50f),
                out _));

            _world.Dispose();
            _world = new World("FocusableUnitLookupCameraSystemHelperTests-Replacement");
            _entityManager = _world.EntityManager;
            CreateGrid(32, 32);
            Entity replacement = CreateFocusableUnit(new int2(12, 8), new int2(1, 1));

            Assert.IsFalse(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(4, 4),
                new Vector2(50f, 50f),
                out _));
            Assert.IsTrue(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(12, 8),
                new Vector2(50f, 50f),
                out Entity focused));
            Assert.AreEqual(replacement, focused);

            _entityManager.SetComponentData(replacement, new UnitGrid { Cell = new int2(14, 8) });
            _entityManager.SetComponentData(
                replacement,
                new LocalToWorld { Value = float4x4.Translate(new float3(14f, 0f, 8f)) });
            Assert.IsFalse(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(12, 8),
                new Vector2(50f, 50f),
                out _));
            Assert.IsTrue(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(14, 8),
                new Vector2(50f, 50f),
                out focused));
            Assert.AreEqual(replacement, focused);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void GridLookup_SelectsCanonicalOperationMapBuildingWithoutUnitMove()
    {
        GameObject cameraObject = new("FocusableCanonicalBuildingCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 16f;
        camera.pixelRect = new Rect(0f, 0f, 100f, 100f);
        camera.transform.position = new Vector3(0f, 0f, -10f);

        try
        {
            CreateGrid(64, 64);
            Entity building = CreateCanonicalBuilding(new int2(12, 8), new int2(4, 3));
            var lookup = new FocusableUnitLookupCameraSystemHelper();

            Assert.IsTrue(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(12, 8),
                new Vector2(50f, 50f),
                out Entity focused));
            Assert.AreEqual(building, focused);
            Assert.IsFalse(_entityManager.HasComponent<UnitMove>(building));
            Assert.IsTrue(_entityManager.HasComponent<StaticGridBlocker>(building));
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void GridLookup_RejectsDestroyedCanonicalOperationMapBuilding()
    {
        GameObject cameraObject = new("FocusableDestroyedBuildingCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 16f;
        camera.pixelRect = new Rect(0f, 0f, 100f, 100f);
        camera.transform.position = new Vector3(0f, 0f, -10f);

        try
        {
            CreateGrid(64, 64);
            Entity building = CreateCanonicalBuilding(new int2(12, 8), new int2(4, 3));
            _entityManager.SetComponentEnabled<OperationMapBuildingDestroyedComponent>(building, true);
            var lookup = new FocusableUnitLookupCameraSystemHelper();

            Assert.IsFalse(lookup.TryGetClickedUnitEntity(
                _entityManager,
                camera,
                new int2(12, 8),
                new Vector2(50f, 50f),
                out _));
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

    private Entity CreateCanonicalBuilding(int2 cell, int2 footprint)
    {
        Entity entity = _entityManager.CreateEntity(
            typeof(OperationMapBuildingComponent),
            typeof(OperationMapBuildingDestroyedComponent),
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(LocalToWorld),
            typeof(StaticGridBlocker));
        _entityManager.SetComponentEnabled<OperationMapBuildingDestroyedComponent>(entity, false);
        _entityManager.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = cell });
        _entityManager.SetComponentData(entity, new UnitFootprint { Size = footprint });
        _entityManager.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        _entityManager.SetComponentData(entity, new LocalToWorld
        {
            Value = float4x4.Translate(new float3(cell.x, 0f, cell.y))
        });
        return entity;
    }
}
#endif

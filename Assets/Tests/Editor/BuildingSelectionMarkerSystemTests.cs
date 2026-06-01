#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class BuildingSelectionMarkerSystemTests
{
    private GameObject _markerPrefab;
    private GameObject _root;
    private readonly System.Collections.Generic.List<GameObject> _objects = new();

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("BuildingSelectionMarkerTestsRoot");
        _markerPrefab = CreateMarkerPrefab();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _objects.Count; i++)
        {
            if (_objects[i] != null)
                Object.DestroyImmediate(_objects[i]);
        }

        _objects.Clear();
        if (_markerPrefab != null)
            Object.DestroyImmediate(_markerPrefab);
        if (_root != null)
            Object.DestroyImmediate(_root);
    }

    [Test]
    public void RefreshMovesSingleMarkerBetweenSelectedBuildings()
    {
        var runtimeBuildings = new RuntimeBuildingSystem<RuntimeBuildingData>();
        RuntimeBuildingData buildingA = CreateBuilding(1, new Vector2Int(2, 4), new Vector2Int(6, 4), 0f);
        RuntimeBuildingData buildingB = CreateBuilding(2, new Vector2Int(20, 8), new Vector2Int(10, 8), 0.5f);
        runtimeBuildings.AddBuilding(buildingA.Id, buildingA);
        runtimeBuildings.AddBuilding(buildingB.Id, buildingB);

        var system = new BuildingSelectionMarkerSystem();
        BuildingSelectionMarkerSystem.Context context = CreateContext(runtimeBuildings);

        runtimeBuildings.SelectBuilding(buildingA.Id);
        system.Refresh(context);
        GameObject marker = system.RuntimeMarkerForTests;

        Assert.IsNotNull(marker);
        Assert.IsTrue(marker.activeSelf);
        Assert.That(marker.transform.position.x, Is.EqualTo(5f).Within(0.001f));
        Assert.That(marker.transform.position.z, Is.EqualTo(6f).Within(0.001f));

        runtimeBuildings.SelectBuilding(buildingB.Id);
        system.Refresh(context);

        Assert.AreSame(marker, system.RuntimeMarkerForTests);
        Assert.IsTrue(marker.activeSelf);
        Assert.That(marker.transform.position.x, Is.EqualTo(25f).Within(0.001f));
        Assert.That(marker.transform.position.z, Is.EqualTo(12f).Within(0.001f));
        Assert.That(marker.transform.position.y, Is.EqualTo(0.535f).Within(0.001f));
    }

    [Test]
    public void RefreshHidesMarkerWhenSelectionClearsOrSelectedBuildingIsDestroyed()
    {
        var runtimeBuildings = new RuntimeBuildingSystem<RuntimeBuildingData>();
        RuntimeBuildingData building = CreateBuilding(1, Vector2Int.zero, new Vector2Int(4, 4), 0f);
        runtimeBuildings.AddBuilding(building.Id, building);

        var system = new BuildingSelectionMarkerSystem();
        BuildingSelectionMarkerSystem.Context context = CreateContext(runtimeBuildings);

        runtimeBuildings.SelectBuilding(building.Id);
        system.Refresh(context);
        Assert.IsTrue(system.RuntimeMarkerForTests.activeSelf);

        runtimeBuildings.ClearSelection();
        system.Refresh(context);
        Assert.IsFalse(system.RuntimeMarkerForTests.activeSelf);

        runtimeBuildings.SelectBuilding(building.Id);
        building.IsDestroyed = true;
        system.Refresh(context);
        Assert.IsFalse(system.RuntimeMarkerForTests.activeSelf);
    }

    [Test]
    public void RuntimeVisualInitializationCachesBuildingRenderersWithoutMarkerChildren()
    {
        GameObject buildingObject = new("RuntimeBuildingVisual");
        _objects.Add(buildingObject);
        GameObject modelRoot = new("ModelRoot");
        modelRoot.transform.SetParent(buildingObject.transform, false);
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(model.GetComponent<Collider>());
        model.name = "Model";
        model.transform.SetParent(modelRoot.transform, false);

        var building = new RuntimeBuildingData
        {
            Instance = buildingObject,
            Definition = new BuildingDefinition { FootprintCells = new Vector2Int(4, 4) }
        };
        var visualSystem = new BuildingRuntimeVisualSystem();
        var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingData> { { 1, building } };
        var context = new BuildingRuntimeVisualSystem.Context(
            runtimeBuildings,
            new BuildingVisualSystem(),
            new BuildingFactionVisualSystem(),
            new BuildingBarrierSystem(),
            null,
            new MaterialPropertyBlock(),
            0.2f);

        visualSystem.InitializeBuildingVisuals(context, building);

        Assert.IsNotNull(building.FactionVisualRenderers);
        Assert.AreEqual(1, building.FactionVisualRenderers.Length);
        Assert.AreSame(model.GetComponent<Renderer>(), building.FactionVisualRenderers[0]);
    }

    private BuildingSelectionMarkerSystem.Context CreateContext(RuntimeBuildingSystem<RuntimeBuildingData> runtimeBuildings)
    {
        GridConfig grid = new()
        {
            CellSize = 1f,
            Width = 64,
            Height = 64
        };

        return new BuildingSelectionMarkerSystem.Context(
            runtimeBuildings,
            runtimeBuildings.Buildings,
            (out GridConfig resolvedGrid) =>
            {
                resolvedGrid = grid;
                return true;
            },
            (origin, footprint, resolvedGrid) => new Vector3(
                (origin.x + footprint.x * 0.5f) * resolvedGrid.CellSize,
                0f,
                (origin.y + footprint.y * 0.5f) * resolvedGrid.CellSize),
            _markerPrefab,
            _root.transform,
            new BuildingVisualSystem(),
            null,
            new MaterialPropertyBlock(),
            Object.DestroyImmediate);
    }

    private RuntimeBuildingData CreateBuilding(int id, Vector2Int origin, Vector2Int footprint, float y)
    {
        GameObject instance = new($"Building_{id}");
        _objects.Add(instance);
        instance.transform.position = new Vector3(origin.x, y, origin.y);
        return new RuntimeBuildingData
        {
            Id = id,
            OriginCell = origin,
            Instance = instance,
            Definition = new BuildingDefinition { FootprintCells = footprint }
        };
    }

    private static GameObject CreateMarkerPrefab()
    {
        GameObject marker = new("MarkerPrefab");
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(model.GetComponent<Collider>());
        model.name = "Model";
        model.transform.SetParent(marker.transform, false);
        model.transform.localScale = Vector3.one;
        return marker;
    }
}
#endif

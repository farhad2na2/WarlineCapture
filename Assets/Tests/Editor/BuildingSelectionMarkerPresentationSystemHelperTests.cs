using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class BuildingSelectionMarkerPresentationSystemHelperTests
{
    private const string SelectionMarkerMaterialPath = "Assets/Game/Rendering/Materials/Selection/Mat_Selection_Player_Hologram.mat";

    private GameObject _markerPrefab;
    private GameObject _root;
    private World _world;
    private readonly System.Collections.Generic.List<GameObject> _objects = new();

    [MenuItem("Tools/Validation/Building Selection Marker Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.RefreshMovesSingleMarkerBetweenSelectedBuildings());
            RunCase(test => test.RefreshHidesMarkerWhenSelectionClearsOrSelectedBuildingIsDestroyed());
            RunCase(test => test.RefreshAppliesHologramCompatibleMarkerColorProperties());
            RunCase(test => test.RefreshKeepsMapAuthoredMarkerRenderableBoundsAboveSurface());
            RunCase(test => test.RefreshUsesCanonicalFootprintInsteadOfBroadMapAuthoredRendererBounds());
            RunCase(test => test.RuntimeVisualInitializationCachesBuildingRenderersWithoutMarkerChildren());
            RunCase(test => test.RuntimeResourceVisualsPreferEcsStorageForProductionState());
            RunCase(test => test.RefreshCreatesMeshBoundObjectOutlineForSelectedBuilding());
            Debug.Log("[BuildingSelectionMarkerFocusedValidation] result=Passed tests=8");
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingSelectionMarkerFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunCase(System.Action<BuildingSelectionMarkerPresentationSystemHelperTests> testCase)
    {
        var test = new BuildingSelectionMarkerPresentationSystemHelperTests();
        test.SetUp();
        try
        {
            testCase(test);
        }
        finally
        {
            test.TearDown();
        }
    }

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
        if (_world != null && _world.IsCreated)
            _world.Dispose();
        _world = null;
    }

    [Test]
    public void RefreshMovesSingleMarkerBetweenSelectedBuildings()
    {
        var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
        RuntimeBuildingEntity buildingA = CreateBuilding(1, new Vector2Int(2, 4), new Vector2Int(6, 4), 0f);
        RuntimeBuildingEntity buildingB = CreateBuilding(2, new Vector2Int(20, 8), new Vector2Int(10, 8), 0.5f);
        runtimeBuildings.AddBuilding(buildingA.Id, buildingA);
        runtimeBuildings.AddBuilding(buildingB.Id, buildingB);

        BuildingSelectionMarkerPresentationSystemHelper system = CreateBuildingSelectionMarkerPresentationSystemHelper();
        BuildingSelectionMarkerPresentationSystemHelper.Context context = CreateContext(runtimeBuildings);

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
        AssertMarkerRenderableMinY(marker, 0.53f);
    }

    [Test]
    public void RefreshHidesMarkerWhenSelectionClearsOrSelectedBuildingIsDestroyed()
    {
        var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
        RuntimeBuildingEntity building = CreateBuilding(1, Vector2Int.zero, new Vector2Int(4, 4), 0f);
        runtimeBuildings.AddBuilding(building.Id, building);

        BuildingSelectionMarkerPresentationSystemHelper system = CreateBuildingSelectionMarkerPresentationSystemHelper();
        BuildingSelectionMarkerPresentationSystemHelper.Context context = CreateContext(runtimeBuildings);

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
    public void RefreshAppliesHologramCompatibleMarkerColorProperties()
    {
        var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
        RuntimeBuildingEntity building = CreateBuilding(1, new Vector2Int(2, 4), new Vector2Int(6, 4), 0f);
        runtimeBuildings.AddBuilding(building.Id, building);

        BuildingSelectionMarkerPresentationSystemHelper system = CreateBuildingSelectionMarkerPresentationSystemHelper();
        BuildingSelectionMarkerPresentationSystemHelper.Context context = CreateContext(runtimeBuildings);

        runtimeBuildings.SelectBuilding(building.Id);
        system.Refresh(context);

        GameObject marker = system.RuntimeMarkerForTests;
        Assert.IsNotNull(marker);
        Renderer renderer = marker.GetComponentInChildren<Renderer>();
        Assert.IsNotNull(renderer);

        var propertyBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propertyBlock);
        AssertColorClose(new Color(0.05f, 0.88f, 1f, 0.94f), propertyBlock.GetColor("_BaseColor"));
        AssertColorClose(new Color(0.05f, 0.88f, 1f, 0.94f), propertyBlock.GetColor("_Color"));
        AssertColorClose(new Color(0.05f, 0.88f, 1f, 0.94f), propertyBlock.GetColor("_EmissionColor"));
        AssertColorClose(new Color(0.05f, 0.88f, 1f, 0.94f), propertyBlock.GetColor("_AccentColor"));
    }

    [Test]
    public void RefreshKeepsMapAuthoredMarkerRenderableBoundsAboveSurface()
    {
        var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
        RuntimeBuildingEntity building = CreateBuilding(1, new Vector2Int(4, 5), new Vector2Int(4, 4), 0.25f);
        building.Instance.AddComponent<MapAuthoredBuildingVisualComponent>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.name = "Model";
        visual.transform.SetParent(building.Instance.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.8f, 0f);
        visual.transform.localScale = new Vector3(2f, 1f, 2f);
        _objects.Add(visual);

        runtimeBuildings.AddBuilding(building.Id, building);

        BuildingSelectionMarkerPresentationSystemHelper system = CreateBuildingSelectionMarkerPresentationSystemHelper();
        BuildingSelectionMarkerPresentationSystemHelper.Context context = CreateContext(runtimeBuildings);

        runtimeBuildings.SelectBuilding(building.Id);
        system.Refresh(context);

        GameObject marker = system.RuntimeMarkerForTests;
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker.activeSelf);
        AssertMarkerRenderableMinY(marker, 0.3f);
    }

    [Test]
    public void RefreshUsesCanonicalFootprintInsteadOfBroadMapAuthoredRendererBounds()
    {
        var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
        RuntimeBuildingEntity building = CreateBuilding(1, new Vector2Int(4, 5), new Vector2Int(4, 3), 0.25f);
        building.Instance.AddComponent<MapAuthoredBuildingVisualComponent>();

        GameObject broadVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(broadVisual.GetComponent<Collider>());
        broadVisual.name = "BroadMapAuthoredVisualHierarchy";
        broadVisual.transform.SetParent(building.Instance.transform, false);
        broadVisual.transform.localScale = new Vector3(30f, 2f, 18f);
        _objects.Add(broadVisual);

        runtimeBuildings.AddBuilding(building.Id, building);
        BuildingSelectionMarkerPresentationSystemHelper system = CreateBuildingSelectionMarkerPresentationSystemHelper();
        BuildingSelectionMarkerPresentationSystemHelper.Context context = CreateContext(runtimeBuildings);

        runtimeBuildings.SelectBuilding(building.Id);
        system.Refresh(context);

        GameObject marker = system.RuntimeMarkerForTests;
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker.activeSelf);
        Assert.That(marker.transform.position.x, Is.EqualTo(6f).Within(0.001f));
        Assert.That(marker.transform.position.z, Is.EqualTo(6.5f).Within(0.001f));
        Assert.That(marker.transform.localScale.x, Is.EqualTo(4f).Within(0.001f));
        Assert.That(marker.transform.localScale.z, Is.EqualTo(3f).Within(0.001f));
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

        var building = new RuntimeBuildingEntity
        {
            Instance = buildingObject,
            Definition = new BuildingDefinition { FootprintCells = new Vector2Int(4, 4) }
        };
        BuildingRuntimeVisualPresentationSystemHelper visualSystem = CreateBuildingRuntimeVisualPresentationSystemHelper();
        var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity> { { 1, building } };
        var context = new BuildingRuntimeVisualPresentationSystemHelper.Context(
            runtimeBuildings,
            CreateBuildingVisualSystem(),
            CreateBuildingFactionVisualSystem(),
            new BuildingBarrierUtilitySystemHelper(),
            null,
            new MaterialPropertyBlock(),
            0.2f);

        visualSystem.InitializeBuildingVisuals(context, building);

        Assert.IsNotNull(building.FactionVisualRenderers);
        Assert.AreEqual(1, building.FactionVisualRenderers.Length);
        Assert.AreSame(model.GetComponent<Renderer>(), building.FactionVisualRenderers[0]);
    }

    [Test]
    public void RuntimeResourceVisualsPreferEcsStorageForProductionState()
    {
        GameObject buildingObject = new("RuntimeResourceVisual");
        _objects.Add(buildingObject);
        GameObject modelRoot = new("ModelRoot");
        modelRoot.transform.SetParent(buildingObject.transform, false);
        GameObject animated = new("Pump_Y_30");
        animated.transform.SetParent(modelRoot.transform, false);

        BuildingVisualSystem buildingVisualSystem = CreateBuildingVisualSystem();
        EntityManager entityManager = _world.EntityManager;
        Entity storageEntity = entityManager.CreateEntity(typeof(BuildingResourceStorageComponent));
        entityManager.SetComponentData(storageEntity, new BuildingResourceStorageComponent
        {
            OilStorageCapacity = 10,
            OilBarrelsPerDay = 2f,
            StoredOilBarrels = 5f
        });

        var building = new RuntimeBuildingEntity
        {
            Id = 2,
            Instance = buildingObject,
            Definition = new BuildingDefinition
            {
                FootprintCells = new Vector2Int(2, 2),
                OilStorageCapacity = 10,
                OilBarrelsPerDay = 2f
            },
            CombatEntity = storageEntity,
            StoredOilBarrels = 10f
        };
        building.AnimatedParts = buildingVisualSystem.FindAnimatedBuildingParts(modelRoot.transform);
        var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity> { { building.Id, building } };
        var context = new BuildingRuntimeVisualPresentationSystemHelper.Context(
            runtimeBuildings,
            buildingVisualSystem,
            CreateBuildingFactionVisualSystem(),
            new BuildingBarrierUtilitySystemHelper(),
            null,
            new MaterialPropertyBlock(),
            0.2f,
            TryGetEntityManager);

        BuildingRuntimeVisualPresentationSystemHelper visualSystem = CreateBuildingRuntimeVisualPresentationSystemHelper();
        visualSystem.UpdateBuildingResourceVisuals(context, 1f);

        Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(0f, animated.transform.localEulerAngles.y)), 0.01f);

        bool TryGetEntityManager(out EntityManager em)
        {
            em = entityManager;
            return true;
        }
    }

    [Test]
    public void RefreshCreatesMeshBoundObjectOutlineForSelectedBuilding()
    {
        var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
        RuntimeBuildingEntity building = CreateBuilding(1, new Vector2Int(2, 4), new Vector2Int(6, 4), 0f);
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(model.GetComponent<Collider>());
        model.name = "SelectableModel";
        model.transform.SetParent(building.Instance.transform, false);
        model.transform.localScale = new Vector3(3f, 1.6f, 2f);
        _objects.Add(model);
        runtimeBuildings.AddBuilding(building.Id, building);

        BuildingSelectionMarkerPresentationSystemHelper system = CreateBuildingSelectionMarkerPresentationSystemHelper();
        BuildingSelectionMarkerPresentationSystemHelper.Context context = CreateContext(runtimeBuildings);

        runtimeBuildings.SelectBuilding(building.Id);
        system.Refresh(context);

        GameObject marker = system.RuntimeMarkerForTests;
        Assert.IsNotNull(marker.GetComponent<PremiumWorldSelectionObjectOutlineView>());
        Transform overlay = FindDescendantContaining(building.Instance.transform, "SelectionObjectOutline_");
        Assert.IsNotNull(overlay, "Selected building model must receive a mesh-bound selection outline overlay.");
        Assert.IsTrue(overlay.gameObject.activeSelf);
        MeshRenderer overlayRenderer = overlay.GetComponent<MeshRenderer>();
        Assert.IsNotNull(overlayRenderer);
        Assert.IsNotNull(overlayRenderer.sharedMaterial);
        Assert.AreEqual("WarlineCapture/Markers/SelectionObjectOutline", overlayRenderer.sharedMaterial.shader.name);

        runtimeBuildings.ClearSelection();
        system.Refresh(context);

        Assert.IsFalse(overlay.gameObject.activeSelf);
        system.Dispose(context);
    }

    private BuildingSelectionMarkerPresentationSystemHelper.Context CreateContext(RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildings)
    {
        GridConfig grid = new()
        {
            CellSize = 1f,
            Width = 64,
            Height = 64
        };

        return new BuildingSelectionMarkerPresentationSystemHelper.Context(
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
            CreateBuildingVisualSystem(),
            null,
            new MaterialPropertyBlock(),
            Object.DestroyImmediate);
    }

    private BuildingVisualSystem CreateBuildingVisualSystem()
    {
        _world ??= new World(nameof(BuildingSelectionMarkerPresentationSystemHelperTests));
        return _world.GetOrCreateSystemManaged<BuildingVisualSystem>();
    }

    private BuildingFactionVisualSystem CreateBuildingFactionVisualSystem()
    {
        _world ??= new World(nameof(BuildingSelectionMarkerPresentationSystemHelperTests));
        return _world.GetOrCreateSystemManaged<BuildingFactionVisualSystem>();
    }

    private BuildingRuntimeVisualPresentationSystemHelper CreateBuildingRuntimeVisualPresentationSystemHelper()
    {
        return new BuildingRuntimeVisualPresentationSystemHelper();
    }

    private BuildingSelectionMarkerPresentationSystemHelper CreateBuildingSelectionMarkerPresentationSystemHelper()
    {
        return new BuildingSelectionMarkerPresentationSystemHelper();
    }

    private RuntimeBuildingEntity CreateBuilding(int id, Vector2Int origin, Vector2Int footprint, float y)
    {
        GameObject instance = new($"Building_{id}");
        _objects.Add(instance);
        instance.transform.position = new Vector3(origin.x, y, origin.y);
        return new RuntimeBuildingEntity
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
        Material material = AssetDatabase.LoadAssetAtPath<Material>(SelectionMarkerMaterialPath);
        Assert.IsNotNull(material, $"Missing selection marker material at {SelectionMarkerMaterialPath}");
        model.GetComponent<Renderer>().sharedMaterial = material;
        model.transform.SetParent(marker.transform, false);
        model.transform.localScale = Vector3.one;
        return marker;
    }

    private static void AssertMarkerRenderableMinY(GameObject marker, float expectedMinimumY)
    {
        Assert.IsTrue(TryCalculateRendererBounds(marker, out Bounds bounds), "Marker must have renderer bounds.");
        Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(expectedMinimumY - 0.001f));
    }

    private static bool TryCalculateRendererBounds(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (hasBounds)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            else
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
        }

        return hasBounds;
    }

    private static Transform FindDescendantContaining(Transform root, string nameFragment)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform != null &&
                transform.name.Contains(nameFragment, System.StringComparison.OrdinalIgnoreCase))
            {
                return transform;
            }
        }

        return null;
    }

    private static void AssertColorClose(Color expected, Color actual)
    {
        Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
        Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
        Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
        Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
    }
}
#endif

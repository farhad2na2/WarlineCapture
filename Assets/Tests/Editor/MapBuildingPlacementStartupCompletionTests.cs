using NUnit.Framework;
using Exception = System.Exception;
using UnityEditor;
using UnityEngine;
using Game.Components;
using Game.Configs;
using Game.Runtime;

public sealed class MapBuildingPlacementStartupCompletionTests
{
    [MenuItem("Tools/Validation/Static Reuse Selection Geometry Focused")]
    public static void RunStaticReuseSelectionGeometryValidation()
    {
        try
        {
            using (ValidationExit.SuppressProcessExit())
            {
                RunFocusedValidation();
                BuildingSelectionMarkerPresentationSystemHelperTests.RunFocusedValidation();
                ValidationExit.ClearLastExitCode();
                RuntimeBuildingSystemTests.RunFocusedValidation();
                if (ValidationExit.LastExitCode != 0)
                    throw new System.InvalidOperationException(
                        $"Runtime building focused validation exit={ValidationExit.LastExitCode?.ToString() ?? "missing"}.");
            }

            Debug.Log("[StaticReuseSelectionGeometryValidation] result=Passed placementTests=5 markerTests=10 runtimeTests=7");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[StaticReuseSelectionGeometryValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    public static void RunFocusedValidation()
    {
        var tests = new MapBuildingPlacementStartupCompletionTests();
        tests.EmptyPlacementConfigCompletesOnlyAfterAuthoringRootIsHidden();
        tests.RendererFreeMapUsesBakedPlacementPrefab();
        tests.StaticPresentationMapDoesNotClonePrefabWhenAuthoringVisualIsMissing();
        tests.StaticPresentationMapRetainsExactPlacementPrefabGeometryWithoutCloning();
        tests.OperationMapCompatibilityPlacementsNeverClonePrefabsWithoutAuthoringHierarchy();
        UnityEngine.Debug.Log("[MapBuildingPlacementStartupCompletionValidation] result=Passed tests=5");
    }

    [Test]
    public void EmptyPlacementConfigCompletesOnlyAfterAuthoringRootIsHidden()
    {
        MapBuildingPlacementConfig config = ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
        GameObject root = new("Buildings");
        try
        {
            MapBuildingPlacementSpawnPrefabSystemHelper system = new();
            var context = new MapBuildingPlacementSpawnPrefabSystemHelper.Context(
                config,
                root.transform,
                runtimeSpawnSystem: null,
                runtimeSpawnContext: default,
                tryGetGridData: null,
                logWarning: null);

            Assert.IsFalse(system.IsCompleteFor(config, root.transform));

            system.Update(context);
            Assert.IsFalse(system.IsCompleteFor(config, root.transform));
            Assert.IsTrue(root.activeSelf);

            system.Update(context);
            Assert.IsTrue(system.IsCompleteFor(config, root.transform));
            Assert.IsFalse(root.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void RendererFreeMapUsesBakedPlacementPrefab()
    {
        MapBuildingPlacementConfig config = ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
        GameObject authoringRoot = new("Buildings");
        GameObject runtimeRoot = new("RuntimeBuildings");
        GameObject prefab = new("Building_Test");
        try
        {
            var placement = new MapBuildingPlacementConfigEntry(
                "Map/Buildings/Building_Test/Visual",
                "Building_Test",
                prefab,
                factionId: 1,
                worldCenter: new Vector3(10f, 0f, 20f),
                worldPosition: new Vector3(10f, 0f, 20f),
                worldEulerAngles: new Vector3(0f, 45f, 0f),
                worldScale: Vector3.one,
                yawDegrees: 45f,
                rotateVertical: false);
            var context = new MapBuildingPlacementSpawnPrefabSystemHelper.Context(
                config,
                authoringRoot.transform,
                runtimeSpawnSystem: null,
                runtimeSpawnContext: default,
                tryGetGridData: null,
                logWarning: Assert.Fail);

            GameObject wrapper = MapBuildingPlacementSpawnPrefabSystemHelper.CreateAuthoredMapVisualInstance(
                context,
                placement,
                runtimeRoot.transform);

            Assert.That(wrapper, Is.Not.Null);
            Assert.That(wrapper.transform.childCount, Is.EqualTo(1));
            Assert.That(wrapper.transform.GetChild(0).name, Is.EqualTo(prefab.name));
            Assert.That(wrapper.transform.position, Is.EqualTo(placement.WorldPosition));
        }
        finally
        {
            Object.DestroyImmediate(runtimeRoot);
            Object.DestroyImmediate(authoringRoot);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void StaticPresentationMapDoesNotClonePrefabWhenAuthoringVisualIsMissing()
    {
        MapBuildingPlacementConfig config = ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
        GameObject authoringRoot = new("Buildings");
        GameObject runtimeRoot = new("RuntimeBuildings");
        GameObject prefab = new("Building_Refinery");
        try
        {
            config.EditorSetUseExistingStaticPresentationWhenAuthoringVisualMissing(true);
            var placement = new MapBuildingPlacementConfigEntry(
                "Map/Buildings/Building_Refinery/SM_Bld_GasTower_02",
                "Building_Refinery",
                prefab,
                factionId: 1,
                worldCenter: new Vector3(10f, 0f, 20f),
                worldPosition: new Vector3(10f, 0f, 20f),
                worldEulerAngles: Vector3.zero,
                worldScale: Vector3.one,
                yawDegrees: 0f,
                rotateVertical: false);
            var context = new MapBuildingPlacementSpawnPrefabSystemHelper.Context(
                config,
                authoringRoot.transform,
                runtimeSpawnSystem: null,
                runtimeSpawnContext: default,
                tryGetGridData: null,
                logWarning: Assert.Fail);

            GameObject wrapper = MapBuildingPlacementSpawnPrefabSystemHelper.CreateAuthoredMapVisualInstance(
                context,
                placement,
                runtimeRoot.transform);

            Assert.That(wrapper, Is.Not.Null);
            Assert.That(wrapper.transform.childCount, Is.Zero);
            Assert.That(wrapper.GetComponent<MapAuthoredBuildingVisualComponent>(), Is.Not.Null);
            Assert.That(wrapper.transform.position, Is.EqualTo(placement.WorldPosition));
        }
        finally
        {
            Object.DestroyImmediate(runtimeRoot);
            Object.DestroyImmediate(authoringRoot);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void StaticPresentationMapRetainsExactPlacementPrefabGeometryWithoutCloning()
    {
        MapBuildingPlacementConfig config = ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
        GameObject authoringRoot = new("Buildings");
        GameObject runtimeRoot = new("RuntimeBuildings");
        GameObject prefab = new("Building_ExactPresentation");
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(prefab.transform, false);
            visual.transform.localPosition = new Vector3(3f, 2f, -4f);
            visual.transform.localScale = new Vector3(6f, 4f, 10f);
            config.EditorSetUseExistingStaticPresentationWhenAuthoringVisualMissing(true);
            var placement = new MapBuildingPlacementConfigEntry(
                "Map/Buildings/Building_ExactPresentation/Visual",
                "Building_ExactPresentation",
                prefab,
                factionId: 1,
                worldCenter: new Vector3(20f, 3f, 30f),
                worldPosition: new Vector3(20f, 1f, 30f),
                worldEulerAngles: new Vector3(0f, 90f, 0f),
                worldScale: new Vector3(2f, 1f, 0.5f),
                yawDegrees: 90f,
                rotateVertical: true);
            var context = new MapBuildingPlacementSpawnPrefabSystemHelper.Context(
                config,
                authoringRoot.transform,
                runtimeSpawnSystem: null,
                runtimeSpawnContext: default,
                tryGetGridData: null,
                logWarning: Assert.Fail);

            GameObject wrapper = MapBuildingPlacementSpawnPrefabSystemHelper.CreateAuthoredMapVisualInstance(
                context,
                placement,
                runtimeRoot.transform);

            Assert.That(wrapper, Is.Not.Null);
            Assert.That(wrapper.transform.childCount, Is.Zero);
            MapAuthoredBuildingVisualComponent presentation =
                wrapper.GetComponent<MapAuthoredBuildingVisualComponent>();
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.HasPresentationGeometry, Is.True);
            Assert.That(presentation.PresentationWorldCenter, Is.EqualTo(placement.WorldCenter));
            Assert.That(presentation.PresentationWorldSize.x, Is.EqualTo(12f).Within(0.001f));
            Assert.That(presentation.PresentationWorldSize.y, Is.EqualTo(4f).Within(0.001f));
            Assert.That(presentation.PresentationWorldSize.z, Is.EqualTo(5f).Within(0.001f));
            Assert.That(presentation.PresentationYawDegrees, Is.EqualTo(90f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(runtimeRoot);
            Object.DestroyImmediate(authoringRoot);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void OperationMapCompatibilityPlacementsNeverClonePrefabsWithoutAuthoringHierarchy()
    {
        const string configPath =
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01_BuildingPlacements.asset";
        MapBuildingPlacementConfig config = AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(configPath);
        Assert.That(config, Is.Not.Null);
        Assert.That(config.UseExistingStaticPresentationWhenAuthoringVisualMissing, Is.True);
        Assert.That(config.Placements, Is.Not.Empty);

        GameObject mapRoot = new("RuntimeMapBindings");
        GameObject authoringRoot = new("Buildings");
        GameObject runtimeRoot = new("RuntimeBuildings");
        authoringRoot.transform.SetParent(mapRoot.transform, false);
        runtimeRoot.transform.SetParent(mapRoot.transform, false);
        try
        {
            var context = new MapBuildingPlacementSpawnPrefabSystemHelper.Context(
                config,
                authoringRoot.transform,
                runtimeSpawnSystem: null,
                runtimeSpawnContext: default,
                tryGetGridData: null,
                logWarning: null);

            for (int index = 0; index < config.Placements.Count; index++)
            {
                MapBuildingPlacementConfigEntry placement = config.Placements[index];
                GameObject wrapper = MapBuildingPlacementSpawnPrefabSystemHelper.CreateAuthoredMapVisualInstance(
                    context,
                    placement,
                    runtimeRoot.transform);

                Assert.That(wrapper, Is.Not.Null, $"Placement {index} did not create its logical wrapper.");
                Assert.That(
                    wrapper.transform.childCount,
                    Is.Zero,
                    $"Placement {index} ({placement.SourcePath}) cloned {placement.BuildingPrefab.name} over static presentation.");
            }
        }
        finally
        {
            Object.DestroyImmediate(mapRoot);
        }
    }
}

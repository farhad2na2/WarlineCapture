#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WarlineCaptureDemoClusterCityBuilder
{
    private const string DemoScenePath = "Assets/Game/Scenes/Demo.unity";
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC02_DemoClusterCity_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048";
    private const string ReportPath = "Design/AgentReports/2026-05-20_gameplay_gc02-demo-cluster-city-2048.md";
    private const float MapSize = 2048f;

    private static readonly List<string> CloneLog = new();
    private static readonly List<string> RejectLog = new();
    private static readonly List<string> UnitProofLog = new();
    private static int rejectedInteriorTerrainRoots;

    private readonly struct ClusterSpec
    {
        public readonly string Name;
        public readonly Bounds SourceBounds;
        public readonly Vector3 TargetCenter;
        public readonly float RotationY;
        public readonly float Scale;
        public readonly bool FlattenGameplayY;

        public ClusterSpec(string name, Bounds sourceBounds, Vector3 targetCenter, float rotationY, float scale, bool flattenGameplayY)
        {
            Name = name;
            SourceBounds = sourceBounds;
            TargetCenter = targetCenter;
            RotationY = rotationY;
            Scale = scale;
            FlattenGameplayY = flattenGameplayY;
        }
    }

    [MenuItem("WarlineCapture/Design/Build Generated Scene GC02 Demo Cluster City 2048")]
    public static void BuildGc02DemoClusterCity2048()
    {
        CloneLog.Clear();
        RejectLog.Clear();
        UnitProofLog.Clear();
        rejectedInteriorTerrainRoots = 0;
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));

        Scene demoScene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        List<ClusterSpec> clusters = BuildClusterSpecs();
        Dictionary<string, List<GameObject>> sourceByCluster = new(StringComparer.Ordinal);
        foreach (ClusterSpec cluster in clusters)
            sourceByCluster[cluster.Name] = CollectSourceRoots(cluster);

        Scene generatedScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(generatedScene);
        BuildScene(sourceByCluster, clusters);
        EditorSceneManager.CloseScene(demoScene, true);
        EditorSceneManager.SaveScene(generatedScene, ScenePath);

        CaptureScene();
        WriteReport(sourceByCluster, clusters);
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GC02_DEMO_CLUSTER_CITY_BUILT scene={ScenePath} captureRoot={CaptureRoot} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static List<ClusterSpec> BuildClusterSpecs()
    {
        Bounds townCore = new(new Vector3(-38f, 16f, -58f), new Vector3(220f, 90f, 220f));
        Bounds townHighway = new(new Vector3(-10f, 10f, 40f), new Vector3(90f, 80f, 190f));
        Bounds baseCore = new(new Vector3(48f, 8f, 176f), new Vector3(130f, 80f, 140f));
        Bounds runwayCore = new(new Vector3(96f, 8f, 188f), new Vector3(70f, 60f, 220f));
        Bounds industrialCore = new(new Vector3(85f, 15f, 430f), new Vector3(280f, 120f, 240f));
        Bounds roadSpine = new(new Vector3(-11f, 5f, 0f), new Vector3(42f, 35f, 880f));

        return new List<ClusterSpec>
        {
            new("TownDistrict_A", townCore, new Vector3(-420f, 0f, -260f), 0f, 1.85f, true),
            new("TownDistrict_B", townCore, new Vector3(-385f, 0f, 220f), 180f, 1.65f, true),
            new("TownDistrict_C", townCore, new Vector3(260f, 0f, -360f), 90f, 1.55f, true),
            new("TownHighwayStrip", townHighway, new Vector3(-55f, 0f, -45f), 0f, 2.25f, true),
            new("MilitaryBase", baseCore, new Vector3(410f, 0f, 245f), 0f, 1.8f, true),
            new("RunwayEdge", runwayCore, new Vector3(690f, 0f, 280f), 0f, 2.05f, true),
            new("IndustrialObjective", industrialCore, new Vector3(470f, 0f, -395f), 180f, 1.45f, true),
            new("LongHighway", roadSpine, new Vector3(0f, 0f, 0f), 0f, 2.1f, true),
        };
    }

    private static void BuildScene(Dictionary<string, List<GameObject>> sourceByCluster, List<ClusterSpec> clusters)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.72f, 0.69f, 0.62f, 1f);
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.57f, 0.49f, 0.37f, 1f);
        RenderSettings.fogDensity = 0.00042f;

        GameObject root = new("GC02_DemoClusterCity_2048_Root");
        BuildFlatGameplayPlane(root);
        GameObject copied = Child(root, "DemoAuthoredClusters");
        foreach (ClusterSpec cluster in clusters)
            CloneCluster(copied, sourceByCluster[cluster.Name], cluster);

        BuildRtsUnitProof(root);
        BuildCompositionDressing(root);
        BuildCameras(root);
        BuildLight(root);
        NormalizeSceneLightBudget(root);
        BuildPostProcessing(root);
    }

    private static void BuildFlatGameplayPlane(GameObject root)
    {
        Material sand = CreateMaterial("GC02_SandBase", new Color(0.55f, 0.45f, 0.29f, 1f));
        Material border = CreateMaterial("GC02_Border", new Color(0.14f, 0.32f, 0.34f, 1f));
        Surface(root, "FlatGameplayPlane_2048x2048", Vector3.zero, new Vector2(MapSize, MapSize), sand);
        Surface(root, "NorthBounds_2048", new Vector3(0f, 0.04f, MapSize * 0.5f), new Vector2(MapSize, 6f), border);
        Surface(root, "SouthBounds_2048", new Vector3(0f, 0.04f, -MapSize * 0.5f), new Vector2(MapSize, 6f), border);
        Surface(root, "WestBounds_2048", new Vector3(-MapSize * 0.5f, 0.04f, 0f), new Vector2(6f, MapSize), border);
        Surface(root, "EastBounds_2048", new Vector3(MapSize * 0.5f, 0.04f, 0f), new Vector2(6f, MapSize), border);
    }

    private static void CloneCluster(GameObject parent, List<GameObject> sourceRoots, ClusterSpec cluster)
    {
        GameObject clusterRoot = Child(parent, cluster.Name);
        Quaternion rotation = Quaternion.Euler(0f, cluster.RotationY, 0f);
        foreach (GameObject source in sourceRoots)
        {
            if (source == null)
                continue;

            GameObject clone = Object.Instantiate(source);
            clone.name = cluster.Name + "_" + source.name;
            clone.transform.SetParent(clusterRoot.transform, true);

            Vector3 relative = source.transform.position - cluster.SourceBounds.center;
            relative = rotation * (relative * cluster.Scale);
            Vector3 target = cluster.TargetCenter + relative;
            if (cluster.FlattenGameplayY)
                target.y = source.transform.position.y * 0.18f;
            clone.transform.position = target;
            clone.transform.rotation = rotation * source.transform.rotation;
            clone.transform.localScale = source.transform.lossyScale * cluster.Scale;
            AlignBottomNearGround(clone);
        }

        CloneLog.Add($"{cluster.Name}: cloned {sourceRoots.Count} authored roots from Demo bounds center={Format(cluster.SourceBounds.center)} size={Format(cluster.SourceBounds.size)} to target={Format(cluster.TargetCenter)} scale={cluster.Scale.ToString("0.##", CultureInfo.InvariantCulture)}");
    }

    private static List<GameObject> CollectSourceRoots(ClusterSpec cluster)
    {
        Dictionary<int, GameObject> roots = new();
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (transform == null || transform.gameObject.scene.path != DemoScenePath)
                continue;
            if (transform.GetComponent<Camera>() != null || transform.GetComponent<Light>() != null)
                continue;

            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0)
                continue;

            Bounds bounds = CalculateBounds(renderers);
            if (!cluster.SourceBounds.Intersects(bounds))
                continue;
            if (IsSkyOrHugeBackground(transform.name, bounds))
                continue;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject);
            if (root == null)
                root = TopSceneObject(transform);
            if (root == null || root.GetComponent<Camera>() != null || root.GetComponent<Light>() != null)
                continue;

            Renderer[] rootRenderers = root.GetComponentsInChildren<Renderer>(false);
            if (rootRenderers.Length == 0)
                continue;
            Bounds rootBounds = CalculateBounds(rootRenderers);
            if (!cluster.SourceBounds.Intersects(rootBounds))
                continue;
            if (IsSkyOrHugeBackground(root.name, rootBounds))
                continue;
            if (IsInteriorTerrainBlocker(root, rootBounds))
            {
                rejectedInteriorTerrainRoots++;
                continue;
            }

            roots[root.GetInstanceID()] = root;
        }

        List<GameObject> result = roots.Values
            .Where(go => !HasSelectedAncestor(go.transform, roots))
            .OrderBy(go => go.name, StringComparer.Ordinal)
            .ToList();

        if (result.Count == 0)
            RejectLog.Add($"{cluster.Name}: no source roots selected from Demo.");
        return result;
    }

    private static bool HasSelectedAncestor(Transform transform, Dictionary<int, GameObject> selected)
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (selected.ContainsKey(parent.gameObject.GetInstanceID()))
                return true;
            parent = parent.parent;
        }

        return false;
    }

    private static GameObject TopSceneObject(Transform transform)
    {
        Transform current = transform;
        while (current.parent != null)
            current = current.parent;
        return current.gameObject;
    }

    private static void BuildCompositionDressing(GameObject root)
    {
        GameObject dressing = Child(root, "LargeScaleDressing");
        PlacePrefab(dressing, "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_03.prefab", new Vector3(-980f, 0f, 860f), 21f, new Vector3(0.34f, 0.1f, 0.34f));
        PlacePrefab(dressing, "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_03.prefab", new Vector3(980f, 0f, -850f), -35f, new Vector3(0.34f, 0.1f, 0.34f));
        PlacePrefab(dressing, "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_02.prefab", new Vector3(-950f, 0f, -870f), 39f, new Vector3(0.28f, 0.09f, 0.28f));
        PlacePrefab(dressing, "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_01.prefab", new Vector3(940f, 0f, 875f), -12f, new Vector3(0.28f, 0.09f, 0.28f));
        PlacePrefab(dressing, "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_05.prefab", new Vector3(-1045f, 0f, -860f), 24f, new Vector3(0.85f, 0.55f, 0.85f));
        PlacePrefab(dressing, "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_01.prefab", new Vector3(1040f, 0f, 840f), -18f, new Vector3(0.78f, 0.55f, 0.78f));
        PlacePrefab(dressing, "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_06.prefab", new Vector3(-1060f, 0f, 320f), 8f, new Vector3(0.62f, 0.45f, 0.62f));
        PlacePrefab(dressing, "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_04.prefab", new Vector3(1060f, 0f, -280f), -31f, new Vector3(0.62f, 0.45f, 0.62f));
    }

    private static void BuildCameras(GameObject root)
    {
        Camera overview = CameraObject(root, "Camera_GC02_Overview");
        overview.orthographic = true;
        overview.orthographicSize = 760f;
        overview.transform.position = new Vector3(0f, 1100f, 0f);
        overview.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Camera city = CameraObject(root, "Camera_GC02_CityClose");
        city.fieldOfView = 34f;
        city.transform.position = new Vector3(-760f, 360f, -720f);
        city.transform.LookAt(new Vector3(-170f, 0f, -85f));

        Camera baseView = CameraObject(root, "Camera_GC02_BaseClose");
        baseView.fieldOfView = 34f;
        baseView.transform.position = new Vector3(1060f, 460f, 700f);
        baseView.transform.LookAt(new Vector3(520f, 0f, 245f));

        Camera rtsCity = CameraObject(root, "Camera_GC02_RtsCityUnits");
        rtsCity.orthographic = true;
        rtsCity.orthographicSize = 112f;
        rtsCity.transform.position = new Vector3(-315f, 175f, -385f);
        rtsCity.transform.rotation = Quaternion.Euler(40f, 0f, 0f);

        Camera rtsConvoy = CameraObject(root, "Camera_GC02_RtsConvoyUnits");
        rtsConvoy.orthographic = true;
        rtsConvoy.orthographicSize = 96f;
        rtsConvoy.transform.position = new Vector3(-155f, 175f, -470f);
        rtsConvoy.transform.rotation = Quaternion.Euler(40f, 0f, 0f);

        Camera rtsProfessional = CameraObject(root, "Camera_GC02_RtsProfessional40");
        rtsProfessional.orthographic = false;
        rtsProfessional.fieldOfView = 42f;
        rtsProfessional.transform.position = new Vector3(-355f, 68f, -295f);
        rtsProfessional.transform.LookAt(new Vector3(-355f, 0f, -205f));

        BuildRtsPerspectiveCamera(root, "Camera_GC02_RtsLitScene01_CityLane", new Vector3(-355f, 0f, -205f), new Vector3(0f, 68f, -90f));
        BuildRtsPerspectiveCamera(root, "Camera_GC02_RtsLitScene02_HighwayPush", new Vector3(-335f, 0f, -238f), new Vector3(0f, 70f, -92f));
        BuildRtsPerspectiveCamera(root, "Camera_GC02_RtsLitScene03_TownEntry", new Vector3(-455f, 0f, -260f), new Vector3(0f, 70f, -92f));
        BuildRtsPerspectiveCamera(root, "Camera_GC02_RtsLitScene04_TownMarket", new Vector3(-415f, 0f, 195f), new Vector3(0f, 72f, -96f));
        BuildRtsPerspectiveCamera(root, "Camera_GC02_RtsLitScene05_BaseApproach", new Vector3(420f, 0f, 245f), new Vector3(0f, 80f, -112f));
    }

    private static Camera BuildRtsPerspectiveCamera(GameObject root, string name, Vector3 target, Vector3 offset)
    {
        Camera camera = CameraObject(root, name);
        camera.orthographic = false;
        camera.fieldOfView = 42f;
        camera.transform.position = target + offset;
        camera.transform.LookAt(target);
        return camera;
    }

    private static Camera CameraObject(GameObject root, string name)
    {
        GameObject cameraObject = Child(root, name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.42f, 0.35f, 0.24f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 3000f;
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        return camera;
    }

    private static void BuildLight(GameObject root)
    {
        GameObject lightObject = Child(root, "DirectionalLight_Key");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.7f;
        light.color = new Color(1f, 0.9f, 0.72f, 1f);
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.58f;
        light.shadowBias = 0.04f;
        light.shadowNormalBias = 0.42f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -42f, 0f);

        GameObject fillObject = Child(root, "DirectionalLight_Fill");
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.5f;
        fill.color = new Color(0.58f, 0.74f, 1f, 1f);
        fill.shadows = LightShadows.None;
        fillObject.transform.rotation = Quaternion.Euler(30f, 136f, 0f);

        GameObject rimObject = Child(root, "DirectionalLight_Rim");
        Light rim = rimObject.AddComponent<Light>();
        rim.type = LightType.Directional;
        rim.intensity = 0.22f;
        rim.color = new Color(0.9f, 0.96f, 1f, 1f);
        rim.shadows = LightShadows.None;
        rimObject.transform.rotation = Quaternion.Euler(26f, 210f, 0f);
    }

    private static void NormalizeSceneLightBudget(GameObject root)
    {
        foreach (Light light in root.GetComponentsInChildren<Light>(true))
        {
            if (light.type == LightType.Directional)
                continue;

            light.shadows = LightShadows.None;
            light.intensity = Mathf.Min(light.intensity * 0.15f, 0.25f);
        }
    }

    private static void BuildPostProcessing(GameObject root)
    {
        GameObject volumeObject = Child(root, "GC02_RTS_PresentationVolume");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "GC02_RTS_PresentationProfile";
        volume.sharedProfile = profile;

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.12f);
        color.contrast.Override(14f);
        color.saturation.Override(4f);
        color.colorFilter.Override(new Color(1f, 0.95f, 0.86f, 1f));

        Tonemapping tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.ACES);

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.04f);
        bloom.threshold.Override(1.4f);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.035f);
        vignette.smoothness.Override(0.35f);
    }

    private static void CaptureScene()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (camera.name == "Camera_GC02_Overview")
                Render(camera, ProjectPath(CaptureRoot + "/gc02_overview_2048_map_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC02_CityClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc02_city_close_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC02_BaseClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc02_base_close_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC02_RtsCityUnits")
            {
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_city_units_1920x1080.png"), 1920, 1080);
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_ortho_city_units_1920x1080.png"), 1920, 1080);
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_60deg_city_units_1920x1080.png"), 1920, 1080);
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_40deg_city_units_1920x1080.png"), 1920, 1080);
            }
            if (camera.name == "Camera_GC02_RtsConvoyUnits")
            {
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_convoy_units_1920x1080.png"), 1920, 1080);
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_ortho_convoy_units_1920x1080.png"), 1920, 1080);
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_60deg_convoy_units_1920x1080.png"), 1920, 1080);
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_40deg_convoy_units_1920x1080.png"), 1920, 1080);
            }
            if (camera.name == "Camera_GC02_RtsProfessional40")
            {
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_professional_40deg_1920x1080.png"), 1920, 1080);
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_readability_32deg_1920x1080.png"), 1920, 1080);
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_gameplay_readable_35deg_1920x1080.png"), 1920, 1080);
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_unit_control_zoom_35deg_1920x1080.png"), 1920, 1080);
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_perspective_readable_3d_1920x1080.png"), 1920, 1080);
            }
            if (camera.name == "Camera_GC02_RtsLitScene01_CityLane")
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_lit_scene_01_city_lane_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC02_RtsLitScene02_HighwayPush")
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_lit_scene_02_highway_push_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC02_RtsLitScene03_TownEntry")
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_lit_scene_03_town_entry_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC02_RtsLitScene04_TownMarket")
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_lit_scene_04_town_market_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC02_RtsLitScene05_BaseApproach")
                Render(camera, ProjectPath(CaptureRoot + "/gc02_rts_lit_scene_05_base_approach_1920x1080.png"), 1920, 1080);
        }
    }

    private static void WriteReport(Dictionary<string, List<GameObject>> sourceByCluster, List<ClusterSpec> clusters)
    {
        StringBuilder report = new();
        report.AppendLine("# GC02 Demo Cluster City 2048");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Build a high-end 2048x2048 generated city scene by cloning authored Demo scene clusters with child decoration intact.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureDemoClusterCityBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC02_DemoClusterCity_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_overview_2048_map_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_city_close_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_base_close_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_city_units_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_convoy_units_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_ortho_city_units_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_ortho_convoy_units_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_60deg_city_units_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_60deg_convoy_units_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_40deg_city_units_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_40deg_convoy_units_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_professional_40deg_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_readability_32deg_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_gameplay_readable_35deg_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_unit_control_zoom_35deg_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_perspective_readable_3d_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_lit_scene_01_city_lane_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_lit_scene_02_highway_push_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_lit_scene_03_town_entry_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_lit_scene_04_town_market_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC02_DemoClusterCity_2048/gc02_rts_lit_scene_05_base_approach_1920x1080.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: none. This is a generated design-target scene and does not change runtime ECS/game flow.");
        report.AppendLine("User-visible behavior: none in shipped flow yet; the scene is available for visual review under Generated scenes.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureDemoClusterCityBuilder.BuildGc02DemoClusterCity2048`.");
        report.AppendLine("Validation result: scene saved and five fresh RTS perspective lighting proof captures exported with shadowed key light, cooler fill, subtle fog, and URP post processing enabled on proof cameras.");
        report.AppendLine("Known gaps: interior large terrain is filtered so playable districts stay visually flat; some Demo base/runway source materials still read darker than the city district, so the base cluster needs a material/decal audit before PM acceptance. Next pass should add path/walkability overlays, road masks, and more city block variants.");
        report.AppendLine("Cross-lane impacts: Designer/PM can review scene composition; no UI/runtime source files are changed.");
        report.AppendLine("Next recommended task: convert accepted clusters into reusable city-block templates with footprint metadata and blocked/walkable masks.");
        report.AppendLine();
        report.AppendLine($"Map size: {MapSize:0}x{MapSize:0} world units.");
        report.AppendLine();
        report.AppendLine("Clusters cloned:");
        foreach (string line in CloneLog)
            report.AppendLine("- " + line);
        report.AppendLine();
        report.AppendLine($"Interior terrain/blocker roots rejected from playable clusters: {rejectedInteriorTerrainRoots}");
        report.AppendLine();
        report.AppendLine("RTS proof units:");
        foreach (string line in UnitProofLog)
            report.AppendLine("- " + line);
        if (RejectLog.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("Selection warnings:");
            foreach (string line in RejectLog)
                report.AppendLine("- " + line);
        }

        File.WriteAllText(ProjectPath(ReportPath), report.ToString(), Encoding.UTF8);
    }

    private static GameObject PlacePrefab(GameObject parent, string path, Vector3 position, float rotationY, Vector3 scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            RejectLog.Add("Missing dressing prefab: " + path);
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent.transform, true);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        instance.transform.localScale = scale;
        AlignBottomNearGround(instance);
        return instance;
    }

    private static void BuildRtsUnitProof(GameObject root)
    {
        GameObject unitRoot = Child(root, "RTS_UnitScaleProof_NotRuntime");
        Material blueRing = CreateMaterial("GC02_RtsProof_BlueRing", new Color(0.08f, 0.18f, 0.9f, 1f));
        Material redRing = CreateMaterial("GC02_RtsProof_RedRing", new Color(0.78f, 0.06f, 0.04f, 1f));

        Vector3[] blueSquad =
        {
            new(-430f, 0f, -245f),
            new(-414f, 0f, -232f),
            new(-446f, 0f, -228f),
            new(-398f, 0f, -214f),
            new(-462f, 0f, -210f),
            new(-426f, 0f, -198f)
        };

        Vector3[] redSquad =
        {
            new(-300f, 0f, -158f),
            new(-282f, 0f, -146f),
            new(-318f, 0f, -142f),
            new(-264f, 0f, -130f),
            new(-336f, 0f, -124f),
            new(-300f, 0f, -110f)
        };

        foreach (Vector3 position in blueSquad)
            PlaceUnit(unitRoot, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab", position, 36f, Vector3.one * 1.15f, blueRing, "blue infantry");
        foreach (Vector3 position in redSquad)
            PlaceUnit(unitRoot, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab", position, 216f, Vector3.one * 1.15f, redRing, "red infantry");

        PlaceUnit(unitRoot, "Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Heavy.prefab", new Vector3(-455f, 0f, -295f), 38f, Vector3.one * 1.2f, blueRing, "blue APC");
        PlaceUnit(unitRoot, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab", new Vector3(-398f, 0f, -270f), 36f, Vector3.one * 1.2f, blueRing, "blue tank");
        PlaceUnit(unitRoot, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Light_Armored_Car.prefab", new Vector3(-285f, 0f, -210f), 218f, Vector3.one * 1.2f, redRing, "red armored car");
        PlaceUnit(unitRoot, "Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Fast.prefab", new Vector3(-230f, 0f, -184f), 218f, Vector3.one * 1.2f, redRing, "red APC");
    }

    private static GameObject PlaceUnit(GameObject parent, string path, Vector3 position, float rotationY, Vector3 scale, Material ringMaterial, string label)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            RejectLog.Add("Missing RTS proof unit prefab: " + path);
            return null;
        }

        GameObject unit = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        unit.name = "RTSProof_" + Path.GetFileNameWithoutExtension(path);
        unit.transform.SetParent(parent.transform, true);
        unit.transform.position = position;
        unit.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        unit.transform.localScale = scale;
        AlignBottomNearGround(unit);
        BuildSelectionRing(parent, unit.name + "_Ring", new Vector3(unit.transform.position.x, 0.08f, unit.transform.position.z), IsVehicleLabel(label) ? 8.5f : 3.1f, ringMaterial);
        UnitProofLog.Add($"{label}: {path} at {Format(unit.transform.position)} yaw={rotationY:0.#}");
        return unit;
    }

    private static bool IsVehicleLabel(string label)
    {
        return label.Contains("APC", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("tank", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("car", StringComparison.OrdinalIgnoreCase);
    }

    private static void BuildSelectionRing(GameObject parent, string name, Vector3 position, float radius, Material material)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = name;
        ring.transform.SetParent(parent.transform, true);
        ring.transform.position = position;
        ring.transform.localScale = new Vector3(radius, 0.035f, radius);
        Object.DestroyImmediate(ring.GetComponent<Collider>());
        MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void AlignBottomNearGround(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return;

        Bounds bounds = CalculateBounds(renderers);
        if (bounds.min.y < -2f || bounds.min.y > 8f)
            go.transform.position -= new Vector3(0f, bounds.min.y, 0f);
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static bool IsSkyOrHugeBackground(string name, Bounds bounds)
    {
        string lower = name.ToLowerInvariant();
        return lower.Contains("sky") ||
            lower.Contains("cloud") ||
            lower.Contains("skydome") ||
            bounds.center.y > 500f ||
            bounds.size.x > 1300f ||
            bounds.size.z > 1300f;
    }

    private static bool IsInteriorTerrainBlocker(GameObject root, Bounds bounds)
    {
        string lowerName = root.name.ToLowerInvariant();
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(root);
        string path = source == null ? string.Empty : AssetDatabase.GetAssetPath(source).ToLowerInvariant();
        bool terrainNamed =
            lowerName.Contains("mountain") ||
            lowerName.Contains("sanddune") ||
            lowerName.Contains("sand_dune") ||
            lowerName.Contains("dune") ||
            lowerName.Contains("hill") ||
            lowerName.Contains("slope") ||
            lowerName.Contains("ground_round") ||
            lowerName.Contains("ground_square") ||
            lowerName.Contains("ground_flat") ||
            path.Contains("mountain") ||
            path.Contains("sanddune") ||
            path.Contains("sand_dune") ||
            path.Contains("dune") ||
            path.Contains("hill") ||
            path.Contains("slope") ||
            path.Contains("ground_round") ||
            path.Contains("ground_square") ||
            path.Contains("ground_flat");

        bool tooTallForPlayableLane = bounds.size.y > 8f && (bounds.size.x > 18f || bounds.size.z > 18f);
        bool tooWideForPlayableLane = bounds.size.x > 55f || bounds.size.z > 55f;
        return terrainNamed && (tooTallForPlayableLane || tooWideForPlayableLane);
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static void Surface(GameObject parent, string name, Vector3 position, Vector2 size, Material material)
    {
        GameObject surface = new(name);
        surface.transform.SetParent(parent.transform, false);
        surface.transform.position = position;
        Mesh mesh = new();
        float halfX = size.x * 0.5f;
        float halfZ = size.y * 0.5f;
        mesh.vertices = new[]
        {
            new Vector3(-halfX, 0f, -halfZ),
            new Vector3(-halfX, 0f, halfZ),
            new Vector3(halfX, 0f, halfZ),
            new Vector3(halfX, 0f, -halfZ)
        };
        mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        MeshFilter filter = surface.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material material = new(shader)
        {
            name = name,
            color = color
        };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", Texture2D.whiteTexture);
        return material;
    }

    private static void Render(Camera camera, string path, int width, int height)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture texture = new(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
        camera.targetTexture = texture;
        RenderTexture.active = texture;
        GL.Clear(true, true, camera.backgroundColor);
        camera.Render();
        Texture2D image = new(width, height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        texture.Release();
        Object.DestroyImmediate(texture);
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);
    }

    private static string Format(Vector3 value)
    {
        return $"({value.x:0.##}, {value.y:0.##}, {value.z:0.##})";
    }
}
#endif

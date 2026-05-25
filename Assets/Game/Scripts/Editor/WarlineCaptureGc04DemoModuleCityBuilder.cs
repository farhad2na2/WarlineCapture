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

public static class WarlineCaptureGc04DemoModuleCityBuilder
{
    private const string DemoScenePath = "Assets/Game/Scenes/Demo.unity";
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC04_DemoModulePlayableCity_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC04_DemoModulePlayableCity_2048";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GC04_DemoModulePlayableCity_2048";
    private const string ModuleCatalogPath = DataRoot + "/gc04_demo_module_catalog.json";
    private const string ReportPath = "Design/AgentReports/2026-05-20_gameplay_gc04-demo-module-playable-city.md";
    private const float MapSize = 2048f;

    private static readonly List<Zone> Roads = new();
    private static readonly List<Zone> Spawns = new();
    private static readonly List<Zone> Objectives = new();
    private static readonly List<Zone> ModuleFootprints = new();
    private static readonly List<string> BuildLog = new();
    private static readonly List<string> ValidationLog = new();
    private static readonly List<string> RejectLog = new();
    private static readonly List<ModuleRecord> ModuleRecords = new();
    private static int rejectedInteriorTerrainRoots;

    private readonly struct Zone
    {
        public readonly string Name;
        public readonly Rect Rect;

        public Zone(string name, Rect rect)
        {
            Name = name;
            Rect = rect;
        }
    }

    private readonly struct ModuleSpec
    {
        public readonly string Name;
        public readonly string Role;
        public readonly Bounds SourceBounds;
        public readonly Vector3 TargetCenter;
        public readonly float RotationY;
        public readonly float Scale;

        public ModuleSpec(string name, string role, Bounds sourceBounds, Vector3 targetCenter, float rotationY, float scale)
        {
            Name = name;
            Role = role;
            SourceBounds = sourceBounds;
            TargetCenter = targetCenter;
            RotationY = rotationY;
            Scale = scale;
        }

        public Vector2 Footprint => new(SourceBounds.size.x * Scale + 18f, SourceBounds.size.z * Scale + 18f);
    }

    [Serializable]
    private sealed class ModuleRecord
    {
        public string name;
        public string role;
        public float sourceCenterX;
        public float sourceCenterZ;
        public float sourceWidth;
        public float sourceDepth;
        public float targetX;
        public float targetZ;
        public float rotationY;
        public float scale;
        public float footprintWidth;
        public float footprintDepth;
        public int clonedRoots;
    }

    [Serializable]
    private sealed class ModuleCatalogFile
    {
        public string generatedBy;
        public string sourceScene;
        public string generatedScene;
        public List<ModuleRecord> modules = new();
    }

    [MenuItem("WarlineCapture/Design/Build GC04 Demo Module Playable City 2048")]
    public static void BuildGc04DemoModulePlayableCity2048()
    {
        Roads.Clear();
        Spawns.Clear();
        Objectives.Clear();
        ModuleFootprints.Clear();
        BuildLog.Clear();
        ValidationLog.Clear();
        RejectLog.Clear();
        ModuleRecords.Clear();
        rejectedInteriorTerrainRoots = 0;

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        Directory.CreateDirectory(ProjectPath(DataRoot));

        Scene demoScene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        List<ModuleSpec> modules = BuildModuleSpecs();
        Dictionary<string, List<GameObject>> sourceByModule = new(StringComparer.Ordinal);
        foreach (ModuleSpec module in modules)
            sourceByModule[module.Name] = CollectSourceRoots(module);

        Scene generatedScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(generatedScene);

        GameObject root = new("GC04_DemoModulePlayableCity_2048_Root");
        BuildRenderEnvironment(root);
        BuildBasePlane(root);
        DefineGameplayMasks();
        BuildMaskSurfaces(root);
        CloneModules(root, sourceByModule, modules);
        NormalizeSceneLightBudget(root);
        PlaceSoldierRouteProof(root);
        BuildCameras(root);
        ValidateLayout();

        EditorSceneManager.CloseScene(demoScene, true);
        EditorSceneManager.SaveScene(generatedScene, ScenePath);
        CaptureScene();
        WriteModuleCatalog();
        WriteReport();
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GC04_DEMO_MODULE_CITY_BUILT scene={ScenePath} captureRoot={CaptureRoot} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static List<ModuleSpec> BuildModuleSpecs()
    {
        Bounds townCore = new(new Vector3(-38f, 16f, -58f), new Vector3(220f, 90f, 220f));
        Bounds townHighway = new(new Vector3(-10f, 10f, 40f), new Vector3(90f, 80f, 190f));
        Bounds baseCore = new(new Vector3(48f, 8f, 176f), new Vector3(130f, 80f, 140f));
        Bounds runwayCore = new(new Vector3(96f, 8f, 188f), new Vector3(70f, 60f, 220f));
        Bounds industrialCore = new(new Vector3(85f, 15f, 430f), new Vector3(280f, 120f, 240f));

        return new List<ModuleSpec>
        {
            new("TownBlock_SW_DemoAuthored", "town", townCore, new Vector3(-800f, 0f, -620f), 0f, 0.58f),
            new("TownBlock_SouthCenter_DemoAuthored", "town", townCore, new Vector3(-600f, 0f, -620f), 90f, 0.54f),
            new("TownBlock_SouthEast_DemoAuthored", "town", townCore, new Vector3(-400f, 0f, -620f), 180f, 0.54f),
            new("TownBlock_WestMid_DemoAuthored", "town", townCore, new Vector3(-800f, 0f, -220f), 270f, 0.56f),
            new("TownBlock_Center_DemoAuthored", "town", townCore, new Vector3(-600f, 0f, -220f), 180f, 0.56f),
            new("TownMarket_DemoAuthored", "town", townCore, new Vector3(-400f, 0f, 180f), 90f, 0.60f),
            new("TownBlock_WestMarket_DemoAuthored", "town", townCore, new Vector3(-800f, 0f, 180f), 0f, 0.54f),
            new("TownBlock_NorthCenter_DemoAuthored", "town", townCore, new Vector3(-600f, 0f, 580f), 180f, 0.54f),
            new("TownNorth_DemoAuthored", "town", townHighway, new Vector3(-800f, 0f, 580f), 0f, 0.72f),
            new("BaseBarracks_DemoAuthored", "base", baseCore, new Vector3(340f, 0f, -260f), 0f, 0.74f),
            new("BaseMotorPool_DemoAuthored", "base", baseCore, new Vector3(760f, 0f, -260f), 90f, 0.70f),
            new("BaseSouthDepot_DemoAuthored", "base", baseCore, new Vector3(340f, 0f, -740f), 180f, 0.66f),
            new("BaseCommand_DemoAuthored", "base", baseCore, new Vector3(560f, 0f, 240f), 180f, 0.72f),
            new("BaseNorthDepot_DemoAuthored", "base", baseCore, new Vector3(340f, 0f, 500f), 0f, 0.68f),
            new("RunwayApron_DemoAuthored", "base", runwayCore, new Vector3(780f, 0f, 500f), 0f, 0.62f),
            new("IndustrialObjective_DemoAuthored", "industrial", industrialCore, new Vector3(560f, 0f, -760f), 180f, 0.40f),
        };
    }

    private static void BuildRenderEnvironment(GameObject root)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.72f, 0.69f, 0.62f, 1f);
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.57f, 0.49f, 0.37f, 1f);
        RenderSettings.fogDensity = 0.00036f;

        Light key = Child(root, "DirectionalLight_Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.7f;
        key.color = new Color(1f, 0.9f, 0.72f, 1f);
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.58f;
        key.shadowBias = 0.04f;
        key.shadowNormalBias = 0.42f;
        key.transform.rotation = Quaternion.Euler(50f, -42f, 0f);

        Light fill = Child(root, "DirectionalLight_Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.46f;
        fill.color = new Color(0.58f, 0.74f, 1f, 1f);
        fill.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(30f, 136f, 0f);

        Volume volume = Child(root, "GC04_RTS_PresentationVolume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "GC04_RTS_PresentationProfile";
        volume.sharedProfile = profile;
        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.1f);
        color.contrast.Override(14f);
        color.saturation.Override(4f);
        color.colorFilter.Override(new Color(1f, 0.95f, 0.86f, 1f));
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
        profile.Add<Bloom>(true).intensity.Override(0.035f);
    }

    private static void BuildBasePlane(GameObject root)
    {
        Surface(root, "FlatGameplayPlane_2048x2048", Vector3.zero, new Vector2(MapSize, MapSize), CreateMaterial("GC04_SandBase", new Color(0.55f, 0.45f, 0.29f, 1f)), 0f);
    }

    private static void DefineGameplayMasks()
    {
        AddRoad("MainHighway", 0f, 0f, 92f, 1920f);

        foreach (float x in new[] { -900f, -700f, -500f, -300f, -110f })
            AddRoad($"TownVertical_{x:0}", x, 0f, 36f, 1560f);
        foreach (float z in new[] { -720f, -520f, -320f, -120f, 80f, 280f, 480f, 680f })
            AddRoad($"TownHorizontal_{z:0}", -505f, z, 850f, 38f);

        AddRoad("TownHighwayConnector_North", -145f, 480f, 260f, 38f);
        AddRoad("TownHighwayConnector_Market", -145f, 80f, 260f, 38f);
        AddRoad("TownHighwayConnector_South", -145f, -320f, 260f, 38f);

        foreach (float x in new[] { 230f, 450f, 670f, 850f })
            AddRoad($"BaseVertical_{x:0}", x, 40f, 36f, 1420f);
        foreach (float z in new[] { -620f, -380f, -140f, 120f, 360f, 600f })
            AddRoad($"BaseHorizontal_{z:0}", 535f, z, 690f, 38f);

        AddRoad("BaseHighwayConnector_North", 145f, 360f, 260f, 38f);
        AddRoad("BaseHighwayConnector_Gate", 145f, 120f, 260f, 38f);
        AddRoad("IndustrialHighwayConnector", 145f, -380f, 260f, 38f);

        Spawns.Add(new Zone("PlayerSpawn_WestTownStreet", CenterRect(-900f, -320f, 74f, 48f)));
        Spawns.Add(new Zone("EnemySpawn_BaseRoad", CenterRect(450f, 120f, 74f, 48f)));
        Objectives.Add(new Zone("Objective_TownMarket", CenterRect(-500f, 80f, 94f, 60f)));
        Objectives.Add(new Zone("Objective_BaseGate", CenterRect(230f, 120f, 90f, 60f)));
        Objectives.Add(new Zone("Objective_IndustrialFuel", CenterRect(450f, -380f, 96f, 60f)));
    }

    private static void AddRoad(string name, float centerX, float centerZ, float sizeX, float sizeZ)
    {
        Roads.Add(new Zone(name, CenterRect(centerX, centerZ, sizeX, sizeZ)));
    }

    private static void BuildMaskSurfaces(GameObject root)
    {
        GameObject masks = Child(root, "GameplayMasks_VisibleProof");
        Material road = CreateMaterial("GC04_WalkableRoad_Proof", new Color(0.13f, 0.16f, 0.14f, 1f));
        Material shoulder = CreateMaterial("GC04_WalkableShoulder_Proof", new Color(0.68f, 0.56f, 0.35f, 1f));
        Material spawn = CreateMaterial("GC04_Spawn_Proof", new Color(0.04f, 0.18f, 0.9f, 1f));
        Material objective = CreateMaterial("GC04_Objective_Proof", new Color(0.9f, 0.48f, 0.04f, 1f));

        foreach (Zone roadZone in Roads)
        {
            Surface(masks, roadZone.Name + "_Shoulder", Center(roadZone.Rect, 0.045f), new Vector2(roadZone.Rect.width + 18f, roadZone.Rect.height + 18f), shoulder, 0.045f);
            Surface(masks, roadZone.Name, Center(roadZone.Rect, 0.055f), new Vector2(roadZone.Rect.width, roadZone.Rect.height), road, 0.055f);
        }

        foreach (Zone zone in Spawns)
            Surface(masks, zone.Name, Center(zone.Rect, 0.075f), new Vector2(zone.Rect.width, zone.Rect.height), spawn, 0.075f);
        foreach (Zone zone in Objectives)
            Surface(masks, zone.Name, Center(zone.Rect, 0.078f), new Vector2(zone.Rect.width, zone.Rect.height), objective, 0.078f);
    }

    private static void CloneModules(GameObject root, Dictionary<string, List<GameObject>> sourceByModule, List<ModuleSpec> modules)
    {
        GameObject moduleRoot = Child(root, "DemoAuthoredPlayableModules");
        foreach (ModuleSpec module in modules)
        {
            Rect footprint = FootprintRect(module.TargetCenter, module.Footprint);
            if (Roads.Any(road => road.Rect.Overlaps(footprint)))
            {
                ValidationLog.Add($"ERROR: module {module.Name} footprint overlaps explicit walkable road. footprint={Format(footprint)}");
                continue;
            }

            if (ModuleFootprints.Any(existing => existing.Rect.Overlaps(footprint)))
            {
                ValidationLog.Add($"ERROR: module {module.Name} footprint overlaps existing module footprint. footprint={Format(footprint)}");
                continue;
            }

            List<GameObject> roots = sourceByModule[module.Name];
            GameObject placedRoot = Child(moduleRoot, module.Name);
            Quaternion rotation = Quaternion.Euler(0f, module.RotationY, 0f);
            foreach (GameObject source in roots)
            {
                if (source == null)
                    continue;

                GameObject clone = Object.Instantiate(source);
                clone.name = module.Name + "_" + source.name;
                clone.transform.SetParent(placedRoot.transform, true);

                Vector3 relative = source.transform.position - module.SourceBounds.center;
                relative = rotation * (relative * module.Scale);
                clone.transform.position = module.TargetCenter + new Vector3(relative.x, source.transform.position.y * 0.12f, relative.z);
                clone.transform.rotation = rotation * source.transform.rotation;
                clone.transform.localScale = source.transform.lossyScale * module.Scale;
                AlignBottomNearGround(clone);
            }

            ModuleFootprints.Add(new Zone(module.Name, footprint));
            ModuleRecords.Add(new ModuleRecord
            {
                name = module.Name,
                role = module.Role,
                sourceCenterX = module.SourceBounds.center.x,
                sourceCenterZ = module.SourceBounds.center.z,
                sourceWidth = module.SourceBounds.size.x,
                sourceDepth = module.SourceBounds.size.z,
                targetX = module.TargetCenter.x,
                targetZ = module.TargetCenter.z,
                rotationY = module.RotationY,
                scale = module.Scale,
                footprintWidth = module.Footprint.x,
                footprintDepth = module.Footprint.y,
                clonedRoots = roots.Count
            });
            BuildLog.Add($"{module.Name}: cloned {roots.Count} Demo roots into legal module footprint={Format(footprint)} role={module.Role} scale={module.Scale.ToString("0.##", CultureInfo.InvariantCulture)}");
        }
    }

    private static List<GameObject> CollectSourceRoots(ModuleSpec module)
    {
        Dictionary<GameObject, GameObject> roots = new();
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (transform == null || transform.gameObject.scene.path != DemoScenePath)
                continue;
            if (transform.GetComponent<Camera>() != null || transform.GetComponent<Light>() != null)
                continue;

            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0)
                continue;

            Bounds bounds = CalculateBounds(renderers);
            if (!module.SourceBounds.Intersects(bounds))
                continue;
            if (IsSkyOrHugeBackground(transform.name, bounds))
                continue;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject) ?? TopSceneObject(transform);
            if (root == null || root.GetComponent<Camera>() != null || root.GetComponent<Light>() != null)
                continue;

            Renderer[] rootRenderers = root.GetComponentsInChildren<Renderer>(false);
            if (rootRenderers.Length == 0)
                continue;
            Bounds rootBounds = CalculateBounds(rootRenderers);
            if (!module.SourceBounds.Intersects(rootBounds))
                continue;
            if (IsSkyOrHugeBackground(root.name, rootBounds))
                continue;
            if (IsInteriorTerrainBlocker(root, rootBounds))
            {
                rejectedInteriorTerrainRoots++;
                continue;
            }

            roots[root] = root;
        }

        List<GameObject> result = roots.Values
            .Where(go => !HasSelectedAncestor(go.transform, roots))
            .OrderBy(go => go.name, StringComparer.Ordinal)
            .ToList();

        if (result.Count == 0)
            RejectLog.Add($"{module.Name}: no source roots selected from Demo.");
        return result;
    }

    private static bool HasSelectedAncestor(Transform transform, Dictionary<GameObject, GameObject> selected)
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (selected.ContainsKey(parent.gameObject))
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

    private static void PlaceSoldierRouteProof(GameObject root)
    {
        GameObject units = Child(root, "Soldiers_OnValidatedWalkableStreets");
        Material blue = CreateMaterial("GC04_BlueRoute", new Color(0.04f, 0.16f, 0.95f, 1f));
        Material red = CreateMaterial("GC04_RedRoute", new Color(0.85f, 0.04f, 0.02f, 1f));
        Material yellow = CreateMaterial("GC04_RouteLine", new Color(0.95f, 0.75f, 0.05f, 1f));

        BuildRoute(units, "PlayerRoute_WalkableProof", new[]
        {
            new Vector3(-900f, 0f, -320f),
            new Vector3(-700f, 0f, -320f),
            new Vector3(-700f, 0f, 80f),
            new Vector3(-500f, 0f, 80f),
            new Vector3(-110f, 0f, 80f),
            new Vector3(-10f, 0f, 80f)
        }, yellow);

        BuildRoute(units, "EnemyRoute_WalkableProof", new[]
        {
            new Vector3(450f, 0f, 120f),
            new Vector3(230f, 0f, 120f),
            new Vector3(45f, 0f, 120f),
            new Vector3(0f, 0f, 80f)
        }, yellow);

        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab", new Vector3(-900f, 0f, -320f), 72f, blue, "player soldier 1");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab", new Vector3(-872f, 0f, -302f), 72f, blue, "player soldier 2");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_01.prefab", new Vector3(-872f, 0f, -338f), 72f, blue, "player soldier 3");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_02.prefab", new Vector3(-842f, 0f, -320f), 72f, blue, "player soldier 4");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab", new Vector3(450f, 0f, 120f), 252f, red, "enemy soldier 1");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_02.prefab", new Vector3(420f, 0f, 138f), 252f, red, "enemy soldier 2");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Female_01.prefab", new Vector3(420f, 0f, 102f), 252f, red, "enemy soldier 3");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_03.prefab", new Vector3(390f, 0f, 120f), 252f, red, "enemy soldier 4");
    }

    private static void BuildRoute(GameObject parent, string name, Vector3[] points, Material material)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 from = points[i];
            Vector3 to = points[i + 1];
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = $"{name}_{i:00}";
            segment.transform.SetParent(parent.transform, true);
            segment.transform.position = new Vector3((from.x + to.x) * 0.5f, 0.16f, (from.z + to.z) * 0.5f);
            segment.transform.localScale = new Vector3(8f, 0.05f, Vector3.Distance(from, to));
            segment.transform.rotation = Quaternion.LookRotation(to - from, Vector3.up);
            Object.DestroyImmediate(segment.GetComponent<Collider>());
            segment.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    private static void PlaceUnit(GameObject parent, string path, Vector3 position, float rotationY, Material ringMaterial, string label)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            RejectLog.Add("Missing proof unit: " + path);
            return;
        }

        GameObject unit = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        unit.name = label;
        unit.transform.SetParent(parent.transform, true);
        unit.transform.position = position;
        unit.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        unit.transform.localScale = Vector3.one * 1.1f;
        AlignBottomNearGround(unit);
        BuildSelectionRing(parent, label + "_Ring", new Vector3(unit.transform.position.x, 0.12f, unit.transform.position.z), 3.4f, ringMaterial);

        if (!IsPointOnRoad(position))
            ValidationLog.Add($"ERROR: {label} is not on a walkable road at {Format(position)}");
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
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void BuildCameras(GameObject root)
    {
        Camera map = CameraObject(root, "Camera_GC04_TopDownModulesWalkability");
        map.orthographic = true;
        map.orthographicSize = 1030f;
        map.transform.position = new Vector3(0f, 1400f, 0f);
        map.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        BuildRtsCamera(root, "Camera_GC04_RtsTownModules", new Vector3(-680f, 0f, -390f), new Vector3(-60f, 88f, -138f));
        BuildRtsCamera(root, "Camera_GC04_RtsTownMarketModules", new Vector3(-500f, 0f, 80f), new Vector3(-40f, 88f, -142f));
        BuildRtsCamera(root, "Camera_GC04_RtsBaseModules", new Vector3(520f, 0f, 120f), new Vector3(20f, 94f, -150f));
        BuildRtsCamera(root, "Camera_GC04_RtsIndustrialModules", new Vector3(560f, 0f, -760f), new Vector3(20f, 92f, -145f));
    }

    private static void BuildRtsCamera(GameObject root, string name, Vector3 target, Vector3 offset)
    {
        Camera camera = CameraObject(root, name);
        camera.orthographic = false;
        camera.fieldOfView = 40f;
        camera.transform.position = target + offset;
        camera.transform.LookAt(target);
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

    private static void NormalizeSceneLightBudget(GameObject root)
    {
        foreach (Light light in root.GetComponentsInChildren<Light>(true))
        {
            if (light.type == LightType.Directional)
                continue;

            light.shadows = LightShadows.None;
            light.intensity = Mathf.Min(light.intensity * 0.12f, 0.22f);
        }
    }

    private static void ValidateLayout()
    {
        foreach (Zone module in ModuleFootprints)
            foreach (Zone road in Roads)
                if (module.Rect.Overlaps(road.Rect))
                    ValidationLog.Add($"ERROR: module {module.Name} overlaps explicit walkable road {road.Name}");

        foreach (Zone spawn in Spawns)
            if (!Roads.Any(road => road.Rect.Overlaps(spawn.Rect)))
                ValidationLog.Add($"ERROR: spawn {spawn.Name} is not connected to a road.");

        foreach (Zone objective in Objectives)
            if (!Roads.Any(road => road.Rect.Overlaps(objective.Rect)))
                ValidationLog.Add($"ERROR: objective {objective.Name} is not connected to a road.");

        if (ModuleRecords.Count < 6)
            ValidationLog.Add($"ERROR: expected at least 6 Demo-authored modules, placed {ModuleRecords.Count}.");

        if (ValidationLog.Count == 0)
            ValidationLog.Add("PASS: GC04 placed Demo-authored modules into legal footprints; explicit walkable roads, spawns, objectives, and proof soldiers remain connected.");
    }

    private static void CaptureScene()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (camera.name == "Camera_GC04_TopDownModulesWalkability")
                Render(camera, ProjectPath(CaptureRoot + "/gc04_topdown_modules_walkability_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC04_RtsTownModules")
                Render(camera, ProjectPath(CaptureRoot + "/gc04_rts_town_modules_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC04_RtsTownMarketModules")
                Render(camera, ProjectPath(CaptureRoot + "/gc04_rts_town_market_modules_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC04_RtsBaseModules")
                Render(camera, ProjectPath(CaptureRoot + "/gc04_rts_base_modules_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC04_RtsIndustrialModules")
                Render(camera, ProjectPath(CaptureRoot + "/gc04_rts_industrial_modules_1920x1080.png"), 1920, 1080);
        }
    }

    private static void WriteModuleCatalog()
    {
        ModuleCatalogFile catalog = new()
        {
            generatedBy = nameof(WarlineCaptureGc04DemoModuleCityBuilder),
            sourceScene = DemoScenePath,
            generatedScene = ScenePath,
            modules = ModuleRecords
        };
        File.WriteAllText(ProjectPath(ModuleCatalogPath), JsonUtility.ToJson(catalog, true), Encoding.UTF8);
    }

    private static void WriteReport()
    {
        StringBuilder report = new();
        report.AppendLine("# GC04 Demo Module Playable City 2048");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Convert accepted Demo scene clusters into reusable city/base modules and place them around explicit GC03-style walkable roads.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGc04DemoModuleCityBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC04_DemoModulePlayableCity_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Data/GeneratedScenes/GC04_DemoModulePlayableCity_2048/gc04_demo_module_catalog.json`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC04_DemoModulePlayableCity_2048/gc04_topdown_modules_walkability_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC04_DemoModulePlayableCity_2048/gc04_rts_town_modules_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC04_DemoModulePlayableCity_2048/gc04_rts_town_market_modules_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC04_DemoModulePlayableCity_2048/gc04_rts_base_modules_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC04_DemoModulePlayableCity_2048/gc04_rts_industrial_modules_1920x1080.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: Gameplay playable scene generation workflow contract.");
        report.AppendLine("User-visible behavior: none in shipped flow; generated GC04 scene is available for visual/design review.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureGc04DemoModuleCityBuilder.BuildGc04DemoModulePlayableCity2048`.");
        report.AppendLine("Validation result: " + (ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)) ? "failed with module placement errors; see validation log below." : "passed Demo module placement and walkability validation."));
        report.AppendLine("Known gaps: modules are cloned from Demo bounds and flattened for grid gameplay, but they are not yet authored as reusable prefabs with designer-authored internal blocked/walkable masks.");
        report.AppendLine("Cross-lane impacts: PM/Design can review whether these Demo-derived modules are the right visual basis before we convert them into reusable prefabs.");
        report.AppendLine("Next recommended task: promote accepted GC04 modules into prefab assets with explicit internal blocked masks and module sockets for roads/objectives.");
        report.AppendLine();
        report.AppendLine($"Modules placed: {ModuleRecords.Count}");
        report.AppendLine($"Interior terrain/blocker roots rejected from playable modules: {rejectedInteriorTerrainRoots}");
        report.AppendLine();
        report.AppendLine("Validation log:");
        foreach (string line in ValidationLog)
            report.AppendLine("- " + line);
        report.AppendLine();
        report.AppendLine("Module log:");
        foreach (string line in BuildLog)
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

    private static bool IsPointOnRoad(Vector3 position)
    {
        Vector2 point = new(position.x, position.z);
        return Roads.Any(road => road.Rect.Contains(point));
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
        string lower = root.name.ToLowerInvariant();
        if (lower.Contains("terrain") || lower.Contains("mountain") || lower.Contains("sanddune") || lower.Contains("dune"))
            return true;
        return bounds.size.x > 170f && bounds.size.z > 170f && bounds.size.y < 30f;
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

    private static Rect CenterRect(float centerX, float centerZ, float width, float depth)
    {
        return new Rect(centerX - width * 0.5f, centerZ - depth * 0.5f, width, depth);
    }

    private static Rect FootprintRect(Vector3 position, Vector2 size)
    {
        return CenterRect(position.x, position.z, size.x, size.y);
    }

    private static Vector3 Center(Rect rect, float y)
    {
        return new Vector3(rect.center.x, y, rect.center.y);
    }

    private static void Surface(GameObject parent, string name, Vector3 position, Vector2 size, Material material, float y)
    {
        GameObject surface = new(name);
        surface.transform.SetParent(parent.transform, false);
        surface.transform.position = new Vector3(position.x, y, position.z);
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
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        surface.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Color");
        Material material = new(shader)
        {
            name = name,
            color = color
        };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent.transform, false);
        return child;
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

    private static string Format(Rect rect)
    {
        return $"center=({rect.center.x:0.##}, {rect.center.y:0.##}) size=({rect.width:0.##}, {rect.height:0.##})";
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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

public static class WarlineCaptureGc05ModulePrefabPromoter
{
    private const string SourceScenePath = "Assets/Game/Scenes/Generated/GC04_DemoModulePlayableCity_2048.unity";
    private const string PrefabRootPath = "Assets/Game/Prefabs/Generated/GC04Modules";
    private const string PreviewScenePath = "Assets/Game/Scenes/Generated/GC05_ModulePrefabPreview_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC05_ModulePrefabPreview_2048";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GC05_ModulePrefabPreview_2048";
    private const string CatalogPath = DataRoot + "/gc05_promoted_module_prefab_catalog.json";
    private const string ReportPath = "Design/AgentReports/2026-05-20_gameplay_gc05-module-prefab-promotion.md";
    private const float MapSize = 2048f;

    private static readonly List<ModuleInfo> Modules = new();
    private static readonly List<string> ValidationLog = new();
    private static readonly List<string> BuildLog = new();

    private sealed class ModuleInfo
    {
        public string Name;
        public string Role;
        public Vector3 Center;
        public Vector2 Footprint;
        public string PrefabPath;
        public int DirectChildren;
        public int RendererCount;
    }

    private readonly struct AcceptedModulePlacement
    {
        public readonly string Role;
        public readonly Vector3 Center;
        public readonly Vector2 Footprint;

        public AcceptedModulePlacement(string role, Vector3 center, Vector2 footprint)
        {
            Role = role;
            Center = center;
            Footprint = footprint;
        }
    }

    [Serializable]
    private sealed class ModuleRecord
    {
        public string name;
        public string role;
        public string prefabPath;
        public float footprintWidth;
        public float footprintDepth;
        public int directChildren;
        public int rendererCount;
        public string[] sockets;
        public string[] masks;
    }

    [Serializable]
    private sealed class ModuleCatalog
    {
        public string generatedBy;
        public string sourceScene;
        public string previewScene;
        public List<ModuleRecord> modules = new();
    }

    [MenuItem("WarlineCapture/Design/Promote GC04 Modules To Prefabs")]
    public static void PromoteGc04ModulesToPrefabs()
    {
        Modules.Clear();
        ValidationLog.Clear();
        BuildLog.Clear();

        Directory.CreateDirectory(ProjectPath(PrefabRootPath));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        Directory.CreateDirectory(ProjectPath(DataRoot));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(PreviewScenePath)));

        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        Transform modulesRoot = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .FirstOrDefault(t => t.name == "DemoAuthoredPlayableModules");

        if (modulesRoot == null)
        {
            ValidationLog.Add("ERROR: DemoAuthoredPlayableModules root was not found in GC04 scene.");
            WriteReport();
            EditorApplication.Exit(1);
            return;
        }

        foreach (Transform sourceModule in modulesRoot)
        {
            ModuleInfo info = BuildModuleInfo(sourceModule);
            if (info == null)
                continue;

            PromoteModule(sourceModule, info);
            Modules.Add(info);
        }

        EditorSceneManager.CloseScene(sourceScene, true);
        BuildPreviewScene();
        WriteCatalog();
        ValidatePromotion();
        CaptureScene();
        WriteReport();
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_GC05_MODULE_PREFABS_PROMOTED count={Modules.Count} prefabRoot={PrefabRootPath} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static ModuleInfo BuildModuleInfo(Transform moduleRoot)
    {
        Renderer[] renderers = moduleRoot.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
        {
            ValidationLog.Add($"ERROR: module {moduleRoot.name} has no renderers.");
            return null;
        }

        Dictionary<string, AcceptedModulePlacement> placements = AcceptedModulePlacements();
        if (!placements.TryGetValue(moduleRoot.name, out AcceptedModulePlacement placement))
        {
            ValidationLog.Add($"ERROR: module {moduleRoot.name} was not found in accepted GC04 placement contract.");
            return null;
        }

        return new ModuleInfo
        {
            Name = moduleRoot.name,
            Role = placement.Role,
            Center = placement.Center,
            Footprint = placement.Footprint,
            PrefabPath = $"{PrefabRootPath}/{moduleRoot.name}.prefab",
            DirectChildren = moduleRoot.childCount,
            RendererCount = renderers.Length
        };
    }

    private static Dictionary<string, AcceptedModulePlacement> AcceptedModulePlacements()
    {
        return new Dictionary<string, AcceptedModulePlacement>(StringComparer.Ordinal)
        {
            ["TownBlock_SW_DemoAuthored"] = new("town", new Vector3(-800f, 0f, -620f), new Vector2(145.6f, 145.6f)),
            ["TownBlock_SouthCenter_DemoAuthored"] = new("town", new Vector3(-600f, 0f, -620f), new Vector2(136.8f, 136.8f)),
            ["TownBlock_SouthEast_DemoAuthored"] = new("town", new Vector3(-400f, 0f, -620f), new Vector2(136.8f, 136.8f)),
            ["TownBlock_WestMid_DemoAuthored"] = new("town", new Vector3(-800f, 0f, -220f), new Vector2(141.2f, 141.2f)),
            ["TownBlock_Center_DemoAuthored"] = new("town", new Vector3(-600f, 0f, -220f), new Vector2(141.2f, 141.2f)),
            ["TownMarket_DemoAuthored"] = new("town", new Vector3(-400f, 0f, 180f), new Vector2(150f, 150f)),
            ["TownBlock_WestMarket_DemoAuthored"] = new("town", new Vector3(-800f, 0f, 180f), new Vector2(136.8f, 136.8f)),
            ["TownBlock_NorthCenter_DemoAuthored"] = new("town", new Vector3(-600f, 0f, 580f), new Vector2(136.8f, 136.8f)),
            ["TownNorth_DemoAuthored"] = new("town", new Vector3(-800f, 0f, 580f), new Vector2(82.8f, 154.8f)),
            ["BaseBarracks_DemoAuthored"] = new("base", new Vector3(340f, 0f, -260f), new Vector2(114.2f, 121.6f)),
            ["BaseMotorPool_DemoAuthored"] = new("base", new Vector3(760f, 0f, -260f), new Vector2(109f, 116f)),
            ["BaseSouthDepot_DemoAuthored"] = new("base", new Vector3(340f, 0f, -740f), new Vector2(103.8f, 110.4f)),
            ["BaseCommand_DemoAuthored"] = new("base", new Vector3(560f, 0f, 240f), new Vector2(111.6f, 118.8f)),
            ["BaseNorthDepot_DemoAuthored"] = new("base", new Vector3(340f, 0f, 500f), new Vector2(106.4f, 113.2f)),
            ["RunwayApron_DemoAuthored"] = new("base", new Vector3(780f, 0f, 500f), new Vector2(61.4f, 154.4f)),
            ["IndustrialObjective_DemoAuthored"] = new("industrial", new Vector3(560f, 0f, -760f), new Vector2(130f, 114f)),
        };
    }

    private static void PromoteModule(Transform sourceModule, ModuleInfo info)
    {
        GameObject prefabRoot = new(info.Name);
        GameObject artRoot = Child(prefabRoot, "Art");

        foreach (Transform child in sourceModule)
        {
            GameObject clone = Object.Instantiate(child.gameObject);
            clone.name = child.name;
            clone.transform.SetParent(artRoot.transform, false);
            clone.transform.localPosition = new Vector3(child.position.x - info.Center.x, child.position.y, child.position.z - info.Center.z);
            clone.transform.rotation = child.rotation;
            clone.transform.localScale = child.lossyScale;
        }

        AddModuleMarkers(prefabRoot, info);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, info.PrefabPath);
        Object.DestroyImmediate(prefabRoot);
        BuildLog.Add($"{info.Name}: promoted to {info.PrefabPath} footprint=({info.Footprint.x:0.#}, {info.Footprint.y:0.#}) children={info.DirectChildren} renderers={info.RendererCount}");
    }

    private static void AddModuleMarkers(GameObject root, ModuleInfo info)
    {
        GameObject metadata = Child(root, "ModuleMetadata_DoNotRender");
        metadata.hideFlags = HideFlags.NotEditable;

        Empty(metadata, "Socket_North_Road", new Vector3(0f, 0f, info.Footprint.y * 0.5f));
        Empty(metadata, "Socket_South_Road", new Vector3(0f, 0f, -info.Footprint.y * 0.5f));
        Empty(metadata, "Socket_East_Road", new Vector3(info.Footprint.x * 0.5f, 0f, 0f));
        Empty(metadata, "Socket_West_Road", new Vector3(-info.Footprint.x * 0.5f, 0f, 0f));
        Empty(metadata, "ObjectiveSocket_Center", Vector3.zero);

        GameObject masks = Child(root, "Masks_DoNotRender");
        Empty(masks, "BlockedFootprintMask", Vector3.zero);
        Empty(masks, "WalkableSocketMask", Vector3.zero);
    }

    private static void BuildPreviewScene()
    {
        Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SetActiveScene(preview);
        GameObject root = new("GC05_ModulePrefabPreview_2048_Root");
        BuildRenderEnvironment(root);
        BuildBasePlane(root);
        BuildPreviewRoads(root);

        GameObject moduleRoot = Child(root, "PromotedModulePrefabInstances");
        foreach (ModuleInfo info in Modules)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(info.PrefabPath);
            if (prefab == null)
            {
                ValidationLog.Add("ERROR: missing promoted prefab " + info.PrefabPath);
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(moduleRoot.transform, true);
            instance.transform.position = info.Center;
            BuildLog.Add($"{info.Name}: preview instance placed at ({info.Center.x:0.#}, {info.Center.z:0.#})");
        }

        BuildCameras(root);
        EditorSceneManager.SaveScene(preview, PreviewScenePath);
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
        key.transform.rotation = Quaternion.Euler(50f, -42f, 0f);

        Volume volume = Child(root, "GC05_RTS_PresentationVolume").AddComponent<Volume>();
        volume.isGlobal = true;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "GC05_RTS_PresentationProfile";
        volume.sharedProfile = profile;
        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.1f);
        color.contrast.Override(14f);
        color.saturation.Override(4f);
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
    }

    private static void BuildBasePlane(GameObject root)
    {
        Surface(root, "FlatGameplayPlane_2048x2048", Vector3.zero, new Vector2(MapSize, MapSize), CreateMaterial("GC05_SandBase", new Color(0.55f, 0.45f, 0.29f, 1f)), 0f);
    }

    private static void BuildPreviewRoads(GameObject root)
    {
        GameObject roads = Child(root, "PreviewRoads_FromGC04");
        Material road = CreateMaterial("GC05_Road", new Color(0.13f, 0.16f, 0.14f, 1f));
        Material shoulder = CreateMaterial("GC05_RoadShoulder", new Color(0.68f, 0.56f, 0.35f, 1f));
        foreach ((string name, float x, float z, float w, float d) in RoadSpecs())
        {
            Surface(roads, name + "_Shoulder", new Vector3(x, 0.045f, z), new Vector2(w + 18f, d + 18f), shoulder, 0.045f);
            Surface(roads, name, new Vector3(x, 0.055f, z), new Vector2(w, d), road, 0.055f);
        }
    }

    private static IEnumerable<(string name, float x, float z, float w, float d)> RoadSpecs()
    {
        yield return ("MainHighway", 0f, 0f, 92f, 1920f);
        foreach (float x in new[] { -900f, -700f, -500f, -300f, -110f })
            yield return ($"TownVertical_{x:0}", x, 0f, 36f, 1560f);
        foreach (float z in new[] { -720f, -520f, -320f, -120f, 80f, 280f, 480f, 680f })
            yield return ($"TownHorizontal_{z:0}", -505f, z, 850f, 38f);
        yield return ("TownHighwayConnector_North", -145f, 480f, 260f, 38f);
        yield return ("TownHighwayConnector_Market", -145f, 80f, 260f, 38f);
        yield return ("TownHighwayConnector_South", -145f, -320f, 260f, 38f);
        foreach (float x in new[] { 230f, 450f, 670f, 850f })
            yield return ($"BaseVertical_{x:0}", x, 40f, 36f, 1420f);
        foreach (float z in new[] { -620f, -380f, -140f, 120f, 360f, 600f })
            yield return ($"BaseHorizontal_{z:0}", 535f, z, 690f, 38f);
        yield return ("BaseHighwayConnector_North", 145f, 360f, 260f, 38f);
        yield return ("BaseHighwayConnector_Gate", 145f, 120f, 260f, 38f);
        yield return ("IndustrialHighwayConnector", 145f, -380f, 260f, 38f);
    }

    private static void BuildCameras(GameObject root)
    {
        Camera top = CameraObject(root, "Camera_GC05_TopDownPrefabPreview");
        top.orthographic = true;
        top.orthographicSize = 1030f;
        top.transform.position = new Vector3(0f, 1400f, 0f);
        top.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        BuildRtsCamera(root, "Camera_GC05_RtsPrefabTownReview", new Vector3(-650f, 0f, -250f), new Vector3(-50f, 88f, -138f));
        BuildRtsCamera(root, "Camera_GC05_RtsPrefabBaseReview", new Vector3(560f, 0f, 120f), new Vector3(20f, 94f, -150f));
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

    private static void ValidatePromotion()
    {
        if (Modules.Count < 16)
            ValidationLog.Add($"ERROR: expected 16 promoted modules, promoted {Modules.Count}.");

        foreach (ModuleInfo info in Modules)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(info.PrefabPath);
            if (prefab == null)
            {
                ValidationLog.Add("ERROR: missing promoted prefab " + info.PrefabPath);
                continue;
            }
            if (prefab.transform.Find("ModuleMetadata_DoNotRender/Socket_North_Road") == null ||
                prefab.transform.Find("Masks_DoNotRender/BlockedFootprintMask") == null)
                ValidationLog.Add("ERROR: promoted prefab is missing socket or mask metadata: " + info.PrefabPath);
        }

        if (ValidationLog.Count == 0)
            ValidationLog.Add("PASS: GC05 promoted all GC04 modules into reusable prefabs with socket and mask marker children.");
    }

    private static void CaptureScene()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (camera.name == "Camera_GC05_TopDownPrefabPreview")
                Render(camera, ProjectPath(CaptureRoot + "/gc05_topdown_prefab_preview_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC05_RtsPrefabTownReview")
                Render(camera, ProjectPath(CaptureRoot + "/gc05_rts_prefab_town_review_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC05_RtsPrefabBaseReview")
                Render(camera, ProjectPath(CaptureRoot + "/gc05_rts_prefab_base_review_1920x1080.png"), 1920, 1080);
        }
    }

    private static void WriteCatalog()
    {
        ModuleCatalog catalog = new()
        {
            generatedBy = nameof(WarlineCaptureGc05ModulePrefabPromoter),
            sourceScene = SourceScenePath,
            previewScene = PreviewScenePath,
            modules = Modules.Select(info => new ModuleRecord
            {
                name = info.Name,
                role = info.Role,
                prefabPath = info.PrefabPath,
                footprintWidth = info.Footprint.x,
                footprintDepth = info.Footprint.y,
                directChildren = info.DirectChildren,
                rendererCount = info.RendererCount,
                sockets = new[] { "Socket_North_Road", "Socket_South_Road", "Socket_East_Road", "Socket_West_Road", "ObjectiveSocket_Center" },
                masks = new[] { "BlockedFootprintMask", "WalkableSocketMask" }
            }).ToList()
        };
        File.WriteAllText(ProjectPath(CatalogPath), JsonUtility.ToJson(catalog, true), Encoding.UTF8);
    }

    private static void WriteReport()
    {
        StringBuilder report = new();
        report.AppendLine("# GC05 Module Prefab Promotion");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Promote accepted GC04 Demo-authored modules into reusable prefab assets with socket and mask marker contracts.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGc05ModulePrefabPromoter.cs`");
        report.AppendLine("- `Assets/Game/Prefabs/Generated/GC04Modules/*.prefab`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC05_ModulePrefabPreview_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Data/GeneratedScenes/GC05_ModulePrefabPreview_2048/gc05_promoted_module_prefab_catalog.json`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC05_ModulePrefabPreview_2048/gc05_topdown_prefab_preview_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC05_ModulePrefabPreview_2048/gc05_rts_prefab_town_review_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC05_ModulePrefabPreview_2048/gc05_rts_prefab_base_review_1920x1080.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: Gameplay playable scene generation workflow contract; GC05 introduces prefab-level socket/mask marker names for generated city modules.");
        report.AppendLine("User-visible behavior: none in shipped flow; generated prefabs and preview scene are available for design review.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureGc05ModulePrefabPromoter.PromoteGc04ModulesToPrefabs`.");
        report.AppendLine("Validation result: " + (ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)) ? "failed; see validation log below." : "passed prefab promotion validation."));
        report.AppendLine("Known gaps: marker children are contract placeholders; the next implementation pass should convert them to real ECS/grid authoring data or ScriptableObject module definitions.");
        report.AppendLine("Cross-lane impacts: PM/Design can review promoted modules; runtime ECS/game flow is untouched.");
        report.AppendLine("Next recommended task: build a module placement authoring asset that consumes these prefabs, sockets, and masks instead of reading generated scene geometry.");
        report.AppendLine();
        report.AppendLine($"Modules promoted: {Modules.Count}");
        report.AppendLine();
        report.AppendLine("Validation log:");
        foreach (string line in ValidationLog)
            report.AppendLine("- " + line);
        report.AppendLine();
        report.AppendLine("Promotion log:");
        foreach (string line in BuildLog)
            report.AppendLine("- " + line);
        File.WriteAllText(ProjectPath(ReportPath), report.ToString(), Encoding.UTF8);
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static GameObject Empty(GameObject parent, string name, Vector3 localPosition)
    {
        GameObject child = Child(parent, name);
        child.transform.localPosition = localPosition;
        return child;
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent.transform, false);
        return child;
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
}
#endif

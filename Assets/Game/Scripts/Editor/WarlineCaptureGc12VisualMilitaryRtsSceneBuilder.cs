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

public static class WarlineCaptureGc12VisualMilitaryRtsSceneBuilder
{
    private const string DemoScenePath = "Assets/Game/Scenes/Demo.unity";
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC12_VisualMilitaryRts_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC12_VisualMilitaryRts_2048";
    private const string ReportPath = "Design/AgentReports/2026-05-20_gameplay_gc12-visual-military-rts-scene.md";
    private const float DefaultScale = 3.0f;
    private static readonly List<string> ValidationLog = new();
    private static readonly List<string> BuildLog = new();

    private readonly struct ModuleSpec
    {
        public readonly string Name;
        public readonly Bounds SourceBounds;
        public readonly Vector3 TargetCenter;
        public readonly float RotationY;
        public readonly float Scale;

        public ModuleSpec(string name, Bounds sourceBounds, Vector3 targetCenter, float rotationY, float scale)
        {
            Name = name;
            SourceBounds = sourceBounds;
            TargetCenter = targetCenter;
            RotationY = rotationY;
            Scale = scale;
        }
    }

    public static void BuildGc12VisualMilitaryRts2048()
    {
        ValidationLog.Clear();
        BuildLog.Clear();
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));

        Scene demoScene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        List<ModuleSpec> modules = BuildModuleSpecs();
        Dictionary<string, List<GameObject>> sourceByModule = new(StringComparer.Ordinal);
        foreach (ModuleSpec module in modules)
            sourceByModule[module.Name] = CollectSourceRoots(module.SourceBounds);

        Scene generatedScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(generatedScene);

        GameObject root = new("GC12_VisualMilitaryRts_2048_Root");
        BuildEnvironment(root);
        BuildBasePlane(root);
        BuildPlannedRoads(root);
        CloneModules(root, modules, sourceByModule);
        PlaceProofSoldiers(root);
        BuildCameras(root);
        int sourceRootCount = sourceByModule.Values.Sum(list => list.Count);
        Validate(sourceRootCount);

        EditorSceneManager.CloseScene(demoScene, true);
        EditorSceneManager.SaveScene(generatedScene, ScenePath);
        CaptureScene();
        WriteReport(sourceRootCount);
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GC12_VISUAL_MILITARY_RTS_BUILT sourceRoots={sourceRootCount} scene={ScenePath} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static List<ModuleSpec> BuildModuleSpecs()
    {
        return new List<ModuleSpec>
        {
            new("NorthRunway_AircraftApron", new Bounds(new Vector3(82f, 18f, 205f), new Vector3(115f, 120f, 180f)), new Vector3(420f, 0f, 430f), -18f, 2.65f),
            new("WestTentCamp", new Bounds(new Vector3(20f, 12f, 76f), new Vector3(135f, 105f, 135f)), new Vector3(-405f, 0f, 285f), 8f, 2.9f),
            new("CentralCommandDepot", new Bounds(new Vector3(35f, 12f, 118f), new Vector3(150f, 105f, 135f)), new Vector3(-20f, 0f, 95f), -28f, 2.85f),
            new("SouthVehicleYard", new Bounds(new Vector3(42f, 10f, 80f), new Vector3(145f, 105f, 110f)), new Vector3(300f, 0f, -310f), 142f, 2.7f),
            new("EastFuelUtility", new Bounds(new Vector3(82f, 14f, 420f), new Vector3(230f, 120f, 250f)), new Vector3(640f, 0f, -80f), -70f, 2.25f),
            new("SouthGateCheckpoint", new Bounds(new Vector3(-15f, 10f, -72f), new Vector3(180f, 95f, 145f)), new Vector3(-350f, 0f, -450f), 38f, 2.55f),
            new("VillageForwardOutpost", new Bounds(new Vector3(-38f, 16f, -58f), new Vector3(220f, 90f, 220f)), new Vector3(-690f, 0f, -95f), 16f, 1.55f),
        };
    }

    private static List<GameObject> CollectSourceRoots(Bounds sourceBounds)
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
            if (!sourceBounds.Intersects(bounds) || IsHugeBackground(transform.name, bounds) || IsTerrainOrHillSource(transform.name, bounds))
                continue;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject) ?? TopSceneObject(transform);
            if (root == null)
                continue;

            Renderer[] rootRenderers = root.GetComponentsInChildren<Renderer>(false);
            if (rootRenderers.Length == 0)
                continue;

            Bounds rootBounds = CalculateBounds(rootRenderers);
            if (!sourceBounds.Intersects(rootBounds) || IsHugeBackground(root.name, rootBounds) || IsTerrainOrHillSource(root.name, rootBounds))
                continue;

            roots[root.GetInstanceID()] = root;
        }

        return roots.Values
            .Where(go => !HasSelectedAncestor(go.transform, roots))
            .OrderBy(go => go.name, StringComparer.Ordinal)
            .ToList();
    }

    private static void CloneModules(GameObject root, List<ModuleSpec> modules, Dictionary<string, List<GameObject>> sourceByModule)
    {
        GameObject cloneRoot = Child(root, "PlannedMilitaryDistricts_RearrangedDemoModules");
        foreach (ModuleSpec module in modules)
        {
            GameObject moduleRoot = Child(cloneRoot, module.Name);
            Quaternion rotation = Quaternion.Euler(0f, module.RotationY, 0f);
            foreach (GameObject source in sourceByModule[module.Name])
            {
                GameObject clone = Object.Instantiate(source);
                clone.name = "GC12_" + module.Name + "_" + source.name;
                clone.transform.SetParent(moduleRoot.transform, true);
                Vector3 relative = source.transform.position - module.SourceBounds.center;
                relative = rotation * (relative * module.Scale);
                clone.transform.position = module.TargetCenter + new Vector3(relative.x, source.transform.position.y * 0.32f, relative.z);
                clone.transform.rotation = rotation * source.transform.rotation;
                clone.transform.localScale = source.transform.lossyScale * module.Scale;
                AlignBottomNearGround(clone);
            }

            BuildLog.Add($"{module.Name}: cloned {sourceByModule[module.Name].Count} roots to {module.TargetCenter} rotation={module.RotationY:0.#} scale={module.Scale:0.##}");
        }
    }

    private static void BuildEnvironment(GameObject root)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.88f, 0.80f, 0.66f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.55f, 0.47f, 0.36f, 1f);
        RenderSettings.fogDensity = 0.00055f;

        Light key = Child(root, "DirectionalLight_Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 2.1f;
        key.color = new Color(1f, 0.88f, 0.66f, 1f);
        key.shadows = LightShadows.None;
        key.transform.rotation = Quaternion.Euler(50f, -42f, 0f);

        Light fill = Child(root, "DirectionalLight_Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.75f;
        fill.color = new Color(0.74f, 0.82f, 1f, 1f);
        fill.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(58f, 138f, 0f);
    }

    private static void BuildBasePlane(GameObject root)
    {
        Surface(root, "FlatPlayableSand_2048", Vector3.zero, new Vector2(2048f, 2048f), CreateMaterial("GC12_SandBase", new Color(0.62f, 0.50f, 0.30f, 1f)), -0.03f);
    }

    private static void BuildPlannedRoads(GameObject root)
    {
        GameObject roads = Child(root, "ReadableWalkableRoads_Visual");
        Material mainRoad = CreateMaterial("GC12_MainRoad_CompactedSand", new Color(0.48f, 0.40f, 0.27f, 1f));
        Material lane = CreateMaterial("GC12_ServiceLane_Dust", new Color(0.68f, 0.56f, 0.34f, 1f));
        Surface(roads, "MainSouthNorthRoad", new Vector3(-175f, 0.025f, -65f), new Vector2(86f, 1320f), mainRoad, 0.025f);
        Surface(roads, "CentralEastWestRoad", new Vector3(95f, 0.027f, 30f), new Vector2(960f, 62f), mainRoad, 0.027f);
        Surface(roads, "RunwayAccessRoad", new Vector3(390f, 0.029f, 250f), new Vector2(510f, 56f), lane, 0.029f);
        Surface(roads, "VehicleDepotRoad", new Vector3(205f, 0.031f, -300f), new Vector2(740f, 58f), lane, 0.031f);
        Surface(roads, "TentCampRoad", new Vector3(-440f, 0.033f, 250f), new Vector2(420f, 52f), lane, 0.033f);
        Surface(roads, "SouthGateEntryRoad", new Vector3(-360f, 0.035f, -490f), new Vector2(520f, 58f), mainRoad, 0.035f);
    }

    private static void PlaceProofSoldiers(GameObject root)
    {
        GameObject units = Child(root, "ProofSoldiers_OnFlatPlayableLanes");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab", new Vector3(-410f, 0f, -490f), 42f, "PlayerSoldier_01");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab", new Vector3(-365f, 0f, -464f), 42f, "PlayerSoldier_02");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_01.prefab", new Vector3(-320f, 0f, -438f), 42f, "PlayerSoldier_03");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_02.prefab", new Vector3(-175f, 0f, -105f), 8f, "PatrolSoldier_01");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab", new Vector3(390f, 0f, 250f), 222f, "EnemySoldier_01");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_02.prefab", new Vector3(350f, 0f, 224f), 222f, "EnemySoldier_02");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Female_01.prefab", new Vector3(310f, 0f, 198f), 222f, "EnemySoldier_03");
    }

    private static void PlaceUnit(GameObject parent, string path, Vector3 position, float rotationY, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            ValidationLog.Add("ERROR: missing unit prefab " + path);
            return;
        }

        GameObject unit = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        unit.name = name;
        unit.transform.SetParent(parent.transform, true);
        unit.transform.position = position;
        unit.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        unit.transform.localScale = Vector3.one * 1.5f;
        AlignBottomNearGround(unit);
    }

    private static void BuildCameras(GameObject root)
    {
        BuildCamera(root, "Camera_GC12_TargetStyleOverview", new Vector3(-370f, 690f, -760f), new Vector3(20f, 0f, -20f), 35f);
        BuildCamera(root, "Camera_GC12_PlayableBaseClose", new Vector3(-250f, 455f, -520f), new Vector3(-70f, 0f, -70f), 35f);
        BuildCamera(root, "Camera_GC12_RunwayVehicleClose", new Vector3(120f, 640f, -720f), new Vector3(405f, 0f, 210f), 32f);
        BuildTopDownCamera(root, "Camera_GC12_TopDownBlueprintCompare");
    }

    private static void BuildCamera(GameObject root, string name, Vector3 position, Vector3 target, float fov)
    {
        GameObject cameraObject = Child(root, name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.52f, 0.42f, 0.28f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 4000f;
        camera.fieldOfView = fov;
        camera.transform.position = position;
        camera.transform.LookAt(target);
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = false;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
    }

    private static void BuildTopDownCamera(GameObject root, string name)
    {
        GameObject cameraObject = Child(root, name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.52f, 0.42f, 0.28f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 3000f;
        camera.orthographic = true;
        camera.orthographicSize = 1080f;
        camera.transform.position = new Vector3(0f, 1800f, 0f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = false;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
    }

    private static void Validate(int sourceRootCount)
    {
        if (sourceRootCount < 500)
            ValidationLog.Add($"ERROR: expected dense Demo military source roots, found {sourceRootCount}.");
        if (ValidationLog.Count == 0)
            ValidationLog.Add("PASS: GC12 cloned dense Demo-authored military/town modules into a visual RTS review scene with readable roads and proof units.");
    }

    private static void CaptureScene()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (camera.name == "Camera_GC12_TargetStyleOverview")
                Render(camera, ProjectPath(CaptureRoot + "/gc12_target_style_overview_1920x1080.png"));
            if (camera.name == "Camera_GC12_PlayableBaseClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc12_playable_base_close_1920x1080.png"));
            if (camera.name == "Camera_GC12_RunwayVehicleClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc12_runway_vehicle_close_1920x1080.png"));
            if (camera.name == "Camera_GC12_TopDownBlueprintCompare")
                Render(camera, ProjectPath(CaptureRoot + "/gc12_topdown_blueprint_compare_2048x2048.png"), 2048, 2048);
        }
    }

    private static void WriteReport(int sourceRootCount)
    {
        StringBuilder report = new();
        report.AppendLine("# GC12 Visual Military RTS Demo-Cluster Scene");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Build a Demo-quality visual pass for the expanded 2048 RTS scene by cloning composed Demo scene modules into a planned RTS layout while keeping roads and proof units readable.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGc12VisualMilitaryRtsSceneBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC12_VisualMilitaryRts_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC12_VisualMilitaryRts_2048/gc12_target_style_overview_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC12_VisualMilitaryRts_2048/gc12_playable_base_close_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC12_VisualMilitaryRts_2048/gc12_runway_vehicle_close_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC12_VisualMilitaryRts_2048/gc12_topdown_blueprint_compare_2048x2048.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: GC11 expanded blueprint visual pass; road/walkable mask remains the gameplay contract, Demo clusters are visual dressing.");
        report.AppendLine("User-visible behavior: none in shipped flow; generated scene is available for visual review.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureGc12VisualMilitaryRtsSceneBuilder.BuildGc12VisualMilitaryRts2048`.");
        report.AppendLine("Validation result: " + (ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)) ? "failed; see log below." : "passed visual scene generation validation."));
        report.AppendLine("Known gaps: visual/playable basis only; no ECS conversion yet. This pass uses Demo-authored clusters for visual quality and still needs exact reconciliation against the GC11 road/walkable mask before gameplay integration.");
        report.AppendLine("Cross-lane impacts: PM/Design can review whether the Demo-cluster direction is visually acceptable before Gameplay locks the walkable layout.");
        report.AppendLine("Next recommended task: reconcile the accepted visual clusters to the exact GC11 walkable mask, then trim or replace any cluster that violates lanes.");
        report.AppendLine();
        report.AppendLine($"Source roots cloned: {sourceRootCount}");
        report.AppendLine();
        report.AppendLine("Validation log:");
        foreach (string line in ValidationLog)
            report.AppendLine("- " + line);
        report.AppendLine();
        report.AppendLine("Build log:");
        foreach (string line in BuildLog)
            report.AppendLine("- " + line);
        File.WriteAllText(ProjectPath(ReportPath), report.ToString(), Encoding.UTF8);
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

    private static bool IsHugeBackground(string name, Bounds bounds)
    {
        string lower = name.ToLowerInvariant();
        return lower.Contains("sky") || lower.Contains("cloud") || lower.Contains("skydome") ||
            bounds.center.y > 500f || bounds.size.x > 900f || bounds.size.z > 900f;
    }

    private static bool IsTerrainOrHillSource(string name, Bounds bounds)
    {
        string lower = name.ToLowerInvariant();
        if (lower.Contains("terrain") || lower.Contains("mountain") || lower.Contains("sanddune") || lower.Contains("dune"))
            return true;
        return bounds.size.x > 130f && bounds.size.z > 130f && bounds.size.y < 35f;
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

    private static void Surface(GameObject parent, string name, Vector3 position, Vector2 size, Material material, float y)
    {
        GameObject surface = new(name);
        surface.transform.SetParent(parent.transform, false);
        surface.transform.position = new Vector3(position.x, y, position.z);
        Mesh mesh = new();
        float halfX = size.x * 0.5f;
        float halfZ = size.y * 0.5f;
        mesh.vertices = new[] { new Vector3(-halfX, 0f, -halfZ), new Vector3(-halfX, 0f, halfZ), new Vector3(halfX, 0f, halfZ), new Vector3(halfX, 0f, -halfZ) };
        mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        surface.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material material = new(shader) { name = name, color = color };
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

    private static void Render(Camera camera, string path)
    {
        Render(camera, path, 1920, 1080);
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

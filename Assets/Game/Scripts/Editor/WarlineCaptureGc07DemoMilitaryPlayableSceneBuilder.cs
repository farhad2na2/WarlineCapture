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

public static class WarlineCaptureGc07DemoMilitaryPlayableSceneBuilder
{
    private const string DemoScenePath = "Assets/Game/Scenes/Demo.unity";
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC07_DemoMilitaryPlayable_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC07_DemoMilitaryPlayable_2048";
    private const string ReportPath = "Design/AgentReports/2026-05-20_gameplay_gc07-demo-military-playable-scene.md";
    private const float Scale = 3.15f;
    private static readonly List<string> ValidationLog = new();

    public static void BuildGc07DemoMilitaryPlayableScene()
    {
        ValidationLog.Clear();
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));

        Scene demoScene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        Bounds sourceBounds = new(new Vector3(36f, 20f, 105f), new Vector3(250f, 170f, 470f));
        List<GameObject> sourceRoots = CollectSourceRoots(sourceBounds);

        Scene generatedScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(generatedScene);

        GameObject root = new("GC07_DemoMilitaryPlayable_2048_Root");
        BuildEnvironment(root);
        BuildBasePlane(root);
        CloneSourceRoots(root, sourceRoots, sourceBounds.center);
        PlaceProofSoldiers(root);
        BuildCameras(root);
        Validate(sourceRoots.Count);

        EditorSceneManager.CloseScene(demoScene, true);
        EditorSceneManager.SaveScene(generatedScene, ScenePath);
        CaptureScene();
        WriteReport(sourceRoots.Count);
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GC07_DEMO_MILITARY_PLAYABLE_BUILT sourceRoots={sourceRoots.Count} scene={ScenePath} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static List<GameObject> CollectSourceRoots(Bounds sourceBounds)
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
            if (!sourceBounds.Intersects(bounds) || IsHugeBackground(transform.name, bounds))
                continue;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject) ?? TopSceneObject(transform);
            if (root == null)
                continue;

            Renderer[] rootRenderers = root.GetComponentsInChildren<Renderer>(false);
            if (rootRenderers.Length == 0)
                continue;

            Bounds rootBounds = CalculateBounds(rootRenderers);
            if (!sourceBounds.Intersects(rootBounds) || IsHugeBackground(root.name, rootBounds))
                continue;

            roots[root] = root;
        }

        return roots.Values
            .Where(go => !HasSelectedAncestor(go.transform, roots))
            .OrderBy(go => go.name, StringComparer.Ordinal)
            .ToList();
    }

    private static void CloneSourceRoots(GameObject root, List<GameObject> sourceRoots, Vector3 sourceCenter)
    {
        GameObject cloneRoot = Child(root, "DemoMilitaryBase_SourceClones");
        foreach (GameObject source in sourceRoots)
        {
            GameObject clone = Object.Instantiate(source);
            clone.name = "GC07_" + source.name;
            clone.transform.SetParent(cloneRoot.transform, true);
            Vector3 relative = source.transform.position - sourceCenter;
            clone.transform.position = new Vector3(relative.x * Scale, source.transform.position.y * 0.35f, relative.z * Scale);
            clone.transform.rotation = source.transform.rotation;
            clone.transform.localScale = source.transform.lossyScale * Scale;
            AlignBottomNearGround(clone);
        }
    }

    private static void BuildEnvironment(GameObject root)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.64f, 0.58f, 0.49f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.55f, 0.47f, 0.36f, 1f);
        RenderSettings.fogDensity = 0.00055f;

        Light key = Child(root, "DirectionalLight_Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.85f;
        key.color = new Color(1f, 0.88f, 0.66f, 1f);
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.58f;
        key.transform.rotation = Quaternion.Euler(50f, -42f, 0f);
    }

    private static void BuildBasePlane(GameObject root)
    {
        Surface(root, "FlatPlayableSand_2048", Vector3.zero, new Vector2(2048f, 2048f), CreateMaterial("GC07_SandBase", new Color(0.62f, 0.50f, 0.30f, 1f)), -0.03f);
    }

    private static void PlaceProofSoldiers(GameObject root)
    {
        GameObject units = Child(root, "ProofSoldiers_OnFlatPlayableLanes");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab", new Vector3(-410f, 0f, -430f), 42f, "PlayerSoldier_01");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab", new Vector3(-375f, 0f, -400f), 42f, "PlayerSoldier_02");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_01.prefab", new Vector3(-340f, 0f, -365f), 42f, "PlayerSoldier_03");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab", new Vector3(390f, 0f, 340f), 222f, "EnemySoldier_01");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_02.prefab", new Vector3(350f, 0f, 315f), 222f, "EnemySoldier_02");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Female_01.prefab", new Vector3(315f, 0f, 290f), 222f, "EnemySoldier_03");
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
        unit.transform.localScale = Vector3.one * 1.25f;
        AlignBottomNearGround(unit);
    }

    private static void BuildCameras(GameObject root)
    {
        BuildCamera(root, "Camera_GC07_TargetStyleOverview", new Vector3(-260f, 620f, -690f), new Vector3(40f, 0f, 20f), 34f);
        BuildCamera(root, "Camera_GC07_BaseClose", new Vector3(-210f, 430f, -440f), new Vector3(15f, 0f, -30f), 34f);
        BuildCamera(root, "Camera_GC07_RunwayClose", new Vector3(-190f, 410f, -190f), new Vector3(250f, 0f, 410f), 34f);
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
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
    }

    private static void Validate(int sourceRootCount)
    {
        if (sourceRootCount < 500)
            ValidationLog.Add($"ERROR: expected dense Demo military source roots, found {sourceRootCount}.");
        if (ValidationLog.Count == 0)
            ValidationLog.Add("PASS: GC07 cloned a dense authored Demo military compound into a flat generated playable scene.");
    }

    private static void CaptureScene()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (camera.name == "Camera_GC07_TargetStyleOverview")
                Render(camera, ProjectPath(CaptureRoot + "/gc07_target_style_overview_1920x1080.png"));
            if (camera.name == "Camera_GC07_BaseClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc07_base_close_1920x1080.png"));
            if (camera.name == "Camera_GC07_RunwayClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc07_runway_close_1920x1080.png"));
        }
    }

    private static void WriteReport(int sourceRootCount)
    {
        StringBuilder report = new();
        report.AppendLine("# GC07 Demo Military Playable Scene");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Build a visually good top-down military RTS playable scene from the authored Demo military-base composition.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGc07DemoMilitaryPlayableSceneBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC07_DemoMilitaryPlayable_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC07_DemoMilitaryPlayable_2048/gc07_target_style_overview_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC07_DemoMilitaryPlayable_2048/gc07_base_close_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC07_DemoMilitaryPlayable_2048/gc07_runway_close_1920x1080.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: Gameplay playable scene generation workflow contract.");
        report.AppendLine("User-visible behavior: none in shipped flow; generated scene is available for visual review.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureGc07DemoMilitaryPlayableSceneBuilder.BuildGc07DemoMilitaryPlayableScene`.");
        report.AppendLine("Validation result: " + (ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)) ? "failed; see log below." : "passed dense authored-scene clone validation."));
        report.AppendLine("Known gaps: this is the visual/playable basis pass; internal walkability masks still need to be authored after visual acceptance.");
        report.AppendLine("Cross-lane impacts: PM/Design can review the new scene against the supplied top-down military RTS target before ECS/runtime conversion.");
        report.AppendLine("Next recommended task: tune camera, crop, and any blocked-lane cleanup based on visual review.");
        report.AppendLine();
        report.AppendLine($"Source roots cloned: {sourceRootCount}");
        report.AppendLine();
        report.AppendLine("Validation log:");
        foreach (string line in ValidationLog)
            report.AppendLine("- " + line);
        File.WriteAllText(ProjectPath(ReportPath), report.ToString(), Encoding.UTF8);
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

    private static bool IsHugeBackground(string name, Bounds bounds)
    {
        string lower = name.ToLowerInvariant();
        return lower.Contains("sky") || lower.Contains("cloud") || lower.Contains("skydome") ||
            bounds.center.y > 500f || bounds.size.x > 900f || bounds.size.z > 900f;
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
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture texture = new(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
        camera.targetTexture = texture;
        RenderTexture.active = texture;
        GL.Clear(true, true, camera.backgroundColor);
        camera.Render();
        Texture2D image = new(1920, 1080, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
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

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

public static class WarlineCaptureSceneKitAudit
{
    private const string ReportPath = "Design/AgentReports/2026-05-19_gameplay_3d-scene-kit-audit.md";
    private const string JsonPath = "Design/AgentReports/Captures/scene_kit_audit.json";

    private static readonly string[] ScenePaths =
    {
        "Assets/Game/Scenes/Demo.unity",
        "Assets/Game/Scenes/Game.unity",
        "Assets/PolygonMilitary/Scenes/Demo.unity"
    };

    private static readonly string[] PrefabRoots =
    {
        "Assets/Game/Prefabs/Buildings",
        "Assets/Game/Prefabs/Environment",
        "Assets/Game/Prefabs/Vehicles",
        "Assets/PolygonMilitary/Prefabs/Buildings",
        "Assets/PolygonMilitary/Prefabs/Environment",
        "Assets/PolygonMilitary/Prefabs/Props",
        "Assets/PolygonMilitary/Prefabs/Vehicles",
        "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs"
    };

    public static void Run()
    {
        Directory.CreateDirectory("Design/AgentReports/Captures");
        List<SceneAudit> scenes = ScenePaths
            .Where(File.Exists)
            .Select(AuditScene)
            .ToList();
        List<PrefabAudit> prefabs = AuditPrefabs();

        File.WriteAllText(JsonPath, BuildJson(scenes, prefabs), Encoding.UTF8);
        File.WriteAllText(ReportPath, BuildMarkdown(scenes, prefabs), Encoding.UTF8);
        Debug.Log($"WARLINECAPTURE_SCENE_KIT_AUDIT report={ReportPath} json={JsonPath} scenes={scenes.Count} prefabs={prefabs.Count}");
        EditorApplication.Exit(0);
    }

    private static SceneAudit AuditScene(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        Renderer[] renderers = roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray();
        MeshFilter[] meshFilters = roots.SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true)).ToArray();
        Animator[] animators = roots.SelectMany(root => root.GetComponentsInChildren<Animator>(true)).ToArray();
        Camera[] cameras = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).ToArray();
        Light[] lights = roots.SelectMany(root => root.GetComponentsInChildren<Light>(true)).ToArray();
        Bounds bounds = CalculateBounds(renderers);
        Dictionary<string, int> prefabUse = new(StringComparer.Ordinal);

        foreach (GameObject root in roots)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(transform.gameObject);
                if (source == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(source);
                if (string.IsNullOrEmpty(path))
                    continue;
                prefabUse[path] = prefabUse.TryGetValue(path, out int count) ? count + 1 : 1;
            }
        }

        return new SceneAudit
        {
            Path = scenePath,
            RootNames = roots.Select(root => root.name).OrderBy(name => name, StringComparer.Ordinal).ToList(),
            GameObjectCount = roots.Sum(root => root.GetComponentsInChildren<Transform>(true).Length),
            RendererCount = renderers.Length,
            MeshFilterCount = meshFilters.Length,
            AnimatorCount = animators.Length,
            CameraCount = cameras.Length,
            LightCount = lights.Length,
            BoundsCenter = bounds.center,
            BoundsSize = bounds.size,
            PrefabUses = prefabUse.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.Ordinal).Take(80).ToList()
        };
    }

    private static List<PrefabAudit> AuditPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", PrefabRoots.Where(AssetDatabase.IsValidFolder).ToArray());
        List<PrefabAudit> audits = new();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            GameObject instance = null;
            try
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.hideFlags = HideFlags.HideAndDontSave;
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                MeshFilter[] meshFilters = instance.GetComponentsInChildren<MeshFilter>(true);
                SkinnedMeshRenderer[] skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
                Bounds bounds = CalculateBounds(renderers);
                audits.Add(new PrefabAudit
                {
                    Path = path,
                    Name = prefab.name,
                    Role = Classify(path),
                    ChildCount = instance.GetComponentsInChildren<Transform>(true).Length,
                    RendererCount = renderers.Length,
                    MeshFilterCount = meshFilters.Length,
                    SkinnedRendererCount = skinned.Length,
                    ColliderCount = colliders.Length,
                    BoundsCenter = bounds.center,
                    BoundsSize = bounds.size,
                    MaterialCount = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().Count()
                });
            }
            finally
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        return audits
            .OrderBy(audit => audit.Role, StringComparer.Ordinal)
            .ThenByDescending(audit => audit.FootprintArea)
            .ThenBy(audit => audit.Path, StringComparer.Ordinal)
            .ToList();
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        bool hasBounds = false;
        Bounds bounds = default;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.zero);
    }

    private static string Classify(string path)
    {
        string lower = path.ToLowerInvariant();
        if (lower.Contains("/vehicles/") || lower.Contains("_veh_"))
            return lower.Contains("destroyed") ? "vehicle_destroyed" : "vehicle";
        if (lower.Contains("/buildings/") || lower.Contains("_bld_"))
            return lower.Contains("destroyed") ? "building_destroyed" : "building";
        if (lower.Contains("road") || lower.Contains("sidewalk"))
            return "road";
        if (lower.Contains("barrier") || lower.Contains("fence") || lower.Contains("sandbag") || lower.Contains("cover"))
            return "cover";
        if (lower.Contains("debris") || lower.Contains("rubbish") || lower.Contains("crater") || lower.Contains("blood") || lower.Contains("shell"))
            return "debris";
        if (lower.Contains("soldier") || lower.Contains("human_") || lower.Contains("/prefabs/weapons/"))
            return "soldier_or_weapon";
        if (lower.Contains("/environment/") || lower.Contains("_env_"))
            return "environment";
        if (lower.Contains("/props/") || lower.Contains("_prop_") || lower.Contains("_item_"))
            return "prop";
        return "uncategorized";
    }

    private static string BuildMarkdown(List<SceneAudit> scenes, List<PrefabAudit> prefabs)
    {
        StringBuilder builder = new();
        builder.AppendLine("# WarlineCapture Handoff - Gameplay 3D Scene Kit Audit");
        builder.AppendLine();
        builder.AppendLine("Date: 2026-05-19");
        builder.AppendLine("Lane: Gameplay");
        builder.AppendLine("Status: audit complete, no scene generation performed");
        builder.AppendLine("Priority: exploratory scene assembly groundwork");
        builder.AppendLine();
        builder.AppendLine("## Lane");
        builder.AppendLine();
        builder.AppendLine("Gameplay");
        builder.AppendLine();
        builder.AppendLine("## Task");
        builder.AppendLine();
        builder.AppendLine("Review existing promoted `Game`, `Demo`, and available 3D model kit before any procedural scene generation.");
        builder.AppendLine();
        builder.AppendLine("## Files changed");
        builder.AppendLine();
        builder.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureSceneKitAudit.cs`");
        builder.AppendLine("- `Design/AgentReports/2026-05-19_gameplay_3d-scene-kit-audit.md`");
        builder.AppendLine("- `Design/AgentReports/Captures/scene_kit_audit.json`");
        builder.AppendLine();
        builder.AppendLine("## Scene Inventory");
        builder.AppendLine();
        foreach (SceneAudit scene in scenes)
        {
            builder.AppendLine($"### `{scene.Path}`");
            builder.AppendLine();
            builder.AppendLine($"- Roots: {scene.RootNames.Count}");
            builder.AppendLine($"- GameObjects: {scene.GameObjectCount}");
            builder.AppendLine($"- Renderers: {scene.RendererCount}");
            builder.AppendLine($"- MeshFilters: {scene.MeshFilterCount}");
            builder.AppendLine($"- Animators: {scene.AnimatorCount}");
            builder.AppendLine($"- Cameras: {scene.CameraCount}");
            builder.AppendLine($"- Lights: {scene.LightCount}");
            builder.AppendLine($"- Bounds center: {Format(scene.BoundsCenter)}");
            builder.AppendLine($"- Bounds size: {Format(scene.BoundsSize)}");
            builder.AppendLine($"- Root samples: {string.Join(", ", scene.RootNames.Take(12).Select(name => $"`{name}`"))}");
            if (scene.PrefabUses.Count > 0)
            {
                builder.AppendLine("- Top prefab references:");
                foreach (KeyValuePair<string, int> pair in scene.PrefabUses.Take(12))
                    builder.AppendLine($"  - {pair.Value}x `{pair.Key}`");
            }
            builder.AppendLine();
        }

        builder.AppendLine("## Prefab Kit Summary");
        builder.AppendLine();
        foreach (IGrouping<string, PrefabAudit> group in prefabs.GroupBy(prefab => prefab.Role).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{group.Key}`: {group.Count()} prefabs");
        }
        builder.AppendLine();

        builder.AppendLine("## Largest Footprints By Role");
        builder.AppendLine();
        foreach (IGrouping<string, PrefabAudit> group in prefabs.GroupBy(prefab => prefab.Role).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            builder.AppendLine($"### `{group.Key}`");
            foreach (PrefabAudit prefab in group.Take(10))
            {
                builder.AppendLine($"- `{prefab.Path}` size={Format(prefab.BoundsSize)} renderers={prefab.RendererCount} colliders={prefab.ColliderCount} materials={prefab.MaterialCount}");
            }
            builder.AppendLine();
        }

        builder.AppendLine("## Contracts touched");
        builder.AppendLine();
        builder.AppendLine("- None. This pass audits scene/model data only.");
        builder.AppendLine();
        builder.AppendLine("## User-visible behavior");
        builder.AppendLine();
        builder.AppendLine("- None. No runtime scene or prefab placement was changed.");
        builder.AppendLine();
        builder.AppendLine("## Validation run");
        builder.AppendLine();
        builder.AppendLine("- Unity editor asset/scene audit through `WarlineCaptureSceneKitAudit.Run`.");
        builder.AppendLine();
        builder.AppendLine("## Validation result");
        builder.AppendLine();
        builder.AppendLine($"- Audited {scenes.Count} scenes and {prefabs.Count} prefabs.");
        builder.AppendLine($"- JSON output: `{JsonPath}`");
        builder.AppendLine();
        builder.AppendLine("## Known gaps");
        builder.AppendLine();
        builder.AppendLine("- This is a bounds/catalog pass. It does not yet generate screenshot contact sheets or classify road sockets.");
        builder.AppendLine("- Mesh triangle counts are not included yet; Unity import mesh access needs a second pass.");
        builder.AppendLine("- Scene composition quality has not been generated or judged yet.");
        builder.AppendLine();
        builder.AppendLine("## Cross-lane impacts");
        builder.AppendLine();
        builder.AppendLine("- Provides the asset catalog foundation needed before Designer/Gameplay procedural scene direction.");
        builder.AppendLine();
        builder.AppendLine("## Next recommended task");
        builder.AppendLine();
        builder.AppendLine("Generate visual contact sheets for the top building, road, vehicle, cover, debris, and soldier prefabs, then define road sockets and mission-layout grammar before creating any scene candidate.");
        return builder.ToString();
    }

    private static string BuildJson(List<SceneAudit> scenes, List<PrefabAudit> prefabs)
    {
        StringBuilder builder = new();
        builder.AppendLine("{");
        builder.AppendLine("  \"scenes\": [");
        for (int i = 0; i < scenes.Count; i++)
        {
            SceneAudit scene = scenes[i];
            builder.AppendLine("    {");
            builder.AppendLine($"      \"path\": {Json(scene.Path)},");
            builder.AppendLine($"      \"gameObjects\": {scene.GameObjectCount},");
            builder.AppendLine($"      \"renderers\": {scene.RendererCount},");
            builder.AppendLine($"      \"meshFilters\": {scene.MeshFilterCount},");
            builder.AppendLine($"      \"animators\": {scene.AnimatorCount},");
            builder.AppendLine($"      \"cameras\": {scene.CameraCount},");
            builder.AppendLine($"      \"lights\": {scene.LightCount},");
            builder.AppendLine($"      \"boundsCenter\": {JsonVector(scene.BoundsCenter)},");
            builder.AppendLine($"      \"boundsSize\": {JsonVector(scene.BoundsSize)}");
            builder.AppendLine(i == scenes.Count - 1 ? "    }" : "    },");
        }
        builder.AppendLine("  ],");
        builder.AppendLine("  \"prefabs\": [");
        for (int i = 0; i < prefabs.Count; i++)
        {
            PrefabAudit prefab = prefabs[i];
            builder.AppendLine("    {");
            builder.AppendLine($"      \"path\": {Json(prefab.Path)},");
            builder.AppendLine($"      \"role\": {Json(prefab.Role)},");
            builder.AppendLine($"      \"boundsSize\": {JsonVector(prefab.BoundsSize)},");
            builder.AppendLine($"      \"renderers\": {prefab.RendererCount},");
            builder.AppendLine($"      \"meshFilters\": {prefab.MeshFilterCount},");
            builder.AppendLine($"      \"skinnedRenderers\": {prefab.SkinnedRendererCount},");
            builder.AppendLine($"      \"colliders\": {prefab.ColliderCount},");
            builder.AppendLine($"      \"materials\": {prefab.MaterialCount}");
            builder.AppendLine(i == prefabs.Count - 1 ? "    }" : "    },");
        }
        builder.AppendLine("  ]");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string Json(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static string JsonVector(Vector3 value)
    {
        return $"[{F(value.x)},{F(value.y)},{F(value.z)}]";
    }

    private static string Format(Vector3 value)
    {
        return $"({F(value.x)}, {F(value.y)}, {F(value.z)})";
    }

    private static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private sealed class SceneAudit
    {
        public string Path;
        public List<string> RootNames;
        public int GameObjectCount;
        public int RendererCount;
        public int MeshFilterCount;
        public int AnimatorCount;
        public int CameraCount;
        public int LightCount;
        public Vector3 BoundsCenter;
        public Vector3 BoundsSize;
        public List<KeyValuePair<string, int>> PrefabUses;
    }

    private sealed class PrefabAudit
    {
        public string Path;
        public string Name;
        public string Role;
        public int ChildCount;
        public int RendererCount;
        public int MeshFilterCount;
        public int SkinnedRendererCount;
        public int ColliderCount;
        public int MaterialCount;
        public Vector3 BoundsCenter;
        public Vector3 BoundsSize;
        public float FootprintArea => Mathf.Abs(BoundsSize.x * BoundsSize.z);
    }
}
#endif

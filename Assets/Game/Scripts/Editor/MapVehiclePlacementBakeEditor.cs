#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class MapVehiclePlacementBakeEditor
{
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private const string ConfigAssetPath = "Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset";
    private const string VehiclesPrefabRoot = "Assets/Game/Prefabs/Vehicles";
    private const string ReportPath = "/private/tmp/map-vehicle-placement-bake-report.json";

    [Serializable]
    private sealed class BakeReport
    {
        public int placementCount;
        public int skippedFolderCount;
        public int missingPrefabCount;
        public int emptyCategoryCount;
        public int faction0Count;
        public int faction1Count;
        public int faction2Count;
        public List<string> categories = new();
        public List<string> emptyCategories = new();
        public List<string> skippedFolders = new();
        public List<string> missingPrefabs = new();
        public List<string> warnings = new();
        public List<string> errors = new();
    }

    [MenuItem("Game/Map/Bake Match Vehicle Placements")]
    public static void BakeMatchVehiclePlacements()
    {
        BakeReport report = new();
        try
        {
            Scene scene = SceneManager.GetSceneByPath(MatchScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);

            GameObject map = FindMapWithVehicles(scene);
            if (map == null)
                throw new InvalidOperationException("Map object not found in Match scene.");

            Transform vehiclesRoot = map.transform.Find("Vehicles");
            if (vehiclesRoot == null)
                throw new InvalidOperationException("Map/Vehicles root not found in Match scene.");

            MatchSceneView matchSceneView = FindComponentInScene<MatchSceneView>(scene);
            if (matchSceneView == null)
                throw new InvalidOperationException("MatchSceneView not found in Match scene.");

            Bounds? faction1 = TryGetObjectBounds(FindInScene(scene, "Faction1"));
            Bounds? faction2 = TryGetObjectBounds(FindInScene(scene, "Faction2"));
            if (!faction1.HasValue)
                report.warnings.Add("Faction1 volume not found; player-faction vehicles will not be assigned.");
            if (!faction2.HasValue)
                report.warnings.Add("Faction2 volume not found; enemy-faction vehicles will not be assigned.");

            List<MapVehiclePlacementConfigEntry> placements = new();
            HashSet<string> categories = new(StringComparer.Ordinal);
            for (int i = 0; i < vehiclesRoot.childCount; i++)
            {
                Transform categoryRoot = vehiclesRoot.GetChild(i);
                if (!categoryRoot.name.StartsWith("Unit_Veh_", StringComparison.Ordinal))
                {
                    report.skippedFolderCount++;
                    report.skippedFolders.Add(GetHierarchyPath(categoryRoot));
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{VehiclesPrefabRoot}/{categoryRoot.name}.prefab");
                if (prefab == null)
                {
                    report.missingPrefabCount++;
                    report.missingPrefabs.Add($"{VehiclesPrefabRoot}/{categoryRoot.name}.prefab");
                    continue;
                }

                categories.Add(categoryRoot.name);
                if (categoryRoot.childCount == 0)
                {
                    report.emptyCategoryCount++;
                    report.emptyCategories.Add(categoryRoot.name);
                    report.warnings.Add($"Vehicle category has no authored placements: {categoryRoot.name}");
                    continue;
                }

                for (int childIndex = 0; childIndex < categoryRoot.childCount; childIndex++)
                {
                    Transform placementRoot = categoryRoot.GetChild(childIndex);
                    Vector3 center = TryGetObjectBounds(placementRoot.gameObject) is { } bounds
                        ? bounds.center
                        : placementRoot.position;
                    byte factionId = ResolveFactionId(center, faction1, faction2, placementRoot, report);
                    placements.Add(new MapVehiclePlacementConfigEntry(
                        GetHierarchyPath(placementRoot),
                        categoryRoot.name,
                        prefab,
                        factionId,
                        center,
                        placementRoot.position,
                        placementRoot.eulerAngles,
                        placementRoot.lossyScale));

                    if (factionId == 1)
                        report.faction1Count++;
                    else if (factionId == 2)
                        report.faction2Count++;
                    else
                        report.faction0Count++;
                }
            }

            if (report.errors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, report.errors));

            MapVehiclePlacementConfig config = AssetDatabase.LoadAssetAtPath<MapVehiclePlacementConfig>(ConfigAssetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<MapVehiclePlacementConfig>();
                AssetDatabase.CreateAsset(config, ConfigAssetPath);
            }

            config.EditorSetPlacements(placements);
            AssignMatchSceneViewReferences(matchSceneView, config, vehiclesRoot);
            report.placementCount = placements.Count;
            report.categories.AddRange(categories);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MapVehiclePlacement] baked {placements.Count} placements to {ConfigAssetPath}. Report: {ReportPath}");
        }
        catch (Exception ex)
        {
            report.errors.Add(ex.Message);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            Debug.LogError($"[MapVehiclePlacement] bake failed: {ex.Message}. Report: {ReportPath}");
            throw;
        }
    }

    private static byte ResolveFactionId(
        Vector3 center,
        Bounds? faction1,
        Bounds? faction2,
        Transform placementRoot,
        BakeReport report)
    {
        bool inFaction1 = faction1.HasValue && faction1.Value.Contains(center);
        bool inFaction2 = faction2.HasValue && faction2.Value.Contains(center);
        if (inFaction1 && inFaction2)
        {
            report.errors.Add($"{GetHierarchyPath(placementRoot)} is inside both Faction1 and Faction2 volumes.");
            return 0;
        }

        if (inFaction1)
            return 1;

        return inFaction2 ? (byte)2 : (byte)0;
    }

    private static void AssignMatchSceneViewReferences(
        MatchSceneView view,
        MapVehiclePlacementConfig config,
        Transform vehiclesRoot)
    {
        SerializedObject serialized = new(view);
        serialized.FindProperty("mapVehiclePlacementConfig").objectReferenceValue = config;
        serialized.FindProperty("mapVehicleAuthoringRoot").objectReferenceValue = vehiclesRoot;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(view);
    }

    private static Bounds? TryGetObjectBounds(GameObject root)
    {
        if (root == null)
            return null;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds? result = null;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (result.HasValue)
            {
                Bounds expanded = result.Value;
                expanded.Encapsulate(renderers[i].bounds);
                result = expanded;
            }
            else
            {
                result = renderers[i].bounds;
            }
        }

        if (result.HasValue)
            return result.Value;

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (result.HasValue)
            {
                Bounds expanded = result.Value;
                expanded.Encapsulate(colliders[i].bounds);
                result = expanded;
            }
            else
            {
                result = colliders[i].bounds;
            }
        }

        return result;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform match = FindChildRecursive(roots[i].transform, objectName);
            if (match != null)
                return match.gameObject;
        }

        return null;
    }

    private static GameObject FindMapWithVehicles(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform match = FindMapWithVehiclesRecursive(roots[i].transform);
            if (match != null)
                return match.gameObject;
        }

        return null;
    }

    private static Transform FindMapWithVehiclesRecursive(Transform root)
    {
        if (root.name == "Map" && root.Find("Vehicles") != null)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindMapWithVehiclesRecursive(root.GetChild(i));
            if (match != null)
                return match;
        }

        return null;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindChildRecursive(root.GetChild(i), objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
#endif

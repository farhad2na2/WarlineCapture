using Game.Configs;
using Game.Runtime;
using Game.Composition;

namespace Game.Editor
{
    #if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    internal static class MapBuildingPlacementBakeEditor
    {
        private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
        private const string ConfigAssetPath = "Assets/Game/Configs/Scene/Match_MapBuildingPlacement_Config.asset";
        private const string BuildingsPrefabRoot = "Assets/Game/Prefabs/Buildings";
        private const string ReportPath = "/private/tmp/warline-map-building-placement-bake-report.json";
        private const string OperationMapReportPath =
            "/private/tmp/warline-operation-map-building-placement-bake-report.json";

        [Serializable]
        private sealed class BakeReport
        {
            public int placementCount;
            public int faction0Count;
            public int faction1Count;
            public int faction2Count;
            public List<string> categories = new();
            public List<string> warnings = new();
            public List<string> errors = new();
        }

        [MenuItem("Game/Map/Bake Match Building Placements")]
        public static void BakeMatchBuildingPlacements()
        {
            BakeReport report = new();
            try
            {
                Scene scene = SceneManager.GetSceneByPath(MatchScenePath);
                if (!scene.IsValid() || !scene.isLoaded)
                    scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);

                GameObject map = FindMapWithBuildings(scene);
                if (map == null)
                    throw new InvalidOperationException("Map object not found in Match scene.");

                MatchSceneView matchSceneView = FindComponentInScene<MatchSceneView>(scene);
                if (matchSceneView == null)
                    throw new InvalidOperationException("MatchSceneView not found in Match scene.");

                MapBuildingPlacementConfig config = AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(ConfigAssetPath);
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
                    AssetDatabase.CreateAsset(config, ConfigAssetPath);
                }

                Transform buildingsRoot = BakePlacements(scene, map.transform, config, report);
                AssignMatchSceneViewReferences(matchSceneView, config, buildingsRoot);
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[MapBuildingPlacement] baked {report.placementCount} placements to {ConfigAssetPath}. Report: {ReportPath}");
            }
            catch (Exception ex)
            {
                report.errors.Add(ex.Message);
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
                Debug.LogError($"[MapBuildingPlacement] bake failed: {ex.Message}. Report: {ReportPath}");
                throw;
            }
        }

        internal static void BakeOperationMapBuildingPlacements(
            Scene scene,
            OperationMapSceneView operationMapSceneView)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Operation-map scene must be loaded before placement baking.");
            if (operationMapSceneView == null || operationMapSceneView.MapRoot == null ||
                operationMapSceneView.BuildingPlacements == null)
            {
                throw new InvalidOperationException("Operation-map building placement bindings are incomplete.");
            }

            BakeReport report = new();
            try
            {
                BakePlacements(
                    scene,
                    operationMapSceneView.MapRoot,
                    operationMapSceneView.BuildingPlacements,
                    report);
                File.WriteAllText(OperationMapReportPath, JsonUtility.ToJson(report, true));
                AssetDatabase.SaveAssetIfDirty(operationMapSceneView.BuildingPlacements);
                Debug.Log(
                    $"[OperationMapBuildingPlacement] baked {report.placementCount} placements. " +
                    $"Report: {OperationMapReportPath}");
            }
            catch (Exception ex)
            {
                report.errors.Add(ex.Message);
                File.WriteAllText(OperationMapReportPath, JsonUtility.ToJson(report, true));
                throw;
            }
        }

        private static Transform BakePlacements(
            Scene scene,
            Transform map,
            MapBuildingPlacementConfig config,
            BakeReport report)
        {
            Transform buildingsRoot = map.Find("Buildings");
            if (buildingsRoot == null)
                throw new InvalidOperationException("Map/Buildings root not found.");

            Bounds? faction1 = TryGetObjectBounds(FindInScene(scene, "Faction1"));
            Bounds? faction2 = TryGetObjectBounds(FindInScene(scene, "Faction2"));
            if (!faction1.HasValue)
                report.warnings.Add("Faction1 volume not found; player-faction buildings will not be assigned.");
            if (!faction2.HasValue)
                report.warnings.Add("Faction2 volume not found; enemy-faction buildings will not be assigned.");

            List<MapBuildingPlacementConfigEntry> placements = new();
            HashSet<string> categories = new(StringComparer.Ordinal);
            Dictionary<string, byte> existingFactions = new(StringComparer.Ordinal);
            for (int i = 0; i < config.Placements.Count; i++)
                existingFactions[config.Placements[i].SourcePath] = config.Placements[i].FactionId;
            for (int i = 0; i < buildingsRoot.childCount; i++)
            {
                Transform categoryRoot = buildingsRoot.GetChild(i);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{BuildingsPrefabRoot}/{categoryRoot.name}.prefab");
                if (prefab == null)
                {
                    report.warnings.Add($"Skipped category without matching building prefab: {categoryRoot.name}");
                    continue;
                }

                categories.Add(categoryRoot.name);
                for (int childIndex = 0; childIndex < categoryRoot.childCount; childIndex++)
                {
                    Transform placementRoot = categoryRoot.GetChild(childIndex);
                    string sourcePath = GetHierarchyPath(placementRoot);
                    Bounds? placementBounds = TryGetObjectBounds(placementRoot.gameObject);
                    Vector3 center = placementBounds is { } bounds ? bounds.center : placementRoot.position;
                    byte factionId = ResolveFactionId(
                        center, placementBounds, faction1, faction2, placementRoot, report);
                    if (!faction1.HasValue && !faction2.HasValue &&
                        existingFactions.TryGetValue(sourcePath, out byte existingFactionId))
                    {
                        factionId = existingFactionId;
                    }
                    placements.Add(new MapBuildingPlacementConfigEntry(
                        sourcePath,
                        categoryRoot.name,
                        prefab,
                        factionId,
                        center,
                        placementRoot.position,
                        placementRoot.eulerAngles,
                        placementRoot.lossyScale,
                        placementRoot.eulerAngles.y,
                        IsVerticalRotation(placementRoot.eulerAngles.y)));

                    if (factionId == 1) report.faction1Count++;
                    else if (factionId == 2) report.faction2Count++;
                    else report.faction0Count++;
                }
            }

            if (report.errors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, report.errors));

            config.EditorSetPlacements(placements);
            report.placementCount = placements.Count;
            report.categories.AddRange(categories);
            return buildingsRoot;
        }

        private static byte ResolveFactionId(
            Vector3 center,
            Bounds? placementBounds,
            Bounds? faction1,
            Bounds? faction2,
            Transform placementRoot,
            BakeReport report)
        {
            bool inFaction1 = faction1.HasValue && faction1.Value.Contains(center);
            bool inFaction2 = faction2.HasValue && faction2.Value.Contains(center);
            if (!inFaction1 && !inFaction2 && placementBounds.HasValue)
            {
                inFaction1 = faction1.HasValue && faction1.Value.Intersects(placementBounds.Value);
                inFaction2 = faction2.HasValue && faction2.Value.Intersects(placementBounds.Value);
            }

            if (inFaction1 && inFaction2)
            {
                report.errors.Add($"{GetHierarchyPath(placementRoot)} is inside both Faction1 and Faction2 volumes.");
                return 0;
            }

            if (inFaction1)
                return 1;

            return inFaction2 ? (byte)2 : (byte)0;
        }

        private static bool IsVerticalRotation(float yawDegrees)
        {
            float yaw = Mathf.Repeat(yawDegrees, 180f);
            return Mathf.Abs(yaw - 90f) <= 45f;
        }

        private static void AssignMatchSceneViewReferences(
            MatchSceneView view,
            MapBuildingPlacementConfig config,
            Transform buildingsRoot)
        {
            SerializedObject serialized = new(view);
            serialized.FindProperty("mapBuildingPlacementConfig").objectReferenceValue = config;
            serialized.FindProperty("mapBuildingAuthoringRoot").objectReferenceValue = buildingsRoot;
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

        private static GameObject FindMapWithBuildings(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform match = FindMapWithBuildingsRecursive(roots[i].transform);
                if (match != null)
                    return match.gameObject;
            }

            return null;
        }

        private static Transform FindMapWithBuildingsRecursive(Transform root)
        {
            if (root.name == "Map" && root.Find("Buildings") != null)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindMapWithBuildingsRecursive(root.GetChild(i));
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
}

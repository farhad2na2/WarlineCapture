using Game.Authoring;

namespace Game.Editor
{
    #if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class MapBuildingMeshGroupingMigration
    {
        private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
        private const string BuildingPrefabRoot = "Assets/Game/Prefabs/Buildings";
        private const string ReportPath = "/private/tmp/warline-map-building-grouping-report.json";

        [MenuItem("Game/Map/Group Match Buildings By Prefab Mesh")]
        public static void GroupMatchSceneBuildingsByPrefabMesh()
        {
            try
            {
                Scene scene = SceneManager.GetSceneByPath(MatchScenePath);
                if (!scene.IsValid() || !scene.isLoaded)
                    scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);

                Transform source = FindRequiredTransform("Map/Buildings/Building");
                Transform groupsRoot = source.parent;
                Dictionary<Mesh, List<string>> meshToPrefabNames = BuildMeshToPrefabNameLookup();
                var report = new GroupingReport();
                var sourceChildren = new List<Transform>();
                for (int i = 0; i < source.childCount; i++)
                    sourceChildren.Add(source.GetChild(i));

                foreach (Transform child in sourceChildren)
                {
                    string category = ResolveCategory(child, meshToPrefabNames, report);
                    if (string.IsNullOrWhiteSpace(category))
                    {
                        report.Unmatched.Add(GetPath(child));
                        continue;
                    }

                    Transform categoryParent = GetOrCreateCategoryParent(groupsRoot, category, report);
                    Vector3 position = child.position;
                    Quaternion rotation = child.rotation;
                    Vector3 lossyScale = child.lossyScale;

                    child.SetParent(categoryParent, true);

                    if (!Approximately(position, child.position) ||
                        !Approximately(rotation, child.rotation) ||
                        !Approximately(lossyScale, child.lossyScale))
                    {
                        throw new InvalidOperationException(
                            $"World transform changed while grouping {child.name} under {category}.");
                    }

                    report.Moved++;
                    Increment(report.ByCategory, category);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.PrepareForSerialization();
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
                Debug.Log(
                    $"[MapBuildingMeshGroupingMigration] moved={report.Moved} " +
                    $"groupsCreated={report.GroupsCreated} unmatched={report.Unmatched.Count} " +
                    $"ambiguous={report.Ambiguous.Count} report={ReportPath}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        private static Dictionary<Mesh, List<string>> BuildMeshToPrefabNameLookup()
        {
            var lookup = new Dictionary<Mesh, List<string>>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { BuildingPrefabRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!ShouldUseBuildingPrefab(path))
                    continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<BuildingDefinitionAuthoring>() == null)
                    continue;

                string prefabName = Path.GetFileNameWithoutExtension(path);
                MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
                for (int i = 0; i < meshFilters.Length; i++)
                {
                    Mesh mesh = meshFilters[i] != null ? meshFilters[i].sharedMesh : null;
                    if (mesh == null)
                        continue;

                    if (!lookup.TryGetValue(mesh, out List<string> prefabNames))
                    {
                        prefabNames = new List<string>();
                        lookup.Add(mesh, prefabNames);
                    }

                    if (!prefabNames.Contains(prefabName, StringComparer.Ordinal))
                        prefabNames.Add(prefabName);
                }
            }

            return lookup;
        }

        private static bool ShouldUseBuildingPrefab(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            if (!path.StartsWith(BuildingPrefabRoot + "/", StringComparison.Ordinal))
                return false;
            if (path.StartsWith(BuildingPrefabRoot + "/Destroyed/", StringComparison.Ordinal))
                return false;

            string name = Path.GetFileName(path);
            return name != "Building.prefab" &&
                   name != "BuildingSelectionMarker.prefab" &&
                   Path.GetExtension(path) == ".prefab";
        }

        private static string ResolveCategory(
            Transform child,
            Dictionary<Mesh, List<string>> meshToPrefabNames,
            GroupingReport report)
        {
            string nameCategory = ResolveCategoryByName(child.name);
            if (!string.IsNullOrWhiteSpace(nameCategory))
                return nameCategory;

            var scores = new Dictionary<string, int>(StringComparer.Ordinal);
            MeshFilter[] meshFilters = child.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                Mesh mesh = meshFilters[i] != null ? meshFilters[i].sharedMesh : null;
                if (mesh == null || !meshToPrefabNames.TryGetValue(mesh, out List<string> candidates))
                    continue;

                for (int c = 0; c < candidates.Count; c++)
                    Increment(scores, candidates[c]);
            }

            if (scores.Count == 0)
                return null;

            int bestScore = scores.Values.Max();
            List<string> best = scores
                .Where(pair => pair.Value == bestScore)
                .Select(pair => pair.Key)
                .OrderByDescending(CategoryPriority)
                .ThenBy(name => name, StringComparer.Ordinal)
                .ToList();

            if (best.Count > 1)
                report.Ambiguous.Add($"{GetPath(child)} => {string.Join(", ", best)}");

            return best[0];
        }

        private static string ResolveCategoryByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            if (objectName.StartsWith("SM_Bld_Shop_", StringComparison.Ordinal))
                return "Building_Shop";

            if (objectName.StartsWith("SM_Bld_Village_House_", StringComparison.Ordinal))
                return "Building_House";

            return null;
        }

        private static int CategoryPriority(string category)
        {
            return category switch
            {
                "Tent_Regular" => 100,
                "Building_House" => 95,
                "Building_Shop" => 95,
                "Building_Hall" => 95,
                "Building_Barrack" => 90,
                "Building_Airport" => 90,
                "Building_Ammunition_Depot" => 90,
                "Building_GuardTower" => 90,
                "Building_GuardTower_Big" => 90,
                "Building_Refinery" => 90,
                "Building_Refinery_Big" => 90,
                "Wall_Fence_Straight" => 80,
                _ => 0
            };
        }

        private static Transform GetOrCreateCategoryParent(Transform groupsRoot, string category, GroupingReport report)
        {
            Transform existing = groupsRoot.Find(category);
            if (existing != null)
                return existing;

            GameObject group = new(category);
            group.transform.SetParent(groupsRoot, false);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;
            report.GroupsCreated++;
            report.CreatedGroups.Add(category);
            return group.transform;
        }

        private static Transform FindRequiredTransform(string path)
        {
            string[] parts = path.Split('/');
            GameObject root = GameObject.Find(parts[0]);
            if (root == null)
                throw new InvalidOperationException($"Could not find scene object {parts[0]}.");

            Transform current = root.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
                if (current == null)
                    throw new InvalidOperationException($"Could not find scene object path {path}.");
            }

            return current;
        }

        private static void Increment(Dictionary<string, int> values, string key)
        {
            values.TryGetValue(key, out int value);
            values[key] = value + 1;
        }

        private static bool Approximately(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude < 0.000001f;
        }

        private static bool Approximately(Quaternion a, Quaternion b)
        {
            return Mathf.Abs(Quaternion.Dot(a, b)) > 0.999999f;
        }

        private static string GetPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        [Serializable]
        private sealed class GroupingReport
        {
            public int Moved;
            public int GroupsCreated;
            public List<string> CreatedGroups = new();
            public List<string> Unmatched = new();
            public List<string> Ambiguous = new();
            public SerializableCategoryCount[] CategoryCounts = Array.Empty<SerializableCategoryCount>();

            [NonSerialized]
            public readonly Dictionary<string, int> ByCategory = new(StringComparer.Ordinal);

            public void PrepareForSerialization()
            {
                CategoryCounts = ByCategory
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new SerializableCategoryCount { Category = pair.Key, Count = pair.Value })
                    .ToArray();
            }
        }

        [Serializable]
        private sealed class SerializableCategoryCount
        {
            public string Category;
            public int Count;
        }
    }
    #endif
}

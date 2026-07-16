using System;
using System.Collections.Generic;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapCurrentCompatibilityPlacementStager
    {
        public const string SourceBuildingConfigPath =
            "Assets/Game/Configs/Scene/Match_MapBuildingPlacement_Config.asset";
        public const string SourceVehicleConfigPath =
            "Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset";
        public const string DestinationBuildingConfigPath =
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01_BuildingPlacements.asset";
        public const string DestinationVehicleConfigPath =
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01_VehiclePlacements.asset";

        private const string DestinationBuildingConfigName =
            "OperationMap_Compatibility_DesertBase01_BuildingPlacements";
        private const string DestinationVehicleConfigName =
            "OperationMap_Compatibility_DesertBase01_VehiclePlacements";

        private const string BuildingPrefabRoot = "Assets/Game/Prefabs/Buildings";
        private const string VehiclePrefabRoot = "Assets/Game/Prefabs/Vehicles";
        private const string RuntimeVehiclePrefix = "Unit_Veh_";
        private const string AuthoringVehiclePrefix = "MapVehicle_";

        private readonly struct PlacementIdentity
        {
            public readonly string SourcePath;
            public readonly string Category;
            public readonly GameObject Prefab;
            public readonly Transform Transform;

            public PlacementIdentity(string sourcePath, string category, GameObject prefab, Transform transform)
            {
                SourcePath = sourcePath;
                Category = category;
                Prefab = prefab;
                Transform = transform;
            }
        }

        [MenuItem("Tools/Warline Capture/Operation Maps/Stage Current Placement Configs")]
        public static void Stage()
        {
            OperationMapCurrentCompatibilityRootExtractor.Extract();
            MapBuildingPlacementConfig sourceBuildings = LoadRequired<MapBuildingPlacementConfig>(
                SourceBuildingConfigPath);
            MapVehiclePlacementConfig sourceVehicles = LoadRequired<MapVehiclePlacementConfig>(
                SourceVehicleConfigPath);

            Scene scene = OpenStagedScene();
            try
            {
                Transform map = FindRequiredRoot(scene, "Map").transform;
                ValidateBuildingHierarchy(map, sourceBuildings);
                ValidateVehicleHierarchy(map, sourceVehicles);
            }
            finally
            {
                CloseScene(scene);
            }

            CopySerializedConfig(
                sourceBuildings, DestinationBuildingConfigPath, DestinationBuildingConfigName);
            CopySerializedConfig(
                sourceVehicles, DestinationVehicleConfigPath, DestinationVehicleConfigName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (!TryValidate(out string error))
                throw new InvalidOperationException(error);
        }

        public static void StageForBatch() => Stage();

        public static bool TryValidate(out string error)
        {
            try
            {
                MapBuildingPlacementConfig sourceBuildings = LoadRequired<MapBuildingPlacementConfig>(
                    SourceBuildingConfigPath);
                MapBuildingPlacementConfig destinationBuildings = LoadRequired<MapBuildingPlacementConfig>(
                    DestinationBuildingConfigPath);
                MapVehiclePlacementConfig sourceVehicles = LoadRequired<MapVehiclePlacementConfig>(
                    SourceVehicleConfigPath);
                MapVehiclePlacementConfig destinationVehicles = LoadRequired<MapVehiclePlacementConfig>(
                    DestinationVehicleConfigPath);

                ValidateDistinctGuid(SourceBuildingConfigPath, DestinationBuildingConfigPath);
                ValidateDistinctGuid(SourceVehicleConfigPath, DestinationVehicleConfigPath);
                ValidateAssetName(destinationBuildings, DestinationBuildingConfigName);
                ValidateAssetName(destinationVehicles, DestinationVehicleConfigName);
                ValidateBuildingConfigsEqual(sourceBuildings, destinationBuildings);
                ValidateVehicleConfigsEqual(sourceVehicles, destinationVehicles);

                Scene scene = OpenStagedScene();
                try
                {
                    Transform map = FindRequiredRoot(scene, "Map").transform;
                    ValidateBuildingHierarchy(map, destinationBuildings);
                    ValidateVehicleHierarchy(map, destinationVehicles);
                }
                finally
                {
                    CloseScene(scene);
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static void ValidateBuildingHierarchy(Transform map, MapBuildingPlacementConfig config)
        {
            Transform root = map.Find("Buildings");
            if (root == null)
                throw new InvalidOperationException("Staged operation map has no Map/Buildings root.");

            List<PlacementIdentity> authored = CollectBuildingIdentities(root);
            if (authored.Count != config.Placements.Count)
            {
                throw new InvalidOperationException(
                    $"Building placement count drift: scene={authored.Count}, config={config.Placements.Count}.");
            }

            bool[] matched = new bool[authored.Count];
            for (int entryIndex = 0; entryIndex < config.Placements.Count; entryIndex++)
            {
                MapBuildingPlacementConfigEntry entry = config.Placements[entryIndex];
                int match = FindMatch(authored, matched, entry.SourcePath, entry.Category,
                    entry.BuildingPrefab, entry.WorldPosition, entry.WorldEulerAngles, entry.WorldScale);
                if (match < 0)
                    throw new InvalidOperationException($"Building placement is stale or missing: '{entry.SourcePath}'.");
                matched[match] = true;
            }
        }

        private static void ValidateVehicleHierarchy(Transform map, MapVehiclePlacementConfig config)
        {
            Transform root = map.Find("Vehicles");
            if (root == null)
                throw new InvalidOperationException("Staged operation map has no Map/Vehicles root.");

            List<PlacementIdentity> authored = CollectVehicleIdentities(root);
            if (authored.Count != config.Placements.Count)
            {
                throw new InvalidOperationException(
                    $"Vehicle placement count drift: scene={authored.Count}, config={config.Placements.Count}.");
            }

            bool[] matched = new bool[authored.Count];
            for (int entryIndex = 0; entryIndex < config.Placements.Count; entryIndex++)
            {
                MapVehiclePlacementConfigEntry entry = config.Placements[entryIndex];
                int match = FindMatch(authored, matched, entry.SourcePath, entry.Category,
                    entry.VehiclePrefab, entry.WorldPosition, entry.WorldEulerAngles, entry.WorldScale);
                if (match < 0)
                    throw new InvalidOperationException($"Vehicle placement is stale or missing: '{entry.SourcePath}'.");
                matched[match] = true;
            }
        }

        private static List<PlacementIdentity> CollectBuildingIdentities(Transform root)
        {
            var identities = new List<PlacementIdentity>();
            for (int categoryIndex = 0; categoryIndex < root.childCount; categoryIndex++)
            {
                Transform category = root.GetChild(categoryIndex);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{BuildingPrefabRoot}/{category.name}.prefab");
                if (prefab == null)
                    continue;

                AddChildren(identities, category, category.name, prefab);
            }

            return identities;
        }

        private static List<PlacementIdentity> CollectVehicleIdentities(Transform root)
        {
            var identities = new List<PlacementIdentity>();
            for (int categoryIndex = 0; categoryIndex < root.childCount; categoryIndex++)
            {
                Transform category = root.GetChild(categoryIndex);
                string categoryName = ResolveVehicleCategory(category.name);
                if (string.IsNullOrEmpty(categoryName))
                    continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{VehiclePrefabRoot}/{categoryName}.prefab");
                if (prefab == null)
                    continue;

                AddChildren(identities, category, categoryName, prefab);
            }

            return identities;
        }

        private static void AddChildren(
            ICollection<PlacementIdentity> identities,
            Transform category,
            string categoryName,
            GameObject prefab)
        {
            for (int childIndex = 0; childIndex < category.childCount; childIndex++)
            {
                Transform placement = category.GetChild(childIndex);
                identities.Add(new PlacementIdentity(
                    GetHierarchyPath(placement), categoryName, prefab, placement));
            }
        }

        private static int FindMatch(
            IReadOnlyList<PlacementIdentity> authored,
            IReadOnlyList<bool> matched,
            string sourcePath,
            string category,
            GameObject prefab,
            Vector3 position,
            Vector3 eulerAngles,
            Vector3 scale)
        {
            for (int index = 0; index < authored.Count; index++)
            {
                PlacementIdentity candidate = authored[index];
                if (!matched[index] &&
                    string.Equals(candidate.SourcePath, sourcePath, StringComparison.Ordinal) &&
                    string.Equals(candidate.Category, category, StringComparison.Ordinal) &&
                    candidate.Prefab == prefab &&
                    Approximately(candidate.Transform.position, position) &&
                    ApproximatelyEuler(candidate.Transform.eulerAngles, eulerAngles) &&
                    Approximately(candidate.Transform.lossyScale, scale))
                {
                    return index;
                }
            }

            return -1;
        }

        private static void ValidateBuildingConfigsEqual(
            MapBuildingPlacementConfig source,
            MapBuildingPlacementConfig destination)
        {
            ValidateFlagsAndCount(source.SpawnOnMatchStart, destination.SpawnOnMatchStart,
                source.HideAuthoringVisualsAfterSpawn, destination.HideAuthoringVisualsAfterSpawn,
                source.Placements.Count, destination.Placements.Count, "building");
            for (int index = 0; index < source.Placements.Count; index++)
            {
                if (!string.Equals(
                        EditorJsonUtility.ToJson(source.Placements[index]),
                        EditorJsonUtility.ToJson(destination.Placements[index]),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Staged building placement {index} differs from source.");
                }
            }
        }

        private static void ValidateVehicleConfigsEqual(
            MapVehiclePlacementConfig source,
            MapVehiclePlacementConfig destination)
        {
            ValidateFlagsAndCount(source.SpawnOnMatchStart, destination.SpawnOnMatchStart,
                source.HideAuthoringVisualsAfterSpawn, destination.HideAuthoringVisualsAfterSpawn,
                source.Placements.Count, destination.Placements.Count, "vehicle");
            for (int index = 0; index < source.Placements.Count; index++)
            {
                if (!string.Equals(
                        EditorJsonUtility.ToJson(source.Placements[index]),
                        EditorJsonUtility.ToJson(destination.Placements[index]),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Staged vehicle placement {index} differs from source.");
                }
            }
        }

        private static void ValidateFlagsAndCount(
            bool sourceSpawn,
            bool destinationSpawn,
            bool sourceHide,
            bool destinationHide,
            int sourceCount,
            int destinationCount,
            string kind)
        {
            if (sourceSpawn != destinationSpawn || sourceHide != destinationHide || sourceCount != destinationCount)
                throw new InvalidOperationException($"Staged {kind} config behavior differs from source.");
        }

        private static void CopySerializedConfig<T>(T source, string destinationPath, string destinationName)
            where T : ScriptableObject
        {
            T destination = AssetDatabase.LoadAssetAtPath<T>(destinationPath);
            if (destination == null)
            {
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), destinationPath))
                    throw new InvalidOperationException($"Failed to create staged placement config '{destinationPath}'.");
                destination = LoadRequired<T>(destinationPath);
            }

            EditorUtility.CopySerialized(source, destination);
            destination.name = destinationName;
            EditorUtility.SetDirty(destination);
        }

        private static void ValidateAssetName(UnityEngine.Object asset, string expectedName)
        {
            if (!string.Equals(asset.name, expectedName, StringComparison.Ordinal))
                throw new InvalidOperationException($"Staged placement config name drifted: '{asset.name}'.");
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new InvalidOperationException($"Required asset is missing: '{path}'.");
        }

        private static void ValidateDistinctGuid(string sourcePath, string destinationPath)
        {
            string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            string destinationGuid = AssetDatabase.AssetPathToGUID(destinationPath);
            if (string.IsNullOrEmpty(sourceGuid) || string.IsNullOrEmpty(destinationGuid) ||
                string.Equals(sourceGuid, destinationGuid, StringComparison.Ordinal))
                throw new InvalidOperationException($"Placement configs require distinct GUIDs: '{destinationPath}'.");
        }

        private static Scene OpenStagedScene()
        {
            string path = OperationMapCurrentCompatibilitySceneStager.DestinationScenePath;
            Scene loaded = SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded)
                throw new InvalidOperationException($"Close '{path}' before staging placement configs.");
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private static void CloseScene(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, removeScene: true);
        }

        private static GameObject FindRequiredRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (string.Equals(roots[index].name, name, StringComparison.Ordinal))
                    return roots[index];
            }

            throw new InvalidOperationException($"Staged operation map has no '{name}' root.");
        }

        private static string ResolveVehicleCategory(string authoringName)
        {
            if (string.IsNullOrWhiteSpace(authoringName))
                return string.Empty;
            string trimmed = authoringName.Trim();
            if (trimmed.StartsWith(RuntimeVehiclePrefix, StringComparison.Ordinal))
                return trimmed;
            return trimmed.StartsWith(AuthoringVehiclePrefix, StringComparison.Ordinal)
                ? RuntimeVehiclePrefix + trimmed.Substring(AuthoringVehiclePrefix.Length)
                : string.Empty;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private static bool Approximately(Vector3 left, Vector3 right) =>
            (left - right).sqrMagnitude <= 0.000001f;

        private static bool ApproximatelyEuler(Vector3 left, Vector3 right) =>
            Mathf.Abs(Mathf.DeltaAngle(left.x, right.x)) <= 0.001f &&
            Mathf.Abs(Mathf.DeltaAngle(left.y, right.y)) <= 0.001f &&
            Mathf.Abs(Mathf.DeltaAngle(left.z, right.z)) <= 0.001f;
    }
}

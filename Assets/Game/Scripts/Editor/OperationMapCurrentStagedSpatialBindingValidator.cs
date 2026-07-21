using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Authoring;
using Game.Composition;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapCurrentStagedSpatialBindingValidator
    {
        private const string LightingDataPath = "Assets/Game/Scenes/Match/LightingData.asset";
        private const string ReflectionProbe0Path = "Assets/Game/Scenes/Match/ReflectionProbe-0.exr";
        private const string ReflectionProbe1Path = "Assets/Game/Scenes/Match/ReflectionProbe-1.exr";
        private const string AirportCategory = "Building_Airport";
        private const string HelipadCategory = "Building_Helipad";

        [MenuItem("Game/Operation Maps/Validate Current Staged Spatial Bindings")]
        public static void Run()
        {
            if (!TryValidate(out string error))
                throw new InvalidOperationException(error);
            Debug.Log("[OperationMapSpatialBindings] Passed current staged map.");
        }

        public static bool TryValidate(out string error)
        {
            Scene scene = default;
            try
            {
                string scenePath = OperationMapCurrentCompatibilitySceneStager.DestinationScenePath;
                Scene loaded = SceneManager.GetSceneByPath(scenePath);
                if (loaded.IsValid() && loaded.isLoaded)
                {
                    error = $"Close '{scenePath}' before spatial binding validation.";
                    return false;
                }

                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                if (!TryFindSingle(scene, out OperationMapSceneView view, out error) ||
                    !view.TryValidate(out error) ||
                    !view.Definition.TryValidateMetadata(out error))
                    return false;

                Transform map = FindDirectRoot(scene, "Map");
                Transform buildings = FindDirectChild(map, "Buildings");
                Transform runways = FindDirectChild(map, "Runways");
                Transform airport = FindDirectChild(buildings, AirportCategory);
                Transform helipads = FindDirectChild(buildings, HelipadCategory);
                if (map == null || buildings == null || runways == null || airport == null || helipads == null)
                {
                    error = "Staged operation map is missing an exact map/building/runway/helipad root.";
                    return false;
                }

                MapSurfaceAuthoring surface = view.MapSurfaceAuthoring;
                if (surface.transform != map && !surface.transform.IsChildOf(map))
                {
                    error = "Map-surface authoring must be owned by the staged Map root.";
                    return false;
                }

                OperationMapDefinition definition = view.Definition;
                OperationMapCatalogConfig catalog =
                    AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(
                        OperationMapAddressablesLayoutBuilder.CatalogPath);
                if (catalog == null ||
                    !catalog.TryResolve(view.OperationMapId, out OperationMapDefinition runtimeDefinition) ||
                    !MatchesSurfaceMetadata(surface.BakedSurfaceData, definition.SurfaceMetadata) ||
                    !MatchesSurfaceMetadata(surface.BakedSurfaceData, runtimeDefinition.SurfaceMetadata) ||
                    !MatchesAssetGuid(surface.GridConfig, definition.GridMetadata.AssetGuid))
                {
                    error = "Staged/runtime surface or grid references do not match operation-map metadata.";
                    return false;
                }

                if (runways.GetComponentsInChildren<Renderer>(true).Length == 0 ||
                    CountPlacements(view.BuildingPlacements.Placements, AirportCategory) != 1 ||
                    CountPlacements(view.BuildingPlacements.Placements, HelipadCategory) != 3)
                {
                    error = "Staged runway geometry or airport/helipad placement bindings drifted.";
                    return false;
                }

                if (!TryCountStagedStaticBlockers(out int staticBlockerCount, out error) ||
                    definition.NavigationMetadata.StaticGridBlockerCount != staticBlockerCount ||
                    !ContainsAllSceneDependencies(
                        scenePath,
                        LightingDataPath,
                        ReflectionProbe0Path,
                        ReflectionProbe1Path))
                {
                    error = "Staged blocker or lighting/probe metadata binding drifted.";
                    return false;
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        private static bool TryCountStagedStaticBlockers(out int count, out string error)
        {
            count = 0;
            Scene subScene = default;
            try
            {
                string path = OperationMapCurrentCompatibilitySubSceneStager.DestinationSubScenePath;
                Scene loaded = SceneManager.GetSceneByPath(path);
                if (loaded.IsValid() && loaded.isLoaded)
                {
                    error = $"Close '{path}' before spatial binding validation.";
                    return false;
                }

                subScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                foreach (GameObject root in subScene.GetRootGameObjects())
                    count += root.GetComponentsInChildren<StaticGridBlockerAuthoring>(true).Length;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                if (subScene.IsValid() && subScene.isLoaded)
                    EditorSceneManager.CloseScene(subScene, removeScene: true);
            }
        }

        private static int CountPlacements(
            IReadOnlyList<MapBuildingPlacementConfigEntry> placements,
            string category)
        {
            int count = 0;
            for (int index = 0; index < placements.Count; index++)
            {
                MapBuildingPlacementConfigEntry placement = placements[index];
                if (string.Equals(placement.Category, category, StringComparison.Ordinal) &&
                    placement.SourcePath.StartsWith($"Map/Buildings/{category}/", StringComparison.Ordinal))
                    count++;
            }
            return count;
        }

        private static bool ContainsAllSceneDependencies(string scenePath, params string[] requiredPaths)
        {
            var dependencies = new HashSet<string>(
                AssetDatabase.GetDependencies(scenePath, recursive: true),
                StringComparer.Ordinal);
            for (int index = 0; index < requiredPaths.Length; index++)
            {
                if (!dependencies.Contains(requiredPaths[index]))
                    return false;
            }
            return true;
        }

        private static bool MatchesAssetGuid(UnityEngine.Object asset, string expectedGuid) =>
            asset != null && string.Equals(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)),
                expectedGuid,
                StringComparison.Ordinal);

        private static bool MatchesSurfaceMetadata(
            MapSurfaceDataAsset surface,
            OperationMapSurfaceMetadataConfig expected)
        {
            if (surface == null)
                return false;

            string assetPath = AssetDatabase.GetAssetPath(surface);
            return string.Equals(
                       AssetDatabase.AssetPathToGUID(assetPath),
                       expected.AssetGuid,
                       StringComparison.Ordinal) &&
                   string.Equals(ComputeFileHash(assetPath), expected.ContentHash, StringComparison.Ordinal) &&
                   string.Equals(
                       surface.ComputeRuntimeBlobHash().ToString(),
                       expected.RuntimeBlobHash,
                       StringComparison.Ordinal) &&
                   surface.SurfaceCount == expected.SurfaceCount &&
                   surface.PayloadVersion == expected.PayloadVersion &&
                   surface.PayloadEncoding == expected.PayloadEncoding;
        }

        private static string ComputeFileHash(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string physicalPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            using SHA256 algorithm = SHA256.Create();
            using FileStream stream = File.OpenRead(physicalPath);
            byte[] hash = algorithm.ComputeHash(stream);
            StringBuilder builder = new(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2"));
            return builder.ToString();
        }

        private static bool TryFindSingle<T>(Scene scene, out T found, out string error) where T : Component
        {
            found = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T[] candidates = root.GetComponentsInChildren<T>(true);
                for (int index = 0; index < candidates.Length; index++)
                {
                    if (found != null)
                    {
                        error = $"Staged operation map contains multiple {typeof(T).Name} components.";
                        return false;
                    }
                    found = candidates[index];
                }
            }

            error = found == null ? $"Staged operation map has no {typeof(T).Name} component." : null;
            return found != null;
        }

        private static Transform FindDirectRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, name, StringComparison.Ordinal))
                    return root.transform;
            }
            return null;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            if (parent == null)
                return null;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                    return child;
            }
            return null;
        }
    }
}

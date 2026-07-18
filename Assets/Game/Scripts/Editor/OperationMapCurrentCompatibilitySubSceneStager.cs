using System;
using System.Collections.Generic;
using System.IO;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapCurrentCompatibilitySubSceneStager
    {
        public const string SourceSubScenePath = "Assets/Game/Scenes/Match/MatchSubScene.unity";
        public const string DestinationSubScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01_subscene.unity";

        private static readonly string[] SourceRootNames =
        {
            "Grid",
            "InitialUnitsSpawnerAuthoring",
            "UnitPrefabRegistryAuthoring"
        };

        private static readonly string[] MapRootNames =
        {
            "Grid",
            "InitialUnitsSpawnerAuthoring"
        };

        [MenuItem("Tools/Warline Capture/Operation Maps/Stage Current Compatibility SubScene")]
        public static void Stage()
        {
            OperationMapCurrentCompatibilityRootExtractor.Extract();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceSubScenePath) == null)
                throw new FileNotFoundException("Canonical Match subscene is missing.", SourceSubScenePath);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationSubScenePath) == null &&
                !AssetDatabase.CopyAsset(SourceSubScenePath, DestinationSubScenePath))
            {
                throw new InvalidOperationException(
                    $"AssetDatabase failed to stage '{SourceSubScenePath}' at '{DestinationSubScenePath}'.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ExtractMapOwnedRoots();
            RebindStagedMapScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (!TryValidate(out string error))
                throw new InvalidOperationException(error);
        }

        public static void StageForBatch() => Stage();

        public static bool TryValidate(out string error)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceSubScenePath) == null ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationSubScenePath) == null)
            {
                error = "Source and staged operation-map subscenes must both exist.";
                return false;
            }

            string sourceGuid = AssetDatabase.AssetPathToGUID(SourceSubScenePath);
            string destinationGuid = AssetDatabase.AssetPathToGUID(DestinationSubScenePath);
            if (string.IsNullOrEmpty(sourceGuid) ||
                string.IsNullOrEmpty(destinationGuid) ||
                string.Equals(sourceGuid, destinationGuid, StringComparison.Ordinal))
            {
                error = "The staged operation-map subscene requires a distinct, non-empty Unity GUID.";
                return false;
            }

            if (!TryValidateSceneRoots(SourceSubScenePath, SourceRootNames, out error) ||
                !TryValidateSceneRoots(DestinationSubScenePath, MapRootNames, out error))
            {
                return false;
            }

            bool hasThinMatchShell = OperationMapCurrentMatchShellCutover.TryValidateThinShell(out _);
            if ((!hasThinMatchShell &&
                 !TryValidateSubSceneReference(
                     OperationMapCurrentCompatibilitySceneStager.SourceScenePath,
                     SourceSubScenePath,
                     out error)) ||
                !TryValidateSubSceneReference(
                    OperationMapCurrentCompatibilitySceneStager.DestinationScenePath,
                    DestinationSubScenePath,
                    out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        private static void ExtractMapOwnedRoots()
        {
            Scene scene = OpenScene(DestinationSubScenePath);
            try
            {
                List<GameObject> roots = GetRoots(scene);
                if (HasExactRootNames(roots, MapRootNames))
                    return;
                if (!HasExactRootNames(roots, SourceRootNames))
                    throw new InvalidOperationException("Staged Match subscene root identities drifted before extraction.");

                UnityEngine.Object.DestroyImmediate(roots[2]);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, DestinationSubScenePath, saveAsCopy: false))
                    throw new InvalidOperationException("Unity failed to save the staged operation-map subscene.");
            }
            finally
            {
                CloseScene(scene);
            }
        }

        private static void RebindStagedMapScene()
        {
            Scene scene = OpenScene(OperationMapCurrentCompatibilitySceneStager.DestinationScenePath);
            try
            {
                SubScene subScene = FindRequiredSubScene(scene);
                SceneAsset destination = AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationSubScenePath);
                if (subScene.SceneAsset != destination)
                {
                    subScene.SceneAsset = destination;
                    EditorUtility.SetDirty(subScene);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(
                            scene,
                            OperationMapCurrentCompatibilitySceneStager.DestinationScenePath,
                            saveAsCopy: false))
                    {
                        throw new InvalidOperationException("Unity failed to save the staged subscene reference.");
                    }
                }
            }
            finally
            {
                CloseScene(scene);
            }
        }

        private static bool TryValidateSceneRoots(
            string scenePath,
            IReadOnlyList<string> expected,
            out string error)
        {
            Scene scene;
            try
            {
                scene = OpenScene(scenePath);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            try
            {
                if (!HasExactRootNames(GetRoots(scene), expected))
                {
                    error = $"Scene '{scenePath}' does not contain its exact accepted root set.";
                    return false;
                }

                error = null;
                return true;
            }
            finally
            {
                CloseScene(scene);
            }
        }

        private static bool TryValidateSubSceneReference(
            string scenePath,
            string expectedSubScenePath,
            out string error)
        {
            Scene scene;
            try
            {
                scene = OpenScene(scenePath);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            try
            {
                SubScene subScene = FindRequiredSubScene(scene);
                string actualPath = AssetDatabase.GetAssetPath(subScene.SceneAsset);
                if (!string.Equals(actualPath, expectedSubScenePath, StringComparison.Ordinal))
                {
                    error = $"Scene '{scenePath}' references '{actualPath}' instead of '{expectedSubScenePath}'.";
                    return false;
                }

                error = null;
                return true;
            }
            finally
            {
                CloseScene(scene);
            }
        }

        private static Scene OpenScene(string scenePath)
        {
            Scene loaded = SceneManager.GetSceneByPath(scenePath);
            if (loaded.IsValid() && loaded.isLoaded)
                throw new InvalidOperationException($"Close '{scenePath}' before staging or validation.");

            return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        private static void CloseScene(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, removeScene: true);
        }

        private static SubScene FindRequiredSubScene(Scene scene)
        {
            List<GameObject> roots = GetRoots(scene);
            for (int index = 0; index < roots.Count; index++)
            {
                if (roots[index].TryGetComponent(out SubScene subScene))
                    return subScene;
            }

            throw new InvalidOperationException($"Scene '{scene.path}' has no root SubScene component.");
        }

        private static List<GameObject> GetRoots(Scene scene)
        {
            var roots = new List<GameObject>(SourceRootNames.Length);
            scene.GetRootGameObjects(roots);
            return roots;
        }

        private static bool HasExactRootNames(IReadOnlyList<GameObject> roots, IReadOnlyList<string> expected)
        {
            if (roots.Count != expected.Count)
                return false;

            for (int index = 0; index < expected.Count; index++)
            {
                if (!string.Equals(roots[index].name, expected[index], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }
    }
}

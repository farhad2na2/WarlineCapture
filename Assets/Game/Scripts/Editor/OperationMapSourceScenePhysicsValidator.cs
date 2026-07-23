using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapSourceScenePhysicsValidator
    {
        private static readonly string[] AcceptedScenePaths =
        {
            OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
            OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath
        };

        [MenuItem("Game/Operation Maps/EntityScene Migration/Validate Accepted Source Physics")]
        public static void ValidateAcceptedSources()
        {
            if (!TryValidateAcceptedSources(out string error))
                throw new InvalidOperationException(error);

            Debug.Log(
                "[OperationMapSourceScenePhysicsValidator] result=Passed " +
                $"scenes={AcceptedScenePaths.Length}");
        }

        public static void ValidateAcceptedSourcesBatch() => ValidateAcceptedSources();

        internal static bool TryValidateAcceptedSources(out string error)
        {
            var openedScenes = new List<Scene>(AcceptedScenePaths.Length);
            Scene activeScene = SceneManager.GetActiveScene();
            try
            {
                for (int index = 0; index < AcceptedScenePaths.Length; index++)
                {
                    string scenePath = AcceptedScenePaths[index];
                    Scene scene = SceneManager.GetSceneByPath(scenePath);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                        openedScenes.Add(scene);
                    }

                    if (!TryValidateLoadedScene(scene, scenePath, out error))
                        return false;
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = $"Accepted source physics validation failed: {exception.Message}";
                return false;
            }
            finally
            {
                for (int index = openedScenes.Count - 1; index >= 0; index--)
                {
                    Scene scene = openedScenes[index];
                    if (scene.IsValid() && scene.isLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                }

                if (activeScene.IsValid() && activeScene.isLoaded)
                    SceneManager.SetActiveScene(activeScene);
            }
        }

        internal static bool TryValidateLoadedScene(
            Scene scene,
            string expectedScenePath,
            out string error)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                error = $"Accepted source scene is not loaded: '{expectedScenePath}'.";
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            if (roots.Length == 0)
            {
                error = $"Accepted source scene has no hierarchy roots: '{expectedScenePath}'.";
                return false;
            }

            for (int index = 0; index < roots.Length; index++)
            {
                if (DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                        roots[index],
                        out string hierarchyError))
                {
                    continue;
                }

                error =
                    $"Accepted source scene '{expectedScenePath}' contains prohibited physics. " +
                    hierarchyError;
                return false;
            }

            error = null;
            return true;
        }
    }
}

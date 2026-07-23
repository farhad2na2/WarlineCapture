using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapEntitySceneRuntimePhysicsValidator
    {
        [MenuItem("Game/Operation Maps/EntityScene Migration/Validate Candidate Runtime Physics")]
        public static void ValidateCurrentCandidate()
        {
            if (!TryValidateCurrentCandidate(out string error))
                throw new InvalidOperationException(error);

            Debug.Log(
                "[OperationMapEntitySceneRuntimePhysicsValidator] result=Passed " +
                "assets=2");
        }

        public static void ValidateCurrentCandidateBatch() => ValidateCurrentCandidate();

        internal static bool TryValidateCurrentCandidate(out string error)
        {
            string bindingPath =
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath;
            string subScenePath =
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            var openedScenes = new List<Scene>(2);
            Scene activeScene = SceneManager.GetActiveScene();
            try
            {
                Scene bindingScene = OpenIfNeeded(bindingPath, openedScenes);
                Scene candidateSubScene = OpenIfNeeded(subScenePath, openedScenes);

                if (!OperationMapRuntimeBindingSceneValidator.TryValidateLoadedEntityScene(
                        bindingScene,
                        OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                        OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath,
                        subScenePath,
                        out error))
                {
                    return false;
                }

                if (!TryValidateSceneHierarchy(candidateSubScene, subScenePath, out error))
                    return false;

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = $"Candidate runtime physics validation failed: {exception.Message}";
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

        internal static bool TryValidateSceneHierarchy(
            Scene scene,
            string expectedScenePath,
            out string error)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                error = $"Candidate runtime scene is not loaded: '{expectedScenePath}'.";
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            if (roots.Length == 0)
            {
                error = $"Candidate runtime scene has no hierarchy roots: '{expectedScenePath}'.";
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
                    $"Candidate runtime scene '{expectedScenePath}' contains prohibited physics. " +
                    hierarchyError;
                return false;
            }

            error = null;
            return true;
        }

        private static Scene OpenIfNeeded(string scenePath, ICollection<Scene> openedScenes)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid() && scene.isLoaded)
                return scene;

            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            openedScenes.Add(scene);
            return scene;
        }
    }
}

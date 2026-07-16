using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapCurrentCompatibilityRootExtractor
    {
        private static readonly string[] SourceRootNames =
        {
            "Bootstrap",
            "MatchSubScene",
            "Start",
            "End",
            "Decorations",
            "Main Camera",
            "Reflection Probe",
            "Global Volume",
            "Directional Light",
            "Directional Light (1)",
            "Map",
            "Faction2",
            "Faction3",
            "Faction4",
            "Faction5",
            "Faction1"
        };

        private static readonly string[] ExtractedMapRootNames =
        {
            "MatchSubScene",
            "Start",
            "End",
            "Decorations",
            "Reflection Probe",
            "Map",
            "Faction2",
            "Faction3",
            "Faction4",
            "Faction5",
            "Faction1"
        };

        private static readonly string[] BoundMapRootNames =
        {
            "MatchSubScene",
            "Start",
            "End",
            "Decorations",
            "Reflection Probe",
            "Map",
            "Faction2",
            "Faction3",
            "Faction4",
            "Faction5",
            "Faction1",
            "OperationMapSceneView"
        };

        private static readonly HashSet<string> ShellRootNames = new(StringComparer.Ordinal)
        {
            "Bootstrap",
            "Main Camera",
            "Global Volume",
            "Directional Light",
            "Directional Light (1)"
        };

        [MenuItem("Tools/Warline Capture/Operation Maps/Extract Current Map Roots")]
        public static void Extract()
        {
            OperationMapCurrentCompatibilitySceneStager.Stage();
            Scene scene = OpenStagedScene();
            try
            {
                List<GameObject> roots = GetRoots(scene);
                if (HasExactRootNames(roots, ExtractedMapRootNames) ||
                    HasExactRootNames(roots, BoundMapRootNames))
                    return;
                if (!HasExactRootNames(roots, SourceRootNames))
                    throw new InvalidOperationException("Staged Match root identities drifted before extraction.");

                for (int index = roots.Count - 1; index >= 0; index--)
                {
                    if (ShellRootNames.Contains(roots[index].name))
                        UnityEngine.Object.DestroyImmediate(roots[index]);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(
                        scene,
                        OperationMapCurrentCompatibilitySceneStager.DestinationScenePath,
                        saveAsCopy: false))
                {
                    throw new InvalidOperationException("Unity failed to save the extracted operation-map scene.");
                }

                if (!HasExactRootNames(GetRoots(scene), ExtractedMapRootNames))
                    throw new InvalidOperationException("Extracted operation-map root validation failed.");
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        public static void ExtractForBatch() => Extract();

        public static bool TryValidate(out string error)
        {
            if (!OperationMapCurrentCompatibilitySceneStager.TryValidate(out error))
                return false;

            Scene scene;
            try
            {
                scene = OpenStagedScene();
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            try
            {
                List<GameObject> roots = GetRoots(scene);
                if (!HasExactRootNames(roots, ExtractedMapRootNames) &&
                    !HasExactRootNames(roots, BoundMapRootNames))
                {
                    error = "Staged operation-map scene does not contain the exact accepted map-root set.";
                    return false;
                }

                error = null;
                return true;
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        private static Scene OpenStagedScene()
        {
            Scene loaded = SceneManager.GetSceneByPath(
                OperationMapCurrentCompatibilitySceneStager.DestinationScenePath);
            if (loaded.IsValid() && loaded.isLoaded)
                throw new InvalidOperationException("Close the staged operation-map scene before running extraction validation.");

            return EditorSceneManager.OpenScene(
                OperationMapCurrentCompatibilitySceneStager.DestinationScenePath,
                OpenSceneMode.Additive);
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

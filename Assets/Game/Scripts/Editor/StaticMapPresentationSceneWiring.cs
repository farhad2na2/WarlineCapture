using System;
using System.Collections.Generic;
using Game.Composition;
using Game.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class StaticMapPresentationSceneWiring
    {
        private const string ManifestFieldName = "staticMapPresentationManifest";

        [MenuItem("Game/Tools/Performance/Wire Static Map Presentation Manifest")]
        public static void Wire()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    StaticMapPresentationBaker.CanonicalMatchScenePath,
                    OpenSceneMode.Single);
                MatchSceneView view = FindSingleMatchSceneView(scene);
                StaticMapPresentationManifest manifest = LoadValidatedManifest();

                SerializedObject serializedView = new(view);
                SerializedProperty manifestProperty = serializedView.FindProperty(ManifestFieldName);
                if (manifestProperty == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(MatchSceneView)} is missing serialized field {ManifestFieldName}.");
                }

                manifestProperty.objectReferenceValue = manifest;
                serializedView.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException($"Failed to save {scene.path} after manifest wiring.");

                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"[StaticMapPresentationSceneWiring] result=Passed scene={scene.path} " +
                    $"manifest={StaticMapPresentationBaker.ManifestPath} chunks={manifest.Chunks.Count} " +
                    $"contentHash={manifest.ContentHash}");
            }
            finally
            {
                if (!Application.isBatchMode && previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        internal static MatchSceneView FindSingleMatchSceneView(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Match scene must be valid and loaded before manifest wiring.");

            List<MatchSceneView> views = new();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                views.AddRange(roots[rootIndex].GetComponentsInChildren<MatchSceneView>(true));

            if (views.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one {nameof(MatchSceneView)} in {scene.path}, found {views.Count}.");
            }

            return views[0];
        }

        internal static StaticMapPresentationManifest LoadValidatedManifest()
        {
            StaticMapPresentationManifest manifest =
                AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(
                    StaticMapPresentationBaker.ManifestPath);
            if (manifest == null)
            {
                throw new InvalidOperationException(
                    $"Missing static map presentation manifest at {StaticMapPresentationBaker.ManifestPath}.");
            }
            if (!StaticMapPresentationManifest.IsSchemaReadable(manifest.SchemaVersion))
            {
                throw new InvalidOperationException(
                    $"Static map presentation manifest schema is {manifest.SchemaVersion}; " +
                    $"expected {StaticMapPresentationManifest.CurrentSchemaVersion}.");
            }
            if (!string.Equals(
                    manifest.CanonicalScenePath,
                    StaticMapPresentationBaker.CanonicalMatchScenePath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Manifest canonical scene is {manifest.CanonicalScenePath}; " +
                    $"expected {StaticMapPresentationBaker.CanonicalMatchScenePath}.");
            }
            if (manifest.Chunks.Count == 0 || string.IsNullOrWhiteSpace(manifest.ContentHash))
                throw new InvalidOperationException("Static map presentation manifest has no generated content.");

            return manifest;
        }
    }
}

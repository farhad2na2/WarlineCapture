using System;
using System.Collections.Generic;
using Game.Composition;
using Game.Configs;
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
                StaticMapPresentationManifest manifest = LoadValidatedManifest(
                    view.OperationMapCatalog,
                    view.OperationMapId,
                    scene.path);

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
                    $"operationMapId={view.OperationMapId} manifest={AssetDatabase.GetAssetPath(manifest)} " +
                    $"chunks={manifest.Chunks.Count} " +
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

        internal static StaticMapPresentationManifest LoadValidatedManifest(
            OperationMapCatalogConfig catalog,
            string operationMapId,
            string canonicalScenePath)
        {
            if (catalog == null)
                throw new InvalidOperationException("Selected operation-map catalog is missing.");
            if (!catalog.TryValidate(out string catalogError))
            {
                throw new InvalidOperationException(
                    $"Selected operation-map catalog is invalid: {catalogError}.");
            }
            if (!catalog.TryResolve(operationMapId, out OperationMapDefinition definition))
            {
                throw new InvalidOperationException(
                    $"Selected operation-map id '{operationMapId ?? "<null>"}' is not present in the catalog.");
            }
            if (!StaticMapPresentationOutputPathContract.TryResolveManifestAssetPath(
                    definition.OperationMapId,
                    out string manifestPath,
                    out string pathError))
            {
                throw new InvalidOperationException(pathError);
            }

            StaticMapPresentationManifest manifest =
                AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(
                    manifestPath);
            if (manifest == null)
            {
                throw new InvalidOperationException(
                    $"Missing static map presentation manifest for '{definition.OperationMapId}' at {manifestPath}.");
            }
            if (!StaticMapPresentationManifest.IsSchemaReadable(manifest.SchemaVersion))
            {
                throw new InvalidOperationException(
                    $"Static map presentation manifest schema is {manifest.SchemaVersion}; " +
                    $"expected {StaticMapPresentationManifest.CurrentSchemaVersion}.");
            }
            if (!string.Equals(manifest.OperationMapId, definition.OperationMapId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Manifest owner is '{manifest.OperationMapId}'; expected '{definition.OperationMapId}'.");
            }
            if (!string.Equals(manifest.CanonicalScenePath, canonicalScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Manifest canonical scene is {manifest.CanonicalScenePath}; " +
                    $"expected {canonicalScenePath}.");
            }
            if (manifest.Chunks.Count == 0 || string.IsNullOrWhiteSpace(manifest.ContentHash))
                throw new InvalidOperationException("Static map presentation manifest has no generated content.");

            return manifest;
        }
    }
}

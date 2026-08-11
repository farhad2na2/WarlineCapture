using System;
using Game.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Composition
{
    internal static class OperationMapPresentationManifestValidation
    {
        public static bool TryValidate(
            StaticMapPresentationManifest manifest,
            Scene scene,
            OperationMapSceneView view,
            string loadedSceneGuid,
            out string error)
        {
            if (manifest == null ||
                !StaticMapPresentationManifest.HasRequiredIdentity(
                    manifest.SchemaVersion,
                    manifest.OperationMapId,
                    manifest.CanonicalSceneGuid,
                    manifest.CanonicalScenePath))
            {
                error = "Operation-map presentation manifest identity is missing or unsupported.";
                return false;
            }

            if (!string.Equals(
                    view.Definition.SourceSceneReference.AssetGUID,
                    loadedSceneGuid,
                    StringComparison.Ordinal))
            {
                error = "Operation-map definition does not identify the loaded source scene.";
                return false;
            }

            string presentationSourceGuid = view.CanonicalPresentationMode ==
                OperationMapCanonicalPresentationMode.PresentationOnly
                    ? view.PresentationSourceSceneGuid
                    : loadedSceneGuid;
            string presentationSourcePath = view.CanonicalPresentationMode ==
                OperationMapCanonicalPresentationMode.PresentationOnly
                    ? view.PresentationSourceScenePath
                    : scene.path;
            if (!string.Equals(manifest.OperationMapId, view.OperationMapId, StringComparison.Ordinal) ||
                !string.Equals(manifest.CanonicalSceneGuid, presentationSourceGuid, StringComparison.Ordinal) ||
                !string.Equals(manifest.CanonicalScenePath, presentationSourcePath, StringComparison.Ordinal))
            {
                error = "Operation-map presentation manifest does not match the declared authoring source.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(manifest.ContentHash) ||
                string.IsNullOrWhiteSpace(manifest.CanonicalSceneDependencyHash) ||
                manifest.ChunkSize <= 0f ||
                manifest.Chunks == null || manifest.Chunks.Count == 0 ||
                manifest.Sources == null || manifest.Sources.Count == 0)
            {
                error = "Operation-map presentation manifest content is incomplete.";
                return false;
            }

            error = null;
            return true;
        }
    }
}

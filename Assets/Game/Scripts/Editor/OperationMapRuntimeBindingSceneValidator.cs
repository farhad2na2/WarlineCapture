using System;
using Game.Composition;
using Game.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapRuntimeBindingSceneValidator
    {
        public static bool TryValidateLoadedScene(
            Scene scene,
            string expectedOperationMapId,
            out string error)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                error = "Runtime binding scene is not loaded.";
                return false;
            }

            OperationMapSceneView view = null;
            int rootCount = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                rootCount++;
                OperationMapSceneView[] views =
                    root.GetComponentsInChildren<OperationMapSceneView>(true);
                for (int index = 0; index < views.Length; index++)
                {
                    if (view != null)
                    {
                        error = "Runtime binding scene contains multiple operation-map views.";
                        return false;
                    }

                    view = views[index];
                }
            }

            if (rootCount != 2 || view == null)
            {
                error = "Runtime binding scene requires exactly two roots and one operation-map view.";
                return false;
            }
            if (!string.Equals(view.OperationMapId, expectedOperationMapId, StringComparison.Ordinal) ||
                view.CanonicalPresentationMode != OperationMapCanonicalPresentationMode.PresentationOnly)
            {
                error = "Runtime binding scene identity or presentation mode is invalid.";
                return false;
            }
            if (!view.TryValidate(out error))
                return false;
            if (!string.Equals(
                    view.PresentationSourceSceneGuid,
                    AssetDatabase.AssetPathToGUID(OperationMapAddressablesLayoutBuilder.AuthoringScenePath),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    view.PresentationSourceScenePath,
                    OperationMapAddressablesLayoutBuilder.AuthoringScenePath,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(view.Definition),
                    OperationMapAddressablesLayoutBuilder.DefinitionPath,
                    StringComparison.Ordinal))
            {
                error = "Runtime binding scene authoring-source or definition identity is invalid.";
                return false;
            }
            if (view.MapRoot.GetComponentsInChildren<Renderer>(true).Length != 0 ||
                view.MapRoot.GetComponentsInChildren<Collider>(true).Length != 0 ||
                view.MapRoot.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
                view.MapRoot.GetComponentsInChildren<Camera>(true).Length != 0 ||
                view.MapRoot.GetComponentsInChildren<Light>(true).Length != 0)
            {
                error = "Runtime binding map root contains visual, camera, lighting, or unmanaged physics content.";
                return false;
            }
            if (view.DecorationRoot.childCount != 0 ||
                view.BuildingAuthoringRoot.childCount != 0 ||
                view.VehicleAuthoringRoot.childCount != 0)
            {
                error = "Runtime binding presentation and placement roots must remain empty.";
                return false;
            }

            error = null;
            return true;
        }
    }
}

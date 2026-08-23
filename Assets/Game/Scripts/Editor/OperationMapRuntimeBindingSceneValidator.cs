using System;
using Game.Composition;
using Game.Configs;
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
            if (!TryValidateStructure(
                    scene,
                    expectedOperationMapId,
                    OperationMapCanonicalPresentationMode.PresentationOnly,
                    out OperationMapSceneView view,
                    out error))
            {
                return false;
            }

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

            error = null;
            return true;
        }

        public static bool TryValidateLoadedEntityScene(
            Scene scene,
            string expectedOperationMapId,
            string expectedDefinitionPath,
            string expectedSubScenePath,
            out string error)
        {
            if (!TryValidateStructure(
                    scene,
                    expectedOperationMapId,
                    OperationMapCanonicalPresentationMode.EntityScene,
                    out OperationMapSceneView view,
                    out error))
            {
                return false;
            }

            if (view.Definition.PresentationKind != OperationMapPresentationKind.EntityScene ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(view.Definition),
                    expectedDefinitionPath,
                    StringComparison.Ordinal) ||
                view.MapSubScene == null ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(view.MapSubScene.SceneAsset),
                    expectedSubScenePath,
                    StringComparison.Ordinal))
            {
                error = "EntityScene runtime binding definition or SubScene identity is invalid.";
                return false;
            }
            if (!view.MapSubScene.AutoLoadScene)
            {
                error = "EntityScene runtime binding SubScene must auto-load.";
                return false;
            }
            if (view.BuildingPlacements != null || view.VehiclePlacements != null)
            {
                error = "EntityScene runtime binding must not retain legacy placement configs.";
                return false;
            }
            if (!string.IsNullOrWhiteSpace(view.PresentationSourceSceneGuid) ||
                !string.IsNullOrWhiteSpace(view.PresentationSourceScenePath))
            {
                error = "EntityScene runtime binding must not retain PresentationOnly source identity.";
                return false;
            }
            if (view.MapSurfaceAuthoring.SceneOverlays.Length == 0)
            {
                error = "EntityScene runtime binding is missing serialized road surface overlays.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidateStructure(
            Scene scene,
            string expectedOperationMapId,
            OperationMapCanonicalPresentationMode expectedPresentationMode,
            out OperationMapSceneView view,
            out string error)
        {
            view = null;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                error = "Runtime binding scene is not loaded.";
                return false;
            }

            int rootCount = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                rootCount++;
                if (!DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                        root,
                        out error))
                {
                    error =
                        $"Runtime binding scene contains prohibited collider or rigidbody physics. {error}";
                    return false;
                }

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
                view.CanonicalPresentationMode != expectedPresentationMode)
            {
                error = "Runtime binding scene identity or presentation mode is invalid.";
                return false;
            }
            if (!view.TryValidate(out error))
                return false;
            if (view.MapRoot.GetComponentsInChildren<Renderer>(true).Length != 0)
            {
                error = "Runtime binding map root contains renderer content.";
                return false;
            }
            if (view.MapRoot.GetComponentsInChildren<Camera>(true).Length != 0 ||
                view.MapRoot.GetComponentsInChildren<Light>(true).Length != 0)
            {
                error = "Runtime binding map root contains camera or lighting content.";
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

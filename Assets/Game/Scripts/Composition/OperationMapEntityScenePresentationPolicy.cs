using Game.Configs;
using Game.Rendering;
using UnityEngine;

namespace Game.Composition
{
    /// <summary>
    /// Fail-closed EntityScene presentation policy. Production maps remain on
    /// <see cref="OperationMapPresentationKind.StaticSceneChunks"/> until an accepted cutover
    /// flips both definition kind and canonical mode together.
    /// </summary>
    public static class OperationMapEntityScenePresentationPolicy
    {
        public static bool UsesEntityScenePresentation(OperationMapDefinition definition) =>
            definition != null &&
            definition.PresentationKind == OperationMapPresentationKind.EntityScene;

        public static bool ShouldSkipStaticManifestStreamerAndOwnership(
            OperationMapDefinition definition) =>
            UsesEntityScenePresentation(definition);

        public static bool TryValidateEntitySceneBinding(
            OperationMapDefinition definition,
            OperationMapCanonicalPresentationMode canonicalPresentationMode,
            Transform mapRoot,
            Transform buildingAuthoringRoot,
            Transform vehicleAuthoringRoot,
            Unity.Scenes.SubScene mapSubScene,
            MapBuildingPlacementConfig buildingPlacements,
            MapVehiclePlacementConfig vehiclePlacements,
            out string error)
        {
            if (!UsesEntityScenePresentation(definition))
            {
                error = "EntityScene binding requires OperationMapPresentationKind.EntityScene.";
                return false;
            }

            if (canonicalPresentationMode != OperationMapCanonicalPresentationMode.EntityScene)
            {
                error =
                    "EntityScene presentation kind requires OperationMapCanonicalPresentationMode.EntityScene.";
                return false;
            }

            if (mapSubScene == null)
            {
                error = "EntityScene operation maps require a bound map SubScene.";
                return false;
            }

            if (mapRoot == null || buildingAuthoringRoot == null || vehicleAuthoringRoot == null)
            {
                error = "EntityScene operation maps require map and legacy placement root references.";
                return false;
            }

            if (mapRoot.GetComponentInChildren<MeshRenderer>(true) != null)
            {
                error =
                    "EntityScene binding scenes must be renderer-free; map visuals belong to the SubScene.";
                return false;
            }

            // Empty legacy placement configs are accepted as migration evidence only.
            // Non-null empty configs are allowed; null is also allowed for EntityScene.
            if (buildingPlacements != null && buildingPlacements.Placements == null)
            {
                error = "EntityScene building placement evidence list is corrupt.";
                return false;
            }

            if (vehiclePlacements != null && vehiclePlacements.Placements == null)
            {
                error = "EntityScene vehicle placement evidence list is corrupt.";
                return false;
            }

            error = null;
            return true;
        }
    }
}

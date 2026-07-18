using System;
using Game.Authoring;
using Game.Configs;
using Game.Runtime;
using Unity.Scenes;
using UnityEngine;

namespace Game.Composition
{
    [DisallowMultipleComponent]
    public sealed class OperationMapSceneView : MonoBehaviour
    {
        [SerializeField] private string operationMapId;
        [SerializeField] private OperationMapDefinition definition;
        [SerializeField] private Transform mapRoot;
        [SerializeField] private CombinedMeshBaker decorationCombinedMeshBaker;
        [SerializeField] private Transform decorationRoot;
        [SerializeField] private Transform buildingAuthoringRoot;
        [SerializeField] private Transform vehicleAuthoringRoot;
        [SerializeField] private MapSurfaceAuthoring mapSurfaceAuthoring;
        [SerializeField] private GridAuthoringConfig gridAuthoringConfig;
        [SerializeField] private MapBuildingPlacementConfig buildingPlacements;
        [SerializeField] private MapVehiclePlacementConfig vehiclePlacements;
        [SerializeField] private SubScene mapSubScene;

        public string OperationMapId => operationMapId;
        public OperationMapDefinition Definition => definition;
        public Transform MapRoot => mapRoot;
        public CombinedMeshBaker DecorationCombinedMeshBaker => decorationCombinedMeshBaker;
        public Transform DecorationRoot => decorationRoot;
        public Transform BuildingAuthoringRoot => buildingAuthoringRoot;
        public Transform VehicleAuthoringRoot => vehicleAuthoringRoot;
        public MapSurfaceAuthoring MapSurfaceAuthoring => mapSurfaceAuthoring;
        public GridAuthoringConfig GridAuthoringConfig => gridAuthoringConfig;
        public MapBuildingPlacementConfig BuildingPlacements => buildingPlacements;
        public MapVehiclePlacementConfig VehiclePlacements => vehiclePlacements;
        public SubScene MapSubScene => mapSubScene;

        public bool TryValidate(out string error)
        {
            if (definition == null ||
                !string.Equals(operationMapId, definition.OperationMapId, StringComparison.Ordinal))
            {
                error = "Operation-map view identity does not match its definition.";
                return false;
            }

            if (mapRoot == null || decorationCombinedMeshBaker == null ||
                decorationRoot == null || buildingAuthoringRoot == null ||
                vehicleAuthoringRoot == null || mapSurfaceAuthoring == null ||
                gridAuthoringConfig == null ||
                mapSubScene == null)
            {
                error = "Operation-map view requires map-owned presentation, placement, surface, and subscene references.";
                return false;
            }

            if (buildingPlacements == null || buildingPlacements.Placements.Count == 0 ||
                vehiclePlacements == null || vehiclePlacements.Placements.Count == 0)
            {
                error = "Operation-map view requires non-empty building and vehicle placement configs.";
                return false;
            }

            if (mapRoot.gameObject.scene != gameObject.scene ||
                decorationCombinedMeshBaker.gameObject.scene != gameObject.scene ||
                decorationRoot.gameObject.scene != gameObject.scene ||
                buildingAuthoringRoot.gameObject.scene != gameObject.scene ||
                vehicleAuthoringRoot.gameObject.scene != gameObject.scene ||
                mapSurfaceAuthoring.gameObject.scene != gameObject.scene ||
                mapSubScene.gameObject.scene != gameObject.scene)
            {
                error = "Operation-map scene references must belong to the same scene as the view.";
                return false;
            }

            if (decorationCombinedMeshBaker.transform != decorationRoot ||
                !buildingAuthoringRoot.IsChildOf(mapRoot) ||
                !vehicleAuthoringRoot.IsChildOf(mapRoot))
            {
                error = "Operation-map presentation and placement roots must remain under the map root.";
                return false;
            }

            if (!string.Equals(
                    definition.NavigationMetadata.AuthoredSubSceneGuid,
                    mapSubScene.SceneGUID.ToString(),
                    StringComparison.Ordinal))
            {
                error = "Operation-map definition does not identify the bound map subscene.";
                return false;
            }

            error = null;
            return true;
        }
    }
}

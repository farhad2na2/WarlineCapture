using System;
using Game.Authoring;
using Game.Configs;
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
        [SerializeField] private MapSurfaceAuthoring mapSurfaceAuthoring;
        [SerializeField] private MapBuildingPlacementConfig buildingPlacements;
        [SerializeField] private MapVehiclePlacementConfig vehiclePlacements;
        [SerializeField] private SubScene mapSubScene;

        public string OperationMapId => operationMapId;
        public OperationMapDefinition Definition => definition;
        public Transform MapRoot => mapRoot;
        public MapSurfaceAuthoring MapSurfaceAuthoring => mapSurfaceAuthoring;
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

            if (mapRoot == null || mapSurfaceAuthoring == null || mapSubScene == null)
            {
                error = "Operation-map view requires map, surface, and subscene references.";
                return false;
            }

            if (buildingPlacements == null || buildingPlacements.Placements.Count == 0 ||
                vehiclePlacements == null || vehiclePlacements.Placements.Count == 0)
            {
                error = "Operation-map view requires non-empty building and vehicle placement configs.";
                return false;
            }

            if (mapRoot.gameObject.scene != gameObject.scene ||
                mapSurfaceAuthoring.gameObject.scene != gameObject.scene ||
                mapSubScene.gameObject.scene != gameObject.scene)
            {
                error = "Operation-map scene references must belong to the same scene as the view.";
                return false;
            }

            error = null;
            return true;
        }
    }
}

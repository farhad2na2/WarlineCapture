using UnityEngine;
using Game.Authoring;
using Game.Runtime;

namespace Game.Composition
{
    internal static class BuildingProductionUnitMetadataPrefabSystemHelper
    {
        public static void PrepareTransportDropVisual(GameObject visual)
        {
            if (visual != null && visual.TryGetComponent(out UnitGridAuthoring authoring))
                authoring.enabled = false;
        }

        public static bool TryGetMetadata(GameObject prefab, out BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata metadata)
        {
            if (prefab != null && prefab.TryGetComponent(out UnitGridAuthoring authoring))
            {
                metadata = new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(
                    authoring.ProductionDurationSeconds,
                    authoring.ProductionTransportPrefab,
                    authoring.IsAirUnit,
                    authoring.ProductionTransportArrivalSeconds,
                    authoring.ProductionTransportHoldForNextReadySeconds,
                    authoring.ProductionTransportMaxConcurrent,
                    authoring.ProductionTransportRequiresAirportRunway,
                    authoring.ProductionTransportUsesRunwayLanding,
                    authoring.GetConfiguredFootprintCells());
                return true;
            }

            metadata = default;
            return false;
        }
    }
}

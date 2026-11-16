using UnityEngine;
using Game.Components;
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
                Game.Components.UnitProductionMetadataScalarValues scalarMetadata =
                    Game.Components.UnitProductionMetadataScalarPolicy.Project(authoring.CaptureProductionMetadataScalarSource());
                metadata = new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(
                    scalarMetadata.ProductionDurationSeconds,
                    authoring.ProductionTransportPrefab,
                    scalarMetadata.IsAirUnit,
                    scalarMetadata.ProductionTransportArrivalSeconds,
                    scalarMetadata.ProductionTransportHoldForNextReadySeconds,
                    scalarMetadata.ProductionTransportMaxConcurrent,
                    scalarMetadata.ProductionTransportRequiresAirportRunway,
                    scalarMetadata.ProductionTransportUsesRunwayLanding,
                    new Vector2Int(scalarMetadata.FootprintWidth, scalarMetadata.FootprintDepth));
                return true;
            }

            metadata = default;
            return false;
        }
    }
}

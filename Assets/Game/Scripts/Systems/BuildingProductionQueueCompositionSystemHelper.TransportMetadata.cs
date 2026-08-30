using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class BuildingProductionQueueCompositionSystemHelper
    {
        public readonly struct ProductionTransportSettings
        {
            public readonly GameObject TransportPrefab;
            public readonly float ArrivalSeconds;
            public readonly float HoldForNextReadySeconds;
            public readonly int MaxConcurrent;
            public readonly ProductionTransportMode Mode;
            public readonly bool RequiresAirportRunway;

            public ProductionTransportSettings(
                GameObject transportPrefab,
                float arrivalSeconds,
                float holdForNextReadySeconds,
                int maxConcurrent,
                ProductionTransportMode mode,
                bool requiresAirportRunway)
            {
                TransportPrefab = transportPrefab;
                ArrivalSeconds = arrivalSeconds;
                HoldForNextReadySeconds = holdForNextReadySeconds;
                MaxConcurrent = maxConcurrent;
                Mode = mode;
                RequiresAirportRunway = requiresAirportRunway;
            }
        }

        public readonly struct UnitProductionMetadata
        {
            public readonly float ProductionDurationSeconds;
            public readonly GameObject ProductionTransportPrefab;
            public readonly bool IsAirUnit;
            public readonly float ProductionTransportArrivalSeconds;
            public readonly float ProductionTransportHoldForNextReadySeconds;
            public readonly int ProductionTransportMaxConcurrent;
            public readonly bool ProductionTransportRequiresAirportRunway;
            public readonly bool ProductionTransportUsesRunwayLanding;
            public readonly Vector2Int FootprintCells;

            public UnitProductionMetadata(
                float productionDurationSeconds,
                GameObject productionTransportPrefab,
                bool isAirUnit,
                float productionTransportArrivalSeconds,
                float productionTransportHoldForNextReadySeconds,
                int productionTransportMaxConcurrent,
                bool productionTransportRequiresAirportRunway,
                bool productionTransportUsesRunwayLanding,
                Vector2Int footprintCells)
            {
                ProductionDurationSeconds = productionDurationSeconds;
                ProductionTransportPrefab = productionTransportPrefab;
                IsAirUnit = isAirUnit;
                ProductionTransportArrivalSeconds = productionTransportArrivalSeconds;
                ProductionTransportHoldForNextReadySeconds = productionTransportHoldForNextReadySeconds;
                ProductionTransportMaxConcurrent = productionTransportMaxConcurrent;
                ProductionTransportRequiresAirportRunway = productionTransportRequiresAirportRunway;
                ProductionTransportUsesRunwayLanding = productionTransportUsesRunwayLanding;
                FootprintCells = footprintCells;
            }
        }
    }
}

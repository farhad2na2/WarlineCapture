using UnityEngine;

namespace Game.Components
{
    /// <summary>
    /// Inputs for the existing unit metadata scalar policy. Native authoring supplies
    /// serialized getter inputs; the isolated source adapter may explicitly supply
    /// already-resolved original getter values.
    /// </summary>
    public readonly struct UnitProductionMetadataScalarSource
    {
        public readonly bool HasConfig;
        public readonly bool ValuesAreResolvedGetterSnapshot;
        public readonly bool PrefabIsAirUnit;
        public readonly bool ConfigIsAirUnit;
        public readonly bool PrefabRequiresAirportRunway;
        public readonly bool ConfigRequiresAirportRunway;
        public readonly bool PrefabUsesRunwayLanding;
        public readonly bool ConfigUsesRunwayLanding;
        public readonly float ProductionDurationSeconds;
        public readonly float ProductionTransportArrivalSeconds;
        public readonly float ProductionTransportHoldForNextReadySeconds;
        public readonly int ProductionTransportMaxConcurrent;
        public readonly int FootprintWidth;
        public readonly int FootprintDepth;

        public UnitProductionMetadataScalarSource(
            bool hasConfig,
            bool valuesAreResolvedGetterSnapshot,
            bool prefabIsAirUnit,
            bool configIsAirUnit,
            bool prefabRequiresAirportRunway,
            bool configRequiresAirportRunway,
            bool prefabUsesRunwayLanding,
            bool configUsesRunwayLanding,
            float productionDurationSeconds,
            float productionTransportArrivalSeconds,
            float productionTransportHoldForNextReadySeconds,
            int productionTransportMaxConcurrent,
            int footprintWidth,
            int footprintDepth)
        {
            HasConfig = hasConfig;
            ValuesAreResolvedGetterSnapshot = valuesAreResolvedGetterSnapshot;
            PrefabIsAirUnit = prefabIsAirUnit;
            ConfigIsAirUnit = configIsAirUnit;
            PrefabRequiresAirportRunway = prefabRequiresAirportRunway;
            ConfigRequiresAirportRunway = configRequiresAirportRunway;
            PrefabUsesRunwayLanding = prefabUsesRunwayLanding;
            ConfigUsesRunwayLanding = configUsesRunwayLanding;
            ProductionDurationSeconds = productionDurationSeconds;
            ProductionTransportArrivalSeconds = productionTransportArrivalSeconds;
            ProductionTransportHoldForNextReadySeconds = productionTransportHoldForNextReadySeconds;
            ProductionTransportMaxConcurrent = productionTransportMaxConcurrent;
            FootprintWidth = footprintWidth;
            FootprintDepth = footprintDepth;
        }
    }

    /// <summary>Scalar values returned by the original unit production metadata owner.</summary>
    public readonly struct UnitProductionMetadataScalarValues
    {
        public readonly bool IsAirUnit;
        public readonly bool ProductionTransportRequiresAirportRunway;
        public readonly bool ProductionTransportUsesRunwayLanding;
        public readonly float ProductionDurationSeconds;
        public readonly float ProductionTransportArrivalSeconds;
        public readonly float ProductionTransportHoldForNextReadySeconds;
        public readonly int ProductionTransportMaxConcurrent;
        public readonly int FootprintWidth;
        public readonly int FootprintDepth;

        internal UnitProductionMetadataScalarValues(
            bool isAirUnit,
            bool productionTransportRequiresAirportRunway,
            bool productionTransportUsesRunwayLanding,
            float productionDurationSeconds,
            float productionTransportArrivalSeconds,
            float productionTransportHoldForNextReadySeconds,
            int productionTransportMaxConcurrent,
            int footprintWidth,
            int footprintDepth)
        {
            IsAirUnit = isAirUnit;
            ProductionTransportRequiresAirportRunway = productionTransportRequiresAirportRunway;
            ProductionTransportUsesRunwayLanding = productionTransportUsesRunwayLanding;
            ProductionDurationSeconds = productionDurationSeconds;
            ProductionTransportArrivalSeconds = productionTransportArrivalSeconds;
            ProductionTransportHoldForNextReadySeconds = productionTransportHoldForNextReadySeconds;
            ProductionTransportMaxConcurrent = productionTransportMaxConcurrent;
            FootprintWidth = footprintWidth;
            FootprintDepth = footprintDepth;
        }
    }

    /// <summary>
    /// Shared data-only policy for the existing unit production metadata owner.
    /// Operand order matches UnitGridAuthoring's current Mathf.Max getters.
    /// </summary>
    public static class UnitProductionMetadataScalarPolicy
    {
        public static UnitProductionMetadataScalarValues Project(UnitProductionMetadataScalarSource source)
        {
            if (source.ValuesAreResolvedGetterSnapshot)
            {
                return new UnitProductionMetadataScalarValues(
                    source.PrefabIsAirUnit,
                    source.PrefabRequiresAirportRunway,
                    source.PrefabUsesRunwayLanding,
                    source.ProductionDurationSeconds,
                    source.ProductionTransportArrivalSeconds,
                    source.ProductionTransportHoldForNextReadySeconds,
                    source.ProductionTransportMaxConcurrent,
                    source.FootprintWidth,
                    source.FootprintDepth);
            }

            return new UnitProductionMetadataScalarValues(
                source.HasConfig ? source.ConfigIsAirUnit : source.PrefabIsAirUnit,
                source.HasConfig ? source.ConfigRequiresAirportRunway : source.PrefabRequiresAirportRunway,
                source.HasConfig ? source.ConfigUsesRunwayLanding : source.PrefabUsesRunwayLanding,
                Mathf.Max(0.01f, source.ProductionDurationSeconds),
                Mathf.Max(0.01f, source.ProductionTransportArrivalSeconds),
                Mathf.Max(0.01f, source.ProductionTransportHoldForNextReadySeconds),
                Mathf.Max(1, source.ProductionTransportMaxConcurrent),
                source.FootprintWidth,
                source.FootprintDepth);
        }
    }
}

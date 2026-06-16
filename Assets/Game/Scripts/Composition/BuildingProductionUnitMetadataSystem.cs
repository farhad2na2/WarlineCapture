using Unity.Entities;
using UnityEngine;

internal sealed partial class BuildingProductionUnitMetadataSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public static void PrepareTransportDropVisual(GameObject visual)
    {
        if (visual != null && visual.TryGetComponent(out UnitGridAuthoring authoring))
            authoring.enabled = false;
    }

    public static bool TryGetMetadata(GameObject prefab, out BuildingProductionSystem.UnitProductionMetadata metadata)
    {
        if (prefab != null && prefab.TryGetComponent(out UnitGridAuthoring authoring))
        {
            metadata = new BuildingProductionSystem.UnitProductionMetadata(
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

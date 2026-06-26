using System;

internal sealed class BuildingProductionTickCompositionSystemHelper
{
    public BuildingProductionRuntimeTickCompositionSystemHelper.Context Create(
        BuildingGameplaySourceCompositionSystemHelper source,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingProductionContextCompositionSystemHelper.Source> createProductionRuntimeContextSource,
        float oilBarrelsPerFuelBarrel)
    {
        BuildingProductionContextCompositionSystemHelper.Source productionSource = createProductionRuntimeContextSource(source);
        return new BuildingProductionRuntimeTickCompositionSystemHelper.Context(
            source.RuntimeBuildingSystem.Buildings,
            source.BuildingGameplayDependencyCompositionSystemHelper.DayNightSystem,
            source.FactionResourceCompositionSystemHelper,
            source.BuildingProductionUpdateCompositionSystemHelper,
            source.BuildingProductionContextCompositionSystemHelper.CreateProductionUpdateContext(productionSource),
            source.BuildingResourceHaulerBridgeCompositionSystemHelper,
            source.BuildingProductionContextCompositionSystemHelper.CreateResourceHaulerBridgeContext(productionSource),
            source.BuildingSpawnCompositionSystemHelper,
            () => source.BuildingSpawnRandomState,
            value => source.BuildingSpawnRandomState = value,
            GameRuntimeStats.RecordOilExtracted,
            GameRuntimeStats.RecordFuelProduced,
            source.UnitPathfindingPendingStateReader.HasPendingPathJob,
            oilBarrelsPerFuelBarrel);
    }
}

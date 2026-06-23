using System;

internal sealed class BuildingProductionTickCompositionSystemHelper
{
    public BuildingProductionRuntimeTickSystem.Context Create(
        BuildingGameplaySourceCompositionSystemHelper source,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingProductionContextCompositionSystemHelper.Source> createProductionRuntimeContextSource,
        float oilBarrelsPerFuelBarrel)
    {
        BuildingProductionContextCompositionSystemHelper.Source productionSource = createProductionRuntimeContextSource(source);
        return new BuildingProductionRuntimeTickSystem.Context(
            source.RuntimeBuildingSystem.Buildings,
            source.BuildingGameplayDependencySystem.DayNightSystem,
            source.FactionResourceSystem,
            source.BuildingProductionUpdateSystem,
            source.BuildingProductionContextCompositionSystemHelper.CreateProductionUpdateContext(productionSource),
            source.BuildingResourceHaulerBridgeSystem,
            source.BuildingProductionContextCompositionSystemHelper.CreateResourceHaulerBridgeContext(productionSource),
            source.BuildingSpawnSystem,
            () => source.BuildingSpawnRandomState,
            value => source.BuildingSpawnRandomState = value,
            GameRuntimeStats.RecordOilExtracted,
            GameRuntimeStats.RecordFuelProduced,
            source.UnitPathfindingPendingStateReader.HasPendingPathJob,
            oilBarrelsPerFuelBarrel);
    }
}

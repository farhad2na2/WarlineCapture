using System;

internal sealed class BuildingProductionTickCompositionSystemHelper
{
    public BuildingProductionRuntimeTickSystem.Context Create(
        BuildingGameplayCompositionSourceSystem source,
        Func<BuildingGameplayCompositionSourceSystem, BuildingProductionContextSystem.Source> createProductionRuntimeContextSource,
        float oilBarrelsPerFuelBarrel)
    {
        BuildingProductionContextSystem.Source productionSource = createProductionRuntimeContextSource(source);
        return new BuildingProductionRuntimeTickSystem.Context(
            source.RuntimeBuildingSystem.Buildings,
            source.BuildingGameplayDependencySystem.DayNightSystem,
            source.FactionResourceSystem,
            source.BuildingProductionUpdateSystem,
            source.BuildingProductionContextSystem.CreateProductionUpdateContext(productionSource),
            source.BuildingResourceHaulerBridgeSystem,
            source.BuildingProductionContextSystem.CreateResourceHaulerBridgeContext(productionSource),
            source.BuildingSpawnSystem,
            () => source.BuildingSpawnRandomState,
            value => source.BuildingSpawnRandomState = value,
            GameRuntimeStats.RecordOilExtracted,
            GameRuntimeStats.RecordFuelProduced,
            source.UnitPathfindingPendingStateReader.HasPendingPathJob,
            oilBarrelsPerFuelBarrel);
    }
}

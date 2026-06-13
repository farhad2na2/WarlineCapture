using System;
using UnityEngine;

internal sealed class BuildingRuntimeBoundaryCompositionSystem
{
    public BuildingRuntimeBoundaryPublishSystem.Context Create(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingRuntimeContextSystem.Source> createBuildingRuntimeContextSource,
        Func<BuildingGameplayCompositionSourceSystem, BuildingProductionContextSystem.Source> createProductionRuntimeContextSource,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource)
    {
        return new BuildingRuntimeBoundaryPublishSystem.Context(
            source.BuildingEntityManagerAccessSystem.TryGetEntityManager,
            source.BuildingGameplayEcsQuerySystem.EnsureEntityQueries,
            source.BuildingRuntimeBoundarySystem,
            source.BuildingDefinitionSystem,
            source.BuildingRuntimeSpawnSystem,
            source.BuildingRuntimeContextSystem.CreateSpawnContext(createBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock)),
            source.BuildingProductionRequestSystem,
            source.BuildingProductionContextSystem.CreateProductionRequestContext(createProductionRuntimeContextSource(source)),
            source.BuildingRuntimeQuerySystem,
            source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
            source.FactionResourceSystem,
            () => source.BuildingGameplayEcsQuerySystem.BuildingRuntimeBoundaryQuery,
            source.RuntimeBuildingSystem.Buildings);
    }
}

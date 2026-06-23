using System;
using UnityEngine;

internal sealed class BuildingRuntimeBoundaryCompositionSystemHelper
{
    public BuildingRuntimeBoundaryPublishCompositionSystemHelper.Context Create(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingRuntimeContextSystem.Source> createBuildingRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingProductionContextCompositionSystemHelper.Source> createProductionRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource)
    {
        return new BuildingRuntimeBoundaryPublishCompositionSystemHelper.Context(
            source.BuildingEntityManagerAccessSystem.TryGetEntityManager,
            source.BuildingGameplayEcsQuerySystem.EnsureEntityQueries,
            source.BuildingRuntimeBoundarySystem,
            source.BuildingDefinitionSystem,
            source.BuildingRuntimeSpawnSystem,
            source.BuildingRuntimeContextSystem.CreateSpawnContext(createBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock)),
            source.BuildingProductionRequestBoundary,
            source.BuildingProductionContextCompositionSystemHelper.CreateProductionRequestContext(createProductionRuntimeContextSource(source)),
            source.BuildingRuntimeQuerySystem,
            source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
            source.FactionResourceSystem,
            () => source.BuildingGameplayEcsQuerySystem.BuildingRuntimeBoundaryQuery,
            source.RuntimeBuildingSystem.Buildings);
    }
}

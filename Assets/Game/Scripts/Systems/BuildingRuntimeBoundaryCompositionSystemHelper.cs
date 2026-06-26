using System;
using UnityEngine;

internal sealed class BuildingRuntimeBoundaryCompositionSystemHelper
{
    public BuildingRuntimeBoundaryPublishCompositionSystemHelper.Context Create(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingRuntimeContextFactoryCompositionSystemHelper.Source> createBuildingRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingProductionContextCompositionSystemHelper.Source> createProductionRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource> createRuntimeContextSource)
    {
        return new BuildingRuntimeBoundaryPublishCompositionSystemHelper.Context(
            source.BuildingEntityManagerAccessSystem.TryGetEntityManager,
            source.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries,
            source.BuildingRuntimeBoundaryProcessingCompositionSystemHelper,
            source.BuildingDefinitionPrefabSystemHelper,
            source.BuildingRuntimeSpawnCompositionSystemHelper,
            source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateSpawnContext(createBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock)),
            source.BuildingProductionRequestBoundary,
            source.BuildingProductionContextCompositionSystemHelper.CreateProductionRequestContext(createProductionRuntimeContextSource(source)),
            source.BuildingRuntimeReadModelCompositionSystemHelper,
            source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
            source.FactionResourceCompositionSystemHelper,
            () => source.BuildingGameplayEcsQueryCompositionSystemHelper.BuildingRuntimeBoundaryQuery,
            source.RuntimeBuildingSystem.Buildings);
    }
}

using System;
using UnityEngine;

internal sealed class BuildingRuntimeCompositionSystemHelper
{
    public BuildingRuntimePublishCompositionSystemHelper.Context Create(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingRuntimeContextFactoryCompositionSystemHelper.Source> createBuildingRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingProductionContextCompositionSystemHelper.Source> createProductionRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource> createRuntimeContextSource)
    {
        return new BuildingRuntimePublishCompositionSystemHelper.Context(
            source.BuildingEntityManagerAccessSystem.TryGetEntityManager,
            source.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries,
            source.BuildingRuntimeProcessingCompositionSystemHelper,
            source.BuildingDefinitionPrefabSystemHelper,
            source.BuildingRuntimeSpawnCompositionSystemHelper,
            source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateSpawnContext(createBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock)),
            source.BuildingProductionRequestSystemHelper,
            source.BuildingProductionContextCompositionSystemHelper.CreateProductionRequestContext(createProductionRuntimeContextSource(source)),
            source.BuildingRuntimeReadModelCompositionSystemHelper,
            source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
            source.FactionResourceCompositionSystemHelper,
            () => source.BuildingGameplayEcsQueryCompositionSystemHelper.BuildingRuntimeStateQuery,
            source.RuntimeBuildingSystem.Buildings);
    }
}

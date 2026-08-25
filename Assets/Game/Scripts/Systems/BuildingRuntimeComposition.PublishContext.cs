using System;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class BuildingRuntimeCompositionSystemHelper
    {
        public BuildingRuntimePublishCompositionSystemHelper.Context Create(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingRuntimeContextFactoryCompositionSystemHelper.Source> createBuildingRuntimeContextSource,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingProductionContextCompositionSystemHelper.Source> createProductionRuntimeContextSource,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource> createRuntimeContextSource)
        {
            BuildingRuntimeContextFactoryCompositionSystemHelper.Source runtimeSource =
                createBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock);
            source.BuildingRuntimeProcessingCompositionSystemHelper.ConfigureDeleteBuildingById(
                runtimeSource.DeleteBuildingById);
            return new BuildingRuntimePublishCompositionSystemHelper.Context(
                source.BuildingEntityManagerAccessSystem.TryGetEntityManager,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries,
                source.BuildingRuntimeProcessingCompositionSystemHelper,
                source.BuildingDefinitionPrefabSystemHelper,
                source.BuildingRuntimeSpawnCompositionSystemHelper,
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateSpawnContext(runtimeSource),
                source.BuildingProductionRequestSystemHelper,
                source.BuildingProductionContextCompositionSystemHelper.CreateProductionRequestContext(createProductionRuntimeContextSource(source)),
                source.BuildingRuntimeReadModelCompositionSystemHelper,
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
                source.FactionResourceCompositionSystemHelper,
                () => source.BuildingGameplayEcsQueryCompositionSystemHelper.BuildingRuntimeStateQuery,
                source.RuntimeBuildingSystem.Buildings);
        }
    }
}

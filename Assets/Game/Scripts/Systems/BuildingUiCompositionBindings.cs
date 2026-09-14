using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class BuildingUiCompositionSystemHelper
    {
        public BuildingUiContextCompositionSystemHelper.Source CreateSource(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource> createRuntimeContextSource,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementQueryUiSystemHelper.Context> createBuildingPlacementQueryContext,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionRuntimeCompositionSystemHelper.Context> createBuildingSelectionContext)
        {
            return source.BuildingUiContextCompositionSystemHelper.CreateSource(
                source.RuntimeFactionResourceSystemHelper,
                source.BuildingDefinitionPrefabSystemHelper,
                source.RuntimeBuildingSystem,
                source.BuildingProductionQueueCompositionSystemHelper,
                source.BuildingProductionRequestSystemHelper,
                () => source.BuildingProductionContextCompositionSystemHelper.CreateProductionRequestContext(
                    source.BuildingProductionCompositionSystemHelper.CreateRuntimeContextSource(
                        source,
                        createRuntimeContextSource,
                        createPlacementCommandContext,
                        interactionContext,
                        markerPropertyBlock)),
                () => source.RuntimeBuildingSystem.CurrentActiveBuildingId,
                () => UnityEngine.Time.frameCount,
                source.BuildingEntityManagerAccessSystem.TryGetEntityManager,
                () => UnityEngine.Time.time,
                source.RuntimeBuildingSystem.HasSelectedBuilding,
                () => source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue,
                () => source.BuildingPlacementLifecycleCompositionSystemHelper.HasPendingBuildingPlacement,
                () => source.BuildingPlacementLifecycleCompositionSystemHelper.CanConfirmBuildingPlacement,
                () => source.BuildingPlacementQueryUiSystemHelper.GetPlacementStatusText(source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement),
                () => source.BuildingPlacementQueryUiSystemHelper.GetSelectedBuildingLabel(createBuildingPlacementQueryContext(source)),
                () => source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacementCost,
                () => source.BuildingPlacementQueryUiSystemHelper.GetActivePlacementDurationSeconds(source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement),
                () => source.BuildingPlacementQueryUiSystemHelper.GetSelectedBuildingDisplayName(createBuildingPlacementQueryContext(source)),
                () => source.BuildingPlacementQueryUiSystemHelper.GetSelectedBuildingDescription(createBuildingPlacementQueryContext(source)),
                (out int current, out int max) => source.BuildingPlacementQueryUiSystemHelper.TryGetSelectedBuildingHealth(
                    createBuildingPlacementQueryContext(source),
                    out current,
                    out max),
                (out GameObject prefab) => source.BuildingPlacementQueryUiSystemHelper.TryGetSelectedBuildingPreviewPrefab(
                    createBuildingPlacementQueryContext(source),
                    out prefab),
                buildingId => source.BuildingRuntimeReadModelCompositionSystemHelper.IsRuntimeBuildingWall(
                    source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
                    buildingId),
                buildingId => source.BuildingRuntimeReadModelCompositionSystemHelper.IsRuntimeBuildingCityGenerated(
                    source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
                    buildingId),
                (int buildingId, out byte factionId) => source.BuildingRuntimeReadModelCompositionSystemHelper.TryGetRuntimeBuildingOwnerFaction(
                    source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
                    buildingId,
                    out factionId),
                camera => source.BuildingSelectionRuntimeCompositionSystemHelper.HasVisibleSelectableBuilding(
                    createBuildingSelectionContext(source),
                    camera != null ? camera : source.BuildingPlacementStartupSystemHelper.WorldCamera,
                    Screen.width,
                    Screen.height),
                (Entity unitEntity, out GameObject prefab) =>
                {
                    RuntimeUnitPrefabSystem.Context runtimeUnitPrefabContext =
                        BuildingRuntimeResourcePrefabContextCompositionSystemHelper.CreateRuntimeUnitPrefabContext(
                            source.BuildingRuntimeResourcePrefabContextCompositionSystemHelper,
                            BuildingRuntimeResourcePrefabCompositionSystemHelper.Create(
                                source.BuildingRuntimeResourcePrefabCompositionHelper,
                                source));
                    return TryResolveLiveUnitPreviewPrefab(source, runtimeUnitPrefabContext, unitEntity, out prefab);
                },
                () => EnqueueAndProcessConfirmBuildingPlacement(
                    source,
                    createPlacementCommandContext(source, interactionContext, markerPropertyBlock)),
                () => EnqueueAndProcessCancelBuildingPlacement(
                    source,
                    createPlacementCommandContext(source, interactionContext, markerPropertyBlock)),
                () => EnqueueAndProcessRotateBuildingPlacement(
                    source,
                    createPlacementCommandContext(source, interactionContext, markerPropertyBlock)),
                () => Mathf.Max(
                    0,
                    source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement?.Definition?.CreditsCost ?? 0),
                () =>
                {
                    var placement = source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement;
                    return placement == null ? Vector2Int.zero : source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint(
                        placement.Definition, placement.AutoRotateVertical);
                });
        }
    }
}

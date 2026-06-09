using System;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingUiCompositionSystem
{
    public BuildingUiContextSystem.Source CreateSource(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementQuerySystem.Context> createBuildingPlacementQueryContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeEntitySystem.Context> createBuildingRuntimeEntityContext)
    {
        return source.BuildingUiContextSystem.CreateSource(
            source.RuntimeResourceSystem,
            source.BuildingDefinitionSystem,
            source.RuntimeBuildingSystem,
            source.BuildingProductionSystem,
            source.BuildingProductionRequestSystem,
            () => source.BuildingProductionContextSystem.CreateProductionRequestContext(
                source.BuildingProductionCompositionSystem.CreateRuntimeContextSource(
                    source,
                    createRuntimeContextSource,
                    createPlacementCommandContext,
                    interactionContext,
                    markerPropertyBlock)),
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId,
            () => Time.frameCount,
            TryGetEntityManager,
            () => Time.time,
            source.RuntimeBuildingSystem.HasSelectedBuilding,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue,
            () => source.BuildingPlacementLifecycleSystem.HasPendingBuildingPlacement,
            () => source.BuildingPlacementLifecycleSystem.CanConfirmBuildingPlacement,
            () => source.BuildingPlacementQuerySystem.GetPlacementStatusText(source.BuildingPlacementLifecycleSystem.ActivePlacement),
            () => source.BuildingPlacementQuerySystem.GetSelectedBuildingLabel(createBuildingPlacementQueryContext(source)),
            () => source.BuildingPlacementLifecycleSystem.ActivePlacementCost,
            () => source.BuildingPlacementLifecycleSystem.ActivePlacement?.Definition != null
                ? source.BuildingPlacementLifecycleSystem.ActivePlacement.Definition.ProductionDurationSeconds
                : 0f,
            () => source.BuildingPlacementQuerySystem.GetSelectedBuildingDisplayName(createBuildingPlacementQueryContext(source)),
            () => source.BuildingPlacementQuerySystem.GetSelectedBuildingDescription(createBuildingPlacementQueryContext(source)),
            (out int current, out int max) => source.BuildingPlacementQuerySystem.TryGetSelectedBuildingHealth(
                createBuildingPlacementQueryContext(source),
                out current,
                out max),
            (out GameObject prefab) => source.BuildingPlacementQuerySystem.TryGetSelectedBuildingPreviewPrefab(
                createBuildingPlacementQueryContext(source),
                out prefab),
            buildingId => source.BuildingRuntimeQuerySystem.IsRuntimeBuildingWall(
                source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
                buildingId),
            buildingId => source.BuildingRuntimeQuerySystem.IsRuntimeBuildingCityGenerated(
                source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
                buildingId),
            (int buildingId, out byte factionId) => source.BuildingRuntimeQuerySystem.TryGetRuntimeBuildingOwnerFaction(
                source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
                buildingId,
                out factionId),
            camera => source.BuildingSelectionSystem.HasVisibleSelectableBuilding(
                createBuildingSelectionContext(source),
                camera != null ? camera : source.BuildingPlacementStartupSystem.WorldCamera,
                Screen.width,
                Screen.height),
            (Entity unitEntity, out GameObject prefab) => source.RuntimeUnitPrefabSystem.TryResolveLiveUnitPreviewPrefab(
                source.BuildingRuntimeResourcePrefabContextSystem.CreateRuntimeUnitPrefabContext(source.BuildingRuntimeResourcePrefabCompositionSystem.Create(source)),
                unitEntity,
                out prefab),
            () => source.BuildingSelectionSystem.DeleteSelectedBuilding(
                createBuildingSelectionContext(source),
                buildingId => source.BuildingRuntimeEntitySystem.DeleteBuildingById(createBuildingRuntimeEntityContext(source), buildingId)),
            () => source.BuildingPlacementCommandSystem.ConfirmBuildingPlacement(createPlacementCommandContext(source, interactionContext, markerPropertyBlock)),
            () => source.BuildingPlacementCommandSystem.CancelBuildingPlacement(createPlacementCommandContext(source, interactionContext, markerPropertyBlock)),
            _ => source.BuildingSelectionSystem.ClearSelectedBuilding(createBuildingSelectionContext(source)),
            () => source.BuildingPlacementCommandSystem.ExitBuildMode(createPlacementCommandContext(source, interactionContext, markerPropertyBlock)));
    }

    public BuildingUiCommandSystem.Context CreateCommandContext(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementQuerySystem.Context> createBuildingPlacementQueryContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeEntitySystem.Context> createBuildingRuntimeEntityContext)
    {
        return source.BuildingUiContextSystem.CreateCommandContext(
            CreateSource(
                source,
                interactionContext,
                markerPropertyBlock,
                createRuntimeContextSource,
                createPlacementCommandContext,
                createBuildingPlacementQueryContext,
                createBuildingSelectionContext,
                createBuildingRuntimeEntityContext));
    }

    public BuildingUiQuerySystem.Context CreateQueryContext(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementQuerySystem.Context> createBuildingPlacementQueryContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeEntitySystem.Context> createBuildingRuntimeEntityContext)
    {
        return source.BuildingUiContextSystem.CreateQueryContext(
            CreateSource(
                source,
                interactionContext,
                markerPropertyBlock,
                createRuntimeContextSource,
                createPlacementCommandContext,
                createBuildingPlacementQueryContext,
                createBuildingSelectionContext,
                createBuildingRuntimeEntityContext));
    }

    private static bool TryGetEntityManager(out EntityManager entityManager)
    {
        entityManager = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        return true;
    }
}

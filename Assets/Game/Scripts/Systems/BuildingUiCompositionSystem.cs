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
            source.BuildingEntityManagerAccessSystem.TryGetEntityManager,
            () => Time.time,
            source.RuntimeBuildingSystem.HasSelectedBuilding,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue,
            () => source.BuildingPlacementLifecycleSystem.HasPendingBuildingPlacement,
            () => source.BuildingPlacementLifecycleSystem.CanConfirmBuildingPlacement,
            () => source.BuildingPlacementQuerySystem.GetPlacementStatusText(source.BuildingPlacementLifecycleSystem.ActivePlacement),
            () => source.BuildingPlacementQuerySystem.GetSelectedBuildingLabel(createBuildingPlacementQueryContext(source)),
            () => source.BuildingPlacementLifecycleSystem.ActivePlacementCost,
            () => source.BuildingPlacementQuerySystem.GetActivePlacementDurationSeconds(source.BuildingPlacementLifecycleSystem.ActivePlacement),
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
            () => EnqueueAndProcessDeleteSelectedBuilding(
                source,
                createBuildingSelectionContext(source),
                buildingId => source.BuildingRuntimeEntitySystem.DeleteBuildingById(createBuildingRuntimeEntityContext(source), buildingId)),
            () => EnqueueAndProcessConfirmBuildingPlacement(
                source,
                createPlacementCommandContext(source, interactionContext, markerPropertyBlock)),
            () => EnqueueAndProcessCancelBuildingPlacement(
                source,
                createPlacementCommandContext(source, interactionContext, markerPropertyBlock)),
            _ => EnqueueAndProcessClearSelectedBuilding(
                source,
                createBuildingSelectionContext(source)),
            () => EnqueueAndProcessExitBuildMode(
                source,
                createPlacementCommandContext(source, interactionContext, markerPropertyBlock)),
            () => EnqueueAndProcessRotateBuildingPlacement(
                source,
                createPlacementCommandContext(source, interactionContext, markerPropertyBlock)));
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

    private static bool EnqueueAndProcessConfirmBuildingPlacement(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementCommandSystem.Context context)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingPlacementCommandSystem.EnqueueAndProcessConfirmBuildingPlacement(entityManager, context)
            : ConfirmBuildingPlacementWithoutEntityManager(context);
    }

    private static void EnqueueAndProcessCancelBuildingPlacement(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementCommandSystem.Context context)
    {
        if (source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager))
            source.BuildingPlacementCommandSystem.EnqueueAndProcessCancelBuildingPlacement(entityManager, context);
        else
            CancelBuildingPlacementWithoutEntityManager(context);
    }

    private static void EnqueueAndProcessExitBuildMode(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementCommandSystem.Context context)
    {
        if (source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager))
            source.BuildingPlacementCommandSystem.EnqueueAndProcessExitBuildMode(entityManager, context);
        else
            ExitBuildModeWithoutEntityManager(context);
    }

    private static bool EnqueueAndProcessRotateBuildingPlacement(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementCommandSystem.Context context)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingPlacementCommandSystem.EnqueueAndProcessRotateBuildingPlacement(entityManager, context)
            : RotateBuildingPlacementWithoutEntityManager(context);
    }

    private static bool ConfirmBuildingPlacementWithoutEntityManager(BuildingPlacementCommandSystem.Context context)
    {
        return context.SessionSystem != null &&
               context.SessionSystem.ConfirmBuildingPlacement(context.SessionContext);
    }

    private static void CancelBuildingPlacementWithoutEntityManager(BuildingPlacementCommandSystem.Context context)
    {
        context.SessionSystem?.CancelBuildingPlacement(context.SessionContext);
    }

    private static void ExitBuildModeWithoutEntityManager(BuildingPlacementCommandSystem.Context context)
    {
        context.SessionSystem?.ExitBuildMode(context.SessionContext);
    }

    private static bool RotateBuildingPlacementWithoutEntityManager(BuildingPlacementCommandSystem.Context context)
    {
        return context.SessionSystem != null &&
               context.SessionSystem.RotateBuildingPlacement(context.SessionContext);
    }

    private static bool EnqueueAndProcessDeleteSelectedBuilding(
        BuildingGameplayCompositionSourceSystem source,
        BuildingSelectionSystem.Context context,
        BuildingSelectionSystem.BuildingIdAction deleteBuildingById)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingSelectionSystem.EnqueueAndProcessDeleteSelectedBuilding(entityManager, context, deleteBuildingById)
            : DeleteSelectedBuilding(source, context, deleteBuildingById);
    }

    private static bool EnqueueAndProcessClearSelectedBuilding(
        BuildingGameplayCompositionSourceSystem source,
        BuildingSelectionSystem.Context context)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingSelectionSystem.EnqueueAndProcessClearSelectedBuilding(entityManager, context)
            : ClearSelectedBuilding(source, context);
    }

    private static bool DeleteSelectedBuilding(
        BuildingGameplayCompositionSourceSystem source,
        BuildingSelectionSystem.Context context,
        BuildingSelectionSystem.BuildingIdAction deleteBuildingById)
    {
        int? buildingId = source.RuntimeBuildingSystem.CurrentActiveBuildingId;
        source.BuildingSelectionSystem.DeleteSelectedBuilding(context, deleteBuildingById);
        return buildingId.HasValue && !source.RuntimeBuildingSystem.ContainsBuilding(buildingId.Value);
    }

    private static bool ClearSelectedBuilding(
        BuildingGameplayCompositionSourceSystem source,
        BuildingSelectionSystem.Context context)
    {
        source.BuildingSelectionSystem.ClearSelectedBuilding(context);
        return !source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue;
    }
}

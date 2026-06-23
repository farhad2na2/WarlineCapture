using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingPlacementInteractionCompositionSystemHelper
{
    internal delegate bool TryGetGridForPlacementInputDelegate(
        BuildingGameplayCompositionSourceSystem source,
        out GridConfig grid);

    internal delegate bool TryGetGridCellDelegate(
        BuildingGameplayCompositionSourceSystem source,
        Vector2 screenPosition,
        GridConfig grid,
        out Vector2Int cell);

    internal delegate void UpdatePlacementDelegate(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Vector2 screenPosition);

    public BuildingPlacementInputSystem.ActivePlacementPointerContext CreateActivePlacementPointerContext(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        TryGetGridForPlacementInputDelegate tryGetGridForPlacementInput,
        TryGetGridCellDelegate tryGetGridCell,
        UpdatePlacementDelegate updatePlacement)
    {
        return new BuildingPlacementInputSystem.ActivePlacementPointerContext(
            (out GridConfig grid) => tryGetGridForPlacementInput(source, out grid),
            (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => tryGetGridCell(source, screenPosition, grid, out cell),
            BuildingPlacementGridSystem.CenterCellToOrigin,
            BuildingPlacementCommitSystem.GetWallSegmentFootprint,
            source.BuildingGameplayDependencySystem.IsPointerOverPlacementUi,
            BuildingBarrierSystem.IsLinearWallDefinition,
            screenPosition => updatePlacement(source, interactionContext, markerPropertyBlock, screenPosition));
    }

    public BuildingPlacementInteractionSystem.Context CreateBuildingPlacementInteractionContext(
        BuildingGameplayCompositionSourceSystem source,
        Func<BuildingPlacementInteractionSystem.Context> getInteractionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingUiQuerySystem.Context> createBuildingUiQueryContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingProductionRequestBoundary.Context> createProductionRequestContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeEntitySystem.Context> createBuildingRuntimeEntityContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource)
    {
        return source.BuildingPlacementInteractionContextSystem.CreateContext(
            source.BuildingPlacementInteractionContextSystem.CreateSource(
                () => source.BuildingPlacementLifecycleSystem.HasPendingBuildingPlacement,
                () => source.BuildingPlacementLifecycleSystem.CanConfirmBuildingPlacement,
                () => source.RuntimeBuildingSystem.HasSelectedBuilding(),
                () => source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue,
                () => source.BuildingPlacementLifecycleSystem.HasPendingBuildingPlacement &&
                      source.BuildingPlacementInputSystem.IsDraggingPlacement,
                () => source.BuildingUiQuerySystem.PlacementStatusText(
                    createBuildingUiQueryContext(source, getInteractionContext(), markerPropertyBlock)),
                () => source.BuildingUiQuerySystem.SelectedBuildingLabel(
                    createBuildingUiQueryContext(source, getInteractionContext(), markerPropertyBlock)),
                () => EnqueueAndProcessBeginSoldierBasePlacement(
                    source,
                    createPlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                () => EnqueueAndProcessConfirmBuildingPlacement(
                    source,
                    createPlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                () => EnqueueAndProcessCancelBuildingPlacement(
                    source,
                    createPlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                () => EnqueueAndProcessCreateUnitFromSelectedBuilding(
                    source,
                    createProductionRequestContext(source, getInteractionContext(), markerPropertyBlock)),
                () => EnqueueAndProcessDeleteSelectedBuilding(
                    source,
                    createBuildingSelectionContext(source),
                    buildingId => source.BuildingRuntimeEntitySystem.DeleteBuildingById(createBuildingRuntimeEntityContext(source), buildingId)),
                _ => EnqueueAndProcessClearSelectedBuilding(
                    source,
                    createBuildingSelectionContext(source)),
                () => EnqueueAndProcessExitBuildMode(
                    source,
                    createPlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                (buildingId, blockerEntity, buildingObject) => source.BuildingRuntimeEntitySystem.HandleRuntimeBuildingEntityDestroyed(
                    createBuildingRuntimeEntityContext(source),
                    buildingId,
                    blockerEntity,
                    buildingObject),
                (
                    byte attackerFactionId,
                    Entity finalTarget,
                    int2 finalTargetCell,
                    int2 attackerCell,
                    out Entity breachTarget,
                    out int2 breachCell,
                    out float3 breachPosition,
                    out string reason) => source.BuildingRuntimeQuerySystem.TryResolveBaseBreachTarget(
                    source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
                    attackerFactionId,
                    finalTarget,
                    finalTargetCell,
                    attackerCell,
                    out breachTarget,
                    out breachCell,
                    out breachPosition,
                    out reason)));
    }

    private static void EnqueueAndProcessCreateUnitFromSelectedBuilding(
        BuildingGameplayCompositionSourceSystem source,
        BuildingProductionRequestBoundary.Context context)
    {
        if (source.BuildingProductionRequestBoundary == null ||
            !source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager))
        {
            return;
        }

        source.BuildingProductionRequestBoundary.EnqueueAndProcessCreateUnitFromSelectedBuilding(
            entityManager,
            context,
            source.RuntimeBuildingSystem.CurrentActiveBuildingId,
            productionIndex: 0,
            UnityEngine.Time.frameCount);
    }

    private static bool EnqueueAndProcessConfirmBuildingPlacement(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementCommandSystem.Context context)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingPlacementCommandSystem.EnqueueAndProcessConfirmBuildingPlacement(entityManager, context)
            : ConfirmBuildingPlacementWithoutEntityManager(context);
    }

    private static void EnqueueAndProcessBeginSoldierBasePlacement(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementCommandSystem.Context context)
    {
        if (source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager))
            source.BuildingPlacementCommandSystem.EnqueueAndProcessBeginSoldierBasePlacement(entityManager, context);
        else
            BeginSoldierBasePlacementWithoutEntityManager(context);
    }

    private static void BeginSoldierBasePlacementWithoutEntityManager(BuildingPlacementCommandSystem.Context context)
    {
        BeginConfiguredPlacementWithoutEntityManager(
            context,
            context.StartupSystem?.SoldierBaseDefinition,
            "BuildingPlacementCommandSystem is missing the Soldier Base spawnable prefab reference.");
    }

    private static void BeginConfiguredPlacementWithoutEntityManager(
        BuildingPlacementCommandSystem.Context context,
        BuildingDefinition definition,
        string missingPrefabWarning)
    {
        if (definition == null || definition.Prefab == null)
        {
            context.LogWarning?.Invoke(missingPrefabWarning);
            return;
        }

        context.SessionSystem?.BeginPlacement(context.SessionContext, definition);
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

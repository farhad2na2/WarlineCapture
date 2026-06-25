using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingPlacementInteractionCompositionSystemHelper
{
    internal delegate bool TryGetGridForPlacementInputDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        out GridConfig grid);

    internal delegate bool TryGetGridCellDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        Vector2 screenPosition,
        GridConfig grid,
        out Vector2Int cell);

    internal delegate void UpdatePlacementDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Vector2 screenPosition);

    public BuildingPlacementInputUiSystemHelper.ActivePlacementPointerContext CreateActivePlacementPointerContext(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        TryGetGridForPlacementInputDelegate tryGetGridForPlacementInput,
        TryGetGridCellDelegate tryGetGridCell,
        UpdatePlacementDelegate updatePlacement)
    {
        return new BuildingPlacementInputUiSystemHelper.ActivePlacementPointerContext(
            (out GridConfig grid) => tryGetGridForPlacementInput(source, out grid),
            (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => tryGetGridCell(source, screenPosition, grid, out cell),
            BuildingPlacementGridCameraSystemHelper.CenterCellToOrigin,
            BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint,
            source.BuildingGameplayDependencyCompositionSystemHelper.IsPointerOverPlacementUi,
            BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition,
            screenPosition => updatePlacement(source, interactionContext, markerPropertyBlock, screenPosition));
    }

    public BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context CreateBuildingPlacementInteractionContext(
        BuildingGameplaySourceCompositionSystemHelper source,
        Func<BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context> getInteractionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingUiQuerySystem.Context> createBuildingUiQueryContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingProductionRequestBoundary.Context> createProductionRequestContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeEntityCompositionSystemHelper.Context> createBuildingRuntimeEntityContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource> createRuntimeContextSource)
    {
        return source.BuildingPlacementInteractionContextCompositionSystemHelper.CreateContext(
            source.BuildingPlacementInteractionContextCompositionSystemHelper.CreateSource(
                () => source.BuildingPlacementLifecycleCompositionSystemHelper.HasPendingBuildingPlacement,
                () => source.BuildingPlacementLifecycleCompositionSystemHelper.CanConfirmBuildingPlacement,
                () => source.RuntimeBuildingSystem.HasSelectedBuilding(),
                () => source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue,
                () => source.BuildingPlacementLifecycleCompositionSystemHelper.HasPendingBuildingPlacement &&
                      source.BuildingPlacementInputUiSystemHelper.IsDraggingPlacement,
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
                    buildingId => source.BuildingRuntimeEntityCompositionSystemHelper.DeleteBuildingById(createBuildingRuntimeEntityContext(source), buildingId)),
                _ => EnqueueAndProcessClearSelectedBuilding(
                    source,
                    createBuildingSelectionContext(source)),
                () => EnqueueAndProcessExitBuildMode(
                    source,
                    createPlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                (buildingId, blockerEntity, buildingObject) => source.BuildingRuntimeEntityCompositionSystemHelper.HandleRuntimeBuildingEntityDestroyed(
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
                    out string reason) => source.BuildingRuntimeReadModelCompositionSystemHelper.TryResolveBaseBreachTarget(
                    source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
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
        BuildingGameplaySourceCompositionSystemHelper source,
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
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessConfirmBuildingPlacement(entityManager, context)
            : ConfirmBuildingPlacementWithoutEntityManager(context);
    }

    private static void EnqueueAndProcessBeginSoldierBasePlacement(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        if (source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager))
            source.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessBeginSoldierBasePlacement(entityManager, context);
        else
            BeginSoldierBasePlacementWithoutEntityManager(context);
    }

    private static void BeginSoldierBasePlacementWithoutEntityManager(BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        BeginConfiguredPlacementWithoutEntityManager(
            context,
            context.StartupSystem?.SoldierBaseDefinition,
            "BuildingPlacementCommandRequestCompositionSystemHelper is missing the Soldier Base spawnable prefab reference.");
    }

    private static void BeginConfiguredPlacementWithoutEntityManager(
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context,
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
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        if (source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager))
            source.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessCancelBuildingPlacement(entityManager, context);
        else
            CancelBuildingPlacementWithoutEntityManager(context);
    }

    private static void EnqueueAndProcessExitBuildMode(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        if (source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager))
            source.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessExitBuildMode(entityManager, context);
        else
            ExitBuildModeWithoutEntityManager(context);
    }

    private static bool ConfirmBuildingPlacementWithoutEntityManager(BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        return context.SessionSystem != null &&
               context.SessionSystem.ConfirmBuildingPlacement(context.SessionContext);
    }

    private static void CancelBuildingPlacementWithoutEntityManager(BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        context.SessionSystem?.CancelBuildingPlacement(context.SessionContext);
    }

    private static void ExitBuildModeWithoutEntityManager(BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        context.SessionSystem?.ExitBuildMode(context.SessionContext);
    }

    private static bool EnqueueAndProcessDeleteSelectedBuilding(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingSelectionSystem.Context context,
        BuildingSelectionSystem.BuildingIdAction deleteBuildingById)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingSelectionSystem.EnqueueAndProcessDeleteSelectedBuilding(entityManager, context, deleteBuildingById)
            : DeleteSelectedBuilding(source, context, deleteBuildingById);
    }

    private static bool EnqueueAndProcessClearSelectedBuilding(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingSelectionSystem.Context context)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingSelectionSystem.EnqueueAndProcessClearSelectedBuilding(entityManager, context)
            : ClearSelectedBuilding(source, context);
    }

    private static bool DeleteSelectedBuilding(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingSelectionSystem.Context context,
        BuildingSelectionSystem.BuildingIdAction deleteBuildingById)
    {
        int? buildingId = source.RuntimeBuildingSystem.CurrentActiveBuildingId;
        source.BuildingSelectionSystem.DeleteSelectedBuilding(context, deleteBuildingById);
        return buildingId.HasValue && !source.RuntimeBuildingSystem.ContainsBuilding(buildingId.Value);
    }

    private static bool ClearSelectedBuilding(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingSelectionSystem.Context context)
    {
        source.BuildingSelectionSystem.ClearSelectedBuilding(context);
        return !source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue;
    }
}

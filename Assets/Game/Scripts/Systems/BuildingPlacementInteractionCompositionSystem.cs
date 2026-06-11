using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingPlacementInteractionCompositionSystem
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
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingUiCommandSystem.Context> createBuildingUiCommandContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingUiQuerySystem.Context> createBuildingUiQueryContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext,
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
                () => source.BuildingPlacementCommandSystem.BeginSoldierBasePlacement(createPlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                () => source.BuildingPlacementCommandSystem.ConfirmBuildingPlacement(createPlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                () => source.BuildingPlacementCommandSystem.CancelBuildingPlacement(createPlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                () => source.BuildingUiCommandSystem.CreateUnitFromSelectedBuilding(
                    createBuildingUiCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                () => source.BuildingSelectionSystem.DeleteSelectedBuilding(
                    createBuildingSelectionContext(source),
                    buildingId => source.BuildingRuntimeEntitySystem.DeleteBuildingById(createBuildingRuntimeEntityContext(source), buildingId)),
                _ => source.BuildingSelectionSystem.ClearSelectedBuilding(createBuildingSelectionContext(source)),
                () => source.BuildingPlacementCommandSystem.ExitBuildMode(createPlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
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
}

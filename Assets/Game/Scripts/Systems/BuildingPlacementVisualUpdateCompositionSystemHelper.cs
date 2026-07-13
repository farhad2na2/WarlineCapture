using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    using PlacementState = BuildingPlacementLifecycleCompositionSystemHelper.PlacementState;

    internal sealed class BuildingPlacementVisualUpdateCompositionSystemHelper
    {
        internal delegate bool TryGetGridDataDelegate(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData);
        internal delegate Vector2Int GetPlacementFootprintDelegate(BuildingDefinition definition, bool rotateVertical);
        internal delegate bool IsPlacementValidDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid, DynamicBuffer<GridRoad> roads, DynamicBlockerComponent blockerData);
        internal delegate Vector3 GetFootprintCenterDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid);
        internal delegate BuildingPlacementContextCompositionSystemHelper.Source CreatePlacementContextSourceDelegate();
        internal delegate BuildingBarrierUtilitySystemHelper.Context CreateBuildingBarrierContextDelegate();
        internal delegate void SelectAndFocusBuildingDelegate(RuntimeBuildingEntity building);

        internal readonly struct Context
        {
            public readonly BuildingPlacementInputUiSystemHelper InputSystem;
            public readonly BuildingPlacementPreviewPresentationSystemHelper PreviewSystem;
            public readonly BuildingPlacementValidationUtilitySystemHelper ValidationSystem;
            public readonly BuildingPlacementGridCameraSystemHelper GridSystem;
            public readonly BuildingPlacementStartupSystemHelper StartupSystem;
            public readonly BuildingGameplayDependencyCompositionSystemHelper DependencySystem;
            public readonly BuildingPlacementContextCompositionSystemHelper ContextSystem;
            public readonly BuildingPlacementCommitCompositionSystemHelper CommitSystem;
            public readonly BuildingPlacementLifecycleCompositionSystemHelper LifecycleSystem;
            public readonly BuildingBarrierUtilitySystemHelper BarrierSystem;
            public readonly BuildingPlacementInputUiSystemHelper.TryGetGridCellDelegate TryGetGridCell;
            public readonly TryGetGridDataDelegate TryGetGridData;
            public readonly GetPlacementFootprintDelegate GetPlacementFootprint;
            public readonly IsPlacementValidDelegate IsPlacementValid;
            public readonly GetFootprintCenterDelegate GetFootprintCenter;
            public readonly BuildingPlacementPreviewPresentationSystemHelper.CreateVisualDelegate CreateBuildingVisualInstance;
            public readonly BuildingPlacementPreviewPresentationSystemHelper.PositionVisualDelegate PositionBuildingObject;
            public readonly BuildingPlacementPreviewPresentationSystemHelper.ReleaseVisualDelegate ReleaseBuildingVisualInstance;
            public readonly CreatePlacementContextSourceDelegate CreatePlacementContextSource;
            public readonly CreateBuildingBarrierContextDelegate CreateBuildingBarrierContext;
            public readonly SelectAndFocusBuildingDelegate SelectAndFocusBuilding;

            public Context(
                BuildingPlacementInputUiSystemHelper inputSystem,
                BuildingPlacementPreviewPresentationSystemHelper previewSystem,
                BuildingPlacementValidationUtilitySystemHelper validationSystem,
                BuildingPlacementGridCameraSystemHelper gridSystem,
                BuildingPlacementStartupSystemHelper startupSystem,
                BuildingGameplayDependencyCompositionSystemHelper dependencySystem,
                BuildingPlacementContextCompositionSystemHelper contextSystem,
                BuildingPlacementCommitCompositionSystemHelper commitSystem,
                BuildingPlacementLifecycleCompositionSystemHelper lifecycleSystem,
                BuildingBarrierUtilitySystemHelper barrierSystem,
                BuildingPlacementInputUiSystemHelper.TryGetGridCellDelegate tryGetGridCell,
                TryGetGridDataDelegate tryGetGridData,
                GetPlacementFootprintDelegate getPlacementFootprint,
                IsPlacementValidDelegate isPlacementValid,
                GetFootprintCenterDelegate getFootprintCenter,
                BuildingPlacementPreviewPresentationSystemHelper.CreateVisualDelegate createBuildingVisualInstance,
                BuildingPlacementPreviewPresentationSystemHelper.PositionVisualDelegate positionBuildingObject,
                BuildingPlacementPreviewPresentationSystemHelper.ReleaseVisualDelegate releaseBuildingVisualInstance,
                CreatePlacementContextSourceDelegate createPlacementContextSource,
                CreateBuildingBarrierContextDelegate createBuildingBarrierContext,
                SelectAndFocusBuildingDelegate selectAndFocusBuilding)
            {
                InputSystem = inputSystem;
                PreviewSystem = previewSystem;
                ValidationSystem = validationSystem;
                GridSystem = gridSystem;
                StartupSystem = startupSystem;
                DependencySystem = dependencySystem;
                ContextSystem = contextSystem;
                CommitSystem = commitSystem;
                LifecycleSystem = lifecycleSystem;
                BarrierSystem = barrierSystem;
                TryGetGridCell = tryGetGridCell;
                TryGetGridData = tryGetGridData;
                GetPlacementFootprint = getPlacementFootprint;
                IsPlacementValid = isPlacementValid;
                GetFootprintCenter = getFootprintCenter;
                CreateBuildingVisualInstance = createBuildingVisualInstance;
                PositionBuildingObject = positionBuildingObject;
                ReleaseBuildingVisualInstance = releaseBuildingVisualInstance;
                CreatePlacementContextSource = createPlacementContextSource;
                CreateBuildingBarrierContext = createBuildingBarrierContext;
                SelectAndFocusBuilding = selectAndFocusBuilding;
            }
        }

        internal void FocusActivePlacement(Context context, PlacementState placement)
        {
            if (placement != null &&
                context.TryGetGridData(out _, out GridConfig grid, out _, out _))
            {
                context.DependencySystem.SmoothMoveCameraGroundCenterTo(
                    ResolveCurrentPlacementFocusWorldPosition(context, placement, grid));
            }
        }

        internal bool ValidateActivePlacementForConfirm(Context context, PlacementState placement)
        {
            if (placement == null)
                return false;

            if (!BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition(placement.Definition))
                return true;

            return context.TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) &&
                   context.ValidationSystem.AreAllPendingWallRunsValid(
                       placement,
                       context.InputSystem,
                       BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint,
                       grid,
                       roads,
                       blockerData,
                       context.ContextSystem.CreateWallValidationContext(context.CreatePlacementContextSource()));
        }

        internal void UpdatePlacement(Context context, PlacementState activePlacement, Vector2 screenPosition)
        {
            if (activePlacement == null)
                return;

            UpdatePlacementVisual(context, activePlacement, context.InputSystem.ShouldUpdateCellFromPointer, screenPosition);
        }

        internal void UpdatePlacementVisual(Context context, PlacementState placement, bool updateCellFromPointer, Vector2 screenPosition)
        {
            if (placement == null || placement.PreviewInstance == null)
                return;

            if (!context.TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData))
            {
                placement.IsValid = false;
                context.PreviewSystem.HideOutline();
                return;
            }

            bool shouldFollowCamera = context.InputSystem.ApplyPointerHover(
                placement,
                updateCellFromPointer,
                screenPosition,
                grid,
                UnityEngine.Time.time,
                context.TryGetGridCell,
                BuildingPlacementGridCameraSystemHelper.CenterCellToOrigin);

            if (BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition(placement.Definition))
            {
                UpdateWallPlacementVisual(context, placement, grid, roads, blockerData, shouldFollowCamera);
                return;
            }

            placement.AutoRotateVertical = context.BarrierSystem.ResolvePlacementRotateVertical(
                context.CreateBuildingBarrierContext(),
                context.InputSystem,
                placement);
            Vector2Int placementFootprint = context.GetPlacementFootprint(placement.Definition, placement.AutoRotateVertical);
            placement.IsValid = context.IsPlacementValid(placement.OriginCell, placementFootprint, grid, roads, blockerData);
            context.PositionBuildingObject(placement.PreviewInstance, placement.OriginCell, placement.Definition, grid, placement.AutoRotateVertical);
            context.PreviewSystem.UpdateOutline(
                placement.OriginCell,
                placementFootprint,
                grid,
                placement.Definition,
                placement.IsValid,
                (origin, footprint, gridData) => context.GetFootprintCenter(origin, footprint, gridData));
            if (shouldFollowCamera)
                context.DependencySystem.FollowCameraGroundCenterTo(context.GetFootprintCenter(placement.OriginCell, placementFootprint, grid));
        }

        internal Vector3 ResolveCurrentPlacementFocusWorldPosition(Context context, PlacementState placement, GridConfig grid)
        {
            if (placement == null)
                return Vector3.zero;

            if (BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition(placement.Definition))
            {
                bool vertical = context.InputSystem.IsWallPlacementVertical(placement);
                Vector2Int wallFootprint = BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint(placement.Definition, vertical);
                IReadOnlyList<Vector2Int> currentOrigins = context.InputSystem.BuildWallPlacementOriginsScratch(placement, BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint);
                IReadOnlyList<Vector2Int> allOrigins = context.InputSystem.GetAllWallPlacementOriginsScratch(placement, currentOrigins);
                return context.GridSystem.ResolvePlacementFocusWorldPosition(
                    placement,
                    allOrigins,
                    grid,
                    wallFootprint,
                    context.StartupSystem.BuildPlaneY);
            }

            bool rotateVertical = context.BarrierSystem.ResolvePlacementRotateVertical(
                context.CreateBuildingBarrierContext(),
                context.InputSystem,
                placement);
            Vector2Int footprint = context.GetPlacementFootprint(placement.Definition, rotateVertical);
            return context.GetFootprintCenter(placement.OriginCell, footprint, grid);
        }

        internal BuildingPlacementCommitCompositionSystemHelper.CommitOutcome PlaceBuilding(
            Context context,
            PlacementState placement)
        {
            if (placement == null)
                return default;

            bool hasGrid = context.TryGetGridData(out _, out GridConfig placementGrid, out _, out _);
            BuildingPlacementContextCompositionSystemHelper.Source placementContextSource = context.CreatePlacementContextSource();
            BuildingPlacementCommitCompositionSystemHelper.CommitRequest request = context.ContextSystem.CreateCommitRequest(placementContextSource, placement);
            BuildingPlacementCommitCompositionSystemHelper.CommitContext commitContext = context.ContextSystem.CreateCommitContext(placementContextSource, hasGrid, placementGrid);

            BuildingPlacementCommitCompositionSystemHelper.CommitOutcome outcome =
                context.CommitSystem.CommitPlacement(request, commitContext);
            if (outcome.PlacementCommitted)
                context.LifecycleSystem.ReleasePreviewOwnership(placement);
            if (outcome.AutoSelectBuilding != null)
                context.SelectAndFocusBuilding(outcome.AutoSelectBuilding);
            return outcome;
        }

        private static void UpdateWallPlacementVisual(
            Context context,
            PlacementState placement,
            GridConfig grid,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData,
            bool shouldFollowCamera)
        {
            IReadOnlyList<Vector2Int> wallOrigins = placement.HideCurrentWallPreview
                ? context.InputSystem.ClearWallPlacementOriginsScratch()
                : context.InputSystem.BuildWallPlacementOriginsScratch(placement, BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint);
            bool vertical = context.InputSystem.IsWallPlacementVertical(placement);
            placement.AutoRotateVertical = vertical;
            Vector2Int wallFootprint = BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint(placement.Definition, vertical);
            BuildingPlacementValidationUtilitySystemHelper.WallValidationContext validationContext =
                context.ContextSystem.CreateWallValidationContext(context.CreatePlacementContextSource());
            placement.IsValid = placement.HideCurrentWallPreview
                ? context.ValidationSystem.AreAllPendingWallRunsValid(
                    placement,
                    context.InputSystem,
                    BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint,
                    grid,
                    roads,
                    blockerData,
                    validationContext)
                : context.ValidationSystem.AreWallPlacementOriginsValid(
                    placement,
                    wallOrigins,
                    wallFootprint,
                    vertical,
                    grid,
                    roads,
                    blockerData,
                    validationContext,
                    BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint);
            context.PreviewSystem.RebuildWallPlacementPreview(
                placement,
                wallOrigins,
                vertical,
                grid,
                context.CreateBuildingVisualInstance,
                context.PositionBuildingObject,
                context.ReleaseBuildingVisualInstance);
            context.PreviewSystem.UpdateWallOutline(
                context.InputSystem.GetAllWallPlacementOriginsScratch(placement, wallOrigins),
                wallFootprint,
                grid,
                placement.Definition,
                placement.IsValid,
                (origin, footprint, gridData) => context.GetFootprintCenter(origin, footprint, gridData));
            if (shouldFollowCamera)
            {
                IReadOnlyList<Vector2Int> allOrigins = context.InputSystem.GetAllWallPlacementOriginsScratch(placement, wallOrigins);
                context.DependencySystem.FollowCameraGroundCenterTo(
                    context.GridSystem.ResolvePlacementFocusWorldPosition(
                        placement,
                        allOrigins,
                        grid,
                        wallFootprint,
                        context.StartupSystem.BuildPlaneY));
            }
        }
    }
}

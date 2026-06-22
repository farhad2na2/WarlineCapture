using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using PlacementState = BuildingPlacementLifecycleSystem.PlacementState;

internal sealed class BuildingPlacementVisualUpdateSystem
{
    internal delegate bool TryGetGridDataDelegate(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData);
    internal delegate Vector2Int GetPlacementFootprintDelegate(BuildingDefinition definition, bool rotateVertical);
    internal delegate bool IsPlacementValidDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid, DynamicBuffer<GridRoad> roads, DynamicBlockerComponent blockerData);
    internal delegate Vector3 GetFootprintCenterDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid);
    internal delegate BuildingPlacementContextSystem.Source CreatePlacementContextSourceDelegate();
    internal delegate BuildingBarrierSystem.Context CreateBuildingBarrierContextDelegate();
    internal delegate void SelectAndFocusBuildingDelegate(RuntimeBuildingEntity building);

    internal readonly struct Context
    {
        public readonly BuildingPlacementInputSystem InputSystem;
        public readonly BuildingPlacementPreviewSystem PreviewSystem;
        public readonly BuildingPlacementValidationSystem ValidationSystem;
        public readonly BuildingPlacementGridSystem GridSystem;
        public readonly BuildingPlacementStartupSystem StartupSystem;
        public readonly BuildingGameplayDependencySystem DependencySystem;
        public readonly BuildingPlacementContextSystem ContextSystem;
        public readonly BuildingPlacementCommitSystem CommitSystem;
        public readonly BuildingPlacementLifecycleSystem LifecycleSystem;
        public readonly BuildingBarrierSystem BarrierSystem;
        public readonly BuildingPlacementInputSystem.TryGetGridCellDelegate TryGetGridCell;
        public readonly TryGetGridDataDelegate TryGetGridData;
        public readonly GetPlacementFootprintDelegate GetPlacementFootprint;
        public readonly IsPlacementValidDelegate IsPlacementValid;
        public readonly GetFootprintCenterDelegate GetFootprintCenter;
        public readonly BuildingPlacementPreviewSystem.CreateVisualDelegate CreateBuildingVisualInstance;
        public readonly BuildingPlacementPreviewSystem.PositionVisualDelegate PositionBuildingObject;
        public readonly CreatePlacementContextSourceDelegate CreatePlacementContextSource;
        public readonly CreateBuildingBarrierContextDelegate CreateBuildingBarrierContext;
        public readonly SelectAndFocusBuildingDelegate SelectAndFocusBuilding;

        public Context(
            BuildingPlacementInputSystem inputSystem,
            BuildingPlacementPreviewSystem previewSystem,
            BuildingPlacementValidationSystem validationSystem,
            BuildingPlacementGridSystem gridSystem,
            BuildingPlacementStartupSystem startupSystem,
            BuildingGameplayDependencySystem dependencySystem,
            BuildingPlacementContextSystem contextSystem,
            BuildingPlacementCommitSystem commitSystem,
            BuildingPlacementLifecycleSystem lifecycleSystem,
            BuildingBarrierSystem barrierSystem,
            BuildingPlacementInputSystem.TryGetGridCellDelegate tryGetGridCell,
            TryGetGridDataDelegate tryGetGridData,
            GetPlacementFootprintDelegate getPlacementFootprint,
            IsPlacementValidDelegate isPlacementValid,
            GetFootprintCenterDelegate getFootprintCenter,
            BuildingPlacementPreviewSystem.CreateVisualDelegate createBuildingVisualInstance,
            BuildingPlacementPreviewSystem.PositionVisualDelegate positionBuildingObject,
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

        if (!BuildingBarrierSystem.IsLinearWallDefinition(placement.Definition))
            return true;

        return context.TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) &&
               context.ValidationSystem.AreAllPendingWallRunsValid(
                   placement,
                   context.InputSystem,
                   BuildingPlacementCommitSystem.GetWallSegmentFootprint,
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
            BuildingPlacementGridSystem.CenterCellToOrigin);

        if (BuildingBarrierSystem.IsLinearWallDefinition(placement.Definition))
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

        if (BuildingBarrierSystem.IsLinearWallDefinition(placement.Definition))
        {
            bool vertical = context.InputSystem.IsWallPlacementVertical(placement);
            Vector2Int wallFootprint = BuildingPlacementCommitSystem.GetWallSegmentFootprint(placement.Definition, vertical);
            IReadOnlyList<Vector2Int> currentOrigins = context.InputSystem.BuildWallPlacementOriginsScratch(placement, BuildingPlacementCommitSystem.GetWallSegmentFootprint);
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

    internal void PlaceBuilding(Context context, PlacementState placement)
    {
        if (placement == null)
            return;

        bool hasGrid = context.TryGetGridData(out _, out GridConfig placementGrid, out _, out _);
        BuildingPlacementContextSystem.Source placementContextSource = context.CreatePlacementContextSource();
        BuildingPlacementCommitSystem.CommitRequest request = context.ContextSystem.CreateCommitRequest(placementContextSource, placement);
        BuildingPlacementCommitSystem.CommitContext commitContext = context.ContextSystem.CreateCommitContext(placementContextSource, hasGrid, placementGrid);

        RuntimeBuildingEntity building = context.CommitSystem.CommitPlacement(request, commitContext);
        context.LifecycleSystem.ReleasePreviewOwnership(placement);
        if (building != null)
            context.SelectAndFocusBuilding(building);
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
            : context.InputSystem.BuildWallPlacementOriginsScratch(placement, BuildingPlacementCommitSystem.GetWallSegmentFootprint);
        bool vertical = context.InputSystem.IsWallPlacementVertical(placement);
        placement.AutoRotateVertical = vertical;
        Vector2Int wallFootprint = BuildingPlacementCommitSystem.GetWallSegmentFootprint(placement.Definition, vertical);
        BuildingPlacementValidationSystem.WallValidationContext validationContext =
            context.ContextSystem.CreateWallValidationContext(context.CreatePlacementContextSource());
        placement.IsValid = placement.HideCurrentWallPreview
            ? context.ValidationSystem.AreAllPendingWallRunsValid(
                placement,
                context.InputSystem,
                BuildingPlacementCommitSystem.GetWallSegmentFootprint,
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
                BuildingPlacementCommitSystem.GetWallSegmentFootprint);
        context.PreviewSystem.RebuildWallPlacementPreview(
            placement,
            wallOrigins,
            vertical,
            grid,
            context.CreateBuildingVisualInstance,
            context.PositionBuildingObject);
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

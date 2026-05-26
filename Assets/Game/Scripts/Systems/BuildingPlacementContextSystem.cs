using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingPlacementContextSystem
{
    private readonly List<BuildingPlacementCommitSystem.WallRun> _wallCommitRuns = new();

    public readonly struct Source
    {
        public readonly RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly BuildingPlacementLifecycleSystem LifecycleSystem;
        public readonly BuildingPlacementInputSystem InputSystem;
        public readonly BuildingPlacementPreviewSystem PreviewSystem;
        public readonly BuildingPlacementValidationSystem PlacementValidationSystem;
        public readonly RuntimeBuildingSystem<RuntimeBuildingData> RuntimeBuildingSystem;
        public readonly Transform BuildingRoot;
        public readonly BuildingPlacementLifecycleSystem.CreatePreviewDelegate CreatePreview;
        public readonly BuildingPlacementLifecycleSystem.DestroyPreviewDelegate DestroyPreview;
        public readonly BuildingPlacementLifecycleSystem.GetInitialOriginDelegate GetInitialOrigin;
        public readonly BuildingPlacementLifecycleSystem.TryResolveInitialOriginDelegate TryResolveInitialOrigin;
        public readonly BuildingPlacementLifecycleSystem.UpdatePlacementVisualDelegate UpdatePlacementVisual;
        public readonly BuildingPlacementLifecycleSystem.FocusPlacementDelegate FocusPlacement;
        public readonly BuildingPlacementLifecycleSystem.ValidateConfirmDelegate ValidateConfirm;
        public readonly BuildingPlacementLifecycleSystem.TrySpendCostDelegate TrySpendCost;
        public readonly BuildingPlacementLifecycleSystem.CommitPlacementDelegate CommitPlacement;
        public readonly Action ApplyBuildCommandMode;
        public readonly Action ClearSelectedBuildingForBegin;
        public readonly BuildingPlacementInputSystem.TryGetGridForInputDelegate TryGetGridForPlacementInput;
        public readonly BuildingPlacementInputSystem.TryGetGridCellDelegate TryGetGridCell;
        public readonly BuildingPlacementInputSystem.IsPointerOverPlacementUiDelegate IsPointerOverPlacementUi;
        public readonly BuildingPlacementInputSystem.UpdatePlacementFromPointerDelegate UpdatePlacement;
        public readonly Func<int, int, int, int, bool> IsRuntimeBlockerCell;
        public readonly Func<GridConfig, Vector2Int, Vector2Int, bool> HasRoadInFootprint;
        public readonly BuildingPlacementCommitSystem.CreateVisualDelegate CreateBuildingVisualInstance;
        public readonly BuildingPlacementCommitSystem.PositionVisualDelegate PositionBuildingObject;
        public readonly BuildingPlacementCommitSystem.RegisterRuntimeBuildingDelegate RegisterRuntimeBuilding;
        public readonly BuildingPlacementCommitSystem.CloneDefinitionWithFootprintDelegate CloneDefinitionWithFootprint;
        public readonly BuildingPlacementCommitSystem.GetPlacementFootprintDelegate GetPlacementFootprint;
        public readonly BuildingPlacementCommitSystem.DestroyRuntimeObjectDelegate DestroyRuntimeObject;

        public Source(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            BuildingPlacementLifecycleSystem lifecycleSystem,
            BuildingPlacementInputSystem inputSystem,
            BuildingPlacementPreviewSystem previewSystem,
            BuildingPlacementValidationSystem placementValidationSystem,
            RuntimeBuildingSystem<RuntimeBuildingData> runtimeBuildingSystem,
            Transform buildingRoot,
            BuildingPlacementLifecycleSystem.CreatePreviewDelegate createPreview,
            BuildingPlacementLifecycleSystem.DestroyPreviewDelegate destroyPreview,
            BuildingPlacementLifecycleSystem.GetInitialOriginDelegate getInitialOrigin,
            BuildingPlacementLifecycleSystem.TryResolveInitialOriginDelegate tryResolveInitialOrigin,
            BuildingPlacementLifecycleSystem.UpdatePlacementVisualDelegate updatePlacementVisual,
            BuildingPlacementLifecycleSystem.FocusPlacementDelegate focusPlacement,
            BuildingPlacementLifecycleSystem.ValidateConfirmDelegate validateConfirm,
            BuildingPlacementLifecycleSystem.TrySpendCostDelegate trySpendCost,
            BuildingPlacementLifecycleSystem.CommitPlacementDelegate commitPlacement,
            Action applyBuildCommandMode,
            Action clearSelectedBuildingForBegin,
            BuildingPlacementInputSystem.TryGetGridForInputDelegate tryGetGridForPlacementInput,
            BuildingPlacementInputSystem.TryGetGridCellDelegate tryGetGridCell,
            BuildingPlacementInputSystem.IsPointerOverPlacementUiDelegate isPointerOverPlacementUi,
            BuildingPlacementInputSystem.UpdatePlacementFromPointerDelegate updatePlacement,
            Func<int, int, int, int, bool> isRuntimeBlockerCell,
            Func<GridConfig, Vector2Int, Vector2Int, bool> hasRoadInFootprint,
            BuildingPlacementCommitSystem.CreateVisualDelegate createBuildingVisualInstance,
            BuildingPlacementCommitSystem.PositionVisualDelegate positionBuildingObject,
            BuildingPlacementCommitSystem.RegisterRuntimeBuildingDelegate registerRuntimeBuilding,
            BuildingPlacementCommitSystem.CloneDefinitionWithFootprintDelegate cloneDefinitionWithFootprint,
            BuildingPlacementCommitSystem.GetPlacementFootprintDelegate getPlacementFootprint,
            BuildingPlacementCommitSystem.DestroyRuntimeObjectDelegate destroyRuntimeObject)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            LifecycleSystem = lifecycleSystem;
            InputSystem = inputSystem;
            PreviewSystem = previewSystem;
            PlacementValidationSystem = placementValidationSystem;
            RuntimeBuildingSystem = runtimeBuildingSystem;
            BuildingRoot = buildingRoot;
            CreatePreview = createPreview;
            DestroyPreview = destroyPreview;
            GetInitialOrigin = getInitialOrigin;
            TryResolveInitialOrigin = tryResolveInitialOrigin;
            UpdatePlacementVisual = updatePlacementVisual;
            FocusPlacement = focusPlacement;
            ValidateConfirm = validateConfirm;
            TrySpendCost = trySpendCost;
            CommitPlacement = commitPlacement;
            ApplyBuildCommandMode = applyBuildCommandMode;
            ClearSelectedBuildingForBegin = clearSelectedBuildingForBegin;
            TryGetGridForPlacementInput = tryGetGridForPlacementInput;
            TryGetGridCell = tryGetGridCell;
            IsPointerOverPlacementUi = isPointerOverPlacementUi;
            UpdatePlacement = updatePlacement;
            IsRuntimeBlockerCell = isRuntimeBlockerCell;
            HasRoadInFootprint = hasRoadInFootprint;
            CreateBuildingVisualInstance = createBuildingVisualInstance;
            PositionBuildingObject = positionBuildingObject;
            RegisterRuntimeBuilding = registerRuntimeBuilding;
            CloneDefinitionWithFootprint = cloneDefinitionWithFootprint;
            GetPlacementFootprint = getPlacementFootprint;
            DestroyRuntimeObject = destroyRuntimeObject;
        }
    }

    public BuildingPlacementInputSystem.ActivePlacementPointerContext CreateActivePlacementPointerContext(Source source)
    {
        return new BuildingPlacementInputSystem.ActivePlacementPointerContext(
            source.TryGetGridForPlacementInput,
            source.TryGetGridCell,
            BuildingPlacementGridSystem.CenterCellToOrigin,
            BuildingPlacementCommitSystem.GetWallSegmentFootprint,
            source.IsPointerOverPlacementUi,
            BuildingBarrierSystem.IsLinearWallDefinition,
            source.UpdatePlacement);
    }

    public BuildingPlacementLifecycleSystem.CancelContext CreateCancelContext(Source source)
    {
        return new BuildingPlacementLifecycleSystem.CancelContext(
            source.InputSystem,
            source.PreviewSystem,
            source.DestroyPreview);
    }

    public BuildingPlacementLifecycleSystem.BeginContext CreateBeginContext(Source source)
    {
        return new BuildingPlacementLifecycleSystem.BeginContext(
            source.RuntimeGameplayStateSystem,
            source.InputSystem,
            source.PreviewSystem,
            source.BuildingRoot,
            source.CreatePreview,
            source.DestroyPreview,
            source.GetInitialOrigin,
            source.TryResolveInitialOrigin,
            source.UpdatePlacementVisual,
            source.FocusPlacement,
            source.ApplyBuildCommandMode,
            source.ClearSelectedBuildingForBegin);
    }

    public BuildingPlacementLifecycleSystem.ConfirmContext CreateConfirmContext(Source source)
    {
        return new BuildingPlacementLifecycleSystem.ConfirmContext(
            source.ValidateConfirm,
            source.TrySpendCost,
            source.CommitPlacement);
    }

    public BuildingPlacementSessionSystem.Context CreateSessionContext(
        Source source,
        Action recordBuildingBuilt,
        Action notifyStaticMinimapChanged,
        Action<string> clearSelectedBuilding,
        Action clearCommandMode)
    {
        return new BuildingPlacementSessionSystem.Context(
            source.RuntimeGameplayStateSystem,
            source.LifecycleSystem,
            source.InputSystem,
            source.PreviewSystem,
            () => CreateCancelContext(source),
            () => CreateBeginContext(source),
            () => CreateConfirmContext(source),
            recordBuildingBuilt,
            notifyStaticMinimapChanged,
            clearSelectedBuilding,
            clearCommandMode);
    }

    public BuildingPlacementCommandSystem.Context CreateCommandContext(
        Source source,
        BuildingPlacementStartupSystem startupSystem,
        BuildingDefinitionSystem definitionSystem,
        BuildingPlacementSessionSystem sessionSystem,
        Action<string> logWarning,
        Action recordBuildingBuilt,
        Action notifyStaticMinimapChanged,
        Action<string> clearSelectedBuilding,
        Action clearCommandMode)
    {
        return new BuildingPlacementCommandSystem.Context(
            startupSystem,
            definitionSystem,
            sessionSystem,
            CreateSessionContext(
                source,
                recordBuildingBuilt,
                notifyStaticMinimapChanged,
                clearSelectedBuilding,
                clearCommandMode),
            logWarning);
    }

    public BuildingPlacementValidationSystem.WallValidationContext CreateWallValidationContext(Source source)
    {
        return new BuildingPlacementValidationSystem.WallValidationContext(
            source.RuntimeBuildingSystem.Buildings,
            source.IsRuntimeBlockerCell,
            source.HasRoadInFootprint);
    }

    public BuildingPlacementCommitSystem.CommitRequest CreateCommitRequest(
        Source source,
        BuildingPlacementLifecycleSystem.PlacementState placement)
    {
        _wallCommitRuns.Clear();
        if (placement.CommittedWallRuns != null)
        {
            for (int i = 0; i < placement.CommittedWallRuns.Count; i++)
            {
                BuildingPlacementInputSystem.WallRun run = placement.CommittedWallRuns[i];
                if (run?.Origins == null || run.Origins.Count == 0)
                    continue;

                _wallCommitRuns.Add(new BuildingPlacementCommitSystem.WallRun(run.Origins, run.Vertical));
            }
        }

        List<Vector2Int> currentWallOrigins = null;
        bool currentWallVertical = false;
        if (BuildingBarrierSystem.IsLinearWallDefinition(placement.Definition))
        {
            currentWallVertical = source.InputSystem.IsWallPlacementVertical(placement);
            if (!placement.HideCurrentWallPreview)
                currentWallOrigins = source.InputSystem.BuildWallPlacementOrigins(placement, BuildingPlacementCommitSystem.GetWallSegmentFootprint);
        }

        return new BuildingPlacementCommitSystem.CommitRequest(
            placement.Definition,
            placement.PreviewInstance,
            placement.OriginCell,
            placement.AutoRotateVertical,
            BuildingBarrierSystem.IsLinearWallDefinition(placement.Definition),
            placement.HideCurrentWallPreview,
            _wallCommitRuns,
            currentWallOrigins,
            currentWallVertical);
    }

    public BuildingPlacementCommitSystem.CommitContext CreateCommitContext(Source source, bool hasGrid, GridConfig placementGrid)
    {
        return new BuildingPlacementCommitSystem.CommitContext(
            source.BuildingRoot,
            hasGrid,
            placementGrid,
            source.CreateBuildingVisualInstance,
            source.PositionBuildingObject,
            source.RegisterRuntimeBuilding,
            source.CloneDefinitionWithFootprint,
            source.GetPlacementFootprint,
            BuildingPlacementCommitSystem.GetWallSegmentFootprint,
            source.DestroyRuntimeObject);
    }
}

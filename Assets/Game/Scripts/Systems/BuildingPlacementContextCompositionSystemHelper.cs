using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingPlacementContextCompositionSystemHelper
{
    private readonly List<BuildingPlacementCommitCompositionSystemHelper.WallRun> _wallCommitRuns = new();

    public struct Source
    {
        public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly BuildingPlacementLifecycleCompositionSystemHelper LifecycleSystem;
        public readonly BuildingPlacementInputSystem InputSystem;
        public readonly BuildingPlacementPreviewPresentationSystemHelper PreviewSystem;
        public readonly BuildingPlacementValidationSystem PlacementValidationSystem;
        public readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem;
        public readonly Transform BuildingRoot;
        public readonly BuildingPlacementLifecycleCompositionSystemHelper.CreatePreviewDelegate CreatePreview;
        public readonly BuildingPlacementLifecycleCompositionSystemHelper.DestroyPreviewDelegate DestroyPreview;
        public readonly BuildingPlacementLifecycleCompositionSystemHelper.GetInitialOriginDelegate GetInitialOrigin;
        public readonly BuildingPlacementLifecycleCompositionSystemHelper.TryResolveInitialOriginDelegate TryResolveInitialOrigin;
        public readonly BuildingPlacementLifecycleCompositionSystemHelper.UpdatePlacementVisualDelegate UpdatePlacementVisual;
        public readonly BuildingPlacementLifecycleCompositionSystemHelper.FocusPlacementDelegate FocusPlacement;
        public readonly BuildingPlacementLifecycleCompositionSystemHelper.ValidateConfirmDelegate ValidateConfirm;
        public readonly BuildingPlacementLifecycleCompositionSystemHelper.TrySpendCostDelegate TrySpendCost;
        public readonly BuildingPlacementLifecycleCompositionSystemHelper.CommitPlacementDelegate CommitPlacement;
        public readonly Action ApplyBuildCommandMode;
        public readonly Action ClearSelectedBuildingForBegin;
        public readonly BuildingPlacementInputSystem.TryGetGridForInputDelegate TryGetGridForPlacementInput;
        public readonly BuildingPlacementInputSystem.TryGetGridCellDelegate TryGetGridCell;
        public readonly BuildingPlacementInputSystem.IsPointerOverPlacementUiDelegate IsPointerOverPlacementUi;
        public readonly BuildingPlacementInputSystem.UpdatePlacementFromPointerDelegate UpdatePlacement;
        public readonly Func<int, int, int, int, bool> IsRuntimeBlockerCell;
        public readonly Func<GridConfig, Vector2Int, Vector2Int, bool> HasRoadInFootprint;
        public readonly BuildingPlacementCommitCompositionSystemHelper.CreateVisualDelegate CreateBuildingVisualInstance;
        public readonly BuildingPlacementCommitCompositionSystemHelper.PositionVisualDelegate PositionBuildingObject;
        public readonly BuildingPlacementCommitCompositionSystemHelper.RegisterRuntimeBuildingDelegate RegisterRuntimeBuilding;
        public readonly BuildingPlacementCommitCompositionSystemHelper.CloneDefinitionWithFootprintDelegate CloneDefinitionWithFootprint;
        public readonly BuildingPlacementCommitCompositionSystemHelper.GetPlacementFootprintDelegate GetPlacementFootprint;
        public readonly BuildingPlacementCommitCompositionSystemHelper.DestroyRuntimeObjectDelegate DestroyRuntimeObject;

        public Source(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            BuildingPlacementLifecycleCompositionSystemHelper lifecycleSystem,
            BuildingPlacementInputSystem inputSystem,
            BuildingPlacementPreviewPresentationSystemHelper previewSystem,
            BuildingPlacementValidationSystem placementValidationSystem,
            RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
            Transform buildingRoot,
            BuildingPlacementLifecycleCompositionSystemHelper.CreatePreviewDelegate createPreview,
            BuildingPlacementLifecycleCompositionSystemHelper.DestroyPreviewDelegate destroyPreview,
            BuildingPlacementLifecycleCompositionSystemHelper.GetInitialOriginDelegate getInitialOrigin,
            BuildingPlacementLifecycleCompositionSystemHelper.TryResolveInitialOriginDelegate tryResolveInitialOrigin,
            BuildingPlacementLifecycleCompositionSystemHelper.UpdatePlacementVisualDelegate updatePlacementVisual,
            BuildingPlacementLifecycleCompositionSystemHelper.FocusPlacementDelegate focusPlacement,
            BuildingPlacementLifecycleCompositionSystemHelper.ValidateConfirmDelegate validateConfirm,
            BuildingPlacementLifecycleCompositionSystemHelper.TrySpendCostDelegate trySpendCost,
            BuildingPlacementLifecycleCompositionSystemHelper.CommitPlacementDelegate commitPlacement,
            Action applyBuildCommandMode,
            Action clearSelectedBuildingForBegin,
            BuildingPlacementInputSystem.TryGetGridForInputDelegate tryGetGridForPlacementInput,
            BuildingPlacementInputSystem.TryGetGridCellDelegate tryGetGridCell,
            BuildingPlacementInputSystem.IsPointerOverPlacementUiDelegate isPointerOverPlacementUi,
            BuildingPlacementInputSystem.UpdatePlacementFromPointerDelegate updatePlacement,
            Func<int, int, int, int, bool> isRuntimeBlockerCell,
            Func<GridConfig, Vector2Int, Vector2Int, bool> hasRoadInFootprint,
            BuildingPlacementCommitCompositionSystemHelper.CreateVisualDelegate createBuildingVisualInstance,
            BuildingPlacementCommitCompositionSystemHelper.PositionVisualDelegate positionBuildingObject,
            BuildingPlacementCommitCompositionSystemHelper.RegisterRuntimeBuildingDelegate registerRuntimeBuilding,
            BuildingPlacementCommitCompositionSystemHelper.CloneDefinitionWithFootprintDelegate cloneDefinitionWithFootprint,
            BuildingPlacementCommitCompositionSystemHelper.GetPlacementFootprintDelegate getPlacementFootprint,
            BuildingPlacementCommitCompositionSystemHelper.DestroyRuntimeObjectDelegate destroyRuntimeObject)
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
            BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint,
            source.IsPointerOverPlacementUi,
            BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition,
            source.UpdatePlacement);
    }

    public BuildingPlacementLifecycleCompositionSystemHelper.CancelContext CreateCancelContext(Source source)
    {
        return new BuildingPlacementLifecycleCompositionSystemHelper.CancelContext(
            source.InputSystem,
            source.PreviewSystem,
            source.DestroyPreview);
    }

    public BuildingPlacementLifecycleCompositionSystemHelper.BeginContext CreateBeginContext(Source source)
    {
        return new BuildingPlacementLifecycleCompositionSystemHelper.BeginContext(
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

    public BuildingPlacementLifecycleCompositionSystemHelper.ConfirmContext CreateConfirmContext(Source source)
    {
        return new BuildingPlacementLifecycleCompositionSystemHelper.ConfirmContext(
            source.ValidateConfirm,
            source.TrySpendCost,
            source.CommitPlacement);
    }

    public BuildingPlacementLifecycleCompositionSystemHelper.RotateContext CreateRotateContext(Source source)
    {
        return new BuildingPlacementLifecycleCompositionSystemHelper.RotateContext(source.UpdatePlacementVisual);
    }

    public BuildingPlacementSessionCompositionSystemHelper.Context CreateSessionContext(
        Source source,
        Action recordBuildingBuilt,
        Action notifyStaticMinimapChanged,
        Action<string> clearSelectedBuilding,
        Action clearCommandMode)
    {
        return new BuildingPlacementSessionCompositionSystemHelper.Context(
            source.RuntimeGameplayStateSystem,
            source.LifecycleSystem,
            source.InputSystem,
            source.PreviewSystem,
            () => CreateCancelContext(source),
            () => CreateBeginContext(source),
            () => CreateConfirmContext(source),
            () => CreateRotateContext(source),
            recordBuildingBuilt,
            notifyStaticMinimapChanged,
            clearSelectedBuilding,
            clearCommandMode);
    }

    public BuildingPlacementCommandRequestCompositionSystemHelper.Context CreateCommandContext(
        Source source,
        BuildingPlacementStartupSystemHelper startupSystem,
        BuildingDefinitionPrefabSystemHelper definitionSystem,
        BuildingPlacementSessionCompositionSystemHelper sessionSystem,
        Action<string> logWarning,
        Action recordBuildingBuilt,
        Action notifyStaticMinimapChanged,
        Action<string> clearSelectedBuilding,
        Action clearCommandMode)
    {
        return new BuildingPlacementCommandRequestCompositionSystemHelper.Context(
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

    public BuildingPlacementCommitCompositionSystemHelper.CommitRequest CreateCommitRequest(
        Source source,
        BuildingPlacementLifecycleCompositionSystemHelper.PlacementState placement)
    {
        _wallCommitRuns.Clear();
        if (placement.CommittedWallRuns != null)
        {
            for (int i = 0; i < placement.CommittedWallRuns.Count; i++)
            {
                BuildingPlacementInputSystem.WallRun run = placement.CommittedWallRuns[i];
                if (run?.Origins == null || run.Origins.Count == 0)
                    continue;

                _wallCommitRuns.Add(new BuildingPlacementCommitCompositionSystemHelper.WallRun(run.Origins, run.Vertical));
            }
        }

        List<Vector2Int> currentWallOrigins = null;
        bool currentWallVertical = false;
        if (BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition(placement.Definition))
        {
            currentWallVertical = source.InputSystem.IsWallPlacementVertical(placement);
            if (!placement.HideCurrentWallPreview)
                currentWallOrigins = source.InputSystem.BuildWallPlacementOrigins(placement, BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint);
        }

        return new BuildingPlacementCommitCompositionSystemHelper.CommitRequest(
            placement.Definition,
            placement.PreviewInstance,
            placement.OriginCell,
            placement.AutoRotateVertical,
            BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition(placement.Definition),
            placement.HideCurrentWallPreview,
            _wallCommitRuns,
            currentWallOrigins,
            currentWallVertical);
    }

    public BuildingPlacementCommitCompositionSystemHelper.CommitContext CreateCommitContext(Source source, bool hasGrid, GridConfig placementGrid)
    {
        return new BuildingPlacementCommitCompositionSystemHelper.CommitContext(
            source.BuildingRoot,
            hasGrid,
            placementGrid,
            source.CreateBuildingVisualInstance,
            source.PositionBuildingObject,
            source.RegisterRuntimeBuilding,
            source.CloneDefinitionWithFootprint,
            source.GetPlacementFootprint,
            BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint,
            source.DestroyRuntimeObject);
    }
}

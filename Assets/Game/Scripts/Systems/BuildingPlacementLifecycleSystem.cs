using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingPlacementLifecycleSystem
{
    public sealed class PlacementState : BuildingPlacementInputSystem.IPlacementState
    {
        public BuildingDefinition Definition { get; set; }
        public GameObject PreviewInstance { get; set; }
        public Vector2Int OriginCell { get; set; }
        public Vector2Int CommittedOriginCell { get; set; }
        public Vector2Int DragStartOriginCell { get; set; }
        public Vector2Int DragCurrentOriginCell { get; set; }
        public BuildingPlacementInputSystem.DragFirstAxis DragFirstAxis { get; set; }
        public bool AutoRotateVertical { get; set; }
        public List<BuildingPlacementInputSystem.WallRun> CommittedWallRuns { get; set; }
        public bool HideCurrentWallPreview { get; set; }
        public bool IsValid { get; set; }
        public float LastPointerMovedAt { get; set; }
        public Vector2 LastPointerScreenPosition { get; set; }
    }

    public delegate GameObject CreatePreviewDelegate(BuildingDefinition definition, Transform parent);
    public delegate void DestroyPreviewDelegate(GameObject preview);
    public delegate Vector2Int GetInitialOriginDelegate(Vector2Int footprintCells);
    public delegate bool TryResolveInitialOriginDelegate(BuildingDefinition definition, Vector2Int preferredOrigin, out Vector2Int resolvedOrigin);
    public delegate void UpdatePlacementVisualDelegate(PlacementState placement, bool updateCellFromPointer, Vector2 screenPosition);
    public delegate void FocusPlacementDelegate(PlacementState placement);
    public delegate bool ValidateConfirmDelegate(PlacementState placement);
    public delegate bool TrySpendCostDelegate(int cost);
    public delegate void CommitPlacementDelegate(PlacementState placement);

    public enum ConfirmFailureReason
    {
        None,
        MissingActivePlacement,
        BlockedPlacement,
        InvalidPlacement,
        NotEnoughMoney
    }

    public readonly struct CancelContext
    {
        public readonly BuildingPlacementInputSystem InputSystem;
        public readonly BuildingPlacementPreviewPresentationSystemHelper PreviewSystem;
        public readonly DestroyPreviewDelegate DestroyPreview;

        public CancelContext(
            BuildingPlacementInputSystem inputSystem,
            BuildingPlacementPreviewPresentationSystemHelper previewSystem,
            DestroyPreviewDelegate destroyPreview)
        {
            InputSystem = inputSystem;
            PreviewSystem = previewSystem;
            DestroyPreview = destroyPreview;
        }
    }

    public struct BeginContext
    {
        public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly BuildingPlacementInputSystem InputSystem;
        public readonly BuildingPlacementPreviewPresentationSystemHelper PreviewSystem;
        public readonly Transform BuildingRoot;
        public readonly CreatePreviewDelegate CreatePreview;
        public readonly DestroyPreviewDelegate DestroyPreview;
        public readonly GetInitialOriginDelegate GetInitialOrigin;
        public readonly TryResolveInitialOriginDelegate TryResolveInitialOrigin;
        public readonly UpdatePlacementVisualDelegate UpdatePlacementVisual;
        public readonly FocusPlacementDelegate FocusPlacement;
        public readonly Action ApplyBuildCommandMode;
        public readonly Action ClearSelectedBuilding;

        public BeginContext(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            BuildingPlacementInputSystem inputSystem,
            BuildingPlacementPreviewPresentationSystemHelper previewSystem,
            Transform buildingRoot,
            CreatePreviewDelegate createPreview,
            DestroyPreviewDelegate destroyPreview,
            GetInitialOriginDelegate getInitialOrigin,
            TryResolveInitialOriginDelegate tryResolveInitialOrigin,
            UpdatePlacementVisualDelegate updatePlacementVisual,
            FocusPlacementDelegate focusPlacement,
            Action applyBuildCommandMode,
            Action clearSelectedBuilding)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            InputSystem = inputSystem;
            PreviewSystem = previewSystem;
            BuildingRoot = buildingRoot;
            CreatePreview = createPreview;
            DestroyPreview = destroyPreview;
            GetInitialOrigin = getInitialOrigin;
            TryResolveInitialOrigin = tryResolveInitialOrigin;
            UpdatePlacementVisual = updatePlacementVisual;
            FocusPlacement = focusPlacement;
            ApplyBuildCommandMode = applyBuildCommandMode;
            ClearSelectedBuilding = clearSelectedBuilding;
        }

        public CancelContext ToCancelContext()
        {
            return new CancelContext(InputSystem, PreviewSystem, DestroyPreview);
        }
    }

    public readonly struct ConfirmContext
    {
        public readonly ValidateConfirmDelegate ValidateConfirm;
        public readonly TrySpendCostDelegate TrySpendCost;
        public readonly CommitPlacementDelegate CommitPlacement;

        public ConfirmContext(
            ValidateConfirmDelegate validateConfirm,
            TrySpendCostDelegate trySpendCost,
            CommitPlacementDelegate commitPlacement)
        {
            ValidateConfirm = validateConfirm;
            TrySpendCost = trySpendCost;
            CommitPlacement = commitPlacement;
        }
    }

    public readonly struct RotateContext
    {
        public readonly UpdatePlacementVisualDelegate UpdatePlacementVisual;

        public RotateContext(UpdatePlacementVisualDelegate updatePlacementVisual)
        {
            UpdatePlacementVisual = updatePlacementVisual;
        }
    }

    public PlacementState ActivePlacement { get; private set; }
    public int ActivePlacementCost { get; private set; }
    public bool HasPendingBuildingPlacement => ActivePlacement != null;
    public bool CanConfirmBuildingPlacement => ActivePlacement != null && ActivePlacement.IsValid;

    public void SetActivePlacementCost(int cost)
    {
        ActivePlacementCost = Mathf.Max(0, cost);
    }

    public void NotifyPlacementUiPointerDown(BuildingPlacementInputSystem inputSystem)
    {
        if (ActivePlacement != null)
            inputSystem?.NotifyPlacementUiPointerDown(ActivePlacement);
    }

    public void Begin(BuildingDefinition definition, BeginContext context)
    {
        context.RuntimeGameplayStateSystem.BuildModeActive = true;
        context.RuntimeGameplayStateSystem.SelectionModeActive = false;
        context.ApplyBuildCommandMode?.Invoke();
        context.ClearSelectedBuilding?.Invoke();
        Cancel(context.ToCancelContext());
        SetActivePlacementCost(0);
        context.InputSystem?.Reset();

        if (definition == null || definition.Prefab == null || context.BuildingRoot == null)
            return;

        Vector2Int origin = context.GetInitialOrigin != null
            ? context.GetInitialOrigin(definition.FootprintCells)
            : Vector2Int.zero;
        if (context.TryResolveInitialOrigin != null &&
            context.TryResolveInitialOrigin(definition, origin, out Vector2Int resolvedOrigin))
            origin = resolvedOrigin;

        ActivePlacement = new PlacementState
        {
            Definition = definition,
            PreviewInstance = context.CreatePreview?.Invoke(definition, context.BuildingRoot),
            OriginCell = origin,
            CommittedOriginCell = origin,
            DragStartOriginCell = origin,
            DragCurrentOriginCell = origin,
            DragFirstAxis = BuildingPlacementInputSystem.DragFirstAxis.None,
            AutoRotateVertical = false,
            CommittedWallRuns = new List<BuildingPlacementInputSystem.WallRun>(),
            HideCurrentWallPreview = false,
            LastPointerMovedAt = UnityEngine.Time.time,
            LastPointerScreenPosition = GamePointerInput.TryGetPointerPosition(out Vector2 pointerPosition) ? pointerPosition : Vector2.zero
        };

        context.UpdatePlacementVisual?.Invoke(ActivePlacement, false, default);
        if (ActivePlacement != null)
            context.FocusPlacement?.Invoke(ActivePlacement);
    }

    public bool Confirm(ConfirmContext context)
    {
        return Confirm(context, out _);
    }

    public bool Confirm(ConfirmContext context, out ConfirmFailureReason failureReason)
    {
        PlacementState placement = ActivePlacement;
        if (placement == null)
        {
            failureReason = ConfirmFailureReason.MissingActivePlacement;
            return false;
        }

        if (!placement.IsValid)
        {
            failureReason = ConfirmFailureReason.BlockedPlacement;
            return false;
        }

        if (context.ValidateConfirm != null && !context.ValidateConfirm(placement))
        {
            failureReason = ConfirmFailureReason.InvalidPlacement;
            return false;
        }

        int placementCost = Mathf.Max(0, ActivePlacementCost);
        if (placementCost > 0 && context.TrySpendCost != null && !context.TrySpendCost(placementCost))
        {
            failureReason = ConfirmFailureReason.NotEnoughMoney;
            return false;
        }

        placement.OriginCell = placement.CommittedOriginCell;
        ActivePlacementCost = 0;
        context.CommitPlacement?.Invoke(placement);
        failureReason = ConfirmFailureReason.None;
        return true;
    }

    public bool Rotate(RotateContext context)
    {
        PlacementState placement = ActivePlacement;
        if (placement?.Definition == null)
            return false;

        placement.AutoRotateVertical = !placement.AutoRotateVertical;
        context.UpdatePlacementVisual?.Invoke(placement, false, default);
        return true;
    }

    public void ReleasePreviewOwnership(PlacementState placement)
    {
        if (placement != null && ReferenceEquals(placement, ActivePlacement))
            placement.PreviewInstance = null;
    }

    public void Cancel(CancelContext context)
    {
        if (ActivePlacement?.PreviewInstance != null)
            context.DestroyPreview?.Invoke(ActivePlacement.PreviewInstance);

        ActivePlacement = null;
        ActivePlacementCost = 0;
        context.InputSystem?.Reset();
        context.PreviewSystem?.HideOutline();
    }
}

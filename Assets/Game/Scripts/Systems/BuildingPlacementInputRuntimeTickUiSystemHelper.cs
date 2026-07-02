using System;
using UnityEngine;
using Game.UI.Contracts;

namespace Game.Runtime
{
    internal sealed class BuildingPlacementInputRuntimeTickUiSystemHelper
    {
        private const float DefaultClickDragThresholdPixels = 8f;

        private bool _pendingBuildingSelectionClick;
        private Vector2 _buildingSelectionPressPosition;

        public struct Context
        {
            public readonly Func<Camera> GetWorldCamera;
            public readonly Func<BuildingPlacementLifecycleCompositionSystemHelper.PlacementState> GetActivePlacement;
            public readonly BuildingPlacementInputUiSystemHelper PlacementInputSystem;
            public readonly BuildingPlacementInputUiSystemHelper.ActivePlacementPointerContext ActivePlacementPointerContext;
            public readonly Func<bool> IsPlayRequested;
            public readonly Func<bool> IsBuildModeActive;
            public readonly BuildingPlacementPreviewPresentationSystemHelper PlacementPreviewSystem;
            public readonly Func<bool> HasActiveBuilding;
            public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
            public readonly Func<IMatchRuntimeUi> GetMainMenu;
            public readonly BuildingSelectionClickUtilitySystemHelper SelectionClickSystem;
            public readonly BuildingSelectionClickUtilitySystemHelper.Context SelectionClickContext;
            public readonly Func<bool> ShouldBlockBuildingSelectionClick;
            public readonly float ClickDragThresholdPixels;
            public readonly Action ProcessPendingPlacementCommands;

            public Context(
                Func<Camera> getWorldCamera,
                Func<BuildingPlacementLifecycleCompositionSystemHelper.PlacementState> getActivePlacement,
                BuildingPlacementInputUiSystemHelper placementInputSystem,
                BuildingPlacementInputUiSystemHelper.ActivePlacementPointerContext activePlacementPointerContext,
                Func<bool> isPlayRequested,
                Func<bool> isBuildModeActive,
                BuildingPlacementPreviewPresentationSystemHelper placementPreviewSystem,
                Func<bool> hasActiveBuilding,
                RuntimeGameplayStateSystem runtimeGameplayStateSystem,
                Func<IMatchRuntimeUi> getMainMenu,
                BuildingSelectionClickUtilitySystemHelper selectionClickSystem,
                BuildingSelectionClickUtilitySystemHelper.Context selectionClickContext,
                Func<bool> shouldBlockBuildingSelectionClick,
                float clickDragThresholdPixels,
                Action processPendingPlacementCommands = null)
            {
                GetWorldCamera = getWorldCamera;
                GetActivePlacement = getActivePlacement;
                PlacementInputSystem = placementInputSystem;
                ActivePlacementPointerContext = activePlacementPointerContext;
                IsPlayRequested = isPlayRequested;
                IsBuildModeActive = isBuildModeActive;
                PlacementPreviewSystem = placementPreviewSystem;
                HasActiveBuilding = hasActiveBuilding;
                RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
                GetMainMenu = getMainMenu;
                SelectionClickSystem = selectionClickSystem;
                SelectionClickContext = selectionClickContext;
                ShouldBlockBuildingSelectionClick = shouldBlockBuildingSelectionClick;
                ClickDragThresholdPixels = clickDragThresholdPixels > 0f
                    ? clickDragThresholdPixels
                    : DefaultClickDragThresholdPixels;
                ProcessPendingPlacementCommands = processPendingPlacementCommands;
            }
        }

        public readonly struct Result
        {
            public readonly double AfterOutline;
            public readonly double AfterMouse;
            public readonly double AfterUi;
            public readonly double AfterBuildingClick;
            public readonly double AfterInput;

            public Result(
                double afterOutline,
                double afterMouse,
                double afterUi,
                double afterBuildingClick,
                double afterInput)
            {
                AfterOutline = afterOutline;
                AfterMouse = afterMouse;
                AfterUi = afterUi;
                AfterBuildingClick = afterBuildingClick;
                AfterInput = afterInput;
            }
        }

        public Result Update(Context context)
        {
            double afterOutline = 0d;
            double afterMouse = 0d;
            double afterUi = 0d;
            double afterBuildingClick = 0d;
            double afterInput = 0d;

            context.ProcessPendingPlacementCommands?.Invoke();

            if (context.GetWorldCamera?.Invoke() == null)
                return default;

            bool hasPointer = GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer);
            afterMouse = UnityEngine.Time.realtimeSinceStartupAsDouble;
            if (!hasPointer)
                return new Result(afterOutline, afterMouse, afterUi, afterBuildingClick, afterInput);

            BuildingPlacementLifecycleCompositionSystemHelper.PlacementState activePlacement = context.GetActivePlacement?.Invoke();
            if (activePlacement != null)
            {
                _pendingBuildingSelectionClick = false;
                context.PlacementInputSystem?.UpdateActivePlacementPointer(
                    activePlacement,
                    pointer,
                    context.ActivePlacementPointerContext);
                afterInput = UnityEngine.Time.realtimeSinceStartupAsDouble;
                return new Result(afterInput, afterMouse, afterInput, afterInput, afterInput);
            }

            if (context.IsPlayRequested?.Invoke() != true)
            {
                _pendingBuildingSelectionClick = false;
                context.PlacementPreviewSystem?.HideOutline();
                afterOutline = UnityEngine.Time.realtimeSinceStartupAsDouble;
                return new Result(afterOutline, afterMouse, afterOutline, afterOutline, afterOutline);
            }

            if (context.IsBuildModeActive?.Invoke() != true)
                context.PlacementPreviewSystem?.HideOutline();
            afterOutline = UnityEngine.Time.realtimeSinceStartupAsDouble;

            if (pointer.WasPressedThisFrame)
            {
                Vector2 pointerPosition = pointer.Position;
                IMatchRuntimeUi mainMenu = context.GetMainMenu?.Invoke();
                BuildingSelectionClickGate gate = GetBuildingSelectionClickGate(context, mainMenu, pointerPosition);
                afterUi = gate.MeasuredAt;

                if (!gate.BlockedByCommandMode &&
                    !gate.IgnoreBecauseCommandUiPressed &&
                    !gate.OverGameplayUi &&
                    gate.OverUnitCommandUi &&
                    gate.HasActiveBuilding)
                {
                    _pendingBuildingSelectionClick = false;
                    context.RuntimeGameplayStateSystem.SuppressNextWorldClick = true;
                    afterInput = UnityEngine.Time.realtimeSinceStartupAsDouble;
                    return new Result(afterOutline, afterMouse, afterUi, afterInput, afterInput);
                }

                _pendingBuildingSelectionClick = !gate.BlockedByCommandMode &&
                                                 !gate.IgnoreBecauseCommandUiPressed &&
                                                 !gate.OverGameplayUi &&
                                                 !gate.OverUnitCommandUi;
                if (_pendingBuildingSelectionClick)
                    _buildingSelectionPressPosition = pointerPosition;
            }

            if (pointer.WasReleasedThisFrame)
            {
                Vector2 pointerPosition = pointer.Position;
                if (_pendingBuildingSelectionClick &&
                    Vector2.Distance(_buildingSelectionPressPosition, pointerPosition) < context.ClickDragThresholdPixels)
                {
                    IMatchRuntimeUi mainMenu = context.GetMainMenu?.Invoke();
                    BuildingSelectionClickGate gate = GetBuildingSelectionClickGate(context, mainMenu, pointerPosition);
                    afterUi = Math.Max(afterUi, gate.MeasuredAt);

                    if (!gate.BlockedByCommandMode &&
                        !gate.IgnoreBecauseCommandUiPressed &&
                        !gate.OverGameplayUi &&
                        !gate.OverUnitCommandUi)
                    {
                        context.SelectionClickSystem?.HandleBuildingSelectionClick(
                            context.SelectionClickContext,
                            pointerPosition);
                        afterBuildingClick = UnityEngine.Time.realtimeSinceStartupAsDouble;
                    }
                }

                _pendingBuildingSelectionClick = false;
            }
            else if (!pointer.IsPressed)
            {
                _pendingBuildingSelectionClick = false;
            }

            afterInput = UnityEngine.Time.realtimeSinceStartupAsDouble;
            if (afterUi < afterOutline)
                afterUi = afterOutline;
            if (afterBuildingClick < afterUi)
                afterBuildingClick = afterUi;

            return new Result(afterOutline, afterMouse, afterUi, afterBuildingClick, afterInput);
        }

        private static BuildingSelectionClickGate GetBuildingSelectionClickGate(
            Context context,
            IMatchRuntimeUi mainMenu,
            Vector2 pointerPosition)
        {
            bool ignoreBecauseCommandUiPressed = mainMenu != null &&
                                                 mainMenu.ShouldIgnoreBuildingSelectionThisFrame();
            bool blockedByCommandMode = context.ShouldBlockBuildingSelectionClick?.Invoke() == true;
            bool overGameplayUi = mainMenu != null &&
                                  mainMenu.IsPointerOverAnyGameplayUi(pointerPosition, out _);
            if (!overGameplayUi && mainMenu != null)
                overGameplayUi = mainMenu.IsPointerOverRaycastableUi(pointerPosition, out _);
            bool hasActiveBuilding = context.HasActiveBuilding?.Invoke() == true;
            bool overUnitCommandUi = false;
            if (!ignoreBecauseCommandUiPressed && !overGameplayUi && hasActiveBuilding)
            {
                overUnitCommandUi = mainMenu != null &&
                                    mainMenu.IsPointerOverUnitCommandUi(pointerPosition, out _);
            }

            return new BuildingSelectionClickGate(
                blockedByCommandMode,
                ignoreBecauseCommandUiPressed,
                overGameplayUi,
                hasActiveBuilding,
                overUnitCommandUi,
                UnityEngine.Time.realtimeSinceStartupAsDouble);
        }

        private readonly struct BuildingSelectionClickGate
        {
            public readonly bool IgnoreBecauseCommandUiPressed;
            public readonly bool BlockedByCommandMode;
            public readonly bool OverGameplayUi;
            public readonly bool HasActiveBuilding;
            public readonly bool OverUnitCommandUi;
            public readonly double MeasuredAt;

            public BuildingSelectionClickGate(
                bool blockedByCommandMode,
                bool ignoreBecauseCommandUiPressed,
                bool overGameplayUi,
                bool hasActiveBuilding,
                bool overUnitCommandUi,
                double measuredAt)
            {
                BlockedByCommandMode = blockedByCommandMode;
                IgnoreBecauseCommandUiPressed = ignoreBecauseCommandUiPressed;
                OverGameplayUi = overGameplayUi;
                HasActiveBuilding = hasActiveBuilding;
                OverUnitCommandUi = overUnitCommandUi;
                MeasuredAt = measuredAt;
            }
        }
    }
}

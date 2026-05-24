using System;
using Game.Scripts.UI;
using UnityEngine;

internal sealed class BuildingPlacementInputRuntimeTickSystem
{
    public readonly struct Context
    {
        public readonly Func<Camera> GetWorldCamera;
        public readonly Func<BuildingPlacementLifecycleSystem.PlacementState> GetActivePlacement;
        public readonly BuildingPlacementInputSystem PlacementInputSystem;
        public readonly BuildingPlacementInputSystem.ActivePlacementPointerContext ActivePlacementPointerContext;
        public readonly Func<bool> IsPlayRequested;
        public readonly Func<bool> IsBuildModeActive;
        public readonly BuildingPlacementPreviewSystem PlacementPreviewSystem;
        public readonly Func<bool> HasActiveBuilding;
        public readonly RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly Func<MainMenuPlayUI> GetMainMenu;
        public readonly BuildingSelectionClickSystem SelectionClickSystem;
        public readonly BuildingSelectionClickSystem.Context SelectionClickContext;

        public Context(
            Func<Camera> getWorldCamera,
            Func<BuildingPlacementLifecycleSystem.PlacementState> getActivePlacement,
            BuildingPlacementInputSystem placementInputSystem,
            BuildingPlacementInputSystem.ActivePlacementPointerContext activePlacementPointerContext,
            Func<bool> isPlayRequested,
            Func<bool> isBuildModeActive,
            BuildingPlacementPreviewSystem placementPreviewSystem,
            Func<bool> hasActiveBuilding,
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            Func<MainMenuPlayUI> getMainMenu,
            BuildingSelectionClickSystem selectionClickSystem,
            BuildingSelectionClickSystem.Context selectionClickContext)
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

        if (context.GetWorldCamera?.Invoke() == null)
            return default;

        bool hasPointer = GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer);
        afterMouse = Time.realtimeSinceStartupAsDouble;
        if (!hasPointer)
            return new Result(afterOutline, afterMouse, afterUi, afterBuildingClick, afterInput);

        BuildingPlacementLifecycleSystem.PlacementState activePlacement = context.GetActivePlacement?.Invoke();
        if (activePlacement != null)
        {
            context.PlacementInputSystem?.UpdateActivePlacementPointer(
                activePlacement,
                pointer,
                context.ActivePlacementPointerContext);
            afterInput = Time.realtimeSinceStartupAsDouble;
            return new Result(afterInput, afterMouse, afterInput, afterInput, afterInput);
        }

        if (context.IsPlayRequested?.Invoke() != true)
        {
            context.PlacementPreviewSystem?.HideOutline();
            afterOutline = Time.realtimeSinceStartupAsDouble;
            return new Result(afterOutline, afterMouse, afterOutline, afterOutline, afterOutline);
        }

        if (context.IsBuildModeActive?.Invoke() != true)
            context.PlacementPreviewSystem?.HideOutline();
        afterOutline = Time.realtimeSinceStartupAsDouble;

        if (pointer.WasPressedThisFrame)
        {
            Vector2 pointerPosition = pointer.Position;
            MainMenuPlayUI mainMenu = context.GetMainMenu?.Invoke();
            bool ignoreBecauseCommandUiPressed = mainMenu != null &&
                                                 mainMenu.ShouldIgnoreBuildingSelectionThisFrame();
            bool overGameplayUi = mainMenu != null &&
                                  mainMenu.IsPointerOverAnyGameplayUi(pointerPosition, out _);
            bool hasActiveBuilding = context.HasActiveBuilding?.Invoke() == true;
            bool overUnitCommandUi = false;
            if (!ignoreBecauseCommandUiPressed && !overGameplayUi && hasActiveBuilding)
            {
                overUnitCommandUi = mainMenu != null &&
                                    mainMenu.IsPointerOverUnitCommandUi(pointerPosition, out _);
            }
            afterUi = Time.realtimeSinceStartupAsDouble;

            if (!ignoreBecauseCommandUiPressed && !overGameplayUi && overUnitCommandUi && hasActiveBuilding)
            {
                if (context.RuntimeGameplayStateSystem != null)
                    context.RuntimeGameplayStateSystem.SuppressNextWorldClick = true;
                afterInput = Time.realtimeSinceStartupAsDouble;
                return new Result(afterOutline, afterMouse, afterUi, afterInput, afterInput);
            }

            if (!ignoreBecauseCommandUiPressed && !overGameplayUi && !overUnitCommandUi)
            {
                context.SelectionClickSystem?.HandleBuildingSelectionClick(
                    context.SelectionClickContext,
                    pointerPosition);
                afterBuildingClick = Time.realtimeSinceStartupAsDouble;
            }
        }

        afterInput = Time.realtimeSinceStartupAsDouble;
        if (afterUi < afterOutline)
            afterUi = afterOutline;
        if (afterBuildingClick < afterUi)
            afterBuildingClick = afterUi;

        return new Result(afterOutline, afterMouse, afterUi, afterBuildingClick, afterInput);
    }
}

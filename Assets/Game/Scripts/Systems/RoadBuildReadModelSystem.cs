using System;

public sealed class RoadBuildReadModelSystem
{
    internal struct Context
    {
        public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RoadBuildSessionSystem RoadBuildSessionSystem;
        public readonly RoadBuildSessionSystem.State RoadBuildSessionState;
        public readonly RoadBuildInputSystem RoadBuildInputSystem;
        public readonly RoadBuildInputSystem.State RoadBuildInputState;
        public readonly RoadBuildPlacementStorageSystem PlacementStorageSystem;
        public readonly RoadBuildDependencySystem.State DependencyState;
        public readonly Func<bool> IsDraggingBuildingPlacement;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RoadBuildSessionSystem roadBuildSessionSystem,
            RoadBuildSessionSystem.State roadBuildSessionState,
            RoadBuildInputSystem roadBuildInputSystem,
            RoadBuildInputSystem.State roadBuildInputState,
            RoadBuildPlacementStorageSystem placementStorageSystem,
            RoadBuildDependencySystem.State dependencyState,
            Func<bool> isDraggingBuildingPlacement)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            RoadBuildSessionSystem = roadBuildSessionSystem;
            RoadBuildSessionState = roadBuildSessionState;
            RoadBuildInputSystem = roadBuildInputSystem;
            RoadBuildInputState = roadBuildInputState;
            PlacementStorageSystem = placementStorageSystem;
            DependencyState = dependencyState;
            IsDraggingBuildingPlacement = isDraggingBuildingPlacement;
        }
    }

    private Context _context;

    public bool IsRoadBuildModeActive =>
        _context.RoadBuildSessionSystem != null &&
        _context.RoadBuildSessionSystem.IsRoadBuildModeActive(CreateSessionContext());

    public bool IsDraggingBuildInteraction =>
        (_context.RoadBuildInputSystem != null && _context.RoadBuildInputSystem.IsDrawing(_context.RoadBuildInputState)) ||
        (_context.PlacementStorageSystem != null &&
         _context.PlacementStorageSystem.HasPendingBuildingPlacement &&
         _context.IsDraggingBuildingPlacement?.Invoke() == true);

    public bool HasPendingBuildingPlacement =>
        _context.PlacementStorageSystem != null &&
        _context.PlacementStorageSystem.HasPendingBuildingPlacement;

    public bool HasSelectedBuilding
    {
        get
        {
            BuildingPlacementInteractionSystem interaction = _context.DependencyState?.BuildingPlacementInteractionSystem;
            if (interaction != null)
                return interaction.HasSelectedBuilding(_context.DependencyState.BuildingPlacementInteractionContext);

            return _context.PlacementStorageSystem != null &&
                   _context.PlacementStorageSystem.HasSelectedBuilding;
        }
    }

    public bool CanConfirmBuildingPlacement
    {
        get
        {
            BuildingPlacementInteractionSystem interaction = _context.DependencyState?.BuildingPlacementInteractionSystem;
            if (interaction != null)
                return interaction.CanConfirmBuildingPlacement(_context.DependencyState.BuildingPlacementInteractionContext);

            return _context.PlacementStorageSystem != null &&
                   _context.PlacementStorageSystem.CanConfirmBuildingPlacement;
        }
    }

    public string PlacementStatusText
    {
        get
        {
            BuildingPlacementInteractionSystem interaction = _context.DependencyState?.BuildingPlacementInteractionSystem;
            if (interaction != null &&
                interaction.HasPendingBuildingPlacement(_context.DependencyState.BuildingPlacementInteractionContext))
            {
                return interaction.PlacementStatusText(_context.DependencyState.BuildingPlacementInteractionContext);
            }

            BuildingPlacementInputUiSystemHelper.IPlacementState activePlacement = _context.PlacementStorageSystem?.ActivePlacement;
            if (activePlacement == null)
                return "Choose a build type.";

            string state = activePlacement.IsValid ? "Valid placement" : "Blocked by road or blocker";
            UnityEngine.Vector2Int origin = activePlacement.OriginCell;
            UnityEngine.Vector2Int size = activePlacement.Definition.FootprintCells;
            return $"{activePlacement.Definition.DisplayName}: {state} ({origin.x},{origin.y}) {size.x}x{size.y}";
        }
    }

    public string SelectedBuildingLabel
    {
        get
        {
            BuildingPlacementInteractionSystem interaction = _context.DependencyState?.BuildingPlacementInteractionSystem;
            if (interaction != null &&
                interaction.HasActiveBuilding(_context.DependencyState.BuildingPlacementInteractionContext))
            {
                return interaction.SelectedBuildingLabel(_context.DependencyState.BuildingPlacementInteractionContext);
            }

            if (!HasSelectedBuilding)
                return "Building";

            return _context.PlacementStorageSystem != null &&
                   _context.PlacementStorageSystem.TryGetSelectedBuilding(out RuntimeBuildingEntity building)
                ? $"{building.Definition.DisplayName} ({building.OriginCell.x},{building.OriginCell.y})"
                : "Building";
        }
    }

    public string ActiveModeStatusText
    {
        get
        {
            if (_context.RoadBuildSessionSystem != null &&
                _context.RoadBuildSessionSystem.IsActiveTool(_context.RoadBuildSessionState, RoadBuildSessionSystem.BuildToolMode.Road))
            {
                return "Road build mode active";
            }

            if (HasSelectedBuilding)
                return "Building selected";
            if (_context.RuntimeGameplayStateSystem.BuildModeActive)
                return "Build mode active";
            return "Simulation running";
        }
    }

    internal void Configure(Context context)
    {
        _context = context;
    }

    public void Clear()
    {
        _context = default;
    }

    private RoadBuildSessionSystem.Context CreateSessionContext()
    {
        return new RoadBuildSessionSystem.Context(
            _context.RoadBuildSessionState,
            _context.RuntimeGameplayStateSystem,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }
}

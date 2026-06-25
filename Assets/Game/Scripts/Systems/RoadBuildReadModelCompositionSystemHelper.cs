using System;

public sealed class RoadBuildReadModelCompositionSystemHelper
{
    internal struct Context
    {
        public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RoadBuildSessionCompositionSystemHelper RoadBuildSessionCompositionSystemHelper;
        public readonly RoadBuildSessionCompositionSystemHelper.State RoadBuildSessionState;
        public readonly RoadBuildInputCompositionSystemHelper RoadBuildInputCompositionSystemHelper;
        public readonly RoadBuildInputCompositionSystemHelper.State RoadBuildInputState;
        public readonly RoadBuildPlacementStorageCompositionSystemHelper PlacementStorageSystem;
        public readonly RoadBuildDependencyCompositionSystemHelper.State DependencyState;
        public readonly Func<bool> IsDraggingBuildingPlacement;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RoadBuildSessionCompositionSystemHelper roadBuildSessionSystem,
            RoadBuildSessionCompositionSystemHelper.State roadBuildSessionState,
            RoadBuildInputCompositionSystemHelper roadBuildInputSystem,
            RoadBuildInputCompositionSystemHelper.State roadBuildInputState,
            RoadBuildPlacementStorageCompositionSystemHelper placementStorageSystem,
            RoadBuildDependencyCompositionSystemHelper.State dependencyState,
            Func<bool> isDraggingBuildingPlacement)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            RoadBuildSessionCompositionSystemHelper = roadBuildSessionSystem;
            RoadBuildSessionState = roadBuildSessionState;
            RoadBuildInputCompositionSystemHelper = roadBuildInputSystem;
            RoadBuildInputState = roadBuildInputState;
            PlacementStorageSystem = placementStorageSystem;
            DependencyState = dependencyState;
            IsDraggingBuildingPlacement = isDraggingBuildingPlacement;
        }
    }

    private Context _context;

    public bool IsRoadBuildModeActive =>
        _context.RoadBuildSessionCompositionSystemHelper != null &&
        _context.RoadBuildSessionCompositionSystemHelper.IsRoadBuildModeActive(CreateSessionContext());

    public bool IsDraggingBuildInteraction =>
        (_context.RoadBuildInputCompositionSystemHelper != null && _context.RoadBuildInputCompositionSystemHelper.IsDrawing(_context.RoadBuildInputState)) ||
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
            BuildingPlacementInteractionBoundaryCompositionSystemHelper interaction = _context.DependencyState?.BuildingPlacementInteractionBoundaryCompositionSystemHelper;
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
            BuildingPlacementInteractionBoundaryCompositionSystemHelper interaction = _context.DependencyState?.BuildingPlacementInteractionBoundaryCompositionSystemHelper;
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
            BuildingPlacementInteractionBoundaryCompositionSystemHelper interaction = _context.DependencyState?.BuildingPlacementInteractionBoundaryCompositionSystemHelper;
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
            BuildingPlacementInteractionBoundaryCompositionSystemHelper interaction = _context.DependencyState?.BuildingPlacementInteractionBoundaryCompositionSystemHelper;
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
            if (_context.RoadBuildSessionCompositionSystemHelper != null &&
                _context.RoadBuildSessionCompositionSystemHelper.IsActiveTool(_context.RoadBuildSessionState, RoadBuildSessionCompositionSystemHelper.BuildToolMode.Road))
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

    private RoadBuildSessionCompositionSystemHelper.Context CreateSessionContext()
    {
        return new RoadBuildSessionCompositionSystemHelper.Context(
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

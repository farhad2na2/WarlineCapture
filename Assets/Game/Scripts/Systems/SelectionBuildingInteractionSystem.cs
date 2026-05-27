using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SelectionBuildingInteractionSystem
{
    private readonly FocusedUnitLifecycleSystem _focusedUnitLifecycleSystem = new();
    private readonly SelectionHudFeedbackSystem _selectionHudFeedbackSystem = new();
    private readonly FocusableUnitLookupSystem _focusableUnitLookupSystem = new();
    private readonly TransportBoardingCommandSystem _transportBoardingCommandSystem = new();
    private readonly UnitTransportBoardingRuleSystem _unitTransportBoardingRuleSystem = new();
    private readonly UnitTransportBoardingQuerySystem _unitTransportBoardingQuerySystem = new();
    private readonly BuildingTargetMoveOrderSystem _buildingTargetMoveOrderSystem = new();

    private SelectionStateSystem _selectionStateSystem;
    private SelectionScreenMarkerSystem _screenMarkerSystem;
    private Camera _worldCamera;
    private World _queryWorld;
    private EntityQuery _gridConfigQuery;

    public void Init(
        SelectionStateSystem selectionStateSystem,
        SelectionScreenMarkerSystem screenMarkerSystem,
        Camera worldCamera)
    {
        _selectionStateSystem = selectionStateSystem;
        _screenMarkerSystem = screenMarkerSystem;
        _worldCamera = worldCamera;
        _selectionHudFeedbackSystem.ResetBridgeCache();
    }

    public void ClearFocusedUnit()
    {
        _focusedUnitLifecycleSystem.ClearFocusedUnit(SelectionState);
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _selectionHudFeedbackSystem.QueueClearSelection(em);
        _selectionHudFeedbackSystem.QueueClearCommandMode(em);
        _selectionHudFeedbackSystem.QueueWorldMarkersVisible(em, false);
        _selectionHudFeedbackSystem.ProcessPendingFeedback(em);
    }

    public bool IsBoardablePlayerTransportClick(Vector2 screenPosition)
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return false;

        EnsureEntityQueries(em);
        return _transportBoardingCommandSystem.IsBoardablePlayerTransportClick(
            em,
            screenPosition,
            _unitTransportBoardingRuleSystem,
            _unitTransportBoardingQuerySystem,
            TryGetClickedUnitEntity,
            TryGetClickedCell);
    }

    public bool TryIssueMoveOrderToBuilding(Vector2Int originCell, Vector2Int footprintCells)
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return false;

        bool issued = _buildingTargetMoveOrderSystem.TryIssueMoveOrderToBuilding(em, originCell, footprintCells);
        if (!issued)
            return false;

        _focusedUnitLifecycleSystem.ClearCurrentSelection(
            em,
            SelectionState,
            "MoveOrderToBuilding",
            null,
            () => _selectionHudFeedbackSystem.QueueClearSelection(em));
        _focusedUnitLifecycleSystem.ClearFocusedUnit(SelectionState);
        _selectionHudFeedbackSystem.ProcessPendingFeedback(em);

        if (TryGetPointerPosition(out Vector2 markerScreenPosition))
            _screenMarkerSystem?.RequestMoveOrderMarker(markerScreenPosition);
        return true;
    }

    private SelectionStateSystem SelectionState => _selectionStateSystem ??= new SelectionStateSystem();

    private void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _gridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        _focusableUnitLookupSystem.EnsureEntityQueries(em);
        _transportBoardingCommandSystem.EnsureEntityQueries(em);
        _focusedUnitLifecycleSystem.EnsureEntityQueries(em);
    }

    private bool TryGetClickedCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        cell = default;
        worldPoint = default;
        if (_worldCamera == null)
            return false;

        EnsureEntityQueries(em);
        if (_gridConfigQuery.IsEmptyIgnoreFilter)
            return false;

        GridConfig grid = em.GetComponentData<GridConfig>(_gridConfigQuery.GetSingletonEntity());
        Ray ray = _worldCamera.ScreenPointToRay(screenPosition);
        Plane plane = new(Vector3.up, new Vector3(0f, grid.Origin.y, 0f));
        if (!plane.Raycast(ray, out float distance))
            return false;

        worldPoint = ray.GetPoint(distance);
        cell = GridUtils.WorldToCell(grid, worldPoint);
        return GridUtils.InBounds(cell, grid.Width, grid.Height);
    }

    private bool TryGetClickedUnitEntity(Vector2 screenPosition, EntityManager em, out Entity bestEntity)
    {
        bestEntity = Entity.Null;
        if (!TryGetClickedCell(screenPosition, em, out int2 clickedCell, out _))
            return false;

        return _focusableUnitLookupSystem.TryGetClickedUnitEntity(
            em,
            _worldCamera,
            clickedCell,
            screenPosition,
            out bestEntity);
    }

    private static bool TryGetPointerPosition(out Vector2 pointerPosition)
    {
        if (GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
        {
            pointerPosition = pointer.Position;
            return true;
        }

        pointerPosition = default;
        return false;
    }

    private static bool TryGetDefaultEntityManager(out EntityManager em)
    {
        em = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        em = world.EntityManager;
        return true;
    }
}

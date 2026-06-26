using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SelectionRectangleRequestSystem
{
    public delegate void ClearCurrentSelectionAction(EntityManager em, string reason);
    public delegate void CacheSelectedEntitiesAction(EntityManager em, List<Entity> entities);
    public delegate void ApplyHudSelectionAction(EntityManager em, Entity entity);
    public delegate void ApplyHudSquadSelectionAction(int selectedCount);
    public delegate void LogSelectionAction(string message);
    public delegate bool TrySelectBuildingInRectAction(Rect screenRect);

    private readonly List<RtsSelectionPointerRequestElement> _pendingRectangleRequests = new();

    public bool ProcessPendingRequests(
        EntityManager em,
        DynamicBuffer<RtsSelectionPointerRequestElement> pointerRequests,
        Camera worldCamera,
        SelectionUiReadModelLookup selectionUiReadModelLookup,
        VisibleUnitSelectionSystem visibleUnitSelectionSystem,
        SelectionStateSystem selectionStateSystem,
        FocusedUnitLifecycleCompositionSystemHelper focusedUnitLifecycleSystem,
        List<Entity> selectedScratch,
        ClearCurrentSelectionAction clearCurrentSelection,
        CacheSelectedEntitiesAction cacheSelectedMoveEntities,
        ApplyHudSelectionAction applyHudSelection,
        ApplyHudSquadSelectionAction applyHudSquadSelection,
        LogSelectionAction logSelectionDiagnostic,
        Action clearSelectedBuilding,
        TrySelectBuildingInRectAction trySelectBuildingInRect)
    {
        _pendingRectangleRequests.Clear();
        for (int i = 0; i < pointerRequests.Length;)
        {
            RtsSelectionPointerRequestElement request = pointerRequests[i];
            if (!IsSelectionRectangleRequest(request.Kind))
            {
                i++;
                continue;
            }

            pointerRequests.RemoveAt(i);
            _pendingRectangleRequests.Add(request);
        }

        for (int i = 0; i < _pendingRectangleRequests.Count; i++)
        {
            RtsSelectionPointerRequestElement request = _pendingRectangleRequests[i];
            ApplySelectionRectangle(
                em,
                GetScreenRect(request.DragStart, request.DragCurrent),
                ResolveFilter(request.SelectionFilter),
                worldCamera,
                selectionUiReadModelLookup,
                visibleUnitSelectionSystem,
                selectionStateSystem,
                focusedUnitLifecycleSystem,
                selectedScratch,
                clearCurrentSelection,
                cacheSelectedMoveEntities,
                applyHudSelection,
                applyHudSquadSelection,
                logSelectionDiagnostic,
                clearSelectedBuilding,
                trySelectBuildingInRect);
        }

        return _pendingRectangleRequests.Count > 0;
    }

    private static bool IsSelectionRectangleRequest(RtsSelectionPointerRequestKind kind)
    {
        return kind == RtsSelectionPointerRequestKind.SelectionRectUpdated ||
               kind == RtsSelectionPointerRequestKind.SelectionRectCommitted;
    }

    private static void ApplySelectionRectangle(
        EntityManager em,
        Rect screenRect,
        VisibleUnitSelectionSystem.Filter filter,
        Camera worldCamera,
        SelectionUiReadModelLookup selectionUiReadModelLookup,
        VisibleUnitSelectionSystem visibleUnitSelectionSystem,
        SelectionStateSystem selectionStateSystem,
        FocusedUnitLifecycleCompositionSystemHelper focusedUnitLifecycleSystem,
        List<Entity> selectedScratch,
        ClearCurrentSelectionAction clearCurrentSelection,
        CacheSelectedEntitiesAction cacheSelectedMoveEntities,
        ApplyHudSelectionAction applyHudSelection,
        ApplyHudSquadSelectionAction applyHudSquadSelection,
        LogSelectionAction logSelectionDiagnostic,
        Action clearSelectedBuilding,
        TrySelectBuildingInRectAction trySelectBuildingInRect)
    {
        int selectedCount = visibleUnitSelectionSystem.CollectVisiblePlayerUnits(
            em,
            worldCamera,
            selectionUiReadModelLookup,
            screenRect,
            filter,
            selectedScratch);

        clearCurrentSelection(em, "SelectUnitsInRectangle");
        if (selectedCount == 0 && trySelectBuildingInRect != null && trySelectBuildingInRect(screenRect))
        {
            logSelectionDiagnostic?.Invoke("result=SelectRectangleBuilding selected=0 building=True");
            return;
        }

        visibleUnitSelectionSystem.ApplySelectedUnitTags(em, selectedScratch);
        cacheSelectedMoveEntities(em, selectedScratch);
        logSelectionDiagnostic?.Invoke($"result=SelectRectangle filter={filter} selected={selectedCount} cache={selectionStateSystem.CachedSelectedMoveEntities.Count}");

        Entity focusedUnit = focusedUnitLifecycleSystem.ApplySelectionFocus(
            em,
            selectionStateSystem,
            selectedScratch,
            selectedCount,
            (entityManager, entity) => applyHudSelection?.Invoke(entityManager, entity),
            count => applyHudSquadSelection?.Invoke(count));
        if (focusedUnit != Entity.Null)
            clearSelectedBuilding?.Invoke();
    }

    private static Rect GetScreenRect(float2 a, float2 b)
    {
        float2 min = math.min(a, b);
        float2 max = math.max(a, b);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static VisibleUnitSelectionSystem.Filter ResolveFilter(byte value)
    {
        return value <= (byte)VisibleUnitSelectionSystem.Filter.Vehicles
            ? (VisibleUnitSelectionSystem.Filter)value
            : VisibleUnitSelectionSystem.Filter.All;
    }
}

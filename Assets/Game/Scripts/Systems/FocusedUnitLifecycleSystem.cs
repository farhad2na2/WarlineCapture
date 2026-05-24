using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class FocusedUnitLifecycleSystem
{
    public delegate bool TryGetClickedUnitEntityDelegate(Vector2 screenPosition, EntityManager em, out Entity entity);
    public delegate string DescribeEntityDelegate(EntityManager em, Entity entity);

    private World _queryWorld;
    private EntityQuery _selectedTagQuery;

    public void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _selectedTagQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
    }

    public bool TryGetFocusedUnitEntity(EntityManager em, SelectionStateSystem selectionStateSystem, out Entity entity)
    {
        entity = Entity.Null;
        Entity focusedUnit = selectionStateSystem.FocusedUnit;
        if (focusedUnit == Entity.Null || !em.Exists(focusedUnit))
            return false;

        entity = focusedUnit;
        return true;
    }

    public void ClearFocusedUnit(SelectionStateSystem selectionStateSystem)
    {
        selectionStateSystem.ClearFocusedUnit();
    }

    public void SetFocusedUnit(SelectionStateSystem selectionStateSystem, Entity entity)
    {
        selectionStateSystem.SetFocusedUnit(entity);
    }

    public void ClearCurrentSelection(
        EntityManager em,
        SelectionStateSystem selectionStateSystem,
        string reason,
        System.Action<string> logSelectionDiagnostic,
        System.Action clearHudSelection)
    {
        EnsureEntityQueries(em);
        using NativeArray<Entity> entities = _selectedTagQuery.ToEntityArray(Allocator.Temp);
        int cacheBefore = selectionStateSystem.CachedSelectedMoveEntities.Count;
        if (entities.Length > 0 || cacheBefore > 0)
            logSelectionDiagnostic?.Invoke($"result=Clear reason={reason} selected={entities.Length} cache={cacheBefore}");

        selectionStateSystem.ClearSelectedMoveCache();
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.HasComponent<SelectedUnitTag>(entity))
                em.RemoveComponent<SelectedUnitTag>(entity);
        }

        clearHudSelection?.Invoke();
    }

    public bool RefreshFocusedUnit(
        EntityManager em,
        SelectionStateSystem selectionStateSystem,
        System.Action<EntityManager, Entity> applyHudSelection)
    {
        EnsureEntityQueries(em);
        Entity focusedUnit = selectionStateSystem.FocusedUnit;
        if (focusedUnit != Entity.Null)
        {
            if (!em.Exists(focusedUnit))
            {
                selectionStateSystem.ClearFocusedUnit();
                focusedUnit = Entity.Null;
            }
            else if (em.HasComponent<Faction>(focusedUnit) &&
                     em.GetComponentData<Faction>(focusedUnit).Id != 0 &&
                     em.HasComponent<SelectedUnitTag>(focusedUnit))
            {
                em.RemoveComponent<SelectedUnitTag>(focusedUnit);
            }
        }

        if (focusedUnit != Entity.Null)
        {
            applyHudSelection?.Invoke(em, focusedUnit);
            return true;
        }

        using NativeArray<Entity> selectedEntities = _selectedTagQuery.ToEntityArray(Allocator.Temp);
        if (selectedEntities.Length != 1)
            return false;

        Entity selectedEntity = selectedEntities[0];
        if (!em.Exists(selectedEntity) || !em.HasComponent<Faction>(selectedEntity))
            return false;

        if (em.GetComponentData<Faction>(selectedEntity).Id != 0)
            return false;

        selectionStateSystem.SetFocusedUnit(selectedEntity);
        applyHudSelection?.Invoke(em, selectedEntity);
        return true;
    }

    public Entity ApplySelectionFocus(
        EntityManager em,
        SelectionStateSystem selectionStateSystem,
        IReadOnlyList<Entity> selectedEntities,
        int selectedCount,
        System.Action<EntityManager, Entity> applyHudSelection,
        System.Action<int> applyHudSquadSelection)
    {
        Entity focusedUnit = selectedCount == 1 && selectedEntities != null && selectedEntities.Count > 0
            ? selectedEntities[0]
            : Entity.Null;

        selectionStateSystem.SetFocusedUnit(focusedUnit);
        if (focusedUnit != Entity.Null)
            applyHudSelection?.Invoke(em, focusedUnit);
        else
            applyHudSquadSelection?.Invoke(selectedCount);

        return focusedUnit;
    }

    public bool FocusUnitEntity(
        EntityManager em,
        Entity entity,
        SelectionStateSystem selectionStateSystem,
        UnitTargetOrderSystem targetOrderSystem,
        string clearReason,
        string diagnosticSource,
        System.Action<string> logSelectionDiagnostic,
        DescribeEntityDelegate describeEntity,
        System.Action clearHudSelection,
        System.Action<EntityManager, Entity> applyHudSelection)
    {
        EnsureEntityQueries(em);
        if (!em.Exists(entity) || !em.HasComponent<Faction>(entity))
            return false;

        ClearCurrentSelection(em, selectionStateSystem, clearReason, logSelectionDiagnostic, clearHudSelection);
        if (em.GetComponentData<Faction>(entity).Id == 0 && !em.HasComponent<SelectedUnitTag>(entity))
            em.AddComponent<SelectedUnitTag>(entity);

        selectionStateSystem.CacheSelectedMoveEntity(em, entity);
        string description = describeEntity != null ? describeEntity(em, entity) : entity.ToString();
        logSelectionDiagnostic?.Invoke($"result=Focus source={diagnosticSource} entity={description} cache={selectionStateSystem.CachedSelectedMoveEntities.Count}");

        selectionStateSystem.SetFocusedUnit(entity);
        if (em.HasComponent<UnitAirMovement>(entity))
            targetOrderSystem.ClearAccidentalAirSelectionMove(em, entity);

        applyHudSelection?.Invoke(em, entity);
        return true;
    }

    public bool TryFocusUnit(
        EntityManager em,
        Vector2 screenPosition,
        SelectionStateSystem selectionStateSystem,
        UnitTargetOrderSystem targetOrderSystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        string clearReason,
        string diagnosticSource,
        System.Action<string> logSelectionDiagnostic,
        DescribeEntityDelegate describeEntity,
        System.Action clearHudSelection,
        System.Action<EntityManager, Entity> applyHudSelection,
        out Entity focusedEntity)
    {
        focusedEntity = Entity.Null;
        EnsureEntityQueries(em);
        if (!tryGetClickedUnitEntity(screenPosition, em, out Entity bestEntity))
            return false;
        if (targetOrderSystem.IsBuildingEntity(em, bestEntity))
            return false;
        if (!FocusUnitEntity(
                em,
                bestEntity,
                selectionStateSystem,
                targetOrderSystem,
                clearReason,
                diagnosticSource,
                logSelectionDiagnostic,
                describeEntity,
                clearHudSelection,
                applyHudSelection))
        {
            return false;
        }

        focusedEntity = bestEntity;
        return true;
    }
}

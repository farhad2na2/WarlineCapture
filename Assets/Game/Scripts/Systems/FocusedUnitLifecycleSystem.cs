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
        NativeList<Entity> entities = CollectSelectedEntities(em);
        int cacheBefore = selectionStateSystem.CachedSelectedMoveEntities.Count;
        Entity focusedBefore = selectionStateSystem.FocusedUnit;
        try
        {
            if (entities.Length > 0 || cacheBefore > 0 || (focusedBefore != Entity.Null && em.Exists(focusedBefore)))
                SelectionRuntimeDiagnosticsSystem.LogSelectionClickDebug(
                    $"[SelectionClick] ONE_SELECTION_DEBUG action=Clear reason={reason} frame={Time.frameCount} " +
                    $"selected={entities.Length} cacheBefore={cacheBefore} focusedBefore={DescribeSelectionEntity(em, focusedBefore)}");
            if (entities.Length > 0 || cacheBefore > 0)
                logSelectionDiagnostic?.Invoke($"result=Clear reason={reason} selected={entities.Length} cache={cacheBefore}");

            selectionStateSystem.ClearSelectedMoveCache();
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (em.HasComponent<SelectedUnitTag>(entity))
                    em.RemoveComponent<SelectedUnitTag>(entity);
            }
        }
        finally
        {
            entities.Dispose();
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
                SelectionRuntimeDiagnosticsSystem.LogSelectionClickDebug($"[SelectionClick] ONE_SELECTION_DEBUG action=ClearFocused reason=FocusedEntityMissing frame={Time.frameCount} focused={focusedUnit}");
                selectionStateSystem.ClearFocusedUnit();
                focusedUnit = Entity.Null;
            }
            else if (em.HasComponent<Faction>(focusedUnit) &&
                     !FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(focusedUnit).Id) &&
                     em.HasComponent<SelectedUnitTag>(focusedUnit))
            {
                SelectionRuntimeDiagnosticsSystem.LogSelectionClickDebug(
                    $"[SelectionClick] ONE_SELECTION_DEBUG action=RemoveSelected reason=FocusedNotPlayer frame={Time.frameCount} " +
                    $"focused={DescribeSelectionEntity(em, focusedUnit)}");
                em.RemoveComponent<SelectedUnitTag>(focusedUnit);
            }
        }

        if (focusedUnit != Entity.Null)
        {
            applyHudSelection?.Invoke(em, focusedUnit);
            return true;
        }

        if (_selectedTagQuery.CalculateEntityCount() != 1)
            return false;

        Entity selectedEntity = _selectedTagQuery.GetSingletonEntity();
        if (!em.Exists(selectedEntity) || !em.HasComponent<Faction>(selectedEntity))
            return false;

        if (!FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(selectedEntity).Id))
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
        byte factionId = em.GetComponentData<Faction>(entity).Id;
        bool playerControlled = FactionIdentitySystem.IsPlayerControlled(factionId);
        if (playerControlled && !em.HasComponent<SelectedUnitTag>(entity))
            em.AddComponent<SelectedUnitTag>(entity);

        bool selectedAfterAdd = em.HasComponent<SelectedUnitTag>(entity);
        bool cacheableAfterAdd = SelectionStateSystem.IsCacheableSelectedMoveEntity(em, entity);
        selectionStateSystem.CacheSelectedMoveEntity(em, entity);
        string description = describeEntity != null ? describeEntity(em, entity) : entity.ToString();
        SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
            $"focusUnitEntity source={diagnosticSource} result=True entity={description} " +
            $"playerControlled={playerControlled} selectedAfterAdd={selectedAfterAdd} cacheable={cacheableAfterAdd} " +
            $"cacheCount={selectionStateSystem.CachedSelectedMoveEntities.Count} hasMove={em.HasComponent<UnitMove>(entity)} " +
            $"hasGrid={em.HasComponent<UnitGrid>(entity)} disabled={em.HasComponent<Disabled>(entity)} " +
            $"passenger={em.HasComponent<UnitTransportPassenger>(entity)} frame={Time.frameCount}");
        SelectionRuntimeDiagnosticsSystem.LogSelectionClickDebug(
            $"[SelectionClick] ONE_SELECTION_DEBUG action=Focus source={diagnosticSource} frame={Time.frameCount} " +
            $"entity={description} selectedAfterAdd={selectedAfterAdd} cache={selectionStateSystem.CachedSelectedMoveEntities.Count} " +
            $"playerControlled={playerControlled}");
        logSelectionDiagnostic?.Invoke(
            $"result=Focus source={diagnosticSource} entity={description} " +
            $"selectedAfterAdd={selectedAfterAdd} cacheable={cacheableAfterAdd} playerControlled={playerControlled} " +
            $"hasMove={em.HasComponent<UnitMove>(entity)} hasGrid={em.HasComponent<UnitGrid>(entity)} " +
            $"disabled={em.HasComponent<Disabled>(entity)} passenger={em.HasComponent<UnitTransportPassenger>(entity)} " +
            $"cache={selectionStateSystem.CachedSelectedMoveEntities.Count}");

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
        {
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                $"tryFocusUnit result=False reason=NoClickedUnit screen={screenPosition} frame={Time.frameCount}");
            return false;
        }
        if (targetOrderSystem.IsBuildingEntity(em, bestEntity))
        {
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                $"tryFocusUnit result=False reason=ClickedBuilding entity={DescribeSelectionEntity(em, bestEntity)} screen={screenPosition} frame={Time.frameCount}");
            return false;
        }
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

    private NativeList<Entity> CollectSelectedEntities(EntityManager em)
    {
        int count = _selectedTagQuery.CalculateEntityCount();
        NativeList<Entity> selectedEntities = new(count, Allocator.Temp);
        if (count <= 0)
            return selectedEntities;

        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = _selectedTagQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
                selectedEntities.Add(entities[i]);
        }

        return selectedEntities;
    }

    private static string DescribeSelectionEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null)
            return "null";
        if (!em.Exists(entity))
            return $"{entity}/missing";

        string source = em.HasComponent<UnitSourcePrefabKey>(entity)
            ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
            : em.GetName(entity);
        byte faction = em.HasComponent<Faction>(entity)
            ? em.GetComponentData<Faction>(entity).Id
            : (byte)0;
        string grid = em.HasComponent<UnitGrid>(entity)
            ? em.GetComponentData<UnitGrid>(entity).Cell.ToString()
            : "none";
        string target = em.HasComponent<UnitTarget>(entity)
            ? em.GetComponentData<UnitTarget>(entity).Cell.ToString()
            : "none";
        return $"{entity}/{source}/faction={faction}/selected={em.HasComponent<SelectedUnitTag>(entity)}/grid={grid}/target={target}";
    }
}

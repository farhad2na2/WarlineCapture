using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class VisibleUnitSelectionSystem
{
    public enum Filter
    {
        All,
        Soldiers,
        Vehicles
    }

    private World _queryWorld;
    private EntityQuery _visiblePlayerUnitQuery;

    public void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _visiblePlayerUnitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<LocalToWorld>(),
            ComponentType.ReadOnly<UnitGrid>());
    }

    public bool HasVisiblePlayerUnits(
        EntityManager em,
        Camera worldCamera,
        SelectionUiQuerySystem selectionUiQuerySystem,
        Rect screenRect,
        Filter filter)
    {
        return CollectVisiblePlayerUnits(
            em,
            worldCamera,
            selectionUiQuerySystem,
            screenRect,
            filter,
            null,
            stopAtFirst: true) > 0;
    }

    public int CollectVisiblePlayerUnits(
        EntityManager em,
        Camera worldCamera,
        SelectionUiQuerySystem selectionUiQuerySystem,
        Rect screenRect,
        Filter filter,
        List<Entity> selected)
    {
        return CollectVisiblePlayerUnits(
            em,
            worldCamera,
            selectionUiQuerySystem,
            screenRect,
            filter,
            selected,
            stopAtFirst: false);
    }

    private int CollectVisiblePlayerUnits(
        EntityManager em,
        Camera worldCamera,
        SelectionUiQuerySystem selectionUiQuerySystem,
        Rect screenRect,
        Filter filter,
        List<Entity> selected,
        bool stopAtFirst)
    {
        selected?.Clear();
        if (worldCamera == null || selectionUiQuerySystem == null)
            return 0;

        EnsureEntityQueries(em);
        int count = 0;
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<LocalToWorld> localToWorldType = em.GetComponentTypeHandle<LocalToWorld>(true);
        using NativeArray<ArchetypeChunk> chunks = _visiblePlayerUnitQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<LocalToWorld> transforms = chunk.GetNativeArray(ref localToWorldType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!IsVisiblePlayerUnit(em, entity, selectionUiQuerySystem, filter))
                    continue;

                float3 pos = transforms[i].Position;
                Vector3 screen = worldCamera.WorldToScreenPoint(pos);
                if (screen.z <= 0f)
                    continue;

                if (screenRect.Contains(new Vector2(screen.x, screen.y)))
                {
                    if (stopAtFirst)
                        return 1;

                    selected?.Add(entity);
                    count++;
                }
            }
        }

        return count;
    }

    public void ApplySelectedUnitTags(EntityManager em, IReadOnlyList<Entity> selected)
    {
        for (int i = 0; i < selected.Count; i++)
        {
            Entity entity = selected[i];
            if (!em.HasComponent<SelectedUnitTag>(entity))
                em.AddComponent<SelectedUnitTag>(entity);
        }
    }

    private static bool IsVisiblePlayerUnit(
        EntityManager em,
        Entity entity,
        SelectionUiQuerySystem selectionUiQuerySystem,
        Filter filter)
    {
        if (em.HasComponent<Prefab>(entity) || em.HasComponent<StaticGridBlocker>(entity))
            return false;

        if (!FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id))
            return false;

        if (!em.HasComponent<UnitMove>(entity))
            return false;

        bool isVehicle = selectionUiQuerySystem.IsVehicleForVisibleSelection(em, entity);
        if (filter == Filter.Soldiers && isVehicle)
            return false;
        if (filter == Filter.Vehicles && !isVehicle)
            return false;

        return true;
    }
}

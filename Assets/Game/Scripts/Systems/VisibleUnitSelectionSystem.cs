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
        if (worldCamera == null || selectionUiQuerySystem == null)
            return false;

        EnsureEntityQueries(em);
        using var entities = _visiblePlayerUnitQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!IsVisiblePlayerUnit(em, entity, selectionUiQuerySystem, filter))
                continue;

            Vector3 screen = worldCamera.WorldToScreenPoint(em.GetComponentData<LocalToWorld>(entity).Position);
            if (screen.z > 0f && screenRect.Contains(new Vector2(screen.x, screen.y)))
                return true;
        }

        return false;
    }

    public int CollectVisiblePlayerUnits(
        EntityManager em,
        Camera worldCamera,
        SelectionUiQuerySystem selectionUiQuerySystem,
        Rect screenRect,
        Filter filter,
        List<Entity> selected)
    {
        selected.Clear();
        if (worldCamera == null || selectionUiQuerySystem == null)
            return 0;

        EnsureEntityQueries(em);
        using var entities = _visiblePlayerUnitQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!IsVisiblePlayerUnit(em, entity, selectionUiQuerySystem, filter))
                continue;

            float3 pos = em.GetComponentData<LocalToWorld>(entity).Position;
            Vector3 screen = worldCamera.WorldToScreenPoint(pos);
            if (screen.z <= 0f)
                continue;

            if (screenRect.Contains(new Vector2(screen.x, screen.y)))
                selected.Add(entity);
        }

        return selected.Count;
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

        if (em.GetComponentData<Faction>(entity).Id != 0)
            return false;

        bool isVehicle = selectionUiQuerySystem.IsVehicleForVisibleSelection(em, entity);
        if (filter == Filter.Soldiers && isVehicle)
            return false;
        if (filter == Filter.Vehicles && !isVehicle)
            return false;

        return true;
    }
}

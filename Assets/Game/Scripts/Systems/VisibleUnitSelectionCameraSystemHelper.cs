using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class VisibleUnitSelectionCameraSystemHelper
{
    public enum Filter
    {
        All,
        Soldiers,
        Vehicles
    }

    private Unity.Entities.World _queryWorld;
    private EntityQuery _visiblePlayerUnitQuery;

    public void EnsureEntityQueries(EntityManager em)
    {
        Unity.Entities.World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _visiblePlayerUnitQuery = em.CreateEntityQuery(VisibleUnitSelectionCandidateCollector.CreateQueryDesc());
    }

    public bool HasVisiblePlayerUnits(
        EntityManager em,
        Camera worldCamera,
        SelectionUiReadModelLookup selectionUiReadModelLookup,
        Rect screenRect,
        Filter filter)
    {
        return CollectVisiblePlayerUnits(
            em,
            worldCamera,
            selectionUiReadModelLookup,
            screenRect,
            filter,
            null,
            stopAtFirst: true) > 0;
    }

    public int CollectVisiblePlayerUnits(
        EntityManager em,
        Camera worldCamera,
        SelectionUiReadModelLookup selectionUiReadModelLookup,
        Rect screenRect,
        Filter filter,
        List<Entity> selected)
    {
        return CollectVisiblePlayerUnits(
            em,
            worldCamera,
            selectionUiReadModelLookup,
            screenRect,
            filter,
            selected,
            stopAtFirst: false);
    }

    private int CollectVisiblePlayerUnits(
        EntityManager em,
        Camera worldCamera,
        SelectionUiReadModelLookup selectionUiReadModelLookup,
        Rect screenRect,
        Filter filter,
        List<Entity> selected,
        bool stopAtFirst)
    {
        selected?.Clear();
        if (worldCamera == null || selectionUiReadModelLookup == null)
            return 0;

        EnsureEntityQueries(em);
        int candidateCapacity = _visiblePlayerUnitQuery.CalculateEntityCount();
        using NativeList<VisibleUnitSelectionCandidate> candidates = new(candidateCapacity, Allocator.TempJob);
        VisibleUnitSelectionCandidateCollector.Collect(em, _visiblePlayerUnitQuery, filter, candidates);

        int count = 0;
        for (int i = 0; i < candidates.Length; i++)
        {
            VisibleUnitSelectionCandidate candidate = candidates[i];
            Vector3 screen = worldCamera.WorldToScreenPoint(candidate.Position);
            if (screen.z <= 0f)
                continue;

            if (screenRect.Contains(new Vector2(screen.x, screen.y)))
            {
                if (stopAtFirst)
                    return 1;

                selected?.Add(candidate.Entity);
                count++;
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

    public struct VisibleUnitSelectionCandidate
    {
        public Entity Entity;
        public float3 Position;
        public byte IsVehicle;
    }
}

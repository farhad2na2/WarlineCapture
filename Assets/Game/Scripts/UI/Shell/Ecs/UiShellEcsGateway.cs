using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public static class UiShellEcsGateway
{
    private static World cachedWorld;
    private static EntityQuery boundaryQuery;
    private static bool hasBoundaryQuery;

    public static bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        DynamicBuffer<UiShellRouteRequestComponent> requests =
            entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        requests.Add(new UiShellRouteRequestComponent
        {
            Intent = intent,
            Route = route,
            PushHistory = pushHistory ? (byte)1 : (byte)0
        });
        return true;
    }

    public static bool TryReadLoadingProgress(out UiShellLoadingProgressComponent loading)
    {
        loading = default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        if (!entityManager.HasComponent<UiShellLoadingProgressComponent>(boundary))
            return false;

        loading = entityManager.GetComponentData<UiShellLoadingProgressComponent>(boundary);
        return true;
    }

    public static bool TrySetLoadingProgress(float progress01, FixedString64Bytes status, bool complete)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        entityManager.SetComponentData(boundary, new UiShellLoadingProgressComponent
        {
            Progress01 = Mathf.Clamp01(progress01),
            Status = status,
            IsComplete = complete ? (byte)1 : (byte)0
        });
        return true;
    }

    public static bool TryReadShellState(out UiShellStateComponent state)
    {
        state = default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        if (!entityManager.HasComponent<UiShellStateComponent>(boundary))
            return false;

        state = entityManager.GetComponentData<UiShellStateComponent>(boundary);
        return true;
    }

    public static bool TryReadArmoryCategory(out ArmoryCatalogCategory category)
    {
        category = ArmoryCatalogCategory.Characters;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        EnsureArmoryCategoryState(entityManager, boundary);
        category = entityManager.GetComponentData<UiShellArmoryCategoryComponent>(boundary).Category;
        return true;
    }

    public static bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        EnsureArmoryCategoryState(entityManager, boundary);
        DynamicBuffer<UiShellArmoryCategoryRequestComponent> requests =
            entityManager.GetBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
        requests.Add(new UiShellArmoryCategoryRequestComponent
        {
            Category = category
        });
        return true;
    }

    public static bool TryConsumePresentationCommands(List<UiShellPresentationCommandComponent> commands)
    {
        if (commands == null)
            return false;

        commands.Clear();
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        if (!entityManager.HasBuffer<UiShellPresentationCommandComponent>(boundary))
            return false;

        DynamicBuffer<UiShellPresentationCommandComponent> buffer =
            entityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary);
        if (buffer.Length == 0)
            return false;

        for (int i = 0; i < buffer.Length; i++)
            commands.Add(buffer[i]);
        buffer.Clear();
        return commands.Count > 0;
    }

    public static bool TryEnqueueTransitionComplete(UiShellTransitionCompleteComponent completion)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        if (!entityManager.HasBuffer<UiShellTransitionCompleteComponent>(boundary))
            return false;

        DynamicBuffer<UiShellTransitionCompleteComponent> completions =
            entityManager.GetBuffer<UiShellTransitionCompleteComponent>(boundary);
        completions.Add(completion);
        return true;
    }

    private static bool TryGetBoundary(out EntityManager entityManager, out Entity boundary)
    {
        entityManager = default;
        boundary = Entity.Null;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        if (cachedWorld != world || !hasBoundaryQuery)
        {
            cachedWorld = world;
            boundaryQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellBoundaryComponent>());
            hasBoundaryQuery = true;
        }

        if (boundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entityManager = world.EntityManager;
        boundary = boundaryQuery.GetSingletonEntity();
        return true;
    }

    private static void EnsureArmoryCategoryState(EntityManager entityManager, Entity boundary)
    {
        if (!entityManager.HasComponent<UiShellArmoryCategoryComponent>(boundary))
        {
            entityManager.AddComponentData(boundary, new UiShellArmoryCategoryComponent
            {
                Category = ArmoryCatalogCategory.Characters
            });
        }

        if (!entityManager.HasBuffer<UiShellArmoryCategoryRequestComponent>(boundary))
            entityManager.AddBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
    }
}

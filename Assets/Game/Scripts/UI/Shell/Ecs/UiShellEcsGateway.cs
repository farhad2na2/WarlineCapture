using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class UiShellEcsGateway : IUiShellRuntimeGateway
{
    private static readonly UiShellEcsGateway Shared = new();
    private static World cachedWorld;
    private static EntityQuery boundaryQuery;
    private static bool hasBoundaryQuery;

    private UiShellEcsGateway()
    {
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void RegisterAsRuntimeGateway()
    {
        cachedWorld = null;
        boundaryQuery = default;
        hasBoundaryQuery = false;
        UiShellRuntimeGateway.Register(Shared);
    }

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

    public static bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading)
    {
        loading = default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        if (!entityManager.HasComponent<UiShellLoadingProgressComponent>(boundary))
            return false;

        UiShellLoadingProgressComponent component =
            entityManager.GetComponentData<UiShellLoadingProgressComponent>(boundary);
        loading = new UiShellLoadingProgressModel(
            component.Progress01,
            component.Status.ToString(),
            component.IsComplete != 0);
        return true;
    }

    public static bool TrySetLoadingProgress(float progress01, string status, bool complete)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        entityManager.SetComponentData(boundary, new UiShellLoadingProgressComponent
        {
            Progress01 = Mathf.Clamp01(progress01),
            Status = new FixedString64Bytes(status ?? string.Empty),
            IsComplete = complete ? (byte)1 : (byte)0
        });
        return true;
    }

    public static bool TryReadShellState(out UiShellStateModel state)
    {
        state = default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        if (!entityManager.HasComponent<UiShellStateComponent>(boundary))
            return false;

        UiShellStateComponent component = entityManager.GetComponentData<UiShellStateComponent>(boundary);
        state = new UiShellStateModel(
            component.CurrentMode,
            component.ActiveRoute,
            component.Phase,
            component.TransitionSequenceId,
            component.IsTransitionRunning != 0);
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

    public static bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands)
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
        {
            UiShellPresentationCommandComponent command = buffer[i];
            commands.Add(new UiShellPresentationCommandModel(
                command.Kind,
                command.Region,
                command.Route,
                command.TargetMode,
                command.SequenceId));
        }

        buffer.Clear();
        return commands.Count > 0;
    }

    public static bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        if (!entityManager.HasBuffer<UiShellTransitionCompleteComponent>(boundary))
            return false;

        DynamicBuffer<UiShellTransitionCompleteComponent> completions =
            entityManager.GetBuffer<UiShellTransitionCompleteComponent>(boundary);
        completions.Add(new UiShellTransitionCompleteComponent
        {
            Kind = completion.Kind,
            Region = completion.Region,
            SequenceId = completion.SequenceId
        });
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

    bool IUiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
    {
        return TryEnqueueRouteRequest(intent, route, pushHistory);
    }

    bool IUiShellRuntimeGateway.TryReadLoadingProgress(out UiShellLoadingProgressModel loading)
    {
        return TryReadLoadingProgress(out loading);
    }

    bool IUiShellRuntimeGateway.TrySetLoadingProgress(float progress01, string status, bool complete)
    {
        return TrySetLoadingProgress(progress01, status, complete);
    }

    bool IUiShellRuntimeGateway.TryReadShellState(out UiShellStateModel state)
    {
        return TryReadShellState(out state);
    }

    bool IUiShellRuntimeGateway.TryReadArmoryCategory(out ArmoryCatalogCategory category)
    {
        return TryReadArmoryCategory(out category);
    }

    bool IUiShellRuntimeGateway.TryEnqueueArmoryCategory(ArmoryCatalogCategory category)
    {
        return TryEnqueueArmoryCategory(category);
    }

    bool IUiShellRuntimeGateway.TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands)
    {
        return TryConsumePresentationCommands(commands);
    }

    bool IUiShellRuntimeGateway.TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion)
    {
        return TryEnqueueTransitionComplete(completion);
    }
}

using System.Collections.Generic;
using Unity.Collections;

public interface IUiShellRuntimeGateway
{
    bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory);

    bool TryReadLoadingProgress(out UiShellLoadingProgressComponent loading);

    bool TrySetLoadingProgress(float progress01, FixedString64Bytes status, bool complete);

    bool TryReadShellState(out UiShellStateComponent state);

    bool TryReadArmoryCategory(out ArmoryCatalogCategory category);

    bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category);

    bool TryConsumePresentationCommands(List<UiShellPresentationCommandComponent> commands);

    bool TryEnqueueTransitionComplete(UiShellTransitionCompleteComponent completion);
}

public static class UiShellRuntimeGateway
{
    private static IUiShellRuntimeGateway current = NullUiShellRuntimeGateway.Instance;

    public static void Register(IUiShellRuntimeGateway gateway)
    {
        current = gateway ?? NullUiShellRuntimeGateway.Instance;
    }

    public static bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
    {
        return current.TryEnqueueRouteRequest(intent, route, pushHistory);
    }

    public static bool TryReadLoadingProgress(out UiShellLoadingProgressComponent loading)
    {
        return current.TryReadLoadingProgress(out loading);
    }

    public static bool TrySetLoadingProgress(float progress01, FixedString64Bytes status, bool complete)
    {
        return current.TrySetLoadingProgress(progress01, status, complete);
    }

    public static bool TryReadShellState(out UiShellStateComponent state)
    {
        return current.TryReadShellState(out state);
    }

    public static bool TryReadArmoryCategory(out ArmoryCatalogCategory category)
    {
        return current.TryReadArmoryCategory(out category);
    }

    public static bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category)
    {
        return current.TryEnqueueArmoryCategory(category);
    }

    public static bool TryConsumePresentationCommands(List<UiShellPresentationCommandComponent> commands)
    {
        return current.TryConsumePresentationCommands(commands);
    }

    public static bool TryEnqueueTransitionComplete(UiShellTransitionCompleteComponent completion)
    {
        return current.TryEnqueueTransitionComplete(completion);
    }

    private sealed class NullUiShellRuntimeGateway : IUiShellRuntimeGateway
    {
        public static readonly NullUiShellRuntimeGateway Instance = new();

        public bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
        {
            return false;
        }

        public bool TryReadLoadingProgress(out UiShellLoadingProgressComponent loading)
        {
            loading = default;
            return false;
        }

        public bool TrySetLoadingProgress(float progress01, FixedString64Bytes status, bool complete)
        {
            return false;
        }

        public bool TryReadShellState(out UiShellStateComponent state)
        {
            state = default;
            return false;
        }

        public bool TryReadArmoryCategory(out ArmoryCatalogCategory category)
        {
            category = ArmoryCatalogCategory.Characters;
            return false;
        }

        public bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category)
        {
            return false;
        }

        public bool TryConsumePresentationCommands(List<UiShellPresentationCommandComponent> commands)
        {
            commands?.Clear();
            return false;
        }

        public bool TryEnqueueTransitionComplete(UiShellTransitionCompleteComponent completion)
        {
            return false;
        }
    }
}

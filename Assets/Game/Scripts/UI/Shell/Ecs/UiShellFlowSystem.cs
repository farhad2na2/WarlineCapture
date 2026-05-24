using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UiShellFlowSystem : ISystem
{
    private EntityQuery boundaryQuery;

    public void OnCreate(ref SystemState state)
    {
        boundaryQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<UiShellStateComponent>(),
            ComponentType.ReadWrite<UiShellLoadingProgressComponent>(),
            ComponentType.ReadWrite<UiShellRouteRequestComponent>(),
            ComponentType.ReadWrite<UiShellPopupRequestComponent>(),
            ComponentType.ReadWrite<UiShellPresentationCommandComponent>(),
            ComponentType.ReadWrite<UiShellTransitionCompleteComponent>(),
            ComponentType.ReadOnly<UiShellBoundaryComponent>());
        state.RequireForUpdate(boundaryQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity boundary = boundaryQuery.GetSingletonEntity();
        UiShellStateComponent shellState = state.EntityManager.GetComponentData<UiShellStateComponent>(boundary);
        UiShellLoadingProgressComponent loading = state.EntityManager.GetComponentData<UiShellLoadingProgressComponent>(boundary);
        DynamicBuffer<UiShellRouteRequestComponent> routeRequests = state.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        DynamicBuffer<UiShellPopupRequestComponent> popupRequests = state.EntityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
        DynamicBuffer<UiShellPresentationCommandComponent> commands = state.EntityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary);
        DynamicBuffer<UiShellTransitionCompleteComponent> completions = state.EntityManager.GetBuffer<UiShellTransitionCompleteComponent>(boundary);

        ConsumeCompletions(ref shellState, completions);
        completions.Clear();

        if (shellState.IsTransitionRunning != 0)
        {
            state.EntityManager.SetComponentData(boundary, shellState);
            return;
        }

        if (shellState.CurrentMode == UiShellMode.None)
        {
            BeginCommandSequence(ref shellState, commands, UiShellCommandKind.ShowLoading, UiShellRegionId.LoadingLayer, WarlineCaptureRoute.Splash, UiShellMode.Loading);
            shellState.CurrentMode = UiShellMode.Loading;
            shellState.ActiveRoute = WarlineCaptureRoute.Splash;
            shellState.Phase = UiShellTransitionPhase.ShowingLoading;
            state.EntityManager.SetComponentData(boundary, shellState);
            return;
        }

        if (TryConsumePopupRequest(popupRequests, out UiShellPopupRequestComponent popupRequest))
        {
            ProcessPopupRequest(ref shellState, commands, popupRequest);
            state.EntityManager.SetComponentData(boundary, shellState);
            return;
        }

        if (TryConsumeRouteRequest(routeRequests, out UiShellRouteRequestComponent routeRequest))
        {
            ProcessRouteRequest(ref shellState, commands, routeRequest);
            state.EntityManager.SetComponentData(boundary, shellState);
            return;
        }

        if (shellState.CurrentMode == UiShellMode.Loading && loading.IsComplete != 0)
        {
            BeginCommandSequence(ref shellState, commands, UiShellCommandKind.ExitLoading, UiShellRegionId.LoadingLayer, WarlineCaptureRoute.MainMenu, UiShellMode.MainMenu);
            AppendCommand(commands, shellState, UiShellCommandKind.EnterMenu, UiShellRegionId.None, WarlineCaptureRoute.MainMenu, UiShellMode.MainMenu);
            shellState.CurrentMode = UiShellMode.MainMenu;
            shellState.ActiveRoute = WarlineCaptureRoute.MainMenu;
            shellState.Phase = UiShellTransitionPhase.EnteringMenu;
            state.EntityManager.SetComponentData(boundary, shellState);
            return;
        }

        state.EntityManager.SetComponentData(boundary, shellState);
    }

    private static void ProcessRouteRequest(
        ref UiShellStateComponent shellState,
        DynamicBuffer<UiShellPresentationCommandComponent> commands,
        UiShellRouteRequestComponent request)
    {
        switch (request.Intent)
        {
            case UiShellRouteIntent.EnterMatch:
                BeginCommandSequence(ref shellState, commands, UiShellCommandKind.ShowLoading, UiShellRegionId.LoadingLayer, WarlineCaptureRoute.Match, UiShellMode.Loading);
                AppendCommand(commands, shellState, UiShellCommandKind.ExitMenu, UiShellRegionId.None, WarlineCaptureRoute.Match, UiShellMode.Loading);
                AppendCommand(commands, shellState, UiShellCommandKind.EnterMatchHud, UiShellRegionId.None, WarlineCaptureRoute.Match, UiShellMode.MatchHud);
                shellState.CurrentMode = UiShellMode.MatchHud;
                shellState.ActiveRoute = WarlineCaptureRoute.Match;
                shellState.Phase = UiShellTransitionPhase.EnteringMatchHud;
                break;
            case UiShellRouteIntent.ReturnToMainMenu:
                BeginCommandSequence(ref shellState, commands, UiShellCommandKind.ShowLoading, UiShellRegionId.LoadingLayer, WarlineCaptureRoute.MainMenu, UiShellMode.Loading);
                AppendCommand(commands, shellState, UiShellCommandKind.EnterMenu, UiShellRegionId.None, WarlineCaptureRoute.MainMenu, UiShellMode.MainMenu);
                shellState.CurrentMode = UiShellMode.MainMenu;
                shellState.ActiveRoute = WarlineCaptureRoute.MainMenu;
                shellState.Phase = UiShellTransitionPhase.EnteringMenu;
                break;
            default:
                ProcessMenuRouteRequest(ref shellState, commands, request.Route);
                break;
        }
    }

    private static void ProcessMenuRouteRequest(
        ref UiShellStateComponent shellState,
        DynamicBuffer<UiShellPresentationCommandComponent> commands,
        WarlineCaptureRoute route)
    {
        if (shellState.CurrentMode != UiShellMode.MainMenu)
        {
            BeginCommandSequence(ref shellState, commands, UiShellCommandKind.EnterMenu, UiShellRegionId.None, route, UiShellMode.MainMenu);
            shellState.CurrentMode = UiShellMode.MainMenu;
            shellState.ActiveRoute = route;
            shellState.Phase = UiShellTransitionPhase.EnteringMenu;
            return;
        }

        BeginCommandSequence(ref shellState, commands, UiShellCommandKind.SwapMenuMiddle, UiShellRegionId.MiddleRegion, route, UiShellMode.MainMenu);
        shellState.ActiveRoute = route;
        shellState.Phase = UiShellTransitionPhase.MenuReady;
    }

    private static void ProcessPopupRequest(
        ref UiShellStateComponent shellState,
        DynamicBuffer<UiShellPresentationCommandComponent> commands,
        UiShellPopupRequestComponent request)
    {
        UiShellCommandKind kind = request.Intent == UiShellPopupIntent.Hide
            ? UiShellCommandKind.HidePopup
            : UiShellCommandKind.ShowPopup;
        BeginCommandSequence(ref shellState, commands, kind, UiShellRegionId.PopupLayer, shellState.ActiveRoute, UiShellMode.PopupOnly);
        shellState.Phase = request.Intent == UiShellPopupIntent.Hide
            ? UiShellTransitionPhase.HidingPopup
            : UiShellTransitionPhase.ShowingPopup;
    }

    private static bool TryConsumeRouteRequest(
        DynamicBuffer<UiShellRouteRequestComponent> routeRequests,
        out UiShellRouteRequestComponent request)
    {
        if (routeRequests.Length == 0)
        {
            request = default;
            return false;
        }

        request = routeRequests[0];
        routeRequests.Clear();
        return true;
    }

    private static bool TryConsumePopupRequest(
        DynamicBuffer<UiShellPopupRequestComponent> popupRequests,
        out UiShellPopupRequestComponent request)
    {
        if (popupRequests.Length == 0)
        {
            request = default;
            return false;
        }

        request = popupRequests[0];
        popupRequests.Clear();
        return true;
    }

    private static void BeginCommandSequence(
        ref UiShellStateComponent shellState,
        DynamicBuffer<UiShellPresentationCommandComponent> commands,
        UiShellCommandKind kind,
        UiShellRegionId region,
        WarlineCaptureRoute route,
        UiShellMode targetMode)
    {
        shellState.TransitionSequenceId++;
        shellState.IsTransitionRunning = 1;
        commands.Clear();
        AppendCommand(commands, shellState, kind, region, route, targetMode);
    }

    private static void AppendCommand(
        DynamicBuffer<UiShellPresentationCommandComponent> commands,
        UiShellStateComponent shellState,
        UiShellCommandKind kind,
        UiShellRegionId region,
        WarlineCaptureRoute route,
        UiShellMode targetMode)
    {
        commands.Add(new UiShellPresentationCommandComponent
        {
            Kind = kind,
            Region = region,
            Route = route,
            TargetMode = targetMode,
            SequenceId = shellState.TransitionSequenceId
        });
    }

    private static void ConsumeCompletions(
        ref UiShellStateComponent shellState,
        DynamicBuffer<UiShellTransitionCompleteComponent> completions)
    {
        for (int i = 0; i < completions.Length; i++)
        {
            UiShellTransitionCompleteComponent completion = completions[i];
            if (completion.SequenceId != shellState.TransitionSequenceId)
                continue;

            shellState.IsTransitionRunning = 0;
            shellState.Phase = CompletionPhase(completion.Kind);
        }
    }

    private static UiShellTransitionPhase CompletionPhase(UiShellCommandKind kind)
    {
        return kind switch
        {
            UiShellCommandKind.ShowLoading => UiShellTransitionPhase.ShowingLoading,
            UiShellCommandKind.ExitLoading => UiShellTransitionPhase.Idle,
            UiShellCommandKind.EnterMenu => UiShellTransitionPhase.MenuReady,
            UiShellCommandKind.SwapMenuMiddle => UiShellTransitionPhase.MenuReady,
            UiShellCommandKind.SwapLeftRegion => UiShellTransitionPhase.MenuReady,
            UiShellCommandKind.SwapRightRegion => UiShellTransitionPhase.MenuReady,
            UiShellCommandKind.EnterMatchHud => UiShellTransitionPhase.MatchHudReady,
            UiShellCommandKind.ExitMatchHud => UiShellTransitionPhase.Idle,
            UiShellCommandKind.ShowPopup => UiShellTransitionPhase.PopupVisible,
            UiShellCommandKind.HidePopup => UiShellTransitionPhase.Idle,
            _ => UiShellTransitionPhase.Idle
        };
    }
}

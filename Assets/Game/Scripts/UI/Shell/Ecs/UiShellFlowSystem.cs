using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UiShellFlowSystem : ISystem
{
    private EntityQuery boundaryQuery;

    public void OnCreate(ref SystemState state)
    {
        UiShellEcsGateway.RegisterAsRuntimeGateway();
        boundaryQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<UiShellStateComponent>(),
            ComponentType.ReadWrite<UiShellLoadingProgressComponent>(),
            ComponentType.ReadWrite<UiShellLoadingProgressRequestComponent>(),
            ComponentType.ReadWrite<MatchIntroTransitionComponent>(),
            ComponentType.ReadWrite<UiShellRouteRequestComponent>(),
            ComponentType.ReadWrite<UiShellRouteHistoryComponent>(),
            ComponentType.ReadWrite<UiShellActivePopupComponent>(),
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
        DynamicBuffer<UiShellLoadingProgressRequestComponent> loadingRequests =
            state.EntityManager.GetBuffer<UiShellLoadingProgressRequestComponent>(boundary);
        MatchIntroTransitionComponent matchIntro = state.EntityManager.GetComponentData<MatchIntroTransitionComponent>(boundary);
        DynamicBuffer<UiShellRouteRequestComponent> routeRequests = state.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        DynamicBuffer<UiShellRouteHistoryComponent> routeHistory = state.EntityManager.GetBuffer<UiShellRouteHistoryComponent>(boundary);
        DynamicBuffer<UiShellPopupRequestComponent> popupRequests = state.EntityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
        DynamicBuffer<UiShellPresentationCommandComponent> commands = state.EntityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary);
        DynamicBuffer<UiShellTransitionCompleteComponent> completions = state.EntityManager.GetBuffer<UiShellTransitionCompleteComponent>(boundary);
        UiShellActivePopupComponent activePopup = state.EntityManager.GetComponentData<UiShellActivePopupComponent>(boundary);

        ConsumeCompletions(ref shellState, ref matchIntro, completions);
        completions.Clear();

        if (TryConsumeLoadingProgressRequest(loadingRequests, out UiShellLoadingProgressRequestComponent loadingRequest))
        {
            loading.Progress01 = UnityEngine.Mathf.Clamp01(loadingRequest.Progress01);
            loading.Status = loadingRequest.Status;
            loading.IsComplete = loadingRequest.IsComplete;
            state.EntityManager.SetComponentData(boundary, loading);
        }

        if (shellState.IsTransitionRunning != 0)
        {
            state.EntityManager.SetComponentData(boundary, shellState);
            state.EntityManager.SetComponentData(boundary, matchIntro);
            return;
        }

        if (shellState.CurrentMode == UiShellMode.None)
        {
            BeginCommandSequence(ref shellState, commands, UiShellCommandKind.EnterMenu, UiShellRegionId.None, UIRoute.MainMenu, UiShellMode.MainMenu);
            shellState.CurrentMode = UiShellMode.MainMenu;
            shellState.ActiveRoute = UIRoute.MainMenu;
            shellState.Phase = UiShellTransitionPhase.EnteringMenu;
            state.EntityManager.SetComponentData(boundary, shellState);
            state.EntityManager.SetComponentData(boundary, matchIntro);
            return;
        }

        if (TryConsumePopupRequest(popupRequests, out UiShellPopupRequestComponent popupRequest))
        {
            ProcessPopupRequest(ref shellState, ref activePopup, commands, popupRequest);
            state.EntityManager.SetComponentData(boundary, shellState);
            state.EntityManager.SetComponentData(boundary, activePopup);
            state.EntityManager.SetComponentData(boundary, matchIntro);
            return;
        }

        if (TryConsumeRouteRequest(routeRequests, out UiShellRouteRequestComponent routeRequest))
        {
            ProcessRouteRequest(ref shellState, ref matchIntro, commands, routeHistory, routeRequest);
            if (routeRequest.Intent == UiShellRouteIntent.EnterMatch ||
                routeRequest.Intent == UiShellRouteIntent.ReturnToMainMenu)
            {
                ResetLoading(ref loading, "Loading operation interface");
                state.EntityManager.SetComponentData(boundary, loading);
            }
            state.EntityManager.SetComponentData(boundary, shellState);
            state.EntityManager.SetComponentData(boundary, matchIntro);
            return;
        }

        if (shellState.CurrentMode == UiShellMode.Loading && loading.IsComplete != 0)
        {
            if (shellState.ActiveRoute == UIRoute.Match)
            {
                BeginCommandSequence(ref shellState, commands, UiShellCommandKind.ExitLoading, UiShellRegionId.LoadingLayer, UIRoute.Match, UiShellMode.MatchHud);
                AppendCommand(commands, shellState, UiShellCommandKind.EnterMatchHud, UiShellRegionId.None, UIRoute.Match, UiShellMode.MatchHud);
                shellState.CurrentMode = UiShellMode.MatchHud;
                shellState.Phase = UiShellTransitionPhase.EnteringMatchHud;
                SetMatchIntro(
                    ref matchIntro,
                    MatchIntroTransitionStateKind.EnteringHud,
                    0.66f,
                    inputLocked: true,
                    shellState.TransitionSequenceId,
                    "Entering HUD");
            }
            else
            {
                BeginCommandSequence(ref shellState, commands, UiShellCommandKind.ExitLoading, UiShellRegionId.LoadingLayer, UIRoute.MainMenu, UiShellMode.MainMenu);
                AppendCommand(commands, shellState, UiShellCommandKind.EnterMenu, UiShellRegionId.None, UIRoute.MainMenu, UiShellMode.MainMenu);
                shellState.CurrentMode = UiShellMode.MainMenu;
                shellState.ActiveRoute = UIRoute.MainMenu;
                shellState.Phase = UiShellTransitionPhase.EnteringMenu;
                SetMatchIntroInactive(ref matchIntro);
            }
            state.EntityManager.SetComponentData(boundary, shellState);
            state.EntityManager.SetComponentData(boundary, matchIntro);
            return;
        }

        state.EntityManager.SetComponentData(boundary, shellState);
        state.EntityManager.SetComponentData(boundary, matchIntro);
    }

    private static void ProcessRouteRequest(
        ref UiShellStateComponent shellState,
        ref MatchIntroTransitionComponent matchIntro,
        DynamicBuffer<UiShellPresentationCommandComponent> commands,
        DynamicBuffer<UiShellRouteHistoryComponent> routeHistory,
        UiShellRouteRequestComponent request)
    {
        switch (request.Intent)
        {
            case UiShellRouteIntent.EnterMatch:
                routeHistory.Clear();
                BeginCommandSequence(ref shellState, commands, UiShellCommandKind.ShowLoading, UiShellRegionId.LoadingLayer, UIRoute.Match, UiShellMode.Loading);
                AppendCommand(commands, shellState, UiShellCommandKind.ExitMenu, UiShellRegionId.None, UIRoute.Match, UiShellMode.Loading);
                shellState.CurrentMode = UiShellMode.Loading;
                shellState.ActiveRoute = UIRoute.Match;
                shellState.Phase = UiShellTransitionPhase.ShowingLoading;
                SetMatchIntro(
                    ref matchIntro,
                    MatchIntroTransitionStateKind.WaitingForWorldReady,
                    0f,
                    inputLocked: true,
                    shellState.TransitionSequenceId,
                    "Waiting for world");
                break;
            case UiShellRouteIntent.ReturnToMainMenu:
                routeHistory.Clear();
                BeginCommandSequence(ref shellState, commands, UiShellCommandKind.ShowLoading, UiShellRegionId.LoadingLayer, UIRoute.MainMenu, UiShellMode.Loading);
                if (shellState.CurrentMode == UiShellMode.MatchHud)
                    AppendCommand(commands, shellState, UiShellCommandKind.ExitMatchHud, UiShellRegionId.None, UIRoute.MainMenu, UiShellMode.Loading);
                shellState.CurrentMode = UiShellMode.Loading;
                shellState.ActiveRoute = UIRoute.MainMenu;
                shellState.Phase = UiShellTransitionPhase.ShowingLoading;
                SetMatchIntroInactive(ref matchIntro);
                break;
            case UiShellRouteIntent.BackMenuRoute:
                SetMatchIntroInactive(ref matchIntro);
                ProcessMenuRouteRequest(ref shellState, commands, PopRouteHistory(routeHistory, request.Route));
                break;
            default:
                if (request.PushHistory != 0)
                    PushRouteHistory(routeHistory, shellState.ActiveRoute, request.Route);
                SetMatchIntroInactive(ref matchIntro);
                ProcessMenuRouteRequest(ref shellState, commands, request.Route);
                break;
        }
    }

    private static void ProcessMenuRouteRequest(
        ref UiShellStateComponent shellState,
        DynamicBuffer<UiShellPresentationCommandComponent> commands,
        UIRoute route)
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

    private static void PushRouteHistory(
        DynamicBuffer<UiShellRouteHistoryComponent> routeHistory,
        UIRoute currentRoute,
        UIRoute targetRoute)
    {
        if (currentRoute == targetRoute ||
            currentRoute == UIRoute.Splash ||
            currentRoute == UIRoute.Match)
        {
            return;
        }

        if (routeHistory.Length > 0 &&
            routeHistory[routeHistory.Length - 1].Route == currentRoute)
        {
            return;
        }

        routeHistory.Add(new UiShellRouteHistoryComponent
        {
            Route = currentRoute
        });
    }

    private static UIRoute PopRouteHistory(
        DynamicBuffer<UiShellRouteHistoryComponent> routeHistory,
        UIRoute fallbackRoute)
    {
        if (routeHistory.Length == 0)
            return fallbackRoute;

        int index = routeHistory.Length - 1;
        UIRoute route = routeHistory[index].Route;
        routeHistory.RemoveAt(index);
        return route == UIRoute.Splash || route == UIRoute.Match
            ? fallbackRoute
            : route;
    }

    private static void ProcessPopupRequest(
        ref UiShellStateComponent shellState,
        ref UiShellActivePopupComponent activePopup,
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
        if (request.Intent == UiShellPopupIntent.Hide)
        {
            if (activePopup.PopupKind == request.PopupKind)
                activePopup.Visible = 0;
            return;
        }

        activePopup.PopupKind = request.PopupKind;
        activePopup.Visible = 1;
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

    private static bool TryConsumeLoadingProgressRequest(
        DynamicBuffer<UiShellLoadingProgressRequestComponent> loadingRequests,
        out UiShellLoadingProgressRequestComponent request)
    {
        if (loadingRequests.Length == 0)
        {
            request = default;
            return false;
        }

        request = loadingRequests[loadingRequests.Length - 1];
        loadingRequests.Clear();
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
        UIRoute route,
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
        UIRoute route,
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
        ref MatchIntroTransitionComponent matchIntro,
        DynamicBuffer<UiShellTransitionCompleteComponent> completions)
    {
        for (int i = 0; i < completions.Length; i++)
        {
            UiShellTransitionCompleteComponent completion = completions[i];
            if (completion.SequenceId != shellState.TransitionSequenceId)
                continue;

            shellState.IsTransitionRunning = 0;
            shellState.Phase = CompletionPhase(completion.Kind);
            if (completion.Kind == UiShellCommandKind.EnterMatchHud &&
                matchIntro.State != MatchIntroTransitionStateKind.Inactive)
            {
                SetMatchIntro(
                    ref matchIntro,
                    MatchIntroTransitionStateKind.Complete,
                    1f,
                    inputLocked: false,
                    completion.SequenceId,
                    "Intro complete");
            }
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

    private static void ResetLoading(ref UiShellLoadingProgressComponent loading, string status)
    {
        loading.Progress01 = 0f;
        loading.Status = new Unity.Collections.FixedString64Bytes(status);
        loading.IsComplete = 0;
    }

    private static void SetMatchIntroInactive(ref MatchIntroTransitionComponent matchIntro)
    {
        SetMatchIntro(
            ref matchIntro,
            MatchIntroTransitionStateKind.Inactive,
            0f,
            inputLocked: false,
            0,
            "Inactive");
    }

    private static void SetMatchIntro(
        ref MatchIntroTransitionComponent matchIntro,
        MatchIntroTransitionStateKind state,
        float progress01,
        bool inputLocked,
        int sequenceId,
        string status)
    {
        matchIntro.State = state;
        matchIntro.Progress01 = UnityEngine.Mathf.Clamp01(progress01);
        matchIntro.InputLocked = inputLocked ? (byte)1 : (byte)0;
        matchIntro.SequenceId = sequenceId;
        matchIntro.Status = new Unity.Collections.FixedString64Bytes(status);
    }
}

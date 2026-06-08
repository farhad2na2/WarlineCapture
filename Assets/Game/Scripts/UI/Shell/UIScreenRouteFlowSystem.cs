using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class UIScreenRouteFlowSystem
{
    private readonly Dictionary<UIRoute, UIScreenView> screenByRoute = new();
    private readonly Stack<UIRoute> backStack = new();

    public UIRoute ActiveRoute { get; private set; }
    public bool HasActiveRoute { get; private set; }
    public bool IsInitialized { get; private set; }

    public void Reset()
    {
        screenByRoute.Clear();
        backStack.Clear();
        ActiveRoute = default;
        HasActiveRoute = false;
        IsInitialized = false;
    }

    public void Initialize(
        Transform contentRoot,
        IReadOnlyList<UIScreenView> screens,
        IReadOnlyList<UIScreenView> screenPrefabs,
        UIRoute initialRoute,
        bool routeToInitial)
    {
        if (IsInitialized)
            return;

        screenByRoute.Clear();
        RegisterScreens(screens);
        InstantiateScreenPrefabs(contentRoot, screenPrefabs);
        HideAllScreens();
        IsInitialized = true;

        if (routeToInitial && IsRegistered(initialRoute))
            GoTo(initialRoute, false);
    }

    public void Register(UIScreenView screen)
    {
        if (screen == null)
            return;

        screenByRoute[screen.Route] = screen;
    }

    public void GoTo(UIRoute route, bool pushCurrentRoute)
    {
        if (!screenByRoute.TryGetValue(route, out UIScreenView nextScreen))
            throw new InvalidOperationException($"No UI screen registered for route '{route}'.");

        if (HasActiveRoute && ActiveRoute.Equals(route))
            return;

        if (pushCurrentRoute && HasActiveRoute)
            backStack.Push(ActiveRoute);

        if (HasActiveRoute && screenByRoute.TryGetValue(ActiveRoute, out UIScreenView currentScreen))
            currentScreen.Hide();

        nextScreen.Show();
        ActiveRoute = route;
        HasActiveRoute = true;
    }

    public bool Back()
    {
        if (backStack.Count == 0)
            return false;

        UIRoute previous = backStack.Pop();
        GoTo(previous, false);
        return true;
    }

    public bool IsRegistered(UIRoute route)
    {
        return screenByRoute.ContainsKey(route);
    }

    public bool TryGetRegisteredScreen(UIRoute route, out UIScreenView screen)
    {
        return screenByRoute.TryGetValue(route, out screen);
    }

    private void RegisterScreens(IReadOnlyList<UIScreenView> screens)
    {
        if (screens == null)
            return;

        for (int i = 0; i < screens.Count; i++)
            Register(screens[i]);
    }

    private void InstantiateScreenPrefabs(Transform contentRoot, IReadOnlyList<UIScreenView> screenPrefabs)
    {
        if (contentRoot == null || screenPrefabs == null)
            return;

        for (int i = 0; i < screenPrefabs.Count; i++)
        {
            UIScreenView screenPrefab = screenPrefabs[i];
            if (screenPrefab == null || IsRegistered(screenPrefab.Route))
                continue;

            UIScreenView screen = UnityEngine.Object.Instantiate(screenPrefab, contentRoot, false);
            screen.name = screenPrefab.name;
            Register(screen);
        }
    }

    private void HideAllScreens()
    {
        foreach (UIScreenView screen in screenByRoute.Values)
            screen.Hide();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UIRouter : MonoBehaviour
{
    [SerializeField] private UIRoute initialRoute = UIRoute.MainMenu;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private UIScreenSystem[] screens = Array.Empty<UIScreenSystem>();
    [SerializeField] private UIScreenSystem[] screenPrefabs = Array.Empty<UIScreenSystem>();

    private readonly Dictionary<UIRoute, UIScreenSystem> _screenByRoute = new();
    private readonly Stack<UIRoute> _backStack = new();
    private bool _initialized;

    public UIRoute ActiveRoute { get; private set; }
    public bool HasActiveRoute { get; private set; }
    public Transform ContentRoot => contentRoot;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_initialized)
            return;

        RegisterConfiguredScreens();
        HideAllScreens();
        _initialized = true;

        if (_screenByRoute.ContainsKey(initialRoute))
            GoTo(initialRoute, false);
    }

    public void ConfigureForTests(UIScreenSystem[] configuredScreens, UIRoute route)
    {
        screens = configuredScreens ?? Array.Empty<UIScreenSystem>();
        screenPrefabs = Array.Empty<UIScreenSystem>();
        initialRoute = route;
        _initialized = false;
        _screenByRoute.Clear();
        _backStack.Clear();
        HasActiveRoute = false;
        Initialize();
    }

    public void Register(UIScreenSystem screen)
    {
        if (screen == null)
            return;

        _screenByRoute[screen.Route] = screen;
    }

    public void GoTo(UIRoute route)
    {
        GoTo(route, true);
    }

    public void GoTo(UIRoute route, bool pushCurrentRoute)
    {
        InitializeIfNeededWithoutRouting();

        if (!_screenByRoute.TryGetValue(route, out UIScreenSystem nextScreen))
            throw new InvalidOperationException($"No WarlineCapture screen registered for route '{route}'.");

        if (HasActiveRoute && ActiveRoute.Equals(route))
            return;

        if (pushCurrentRoute && HasActiveRoute)
            _backStack.Push(ActiveRoute);

        if (HasActiveRoute && _screenByRoute.TryGetValue(ActiveRoute, out UIScreenSystem currentScreen))
            currentScreen.Hide();

        nextScreen.Show();
        ActiveRoute = route;
        HasActiveRoute = true;
    }

    public bool TryGoTo(UIRoute route)
    {
        InitializeIfNeededWithoutRouting();
        if (!_screenByRoute.ContainsKey(route))
            return false;

        GoTo(route);
        return true;
    }

    public bool TryGetRegisteredScreen(UIRoute route, out UIScreenSystem screen)
    {
        InitializeIfNeededWithoutRouting();
        return _screenByRoute.TryGetValue(route, out screen);
    }

    public bool Back()
    {
        InitializeIfNeededWithoutRouting();
        if (_backStack.Count == 0)
            return false;

        UIRoute previous = _backStack.Pop();
        GoTo(previous, false);
        return true;
    }

    private void RegisterConfiguredScreens()
    {
        _screenByRoute.Clear();

        if (contentRoot == null)
            contentRoot = transform;

        foreach (UIScreenSystem screen in screens)
            Register(screen);

        foreach (UIScreenSystem screen in contentRoot.GetComponentsInChildren<UIScreenSystem>(true))
            Register(screen);

        InstantiateScreenPrefabs();
    }

    private void InstantiateScreenPrefabs()
    {
        foreach (UIScreenSystem screenPrefab in screenPrefabs)
        {
            if (screenPrefab == null || _screenByRoute.ContainsKey(screenPrefab.Route))
                continue;

            UIScreenSystem screen = Instantiate(screenPrefab, contentRoot, false);
            screen.name = screenPrefab.name;
            Register(screen);
        }
    }

    private void HideAllScreens()
    {
        foreach (UIScreenSystem screen in _screenByRoute.Values)
            screen.Hide();
    }

    private void InitializeIfNeededWithoutRouting()
    {
        if (_initialized)
            return;

        RegisterConfiguredScreens();
        HideAllScreens();
        _initialized = true;
    }
}

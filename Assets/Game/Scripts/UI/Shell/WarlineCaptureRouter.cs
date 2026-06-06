using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WarlineCaptureRouter : MonoBehaviour
{
    [SerializeField] private WarlineCaptureRoute initialRoute = WarlineCaptureRoute.MainMenu;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private WarlineCaptureScreenSystem[] screens = Array.Empty<WarlineCaptureScreenSystem>();
    [SerializeField] private WarlineCaptureScreenSystem[] screenPrefabs = Array.Empty<WarlineCaptureScreenSystem>();

    private readonly Dictionary<WarlineCaptureRoute, WarlineCaptureScreenSystem> _screenByRoute = new();
    private readonly Stack<WarlineCaptureRoute> _backStack = new();
    private bool _initialized;

    public WarlineCaptureRoute ActiveRoute { get; private set; }
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

    public void ConfigureForTests(WarlineCaptureScreenSystem[] configuredScreens, WarlineCaptureRoute route)
    {
        screens = configuredScreens ?? Array.Empty<WarlineCaptureScreenSystem>();
        screenPrefabs = Array.Empty<WarlineCaptureScreenSystem>();
        initialRoute = route;
        _initialized = false;
        _screenByRoute.Clear();
        _backStack.Clear();
        HasActiveRoute = false;
        Initialize();
    }

    public void Register(WarlineCaptureScreenSystem screen)
    {
        if (screen == null)
            return;

        _screenByRoute[screen.Route] = screen;
    }

    public void GoTo(WarlineCaptureRoute route)
    {
        GoTo(route, true);
    }

    public void GoTo(WarlineCaptureRoute route, bool pushCurrentRoute)
    {
        InitializeIfNeededWithoutRouting();

        if (!_screenByRoute.TryGetValue(route, out WarlineCaptureScreenSystem nextScreen))
            throw new InvalidOperationException($"No WarlineCapture screen registered for route '{route}'.");

        if (HasActiveRoute && ActiveRoute.Equals(route))
            return;

        if (pushCurrentRoute && HasActiveRoute)
            _backStack.Push(ActiveRoute);

        if (HasActiveRoute && _screenByRoute.TryGetValue(ActiveRoute, out WarlineCaptureScreenSystem currentScreen))
            currentScreen.Hide();

        nextScreen.Show();
        ActiveRoute = route;
        HasActiveRoute = true;
    }

    public bool TryGoTo(WarlineCaptureRoute route)
    {
        InitializeIfNeededWithoutRouting();
        if (!_screenByRoute.ContainsKey(route))
            return false;

        GoTo(route);
        return true;
    }

    public bool TryGetRegisteredScreen(WarlineCaptureRoute route, out WarlineCaptureScreenSystem screen)
    {
        InitializeIfNeededWithoutRouting();
        return _screenByRoute.TryGetValue(route, out screen);
    }

    public bool Back()
    {
        InitializeIfNeededWithoutRouting();
        if (_backStack.Count == 0)
            return false;

        WarlineCaptureRoute previous = _backStack.Pop();
        GoTo(previous, false);
        return true;
    }

    private void RegisterConfiguredScreens()
    {
        _screenByRoute.Clear();

        if (contentRoot == null)
            contentRoot = transform;

        foreach (WarlineCaptureScreenSystem screen in screens)
            Register(screen);

        foreach (WarlineCaptureScreenSystem screen in contentRoot.GetComponentsInChildren<WarlineCaptureScreenSystem>(true))
            Register(screen);

        InstantiateScreenPrefabs();
    }

    private void InstantiateScreenPrefabs()
    {
        foreach (WarlineCaptureScreenSystem screenPrefab in screenPrefabs)
        {
            if (screenPrefab == null || _screenByRoute.ContainsKey(screenPrefab.Route))
                continue;

            WarlineCaptureScreenSystem screen = Instantiate(screenPrefab, contentRoot, false);
            screen.name = screenPrefab.name;
            Register(screen);
        }
    }

    private void HideAllScreens()
    {
        foreach (WarlineCaptureScreenSystem screen in _screenByRoute.Values)
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

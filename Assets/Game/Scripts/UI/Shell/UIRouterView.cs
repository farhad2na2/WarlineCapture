using System;
using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed class UIRouterView : MonoBehaviour
    {
        private readonly UIScreenRouteFlowUiSystemHelper routeFlowSystem = new();

        [SerializeField] private UIRoute initialRoute = UIRoute.MainMenu;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private UIScreenView[] screens = Array.Empty<UIScreenView>();
        [SerializeField] private UIScreenView[] screenPrefabs = Array.Empty<UIScreenView>();

        public UIRoute ActiveRoute => routeFlowSystem.ActiveRoute;
        public bool HasActiveRoute => routeFlowSystem.HasActiveRoute;
        public Transform ContentRoot => contentRoot;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            EnsureContentRoot();
            routeFlowSystem.Initialize(contentRoot, screens, screenPrefabs, initialRoute, true);
        }

        public void ConfigureForTests(UIScreenView[] configuredScreens, UIRoute route)
        {
            screens = configuredScreens ?? Array.Empty<UIScreenView>();
            screenPrefabs = Array.Empty<UIScreenView>();
            initialRoute = route;
            routeFlowSystem.Reset();
            Initialize();
        }

        public void Register(UIScreenView screen)
        {
            routeFlowSystem.Register(screen);
        }

        public void GoTo(UIRoute route)
        {
            GoTo(route, true);
        }

        public void GoTo(UIRoute route, bool pushCurrentRoute)
        {
            InitializeIfNeededWithoutRouting();
            routeFlowSystem.GoTo(route, pushCurrentRoute);
        }

        public bool TryGoTo(UIRoute route)
        {
            InitializeIfNeededWithoutRouting();
            if (!routeFlowSystem.IsRegistered(route))
                return false;

            GoTo(route);
            return true;
        }

        public bool TryGetRegisteredScreen(UIRoute route, out UIScreenView screen)
        {
            InitializeIfNeededWithoutRouting();
            return routeFlowSystem.TryGetRegisteredScreen(route, out screen);
        }

        public bool Back()
        {
            InitializeIfNeededWithoutRouting();
            return routeFlowSystem.Back();
        }

        private void InitializeIfNeededWithoutRouting()
        {
            if (routeFlowSystem.IsInitialized)
                return;

            EnsureContentRoot();
            routeFlowSystem.Initialize(contentRoot, screens, screenPrefabs, initialRoute, false);
        }

        private void EnsureContentRoot()
        {
            if (contentRoot == null)
                contentRoot = transform;
        }
    }
}

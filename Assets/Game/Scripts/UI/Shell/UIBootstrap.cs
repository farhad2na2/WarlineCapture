using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed class UIBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject appCanvasPrefab;
        [SerializeField] private UIStartupMode startupMode = UIStartupMode.UseLegacyMenu;
        [SerializeField] private bool enableParallelUiOnStart;
        [SerializeField] private UIRoute parallelStartupRoute = UIRoute.MainMenu;

        public GameObject AppCanvasInstance { get; private set; }
        public UIStartupMode StartupMode => startupMode;
        public UIRoute ParallelStartupRoute => parallelStartupRoute;

        private void Awake()
        {
            if (appCanvasPrefab == null)
                return;

            bool shouldEnableParallelUi = ShouldEnableParallelUi();
            AppCanvasInstance = Instantiate(appCanvasPrefab);
            AppCanvasInstance.name = appCanvasPrefab.name;
            AppCanvasInstance.SetActive(shouldEnableParallelUi);

            if (shouldEnableParallelUi && AppCanvasInstance.TryGetComponent(out UIRouterView router))
                router.GoTo(parallelStartupRoute, false);
        }

        public void SetParallelUiEnabled(bool enabled)
        {
            startupMode = enabled ? UIStartupMode.UseParallelCodexUi : UIStartupMode.UseLegacyMenu;
            enableParallelUiOnStart = enabled;
            if (AppCanvasInstance != null)
            {
                AppCanvasInstance.SetActive(enabled);
                if (enabled && AppCanvasInstance.TryGetComponent(out UIRouterView router))
                    router.GoTo(parallelStartupRoute, false);
            }
        }

        private bool ShouldEnableParallelUi()
        {
            return startupMode == UIStartupMode.UseParallelCodexUi || enableParallelUiOnStart;
        }
    }
}

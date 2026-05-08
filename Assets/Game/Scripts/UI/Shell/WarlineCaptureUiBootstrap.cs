using UnityEngine;

public sealed class WarlineCaptureUiBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject appCanvasPrefab;
    [SerializeField] private WarlineCaptureUiStartupMode startupMode = WarlineCaptureUiStartupMode.UseLegacyMenu;
    [SerializeField] private bool enableParallelUiOnStart;
    [SerializeField] private WarlineCaptureRoute parallelStartupRoute = WarlineCaptureRoute.Splash;

    public GameObject AppCanvasInstance { get; private set; }
    public WarlineCaptureUiStartupMode StartupMode => startupMode;
    public WarlineCaptureRoute ParallelStartupRoute => parallelStartupRoute;

    private void Awake()
    {
        if (appCanvasPrefab == null)
            return;

        bool shouldEnableParallelUi = ShouldEnableParallelUi();
        AppCanvasInstance = Instantiate(appCanvasPrefab);
        AppCanvasInstance.name = appCanvasPrefab.name;
        AppCanvasInstance.SetActive(shouldEnableParallelUi);

        if (shouldEnableParallelUi && AppCanvasInstance.TryGetComponent(out WarlineCaptureRouter router))
            router.GoTo(parallelStartupRoute, false);
    }

    public void SetParallelUiEnabled(bool enabled)
    {
        startupMode = enabled ? WarlineCaptureUiStartupMode.UseParallelCodexUi : WarlineCaptureUiStartupMode.UseLegacyMenu;
        enableParallelUiOnStart = enabled;
        if (AppCanvasInstance != null)
        {
            AppCanvasInstance.SetActive(enabled);
            if (enabled && AppCanvasInstance.TryGetComponent(out WarlineCaptureRouter router))
                router.GoTo(parallelStartupRoute, false);
        }
    }

    private bool ShouldEnableParallelUi()
    {
        return startupMode == WarlineCaptureUiStartupMode.UseParallelCodexUi || enableParallelUiOnStart;
    }
}

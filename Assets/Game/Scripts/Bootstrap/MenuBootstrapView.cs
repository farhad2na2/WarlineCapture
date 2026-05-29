using UnityEngine;

[DisallowMultipleComponent]
public sealed class MenuBootstrapView : MonoBehaviour
{
    private readonly MenuBootstrapSystem menuBootstrapSystem = new();

    [SerializeField] private Camera uiCamera;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private WarlineCaptureShellView shellView;
    [SerializeField] private WarlineCaptureShellEcsBridgeView shellEcsBridge;
    [SerializeField] private WarlineCaptureShellContentPresenterView contentPresenter;
    [SerializeField] private WarlineCaptureRouter router;
    [SerializeField] private float startupLoadingDurationSeconds = 2f;

    public Camera UiCamera => uiCamera;
    public Canvas UiCanvas => uiCanvas;
    public WarlineCaptureShellView ShellView => shellView;
    public WarlineCaptureShellEcsBridgeView ShellEcsBridge => shellEcsBridge;
    public WarlineCaptureShellContentPresenterView ContentPresenter => contentPresenter;
    public WarlineCaptureRouter Router => router;
    public float StartupLoadingDurationSeconds => startupLoadingDurationSeconds;
    public PerformanceDiagnosticsSystem PerformanceDiagnostics => menuBootstrapSystem.PerformanceDiagnostics;

    public void Configure(
        Camera configuredUiCamera,
        Canvas configuredUiCanvas,
        WarlineCaptureShellView configuredShellView,
        WarlineCaptureShellEcsBridgeView configuredShellEcsBridge,
        WarlineCaptureShellContentPresenterView configuredContentPresenter,
        WarlineCaptureRouter configuredRouter,
        float configuredStartupLoadingDurationSeconds)
    {
        uiCamera = configuredUiCamera;
        uiCanvas = configuredUiCanvas;
        shellView = configuredShellView;
        shellEcsBridge = configuredShellEcsBridge;
        contentPresenter = configuredContentPresenter;
        router = configuredRouter;
        startupLoadingDurationSeconds = Mathf.Max(0.01f, configuredStartupLoadingDurationSeconds);
    }

    private void Awake()
    {
        menuBootstrapSystem.Initialize(this);
    }

    private void OnEnable()
    {
        menuBootstrapSystem.Initialize(this);
    }

    private void Update()
    {
        menuBootstrapSystem.Update(this, Time.unscaledDeltaTime);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        menuBootstrapSystem.OnApplicationFocus(hasFocus);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        menuBootstrapSystem.OnApplicationPause(pauseStatus);
    }

    private void OnDisable()
    {
        menuBootstrapSystem.Shutdown(this);
    }
}

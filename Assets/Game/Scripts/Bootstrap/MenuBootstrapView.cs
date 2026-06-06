using UnityEngine;

[DisallowMultipleComponent]
public sealed class MenuBootstrapView : MonoBehaviour
{
    private readonly MenuBootstrapSystem menuBootstrapSystem = new();

    [SerializeField] private Camera uiCamera;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private WarlineCaptureShellView shellView;
    [SerializeField] private WarlineCaptureShellEcsPresentationSystem shellEcsPresentation;
    [SerializeField] private WarlineCaptureShellContentSystem contentSystem;
    [SerializeField] private WarlineCaptureRouter router;

    public Camera UiCamera => uiCamera;
    public Canvas UiCanvas => uiCanvas;
    public WarlineCaptureShellView ShellView => shellView;
    public WarlineCaptureShellEcsPresentationSystem ShellEcsPresentation => shellEcsPresentation;
    public WarlineCaptureShellContentSystem ContentSystem => contentSystem;
    public WarlineCaptureRouter Router => router;
    public PerformanceDiagnosticsSystem PerformanceDiagnostics => menuBootstrapSystem.PerformanceDiagnostics;

    public void Configure(
        Camera configuredUiCamera,
        Canvas configuredUiCanvas,
        WarlineCaptureShellView configuredShellView,
        WarlineCaptureShellEcsPresentationSystem configuredShellEcsPresentation,
        WarlineCaptureShellContentSystem configuredContentSystem,
        WarlineCaptureRouter configuredRouter)
    {
        uiCamera = configuredUiCamera;
        uiCanvas = configuredUiCanvas;
        shellView = configuredShellView;
        shellEcsPresentation = configuredShellEcsPresentation;
        contentSystem = configuredContentSystem;
        router = configuredRouter;
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

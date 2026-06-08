using UnityEngine;

[DisallowMultipleComponent]
public sealed class MenuBootstrapView : MonoBehaviour
{
    private readonly MenuBootstrapSystem menuBootstrapSystem = new();

    [SerializeField] private Camera uiCamera;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private UIShellView shellView;
    [SerializeField] private UIShellEcsPresentationSystem shellEcsPresentation;
    [SerializeField] private UIShellContentView contentSystem;
    [SerializeField] private UIRouterView router;

    public Camera UiCamera => uiCamera;
    public Canvas UiCanvas => uiCanvas;
    public UIShellView ShellView => shellView;
    public UIShellEcsPresentationSystem ShellEcsPresentation => shellEcsPresentation;
    public UIShellContentView ContentSystem => contentSystem;
    public UIRouterView Router => router;
    public PerformanceDiagnosticsSystem PerformanceDiagnostics => menuBootstrapSystem.PerformanceDiagnostics;

    public void Configure(
        Camera configuredUiCamera,
        Canvas configuredUiCanvas,
        UIShellView configuredShellView,
        UIShellEcsPresentationSystem configuredShellEcsPresentation,
        UIShellContentView configuredContentSystem,
        UIRouterView configuredRouter)
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

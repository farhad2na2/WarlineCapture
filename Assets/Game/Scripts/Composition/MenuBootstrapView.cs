using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class MenuBootstrapView : MonoBehaviour
{
    private readonly MenuBootstrapSystem menuBootstrapSystem = new();

    [SerializeField] private Camera uiCamera;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private UIShellView shellView;
    [FormerlySerializedAs("shellEcsBridge")]
    [SerializeField] private UIShellEcsPresentationSystem shellEcsPresentation;
    [FormerlySerializedAs("contentPresenter")]
    [SerializeField] private UIShellContentView contentSystem;
    [SerializeField] private UIRouterView router;

    public Camera UiCamera => uiCamera;
    public Canvas UiCanvas => uiCanvas;
    public UIShellView ShellView => shellView;
    public UIShellEcsPresentationSystem ShellEcsPresentation => shellEcsPresentation;
    public UIShellContentView ContentSystem => contentSystem;
    public UIRouterView Router => router;
    public PerformanceDiagnosticsSystem PerformanceDiagnostics => menuBootstrapSystem.PerformanceDiagnostics;

#if UNITY_EDITOR
    private static long editorAllocationBytes;
    private static int editorAllocationSamples;
    private static int editorUpdateSamples;

    public static void ResetEditorAllocationProbe()
    {
        editorAllocationBytes = 0;
        editorAllocationSamples = 0;
        editorUpdateSamples = 0;
    }

    public static void GetEditorAllocationProbe(out long bytes, out int allocationSamples, out int updateSamples)
    {
        bytes = editorAllocationBytes;
        allocationSamples = editorAllocationSamples;
        updateSamples = editorUpdateSamples;
    }
#endif

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
#if UNITY_EDITOR
        long allocationStart = System.GC.GetAllocatedBytesForCurrentThread();
        try
        {
#endif
        menuBootstrapSystem.Update(this, Time.unscaledDeltaTime);
#if UNITY_EDITOR
        }
        finally
        {
            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - allocationStart;
            editorUpdateSamples++;
            if (allocated > 0)
            {
                editorAllocationBytes += allocated;
                editorAllocationSamples++;
            }
        }
#endif
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

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Unity.Entities;

[DisallowMultipleComponent]
public sealed class MenuBootstrapView : MonoBehaviour
{
    private readonly MenuBootstrapSystem menuBootstrapSystem = new();

    [SerializeField] private RuntimeUiConfig runtimeUiConfig;
    [SerializeField] private Camera uiCamera;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private UIDocument uiToolkitDocument;
    [SerializeField] private GameObject uiToolkitShellRoot;
    [SerializeField] private UiToolkitShellView uiToolkitShellView;
    [SerializeField] private UIShellView shellView;
    [FormerlySerializedAs("shellEcsBridge")]
    [SerializeField] private UIShellEcsPresentationSystem shellEcsPresentation;
    [FormerlySerializedAs("contentPresenter")]
    [SerializeField] private UIShellContentView contentSystem;
    [SerializeField] private UIRouterView router;

    public RuntimeUiConfig RuntimeUiConfig => runtimeUiConfig;
    public RuntimeUiMode UiMode => runtimeUiConfig != null ? runtimeUiConfig.Mode : RuntimeUiMode.Canvas;
    public bool IsUiToolkitMode => UiMode == RuntimeUiMode.UiToolkit;
    public Camera UiCamera => uiCamera;
    public Canvas UiCanvas => uiCanvas;
    public UIDocument UiToolkitDocument => uiToolkitDocument;
    public GameObject UiToolkitShellRoot => uiToolkitShellRoot;
    public UiToolkitShellView UiToolkitShellView => uiToolkitShellView;
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
        UIRouterView configuredRouter,
        RuntimeUiConfig configuredRuntimeUiConfig = null,
        UIDocument configuredUiToolkitDocument = null,
        GameObject configuredUiToolkitShellRoot = null,
        UiToolkitShellView configuredUiToolkitShellView = null)
    {
        if (configuredRuntimeUiConfig != null)
            runtimeUiConfig = configuredRuntimeUiConfig;
        uiCamera = configuredUiCamera;
        uiCanvas = configuredUiCanvas;
        if (configuredUiToolkitDocument != null)
            uiToolkitDocument = configuredUiToolkitDocument;
        if (configuredUiToolkitShellRoot != null)
            uiToolkitShellRoot = configuredUiToolkitShellRoot;
        if (configuredUiToolkitShellView != null)
            uiToolkitShellView = configuredUiToolkitShellView;
        shellView = configuredShellView;
        shellEcsPresentation = configuredShellEcsPresentation;
        contentSystem = configuredContentSystem;
        router = configuredRouter;
    }

    public void ApplyRuntimeUiMode()
    {
        bool useUiToolkit = IsUiToolkitMode;

        if (uiCanvas != null)
        {
            if (!useUiToolkit)
            {
                if (!uiCanvas.gameObject.activeSelf)
                    uiCanvas.gameObject.SetActive(true);
                if (uiCanvas.transform.localScale != Vector3.one)
                    uiCanvas.transform.localScale = Vector3.one;
            }

            if (uiCanvas.enabled == useUiToolkit)
                uiCanvas.enabled = !useUiToolkit;
        }
        if (shellEcsPresentation != null && shellEcsPresentation.enabled == useUiToolkit)
            shellEcsPresentation.enabled = !useUiToolkit;
        if (contentSystem != null && contentSystem.enabled == useUiToolkit)
            contentSystem.enabled = !useUiToolkit;
        if (router != null && router.enabled == useUiToolkit)
            router.enabled = !useUiToolkit;

        if (uiToolkitDocument != null && uiToolkitDocument.enabled != useUiToolkit)
            uiToolkitDocument.enabled = useUiToolkit;
        if (uiToolkitShellRoot != null && uiToolkitShellRoot.activeSelf != useUiToolkit)
            uiToolkitShellRoot.SetActive(useUiToolkit);
        if (useUiToolkit && uiToolkitShellView != null && !uiToolkitShellView.IsMounted)
        {
            if (uiToolkitShellView.Mount())
                uiToolkitShellView.EnsureMainMenuVisible(UIRoute.MainMenu);
        }
        if (!useUiToolkit && uiToolkitShellView != null)
            uiToolkitShellView.ClearCache();

        ConfigureUiToolkitApplySystem(useUiToolkit);
    }

    private void ConfigureUiToolkitApplySystem(bool useUiToolkit)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        if (useUiToolkit && uiToolkitShellView != null)
        {
            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            if (!uiToolkitShellView.IsMounted)
                uiToolkitShellView.Mount();
            applySystem.ConfigureShellView(uiToolkitShellView);
            return;
        }

        UiToolkitShellApplySystem existingSystem = world.GetExistingSystemManaged<UiToolkitShellApplySystem>();
        existingSystem?.ClearShellView(uiToolkitShellView);
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

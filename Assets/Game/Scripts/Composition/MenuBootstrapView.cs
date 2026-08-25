using UnityEngine;
using UnityEngine.Serialization;
using Game.Configs;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.Composition
{
    [DisallowMultipleComponent]
    public sealed class MenuBootstrapView : MonoBehaviour
    {
        private readonly MenuBootstrapCompositionSystemHelper menuBootstrapSystem = new();
        private readonly CampaignMissionMenuBootstrapRuntime campaignMissionBootstrap = new();

        [SerializeField] private RuntimeUiConfig runtimeUiConfig;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private UIShellView shellView;
        [FormerlySerializedAs("shellEcsBridge")]
        [SerializeField] private UIShellEcsPresentationSystem shellEcsPresentation;
        [FormerlySerializedAs("contentPresenter")]
        [SerializeField] private UIShellContentView contentSystem;
        [SerializeField] private UIRouterView router;
        [Header("First Launch Narrative")]
        [SerializeField] private NarrativeSequenceView firstLaunchNarrativeView;
        [SerializeField] private NarrativeSequenceConfig firstLaunchNarrativeConfig;
        [SerializeField] private NarrativeSpeakerCatalog firstLaunchSpeakerCatalog;
        [SerializeField] private NarrativePunctuationConfig firstLaunchPunctuationProfile;
        [SerializeField] private FirstLaunchLanguageChoiceView firstLaunchLanguageChoiceView;
        [SerializeField] private NarrativeLocaleConfig firstLaunchPersianLocale;
        [Header("Campaign Narrative")]
        [SerializeField] private NarrativeSequenceConfig[] campaignMissionNarrativeConfigs =
            System.Array.Empty<NarrativeSequenceConfig>();
        [Header("Campaign Mission Runtime")]
        [SerializeField] private MissionDefinitionCatalogConfig campaignMissionCatalog;
        [SerializeField] private MissionDefinitionConfig campaignMissionDefinition;
        [SerializeField] private ScenarioSetupConfig campaignScenarioSetup;
        [SerializeField] private OperationMapCatalogConfig campaignOperationMapCatalog;

        public RuntimeUiConfig RuntimeUiConfig => runtimeUiConfig;
        public RuntimeUiMode UiMode => runtimeUiConfig != null ? runtimeUiConfig.Mode : RuntimeUiMode.Canvas;
        public Camera UiCamera => uiCamera;
        public Canvas UiCanvas => uiCanvas;
        public UIShellView ShellView => shellView;
        public UIShellEcsPresentationSystem ShellEcsPresentation => shellEcsPresentation;
        public UIShellContentView ContentSystem => contentSystem;
        public UIRouterView Router => router;
        public NarrativeSequenceView FirstLaunchNarrativeView => firstLaunchNarrativeView;
        public NarrativeSequenceConfig FirstLaunchNarrativeConfig => firstLaunchNarrativeConfig;
        public NarrativeSpeakerCatalog FirstLaunchSpeakerCatalog => firstLaunchSpeakerCatalog;
        public NarrativePunctuationConfig FirstLaunchPunctuationProfile => firstLaunchPunctuationProfile;
        public FirstLaunchLanguageChoiceView FirstLaunchLanguageChoiceView => firstLaunchLanguageChoiceView;
        public NarrativeLocaleConfig FirstLaunchPersianLocale => firstLaunchPersianLocale;
        public NarrativeSequenceConfig[] CampaignMissionNarrativeConfigs => campaignMissionNarrativeConfigs;
        public MissionDefinitionCatalogConfig CampaignMissionCatalog => campaignMissionCatalog;
        public MissionDefinitionConfig CampaignMissionDefinition => campaignMissionDefinition;
        public ScenarioSetupConfig CampaignScenarioSetup => campaignScenarioSetup;
        public OperationMapCatalogConfig CampaignOperationMapCatalog => campaignOperationMapCatalog;
        public PerformanceDiagnosticsSystemHelper PerformanceDiagnostics => menuBootstrapSystem.PerformanceDiagnostics;
        public bool IsPerformanceDiagnosticsInitialized => menuBootstrapSystem.IsPerformanceDiagnosticsInitialized;

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
            NarrativeSequenceView configuredFirstLaunchNarrativeView = null,
            NarrativeSequenceConfig configuredFirstLaunchNarrativeConfig = null,
            NarrativeSpeakerCatalog configuredFirstLaunchSpeakerCatalog = null,
            NarrativePunctuationConfig configuredFirstLaunchPunctuationProfile = null,
            FirstLaunchLanguageChoiceView configuredFirstLaunchLanguageChoiceView = null,
            NarrativeLocaleConfig configuredFirstLaunchPersianLocale = null,
            NarrativeSequenceConfig[] configuredCampaignMissionNarrativeConfigs = null,
            MissionDefinitionConfig configuredCampaignMissionDefinition = null,
            ScenarioSetupConfig configuredCampaignScenarioSetup = null,
            OperationMapCatalogConfig configuredCampaignOperationMapCatalog = null,
            MissionDefinitionCatalogConfig configuredCampaignMissionCatalog = null)
        {
            if (configuredRuntimeUiConfig != null)
                runtimeUiConfig = configuredRuntimeUiConfig;
            uiCamera = configuredUiCamera;
            uiCanvas = configuredUiCanvas;
            shellView = configuredShellView;
            shellEcsPresentation = configuredShellEcsPresentation;
            contentSystem = configuredContentSystem;
            router = configuredRouter;
            if (configuredFirstLaunchNarrativeView != null)
                firstLaunchNarrativeView = configuredFirstLaunchNarrativeView;
            if (configuredFirstLaunchNarrativeConfig != null)
                firstLaunchNarrativeConfig = configuredFirstLaunchNarrativeConfig;
            if (configuredFirstLaunchSpeakerCatalog != null)
                firstLaunchSpeakerCatalog = configuredFirstLaunchSpeakerCatalog;
            if (configuredFirstLaunchPunctuationProfile != null)
                firstLaunchPunctuationProfile = configuredFirstLaunchPunctuationProfile;
            if (configuredFirstLaunchLanguageChoiceView != null)
                firstLaunchLanguageChoiceView = configuredFirstLaunchLanguageChoiceView;
            if (configuredFirstLaunchPersianLocale != null)
                firstLaunchPersianLocale = configuredFirstLaunchPersianLocale;
            if (configuredCampaignMissionNarrativeConfigs != null)
                campaignMissionNarrativeConfigs = configuredCampaignMissionNarrativeConfigs;
            if (configuredCampaignMissionDefinition != null)
                campaignMissionDefinition = configuredCampaignMissionDefinition;
            if (configuredCampaignScenarioSetup != null)
                campaignScenarioSetup = configuredCampaignScenarioSetup;
            if (configuredCampaignOperationMapCatalog != null)
                campaignOperationMapCatalog = configuredCampaignOperationMapCatalog;
            if (configuredCampaignMissionCatalog != null)
                campaignMissionCatalog = configuredCampaignMissionCatalog;
        }

        public void ApplyRuntimeUiMode()
        {
            if (uiCanvas != null)
            {
                if (uiCanvas.transform.localScale != Vector3.one)
                    uiCanvas.transform.localScale = Vector3.one;

                if (!uiCanvas.gameObject.activeSelf)
                    uiCanvas.gameObject.SetActive(true);

                if (!uiCanvas.enabled)
                    uiCanvas.enabled = true;
            }
            if (shellEcsPresentation != null && !shellEcsPresentation.enabled)
                shellEcsPresentation.enabled = true;
            if (contentSystem != null && !contentSystem.enabled)
                contentSystem.enabled = true;
            if (router != null && !router.enabled)
                router.enabled = true;
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
            campaignMissionBootstrap.Update(this, Time.unscaledDeltaTime);
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
            campaignMissionBootstrap.Shutdown();
            menuBootstrapSystem.Shutdown(this);
        }
    }
}

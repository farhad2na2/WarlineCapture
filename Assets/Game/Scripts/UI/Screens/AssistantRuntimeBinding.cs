using System;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AssistantRuntimeBinding : MonoBehaviour
{
    [SerializeField] private AssistantPanelController panelController;
    [SerializeField] private AssistantButtonView buttonView;
    [SerializeField] private bool refreshOnUpdate = true;
    [SerializeField] private float refreshIntervalSeconds = 0.25f;

    private readonly TutorialSessionState _sessionState = new();
    private WarlineCaptureAssistantService _assistantService;
    private AssistantContextProvider _contextProvider;
    private CommandIntentExecutor _commandExecutor;
    private Func<TutorialSessionState, AssistantContext> _contextFactory;
    private float _nextRefreshTime;
    private AssistantContext _lastContext;
    private AssistantRecommendation _lastRecommendation;
    private AssistantPanelController _wiredPanelController;
    private bool _lastPlayerInputOverrideHandled;

    public AssistantPanelController PanelController => panelController;
    public AssistantButtonView ButtonView => buttonView;
    public WarlineCaptureAssistantService AssistantService => _assistantService;
    public AssistantContext LastContext => _lastContext;
    public AssistantRecommendation LastRecommendation => _lastRecommendation;
    public TacticalCommandResult LastDoItResult { get; private set; }
    public bool HasLastDoItResult { get; private set; }
    public bool LastPlayerInputOverrideHandled => _lastPlayerInputOverrideHandled;

    private void Awake()
    {
        ResolveReferences();
        EnsureRuntime();
        WirePanelController();
    }

    private void OnEnable()
    {
        RefreshNow();
    }

    private void Update()
    {
        if (!refreshOnUpdate || Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshIntervalSeconds);
        RefreshNow();
    }

    private void OnDestroy()
    {
        UnwirePanelController();
    }

    public void SetRuntimeForTests(
        AssistantPanelController controller,
        AssistantButtonView assistantButton,
        WarlineCaptureAssistantService service,
        Func<TutorialSessionState, AssistantContext> contextFactory,
        CommandIntentExecutor executor)
    {
        UnwirePanelController();
        panelController = controller;
        buttonView = assistantButton;
        _assistantService = service;
        _contextFactory = contextFactory;
        _commandExecutor = executor;
        WirePanelController();
    }

    public void BindRuntimeDependencies(
        World world,
        TacticalMapRuntimeLoader loader,
        RTSSelectionSystem selectionSystem,
        BattleHudGameplayBridge gameplayBridge,
        WarlineCaptureRouter router = null,
        WarlineCaptureMatchResultFlow resultFlow = null,
        MatchObjectivePanelController objectivePanel = null)
    {
        EnsureRuntime();
        _contextFactory = null;
        _contextProvider = new AssistantContextProvider(
            world,
            loader,
            selectionSystem,
            gameplayBridge,
            router,
            resultFlow,
            objectivePanel);
        _commandExecutor = new CommandIntentExecutor(
            _assistantService.SessionState,
            world,
            loader,
            selectionSystem);
    }

    public AssistantPanelPresentationData RefreshNow()
    {
        EnsureRuntime();
        ResolveReferences();
        WirePanelController();

        _lastContext = BuildContext();
        _lastRecommendation = _assistantService.Evaluate(_lastContext);
        AssistantPanelPresentationData presentation = CreatePanelPresentation();
        buttonView?.SetState(ResolveButtonState(_lastContext, _lastRecommendation));

        if (panelController != null && panelController.IsOpen)
            panelController.ShowRecommendation(presentation);

        return presentation;
    }

    private void HandleShowMeRequested(string recommendationId)
    {
        if (!_lastRecommendation.HasRecommendation ||
            _lastRecommendation.RecommendationId != recommendationId ||
            !_lastRecommendation.ShowMeIntent.HasIntent)
        {
            return;
        }

        _assistantService.SessionState.SetActivePreview(_lastRecommendation.ShowMeIntent.IntentId);
        RefreshNow();
    }

    private void HandleDoItRequested(string recommendationId)
    {
        if (!_lastRecommendation.HasRecommendation || _lastRecommendation.RecommendationId != recommendationId)
            return;

        EnsureRuntime();
        _assistantService.SessionState.SetActiveTakeover(_lastRecommendation.DoItIntent.IntentId);
        LastDoItResult = _assistantService.ExecuteCurrentDoIt(_commandExecutor);
        HasLastDoItResult = true;
        _assistantService.StopAssistantOwnedState();
        RefreshNow();
    }

    private void HandleStopRequested(string recommendationId)
    {
        EnsureRuntime();
        bool dismissesResultExplain = _lastRecommendation.HasRecommendation &&
            _lastRecommendation.RecommendationId == M01AssistantIds.ResultExplainRecommendationId;

        TacticalCommandResult result = _commandExecutor != null
            ? _commandExecutor.StopAssistantControl()
            : TacticalCommandResult.Success();

        LastDoItResult = result;
        HasLastDoItResult = true;
        _assistantService.StopAssistantOwnedState();

        if (dismissesResultExplain)
            _assistantService.DismissCurrentRecommendation();

        RefreshNow();

        if (dismissesResultExplain)
            panelController?.Hide();
    }

    public bool NotifyPlayerInputOutsideAssistant()
    {
        EnsureRuntime();
        _lastPlayerInputOverrideHandled = false;

        if (!IsAssistantOwnedState())
            return false;

        _lastPlayerInputOverrideHandled = true;
        if (_lastContext != null)
            _lastContext.CurrentControlOwnerState = AssistantControlOwnerState.PlayerOverridePending;

        _commandExecutor?.StopAssistantControl();
        _assistantService.StopAssistantOwnedState();
        RefreshNow();
        return true;
    }

    private AssistantContext BuildContext()
    {
        if (_contextFactory != null)
            return _contextFactory(_assistantService.SessionState) ?? new AssistantContext();

        return _contextProvider.BuildContext(_assistantService.SessionState);
    }

    private AssistantPanelPresentationData CreatePanelPresentation()
    {
        return _assistantService.CreatePresentationData()
            .WithStatusLabel(ResolveOwnershipStatusLabel(_lastContext));
    }

    private void ResolveReferences()
    {
        if (panelController == null)
            panelController = GetComponent<AssistantPanelController>();

        if (buttonView == null)
            buttonView = null;
    }

    private void EnsureRuntime()
    {
        if (_assistantService == null)
            _assistantService = new WarlineCaptureAssistantService(new M01AssistantRecommendationProvider(), _sessionState);
        if (_contextProvider == null)
            _contextProvider = new AssistantContextProvider();
        if (_commandExecutor == null)
            _commandExecutor = new CommandIntentExecutor(_assistantService.SessionState);
    }

    private void WirePanelController()
    {
        if (panelController == null)
            return;
        if (_wiredPanelController == panelController)
            return;

        panelController.SetPresentationProvider(RefreshNow);
        panelController.ShowMeRequested += HandleShowMeRequested;
        panelController.DoItRequested += HandleDoItRequested;
        panelController.StopRequested += HandleStopRequested;
        _wiredPanelController = panelController;
    }

    private void UnwirePanelController()
    {
        if (_wiredPanelController == null)
            return;

        _wiredPanelController.ShowMeRequested -= HandleShowMeRequested;
        _wiredPanelController.DoItRequested -= HandleDoItRequested;
        _wiredPanelController.StopRequested -= HandleStopRequested;
        _wiredPanelController.SetPresentationProvider(null);
        _wiredPanelController = null;
    }

    private static AssistantButtonVisualState ResolveButtonState(
        AssistantContext context,
        AssistantRecommendation recommendation)
    {
        if (context != null && context.AssistantMuted)
            return AssistantButtonVisualState.Muted;
        if (context != null && context.CurrentControlOwnerState == AssistantControlOwnerState.AssistantTakeover)
            return AssistantButtonVisualState.Takeover;
        if (recommendation.HasRecommendation &&
            (recommendation.BlockingReasonCode != TacticalCommandReasonCode.None ||
                context != null && !context.LastCommandResultAccepted))
        {
            return AssistantButtonVisualState.Critical;
        }
        if (recommendation.HasRecommendation)
            return AssistantButtonVisualState.Recommendation;

        return AssistantButtonVisualState.Idle;
    }

    private bool IsAssistantOwnedState()
    {
        TutorialSessionState session = _assistantService.SessionState;
        return !string.IsNullOrEmpty(session.ActivePreviewIntentId) ||
            !string.IsNullOrEmpty(session.ActiveTakeoverIntentId) ||
            _lastContext != null &&
            (_lastContext.CurrentControlOwnerState == AssistantControlOwnerState.AssistantPreview ||
                _lastContext.CurrentControlOwnerState == AssistantControlOwnerState.AssistantTakeover ||
                _lastContext.CurrentControlOwnerState == AssistantControlOwnerState.PlayerOverridePending);
    }

    private static string ResolveOwnershipStatusLabel(AssistantContext context)
    {
        if (context == null)
            return "ADAPTIVE RESPONSE INTELLIGENCE ASSISTANT";

        return context.CurrentControlOwnerState switch
        {
            AssistantControlOwnerState.AssistantTakeover => "ARIA CONTROL ACTIVE - PLAYER INPUT OVERRIDES",
            AssistantControlOwnerState.AssistantPreview => "ARIA PREVIEW ACTIVE - STOP RETURNS CONTROL",
            AssistantControlOwnerState.PlayerOverridePending => "PLAYER OVERRIDE - ARIA RELEASING CONTROL",
            AssistantControlOwnerState.Guided => "ARIA GUIDANCE ACTIVE - PLAYER REMAINS IN CONTROL",
            _ => "ADAPTIVE RESPONSE INTELLIGENCE ASSISTANT"
        };
    }
}

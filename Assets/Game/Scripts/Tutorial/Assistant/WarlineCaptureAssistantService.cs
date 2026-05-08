public sealed class WarlineCaptureAssistantService
{
    private readonly M01AssistantRecommendationProvider _m01Provider;
    private readonly TutorialSessionState _sessionState;

    public WarlineCaptureAssistantService()
        : this(new M01AssistantRecommendationProvider(), new TutorialSessionState())
    {
    }

    public WarlineCaptureAssistantService(M01AssistantRecommendationProvider m01Provider, TutorialSessionState sessionState)
    {
        _m01Provider = m01Provider ?? new M01AssistantRecommendationProvider();
        _sessionState = sessionState ?? new TutorialSessionState();
    }

    public TutorialSessionState SessionState => _sessionState;
    public AssistantRecommendation CurrentRecommendation { get; private set; }

    public AssistantRecommendation Evaluate(AssistantContext context)
    {
        CurrentRecommendation = _m01Provider.Evaluate(context, _sessionState);
        return CurrentRecommendation;
    }

    public void DismissCurrentRecommendation()
    {
        if (CurrentRecommendation.HasRecommendation)
            _sessionState.DismissRecommendation(CurrentRecommendation.RecommendationId);
    }

    public void CompleteStep(string stepId)
    {
        _sessionState.CompleteStep(stepId);
    }

    public void StopAssistantOwnedState()
    {
        _sessionState.ClearAssistantOwnedState();
    }

    public TacticalCommandResult ExecuteCurrentDoIt(CommandIntentExecutor executor)
    {
        if (executor == null || !CurrentRecommendation.HasRecommendation)
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

        return executor.ExecuteDoIt(CurrentRecommendation);
    }

    public AssistantPanelPresentationData CreatePresentationData()
    {
        if (!CurrentRecommendation.HasRecommendation)
            return new AssistantPanelPresentationData(string.Empty, string.Empty, string.Empty, System.Array.Empty<string>(), false, false, false);

        return new AssistantPanelPresentationData(
            CurrentRecommendation.RecommendationId,
            CurrentRecommendation.Title,
            CurrentRecommendation.Body,
            CurrentRecommendation.Chips,
            CurrentRecommendation.CanShow,
            CurrentRecommendation.CanExecute,
            CurrentRecommendation.CanStop);
    }
}

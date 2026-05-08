using System;
using System.Collections.Generic;

public sealed class TutorialSessionState
{
    private readonly HashSet<string> _completedStepIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _dismissedRecommendationIds = new(StringComparer.Ordinal);

    public string ActiveRecommendationId { get; private set; } = string.Empty;
    public string ActivePreviewIntentId { get; private set; } = string.Empty;
    public string ActiveTakeoverIntentId { get; private set; } = string.Empty;
    public TacticalCommandReasonCode LastRejectedReasonCode { get; private set; } = TacticalCommandReasonCode.None;
    public string LastRejectedAtStepId { get; private set; } = string.Empty;
    public bool M01MoveCommandAccepted { get; private set; }
    public bool M01AttackCommandAccepted { get; private set; }
    public bool M01ResultExplained { get; private set; }

    public IReadOnlyCollection<string> CompletedStepIds => _completedStepIds;
    public IReadOnlyCollection<string> DismissedRecommendationIds => _dismissedRecommendationIds;

    public void SetActiveRecommendation(string recommendationId)
    {
        ActiveRecommendationId = recommendationId ?? string.Empty;
    }

    public void SetActivePreview(string intentId)
    {
        ActivePreviewIntentId = intentId ?? string.Empty;
    }

    public void SetActiveTakeover(string intentId)
    {
        ActiveTakeoverIntentId = intentId ?? string.Empty;
    }

    public void ClearAssistantOwnedState()
    {
        ActivePreviewIntentId = string.Empty;
        ActiveTakeoverIntentId = string.Empty;
    }

    public void CompleteStep(string stepId)
    {
        if (!string.IsNullOrWhiteSpace(stepId))
            _completedStepIds.Add(stepId);
    }

    public bool IsStepCompleted(string stepId)
    {
        return !string.IsNullOrWhiteSpace(stepId) && _completedStepIds.Contains(stepId);
    }

    public void DismissRecommendation(string recommendationId)
    {
        if (!string.IsNullOrWhiteSpace(recommendationId))
            _dismissedRecommendationIds.Add(recommendationId);
    }

    public bool IsRecommendationDismissed(string recommendationId)
    {
        return !string.IsNullOrWhiteSpace(recommendationId) && _dismissedRecommendationIds.Contains(recommendationId);
    }

    public void RecordRejectedCommand(TacticalCommandReasonCode reasonCode, string stepId)
    {
        LastRejectedReasonCode = reasonCode;
        LastRejectedAtStepId = stepId ?? string.Empty;
    }

    public void MarkM01MoveCommandAccepted()
    {
        M01MoveCommandAccepted = true;
        CompleteStep(M01AssistantIds.MoveStepId);
    }

    public void MarkM01AttackCommandAccepted()
    {
        M01AttackCommandAccepted = true;
        CompleteStep(M01AssistantIds.AttackStepId);
    }

    public void MarkM01ResultExplained()
    {
        M01ResultExplained = true;
        CompleteStep(M01AssistantIds.CompleteStepId);
    }
}

using Unity.Entities;

public sealed class CommandIntentExecutor
{
    private readonly TutorialSessionState _sessionState;
    private readonly World _world;
    private readonly TacticalMapRuntimeLoader _loader;
    private readonly RTSSelectionSystem _selectionSystem;

    public CommandIntentExecutor()
        : this(null)
    {
    }

    public CommandIntentExecutor(TutorialSessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public CommandIntentExecutor(
        TutorialSessionState sessionState,
        World world,
        TacticalMapRuntimeLoader loader,
        RTSSelectionSystem selectionSystem)
    {
        _sessionState = sessionState;
        _world = world;
        _loader = loader;
        _selectionSystem = selectionSystem;
    }

    public TacticalCommandResult ExecuteDoIt(AssistantRecommendation recommendation)
    {
        return ExecuteDoItIntent(recommendation.DoItIntent, recommendation.StepId);
    }

    public TacticalCommandResult ExecuteDoItIntent(AssistantIntent intent)
    {
        return ExecuteDoItIntent(intent, intent.StepId);
    }

    public TacticalCommandResult ExecuteDoItIntent(AssistantIntent intent, string stepId)
    {
        if (!intent.HasIntent || !intent.CanExecuteGameplay)
            return Reject(TacticalCommandReasonCode.TargetNotAttackable, stepId);
        if (intent.MissionId != M01AssistantIds.MissionId)
            return Reject(TacticalCommandReasonCode.TargetNotAttackable, stepId);

        TacticalCommandResult result = intent.IntentKind switch
        {
            AssistantIntentKind.SelectRuntimeEntity => ExecuteSelectRuntimeEntity(intent),
            AssistantIntentKind.MoveSelectedUnits => ExecuteMoveSelectedUnits(intent),
            AssistantIntentKind.AttackTarget => ExecuteAttackTarget(intent),
            _ => TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable)
        };

        if (result.Accepted)
            MarkAcceptedStep(intent, stepId);
        else
            _sessionState?.RecordRejectedCommand(result.ReasonCode, stepId);

        return result;
    }

    public TacticalCommandResult StopAssistantControl()
    {
        _sessionState?.ClearAssistantOwnedState();
        return TacticalCommandResult.Success();
    }

    public TacticalCommandResult GetBuildCommandResult()
    {
        TacticalCommandResult result = M01AssistantCommandRuntime.GetBuildCommandResult();
        if (!result.Accepted)
            _sessionState?.RecordRejectedCommand(result.ReasonCode, string.Empty);

        return result;
    }

    private TacticalCommandResult ExecuteSelectRuntimeEntity(AssistantIntent intent)
    {
        if (intent.TargetType != AssistantTargetType.RuntimeEntity ||
            intent.TargetId != M01AssistantIds.PlayerSquadEntityId)
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        if (_world != null || _selectionSystem != null)
            return M01AssistantCommandRuntime.TrySelectRuntimeEntity(_world, _selectionSystem, intent.TargetId);

        return M01AssistantCommandRuntime.TrySelectRuntimeEntity(intent.TargetId);
    }

    private TacticalCommandResult ExecuteMoveSelectedUnits(AssistantIntent intent)
    {
        if (intent.TargetType != AssistantTargetType.TacticalAnchor ||
            intent.TargetId != M01AssistantIds.MoveTargetAnchorId)
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        if (_world != null || _loader != null || _selectionSystem != null)
            return M01AssistantCommandRuntime.TryIssueMoveToAnchor(_world, _loader, _selectionSystem, intent.TargetId);

        return M01AssistantCommandRuntime.TryIssueMoveToAnchor(intent.TargetId);
    }

    private TacticalCommandResult ExecuteAttackTarget(AssistantIntent intent)
    {
        if (intent.TargetType != AssistantTargetType.RuntimeEntity ||
            intent.TargetId != M01AssistantIds.EnemyPatrolEntityId)
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        if (_world != null || _selectionSystem != null)
            return M01AssistantCommandRuntime.TryIssueAttackTarget(_world, _selectionSystem, intent.TargetId);

        return M01AssistantCommandRuntime.TryIssueAttackTarget(intent.TargetId);
    }

    private void MarkAcceptedStep(AssistantIntent intent, string stepId)
    {
        if (_sessionState == null)
            return;

        switch (intent.IntentKind)
        {
            case AssistantIntentKind.SelectRuntimeEntity:
                _sessionState.CompleteStep(M01AssistantIds.SelectSquadStepId);
                break;
            case AssistantIntentKind.MoveSelectedUnits:
                _sessionState.MarkM01MoveCommandAccepted();
                break;
            case AssistantIntentKind.AttackTarget:
                _sessionState.MarkM01AttackCommandAccepted();
                break;
            default:
                _sessionState.CompleteStep(stepId);
                break;
        }
    }

    private TacticalCommandResult Reject(TacticalCommandReasonCode reasonCode, string stepId)
    {
        _sessionState?.RecordRejectedCommand(reasonCode, stepId);
        return TacticalCommandResult.Rejected(reasonCode);
    }
}

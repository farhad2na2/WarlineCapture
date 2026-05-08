using System;

public enum AssistantIntentKind
{
    None,
    FocusUiElement,
    FocusRuntimeEntity,
    PreviewPathToAnchor,
    FocusObjectiveAnchor,
    SelectRuntimeEntity,
    MoveSelectedUnits,
    AttackTarget,
    StopAssistantControl
}

public enum AssistantTargetType
{
    None,
    UiElement,
    RuntimeEntity,
    TacticalAnchor,
    ObjectiveAnchor,
    Popup,
    AssistantControl
}

public enum AssistantControlOwnerState
{
    Player,
    Guided,
    AssistantPreview,
    AssistantTakeover,
    PlayerOverridePending
}

public static class M01AssistantIds
{
    public const string MissionId = "saga.ch01.m01.first_contact";
    public const string ScenarioSetupId = "scenario.ch01.m01.first_contact";
    public const string LevelId = "level.ch01.district_edge_01";
    public const string IsoMapId = "iso.ch01.district_edge_01";
    public const string PlayerSquadEntityId = "unit.player.rifle_squad_01";
    public const string EnemyPatrolEntityId = "unit.enemy.patrol_01";
    public const string MoveTargetAnchorId = "tutorial.move_target.cover_01";
    public const string ObjectiveAnchorId = "objective.destroy_patrol_group";
    public const string ResultPopupId = "POP-05_MissionResult";
    public const string ObjectivePanelId = "BattleHud.ObjectivePanel";

    public const string ObjectivesStepId = "ftue.m01.objectives";
    public const string SelectSquadStepId = "ftue.m01.select_squad";
    public const string MoveStepId = "ftue.m01.move";
    public const string AttackStepId = "ftue.m01.attack";
    public const string CompleteStepId = "ftue.m01.complete";

    public const string ObjectivesRecommendationId = "M01.ObjectivesIntro";
    public const string SelectSquadRecommendationId = "M01.SelectSquad";
    public const string MoveToCoverRecommendationId = "M01.MoveToCover";
    public const string AttackPatrolRecommendationId = "M01.AttackPatrol";
    public const string InvalidCommandRecommendationId = "M01.InvalidCommandRecovery";
    public const string ResultExplainRecommendationId = "M01.ResultExplain";
}

public readonly struct AssistantIntent
{
    public AssistantIntent(
        string intentId,
        AssistantIntentKind intentKind,
        string missionId,
        string stepId,
        AssistantTargetType targetType,
        string targetId,
        TacticalCommandMode commandMode,
        bool canExecuteGameplay,
        bool requiresSelectedEntity,
        bool requiresVisibleTarget,
        string completionBoundary)
    {
        IntentId = intentId ?? string.Empty;
        IntentKind = intentKind;
        MissionId = missionId ?? string.Empty;
        StepId = stepId ?? string.Empty;
        TargetType = targetType;
        TargetId = targetId ?? string.Empty;
        CommandMode = commandMode;
        CanExecuteGameplay = canExecuteGameplay;
        RequiresSelectedEntity = requiresSelectedEntity;
        RequiresVisibleTarget = requiresVisibleTarget;
        CompletionBoundary = completionBoundary ?? string.Empty;
    }

    public string IntentId { get; }
    public AssistantIntentKind IntentKind { get; }
    public string MissionId { get; }
    public string StepId { get; }
    public AssistantTargetType TargetType { get; }
    public string TargetId { get; }
    public TacticalCommandMode CommandMode { get; }
    public bool CanExecuteGameplay { get; }
    public bool RequiresSelectedEntity { get; }
    public bool RequiresVisibleTarget { get; }
    public string CompletionBoundary { get; }
    public bool HasIntent => IntentKind != AssistantIntentKind.None && !string.IsNullOrEmpty(IntentId);

    public static AssistantIntent None => default;
}

public readonly struct AssistantRecommendation
{
    public AssistantRecommendation(
        string recommendationId,
        string stepId,
        string missionId,
        int priority,
        string title,
        string body,
        string reason,
        string tab,
        string[] chips,
        string[] highlightTargets,
        AssistantIntent showMeIntent,
        AssistantIntent doItIntent,
        bool canShow,
        bool canExecute,
        bool canStop,
        TacticalCommandReasonCode blockingReasonCode,
        string completionRule,
        bool suppressAfterCompletion)
    {
        RecommendationId = recommendationId ?? string.Empty;
        StepId = stepId ?? string.Empty;
        MissionId = missionId ?? string.Empty;
        Priority = priority;
        Title = title ?? string.Empty;
        Body = body ?? string.Empty;
        Reason = reason ?? string.Empty;
        Tab = tab ?? string.Empty;
        Chips = chips ?? Array.Empty<string>();
        HighlightTargets = highlightTargets ?? Array.Empty<string>();
        ShowMeIntent = showMeIntent;
        DoItIntent = doItIntent;
        CanShow = canShow;
        CanExecute = canExecute;
        CanStop = canStop;
        BlockingReasonCode = blockingReasonCode;
        CompletionRule = completionRule ?? string.Empty;
        SuppressAfterCompletion = suppressAfterCompletion;
    }

    public string RecommendationId { get; }
    public string StepId { get; }
    public string MissionId { get; }
    public int Priority { get; }
    public string Title { get; }
    public string Body { get; }
    public string Reason { get; }
    public string Tab { get; }
    public string[] Chips { get; }
    public string[] HighlightTargets { get; }
    public AssistantIntent ShowMeIntent { get; }
    public AssistantIntent DoItIntent { get; }
    public bool CanShow { get; }
    public bool CanExecute { get; }
    public bool CanStop { get; }
    public TacticalCommandReasonCode BlockingReasonCode { get; }
    public string CompletionRule { get; }
    public bool SuppressAfterCompletion { get; }
    public bool HasRecommendation => !string.IsNullOrEmpty(RecommendationId);

    public static AssistantRecommendation None => default;
}

public sealed class AssistantContext
{
    public string ActiveRoute { get; set; } = string.Empty;
    public string MissionId { get; set; } = string.Empty;
    public string ScenarioSetupId { get; set; } = string.Empty;
    public string LevelId { get; set; } = string.Empty;
    public string IsoMapId { get; set; } = string.Empty;
    public string MapPreviewArtId { get; set; } = string.Empty;
    public string MinimapArtId { get; set; } = string.Empty;
    public bool IsMatchOverlayActive { get; set; }
    public bool ObjectivePanelVisible { get; set; }
    public bool ResultPopupVisible { get; set; }
    public bool CommandSquadSpawned { get; set; }
    public bool CommandSquadSelected { get; set; }
    public bool CommandSquadAlive { get; set; } = true;
    public bool EnemyPatrolSpawned { get; set; }
    public bool EnemyPatrolVisible { get; set; }
    public bool EnemyPatrolDestroyed { get; set; }
    public bool MoveTargetAvailable { get; set; }
    public bool MoveCommandAccepted { get; set; }
    public bool AttackCommandAccepted { get; set; }
    public bool M01ResultExplained { get; set; }
    public bool LastCommandResultAccepted { get; set; } = true;
    public TacticalCommandReasonCode LastCommandReasonCode { get; set; } = TacticalCommandReasonCode.None;
    public string LastCommandReasonText { get; set; } = string.Empty;
    public TacticalCommandMode CurrentCommandMode { get; set; } = TacticalCommandMode.None;
    public AssistantControlOwnerState CurrentControlOwnerState { get; set; } = AssistantControlOwnerState.Player;
    public string AssistanceLevel { get; set; } = "FullGuidance";
    public bool AssistantMuted { get; set; }
    public bool TypedCommandHooksAvailable { get; set; }

    public bool IsM01Active =>
        MissionId == M01AssistantIds.MissionId &&
        (string.IsNullOrEmpty(ScenarioSetupId) || ScenarioSetupId == M01AssistantIds.ScenarioSetupId) &&
        (string.IsNullOrEmpty(LevelId) || LevelId == M01AssistantIds.LevelId) &&
        (string.IsNullOrEmpty(IsoMapId) || IsoMapId == M01AssistantIds.IsoMapId);
}

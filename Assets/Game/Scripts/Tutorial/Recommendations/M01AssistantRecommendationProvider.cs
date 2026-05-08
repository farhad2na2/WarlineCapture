using System;

public sealed class M01AssistantRecommendationProvider
{
    public AssistantRecommendation Evaluate(AssistantContext context, TutorialSessionState session)
    {
        if (context == null || !context.IsM01Active || context.AssistantMuted)
            return AssistantRecommendation.None;

        session ??= new TutorialSessionState();
        UpdateSessionProgress(context, session);

        AssistantRecommendation recovery = TryCreateInvalidCommandRecovery(context, session);
        if (recovery.HasRecommendation)
            return Activate(session, recovery);

        AssistantRecommendation recommendation = CreateBaseRecommendation(context, session);
        if (recommendation.HasRecommendation)
            return Activate(session, recommendation);

        session.SetActiveRecommendation(string.Empty);
        return AssistantRecommendation.None;
    }

    private static void UpdateSessionProgress(AssistantContext context, TutorialSessionState session)
    {
        if (context.CommandSquadSelected)
        {
            session.CompleteStep(M01AssistantIds.ObjectivesStepId);
            session.CompleteStep(M01AssistantIds.SelectSquadStepId);
        }

        if (context.MoveCommandAccepted)
            session.MarkM01MoveCommandAccepted();

        if (context.AttackCommandAccepted || context.EnemyPatrolDestroyed)
            session.MarkM01AttackCommandAccepted();

        if (context.M01ResultExplained)
            session.MarkM01ResultExplained();
    }

    private static AssistantRecommendation CreateBaseRecommendation(AssistantContext context, TutorialSessionState session)
    {
        if (context.ResultPopupVisible || context.EnemyPatrolDestroyed)
            return CreateResultExplain(context, session);

        if (context.ObjectivePanelVisible && !session.IsStepCompleted(M01AssistantIds.ObjectivesStepId))
            return CreateObjectivesIntro(session);

        if (context.CommandSquadSpawned && !context.CommandSquadSelected && !session.IsStepCompleted(M01AssistantIds.SelectSquadStepId))
            return CreateSelectSquad(context, session, TacticalCommandReasonCode.None);

        if (context.CommandSquadSelected && context.MoveTargetAvailable && !session.IsStepCompleted(M01AssistantIds.MoveStepId))
            return CreateMoveToCover(context, session, TacticalCommandReasonCode.None);

        if (context.EnemyPatrolSpawned && context.EnemyPatrolVisible && !context.EnemyPatrolDestroyed && !session.IsStepCompleted(M01AssistantIds.AttackStepId))
            return CreateAttackPatrol(context, session, TacticalCommandReasonCode.None);

        return AssistantRecommendation.None;
    }

    private static AssistantRecommendation TryCreateInvalidCommandRecovery(AssistantContext context, TutorialSessionState session)
    {
        if (context.LastCommandResultAccepted || context.LastCommandReasonCode == TacticalCommandReasonCode.None)
            return AssistantRecommendation.None;

        session.RecordRejectedCommand(context.LastCommandReasonCode, ResolveCurrentStepId(context, session));

        return context.LastCommandReasonCode switch
        {
            TacticalCommandReasonCode.NoSelection => CreateSelectSquad(context, session, context.LastCommandReasonCode, recovery: true),
            TacticalCommandReasonCode.TargetOutOfBounds => CreateRecoveryForCurrentTarget(context, session),
            TacticalCommandReasonCode.TargetBlocked => CreateMoveToCover(context, session, context.LastCommandReasonCode, recovery: true),
            TacticalCommandReasonCode.TargetNotEnemy => CreateRecoveryForCurrentTarget(context, session),
            TacticalCommandReasonCode.TargetNotAttackable => CreateRecoveryForCurrentTarget(context, session),
            TacticalCommandReasonCode.TargetUnreachable => CreateRecoveryForCurrentTarget(context, session),
            TacticalCommandReasonCode.MissionDoesNotAllowBuild => CreateBuildLockedRecovery(context, session),
            TacticalCommandReasonCode.CommandUnavailable => CreatePlainRejectedRecovery(context, session),
            TacticalCommandReasonCode.CameraJumpUnavailable => CreatePlainRejectedRecovery(context, session),
            _ => CreatePlainRejectedRecovery(context, session)
        };
    }

    private static AssistantRecommendation CreateRecoveryForCurrentTarget(AssistantContext context, TutorialSessionState session)
    {
        if (context.CommandSquadSelected && context.MoveTargetAvailable && !session.IsStepCompleted(M01AssistantIds.MoveStepId))
            return CreateMoveToCover(context, session, context.LastCommandReasonCode, recovery: true);

        if (context.EnemyPatrolSpawned && context.EnemyPatrolVisible && !context.EnemyPatrolDestroyed)
            return CreateAttackPatrol(context, session, context.LastCommandReasonCode, recovery: true);

        if (context.CommandSquadSpawned && !context.CommandSquadSelected)
            return CreateSelectSquad(context, session, context.LastCommandReasonCode, recovery: true);

        return CreatePlainRejectedRecovery(context, session);
    }

    private static AssistantRecommendation CreateObjectivesIntro(TutorialSessionState session)
    {
        return CreateRecommendation(
            session,
            M01AssistantIds.ObjectivesRecommendationId,
            M01AssistantIds.ObjectivesStepId,
            "Read the objective",
            "Destroy the hostile patrol and keep the command squad alive.",
            "The objective tracker is the source of truth for the current win condition.",
            new[] { "Check objective tracker" },
            new[] { M01AssistantIds.ObjectivePanelId },
            CreateIntent("show.objective_panel", AssistantIntentKind.FocusUiElement, M01AssistantIds.ObjectivesStepId, AssistantTargetType.UiElement, M01AssistantIds.ObjectivePanelId, TacticalCommandMode.None, false, false, false, "preview"),
            AssistantIntent.None,
            canShow: true,
            canExecute: false,
            canStop: IsAssistantOwned(session),
            TacticalCommandReasonCode.None,
            "Objective panel acknowledged or squad selected.");
    }

    private static AssistantRecommendation CreateSelectSquad(
        AssistantContext context,
        TutorialSessionState session,
        TacticalCommandReasonCode blockingReasonCode,
        bool recovery = false)
    {
        string recommendationId = recovery ? M01AssistantIds.InvalidCommandRecommendationId : M01AssistantIds.SelectSquadRecommendationId;
        string title = recovery ? "Select a squad first" : "Select Rifle Squad";
        string body = recovery
            ? "Orders start with selection. Select the highlighted response team before issuing commands."
            : "Orders start with selection. Select the highlighted response team.";

        return CreateRecommendation(
            session,
            recommendationId,
            M01AssistantIds.SelectSquadStepId,
            title,
            body,
            "The command squad must be selected before ARIA can recommend movement or attack orders.",
            new[] { "Select squad", "Prepare move order" },
            new[] { M01AssistantIds.PlayerSquadEntityId },
            CreateIntent("show.select_squad", AssistantIntentKind.FocusRuntimeEntity, M01AssistantIds.SelectSquadStepId, AssistantTargetType.RuntimeEntity, M01AssistantIds.PlayerSquadEntityId, TacticalCommandMode.None, false, false, false, "preview"),
            CreateIntent("do.select_squad", AssistantIntentKind.SelectRuntimeEntity, M01AssistantIds.SelectSquadStepId, AssistantTargetType.RuntimeEntity, M01AssistantIds.PlayerSquadEntityId, TacticalCommandMode.None, true, false, false, "one selection intent"),
            canShow: context.CommandSquadSpawned,
            canExecute: context.TypedCommandHooksAvailable && context.CommandSquadSpawned,
            canStop: recovery || IsAssistantOwned(session),
            blockingReasonCode,
            "Squad selected.");
    }

    private static AssistantRecommendation CreateMoveToCover(
        AssistantContext context,
        TutorialSessionState session,
        TacticalCommandReasonCode blockingReasonCode,
        bool recovery = false)
    {
        string recommendationId = recovery ? M01AssistantIds.InvalidCommandRecommendationId : M01AssistantIds.MoveToCoverRecommendationId;
        return CreateRecommendation(
            session,
            recommendationId,
            M01AssistantIds.MoveStepId,
            recovery ? "Use the marked cover point" : "Move to cover",
            "Move the squad to the marked cover point before patrol contact.",
            "Cover creates a safer setup before the hostile patrol reaches civilians.",
            new[] { "Move to cover", "Use MOVE", "Patrol approaching" },
            new[] { M01AssistantIds.MoveTargetAnchorId },
            CreateIntent("show.move_to_cover", AssistantIntentKind.PreviewPathToAnchor, M01AssistantIds.MoveStepId, AssistantTargetType.TacticalAnchor, M01AssistantIds.MoveTargetAnchorId, TacticalCommandMode.Move, false, true, false, "preview"),
            CreateIntent("do.move_to_cover", AssistantIntentKind.MoveSelectedUnits, M01AssistantIds.MoveStepId, AssistantTargetType.TacticalAnchor, M01AssistantIds.MoveTargetAnchorId, TacticalCommandMode.Move, true, true, false, "one move intent"),
            canShow: context.CommandSquadSelected && context.MoveTargetAvailable,
            canExecute: context.TypedCommandHooksAvailable && context.CommandSquadSelected && context.MoveTargetAvailable,
            canStop: recovery || IsAssistantOwned(session),
            blockingReasonCode,
            "Move command accepted or squad reaches cover.");
    }

    private static AssistantRecommendation CreateAttackPatrol(
        AssistantContext context,
        TutorialSessionState session,
        TacticalCommandReasonCode blockingReasonCode,
        bool recovery = false)
    {
        string recommendationId = recovery ? M01AssistantIds.InvalidCommandRecommendationId : M01AssistantIds.AttackPatrolRecommendationId;
        return CreateRecommendation(
            session,
            recommendationId,
            M01AssistantIds.AttackStepId,
            recovery ? "Attack the valid patrol target" : "Attack hostile patrol",
            "Focus the hostile patrol before it reaches the civilian block.",
            "Destroying the patrol completes the first tactical objective.",
            new[] { "Attack patrol", "Use ATTACK", "Protect civilians" },
            new[] { M01AssistantIds.EnemyPatrolEntityId, M01AssistantIds.ObjectiveAnchorId },
            CreateIntent("show.attack_patrol", AssistantIntentKind.FocusRuntimeEntity, M01AssistantIds.AttackStepId, AssistantTargetType.RuntimeEntity, M01AssistantIds.EnemyPatrolEntityId, TacticalCommandMode.Attack, false, true, true, "preview"),
            CreateIntent("do.attack_patrol", AssistantIntentKind.AttackTarget, M01AssistantIds.AttackStepId, AssistantTargetType.RuntimeEntity, M01AssistantIds.EnemyPatrolEntityId, TacticalCommandMode.Attack, true, true, true, "one attack intent"),
            canShow: context.EnemyPatrolSpawned && context.EnemyPatrolVisible,
            canExecute: context.TypedCommandHooksAvailable && context.CommandSquadSelected && context.EnemyPatrolSpawned && context.EnemyPatrolVisible,
            canStop: recovery || IsAssistantOwned(session),
            blockingReasonCode,
            "Attack command accepted or patrol destroyed.");
    }

    private static AssistantRecommendation CreateResultExplain(AssistantContext context, TutorialSessionState session)
    {
        if (session.IsStepCompleted(M01AssistantIds.CompleteStepId) ||
            session.IsRecommendationDismissed(M01AssistantIds.ResultExplainRecommendationId))
        {
            return AssistantRecommendation.None;
        }

        return CreateRecommendation(
            session,
            M01AssistantIds.ResultExplainRecommendationId,
            M01AssistantIds.CompleteStepId,
            "Mission complete",
            "The patrol is destroyed. The result screen shows stars, rewards, and city impact.",
            "Result details explain how the first response affects the city campaign.",
            new[] { "Read result" },
            new[] { M01AssistantIds.ResultPopupId },
            CreateIntent("show.result_popup", AssistantIntentKind.FocusUiElement, M01AssistantIds.CompleteStepId, AssistantTargetType.Popup, M01AssistantIds.ResultPopupId, TacticalCommandMode.None, false, false, false, "preview"),
            AssistantIntent.None,
            canShow: context.ResultPopupVisible,
            canExecute: false,
            canStop: IsAssistantOwned(session),
            TacticalCommandReasonCode.None,
            "Result popup acknowledged.");
    }

    private static AssistantRecommendation CreateBuildLockedRecovery(AssistantContext context, TutorialSessionState session)
    {
        return CreateRecommendation(
            session,
            M01AssistantIds.InvalidCommandRecommendationId,
            ResolveCurrentStepId(context, session),
            "Building unlocks later",
            "Building unlocks in the next mission. For now, finish the patrol objective.",
            "M01 teaches selection, movement, attack, objectives, and results before build tools.",
            new[] { "Build locked", "Focus objective" },
            new[] { M01AssistantIds.ObjectiveAnchorId },
            CreateIntent("show.active_objective", AssistantIntentKind.FocusObjectiveAnchor, ResolveCurrentStepId(context, session), AssistantTargetType.ObjectiveAnchor, M01AssistantIds.ObjectiveAnchorId, TacticalCommandMode.None, false, false, false, "preview"),
            AssistantIntent.None,
            canShow: true,
            canExecute: false,
            canStop: true,
            context.LastCommandReasonCode,
            "Player returns to a valid M01 action.");
    }

    private static AssistantRecommendation CreatePlainRejectedRecovery(AssistantContext context, TutorialSessionState session)
    {
        string reasonText = string.IsNullOrWhiteSpace(context.LastCommandReasonText)
            ? TacticalCommandFeedbackText.ToDisplayText(context.LastCommandReasonCode)
            : context.LastCommandReasonText;

        return CreateRecommendation(
            session,
            M01AssistantIds.InvalidCommandRecommendationId,
            ResolveCurrentStepId(context, session),
            "Command blocked",
            string.IsNullOrWhiteSpace(reasonText) ? "That command is not available right now." : reasonText,
            "ARIA can only help with a safe replacement action when the current M01 target is known.",
            new[] { "Blocked", "Try valid target" },
            Array.Empty<string>(),
            AssistantIntent.None,
            AssistantIntent.None,
            canShow: false,
            canExecute: false,
            canStop: true,
            context.LastCommandReasonCode,
            "Player issues a valid command or dismisses recovery.");
    }

    private static AssistantRecommendation CreateRecommendation(
        TutorialSessionState session,
        string recommendationId,
        string stepId,
        string title,
        string body,
        string reason,
        string[] chips,
        string[] highlightTargets,
        AssistantIntent showMeIntent,
        AssistantIntent doItIntent,
        bool canShow,
        bool canExecute,
        bool canStop,
        TacticalCommandReasonCode blockingReasonCode,
        string completionRule)
    {
        if (session.IsRecommendationDismissed(recommendationId) ||
            recommendationId != M01AssistantIds.InvalidCommandRecommendationId && session.IsStepCompleted(stepId))
        {
            return AssistantRecommendation.None;
        }

        return new AssistantRecommendation(
            recommendationId,
            stepId,
            M01AssistantIds.MissionId,
            recommendationId == M01AssistantIds.InvalidCommandRecommendationId ? 100 : 50,
            title,
            body,
            reason,
            "Next",
            chips,
            highlightTargets,
            showMeIntent,
            doItIntent,
            canShow,
            canExecute,
            canStop,
            blockingReasonCode,
            completionRule,
            suppressAfterCompletion: true);
    }

    private static AssistantRecommendation Activate(TutorialSessionState session, AssistantRecommendation recommendation)
    {
        session.SetActiveRecommendation(recommendation.RecommendationId);
        return recommendation;
    }

    private static AssistantIntent CreateIntent(
        string intentId,
        AssistantIntentKind kind,
        string stepId,
        AssistantTargetType targetType,
        string targetId,
        TacticalCommandMode commandMode,
        bool canExecuteGameplay,
        bool requiresSelectedEntity,
        bool requiresVisibleTarget,
        string completionBoundary)
    {
        return new AssistantIntent(
            intentId,
            kind,
            M01AssistantIds.MissionId,
            stepId,
            targetType,
            targetId,
            commandMode,
            canExecuteGameplay,
            requiresSelectedEntity,
            requiresVisibleTarget,
            completionBoundary);
    }

    private static string ResolveCurrentStepId(AssistantContext context, TutorialSessionState session)
    {
        if (!session.IsStepCompleted(M01AssistantIds.SelectSquadStepId) || !context.CommandSquadSelected)
            return M01AssistantIds.SelectSquadStepId;
        if (!session.IsStepCompleted(M01AssistantIds.MoveStepId))
            return M01AssistantIds.MoveStepId;
        if (!session.IsStepCompleted(M01AssistantIds.AttackStepId))
            return M01AssistantIds.AttackStepId;
        return M01AssistantIds.ObjectivesStepId;
    }

    private static bool IsAssistantOwned(TutorialSessionState session)
    {
        return !string.IsNullOrEmpty(session.ActivePreviewIntentId) ||
            !string.IsNullOrEmpty(session.ActiveTakeoverIntentId);
    }
}

using UnityEngine;

public readonly struct BattleHudRuntimeFeedbackState
{
    public BattleHudRuntimeFeedbackState(
        TacticalCommandMode currentCommandMode,
        TacticalCommandMode stickyCommandMode,
        TacticalCommandResult lastCommandResult,
        bool hasLastCommandResult)
    {
        CurrentCommandMode = currentCommandMode;
        StickyCommandMode = stickyCommandMode;
        LastCommandResult = lastCommandResult;
        HasLastCommandResult = hasLastCommandResult;
    }

    public TacticalCommandMode CurrentCommandMode { get; }
    public TacticalCommandMode StickyCommandMode { get; }
    public TacticalCommandResult LastCommandResult { get; }
    public bool HasLastCommandResult { get; }

    public static BattleHudRuntimeFeedbackState Empty =>
        new(TacticalCommandMode.None, TacticalCommandMode.None, TacticalCommandResult.Success(), false);
}

public sealed class BattleHudRuntimeFeedbackSystem
{
    private static readonly MatchOverlayCommandTabFeedbackSystem CommandTabFeedbackSystem = new();

    public static BattleHudRuntimeFeedbackState GetState(BattleHudRuntimeFeedbackView view)
    {
        return view != null ? view.RuntimeFeedbackState : BattleHudRuntimeFeedbackState.Empty;
    }

    public static void ApplySelection(BattleHudRuntimeFeedbackView view, string displayName, string status)
    {
        BattleHudTacticalFeedbackView feedback = ResolveTacticalFeedback(view);
        if (feedback == null)
            return;

        if (string.IsNullOrWhiteSpace(displayName))
            feedback.HideSelectedEntity();
        else
            feedback.ShowSelectedEntity(displayName, status);
    }

    public static void ClearSelection(BattleHudRuntimeFeedbackView view)
    {
        ResolveTacticalFeedback(view)?.HideSelectedEntity();
    }

    public static void ApplyCommandMode(BattleHudRuntimeFeedbackView view, TacticalCommandMode mode)
    {
        if (view == null)
            return;

        view.CurrentCommandMode = mode;
        ApplyCommandModeVisuals(view, mode);
    }

    public static void ApplyStickyCommandMode(BattleHudRuntimeFeedbackView view, TacticalCommandMode mode)
    {
        if (view == null)
            return;

        view.StickyCommandMode = mode;
        view.CurrentCommandMode = mode;
        ApplyCommandModeVisuals(view, mode);
    }

    public static void ClearStickyCommandMode(BattleHudRuntimeFeedbackView view, TacticalCommandMode mode)
    {
        if (view == null)
            return;

        if (view.StickyCommandMode != mode)
            return;

        view.StickyCommandMode = TacticalCommandMode.None;
        ClearCommandModeInternal(view);
    }

    public static void ClearCommandMode(BattleHudRuntimeFeedbackView view)
    {
        if (view == null)
            return;

        if (view.StickyCommandMode != TacticalCommandMode.None)
        {
            view.CurrentCommandMode = view.StickyCommandMode;
            ApplyCommandModeVisuals(view, view.StickyCommandMode);
            return;
        }

        ClearCommandModeInternal(view);
    }

    public static void ApplyCommandResult(BattleHudRuntimeFeedbackView view, TacticalCommandResult result)
    {
        if (view == null)
            return;

        view.LastCommandResult = result;
        view.HasLastCommandResult = true;

        if (result.Accepted)
        {
            ResolveTacticalFeedback(view)?.HideInvalidCommand();
            MatchHudCommandFeedbackModel feedbackModel = BuildCommandResultFeedback(result, view.RuntimeFeedbackState);
            if (!feedbackModel.Visible)
                view.HideFeedbackMessage();
            else
                view.ApplyCommandFeedback(feedbackModel);
            return;
        }

        string reason = !string.IsNullOrWhiteSpace(result.Message)
            ? result.Message
            : TacticalCommandFeedbackText.ToDisplayText(result.ReasonCode);
        ResolveTacticalFeedback(view)?.ShowInvalidCommand(reason);
        view.ApplyCommandFeedback(MatchHudCommandFeedbackModel.Show(reason, CommandFeedbackSeverity.Error));
    }

    public static void SetWorldMarkersVisible(BattleHudRuntimeFeedbackView view, bool visible)
    {
        // The HUD marker layer is a static art-preview surface. Live targeting feedback
        // must come from grounded runtime markers so fixed screen-space art cannot cover units.
        ResolveTacticalFeedback(view)?.SetWorldMarkersVisible(false);
    }

    private static void ApplyCommandModeVisuals(BattleHudRuntimeFeedbackView view, TacticalCommandMode mode)
    {
        if (mode == TacticalCommandMode.None)
            CommandTabFeedbackSystem.ClearCommandMode(view.CommandTabGroups);
        else
            CommandTabFeedbackSystem.ApplyCommandMode(view.CommandTabGroups, mode);

        BattleHudTacticalFeedbackView feedback = ResolveTacticalFeedback(view);
        MatchHudCommandFeedbackModel commandFeedback = BuildCommandModeFeedback(mode);
        if (!commandFeedback.Visible)
            view.HideFeedbackMessage();
        else
            view.ApplyCommandFeedback(commandFeedback);

        if (feedback == null)
            return;

        string displayText = TacticalCommandFeedbackText.ToDisplayText(mode);
        if (string.IsNullOrEmpty(displayText))
            feedback.HideCommandMode();
        else
            feedback.ShowCommandMode(displayText);
    }

    private static void ClearCommandModeInternal(BattleHudRuntimeFeedbackView view)
    {
        view.CurrentCommandMode = TacticalCommandMode.None;
        view.HideFeedbackMessage();
        ResolveTacticalFeedback(view)?.HideCommandMode();
        CommandTabFeedbackSystem.ClearCommandMode(view.CommandTabGroups);
    }

    private static MatchHudCommandFeedbackModel BuildCommandModeFeedback(TacticalCommandMode mode)
    {
        string instruction = TacticalCommandFeedbackText.ToInstructionText(mode);
        return MatchHudCommandFeedbackModel.Show(
            instruction,
            TacticalCommandFeedbackText.ToInstructionSeverity(mode));
    }

    private static MatchHudCommandFeedbackModel BuildCommandResultFeedback(TacticalCommandResult result, BattleHudRuntimeFeedbackState state)
    {
        if (!result.Accepted)
        {
            string reason = !string.IsNullOrWhiteSpace(result.Message)
                ? result.Message
                : TacticalCommandFeedbackText.ToDisplayText(result.ReasonCode);
            return MatchHudCommandFeedbackModel.Show(reason, CommandFeedbackSeverity.Error);
        }

        if (string.IsNullOrWhiteSpace(result.Message))
            return MatchHudCommandFeedbackModel.Hidden;

        TacticalCommandMode mode = state.CurrentCommandMode != TacticalCommandMode.None
            ? state.CurrentCommandMode
            : state.StickyCommandMode;
        return MatchHudCommandFeedbackModel.Show(result.Message, ResolveAcceptedResultSeverity(result.Message, mode));
    }

    private static CommandFeedbackSeverity ResolveAcceptedResultSeverity(string message, TacticalCommandMode mode)
    {
        string normalized = message?.ToUpperInvariant() ?? string.Empty;
        if (normalized.Contains("CANCEL") ||
            normalized.Contains("CLEARED") ||
            normalized.Contains("DESTROY") ||
            normalized.Contains("STOP"))
        {
            return CommandFeedbackSeverity.Warning;
        }

        return mode switch
        {
            TacticalCommandMode.Select or
            TacticalCommandMode.Special => CommandFeedbackSeverity.Neutral,
            _ => CommandFeedbackSeverity.Ready
        };
    }

    private static BattleHudTacticalFeedbackView ResolveTacticalFeedback(BattleHudRuntimeFeedbackView view)
    {
        if (view == null)
            return null;

        return view.TacticalFeedback != null
            ? view.TacticalFeedback
            : view.GetComponent<BattleHudTacticalFeedbackView>();
    }

}

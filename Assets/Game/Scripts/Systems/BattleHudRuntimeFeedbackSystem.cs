using System.Collections.Generic;
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
    private sealed class MutableState
    {
        public TacticalCommandMode CurrentCommandMode = TacticalCommandMode.None;
        public TacticalCommandMode StickyCommandMode = TacticalCommandMode.None;
        public TacticalCommandResult LastCommandResult = TacticalCommandResult.Success();
        public bool HasLastCommandResult;

        public BattleHudRuntimeFeedbackState Snapshot()
        {
            return new BattleHudRuntimeFeedbackState(
                CurrentCommandMode,
                StickyCommandMode,
                LastCommandResult,
                HasLastCommandResult);
        }
    }

    private static readonly Dictionary<BattleHudRuntimeFeedbackView, MutableState> StatesByView = new();
    private static readonly MatchOverlayCommandTabFeedbackSystem CommandTabFeedbackSystem = new();
    private static BattleHudRuntimeFeedbackView ActiveView;

    public static void SetActiveView(BattleHudRuntimeFeedbackView view)
    {
        if (view == null)
            return;

        ActiveView = view;
        ResolveState(view);
    }

    public static void ClearActiveView(BattleHudRuntimeFeedbackView view)
    {
        if (view == null)
            return;

        StatesByView.Remove(view);
        if (ReferenceEquals(ActiveView, view))
            ActiveView = null;
    }

    public static BattleHudRuntimeFeedbackView ResolveActiveView()
    {
        if (IsValidView(ActiveView, requireActiveInHierarchy: true))
            return ActiveView;

        return IsValidView(ActiveView, requireActiveInHierarchy: false) ? ActiveView : null;
    }

    public static BattleHudRuntimeFeedbackState GetState(BattleHudRuntimeFeedbackView view = null)
    {
        view ??= ResolveActiveView();
        return view != null && StatesByView.TryGetValue(view, out MutableState state)
            ? state.Snapshot()
            : BattleHudRuntimeFeedbackState.Empty;
    }

    public static void ApplySelection(string displayName, string status)
    {
        ApplySelection(ResolveActiveView(), displayName, status);
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

    public static void ClearSelection(BattleHudRuntimeFeedbackView view = null)
    {
        ResolveTacticalFeedback(view ?? ResolveActiveView())?.HideSelectedEntity();
    }

    public static void ApplyCommandMode(TacticalCommandMode mode)
    {
        ApplyCommandMode(ResolveActiveView(), mode);
    }

    public static void ApplyCommandMode(BattleHudRuntimeFeedbackView view, TacticalCommandMode mode)
    {
        if (view == null)
            return;

        MutableState state = ResolveState(view);
        state.CurrentCommandMode = mode;
        ApplyCommandModeVisuals(view, mode);
    }

    public static void ApplyStickyCommandMode(TacticalCommandMode mode)
    {
        ApplyStickyCommandMode(ResolveActiveView(), mode);
    }

    public static void ApplyStickyCommandMode(BattleHudRuntimeFeedbackView view, TacticalCommandMode mode)
    {
        if (view == null)
            return;

        MutableState state = ResolveState(view);
        state.StickyCommandMode = mode;
        state.CurrentCommandMode = mode;
        ApplyCommandModeVisuals(view, mode);
    }

    public static void ClearStickyCommandMode(TacticalCommandMode mode)
    {
        ClearStickyCommandMode(ResolveActiveView(), mode);
    }

    public static void ClearStickyCommandMode(BattleHudRuntimeFeedbackView view, TacticalCommandMode mode)
    {
        if (view == null)
            return;

        MutableState state = ResolveState(view);
        if (state.StickyCommandMode != mode)
            return;

        state.StickyCommandMode = TacticalCommandMode.None;
        ClearCommandModeInternal(view, state);
    }

    public static void ClearCommandMode()
    {
        ClearCommandMode(ResolveActiveView());
    }

    public static void ClearCommandMode(BattleHudRuntimeFeedbackView view)
    {
        if (view == null)
            return;

        MutableState state = ResolveState(view);
        if (state.StickyCommandMode != TacticalCommandMode.None)
        {
            state.CurrentCommandMode = state.StickyCommandMode;
            ApplyCommandModeVisuals(view, state.StickyCommandMode);
            return;
        }

        ClearCommandModeInternal(view, state);
    }

    public static void ApplyCommandResult(TacticalCommandResult result)
    {
        ApplyCommandResult(ResolveActiveView(), result);
    }

    public static void ApplyCommandResult(BattleHudRuntimeFeedbackView view, TacticalCommandResult result)
    {
        if (view == null)
            return;

        MutableState state = ResolveState(view);
        state.LastCommandResult = result;
        state.HasLastCommandResult = true;

        if (result.Accepted)
        {
            ResolveTacticalFeedback(view)?.HideInvalidCommand();
            MatchHudCommandFeedbackModel feedbackModel = BuildCommandResultFeedback(result, state);
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
        ResolveTacticalFeedback(view ?? ResolveActiveView())?.SetWorldMarkersVisible(false);
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

    private static void ClearCommandModeInternal(BattleHudRuntimeFeedbackView view, MutableState state)
    {
        state.CurrentCommandMode = TacticalCommandMode.None;
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

    private static MatchHudCommandFeedbackModel BuildCommandResultFeedback(TacticalCommandResult result, MutableState state)
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

    private static bool IsValidView(BattleHudRuntimeFeedbackView view, bool requireActiveInHierarchy)
    {
        return view != null &&
            view.gameObject != null &&
            view.gameObject.scene.IsValid() &&
            (!requireActiveInHierarchy || view.gameObject.activeInHierarchy);
    }

    private static MutableState ResolveState(BattleHudRuntimeFeedbackView view)
    {
        if (!StatesByView.TryGetValue(view, out MutableState state))
        {
            state = new MutableState();
            StatesByView.Add(view, state);
        }

        return state;
    }
}

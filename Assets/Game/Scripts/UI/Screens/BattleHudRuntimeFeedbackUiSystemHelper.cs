using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public static class BattleHudRuntimeFeedbackUiSystemHelper
    {
        public const float SuccessFeedbackDurationSeconds = 1.4f;
        public const float ErrorFeedbackDurationSeconds = 2.25f;

        public static BattleHudRuntimeFeedbackState GetState(IBattleHudRuntimeFeedbackView view)
        {
            return view != null ? view.RuntimeFeedbackState : BattleHudRuntimeFeedbackState.Empty;
        }

        public static void ApplySelection(IBattleHudRuntimeFeedbackView view, string displayName, string status)
        {
            if (view == null)
                return;

            if (string.IsNullOrWhiteSpace(displayName))
                view.HideSelectedEntity();
            else
                view.ShowSelectedEntity(displayName, status);
        }

        public static void ClearSelection(IBattleHudRuntimeFeedbackView view)
        {
            view?.HideSelectedEntity();
        }

        public static void ApplyCommandMode(IBattleHudRuntimeFeedbackView view, TacticalCommandMode mode)
        {
            if (view == null)
                return;

            view.CurrentCommandMode = mode;
            ApplyCommandModeVisuals(view, mode);
        }

        public static void ApplyBoardCommandMode(
            IBattleHudRuntimeFeedbackView view,
            UiBoardCommandModeDirection direction,
            bool boardAllInteractable)
        {
            if (view == null)
                return;

            view.CurrentCommandMode = TacticalCommandMode.Board;
            view.ApplyCommandModeTabs(TacticalCommandMode.Board);
            view.ApplyCurrentOrderBanner(MatchHudCurrentOrderBannerUiSystemHelper.BuildBoardCommandModeBanner(
                direction,
                view.ResolveCommandIconSprite(TacticalCommandMode.Board)));

            MatchHudCommandFeedbackModel commandFeedback = direction == UiBoardCommandModeDirection.TransportToPassenger
                ? MatchHudCommandFeedbackModel.Show(GameText.Get("tactical.command.board.prompt_transport_to_passenger", "Select units to board or use BOARD ALL."), CommandFeedbackSeverity.Ready)
                : MatchHudCommandFeedbackModel.Show(GameText.Get("tactical.command.board.prompt_passenger_to_transport", "Select a transport."), CommandFeedbackSeverity.Ready);
            MatchHudCommandFeedbackActionsModel actions = direction == UiBoardCommandModeDirection.TransportToPassenger
                ? MatchHudCommandFeedbackActionsModel.BoardPassengerSelection(boardAllInteractable)
                : MatchHudCommandFeedbackActionsModel.CancelOnly;
            view.ApplyPersistentCommandFeedback(commandFeedback, actions);

            view.ShowCommandMode(ResolveCommandModeText(TacticalCommandMode.Board));
        }

        public static void ApplyStickyCommandMode(IBattleHudRuntimeFeedbackView view, TacticalCommandMode mode)
        {
            if (view == null)
                return;

            view.StickyCommandMode = mode;
            view.CurrentCommandMode = mode;
            ApplyCommandModeVisuals(view, mode);
        }

        public static void ClearStickyCommandMode(IBattleHudRuntimeFeedbackView view, TacticalCommandMode mode)
        {
            if (view == null)
                return;

            if (view.StickyCommandMode != mode)
                return;

            view.StickyCommandMode = TacticalCommandMode.None;
            ClearCommandModeInternal(view);
        }

        public static void ClearCommandMode(IBattleHudRuntimeFeedbackView view)
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

        public static void ApplyCommandResult(IBattleHudRuntimeFeedbackView view, TacticalCommandResult result)
        {
            if (view == null)
                return;

            view.LastCommandResult = result;
            view.HasLastCommandResult = true;

            if (result.Accepted)
            {
                view.HideInvalidCommand();
                TacticalCommandMode bannerMode = MatchHudCurrentOrderBannerUiSystemHelper.ResolveAcceptedResultCommandMode(
                    result,
                    view.RuntimeFeedbackState);
                if (bannerMode != TacticalCommandMode.None)
                {
                    view.ApplyTransientCurrentOrderBanner(
                        MatchHudCurrentOrderBannerUiSystemHelper.BuildAcceptedResultBanner(
                            result,
                            bannerMode,
                            view.ResolveCommandIconSprite(bannerMode)),
                        Time.unscaledTime,
                        SuccessFeedbackDurationSeconds);
                }

                MatchHudCommandFeedbackModel feedbackModel = BuildCommandResultFeedback(result, view.RuntimeFeedbackState);
                if (feedbackModel.Visible)
                    view.ApplyTransientCommandFeedback(feedbackModel, Time.unscaledTime);
                return;
            }

            string reason = !string.IsNullOrWhiteSpace(result.Message)
                ? result.Message
                : ResolveCommandReasonText(result.ReasonCode);
            view.ShowInvalidCommand(reason);
            view.ApplyTransientCommandFeedback(
                MatchHudCommandFeedbackModel.ShowTransient(reason, CommandFeedbackSeverity.Error, ErrorFeedbackDurationSeconds),
                Time.unscaledTime);
        }

        public static void TickFeedbackLifetime(IBattleHudRuntimeFeedbackView view, float now)
        {
            view?.TickFeedbackLifetime(now);
        }

        public static void SetWorldMarkersVisible(IBattleHudRuntimeFeedbackView view, bool visible)
        {
            // The HUD marker layer is a static art-preview surface. Live targeting feedback
            // must come from grounded runtime markers so fixed screen-space art cannot cover units.
            view?.SetWorldMarkersVisible(false);
        }

        private static void ApplyCommandModeVisuals(IBattleHudRuntimeFeedbackView view, TacticalCommandMode mode)
        {
            if (mode == TacticalCommandMode.None)
            {
                view.ClearCommandModeTabs();
                view.HideCurrentOrderBanner();
            }
            else
            {
                view.ApplyCommandModeTabs(mode);
                view.ApplyCurrentOrderBanner(MatchHudCurrentOrderBannerUiSystemHelper.BuildCommandModeBanner(
                    mode,
                    view.ResolveCommandIconSprite(mode)));
            }

            MatchHudCommandFeedbackModel commandFeedback = BuildCommandModeFeedback(mode);
            if (!commandFeedback.Visible)
                view.HideFeedbackMessage();
            else
                view.ApplyPersistentCommandFeedback(commandFeedback, MatchHudCommandFeedbackActionsModel.Hidden);

            string displayText = ResolveCommandModeText(mode);
            if (string.IsNullOrEmpty(displayText))
                view.HideCommandMode();
            else
                view.ShowCommandMode(displayText);
        }

        private static void ClearCommandModeInternal(IBattleHudRuntimeFeedbackView view)
        {
            view.CurrentCommandMode = TacticalCommandMode.None;
            view.ClearPersistentCommandFeedback();
            view.HideFeedbackMessage();
            view.HideCommandMode();
            view.HideCurrentOrderBanner();
            view.ClearCommandModeTabs();
        }

        private static MatchHudCommandFeedbackModel BuildCommandModeFeedback(TacticalCommandMode mode)
        {
            string instruction = ResolveCommandInstructionText(mode);
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
                    : ResolveCommandReasonText(result.ReasonCode);
                return MatchHudCommandFeedbackModel.ShowTransient(reason, CommandFeedbackSeverity.Error, ErrorFeedbackDurationSeconds);
            }

            if (string.IsNullOrWhiteSpace(result.Message))
                return MatchHudCommandFeedbackModel.Hidden;

            TacticalCommandMode mode = state.CurrentCommandMode != TacticalCommandMode.None
                ? state.CurrentCommandMode
                : state.StickyCommandMode;
            CommandFeedbackSeverity severity = ResolveAcceptedResultSeverity(result.Message, mode);
            return MatchHudCommandFeedbackModel.ShowTransient(
                result.Message,
                severity,
                ResolveResultDuration(severity));
        }

        private static string ResolveCommandModeText(TacticalCommandMode mode)
        {
            return GameText.Get(
                TacticalCommandFeedbackText.ToDisplayTextKey(mode),
                TacticalCommandFeedbackText.ToDisplayText(mode));
        }

        private static string ResolveCommandReasonText(TacticalCommandReasonCode reasonCode)
        {
            return GameText.Get(
                TacticalCommandFeedbackText.ToDisplayTextKey(reasonCode),
                TacticalCommandFeedbackText.ToDisplayText(reasonCode));
        }

        private static string ResolveCommandInstructionText(TacticalCommandMode mode)
        {
            return GameText.Get(
                TacticalCommandFeedbackText.ToInstructionTextKey(mode),
                TacticalCommandFeedbackText.ToInstructionText(mode));
        }

        private static float ResolveResultDuration(CommandFeedbackSeverity severity)
        {
            return severity == CommandFeedbackSeverity.Warning || severity == CommandFeedbackSeverity.Error
                ? ErrorFeedbackDurationSeconds
                : SuccessFeedbackDurationSeconds;
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

    }
}

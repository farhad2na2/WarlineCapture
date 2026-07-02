using UnityEngine;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public static class MatchHudCurrentOrderBannerUiSystemHelper
    {
        public static MatchHudCurrentOrderBannerModel BuildCommandModeBanner(
            TacticalCommandMode commandMode,
            Sprite iconSprite)
        {
            return commandMode switch
            {
                TacticalCommandMode.Move => Build(commandMode, "MOVE ORDER", "Select a destination.", iconSprite),
                TacticalCommandMode.Attack => Build(commandMode, "ATTACK ORDER", "Select an enemy target.", iconSprite),
                TacticalCommandMode.Scan => Build(commandMode, "SCAN ORDER", "Select an area to scan.", iconSprite),
                TacticalCommandMode.Board => Build(commandMode, "BOARD ORDER", "Select a transport.", iconSprite),
                TacticalCommandMode.Build => Build(commandMode, "BUILD ORDER", "Place structure on valid terrain.", iconSprite),
                _ => MatchHudCurrentOrderBannerModel.Hidden
            };
        }

        public static MatchHudCurrentOrderBannerModel BuildBoardCommandModeBanner(
            UiBoardCommandModeDirection direction,
            Sprite iconSprite)
        {
            string description = direction == UiBoardCommandModeDirection.TransportToPassenger
                ? "Select units to board."
                : "Select a transport.";
            return Build(TacticalCommandMode.Board, "BOARD ORDER", description, iconSprite);
        }

        public static TacticalCommandMode ResolveAcceptedResultCommandMode(
            TacticalCommandResult result,
            BattleHudRuntimeFeedbackState state)
        {
            if (!result.Accepted)
                return TacticalCommandMode.None;

            TacticalCommandMode activeMode = state.CurrentCommandMode != TacticalCommandMode.None
                ? state.CurrentCommandMode
                : state.StickyCommandMode;
            if (CanShowBanner(activeMode))
                return activeMode;

            string normalized = result.Message?.ToUpperInvariant() ?? string.Empty;
            if (normalized.Contains("HOLD"))
                return TacticalCommandMode.Hold;
            if (normalized.Contains("STOP") || normalized.Contains("CLEAR"))
                return TacticalCommandMode.Stop;
            if (normalized.Contains("SCAN"))
                return TacticalCommandMode.Scan;
            if (normalized.Contains("BOARD"))
                return TacticalCommandMode.Board;
            if (normalized.Contains("BUILD") || normalized.Contains("PLACE"))
                return TacticalCommandMode.Build;
            if (normalized.Contains("RETURN") || normalized.Contains("DESTROY"))
                return TacticalCommandMode.Special;

            return TacticalCommandMode.None;
        }

        public static MatchHudCurrentOrderBannerModel BuildAcceptedResultBanner(
            TacticalCommandResult result,
            TacticalCommandMode commandMode,
            Sprite iconSprite)
        {
            if (!result.Accepted || commandMode == TacticalCommandMode.None)
                return MatchHudCurrentOrderBannerModel.Hidden;

            return commandMode switch
            {
                TacticalCommandMode.Move => Build(commandMode, "MOVE ORDER", "Units moving to target.", iconSprite),
                TacticalCommandMode.Attack => Build(commandMode, "ATTACK ORDER", "Engaging target.", iconSprite),
                TacticalCommandMode.Hold => Build(commandMode, "HOLD POSITION", "Selected units holding ground.", iconSprite),
                TacticalCommandMode.Stop => Build(commandMode, "STOP ORDER", "Selected units clearing orders.", iconSprite),
                TacticalCommandMode.Scan => Build(commandMode, "SCAN ORDER", "Recon sweep in progress.", iconSprite),
                TacticalCommandMode.Board => Build(commandMode, "BOARD ORDER", "Boarding transport.", iconSprite),
                TacticalCommandMode.Build => Build(commandMode, "BUILD ORDER", "Building order accepted.", iconSprite),
                TacticalCommandMode.Special => BuildSpecialAcceptedResult(result, iconSprite),
                _ => MatchHudCurrentOrderBannerModel.Hidden
            };
        }

        private static bool CanShowBanner(TacticalCommandMode commandMode)
        {
            return commandMode is TacticalCommandMode.Move or
                TacticalCommandMode.Attack or
                TacticalCommandMode.Scan or
                TacticalCommandMode.Board or
                TacticalCommandMode.Build;
        }

        private static MatchHudCurrentOrderBannerModel BuildSpecialAcceptedResult(TacticalCommandResult result, Sprite iconSprite)
        {
            string normalized = result.Message?.ToUpperInvariant() ?? string.Empty;
            if (normalized.Contains("RETURN"))
                return Build(TacticalCommandMode.Special, "RETURN ORDER", "Unit returning to base.", iconSprite);
            if (normalized.Contains("DESTROY"))
                return Build(TacticalCommandMode.Special, "DESTROY ORDER", "Selected unit removed.", iconSprite);

            return MatchHudCurrentOrderBannerModel.Hidden;
        }

        private static MatchHudCurrentOrderBannerModel Build(
            TacticalCommandMode commandMode,
            string orderText,
            string descriptionText,
            Sprite iconSprite)
        {
            return new MatchHudCurrentOrderBannerModel(
                true,
                commandMode,
                orderText,
                descriptionText,
                iconSprite);
        }
    }
}

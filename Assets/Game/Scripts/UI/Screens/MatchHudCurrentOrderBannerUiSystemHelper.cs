using UnityEngine;
using Game.Configs;
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
                TacticalCommandMode.Move => Build(commandMode, Text("tactical.banner.mode.move.title", "MOVE ORDER"), Text("tactical.banner.mode.move.description", "Select a destination."), iconSprite),
                TacticalCommandMode.Attack => Build(commandMode, Text("tactical.banner.mode.attack.title", "ATTACK ORDER"), Text("tactical.banner.mode.attack.description", "Select an enemy target."), iconSprite),
                TacticalCommandMode.Scan => Build(commandMode, Text("tactical.banner.mode.scan.title", "SCAN ORDER"), Text("tactical.banner.mode.scan.description", "Select an area to scan."), iconSprite),
                TacticalCommandMode.Board => Build(commandMode, Text("tactical.banner.mode.board.title", "BOARD ORDER"), Text("tactical.banner.mode.board.description", "Select a transport."), iconSprite),
                TacticalCommandMode.Build => Build(commandMode, Text("tactical.banner.mode.build.title", "BUILD ORDER"), Text("tactical.banner.mode.build.description", "Place structure on valid terrain."), iconSprite),
                _ => MatchHudCurrentOrderBannerModel.Hidden
            };
        }

        public static MatchHudCurrentOrderBannerModel BuildBoardCommandModeBanner(
            UiBoardCommandModeDirection direction,
            Sprite iconSprite)
        {
            string description = direction == UiBoardCommandModeDirection.TransportToPassenger
                ? Text("tactical.banner.mode.board.description_transport_to_passenger", "Select units to board.")
                : Text("tactical.banner.mode.board.description", "Select a transport.");
            return Build(TacticalCommandMode.Board, Text("tactical.banner.mode.board.title", "BOARD ORDER"), description, iconSprite);
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
                TacticalCommandMode.Move => Build(commandMode, Text("tactical.banner.accepted.move.title", "MOVE ORDER"), Text("tactical.banner.accepted.move.description", "Units moving to target."), iconSprite),
                TacticalCommandMode.Attack => Build(commandMode, Text("tactical.banner.accepted.attack.title", "ATTACK ORDER"), Text("tactical.banner.accepted.attack.description", "Engaging target."), iconSprite),
                TacticalCommandMode.Hold => Build(commandMode, Text("tactical.banner.accepted.hold.title", "HOLD POSITION"), Text("tactical.banner.accepted.hold.description", "Selected units holding ground."), iconSprite),
                TacticalCommandMode.Stop => Build(commandMode, Text("tactical.banner.accepted.stop.title", "STOP ORDER"), Text("tactical.banner.accepted.stop.description", "Selected units clearing orders."), iconSprite),
                TacticalCommandMode.Scan => Build(commandMode, Text("tactical.banner.accepted.scan.title", "SCAN ORDER"), Text("tactical.banner.accepted.scan.description", "Recon sweep in progress."), iconSprite),
                TacticalCommandMode.Board => Build(commandMode, Text("tactical.banner.accepted.board.title", "BOARD ORDER"), Text("tactical.banner.accepted.board.description", "Boarding transport."), iconSprite),
                TacticalCommandMode.Build => Build(commandMode, Text("tactical.banner.accepted.build.title", "BUILD ORDER"), Text("tactical.banner.accepted.build.description", "Building order accepted."), iconSprite),
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
                return Build(TacticalCommandMode.Special, Text("tactical.banner.accepted.return.title", "RETURN ORDER"), Text("tactical.banner.accepted.return.description", "Unit returning to base."), iconSprite);
            if (normalized.Contains("DESTROY"))
                return Build(TacticalCommandMode.Special, Text("tactical.banner.accepted.destroy.title", "DESTROY ORDER"), Text("tactical.banner.accepted.destroy.description", "Selected unit removed."), iconSprite);

            return MatchHudCurrentOrderBannerModel.Hidden;
        }

        private static string Text(string key, string fallback)
        {
            return GameText.Get(key, fallback);
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

using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static UiMissionResultPopupModel LocalizeMarketLifelineResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=facts.MarketFailure switch {MarketLifelineFailure.SquadLost=>"squad",MarketLifelineFailure.ConvoyLost=>"convoy",MarketLifelineFailure.Deadline=>"deadline",_=>"integrity"};
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,GameText.Get("mission.market_lifeline.result."+(victory?"victory":"defeat")),GameText.Get("mission.market_lifeline.name"),
                GameText.Get(victory?"mission.market_lifeline.result.success":"mission.market_lifeline.failure."+reason),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired);
        }
    }
}

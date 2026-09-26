using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static UiMissionResultPopupModel LocalizeRouteReopenedResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=facts.RouteReopenedFailure switch {RouteReopenedFailure.SquadLost=>"squad",RouteReopenedFailure.EngineerLost=>"engineer",RouteReopenedFailure.ReliefConvoyLost=>"relief",RouteReopenedFailure.FuelConvoyLost=>"fuel",RouteReopenedFailure.RecordsLost=>"records",RouteReopenedFailure.Deadline=>"deadline",_=>"integrity"};
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,GameText.Get("mission.route_reopened.result."+(victory?"victory":"defeat")),GameText.Get("mission.route_reopened.name"),
                GameText.Get(victory?"mission.route_reopened.result.success":"mission.route_reopened.failure."+reason),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired);
        }
    }
}

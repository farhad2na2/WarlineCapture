using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static UiMissionResultPopupModel LocalizePowerRelayResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=facts.PowerRelayFailure switch {PowerRelayFailure.SquadLost=>"squad",PowerRelayFailure.EngineerLost=>"engineer",PowerRelayFailure.FamilyConvoyLost=>"families",PowerRelayFailure.FuelServiceLost=>"fuel",PowerRelayFailure.Deadline=>"deadline",_=>"integrity"};
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,GameText.Get("mission.power_relay.result."+(victory?"victory":"defeat")),GameText.Get("mission.power_relay.name"),
                GameText.Get(victory?"mission.power_relay.result.success":"mission.power_relay.failure."+reason),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired);
        }
    }
}

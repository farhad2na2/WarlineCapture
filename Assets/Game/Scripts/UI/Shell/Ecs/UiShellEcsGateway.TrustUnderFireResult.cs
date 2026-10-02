using Game.Components;
using Game.Configs;
using Game.UI.Contracts;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static UiMissionResultPopupModel LocalizeTrustUnderFireResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=facts.TrustFailure switch
            {TrustUnderFireFailure.NorthConvoyLost=>"north_lost",TrustUnderFireFailure.SouthConvoyLost=>"south_lost",TrustUnderFireFailure.ShelterLost=>"shelter_lost",TrustUnderFireFailure.StaffLost=>"staff_lost",TrustUnderFireFailure.EngineerLost=>"engineer_lost",TrustUnderFireFailure.EscortLost=>"escort_lost",TrustUnderFireFailure.FuelReserveLost=>"fuel_lost",TrustUnderFireFailure.BroadcastLost=>"broadcast_lost",TrustUnderFireFailure.Deadline=>"timeout",_=>"loss"};
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,
                GameText.Get("mission.trust_under_fire.result."+(victory?"victory":"defeat")),GameText.Get("mission.trust_under_fire.result.subtitle"),
                GameText.Get("mission.trust_under_fire.result."+(victory?"success":reason)),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),
                model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired,
                new UiMissionDefenseResultDetails(facts.CivilianLossCount,false,false,false,facts.HostileRosterIntegrityFault!=0,false), civilianLossCount:facts.CivilianLossCount);
        }
    }
}

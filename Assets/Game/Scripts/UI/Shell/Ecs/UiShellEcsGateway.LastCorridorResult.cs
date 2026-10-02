using Game.Components;
using Game.Configs;
using Game.UI.Contracts;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static UiMissionResultPopupModel LocalizeLastCorridorResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=facts.CorridorFailure switch
            {LastCorridorFailure.EngineerLost=>"engineer_lost",LastCorridorFailure.KeysLost=>"keys_lost",LastCorridorFailure.MedicineLost=>"medicine_lost",LastCorridorFailure.FuelCargoLost=>"fuel_lost",LastCorridorFailure.ReinforcementsLost=>"reinforcements_lost",LastCorridorFailure.CivicLost=>"civic_lost",LastCorridorFailure.StaffLost=>"staff_lost",LastCorridorFailure.EscortLost=>"escort_lost",LastCorridorFailure.FuelReserveLost=>"fuel_lost",LastCorridorFailure.ReceiverLost=>"receiver_lost",LastCorridorFailure.Deadline=>"timeout",_=>"loss"};
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,
                GameText.Get("mission.last_corridor.result."+(victory?"victory":"defeat")),GameText.Get("mission.last_corridor.result.subtitle"),
                GameText.Get("mission.last_corridor.result."+(victory?"success":reason)),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),
                model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired,
                new UiMissionDefenseResultDetails(facts.CivilianLossCount,false,false,false,facts.HostileRosterIntegrityFault!=0,false), civilianLossCount:facts.CivilianLossCount);
        }
    }
}

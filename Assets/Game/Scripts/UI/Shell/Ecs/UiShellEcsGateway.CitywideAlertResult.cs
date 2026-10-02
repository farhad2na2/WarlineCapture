using Game.Components;
using Game.Configs;
using Game.UI.Contracts;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static UiMissionResultPopupModel LocalizeCitywideAlertResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=facts.CitywideFailure switch
            {CitywideAlertFailure.ClinicLost=>"clinic_lost",CitywideAlertFailure.UtilityLost=>"utility_lost",CitywideAlertFailure.StaffLost=>"staff_lost",CitywideAlertFailure.EngineerLost=>"engineer_lost",CitywideAlertFailure.ResponseLost=>"response_lost",CitywideAlertFailure.FuelReserveLost=>"fuel_lost",CitywideAlertFailure.ProducerLost=>"producer_lost",CitywideAlertFailure.ServiceOutage=>"outage",CitywideAlertFailure.Deadline=>"timeout",_=>"loss"};
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,
                GameText.Get("mission.citywide_alert.result."+(victory?"victory":"defeat")),GameText.Get("mission.citywide_alert.result.subtitle"),
                GameText.Get("mission.citywide_alert.result."+(victory?"success":reason)),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),
                model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired,
                new UiMissionDefenseResultDetails(facts.CivilianLossCount,false,false,false,facts.HostileRosterIntegrityFault!=0,false), civilianLossCount:facts.CivilianLossCount);
        }
    }
}

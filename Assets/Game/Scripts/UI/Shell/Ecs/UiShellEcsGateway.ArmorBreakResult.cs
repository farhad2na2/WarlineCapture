using Game.Components;
using Game.Configs;
using Game.UI.Contracts;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static UiMissionResultPopupModel LocalizeArmorBreakResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=facts.ArmorBreakFailure switch
            {ArmorBreakFailure.MasteryLost=>"mastery_lost",ArmorBreakFailure.ArmorLost=>"armor_lost",ArmorBreakFailure.AirDefenseLost=>"air_defense_lost",ArmorBreakFailure.LauncherLost=>"launcher_lost",ArmorBreakFailure.AircraftLost=>"aircraft_lost",ArmorBreakFailure.CommandSquadLost=>"team_lost",ArmorBreakFailure.ReliefLost=>"relief_lost",ArmorBreakFailure.FuelReserveLost=>"fuel_lost",ArmorBreakFailure.Deadline=>"timeout",_=>"loss"};
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,
                GameText.Get("mission.armor_break.result."+(victory?"victory":"defeat")),GameText.Get("mission.armor_break.result.subtitle"),
                GameText.Get("mission.armor_break.result."+(victory?"success":reason)),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),
                model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired,
                new UiMissionDefenseResultDetails(facts.CivilianLossCount,false,false,false,facts.HostileRosterIntegrityFault!=0,false), civilianLossCount:facts.CivilianLossCount);
        }
    }
}

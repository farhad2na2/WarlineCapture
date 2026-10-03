using Game.Components;
using Game.Configs;
using Game.UI.Contracts;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static UiMissionResultPopupModel LocalizeCommandNodeResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=facts.CommandNodeFailure switch
            {CommandNodeFailure.EngineerLost=>"engineer_lost",CommandNodeFailure.SpecialistsLost=>"specialists_lost",CommandNodeFailure.ServicesLost=>"services_lost",CommandNodeFailure.StaffLost=>"staff_lost",CommandNodeFailure.EscortLost=>"escort_lost",CommandNodeFailure.FuelReserveLost=>"fuel_lost",CommandNodeFailure.UnisolatedNodeDestroyed=>"unsafe_node",CommandNodeFailure.Deadline=>"timeout",_=>"loss"};
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,
                GameText.Get("mission.command_node.result."+(victory?"victory":"defeat")),GameText.Get("mission.command_node.result.subtitle"),
                GameText.Get("mission.command_node.result."+(victory?"success":reason)),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),
                model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired,
                new UiMissionDefenseResultDetails(facts.CivilianLossCount,false,false,false,facts.HostileRosterIntegrityFault!=0,false), civilianLossCount:facts.CivilianLossCount);
        }
    }
}

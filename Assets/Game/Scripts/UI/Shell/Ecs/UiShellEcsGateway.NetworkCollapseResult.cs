using Game.Components;
using Game.Configs;
using Game.UI.Contracts;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static UiMissionResultPopupModel LocalizeNetworkCollapseResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=facts.NetworkFailure switch
            {NetworkCollapseFailure.EngineerLost=>"engineer_lost",NetworkCollapseFailure.CarrierLost=>"carrier_lost",NetworkCollapseFailure.CivicLost=>"civic_lost",NetworkCollapseFailure.AuditLost=>"audit_lost",NetworkCollapseFailure.StaffLost=>"staff_lost",NetworkCollapseFailure.EscortLost=>"escort_lost",NetworkCollapseFailure.FuelReserveLost=>"fuel_lost",NetworkCollapseFailure.UnverifiedNodeDestroyed=>"unverified",NetworkCollapseFailure.Deadline=>"timeout",_=>"loss"};
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,
                GameText.Get("mission.network_collapse.result."+(victory?"victory":"defeat")),GameText.Get("mission.network_collapse.result.subtitle"),
                GameText.Get("mission.network_collapse.result."+(victory?"success":reason)),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),
                model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired,
                new UiMissionDefenseResultDetails(facts.CivilianLossCount,false,false,false,facts.HostileRosterIntegrityFault!=0,false), civilianLossCount:facts.CivilianLossCount);
        }
    }
}

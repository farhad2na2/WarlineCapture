using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static UiMissionResultPopupModel LocalizeExtractionResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=victory?"success":facts.CivilianLossCount>0?"passenger_lost":facts.ExtractionAircraftLost!=0?"aircraft_lost":facts.ExtractionCarrierLost!=0?"carrier_lost":facts.ExtractionTimedOut!=0?"timeout":facts.HostileRosterIntegrityFault!=0?"integrity":"escort_lost";
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,GameText.Get("mission.m04.result."+(victory?"victory":"defeat")),
                GameText.Get("mission.m04.result.subtitle"),GameText.Get("mission.m04.result."+reason),model.Stars,
                model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),
                GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired,
                default,model.SettlementFailed,new UiMissionExtractionResultDetails(facts.ExtractionPassengersDelivered,facts.CivilianLossCount,facts.ExtractionCarrierLegCount,
                    facts.ExtractionCarrierLost!=0,facts.ExtractionAircraftLost!=0,facts.ExtractionTimedOut!=0));
        }
    }
}

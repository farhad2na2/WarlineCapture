using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static UiMissionResultPopupModel LocalizeBreachResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts,string prefix="mission.m05")
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=victory?"success":facts.HostileRosterIntegrityFault!=0?"setup":facts.BreachTimedOut!=0?"timeout":"loss";
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,GameText.Get(prefix+".result."+(victory?"victory":"defeat")),
                GameText.Get(prefix+".name"),GameText.Get(prefix+".result."+reason),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),
                model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired,default,model.SettlementFailed,default,
                new UiMissionBreachResultDetails(facts.BreachSupportLost!=0,facts.ElapsedMilliseconds));
        }
    }
}

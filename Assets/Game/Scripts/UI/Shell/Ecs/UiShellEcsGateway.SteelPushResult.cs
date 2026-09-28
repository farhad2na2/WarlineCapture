using Game.Components;
using Game.Configs;
using Game.UI.Contracts;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static UiMissionResultPopupModel LocalizeSteelPushResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,
                GameText.Get("mission.steel_push.result."+(victory?"victory":"defeat")),GameText.Get("mission.steel_push.name"),
                GameText.Get("mission.steel_push.result."+(victory?"success":"loss")),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),
                model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired,
                new UiMissionDefenseResultDetails(facts.SquadLossCount,facts.ForwardPostDamaged!=0,facts.ForwardPostDestroyed!=0,facts.CoreBreached!=0,facts.HostileRosterIntegrityFault!=0,false));
        }
    }
}

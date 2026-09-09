using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
using Unity.Collections;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static readonly FixedString64Bytes RadarResultMissionId="saga.ch01.m03.radar_warning";
        private static string cachedMissionResultLocale;
        private static UiMissionResultPopupModel LocalizeDefenseResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts,in CampaignMissionRuntimeComponent runtime)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=victory ? "victory_body" : facts.ForwardPostDestroyed!=0 ? "post_lost" : facts.CoreBreached!=0 ? "breach" : "integrity";
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,
                GameText.Get("mission.m03.result."+(victory ? "victory" : "defeat")),
                GameText.Get("mission.m03.name")+" • "+GameText.Get("mission.m03.location"),
                GameText.Get("mission.m03.result."+reason),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory ? model.RewardsText : GameText.Get("mission.m03.result.no_reward"),
                GameText.Get(victory ? "mission.m03.action.continue" : "mission.m03.result.retry"),
                model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired,
                new UiMissionDefenseResultDetails(facts.CivilianLossCount,facts.ForwardPostDamaged!=0,facts.ForwardPostDestroyed!=0,facts.CoreBreached!=0,facts.HostileRosterIntegrityFault!=0,
                    runtime.Guidance!=Game.Narrative.Contracts.NarrativeGuidanceMode.Minimal && (runtime.RunKind==Game.Missions.Contracts.MissionRunKind.FirstClear || runtime.ReplayTutorialEnabled!=0)));
        }
    }
}

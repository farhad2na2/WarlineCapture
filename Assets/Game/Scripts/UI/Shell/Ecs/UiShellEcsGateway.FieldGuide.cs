using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        public bool IsMissionFieldGuidePresenting()
        {
            if(!TryGetBoundary(out var em,out var root) || !em.HasComponent<UiShellActivePopupComponent>(root)) return false;
            var popup=em.GetComponentData<UiShellActivePopupComponent>(root);
            var state=em.GetComponentData<UiShellStateComponent>(root);
            return popup.PopupKind==UiShellPopupKind.MissionFieldGuide && (popup.Visible!=0 || state.IsTransitionRunning!=0);
        }
        private static bool TryGetGuideMission(out CampaignMissionRuntimeComponent mission,out bool archived)
        {
            mission=default; archived=false;
            if(!TryGetMissionRoot(out var em,out var root) || !TryGetBoundary(out var shell,out var boundary)) return false;
            mission=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if(shell.GetComponentData<UiShellStateComponent>(boundary).ActiveRoute==UIRoute.Match)
            {
                if(em.HasComponent<CampaignMissionGridlockState>(root) && mission.MissionId.Equals(new Unity.Collections.FixedString64Bytes(Game.Missions.Contracts.CampaignMissionSequence.Gridlock)))
                {
                    var gridlock=em.GetComponentData<CampaignMissionGridlockState>(root);
                    return gridlock.SessionToken.Equals(mission.SessionToken) && gridlock.AttemptOrdinal==mission.AttemptOrdinal && gridlock.SourceVersion==mission.SourceVersion;
                }
                if(mission.MissionId.Equals(BreachId) && em.HasComponent<CampaignMissionBreachState>(root))
                {
                    var breach=em.GetComponentData<CampaignMissionBreachState>(root);
                    return breach.SessionToken.Equals(mission.SessionToken) && breach.AttemptOrdinal==mission.AttemptOrdinal && breach.SourceVersion==mission.SourceVersion;
                }
                if(mission.MissionId.Equals(AirliftId) && em.HasComponent<CampaignMissionExtractionState>(root))
                {
                    var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
                    return extraction.SessionToken.Equals(mission.SessionToken) && extraction.AttemptOrdinal==mission.AttemptOrdinal && extraction.SourceVersion==mission.SourceVersion;
                }
                if(!mission.MissionId.Equals(RadarResultMissionId) || !em.HasComponent<CampaignMissionDefenseStateComponent>(root)) return false;
                var defense=em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
                return defense.SessionToken.Equals(mission.SessionToken) && defense.AttemptOrdinal==mission.AttemptOrdinal && defense.SourceVersion==mission.SourceVersion;
            }
            if(!UiShellReadModelAdapter.TryReadCampaignOperations(out var campaign) || campaign.SelectedMission.MissionId!="saga.ch01.m03.radar_warning" && campaign.SelectedMission.MissionId!="saga.ch01.m04.airlift" && campaign.SelectedMission.MissionId!="saga.ch01.m05.breach_assault" && campaign.SelectedMission.MissionId!=Game.Missions.Contracts.CampaignMissionSequence.Gridlock || !campaign.SelectedMission.Available) return false;
            archived=campaign.SelectedMission.FirstClearCompleted; mission=default; return true;
        }
        public bool TryReadMissionRadioArchive()
        {
            if(!TryGetGuideMission(out var mission,out bool archived)) return false;
            if(archived) return true;
            return mission.MissionId.Equals(RadarResultMissionId) && TryGetMissionRoot(out var em,out var root) &&
                em.HasComponent<CampaignMissionAttemptFactsComponent>(root) && em.GetComponentData<CampaignMissionAttemptFactsComponent>(root).ElapsedMilliseconds>=45000;
        }
    }
}

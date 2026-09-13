using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiMissionBreachGateway
    {
        private static readonly FixedString64Bytes BreachId="saga.ch01.m05.breach_assault";
        private static string AppendBreachStatus(string text)
        {
            if(!TryGetMissionRoot(out var em,out var root) || !em.HasComponent<CampaignMissionBreachState>(root))return text;
            var breach=em.GetComponentData<CampaignMissionBreachState>(root);
            if((breach.GuidanceCompletedMask&1)==0)return text;
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            int remaining=System.Math.Max(0,(breach.DeadlineMilliseconds-facts.ElapsedMilliseconds+999)/1000);
            string status=string.Format(GameText.Get("mission.m05.hud.status"),breach.SecureHoldMilliseconds/1000,
                System.Math.Max(0,facts.HostileTotalCount-facts.HostileDefeatedCount),$"{remaining/60:00}:{remaining%60:00}",breach.SecureRequiredMilliseconds/1000);
            if(breach.CounterattackReleaseAtMilliseconds>0 && breach.CounterattackReleased==0)
                status=string.Format(GameText.Get("mission.m05.hud.counterattack"),System.Math.Max(0,(breach.CounterattackReleaseAtMilliseconds-facts.ElapsedMilliseconds+999)/1000))+"\n"+status;
            return text+"\n\n"+status;
        }
        public bool IsBreachGuideContext()
        {
            if(TryGetMissionRoot(out var em,out var root) && em.GetComponentData<CampaignMissionRuntimeComponent>(root).MissionId.Equals(BreachId) &&
                TryGetBoundary(out var shell,out var boundary) && shell.GetComponentData<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>(boundary).ActiveRoute==UIRoute.Match) return true;
            return UiShellReadModelAdapter.TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId==BreachId.ToString();
        }
        public bool TryReadBreachInputMode(out int mode)
        {
            mode=0;if(!TryGetMissionRoot(out var em,out var root) || !em.GetComponentData<CampaignMissionRuntimeComponent>(root).MissionId.Equals(BreachId))return false;
            using var query=em.CreateEntityQuery(typeof(RtsSelectionInputStateComponent));if(query.CalculateEntityCount()!=1)return false;
            mode=query.GetSingleton<RtsSelectionInputStateComponent>().ActiveCommandMode;return true;
        }
        public bool TrySelectBreachActor()
        {
            if(!TryGetMissionRoot(out var em,out var root) || !em.GetComponentData<CampaignMissionRuntimeComponent>(root).MissionId.Equals(BreachId)) return false;
            var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            if(guidance.Active==0 || guidance.CanExecute==0 || !em.Exists(guidance.SourceEntity)) return false;
            using var query=em.CreateEntityQuery(typeof(RtsSelectionInputRequestQueueComponent),typeof(RtsSelectionCommandIntentRequestElement));
            if(query.CalculateEntityCount()!=1)return false;
            var request=new AssistantCommandIntentRequestElement {Frame=Time.frameCount,TargetKind=AssistantTargetKind.Squad,
                TargetEntity=guidance.SourceEntity,SourceEntity=guidance.SourceEntity};
            return AssistantSelectionCommandUtility.TryQueue(em,query.GetSingletonEntity(),in request,out _);
        }
        public bool TryContinueBreachPlan()
        {
            if(!TryGetMissionRoot(out var em,out var root) || !em.HasComponent<CampaignMissionGuidanceProjectionComponent>(root)) return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            if(!runtime.MissionId.Equals(BreachId) || runtime.Phase!=MissionPhaseKind.Engage || runtime.Outcome!=MissionOutcomeKind.None || guidance.Active==0 || guidance.GuidanceId!=65001) return false;
            var requests=em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
            if(requests.Length>=8) return false;
            requests.Add(new CampaignMissionGuidanceAcknowledgementRequestElement {SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,GuidanceId=65001}); return true;
        }
    }
}

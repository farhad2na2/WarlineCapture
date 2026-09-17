using Game.Narrative.Contracts;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    public partial struct CampaignMissionGuidanceProjectionSystem
    {
        private static readonly FixedString64Bytes BreachMissionId="saga.ch01.m05.breach_assault";
        private static readonly FixedString64Bytes BreachTitlePrefix="mission.m05.tutorial.";
        private static readonly FixedString32Bytes BreachFallbackSuffix=".fallback";
        private static readonly FixedString128Bytes BreachBodyPrefix="mission.m05.tutorial.";
        private bool TryUpdateBreachGuidance(ref SystemState state,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts,in AssistantSettingsComponent settings,in CampaignMissionGuidanceProjectionComponent current)
        {
            if(!runtime.MissionId.Equals(BreachMissionId)) return false;
            var em=state.EntityManager;
            if(runtime.Phase!=MissionPhaseKind.Engage || runtime.Outcome!=MissionOutcomeKind.None || !em.HasComponent<CampaignMissionBreachState>(root))
            {ClearDefenseGuidance(em,root,in current);return true;}
            var breach=em.GetComponentData<CampaignMissionBreachState>(root);
            if(breach.Ready==0 || !breach.SessionToken.Equals(runtime.SessionToken) || breach.AttemptOrdinal!=runtime.AttemptOrdinal || breach.SourceVersion!=runtime.SourceVersion) return true;
            var acks=em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
            foreach(var ack in acks) if(ack.GuidanceId==65001 && ack.SessionToken.Equals(runtime.SessionToken) && ack.AttemptOrdinal==runtime.AttemptOrdinal) breach.GuidanceCompletedMask|=1;
            acks.Clear();
            var members=em.GetBuffer<CampaignMissionBreachMember>(root,true);
            Entity actor=Entity.Null,rifle=Entity.Null,hostile=Entity.Null; bool rifleThrough=false,atArchive=breach.FriendlyAtArchive!=0;
            foreach(var member in members)
            {
                var e=member.Entity;
                if(!em.Exists(e) || !em.HasComponent<UnitHealth>(e) || em.GetComponentData<UnitHealth>(e).Current<=0 || !em.HasComponent<LocalTransform>(e)) continue;
                var position=em.GetComponentData<LocalTransform>(e).Position;
                if(member.Kind<2)
                {
                    if(actor==Entity.Null || em.HasComponent<SelectedUnitTag>(e)) actor=e;
                    if(member.Kind==0 && (rifle==Entity.Null || em.HasComponent<SelectedUnitTag>(e))) rifle=e;
                    if(member.Kind==0 && math.distancesq(position.xz,breach.ArchiveCenter.xz)<28*28) rifleThrough=true;
                }
                else if(hostile==Entity.Null) hostile=e;
            }
            if(breach.SupportLost==0 && em.Exists(breach.Support) && (actor==Entity.Null || !em.HasComponent<SelectedUnitTag>(actor))) actor=breach.Support;
            if(actor==Entity.Null) {ClearDefenseGuidance(em,root,in current);return true;}
            if(em.HasComponent<SelectedUnitTag>(actor)) breach.GuidanceCompletedMask|=2;
            if(breach.GateDestroyed!=0) breach.GuidanceCompletedMask|=4;
            if(breach.GateDestroyed!=0 && (rifleThrough || rifle==Entity.Null && atArchive)) breach.GuidanceCompletedMask|=8;
            if(breach.CoreDestroyed!=0) breach.GuidanceCompletedMask|=16;
            if(breach.CounterattackReleased!=0 && hostile==Entity.Null) breach.GuidanceCompletedMask|=32;
            if(breach.CoreDestroyed!=0 && atArchive) breach.GuidanceCompletedMask|=64;
            em.SetComponentData(root,breach);
            int step=1;while(step<8 && (breach.GuidanceCompletedMask&(1u<<(step-1)))!=0) step++;
            if(step==4 && rifle!=Entity.Null) actor=rifle;
            Entity target=step==3?breach.Gate:step==5?breach.Core:step==6?hostile:Entity.Null;
            float3 destination=step==3?breach.GateCenter:step==5?breach.CoreCenter:breach.ArchiveCenter;
            if(target!=Entity.Null && em.HasComponent<LocalTransform>(target)) destination=em.GetComponentData<LocalTransform>(target).Position;
            var title=BreachTitlePrefix; title.Append(step);title.Append(ExtractionTitleSuffix);
            var body=BreachBodyPrefix; body.Append(step);body.Append(step==4 && rifle==Entity.Null ? BreachFallbackSuffix : ExtractionBodySuffix);
            var next=new CampaignMissionGuidanceProjectionComponent {GuidanceId=65000+step,Version=Next(current.Version),MissionSourceVersion=runtime.Version,
                Prompt=(CampaignMissionGuidancePromptKind)(36+step),GuidanceMode=NarrativeGuidanceMode.Full,Active=1,Priority=AssistantMessagePriority.High,
                RecommendationKind=step is 3 or 5 or 6?AssistantRecommendationKind.Attack:(step is 4 or 7 || step==8 && !atArchive)?AssistantRecommendationKind.Move:step==2?AssistantRecommendationKind.Select:AssistantRecommendationKind.Explain,
                TargetKind=step==2?AssistantTargetKind.Squad:target!=Entity.Null?AssistantTargetKind.Entity:AssistantTargetKind.WorldPosition,
                SourceEntity=actor,TargetEntity=step==2?actor:target,WorldPosition=destination,HasWorldPosition=1,Title=title,Body=body,
                ActionLabel=step==1?RadarContinue:RadarAct,CanExecute=(step<8 || !atArchive)&&(step!=6||target!=Entity.Null)?(byte)1:(byte)0,
                CanShow=(step!=6||target!=Entity.Null)?(byte)1:(byte)0,
                SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled};
            if(!ProjectionEquals(in current,in next) || current.MissionSourceVersion!=next.MissionSourceVersion) em.SetComponentData(root,next);
            return true;
        }
    }
}

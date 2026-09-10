using Unity.Collections;
using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    public partial struct CampaignMissionGuidanceProjectionSystem
    {
        private static readonly FixedString64Bytes ExtractionMissionId = "saga.ch01.m04.airlift";
        private static readonly FixedString64Bytes ExtractionTitlePrefix = "mission.m04.tutorial.";
        private static readonly FixedString128Bytes ExtractionBodyPrefix = "mission.m04.tutorial.";
        private static readonly FixedString32Bytes ExtractionTitleSuffix = ".title";
        private static readonly FixedString32Bytes ExtractionBodySuffix = ".body";
        private static readonly FixedString64Bytes ExtractionTargetPrefix = "tutorial.ch01.m04.";
        private static readonly FixedString64Bytes ExtractionGuideAction = "mission.m03.guide.open";
        private bool TryUpdateExtractionGuidance(ref SystemState state,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts,in AssistantSettingsComponent settings,in CampaignMissionGuidanceProjectionComponent current)
        {
            var em=state.EntityManager;
            if(!runtime.MissionId.Equals(ExtractionMissionId))return false;
            if(!em.HasComponent<CampaignMissionExtractionState>(root)) {ClearDefenseGuidance(em,root,in current);return true;}
            var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
            if(!extraction.SessionToken.Equals(runtime.SessionToken) || extraction.AttemptOrdinal!=runtime.AttemptOrdinal || extraction.SourceVersion!=runtime.SourceVersion ||
                runtime.Phase!=MissionPhaseKind.Engage || runtime.Outcome!=MissionOutcomeKind.None || runtime.Guidance==NarrativeGuidanceMode.Minimal ||
                runtime.RunKind!=MissionRunKind.FirstClear && runtime.ReplayTutorialEnabled==0)
            {ClearDefenseGuidance(em,root,in current);return true;}
            if(!SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) || gameplay.SimulationActive==0)return true;
            var acks=em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
            for(int i=0;i<acks.Length;i++) if(acks[i].SessionToken.Equals(runtime.SessionToken) && acks[i].AttemptOrdinal==runtime.AttemptOrdinal && acks[i].GuidanceId==55001) extraction.GuidanceCompletedMask|=1;
            acks.Clear();
            bool selectedCarrier=em.Exists(extraction.Carrier)&&em.HasComponent<SelectedUnitTag>(extraction.Carrier);
            bool selectedAircraft=em.Exists(extraction.Aircraft)&&em.HasComponent<SelectedUnitTag>(extraction.Aircraft);
            bool selectedTeam=false; int inAircraft=0,groundAtLanding=0;float3 team=default;int teamCount=0;
            var members=em.GetBuffer<CampaignMissionExtractionMember>(root,true);
            for(int i=0;i<members.Length;i++) if(members[i].Kind==1 && em.Exists(members[i].Entity))
            {
                var e=members[i].Entity;selectedTeam|=em.HasComponent<SelectedUnitTag>(e);
                if(em.HasComponent<LocalTransform>(e)){var pos=em.GetComponentData<LocalTransform>(e).Position;team+=pos;teamCount++;
                    if(!em.HasComponent<UnitTransportPassenger>(e)&&math.distancesq(pos.xz,extraction.LandingCenter.xz)<=25*25)groundAtLanding++;}
                if(em.HasComponent<UnitTransportPassenger>(e)&&em.GetComponentData<UnitTransportPassenger>(e).Transport==extraction.Aircraft)inAircraft++;
            }
            if(teamCount>0)team/=teamCount;
            float3 carrier=em.Exists(extraction.Carrier)&&em.HasComponent<LocalTransform>(extraction.Carrier)?em.GetComponentData<LocalTransform>(extraction.Carrier).Position:default;
            bool nearTeam=math.distancesq(carrier.xz,team.xz)<=20*20;
            bool atLanding=math.distancesq(carrier.xz,extraction.LandingCenter.xz)<=20*20;
            if(groundAtLanding==4 && facts.ExtractionCarrierLegCount==4)extraction.UnloadedAtLanding=1;
            if(runtime.Guidance==NarrativeGuidanceMode.Contextual)extraction.GuidanceCompletedMask|=1u|2u|8u|128u;
            for(int step=1;step<=12;step++)
            {
                bool done=step switch {2=>selectedCarrier,3=>nearTeam||facts.ExtractionCarrierLegCount>0,4=>selectedTeam||facts.ExtractionCarrierLegCount>0,
                    5=>facts.ExtractionCarrierLegCount==4,6=>atLanding&&facts.ExtractionCarrierLegCount==4,7=>extraction.UnloadedAtLanding!=0||inAircraft==4,
                    8=>selectedAircraft||inAircraft>0,9=>inAircraft==4,10=>extraction.DepartureCleared!=0,11=>facts.ExtractionDeparted!=0,12=>runtime.Outcome!=MissionOutcomeKind.None,_=>false};
                if(done)extraction.GuidanceCompletedMask|=1u<<(step-1);
            }
            em.SetComponentData(root,extraction);
            int chosen=1;while(chosen<=12&&(extraction.GuidanceCompletedMask&(1u<<(chosen-1)))!=0)chosen++;
            if(chosen>12){ClearDefenseGuidance(em,root,in current);return true;}
            var title=ExtractionTitlePrefix;title.Append(chosen);title.Append(ExtractionTitleSuffix);
            var body=ExtractionBodyPrefix;body.Append(chosen);body.Append(ExtractionBodySuffix);
            var targetId=ExtractionTargetPrefix;targetId.Append(chosen);
            var next=new CampaignMissionGuidanceProjectionComponent {GuidanceId=55000+chosen,Version=Next(current.Version),MissionSourceVersion=runtime.Version,
                Prompt=(CampaignMissionGuidancePromptKind)(24+chosen),GuidanceMode=runtime.Guidance,Active=1,
                RecommendationKind=AssistantRecommendationKind.Explain,TargetKind=AssistantTargetKind.UiSurface,
                TargetId=targetId,
                Title=title,Body=body,
                ActionLabel=chosen==1?RadarContinue:chosen is 7 or 10 or 12?ExtractionGuideAction:RadarAct,
                Priority=facts.ElapsedMilliseconds>=480000?AssistantMessagePriority.Critical:AssistantMessagePriority.High,
                CanShow=1,CanExecute=1,WorldPosition=chosen<=5?team:chosen>=11?extraction.DepartureCenter:extraction.LandingCenter,HasWorldPosition=1,
                SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled};
            if(!ProjectionEquals(in current,in next)||current.MissionSourceVersion!=next.MissionSourceVersion||!current.Title.Equals(next.Title)||current.Priority!=next.Priority)em.SetComponentData(root,next);
            return true;
        }
    }
}

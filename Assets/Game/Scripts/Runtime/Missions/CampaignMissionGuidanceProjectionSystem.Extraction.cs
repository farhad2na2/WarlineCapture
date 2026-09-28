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
        private static readonly FixedString64Bytes EvidenceChainMissionId = CampaignMissionSequence.EvidenceChain;
        private static readonly FixedString64Bytes ExtractionTitlePrefix = "mission.m04.tutorial.";
        private static readonly FixedString128Bytes ExtractionBodyPrefix = "mission.m04.tutorial.";
        private static readonly FixedString64Bytes EvidenceChainTitlePrefix = "mission.evidence_chain.tutorial.";
        private static readonly FixedString128Bytes EvidenceChainBodyPrefix = "mission.evidence_chain.tutorial.";
        private static readonly FixedString64Bytes EvidenceChainArmoredTitlePrefix = "mission.evidence_chain.tutorial.armored.";
        private static readonly FixedString128Bytes EvidenceChainArmoredBodyPrefix = "mission.evidence_chain.tutorial.armored.";
        private static readonly FixedString32Bytes ExtractionTitleSuffix = ".title";
        private static readonly FixedString32Bytes ExtractionBodySuffix = ".body";
        private static readonly FixedString64Bytes ExtractionTargetPrefix = "tutorial.ch01.m04.";
        private static readonly FixedString64Bytes EvidenceChainTargetPrefix = "tutorial.ch03.m04.";
        private static readonly FixedString64Bytes ExtractionGuideAction = "mission.m03.guide.open";
        private static readonly FixedString64Bytes ExtractionPassengerAction="mission.m04.action.passengers";
        private bool TryUpdateExtractionGuidance(ref SystemState state,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts,in AssistantSettingsComponent settings,in CampaignMissionGuidanceProjectionComponent current)
        {
            var em=state.EntityManager;
            bool evidenceChain = runtime.MissionId.Equals(EvidenceChainMissionId);
            if(!runtime.MissionId.Equals(ExtractionMissionId) && !evidenceChain)return false;
            if(!em.HasComponent<CampaignMissionExtractionState>(root)) {ClearDefenseGuidance(em,root,in current);return true;}
            var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
            bool armored = evidenceChain && extraction.ArmoredRoute != 0;
            if(!extraction.SessionToken.Equals(runtime.SessionToken) || extraction.AttemptOrdinal!=runtime.AttemptOrdinal || extraction.SourceVersion!=runtime.SourceVersion ||
                runtime.Phase!=MissionPhaseKind.Engage || runtime.Outcome!=MissionOutcomeKind.None)
            {ClearDefenseGuidance(em,root,in current);return true;}
            if(!SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) || gameplay.SimulationActive==0)return true;
            var acks=em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
            for(int i=0;i<acks.Length;i++) if(acks[i].SessionToken.Equals(runtime.SessionToken) && acks[i].AttemptOrdinal==runtime.AttemptOrdinal && acks[i].GuidanceId==55001) extraction.GuidanceCompletedMask|=1;
            acks.Clear();
            bool selectedCarrier=em.Exists(extraction.Carrier)&&em.HasComponent<SelectedUnitTag>(extraction.Carrier);
            bool selectedAircraft=em.Exists(extraction.Aircraft)&&em.HasComponent<SelectedUnitTag>(extraction.Aircraft);
            int inAircraft=0,groundAtLanding=0;float3 team=default;int selectedTeamCount=0; int teamCount=0;
            float3 handoff = evidenceChain && !armored ? extraction.HandoffCenter : extraction.LandingCenter;
            var members=em.GetBuffer<CampaignMissionExtractionMember>(root,true);
            int required = math.max(1, facts.ExtractionPassengerTotal);
            for(int i=0;i<members.Length;i++) if(members[i].Kind==1 && em.Exists(members[i].Entity))
            {
                var e=members[i].Entity;if(em.HasComponent<SelectedUnitTag>(e)) selectedTeamCount++;
                if(em.HasComponent<LocalTransform>(e)){var pos=em.GetComponentData<LocalTransform>(e).Position;team+=pos;teamCount++;
                    if(!em.HasComponent<UnitTransportPassenger>(e)&&math.distancesq(pos.xz,handoff.xz)<=25*25)groundAtLanding++;}
                if(em.HasComponent<UnitTransportPassenger>(e)&&em.GetComponentData<UnitTransportPassenger>(e).Transport==extraction.Aircraft)inAircraft++;
            }
            if(teamCount>0)team/=teamCount;
            float3 carrier=em.Exists(extraction.Carrier)&&em.HasComponent<LocalTransform>(extraction.Carrier)?em.GetComponentData<LocalTransform>(extraction.Carrier).Position:default;
            bool nearTeam=math.distancesq(carrier.xz,team.xz)<=20*20;
            bool atLanding=math.distancesq(carrier.xz,handoff.xz)<=20*20;
            if(groundAtLanding==required && facts.ExtractionCarrierLegCount==required)extraction.UnloadedAtLanding=1;
            // M4 teaches the rescue workflow on every entry, including older replay/retry payloads.
            for(int step=1;step<=12;step++)
            {
                bool done=step switch {2=>selectedCarrier,3=>nearTeam||facts.ExtractionCarrierLegCount>0,4=>selectedTeamCount==required||facts.ExtractionCarrierLegCount==required,
                    5=>facts.ExtractionCarrierLegCount==required,6=>atLanding&&facts.ExtractionCarrierLegCount==required,
                    7=>armored ? atLanding && facts.ExtractionCarrierLegCount==required : extraction.UnloadedAtLanding!=0||inAircraft==required,
                    8=>armored ? atLanding && facts.ExtractionCarrierLegCount==required : selectedAircraft||inAircraft>0,
                    9=>armored ? atLanding && facts.ExtractionCarrierLegCount==required : inAircraft==required,
                    10=>extraction.DepartureCleared!=0,11=>facts.ExtractionDeparted!=0,12=>runtime.Outcome!=MissionOutcomeKind.None,_=>false};
                if(done)extraction.GuidanceCompletedMask|=1u<<(step-1);
            }
            em.SetComponentData(root,extraction);
            int chosen=1;while(chosen<=12&&(extraction.GuidanceCompletedMask&(1u<<(chosen-1)))!=0)chosen++;
            if(chosen>12){ClearDefenseGuidance(em,root,in current);return true;}
            var title=evidenceChain ? EvidenceChainTitlePrefix : ExtractionTitlePrefix;title.Append(chosen);title.Append(ExtractionTitleSuffix);
            var body=evidenceChain ? EvidenceChainBodyPrefix : ExtractionBodyPrefix;body.Append(chosen);body.Append(ExtractionBodySuffix);
            if (armored && chosen is 6 or 10 or 11)
            {
                title = EvidenceChainArmoredTitlePrefix; title.Append(chosen); title.Append(ExtractionTitleSuffix);
                body = EvidenceChainArmoredBodyPrefix; body.Append(chosen); body.Append(ExtractionBodySuffix);
            }
            var targetId=evidenceChain ? EvidenceChainTargetPrefix : ExtractionTargetPrefix;targetId.Append(chosen);
            var next=new CampaignMissionGuidanceProjectionComponent {GuidanceId=55000+chosen,Version=Next(current.Version),MissionSourceVersion=runtime.Version,
                Prompt=(CampaignMissionGuidancePromptKind)(24+chosen),GuidanceMode=NarrativeGuidanceMode.Full,Active=1,
                RecommendationKind=AssistantRecommendationKind.Explain,TargetKind=AssistantTargetKind.UiSurface,
                TargetId=targetId,
                Title=title,Body=body,
                ActionLabel=chosen==7 ? ExtractionPassengerAction : chosen==1?RadarContinue:chosen is 10 or 12?ExtractionGuideAction:RadarAct,
                Priority=facts.ElapsedMilliseconds>=480000?AssistantMessagePriority.Critical:AssistantMessagePriority.High,
                CanShow=1,CanExecute=chosen==7 && !selectedCarrier ? (byte)0 : (byte)1,
                WorldPosition=chosen<=5?team:chosen is 6 or 7 ? handoff : chosen>=11?extraction.DepartureCenter:extraction.LandingCenter,HasWorldPosition=1,
                SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled};
            if(!ProjectionEquals(in current,in next)||current.MissionSourceVersion!=next.MissionSourceVersion||!current.Title.Equals(next.Title)||current.Priority!=next.Priority)em.SetComponentData(root,next);
            return true;
        }
    }
}

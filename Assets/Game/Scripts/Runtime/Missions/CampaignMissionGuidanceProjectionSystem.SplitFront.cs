using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionGuidanceProjectionSystem
    {
        private static readonly FixedString64Bytes SplitFrontMissionId=CampaignMissionSequence.SplitFront;
        private static readonly FixedString32Bytes SplitTitleSuffix=".title",SplitBodySuffix=".body";
        private static readonly FixedString64Bytes SplitTutorialPrefix="mission.split_front.tutorial.";
        private static readonly FixedString128Bytes SplitMainBody="mission.split_front.tutorial.main.body";
        private static readonly FixedString64Bytes SplitWestLauncher="group.ch04.m03.launcher",SplitCoverageAnchor="anchor.ch04.m03.fork";
        private bool TryUpdateSplitFrontGuidance(ref SystemState state,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts,in AssistantSettingsComponent settings,in CampaignMissionGuidanceProjectionComponent current)
        {
            if(!runtime.MissionId.Equals(SplitFrontMissionId))return false;
            var em=state.EntityManager;
            if(runtime.Phase!=MissionPhaseKind.Engage||runtime.Outcome!=MissionOutcomeKind.None||!em.HasComponent<CampaignMissionDefenseStateComponent>(root))
            {ClearDefenseGuidance(em,root,in current);return true;}
            var defense=em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
            if(!defense.SessionToken.Equals(runtime.SessionToken))return true;
            var acks=em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
            foreach(var ack in acks)if(ack.GuidanceId==65001&&ack.SessionToken.Equals(runtime.SessionToken)&&ack.AttemptOrdinal==runtime.AttemptOrdinal)defense.AcknowledgedGuidanceMask|=1;
            acks.Clear();Entity actor=Entity.Null;
            foreach(var (role,faction,health,e) in SystemAPI.Query<RefRO<CampaignMissionUnitRoleComponent>,RefRO<Faction>,RefRO<UnitHealth>>().WithEntityAccess())
                if(role.ValueRO.SessionToken.Equals(runtime.SessionToken)&&role.ValueRO.UnitGroupId.Equals(SplitWestLauncher)&&health.ValueRO.Current>0&&faction.ValueRO.Id==1)actor=e;
            if(actor==Entity.Null){ClearDefenseGuidance(em,root,in current);return true;}
            float3 position=em.GetComponentData<LocalTransform>(actor).Position,coverage=position;
            if(SystemAPI.TryGetSingleton(out OperationMapMetadataComponent map)&&map.Blob.IsCreated&&
                CampaignMissionSpawnSystem.TryFindAnchor(ref map.Blob.Value,SplitCoverageAnchor,out var anchor))coverage=anchor.Position;
            if(em.HasComponent<SelectedUnitTag>(actor))defense.AcknowledgedGuidanceMask|=2;
            if((defense.AcknowledgedGuidanceMask&3)==3)defense.AcknowledgedGuidanceMask|=0x1FFu;
            Entity battery=Entity.Null;foreach(var (role,health,e) in SystemAPI.Query<RefRO<CampaignMissionUnitRoleComponent>,RefRO<UnitHealth>>().WithEntityAccess())
                if(role.ValueRO.SessionToken.Equals(runtime.SessionToken)&&role.ValueRO.MissionRoleId.Equals(new FixedString64Bytes("role.hostile.battery"))&&health.ValueRO.Current>0)battery=e;
            var consent=em.HasComponent<SplitFrontLauncherCommandState>(actor)?em.GetComponentData<SplitFrontLauncherCommandState>(actor):default;
            if(battery!=Entity.Null)coverage=em.GetComponentData<LocalTransform>(battery).Position;
            em.SetComponentData(root,defense);
            int step=(defense.AcknowledgedGuidanceMask&1)==0?1:(defense.AcknowledgedGuidanceMask&2)==0?2:battery!=Entity.Null&&consent.CommandedTarget==Entity.Null&&!em.HasComponent<GroundMissileInFlightComponent>(actor)&&em.GetComponentData<GroundMissileLauncherStateComponent>(actor).Phase==(byte)GroundMissileLauncherPhase.Idle?3:4;
            int id=step==3?65004:step==4?65008:65000+step;
            FixedString64Bytes title=SplitTutorialPrefix;title.Append(step);title.Append(SplitTitleSuffix);
            FixedString128Bytes body=SplitTutorialPrefix;body.Append(step);body.Append(SplitBodySuffix);
            if(step==4&&battery==Entity.Null)body=SplitMainBody;
            var next=new CampaignMissionGuidanceProjectionComponent{
                GuidanceId=id,Version=Next(current.Version),MissionSourceVersion=runtime.Version,
                Prompt=(CampaignMissionGuidancePromptKind)(82+step),GuidanceMode=NarrativeGuidanceMode.Full,Active=1,
                RecommendationKind=step==2?AssistantRecommendationKind.Select:step==3?AssistantRecommendationKind.Attack:AssistantRecommendationKind.Explain,
                TargetKind=step==2?AssistantTargetKind.Squad:AssistantTargetKind.WorldPosition,SourceEntity=actor,TargetEntity=step==2?actor:step==3?battery:Entity.Null,
                WorldPosition=coverage,HasWorldPosition=1,Title=title,Body=body,ActionLabel=step==1?RadarContinue:step==4?WaitAction:RadarAct,
                CanShow=1,CanExecute=step<4?(byte)1:(byte)0,Priority=AssistantMessagePriority.High,
                SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled};
            if(!ProjectionEquals(in current,in next)||!current.Body.Equals(next.Body))em.SetComponentData(root,next);
            return true;
        }
    }
}

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
        private static readonly FixedString64Bytes SteelPushMissionId=CampaignMissionSequence.SteelPush;
        private static readonly FixedString32Bytes SteelTitleSuffix=".title",SteelBodySuffix=".body";
        private static readonly FixedString64Bytes SteelTutorialPrefix="mission.steel_push.tutorial.";
        private static readonly FixedString128Bytes SteelMainBody="mission.steel_push.tutorial.main.body";
        private static readonly FixedString64Bytes SteelWestLauncher="group.ch04.m02.tank_front",SteelCoverageAnchor="anchor.ch04.m02.fork";
        private bool TryUpdateSteelPushGuidance(ref SystemState state,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts,in AssistantSettingsComponent settings,in CampaignMissionGuidanceProjectionComponent current)
        {
            if(!runtime.MissionId.Equals(SteelPushMissionId))return false;
            var em=state.EntityManager;
            if(runtime.Phase!=MissionPhaseKind.Engage||runtime.Outcome!=MissionOutcomeKind.None||!em.HasComponent<CampaignMissionDefenseStateComponent>(root))
            {ClearDefenseGuidance(em,root,in current);return true;}
            var defense=em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
            if(!defense.SessionToken.Equals(runtime.SessionToken))return true;
            var acks=em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
            foreach(var ack in acks)if(ack.GuidanceId==65001&&ack.SessionToken.Equals(runtime.SessionToken)&&ack.AttemptOrdinal==runtime.AttemptOrdinal)defense.AcknowledgedGuidanceMask|=1;
            acks.Clear();Entity actor=Entity.Null;
            foreach(var (role,faction,health,e) in SystemAPI.Query<RefRO<CampaignMissionUnitRoleComponent>,RefRO<Faction>,RefRO<UnitHealth>>().WithEntityAccess())
                if(role.ValueRO.SessionToken.Equals(runtime.SessionToken)&&role.ValueRO.UnitGroupId.Equals(SteelWestLauncher)&&health.ValueRO.Current>0&&faction.ValueRO.Id==1)actor=e;
            // Losing the front tank must not hide the live hold/defense instruction
            // while the remaining counterforce can still complete the mission.
            if(actor==Entity.Null&&(defense.AcknowledgedGuidanceMask&0x1FFu)==0x1FFu)
                foreach(var (role,faction,health,e) in SystemAPI.Query<RefRO<CampaignMissionUnitRoleComponent>,RefRO<Faction>,RefRO<UnitHealth>>().WithEntityAccess())
                    if(role.ValueRO.SessionToken.Equals(runtime.SessionToken)&&health.ValueRO.Current>0&&faction.ValueRO.Id==1)actor=e;
            if(actor==Entity.Null){ClearDefenseGuidance(em,root,in current);return true;}
            float3 position=em.GetComponentData<LocalTransform>(actor).Position,coverage=position;
            if(SystemAPI.TryGetSingleton(out OperationMapMetadataComponent map)&&map.Blob.IsCreated&&
                CampaignMissionSpawnSystem.TryFindAnchor(ref map.Blob.Value,SteelCoverageAnchor,out var anchor))coverage=anchor.Position;
            bool shortage=false;
            if(em.HasComponent<UnitFuelConsumption>(actor)&&em.HasComponent<CampaignMissionSteelPushState>(root))
            {
                var consumption=em.GetComponentData<UnitFuelConsumption>(actor);
                float required=(math.abs(position.x-coverage.x)+math.abs(position.z-coverage.z))*consumption.GroundFuelPerCell;
                var reserve=em.GetComponentData<CampaignMissionSteelPushState>(root);
                if(reserve.Initialized!=0&&em.Exists(reserve.FuelReserve)&&em.HasComponent<BuildingResourceStorageComponent>(reserve.FuelReserve))
                {
                    var storage=em.GetComponentData<BuildingResourceStorageComponent>(reserve.FuelReserve);
                    float usable=math.max(0,storage.StoredFuelBarrels-storage.ReservedFuelOutboundBarrels-storage.CivilianFuelReserveBarrels);
                    shortage=consumption.Enabled!=0&&usable+.001f<required;
                }
            }
            if(em.HasComponent<SelectedUnitTag>(actor))defense.AcknowledgedGuidanceMask|=2;
            if((defense.AcknowledgedGuidanceMask&3)==3&&math.distancesq(position.xz,coverage.xz)<=4f&&!em.HasComponent<UnitPathRequest>(actor)&&!em.HasComponent<UnitPathFollow>(actor))defense.AcknowledgedGuidanceMask|=4;
            if((defense.AcknowledgedGuidanceMask&3)==3&&em.HasComponent<HoldPositionOrderTag>(actor)&&((defense.AcknowledgedGuidanceMask&4)!=0||shortage))defense.AcknowledgedGuidanceMask|=0x1FFu;
            em.SetComponentData(root,defense);
            int step=(defense.AcknowledgedGuidanceMask&1)==0?1:(defense.AcknowledgedGuidanceMask&2)==0?2:(defense.AcknowledgedGuidanceMask&4)==0?3:4;
            int id=step==3?65004:step==4?65008:65000+step;
            bool awaitingHold=step==4&&(defense.AcknowledgedGuidanceMask&0x1FFu)!=0x1FFu;
            if(awaitingHold)id=65006;
            FixedString64Bytes title=SteelTutorialPrefix;title.Append(step);title.Append(SteelTitleSuffix);
            FixedString128Bytes body=SteelTutorialPrefix;body.Append(step);body.Append(SteelBodySuffix);
            if(step==3&&shortage){title="mission.steel_push.tutorial.shortage.title";body="mission.steel_push.tutorial.shortage.body";}
            if(awaitingHold){title="mission.steel_push.tutorial.hold.title";body="mission.steel_push.tutorial.hold.body";}
            if(step==4&&facts.ElapsedMilliseconds>=55000)body=SteelMainBody;
            var next=new CampaignMissionGuidanceProjectionComponent{
                GuidanceId=id,Version=Next(current.Version),MissionSourceVersion=runtime.Version,
                Prompt=(CampaignMissionGuidancePromptKind)(78+step),GuidanceMode=NarrativeGuidanceMode.Full,Active=1,
                RecommendationKind=step==2?AssistantRecommendationKind.Select:step==3?(shortage?AssistantRecommendationKind.DefensiveAlert:AssistantRecommendationKind.Move):awaitingHold?AssistantRecommendationKind.DefensiveAlert:AssistantRecommendationKind.Explain,
                TargetKind=step==2?AssistantTargetKind.Squad:AssistantTargetKind.WorldPosition,SourceEntity=actor,TargetEntity=step==2?actor:Entity.Null,
                WorldPosition=shortage||awaitingHold?position:coverage,HasWorldPosition=1,Title=title,Body=body,ActionLabel=step==1?RadarContinue:step==4&&!awaitingHold?WaitAction:RadarAct,
                CanShow=1,CanExecute=step<4||awaitingHold?(byte)1:(byte)0,Priority=AssistantMessagePriority.High,
                SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled};
            if(!ProjectionEquals(in current,in next)||!current.Body.Equals(next.Body))em.SetComponentData(root,next);
            return true;
        }
    }
}

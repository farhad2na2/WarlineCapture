using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private bool TryAdvanceBreach(ref SystemState system,Entity root,in CampaignMissionRuntimeComponent runtime)
        {
            if(!SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog,in runtime,out int index)) return false;
            ref var definition=ref catalog.Blob.Value.Missions[index];
            if(definition.Breach.Enabled==0) return false;
            if(runtime.Outcome!=MissionOutcomeKind.None) return true;
            var em=system.EntityManager;
            if(!em.HasComponent<CampaignMissionBreachState>(root)) return true;
            var breach=em.GetComponentData<CampaignMissionBreachState>(root);
            if(!breach.SessionToken.Equals(runtime.SessionToken) || breach.AttemptOrdinal!=runtime.AttemptOrdinal || breach.SourceVersion!=runtime.SourceVersion) return true;
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            bool openingComplete=em.HasComponent<CampaignMissionOpeningPresentationComponent>(root) &&
                em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage>=6;
            if(facts.CommandSquadSpawned!=0)
            {
                PrepareBreachTargets(ref system,ref breach,ref definition.Breach,ref facts);
                if(breach.Ready==0) {breach.PreparationMilliseconds=SaturatingAddMilliseconds(breach.PreparationMilliseconds,SystemAPI.Time.DeltaTime);
                    if(breach.PreparationMilliseconds>30000) facts.HostileRosterIntegrityFault=1;}
                ProjectBreachRoster(em,root,in runtime,ref breach,ref definition.Breach,ref facts,SystemAPI.Time.DeltaTime);
            }
            if(breach.Ready!=0 && openingComplete && runtime.Phase==MissionPhaseKind.Engage && (breach.GuidanceCompletedMask&1)!=0)
            {
                facts.ElapsedMilliseconds=SaturatingAddMilliseconds(facts.ElapsedMilliseconds,SystemAPI.Time.DeltaTime);
                if(facts.ElapsedMilliseconds>=definition.Breach.DeadlineMilliseconds) breach.TimedOut=1;
            }
            facts.BreachGateDestroyed=breach.GateDestroyed;facts.BreachCoreDestroyed=breach.CoreDestroyed;
            facts.BreachArchiveSecured=breach.ArchiveSecured;facts.BreachSupportLost=breach.SupportLost;
            facts.BreachTimedOut=breach.TimedOut;facts.BreachSecureMilliseconds=breach.SecureHoldMilliseconds;
            em.SetComponentData(root,breach);em.SetComponentData(root,facts);
            if(CampaignMissionBreachRuleUtility.TryAdvance(in runtime,in facts,breach.Ready!=0,openingComplete,out var next)) em.SetComponentData(root,next);
            return true;
        }

        private void PrepareBreachTargets(ref SystemState system,ref CampaignMissionBreachState breach,
            ref CampaignMissionBreachDefinitionBlob definition,ref CampaignMissionAttemptFactsComponent facts)
        {
            if(!SystemAPI.TryGetSingletonEntity<BuildingRuntimeStateTag>(out var boundary) ||
                !SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) || !metadata.Blob.IsCreated) return;
            var em=system.EntityManager;
            if(!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary)) return;
            PrepareBreachTarget(em,boundary,metadata,definition.GateBuildingId,breach.GateCenter,definition.GateHealth,0,
                ref breach.GateRequestId,ref breach.Gate,ref breach.GateInitialized,ref facts);
            PrepareBreachTarget(em,boundary,metadata,definition.CoreBuildingId,breach.CoreCenter,definition.CoreHealth,0,
                ref breach.CoreRequestId,ref breach.Core,ref breach.CoreInitialized,ref facts);
            if(em.Exists(breach.Gate) && em.HasComponent<LocalTransform>(breach.Gate)) breach.GateCenter=em.GetComponentData<LocalTransform>(breach.Gate).Position;
            if(em.Exists(breach.Core) && em.HasComponent<LocalTransform>(breach.Core)) breach.CoreCenter=em.GetComponentData<LocalTransform>(breach.Core).Position;
        }

        private static void PrepareBreachTarget(EntityManager em,Entity boundary,OperationMapMetadataComponent metadata,
            FixedString128Bytes id,float3 center,int hitPoints,byte rotate,ref int requestId,ref Entity target,ref byte initialized,ref CampaignMissionAttemptFactsComponent facts)
        {
            if(initialized!=0) return;
            var requests=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            if(requestId==0)
            {
                requestId=1;for(int i=0;i<requests.Length;i++) requestId=math.max(requestId,requests[i].RequestId+1);
                requests.Add(new BuildingRuntimeSpawnRequest {RequestId=requestId,RequestKind=BuildingRuntimeSpawnRequest.KindBuilding,
                    FactionId=2,HasOwnerFaction=1,AllowNonBuildableEnemy=1,RequirePreferredOrigin=1,BuildingId=id,RotateVertical=rotate,PreferredOrigin=CampaignMissionSpawnSystem.ToGridCell(center,metadata.Blob.Value.Grid)});
                return;
            }
            int runtimeId=0;int2 actualOrigin=default;
            for(int i=0;i<requests.Length;i++) if(requests[i].RequestId==requestId)
            {
                if(requests[i].Status==BuildingRuntimeSpawnRequest.Failed) facts.HostileRosterIntegrityFault=1;
                if(requests[i].Status==BuildingRuntimeSpawnRequest.Succeeded) {runtimeId=requests[i].BuildingRuntimeId;actualOrigin=requests[i].ActualOrigin;}
                break;
            }
            if(runtimeId<=0) return;
            using var query=new EntityQueryBuilder(Allocator.Temp).WithAll<RuntimeBuildingCombatInfo,UnitHealth>().Build(em);
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)
            {
                var identity=em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                if(!MatchesBreachTarget(in identity,runtimeId,actualOrigin)) continue;
                target=entity;initialized=1;em.SetComponentData(entity,new UnitHealth {Current=hitPoints,Max=hitPoints});break;
            }
        }

        internal static void ProjectBreachRoster(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,
            ref CampaignMissionBreachState breach,ref CampaignMissionBreachDefinitionBlob definition,ref CampaignMissionAttemptFactsComponent facts,float deltaTime)
        {
            var members=em.GetBuffer<CampaignMissionBreachMember>(root);
            int initialized=0,friendlyAlive=0,friendlyLost=0,hostileAlive=0,hostileDead=0;bool atArchive=false,contested=false;
            for(int i=0;i<members.Length;i++)
            {
                var member=members[i];
                bool exists=em.Exists(member.Entity) && em.HasComponent<UnitHealth>(member.Entity);
                if(!exists && member.Dead==0) {facts.HostileRosterIntegrityFault=1;continue;}
                var health=exists?em.GetComponentData<UnitHealth>(member.Entity):default;
                if(health.Max>0) member.HealthInitialized=1;
                initialized+=member.HealthInitialized;
                if(member.HealthInitialized!=0 && health.Current<=0) member.Dead=1;
                if(member.Kind<=1) {if(member.Dead==0) friendlyAlive++;else friendlyLost++;if(member.Kind==1) breach.SupportLost=member.Dead;}
                else {if(member.Dead==0) hostileAlive++;else hostileDead++;}
                if(member.Dead==0 && em.HasComponent<LocalTransform>(member.Entity) && !em.HasComponent<UnitTransportPassenger>(member.Entity))
                {
                    bool near=math.distancesq(em.GetComponentData<LocalTransform>(member.Entity).Position.xz,breach.ArchiveCenter.xz)<=definition.ArchiveRadius*definition.ArchiveRadius;
                    if(member.Kind<=1) atArchive|=near;else contested|=near;
                }
                members[i]=member;
            }
            if(initialized==members.Length && members.Length>0 && breach.GateInitialized!=0 && breach.CoreInitialized!=0) breach.Ready=1;
            if(breach.Ready==0) return;
            facts.CommandSquadAlive=friendlyAlive>0?(byte)1:(byte)0;facts.SquadLossCount=friendlyLost;facts.HostileDefeatedCount=hostileDead;
            bool gateDead=IsBreachTargetDead(em,breach.Gate),coreDead=IsBreachTargetDead(em,breach.Core);
            if(gateDead) breach.GateDestroyed=1;
            if(breach.GateDestroyed!=0 && coreDead) breach.CoreDestroyed=1;
            bool gateHit=gateDead || em.HasComponent<UnitHealth>(breach.Gate) && em.GetComponentData<UnitHealth>(breach.Gate).Current<definition.GateHealth;
            if(gateHit && breach.CounterattackReleaseAtMilliseconds==0) breach.CounterattackReleaseAtMilliseconds=facts.ElapsedMilliseconds+definition.CounterattackWarningMilliseconds;
            if(breach.CounterattackReleaseAtMilliseconds>0 && facts.ElapsedMilliseconds>=breach.CounterattackReleaseAtMilliseconds) breach.CounterattackReleased=1;
            breach.Contested=contested?(byte)1:(byte)0;
            breach.FriendlyAtArchive=atArchive?(byte)1:(byte)0;
            if(runtime.Phase!=MissionPhaseKind.Engage || breach.ArchiveSecured!=0) return;
            bool canHold=breach.GateDestroyed!=0 && breach.CoreDestroyed!=0 && breach.CounterattackReleased!=0 && hostileAlive==0 && atArchive && !contested;
            breach.SecureHoldMilliseconds=canHold?SaturatingAddMilliseconds(breach.SecureHoldMilliseconds,deltaTime):0;
            if(breach.SecureHoldMilliseconds>=definition.SecureHoldMilliseconds) breach.ArchiveSecured=1;
        }
        internal static bool MatchesBreachTarget(in RuntimeBuildingCombatInfo identity,int runtimeId,int2 origin) =>
            identity.RuntimeBuildingId==runtimeId && identity.OwnerFactionId==2 && math.all(identity.OriginCell==origin);
        private static bool IsBreachTargetDead(EntityManager em,Entity entity) =>
            entity!=Entity.Null && (!em.Exists(entity) || em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Current<=0);
    }
}

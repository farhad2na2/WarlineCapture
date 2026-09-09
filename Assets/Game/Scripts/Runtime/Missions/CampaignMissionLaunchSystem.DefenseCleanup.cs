using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionLaunchSystem
    {
        private static void ResetDefenseAttemptState(EntityManager em, Entity root)
        {
            ClearBufferIfPresent<CampaignMissionDefenseMember>(em, root);
            ClearBufferIfPresent<CampaignMissionExtractionMember>(em, root);
            ResetComponentIfPresent<CampaignMissionExtractionState>(em, root);
            ClearBufferIfPresent<CampaignMissionConvoyElementState>(em, root);
            ClearBufferIfPresent<RadarPingRequest>(em, root);
            ClearBufferIfPresent<MissionDefenseInteractionRequest>(em, root);
            ClearBufferIfPresent<ThreatWarningRecord>(em, root);
            ClearBufferIfPresent<ThreatWarningObservation>(em, root);
            ResetComponentIfPresent<CampaignMissionDefenseStateComponent>(em, root);
            ResetComponentIfPresent<CampaignMissionCameraTourState>(em, root);
            ResetComponentIfPresent<ThreatWarningLedgerState>(em, root);
            ResetComponentIfPresent<RadarPingState>(em, root);
        }

        private static bool TryQueueDefenseAttemptCleanup(EntityManager em,ref EntityCommandBuffer cleanup,Entity root,
            in CampaignMissionCatalogComponent catalog,in CampaignMissionRuntimeComponent runtime)
        {
            if(!CampaignMissionSpawnSystem.TryFindDefinition(in catalog,in runtime,out int index) || catalog.Blob.Value.Missions[index].Defense.Enabled==0)
                return false;
            if(!em.HasComponent<CampaignMissionDefenseStateComponent>(root)) return true;
            var defense=em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
            if(defense.Initialized==0 || !defense.SessionToken.Equals(runtime.SessionToken) || defense.AttemptOrdinal!=runtime.AttemptOrdinal ||
                defense.SourceVersion!=runtime.SourceVersion) return true;
            using var boundaryQuery=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingRuntimeDeleteRequest),typeof(BuildingProducedUnitReadModel));
            if(boundaryQuery.CalculateEntityCount()!=1) return true;
            Entity boundary=boundaryQuery.GetSingletonEntity();
            var deletes=em.GetBuffer<BuildingRuntimeDeleteRequest>(boundary);
            using var buildingsQuery=new EntityQueryBuilder(Allocator.Temp).WithAll<RuntimeBuildingCombatInfo>()
                .WithNone<OperationMapBuildingComponent>().Build(em);
            using var buildings=buildingsQuery.ToComponentDataArray<RuntimeBuildingCombatInfo>(Allocator.Temp);
            foreach(var building in buildings)
            {
                if(building.RuntimeBuildingId<=defense.RuntimeBuildingBaselineId || !FactionIdentity.IsPlayerControlled(building.OwnerFactionId)) continue;
                int existing=-1;
                for(int i=0;i<deletes.Length;i++) if(deletes[i].BuildingRuntimeId==building.RuntimeBuildingId) {existing=i; break;}
                var request=new BuildingRuntimeDeleteRequest {BuildingRuntimeId=building.RuntimeBuildingId,ImmediateCleanup=1};
                if(existing>=0) deletes[existing]=request; else deletes.Add(request);
            }
            var produced=em.GetBuffer<BuildingProducedUnitReadModel>(boundary,true);
            if(defense.ProducedUnitBaselineCount<0 || defense.ProducedUnitBaselineCount>produced.Length) return true;
            using var seen=new NativeHashSet<Entity>(produced.Length+1,Allocator.Temp);
            for(int i=defense.ProducedUnitBaselineCount;i<produced.Length;i++)
            {
                var unit=produced[i];
                if(unit.HasOwnerFaction==0 || !FactionIdentity.IsPlayerControlled(unit.OwnerFactionId) || unit.BuildingRuntimeId<=defense.RuntimeBuildingBaselineId ||
                    !em.Exists(unit.Unit) || em.HasComponent<Prefab>(unit.Unit) || em.HasComponent<CampaignMissionUnitRoleComponent>(unit.Unit) || !seen.Add(unit.Unit)) continue;
                cleanup.DestroyEntity(unit.Unit);
            }
            return true;
        }
    }
}

using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private EntityQuery _gridlockBuildings, _gridlockGrid, _gridlockMap;
        private void CreateGridlockQueries(ref SystemState state)
        {
            _gridlockBuildings=state.GetEntityQuery(ComponentType.ReadOnly<RuntimeBuildingCombatInfo>());
            _gridlockGrid=new EntityQueryBuilder(Allocator.Temp).WithAll<GridConfig,GridWalkable,DynamicBlockerComponent>().Build(ref state);
            _gridlockMap=state.GetEntityQuery(ComponentType.ReadOnly<OperationMapMetadataComponent>());
        }

        private void AdvanceGridlockSites(EntityManager em,Entity root,Entity boundary,ref CampaignMissionGridlockDefinitionBlob rules,
            ref CampaignMissionGridlockState g,bool active,int delta)
        {
            if(!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary) || !em.HasBuffer<BuildingRuntimeDeleteRequest>(boundary)) return;
            for(int i=0;i<2;i++)
            {
                var site=em.GetBuffer<CampaignMissionGridlockWorkSite>(root)[i];
                if(site.Initialized==0)
                {
                    var spawns=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
                    if(site.SpawnRequestId==0)
                    {
                        int id=1;for(int n=0;n<spawns.Length;n++) id=math.max(id,spawns[n].RequestId+1);
                        site.SpawnRequestId=id;
                        spawns.Add(new BuildingRuntimeSpawnRequest {RequestId=id,RequestKind=BuildingRuntimeSpawnRequest.KindMissionRoadObstruction,
                            HasOwnerFaction=1,FactionId=0,AllowNonBuildableEnemy=1,RequirePreferredOrigin=1,
                            BuildingId=rules.ObstructionBuildingId,PreferredOrigin=site.ObstructionOrigin});
                    }
                    else
                    {
                        for(int n=0;n<spawns.Length;n++)
                        {
                            var request=spawns[n];if(request.RequestId!=site.SpawnRequestId) continue;
                            if(request.Status==BuildingRuntimeSpawnRequest.Failed) g.Failure=GridlockFailure.Integrity;
                            if(request.Status!=BuildingRuntimeSpawnRequest.Succeeded) break;
                            site.BuildingRuntimeId=request.BuildingRuntimeId;
                            using var buildings=_gridlockBuildings.ToEntityArray(Allocator.Temp);
                            foreach(var entity in buildings)
                            {
                                var identity=em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                                if(identity.RuntimeBuildingId!=site.BuildingRuntimeId || !math.all(identity.OriginCell==site.ObstructionOrigin)) continue;
                                site.Obstruction=entity;site.Initialized=1;break;
                            }
                            break;
                        }
                    }
                }
                site.Contested=GridlockHostileNear(em,root,site.Center,rules.ThreatRadius)?(byte)1:(byte)0;
                if(site.Initialized!=0 && site.Complete==0)
                {
                    bool exists=em.Exists(site.Obstruction) && em.HasComponent<RuntimeBuildingCombatInfo>(site.Obstruction);
                    if(!exists && site.ClearRequested==0) g.Failure=GridlockFailure.Integrity;
                    // Completion waits for both the building owner and grid cleanup owner.
                    bool removed=site.ClearRequested!=0 && !exists && GridlockFootprintClear(em,site.ObstructionOrigin,new int2(2,16));
                    bool fadi=false,worker=false;
                    var members=em.GetBuffer<CampaignMissionGridlockMember>(root);
                    for(int n=0;n<members.Length;n++)
                    {
                        var m=members[n];
                        if(m.Dead!=0 || m.HealthInitialized==0 || (m.Kind!=GridlockMemberKind.Fadi && m.Kind!=GridlockMemberKind.Worker) ||
                            !em.HasComponent<LocalTransform>(m.Entity) || em.HasComponent<UnitTransportPassenger>(m.Entity)) continue;
                        bool near=math.distancesq(em.GetComponentData<LocalTransform>(m.Entity).Position.xz,site.Center.xz)<=rules.WorkRadius*rules.WorkRadius;
                        if(m.Kind==GridlockMemberKind.Fadi) fadi|=near;else worker|=near;
                    }
                    bool eligible=active && (i==0 || em.GetBuffer<CampaignMissionGridlockWorkSite>(root)[0].Complete!=0);
                    site.Status=CampaignMissionGridlockRuleUtility.AdvanceWork(ref site.WorkMilliseconds,rules.WorkMilliseconds,delta,
                        eligible,fadi,worker,site.Contested!=0,site.ClearRequested!=0,removed);
                    if(removed) site.Complete=1;
                    else if(eligible && site.WorkMilliseconds>=rules.WorkMilliseconds && site.ClearRequested==0)
                    {
                        em.GetBuffer<BuildingRuntimeDeleteRequest>(boundary).Add(new BuildingRuntimeDeleteRequest {BuildingRuntimeId=site.BuildingRuntimeId,ImmediateCleanup=1});
                        site.ClearRequested=1;
                    }
                }
                var updatedSites=em.GetBuffer<CampaignMissionGridlockWorkSite>(root);
                updatedSites[i]=site;
            }
        }

        private bool GridlockFootprintClear(EntityManager em,int2 origin,int2 size)
        {
            if(_gridlockGrid.CalculateEntityCount()!=1) return false;
            var gridEntity=_gridlockGrid.GetSingletonEntity();var grid=em.GetComponentData<GridConfig>(gridEntity);
            var blocks=em.GetComponentData<DynamicBlockerComponent>(gridEntity);var walk=em.GetBuffer<GridWalkable>(gridEntity,true);
            for(int z=0;z<size.y;z++) for(int x=0;x<size.x;x++)
            {
                int2 cell=origin+new int2(x,z);if(!GridUtils.InBounds(cell,grid.Width,grid.Height)) return false;
                int index=GridUtils.CellToIndex(cell,grid.Width);
                if(index>=walk.Length || !blocks.Blocked.IsCreated || index>=blocks.Blocked.Length || walk[index].Value==0 || blocks.Blocked.IsSet(index)) return false;
            }
            return true;
        }

        private bool GridlockRouteClear(EntityManager em,ref CampaignMissionDefinitionBlob definition)
        {
            if(_gridlockMap.CalculateEntityCount()!=1) return false;
            var metadata=em.GetComponentData<OperationMapMetadataComponent>(_gridlockMap.GetSingletonEntity());if(!metadata.Blob.IsCreated) return false;
            ref var map=ref metadata.Blob.Value;
            for(int i=0;i<definition.PatrolRoutes.Length;i++)
            {
                ref var route=ref definition.PatrolRoutes[i];
                bool relief=false;
                for(int n=0;n<definition.ForceGroups.Length;n++)
                {
                    ref var group=ref definition.ForceGroups[n];if(!group.GroupId.Equals(route.UnitGroupId)) continue;
                    for(int u=0;u<group.Units.Length;u++) relief|=group.Units[u].MissionRoleId.Equals(definition.Gridlock.VehicleRoleId);
                }
                if(!relief) continue;
                if(route.AnchorIds.Length<2) return false;
                for(int n=1;n<route.AnchorIds.Length;n++)
                {
                    if(!CampaignMissionSpawnSystem.TryFindAnchor(ref map,route.AnchorIds[n-1],out var a) ||
                        !CampaignMissionSpawnSystem.TryFindAnchor(ref map,route.AnchorIds[n],out var b)) return false;
                    int2 from=CampaignMissionSpawnSystem.ToGridCell(a.Position,map.Grid),to=CampaignMissionSpawnSystem.ToGridCell(b.Position,map.Grid);
                    int steps=math.max(math.abs(to.x-from.x),math.abs(to.y-from.y));
                    for(int k=0;k<=steps;k++)
                    {
                        int2 cell=(int2)math.round(math.lerp((float2)from,(float2)to,steps==0?0:(float)k/steps));
                        if(!GridlockFootprintClear(em,cell-new int2(1),new int2(3))) return false;
                    }
                }
                return true;
            }
            return false;
        }

        private bool GridlockRouteContested(EntityManager em,Entity root,ref CampaignMissionDefinitionBlob definition)
        {
            var sites=em.GetBuffer<CampaignMissionGridlockWorkSite>(root);
            for(int i=0;i<sites.Length;i++)
                if(sites[i].Complete!=0 && GridlockHostileNear(em,root,sites[i].Center,definition.Gridlock.ThreatRadius)) return true;
            return false;
        }
    }
}

using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private bool TryAdvanceGridlock(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime)
        {
            if(!SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog,in runtime,out int index)) return false;
            ref var definition=ref catalog.Blob.Value.Missions[index];
            if(definition.Gridlock.Enabled==0) return false;
            if(runtime.Outcome!=MissionOutcomeKind.None) return true;
            var em=system.EntityManager;
            if(!em.HasComponent<CampaignMissionGridlockState>(root)) return true;
            var g=em.GetComponentData<CampaignMissionGridlockState>(root);
            if(!g.SessionToken.Equals(runtime.SessionToken) || g.AttemptOrdinal!=runtime.AttemptOrdinal || g.SourceVersion!=runtime.SourceVersion) return true;
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            ref var rules=ref definition.Gridlock;
            bool opening=em.HasComponent<CampaignMissionOpeningPresentationComponent>(root) &&
                em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage>=6;
            bool active=opening && runtime.Phase==MissionPhaseKind.Engage && g.Ready!=0;
            int delta=(int)math.round(math.max(0,SystemAPI.Time.DeltaTime)*1000);
            ProjectGridlockRoster(em,root,in runtime,ref g,ref facts);
            if(facts.CommandSquadSpawned!=0)
            {
                if(SystemAPI.TryGetSingletonEntity<BuildingRuntimeStateTag>(out var boundary))
                    AdvanceGridlockSites(em,root,boundary,ref rules,ref g,active,delta);
                if(g.Ready==0)
                {
                    g.PreparationMilliseconds=SaturatingAddMilliseconds(g.PreparationMilliseconds,SystemAPI.Time.DeltaTime);
                    if(g.PreparationMilliseconds>=30000) g.Failure=GridlockFailure.Integrity;
                }
            }
            var sites=em.GetBuffer<CampaignMissionGridlockWorkSite>(root);
            bool complete=sites.Length==2 && sites[0].Complete!=0 && sites[1].Complete!=0;
            if(active)
            {
                g.ElapsedMilliseconds=SaturatingAddMilliseconds(g.ElapsedMilliseconds,SystemAPI.Time.DeltaTime);
                if(sites[0].Complete!=0 && g.CounterattackWarned==0)
                {g.CounterattackWarned=1;g.CounterattackReleaseAtMilliseconds=g.ElapsedMilliseconds+rules.WarningMilliseconds;}
                g.RouteConnected=complete && GridlockRouteClear(em,ref definition)?(byte)1:(byte)0;
                g.RouteContested=GridlockRouteContested(em,root,ref definition)?(byte)1:(byte)0;
                bool arrived=g.LivingVehicle!=0 && em.HasComponent<LocalTransform>(g.Vehicle) &&
                    math.distancesq(em.GetComponentData<LocalTransform>(g.Vehicle).Position.xz,g.HospitalCenter.xz)<=rules.HospitalRadius*rules.HospitalRadius;
                g.VehicleArrived=arrived?(byte)1:(byte)0;
                bool hospitalContested=GridlockHostileNear(em,root,g.HospitalCenter,rules.ThreatRadius);
                CampaignMissionGridlockRuleUtility.AdvanceHold(ref g.HoldMilliseconds,rules.HoldMilliseconds,delta,true,
                    complete,g.RouteConnected!=0,arrived,hospitalContested);
            }
            g.Failure=CampaignMissionGridlockRuleUtility.Failure(g,rules.DeadlineMilliseconds);
            bool victory=CampaignMissionGridlockRuleUtility.IsVictory(g,complete,rules.HoldMilliseconds,rules.DeadlineMilliseconds);
            if(victory) g.Delivered=1;
            facts.ElapsedMilliseconds=g.ElapsedMilliseconds;
            facts.GridlockSiteAComplete=sites.Length==2?sites[0].Complete:(byte)0;
            facts.GridlockSiteBComplete=sites.Length==2?sites[1].Complete:(byte)0;
            facts.GridlockDelivered=g.Delivered;facts.GridlockFailure=g.Failure;
            em.SetComponentData(root,g);em.SetComponentData(root,facts);
            UpdateGridlockReserve(em, root, g);
            // Integrity failures are retryable diagnostics, never settled wins or invented deaths.
            if(g.Failure==GridlockFailure.Integrity) return true;
            var phase=runtime.Phase;var outcome=MissionOutcomeKind.None;
            if(phase==MissionPhaseKind.Preparing && (runtime.ReadyReadiness&runtime.RequiredReadiness)==runtime.RequiredReadiness) phase=MissionPhaseKind.InteractiveBrief;
            else if(phase==MissionPhaseKind.InteractiveBrief && facts.InteractiveBriefCompleted!=0) phase=MissionPhaseKind.FindSquad;
            else if(phase>=MissionPhaseKind.FindSquad && g.Ready!=0 && g.Failure!=GridlockFailure.None) {phase=MissionPhaseKind.Result;outcome=MissionOutcomeKind.Defeat;}
            else if(phase==MissionPhaseKind.FindSquad && g.Ready!=0 && opening) phase=MissionPhaseKind.Engage;
            else if(phase==MissionPhaseKind.Engage && victory) {phase=MissionPhaseKind.Result;outcome=MissionOutcomeKind.Victory;}
            if(phase!=runtime.Phase && TryTransition(runtime,phase,outcome,outcome==MissionOutcomeKind.None?
                MissionReturnDestinationKind.None:MissionReturnDestinationKind.CampaignOperations,out var next)) em.SetComponentData(root,next);
            return true;
        }

        private static void ProjectGridlockRoster(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,
            ref CampaignMissionGridlockState g,ref CampaignMissionAttemptFactsComponent facts)
        {
            var members=em.GetBuffer<CampaignMissionGridlockMember>(root);
            g.LivingFadi=g.LivingWorkers=g.LivingRifles=g.LivingVehicle=0;
            int initialized=0,deadRifles=0,deadCivilians=0,deadHostiles=0;
            for(int i=0;i<members.Length;i++)
            {
                var member=members[i];
                if(member.Dead==0)
                {
                    bool exists=em.Exists(member.Entity) && em.HasComponent<UnitHealth>(member.Entity) &&
                        em.HasComponent<CampaignMissionUnitRoleComponent>(member.Entity) &&
                        em.GetComponentData<CampaignMissionUnitRoleComponent>(member.Entity).SessionToken.Equals(runtime.SessionToken);
                    if(!exists)
                    {
                        // Prefab initialization may add health after the spawn boundary.
                        // Once observed, disappearing identity is an integrity fault; before
                        // that, the bounded preparation timer owns the diagnostic.
                        if(member.HealthInitialized!=0 || g.Ready!=0) g.Failure=GridlockFailure.Integrity;
                        continue;
                    }
                    var health=em.GetComponentData<UnitHealth>(member.Entity);
                    if(health.Max>0) {member.HealthInitialized=1;if(health.Current<=0) member.Dead=1;}
                }
                initialized+=member.HealthInitialized;
                if(member.Dead!=0)
                {
                    if(member.Kind==GridlockMemberKind.Rifle) deadRifles++;
                    else if(member.Kind==GridlockMemberKind.Fadi || member.Kind==GridlockMemberKind.Worker) deadCivilians++;
                    else if(member.Kind>=GridlockMemberKind.Hostile) deadHostiles++;
                }
                else if(member.HealthInitialized!=0)
                {
                    if(member.Kind==GridlockMemberKind.Rifle) g.LivingRifles++;
                    else if(member.Kind==GridlockMemberKind.Fadi) g.LivingFadi++;
                    else if(member.Kind==GridlockMemberKind.Worker) g.LivingWorkers++;
                    else if(member.Kind==GridlockMemberKind.ReliefVehicle) g.LivingVehicle++;
                }
                members[i]=member;
            }
            var sites=em.GetBuffer<CampaignMissionGridlockWorkSite>(root);
            if(initialized==members.Length && members.Length==22 && sites.Length==2 && sites[0].Initialized!=0 && sites[1].Initialized!=0) g.Ready=1;
            facts.CommandSquadAlive=g.LivingRifles>0?(byte)1:(byte)0;
            facts.SquadLossCount=deadRifles;facts.CivilianTotalCount=3;facts.CivilianLossCount=deadCivilians;facts.HostileDefeatedCount=deadHostiles;
        }

        private static bool GridlockHostileNear(EntityManager em,Entity root,float3 center,float radius)
        {
            var members=em.GetBuffer<CampaignMissionGridlockMember>(root);
            for(int i=0;i<members.Length;i++)
            {
                var m=members[i];
                if(m.Kind<GridlockMemberKind.Hostile || m.Dead!=0 || m.HealthInitialized==0 ||
                    em.HasComponent<Disabled>(m.Entity) || !em.HasComponent<LocalTransform>(m.Entity)) continue;
                if(math.distancesq(em.GetComponentData<LocalTransform>(m.Entity).Position.xz,center.xz)<=radius*radius) return true;
            }
            return false;
        }
    }
}

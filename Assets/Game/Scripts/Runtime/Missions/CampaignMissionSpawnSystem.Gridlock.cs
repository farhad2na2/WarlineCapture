using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        private static void InitializeGridlockAttempt(EntityManager em, Entity root, ref CampaignMissionDefinitionBlob definition,
            ref OperationMapBlob map, in CampaignMissionRuntimeComponent runtime)
        {
            if(definition.Gridlock.Enabled==0) return;
            ref var g=ref definition.Gridlock;
            TryFindAnchor(ref map,g.HospitalAnchorId,out var hospital);
            SetOrAdd(em,root,new CampaignMissionGridlockState {SessionToken=runtime.SessionToken,
                AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion,HospitalCenter=hospital.Position});
            if(!em.HasBuffer<CampaignMissionGridlockMember>(root)) em.AddBuffer<CampaignMissionGridlockMember>(root);
            em.GetBuffer<CampaignMissionGridlockMember>(root).Clear();
            if(!em.HasBuffer<CampaignMissionGridlockWorkSite>(root)) em.AddBuffer<CampaignMissionGridlockWorkSite>(root);
            var sites=em.GetBuffer<CampaignMissionGridlockWorkSite>(root);sites.Clear();
            for(int i=0;i<2;i++)
            {
                TryFindAnchor(ref map,i==0?g.SiteAAnchorId:g.SiteBAnchorId,out var work);
                TryFindAnchor(ref map,i==0?g.ObstructionAAnchorId:g.ObstructionBAnchorId,out var obstacle);
                sites.Add(new CampaignMissionGridlockWorkSite {Center=work.Position,ObstructionOrigin=ToGridCell(obstacle.Position,map.Grid)});
            }
        }

        private static void RegisterGridlockMember(EntityManager em,Entity root,Entity instance,
            ref CampaignMissionDefinitionBlob definition,ref CampaignMissionForceGroupBlob group,in CampaignMissionForceUnitBlob unit)
        {
            if(definition.Gridlock.Enabled==0) return;
            ref var g=ref definition.Gridlock;
            GridlockMemberKind kind=unit.MissionRoleId.Equals(g.FadiRoleId)?GridlockMemberKind.Fadi:
                unit.MissionRoleId.Equals(g.WorkerRoleId)?GridlockMemberKind.Worker:
                unit.MissionRoleId.Equals(g.VehicleRoleId)?GridlockMemberKind.ReliefVehicle:
                unit.MissionRoleId.Equals(g.CounterattackRoleId)?GridlockMemberKind.Counterattack:
                group.FactionId==1?GridlockMemberKind.Rifle:GridlockMemberKind.Hostile;
            em.GetBuffer<CampaignMissionGridlockMember>(root).Add(new CampaignMissionGridlockMember {Entity=instance,Kind=kind});
            if(kind==GridlockMemberKind.ReliefVehicle)
            {
                var state=em.GetComponentData<CampaignMissionGridlockState>(root);state.Vehicle=instance;em.SetComponentData(root,state);
                // This mission supplies the relief run's fuel; no account Fuel or refinery is required.
                if(em.HasComponent<UnitFuelConsumption>(instance)) {var fuel=em.GetComponentData<UnitFuelConsumption>(instance);fuel.Enabled=0;em.SetComponentData(instance,fuel);}
            }
            if(kind==GridlockMemberKind.ReliefVehicle || kind==GridlockMemberKind.Counterattack)
                SetOrAdd(em,instance,new CampaignMissionConvoyRouteProgress());
        }
    }
}

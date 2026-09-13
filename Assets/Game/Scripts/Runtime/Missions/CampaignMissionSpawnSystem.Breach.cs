using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        private static void InitializeBreachAttempt(EntityManager em,Entity root,ref CampaignMissionDefinitionBlob definition,
            ref OperationMapBlob map,in CampaignMissionRuntimeComponent runtime)
        {
            if(definition.Breach.Enabled==0) return;
            TryFindAnchor(ref map,definition.Breach.ApproachAnchorId,out var approach);
            TryFindAnchor(ref map,definition.Breach.GateAnchorId,out var gate);
            TryFindAnchor(ref map,definition.Breach.CoreAnchorId,out var core);
            TryFindAnchor(ref map,definition.Breach.ArchiveAnchorId,out var archive);
            SetOrAdd(em,root,new CampaignMissionBreachState {SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,
                SourceVersion=runtime.SourceVersion,DeadlineMilliseconds=definition.Breach.DeadlineMilliseconds,SecureRequiredMilliseconds=definition.Breach.SecureHoldMilliseconds,ApproachCenter=approach.Position,GateCenter=gate.Position,CoreCenter=core.Position,ArchiveCenter=archive.Position});
            SetOrAdd(em,root,new CampaignMissionCameraTourState {SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion});
            if(!em.HasBuffer<CampaignMissionBreachMember>(root)) em.AddBuffer<CampaignMissionBreachMember>(root);
            em.GetBuffer<CampaignMissionBreachMember>(root).Clear();
        }
        private static void RegisterBreachMember(EntityManager em,Entity root,Entity instance,
            ref CampaignMissionDefinitionBlob definition,ref CampaignMissionForceGroupBlob group,in CampaignMissionForceUnitBlob unit)
        {
            if(definition.Breach.Enabled==0) return;
            byte kind=unit.MissionRoleId.Equals(definition.Breach.CounterattackRoleId)?(byte)3:
                group.FactionId>FactionIdentity.PlayerFactionId?(byte)2:unit.MissionRoleId.Equals(definition.Breach.SupportRoleId)?(byte)1:(byte)0;
            em.GetBuffer<CampaignMissionBreachMember>(root).Add(new CampaignMissionBreachMember {Entity=instance,Kind=kind});
            if(kind==1)
            {
                var breach=em.GetComponentData<CampaignMissionBreachState>(root);breach.Support=instance;em.SetComponentData(root,breach);
                if(em.HasComponent<UnitFuelConsumption>(instance)) {var fuel=em.GetComponentData<UnitFuelConsumption>(instance);fuel.Enabled=0;em.SetComponentData(instance,fuel);}
            }
            if(kind>=2 && !em.HasComponent<CampaignMissionConvoyRouteProgress>(instance)) em.AddComponent<CampaignMissionConvoyRouteProgress>(instance);
        }
    }
}

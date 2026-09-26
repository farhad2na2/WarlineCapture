using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        private static void InitializePowerRelayAttempt(EntityManager em,Entity root,ref CampaignMissionDefinitionBlob definition,ref OperationMapBlob map,in CampaignMissionRuntimeComponent runtime)
        {
            if(definition.PowerRelay.Enabled==0)return;
            TryFindAnchor(ref map,definition.PowerRelay.ShortRouteAnchorId,out var shortRoute);TryFindAnchor(ref map,definition.PowerRelay.SafeRouteAnchorId,out var safeRoute);
            TryFindAnchor(ref map,definition.PowerRelay.ShelterAnchorId,out var shelter);TryFindAnchor(ref map,definition.PowerRelay.RepairAnchorId,out var repair);
            SetOrAdd(em,root,new CampaignMissionPowerRelayState {ShortRouteCell=ToGridCell(shortRoute.Position,map.Grid),SafeRouteCell=ToGridCell(safeRoute.Position,map.Grid),ShelterCell=ToGridCell(shelter.Position,map.Grid),RepairCell=ToGridCell(repair.Position,map.Grid),SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion});
            SetOrAdd(em,root,new CampaignMissionCameraTourState {SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion});
            if(!em.HasBuffer<CampaignMissionPowerRelayMember>(root))em.AddBuffer<CampaignMissionPowerRelayMember>(root);
            em.GetBuffer<CampaignMissionPowerRelayMember>(root).Clear();
        }
        private static void RegisterPowerRelayMember(EntityManager em,Entity root,Entity instance,ref CampaignMissionDefinitionBlob definition,ref CampaignMissionForceGroupBlob group,in CampaignMissionForceUnitBlob unit)
        {
            if(definition.PowerRelay.Enabled==0)return;
            byte kind=group.FactionId==2?(byte)4:unit.MissionRoleId.Equals(new FixedString64Bytes("role.power.engineer"))?(byte)1:
                unit.MissionRoleId.Equals(new FixedString64Bytes("role.power.family_convoy"))?(byte)2:
                unit.MissionRoleId.Equals(new FixedString64Bytes("role.power.fuel_service"))?(byte)3:(byte)0;
            // The mission brief and route correction are blocking story beats. Keep both
            // factions inert until the runtime explicitly hands control to the player.
            if(!em.HasComponent<CampaignMissionCombatSuppressedTag>(instance))
                em.AddComponent<CampaignMissionCombatSuppressedTag>(instance);
            if((kind==2||kind==3)&&em.HasComponent<UnitFuelConsumption>(instance)){var fuel=em.GetComponentData<UnitFuelConsumption>(instance);fuel.Enabled=0;em.SetComponentData(instance,fuel);}
            em.GetBuffer<CampaignMissionPowerRelayMember>(root).Add(new CampaignMissionPowerRelayMember {Entity=instance,Kind=kind});
        }
    }
}

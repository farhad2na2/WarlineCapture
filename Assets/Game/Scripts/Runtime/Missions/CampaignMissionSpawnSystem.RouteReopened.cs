using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        private static void InitializeRouteReopenedAttempt(EntityManager em,Entity root,ref CampaignMissionDefinitionBlob definition,ref OperationMapBlob map,in CampaignMissionRuntimeComponent runtime)
        {
            if(definition.RouteReopened.Enabled==0)return;
            TryFindAnchor(ref map,definition.RouteReopened.ReliefGoalAnchorId,out var relief);TryFindAnchor(ref map,definition.RouteReopened.FuelGoalAnchorId,out var fuel);
            TryFindAnchor(ref map,definition.RouteReopened.DisruptedLinkAnchorId,out var link);TryFindAnchor(ref map,definition.RouteReopened.HubGateAnchorId,out var gate);TryFindAnchor(ref map,definition.RouteReopened.RecordsAnchorId,out var records);
            SetOrAdd(em,root,new CampaignMissionRouteReopenedState {ReliefGoalCell=ToGridCell(relief.Position,map.Grid),FuelGoalCell=ToGridCell(fuel.Position,map.Grid),DisruptedLinkCell=ToGridCell(link.Position,map.Grid),HubGateCell=ToGridCell(gate.Position,map.Grid),RecordsCell=ToGridCell(records.Position,map.Grid),SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion});
            SetOrAdd(em,root,new CampaignMissionCameraTourState {SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion});
            if(!em.HasBuffer<CampaignMissionRouteReopenedMember>(root))em.AddBuffer<CampaignMissionRouteReopenedMember>(root);
            em.GetBuffer<CampaignMissionRouteReopenedMember>(root).Clear();
        }
        private static void RegisterRouteReopenedMember(EntityManager em,Entity root,Entity instance,ref CampaignMissionDefinitionBlob definition,ref CampaignMissionForceGroupBlob group,in CampaignMissionForceUnitBlob unit)
        {
            if(definition.RouteReopened.Enabled==0)return;
            byte kind=group.FactionId==2?(byte)4:unit.MissionRoleId.Equals(new FixedString64Bytes("role.route.engineer"))?(byte)1:
                unit.MissionRoleId.Equals(new FixedString64Bytes("role.route.relief_convoy"))?(byte)2:
                unit.MissionRoleId.Equals(new FixedString64Bytes("role.route.fuel_convoy"))?(byte)3:(byte)0;
            // The force-split briefing is blocking. Keep both factions inert until control passes to the player.
            if(!em.HasComponent<CampaignMissionCombatSuppressedTag>(instance))
                em.AddComponent<CampaignMissionCombatSuppressedTag>(instance);
            if((kind==2||kind==3)&&em.HasComponent<UnitFuelConsumption>(instance)){var fuel=em.GetComponentData<UnitFuelConsumption>(instance);fuel.Enabled=0;em.SetComponentData(instance,fuel);}
            em.GetBuffer<CampaignMissionRouteReopenedMember>(root).Add(new CampaignMissionRouteReopenedMember {Entity=instance,Kind=kind});
        }
    }
}

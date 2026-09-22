using Game.Components;
using Unity.Entities;
namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        private static void InitializeSupplyLineAttempt(EntityManager em,Entity root,ref CampaignMissionDefinitionBlob definition,ref OperationMapBlob map,in CampaignMissionRuntimeComponent runtime)
        {
            if(definition.SupplyLine.Enabled==0)return;
            SetOrAdd(em,root,default(CampaignMissionSupplyLineAllocationRequest));
            TryFindAnchor(ref map,definition.SupplyLine.AlternateLaneAnchorId,out var alternate);
            SetOrAdd(em,root,new CampaignMissionSupplyLineState {AlternateLane=ToGridCell(alternate.Position,map.Grid),SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion});
            if(!em.HasBuffer<CampaignMissionSupplyLineMember>(root))em.AddBuffer<CampaignMissionSupplyLineMember>(root);
            em.GetBuffer<CampaignMissionSupplyLineMember>(root).Clear();
            if(!em.HasBuffer<CampaignMissionSupplyLineLink>(root))em.AddBuffer<CampaignMissionSupplyLineLink>(root);
            var links=em.GetBuffer<CampaignMissionSupplyLineLink>(root);links.Clear();ref var s=ref definition.SupplyLine;
            for(int i=0;i<3;i++)
            {
                TryFindAnchor(ref map,i==0?s.OilAnchorId:i==1?s.RefineryAnchorId:s.StorageAnchorId,out var anchor);
                links.Add(new CampaignMissionSupplyLineLink {Origin=ToGridCell(anchor.Position,map.Grid),BuildingId=i==0?s.OilBuildingId:i==1?s.RefineryBuildingId:s.StorageBuildingId});
            }
        }
        private static void RegisterSupplyLineMember(EntityManager em,Entity root,Entity instance,ref CampaignMissionDefinitionBlob definition,ref CampaignMissionForceGroupBlob group,in CampaignMissionForceUnitBlob unit)
        {
            if(definition.SupplyLine.Enabled==0)return;
            byte kind=group.FactionId==2?(byte)3:unit.MissionRoleId.Equals(new Unity.Collections.FixedString64Bytes("role.supply.oil_hauler"))?(byte)1:unit.MissionRoleId.Equals(new Unity.Collections.FixedString64Bytes("role.supply.fuel_hauler"))?(byte)2:(byte)0;
            em.GetBuffer<CampaignMissionSupplyLineMember>(root).Add(new CampaignMissionSupplyLineMember {Entity=instance,Kind=kind});
        }
    }
}

using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        private static void InitializeMarketLifelineAttempt(EntityManager em,Entity root,ref CampaignMissionDefinitionBlob definition,ref OperationMapBlob map,in CampaignMissionRuntimeComponent runtime)
        {
            if(definition.MarketLifeline.Enabled==0)return;
            TryFindAnchor(ref map,definition.MarketLifeline.DeliveryAnchorId,out var delivery);
            TryFindAnchor(ref map,definition.MarketLifeline.ManifestAnchorId,out var manifest);
            TryFindAnchor(ref map,definition.MarketLifeline.CorruptManifestAnchorId,out var corruptManifest);
            SetOrAdd(em,root,new CampaignMissionMarketLifelineState {DeliveryCell=ToGridCell(delivery.Position,map.Grid),ManifestCell=ToGridCell(manifest.Position,map.Grid),CorruptManifestCell=ToGridCell(corruptManifest.Position,map.Grid),SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion});
            SetOrAdd(em,root,new CampaignMissionCameraTourState {SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion});
            if(!em.HasBuffer<CampaignMissionMarketLifelineMember>(root))em.AddBuffer<CampaignMissionMarketLifelineMember>(root);
            em.GetBuffer<CampaignMissionMarketLifelineMember>(root).Clear();
        }
        private static void RegisterMarketLifelineMember(EntityManager em,Entity root,Entity instance,ref CampaignMissionDefinitionBlob definition,ref CampaignMissionForceGroupBlob group,in CampaignMissionForceUnitBlob unit)
        {
            if(definition.MarketLifeline.Enabled==0)return;
            byte kind=group.FactionId==2?(byte)2:unit.MissionRoleId.Equals(new FixedString64Bytes("role.market.relief_convoy"))?(byte)1:
                unit.MissionRoleId.Equals(new FixedString64Bytes("role.market.corrupt_transfer"))?(byte)3:(byte)0;
            if((kind==1||kind==3) && em.HasComponent<UnitFuelConsumption>(instance)){var fuel=em.GetComponentData<UnitFuelConsumption>(instance);fuel.Enabled=0;em.SetComponentData(instance,fuel);}
            em.GetBuffer<CampaignMissionMarketLifelineMember>(root).Add(new CampaignMissionMarketLifelineMember {Entity=instance,Kind=kind});
        }
    }
}

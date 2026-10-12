using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    // A planning preflight only. The placement boundary remains authoritative, and
    // authored starting grants never pass through this generic AI plan filter.
    internal static class CampaignAIBuildEligibility
    {
        internal static bool ReadAllowed(EntityManager em, NativeList<FixedString128Bytes> allowed)
        {
            allowed.Clear();
            using var query=em.CreateEntityQuery(typeof(CampaignMissionRuntimeComponent));
            int count=query.CalculateEntityCount();
            if(count==0)return false;
            if(count!=1)return true;
            var entity=query.GetSingletonEntity();
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(entity);
            if(runtime.Phase==MissionPhaseKind.None||runtime.MissionId.IsEmpty)return false;
            if(!em.HasComponent<CampaignMissionCatalogComponent>(entity))return true;
            var catalog=em.GetComponentData<CampaignMissionCatalogComponent>(entity);
            if(!catalog.Blob.IsCreated||runtime.SourceVersion==0||runtime.SourceVersion!=catalog.SourceVersion)return true;
            for(int i=0;i<catalog.Blob.Value.Missions.Length;i++)
            {
                ref var mission=ref catalog.Blob.Value.Missions[i];
                if(!mission.MissionId.Equals(runtime.MissionId))continue;
                if(mission.MissionRuntimeEnabled==0)return false;
                if(runtime.MissionId.Equals(new FixedString64Bytes("saga.ch05.m01.citywide_alert")))
                {
                    // Match the exact prefab set accepted by IsAllowedCitywideStartingLot.
                    allowed.Add(new FixedString128Bytes("building_citywide_clinic"));
                    allowed.Add(new FixedString128Bytes("building_citywide_utility"));
                    allowed.Add(new FixedString128Bytes("building_barrack"));
                    allowed.Add(new FixedString128Bytes("building_citywide_reservedepot"));
                    return true;
                }
                // No legal build zone means the boundary rejects every generic build.
                if(mission.BuildZone.AnchorId.IsEmpty||mission.BuildZone.HalfWidthCells<=0||mission.BuildZone.HalfHeightCells<=0)return true;
                for(int j=0;j<mission.BuildCatalog.Length;j++)
                {
                    ref var entry=ref mission.BuildCatalog[j];
                    if(entry.MaxCount>0&&!entry.BuildingConfigId.IsEmpty)
                        { FixedString128Bytes id=entry.BuildingConfigId;allowed.Add(id.ToLowerAscii()); }
                }
                return true;
            }
            return true;
        }
        internal static bool Contains(NativeArray<FixedString128Bytes> allowed,FixedString128Bytes id)
        {
            for(int i=0;i<allowed.Length;i++)if(allowed[i].Equals(id))return true;
            return false;
        }
    }
}

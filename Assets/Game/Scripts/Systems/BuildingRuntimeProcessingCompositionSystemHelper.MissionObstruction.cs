using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Runtime
{
    public sealed partial class BuildingRuntimeProcessingCompositionSystemHelper
    {
        private static bool IsRegisteredMissionRoadObstruction(EntityManager em,in BuildingRuntimeSpawnRequest request,
            BuildingDefinition definition,BuildingRuntimeSpawnCompositionSystemHelper.Context context)
        {
            if(request.HasOwnerFaction==0 || request.FactionId!=0 || request.RequirePreferredOrigin==0 || definition==null ||
                !context.TryGetGridData(out _,out var grid,out _,out _)) return false;
            using var roots=em.CreateEntityQuery(typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionGridlockState),typeof(CampaignMissionGridlockWorkSite));
            if(roots.CalculateEntityCount()!=1)return false;
            var root=roots.GetSingletonEntity();var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            var state=em.GetComponentData<CampaignMissionGridlockState>(root);
            if(!state.SessionToken.Equals(runtime.SessionToken) || state.AttemptOrdinal!=runtime.AttemptOrdinal || state.SourceVersion!=runtime.SourceVersion ||
                !em.HasComponent<CampaignMissionCatalogComponent>(root)) return false;
            var catalog=em.GetComponentData<CampaignMissionCatalogComponent>(root);
            if(!CampaignMissionSpawnSystem.TryFindDefinition(in catalog,in runtime,out int index)) return false;
            ref var rule=ref catalog.Blob.Value.Missions[index].Gridlock;
            if(rule.Enabled==0 || !rule.ObstructionBuildingId.Equals(request.BuildingId))return false;
            bool registered=false;
            foreach(var site in em.GetBuffer<CampaignMissionGridlockWorkSite>(root,true))
                registered|=site.SpawnRequestId==request.RequestId && site.Initialized==0 && math.all(site.ObstructionOrigin==request.PreferredOrigin);
            if(!registered)return false;
            using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
            if(surfaces.CalculateEntityCount()!=1)return false;
            var blob=surfaces.GetSingleton<MapSurfaceComponent>().SurfaceBlob;
            if(!blob.IsCreated)return false;
            RectInt rect=context.GetEffectivePlacementRect(definition,new Vector2Int(request.PreferredOrigin.x,request.PreferredOrigin.y),grid,false);
            for(int z=rect.yMin;z<rect.yMax;z++) for(int x=rect.xMin;x<rect.xMax;x++)
                if(!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(x,z),out var sample) ||
                    (sample.MovementMask&MapSurfaceMovementMask.Infantry)==0)return false;
            return true;
        }
    }
}

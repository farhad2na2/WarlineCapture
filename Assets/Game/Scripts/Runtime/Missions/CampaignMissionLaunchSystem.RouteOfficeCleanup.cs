using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionLaunchSystem
    {
        private static bool TryQueueRouteOfficeCleanup(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime)
        {
            if(!em.HasComponent<CampaignMissionRouteReopenedState>(root))return false;
            var office=em.GetComponentData<CampaignMissionRouteReopenedState>(root);
            if(!office.SessionToken.Equals(runtime.SessionToken)||office.AttemptOrdinal!=runtime.AttemptOrdinal)return false;
            using var query=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingRuntimeSpawnRequest),typeof(BuildingRuntimeDeleteRequest));
            if(query.CalculateEntityCount()!=1)return true;
            var boundary=query.GetSingletonEntity();
            var deletes=em.GetBuffer<BuildingRuntimeDeleteRequest>(boundary);
            foreach(var request in em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary,true))
            {
                if(request.RequestId!=office.RecordsSpawnRequestId||request.Status!=BuildingRuntimeSpawnRequest.Succeeded||request.BuildingRuntimeId<=0)continue;
                for(int i=0;i<deletes.Length;i++)if(deletes[i].BuildingRuntimeId==request.BuildingRuntimeId)return true;
                deletes.Add(new BuildingRuntimeDeleteRequest{BuildingRuntimeId=request.BuildingRuntimeId,ImmediateCleanup=1});
            }
            return true;
        }
    }
}

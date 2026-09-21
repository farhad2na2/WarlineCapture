using Game.Components;
using Unity.Collections;
using Unity.Entities;
namespace Game.Runtime
{
    public partial struct CampaignMissionLaunchSystem
    {
        private static bool TryQueueGridlockCleanup(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime)
        {
            if(!em.HasComponent<CampaignMissionGridlockState>(root) || !em.HasBuffer<CampaignMissionGridlockWorkSite>(root)) return false;
            var g=em.GetComponentData<CampaignMissionGridlockState>(root);
            if(!g.SessionToken.Equals(runtime.SessionToken) || g.AttemptOrdinal!=runtime.AttemptOrdinal || g.SourceVersion!=runtime.SourceVersion) return false;
            using var query=new EntityQueryBuilder(Allocator.Temp).WithAll<BuildingRuntimeStateTag,BuildingRuntimeSpawnRequest,BuildingRuntimeDeleteRequest>().Build(em);
            if(query.CalculateEntityCount()!=1) return true;
            var boundary=query.GetSingletonEntity();var requests=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            var deletes=em.GetBuffer<BuildingRuntimeDeleteRequest>(boundary);var sites=em.GetBuffer<CampaignMissionGridlockWorkSite>(root);
            for(int i=requests.Length-1;i>=0;i--)
            {
                var r=requests[i];bool owned=false;for(int n=0;n<sites.Length;n++) owned|=sites[n].SpawnRequestId==r.RequestId && r.RequestId!=0;
                if(!owned) continue;
                if(r.Status==BuildingRuntimeSpawnRequest.Succeeded && r.BuildingRuntimeId>0)
                {
                    bool queued=false;for(int n=0;n<deletes.Length;n++) queued|=deletes[n].BuildingRuntimeId==r.BuildingRuntimeId;
                    if(!queued) deletes.Add(new BuildingRuntimeDeleteRequest {BuildingRuntimeId=r.BuildingRuntimeId,ImmediateCleanup=1});
                }
                requests.RemoveAt(i);
            }
            return true;
        }
    }
}

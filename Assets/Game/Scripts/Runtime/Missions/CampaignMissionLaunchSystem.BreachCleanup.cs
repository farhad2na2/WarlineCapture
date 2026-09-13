using Game.Components;
using Unity.Entities;
using Unity.Collections;
namespace Game.Runtime
{
    public partial struct CampaignMissionLaunchSystem
    {
        private static bool TryQueueBreachCleanup(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime)
        {
            if(!em.HasComponent<CampaignMissionBreachState>(root)) return false;
            var breach=em.GetComponentData<CampaignMissionBreachState>(root);
            if(!breach.SessionToken.Equals(runtime.SessionToken) || breach.SourceVersion!=runtime.SourceVersion || breach.AttemptOrdinal!=runtime.AttemptOrdinal) return false;
            using var query=new EntityQueryBuilder(Allocator.Temp).WithAll<BuildingRuntimeStateTag,BuildingRuntimeDeleteRequest,BuildingRuntimeSpawnRequest>().Build(em);
            if(query.CalculateEntityCount()!=1) return true;
            var boundary=query.GetSingletonEntity();
            var spawns=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            var deletes=em.GetBuffer<BuildingRuntimeDeleteRequest>(boundary);
            for(int i=spawns.Length-1;i>=0;i--)
            {
                var spawn=spawns[i];
                if(spawn.RequestId!=breach.GateRequestId && spawn.RequestId!=breach.CoreRequestId) continue;
                if(spawn.Status==BuildingRuntimeSpawnRequest.Succeeded && spawn.BuildingRuntimeId>0)
                {
                    bool queued=false;for(int n=0;n<deletes.Length;n++) queued|=deletes[n].BuildingRuntimeId==spawn.BuildingRuntimeId;
                    if(!queued) deletes.Add(new BuildingRuntimeDeleteRequest {BuildingRuntimeId=spawn.BuildingRuntimeId,ImmediateCleanup=1});
                }
                spawns.RemoveAt(i);
            }
            return true;
        }
    }
}

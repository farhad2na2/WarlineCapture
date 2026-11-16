#if UNITY_EDITOR
using System;
using System.Reflection;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
namespace Game.Runtime
{
    [Serializable] public sealed class NativePathTraceRequest
    {
        public int index,version,startX,startZ,requestedX,requestedZ,footprintX,footprintZ;
        public byte faction,manual,vehicle;
    }
    [Serializable] public sealed class NativePathTraceSnapshot
    {
        public int engineFrame,scheduleFrame,pendingCount,pendingBudget,adaptiveBudget;
        public double realtime,scheduleRealtime;
        public bool hasPending,observedComplete;
        public NativePathTraceRequest[] orderedRequests=Array.Empty<NativePathTraceRequest>();
    }
    // Owned test Worlds only. Readonly metadata probe, not a completion mechanism.
    // In particular Goals/AssignedGoals/status/stream are job-writeable and are NEVER
    // read here. Completed outputs must be observed after production applies them.
    public static unsafe class NativePathTraceProbe
    {
        private static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        private static T Read<T>(object system,string field)=>(T)typeof(UnitPathfindingSystem).GetField(field,Flags).GetValue(system);
        public static NativePathTraceSnapshot Capture(World world,SystemHandle handle)
        {
            ref var system=ref world.Unmanaged.GetUnsafeSystemRef<UnitPathfindingSystem>(handle);
            object copy=system;
            var result=new NativePathTraceSnapshot{engineFrame=Time.frameCount,realtime=Time.realtimeSinceStartupAsDouble,
                hasPending=Read<bool>(copy,"_hasPendingPathJob"),scheduleFrame=Read<int>(copy,"_pendingScheduleFrame"),scheduleRealtime=Read<double>(copy,"_pendingScheduleTime"),
                pendingCount=Read<int>(copy,"_pendingRequestCount"),pendingBudget=Read<int>(copy,"_pendingRequestBudget")};
            if(result.hasPending)result.observedComplete=Read<JobHandle>(copy,"_pendingPathHandle").IsCompleted;
            object budget=typeof(UnitPathfindingSystem).GetField("_budget",Flags).GetValue(copy);
            result.adaptiveBudget=(int)budget.GetType().GetProperty("AdaptiveRequestsPerFrame",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(budget);
            if(!result.hasPending)return result;
            var requests=Read<UnitPathRequestBuffer>(copy,"_requestBuffers");int count=Math.Min(result.pendingCount,requests.Entities.Length);
            result.orderedRequests=new NativePathTraceRequest[count];
            for(int i=0;i<count;i++)
            {
                var entity=requests.Entities[i];var cell=requests.UnitGrids[i].Cell;var goal=requests.Goals[i].Goal;var size=requests.Footprints[i].Size;
                result.orderedRequests[i]=new NativePathTraceRequest{index=entity.Index,version=entity.Version,startX=cell.x,startZ=cell.y,requestedX=goal.x,requestedZ=goal.y,
                    footprintX=size.x,footprintZ=size.y,faction=requests.Factions[i],manual=requests.ManualMoves[i],vehicle=requests.MovementBehaviors[i].UsesVehicleMotion};
            }
            return result;
        }
    }
}
#endif

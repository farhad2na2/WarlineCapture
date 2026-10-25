using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using Unity.Entities;using Unity.Collections;
namespace Game.Runtime{
 internal static class OwnedEntityJobBackend {
  internal static void Execute<T>(T job,EntityManager em)where T:struct,IJobEntity{
   if(UnitEngagementSystem.TryExecuteOwnedAcquisition(job,em))return;
   var admitted=new[]{"Game.Runtime.UnitGridMoveJob","Game.Runtime.UnitPathFollowCleanupJob","Game.Runtime.UnitEngagementSystem+BuildSpatialMapJob","Game.Runtime.UnitEngagementSystem+AcquireTargetsJob","Game.Runtime.UnitEngagedMovementSystem+EngagedMoveJob","Game.Runtime.EngageTargetValidateSystem+ValidateEngageTargetJob","Game.Runtime.EngageTargetSyncSystem+SyncJob","Game.Runtime.UnitDeathSystem+CollectDeathBeginCandidatesJob","Game.Runtime.UnitDeathSystem+CollectDeathAnimationFinalizeJob","Game.Runtime.VehicleWreckCleanupSystem+CollectExpiredWrecksJob","Game.Runtime.UnitManualMoveRetrySystem+ClearRetryCooldownJob","Game.Runtime.UnitManualMoveRetrySystem+RemoveStaleGroupMemberJob","Game.Runtime.UnitManualMoveRetrySystem+RestoreManualPathRequestJob","Game.Runtime.UnitManualMoveRetrySystem+RestoreLongDistancePathRequestJob"};if(!admitted.Contains(typeof(T).FullName))throw new NotSupportedException("Unadmitted source IJobEntity "+typeof(T).FullName);
   var method=typeof(T).GetMethod("Execute",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)??throw new NotSupportedException("Original source Execute missing");var parameters=method.GetParameters();var all=parameters.Where(p=>p.ParameterType.IsByRef).Select(p=>p.ParameterType.GetElementType()).ToArray();var none=new List<Type>();var changed=new List<Type>();
   foreach(var a in typeof(T).GetCustomAttributesData())if(a.AttributeType.Name=="WithAllAttribute")all=all.Concat(((IEnumerable<CustomAttributeTypedArgument>)a.ConstructorArguments[0].Value).Select(x=>(Type)x.Value)).Distinct().ToArray();
   foreach(var a in typeof(T).GetCustomAttributesData())if(a.AttributeType.Name=="WithNoneAttribute")foreach(var x in (IEnumerable<CustomAttributeTypedArgument>)a.ConstructorArguments[0].Value)none.Add((Type)x.Value);
   foreach(var a in typeof(T).GetCustomAttributesData())if(a.AttributeType.Name=="WithChangeFilterAttribute")foreach(var x in (IEnumerable<CustomAttributeTypedArgument>)a.ConstructorArguments[0].Value)changed.Add((Type)x.Value);
   var rows=em.CreateEntityQuery(new EntityQueryDesc{All=all.Select(t=>(ComponentType)t).ToArray(),None=none.Select(t=>(ComponentType)t).ToArray()}).ToEntityArray(Allocator.Temp);
   if(rows.Length>65536)throw new NotSupportedException("Entity job domain bound");object boxed=job;
   // Original readonly lookup inputs are unchanged through both jobs; each job writes only its own ref tuple.
   // Collision checks use the original immutable pre-job live snapshot. Deferred changes wait for EndSimulation.
   for(int index=0;index<rows.Length;index++){
    var entity=rows[index];if(changed.Count>0&&!changed.Any(t=>em.Changed(entity,t,SystemAPI.LastVersion)))continue;var args=new object[parameters.Length];
    for(int p=0;p<args.Length;p++){var t=parameters[p].ParameterType;args[p]=t==typeof(int)?(object)index:t==typeof(Entity)?entity:t.IsByRef?em.GetComponent(entity,t.GetElementType()):throw new NotSupportedException("Unknown source job argument");}
    try{method.Invoke(boxed,args);}catch(TargetInvocationException error){throw error.InnerException??error;}
    for(int p=0;p<args.Length;p++)if(parameters[p].ParameterType.IsByRef&&!parameters[p].IsIn)em.SetComponent(entity,parameters[p].ParameterType.GetElementType(),args[p]);
   }
  }
 }
}
namespace Unity.Entities{
 public sealed class OwnedEndSimulationBuffers{
  readonly List<EntityCommandBuffer> buffers=new List<EntityCommandBuffer>();public EntityCommandBuffer Create(){if(buffers.Count>=256)throw new NotSupportedException("EndSimulation buffer bound");var b=new EntityCommandBuffer(Allocator.TempJob);buffers.Add(b);return b;}
  public void Playback(EntityManager em){foreach(var b in buffers){b.Playback(em);b.Dispose();}buffers.Clear();}public int PendingCount=>buffers.Count;
 }
 public sealed class EndSimulationEntityCommandBufferSystem{
  public struct Singleton:IComponentData{public OwnedEndSimulationBuffers Owner;public EntityCommandBuffer CreateCommandBuffer(WorldClock world){if(Owner==null)throw new NotSupportedException("Initial EndSimulation queue owner must be admitted");return Owner.Create();}}
 }
}

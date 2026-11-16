using System;using Game.Components;using Game.Runtime.Pathfinding;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;using Unity.Transforms;using Newtonsoft.Json.Linq;
namespace Game.Runtime {
// Validator-only readback. Predictions and observed post-state are not acceptance receipts.
internal static class AttackMoveCorrespondence {
 internal sealed class Snapshot { internal Entity[] Fighters; internal int2[] Goals; internal bool Legal; internal string Error; }
 internal static Snapshot Capture(EntityManager em,int2 click){
  var r=new Snapshot();try{
   using var q=em.CreateEntityQuery(typeof(SelectedUnitTag),typeof(UnitMove),typeof(UnitGrid),typeof(UnitAttack),typeof(UnitCombat),typeof(UnitHealth),typeof(LocalTransform),typeof(Faction));
   using var entities=q.ToEntityArray(Allocator.Temp);using var fighters=new NativeList<Entity>(Allocator.Temp);AttackMovePlanning.CollectFighters(em,entities,FactionIdentity.PlayerFactionId,fighters);
   r.Fighters=new Entity[fighters.Length];for(int i=0;i<fighters.Length;i++)r.Fighters[i]=fighters[i];r.Goals=new int2[fighters.Length];
   using var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent));using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
   if(grids.CalculateEntityCount()!=1)throw new NotSupportedException("unique grid required");var ge=grids.GetSingletonEntity();var grid=em.GetComponentData<GridConfig>(ge);var blockers=em.GetComponentData<DynamicBlockerComponent>(ge);var reader=new MapSurfacePathfindingSnapshot();
   if(!reader.TryCreateContext(em,surfaces,out var surface))throw new NotSupportedException("source surface unavailable");
   r.Legal=AttackMovePlanning.TryPlanAll(em,grid,em.GetBuffer<GridWalkable>(ge,true).AsNativeArray(),blockers.Blocked,blockers.FriendlyPassFactionIds,em.GetComponentData<DynamicOccupancyComponent>(ge).Occupied,fighters.AsArray(),click,surface,r.Goals);
  }catch(Exception e){r.Error=e.ToString();}return r;
 }
 internal static JObject Finish(EntityManager em,Snapshot r,bool commandAccepted){var rows=new JArray();bool match=r.Error==null&&r.Legal;
  if(r.Fighters!=null)for(int i=0;i<r.Fighters.Length;i++){var e=r.Fighters[i];bool exists=em.Exists(e),has=exists&&em.HasComponent<UnitTarget>(e);int2 cell=has?em.GetComponentData<UnitTarget>(e).Cell:default;bool hasOrder=exists&&em.HasComponent<AttackMoveOrder>(e);bool equal=r.Legal&&has&&cell.Equals(r.Goals[i]);match &= equal;rows.Add(new JObject{["slot"]=i,["entity"]=e.ToString(),["predicted"]=r.Legal?new JArray(r.Goals[i].x,r.Goals[i].y):null,["post_target"]=has?new JArray(cell.x,cell.y):null,["post_attack_order_present"]=hasOrder,["target_matches_prediction"]=equal});}
  return new JObject{["scope"]="ValidatorOnly_NoMemberAcceptanceInference",["command_result_accepted"]=commandAccepted,["plan_legal"]=r.Legal,["error"]=r.Error,["all_post_targets_match"]=match,["fighters"]=rows};
 }
}
}

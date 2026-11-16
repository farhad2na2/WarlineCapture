using Game.Components;using Game.Simulation;using Unity.Entities;using Unity.Mathematics;using Unity.Transforms;
namespace Game.Runtime {
public readonly struct SkirmishPublicObjectiveFacts {
 public readonly bool EnemyObjectiveAlive,EnemyTargetVisible,PlayerPositionKnown;public readonly AriaGroundPosition EnemyPosition,PlayerPosition;
 public SkirmishPublicObjectiveFacts(bool alive,bool visible,bool playerKnown,AriaGroundPosition enemy,AriaGroundPosition player){EnemyObjectiveAlive=alive;EnemyTargetVisible=visible;PlayerPositionKnown=playerKnown;EnemyPosition=enemy;PlayerPosition=player;}
}
// Data published by the existing mission objective label. Only designated base
// handles enter here; this is not a hidden tactical contact position projection.
public static class SkirmishPublicObjectiveProjection {
 // Shared entity-to-position read. Callers still decide which entities are
 // public and whether a camera presentation request can be issued.
 public static bool TryReadFocusPosition(EntityManager em,Entity entity,out float3 position){
  position=default;if(!em.Exists(entity)||!em.HasComponent<LocalTransform>(entity))return false;
  position=em.GetComponentData<LocalTransform>(entity).Position;return true;
 }
 public static bool PlayerVisible(EntityManager em,Entity contact)=>em.Exists(contact)&&(!em.HasComponent<SkirmishAttemptOwnedComponent>(contact)||SkirmishFogService.IsVisibleToFaction(em,contact,1));
 public static SkirmishPublicObjectiveFacts Read(EntityManager em,Entity session)=>Read(em,em.GetComponentData<SkirmishMatchState>(session));
 public static SkirmishPublicObjectiveFacts Read(EntityManager em,in SkirmishMatchState match){
  Entity enemyMainBase=match.EnemyMainBase,playerMainBase=match.PlayerMainBase;
  float3 enemyPoint=default;
  bool alive=em.Exists(enemyMainBase)&&em.HasComponent<UnitHealth>(enemyMainBase)&&em.GetComponentData<UnitHealth>(enemyMainBase).Current>0&&TryReadFocusPosition(em,enemyMainBase,out enemyPoint);
  bool playerKnown=TryReadFocusPosition(em,playerMainBase,out var playerPoint);AriaGroundPosition enemy=default,player=default;
  if(alive)enemy=new AriaGroundPosition(enemyPoint.x,enemyPoint.y,enemyPoint.z);
  if(playerKnown)player=new AriaGroundPosition(playerPoint.x,playerPoint.y,playerPoint.z);
  return new SkirmishPublicObjectiveFacts(alive,PlayerVisible(em,enemyMainBase),playerKnown,enemy,player);
 }
}
}

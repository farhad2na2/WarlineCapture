using Game.Components;using Game.Simulation;using Unity.Entities;using Unity.Transforms;
namespace Game.Runtime {
public readonly struct SkirmishPublicObjectiveFacts {
 public readonly bool EnemyObjectiveAlive,EnemyTargetVisible,PlayerPositionKnown;public readonly AriaGroundPosition EnemyPosition,PlayerPosition;
 public SkirmishPublicObjectiveFacts(bool alive,bool visible,bool playerKnown,AriaGroundPosition enemy,AriaGroundPosition player){EnemyObjectiveAlive=alive;EnemyTargetVisible=visible;PlayerPositionKnown=playerKnown;EnemyPosition=enemy;PlayerPosition=player;}
}
// Data published by the existing mission objective label. Only designated base
// handles enter here; this is not a hidden tactical contact position projection.
public static class SkirmishPublicObjectiveProjection {
 public static bool PlayerVisible(EntityManager em,Entity contact)=>em.Exists(contact)&&(!em.HasComponent<SkirmishAttemptOwnedComponent>(contact)||SkirmishFogService.IsVisibleToFaction(em,contact,1));
 public static SkirmishPublicObjectiveFacts Read(EntityManager em,Entity session)=>Read(em,em.GetComponentData<SkirmishMatchState>(session));
 public static SkirmishPublicObjectiveFacts Read(EntityManager em,in SkirmishMatchState match){
  Entity enemyMainBase=match.EnemyMainBase,playerMainBase=match.PlayerMainBase;
  bool alive=em.Exists(enemyMainBase)&&em.HasComponent<UnitHealth>(enemyMainBase)&&em.GetComponentData<UnitHealth>(enemyMainBase).Current>0&&em.HasComponent<LocalTransform>(enemyMainBase);
  bool playerKnown=em.Exists(playerMainBase)&&em.HasComponent<LocalTransform>(playerMainBase);AriaGroundPosition enemy=default,player=default;
  if(alive){var p=em.GetComponentData<LocalTransform>(enemyMainBase).Position;enemy=new AriaGroundPosition(p.x,p.y,p.z);}
  if(playerKnown){var p=em.GetComponentData<LocalTransform>(playerMainBase).Position;player=new AriaGroundPosition(p.x,p.y,p.z);}
  return new SkirmishPublicObjectiveFacts(alive,PlayerVisible(em,enemyMainBase),playerKnown,enemy,player);
 }
}
}

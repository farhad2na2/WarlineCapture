using System;using System.Collections.Generic;using System.Linq;using System.Reflection;
using Game.Components;using Unity.Entities;using Unity.Collections;using Unity.Transforms;
namespace Game.Runtime {
 public partial struct UnitEngagementSystem {
  static readonly object OwnedAcquisitionAdmissionLock=new object();static bool ownedBuildAdmitted,ownedAcquireAdmitted;
  // Finite source-host boundary only; original job bodies and policy remain in UnitEngagementSystem.cs.
  internal static bool TryExecuteOwnedAcquisition<T>(T job,EntityManager em)where T:struct,IJobEntity{
   if(typeof(T)==typeof(BuildSpatialMapJob)){EnsureOwnedAcquisitionHeader(false);ExecuteOwnedBuild((BuildSpatialMapJob)(object)job,em);return true;}
   if(typeof(T)==typeof(AcquireTargetsJob)){EnsureOwnedAcquisitionHeader(true);ExecuteOwnedAcquire((AcquireTargetsJob)(object)job,em);return true;}
   return false;
  }
  static void ExecuteOwnedBuild(BuildSpatialMapJob job,EntityManager em){
   using var query=em.CreateEntityQuery(new EntityQueryDesc{
    All=new[]{ComponentType.ReadOnly<LocalTransform>(),ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitHealth>()},
    None=new[]{ComponentType.ReadOnly<StaticGridBlocker>(),ComponentType.ReadOnly<CampaignMissionCombatSuppressedTag>()}});
   using var rows=query.ToEntityArray(Allocator.Temp);
   if(rows.Length>65536)throw new NotSupportedException("Entity job domain bound");
   for(int sortKey=0;sortKey<rows.Length;sortKey++){
    Entity entity=rows[sortKey];var transform=em.GetComponentData<LocalTransform>(entity);var unitGrid=em.GetComponentData<UnitGrid>(entity);var faction=em.GetComponentData<Faction>(entity);var health=em.GetComponentData<UnitHealth>(entity);
    job.Execute(sortKey,entity,in transform,in unitGrid,in faction,in health);
   }
  }
  static void ExecuteOwnedAcquire(AcquireTargetsJob job,EntityManager em){
   using var query=em.CreateEntityQuery(new EntityQueryDesc{
    All=new[]{ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitCombat>(),ComponentType.ReadOnly<UnitAttack>(),ComponentType.ReadOnly<LocalTransform>()},
    None=new[]{ComponentType.ReadOnly<StaticGridBlocker>(),ComponentType.ReadOnly<EngageTarget>(),ComponentType.ReadOnly<BaseBreachOrder>(),ComponentType.ReadOnly<GroundMissileInFlightComponent>(),ComponentType.ReadOnly<GroundMissileLauncherComponent>(),ComponentType.ReadOnly<CampaignMissionCombatSuppressedTag>()}});
   using var rows=query.ToEntityArray(Allocator.Temp);
   if(rows.Length>65536)throw new NotSupportedException("Entity job domain bound");
   for(int sortKey=0;sortKey<rows.Length;sortKey++){
    Entity entity=rows[sortKey];var selfGrid=em.GetComponentData<UnitGrid>(entity);var selfFaction=em.GetComponentData<Faction>(entity);var combat=em.GetComponentData<UnitCombat>(entity);var attack=em.GetComponentData<UnitAttack>(entity);var selfTransform=em.GetComponentData<LocalTransform>(entity);
    job.Execute(sortKey,entity,in selfGrid,in selfFaction,in combat,in attack,in selfTransform);
   }
  }
  // Reflection is used once per exact admitted job header, never per entity/body invocation.
  static void EnsureOwnedAcquisitionHeader(bool acquire){lock(OwnedAcquisitionAdmissionLock){
   if(acquire?ownedAcquireAdmitted:ownedBuildAdmitted)return;
   Type type=acquire?typeof(AcquireTargetsJob):typeof(BuildSpatialMapJob);
   ValidateOwnedAcquisitionHeader(type,acquire);
   if(acquire)ownedAcquireAdmitted=true;else ownedBuildAdmitted=true;
  }}
  static void ValidateOwnedAcquisitionHeader(Type type,bool acquire){
   Type expected=acquire?typeof(AcquireTargetsJob):typeof(BuildSpatialMapJob);
   if(type!=expected||!type.IsValueType||!(acquire?type.IsNestedAssembly:type.IsNestedPrivate))throw new NotSupportedException("Exact original acquisition job type required");
   var methods=type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(m=>m.Name=="Execute").ToArray();
   if(methods.Length!=1||!methods[0].IsPublic||methods[0].ReturnType!=typeof(void))throw new NotSupportedException("Original acquisition Execute header changed");
   var p=methods[0].GetParameters();Type[] components=acquire?new[]{typeof(UnitGrid),typeof(Faction),typeof(UnitCombat),typeof(UnitAttack),typeof(LocalTransform)}:new[]{typeof(LocalTransform),typeof(UnitGrid),typeof(Faction),typeof(UnitHealth)};
   if(p.Length!=components.Length+2||p[0].ParameterType!=typeof(int)||p[1].ParameterType!=typeof(Entity)||p[0].IsOut||p[1].IsOut||p[0].IsIn||p[1].IsIn)throw new NotSupportedException("Original acquisition index/entity header changed");
   var sortAttributes=p[0].GetCustomAttributesData().Where(a=>OwnedAttributeName(a.AttributeType)=="EntityIndexInQuery").ToArray();
   if(sortAttributes.Length!=1)throw new NotSupportedException("Original packed query index attribute required");
   for(int i=0;i<components.Length;i++)if(p[i+2].ParameterType!=components[i].MakeByRefType()||!p[i+2].IsIn||p[i+2].IsOut)throw new NotSupportedException("Original readonly in-component header changed");
   var queryAttributes=type.GetCustomAttributesData().Where(a=>a.AttributeType.Namespace=="Unity.Entities"&&OwnedAttributeName(a.AttributeType).StartsWith("With",StringComparison.Ordinal)).ToArray();
   if(queryAttributes.Length!=1||OwnedAttributeName(queryAttributes[0].AttributeType)!="WithNone"||queryAttributes[0].ConstructorArguments.Count!=1)throw new NotSupportedException("Original acquisition query attributes changed");
   var none=((IEnumerable<CustomAttributeTypedArgument>)queryAttributes[0].ConstructorArguments[0].Value).Select(x=>(Type)x.Value).ToArray();
   Type[] expectedNone=acquire?new[]{typeof(StaticGridBlocker),typeof(EngageTarget),typeof(BaseBreachOrder),typeof(GroundMissileInFlightComponent),typeof(GroundMissileLauncherComponent),typeof(CampaignMissionCombatSuppressedTag)}:new[]{typeof(StaticGridBlocker),typeof(CampaignMissionCombatSuppressedTag)};
   if(!none.SequenceEqual(expectedNone))throw new NotSupportedException("Original acquisition exclusions changed");
  }
  static string OwnedAttributeName(Type type){string n=type.Name;return n.EndsWith("Attribute",StringComparison.Ordinal)?n.Substring(0,n.Length-9):n;}
 }
}

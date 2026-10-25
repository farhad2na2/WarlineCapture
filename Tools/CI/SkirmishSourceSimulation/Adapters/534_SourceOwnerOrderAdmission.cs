using System;using System.Collections.Generic;using System.Linq;using System.Reflection;
namespace Game.Runtime {
// Finite admission of the named current-source owners, not a scheduler implementation.
internal sealed class SourceOwnerOrderAdmission {
 internal static readonly string[] DeclaredOrder = {"CombatDamageObservationBootstrapSystem","UnitAttackStateEnsureSystem","UnitAttackTraceStateEnsureSystem","SkirmishRosterProjectionSystem","RuntimeGridDeduplicationSystem","DynamicBlockerInitSystem","StaticGridBlockerUpdateSystem","MaterialFabricationSystem","UnitMoveOrderRequestSystem","SkirmishSharedFogUpdateSystem","EngageTargetValidateSystem","DynamicOccupancyRebuildSystem","AttackMoveSystem","UnitEngagementSystem","EngageTargetSyncSystem","UnitManualMoveRetrySystem","UnitPathfindingSystem","UnitEngagedMovementSystem","UnitGridMovementSystem","VehicleFuelConsumptionSystem","UnitAttackSystem","BuildingDefenseAttackSystem","SkirmishCapacityLifecycleSystem","SkirmishArmyGroupSystem","SkirmishResearchSystem","UnitDeathSystem","VehicleWreckCleanupSystem","SkirmishObjectiveFactProjectionSystem","SkirmishBaseAssaultObjectiveSystem","SkirmishNativeObjectiveTickSystem","SkirmishEnemyStrategySystem","SkirmishOutcomeSystem"};
 static readonly Dictionary<string,string[]> PhaseOwners=new Dictionary<string,string[]>(StringComparer.Ordinal){
 {"combat_ensure",new[]{"CombatDamageObservationBootstrapSystem","UnitAttackStateEnsureSystem","UnitAttackTraceStateEnsureSystem"}},
 {"roster",new[]{"SkirmishRosterProjectionSystem"}},
 {"blockers",new[]{"RuntimeGridDeduplicationSystem","DynamicBlockerInitSystem","StaticGridBlockerUpdateSystem"}},
 {"fabrication",new[]{"MaterialFabricationSystem"}},
 {"queue",new[]{"UnitMoveOrderRequestSystem"}},
 {"fog",new[]{"SkirmishSharedFogUpdateSystem"}},
 {"validate",new[]{"EngageTargetValidateSystem"}},
 {"occupancy",new[]{"DynamicOccupancyRebuildSystem"}},
 {"attack_resume",new[]{"AttackMoveSystem"}},
 {"acquire",new[]{"UnitEngagementSystem"}},
 {"engage_sync",new[]{"EngageTargetSyncSystem"}},
 {"manual_retry",new[]{"UnitManualMoveRetrySystem"}},
 {"path",new[]{"UnitPathfindingSystem"}},
 {"chase",new[]{"UnitEngagedMovementSystem"}},
 {"movement",new[]{"UnitGridMovementSystem"}},
 {"fuel",new[]{"VehicleFuelConsumptionSystem"}},
 {"attack",new[]{"UnitAttackSystem"}},
 {"defense",new[]{"BuildingDefenseAttackSystem"}},
 {"capacity",new[]{"SkirmishCapacityLifecycleSystem"}},
 {"groups",new[]{"SkirmishArmyGroupSystem"}},
 {"research",new[]{"SkirmishResearchSystem"}},
 {"death",new[]{"UnitDeathSystem"}},
 {"vehicle_wreck",new[]{"VehicleWreckCleanupSystem"}},
 {"objective_facts",new[]{"SkirmishObjectiveFactProjectionSystem","SkirmishBaseAssaultObjectiveSystem","SkirmishNativeObjectiveTickSystem"}},
 {"enemy",new[]{"SkirmishEnemyStrategySystem"}},
 {"objective_outcome",new[]{"SkirmishOutcomeSystem"}}
 };
 static bool admitted;int lastOwner=-1;bool tick;int observed;
 internal static void RequirePlan(){if(admitted)return;ValidateOrder(DeclaredOrder);admitted=true;}
 internal static void ValidateOrder(IReadOnlyList<string> order){
  var position=new Dictionary<string,int>(StringComparer.Ordinal);for(int i=0;i<order.Count;i++)if(order[i]==null||position.ContainsKey(order[i]))throw new InvalidOperationException("Duplicate/null declared owner");else position.Add(order[i],i);
  foreach(string required in DeclaredOrder)if(!position.ContainsKey(required))throw new InvalidOperationException("Missing required current source owner "+required);
  if(position.Count!=DeclaredOrder.Length)throw new InvalidOperationException("Unknown declared source owner");
  var types=typeof(SourceOwnerOrderAdmission).Assembly.GetTypes().Where(t=>t.Namespace=="Game.Runtime.NativeOrderMetadata").ToDictionary(t=>t.Name,StringComparer.Ordinal);
  var edges=types.Keys.ToDictionary(n=>n,n=>new HashSet<string>(StringComparer.Ordinal),StringComparer.Ordinal);var groups=new Dictionary<string,string>();var first=new HashSet<string>();var last=new HashSet<string>();
  foreach(var pair in types){groups[pair.Key]="SimulationSystemGroup";foreach(var attr in pair.Value.GetCustomAttributesData()){
   string kind=attr.AttributeType.Name;if(kind!="UpdateAfterAttribute"&&kind!="UpdateBeforeAttribute"&&kind!="UpdateInGroupAttribute")continue;
   if(attr.ConstructorArguments.Count!=1||!(attr.ConstructorArguments[0].Value is Type target)||target.Namespace!="Game.Runtime.NativeOrderMetadata"||!types.ContainsKey(target.Name))throw new InvalidOperationException("Malformed finite compiled ordering metadata");
   if(kind=="UpdateAfterAttribute")edges[target.Name].Add(pair.Key);else if(kind=="UpdateBeforeAttribute")edges[pair.Key].Add(target.Name);else{groups[pair.Key]=target.Name;foreach(var arg in attr.NamedArguments)if(arg.TypedValue.Value is bool yes&&yes){if(arg.MemberName=="OrderFirst")first.Add(pair.Key);else if(arg.MemberName=="OrderLast")last.Add(pair.Key);}}
  }}
  foreach(string a in order)foreach(string b in order){if(a==b)continue;if(groups[a]==groups[b]&&(first.Contains(a)&&!first.Contains(b)||last.Contains(b)&&!last.Contains(a)))edges[a].Add(b);if(groups[a]=="InitializationSystemGroup"&&groups[b]=="SimulationSystemGroup")edges[a].Add(b);}
  foreach(string start in order){var seen=new HashSet<string>();var todo=new Stack<string>(edges[start]);while(todo.Count!=0){string node=todo.Pop();if(!seen.Add(node))continue;foreach(string next in edges[node])todo.Push(next);if(position.TryGetValue(node,out int target)&&position[start]>=target)throw new InvalidOperationException("Current native ordering violation: "+start+" -> "+node);}}
 }
 internal void EndTick(){if(!tick||observed!=DeclaredOrder.Length)throw new InvalidOperationException("Required finite source owner phase omitted");tick=false;}
 internal void BeginTick(){RequirePlan();lastOwner=-1;observed=0;tick=true;}
 internal void EnterPhase(string phase){if(!PhaseOwners.TryGetValue(phase,out var owners))return;if(!tick)throw new InvalidOperationException("Source tick not admitted");foreach(string owner in owners){int index=Array.IndexOf(DeclaredOrder,owner);if(index<=lastOwner)throw new InvalidOperationException("Actual source phase differs from admitted finite order: "+owner);lastOwner=index;observed++;}}
}
}

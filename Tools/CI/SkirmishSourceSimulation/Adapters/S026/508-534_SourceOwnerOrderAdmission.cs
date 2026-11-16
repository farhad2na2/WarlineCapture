using System;using System.Collections.Generic;using System.Linq;using System.Reflection;
namespace Game.Runtime {
// Finite admission of the named current-source owners, not a scheduler implementation.
internal sealed class SourceOwnerOrderAdmission {
 internal static readonly string[] DeclaredOrder = new[]{"CombatDamageObservationBootstrapSystem","UnitAttackStateEnsureSystem","UnitAttackTraceStateEnsureSystem","SkirmishRosterProjectionSystem","UnitScanOrderExecutionSystem","UnitTransportRopeDisembarkSystem","RuntimeGridDeduplicationSystem","DynamicBlockerInitSystem","StaticGridBlockerUpdateSystem","ScanIntelCommandSystem","SkirmishSharedFogUpdateSystem","UnitAttackOrderRequestSystem","UnitTransportRopeDisperseSystem","ParentSystem","LocalToWorldSystem","MaterialFabricationSystem","UnitMoveOrderRequestSystem","EngageTargetValidateSystem","DynamicOccupancyRebuildSystem","AttackMoveSystem","UnitEngagementSystem","EngageTargetSyncSystem","UnitManualMoveRetrySystem","UnitAirMovementSystem","UnitPathfindingSystem","UnitEngagedMovementSystem","UnitGridMovementSystem","UnitTransportDeployOrderSystem","VehicleFuelConsumptionSystem","UnitAttackSystem","GroundMissileLauncherFireSystem","GroundMissileLauncherVisualSystem","GroundMissileFlyingRocketVisualSystem","GroundMissileRocketTrailSystem","BuildingDefenseAttackSystem","SkirmishCapacityLifecycleSystem","SkirmishResearchSystem","SkirmishArmyGroupSystem","SkirmishWorldMovementSystem","GroundMissileProjectileFlightSystem","UnitTransportBoardingSystem","TransportBoardingDiagnosticLogFlushSystem","MissileFlightAudioSystem","GroundMissileImpactSystem","UnitDeathSystem","VehicleWreckCleanupSystem","SkirmishObjectiveFactProjectionSystem","SkirmishBaseAssaultObjectiveSystem","VehicleFieldRepairSystem","SkirmishNativeObjectiveTickSystem","SkirmishEnemyStrategySystem","UnitTransportDeployAttackSystem","UnitTransportRopeDropSystem","SkirmishOutcomeSystem"};
 static readonly Dictionary<string,string[]> PhaseOwners=new Dictionary<string,string[]>(StringComparer.Ordinal){
{"combat_ensure",new[]{"CombatDamageObservationBootstrapSystem","UnitAttackStateEnsureSystem","UnitAttackTraceStateEnsureSystem"}},
{"roster",new[]{"SkirmishRosterProjectionSystem"}},
{"scan_execution5",new[]{"UnitScanOrderExecutionSystem"}},
{"rope_disembark18",new[]{"UnitTransportRopeDisembarkSystem"}},
{"blockers",new[]{"RuntimeGridDeduplicationSystem","DynamicBlockerInitSystem","StaticGridBlockerUpdateSystem"}},
{"scan_commands40",new[]{"ScanIntelCommandSystem"}},
{"fog",new[]{"SkirmishSharedFogUpdateSystem"}},
{"attack_requests",new[]{"UnitAttackOrderRequestSystem"}},
{"rope_disperse60",new[]{"UnitTransportRopeDisperseSystem"}},
{"transforms61",new[]{"ParentSystem","LocalToWorldSystem"}},
{"fabrication",new[]{"MaterialFabricationSystem"}},
{"queue",new[]{"UnitMoveOrderRequestSystem"}},
{"validate",new[]{"EngageTargetValidateSystem"}},
{"occupancy",new[]{"DynamicOccupancyRebuildSystem"}},
{"attack_resume",new[]{"AttackMoveSystem"}},
{"acquire",new[]{"UnitEngagementSystem"}},
{"engage_sync",new[]{"EngageTargetSyncSystem"}},
{"manual_retry",new[]{"UnitManualMoveRetrySystem"}},
{"air_movement131",new[]{"UnitAirMovementSystem"}},
{"path",new[]{"UnitPathfindingSystem"}},
{"chase",new[]{"UnitEngagedMovementSystem"}},
{"movement",new[]{"UnitGridMovementSystem"}},
{"transport_deploy135",new[]{"UnitTransportDeployOrderSystem"}},
{"fuel",new[]{"VehicleFuelConsumptionSystem"}},
{"attack",new[]{"UnitAttackSystem"}},
{"missile_fire",new[]{"GroundMissileLauncherFireSystem"}},
{"missile_visual",new[]{"GroundMissileLauncherVisualSystem"}},
{"flying_visual",new[]{"GroundMissileFlyingRocketVisualSystem"}},
{"rocket_trail",new[]{"GroundMissileRocketTrailSystem"}},
{"defense",new[]{"BuildingDefenseAttackSystem"}},
{"capacity",new[]{"SkirmishCapacityLifecycleSystem"}},
{"research",new[]{"SkirmishResearchSystem"}},
{"groups",new[]{"SkirmishArmyGroupSystem"}},
{"expanded_movement",new[]{"SkirmishWorldMovementSystem"}},
{"projectile_flight",new[]{"GroundMissileProjectileFlightSystem"}},
{"boarding164",new[]{"UnitTransportBoardingSystem"}},
{"transport_diagnostic172",new[]{"TransportBoardingDiagnosticLogFlushSystem"}},
{"missile_audio",new[]{"MissileFlightAudioSystem"}},
{"missile_impact",new[]{"GroundMissileImpactSystem"}},
{"death",new[]{"UnitDeathSystem"}},
{"vehicle_wreck",new[]{"VehicleWreckCleanupSystem"}},
{"objective_facts",new[]{"SkirmishObjectiveFactProjectionSystem","SkirmishBaseAssaultObjectiveSystem"}},
{"vehicle_repair",new[]{"VehicleFieldRepairSystem"}},
{"objective_tick",new[]{"SkirmishNativeObjectiveTickSystem"}},
{"enemy",new[]{"SkirmishEnemyStrategySystem"}},
{"transport_attack203",new[]{"UnitTransportDeployAttackSystem"}},
{"rope_drop206",new[]{"UnitTransportRopeDropSystem"}},
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

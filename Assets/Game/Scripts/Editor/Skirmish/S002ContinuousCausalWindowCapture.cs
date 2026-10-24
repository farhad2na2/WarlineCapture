#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Editor {
// Passive capture ONLY. This class contains no simulation, bank, native request chooser or replay parser.
internal static class S002ContinuousCausalWindowCapture {
 const int GlobalEventLimit=131072;
 const int WorkLimit=1500000, ByteLimit=2097152, RootHold=40*8192, TerminalHold=16384, EndHold=16384;
 const int OwnerLimit=128, DispatchLimit=16, HaulerLimit=8, CandidateLimit=18, FuelLimit=32, UnitLimit=34, RootLimit=16, StoreLimit=51, ProviderLimit=512;
 static long recordCallbackStopwatchTicks, recordCallbackMeasurementCalls;
 static bool opted, active, ended, takingTerminalBank, terminalBankDone; static long terminalBankOrdinal=-1; static int legacyCausalBytes, temporaryUtf8BufferBytes; static bool terminalLegacyPending; static int acceptedJsonNodes; static JObject cachedReference; static string failure; static World world; static Entity session; static string sessionId;
 static int work, bytes=EndHold, ownerCount, dispatchCount, fuelCount, providerCount, writerFacts, rootBytes, rootRows, productionCount;
 static long ordinal, dispatchId, selectionId, selectedEntry, terminalRoot, rootEntry, pendingSync=-1, ownerPre=-1, ownerInvoke=-1;
 static Entity pendingSyncEntity; static RuntimeBuildingEntity pendingSyncBuilding;
 static readonly Dictionary<string,int> kindCounts=new Dictionary<string,int>();
 static long activeWriter;
 static Entity selectedUnit; static object ownerIdentity; static bool ownerOpen, fuelOpen;
 static readonly JArray events=new JArray(); static readonly Dictionary<Entity,JToken> roots=new Dictionary<Entity,JToken>();
 static readonly Dictionary<Entity,JToken> units=new Dictionary<Entity,JToken>(); static readonly HashSet<Entity> currentFuel=new HashSet<Entity>();
 static readonly Dictionary<RuntimeBuildingEntity,int> bindingRefs=new Dictionary<RuntimeBuildingEntity,int>();
 static readonly HashSet<string> storeIds=new HashSet<string>(); static readonly Dictionary<string,bool> initialProfiles=new Dictionary<string,bool>(); static readonly int[] drainIndices=new int[256]; static Entity lastDrainEntity; static int lastDrainFaction;
 static readonly Dictionary<Entity,JToken> staticHaulers=new Dictionary<Entity,JToken>(); static readonly Dictionary<int,JToken> staticCandidates=new Dictionary<int,JToken>();
 static readonly Dictionary<long,long> openCalls=new Dictionary<long,long>();
 static void Need(bool ok,string reason) { if(!ok)throw new InvalidOperationException(reason); }
 static uint Bits(float value)=>BitConverter.ToUInt32(BitConverter.GetBytes(value),0);
 static ulong Bits(double value)=>BitConverter.ToUInt64(BitConverter.GetBytes(value),0);
 static JObject E(Entity value)=>new JObject { ["index"]=value.Index,["version"]=value.Version };
 static bool Same(EntityManager em)=>world!=null&&ReferenceEquals(world,em.World)&&em.Exists(session)&&em.HasComponent<SkirmishExpandedSessionComponent>(session)&&em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.ToString()==sessionId;
 internal static void Attach() {
  opted=Environment.GetEnvironmentVariable("WARLINE_S002_CONTINUOUS_WINDOW_V8")=="1";
  if(!opted)return;
  S002PhysicalStartupObservation.ContinuousFailed=Fail;
  S002PhysicalStartupObservation.ContinuousBeforeRead=Before;
  S002PhysicalStartupObservation.ContinuousOwner=Owner;
  S002PhysicalStartupObservation.ContinuousDispatch=Dispatch;
  S002PhysicalStartupObservation.ContinuousSelection=Selection;
  S002PhysicalStartupObservation.ContinuousProvider=Provider;
  S002PhysicalStartupObservation.ContinuousFuel=FuelUnit;
  S002PhysicalStartupObservation.ContinuousFuelStatePost=FuelStatePost;
  S002PhysicalStartupObservation.ContinuousFuelBoundary=FuelBoundary;
  S002PhysicalStartupObservation.ContinuousRequested=Requested;
  S002PhysicalStartupObservation.ContinuousDrainVisit=DrainVisit;
  S002PhysicalStartupObservation.ContinuousDrainPost=DrainPost;
  S002PhysicalStartupObservation.ContinuousRoots=CheckRoots;
  S002PhysicalStartupObservation.ContinuousSync=Sync;
  S002PhysicalStartupObservation.ContinuousStatus=Status;
  S002PhysicalStartupObservation.ContinuousStorage=Storage;
 }
 static void Fail(string reason) { if(failure==null)failure=reason; active=false; ClearCallbacks(); }
 static bool Before(int cost) {
  if(!active||failure!=null)return false;
  if(cost<0||work>WorkLimit-cost){Fail("v8 pre-read work cap exceeded work="+work+" requested="+cost);return false;}
  if(bytes>ByteLimit-256){Fail("v8 callback scratch pre-allocation byte headroom exhausted");return false;}
  work+=cost;return true;
 }
 static void Record(string kind,int allowance,int cost,Func<JObject> build,bool root=false) {
  if(!active||!Before(2))return;
  long recordStarted=System.Diagnostics.Stopwatch.GetTimestamp();
  try {
   Need(allowance>0&&cost>=0,"v8 invalid record allowance");
   int serializationReserve=checked(allowance*2);
   cost=checked(cost+8+serializationReserve); // Frame/ordinal/kind bookkeeping, object/header copy and exception-safe admission overhead.
   Need(ordinal<GlobalEventLimit,"v8 global source-event cap exceeded");
   Need(work<=WorkLimit-cost,"v8 pre-copy work cap exceeded");
   Need(bytes<=ByteLimit-allowance-(root?0:RootHold-rootBytes)-(!terminalBankDone&&!takingTerminalBank?TerminalHold:0),"v8 pre-copy byte/root-completion reservation exceeded");
   if(root)Need(rootBytes<=RootHold-allowance&&rootRows<40,"v8 reserved root completion exhausted");
   work+=cost;var row=build();row["observer_ordinal"]=ordinal;row["kind"]=kind;
   int nodeWork=checked(3*CountNodes(row));Need(nodeWork<=serializationReserve,"v8 JSON serialization node allowance exceeded");work-=serializationReserve-nodeWork;
   int actual=Encoding.UTF8.GetByteCount(row.ToString(Formatting.None))+1;
   Need(actual<=allowance,"v8 row static byte bound exceeded kind="+kind);
   bytes+=actual;if(root){rootBytes+=actual;rootRows++;}events.Add(row);acceptedJsonNodes+=nodeWork/3;if(!kindCounts.ContainsKey(kind))kindCounts.Add(kind,0);kindCounts[kind]++;ordinal++;
  }catch(Exception ex){Fail(ex.Message);}
  finally { recordCallbackStopwatchTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-recordStarted;recordCallbackMeasurementCalls++; }
 }
 static int CountNodes(JToken token) { int count=1;if(token is JContainer container)foreach(var child in container.Children())count=checked(count+CountNodes(child));return count; }
 static JObject Frame()=>new JObject { ["engine_frame"]=Time.frameCount };
 internal static void LegacyFuelHeader(JObject header) { if(active)ChargeLegacy(header,2,8192,false); }
 internal static bool AllowLegacyCopies => !opted || (failure==null && S002PhysicalStartupObservation.ContinuousFailure==null);
 internal static bool ProductionBegin(EntityManager em,Entity selected,EntityQuery query,string boundary) {
  if(!opted)return true;if(!AllowLegacyCopies)return false;if(ended)return true;
  if(!active) {
   if(boundary!="before-production")return false;
   Need(work==0&&bytes==EndHold&&ByteLimit>=EndHold+RootHold+TerminalHold,"v8 initial header reservation invalid");work+=16;
   world=em.World;session=selected;var s=em.GetComponentData<SkirmishExpandedSessionComponent>(session);sessionId=s.SessionId.ToString();
   active=true;
   if(s.CatalogId.ToString()!="S002"||!new FixedString64Bytes(sessionId).Equals(s.SessionId)||!new FixedString64Bytes(s.CatalogId.ToString()).Equals(s.CatalogId)||!new FixedString64Bytes(s.DefinitionId.ToString()).Equals(s.DefinitionId)){Fail("v8 S002 core/string header mismatch");return false;}
   Record("window-start",4096,16,()=>new JObject { ["schema_version"]=8,["session_entity"]=E(session),["session_id"]=sessionId,["catalog_id"]=s.CatalogId.ToString(),["definition_id"]=s.DefinitionId.ToString(),["content_version"]=s.ContentVersion,["setup_hash"]=s.SetupHash,["seed"]=s.Seed,["phase"]= (byte)s.Phase,["is_legacy"]=s.IsLegacy,["is_custom"]=s.IsCustom,["initialization_complete"]=s.InitializationComplete,["spawn_complete"]=s.SpawnComplete,["tick_clock"]=s.TickClock,["ordinal_space"]="v8-causal-window-only",["start_boundary"]="first-observed-S002-before-production",["producer_callsite"]="BuildingResourceProductionEcsSystem.ApplyStorageQuery.before-production",["session_discovery_query"]="Expanded+ResolvedSetup+EconomyStock+EnemyStock",["work_limit"]=WorkLimit,["byte_limit"]=ByteLimit,["source_writer_fact_limit"]=512,["root_completion_reservation_bytes"]=RootHold });
   try {
    Need(Before(1),"v8 broad roots query admission");using var broad=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));int n=broad.CalculateEntityCount();Need(n<=RootLimit,"v8 broad root cardinality exceeded");Need(Before(3*n),"v8 broad roots pre-array admission");using var list=broad.ToEntityArray(Allocator.Temp);
    Record("initial-broad-expanded-sessions",16384,n,()=> {var rows=new JArray();foreach(var e in list){var state=em.GetComponentData<SkirmishExpandedSessionComponent>(e);var row=new JObject { ["entity"]=E(e),["SessionId"]=state.SessionId.ToString(),["IsLegacy"]=state.IsLegacy };Need(new FixedString64Bytes(state.SessionId.ToString()).Equals(state.SessionId),"v8 session string loss");roots.Add(e,row);rows.Add(row);}return new JObject { ["query"]="SkirmishExpandedSessionComponent-only",["rows"]=rows }; });
   }catch(Exception ex){Fail(ex.Message);}
  }
  if(!active)return false;
  try{Need(Same(em),"v8 production session changed");int n=query.CalculateEntityCount();Need(n<=StoreLimit,"v8 global storage bound");if(boundary=="before-production")Need(++productionCount<=4,"v8 production invocation cap");Need(Before(3*n+129),"v8 production existing snapshot read accounting");}catch(Exception ex){Fail(ex.Message);}
   return active;
 }
 internal static void ProductionRows(EntityManager em,string boundary,float seconds,float delta,float ratio,JArray rows,JObject legacyRow,int legacyIndex) {
  if(!active)return;
  if(productionCount==1&&boundary=="before-production"){foreach(var row in rows){string id=(string)row["id"];if(id==null||!storeIds.Add(id)){Fail("v8 duplicate/absent initial store identity");return;}initialProfiles.Add(id,row["profile"]!=null&&row["profile"].Type!=JTokenType.Null);}}
  ChargeLegacy(legacyRow,4,131072,false);
  Record("production",2048,8,()=>new JObject { ["boundary"]=boundary,["session_entity"]=E(session),["seconds_per_day_bits"]=Bits(seconds),["delta_time_bits"]=Bits(delta),["oil_ratio_bits"]=Bits(ratio),["snapshot_role"]=productionCount==1&&boundary=="before-production"?"initial-before-input":"assertion-only-against-carried-bank",["legacy_production_history_index"]=legacyIndex,["legacy_observer_ordinal"]=legacyRow["observer_ordinal"],["frame"]=Time.frameCount });
 }
 internal static void ChargeLegacy(JToken value,int indent,int capacity,bool root) {
  if(!active&&!terminalLegacyPending)return;
  try {
   Need(work<=WorkLimit-capacity*2,"v8 legacy serializer pre-allocation work exhausted");
   Need(bytes<=ByteLimit-capacity-(root?0:RootHold-rootBytes)-(!terminalBankDone?TerminalHold:0),"v8 aggregate legacy/v8 byte reservation exhausted");
   if(root)Need(rootBytes<=RootHold-capacity,"v8 aggregate root completion hold exhausted");
   work+=capacity*2;int nodes=CountNodes(value);Need(nodes*3<=capacity*2,"v8 legacy serializer node bound");work-=capacity*2-nodes*3;
   byte[] buffer=new byte[capacity];temporaryUtf8BufferBytes=checked(temporaryUtf8BufferBytes+capacity);
   int length;using(var stream=new MemoryStream(buffer,0,buffer.Length,true,true)){stream.SetLength(0);using(var text=new StreamWriter(stream,new UTF8Encoding(false,true),1024,true))using(var writer=new JsonTextWriter(text){Formatting=Formatting.Indented}){value.WriteTo(writer);writer.Flush();text.Flush();}length=checked((int)stream.Length);}
   int lines=1;for(int k=0;k<length;k++)if(buffer[k]==10)lines++;
   int actual=checked(length+indent*lines+2);Need(actual<=capacity,"v8 legacy pretty/framing bound exceeded");bytes+=actual;legacyCausalBytes+=actual;if(root)rootBytes+=actual;
  }catch(Exception ex){Fail(ex.Message);}
 }
 internal static void LegacyInitial(JObject snapshot) {if(active)ChargeLegacy(snapshot,2,131072,false);}
 internal static void LegacyProvider(JObject row) {if(active)ChargeLegacy(row,6,4096,false);}
 internal static void LegacyFuel(JArray row) {if(active)ChargeLegacy(row,6,2048,false);}
 internal static void LegacyWriter(S002PhysicalStartupObservation.StorageWriterFacts f,JObject row) {
  if(!active&&!(terminalLegacyPending&&f.Invocation==terminalRoot&&f.Stage=="exit"))return;
  ChargeLegacy(row,4,8192,true);if(terminalLegacyPending&&f.Invocation==terminalRoot&&f.Stage=="exit")terminalLegacyPending=false;
 }
 static void Owner(S002PhysicalStartupObservation.ContinuousOwnerFacts f) {
  if(!active)return;
  Record(f.After?"owner-hauler-check-after":f.Invoke?"owner-hauler-invoke":"owner-hauler-check-before",1024,16,()=> {
   if(!f.After&&!f.Invoke){ownerPre=ordinal;ownerInvoke=-1;Need(!ownerOpen&&++ownerCount<=OwnerLimit,"v8 owner loop bound/order");ownerOpen=true;if(ownerIdentity==null)ownerIdentity=f.Owner;Need(ReferenceEquals(ownerIdentity,f.Owner),"v8 owner identity changed");}
   else{Need(ownerOpen,"v8 owner post/invoke without pre");if(f.After)ownerOpen=false;else{Need(ownerInvoke<0,"v8 duplicate owner invoke");ownerInvoke=ordinal;}}
   return new JObject { ["owner_loop"]=ownerCount,["realtime_now_bits"]=Bits(f.Now),["before_due_bits"]=Bits(f.BeforeDue),["after_due_bits"]=Bits(f.AfterDue),["disabled"]=f.Disabled,["callback_present"]=f.CallbackPresent,["due_role"]=ownerCount==1&&!f.After&&!f.Invoke?"initial-private-input":"assertion-derived-owner-private",["frame"]=Time.frameCount };
  });
 }
 static readonly string[] HaulerStaticKeys={"source_prefab_key","Faction","UnitResourceHauler","UnitFootprint","UnitHealth","UnitDeathAnimationComponent","ManualMoveOrderTag","UnitAirMovement","AutoWanderMoveTag","attempt_owned","SkirmishUnitRoleComponent"};
 static readonly string[] HaulerExternalKeys={"UnitGrid","LocalTransform","UnitPrevWorldPos","UnitVehicleKinematics"};
 static readonly string[] HaulerAssertionKeys={"UnitFuelConsumptionState","UnitResourceHaulOrder","UnitResourceHaulReservation","UnitResourceHaulStatus","UnitTarget","UnitPathRequest","UnitPathFollow","UnitPathRange","UnitPathRetryCooldown","UnitLongDistanceMove","EngageTarget"};
 static JToken StaticHauler(JObject row) {
  var stable=new JObject();var covered=new HashSet<string>{"query_ordinal","entity","UnitMove"};
  Need(row["UnitMove"]!=null&&row["UnitMove"].Type!=JTokenType.Null,"v8 actual required UnitMove query presence");
  foreach(var key in HaulerStaticKeys){Need(row.Property(key)!=null,"v8 missing static hauler field "+key);stable[key]=row[key].DeepClone();covered.Add(key);}
  foreach(var key in HaulerExternalKeys){Need(row.Property(key)!=null,"v8 missing external hauler field "+key);covered.Add(key);}
  foreach(var key in HaulerAssertionKeys){Need(row.Property(key)!=null,"v8 missing assertion hauler field "+key);covered.Add(key);}
  foreach(var prop in row.Properties())Need(covered.Contains(prop.Name),"v8 unclassified hauler field "+prop.Name);
  stable["UnitMove_presence"]=true;return stable;
 }
 static JToken StaticCandidate(JObject row) {
  var copy=(JObject)row.DeepClone();copy.Remove("enumeration_ordinal");
  var storage=copy["storage"] as JObject;if(storage!=null){foreach(var key in new[]{"StoredOilBarrels","StoredFuelBarrels","ReservedOilInboundBarrels","ReservedOilOutboundBarrels","ReservedFuelInboundBarrels","ReservedFuelOutboundBarrels","CivilianFuelReserveBarrels","ReceivedHaulerFuelBarrels","Version"})storage.Remove(key);}
  var profile=copy["production_profile"] as JObject;if(profile!=null)profile.Remove("FuelBatchProgressSeconds");
  var fabrication=copy["fabrication"] as JObject;if(fabrication!=null)foreach(var key in new[]{"CycleProgressSeconds","Status","BlockReason","Version"})fabrication.Remove(key);
  return copy;
 }
 static void Dispatch(S002PhysicalStartupObservation.InitialHaulerDispatchFacts f) {
  if(!active)return;
  if(!Before(8))return;
  if(f.ContinuousAfter){Record("bridge-dispatch-after",2048,16,()=>new JObject { ["dispatch_id"]=dispatchId,["owner_loop"]=ownerCount,["has_signature_assertion"]=f.HasSignature,["last_signature_assertion"]=f.LastSignature,["next_refresh_bits_assertion"]=Bits(f.NextRefreshAt),["counter_assertion"]=f.NextReservationId });return;}
  try{Need(Same(f.Manager)&&f.Haulers.Count==f.ActualQueryCount&&f.ActualQueryCount<=HaulerLimit,"v8 dispatch identity/query cap");Need(f.Context.RuntimeBuildings is Dictionary<int,RuntimeBuildingEntity> d&&d.Count<=CandidateLimit,"v8 complete candidate dictionary required");Need(++dispatchCount<=DispatchLimit&&selectedUnit==Entity.Null,"v8 dispatch bound/open selector");}catch(Exception ex){Fail(ex.Message);return;}
  Record("bridge-dispatch-before",131072,2048*f.Haulers.Count+1024*f.Context.RuntimeBuildings.Count,()=> {
   dispatchId=dispatchCount;var hs=new JArray();var cs=new JArray();var seen=new HashSet<Entity>();
   foreach(var e in f.Haulers){Need(seen.Add(e),"v8 duplicate hauler entity");var row=S002ForwardIntervalCapture.InitialHauler(f.Manager,e,hs.Count);var stable=StaticHauler(row);if(staticHaulers.TryGetValue(e,out var old))Need(JToken.DeepEquals(old,stable),"v8 unsupported stable hauler/order input change");else{Need(dispatchCount==1,"v8 unsupported new hauler without birth closure");staticHaulers.Add(e,stable);}hs.Add(row);}
   foreach(var pair in f.Context.RuntimeBuildings){var row=S002ForwardIntervalCapture.InitialCandidate(f.Manager,pair.Key,pair.Value,cs.Count);var stable=StaticCandidate(row);if(staticCandidates.TryGetValue(pair.Key,out var old))Need(JToken.DeepEquals(old,stable),"v8 runtime candidate metadata/presence changed");else{Need(dispatchCount==1,"v8 candidate birth unsupported");staticCandidates.Add(pair.Key,stable);}if(pair.Value!=null&&!bindingRefs.ContainsKey(pair.Value))bindingRefs.Add(pair.Value,bindingRefs.Count+1);cs.Add(new JObject { ["enumeration_ordinal"]=cs.Count,["dictionary_key"]=pair.Key,["runtime_id"]=row["runtime_id"]?.DeepClone()??JValue.CreateNull(),["combat_entity"]=row["combat_entity"]?.DeepClone()??JValue.CreateNull(),["runtime_binding_ref"]=pair.Value==null?JValue.CreateNull():(JToken)bindingRefs[pair.Value],["static_equals_first_dispatch_guard"]=true,["storage_assertion"]=row["storage"]?.DeepClone()??JValue.CreateNull(),["profile_assertion"]=row["production_profile"]?.DeepClone()??JValue.CreateNull(),["fabrication_assertion"]=row["fabrication"]?.DeepClone()??JValue.CreateNull() });}
   Need(hs.Count==staticHaulers.Count&&cs.Count==staticCandidates.Count,"v8 complete collection membership changed");
   return new JObject { ["dispatch_id"]=dispatchId,["owner_loop"]=ownerCount,["session_entity"]=E(session),["session_id"]=sessionId,["now_argument_bits"]=Bits(f.Now),["pending_path_argument"]=f.HasPendingPathJob,["grid"]=S002ForwardIntervalCapture.StorageBits(f.Grid),["hauler_order_and_observed_components"]=hs,["candidate_order_and_observed_components"]=cs,["candidate_static_initial_input_ref"]="legacy.initial_hauler_dispatch.full_runtime_buildings_enumeration (component/static inputs only; never legacy provider scope)",["field_partition"]=new JObject { ["static_exact_guard"]=new JArray(HaulerStaticKeys),["exogenous_before_inputs"]=new JArray(HaulerExternalKeys),["computed_or_outside_pre_pair_reads_assertions"]=new JArray(HaulerAssertionKeys),["presence_only_guard"]=new JArray("UnitMove"),["UnitMove_values"]="outside supported pre-pair reads; assertions only",["candidate_static"]="all fields except listed state paths; fabrication recipe/ProductionEnabled/owner exact guard; profile constants exact guard; storage metadata exact guard",["candidate_state_assertions"]=new JArray("storage.Stored*/Reserved*/CivilianFuelReserve/ReceivedHaulerFuel/Version","production_profile.FuelBatchProgressSeconds","fabrication.CycleProgressSeconds/Status/BlockReason/Version") },["private_state_role"]=dispatchCount==1?"initial-input":"assertion-only",["has_signature"]=f.HasSignature,["last_signature"]=f.LastSignature,["next_refresh_bits"]=Bits(f.NextRefreshAt),["next_reservation_id"]=f.NextReservationId,["approach_query_created"]=f.ApproachQueryCreated,["ai_policy_present"]=f.AIPolicyPresent,["frame"]=Time.frameCount };
  });
 }
 static void Selection(EntityManager em,Entity unit,string stage,bool? result) {
  if(!active)return;Record("selector-"+stage,1024,12,()=>{
   Need(Same(em)&&dispatchId!=0,"v8 selector missing dispatch");
   if(stage=="entry"){Need(selectedUnit==Entity.Null,"v8 nested selector");selectedUnit=unit;selectionId++;Need(selectionId<=128,"v8 selector cap");selectedEntry=ordinal;}
   else{Need(selectedUnit==unit&&result.HasValue,"v8 selector exit mismatch/exception");}
   var row=new JObject { ["selection_id"]=selectionId,["dispatch_id"]=dispatchId,["unit"]=E(unit),["entry_ordinal"]=selectedEntry,["result_assertion"]=result.HasValue?(JToken)result.Value:JValue.CreateNull() };
   if(stage=="exit")selectedUnit=Entity.Null;Need(++providerCount<=ProviderLimit,"v8 all-provider event cap");return row;
  });
 }
 static void Provider(S002PhysicalStartupObservation.InitialHaulerProviderFacts f) {
  if(!active)return;Record("selector-provider",2048,20,()=> {Need(Same(f.Manager)&&selectedUnit!=Entity.Null&&++providerCount<=ProviderLimit,"v8 provider outside complete selector/bound");var row=new JObject { ["dispatch_id"]=dispatchId,["selection_id"]=selectionId,["unit"]=E(selectedUnit),["provider_kind"]=f.Kind,["callsite"]=f.Callsite };if(f.Kind=="focus-return"){Need(f.Building!=null,"v8 focus target absent");row["runtime_id"]=f.Building.Id;row["position_bits"]=new JArray(Bits(f.Position.x),Bits(f.Position.y),Bits(f.Position.z));}else{row["faction"]=f.Faction;row["resolver_present"]=f.ResolverPresent;row["has_input"]=f.HasInput;row["input"]=new JArray(f.Input.PlannedMaterialsCost,f.Input.AvailableMaterials,f.Input.MaterialsCapacity,Bits(f.Input.StoredFuelBarrels),f.Input.FuelStorageCapacity);}return row; });
 }
 static void CheckRoots(EntityManager em,NativeArray<Entity> list) {
  if(!active)return;try{Need(list.Length<=RootLimit&&list.Length==roots.Count,"v8 broad root membership changed");Need(Before(5*list.Length+1),"v8 broad root use work");}catch(Exception ex){Fail(ex.Message);return;}
  Record("broad-root-use-guard",2048,list.Length,()=>{var ids=new JArray();var seen=new HashSet<Entity>();foreach(var e in list){bool known=roots.TryGetValue(e,out var old);Need(seen.Add(e)&&known,"v8 broad root identity changed");var s=em.GetComponentData<SkirmishExpandedSessionComponent>(e);Need((string)old["SessionId"]==s.SessionId.ToString()&&(byte)old["IsLegacy"]==s.IsLegacy,"v8 broad root fields changed");ids.Add(E(e));}return new JObject { ["dispatch_id"]=dispatchId,["selection_id"]=selectionId,["exact_order"]=ids };});
 }
 static void FuelBoundary(EntityManager em,string boundary,bool steel,int expanded,bool legacy,int grids,GridConfig grid) {
  if(!active)return;Record("fuel-"+boundary,2048,16,()=>{Need(Same(em)&&!steel&&!legacy&&expanded==1,"v8 unsupported SteelPush/legacy/session routing");Need(grids==1&&grid.CellSize>0&&!float.IsNaN(grid.CellSize)&&!float.IsInfinity(grid.CellSize),"v8 actual unique positive physical grid required");if(boundary=="before-ground-fuel-consumer"){Need(!fuelOpen&&++fuelCount<=FuelLimit,"v8 fuel invocation bound/order");fuelOpen=true;currentFuel.Clear();Array.Clear(drainIndices,0,drainIndices.Length);}else{Need(fuelOpen&&currentFuel.Count==units.Count,"v8 incomplete/mutated global fuel census");fuelOpen=false;}return new JObject { ["fuel_call_id"]=fuelCount,["steel_push"]=steel,["expanded_four_component_count"]=expanded,["legacy_routing"]=legacy,["grid_count"]=grids,["grid"]=S002ForwardIntervalCapture.StorageBits(grid),["frame"]=Time.frameCount };});
 }
 static void FuelUnit(S002PhysicalStartupObservation.ContinuousFuelFacts f) {
  if(!active)return;Record("fuel-unit-before",1024,20,()=>{Need(fuelOpen&&Same(f.Manager)&&currentFuel.Count<UnitLimit&&currentFuel.Add(f.Entity),"v8 complete ordered unique global fuel query bound");var stable=new JArray(f.Faction,f.Motion,f.Enabled,Bits(f.GroundRate),Bits(f.AirRate),f.Air);bool initial=!units.TryGetValue(f.Entity,out var old);Need(!initial||fuelCount==1,"v8 late fuel query birth unsupported");if(initial)units.Add(f.Entity,stable.DeepClone());bool changed=!initial&&!JToken.DeepEquals(old,stable);if(changed)units[f.Entity]=stable.DeepClone();return new JObject { ["fuel_call_id"]=fuelCount,["query_index"]=currentFuel.Count-1,["entity"]=E(f.Entity),["initial_state_input"]=initial,["static_input_delta"]=initial||changed?stable:JValue.CreateNull(),["cell_before_external"]=new JArray(f.Cell.x,f.Cell.y),["air"]=f.Air,["transform_used_presence"]=f.HasTransform,["air_position_bits"]=f.Air&&f.HasTransform?new JArray(Bits(f.Position.x),Bits(f.Position.y),Bits(f.Position.z)):JValue.CreateNull(),["fuel_state_semantics"]=initial?"initial-before-input":"assertion-only-carried-state",["observed_state"]=new JArray(f.LastCell.x,f.LastCell.y,f.Initialized) };});
 }
 static void FuelStatePost(EntityManager em,Entity unit,int2 cell,byte initialized) { if(active)Record("fuel-state-post-assertion",512,4,()=> {Need(fuelOpen&&Same(em)&&currentFuel.Contains(unit),"v8 fuel state write outside ordered query");return new JObject { ["fuel_call_id"]=fuelCount,["entity"]=E(unit),["last_cell_assertion"]=new JArray(cell.x,cell.y),["initialized_assertion"]=initialized };}); }
 static void Requested(NativeArray<float> values) {
  if(!active)return;Record("fuel-requested-assertions",8192,256,()=>{Need(fuelOpen&&values.Length==256,"v8 complete256 requested vector required");var a=new JArray();foreach(var value in values)a.Add(Bits(value));return new JObject { ["fuel_call_id"]=fuelCount,["bits_assertion"]=a };});
 }
 static void DrainVisit(EntityManager em,Entity e,int faction,float remaining) { if(active)Record("fuel-drain-visit",512,2,()=>{Need(fuelOpen&&Same(em)&&faction>=0&&faction<256&&storeIds.Contains(e.Index+":"+e.Version),"v8 drain outside fuel/initial global store universe");lastDrainEntity=e;lastDrainFaction=faction;int visit=drainIndices[faction]++;return new JObject { ["fuel_call_id"]=fuelCount,["entity"]=E(e),["faction"]=faction,["visit_index"]=visit,["remaining_before_bits_assertion"]=Bits(remaining) };}); }
 static void DrainPost(EntityManager em,Entity e,int faction,float remaining,float balance,uint version) { if(active)Record("fuel-drain-post",512,6,()=> {Need(fuelOpen&&Same(em)&&lastDrainEntity==e&&lastDrainFaction==faction,"v8 drain post missing exact visit identity");return new JObject { ["fuel_call_id"]=fuelCount,["entity"]=E(e),["faction"]=faction,["remaining_after_bits_assertion"]=Bits(remaining),["fuel_balance_bits_assertion"]=Bits(balance),["version_assertion"]=version };}); }
 static long TrackSync(Entity entity,RuntimeBuildingEntity building,bool after) { if(!after){Need(pendingSync<0,"v8 nested Sync");pendingSync=ordinal;pendingSyncEntity=entity;pendingSyncBuilding=building;}else Need(pendingSync>=0&&pendingSyncEntity==entity&&ReferenceEquals(pendingSyncBuilding,building),"v8 Sync post missing/mismatched identity pre");long pre=pendingSync;if(after)pendingSync=-1;return pre; }
 static void Sync(S002PhysicalStartupObservation.ContinuousSyncFacts f) {
  if(!active)return;Record(f.After?"sync-after":"sync-before",4096,40,()=>{Need(Same(f.Manager)&&f.Building!=null,"v8 Sync World/binding absent");if(!bindingRefs.TryGetValue(f.Building,out int binding)){Need(dispatchCount==0,"v8 unknown Sync binding");binding=bindingRefs.Count+1;bindingRefs.Add(f.Building,binding);}long pre=TrackSync(f.Building.CombatEntity,f.Building,f.After);return new JObject { ["session_entity"]=E(session),["dispatch_id"]=dispatchId,["production_call_id"]=productionCount,["runtime_binding_ref"]=binding,["runtime_id"]=f.Building.Id,["runtime_metadata_inputs"]=new JObject { ["owner"]=f.Building.OwnerFactionId,["oil_capacity"]=f.Building.OilStorageCapacity,["fuel_capacity"]=f.Building.FuelStorageCapacity,["oil_rate_bits"]=Bits(f.Building.OilBarrelsPerDay),["fuel_rate_bits"]=Bits(f.Building.FuelBarrelsPerDay) },["target_entity"]=E(f.Building.CombatEntity),["pre_ordinal"]=pre,["before_storage_assertion"]=S002ForwardIntervalCapture.StorageBits(f.Before),["prepared_storage_assertion"]=S002ForwardIntervalCapture.StorageBits(f.Prepared),["profile_assertion"]=f.HasProfile?S002ForwardIntervalCapture.StorageBits(f.Profile):JValue.CreateNull(),["oil_mirror_bits_assertion"]=Bits(f.Building.StoredOilBarrels),["fuel_mirror_bits_assertion"]=Bits(f.Building.StoredFuelBarrels) };});
 }
 static void Status(S002PhysicalStartupObservation.ContinuousStatusFacts f) {
  if(!active)return;Record(f.After?"status-after-write-assertion":"status-before-write-assertion",1024,8,()=>{Need(Same(f.Manager)&&selectedUnit==f.Entity,"v8 status write outside selector");bool tray=false;if(staticHaulers.TryGetValue(f.Entity,out var facts))tray=(string)facts["source_prefab_key"]=="Unit_Veh_Truck_Tray";Need(!f.EnteredBlockedEpisode||f.ResourceKind!=0||!tray,"v8 new oil-Tray blocked telemetry query unsupported; no empty-query inference");return new JObject { ["dispatch_id"]=dispatchId,["selection_id"]=selectionId,["unit"]=E(f.Entity),["entered_blocked_episode_assertion"]=f.EnteredBlockedEpisode,["prepared_status_assertion"]=S002ForwardIntervalCapture.StorageBits(f.Status),["telemetry_query_not_reached_source_guard"]=true };});
 }
 static void TrackWriterEntry(long invocation,long parent) { Need(!openCalls.ContainsKey(invocation)&&parent==activeWriter,"v8 duplicate/orphan/out-of-order writer entry");openCalls.Add(invocation,parent);activeWriter=invocation; }
 static void TrackWriterExit(long invocation) { Need(activeWriter==invocation&&openCalls.TryGetValue(invocation,out _),"v8 writer exit before nested child complete");long parent=openCalls[invocation];openCalls.Remove(invocation);activeWriter=parent; }
 static void ValidateRootExit(Entity unit) { Need(openCalls.Count==0&&activeWriter==0&&pendingSync<0&&!fuelOpen&&selectedUnit==unit&&ownerOpen&&ownerPre>=0&&ownerInvoke>=0,"v8 incomplete root window end"); }
 static void Storage(S002PhysicalStartupObservation.StorageWriterFacts f) {
  if(!active||!Before(8))return;try{Need(Same(f.Manager),"v8 writer session mismatch");Need(++writerFacts<=512,"v8 sourcewriter fact cap");}catch(Exception ex){Fail(ex.Message);return;}
  Record("storage-writer",f.Stage=="exit"&&f.Parent==0?1200:400,256,()=>{
   if(f.Stage=="entry"){TrackWriterEntry(f.Invocation,f.Parent);if(f.Parent==0){Need(f.Operation=="ReservePair"&&selectedUnit==f.Unit&&terminalRoot==0,"v8 unsupported root writer/caller");Need(f.Manager.HasComponent<SkirmishAttemptOwnedComponent>(f.Unit),"v8 foreign pair root unsupported");var own=f.Manager.GetComponentData<SkirmishAttemptOwnedComponent>(f.Unit);Need(own.SessionId.ToString()==sessionId,"v8 root not actual attempt owned");Need(staticHaulers.TryGetValue(f.Unit,out var rootHauler)&&rootHauler["attempt_owned"] is JObject,"v8 root ownership initial fact missing");var currentOwned=new JObject { ["SessionId"]=own.SessionId.ToString(),["StableObjectId"]=own.StableObjectId.ToString(),["FactionId"]=own.FactionId,["IsStructure"]=own.IsStructure,["CapacityReleased"]=own.CapacityReleased,["ReservationId"]=own.ReservationId };Need(JToken.DeepEquals(rootHauler["attempt_owned"],currentOwned),"v8 root ownership changed since admitted before facts");terminalRoot=f.Invocation;rootEntry=ordinal;}}
   else Need(openCalls.TryGetValue(f.Invocation,out long parent)&&parent==f.Parent&&activeWriter==f.Invocation,"v8 writer event outside exact active invocation/parent");
   var row=new JObject { ["legacy_writer_index"]=S002ForwardIntervalCapture.LegacyWriterRowCount,["invocation_id"]=f.Invocation,["parent_invocation_id"]=f.Parent==0?JValue.CreateNull():(JToken)f.Parent,["operation"]=f.Operation,["stage"]=f.Stage,["dispatch_id"]=dispatchId,["selection_id"]=selectionId,["unit"]=E(f.Unit) };
   if(f.Stage=="exit"){TrackWriterExit(f.Invocation);if(f.Invocation==terminalRoot){ValidateRootExit(f.Unit);row["window_end_certificate"]=new JObject { ["session_entity"]=E(session),["session_id"]=sessionId,["dispatch_id"]=dispatchId,["selection_id"]=selectionId,["selection_entry_ordinal"]=selectedEntry,["owner_loop"]=ownerCount,["owner_pre_ordinal"]=ownerPre,["owner_invoke_ordinal"]=ownerInvoke,["terminal_owner_after_absent"]=true,["unit"]=E(selectedUnit),["root_invocation_id"]=terminalRoot,["root_entry_ordinal"]=rootEntry,["root_exit_ordinal"]=ordinal,["terminal_selector_exit_absent"]=true,["prior_selector_scopes_closed"]=true,["all_child_calls_closed"]=true,["provider_prefix_complete_through_root_exit"]=true };}}
   return row;
  },true);
  if(active&&f.Stage=="exit"&&f.Invocation==terminalRoot&&openCalls.Count==0){long exit=ordinal-1;TerminalBank(f.Manager,exit);if(!active)return;bytes-=2048;Record("window-end",2048,8,()=>new JObject { ["root_exit_ordinal"]=exit,["terminal_bank_ordinal"]=terminalBankOrdinal,["root_invocation_id"]=terminalRoot,["session_entity"]=E(session),["session_id"]=sessionId,["dispatch_id"]=dispatchId,["selection_id"]=selectionId,["selection_entry_ordinal"]=selectedEntry,["unit"]=E(selectedUnit),["owner_loop"]=ownerCount,["owner_pre_ordinal"]=ownerPre,["owner_invoke_ordinal"]=ownerInvoke,["terminal_selector_exit_absent"]=true,["terminal_owner_after_absent"]=true,["all_prior_scopes_closed"]=true,["all_root_children_closed"]=true });if(active){ended=true;terminalLegacyPending=true;active=false;ClearCallbacks();}}
 }
 static void CheckTerminalStore(string id,bool profile,HashSet<string> seen) { Need(seen.Add(id)&&storeIds.Contains(id),"v8 terminal missing/new/reversioned store identity");Need(profile==initialProfiles[id],"v8 terminal profile presence changed"); }
 static void TerminalBank(EntityManager em,long rootExit) {
  if(!active||!Before(1))return;
  try {
   Need(Same(em)&&BitConverter.IsLittleEndian,"v8 terminal bank World/endian mismatch");
   using var query=em.CreateEntityQuery(ComponentType.ReadOnly<BuildingResourceStorageComponent>());int count=query.CalculateEntityCount();
   Need(count==storeIds.Count&&count<=StoreLimit,"v8 terminal full bank membership count changed");takingTerminalBank=true;
   Record("terminal-full-bank-assertion",TerminalHold,64*count+8,()=> {
    terminalBankOrdinal=ordinal;byte[] buffer=new byte[StoreLimit*94];temporaryUtf8BufferBytes+=buffer.Length;int length;var seen=new HashSet<string>();
    using(var stream=new MemoryStream(buffer,0,buffer.Length,true,true)){stream.SetLength(0);using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))using(var entities=query.ToEntityArray(Allocator.Temp))foreach(var e in entities){string id=e.Index+":"+e.Version;bool profile=em.HasComponent<BuildingResourceProductionProfileComponent>(e);CheckTerminalStore(id,profile,seen);writer.Write(e.Index);writer.Write(e.Version);Write(writer,em.GetComponentData<BuildingResourceStorageComponent>(e));writer.Write((byte)(profile?1:0));if(profile)Write(writer,em.GetComponentData<BuildingResourceProductionProfileComponent>(e));}length=checked((int)stream.Length);}
    return new JObject { ["root_exit_ordinal"]=rootExit,["session_entity"]=E(session),["assertion_only_never_reseed"]=true,["query"]="BuildingResourceStorageComponent defaultquery",["encoding"]="entity8-storage57-profileflag1-profile28-le-v1",["count"]=count,["wire_bytes"]=length,["rows_base64"]=Convert.ToBase64String(buffer,0,length) };
   });if(active)terminalBankDone=true;
  }catch(Exception ex){Fail(ex.Message);}finally{takingTerminalBank=false;}
 }
 static void Write(BinaryWriter w,BuildingResourceStorageComponent v){w.Write(v.RuntimeBuildingId);w.Write(v.OwnerFactionId);w.Write(v.OilStorageCapacity);w.Write(v.FuelStorageCapacity);w.Write(v.OilBarrelsPerDay);w.Write(v.FuelBarrelsPerDay);w.Write(v.StoredOilBarrels);w.Write(v.StoredFuelBarrels);w.Write(v.ReservedOilInboundBarrels);w.Write(v.ReservedOilOutboundBarrels);w.Write(v.ReservedFuelInboundBarrels);w.Write(v.ReservedFuelOutboundBarrels);w.Write(v.CivilianFuelReserveBarrels);w.Write(v.ReceivedHaulerFuelBarrels);w.Write(v.Version);}
 static void Write(BinaryWriter w,BuildingResourceProductionProfileComponent v){w.Write(v.PeriodSeconds);w.Write(v.OilPerPeriod);w.Write(v.FuelPerPeriod);w.Write(v.OilPerFuelBatch);w.Write(v.FuelPerBatch);w.Write(v.FuelBatchSeconds);w.Write(v.FuelBatchProgressSeconds);}
 static void ValidateFinalBytes(int actual) { Need(actual>0&&actual+legacyCausalBytes<=ByteLimit&&actual+legacyCausalBytes<=bytes,"v8 actual final compactUTF8/framing exceeds aggregate reserved byte budget"); }
 internal static JObject Export(string outputDirectory) {
  if(!opted)return null;if(!ended&&failure==null)Fail("v8 export before complete declared root EXIT frontier");if(cachedReference!=null)return (JObject)cachedReference.DeepClone();
  try {
   int finalWork=checked(2*(acceptedJsonNodes+EndHold)+1);Need(!terminalLegacyPending&&(!ended||terminalBankDone),"v8 terminal legacy root row/bank missing");Need(work<=WorkLimit-finalWork,"v8 final serializer pre-allocation work exhausted");work+=finalWork;
   long timerFrequency=System.Diagnostics.Stopwatch.Frequency;Need(recordCallbackStopwatchTicks>=0&&recordCallbackMeasurementCalls>=0&&recordCallbackMeasurementCalls<=GlobalEventLimit+1L&&timerFrequency>0,"v8 invalid partial Record timing counters");
   var artifact=new JObject { ["schema_version"]=8,["diagnostic_budget_revision"]="aggregate-2MiB-record-timing-v1",["ordinal_space"]="v8-causal-window-only",["legacy_v7_space_distinct"]=true,["start_observed"]=world!=null,["end_observed"]=ended,["window_complete"]=ended&&failure==null&&S002PhysicalStartupObservation.ContinuousFailure==null,["failure"]=failure??S002PhysicalStartupObservation.ContinuousFailure,["charged_work"]=work,["aggregate_causal_charged_utf8_bytes_including_header_reserve"]=bytes,["legacy_early_causal_pretty_utf8_bytes_including_indent_separator"]=legacyCausalBytes,["temporary_utf8_buffer_allocated_bytes"]=temporaryUtf8BufferBytes,["managed_object_allocations_measurement"]="not-measured; fixed byte buffers accounted separately",["end_header_reserved_bytes"]=EndHold,["work_limit"]=WorkLimit,["byte_limit"]=ByteLimit,["global_event_limit"]=GlobalEventLimit,["source_event_counts"]=JObject.FromObject(kindCounts),["sourcewriter_facts"]=writerFacts,["root_rows_including_end_header"]=rootRows,["owner_loops"]=ownerCount,["dispatches"]=dispatchCount,["fuel_calls"]=fuelCount,["provider_events"]=providerCount,["timing_measurement"]="Record-body-only; excludes pre-Record source reads/copies, legacy snapshots/serialization, final export and logging; not total observer overhead",["record_callback_stopwatch_ticks"]=recordCallbackStopwatchTicks,["record_callback_measurement_calls"]=recordCallbackMeasurementCalls,["stopwatch_frequency"]=timerFrequency,["events"]=events };
   string json=artifact.ToString(Formatting.None);int actual=Encoding.UTF8.GetByteCount(json);
   ValidateFinalBytes(actual);
   byte[] raw=Encoding.UTF8.GetBytes(json);temporaryUtf8BufferBytes=checked(temporaryUtf8BufferBytes+raw.Length);string path=Path.Combine(outputDirectory,"continuous-causal-window-v8.json");Need(!File.Exists(path),"v8 refuses to overwrite an existing sidecar");
   using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)){file.Write(raw,0,raw.Length);file.Flush();}string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(raw)).Replace("-","").ToLowerInvariant();
   cachedReference=new JObject { ["schema_version"]=8,["diagnostic_byte_limit"]=ByteLimit,["path"]="continuous-causal-window-v8.json",["bytes"]=raw.Length,["sha256"]=hash,["window_complete"]=(bool)artifact["window_complete"],["failure"]=artifact["failure"],["charged_work"]=work,["aggregate_causal_actual_utf8_bytes"]=raw.Length+legacyCausalBytes,["legacy_early_causal_utf8_bytes"]=legacyCausalBytes,["temporary_utf8_buffer_allocated_bytes"]=temporaryUtf8BufferBytes,["events"]=ordinal,["sourcewriter_facts"]=writerFacts,["ordinal_space"]="v8-causal-window-only" };
   Debug.Log("[S002ContinuousCausalWindowV8] result="+((bool)artifact["window_complete"]?"Passed":"Failed")+" scope=FirstS002ProductionBeforeToFirstOwnedReservePairRootExit work="+work+" bytes="+raw.Length+" aggregateCausalBytes="+(raw.Length+legacyCausalBytes)+" events="+ordinal+" sourceFacts="+writerFacts+" kindCounts="+JObject.FromObject(kindCounts).ToString(Formatting.None));
   return (JObject)cachedReference.DeepClone();
  }catch(Exception ex){Fail(ex.Message);Debug.LogError("[S002ContinuousCausalWindowV8] result=Failed scope=FirstS002ProductionBeforeToFirstOwnedReservePairRootExit reason="+failure+" work="+work+" bytes="+bytes+" events="+ordinal);return new JObject { ["schema_version"]=8,["window_complete"]=false,["failure"]=failure,["path"]=JValue.CreateNull(),["bytes"]=0,["sha256"]=JValue.CreateNull() };}
 }
 internal static void Detach() {
  if(opted&&!ended&&failure==null)Fail("v8 detached before complete root EXIT");
  ClearCallbacks();
 }
 static void ClearCallbacks() {
  S002PhysicalStartupObservation.ContinuousOwner=null;S002PhysicalStartupObservation.ContinuousDispatch=null;S002PhysicalStartupObservation.ContinuousSelection=null;S002PhysicalStartupObservation.ContinuousProvider=null;S002PhysicalStartupObservation.ContinuousFuel=null;S002PhysicalStartupObservation.ContinuousFuelStatePost=null;S002PhysicalStartupObservation.ContinuousFuelBoundary=null;S002PhysicalStartupObservation.ContinuousRequested=null;S002PhysicalStartupObservation.ContinuousDrainVisit=null;S002PhysicalStartupObservation.ContinuousDrainPost=null;S002PhysicalStartupObservation.ContinuousRoots=null;S002PhysicalStartupObservation.ContinuousSync=null;S002PhysicalStartupObservation.ContinuousStatus=null;S002PhysicalStartupObservation.ContinuousStorage=null;S002PhysicalStartupObservation.ContinuousBeforeRead=null;S002PhysicalStartupObservation.ContinuousFailed=null;
 }
}
}
#endif

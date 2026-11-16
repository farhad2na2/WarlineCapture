#if UNITY_EDITOR
using System;using System.Collections.Generic;using Game.Components;using Game.Runtime;using Newtonsoft.Json.Linq;using Unity.Collections;using Unity.Entities;using UnityEngine;
namespace Game.Editor {
internal static partial class S002ContinuousCausalWindowCapture {
 const int MaterialHold=18*4096+16*8192+4096, MaterialFabricationLimit=18, MaterialOwnerLimit=16, MaterialUpdateLimit=128, MaterialIngressLimit=128;
 const string MaterialRevision="v8-material-subsystem-v2-source-order";
 static bool materialSeedDone, terminalMaterialDone, takingTerminalMaterial, materialSystemSeen, materialRequestsComplete, materialDeltaSeen, materialLookupComplete, materialTicksComplete;
 static SystemHandle materialSystem;static byte materialSimulationActive;
 static S002PhysicalStartupObservation.MaterialSourceScope materialScope, materialIngressScope;
 static int materialUpdates, materialIngressEvents, materialRequestsSeen, materialLookupsSeen, materialTicksSeen;static Entity materialRequestEntity=Entity.Null, materialTickEntity=Entity.Null;
 static long terminalMaterialOrdinal=-1;
 static readonly Dictionary<Entity,JToken> fabricationInitialGuards=new Dictionary<Entity,JToken>(), materialInitialGuards=new Dictionary<Entity,JToken>();
 static int MaterialReservation=>terminalMaterialDone||takingTerminalMaterial?0:MaterialHold;
 static void AttachMaterial(){S002PhysicalStartupObservation.ContinuousMaterialAdmission=MaterialAdmission;S002PhysicalStartupObservation.ContinuousMaterial=Material;}
 static bool MaterialAdmission(EntityManager em,bool ingress){
  if(!active)return false;
  try {if(!ReferenceEquals(world,em.World))return false;Need(Same(em)&&materialSeedDone,"material boundary without complete seed/same session");Need(Before(768),"material source scope pre-allocation admission");
   if(ingress){Need(materialIngressScope==null&&materialIngressEvents<MaterialIngressLimit,"material ingress bound/nesting");materialIngressEvents++;}
   else{Need(materialScope==null&&materialUpdates<MaterialUpdateLimit,"material update bound/nesting");materialUpdates++;}
   return true;
  }catch(Exception ex){Fail(ex.Message);return false;}
 }
 static JToken MC<T>(EntityManager em,Entity e)where T:unmanaged,IComponentData=>e!=Entity.Null&&em.Exists(e)&&em.HasComponent<T>(e)?S002ForwardIntervalCapture.StorageBits(em.GetComponentData<T>(e)):JValue.CreateNull();
 static JToken MB<T>(EntityManager em,Entity e,int limit)where T:unmanaged,IBufferElementData{
  if(e==Entity.Null||!em.Exists(e)||!em.HasBuffer<T>(e))return JValue.CreateNull();return BufferBits(em.GetBuffer<T>(e,true),limit);
 }
 static JArray BufferBits<T>(DynamicBuffer<T> buffer,int limit)where T:unmanaged,IBufferElementData{
  Need(buffer.IsCreated&&buffer.Length<=limit,"material borrowed buffer count bound");var rows=new JArray();for(int i=0;i<buffer.Length;i++)rows.Add(S002ForwardIntervalCapture.StorageBits(buffer[i]));return rows;
 }
 static JObject FabricationGuard(EntityManager em,Entity e,in MaterialFabricationComponent fabrication){
  var config=(JObject)S002ForwardIntervalCapture.StorageBits(fabrication);foreach(var k in new[]{"ProductionEnabled","CycleProgressSeconds","Status","BlockReason","Version"})config.Remove(k);
  return new JObject{["configuration"]=config,["input_tag_present"]=em.HasComponent<MaterialFabricationInputTag>(e),["health_bits_or_null"]=MC<UnitHealth>(em,e),["disabled_present"]=em.HasComponent<Disabled>(e),["death_animation_present"]=em.HasComponent<UnitDeathAnimationComponent>(e),["command_queue_present"]=em.HasComponent<MaterialFabricationCommandQueueComponent>(e),["request_buffer_present"]=em.HasBuffer<MaterialFabricationRequestComponent>(e),["result_buffer_present"]=em.HasBuffer<MaterialFabricationResultComponent>(e)};
 }
 static void CheckFab(EntityManager em,Entity e,in MaterialFabricationComponent f){Need(fabricationInitialGuards.TryGetValue(e,out var initial)&&JToken.DeepEquals(initial,FabricationGuard(em,e,f)),"material fabrication identity/static health/tag/configuration/presence changed");}
 static JObject MaterialGuard(EntityManager em,Entity e,in FactionTacticalMaterialsComponent m)=>new JObject{["faction_id"]=m.FactionId,["faction_economy_faction_id_or_null"]=em.HasComponent<FactionEconomy>(e)?(JToken)em.GetComponentData<FactionEconomy>(e).FactionId:JValue.CreateNull(),["disabled_present"]=em.HasComponent<Disabled>(e),["telemetry_present"]=em.HasComponent<FactionMaterialFabricationTelemetryComponent>(e),["economy_event_queue_present"]=em.HasComponent<MaterialFabricationEconomyEventQueueComponent>(e),["economy_event_buffer_present"]=em.HasBuffer<MaterialFabricationEconomyEventElement>(e)};
 static void CheckMaterial(EntityManager em,Entity e,in FactionTacticalMaterialsComponent m){Need(materialInitialGuards.TryGetValue(e,out var initial)&&JToken.DeepEquals(initial,MaterialGuard(em,e,m)),"material owner identity/static faction/presence changed");}
 static JObject FabRow(EntityManager em,Entity e,bool initial){
  var f=em.GetComponentData<MaterialFabricationComponent>(e);if(initial)fabricationInitialGuards.Add(e,FabricationGuard(em,e,f));else CheckFab(em,e,f);
  return new JObject{["entity"]=E(e),["fabrication_bits"]=S002ForwardIntervalCapture.StorageBits(f),["input_tag_present"]=em.HasComponent<MaterialFabricationInputTag>(e),["health_bits_or_null"]=MC<UnitHealth>(em,e),["disabled_present"]=em.HasComponent<Disabled>(e),["death_animation_present"]=em.HasComponent<UnitDeathAnimationComponent>(e),["command_queue_bits_or_null"]=MC<MaterialFabricationCommandQueueComponent>(em,e),["request_buffer_or_null"]=MB<MaterialFabricationRequestComponent>(em,e,8),["result_buffer_or_null"]=MB<MaterialFabricationResultComponent>(em,e,8)};
 }
 static JObject MaterialRow(EntityManager em,Entity e,bool initial){
  var m=em.GetComponentData<FactionTacticalMaterialsComponent>(e);if(initial)materialInitialGuards.Add(e,MaterialGuard(em,e,m));else CheckMaterial(em,e,m);
  return new JObject{["entity"]=E(e),["tactical_materials_bits"]=S002ForwardIntervalCapture.StorageBits(m),["faction_economy_faction_id_or_null"]=em.HasComponent<FactionEconomy>(e)?(JToken)em.GetComponentData<FactionEconomy>(e).FactionId:JValue.CreateNull(),["disabled_present"]=em.HasComponent<Disabled>(e),["fabrication_telemetry_bits_or_null"]=MC<FactionMaterialFabricationTelemetryComponent>(em,e),["economy_event_queue_bits_or_null"]=MC<MaterialFabricationEconomyEventQueueComponent>(em,e),["economy_event_buffer_or_null"]=MB<MaterialFabricationEconomyEventElement>(em,e,16)};
 }
 static void MaterialSnapshot(EntityManager em,bool initial,long rootExit){
  if(!active||!Before(8))return;
  try{
   Need(Same(em)&&materialScope==null&&materialIngressScope==null,"material census overlapping open source scope");
   using var fq=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<MaterialFabricationComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities});
   using var mq=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<FactionTacticalMaterialsComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities});
   int fn=fq.CalculateEntityCount(),mn=mq.CalculateEntityCount();Need(fn<=MaterialFabricationLimit&&mn<=MaterialOwnerLimit,"complete material seed/terminal domain count cap");
   if(!initial)Need(fn==fabricationInitialGuards.Count&&mn==materialInitialGuards.Count,"material terminal birth/despawn/presence changed");takingTerminalMaterial=!initial;
   Record(initial?"initial-material-subsystem":"terminal-material-subsystem-assertion",MaterialHold,1024*fn+2048*mn+64,()=>{
    if(!initial)terminalMaterialOrdinal=ordinal;var fs=new JArray();var ms=new JArray();var seen=new HashSet<Entity>();
    using(var es=fq.ToEntityArray(Allocator.Temp))foreach(var e in es){Need(seen.Add(e),"duplicate material fabrication entity");var row=FabRow(em,e,initial);Need(System.Text.Encoding.UTF8.GetByteCount(row.ToString(Newtonsoft.Json.Formatting.None))<=4096,"material fabrication row static bound");fs.Add(row);}seen.Clear();
    using(var es=mq.ToEntityArray(Allocator.Temp))foreach(var e in es){Need(seen.Add(e),"duplicate tactical material entity");var row=MaterialRow(em,e,initial);Need(System.Text.Encoding.UTF8.GetByteCount(row.ToString(Newtonsoft.Json.Formatting.None))<=8192,"material owner row static bound");ms.Add(row);}
    var result=new JObject{["fabrication_rows"]=fs,["material_rows"]=ms};if(!initial){result["root_exit_ordinal"]=rootExit;result["assertion_only_never_reseed"]=true;}return result;
   });if(active){if(initial)materialSeedDone=true;else terminalMaterialDone=true;}
  }catch(Exception ex){Fail(ex.Message);}finally{takingTerminalMaterial=false;}
 }
 static JToken MaterialSide<T>(EntityManager em,Entity e)where T:unmanaged,IComponentData=>e==Entity.Null?JValue.CreateNull():MC<T>(em,e);
 static void Material(S002PhysicalStartupObservation.MaterialFacts f){
  if(!active)return;
  try{Need(f.Scope!=null&&Same(f.Scope.Manager),"material actual source World/session missing");var em=f.Scope.Manager;
   if(f.Stage=="update-before"){
    Need(materialScope==null&&!f.Scope.Ingress,"material update scope nesting");if(materialSystemSeen)Need(materialSystem.Equals(f.Scope.System),"material actual SystemHandle changed");else{materialSystem=f.Scope.System;materialSystemSeen=true;}materialScope=f.Scope;f.Scope.Id=materialUpdates;materialRequestsSeen=materialLookupsSeen=materialTicksSeen=0;materialRequestsComplete=materialDeltaSeen=materialLookupComplete=materialTicksComplete=false;materialRequestEntity=materialTickEntity=Entity.Null;
    Record("material-update-before",1024,16,()=>new JObject{["material_update_id"]=f.Scope.Id,["system_handle_same_source_guard"]=true,["frame"]=Time.frameCount,["elapsed_before_bits"]=Bits(f.Elapsed),["elapsed_role"]=materialUpdates==1?"initial-private-input":"assertion-only-carried-private"});return;
   }
   if(f.Stage=="ingress-before"){
    Need(materialIngressScope==null&&f.Scope.Ingress,"material ingress nesting");materialIngressScope=f.Scope;f.Scope.Id=materialIngressEvents;
    Record("material-request-ingress-before",4096,1024,()=>{bool exists=f.Entity!=Entity.Null&&em.Exists(f.Entity);bool fab=exists&&em.HasComponent<MaterialFabricationComponent>(f.Entity);if(fab)CheckFab(em,f.Entity,em.GetComponentData<MaterialFabricationComponent>(f.Entity));return new JObject{["ingress_id"]=f.Scope.Id,["entity"]=E(f.Entity),["requester_faction_id"]=f.Scope.Requester,["production_enabled"]=f.Scope.Enabled,["queue_before_assertion_or_null"]=MC<MaterialFabricationCommandQueueComponent>(em,f.Entity),["requests_before_assertion_or_null"]=MB<MaterialFabricationRequestComponent>(em,f.Entity,8),["results_presence_assertion"]=exists&&em.HasBuffer<MaterialFabricationResultComponent>(f.Entity),["entity_exists_assertion"]=exists,["fabrication_present_assertion"]=fab};});return;
   }
   if(f.Stage=="ingress-after"){
    Need(ReferenceEquals(materialIngressScope,f.Scope)&&f.Success.HasValue,"material ingress missing normal source return");
    Record("material-request-ingress-after",4096,1024,()=>new JObject{["ingress_id"]=f.Scope.Id,["entity"]=E(f.Entity),["success_assertion"]=f.Success.Value,["request_id_assertion"]=f.RequestId,["queue_after_assertion_or_null"]=MC<MaterialFabricationCommandQueueComponent>(em,f.Entity),["requests_after_assertion_or_null"]=MB<MaterialFabricationRequestComponent>(em,f.Entity,8)});materialIngressScope=null;return;
   }
   Need(ReferenceEquals(materialScope,f.Scope),"material event outside exact actual update scope");
   if(f.Stage=="request-owner-before"||f.Stage=="request-owner-after"){
    bool after=f.Stage.EndsWith("after",StringComparison.Ordinal);Need(!materialRequestsComplete&&f.Index==materialRequestsSeen&&f.Index<MaterialFabricationLimit&&(after?materialRequestEntity==f.Entity:materialRequestEntity==Entity.Null),"material request iterator prefix/pair differs");
    Record("material-"+f.Stage,4096,1024,()=>{CheckFab(em,f.Entity,f.Fabrication);return new JObject{["material_update_id"]=f.Scope.Id,["request_query_index"]=f.Index,["entity"]=E(f.Entity),["fabrication_assertion"]=S002ForwardIntervalCapture.StorageBits(f.Fabrication),["requests_assertion"]=BufferBits(f.Requests,8),["results_assertion"]=BufferBits(f.Results,8)};});if(after){materialRequestEntity=Entity.Null;materialRequestsSeen++;}else materialRequestEntity=f.Entity;return;
   }
   if(f.Stage=="requests-complete"){
    Need(!materialRequestsComplete&&materialRequestEntity==Entity.Null&&f.Count==materialRequestsSeen,"material requests incomplete");materialRequestsComplete=true;materialSimulationActive=f.Simulation;
    Record("material-requests-complete",512,8,()=>new JObject{["material_update_id"]=f.Scope.Id,["request_owner_count"]=f.Count,["simulation_active"]=f.Simulation});return;
   }
   if(f.Stage=="delta-input"){
    Need(materialRequestsComplete&&materialSimulationActive!=0&&!materialDeltaSeen,"material delta source order");materialDeltaSeen=true;Record("material-delta-input",512,4,()=>new JObject{["material_update_id"]=f.Scope.Id,["delta_time_bits"]=Bits(f.Delta)});return;
   }
   if(f.Stage=="lookup-row"){
    Need(materialDeltaSeen&&!materialLookupComplete&&f.Index==materialLookupsSeen&&f.Index<MaterialOwnerLimit,"material actual lookup iterator order/count cap");Record("material-lookup-row",2048,512,()=>{CheckMaterial(em,f.Entity,f.Materials);return new JObject{["material_update_id"]=f.Scope.Id,["lookup_query_index"]=f.Index,["entity"]=E(f.Entity),["faction_economy_faction_id"]=f.Simulation,["tactical_materials_assertion"]=S002ForwardIntervalCapture.StorageBits(f.Materials)};});materialLookupsSeen++;return;
   }
   if(f.Stage=="lookup-complete"){
    Need(materialDeltaSeen&&!materialLookupComplete&&f.Count==materialLookupsSeen,"material lookup incomplete");materialLookupComplete=true;Record("material-lookup-complete",512,4,()=>new JObject{["material_update_id"]=f.Scope.Id,["lookup_owner_count"]=f.Count});return;
   }
   if(f.Stage=="tick-before"){
    Need(materialLookupComplete&&!materialTicksComplete&&materialTickEntity==Entity.Null&&f.Index==materialTicksSeen&&f.Index<MaterialFabricationLimit&&storeIds.Contains(f.Entity.Index+":"+f.Entity.Version),"material exact tick iterator/bank owner differs");
    Record("material-tick-before",4096,1024,()=>{CheckFab(em,f.Entity,f.Fabrication);if(f.HasMaterials)CheckMaterial(em,f.Selected,f.Materials);return new JObject{["material_update_id"]=f.Scope.Id,["tick_query_index"]=f.Index,["entity"]=E(f.Entity),["fabrication_assertion"]=S002ForwardIntervalCapture.StorageBits(f.Fabrication),["storage_assertion"]=S002ForwardIntervalCapture.StorageBits(f.Storage),["health_current_input"]=f.Health,["disabled_input"]=f.Disabled,["death_animation_input"]=f.Death,["building_operational_assertion"]=f.Operational,["selected_material_entity_assertion_or_null"]=f.Selected==Entity.Null?JValue.CreateNull():E(f.Selected),["material_owner_count_assertion"]=f.OwnerCount,["tactical_materials_assertion_or_null"]=f.HasMaterials?S002ForwardIntervalCapture.StorageBits(f.Materials):JValue.CreateNull()};});materialTickEntity=f.Entity;return;
   }
   if(f.Stage=="tick-after"){
    Need(materialTickEntity==f.Entity&&f.Index==materialTicksSeen,"material tick source after without before");
    Record("material-tick-after",12288,2048,()=>{CheckFab(em,f.Entity,f.Fabrication);if(f.HasMaterials)CheckMaterial(em,f.Selected,f.Materials);return new JObject{["material_update_id"]=f.Scope.Id,["tick_query_index"]=f.Index,["entity"]=E(f.Entity),["fabrication_assertion"]=S002ForwardIntervalCapture.StorageBits(f.Fabrication),["storage_assertion"]=S002ForwardIntervalCapture.StorageBits(f.Storage),["selected_material_entity_assertion_or_null"]=f.Selected==Entity.Null?JValue.CreateNull():E(f.Selected),["tactical_materials_assertion_or_null"]=f.HasMaterials?S002ForwardIntervalCapture.StorageBits(f.Materials):JValue.CreateNull(),["tick_result_assertion"]=f.Applied?S002ForwardIntervalCapture.StorageBits(f.Tick):JValue.CreateNull(),["telemetry_assertion_or_null"]=MaterialSide<FactionMaterialFabricationTelemetryComponent>(em,f.Selected),["economy_event_queue_assertion_or_null"]=MaterialSide<MaterialFabricationEconomyEventQueueComponent>(em,f.Selected),["economy_event_buffer_assertion_or_null"]=MB<MaterialFabricationEconomyEventElement>(em,f.Selected,16),["tick_applied_assertion"]=f.Applied};});materialTickEntity=Entity.Null;materialTicksSeen++;return;
   }
   if(f.Stage=="ticks-complete"){
    Need(materialLookupComplete&&!materialTicksComplete&&materialTickEntity==Entity.Null&&f.Count==materialTicksSeen,"material ticks incomplete");materialTicksComplete=true;Record("material-ticks-complete",512,4,()=>new JObject{["material_update_id"]=f.Scope.Id,["tick_owner_count"]=f.Count});return;
   }
   if(f.Stage=="update-after"){
    Need(f.Normal&&materialRequestsComplete&&(materialSimulationActive!=0?materialDeltaSeen:!materialDeltaSeen)&&materialRequestEntity==Entity.Null&&materialTickEntity==Entity.Null&&f.Count==materialRequestsSeen&&f.Index==materialTicksSeen&&f.LookupReached==materialLookupComplete&&(!f.LookupReached||materialTicksComplete),"material source exception/incomplete normal return");
    Record("material-update-after",1024,8,()=>new JObject{["material_update_id"]=f.Scope.Id,["frame"]=Time.frameCount,["elapsed_after_bits"]=Bits(f.Elapsed),["completed_normally"]=f.Normal,["request_owner_count"]=f.Count,["lookup_reached_assertion"]=f.LookupReached,["tick_owner_count"]=f.Index});materialScope=null;return;
   }
   throw new InvalidOperationException("unknown material source stage "+f.Stage);
  }catch(Exception ex){Fail(ex.Message);}
 }
}
}
#endif

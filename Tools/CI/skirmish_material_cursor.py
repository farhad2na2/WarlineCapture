"""Pinned material event/identity cursor only; no numerical scheduler or storage bank."""
from pathlib import Path
import hashlib,json
CONTRACT=Path(__file__).parent/'SkirmishOfflineSim/RetainedCausal/contracts/material-row-contract.json'
CONTRACT_SHA='a64f079544ac1c039f0f7fb409ed35e80fd892bf8ea57794b3d2110a23dbdf44'
raw=CONTRACT.read_bytes()
if hashlib.sha256(raw).hexdigest()!=CONTRACT_SHA:raise ValueError('Frozen material row contract changed')
C=json.loads(raw);REVISION=C['revision']
FAB={'RuntimeBuildingId':'i','OwnerFactionId':'b','ProductionEnabled':'b','OutputCapacityPolicy':'b','OilConsumedPerCycle':'f','MaterialsOutputPerCycle':'i','CycleDurationSeconds':'f','CycleProgressSeconds':'f','Status':'b','BlockReason':'b','Version':'u'}
MAT={k:('b' if k=='FactionId' else 'u' if k=='Version' else 'i')for k in ['FactionId','Current','Capacity','LifetimeFabricated','LifetimeImported','LifetimeRewarded','LifetimeExported','LifetimeSpent','LifetimeConstructionSpent','LifetimeRepairSpent','LifetimeInfrastructureSpent','LifetimeUpgradeSpent','LifetimeProductionSpent','Version']}
TELE={k:('b' if k=='FactionId' else 'u' if k=='Version' else 'f')for k in C['side_state_fields']['telemetry']}
REQUEST={'RequestId':'i','RequesterFactionId':'b','ProductionEnabled':'b','Kind':'b'}
RESULT={'RequestId':'i','Accepted':'b','ProductionEnabled':'b','Code':'b','FabricationVersion':'u'}
EVENT={k:('f' if k=='OilConsumedBarrels' else 'b' if k in ('FactionId','EventKind','Status','BlockReason')else 'i')for k in C['side_state_fields']['event']}
QUEUE={'LastRequestId':'i'};EQUEUE={'LastEventId':'i','Version':'u'};HEALTH={'Current':'i','Max':'i'};TICK={'CompletedCycles':'i','OilConsumedBarrels':'f','MaterialsProduced':'i','ActiveSeconds':'f'}
STORE={k:('b' if k=='OwnerFactionId' else 'u' if k=='Version' else 'i' if k in ('RuntimeBuildingId','OilStorageCapacity','FuelStorageCapacity')else 'f')for k in ['RuntimeBuildingId','OwnerFactionId','OilStorageCapacity','FuelStorageCapacity','OilBarrelsPerDay','FuelBarrelsPerDay','StoredOilBarrels','StoredFuelBarrels','ReservedOilInboundBarrels','ReservedOilOutboundBarrels','ReservedFuelInboundBarrels','ReservedFuelOutboundBarrels','CivilianFuelReserveBarrels','ReceivedHaulerFuelBarrels','Version']}
class MaterialCursor:
 def __init__(self,v,initial_store_ids):
  self.v=v;self.stores=set(initial_store_ids);self.fabs=None;self.materials=None;self.frame=None;self.ingress=None;self.updates=0;self.ingresses=0;self.terminal=None
 def need(self,x,s):self.v.need(x,'Material cursor: '+s)
 def scalar(self,x,t):
  if t=='f':self.v.f32(x)
  else:self.v.integer(x,*({'i':(-2147483648,2147483647),'u':(0,0xffffffff),'b':(0,255)}[t]))
 def component(self,x,schema,nullable=False):
  if nullable and x is None:return
  self.v.exact(x,schema)
  for k,t in schema.items():self.scalar(x[k],t)
 def buffer(self,x,schema,maxn,nullable=False):
  if nullable and x is None:return
  for r in self.v.array(x,maximum=maxn):self.component(r,schema)
 def identity(self,x):
  i=self.v.identity(x);self.need(i[0]>0,'Positive typed Entity index required');return i
 def snapshot(self,row,terminal=False):
  fs={};ms={}
  for r in self.v.array(row['fabrication_rows'],maximum=18):
   self.v.exact(r,C['initial_fabrication_row']);e=self.identity(r['entity']);self.need(e not in fs,'Duplicate fabrication seed/terminal owner');fs[e]=r
   self.component(r['fabrication_bits'],FAB)
   for k in ('input_tag_present','disabled_present','death_animation_present'):self.v.flag(r[k])
   self.component(r['health_bits_or_null'],HEALTH,True);self.component(r['command_queue_bits_or_null'],QUEUE,True)
   self.buffer(r['request_buffer_or_null'],REQUEST,8,True);self.buffer(r['result_buffer_or_null'],RESULT,8,True)
  for r in self.v.array(row['material_rows'],maximum=16):
   self.v.exact(r,C['initial_material_row']);e=self.identity(r['entity']);self.need(e not in ms,'Duplicate tactical materials seed/terminal owner');ms[e]=r
   self.component(r['tactical_materials_bits'],MAT);self.v.flag(r['disabled_present'])
   if r['faction_economy_faction_id_or_null'] is not None:self.scalar(r['faction_economy_faction_id_or_null'],'b')
   self.component(r['fabrication_telemetry_bits_or_null'],TELE,True);self.component(r['economy_event_queue_bits_or_null'],EQUEUE,True);self.buffer(r['economy_event_buffer_or_null'],EVENT,16,True)
  if not terminal:self.fabs=fs;self.materials=ms;return
  self.need(set(fs)==set(self.fabs)and set(ms)==set(self.materials),'Terminal full material domains differ')
  for e,r in fs.items():
   seed=self.fabs[e]
   for k in ('input_tag_present','disabled_present','death_animation_present'):self.need(r[k]==seed[k],'Unmodeled terminal fabrication prerequisite change')
   for k in ('health_bits_or_null','command_queue_bits_or_null','request_buffer_or_null','result_buffer_or_null'):self.need((r[k] is None)==(seed[k] is None),'Terminal fabrication presence changed')
  for e,r in ms.items():
   seed=self.materials[e]
   for k in ('faction_economy_faction_id_or_null','disabled_present'):self.need(r[k]==seed[k],'Terminal material lookup static input changed')
   for k in ('fabrication_telemetry_bits_or_null','economy_event_queue_bits_or_null','economy_event_buffer_or_null'):self.need((r[k] is None)==(seed[k] is None),'Terminal material side-state presence changed')
 def request_domain(self):return {e for e,r in self.fabs.items()if r['input_tag_present']and r['request_buffer_or_null']is not None and r['result_buffer_or_null']is not None}
 def lookup_domain(self):return {e for e,r in self.materials.items()if not r['disabled_present']and r['faction_economy_faction_id_or_null']is not None}
 def tick_domain(self):return {e for e,r in self.fabs.items()if e in self.stores and r['input_tag_present']and r['health_bits_or_null']is not None}
 def consume(self,row,*,ordinary_outer,seed_frontier=False,root_exit=None):
  kind=row['kind'];self.need(kind in C['events'],'Unknown source material row');self.v.exact(row,C['events'][kind]['fields']+['kind','observer_ordinal'])
  for key in ('material_update_id','ingress_id'):
   if key in row:self.v.integer(row[key],1,128)
  for key,limit in (('request_query_index',17),('lookup_query_index',15),('tick_query_index',17),('request_owner_count',18),('lookup_owner_count',16),('tick_owner_count',18)):
   if key in row:self.v.integer(row[key],0,limit)
  if 'root_exit_ordinal'in row:self.v.integer(row['root_exit_ordinal'],0,131071)
  if kind=='initial-material-subsystem':
   self.need(self.fabs is None and seed_frontier,'Exactly one material seed at initial BEFORE production frontier');self.snapshot(row);return
  self.need(self.fabs is not None,'Material row before initial seed')
  if kind=='terminal-material-subsystem-assertion':
   self.need(self.frame is None and self.ingress is None and self.terminal is None and root_exit is not None,'Material terminal frontier not closed')
   self.need(row['root_exit_ordinal']==root_exit and row['observer_ordinal']==root_exit+1,'Material terminal immediately follows actual ROOT EXIT');self.v.flag(row['assertion_only_never_reseed'],True);self.snapshot(row,True);self.terminal=row['observer_ordinal'];return
  self.need(ordinary_outer,'Material source event interleaves another active world/caller scope')
  if kind.startswith('material-request-ingress-'):
   self.need(self.frame is None,'Request ingress cannot interleave original material consumer body')
   if kind.endswith('before'):
    self.need(self.ingress is None,'Nested ingress');self.ingresses+=1;self.need(row['ingress_id']==self.ingresses<=128,'Consecutive bounded ingress');self.scalar(row['requester_faction_id'],'b');self.v.flag(row['production_enabled'])
    # Invalid/null/nonmember targets may be observed; they are never seeded.
    self.v.exact(row['entity'],('index','version'));self.v.integer(row['entity']['index'],0,2147483647);self.v.integer(row['entity']['version'],0,2147483647)
    self.component(row['queue_before_assertion_or_null'],QUEUE,True);self.buffer(row['requests_before_assertion_or_null'],REQUEST,8,True)
    for k in ('results_presence_assertion','entity_exists_assertion','fabrication_present_assertion'):self.v.flag(row[k])
    e=(row['entity']['index'],row['entity']['version']);self.need(row['fabrication_present_assertion']==(e in self.fabs),'Ingress fabrication domain/presence changed');self.ingress=row
   else:
    self.need(self.ingress is not None and row['ingress_id']==self.ingresses and row['entity']==self.ingress['entity'],'Ingress after identity/order');self.v.flag(row['success_assertion']);self.scalar(row['request_id_assertion'],'i');self.component(row['queue_after_assertion_or_null'],QUEUE,True);self.buffer(row['requests_after_assertion_or_null'],REQUEST,8,True);self.ingress=None
   return
  if kind=='material-update-before':
   self.need(self.frame is None and self.ingress is None,'Nested material update');self.updates+=1;self.need(row['material_update_id']==self.updates<=128,'Consecutive bounded updates');self.v.flag(row['system_handle_same_source_guard'],True);self.scalar(row['elapsed_before_bits'],'f');self.v.integer(row['frame'],0,2147483647);self.need(row['elapsed_role']==(C['events']['material-update-before']['exact_literals']['elapsed_role_first']if self.updates==1 else C['events']['material-update-before']['exact_literals']['elapsed_role_later']),'Private material clock may initialize once only')
   self.frame={'before':row,'stage':'requests','requests':[],'request_open':None,'sim':None,'delta':False,'lookup':[],'lookup_done':False,'ticks':[],'tick_open':None,'ticks_done':False};return
  f=self.frame;self.need(f is not None and row['material_update_id']==self.updates,'Material child outside current update')
  if kind.startswith('material-request-owner-'):
   self.need(f['stage']=='requests','Request processing after scheduler stage');e=self.identity(row['entity']);self.need(e in self.request_domain(),'Unknown/ineligible request owner');self.component(row['fabrication_assertion'],FAB);self.buffer(row['requests_assertion'],REQUEST,8);self.buffer(row['results_assertion'],RESULT,8)
   if kind.endswith('before'):
    self.need(f['request_open']is None and row['request_query_index']==len(f['requests'])and e not in f['requests'],'Dense actual request source iterator');f['request_open']=e;f['requests'].append(e)
   else:self.need(f['request_open']==e and row['request_query_index']==len(f['requests'])-1,'Request after identity/order');f['request_open']=None
  elif kind=='material-requests-complete':
   self.need(f['stage']=='requests'and f['request_open']is None and set(f['requests'])==self.request_domain()and row['request_owner_count']==len(f['requests']),'Incomplete original request query');self.scalar(row['simulation_active'],'b');f['sim']=row['simulation_active'];f['stage']='scheduler'
  elif kind=='material-delta-input':
   self.need(f['stage']=='scheduler'and f['sim']not in(None,0)and not f['delta'],'Delta not actual active-path read');self.scalar(row['delta_time_bits'],'f');f['delta']=True
  elif kind=='material-lookup-row':
   self.need(f['stage']in('scheduler','lookup')and f['delta'],'Lookup before actual active delta');e=self.identity(row['entity']);self.need(e in self.lookup_domain()and e not in f['lookup']and row['lookup_query_index']==len(f['lookup']),'Exact dense source lookup order/domain');self.scalar(row['faction_economy_faction_id'],'b');self.need(row['faction_economy_faction_id']==self.materials[e]['faction_economy_faction_id_or_null'],'Unmodeled economy faction changed');self.component(row['tactical_materials_assertion'],MAT);f['lookup'].append(e);f['stage']='lookup'
  elif kind=='material-lookup-complete':
   self.need(f['stage']in('scheduler','lookup')and f['delta']and not f['lookup_done']and set(f['lookup'])==self.lookup_domain()and row['lookup_owner_count']==len(f['lookup']),'Incomplete actual materials lookup');f['lookup_done']=True;f['stage']='ticks'
  elif kind.startswith('material-tick-'):
   self.need(f['stage']=='ticks','Tick before complete lookup');e=self.identity(row['entity']);self.need(e in self.tick_domain(),'Tick owner outside admitted SAME bank/domain');self.component(row['fabrication_assertion'],FAB);self.component(row['storage_assertion'],STORE)
   target=row['selected_material_entity_assertion_or_null']
   if target is not None:self.need(self.identity(target)in self.materials,'Selected material Entity/version outside initial domain')
   self.component(row['tactical_materials_assertion_or_null'],MAT,True)
   if kind.endswith('before'):
    self.need(f['tick_open']is None and row['tick_query_index']==len(f['ticks'])and e not in f['ticks'],'Dense actual tick iterator');self.scalar(row['health_current_input'],'i')
    for k in ('disabled_input','death_animation_input','building_operational_assertion'):self.v.flag(row[k])
    self.v.integer(row['material_owner_count_assertion'],0,255);f['ticks'].append(e);f['tick_open']=e
   else:
    self.need(f['tick_open']==e and row['tick_query_index']==len(f['ticks'])-1,'Tick post identity/order');self.v.flag(row['tick_applied_assertion']);self.need((row['tick_result_assertion']is not None)==row['tick_applied_assertion'],'Actual ApplyTick result presence');self.component(row['tick_result_assertion'],TICK,True);self.component(row['telemetry_assertion_or_null'],TELE,True);self.component(row['economy_event_queue_assertion_or_null'],EQUEUE,True);self.buffer(row['economy_event_buffer_assertion_or_null'],EVENT,16,True);f['tick_open']=None
  elif kind=='material-ticks-complete':
   self.need(f['stage']=='ticks'and f['tick_open']is None and set(f['ticks'])==self.tick_domain()and row['tick_owner_count']==len(f['ticks']),'Incomplete original material tick query');f['ticks_done']=True;f['stage']='after'
  elif kind=='material-update-after':
   self.need(f['stage']in('scheduler','after')and f['request_open']is None and f['tick_open']is None,'Unclosed original material source iterator');self.v.flag(row['completed_normally'],True);self.v.flag(row['lookup_reached_assertion']);self.need(row['lookup_reached_assertion']==f['lookup_done']==f['ticks_done'],'Material threshold stage closure differs');self.need(f['delta']==(f['sim']!=0),'Active delta missing/inactive delta invented');self.need(row['request_owner_count']==len(f['requests'])and row['tick_owner_count']==len(f['ticks'])and row['frame']==f['before']['frame'],'Frame count/identity differs');self.scalar(row['elapsed_after_bits'],'f');self.frame=None
  else:self.need(False,'Unknown material child kind')
 def finish(self,window,end):
  self.need(self.fabs is not None and self.frame is None and self.ingress is None and self.terminal is not None,'Material source prefix incomplete')
  expected={'material_extension_revision':REVISION,'terminal_material_reserved_bytes':208896,'terminal_material_done':True,'material_updates':self.updates,'material_ingress_events':self.ingresses,'initial_material_fabrication_count':len(self.fabs),'initial_material_owner_count':len(self.materials)}
  for k,value in expected.items():self.need(type(window[k])is type(value)and window[k]==value,'Source material summary differs '+k)
  self.need(end['terminal_material_ordinal']==self.terminal,'Window end material frontier link differs')
  return {'material_updates':self.updates,'material_request_ingress_events':self.ingresses,'initial_fabrication_entities':len(self.fabs),'initial_tactical_material_entities':len(self.materials),'material_terminal_ordinal':self.terminal,'material_numeric_execution':False}

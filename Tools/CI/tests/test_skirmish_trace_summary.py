import json,os,subprocess,sys,tempfile,unittest
from pathlib import Path
ROOT=os.path.dirname(os.path.dirname(__file__));CLI=os.path.join(ROOT,'skirmish_trace_summary.py')
def write(p,x):
 with open(p,'w') as f:
  for r in x:f.write(r if isinstance(r,str) else json.dumps(r));f.write('\n')
def ar(t,w='w',exe=True,a='Support'):return {'time':t,'executed':exe,'action':a,'target_id':9,'receipt':{'world_id':w,'accepted':True,'source_api':'A'}}
def va(t,x=0,g=4,req=False):
 c={'Game.Components.UnitGrid':{'Cell':{'x':0,'y':0}},'Unity.Transforms.LocalTransform':{'Position':{'x':x,'z':0}}}
 if req:c['Game.Components.UnitPathRequest']={'Goal':{'x':g,'y':0}}
 return {'world_id':'w','simulation_seconds':t,'actors':[{'entity':'e','path_state':None if req else {'range_length':2,'next_cell':{'x':0,'y':0},'final_cell':{'x':g,'y':0}},'components':c}]}
def reservation_row(receipts,owners):
 return {'world_id':'w','simulation_seconds':0,'actors':[{'entity':'e'+str(i),'components':{'Game.Components.SkirmishAttemptOwnedComponent':x,'Game.Components.UnitGrid':{'Cell':{'x':0,'y':0}},'Unity.Transforms.LocalTransform':{'Position':{'x':0,'z':0}}}} for i,x in enumerate(owners)],'reservations':receipts,'session_outcome_facts':{'Game.Components.SkirmishExpandedSessionComponent':{'SessionId':'s'}}}
def receipt(rid=7,faction=2,phase=3,member=4,delivered=4,remaining=4):return {'ReservationId':rid,'FactionId':faction,'Phase':phase,'MemberCount':member,'DeliveredMembers':delivered,'RemainingMembers':remaining}
def owner(rid=7,faction=2,session='s',released=0,structure=0):return {'ReservationId':rid,'FactionId':faction,'SessionId':session,'CapacityReleased':released,'IsStructure':structure}
class T(unittest.TestCase):
 def go(self,a,v,extra=()):
  with tempfile.TemporaryDirectory() as d:
   p={k:os.path.join(d,k) for k in 'avrq o'.replace(' ','')};write(p['a'],a);write(p['v'],v)
   for k,x in [('r',{'status':'S'}),('q',{'input_and_runtime_pins':{'C:\\repo\\Assets\\Game\\Scripts\\Skirmish\\Contracts\\SkirmishProductionContracts.cs':'5f8a981caa9873d573697ad035196a33541af39afa3c04f7af01d2ad5e88f73d'}})]:
    with open(p[k],'w') as f:json.dump(x,f)
   z=subprocess.run([sys.executable,CLI,'--aria',p['a'],'--validator',p['v'],'--result',p['r'],'--receipt',p['q'],'--out',p['o'],*extra]);
   with open(p['o']) as f:return z.returncode,json.load(f)
 def test_consecutive_total_window_and_wait(self):
  rc,o=self.go([ar(0),ar(1,exe=False),ar(2),ar(4)],[va(0)])
  self.assertEqual(0,rc);self.assertEqual(1,len(o['warnings']['repeated_consecutive_executed_actions']))
  rc,o=self.go([ar(0),ar(4),ar(8)],[va(0)])
  self.assertFalse(o['warnings']['repeated_consecutive_executed_actions'])
 def test_path_final_and_request_stationary(self):
  rc,o=self.go([ar(0)],[va(0),va(6),va(12)])
  self.assertEqual(1,len(o['warnings']['stationary_with_pending_movement']))
  rc,o=self.go([ar(0)],[va(0,req=True),va(6,req=True),va(12,req=True)])
  self.assertEqual(1,len(o['warnings']['stationary_with_pending_movement']))
 def test_movement_gap_idle_and_nested_fail(self):
  rc,o=self.go([ar(0)],[va(0),va(6,x=1),{'world_id':'w','simulation_seconds':12,'actors':[]},va(18),va(24,g=0)])
  self.assertFalse(o['warnings']['stationary_with_pending_movement'])
  rc,o=self.go([ar(0)],[{'world_id':'w','simulation_seconds':0,'actors':[{'entity':'e','components':[]}]}])
  self.assertEqual(2,rc);self.assertTrue(o['input_errors'])
 def test_isolated_nonfinite_duplicate_world_conflict_missing(self):
  rc,o=self.go(['{"time":NaN}'],[va(0)]);self.assertEqual(2,rc)
  rc,o=self.go(['{"time":1,"time":1}'],[va(0)]);self.assertEqual(2,rc)
  x=ar(0);x['world_id']='x';rc,o=self.go([x],[va(0)]);self.assertEqual(2,rc)
  with tempfile.TemporaryDirectory() as d:
   p=os.path.join(d,'out');z=subprocess.run([sys.executable,CLI,'--aria',os.path.join(d,'none'),'--validator',os.path.join(d,'no2'),'--result',os.path.join(d,'no3'),'--out',p]);self.assertEqual(2,z.returncode);self.assertTrue(os.path.exists(p))
 def test_sliding_repeat_window_keeps_recent_attempts(self):
  rc,o=self.go([ar(t) for t in (0,4,6,7)],[va(0)])
  self.assertEqual(0,rc);w=o['warnings']['repeated_consecutive_executed_actions']
  self.assertEqual(1,len(w));self.assertEqual(4,w[0]['start_seconds']);self.assertEqual(7,w[0]['end_seconds'])
 def test_exponent_overflow_and_huge_clock_fail(self):
  for raw in ('{"time":1e999,"world_id":"w","executed":false}', '{"time":'+str(10**400)+',"world_id":"w","executed":false}'):
   rc,o=self.go([raw],[va(0)]);self.assertEqual(2,rc);self.assertTrue(o['input_errors'])
 def test_nested_field_types_fail_without_traceback(self):
  cases=[]
  for field,value in [('Game.Components.UnitGrid',[]),('Unity.Transforms.LocalTransform',[]),('Game.Components.UnitPathRequest',[])]:
   row=va(0,req=True);row['actors'][0]['components'][field]=value;cases.append(row)
  row=va(0,req=True);row['actors'][0]['components']['Unity.Transforms.LocalTransform']['Position']=[];cases.append(row)
  for row in cases:
   rc,o=self.go([ar(0)],[row]);self.assertEqual(2,rc);self.assertTrue(o['input_errors'])
 def test_duplicate_actor_and_missing_world_are_errors(self):
  row=va(0);row['actors'].append(row['actors'][0])
  rc,o=self.go([ar(0)],[row]);self.assertEqual(2,rc)
  row=ar(0);row.pop('receipt');rc,o=self.go([row],[va(0)]);self.assertEqual(2,rc)
 def test_counter_and_warning_caps_are_explicit(self):
  rows=[]
  for i in range(1025):
   row=ar(i,a='Other');row['receipt']['source_api']='api'+str(i);rows.append(row)
  rc,o=self.go(rows,[va(0)]);self.assertEqual(2,rc);self.assertTrue(any('key cap exceeded' in x for x in o['input_errors']))
  rows=[]
  for i in range(51):
   rows.extend(ar(i*4+j) for j in range(3));rows.append(ar(i*4+3,a='Other'))
  rc,o=self.go(rows,[va(0)]);self.assertEqual(2,rc);self.assertEqual(50,len(o['warnings']['repeated_consecutive_executed_actions']))
 def test_output_cannot_overwrite_evidence(self):
  with tempfile.TemporaryDirectory() as d:
   p=os.path.join(d,'evidence');data=b'preserve exact bytes';Path(p).write_bytes(data)
   z=subprocess.run([sys.executable,CLI,'--aria',p,'--validator',p,'--result',p,'--out',p],capture_output=True)
   self.assertEqual(2,z.returncode);self.assertEqual(data,Path(p).read_bytes())
 def test_reservation_upper_bound_cases(self):
  row=reservation_row([receipt()],[owner() for _ in range(4)])
  rc,o=self.go([ar(0)],[row]);self.assertEqual(0,rc);self.assertEqual('Passed',o['checks']['reservation_owner_upper_bounds']['status'])
  rc,o=self.go([ar(0)],[reservation_row([receipt()],[owner() for _ in range(5)])]);self.assertEqual(1,rc)
  rc,o=self.go([ar(0)],[reservation_row([receipt()],[owner(released=1),owner(structure=1),owner()])]);self.assertEqual(0,rc)
  for bad in (owner(faction=1),owner(session='other')):
   rc,o=self.go([ar(0)],[reservation_row([receipt()],[bad])]);self.assertEqual(1,rc)
  rc,o=self.go([ar(0)],[reservation_row([receipt(),receipt()],[owner()])]);self.assertEqual(1,rc)
  rc,o=self.go([ar(0)],[reservation_row([receipt(faction=2)],[owner(faction=2) for _ in range(4)])]);self.assertEqual(0,rc)
  rc,o=self.go([ar(0)],[reservation_row([receipt(phase=4)],[owner()])]);self.assertEqual('Passed',o['checks']['reservation_owner_upper_bounds']['status'])
  rc,o=self.go([ar(0)],[va(0)]);self.assertEqual('Unsupported',o['checks']['reservation_owner_upper_bounds']['status'])
 def test_reservation_schema_unsupported_and_malformed_boundaries(self):
  row=reservation_row([receipt(phase=9)],[owner()]);rc,o=self.go([ar(0)],[row])
  self.assertEqual(0,rc);self.assertEqual('Unsupported',o['checks']['reservation_owner_upper_bounds']['status'])
  row=reservation_row([receipt()],[owner()]);row['session_outcome_facts']['Game.Components.SkirmishExpandedSessionComponent']=[];rc,o=self.go([ar(0)],[row])
  self.assertEqual(2,rc);self.assertEqual('Unsupported',o['checks']['reservation_owner_upper_bounds']['status'])
  row=reservation_row([receipt()],[owner()]);row['reservations'][0]['DeliveredMembers']='four';rc,o=self.go([ar(0)],[row])
  self.assertEqual(2,rc);self.assertEqual('Unsupported',o['checks']['reservation_owner_upper_bounds']['status'])
  row=reservation_row([receipt() for _ in range(10001)],[owner()]);rc,o=self.go([ar(0)],[row])
  self.assertEqual(2,rc);self.assertEqual('Unsupported',o['checks']['reservation_owner_upper_bounds']['status'])
 def test_owned_component_schema_never_silently_becomes_opening(self):
  for value in ([],{'ReservationId':'7'},{'FactionId':2,'SessionId':'s','CapacityReleased':0,'IsStructure':0},{'ReservationId':-1,'FactionId':2,'SessionId':'s','CapacityReleased':0,'IsStructure':0}):
   row=reservation_row([receipt()],[owner()]);row['actors'][0]['components']['Game.Components.SkirmishAttemptOwnedComponent']=value;rc,o=self.go([ar(0)],[row])
   self.assertEqual(2,rc);self.assertEqual('Unsupported',o['checks']['reservation_owner_upper_bounds']['status'])
  row=reservation_row([receipt()],[owner()]);row['actors'][0]['components'].pop('Game.Components.SkirmishAttemptOwnedComponent');rc,o=self.go([ar(0)],[row])
  self.assertEqual(0,rc);self.assertEqual('Unsupported',o['checks']['reservation_owner_upper_bounds']['status'])
 def test_phase_pin_receipt_gate(self):
  row=reservation_row([receipt()],[owner()])
  for pin in ({}, {'input_and_runtime_pins':{'Assets/Game/Scripts/Skirmish/Contracts/SkirmishProductionContracts.cs':'bad'}}, {'source_and_reference_pins':{'Assets/Game/Scripts/Skirmish/Contracts/SkirmishProductionContracts.cs':'5f8a981caa9873d573697ad035196a33541af39afa3c04f7af01d2ad5e88f73d'},'input_and_runtime_pins':{'x/Assets/Game/Scripts/Skirmish/Contracts/SkirmishProductionContracts.cs':'5f8a981caa9873d573697ad035196a33541af39afa3c04f7af01d2ad5e88f73d'}}, {'input_and_runtime_pins':{'notAssets/Game/Scripts/Skirmish/Contracts/SkirmishProductionContracts.cs':'5f8a981caa9873d573697ad035196a33541af39afa3c04f7af01d2ad5e88f73d'}}):
   with tempfile.TemporaryDirectory() as d:
    a,v,r,q,o=[os.path.join(d,x) for x in ('a','v','r','q','o')];write(a,[ar(0)]);write(v,[row])
    with open(r,'w') as f:json.dump({'status':'S'},f)
    with open(q,'w') as f:json.dump(pin,f)
    rc=subprocess.run([sys.executable,CLI,'--aria',a,'--validator',v,'--result',r,'--receipt',q,'--out',o]).returncode
    with open(o) as f:out=json.load(f)
    self.assertEqual(0,rc);self.assertEqual('Unsupported',out['checks']['reservation_owner_upper_bounds']['status'])
if __name__=='__main__':unittest.main()

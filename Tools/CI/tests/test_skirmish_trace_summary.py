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
class T(unittest.TestCase):
 def go(self,a,v,extra=()):
  with tempfile.TemporaryDirectory() as d:
   p={k:os.path.join(d,k) for k in 'avrq o'.replace(' ','')};write(p['a'],a);write(p['v'],v)
   for k,x in [('r',{'status':'S'}),('q',{})]:
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
if __name__=='__main__':unittest.main()

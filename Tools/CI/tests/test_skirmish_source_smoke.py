"""Protocol/stage controls only. No Unity, compiler or simulation world runs."""
import ast,contextlib,hashlib,io,json,math,sys,tempfile,time,types,unittest
from pathlib import Path
from unittest import mock
ROOT=Path(__file__).resolve().parents[1]
from skirmish_source_profiles import require_profile as actual_require_profile

def need(ok,message):
 if not ok:raise ValueError(message)
def duplicates(pairs):
 result={}
 for key,value in pairs:
  if key in result:raise ValueError('duplicate '+key)
  result[key]=value
 return result
def pin(path):
 path=Path(path).resolve(strict=True);raw=path.read_bytes();return {'path':str(path),'sha256':hashlib.sha256(raw).hexdigest(),'bytes':len(raw)}
def write(path,value):
 path=Path(path);path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value))
def verify(pins):
 for path,digest in pins.items():need(pin(path)['sha256']==digest,'Input changed:'+path)

class SmokeControls(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name);self.manifest=self.root/'manifest.json';self.manifest.write_text('{}');self.b=types.SimpleNamespace(need=need,pin=pin,sha=lambda raw:hashlib.sha256(raw).hexdigest(),write=write,verify=verify);self.owned=types.SimpleNamespace(pins=lambda:{},require=lambda name:types.SimpleNamespace(no_duplicates=duplicates));self.module=types.ModuleType('smoke_control_module')
  with mock.patch.dict(sys.modules,{'skirmish_owned_source':self.owned,'skirmish_source_build':self.b}):exec(compile((ROOT/'skirmish_source_smoke.py').read_bytes(),str(ROOT/'skirmish_source_smoke.py'),'exec'),self.module.__dict__)
 def tearDown(self):self.temp.cleanup()
 def run_smoke(self,stages,startup=True,timeout=1):
  self.module.STAGES=stages
  return self.module.smoke(self.root,self.root,self.manifest,self.root/'out','S002',104731,timeout=timeout,startup=startup)
 @staticmethod
 def passed(ctx):return 'Passed','field','actual stage completed',{'scope':'protocolfixture'}
 def test_world_startup_requires_objective_success(self):
  stages={name:deps for name,fn,deps in self.module.STAGES};self.assertIn('objective_rules',stages['startup'])
 def test_source_build_waits_for_cheap_catalog_matrix(self):
  stages={name:deps for name,fn,deps in self.module.STAGES};self.assertIn('catalog_matrix_csv',stages['source_build'])
 def test_pass_is_only_smoke(self):
  r=self.run_smoke([('one',self.passed,())]);self.assertEqual((r['status'],r['exit_code']),('SmokePassed',0));self.assertFalse(r['validation_passed']);self.assertFalse(r['mission_ready']);self.assertFalse(r['full_outcome_observed']);self.assertTrue(r['unimplemented_coverage'])
 def test_fullpreflight_does_not_initialize_startup(self):
  r=self.run_smoke([('startup',self.module._startup,())],startup=False);self.assertEqual(r['status'],'PreflightPassed');self.assertEqual(r['stages'][0]['status'],'DeferredToFullRun')
 def test_failure_skips_only_dependent_stages(self):
  seen=[]
  def fail(c):return 'Failed','layout','bad authored field',None
  def independent(c):seen.append('independent');return self.passed(c)
  r=self.run_smoke([('input',fail,()),('dependent',self.passed,('input',)),('independent',independent,())]);self.assertEqual(r['exit_code'],1);self.assertEqual(r['stages'][1]['status'],'Skipped');self.assertEqual(seen,['independent'])
 def test_native_block_never_green(self):
  r=self.run_smoke([('native',lambda c:('Blocked','worldBinding','original reason',None),()),('startup',self.passed,('native',))]);self.assertEqual((r['status'],r['exit_code']),('Blocked',2))
 def test_unsupported_profile_never_green(self):
  r=self.run_smoke([('profile',lambda c:('Unsupported','mission','unsupported',None),())]);self.assertEqual(r['exit_code'],2)
 def test_extension_stage_is_required(self):
  r=self.run_smoke([('existing',self.passed,()),('future_native_owner',lambda c:('Failed','owner','original owner rejected',None),('existing',))]);self.assertEqual(r['status'],'Failed')
 def test_unknown_status_fails_closed(self):
  r=self.run_smoke([('bad',lambda c:('green','x','x',None),())]);self.assertEqual(r['status'],'Failed');self.assertIn('Unknown smoke stage status',r['stages'][0]['reason'])
 def test_only_actual_fullrun_startup_can_defer(self):
  r=self.run_smoke([('other',lambda c:('DeferredToFullRun','x','x',None),())]);self.assertEqual(r['status'],'Failed')
 def test_deadline_stops_later_stages(self):
  seen=[]
  def slow(c):time.sleep(.02);return self.passed(c)
  r=self.run_smoke([('slow',slow,()),('later',lambda c:(seen.append(1)or self.passed(c)),())],timeout=.001);self.assertFalse(seen);self.assertEqual(r['status'],'Blocked')
 def test_postrun_source_drift_fails(self):
  path=self.root/'source';path.write_text('before');self.owned.pins=lambda:{str(path):pin(path)['sha256']}
  def drift(c):path.write_text('after');return self.passed(c)
  r=self.run_smoke([('read',drift,())]);self.assertEqual(r['status'],'Failed');self.assertEqual(r['stages'][-1]['field'],'input_pins')
 def test_duplicate_json_rejected(self):
  p=self.root/'bad.json';p.write_text('{"a":1,"a":2}');self.assertRaises(ValueError,self.module._json,p)
 def test_trailing_json_rejected(self):
  p=self.root/'bad.json';p.write_text('{} {}');self.assertRaises(ValueError,self.module._json,p)
 def test_nonfinite_json_rejected(self):
  p=self.root/'bad.json';p.write_text('{"a":NaN}');self.assertRaises(ValueError,self.module._json,p)
 def test_changed_descriptor_rejected(self):
  p=self.root/'input';p.write_text('x');self.assertRaises(ValueError,self.module._descriptor,{'path':str(p),'sha256':'0'*64})
 def test_relative_descriptor_rejected(self):self.assertRaises(ValueError,self.module._descriptor,{'path':'relative.json','sha256':'0'*64})
 def test_existing_optional_byte_count_checked(self):
  p=self.root/'input';p.write_text('abc');row=pin(p);self.assertEqual(self.module._descriptor(row)['bytes'],3);row['bytes']=4;self.assertRaises(ValueError,self.module._descriptor,row)
 def test_unknown_descriptor_key_rejected(self):self.assertRaises(ValueError,self.module._descriptor,{'path':'x','sha256':'0'*64,'extra':1})
 def test_owned_process_actual_nonzero(self):
  r=self.module._command([sys.executable,'-c','raise SystemExit(7)'],self.root/'child',1);self.assertEqual(r['process_exit'],7);self.assertFalse(r['timed_out'])
 def test_owned_process_timeout_terminated(self):
  r=self.module._command([sys.executable,'-c','import time;time.sleep(3)'],self.root/'child',.02);self.assertTrue(r['timed_out']);self.assertIsNotNone(r['process_exit']);self.assertNotEqual(r['process_exit'],0)
 def test_invalid_process_timeout_rejected(self):
  for value in [0,-1,float('nan'),float('inf')]:self.assertRaises(ValueError,self.module._command,[],self.root/'unused',value)
 def test_known_startup_failure_propagates(self):
  self.b.run=lambda *a,**k:{'diagnostic_prefix_completed':False,'process_exit':1,'status':'Failed','source_result':{'error':'MissingUniqueRuntimeHelipadProductionSlotOwner'}}
  r=self.module._startup({'startup':True,'mission':'S002','repo':self.root,'build_receipt':'b','bundle':'d','out':self.root,'remaining':lambda:1,'data_repo':self.root});self.assertEqual(r[0],'Failed');self.assertIn('MissingUnique',r[3]['source_result']['error'])
 def test_repro_carries_explicit_inputs(self):
  self.module.STAGES=[('one',self.passed,())];r=self.module.smoke(self.root,self.root,self.manifest,self.root/'out','S002',104731,bundle='actual-bundle.json',build_receipt='actual-build.json',objective_build='objective-build',timeout=1);self.assertIn('actual-bundle.json',r['repro']);self.assertEqual(r['stages'][0]['repro'],r['repro'])

class EntryHookControls(unittest.TestCase):
 def qualified_profile(self,mission,seed):
  # Explicit protocol stub: the real guard qualified this tuple before fake build-module injection.
  if (mission,seed)!=('S002',104731):raise ValueError('Unqualified source execution profile')
  return self.profile_pins
 def setUp(self):
  self.profile_pins=actual_require_profile('S002',104731)
  self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name);self.calls=[]
  def fake_smoke(*args,**kwargs):
   self.calls.append(('smoke',args,kwargs));out=Path(args[3]);out.mkdir(parents=True);result={'status':'PreflightPassed','exit_code':0,'validation_passed':False,'build_receipt':args[8]or'preflight-checked-build'};write(out/'smoke-report.json',result);return result
  def fake_run(*args):
   self.calls.append(('run',args,{}));out=Path(args[3]);out.mkdir();(out/'process.log').write_text('[ActualS002AutonomousStart]');return {'status':'SourceOutcomeObserved','process_exit':0,'timed_out':False,'wall_seconds':1,'validation_passed':True,'full_outcome_observed':True,'diagnostic_prefix_completed':False}
  self.fake_smoke=fake_smoke;self.b=types.SimpleNamespace(run=fake_run,build=lambda *a:self.fail('duplicate build'),write=write,pin=pin);owned=types.SimpleNamespace(require=lambda name:types.SimpleNamespace(require_profile=self.qualified_profile,runtime_backend=lambda mission:'LegacyGroundS002V1',smoke=self.fake_smoke));tree=ast.parse((ROOT/'run_skirmish_source_simulation.py').read_text());main=next(x for x in tree.body if isinstance(x,ast.FunctionDef)and x.name=='main');module=ast.Module(body=[main],type_ignores=[]);self.ns={'argparse':__import__('argparse'),'json':json,'sys':sys,'Path':Path,'time':time,'owned':owned,'UnsupportedSourceShape':ValueError,'__doc__':'control','__file__':str(ROOT/'run_skirmish_source_simulation.py')};exec(compile(module,'entry-main','exec'),self.ns)
 def tearDown(self):self.temp.cleanup()
 def invoke(self,extra,checked=True):
  args=['cli','run','--mission','S002','--repo',str(self.root),'--package-root',str(self.root),'--output',str(self.root/'run'),'--build-receipt','b','--data-bundle','d']+extra
  if not checked:args.remove('--build-receipt');args.remove('b')
  with mock.patch.dict(sys.modules,{'skirmish_source_build':self.b}),mock.patch.object(sys,'argv',args),contextlib.redirect_stdout(io.StringIO()):return self.ns['main']()
 def test_fullrun_preflight_defers_one_startup(self):
  self.assertEqual(self.invoke([]),0);self.assertEqual([x[0]for x in self.calls],['smoke','run']);self.assertFalse(self.calls[0][2]['startup']);self.assertIsNone(self.calls[1][1][4])
 def test_unbuilt_fullrun_reuses_preflight_created_build(self):
  self.assertEqual(self.invoke([],checked=False),0);self.assertIsNone(self.calls[0][1][8]);self.assertEqual(self.calls[1][1][1],'preflight-checked-build')
 def test_unbuilt_fullrun_bad_csv_never_builds_or_runs(self):
  self.ns['owned']=types.SimpleNamespace(require=lambda name:types.SimpleNamespace(require_profile=self.qualified_profile,runtime_backend=lambda mission:'LegacyGroundS002V1',smoke=lambda *a,**k:{'status':'Failed','exit_code':1,'stages':[{'id':'catalog_matrix_csv','status':'Failed'},{'id':'source_build','status':'Skipped'}]}));self.assertEqual(self.invoke([],checked=False),1);self.assertFalse(self.calls)
 def test_explicit_diagnostic_does_not_recurse(self):
  self.assertEqual(self.invoke(['--max-ticks','5']),0);self.assertEqual([x[0]for x in self.calls],['run']);self.assertEqual(self.calls[0][1][4],5)
 def test_failed_preflight_blocks_fullworker(self):
  self.ns['owned']=types.SimpleNamespace(require=lambda name:types.SimpleNamespace(require_profile=self.qualified_profile,runtime_backend=lambda mission:'LegacyGroundS002V1',smoke=lambda *a,**k:{'status':'Blocked','exit_code':2}));self.assertEqual(self.invoke([]),2);self.assertFalse(self.calls)

if __name__=='__main__':unittest.main()

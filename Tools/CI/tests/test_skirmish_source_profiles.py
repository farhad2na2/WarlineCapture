import subprocess,sys,tempfile,shutil,json,unittest
from pathlib import Path
base=Path(__file__).resolve().parents[1]
checks=[]
EXPECTED={'profile_unowned_collision':'Unowned module collision: skirmish_source_profiles','profile_executed_source_drift':'Owned Python source changed since execution:','profile_json_drift_before_generation':'Admission JSON changed since owned module execution','profile_json_drift_during_generation':'Admission JSON changed during guard generation','profile_json_pin_conflict':'Conflicting source/runtime epoch:'}
boot="""import runpy,sys,types
from pathlib import Path
root=Path(sys.argv[1]);sys.dont_write_bytecode=True
"""
load="namespace=runpy.run_path(str(root/'run_skirmish_source_simulation.py'),run_name='fixture');owned=namespace['owned'];profiles=owned.require('skirmish_source_profiles')\n".replace('\\n','\n')
cases={
'profile_unowned_collision':"sys.modules['skirmish_source_profiles']=types.ModuleType('skirmish_source_profiles')\n"+load,
'profile_executed_source_drift':load+"f=root/'skirmish_source_profiles.py';f.write_text(f.read_text()+'# drift\\n');owned.pins()\n",
'profile_json_drift_before_generation':load+"profiles.require_profile('S002',1);profiles.PATH.write_text(profiles.PATH.read_text()+' ');profiles.generate_guard()\n",
'profile_json_drift_during_generation':load+"original=profiles.json.dumps\ndef changed(*a,**kw):\n profiles.PATH.write_text(profiles.PATH.read_text()+' ');return original(*a,**kw)\nprofiles.json.dumps=changed;profiles.generate_guard()\n",
'profile_json_pin_conflict':load+"pins=profiles.require_profile('S002',1);other=dict(pins);other[str(profiles.PATH)]='0'*64;owned.require('skirmish_source_build').merge_pins(pins,other)\n"}
class SourceProfileOwnedControls(unittest.TestCase):
 def test_exact_owned_profile_epoch_and_json_binding(self):
  for name,code in cases.items():
   with tempfile.TemporaryDirectory(prefix='source-profile-control-')as temporary:
    dest=Path(temporary)/'ci';shutil.copytree(base,dest,ignore=shutil.ignore_patterns('__pycache__'));r=subprocess.run([sys.executable,'-B','-c',boot+code,str(dest)],capture_output=True,text=True)
    assert r.returncode!=0,(name,r.stdout);assert EXPECTED[name] in r.stderr,(name,r.stderr);checks.append({'id':name,'rejected':True,'reason':r.stderr.splitlines()[-1]})
  self.assertEqual(len(checks),5)
 def test_qualified_selection_and_strict_schema(self):
  with tempfile.TemporaryDirectory(prefix='source-profile-selection-')as temporary:
   dest=Path(temporary)/'ci';shutil.copytree(base,dest,ignore=shutil.ignore_patterns('__pycache__'))
   valid=boot+load+"profiles.require_profile('S002',1);profiles.require_profile('S002',104731)\n"
   self.assertEqual(subprocess.run([sys.executable,'-B','-c',valid,str(dest)],capture_output=True).returncode,0)
   for expression in ["profiles.require_profile('S002',2)","profiles.require_profile('S026',104755)","profiles.require_profile('S002',1,1,1)","profiles.require_profile('S002',1,2,2)","profiles.require_profile('S002',True)"]:
    r=subprocess.run([sys.executable,'-B','-c',boot+load+expression,str(dest)],capture_output=True,text=True);self.assertNotEqual(r.returncode,0);self.assertIn('Unqualified source execution profile',r.stderr)
   config=dest/'SkirmishSourceSimulation/admitted_profiles.json';raw=config.read_text()
   for mutate,reason in [(lambda d:d.update(extra=1),'Unsupported admission schema/runtime'),(lambda d:d.update(schema_version=2),'Unsupported admission schema/runtime'),(lambda d:d['profiles'][0].update(extra=1),'Exact profile keys required'),(lambda d:d['profiles'][0]['seeds'].append(2),'Unqualified seed')]:
    d=json.loads(raw);mutate(d);config.write_text(json.dumps(d));r=subprocess.run([sys.executable,'-B','-c',boot+load+"profiles.require_profile('S002',1)",str(dest)],capture_output=True,text=True);self.assertNotEqual(r.returncode,0);self.assertIn(reason,r.stderr)


if __name__=="__main__":unittest.main()

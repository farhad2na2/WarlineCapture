import ast,os,subprocess,sys,tempfile,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
CLI=ROOT/"Tools/CI/run_skirmish_source_simulation.py"
class S073PreparingGuards(unittest.TestCase):
 def invoke(self,output,*flags):
  return subprocess.run([sys.executable,"-B",str(CLI),"run","--repo",str(ROOT),"--mission","S073","--seed","104729","--data-bundle","/nonexistent-s073-bundle.json","--build-receipt","/nonexistent-s073-build.json","--output",str(output),*flags],cwd=ROOT,capture_output=True,text=True)
 def test_prepare_only_never_allocates(self):
  with tempfile.TemporaryDirectory() as temp:
   output=Path(temp)/"must-not-exist";p=self.invoke(output,"--s073-current-source-diagnostic","--prepare-only")
   self.assertNotEqual(p.returncode,0);self.assertIn("S073 prepare-only is unsupported; no build or worker will be launched",p.stderr);self.assertFalse(output.exists())
 def test_historical_requires_current_source(self):
  with tempfile.TemporaryDirectory() as temp:
   p=self.invoke(Path(temp)/"out","--s073-historical-input-diagnostic","--max-ticks","1")
   self.assertNotEqual(p.returncode,0);self.assertIn("requires current-source S073",p.stderr)
 def test_historical_preparing_requires_bound(self):
  with tempfile.TemporaryDirectory() as temp:
   p=self.invoke(Path(temp)/"out","--s073-current-source-diagnostic","--s073-historical-input-diagnostic")
   self.assertNotEqual(p.returncode,0);self.assertIn("explicit bounded preparing diagnostic",p.stderr)
 def test_no_player_policy_requires_bounded_host_and_no_recruit(self):
  with tempfile.TemporaryDirectory() as temp:
   output=Path(temp)/"out";env=os.environ.copy();env["WARLINE_S073_NO_PLAYER_POLICY_DIAGNOSTIC"]="1";env["WARLINE_S073_SYNTHETIC_PLAYER_RECRUIT_DIAGNOSTIC"]="1"
   command=[sys.executable,"-B",str(CLI),"run","--repo",str(ROOT),"--mission","S073","--data-bundle","/none","--build-receipt","/none","--output",str(output),"--s073-source-host-diagnostic-admission","--max-ticks","1"]
   p=subprocess.run(command,cwd=ROOT,capture_output=True,text=True,env=env)
   self.assertNotEqual(p.returncode,0);self.assertIn("no explicit recruit command",p.stderr);self.assertFalse(output.exists())
 def test_bounded_preparing_reaches_input_validation(self):
  with tempfile.TemporaryDirectory() as temp:
   output=Path(temp)/"out";p=self.invoke(output,"--s073-current-source-diagnostic","--s073-historical-input-diagnostic","--max-ticks","1")
   self.assertNotEqual(p.returncode,0);self.assertNotIn("requires current-source S073",p.stderr);self.assertFalse((output/"process.log").exists())
class S073PreparingPlacementScope(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  tree=ast.parse((ROOT/"Tools/CI/skirmish_source_build.py").read_text())
  functions=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=="s073_preparing_placement_scope_matches"]
  if len(functions)!=1: raise AssertionError("Expected one scope classifier")
  namespace={};exec(compile(ast.Module(body=functions,type_ignores=[]),"<source-classifier>","exec"),namespace)
  cls.matches=staticmethod(namespace["s073_preparing_placement_scope_matches"])
 def test_historical_only_with_explicit_unverified_scope(self):
  d={"historical_input_diagnostic":True,"historical_input_current_source_equivalence":False,"placement_input_scope":"HistoricalCapturedCampaignSource_CurrentSourceEquivalenceUnverified"}
  self.assertTrue(self.matches(d,True));self.assertFalse(self.matches(d,False))
  for key in ("historical_input_diagnostic","historical_input_current_source_equivalence","placement_input_scope"):
   altered=d.copy();altered.pop(key);self.assertFalse(self.matches(altered,True))
  altered=d.copy();altered["historical_input_current_source_equivalence"]=True;self.assertFalse(self.matches(altered,True))
 def test_current_scope_remains_distinct(self):
  d={"historical_input_diagnostic":False,"placement_input_scope":"CapturedCampaignSource_NotLoadedSceneOrSourceFit"}
  self.assertTrue(self.matches(d,False));self.assertFalse(self.matches(d,True))
  d["historical_input_diagnostic"]=True;self.assertFalse(self.matches(d,False))
if __name__=="__main__":unittest.main()

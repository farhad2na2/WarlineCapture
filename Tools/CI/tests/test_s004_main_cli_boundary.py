import hashlib,importlib.util,json,subprocess,sys,tempfile,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];CLI=ROOT/"Tools/CI/run_skirmish_source_simulation.py"
class Controls(unittest.TestCase):
 def invoke(self,mission="S004",seed="104733",extra=()):
  with tempfile.TemporaryDirectory() as d:
   out=Path(d)/"out";cmd=[sys.executable,"-B",str(CLI),"s004-diagnostic","--mission",mission,"--seed",seed,"--build-receipt","unused","--data-bundle","unused","--data-bundle-sha256","0"*64,"--output",str(out),*extra];p=subprocess.run(cmd,cwd=ROOT,capture_output=True,text=True,timeout=30);return p,out.exists()
 def test_wrong_mission_rejected(self):
  p,exists=self.invoke(mission="S003");self.assertEqual(p.returncode,2);self.assertFalse(exists)
 def test_wrong_seed_rejected(self):
  p,exists=self.invoke(seed="104731");self.assertEqual(p.returncode,2);self.assertFalse(exists)
 def test_other_mode_flag_rejected(self):
  p,exists=self.invoke(extra=("--max-ticks","5"));self.assertEqual(p.returncode,2);self.assertFalse(exists)
 def test_unknown_bundle_fails_before_worker(self):
  with tempfile.TemporaryDirectory() as d:
   path=Path(d)/"unknown.json";path.write_text('{"unqualified":true}\n');out=Path(d)/"out";digest=hashlib.sha256(path.read_bytes()).hexdigest()
   cmd=[sys.executable,"-B",str(CLI),"s004-diagnostic","--mission","S004","--seed","104733","--build-receipt","unused","--data-bundle",str(path),"--data-bundle-sha256",digest,"--output",str(out)]
   p=subprocess.run(cmd,cwd=ROOT,capture_output=True,text=True,timeout=30);self.assertEqual(p.returncode,1)
   receipt=json.loads((out/"failure-receipt.json").read_text());self.assertFalse(receipt["worker_started"]);self.assertFalse(receipt["mission_ready"])
 def test_bundle_digest_mismatch_fails_before_worker(self):
  with tempfile.TemporaryDirectory() as d:
   path=Path(d)/"bad.json";path.write_text('{}\n');out=Path(d)/"out"
   cmd=[sys.executable,"-B",str(CLI),"s004-diagnostic","--mission","S004","--seed","104733","--build-receipt","unused","--data-bundle",str(path),"--data-bundle-sha256","5ebddd9cfaeeaa25e1f2b6a7f69e2bb8d04412f2b0254d2e5c5a4bac90aa415c","--output",str(out)]
   p=subprocess.run(cmd,cwd=ROOT,capture_output=True,text=True,timeout=30);self.assertEqual(p.returncode,1)
   receipt=json.loads((out/"failure-receipt.json").read_text());self.assertFalse(receipt["worker_started"]);self.assertFalse(receipt["validation_passed"])

class ResourceDiagnosticClassification(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  spec=importlib.util.spec_from_file_location("s004_diagnostic_wrapper",ROOT/"Tools/CI/run_skirmish_s004_source_diagnostic.py");cls.module=importlib.util.module_from_spec(spec);spec.loader.exec_module(cls.module)
 def test_startup_only(self):
  self.assertIsNone(self.module.classify_resource_diagnostic({},{}))
 def test_cadence_requires_observed_scoped_result(self):
  env={"WARLINE_S004_RESOURCE_CADENCE_DIAGNOSTIC":"1"};row={"scope":"DirectSourceCadenceAndOwner_OutsideInactiveScheduler","native_cadence_qualified":False}
  self.assertEqual(self.module.classify_resource_diagnostic({"resource_cadence_diagnostic":row},env),"ResourceCadenceAndOwner")
  with self.assertRaises(ValueError):self.module.classify_resource_diagnostic({},env)
  with self.assertRaises(ValueError):self.module.classify_resource_diagnostic({"resource_cadence_diagnostic":row},{})
  with self.assertRaises(ValueError):self.module.classify_resource_diagnostic({"resource_cadence_diagnostic":dict(row,native_cadence_qualified=True)},env)
 def test_direct_and_conflict(self):
  env={"WARLINE_S004_DIRECT_RESOURCE_OWNER_DIAGNOSTIC":"1"};row={"scope":"DirectSourceOwnerDiagnostic_OutsideInactiveScheduler","native_cadence_qualified":False}
  self.assertEqual(self.module.classify_resource_diagnostic({"direct_resource_owner_diagnostic":row},env),"DirectResourceOwner")
  with self.assertRaises(ValueError):self.module.classify_resource_diagnostic({"direct_resource_owner_diagnostic":row},{**env,"WARLINE_S004_RESOURCE_CADENCE_DIAGNOSTIC":"1"})
if __name__=="__main__":unittest.main()

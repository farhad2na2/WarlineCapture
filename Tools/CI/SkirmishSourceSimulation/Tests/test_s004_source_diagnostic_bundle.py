import importlib.util,hashlib,tempfile,unittest
from pathlib import Path
R=Path(__file__).resolve().parents[4]; P=R/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/build_s004_source_diagnostic_bundle.py"
spec=importlib.util.spec_from_file_location("s004_bundle",P);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
class Controls(unittest.TestCase):
 def test_regular_and_changed_input(self):
  with tempfile.TemporaryDirectory() as d:
   p=Path(d)/"input";p.write_bytes(b"original");digest=hashlib.sha256(b"original").hexdigest();self.assertEqual(m.descriptor(p,digest)["bytes"],8)
   p.write_bytes(b"altered!")
   with self.assertRaises(ValueError):m.descriptor(p,digest)
 def test_relative_and_symlink_rejected(self):
  with tempfile.TemporaryDirectory() as d:
   p=Path(d)/"input";p.write_bytes(b"a");q=Path(d)/"link";q.symlink_to(p);digest=hashlib.sha256(b"a").hexdigest()
   with self.assertRaises(ValueError):m.descriptor(q,digest)
   with self.assertRaises(ValueError):m.descriptor(Path("relative"),digest)
 def test_fixed_bundle_scope(self):
  import json
  data=json.loads((R/"T Logs/SkirmishProduction/s004-source-diagnostic-bundle-20261104-T1/bundle.json").read_text())
  self.assertEqual(data["mission_id"],"S004");self.assertEqual(set(data["inputs"]),set(m.PATHS));self.assertTrue(all(value is False for value in data["qualification"].values()))
if __name__=="__main__":unittest.main()

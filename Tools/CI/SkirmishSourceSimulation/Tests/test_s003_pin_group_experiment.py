import hashlib,importlib.util,tempfile,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];source=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s003_pin_group_experiment.py";spec=importlib.util.spec_from_file_location("pinexp",source);mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
class PinGroupControls(unittest.TestCase):
 def test_shared_pin_read_once_per_phase_and_rechecked_later(self):
  with tempfile.TemporaryDirectory() as tmp:
   root=Path(tmp);p=root/"x";p.write_bytes(b"actual");digest=hashlib.sha256(b"actual").hexdigest();groups={"a":{"x":digest},"b":{str(p):digest}};calls=[]
   def read(x):calls.append(x);return x.read_bytes()
   got=mod.verify_groups(root,groups,read);self.assertEqual(len(calls),1);self.assertEqual(got["a"],got["b"])
   p.write_bytes(b"altered")
   with self.assertRaisesRegex(ValueError,"missing or changed"):mod.verify_groups(root,groups,read)
   self.assertEqual(len(calls),2)
 def test_conflicting_expected_digest_is_rejected(self):
  with tempfile.TemporaryDirectory() as tmp:
   root=Path(tmp);(root/"x").write_bytes(b"x");digest=hashlib.sha256(b"x").hexdigest()
   with self.assertRaisesRegex(ValueError,"Conflicting"):mod.verify_groups(root,{"a":{"x":digest},"b":{"x":"0"*64}})
 def test_missing_pin_is_rejected(self):
  with tempfile.TemporaryDirectory() as tmp:
   with self.assertRaisesRegex(ValueError,"missing or changed"):mod.verify_groups(Path(tmp),{"a":{"missing":"0"*64}})
 def test_midphase_mutation_can_escape_deduplicated_pass(self):
  with tempfile.TemporaryDirectory() as tmp:
   root=Path(tmp);path=root/"x";path.write_bytes(b"before");digest=hashlib.sha256(b"before").hexdigest();groups={"a":{"x":digest},"b":{"x":digest}}
   def read_then_mutate(p):
    data=p.read_bytes();p.write_bytes(b"after");return data
   result=mod.verify_groups(root,groups,read_then_mutate)
   self.assertEqual(len(result),2)
   self.assertNotEqual(hashlib.sha256(path.read_bytes()).hexdigest(),digest)
   with self.assertRaisesRegex(ValueError,"missing or changed"):mod.verify_groups(root,groups)
   # The existing verifier reads the same pin in each group and catches this drift.
   import sys
   sys.path.insert(0,str(source.parent))
   try:
    from audit_s003_refreshed_evidence_chain import verify_pins
    path.write_bytes(b"before");verify_pins(root,groups["a"]);path.write_bytes(b"after")
    with self.assertRaisesRegex(ValueError,"missing or changed"):verify_pins(root,groups["b"])
   finally:sys.path.pop(0)
if __name__=="__main__":unittest.main()

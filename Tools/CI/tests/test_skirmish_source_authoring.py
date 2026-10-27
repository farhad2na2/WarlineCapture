"""Finite input checks. Actual native-author protocol cases are recorded separately."""
import json,math,sys,tempfile,unittest
from pathlib import Path
CI=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(CI))
import skirmish_source_authoring as a
class AuthorInputTests(unittest.TestCase):
 def setUp(self):self.tmp=tempfile.TemporaryDirectory();self.root=Path(self.tmp.name)
 def tearDown(self):self.tmp.cleanup()
 def read(self,text):p=self.root/'input.json';p.write_text(text);return a.read_author_json(p)
 def test_single_document_and_same_byte_pin(self):
  value,pin=self.read('{"one":1}\n');self.assertEqual(value,{'one':1});self.assertEqual(pin['sha256'],a.b.sha(b'{"one":1}\n'));self.assertEqual(pin['bytes'],10)
 def test_duplicate_nested_field_rejects(self):
  with self.assertRaisesRegex(ValueError,'Duplicate JSON field'):self.read('{"nested":{"x":1,"x":2}}')
 def test_trailing_document_rejects(self):
  with self.assertRaises(json.JSONDecodeError):self.read('{} {}')
 def test_trailing_garbage_rejects(self):
  with self.assertRaises(json.JSONDecodeError):self.read('{} garbage')
 def test_nonfinite_rejects(self):
  for value in ['NaN','Infinity','-Infinity']:
   with self.subTest(value=value),self.assertRaisesRegex(ValueError,'Nonfinite'):self.read('{"x":'+value+'}')
 def test_size_bound_rejects(self):
  p=self.root/'large.json';p.write_bytes(b' '*(16*1024*1024+1))
  with self.assertRaisesRegex(Exception,'Bounded authoring JSON'):a.read_author_json(p)
 def call(self,mission='S026',seed=104755,timeout=30):
  # Dispatch reads a valid draft before the unchanged legacy identity/timeout guards.
  draft=self.root/'unused.json';draft.write_text('{}')
  return a.author(self.root,self.root,None,self.root/'output',mission,seed,draft,timeout=timeout)
 def test_wrong_entry_identity_precedes_build_or_output(self):
  for mission,seed in [('S002',104755),('S026',104731)]:
   with self.subTest(mission=mission,seed=seed),self.assertRaisesRegex(Exception,'Current data authoring scope'):self.call(mission,seed)
  self.assertFalse((self.root/'output').exists())
 def test_invalid_timeout_precedes_build_or_output(self):
  for value in [0,-1,float('inf'),float('nan')]:
   with self.subTest(timeout=value),self.assertRaisesRegex(Exception,'Finite positive'):self.call(timeout=value)
  self.assertFalse((self.root/'output').exists())
if __name__=='__main__':unittest.main()

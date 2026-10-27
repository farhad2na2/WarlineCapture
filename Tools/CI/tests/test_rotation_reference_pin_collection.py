"""Rotation input-pin protocol controls; native function semantics stay with original C# owner.

Pass an existing CLI directory and an external qualified schema-2 reference-set file.
No capture, component facts, World, copied arrays, or runtime acceptance is fabricated.
"""
import argparse,copy,hashlib,importlib.util,json,sys,tempfile,unittest
from pathlib import Path
ARGS=None;M=None;ACTUAL=None
class RotationReferencePinCollection(unittest.TestCase):
 def setUp(self):
  self.rows=copy.deepcopy(ACTUAL);self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.root=Path(self.temp.name)
 def descriptor(self,d):
  self.assertEqual(set(d),{'path','sha256'});p=Path(d['path']);self.assertEqual(hashlib.sha256(p.read_bytes()).hexdigest(),d['sha256']);self.seen.add(str(p));return p
 def collect(self,value):
  self.seen=set();M.collect_rotation_reference_pins(value,self.descriptor);return self.seen
 def test_actual_schema2_pins_every_nested_native_input_contract(self):
  actual=self.collect(self.rows);expected={self.rows['base_reference']['path']}
  for row in self.rows['supplementary_references']:
   expected.update(row[k]['path']for k in ['reference','capture_input','function_contract'])
  self.assertEqual(actual,expected)
 def test_legacy_schema1_exact_two_callbacks_unchanged(self):
  seen=[];base={'path':'base-controlled','sha256':'a'*64};supp={'path':'supp-controlled','sha256':'b'*64};M.collect_rotation_reference_pins({'schema_version':1,'kind':'SourceQualifiedOperationMapRotationReferenceSet','base_reference':base,'supplementary_reference':supp},lambda d:seen.append(d));self.assertEqual(seen,[base,supp])
 def test_unknown_schema_fails(self):
  self.rows['schema_version']=3
  with self.assertRaisesRegex(ValueError,'Supported explicit rotation'):self.collect(self.rows)
 def test_bool_schema_fails(self):
  self.rows['schema_version']=True
  with self.assertRaisesRegex(ValueError,'Supported explicit rotation'):self.collect(self.rows)
 def test_unknown_root_field_fails(self):
  self.rows['unknown']=True
  with self.assertRaisesRegex(ValueError,'Exact version2'):self.collect(self.rows)
 def test_missing_capture_input_fails(self):
  del self.rows['supplementary_references'][0]['capture_input']
  with self.assertRaisesRegex(ValueError,'Exact native rotation supplement'):self.collect(self.rows)
 def test_unknown_descriptor_field_fails(self):
  self.rows['supplementary_references'][0]['reference']['unknown']=True
  with self.assertRaisesRegex(ValueError,'Exact native rotation descriptor'):self.collect(self.rows)
 def test_empty_supplements_fails(self):
  self.rows['supplementary_references']=[]
  with self.assertRaisesRegex(ValueError,'Finite native rotation'):self.collect(self.rows)
 def changed_contract(self,key):
  row=self.rows['supplementary_references'][0];value=json.loads(Path(row['function_contract']['path']).read_text());value[key]=dict(value[key],sha256='0'*64);p=self.root/'controlled-wrong-contract.json';p.write_text(json.dumps(value));row['function_contract']={'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
 def test_wrong_native_contract_link_fails(self):
  self.changed_contract('native_reference')
  with self.assertRaisesRegex(ValueError,'Native rotation function contract descriptor differs'):self.collect(self.rows)
 def test_wrong_capture_contract_link_fails(self):
  self.changed_contract('capture_input')
  with self.assertRaisesRegex(ValueError,'Native rotation function contract descriptor differs'):self.collect(self.rows)
def main():
 global ARGS,M,ACTUAL
 p=argparse.ArgumentParser();p.add_argument('--ci-root',required=True);p.add_argument('--reference-set',required=True);ARGS=p.parse_args();root=Path(ARGS.ci_root).resolve();sys.path.insert(0,str(root));M=__import__('skirmish_source_build');ACTUAL=json.loads(Path(ARGS.reference_set).read_text());suite=unittest.defaultTestLoader.loadTestsFromTestCase(RotationReferencePinCollection);result=unittest.TextTestRunner(verbosity=2).run(suite)
 if not result.wasSuccessful():raise SystemExit(1)
 print('[RotationReferencePinCollection] result=Passed checks='+str(result.testsRun)+' World=False')
if __name__=='__main__':main()

import copy,json,pathlib,tempfile,unittest
import s002_derived_bootstrap as b

class Tests(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory(dir='/private/tmp');self.path=pathlib.Path(self.temp.name)/'derived.json'
  self.v=dict(schema_version=1,kind='S002SourceOnlyDerivedSeed',mission='S002',seed=99,target_setup_hash=123,origin_seed=104731,origin_setup_hash=3657809016,native_factory_sha256=b.ORIGIN,semantic_inputs_sha256='5f5f805b12440d38e45b65067bf216b9350d14026568cee565c9d39515386cdf',diagnostic_only=True,native_context_qualified=False,mission_ready=False)
 def tearDown(self):self.temp.cleanup()
 def write(self,v):self.path.write_text(json.dumps(v));return b.load(self.path)
 def test_descriptor_and_initial(self):
  v,d=self.write(self.v);i={'mission_id':'S002','selection':{'seed':99,'size_id':1,'difficulty_id':2},'s002_derived_seed':d,'metadata_compatibility':{'sha256':b.ORIGIN}};self.assertTrue(b.admit_initial(i,v,d))
  for key,value in [('mission_id','S026'),('selection',{'seed':100,'size_id':1,'difficulty_id':2}),('s002_derived_seed',{'path':d['path'],'sha256':'0'*64}),('metadata_compatibility',{'sha256':'0'*64})]:
   bad=copy.deepcopy(i);bad[key]=value
   with self.subTest(key=key),self.assertRaises(ValueError):b.admit_initial(bad,v,d)
  guard=b.generated_guard(v,d);self.assertIn('q["seed"]',guard);self.assertIn('q["native_factory_sha256"]',guard)
 def test_current_native_epoch_is_exactly_bound(self):
  value=copy.deepcopy(self.v);value['native_factory_sha256']=b.CURRENT_ORIGIN
  value,descriptor=self.write(value)
  initial={'mission_id':'S002','selection':{'seed':99,'size_id':1,'difficulty_id':2},'s002_derived_seed':descriptor,'metadata_compatibility':{'sha256':b.CURRENT_ORIGIN}}
  self.assertTrue(b.admit_initial(initial,value,descriptor))
  guard=b.generated_guard(value,descriptor);self.assertIn(b.CURRENT_ORIGIN,guard);self.assertIn('q["native_factory_sha256"]',guard)
  for native_sha in (b.ORIGIN,'0'*64):
   bad=copy.deepcopy(initial);bad['metadata_compatibility']['sha256']=native_sha
   with self.subTest(native_sha=native_sha),self.assertRaises(ValueError):b.admit_initial(bad,value,descriptor)
  bad_value=copy.deepcopy(value);bad_value['native_factory_sha256']=b.ORIGIN
  bad_value,wrong_descriptor=self.write(bad_value);bad=copy.deepcopy(initial);bad['s002_derived_seed']=wrong_descriptor
  with self.assertRaises(ValueError):b.admit_initial(bad,bad_value,wrong_descriptor)
  for field,value_to_test in [('semantic_inputs_sha256','0'*64),('origin_setup_hash',3657809017),('native_factory_sha256','0'*64)]:
   bad_value=copy.deepcopy(value);bad_value[field]=value_to_test
   with self.subTest(field=field),self.assertRaises(ValueError):self.write(bad_value)
 def test_invalid_fields(self):
  for key,value in [('seed',True),('seed',1),('mission','S026'),('target_setup_hash',-1),('native_factory_sha256','0'*64),('native_factory_sha256',[]),('semantic_inputs_sha256','0'*64),('native_context_qualified',True),('mission_ready',True),('diagnostic_only',False),('origin_seed',1)]:
   v=copy.deepcopy(self.v);v[key]=value
   with self.subTest(key=key),self.assertRaises(ValueError):self.write(v)
 def test_duplicate_and_result(self):
  self.path.write_text('{"seed":1,"seed":2}')
  with self.assertRaises(ValueError):b.load(self.path)
  r=b.classify({'status':'SourceOutcomeObserved','source_result':{'source_outcome':'Victory'},'validation_passed':True});self.assertEqual(r['status'],'DiagnosticSourceOutcomeObserved');self.assertFalse(r['validation_passed']);self.assertFalse(r['native_acceptance'])
if __name__=='__main__':unittest.main()

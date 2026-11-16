"""Finite input checks. Actual native-author protocol cases are recorded separately."""
import json,math,shutil,sys,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
CI=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(CI))
import skirmish_source_authoring as a
class AuthorInputTests(unittest.TestCase):
 def setUp(self):self.tmp=tempfile.TemporaryDirectory();self.root=Path(self.tmp.name).resolve()
 def tearDown(self):self.tmp.cleanup()
 def manifest_package(self):
  package=self.root/'package';default=package/'Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json'
  default.parent.mkdir(parents=True)
  shutil.copyfile(CI/'SkirmishSourceSimulation/source_manifest.plan.json',default)
  output=self.root/'author-output';output.mkdir()
  return package,default,output
 def custom_manifest(self,profile=None,entries=None):
  value={'schema_version':1,'profile':profile or {'mission':'S002','seed':104731,'numeric_runtime':'InstalledMono'},
         'source_profile_variants':[{'mission':'S073','seed':104729,'runtime_backend':'LegacyS073OwnerV1','scope':'LegacyStartupAndQueueOnly'}],
         'compiler_defines':['WARLINE_SOURCE_GRID_WITNESS'],'sources':entries or []}
  path=self.root/'custom-source-manifest.json';path.write_text(json.dumps(value)+'\n');return path
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
 def call(self,mission='S026',seed=104755,timeout=30):return a.author(self.root,self.root,None,self.root/'output',mission,seed,self.root/'unused.json',timeout=timeout)
 def test_wrong_entry_identity_precedes_build_or_output(self):
  for mission,seed in [('S000',1),('S121',1),('s050',1),('S050',0),('S050',True),('S050',2147483648),('S026',104731)]:
   with self.subTest(mission=mission,seed=seed),self.assertRaisesRegex(Exception,'Canonical mission and positive seed'):self.call(mission,seed)
  self.assertFalse((self.root/'output').exists())
 def test_invalid_timeout_precedes_build_or_output(self):
  for value in [0,-1,float('inf'),float('nan')]:
   with self.subTest(timeout=value),self.assertRaisesRegex(Exception,'Finite positive'):self.call(timeout=value)
  self.assertFalse((self.root/'output').exists())
 def test_graph_pin_batch_preserves_pins_and_merges_once(self):
  old=self.root/'prior.cs';old.write_text('prior');one=self.root/'one.cs';one.write_text('one');two=self.root/'two.asset';two.write_text('two')
  pins={str(old):a.b.sha(old.read_bytes())};graph={str(one):a.b.sha(one.read_bytes()),str(two):a.b.sha(two.read_bytes())}
  with patch.object(a.b,'merge_pins',wraps=a.b.merge_pins) as merge:a.merge_authoring_graph_pins(pins,graph,self.root,self.root)
  self.assertEqual(merge.call_count,1);self.assertEqual(pins,{str(old):a.b.sha(old.read_bytes()),**graph})
 def test_graph_pin_batch_rejects_stale_accumulated_pin(self):
  old=self.root/'prior.cs';old.write_text('changed');one=self.root/'one.cs';one.write_text('one')
  with self.assertRaisesRegex(Exception,'Input changed:'):a.merge_authoring_graph_pins({str(old):a.b.sha(b'prior')},{str(one):a.b.sha(one.read_bytes())},self.root,self.root)
 def test_graph_pin_batch_rechecks_graph_bytes_at_merge(self):
  one=self.root/'one.cs';one.write_text('one');actual_pin=a.b.pin
  def change_after_pin(path):
   result=actual_pin(path);one.write_text('changed');return result
  with patch.object(a.b,'pin',side_effect=change_after_pin),self.assertRaisesRegex(Exception,'Input changed before merge'):a.merge_authoring_graph_pins({}, {str(one):a.b.sha(b'one')},self.root,self.root)
 def test_graph_pin_batch_rejects_conflicting_epoch(self):
  one=self.root/'one.cs';one.write_text('prior');new=a.b.sha(b'new')
  with patch.object(a.b,'pin',return_value={'sha256':new}),self.assertRaisesRegex(Exception,'Conflicting source/runtime epoch'):a.merge_authoring_graph_pins({str(one):a.b.sha(one.read_bytes())},{str(one):new},self.root,self.root)
 def draft(self):
  return {'schema_version':1,'kind':'SkirmishMissionAuthoringDraft','catalog_id':'S050','seed':104779,'size':'Standard','difficulty':'Regular','graph':{},'profiles':{},'campaign_source_path':'Assets/Game/source.asset','layout':{},'setup_matrix':{}}
 def test_authoring_draft_keeps_legacy_request_and_accepts_opt_in_preset(self):
  legacy=self.draft();a.validate_authoring_draft_fields(legacy,'S050',104779)
  selected=self.draft();selected['resource_preset']={'path':'/unused','sha256':'0'*64}
  a.validate_authoring_draft_fields(selected,'S050',104779)
 def test_authoring_draft_rejects_unknown_fields(self):
  selected=self.draft();selected['invented_preset']='SkirmishBaseAssault'
  with self.assertRaisesRegex(Exception,'Exact finite authoring input fields'):a.validate_authoring_draft_fields(selected,'S050',104779)
 def test_resource_preset_selection_requires_hash_and_typed_graph_node(self):
  asset=self.root/'Assets/Game/Resources/Selected.asset';asset.parent.mkdir(parents=True);asset.write_text('preset')
  pin=a.b.sha(asset.read_bytes());descriptor={'path':str(asset),'sha256':pin}
  node={'source_path':'Assets/Game/Resources/Selected.asset','source_type':'SkirmishPresetConfig','source_sha256':pin,'fields':{'supplyDaySeconds':120},'scalar_spellings':{'supplyDaySeconds':'120'}}
  graph={'source_pins':{str(asset):pin},'nodes':[node]}
  self.assertEqual(a.validate_resource_preset_selection(descriptor,graph,self.root),descriptor)
  with self.assertRaisesRegex(Exception,'hash mismatch'):
   a.validate_resource_preset_selection({'path':str(asset),'sha256':'0'*64},graph,self.root)
  with self.assertRaisesRegex(Exception,'exact source-graph pin'):
   a.validate_resource_preset_selection(descriptor,{'source_pins':{},'nodes':graph['nodes']},self.root)
  wrong_type={'source_pins':graph['source_pins'],'nodes':[dict(node,source_type='SkirmishEconomyConfig')]}
  with self.assertRaisesRegex(Exception,'source-hash-matched SkirmishPresetConfig'):
   a.validate_resource_preset_selection(descriptor,wrong_type,self.root)
  for label,changed in [('changed',dict(node,fields={'supplyDaySeconds':121})),('missing',dict(node,fields={})),('nonfinite',dict(node,fields={'supplyDaySeconds':float('inf')}))]:
   with self.subTest(label=label),self.assertRaisesRegex(Exception,'finite positive serialized graph scalar'):
    a.validate_resource_preset_selection(descriptor,{'source_pins':graph['source_pins'],'nodes':[changed]},self.root)
  duplicate={'source_pins':graph['source_pins'],'nodes':[node,dict(node)]}
  with self.assertRaisesRegex(Exception,'source-hash-matched SkirmishPresetConfig'):
   a.validate_resource_preset_selection(descriptor,duplicate,self.root)
  with self.assertRaisesRegex(Exception,'Exact optional resource-preset descriptor'):
   a.validate_resource_preset_selection(None,graph,self.root)
 def test_author_manifest_derives_pinned_default_without_mutating_base(self):
  package,base,output=self.manifest_package();before=base.read_bytes();base_value=json.loads(before)
  derived,base_pin,derived_pin=a.prepare_authoring_manifest(base,package,output)
  self.assertEqual(base.read_bytes(),before)
  self.assertEqual(base_pin['sha256'],a.AUTHOR_BASE_MANIFEST_SHA256)
  self.assertEqual(a.b.sha(derived.read_bytes()),derived_pin['sha256'])
  derived_value=json.loads(derived.read_bytes())
  self.assertEqual(derived_value['profile'],base_value['profile'])
  self.assertEqual(derived_value['source_profile_variants'],base_value['source_profile_variants'])
  self.assertEqual(derived_value['sources'],base_value['sources'])
  self.assertEqual(sum(row.get('id')==a.AUTHOR_CONFIGURATION_QUALIFICATION['id'] for row in derived_value['sources']),1)
 def test_author_manifest_rejects_conflicting_custom_adapter(self):
  package,_,output=self.manifest_package()
  bad=dict(a.AUTHOR_CONFIGURATION_QUALIFICATION);bad['scope']='conflicting qualification scope'
  path=self.custom_manifest(entries=[bad])
  with self.assertRaisesRegex(Exception,'Custom author manifest must contain one exact shared qualification adapter'):
   a.prepare_authoring_manifest(path,package,output)
 def test_author_manifest_rejects_changed_pinned_default(self):
  package,base,output=self.manifest_package()
  base.write_bytes(base.read_bytes()+b' ')
  with self.assertRaisesRegex(Exception,'Pinned shared S002 source manifest changed'):
   a.prepare_authoring_manifest(base,package,output)
 def test_author_manifest_accepts_supported_custom_manifest_without_rewriting(self):
  package,_,output=self.manifest_package()
  path=self.custom_manifest(entries=[dict(a.AUTHOR_CONFIGURATION_QUALIFICATION)]);before=path.read_bytes()
  selected,base_pin,derived_pin=a.prepare_authoring_manifest(path,package,output)
  self.assertEqual(selected,path.resolve());self.assertIsNone(base_pin);self.assertIsNone(derived_pin)
  self.assertEqual(path.read_bytes(),before)
 def test_author_manifest_rejects_custom_non_s002_profile(self):
  package,_,output=self.manifest_package()
  profile={'runtime_backend':'TypedGroundS026V1','numeric_runtime':'InstalledMono','qualification_lineage':{'mission':'S026','seed':104755}}
  path=self.custom_manifest(profile=profile,entries=[dict(a.AUTHOR_CONFIGURATION_QUALIFICATION)])
  with self.assertRaisesRegex(Exception,'Shared authoring manifest must retain the exact S002/104731 compilation profile'):
   a.prepare_authoring_manifest(path,package,output)
 def test_author_manifest_rechecks_base_pin_after_derivation(self):
  package,base,output=self.manifest_package();original=Path.read_bytes;reads={'count':0}
  def changed_on_recheck(path):
   raw=original(path)
   if path.resolve()==base.resolve():
    reads['count']+=1
    if reads['count']==2:return raw+b'changed-after-pin'
   return raw
  with patch.object(Path,'read_bytes',changed_on_recheck),self.assertRaisesRegex(Exception,'Shared base manifest changed during author-only derivation'):
   a.prepare_authoring_manifest(base,package,output)
  self.assertEqual(base.read_bytes(),original(base))
 def projection(self):
  def descriptor(name):
   path=self.root/(name+'.json');path.write_text(name);return {'path':str(path),'sha256':a.b.sha(path.read_bytes())}
  request_pin=descriptor('request')
  descriptors={name:descriptor(name) for name in ('source_config_graph','setup_matrix','layout','logical_binding','canonical_battle_catalog')}
  return {'kind':'SkirmishMissionAuthoringOwnerProjection','status':'Partial_CompiledOwnerInputsOnly','scope':'OriginalConfigureAndTryCompileOwnerOutputAndExactRequestPins;NoInitializationOrRuntimeAdmission','target':{'mission_id':'S074','seed':104803,'setup_hash':1298808707,'size':'Standard','difficulty':'Regular','operation_map_id':'logical-river-ford','source_definition_operation_map_id':'campaign-river-definition','source_operation_map_id':'campaign-river-map'},'owner_request':request_pin,'owner_descriptors':descriptors,'runtime_fields_not_supplied_by_this_projection':['GeneratedMissionInitialization','camera_profile','camera_values','camera_motion'],'world_allocated':False,'runtime_admission':False,'source_fit_certified':False,'mission_ready':False,'native_acceptance':False}
 def projection_result(self):
  return {'size':'Standard','difficulty':'Regular','operation_map_id':'campaign-river-definition','physical_source_operation_map_id':'campaign-river-map','resolved_setup':{'setup_hash':1298808707,'operation_map_id':'logical-river-ford'}}
 def test_owner_projection_contract_accepts_exact_partial_owner_data(self):
  value=self.projection();self.assertTrue(a.validate_owner_projection(value,value['owner_request'],self.projection_result(),'S074',104803))
 def test_owner_projection_accepts_distinct_logical_alias_and_physical_source(self):
  value=self.projection();result=self.projection_result()
  self.assertNotEqual(value['target']['operation_map_id'],value['target']['source_definition_operation_map_id'])
  self.assertNotEqual(value['target']['source_definition_operation_map_id'],value['target']['source_operation_map_id'])
  self.assertTrue(a.validate_owner_projection(value,value['owner_request'],result,'S074',104803))
 def test_owner_projection_rejects_swapped_or_mismatched_map_identities(self):
  value=self.projection();result=self.projection_result();value['target']['source_operation_map_id']=result['operation_map_id']
  with self.assertRaisesRegex(Exception,'Logical setup, source definition and physical source map identities'):
   a.validate_owner_projection(value,value['owner_request'],result,'S074',104803)
  value=self.projection();value['target']['source_definition_operation_map_id']='other-source-definition'
  with self.assertRaisesRegex(Exception,'Logical setup, source definition and physical source map identities'):
   a.validate_owner_projection(value,value['owner_request'],result,'S074',104803)
  value=self.projection();value['target']['operation_map_id']='campaign-river-definition'
  with self.assertRaisesRegex(Exception,'Logical setup, source definition and physical source map identities'):
   a.validate_owner_projection(value,value['owner_request'],result,'S074',104803)
 def test_owner_projection_rejects_missing_owner_descriptor(self):
  value=self.projection();del value['owner_descriptors']['layout']
  with self.assertRaisesRegex(Exception,'Exact finite owner descriptor set'):a.validate_owner_projection(value,value['owner_request'],self.projection_result(),'S074',104803)
 def test_owner_projection_rejects_altered_owner_input_pin(self):
  value=self.projection();value['owner_descriptors']['source_config_graph']['sha256']='0'*64
  with self.assertRaisesRegex(Exception,'Owner descriptor changed'):a.validate_owner_projection(value,value['owner_request'],self.projection_result(),'S074',104803)
 def test_owner_projection_opt_in_preset_must_be_in_compiler_pin_closure(self):
  value=self.projection();preset=self.root/'preset.asset';preset.write_text('preset');preset_desc={'path':str(preset),'sha256':a.b.sha(preset.read_bytes())};value['owner_descriptors']['resource_preset']=preset_desc
  result={**self.projection_result(),'input_pins':{value['owner_request']['path']:{'sha256':value['owner_request']['sha256']},str(preset):{'sha256':preset_desc['sha256']}}}
  self.assertTrue(a.validate_owner_projection(value,value['owner_request'],result,'S074',104803))
  result['input_pins'][str(preset)]['sha256']='0'*64
  with self.assertRaisesRegex(Exception,'absent from exact compiler input pins'):a.validate_owner_projection(value,value['owner_request'],result,'S074',104803)
 def test_owner_projection_rejects_generated_init_or_readiness_claim(self):
  value=self.projection();value['mission_ready']=True
  with self.assertRaisesRegex(Exception,'Owner request pin or non-readiness boundary'):a.validate_owner_projection(value,value['owner_request'],self.projection_result(),'S074',104803)
if __name__=='__main__':unittest.main()

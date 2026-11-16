"""Source-only admission controls; fixtures do not establish mission readiness."""
import hashlib
import json
import runpy
import sys
import tempfile
import unittest
from pathlib import Path

CI = Path(__file__).resolve().parents[1]
owned = sys.modules.get('skirmish_owned_source')
if owned is None:
 namespace = runpy.run_path(str(CI / 'run_skirmish_source_simulation.py'), run_name='fixture')
 owned = namespace['owned']
profiles = owned.require('skirmish_source_profiles')
build = owned.require('skirmish_source_build')


class S026SourceContextControls(unittest.TestCase):
 def setUp(self):
  profiles._EXTERNAL = None
  self.temp = tempfile.TemporaryDirectory(prefix='s026-source-context-')
  self.root = Path(self.temp.name).resolve()

 def tearDown(self):
  profiles._EXTERNAL = None
  self.temp.cleanup()

 def bind_fixture(self, seed=104755, mission='S026', difficulty='Regular'):
  target={'catalog_id':mission,'definition_id':'skirmish.'+mission.lower(),'content_version':1,
          'seed':seed,'size':'Standard','difficulty':difficulty,'setup_hash':1,
          'operation_map_id':'fixture.map'}
  for name in ('source_content_hash','source_fingerprint','layout_fingerprint',
               'layout_binding_fingerprint','mission_logical_binding_fingerprint'):
   target[name]='a'*64
  checks={name:True for name in ('original_all_role_overlay_fields_raw_equal',
          'original_provider_selection_equal','original_native_origin68_and_assets_exact',
          'current_factory67_package_exact','factory_sections_unchanged')}
  context={'schema_version':1,'kind':'OriginalCompilerDerivedNativeFactoryContext',
           'authority':'ActualSourceCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050',
           'target_context':target,'native_factory_reference':{},'native_origin':{},
           'derived_metadata':{},'derived_metadata_key_sha256':'b'*64,
           'unchanged_native_sections_sha256':{},'section_hash_format':'NewtonsoftCompactUtf8Sha256',
           'native_origin_descriptor':{},'current_factory_source_root':str(self.root),
           'consumed_input_pins':{},'checks':checks,'world_allocated':False,
           'identities_allocated':0,'native_S050_capture':False,'source_fit_certified':False,
           'active_mission_water_qualified':False,'mission_ready':False,'runtime_loader_admitted':False}
  cp=self.root/'context.json';cp.write_text(json.dumps(context))
  descriptor={'path':str(cp),'sha256':hashlib.sha256(cp.read_bytes()).hexdigest()}
  qualification={'schema_version':1,'kind':'ExperimentalGroundConfigurationQualification',
                 'runtime_backend':'TypedGroundS026V1','compiler_context':descriptor,
                 'scope':'OriginalCompilerAndQualifiedFactoryInputs_SourceOnly',
                 'native_context_qualified':False,'mission_ready':False}
  qp=self.root/'qualification.json';qp.write_text(json.dumps(qualification))
  profiles.bind_configuration_qualification(str(qp))
  return descriptor

 def test_explicit_context_replaces_only_matching_s026_profile(self):
  profiles.require_profile('S026',104755)
  self.bind_fixture()
  data,_=profiles.load_profiles()
  selected=[r for r in data['profiles'] if (r['mission_id'],r['difficulty_id'],r['size_id'])==('S026',2,1)]
  self.assertEqual(len(selected),1)
  self.assertIn('compiler_context',selected[0])
  profiles.require_profile('S026',104755)
  with self.assertRaisesRegex(ValueError,'Unqualified source execution profile'):
   profiles.require_profile('S026',104754)
  profiles._EXTERNAL=None
  self.bind_fixture(seed=104754)
  with self.assertRaisesRegex(ValueError,'Duplicate profile'):
   profiles.load_profiles()

 def test_candidate_requires_explicit_context_and_exact_inactive_identity(self):
  context=self.bind_fixture()
  nav={'path':str(self.root/'capture.json'),'sha256':'fa0d0dedd774ba7648ee2cedaa872f506339878bff45429abadf9585157e2710'}
  candidate={'path':str(self.root/'candidate.json'),'sha256':'0a412bf51e718e3fba00d1a89f3699a9b8959c200933eb8d20c097fc0db86f61'}
  initial={'mission_id':'S026','selection':{'difficulty_id':2,'size_id':1,'seed':104755},
           'metadata_compiler_context':context,'configuration_qualification':profiles.configuration_qualification(),
           'navigation_reference':nav,'s026_declared_pad_candidate':candidate,
           'placement_domain':{'s026_inactive_diagnostic':dict(nav,source_root=str(self.root),external_inputs={})}}
  inputs={'mission':'S026','static_clearance':nav}
  self.assertEqual(build.require_s026_declared_candidate(initial,inputs),candidate)
  initial['selection']['seed']=104754
  with self.assertRaisesRegex(ValueError,'diagnostic candidate'):
   build.require_s026_declared_candidate(initial,inputs)
  initial['selection']['seed']=104755
  initial['configuration_qualification']=None
  with self.assertRaisesRegex(ValueError,'diagnostic candidate'):
   build.require_s026_declared_candidate(initial,inputs)

 def test_s026_preflight_selects_existing_inactive_template_owner(self):
  generated,_=profiles.generate_guard('S026')
  self.assertIn('GeneratedReferenceTemplateView.ReadS026Diagnostic',generated)
  self.assertIn('SourceNativeDeathReadModel.LoadS026Diagnostic',generated)
  self.assertIn('SourceNativeDeathReadModel.LoadAtBoundary',generated)
  self.assertIn('inactive!=null?',generated)
  self.assertIn('GeneratedReferenceTemplateView.Read((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],out binaryDirectory,expectedCaptureGraph)',generated)

 def test_s074_preflight_dispatches_contextual_template_reader_before_generic_branch(self):
  repo=CI.parents[1]
  profiles.bind_configuration_qualification(str(repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/qualification/configuration-qualification.json'))
  generated,_=profiles.generate_guard('S074')
  s074='if(compiled.setup.CatalogId=="S074")templateDomain=Game.Runtime.GeneratedReferenceTemplateView.Read((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],out binaryDirectory,(string)run["source_repository_root"],initial)'
  generic='Game.Runtime.GeneratedReferenceTemplateView.Read((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],out binaryDirectory,expectedCaptureGraph)'
  self.assertEqual(generated.count(s074),1)
  self.assertEqual(generated.count('Game.Runtime.GeneratedReferenceTemplateView.ReadS026Diagnostic'),1)
  self.assertLess(generated.index(s074),generated.index(generic))
  self.assertEqual(generated.count('SourceNativeDeathReadModel.LoadS074NoCameraDiagnostic'),1)

 def test_generic_source_context_binds_exact_difficulty_only(self):
  for number,name in enumerate(('Recruit','Regular','Veteran','Commander'),1):
   with self.subTest(difficulty=name):
    profiles._EXTERNAL=None
    self.bind_fixture(seed=104779,mission='S050',difficulty=name)
    rows,_=profiles.load_profiles()
    selected=[r for r in rows['profiles'] if r['mission_id']=='S050']
    self.assertEqual(len(selected),1)
    self.assertEqual(selected[0]['difficulty_id'],number)
    profiles.require_profile('S050',104779,difficulty_id=number)
    with self.assertRaisesRegex(ValueError,'Unqualified source execution profile'):
     profiles.require_profile('S050',104779,difficulty_id=number%4+1)

 def test_explicit_s026_context_rejects_stock_profile_fallback(self):
  for number,name in enumerate(('Recruit','Regular','Veteran','Commander'),1):
   with self.subTest(difficulty=name):
    profiles._EXTERNAL=None
    descriptor=self.bind_fixture(difficulty=name)
    initial={'mission_id':'S026','metadata_compiler_context':descriptor,
             'configuration_qualification':profiles.configuration_qualification(),
             'selection':{'difficulty_id':number,'size_id':1,'seed':104755}}
    profiles.require_initial_qualification_pair(initial)
    initial['selection']['difficulty_id']=number%4+1
    with self.assertRaisesRegex(ValueError,'explicitly bound source context'):
     profiles.require_initial_qualification_pair(initial)
    initial['selection']['difficulty_id']=number
    initial['selection']['seed']=104754
    with self.assertRaisesRegex(ValueError,'explicitly bound source context'):
     profiles.require_initial_qualification_pair(initial)
    initial['selection']['seed']=104755
    initial['metadata_compiler_context']={'path':descriptor['path'],'sha256':'0'*64}
    with self.assertRaisesRegex(ValueError,'explicitly bound source context'):
     profiles.require_initial_qualification_pair(initial)


if __name__=='__main__':
 unittest.main()

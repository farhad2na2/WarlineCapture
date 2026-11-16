import subprocess,sys,tempfile,shutil,json,unittest,runpy,ast,types
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
 def test_typed_preflight_binds_current_s003_template_to_validated_capture_graph(self):
  code=boot+load+"profiles.require_profile('S026',104755);source,_=profiles.generate_guard('S026')\n"
  code+="assert 'Newtonsoft.Json.Linq.JObject expectedCaptureGraph=null;if(compiled.setup.CatalogId==\\\"S003\\\"&&initial[\\\"metadata_compiler_context\\\"]!=null)' in source\n"
  code+="assert 'Game.Runtime.SourceConfigurationQualification.Context(initial)' in source\n"
  code+="assert 'expectedCaptureGraph=validatedTemplateContext[\\\"source_graph\\\"]as Newtonsoft.Json.Linq.JObject' in source\n"
  code+="assert 'GeneratedReferenceTemplateView.Read((Newtonsoft.Json.Linq.JObject)initial[\\\"reference_templates\\\"],out binaryDirectory,expectedCaptureGraph)' in source\n"
  with tempfile.TemporaryDirectory(prefix='source-profile-template-graph-') as temporary:
   dest=Path(temporary)/'ci';shutil.copytree(base,dest,ignore=shutil.ignore_patterns('__pycache__'))
   result=subprocess.run([sys.executable,'-B','-c',code,str(dest)],capture_output=True,text=True)
   self.assertEqual(result.returncode,0,result.stderr)

 def test_exact_owned_profile_epoch_and_json_binding(self):
  for name,code in cases.items():
   with tempfile.TemporaryDirectory(prefix='source-profile-control-')as temporary:
    dest=Path(temporary)/'ci';shutil.copytree(base,dest,ignore=shutil.ignore_patterns('__pycache__'));r=subprocess.run([sys.executable,'-B','-c',boot+code,str(dest)],capture_output=True,text=True)
    assert r.returncode!=0,(name,r.stdout);assert EXPECTED[name] in r.stderr,(name,r.stderr);checks.append({'id':name,'rejected':True,'reason':r.stderr.splitlines()[-1]})
  self.assertEqual(len(checks),5)
 def test_current_s003_descriptor_scope_and_target_negative_controls(self):
  code=boot+load+"import copy\nq={'schema_version':1,'kind':'ExperimentalS003CurrentSourceQualificationDescriptor','scope':'DescriptorOnly_CurrentCompilerAndFactoryData_SourceOnly','compiler_context':{'path':'/repo/context.json','sha256':'0'*64},'native_context_qualified':False,'source_fit_certified':False,'runtime_admission':False,'mission_ready':False}\ntarget={'catalog_id':'S003','definition_id':'skirmish.s003','seed':104732,'size':'Standard','difficulty':'Regular','setup_hash':2394754492,'layout_binding_fingerprint':'','mission_logical_binding_fingerprint':''}\nc={'kind':'OriginalS003CurrentSourceOnlyFactoryContext','authority':'CurrentAuthoredS003Graph_OriginalCompilerAndFactoryData_DescriptorOnly','target_context':target,'runtime_admission':False,'mission_ready':False,'native_context_qualified':False,'source_fit_certified':False,'full_mission_initialized':False,'mission_world_created':False,'runtime_state_transferred':False}\nprofiles._validate_s003_current_qualification_shape(q);profiles._validate_s003_current_target(c)\nfor key in ('native_context_qualified','source_fit_certified','runtime_admission','mission_ready'):\n bad=copy.deepcopy(q);bad[key]=True\n try:profiles._validate_s003_current_qualification_shape(bad);raise AssertionError(key)\n except ValueError:pass\nbad=copy.deepcopy(c);bad['target_context']['seed']+=1\ntry:profiles._validate_s003_current_target(bad);raise AssertionError('target')\nexcept ValueError:pass\n"
  r=subprocess.run([sys.executable,'-B','-c',code,str(base)],capture_output=True,text=True)
  self.assertEqual(r.returncode,0,r.stderr)
 def test_current_s003_projection_path_is_context_bound_and_portable(self):
  code=boot+load+"import hashlib,json,tempfile,copy\n"
  code+="root=Path(sys.argv[2]).resolve();out=root/'evidence'/'distinct-projection-output';out.mkdir(parents=True)\n"
  code+="capture={'path':str(root/'capture.json'),'sha256':'1'*64};capture_receipt={'path':str(root/'capture-receipt.json'),'sha256':'2'*64};graph={'path':str(root/'graph.json'),'sha256':'3'*64};compiler={'path':str(root/'compiler.json'),'sha256':'4'*64};audit={'path':str(root/'audit.json'),'sha256':'5'*64};build={'path':str(root/'build.json'),'sha256':'6'*64};closure={'path':str(root/'closure.json'),'sha256':'7'*64}\n"
  code+="artifacts={name:{'path':str(out/name),'sha256':str(i)*64} for i,name in enumerate(['building-definitions.json','unit-production-metadata.json','configured-metadata.json'],8)}\n"
  code+="epoch={'compiler_receipt_path':compiler['path'],'compiler_receipt_sha256':compiler['sha256'],'compiler_audit_path':audit['path'],'compiler_audit_sha256':audit['sha256'],'build_path':build['path'],'build_sha256':build['sha256'],'graph_closure_path':closure['path'],'graph_closure_sha256':closure['sha256']}\n"
  code+="projection={'kind':'S003CurrentFactoryMetadataProjectionReceipt','status':'PassedSourceOnlyProjection','target':{'catalog_id':'S003','difficulty':'Regular','size':'Standard','seed':104732,'setup_hash':2394754492},'capture':{**capture,'receipt_path':capture_receipt['path'],'receipt_sha256':capture_receipt['sha256']},'source_graph':graph,'artifacts':artifacts,'current_compiler_epoch':epoch,'runtime_admission':False,'mission_ready':False,'native_context_qualified':False,'source_fit_certified':False}\n"
  code+="p=out/'projection-receipt.json';raw=json.dumps(projection,separators=(',',':')).encode();p.write_bytes(raw);context={'native_factory_capture':capture,'native_factory_capture_receipt':capture_receipt,'source_graph':graph,'factory_projection_artifacts':artifacts,'compiler_receipt':compiler,'compiler_post_run_pin_audit':audit,'source_build_receipt':build,'compiler_graph':closure,'factory_projection_receipt':{'path':str(p),'sha256':hashlib.sha256(raw).hexdigest()}}\n"
  code+="assert profiles._s003_projection_directory(root,context)==out\n"
  code+="bad=copy.deepcopy(context);bad['factory_projection_receipt']['sha256']='0'*64\ntry:profiles._s003_projection_directory(root,bad);raise AssertionError('digest')\nexcept ValueError:pass\n"
  code+="bad_projection=dict(projection);bad_projection['target']=dict(projection['target'],seed=104733);bad_raw=json.dumps(bad_projection,separators=(',',':')).encode();p.write_bytes(bad_raw);bad=copy.deepcopy(context);bad['factory_projection_receipt']['sha256']=hashlib.sha256(bad_raw).hexdigest()\ntry:profiles._s003_projection_directory(root,bad);raise AssertionError('target')\nexcept ValueError:pass\n"
  with tempfile.TemporaryDirectory(prefix='s003-current-projection-path-') as temporary:
   r=subprocess.run([sys.executable,'-B','-c',code,str(base),temporary],capture_output=True,text=True)
   self.assertEqual(r.returncode,0,r.stderr)
 def test_current_s003_descriptor_generated_fixture_binds_only_exact_selection(self):
  root=base.parent.parent
  required=(root/'Logs/SkirmishProduction/s003-current-config-graph-20261103-r1/graph-r5/source-config-graph.json',root/'Logs/SkirmishProduction/s003-current-metadata-capture-20261103-r1/data/s003-native-metadata.json',root/'T Logs/SkirmishProduction/s003-current-factory-metadata-projection-20261103-T1/projection-receipt.json',root/'T Logs/SkirmishProduction/s003-current-factory-epoch-compiler-20261103-r1/receipt.json',root/'Logs/SkirmishProduction/s002-current-s003-factory-epoch-build-20261103-r1/build.json')
  if not all(p.is_file() for p in required):self.skipTest('Optional workspace-specific S003 source proof inputs are absent')
  code=boot+load+f"import importlib.util,tempfile\nbp=root/'SkirmishSourceSimulation/Recipes/build_s003_current_source_qualification_descriptor.py';sp=importlib.util.spec_from_file_location('s003_gate_test_builder',bp);builder=importlib.util.module_from_spec(sp);sp.loader.exec_module(builder)\nwith tempfile.TemporaryDirectory(dir=builder.ROOT) as td:\n out=Path(td)/'descriptor-output'\n try:builder.build(builder.ROOT,builder.ROOT/builder.PROJECTION_REL,out)\n except ValueError as error:\n  if 'Current source-build pin mismatch' in str(error) or str(error).startswith('Capture source changed: '):\n   assert not (out/'s003-current-source-qualification-descriptor.json').exists(), 'Stale evidence produced a descriptor'\n   print('STALE_EPOCH_FAIL_CLOSED');raise SystemExit(0)\n  raise\n qualification=out/'s003-current-source-qualification-descriptor.json'\n profiles.bind_configuration_qualification(qualification)\n profiles.require_profile('S003',104732)\n assert profiles.runtime_backend('S003')=='TypedGroundS026V1'\n try:profiles.require_profile('S003',104733);raise AssertionError('wrong seed admitted')\n except ValueError as error:assert 'Unqualified source execution profile' in str(error)\n print('CURRENT_EPOCH_BOUND_NO_MISSION')\n"
  r=subprocess.run([sys.executable,'-B','-c',code,str(base)],capture_output=True,text=True)
  self.assertEqual(r.returncode,0,r.stderr)
  self.assertTrue('STALE_EPOCH_FAIL_CLOSED' in r.stdout or 'CURRENT_EPOCH_BOUND_NO_MISSION' in r.stdout,r.stdout)
 def test_qualified_selection_and_strict_schema(self):
  with tempfile.TemporaryDirectory(prefix='source-profile-selection-')as temporary:
   dest=Path(temporary)/'ci';shutil.copytree(base,dest,ignore=shutil.ignore_patterns('__pycache__'))
   valid=boot+load+"profiles.require_profile('S002',1);profiles.require_profile('S002',104731);profiles.require_profile('S026',104755)\n"
   self.assertEqual(subprocess.run([sys.executable,'-B','-c',valid,str(dest)],capture_output=True).returncode,0)
   for expression in ["profiles.require_profile('S002',2)","profiles.require_profile('S026',104754)","profiles.require_profile('S003',104732)","profiles.require_profile('S002',1,1,1)","profiles.require_profile('S002',1,2,2)","profiles.require_profile('S002',True)"]:
    r=subprocess.run([sys.executable,'-B','-c',boot+load+expression,str(dest)],capture_output=True,text=True);self.assertNotEqual(r.returncode,0);self.assertIn('Unqualified source execution profile',r.stderr)
   config=dest/'SkirmishSourceSimulation/admitted_profiles.json';raw=config.read_text()
   for mutate,reason in [(lambda d:d.update(extra=1),'Unsupported admission schema/runtime'),(lambda d:d.update(schema_version=2),'Unsupported admission schema/runtime'),(lambda d:d['profiles'][0].update(extra=1),'Exact profile keys required'),(lambda d:d['profiles'][0]['seeds'].append(2),'Unqualified seed')]:
    d=json.loads(raw);mutate(d);config.write_text(json.dumps(d));r=subprocess.run([sys.executable,'-B','-c',boot+load+"profiles.require_profile('S002',1)",str(dest)],capture_output=True,text=True);self.assertNotEqual(r.returncode,0);self.assertIn(reason,r.stderr)


class S003DerivedContextControls(unittest.TestCase):
 def source_modules(self):
  owned=sys.modules.get('skirmish_owned_source')
  if owned is None:
   owned=runpy.run_path(str(base/'run_skirmish_source_simulation.py'),run_name='s003-context-control')['owned']
  return owned,owned.require('skirmish_source_profiles'),owned.require('skirmish_source_build')

 def cli_default_manifest(self,profiles):
  """Execute the real CLI default_manifest function with the loaded profile module."""
  runner_path=base/'run_skirmish_source_simulation.py'
  tree=ast.parse(runner_path.read_text(),filename=str(runner_path))
  node=next(item for item in tree.body if isinstance(item,ast.FunctionDef) and item.name=='default_manifest')
  namespace={'owned':types.SimpleNamespace(require=lambda name:profiles)}
  exec(compile(ast.Module(body=[node],type_ignores=[]),str(runner_path),'exec'),namespace)
  return namespace['default_manifest']

 def test_s003_derived_s075_context_and_source_only_scope(self):
  """A retained S075 context is admitted only with its exact schema and false gates."""
  context_path=base.parent.parent/'T Logs/SkirmishProduction/s075-s003-exporter-source-control-20261114-r1/result/source-factory-context.json'
  if not context_path.is_file():
   self.skipTest('Optional captured S075 derived compiler context is absent')
  _,profiles,_=self.source_modules()
  context=json.loads(context_path.read_text())
  target={
   'catalog_id':'S075','definition_id':'skirmish.s075','content_version':2,
   'seed':104804,'size':'Standard','difficulty':'Regular','setup_hash':1280564676,
   'operation_map_id':'opmap.skirmish.industrial_basin',
   'source_content_hash':'cbbaa8366cbfb5e0ad9bc6bf70a98b48503df10e3d0c5c45757295af841a7a6d',
   'source_fingerprint':'3f329ea016436f3104e7f69e975d871f842381e0b5654ff3f483256e786c3c96',
   'layout_fingerprint':'0777cae578e8f75106fd2f55478c8688498d3473ef8691113fd56c41031dc18a',
   'layout_binding_fingerprint':'cdf0f891866cbf3be71b819e8f9db5d59337514dd0c22ee8a1aef765e01eda6c',
   'mission_logical_binding_fingerprint':'a032ea05c372d21dbba6b7cee6b7f866e4bff81353378d74ccd5bebb2aa4e8bb'}
  self.assertEqual(profiles._validated_context_target(context),target)

  for flag in ('world_allocated','native_S050_capture','source_fit_certified',
               'active_mission_water_qualified','mission_ready','runtime_loader_admitted'):
   with self.subTest(flag=flag):
    bad=json.loads(json.dumps(context));bad[flag]=True
    with self.assertRaises(ValueError):profiles._validated_context_target(bad)
  for key,value in (('schema_version',3),('authority','Other'),('identities_allocated',1)):
   with self.subTest(key=key):
    bad=json.loads(json.dumps(context));bad[key]=value
    with self.assertRaises(ValueError):profiles._validated_context_target(bad)
  bad=json.loads(json.dumps(context));bad['checks']['factory_keyed_coverage_and_order_projected']=False
  with self.assertRaises(ValueError):profiles._validated_context_target(bad)
  bad=json.loads(json.dumps(context));bad['checks']['unexpected_check']=True
  with self.assertRaises(ValueError):profiles._validated_context_target(bad)
  bad=json.loads(json.dumps(context));bad['target_context']['catalog_id']='S074'
  with self.assertRaises(ValueError):profiles._validated_context_target(bad)

 def test_derived_qualification_is_exact_s075_and_selects_only_derived_manifest(self):
  repo=base.parent.parent
  qualification_path=repo/'T Logs/SkirmishProduction/s075-s003-derived-loader-control-20261114-r1/configuration-qualification.json'
  context_path=repo/'T Logs/SkirmishProduction/s075-s003-exporter-source-control-20261114-r1/result/source-factory-context.json'
  if not qualification_path.is_file() or not context_path.is_file():
   self.skipTest('Optional captured S075 derived qualification is absent')
  _,profiles,build=self.source_modules();default_manifest=self.cli_default_manifest(profiles)
  profiles._EXTERNAL=None
  profiles.bind_configuration_qualification(str(qualification_path))
  self.assertTrue(profiles.uses_s003_derived_factory('S075'))
  profiles.require_profile('S075',104804,2,1)
  self.assertEqual(build.canonical_source_manifest(repo,'S075').name,'source_manifest.s003derived.plan.json')
  self.assertEqual(default_manifest(repo,'S075','build',False).name,'source_manifest.s003derived.plan.json')

  original_context=json.loads(context_path.read_text())
  qualification=json.loads(qualification_path.read_text())
  mutations=(
   ('catalog_id','S074'),('definition_id','skirmish.s074'),('content_version',3),
   ('seed',104803),('size','War'),('difficulty','Veteran'),('setup_hash',1559121027),
   ('operation_map_id','opmap.other'),('source_content_hash','0'*64),
   ('source_fingerprint','0'*64),('layout_fingerprint','0'*64),
   ('layout_binding_fingerprint','0'*64),('mission_logical_binding_fingerprint','0'*64))
  for name,value in mutations:
   with self.subTest(target_field=name), tempfile.TemporaryDirectory(prefix='s075-derived-target-fence-') as temporary:
    profiles._EXTERNAL=None
    context=json.loads(json.dumps(original_context));context['target_context'][name]=value
    context_file=Path(temporary)/'context.json';context_file.write_text(json.dumps(context,separators=(',',':')))
    q=json.loads(json.dumps(qualification))
    q['compiler_context']={'path':str(context_file.resolve()),'sha256':__import__('hashlib').sha256(context_file.read_bytes()).hexdigest()}
    q_file=Path(temporary)/'qualification.json';q_file.write_text(json.dumps(q,separators=(',',':')))
    with self.assertRaises(ValueError):profiles.bind_configuration_qualification(str(q_file))
  profiles._EXTERNAL=None

 def test_persisted_s003_derived_row_requires_explicit_qualification_binding(self):
  repo=base.parent.parent
  context_path=repo/'T Logs/SkirmishProduction/s075-s003-exporter-source-control-20261114-r1/result/source-factory-context.json'
  if not context_path.is_file():
   self.skipTest('Optional captured S075 derived compiler context is absent')
  descriptor={'path':str(context_path.resolve()),'sha256':__import__('hashlib').sha256(context_path.read_bytes()).hexdigest()}
  row={'mission_id':'S075','difficulty_id':2,'size_id':1,'seeds':[104804],
       'runtime_backend':'TypedGroundS026V1','compiler_context':descriptor}
  with tempfile.TemporaryDirectory(prefix='unbound-s075-profile-') as temporary:
   dest=Path(temporary)/'ci';shutil.copytree(base,dest,ignore=shutil.ignore_patterns('__pycache__'))
   config=dest/'SkirmishSourceSimulation/admitted_profiles.json'
   data=json.loads(config.read_text());data['profiles'].append(row);config.write_text(json.dumps(data))
   code=boot+load+'profiles.load_profiles()\n'
   result=subprocess.run([sys.executable,'-B','-c',code,str(dest)],capture_output=True,text=True)
   self.assertNotEqual(result.returncode,0)
   self.assertIn('S003-derived target must be explicitly bound to its exact source qualification',result.stderr)

 def test_s074_and_strict_s003_manifest_dispatch_remain_unchanged(self):
  repo=base.parent.parent
  s074_qualification=repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/qualification/configuration-qualification.json'
  if not s074_qualification.is_file():
   self.skipTest('Optional retained S074 source qualification is absent')
  _,profiles,build=self.source_modules();default_manifest=self.cli_default_manifest(profiles)
  profiles._EXTERNAL=None;profiles.bind_configuration_qualification(str(s074_qualification))
  self.assertFalse(profiles.uses_s003_derived_factory('S074'))
  self.assertEqual(build.canonical_source_manifest(repo,'S074').name,'source_manifest.s026.plan.json')
  self.assertEqual(default_manifest(repo,'S074','build',False).name,'source_manifest.s026.plan.json')
  guard,_=profiles.generate_guard('S074')
  self.assertIn('if(compiled.setup.CatalogId=="S074")templateDomain=',guard)
  self.assertIn('((int)templatePreflight["roots"]!=16',guard)

  # The S003 catalog still selects its dedicated strict plan before the reusable
  # derived plan, even when testing the backend dispatcher without a live q.
  prior_backend=profiles.runtime_backend;prior_external=profiles._EXTERNAL
  try:
   profiles.runtime_backend=lambda mission:'TypedGroundS026V1'
   profiles._EXTERNAL={'row':{'mission_id':'S003'},'s003_derived_factory':False}
   self.assertEqual(build.canonical_source_manifest(repo,'S003').name,'source_manifest.s003.plan.json')
   self.assertEqual(default_manifest(repo,'S003','build',False).name,'source_manifest.s003.plan.json')
  finally:
   profiles.runtime_backend=prior_backend;profiles._EXTERNAL=prior_external


if __name__=="__main__":unittest.main()

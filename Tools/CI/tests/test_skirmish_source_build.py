"""Protocol-only build/run controls: temporary receipts and owned process stubs.

No fixture is gameplay/outcome evidence. No compiler, world, Unity or player runs.
"""
import ast,copy,hashlib,importlib.util,json,math,subprocess,sys,tempfile,unittest,shutil
from pathlib import Path
from unittest.mock import patch
sys.dont_write_bytecode=True
CI=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(CI))
import skirmish_source_build as m
import run_skirmish_source_batch as batch
from skirmish_source_manifest import UnsupportedSourceShape
COMPOSER_PATH=CI/'SkirmishSourceSimulation/Recipes/compose_s074_source_only_bundle.py'
COMPOSER_SPEC=importlib.util.spec_from_file_location('s074_source_only_composer_test',COMPOSER_PATH)
composer=importlib.util.module_from_spec(COMPOSER_SPEC);COMPOSER_SPEC.loader.exec_module(composer)

class ProtocolProcess:
    """Explicit fake child lifecycle; status artifacts are protocol fixtures only."""
    def __init__(self,command,*,env,stdout,stderr,source_result,returncode=0,timeout=False,kill_required=False,manifests=True,on_exit=None):
        self.command=command;self.env=env;self.stdout=stdout;self.stderr=stderr
        self.returncode=None;self.exit_code=returncode;self.timeout=timeout;self.kill_required=kill_required
        self.terminated=False;self.killed=False;self.waits=[];self.on_exit=on_exit
        request=json.loads(Path(command[-1]).read_bytes());out=Path(request['output_directory'])
        if source_result is not None:(out/'result.json').write_text(json.dumps(source_result))
        if manifests:
            for name in ['runtime-before-world.json','runtime-after-outcome.json']:
                (out/name).write_text(json.dumps({'protocol_fixture_only':True,'not_gameplay_or_loaded_runtime_proof':True}))
        stdout.write('PROTOCOL_PROCESS_STUB_ONLY\n');stdout.flush()
    def wait(self,timeout=None):
        self.waits.append(timeout)
        if self.timeout and len(self.waits)==1:raise subprocess.TimeoutExpired(self.command,timeout)
        if self.kill_required and self.terminated and not self.killed:raise subprocess.TimeoutExpired(self.command,timeout)
        self.returncode=-9 if self.killed else -15 if self.terminated else self.exit_code
        if self.on_exit:self.on_exit()
        return self.returncode
    def poll(self):return self.returncode
    def terminate(self):self.terminated=True
    def kill(self):self.killed=True

class SourceBuildProtocolTests(unittest.TestCase):
    def test_compiler_stage_clock_excludes_postverify_while_compile_wall_keeps_it(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary).resolve();repo=root/'repo';package=root/'package';repo.mkdir();package.mkdir()
            manifest=package/'manifest.json';manifest.write_text('{}');out=root/'build'
            clock={'now':0.0};verify_calls=[]
            def monotonic():return clock['now']
            def resolve(*args):
                (out/'source').mkdir(parents=True);(out/'source/resolution.json').write_text('{}')
                clock['now']=1.0
                return {'unresolved':[],'pins':{},'resolved':[],'runtime_backend':'FixtureBackend'}
            def placement_build(*args):
                clock['now']=2.0
                return {'dll':str(root/'placement.dll'),'source_and_reference_pins':{}}
            def verify_stub(_pins):
                verify_calls.append(clock['now'])
                if len(verify_calls)==2 and clock['now']==2.0:clock['now']=4.0
                if clock['now']>=8.0:clock['now']=13.0 # delayed post-compiler verification
            def compile_stub(*args,**kwargs):
                clock['now']=8.0 # compiler ran from t=4 to t=8
                return type('CompilerResult',(),{'returncode':0,'stdout':'','stderr':''})()
            profile=type('Profile',(),{
                'runtime_configuration_mode':staticmethod(lambda mission:'fixture'),
                'runtime_backend':staticmethod(lambda mission:'FixtureBackend'),
                'derived':staticmethod(lambda:None),
            })()
            tools={name:root/name for name in ('mono','dotnet','csc','core','script','engine','contents','newtonsoft')}
            tools['refs']=[]
            descriptor={'path':str(manifest),'sha256':'a'*64,'bytes':2}
            with patch.object(m,'resolve_manifest',side_effect=resolve), \
                 patch.object(m,'runtime_paths',return_value=tools), \
                 patch.object(m.owned,'load_placement',return_value=type('Placement',(),{'build':staticmethod(placement_build)})()), \
                 patch.object(m.owned,'pins',return_value={}), \
                 patch.object(m.owned,'require',return_value=profile), \
                 patch.object(m,'_generate_navigation_identity'), \
                 patch.object(m,'merge_pins'), patch.object(m,'merge_file_pins'), \
                 patch.object(m,'verify',side_effect=verify_stub), \
                 patch.object(m,'read_json_pinned',return_value=({},descriptor)), \
                 patch.object(m,'canonical_source_manifest',return_value=package/'canonical.json'), \
                 patch.object(m,'pin',side_effect=lambda p:{'path':str(Path(p)),'sha256':'b'*64,'bytes':1}), \
                 patch.object(m.subprocess,'run',side_effect=compile_stub), \
                 patch.object(m.time,'monotonic',side_effect=monotonic):
                receipt=m.build(repo,package,manifest,out)
            self.assertEqual(verify_calls,[2.0,2.0,8.0])
            self.assertEqual(receipt['build_stage_wall_seconds']['compiler_process'],4.0,(verify_calls,receipt['build_stage_wall_seconds']))
            self.assertEqual(receipt['build_stage_wall_seconds']['compile_log_and_post_verify'],5.0)
            self.assertEqual(receipt['compile_wall_seconds'],9.0)
            written=json.loads((out/'build.json').read_text())
            self.assertEqual(written['compile_wall_seconds'],9.0)
            self.assertEqual(written['build_stage_wall_seconds'],receipt['build_stage_wall_seconds'])

    def test_s050_nested_death_projection_is_recomputed_and_pinned(self):
        repo=CI.parents[1]
        bundle=json.loads((repo/'T Logs/SkirmishProduction/s050-source-only-composed-bundle-20261115-r3/data-bundle.json').read_bytes())
        initial=json.loads(Path(bundle['initialization']['path']).read_bytes())
        pins=m.s050_source_diagnostic_death_pins(initial,repo)
        self.assertGreaterEqual(len(pins),8106)
        self.assertIn(initial['death_mechanical_inputs']['s050_source_diagnostic']['projection']['path'],pins)
        stale=copy.deepcopy(initial);stale['death_mechanical_inputs']['s050_source_diagnostic']['projection']['sha256']='0'*64
        with self.assertRaisesRegex(UnsupportedSourceShape,'S050 diagnostic input pin mismatch'):
            m.s050_source_diagnostic_death_pins(stale,repo)
        flat=copy.deepcopy(initial);flat['death_mechanical_inputs']={'path':'/tmp/legacy.json','sha256':'0'*64}
        with self.assertRaisesRegex(UnsupportedSourceShape,'Exact nested S050 death diagnostic descriptor'):
            m.s050_source_diagnostic_death_pins(flat,repo)
        nested=initial['death_mechanical_inputs']['s050_source_diagnostic']
        with tempfile.TemporaryDirectory() as td:
            changed_projection=Path(td)/'projection.json'
            projection=json.loads(Path(nested['projection']['path']).read_bytes())
            projection['rows'][0]['death_animation_index']+=1
            changed_projection.write_text(json.dumps(projection,indent=2,sort_keys=True)+'\n')
            altered=copy.deepcopy(initial)
            altered_desc=altered['death_mechanical_inputs']['s050_source_diagnostic']['projection']
            altered_desc.update(path=str(changed_projection.resolve()),sha256=hashlib.sha256(changed_projection.read_bytes()).hexdigest())
            with self.assertRaisesRegex(UnsupportedSourceShape,'compact projection differs'):
                m.s050_source_diagnostic_death_pins(altered,repo)
            changed_receipt=Path(td)/'receipt.json'
            receipt=json.loads(Path(nested['receipt']['path']).read_bytes());receipt['runtime_admitted']=True
            changed_receipt.write_text(json.dumps(receipt,indent=2,sort_keys=True)+'\n')
            altered=copy.deepcopy(initial)
            altered_desc=altered['death_mechanical_inputs']['s050_source_diagnostic']['receipt']
            altered_desc.update(path=str(changed_receipt.resolve()),sha256=hashlib.sha256(changed_receipt.read_bytes()).hexdigest())
            with self.assertRaisesRegex(UnsupportedSourceShape,'receipt differs from recomputed'):
                m.s050_source_diagnostic_death_pins(altered,repo)

    def test_s074_receipt_candidate_accepts_new_compiler_identity_and_rejects_drift(self):
        repo=CI.parents[1];base=repo/'T Logs/SkirmishProduction';owner=base/'s074-both-vehicle-ten-metre-authoring-20261114-r1'
        req=json.loads((owner/'authoring-request.json').read_bytes());ctxpath=base/'s074-both-vehicle-ten-metre-factory-metadata-20261114-r1/s074/source-factory-context.json'
        context=json.loads(ctxpath.read_bytes());initial={'source_config_graph':req['graph'],'setup_matrix':req['setup_matrix'],
            'typed_mission_configuration':{'layout':req['layout']},'s074_candidate_author':{}}
        for label,name in [('request','authoring-request.json'),('result','authoring-result.json')]:
            p=m.pin(owner/name);initial['s074_candidate_author'][label]={k:p[k] for k in ('path','sha256')}
        self.assertTrue(m._s074_receipt_candidate_owner_matches(initial,context))
        changed=copy.deepcopy(initial);changed['typed_mission_configuration']['layout']['sha256']='0'*64
        self.assertFalse(m._s074_receipt_candidate_owner_matches(changed,context))
        changed=copy.deepcopy(context);changed['target_context']['setup_hash']+=1
        self.assertFalse(m._s074_receipt_candidate_owner_matches(initial,changed))

    def test_s074_receipt_candidate_derives_identity_from_compiler(self):
        repo=CI.parents[1];base=repo/'T Logs/SkirmishProduction'
        bundle=json.loads((base/'s074-ten-metre-player-vehicle-source-bundle-20261114-r1/data-bundle.json').read_bytes())
        initial=json.loads(Path(bundle['initialization']['path']).read_bytes())
        owner=base/'s074-ten-metre-player-vehicle-authoring-20261114-r2'
        refs={}
        for label,name in [('request','authoring-request.json'),('result','authoring-result.json')]:
            p=m.pin(owner/name);refs[label]={k:p[k] for k in ('path','sha256')}
        initial['s074_candidate_author']=refs
        context=json.loads(Path(initial['metadata_compiler_context']['path']).read_bytes())
        self.assertTrue(m._s074_receipt_candidate_owner_matches(initial,context))
        stale=copy.deepcopy(initial);stale['setup_matrix']['sha256']='0'*64
        self.assertFalse(m._s074_receipt_candidate_owner_matches(stale,context))
        bad=copy.deepcopy(context);bad['target_context']['setup_hash']+=1
        self.assertFalse(m._s074_receipt_candidate_owner_matches(initial,bad))

    def test_s074_ten_metre_owner_context_rejects_stale_matrix(self):
        repo=CI.parents[1];base=repo/'T Logs/SkirmishProduction'
        context=base/'s074-ten-metre-player-vehicle-factory-metadata-20261114-r1/s074/source-factory-context.json'
        qualification=base/'s074-ten-metre-player-vehicle-source-qualification-20261114-r1/qualification.json'
        author=json.loads((base/'s074-ten-metre-player-vehicle-authoring-20261114-r2/authoring-request.json').read_bytes())
        cp=m.pin(context);qp=m.pin(qualification)
        initial={'mission_id':'S074','source_epoch':'OriginalCompilerDerivedNativeFactoryContext/v1',
                 'metadata_compiler_context':{k:cp[k] for k in ('path','sha256')},'configuration_qualification':{k:qp[k] for k in ('path','sha256')},'setup_matrix':author['setup_matrix']}
        self.assertTrue(m._s074_qualified_external_context(initial))
        self.assertTrue(m._s074_owner_setup_matrix_matches(initial))
        stale={**initial,'setup_matrix':{**initial['setup_matrix'],'sha256':'0'*64}}
        self.assertFalse(m._s074_qualified_external_context(stale))
        self.assertFalse(m._s074_owner_setup_matrix_matches(stale))

    def test_s074_eight_metre_owner_context_rejects_stale_matrix(self):
        repo=CI.parents[1];base=repo/'T Logs/SkirmishProduction'
        context=base/'s074-eight-metre-factory-metadata-20261114-r1/s074/source-factory-context.json'
        qualification=base/'s074-eight-metre-source-qualification-20261114-r3/qualification.json'
        author=json.loads((base/'s074-eight-metre-bound-author-20261114-r1/authoring-request.json').read_bytes())
        cp=m.pin(context);qp=m.pin(qualification)
        initial={'mission_id':'S074','source_epoch':'OriginalCompilerDerivedNativeFactoryContext/v1',
                 'metadata_compiler_context':{k:cp[k] for k in ('path','sha256')},'configuration_qualification':{k:qp[k] for k in ('path','sha256')},'setup_matrix':author['setup_matrix']}
        self.assertTrue(m._s074_qualified_external_context(initial))
        self.assertTrue(m._s074_owner_setup_matrix_matches(initial))
        stale={**initial,'setup_matrix':{**initial['setup_matrix'],'sha256':'0'*64}}
        self.assertFalse(m._s074_qualified_external_context(stale))
        self.assertFalse(m._s074_owner_setup_matrix_matches(stale))

    def test_s074_six_metre_owner_context_rejects_stale_matrix(self):
        repo=CI.parents[1];base=repo/'T Logs/SkirmishProduction'
        context=base/'s074-six-metre-factory-metadata-20261114-r4/s074/source-factory-context.json'
        qualification=base/'s074-six-metre-source-qualification-20261114-r1/qualification.json'
        author=json.loads((base/'s074-six-metre-bound-author-20261114-r1/authoring-request.json').read_bytes())
        context_pin=m.pin(context);qualification_pin=m.pin(qualification)
        initial={'mission_id':'S074','source_epoch':'OriginalCompilerDerivedNativeFactoryContext/v1',
                 'metadata_compiler_context':{k:context_pin[k] for k in ('path','sha256')},'configuration_qualification':{k:qualification_pin[k] for k in ('path','sha256')},'setup_matrix':author['setup_matrix']}
        self.assertTrue(m._s074_qualified_external_context(initial))
        self.assertTrue(m._s074_owner_setup_matrix_matches(initial))
        stale={**initial,'setup_matrix':{**initial['setup_matrix'],'sha256':'0'*64}}
        self.assertFalse(m._s074_qualified_external_context(stale))
        self.assertFalse(m._s074_owner_setup_matrix_matches(stale))

    def test_s074_origin_context_binds_owner_matrix_and_rejects_stale_selection(self):
        repo=CI.parents[1]
        bundle=json.loads((repo/'T Logs/SkirmishProduction/s074-origin-source-only-composer-20261113-r1/data-bundle.json').read_bytes())
        initial=json.loads(Path(bundle['initialization']['path']).read_bytes())
        self.assertTrue(m._s074_qualified_external_context(initial))
        self.assertTrue(m._s074_owner_setup_matrix_matches(initial))
        graph=json.loads(Path(initial['source_config_graph']['path']).read_bytes())
        stale={**initial,'setup_matrix':graph['setup_matrix']}
        self.assertFalse(m._s074_qualified_external_context(stale))
        self.assertFalse(m._s074_owner_setup_matrix_matches(stale))

    def test_s074_alias_gate_accepts_actual_join_and_rejects_unbound_flags(self):
        repo=CI.parents[1]
        join_path=repo/'T Logs/SkirmishProduction/s074-source-derived-worker-alias-join-20261107-r2/receipt.json'
        join=json.loads(join_path.read_text());package_path=repo/'T Logs/SkirmishProduction/s026-source-map-bundle-20261107-r2/map-package.json'
        package=json.loads(package_path.read_text());context_path=repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/exporter/s074/source-factory-context.json'
        qualification_path=repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/qualification/configuration-qualification.json'
        logical_path=repo/'Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset'
        physical_path=repo/'Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset'
        graph_path=Path(join['source_graph']['path']);source_build=m.pin(join['source_build']['path'])
        context_pin=m.pin(context_path);qualification_pin=m.pin(qualification_path)
        initial={'mission_id':'S074','source_epoch':'OriginalCompilerDerivedNativeFactoryContext/v1',
            'metadata_compiler_context':{k:context_pin[k] for k in ('path','sha256')},'configuration_qualification':{k:qualification_pin[k] for k in ('path','sha256')},
            'source_config_graph':m.pin(graph_path),'static_map':{'operation_map_id':'opmap.skirmish.industrial_basin',
                'definition':m.pin(logical_path),'grid_config':package['static_map']['grid_config'],'surface_data':package['static_map']['surface_data']}}
        package_pin=m.pin(package_path);inputs={'source_derived_map':{k:package_pin[k] for k in ('path','sha256')}}
        actual=m.pin(join_path)
        self.assertTrue(m._s074_qualified_external_context(initial))
        self.assertTrue(m._s074_verified_alias_join(initial,inputs,package,actual,source_build,repo))
        self.assertFalse(m._s074_needs_original_owner_mask_capture(initial,None,True))
        self.assertFalse(m._s074_needs_original_placement_inputs(None,True))
        self.assertFalse(m._s074_verified_alias_join(initial,{**inputs,'source_derived_map_alias':True},package,None,source_build,repo))
        self.assertTrue(m._s074_needs_original_owner_mask_capture(initial,None,False))
        self.assertTrue(m._s074_needs_original_placement_inputs(None,False))
        self.assertFalse(m._s074_verified_alias_join(initial,{},package,actual,source_build,repo))
        self.assertFalse(m._s074_verified_alias_join(initial,inputs,package,{**actual,'sha256':'0'*64},source_build,repo))
        wrong_qual=m.pin(repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/qualification/profile-binding-receipt.json')
        forged={**initial,'configuration_qualification':wrong_qual}
        self.assertFalse(m._s074_verified_alias_join(forged,inputs,package,actual,source_build,repo))
        self.assertTrue(m._s074_needs_original_owner_mask_capture(forged,None,False))

    def test_s074_runtime_pins_accept_actual_alias_without_native_placement_capture(self):
        repo=CI.parents[1]
        bundle_path=repo/'T Logs/SkirmishProduction/s074-source-only-composer-20261107-r2/data-bundle.json'
        bundle=json.loads(bundle_path.read_text());initial=json.loads(Path(bundle['initialization']['path']).read_text())
        author=json.loads((repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/exporter/s074/authoring-request.json').read_bytes())
        owner=json.loads((repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/candidate/receipt.json').read_bytes())
        initial['setup_matrix']=composer.validate_owner_setup_matrix(author,owner,author['setup_matrix'])
        package=json.loads(Path(bundle['source_derived_map']['path']).read_text())
        join_path=repo/'T Logs/SkirmishProduction/s074-source-derived-worker-alias-join-20261107-r2/receipt.json'
        join=json.loads(join_path.read_text());build_pin=m.pin(join['source_build']['path'])
        policy={'schema_version':1,'kind':'SkirmishSourceCameraPolicy','mission':'S074','mode':'NoCameraLogicalDeliveryDiagnostic',
            'configuration_qualification':initial['configuration_qualification'],'source_build_receipt':build_pin,
            'build_provenance':{'trusted':True,'experimental':True,'canonical':False,'experimental_proofs':[{'fixture':'actual-alias-control'}]}}
        profiles= m.owned.require('skirmish_source_profiles')
        with patch.object(profiles,'runtime_backend',return_value='TypedGroundS026V1'),patch.object(profiles,'configuration_qualification',return_value=initial['configuration_qualification']):
            pins=m.runtime_input_pins(initial,bundle,repo,{},repo,package,m.pin(join_path),build_pin,policy)
        self.assertIn(str(Path(initial['metadata_compiler_context']['path']).resolve()),pins)
        self.assertIn(str(Path(initial['unit_production_metadata']['path']).resolve()),pins)
        self.assertNotIn('navigation_reference',initial)
        self.assertEqual(initial['placement_domain'],{})
        with patch.object(profiles,'runtime_backend',return_value='TypedGroundS026V1'),patch.object(profiles,'configuration_qualification',return_value=initial['configuration_qualification']):
            with self.assertRaisesRegex(ValueError,'Verified source-map alias join or original navigation reference required'):
                m.runtime_input_pins(initial,bundle,repo,{},repo,package,None,build_pin,policy)

    def test_s074_portable_logical_binding_projection_is_exact_and_fail_closed(self):
        repo=CI.parents[1];projection_dir=repo/'T Logs/SkirmishProduction/s074-declared-logical-binding-portable-20261107-r6'
        wrapper_path=projection_dir/'logical-binding.json';receipt=json.loads((projection_dir/'receipt.json').read_bytes())
        wrapper=json.loads(wrapper_path.read_bytes());context_path=repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/exporter/s074/source-factory-context.json'
        source_binding=repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/candidate/mission-logical-binding.json'
        target=json.loads(context_path.read_bytes())['target_context']
        expected={'logical_binding_projection':receipt['logical_binding'],'logical_binding_owner':wrapper['source_owner_binding']}
        self.assertEqual(composer.validate_logical_binding_projection(wrapper,receipt,wrapper_path,source_binding,context_path,target),expected)
        cases=[]
        bad=copy.deepcopy(wrapper);bad['metadata_compiler_context']['sha256']='0'*64;cases.append((bad,receipt))
        bad=copy.deepcopy(wrapper);bad['target_context']['setup_hash']+=1;cases.append((bad,receipt))
        bad=copy.deepcopy(wrapper);bad['native_capture']=True;cases.append((bad,receipt))
        bad=copy.deepcopy(wrapper);bad['portable_binding']['float_encoding']='json-decimal';cases.append((bad,receipt))
        bad=copy.deepcopy(wrapper);bad['source_owner_binding']['sha256']='0'*64;cases.append((bad,receipt))
        bad_receipt=copy.deepcopy(receipt);bad_receipt['logical_binding']['sha256']='0'*64;cases.append((wrapper,bad_receipt))
        for candidate, candidate_receipt in cases:
            with self.subTest(kind=candidate.get('kind'),receipt=candidate_receipt),self.assertRaises(ValueError):
                composer.validate_logical_binding_projection(candidate,candidate_receipt,wrapper_path,source_binding,context_path,target)

    def test_s074_effective_setup_matrix_comes_from_original_authoring_request(self):
        repo=CI.parents[1]
        author=json.loads((repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/exporter/s074/authoring-request.json').read_bytes())
        graph=json.loads((repo/'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json').read_bytes())
        owner=json.loads((repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/candidate/receipt.json').read_bytes())
        selected=composer.validate_owner_setup_matrix(author,owner,author['setup_matrix'])
        self.assertEqual(selected['path'],str((repo/'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/experimental-opening-mix-matrix.csv').resolve()))
        self.assertEqual(selected['sha256'],'9661befa60717d575c78571e28761c8d9e310d8b9c2b45874a3a7b92dbdebd22')
        self.assertNotEqual(selected,graph['setup_matrix'])
        with self.assertRaisesRegex(ValueError,'exact original S074 authoring-request descriptor'):
            composer.validate_owner_setup_matrix(author,owner,graph['setup_matrix'])

    def test_s074_runtime_setup_matrix_is_bound_to_owner_request(self):
        repo=CI.parents[1]
        bundle=json.loads((repo/'T Logs/SkirmishProduction/s074-source-only-composer-20261107-r2/data-bundle.json').read_bytes())
        initial=json.loads(Path(bundle['initialization']['path']).read_bytes())
        author=json.loads((repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/exporter/s074/authoring-request.json').read_bytes())
        graph=json.loads((repo/'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json').read_bytes())
        owner=json.loads((repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/candidate/receipt.json').read_bytes())
        actual={**initial,'setup_matrix':composer.validate_owner_setup_matrix(author,owner,author['setup_matrix'])}
        self.assertTrue(m._s074_owner_setup_matrix_matches(actual))
        self.assertFalse(m._s074_owner_setup_matrix_matches({**actual,'setup_matrix':graph['setup_matrix']}))
        join_path=repo/'T Logs/SkirmishProduction/s074-source-derived-worker-alias-join-20261107-r2/receipt.json'
        join=json.loads(join_path.read_bytes());package=json.loads(Path(bundle['source_derived_map']['path']).read_bytes())
        build_pin=m.pin(join['source_build']['path'])
        policy={'schema_version':1,'kind':'SkirmishSourceCameraPolicy','mission':'S074','mode':'NoCameraLogicalDeliveryDiagnostic',
            'configuration_qualification':actual['configuration_qualification'],'source_build_receipt':build_pin,
            'build_provenance':{'trusted':True,'experimental':True,'canonical':False,'experimental_proofs':[{'fixture':'actual-owner-matrix-control'}]}}
        profiles=m.owned.require('skirmish_source_profiles')
        with patch.object(profiles,'runtime_backend',return_value='TypedGroundS026V1'),patch.object(profiles,'configuration_qualification',return_value=actual['configuration_qualification']):
            m.runtime_input_pins(actual,bundle,repo,{},repo,package,m.pin(join_path),build_pin,policy)
        with patch.object(profiles,'runtime_backend',return_value='TypedGroundS026V1'),patch.object(profiles,'configuration_qualification',return_value=actual['configuration_qualification']):
            with self.assertRaisesRegex(ValueError,'S074 setup matrix differs from exact owner-compiled authoring request'):
                m.runtime_input_pins({**actual,'setup_matrix':graph['setup_matrix']},bundle,repo,{},repo,package,m.pin(join_path),build_pin,policy)

    def test_s074_native_factory_descriptors_are_exact_context_pair_and_origin_pins(self):
        repo=CI.parents[1]
        context_path=repo/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/exporter/s074/source-factory-context.json'
        context=json.loads(context_path.read_bytes())
        metadata,origin=composer.validate_native_factory_origin(context,context['native_factory_reference'],context['native_origin_descriptor'])
        self.assertEqual(metadata,context['native_factory_reference'])
        self.assertEqual(origin,context['native_origin_descriptor'])
        for bad_metadata,bad_origin in [({**metadata,'sha256':'0'*64},origin),(metadata,{**origin,'sha256':'0'*64})]:
            with self.assertRaises(ValueError):composer.validate_native_factory_origin(context,bad_metadata,bad_origin)
        build=json.loads((repo/'T Logs/SkirmishProduction/source-map-alias-s074-build-20261107-r12/build.json').read_bytes())
        initial={'metadata_compatibility':metadata,'metadata_source_origin':origin}
        pins=m.metadata_origin_pins(initial,repo,build)
        self.assertEqual(pins[metadata['path']],metadata['sha256'])
        self.assertEqual(pins[origin['path']],origin['sha256'])
        with self.assertRaisesRegex(ValueError,'Native origin artifact binding differs'):
            m.metadata_origin_pins({**initial,'metadata_compatibility':{**metadata,'sha256':'0'*64}},repo,build)

    def test_s074_alias_runtime_requires_validated_bounded_no_camera_policy(self):
        repo=CI.parents[1]
        bundle=json.loads((repo/'T Logs/SkirmishProduction/s074-both-vehicle-generic-source-only-bundle-20261114-r2/data-bundle.json').read_bytes())
        initial=json.loads(Path(bundle['initialization']['path']).read_bytes())
        package=json.loads(Path(bundle['source_derived_map']['path']).read_bytes())
        build_path=repo/'T Logs/SkirmishProduction/s074-current-host-build-20261116-r2/build.json'
        build=json.loads(build_path.read_bytes());build_pin=m.pin(build_path)
        profiles=m.owned.require('skirmish_source_profiles')
        with patch.object(profiles,'_EXTERNAL',None):
            profiles.bind_configuration_qualification(str(repo/'T Logs/SkirmishProduction/s074-both-vehicle-ten-metre-source-qualification-20261114-r1/qualification.json'))
            provenance=m.source_build_provenance(build)
        self.assertTrue(provenance['trusted'])
        self.assertTrue(provenance['experimental'])
        self.assertFalse(provenance['canonical'])
        self.assertGreater(len(provenance['experimental_proofs']),0)
        policy=m.source_camera_policy_selection(bundle,initial,provenance,600,build_pin)
        self.assertTrue(m._s074_alias_runtime_admitted(repo,initial,bundle,package,provenance,600,build_pin,policy))
        self.assertFalse(m._s074_alias_runtime_admitted(repo,initial,bundle,package,provenance,600,build_pin,None))
        forged={**policy,'build_provenance':{**policy['build_provenance'],'trusted':False}}
        self.assertFalse(m._s074_alias_runtime_admitted(repo,initial,bundle,package,provenance,600,build_pin,forged))
        self.assertFalse(m._s074_alias_runtime_admitted(repo,initial,bundle,package,provenance,None,build_pin,policy))
        unbounded={**policy,'build_provenance':{**policy['build_provenance'],'experimental_proofs':[]}}
        self.assertFalse(m._s074_alias_runtime_admitted(repo,initial,bundle,package,provenance,600,build_pin,unbounded))
        no_policy_bundle={**bundle};no_policy_bundle.pop('source_camera_policy')
        self.assertFalse(m._s074_alias_runtime_admitted(repo,initial,no_policy_bundle,package,provenance,600,build_pin,None))
    def test_declared_logical_binding_compiler_failure_reports_observed_values_without_relaxing_gate(self):
        source=(CI/'SkirmishSourceSimulation/Adapters/SourceDeclaredMissionLogicalBindingReference.cs').read_text()
        for field in ('target_setup_hash','actual_setup_hash','target_seed','actual_seed','target_catalog_id','actual_catalog_id',
                      'compile_attempted','compile_succeeded','compile_error','target_logical_fingerprint',
                      'setup_logical_fingerprint','compiled_logical_fingerprint','setup_hash_matches','seed_matches',
                      'catalog_matches','setup_fingerprint_matches','target_fingerprint_matches'):
            self.assertIn(f'["{field}"]',source)
        gate=source.split('if(!setupHashMatches',1)[1].split('var observed=',1)[0]
        for required in ('!seedMatches','!catalogMatches','!compileSucceeded','!setupFingerprintMatches','!targetFingerprintMatches'):
            self.assertIn(required,gate)
        self.assertIn('compileSucceeded=SkirmishMissionLogicalBindingCompiler.TryCompile(definition,setup,out logical,out error)',source)

    def test_s074_no_camera_policy_is_explicit_trusted_bounded_and_fail_closed(self):
        qualification={'schema_version':1,'kind':'S074ConfigurationQualification','identity':'fixture-only'}
        policy={'schema_version':1,'kind':'SkirmishSourceCameraPolicy','mission':'S074','mode':'NoCameraLogicalDeliveryDiagnostic','configuration_qualification':qualification}
        provenance={'trusted':True,'experimental':True,'canonical':False,'reason':'ExperimentalGenerator','manifest_sha256':'a'*64,'resolution_pin':{'path':'/tmp/source-resolution.json','sha256':'b'*64,'bytes':1},'experimental_proofs':[{'generator':'fixture','generator_sha256':'c'*64,'output':'fixture.cs','output_sha256':'d'*64}]}
        selected=m.source_camera_policy_selection({'mission':'S074','source_camera_policy':policy},{'mission_id':'S074','configuration_qualification':qualification},provenance,600,{'path':'/tmp/build-receipt.json','sha256':'e'*64,'bytes':2})
        self.assertEqual(selected['mode'],'NoCameraLogicalDeliveryDiagnostic');self.assertTrue(selected['build_provenance']['trusted']);self.assertTrue(selected['build_provenance']['experimental']);self.assertFalse(selected['build_provenance']['canonical']);self.assertEqual(selected['configuration_qualification'],qualification)
        cases=[({'mission':'S002','source_camera_policy':policy},{'mission_id':'S074','configuration_qualification':qualification},provenance,600),({'mission':'S074','source_camera_policy':policy},{'mission_id':'S074','configuration_qualification':{'other':'qualification'}},provenance,600),({'mission':'S074','source_camera_policy':policy,'camera_values':{'path':'ignored'}},{'mission_id':'S074','configuration_qualification':qualification},provenance,600),({'mission':'S074','source_camera_policy':policy,'mission_scene':{'path':'caller-scene'}},{'mission_id':'S074','configuration_qualification':qualification},provenance,600),({'mission':'S074','source_camera_policy':policy},{'mission_id':'S074','configuration_qualification':qualification},{**provenance,'trusted':False},600),({'mission':'S074','source_camera_policy':policy},{'mission_id':'S074','configuration_qualification':qualification},provenance,None)]
        for inputs,initial,build,ticks in cases:
            with self.subTest(inputs=inputs,initial=initial,ticks=ticks),self.assertRaises(ValueError):m.source_camera_policy_selection(inputs,initial,build,ticks,{'path':'/tmp/build-receipt.json','sha256':'e'*64,'bytes':2})
    def test_s074_no_camera_binds_canonical_match_scene_source_pin(self):
        with tempfile.TemporaryDirectory() as temporary:
            data_repo=Path(temporary);scene=data_repo/'Assets/Game/Scenes/Match.unity';scene.parent.mkdir(parents=True);scene.write_bytes(b'actual scene source fixture')
            expected={'path':str(scene.resolve()),'sha256':hashlib.sha256(scene.read_bytes()).hexdigest(),'bytes':scene.stat().st_size}
            request={'source_camera_policy':{'mode':'NoCameraLogicalDeliveryDiagnostic'}}
            # The scene binding is independent of camera mode and uses only the canonical data root.
            request['mission_scene']=m.source_mission_scene_pin(data_repo)
            self.assertEqual(request['mission_scene'],expected)
            self.assertNotIn('camera_profile',request)
            scene.write_bytes(b'changed scene source')
            with self.assertRaisesRegex(ValueError,'Input changed:'):
                m.verify({expected['path']:expected['sha256']})
    def test_s074_no_camera_host_preserves_actual_death_owner_and_default_camera_path(self):
        host=(CI/'SkirmishSourceSimulation/Adapters/DeclaredGround/SourceSimulationProgram.cs').read_text()
        world=(CI/'SkirmishSourceSimulation/Adapters/S026/000-000_AutonomousWorld.cs').read_text()
        death_readset=(CI/'SkirmishSourceSimulation/Adapters/S026/SourceNativeDeathReadModel.cs').read_text()
        source_build=(CI/'skirmish_source_build.py').read_text()
        death=(CI.parents[1]/'Assets/Tests/Editor/UnitDeathLifecycleKernelTests.cs').read_text()
        self.assertIn('SourceS002NumericCameraProvider camera=noCameraDiagnostic?null:SourceS002NumericCameraProvider.CreateGenerated',host)
        self.assertIn('SourceHelicopterDelivery.WithLogicalBasis',host)
        self.assertIn('camera?.AdvanceSourceUnscaledTime',host)
        self.assertIn('world.AdvanceConstructedSourceOwners()',host)
        self.assertIn('SourceNativeDeathReadModel.LoadS074NoCameraDiagnostic',world)
        self.assertIn('GeneratedReferenceTemplateView.VerifyS074SourcePins(pins,captureRoot,8091)',death_readset)
        self.assertIn('original["actual_original_prepare_bits"]',death_readset)
        self.assertIn('death_readset_session',host)
        self.assertIn("death_readback.get('raw_sha256')=='5e1bf6de50b045dd6f5a380a63aa1dcfcfcba2c13d33a3fcf4eb3d94374f3664'",source_build)
        self.assertIn('world.GetOrCreateSystem<UnitDeathSystem>()',death)
        self.assertIn('same owner manager must exercise true snapshot absence',death)
        self.assertIn('validSnapshot=Finalize',death)
        self.assertIn('[S074NoCameraDeathOwnerControl] result=Passed',death)
    def test_source_resource_bindings_are_exactly_pinned_and_detect_drift(self):
        self.assertEqual(m.source_resource_binding_input_pins({}),{})
        with tempfile.TemporaryDirectory() as temporary:
            path=(Path(temporary)/'resources.json').resolve();path.write_text('[]')
            digest=hashlib.sha256(path.read_bytes()).hexdigest()
            valid={'source_resource_bindings':{'path':str(path),'sha256':digest}}
            pins=m.source_resource_binding_input_pins(valid);self.assertEqual(pins,{str(path):digest})
            for bad in (None,{}, {'path':str(path),'sha256':'0'*64}, {'path':str(path),'sha256':digest,'extra':True}, {'path':'relative.json','sha256':digest}):
                with self.subTest(descriptor=bad),self.assertRaises(ValueError):m.source_resource_binding_input_pins({'source_resource_bindings':bad})
            path.write_text('[{}]')
            with self.assertRaises(ValueError):m.verify(pins)
    def test_layout_replay_input_is_exactly_pinned_and_detects_later_drift(self):
        self.assertEqual(m.layout_replay_input_pins({'mission_id':'S004'}),{})
        with tempfile.TemporaryDirectory() as temporary:
            path=(Path(temporary)/'capture.json').resolve();path.write_text('{"layout":1}')
            digest=hashlib.sha256(path.read_bytes()).hexdigest()
            valid={'layout_json_replay':{'path':str(path),'sha256':digest}}
            self.assertEqual(m.layout_replay_input_pins(valid),{str(path):digest})
            for bad in (None,{}, {'path':str(path),'sha256':'0'*64},
                        {'path':str(path),'sha256':digest,'extra':True},
                        {'path':'relative.json','sha256':digest}):
                with self.subTest(descriptor=bad),self.assertRaises(ValueError):
                    m.layout_replay_input_pins({'layout_json_replay':bad})
            pins=m.layout_replay_input_pins(valid);path.write_text('{"layout":2}')
            with self.assertRaises(ValueError):m.verify(pins)
    def test_s073_shell_owner_flag_requires_derived_profile_and_pins_child_admission(self):
        with tempfile.TemporaryDirectory() as temporary:
            manifest_path=Path(temporary)/'decision.json'
            manifest={'compiler_defines':['WARLINE_SOURCE_S073_UI_SHELL_OWNER_PROFILE'],
                'experimental_source_profile':{'id':'S073UiShellStateOwnerOnCreateV1_NonNative','scope':'Diagnostic-only non-native source host'}}
            manifest_path.write_text(json.dumps(manifest))
            raw=manifest_path.read_bytes()
            receipt={'source_manifest':{'path':str(manifest_path),'sha256':hashlib.sha256(raw).hexdigest(),'bytes':len(raw)}}
            m._validate_s073_ui_shell_state_owner_diagnostic(False,{})
            m._validate_s073_ui_shell_state_owner_diagnostic(True,receipt)
            with self.assertRaises(UnsupportedSourceShape):m._validate_s073_ui_shell_state_owner_diagnostic(True,{})
            with self.assertRaises(UnsupportedSourceShape):
                m._validate_s073_ui_shell_state_owner_diagnostic(True,{'source_manifest':{'path':str(manifest_path),'sha256':hashlib.sha256(raw).hexdigest()}})
            bad_bytes={'source_manifest':dict(receipt['source_manifest'],bytes=len(raw)+1)}
            with self.assertRaises(UnsupportedSourceShape):m._validate_s073_ui_shell_state_owner_diagnostic(True,bad_bytes)
            manifest['compiler_defines']=[];manifest_path.write_text(json.dumps(manifest))
            raw=manifest_path.read_bytes();receipt['source_manifest']['sha256']=hashlib.sha256(raw).hexdigest();receipt['source_manifest']['bytes']=len(raw)
            with self.assertRaises(UnsupportedSourceShape):m._validate_s073_ui_shell_state_owner_diagnostic(True,receipt)
        build_source=(CI/'skirmish_source_build.py').read_text()
        self.assertIn("env.pop('WARLINE_S073_UI_SHELL_OWNER_DIAGNOSTIC',None)",build_source)
        self.assertIn("env['WARLINE_S073_UI_SHELL_OWNER_DIAGNOSTIC']='1'",build_source)
        self.assertIn("'s073_ui_shell_state_owner_diagnostic':s073_ui_shell_state_owner_diagnostic",build_source)
        worker=(CI/'SkirmishSourceSimulation/Recipes/s073_ui_shell_state_owner/generate.py').read_text()
        self.assertIn('s073_ui_shell_state_owner_diagnostic',worker)
        self.assertIn('WARLINE_S073_UI_SHELL_OWNER_DIAGNOSTIC',worker)

    def test_s073_source_transport_diagnostic_requires_both_opt_ins_and_clears_stale_environment(self):
        with self.assertRaises(UnsupportedSourceShape):
            m._validate_s073_source_transport_diagnostic(False, True, True)
        with self.assertRaises(UnsupportedSourceShape):
            m._validate_s073_source_transport_diagnostic(True, False, True)
        m._validate_s073_source_transport_diagnostic(False, False, False)
        build_source=(CI/'skirmish_source_build.py').read_text()
        self.assertIn("env.pop('WARLINE_S073_SOURCE_TRANSPORT_DIAGNOSTIC',None)",build_source)
        self.assertIn("env['WARLINE_S073_SOURCE_TRANSPORT_DIAGNOSTIC']='1'",build_source)
        worker=(CI/'SkirmishSourceSimulation/Adapters/SourceS073LegacyNormalRun.cs').read_text()
        self.assertIn('sourceTransportDiagnosticRequested != sourceTransportDiagnosticEnvironmentOptIn',worker)

    def test_s073_seven_case_admission_uses_receipt_owner_keys(self):
        receipt_path=CI.parents[1]/'Logs/SkirmishProduction/s073-reserve-mask-cells-20261107-r1/receipt.json'
        receipt=json.loads(receipt_path.read_bytes())
        self.assertEqual(receipt['kind'],'SourceHostOriginalReserveMaskVsS073ProjectionAdapter')
        self.assertEqual(receipt['status'],'Passed')
        self.assertEqual(receipt['compile_exit'],0)
        self.assertEqual(receipt['run_exit'],0)
        self.assertIs(receipt['runtime_executed'],True)
        self.assertIs(receipt['mask_comparison_pass_marker'],True)
        self.assertEqual(receipt['captured_bound_count'],90865)
        self.assertEqual(receipt['grid_mask_cells'],2048*1024)
        self.assertEqual(receipt['captured_projection_sha256'],'8b5087c8a96ad81113c93fb4fba1632f10c8920bd2ed16fcb168e58d22572065')
        self.assertEqual(receipt['scope'],'Historical pregame captured bounds; no provider parity, mission admission or readiness claim')
        run_log=(receipt_path.parent/'run.log').read_text()
        self.assertIn('result=Passed input_bounds=90865 preferred_count=7 total_reserved=148760 mask_cells=2097152 differing_cells=0 provider_parity=false mission_ready=false',run_log)
        hashes=receipt['source_hashes']
        self.assertTrue({'reserve_owner','collector_owner','provider_contract','grid_utils'} <= set(hashes))
        expected={
            'reserve_owner':'Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs',
            'collector_owner':'Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs',
            'provider_contract':'Assets/Game/Scripts/Rendering/Contracts/IRenderBoundsPresentation.cs',
            'grid_utils':'Assets/Game/Scripts/Components/GridComponents.cs',
        }
        repo=CI.parents[1]
        for key,relative in expected.items():
            self.assertEqual(hashes[key],hashlib.sha256((repo/relative).read_bytes()).hexdigest())
        admission=(CI/'SkirmishSourceSimulation/Adapters/SourceS073CapturedClearanceAdmission.cs').read_text()
        for key in expected:
            self.assertIn(f'VerifyReceiptSourceHash(root, sourceHashes, "{key}"',admission)

    def test_derived_transport_manifest_is_explicitly_diagnostic(self):
        path=CI.parents[1]/'Logs/SkirmishProduction/s073-camera-transport-diagnostic-inputs-20261103-r1/decision.json'
        manifest=json.loads(path.read_bytes())
        self.assertEqual(manifest['source_diagnostic_classification']['class'],'ExplicitS073SourceHostTransportDiagnostic')
        self.assertIs(manifest['source_diagnostic_classification']['canonical_manifest'],False)
        self.assertIs(manifest['source_diagnostic_classification']['mission_ready'],False)
        counts={ident:sum(source.get('id')==ident for source in manifest['sources']) for ident in ('SourceS073CapturedClearanceProjection','SourceS073CapturedClearanceAdmission')}
        self.assertEqual(counts,{'SourceS073CapturedClearanceProjection':1,'SourceS073CapturedClearanceAdmission':1})

    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.base=Path(self.temp.name).resolve()
        self.repo=self.base/'repo';self.repo.mkdir();self.recorded=self.base/'recorded-runtime'
        self.data_repo=self.base/'data-repo';self.data_repo.mkdir()
        for repository in [self.repo,self.data_repo]:
            for relative,value in [('Assets/Game/Scenes/Match.unity','CURRENT_SCENE_PROTOCOL_FIXTURE'),('Assets/Game/Resources/SkirmishMissionViewProfile_S002.asset','CURRENT_PROFILE_PROTOCOL_FIXTURE'),('Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv','MATRIX_PROTOCOL_FIXTURE')]:
                asset=repository/relative;asset.parent.mkdir(parents=True,exist_ok=True);asset.write_text(value)

        self.contents=self.recorded/'Contents';self.script=self.contents/'Resources/Scripting';self.engine=self.script/'Managed/UnityEngine'
        self.engine.mkdir(parents=True);self.mono=self.script/'MonoBleedingEdge/bin/mono';self.mono.parent.mkdir(parents=True);self.mono.write_bytes(b'PROTOCOL_MONO_PATH_ONLY')
        config=self.script/'MonoBleedingEdge/etc/mono/config';config.parent.mkdir(parents=True);config.write_text('<configuration><dllmap target="libMonoPosixHelper.dylib" /></configuration>')
        self.helper=self.contents/'Frameworks/libMonoPosixHelper.dylib';self.helper.parent.mkdir(parents=True);self.helper.write_bytes(b'PROTOCOL_HELPER_ONLY')
        self.newton=self.base/'newtonsoft/Newtonsoft.Json.dll';self.newton.parent.mkdir();self.newton.write_bytes(b'PROTOCOL_REFERENCE_ONLY')
        self.exe=self.base/'build/SourceSimulation.exe';self.exe.parent.mkdir();self.exe.write_bytes(b'PROTOCOL_NOT_EXECUTABLE')
        self.placement=self.base/'build/placement/build.json';self.placement.parent.mkdir();self.placement.write_text('{}')
        self.source=self.base/'CurrentPolicy.cs';self.source.write_text('class CurrentPolicy {}')
        self.profile={'mission':'S002','seed':104731,'numeric_runtime':'InstalledMono'}
        self.manifest=self.repo/'Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json';self.manifest.parent.mkdir(parents=True);self.manifest.write_text(json.dumps({'schema_version':1,'profile':self.profile,'compiler_defines':[]})+'\n')
        self.resolution=self.base/'build/source/resolution.json';self.resolution.parent.mkdir(parents=True)
        self.resolution.write_text(json.dumps({'schema_version':1,'kind':'SkirmishSourceResolution','status':'Resolved','runtime_backend':'LegacyGroundS002V1','profile':self.profile,'pins':{str(self.manifest):m.sha(self.manifest.read_bytes())},'resolved':[],'unresolved':[]}))
        manifest_pin=m.pin(self.manifest);resolution_pin=m.pin(self.resolution)
        self.pins={str(p.resolve()):m.sha(p.read_bytes())for p in [self.mono,self.newton,self.source]}
        self.pins.update({manifest_pin['path']:manifest_pin['sha256'],resolution_pin['path']:resolution_pin['sha256']})
        self.receipt={'runtime_backend':'LegacyGroundS002V1','schema_version':1,'kind':'SkirmishSourceSimulationBuild','status':'Passed','mission':'S002','seed':104731,'repository_root':str(self.repo),'package_root':str(self.repo),'source_and_reference_pins':dict(self.pins),'source_manifest':manifest_pin,'canonical_source_manifest':manifest_pin,'source_resolution':resolution_pin['path'],'source_resolution_pin':resolution_pin,'executable':m.pin(self.exe),'placement_build':str(self.placement),'runtime_selection':{k:str(v)for k,v in {'contents':self.contents,'script':self.script,'engine':self.engine,'mono':self.mono,'newtonsoft':self.newton}.items()}}
        self.build=self.base/'build.json';self.save_build()
        self.inputs={'schema_version':1,'mission':'S002','seed':104731}
        matrix=self.base/'fixture-matrix.csv';matrix.write_text('MATRIX_PROTOCOL_FIXTURE')
        initial={'mission_id':'S002','selection':{'seed':104731,'difficulty_id':2,'size_id':1},'setup_matrix':{'path':str(matrix),'sha256':m.sha(matrix.read_bytes())}}
        for name in ['initialization','unit_metadata','static_clearance','camera_values','camera_motion']:
            path=self.base/(name+'.data');path.write_bytes(json.dumps(initial).encode()if name=='initialization'else('PROTOCOL_DATA_ONLY:'+name).encode());self.inputs[name]=m.pin(path)
        self.bundle=self.base/'bundle.json';self.bundle.write_text(json.dumps(self.inputs));self.children=[]
    def save_build(self):self.build.write_text(json.dumps(self.receipt))
    def bind_sparse_experimental_source_manifest(self,with_proof=True):
        alternate=self.repo/'alternate-sparse-manifest.json';alternate.write_text(json.dumps({'schema_version':1,'profile':self.profile,'compiler_defines':[],'scope':'Opt-in sparse source experiment'})+'\n')
        generator=self.repo/'Tools/CI/SkirmishSourceSimulation/Recipes/sparse_acquisition/generate.py';generator.parent.mkdir(parents=True,exist_ok=True);generator.write_text('# sparse generator protocol fixture\n')
        source=self.repo/'Assets/Game/Scripts/Systems/TargetAcquisitionKernel.cs';source.parent.mkdir(parents=True,exist_ok=True);source.write_text('class TargetAcquisitionKernel {}\n')
        generated=self.resolution.parent/'generated/SparseTargetAcquisitionKernel.cs';generated.parent.mkdir(parents=True,exist_ok=True);generated.write_text('class SparseTargetAcquisitionKernel {}\n')
        source_manifest_pin=m.pin(alternate);generator_pin=m.pin(generator);source_pin=m.pin(source);output_pin=m.pin(generated)
        resolver_pins={source_manifest_pin['path']:source_manifest_pin['sha256']}
        resolved=[]
        if with_proof:
            resolver_pins.update({generator_pin['path']:generator_pin['sha256'],source_pin['path']:source_pin['sha256']})
            detail={'experimental_only':True,'source_pins':{source_pin['path']:source_pin['sha256']},'outputs':{generated.name:output_pin['sha256']}}
            resolved=[{'id':'sparse_kernel','kind':'current_original','generated':generated.name,'path':str(generated),'sha256':output_pin['sha256'],'proof':[{'generator':generator_pin['path'],'generator_sha256':generator_pin['sha256'],'generation_proof':detail}]}]
        resolution={'schema_version':1,'kind':'SkirmishSourceResolution','status':'Resolved','runtime_backend':'LegacyGroundS002V1','profile':self.profile,'pins':resolver_pins,'resolved':resolved,'unresolved':[]}
        self.resolution.write_text(json.dumps(resolution,sort_keys=True,separators=(',',':'))+'\n')
        self.receipt['source_manifest']=source_manifest_pin;self.receipt['source_resolution']=str(self.resolution);self.receipt['source_resolution_pin']=m.pin(self.resolution)
        self.receipt['source_and_reference_pins'].update({source_manifest_pin['path']:source_manifest_pin['sha256'],self.receipt['source_resolution_pin']['path']:self.receipt['source_resolution_pin']['sha256']})
        if with_proof:self.receipt['source_and_reference_pins'].update({generator_pin['path']:generator_pin['sha256'],source_pin['path']:source_pin['sha256'],output_pin['path']:output_pin['sha256']})
        self.save_build()
    def bind_identity_fixture(self,mission,backend,profile,seed,variants=None):
        manifest=m.canonical_source_manifest(self.repo,mission);manifest.parent.mkdir(parents=True,exist_ok=True)
        manifest.write_text(json.dumps({'schema_version':1,'profile':profile,'compiler_defines':[],**({'source_profile_variants':variants}if variants is not None else {})},sort_keys=True)+'\n')
        manifest_pin=m.pin(manifest)
        resolution={'schema_version':1,'kind':'SkirmishSourceResolution','status':'Resolved','runtime_backend':backend,'profile':profile,
                    'pins':{manifest_pin['path']:manifest_pin['sha256']},'resolved':[],'unresolved':[]}
        self.resolution.write_text(json.dumps(resolution,sort_keys=True,separators=(',',':'))+'\n');resolution_pin=m.pin(self.resolution)
        self.receipt.update(mission=mission,seed=seed,runtime_backend=backend,source_manifest=manifest_pin,
            canonical_source_manifest=manifest_pin,source_resolution=resolution_pin['path'],source_resolution_pin=resolution_pin)
        self.receipt['source_and_reference_pins'].update({manifest_pin['path']:manifest_pin['sha256'],resolution_pin['path']:resolution_pin['sha256']})
        self.save_build()
    def invoke(self,result=None,exit_code=0,max_ticks=None,timeout=10,kill_required=False,child_timeout=False,manifests=True,on_exit=None,data_repo=None):
        def create(command,**kwargs):
            child=ProtocolProcess(command,source_result=result,returncode=exit_code,timeout=child_timeout,kill_required=kill_required,manifests=manifests,on_exit=on_exit,**kwargs);self.children.append(child);return child
        out=self.base/'run'
        with patch.object(m.subprocess,'Popen',side_effect=create)as factory,patch.object(m,'runtime_paths',side_effect=AssertionError('Runtime rediscovery is forbidden')):
            receipt=m.run(self.repo,self.build,self.bundle,out,max_ticks=max_ticks,timeout=timeout,data_repo=data_repo)
        self.assertEqual(factory.call_count,1);return receipt
    def assert_failed(self,result):
        self.assertEqual(result['status'],'Failed');self.assertFalse(result['validation_passed']);self.assertFalse(result['full_outcome_observed']);self.assertFalse(result['diagnostic_prefix_completed'])
    def test_conflicting_epoch_rejected_without_overwriting_original_pin(self):
        pins=dict(self.pins);old=pins[str(self.source)]
        with self.assertRaisesRegex(UnsupportedSourceShape,'Conflicting source/runtime epoch'):m.merge_pins(pins,{str(self.source):m.sha(b'OTHER_EPOCH')})
        self.assertEqual(pins[str(self.source)],old)
    def test_existing_source_drift_rejected_before_generated_pin_merge(self):
        pins=dict(self.pins);self.source.write_bytes(b'new policy epoch')
        with self.assertRaisesRegex(UnsupportedSourceShape,'Input changed:'):m.merge_pins(pins,{str(self.source):m.sha(self.source.read_bytes())})
        self.assertEqual(pins,self.pins)
    def test_new_generated_pin_drift_rejected(self):
        generated=self.base/'Generated.cs';generated.write_bytes(b'class Generated{}')
        with self.assertRaisesRegex(UnsupportedSourceShape,'Input changed before merge'):m.merge_pins({}, {str(generated):m.sha(b'wrong generated bytes')})
    def test_equivalent_absolute_pin_merge_keeps_one_epoch(self):
        pins=dict(self.pins);m.merge_pins(pins,{str(self.source.parent/'.'/self.source.name):self.pins[str(self.source)]});self.assertEqual(pins,self.pins)
    def test_json_pin_hashes_exact_parsed_bytes_and_later_recheck_catches_race(self):
        path=self.base/'race.json';raw=b'{ "epoch" : 1 }\n';path.write_bytes(raw);loads=json.loads
        def mutate_after_parse(value):
            result=loads(value);path.write_bytes(b'{"epoch":2}');return result
        with patch.object(m.json,'loads',side_effect=mutate_after_parse):value,pin=m.read_json_pinned(path)
        self.assertEqual(value,{'epoch':1});self.assertEqual(pin['sha256'],hashlib.sha256(raw).hexdigest());self.assertEqual(pin['bytes'],len(raw))
        with self.assertRaisesRegex(UnsupportedSourceShape,'Input changed before merge'):m.merge_pins({}, {pin['path']:pin['sha256']})
    def test_run_uses_only_build_recorded_runtime_and_exact_command(self):
        result=self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},data_repo=self.data_repo)
        request=json.loads((self.base/'run/run-request.json').read_bytes());self.assertEqual(request['source_repository_root'],str(self.repo));self.assertEqual(request['repository_root'],str(self.data_repo));self.assertEqual(request['mission_scene'],m.pin(self.data_repo/'Assets/Game/Scenes/Match.unity'));self.assertEqual(request['camera_profile'],m.pin(self.data_repo/'Assets/Game/Resources/SkirmishMissionViewProfile_S002.asset'));self.assertEqual(result['source_repository_root'],str(self.repo));self.assertEqual(result['data_repository_root'],str(self.data_repo))
        child=self.children[0];self.assertEqual(child.command[0],str(self.mono.resolve()));self.assertEqual(child.command[1],'--config');self.assertEqual(child.command[3],'--optimize=-float32');self.assertEqual(child.command[4],str(self.exe));self.assertIn(str(self.engine),child.env['MONO_PATH'])
        self.assertTrue(result['validation_passed']);self.assertTrue(result['full_outcome_observed']);self.assertFalse(result['native_acceptance'])

    def test_canonical_source_map_join_is_prepare_only_and_never_allocates_world(self):
        # Exercise the actual run admission branch with the established run
        # fixture. Heavy provenance and source-map validation are explicit
        # protocol seams; no worker or World is created in this test.
        source_map=self.base/'source-derived-map.json';source_map.write_text('{"protocol_fixture_only":true}')
        source_map_pin=m.pin(source_map)
        self.inputs['source_derived_map']={key:source_map_pin[key] for key in ('path','sha256')};self.bundle.write_text(json.dumps(self.inputs))
        provenance={'trusted':True,'experimental':False,'canonical':True}
        join={'status':'SourceOnlyPreflightPassed','world_created':False,'verified_inputs':[]}
        with patch.object(m,'source_build_provenance',return_value=provenance), \
             patch.object(m,'source_camera_policy_selection',return_value=None), \
             patch.object(m,'prepare_source_derived_map_join',return_value=(join,None,{})) as join_call, \
             patch.object(m,'metadata_origin_pins',return_value={}), \
             patch.object(m,'layout_replay_input_pins',return_value={}), \
             patch.object(m,'source_resource_binding_input_pins',return_value={}), \
             patch.object(m,'runtime_input_pins',return_value={}), \
             patch.object(m,'setup_matrix_provenance',return_value=None), \
             patch.object(m,'verify'):
            with self.assertRaisesRegex(UnsupportedSourceShape,'Bounded, trusted noncanonical source build required'):
                m.run(self.repo,self.build,self.bundle,self.base/'runtime-run',max_ticks=12,data_repo=self.data_repo)
            join_call.assert_not_called()
            prepared=m.run(self.repo,self.build,self.bundle,self.base/'prepare-run',max_ticks=12,data_repo=self.data_repo,prepare_only=True)
        self.assertEqual(join_call.call_count,1)
        self.assertEqual(prepared['status'],'PreparedOnly')
        self.assertIs(prepared['world_allocated'],False)
        self.assertFalse((self.base/'prepare-run'/'process.log').exists())
    def test_unbound_runtime_selection_rejected_before_child(self):
        other_repo=self.base/'different-source-root';other_repo.mkdir()
        with patch.object(m.subprocess,'Popen')as factory,self.assertRaisesRegex(UnsupportedSourceShape,'Run repository differs from built source root'):m.run(other_repo,self.build,self.bundle,self.base/'run')
        factory.assert_not_called();shutil.rmtree(self.base/'run')
        other=self.base/'other-mono';other.write_bytes(b'UNBOUND');self.receipt['runtime_selection']['mono']=str(other);self.save_build()
        with patch.object(m.subprocess,'Popen')as factory,self.assertRaisesRegex(UnsupportedSourceShape,'Unbound runtime selection'):m.run(self.repo,self.build,self.bundle,self.base/'run')
        factory.assert_not_called()
    def test_build_bound_runtime_drift_rejected_before_child(self):
        self.mono.write_bytes(b'CHANGED_RUNTIME')
        with patch.object(m.subprocess,'Popen')as factory,self.assertRaisesRegex(UnsupportedSourceShape,'Input changed:'):m.run(self.repo,self.build,self.bundle,self.base/'run')
        factory.assert_not_called()
    def test_explicit_prefix_is_diagnostic_and_never_full_validation(self):
        result=self.invoke({'status':'BoundedPrefix','source_outcome':'None'},max_ticks=12)
        self.assertEqual(result['status'],'BoundedPrefix');self.assertTrue(result['diagnostic_prefix_completed']);self.assertFalse(result['validation_passed']);self.assertFalse(result['full_outcome_observed'])
        request=json.loads((self.base/'run/run-request.json').read_bytes());self.assertEqual(request['max_ticks'],12)
    def test_default_bound_is_not_a_successful_diagnostic_or_validation(self):
        self.assert_failed(self.invoke({'status':'BoundedPrefix','source_outcome':'None'}))
    def test_defeat_is_a_real_observed_outcome_but_expected_victory_failure(self):
        result=self.invoke({'status':'SourceTerminal','source_outcome':'Defeat'})
        self.assertEqual(result['status'],'SourceOutcomeObserved');self.assertTrue(result['full_outcome_observed']);self.assertFalse(result['validation_passed']);self.assertFalse(result['diagnostic_prefix_completed']);self.assertEqual(result['expected_outcome'],'Victory')
    def test_source_aria_stop_is_not_a_complete_gameplay_outcome(self):
        self.assert_failed(self.invoke({'status':'SourceAriaStopped','source_outcome':'None'}))
    def test_nonzero_child_cannot_turn_terminal_payload_green(self):
        result=self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},exit_code=7)
        self.assert_failed(result);self.assertEqual(result['process_exit'],7)
    def test_missing_source_result_stays_failed(self):self.assert_failed(self.invoke())
    def test_timeout_terminates_only_returned_child_and_preserves_actual_code(self):
        result=self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},child_timeout=True)
        self.assert_failed(result);self.assertTrue(result['timed_out']);self.assertEqual(result['process_exit'],-15);child=self.children[0];self.assertTrue(child.terminated);self.assertFalse(child.killed);self.assertEqual(child.waits,[10,5])
    def test_timeout_escalates_only_owned_child_then_records_kill_code(self):
        result=self.invoke({'status':'BoundedPrefix'},max_ticks=12,child_timeout=True,kill_required=True)
        self.assert_failed(result);self.assertEqual(result['process_exit'],-9);self.assertTrue(result['timed_out']);child=self.children[0];self.assertTrue(child.terminated);self.assertTrue(child.killed);self.assertEqual(child.waits,[10,5,5])
    def test_invalid_timeout_rejected_before_output_or_child(self):
        for value in [0,-1,float('nan'),float('inf')]:
            with self.subTest(timeout=value),patch.object(m.subprocess,'Popen')as factory,self.assertRaisesRegex(UnsupportedSourceShape,'Finite positive'):m.run(self.repo,self.build,self.bundle,self.base/'run',timeout=value)
            factory.assert_not_called();self.assertFalse((self.base/'run').exists())
    def test_post_child_source_drift_fails_closed(self):
        for tracked in [self.source,self.data_repo/'Assets/Game/Scenes/Match.unity',self.data_repo/'Assets/Game/Resources/SkirmishMissionViewProfile_S002.asset']:
            with self.subTest(changed=tracked.name):
                original=tracked.read_bytes()
                with self.assertRaisesRegex(UnsupportedSourceShape,'Input changed:'):self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},on_exit=lambda path=tracked:path.write_bytes(b'CHANGED_DURING_RUN'),data_repo=self.data_repo)
                tracked.write_bytes(original);shutil.rmtree(self.base/'run')
    def test_bundle_hash_drift_rejected_before_child(self):
        Path(self.inputs['unit_metadata']['path']).write_bytes(b'CHANGED_DATA')
        with patch.object(m.subprocess,'Popen')as factory,self.assertRaisesRegex(UnsupportedSourceShape,'Bundle input changed: unit_metadata'):m.run(self.repo,self.build,self.bundle,self.base/'run')
        factory.assert_not_called()
    def test_loaded_runtime_manifest_absence_fails_closed(self):
        with self.assertRaisesRegex(UnsupportedSourceShape,'Actual loaded-runtime manifests missing'):self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},manifests=False)
    def test_timeout_without_post_outcome_manifest_returns_failed_receipt(self):
        result=self.invoke(child_timeout=True,manifests=False)
        self.assert_failed(result);self.assertTrue(result['timed_out']);self.assertEqual(result['process_exit'],-15)
        self.assertEqual(result['runtime_manifest_failure']['reason'],'Actual loaded-runtime manifests missing')
    def test_sparse_experimental_resolution_demotes_victory_through_run_and_batch(self):
        self.bind_sparse_experimental_source_manifest(with_proof=True)
        result=self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},data_repo=self.data_repo)
        self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved');self.assertTrue(result['diagnostic_only']);self.assertFalse(result['validation_passed']);self.assertTrue(result['full_outcome_observed'])
        self.assertTrue(result['source_build_provenance']['trusted']);self.assertTrue(result['source_build_provenance']['experimental'])
        classified=batch.classify({'mission':'S002','seed':104731},{'cli_exit':1,'outer_timeout':False},result)
        self.assertTrue(classified['execution_complete']);self.assertIsNone(classified['mission_victory']);self.assertFalse(classified['validation_passed'])
    def test_alternate_manifest_without_generator_proof_stays_untrusted(self):
        self.bind_sparse_experimental_source_manifest(with_proof=False)
        result=self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},data_repo=self.data_repo)
        self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved');self.assertFalse(result['validation_passed'])
        self.assertTrue(result['source_build_provenance']['trusted']);self.assertFalse(result['source_build_provenance']['canonical'])
    def test_old_build_receipt_runs_only_as_diagnostic(self):
        self.receipt.pop('source_resolution_pin');self.receipt.pop('canonical_source_manifest');self.save_build()
        result=self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},data_repo=self.data_repo)
        self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved');self.assertFalse(result['validation_passed'])
        self.assertEqual(result['source_build_provenance']['reason'],'LegacyBuildReceiptWithoutResolutionProof')
    def test_canonical_build_without_new_resolution_binding_cannot_validate(self):
        self.receipt.pop('source_resolution_pin');self.save_build()
        result=self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},data_repo=self.data_repo)
        self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved');self.assertFalse(result['validation_passed'])
        self.assertEqual(result['source_build_provenance']['reason'],'IncompleteNewBuildProvenance')
    def test_spoofed_canonical_manifest_path_is_not_accepted(self):
        alias=self.base/'canonical-manifest-alias.json';alias.write_bytes(self.manifest.read_bytes());alias_pin=m.pin(alias)
        self.receipt['canonical_source_manifest']=alias_pin;self.receipt['source_and_reference_pins'][alias_pin['path']]=alias_pin['sha256'];self.save_build()
        result=self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},data_repo=self.data_repo)
        self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved');self.assertFalse(result['validation_passed'])
        self.assertEqual(result['source_build_provenance']['reason'],'InvalidBuildProvenance')
    def test_real_resolver_unannotated_native_and_finite_rows_are_accepted(self):
        resolution=json.loads(self.resolution.read_text())
        resolution['pins'][str(self.source)]=m.sha(self.source.read_bytes())
        adapter=self.resolution.parent/'generated/SourceFiniteAdapter.cs';adapter.parent.mkdir(parents=True,exist_ok=True);adapter.write_text('class SourceFiniteAdapter {}\n')
        adapter_pin=m.pin(adapter);self.receipt['source_and_reference_pins'][adapter_pin['path']]=adapter_pin['sha256']
        resolution['resolved']=[
            {'id':'native_owner','kind':'native_direct','path':str(self.source),'sha256':m.sha(self.source.read_bytes()),'origin':'current_original_no_proof'},
            {'id':'finite_adapter','kind':'finite_adapter','generated':adapter.name,'path':str(adapter),'sha256':adapter_pin['sha256'],'origin':'strict_profile_json'},
        ]
        self.resolution.write_text(json.dumps(resolution,sort_keys=True,separators=(',',':'))+'\n')
        resolution_pin=m.pin(self.resolution);self.receipt['source_resolution_pin']=resolution_pin
        self.receipt['source_and_reference_pins'][resolution_pin['path']]=resolution_pin['sha256'];self.save_build()
        provenance=m.source_build_provenance(self.receipt)
        self.assertTrue(provenance['trusted']);self.assertFalse(provenance['experimental']);self.assertTrue(provenance['canonical'])
    def test_current_original_resolver_row_still_requires_proof(self):
        resolution=json.loads(self.resolution.read_text())
        resolution['resolved']=[{'id':'unproven_current','kind':'current_original','generated':'Current.cs','path':str(self.source),'sha256':m.sha(self.source.read_bytes())}]
        self.resolution.write_text(json.dumps(resolution,sort_keys=True,separators=(',',':'))+'\n')
        resolution_pin=m.pin(self.resolution);self.receipt['source_resolution_pin']=resolution_pin
        self.receipt['source_and_reference_pins'][resolution_pin['path']]=resolution_pin['sha256'];self.save_build()
        with self.assertRaisesRegex(UnsupportedSourceShape,'Resolved current source proof row required'):
            m.source_build_provenance(self.receipt)
    def test_retained_sparse_resolver_proof_is_recognized_without_receipt_qualification(self):
        # Construct a synthetic protocol fixture from retained real resolver
        # row/proof shapes. Refresh current source pins and rebase copied outputs
        # into a temporary directory. This is not a relabeled or qualified build.
        retained=Path('/private/tmp/s002-sparse-acquisition-build')
        build_path=retained/'build.json';resolution_path=retained/'source/resolution.json'
        if not build_path.is_file()or not resolution_path.is_file():self.skipTest('Retained sparse resolver artifacts are unavailable')
        build=json.loads(build_path.read_text());resolution=json.loads(resolution_path.read_text())
        with tempfile.TemporaryDirectory()as temporary:
            synthetic=Path(temporary);(synthetic/'source/generated').mkdir(parents=True)
            build['source_and_reference_pins']=dict(build['source_and_reference_pins'])
            resolution['pins']=dict(resolution['pins'])
            # This synthetic fixture exercises experimental proof closure only.
            # Refresh the proof's dependencies below; hashing unrelated resolver
            # inputs here does not strengthen that check and can read gigabytes.
            for row in resolution['resolved']:
                for proof in row.get('proof',[]):
                    detail=proof.get('generation_proof')
                    if type(detail)is not dict or detail.get('experimental_only')is not True:continue
                    generator=proof['generator'];generator_path=Path(generator);generator_sha=m.sha(generator_path.read_bytes())
                    proof['generator_sha256']=generator_sha;resolution['pins'][generator]=generator_sha;build['source_and_reference_pins'][generator]=generator_sha
                    for source_path in list(detail.get('source_pins',{})):
                        source=Path(source_path);digest=m.sha(source.read_bytes());detail['source_pins'][source_path]=digest
                        resolution['pins'][source_path]=digest;build['source_and_reference_pins'][source_path]=digest
                    outputs={}
                    for output in list(detail.get('outputs',{})):
                        original=retained/'source/generated'/output;copied=synthetic/'source/generated'/output
                        if not original.is_file():self.skipTest('Retained sparse generated output is unavailable: '+output)
                        copied.write_bytes(original.read_bytes());outputs[output]=m.sha(copied.read_bytes())
                        build['source_and_reference_pins'][str(copied)]=outputs[output]
                    detail['outputs']=outputs
                    output_name=row['generated'];copied=synthetic/'source/generated'/output_name
                    row['path']=str(copied);row['sha256']=outputs[output_name]
            manifest=Path(build['source_manifest']['path']);manifest_sha=m.sha(manifest.read_bytes())
            build['source_manifest']=m.pin(manifest);resolution['pins'][str(manifest)]=manifest_sha
            build['source_and_reference_pins'][str(manifest)]=manifest_sha
            resolution_path=synthetic/'source/resolution.json'
            resolution_path.write_text(json.dumps(resolution,sort_keys=True,separators=(',',':'))+'\n')
            resolution_pin=m.pin(resolution_path);build['source_resolution']=resolution_pin['path']
            build['source_resolution_pin']=resolution_pin
            build['source_and_reference_pins'][resolution_pin['path']]=resolution_pin['sha256']
            build['package_root']=str(Path(__file__).resolve().parents[3])
            package=Path(build['package_root'])
            canonical=m.pin(m.canonical_source_manifest(package,'S002'))
            build['canonical_source_manifest']=canonical
            build['source_and_reference_pins'][canonical['path']]=canonical['sha256']
            provenance=m.source_build_provenance(build)
            self.assertTrue(provenance['trusted']);self.assertTrue(provenance['experimental']);self.assertFalse(provenance['canonical'])
            self.assertGreaterEqual(len(provenance['experimental_proofs']),3)
    def test_source_profile_identity_accepts_s002_s026_and_shared_s073_manifest(self):
        cases=[('S002','LegacyGroundS002V1',{'mission':'S002','seed':104731,'numeric_runtime':'InstalledMono'},104731,None),
               ('S026','TypedGroundS026V1',{'runtime_backend':'TypedGroundS026V1','numeric_runtime':'InstalledMono','qualification_lineage':{'mission':'S026','seed':104755}},104755,None),
               ('S073','LegacyS073OwnerV1',{'mission':'S002','seed':104731,'numeric_runtime':'InstalledMono'},104729,[{'mission':'S073','seed':104729,'runtime_backend':'LegacyS073OwnerV1','scope':'LegacyStartupAndQueueOnly'}])]
        for mission,backend,profile,seed,variants in cases:
            with self.subTest(mission=mission):
                self.bind_identity_fixture(mission,backend,profile,seed,variants)
                provenance=m.source_build_provenance(self.receipt)
                self.assertTrue(provenance['trusted']);self.assertTrue(provenance['canonical']);self.assertFalse(provenance['experimental'])
    def test_runtime_backend_and_resolution_profile_must_match_manifest(self):
        profile={'runtime_backend':'TypedGroundS026V1','numeric_runtime':'InstalledMono','qualification_lineage':{'mission':'S026','seed':104755}}
        self.bind_identity_fixture('S026','TypedGroundS026V1',profile,104755)
        resolution=json.loads(self.resolution.read_text());resolution['runtime_backend']='LegacyGroundS002V1'
        self.resolution.write_text(json.dumps(resolution,sort_keys=True,separators=(',',':'))+'\n');pin=m.pin(self.resolution)
        self.receipt['source_resolution_pin']=pin;self.receipt['source_resolution']=pin['path'];self.receipt['source_and_reference_pins'][pin['path']]=pin['sha256']
        with self.assertRaisesRegex(UnsupportedSourceShape,'Build/resolution runtime backend differs'):m.source_build_provenance(self.receipt)
        resolution['runtime_backend']='TypedGroundS026V1';resolution['profile']={'mission':'S002','seed':104731,'numeric_runtime':'InstalledMono'}
        self.resolution.write_text(json.dumps(resolution,sort_keys=True,separators=(',',':'))+'\n');pin=m.pin(self.resolution)
        self.receipt['source_resolution_pin']=pin;self.receipt['source_resolution']=pin['path'];self.receipt['source_and_reference_pins'][pin['path']]=pin['sha256']
        with self.assertRaisesRegex(UnsupportedSourceShape,'Resolved source profile differs'):m.source_build_provenance(self.receipt)
    def test_self_consistent_manifest_profile_tamper_is_rejected(self):
        profile={'runtime_backend':'TypedGroundS026V1','numeric_runtime':'InstalledMono','qualification_lineage':{'mission':'S026','seed':104755}}
        self.bind_identity_fixture('S026','TypedGroundS026V1',profile,104755)
        manifest=Path(self.receipt['source_manifest']['path']);data=json.loads(manifest.read_text());data['profile']['qualification_lineage']['seed']=104756
        manifest.write_text(json.dumps(data,sort_keys=True)+'\n');manifest_pin=m.pin(manifest)
        resolution=json.loads(self.resolution.read_text());resolution['profile']=data['profile'];resolution['pins'][manifest_pin['path']]=manifest_pin['sha256']
        self.resolution.write_text(json.dumps(resolution,sort_keys=True,separators=(',',':'))+'\n');resolution_pin=m.pin(self.resolution)
        self.receipt['source_manifest']=manifest_pin;self.receipt['canonical_source_manifest']=manifest_pin
        self.receipt['source_resolution_pin']=resolution_pin;self.receipt['source_resolution']=resolution_pin['path']
        self.receipt['source_and_reference_pins'].update({manifest_pin['path']:manifest_pin['sha256'],resolution_pin['path']:resolution_pin['sha256']})
        with self.assertRaisesRegex(UnsupportedSourceShape,'Pinned manifest profile differs from resolver backend profile'):m.source_build_provenance(self.receipt)
    def test_unknown_backend_and_missing_or_invalid_s073_variant_reject(self):
        s002={'mission':'S002','seed':104731,'numeric_runtime':'InstalledMono'}
        self.bind_identity_fixture('S002','UnknownBackend',s002,104731)
        with self.assertRaisesRegex(UnsupportedSourceShape,'Build/resolution runtime backend differs'):
            m.source_build_provenance(self.receipt)
        variant={'mission':'S073','seed':104729,'runtime_backend':'LegacyS073OwnerV1','scope':'LegacyStartupAndQueueOnly'}
        for variants in (None,[],[{**variant,'scope':'Unbounded'}]):
            with self.subTest(variants=variants):
                self.bind_identity_fixture('S073','LegacyS073OwnerV1',s002,104729,variants)
                with self.assertRaisesRegex(UnsupportedSourceShape,'Exact bounded S073 source profile variant required'):
                    m.source_build_provenance(self.receipt)
    def test_mutated_bound_resolution_is_rejected_before_worker(self):
        self.resolution.write_text(self.resolution.read_text()+' ')
        with patch.object(m.subprocess,'Popen')as factory,self.assertRaisesRegex(UnsupportedSourceShape,'Input changed:'):
            m.run(self.repo,self.build,self.bundle,self.base/'run')
        factory.assert_not_called()
    def test_self_consistent_but_spoofed_experimental_output_claim_is_diagnostic(self):
        self.bind_sparse_experimental_source_manifest(with_proof=True)
        resolution=json.loads(self.resolution.read_text())
        resolution['resolved'][0]['proof'][0]['generation_proof']['outputs']['SparseTargetAcquisitionKernel.cs']='0'*64
        self.resolution.write_text(json.dumps(resolution,sort_keys=True,separators=(',',':'))+'\n')
        pin=m.pin(self.resolution);self.receipt['source_resolution_pin']=pin
        self.receipt['source_and_reference_pins'][pin['path']]=pin['sha256'];self.save_build()
        result=self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},data_repo=self.data_repo)
        self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved');self.assertFalse(result['validation_passed'])
        self.assertEqual(result['source_build_provenance']['reason'],'InvalidBuildProvenance')
    def test_external_qualification_demotes_even_trusted_canonical_build(self):
        provenance=m.source_build_provenance(self.receipt)
        result=m.classify_experimental_source_run({'mission_id':'S002','configuration_qualification':{},'metadata_compiler_context':{}},provenance,{'status':'SourceOutcomeObserved','validation_passed':True,'full_outcome_observed':True})
        self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved');self.assertFalse(result['validation_passed'])
        self.assertEqual(result['source_provenance_classification']['class'],'ExternalConfigurationQualification')
    def test_external_matrix_demotes_actual_run_receipt(self):
        initial_path=Path(self.inputs['initialization']['path'])
        initial=json.loads(initial_path.read_text())
        changed=self.base/'changed-matrix.csv';changed.write_text('CHANGED_MATRIX_PROTOCOL_FIXTURE')
        initial['setup_matrix']={'path':str(changed),'sha256':m.sha(changed.read_bytes())}
        initial_path.write_text(json.dumps(initial));self.inputs['initialization']=m.pin(initial_path)
        self.bundle.write_text(json.dumps(self.inputs))
        result=self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},data_repo=self.data_repo)
        self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved')
        self.assertFalse(result['validation_passed']);self.assertFalse(result['mission_ready'])
        self.assertEqual(result['source_provenance_classification']['class'],'ExternalOrUnverifiedSetupMatrix')
    def test_packaged_matrix_changed_during_run_cannot_qualify(self):
        packaged=self.data_repo/'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
        result=self.invoke({'status':'SourceTerminal','source_outcome':'Victory'},data_repo=self.data_repo,
                           on_exit=lambda:packaged.write_text('CHANGED_DURING_RUN'))
        self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved')
        self.assertFalse(result['validation_passed']);self.assertFalse(result['mission_ready'])
        matrix=result['source_provenance_classification']['setup_matrix_provenance']
        self.assertTrue(matrix['before']['canonical']);self.assertFalse(matrix['canonical'])
    def test_layout_replay_field_always_demotes_even_on_a_canonical_build(self):
        provenance={'trusted':True,'experimental':False,'canonical':True,'reason':'CanonicalSourceManifest'}
        for descriptor in (None, {}, {'path':'/copied/layout.json','sha256':'0'*64}):
            with self.subTest(descriptor=descriptor):
                initial={'mission_id':'S004','layout_json_replay':descriptor}
                result=m.classify_experimental_source_run(initial,provenance,{'status':'SourceOutcomeObserved','validation_passed':True,'mission_ready':True,'source_fit_certified':True})
                self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved')
                self.assertFalse(result['validation_passed']);self.assertFalse(result['mission_ready']);self.assertFalse(result['source_fit_certified'])
                classification=result['source_provenance_classification']
                self.assertEqual(classification['class'],'ExternalLayoutJsonReplay')
                self.assertTrue(classification['external_layout_json_replay'])
                self.assertFalse(classification['external_configuration_qualification'])
    def test_source_resource_bindings_always_demote_even_on_a_canonical_build(self):
        provenance={'trusted':True,'experimental':False,'canonical':True,'reason':'CanonicalSourceManifest'}
        for descriptor in (None, {}, {'path':'/copied/resources.json','sha256':'0'*64}):
            with self.subTest(descriptor=descriptor):
                result=m.classify_experimental_source_run({'mission_id':'S004','source_resource_bindings':descriptor},provenance,
                    {'status':'SourceOutcomeObserved','validation_passed':True,'mission_ready':True,'source_fit_certified':True})
                self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved')
                self.assertFalse(result['validation_passed']);self.assertFalse(result['mission_ready']);self.assertFalse(result['source_fit_certified'])
                classification=result['source_provenance_classification']
                self.assertEqual(classification['class'],'ExternalSourceResourceBindings')
                self.assertTrue(classification['external_source_resource_bindings'])
                self.assertFalse(classification['external_configuration_qualification'])
    def test_expanded_source_diagnostic_presence_never_grants_qualification(self):
        provenance={'trusted':True,'experimental':False,'canonical':True,'reason':'CanonicalSourceManifest'}
        for descriptor in (None, {}, {'kind':'ExpandedAuthoredSourceDiagnostic'}):
            with self.subTest(descriptor=descriptor):
                result=m.classify_experimental_source_run({'mission_id':'S004','source_expanded_diagnostic':descriptor},provenance,
                    {'status':'SourceOutcomeObserved','validation_passed':True,'mission_ready':True,'source_fit_certified':True})
                self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved')
                self.assertFalse(result['validation_passed']);self.assertFalse(result['mission_ready']);self.assertFalse(result['source_fit_certified'])
                self.assertEqual(result['source_provenance_classification']['class'],'ExpandedSourceDiagnostic')
                self.assertTrue(result['source_provenance_classification']['expanded_source_diagnostic'])
    def test_canonical_typed_lane_is_left_to_existing_runtime_authority_gates(self):
        provenance={'trusted':True,'experimental':False,'canonical':True,'reason':'CanonicalSourceManifest'}
        result={'status':'SourceOutcomeObserved','validation_passed':True,'full_outcome_observed':True}
        before=json.dumps(result,sort_keys=True)
        classified=m.classify_experimental_source_run({'mission_id':'S026'},provenance,result)
        self.assertEqual(json.dumps(classified,sort_keys=True),before)
class S002DiagnosticReceiptClassificationTests(unittest.TestCase):
    def setUp(self):
        self.initial={'mission_id':'S002','placement_domain':{'s002_original_owner_diagnostic':{}}}
    def test_bounded_prefix_is_nonvalidating_and_successful_cli(self):
        receipt={'status':'BoundedPrefix','validation_passed':False,'full_outcome_observed':False,'diagnostic_prefix_completed':True,'source_result':{'status':'BoundedPrefix'}}
        result=m.classify_s002_source_diagnostic(self.initial,receipt)
        self.assertEqual(result['status'],'BoundedPrefix');self.assertTrue(result['diagnostic_only']);self.assertFalse(result['validation_passed'])
        self.assertTrue(result['validation_passed'] or result['diagnostic_prefix_completed'])
        for key in ('camera_qualified','active_mission_water_qualified','runtime_presentation_qualified','source_fit_certified','native_acceptance','mission_ready'):self.assertIs(result[key],False)
    def test_failed_or_missing_result_stays_failed(self):
        for source in (None,{'status':'Failed'}):
            with self.subTest(source=source):
                receipt={'status':'Failed','validation_passed':False,'full_outcome_observed':False,'diagnostic_prefix_completed':False,'source_result':source}
                result=m.classify_s002_source_diagnostic(self.initial,receipt)
                self.assertEqual(result['status'],'Failed');self.assertFalse(result['validation_passed'] or result['diagnostic_prefix_completed']);self.assertIs(result['source_result'],source)
    def test_legacy_full_outcome_is_unchanged(self):
        receipt={'status':'SourceOutcomeObserved','validation_passed':True,'full_outcome_observed':True,'diagnostic_prefix_completed':False,'source_result':{'status':'SourceTerminal','source_outcome':'Victory'}}
        initial={'mission_id':'S002','placement_domain':{'packed':{'sha256':'fixture'}}};before=json.dumps(receipt,sort_keys=True)
        self.assertEqual(json.dumps(m.classify_s002_source_diagnostic(initial,receipt),sort_keys=True),before)
    def test_failed_focused_capture_forces_future_terminal_run_to_diagnostic_only(self):
        initial={'mission_id':'S002','reference_templates':{'source_reference_sha256':m.S002_FOCUSED_PREFAB_FAILED_CAPTURE_SHA}}
        receipt={'status':'SourceOutcomeObserved','validation_passed':True,'full_outcome_observed':True,'diagnostic_prefix_completed':False,'source_result':{'status':'SourceTerminal','source_outcome':'Victory'}}
        result=m.classify_s002_source_diagnostic(initial,receipt)
        self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved')
        self.assertEqual(result['capture_validation_status'],'Failed')
        self.assertFalse(result['capture_pass_marker_present'])
        self.assertFalse(result['validation_passed'] or result['native_acceptance'] or result['mission_ready'] or result['qualified_source_mission'])
        self.assertEqual(result['source_capture_validation']['capture_sha256'],m.S002_FOCUSED_PREFAB_FAILED_CAPTURE_SHA)

class S003CanonicalManifestDiagnosticTests(unittest.TestCase):
    def test_unqualified_mission_authoring_does_not_select_runtime_profile(self):
        source=CI/'run_skirmish_source_simulation.py'
        tree=ast.parse(source.read_text())
        function=next(node for node in tree.body if isinstance(node,ast.FunctionDef) and node.name=='default_manifest')
        class Profiles:
            @staticmethod
            def runtime_backend(mission):
                raise ValueError('Unqualified runtime profile: '+mission)
        class Owned:
            @staticmethod
            def require(name):return Profiles
        namespace={'owned':Owned}
        exec(compile(ast.Module(body=[function],type_ignores=[]),str(source),'exec'),namespace)
        select=namespace['default_manifest'];package=Path('/reviewed/package')
        self.assertEqual(select(package,'S074','author',False),package/'Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json')
        with self.assertRaisesRegex(ValueError,'Unqualified runtime profile'):
            select(package,'S074','run',True)

    def test_default_manifest_uses_s003_only_for_runtime_command_contexts(self):
        source=CI/'run_skirmish_source_simulation.py'
        tree=ast.parse(source.read_text())
        function=next(node for node in tree.body if isinstance(node,ast.FunctionDef) and node.name=='default_manifest')
        class Profiles:
            @staticmethod
            def runtime_backend(mission):return 'TypedGroundS026V1'
            @staticmethod
            def uses_s003_derived_factory(mission):return False
        class Owned:
            @staticmethod
            def require(name):return Profiles
        namespace={'owned':Owned}
        exec(compile(ast.Module(body=[function],type_ignores=[]),str(source),'exec'),namespace)
        select=namespace['default_manifest'];package=Path('/reviewed/package')
        dedicated=package/'Tools/CI/SkirmishSourceSimulation/source_manifest.s003.plan.json'
        general=package/'Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json'
        for command,bundle_present in [('resolve',False),('build',False),('run',False),('smoke',True)]:
            with self.subTest(command=command,bundle_present=bundle_present):
                self.assertEqual(select(package,'S003',command,bundle_present),dedicated)
        for command,bundle_present in [('author',False),('smoke',False)]:
            with self.subTest(command=command,bundle_present=bundle_present):
                self.assertEqual(select(package,'S003',command,bundle_present),general)

    def test_s003_uses_dedicated_manifest_and_cannot_be_promoted(self):
        package=Path('/reviewed/package')
        profiles=m.owned.require('skirmish_source_profiles')
        with patch.object(profiles,'runtime_backend',return_value='TypedGroundS026V1'):
            self.assertEqual(m.canonical_source_manifest(package,'S003'),package/'Tools/CI/SkirmishSourceSimulation/source_manifest.s003.plan.json')
        initial={'mission_id':'S003','placement_domain':{'kind':'S003PhysicalSnapshotDiagnostic'}}
        receipt={'status':'SourceOutcomeObserved','validation_passed':True,'full_outcome_observed':True,'diagnostic_prefix_completed':False,'mission_ready':True}
        result=m.classify_s003_source_diagnostic(initial,receipt)
        self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved')
        self.assertTrue(result['diagnostic_only'])
        for key in ('camera_qualified','active_mission_water_qualified','placement_qualified','runtime_presentation_qualified','source_fit_certified','native_acceptance','mission_ready','native_aria_planner_parity_proven'):
            self.assertIs(result[key],False)

    def test_failed_focused_capture_also_labels_failed_run_receipt(self):
        initial={'mission_id':'S002','reference_templates':{'source_reference_sha256':m.S002_FOCUSED_PREFAB_FAILED_CAPTURE_SHA}}
        receipt={'status':'Failed','validation_passed':False,'full_outcome_observed':False,'diagnostic_prefix_completed':False,'source_result':{'status':'Failed'}}
        result=m.classify_s002_source_diagnostic(initial,receipt)
        self.assertEqual(result['status'],'Failed')
        self.assertEqual(result['capture_validation_status'],'Failed')
        self.assertFalse(result['validation_passed'] or result['diagnostic_prefix_completed'] or result['mission_ready'])

class S026InactiveDiagnosticAdmissionTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.base=Path(self.temp.name).resolve()
        self.source_root=self.base/'captured-source';self.source_root.mkdir()
        self.capture=self.base/'physical-input.json';self.capture.write_text('CAPTURE_FIXTURE_ONLY')
        descriptor={'path':str(self.capture),'sha256':'fa0d0dedd774ba7648ee2cedaa872f506339878bff45429abadf9585157e2710'}
        self.initial={'mission_id':'S026','navigation_reference':dict(descriptor),'placement_domain':{'s026_inactive_diagnostic':{**descriptor,'source_root':str(self.source_root),'external_inputs':{}}}}
        self.inputs={'mission':'S026','static_clearance':dict(descriptor)}

    def test_exact_inactive_capture_contract_is_admitted_without_packed_map_inputs(self):
        diagnostic,origin=m.require_s026_inactive_diagnostic_contract(self.initial,self.inputs)
        self.assertEqual(diagnostic,self.initial['placement_domain']['s026_inactive_diagnostic'])
        self.assertEqual(origin,self.source_root)
        self.assertNotIn('packed',self.initial['placement_domain'])

    def test_wrong_mission_or_copied_capture_digest_fails_closed(self):
        for mutate,expected in [
            (lambda initial,inputs:inputs.update(mission='S049'),'Exact S026 inactive capture identity required'),
            (lambda initial,inputs:initial['placement_domain']['s026_inactive_diagnostic'].update(sha256='0'*64),'Exact S026 inactive capture identity required'),
            (lambda initial,inputs:initial.update(navigation_reference={'path':str(self.capture),'sha256':'0'*64}),'Exact S026 inactive capture identity required'),
        ]:
            with self.subTest(expected=expected):
                initial=json.loads(json.dumps(self.initial));inputs=json.loads(json.dumps(self.inputs));mutate(initial,inputs)
                with self.assertRaisesRegex(UnsupportedSourceShape,expected):m.require_s026_inactive_diagnostic_contract(initial,inputs)

    def test_copied_project_file_with_different_bytes_fails_pinned_source_check(self):
        asset=self.source_root/'Assets/Game/Map.asset';asset.parent.mkdir(parents=True);asset.write_bytes(b'COPIED_WRONG_SOURCE')
        wrong_sha=m.sha(asset.read_bytes())
        def descriptor(row,maximum):
            path=Path(row['path']);m.need(path.is_file() and path.stat().st_size<=maximum and m.sha(path.read_bytes())==row['sha256'],'S026 input changed: '+str(path));return path
        with self.assertRaisesRegex(UnsupportedSourceShape,'S026 input changed:'):
            m.verify_s026_project_pin(self.source_root,'Assets/Game/Map.asset','0'*64,descriptor)
        self.assertNotEqual(wrong_sha,'0'*64)

    def test_symlinked_captured_project_file_fails_closed(self):
        outside=self.base/'outside.asset';outside.write_bytes(b'ORIGINAL_CAPTURE_SOURCE')
        link=self.source_root/'Assets/Game/Map.asset';link.parent.mkdir(parents=True);link.symlink_to(outside)
        with self.assertRaisesRegex(UnsupportedSourceShape,'Captured source symlink forbidden'):
            m.verify_s026_project_pin(self.source_root,'Assets/Game/Map.asset',m.sha(outside.read_bytes()),lambda *_:outside)

    def test_inactive_s026_reference_receipt_does_not_require_runtime_projection_fields(self):
        initial={'reference_templates':{'path':'/capture/reference.json','sha256':'capture-pin'}}
        m.pin_reference_template_runtime_inputs(initial,{},lambda *_:self.fail('inactive diagnostic must not require projection files'))

    def test_non_diagnostic_reference_requires_both_projection_file_pins(self):
        initial={'reference_templates':{'source_reference_path':'/reference/source.json','source_reference_sha256':'source-pin','projection_manifest_path':'/reference/projection.json','projection_manifest_sha256':'projection-pin'}}
        seen=[]
        m.pin_reference_template_runtime_inputs(initial,None,lambda row:seen.append(row))
        self.assertEqual(seen,[{'path':'/reference/source.json','sha256':'source-pin'},{'path':'/reference/projection.json','sha256':'projection-pin'}])

    def test_declared_candidate_requires_matching_single_context_and_inactive_capture(self):
        self.initial.update(selection={'difficulty_id':2,'size_id':1,'seed':104755},
            s026_declared_pad_candidate={'path':'/candidate','sha256':'0a412bf51e718e3fba00d1a89f3699a9b8959c200933eb8d20c097fc0db86f61'},
            metadata_compatibility={'sha256':'0c8c592b13e04ab49194dd076a287cbdd6db8ca486d06b8930907df97f2b6dbb'},
            mission_logical_binding={'sha256':'d44197811ecac5ccc94357bf6fc974abdd3beb3c9eb3c73337f88c32e4018e70'})
        self.assertEqual(m.require_s026_declared_candidate(self.initial,self.inputs),self.initial['s026_declared_pad_candidate'])
        for mutate in [lambda x:x['selection'].update(difficulty_id=1),lambda x:x['s026_declared_pad_candidate'].update(sha256='0'*64),lambda x:x['mission_logical_binding'].update(sha256='0'*64),lambda x:x.pop('placement_domain'),lambda x:x.pop('s026_declared_pad_candidate')]:
            with self.subTest(mutate=mutate):
                initial=json.loads(json.dumps(self.initial));mutate(initial)
                with self.assertRaises(UnsupportedSourceShape):m.require_s026_declared_candidate(initial,self.inputs)

class S073RecurringDiagnosticReceiptTests(unittest.TestCase):
    def setUp(self):
        self.result={
            'status':'DiagnosticPlayingNativeReadinessUnverified','session_phase':'Playing','session_outcome':'None',
            'source_host_diagnostic_activation':True,'operation_map_physical_source_validated':False,
            'world_initialized':False,'scheduler_tick_executed':True,'mission_ready':False,
            'original_legacy_rules_diagnostic_playing_step_passed':True,'map_blocker_parity_verified':False,
            'source_host_diagnostic_gameplay_admission':{'opt_in':True,'shared_start_completion_predicate_passed':True,
                'physical_source_validated_before':False,'physical_source_validated_after':False},
            'source_host_original_tactical_owner_scheduler_tick':{'status':'RecurringOriginalOwnerTicksExecuted',
                'tick_budget':1200,'ticks_executed':1200,'owner_update_counts':{'AIEconomySystem':1200,
                    'AISquadSystem':1200,'SkirmishRulesSystem':1200},
                'simulation_advance_wall_seconds':2.5,'simulation_advance_timing_status':'Measured',
                'simulation_advance_timing_scope':'tick loop only'},
            'source_host_aria_policy_step':{'kind':'S073LegacySharedAriaRecurringPolicySteps','step_count':1200,
                'accepted_action_count':0,'readiness_claimed':False,'mission_outcome_claimed':False,
                'last_step':{'action':'None'}},
            'worker_setup_wall_seconds':1.25,'worker_setup_timing_status':'Measured','worker_setup_timing_scope':'setup only',
            'simulation_advance_wall_seconds':2.5,'simulation_advance_timing_status':'Measured',
            'simulation_advance_timing_scope':'tick loop only','worker_total_wall_seconds':10.0,
            'worker_total_timing_scope':'worker assembly before serialization'}

    def test_recurring_nonready_diagnostic_is_observed_without_promoting_validation(self):
        self.assertTrue(m.s073_source_host_diagnostic_expected(self.result,2,False,True))
        self.assertFalse(self.result['mission_ready'])
        self.assertFalse(self.result['map_blocker_parity_verified'])
        receipt=m.s073_source_host_diagnostic_receipt_fields(self.result,True)
        self.assertTrue(receipt['diagnostic_prefix_completed'])
        self.assertFalse(receipt['full_outcome_observed'])
        self.assertFalse(receipt['validation_passed'])
        self.assertFalse(receipt['mission_ready'])
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);(root/'result.json').write_text('{"status":"worker-only"}')
            (root/'process.log').write_text('actual worker log')
            executable={'path':str(root/'missing.exe'),'sha256':'0'*64}
            failure=m.s073_record_postrun_integrity_failure(root,FileNotFoundError('missing executable'),executable,2,False)
            self.assertEqual(failure['status'],'PostRunIntegrityVerificationFailed')
            self.assertIsNone(failure['executable_observed_after_worker'])
            self.assertEqual(failure['worker_result_observed']['sha256'],m.sha((root/'result.json').read_bytes()))
            self.assertFalse(failure['worker_result_classified']);self.assertFalse(failure['mission_ready'])
            self.assertEqual(json.loads((root/'postrun-integrity-failure.json').read_text()),failure)
        row=json.loads(json.dumps(self.result))
        row['native_player_input_observed']=False
        row['source_host_aria_policy_step'].update(step_count=0, last_step=None,
            no_player_policy_diagnostic=True, planner_route='NoPlayerPolicy_ExplicitSourceDiagnostic')
        row['source_host_original_tactical_owner_scheduler_tick'].update(
            no_player_policy_diagnostic=True, aria_steps=[], aria_actions_accepted=0, aria_failure=None)
        self.assertTrue(m.s073_source_host_diagnostic_expected(row,2,False,True,True))
        self.assertFalse(m.s073_source_host_diagnostic_expected(row,2,False,True,False))
        for change in ('action','steps','input','request'):
            mutated=json.loads(json.dumps(row))
            if change=='action': mutated['source_host_aria_policy_step']['accepted_action_count']=1
            if change=='steps': mutated['source_host_original_tactical_owner_scheduler_tick']['aria_steps']=[{}]
            if change=='input': mutated['native_player_input_observed']=True
            if change=='request': mutated['source_host_original_tactical_owner_scheduler_tick']['no_player_policy_diagnostic']=False
            self.assertFalse(m.s073_source_host_diagnostic_expected(mutated,2,False,True,True))

    def test_rejects_nonmonotonic_or_mismatched_step_receipt(self):
        row=json.loads(json.dumps(self.result));row['source_host_aria_policy_step']['step_count']=1199
        self.assertFalse(m.s073_source_host_diagnostic_expected(row,2,False,True))
        row=json.loads(json.dumps(self.result));row['source_host_original_tactical_owner_scheduler_tick']['ticks_executed']=1201
        self.assertFalse(m.s073_source_host_diagnostic_expected(row,2,False,True))

    def test_bounded_diagnostic_rejects_timeout_terminal_outcome_or_any_readiness_claim(self):
        self.assertFalse(m.s073_source_host_diagnostic_expected(self.result,2,True,True))
        row=json.loads(json.dumps(self.result));row['session_outcome']='Victory'
        self.assertFalse(m.s073_source_host_diagnostic_expected(row,2,False,True))
        row=json.loads(json.dumps(self.result));row['mission_ready']=True
        self.assertFalse(m.s073_source_host_diagnostic_expected(row,2,False,True))

    def terminal_result(self,outcome):
        row=json.loads(json.dumps(self.result))
        row.update(status='DiagnosticTerminalOutcomeNativeReadinessUnverified',session_phase='Finished',session_outcome=outcome)
        scheduler=row['source_host_original_tactical_owner_scheduler_tick']
        scheduler.update(tick_budget='until_original_terminal_outcome',ticks_executed=1201,terminal_phase='Finished',terminal_outcome=outcome)
        scheduler['owner_update_counts']={key:1201 for key in ('AIEconomySystem','AISquadSystem','SkirmishRulesSystem')}
        scheduler['aria_steps']=[{'action':'None'} for _ in range(1200)]
        aria=row['source_host_aria_policy_step'];aria.update(step_count=1200,last_step={'action':'None'})
        row['original_legacy_rules_diagnostic_playing_step_passed']=False
        return row

    def test_accepts_only_observed_natural_victory_defeat_or_draw_as_diagnostic(self):
        for outcome in ('Victory','Defeat','Draw'):
            with self.subTest(outcome=outcome):
                row=self.terminal_result(outcome)
                self.assertTrue(m.s073_source_host_diagnostic_expected(row,2,False,True))
                self.assertFalse(row['mission_ready'])
                self.assertFalse(row['map_blocker_parity_verified'])
                receipt=m.s073_source_host_diagnostic_receipt_fields(row,True)
                self.assertTrue(receipt['full_outcome_observed'])
                self.assertFalse(receipt['diagnostic_prefix_completed'])
                self.assertFalse(receipt['validation_passed'])
                self.assertFalse(receipt['mission_ready'])
                self.assertTrue(receipt['terminal_phase_observed'])
                self.assertEqual(receipt['terminal_outcome_observed'],outcome)

    def test_natural_terminal_requires_consistent_original_outcome_and_actual_counts(self):
        for mutate in (
            lambda row: row.update(session_outcome='None'),
            lambda row: row.update(session_phase='Playing'),
            lambda row: row['source_host_original_tactical_owner_scheduler_tick'].update(terminal_outcome='Defeat'),
            lambda row: row['source_host_original_tactical_owner_scheduler_tick']['owner_update_counts'].update(AIEconomySystem=1200),
            lambda row: row['source_host_original_tactical_owner_scheduler_tick'].update(ticks_executed=1200),
        ):
            row=self.terminal_result('Victory');mutate(row)
            self.assertFalse(m.s073_source_host_diagnostic_expected(row,2,False,True))
        row=self.terminal_result('Victory');row['mission_ready']=True
        self.assertFalse(m.s073_source_host_diagnostic_expected(row,2,False,True))

    def stopped_terminal_result(self):
        row=self.terminal_result('Draw')
        row['session_id']='same-source-session'
        scheduler=row['source_host_original_tactical_owner_scheduler_tick']
        aria=row['source_host_aria_policy_step']
        scheduler.update(ticks_executed=54011,aria_stopped=True,aria_actions_accepted=0,
            session_id='same-source-session')
        scheduler['owner_update_counts']={key:54011 for key in ('AIEconomySystem','AISquadSystem','SkirmishRulesSystem')}
        aria.update(step_count=10801,accepted_action_count=0,last_step={'frame':10801,'action':'None','stopped':True})
        scheduler['aria_steps']=[{'frame':i+1,'action':'None','stopped':i==10800} for i in range(10801)]
        scheduler['original_production_runtime']={'source_host_terminal_observed':True,
            'source_host_terminal_phase':'Finished','source_host_terminal_outcome':'Draw'}
        return row

    def test_accepts_genuine_early_stopped_aria_with_correlated_source_terminal(self):
        row=self.stopped_terminal_result()
        self.assertTrue(m.s073_source_host_diagnostic_expected(row,2,False,True))
        receipt=m.s073_source_host_diagnostic_receipt_fields(row,True)
        self.assertTrue(receipt['full_outcome_observed'])
        self.assertFalse(receipt['validation_passed'])
        self.assertFalse(receipt['mission_ready'])

    def test_stopped_aria_terminal_rejects_spoofed_or_stale_evidence(self):
        mutations=(
            lambda r:r['source_host_original_tactical_owner_scheduler_tick'].update(aria_stopped=False),
            lambda r:r['source_host_aria_policy_step']['last_step'].update(stopped=False),
            lambda r:r['source_host_original_tactical_owner_scheduler_tick']['aria_steps'][-1].update(stopped=False),
            lambda r:r['source_host_aria_policy_step'].update(step_count=10800),
            lambda r:r['source_host_original_tactical_owner_scheduler_tick'].update(aria_actions_accepted=1),
            lambda r:r['source_host_original_tactical_owner_scheduler_tick']['original_production_runtime'].update(source_host_terminal_outcome='Victory'),
            lambda r:r['source_host_original_tactical_owner_scheduler_tick']['owner_update_counts'].update(AIEconomySystem=54010),
            lambda r:r.update(session_id='stale-session'),
        )
        for mutate in mutations:
            with self.subTest(mutation=mutate):
                row=self.stopped_terminal_result();mutate(row)
                self.assertFalse(m.s073_source_host_diagnostic_expected(row,2,False,True))

    def test_worker_timing_receipt_copies_only_measured_values_and_scopes(self):
        fields=m.s073_worker_timing_receipt_fields(self.result)
        self.assertEqual(fields['worker_setup_wall_seconds'],1.25)
        self.assertEqual(fields['simulation_advance_wall_seconds'],2.5)
        self.assertEqual(fields['worker_total_wall_seconds'],10.0)
        self.assertEqual(fields['simulation_advance_timing_scope'],'tick loop only')
        unmeasured=m.s073_worker_timing_receipt_fields({'worker_setup_wall_seconds':None,
            'worker_setup_timing_status':'NotMeasured_SchedulerNotInvoked',
            'simulation_advance_wall_seconds':None,'simulation_advance_timing_status':'NotMeasured_SchedulerNotInvoked',
            'worker_total_wall_seconds':3.0})
        self.assertIsNone(unmeasured['worker_setup_wall_seconds'])
        self.assertIsNone(unmeasured['simulation_advance_wall_seconds'])
        self.assertEqual(unmeasured['worker_setup_timing_status'],'NotMeasured_SchedulerNotInvoked')
        self.assertEqual(unmeasured['worker_total_wall_seconds'],3.0)


class S073NaturalTerminalBatchClassificationTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.root=Path(self.temp.name)
        self.job={'mission':'S073','seed':104729}
        self.process={'cli_exit':0,'outer_timeout':False,'owned_descendants_survived_cli':False}

    def make_receipt(self,outcome,worker_outcome=None,budget=None,ticks=12):
        actual=outcome if worker_outcome is None else worker_outcome
        aria_steps=max(0,ticks-1)
        worker={'kind':'SkirmishSourceSimulationRunReceipt','mission':'S073','seed':104729,'process_exit':2,
            'status':'DiagnosticTerminalOutcomeNativeReadinessUnverified','source_host_diagnostic_activation':True,
            'scheduler_tick_executed':True,
            'session_phase':'Finished','session_outcome':actual,'operation_map_physical_source_validated':False,
            'world_initialized':False,'mission_ready':False,'validation_passed':False,'map_blocker_parity_verified':False,
            'source_host_diagnostic_gameplay_admission':{'opt_in':True,'shared_start_completion_predicate_passed':True,
                'physical_source_validated_before':False,'physical_source_validated_after':False},
            'source_host_original_tactical_owner_scheduler_tick':{'status':'RecurringOriginalOwnerTicksExecuted',
                'tick_budget':'until_original_terminal_outcome' if budget is None else budget,'ticks_executed':ticks,
                'terminal_phase':'Finished','terminal_outcome':actual,
                'owner_update_counts':{'AIEconomySystem':ticks,'AISquadSystem':ticks,'SkirmishRulesSystem':ticks},
                'aria_steps':[{'action':'None'} for _ in range(aria_steps)]},
            'source_host_aria_policy_step':{'kind':'S073LegacySharedAriaRecurringPolicySteps',
                'step_count':aria_steps,'readiness_claimed':False,'mission_outcome_claimed':False}}
        worker_path=self.root/'worker-result.json';worker_path.write_text(json.dumps(worker))
        receipt={'kind':'SkirmishSourceSimulationRunReceipt','schema_version':1,'mission':'S073','seed':104729,
            'status':'DiagnosticTerminalOutcomeNativeReadinessUnverified','process_exit':2,'timed_out':False,
            'source_host_diagnostic_observed':True,'full_outcome_observed':True,'diagnostic_prefix_completed':False,
            'terminal_phase_observed':True,'terminal_outcome_observed':outcome,'validation_passed':False,
            'mission_ready':False,'map_blocker_parity_verified':False,
            'worker_result':{'path':str(worker_path),'sha256':batch.sha(worker_path)}}
        return receipt,worker_path,worker

    def test_victory_defeat_draw_are_completed_but_unqualified(self):
        for outcome in ('Victory','Defeat','Draw'):
            with self.subTest(outcome=outcome):
                receipt,_,_=self.make_receipt(outcome)
                result=batch.classify(self.job,self.process,receipt)
                self.assertTrue(result['execution_complete'])
                self.assertIsNone(result['mission_victory'])
                self.assertFalse(result['validation_passed'])
                self.assertEqual(result['diagnostic_source_outcome'],outcome)
                self.assertTrue(m.s073_natural_terminal_diagnostic_completed(receipt))

    def test_rejects_inconsistent_or_malformed_receipt_and_outcome(self):
        for outcome in ('None','Partial',''):
            receipt,_,_=self.make_receipt(outcome)
            self.assertFalse(batch.classify(self.job,self.process,receipt)['execution_complete'])
            self.assertFalse(m.s073_natural_terminal_diagnostic_completed(receipt))
        mutations=(
            lambda r:r.update(terminal_phase_observed=False),
            lambda r:r.update(full_outcome_observed=False),
            lambda r:r.update(diagnostic_prefix_completed=True),
            lambda r:r.update(validation_passed=True),
            lambda r:r.update(mission_ready=True),
            lambda r:r.update(map_blocker_parity_verified=True),
            lambda r:r.update(historical_input_diagnostic_requested=True),
            lambda r:r.update(source_host_diagnostic_observed=False),
            lambda r:r.update(process_exit=0),
            lambda r:r.update(timed_out=True),
        )
        for mutate in mutations:
            receipt,_,_=self.make_receipt('Victory');mutate(receipt)
            self.assertFalse(batch.classify(self.job,self.process,receipt)['execution_complete'])
            self.assertFalse(m.s073_natural_terminal_diagnostic_completed(receipt))

    def test_rejects_cli_failure_worker_pin_drift_and_worker_outcome_mismatch(self):
        receipt,_,_=self.make_receipt('Defeat')
        self.assertFalse(batch.classify(self.job,dict(self.process,cli_exit=2),receipt)['execution_complete'])
        receipt,worker_path,_=self.make_receipt('Victory')
        worker_path.write_text('{}')
        self.assertFalse(batch.classify(self.job,self.process,receipt)['execution_complete'])
        receipt,_,_=self.make_receipt('Victory',worker_outcome='Defeat')
        self.assertFalse(batch.classify(self.job,self.process,receipt)['execution_complete'])
        receipt,worker_path,worker=self.make_receipt('Victory')
        worker['source_host_original_tactical_owner_scheduler_tick']['terminal_outcome']='Draw'
        worker_path.write_text(json.dumps(worker));receipt['worker_result']['sha256']=batch.sha(worker_path)
        self.assertFalse(batch.classify(self.job,self.process,receipt)['execution_complete'])
        receipt,worker_path,worker=self.make_receipt('Victory')
        worker['source_host_original_tactical_owner_scheduler_tick']['owner_update_counts']['AIEconomySystem']=11
        worker_path.write_text(json.dumps(worker));receipt['worker_result']['sha256']=batch.sha(worker_path)
        self.assertFalse(batch.classify(self.job,self.process,receipt)['execution_complete'])

    def test_bounded_and_preparing_diagnostics_are_not_terminal_completions(self):
        receipt,_,_=self.make_receipt('Victory')
        receipt.update(status='DiagnosticPlayingNativeReadinessUnverified',full_outcome_observed=False,
            diagnostic_prefix_completed=True,terminal_phase_observed=False,terminal_outcome_observed=None)
        self.assertFalse(batch.classify(self.job,dict(self.process,cli_exit=2),receipt)['execution_complete'])
        self.assertFalse(m.s073_natural_terminal_diagnostic_completed(receipt))

    def test_bounded_natural_terminal_within_cap_completes_but_over_cap_rejects(self):
        for ticks in (11,12):
            with self.subTest(ticks=ticks):
                job=dict(self.job,max_ticks=12)
                receipt,_,_=self.make_receipt('Defeat',budget=12,ticks=ticks)
                result=batch.classify(job,self.process,receipt)
                self.assertTrue(result['execution_complete'])
                self.assertIsNone(result['mission_victory'])
                self.assertFalse(result['validation_passed'])
                self.assertEqual(result['diagnostic_source_outcome'],'Defeat')
        job=dict(self.job,max_ticks=12)
        receipt,_,_=self.make_receipt('Victory',budget=12,ticks=13)
        self.assertFalse(batch.classify(job,self.process,receipt)['execution_complete'])
        receipt,_,_=self.make_receipt('Victory',budget=13,ticks=12)
        self.assertFalse(batch.classify(job,self.process,receipt)['execution_complete'])
        receipt,worker_path,worker=self.make_receipt('Victory',budget=12,ticks=12)
        worker['status']='DiagnosticPlayingNativeReadinessUnverified'
        worker['session_phase']='Playing';worker['session_outcome']='None'
        scheduler=worker['source_host_original_tactical_owner_scheduler_tick']
        scheduler['terminal_phase']='Playing';scheduler['terminal_outcome']='None'
        worker_path.write_text(json.dumps(worker));receipt['worker_result']['sha256']=batch.sha(worker_path)
        receipt.update(status='DiagnosticPlayingNativeReadinessUnverified',full_outcome_observed=False,
            diagnostic_prefix_completed=True,terminal_phase_observed=False,terminal_outcome_observed=None)
        self.assertFalse(batch.classify(job,dict(self.process,cli_exit=2),receipt)['execution_complete'])


class S073ProductionOwnerPhaseTraceSourceControlTests(unittest.TestCase):
    def test_phase_trace_is_opt_in_read_only_and_anchored_to_both_real_owners(self):
        source=(CI/'SkirmishSourceSimulation/Adapters/SourceS073LegacyNormalRun.cs').read_text()
        capture=source.split('static JObject CaptureProductionPhaseState',1)[1].split('static JObject CaptureProductionDecisionInputs',1)[0]
        self.assertIn('FactionTacticalMaterialsComponent',capture)
        self.assertIn('productionRuntime.Snapshot()',capture)
        self.assertIn('match.SessionId.ToString()',capture)
        self.assertIn('em.WorldUnmanaged.SequenceNumber',capture)
        self.assertIn('if (traceProductionPhases)',source)
        self.assertIn('frame >= 898 && frame <= 903',source)
        for boundary in ('before_AIProductionSystem_OnUpdate','after_AIProductionSystem_OnUpdate',
                         'before_SourceS073LegacyProductionRuntime_Step','after_SourceS073LegacyProductionRuntime_Step'):
            self.assertIn(boundary,source)
        self.assertIn('JToken.DeepEquals(previousProductionPhaseState, beforeProductionOwner["observed"])',source)
        self.assertIn('productionPhaseTrace',source)

    def test_terminal_guard_requires_same_session_scheduler_evidence(self):
        source=(CI/'SkirmishSourceSimulation/Adapters/SourceS073LegacyNormalRun.cs').read_text()
        corroboration=source.split('static bool CorroboratesDiagnosticTerminal',1)[1].split('static void TraceStage',1)[0]
        for evidence in ('RecurringOriginalOwnerTicksExecuted','until_original_terminal_outcome',
                         'requestedTickBudget > 0','schedulerBudget?.Type == JTokenType.Integer',
                         'ticks <= requestedTickBudget',
                         'session.Phase == SkirmishPhase.Finished','SkirmishOutcome.Victory',
                         'SkirmishOutcome.Defeat','SkirmishOutcome.Draw','originalSessionId',
                         'terminal_phase','terminal_outcome','world_sequence','ticks_executed'):
            self.assertIn(evidence,corroboration)
        self.assertIn('diagnosticSourceActivation &&\n            CorroboratesDiagnosticTerminal(em, session, originalSessionId, schedulerTickBudget, legacyOwnerSchedulerTick)',source)
        self.assertIn('em.GetComponentData<OperationMapMetadataComponent>(mapRoot).PhysicalSourceValidated != 0',source)
        self.assertIn('["session_id"] = final.SessionId.ToString()',source)
        self.assertIn('["world_sequence"] = em.WorldUnmanaged.SequenceNumber',source)


class S026InactiveDiagnosticReceiptClassificationTests(unittest.TestCase):
    def setUp(self):
        self.initial={'mission_id':'S026','placement_domain':{'s026_inactive_diagnostic':{}}}

    def test_terminal_victory_remains_diagnostic_and_all_readiness_stays_false(self):
        result={'status':'SourceOutcomeObserved','validation_passed':True,'active_mission_water_qualified':True,'source_fit_certified':True,'native_acceptance':True,'mission_ready':True,'full_outcome_observed':True}
        classified=m.classify_s026_inactive_diagnostic(self.initial,result)
        self.assertEqual(classified['status'],'DiagnosticSourceOutcomeObserved')
        self.assertTrue(classified['diagnostic_only']);self.assertTrue(classified['full_outcome_observed'])
        for key in ('validation_passed','active_mission_water_qualified','source_fit_certified','native_acceptance','mission_ready'):self.assertIs(classified[key],False)
        self.assertEqual(classified['scope'],'S026 source-only inactive-water diagnostic; active water, source fit, native input and player acceptance remain unqualified.')

    def test_failed_run_stays_failed_while_readiness_stays_false(self):
        result={'status':'Failed','validation_passed':False,'active_mission_water_qualified':False,'source_fit_certified':False,'native_acceptance':False,'mission_ready':False}
        classified=m.classify_s026_inactive_diagnostic(self.initial,result)
        self.assertEqual(classified['status'],'Failed');self.assertTrue(classified['diagnostic_only'])
        for key in ('validation_passed','active_mission_water_qualified','source_fit_certified','native_acceptance','mission_ready'):self.assertIs(classified[key],False)

class SharedS003DiagnosticSelectionTests(unittest.TestCase):
    def test_explicit_pinned_selection_and_fail_closed_controls(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp).resolve();manifest=root/'manifest.json';rsp=root/'compile.rsp';config=root/'selection.json'
            manifest.write_text(json.dumps({'compiler_defines':['WARLINE_S003_SHARED_SELECTION']}))
            rsp.write_text(json.dumps('-define:OTHER,WARLINE_S003_SHARED_SELECTION')+'\n')
            descriptor=lambda p:{'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size}
            receipt={'source_manifest':descriptor(manifest),'compiler_response':descriptor(rsp)}
            initial={'mission_id':'S003','selection':{'seed':104732,'size_id':1,'difficulty_id':2},'configuration_qualification':{'path':'/fixture/regular','sha256':'a'*64}}
            value={'schema_version':1,'kind':'S003SourceDiagnosticSelection','mission_id':'S003','seed':104732,'size_id':1,'selected_difficulty':'Recruit','capture_difficulty':'Regular','capture_qualification':initial['configuration_qualification'],'native_parity':False,'runtime_admission':False,'mission_ready':False}
            config.write_text(json.dumps(value));env={'WARLINE_SOURCE_DIAGNOSTIC_CONFIG':str(config),'WARLINE_SOURCE_DIAGNOSTIC_CONFIG_SHA256':descriptor(config)['sha256']};provenance={'trusted':True,'experimental':True,'canonical':False}
            selected=m.s003_source_diagnostic_selection(receipt,{'mission':'S003'},initial,provenance,env)
            self.assertEqual(selected['selected_difficulty'],'Recruit');self.assertEqual(selected['sha256'],env['WARLINE_SOURCE_DIAGNOSTIC_CONFIG_SHA256'])
            self.assertIsNone(m.s003_source_diagnostic_selection(receipt,{'mission':'S003'},initial,provenance,{}))
            for override in ({'WARLINE_SOURCE_DIAGNOSTIC_CONFIG':str(config)},dict(env,WARLINE_SOURCE_DIAGNOSTIC_CONFIG_SHA256='0'*64)):
                with self.assertRaises(UnsupportedSourceShape):m.s003_source_diagnostic_selection(receipt,{'mission':'S003'},initial,provenance,override)
            with self.assertRaises(UnsupportedSourceShape):m.s003_source_diagnostic_selection(receipt,{'mission':'S002'},initial,provenance,env)
            with self.assertRaises(UnsupportedSourceShape):m.s003_source_diagnostic_selection(receipt,{'mission':'S003'},initial,{'trusted':True,'experimental':False,'canonical':True},env)
            for key,bad in [('selected_difficulty','1'),('capture_difficulty','Recruit'),('native_parity',True),('seed',104733)]:
                changed=dict(value);changed[key]=bad;config.write_text(json.dumps(changed));changed_env=dict(env,WARLINE_SOURCE_DIAGNOSTIC_CONFIG_SHA256=descriptor(config)['sha256'])
                with self.assertRaises(UnsupportedSourceShape):m.s003_source_diagnostic_selection(receipt,{'mission':'S003'},initial,provenance,changed_env)
            config.write_text(json.dumps(value));manifest.write_text(json.dumps({'compiler_defines':[]}));receipt['source_manifest']=descriptor(manifest)
            with self.assertRaises(UnsupportedSourceShape):m.s003_source_diagnostic_selection(receipt,{'mission':'S003'},initial,provenance,env)

if __name__=='__main__':unittest.main()

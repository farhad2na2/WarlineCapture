"""Protocol controls for the pinned S049 Projection-17 diagnostic receipt."""
import importlib.util,json,sys,tempfile,unittest
from pathlib import Path
from unittest.mock import patch

CI=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(CI));sys.dont_write_bytecode=True
bootstrap_spec=importlib.util.spec_from_file_location('_s049_checked_cli_bootstrap',CI/'run_skirmish_source_simulation.py')
bootstrap=importlib.util.module_from_spec(bootstrap_spec);sys.modules[bootstrap_spec.name]=bootstrap;bootstrap_spec.loader.exec_module(bootstrap)
source_build=bootstrap.owned.require('skirmish_source_build')
batch=bootstrap.owned.require('run_skirmish_source_batch')

class S049DiagnosticProtocolTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.base=Path(self.temp.name).resolve()
        self.identity=source_build._s049_expected_identity()
        self.bundle={'path':self.identity['data_bundle']['path'],'sha256':self.identity['data_bundle']['sha256']}
        self.inputs={'mission':'S049','seed':104734,
            'initialization':dict(self.identity['initialization']),
            'static_clearance':dict(self.identity['fixed_map']),
            'camera_values':{'path':'/tmp/camera-values.json','sha256':self.identity['camera_values_sha256']},
            'camera_motion':{'path':'/tmp/camera-motion.json','sha256':self.identity['camera_motion_sha256']}}
        self.initial={'mission_id':'S049','source_epoch':'OriginalCompilerDerivedNativeFactoryContext/v1',
            'world_profile':{'identity_backend':'FreshLogicalEntitiesV1','system_initialization':'OriginalOnCreate','future_native_inputs':False},
            'metadata_compiler_context':dict(self.identity['metadata_epoch']),
            'configuration_qualification':dict(self.identity['configuration_qualification']),
            'placement_domain':{
                'original_owner_masks':{'path':'/Volumes/T7/Projects/WarlineCapture/Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project/Logs/SkirmishProduction/s049-armorbreak-placement-read-domain-20261028-r1.json','sha256':self.identity['native_readback_sha256']},
                'captured_asset_origin':{'source_root':'/Volumes/T7/Projects/WarlineCapture/Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project','source_definition_path':'Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset','source_definition_sha256':self.identity['source_definition_sha256']}},
            'static_map':{'definition':{'sha256':self.identity['source_definition_sha256']}},
            'engineering_camera_profile':{'sha256':self.identity['camera_profile_sha256']},
            'reference_templates':{'source_reference_sha256':self.identity['source_reference_sha256'],'sha256':self.identity['template_slice_sha256']}}
        self.preflight_pin,self.worker_log,self.pins=self.make_evidence()

    def write_json(self,path,value):
        path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,sort_keys=True)+'\n');return source_build.pin(path)

    def make_evidence(self,water=True,camera=True):
        selected={'s049_physical_water_diagnostic':water,'active_mission_water_qualified':False,'source_fit_certified':False,'native_camera_context_qualified':False,'actual_camera_profile_sha256':self.identity['camera_profile_sha256']}
        payload={'status':'Passed','world_allocated':False,'entity_ids_allocated':0,'selected_runtime_map_preflight':selected}
        base=self.base/'proof';base.mkdir(exist_ok=True)
        native_log=base/'native-process.log'
        native_lines=[source_build.S049_PHYSICAL_WATER_MARKER,source_build.S049_CAMERA_MARKER,
            '[SourceRunPreflight] '+json.dumps(payload,separators=(',',':'))]
        native_log.write_text('\n'.join(native_lines)+'\n')
        executable=base/'SourceSimulation.exe';executable.write_bytes(b'protocol fixture executable')
        request=base/'run-request.json'
        request_pin=self.write_json(request,{'kind':'SkirmishSourceSimulationRun','mission':'S049','seed':104734,'source_repository_root':'/Volumes/T7/Projects/WarlineCapture','initialization':dict(self.identity['initialization']),'static_clearance':dict(self.identity['fixed_map'])})
        runtime_paths=[CI/'skirmish_source_build.py',CI/'run_skirmish_source_simulation.py'];runtime_pins={str(path.resolve()):__import__('hashlib').sha256(path.read_bytes()).hexdigest() for path in runtime_paths}
        input_pins=base/'input-pins.json';input_pin=self.write_json(input_pins,{self.identity['initialization']['path']:self.identity['initialization']['sha256'],self.identity['fixed_map']['path']:self.identity['fixed_map']['sha256'],**runtime_pins})
        process=base/'native-process.json';process_pin=self.write_json(process,{'process_exit':0,'timed_out':False,'argv':['/qualified/mono','--config',str(base/'mono-config.xml'),'--optimize=-float32',str(executable),'--source-run-preflight',str(request)]})
        build_path=base/'build.json';build_pin=self.write_json(build_path,{'kind':'SkirmishSourceSimulationBuild','status':'Passed','mission':'S049','seed':104734,'source_and_reference_pins':runtime_pins,'executable':{'path':str(executable)}})
        native_log_pin=source_build.pin(native_log)
        native_data={'status':'Passed','mission':'S049','compiler_hash':971644661,'world_allocated':False,'entity_ids_allocated':0,'metadata_source_and_current_origins_checked':True,'fresh_storage_authority_created_unclaimed':True,'selected_runtime_map_preflight':selected,'configuration_qualification_scope':{'selected_context':{'catalog_id':'S049','seed':104734,'setup_hash':971644661}},'s049_diagnostic_markers':{'physical_water':source_build.S049_PHYSICAL_WATER_MARKER if water else 'forged-water-flag','camera':source_build.S049_CAMERA_MARKER if camera else 'forged-camera-flag','native_preflight_log':native_log_pin,'native_preflight_process':process_pin,'native_preflight_request':request_pin,'native_preflight_input_pins':input_pin}}
        evidence_path=base/'preflight-evidence.json'
        evidence={'schema_version':1,'kind':source_build.S049_DIAGNOSTIC_PREFLIGHT_KIND,'mission':'S049','seed':104734,'identity':self.identity,'data_bundle':self.bundle,'native_data':native_data,'build_receipt':build_pin}
        preflight_pin=self.write_json(evidence_path,evidence)
        worker_log=base/'worker-process.log';worker_log.write_text(source_build.S049_PHYSICAL_WATER_MARKER+'\n'+source_build.S049_CAMERA_MARKER+'\n')
        pins={preflight_pin['path']:preflight_pin['sha256'],native_log_pin['path']:native_log_pin['sha256'],process_pin['path']:process_pin['sha256'],request_pin['path']:request_pin['sha256'],input_pin['path']:input_pin['sha256'],build_pin['path']:build_pin['sha256'],**runtime_pins}
        return preflight_pin,worker_log,pins

    def classify(self,status='SourceOutcomeObserved',preflight='default'):
        raw={'status':'SourceTerminal','source_outcome':'Victory'} if status=='SourceOutcomeObserved' else {'status':status,'source_outcome':'None'} if status=='BoundedPrefix' else {'status':'Failed'}
        result={'status':status,'process_exit':0,'timed_out':False,'full_outcome_observed':status=='SourceOutcomeObserved','diagnostic_prefix_completed':status=='BoundedPrefix','validation_passed':status=='SourceOutcomeObserved','source_result':raw,'data_bundle':self.bundle,'build_receipt':self.pins and {'path':next(p for p in self.pins if p.endswith('build.json')),'sha256':self.pins[next(p for p in self.pins if p.endswith('build.json'))]},'input_and_runtime_pins':dict(self.pins)}
        evidence=self.preflight_pin if preflight=='default' else preflight
        with patch.dict(source_build.os.environ,{'WARLINE_S049_PHYSICAL_WATER_DIAGNOSTIC':'1','WARLINE_S049_CAMERA_DIAGNOSTIC':'1'}):
            source_build.classify_s049_source_diagnostic(self.initial,self.inputs,self.bundle,result,evidence,self.worker_log)
        return result

    def classify_context(self,initial=None,inputs=None,bundle=None,preflight='default',worker_log=None):
        result={'status':'SourceOutcomeObserved','process_exit':0,'timed_out':False,'full_outcome_observed':True,'diagnostic_prefix_completed':False,'validation_passed':True,'source_result':{'status':'SourceTerminal','source_outcome':'Victory'}}
        evidence=self.preflight_pin if preflight=='default' else preflight
        with patch.dict(source_build.os.environ,{'WARLINE_S049_PHYSICAL_WATER_DIAGNOSTIC':'1','WARLINE_S049_CAMERA_DIAGNOSTIC':'1'}):
            source_build.classify_s049_source_diagnostic(initial or self.initial,inputs or self.inputs,bundle or self.bundle,result,evidence,worker_log or self.worker_log)
        return result

    def batch_classify(self,receipt,cli_exit=1):
        return batch.classify({'mission':'S049','seed':104734,'max_ticks':12},{'cli_exit':cli_exit,'outer_timeout':False,'owned_descendants_survived_cli':False},receipt)

    def test_terminal_outcome_is_diagnostic_only_and_batch_never_calls_it_victory(self):
        receipt=self.classify()
        receipt.update(schema_version=1,kind='SkirmishSourceSimulationRunReceipt',mission='S049',seed=104734)
        self.assertEqual(receipt['status'],'DiagnosticSourceOutcomeObserved')
        self.assertTrue(receipt['diagnostic_only']);self.assertFalse(receipt['validation_passed'])
        for field in ('camera_qualified','motion_reference_passed','active_mission_water_qualified','placement_qualified','runtime_presentation_qualified','source_fit_certified','native_acceptance','mission_ready','qualified_source_mission'):self.assertIs(receipt[field],False)
        result=self.batch_classify(receipt)
        self.assertEqual(result['status'],'DiagnosticSourceOutcomeObserved');self.assertIsNone(result['mission_victory']);self.assertFalse(result['validation_passed'])

    def test_bounded_prefix_and_failure_remain_nonqualifying(self):
        prefix=self.classify('BoundedPrefix');self.assertEqual(prefix['status'],'BoundedPrefix');self.assertTrue(prefix['diagnostic_prefix_completed']);self.assertFalse(prefix['validation_passed'])
        prefix.update(schema_version=1,kind='SkirmishSourceSimulationRunReceipt',mission='S049',seed=104734)
        self.assertEqual(self.batch_classify(prefix,cli_exit=0)['status'],'BoundedPrefix')
        failed=self.classify('Failed');self.assertEqual(failed['status'],'Failed');self.assertFalse(failed['validation_passed'])

    def test_missing_preflight_and_forged_environment_flags_fail_closed(self):
        missing=self.classify(preflight=None);missing['s049_diagnostic_context'].pop('preflight_evidence',None)
        self.assertEqual(missing['status'],'Failed');self.assertTrue(missing['diagnostic_only']);self.assertFalse(missing['validation_passed'])
        forged_pin,forged_log,_=self.make_evidence(water=False)
        original=self.worker_log;self.worker_log=forged_log
        forged=self.classify(preflight=forged_pin);self.worker_log=original
        self.assertEqual(forged['status'],'Failed');self.assertFalse(forged['s049_diagnostic_context']['evidence_valid'])

    def test_wrong_seed_hash_identity_or_map_cannot_retain_normal_success(self):
        bad_inputs=dict(self.inputs);bad_inputs['seed']=104735
        bad_bundle=dict(self.bundle);bad_bundle['sha256']='0'*64
        bad_initial=dict(self.initial);bad_initial['source_epoch']='Unpinned'
        bad_map=dict(self.inputs);bad_map['static_clearance']=dict(self.identity['fixed_map'],sha256='1'*64)
        for initial,inputs,bundle in [(self.initial,bad_inputs,self.bundle),(self.initial,self.inputs,bad_bundle),(bad_initial,self.inputs,self.bundle),(self.initial,bad_map,self.bundle)]:
            with self.subTest(seed=inputs.get('seed'),bundle=bundle.get('sha256'),epoch=initial.get('source_epoch'),map=inputs.get('static_clearance')):
                result=self.classify_context(initial,inputs,bundle)
                self.assertEqual(result['status'],'Failed');self.assertTrue(result['diagnostic_only']);self.assertFalse(result['validation_passed'])

    def test_copied_bundle_cannot_escape_diagnostic_classification(self):
        copied=dict(self.bundle,path=str(self.base/'copied-bundle.json'))
        result=self.classify_context(bundle=copied)
        self.assertEqual(result['status'],'Failed')
        self.assertTrue(result['diagnostic_only'])
        self.assertFalse(result['validation_passed'])
        legacy={'schema_version':1,'kind':'SkirmishSourceSimulationRunReceipt','mission':'S049','seed':104734,
                'status':'SourceOutcomeObserved','process_exit':0,'timed_out':False,'validation_passed':True,
                'full_outcome_observed':True,'diagnostic_prefix_completed':False,
                'source_result':{'status':'SourceTerminal','source_outcome':'Victory'},'data_bundle':copied}
        self.assertFalse(self.batch_classify(legacy,cli_exit=0)['validation_passed'])
        legacy.pop('data_bundle')
        job={'mission':'S049','seed':104734,'data_bundle':copied}
        process={'cli_exit':0,'outer_timeout':False,'owned_descendants_survived_cli':False}
        self.assertFalse(batch.classify(job,process,legacy)['validation_passed'])
        # A repackaged bundle still carries the exact pinned diagnostic epoch.
        legacy['data_bundle']={'path':str(self.base/'repacked.json'),'sha256':'f'*64}
        legacy['input_and_runtime_pins']={self.identity['metadata_epoch']['path']:self.identity['metadata_epoch']['sha256']}
        self.assertFalse(batch.classify({'mission':'S049','seed':104734},process,legacy)['validation_passed'])

    def test_mutated_preflight_and_missing_or_duplicate_worker_markers_fail_closed(self):
        mutated=self.base/'mutated-preflight.json';doc=json.loads(Path(self.preflight_pin['path']).read_text());doc['seed']=104735
        mutated_pin=self.write_json(mutated,doc)
        self.assertEqual(self.classify(preflight=mutated_pin)['status'],'Failed')
        for contents in ['',source_build.S049_PHYSICAL_WATER_MARKER+'\n'+source_build.S049_CAMERA_MARKER+'\n'+source_build.S049_CAMERA_MARKER+'\n']:
            path=self.base/'worker-tamper.log';path.write_text(contents)
            result=self.classify_context(worker_log=path)
            self.assertEqual(result['status'],'Failed');self.assertFalse(result['s049_diagnostic_context']['evidence_valid'])

    def test_batch_rechecks_preflight_hash_and_rejects_forged_readiness(self):
        receipt=self.classify();receipt.update(schema_version=1,kind='SkirmishSourceSimulationRunReceipt',mission='S049',seed=104734)
        receipt['mission_ready']=True
        self.assertEqual(self.batch_classify(receipt)['reason'],'ContradictoryDiagnosticQualification')
        receipt['mission_ready']=False
        evidence=Path(receipt['s049_diagnostic_context']['preflight_evidence']['path']);evidence.write_text(evidence.read_text()+'tampered\n')
        self.assertEqual(self.batch_classify(receipt)['reason'],'MissingOrContradictoryS049DiagnosticEvidence')

    def test_batch_rechecks_worker_log_digest(self):
        receipt=self.classify();receipt.update(schema_version=1,kind='SkirmishSourceSimulationRunReceipt',mission='S049',seed=104734)
        worker=Path(receipt['s049_diagnostic_context']['worker_process_log']['path']);worker.write_text(worker.read_text()+'late mutation\n')
        self.assertEqual(self.batch_classify(receipt)['reason'],'MissingOrContradictoryS049DiagnosticEvidence')

    def test_batch_rejects_forged_diagnostic_flag_and_legacy_r17_success(self):
        forged=self.classify();forged.update(schema_version=1,kind='SkirmishSourceSimulationRunReceipt',mission='S049',seed=104734)
        forged['s049_diagnostic_context']['evidence_valid']=False
        self.assertEqual(self.batch_classify(forged)['reason'],'MissingOrContradictoryS049DiagnosticEvidence')
        legacy={'schema_version':1,'kind':'SkirmishSourceSimulationRunReceipt','mission':'S049','seed':104734,'status':'SourceOutcomeObserved','process_exit':0,'timed_out':False,'validation_passed':True,'full_outcome_observed':True,'diagnostic_prefix_completed':False,'source_result':{'status':'SourceTerminal','source_outcome':'Victory'},'data_bundle':self.bundle}
        self.assertEqual(self.batch_classify(legacy)['reason'],'S049Projection17DiagnosticClassificationMissing')

    def test_batch_requires_current_runner_and_cli_in_both_preflight_and_worker_pins(self):
        receipt=self.classify();receipt.update(schema_version=1,kind='SkirmishSourceSimulationRunReceipt',mission='S049',seed=104734)
        path=str((CI/'run_skirmish_source_simulation.py').resolve())
        receipt['input_and_runtime_pins'].pop(path)
        self.assertEqual(self.batch_classify(receipt)['reason'],'MissingOrContradictoryS049DiagnosticEvidence')

if __name__=='__main__':unittest.main()

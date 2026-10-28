"""Protocol-only build/run controls: temporary receipts and owned process stubs.

No fixture is gameplay/outcome evidence. No compiler, world, Unity or player runs.
"""
import hashlib,importlib.util,json,math,subprocess,sys,tempfile,unittest,shutil
from pathlib import Path
from unittest.mock import patch
sys.dont_write_bytecode=True
CI=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(CI))
import skirmish_source_build as m
from skirmish_source_manifest import UnsupportedSourceShape

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
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.base=Path(self.temp.name).resolve()
        self.repo=self.base/'repo';self.repo.mkdir();self.recorded=self.base/'recorded-runtime'
        self.data_repo=self.base/'data-repo';self.data_repo.mkdir()
        for repository in [self.repo,self.data_repo]:
            for relative,value in [('Assets/Game/Scenes/Match.unity','CURRENT_SCENE_PROTOCOL_FIXTURE'),('Assets/Game/Resources/SkirmishMissionViewProfile_S002.asset','CURRENT_PROFILE_PROTOCOL_FIXTURE')]:
                asset=repository/relative;asset.parent.mkdir(parents=True,exist_ok=True);asset.write_text(value)

        self.contents=self.recorded/'Contents';self.script=self.contents/'Resources/Scripting';self.engine=self.script/'Managed/UnityEngine'
        self.engine.mkdir(parents=True);self.mono=self.script/'MonoBleedingEdge/bin/mono';self.mono.parent.mkdir(parents=True);self.mono.write_bytes(b'PROTOCOL_MONO_PATH_ONLY')
        config=self.script/'MonoBleedingEdge/etc/mono/config';config.parent.mkdir(parents=True);config.write_text('<configuration><dllmap target="libMonoPosixHelper.dylib" /></configuration>')
        self.helper=self.contents/'Frameworks/libMonoPosixHelper.dylib';self.helper.parent.mkdir(parents=True);self.helper.write_bytes(b'PROTOCOL_HELPER_ONLY')
        self.newton=self.base/'newtonsoft/Newtonsoft.Json.dll';self.newton.parent.mkdir();self.newton.write_bytes(b'PROTOCOL_REFERENCE_ONLY')
        self.exe=self.base/'build/SourceSimulation.exe';self.exe.parent.mkdir();self.exe.write_bytes(b'PROTOCOL_NOT_EXECUTABLE')
        self.placement=self.base/'build/placement/build.json';self.placement.parent.mkdir();self.placement.write_text('{}')
        self.source=self.base/'CurrentPolicy.cs';self.source.write_text('class CurrentPolicy {}')
        self.pins={str(p.resolve()):m.sha(p.read_bytes())for p in [self.mono,self.newton,self.source]}
        self.receipt={'runtime_backend':'LegacyGroundS002V1','schema_version':1,'kind':'SkirmishSourceSimulationBuild','status':'Passed','mission':'S002','seed':104731,'repository_root':str(self.repo),'source_and_reference_pins':dict(self.pins),'executable':m.pin(self.exe),'placement_build':str(self.placement),'runtime_selection':{k:str(v)for k,v in {'contents':self.contents,'script':self.script,'engine':self.engine,'mono':self.mono,'newtonsoft':self.newton}.items()}}
        self.build=self.base/'build.json';self.save_build()
        self.inputs={'schema_version':1,'mission':'S002','seed':104731}
        for name in ['initialization','unit_metadata','static_clearance','camera_values','camera_motion']:
            path=self.base/(name+'.data');path.write_bytes(json.dumps({'mission_id':'S002','selection':{'seed':104731,'difficulty_id':2,'size_id':1}}).encode()if name=='initialization'else('PROTOCOL_DATA_ONLY:'+name).encode());self.inputs[name]=m.pin(path)
        self.bundle=self.base/'bundle.json';self.bundle.write_text(json.dumps(self.inputs));self.children=[]
    def save_build(self):self.build.write_text(json.dumps(self.receipt))
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
if __name__=='__main__':unittest.main()

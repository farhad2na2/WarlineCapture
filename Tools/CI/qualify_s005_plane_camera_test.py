"""Synthetic CLI envelope controls, never gameplay/source readiness evidence."""
import copy
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import unittest
from s005_paid_plane_lineage_qualifier_test import fixture
from s005_plane_camera_evidence_test import CameraEvidenceControls
from s005_windows_source_snapshot import candidate_hash


class CameraCliControls(unittest.TestCase):
    def test_source_wrapper_and_raw_joins(self):
        camera_fixture=CameraEvidenceControls();camera_fixture.setUp()
        self.addCleanup(camera_fixture.doCleanups)
        directory=Path(camera_fixture.temp.name)
        raw,contacts,log=fixture();trace=directory/'synthetic-only-trace.jsonl'
        for row in raw:row['tracePath']=str(trace)
        Path(str(trace)+'.touch.jsonl').write_text(''.join(json.dumps(row)+'\n' for row in contacts))
        camera=copy.deepcopy(camera_fixture.rows)
        for row in camera:row['attempt']=row['measurement']['SessionId']='synthetic-only'
        camera[1]['frame']=camera[1]['measurement']['Frame']=199
        manifest=[dict(path='Assets/SyntheticOnly.cs',sha256='1'*64)]
        builds=[dict(path='Library/ScriptAssemblies/Game.Editor.dll',sha256='2'*64)]
        snapshot=dict(source_manifest=manifest,candidate_sha256=candidate_hash(manifest),build_manifest=builds,
            build_sha256=hashlib.sha256(json.dumps(builds,sort_keys=True,separators=(',',':')).encode()).hexdigest())
        digest=snapshot['candidate_sha256']
        log+='\n[UnityInvoke] ExitCode: 0\n'+''.join('[SkirmishCandidateFingerprint] phase=Completed owner='+owner+' sha256='+digest+'\n' for owner in ('LaunchCatalog','Finish'))
        for name,content in [('before.json',snapshot),('after.json',snapshot)]: (directory/name).write_text(json.dumps(content))
        rawpath=directory/'synthetic-only-raw.jsonl';logpath=directory/'synthetic-only.log'
        rawpath.write_text(''.join(json.dumps(row)+'\n' for row in raw))
        Path(str(rawpath)+'.camera.jsonl').write_text(''.join(json.dumps(row)+'\n' for row in camera))
        tool=Path(__file__).with_name('qualify_s005_plane_camera.py')
        for index,(mode,expected) in enumerate([('baseline',0),('bad-native-fingerprint',1),('wrong-wrapper',1),('bad-after',1),('pre-attempt-native-rendering-failure',1)]):
            with self.subTest(mode=mode):
                current_log=log.replace('owner=Finish sha256='+digest,'owner=Finish sha256='+'0'*64) if mode=='bad-native-fingerprint' else log
                if mode=='pre-attempt-native-rendering-failure':
                    current_log=log.replace('[UnityInvoke] ExitCode: 0','[UnityInvoke] ExitCode: 2')+'\n[SkirmishNativeRendering] result=Failed reason=incompletePackedRenderInitialization\n'
                    rawpath.write_text(json.dumps(dict(stage='Started'))+'\n')
                else:rawpath.write_text(''.join(json.dumps(row)+'\n' for row in raw))
                logpath.write_text(current_log)
                current_after=copy.deepcopy(snapshot)
                if mode=='bad-after':current_after['build_sha256']='0'*64
                (directory/'after.json').write_text(json.dumps(current_after))
                command=['rtk','proxy',sys.executable,str(tool),'--raw',str(rawpath),'--log',str(logpath),
                    '--source-before',str(directory/'before.json'),'--source-after',str(directory/'after.json'),
                    '--wrapper-exit','2' if mode=='pre-attempt-native-rendering-failure' else '37' if mode=='wrong-wrapper' else '0','--output',str(directory/('result'+str(index)+'.json')),
                    '--pin','Assets/SyntheticOnly.cs='+'1'*64]
                result=subprocess.run(command,capture_output=True,text=True)
                self.assertEqual(result.returncode,expected,result.stdout+result.stderr)
                evidence=json.loads((directory/('result'+str(index)+'.json')).read_text())
                self.assertFalse(evidence['cameraAccepted']);self.assertFalse(evidence['fullMissionCertified'])
                if mode=='pre-attempt-native-rendering-failure':
                    self.assertFalse(evidence['qualified']);self.assertEqual(evidence['result'],'Failed')
                    self.assertEqual(evidence['failure_category'],'NativePreAttemptRenderingFailure')
                    self.assertEqual(evidence['native_failure_reasons'],['incompletePackedRenderInitialization'])
                    self.assertEqual(evidence['wrapper_exit'],2);self.assertFalse(evidence['native_attempt_observed'])
                    self.assertNotIn('Traceback',result.stderr)

if __name__=='__main__':unittest.main()

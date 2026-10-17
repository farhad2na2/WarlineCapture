import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import threading
import time
import unittest

ROOT=next(p for p in Path(__file__).resolve().parents if (p/'ProjectSettings/ProjectVersion.txt').is_file())
FOLDER=Path(__file__).resolve().parent

def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path)
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    return module

assert hashlib.sha256((FOLDER/'old_snapshot.py').read_bytes()).hexdigest()=='2b422d58b6e06ffa19286938118ecfe967027bc538eb6eb9ae5d5b01d5b0ab81'
assert hashlib.sha256((FOLDER/'new_snapshot.py').read_bytes()).hexdigest()=='d32fdd516a437c4470e296fd20c558ae2ae5055089d6480d32be4aa182bad962'
assert (FOLDER/'old_snapshot.py').read_bytes()!=(FOLDER/'new_snapshot.py').read_bytes()
old=load('old_snapshot',FOLDER/'old_snapshot.py')
new=load('new_snapshot',FOLDER/'new_snapshot.py')

class SnapshotControls(unittest.TestCase):
    def setUp(self):
        parent=ROOT/'Logs/SkirmishProduction/Offline/ParallelSnapshotFixtures';parent.mkdir(parents=True,exist_ok=True)
        self.root=Path(tempfile.mkdtemp(prefix='small-',dir=parent))
        assert self.root.resolve().is_relative_to(parent.resolve())
        for name in ['Assets','Packages','ProjectSettings','Library/ScriptAssemblies']:(self.root/name).mkdir(parents=True,exist_ok=True)
        for i,name in enumerate(['Assets/a.cs','Assets/Nested/Z.META','Packages/package.json','ProjectSettings/ProjectSettings.asset',
            'Assets/Unicode/é.cs','Assets/Unicode/\U00010000.shader','Assets/Unicode/\ue000.prefab','Assets/long'+('x'*130)+'.cs']):
            p=self.root/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(bytes([i])*((i+1)*137))
        (self.root/'Assets/ignored.txt').write_text('excluded by exact predecessor scope')
        for i in range(3):(self.root/f'Library/ScriptAssemblies/{i}.dll').write_bytes(bytes([i])*2048)

    def test_exact_manifest_candidate_build_and_cli_output_bytes(self):
        expected=old.snapshot(self.root);actual=new.snapshot(self.root)
        self.assertEqual(expected,actual);self.assertEqual(old.candidate_hash(expected),new.candidate_hash(actual))
        self.assertEqual(old.build_snapshot(self.root),new.build_snapshot(self.root))
        outputs=[]
        for name,script in [('old',FOLDER/'old_snapshot.py'),('new',FOLDER/'new_snapshot.py')]:
            path=self.root/(name+'.json')
            run=subprocess.run([sys.executable,str(script),'--project',str(self.root),'--output',str(path),'--capture-only'],capture_output=True,text=True)
            self.assertEqual(0,run.returncode,run.stderr);self.assertIn('result=CapturedUnqualified',run.stdout);outputs.append(path.read_bytes())
        self.assertEqual(*outputs)

    def test_required_tree_and_missing_build_refusal(self):
        (self.root/'Packages').rename(self.root/'Packages.absent')
        for module in [old,new]:
            with self.assertRaisesRegex(ValueError,'Required source tree absent: Packages'):module.snapshot(self.root)
        for module in [old,new]:
            with self.assertRaisesRegex(ValueError,'Compiled ScriptAssemblies absent'):module.build_snapshot(self.root/'Assets')

    def test_hash_read_failure_propagates_without_success(self):
        original=new._file_row
        def failing(project,path):
            if path.name=='a.cs':raise PermissionError('injected exact source read refusal')
            return original(project,path)
        new._file_row=failing
        try:
            with self.assertRaisesRegex(PermissionError,'exact source read refusal'):new.snapshot(self.root)
        finally:new._file_row=original

    def test_build_read_failure_propagates(self):
        original=new._file_row
        def failing(project,path):
            if path.name=='1.dll':raise OSError('injected DLL disappeared')
            return original(project,path)
        new._file_row=failing
        try:
            with self.assertRaisesRegex(OSError,'DLL disappeared'):new.build_snapshot(self.root)
        finally:new._file_row=original

    def test_bounded_workers_and_out_of_order_completion_parity(self):
        original=new._file_row;lock=threading.Lock();active=maximum=0;finished=[]
        paths=list((self.root/'Assets').rglob('*.cs'))*12
        def delayed(project,path):
            nonlocal active,maximum
            with lock:active+=1;maximum=max(maximum,active)
            try:
                time.sleep(.003 if path.name=='a.cs' else .001)
                return original(project,path)
            finally:
                with lock:active-=1;finished.append(path)
        new._file_row=delayed
        try:actual=new._hash_paths(self.root,paths)
        finally:new._file_row=original
        self.assertEqual([original(self.root,p) for p in paths],actual)
        self.assertGreater(maximum,1);self.assertLessEqual(maximum,8);self.assertEqual(0,active)
        self.assertEqual(len(paths),len(finished));self.assertEqual(32,new.MAX_PENDING_HASHES)

    def test_no_hash_cache_after_file_mutation(self):
        first=new.snapshot(self.root);(self.root/'Assets/a.cs').write_bytes(b'changed')
        second=new.snapshot(self.root)
        self.assertNotEqual(first,second);self.assertEqual(old.snapshot(self.root),second)
        self.assertNotEqual(new.candidate_hash(first),new.candidate_hash(second))

    def test_job_mismatch_keeps_snapshot_and_no_pass(self):
        job=self.root/'job.json';job.write_text(json.dumps({'source_manifest':[],'candidate_sha256':'0'*64,'build_sha256':'0'*64}))
        path=self.root/'refused.json'
        run=subprocess.run([sys.executable,str(FOLDER/'new_snapshot.py'),'--project',str(self.root),'--output',str(path),'--job',str(job)],capture_output=True,text=True)
        self.assertNotEqual(0,run.returncode);self.assertNotIn('result=Passed',run.stdout)
        self.assertIn('Complete source bytes differ',run.stderr);self.assertTrue(path.is_file())
        self.assertEqual(old.snapshot(self.root),json.loads(path.read_text())['source_manifest'])

if __name__=='__main__':unittest.main(verbosity=2)

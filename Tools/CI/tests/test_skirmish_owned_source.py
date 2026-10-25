"""Owned source execution fixtures only: no compiler, Unity or gameplay world."""
import hashlib
import json
import os
import py_compile
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

CI=Path(__file__).resolve().parents[1]
FILES=['skirmish_owned_source.py','run_skirmish_source_simulation.py',
       'skirmish_source_syntax.py','skirmish_production_worker.py','skirmish_causal_worker.py',
       'skirmish_source_manifest.py','skirmish_source_build.py','skirmish_source_authoring.py',
       'SkirmishSourceSimulation/Placement/regenerate_placement_numeric.py',
       'SkirmishSourceSimulation/Placement/build_source_placement_domain.py']
BOOT='''import runpy,sys,json,hashlib,types
from pathlib import Path
root=Path(sys.argv[1]);sys.dont_write_bytecode=True
namespace=runpy.run_path(str(root/'run_skirmish_source_simulation.py'),run_name='qualified_fixture')
owned=namespace['owned']
'''


class OwnedSourceTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)/'package';self.copy_ci(self.root)

    def copy_ci(self,root):
        for name in FILES:
            path=root/name;path.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(CI/name,path)

    def run_worker(self,body):
        result=subprocess.run([sys.executable,'-c',BOOT+body,str(self.root)],capture_output=True,text=True,timeout=10)
        self.assertEqual(result.returncode,0,result.stdout+result.stderr)
        return json.loads(result.stdout)if result.stdout.strip()else None

    def stale(self,name,old,new,body):
        path=self.root/name;raw=path.read_bytes();fresh=raw.decode().replace(old,new).encode()
        self.assertNotEqual(raw,fresh);self.assertEqual(len(raw),len(fresh))
        os.utime(path,ns=(1700000000125000000,1700000000125000000));cache=Path(py_compile.compile(str(path),doraise=True));cache_sha=hashlib.sha256(cache.read_bytes()).hexdigest()
        path.write_bytes(fresh);os.utime(path,ns=(1700000000875000000,1700000000875000000));answer=self.run_worker(body)
        self.assertEqual(hashlib.sha256(cache.read_bytes()).hexdigest(),cache_sha)
        self.assertEqual(answer['pin'],hashlib.sha256(fresh).hexdigest())
        return answer

    def test_current_manifest_bytes_ignore_valid_stale_pyc(self):
        result=self.stale('skirmish_source_manifest.py','def sha(raw):return hashlib.sha256(raw).hexdigest()','def sha(raw):return hashlib.sha512(raw).hexdigest()',"m=owned.require('skirmish_source_manifest');print(json.dumps({'value':m.sha(b'probe'),'pin':m.MODULE_SOURCE_SHA}))")
        self.assertEqual(result['value'],hashlib.sha512(b'probe').hexdigest())

    def test_raw_bootstrap_ignores_stale_loader_pyc_and_pins_itself(self):
        path=self.root/'skirmish_owned_source.py';path.write_bytes(path.read_bytes()+b"\nLOADER_MARKER='OLD'\n")
        result=self.stale('skirmish_owned_source.py',"LOADER_MARKER='OLD'","LOADER_MARKER='NEW'","print(json.dumps({'value':owned.LOADER_MARKER,'pin':owned.pins()[str((root/'skirmish_owned_source.py').resolve())]}))")
        self.assertEqual(result['value'],'NEW')

    def test_current_build_bytes_ignore_valid_stale_pyc(self):
        result=self.stale('skirmish_source_build.py',"'bytes':len(raw)","'bytex':len(raw)","m=owned.require('skirmish_source_build');p=root/'data';p.write_bytes(b'x');print(json.dumps({'value':list(m.pin(p)),'pin':owned.pins()[str((root/'skirmish_source_build.py').resolve())]}))")
        self.assertIn('bytex',result['value']);self.assertNotIn('bytes',result['value'])

    def test_current_syntax_bytes_ignore_valid_stale_pyc(self):
        result=self.stale('skirmish_source_syntax.py','return a,k','return a,j',"m=owned.require('skirmish_source_syntax');print(json.dumps({'value':m.method_span('void F() { return; }','void F()'),'pin':owned.pins()[str((root/'skirmish_source_syntax.py').resolve())]}))")
        self.assertEqual(result['value'],[0,9])

    def test_current_placement_bytes_ignore_valid_stale_pyc(self):
        result=self.stale('SkirmishSourceSimulation/Placement/build_source_placement_domain.py','SourcePlacementDomain.dll','SourcePlacementDomAin.dll',"m=owned.load_placement(root/'SkirmishSourceSimulation/Placement');print(json.dumps({'value':[x for x in m.build.__code__.co_consts if isinstance(x,str)and x.startswith('SourcePlacement')and x.endswith('.dll')],'pin':owned.pins()[str((root/'SkirmishSourceSimulation/Placement/build_source_placement_domain.py').resolve())]}))")
        self.assertTrue(result['value']);self.assertTrue(all(x=='SourcePlacementDomAin.dll'for x in result['value']))

    def test_transitive_placement_helper_uses_current_bytes(self):
        path=self.root/'SkirmishSourceSimulation/Placement/regenerate_placement_numeric.py';path.write_text('def generate(*args):return "current"\n')
        result=self.run_worker("m=owned.load_placement(root/'SkirmishSourceSimulation/Placement');print(json.dumps({'value':m.generate(None),'pin':owned.pins()[str((root/'SkirmishSourceSimulation/Placement/regenerate_placement_numeric.py').resolve())]}))")
        self.assertEqual(result['value'],'current');self.assertEqual(result['pin'],hashlib.sha256(path.read_bytes()).hexdigest())

    def test_same_epoch_reuse_and_self_loader_pin(self):
        self.run_worker("before=owned.require('skirmish_source_build');owned.load_cli(root/'.');assert owned.require('skirmish_source_build')is before;assert owned.pins()[str((root/'skirmish_owned_source.py').resolve())]==hashlib.sha256((root/'skirmish_owned_source.py').read_bytes()).hexdigest()")

    def test_source_drift_before_qualification_poisons_epoch(self):
        self.run_worker("p=root/'skirmish_source_syntax.py';raw=p.read_bytes();p.write_bytes(raw+b'\\n');\ntry:owned.pins();raise AssertionError('drift admitted')\nexcept owned.OwnedSourceError:pass\np.write_bytes(raw)\ntry:owned.pins();raise AssertionError('poison cleared')\nexcept owned.OwnedSourceError:pass")

    def test_changed_module_binding_rejected(self):
        self.run_worker("sys.modules['skirmish_source_manifest']=types.ModuleType('wrong');\ntry:owned.pins();raise AssertionError('unowned binding admitted')\nexcept owned.OwnedSourceError:pass")

    def test_changed_origin_rejected(self):
        self.run_worker("owned.require('skirmish_source_manifest').__file__=str(root/'skirmish_source_syntax.py');\ntry:owned.pins();raise AssertionError('wrong origin admitted')\nexcept owned.OwnedSourceError:pass")

    def test_alternate_placement_package_records_actual_origins(self):
        alternate=Path(self.temp.name)/'different-package';self.copy_ci(alternate)
        body="other=Path("+repr(str(alternate))+");owned.load_placement(other/'SkirmishSourceSimulation/Placement');pins=owned.pins();assert str((other/'SkirmishSourceSimulation/Placement/regenerate_placement_numeric.py').resolve())in pins;assert str((other/'SkirmishSourceSimulation/Placement/build_source_placement_domain.py').resolve())in pins;assert str((root/'skirmish_source_manifest.py').resolve())in pins"
        self.run_worker(body)

    def test_wrong_cli_root_rejected(self):
        alternate=Path(self.temp.name)/'different-package';self.copy_ci(alternate)
        self.run_worker("\ntry:owned.load_cli(Path("+repr(str(alternate))+"));raise AssertionError('root changed')\nexcept owned.OwnedSourceError:pass")

    def failure_worker(self,body):
        # Isolated helper bootstrap, deliberately before the admitted Syntax load.
        code="""import sys,types,hashlib
from pathlib import Path
root=Path(sys.argv[1]);path=root/'skirmish_owned_source.py';raw=path.read_bytes();owned=types.ModuleType('skirmish_owned_source');owned.__file__=str(path);owned.__executed_source_pin__={'path':str(path),'sha256':hashlib.sha256(raw).hexdigest()};sys.modules[owned.__name__]=owned;exec(compile(raw,str(path),'exec'),owned.__dict__)
"""+body
        result=subprocess.run([sys.executable,'-c',code,str(self.root)],capture_output=True,text=True,timeout=10);self.assertEqual(result.returncode,0,result.stdout+result.stderr)

    def test_unowned_collision_fails_closed(self):
        self.failure_worker("sys.modules['skirmish_source_syntax']=types.ModuleType('skirmish_source_syntax');\ntry:owned.load_cli(root);raise AssertionError('collision admitted')\nexcept owned.OwnedSourceError:pass\ntry:owned.pins();raise AssertionError('failed epoch reused')\nexcept owned.OwnedSourceError:pass")

    def test_failed_execution_cleanup_and_poison(self):
        (self.root/'skirmish_source_syntax.py').write_text("raise RuntimeError('fixture failure')\n")
        self.failure_worker("\ntry:owned.load_cli(root);raise AssertionError('failed exec admitted')\nexcept RuntimeError:pass\nassert 'skirmish_source_syntax'not in sys.modules\ntry:owned.pins();raise AssertionError('failed epoch reused')\nexcept owned.OwnedSourceError:pass")

    def test_execution_source_drift_cleanup_and_poison(self):
        (self.root/'skirmish_source_syntax.py').write_text("from pathlib import Path\nPath(__file__).write_text('# drift')\n")
        self.failure_worker("\ntry:owned.load_cli(root);raise AssertionError('drift admitted')\nexcept owned.OwnedSourceError:pass\nassert 'skirmish_source_syntax'not in sys.modules\ntry:owned.pins();raise AssertionError('failed epoch reused')\nexcept owned.OwnedSourceError:pass")

    def test_missing_required_module_cannot_qualify_partial_epoch(self):
        (self.root/'skirmish_production_worker.py').unlink()
        self.failure_worker("\ntry:owned.load_cli(root);raise AssertionError('missing module admitted')\nexcept FileNotFoundError:pass\ntry:owned.pins();raise AssertionError('partial epoch qualified')\nexcept owned.OwnedSourceError:pass")

    def test_read_growth_beyond_stat_bound_rejects_before_execution(self):
        self.failure_worker("from unittest import mock\noriginal=Path.read_bytes\ndef read(path):\n return b'x'*(2*1024*1024+1)if path.name=='skirmish_source_syntax.py'else original(path)\nwith mock.patch.object(Path,'read_bytes',read):\n try:owned.load_cli(root);raise AssertionError('read growth admitted')\n except owned.OwnedSourceError:pass\nassert 'skirmish_source_syntax'not in sys.modules")

    def test_bootstrap_loader_read_is_bounded(self):
        path=self.root/'skirmish_owned_source.py';path.write_bytes(path.read_bytes()+b'#'*(2*1024*1024))
        code="import runpy,sys\ntry:runpy.run_path(sys.argv[1],run_name='bounded_fixture');raise AssertionError('oversize bootstrap admitted')\nexcept ValueError as error:assert 'Bounded owned loader'in str(error)"
        result=subprocess.run([sys.executable,'-c',code,str(self.root/'run_skirmish_source_simulation.py')],capture_output=True,text=True,timeout=10);self.assertEqual(result.returncode,0,result.stdout+result.stderr)


if __name__=='__main__':unittest.main()

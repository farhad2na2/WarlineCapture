import sys
import tempfile
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph'))
from source_s004_run_evidence import before,after,sha


class EvidenceTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.root=Path(self.tmp.name)
        self.source=self.root/'generated.cs';self.source.write_text('class A {}')
        self.rsp=self.root/'compile.rsp';self.rsp.write_text('response')
        self.exe=self.root/'host.exe';self.exe.write_bytes(b'executable')
    def tearDown(self): self.tmp.cleanup()
    def create(self): return before(self.root,{'input':'digest'},{'build':'digest'},['-nologo',str(self.source)],self.exe)
    def test_records_and_links_checkpoints(self):
        p=self.create();r=after(self.root,p);self.assertEqual(r['before_manifest_sha256'],sha(self.root/'source-pins-before.json'));self.assertEqual(r['generated_sources_rechecked'],1);self.assertFalse(r['mission_ready'])
    def test_generated_mutation_rejected(self):
        p=self.create();self.source.write_text('changed')
        with self.assertRaises(ValueError): after(self.root,p)
        self.assertFalse((self.root/'source-pins-after.json').exists())
    def test_response_mutation_rejected(self):
        p=self.create();self.rsp.write_text('changed')
        with self.assertRaises(ValueError): after(self.root,p)
    def test_executable_mutation_rejected(self):
        p=self.create();self.exe.write_bytes(b'changed')
        with self.assertRaises(ValueError): after(self.root,p)
    def test_cannot_overwrite_preexisting_evidence(self):
        self.create()
        with self.assertRaises(FileExistsError): self.create()
    def test_missing_projection_fails_closed(self):
        with self.assertRaises(ValueError): before(self.root,{}, {},['-nologo'],self.exe)

if __name__=='__main__': unittest.main()

"""Synthetic packet construction controls only; none are native qualification evidence."""
import json
from pathlib import Path
import tempfile
import unittest
from prepare_s005_north_job_packet import prepare,file_hash
from s005_windows_source_snapshot import snapshot,candidate_hash,build_snapshot
from skirmish_native_identity import from_native_census


class PacketControls(unittest.TestCase):
    def setUp(self):
        repository=Path(__file__).resolve().parents[2]
        parent=repository/"Temp/S005NorthPacketTests";parent.mkdir(parents=True,exist_ok=True)
        self.temp=tempfile.TemporaryDirectory(dir=parent,prefix="synthetic-");self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        for directory in ("Assets/Editor","Packages","ProjectSettings","Library/ScriptAssemblies","prepared"):
            (self.root/directory).mkdir(parents=True,exist_ok=True)
        self.observer=self.root/"Assets/Editor/Observer.cs";self.observer.write_bytes(b"synthetic observer only")
        self.collector=self.root/"Assets/Editor/Collector.cs";self.collector.write_bytes(b"synthetic collector only")
        self.dll=self.root/"Library/ScriptAssemblies/Game.Editor.dll";self.dll.write_bytes(b"synthetic DLL only")
        self.packet=self.root/"packet.md";self.packet.write_text("## S005\nRegular seeds `104734`, `130368`, `155926` in both EN and FA at Standard.\n",encoding="utf-8")
        self.identity=from_native_census(catalog_id="S005",definition_id="skirmish.s005",definition_version=2,difficulty="Regular",size="Standard",
            seed=155926,native_locale="fa-IR",native_setup_hash_uint32=3245678901,native_content_identity="synthetic.native.content.only")
        self.census=self.root/"native-census.json"
        self.write(self.census,dict(schema_version=1,collector_kind="SkirmishNativeCompiledSetupCensus",native_setup_identity=self.identity,
            actual_native_locale="fa-IR",native_config_sha256=file_hash(self.collector)))
        manifest=snapshot(self.root);builds,build_hash=build_snapshot(self.root)
        self.capture=self.root/"source.json"
        self.write(self.capture,dict(source_manifest=manifest,candidate_sha256=candidate_hash(manifest),build_manifest=builds,build_sha256=build_hash))
        draft=self.root/"prepared/Observer.cs.txt";draft.write_bytes(self.observer.read_bytes())
        self.preparation=self.root/"prepared/PREPARATION.json"
        self.write(self.preparation,dict(runId="1632",candidate=draft.name,candidateSha256=file_hash(draft),nativeIdentityFieldsCaptured=True))
        self.log=self.root/"census.log";self.log.write_text("[SyntheticCensus] result=Passed\n[UnityInvoke] ExitCode: 0\n",encoding="utf-8")
    def write(self,path,value):path.write_text(json.dumps(value),encoding="utf-8")
    def run_builder(self):
        return prepare(self.root,self.census,self.capture,self.packet,self.preparation,self.observer,self.collector,self.log,
                       "[SyntheticCensus] result=Passed","S005-synthetic-test",self.root/"packet-output")
    def test_packet_derives_real_shapes_not_hash_padding(self):
        result=self.run_builder()
        job=json.loads((self.root/"packet-output/job.json").read_text())
        self.assertEqual(result["native_qualification"],"pending")
        self.assertFalse(result["gameplay_started"])
        self.assertNotEqual(job["setup_sha256"],str(self.identity["native_setup_hash_uint32"]).zfill(64))
        self.assertEqual(job["source_manifest"],json.loads(self.capture.read_text())["source_manifest"])
        with self.assertRaises(ValueError):self.run_builder()
    def test_actual_census_locale_and_wrapper_checks(self):
        value=json.loads(self.census.read_text());value["actual_native_locale"]="en";self.write(self.census,value)
        with self.assertRaises(ValueError):self.run_builder()
        value["actual_native_locale"]="fa-IR";self.write(self.census,value)
        self.log.write_text("[SyntheticCensus] result=Passed\n",encoding="utf-8")
        with self.assertRaises(ValueError):self.run_builder()
    def test_source_build_and_observer_changes_reject(self):
        original=self.dll.read_bytes();self.dll.write_bytes(b"changed DLL")
        with self.assertRaises(ValueError):self.run_builder()
        self.dll.write_bytes(original)
        self.observer.write_bytes(b"changed observer")
        with self.assertRaises(ValueError):self.run_builder()
    def test_incomplete_source_and_legacy_observer_reject(self):
        capture=json.loads(self.capture.read_text());capture["source_manifest"].pop();capture["candidate_sha256"]=candidate_hash(capture["source_manifest"]);self.write(self.capture,capture)
        with self.assertRaises(ValueError):self.run_builder()
        # Fresh complete capture, but old observer lacks actual native identity fields.
        manifest=snapshot(self.root);capture["source_manifest"]=manifest;capture["candidate_sha256"]=candidate_hash(manifest);self.write(self.capture,capture)
        value=json.loads(self.preparation.read_text());value["nativeIdentityFieldsCaptured"]=False;self.write(self.preparation,value)
        with self.assertRaises(ValueError):self.run_builder()


if __name__=="__main__":unittest.main(verbosity=2)

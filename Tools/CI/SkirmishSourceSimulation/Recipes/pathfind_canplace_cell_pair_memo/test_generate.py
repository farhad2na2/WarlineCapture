import hashlib, importlib.util, json, tempfile, unittest
from pathlib import Path

HERE=Path(__file__).resolve().parent
SPEC=importlib.util.spec_from_file_location("cell_pair_memo",HERE/"generate.py")
M=importlib.util.module_from_spec(SPEC);SPEC.loader.exec_module(M)

class CellPairMemoControls(unittest.TestCase):
    def setUp(self):
        self.job=(M.ROOT/M.JOB).read_text()
        self.base=M.load_base()
        with tempfile.TemporaryDirectory() as td:
            import importlib.util
            rp=M.ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/regenerate_path_partials.py"
            spec=importlib.util.spec_from_file_location("cell_pair_test_resolver",rp);r=importlib.util.module_from_spec(spec);spec.loader.exec_module(r)
            r.generate(M.ROOT,Path(td)/"out")
            self.validator=(Path(td)/"out"/"UnitPathPlacementValidation.cs").read_text()

    def test_key_is_only_target_current_and_hit_shadow_recomputes_original(self):
        out=M.transform_validator(self.validator)
        self.assertIn("new MemoKey(cell,currentCell)",out)
        key=out[out.index("private readonly struct MemoKey"):out.index("private static bool CanPlaceForPathingOriginal")]
        self.assertIn("int2 target,current",key)
        for excluded in ("NativeArray", "Entity", "Footprint", "Faction", "vehicle", "manual", "GridConfig"):
            self.assertNotIn(excluded,key)
        self.assertIn("bool actual=CanPlaceForPathingOriginal(",out)
        self.assertIn("if(actual!=cached)",out)
        self.assertIn("SourceHostMemoCapacity=4096",out)
        self.assertIn("else sourceHostFallbacks++",out)

    def test_pinned_job_requires_fixed_context_and_sole_call(self):
        M.prove_fixed_context(self.job)
        for changed in (self.job.replace(M.JOB_CONTEXT,"",1),
                        self.job.replace(M.JOB_CALL,M.JOB_CALL.replace("Job.LiveUnitCoverage","default"),1),
                        self.job+"\n// UnitPathPlacementValidation.CanPlaceForPathing(\n"):
            with self.assertRaisesRegex(ValueError,"fixed-context, route, and sole CanPlace"):
                M.prove_fixed_context(changed)

    def test_generation_proof_pins_job_outputs_native_direct_and_limits(self):
        job_path=M.ROOT/M.JOB; before=job_path.read_bytes()
        with tempfile.TemporaryDirectory() as td:
            dest=Path(td)/"candidate"; proof=M.generate(M.ROOT,dest)
            saved=json.loads((dest/"source-extraction.json").read_text())
            self.assertEqual(saved["source_pins"][str(job_path)],hashlib.sha256(before).hexdigest())
            self.assertEqual(saved["outputs"],proof["outputs"])
            self.assertEqual(set(saved["outputs"]),{"192_UnitPathPlacementValidation.CellPairMemo.cs","119-123_OwnedSystemRuntime.CellPairMemo.cs"})
            for name,digest in saved["outputs"].items(): self.assertEqual(hashlib.sha256((dest/name).read_bytes()).hexdigest(),digest)
            details=saved["source_host_cell_pair_memo"]
            self.assertEqual(details["key"],"target x/z plus current x/z only")
            self.assertEqual(details["capacity"],4096)
            self.assertIn("no concurrent writer",details["concurrency"])
            self.assertEqual(details["native_support"],"none; source-host-only managed dictionary overlay; no native/Burst support")
            self.assertTrue(saved["pathfind_batch_job_native_direct_preserved"])
            self.assertFalse(saved["build_manifest_changed"])
            self.assertFalse(saved["mission_run"])
        self.assertEqual(before,job_path.read_bytes())

if __name__=="__main__": unittest.main()

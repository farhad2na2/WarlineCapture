import hashlib, importlib.util, tempfile, unittest, json, runpy
from pathlib import Path
HERE=Path(__file__).resolve().parent
SPEC=importlib.util.spec_from_file_location("memo_boundary",HERE/"generate.py")
M=importlib.util.module_from_spec(SPEC);SPEC.loader.exec_module(M)

class OwnedBoundaryMemoControls(unittest.TestCase):
    def setUp(self):
        self.validator=(M.ROOT/M.VALIDATOR).read_text();self.backend=(M.ROOT/M.BACKEND).read_text()
    def test_validator_has_full_key_bounded_fallback_and_shadow(self):
        out=M.transform_validator(self.validator)
        self.assertEqual(out.count("PathPlacementKernel.CanPlaceForPathingWithCoverage("),1)
        for field in ("walkable", "dynamicBlocked", "friendlyPassFactionIds", "occupied", "liveUnitEntities", "liveUnitGrids", "liveUnitFootprints", "liveUnitManualGroupMembers", "movingEntity", "cell", "footprintSize", "currentCell", "isVehicle", "manualMove", "factionId", "ignoredOccupancyEntity", "ignoredOccupancyCell", "ignoredOccupancySize", "unitCoverage", "grid.Origin"):
            self.assertIn(field,out)
        self.assertIn("SourceHostMemoCapacity=4096",out)
        self.assertIn("if(sourceHostMemo.Count<SourceHostMemoCapacity)",out)
        self.assertIn("bool actual=CanPlaceForPathingOriginal(",out)
        self.assertIn("actual!=cached",out)
        self.assertIn("sourceHostMemo.Clear();throw new System.InvalidOperationException",out)
        self.assertIn("return actual;",out)
        self.assertIn("sourceHostHits,sourceHostMisses,sourceHostFallbacks,sourceHostMismatches",out)
        self.assertIn("Nested source-host CanPlace memo scope",out)
    def test_backend_scopes_only_pathfind_execute_and_always_clears(self):
        out=M.transform_backend(self.backend)
        self.assertEqual(out.count('m.Invoke(boxed,new object[]{i})'),1)
        self.assertIn('typeof(T).FullName=="Game.Runtime.PathfindBatchJob"',out)
        self.assertIn('Environment.GetEnvironmentVariable("WARLINE_SOURCE_CANPLACE_MEMO")=="1"',out)
        self.assertIn("BeginSourceHostMemo(shadow);try{m.Invoke",out)
        self.assertIn("finally{if(memo)Game.Runtime.UnitPathPlacementValidation.EndSourceHostMemo();}",out)
        self.assertIn('Environment.GetEnvironmentVariable("WARLINE_SOURCE_CANPLACE_MEMO_SHADOW")=="1"',out)
    def test_both_anchors_fail_closed_and_inputs_remain_unmodified(self):
        with self.assertRaisesRegex(ValueError,"original body anchor"):
            M.transform_validator(self.validator.replace(M.VALIDATOR_ANCHOR,"",1))
        with self.assertRaisesRegex(ValueError,"S026 IJobFor boundary"):
            M.transform_backend(self.backend.replace(M.ORIGINAL,"",1))
        self.assertEqual(self.validator,(M.ROOT/M.VALIDATOR).read_text())
        self.assertEqual(self.backend,(M.ROOT/M.BACKEND).read_text())
    def test_output_hashes_and_receipt_preserve_native_direct_path_source(self):
        originals={p:(M.ROOT/p).read_bytes() for p in (M.VALIDATOR,M.BACKEND)}
        pathjob=(M.ROOT/"Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs").read_bytes()
        with tempfile.TemporaryDirectory() as tmp:
            rec=M.generate(M.ROOT,Path(tmp)/"candidate")
            generated=Path(tmp)/"candidate"
            hashes=rec["source_host_memo_overlay"]["generated_output_sha256"]
            for name,digest in hashes.items():
                self.assertEqual(hashlib.sha256((generated/name).read_bytes()).hexdigest(),digest)
            proof=json.loads((generated/"source-extraction.json").read_text())
            self.assertTrue(proof["experimental_only"])
            self.assertIn("unverified",proof["compile_status"])
            resolver=runpy.run_path(str(M.ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/regenerate_path_partials.py"))
            self.assertEqual(proof["required_current_links"],[str(M.ROOT/p) for p in resolver["REQUIRED_CURRENT_LINKS"]])
            self.assertEqual(proof["source_inputs"],rec["source_inputs"])
            self.assertEqual(set(proof["outputs"]),{"192_UnitPathPlacementValidation.cs","119-123_OwnedSystemRuntime.SourceHostMemo.cs"})
            self.assertEqual(proof["outputs"]["192_UnitPathPlacementValidation.cs"],rec["source_host_memo_overlay"]["generated_output_sha256"]["192_UnitPathPlacementValidation.cs"])
            self.assertEqual(proof["outputs"]["119-123_OwnedSystemRuntime.SourceHostMemo.cs"],rec["source_host_memo_overlay"]["generated_output_sha256"]["119-123_OwnedSystemRuntime.SourceHostMemo.cs"])
            self.assertEqual(proof["source_pins"],{**proof["source_inputs"],str(M.ROOT/M.BACKEND):proof["source_host_memo_overlay"]["backend_source_sha256"]})
            self.assertTrue((generated/"192_UnitPathPlacementValidation.cs").is_file())
            self.assertFalse((generated/"UnitPathPlacementValidation.cs").exists())
            self.assertIn("allocations may occur",proof["source_host_memo_overlay"]["allocations"])
            self.assertTrue(rec["pathfind_batch_job_native_direct_preserved"])
            self.assertFalse(rec["build_manifest_changed"])
        self.assertEqual(pathjob,(M.ROOT/"Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs").read_bytes())
        for p,raw in originals.items():self.assertEqual(raw,(M.ROOT/p).read_bytes())

if __name__=="__main__":unittest.main()

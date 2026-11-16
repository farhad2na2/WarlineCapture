import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
SPEC = importlib.util.spec_from_file_location("surface_memo_recipe", HERE / "generate.py")
M = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(M)


class SurfaceMemoRecipeControls(unittest.TestCase):
    def setUp(self):
        self.source = (M.ROOT / M.SOURCE).read_text()

    def test_exact_surface_only_request_scope_and_shadow(self):
        candidate = M.transform(self.source)
        self.assertEqual(candidate.count("GridPathRoutingKernel.Route(ref context, request, ref state);"), 1)
        self.assertIn("new Dictionary<int2,bool>()", candidate)
        self.assertIn("FactionId = Factions[index],\n                    SurfaceCellMemo =", candidate)
        self.assertIn("SurfaceCellMemo?.Clear()", candidate)
        self.assertIn("SurfaceFootprintMemo?.Clear()", candidate)
        self.assertIn("SurfaceCellMemo.Count<4096", candidate)
        self.assertIn("SurfaceFootprintMemo.Count<4096", candidate)
        self.assertIn("bool actual=IsSurfaceCellPathableOriginal(x,z)", candidate)
        self.assertIn("bool actual=IsSurfaceFootprintPathableOriginal(x,z)", candidate)
        self.assertIn("Job.IsSurfaceCellPathable(new int2(x,z),IsVehicle)", candidate)
        self.assertIn("Job.IsSurfaceFootprintPathable(new int2(x,z),FootprintSize,IsVehicle)", candidate)
        self.assertIn("UnitPathPlacementValidation.CanPlaceForPathing(", candidate)
        self.assertNotIn("[BurstCompile]", candidate)
        self.assertFalse("CanPlaceMemo" in candidate)
        self.assertEqual(self.source, (M.ROOT / M.SOURCE).read_text())

    def test_anchor_mismatch_fails_closed(self):
        for anchor in (M.ANCHOR_CONTEXT, M.ANCHOR_ROUTE, M.ANCHOR_CELL, M.ANCHOR_FOOTPRINT):
            with self.assertRaisesRegex(ValueError, "exact PathfindBatchJob surface-memo anchors"):
                M.transform(self.source.replace(anchor, "", 1))

    def test_generation_pins_candidate_identity_and_preserves_input(self):
        path = M.ROOT / M.SOURCE
        before = path.read_bytes()
        with tempfile.TemporaryDirectory() as temp:
            receipt = M.generate(M.ROOT, Path(temp) / "candidate")
            candidate = (Path(temp) / "candidate" / receipt["candidate"]).read_bytes()
            self.assertEqual(hashlib.sha256(candidate).hexdigest(), receipt["candidate_sha256"])
            self.assertEqual("sha256(candidate bytes)", receipt["output_identity"])
            self.assertFalse(receipt["dynamic_placement_touched"])
            self.assertFalse(receipt["route_algorithm_changed"])
            self.assertFalse(receipt["burst_compile_attribute_in_candidate"])
            self.assertFalse(receipt["build_inputs_changed"])
            evidence = receipt["snapshot_lifetime_evidence"]
            self.assertIn("one Execute/request", evidence["claim"])
            self.assertEqual(set(map(str, M.SNAPSHOT_SOURCES)), set(evidence["source_pins"]))
            for relative, digest in evidence["source_pins"].items():
                self.assertEqual(hashlib.sha256((M.ROOT / relative).read_bytes()).hexdigest(), digest)
        self.assertEqual(before, path.read_bytes())

    def test_resume_manifest_preparation_replaces_only_job_source(self):
        base_path = M.ROOT / M.DEFAULT_RESUME_MANIFEST
        base_bytes = base_path.read_bytes()
        base = json.loads(base_bytes)
        with tempfile.TemporaryDirectory() as temp:
            output = Path(temp) / "source-manifest.json"
            candidate = "T Logs/SkirmishProduction/isolated/PathfindBatchJob.SurfaceMemo.SourceHost.cs"
            receipt = {
                "candidate_sha256": "a" * 64,
                "snapshot_lifetime_evidence": {"source_pins": {"source": "b" * 64}},
            }
            prepared = M.prepare_manifest(M.ROOT, M.DEFAULT_RESUME_MANIFEST, candidate, output, receipt)
            result = json.loads(output.read_bytes())
            self.assertEqual(prepared["base_manifest_sha256"], hashlib.sha256(base_bytes).hexdigest())
            self.assertTrue(result["experimental_only"])
            self.assertEqual(result["source_host_surface_memo_overlay"]["candidate_sha256"], "a" * 64)
            changed = {row["id"]: row.get("path") for row in result["sources"]}
            original = {row["id"]: row.get("path") for row in base["sources"]}
            differences = {key for key in original if original[key] != changed[key]}
            self.assertEqual(differences, {"130_PathfindBatchJob.cs"})
        self.assertEqual(base_bytes, base_path.read_bytes())


if __name__ == "__main__":
    unittest.main()

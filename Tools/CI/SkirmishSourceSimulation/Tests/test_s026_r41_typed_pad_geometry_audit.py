from __future__ import annotations

import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[4]
MODULE_PATH = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s026_alias_owner_candidate/audit_r41_typed_pad_geometry.py"
SPEC = importlib.util.spec_from_file_location("audit_r41_typed_pad_geometry", MODULE_PATH)
AUDIT = importlib.util.module_from_spec(SPEC)
assert SPEC is not None and SPEC.loader is not None
SPEC.loader.exec_module(AUDIT)


class S026R41TypedPadGeometryAuditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.report = AUDIT.read_json(ROOT / AUDIT.REPORT_REL)
        cls.typed = cls.report["sharedTankWaypointsWithCandidateFacilityBlockers"]["typedPadHypotheses"]
        cls.facilities = AUDIT.locate_exact_facility_rectangles(cls.report)
        grid = cls.report["nativeSourceGrid"]
        bounds = cls.report["aliasPlayableBounds"]
        cls.grid_origin = tuple(grid["origin"])
        cls.cell_size = grid["cellSize"]
        cls.bounds = (bounds["minX"], bounds["minZ"], bounds["maxX"], bounds["maxZ"])

    def test_receipt_is_repeatable(self) -> None:
        first = AUDIT.build_receipt(ROOT)
        second = AUDIT.build_receipt(ROOT)
        encoded_first = json.dumps(first, sort_keys=True, indent=2, ensure_ascii=False) + "\n"
        encoded_second = json.dumps(second, sort_keys=True, indent=2, ensure_ascii=False) + "\n"
        self.assertEqual(encoded_first, encoded_second)

    def test_rejects_report_digest_drift(self) -> None:
        with tempfile.TemporaryDirectory(prefix="s026-r41-audit-") as directory:
            changed = Path(directory) / "report.json"
            changed.write_bytes((ROOT / AUDIT.REPORT_REL).read_bytes() + b" ")
            with self.assertRaisesRegex(ValueError, "r41 report hash mismatch"):
                AUDIT.build_receipt(ROOT, changed)

    def test_original_two_variants_pass_geometry(self) -> None:
        self.assertEqual(len(self.facilities), 20)
        audited = [AUDIT.variant_audit(v, self.facilities, self.grid_origin, self.cell_size, self.bounds)
                   for v in self.typed["variants"]]
        self.assertEqual([v["requestedActors"] for v in audited], [52, 52])
        self.assertTrue(all(not v["padFacilityOverlapPairs"] and not v["actorFacilityOverlapPairs"]
                            and not v["actorFootprintOverlapPairs"] and not v["padPadOverlapPairs"]
                            for v in audited))

    def test_rejects_pad_facility_overlap(self) -> None:
        variant = copy.deepcopy(self.typed["variants"][0])
        rect = self.facilities[0][1]
        variant["candidatePads"][0]["CenterX"] = (rect[0] + rect[2]) / 2
        variant["candidatePads"][0]["CenterZ"] = (rect[1] + rect[3]) / 2
        with self.assertRaisesRegex(ValueError, "Candidate pad intersects"):
            AUDIT.variant_audit(variant, self.facilities, self.grid_origin, self.cell_size, self.bounds)

    def test_rejects_actor_footprint_overlap(self) -> None:
        variant = copy.deepcopy(self.typed["variants"][0])
        row = next(r for r in variant["packingRows"] if len(r["candidateCells"]) > 1)
        row["candidateCells"][1]["x"] = row["candidateCells"][0]["x"]
        row["candidateCells"][1]["z"] = row["candidateCells"][0]["z"]
        with self.assertRaisesRegex(ValueError, "Actor footprints overlap"):
            AUDIT.variant_audit(variant, self.facilities, self.grid_origin, self.cell_size, self.bounds)

    def test_rejects_duplicate_pad_overlap(self) -> None:
        variant = copy.deepcopy(self.typed["variants"][0])
        variant["candidatePads"][1]["CenterX"] = variant["candidatePads"][0]["CenterX"]
        variant["candidatePads"][1]["CenterZ"] = variant["candidatePads"][0]["CenterZ"]
        with self.assertRaisesRegex(ValueError, "Candidate pads overlap"):
            AUDIT.variant_audit(variant, self.facilities, self.grid_origin, self.cell_size, self.bounds)


if __name__ == "__main__":
    unittest.main()

import json
import tempfile
import unittest
from pathlib import Path

from attribute_s073_occupancy import attribute, footprint_set, source_census_sets


ROOT = Path(__file__).resolve().parents[4]
EVIDENCE = ROOT / "Logs/SkirmishProduction/s073-native-occupancy-census-20261101-r1"
SOURCE = ROOT / "Logs/SkirmishProduction/s073-blocker-occupancy-comparison-20261101-r1/source-run/run"


class S073OccupancyAttributionTests(unittest.TestCase):
    def test_kernel_footprint_set_uses_row_major_cells(self):
        census = {"grid_width": 4, "grid_height": 3, "entity_count": 1, "entities": [{
            "entity": [1, 2], "cell": [1, 0], "authored_footprint": [2, 1],
            "kernel_footprint": [2, 1], "kernel_clamped_bounds_min": [1, 0],
            "kernel_clamped_bounds_max_exclusive": [3, 1],
            "faction_id": 0, "out_of_bounds_footprint_cells": 0,
            "occupancy_missing_footprint_cells": 0, "in_bounds_footprint_cells": 2,
            "footprint_cells": [
                {"x": 1, "y": 0, "row_major_index": 1, "occupancy_bit_set": True},
                {"x": 2, "y": 0, "row_major_index": 2, "occupancy_bit_set": True},
            ],
        }]}
        self.assertEqual(footprint_set(census), {1, 2})
        self.assertEqual(footprint_set(census, 1), set())

    def test_source_census_rejects_nonproduction_cell_rows(self):
        census = {
            "kind": "S073SourceDynamicOccupancyContributorCensus",
            "scope": "source-host-read-only-post-original-owner-sample",
            "source_state_imported": False,
            "activation_or_admission_claimed": False,
            "readiness_claimed": False,
            "query": {"all": ["UnitGrid", "UnitFootprint"],
                      "none": ["StaticGridBlocker", "RuntimeBuildingCombatTag", "UnitDeathAnimationComponent"],
                      "entity_query_default_exclusions": ["Prefab", "Disabled"],
                      "actual_system": "DynamicOccupancyRebuildSystem._trackedUnitsQuery"},
            "grid": {"width": 2048, "height": 1024, "cell_count": 2097152},
            "contributor_count": 1,
            "contributors_with_faction_component": 1,
            "contributors": [{"entity_index": 1, "entity_version": 1, "cell": [1, 0],
                              "footprint": [1, 1], "faction_id": 1, "source_prefab_key": "test",
                              "covered_row_major_cell_indices_from_production_bounds": [2]}],
            "actual_occupied_cells": [1], "actual_occupied_cell_count": 1,
            "reconstructed_occupied_cell_count": 1, "actual_vs_reconstructed_cell_mismatch_count": 0,
        }
        with self.assertRaisesRegex(ValueError, "differ from production kernel bounds"):
            source_census_sets(census)

    def test_real_evidence_reproduces_faction_attribution(self):
        result = attribute(
            EVIDENCE / "snapshot.json", EVIDENCE / "receipt.json",
            Path("/private/tmp/warline-s073-occupancy-census-20261101-r1/native-blocker-occupancy-pre-enqueue-r1.bin"),
            EVIDENCE / "native-blocker-occupancy-pre-enqueue-r1.bin.json",
            Path("/private/tmp/s073-source-raw-export-r20/run/source-blocker-occupancy-r1.bin"),
            Path("/private/tmp/s073-source-raw-export-r20/run/source-blocker-occupancy-r1.bin.json"),
            SOURCE / "run-receipt.json", EVIDENCE / "comparison-with-source-r20.json",
            ROOT / "Logs/SkirmishProduction/s073-source-occupancy-census-run-20261101-r2/result.json",
            ROOT / "Logs/SkirmishProduction/s073-source-occupancy-census-run-20261101-r2/run-receipt.json")
        relation = result["set_relationship"]
        self.assertEqual(relation["native_extra_vs_source_cells"], 109)
        self.assertEqual(relation["source_extra_vs_native_cells"], 0)
        self.assertTrue(relation["faction_0_absent_cells_are_all_native_extras"])
        self.assertTrue(relation["faction_1_and_2_union_equals_source"])
        self.assertFalse(result["causal_or_source_ownership_inference"])
        source_census = result["source_contributor_census"]
        self.assertEqual(source_census["contributor_count"], 24)
        self.assertEqual(source_census["faction_occupied_cell_counts"], {"0": 0, "1": 44, "2": 44})
        self.assertEqual(source_census["source_raw_capture_overlap"]["intersection_cells"], 88)
        self.assertFalse(source_census["source_raw_capture_overlap"]["same_source_session"])
        self.assertEqual(source_census["source_graph_coordinate_ownership"], "Unknown")

    def test_modified_comparator_fails_closed(self):
        original = json.loads((EVIDENCE / "comparison-with-source-r20.json").read_text())
        original["native_metrics"]["occupied_cells"] += 1
        with tempfile.TemporaryDirectory() as temp:
            tampered = Path(temp) / "comparison.json"
            tampered.write_text(json.dumps(original))
            with self.assertRaisesRegex(ValueError, "not reproducible"):
                attribute(
                    EVIDENCE / "snapshot.json", EVIDENCE / "receipt.json",
                    Path("/private/tmp/warline-s073-occupancy-census-20261101-r1/native-blocker-occupancy-pre-enqueue-r1.bin"),
                    EVIDENCE / "native-blocker-occupancy-pre-enqueue-r1.bin.json",
                    Path("/private/tmp/s073-source-raw-export-r20/run/source-blocker-occupancy-r1.bin"),
                    Path("/private/tmp/s073-source-raw-export-r20/run/source-blocker-occupancy-r1.bin.json"),
                    SOURCE / "run-receipt.json", tampered)


if __name__ == "__main__":
    unittest.main()

import copy
import unittest

from compare_s073_combined_blockers import compare


class CombinedBlockerComparisonTests(unittest.TestCase):
    def setUp(self):
        row = {"history_bound": True, "min": [1, 2], "max": [3, 4], "friendly_pass_faction": 255}
        domain = {"histories_bound_to_grid": 4932, "static_domain_count": 4932,
                  "source_building_count": 4931, "blocked_mask_sha256": "mask", "other_blockers": [row]}
        self.native = ({"status": "Passed", "post_stream": {"complete_static_blocker_observation": domain}}, "native")
        self.receipt = {"validation_status": "Passed", "wrapper_exit_code": 0, "required_marker_present": True, "census_sha256": "native"}
        self.host = ({"mission_ready": False, "experimental_physical_blocker_projection": {"combined_after_starting_buildings": copy.deepcopy(domain)}}, "host")
        self.host_receipt = {"worker_result": {"sha256": "host"}, "status": "PreparingPhysicalSourceRequired", "process_exit": 2}
        self.identity = {"status": "IdentitySetsMatch", "native_sha256": "native"}

    def run_compare(self):
        return compare(self.native, self.receipt, self.host, self.host_receipt, self.identity)

    def test_match_and_different_history(self):
        self.assertEqual(self.run_compare()["status"], "Match")
        self.host[0]["experimental_physical_blocker_projection"]["combined_after_starting_buildings"]["other_blockers"][0]["max"] = [4, 4]
        result = self.run_compare()
        self.assertEqual(result["status"], "Mismatch")
        self.assertEqual(len(result["native_only_histories"]), 1)
        self.assertFalse(result["mission_ready"])

    def test_rejects_unbound_or_stale(self):
        self.receipt["census_sha256"] = "wrong"
        with self.assertRaises(ValueError): self.run_compare()
        self.receipt["census_sha256"] = "native"
        self.host[0]["experimental_physical_blocker_projection"]["combined_after_starting_buildings"]["other_blockers"][0]["history_bound"] = False
        with self.assertRaises(ValueError): self.run_compare()


if __name__ == "__main__":
    unittest.main()

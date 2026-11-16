import copy
import unittest

from audit_s073_startup_scenery_bounds import audit


class StartupSceneryBoundsTests(unittest.TestCase):
    def setUp(self):
        row = {"static_history": {"min": [100, 100], "max": [110, 110], "bound_to_grid_epoch": True}}
        self.history = {"normalized_buildings": [copy.deepcopy(row) for _ in range(4931)]}
        request = {"status": 1, "request_id": 1, "actual_origin": [0, 0], "actual_footprint": [5, 5]}
        doc = {"mission": "S073", "mission_ready": False, "scheduler_tick_executed": False,
               "building_and_unit_startup_diagnostic": {"requests": [copy.deepcopy(request) for _ in range(10)]}}
        self.startup = (doc, "startup-sha")
        self.receipt = {"worker_result": {"sha256": "startup-sha"}, "status": "PreparingPhysicalSourceRequired", "process_exit": 2}
        self.comparison = {"status": "HistoriesMatch", "isolated_sha256": "history-sha", "building_count": 4931,
                           "full_grid_parity": False, "native_sha256": "native-sha"}

    def run_audit(self):
        return audit(self.startup, self.receipt, self.history, "history-sha", self.comparison)

    def test_disjoint_and_conservative_edge(self):
        self.assertEqual(self.run_audit()["requests"][0]["potential_source_bounds_overlaps"], 0)
        self.history["normalized_buildings"][0]["static_history"]["min"] = [5, 5]
        self.assertEqual(self.run_audit()["requests"][0]["potential_source_bounds_overlaps"], 1)
        self.assertFalse(self.run_audit()["source_fit_certified"])

    def test_rejects_stale_and_unbound_evidence(self):
        self.comparison["isolated_sha256"] = "wrong"
        with self.assertRaises(ValueError):
            self.run_audit()
        self.comparison["isolated_sha256"] = "history-sha"
        self.history["normalized_buildings"][0]["static_history"]["bound_to_grid_epoch"] = False
        with self.assertRaises(ValueError):
            self.run_audit()
        self.history["normalized_buildings"][0]["static_history"]["bound_to_grid_epoch"] = True
        self.receipt["worker_result"]["sha256"] = "wrong"
        with self.assertRaises(ValueError):
            self.run_audit()


if __name__ == "__main__":
    unittest.main()

import unittest
from compare_s073_startup_roads import compare


class RoadComparisonTests(unittest.TestCase):
    def setUp(self):
        self.log = ('[S073StartupRoadTrace] building=a origin=int2(1, 2) rect=(x:1, y:2, width:1, height:1) cached=1:2 directSurface=1:2 sidewalk=\n'
                    '[S073StartupRoadTrace] building=b origin=int2(3, 4) rect=(x:3, y:4, width:1, height:1) cached= directSurface= sidewalk=\n')
        self.receipt = {"validation_status": "Passed", "wrapper_exit_code": 0, "required_marker_present": True, "editor_log_sha256": "log"}
        sample = lambda x, y: {"origin": [x, y], "footprint": [1, 1], "captured_road_cells": [], "surface_road_cells": [], "sidewalk_cells": []}
        self.host = {"experimental_physical_blocker_projection": {"rejected_native_origin_road_inputs": [sample(1, 2), sample(3, 4)]}}
        self.hr = {"worker_result": {"sha256": "host"}, "status": "PreparingPhysicalSourceRequired", "process_exit": 2}

    def test_reports_missing_surface(self):
        result = compare(self.log, "log", self.receipt, self.host, "host", self.hr)
        self.assertEqual(result["samples"][0]["native_surface_missing_from_host"], [[1, 2]])
        self.assertFalse(result["mission_ready"])

    def test_rejects_stale_receipt(self):
        with self.assertRaises(ValueError): compare(self.log, "wrong", self.receipt, self.host, "host", self.hr)


if __name__ == "__main__": unittest.main()

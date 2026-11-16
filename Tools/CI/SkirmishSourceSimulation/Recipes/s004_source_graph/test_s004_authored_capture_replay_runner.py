import hashlib
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).parent))
import run_s004_authored_capture_replay as runner


def passed_result():
    return {
        "status": "Passed",
        "world_initialized": False,
        "physical_source_fit_verified": False,
        "mission_readiness": False,
        "compiler": [
            {"size": size, "setupHash": value, "nativeSetupHash": str(value), "matched": True}
            for size, value in (("Standard", 11), ("War", 22), ("LargeWar", 33))
        ],
    }


class S004AuthoredCaptureReplayRunnerTests(unittest.TestCase):
    def test_accepts_exact_three_way_compiler_match_with_unverified_readiness(self):
        runner.validate_result(passed_result())

    def test_rejects_source_host_boundary_even_when_other_fields_look_valid(self):
        result = passed_result()
        result["status"] = "SourceHostBoundary"
        with self.assertRaisesRegex(ValueError, "status is not Passed"):
            runner.validate_result(result)

    def test_rejects_missing_or_unmatched_compiler_rows(self):
        result = passed_result()
        result["compiler"][2]["matched"] = False
        with self.assertRaisesRegex(ValueError, "setup hashes must match"):
            runner.validate_result(result)

    def test_rejects_readiness_or_world_claims_and_missing_flags(self):
        for field, value in (("world_initialized", True), ("mission_readiness", True),
                             ("physical_source_fit_verified", True), ("mission_readiness", None)):
            result = passed_result()
            result[field] = value
            with self.subTest(field=field, value=value):
                with self.assertRaisesRegex(ValueError, "explicitly leave world"):
                    runner.validate_result(result)

    def test_graph_source_pins_are_nonempty_bounded_and_verified(self):
        with tempfile.TemporaryDirectory() as temp:
            source = Path(temp) / "asset"
            source.write_text("authored")
            good = {"source_pins": {str(source): hashlib.sha256(b"authored").hexdigest()}}
            runner.validate_graph_source_pins(good)
            for pins in ({}, {str(source): "not-a-sha256"},
                         {str(source) + str(i): hashlib.sha256(b"authored").hexdigest()
                          for i in range(513)}):
                with self.subTest(pin_count=len(pins)):
                    with self.assertRaises(ValueError):
                        runner.validate_graph_source_pins({"source_pins": pins})


if __name__ == "__main__":
    unittest.main()

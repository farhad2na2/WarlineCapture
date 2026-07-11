import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "aph802_preserve_editor_p95_run.py"
SPEC = importlib.util.spec_from_file_location("aph802_preserve", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class Aph802PreserveEditorP95RunTests(unittest.TestCase):
    def test_preserves_valid_pair_and_refuses_overwrite(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            baseline = root / "baseline.json"
            log = root / "run.log"
            baseline.write_text(json.dumps(self._baseline()), encoding="utf-8")
            log.write_text(MODULE.PASS_MARKER, encoding="utf-8")
            args = (baseline, log, root / "runs", "run-01", "a" * 40,
                    "2026-07-11T19:31:13Z", "Unity -executeMethod", self._environment())
            json_path, markdown_path = MODULE.preserve(*args)
            run = json.loads(json_path.read_text(encoding="utf-8"))
            self.assertEqual("Accepted", run["decision"]["status"])
            self.assertEqual(10.231, run["measurements"]["p95FrameMs"])
            self.assertIn("APH802-Artifact-Id: run-01", markdown_path.read_text(encoding="utf-8"))
            with self.assertRaisesRegex(MODULE.PreserveError, "already exists"):
                MODULE.preserve(*args)

    def test_rejects_missing_pass_marker(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            baseline = root / "baseline.json"
            log = root / "run.log"
            baseline.write_text(json.dumps(self._baseline()), encoding="utf-8")
            log.write_text("failed", encoding="utf-8")
            with self.assertRaisesRegex(MODULE.PreserveError, "pass marker"):
                MODULE.preserve(baseline, log, root / "runs", "run-01", "a" * 40,
                                "2026-07-11T19:31:13Z", "Unity", self._environment())

    @staticmethod
    def _baseline():
        return {
            "source": MODULE.RUNNER,
            "observationSeconds": 4.002,
            "frameCount": 653,
            "averageFrameMs": 6.15,
            "p95FrameMs": 10.231,
            "p99FrameMs": 15.127,
            "maxFrameMs": 29.218,
            "editorP95FrameBudgetPassed": True,
            "allocatedBytesCurrentThread": 0,
            "unitCount": 733,
            "runtimeBuildingCount": 628,
            "readyStatus": "matchSceneLoaded=1",
            "stableStatus": "sourceKeys=733",
        }

    @staticmethod
    def _environment():
        return {
            "unityVersion": "6000.5.2f1",
            "os": "macOS",
            "machineId": "test-machine",
            "graphicsDevice": "test-gpu",
            "qualityLevel": "Mobile",
            "resolution": {"width": 1, "height": 1},
            "captureMode": "windowed batchmode",
            "cacheStatePolicy": "retained cache; fresh process",
        }


if __name__ == "__main__":
    unittest.main()

#!/usr/bin/env python3

from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "architecture_performance_dashboard.py"
SPEC = importlib.util.spec_from_file_location("architecture_performance_dashboard", SCRIPT)
assert SPEC and SPEC.loader
dashboard = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = dashboard
SPEC.loader.exec_module(dashboard)


class ArchitecturePerformanceDashboardTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.revision = "a" * 40

    def tearDown(self) -> None:
        self.temp.cleanup()

    def write(self, relative: str, value: object) -> None:
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(value), encoding="utf-8")

    def test_missing_inputs_are_explicit_and_path_order_is_deterministic(self) -> None:
        result = dashboard.build_dashboard(self.root, self.revision)
        paths = [item["path"] for item in result["inputs"]]
        self.assertEqual(paths, sorted(paths))
        self.assertEqual(result["summary"]["freshnessCounts"]["missing"], len(dashboard.INPUTS))
        self.assertTrue(all(item["reasons"] for item in result["inputs"]))

    def test_current_stale_dirty_unknown_and_invalid_states_are_distinct(self) -> None:
        specs = dashboard.INPUTS
        self.write(specs[0].path, {"exactCommit": self.revision, "dirty": False, "summary": {"z": 2}})
        self.write(specs[1].path, {"exactCommit": "b" * 40, "snapshots": []})
        self.write(specs[2].path, {"exactCommit": self.revision, "dirty": True, "snapshots": []})
        self.write(specs[3].path, {"artifactBytes": 7})
        invalid = self.root / specs[4].path
        invalid.parent.mkdir(parents=True, exist_ok=True)
        invalid.write_text("{", encoding="utf-8")

        result = dashboard.build_dashboard(self.root, self.revision)
        by_path = {item["path"]: item for item in result["inputs"]}
        self.assertEqual(by_path[specs[0].path]["freshness"], "current")
        self.assertEqual(by_path[specs[1].path]["freshness"], "stale")
        self.assertEqual(by_path[specs[2].path]["freshness"], "stale")
        self.assertEqual(by_path[specs[3].path]["freshness"], "unknown")
        self.assertEqual(by_path[specs[4].path]["freshness"], "invalid")

    def test_metrics_are_derived_from_source_json(self) -> None:
        spec = next(item for item in dashboard.INPUTS if item.category == "runtime-performance")
        self.write(spec.path, {"averageFrameMs": 8.25, "frameCount": 120, "stableStatus": "ready"})
        item = next(
            entry for entry in dashboard.build_dashboard(self.root, self.revision)["inputs"]
            if entry["path"] == spec.path
        )
        self.assertEqual(item["metrics"], {"averageFrameMs": 8.25, "frameCount": 120})

    def test_audio_snapshot_metrics_use_phase_and_ignore_nested_clip_rows(self) -> None:
        spec = next(item for item in dashboard.INPUTS if item.category == "audio")
        self.write(spec.path, {
            "snapshots": [{
                "phase": "steady",
                "catalogRuntimeMemoryBytes": 99,
                "loadedCatalogClipCount": 3,
                "catalogClips": [{"runtimeMemoryBytes": 44}],
            }]
        })
        item = next(
            entry for entry in dashboard.build_dashboard(self.root, self.revision)["inputs"]
            if entry["path"] == spec.path
        )
        self.assertEqual(item["metrics"], {
            "snapshotCount": 1,
            "steady.catalogRuntimeMemoryBytes": 99,
            "steady.loadedCatalogClipCount": 3,
        })

    def test_json_and_markdown_outputs_are_byte_deterministic(self) -> None:
        first_json = "out/first.json"
        first_md = "out/first.md"
        second_json = "out/second.json"
        second_md = "out/second.md"
        dashboard.write_dashboard(self.root, first_json, first_md, self.revision)
        dashboard.write_dashboard(self.root, second_json, second_md, self.revision)
        self.assertEqual((self.root / first_json).read_bytes(), (self.root / second_json).read_bytes())
        self.assertEqual((self.root / first_md).read_bytes(), (self.root / second_md).read_bytes())

    def test_markdown_contains_missing_and_stale_reasons(self) -> None:
        spec = dashboard.INPUTS[0]
        self.write(spec.path, {"exactCommit": "c" * 40, "summary": {"assemblyCount": 18}})
        rendered = dashboard.render_markdown(dashboard.build_dashboard(self.root, self.revision))
        self.assertIn("**stale**", rendered)
        self.assertIn("does not match dashboard revision", rendered)
        self.assertIn("**missing**", rendered)


if __name__ == "__main__":
    unittest.main()

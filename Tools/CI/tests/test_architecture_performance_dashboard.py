#!/usr/bin/env python3

from __future__ import annotations

import importlib.util
import json
import subprocess
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
        self.environment_hash = "e" * 64
        self.owner_path = "owner.py"
        (self.root / self.owner_path).write_text("# owner\n", encoding="utf-8")
        self.environment_path = "environment.json"
        (self.root / self.environment_path).write_text("environment\n", encoding="utf-8")
        self.environment_hash = dashboard.sha256_bytes(
            (self.root / self.environment_path).read_bytes()
        )
        self.registry = self.make_registry()
        self.write_registry()

    def tearDown(self) -> None:
        self.temp.cleanup()

    def make_registry(self) -> dict[str, object]:
        inputs = [
            self.input("advisory-audio", "audio.json", "advisory", "deferred", "audio", "not-required"),
            self.input("advisory-build", "build.json", "advisory", "deferred", "build", "not-required"),
            self.input("advisory-summary", "summary.json", "advisory", "deferred", "summary", "not-required"),
            self.input("required-performance", "performance.json", "required", "active", "performance", "exact-environment"),
            self.input("required-summary", "architecture.json", "required", "active", "summary", "exact-environment"),
        ]
        return {
            "artifactId": "AM-005-TEST",
            "baseline": {"commit": self.revision, "tree": "b" * 40},
            "environmentIdentity": {
                "path": self.environment_path,
                "sha256": self.environment_hash,
            },
            "evidenceInputs": inputs,
            "schemaVersion": 1,
            "validators": [{
                "id": "owner",
                "laneState": "active",
                "owner": {"path": self.owner_path, "selector": "Owner.Run"},
                "responsibilities": ["test-responsibility"],
            }],
        }

    @staticmethod
    def input(
        input_id: str,
        path: str,
        requirement: str,
        lane_state: str,
        metric_reader: str,
        environment_policy: str,
    ) -> dict[str, object]:
        required_fields = {
            "audio": ["snapshots"],
            "build": ["artifactBytes", "status"],
            "performance": [
                "editorP95FrameBudgetMs",
                "editorP95FrameBudgetPassed",
                "frameCount",
                "p95FrameMs",
            ],
            "summary": ["summary"],
        }[metric_reader]
        return {
            "category": metric_reader,
            "environmentPolicy": environment_policy,
            "id": input_id,
            "laneState": lane_state,
            "metricReader": metric_reader,
            "ownerValidatorId": "owner",
            "path": path,
            "requiredFields": required_fields,
            "requirement": requirement,
            "revisionPolicy": "exact-commit",
        }

    def write_registry(self) -> None:
        self.write(dashboard.DEFAULT_REGISTRY, self.registry)

    def write(self, relative: str, value: object) -> None:
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    def current(self, value: dict[str, object] | None = None) -> dict[str, object]:
        result: dict[str, object] = {
            "dirty": False,
            "environmentIdentitySha256": self.environment_hash,
            "exactCommit": self.revision,
        }
        result.update(value or {})
        return result

    def populate_all_current(self) -> None:
        for item in self.registry["evidenceInputs"]:
            assert isinstance(item, dict)
            value = self.current()
            if item["metricReader"] == "audio":
                value["snapshots"] = []
            elif item["metricReader"] == "build":
                value.update({"artifactBytes": 100, "status": "complete"})
            elif item["metricReader"] == "performance":
                value.update({
                    "editorP95FrameBudgetMs": 50.0,
                    "editorP95FrameBudgetPassed": True,
                    "frameCount": 120,
                    "p95FrameMs": 8.25,
                })
            elif item["metricReader"] == "summary":
                value["summary"] = {"count": 1}
            self.write(str(item["path"]), value)

    def test_missing_inputs_are_explicit_sorted_and_fail_required_gate(self) -> None:
        result = dashboard.build_dashboard(self.root, self.revision)
        paths = [item["path"] for item in result["inputs"]]
        self.assertEqual(paths, sorted(paths))
        self.assertEqual(result["summary"]["freshnessCounts"]["missing"], 5)
        self.assertEqual(result["summary"]["requiredRejectedCount"], 2)
        self.assertEqual(result["gateState"], "rejected")
        self.assertTrue(all(item["reasons"][0]["code"] == "MISSING_INPUT" for item in result["inputs"]))

    def test_current_stale_dirty_unknown_and_malformed_states_are_distinct(self) -> None:
        self.write("architecture.json", self.current({"summary": {"z": 2}}))
        self.write("performance.json", self.current({
            "editorP95FrameBudgetMs": 50.0,
            "editorP95FrameBudgetPassed": True,
            "exactCommit": "b" * 40,
            "frameCount": 120,
            "p95FrameMs": 8.25,
        }))
        self.write("audio.json", self.current({"dirty": True, "snapshots": []}))
        self.write("build.json", {"artifactBytes": 7, "status": "complete"})
        (self.root / "summary.json").write_text("{", encoding="utf-8")

        result = dashboard.build_dashboard(self.root, self.revision)
        by_path = {item["path"]: item for item in result["inputs"]}
        self.assertEqual(by_path["architecture.json"]["freshness"], "current")
        self.assertEqual(by_path["performance.json"]["freshness"], "stale")
        self.assertEqual(by_path["audio.json"]["freshness"], "stale")
        self.assertEqual(by_path["build.json"]["freshness"], "unknown")
        self.assertEqual(by_path["summary.json"]["freshness"], "malformed")

    def test_required_environment_is_exact_and_mismatch_is_stale(self) -> None:
        self.populate_all_current()
        self.write("architecture.json", self.current({
            "environmentIdentitySha256": "f" * 64,
            "summary": {"assemblyCount": 12},
        }))
        result = dashboard.build_dashboard(self.root, self.revision)
        item = next(entry for entry in result["inputs"] if entry["path"] == "architecture.json")
        self.assertEqual(item["freshness"], "stale")
        self.assertIn("ENVIRONMENT_MISMATCH", {entry["code"] for entry in item["reasons"]})
        self.assertEqual(result["gateState"], "rejected")

    def test_schema_valid_json_without_required_measurements_is_malformed(self) -> None:
        self.populate_all_current()
        self.write("architecture.json", self.current())
        result = dashboard.build_dashboard(self.root, self.revision)
        item = next(entry for entry in result["inputs"] if entry["path"] == "architecture.json")
        self.assertEqual(item["freshness"], "malformed")
        self.assertEqual(item["reasons"][0]["code"], "MALFORMED_REQUIRED_FIELDS")
        self.assertEqual(result["gateState"], "rejected")

    def test_advisory_stale_input_does_not_reject_green_required_inputs(self) -> None:
        self.populate_all_current()
        self.write("audio.json", self.current({"exactCommit": "c" * 40, "snapshots": []}))
        result = dashboard.build_dashboard(self.root, self.revision)
        self.assertEqual(result["summary"]["requiredRejectedCount"], 0)
        self.assertEqual(result["summary"]["advisoryAttentionCount"], 1)
        self.assertEqual(result["gateState"], "accepted")

    def test_duplicate_responsibility_owner_rejects_registry(self) -> None:
        validators = self.registry["validators"]
        assert isinstance(validators, list)
        validators.append({
            "id": "owner-two",
            "laneState": "active",
            "owner": {"path": self.owner_path, "selector": "OwnerTwo.Run"},
            "responsibilities": ["test-responsibility"],
        })
        self.write_registry()
        result = dashboard.build_dashboard(self.root, self.revision)
        codes = {item["code"] for item in result["registry"]["errors"]}
        self.assertIn("DUPLICATE_RESPONSIBILITY_OWNER", codes)
        self.assertEqual(result["gateState"], "rejected")

    def test_duplicate_evidence_path_owner_rejects_registry(self) -> None:
        inputs = self.registry["evidenceInputs"]
        assert isinstance(inputs, list)
        duplicate = dict(inputs[-1])
        duplicate["id"] = "z-duplicate"
        inputs.append(duplicate)
        self.write_registry()
        result = dashboard.build_dashboard(self.root, self.revision)
        codes = {item["code"] for item in result["registry"]["errors"]}
        self.assertIn("DUPLICATE_INPUT_OWNER", codes)
        self.assertEqual(result["gateState"], "rejected")

    def test_registry_revision_mismatch_rejects_dashboard(self) -> None:
        baseline = self.registry["baseline"]
        assert isinstance(baseline, dict)
        baseline["commit"] = "d" * 40
        self.write_registry()
        result = dashboard.build_dashboard(self.root, self.revision)
        codes = {item["code"] for item in result["registry"]["errors"]}
        self.assertIn("REGISTRY_REVISION_MISMATCH", codes)
        self.assertEqual(result["gateState"], "rejected")

    def test_metrics_are_derived_from_source_json(self) -> None:
        self.write("performance.json", self.current({
            "averageFrameMs": 8.25,
            "editorP95FrameBudgetMs": 50.0,
            "editorP95FrameBudgetPassed": True,
            "frameCount": 120,
            "p95FrameMs": 8.25,
            "stableStatus": "ready",
        }))
        item = next(
            entry for entry in dashboard.build_dashboard(self.root, self.revision)["inputs"]
            if entry["path"] == "performance.json"
        )
        self.assertEqual(item["metrics"], {
            "averageFrameMs": 8.25,
            "editorP95FrameBudgetMs": 50.0,
            "editorP95FrameBudgetPassed": True,
            "frameCount": 120,
            "p95FrameMs": 8.25,
        })

    def test_audio_snapshot_metrics_use_phase_and_ignore_nested_clip_rows(self) -> None:
        self.write("audio.json", self.current({
            "snapshots": [{
                "phase": "steady",
                "catalogRuntimeMemoryBytes": 99,
                "loadedCatalogClipCount": 3,
                "catalogClips": [{"runtimeMemoryBytes": 44}],
            }]
        }))
        item = next(
            entry for entry in dashboard.build_dashboard(self.root, self.revision)["inputs"]
            if entry["path"] == "audio.json"
        )
        self.assertEqual(item["metrics"], {
            "snapshotCount": 1,
            "steady.catalogRuntimeMemoryBytes": 99,
            "steady.loadedCatalogClipCount": 3,
        })

    def test_json_markdown_and_registry_render_are_byte_deterministic(self) -> None:
        dashboard.write_dashboard(
            self.root, "out/first.json", "out/first.md", self.revision,
            registry_markdown_path="out/first-registry.md",
        )
        dashboard.write_dashboard(
            self.root, "out/second.json", "out/second.md", self.revision,
            registry_markdown_path="out/second-registry.md",
        )
        self.assertEqual((self.root / "out/first.json").read_bytes(), (self.root / "out/second.json").read_bytes())
        self.assertEqual((self.root / "out/first.md").read_bytes(), (self.root / "out/second.md").read_bytes())
        self.assertEqual(
            (self.root / "out/first-registry.md").read_bytes(),
            (self.root / "out/second-registry.md").read_bytes(),
        )

    def test_check_mode_exits_nonzero_for_rejected_required_evidence(self) -> None:
        result = subprocess.run(
            [
                sys.executable,
                str(SCRIPT),
                "--root", str(self.root),
                "--revision", self.revision,
                "--json-output", "out/dashboard.json",
                "--markdown-output", "out/dashboard.md",
                "--registry-markdown-output", "out/registry.md",
                "--check",
            ],
            check=False,
            capture_output=True,
            text=True,
        )
        self.assertEqual(result.returncode, 2)
        self.assertIn("result=Rejected", result.stdout)

    def test_cli_requires_explicit_revision_before_writing_outputs(self) -> None:
        result = subprocess.run(
            [
                sys.executable,
                str(SCRIPT),
                "--root", str(self.root),
                "--json-output", "out/dashboard.json",
                "--markdown-output", "out/dashboard.md",
            ],
            check=False,
            capture_output=True,
            text=True,
        )
        self.assertEqual(result.returncode, 2)
        self.assertIn("--revision", result.stderr)
        self.assertFalse((self.root / "out/dashboard.json").exists())

    def test_markdown_contains_machine_reason_codes_and_gate_state(self) -> None:
        self.write("architecture.json", self.current({
            "exactCommit": "c" * 40,
            "summary": {"assemblyCount": 18},
        }))
        rendered = dashboard.render_markdown(dashboard.build_dashboard(self.root, self.revision))
        self.assertIn("Gate: **rejected**", rendered)
        self.assertIn("`REVISION_MISMATCH`", rendered)
        self.assertIn("**missing**", rendered)


if __name__ == "__main__":
    unittest.main()

#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import importlib.util
import io
import json
from collections import Counter
import subprocess
import sys
import tarfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / "Tools/CI/architecture_dependency_hazard_inventory.py"
INVENTORY_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/am018_dependency_hazard_inventory.json"
sys.path.insert(0, str(SCRIPT.parent))
SPEC = importlib.util.spec_from_file_location("architecture_dependency_hazard_inventory_evidence", SCRIPT)
assert SPEC and SPEC.loader
inventory = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = inventory
SPEC.loader.exec_module(inventory)


def git_text(*arguments: str) -> str:
    result = subprocess.run(
        ["git", *arguments],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    return result.stdout.strip()


def archive_files(commit: str, paths: list[str]) -> dict[str, bytes]:
    result = subprocess.run(
        ["git", "archive", "--format=tar", commit, *paths],
        cwd=ROOT,
        check=True,
        capture_output=True,
    )
    files: dict[str, bytes] = {}
    with tarfile.open(fileobj=io.BytesIO(result.stdout), mode="r:") as archive:
        for member in archive.getmembers():
            if not member.isfile():
                continue
            extracted = archive.extractfile(member)
            assert extracted is not None
            files[member.name] = extracted.read()
    return files


def sha256(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


class ArchitectureDependencyHazardInventoryEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.data = json.loads(INVENTORY_PATH.read_text(encoding="utf-8"))
        baseline = cls.data["baseline"]
        archive_paths = [inventory.SOURCE_ROOT, *inventory.AUTHORITY_PATHS]
        cls.archived = archive_files(baseline["commit"], archive_paths)
        cls.archived_exclusions = inventory.parse_active_exclusions(
            cls.archived["Design/AgentReports/ArchitectureMaturity/ownership_inventory.json"]
        )

    def test_baseline_identity_is_exact_and_integrated(self) -> None:
        baseline = self.data["baseline"]
        self.assertEqual(
            git_text("rev-parse", "--verify", f"{baseline['commit']}^{{commit}}"),
            baseline["commit"],
        )
        self.assertEqual(git_text("rev-parse", f"{baseline['commit']}^{{tree}}"), baseline["tree"])
        ancestor = subprocess.run(
            ["git", "merge-base", "--is-ancestor", baseline["commit"], "HEAD"],
            cwd=ROOT,
            capture_output=True,
        )
        self.assertEqual(ancestor.returncode, 0)

    def test_source_manifest_is_complete_and_hash_bound_to_baseline(self) -> None:
        expected = {
            path: content
            for path, content in self.archived.items()
            if path.startswith(f"{inventory.SOURCE_ROOT}/")
            and path.endswith(".cs")
            and "/Editor/" not in path
        }
        manifest = self.data["sourceManifest"]
        self.assertEqual(manifest["fileCount"], len(expected))
        self.assertEqual([row["path"] for row in manifest["files"]], sorted(expected))
        for row in manifest["files"]:
            self.assertEqual(row["sha256"], sha256(expected[row["path"]]), row["path"])
        self.assertEqual(manifest["digestSha256"], inventory.source_manifest_digest(manifest["files"]))

    def test_authorities_and_findings_recompute_from_baseline_bytes(self) -> None:
        self.assertEqual(
            [authority["path"] for authority in self.data["sourceAuthorities"]],
            list(inventory.AUTHORITY_PATHS),
        )
        for authority in self.data["sourceAuthorities"]:
            self.assertEqual(authority["sha256"], sha256(self.archived[authority["path"]]), authority["path"])
        sources = [
            (path, content.decode("utf-8"))
            for path, content in self.archived.items()
            if path.startswith(f"{inventory.SOURCE_ROOT}/")
            and path.endswith(".cs")
            and "/Editor/" not in path
        ]
        self.assertEqual(self.data["activeWorkExclusions"], self.archived_exclusions)
        recomputed = inventory.scan_sources(sources, self.archived_exclusions)
        self.assertEqual(recomputed, self.data["categories"])

    def test_tool_manifest_binds_generator_and_acceptance_tests(self) -> None:
        self.assertEqual(
            [row["path"] for row in self.data["toolManifest"]],
            list(inventory.TOOL_PATHS),
        )
        for row in self.data["toolManifest"]:
            self.assertEqual(row["sha256"], sha256((ROOT / row["path"]).read_bytes()), row["path"])

    def test_summary_and_routing_are_fail_closed(self) -> None:
        expected_counts = {
            "globalWorldLookups": 74,
            "hiddenSingletons": 8,
            "mutableStaticCaches": 143,
            "runtimeObjectDiscovery": 4,
            "staticEventSubscriptions": 8,
        }
        self.assertEqual(self.data["schemaVersion"], 2)
        self.assertEqual(self.data["summary"]["findingCount"], 237)
        self.assertEqual(self.data["summary"]["protectedFindingCount"], 12)
        self.assertEqual(self.data["summary"]["mutableStaticCacheCandidateCount"], 54)
        self.assertEqual(self.data["summary"]["mutableStaticLifecycleStateCount"], 30)
        self.assertEqual(self.data["summary"]["immutableReferenceClassificationCount"], 59)
        for category, expected in expected_counts.items():
            self.assertEqual(len(self.data["categories"][category]), expected, category)
            self.assertEqual(self.data["summary"][f"{category}Count"], expected, category)
            self.assertTrue(all(
                row["followUpTasks"] == inventory.follow_up_tasks(category, row["disposition"])
                for row in self.data["categories"][category]
            ))
            self.assertTrue(all(row["responsibleOwner"] == row["ownerType"] for row in self.data["categories"][category]))
            self.assertTrue(all(row["disposition"] and row["rationale"] for row in self.data["categories"][category]))
        self.assertEqual(
            Counter(row["disposition"] for row in self.data["categories"]["globalWorldLookups"]),
            {"AD": 15, "CE": 32, "HSL": 2, "PE": 25},
        )
        self.assertEqual(
            Counter(row["disposition"] for row in self.data["categories"]["staticEventSubscriptions"]),
            {"EIP": 7, "ETO": 1},
        )
        self.assertEqual(
            Counter(row["disposition"] for row in self.data["categories"]["runtimeObjectDiscovery"]),
            {"AD": 2, "ROD": 2},
        )
        self.assertEqual(
            [owner["id"] for owner in self.data["activeWorkExclusions"]],
            sorted(owner["id"] for owner in self.data["activeWorkExclusions"]),
        )

    def test_markdown_is_deterministic_projection(self) -> None:
        markdown = ROOT / "Design/AgentReports/ArchitectureMaturity/am018_dependency_hazard_inventory.md"
        self.assertEqual(markdown.read_text(encoding="utf-8"), inventory.render_markdown(self.data))


if __name__ == "__main__":
    unittest.main()

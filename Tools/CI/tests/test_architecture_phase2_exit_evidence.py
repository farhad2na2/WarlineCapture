import copy
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "Tools/CI"))

import architecture_phase2_ownership_delta as delta


def artifact(artifact_id, categories, **extra):
    return {"artifactId": artifact_id, "categories": categories, **extra}


def lifecycle_fixture():
    return artifact(
        "AM-007",
        {
            "nativeContainers": [{"path": "Assets\\A.cs", "ownerType": "Owner", "field": "Cells", "line": 10}],
            "presentationPools": [],
            "queryCaches": [],
            "sceneRoots": [],
            "staticCaches": [{"path": "./Assets/B.cs", "ownerType": "Cache", "field": "Rows", "line": 20}],
            "subscriptions": [],
            "worlds": [],
        },
        policy={"candidateSemantics": "Unmapped lifecycle candidates require explicit follow-up."},
    )


def hazards_fixture():
    return artifact(
        "AM-018",
        {
            "globalWorldLookups": [{
                "path": "Assets/C.cs", "ownerType": "Edge", "memberName": "Boot", "symbol": "World.Default",
                "accessKind": "global-world-property", "disposition": "CE", "rationale": "Explicit composition edge.",
                "protectedOwnerIds": [], "line": 30,
            }],
            "hiddenSingletons": [],
            "mutableStaticCaches": [{
                "path": "Assets/D.cs", "ownerType": "State", "memberName": "<member>", "symbol": "Cache",
                "disposition": "MSL", "rationale": "Needs a lifecycle owner.", "protectedOwnerIds": [], "line": 40,
            }],
            "runtimeObjectDiscovery": [],
            "staticEventSubscriptions": [],
        },
        classification={
            "globalWorldLookups": "Explicit World access classifications.",
            "hiddenSingletons": "Explicit singleton classifications.",
            "mutableStaticCaches": "Explicit static-state classifications.",
            "runtimeObjectDiscovery": "Explicit discovery classifications.",
            "staticEventSubscriptions": "Explicit subscription classifications.",
        },
    )


def ownership_fixture():
    return artifact(
        "AM-021",
        {
            "eventSubscriptions": [],
            "persistentNativeContainers": [{
                "path": "Assets/A.cs", "ownerType": "Owner", "field": "Cells", "status": "explicit",
                "protectedOwnerIds": [], "line": 100,
            }, {
                "path": "Assets/New.cs", "ownerType": "NewOwner", "field": "Data", "status": "explicit",
                "protectedOwnerIds": [], "line": 101,
            }],
            "persistentQueries": [],
            "presentationRoots": [],
        },
    )


class Phase2OwnershipDeltaTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.lifecycle = self.write("lifecycle.json", lifecycle_fixture())
        self.hazards = self.write("hazards.json", hazards_fixture())
        self.ownership = self.write("ownership.json", ownership_fixture())

    def tearDown(self):
        self.temp.cleanup()

    def write(self, name, value):
        path = self.root / name
        path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        return path

    def report(self):
        return delta.build_report(self.lifecycle, self.hazards, self.ownership)

    def test_rows_are_unique_and_classified_exactly_once(self):
        report = self.report()
        rows = report["baselineClassifications"] + report["hazardClassifications"]
        keys = [(row["sourceArtifact"], row["sourceKey"]) for row in rows]
        self.assertEqual(len(keys), len(set(keys)))
        self.assertEqual({"resolved", "open"}, {row["classification"] for row in rows})
        self.assertEqual(len(rows), sum(report["summary"]["classificationCounts"].values()))

    def test_identical_baseline_candidates_receive_line_independent_occurrences(self):
        lifecycle = lifecycle_fixture()
        duplicate = copy.deepcopy(lifecycle["categories"]["staticCaches"][0])
        duplicate["line"] = 999
        lifecycle["categories"]["staticCaches"].append(duplicate)
        self.write("lifecycle.json", lifecycle)
        rows = self.report()["baselineClassifications"]
        keys = [row["sourceKey"] for row in rows if row["sourceCategory"] == "staticCaches"]
        self.assertEqual(2, len(keys))
        self.assertEqual(2, len(set(keys)))
        self.assertTrue(keys[0].endswith("<occurrence:1-of-2>"))
        self.assertTrue(keys[1].endswith("<occurrence:2-of-2>"))

    def test_identical_hazards_receive_line_independent_occurrences(self):
        hazards = hazards_fixture()
        duplicate = copy.deepcopy(hazards["categories"]["globalWorldLookups"][0])
        duplicate["line"] = 999
        hazards["categories"]["globalWorldLookups"].append(duplicate)
        self.write("hazards.json", hazards)
        rows = self.report()["hazardClassifications"]
        keys = [row["sourceKey"] for row in rows if row["sourceCategory"] == "globalWorldLookups"]
        self.assertEqual(2, len(keys))
        self.assertEqual(2, len(set(keys)))
        self.assertTrue(keys[0].endswith("<occurrence:1-of-2>"))
        self.assertTrue(keys[1].endswith("<occurrence:2-of-2>"))

    def test_output_is_deterministic_and_line_independent(self):
        first = self.report()
        lifecycle = json.loads(self.lifecycle.read_text())
        hazards = json.loads(self.hazards.read_text())
        ownership = json.loads(self.ownership.read_text())
        for document in (lifecycle, hazards, ownership):
            for rows in document["categories"].values():
                for row in rows:
                    row["line"] = row.get("line", 0) + 9000
        self.write("lifecycle.json", lifecycle)
        self.write("hazards.json", hazards)
        self.write("ownership.json", ownership)
        second = self.report()
        first_keys = [row["sourceKey"] for row in first["baselineClassifications"] + first["hazardClassifications"]]
        second_keys = [row["sourceKey"] for row in second["baselineClassifications"] + second["hazardClassifications"]]
        self.assertEqual(first_keys, second_keys)
        self.assertEqual(delta.json_bytes(second), delta.json_bytes(self.report()))

    def test_missing_and_ambiguous_authority_fail_closed(self):
        hazards = hazards_fixture()
        del hazards["categories"]["mutableStaticCaches"][0]["rationale"]
        self.write("hazards.json", hazards)
        with self.assertRaises(delta.DeltaError):
            self.report()

        self.write("hazards.json", hazards_fixture())
        ownership = ownership_fixture()
        ownership["categories"]["persistentNativeContainers"].append(
            copy.deepcopy(ownership["categories"]["persistentNativeContainers"][0])
        )
        self.write("ownership.json", ownership)
        with self.assertRaises(delta.DeltaError):
            self.report()

    def test_unknown_disposition_does_not_become_open(self):
        hazards = hazards_fixture()
        hazards["categories"]["mutableStaticCaches"][0]["disposition"] = "UNKNOWN"
        self.write("hazards.json", hazards)
        with self.assertRaises(delta.DeltaError):
            self.report()

    def test_open_count_and_new_after_baseline_are_explicit(self):
        report = self.report()
        self.assertEqual(2, report["summary"]["openCount"])
        self.assertEqual(1, report["summary"]["newAfterBaselineCount"])
        self.assertEqual("Assets/New.cs", report["newAfterBaseline"][0]["sourcePath"])

    def test_protected_hazard_uses_named_owner_authority(self):
        hazards = hazards_fixture()
        row = hazards["categories"]["mutableStaticCaches"][0]
        row["protectedOwnerIds"] = ["audio"]
        self.write("hazards.json", hazards)
        protected = self.report()["hazardClassifications"][1]
        self.assertEqual("protected-deferred", protected["classification"])
        self.assertEqual(["audio"], protected["protectedOwnerIds"])

    def test_cli_writes_and_check_detects_stale_output(self):
        json_output = self.root / "out/report.json"
        markdown_output = self.root / "out/report.md"
        command = [
            sys.executable, str(ROOT / "Tools/CI/architecture_phase2_ownership_delta.py"),
            "--lifecycle", str(self.lifecycle), "--hazards", str(self.hazards),
            "--ownership", str(self.ownership), "--json-output", str(json_output),
            "--markdown-output", str(markdown_output),
        ]
        subprocess.run(command, check=True, capture_output=True, text=True)
        subprocess.run([*command, "--check"], check=True, capture_output=True, text=True)
        markdown_output.write_text("stale\n", encoding="utf-8")
        checked = subprocess.run([*command, "--check"], capture_output=True, text=True)
        self.assertEqual(1, checked.returncode)
        self.assertIn("stale or missing", checked.stdout)


if __name__ == "__main__":
    unittest.main()

#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
EVIDENCE_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/source_responsibility_guardrail_evidence.json"
TRACKER_PATH = ROOT / "Design/Architecture/post_hardening_architecture_maturity_tracker.md"
LEGACY_BASELINE_PATH = ROOT / "Design/Architecture/production_source_growth_baseline.md"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git(*args: str, text: bool = True):
    return subprocess.run(["git", *args], cwd=ROOT, check=True, capture_output=True, text=text).stdout


def measure_bytes(raw: bytes) -> tuple[int, int]:
    normalized = raw.replace(b"\r\n", b"\n").replace(b"\r", b"\n")
    lines = normalized.count(b"\n") + (0 if not normalized or normalized.endswith(b"\n") else 1)
    return lines, len(normalized)


class ArchitectureSourceResponsibilityGuardrailEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.data = json.loads(EVIDENCE_PATH.read_text(encoding="utf-8"))
        contract_path = ROOT / cls.data["contract"]["path"]
        cls.contract = json.loads(contract_path.read_text(encoding="utf-8"))

    def test_identity_and_exact_task_scope(self) -> None:
        baseline = self.data["sourceBaseline"]
        self.assertEqual(git("rev-parse", f"{baseline['commit']}^{{tree}}").strip(), baseline["tree"])
        accepted = self.data.get("acceptedEvidence")
        if self.data["acceptanceRequired"]:
            self.assertIsNotNone(accepted, "Accepted AM-016 evidence must bind an immutable commit and tree.")
        end = accepted["commit"] if accepted else None
        if accepted:
            self.assertEqual(git("rev-parse", f"{end}^{{tree}}").strip(), accepted["tree"])
            changed = git("diff", "--name-only", baseline["commit"], end).splitlines()
        else:
            changed = git("diff", "--name-only", baseline["commit"]).splitlines()
            changed += git("ls-files", "--others", "--exclude-standard").splitlines()
        self.assertEqual(set(changed), set(self.data["implementationChangePaths"]))
        production_changes = [path for path in changed if path.startswith("Assets/Game/Scripts/")]
        self.assertEqual(production_changes, [], "AM-016 must not change runtime production behavior.")

    def test_contract_freezes_size_symbols_and_duplicate_owner_signature(self) -> None:
        contract_meta = self.data["contract"]
        contract_path = ROOT / contract_meta["path"]
        self.assertEqual(contract_meta["sha256"], sha256(contract_path))
        self.assertEqual(self.contract["schemaVersion"], 1)
        self.assertEqual(self.contract["contractId"], "post-hardening-source-responsibility-v1")
        self.assertEqual(len(self.contract["entries"]), contract_meta["guardedSourceCount"])

        evidence_sources = {entry["path"]: entry for entry in self.data["guardedSources"]}
        self.assertEqual(set(evidence_sources), {entry["path"] for entry in self.contract["entries"]})
        production = sorted((ROOT / "Assets/Game/Scripts").rglob("*.cs"))
        production = [path for path in production if "Editor" not in path.relative_to(ROOT).parts]
        for entry in self.contract["entries"]:
            path = ROOT / entry["path"]
            content = path.read_text(encoding="utf-8")
            lines, byte_count = measure_bytes(path.read_bytes())
            self.assertLessEqual(lines, entry["maxLines"], entry["path"])
            self.assertLessEqual(byte_count, entry["maxBytes"], entry["path"])
            self.assertEqual(evidence_sources[entry["path"]]["sha256"], sha256(path))
            self.assertEqual(evidence_sources[entry["path"]]["maxLines"], entry["maxLines"])
            self.assertEqual(evidence_sources[entry["path"]]["maxBytes"], entry["maxBytes"])
            for required in entry["requiredSymbols"]:
                self.assertIn(required, content, f"{entry['path']} missing {required}")
            for forbidden in entry["forbiddenSymbols"]:
                self.assertNotIn(forbidden, content, f"{entry['path']} regained {forbidden}")

            threshold = entry["responsibilitySignatureMatchThreshold"]
            if threshold <= 0:
                continue
            for candidate in production:
                if candidate == path:
                    continue
                candidate_text = candidate.read_text(encoding="utf-8")
                matches = sum(symbol in candidate_text for symbol in entry["responsibilitySignatureSymbols"])
                self.assertLess(matches, threshold, str(candidate.relative_to(ROOT)))

    def test_growth_authorizations_are_exact_accepted_am013_blobs(self) -> None:
        authorizations = self.contract["growthAuthorizations"]
        self.assertEqual(len(authorizations), self.data["contract"]["growthAuthorizationCount"])
        tracker = TRACKER_PATH.read_text(encoding="utf-8")
        identities = set()
        for entry in authorizations:
            identity = (entry["path"], entry["scope"])
            self.assertNotIn(identity, identities)
            identities.add(identity)
            self.assertEqual(entry["trackerTaskId"], "AM-013")
            self.assertIn("- [x] `AM-013`", tracker)
            self.assertIn(entry["acceptedCommit"], tracker)
            blob = git("show", f"{entry['acceptedCommit']}:{entry['path']}", text=False)
            self.assertEqual(measure_bytes(blob), (entry["maxLines"], entry["maxBytes"]))
            current_lines, current_bytes = measure_bytes((ROOT / entry["path"]).read_bytes())
            self.assertLessEqual(current_lines, entry["maxLines"])
            self.assertLessEqual(current_bytes, entry["maxBytes"])

        legacy = LEGACY_BASELINE_PATH.read_text(encoding="utf-8")
        self.assertIn('"path": "Assets/Game/Scripts/UI/Shell/UIShellContentView.cs"', legacy)
        shell = next(entry for entry in self.contract["entries"] if entry["path"].endswith("UIShellContentView.cs"))
        self.assertEqual((shell["maxLines"], shell["maxBytes"]), (898, 38807))

    def test_canonical_validator_and_validation_result_are_bound(self) -> None:
        validator = self.data["canonicalValidator"]
        path = ROOT / validator["path"]
        source = path.read_text(encoding="utf-8")
        self.assertEqual(validator["sha256"], sha256(path))
        self.assertEqual(validator["result"], "Passed")
        self.assertEqual(validator["passedTests"], 17)
        self.assertEqual(validator["compilerErrors"], 0)
        self.assertIn("PostHardeningGuardrailContractHasExpectedRatchets", source)
        self.assertIn("PostHardeningGuardedSourcesStayBoundedAndNarrow", source)
        self.assertIn("result=Passed tests=17", source)
        self.assertIn(self.data["contract"]["path"], source)

    def test_validator_authority_and_accepted_hashes_are_immutable(self) -> None:
        authority = self.data["validatorAuthority"]
        self.assertEqual(authority["path"], str(Path(__file__).resolve().relative_to(ROOT)))
        self.assertEqual(authority["sha256"], sha256(ROOT / authority["path"]))
        accepted = self.data.get("acceptedEvidence")
        if not accepted:
            return
        for entry in self.data["guardedSources"]:
            blob = git("show", f"{accepted['commit']}:{entry['path']}", text=False)
            self.assertEqual(hashlib.sha256(blob).hexdigest(), entry["sha256"])
        validator = self.data["canonicalValidator"]
        blob = git("show", f"{accepted['commit']}:{validator['path']}", text=False)
        self.assertEqual(hashlib.sha256(blob).hexdigest(), validator["sha256"])
        for metadata in (self.data["contract"], self.data["validatorAuthority"]):
            blob = git("show", f"{accepted['commit']}:{metadata['path']}", text=False)
            self.assertEqual(hashlib.sha256(blob).hexdigest(), metadata["sha256"])


if __name__ == "__main__":
    unittest.main()

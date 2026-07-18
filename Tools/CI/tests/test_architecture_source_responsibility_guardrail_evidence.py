#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import gzip
import json
import re
import subprocess
import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "Tools/CI"))
from architecture_owner_risk_ranking import state_slot_count, strip_comments_and_strings

EVIDENCE_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/source_responsibility_guardrail_evidence.json"
VALIDATOR_AMENDMENT_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/am025_source_growth_validator_schema_amendment.json"
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


def count_total_occurrences(contents: str, values: list[str]) -> int:
    return sum(contents.count(value) for value in values)


class ArchitectureSourceResponsibilityGuardrailEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.data = json.loads(EVIDENCE_PATH.read_text(encoding="utf-8"))
        contract_path = ROOT / cls.data["contract"]["path"]
        cls.contract = json.loads(contract_path.read_text(encoding="utf-8"))

    def test_identity_and_exact_task_scope(self) -> None:
        baseline = self.data["sourceBaseline"]
        program_baseline = self.data["programBaseline"]
        self.assertEqual(baseline["branch"], "main")
        self.assertEqual(git("branch", "--show-current").strip(), "main")
        self.assertRegex(baseline["commit"], r"^[0-9a-f]{40}$")
        self.assertEqual(git("rev-parse", f"{baseline['commit']}^{{tree}}").strip(), baseline["tree"])
        self.assertEqual(
            git("rev-parse", f"{program_baseline['commit']}^{{tree}}").strip(),
            program_baseline["tree"],
        )
        allowed_paths = set(self.data["implementationChangePaths"])
        for entry in self.data["priorImplementationCommits"]:
            self.assertRegex(entry["commit"], r"^[0-9a-f]{40}$")
            self.assertEqual(git("rev-parse", f"{entry['commit']}^{{tree}}").strip(), entry["tree"])
            git("merge-base", "--is-ancestor", program_baseline["commit"], entry["commit"])
            git("merge-base", "--is-ancestor", entry["commit"], baseline["commit"])
            parent = git("rev-parse", f"{entry['commit']}^").strip()
            changed_paths = set(git("diff", "--name-only", parent, entry["commit"]).splitlines())
            self.assertTrue(changed_paths)
            self.assertTrue(changed_paths.issubset(allowed_paths), entry["commit"])
        accepted = self.data.get("acceptedEvidence")
        if self.data["acceptanceRequired"]:
            self.assertIsNotNone(accepted, "Accepted AM-016 evidence must bind an immutable commit and tree.")
        end = accepted["commit"] if accepted else None
        if accepted:
            self.assertRegex(accepted["commit"], r"^[0-9a-f]{40}$")
            self.assertEqual(git("rev-parse", f"{end}^{{tree}}").strip(), accepted["tree"])
            git("merge-base", "--is-ancestor", baseline["commit"], end)
            git("merge-base", "--is-ancestor", end, "HEAD")
            tracker = TRACKER_PATH.read_text(encoding="utf-8")
            self.assertIn("- [x] `AM-016`", tracker)
            self.assertIn(end, tracker)
            changed = git("diff", "--name-only", baseline["commit"], end).splitlines()
        else:
            changed = git("diff", "--name-only", baseline["commit"]).splitlines()
            changed += git("ls-files", "--others", "--exclude-standard").splitlines()
        self.assertEqual(set(changed), allowed_paths)
        amendment = self.data.get("contractAmendmentEvidence")
        if amendment:
            self.assertIsNotNone(accepted, "A guardrail amendment requires the original accepted evidence.")
            self.assertRegex(amendment["commit"], r"^[0-9a-f]{40}$")
            self.assertEqual(
                git("rev-parse", f"{amendment['commit']}^{{tree}}").strip(),
                amendment["tree"],
            )
            git("merge-base", "--is-ancestor", accepted["commit"], amendment["commit"])
            git("merge-base", "--is-ancestor", amendment["commit"], "HEAD")
            parent = git("rev-parse", f"{amendment['commit']}^").strip()
            amendment_paths = set(git("diff", "--name-only", parent, amendment["commit"]).splitlines())
            self.assertEqual(amendment_paths, set(amendment["changePaths"]))
        editor_only_paths = set(self.data["editorOnlyIntegrationPaths"])
        self.assertTrue(editor_only_paths.issubset(allowed_paths))
        for path in editor_only_paths:
            self.assertTrue(path.startswith("Assets/Game/Scripts/Editor/"), path)
        production_changes = [
            path
            for path in changed
            if path.startswith("Assets/Game/Scripts/") and path not in editor_only_paths
        ]
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
        boundary = self.contract["replacementOwnerBoundary"]
        allowed_owners = set(boundary["allowedOwnerPaths"])
        generic_lifecycle_anchors = boundary["genericLifecycleAnchorSymbols"]
        self.assertTrue(generic_lifecycle_anchors)
        for entry in self.contract["entries"]:
            path = ROOT / entry["path"]
            content = path.read_text(encoding="utf-8")
            clean_content = strip_comments_and_strings(content)
            lines, byte_count = measure_bytes(path.read_bytes())
            self.assertEqual(entry["sourceSha256"], sha256(path))
            self.assertLessEqual(lines, entry["maxLines"], entry["path"])
            self.assertLessEqual(byte_count, entry["maxBytes"], entry["path"])
            self.assertEqual(evidence_sources[entry["path"]]["sha256"], sha256(path))
            self.assertEqual(evidence_sources[entry["path"]]["maxLines"], entry["maxLines"])
            self.assertEqual(evidence_sources[entry["path"]]["maxBytes"], entry["maxBytes"])
            self.assertLessEqual(content.count(boundary["domainSymbol"]), entry["maxResponsibilityDomainSymbolOccurrences"])
            self.assertLessEqual(state_slot_count(entry["path"], clean_content), entry["maxStateSlots"])
            for required in entry["requiredSymbols"]:
                self.assertIn(required, clean_content, f"{entry['path']} missing active {required}")
            for forbidden in entry["forbiddenSymbols"]:
                self.assertNotIn(forbidden, clean_content, f"{entry['path']} regained active {forbidden}")

            threshold = entry["responsibilitySignatureMatchThreshold"]
            if threshold <= 0:
                continue
            for candidate in production:
                if candidate == path:
                    continue
                candidate_text = candidate.read_text(encoding="utf-8")
                matches = sum(symbol in candidate_text for symbol in entry["responsibilitySignatureSymbols"])
                self.assertLess(matches, threshold, str(candidate.relative_to(ROOT)))

        baseline_commit = boundary["baselineCommit"]
        self.assertRegex(baseline_commit, r"^[0-9a-f]{40}$")
        baseline_paths = set(git("ls-tree", "-r", "--name-only", baseline_commit, "--", boundary["root"]).splitlines())
        changed_production = set(git("diff", "--name-only", baseline_commit, "--", boundary["root"]).splitlines())
        for candidate in production:
            relative = str(candidate.relative_to(ROOT))
            if not relative.startswith(boundary["root"] + "/"):
                continue
            existed_at_baseline = relative in baseline_paths
            if existed_at_baseline and relative not in changed_production:
                continue
            candidate_text = candidate.read_text(encoding="utf-8")
            current_matches = sum(symbol in candidate_text for symbol in boundary["managedLifecycleSymbols"])
            current_occurrences = count_total_occurrences(candidate_text, boundary["managedLifecycleSymbols"])
            current_domain_occurrences = candidate_text.count(boundary["domainSymbol"])
            current_domain_owner = (
                boundary["domainSymbol"] in candidate_text
                and current_matches >= boundary["managedLifecycleMatchThreshold"]
            )
            current_generic_owner = (
                current_matches >= boundary["genericLifecycleMatchThreshold"]
                and any(symbol in candidate_text for symbol in generic_lifecycle_anchors)
            )
            if (not current_domain_owner and not current_generic_owner) or relative in allowed_owners:
                continue

            baseline_text = ""
            baseline_lines = 0
            baseline_bytes = 0
            if existed_at_baseline:
                baseline_text = git("show", f"{baseline_commit}:{relative}")
                baseline_lines, baseline_bytes = measure_bytes(baseline_text.encode("utf-8"))
            baseline_matches = sum(symbol in baseline_text for symbol in boundary["managedLifecycleSymbols"])
            baseline_occurrences = count_total_occurrences(baseline_text, boundary["managedLifecycleSymbols"])
            baseline_domain_occurrences = baseline_text.count(boundary["domainSymbol"])
            baseline_domain_owner = (
                boundary["domainSymbol"] in baseline_text
                and baseline_matches >= boundary["managedLifecycleMatchThreshold"]
            )
            baseline_generic_owner = (
                baseline_matches >= boundary["genericLifecycleMatchThreshold"]
                and any(symbol in baseline_text for symbol in generic_lifecycle_anchors)
            )
            current_lines, current_bytes = measure_bytes(candidate.read_bytes())
            if current_domain_owner:
                self.assertTrue(baseline_domain_owner, relative)
                self.assertLessEqual(current_domain_occurrences, baseline_domain_occurrences, relative)
            if current_generic_owner:
                self.assertTrue(baseline_generic_owner, relative)
                self.assertLessEqual(current_matches, baseline_matches, relative)
                self.assertLessEqual(current_occurrences, baseline_occurrences, relative)
                self.assertLessEqual(current_lines, baseline_lines, relative)
                self.assertLessEqual(current_bytes, baseline_bytes, relative)

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
        self.assertIn("PostHardeningGuardrailContractHasExpectedRatchets", source)
        self.assertIn("PostHardeningGuardedSourcesStayBoundedAndNarrow", source)
        self.assertIn("result=Passed tests=17", source)
        self.assertIn(self.data["contract"]["path"], source)
        current_sha256 = sha256(path)
        if current_sha256 != validator["sha256"]:
            amendment = json.loads(VALIDATOR_AMENDMENT_PATH.read_text(encoding="utf-8"))
            self.assertEqual(amendment["artifactId"], "AM025-SOURCE-GROWTH-VALIDATOR-SCHEMA-AMENDMENT")
            self.assertEqual(amendment["result"], "AcceptedValidatorCorrectionBlockedExternalSourceGrowth")
            self.assertEqual(amendment["validator"]["previousSha256"], validator["sha256"])
            self.assertEqual(amendment["validator"]["currentSha256"], current_sha256)
            self.assertFalse(amendment["validator"]["sourceCeilingsChanged"])
            self.assertEqual(amendment["validator"]["approvedExceptionsAdded"], 0)
            self.assertEqual(amendment["validator"]["compilerErrors"], 0)
            commit = amendment["implementation"]["commit"]
            self.assertEqual(git("rev-parse", f"{commit}^{{tree}}").strip(), amendment["implementation"]["tree"])
            self.assertEqual(git("show", f"{commit}:{validator['path']}", text=False), path.read_bytes())
            log_meta = amendment["canonicalResult"]["log"]
            log_path = ROOT / log_meta["path"]
            self.assertEqual(log_meta["sha256"], sha256(log_path))
            with gzip.open(log_path, "rt", encoding="utf-8", errors="replace") as handle:
                log_text = handle.read()
            self.assertIn(amendment["canonicalResult"]["resultMarker"], log_text)
            for blocked_path in amendment["canonicalResult"]["blockedPaths"]:
                self.assertIn(blocked_path, log_text)
            self.assertIsNone(re.search(r"\berror CS\d+", log_text))
            return

        self.assertEqual(validator["result"], "Passed")
        self.assertEqual(validator["passedTests"], 17)
        self.assertEqual(validator["compilerErrors"], 0)
        log_path = ROOT / validator["log"]
        log_text = log_path.read_text(encoding="utf-8")
        self.assertEqual(validator["logSha256"], sha256(log_path))
        self.assertEqual(validator["logBytes"], log_path.stat().st_size)
        self.assertEqual(log_text.count("[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17"), 1)
        self.assertNotIn("[ProductionSourceGrowthArchitectureValidation] result=Failed", log_text)
        self.assertIsNone(re.search(r"\berror CS\d+", log_text))

    def test_validator_authority_and_accepted_hashes_are_immutable(self) -> None:
        authority = self.data["validatorAuthority"]
        self.assertEqual(authority["path"], str(Path(__file__).resolve().relative_to(ROOT)))
        current_authority_sha256 = sha256(ROOT / authority["path"])
        if current_authority_sha256 != authority["sha256"]:
            amendment = json.loads(VALIDATOR_AMENDMENT_PATH.read_text(encoding="utf-8"))
            amended_authority = amendment["validatorAuthority"]
            self.assertEqual(amended_authority["path"], authority["path"])
            self.assertEqual(amended_authority["previousSha256"], authority["sha256"])
            self.assertEqual(amended_authority["currentSha256"], current_authority_sha256)
        accepted = self.data.get("acceptedEvidence")
        if not accepted:
            return
        for entry in self.data["guardedSources"]:
            blob = git("show", f"{accepted['commit']}:{entry['path']}", text=False)
            self.assertEqual(hashlib.sha256(blob).hexdigest(), entry["sha256"])
        validator = self.data["canonicalValidator"]
        blob = git("show", f"{accepted['commit']}:{validator['path']}", text=False)
        self.assertEqual(hashlib.sha256(blob).hexdigest(), validator["sha256"])
        authority_commit = self.data.get("contractAmendmentEvidence", accepted)["commit"]
        for metadata in (self.data["contract"], self.data["validatorAuthority"]):
            blob = git("show", f"{authority_commit}:{metadata['path']}", text=False)
            self.assertEqual(hashlib.sha256(blob).hexdigest(), metadata["sha256"])
        log = self.data["canonicalValidator"]
        blob = git("show", f"{accepted['commit']}:{log['log']}", text=False)
        self.assertEqual(hashlib.sha256(blob).hexdigest(), log["logSha256"])


if __name__ == "__main__":
    unittest.main()

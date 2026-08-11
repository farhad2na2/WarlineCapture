import hashlib
import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
REPORT_ROOT = ROOT / "Design/AgentReports/ArchitectureMaturity"


def load(name: str):
    return json.loads((REPORT_ROOT / name).read_text(encoding="utf-8"))


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


class Phase2AcceptanceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.policy = load("am025_phase2_exit_policy.json")
        cls.evidence = load("am025_phase2_exit_evidence.json")
        cls.acceptance = load("am025_acceptance_record.json")

    def test_policy_matches_current_zero_debt_authority(self):
        ownership = self.policy["ownershipDelta"]
        self.assertEqual("AcceptedPhase2Exit", self.policy["status"])
        self.assertEqual(640, ownership["am021Rows"])
        self.assertEqual(567, ownership["am021ExplicitOwners"])
        self.assertEqual(73, ownership["am021ProtectedOwners"])
        self.assertEqual(0, ownership["am021GapCount"])
        self.assertEqual(425, ownership["projectedReviewedRowCount"])
        self.assertEqual(0, ownership["projectedGenuineDebtRowCount"])
        self.assertEqual(0, ownership["projectedUnclassifiedRowCount"])
        python_suite = self.policy["canonicalCoreSuites"][0]
        self.assertEqual(179, python_suite["baselinePassed"] + python_suite["am025Added"])
        self.assertEqual(179, python_suite["requiredPassed"])

    def test_all_prerequisite_and_manifest_hashes_resolve(self):
        for row in self.evidence["prerequisites"]:
            path = ROOT / row["path"]
            self.assertTrue(path.is_file(), row["path"])
            self.assertEqual(row["sha256"], sha256(path), row["path"])
        for manifest in ("artifactManifest", "toolManifest", "testManifest"):
            for row in self.evidence[manifest]:
                path = ROOT / row["path"]
                self.assertTrue(path.is_file(), row["path"])
                self.assertEqual(row["sha256"], sha256(path), row["path"])

    def test_capture_identities_are_exact_reachable_ancestors(self):
        identities = (
            self.evidence["baseline"],
            self.evidence["validatedCapture"],
            self.evidence["finalReviewRecord"],
            self.evidence["publicationClaim"],
        )
        for identity in identities:
            commit = identity["commit"]
            tree = subprocess.run(
                ["git", "rev-parse", f"{commit}^{{tree}}"],
                cwd=ROOT,
                check=True,
                capture_output=True,
                text=True,
            ).stdout.strip()
            self.assertEqual(identity["tree"], tree, commit)
            ancestor = subprocess.run(
                ["git", "merge-base", "--is-ancestor", commit, "HEAD"],
                cwd=ROOT,
            )
            self.assertEqual(0, ancestor.returncode, commit)

    def test_exit_counts_and_core_suites_pass_without_release_activation(self):
        ownership = self.evidence["ownership"]
        self.assertEqual(
            (640, 567, 73, 0),
            (
                ownership["totalResources"],
                ownership["explicitOwners"],
                ownership["protectedOwners"],
                ownership["ownershipGaps"],
            ),
        )
        self.assertEqual(
            (425, 420, 5, 0, 0),
            (
                ownership["currentReviewRows"],
                ownership["resolvedRows"],
                ownership["protectedDeferredRows"],
                ownership["genuineDebtRows"],
                ownership["unclassifiedRows"],
            ),
        )
        validation = self.evidence["canonicalValidation"]
        self.assertEqual(17, validation["sourceGrowth"]["tests"])
        self.assertEqual(23, validation["architectureCloseout"]["suites"])
        self.assertEqual(179, validation["architecturePython"]["tests"])
        self.assertEqual(0, validation["worldRecovery"]["recurringAllocatedBytes"])
        self.assertEqual(10, validation["transitionStress"]["measuredCycles"])
        self.assertEqual(5, validation["structuralPoolTrend"]["measuredCycles"])
        self.assertEqual(0, validation["governedAllocation"]["allocatedBytesPerPhase"])
        self.assertEqual(0, validation["unityCompile"]["compilerErrors"])
        self.assertFalse(self.evidence["releaseLane"]["activated"])

    def test_determinism_protected_audit_and_review_are_closed(self):
        self.assertTrue(all(value == "Passed" for key, value in self.evidence["determinism"].items() if key != "logSha256"))
        audit = self.evidence["protectedPathAudit"]
        self.assertEqual(94, audit["changedPathCount"])
        self.assertEqual(0, audit["protectedPathCount"])
        self.assertEqual(0, audit["outsideOwnedDomainCount"])
        self.assertEqual("Passed", audit["result"])
        review = self.evidence["focusedReview"]
        self.assertEqual("Passed", review["result"])
        self.assertEqual(0, review["unresolvedFindingCount"])

    def test_acceptance_record_binds_evidence_policy_progress_and_next_task(self):
        evidence_ref = self.acceptance["acceptedEvidence"]
        policy_ref = self.acceptance["policy"]
        self.assertEqual(evidence_ref["sha256"], sha256(ROOT / evidence_ref["path"]))
        self.assertEqual(policy_ref["sha256"], sha256(ROOT / policy_ref["path"]))
        self.assertEqual("Accepted", self.acceptance["result"])
        self.assertEqual(0, self.acceptance["acceptance"]["ownershipGaps"])
        self.assertEqual(0, self.acceptance["acceptance"]["genuineDebtRows"])
        self.assertEqual(0, self.acceptance["acceptance"]["unclassifiedRows"])
        self.assertEqual(0, self.acceptance["acceptance"]["compilerErrors"])
        self.assertEqual("26 / 86 (30.2%)", self.acceptance["progress"]["overall"])
        self.assertEqual("26 / 68 (38.2%)", self.acceptance["progress"]["core"])
        self.assertEqual("AM-027", self.acceptance["nextTask"]["taskId"])
        self.assertFalse(self.acceptance["nextTask"]["started"])


if __name__ == "__main__":
    unittest.main()

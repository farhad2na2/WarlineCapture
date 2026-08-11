#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
EVIDENCE_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/ui_shell_resource_exchange_decomposition_evidence.json"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=ROOT, check=True, capture_output=True, text=True).stdout


def git_blob_sha256(commit: str, path: str) -> str:
    return hashlib.sha256(
        subprocess.run(
            ["git", "show", f"{commit}:{path}"],
            cwd=ROOT,
            check=True,
            capture_output=True,
        ).stdout
    ).hexdigest()


def git_blob_with_sha256(path: str, expected: str) -> bytes:
    commits = git("rev-list", "--all", "--", path).splitlines()
    for commit in commits:
        blob = subprocess.run(
            ["git", "show", f"{commit}:{path}"],
            cwd=ROOT,
            check=True,
            capture_output=True,
        ).stdout
        if hashlib.sha256(blob).hexdigest() == expected:
            return blob
    raise AssertionError(f"No reachable Git blob for {path} has SHA-256 {expected}")


class ArchitectureUiShellDecompositionEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.data = json.loads(EVIDENCE_PATH.read_text(encoding="utf-8"))

    def test_identity_hashes_and_exact_production_scope(self) -> None:
        baseline = self.data["sourceBaseline"]
        self.assertEqual(git("rev-parse", f"{baseline['commit']}^{{tree}}").strip(), baseline["tree"])
        accepted = self.data.get("acceptedEvidence")
        evidence_commit = accepted["commit"] if accepted else baseline["commit"]
        for entry in self.data["ownedFiles"]:
            if entry["path"].startswith("Assets/Game/Scripts/"):
                self.assertEqual(entry["sha256"], git_blob_sha256(evidence_commit, entry["path"]), entry["path"])
            else:
                git_blob_with_sha256(entry["path"], entry["sha256"])
        authority = self.data["validatorAuthority"]
        self.assertEqual(authority["path"], Path(__file__).resolve().relative_to(ROOT).as_posix())
        git_blob_with_sha256(authority["path"], authority["sha256"])

        if self.data["acceptanceRequired"]:
            self.assertIsNotNone(accepted, "Accepted AM-015 evidence must bind an immutable commit and tree.")
        if accepted:
            end = accepted["commit"]
            self.assertEqual(git("rev-parse", f"{end}^{{tree}}").strip(), accepted["tree"])
            changed = git("diff", "--name-only", baseline["commit"], end, "--", "Assets/Game/Scripts").splitlines()
        else:
            changed = git("diff", "--name-only", baseline["commit"], "--", "Assets/Game/Scripts").splitlines()
            changed += git("ls-files", "--others", "--exclude-standard", "--", "Assets/Game/Scripts").splitlines()
        actual = {path for path in changed if path.endswith(".cs")}
        self.assertEqual(actual, set(self.data["productionChangePaths"]))

    def test_selection_is_current_highest_ranked_recurring_ui_owner(self) -> None:
        selection = self.data["selection"]
        ranking_path = ROOT / selection["rankingPath"]
        self.assertEqual(selection["rankingSha256"], sha256(ranking_path))
        ranking = json.loads(ranking_path.read_text(encoding="utf-8"))
        owner = next(row for row in ranking["screenedOwners"] if row["path"] == selection["ownerPath"])
        eligible_recurring_ui = [
            row for row in ranking["screenedOwners"]
            if "/UI/" in row["path"]
            and row["updateExposure"]["recurring"]
            and row["editEligibility"] == "eligible"
            and row["protectedOwnerId"] is None
        ]
        self.assertEqual(
            min(eligible_recurring_ui, key=lambda row: row["screeningRank"])["path"],
            selection["ownerPath"],
        )
        self.assertEqual(owner["screeningRank"], selection["screeningRank"])
        self.assertEqual(owner["screeningScore"], selection["screeningScore"])
        self.assertEqual(owner["stateSlotCount"], self.data["measuredDecomposition"]["ownerStateSlotsBefore"])
        self.assertTrue(owner["updateExposure"]["recurring"])
        self.assertEqual(owner["editEligibility"], "eligible")
        self.assertIsNone(owner["protectedOwnerId"])

    def test_measured_owner_reduction_is_exact(self) -> None:
        metrics = self.data["measuredDecomposition"]
        baseline_source = git("show", f"{self.data['sourceBaseline']['commit']}:Assets/Game/Scripts/UI/Shell/UIShellContentView.cs")
        accepted_commit = self.data["acceptedEvidence"]["commit"]
        owner = git("show", f"{accepted_commit}:Assets/Game/Scripts/UI/Shell/UIShellContentView.cs")
        binding = git("show", f"{accepted_commit}:Assets/Game/Scripts/UI/Shell/ResourceExchangeShellBinding.cs")
        self.assertEqual(len(baseline_source.splitlines()), metrics["ownerLinesBefore"])
        self.assertEqual(len(baseline_source.encode()), metrics["ownerBytesBefore"])
        self.assertEqual(len(owner.splitlines()), metrics["ownerLinesAfter"])
        self.assertEqual(len(owner.encode()), metrics["ownerBytesAfter"])
        self.assertEqual(len(binding.splitlines()), metrics["bindingLinesAfter"])
        self.assertEqual(len(binding.encode()), metrics["bindingBytesAfter"])
        self.assertLess(metrics["ownerLinesAfter"], metrics["ownerLinesBefore"])
        self.assertLessEqual(metrics["bindingLinesAfter"], 120)
        self.assertEqual(metrics["movedStateSlots"], 4)

    def test_authority_and_naming_boundary_remains_narrow(self) -> None:
        owner = (ROOT / "Assets/Game/Scripts/UI/Shell/UIShellContentView.cs").read_text(encoding="utf-8")
        binding = (ROOT / "Assets/Game/Scripts/UI/Shell/ResourceExchangeShellBinding.cs").read_text(encoding="utf-8")
        for removed in ("_resourceExchangePopupInstance", "_resourceExchangePopupView", "_resourceExchangePopupCloseButton", "_resourceExchangePopupCloseButtonListener"):
            self.assertNotIn(removed, owner)
        for required in ("_instance", "_view", "_closeButton", "_closeListener"):
            self.assertIn(required, binding)
        self.assertIn("UiShellRuntimeGateway.TryEnqueueUiAction", owner)
        self.assertIn("_resourceExchangeShellBinding.Install", owner)
        self.assertIn("_resourceExchangeShellBinding.Close", owner)
        self.assertIn("_resourceExchangeShellBinding.ResetForRegionClear", owner)
        self.assertIn("_resourceExchangeShellBinding.RebindMainMenuPlayUi", owner)
        for forbidden in ("SystemBase", "ISystem", "MonoBehaviour", "static World", "ServiceLocator", "Controller", "Manager", "Provider"):
            self.assertNotIn(forbidden, binding)

    def test_characterization_and_validation_matrix_is_green(self) -> None:
        tests = (ROOT / "Assets/Tests/Editor/ResourceExchangeHeaderRoutingTests.cs").read_text(encoding="utf-8")
        self.assertIn("MenuSceneShell_DirectResourceExchangeCloseIsIdempotent", tests)
        self.assertIn("MenuSceneShell_PopupLayerClearRemovesResourceExchangeCloseListener", tests)
        self.assertIn("MenuSceneShell_RebindingRuntimeUiTransfersOpenResourceExchangePopup", tests)
        expected = {"resource-exchange": 10, "ui-shell": 14, "settings": 8, "architecture": 1}
        validations = self.data["unityValidations"]
        self.assertEqual({entry["id"] for entry in validations}, set(expected))
        for entry in validations:
            self.assertEqual(entry["result"], "Passed")
            self.assertEqual(entry["compilerErrors"], 0)
            self.assertEqual(entry["passedTests"], expected[entry["id"]])
        sources = {entry["id"]: entry for entry in self.data["validationSources"]}
        self.assertEqual(set(sources), set(expected))
        for validation_id, entry in sources.items():
            text = git_blob_with_sha256(entry["path"], entry["sha256"]).decode("utf-8")
            if validation_id == "architecture":
                self.assertIn("RunBroadShellValidation", text)
            else:
                self.assertEqual(text.count("RunValidationStep(") - 1, entry["runnerSteps"])
            self.assertEqual(entry["runnerSteps"], expected[validation_id])


if __name__ == "__main__":
    unittest.main()

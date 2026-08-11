#!/usr/bin/env python3

from __future__ import annotations

import unittest

from Tools.CI import dense_city_generated_artifact_ownership_audit as audit


def complete_tracked_paths() -> list[str]:
    return [
        *audit.REQUIRED_TRACKED_PATHS,
        "Assets/Game/GeneratedOperationMaps/DenseCity/map/Candidate/hash/SurfaceProxies/a.asset",
        "Design/AgentReports/2026-07-25_dense_city_example.json",
    ]


class DenseCityGeneratedArtifactOwnershipAuditTests(unittest.TestCase):
    def test_clean_repository_contract_passes(self) -> None:
        report = audit.audit_paths(complete_tracked_paths(), lambda _: True)

        self.assertEqual(report["result"], "PASS")
        self.assertEqual(report["summary"]["missingRequiredPathCount"], 0)
        self.assertFalse(report["finalArtifactSetComplete"])
        self.assertFalse(report["androidAcceptanceComplete"])

    def test_missing_required_candidate_output_fails_closed(self) -> None:
        tracked = complete_tracked_paths()
        missing = audit.REQUIRED_TRACKED_PATHS[0]
        tracked.remove(missing)

        report = audit.audit_paths(tracked, lambda _: True)

        self.assertEqual(report["result"], "FAIL")
        self.assertEqual(report["violations"]["missingRequiredPaths"], [missing])

    def test_final_closeout_requires_complete_tracked_evidence(self) -> None:
        report = audit.audit_paths(
            [*complete_tracked_paths(), *audit.FINAL_REQUIRED_TRACKED_PATHS],
            lambda _: True,
            final_closeout=True,
        )

        self.assertEqual(report["result"], "PASS")
        self.assertTrue(report["finalArtifactSetComplete"])
        self.assertTrue(report["androidAcceptanceComplete"])
        self.assertEqual(report["summary"]["missingFinalRequiredPathCount"], 0)

    def test_final_closeout_missing_evidence_fails_closed(self) -> None:
        required = list(audit.FINAL_REQUIRED_TRACKED_PATHS)
        missing = required.pop()

        report = audit.audit_paths(
            [*complete_tracked_paths(), *required],
            lambda _: True,
            final_closeout=True,
        )

        self.assertEqual(report["result"], "FAIL")
        self.assertFalse(report["finalArtifactSetComplete"])
        self.assertFalse(report["androidAcceptanceComplete"])
        self.assertEqual(report["violations"]["missingFinalRequiredPaths"], [missing])

    def test_tracked_transient_output_fails_closed(self) -> None:
        for transient in (
            "Library/OperationMapDenseCityRuntimeContent/Addressables/catalog.json",
            ".utmp/dense-city-probe",
        ):
            with self.subTest(transient=transient):
                report = audit.audit_paths(
                    [*complete_tracked_paths(), transient],
                    lambda _: True,
                )

                self.assertEqual(report["result"], "FAIL")
                self.assertEqual(
                    report["violations"]["trackedTransientPaths"],
                    [transient],
                )

    def test_tracked_transaction_leftover_fails_closed(self) -> None:
        tracked = complete_tracked_paths()
        backup = (
            "Assets/Game/GeneratedOperationMaps/DenseCity/map/Candidate/hash/"
            "SurfaceProxies__TransactionBackup/a.asset"
        )
        tracked.append(backup)

        report = audit.audit_paths(tracked, lambda _: True)

        self.assertEqual(report["result"], "FAIL")
        self.assertEqual(
            report["violations"]["trackedForbiddenTransactionArtifacts"],
            [backup],
        )

    def test_unignored_transient_probe_fails_closed(self) -> None:
        rejected_probe = audit.TRANSIENT_IGNORE_PROBES[2]

        report = audit.audit_paths(
            complete_tracked_paths(),
            lambda path: path != rejected_probe,
        )

        self.assertEqual(report["result"], "FAIL")
        self.assertEqual(
            report["violations"]["unignoredTransientProbePaths"],
            [rejected_probe],
        )


if __name__ == "__main__":
    unittest.main()

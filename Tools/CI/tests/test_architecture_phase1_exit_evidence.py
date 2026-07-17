#!/usr/bin/env python3

from __future__ import annotations

import gzip
import hashlib
import json
import re
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
EVIDENCE_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/am017_phase1_exit_evidence.json"
POLICY_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/phase1_exit_capture_policy.json"
ACCEPTANCE_PATH = ROOT / "Design/AgentReports/ArchitectureMaturity/am017_acceptance_record.json"


def load_json(path: str | Path) -> dict:
    resolved = path if isinstance(path, Path) else ROOT / path
    return json.loads(resolved.read_text(encoding="utf-8"))


def sha256_bytes(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def sha256(path: str) -> str:
    return sha256_bytes((ROOT / path).read_bytes())


def git_text(*arguments: str) -> str:
    result = subprocess.run(
        ["git", *arguments],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    return result.stdout.strip()


def git_bytes(commit: str, path: str) -> bytes:
    result = subprocess.run(
        ["git", "show", f"{commit}:{path}"],
        cwd=ROOT,
        check=True,
        capture_output=True,
    )
    return result.stdout


class ArchitecturePhase1ExitEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.evidence = load_json(EVIDENCE_PATH)
        cls.policy = load_json(POLICY_PATH)
        cls.acceptance = load_json(ACCEPTANCE_PATH)

    def assert_git_identity(self, commit: str, expected_tree: str) -> None:
        self.assertEqual(git_text("rev-parse", "--verify", f"{commit}^{{commit}}"), commit)
        self.assertEqual(git_text("rev-parse", f"{commit}^{{tree}}"), expected_tree)

    def assert_is_ancestor(self, ancestor: str, descendant: str) -> None:
        result = subprocess.run(
            ["git", "merge-base", "--is-ancestor", ancestor, descendant],
            cwd=ROOT,
            capture_output=True,
        )
        self.assertEqual(result.returncode, 0, f"{ancestor} is not an ancestor of {descendant}")

    def assert_log(self, entry: dict) -> str:
        path = entry["logPath"]
        compressed = (ROOT / path).read_bytes()
        self.assertEqual(entry["gzipSha256"], sha256_bytes(compressed), path)
        raw = gzip.decompress(compressed)
        self.assertEqual(entry["rawLogSha256"], sha256_bytes(raw), path)
        text = raw.decode("utf-8", errors="replace")
        self.assertIn(entry["resultMarker"], text, path)
        self.assertNotIn("result=Failed", text, path)
        self.assertNotRegex(text, r"\berror CS\d+", path)
        return text

    def test_git_capture_identity_environment_and_governed_sources_are_exact(self) -> None:
        evidence = self.evidence
        contexts = evidence["captureContexts"]
        source = evidence["sourceBaseline"]
        accepted = self.acceptance["acceptedEvidence"]
        integrated_source = self.acceptance["sourceBaseline"]
        integration = {entry["context"]: entry for entry in self.acceptance["captureIntegration"]}
        focused = contexts["focused"]
        canonical = contexts["canonical"]

        self.assert_git_identity(source["commit"], source["tree"])
        self.assert_git_identity(focused["exactCommit"], focused["tree"])
        self.assert_git_identity(canonical["exactCommit"], canonical["tree"])
        self.assertEqual(integrated_source["capturedCommit"], source["commit"])
        self.assertEqual(integrated_source["capturedTree"], source["tree"])
        self.assert_is_ancestor(source["commit"], focused["exactCommit"])
        self.assert_is_ancestor(focused["exactCommit"], canonical["exactCommit"])
        self.assert_is_ancestor(canonical["exactCommit"], accepted["capturedCommit"])
        self.assert_git_identity(integrated_source["integratedCommit"], integrated_source["integratedTree"])
        for context_name, context in (("focused", focused), ("canonical", canonical)):
            mapped = integration[context_name]
            self.assertEqual(mapped["capturedCommit"], context["exactCommit"])
            self.assertEqual(mapped["capturedTree"], context["tree"])
            self.assert_git_identity(mapped["integratedCommit"], mapped["integratedTree"])
        self.assert_is_ancestor(integrated_source["integratedCommit"], integration["focused"]["integratedCommit"])
        self.assert_is_ancestor(integration["focused"]["integratedCommit"], integration["canonical"]["integratedCommit"])
        self.assert_is_ancestor(integration["canonical"]["integratedCommit"], accepted["integratedCommit"])
        self.assert_is_ancestor(accepted["integratedCommit"], "HEAD")
        self.assertEqual(evidence["focusedCaptureCommit"], focused["exactCommit"])
        self.assertEqual(evidence["focusedCaptureTree"], focused["tree"])
        self.assertEqual(evidence["canonicalCaptureCommit"], canonical["exactCommit"])
        self.assertEqual(evidence["canonicalCaptureTree"], canonical["tree"])

        for context in (focused, canonical):
            self.assertFalse(context["dirty"])
            self.assertEqual(context["qualityName"], "Mobile")
            self.assertEqual((context["resolutionWidth"], context["resolutionHeight"]), (640, 480))
        self.assertIn("Profiler disabled", focused["instrumentationState"])
        self.assertIn("profilerEnabled=false", canonical["frameInstrumentationState"])
        self.assertIn("deep profiling disabled", canonical["gcInstrumentationState"])
        self.assertEqual(canonical["launchArguments"], ["-warlineAutoStartMatch"])

        environment = evidence["environmentIdentity"]
        self.assertEqual(environment["sha256"], evidence["environmentIdentitySha256"])
        self.assertEqual(environment["sha256"], sha256(environment["path"]))
        for context in (focused, canonical):
            self.assertEqual(
                environment["sha256"],
                sha256_bytes(git_bytes(context["exactCommit"], environment["path"])),
            )
            integrated_commit = integration["focused" if context is focused else "canonical"]["integratedCommit"]
            self.assertEqual(
                environment["sha256"],
                sha256_bytes(git_bytes(integrated_commit, environment["path"])),
            )

        for entry in evidence["governedSources"]:
            self.assertEqual(entry["sha256"], sha256(entry["path"]), entry["path"])
            capture_commit = contexts[entry["captureContext"]]["exactCommit"]
            self.assertEqual(
                entry["sha256"],
                sha256_bytes(git_bytes(capture_commit, entry["path"])),
                entry["path"],
            )
            integrated_commit = integration[entry["captureContext"]]["integratedCommit"]
            self.assertEqual(
                entry["sha256"],
                sha256_bytes(git_bytes(integrated_commit, entry["path"])),
                entry["path"],
            )

    def test_policy_and_all_tracked_report_hashes_are_current(self) -> None:
        evidence = self.evidence
        self.assertEqual(evidence["taskId"], "AM-017")
        self.assertEqual(evidence["result"], "Passed")
        self.assertEqual(evidence["policy"]["sha256"], sha256(evidence["policy"]["path"]))
        for authority in self.policy["authorityFiles"]:
            self.assertEqual(authority["sha256"], sha256(authority["path"]), authority["path"])

        tracked = []
        frame = evidence["canonicalFrame"]
        tracked.extend(frame["runs"])
        tracked.extend((frame["publishedJson"], frame["publishedMarkdown"]))
        tracked.extend((evidence["canonicalGc"], evidence["laneSummary"], evidence["focusedCaptureManifest"]))
        focused = evidence["focusedPerformance"]
        tracked.extend(focused["groundMissile"]["runs"])
        tracked.extend(value for key, value in focused.items() if key != "groundMissile")
        tracked.extend(evidence["governedSources"])
        for entry in tracked:
            self.assertEqual(entry["sha256"], sha256(entry["path"]), entry["path"])

    def test_focused_manifest_cryptographically_binds_context_runners_reports_and_logs(self) -> None:
        evidence = self.evidence
        manifest_entry = evidence["focusedCaptureManifest"]
        manifest = load_json(manifest_entry["path"])
        self.assertEqual(manifest_entry["sha256"], sha256(manifest_entry["path"]))
        self.assertEqual(manifest["result"], "Passed")
        self.assertEqual(manifest["captureContext"], evidence["captureContexts"]["focused"])

        governed = {entry["path"]: entry for entry in evidence["governedSources"]}
        focused_commit = evidence["focusedCaptureCommit"]
        manifest_logs = set()
        for artifact in manifest["artifacts"]:
            runner = governed[artifact["runnerPath"]]
            self.assertEqual(runner["captureContext"], "focused", artifact["id"])
            self.assertEqual(artifact["runnerSha256"], runner["sha256"], artifact["id"])
            self.assertEqual(
                artifact["runnerSha256"],
                sha256_bytes(git_bytes(focused_commit, artifact["runnerPath"])),
                artifact["id"],
            )
            for report in artifact["reportPaths"]:
                self.assertEqual(report["sha256"], sha256(report["path"]), report["path"])
            self.assert_log(artifact)
            manifest_logs.add(artifact["logPath"])

        focused = evidence["focusedPerformance"]
        expected_logs = {entry["logPath"] for entry in focused["groundMissile"]["runs"]}
        expected_logs.update(entry["logPath"] for key, entry in focused.items() if key != "groundMissile")
        expected_logs.update(entry["logPath"] for entry in evidence["behaviorValidation"])
        self.assertSetEqual(manifest_logs, expected_logs)

    def test_canonical_frame_uses_median_and_passes_frozen_policy(self) -> None:
        frame = self.evidence["canonicalFrame"]
        policy = self.policy["comparison"]["canonicalMatchFrame"]
        self.assertEqual(frame["selectionRule"], self.policy["comparison"]["batchSelection"])
        self.assertEqual(len(frame["runs"]), self.policy["comparison"]["repeatedBatchCount"])
        self.assertEqual(frame["relativeP95CeilingMs"], policy["relativeP95CeilingMs"])
        self.assertEqual(frame["absoluteP95CeilingMs"], policy["absoluteP95CeilingMs"])
        self.assertEqual(frame["allocatedBytesCeiling"], policy["currentThreadAllocatedBytesCeiling"])

        loaded_runs = []
        for entry in frame["runs"]:
            report = load_json(entry["path"])
            self.assertEqual(report["exactCommit"], self.evidence["canonicalCaptureCommit"])
            self.assertEqual(report["environmentIdentitySha256"], self.evidence["environmentIdentitySha256"])
            self.assertFalse(report["dirty"])
            self.assertEqual(report["qualityName"], "Mobile")
            self.assertEqual((report["resolutionWidth"], report["resolutionHeight"]), (640, 480))
            self.assertIn("profilerEnabled=false", report["instrumentationState"])
            self.assertEqual(report["allocatedBytesCurrentThread"], policy["currentThreadAllocatedBytesCeiling"])
            self.assertGreaterEqual(report["frameCount"], policy["minimumMeasuredFrames"])
            self.assertGreaterEqual(report["unitCount"], policy["minimumUnitCount"])
            self.assertGreaterEqual(report["runtimeBuildingCount"], policy["minimumRuntimeBuildingCount"])
            self.assertGreaterEqual(report["visibleModelEstimate"], policy["minimumVisibleModelEstimate"])
            self.assertAlmostEqual(entry["averageFrameMs"], report["averageFrameMs"])
            self.assertAlmostEqual(entry["p95FrameMs"], report["p95FrameMs"])
            loaded_runs.append((report["averageFrameMs"], report["p95FrameMs"], entry["run"], report))

        median = sorted(loaded_runs)[len(loaded_runs) // 2]
        self.assertEqual(frame["selectedRun"], median[2])
        self.assertLessEqual(median[3]["p95FrameMs"], policy["relativeP95CeilingMs"])
        self.assertLessEqual(median[3]["p95FrameMs"], policy["absoluteP95CeilingMs"])
        selected_entry = next(entry for entry in frame["runs"] if entry["run"] == frame["selectedRun"])
        self.assertEqual(frame["publishedJson"]["sha256"], selected_entry["sha256"])

    def test_canonical_gc_arithmetic_classification_and_lane_pass(self) -> None:
        gc = self.evidence["canonicalGc"]
        policy = self.policy["comparison"]["canonicalMatchGc"]
        self.assertEqual(gc["warmupFrames"], policy["warmupFrames"])
        self.assertEqual(gc["measuredFrames"], policy["measuredFrames"])
        self.assertEqual(gc["playerRelevantAllocatedBytesCeiling"], policy["playerRelevantAllocatedBytesCeiling"])
        self.assertLessEqual(gc["playerRelevantAllocatedBytes"], policy["playerRelevantAllocatedBytesCeiling"])
        self.assertEqual(gc["totalGcAllocSamples"], gc["playerRelevantSamples"] + gc["excludedSamples"])
        self.assertEqual(gc["totalGcAllocBytes"], gc["playerRelevantAllocatedBytes"] + gc["excludedBytes"])
        self.assertEqual(gc["totalGcAllocSamples"], gc["rawResolvedSamples"] + gc["rawUnresolvedSamples"])
        self.assertEqual(gc["totalGcAllocBytes"], gc["rawResolvedBytes"] + gc["rawUnresolvedBytes"])
        self.assertEqual(gc["classificationValidation"]["tests"], 41)
        self.assert_log(gc["classificationValidation"])

        report = (ROOT / gc["path"]).read_text(encoding="utf-8")
        self.assertIn(f'Exact commit: `{self.evidence["canonicalCaptureCommit"]}`', report)
        self.assertIn("Dirty at capture start: `false`", report)
        self.assertIn("Quality: `Mobile`", report)
        self.assertIn("Resolution: `640x480`", report)
        self.assertIn("Steady-state player-relevant GC budget: Passed (262 / 1024 bytes)", report)
        parsed = {
            "totalGcAllocSamples": re.search(r"^- GC\.Alloc samples: (\d+)$", report, re.MULTILINE),
            "totalGcAllocBytes": re.search(r"^- GC\.Alloc bytes from hierarchy column: (\d+)$", report, re.MULTILINE),
            "resolved": re.search(r"^- Raw allocation samples resolved: (\d+) \((\d+) bytes\)$", report, re.MULTILINE),
            "unresolved": re.search(
                r"^- Raw allocation samples conservatively unresolved: (\d+) across \d+ hierarchy items \((\d+) bytes\)$",
                report,
                re.MULTILINE,
            ),
            "playerRelevantSamples": re.search(
                r"^- GC\.Alloc samples excluding editor/tooling/diagnostic rows: (\d+)$",
                report,
                re.MULTILINE,
            ),
            "playerRelevantAllocatedBytes": re.search(
                r"^- GC\.Alloc bytes excluding editor/tooling/diagnostic rows: (\d+)$",
                report,
                re.MULTILINE,
            ),
            "excludedSamples": re.search(
                r"^- Editor/tooling/diagnostic GC\.Alloc samples excluded from player-relevant rows: (\d+)$",
                report,
                re.MULTILINE,
            ),
            "excludedBytes": re.search(
                r"^- Editor/tooling/diagnostic GC\.Alloc bytes excluded from player-relevant rows: (\d+)$",
                report,
                re.MULTILINE,
            ),
        }
        self.assertTrue(all(parsed.values()), "GC report attribution fields are incomplete")
        self.assertEqual(int(parsed["totalGcAllocSamples"].group(1)), gc["totalGcAllocSamples"])
        self.assertEqual(int(parsed["totalGcAllocBytes"].group(1)), gc["totalGcAllocBytes"])
        self.assertEqual(int(parsed["resolved"].group(1)), gc["rawResolvedSamples"])
        self.assertEqual(int(parsed["resolved"].group(2)), gc["rawResolvedBytes"])
        self.assertEqual(int(parsed["unresolved"].group(1)), gc["rawUnresolvedSamples"])
        self.assertEqual(int(parsed["unresolved"].group(2)), gc["rawUnresolvedBytes"])
        for field in ("playerRelevantSamples", "playerRelevantAllocatedBytes", "excludedSamples", "excludedBytes"):
            self.assertEqual(int(parsed[field].group(1)), gc[field])
        lane = load_json(self.evidence["laneSummary"]["path"])
        self.assertEqual(lane["result"], "Passed")
        self.assertEqual(lane["steadyStateGc"]["measuredBytes"], gc["playerRelevantAllocatedBytes"])

    def test_focused_performance_recomputes_medians_and_uses_frozen_policy(self) -> None:
        focused = self.evidence["focusedPerformance"]
        comparison = self.policy["comparison"]

        ground = focused["groundMissile"]
        ground_policy = comparison["groundMissile"]
        self.assertEqual(ground["selectionRule"], comparison["batchSelection"])
        self.assertEqual(ground["averageTotalMsCeiling"], ground_policy["averageTotalMsCeiling"])
        self.assertEqual(ground["p95TotalMsCeiling"], ground_policy["p95TotalMsCeiling"])
        ground_runs = []
        for entry in ground["runs"]:
            self.assert_log(entry)
            report = load_json(entry["path"])
            self.assertEqual(report["allocatedBytesCurrentThread"], ground_policy["allocatedBytesCeiling"])
            ground_runs.append((report["averageTotalMs"], report["p95TotalMs"], entry["run"], report))
        selected_ground = sorted(ground_runs)[len(ground_runs) // 2]
        self.assertEqual(ground["selectedRun"], selected_ground[2])
        self.assertLessEqual(selected_ground[3]["averageTotalMs"], ground_policy["averageTotalMsCeiling"])
        self.assertLessEqual(selected_ground[3]["p95TotalMs"], ground_policy["p95TotalMsCeiling"])

        transport_entry = focused["transportBoarding"]
        transport_policy = comparison["transportBoarding"]
        self.assert_log(transport_entry)
        transport = load_json(transport_entry["path"])
        self.assertEqual(transport["measuredBatches"], comparison["repeatedBatchCount"])
        self.assertEqual(transport_entry["averageTotalMsCeiling"], transport_policy["averageTotalMsCeiling"])
        self.assertEqual(transport_entry["p95TotalMsCeiling"], transport_policy["p95TotalMsCeiling"])
        median_batch = sorted(
            transport["batches"],
            key=lambda row: (row["averageTotalMs"], row["p95TotalMs"], row["batchIndex"]),
        )[len(transport["batches"]) // 2]
        self.assertEqual(transport["selectedBatchIndex"], median_batch["batchIndex"])
        self.assertEqual(transport["selectedBatch"], median_batch)
        for field in ("averageTotalMs", "p95TotalMs", "allocatedBytesCurrentThread"):
            self.assertEqual(transport[field], median_batch[field])
        self.assertLessEqual(transport["averageTotalMs"], transport_policy["averageTotalMsCeiling"])
        self.assertLessEqual(transport["p95TotalMs"], transport_policy["p95TotalMsCeiling"])
        self.assertEqual(transport["allocatedBytesCurrentThread"], transport_policy["allocatedBytesCeiling"])

        backend_entry = focused["resourceExchangeBackend"]
        backend_policy = comparison["resourceExchangeBackend"]
        self.assert_log(backend_entry)
        backend = load_json(backend_entry["path"])
        self.assertEqual(backend["warmupFrames"], backend_policy["warmupFrames"])
        self.assertEqual(backend["measuredFrames"], backend_policy["measuredFrames"])
        self.assertEqual(backend_entry["p95MsCeiling"], backend_policy["p95TotalMsCeiling"])
        self.assertEqual(backend_entry["p99MsCeiling"], backend_policy["p99TotalMsCeiling"])
        self.assertLessEqual(backend["p95Ms"], backend_policy["p95TotalMsCeiling"])
        self.assertLessEqual(backend["p99Ms"], backend_policy["p99TotalMsCeiling"])
        self.assertEqual(backend["allocatedBytesCurrentThread"], backend_policy["allocatedBytesCeiling"])

        gc_entry = focused["resourceExchangeGc"]
        self.assert_log(gc_entry)
        gc = load_json(gc_entry["path"])
        self.assertEqual(gc["tests"], 2)
        self.assertEqual(gc["allocatedBytesCeiling"], comparison["changedOwnerFocusedGc"]["allocatedBytesCeiling"])
        for field in (
            "queueTickAndValidationAllocatedBytes",
            "productionUpdateAllocatedBytes",
            "measurementWindowAllocatedBytes",
            "harnessAllocatedBytes",
        ):
            self.assertEqual(gc[field], comparison["changedOwnerFocusedGc"]["allocatedBytesCeiling"], field)

    def test_ui_shell_and_world_query_cache_are_policy_bound_and_allocation_free(self) -> None:
        focused = self.evidence["focusedPerformance"]
        shell_policy = self.policy["comparison"]["resourceExchangeShell"]
        unchanged_entry = focused["resourceExchangeShellUnchanged"]
        self.assert_log(unchanged_entry)
        unchanged = load_json(unchanged_entry["path"])
        self.assertEqual(unchanged["scenario"], shell_policy["requiredState"])
        self.assertEqual(unchanged["warmupFrames"], shell_policy["warmupFrames"])
        self.assertEqual(unchanged["measuredFrames"], shell_policy["measuredFrames"])
        self.assertTrue(unchanged["remainedFullyBound"])
        self.assertEqual(unchanged_entry["p95MsCeiling"], shell_policy["unchangedStateP95MsCeiling"])
        self.assertLessEqual(unchanged["p95FrameMs"], shell_policy["unchangedStateP95MsCeiling"])
        self.assertEqual(unchanged["measurementWindowAllocatedBytes"], shell_policy["unchangedStateAllocatedBytesCeiling"])

        transitions_entry = focused["resourceExchangeShellTransitions"]
        self.assert_log(transitions_entry)
        transitions = load_json(transitions_entry["path"])
        self.assertEqual(transitions["warmupTransitions"], shell_policy["warmupOpenCloseTransitions"])
        self.assertEqual(transitions["measuredTransitions"], shell_policy["measuredOpenCloseTransitions"])
        self.assertTrue(transitions["everyOpenFullyBound"])
        self.assertTrue(transitions["everyCloseDestroyedPopup"])
        self.assertEqual(transitions_entry["openP95MsCeiling"], shell_policy["openP95MsCeiling"])
        self.assertEqual(transitions_entry["closeP95MsCeiling"], shell_policy["closeP95MsCeiling"])
        self.assertLessEqual(transitions["p95OpenMs"], shell_policy["openP95MsCeiling"])
        self.assertLessEqual(transitions["p95CloseMs"], shell_policy["closeP95MsCeiling"])
        self.assertEqual(transitions["focusedUiOpenCloseAllocatedBytes"], shell_policy["recurringTransitionAllocatedBytesCeiling"])

        query_entry = focused["worldScopedQueryCache"]
        self.assert_log(query_entry)
        query_cache = load_json(query_entry["path"])
        self.assertEqual(query_cache["result"], "Passed")
        self.assertEqual(query_cache["combinations"], 2)
        self.assertEqual(query_cache["phases"], 4)
        self.assertEqual(len(query_cache["measurements"]), 4)
        allocation_ceiling = self.policy["comparison"]["changedOwnerFocusedGc"]["allocatedBytesCeiling"]
        self.assertTrue(all(row["allocatedBytes"] == allocation_ceiling for row in query_cache["measurements"]))

    def test_behavior_logs_and_compiler_validation_are_reproducible(self) -> None:
        validation = self.evidence["validation"]
        behavior = self.evidence["behaviorValidation"]
        self.assertEqual(validation["compilerErrors"], 0)
        self.assertEqual(validation["canonicalFrameRuns"], 3)
        self.assertEqual(validation["canonicalGcRuns"], 1)
        self.assertEqual(validation["behaviorTests"], sum(row["tests"] for row in behavior))
        self.assertEqual(validation["behaviorTests"], 212)
        self.assertEqual(validation["behaviorFailures"], 0)
        self.assertEqual(validation["gcClassificationTests"], 41)
        self.assertEqual(validation["unityAcceptanceTests"], 253)
        for entry in behavior:
            self.assertEqual(entry["failures"], 0)
            self.assert_log(entry)

    def test_owned_paths_are_exact_and_protected_work_is_absent(self) -> None:
        acceptance = self.acceptance
        source = acceptance["sourceBaseline"]
        accepted = acceptance["acceptedEvidence"]
        self.assert_git_identity(source["capturedCommit"], source["capturedTree"])
        self.assert_git_identity(source["integratedCommit"], source["integratedTree"])
        self.assert_git_identity(accepted["capturedCommit"], accepted["capturedTree"])
        self.assert_git_identity(accepted["integratedCommit"], accepted["integratedTree"])
        self.assert_is_ancestor(source["capturedCommit"], accepted["capturedCommit"])
        self.assert_is_ancestor(source["integratedCommit"], accepted["integratedCommit"])

        captured_evidence_bytes = git_bytes(accepted["capturedCommit"], accepted["path"])
        integrated_evidence_bytes = git_bytes(accepted["integratedCommit"], accepted["path"])
        self.assertEqual(captured_evidence_bytes, integrated_evidence_bytes)
        self.assertEqual(accepted["sha256"], sha256_bytes(integrated_evidence_bytes))
        self.assertEqual(accepted["sha256"], sha256(accepted["path"]))
        accepted_evidence = json.loads(integrated_evidence_bytes.decode("utf-8"))
        owned = set(accepted_evidence["ownedPaths"])
        changed = set(
            filter(
                None,
                git_text("diff", "--name-only", source["integratedCommit"], accepted["integratedCommit"]).splitlines(),
            )
        )
        self.assertSetEqual(changed, owned)
        self.assertTrue(all((ROOT / path).is_file() for path in owned))

        protected = re.compile(r"operation[_-]?map|firstlaunch|visual[_-]?lock|(^|/)audio(/|$)", re.IGNORECASE)
        self.assertFalse([path for path in owned if protected.search(path)])


if __name__ == "__main__":
    unittest.main()

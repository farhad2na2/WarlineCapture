#!/usr/bin/env python3
"""Audit dense-city generated artifact ownership without invoking Unity."""

from __future__ import annotations

import argparse
import json
import subprocess
from pathlib import Path
from typing import Callable, Iterable


REQUIRED_TRACKED_PATHS = (
    "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/"
    "opmap_skirmish_desert_base_01_dense_city_authoring_candidate.unity",
    "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/"
    "opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity",
    "Assets/Game/Configs/OperationMaps/Candidates/"
    "OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset",
    "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/"
    "Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity",
    "Assets/Game/GeneratedOperationMaps/DenseCity/opmap.skirmish.desert_base_01/"
    "Candidate/SharedMaterials/DenseCity_SkyBox_DOTS.mat",
    "Design/Architecture/dense_city_generated_output_ownership.md",
)

FINAL_REQUIRED_TRACKED_PATHS = (
    "Design/Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md",
    "Design/Architecture/dense_city_virtualized_render_proxy_android_60fps_implementation_tracker.md",
    "Design/AgentReports/2026-08-10_dense_city_final_operation_map_addressables_build_report.json",
    "Design/AgentReports/2026-08-10_dense_city_production_dependency_closure.json",
    "Design/AgentReports/2026-08-10_dense_city_static_retirement_validation.json",
    "Design/AgentReports/2026-08-11_dense_city_final_android_package_evidence.json",
    "Design/AgentReports/2026-08-11_dense_city_final_android_lifecycle_evidence.json",
    "Design/AgentReports/2026-08-11_dense_city_final_android_performance_acceptance.json",
    "Design/AgentReports/2026-08-11_dense_city_final_android_transform_bounds_parity.json",
    "Design/AgentReports/2026-08-11_dense_city_final_destruction_family_acceptance.json",
    "Design/AgentReports/2026-08-11_dense_city_final_evidence_index.json",
)

ANDROID_ACCEPTANCE_TRACKED_PATHS = (
    "Design/AgentReports/2026-08-11_dense_city_final_android_package_evidence.json",
    "Design/AgentReports/2026-08-11_dense_city_final_android_lifecycle_evidence.json",
    "Design/AgentReports/2026-08-11_dense_city_final_android_performance_acceptance.json",
    "Design/AgentReports/2026-08-11_dense_city_final_android_transform_bounds_parity.json",
    "Design/AgentReports/2026-08-11_dense_city_final_destruction_family_acceptance.json",
)

PERSISTENT_OUTPUT_PREFIXES = (
    "Assets/Game/GeneratedOperationMaps/DenseCity/",
    "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/",
    "Assets/Game/Configs/OperationMaps/Candidates/",
    "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/",
)

TRANSIENT_PREFIXES = (
    "Library/",
    "Temp/",
    "Logs/",
    "Obj/",
    "Build/",
    "Builds/",
    "UserSettings/",
    "MemoryCaptures/",
    "ProfilerCaptures/",
    "Recordings/",
    ".utmp/",
)

TRANSIENT_IGNORE_PROBES = (
    "Library/OperationMapDenseCityRuntimeContent/Addressables/catalog.json",
    "Library/OperationMapDenseCityRuntimeContent/Entities/entity-header.bin",
    "Library/OperationMapDenseCityRuntimeContentTransactions/probe/dense-addressables/catalog.json",
    "Library/OperationMapDenseCityRuntimeParity/dense_candidate_runtime_parity.bin",
    "Temp/DenseCity/test-result.xml",
    "Logs/DenseCity/validation.log",
    "Assets/Game/GeneratedAndroidBuild/dense-city-probe.unity",
    ".utmp/dense-city-probe",
)

FORBIDDEN_TRACKED_PARTS = (
    "__TransactionBackup",
    "DenseCityRuntimeContentBuildTemp",
)

REPORT_PATH = (
    "Design/AgentReports/2026-07-25_dense_city_generated_artifact_ownership_audit.json"
)


def _normalise(paths: Iterable[str]) -> tuple[str, ...]:
    normalised: set[str] = set()
    for path in paths:
        if not path:
            continue
        path = path.replace("\\", "/")
        if path.startswith("./"):
            path = path[2:]
        normalised.add(path)
    return tuple(sorted(normalised))


def _is_dense_evidence(path: str) -> bool:
    if path == REPORT_PATH:
        return False
    return (
        path.startswith("Design/AgentReports/")
        and "dense_city" in path.lower()
    )


def audit_paths(
    tracked_paths: Iterable[str],
    ignored: Callable[[str], bool],
    *,
    final_closeout: bool = False,
) -> dict[str, object]:
    tracked = _normalise(tracked_paths)
    tracked_set = set(tracked)
    missing_required = sorted(set(REQUIRED_TRACKED_PATHS) - tracked_set)
    transient_tracked = sorted(
        path for path in tracked if path.startswith(TRANSIENT_PREFIXES)
    )
    forbidden_tracked = sorted(
        path
        for path in tracked
        if any(part.lower() in path.lower() for part in FORBIDDEN_TRACKED_PARTS)
    )
    ignore_probe_failures = sorted(
        path for path in TRANSIENT_IGNORE_PROBES if not ignored(path)
    )
    missing_final_required = (
        sorted(set(FINAL_REQUIRED_TRACKED_PATHS) - tracked_set)
        if final_closeout
        else []
    )
    missing_android_acceptance = (
        sorted(set(ANDROID_ACCEPTANCE_TRACKED_PATHS) - tracked_set)
        if final_closeout
        else []
    )
    persistent_outputs = [
        path for path in tracked if path.startswith(PERSISTENT_OUTPUT_PREFIXES)
    ]
    dense_evidence = [path for path in tracked if _is_dense_evidence(path)]
    failures = (
        len(missing_required)
        + len(transient_tracked)
        + len(forbidden_tracked)
        + len(ignore_probe_failures)
        + len(missing_final_required)
    )

    final_artifact_set_complete = final_closeout and failures == 0
    android_acceptance_complete = (
        final_artifact_set_complete and not missing_android_acceptance
    )

    return {
        "schemaVersion": 1,
        "result": "PASS" if failures == 0 else "FAIL",
        "scope": (
            "final repository ownership closeout"
            if final_closeout
            else "repository ownership foundation"
        ),
        "finalArtifactSetComplete": final_artifact_set_complete,
        "androidAcceptanceComplete": android_acceptance_complete,
        "summary": {
            "requiredTrackedPathCount": len(REQUIRED_TRACKED_PATHS),
            "missingRequiredPathCount": len(missing_required),
            "persistentOutputTrackedFileCount": len(persistent_outputs),
            "denseEvidenceTrackedFileCount": len(dense_evidence),
            "transientTrackedViolationCount": len(transient_tracked),
            "forbiddenTransactionArtifactCount": len(forbidden_tracked),
            "transientIgnoreProbeCount": len(TRANSIENT_IGNORE_PROBES),
            "transientIgnoreProbeFailureCount": len(ignore_probe_failures),
            "finalRequiredTrackedPathCount": len(FINAL_REQUIRED_TRACKED_PATHS),
            "missingFinalRequiredPathCount": len(missing_final_required),
            "androidAcceptanceTrackedPathCount": len(
                ANDROID_ACCEPTANCE_TRACKED_PATHS
            ),
            "missingAndroidAcceptancePathCount": len(
                missing_android_acceptance
            ),
        },
        "violations": {
            "missingRequiredPaths": missing_required,
            "trackedTransientPaths": transient_tracked,
            "trackedForbiddenTransactionArtifacts": forbidden_tracked,
            "unignoredTransientProbePaths": ignore_probe_failures,
            "missingFinalRequiredPaths": missing_final_required,
            "missingAndroidAcceptancePaths": missing_android_acceptance,
        },
    }


def _git(root: Path, *arguments: str) -> subprocess.CompletedProcess[bytes]:
    return subprocess.run(
        ["git", *arguments],
        cwd=root,
        check=False,
        capture_output=True,
    )


def tracked_paths(root: Path) -> tuple[str, ...]:
    result = _git(root, "ls-files", "-z")
    if result.returncode != 0:
        raise RuntimeError(result.stderr.decode("utf-8", errors="replace").strip())
    return _normalise(result.stdout.decode("utf-8").split("\0"))


def is_ignored(root: Path, path: str) -> bool:
    result = _git(root, "check-ignore", "--quiet", "--no-index", "--", path)
    if result.returncode not in (0, 1):
        raise RuntimeError(result.stderr.decode("utf-8", errors="replace").strip())
    return result.returncode == 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path)
    parser.add_argument(
        "--final-closeout",
        action="store_true",
        help="Require the complete committed final evidence set and Android acceptance reports.",
    )
    arguments = parser.parse_args()
    root = arguments.root.resolve()
    report = audit_paths(
        tracked_paths(root),
        lambda path: is_ignored(root, path),
        final_closeout=arguments.final_closeout,
    )
    rendered = json.dumps(report, indent=2, sort_keys=True) + "\n"

    if arguments.output:
        output = arguments.output
        if not output.is_absolute():
            output = root / output
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(rendered, encoding="utf-8")
    else:
        print(rendered, end="")

    return 0 if report["result"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())

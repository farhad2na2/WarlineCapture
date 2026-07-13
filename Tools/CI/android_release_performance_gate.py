#!/usr/bin/env python3
"""Build and validate the non-Unity APH-804 Android release evidence contract."""

from __future__ import annotations

from pathlib import Path
from typing import Any

try:
    from Tools.CI.android_performance_evidence_gate import (
        GatePolicy, GateValidationError,
        build_orchestration_contract as _build_orchestration_contract,
        load_json, load_profile as _load_profile, percentile, run_cli,
        validate_evidence as _validate_evidence,
    )
except ModuleNotFoundError:  # Direct execution adds Tools/CI, not the repository root.
    from android_performance_evidence_gate import (
        GatePolicy, GateValidationError,
        build_orchestration_contract as _build_orchestration_contract,
        load_json, load_profile as _load_profile, percentile, run_cli,
        validate_evidence as _validate_evidence,
    )


SCHEMA_VERSION = 1
TASK_ID = "APH-804"
DEFAULT_PROFILE = Path("Tools/CI/android_release_30fps_reference_device_profile.json")
POLICY = GatePolicy(TASK_ID, "APH-804 AndroidReleaseGate", "release")


def load_profile(path: Path = DEFAULT_PROFILE) -> dict[str, Any]:
    return _load_profile(path, POLICY)


def validate_evidence(
    evidence: Any,
    profile: dict[str, Any],
    *,
    expected_revision: str,
    expected_apk_sha256: str,
    artifact_root: Path | None = None,
) -> dict[str, Any]:
    return _validate_evidence(
        evidence,
        profile,
        expected_revision=expected_revision,
        expected_apk_sha256=expected_apk_sha256,
        artifact_root=artifact_root,
        policy=POLICY,
    )


def build_orchestration_contract(
    profile: dict[str, Any], expected_revision: str, expected_apk_sha256: str
) -> dict[str, Any]:
    return _build_orchestration_contract(profile, expected_revision, expected_apk_sha256, POLICY)


def main() -> int:
    return run_cli(POLICY, DEFAULT_PROFILE, __doc__ or "")


if __name__ == "__main__":
    raise SystemExit(main())

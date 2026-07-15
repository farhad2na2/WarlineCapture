#!/usr/bin/env python3
"""Validate and compare same-artifact Android rebuild evidence for APH-510."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import operator
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Callable


SCHEMA_VERSION = 1
TASK_ID = "APH-510"
MARKER = "APH510Comparison"
SHA256_PATTERN = re.compile(r"^[0-9a-f]{64}$")
COMMIT_PATTERN = re.compile(r"^[0-9a-f]{40}$")

IDENTITY_KEYS = (
    "exactCommit",
    "dirty",
    "apkSha256",
    "aabSha256",
    "buildProfileSha256",
    "buildType",
    "packageName",
    "scriptingBackend",
    "targetArchitecture",
    "qualityTier",
    "requestedFrameRate",
    "actualFrameRate",
    "deviceProfile",
    "deviceSerial",
    "deviceManufacturer",
    "deviceModel",
    "deviceCodeName",
    "soc",
    "androidRelease",
    "sdkLevel",
    "resolutionWidth",
    "resolutionHeight",
    "scenario",
    "warmupSeconds",
    "sampleDurationSeconds",
    "graphicsApi",
)
REVISION_IDENTITY_KEYS = {"exactCommit", "apkSha256", "aabSha256"}

SOURCE_KINDS = {
    "devicePerformance": (
        "device-performance",
        (
            "installedSizeBytes",
            "peakAllocatedMemoryMB",
            "peakPssMemoryMB",
            "frameP95Ms",
            "frameP99Ms",
            "startupP95Ms",
        ),
    ),
    "categoryResidency": (
        "category-residency",
        (
            "textureMemoryBytes",
            "meshMemoryBytes",
            "audioMemoryBytes",
            "graphicsDriverMemoryBytes",
        ),
    ),
    "io": ("io", ("ioReadBytes", "ioWriteBytes")),
}

METRICS = (
    ("package", "apkSizeBytes", "bytes"),
    ("package", "aabSizeBytes", "bytes"),
    ("package", "installedSizeBytes", "bytes"),
    ("memory", "peakAllocatedMemoryMB", "MB"),
    ("memory", "peakPssMemoryMB", "MB"),
    ("memory", "textureMemoryBytes", "bytes"),
    ("memory", "meshMemoryBytes", "bytes"),
    ("memory", "audioMemoryBytes", "bytes"),
    ("memory", "graphicsDriverMemoryBytes", "bytes"),
    ("frame", "frameP95Ms", "ms"),
    ("frame", "frameP99Ms", "ms"),
    ("startup", "startupP95Ms", "ms"),
    ("io", "ioReadBytes", "bytes"),
    ("io", "ioWriteBytes", "bytes"),
)
METRIC_NAMES = tuple(metric for _, metric, _ in METRICS)
INTEGER_METRICS = {
    "apkSizeBytes",
    "aabSizeBytes",
    "installedSizeBytes",
    "textureMemoryBytes",
    "meshMemoryBytes",
    "audioMemoryBytes",
    "graphicsDriverMemoryBytes",
    "ioReadBytes",
    "ioWriteBytes",
}
ZERO_ALLOWED_METRICS = {"ioReadBytes", "ioWriteBytes"}

ABSOLUTE_COMPARISONS: dict[str, Callable[[float, float], bool]] = {
    "lessThan": operator.lt,
    "lessThanOrEqual": operator.le,
    "greaterThan": operator.gt,
    "greaterThanOrEqual": operator.ge,
    "equal": operator.eq,
}
RELATIVE_COMPARISONS = {"atLeastReductionPercent", "atMostIncreasePercent"}


class ComparisonValidationError(RuntimeError):
    """Raised when APH-510 input cannot be trusted for comparison."""


@dataclass(frozen=True)
class EvidenceBundle:
    identity: dict[str, Any]
    metrics: dict[str, int | float]
    bindings: dict[str, Any]


def _object(value: Any, path: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise ComparisonValidationError(f"{path} must be an object")
    return value


def _array(value: Any, path: str) -> list[Any]:
    if not isinstance(value, list):
        raise ComparisonValidationError(f"{path} must be an array")
    return value


def _exact_keys(value: dict[str, Any], expected: set[str], path: str) -> None:
    missing = sorted(expected - set(value))
    unknown = sorted(set(value) - expected)
    if missing:
        raise ComparisonValidationError(f"{path} is missing: {', '.join(missing)}")
    if unknown:
        raise ComparisonValidationError(f"{path} has unknown fields: {', '.join(unknown)}")


def _required_keys(value: dict[str, Any], required: set[str], path: str) -> None:
    missing = sorted(required - set(value))
    if missing:
        raise ComparisonValidationError(f"{path} is missing: {', '.join(missing)}")


def _string(value: Any, path: str) -> str:
    if not isinstance(value, str) or not value.strip():
        raise ComparisonValidationError(f"{path} must be a non-empty string")
    return value.strip()


def _integer(value: Any, path: str, *, minimum: int = 0) -> int:
    if isinstance(value, bool) or not isinstance(value, int) or value < minimum:
        raise ComparisonValidationError(f"{path} must be an integer >= {minimum}")
    return value


def _number(value: Any, path: str, *, positive: bool = False) -> int | float:
    if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value):
        raise ComparisonValidationError(f"{path} must be a finite number")
    if value < 0 or (positive and value <= 0):
        qualifier = "positive" if positive else "non-negative"
        raise ComparisonValidationError(f"{path} must be {qualifier}")
    return value


def _sha256_text(value: Any, path: str) -> str:
    digest = _string(value, path)
    if SHA256_PATTERN.fullmatch(digest) is None:
        raise ComparisonValidationError(
            f"{path} must be 64 lowercase hexadecimal characters"
        )
    return digest


def _commit(value: Any, path: str) -> str:
    revision = _string(value, path)
    if COMMIT_PATTERN.fullmatch(revision) is None:
        raise ComparisonValidationError(
            f"{path} must be 40 lowercase hexadecimal characters"
        )
    return revision


def _sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_json(path: Path, label: str | None = None) -> dict[str, Any]:
    if not path.is_file():
        raise ComparisonValidationError(f"missing JSON file: {path}")
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise ComparisonValidationError(f"invalid JSON '{path}': {exc}") from exc
    return _object(value, label or str(path))


def _descriptor(value: Any, path: str, *, sized: bool) -> dict[str, Any]:
    item = _object(value, path)
    keys = {"path", "sha256", "sizeBytes"} if sized else {"path", "sha256"}
    _exact_keys(item, keys, path)
    result: dict[str, Any] = {
        "path": _string(item["path"], f"{path}.path"),
        "sha256": _sha256_text(item["sha256"], f"{path}.sha256"),
    }
    if sized:
        result["sizeBytes"] = _integer(item["sizeBytes"], f"{path}.sizeBytes", minimum=1)
    return result


def _resolve_verified(
    descriptor: dict[str, Any], artifact_root: Path, path: str
) -> Path:
    root = artifact_root.resolve()
    described = Path(descriptor["path"])
    resolved = (described if described.is_absolute() else root / described).resolve()
    try:
        resolved.relative_to(root)
    except ValueError as exc:
        raise ComparisonValidationError(f"{path}.path escapes artifact root: {resolved}") from exc
    if not resolved.is_file() or resolved.stat().st_size == 0:
        raise ComparisonValidationError(f"{path}.path is missing or empty: {resolved}")
    actual_sha = _sha256_file(resolved)
    if actual_sha != descriptor["sha256"]:
        raise ComparisonValidationError(f"{path}.sha256 does not match file: {resolved}")
    if "sizeBytes" in descriptor and resolved.stat().st_size != descriptor["sizeBytes"]:
        raise ComparisonValidationError(f"{path}.sizeBytes does not match file: {resolved}")
    return resolved


def _validate_identity(value: Any, path: str) -> dict[str, Any]:
    identity = _object(value, path)
    _exact_keys(identity, set(IDENTITY_KEYS), path)
    _commit(identity["exactCommit"], f"{path}.exactCommit")
    if identity["dirty"] is not False:
        raise ComparisonValidationError(f"{path}.dirty must be false")
    for key in ("apkSha256", "aabSha256", "buildProfileSha256"):
        _sha256_text(identity[key], f"{path}.{key}")
    for key in (
        "buildType",
        "packageName",
        "scriptingBackend",
        "targetArchitecture",
        "qualityTier",
        "deviceProfile",
        "deviceSerial",
        "deviceManufacturer",
        "deviceModel",
        "deviceCodeName",
        "soc",
        "androidRelease",
        "scenario",
        "graphicsApi",
    ):
        _string(identity[key], f"{path}.{key}")
    for key in (
        "requestedFrameRate",
        "actualFrameRate",
        "sdkLevel",
        "resolutionWidth",
        "resolutionHeight",
        "warmupSeconds",
        "sampleDurationSeconds",
    ):
        _integer(identity[key], f"{path}.{key}", minimum=1)
    if identity["buildType"] != "release":
        raise ComparisonValidationError(f"{path}.buildType must be release")
    if identity["scriptingBackend"] != "IL2CPP":
        raise ComparisonValidationError(f"{path}.scriptingBackend must be IL2CPP")
    if identity["targetArchitecture"] != "ARM64":
        raise ComparisonValidationError(f"{path}.targetArchitecture must be ARM64")
    return dict(identity)


def _expect_equal(actual: Any, expected: Any, path: str) -> None:
    if actual != expected:
        raise ComparisonValidationError(
            f"{path} mismatch: expected {expected!r}, found {actual!r}"
        )


def _validate_build_profile(profile: dict[str, Any], identity: dict[str, Any], path: str) -> None:
    _required_keys(profile, {"schemaVersion", "taskId", "device", "build", "capture"}, path)
    _integer(profile["schemaVersion"], f"{path}.schemaVersion", minimum=1)
    _string(profile["taskId"], f"{path}.taskId")
    device = _object(profile["device"], f"{path}.device")
    build = _object(profile["build"], f"{path}.build")
    capture = _object(profile["capture"], f"{path}.capture")
    mappings = (
        (device, "serial", "deviceSerial"),
        (device, "manufacturer", "deviceManufacturer"),
        (device, "model", "deviceModel"),
        (device, "deviceCodeName", "deviceCodeName"),
        (device, "soc", "soc"),
        (device, "androidRelease", "androidRelease"),
        (device, "sdkLevel", "sdkLevel"),
        (device, "resolutionWidth", "resolutionWidth"),
        (device, "resolutionHeight", "resolutionHeight"),
        (build, "buildType", "buildType"),
        (build, "packageName", "packageName"),
        (build, "scriptingBackend", "scriptingBackend"),
        (build, "architecture", "targetArchitecture"),
        (build, "qualityTier", "qualityTier"),
        (build, "requestedFrameRate", "requestedFrameRate"),
        (build, "actualFrameRate", "actualFrameRate"),
        (capture, "warmupSeconds", "warmupSeconds"),
        (capture, "sustainedSampleSeconds", "sampleDurationSeconds"),
    )
    for container, source_key, identity_key in mappings:
        if source_key not in container:
            raise ComparisonValidationError(f"{path} is missing profile field: {source_key}")
        _expect_equal(container[source_key], identity[identity_key], f"{path}.{source_key}")


def _validate_build_report(
    report: dict[str, Any],
    identity: dict[str, Any],
    artifact: dict[str, Any],
    package_type: str,
    path: str,
) -> int:
    required = {
        "schemaVersion",
        "taskId",
        "status",
        "exactCommit",
        "dirty",
        "releaseBuildType",
        "packageType",
        "buildTarget",
        "scriptingBackend",
        "targetArchitecture",
        "detailedBuildReport",
        "artifactPath",
        "artifactBytes",
        "artifactSha256",
        "buildReportIncludedAssets",
    }
    _required_keys(report, required, path)
    _integer(report["schemaVersion"], f"{path}.schemaVersion", minimum=1)
    _expect_equal(report["taskId"], "APH-500", f"{path}.taskId")
    _expect_equal(report["status"], "complete", f"{path}.status")
    _expect_equal(report["exactCommit"], identity["exactCommit"], f"{path}.exactCommit")
    _expect_equal(report["dirty"], False, f"{path}.dirty")
    _expect_equal(report["releaseBuildType"], identity["buildType"], f"{path}.releaseBuildType")
    _expect_equal(report["packageType"], package_type, f"{path}.packageType")
    _expect_equal(report["buildTarget"], "Android", f"{path}.buildTarget")
    _expect_equal(report["scriptingBackend"], identity["scriptingBackend"], f"{path}.scriptingBackend")
    _expect_equal(report["targetArchitecture"], identity["targetArchitecture"], f"{path}.targetArchitecture")
    _expect_equal(report["detailedBuildReport"], True, f"{path}.detailedBuildReport")
    _string(report["artifactPath"], f"{path}.artifactPath")
    size = _integer(report["artifactBytes"], f"{path}.artifactBytes", minimum=1)
    _expect_equal(size, artifact["sizeBytes"], f"{path}.artifactBytes")
    expected_sha_key = "apkSha256" if package_type == "APK" else "aabSha256"
    _expect_equal(report["artifactSha256"], identity[expected_sha_key], f"{path}.artifactSha256")
    _expect_equal(report["artifactSha256"], artifact["sha256"], f"{path}.artifactSha256")
    _array(report["buildReportIncludedAssets"], f"{path}.buildReportIncludedAssets")
    return size


def _validate_metric(value: Any, metric: str, path: str) -> int | float:
    if metric in INTEGER_METRICS:
        minimum = 0 if metric in ZERO_ALLOWED_METRICS else 1
        return _integer(value, path, minimum=minimum)
    return _number(value, path, positive=True)


def _validate_measurement_source(
    source: dict[str, Any],
    expected_kind: str,
    measurement_names: tuple[str, ...],
    identity: dict[str, Any],
    path: str,
) -> dict[str, int | float]:
    _exact_keys(source, {"schemaVersion", "taskId", "kind", "identity", "measurements"}, path)
    _expect_equal(source["schemaVersion"], SCHEMA_VERSION, f"{path}.schemaVersion")
    _expect_equal(source["taskId"], TASK_ID, f"{path}.taskId")
    _expect_equal(source["kind"], expected_kind, f"{path}.kind")
    source_identity = _validate_identity(source["identity"], f"{path}.identity")
    if source_identity != identity:
        mismatches = [key for key in IDENTITY_KEYS if source_identity[key] != identity[key]]
        raise ComparisonValidationError(
            f"{path}.identity is mixed or stale: {', '.join(mismatches)}"
        )
    measurements = _object(source["measurements"], f"{path}.measurements")
    _exact_keys(measurements, set(measurement_names), f"{path}.measurements")
    return {
        metric: _validate_metric(measurements[metric], metric, f"{path}.measurements.{metric}")
        for metric in measurement_names
    }


def load_evidence_bundle(
    manifest: dict[str, Any],
    *,
    role: str,
    artifact_root: Path,
    expected_revision: str | None = None,
    expected_apk_sha256: str | None = None,
    expected_aab_sha256: str | None = None,
) -> EvidenceBundle:
    root_path = f"{role}Evidence"
    _exact_keys(
        manifest,
        {"schemaVersion", "taskId", "role", "identity", "artifacts", "sources"},
        root_path,
    )
    _expect_equal(manifest["schemaVersion"], SCHEMA_VERSION, f"{root_path}.schemaVersion")
    _expect_equal(manifest["taskId"], TASK_ID, f"{root_path}.taskId")
    _expect_equal(manifest["role"], role, f"{root_path}.role")
    identity = _validate_identity(manifest["identity"], f"{root_path}.identity")

    if expected_revision is not None:
        _commit(expected_revision, "expectedCandidateRevision")
        _expect_equal(identity["exactCommit"], expected_revision, f"{root_path}.identity.exactCommit")
    if expected_apk_sha256 is not None:
        _sha256_text(expected_apk_sha256, "expectedCandidateApkSha256")
        _expect_equal(identity["apkSha256"], expected_apk_sha256, f"{root_path}.identity.apkSha256")
    if expected_aab_sha256 is not None:
        _sha256_text(expected_aab_sha256, "expectedCandidateAabSha256")
        _expect_equal(identity["aabSha256"], expected_aab_sha256, f"{root_path}.identity.aabSha256")

    artifacts_value = _object(manifest["artifacts"], f"{root_path}.artifacts")
    _exact_keys(artifacts_value, {"apk", "aab"}, f"{root_path}.artifacts")
    artifacts = {
        name: _descriptor(artifacts_value[name], f"{root_path}.artifacts.{name}", sized=True)
        for name in ("apk", "aab")
    }
    if not artifacts["apk"]["path"].lower().endswith(".apk"):
        raise ComparisonValidationError(f"{root_path}.artifacts.apk.path must end with .apk")
    if not artifacts["aab"]["path"].lower().endswith(".aab"):
        raise ComparisonValidationError(f"{root_path}.artifacts.aab.path must end with .aab")
    _expect_equal(artifacts["apk"]["sha256"], identity["apkSha256"], f"{root_path}.artifacts.apk.sha256")
    _expect_equal(artifacts["aab"]["sha256"], identity["aabSha256"], f"{root_path}.artifacts.aab.sha256")

    source_names = {"buildProfile", "apkBuildReport", "aabBuildReport", *SOURCE_KINDS}
    sources_value = _object(manifest["sources"], f"{root_path}.sources")
    _exact_keys(sources_value, source_names, f"{root_path}.sources")
    sources = {
        name: _descriptor(sources_value[name], f"{root_path}.sources.{name}", sized=False)
        for name in sorted(source_names)
    }
    _expect_equal(
        sources["buildProfile"]["sha256"],
        identity["buildProfileSha256"],
        f"{root_path}.sources.buildProfile.sha256",
    )

    all_paths = [item["path"] for item in artifacts.values()] + [item["path"] for item in sources.values()]
    if len(all_paths) != len(set(all_paths)):
        raise ComparisonValidationError(f"{root_path} reuses an artifact or source path")

    verified_artifacts = {
        name: _resolve_verified(item, artifact_root, f"{root_path}.artifacts.{name}")
        for name, item in artifacts.items()
    }
    verified_sources = {
        name: _resolve_verified(item, artifact_root, f"{root_path}.sources.{name}")
        for name, item in sources.items()
    }

    profile = load_json(verified_sources["buildProfile"], f"{root_path}.buildProfile")
    _validate_build_profile(profile, identity, f"{root_path}.buildProfile")

    metrics: dict[str, int | float] = {}
    for name, package_type, metric in (
        ("apkBuildReport", "APK", "apkSizeBytes"),
        ("aabBuildReport", "AAB", "aabSizeBytes"),
    ):
        report = load_json(verified_sources[name], f"{root_path}.{name}")
        artifact_name = package_type.lower()
        metrics[metric] = _validate_build_report(
            report,
            identity,
            artifacts[artifact_name],
            package_type,
            f"{root_path}.{name}",
        )
        if verified_artifacts[artifact_name].stat().st_size != metrics[metric]:
            raise ComparisonValidationError(
                f"{root_path}.{name} size does not match verified {artifact_name} file"
            )

    for source_name, (kind, measurement_names) in SOURCE_KINDS.items():
        source = load_json(verified_sources[source_name], f"{root_path}.{source_name}")
        source_metrics = _validate_measurement_source(
            source,
            kind,
            measurement_names,
            identity,
            f"{root_path}.{source_name}",
        )
        overlap = set(metrics) & set(source_metrics)
        if overlap:
            raise ComparisonValidationError(
                f"{root_path}.{source_name} duplicates metrics: {', '.join(sorted(overlap))}"
            )
        metrics.update(source_metrics)

    _exact_keys(metrics, set(METRIC_NAMES), f"{root_path}.derivedMetrics")
    return EvidenceBundle(
        identity=identity,
        metrics=metrics,
        bindings={"artifacts": artifacts, "sources": sources},
    )


def load_limits(value: Any) -> dict[str, dict[str, Any]]:
    profile = _object(value, "limitsProfile")
    _exact_keys(profile, {"schemaVersion", "taskId", "limits"}, "limitsProfile")
    _expect_equal(profile["schemaVersion"], SCHEMA_VERSION, "limitsProfile.schemaVersion")
    _expect_equal(profile["taskId"], TASK_ID, "limitsProfile.taskId")
    limits_value = _object(profile["limits"], "limitsProfile.limits")
    _exact_keys(limits_value, set(METRIC_NAMES), "limitsProfile.limits")
    limits: dict[str, dict[str, Any]] = {}
    for metric in METRIC_NAMES:
        path = f"limitsProfile.limits.{metric}"
        limit = _object(limits_value[metric], path)
        _exact_keys(limit, {"comparison", "value", "status"}, path)
        comparison = _string(limit["comparison"], f"{path}.comparison")
        if comparison not in ABSOLUTE_COMPARISONS and comparison not in RELATIVE_COMPARISONS:
            raise ComparisonValidationError(
                f"{path}.comparison has unsupported comparison: {comparison}"
            )
        value = limit["value"]
        status = _string(limit["status"], f"{path}.status")
        if value is None:
            if status != "measurement-required":
                raise ComparisonValidationError(
                    f"{path} with a null value must have measurement-required status"
                )
        else:
            _number(value, f"{path}.value")
            if status != "tracked-budget":
                raise ComparisonValidationError(
                    f"{path} with a numeric value must have tracked-budget status"
                )
        limits[metric] = {
            "comparison": comparison,
            "value": value,
            "status": status,
        }
    return limits


def _clean_number(value: float) -> int | float:
    if value == 0:
        return 0
    if value.is_integer():
        return int(value)
    return round(value, 6)


def _percent_delta(baseline: int | float, candidate: int | float) -> int | float | None:
    if baseline == 0:
        return None
    return _clean_number((float(candidate) - float(baseline)) * 100.0 / float(baseline))


def _evaluate_limit(
    baseline: int | float,
    candidate: int | float,
    limit: dict[str, Any],
    metric: str,
) -> tuple[bool, str, dict[str, Any]]:
    if limit["value"] is None:
        return False, "measurement-required", {"basis": "none", "observed": None}
    threshold = float(limit["value"])
    comparison = limit["comparison"]
    if comparison in ABSOLUTE_COMPARISONS:
        observed = float(candidate)
        accepted = ABSOLUTE_COMPARISONS[comparison](observed, threshold)
        return accepted, "accepted" if accepted else "limit-failed", {
            "basis": "candidate",
            "observed": candidate,
        }
    if baseline == 0:
        raise ComparisonValidationError(
            f"limitsProfile.limits.{metric} cannot evaluate {comparison} from a zero baseline"
        )
    increase_percent = (float(candidate) - float(baseline)) * 100.0 / float(baseline)
    if comparison == "atLeastReductionPercent":
        observed = -increase_percent
        accepted = observed >= threshold
        basis = "reductionPercent"
    else:
        observed = increase_percent
        accepted = observed <= threshold
        basis = "increasePercent"
    return accepted, "accepted" if accepted else "limit-failed", {
        "basis": basis,
        "observed": _clean_number(observed),
    }


def compare_evidence(
    baseline: EvidenceBundle,
    candidate: EvidenceBundle,
    limits: dict[str, dict[str, Any]],
) -> dict[str, Any]:
    for key in IDENTITY_KEYS:
        if key in REVISION_IDENTITY_KEYS:
            continue
        if baseline.identity[key] != candidate.identity[key]:
            raise ComparisonValidationError(
                f"baseline/candidate identity mismatch for {key}: "
                f"{baseline.identity[key]!r} != {candidate.identity[key]!r}"
            )

    comparisons: list[dict[str, Any]] = []
    blockers: list[str] = []
    accepted_count = 0
    measurement_required_count = 0
    limit_failed_count = 0
    for category, metric, unit in METRICS:
        baseline_value = baseline.metrics[metric]
        candidate_value = candidate.metrics[metric]
        accepted, decision, evaluation = _evaluate_limit(
            baseline_value, candidate_value, limits[metric], metric
        )
        if accepted:
            accepted_count += 1
        elif decision == "measurement-required":
            measurement_required_count += 1
            blockers.append(f"{metric}: measurement-required limit is null")
        else:
            limit_failed_count += 1
            blockers.append(f"{metric}: tracked limit failed")
        comparisons.append(
            {
                "category": category,
                "metric": metric,
                "unit": unit,
                "baseline": baseline_value,
                "candidate": candidate_value,
                "delta": _clean_number(float(candidate_value) - float(baseline_value)),
                "deltaPercent": _percent_delta(baseline_value, candidate_value),
                "limit": limits[metric],
                "evaluation": evaluation,
                "accepted": accepted,
                "decision": decision,
            }
        )

    acceptance_ready = not blockers
    stable_identity = {
        key: candidate.identity[key]
        for key in IDENTITY_KEYS
        if key not in REVISION_IDENTITY_KEYS and key != "dirty"
    }
    return {
        "schemaVersion": SCHEMA_VERSION,
        "taskId": TASK_ID,
        "result": "Passed" if acceptance_ready else "NotAccepted",
        "acceptanceReady": acceptance_ready,
        "baseline": {
            "identity": baseline.identity,
            "bindings": baseline.bindings,
        },
        "candidate": {
            "identity": candidate.identity,
            "bindings": candidate.bindings,
        },
        "comparisonIdentity": stable_identity,
        "comparisons": comparisons,
        "summary": {
            "comparisonCount": len(comparisons),
            "acceptedCount": accepted_count,
            "measurementRequiredCount": measurement_required_count,
            "limitFailedCount": limit_failed_count,
            "blockers": blockers,
        },
    }


def compare_paths(
    *,
    baseline_path: Path,
    candidate_path: Path,
    limits_path: Path,
    baseline_artifact_root: Path | None,
    candidate_artifact_root: Path | None,
    expected_candidate_revision: str,
    expected_candidate_apk_sha256: str,
    expected_candidate_aab_sha256: str,
) -> dict[str, Any]:
    baseline_root = baseline_artifact_root or baseline_path.parent
    candidate_root = candidate_artifact_root or candidate_path.parent
    baseline = load_evidence_bundle(
        load_json(baseline_path), role="baseline", artifact_root=baseline_root
    )
    candidate = load_evidence_bundle(
        load_json(candidate_path),
        role="candidate",
        artifact_root=candidate_root,
        expected_revision=expected_candidate_revision,
        expected_apk_sha256=expected_candidate_apk_sha256,
        expected_aab_sha256=expected_candidate_aab_sha256,
    )
    limits = load_limits(load_json(limits_path))
    return compare_evidence(baseline, candidate, limits)


def render_json(result: dict[str, Any]) -> str:
    return json.dumps(result, indent=2, sort_keys=True, allow_nan=False) + "\n"


def _display_number(value: Any) -> str:
    if value is None:
        return "n/a"
    if isinstance(value, int):
        return str(value)
    return format(value, ".12g")


def _markdown_text(value: Any) -> str:
    return str(value).replace("|", "\\|").replace("`", "\\`")


def _limit_text(limit: dict[str, Any]) -> str:
    if limit["value"] is None:
        return "measurement-required / null"
    return f"{limit['comparison']} {_display_number(limit['value'])}"


def render_markdown(result: dict[str, Any]) -> str:
    baseline = result["baseline"]["identity"]
    candidate = result["candidate"]["identity"]
    lines = [
        "# APH-510 Android Category Comparison",
        "",
        f"- Result: **{result['result']}**",
        f"- Acceptance ready: **{str(result['acceptanceReady']).lower()}**",
        f"- Baseline revision: `{baseline['exactCommit']}`",
        f"- Baseline APK SHA-256: `{baseline['apkSha256']}`",
        f"- Baseline AAB SHA-256: `{baseline['aabSha256']}`",
        f"- Candidate revision: `{candidate['exactCommit']}`",
        f"- Candidate APK SHA-256: `{candidate['apkSha256']}`",
        f"- Candidate AAB SHA-256: `{candidate['aabSha256']}`",
        f"- Build profile SHA-256: `{candidate['buildProfileSha256']}`",
        f"- Device: `{_markdown_text(candidate['deviceProfile'])}` / `{_markdown_text(candidate['deviceSerial'])}`",
        "",
        "## Per-Category Deltas",
        "",
        "| Category | Metric | Unit | Baseline | Candidate | Delta | Delta % | Limit | Decision |",
        "|---|---|---|---:|---:|---:|---:|---|---|",
    ]
    for row in result["comparisons"]:
        delta_percent = (
            "n/a" if row["deltaPercent"] is None else f"{_display_number(row['deltaPercent'])}%"
        )
        lines.append(
            f"| {row['category']} | `{row['metric']}` | {row['unit']} | "
            f"{_display_number(row['baseline'])} | {_display_number(row['candidate'])} | "
            f"{_display_number(row['delta'])} | {delta_percent} | "
            f"{_limit_text(row['limit'])} | {row['decision']} |"
        )

    lines.extend(["", "## Acceptance Blockers", ""])
    blockers = result["summary"]["blockers"]
    if blockers:
        lines.extend(f"- {_markdown_text(blocker)}" for blocker in blockers)
    else:
        lines.append("- None.")

    lines.extend(["", "## Evidence Bindings", ""])
    for role in ("baseline", "candidate"):
        lines.extend(
            [
                f"### {role.title()}",
                "",
                "| Kind | Path | SHA-256 |",
                "|---|---|---|",
            ]
        )
        bindings = result[role]["bindings"]
        for group in ("artifacts", "sources"):
            for name in sorted(bindings[group]):
                descriptor = bindings[group][name]
                lines.append(
                    f"| {group}.{name} | `{_markdown_text(descriptor['path'])}` | "
                    f"`{descriptor['sha256']}` |"
                )
        lines.append("")
    return "\n".join(lines)


def _write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Compare hash-verified same-artifact Android evidence for APH-510."
    )
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--candidate", type=Path, required=True)
    parser.add_argument("--limits", type=Path, required=True)
    parser.add_argument("--baseline-artifact-root", type=Path)
    parser.add_argument("--candidate-artifact-root", type=Path)
    parser.add_argument("--expected-candidate-revision", required=True)
    parser.add_argument("--expected-candidate-apk-sha256", required=True)
    parser.add_argument("--expected-candidate-aab-sha256", required=True)
    parser.add_argument("--output-json", type=Path, required=True)
    parser.add_argument("--output-markdown", type=Path, required=True)
    args = parser.parse_args()

    if args.output_json.resolve() == args.output_markdown.resolve():
        print(f"[{MARKER}] result=Failed reason=output paths must be distinct")
        return 1
    try:
        result = compare_paths(
            baseline_path=args.baseline,
            candidate_path=args.candidate,
            limits_path=args.limits,
            baseline_artifact_root=args.baseline_artifact_root,
            candidate_artifact_root=args.candidate_artifact_root,
            expected_candidate_revision=args.expected_candidate_revision,
            expected_candidate_apk_sha256=args.expected_candidate_apk_sha256,
            expected_candidate_aab_sha256=args.expected_candidate_aab_sha256,
        )
        _write_text(args.output_json, render_json(result))
        _write_text(args.output_markdown, render_markdown(result))
    except (OSError, ComparisonValidationError) as exc:
        print(f"[{MARKER}] result=Failed reason={exc}")
        return 1

    print(
        f"[{MARKER}] result={result['result']} "
        f"revision={result['candidate']['identity']['exactCommit']} "
        f"device={result['candidate']['identity']['deviceSerial']} "
        f"comparisons={result['summary']['comparisonCount']}"
    )
    return 0 if result["acceptanceReady"] else 1


if __name__ == "__main__":
    raise SystemExit(main())

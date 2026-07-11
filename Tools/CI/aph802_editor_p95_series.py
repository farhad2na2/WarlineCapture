#!/usr/bin/env python3
"""Validate and summarize a same-revision Editor Match p95 capture series."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
import statistics
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable


SCHEMA = "WarlineCapture.APH802EditorP95Run.v1"
SERIES_SCHEMA = "WarlineCapture.APH802EditorP95Series.v1"
MINIMUM_ACCEPTED_RUNS = 5
EXPECTED_RUNNER = "Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline"
ARTIFACT_MARKER = re.compile(r"^APH802-Artifact-Id:\s*(?P<value>\S+)\s*$", re.MULTILINE)
COMMIT_MARKER = re.compile(r"^APH802-Exact-Commit:\s*(?P<value>[0-9a-f]{40})\s*$", re.MULTILINE)


class SeriesValidationError(RuntimeError):
    pass


def load_run_pair(json_path: Path) -> dict[str, Any]:
    markdown_path = json_path.with_suffix(".md")
    if not markdown_path.is_file():
        raise SeriesValidationError(f"missing Markdown pair for {json_path.name}")
    try:
        run = json.loads(json_path.read_text(encoding="utf-8-sig"))
        markdown = markdown_path.read_text(encoding="utf-8-sig")
    except (OSError, json.JSONDecodeError) as exc:
        raise SeriesValidationError(f"invalid run pair '{json_path.name}': {exc}") from exc
    if not isinstance(run, dict):
        raise SeriesValidationError(f"run JSON root must be an object: {json_path.name}")

    _validate_run_shape(run, json_path)
    artifact_id = run["artifactId"]
    if json_path.stem != artifact_id:
        raise SeriesValidationError(
            f"artifactId '{artifact_id}' must match filename stem '{json_path.stem}'"
        )
    artifact_marker = _single_marker(ARTIFACT_MARKER, markdown, "artifact id", markdown_path)
    commit_marker = _single_marker(COMMIT_MARKER, markdown, "exact commit", markdown_path)
    if artifact_marker != artifact_id:
        raise SeriesValidationError(f"Markdown artifact id does not match {json_path.name}")
    if commit_marker != run["exactCommit"]:
        raise SeriesValidationError(f"Markdown exact commit does not match {json_path.name}")

    return {
        **run,
        "jsonPath": json_path.as_posix(),
        "markdownPath": markdown_path.as_posix(),
        "jsonSha256": _sha256(json_path),
        "markdownSha256": _sha256(markdown_path),
    }


def build_series(
    input_paths: Iterable[Path],
    expected_commit: str,
    now_utc: datetime,
    max_age_hours: float,
) -> dict[str, Any]:
    paths = sorted(input_paths, key=lambda path: path.as_posix())
    if not paths:
        raise SeriesValidationError("no run JSON inputs were provided")
    if not re.fullmatch(r"[0-9a-f]{40}", expected_commit):
        raise SeriesValidationError("expected commit must be a 40-character lowercase SHA-1")
    if now_utc.tzinfo is None:
        raise SeriesValidationError("now_utc must be timezone-aware")
    if not math.isfinite(max_age_hours) or max_age_hours <= 0:
        raise SeriesValidationError("max age hours must be positive and finite")

    runs = [load_run_pair(path) for path in paths]
    artifact_ids = [run["artifactId"] for run in runs]
    if len(set(artifact_ids)) != len(artifact_ids):
        raise SeriesValidationError("duplicate artifactId values are not allowed")

    reference = runs[0]
    for run in runs:
        if run["exactCommit"] != expected_commit:
            raise SeriesValidationError(
                f"mixed or unexpected commit in {run['artifactId']}: {run['exactCommit']}"
            )
        _require_common(reference, run, "environment")
        _require_common(reference, run, "runner")
        _require_common(reference, run, "fixture")
        age_hours = (now_utc.astimezone(timezone.utc) - _parse_utc(run["capturedAtUtc"])).total_seconds() / 3600.0
        if age_hours < 0:
            raise SeriesValidationError(f"future capturedAtUtc in {run['artifactId']}")
        if age_hours > max_age_hours:
            raise SeriesValidationError(
                f"stale input {run['artifactId']}: age {age_hours:.2f} h exceeds {max_age_hours:.2f} h"
            )
        _validate_measurement_contract(run)

    accepted = [run for run in runs if run["decision"]["status"] == "Accepted"]
    rejected = [run for run in runs if run["decision"]["status"] == "Rejected"]
    if len(accepted) < MINIMUM_ACCEPTED_RUNS:
        raise SeriesValidationError(
            f"at least {MINIMUM_ACCEPTED_RUNS} accepted runs are required; found {len(accepted)}"
        )

    values = [float(run["measurements"]["p95FrameMs"]) for run in accepted]
    mean = statistics.fmean(values)
    sample_stdev = statistics.stdev(values)
    outliers = [
        {
            "artifactId": run["artifactId"],
            "p95FrameMs": run["measurements"]["p95FrameMs"],
            "rule": run["outlier"]["rule"],
            "reason": run["outlier"]["reason"],
        }
        for run in accepted
        if run["outlier"]["declared"]
    ]
    return {
        "schema": SERIES_SCHEMA,
        "result": "Passed",
        "exactCommit": expected_commit,
        "environment": reference["environment"],
        "runner": reference["runner"],
        "fixture": reference["fixture"],
        "maximumInputAgeHours": max_age_hours,
        "generatedAtUtc": _format_utc(now_utc),
        "runCount": len(runs),
        "acceptedRunCount": len(accepted),
        "rejectedRunCount": len(rejected),
        "statistics": {
            "sampleCount": len(values),
            "minimumP95FrameMs": min(values),
            "maximumP95FrameMs": max(values),
            "meanP95FrameMs": mean,
            "medianP95FrameMs": statistics.median(values),
            "sampleStandardDeviationMs": sample_stdev,
            "coefficientOfVariation": sample_stdev / mean if mean else 0.0,
            "coefficientOfVariationPercent": sample_stdev * 100.0 / mean if mean else 0.0,
        },
        "declaredOutliers": outliers,
        "runs": [
            {
                "artifactId": run["artifactId"],
                "capturedAtUtc": run["capturedAtUtc"],
                "status": run["decision"]["status"],
                "rejectionReasons": run["decision"]["rejectionReasons"],
                "p95FrameMs": run["measurements"]["p95FrameMs"],
                "declaredOutlier": run["outlier"]["declared"],
                "jsonPath": run["jsonPath"],
                "markdownPath": run["markdownPath"],
                "jsonSha256": run["jsonSha256"],
                "markdownSha256": run["markdownSha256"],
            }
            for run in runs
        ],
    }


def render_markdown(series: dict[str, Any]) -> str:
    stats = series["statistics"]
    lines = [
        "# APH-802 Same-Revision Editor P95 Series",
        "",
        f"- Result: `{series['result']}`",
        f"- Exact commit: `{series['exactCommit']}`",
        f"- Accepted runs: `{series['acceptedRunCount']}` / `{series['runCount']}`",
        f"- Runner: `{series['runner']['executeMethod']}`",
        f"- Fixture: `{series['fixture']['fixtureId']}`",
        "",
        "## Statistics",
        "",
        "| Statistic | P95 frame time |",
        "|---|---:|",
        f"| Sample count | {stats['sampleCount']} |",
        f"| Minimum | {stats['minimumP95FrameMs']:.6f} ms |",
        f"| Maximum | {stats['maximumP95FrameMs']:.6f} ms |",
        f"| Mean | {stats['meanP95FrameMs']:.6f} ms |",
        f"| Median | {stats['medianP95FrameMs']:.6f} ms |",
        f"| Sample standard deviation | {stats['sampleStandardDeviationMs']:.6f} ms |",
        f"| Coefficient of variation | {stats['coefficientOfVariationPercent']:.4f}% |",
        "",
        "## Runs",
        "",
        "| Artifact | Status | P95 ms | Outlier | JSON SHA-256 | Markdown SHA-256 |",
        "|---|---|---:|---|---|---|",
    ]
    for run in series["runs"]:
        lines.append(
            f"| `{run['artifactId']}` | {run['status']} | {run['p95FrameMs']:.6f} | "
            f"{'yes' if run['declaredOutlier'] else 'no'} | `{run['jsonSha256']}` | `{run['markdownSha256']}` |"
        )
    lines.extend(["", "## Declared Outliers", ""])
    if not series["declaredOutliers"]:
        lines.append("None declared.")
    else:
        for outlier in series["declaredOutliers"]:
            lines.append(
                f"- `{outlier['artifactId']}`: {outlier['p95FrameMs']:.6f} ms; "
                f"rule `{outlier['rule']}`; {outlier['reason']}"
            )
    return "\n".join(lines) + "\n"


def write_series_pair(series: dict[str, Any], output_json: Path, output_markdown: Path) -> None:
    if output_json.suffix.lower() != ".json" or output_markdown.suffix.lower() != ".md":
        raise SeriesValidationError("series outputs must be a JSON/Markdown pair")
    if output_json.stem != output_markdown.stem:
        raise SeriesValidationError("series output JSON/Markdown stems must match")
    if output_json.exists() or output_markdown.exists():
        raise SeriesValidationError("series output already exists; evidence is append-only")
    output_json.parent.mkdir(parents=True, exist_ok=True)
    output_markdown.parent.mkdir(parents=True, exist_ok=True)
    output_json.write_text(json.dumps(series, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    output_markdown.write_text(render_markdown(series), encoding="utf-8")


def _validate_run_shape(run: dict[str, Any], path: Path) -> None:
    required = (
        "schema", "artifactId", "capturedAtUtc", "exactCommit", "environment",
        "runner", "fixture", "measurements", "decision", "outlier",
    )
    missing = [key for key in required if key not in run]
    if missing:
        raise SeriesValidationError(f"missing fields in {path.name}: {missing}")
    if run["schema"] != SCHEMA:
        raise SeriesValidationError(f"unexpected schema in {path.name}")
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]{2,127}", str(run["artifactId"])):
        raise SeriesValidationError(f"invalid artifactId in {path.name}")
    if not re.fullmatch(r"[0-9a-f]{40}", str(run["exactCommit"])):
        raise SeriesValidationError(f"invalid exactCommit in {path.name}")
    _parse_utc(run["capturedAtUtc"])
    for key in ("environment", "runner", "fixture", "measurements", "decision", "outlier"):
        if not isinstance(run[key], dict):
            raise SeriesValidationError(f"{key} must be an object in {path.name}")

    _require_keys(run["environment"], (
        "unityVersion", "os", "machineId", "graphicsDevice", "qualityLevel",
        "resolution", "captureMode", "commandLine", "cacheStatePolicy",
    ), "environment", path)
    _require_keys(run["runner"], ("executeMethod", "measurementSemantics"), "runner", path)
    _require_keys(run["fixture"], (
        "fixtureId", "observationSeconds", "minimumUnitCount",
        "minimumRuntimeBuildingCount", "readyGate", "stableGate",
        "allocatedBytesBudget",
    ), "fixture", path)
    _require_keys(run["measurements"], (
        "frameCount", "p95FrameMs", "unitCount", "runtimeBuildingCount",
        "allocatedBytesCurrentThread", "readyGatePassed", "stableGatePassed",
    ), "measurements", path)
    _require_keys(run["decision"], ("status", "rejectionReasons"), "decision", path)
    _require_keys(run["outlier"], ("declared", "rule", "reason"), "outlier", path)

    environment = run["environment"]
    _require_nonempty_strings(
        environment,
        (
            "unityVersion", "os", "machineId", "graphicsDevice", "qualityLevel",
            "captureMode", "commandLine", "cacheStatePolicy",
        ),
        "environment",
        path,
    )
    resolution = environment["resolution"]
    if not isinstance(resolution, dict):
        raise SeriesValidationError(f"environment.resolution must be an object in {path.name}")
    _require_keys(resolution, ("width", "height"), "environment.resolution", path)
    for key in ("width", "height"):
        if isinstance(resolution[key], bool) or not isinstance(resolution[key], int) or resolution[key] <= 0:
            raise SeriesValidationError(f"invalid environment.resolution.{key} in {path.name}")

    _require_nonempty_strings(run["runner"], ("executeMethod", "measurementSemantics"), "runner", path)
    if run["runner"]["executeMethod"] != EXPECTED_RUNNER:
        raise SeriesValidationError(f"unexpected runner in {path.name}")

    fixture = run["fixture"]
    _require_nonempty_strings(fixture, ("fixtureId", "readyGate", "stableGate"), "fixture", path)
    _require_finite_number(fixture, "observationSeconds", path, positive=True)
    _require_integer(fixture, "minimumUnitCount", path, minimum=700)
    _require_integer(fixture, "minimumRuntimeBuildingCount", path, minimum=600)
    _require_integer(fixture, "allocatedBytesBudget", path, minimum=0)

    decision = run["decision"]
    if decision["status"] not in ("Accepted", "Rejected") or not isinstance(decision["rejectionReasons"], list):
        raise SeriesValidationError(f"invalid decision in {path.name}")
    if any(not isinstance(reason, str) or not reason.strip() for reason in decision["rejectionReasons"]):
        raise SeriesValidationError(f"decision rejection reasons must be non-empty strings in {path.name}")
    if decision["status"] == "Accepted" and decision["rejectionReasons"]:
        raise SeriesValidationError(f"accepted run has rejection reasons in {path.name}")
    if decision["status"] == "Rejected" and not decision["rejectionReasons"]:
        raise SeriesValidationError(f"rejected run requires reasons in {path.name}")
    outlier = run["outlier"]
    if not isinstance(outlier["declared"], bool):
        raise SeriesValidationError(f"outlier.declared must be boolean in {path.name}")
    if not isinstance(outlier["rule"], str) or not isinstance(outlier["reason"], str):
        raise SeriesValidationError(f"outlier rule and reason must be strings in {path.name}")
    if outlier["declared"] and (not str(outlier["rule"]).strip() or not str(outlier["reason"]).strip()):
        raise SeriesValidationError(f"declared outlier requires rule and reason in {path.name}")
    if not outlier["declared"] and (str(outlier["rule"]).strip() or str(outlier["reason"]).strip()):
        raise SeriesValidationError(f"undeclared outlier cannot carry rule or reason in {path.name}")


def _validate_measurement_contract(run: dict[str, Any]) -> None:
    fixture = run["fixture"]
    measured = run["measurements"]
    numeric_fields = (
        "frameCount", "p95FrameMs", "unitCount", "runtimeBuildingCount",
        "allocatedBytesCurrentThread",
    )
    for key in numeric_fields:
        value = measured[key]
        if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(float(value)) or value < 0:
            raise SeriesValidationError(f"invalid measurement {key} in {run['artifactId']}")
    for key in ("frameCount", "unitCount", "runtimeBuildingCount", "allocatedBytesCurrentThread"):
        if not isinstance(measured[key], int):
            raise SeriesValidationError(f"measurement {key} must be an integer in {run['artifactId']}")
    for key in ("readyGatePassed", "stableGatePassed"):
        if not isinstance(measured[key], bool):
            raise SeriesValidationError(f"measurement {key} must be boolean in {run['artifactId']}")
    if measured["frameCount"] <= 0 or measured["p95FrameMs"] <= 0:
        raise SeriesValidationError(f"non-positive frame sample in {run['artifactId']}")
    if run["decision"]["status"] != "Accepted":
        return
    failures = []
    if measured["unitCount"] < fixture["minimumUnitCount"]:
        failures.append("unit count")
    if measured["runtimeBuildingCount"] < fixture["minimumRuntimeBuildingCount"]:
        failures.append("runtime building count")
    if measured["allocatedBytesCurrentThread"] > fixture["allocatedBytesBudget"]:
        failures.append("allocated bytes")
    if measured["readyGatePassed"] is not True:
        failures.append("ready gate")
    if measured["stableGatePassed"] is not True:
        failures.append("stable gate")
    if failures:
        raise SeriesValidationError(
            f"accepted run {run['artifactId']} violates fixture contract: {', '.join(failures)}"
        )


def _require_common(reference: dict[str, Any], run: dict[str, Any], key: str) -> None:
    if run[key] != reference[key]:
        raise SeriesValidationError(f"mixed {key} in {run['artifactId']}")


def _require_keys(value: dict[str, Any], keys: tuple[str, ...], label: str, path: Path) -> None:
    missing = [key for key in keys if key not in value]
    if missing:
        raise SeriesValidationError(f"missing {label} fields in {path.name}: {missing}")


def _require_nonempty_strings(
    value: dict[str, Any],
    keys: tuple[str, ...],
    label: str,
    path: Path,
) -> None:
    for key in keys:
        if not isinstance(value[key], str) or not value[key].strip():
            raise SeriesValidationError(f"{label}.{key} must be a non-empty string in {path.name}")


def _require_finite_number(
    value: dict[str, Any],
    key: str,
    path: Path,
    positive: bool,
) -> None:
    item = value[key]
    if isinstance(item, bool) or not isinstance(item, (int, float)) or not math.isfinite(float(item)):
        raise SeriesValidationError(f"invalid fixture.{key} in {path.name}")
    if positive and item <= 0:
        raise SeriesValidationError(f"fixture.{key} must be positive in {path.name}")


def _require_integer(value: dict[str, Any], key: str, path: Path, minimum: int) -> None:
    item = value[key]
    if isinstance(item, bool) or not isinstance(item, int) or item < minimum:
        raise SeriesValidationError(f"invalid fixture.{key} in {path.name}")


def _single_marker(pattern: re.Pattern[str], text: str, label: str, path: Path) -> str:
    matches = list(pattern.finditer(text))
    if len(matches) != 1:
        raise SeriesValidationError(f"{path.name} must contain exactly one {label} marker")
    return matches[0].group("value")


def _parse_utc(value: Any) -> datetime:
    if not isinstance(value, str) or not value.endswith("Z"):
        raise SeriesValidationError("capturedAtUtc must use UTC Z notation")
    try:
        return datetime.fromisoformat(value[:-1] + "+00:00")
    except ValueError as exc:
        raise SeriesValidationError(f"invalid capturedAtUtc: {value}") from exc


def _format_utc(value: datetime) -> str:
    return value.astimezone(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z")


def _sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input-dir", type=Path, required=True)
    parser.add_argument("--expected-commit", required=True)
    parser.add_argument("--max-age-hours", type=float, required=True)
    parser.add_argument("--output-json", type=Path, required=True)
    parser.add_argument("--output-markdown", type=Path, required=True)
    args = parser.parse_args()
    try:
        input_paths = sorted(args.input_dir.glob("*.json"))
        series = build_series(
            input_paths,
            args.expected_commit,
            datetime.now(timezone.utc),
            args.max_age_hours,
        )
        write_series_pair(series, args.output_json, args.output_markdown)
    except (OSError, SeriesValidationError) as exc:
        print(f"[APH802EditorP95Series] result=Failed reason={exc}")
        return 1
    print(
        "[APH802EditorP95Series] result=Passed "
        f"accepted={series['acceptedRunCount']} meanP95Ms={series['statistics']['meanP95FrameMs']:.6f} "
        f"cvPercent={series['statistics']['coefficientOfVariationPercent']:.4f}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

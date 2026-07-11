#!/usr/bin/env python3
"""Preserve one canonical Editor Match baseline as an APH-802 run pair."""

from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path


RUNNER = "Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline"
PASS_MARKER = "[MatchRuntimeShellSmokeValidation] result=Passed [MatchRuntimeBaselineMetrics] result=Passed"


class PreserveError(ValueError):
    pass


def preserve(
    baseline_path: Path,
    log_path: Path,
    output_dir: Path,
    artifact_id: str,
    exact_commit: str,
    captured_at_utc: str,
    command_line: str,
    environment: dict,
) -> tuple[Path, Path]:
    if len(exact_commit) != 40 or any(ch not in "0123456789abcdef" for ch in exact_commit):
        raise PreserveError("exact commit must be 40 lowercase hexadecimal characters")
    if not artifact_id or any(ch not in "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-" for ch in artifact_id):
        raise PreserveError("invalid artifact id")
    _parse_utc(captured_at_utc)

    baseline = json.loads(baseline_path.read_text(encoding="utf-8"))
    log_text = log_path.read_text(encoding="utf-8")
    if baseline.get("source") != RUNNER:
        raise PreserveError("baseline runner does not match APH-802")
    if PASS_MARKER not in log_text:
        raise PreserveError("runner pass marker is missing")
    if not baseline.get("editorP95FrameBudgetPassed"):
        raise PreserveError("canonical p95 gate failed")

    ready_status = str(baseline.get("readyStatus", ""))
    stable_status = str(baseline.get("stableStatus", ""))
    required = {
        "observationSeconds": baseline.get("observationSeconds"),
        "frameCount": baseline.get("frameCount"),
        "p95FrameMs": baseline.get("p95FrameMs"),
        "unitCount": baseline.get("unitCount"),
        "runtimeBuildingCount": baseline.get("runtimeBuildingCount"),
        "allocatedBytesCurrentThread": baseline.get("allocatedBytesCurrentThread"),
    }
    if any(value is None for value in required.values()):
        raise PreserveError("canonical baseline is missing required measurements")

    accepted = (
        float(required["observationSeconds"]) >= 4.0
        and int(required["frameCount"]) >= 180
        and int(required["unitCount"]) >= 700
        and int(required["runtimeBuildingCount"]) >= 600
        and int(required["allocatedBytesCurrentThread"]) == 0
        and "matchSceneLoaded=1" in ready_status
        and "sourceKeys=" in stable_status
    )
    rejection_reasons: list[str] = []
    if not accepted:
        rejection_reasons.append("canonical runner acceptance contract failed")

    run = {
        "schema": "WarlineCapture.APH802EditorP95Run.v1",
        "artifactId": artifact_id,
        "capturedAtUtc": captured_at_utc,
        "exactCommit": exact_commit,
        "environment": {**environment, "commandLine": command_line},
        "runner": {
            "executeMethod": RUNNER,
            "measurementSemantics": "post-ready four-second Match delta-time observation",
        },
        "fixture": {
            "fixtureId": "match-runtime-baseline-733-units-628-buildings",
            "observationSeconds": 4.0,
            "minimumUnitCount": 700,
            "minimumRuntimeBuildingCount": 600,
            "readyGate": ready_status,
            "stableGate": stable_status,
            "allocatedBytesBudget": 0,
        },
        "measurements": {
            "frameCount": int(required["frameCount"]),
            "p95FrameMs": float(required["p95FrameMs"]),
            "unitCount": int(required["unitCount"]),
            "runtimeBuildingCount": int(required["runtimeBuildingCount"]),
            "allocatedBytesCurrentThread": int(required["allocatedBytesCurrentThread"]),
            "readyGatePassed": "matchSceneLoaded=1" in ready_status,
            "stableGatePassed": "sourceKeys=" in stable_status,
        },
        "decision": {
            "status": "Accepted" if accepted else "Rejected",
            "rejectionReasons": rejection_reasons,
        },
        "outlier": {"declared": False, "rule": "", "reason": ""},
    }

    output_dir.mkdir(parents=True, exist_ok=True)
    json_path = output_dir / f"{artifact_id}.json"
    markdown_path = output_dir / f"{artifact_id}.md"
    if json_path.exists() or markdown_path.exists():
        raise PreserveError("run output already exists; evidence is append-only")
    json_path.write_text(json.dumps(run, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    markdown_path.write_text(_render_markdown(run, baseline), encoding="utf-8")
    return json_path, markdown_path


def _render_markdown(run: dict, baseline: dict) -> str:
    return (
        "# APH-802 Editor P95 Run\n\n"
        f"APH802-Artifact-Id: {run['artifactId']}\n"
        f"APH802-Exact-Commit: {run['exactCommit']}\n\n"
        f"- Captured UTC: `{run['capturedAtUtc']}`\n"
        f"- Decision: `{run['decision']['status']}`\n"
        f"- Frames: `{baseline['frameCount']}`\n"
        f"- Average: `{baseline['averageFrameMs']:.3f} ms`\n"
        f"- P95: `{baseline['p95FrameMs']:.3f} ms`\n"
        f"- P99: `{baseline['p99FrameMs']:.3f} ms`\n"
        f"- Maximum: `{baseline['maxFrameMs']:.3f} ms`\n"
        f"- Current-thread allocation: `{baseline['allocatedBytesCurrentThread']} bytes`\n"
        f"- Units/buildings: `{baseline['unitCount']} / {baseline['runtimeBuildingCount']}`\n"
    )


def _parse_utc(value: str) -> datetime:
    if not value.endswith("Z"):
        raise PreserveError("captured time must use UTC Z notation")
    try:
        return datetime.fromisoformat(value[:-1] + "+00:00").astimezone(timezone.utc)
    except ValueError as exc:
        raise PreserveError("invalid captured time") from exc


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--log", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--artifact-id", required=True)
    parser.add_argument("--exact-commit", required=True)
    parser.add_argument("--captured-at-utc", required=True)
    parser.add_argument("--command-line", required=True)
    parser.add_argument("--unity-version", required=True)
    parser.add_argument("--os", required=True)
    parser.add_argument("--machine-id", required=True)
    parser.add_argument("--graphics-device", required=True)
    parser.add_argument("--quality-level", required=True)
    parser.add_argument("--resolution-width", type=int, required=True)
    parser.add_argument("--resolution-height", type=int, required=True)
    parser.add_argument("--capture-mode", required=True)
    parser.add_argument("--cache-state-policy", required=True)
    args = parser.parse_args()
    environment = {
        "unityVersion": args.unity_version,
        "os": args.os,
        "machineId": args.machine_id,
        "graphicsDevice": args.graphics_device,
        "qualityLevel": args.quality_level,
        "resolution": {"width": args.resolution_width, "height": args.resolution_height},
        "captureMode": args.capture_mode,
        "cacheStatePolicy": args.cache_state_policy,
    }
    preserve(args.baseline, args.log, args.output_dir, args.artifact_id, args.exact_commit,
             args.captured_at_utc, args.command_line, environment)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

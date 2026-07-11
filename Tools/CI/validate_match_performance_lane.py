#!/usr/bin/env python3
"""Fail-closed validation for the scheduled Editor Match performance lane."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from typing import Any


EXPECTED_BASELINE_SOURCE = (
    "Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline"
)
GC_BUDGET_PATTERN = re.compile(
    r"Steady-state player-relevant GC budget:\s*"
    r"(?P<status>Passed|Failed)\s*"
    r"\((?P<measured>[0-9]+)\s*/\s*(?P<budget>[0-9]+)\s+bytes\)"
)


class ValidationError(RuntimeError):
    pass


def _load_json_object(path: Path) -> dict[str, Any]:
    if not path.is_file():
        raise ValidationError(f"missing baseline JSON: {path}")
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ValidationError(f"invalid baseline JSON '{path}': {exc}") from exc
    if not isinstance(value, dict):
        raise ValidationError(f"baseline JSON root must be an object: {path}")
    return value


def _required_number(data: dict[str, Any], key: str) -> float:
    value = data.get(key)
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValidationError(f"baseline field '{key}' must be numeric")
    return float(value)


def validate(
    baseline_path: Path,
    gc_report_path: Path,
    expected_gc_budget_bytes: int,
) -> dict[str, Any]:
    if expected_gc_budget_bytes != 1024:
        raise ValidationError(
            "the steady-state GC budget is fixed at 1024 bytes; "
            f"received {expected_gc_budget_bytes}"
        )

    baseline = _load_json_object(baseline_path)
    if baseline.get("source") != EXPECTED_BASELINE_SOURCE:
        raise ValidationError("baseline JSON has an unexpected or missing source")

    frame_count = int(_required_number(baseline, "frameCount"))
    p95_ms = _required_number(baseline, "p95FrameMs")
    p95_budget_ms = _required_number(baseline, "editorP95FrameBudgetMs")
    if frame_count <= 0:
        raise ValidationError("baseline frameCount must be greater than zero")
    if baseline.get("editorP95FrameBudgetPassed") is not True:
        raise ValidationError("baseline editor p95 budget did not pass")
    if p95_ms > p95_budget_ms:
        raise ValidationError(
            f"baseline p95 {p95_ms} ms exceeds its {p95_budget_ms} ms budget"
        )

    if not gc_report_path.is_file():
        raise ValidationError(f"missing steady-state GC report: {gc_report_path}")
    try:
        gc_report = gc_report_path.read_text(encoding="utf-8-sig")
    except OSError as exc:
        raise ValidationError(f"could not read GC report '{gc_report_path}': {exc}") from exc

    matches = list(GC_BUDGET_PATTERN.finditer(gc_report))
    if len(matches) != 1:
        raise ValidationError(
            "steady-state GC report must contain exactly one parseable budget result"
        )
    match = matches[0]
    status = match.group("status")
    measured_bytes = int(match.group("measured"))
    report_budget_bytes = int(match.group("budget"))
    if report_budget_bytes != expected_gc_budget_bytes:
        raise ValidationError(
            f"GC report budget changed to {report_budget_bytes}; "
            f"expected {expected_gc_budget_bytes}"
        )
    if status != "Passed" or measured_bytes > expected_gc_budget_bytes:
        raise ValidationError(
            f"steady-state GC budget failed: {measured_bytes} / "
            f"{expected_gc_budget_bytes} bytes"
        )

    return {
        "result": "Passed",
        "baseline": {
            "source": EXPECTED_BASELINE_SOURCE,
            "frameCount": frame_count,
            "p95FrameMs": p95_ms,
            "editorP95FrameBudgetMs": p95_budget_ms,
        },
        "steadyStateGc": {
            "measuredBytes": measured_bytes,
            "budgetBytes": expected_gc_budget_bytes,
            "status": status,
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--baseline-json", type=Path, required=True)
    parser.add_argument("--gc-report", type=Path, required=True)
    parser.add_argument("--expected-gc-budget-bytes", type=int, default=1024)
    parser.add_argument("--output-json", type=Path, required=True)
    args = parser.parse_args()

    try:
        summary = validate(
            args.baseline_json,
            args.gc_report,
            args.expected_gc_budget_bytes,
        )
        args.output_json.parent.mkdir(parents=True, exist_ok=True)
        args.output_json.write_text(
            json.dumps(summary, indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
        )
    except (OSError, ValidationError) as exc:
        print(f"[MatchPerformanceLane] result=Failed reason={exc}")
        return 1

    print(
        "[MatchPerformanceLane] result=Passed "
        f"p95Ms={summary['baseline']['p95FrameMs']} "
        f"gcBytes={summary['steadyStateGc']['measuredBytes']} "
        f"gcBudgetBytes={summary['steadyStateGc']['budgetBytes']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

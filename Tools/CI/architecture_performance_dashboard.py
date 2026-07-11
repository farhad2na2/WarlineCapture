#!/usr/bin/env python3
"""Generate the tracked architecture/performance dashboard from JSON evidence."""

from __future__ import annotations

import argparse
import json
import subprocess
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Callable


SCHEMA_VERSION = 1
DEFAULT_JSON = "Design/AgentReports/architecture_performance_dashboard.json"
DEFAULT_MARKDOWN = "Design/AgentReports/architecture_performance_dashboard.md"


@dataclass(frozen=True)
class InputSpec:
    path: str
    category: str
    metric_reader: Callable[[dict[str, Any]], dict[str, Any]]


def scalar_items(value: Any) -> dict[str, Any]:
    if not isinstance(value, dict):
        return {}
    return {
        str(key): item
        for key, item in sorted(value.items())
        if isinstance(item, (bool, int, float)) and item is not None
    }


def build_metrics(data: dict[str, Any]) -> dict[str, Any]:
    excluded = {"schemaVersion"}
    return {
        key: value
        for key, value in scalar_items(data).items()
        if key not in excluded
    }


def summary_metrics(data: dict[str, Any]) -> dict[str, Any]:
    return scalar_items(data.get("summary"))


def performance_metrics(data: dict[str, Any]) -> dict[str, Any]:
    return scalar_items(data)


def audio_metrics(data: dict[str, Any]) -> dict[str, Any]:
    snapshots = data.get("snapshots")
    if not isinstance(snapshots, list):
        return {"snapshotCount": 0}

    metrics: dict[str, Any] = {"snapshotCount": len(snapshots)}
    for index, snapshot in enumerate(snapshots):
        if not isinstance(snapshot, dict):
            continue
        phase = snapshot.get("phase")
        prefix = str(phase) if isinstance(phase, str) and phase else f"snapshot-{index:03d}"
        for key, value in scalar_items(snapshot).items():
            metrics[f"{prefix}.{key}"] = value
    return dict(sorted(metrics.items()))


INPUTS = tuple(sorted((
    InputSpec(
        "Design/AgentReports/2026-07-10_aph-700_first_party_assembly_dependencies.json",
        "architecture",
        summary_metrics,
    ),
    InputSpec(
        "Design/AgentReports/aph-401_audio-memory-playback-match.json",
        "audio",
        audio_metrics,
    ),
    InputSpec(
        "Design/AgentReports/aph-401_audio-memory-playback-menu.json",
        "audio",
        audio_metrics,
    ),
    InputSpec(
        "Design/AgentReports/architecture_performance_android_aab_build_report.json",
        "build",
        build_metrics,
    ),
    InputSpec(
        "Design/AgentReports/architecture_performance_android_apk_build_report.json",
        "build",
        build_metrics,
    ),
    InputSpec(
        "Design/AgentReports/architecture_performance_content_residency_baseline.json",
        "content-residency",
        summary_metrics,
    ),
    InputSpec(
        "Design/AgentReports/performance_regression_match_baseline.json",
        "runtime-performance",
        performance_metrics,
    ),
), key=lambda spec: spec.path))


def git_revision(root: Path) -> str:
    result = subprocess.run(
        ["git", "rev-parse", "HEAD"],
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    )
    return result.stdout.strip()


def claimed_revision(data: dict[str, Any]) -> tuple[str | None, str | None]:
    for field in ("exactCommit", "baselineCommit"):
        value = data.get(field)
        if isinstance(value, str) and value:
            return field, value
    return None, None


def freshness(data: dict[str, Any], revision: str) -> tuple[str, list[str]]:
    field, claimed = claimed_revision(data)
    reasons: list[str] = []
    if claimed is None:
        return "unknown", ["input does not declare exactCommit or baselineCommit"]
    if claimed != revision:
        reasons.append(f"{field} {claimed} does not match dashboard revision {revision}")
    if data.get("dirty") is True:
        reasons.append("input declares dirty=true")
    return ("stale", reasons) if reasons else ("current", [])


def read_input(root: Path, spec: InputSpec, revision: str) -> dict[str, Any]:
    path = root / spec.path
    base: dict[str, Any] = {
        "category": spec.category,
        "freshness": "missing",
        "metrics": {},
        "path": spec.path,
        "reasons": ["expected tracked JSON input is missing"],
    }
    if not path.is_file():
        return base
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as error:
        base["freshness"] = "invalid"
        base["reasons"] = [f"input is not readable JSON: {error}"]
        return base
    if not isinstance(data, dict):
        base["freshness"] = "invalid"
        base["reasons"] = ["input root must be a JSON object"]
        return base

    state, reasons = freshness(data, revision)
    field, claimed = claimed_revision(data)
    base.update({
        "claimedRevision": claimed,
        "claimedRevisionField": field,
        "dirty": data.get("dirty") if isinstance(data.get("dirty"), bool) else None,
        "freshness": state,
        "metrics": spec.metric_reader(data),
        "reasons": reasons,
        "sourceStatus": data.get("status", data.get("captureResult")),
        "taskId": data.get("taskId"),
    })
    return base


def build_dashboard(root: Path, revision: str) -> dict[str, Any]:
    inputs = [read_input(root, spec, revision) for spec in INPUTS]
    freshness_counts = {
        state: sum(item["freshness"] == state for item in inputs)
        for state in ("current", "stale", "unknown", "missing", "invalid")
    }
    return {
        "dashboardRevision": revision,
        "inputCount": len(inputs),
        "inputs": inputs,
        "schemaVersion": SCHEMA_VERSION,
        "summary": {
            "freshnessCounts": freshness_counts,
            "healthyInputCount": freshness_counts["current"],
            "requiresAttentionCount": len(inputs) - freshness_counts["current"],
        },
    }


def format_value(value: Any) -> str:
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, float):
        return format(value, ".12g")
    return str(value)


def render_markdown(dashboard: dict[str, Any]) -> str:
    summary = dashboard["summary"]
    counts = summary["freshnessCounts"]
    lines = [
        "# Architecture and Performance Dashboard",
        "",
        "> Generated by `python3 Tools/CI/architecture_performance_dashboard.py`; do not edit manually.",
        "",
        f"- Dashboard revision: `{dashboard['dashboardRevision']}`",
        f"- Inputs: {dashboard['inputCount']}",
        f"- Current: {counts['current']}",
        f"- Requires attention: {summary['requiresAttentionCount']}",
        "",
        "## Input Health",
        "",
        "| Input | Category | Freshness | Source status | Claimed revision |",
        "|---|---|---|---|---|",
    ]
    for item in dashboard["inputs"]:
        lines.append(
            "| `{path}` | {category} | **{freshness}** | {status} | {revision} |".format(
                path=item["path"],
                category=item["category"],
                freshness=item["freshness"],
                status=item.get("sourceStatus") or "unknown",
                revision=f"`{item['claimedRevision']}`" if item.get("claimedRevision") else "unknown",
            )
        )

    lines.extend(["", "## Details", ""])
    for item in dashboard["inputs"]:
        lines.extend([f"### `{item['path']}`", "", f"Freshness: **{item['freshness']}**", ""])
        if item["reasons"]:
            lines.append("Reasons:")
            lines.extend(f"- {reason}" for reason in item["reasons"])
            lines.append("")
        metrics = item["metrics"]
        if metrics:
            lines.extend(["| Metric | Value |", "|---|---:|"])
            lines.extend(f"| `{key}` | {format_value(value)} |" for key, value in sorted(metrics.items()))
        else:
            lines.append("No metrics available.")
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def write_dashboard(root: Path, json_path: str, markdown_path: str, revision: str) -> None:
    dashboard = build_dashboard(root, revision)
    json_output = root / json_path
    markdown_output = root / markdown_path
    json_output.parent.mkdir(parents=True, exist_ok=True)
    markdown_output.parent.mkdir(parents=True, exist_ok=True)
    json_output.write_text(json.dumps(dashboard, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    markdown_output.write_text(render_markdown(dashboard), encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--revision", help="Revision used for input freshness checks")
    parser.add_argument("--json-output", default=DEFAULT_JSON)
    parser.add_argument("--markdown-output", default=DEFAULT_MARKDOWN)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    root = args.root.resolve()
    write_dashboard(
        root,
        args.json_output,
        args.markdown_output,
        args.revision or git_revision(root),
    )


if __name__ == "__main__":
    main()

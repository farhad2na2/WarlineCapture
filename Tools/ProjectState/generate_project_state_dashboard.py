#!/usr/bin/env python3
"""Generate the WarlineCapture project-state dashboard from its JSON source."""

from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Design" / "Project_State_Source.json"
OUTPUT = ROOT / "Design" / "Project_State_Dashboard.md"


STATUS_LABELS = {
    "done": "Done",
    "in_progress": "In Progress",
    "on_hold": "On Hold",
    "blocked": "Blocked",
    "planned": "Planned",
}


STATUS_ORDER = {
    "done": 0,
    "in_progress": 1,
    "on_hold": 2,
    "blocked": 3,
    "planned": 4,
}


def load_source() -> dict:
    with SOURCE.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def markdown_escape(value: object) -> str:
    text = str(value)
    return text.replace("|", "\\|").replace("\n", "<br>")


def status_label(status: str) -> str:
    return STATUS_LABELS.get(status, status.replace("_", " ").title())


def weighted_completion(plans: list[dict]) -> int:
    total_weight = sum(float(plan.get("weight", 1)) for plan in plans)
    if total_weight <= 0:
        return 0
    total = sum(float(plan.get("percentComplete", 0)) * float(plan.get("weight", 1)) for plan in plans)
    return round(total / total_weight)


def status_counts(plans: list[dict]) -> dict[str, int]:
    counts: dict[str, int] = {}
    for plan in plans:
        status = plan.get("status", "planned")
        counts[status] = counts.get(status, 0) + 1
    return counts


def plan_lookup(data: dict) -> dict[str, str]:
    lookup: dict[str, str] = {}
    for plan in data.get("plans", []):
        lookup[plan["id"]] = plan["title"]
    for stage in data.get("roadmapStages", []):
        lookup[stage["id"]] = stage["title"]
    return lookup


def mermaid_id(raw: str) -> str:
    return raw.replace(".", "_").replace("-", "_")


def render_summary(data: dict) -> list[str]:
    plans = data.get("plans", [])
    stages = data.get("roadmapStages", [])
    counts = status_counts(plans)
    overall = weighted_completion(plans)
    forecast = data.get("completionForecast", {})
    lines = [
        "# WarlineCapture Project State Dashboard",
        "",
        f"Generated from `Design/Project_State_Source.json` on `{data.get('lastUpdated', 'unknown')}`.",
        "",
        "> Do not manually edit this dashboard. Update the JSON source and run `python3 Tools/ProjectState/generate_project_state_dashboard.py`.",
        "",
        "## Quick Read",
        "",
        f"- Overall estimated completion: **{overall}%**",
    ]
    if forecast:
        range_start = forecast.get("targetRangeStart", "unknown")
        range_end = forecast.get("targetRangeEnd", "unknown")
        lines.extend(
            [
                f"- Estimated 100% planning date: **{forecast.get('estimated100PercentDate', 'unknown')}**",
                f"- Forecast range: **{range_start} to {range_end}**",
                f"- Forecast confidence: **{forecast.get('confidence', 'unknown')}**",
            ]
        )
        if forecast.get("updateCadence"):
            lines.append(f"- Forecast update cadence: **{forecast.get('updateCadence')}**")
        if forecast.get("basis"):
            lines.append(f"- Forecast basis: {forecast.get('basis')}")
    lines.extend(
        [
        f"- Plans tracked: **{len(plans)}**",
        f"- Roadmap stages tracked: **{len(stages)}**",
        f"- Done: **{counts.get('done', 0)}**",
        f"- In progress: **{counts.get('in_progress', 0)}**",
        f"- On hold: **{counts.get('on_hold', 0)}**",
        f"- Blocked: **{counts.get('blocked', 0)}**",
        f"- Planned: **{counts.get('planned', 0)}**",
        "",
        ]
    )
    return lines


def render_roadmap(stages: list[dict]) -> list[str]:
    lines = [
        "## Roadmap",
        "",
        "| Stage | Status | Completion | Depends On | Summary |",
        "| --- | --- | ---: | --- | --- |",
    ]
    for stage in stages:
        depends = ", ".join(stage.get("dependsOn", [])) or "-"
        lines.append(
            "| {title} | {status} | {percent}% | {depends} | {summary} |".format(
                title=markdown_escape(stage.get("title", "")),
                status=markdown_escape(status_label(stage.get("status", "planned"))),
                percent=markdown_escape(stage.get("percentComplete", 0)),
                depends=markdown_escape(depends),
                summary=markdown_escape(stage.get("summary", "")),
            )
        )
    lines.append("")
    return lines


def render_plan_table(plans: list[dict]) -> list[str]:
    sorted_plans = sorted(
        plans,
        key=lambda plan: (STATUS_ORDER.get(plan.get("status", "planned"), 99), plan.get("area", ""), plan.get("title", "")),
    )
    lines = [
        "## Plan Status",
        "",
        "| Plan | Area | Status | Completion | Source |",
        "| --- | --- | --- | ---: | --- |",
    ]
    for plan in sorted_plans:
        source = plan.get("ownerDoc", "")
        lines.append(
            "| {title} | {area} | {status} | {percent}% | `{source}` |".format(
                title=markdown_escape(plan.get("title", "")),
                area=markdown_escape(plan.get("area", "")),
                status=markdown_escape(status_label(plan.get("status", "planned"))),
                percent=markdown_escape(plan.get("percentComplete", 0)),
                source=markdown_escape(source),
            )
        )
    lines.append("")
    return lines


def render_dependency_diagram(data: dict) -> list[str]:
    lookup = plan_lookup(data)
    nodes: dict[str, str] = {}
    edges: list[tuple[str, str]] = []
    all_items = data.get("roadmapStages", []) + data.get("plans", [])
    for item in all_items:
        item_id = item["id"]
        nodes[item_id] = item.get("title", item_id)
        for dep in item.get("dependsOn", []):
            nodes[dep] = lookup.get(dep, dep)
            edges.append((dep, item_id))

    lines = [
        "## Dependency Map",
        "",
        "```mermaid",
        "flowchart TD",
    ]
    for item_id, title in sorted(nodes.items()):
        lines.append(f'  {mermaid_id(item_id)}["{title}"]')
    for dep, item_id in edges:
        lines.append(f"  {mermaid_id(dep)} --> {mermaid_id(item_id)}")
    lines.extend(["```", ""])
    return lines


def render_completion_chart(plans: list[dict]) -> list[str]:
    lines = [
        "## Completion By Plan",
        "",
        "```mermaid",
        "xychart-beta",
        '  title "Estimated Completion By Plan"',
        '  x-axis "Plan" [',
    ]
    labels = [plan["id"].replace("plan.", "").replace("_", " ") for plan in plans]
    lines[-1] += ", ".join(f'"{label[:18]}"' for label in labels) + "]"
    values = ", ".join(str(int(plan.get("percentComplete", 0))) for plan in plans)
    lines.append('  y-axis "Percent" 0 --> 100')
    lines.append(f"  bar [{values}]")
    lines.extend(["```", ""])
    return lines


def render_plan_details(data: dict) -> list[str]:
    lines = ["## Detailed State", ""]
    lookup = plan_lookup(data)
    for plan in data.get("plans", []):
        depends = [lookup.get(dep, dep) for dep in plan.get("dependsOn", [])]
        lines.extend(
            [
                f"### {plan.get('title', plan['id'])}",
                "",
                f"- Status: **{status_label(plan.get('status', 'planned'))}**",
                f"- Completion: **{plan.get('percentComplete', 0)}%**",
                f"- Area: `{plan.get('area', '')}`",
                f"- Source: `{plan.get('ownerDoc', '')}`",
                f"- Depends on: {', '.join(depends) if depends else '-'}",
                f"- Summary: {plan.get('summary', '')}",
                "",
            ]
        )
        for key, title in (("done", "Done"), ("inProgress", "In Progress"), ("onHold", "On Hold"), ("next", "Next")):
            items = plan.get(key, [])
            lines.append(f"**{title}**")
            if items:
                lines.extend(f"- {item}" for item in items)
            else:
                lines.append("- -")
            lines.append("")
    return lines


def render_dashboard(data: dict) -> str:
    lines: list[str] = []
    lines.extend(render_summary(data))
    lines.extend(render_roadmap(data.get("roadmapStages", [])))
    lines.extend(render_plan_table(data.get("plans", [])))
    lines.extend(render_dependency_diagram(data))
    lines.extend(render_completion_chart(data.get("plans", [])))
    lines.extend(render_plan_details(data))
    return "\n".join(lines).rstrip() + "\n"


def main() -> None:
    data = load_source()
    OUTPUT.write_text(render_dashboard(data), encoding="utf-8")
    print(f"Generated {OUTPUT.relative_to(ROOT)} from {SOURCE.relative_to(ROOT)}")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Read-only APH-502 inventory of tracked Unity TextureImporter metadata."""

from __future__ import annotations

import argparse
import collections
import json
import re
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
CONTENT_RESIDENCY = ROOT / "Design/AgentReports/architecture_performance_content_residency_baseline.json"
BUILD_REPORTS = (
    ROOT / "Design/AgentReports/architecture_performance_android_aab_build_report.json",
    ROOT / "Design/AgentReports/architecture_performance_android_apk_build_report.json",
)
SEMANTIC_CATEGORIES = (
    "UI",
    "world albedo",
    "world normal/mask",
    "VFX",
    "impostor/atlas",
    "generated source/reference",
)
NO_ACCEPTED_EVIDENCE = "excluded/unreferenced"


def tracked_texture_metas() -> list[str]:
    result = subprocess.run(
        ["git", "ls-files", "-z", "--", "*.meta"],
        cwd=ROOT,
        check=True,
        capture_output=True,
    )
    paths = result.stdout.decode("utf-8").split("\0")
    return sorted(
        path
        for path in paths
        if path and (ROOT / path).is_file() and "\nTextureImporter:\n" in (ROOT / path).read_text(encoding="utf-8")
    )


def scalar_int(yaml_text: str, key: str) -> int | None:
    match = re.search(rf"^  {re.escape(key)}: (-?\d+)\s*$", yaml_text, re.MULTILINE)
    return int(match.group(1)) if match else None


def git_output(*args: str) -> str:
    result = subprocess.run(["git", *args], cwd=ROOT, check=True, capture_output=True)
    return result.stdout.decode("utf-8").strip()


def current_revision() -> tuple[str, list[str]]:
    head = git_output("rev-parse", "HEAD")
    result = subprocess.run(
        ["git", "status", "--porcelain=v1", "-z", "--untracked-files=no"],
        cwd=ROOT,
        check=True,
        capture_output=True,
    )
    entries = sorted(entry for entry in result.stdout.decode("utf-8").split("\0") if entry)
    return head, entries


def texture_paths_from_top_assets(report: dict[str, object]) -> set[str]:
    rows = report.get("buildReportIncludedAssets", [])
    if not isinstance(rows, list):
        return set()
    return {
        row["sourceAssetPath"]
        for row in rows
        if isinstance(row, dict)
        and isinstance(row.get("objectTypes"), list)
        and "UnityEngine.Texture2D" in row["objectTypes"]
        and isinstance(row.get("sourceAssetPath"), str)
    }


def complete_texture_path_export(report: dict[str, object]) -> tuple[set[str], bool]:
    if (
        report.get("schemaVersion") != 1
        or report.get("allIncludedTexturePathsExported") is not True
    ):
        return set(), False

    rows = report.get("buildReportIncludedTextures")
    if not isinstance(rows, list):
        return set(), False

    ordered_paths: list[str] = []
    for row in rows:
        if not isinstance(row, dict):
            return set(), False
        source_path = row.get("sourceAssetPath")
        object_types = row.get("objectTypes")
        if (
            not isinstance(source_path, str)
            or not source_path
            or source_path != source_path.strip()
            or "\\" in source_path
            or source_path.startswith("./")
            or "//" in source_path
            or not isinstance(object_types, list)
            or "UnityEngine.Texture2D" not in object_types
        ):
            return set(), False
        ordered_paths.append(source_path)

    unique_paths = set(ordered_paths)
    is_deterministic_complete_export = (
        len(unique_paths) == len(ordered_paths)
        and ordered_paths == sorted(ordered_paths)
    )
    return unique_paths, is_deterministic_complete_export


def evidence_path_label(path: Path) -> str:
    try:
        return path.relative_to(ROOT).as_posix()
    except ValueError:
        return path.as_posix()


def revision_checked_evidence(
    head: str,
    tracked_worktree_dirty: bool,
    content_residency_path: Path = CONTENT_RESIDENCY,
    build_report_paths: tuple[Path, ...] = BUILD_REPORTS,
) -> tuple[set[str], dict[str, object], bool]:
    current_included: set[str] = set()
    details: dict[str, object] = {"contentResidency": {}, "buildReports": []}

    content = json.loads(content_residency_path.read_text(encoding="utf-8"))
    content_paths = {
        row["assetPath"]
        for row in content.get("assets", [])
        if row.get("assetType") == "Texture2D" and isinstance(row.get("assetPath"), str)
    }
    content_revision = content.get("baselineCommit")
    content_current = (
        content.get("status") == "complete"
        and content_revision == head
        and not tracked_worktree_dirty
    )
    if content_current:
        current_included.update(content_paths)
    details["contentResidency"] = {
        "status": content.get("status"),
        "evidenceRevision": content_revision,
        "textureRows": len(content_paths),
        "revisionMatchesCurrent": content_revision == head,
        "acceptedForCurrentRevision": content_current,
        "disposition": "current accepted inclusion evidence" if content_current else "historical only",
    }

    complete_current_build_report_exists = False
    for report_path in build_report_paths:
        report = json.loads(report_path.read_text(encoding="utf-8"))
        report_revision = report.get("exactCommit")
        accepted = (
            report.get("schemaVersion") == 1
            and report.get("taskId") == "APH-500"
            and report.get("status") == "complete"
            and report.get("dirty") is False
            and report.get("releaseBuildType") == "release"
            and report.get("buildTarget") == "Android"
            and report.get("detailedBuildReport") is True
            and report_revision == head
            and not tracked_worktree_dirty
        )
        complete_paths, complete_export_valid = complete_texture_path_export(report)
        if accepted and complete_export_valid:
            complete_current_build_report_exists = True
        top_table_paths = texture_paths_from_top_assets(report)
        report_paths = complete_paths if complete_export_valid else top_table_paths
        if accepted:
            current_included.update(report_paths)
        details["buildReports"].append(
            {
                "path": evidence_path_label(report_path),
                "status": report.get("status"),
                "dirty": report.get("dirty"),
                "evidenceRevision": report_revision,
                "reportedTextureRows": len(report_paths),
                "topTableTextureRows": len(top_table_paths),
                "completeTextureRows": len(complete_paths),
                "revisionMatchesCurrent": report_revision == head,
                "acceptedForCurrentRevision": accepted,
                "completeTexturePathExportMarker": report.get("allIncludedTexturePathsExported") is True,
                "completeTexturePathExport": complete_export_valid,
                "disposition": "current accepted inclusion evidence" if accepted else "historical/rejected only",
            }
        )
    final_buckets_accepted = (
        details["contentResidency"]["acceptedForCurrentRevision"]
        and complete_current_build_report_exists
    )
    return current_included, details, final_buckets_accepted


def semantic_candidates(asset_path: str, yaml_text: str) -> set[str]:
    lower = asset_path.lower()
    stem = Path(lower).stem
    tokens = set(filter(None, re.split(r"[^a-z0-9]+", stem)))
    candidates: set[str] = set()

    if is_generated_source_or_reference(asset_path):
        candidates.add("generated source/reference")
    if "impostor" in lower or "atlas" in tokens or "/atlases/" in lower:
        candidates.add("impostor/atlas")
    if (
        "/effects/" in lower
        or "/fx/" in lower
        or "/vfx/" in lower
        or tokens.intersection({"vfx", "particle", "particles", "muzzleflash", "smoke", "glow"})
    ):
        candidates.add("VFX")
    if scalar_int(yaml_text, "textureType") == 1 or tokens.intersection(
        {"normal", "normals", "mask", "masks", "metallic", "roughness", "occlusion", "specular"}
    ):
        candidates.add("world normal/mask")
    if (
        scalar_int(yaml_text, "spriteMode") not in (None, 0)
        or scalar_int(yaml_text, "textureType") == 8
        or any(marker in lower for marker in ("/ui/", "/gui/", "/interface", "/fonts/"))
    ):
        candidates.add("UI")
    if not candidates:
        candidates.add("world albedo")
    return candidates


def is_generated_source_or_reference(asset_path: str) -> bool:
    lower = asset_path.lower()
    if "/generated/" not in lower:
        return False
    return bool(
        re.search(r"/(references?|sources?)/", lower)
        or re.search(r"(?:^|[_-])(reference|source)(?:[_-]|\.)", Path(lower).name)
    )


def choose_semantic_category(candidates: set[str]) -> str:
    precedence = ("generated source/reference", "impostor/atlas", "VFX", "UI", "world normal/mask", "world albedo")
    for category in precedence:
        if category in candidates:
            return category
    raise ValueError(f"No chosen semantic category for candidates: {sorted(candidates)}")


def inventory() -> dict[str, object]:
    meta_paths = tracked_texture_metas()
    head, tracked_worktree_changes = current_revision()
    evidence_paths, evidence, current_unity_evidence_exists = revision_checked_evidence(
        head, bool(tracked_worktree_changes)
    )
    rows = []
    for meta_path in meta_paths:
        asset_path = meta_path[:-5]
        yaml_text = (ROOT / meta_path).read_text(encoding="utf-8")
        candidates = semantic_candidates(asset_path, yaml_text)
        category = choose_semantic_category(candidates)
        has_current_inclusion = asset_path in evidence_paths
        rows.append(
            {
                "assetPath": asset_path,
                "metaPath": meta_path,
                "chosenSemanticCategory": category,
                "semanticCandidates": sorted(candidates),
                "semanticAmbiguity": len(candidates) > 1,
                "evidenceStatus": "accepted current inclusion" if has_current_inclusion else NO_ACCEPTED_EVIDENCE,
                "evidenceMeaning": (
                    "accepted positive inclusion evidence at the analyzed revision"
                    if has_current_inclusion
                    else "no accepted inclusion evidence; exclusion and reference state are not proven"
                ),
                "spriteMode": scalar_int(yaml_text, "spriteMode"),
                "textureType": scalar_int(yaml_text, "textureType"),
            }
        )

    semantic_counts = collections.Counter(row["chosenSemanticCategory"] for row in rows)
    evidence_counts = collections.Counter(row["evidenceStatus"] for row in rows)
    ambiguous = [row for row in rows if row["semanticAmbiguity"]]
    unclassified = [row for row in rows if row["chosenSemanticCategory"] not in SEMANTIC_CATEGORIES]
    matched_evidence = {row["assetPath"] for row in rows if row["evidenceStatus"] == "accepted current inclusion"}
    return {
        "taskId": "APH-502",
        "status": "complete" if current_unity_evidence_exists else "incomplete",
        "finalBucketsAccepted": current_unity_evidence_exists,
        "finalBucketsAcceptanceRequirement": (
            "current-revision content residency plus a complete all-texture BuildReport export "
            "from a fully clean tracked worktree"
        ),
        "analyzedRevision": head,
        "trackedWorktreeClean": not tracked_worktree_changes,
        "trackedWorktreeChanges": tracked_worktree_changes,
        "trackedTextureImporterCount": len(rows),
        "chosenSemanticCounts": {category: semantic_counts[category] for category in SEMANTIC_CATEGORIES},
        "evidenceStatusCounts": {
            "accepted current inclusion": evidence_counts["accepted current inclusion"],
            NO_ACCEPTED_EVIDENCE: evidence_counts[NO_ACCEPTED_EVIDENCE],
        },
        "evidence": evidence,
        "acceptedCurrentEvidenceTexturePathCount": len(evidence_paths),
        "acceptedCurrentEvidenceMatchedImporterCount": len(matched_evidence),
        "acceptedCurrentEvidenceWithoutTrackedImporter": sorted(evidence_paths - matched_evidence),
        "ambiguityCount": len(ambiguous),
        "ambiguities": ambiguous,
        "unclassifiedCount": len(unclassified),
        "unclassified": unclassified,
        "rows": rows,
    }


def print_markdown(data: dict[str, object]) -> None:
    print(f"Status: {data['status']}")
    print(f"Final buckets accepted: {str(data['finalBucketsAccepted']).lower()}")
    print()
    print("| Chosen current semantic category | Count |")
    print("|---|---:|")
    for category, count in data["chosenSemanticCounts"].items():
        print(f"| {category} | {count:,} |")
    print(f"| **Total** | **{data['trackedTextureImporterCount']:,}** |")
    print()
    print("| Current-revision evidence status | Count |")
    print("|---|---:|")
    for status, count in data["evidenceStatusCounts"].items():
        print(f"| {status} | {count:,} |")
    print()
    print(f"Ambiguities: {data['ambiguityCount']}; unclassified: {data['unclassifiedCount']}")
    for row in data["ambiguities"]:
        print(
            f"- `{row['assetPath']}`: {', '.join(row['semanticCandidates'])} "
            f"-> {row['chosenSemanticCategory']} [{row['evidenceStatus']}]"
        )
    for row in data["unclassified"]:
        print(f"- UNCLASSIFIED `{row['assetPath']}`")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--json", action="store_true", help="emit complete machine-readable inventory")
    args = parser.parse_args()
    data = inventory()
    if args.json:
        print(json.dumps(data, indent=2, sort_keys=False))
    else:
        print_markdown(data)


if __name__ == "__main__":
    main()

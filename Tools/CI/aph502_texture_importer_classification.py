#!/usr/bin/env python3
"""Deterministic, read-only APH-502 TextureImporter classification inventory."""

from __future__ import annotations

import argparse
import collections
import json
import re
import subprocess
from pathlib import Path
from typing import Iterable


ROOT = Path(__file__).resolve().parents[2]
TASK_ID = "APH-502"
CONTENT_RESIDENCY_PATH = Path(
    "Design/AgentReports/architecture_performance_content_residency_baseline.json"
)
BUILD_REPORT_PATHS = (
    Path("Design/AgentReports/architecture_performance_android_aab_build_report.json"),
    Path("Design/AgentReports/architecture_performance_android_apk_build_report.json"),
)
JSON_REPORT_PATH = Path(
    "Design/AgentReports/2026-07-10_aph-502_texture_importer_classification.json"
)
MARKDOWN_REPORT_PATH = Path(
    "Design/AgentReports/2026-07-10_aph-502_texture_importer_classification.md"
)
GENERATED_REPORT_PATHS = (JSON_REPORT_PATH, MARKDOWN_REPORT_PATH)
SEMANTIC_CATEGORIES = (
    "UI",
    "world albedo",
    "world normal/mask",
    "VFX",
    "impostor/atlas",
    "generated source/reference",
)
NO_ACCEPTED_EVIDENCE = "excluded/unreferenced"


class DuplicateJsonKeyError(ValueError):
    pass


def tracked_texture_metas(root: Path = ROOT) -> list[str]:
    result = subprocess.run(
        ["git", "ls-files", "-z", "--", "*.meta"],
        cwd=root,
        check=True,
        capture_output=True,
    )
    paths = result.stdout.decode("utf-8").split("\0")
    return sorted(
        path
        for path in paths
        if path
        and (root / path).is_file()
        and "\nTextureImporter:\n" in (root / path).read_text(encoding="utf-8")
    )


def scalar_int(yaml_text: str, key: str) -> int | None:
    match = re.search(rf"^  {re.escape(key)}: (-?\d+)\s*$", yaml_text, re.MULTILINE)
    return int(match.group(1)) if match else None


def git_output(root: Path, *args: str) -> str:
    result = subprocess.run(["git", *args], cwd=root, check=True, capture_output=True)
    return result.stdout.decode("utf-8").strip()


def current_revision(
    root: Path = ROOT,
    ignored_paths: Iterable[Path] = GENERATED_REPORT_PATHS,
) -> tuple[str, list[str]]:
    head = git_output(root, "rev-parse", "HEAD")
    pathspecs = [".", *(f":(exclude){path.as_posix()}" for path in ignored_paths)]
    result = subprocess.run(
        ["git", "status", "--porcelain=v1", "-z", "--untracked-files=no", "--", *pathspecs],
        cwd=root,
        check=True,
        capture_output=True,
    )
    entries = sorted(entry for entry in result.stdout.decode("utf-8").split("\0") if entry)
    return head, entries


def _reject_duplicate_json_keys(pairs: list[tuple[str, object]]) -> dict[str, object]:
    result: dict[str, object] = {}
    for key, value in pairs:
        if key in result:
            raise DuplicateJsonKeyError(key)
        result[key] = value
    return result


def read_json_object(path: Path) -> tuple[dict[str, object] | None, list[str]]:
    try:
        value = json.loads(
            path.read_text(encoding="utf-8"),
            object_pairs_hook=_reject_duplicate_json_keys,
        )
    except FileNotFoundError:
        return None, ["file-missing"]
    except OSError as error:
        return None, [f"file-unreadable:{type(error).__name__}"]
    except DuplicateJsonKeyError as error:
        return None, [f"duplicate-json-key:{error}"]
    except json.JSONDecodeError as error:
        return None, [f"json-invalid:{error.msg}"]
    if not isinstance(value, dict):
        return None, ["root-not-object"]
    return value, []


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


def complete_texture_path_export(
    report: dict[str, object],
) -> tuple[set[str], bool, list[str]]:
    errors: list[str] = []
    if report.get("schemaVersion") != 1:
        errors.append("schema-version-not-1")
    if report.get("allIncludedTexturePathsExported") is not True:
        errors.append("complete-texture-export-marker-not-true")

    rows = report.get("buildReportIncludedTextures")
    if not isinstance(rows, list):
        errors.append("complete-texture-export-not-array")
        return set(), False, errors

    ordered_paths: list[str] = []
    for index, row in enumerate(rows):
        if not isinstance(row, dict):
            errors.append(f"complete-texture-row-not-object:{index}")
            continue
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
            errors.append(f"complete-texture-row-invalid:{index}")
            continue
        ordered_paths.append(source_path)

    unique_paths = set(ordered_paths)
    if len(unique_paths) != len(ordered_paths):
        errors.append("complete-texture-export-duplicates")
    if ordered_paths != sorted(ordered_paths):
        errors.append("complete-texture-export-not-sorted")
    return unique_paths, not errors, errors


def evidence_path_label(path: Path, root: Path = ROOT) -> str:
    try:
        return path.relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()


def content_residency_texture_paths(
    content: dict[str, object],
) -> tuple[set[str], bool, list[str]]:
    errors: list[str] = []
    rows = content.get("assets")
    if not isinstance(rows, list):
        return set(), False, ["assets-not-array"]

    ordered_paths: list[str] = []
    for index, row in enumerate(rows):
        if not isinstance(row, dict):
            errors.append(f"asset-row-not-object:{index}")
            continue
        if row.get("assetType") != "Texture2D":
            continue
        asset_path = row.get("assetPath")
        if (
            not isinstance(asset_path, str)
            or not asset_path
            or asset_path != asset_path.strip()
            or "\\" in asset_path
            or asset_path.startswith("./")
            or "//" in asset_path
        ):
            errors.append(f"texture-asset-path-invalid:{index}")
            continue
        ordered_paths.append(asset_path)

    unique_paths = set(ordered_paths)
    if len(unique_paths) != len(ordered_paths):
        errors.append("texture-asset-paths-duplicate")
    if ordered_paths != sorted(ordered_paths):
        errors.append("texture-asset-paths-not-sorted")

    summary = content.get("summary")
    summary_count = summary.get("textureAssetCount") if isinstance(summary, dict) else None
    if not isinstance(summary_count, int):
        errors.append("summary-texture-asset-count-missing")
    elif summary_count != len(unique_paths):
        errors.append(
            f"summary-texture-asset-count-mismatch:{summary_count}!={len(unique_paths)}"
        )
    return unique_paths, not errors, errors


def revision_checked_evidence(
    head: str,
    tracked_worktree_dirty: bool,
    content_residency_path: Path | None = None,
    build_report_paths: tuple[Path, ...] | None = None,
    root: Path = ROOT,
) -> tuple[set[str], dict[str, object], bool]:
    content_residency_path = content_residency_path or root / CONTENT_RESIDENCY_PATH
    build_report_paths = build_report_paths or tuple(root / path for path in BUILD_REPORT_PATHS)
    details: dict[str, object] = {"contentResidency": {}, "buildReports": []}

    content, content_read_errors = read_json_object(content_residency_path)
    content = content or {}
    content_paths, content_complete, content_validation_errors = content_residency_texture_paths(content)
    content_revision = content.get("baselineCommit")
    content_provenance_current = (
        not content_read_errors
        and content.get("status") == "complete"
        and content_revision == head
        and not tracked_worktree_dirty
    )
    content_accepted = content_provenance_current and content_complete
    content_errors = [*content_read_errors, *content_validation_errors]
    if content.get("status") != "complete":
        content_errors.append("status-not-complete")
    if content_revision != head:
        content_errors.append(f"revision-mismatch:{content_revision}->{head}")
    if tracked_worktree_dirty:
        content_errors.append("tracked-worktree-dirty")
    details["contentResidency"] = {
        "path": evidence_path_label(content_residency_path, root),
        "status": content.get("status"),
        "evidenceRevision": content_revision,
        "textureRows": len(content_paths),
        "summaryTextureRows": (
            content.get("summary", {}).get("textureAssetCount")
            if isinstance(content.get("summary"), dict)
            else None
        ),
        "revisionMatchesCurrent": content_revision == head,
        "completeInventory": content_complete,
        "acceptedForCurrentRevision": content_accepted,
        "validationErrors": sorted(set(content_errors)),
        "disposition": (
            "current accepted complete residency evidence"
            if content_accepted
            else "historical/incomplete/rejected only"
        ),
    }

    accepted_build_paths: set[str] = set()
    accepted_complete_build_report_exists = False
    for report_path in build_report_paths:
        report, report_read_errors = read_json_object(report_path)
        report = report or {}
        report_revision = report.get("exactCommit")
        provenance_current = (
            not report_read_errors
            and report.get("taskId") == "APH-500"
            and report.get("status") == "complete"
            and report.get("dirty") is False
            and report.get("releaseBuildType") == "release"
            and report.get("buildTarget") == "Android"
            and report.get("detailedBuildReport") is True
            and report_revision == head
            and not tracked_worktree_dirty
        )
        complete_paths, complete_export_valid, export_errors = complete_texture_path_export(report)
        accepted = provenance_current and complete_export_valid
        if accepted:
            accepted_complete_build_report_exists = True
            accepted_build_paths.update(complete_paths)
        top_table_paths = texture_paths_from_top_assets(report)
        report_errors = [*report_read_errors, *export_errors]
        expected_fields = (
            ("schema-version-not-1", report.get("schemaVersion") == 1),
            ("task-id-not-APH-500", report.get("taskId") == "APH-500"),
            ("status-not-complete", report.get("status") == "complete"),
            ("dirty-provenance-not-false", report.get("dirty") is False),
            ("release-build-type-invalid", report.get("releaseBuildType") == "release"),
            ("build-target-not-Android", report.get("buildTarget") == "Android"),
            ("detailed-build-report-not-true", report.get("detailedBuildReport") is True),
            (f"revision-mismatch:{report_revision}->{head}", report_revision == head),
            ("tracked-worktree-dirty", not tracked_worktree_dirty),
        )
        report_errors.extend(code for code, valid in expected_fields if not valid)
        details["buildReports"].append(
            {
                "path": evidence_path_label(report_path, root),
                "status": report.get("status"),
                "dirty": report.get("dirty"),
                "evidenceRevision": report_revision,
                "reportedTextureRows": len(complete_paths),
                "topTableTextureRows": len(top_table_paths),
                "completeTextureRows": len(complete_paths),
                "revisionMatchesCurrent": report_revision == head,
                "acceptedForCurrentRevision": accepted,
                "completeTexturePathExportMarker": report.get("allIncludedTexturePathsExported") is True,
                "completeTexturePathExport": complete_export_valid,
                "validationErrors": sorted(set(report_errors)),
                "disposition": (
                    "current accepted complete Android BuildReport evidence"
                    if accepted
                    else "historical/incomplete/rejected only"
                ),
            }
        )
    final_buckets_accepted = (
        content_accepted and accepted_complete_build_report_exists
    )
    accepted_paths = content_paths | accepted_build_paths if final_buckets_accepted else set()
    return accepted_paths, details, final_buckets_accepted


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


def classify_rows(
    root: Path,
    meta_paths: Iterable[str],
    accepted_evidence_paths: set[str],
) -> list[dict[str, object]]:
    rows: list[dict[str, object]] = []
    for meta_path in sorted(meta_paths):
        asset_path = meta_path[:-5]
        yaml_text = (root / meta_path).read_text(encoding="utf-8")
        candidates = semantic_candidates(asset_path, yaml_text)
        category = choose_semantic_category(candidates)
        has_current_inclusion = asset_path in accepted_evidence_paths
        rows.append(
            {
                "assetPath": asset_path,
                "metaPath": meta_path,
                "chosenSemanticCategory": category,
                "semanticCandidates": sorted(candidates),
                "semanticAmbiguity": len(candidates) > 1,
                "evidenceStatus": (
                    "accepted current inclusion" if has_current_inclusion else NO_ACCEPTED_EVIDENCE
                ),
                "evidenceMeaning": (
                    "accepted positive inclusion evidence at the analyzed revision"
                    if has_current_inclusion
                    else "no accepted inclusion evidence; exclusion and reference state are not proven"
                ),
                "spriteMode": scalar_int(yaml_text, "spriteMode"),
                "textureType": scalar_int(yaml_text, "textureType"),
            }
        )
    return rows


def summarize_rows(rows: list[dict[str, object]]) -> dict[str, object]:
    semantic_counts = collections.Counter(row["chosenSemanticCategory"] for row in rows)
    candidate_counts: collections.Counter[str] = collections.Counter()
    overlap_counts: collections.Counter[str] = collections.Counter()
    evidence_counts = collections.Counter(row["evidenceStatus"] for row in rows)
    for row in rows:
        candidates = tuple(row["semanticCandidates"])
        candidate_counts.update(candidates)
        if len(candidates) > 1:
            overlap_counts[" + ".join(candidates)] += 1

    ambiguous = [row for row in rows if row["semanticAmbiguity"]]
    unclassified = [row for row in rows if row["chosenSemanticCategory"] not in SEMANTIC_CATEGORIES]
    matched_evidence = {
        row["assetPath"] for row in rows if row["evidenceStatus"] == "accepted current inclusion"
    }
    return {
        "trackedTextureImporterCount": len(rows),
        "chosenSemanticCounts": {
            category: semantic_counts[category] for category in SEMANTIC_CATEGORIES
        },
        "semanticCandidateCounts": {
            category: candidate_counts[category] for category in SEMANTIC_CATEGORIES
        },
        "semanticOverlapCounts": dict(sorted(overlap_counts.items())),
        "evidenceStatusCounts": {
            "accepted current inclusion": evidence_counts["accepted current inclusion"],
            NO_ACCEPTED_EVIDENCE: evidence_counts[NO_ACCEPTED_EVIDENCE],
        },
        "acceptedCurrentEvidenceMatchedImporterCount": len(matched_evidence),
        "ambiguityCount": len(ambiguous),
        "ambiguities": ambiguous,
        "unclassifiedCount": len(unclassified),
        "unclassified": unclassified,
    }


def acceptance_blockers(
    tracked_worktree_changes: list[str],
    evidence: dict[str, object],
) -> list[str]:
    blockers: list[str] = []
    if tracked_worktree_changes:
        blockers.append(
            f"tracked-worktree-dirty:{len(tracked_worktree_changes)}-tracked-change-records"
        )
    content = evidence["contentResidency"]
    if not content["acceptedForCurrentRevision"]:
        blockers.extend(
            f"content-residency:{error}" for error in content["validationErrors"]
        )
    accepted_build_reports = [
        report for report in evidence["buildReports"] if report["acceptedForCurrentRevision"]
    ]
    if not accepted_build_reports:
        blockers.append("android-build-report:no-current-complete-texture-export")
        for report in evidence["buildReports"]:
            blockers.extend(
                f"android-build-report:{Path(report['path']).name}:{error}"
                for error in report["validationErrors"]
            )
    return sorted(set(blockers))


def inventory(root: Path = ROOT) -> dict[str, object]:
    meta_paths = tracked_texture_metas(root)
    head, tracked_worktree_changes = current_revision(root)
    evidence_paths, evidence, current_unity_evidence_exists = revision_checked_evidence(
        head,
        bool(tracked_worktree_changes),
        root=root,
    )
    rows = classify_rows(root, meta_paths, evidence_paths)
    summary = summarize_rows(rows)
    data = {
        "taskId": TASK_ID,
        "status": "complete" if current_unity_evidence_exists else "incomplete",
        "finalBucketsAccepted": current_unity_evidence_exists,
        "finalBucketsAcceptanceRequirement": (
            "current-revision content residency plus a complete all-texture BuildReport export "
            "from a fully clean tracked worktree"
        ),
        "analyzedRevision": head,
        "trackedWorktreeClean": not tracked_worktree_changes,
        "trackedWorktreeChanges": tracked_worktree_changes,
        "evidence": evidence,
        "acceptedCurrentEvidenceTexturePathCount": len(evidence_paths),
        **summary,
        "rows": rows,
    }
    matched_evidence = {
        row["assetPath"] for row in rows if row["evidenceStatus"] == "accepted current inclusion"
    }
    data["acceptedCurrentEvidenceWithoutTrackedImporter"] = sorted(evidence_paths - matched_evidence)
    data["acceptanceBlockers"] = acceptance_blockers(tracked_worktree_changes, evidence)
    return data


def generated_report_document(data: dict[str, object]) -> dict[str, object]:
    return {key: value for key, value in data.items() if key != "rows"}


def render_json(data: dict[str, object], include_rows: bool = False) -> str:
    document = data if include_rows else generated_report_document(data)
    return json.dumps(document, indent=2, sort_keys=False) + "\n"


def _display_category(category: str) -> str:
    return category if category == "VFX" else category[:1].upper() + category[1:]


def render_markdown(data: dict[str, object]) -> str:
    lines = [
        "# APH-502 Texture Importer Classification Inventory",
        "",
        f"- Task: `{data['taskId']}`",
        f"- Status: `{data['status']}`",
        f"- Final buckets accepted: `{str(data['finalBucketsAccepted']).lower()}`",
        f"- Analyzed revision: `{data['analyzedRevision']}`",
        f"- Tracked worktree clean: `{str(data['trackedWorktreeClean']).lower()}`",
        "- Scope: Git-tracked `.meta` files whose current YAML contains `TextureImporter:`",
        "- Import settings changed: none",
        "- Unity run: none",
        "",
        "## Current Result",
        "",
        f"The current tracked input set contains **{data['trackedTextureImporterCount']:,}** texture importers. "
        "All category, candidate, overlap, ambiguity, and evidence counts below are generated from the current "
        "tracked importer metadata and evidence inputs; no inventory total is hard-coded.",
        "",
        "The semantic classification is current, but the final inclusion/exclusion buckets remain unaccepted. "
        "`excluded/unreferenced` means only that no clean same-revision complete evidence pair was accepted; it "
        "does not prove that an asset is unused, unreachable, or safe to remove.",
        "",
        "### Mutually Exclusive Chosen Semantic Categories",
        "",
        "| Chosen category | Count |",
        "|---|---:|",
    ]
    for category, count in data["chosenSemanticCounts"].items():
        lines.append(f"| {_display_category(category)} | {count:,} |")
    lines.extend(
        [
            f"| **Total** | **{data['trackedTextureImporterCount']:,}** |",
            "",
            "### Overlapping Semantic Candidates",
            "",
            "| Candidate | Membership count |",
            "|---|---:|",
        ]
    )
    for category, count in data["semanticCandidateCounts"].items():
        lines.append(f"| {_display_category(category)} | {count:,} |")
    lines.extend(
        [
            "",
            f"Ambiguous importers: **{data['ambiguityCount']:,}**. Unclassified importers: "
            f"**{data['unclassifiedCount']:,}**.",
            "",
            "| Exact candidate overlap | Count |",
            "|---|---:|",
        ]
    )
    for overlap, count in data["semanticOverlapCounts"].items():
        lines.append(f"| {overlap} | {count:,} |")
    if not data["semanticOverlapCounts"]:
        lines.append("| None | 0 |")

    lines.extend(
        [
            "",
            "### Current Inclusion Evidence Status",
            "",
            "| Evidence status | Count |",
            "|---|---:|",
        ]
    )
    for status, count in data["evidenceStatusCounts"].items():
        lines.append(f"| {status} | {count:,} |")
    lines.extend(
        [
            "",
            "## Evidence Gate",
            "",
            "Acceptance requires both a clean same-revision complete Unity content-residency inventory and at "
            "least one clean same-revision detailed Android BuildReport with a deterministic complete "
            "`buildReportIncludedTextures` export. Until both exist, the analyzer accepts zero inclusion paths "
            "and zero exclusion claims.",
            "",
            "### Content Residency",
            "",
        ]
    )
    content = data["evidence"]["contentResidency"]
    lines.extend(
        [
            f"- Path: `{content['path']}`",
            f"- Evidence revision: `{content['evidenceRevision']}`",
            f"- Texture rows: `{content['textureRows']}`",
            f"- Summary texture rows: `{content['summaryTextureRows']}`",
            f"- Complete inventory: `{str(content['completeInventory']).lower()}`",
            f"- Accepted for current revision: `{str(content['acceptedForCurrentRevision']).lower()}`",
            f"- Disposition: {content['disposition']}",
            f"- Validation errors: `{', '.join(content['validationErrors']) or 'none'}`",
            "",
            "### Android BuildReports",
            "",
            "| Path | Revision | Complete texture rows | Complete export | Accepted |",
            "|---|---|---:|---|---|",
        ]
    )
    for report in data["evidence"]["buildReports"]:
        lines.append(
            f"| `{report['path']}` | `{report['evidenceRevision']}` | {report['completeTextureRows']:,} | "
            f"`{str(report['completeTexturePathExport']).lower()}` | "
            f"`{str(report['acceptedForCurrentRevision']).lower()}` |"
        )
    lines.extend(["", "### Remaining Blockers", ""])
    lines.extend(f"- `{blocker}`" for blocker in data["acceptanceBlockers"])
    if not data["acceptanceBlockers"]:
        lines.append("- None")

    lines.extend(
        [
            "",
            "## Semantic Rules",
            "",
            "Candidate rules are case-insensitive and are applied to every importer before precedence:",
            "",
            "- Generated source/reference: `/Generated/` plus a reference/source segment or filename token.",
            "- Impostor/atlas: `impostor` path text, an `atlas` filename token, or `/Atlases/`.",
            "- VFX: effects/FX/VFX paths or VFX, particle, muzzle-flash, smoke, or glow tokens.",
            "- World normal/mask: `textureType: 1` or normal/mask/material-channel filename tokens.",
            "- UI: sprite import, `textureType: 8`, or UI/GUI/Interface/Fonts paths.",
            "- World albedo: fallback only when no other candidate applies.",
            "",
            "Chosen precedence is generated source/reference, impostor/atlas, VFX, UI, world normal/mask, then "
            "world albedo. Exact ambiguous paths remain available in the generated JSON report.",
            "",
            "## Reproduction",
            "",
            "```sh",
            "PYTHONPYCACHEPREFIX=/tmp/aph502-pyc python3 -m unittest \\",
            "  Tools.CI.tests.test_aph502_texture_importer_classification -v",
            "PYTHONPYCACHEPREFIX=/tmp/aph502-pyc python3 \\",
            "  Tools/CI/aph502_texture_importer_classification.py --write",
            "PYTHONPYCACHEPREFIX=/tmp/aph502-pyc python3 \\",
            "  Tools/CI/aph502_texture_importer_classification.py --check",
            "```",
            "",
            "No importer or asset mutation is authorized by this report.",
            "",
        ]
    )
    return "\n".join(lines)


def validate_inventory(data: dict[str, object]) -> list[str]:
    errors: list[str] = []
    total = data["trackedTextureImporterCount"]
    if sum(data["chosenSemanticCounts"].values()) != total:
        errors.append("chosen-semantic-counts-do-not-close")
    if sum(data["evidenceStatusCounts"].values()) != total:
        errors.append("evidence-status-counts-do-not-close")
    if sum(data["semanticOverlapCounts"].values()) != data["ambiguityCount"]:
        errors.append("overlap-counts-do-not-close")
    if data["unclassifiedCount"]:
        errors.append(f"unclassified-importers:{data['unclassifiedCount']}")
    if data["finalBucketsAccepted"] and data["acceptanceBlockers"]:
        errors.append("accepted-inventory-has-blockers")
    if not data["finalBucketsAccepted"] and data["acceptedCurrentEvidenceTexturePathCount"]:
        errors.append("incomplete-inventory-accepted-evidence-paths")
    return errors


def write_reports(root: Path, data: dict[str, object]) -> None:
    (root / JSON_REPORT_PATH).write_text(render_json(data), encoding="utf-8")
    (root / MARKDOWN_REPORT_PATH).write_text(render_markdown(data), encoding="utf-8")


def report_check_errors(root: Path, data: dict[str, object]) -> list[str]:
    expected = {
        JSON_REPORT_PATH: render_json(data),
        MARKDOWN_REPORT_PATH: render_markdown(data),
    }
    errors = validate_inventory(data)
    for path, content in expected.items():
        absolute_path = root / path
        try:
            actual = absolute_path.read_text(encoding="utf-8")
        except FileNotFoundError:
            errors.append(f"generated-report-missing:{path.as_posix()}")
            continue
        if actual != content:
            errors.append(f"generated-report-stale:{path.as_posix()}")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=ROOT, help="repository root")
    parser.add_argument("--json", action="store_true", help="emit complete row-level inventory JSON")
    parser.add_argument("--write", action="store_true", help="write the APH-502 JSON and Markdown reports")
    parser.add_argument("--check", action="store_true", help="validate inventory closure and report freshness")
    args = parser.parse_args()
    root = args.root.resolve()
    data = inventory(root)
    if args.write:
        write_reports(root, data)
    if args.json:
        print(render_json(data, include_rows=True), end="")
    elif args.check:
        errors = report_check_errors(root, data)
        if errors:
            print("APH-502 texture importer classification check failed:")
            for error in errors:
                print(f"- {error}")
            return 1
        print(
            "APH-502 texture importer classification check passed: "
            f"{data['trackedTextureImporterCount']} importers, "
            f"{data['ambiguityCount']} overlaps, final acceptance "
            f"{str(data['finalBucketsAccepted']).lower()}."
        )
    elif not args.write:
        print(render_markdown(data), end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

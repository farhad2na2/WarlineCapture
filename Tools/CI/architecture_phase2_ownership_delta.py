#!/usr/bin/env python3
"""Build the deterministic AM-025 Phase 2 ownership delta."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
from typing import Any, Iterable


SCHEMA_VERSION = 2
ARTIFACT_ID = "AM-025-OWNERSHIP-DELTA"
CLASSIFICATIONS = ("resolved", "protected-deferred", "open")
REVIEW_DECISIONS = ("resolved", "protected-deferred", "genuine-debt")

BASELINE_FINAL_CATEGORIES = {
    "nativeContainers": ("persistentNativeContainers", ("path", "ownerType", "field")),
    "queryCaches": ("persistentQueries", ("path", "ownerType", "field")),
    "subscriptions": ("eventSubscriptions", ("path", "ownerType", "target", "handler")),
    "sceneRoots": ("presentationRoots", ("path", "ownerType", "<component-root>")),
}
FINAL_CATEGORY_KEYS = {
    "persistentNativeContainers": ("path", "ownerType", "field"),
    "persistentQueries": ("path", "ownerType", "field"),
    "eventSubscriptions": ("path", "ownerType", "target", "handler"),
    "presentationRoots": ("path", "ownerType", "root"),
}
BASELINE_CATEGORY_KEYS = {
    "nativeContainers": ("path", "ownerType", "field"),
    "presentationPools": ("path", "ownerType", "field"),
    "queryCaches": ("path", "ownerType", "field"),
    "sceneRoots": ("path", "ownerType", "<component-root>"),
    "staticCaches": ("path", "ownerType", "field"),
    "subscriptions": ("path", "ownerType", "target", "handler"),
    "worlds": ("path", "ownerType", "<world-owner>"),
}
HAZARD_FIELD_CATEGORIES = frozenset({"hiddenSingletons", "mutableStaticCaches", "staticEventSubscriptions"})
HAZARD_ACCESS_CATEGORIES = frozenset({"globalWorldLookups", "runtimeObjectDiscovery"})
RESOLVED_HAZARD_DISPOSITIONS = frozenset({"AD", "CE", "ETO", "IB", "PE"})
OPEN_HAZARD_DISPOSITIONS = frozenset({"CLR", "EIP", "ESU", "HSL", "IRC", "MSL", "ROD"})


class DeltaError(ValueError):
    """Raised when an input cannot support an unambiguous classification."""


def normalized_path(value: Any) -> str:
    if not isinstance(value, str) or not value.strip():
        raise DeltaError("path must be a non-empty string")
    raw = value.strip().replace("\\", "/")
    if raw.startswith("/"):
        raise DeltaError(f"absolute paths are not allowed: {value!r}")
    parts = [part for part in raw.split("/") if part not in ("", ".")]
    if not parts or ".." in parts:
        raise DeltaError(f"path escapes the report root: {value!r}")
    return PurePosixPath(*parts).as_posix()


def required_text(row: dict[str, Any], field: str, context: str) -> str:
    value = row.get(field)
    if not isinstance(value, str) or not value.strip():
        raise DeltaError(f"{context} is missing required field {field!r}")
    return value.strip()


def key_part(row: dict[str, Any], field: str, context: str) -> str:
    if field.startswith("<"):
        return field
    value = required_text(row, field, context)
    return normalized_path(value) if field == "path" else value


def make_key(row: dict[str, Any], fields: Iterable[str], context: str) -> str:
    return "\0".join(key_part(row, field, context) for field in fields)


def stable_identity(row: dict[str, Any], fields: Iterable[str], context: str) -> dict[str, str]:
    return {
        field: key_part(row, field, context)
        for field in fields
        if not field.startswith("<")
    }


def hazard_key(category: str, row: dict[str, Any], context: str) -> str:
    common = ("path", "ownerType", "memberName", "symbol")
    if category in HAZARD_FIELD_CATEGORIES:
        fields = common
    elif category in HAZARD_ACCESS_CATEGORIES:
        fields = (*common, "accessKind")
    else:
        raise DeltaError(f"unsupported hazard category {category!r}")
    return "\0".join((category, make_key(row, fields, context)))


def load_artifact(path: Path, expected_id: str) -> dict[str, Any]:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise DeltaError(f"cannot read {path}: {exc}") from exc
    if data.get("artifactId") != expected_id:
        raise DeltaError(f"{path} must have artifactId {expected_id!r}")
    if not isinstance(data.get("categories"), dict):
        raise DeltaError(f"{path} is missing categories")
    return data


def load_closure_audit(path: Path) -> dict[str, Any]:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise DeltaError(f"cannot read {path}: {exc}") from exc
    if data.get("artifactId") != "AM025-PHASE2-CLOSURE-AUDIT":
        raise DeltaError(f"{path} must have artifactId 'AM025-PHASE2-CLOSURE-AUDIT'")
    rules = data.get("reviewRules")
    if not isinstance(rules, list) or not rules:
        raise DeltaError(f"{path} is missing reviewRules")
    return data


def artifact_descriptor(path: Path, data: dict[str, Any]) -> dict[str, Any]:
    return {
        "artifactId": data["artifactId"],
        "path": path.as_posix(),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
    }


def protected_ids(row: dict[str, Any], context: str) -> list[str]:
    value = row.get("protectedOwnerIds", [])
    if not isinstance(value, list) or any(not isinstance(item, str) or not item for item in value):
        raise DeltaError(f"{context} has invalid protectedOwnerIds")
    return sorted(set(value))


def unique_index(rows: list[dict[str, Any]], category: str, fields: tuple[str, ...]) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for position, row in enumerate(rows):
        context = f"final {category}[{position}]"
        key = make_key(row, fields, context)
        if key in result:
            raise DeltaError(f"ambiguous final key in {category}: {key!r}")
        result[key] = row
    return result


def occurrence_keys(base_keys: list[str]) -> list[str]:
    totals: dict[str, int] = {}
    for key in base_keys:
        totals[key] = totals.get(key, 0) + 1
    positions: dict[str, int] = {}
    result: list[str] = []
    for key in base_keys:
        if totals[key] == 1:
            result.append(key)
            continue
        position = positions.get(key, 0) + 1
        positions[key] = position
        result.append(f"{key}\0<occurrence:{position}-of-{totals[key]}>")
    return result


def final_indexes(ownership: dict[str, Any]) -> dict[str, dict[str, dict[str, Any]]]:
    categories = ownership["categories"]
    if set(categories) != set(FINAL_CATEGORY_KEYS):
        raise DeltaError("AM-021 final categories do not match the supported ownership schema")
    return {
        category: unique_index(rows, category, FINAL_CATEGORY_KEYS[category])
        for category, rows in categories.items()
    }


def baseline_rows(
    lifecycle: dict[str, Any],
    indexes: dict[str, dict[str, dict[str, Any]]],
    matched_final: set[tuple[str, str]],
) -> list[dict[str, Any]]:
    categories = lifecycle["categories"]
    if set(categories) != set(BASELINE_CATEGORY_KEYS):
        raise DeltaError("AM-007 baseline categories do not match the supported lifecycle schema")
    policy = lifecycle.get("policy", {})
    candidate_semantics = policy.get("candidateSemantics")
    if not isinstance(candidate_semantics, str) or not candidate_semantics.strip():
        raise DeltaError("AM-007 policy.candidateSemantics is required for uncovered baseline rows")

    result: list[dict[str, Any]] = []
    seen: set[str] = set()
    for category in sorted(categories):
        fields = BASELINE_CATEGORY_KEYS[category]
        rows = categories[category]
        base_keys = [
            "\0".join((category, make_key(row, fields, f"baseline {category}[{position}]")))
            for position, row in enumerate(rows)
        ]
        for position, (row, source_key) in enumerate(zip(rows, occurrence_keys(base_keys))):
            context = f"baseline {category}[{position}]"
            if source_key in seen:
                raise DeltaError(f"duplicate baseline key: {source_key!r}")
            seen.add(source_key)
            entry = {
                "authority": "AM-007 policy.candidateSemantics",
                "classification": "open",
                "finalArtifact": None,
                "finalCategory": None,
                "finalKey": None,
                "finalStatus": None,
                "protectedOwnerIds": [],
                "rationale": candidate_semantics.strip(),
                "sourceArtifact": "AM-007",
                "sourceCategory": category,
                "sourceKey": source_key,
                "sourceIdentity": stable_identity(row, fields, context),
                "sourceLine": row.get("line"),
                "sourcePath": normalized_path(required_text(row, "path", context)),
            }
            mapping = BASELINE_FINAL_CATEGORIES.get(category)
            if mapping is not None:
                final_category, join_fields = mapping
                join_key = make_key(row, join_fields, context)
                final = indexes[final_category].get(join_key)
                if final is not None:
                    status = required_text(final, "status", f"matched final {final_category}")
                    if status not in ("explicit", "protected-owner"):
                        raise DeltaError(f"unsupported AM-021 status {status!r}")
                    owners = protected_ids(final, f"matched final {final_category}")
                    if status == "protected-owner" and not owners:
                        raise DeltaError("protected AM-021 row has no protected owner authority")
                    if status == "explicit" and owners:
                        raise DeltaError("explicit AM-021 row unexpectedly names protected owners")
                    entry.update({
                        "authority": "AM-021 persistent-resource ownership",
                        "classification": "protected-deferred" if status == "protected-owner" else "resolved",
                        "finalArtifact": "AM-021",
                        "finalCategory": final_category,
                        "finalKey": join_key,
                        "finalStatus": status,
                        "protectedOwnerIds": owners,
                        "rationale": "AM-021 records an explicit lifecycle owner."
                        if status == "explicit" else "AM-021 records an active protected owner.",
                    })
                    matched_final.add((final_category, join_key))
            result.append(entry)
    return sorted(result, key=lambda row: row["sourceKey"])


def hazard_rows(hazards: dict[str, Any]) -> list[dict[str, Any]]:
    categories = hazards["categories"]
    expected = HAZARD_FIELD_CATEGORIES | HAZARD_ACCESS_CATEGORIES
    if set(categories) != expected:
        raise DeltaError("AM-018 hazard categories do not match the supported schema")
    classification_authority = hazards.get("classification")
    if not isinstance(classification_authority, dict):
        raise DeltaError("AM-018 classification authority is missing")

    result: list[dict[str, Any]] = []
    seen: set[str] = set()
    for category in sorted(categories):
        authority_text = classification_authority.get(category)
        if not isinstance(authority_text, str) or not authority_text.strip():
            raise DeltaError(f"AM-018 classification authority is missing for {category}")
        rows = categories[category]
        base_keys = [
            hazard_key(category, row, f"hazard {category}[{position}]")
            for position, row in enumerate(rows)
        ]
        for position, (row, source_key) in enumerate(zip(rows, occurrence_keys(base_keys))):
            context = f"hazard {category}[{position}]"
            if source_key in seen:
                raise DeltaError(f"duplicate hazard key: {source_key!r}")
            seen.add(source_key)
            disposition = required_text(row, "disposition", context)
            rationale = required_text(row, "rationale", context)
            owners = protected_ids(row, context)
            if owners:
                classification = "protected-deferred"
            elif disposition in RESOLVED_HAZARD_DISPOSITIONS:
                classification = "resolved"
            elif disposition in OPEN_HAZARD_DISPOSITIONS:
                classification = "open"
            else:
                raise DeltaError(f"{context} has unsupported disposition {disposition!r}")
            result.append({
                "authority": f"AM-018 classification.{category}",
                "classification": classification,
                "disposition": disposition,
                "finalArtifact": None,
                "finalCategory": None,
                "finalKey": None,
                "finalStatus": None,
                "protectedOwnerIds": owners,
                "rationale": rationale,
                "sourceArtifact": "AM-018",
                "sourceCategory": category,
                "sourceKey": source_key,
                "sourceIdentity": stable_identity(
                    row,
                    ("path", "ownerType", "memberName", "symbol")
                    if category in HAZARD_FIELD_CATEGORIES
                    else ("path", "ownerType", "memberName", "symbol", "accessKind"),
                    context,
                ),
                "sourceLine": row.get("line"),
                "sourcePath": normalized_path(required_text(row, "path", context)),
            })
    return sorted(result, key=lambda row: row["sourceKey"])


def rule_value_matches(actual: Any, expected: Any) -> bool:
    if isinstance(expected, list):
        return actual in expected
    return actual == expected


def review_rule_matches(row: dict[str, Any], rule: dict[str, Any]) -> bool:
    match = rule.get("match")
    if not isinstance(match, dict) or not match:
        raise DeltaError(f"review rule {rule.get('id')!r} has no match fields")
    identity = row.get("sourceIdentity", {})
    for field, expected in match.items():
        actual = identity.get(field) if field in identity else row.get(field)
        if not rule_value_matches(actual, expected):
            return False
    return True


def current_source_hash(root: Path, source_path: str) -> str:
    path = root / source_path
    if not path.is_file():
        raise DeltaError(f"reviewed source does not exist: {source_path}")
    return hashlib.sha256(path.read_bytes()).hexdigest()


def authority_descriptors(root: Path, authority: list[str]) -> list[dict[str, str]]:
    descriptors = []
    for value in sorted(set(authority)):
        path = root / value
        if not path.is_file():
            raise DeltaError(f"review authority does not exist: {value}")
        descriptors.append({
            "path": normalized_path(value),
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        })
    return descriptors


def apply_review_rules(
    baseline: list[dict[str, Any]],
    hazards: list[dict[str, Any]],
    audit: dict[str, Any],
    source_root: Path,
) -> dict[str, Any]:
    intake = sorted(
        [row for row in baseline + hazards if row["classification"] == "open"],
        key=lambda row: (row["sourceArtifact"], row["sourceKey"]),
    )
    unassigned = {f"{row['sourceArtifact']}\0{row['sourceKey']}": row for row in intake}
    rule_ids: set[str] = set()
    debt_ids: set[str] = set()

    for rule_position, rule in enumerate(audit["reviewRules"]):
        context = f"reviewRules[{rule_position}]"
        rule_id = required_text(rule, "id", context)
        if rule_id in rule_ids:
            raise DeltaError(f"duplicate review rule id: {rule_id}")
        rule_ids.add(rule_id)
        decision = required_text(rule, "decision", context)
        if decision not in REVIEW_DECISIONS:
            raise DeltaError(f"{context} has unsupported decision {decision!r}")
        reason_code = required_text(rule, "reasonCode", context)
        authority = rule.get("authority")
        if not isinstance(authority, list) or not authority or any(
            not isinstance(value, str) or not value.strip() for value in authority
        ):
            raise DeltaError(f"{context} must name authority paths")
        expected_count = rule.get("expectedCount")
        if not isinstance(expected_count, int) or expected_count < 1:
            raise DeltaError(f"{context} has invalid expectedCount")
        matched = sorted(
            [row for row in unassigned.values() if review_rule_matches(row, rule)],
            key=lambda row: (row["sourceArtifact"], row["sourceKey"]),
        )
        if len(matched) != expected_count:
            raise DeltaError(
                f"review rule {rule_id!r} expected {expected_count} rows but matched {len(matched)}"
            )
        protected = rule.get("protectedOwnerIds", [])
        if decision == "protected-deferred":
            if not isinstance(protected, list) or not protected or any(
                not isinstance(value, str) or not value for value in protected
            ):
                raise DeltaError(f"{context} must name protectedOwnerIds")
        elif protected:
            raise DeltaError(f"{context} names protectedOwnerIds for {decision}")
        debt_prefix = rule.get("debtIdPrefix")
        owner_domain = rule.get("ownerDomain")
        work_package = rule.get("workPackageId")
        if decision == "genuine-debt":
            if not all(isinstance(value, str) and value for value in (debt_prefix, owner_domain, work_package)):
                raise DeltaError(f"{context} must name debtIdPrefix, ownerDomain, and workPackageId")

        debt_id_by_group: dict[str, str] = {}
        if decision == "genuine-debt":
            debt_group_field = rule.get("debtGroupBy", "sourcePath")
            if not isinstance(debt_group_field, str) or not debt_group_field:
                raise DeltaError(f"{context} has invalid debtGroupBy")
            groups = sorted({
                str(row.get(debt_group_field, row.get("sourceIdentity", {}).get(debt_group_field, "")))
                for row in matched
            })
            if "" in groups:
                raise DeltaError(f"{context} cannot group debt by {debt_group_field!r}")
            debt_id_by_group = {
                group: f"{debt_prefix}-{position:03d}"
                for position, group in enumerate(groups, start=1)
            }
            duplicate_debt_ids = debt_ids.intersection(debt_id_by_group.values())
            if duplicate_debt_ids:
                raise DeltaError(
                    f"{context} generates duplicate debt ids: {sorted(duplicate_debt_ids)!r}"
                )
            debt_ids.update(debt_id_by_group.values())

        reviewed_authority = authority_descriptors(source_root, authority)

        for row in matched:
            row["reviewDecision"] = decision
            row["reviewRuleId"] = rule_id
            row["reviewReasonCode"] = reason_code
            row["reviewAuthority"] = reviewed_authority
            if decision == "resolved":
                row["classification"] = "resolved"
            elif decision == "protected-deferred":
                row["classification"] = "protected-deferred"
                row["protectedOwnerIds"] = sorted(set(protected))
            else:
                debt_group_field = rule.get("debtGroupBy", "sourcePath")
                debt_group = str(row.get(debt_group_field, row.get("sourceIdentity", {}).get(debt_group_field, "")))
                debt_id = debt_id_by_group[debt_group]
                row["classification"] = "open"
                row["debtId"] = debt_id
                row["ownerDomain"] = owner_domain
                row["workPackageId"] = work_package
                row["currentSourceSha256"] = current_source_hash(source_root, row["sourcePath"])
            unassigned.pop(f"{row['sourceArtifact']}\0{row['sourceKey']}")

    if unassigned:
        first = next(iter(sorted(unassigned)))
        raise DeltaError(f"closure audit leaves {len(unassigned)} intake rows unclassified; first={first!r}")

    decisions = {name: sum(row.get("reviewDecision") == name for row in intake) for name in REVIEW_DECISIONS}
    return {
        "genuineDebtRowCount": decisions["genuine-debt"],
        "historicalInitialOpenRowCount": len(intake),
        "protectedDeferredRowCount": decisions["protected-deferred"],
        "resolvedNonDebtRowCount": decisions["resolved"],
        "reviewedRowCount": sum(decisions.values()),
        "unclassifiedRowCount": len(unassigned),
        "uniqueDebtItemCount": len(debt_ids),
    }


def unmatched_final_rows(
    ownership: dict[str, Any], matched_final: set[tuple[str, str]]
) -> list[dict[str, Any]]:
    result: list[dict[str, Any]] = []
    for category in sorted(ownership["categories"]):
        fields = FINAL_CATEGORY_KEYS[category]
        for position, row in enumerate(ownership["categories"][category]):
            context = f"final {category}[{position}]"
            key = make_key(row, fields, context)
            if (category, key) in matched_final:
                continue
            result.append({
                "classification": "new-after-baseline",
                "finalCategory": category,
                "finalKey": key,
                "finalStatus": required_text(row, "status", context),
                "protectedOwnerIds": protected_ids(row, context),
                "sourceLine": row.get("line"),
                "sourcePath": normalized_path(required_text(row, "path", context)),
            })
    return sorted(result, key=lambda row: (row["finalCategory"], row["finalKey"]))


def build_report(
    lifecycle_path: Path,
    hazards_path: Path,
    ownership_path: Path,
    closure_audit_path: Path | None = None,
    source_root: Path | None = None,
) -> dict[str, Any]:
    lifecycle = load_artifact(lifecycle_path, "AM-007")
    hazards = load_artifact(hazards_path, "AM-018")
    ownership = load_artifact(ownership_path, "AM-021")
    indexes = final_indexes(ownership)
    matched: set[tuple[str, str]] = set()
    baseline = baseline_rows(lifecycle, indexes, matched)
    hazard = hazard_rows(hazards)
    historical_counts = {
        name: sum(row["classification"] == name for row in baseline + hazard)
        for name in CLASSIFICATIONS
    }
    review_summary = None
    audit = None
    if closure_audit_path is not None:
        audit = load_closure_audit(closure_audit_path)
        review_summary = apply_review_rules(
            baseline,
            hazard,
            audit,
            source_root.resolve() if source_root is not None else Path.cwd().resolve(),
        )
        projection = audit.get("projection")
        if not isinstance(projection, dict):
            raise DeltaError("closure audit is missing projection")
        expected = {
            "genuineDebtRowCount": projection.get("genuineDebtRowCount"),
            "historicalInitialOpenRowCount": projection.get("historicalIntakeRowCount"),
            "protectedDeferredRowCount": projection.get("protectedDeferredRowCount"),
            "resolvedNonDebtRowCount": projection.get("resolvedNonDebtRowCount"),
            "reviewedRowCount": projection.get("reviewedRowCount"),
            "unclassifiedRowCount": projection.get("unclassifiedRowCount"),
            "uniqueDebtItemCount": projection.get("uniqueDebtItemCount"),
        }
        for field, value in expected.items():
            if review_summary[field] != value:
                raise DeltaError(
                    f"closure audit projection mismatch for {field}: expected {value}, got {review_summary[field]}"
                )
    classifications = baseline + hazard
    counts = {name: sum(row["classification"] == name for row in classifications) for name in CLASSIFICATIONS}
    unmatched = unmatched_final_rows(ownership, matched)
    inputs = [
        artifact_descriptor(lifecycle_path, lifecycle),
        artifact_descriptor(hazards_path, hazards),
        artifact_descriptor(ownership_path, ownership),
    ]
    if closure_audit_path is not None and audit is not None:
        inputs.append(artifact_descriptor(closure_audit_path, audit))
    return {
        "artifactId": ARTIFACT_ID,
        "baselineClassifications": baseline,
        "hazardClassifications": hazard,
        "inputs": inputs,
        "newAfterBaseline": unmatched,
        "schemaVersion": SCHEMA_VERSION,
        "summary": {
            "baselineRowCount": len(baseline),
            "classificationCounts": counts,
            "classifiedRowCount": len(classifications),
            "finalResourceCount": sum(len(rows) for rows in ownership["categories"].values()),
            "hazardRowCount": len(hazard),
            "historicalClassificationCounts": historical_counts,
            "newAfterBaselineCount": len(unmatched),
            "openCount": counts["open"],
            "review": review_summary,
        },
    }


def json_bytes(report: dict[str, Any]) -> bytes:
    return (json.dumps(report, indent=2, sort_keys=True, ensure_ascii=True) + "\n").encode("utf-8")


def render_markdown(report: dict[str, Any]) -> str:
    summary = report["summary"]
    counts = summary["classificationCounts"]
    lines = [
        "# AM-025 Phase 2 Ownership Delta",
        "",
        "Generated deterministically from AM-007, AM-018, and AM-021. Line numbers are diagnostic only and never participate in identity.",
        "",
        "## Summary",
        "",
        "| Measure | Count |",
        "|---|---:|",
        f"| Baseline rows | {summary['baselineRowCount']} |",
        f"| Hazard rows | {summary['hazardRowCount']} |",
        f"| Resolved | {counts['resolved']} |",
        f"| Protected/deferred | {counts['protected-deferred']} |",
        f"| Open | {counts['open']} |",
        f"| New after baseline | {summary['newAfterBaselineCount']} |",
    ]
    review = summary.get("review")
    if review is not None:
        lines.extend([
            f"| Historical intake rows | {review['historicalInitialOpenRowCount']} |",
            f"| Reviewed non-debt rows | {review['resolvedNonDebtRowCount'] + review['protectedDeferredRowCount']} |",
            f"| Genuine-debt rows | {review['genuineDebtRowCount']} |",
            f"| Unclassified rows | {review['unclassifiedRowCount']} |",
            f"| Unique debt items | {review['uniqueDebtItemCount']} |",
        ])
    lines.extend([
        "",
        "## Open Rows",
        "",
        "| Source | Category | Path | Authority | Debt ID |",
        "|---|---|---|---|---|",
    ])
    open_rows = [
        *[row for row in report["baselineClassifications"] if row["classification"] == "open"],
        *[row for row in report["hazardClassifications"] if row["classification"] == "open"],
    ]
    for row in sorted(open_rows, key=lambda item: item["sourceKey"]):
        values = (
            row["sourceArtifact"],
            row["sourceCategory"],
            row["sourcePath"],
            row["authority"],
            row.get("debtId", "-"),
        )
        lines.append("| " + " | ".join(value.replace("|", "\\|") for value in values) + " |")
    if not open_rows:
        lines.append("| - | - | - | None | - |")
    return "\n".join(lines) + "\n"


def write_or_check(path: Path, content: bytes, check: bool) -> None:
    if check:
        if not path.exists() or path.read_bytes() != content:
            raise DeltaError(f"generated output is stale or missing: {path}")
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.tmp")
    temporary.write_bytes(content)
    os.replace(temporary, path)


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--lifecycle", type=Path, required=True)
    parser.add_argument("--hazards", type=Path, required=True)
    parser.add_argument("--ownership", type=Path, required=True)
    parser.add_argument("--closure-audit", type=Path)
    parser.add_argument("--source-root", type=Path, default=Path("."))
    parser.add_argument("--json-output", type=Path, required=True)
    parser.add_argument("--markdown-output", type=Path, required=True)
    parser.add_argument("--check", action="store_true")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        report = build_report(
            args.lifecycle,
            args.hazards,
            args.ownership,
            closure_audit_path=args.closure_audit,
            source_root=args.source_root,
        )
        write_or_check(args.json_output, json_bytes(report), args.check)
        write_or_check(args.markdown_output, render_markdown(report).encode("utf-8"), args.check)
    except DeltaError as exc:
        print(f"ERROR: {exc}")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

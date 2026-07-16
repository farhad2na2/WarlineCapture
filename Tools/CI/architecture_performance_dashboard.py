#!/usr/bin/env python3
"""Generate and validate the tracked architecture/performance dashboard."""

from __future__ import annotations

import argparse
import hashlib
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Callable


SCHEMA_VERSION = 2
REGISTRY_SCHEMA_VERSION = 1
DEFAULT_JSON = "Design/AgentReports/architecture_performance_dashboard.json"
DEFAULT_MARKDOWN = "Design/AgentReports/architecture_performance_dashboard.md"
DEFAULT_REGISTRY = "Design/AgentReports/ArchitectureMaturity/validator_registry.json"
DEFAULT_REGISTRY_MARKDOWN = "Design/AgentReports/ArchitectureMaturity/validator_registry.md"
ACCEPTED_STATE = "current"
REJECTED_STATES = ("malformed", "missing", "stale", "unknown")


@dataclass(frozen=True)
class InputSpec:
    id: str
    path: str
    category: str
    requirement: str
    lane_state: str
    owner_validator_id: str
    revision_policy: str
    environment_policy: str
    metric_reader: str
    required_fields: tuple[str, ...]


@dataclass(frozen=True)
class RegistryResult:
    data: dict[str, Any] | None
    errors: tuple[dict[str, str], ...]
    sha256: str | None
    specs: tuple[InputSpec, ...]


def scalar_items(value: Any) -> dict[str, Any]:
    if not isinstance(value, dict):
        return {}
    return {
        str(key): item
        for key, item in sorted(value.items())
        if isinstance(item, (bool, int, float)) and item is not None
    }


PROVENANCE_FIELDS = {
    "baselineCommit",
    "dirty",
    "environmentIdentitySha256",
    "exactCommit",
    "schemaVersion",
}


def build_metrics(data: dict[str, Any]) -> dict[str, Any]:
    return {key: value for key, value in scalar_items(data).items() if key not in PROVENANCE_FIELDS}


def summary_metrics(data: dict[str, Any]) -> dict[str, Any]:
    return scalar_items(data.get("summary"))


def performance_metrics(data: dict[str, Any]) -> dict[str, Any]:
    return {key: value for key, value in scalar_items(data).items() if key not in PROVENANCE_FIELDS}


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


METRIC_READERS: dict[str, Callable[[dict[str, Any]], dict[str, Any]]] = {
    "audio": audio_metrics,
    "build": build_metrics,
    "performance": performance_metrics,
    "summary": summary_metrics,
}


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def registry_error(code: str, message: str) -> dict[str, str]:
    return {"code": code, "message": message}


def _string(value: Any) -> str | None:
    return value if isinstance(value, str) and value else None


def load_registry(root: Path, registry_path: str, revision: str) -> RegistryResult:
    path = root / registry_path
    if not path.is_file():
        return RegistryResult(
            None,
            (registry_error("REGISTRY_MISSING", f"validator registry is missing: {registry_path}"),),
            None,
            (),
        )
    try:
        raw = path.read_bytes()
        data = json.loads(raw.decode("utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as error:
        return RegistryResult(
            None,
            (registry_error("REGISTRY_MALFORMED_JSON", f"validator registry is not readable JSON: {error}"),),
            None,
            (),
        )
    if not isinstance(data, dict):
        return RegistryResult(
            None,
            (registry_error("REGISTRY_MALFORMED_ROOT", "validator registry root must be a JSON object"),),
            sha256_bytes(raw),
            (),
        )

    errors: list[dict[str, str]] = []
    if data.get("schemaVersion") != REGISTRY_SCHEMA_VERSION:
        errors.append(registry_error(
            "REGISTRY_SCHEMA_UNSUPPORTED",
            f"validator registry schemaVersion must be {REGISTRY_SCHEMA_VERSION}",
        ))

    baseline = data.get("baseline")
    registry_revision = baseline.get("commit") if isinstance(baseline, dict) else None
    if not _string(registry_revision):
        errors.append(registry_error("REGISTRY_REVISION_UNKNOWN", "registry baseline.commit is missing"))
    elif registry_revision != revision:
        errors.append(registry_error(
            "REGISTRY_REVISION_MISMATCH",
            f"registry baseline.commit {registry_revision} does not match dashboard revision {revision}",
        ))

    environment = data.get("environmentIdentity")
    if not isinstance(environment, dict):
        errors.append(registry_error("REGISTRY_ENVIRONMENT_UNKNOWN", "registry environmentIdentity is missing"))
    else:
        environment_path = _string(environment.get("path"))
        expected_hash = _string(environment.get("sha256"))
        if not environment_path or not expected_hash:
            errors.append(registry_error(
                "REGISTRY_ENVIRONMENT_UNKNOWN",
                "registry environmentIdentity requires path and sha256",
            ))
        else:
            environment_file = root / environment_path
            if not environment_file.is_file():
                errors.append(registry_error(
                    "REGISTRY_ENVIRONMENT_MISSING",
                    f"registry environment identity is missing: {environment_path}",
                ))
            elif sha256_bytes(environment_file.read_bytes()) != expected_hash:
                errors.append(registry_error(
                    "REGISTRY_ENVIRONMENT_MISMATCH",
                    f"registry environment identity hash does not match: {environment_path}",
                ))

    validators = data.get("validators")
    validator_ids: set[str] = set()
    validator_order: list[str] = []
    responsibility_owners: dict[str, str] = {}
    if not isinstance(validators, list):
        errors.append(registry_error("REGISTRY_VALIDATORS_MALFORMED", "validators must be an array"))
        validators = []
    for index, validator in enumerate(validators):
        if not isinstance(validator, dict):
            errors.append(registry_error(
                "REGISTRY_VALIDATOR_MALFORMED",
                f"validators[{index}] must be an object",
            ))
            continue
        validator_id = _string(validator.get("id"))
        if not validator_id:
            errors.append(registry_error(
                "REGISTRY_VALIDATOR_ID_MISSING",
                f"validators[{index}].id is missing",
            ))
            continue
        if validator_id in validator_ids:
            errors.append(registry_error(
                "DUPLICATE_VALIDATOR_ID",
                f"validator id has more than one owner row: {validator_id}",
            ))
        validator_ids.add(validator_id)
        validator_order.append(validator_id)

        owner = validator.get("owner")
        owner_path = owner.get("path") if isinstance(owner, dict) else None
        owner_selector = owner.get("selector") if isinstance(owner, dict) else None
        if not _string(owner_path) or not _string(owner_selector):
            errors.append(registry_error(
                "REGISTRY_OWNER_MALFORMED",
                f"validator {validator_id} requires owner.path and owner.selector",
            ))
        elif not (root / owner_path).is_file():
            errors.append(registry_error(
                "REGISTRY_OWNER_MISSING",
                f"validator {validator_id} owner path is missing: {owner_path}",
            ))

        responsibilities = validator.get("responsibilities")
        if not isinstance(responsibilities, list) or not responsibilities:
            errors.append(registry_error(
                "REGISTRY_RESPONSIBILITY_MISSING",
                f"validator {validator_id} has no responsibilities",
            ))
            continue
        normalized = [item for item in responsibilities if _string(item)]
        if len(normalized) != len(responsibilities) or normalized != sorted(set(normalized)):
            errors.append(registry_error(
                "REGISTRY_RESPONSIBILITY_ORDER",
                f"validator {validator_id} responsibilities must be unique strings sorted ascending",
            ))
        for responsibility in normalized:
            previous = responsibility_owners.get(responsibility)
            if previous is not None and previous != validator_id:
                errors.append(registry_error(
                    "DUPLICATE_RESPONSIBILITY_OWNER",
                    f"responsibility {responsibility} is owned by both {previous} and {validator_id}",
                ))
            else:
                responsibility_owners[responsibility] = validator_id
    if validator_order != sorted(validator_order):
        errors.append(registry_error(
            "REGISTRY_VALIDATOR_ORDER",
            "validators must be sorted by id ascending",
        ))

    aliases = data.get("nonCanonicalAliases", [])
    alias_ids: list[str] = []
    if not isinstance(aliases, list):
        errors.append(registry_error("REGISTRY_ALIASES_MALFORMED", "nonCanonicalAliases must be an array"))
        aliases = []
    for index, alias in enumerate(aliases):
        if not isinstance(alias, dict) or not _string(alias.get("id")):
            errors.append(registry_error(
                "REGISTRY_ALIAS_MALFORMED",
                f"nonCanonicalAliases[{index}] requires an id",
            ))
            continue
        alias_id = alias["id"]
        alias_ids.append(alias_id)
        canonical_ids = alias.get("canonicalOwnerIds")
        if not isinstance(canonical_ids, list) or not canonical_ids:
            errors.append(registry_error(
                "REGISTRY_ALIAS_OWNER_MISSING",
                f"non-canonical alias {alias_id} has no canonicalOwnerIds",
            ))
        else:
            unknown = sorted(item for item in canonical_ids if item not in validator_ids)
            if unknown:
                errors.append(registry_error(
                    "REGISTRY_ALIAS_OWNER_UNKNOWN",
                    f"non-canonical alias {alias_id} references unknown owners: {', '.join(unknown)}",
                ))
    if alias_ids != sorted(set(alias_ids)):
        errors.append(registry_error(
            "REGISTRY_ALIAS_ORDER",
            "nonCanonicalAliases ids must be unique and sorted ascending",
        ))

    evidence_inputs = data.get("evidenceInputs")
    specs: list[InputSpec] = []
    input_ids: set[str] = set()
    input_paths: set[str] = set()
    if not isinstance(evidence_inputs, list):
        errors.append(registry_error("REGISTRY_INPUTS_MALFORMED", "evidenceInputs must be an array"))
        evidence_inputs = []
    for index, item in enumerate(evidence_inputs):
        if not isinstance(item, dict):
            errors.append(registry_error("REGISTRY_INPUT_MALFORMED", f"evidenceInputs[{index}] must be an object"))
            continue
        values = {
            key: _string(item.get(key))
            for key in (
                "id", "path", "category", "requirement", "laneState",
                "ownerValidatorId", "revisionPolicy", "environmentPolicy", "metricReader",
            )
        }
        missing = sorted(key for key, value in values.items() if value is None)
        if missing:
            errors.append(registry_error(
                "REGISTRY_INPUT_FIELD_MISSING",
                f"evidenceInputs[{index}] is missing: {', '.join(missing)}",
            ))
            continue
        input_id = values["id"]
        input_path = values["path"]
        assert input_id and input_path
        if input_id in input_ids:
            errors.append(registry_error("DUPLICATE_INPUT_ID", f"evidence input id is duplicate: {input_id}"))
        if input_path in input_paths:
            errors.append(registry_error(
                "DUPLICATE_INPUT_OWNER",
                f"evidence path has more than one owner row: {input_path}",
            ))
        input_ids.add(input_id)
        input_paths.add(input_path)
        if values["ownerValidatorId"] not in validator_ids:
            errors.append(registry_error(
                "REGISTRY_INPUT_OWNER_UNKNOWN",
                f"evidence input {input_id} references unknown owner {values['ownerValidatorId']}",
            ))
        if values["requirement"] not in ("advisory", "required"):
            errors.append(registry_error(
                "REGISTRY_INPUT_REQUIREMENT_UNKNOWN",
                f"evidence input {input_id} requirement must be advisory or required",
            ))
        if values["laneState"] not in ("active", "deferred"):
            errors.append(registry_error(
                "REGISTRY_INPUT_LANE_UNKNOWN",
                f"evidence input {input_id} laneState must be active or deferred",
            ))
        if values["revisionPolicy"] != "exact-commit":
            errors.append(registry_error(
                "REGISTRY_REVISION_POLICY_UNKNOWN",
                f"evidence input {input_id} revisionPolicy must be exact-commit",
            ))
        if values["environmentPolicy"] not in ("exact-environment", "not-required"):
            errors.append(registry_error(
                "REGISTRY_ENVIRONMENT_POLICY_UNKNOWN",
                f"evidence input {input_id} has an unsupported environmentPolicy",
            ))
        if values["metricReader"] not in METRIC_READERS:
            errors.append(registry_error(
                "REGISTRY_METRIC_READER_UNKNOWN",
                f"evidence input {input_id} has unknown metricReader {values['metricReader']}",
            ))
        required_fields = item.get("requiredFields")
        if (
            not isinstance(required_fields, list)
            or not required_fields
            or any(not _string(field) for field in required_fields)
            or required_fields != sorted(set(required_fields))
        ):
            errors.append(registry_error(
                "REGISTRY_REQUIRED_FIELDS_MALFORMED",
                f"evidence input {input_id} requiredFields must be unique strings sorted ascending",
            ))
            required_fields = []
        specs.append(InputSpec(
            id=input_id,
            path=input_path,
            category=values["category"] or "",
            requirement=values["requirement"] or "",
            lane_state=values["laneState"] or "",
            owner_validator_id=values["ownerValidatorId"] or "",
            revision_policy=values["revisionPolicy"] or "",
            environment_policy=values["environmentPolicy"] or "",
            metric_reader=values["metricReader"] or "",
            required_fields=tuple(required_fields),
        ))

    if specs != sorted(specs, key=lambda spec: spec.id):
        errors.append(registry_error(
            "REGISTRY_INPUT_ORDER",
            "evidenceInputs must be sorted by id ascending",
        ))
    return RegistryResult(
        data,
        tuple(sorted(errors, key=lambda item: (item["code"], item["message"]))),
        sha256_bytes(raw),
        tuple(sorted(specs, key=lambda spec: spec.path)),
    )


def claimed_revision(data: dict[str, Any]) -> tuple[str | None, str | None]:
    for field in ("exactCommit", "baselineCommit"):
        value = data.get(field)
        if isinstance(value, str) and value:
            return field, value
    return None, None


def reason(code: str, message: str) -> dict[str, str]:
    return {"code": code, "message": message}


def shape_reasons(data: dict[str, Any], spec: InputSpec) -> list[dict[str, str]]:
    missing = [field for field in spec.required_fields if field not in data]
    if missing:
        return [reason(
            "MALFORMED_REQUIRED_FIELDS",
            f"input is missing required fields: {', '.join(missing)}",
        )]
    if spec.metric_reader == "summary" and not isinstance(data.get("summary"), dict):
        return [reason("MALFORMED_SUMMARY", "input summary must be a JSON object")]
    if spec.metric_reader == "audio" and not isinstance(data.get("snapshots"), list):
        return [reason("MALFORMED_SNAPSHOTS", "input snapshots must be a JSON array")]
    if spec.metric_reader == "build":
        if isinstance(data.get("artifactBytes"), bool) or not isinstance(data.get("artifactBytes"), (int, float)):
            return [reason("MALFORMED_ARTIFACT_BYTES", "input artifactBytes must be numeric")]
        if not _string(data.get("status")):
            return [reason("MALFORMED_STATUS", "input status must be a non-empty string")]
    if spec.metric_reader == "performance":
        numeric_fields = ("editorP95FrameBudgetMs", "frameCount", "p95FrameMs")
        invalid = [
            field for field in numeric_fields
            if isinstance(data.get(field), bool) or not isinstance(data.get(field), (int, float))
        ]
        if invalid:
            return [reason(
                "MALFORMED_PERFORMANCE_METRICS",
                f"input performance fields must be numeric: {', '.join(invalid)}",
            )]
        if not isinstance(data.get("editorP95FrameBudgetPassed"), bool):
            return [reason(
                "MALFORMED_PERFORMANCE_BUDGET_STATE",
                "input editorP95FrameBudgetPassed must be boolean",
            )]
    return []


def freshness(
    data: dict[str, Any],
    spec: InputSpec,
    revision: str,
    environment_sha256: str | None,
) -> tuple[str, list[dict[str, str]]]:
    field, claimed = claimed_revision(data)
    reasons: list[dict[str, str]] = []
    if claimed is None:
        reasons.append(reason("UNKNOWN_REVISION", "input does not declare exactCommit or baselineCommit"))
    elif claimed != revision:
        reasons.append(reason(
            "REVISION_MISMATCH",
            f"{field} {claimed} does not match dashboard revision {revision}",
        ))
    if data.get("dirty") is True:
        reasons.append(reason("DIRTY_INPUT", "input declares dirty=true"))

    if spec.environment_policy == "exact-environment":
        claimed_environment = data.get("environmentIdentitySha256")
        if not isinstance(claimed_environment, str) or not claimed_environment:
            reasons.append(reason(
                "UNKNOWN_ENVIRONMENT",
                "input does not declare environmentIdentitySha256",
            ))
        elif claimed_environment != environment_sha256:
            reasons.append(reason(
                "ENVIRONMENT_MISMATCH",
                "input environmentIdentitySha256 does not match the registry environment identity",
            ))

    stale_codes = {"DIRTY_INPUT", "ENVIRONMENT_MISMATCH", "REVISION_MISMATCH"}
    if any(item["code"] in stale_codes for item in reasons):
        return "stale", reasons
    if reasons:
        return "unknown", reasons
    return "current", []


def read_input(
    root: Path,
    spec: InputSpec,
    revision: str,
    environment_sha256: str | None,
) -> dict[str, Any]:
    path = root / spec.path
    base: dict[str, Any] = {
        "category": spec.category,
        "disposition": "rejected" if spec.requirement == "required" else "attention",
        "freshness": "missing",
        "id": spec.id,
        "laneState": spec.lane_state,
        "metrics": {},
        "ownerValidatorId": spec.owner_validator_id,
        "path": spec.path,
        "reasons": [reason("MISSING_INPUT", "expected tracked JSON input is missing")],
        "requirement": spec.requirement,
    }
    if not path.is_file():
        return base
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as error:
        base["freshness"] = "malformed"
        base["reasons"] = [reason("MALFORMED_JSON", f"input is not readable JSON: {error}")]
        return base
    if not isinstance(data, dict):
        base["freshness"] = "malformed"
        base["reasons"] = [reason("MALFORMED_ROOT", "input root must be a JSON object")]
        return base

    malformed = shape_reasons(data, spec)
    if malformed:
        base["freshness"] = "malformed"
        base["reasons"] = malformed
        return base

    state, reasons = freshness(data, spec, revision, environment_sha256)
    field, claimed = claimed_revision(data)
    base.update({
        "claimedEnvironmentIdentitySha256": data.get("environmentIdentitySha256"),
        "claimedRevision": claimed,
        "claimedRevisionField": field,
        "dirty": data.get("dirty") if isinstance(data.get("dirty"), bool) else None,
        "disposition": "accepted" if state == ACCEPTED_STATE else (
            "rejected" if spec.requirement == "required" else "attention"
        ),
        "freshness": state,
        "metrics": METRIC_READERS.get(spec.metric_reader, lambda _: {})(data),
        "reasons": reasons,
        "sourceStatus": data.get("status", data.get("captureResult")),
        "taskId": data.get("taskId"),
    })
    return base


def build_dashboard(
    root: Path,
    revision: str,
    registry_path: str = DEFAULT_REGISTRY,
) -> dict[str, Any]:
    registry = load_registry(root, registry_path, revision)
    environment_sha256 = None
    if registry.data and isinstance(registry.data.get("environmentIdentity"), dict):
        environment_sha256 = registry.data["environmentIdentity"].get("sha256")
    inputs = [read_input(root, spec, revision, environment_sha256) for spec in registry.specs]
    freshness_counts = {
        state: sum(item["freshness"] == state for item in inputs)
        for state in ("current", "stale", "unknown", "missing", "malformed")
    }
    required = [item for item in inputs if item["requirement"] == "required"]
    advisory = [item for item in inputs if item["requirement"] == "advisory"]
    required_rejected = sum(item["freshness"] != ACCEPTED_STATE for item in required)
    advisory_attention = sum(item["freshness"] != ACCEPTED_STATE for item in advisory)
    gate_state = "accepted" if not registry.errors and required_rejected == 0 else "rejected"
    return {
        "dashboardRevision": revision,
        "gateState": gate_state,
        "inputCount": len(inputs),
        "inputs": inputs,
        "registry": {
            "errors": list(registry.errors),
            "path": registry_path,
            "schemaVersion": registry.data.get("schemaVersion") if registry.data else None,
            "sha256": registry.sha256,
            "state": "current" if not registry.errors else "malformed",
        },
        "schemaVersion": SCHEMA_VERSION,
        "summary": {
            "advisoryAttentionCount": advisory_attention,
            "advisoryInputCount": len(advisory),
            "freshnessCounts": freshness_counts,
            "healthyInputCount": freshness_counts["current"],
            "registryErrorCount": len(registry.errors),
            "requiredInputCount": len(required),
            "requiredRejectedCount": required_rejected,
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
        "> Generated by `python3 Tools/CI/architecture_performance_dashboard.py --revision <commit>`; do not edit manually.",
        "",
        f"- Dashboard revision: `{dashboard['dashboardRevision']}`",
        f"- Gate: **{dashboard['gateState']}**",
        f"- Registry: **{dashboard['registry']['state']}** (`{dashboard['registry']['sha256'] or 'unknown'}`)",
        f"- Inputs: {dashboard['inputCount']}",
        f"- Required rejected: {summary['requiredRejectedCount']} / {summary['requiredInputCount']}",
        f"- Advisory attention: {summary['advisoryAttentionCount']} / {summary['advisoryInputCount']}",
        f"- Current: {counts['current']}",
        "",
    ]
    if dashboard["registry"]["errors"]:
        lines.extend(["## Registry Rejections", ""])
        lines.extend(
            f"- `{item['code']}`: {item['message']}"
            for item in dashboard["registry"]["errors"]
        )
        lines.append("")
    lines.extend([
        "## Input Health",
        "",
        "| Input | Requirement | Lane | Owner | Freshness | Disposition | Claimed revision |",
        "|---|---|---|---|---|---|---|",
    ])
    for item in dashboard["inputs"]:
        claimed = f"`{item['claimedRevision']}`" if item.get("claimedRevision") else "unknown"
        lines.append(
            f"| `{item['path']}` | {item['requirement']} | {item['laneState']} | "
            f"`{item['ownerValidatorId']}` | **{item['freshness']}** | "
            f"**{item['disposition']}** | {claimed} |"
        )

    lines.extend(["", "## Details", ""])
    for item in dashboard["inputs"]:
        lines.extend([
            f"### `{item['path']}`",
            "",
            f"Freshness: **{item['freshness']}**. Disposition: **{item['disposition']}**.",
            "",
        ])
        if item["reasons"]:
            lines.append("Reasons:")
            lines.extend(f"- `{entry['code']}`: {entry['message']}" for entry in item["reasons"])
            lines.append("")
        metrics = item["metrics"]
        if metrics:
            lines.extend(["| Metric | Value |", "|---|---:|"])
            lines.extend(f"| `{key}` | {format_value(value)} |" for key, value in sorted(metrics.items()))
        else:
            lines.append("No metrics available.")
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def render_registry_markdown(registry: dict[str, Any]) -> str:
    baseline = registry["baseline"]
    environment = registry["environmentIdentity"]
    lines = [
        "# Architecture And Performance Validator Registry",
        "",
        "> Rendered from `validator_registry.json`; edit the JSON authority and regenerate.",
        "",
        f"- Baseline commit: `{baseline['commit']}`",
        f"- Baseline tree: `{baseline['tree']}`",
        f"- Environment identity: `{environment['path']}` (`{environment['sha256']}`)",
        f"- Validators: {len(registry['validators'])}",
        f"- Evidence inputs: {len(registry['evidenceInputs'])}",
        "",
        "## Canonical Validators",
        "",
        "| ID | Lane | Owner | Responsibilities |",
        "|---|---|---|---|",
    ]
    for validator in registry["validators"]:
        owner = validator["owner"]
        responsibilities = ", ".join(f"`{item}`" for item in validator["responsibilities"])
        lines.append(
            f"| `{validator['id']}` | {validator['laneState']} | "
            f"`{owner['path']}::{owner['selector']}` | {responsibilities} |"
        )
    lines.extend([
        "",
        "## Evidence Inputs",
        "",
        "| ID | Requirement | Lane | Owner | Revision | Environment | Required fields | Path |",
        "|---|---|---|---|---|---|---|---|",
    ])
    for item in registry["evidenceInputs"]:
        lines.append(
            f"| `{item['id']}` | {item['requirement']} | {item['laneState']} | "
            f"`{item['ownerValidatorId']}` | {item['revisionPolicy']} | "
            f"{item['environmentPolicy']} | "
            f"{', '.join(f'`{field}`' for field in item['requiredFields'])} | `{item['path']}` |"
        )
    lines.extend([
        "",
        "## Enforcement",
        "",
        "- Every responsibility has exactly one canonical validator owner.",
        "- Every evidence path has exactly one owner row.",
        "- Required evidence fails closed when missing, malformed, stale, unknown, dirty, environment-mismatched, or commit-mismatched.",
        "- Advisory and release-deferred evidence remains visible but does not block the Core Architecture Lane.",
        "- `--check` exits nonzero while the dashboard gate is rejected.",
        "",
    ])
    return "\n".join(lines)


def write_dashboard(
    root: Path,
    json_path: str,
    markdown_path: str,
    revision: str,
    registry_path: str = DEFAULT_REGISTRY,
    registry_markdown_path: str | None = DEFAULT_REGISTRY_MARKDOWN,
) -> dict[str, Any]:
    dashboard = build_dashboard(root, revision, registry_path)
    json_output = root / json_path
    markdown_output = root / markdown_path
    json_output.parent.mkdir(parents=True, exist_ok=True)
    markdown_output.parent.mkdir(parents=True, exist_ok=True)
    json_output.write_text(json.dumps(dashboard, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    markdown_output.write_text(render_markdown(dashboard), encoding="utf-8")
    if registry_markdown_path and dashboard["registry"]["state"] == "current":
        registry = load_registry(root, registry_path, revision)
        assert registry.data is not None
        registry_output = root / registry_markdown_path
        registry_output.parent.mkdir(parents=True, exist_ok=True)
        registry_output.write_text(render_registry_markdown(registry.data), encoding="utf-8")
    return dashboard


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--revision", required=True, help="Exact revision used for input freshness checks")
    parser.add_argument("--json-output", default=DEFAULT_JSON)
    parser.add_argument("--markdown-output", default=DEFAULT_MARKDOWN)
    parser.add_argument("--registry", default=DEFAULT_REGISTRY)
    parser.add_argument("--registry-markdown-output", default=DEFAULT_REGISTRY_MARKDOWN)
    parser.add_argument("--check", action="store_true", help="Exit nonzero when the required gate is rejected")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    root = args.root.resolve()
    dashboard = write_dashboard(
        root,
        args.json_output,
        args.markdown_output,
        args.revision,
        args.registry,
        args.registry_markdown_output,
    )
    if args.check and dashboard["gateState"] != "accepted":
        print(
            "[ArchitecturePerformanceDashboard] result=Rejected "
            f"requiredRejected={dashboard['summary']['requiredRejectedCount']} "
            f"registryErrors={dashboard['summary']['registryErrorCount']}"
        )
        return 2
    print(
        "[ArchitecturePerformanceDashboard] result=Generated "
        f"gate={dashboard['gateState']} inputs={dashboard['inputCount']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

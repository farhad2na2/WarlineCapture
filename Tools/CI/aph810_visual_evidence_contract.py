#!/usr/bin/env python3
"""Validate the fail-closed APH-810 MCP or fallback visual-evidence contract."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


SCHEMA_VERSION = 1
TASK_ID = "APH-810"
SCREENSHOT_SUFFIXES = {".png", ".jpg", ".jpeg"}


class EvidenceValidationError(RuntimeError):
    pass


def _object(value: Any, path: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise EvidenceValidationError(f"{path} must be an object")
    return value


def _non_empty_string(value: Any, path: str) -> str:
    if not isinstance(value, str) or not value.strip():
        raise EvidenceValidationError(f"{path} must be a non-empty string")
    return value.strip()


def _only_keys(value: dict[str, Any], allowed: set[str], path: str) -> None:
    unknown = sorted(set(value) - allowed)
    if unknown:
        raise EvidenceValidationError(f"{path} has unknown fields: {', '.join(unknown)}")


def _validate_capture(
    capture: Any,
    path: str,
    *,
    require_tool: bool,
    root: Path | None,
) -> dict[str, str]:
    item = _object(capture, path)
    allowed = {"path", "view", "description", "tool"}
    _only_keys(item, allowed, path)
    required = {"path", "view", "description"} | ({"tool"} if require_tool else set())
    missing = sorted(required - set(item))
    if missing:
        raise EvidenceValidationError(f"{path} is missing: {', '.join(missing)}")

    result = {
        key: _non_empty_string(item[key], f"{path}.{key}")
        for key in sorted(required)
    }
    screenshot_path = Path(result["path"])
    if screenshot_path.suffix.lower() not in SCREENSHOT_SUFFIXES:
        raise EvidenceValidationError(f"{path}.path must name a PNG or JPEG screenshot")
    if root is not None:
        resolved = screenshot_path if screenshot_path.is_absolute() else root / screenshot_path
        if not resolved.is_file():
            raise EvidenceValidationError(f"{path}.path does not exist: {resolved}")
        if resolved.stat().st_size == 0:
            raise EvidenceValidationError(f"{path}.path is empty: {resolved}")
    return result


def _validate_mcp_operation(value: Any, path: str) -> dict[str, str]:
    item = _object(value, path)
    allowed = {"tool", "target", "result"}
    _only_keys(item, allowed, path)
    missing = sorted(allowed - set(item))
    if missing:
        raise EvidenceValidationError(f"{path} is missing: {', '.join(missing)}")
    return {
        key: _non_empty_string(item[key], f"{path}.{key}")
        for key in sorted(allowed)
    }


def _validate_mcp(data: dict[str, Any], root: Path | None) -> dict[str, Any]:
    if data.get("fallback") is not None:
        raise EvidenceValidationError("fallback must be null when MCP is connected")
    evidence = _object(data.get("mcpEvidence"), "mcpEvidence")
    allowed = {"hierarchy", "console", "playMode", "screenshots"}
    _only_keys(evidence, allowed, "mcpEvidence")
    missing = sorted(allowed - set(evidence))
    if missing:
        raise EvidenceValidationError(
            f"mcpEvidence is missing required evidence: {', '.join(missing)}"
        )

    screenshots = evidence["screenshots"]
    if not isinstance(screenshots, list) or not screenshots:
        raise EvidenceValidationError("mcpEvidence.screenshots must be a non-empty array")
    validated_screenshots = [
        _validate_capture(item, f"mcpEvidence.screenshots[{index}]", require_tool=True, root=root)
        for index, item in enumerate(screenshots)
    ]
    paths = [item["path"] for item in validated_screenshots]
    if len(paths) != len(set(paths)):
        raise EvidenceValidationError("mcpEvidence.screenshots paths must be unique")

    return {
        "hierarchy": _validate_mcp_operation(evidence["hierarchy"], "mcpEvidence.hierarchy"),
        "console": _validate_mcp_operation(evidence["console"], "mcpEvidence.console"),
        "playMode": _validate_mcp_operation(evidence["playMode"], "mcpEvidence.playMode"),
        "screenshots": validated_screenshots,
    }


def _validate_fallback(data: dict[str, Any], root: Path | None) -> dict[str, Any]:
    if data.get("mcpEvidence") is not None:
        raise EvidenceValidationError("mcpEvidence must be null when MCP is unavailable")
    fallback = _object(data.get("fallback"), "fallback")
    allowed = {"runnerCommand", "logPath", "resultMarker", "screenshots"}
    _only_keys(fallback, allowed, "fallback")
    missing = sorted(allowed - set(fallback))
    if missing:
        raise EvidenceValidationError(f"fallback is missing: {', '.join(missing)}")

    runner = _non_empty_string(fallback["runnerCommand"], "fallback.runnerCommand")
    if "\n" in runner or "\r" in runner:
        raise EvidenceValidationError("fallback.runnerCommand must be one exact shell command")
    log_path_text = _non_empty_string(fallback["logPath"], "fallback.logPath")
    result_marker = _non_empty_string(fallback["resultMarker"], "fallback.resultMarker")
    log_path = Path(log_path_text)
    if root is not None:
        resolved_log = log_path if log_path.is_absolute() else root / log_path
        if not resolved_log.is_file():
            raise EvidenceValidationError(f"fallback.logPath does not exist: {resolved_log}")
        try:
            log_text = resolved_log.read_text(encoding="utf-8-sig")
        except (OSError, UnicodeDecodeError) as exc:
            raise EvidenceValidationError(
                f"fallback.logPath is not a readable text log: {resolved_log}: {exc}"
            ) from exc
        if result_marker not in log_text:
            raise EvidenceValidationError(
                f"fallback.resultMarker is absent from log: {result_marker}"
            )

    screenshots = fallback["screenshots"]
    if not isinstance(screenshots, list) or not screenshots:
        raise EvidenceValidationError("fallback.screenshots must be a non-empty array")
    validated_screenshots = [
        _validate_capture(item, f"fallback.screenshots[{index}]", require_tool=False, root=root)
        for index, item in enumerate(screenshots)
    ]
    paths = [item["path"] for item in validated_screenshots]
    if len(paths) != len(set(paths)):
        raise EvidenceValidationError("fallback.screenshots paths must be unique")

    return {
        "runnerCommand": runner,
        "logPath": log_path_text,
        "resultMarker": result_marker,
        "screenshots": validated_screenshots,
    }


def validate_evidence(data: Any, *, artifact_root: Path | None = None) -> dict[str, Any]:
    report = _object(data, "evidence")
    allowed = {
        "schemaVersion",
        "taskId",
        "subject",
        "revision",
        "mcp",
        "mcpEvidence",
        "fallback",
        "conclusion",
    }
    _only_keys(report, allowed, "evidence")
    missing = sorted(allowed - set(report))
    if missing:
        raise EvidenceValidationError(f"evidence is missing: {', '.join(missing)}")
    if report["schemaVersion"] != SCHEMA_VERSION:
        raise EvidenceValidationError(f"schemaVersion must be {SCHEMA_VERSION}")
    if report["taskId"] != TASK_ID:
        raise EvidenceValidationError(f"taskId must be {TASK_ID}")

    mcp = _object(report["mcp"], "mcp")
    _only_keys(mcp, {"status", "probe", "unavailableReason"}, "mcp")
    status = mcp.get("status")
    if status not in {"connected", "unavailable"}:
        raise EvidenceValidationError("mcp.status must be 'connected' or 'unavailable'")
    probe = _non_empty_string(mcp.get("probe"), "mcp.probe")

    if status == "connected":
        if mcp.get("unavailableReason") is not None:
            raise EvidenceValidationError("mcp.unavailableReason must be null when connected")
        path_evidence = _validate_mcp(report, artifact_root)
        unavailable_reason = None
    else:
        unavailable_reason = _non_empty_string(
            mcp.get("unavailableReason"), "mcp.unavailableReason"
        )
        path_evidence = _validate_fallback(report, artifact_root)

    return {
        "result": "Passed",
        "schemaVersion": SCHEMA_VERSION,
        "taskId": TASK_ID,
        "subject": _non_empty_string(report["subject"], "subject"),
        "revision": _non_empty_string(report["revision"], "revision"),
        "mcp": {
            "status": status,
            "probe": probe,
            "unavailableReason": unavailable_reason,
        },
        "evidencePath": "mcp" if status == "connected" else "fallback",
        "evidence": path_evidence,
        "conclusion": _non_empty_string(report["conclusion"], "conclusion"),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence", type=Path, required=True)
    parser.add_argument("--output-json", type=Path)
    parser.add_argument(
        "--artifact-root",
        type=Path,
        help="When supplied, require the recorded log and screenshots to exist under this root.",
    )
    args = parser.parse_args()

    try:
        payload = json.loads(args.evidence.read_text(encoding="utf-8-sig"))
        summary = validate_evidence(payload, artifact_root=args.artifact_root)
        if args.output_json is not None:
            args.output_json.parent.mkdir(parents=True, exist_ok=True)
            args.output_json.write_text(
                json.dumps(summary, indent=2, sort_keys=True) + "\n",
                encoding="utf-8",
            )
    except (OSError, json.JSONDecodeError, EvidenceValidationError) as exc:
        print(f"[APH-810 EvidenceContract] result=Failed reason={exc}")
        return 1

    print(
        "[APH-810 EvidenceContract] result=Passed "
        f"path={summary['evidencePath']} revision={summary['revision']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

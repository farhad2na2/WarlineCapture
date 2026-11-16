#!/usr/bin/env python3
"""Validate retained r13 S073 native observations for diagnostic use only.

This is a read-only evidence validator. It never creates replay/admission inputs.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path


DEFAULT_EVIDENCE = Path("Logs/SkirmishProduction/s073-native-production-window-20261102-r13-qualified")
PINNED_RECEIPT_SHA256 = "5da37bdf725bce31fe6420d66e65f963e9359454e9463cce13b33128a51e37d1"
EXPECTED_MARKER = (
    "[S073NativeProductionWindow] result=ObservationWindowElapsed "
    "observer_status=NaturalProductionStateObserved"
)
UNKNOWN_DOMAINS = {
    "source_revision": "Unknown_NotPinnedByRetainedReceipt",
    "provider_identity": "Unknown_NotCapturedInStaticBoundsRow",
    "grid_revision": "Unknown_NotCapturedInRetainedRows",
    "rng": "Unknown_random_state_observed_false",
}


def _sha(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def _finite_vector(value, size):
    return (isinstance(value, list) and len(value) == size and
            all(type(item) in (int, float) and math.isfinite(item) for item in value))


def _load_rows(path):
    rows = []
    with path.open("r", encoding="utf-8-sig") as stream:
        for number, line in enumerate(stream, 1):
            if not line.strip():
                continue
            row = json.loads(line)
            if not isinstance(row, dict):
                raise ValueError(f"delivery row {number} must be an object")
            rows.append(row)
    return rows


def validate(evidence_dir, expected_receipt_sha256=PINNED_RECEIPT_SHA256):
    root = Path(evidence_dir).resolve(strict=True)
    receipt_path = root / "qualification-receipt.json"
    receipt_sha256 = _sha(receipt_path) if receipt_path.is_file() else None
    if receipt_sha256 is None or receipt_sha256 != expected_receipt_sha256:
        errors = ["qualification receipt SHA-256 does not match the trusted CLI/test anchor"]
    else:
        errors = []
    result_path = root / "result.json"
    delivery_path = root / "delivery.jsonl"
    log_path = root / "editor.log"
    receipt = json.loads(receipt_path.read_text(encoding="utf-8-sig"))
    result = json.loads(result_path.read_text(encoding="utf-8-sig"))
    if receipt.get("kind") != "S073NativeProductionDeliveryQualificationReceipt" or receipt.get("schema_version") != 1:
        errors.append("unsupported qualification receipt envelope")
    if receipt.get("scope") != "PassiveNativeProductionDeliveryObservation_NotReadinessOrAcceptance":
        errors.append("unexpected qualification scope")
    if receipt.get("wrapper", {}).get("exit_code") != 0:
        errors.append("wrapper exit code is not zero")
    wrapper_exit_basis = receipt.get("wrapper", {}).get("exit_code_basis")
    if wrapper_exit_basis != "r13 wrapper result reported by task owner":
        errors.append("wrapper exit basis is missing or differs from retained receipt")
    if receipt.get("wrapper", {}).get("exact_editor_log_marker") != EXPECTED_MARKER:
        errors.append("receipt wrapper marker mismatch")
    if EXPECTED_MARKER not in log_path.read_text(encoding="utf-8-sig", errors="replace"):
        errors.append("required wrapper completion marker missing from editor log")

    artifact_paths = {"result.json": result_path, "delivery.jsonl": delivery_path, "editor.log": log_path}
    artifact_hashes = {}
    for name, path in artifact_paths.items():
        artifact_record = receipt.get("source_artifacts", {}).get(name, {})
        expected = artifact_record.get("sha256")
        actual = _sha(path) if path.is_file() else None
        artifact_hashes[name] = actual
        if (actual is None or expected != actual or
                artifact_record.get("bytes") != path.stat().st_size):
            errors.append(f"{name} hash mismatch or artifact missing")

    session = receipt.get("session", {})
    sid = session.get("session_id")
    if (session.get("mission"), session.get("scenario_index"), session.get("seed")) != ("S073", 3, 104729):
        errors.append("receipt mission/scenario/seed mismatch")
    if result.get("session_id") != sid or result.get("mission") != "S073" or result.get("scenario_index") != 3 or result.get("seed") != 104729:
        errors.append("result session identity mismatch")

    rows = _load_rows(delivery_path)
    matching = [row for row in rows if row.get("session_id") == sid]
    foreign = [row for row in rows if row.get("session_id") != sid]
    if not rows or foreign:
        errors.append("delivery rows are empty or contain other sessions")
    if any(row.get("phase") != "Playing" or row.get("world_name") != session.get("world_name") for row in matching):
        errors.append("same-session rows do not consistently identify the qualified Playing world")
    audit = receipt.get("row_audit", {})
    if (audit.get("raw_rows") != len(rows) or audit.get("same_session_rows") != len(matching) or
            audit.get("other_session_rows") != len(foreign) or audit.get("malformed_rows") != 0):
        errors.append("receipt row audit does not match retained delivery rows")
    actual_kinds = {}
    for row in matching:
        kind = row.get("kind")
        actual_kinds[kind] = actual_kinds.get(kind, 0) + 1
    if audit.get("row_kinds") != actual_kinds:
        errors.append("receipt row-kind audit does not match retained delivery rows")

    bounds_rows = [row for row in matching if row.get("kind") == "S073NativeProductionStaticRenderBoundsObservation"
                   and row.get("tick_owner") == "ReserveStaticRenderDropBuffers"
                   and row.get("static_render_bounds_event") == "Observed"]
    if len(bounds_rows) != 1:
        errors.append("expected exactly one observed original static-bounds provider call")
        bounds = None
    else:
        bounds = bounds_rows[0]
        if bounds.get("scope") != "ActualOriginalReserveStaticRenderDropBuffersCollectStaticBoundsCall":
            errors.append("static bounds row does not identify the original provider call scope")
        raw_bounds = bounds.get("static_bounds")
        if (not isinstance(raw_bounds, list) or len(raw_bounds) != bounds.get("static_render_bounds_count") or
                not raw_bounds):
            errors.append("static bounds count/list mismatch")
        else:
            for item in raw_bounds:
                if (not isinstance(item, dict) or not _finite_vector(item.get("min"), 3) or
                        not _finite_vector(item.get("max"), 3) or
                        any(item["min"][axis] > item["max"][axis] for axis in range(3))):
                    errors.append("invalid static bounds row")
                    break

    searches = [row for row in matching if row.get("kind") == "S073NativeProductionDropSearchObservation"
                and row.get("drop_search_runtime_building_id") == 6]
    camera_ticks = [row for row in matching if row.get("kind") == "S073NativeProductionDeliveryObservation"
                    and row.get("tick_owner") == "ProcessPendingProductions"
                    and row.get("production_event_present") is True]
    correlated = None
    camera = None
    if bounds is not None:
        correlated = [row for row in searches
                     if row.get("unity_time") == bounds.get("unity_time")
                     and row.get("drop_search_tick_time") == bounds.get("unity_time")]
        if len(correlated) != 1:
            errors.append("static bounds call does not correlate uniquely to a same-time drop search")
        same_time_camera = [row for row in camera_ticks if row.get("unity_time") == bounds.get("unity_time")]
        if len(same_time_camera) != 1:
            errors.append("camera production sample does not correlate uniquely by session/time")
        else:
            camera = same_time_camera[0]
            if not all(_finite_vector(camera.get(field), 3) for field in
                       ("camera_position", "camera_forward", "camera_right")):
                errors.append("same-time camera position/axes are incomplete or non-finite")
    if correlated:
        drop = correlated[0]
        if ((drop.get("drop_search_preferred_cell_x"), drop.get("drop_search_preferred_cell_y")) != (1001, 430) or
                (drop.get("drop_search_grid_width"), drop.get("drop_search_grid_height")) != (2048, 1024) or
                drop.get("drop_search_grid_cell_size") != 1.0 or
                drop.get("drop_search_grid_origin") != [0.0, 0.0, 0.0]):
            errors.append("correlated drop-search grid/preferred-cell facts differ from retained r13 record")
        if bounds.get("static_bounds_area") != [903.0, 332.0, 197.0, 197.0]:
            errors.append("static bounds query area differs from retained r13 record")
    if (session.get("terminal_match_phase") != "Playing" or
            session.get("terminal_match_outcome") != "None" or
            result.get("mission_ready") is not False):
        errors.append("receipt/result no-readiness envelope is missing or inconsistent")

    diagnostic = {
        "kind": "S073NativeR13DiagnosticEvidenceValidation",
        "schema_version": 1,
        "validation_status": "Passed" if not errors else "Failed",
        "scope": "ReadOnlyRetainedEvidenceValidation",
        "use_class": "diagnostic_only",
        "session": {"mission": "S073", "scenario_index": 3, "seed": 104729,
                    "session_id": sid, "world_name": session.get("world_name")},
        "verified_artifact_sha256": artifact_hashes,
        "qualification_receipt_sha256": receipt_sha256,
        "qualification_receipt_expected_sha256": expected_receipt_sha256,
        "wrapper_evidence": {
            "reported_exit_code": receipt.get("wrapper", {}).get("exit_code"),
            "exit_code_basis": wrapper_exit_basis,
            "exit_code_independently_verified": False,
            "completion_marker_found_in_editor_log": EXPECTED_MARKER in
                log_path.read_text(encoding="utf-8-sig", errors="replace"),
        },
        "native_capture": {
            "bounds_call": "ActualOriginalReserveStaticRenderDropBuffersCollectStaticBoundsCall",
            "bounds_time": bounds.get("unity_time") if bounds else None,
            "bounds_count": bounds.get("static_render_bounds_count") if bounds else None,
            "bounds_area": bounds.get("static_bounds_area") if bounds else None,
            "drop_search_time": correlated[0].get("unity_time") if correlated else None,
            "preferred_cell": ([correlated[0].get("drop_search_preferred_cell_x"),
                                 correlated[0].get("drop_search_preferred_cell_y")] if correlated else None),
            "camera_sample_time": camera.get("unity_time") if camera else None,
            "camera_position": camera.get("camera_position") if camera else None,
            "camera_forward": camera.get("camera_forward") if camera else None,
            "camera_right": camera.get("camera_right") if camera else None,
            "random_state_observed": False,
        },
        "unknown_domains": UNKNOWN_DOMAINS,
        "prohibited_claims": ["replay", "admission", "mission_readiness", "source_revision_provenance"],
        "replay": False,
        "admission": False,
        "mission_ready": False,
        "errors": errors,
    }
    return diagnostic


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence", type=Path, default=DEFAULT_EVIDENCE)
    parser.add_argument("--output", type=Path, required=True,
                        help="new JSON output path; must not already exist")
    args = parser.parse_args()
    if args.output.exists():
        parser.error("output path already exists; choose a fresh path")
    result = validate(args.evidence)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps({"validation_status": result["validation_status"],
                      "use_class": result["use_class"], "output": str(args.output.resolve()),
                      "errors": result["errors"]}, sort_keys=True))
    return 0 if result["validation_status"] == "Passed" else 1


if __name__ == "__main__":
    raise SystemExit(main())

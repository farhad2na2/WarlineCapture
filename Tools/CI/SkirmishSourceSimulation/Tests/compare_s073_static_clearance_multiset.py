#!/usr/bin/env python3
"""Compare retained S073 static-clearance bounds as independent diagnostics.

The source-side physical capture and the r13 native observation are separate
historical epochs and sessions. This tool imports no native state and makes no
causal, provider-parity, current-source-equivalence, or readiness claim.
"""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import struct
import sys


SOURCE_CAPTURE = Path("Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json")
PROJECTION = Path("Logs/SkirmishProduction/s073-clearance-projection-input-20261102-r1/static-clearance-from-capture.json")
BRIDGE = Path("Logs/SkirmishProduction/s073-source-clearance-identity-bridge-20261102-r1.json")
NATIVE_EVIDENCE = Path("Logs/SkirmishProduction/s073-native-production-window-20261102-r13-qualified")
SOURCE_CAPTURE_SHA256 = "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8"
PROJECTION_SHA256 = "8b5087c8a96ad81113c93fb4fba1632f10c8920bd2ed16fcb168e58d22572065"
BRIDGE_SHA256 = "08b8431f6481f7ddabe6a044fb7f45d658021dce8380e533feb97dc19f3a4e53"
NATIVE_RECEIPT_SHA256 = "5da37bdf725bce31fe6420d66e65f963e9359454e9463cce13b33128a51e37d1"
NATIVE_DELIVERY_SHA256 = "6e1fcd4c814dacb31e64b9ffce7febb56e98e2f89cd76f36c3980b9e3343c74c"
RENDER_BOUNDS_SOURCE = Path("Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs")
RENDER_BOUNDS_SOURCE_SHA256 = "82bf379f5bb3e5ae55705f608c3e4a26ed915afffeec7488c79b9448cecfb5a4"
CAPTURE_SOURCE = Path("Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073StreamedWorld.cs")
CAPTURE_SOURCE_SHA256 = "f278e4383dfb4ae3ad3f00d7b1da4cedce75625cbdceeaff7bdf60d9d9614346"
AUTHORITY = "OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain"


def sha(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def need(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def _read_json(path: Path):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            need(key not in result, "Duplicate JSON field: " + key)
            result[key] = value
        return result
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique)


def f32(value: float) -> float:
    """Round one operation/result to Unity's IEEE-754 single precision."""
    return struct.unpack("<f", struct.pack("<f", float(value)))[0]


def _vec3(value, label: str) -> tuple[float, float, float]:
    need(isinstance(value, (list, tuple)) and len(value) == 3 and
         all(type(v) in (int, float) and math.isfinite(v) for v in value),
         label + " must be a finite three-vector")
    return tuple(f32(v) for v in value)


def _box_key(minimum, maximum) -> tuple[int, ...]:
    values = (*_vec3(minimum, "bounds.min"), *_vec3(maximum, "bounds.max"))
    need(all(values[i] <= values[i + 3] for i in range(3)), "Bounds min exceeds max")
    return tuple(struct.unpack("<I", struct.pack("<f", value))[0] for value in values)


def _key_values(key: tuple[int, ...]) -> dict:
    values = [struct.unpack("<f", struct.pack("<I", bits))[0] for bits in key]
    return {"min": values[:3], "max": values[3:]}


def _source_box(bounds: dict) -> tuple[tuple[int, ...], tuple[float, ...], tuple[float, ...]]:
    center = _vec3(bounds.get("center"), "source bounds.center")
    extents = _vec3(bounds.get("extents"), "source bounds.extents")
    need(all(value >= 0 for value in extents), "Source bounds extents must be nonnegative")
    # Unity Bounds(min,max) uses center +/- extents, each operation rounded to float.
    minimum = tuple(f32(center[i] - extents[i]) for i in range(3))
    maximum = tuple(f32(center[i] + extents[i]) for i in range(3))
    return _box_key(minimum, maximum), center, extents


def filter_source_bounds(source_bounds: list, area: list, minimum_extent: float,
                         maximum_extent: float) -> tuple[Counter, dict]:
    """Apply original CollectStaticBounds extent and Rect overlap rules."""
    need(isinstance(area, list) and len(area) == 4 and
         all(type(v) in (int, float) and math.isfinite(v) for v in area),
         "Native horizontal area must be finite Rect(x,y,width,height)")
    x_min, y_min, width, height = (f32(v) for v in area)
    x_max, y_max = f32(x_min + width), f32(y_min + height)
    min_extent, max_extent = f32(minimum_extent), f32(maximum_extent)
    need(width >= 0 and height >= 0 and min_extent >= 0 and max_extent >= min_extent,
         "Invalid native area or extent limits")
    selected = Counter()
    accepted = 0
    for bounds in source_bounds:
        key, center, extents = _source_box(bounds)
        extent = max(extents[0], extents[2])
        if extent < min_extent or extent > max_extent:
            continue
        # Mirror Rect(center.x-ext.x, center.z-ext.z, ext.x*2, ext.z*2),
        # including single-precision width and xMax/yMax arithmetic.
        bx_min = f32(center[0] - extents[0])
        bz_min = f32(center[2] - extents[2])
        bx_width = f32(extents[0] * f32(2.0))
        bz_height = f32(extents[2] * f32(2.0))
        bx_max, bz_max = f32(bx_min + bx_width), f32(bz_min + bz_height)
        if (bx_max < x_min or bz_max < y_min or bx_min >= x_max or bz_min >= y_max):
            continue
        selected[key] += 1
        accepted += 1
    return selected, {"input_bounds": len(source_bounds), "selected_bounds": accepted,
                      "area_rect_xywh": [x_min, y_min, width, height],
                      "area_rect_max_xy": [x_max, y_max],
                      "minimum_extent": min_extent, "maximum_extent": max_extent}


def _native_multiset(bounds: list) -> Counter:
    need(isinstance(bounds, list) and bounds, "Native static_bounds must be a nonempty list")
    result = Counter()
    for item in bounds:
        need(isinstance(item, dict), "Native bound must be an object")
        result[_box_key(item.get("min"), item.get("max"))] += 1
    return result


def compare_multisets(source: Counter, native: Counter, example_limit: int = 24) -> dict:
    need(0 <= example_limit <= 128, "example limit must be 0..128")
    source_only = source - native
    native_only = native - source
    duplicates_source = {key: count for key, count in source.items() if count > 1}
    duplicates_native = {key: count for key, count in native.items() if count > 1}

    def examples(counter):
        return [{**_key_values(key), "count": count} for key, count in
                sorted(counter.items(), key=lambda pair: pair[0])[:example_limit]]

    return {
        "status": "ExactMultisetMatch" if not source_only and not native_only else "MultisetDifferencesObserved",
        "source_count": sum(source.values()), "source_unique_bounds": len(source),
        "native_count": sum(native.values()), "native_unique_bounds": len(native),
        "source_duplicate_key_count": len(duplicates_source),
        "source_duplicate_excess_count": sum(value - 1 for value in duplicates_source.values()),
        "native_duplicate_key_count": len(duplicates_native),
        "native_duplicate_excess_count": sum(value - 1 for value in duplicates_native.values()),
        "source_only_bound_key_count": len(source_only),
        "source_only_excess_count": sum(source_only.values()),
        "native_only_bound_key_count": len(native_only),
        "native_only_excess_count": sum(native_only.values()),
        "source_only_examples": examples(source_only), "native_only_examples": examples(native_only),
        "source_duplicate_examples": examples(duplicates_source),
        "native_duplicate_examples": examples(duplicates_native),
    }


def _r13_validator(repo: Path, evidence_dir: Path) -> dict:
    validator_path = repo / "Tools/CI/SkirmishSourceSimulation/Tests/validate_s073_native_r13_diagnostic.py"
    spec = importlib.util.spec_from_file_location("validate_s073_native_r13_diagnostic", validator_path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    result = module.validate(evidence_dir, NATIVE_RECEIPT_SHA256)
    need(result.get("validation_status") == "Passed" and result.get("qualification_receipt_sha256") == NATIVE_RECEIPT_SHA256,
         "Retained r13 native diagnostic receipt validation failed: " + "; ".join(result.get("errors", [])))
    return result


def compare(repo: Path, example_limit: int = 24) -> dict:
    repo = repo.resolve(strict=True)
    source_capture_path = repo / SOURCE_CAPTURE
    projection_path = repo / PROJECTION
    bridge_path = repo / BRIDGE
    need(sha(source_capture_path) == SOURCE_CAPTURE_SHA256, "Full physical source capture hash mismatch")
    need(sha(projection_path) == PROJECTION_SHA256, "Full source clearance projection hash mismatch")
    need(sha(bridge_path) == BRIDGE_SHA256, "Source identity bridge hash mismatch")
    render_source = repo / RENDER_BOUNDS_SOURCE
    capture_source = repo / CAPTURE_SOURCE
    need(sha(render_source) == RENDER_BOUNDS_SOURCE_SHA256, "Original RenderBoundsPresentation source pin mismatch")
    need(sha(capture_source) == CAPTURE_SOURCE_SHA256, "Source capture producer source pin mismatch")

    physical = _read_json(source_capture_path)
    projection = _read_json(projection_path)
    bridge = _read_json(bridge_path)
    need(physical.get("kind") == "PinnedCampaignPhysicalInputExport" and
         physical.get("result") == "Collected" and physical.get("worldDisposed") is True and
         physical.get("missionInitialized") is False and physical.get("missionReady") is False and
         physical.get("sourceFitCertified") is False and physical.get("operationMapId") == "opmap.skirmish.desert_base_01" and
         physical.get("sourcePath") == "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset" and
         physical.get("sourceSha256") == "8a03d36fd1d4d59a180c65d82f1c708fd70e7c291f49d480c80629a80ec728c0",
         "Exact historical physical source capture identity/scope required")
    physical_map_path = repo / physical["sourcePath"]
    need(sha(physical_map_path) == physical["sourceSha256"],
         "Current source-map asset no longer matches the captured source identity pin")
    source_record = physical.get("fixedMapReadDomain", {}).get("dropClearanceBounds")
    projection_record = projection.get("source_record")
    need(projection.get("kind") == "GeneratedSourceStaticClearanceProjection" and
         projection.get("source_sha256") == SOURCE_CAPTURE_SHA256 and
         projection.get("source_path") == str(source_capture_path) and
         projection.get("scope") == "CurrentS002SourceOnlyInactiveFixture_NoActiveProviderEquivalence" and
         projection.get("source_fit_certified") is False and projection.get("mission_ready") is False,
         "Exact source projection envelope and capture binding required")
    need(source_record == projection_record and source_record.get("authority") == AUTHORITY and
         source_record.get("authored_transform_projection_count") == 124499 and
         source_record.get("grid_width") == 2048 and source_record.get("grid_height") == 1024 and
         source_record.get("grid_origin") == [0.0, 0.0, 0.0] and source_record.get("cell_size") == 1.0 and
         source_record.get("query_rect") == [-118.0, -118.0, 2284.0, 1260.0] and
         source_record.get("minimum_extent") == 0.25 and source_record.get("maximum_extent") == 20.0 and
         source_record.get("runtime_presentation_activation_sampled") is False and
         len(source_record.get("bounds", [])) == 90865,
         "Full source static-clearance record mismatch")
    need(bridge.get("kind") == "S073SourceClearanceIdentityBridge" and bridge.get("status") == "IdentityBound" and
         bridge.get("source_geometry", {}).get("source_capture_sha256") == SOURCE_CAPTURE_SHA256 and
         bridge.get("source_geometry", {}).get("source_map_id") == "opmap.skirmish.desert_base_01" and
         bridge.get("s073_source_binding", {}).get("logical_map_id") == "opmap.skirmish.industrial_basin" and
         bridge.get("s073_source_binding", {}).get("source_map_id") == "opmap.skirmish.desert_base_01" and
         bridge.get("s073_source_binding", {}).get("streamed_capture_sha256") ==
             "1f375a666f474a73d2aaac5c0b1930a774b117349a1e074f618ecbbd57b01bd7" and
         bridge.get("evidence", {}).get("physical_capture", {}).get("sha256") == SOURCE_CAPTURE_SHA256 and
         bridge.get("evidence", {}).get("provider_implementation", {}).get("sha256") == RENDER_BOUNDS_SOURCE_SHA256 and
         bridge.get("evidence", {}).get("capture_implementation", {}).get("sha256") == CAPTURE_SOURCE_SHA256 and
         bridge.get("provider_parity_established") is False and
         bridge.get("historical_capture_dependency_pins_revalidated") is False and
         bridge.get("source_capture_current_producer_equivalence") is False and
         bridge.get("mission_ready") is False and bridge.get("source_fit_certified") is False,
         "Existing source identity bridge pin/scope mismatch")

    native_validation = _r13_validator(repo, repo / NATIVE_EVIDENCE)
    receipt_path = repo / NATIVE_EVIDENCE / "qualification-receipt.json"
    delivery_path = repo / NATIVE_EVIDENCE / "delivery.jsonl"
    need(sha(receipt_path) == NATIVE_RECEIPT_SHA256 and sha(delivery_path) == NATIVE_DELIVERY_SHA256,
         "Pinned r13 native receipt/delivery artifact hash mismatch")
    receipt = _read_json(receipt_path)
    need(receipt.get("session", {}).get("mission") == "S073" and
         receipt.get("session", {}).get("scenario_index") == 3 and receipt.get("session", {}).get("seed") == 104729 and
         receipt.get("session", {}).get("session_id") == native_validation["session"]["session_id"] and
         receipt.get("scope") == "PassiveNativeProductionDeliveryObservation_NotReadinessOrAcceptance",
         "r13 native session identity/scope mismatch")
    rows = [json.loads(line) for line in delivery_path.read_text(encoding="utf-8-sig").splitlines() if line.strip()]
    sid = receipt["session"]["session_id"]
    matches = [row for row in rows if row.get("kind") == "S073NativeProductionStaticRenderBoundsObservation" and row.get("session_id") == sid]
    need(len(matches) == 1, "Expected one exact same-session original static-bounds observation")
    native_row = matches[0]
    need(native_row.get("scope") == "ActualOriginalReserveStaticRenderDropBuffersCollectStaticBoundsCall" and
         native_row.get("tick_owner") == "ReserveStaticRenderDropBuffers" and native_row.get("phase") == "Playing" and
         native_row.get("static_render_bounds_event") == "Observed" and
         native_row.get("static_render_bounds_count") == len(native_row.get("static_bounds", [])) and
         native_row.get("unity_time") == native_validation["native_capture"]["bounds_time"] and
         native_row.get("static_bounds_area") == native_validation["native_capture"]["bounds_area"] and
         native_row.get("static_bounds_min_extent") == 0.25 and native_row.get("static_bounds_max_extent") == 20.0,
         "Original r13 static-bounds observation differs from validated row")

    source_multiset, filter_record = filter_source_bounds(
        source_record["bounds"], native_row["static_bounds_area"],
        native_row["static_bounds_min_extent"], native_row["static_bounds_max_extent"])
    native_multiset = _native_multiset(native_row["static_bounds"])
    comparison = compare_multisets(source_multiset, native_multiset, example_limit)
    return {
        "kind": "S073StaticClearanceBoundMultisetDiagnostic",
        "schema_version": 1,
        "status": comparison["status"],
        "scope": "OfflineMultisetComparison_IndependentHistoricalSourceCaptureAndNativeObservation",
        "source_capture": {"path": SOURCE_CAPTURE.as_posix(), "sha256": SOURCE_CAPTURE_SHA256,
            "source_map_id": physical["operationMapId"], "source_map_asset_sha256": physical["sourceSha256"],
            "bounds_in_pinned_capture": len(source_record["bounds"])},
        "source_projection": {"path": PROJECTION.as_posix(), "sha256": PROJECTION_SHA256,
            "capture_sha256": projection["source_sha256"], "authority": source_record["authority"],
            "provider_source_sha256": RENDER_BOUNDS_SOURCE_SHA256,
            "capture_implementation_sha256": CAPTURE_SOURCE_SHA256},
        "source_identity_bridge": {"path": BRIDGE.as_posix(), "sha256": BRIDGE_SHA256,
            "status": bridge["status"], "provider_parity_established": False,
            "historical_capture_dependency_pins_revalidated": False,
            "source_capture_current_producer_equivalence": False},
        "native_observation": {"evidence_dir": NATIVE_EVIDENCE.as_posix(),
            "qualification_receipt_sha256": NATIVE_RECEIPT_SHA256,
            "delivery_sha256": NATIVE_DELIVERY_SHA256,
            "session_id": sid, "world_name": native_row["world_name"],
            "scenario_index": 3, "seed": 104729, "unity_time": native_row["unity_time"],
            "bounds_call_scope": native_row["scope"], "bounds_count": native_row["static_render_bounds_count"],
            "diagnostic_validation_status": native_validation["validation_status"]},
        "filter": {"source_rules": "RenderBoundsPresentation.CollectStaticBounds extent and Rect overlap inequalities",
            "arithmetic": "IEEE-754 float32 at each center/extents, width, and Rect max operation",
            **filter_record},
        "multiset": comparison,
        "sessions_and_epochs": {"same_session": False, "same_capture_epoch": False,
            "source_capture_epoch": "2026-11-01 retained physical-source capture",
            "native_observation_epoch": "2026-11-02 r13 retained Playing-world observation",
            "known_temporal_limit": "r13 native observation predates later S073 terrain/placement edits; source capture is a separate historical epoch"},
        "claims": {"native_state_imported": False, "causal_claim": False, "provider_parity_claim": False,
            "current_source_equivalence_claim": False, "readiness_claim": False,
            "source_fit_certified": False, "mission_ready": False},
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[4])
    parser.add_argument("--output", type=Path, required=True, help="Fresh output JSON path; existing files are never overwritten")
    parser.add_argument("--example-limit", type=int, default=24)
    args = parser.parse_args()
    need(not args.output.exists(), "Fresh output path required")
    result = compare(args.repo, args.example_limit)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("x", encoding="utf-8") as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write("\n")
    print(json.dumps({"status": result["status"], "source_count": result["multiset"]["source_count"],
        "native_count": result["multiset"]["native_count"], "output": str(args.output.resolve()),
        "mission_ready": False}, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

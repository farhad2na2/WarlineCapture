#!/usr/bin/env python3
"""Compare separate S073 blocker/occupancy captures; never infer parity or readiness."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import struct
from pathlib import Path

MAGIC = b"S073BOC1"
KIND = "S073BlockerOccupancyRawSnapshot"
MAX_CELLS = 2_097_152


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _verify_file_pin(value: dict, label: str) -> Path:
    need(isinstance(value, dict) and isinstance(value.get("path"), str) and
         isinstance(value.get("sha256"), str), f"{label} descriptor required")
    path = Path(value["path"]).resolve(strict=True)
    need(sha(path) == value["sha256"], f"{label} hash mismatch")
    return path


def load_capture(raw_path: Path, receipt_path: Path, producer: str) -> tuple[dict, dict]:
    receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
    raw = raw_path.read_bytes()
    need(receipt.get("kind") == KIND and receipt.get("schema_version") == 1 and
         (receipt.get("status") == "Captured" if producer == "source" else True) and
         receipt.get("producer") == producer,
         f"Exact {producer} raw capture receipt required")
    need(receipt.get("raw_sha256") == hashlib.sha256(raw).hexdigest() and
         receipt.get("byte_length") == len(raw), f"{producer} raw capture hash/length mismatch")
    need(receipt.get("mission") == "S073" and receipt.get("seed") == 104729,
         f"{producer} S073 mission/seed provenance required")
    if producer == "source":
        need(receipt.get("scheduler_admitted") is False and receipt.get("mission_ready") is False and
             receipt.get("scope") == "IndependentSourceHostRawBlockerOccupancyCapture_NoNativeStateImport_NoAdmission",
             "Source capture must explicitly disclaim admission/readiness and use the reviewed scope")
    else:
        need(receipt.get("source_host_state_transferred") is False and
             receipt.get("campaign_state_transferred") is False and receipt.get("readiness_claimed") is False,
             "Native capture must explicitly disclaim state transfer and readiness")
    need(len(raw) >= 48 and raw[:8] == MAGIC, f"{producer} raw format magic/version mismatch")
    width, height, cell_size, ox, oy, oz, epoch, applied_epoch, revision, cells = struct.unpack_from("<IIffffQQQI", raw, 8)
    need(0 < width <= MAX_CELLS and 0 < height <= MAX_CELLS and width * height == cells <= MAX_CELLS,
         f"{producer} dimensions/cell count invalid")
    need(all(math.isfinite(x) for x in (cell_size, ox, oy, oz)) and cell_size > 0,
         f"{producer} grid frame invalid")
    bit_bytes = (cells + 7) // 8
    expected_length = 60 + bit_bytes + cells * 4 + cells + bit_bytes
    need(len(raw) == expected_length, f"{producer} raw payload length/schema mismatch")
    expected_header = (width, height, cell_size, ox, oy, oz, str(epoch), str(applied_epoch), str(revision))
    actual_header = (receipt.get("width"), receipt.get("height"), receipt.get("cell_size"),
                     *(receipt.get("origin") or []), receipt.get("storage_epoch"),
                     receipt.get("applied_storage_epoch"), receipt.get("applied_revision"))
    need(actual_header == expected_header, f"{producer} receipt/header mismatch")
    pos = 60
    blocked = raw[pos:pos + bit_bytes]; pos += bit_bytes
    if cells & 7:
        need((blocked[-1] >> (cells & 7)) == 0, f"{producer} blocked bitset padding must be zero")
    counts = struct.unpack_from(f"<{cells}i", raw, pos); pos += cells * 4
    need(all(value >= 0 for value in counts), f"{producer} blocker counts cannot be negative")
    friendly = raw[pos:pos + cells]; pos += cells
    occupied = raw[pos:pos + bit_bytes]
    if cells & 7:
        need((occupied[-1] >> (cells & 7)) == 0, f"{producer} occupancy bitset padding must be zero")
    for i, count in enumerate(counts):
        need(bool(blocked[i >> 3] & (1 << (i & 7))) == (count > 0),
             f"{producer} blocked mask/count inconsistency at cell {i}")
    if producer == "source":
        need(receipt.get("source_campaign_state_transferred") is False,
             "Source capture must disclaim Campaign state transfer")
    need(receipt.get("raw_path") is not None, f"{producer} raw path missing")
    need(Path(receipt["raw_path"]).resolve(strict=True) == raw_path.resolve(strict=True),
         f"{producer} raw path differs from receipt")
    if producer == "source":
        for name in ("source_build_receipt", "source_executable", "run_request", "bundle", "source_graph"):
            _verify_file_pin(receipt.get(name), f"Source {name}")
        request_path = Path(receipt["run_request"]["path"])
        request = json.loads(request_path.read_text(encoding="utf-8"))
        def same_pin(left, right):
            return isinstance(left, dict) and isinstance(right, dict) and left.get("path") == right.get("path") and left.get("sha256") == right.get("sha256")
        need(request.get("kind") == "SkirmishSourceSimulationRun" and request.get("mission") == "S073" and
             request.get("seed") == 104729 and same_pin(request.get("source_build_receipt"), receipt["source_build_receipt"]) and
             same_pin(request.get("source_executable"), receipt["source_executable"]),
             "Source run request does not bind this raw capture build")
    return receipt, {"width": width, "height": height, "cell_size": cell_size, "origin": (ox, oy, oz),
                    "epoch": epoch, "applied_epoch": applied_epoch, "revision": revision,
                    "blocked": blocked, "counts": counts, "friendly": friendly, "occupied": occupied}


def need(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def bit(data: bytes, index: int) -> int:
    return (data[index >> 3] >> (index & 7)) & 1


def compare(native_raw: Path, native_receipt_path: Path, source_raw: Path, source_receipt_path: Path,
            native_wrapper_receipt_path: Path, native_snapshot_path: Path,
            source_run_receipt_path: Path, max_coordinates: int = 64) -> dict:
    need(0 <= max_coordinates <= 256, "Bounded mismatch coordinate limit must be 0..256")
    native_receipt, n = load_capture(native_raw, native_receipt_path, "native")
    source_receipt, s = load_capture(source_raw, source_receipt_path, "source")
    wrapper = json.loads(native_wrapper_receipt_path.read_text(encoding="utf-8"))
    snapshot = json.loads(native_snapshot_path.read_text(encoding="utf-8"))
    editor_log_path = native_wrapper_receipt_path.parent / "editor.log"
    if wrapper.get("kind") == "S073NativeWaterMaskCaptureReceipt":
        need(wrapper.get("validation_status") == "Passed" and wrapper.get("wrapper_exit_code") == 0 and
             wrapper.get("mission_ready") is False and wrapper.get("observation_sha256") == sha(native_snapshot_path) and
             editor_log_path.is_file() and wrapper.get("editor_log_sha256") == sha(editor_log_path),
             "Native water/blocker receipt, snapshot or editor log provenance mismatch")
        need(snapshot.get("kind") == "S073NativeWaterMaskObservation" and
             snapshot.get("scope") == "SameSessionOriginalSkirmishWaterPlacementCreateMask_NotReadinessOrMissionAcceptance" and
             snapshot.get("phase") == "Playing" and snapshot.get("seed") == 104729 and
             snapshot.get("scenario_index") == 3 and snapshot.get("session_id") == wrapper.get("session_id") and
             snapshot.get("mission_ready") is False and snapshot.get("player_readiness") is False,
             "Exact passive native Playing S073 snapshot identity required")
        native_observation_name = "playing_passive_input_observation"
    else:
        need(wrapper.get("status") == "Recorded" and wrapper.get("wrapper_exit") == 0 and
             wrapper.get("mission_ready") is False and wrapper.get("snapshot_sha256") == sha(native_snapshot_path) and
             editor_log_path.is_file() and wrapper.get("editor_log_sha256") == sha(editor_log_path),
             "Native wrapper receipt, snapshot or editor log provenance mismatch")
        need(wrapper.get("scope") == "IndependentInstrumentedNativeCapture_NotNormalInputOrReadiness",
             "Native wrapper receipt scope mismatch")
        need(snapshot.get("kind") == "S073NativePathfindingCommandDiagnostic" and
             snapshot.get("schema_version") == 1 and snapshot.get("seed") == 104729 and
             snapshot.get("scenario_index") == 3 and snapshot.get("session_id") == wrapper.get("session_id") and
             snapshot.get("mission_ready") is not True and snapshot.get("player_readiness") is not True,
             "Exact unqualified native S073 snapshot identity required")
        native_observation_name = native_receipt.get("observation")
    native_observation = snapshot.get(native_receipt.get("observation"))
    if native_receipt.get("observation") != native_observation_name:
        native_observation = snapshot.get(native_observation_name)
    need(isinstance(native_observation, dict) and
         native_observation.get("raw_blocker_occupancy_snapshot") == native_receipt,
         "Native raw capture sidecar does not match its named snapshot observation")
    source_run_receipt = json.loads(source_run_receipt_path.read_text(encoding="utf-8"))
    worker = source_run_receipt.get("worker_result") or {}
    worker_path = _verify_file_pin(worker, "Source worker result")
    need(source_run_receipt.get("mission") == "S073" and source_run_receipt.get("seed") == 104729 and
         source_run_receipt.get("status") == "PreparingPhysicalSourceRequired" and
         source_run_receipt.get("process_exit") == 2 and source_run_receipt.get("validation_passed") is False and
         source_run_receipt.get("full_outcome_observed") is False,
         "Expected source diagnostic physical-source gate receipt required")
    worker_json = json.loads(worker_path.read_text(encoding="utf-8"))
    raw_descriptor = worker_json.get("source_blocker_occupancy_raw_capture") or {}
    need(raw_descriptor.get("path") == str(source_raw.resolve()) and
         raw_descriptor.get("sha256") == source_receipt["raw_sha256"] and
         raw_descriptor.get("receipt_path") == str(source_receipt_path.resolve()) and
         raw_descriptor.get("receipt_sha256") == sha(source_receipt_path),
         "Source worker result does not bind the raw payload and sidecar hashes")
    need(source_receipt.get("source_map_id") == native_receipt.get("source_map_id") and
         source_receipt.get("source_campaign_map_id") == native_receipt.get("source_campaign_map_id"),
         "Source and native captures have different selected map identities")
    need(source_receipt.get("source_session_id") != native_receipt.get("native_session_id"),
         "Native and source captures must remain independent sessions")
    if wrapper.get("kind") == "S073NativeWaterMaskCaptureReceipt":
        need(source_receipt.get("source_identity_hash") == native_receipt.get("source_identity_hash") and
             source_receipt.get("source_content_hash") == native_receipt.get("source_content_hash"),
             "Passive native and source captures have different pinned Campaign source identities")
    need((n["width"], n["height"], n["cell_size"], n["origin"]) ==
         (s["width"], s["height"], s["cell_size"], s["origin"]),
         "Native and source captures use different grid frames")
    cells = n["width"] * n["height"]
    mismatch_counts = {name: 0 for name in ("blocked", "counts", "friendly_pass_faction", "occupied")}
    samples = []
    native_blocked = source_blocked = native_occupied = source_occupied = 0
    count_sum_native = count_sum_source = 0
    count_max_native = count_max_source = 0
    for i in range(cells):
        nb, sb = bit(n["blocked"], i), bit(s["blocked"], i)
        no, so = bit(n["occupied"], i), bit(s["occupied"], i)
        nc, sc = n["counts"][i], s["counts"][i]
        nf, sf = n["friendly"][i], s["friendly"][i]
        native_blocked += nb; source_blocked += sb; native_occupied += no; source_occupied += so
        count_sum_native += nc; count_sum_source += sc
        count_max_native = max(count_max_native, nc); count_max_source = max(count_max_source, sc)
        differs = False
        for name, left, right in (("blocked", nb, sb), ("counts", nc, sc),
                                  ("friendly_pass_faction", nf, sf), ("occupied", no, so)):
            if left != right:
                mismatch_counts[name] += 1; differs = True
        if differs and len(samples) < max_coordinates:
            samples.append({"index": i, "x": i % n["width"], "y": i // n["width"],
                            "native": {"blocked": nb, "count": nc, "friendly_pass_faction": nf, "occupied": no},
                            "source": {"blocked": sb, "count": sc, "friendly_pass_faction": sf, "occupied": so}})
    return {
        "kind": "S073BlockerOccupancyRawComparison", "schema_version": 1,
        "status": "DifferencesObserved" if any(mismatch_counts.values()) else "NoDifferencesObserved",
        "native_capture": {"raw_sha256": native_receipt["raw_sha256"], "receipt_sha256": sha(native_receipt_path),
                           "observation": native_receipt["observation"],
                           "native_session_id": native_receipt["native_session_id"]},
        "source_capture": {"raw_sha256": source_receipt["raw_sha256"], "receipt_sha256": sha(source_receipt_path),
                           "observation": source_receipt["observation"],
                           "source_session_id": source_receipt["source_session_id"]},
        "sessions_are_independent": True,
        "source_run_receipt_sha256": sha(source_run_receipt_path),
        "grid": {"width": n["width"], "height": n["height"], "cell_size": n["cell_size"], "origin": list(n["origin"])},
        "native_metrics": {"blocked_cells": native_blocked, "blocker_count_sum": count_sum_native,
                           "blocker_count_max": count_max_native, "occupied_cells": native_occupied},
        "source_metrics": {"blocked_cells": source_blocked, "blocker_count_sum": count_sum_source,
                           "blocker_count_max": count_max_source, "occupied_cells": source_occupied},
        "mismatching_cells_by_field": mismatch_counts,
        "mismatch_coordinate_limit": max_coordinates, "first_mismatch_coordinates": samples,
        "scope": "Independent captured native/source arrays compared offline; captures may differ by world and observation time",
        "state_transfer": False, "parity_claim": False, "scheduler_admitted": False, "mission_ready": False,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--native-raw", type=Path, required=True)
    parser.add_argument("--native-receipt", type=Path, required=True)
    parser.add_argument("--native-wrapper-receipt", type=Path, required=True)
    parser.add_argument("--native-snapshot", type=Path, required=True)
    parser.add_argument("--source-raw", type=Path, required=True)
    parser.add_argument("--source-receipt", type=Path, required=True)
    parser.add_argument("--source-run-receipt", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--max-coordinates", type=int, default=64)
    args = parser.parse_args()
    result = compare(args.native_raw, args.native_receipt, args.source_raw, args.source_receipt,
                     args.native_wrapper_receipt, args.native_snapshot, args.source_run_receipt, args.max_coordinates)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": result["status"], "mismatching_cells_by_field": result["mismatching_cells_by_field"],
                      "scheduler_admitted": False, "mission_ready": False}))


if __name__ == "__main__":
    main()

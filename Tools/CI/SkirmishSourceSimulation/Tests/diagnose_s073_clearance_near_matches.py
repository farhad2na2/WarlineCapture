#!/usr/bin/env python3
"""Strict pinned near-match diagnostic for historical S073 clearance boxes.

This invokes the exact multiset comparator's provenance checks first. It does
not alter that exact result and does not establish parity, causation, current
source equivalence, or readiness.
"""
from __future__ import annotations

import argparse
from collections import Counter
import importlib.util
import json
import math
from pathlib import Path
import struct


TOLERANCE_METERS = 1e-4
EXACT_COMPARATOR_PATH = Path(__file__).with_name("compare_s073_static_clearance_multiset.py")


def _load_exact_comparator():
    spec = importlib.util.spec_from_file_location("s073_exact_static_clearance_comparator", EXACT_COMPARATOR_PATH)
    if spec is None or spec.loader is None:
        raise RuntimeError("Existing exact comparator could not be loaded")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def key_values(key: tuple[int, ...]) -> tuple[float, ...]:
    return tuple(struct.unpack("<f", struct.pack("<I", bits))[0] for bits in key)


def greedy_near_match(source_only: Counter, native_only: Counter,
                      tolerance_m: float = TOLERANCE_METERS) -> dict:
    """Consume duplicate occurrences with a stable nearest-first greedy rule.

    Keys contain float32 bit patterns for minX,minY,minZ,maxX,maxY,maxZ.
    Native keys are visited lexicographically; each consumes the available
    source key with minimum L-infinity distance, breaking ties by source key.
    This is deterministic and duplicate-aware, but not maximum-cardinality.
    """
    if not isinstance(tolerance_m, (int, float)) or not math.isfinite(tolerance_m) or tolerance_m < 0:
        raise ValueError("Finite nonnegative tolerance required")
    remaining = Counter(source_only)
    need_positive = lambda counter: all(type(count) is int and count >= 0 for count in counter.values())
    if not need_positive(remaining) or not need_positive(native_only):
        raise ValueError("Multiset counts must be nonnegative integers")
    pairs = []
    unmatched_native = Counter()
    for native_key, native_count in sorted(native_only.items()):
        native_values = key_values(native_key)
        for _ in range(native_count):
            candidates = []
            for source_key, available in remaining.items():
                if available <= 0:
                    continue
                source_values = key_values(source_key)
                deltas = tuple(abs(left - right) for left, right in zip(native_values, source_values))
                distance = max(deltas)
                if distance <= tolerance_m:
                    candidates.append((distance, source_key, deltas))
            if not candidates:
                unmatched_native[native_key] += 1
                continue
            distance, source_key, deltas = min(candidates, key=lambda item: (item[0], item[1]))
            remaining[source_key] -= 1
            pairs.append({"native_key": native_key, "source_key": source_key,
                          "max_abs_delta_m": distance, "component_abs_delta_m": deltas})
    remaining_source = Counter({key: count for key, count in remaining.items() if count > 0})
    return {"pairs": pairs, "unmatched_native": unmatched_native,
            "remaining_source_only": remaining_source}


def _counter_examples(counter: Counter, exact, limit: int) -> list:
    return [{**exact._key_values(key), "count": count}
            for key, count in sorted(counter.items())[:limit]]


def analyze(repo: Path, example_limit: int = 12) -> dict:
    if not 0 <= example_limit <= 64:
        raise ValueError("example limit must be 0..64")
    exact = _load_exact_comparator()
    # This is the authoritative provenance gate: full source capture/projection,
    # bridge, current source pins, and retained r13 receipt/session/artifact pins.
    exact_result = exact.compare(repo, example_limit=0)
    root = Path(repo).resolve(strict=True)
    source_capture = exact._read_json(root / exact.SOURCE_CAPTURE)
    rows = [json.loads(line) for line in
            (root / exact.NATIVE_EVIDENCE / "delivery.jsonl").read_text(encoding="utf-8-sig").splitlines()
            if line.strip()]
    receipt = exact._read_json(root / exact.NATIVE_EVIDENCE / "qualification-receipt.json")
    session_id = receipt["session"]["session_id"]
    native_row = next(row for row in rows if
        row.get("kind") == "S073NativeProductionStaticRenderBoundsObservation" and
        row.get("session_id") == session_id)
    source_record = source_capture["fixedMapReadDomain"]["dropClearanceBounds"]
    source_filtered, _ = exact.filter_source_bounds(source_record["bounds"],
        native_row["static_bounds_area"], native_row["static_bounds_min_extent"],
        native_row["static_bounds_max_extent"])
    native_filtered = exact._native_multiset(native_row["static_bounds"])
    source_only = source_filtered - native_filtered
    native_only = native_filtered - source_filtered
    matching = greedy_near_match(source_only, native_only)
    pairs = matching["pairs"]
    max_delta = max((pair["max_abs_delta_m"] for pair in pairs), default=0.0)
    return {
        "kind": "S073ClearanceNearMatchDiagnostic",
        "schema_version": 1,
        "status": "NearMatchesObserved" if pairs else "NoNearMatchesObserved",
        "scope": "ReadOnlyApproximateBoxMatching_AfterPinnedExactMultisetComparison",
        "exact_comparison": {
            "status": exact_result["status"],
            "source_count": exact_result["multiset"]["source_count"],
            "native_count": exact_result["multiset"]["native_count"],
            "source_only_excess_count": exact_result["multiset"]["source_only_excess_count"],
            "native_only_excess_count": exact_result["multiset"]["native_only_excess_count"],
            "source_capture_sha256": exact_result["source_capture"]["sha256"],
            "source_projection_sha256": exact_result["source_projection"]["sha256"],
            "native_qualification_receipt_sha256": exact_result["native_observation"]["qualification_receipt_sha256"],
            "native_delivery_sha256": exact_result["native_observation"]["delivery_sha256"],
            "native_session_id": session_id,
        },
        "near_match_policy": {
            "tolerance_meters": TOLERANCE_METERS,
            "coordinate_distance": "L-infinity maximum absolute delta across minX,minY,minZ,maxX,maxY,maxZ",
            "inclusive_threshold": True,
            "duplicate_aware": True,
            "assignment": "Visit native-only float32 keys lexicographically; choose the available source-only occurrence with the smallest L-infinity distance, tie-break by source key; consume one occurrence per match.",
            "greedy_caveat": "Deterministic greedy matching is not maximum-cardinality bipartite matching; another assignment algorithm can change the near-match count.",
        },
        "near_match_counts": {
            "native_only_excess_matched": len(pairs),
            "native_only_excess_unmatched": sum(matching["unmatched_native"].values()),
            "source_only_excess_consumed": len(pairs),
            "source_only_excess_remaining": sum(matching["remaining_source_only"].values()),
            "source_only_distinct_keys_remaining": len(matching["remaining_source_only"]),
            "maximum_matched_component_delta_m": max_delta,
            "matched_max_delta_le_1e_6_m": sum(pair["max_abs_delta_m"] <= 1e-6 for pair in pairs),
            "matched_max_delta_le_1e_5_m": sum(pair["max_abs_delta_m"] <= 1e-5 for pair in pairs),
        },
        "matched_examples": [
            {"native": exact._key_values(pair["native_key"]),
             "source": exact._key_values(pair["source_key"]),
             "max_abs_delta_m": pair["max_abs_delta_m"],
             "component_abs_delta_m": list(pair["component_abs_delta_m"])}
            for pair in pairs[:example_limit]],
        "unmatched_native_examples": _counter_examples(matching["unmatched_native"], exact, example_limit),
        "remaining_source_only_examples": _counter_examples(matching["remaining_source_only"], exact, example_limit),
        "epochs_and_claims": {
            "same_session": False,
            "source_capture_epoch": "2026-11-01 retained physical-source capture",
            "native_observation_epoch": "2026-11-02 r13 retained Playing-world observation",
            "known_temporal_limit": "r13 native observation predates later S073 terrain/placement edits; source capture is a separate historical epoch",
            "native_state_imported": False,
            "causal_claim": False,
            "provider_parity_claim": False,
            "current_source_equivalence_claim": False,
            "readiness_claim": False,
            "source_fit_certified": False,
            "mission_ready": False,
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[4])
    parser.add_argument("--output", type=Path, required=True,
                        help="Fresh result path; existing files are never overwritten")
    parser.add_argument("--example-limit", type=int, default=12)
    args = parser.parse_args()
    if args.output.exists():
        parser.error("Output already exists; choose a fresh result path")
    result = analyze(args.repo, args.example_limit)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("x", encoding="utf-8") as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write("\n")
    print(json.dumps({"status": result["status"], **result["near_match_counts"],
                      "output": str(args.output.resolve()), "mission_ready": False}, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

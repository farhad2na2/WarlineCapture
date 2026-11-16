#!/usr/bin/env python3
"""Bind isolated authored-vehicle projection to a qualified native occupancy census; no parity or ownership inference."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from collections import Counter
from pathlib import Path


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_finalizer():
    path = Path(__file__).with_name("finalize_s073_authored_vehicle_occupancy_projection.py")
    spec = importlib.util.spec_from_file_location("s073_vehicle_projection_finalizer", path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


def rect_cells(width: int, height: int, cell: list[int], footprint: list[int]) -> tuple[list[int], list[int], list[int]]:
    sx, sy = max(1, footprint[0]), max(1, footprint[1])
    x0, y0 = cell[0] - (sx - 1) // 2, cell[1] - (sy - 1) // 2
    low = [max(0, min(width, x0)), max(0, min(height, y0))]
    high = [max(0, min(width, x0 + sx)), max(0, min(height, y0 + sy))]
    return low, high, [y * width + x for y in range(low[1], high[1]) for x in range(low[0], high[0])]


def compare(source_capture_receipt: Path, source_input: Path, source_projection: Path,
            native_receipt: Path, native_snapshot: Path, project_root: Path) -> dict:
    source_capture_receipt = source_capture_receipt.resolve(strict=True)
    source_input = source_input.resolve(strict=True)
    source_projection = source_projection.resolve(strict=True)
    native_receipt = native_receipt.resolve(strict=True)
    native_snapshot = native_snapshot.resolve(strict=True)
    root = project_root.resolve(strict=True)

    # Recompute the complete projection from the captured input. This also rechecks
    # every captured source pin against the current checkout and reproduces the
    # production GridOccupancyKernel footprint rows.
    finalizer = load_finalizer()
    expected_projection = finalizer.finalize(source_input, root)
    projection = json.loads(source_projection.read_text(encoding="utf-8"))
    need(projection == expected_projection, "Source projection differs from the pinned input/current source kernel reduction")
    capture_receipt = json.loads(source_capture_receipt.read_text(encoding="utf-8"))
    source_log = source_capture_receipt.parent / "editor.log"
    need(capture_receipt.get("status") == "Captured" and capture_receipt.get("wrapper_exit") == 0 and
         capture_receipt.get("required_marker_present") is True and capture_receipt.get("mission_ready") is False and
         capture_receipt.get("scope") == "IsolatedPregameAuthoredScene_NotRuntimeCampaignStateOrReadiness" and
         capture_receipt.get("input_sha256") == sha(source_input) and source_log.is_file() and
         capture_receipt.get("editor_log_sha256") == sha(source_log),
         "Source capture receipt/input/editor log binding or scope mismatch")
    need(projection.get("capture_input_sha256") == sha(source_input) and
         projection.get("source_pins_sha256") == json.loads(source_input.read_text(encoding="utf-8")).get("source_pins_sha256"),
         "Projection input hash/source pins mismatch")

    wrapper = json.loads(native_receipt.read_text(encoding="utf-8"))
    snapshot = json.loads(native_snapshot.read_text(encoding="utf-8"))
    native_log = native_receipt.parent / "editor.log"
    need(wrapper.get("status") == "Recorded" and wrapper.get("wrapper_exit") == 0 and
         wrapper.get("mission_ready") is False and wrapper.get("scope") == "IndependentInstrumentedNativeCapture_NotNormalInputOrReadiness" and
         wrapper.get("snapshot_sha256") == sha(native_snapshot) and native_log.is_file() and
         wrapper.get("editor_log_sha256") == sha(native_log) and
         wrapper.get("session_id") == snapshot.get("session_id"),
         "Native wrapper receipt/snapshot/editor log binding or scope mismatch")
    need(snapshot.get("kind") == "S073NativePathfindingCommandDiagnostic" and
         snapshot.get("schema_version") == 1 and snapshot.get("seed") == 104729 and
         snapshot.get("scenario_index") == 3 and snapshot.get("mission_ready") is not True and
         snapshot.get("player_readiness") is not True,
         "Qualified unready native S073 snapshot required")
    observation = snapshot.get("pre_enqueue_input_observation") or {}
    census = observation.get("occupancy_contributor_census") or {}
    need(census.get("session_id") == wrapper.get("session_id") and
         census.get("map_id") == "opmap.skirmish.industrial_basin" and
         census.get("source_campaign_map_id") == "opmap.skirmish.desert_base_01" and
         census.get("read_only") is True and census.get("state_imported") is False and
         census.get("gameplay_state_mutated") is False,
         "Native occupancy census identity/read-only contract mismatch")
    native_pins = census.get("source_pins_sha256")
    need(isinstance(native_pins, dict) and len(native_pins) == wrapper.get("source_pin_count"),
         "Native source pin set/count mismatch")
    for relative, expected in native_pins.items():
        path = root / relative
        need(not Path(relative).is_absolute() and ".." not in Path(relative).parts and path.is_file() and
             sha(path) == expected, "Native captured source pin mismatch: " + relative)
    grid = projection["grid"]
    need((census.get("grid_width"), census.get("grid_height")) == (grid["width"], grid["height"]),
         "Source/native grid dimensions differ")

    all_native = census.get("entities")
    need(isinstance(all_native, list) and len(all_native) == census.get("entity_count"),
         "Native census entity list/count mismatch")
    neutral = [r for r in all_native if r.get("faction_id") == 0]
    need(len(neutral) == 22 and census.get("counts_by_faction_id", {}).get("0") == 22,
         "Expected 22 faction-0 native census rows for this comparison")
    native_cells: set[int] = set()
    native_vehicle_multiset: Counter = Counter()
    for row in neutral:
        cell, fp = row.get("cell"), row.get("kernel_footprint")
        provenance = row.get("source_provenance") or {}
        key = provenance.get("unit_source_prefab_key")
        need(provenance.get("unit_source_prefab_key_component_present") is True and
             isinstance(key, str) and key.startswith("Unit_Veh_"),
             "Faction-0 census row lacks observed vehicle prefab key")
        lo, hi, expected_cells = rect_cells(grid["width"], grid["height"], cell, fp)
        need(row.get("kernel_clamped_bounds_min") == lo and row.get("kernel_clamped_bounds_max_exclusive") == hi,
             "Native census row bounds differ from pinned GridOccupancyKernel semantics")
        observed_cells = []
        for item in row.get("footprint_cells", []):
            x, y = item.get("x"), item.get("y")
            index = y * grid["width"] + x
            need(item.get("row_major_index") == index and item.get("occupancy_bit_set") is True,
                 "Native census cell index/occupancy evidence mismatch")
            observed_cells.append(index)
        need(sorted(observed_cells) == expected_cells and row.get("in_bounds_footprint_cells") == len(expected_cells) and
             row.get("occupancy_set_footprint_cells") == len(expected_cells) and
             row.get("occupancy_missing_footprint_cells") == 0 and row.get("out_of_bounds_footprint_cells") == 0,
             "Native census footprint differs from production kernel or observed occupancy bits")
        native_cells.update(expected_cells)
        native_vehicle_multiset[(key, tuple(cell), tuple(fp))] += 1
    source_cells = set(projection["occupied_row_major_cells"])
    source_multiset = Counter((v["unit_source_prefab_key"], tuple(v["pre_simulation_effective_cell"]), tuple(v["footprint"]))
                              for v in projection["vehicles"])
    need(sum(source_multiset.values()) == projection["vehicle_count"] and
         len(source_cells) == projection["occupied_cell_count"], "Source projection summary does not match rows")
    all_native_cells = set()
    for row in all_native:
        all_native_cells.update(item["row_major_index"] for item in row.get("footprint_cells", [])
                                if item.get("occupancy_bit_set") is True)
    need(len(all_native_cells) == census.get("actual_occupied_cell_count"),
         "Native full-census occupancy union differs from census summary")
    only_source = sorted(source_cells - native_cells)
    only_native = sorted(native_cells - source_cells)
    multiset_delta = native_vehicle_multiset - source_multiset
    source_delta = source_multiset - native_vehicle_multiset
    return {
        "kind": "S073AuthoredVehicleProjectionNativeCensusComparison", "schema_version": 1,
        "status": "ExactFaction0CoordinateAndVehicleMultisetMatch" if not only_source and not only_native and not multiset_delta and not source_delta else "DifferencesObserved",
        "source_capture": {"capture_receipt_sha256": sha(source_capture_receipt), "input_sha256": sha(source_input),
                           "projection_sha256": sha(source_projection), "editor_log_sha256": sha(source_log)},
        "native_capture": {"wrapper_receipt_sha256": sha(native_receipt), "snapshot_sha256": sha(native_snapshot),
                           "editor_log_sha256": sha(native_log), "session_id": wrapper["session_id"]},
        "sessions_are_independent": True,
        "grid": grid,
        "source_projection": {"vehicle_count": len(projection["vehicles"]), "occupied_coordinate_count": len(source_cells),
                              "coordinates": "row-major cell indices", "pre_simulation_projection": True},
        "native_faction_0_census": {"contributor_count": len(neutral), "occupied_coordinate_count": len(native_cells),
                                    "faction_id_observed": 0, "prefab_key_component_observed": True},
        "set_overlap": {"shared_coordinate_count": len(source_cells & native_cells), "source_only_count": len(only_source),
                        "native_faction_0_only_count": len(only_native), "source_only_sample": only_source[:64],
                        "native_faction_0_only_sample": only_native[:64]},
        "vehicle_multiset": {"key": "(prefab key, cell, footprint)", "exact_match": not multiset_delta and not source_delta,
                             "native_only": [{"prefab_key": k[0], "cell": list(k[1]), "footprint": list(k[2]), "count": c}
                                             for k, c in sorted(multiset_delta.items())],
                             "source_only": [{"prefab_key": k[0], "cell": list(k[1]), "footprint": list(k[2]), "count": c}
                                             for k, c in sorted(source_delta.items())]},
        "scope": "Independent native census and isolated pre-simulation authored neutral physical projection compared offline",
        "identity_join_performed": False, "ownership_inferred": False, "full_parity_claim": False,
        "state_transfer": False, "source_validation": False, "scheduler_admitted": False, "mission_ready": False,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-capture-receipt", type=Path, required=True)
    parser.add_argument("--source-input", type=Path, required=True)
    parser.add_argument("--source-projection", type=Path, required=True)
    parser.add_argument("--native-receipt", type=Path, required=True)
    parser.add_argument("--native-snapshot", type=Path, required=True)
    parser.add_argument("--project-root", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = compare(args.source_capture_receipt, args.source_input, args.source_projection,
                     args.native_receipt, args.native_snapshot, args.project_root)
    with args.output.open("x", encoding="utf-8") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
    print(json.dumps({"status": result["status"], "shared_coordinates": result["set_overlap"]["shared_coordinate_count"],
                      "source_only": result["set_overlap"]["source_only_count"],
                      "native_only": result["set_overlap"]["native_faction_0_only_count"], "mission_ready": False}))


if __name__ == "__main__":
    main()

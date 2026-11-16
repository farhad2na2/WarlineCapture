#!/usr/bin/env python3
"""Conservative set attribution for independent S073 occupancy observations."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
COMPARE_PATH = HERE / "compare_s073_blocker_occupancy.py"
spec = importlib.util.spec_from_file_location("compare_s073_blocker_occupancy", COMPARE_PATH)
compare_module = importlib.util.module_from_spec(spec)
assert spec.loader is not None
spec.loader.exec_module(compare_module)


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def footprint_set(census: dict, faction: int | None = None) -> set[int]:
    width = census["grid_width"]
    result: set[int] = set()
    entities = census["entities"]
    need(isinstance(entities, list) and census.get("entity_count") == len(entities),
         "Native census entity rows/count mismatch")
    identities = set()
    for row in entities:
        key = tuple(row.get("entity", []))
        need(len(key) == 2 and key not in identities, "Native census entity identity missing/duplicated")
        identities.add(key)
        faction_id = row.get("faction_id")
        need(faction_id is None or isinstance(faction_id, int), "Invalid faction id in contributor row")
        cell_x, cell_y = row.get("cell", [None, None])
        authored_x, authored_y = row.get("authored_footprint", [None, None])
        kernel_x, kernel_y = row.get("kernel_footprint", [None, None])
        need(isinstance(cell_x, int) and isinstance(cell_y, int) and
             isinstance(authored_x, int) and isinstance(authored_y, int) and
             kernel_x == max(1, authored_x) and kernel_y == max(1, authored_y),
             "Native kernel footprint does not match captured cell/size facts")
        raw_min_x = cell_x - (kernel_x - 1) // 2
        raw_min_y = cell_y - (kernel_y - 1) // 2
        expected_min = [max(0, min(width, raw_min_x)),
                        max(0, min(census["grid_height"], raw_min_y))]
        expected_max = [max(0, min(width, raw_min_x + kernel_x)),
                        max(0, min(census["grid_height"], raw_min_y + kernel_y))]
        need(row.get("kernel_clamped_bounds_min") == expected_min and
             row.get("kernel_clamped_bounds_max_exclusive") == expected_max,
             "Captured bounds do not reproduce production GridOccupancyKernel.Bounds")
        need(row.get("out_of_bounds_footprint_cells") == 0 and
             row.get("occupancy_missing_footprint_cells") == 0 and
             row.get("in_bounds_footprint_cells") == len(row.get("footprint_cells", [])),
             "Native contributor footprint/kernel readback mismatch")
        if faction is not None and faction_id != faction:
            continue
        expected_cells = {(x, y) for y in range(expected_min[1], expected_max[1])
                          for x in range(expected_min[0], expected_max[0])}
        actual_cells = {(cell.get("x"), cell.get("y")) for cell in row["footprint_cells"]}
        need(actual_cells == expected_cells, "Serialized footprint rows differ from production kernel bounds")
        for cell in row["footprint_cells"]:
            x, y = cell["x"], cell["y"]
            need(0 <= x < width and 0 <= y < census["grid_height"], "Contributor cell outside census grid")
            need(cell["row_major_index"] == y * width + x and cell["occupancy_bit_set"] is True,
                 "Contributor cell index or actual occupancy bit mismatch")
            result.add(cell["row_major_index"])
    return result


def source_census_sets(census: dict) -> tuple[dict[str, set[int]], list[dict]]:
    need(census.get("kind") == "S073SourceDynamicOccupancyContributorCensus" and
         census.get("source_state_imported") is False and
         census.get("activation_or_admission_claimed") is False and
         census.get("readiness_claimed") is False and
         census.get("scope") == "source-host-read-only-post-original-owner-sample",
         "Source contributor census scope/import/readiness fields invalid")
    query = census.get("query") or {}
    need(query.get("all") == ["UnitGrid", "UnitFootprint"] and
         query.get("none") == ["StaticGridBlocker", "RuntimeBuildingCombatTag", "UnitDeathAnimationComponent"] and
         query.get("entity_query_default_exclusions") == ["Prefab", "Disabled"] and
         query.get("actual_system") == "DynamicOccupancyRebuildSystem._trackedUnitsQuery",
         "Source census does not document the original tracked query")
    width = census["grid"]["width"]
    height = census["grid"]["height"]
    need(census["grid"].get("cell_count") == width * height and
         width == 2048 and height == 1024, "Source census grid frame invalid")
    rows = census.get("contributors")
    need(isinstance(rows, list) and census.get("contributor_count") == len(rows) and
         census.get("contributors_with_faction_component") == len(rows),
         "Source contributor count/faction component census mismatch")
    result = {str(f): set() for f in (0, 1, 2)}
    identities = set()
    normalized_rows = []
    for row in rows:
        ident = (row.get("entity_index"), row.get("entity_version"))
        need(all(isinstance(v, int) and v > 0 for v in ident) and ident not in identities,
             "Source run-local entity identity absent/duplicated")
        identities.add(ident)
        faction = row.get("faction_id")
        need(faction in (0, 1, 2), "Source contributor faction is outside expected S073 factions")
        x, y = row.get("cell", [None, None])
        sx, sy = row.get("footprint", [None, None])
        need(all(isinstance(v, int) for v in (x, y, sx, sy)) and sx >= 1 and sy >= 1,
             "Source contributor cell or footprint invalid")
        min_x, min_y = max(0, x - (sx - 1) // 2), max(0, y - (sy - 1) // 2)
        max_x, max_y = min(width, x - (sx - 1) // 2 + sx), min(height, y - (sy - 1) // 2 + sy)
        cells = {yy * width + xx for yy in range(min_y, max_y) for xx in range(min_x, max_x)}
        reported = row.get("covered_row_major_cell_indices_from_production_bounds")
        need(isinstance(reported, list) and len(reported) == len(set(reported)) and set(reported) == cells,
             "Source contributor cells differ from production kernel bounds")
        result[str(faction)].update(cells)
        normalized_rows.append({"run_local_entity": [ident[0], ident[1]], "faction_id": faction,
                                "source_prefab_key": row.get("source_prefab_key"),
                                "cell": [x, y], "footprint": [sx, sy],
                                "covered_cell_count": len(cells)})
    union = result["0"] | result["1"] | result["2"]
    actual = census.get("actual_occupied_cells")
    need(isinstance(actual, list) and actual == sorted(set(actual)) and set(actual) == union and
         census.get("actual_occupied_cell_count") == len(union) and
         census.get("reconstructed_occupied_cell_count") == len(union) and
         census.get("actual_vs_reconstructed_cell_mismatch_count") == 0,
         "Source census actual occupancy does not equal reconstructed contributor union")
    return result, normalized_rows


def attribute(native_snapshot: Path, native_wrapper: Path, native_raw: Path,
              native_receipt: Path, source_raw: Path, source_receipt: Path,
              source_run: Path, comparison_path: Path,
              source_result_path: Path | None = None,
              source_census_run_path: Path | None = None) -> dict:
    comparison = json.loads(comparison_path.read_text(encoding="utf-8"))
    wrapper = json.loads(native_wrapper.read_text(encoding="utf-8"))
    snapshot = json.loads(native_snapshot.read_text(encoding="utf-8"))
    source_run_json = json.loads(source_run.read_text(encoding="utf-8"))
    need(wrapper.get("status") == "Recorded" and wrapper.get("wrapper_exit") == 0 and
         wrapper.get("snapshot_sha256") == digest(native_snapshot) and
         wrapper.get("mission_ready") is False and
         wrapper.get("scope") == "IndependentInstrumentedNativeCapture_NotNormalInputOrReadiness",
         "Native wrapper receipt does not bind the supplied snapshot")
    need(snapshot.get("kind") == "S073NativePathfindingCommandDiagnostic" and
         snapshot.get("session_id") == wrapper.get("session_id") and
         snapshot.get("mission_ready") is not True and snapshot.get("player_readiness") is not True,
         "Native snapshot session/scope mismatch")
    obs = snapshot.get("pre_enqueue_input_observation")
    need(isinstance(obs, dict) and obs.get("raw_blocker_occupancy_snapshot") ==
         json.loads(native_receipt.read_text(encoding="utf-8")),
         "Native receipt is not the snapshot's pre-enqueue raw observation")
    need(source_run_json.get("kind") == "SkirmishSourceSimulationRunReceipt" and
         source_run_json.get("mission") == "S073" and source_run_json.get("seed") == 104729 and
         source_run_json.get("status") == "PreparingPhysicalSourceRequired" and
         source_run_json.get("validation_passed") is False and
         source_run_json.get("full_outcome_observed") is False,
         "Source run provenance gate/scope mismatch")

    # Re-run the checked comparator against these exact artifacts, then require its
    # complete result to match the pinned comparison supplied by the caller.
    computed = compare_module.compare(native_raw, native_receipt, source_raw, source_receipt,
                                      native_wrapper, native_snapshot, source_run, 8)
    need(computed == comparison, "Supplied comparison is not reproducible from pinned inputs")
    need(computed.get("sessions_are_independent") is True and
         computed.get("native_capture", {}).get("native_session_id") !=
         computed.get("source_capture", {}).get("source_session_id"),
         "Independent native/source sessions required")

    census = obs.get("occupancy_contributor_census")
    need(isinstance(census, dict) and census.get("read_only") is True and
         census.get("state_imported") is False and census.get("gameplay_state_mutated") is False and
         census.get("session_id") == snapshot.get("session_id") and
         census.get("world_sequence") == snapshot.get("world_sequence") and
         census.get("map_id") == snapshot.get("source_map_id"),
         "Native read-only contributor census is not bound to this native session/map")
    need(census.get("membership_contract", "").startswith("DynamicOccupancyRebuildSystem tracked query:") and
         census.get("entity_query_semantics", "").startswith("default query filters apply"),
         "Census does not state the original tracked-query membership contract")
    need((census.get("grid_width"), census.get("grid_height")) ==
         (computed["grid"]["width"], computed["grid"]["height"]), "Census/comparison grid mismatch")

    native_cells = footprint_set(census)
    native_by_faction = {str(f): footprint_set(census, f) for f in (0, 1, 2)}
    faction_counts = {str(f): 0 for f in (0, 1, 2)}
    for row in census["entities"]:
        if row.get("faction_id") in (0, 1, 2):
            faction_counts[str(row["faction_id"])] += 1
    need(census.get("counts_by_faction_id_including_unfactioned_excluded") == faction_counts,
         "Census faction entity totals do not match the entity rows")
    expected_native = set()
    for cells in native_by_faction.values():
        expected_native |= cells
    need(native_cells == expected_native and census.get("distinct_live_contributor_cells") == len(native_cells) and
         census.get("actual_occupied_cell_count") == len(native_cells) and
         census.get("occupied_cells_without_live_tracked_entity_footprint_count") == 0,
         "Faction kernel footprint union does not reproduce native census totals")
    native_bits = compare_module.load_capture(native_raw, native_receipt, "native")[1]["occupied"]
    source_bits = compare_module.load_capture(source_raw, source_receipt, "source")[1]["occupied"]
    native_raw_set = {i for i in range(census["grid_width"] * census["grid_height"])
                      if compare_module.bit(native_bits, i)}
    source_set = {i for i in range(census["grid_width"] * census["grid_height"])
                  if compare_module.bit(source_bits, i)}
    need(native_cells == native_raw_set, "Kernel footprint union differs from native captured occupied mask")
    need(census.get("footprint_cell_total_in_bounds") ==
         sum(len(row["footprint_cells"]) for row in census["entities"]) and
         census.get("footprint_cell_total_out_of_bounds") == 0,
         "Census footprint contribution totals do not match entity/kernel rows")
    need(len(native_by_faction["0"] - source_set) == 109 and
         native_by_faction["1"] <= source_set and native_by_faction["2"] <= source_set and
         (native_by_faction["1"] | native_by_faction["2"]) == source_set,
         "Observed faction/source set relationship differs from stated attribution precondition")
    neutral = native_by_faction["0"]
    result = {
        "kind": "S073IndependentOccupancySetAttribution",
        "schema_version": 1,
        "status": "AttributionRecorded",
        "native": {"session_id": snapshot["session_id"], "world_sequence": snapshot["world_sequence"],
                   "entity_count": census["entity_count"], "occupied_cells": len(native_cells),
                   "faction_entity_counts": census["counts_by_faction_id_including_unfactioned_excluded"],
                   "faction_occupied_cell_counts": {k: len(v) for k, v in native_by_faction.items()}},
        "source": {"session_id": computed["source_capture"]["source_session_id"],
                   "occupied_cells": len(source_set)},
        "set_relationship": {"native_extra_vs_source_cells": len(native_raw_set - source_set),
                             "source_extra_vs_native_cells": len(source_set - native_raw_set),
                             "faction_0_cells_absent_from_source": len(neutral - source_set),
                             "faction_0_absent_cells_are_all_native_extras":
                             neutral - source_set == native_raw_set - source_set,
                             "faction_1_cells_in_source": len(native_by_faction["1"] & source_set),
                             "faction_2_cells_in_source": len(native_by_faction["2"] & source_set),
                             "faction_1_and_2_union_equals_source":
                             (native_by_faction["1"] | native_by_faction["2"]) == source_set},
        "neutral_source_graph_coordinate_membership": "Unknown; source graph does not provide a validated coordinate-to-live-entity ownership map for these native entities",
        "causal_or_source_ownership_inference": False,
        "sessions_are_independent": True,
        "state_transfer": False,
        "parity_claim": False,
        "scheduler_admitted": False,
        "mission_ready": False,
        "evidence_sha256": {"native_snapshot": digest(native_snapshot), "native_wrapper_receipt": digest(native_wrapper),
                            "native_raw": digest(native_raw), "native_receipt": digest(native_receipt),
                            "source_raw": digest(source_raw), "source_receipt": digest(source_receipt),
                            "source_run_receipt": digest(source_run), "comparison": digest(comparison_path)},
    }
    if source_result_path is not None:
        need(source_census_run_path is not None, "Source census run receipt required")
        census_run_json = json.loads(source_census_run_path.read_text(encoding="utf-8"))
        worker = census_run_json.get("worker_result") or {}
        build = census_run_json.get("build_receipt") or {}
        need(worker.get("path") == str(source_result_path.resolve()) and
             worker.get("sha256") == digest(source_result_path),
             "Source census run receipt does not bind supplied census result")
        build_path = Path(build.get("path", "")).resolve(strict=True)
        need(digest(build_path) == build.get("sha256"), "Source run build receipt pin mismatch")
        build_json = json.loads(build_path.read_text(encoding="utf-8"))
        source_result = json.loads(source_result_path.read_text(encoding="utf-8"))
        executable = build_json.get("executable") or {}
        executable_path = Path(executable.get("path", "")).resolve(strict=True)
        need(digest(executable_path) == executable.get("sha256"), "Source build executable hash mismatch")
        need(census_run_json.get("kind") == "SkirmishSourceSimulationRunReceipt" and
             census_run_json.get("mission") == "S073" and census_run_json.get("seed") == 104729 and
             census_run_json.get("status") == "PreparingPhysicalSourceRequired" and
             build_json.get("kind") == "SkirmishSourceSimulationBuild" and
             build_json.get("mission") == "S073" and build_json.get("seed") == 104729 and
             build_json.get("status") == "Passed" and source_result.get("mission") == "S073" and
             source_result.get("seed") == 104729,
             "Verified S073 source build/run identity mismatch")
        need(source_result.get("kind") == "SkirmishSourceSimulationRunReceipt" and
             source_result.get("mission") == "S073" and source_result.get("seed") == 104729 and
             source_result.get("status") == "PreparingPhysicalSourceRequired" and
             source_result.get("mission_ready") is False,
             "Source census result mission/scope invalid")
        startup = source_result.get("building_and_unit_startup_diagnostic") or {}
        census = startup.get("source_occupancy_contributor_census")
        need(isinstance(census, dict), "Source occupancy census absent from verified run result")
        source_sets, source_rows = source_census_sets(census)
        need((census["grid"]["width"], census["grid"]["height"]) ==
             (computed["grid"]["width"], computed["grid"]["height"]), "Source census/comparison grid mismatch")
        source_census_union = source_sets["0"] | source_sets["1"] | source_sets["2"]
        need(source_census_union == set(census["actual_occupied_cells"]),
             "Source contributor faction cells do not reproduce source census actual mask")
        native_faction_overlap = {f: {"source_cells": len(source_sets[f]),
                                      "native_cells": len(native_by_faction[f]),
                                      "intersection": len(source_sets[f] & native_by_faction[f]),
                                      "source_only_vs_native_faction": len(source_sets[f] - native_by_faction[f]),
                                      "native_only_vs_source_faction": len(native_by_faction[f] - source_sets[f])}
                                  for f in ("0", "1", "2")}
        result["source_contributor_census"] = {
            "run_session_id": source_result["session_id"],
            "verified_build_receipt_sha256": build["sha256"],
            "contributor_count": census["contributor_count"],
            "faction_contributor_counts": {str(f): sum(row["faction_id"] == f for row in source_rows)
                                           for f in (0, 1, 2)},
            "faction_occupied_cell_counts": {k: len(v) for k, v in source_sets.items()},
            "native_faction_overlap": native_faction_overlap,
            "source_raw_capture_overlap": {"raw_capture_session_id": computed["source_capture"]["source_session_id"],
                                            "same_source_session": False,
                                            "intersection_cells": len(source_census_union & source_set),
                                            "census_only_cells": len(source_census_union - source_set),
                                            "raw_only_cells": len(source_set - source_census_union)},
            "run_local_identities_and_source_keys": source_rows,
            "identity_scope": "Source entity index/version values are local to this independent source run; no cross-session identity mapping is asserted",
            "source_graph_coordinate_ownership": "Unknown",
        }
        result["evidence_sha256"]["source_result"] = digest(source_result_path)
        result["evidence_sha256"]["source_census_run_receipt"] = digest(source_census_run_path)
        result["evidence_sha256"]["source_build_receipt"] = digest(build_path)
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("native-snapshot", "native-wrapper-receipt", "native-raw", "native-receipt",
                 "source-raw", "source-receipt", "source-run-receipt", "comparison", "output", "source-result",
                 "source-census-run-receipt"):
        parser.add_argument("--" + name, type=Path, required=True)
    args = parser.parse_args()
    result = attribute(args.native_snapshot, args.native_wrapper_receipt, args.native_raw,
                       args.native_receipt, args.source_raw, args.source_receipt,
                       args.source_run_receipt, args.comparison, args.source_result,
                       args.source_census_run_receipt)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": result["status"], "set_relationship": result["set_relationship"],
                      "causal_or_source_ownership_inference": False, "mission_ready": False}))


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Compare the loaded-scene building census with source-host materialization.

The two captures come from distinct Unity/source-host sessions. A passing join
establishes source identity and authored transform correspondence only; it does
not load a Campaign scene into the source host or admit gameplay.
"""

import argparse
import hashlib
import json
import math
from pathlib import Path


SCENE_GUID = "c00140f2e94a04c3084c8dcb0c18cbd0"
SCENE_PATH = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity"
LOGICAL_MAP = "opmap.skirmish.industrial_basin"
SOURCE_MAP = "opmap.skirmish.desert_base_01"
SOURCE_CAPTURE_SHA256 = "1f375a666f474a73d2aaac5c0b1930a774b117349a1e074f618ecbbd57b01bd7"


def require(ok, message):
    if not ok:
        raise ValueError(message)


def file_sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def identity(row):
    stable = row.get("stable_id")
    source = row.get("source_global_object_id")
    placement = row.get("placement_index")
    require(isinstance(stable, str) and stable, "Building stable ID required")
    require(isinstance(source, str), "Building source GlobalObjectId required")
    require(type(placement) is int and placement >= 0, "Building placement index required")
    return stable, source, placement


def index_rows(rows, label):
    require(isinstance(rows, list) and len(rows) == 4931, label + " must contain exactly 4,931 buildings")
    indexed = {}
    for row in rows:
        require(isinstance(row, dict), label + " row must be an object")
        key = identity(row)
        require(key not in indexed, label + " has duplicate building identity")
        indexed[key] = row
    return indexed


def compare(native, host_result, readback, position_tolerance=0.0001):
    require(native.get("kind") == "S073NativeWaterMaskObservation" and
            native.get("phase") == "Playing" and native.get("scenario_index") == 3 and
            native.get("seed") == 104729 and native.get("operation_map_id") == LOGICAL_MAP and
            native.get("source_campaign_map_id") == SOURCE_MAP and
            native.get("campaign_gameplay_state_copied") is False and
            native.get("mission_ready") is False and native.get("source_fit_certified") is False,
            "Exact scoped native S073 loaded-source observation required")
    fit = native.get("loaded_campaign_source_fit_observation")
    require(isinstance(fit, dict) and fit.get("scene_loaded") is True and
            fit.get("source_scene_guid") == SCENE_GUID and fit.get("source_scene_path") == SCENE_PATH and
            fit.get("campaign_source_map_id") == SOURCE_MAP and
            fit.get("source_fit_certified") is False and fit.get("scheduler_admitted") is False and
            fit.get("mission_ready") is False and fit.get("player_readiness") is False,
            "Native observation must bind the expected loaded Campaign scene and remain diagnostic")
    pins = native.get("source_pins_sha256")
    require(isinstance(pins, dict) and isinstance(pins.get(SCENE_PATH), str) and
            isinstance(pins.get(SCENE_PATH + ".meta"), str), "Native source scene and metadata pins required")

    require(host_result.get("kind") == "SkirmishSourceSimulationRunReceipt" and
            host_result.get("mission") == "S073" and host_result.get("seed") == 104729 and
            host_result.get("status") == "PreparingPhysicalSourceRequired" and
            host_result.get("scheduler_tick_executed") is False and host_result.get("mission_ready") is False and
            host_result.get("operation_map_id") == LOGICAL_MAP and
            host_result.get("operation_map_physical_source_validated") is False,
            "Source host must remain at the known pre-scheduler physical-source gate")
    projection = host_result.get("experimental_physical_blocker_projection")
    descriptor = projection.get("neutral_scenery_readback") if isinstance(projection, dict) else None
    require(isinstance(descriptor, dict) and descriptor.get("count") == 4931 and
            descriptor.get("scope") == "Authoring-derived neutral identity, health and transforms; no resource, production or session state",
            "Authoring-derived source-host scenery readback required")
    require(readback.get("kind") == "S073SourceNeutralSceneryReadback" and
            readback.get("campaign_runtime_state_copied") is False and
            readback.get("complete_gameplay_payload") is False and readback.get("mission_ready") is False,
            "Source-host neutral scenery readback must retain its limited scope")
    require(readback.get("source_capture_sha256") == SOURCE_CAPTURE_SHA256,
            "Source-host readback must bind the reviewed Campaign input capture")

    native_rows = index_rows(fit.get("building_entities"), "Native loaded-scene census")
    host_rows = index_rows(readback.get("entities"), "Source-host scenery readback")
    require(native_rows.keys() == host_rows.keys(), "Native and source-host Campaign building identities differ")

    position_deltas = []
    faction_mismatches = 0
    health_mismatches = 0
    for key in sorted(native_rows):
        native_row, host_row = native_rows[key], host_rows[key]
        require(native_row.get("bound_campaign_scene_owned") is True and
                native_row.get("operation_map_id") == SOURCE_MAP,
                "Native building must be owned by the bound Campaign scene")
        a, b = native_row.get("position"), host_row.get("position")
        require(isinstance(a, list) and len(a) == 3 and isinstance(b, list) and len(b) == 3,
                "Native and source-host 3D building positions required")
        require(all(type(x) in (int, float) and math.isfinite(x) for x in a + b),
                "Building positions must be finite numbers")
        position_deltas.append(max(abs(float(x) - float(y)) for x, y in zip(a, b)))
        if native_row.get("faction_id") != host_row.get("faction"):
            faction_mismatches += 1
        if native_row.get("faction_id") == 0 and (
                host_row.get("health_current") != host_row.get("health_max") or
                host_row.get("combat_owner") != 0 or host_row.get("ai") is not False):
            health_mismatches += 1

    max_delta = max(position_deltas)
    return {
        "kind": "S073LoadedCampaignBuildingMaterializationComparison",
        "status": "IdentityAndPositionMatch" if max_delta <= position_tolerance and faction_mismatches == 0 and health_mismatches == 0 else "Mismatch",
        "native_session_id": native.get("session_id"),
        "native_capture_world": fit.get("world_name"),
        "source_host_session_id": host_result.get("session_id"),
        "native_building_count": len(native_rows),
        "source_host_building_count": len(host_rows),
        "identity_matches": len(native_rows),
        "max_position_delta": max_delta,
        "position_tolerance": position_tolerance,
        "faction_mismatch_count": faction_mismatches,
        "neutral_authoring_state_mismatch_count": health_mismatches,
        "source_scene_sha256": pins[SCENE_PATH],
        "source_scene_meta_sha256": pins[SCENE_PATH + ".meta"],
        "source_host_readback_sha256": descriptor.get("sha256"),
        "scope": "IndependentSessions_4931BuildingIdentityAndAuthoredPositionCorrespondenceOnly",
        "native_scene_loaded_in_source_host": False,
        "campaign_gameplay_state_copied": False,
        "physical_source_validated": False,
        "scheduler_admitted": False,
        "mission_ready": False,
        "player_readiness": False,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--native-observation", required=True, type=Path)
    parser.add_argument("--native-sha256", required=True)
    parser.add_argument("--host-result", required=True, type=Path)
    parser.add_argument("--host-result-sha256", required=True)
    parser.add_argument("--host-readback", required=True, type=Path)
    parser.add_argument("--host-readback-sha256", required=True)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    for path, expected in ((args.native_observation, args.native_sha256),
                           (args.host_result, args.host_result_sha256),
                           (args.host_readback, args.host_readback_sha256)):
        require(file_sha(path) == expected, "Evidence artifact hash mismatch: " + str(path))
    native = json.loads(args.native_observation.read_text())
    host = json.loads(args.host_result.read_text())
    readback = json.loads(args.host_readback.read_text())
    descriptor = host["experimental_physical_blocker_projection"]["neutral_scenery_readback"]
    require(Path(descriptor["path"]).resolve() == args.host_readback.resolve() and
            descriptor["sha256"] == args.host_readback_sha256,
            "Host result must bind the supplied neutral scenery readback")
    result = compare(native, host, readback)
    with args.output.open("x") as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write("\n")
    print(json.dumps(result, separators=(",", ":")))
    return 0 if result["status"] == "IdentityAndPositionMatch" else 2


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, KeyError, TypeError) as error:
        raise SystemExit("S073 loaded-source comparison rejected: " + str(error))

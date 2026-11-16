#!/usr/bin/env python3
"""Audit one source-only S003 logical-defense observation and owner readback."""
import argparse
import hashlib
import json
from pathlib import Path


def read(path):
    raw = path.read_bytes()
    return json.loads(raw), hashlib.sha256(raw).hexdigest()


def sha(path):
    h = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def need(condition, message):
    if not condition:
        raise ValueError(message)


def audit(directory, output):
    receipt, receipt_hash = read(directory / "run-receipt.json")
    result, result_hash = read(directory / "result.json")
    request, request_hash = read(directory / "run-request.json")
    need(receipt.get("mission") == "S003" and receipt.get("seed") == 104732 and
         receipt.get("status") == "BoundedPrefix" and receipt.get("process_exit") == 0 and
         receipt.get("timed_out") is False and receipt.get("source_result") == result and
         receipt.get("validation_passed") is False and receipt.get("native_acceptance") is False and
         receipt.get("source_fit_certified") is False and receipt.get("camera_qualified") is False and
         receipt.get("active_mission_water_qualified") is False and receipt.get("mission_ready") is False,
         "Checked unqualified S003 bounded source result required")
    need(request.get("mission") == "S003" and request.get("seed") == 104732 and
         Path(request["output_directory"]).resolve() == directory.resolve() and
         result.get("status") == "BoundedPrefix" and result.get("source_outcome") == "None",
         "Same-directory source request and nonterminal result required")
    trace_path, census_path = directory / "aria-trace.jsonl", directory / "validator-state.jsonl"
    trace_hash, census_hash = sha(trace_path), sha(census_path)
    with trace_path.open() as stream:
        trace = [json.loads(line) for line in stream]
    with census_path.open() as stream:
        census = [json.loads(line) for line in stream]
    need(trace and census and all(row.get("scope") == "LogicalPublicData" and
         row.get("native_rendered_input") is False and row.get("logical_defense_source_diagnostic") is True and
         row.get("native_defense_control_visibility_known") is False for row in trace),
         "Explicit logical source scope without native-input claim required")
    world = trace[0].get("world_id")
    need(isinstance(world, str) and world and all(row.get("world_id") == world for row in trace + census),
         "Same source world required in action and independent census traces")
    executed = [row for row in trace if row.get("receipt") is not None and row.get("action") in
                ("InspectHome", "OpenDefensePreview", "SelectDefenseSite", "ConfirmDefensePreview")]
    need([row["action"] for row in executed] == ["InspectHome", "OpenDefensePreview", "SelectDefenseSite", "ConfirmDefensePreview"] and
         all(executed[i]["frame"] < executed[i + 1]["frame"] for i in range(3)),
         "One ordered source defense transaction required")
    moved, confirmed = executed[2], executed[3]
    need(moved["receipt"].get("source_preview_moved") is True and
         moved["receipt"].get("captured_geometry_valid") is True and
         confirmed["receipt"].get("accepted") is True and confirmed["receipt"].get("runtime_building_id") == 15 and
         confirmed["receipt"].get("native_touch_observed") is False and
         confirmed["receipt"].get("preview_qualified") is False,
         "Actual source placement acceptance required")
    before = confirmed["observation"]
    after = next((row for row in trace if row["frame"] > confirmed["frame"] and
                  row.get("observation", {}).get("OwnDefenseTowers") == 2), None)
    need(before.get("OwnDefenseTowers") == 1 and before.get("OwnMaterials") == 370 and
         after is not None and after["observation"].get("OwnMaterials") == 320,
         "Actual source tower and resource readback required")
    later = next((row for row in census if row.get("tick", -1) > confirmed["tick"] and any(
        actor.get("faction") == 1 and actor.get("components", {}).get("Game.Components.UnitSourcePrefabKey", {}).get("Value") == "Building_GuardTower" and
        actor.get("components", {}).get("Game.Components.SkirmishAttemptOwnedComponent", {}).get("StableObjectId") == "constructed.15"
        for actor in row.get("actors", []))), None)
    need(later is not None, "Attempt-owned constructed tower missing from source census")
    need(sha(trace_path) == trace_hash and sha(census_path) == census_hash, "Trace changed during audit")
    need(output.parent.is_dir() and not output.exists(), "Fresh audit output required")
    value = {"kind": "S003LogicalDefenseSourceRunAudit", "status": "PassedSourceOnlyObservation",
             "receipt_sha256": receipt_hash, "result_sha256": result_hash, "request_sha256": request_hash,
             "aria_trace_sha256": trace_hash, "validator_census_sha256": census_hash,
             "trace_hashes_bound_by_original_run_receipt": False, "world_id": world,
             "accepted_building_id": 15, "placement_origin": moved["receipt"]["origin"],
             "materials_before": 370, "materials_after": 320, "census_tick": later["tick"],
             "source_outcome": "None", "native_control_visibility_proven": False,
             "native_touch_observed": False, "source_fit_certified": False, "mission_ready": False}
    with output.open("x") as stream:
        json.dump(value, stream, indent=2)
        stream.write("\n")
    print(json.dumps(value))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    audit(args.directory, args.output)

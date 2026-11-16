"""Qualify one instrumented S073 AI-startup observation, not player readiness."""
import argparse
import hashlib
import json
from pathlib import Path
import re


def sha(data): return hashlib.sha256(data).hexdigest()


def qualify(repo, directory, wrapper_exit):
    repo, directory = Path(repo).resolve(), Path(directory).resolve()
    raw, log = (directory / "snapshot.json").read_bytes(), (directory / "editor.log").read_bytes()
    data = json.loads(raw)
    if wrapper_exit != 0 or b"[S073NativeAIStartupSnapshot] result=Passed scope=instrumented_startup " not in log:
        raise ValueError("Successful checked wrapper and native AI marker required")
    if (data.get("kind") != "S073NativeAIStartupSnapshot" or data.get("schema_version") != 1 or
            data.get("scope") != "OriginalS073InstrumentedStartupSnapshot_NotNormalInputOrReadiness" or
            data.get("scenario_index") != 3 or data.get("seed") != 104729 or data.get("phase") != "Playing" or
            data.get("map_id") != "opmap.skirmish.industrial_basin" or data.get("source_map_id") != "opmap.skirmish.desert_base_01" or
            data.get("source_identity_hash") != "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900" or
            data.get("gameplay_start_complete") is not True or data.get("gameplay_start_failed") is not False or
            data.get("normal_input_playthrough") is not False or data.get("player_readiness") is not False or
            not re.fullmatch(r"[0-9a-f]{32}", data.get("session_id", ""))):
        raise ValueError("Exact instrumented S073 startup identity required")
    pins = data.get("source_pins_sha256")
    if not isinstance(pins, dict) or len(pins) < 20:
        raise ValueError("Native AI source pins required")
    for path, digest in pins.items():
        resolved = (repo / path).resolve(strict=True)
        if not resolved.is_relative_to(repo) or sha(resolved.read_bytes()) != digest:
            raise ValueError("Native AI source changed: " + path)
    controls, plans = data.get("faction_control_entries"), data.get("ai_build_plans")
    settings, spawn = data.get("pending_ai_settings_snapshot"), data.get("post_startup_spawn_resolver_observations")
    if (data.get("faction_control_config_tag_count") != 1 or not isinstance(controls, list) or
            not isinstance(plans, list) or data.get("ai_build_plan_count") != len(plans) or
            not isinstance(settings, dict) or not isinstance(spawn, list) or not controls or not plans or
            len({row.get("FactionId") for row in controls}) != len(controls) or
            len({row.get("FactionId") for row in plans}) != len(plans) or
            any(not isinstance(row.get("entries_ordered"), list) for row in plans)):
        raise ValueError("Complete unique observed controls, plans and settings required")
    return {"kind": "S073NativeAIStartupSnapshotReceipt", "schema_version": 1,
            "validation_status": "Passed", "wrapper_exit_code": 0, "required_marker_present": True,
            "snapshot_sha256": sha(raw), "editor_log_sha256": sha(log), "source_pin_count": len(pins),
            "session_id": data["session_id"], "normal_input_playthrough": False, "mission_ready": False,
            "scope": "Instrumented same-session startup observation, not player or device acceptance"}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--directory", required=True)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    args = parser.parse_args()
    result = qualify(args.repo, args.directory, args.wrapper_exit)
    with (Path(args.directory) / "snapshot-receipt.json").open("x") as output:
        json.dump(result, output, indent=2)
        output.write("\n")
    print(json.dumps(result))


if __name__ == "__main__": main()

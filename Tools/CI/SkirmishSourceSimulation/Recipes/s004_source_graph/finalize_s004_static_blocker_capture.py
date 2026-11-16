#!/usr/bin/env python3
"""Check the isolated S004 subscene blocker-input capture, without admission."""
import argparse
import hashlib
import json
from pathlib import Path
import re


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def need(value, message):
    if not value: raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", required=True); parser.add_argument("--wrapper-exit", required=True, type=int)
    parser.add_argument("--output", required=True); args = parser.parse_args()
    directory = Path(args.directory).resolve(strict=True); output = Path(args.output).absolute()
    need(args.wrapper_exit == 0 and not output.exists(), "Successful wrapper and fresh output required")
    capture, log = directory / "data/static-blockers.json", directory / "editor.log"
    before = {str(path): sha(path) for path in (capture, log)}
    data = json.loads(capture.read_text()); text = log.read_text(errors="replace")
    markers = re.findall(r"\[S004StaticBlockerSourceCapture\] result=Collected blockers=(\d+) nonBuilding=(\d+) world=0 readiness=0", text)
    need(len(markers) == 1, "One exact source-capture marker required")
    count, nonbuilding = map(int, markers[0]); rows = data.get("blockers")
    need(data.get("kind") == "S004IsolatedStaticBlockerSourceCapture" and data.get("status") == "CollectedSourceOnly" and data.get("scene_guid") == "cd836ff2ddd2a440e81db4a20332d266", "Exact source subscene capture required")
    need(isinstance(rows, list) and len(rows) == count == data.get("blocker_count") and nonbuilding == data.get("non_building_count") and count > 0, "Marker/blocker count mismatch")
    need(sum(not row.get("building") for row in rows) == nonbuilding, "Nonbuilding count mismatch")
    entities = set(); stable = set()
    for row in rows:
        need(row.get("source_section_owned") is True and isinstance(row.get("source_entity"), str) and row["source_entity"] not in entities, "Blocker ownership or entity identity invalid")
        entities.add(row["source_entity"])
        for name in ("origin", "unit_grid", "size"):
            need(isinstance(row.get(name), list) and len(row[name]) == 2 and all(type(x) is int for x in row[name]), "Invalid blocker " + name)
        need(all(x > 0 for x in row["size"]), "Blocker footprint must be positive")
        if row.get("building"):
            need(isinstance(row.get("stable_id"), str) and row["stable_id"] and row["stable_id"] not in stable, "Building stable identity invalid")
            stable.add(row["stable_id"])
        else: need(row.get("stable_id") is None, "Nonbuilding stable identity invalid")
        need(type(row.get("friendly_pass_present")) is bool and type(row.get("opening_present")) is bool, "Blocker optional flags invalid")
        need((row.get("friendly_pass_faction") is not None) == row["friendly_pass_present"] and (row.get("opening") is not None) == row["opening_present"], "Blocker optional data mismatch")
    pins = data.get("source_pins"); need(isinstance(pins, dict) and len(pins) > 1000, "Source pin closure required")
    for path, digest in pins.items(): need(Path(path).is_file() and sha(path) == digest, "Source pin changed: " + path)
    for flag in ("gameplay_state_copied", "live_session_observed", "physical_source_validated", "source_fit_certified", "mission_ready"):
        need(data.get(flag) is False, "Source scope invalid: " + flag)
    need({str(path): sha(path) for path in (capture, log)} == before, "Capture changed during qualification")
    result = {"status": "PassedIsolatedBakedBlockerCapture", "wrapper_exit_code": args.wrapper_exit, "required_marker_present": True,
              "source_pins_checked": len(pins), "blocker_count": count, "building_count": len(stable), "non_building_count": nonbuilding,
              "opening_count": sum(row["opening_present"] for row in rows), "capture_sha256": before[str(capture)], "log_sha256": before[str(log)],
              "gameplay_state_copied": False, "live_session_observed": False, "physical_source_validated": False, "source_fit_certified": False, "mission_ready": False}
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x") as stream: json.dump(result, stream, indent=2); stream.write("\n")
    print(json.dumps(result))


if __name__ == "__main__": main()

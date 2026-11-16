#!/usr/bin/env python3
"""Compare original S004 production slots against the checked shared factory capture."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[5]
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_metadata import qualify

DIRECTORY = ROOT / "Logs/SkirmishProduction/s004-current-production-slots-20261104-r1"
CAPTURE, LOG = DIRECTORY / "data/production-slots.json", DIRECTORY / "editor.log"
S3 = ROOT / "Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-r3/data/s003-native-metadata.json"
S3RECEIPT = ROOT / "T Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-T3/receipt.json"
JOIN = ROOT / "T Logs/SkirmishProduction/s004-current-factory-template-join-20261104-T1/receipt.json"
MARKER = "[S004ProductionSlotReference] result=Collected buildings=9 unsupported=0 mission=0 readiness=0"


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def need(ok, message):
    if not ok: raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--wrapper-exit", required=True, type=int); parser.add_argument("--output", required=True)
    args = parser.parse_args(); output = Path(args.output).absolute()
    need(args.wrapper_exit == 0 and not output.exists(), "Successful wrapper and fresh output required")
    inputs = (CAPTURE, LOG, S3, S3RECEIPT, JOIN)
    before = {str(path): sha(path) for path in inputs}
    need(LOG.read_text(errors="replace").splitlines().count(MARKER) == 1, "Exact capture marker required")
    capture, receipt, join = (json.loads(path.read_text()) for path in (CAPTURE, S3RECEIPT, JOIN))
    need(capture.get("kind") == "S004OriginalProductionSlots" and capture.get("authority") == "OriginalS004ProductionOverlayAndDefinitionFactory" and capture.get("setup_hash") == 1130726651 and capture.get("seed") == 104733, "Exact S004 source capture required")
    need(capture.get("mission_world_created") is False and capture.get("mission_ready") is False and capture.get("runway_reader_supplied") is False and capture.get("unsupported_values") == [], "Capture scope changed")
    pins = capture.get("source_pins"); need(isinstance(pins, dict) and len(pins) > 1000, "Source pin closure required")
    for path, value in pins.items():
        resolved = Path(path) if Path(path).is_absolute() else ROOT / path
        need(resolved.is_file() and sha(resolved) == value, "Source pin changed: " + path)
    need(qualify(ROOT, S3, Path(receipt["editor_log"]["path"]), Path(receipt["source_compiler_receipt"]["path"]), 0) == receipt, "Shared factory capture changed")
    need(join.get("status") == "PassedSourceOnlyIdentityJoin" and join["input_sha256"][str(S3)] == before[str(S3)] and join.get("building_identities_and_footprints_matched") == 9, "Checked building identity join required")
    shared = {row["prefab"]["key"]: row for row in json.loads(S3.read_text())["context"]["metadata"]["buildings"]}
    rows = capture.get("buildings"); need(isinstance(rows, list) and len(rows) == 9, "Nine S004 building definitions required")
    details = []; total = 0
    for row in rows:
        key = row["prefab"]["key"]
        need(key in shared and row["prefab"] == shared[key]["prefab"], "Prefab identity differs: " + key)
        need(row["production_slots"] == shared[key]["fields"]["ProductionSlots"], "Original production slot payload differs: " + key)
        count = len(row["production_slots"].get("items", [])); total += count
        details.append({"building": key, "slots": count})
    need(len({row["building"] for row in details}) == 9 and total == 27, "Slot domain changed")
    need({str(path): sha(path) for path in inputs} == before, "Capture inputs changed during verification")
    result = {"status": "PassedExactSourceFactorySlotParity", "wrapper_exit_code": 0, "marker_present": True,
              "source_pins_checked": len(pins), "building_count": 9, "slot_count": total, "buildings": details,
              "input_sha256": before, "runway_reader_supplied": False, "mission_world_created": False,
              "runtime_admission": False, "source_fit_certified": False, "mission_ready": False}
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x") as stream: json.dump(result, stream, indent=2); stream.write("\n")
    print(json.dumps({key: result[key] for key in ("status", "building_count", "slot_count", "source_pins_checked")}))


if __name__ == "__main__": main()

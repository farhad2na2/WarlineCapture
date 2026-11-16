#!/usr/bin/env python3
"""Compare all original S004 definition fields to the checked shared capture."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[5]
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_metadata import qualify

DIRECTORY = ROOT / "Logs/SkirmishProduction/s004-current-full-factory-runway-20261104-r1"
CAPTURE, LOG = DIRECTORY / "data/full-factory-runway-definitions.json", DIRECTORY / "editor.log"
S3 = ROOT / "Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-r3/data/s003-native-metadata.json"
S3RECEIPT = ROOT / "T Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-T3/receipt.json"
MARKER = "[S004FullFactoryRunwayReference] result=Collected buildings=9 unsupported=0 mission=0 readiness=0"


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def need(ok, message):
    if not ok: raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--wrapper-exit", required=True, type=int); parser.add_argument("--output", required=True)
    args = parser.parse_args(); output = Path(args.output).absolute()
    need(args.wrapper_exit == 0 and not output.exists(), "Successful wrapper and fresh output required")
    inputs = (CAPTURE, LOG, S3, S3RECEIPT); before = {str(path): sha(path) for path in inputs}
    need(LOG.read_text(errors="replace").splitlines().count(MARKER) == 1, "Exact full factory marker required")
    actual = json.loads(CAPTURE.read_text()); shared_receipt = json.loads(S3RECEIPT.read_text())
    need(actual.get("kind") == "S004OriginalFullFactoryRunwayDefinitions" and actual.get("authority") == "OriginalS004ProductionOverlayAndDefinitionFactory" and actual.get("setup_hash") == 1130726651 and actual.get("seed") == 104733, "Exact original S004 context required")
    need(actual.get("unsupported_values") == [] and actual.get("runway_reader_supplied") is True and actual.get("mission_world_created") is False and actual.get("mission_ready") is False, "Source scope or unsupported data changed")
    pins = actual.get("source_pins"); need(isinstance(pins, dict) and len(pins) > 1000, "Factory source closure required")
    for path, expected in pins.items():
        path = Path(path) if Path(path).is_absolute() else ROOT / path
        need(path.is_file() and sha(path) == expected, "S004 source pin changed: " + str(path))
    need(qualify(ROOT, S3, Path(shared_receipt["editor_log"]["path"]), Path(shared_receipt["source_compiler_receipt"]["path"]), 0) == shared_receipt, "Shared factory evidence changed")
    shared_rows = json.loads(S3.read_text())["context"]["metadata"]["buildings"]
    shared = {row["prefab"]["key"]: row for row in shared_rows}
    need(len(shared) == len(shared_rows), "Duplicate shared factory keys")
    rows = actual.get("buildings"); need(isinstance(rows, list) and len(rows) == 9, "Nine source building definitions required")
    differences = []; fields_compared = 0; identities = set()
    for row in rows:
        prefab = row["prefab"]; key = prefab["key"]
        need(key not in identities and key in shared and prefab == shared[key]["prefab"], "Building identity mismatch: " + key)
        identities.add(key)
        original, other = row["fields"], shared[key]["fields"]
        for field in sorted(set(original) | set(other)):
            fields_compared += 1
            if field not in original or field not in other or original[field] != other[field]:
                differences.append({"building": key, "field": field, "s004_present": field in original, "shared_present": field in other,
                                    "s004_value": original.get(field), "shared_value": other.get(field)})
    need({str(path): sha(path) for path in inputs} == before, "Capture evidence changed during comparison")
    result = {"status": "PassedExactSourceFactoryFieldParity" if not differences else "SourceFactoryFieldDifferencesObserved",
              "wrapper_exit_code": 0, "marker_present": True, "source_pins_checked": len(pins), "buildings_compared": len(rows),
              "fields_compared": fields_compared, "differences": differences, "input_sha256": before,
              "s004_runway_reader_supplied": True, "mission_world_created": False, "runtime_admission": False,
              "source_fit_certified": False, "mission_ready": False}
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x") as stream: json.dump(result, stream, indent=2); stream.write("\n")
    print(json.dumps({"status": result["status"], "buildings_compared": len(rows), "fields_compared": fields_compared, "difference_count": len(differences)}))


if __name__ == "__main__": main()

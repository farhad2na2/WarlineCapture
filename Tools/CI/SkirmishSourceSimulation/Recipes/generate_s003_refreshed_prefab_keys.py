#!/usr/bin/env python3
"""Extract ordered S003 prefab keys from the checked current native factory capture."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_metadata import qualify


def descriptor(path):
    return {"path": str(path.resolve(strict=True)), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def extract(capture):
    rows = capture.get("context", {}).get("metadata", {}).get("units")
    if not isinstance(rows, list) or len(rows) != 19:
        raise ValueError("Exactly 19 actual S003 factory unit rows required")
    identities = []
    for index, row in enumerate(rows):
        prefab = row.get("prefab", {})
        if (row.get("index") != index or prefab.get("codec") != "source-asset-reference-v1"
            or prefab.get("type") != "UnityEngine.GameObject" or not isinstance(prefab.get("key"), str)
            or not prefab["key"] or not re.fullmatch(r"[0-9a-f]{32}", str(prefab.get("guid", "")))
            or type(prefab.get("local_id")) is not int or prefab["local_id"] == 0):
            raise ValueError("Complete ordered native prefab identity required")
        identities.append({"index": index, "key": prefab["key"], "guid": prefab["guid"],
                           "local_id": prefab["local_id"], "path": prefab["path"]})
    if len({row["key"] for row in identities}) != len(identities):
        raise ValueError("Distinct exact prefab keys required")
    return identities


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    capture = ROOT / "Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-r3/data/s003-native-metadata.json"
    log = ROOT / "T Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-T3/editor.log"
    compiler = ROOT / "T Logs/SkirmishProduction/s003-current-original-compiler-20261104-T1/receipt.json"
    receipt = ROOT / "T Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-T3/receipt.json"
    if qualify(ROOT, capture, log, compiler, 0) != json.loads(receipt.read_bytes()):
        raise ValueError("Current native factory capture must requalify")
    identities = extract(json.loads(capture.read_bytes()))
    output = Path(args.output).absolute()
    output.mkdir(parents=True, exist_ok=False)
    keys = output / "prefab-keys.json"
    keys.write_text(json.dumps([row["key"] for row in identities], indent=2) + "\n")
    result = {"kind": "S003RefreshedPrefabKeyDescriptor", "status": "PassedSourceOnly",
              "capture": descriptor(capture), "capture_receipt": descriptor(receipt), "keys": descriptor(keys),
              "ordered_prefab_identities": identities, "runtime_admission": False,
              "source_fit_certified": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"status": result["status"], "keys": result["keys"], "count": len(identities),
                      "runtime_admission": False, "mission_ready": False}))


if __name__ == "__main__":
    main()

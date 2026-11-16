#!/usr/bin/env python3
"""Join retained unit metadata by exact prefab identity; never promote mission admission."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
INPUTS = {
    "factory": "Logs/SkirmishProduction/s004-current-full-factory-runway-20261104-r1/data/full-factory-runway-definitions.json",
    "s003": "T Logs/SkirmishProduction/s003-refreshed-factory-projection-20261104-T1/unit-production-metadata.json",
    "s002": "Logs/SkirmishProduction/s002-current-unit-metadata-20261104-T1/unit-production-metadata.json",
}

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def identity(value): return tuple(value.get(k) for k in ("key", "path", "guid", "local_id"))

def audit():
    paths = {name: ROOT / path for name, path in INPUTS.items()}
    hashes = {str(path): sha(path) for path in paths.values()}
    values = {name: json.loads(path.read_bytes()) for name, path in paths.items()}
    factory = values["factory"]
    if factory.get("setup_hash") != 1130726651 or factory.get("seed") != 104733:
        raise ValueError("Exact S004 compiled factory target required")
    pins = {}
    for data in values.values():
        source = data.get("source_pins")
        if not isinstance(source, dict) or not source: raise ValueError("Complete source pin maps required")
        for name, expected in source.items():
            path = Path(name); path = path if path.is_absolute() else ROOT / path
            if path.is_symlink() or not path.is_file() or sha(path) != expected: raise ValueError("Changed source pin: " + name)
            normalized = str(path.resolve())
            if normalized in pins and pins[normalized] != expected: raise ValueError("Conflicting source pin: " + name)
            pins[normalized] = expected
    units = {}
    for name in ("s003", "s002"):
        seen = set()
        for row in values[name]["units"]:
            key = row["key"]
            if key in seen or key != row["prefab"]["key"]: raise ValueError("Unique unit identity required")
            seen.add(key)
            projection = {k: row[k] for k in ("prefab", "definition_metadata", "production_metadata")}
            if key in units and units[key]["projection"] != projection: raise ValueError("Conflicting retained unit metadata: " + key)
            if key not in units: units[key] = {"projection": projection, "captures": []}
            units[key]["captures"].append(name)
    slots = []
    for building in factory["buildings"]:
        for index, slot in enumerate(building["fields"]["ProductionSlots"]["items"]):
            prefab = slot["fields"]["SpawnUnitPrefab"]
            key = prefab["key"]
            found = units.get(key)
            if found is not None and identity(found["projection"]["prefab"]) != identity(prefab):
                raise ValueError("Production prefab identity mismatch: " + key)
            slots.append({"building": building["prefab"]["key"], "slot": index, "key": key,
                          "prefab_identity": prefab, "retained_metadata_captures": found["captures"] if found else []})
    keys = {row["key"] for row in slots}
    if len(slots) != 27 or len(keys) != 20: raise ValueError("S004 production domain changed")
    if {str(path): sha(path) for path in paths.values()} != hashes: raise ValueError("Input changed during audit")
    return {"kind": "S004UnitMetadataCoverageAudit", "status": "CollectedSourceEvidenceOnly",
            "input_sha256": hashes, "unique_current_source_pins_checked": len(pins), "slots": slots,
            "retained_metadata_keys": sorted(keys.intersection(units)), "missing_metadata_keys": sorted(keys.difference(units)),
            "scope": "Exact identity and matching retained data only; no S004 metadata materialization, transport, native parity or runtime admission.",
            "runtime_admission": False, "mission_ready": False}

def main():
    parser = argparse.ArgumentParser(); parser.add_argument("--output", type=Path, required=True); args = parser.parse_args()
    result = audit()
    with args.output.open("x") as out: json.dump(result, out, indent=2); out.write("\n")
    print(json.dumps({"status":result["status"], "pins":result["unique_current_source_pins_checked"],
                      "covered":len(result["retained_metadata_keys"]), "missing":result["missing_metadata_keys"]}))

if __name__ == "__main__": main()

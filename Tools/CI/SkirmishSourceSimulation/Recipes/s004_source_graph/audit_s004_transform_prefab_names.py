#!/usr/bin/env python3
"""Compare S004 compiled production-slot names with retained prefab-capture names.

This is a name-level inventory only: it does not qualify runtime identity, native
prefab parity, the transform consumer readset, or mission readiness.
"""
import argparse
import base64
import hashlib
import json
from pathlib import Path


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def audit(bundle_path, expected_bundle_hash):
    raw = bundle_path.read_bytes()
    if digest(raw) != expected_bundle_hash:
        raise ValueError("Exact source diagnostic bundle required")
    bundle = json.loads(raw)
    if bundle.get("kind") != "S004SourceDiagnosticInputBundle" or bundle.get("mission_id") != "S004":
        raise ValueError("Exact S004 source bundle required")
    inputs = {}
    descriptors = {}
    for name in ("building_definitions", "prefab_shared", "prefab_s004"):
        descriptor = bundle["inputs"][name]
        path = Path(descriptor["path"])
        if not path.is_absolute() or path.is_symlink():
            raise ValueError("Exact absolute source input required: " + name)
        data = path.read_bytes()
        if len(data) != descriptor["bytes"] or digest(data) != descriptor["sha256"]:
            raise ValueError("Changed source input: " + name)
        inputs[name] = json.loads(data)
        descriptors[name] = descriptor
    definitions = inputs["building_definitions"]
    if definitions.get("setup_hash") != 1130726651 or definitions.get("seed") != 104733:
        raise ValueError("Exact compiled S004 setup required")
    capture_keys = set()
    for name in ("prefab_shared", "prefab_s004"):
        capture = inputs[name]
        keys = capture["requested_prefab_keys"]
        if len(keys) != len(set(keys)) or set(keys) != set(capture["opening_prefabs"]):
            raise ValueError("Complete unique requested prefab keys required")
        if capture_keys.intersection(keys):
            raise ValueError("Overlapping capture keys require identity reconciliation")
        capture_keys.update(keys)
    slots = []
    for building in definitions["buildings"]:
        for index, item in enumerate(building["fields"]["ProductionSlots"]["items"]):
            fields = item["fields"]
            reference = fields["SpawnUnitPrefab"]
            source = fields["SpawnUnitSourceKey"]
            key = reference["key"]
            decoded = base64.b64decode(source["base64"], validate=True).decode("utf-8")
            if reference["codec"] != "source-asset-reference-v1" or not key or decoded.lower() != key.lower():
                raise ValueError("Production slot source key differs from prefab reference")
            slots.append({"building": building["prefab"]["key"], "slot": index,
                          "prefab_key": key, "path": reference["path"], "guid": reference["guid"],
                          "local_id": reference["local_id"], "captured_name": key in capture_keys})
    production = {row["prefab_key"] for row in slots}
    return {"kind": "S004TransformProductionPrefabNameAudit", "status": "CollectedSourceNames",
            "bundle": {"path": str(bundle_path.resolve()), "sha256": expected_bundle_hash},
            "inputs": descriptors, "production_slots": slots, "production_keys": sorted(production),
            "capture_keys": sorted(capture_keys), "missing_names": sorted(production - capture_keys),
            "scope": "Names only; no prefab identity, runtime transform, native parity or readiness qualification.",
            "runtime_admission": False, "mission_ready": False}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--bundle", type=Path, required=True)
    parser.add_argument("--bundle-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = audit(args.bundle, args.bundle_sha256)
    with args.output.open("x") as out:
        json.dump(result, out, indent=2)
        out.write("\n")
    print(json.dumps({"status": result["status"], "production_keys": len(result["production_keys"]),
                      "capture_keys": len(result["capture_keys"]), "missing_names": result["missing_names"]}))


if __name__ == "__main__":
    main()

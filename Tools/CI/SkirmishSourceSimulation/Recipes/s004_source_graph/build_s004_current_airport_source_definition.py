#!/usr/bin/env python3
"""Join current source-only S004 and S003 factory data without placing a runway."""
import argparse
import hashlib
import json
from pathlib import Path

import build_s004_current_role_catalog_metadata as role_builder

ROOT = role_builder.ROOT
FACTORY = ROOT / "Logs/SkirmishProduction/s004-post-planner-factory-20261105-r1/data/full-factory-runway-definitions.json"
FACTORY_AUDIT = ROOT / "T Logs/SkirmishProduction/s004-post-planner-captures-20261105-T1/factory-audit.json"
S003 = ROOT / "T Logs/SkirmishProduction/s003-post-planner-factory-projection-20261105-T4/building-definitions.json"
PROJECTION = S003.parent / "projection-receipt.json"
ROLE = ROOT / "T Logs/SkirmishProduction/s004-current-role-catalog-20261105-T2.json"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(",", ":"))


def build(source_build=None, prefab=None, prefab_audit=None, projection_path=None, metadata=None,
          factory_path=None, factory_audit_path=None, s003_path=None, role_path=None):
    factory_path=Path(FACTORY if factory_path is None else factory_path).resolve(strict=True)
    factory_audit_path=Path(FACTORY_AUDIT if factory_audit_path is None else factory_audit_path).resolve(strict=True)
    s003_path=Path(S003 if s003_path is None else s003_path).resolve(strict=True)
    projection_path=Path(PROJECTION if projection_path is None else projection_path).resolve(strict=True)
    role_path=Path(ROLE if role_path is None else role_path).resolve(strict=True)
    inputs = {str(path): sha(path) for path in (factory_path, factory_audit_path, s003_path, projection_path, role_path, Path(__file__).resolve())}
    role = json.loads(role_path.read_bytes())
    if canonical(role) != canonical(role_builder.build(source_build, prefab, prefab_audit, projection_path, metadata)):
        raise ValueError("Current source role join changed")
    factory, audit, captured, projection = (json.loads(p.read_bytes()) for p in (factory_path, factory_audit_path, s003_path, projection_path))
    if (audit.get("status") != "Passed" or audit.get("scope") != "CurrentDataOnlyFactoryCapture_NoRuntimeAdmission" or
        audit.get("capture_sha256") != sha(factory_path) or audit.get("wrapper_exit_observed") != 0 or
        audit.get("marker_count") != 1 or audit.get("mismatches") != [] or audit.get("mission_ready") is not False):
        raise ValueError("Current checked S004 factory capture required")
    if (projection.get("kind") != "S003IndependentCurrentFactoryMetadataProjection" or
        projection.get("status") != "PassedSourceOnlyProjection" or
        projection.get("artifacts", {}).get("building-definitions.json") != {"path":str(s003_path),"sha256":sha(s003_path)} or
        projection.get("runtime_admission") is not False or projection.get("mission_ready") is not False or
        captured.get("kind") != "NativeQualifiedSkirmishBuildingDefinitions" or
        factory.get("setup_hash") != 1130726651 or factory.get("seed") != 104733):
        raise ValueError("Exact current source-only factory inputs required")
    pins = {}
    for document in (factory, captured):
        for name, expected in document["source_pins"].items():
            path = Path(name)
            path = (path if path.is_absolute() else ROOT / path).resolve(strict=True)
            if str(path) in pins and pins[str(path)] != expected:
                raise ValueError("Conflicting source pin: " + str(path))
            pins[str(path)] = expected
    def check_pins():
        for path, expected in pins.items():
            if sha(path) != expected:
                raise ValueError("Factory source pin changed: " + path)
    check_pins()
    rows = {row["key"]: row for row in captured["definitions"]}
    actual = factory["buildings"]
    if len(actual) != 9 or len(rows) != 10 or any(row["prefab"]["key"] == "Building_Airport" for row in actual):
        raise ValueError("Exact starting factory domain required")
    for row in actual:
        key = row["prefab"]["key"]
        if key not in rows or rows[key]["fields"] != row["fields"]:
            raise ValueError("Factory metadata mismatch: " + key)
    airport = rows["Building_Airport"]
    fields = airport["fields"]
    if fields["Prefab"].get("key") != "Building_Airport" or fields["HasRunway"] != {"codec": "bool-v1", "value": True} or fields["IsWall"] != {"codec": "bool-v1", "value": False}:
        raise ValueError("Exact source airport identity required")
    slots = [item["fields"]["SpawnUnitPrefab"] for item in fields["ProductionSlots"]["items"]]
    if [item["key"] for item in slots] != ["Unit_Veh_Jet_02", "Unit_Veh_Jet_01", "Unit_Veh_Plane_Transport"]:
        raise ValueError("Source airport slot order changed")
    role_units = {row["key"]: row["prefab"] for row in role["units"]}
    if any(role_units.get(item["key"]) != item for item in slots):
        raise ValueError("Airport and role prefab identities differ")
    check_pins()
    if any(sha(path) != expected for path, expected in inputs.items()):
        raise ValueError("Input changed during airport join")
    return {"kind": "S004SourceAirportDefinitionCandidate", "authority": "CurrentAuditedFactories_SourceOnly",
            "catalog_id": "S004", "setup_hash": 1130726651, "seed": 104733, "airport": airport,
            "matching_starting_definition_count": 9, "source_pin_count": len(pins), "source_inputs": inputs,
            "live_runway_registry_validated": False, "source_fit_certified": False,
            "runtime_admission": False, "mission_ready": False,
            "scope": "Original source airport definition only; no placed airport or native S004 parity"}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    for name in ("source-build", "prefab", "prefab-audit", "projection-path", "metadata",
                 "factory-path", "factory-audit-path", "s003-path", "role-path"):
        parser.add_argument("--"+name,type=Path)
    args = parser.parse_args()
    values=(args.source_build,args.prefab,args.prefab_audit,args.projection_path,args.metadata,
            args.factory_path,args.factory_audit_path,args.s003_path,args.role_path)
    if any(value is not None for value in values) and not all(value is not None for value in values):
        raise ValueError("All nine explicit current evidence inputs are required together")
    result = build(*values)
    with args.output.open("x") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
    print(json.dumps({"status": "CollectedSourceOnly", "source_pins_checked": result["source_pin_count"],
                      "live_runway_registry_validated": False, "mission_ready": False}))

#!/usr/bin/env python3
"""Rebuild S004 source-only candidates from the checked shared-focus epoch.

The historical role census remains identified as historical. Every new asset
capture and the current source-build closure are checked by the original
builders. This does not establish native admission or mission readiness.
"""
import argparse
import hashlib
import json
from pathlib import Path

import build_s004_current_airport_source_definition as airports
import build_s004_current_role_catalog_metadata as roles
import build_s004_current_epoch_bundle as bundles

ROOT = roles.ROOT
ROLE = ROOT / "T Logs/SkirmishProduction/s004-shared-focus-role-catalog-20261105-T1.json"
AIRPORT = ROOT / "T Logs/SkirmishProduction/s004-shared-focus-airport-definition-20261105-T1.json"

def configure(terminal_handoff=False):
    global ROLE, AIRPORT
    ROLE=ROOT/("T Logs/SkirmishProduction/s004-terminal-handoff-role-catalog-20261105-T1.json" if terminal_handoff else "T Logs/SkirmishProduction/s004-shared-focus-role-catalog-20261105-T1.json")
    AIRPORT=ROOT/("T Logs/SkirmishProduction/s004-terminal-handoff-airport-definition-20261105-T1.json" if terminal_handoff else "T Logs/SkirmishProduction/s004-shared-focus-airport-definition-20261105-T1.json")
    roles.PREFAB = ROOT / "Logs/SkirmishProduction/s003-shared-focus-prefabs-20261105-r1/data/prefab-reference-data.json"
    roles.PREFAB_AUDIT = ROOT / "T Logs/SkirmishProduction/s003-shared-focus-prefabs-20261105-T1/independent-audit.json"
    roles.PROJECTION = ROOT / "T Logs/SkirmishProduction/s003-shared-focus-factory-projection-20261105-T1/projection-receipt.json"
    roles.METADATA = roles.PROJECTION.parent / "unit-production-metadata.json"
    roles.BUILD = ROOT / ("T Logs/SkirmishProduction/s004-terminal-handoff-s002-build-20261105-T1/build.json" if terminal_handoff else "T Logs/SkirmishProduction/shared-inspection-source-build-20261105-T2/build.json")
    airports.FACTORY = ROOT / "Logs/SkirmishProduction/s004-shared-focus-factory-20261105-r1/data/full-factory-runway-definitions.json"
    airports.FACTORY_AUDIT = ROOT / "T Logs/SkirmishProduction/s004-shared-focus-factory-20261105-T1/factory-audit.json"
    airports.S003 = roles.PROJECTION.parent / "building-definitions.json"
    airports.PROJECTION = roles.PROJECTION
    airports.ROLE = ROLE

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def canonical(value): return json.dumps(value,sort_keys=True,separators=(",",":"))

def verify(path, expected, kind, terminal_handoff=False):
    configure(terminal_handoff)
    actual_path = ROLE if kind == "role" else AIRPORT if kind == "airport" else None
    if actual_path is None or Path(path) != actual_path or not actual_path.is_absolute() or actual_path.is_symlink():
        raise ValueError("Exact shared-focus epoch candidate path required")
    if sha(actual_path) != expected:
        raise ValueError("Shared-focus epoch candidate hash changed")
    actual = json.loads(actual_path.read_bytes())
    rebuilt = roles.build() if kind == "role" else airports.build()
    if canonical(actual) != canonical(rebuilt):
        raise ValueError("Shared-focus epoch source join changed")
    return actual

def build(terminal_handoff=False):
    configure(terminal_handoff)
    if ROLE.exists() or AIRPORT.exists():
        raise ValueError("Fresh output paths required")
    role = roles.build()
    with ROLE.open("x") as stream:
        json.dump(role,stream,indent=2);stream.write("\n")
    airport = airports.build()
    with AIRPORT.open("x") as stream:
        json.dump(airport,stream,indent=2);stream.write("\n")
    print(json.dumps({"status":"CollectedSourceOnly","role_sha256":sha(ROLE),"airport_sha256":sha(AIRPORT),
        "historical_role_census":True,"native_admission":False,"mission_ready":False}))

def bundle(output, terminal_handoff=False):
    configure(terminal_handoff)
    bundles.S002=roles.BUILD
    bundles.S026=ROOT/("T Logs/SkirmishProduction/s004-terminal-handoff-s026-build-20261105-T1/build.json" if terminal_handoff else "T Logs/SkirmishProduction/shared-inspection-s026-build-20261105-T1/build.json")
    bundles.ROLE=ROLE;bundles.AIRPORT=AIRPORT
    bundles.REPLACEMENTS={
        "prefab_shared":roles.PREFAB,
        "prefab_s004":ROOT/"Logs/SkirmishProduction/s004-shared-focus-prefabs-20261105-r1/data/prefab-reference-data.json",
        "building_definitions":airports.FACTORY,
        "configured_building_readmodel":ROOT/"Logs/SkirmishProduction/s004-shared-focus-readmodel-20261105-r1/data/configured-spawnable-readmodel.json",
        "static_blockers":ROOT/"Logs/SkirmishProduction/s004-shared-focus-blockers-20261105-r1/data/static-blockers.json"}
    bundles.AUDITS={
        "prefab_shared":roles.PREFAB_AUDIT,
        "prefab_s004":ROOT/"T Logs/SkirmishProduction/s004-shared-focus-prefabs-20261105-T1/receipt.json",
        "building_definitions":airports.FACTORY_AUDIT,
        "configured_building_readmodel":ROOT/"T Logs/SkirmishProduction/s004-shared-focus-readmodel-20261105-T1/independent-audit.json",
        "static_blockers":ROOT/"T Logs/SkirmishProduction/s004-shared-focus-blockers-20261105-T1/receipt.json"}
    return bundles.build(output)

if __name__ == "__main__":
    parser=argparse.ArgumentParser();parser.add_argument("--bundle-output",type=Path);parser.add_argument("--terminal-handoff",action="store_true");args=parser.parse_args()
    if args.bundle_output: print(json.dumps(bundle(args.bundle_output.resolve(),args.terminal_handoff)))
    else: build(args.terminal_handoff)

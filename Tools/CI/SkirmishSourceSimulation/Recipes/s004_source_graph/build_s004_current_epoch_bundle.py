#!/usr/bin/env python3
"""Assemble current S004 source inputs; historical startup is evidence only."""
import argparse
import hashlib
import json
from pathlib import Path

import build_s004_current_role_catalog_metadata as role_builder
import build_s004_current_airport_source_definition as airport_builder

ROOT = role_builder.ROOT
OLD = ROOT / "T Logs/SkirmishProduction/s004-batch-refresh-bundle-20261104-T1/bundle.json"
OLD_SHA = "4ed5f9620309bd32fd636b90a977c89fe7849e019eeca7fcb1d87e078e280550"
S002 = ROOT / "T Logs/SkirmishProduction/s004-current-epoch-s002-build-20261105-T1/build.json"
S026 = ROOT / "T Logs/SkirmishProduction/s004-current-epoch-s026-build-20261105-T1/build.json"
ROLE = ROOT / "T Logs/SkirmishProduction/s004-current-role-catalog-20261105-T2.json"
AIRPORT = ROOT / "T Logs/SkirmishProduction/s004-current-airport-definition-20261105-T2.json"
CURRENT_SETUP = None
CURRENT_SETUP_RECEIPT = None
REPLACEMENTS = {
    "prefab_shared": ROOT / "Logs/SkirmishProduction/s003-post-planner-prefabs-20261105-r1/data/prefab-reference-data.json",
    "prefab_s004": ROOT / "Logs/SkirmishProduction/s004-post-planner-missing-prefabs-20261105-r1/data/prefab-reference-data.json",
    "building_definitions": ROOT / "Logs/SkirmishProduction/s004-post-planner-factory-20261105-r1/data/full-factory-runway-definitions.json",
    "configured_building_readmodel": ROOT / "Logs/SkirmishProduction/s004-post-planner-readmodel-20261105-r1/data/configured-spawnable-readmodel.json",
    "static_blockers": ROOT / "Logs/SkirmishProduction/s004-post-planner-blockers-20261105-r1/data/static-blockers.json",
}
AUDITS = {
    "prefab_shared": ROOT / "T Logs/SkirmishProduction/s003-post-planner-metadata-capture-20261104-T1/prefab-capture-audit-r1.json",
    "prefab_s004": ROOT / "T Logs/SkirmishProduction/s004-post-planner-captures-20261105-T1/missing-prefab-receipt.json",
    "building_definitions": ROOT / "T Logs/SkirmishProduction/s004-post-planner-captures-20261105-T1/factory-audit.json",
    "configured_building_readmodel": ROOT / "T Logs/SkirmishProduction/s004-post-planner-captures-20261105-T1/readmodel-audit.json",
    "static_blockers": ROOT / "T Logs/SkirmishProduction/s004-post-planner-captures-20261105-T1/blockers-audit.json",
}


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def descriptor(path):
    path = Path(path).resolve(strict=True)
    if not path.is_file() or path.is_symlink() or not path.is_relative_to(ROOT):
        raise ValueError("Regular repository input required: " + str(path))
    return {"path": str(path), "sha256": sha(path), "bytes": path.stat().st_size}


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(",", ":"))


def setup_schema(old, new):
    old_fields, new_fields = dict(old), dict(new)
    old_setup, new_setup = old_fields.pop('compiled_setup'), dict(new_fields.pop('compiled_setup'))
    if 'RequireAuthoredStructureOrigins' in new_setup:
        if new_setup.pop('RequireAuthoredStructureOrigins') is not False:
            raise ValueError('Authored-origin compatibility field must be false')
        schema = 'RetainedSetupWithDisabledCompatibilityField'
    else:
        schema = 'ExactRetainedSetupWithoutCompatibilityField'
    if old_fields != new_fields or old_setup != new_setup:
        raise ValueError('Current setup differs from the retained serialized export')
    return schema


def source_pins(document):
    pins = document.get("source_pins", {})
    if not isinstance(pins, dict):
        raise ValueError("Source pins must be an object")
    for name, expected in pins.items():
        path = Path(name)
        path = path if path.is_absolute() else ROOT / path
        if not path.is_file() or path.is_symlink() or sha(path) != expected:
            raise ValueError("Current source pin changed: " + name)
    return len(pins)


def build(output):
    if sha(OLD) != OLD_SHA:
        raise ValueError("Historical bundle changed")
    historical = json.loads(OLD.read_bytes())
    if historical.get("scope") != "SourceHostDiagnosticOnly" or historical.get("qualification", {}).get("mission_ready") is not False:
        raise ValueError("Historical source-only bundle required")
    evidence_path = Path(historical["evidence"]["path"])
    if sha(evidence_path) != historical["evidence"]["sha256"]:
        raise ValueError("Historical startup evidence changed")
    history = json.loads(evidence_path.read_bytes())
    requests = evidence_path.parent / "requests.json"
    if (history.get("status") != "ObservedDiagnosticSourcePlacement" or history.get("mission_ready") is not False or
        sha(requests) != history.get("requests_sha256")):
        raise ValueError("Retained historical startup receipt required")
    setup_override = None
    if (CURRENT_SETUP is None) != (CURRENT_SETUP_RECEIPT is None):
        raise ValueError("Current setup export and compiler receipt must be supplied together")
    if CURRENT_SETUP is not None:
        current=Path(CURRENT_SETUP).resolve(strict=True); proof=Path(CURRENT_SETUP_RECEIPT).resolve(strict=True)
        current_desc=descriptor(current); proof_desc=descriptor(proof)
        checked=json.loads(proof.read_bytes()); prior_desc=historical['inputs']['compiled_setup']
        prior=Path(prior_desc['path'])
        if sha(prior)!=prior_desc['sha256']:
            raise ValueError("Historical compiled setup changed")
        for key in ('source_build','source_graph','capture_replay_receipt','native_capture_receipt','compiled_setup'):
            item=checked.get(key,{})
            if not isinstance(item,dict) or not Path(item.get('path','')).is_file() or sha(item['path'])!=item.get('sha256'):
                raise ValueError("Current setup compiler evidence changed: "+key)
        if (checked.get('status')!='Passed' or checked.get('compiled_setup',{}).get('path')!=str(current) or
            checked.get('source_build',{}).get('path')!=str(S002.resolve(strict=True)) or
            any(checked.get(key) is not False for key in ('world_initialized','physical_source_fit_verified','mission_readiness'))):
            raise ValueError("Exact current source-only setup export required")
        old=json.loads(prior.read_bytes()); new=json.loads(current.read_bytes())
        # Current source no longer serializes the older authored-origin flag.
        # Accept its absence only when every serialized setup field is exactly
        # the retained export; never manufacture a value for the missing field.
        schema = setup_schema(old, new)
        setup_override={'capture':current_desc,'compiler_receipt':proof_desc,'serialized_schema':schema}
    role = json.loads(ROLE.read_bytes())
    airport = json.loads(AIRPORT.read_bytes())
    if canonical(role) != canonical(role_builder.build()) or canonical(airport) != canonical(airport_builder.build()):
        raise ValueError("Current role or airport join changed")
    checks = {}
    for key, path in REPLACEMENTS.items():
        audit = json.loads(AUDITS[key].read_bytes())
        expected_status = {"prefab_shared": "PassedDataOnlyCapture", "prefab_s004": "PassedScopedSourceOnlyCapture",
                           "building_definitions": "Passed", "configured_building_readmodel": "Passed",
                           "static_blockers": "PassedIsolatedBakedBlockerCapture"}[key]
        audit_hash = audit.get("capture", {}).get("sha256") if key == "prefab_shared" else (
            audit.get("input_sha256", {}).get(str(path)) if key == "prefab_s004" else audit.get("capture_sha256"))
        marker = audit.get("required_marker_present") if key in ("prefab_shared", "static_blockers") else (
            audit.get("marker_present") if key == "prefab_s004" else audit.get("marker_count") == 1)
        if audit.get("status") != expected_status or audit_hash != sha(path) or marker is not True or audit.get("mission_ready") is not False:
            raise ValueError("Current capture audit differs: " + key)
        checks[key] = {"capture": descriptor(path), "audit": descriptor(AUDITS[key]),
                       "current_source_pin_entries": source_pins(json.loads(path.read_bytes()))}
    inputs = {}
    for key, prior in historical["inputs"].items():
        path = Path(CURRENT_SETUP).resolve(strict=True) if key=='compiled_setup' and setup_override else REPLACEMENTS.get(key, Path(prior["path"]))
        if key not in REPLACEMENTS and not (key=='compiled_setup' and setup_override) and sha(path) != prior["sha256"]:
            raise ValueError("Retained non-replaced source input changed: " + key)
        inputs[key] = descriptor(path)
        if key not in REPLACEMENTS and path.suffix == ".json":
            source_pins(json.loads(path.read_bytes()))
    builds = {}
    for name, path in (("s002", S002), ("s026", S026)):
        build = json.loads(path.read_bytes())
        if build.get("status") != "Passed" or build.get("compile_exit") != 0:
            raise ValueError("Fresh source build required: " + name)
        for source, expected in build["source_and_reference_pins"].items():
            if sha(source) != expected:
                raise ValueError("Fresh source-build pin changed: " + source)
        builds[name] = descriptor(path)
    output = Path(output).absolute()
    output.mkdir(parents=True, exist_ok=False)
    evidence = {"kind": "S004CurrentSourceInputEvidence", "scope": "CurrentCaptures_HistoricalStartup_NoCurrentWorld",
                "historical_bundle": descriptor(OLD), "historical_startup": descriptor(evidence_path),
                "historical_requests": descriptor(requests), "current_captures": checks,
                "source_builds": builds, "role_catalog": descriptor(ROLE), "airport_definition": descriptor(AIRPORT),
                "builder": descriptor(Path(__file__).resolve()),
                "current_world_observed": False, "runtime_admission": False, "mission_ready": False}
    if setup_override is not None:
        evidence['current_setup_export']=setup_override
    for row in (*inputs.values(), *(entry["audit"] for entry in checks.values()),
                *builds.values(), evidence["historical_bundle"], evidence["historical_startup"],
                evidence["historical_requests"], evidence["role_catalog"], evidence["airport_definition"], evidence["builder"],
                *((setup_override['compiler_receipt'],) if setup_override else ())):
        if descriptor(row["path"]) != row:
            raise ValueError("Source input changed during bundle assembly: " + row["path"])
    for row in inputs.values():
        path = Path(row["path"])
        if path.suffix == ".json":
            source_pins(json.loads(path.read_bytes()))
    for path in (S002, S026):
        for source, expected in json.loads(path.read_bytes())["source_and_reference_pins"].items():
            if sha(source) != expected:
                raise ValueError("Source-build pin changed during assembly: " + source)
    evidence_path = output / "input-evidence.json"
    evidence_path.write_text(json.dumps(evidence, indent=2) + "\n")
    bundle = {"kind": "S004SourceDiagnosticInputBundle", "schema_version": 1, "mission_id": "S004",
              "scope": "SourceHostDiagnosticOnly", "evidence": descriptor(evidence_path), "inputs": inputs,
              "missing_or_assumed": historical["missing_or_assumed"], "qualification": historical["qualification"]}
    path = output / "bundle.json"
    path.write_text(json.dumps(bundle, indent=2) + "\n")
    return {"status": "CollectedCurrentInputs", "bundle": descriptor(path), "evidence": descriptor(evidence_path),
            "current_world_observed": False, "runtime_admission": False, "mission_ready": False}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    print(json.dumps(build(args.output)))

#!/usr/bin/env python3
"""Bind one shared rifle movement projection to retained S003/S002 prefab imports."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys

RAW = Path("Logs/SkirmishProduction/s003-current-focused-prefab-capture-20261103-r1/data/prefab-reference-data.json")
RAW_SHA = "9b31b6863e80d3bd548cd78631a3385b1c7d0ed007da1785c00f1cb16d7a323e"
STRICT = Path("Logs/SkirmishProduction/s003-current-focused-prefab-capture-20261103-r1/strict-receipt.json")
STRICT_SHA = "824c296c3b993e1d18cad419c43ffbd1c97bf91988fd7ba8a4e1071b3ad5b1ee"
KEYS = Path("T Logs/SkirmishProduction/s003-current-prefab-key-descriptor-20261103-r1/prefab-keys.json")
KEYS_SHA = "38bd886005c62bc121d165e1119a3f8876b456eddaa0ef873a1a5681aa66539b"
KEY_RECEIPT = Path("T Logs/SkirmishProduction/s003-current-prefab-key-descriptor-20261103-r1/receipt.json")
KEY_RECEIPT_SHA = "c676b2a726ea8134f72189d37e46ecb552e34eec8524b7b033c2464540eee181"
RIFLE_KEY = "Unit_Chr_Soldier_Male_02_Alt_04"
CONFIG = Path("Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset")
CONFIG_META = Path(str(CONFIG) + ".meta")
PREFAB = Path("Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab")
AUTHORING = Path("Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs")
AUTHORING_VISUALS = Path("Assets/Game/Scripts/Authorings/UnitGridAuthoring.BakerVisuals.cs")
HELPER = Path("Assets/Game/Scripts/Components/UnitMoveAuthoringProjection.cs")
HELPER_META = Path(str(HELPER) + ".meta")
COMBAT_HEALTH_HELPER = Path("Assets/Game/Scripts/Components/UnitCombatHealthAuthoringProjection.cs")
COMBAT_HEALTH_HELPER_META = Path(str(COMBAT_HEALTH_HELPER) + ".meta")
ATTACK_HELPER = Path("Assets/Game/Scripts/Components/UnitAttackAuthoringProjection.cs")
ATTACK_HELPER_META = Path(str(ATTACK_HELPER) + ".meta")
COMBAT_COMPONENTS = Path("Assets/Game/Scripts/Components/UnitCombatComponents.cs")
CONFIG_MODELS = Path("Assets/Game/Scripts/Configs/GameplayConfigModels.cs")
MANIFESTS = [Path("Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json"),
             Path("Tools/CI/SkirmishSourceSimulation/source_manifest.s003.plan.json")]
HARNESS = Path("Tools/CI/SkirmishSourceSimulation/Tests/S003FocusedTemplateSourceHostControls.cs")
IMPORTER = Path("Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.s003.cs")
IMPORTER_PROJECTION = Path("Tools/CI/SkirmishSourceSimulation/Adapters/GeneratedReferenceTemplateImporter.UnitMoveProjection.cs")
SOURCE_ADAPTER = Path("Tools/CI/SkirmishSourceSimulation/Adapters/SourceUnitMoveAuthoringSource.cs")
STARTUP = Path("Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/307_GeneratedS002StartingWorld.lifetime.s003.cs")
AUTHORITY = Path("Tools/CI/SkirmishSourceSimulation/Adapters/S026/489-513_FreshSourceStorageLifetimeAuthority.s003.cs")
PASS = ("[S003FocusedTemplateSourceHostControls] result=Passed roots=19 entities=647 components=3965 "
        "unsupportedLeaves=32 omittedVfxComponents=26 cleanup=Passed preflightRejected=2 world=0 mission=0 readiness=0")


def need(ok, reason):
    if not ok:
        raise ValueError(reason)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def load(path):
    def unique(pairs):
        result = {}
        for k, v in pairs:
            need(k not in result, "duplicate JSON property: " + k)
            result[k] = v
        return result
    return json.loads(Path(path).read_text(encoding="utf-8"), object_pairs_hook=unique)


def one_float(text, name):
    matches = re.findall(rf"(?m)^\s*{re.escape(name)}:\s*([-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?)\s*$", text)
    need(len(matches) == 1, "expected one exact authored float field: " + name)
    return float(matches[0])


def one_bool(text, name):
    matches = re.findall(rf"(?m)^\s*{re.escape(name)}:\s*([01])\s*$", text)
    need(len(matches) == 1, "expected one exact authored boolean field: " + name)
    return matches[0] == "1"


def one_int(text, parent, child):
    matches = re.findall(rf"(?m)^\s*{re.escape(parent)}:\s*\{{[^\r\n]*?\b{re.escape(child)}:\s*(-?\d+)[^\r\n]*\}}\s*$", text)
    need(len(matches) == 1, "expected one exact authored integer field: " + parent + "." + child)
    return int(matches[0])


def method_body(source, signature):
    start = source.find(signature)
    need(start >= 0, "missing authoring method: " + signature)
    opening = source.find("{", start + len(signature))
    need(opening >= 0, "method body missing: " + signature)
    depth = 0
    for i in range(opening, len(source)):
        if source[i] == "{":
            depth += 1
        elif source[i] == "}":
            depth -= 1
            if depth == 0:
                return source[opening:i + 1]
    raise ValueError("unbalanced authoring method: " + signature)


def create_source_input(repo, output, raw):
    config_path, meta_path, prefab_path, authoring_path, helper_path = [repo / p for p in (CONFIG, CONFIG_META, PREFAB, AUTHORING, HELPER)]
    cfg, meta, prefab, authoring = [p.read_text(encoding="utf-8") for p in (config_path, meta_path, prefab_path, authoring_path)]
    guid_match = re.search(r"(?m)^guid: ([0-9a-f]{32})$", meta)
    need(guid_match is not None, "rifle config meta GUID missing")
    guid = guid_match.group(1)
    need(prefab.count("guid: " + guid) == 1, "rifle prefab must reference exactly this config GUID once")
    need(re.search(r"(?m)^\s*m_Name: Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config$", cfg), "unexpected config asset identity")
    apply_config = method_body(authoring, "private void ApplyConfigIfAvailable()")
    bake = method_body(authoring, "public override void Bake(UnitGridAuthoring authoring)")
    compact_bake = re.sub(r"\s+", "", bake)
    for exact in ("speed = config.Speed;", "walkSpeed = config.WalkSpeed;",
                  "roadSpeedMultiplier = config.RoadSpeedMultiplier;", "arriveDistance = config.ArriveDistance;",
                  "allowIdleWander = config.AllowIdleWander;", "autoCalculateFootprint = config.AutoCalculateFootprint;",
                  "footprintCells = config.FootprintCells;", "usesVehicleMotion = config.UsesVehicleMotion;"):
        need(exact in apply_config, "ApplyConfigIfAvailable does not copy authored value: " + exact)
    need("UnitMoveAuthoringProjection.FromAuthoredMovement(" in bake,
         "original baker does not consume complete shared movement projection")
    need("boolhasModelBounds=TryGetModelLocalBounds(authoring,outmodelBounds);" in compact_bake and
         "UnitMoveAuthoringProjection.FromAuthoredMovement(authoring.speed,authoring.walkSpeed,authoring.roadSpeedMultiplier,authoring.arriveDistance,authoring.footprintCells.x,authoring.footprintCells.y,authoring.autoCalculateFootprint,authoring.UsesVehicleMotion,authoring.allowIdleWander,hasModelBounds,modelBounds.size.x,modelBounds.size.z)" in compact_bake,
         "Baker must pass ordered raw fields and actual bounds inputs to the shared projection")

    values = {"speed": one_float(cfg, "speed"), "walkSpeed": one_float(cfg, "walkSpeed"),
              "roadSpeedMultiplier": one_float(cfg, "roadSpeedMultiplier"),
              "arriveDistance": one_float(cfg, "arriveDistance")}
    source_pins = raw.get("source_pins", {})
    must_pin = [CONFIG, CONFIG_META, PREFAB]
    capture_pins = {}
    for rel in must_pin:
        candidates = [k for k in source_pins if Path(k).as_posix().endswith(rel.as_posix())]
        current = sha(repo / rel)
        need(len(candidates) >= 1 and all(source_pins[key] == current for key in candidates),
             "S003 capture lacks a unique consistent historical pin for " + str(rel))
        capture_pins[str(rel.as_posix())] = current

    pins = [config_path, meta_path, prefab_path, authoring_path, repo / AUTHORING_VISUALS, helper_path, repo / HELPER_META,
            repo / COMBAT_HEALTH_HELPER, repo / COMBAT_HEALTH_HELPER_META, repo / COMBAT_COMPONENTS,
            repo / ATTACK_HELPER, repo / ATTACK_HELPER_META, repo / CONFIG_MODELS,
            repo / SOURCE_ADAPTER, repo / IMPORTER_PROJECTION, *[repo / path for path in MANIFESTS]]
    descriptor = {
        "schema_version": 2,
        "kind": "StrictUnitMovementAuthoringSourceInput",
        "root_key": RIFLE_KEY,
        "config_guid": guid,
        "values_bits": {key: struct.pack("<f", value)[::-1].hex() for key, value in values.items()},
        "movement_values": {
            "allowIdleWander": one_bool(cfg, "allowIdleWander"),
            "usesVehicleMotion": one_bool(cfg, "usesVehicleMotion"),
            "autoCalculateFootprint": one_bool(cfg, "autoCalculateFootprint"),
            "footprintX": one_int(cfg, "footprintCells", "x"),
            "footprintY": one_int(cfg, "footprintCells", "y"),
        },
        "captured_source_pins": capture_pins,
        "source_pins": {str(p.relative_to(repo).as_posix()): sha(p) for p in pins},
        "semantics": "ApplyConfigIfAvailable copies exact movement fields; UnitGridBaker passes raw movement and model-bound values to shared projection. Current Alt04 config/getters plus UnitAttackAuthoringProjection produce the full UnitAttack value slice; the importer compares all captured UnitAttack value fields before allocation and preserves VFX references. Alt04 usesVehicleMotion=false, so footprint and vehicle outputs are model-bounds-independent; nonvehicle vehicle movement constants are original baker outputs.",
    }
    path = output / "unit-movement-source-input.json"
    path.write_text(json.dumps(descriptor, indent=2) + "\n", encoding="utf-8")
    return path, descriptor


def create_current_projection_input(repo, output):
    """Explicit projection-only descriptor: source pins, no capture or authored-value claims."""
    meta = (repo / CONFIG_META).read_text(encoding="utf-8")
    guid_match = re.search(r"(?m)^guid: ([0-9a-f]{32})$", meta)
    need(guid_match is not None, "rifle config meta GUID missing")
    pins = [repo / CONFIG, repo / CONFIG_META, repo / AUTHORING, repo / AUTHORING_VISUALS,
            repo / HELPER, repo / HELPER_META, repo / COMBAT_HEALTH_HELPER, repo / COMBAT_HEALTH_HELPER_META,
            repo / COMBAT_COMPONENTS, repo / ATTACK_HELPER, repo / ATTACK_HELPER_META, repo / CONFIG_MODELS,
            repo / SOURCE_ADAPTER, repo / IMPORTER_PROJECTION, *[repo / path for path in MANIFESTS]]
    descriptor = {
        "schema_version": 1,
        "kind": "StrictCurrentUnitAuthoringProjectionInput",
        "root_key": RIFLE_KEY,
        "config_guid": guid_match.group(1),
        "source_pins": {str(p.relative_to(repo).as_posix()): sha(p) for p in pins},
        "semantics": "Current pinned Alt04 config and original shared movement/combat/health/attack projections only; no retained capture, prefab, entity allocation, VFX references, or readiness claim.",
    }
    path = output / "unit-current-source-projection-input.json"
    path.write_text(json.dumps(descriptor, indent=2) + "\n", encoding="utf-8")
    return path, descriptor


def run(output, source_build_dir, repo, s002_capture=None, s002_receipt=None):
    output = output.resolve()
    repo = repo.resolve(strict=True)
    need((s002_capture is None) == (s002_receipt is None), "S002 historical capture and receipt must be provided together")
    need(output.is_relative_to(repo) and not output.exists(), "fresh output directory within repository required")
    build_dir = source_build_dir.resolve(strict=True)
    build = load(build_dir / "build.json")
    response = build_dir / "compile.rsp"
    exe = build_dir / "SourceSimulation.exe"
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("compile_exit") == 0 and build.get("native_or_player_launched") is False and
         build.get("executable", {}).get("sha256") == sha(exe) and
         build.get("compiler_response", {}).get("sha256") == sha(response), "fresh passed non-Unity source build required")
    pins = build.get("source_and_reference_pins", {})
    compiler = next((Path(p) for p in pins if p.endswith("/Roslyn/bincore/csc.dll") and Path(p).is_file() and sha(p) == pins[p]), None)
    dotnet = shutil.which("dotnet")
    need(compiler is not None and dotnet and str(Path(dotnet).resolve()) in pins,
         "build-pinned Roslyn/dotnet required")
    source_lines = response.read_text(encoding="utf-8").splitlines()
    need(sum(1 for line in source_lines if str(repo / HELPER) in line) == 1 and pins.get(str(repo / HELPER)) == sha(repo / HELPER),
         "fresh source build must compile and pin shared movement helper exactly once")
    need(sum(1 for line in source_lines if str(repo / COMBAT_HEALTH_HELPER) in line) == 1 and
         pins.get(str(repo / COMBAT_HEALTH_HELPER)) == sha(repo / COMBAT_HEALTH_HELPER),
         "fresh S003 source build must compile and pin shared combat/health helper exactly once")
    need(sum(1 for line in source_lines if str(repo / ATTACK_HELPER) in line) == 1 and
         pins.get(str(repo / ATTACK_HELPER)) == sha(repo / ATTACK_HELPER),
         "fresh S003 source build must compile and pin shared UnitAttack value helper exactly once")
    need(sum(1 for line in source_lines if str(repo / IMPORTER) in line) == 1 and pins.get(str(repo / IMPORTER)) == sha(repo / IMPORTER),
         "fresh source build must compile and pin S003 template importer exactly once")
    need(sum(1 for line in source_lines if str(repo / SOURCE_ADAPTER) in line) == 1 and pins.get(str(repo / SOURCE_ADAPTER)) == sha(repo / SOURCE_ADAPTER),
         "fresh source build must compile and pin the strict movement source adapter exactly once")
    need(sum(1 for line in source_lines if str(repo / IMPORTER_PROJECTION) in line) == 1 and
         pins.get(str(repo / IMPORTER_PROJECTION)) == sha(repo / IMPORTER_PROJECTION),
         "fresh source build must compile and pin the shared movement importer projection exactly once")
    for path in (STARTUP, AUTHORITY):
        need(sum(1 for line in source_lines if str(repo / path) in line) == 1 and pins.get(str(repo / path)) == sha(repo / path),
             "fresh S003 source build must compile and pin runtime UnitMove initialization binding exactly once: " + str(path))

    raw_path, strict_path, keys_path, key_receipt_path = [repo / p for p in (RAW, STRICT, KEYS, KEY_RECEIPT)]
    for path, expected in ((raw_path, RAW_SHA), (strict_path, STRICT_SHA), (keys_path, KEYS_SHA), (key_receipt_path, KEY_RECEIPT_SHA)):
        need(sha(path) == expected, "retained strict S003 source evidence changed: " + str(path))
    raw, strict, keys, key_receipt = map(load, (raw_path, strict_path, keys_path, key_receipt_path))
    need(strict.get("kind") == "S003FocusedPrefabCaptureReceipt" and strict.get("validation_status") == "PassedDiagnosticSourceOnly" and
         strict.get("capture_sha256") == RAW_SHA and strict.get("key_descriptor_sha256") == KEYS_SHA and
         strict.get("mission_ready") is False and strict.get("mission_world_created") is False,
         "retained passed S003 historical diagnostic receipt required")
    need(key_receipt.get("status") == "PassedSourceOnly" and keys == raw.get("requested_prefab_keys") and
         raw.get("requested_prefab_keys_sha256") == KEYS_SHA, "retained exact ordered S003 descriptor required")
    need(RIFLE_KEY in keys and RIFLE_KEY in (raw.get("opening_prefabs") or {}), "target rifle root absent from retained S003 baked capture")

    output.mkdir(parents=True)
    source_input_path, descriptor = create_source_input(repo, output, raw)
    current_projection_input_path, current_projection_descriptor = create_current_projection_input(repo, output)
    historical_baker_files = {"UnitGridAuthoring.cs", "UnitGridAuthoring.BakerVisuals.cs"}
    changed_source_pins = {key: {"captured": value, "current": sha(repo / key)}
                           for key, value in raw.get("source_pins", {}).items()
                           if Path(key).name in historical_baker_files and sha(repo / key) != value}

    out_exe = output / "S003RifleMovementProjectionSourceHost.exe"
    rewritten, had_out, had_main = [], False, False
    for line in source_lines:
        value = line.strip().strip('"')
        if value.startswith("-out:"):
            line = '"-out:' + str(out_exe) + '"'; had_out = True
        elif value.startswith("-main:"):
            line = '"-main:S003FocusedTemplateSourceHostControls"'; had_main = True
        rewritten.append(line)
    need(had_out and had_main, "build response lacks expected compiler switches")
    rewritten.append('"' + str(repo / HARNESS) + '"')
    rsp = output / "compile.rsp"
    rsp.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
    compile_result = subprocess.run([dotnet, str(compiler), "@" + str(rsp)], capture_output=True, text=True, timeout=300)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr, encoding="utf-8")
    need(compile_result.returncode == 0 and out_exe.is_file(), "source-host control compilation failed")

    runtime = build.get("runtime_selection", {})
    mono, engine, newtonsoft = Path(runtime.get("mono", "")), Path(runtime.get("engine", "")), Path(runtime.get("newtonsoft", ""))
    need(mono.is_file() and engine.is_dir() and newtonsoft.is_file(), "build-pinned Mono/engine/Newtonsoft refs unavailable")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(build_dir / "placement"), str(engine), str(newtonsoft.parent),
                                        str(repo / "Library/ScriptAssemblies"), str(output)))
    control_receipt = output / "control.json"
    command = [str(mono), str(out_exe), str(raw_path), str(keys_path), str(strict_path),
               str(raw_path.parent), str(control_receipt), str(source_input_path), str(repo), str(current_projection_input_path)]
    s002_capture_record = None
    if s002_capture is not None:
        s002_capture = Path(s002_capture).resolve(strict=True)
        s002_receipt = Path(s002_receipt).resolve(strict=True)
        need(not s002_capture.is_symlink() and not s002_receipt.is_symlink(), "regular S002 historical capture and receipt required")
        s002_capture_doc, s002_receipt_doc = load(s002_capture), load(s002_receipt)
        need(s002_receipt_doc.get("kind") == "S002FocusedPrefabCaptureReceipt" and
             s002_receipt_doc.get("validation_status") == "Passed" and
             s002_receipt_doc.get("capture_sha256") == sha(s002_capture) and
             s002_receipt_doc.get("prefab_count") == 12 and s002_receipt_doc.get("entity_count") == 352 and
             s002_receipt_doc.get("source_pin_count") == len(s002_capture_doc.get("source_pins", {})) and
             s002_receipt_doc.get("mission_ready") is False and
             s002_capture_doc.get("kind") == "AuthoredSkirmishPrefabReferenceData" and
             RIFLE_KEY in s002_capture_doc.get("requested_prefab_keys", []) and
             RIFLE_KEY in (s002_capture_doc.get("opening_prefabs") or {}),
             "Exact diagnostic S002 rifle-root witness and receipt required")
        for rel in (CONFIG, CONFIG_META, PREFAB):
            need(s002_capture_doc["source_pins"].get(str(rel)) == sha(repo / rel),
                 "S002 selected rifle source asset pin differs: " + str(rel))
        command.extend((str(s002_capture), str(s002_receipt)))
        s002_capture_record = {"path": str(s002_capture), "sha256": sha(s002_capture),
                               "receipt_path": str(s002_receipt), "receipt_sha256": sha(s002_receipt),
                               "scope": "Selected Alt04 movement and UnitCombat/UnitHealth field comparisons; full S002 capture source-pin drift is not admitted"}
    control = subprocess.run(command, capture_output=True, text=True, env=env, timeout=180)
    (output / "run.log").write_text(control.stdout + control.stderr, encoding="utf-8")
    need(control.returncode == 0 and control.stdout.splitlines().count(PASS) == 1 and control_receipt.is_file(),
         "source-host movement projection/import control failed")
    result = load(control_receipt)
    projection = result.get("rifle_movement_source_projection", {})
    need(projection.get("captured_baked_movement_slice_match") is True and
         projection.get("captured_baked_combat_health_match") is True and
         projection.get("mismatch_rejected_before_allocation") is True,
         "source host did not bind movement and combat/health projections to captured rifle fields")

    # Capture source pins are retained as historical provenance. Record the known Baker drift instead of
    # silently treating the old baked bytes as a current-source capture.
    report = {
        "schema_version": 1,
        "kind": "S003RifleMovementAuthoringProjectionSourceHostReceipt",
        "status": "PassedHistoricalBakeComparison_CurrentConfigPinned",
        "scope": "One rifle five-component movement slice plus UnitCombat and UnitHealth fields; no other prefab, UnitAttack, presentation fields, transport, mission state, or readiness claim",
        "retained_capture": {"path": str(raw_path), "sha256": RAW_SHA, "strict_receipt": str(strict_path),
                             "strict_receipt_sha256": STRICT_SHA, "historical_baker_pin_drift": changed_source_pins},
        "source_input": {"path": str(source_input_path), "sha256": sha(source_input_path), "values": descriptor["values_bits"],
                         "source_pins": descriptor["source_pins"], "capture_asset_pin_matches": descriptor["captured_source_pins"]},
        "fresh_source_build": {"build_path": str(build_dir / "build.json"), "build_sha256": sha(build_dir / "build.json"),
                               "executable_sha256": sha(exe), "compiler_response_sha256": sha(response),
                         "helper_compiled_once": True, "combat_health_helper_compiled_once": True,
                         "importer_compiled_once": True, "movement_importer_compiled_once": True, "source_adapter_compiled_once": True,
                               "runtime_startup_binding_compiled_once": True, "initialization_schema_compiled_once": True},
        "compiled_host": {"path": str(out_exe), "sha256": sha(out_exe), "compile_response_sha256": sha(rsp),
                          "compile_log_sha256": sha(output / "compile.log"), "run_log_sha256": sha(output / "run.log")},
        "import_result": result,
        "s002_historical_field_witness": s002_capture_record,
        "unity_world_created": False, "mission_manager_created": False, "mission_ready": False,
        "scope_note": "The source host validates pinned current authoring inputs against the retained baked movement, UnitCombat, and UnitHealth fields before allocating entities. Retained capture Baker source is historical and its current pin drift is disclosed; all other missing gameplay fields remain missing.",
    }
    (output / "receipt.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("[S003RifleMovementAuthoringProjectionSourceHost] result=Passed historicalCapture=1 movementSliceMatch=1 combatHealthMatch=1 mismatchRejectedBeforeAllocation=1 unity=0 readiness=0")
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[5])
    parser.add_argument("--source-build-dir", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--s002-capture", type=Path)
    parser.add_argument("--s002-capture-receipt", type=Path)
    args = parser.parse_args()
    try:
        return run(args.output, args.source_build_dir, args.repo, args.s002_capture, args.s002_capture_receipt)
    except (OSError, ValueError, KeyError, TypeError, subprocess.SubprocessError) as error:
        print("[S003RifleMovementAuthoringProjectionSourceHost] result=Failed " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

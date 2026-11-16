#!/usr/bin/env python3
"""Run the targeted Alt04 movement-slice control from a fresh S002 source build."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

import run_unit_move_authoring_source_host as shared

ROOT = Path(__file__).resolve().parents[5]
DEFAULT_OUTPUT = Path("T Logs/SkirmishProduction/s002-rifle-movement-source-host-20261103-T1")
S002_CAPTURE = Path("/private/tmp/s002-scoped-prefab-capture-20261102-r1/prefab-reference-data.json")
S002_RECEIPT = Path("/private/tmp/s002-scoped-prefab-capture-20261102-r1/capture-receipt.json")
CONTROL = Path("Tools/CI/SkirmishSourceSimulation/Tests/S002RifleMovementSourceHostControls.cs")
HELPER = Path("Assets/Game/Scripts/Components/UnitMoveAuthoringProjection.cs")
COMBAT_HEALTH_HELPER = Path("Assets/Game/Scripts/Components/UnitCombatHealthAuthoringProjection.cs")
ATTACK_HELPER = Path("Assets/Game/Scripts/Components/UnitAttackAuthoringProjection.cs")
IMPORTER = Path("Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.cs")
IMPORTER_PROJECTION = Path("Tools/CI/SkirmishSourceSimulation/Adapters/GeneratedReferenceTemplateImporter.UnitMoveProjection.cs")
SOURCE_ADAPTER = Path("Tools/CI/SkirmishSourceSimulation/Adapters/SourceUnitMoveAuthoringSource.cs")
STARTUP = Path("Tools/CI/SkirmishSourceSimulation/Adapters/307_GeneratedS002StartingWorld.lifetime.cs")
AUTHORITY = Path("Tools/CI/SkirmishSourceSimulation/Adapters/513_FreshSourceStorageLifetimeAuthority.cs")
MANIFEST = Path("Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json")
PASS = "[S002RifleMovementSourceHostControls] result=Passed s002=12 s003=19 movementSlice=1 combatHealthSlice=1 unitAttack=1 allValueMismatchesRejected=1 missingRejected=1 cleanup=1 world=0 mission=0 readiness=0"


def need(condition, message):
    if not condition:
        raise ValueError(message)


def run(output, build_dir, repo):
    repo = repo.resolve(strict=True)
    output = (repo / output).resolve()
    build_dir = build_dir.resolve(strict=True)
    need(output.is_relative_to(repo) and not output.exists(), "fresh output directory inside the repository required")
    s002_capture, s002_receipt = repo / S002_CAPTURE, repo / S002_RECEIPT
    s003_capture, s003_receipt = repo / shared.RAW, repo / shared.STRICT
    need(all(path.is_file() and not path.is_symlink() for path in
             (s002_capture, s002_receipt, s003_capture, s003_receipt)), "regular retained S002/S003 witness files required")

    build = shared.load(build_dir / "build.json")
    response, executable = build_dir / "compile.rsp", build_dir / "SourceSimulation.exe"
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("compile_exit") == 0 and build.get("mission") == "S002" and
         build.get("native_or_player_launched") is False and
         build.get("executable", {}).get("sha256") == shared.sha(executable) and
         build.get("compiler_response", {}).get("sha256") == shared.sha(response),
         "fresh passed non-Unity S002 source build required")
    pins = build.get("source_and_reference_pins", {})
    compiler = next((Path(path) for path in pins if path.endswith("/Roslyn/bincore/csc.dll") and
                     Path(path).is_file() and shared.sha(path) == pins[path]), None)
    dotnet = shutil.which("dotnet")
    need(compiler is not None and dotnet and str(Path(dotnet).resolve()) in pins,
         "build-pinned Roslyn and dotnet required")
    source_lines = response.read_text(encoding="utf-8").splitlines()
    required = (HELPER, COMBAT_HEALTH_HELPER, ATTACK_HELPER, IMPORTER, IMPORTER_PROJECTION, SOURCE_ADAPTER, STARTUP, AUTHORITY)
    for path in required:
        absolute = str(repo / path)
        need(sum(1 for line in source_lines if absolute in line) == 1 and pins.get(absolute) == shared.sha(repo / path),
             "passed S002 source build must compile and pin exactly one profile source: " + str(path))
    manifest_abs = str(repo / MANIFEST)
    need(pins.get(manifest_abs) == shared.sha(repo / MANIFEST), "S002 source manifest differs from its passing build pin")

    output.mkdir(parents=True)
    s003 = shared.load(s003_capture)
    source_input, descriptor = shared.create_source_input(repo, output, s003)
    current_projection_input, current_projection_descriptor = shared.create_current_projection_input(repo, output)
    need((shared.load(s002_receipt).get("capture_sha256") == shared.sha(s002_capture) and
          shared.load(s002_receipt).get("mission_ready") is False), "S002 receipt must preserve diagnostic-only status")

    out_exe = output / "S002RifleMovementSourceHost.exe"
    rewritten, had_out, had_main = [], False, False
    for line in source_lines:
        value = line.strip().strip('"')
        if value.startswith("-out:"):
            line = '"-out:' + str(out_exe) + '"'; had_out = True
        elif value.startswith("-main:"):
            line = '"-main:S002RifleMovementSourceHostControls"'; had_main = True
        rewritten.append(line)
    need(had_out and had_main, "S002 compiler response lacks expected output/main switches")
    rewritten.append('"' + str(repo / CONTROL) + '"')
    response_out = output / "compile.rsp"
    response_out.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
    compile_result = subprocess.run([dotnet, str(compiler), "@" + str(response_out)],
                                    capture_output=True, text=True, timeout=300)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr, encoding="utf-8")
    need(compile_result.returncode == 0 and out_exe.is_file(), "S002 profile source-host control compilation failed")

    runtime = build.get("runtime_selection", {})
    mono, engine, newtonsoft = Path(runtime.get("mono", "")), Path(runtime.get("engine", "")), Path(runtime.get("newtonsoft", ""))
    need(mono.is_file() and engine.is_dir() and newtonsoft.is_file(), "build-pinned Mono/engine/Newtonsoft references unavailable")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(build_dir / "placement"), str(engine), str(newtonsoft.parent),
                                        str(repo / "Library/ScriptAssemblies"), str(output)))
    control_path = output / "control.json"
    command = [str(mono), str(out_exe), str(s002_capture), str(s002_receipt), str(s003_capture),
               str(s003_receipt), str(source_input), str(repo), str(output), str(control_path),
               str(current_projection_input)]
    result = subprocess.run(command, capture_output=True, text=True, env=env, timeout=180)
    (output / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
    need(result.returncode == 0 and result.stdout.splitlines().count(PASS) == 1 and control_path.is_file(),
         "S002 profile targeted movement source-host control failed")
    control = shared.load(control_path)

    report = {
        "schema_version": 1,
        "kind": "S002RifleMovementSourceHostReceipt",
        "status": "Passed_TargetedHistoricalMovementCombatHealthAndUnitAttackSlicesOnly",
        "scope": "Alt04 five-component movement slice plus UnitCombat/UnitHealth and all UnitAttack value fields through the S002 profile's compiled importer. Retained S002 and S003 captures remain historical witnesses; VFX references are untouched; no full capture pin audit, admission, or readiness claim.",
        "s002_source_build": {"path": str(build_dir / "build.json"), "sha256": shared.sha(build_dir / "build.json"),
                              "source_manifest_sha256": shared.sha(repo / MANIFEST), "executable_sha256": shared.sha(executable),
                              "compiler_response_sha256": shared.sha(response), "mission": "S002"},
        "source_input": {"path": str(source_input), "sha256": shared.sha(source_input),
                         "current_values_bits": descriptor["values_bits"], "current_movement_values": descriptor["movement_values"],
                         "current_unit_attack_values": "source-backed from pinned Alt04 config/getters and UnitAttackAuthoringProjection",
                         "source_pins": descriptor["source_pins"]},
        "current_source_only_projection_input": {
            "path": str(current_projection_input), "sha256": shared.sha(current_projection_input),
            "kind": current_projection_descriptor["kind"],
            "source_pin_count": len(current_projection_descriptor["source_pins"]),
            "prefab_source_pinned": "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab" in current_projection_descriptor["source_pins"],
            "capture_source_pins_present": "captured_source_pins" in current_projection_descriptor,
            "scope": "Current Alt04 movement/combat/health/attack value projection; no retained capture parameter or prefab read"},
        "current_source_only_projection_controls": control.get("current_source_only_projection"),
        "compiled_source_host": {"path": str(out_exe), "sha256": shared.sha(out_exe),
                                 "compile_response_sha256": shared.sha(response_out),
                                 "compile_log_sha256": shared.sha(output / "compile.log"),
                                 "run_log_sha256": shared.sha(output / "run.log")},
        "control": control,
        "unity_world_created": False, "mission_manager_created": False, "mission_ready": False,
        "full_capture_pin_audit_performed": False,
    }
    (output / "receipt.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("[S002RifleMovementSourceHost] result=Passed profile=S002 historicalWitnesses=2 movementSlice=1 combatHealthSlice=1 unitAttack=1 allValueMismatchesRejected=1 missingRejected=1 cleanup=1 readiness=0")
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--source-build-dir", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()
    try:
        return run(args.output, args.source_build_dir, args.repo)
    except (OSError, ValueError, KeyError, TypeError, subprocess.SubprocessError) as error:
        print("[S002RifleMovementSourceHost] result=Failed " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

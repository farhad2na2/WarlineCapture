#!/usr/bin/env python3
"""Run the disposable S003 source-only semantic request and paid queue fixture."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys

import run_s003_single_rifle_authority_control as authority_control

ROOT = authority_control.ROOT
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/SourceS003RifleSemanticQueueFixture.cs"
AUTHORITY_HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S003SingleRifleAuthorityControl.cs"
RUNNER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/run_source_s003_rifle_semantic_queue_fixture.py"
T6_RECEIPT_SHA = authority_control.T6_RECEIPT_SHA
PROJECTION_HELPER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/unit_move_projection/run_unit_move_authoring_source_host.py"
PASS = re.compile(r"^\[SourceS003RifleSemanticQueueFixture\] result=Passed scope=DisposableS003Move receipt=[1-9][0-9]* materials=80 delivered=1 spawned=1 move=1 path_cells=[1-9][0-9]* movement_ticks=[1-9][0-9]* factory=0 membership=0 admission=0 readiness=0$")
TRUE = ("known_null_camera_and_provider_scope", "missing_transport_gate_rejected_before_payment",
        "missing_fixture_authority_rejected", "mismatched_setup_rejected",
        "receipt_owner_is_current_session", "partial_rifle_template_source_derived",
        "transport_identity_only", "barrack_identity_only")
FALSE = ("source_metadata_complete", "factory_closure", "s003_provider_membership", "world_admission", "mission_readiness")


def sha(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def need(condition, message):
    if not condition:
        raise RuntimeError(message)


def run(output, build_json_path, build_sha256):
    output = Path(output).resolve()
    need(output.is_relative_to(ROOT) and not output.exists(), "Fresh output directory inside repository required")
    output.mkdir(parents=True)
    failure_path = output / "failure-receipt.json"
    stage = "T6 evidence"
    try:
        t6_receipt, t6_diagnostic, t6_files, historical_build = authority_control.verify_t6()
        build_path = authority_control.resolve_repo_file(build_json_path)
        need(build_path != t6_files["source_build"] and len(build_sha256) == 64 and sha(build_path) == build_sha256,
             "Explicit fresh source build must be hash-bound and distinct from T6 historical build")
        build = json.loads(build_path.read_text(encoding="utf-8"))
        need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
             build.get("mission") == "S002" and build.get("compile_exit") == 0,
             "Fresh passed S002 source-host build required")
        pins = authority_control.verify_build_pins(build, ROOT)
        current_authority = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/137_RuntimeBuildingBindings.cs"
        need(pins.get(str(current_authority)) == sha(current_authority),
             "Fresh S002 source build must include the current pinned S003 authority reader")
        response_descriptor = build.get("compiler_response", {})
        response = authority_control.resolve_repo_file(response_descriptor.get("path"))
        need(sha(response) == response_descriptor.get("sha256"),
             "Fresh S002 compiler response differs from its build descriptor")
        executable_descriptor = build.get("executable", {})
        executable = authority_control.resolve_repo_file(executable_descriptor.get("path"))
        need(sha(executable) == executable_descriptor.get("sha256"),
             "Fresh S002 executable differs from its build descriptor")
        csc_candidates = [Path(path) for path in pins if path.endswith("/Roslyn/bincore/csc.dll")]
        dotnet_candidates = [Path(path) for path in pins if Path(path).name == "dotnet"]
        csc = next((path for path in csc_candidates if path.is_file() and sha(path) == pins[str(path)]), None)
        dotnet = shutil_which_dotnet()
        need(csc is not None and dotnet and any(dotnet == path.resolve() for path in dotnet_candidates),
             "Pinned fresh Roslyn/dotnet host required")

        harness_sha = sha(HARNESS)
        authority_harness_sha = sha(AUTHORITY_HARNESS)
        runner_sha = sha(RUNNER)
        projection_helper_sha = sha(PROJECTION_HELPER)
        projection_module_spec = importlib.util.spec_from_file_location("s003_movement_projection_host", PROJECTION_HELPER)
        need(projection_module_spec is not None and projection_module_spec.loader is not None,
             "Pinned current-projection source host helper must load")
        projection_host = importlib.util.module_from_spec(projection_module_spec)
        projection_module_spec.loader.exec_module(projection_host)
        projection_path, projection_descriptor = projection_host.create_current_projection_input(ROOT, output)
        projection_sha = sha(projection_path)
        need(projection_descriptor.get("kind") == "StrictCurrentUnitAuthoringProjectionInput" and
             projection_descriptor.get("root_key") == "Unit_Chr_Soldier_Male_02_Alt_04" and
             projection_descriptor.get("source_pins"),
             "Explicit current Rifle movement projection descriptor required")
        for relative, expected_sha in projection_descriptor["source_pins"].items():
            source_path = (ROOT / relative).resolve(strict=True)
            need(source_path.is_relative_to(ROOT) and sha(source_path) == expected_sha,
                 "Current movement projection source pin mismatch: " + relative)
        control_exe = output / "SourceS003RifleSemanticQueueFixture.exe"
        compile_response = output / "compile.rsp"
        rewritten = []
        main_found = output_found = False
        for line in response.read_text(encoding="utf-8").splitlines():
            value = line.strip()
            unquoted = value[1:-1] if len(value) >= 2 and value[0] == value[-1] == '"' else value
            if unquoted.startswith("-main:"):
                line = '"-main:SourceS003RifleSemanticQueueFixture"'
                main_found = True
            elif unquoted.startswith("-out:"):
                line = '"-out:' + str(control_exe) + '"'
                output_found = True
            rewritten.append(line)
        need(main_found and output_found, "Fresh S002 source response lacks main/output options")
        rewritten.extend(('"' + str(AUTHORITY_HARNESS) + '"', '"' + str(HARNESS) + '"'))
        compile_response.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
        stage = "control compile"
        compiled = subprocess.run([str(dotnet), str(csc), "@" + str(compile_response)],
                                  capture_output=True, text=True, timeout=300)
        (output / "compile.log").write_text(compiled.stdout + compiled.stderr, encoding="utf-8")
        need(compiled.returncode == 0 and control_exe.is_file(), "S003 source semantic queue control compile failed")

        stage = "fixture execution"
        runtime = build["runtime_selection"]
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join((runtime["engine"], str(Path(runtime["newtonsoft"]).parent),
                                             str(ROOT / "Library/ScriptAssemblies"), str(output)))
        result = subprocess.run([runtime["mono"], str(control_exe), str(ROOT), str(output),
                                 str(projection_path), projection_sha],
                                capture_output=True, text=True, env=env, timeout=180)
        (output / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
        passes = [line for line in result.stdout.splitlines() if PASS.fullmatch(line)]
        need(result.returncode == 0 and len(passes) == 1, "S003 semantic/paid queue fixture failed or pass marker missing")
        report_path = output / "control.json"
        report = json.loads(report_path.read_text(encoding="utf-8"))
        need(report.get("kind") == "SourceS003RifleSemanticQueueFixture" and
             report.get("status") == "Passed_SourceOnlyS003MovementFixture", "S003 original movement report identity required")
        for key in TRUE:
            need(report.get(key) is True, "S003 queue fixture failed: " + key)
        for key in FALSE:
            need(report.get(key) is False, "S003 queue fixture scope broadened: " + key)
        need(report.get("setup_catalog_id") == "S003" and report.get("setup_size_id") == "Standard" and
             report.get("setup_seed") == 104732 and report.get("setup_hash") == 2394754492 and
             report.get("quantity") == 4 and report.get("materials_cost") == 80 and
             report.get("materials_debited") == 80 and report.get("credits_debited") == 0 and
             report.get("transport_max_concurrent_resolved_by_queue") == 2 and
             report.get("delivered_members") == 1 and report.get("delivered_member_count") == 1 and
             report.get("pending_count") == 1 and report.get("spawned_cell") is not None and
             report.get("partial_rifle_template_source_derived") is True,
             "Actual setup, payment, receipt, or transport facts differ from source-owned S003 inputs")
        movement = report.get("movement")
        need(isinstance(movement, dict) and movement.get("kind") == "OriginalSourceGroupedMoveAndMovementOwners" and
             movement.get("selected_by_original_presented_group_owner") is True and
             movement.get("move_api") == "SkirmishArmyCommandService.TryMove" and
             movement.get("move_accepted") is True and movement.get("issued_count") == 1 and
             movement.get("movement_owner_ran") is True and movement.get("movement_observed") is True and
             movement.get("order_completed_by_original_cleanup") is True and
             movement.get("stop_reason") == "original_path_and_manual_order_cleanup" and
             movement.get("end_cell") == movement.get("occupancy_derived_goal") and
             movement.get("path_cells") and movement.get("movement_ticks", 0) > 0 and
             movement.get("factory_closure") is False and movement.get("world_admission") is False and
             movement.get("mission_readiness") is False,
             "Original S003 presented-group/move/path/occupancy/grid-movement owners were not exercised")
        report_sha = sha(report_path)

        # After-run pin checks include the entire 3,904-reference fresh build closure,
        # its explicit response/executable, the T6 evidence graph/receipt closure, and harnesses.
        stage = "post-run verification"
        t6_receipt_after, t6_diagnostic_after, _, historical_after = authority_control.verify_t6()
        need(sha(build_path) == build_sha256 and sha(response) == response_descriptor.get("sha256") and
             sha(executable) == executable_descriptor.get("sha256"),
             "Fresh build, response, or executable changed during fixture run")
        authority_control.verify_build_pins(build, ROOT)
        need(sha(HARNESS) == harness_sha and sha(AUTHORITY_HARNESS) == authority_harness_sha and
             sha(RUNNER) == runner_sha and sha(report_path) == report_sha and
             sha(PROJECTION_HELPER) == projection_helper_sha and sha(projection_path) == projection_sha and
             all(sha((ROOT / relative).resolve(strict=True)) == expected_sha
                 for relative, expected_sha in projection_descriptor["source_pins"].items()) and
             t6_receipt_after == t6_receipt and t6_diagnostic_after == t6_diagnostic and
             json.dumps(historical_after, sort_keys=True) == json.dumps(historical_build, sort_keys=True),
             "Fixture harness or T6/fresh-build evidence changed during execution")

        receipt = {
            "kind": "SourceS003RifleSemanticQueueFixtureReceipt",
            "status": report["status"],
            "fresh_s002_source_build": {"path": str(build_path), "sha256": build_sha256,
                                         "compiler_response": response_descriptor, "executable": executable_descriptor},
            "historical_t6_source_build": {"path": str(t6_files["source_build"]),
                                           "sha256": authority_control.T6_FILES["source_build"][1],
                                           "mission": historical_build["mission"],
                                           "compiler_response": historical_build["compiler_response"]},
            "t6_receipt": {"path": str(t6_files["diagnostic"].parent / "receipt.json"), "sha256": T6_RECEIPT_SHA},
            "t6_diagnostic_sha256": authority_control.T6_DIAGNOSTIC_SHA,
            "fixture_source": {"path": str(HARNESS), "sha256": harness_sha},
            "authority_harness_source": {"path": str(AUTHORITY_HARNESS), "sha256": authority_harness_sha},
            "runner_source": {"path": str(RUNNER), "sha256": runner_sha},
            "movement_projection_helper": {"path": str(PROJECTION_HELPER), "sha256": projection_helper_sha},
            "current_movement_projection_input": {"path": str(projection_path), "sha256": projection_sha,
                                                   "descriptor": projection_descriptor},
            "control_executable": {"path": str(control_exe), "sha256": sha(control_exe)},
            "compile_response_sha256": sha(compile_response),
            "compile_log_sha256": sha(output / "compile.log"),
            "run_log_sha256": sha(output / "run.log"),
            "control": report,
            "scope": report["scope"],
        }
        (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
        print(passes[0])
        return 0
    except Exception as error:
        failure_path.write_text(json.dumps({"kind": "SourceS003RifleSemanticQueueFixtureFailure",
                                            "status": "Failed", "stage": stage,
                                            "error": str(error)}, indent=2) + "\n", encoding="utf-8")
        print(str(error), file=sys.stderr)
        return 1


def shutil_which_dotnet():
    import shutil
    command = shutil.which("dotnet")
    return Path(command).resolve() if command else None


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--source-build-json", required=True)
    parser.add_argument("--source-build-sha256", required=True)
    args = parser.parse_args()
    raise SystemExit(run(args.output, args.source_build_json, args.source_build_sha256))

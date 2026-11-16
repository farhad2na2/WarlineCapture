#!/usr/bin/env python3
"""Compile and run the source-only S003 recipe authority controls."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
T6 = ROOT / "T Logs/SkirmishProduction/s003-single-rifle-source-diagnostic-20261103-T6"
T6_RECEIPT_SHA = "8081f14c033f030dd14c0d8a4043c40d48ba568d6cd601e87bb1f93ac4b6ec8f"
T6_DIAGNOSTIC_SHA = "f60bf74aaad6c624d17081d1c2d7b372d3f4b8777e2849ef46b41428b7d48377"
T6_GRAPH_SHA = "34194993cb3430293923140addf3d19d6222e86a675fb82a2fe06b83f84eaf9f"
T17_DESCRIPTOR_SHA = "5e025367f128dde6b862fab29e80ff8acb8e8245a451f97e1e066ef89a42a6af"
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S003SingleRifleAuthorityControl.cs"
RUNNER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/run_s003_single_rifle_authority_control.py"
PASS = "[S003SingleRifleAuthorityControl] result=Passed scope=SourceOnlyOneRifle mutants=10 factory=0 membership=0 admission=0 world=0 readiness=0"
TRUE = ("actual_original_setup_compiled", "identity_only_templates", "exact_one_row_configured_identity_accepted",
        "duplicate_configured_identity_rejected", "wrong_configured_key_rejected", "wrong_seed_rejected",
        "wrong_setup_hash_rejected", "wrong_role_overlay_rejected", "wrong_producer_identity_rejected",
        "wrong_rifle_identity_rejected", "wrong_quantity_rejected", "duplicate_recipe_slot_rejected",
        "full_metadata_shortcut_rejected", "wrong_transport_identity_rejected",
        "non_null_transport_template_rejected")
FALSE = ("source_metadata_complete", "factory_closure", "provider_membership", "runtime_admission",
         "mission_world_created", "mission_readiness")
T6_FILES = {
    "diagnostic": ("T Logs/SkirmishProduction/s003-single-rifle-source-diagnostic-20261103-T6/diagnostic.json", T6_DIAGNOSTIC_SHA),
    "t26_graph": ("T Logs/SkirmishProduction/s003-source-graph-extensions-20261103-T26/grid-surface/source-config-graph-with-grid-surface-nodes.json", T6_GRAPH_SHA),
    "source_descriptor": ("T Logs/SkirmishProduction/barrack-rifle-source-descriptor-20261103-T17/descriptor.json", T17_DESCRIPTOR_SHA),
    "source_build": ("T Logs/SkirmishProduction/s003-t26-qualification-build-20261103-T1/build.json", "f14ab8beb9723412f4a21d406f66dab65a88c5748495c51811aea86c7b7005b1"),
    "original_compiler_receipt": ("T Logs/SkirmishProduction/s003-t26-original-compiler-20261103-T1/receipt.json", "9d23329b7565ed5cb6194f68bbe9257229b80b6f2d29d0f8f4a36d6c701bef06"),
    "source_harness": ("T Logs/SkirmishProduction/s003-single-rifle-source-diagnostic-20261103-T6/S003OriginalCompilerSourceBinding.cs", "79153c493e5620d04127d48c838d9fbfb9bdb72002cff2b19ec9270c8dad4293"),
    "compiler_response": ("T Logs/SkirmishProduction/s003-single-rifle-source-diagnostic-20261103-T6/compile.rsp", "f1bc20d2433ad1894742af1f9b78de1e2d03f135c1650d975c5f6102716f3040"),
    "compile_log": ("T Logs/SkirmishProduction/s003-single-rifle-source-diagnostic-20261103-T6/compile.log", "356e24161d03d8f5a19af06d061e370b9647354261d5a81931f9827818a6fb15"),
    "run_log": ("T Logs/SkirmishProduction/s003-single-rifle-source-diagnostic-20261103-T6/run.log", "a03cac9aa232aaa348021a3868c768c9584028e2ef8f60c41cc34ca8e211017e"),
    "runner": ("Tools/CI/SkirmishSourceSimulation/Recipes/s003_source_role_recipe_diagnostic.py", "10ac744d8247e1d416853d206d0f52bba174a10ad7d9b92c55dcf833077bbff7"),
}


def sha(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def need(condition, message):
    if not condition:
        raise RuntimeError(message)


def resolve_repo_file(raw_path):
    path = Path(raw_path).resolve(strict=True)
    need(path.is_file() and path.is_relative_to(ROOT), "Evidence path must be a regular file inside the repository: " + str(raw_path))
    return path


def verify_t6():
    receipt_path = T6 / "receipt.json"
    need(sha(receipt_path) == T6_RECEIPT_SHA, "Exact frozen T6 receipt required")
    receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
    need(receipt.get("kind") == "S003SingleRifleSourceRoleRecipeDiagnosticReceipt" and
         receipt.get("status") == "PassedSourceOnlyDiagnostic", "T6 source-only diagnostic receipt required")
    files = {}
    for key, (relative, expected) in T6_FILES.items():
        path = resolve_repo_file(ROOT / relative)
        descriptor = receipt.get(key)
        need(isinstance(descriptor, dict) and descriptor.get("path") == str(path) and
             descriptor.get("sha256") == expected and sha(path) == expected,
             "T6 linked evidence changed: " + key)
        files[key] = path
    diagnostic = json.loads(files["diagnostic"].read_text(encoding="utf-8"))
    need(diagnostic.get("kind") == "S003SingleRifleSourceRoleRecipeDiagnostic" and
         diagnostic.get("status") == "PassedSourceOnlyDiagnostic" and
         diagnostic.get("scope", {}).get("factory_closure") is False and
         diagnostic.get("scope", {}).get("unity_factory_invoked") is False and
         diagnostic.get("scope", {}).get("runtime_admission") is False and
         diagnostic.get("scope", {}).get("mission_world_created") is False and
         diagnostic.get("scope", {}).get("mission_readiness") is False,
         "T6 source-only scope changed")
    pins = diagnostic.get("source_pins")
    need(isinstance(pins, dict) and len(pins) == 193, "Exact T6 merged source-pin closure required")
    for raw_path, expected in pins.items():
        path = resolve_repo_file(raw_path)
        need(sha(path) == expected, "T6 current source pin changed: " + raw_path)
    historical_build = json.loads(files["source_build"].read_text(encoding="utf-8"))
    historical_rsp = historical_build.get("compiler_response", {})
    need(historical_build.get("mission") == "S002" and historical_build.get("status") == "Passed" and
         historical_build.get("compile_exit") == 0 and
         historical_rsp.get("path") == str(resolve_repo_file(historical_rsp.get("path"))) and
         sha(historical_rsp["path"]) == historical_rsp.get("sha256"),
         "T6 historical compiler-build link must be retained and correctly identified as S002")
    need(files["source_build"] != resolve_repo_file(historical_rsp["path"]),
         "T6 historical build JSON and compiler response must remain separate artifacts")
    return receipt, diagnostic, files, historical_build


def verify_build_pins(build, repository):
    pins = build.get("source_and_reference_pins")
    need(isinstance(pins, dict) and pins, "Fresh source build full pin closure required")
    runtime_roots = [Path(build["runtime_selection"]["contents"]).resolve(strict=True),
                     Path("/usr/local/share/dotnet").resolve(strict=True)]
    for raw_path, expected in pins.items():
        path = Path(raw_path).resolve(strict=True)
        need(path.is_file() and (path.is_relative_to(repository) or
             any(path.is_relative_to(root) for root in runtime_roots)),
             "Fresh source build pin escaped declared roots: " + raw_path)
        need(len(expected) == 64 and sha(path) == expected,
             "Fresh source build pin mismatch: " + raw_path)
    return pins


def run(output, build_json_path, build_sha256):
    output = Path(output).resolve()
    need(output.is_relative_to(ROOT) and not output.exists(), "Fresh output directory inside repository required")
    output.mkdir(parents=True)
    need(len(build_sha256) == 64, "Explicit fresh source-build SHA256 required")
    receipt_t6, diagnostic, t6_files, historical_build = verify_t6()
    fresh_build_path = resolve_repo_file(build_json_path)
    need(fresh_build_path != t6_files["source_build"] and sha(fresh_build_path) == build_sha256,
         "A separately hashed fresh source-build JSON, distinct from T6 history, is required")
    fresh_build = json.loads(fresh_build_path.read_text(encoding="utf-8"))
    need(fresh_build.get("kind") == "SkirmishSourceSimulationBuild" and
         fresh_build.get("status") == "Passed" and fresh_build.get("mission") == "S002" and
         fresh_build.get("compile_exit") == 0,
         "Fresh passed S002 source-host build required")
    fresh_pins = verify_build_pins(fresh_build, ROOT)
    current_runtime_source = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/137_RuntimeBuildingBindings.cs"
    need(fresh_pins.get(str(current_runtime_source)) == sha(current_runtime_source),
         "Fresh S002 build must compile the current S003 authority reader")
    response_descriptor = fresh_build.get("compiler_response", {})
    response = resolve_repo_file(response_descriptor.get("path"))
    need(sha(response) == response_descriptor.get("sha256"),
         "Fresh S002 compiler response must match its explicit build descriptor")
    response_sha = response_descriptor["sha256"]
    executable_descriptor = fresh_build.get("executable", {})
    executable_path = resolve_repo_file(executable_descriptor.get("path"))
    need(sha(executable_path) == executable_descriptor.get("sha256"),
         "Fresh S002 source executable must match its explicit build descriptor")
    executable_sha = executable_descriptor["sha256"]
    csc_candidates = [Path(path) for path in fresh_pins if path.endswith("/Roslyn/bincore/csc.dll")]
    dotnet_candidates = [Path(path) for path in fresh_pins if Path(path).name == "dotnet"]
    csc = next((path for path in csc_candidates if path.is_file() and sha(path) == fresh_pins[str(path)]), None)
    dotnet_command = shutil.which("dotnet")
    need(csc is not None and dotnet_command and any(Path(dotnet_command).resolve() == p.resolve() for p in dotnet_candidates),
         "Fresh pinned Roslyn and dotnet host required")

    harness_sha = sha(HARNESS)
    runner_sha = sha(RUNNER)
    control_exe = output / "S003SingleRifleAuthorityControl.exe"
    rsp_path = output / "compile.rsp"
    rewritten = []
    changed_main = changed_out = False
    for line in response.read_text(encoding="utf-8").splitlines():
        value = line.strip()
        unquoted = value[1:-1] if len(value) >= 2 and value[0] == value[-1] == '"' else value
        if unquoted.startswith("-main:"):
            line = '"-main:S003SingleRifleAuthorityControl"'
            changed_main = True
        elif unquoted.startswith("-out:"):
            line = '"-out:' + str(control_exe) + '"'
            changed_out = True
        rewritten.append(line)
    need(changed_main and changed_out, "Fresh S002 source response lacks main/output options")
    rewritten.append('"' + str(HARNESS) + '"')
    rsp_path.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
    compiled = subprocess.run([dotnet_command, str(csc), "@" + str(rsp_path)],
                              capture_output=True, text=True, timeout=300)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr, encoding="utf-8")
    need(compiled.returncode == 0 and control_exe.is_file(), "S003 authority-only control compile failed")

    runtime = fresh_build["runtime_selection"]
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((runtime["engine"], str(Path(runtime["newtonsoft"]).parent),
                                         str(ROOT / "Library/ScriptAssemblies"), str(output)))
    result = subprocess.run([runtime["mono"], str(control_exe), str(ROOT), str(output)],
                            capture_output=True, text=True, env=env, timeout=180)
    (output / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
    need(result.returncode == 0 and result.stdout.splitlines().count(PASS) == 1,
         "S003 authority controls failed or exact pass marker missing")
    report_path = output / "control.json"
    report = json.loads(report_path.read_text(encoding="utf-8"))
    report_sha = sha(report_path)
    need(report.get("kind") == "S003SingleRifleAuthorityControl" and
         report.get("status") == "Passed_SourceOnlySingleRifleAuthority", "Authority-control output identity required")
    for key in TRUE:
        need(report.get(key) is True, "S003 authority control failed: " + key)
    for key in FALSE:
        need(report.get(key) is False, "Authority-only scope broadened: " + key)
    need(report.get("setup_catalog_id") == "S003" and report.get("setup_size_id") == "Standard" and
         report.get("setup_seed") == 104732 and report.get("setup_hash") == 2394754492,
         "Actual original S003 setup identity differs from T6 target")

    # Recheck every authority/build input after execution so a mid-run change fails closed.
    receipt_t6_after, diagnostic_after, _, historical_after = verify_t6()
    need(sha(fresh_build_path) == build_sha256 and
         json.dumps(historical_after, sort_keys=True) == json.dumps(historical_build, sort_keys=True),
         "Fresh or historical compiler-build receipt changed during control execution")
    need(sha(response) == response_sha and sha(executable_path) == executable_sha,
         "Fresh S002 compiler response or executable changed during control execution")
    verify_build_pins(fresh_build, ROOT)
    need(sha(HARNESS) == harness_sha and sha(RUNNER) == runner_sha and
         sha(report_path) == report_sha, "Authority control sources or report changed during execution")
    need(receipt_t6_after == receipt_t6 and diagnostic_after == diagnostic,
         "T6 graph/harness/receipt evidence changed during control execution")
    receipt_out = {
        "kind": "S003SingleRifleAuthorityControlReceipt",
        "status": report["status"],
        "fresh_s002_source_build": {"path": str(fresh_build_path), "sha256": build_sha256,
                                     "compiler_response": response_descriptor, "executable": executable_descriptor},
        "historical_t6_build": {"path": str(t6_files["source_build"]), "sha256": T6_FILES["source_build"][1],
                                "mission": historical_build["mission"],
                                "compiler_response": historical_build["compiler_response"]},
        "t6_receipt": {"path": str(T6 / "receipt.json"), "sha256": T6_RECEIPT_SHA},
        "t6_diagnostic_sha256": T6_DIAGNOSTIC_SHA,
        "control_source": {"path": str(HARNESS), "sha256": harness_sha},
        "control_runner": {"path": str(RUNNER), "sha256": runner_sha},
        "control_executable": {"path": str(control_exe), "sha256": sha(control_exe)},
        "compile_response_sha256": sha(rsp_path),
        "compile_log_sha256": sha(output / "compile.log"),
        "run_log_sha256": sha(output / "run.log"),
        "control": report,
        "scope": report["scope"],
    }
    (output / "receipt.json").write_text(json.dumps(receipt_out, indent=2) + "\n", encoding="utf-8")
    print(PASS)
    return 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--source-build-json", required=True)
    parser.add_argument("--source-build-sha256", required=True)
    arguments = parser.parse_args()
    try:
        raise SystemExit(run(arguments.output, arguments.source_build_json, arguments.source_build_sha256))
    except Exception as exception:
        print(str(exception), file=sys.stderr)
        raise SystemExit(1)

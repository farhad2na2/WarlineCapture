#!/usr/bin/env python3
"""Compile and test typed S004 source setup materialization in isolation."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[5]
SETUP = ROOT / "T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/compiled-setup.json"
SETUP_RECEIPT = SETUP.parent / "receipt.json"
HELPER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004CompiledSetupInput.cs"
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S004SessionInitializationOwnerControl.cs"
PROBE = ROOT / "T Logs/SkirmishProduction/s004-session-owner-compile-20261104-T1/receipt.json"
FRAGMENT = PROBE.parent / "S004OriginalOwnerFragments.cs"
MARKER = "[S004SessionInitializationOwnerControl] result=Passed sourceSetup=1 missingSetup=Rejected stock=Passed caps=Passed scheduler=0 mission=0 readiness=0"


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True); parser.add_argument("--output", required=True)
    args = parser.parse_args(); output = Path(args.output).absolute(); output.mkdir(parents=True, exist_ok=False)
    build_path = Path(args.build).resolve(strict=True); build = json.loads(build_path.read_text()); receipt = json.loads(SETUP_RECEIPT.read_text())
    if build.get("status") != "Passed" or build.get("compile_exit") != 0 or receipt.get("status") != "Passed": raise ValueError("Passed source build and compiler export required")
    if receipt.get("compiled_setup", {}).get("sha256") != sha(SETUP): raise ValueError("Compiled setup export changed")
    probe = json.loads(PROBE.read_text())
    if probe.get("status") != "Passed" or probe.get("spawn_owner_excluded") is not True or probe.get("launch_owner_excluded") is not True: raise ValueError("Passing exact session-owner probe required")
    extractor_path = Path(__file__).with_name("probe_s004_spawn_owners_compile.py")
    spec = importlib.util.spec_from_file_location("s004_exact_owner_extractor", extractor_path)
    extractor = importlib.util.module_from_spec(spec); spec.loader.exec_module(extractor)
    session_source = extractor.SESSION.read_text()
    names = ("OnUpdate", "TryInitialize", "ProjectRoles", "Fail")
    methods = [extractor.extract_method(session_source, name) for name in names]
    for name, method in zip(names, methods):
        if hashlib.sha256(method.encode()).hexdigest() != probe["extracted_original_methods"]["session." + name]: raise ValueError("Original session owner changed: " + name)
    expected_fragment = ("using System;\nusing System.Collections.Generic;\nusing Game.Components;\n"
                         "using Game.Configs;\nusing Game.Skirmish.Contracts;\nusing Unity.Entities;\n"
                         "using Unity.Collections;\nusing UnityEngine;\n"
                         "namespace Game.Runtime {\npublic partial struct SkirmishScenarioSpawnSystem {\n\n}\n"
                         "public partial struct SkirmishSessionInitializationSystem {\n" + "\n".join(methods) + "\n}\n}\n")
    if FRAGMENT.read_text() != expected_fragment: raise ValueError("Extracted source owner was modified")
    inputs = (build_path, SETUP, SETUP_RECEIPT, HELPER, HARNESS, PROBE, FRAGMENT); before = {str(path): sha(path) for path in inputs}
    pins = build["source_and_reference_pins"]
    def verify():
        for path, expected in pins.items():
            if sha(path) != expected: raise ValueError("Build pin changed: " + path)
    verify()
    csc = next(Path(path) for path in pins if path.endswith("/Roslyn/bincore/csc.dll")); dotnet = shutil.which("dotnet")
    if not dotnet or not any(Path(path).name == "dotnet" and Path(path).resolve() == Path(dotnet).resolve() for path in pins): raise ValueError("Pinned dotnet required")
    response = Path(build["compiler_response"]["path"])
    if sha(response) != build["compiler_response"]["sha256"]: raise ValueError("Response changed")
    exe = output / "S004SessionInitializationOwnerControl.exe"; lines = []
    for line in response.read_text().splitlines():
        value = line.strip().strip('"')
        if value.startswith("-main:"): line = '"-main:S004SessionInitializationOwnerControl"'
        elif value.startswith("-out:"): line = '"-out:' + str(exe) + '"'
        lines.append(line)
    lines.extend(('"' + str(HELPER) + '"', '"' + str(HARNESS) + '"', '"' + str(FRAGMENT) + '"'))
    rsp = output / "compile.rsp"; rsp.write_text("\n".join(lines) + "\n")
    compiled = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True, text=True, timeout=180)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode != 0: raise RuntimeError("Source control compilation failed")
    runtime = build["runtime_selection"]; env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(Path(build["placement_build"]).parent), runtime["engine"], str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(output)))
    run = subprocess.run([runtime["mono"], str(exe), str(SETUP)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=180)
    (output / "run.log").write_text(run.stdout + run.stderr)
    if run.returncode != 0 or run.stdout.splitlines().count(MARKER) != 1: raise RuntimeError("Source setup control failed")
    verify()
    if {str(path): sha(path) for path in inputs} != before: raise ValueError("Control inputs changed")
    result = {"status": "PassedOriginalSessionOwnerFixture", "input_sha256": before, "build_pins_rechecked": len(pins),
              "original_session_owner_invoked": True, "fixture_session": True, "stock_and_capacity_compared": True, "missing_setup_rejected": True,
              "source_provenance_admission": False, "mission_world_created": False, "runtime_admission": False,
              "source_fit_certified": False, "mission_ready": False,
              "compile_log_sha256": sha(output / "compile.log"), "run_log_sha256": sha(output / "run.log")}
    (output / "receipt.json").write_text(json.dumps(result, indent=2) + "\n"); print(json.dumps(result))


if __name__ == "__main__": main()

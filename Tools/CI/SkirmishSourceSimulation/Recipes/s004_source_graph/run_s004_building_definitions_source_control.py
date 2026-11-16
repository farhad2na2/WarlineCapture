#!/usr/bin/env python3
"""Run original prefab import over the checked four-root S004 capture."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[5]
CAPTURE_DIR = ROOT / "Logs/SkirmishProduction/s004-missing-four-current-capture-20261104-r3"
CAPTURE = CAPTURE_DIR / "data/prefab-reference-data.json"
CHECKED = ROOT / "T Logs/SkirmishProduction/s004-missing-four-current-capture-20261104-T3/receipt.json"
FINALIZER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/finalize_s004_missing_prefab_capture.py"
S3 = ROOT / "Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-r1/data/prefab-reference-data.json"
S3LOG = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/editor.log"
S3RECEIPT = S3LOG.parent / "receipt.json"
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_prefab_capture import qualify
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S004BuildingDefinitionsSourceControl.cs"
HELPER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004CompiledSetupInput.cs"
S004_LOADER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/GeneratedBuildingDefinitions.s004.cs"
SETUP = ROOT / "T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/compiled-setup.json"
SETUP_RECEIPT = SETUP.parent / "receipt.json"
FACTORY = ROOT / "Logs/SkirmishProduction/s004-current-full-factory-runway-20261104-r1/data/full-factory-runway-definitions.json"
PARITY = ROOT / "T Logs/SkirmishProduction/s004-current-full-factory-runway-parity-20261104-T1/receipt.json"
PARITY_CHECK = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/verify_s004_full_factory_runway_parity.py"
MARKER = "[S004BuildingDefinitionsSourceControl] result=Passed buildings=9 slots=27 templates=23 missing=Rejected world=0 readiness=0"


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True); parser.add_argument("--output", required=True)
    args = parser.parse_args()
    output = Path(args.output).absolute(); output.mkdir(parents=True, exist_ok=False)
    build_path = Path(args.build).resolve(strict=True); build = json.loads(build_path.read_text())
    if build.get("kind") != "SkirmishSourceSimulationBuild" or build.get("status") != "Passed" or build.get("compile_exit") != 0:
        raise ValueError("Passed source build required")
    checked = json.loads(CHECKED.read_text())
    if checked.get("status") != "PassedScopedSourceOnlyCapture" or checked.get("wrapper_exit_code") != 0:
        raise ValueError("Checked native scoped capture required")
    setup_receipt = json.loads(SETUP_RECEIPT.read_text()); parity = json.loads(PARITY.read_text())
    if setup_receipt.get("status") != "Passed" or setup_receipt["compiled_setup"]["sha256"] != sha(SETUP) or parity.get("status") != "PassedExactSourceFactoryFieldParity" or parity["input_sha256"][str(FACTORY)] != sha(FACTORY): raise ValueError("Checked S004 setup and full factory parity required")
    pinned = (build_path, CAPTURE, CHECKED, FINALIZER, HARNESS, S3, S3LOG, S3RECEIPT, HELPER, S004_LOADER, SETUP, SETUP_RECEIPT, FACTORY, PARITY, PARITY_CHECK)
    input_sha = {str(path): sha(path) for path in pinned}
    pins = build["source_and_reference_pins"]
    def verify():
        for path, value in pins.items():
            if sha(path) != value: raise ValueError("Source build pin changed: " + path)
    def capture_check(name):
        result = subprocess.run(["python3", str(FINALIZER), "--directory", str(CAPTURE_DIR), "--wrapper-exit", "0", "--output", str(output / name)], cwd=ROOT, text=True, capture_output=True, timeout=180)
        (output / (name + ".log")).write_text(result.stdout + result.stderr)
        if result.returncode != 0 or json.loads((output / name).read_text()) != checked:
            raise ValueError("Capture receipt or current source pins changed")
    def parity_check(name):
        result = subprocess.run(["python3", str(PARITY_CHECK), "--wrapper-exit", "0", "--output", str(output / name)], cwd=ROOT, capture_output=True, text=True, timeout=180)
        (output / (name + ".log")).write_text(result.stdout + result.stderr)
        if result.returncode != 0 or json.loads((output / name).read_text()) != parity: raise ValueError("Full factory parity evidence changed")
    verify(); capture_check("capture-before.json"); parity_check("parity-before.json")
    if qualify(ROOT, S3, S3LOG, 0) != json.loads(S3RECEIPT.read_text()): raise ValueError("Current S003 source capture required")
    csc = next(Path(path) for path in pins if path.endswith("/Roslyn/bincore/csc.dll"))
    dotnet = shutil.which("dotnet")
    if not dotnet or not any(Path(path).name == "dotnet" and Path(path).resolve() == Path(dotnet).resolve() for path in pins): raise ValueError("Pinned dotnet required")
    response = Path(build["compiler_response"]["path"])
    if sha(response) != build["compiler_response"]["sha256"]: raise ValueError("Compiler response changed")
    executable = output / "S004BuildingDefinitionsSourceControl.exe"; lines = []
    for line in response.read_text().splitlines():
        value = line.strip().strip('"')
        if value.startswith("-main:"): line = '"-main:S004BuildingDefinitionsSourceControl"'
        elif value.startswith("-out:"): line = '"-out:' + str(executable) + '"'
        lines.append(line)
    lines.extend('"' + str(path) + '"' for path in (HARNESS, HELPER, S004_LOADER)); rsp = output / "compile.rsp"; rsp.write_text("\n".join(lines) + "\n")
    compiled = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True, text=True, timeout=180)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode != 0: raise RuntimeError("Control compile failed")
    runtime = build["runtime_selection"]; env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(Path(build["placement_build"]).parent), runtime["engine"], str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(output)))
    run = subprocess.run([runtime["mono"], str(executable), str(SETUP), sha(SETUP), str(S3), str(CAPTURE), str(FACTORY), sha(FACTORY)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=180)
    (output / "run.log").write_text(run.stdout + run.stderr)
    if run.returncode != 0 or run.stdout.splitlines().count(MARKER) != 1: raise RuntimeError("Import control failed")
    capture_check("capture-after.json"); parity_check("parity-after.json"); verify()
    if qualify(ROOT, S3, S3LOG, 0) != json.loads(S3RECEIPT.read_text()): raise ValueError("S003 capture drift")
    if {str(path): sha(path) for path in pinned} != input_sha: raise ValueError("Control inputs changed")
    receipt = {"status": "PassedS004SourceDefinitionsControl", "input_sha256": input_sha, "build_pins_rechecked": len(pins),
               "source_definitions": 9, "source_production_slots": 27, "template_roots": 23, "missing_template_rejected": True,
               "campaign_or_mission_world_created": False, "runtime_admission": False, "native_visual_readiness": False, "mission_ready": False,
               "compile_log_sha256": sha(output / "compile.log"), "run_log_sha256": sha(output / "run.log")}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n"); print(json.dumps(receipt))


if __name__ == "__main__": main()

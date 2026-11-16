#!/usr/bin/env python3
"""Run original prefab import over the checked four-root S004 capture."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[5]
CAPTURE_DIR = ROOT / "Logs/SkirmishProduction/s004-missing-four-current-capture-20261104-r3"
CAPTURE = CAPTURE_DIR / "data/prefab-reference-data.json"
CHECKED = ROOT / "T Logs/SkirmishProduction/s004-missing-four-current-capture-20261104-T3/receipt.json"
FINALIZER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/finalize_s004_missing_prefab_capture.py"
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S004FourPrefabImportControl.cs"
MARKER = "[S004FourPrefabImportControl] result=Passed roots=4 entities=214 groundMissile=Passed transport=Passed negative=Passed disposable=1 readiness=0"


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
    pinned = (build_path, CAPTURE, CHECKED, FINALIZER, HARNESS)
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
    verify(); capture_check("capture-before.json")
    csc = next(Path(path) for path in pins if path.endswith("/Roslyn/bincore/csc.dll"))
    dotnet = shutil.which("dotnet")
    if not dotnet or not any(Path(path).name == "dotnet" and Path(path).resolve() == Path(dotnet).resolve() for path in pins): raise ValueError("Pinned dotnet required")
    response = Path(build["compiler_response"]["path"])
    if sha(response) != build["compiler_response"]["sha256"]: raise ValueError("Compiler response changed")
    executable = output / "S004FourPrefabImportControl.exe"; lines = []
    for line in response.read_text().splitlines():
        value = line.strip().strip('"')
        if value.startswith("-main:"): line = '"-main:S004FourPrefabImportControl"'
        elif value.startswith("-out:"): line = '"-out:' + str(executable) + '"'
        lines.append(line)
    lines.append('"' + str(HARNESS) + '"'); rsp = output / "compile.rsp"; rsp.write_text("\n".join(lines) + "\n")
    compiled = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True, text=True, timeout=180)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode != 0: raise RuntimeError("Control compile failed")
    runtime = build["runtime_selection"]; env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(Path(build["placement_build"]).parent), runtime["engine"], str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(output)))
    run = subprocess.run([runtime["mono"], str(executable), str(CAPTURE)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=180)
    (output / "run.log").write_text(run.stdout + run.stderr)
    if run.returncode != 0 or run.stdout.splitlines().count(MARKER) != 1: raise RuntimeError("Import control failed")
    capture_check("capture-after.json"); verify()
    if {str(path): sha(path) for path in pinned} != input_sha: raise ValueError("Control inputs changed")
    receipt = {"status": "PassedDisposableSourceImportControl", "input_sha256": input_sha, "build_pins_rechecked": len(pins),
               "roots": 4, "entities": 214, "ground_missile_fields_compared": True, "transport_capacity_compared": True,
               "campaign_or_mission_world_created": False, "runtime_admission": False, "native_visual_readiness": False, "mission_ready": False,
               "compile_log_sha256": sha(output / "compile.log"), "run_log_sha256": sha(output / "run.log")}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n"); print(json.dumps(receipt))


if __name__ == "__main__": main()

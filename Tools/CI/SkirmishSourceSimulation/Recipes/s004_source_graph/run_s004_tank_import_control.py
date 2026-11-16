#!/usr/bin/env python3
"""Import the checked S002 template capture in an isolated source manager."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[5]
CAPTURE = ROOT / "T Logs/SkirmishProduction/s002-current-baked-templates-20261104-T1/prefab-reference-data.json"
AUDIT = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/audit_s004_s002_tank_capture.py"
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S004TankCaptureImportControl.cs"
MARKER = "[S004TankCaptureImportControl] result=Passed roots=12 entities=352 tank=1 gameplay=Passed negative=Passed disposable=1 readiness=0"


def digest(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    out = Path(args.output).absolute()
    out.mkdir(parents=True, exist_ok=False)
    build_path = Path(args.build).resolve(strict=True)
    build = json.loads(build_path.read_text())
    if build.get("kind") != "SkirmishSourceSimulationBuild" or build.get("status") != "Passed" or build.get("compile_exit") != 0:
        raise ValueError("Passing checked source build required")
    pins = build["source_and_reference_pins"]
    def verify():
        for path, expected in pins.items():
            if digest(path) != expected: raise ValueError("Source build pin changed: " + path)
    verify()
    inputs = {str(p): digest(p) for p in (build_path, HARNESS, AUDIT, CAPTURE)}
    def audit(name):
        completed = subprocess.run(["python3", str(AUDIT), "--output", str(out / name)], cwd=ROOT, capture_output=True, text=True, timeout=180)
        (out / (name + ".log")).write_text(completed.stdout + completed.stderr)
        if completed.returncode != 0: raise RuntimeError("Source capture audit failed; see " + name + ".log")
    audit("before.json")
    csc = next(Path(name) for name in pins if name.endswith("/Roslyn/bincore/csc.dll"))
    dotnet = shutil.which("dotnet")
    if not dotnet or not any(Path(name).name == "dotnet" and Path(name).resolve() == Path(dotnet).resolve() for name in pins):
        raise ValueError("Pinned Roslyn host required")
    response = Path(build["compiler_response"]["path"])
    if digest(response) != build["compiler_response"]["sha256"]: raise ValueError("Compiler response changed")
    executable = out / "S004TankCaptureImportControl.exe"
    lines = []
    for line in response.read_text().splitlines():
        value = line.strip().strip('"')
        if value.startswith("-main:"): line = '"-main:S004TankCaptureImportControl"'
        elif value.startswith("-out:"): line = '"-out:' + str(executable) + '"'
        lines.append(line)
    lines.append('"' + str(HARNESS) + '"')
    rsp = out / "compile.rsp"
    rsp.write_text("\n".join(lines) + "\n")
    compiled = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True, text=True, timeout=180)
    (out / "compile.log").write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode != 0: raise RuntimeError("Control compile failed")
    runtime = build["runtime_selection"]
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(Path(build["placement_build"]).parent), runtime["engine"], str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(out)))
    run = subprocess.run([runtime["mono"], str(executable), str(CAPTURE)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=180)
    (out / "run.log").write_text(run.stdout + run.stderr)
    if run.returncode != 0 or run.stdout.splitlines().count(MARKER) != 1: raise RuntimeError("Import control failed")
    audit("after.json")
    verify()
    if {str(p): digest(p) for p in (build_path, HARNESS, AUDIT, CAPTURE)} != inputs: raise ValueError("Control input changed")
    result = {"status": "PassedDisposableSourceImportControl", "input_sha256": inputs, "build_pins_rechecked": len(pins),
              "capture_roots": 12, "capture_entities": 352, "tank_gameplay_fields_compared": True,
              "other_missing_s004_keys_resolved": False, "native_entity_identity_join_proven": False,
              "mission_manager_created": False, "runtime_admission": False, "mission_ready": False,
              "compile_log_sha256": digest(out / "compile.log"), "run_log_sha256": digest(out / "run.log")}
    (out / "receipt.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result))


if __name__ == "__main__": main()

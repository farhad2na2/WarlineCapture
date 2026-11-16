#!/usr/bin/env python3
"""Run the current importer on a checked S003 capture in a disposable manager."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_prefab_capture import qualify

HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S003RefreshedPrefabImportControl.cs"
CAPTURE = ROOT / "Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-r1/data/prefab-reference-data.json"
LOG = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/editor.log"
RECEIPT = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/receipt.json"
KEYS = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-keys-20261104-T1/prefab-keys.json"
MOVEMENT = ROOT / "T Logs/SkirmishProduction/s003-refreshed-rifle-input-20261104-T1/unit-movement-source-input.json"
MARKER = "[S003RefreshedPrefabImportControl] result=Passed roots=19 entities=647 unsupported=32 omitted=26 negatives=3 sourceProjection=Passed world=0 readiness=0"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    out = Path(args.output).absolute(); out.mkdir(parents=True, exist_ok=False)
    build_path = Path(args.build).resolve(strict=True); build = json.loads(build_path.read_bytes())
    if build.get("status") != "Passed" or build.get("mission") != "S003" or build.get("compile_exit") != 0:
        raise ValueError("Current passed S003 build required")
    if qualify(ROOT, CAPTURE, LOG, 0) != json.loads(RECEIPT.read_bytes()):
        raise ValueError("Fresh checked capture receipt required")
    movement_sha, harness_sha = digest(MOVEMENT), digest(HARNESS)
    pins = build["source_and_reference_pins"]
    for name, expected in pins.items():
        if digest(Path(name)) != expected: raise ValueError("Build pin changed: " + name)
    csc = next(Path(name) for name in pins if name.endswith("/Roslyn/bincore/csc.dll"))
    dotnet = shutil.which("dotnet")
    if not dotnet or not any(Path(name).name == "dotnet" and Path(name).resolve() == Path(dotnet).resolve() for name in pins):
        raise ValueError("Pinned Roslyn host required")
    response = Path(build["compiler_response"]["path"])
    if digest(response) != build["compiler_response"]["sha256"]: raise ValueError("Response changed")
    executable = out / "S003RefreshedPrefabImportControl.exe"; lines = []
    for line in response.read_text().splitlines():
        value = line.strip().strip('"')
        if value.startswith("-main:"): line = '"-main:S003RefreshedPrefabImportControl"'
        elif value.startswith("-out:"): line = '"-out:' + str(executable) + '"'
        lines.append(line)
    lines.append('"' + str(HARNESS) + '"'); rsp = out / "compile.rsp"; rsp.write_text("\n".join(lines) + "\n")
    compiled = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True, text=True, timeout=300)
    (out / "compile.log").write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode != 0: raise RuntimeError("Control compile failed")
    runtime = build["runtime_selection"]; env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(build_path.parent / "placement"), runtime["engine"], str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(out)))
    run = subprocess.run([runtime["mono"], str(executable), str(CAPTURE), str(KEYS), str(MOVEMENT)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (out / "run.log").write_text(run.stdout + run.stderr)
    if run.returncode != 0 or run.stdout.splitlines().count(MARKER) != 1: raise RuntimeError("Importer control failed")
    if qualify(ROOT, CAPTURE, LOG, 0) != json.loads(RECEIPT.read_bytes()): raise ValueError("Capture drift after control")
    if digest(MOVEMENT) != movement_sha or digest(HARNESS) != harness_sha: raise ValueError("Control inputs changed during run")
    for name, expected in pins.items():
        if digest(Path(name)) != expected: raise ValueError("Build pin changed during control: " + name)
    result = {"status": "PassedDisposableImporterControl", "build_sha256": digest(build_path),
              "capture_sha256": digest(CAPTURE), "capture_receipt_sha256": digest(RECEIPT),
              "harness_sha256": harness_sha, "roots": 19, "entities": 647,
              "unsupported_values": 32, "omitted_presentation_components": 26,
              "source_pins_rechecked": len(pins), "movement_input_sha256": movement_sha, "movement_projection_compared": True,
              "world_allocated": False, "runtime_admission": False, "mission_ready": False}
    (out / "receipt.json").write_text(json.dumps(result, indent=2) + "\n"); print(json.dumps(result))


if __name__ == "__main__": main()

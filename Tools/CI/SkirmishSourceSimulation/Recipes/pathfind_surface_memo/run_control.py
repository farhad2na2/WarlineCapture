#!/usr/bin/env python3
"""Compile and run a standalone control against the existing S026 source closure."""
import argparse
import hashlib
import json
import os
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
DEFAULT_BUILD = ROOT / "T Logs/SkirmishProduction/s026-canplace-memo-build-20261113-r1"


def run(build: Path, output: Path) -> dict:
    build = build.resolve(strict=True)
    response_in = build / "compile.rsp"
    build_receipt = json.loads((build / "build.json").read_text())
    runtime = build_receipt["runtime_selection"]
    source = Path(__file__).with_name("SurfaceMemoControl.cs").resolve(strict=True)
    args = [json.loads(row) for row in response_in.read_text().splitlines() if row.strip()]
    mains = [i for i, value in enumerate(args) if value.startswith("-main:")]
    outs = [i for i, value in enumerate(args) if value.startswith("-out:")]
    if len(mains) != 1 or len(outs) != 1:
        raise ValueError("Expected one main and output compiler argument")
    args[mains[0]] = "-main:SurfaceMemoControlProgram"
    output.mkdir(parents=True, exist_ok=False)
    exe = output / "SurfaceMemoControl.exe"
    args[outs[0]] = "-out:" + str(exe)
    if str(source) in args:
        raise ValueError("Control source already present in compile closure")
    args.append(str(source))
    response = output / "compile.rsp"
    response.write_text("\n".join(json.dumps(value) for value in args) + "\n")
    dotnet = Path(shutil.which("dotnet")).resolve(strict=True)
    sdk_dir = dotnet.parent / "sdk"
    sdk_dirs = sorted((p for p in sdk_dir.iterdir() if (p / "Roslyn/bincore/csc.dll").is_file()), key=lambda p: tuple(int(x) for x in p.name.split(".")))
    if not sdk_dirs:
        raise FileNotFoundError("Installed Roslyn csc.dll required")
    csc = sdk_dirs[-1] / "Roslyn/bincore/csc.dll"
    compile_run = subprocess.run([str(dotnet), str(csc), "@" + str(response)], capture_output=True, text=True, timeout=120)
    compile_log = output / "compile.log"
    compile_log.write_text(compile_run.stdout + compile_run.stderr)
    if compile_run.returncode:
        raise RuntimeError("Standalone source control compile failed; see " + str(compile_log))

    mono = Path(runtime["mono"]).resolve(strict=True)
    mono_path = os.pathsep.join([str(ROOT / "Library/ScriptAssemblies"), runtime["engine"],
        str(Path(runtime["newtonsoft"]).parent), str(build / "placement")])
    env = os.environ.copy()
    env["MONO_PATH"] = mono_path
    control_run = subprocess.run([str(mono), "--optimize=-float32", str(exe)], capture_output=True, text=True,
        timeout=120, env=env)
    control_log = output / "control.log"
    control_log.write_text(control_run.stdout + control_run.stderr)
    if control_run.returncode:
        raise RuntimeError("Standalone surface memo control failed; see " + str(control_log))
    report = json.loads(control_run.stdout.strip().splitlines()[-1])
    result = {"kind": "S026SurfaceMemoStandaloneCompileRun", "status": report["status"],
        "source_closure_build": str(build), "source_closure_compile_response_sha256": hashlib.sha256(response_in.read_bytes()).hexdigest(),
        "control_source": str(source), "control_source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
        "compiler_response": str(response), "compiler_response_sha256": hashlib.sha256(response.read_bytes()).hexdigest(),
        "compile_log": str(compile_log), "compile_exit": compile_run.returncode,
        "control_log": str(control_log), "control_exit": control_run.returncode, "report": report,
        "scope": "Standalone original-validation control; no resolver build, PathfindBatchJob execution, gameplay, placement, or mission run"}
    (output / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--build", type=Path, default=DEFAULT_BUILD)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(run(args.build, args.output), indent=2))

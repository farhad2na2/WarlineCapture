#!/usr/bin/env python3
"""Run source-owned camera focus controls against a pinned Roslyn build."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import time


def sha(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--build", required=True)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    started = time.monotonic()
    repo = Path(args.repo).resolve(strict=True)
    build_path = Path(args.build).resolve(strict=True)
    build = json.loads(build_path.read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
            build.get("compile_exit") == 0, "Passing source build required")
    pins = build["source_and_reference_pins"]
    for path, expected in pins.items():
        require(sha(path) == expected, "Source build input changed: " + path)
    response = build["compiler_response"]
    rsp_path = Path(response["path"]).resolve(strict=True)
    require(sha(rsp_path) == response["sha256"], "Pinned compiler response changed")
    source = repo / "Tools/CI/SkirmishSourceSimulation/Tests/CameraFocusSequenceControls.cs"
    source_hash = sha(source)
    output = Path(args.output).absolute()
    require(not output.exists(), "Fresh control output required")
    output.mkdir(parents=True)
    compiler_args = [json.loads(line) for line in rsp_path.read_text().splitlines() if line.strip()]
    entries = [i for i, value in enumerate(compiler_args) if value == "-main:SourceSimulationProgram"]
    outputs = [i for i, value in enumerate(compiler_args) if value.startswith("-out:")]
    require(len(entries) == 1 and len(outputs) == 1, "One source entry and output required")
    executable = output / "CameraFocusSequenceControls.exe"
    compiler_args[entries[0]] = "-main:CameraFocusSequenceControls"
    compiler_args[outputs[0]] = "-out:" + str(executable)
    compiler_args.append(str(source))
    response_path = output / "controls.rsp"
    response_path.write_text("".join(json.dumps(value) + "\n" for value in compiler_args))
    tools = {Path(path).name: Path(path).resolve(strict=True) for path in pins}
    dotnet, csc = tools.get("dotnet"), tools.get("csc.dll")
    require(dotnet and csc, "Pinned Roslyn tools required")
    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    compile_started = time.monotonic()
    compiled = subprocess.run([str(dotnet), str(csc), "@" + str(response_path)], capture_output=True, text=True, timeout=120)
    compile_seconds = time.monotonic() - compile_started
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    run = None
    run_seconds = None
    if compiled.returncode == 0:
        env = dict(os.environ)
        env["MONO_PATH"] = os.pathsep.join([str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
            str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)])
        installed_config = runtime["script"] / "MonoBleedingEdge/etc/mono/config"
        mono_helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
        require(installed_config.is_file() and mono_helper.is_file(), "Installed Mono config and helper required")
        config = installed_config.read_text().replace('target="libMonoPosixHelper.dylib"', 'target="' + str(mono_helper) + '"')
        config = config.replace('target="MonoPosixHelper"', 'target="' + str(mono_helper) + '"')
        mono_config = output / "mono-config.xml"
        mono_config.write_text(config)
        run_started = time.monotonic()
        run = subprocess.run([str(runtime["mono"]), "--config", str(mono_config), "--optimize=-float32", str(executable)],
            env=env, capture_output=True, text=True, timeout=60)
        run_seconds = time.monotonic() - run_started
        (output / "run.log").write_text(run.stdout + run.stderr)
    require(sha(source) == source_hash, "Control source changed during run")
    for path, expected in pins.items():
        require(sha(path) == expected, "Source build input changed during run: " + path)
    marker = "[CameraFocusSequenceControls] result=Passed checks=12"
    passed = compiled.returncode == 0 and run is not None and run.returncode == 0 and \
        run.stdout.splitlines().count(marker) == 1
    result = {"kind": "CameraFocusSequenceControlsRun", "status": "Passed" if passed else "Failed",
        "build": str(build_path), "build_sha256": sha(build_path), "test_source_sha256": source_hash,
        "compile_exit": compiled.returncode, "compile_wall_seconds": compile_seconds,
        "run_exit": None if run is None else run.returncode, "run_wall_seconds": run_seconds,
        "wall_seconds": time.monotonic() - started, "unity_launched": False}
    (output / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, separators=(",", ":")))
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env python3
"""Run the focused S073 template-import regression using a caller-pinned source capture."""
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


def require(condition, reason):
    if not condition:
        raise RuntimeError(reason)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--build", required=True)
    parser.add_argument("--capture", required=True)
    parser.add_argument("--capture-sha256", required=True)
    parser.add_argument("--package-root", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    started = time.monotonic()
    build_path = Path(args.build).resolve(strict=True)
    capture = Path(args.capture).resolve(strict=True)
    package = Path(args.package_root).resolve(strict=True)
    output = Path(args.output).absolute()
    harness = package / "Tools/CI/SkirmishSourceSimulation/Tests/S073FocusedDisabledTemplateImporterHarness.cs"
    build = json.loads(build_path.read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and
            build.get("status") == "Passed" and build.get("compile_exit") == 0,
            "Passing current source build required")
    pins = build["source_and_reference_pins"]
    for path, expected in pins.items():
        require(sha(path) == expected, "Build input changed: " + path)
    capture_sha = args.capture_sha256
    require(sha(capture) == capture_sha, "Exact immutable focused capture required")
    harness_sha = sha(harness)
    response = build["compiler_response"]
    response_path = Path(response["path"]).resolve(strict=True)
    require(sha(response_path) == response["sha256"], "Pinned Roslyn response file changed")
    require(not output.exists(), "Fresh test output directory required")
    output.mkdir(parents=True)

    compiler_args = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    entry = [i for i, value in enumerate(compiler_args) if value == "-main:SourceSimulationProgram"]
    output_arg = [i for i, value in enumerate(compiler_args) if value.startswith("-out:")]
    require(len(entry) == 1 and len(output_arg) == 1, "Exact checked CLI host entry required")
    executable = output / "S073FocusedDisabledTemplateImporterHarness.exe"
    compiler_args[entry[0]] = "-main:S073FocusedDisabledTemplateImporterHarness"
    compiler_args[output_arg[0]] = "-out:" + str(executable)
    compiler_args.append(str(harness))
    compiler_response = output / "harness.rsp"
    compiler_response.write_text("".join(json.dumps(value) + "\n" for value in compiler_args))

    tools = {Path(path).name: Path(path).resolve(strict=True) for path in pins}
    dotnet, csc = tools.get("dotnet"), tools.get("csc.dll")
    require(dotnet is not None and csc is not None, "Pinned .NET/Roslyn tools required")
    compile_result = subprocess.run([str(dotnet), str(csc), "@" + str(compiler_response)],
                                    capture_output=True, text=True, timeout=120)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    run_result = None
    if compile_result.returncode == 0:
        runtime = {key: Path(value).resolve(strict=True)
                   for key, value in build["runtime_selection"].items()}
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join([
            str(package / "Library/ScriptAssemblies"), str(runtime["engine"]),
            str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)])
        run_result = subprocess.run(
            [str(runtime["mono"]), str(executable), str(capture), capture_sha],
            env=env, capture_output=True, text=True, timeout=60)
        (output / "run.log").write_text(run_result.stdout + run_result.stderr)

    for path, expected in pins.items():
        require(sha(path) == expected, "Build input changed during harness run: " + path)
    require(sha(capture) == capture_sha and sha(harness) == harness_sha,
            "Harness source or captured input changed during test")
    marker = "[S073FocusedDisabledTemplateImporterHarness] result=Passed"
    passed = compile_result.returncode == 0 and run_result is not None and \
        run_result.returncode == 0 and marker in run_result.stdout
    result = {
        "schema_version": 1,
        "kind": "S073FocusedDisabledTemplateImporterHarness",
        "status": "Passed" if passed else "Failed",
        "wall_seconds": time.monotonic() - started,
        "build_receipt": str(build_path),
        "build_receipt_sha256": sha(build_path),
        "capture": {"path": str(capture), "sha256": capture_sha},
        "harness_source": str(harness),
        "harness_source_sha256": harness_sha,
        "compile_exit": compile_result.returncode,
        "run_exit": None if run_result is None else run_result.returncode,
        "stdout": "" if run_result is None else run_result.stdout,
        "stderr": "" if run_result is None else run_result.stderr,
        "unity_launched": False,
        "mission_world_allocated": False,
        "mission_outcome_observed": False,
        "mission_ready": False
    }
    (output / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, separators=(",", ":")))
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())

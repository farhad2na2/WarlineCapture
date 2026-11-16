#!/usr/bin/env python3
"""Compile and run focused writable/read-only ECS chunk-array controls."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def run(build_path, output_path):
    repo = Path(__file__).resolve().parents[4]
    build_path = Path(build_path).resolve(strict=True)
    output = Path(output_path).absolute()
    require(not output.exists(), "Fresh isolated output directory required")
    output.mkdir(parents=True)
    build_raw = build_path.read_bytes()
    build = json.loads(build_raw)
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
            build.get("mission") == "S073" and build.get("compile_exit") == 0,
            "Passing checked S073 source build required")
    control = repo / "Tools/CI/SkirmishSourceSimulation/Tests/S073ChunkArrayWriteThroughControl.cs"
    control_sha = sha(control)
    pins = build.get("source_and_reference_pins", {})
    response_path = Path(build["compiler_response"]["path"]).resolve(strict=True)
    require(sha(response_path) == build["compiler_response"]["sha256"], "Compiler response changed")
    arguments = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    mains = [i for i, value in enumerate(arguments) if value == "-main:SourceSimulationProgram"]
    outputs = [i for i, value in enumerate(arguments) if value.startswith("-out:")]
    require(len(mains) == 1 and len(outputs) == 1, "Expected one checked source-host compiler entry")
    arguments[mains[0]] = "-main:S073ChunkArrayWriteThroughControl"
    executable = output / "S073ChunkArrayWriteThroughControl.exe"
    arguments[outputs[0]] = "-out:" + str(executable)
    arguments.append(str(control))
    rsp = output / "control.rsp"
    rsp.write_text("\n".join(json.dumps(value) for value in arguments) + "\n")
    dotnet = next((path for path in pins if Path(path).name == "dotnet"), None)
    compiler = next((path for path in pins if Path(path).name == "csc.dll"), None)
    require(dotnet and compiler, "Pinned Roslyn toolchain required")
    compiled = subprocess.run([dotnet, compiler, "@" + str(rsp)], capture_output=True, text=True, timeout=120)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    require(compiled.returncode == 0, "Control compilation failed; see compile.log")
    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    config = output / "mono-config.xml"
    helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    dllmaps = ''.join(f'<dllmap dll="{name}" target="{helper}" os="!windows"/>' for name in ("MonoPosixHelper",))
    dllmaps += ''.join(f'<dllmap dll="{name}" target="{native}" os="!windows"/>' for name in
                       ("System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"))
    config.write_text("<configuration>" + dllmaps + "</configuration>\n")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
                                        str(runtime["newtonsoft"].parent)])
    result = subprocess.run([str(runtime["mono"]), "--config", str(config), str(executable)], env=env,
                            capture_output=True, text=True, timeout=60)
    (output / "run.log").write_text(result.stdout + result.stderr)
    marker = "[S073ChunkArrayWriteThroughControl] result=Passed"
    require(result.returncode == 0 and marker in result.stdout, "Control failed; see run.log")
    require(sha(control) == control_sha, "Control changed during run")
    print(json.dumps({"status": "Passed", "kind": "S073ChunkArrayWriteThroughControl",
                      "control_sha256": control_sha, "control_stdout": result.stdout.strip(),
                      "output": str(output), "mission_ready": False}, indent=2))


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: run_s073_chunk_array_write_through_control.py <passing-S073-build.json> <fresh-output-dir>")
    run(*sys.argv[1:])

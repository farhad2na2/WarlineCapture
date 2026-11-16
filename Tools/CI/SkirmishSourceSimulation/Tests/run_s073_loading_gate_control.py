#!/usr/bin/env python3
"""Compile and run the source/native shared loading-gate truth-table control."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(condition, message):
    if not condition:
        raise RuntimeError(message)


def run(build_path, output_path):
    repo = Path(__file__).resolve().parents[4]
    build_path = Path(build_path).resolve(strict=True)
    output = Path(output_path).absolute()
    need(not output.exists(), "Fresh isolated output directory required")
    output.mkdir(parents=True)
    build = json.loads(build_path.read_text())
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and
         build.get("status") == "Passed" and build.get("mission") == "S073",
         "Passing current S073 source build required")
    harness = repo / "Tools/CI/SkirmishSourceSimulation/Tests/S073LoadingGateControl.cs"
    harness_sha = sha(harness)

    response = build["compiler_response"]
    response_path = Path(response["path"]).resolve(strict=True)
    need(sha(response_path) == response["sha256"], "S073 compile closure response changed")
    arguments = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    mains = [i for i, value in enumerate(arguments) if value == "-main:SourceSimulationProgram"]
    outputs = [i for i, value in enumerate(arguments) if value.startswith("-out:")]
    need(len(mains) == 1 and len(outputs) == 1, "Expected the exact S073 source-host compiler entry")
    arguments[mains[0]] = "-main:S073LoadingGateControl"
    executable = output / "S073LoadingGateControl.exe"
    arguments[outputs[0]] = "-out:" + str(executable)
    arguments.append(str(harness))
    rsp = output / "control.rsp"
    rsp.write_text("\n".join(json.dumps(value) for value in arguments) + "\n")

    pins = build.get("source_and_reference_pins", {})
    dotnet = next((path for path in pins if Path(path).name == "dotnet"), None)
    compiler = next((path for path in pins if Path(path).name == "csc.dll"), None)
    need(dotnet and compiler, "Pinned Roslyn compiler required")
    compiled = subprocess.run([dotnet, compiler, "@" + str(rsp)], capture_output=True, text=True, timeout=120)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    need(compiled.returncode == 0, "Loading-gate control compilation failed; see compile.log")

    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    config = output / "mono-config.xml"
    helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    dllmaps = "".join(f'<dllmap dll="{name}" target="{helper}" os="!windows"/>' for name in ("MonoPosixHelper",))
    dllmaps += "".join(f'<dllmap dll="{name}" target="{native}" os="!windows"/>'
                        for name in ("System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"))
    config.write_text("<configuration>" + dllmaps + "</configuration>\n")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
                                        str(runtime["newtonsoft"].parent)])
    result = subprocess.run([str(runtime["mono"]), "--config", str(config), str(executable)],
                            env=env, capture_output=True, text=True, timeout=90)
    (output / "run.log").write_text(result.stdout + result.stderr)
    marker = "[S073LoadingGateControl] result=Passed fixtureOnly=1 runtimeAdmission=0"
    need(result.returncode == 0 and marker in result.stdout, "Loading-gate control failed; see run.log")
    need(sha(harness) == harness_sha, "Control source changed during run")
    print(json.dumps({"kind": "S073LoadingGateControlReceipt", "status": "Passed",
                      "control_sha256": harness_sha, "output": str(output),
                      "runtime_admission": False, "mission_readiness": False}, indent=2))


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: run_s073_loading_gate_control.py <passing-S073-build.json> <fresh-output-dir>")
    run(*sys.argv[1:])

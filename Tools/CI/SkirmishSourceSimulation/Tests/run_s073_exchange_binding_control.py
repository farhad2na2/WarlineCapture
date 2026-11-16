#!/usr/bin/env python3
"""Compile and run the focused S073 exchange source-binding control."""
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
    checkout = Path(__file__).resolve().parents[4]
    build_path = Path(build_path).resolve(strict=True)
    output = Path(output_path).absolute()
    need(not output.exists(), "Fresh isolated output directory required")
    output.mkdir(parents=True)
    build = json.loads(build_path.read_text())
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("mission") == "S073", "Passing S073 source build required")

    binding = checkout / "Tools/CI/SkirmishSourceSimulation/Adapters/S073ExchangeSourceBinding.cs"
    binding_sha = sha(binding)
    pins = build.get("source_and_reference_pins", {})
    need(pins.get(str(binding)) == binding_sha, "Source build does not pin the current production binding")

    graph_dir = output / "graph"
    generator = checkout / "Tools/CI/SkirmishSourceSimulation/Tests/generate_s073_exchange_source_graph.py"
    generated = subprocess.run([sys.executable, "-B", str(generator), "--repo", str(checkout),
                                "--output", str(graph_dir)], capture_output=True, text=True, timeout=60)
    (output / "graph-generation.log").write_text(generated.stdout + generated.stderr)
    need(generated.returncode == 0, "Actual S073 exchange graph generation failed; see graph-generation.log")
    graph_receipt = json.loads(generated.stdout)
    graph = graph_dir / "source-exchange-config-graph.json"
    need(graph_receipt.get("status") == "Passed" and
         graph_receipt.get("graph_sha256") == "cf51352a9046d8680cc8611b6d253fc812568b4169163afcb85e375ba7954427" and
         sha(graph) == graph_receipt["graph_sha256"], "Exact qualified S073 r3 graph required")

    response = build["compiler_response"]
    response_path = Path(response["path"]).resolve(strict=True)
    need(sha(response_path) == response["sha256"], "Source-build compiler response changed")
    arguments = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    mains = [i for i, value in enumerate(arguments) if value == "-main:SourceSimulationProgram"]
    outputs = [i for i, value in enumerate(arguments) if value.startswith("-out:")]
    need(len(mains) == 1 and len(outputs) == 1, "Expected checked source-host compiler entry")
    harness = checkout / "Tools/CI/SkirmishSourceSimulation/Tests/S073ExchangeBindingControl.cs"
    harness_sha = sha(harness)
    executable = output / "S073ExchangeBindingControl.exe"
    arguments[mains[0]] = "-main:S073ExchangeBindingControl"
    arguments[outputs[0]] = "-out:" + str(executable)
    arguments.append(str(harness))
    rsp = output / "control.rsp"
    rsp.write_text("\n".join(json.dumps(value) for value in arguments) + "\n")

    pinned_paths = list(pins)
    dotnet = next((path for path in pinned_paths if Path(path).name == "dotnet"), None)
    compiler = next((path for path in pinned_paths if Path(path).name == "csc.dll"), None)
    need(dotnet and compiler, "Pinned Roslyn compiler required")
    compile_result = subprocess.run([dotnet, compiler, "@" + str(rsp)], capture_output=True, text=True, timeout=120)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    need(compile_result.returncode == 0, "Control compilation failed; see compile.log")

    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    mono_config = output / "mono-config.xml"
    helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    mappings = ["MonoPosixHelper"]
    native_mappings = ["System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"]
    dllmaps = "".join(f'<dllmap dll="{name}" target="{helper}" os="!windows"/>' for name in mappings)
    dllmaps += "".join(f'<dllmap dll="{name}" target="{native}" os="!windows"/>' for name in native_mappings)
    mono_config.write_text("<configuration>" + dllmaps + "</configuration>\n")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([
        str(checkout / "Library/ScriptAssemblies"), str(runtime["engine"]),
        str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)
    ])
    result = subprocess.run([str(runtime["mono"]), "--config", str(mono_config), str(executable),
                             str(checkout), str(graph), str(output)],
                            env=env, capture_output=True, text=True, timeout=90)
    (output / "run.log").write_text(result.stdout + result.stderr)
    need(result.returncode == 0 and "[S073ExchangeBindingControl] result=Passed" in result.stdout,
         "Focused binding control failed; see run.log")
    need(sha(binding) == binding_sha and sha(harness) == harness_sha and sha(graph) == graph_receipt["graph_sha256"],
         "Pinned binding, harness, or graph changed during control")
    print(json.dumps({"status": "Passed", "kind": "S073ExchangeBindingControl",
                      "graph_sha256": sha(graph), "binding_sha256": binding_sha,
                      "harness_sha256": harness_sha, "control_stdout": result.stdout.strip(),
                      "output": str(output), "native_world_initialized": False,
                      "mission_readiness_claimed": False}, indent=2))


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: run_s073_exchange_binding_control.py <passing-S073-build.json> <fresh-output-dir>")
    run(*sys.argv[1:])

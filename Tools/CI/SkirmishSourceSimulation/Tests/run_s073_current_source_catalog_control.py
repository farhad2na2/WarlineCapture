#!/usr/bin/env python3
"""Run current S073 config binders without allocating a mission world."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import time


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(condition, message):
    if not condition:
        raise RuntimeError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True)
    parser.add_argument("--bundle", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    build_path = Path(args.build).resolve(strict=True)
    bundle_path = Path(args.bundle).resolve(strict=True)
    output = Path(args.output).absolute()
    need(not output.exists(), "Fresh output directory required")
    output.mkdir(parents=True)
    build = json.loads(build_path.read_text())
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed",
         "Passing exact source build required")
    repo = Path(build["repository_root"]).resolve(strict=True)
    pins = build["source_and_reference_pins"]
    for path, expected in pins.items():
        need(sha(path) == expected, "Build source pin changed: " + path)
    bundle = json.loads(bundle_path.read_text())
    need(bundle.get("schema_version") == 1 and bundle.get("mission") == "S073" and bundle.get("seed") == 104729,
         "Exact retained S073 diagnostic bundle required")
    def descriptor(name):
        row = bundle.get(name)
        need(isinstance(row, dict) and isinstance(row.get("path"), str) and isinstance(row.get("sha256"), str),
             "Exact bundle input required: " + name)
        path = Path(row["path"]).resolve(strict=True)
        need(sha(path) == row["sha256"], "Bundle input changed: " + name)
        return path, row["sha256"]
    definitions, _ = descriptor("building_definitions")
    legacy, _ = descriptor("legacy_config")
    identity, identity_sha = descriptor("source_identity")
    capture, capture_sha = descriptor("prefab_capture")
    control = repo / "Tools/CI/SkirmishSourceSimulation/Tests/S073CurrentSourceCatalogControl.cs"
    control_sha = sha(control)
    response = build["compiler_response"]
    response_path = Path(response["path"]).resolve(strict=True)
    need(sha(response_path) == response["sha256"], "Pinned compiler response changed")
    argv = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    main_ix = [i for i, value in enumerate(argv) if value == "-main:SourceSimulationProgram"]
    out_ix = [i for i, value in enumerate(argv) if value.startswith("-out:")]
    need(len(main_ix) == 1 and len(out_ix) == 1, "Expected one checked source entrypoint/output")
    exe = output / "S073CurrentSourceCatalogControl.exe"
    argv[main_ix[0]] = "-main:S073CurrentSourceCatalogControl"
    argv[out_ix[0]] = "-out:" + str(exe)
    argv.append(str(control))
    test_response = output / "control.rsp"
    test_response.write_text("\n".join(json.dumps(value) for value in argv) + "\n")
    tool_pins = list(pins)
    dotnet = next((path for path in tool_pins if Path(path).name == "dotnet"), None)
    compiler = next((path for path in tool_pins if Path(path).name == "csc.dll"), None)
    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    need(dotnet and compiler, "Build-pinned dotnet/Roslyn missing")
    mono_config = output / "mono-config.xml"
    native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    posix = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    native_names = ("System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple")
    maps = ''.join(f'<dllmap dll="{name}" target="{native}" os="!windows"/>' for name in native_names)
    maps += f'<dllmap dll="MonoPosixHelper" target="{posix}" os="!windows"/>'
    mono_config.write_text("<configuration>" + maps + "</configuration>\n")
    compile_result = subprocess.run([dotnet, compiler, "@" + str(test_response)], capture_output=True,
                                    text=True, timeout=120)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    run_result = None
    if compile_result.returncode == 0:
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join([str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
                                             str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)])
        command = [str(runtime["mono"]), "--config", str(mono_config), str(exe), str(repo), str(definitions),
                   str(legacy), str(identity), identity_sha, str(capture), capture_sha, str(output / "control-result.json")]
        began = time.monotonic()
        run_result = subprocess.run(command, env=env, capture_output=True, text=True, timeout=60)
        (output / "run.log").write_text(run_result.stdout + run_result.stderr)
        elapsed = time.monotonic() - began
    need(sha(control) == control_sha, "Control source changed during execution")
    for path, expected in pins.items():
        need(sha(path) == expected, "Build source changed during control: " + path)
    need(compile_result.returncode == 0 and run_result is not None and run_result.returncode == 0 and
         "[S073CurrentSourceCatalogControl] result=Passed" in (run_result.stdout + run_result.stderr),
         "Current-source catalog control failed; see compile.log/run.log")
    receipt = {"kind": "S073CurrentSourceCatalogControlReceipt", "status": "Passed",
               "scope": "Current asset config owners and exact identity rows; no mission world/readiness/outcome",
               "mission_world_allocated": False, "identity_fixture_entity_manager": True,
               "build_receipt": str(build_path), "build_receipt_sha256": sha(build_path),
               "bundle": str(bundle_path), "bundle_sha256": sha(bundle_path),
               "control_source": str(control), "control_sha256": control_sha,
               "compile_exit": compile_result.returncode, "run_exit": run_result.returncode,
               "elapsed_seconds": elapsed,
               "output": str(output / "control-result.json")}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt, separators=(",", ":")))


if __name__ == "__main__":
    main()

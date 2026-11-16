#!/usr/bin/env python3
"""Compile and run the original path owner on a standalone isolated shim fixture."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(ok, message):
    if not ok:
        raise RuntimeError(message)


def run(build_path, output_path, mono_config):
    build_path = Path(build_path).resolve(strict=True)
    output = Path(output_path).absolute()
    config = Path(mono_config).resolve(strict=True)
    need(not output.exists(), "Fresh isolated output directory required")
    output.mkdir(parents=True)
    build = json.loads(build_path.read_text())
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("mission") == "S073" and build.get("runtime_backend") == "LegacyS073OwnerV1",
         "Passing current S073 source build receipt required")
    pins = build["source_and_reference_pins"]
    for path, expected in pins.items():
        need(sha(path) == expected, "Built source pin changed: " + path)
    response = build["compiler_response"]
    response_path = Path(response["path"]).resolve(strict=True)
    need(sha(response_path) == response["sha256"], "Compiler response changed")
    args = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    output_indices = [i for i, arg in enumerate(args) if isinstance(arg, str) and arg.startswith("-out:")]
    need(len(output_indices) == 1, "Unique executable output required")
    args.append(str(Path(__file__).resolve().with_name("S073PathfindingOwnerFixtureControl.cs")))
    args.append("-main:S073PathfindingFixtureEntry")
    exe = output / "S073PathfindingOwnerFixture.exe"
    args[output_indices[0]] = "-out:" + str(exe)
    rsp = output / "fixture.rsp"
    rsp.write_text("\n".join(json.dumps(arg) for arg in args) + "\n")
    dots = [Path(path) for path in pins if Path(path).name == "dotnet"]
    csc = sorted((Path(path) for path in pins if Path(path).name == "csc.dll"),
                 key=lambda p: tuple(int(part) for part in p.parents[2].name.split(".")))
    need(len(dots) == 1 and csc, "Pinned Roslyn and dotnet required")
    compiled = subprocess.run([str(dots[0]), str(csc[-1]), "@" + str(rsp)], capture_output=True, text=True, timeout=120)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    need(compiled.returncode == 0, "Fixture compilation failed; inspect compile.log")
    mono = Path(build["runtime_selection"]["mono"]).resolve(strict=True)
    need(str(mono) in pins and sha(mono) == pins[str(mono)], "Build-pinned Mono required")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([str(Path(build["repository_root"]) / "Library/ScriptAssemblies"),
                                        str(Path(build["runtime_selection"]["engine"])),
                                        str(Path(build["runtime_selection"]["newtonsoft"]).parent),
                                        str(Path(build["placement_build"]).parent)])
    process = subprocess.run([str(mono), "--config", str(config), "--optimize=-float32", str(exe)],
                             capture_output=True, text=True, timeout=120, env=env)
    (output / "run.log").write_text(process.stdout + process.stderr)
    need(process.returncode == 0, "Fixture owner run failed; inspect run.log")
    lines = [line for line in process.stdout.splitlines() if line.startswith('{')]
    need(len(lines) == 1, "One fixture JSON report required")
    fixture = json.loads(lines[0])
    need(fixture.get("status") == "Passed" and fixture.get("reachable_path_applied") and
         fixture.get("unreachable_path_rejected") and fixture.get("s073_scheduler_admission") is False and
         fixture.get("mission_ready") is False,
         "Original owner fixture assertions failed")
    receipt = {"schema_version": 1, "kind": "S073PathfindingOwnerFixtureControlReceipt", "status": "Passed",
               "build_receipt": {"path": str(build_path), "sha256": sha(build_path)},
               "fixture_report": fixture, "compile_log": {"path": str(output / "compile.log"), "sha256": sha(output / "compile.log")},
               "run_log": {"path": str(output / "run.log"), "sha256": sha(output / "run.log")},
               "standalone_isolated_fixture": True, "s073_scheduler_admitted": False,
               "native_async_or_system_group_parity": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt, separators=(",", ":")))


if __name__ == "__main__":
    if len(sys.argv) != 4:
        raise SystemExit("usage: run_s073_pathfinding_owner_fixture_control.py <current-build.json> <fresh-output> <Mono-config.xml>")
    run(sys.argv[1], sys.argv[2], sys.argv[3])

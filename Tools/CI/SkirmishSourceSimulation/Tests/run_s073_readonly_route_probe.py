#!/usr/bin/env python3
"""Run original PathfindBatchJob routes against the retained S073 source host."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import types

current_stage = "arguments"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(condition, message):
    if not condition:
        raise RuntimeError(message)


def run(build_path, bundle_path, output_path, timeout=1800):
    global current_stage
    current_stage = "receipt-and-source-pin-checks"
    build_path = Path(build_path).resolve(strict=True)
    bundle_path = Path(bundle_path).resolve(strict=True)
    output = Path(output_path).absolute()
    need(not output.exists(), "Fresh isolated output directory required")
    output.mkdir(parents=True)
    receipt = json.loads(build_path.read_text())
    need(receipt.get("kind") == "SkirmishSourceSimulationBuild" and
         receipt.get("status") == "Passed" and receipt.get("mission") == "S073" and
         receipt.get("runtime_backend") == "LegacyS073OwnerV1",
         "Passing current S073 source build receipt required")
    repo = Path(receipt["repository_root"]).resolve(strict=True)
    pins = receipt["source_and_reference_pins"]
    for path, expected in pins.items():
        need(sha(path) == expected, "Built source pin changed: " + path)

    response_descriptor = receipt["compiler_response"]
    response_path = Path(response_descriptor["path"]).resolve(strict=True)
    need(sha(response_path) == response_descriptor["sha256"], "Compiler response changed")
    args = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    normal_run_sources = [i for i, arg in enumerate(args)
                          if isinstance(arg, str) and Path(arg).name == "SourceS073LegacyNormalRun.cs"]
    outputs = [i for i, arg in enumerate(args) if isinstance(arg, str) and arg.startswith("-out:")]
    need(len(normal_run_sources) == 1 and len(outputs) == 1,
         "Expected the exact S073 host source and one executable output in the build response")

    host_source = Path(args[normal_run_sources[0]]).resolve(strict=True)
    host_text = host_source.read_text()
    call = "host.RetainS073LegacyDiagnosticStartup(em, grid, surface, matches.GetSingletonEntity(), boundary, plan, units.ToArray());"
    need(host_text.count(call) == 1, "Unique retained-host boundary required")
    patched_host = output / "SourceS073LegacyNormalRun.RouteProbe.cs"
    helper = repo / "Tools/CI/SkirmishSourceSimulation/Tests/S073ReadOnlyRouteProbe.cs"
    route_file = 'Path.Combine(directory, "s073-readonly-route-probe.json")'
    injected = call + "\n                    S073ReadOnlyRouteProbe.Capture(em, grid, surface, units.ToArray(), " + route_file + ");"
    patched_host.write_text(host_text.replace(call, injected))

    args[normal_run_sources[0]] = str(patched_host)
    args.append(str(helper))
    probe_exe = output / "SourceSimulation.RouteProbe.exe"
    args[outputs[0]] = "-out:" + str(probe_exe)
    response_out = output / "route-probe.rsp"
    response_out.write_text("\n".join(json.dumps(arg) for arg in args) + "\n")

    dotnet_candidates = [Path(path) for path in pins if Path(path).name == "dotnet"]
    csc_candidates = sorted(
        (Path(path) for path in pins if Path(path).name == "csc.dll"),
        key=lambda path: tuple(int(part) for part in path.parents[2].name.split(".")))
    need(len(dotnet_candidates) == 1 and len(csc_candidates) >= 1, "Pinned Roslyn and dotnet tools required")
    started = time.monotonic()
    current_stage = "probe-compilation"
    compiled = subprocess.run([str(dotnet_candidates[0]), str(csc_candidates[-1]), "@" + str(response_out)],
                              capture_output=True, text=True, timeout=120)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    need(compiled.returncode == 0, "Read-only route probe compilation failed; inspect compile.log")
    current_stage = "host-run"

    patched_pins = dict(pins)
    patched_pins[str(patched_host)] = sha(patched_host)
    patched_pins[str(helper.resolve())] = sha(helper)
    patched_pins[str(response_out.resolve())] = sha(response_out)
    patched_receipt = dict(receipt)
    patched_receipt["source_and_reference_pins"] = patched_pins
    patched_receipt["executable"] = {"path": str(probe_exe), "sha256": sha(probe_exe), "bytes": probe_exe.stat().st_size}
    patched_receipt["scope"] = "Original source host plus read-only direct PathfindBatchJob diagnostic; no scheduler admission"
    patched_build_path = output / "route-probe-build.json"
    patched_build_path.write_text(json.dumps(patched_receipt, indent=2) + "\n")

    ci_root = repo / "Tools/CI"
    sys.path.insert(0, str(ci_root))
    loader_path = ci_root / "skirmish_owned_source.py"
    loader_bytes = loader_path.read_bytes()
    need("skirmish_owned_source" not in sys.modules, "Owned loader module collision")
    owned_loader = types.ModuleType("skirmish_owned_source")
    owned_loader.__file__ = str(loader_path)
    owned_loader.__executed_source_pin__ = {"path": str(loader_path), "sha256": hashlib.sha256(loader_bytes).hexdigest()}
    sys.modules[owned_loader.__name__] = owned_loader
    exec(compile(loader_bytes, str(loader_path), "exec"), owned_loader.__dict__)
    need(loader_path.read_bytes() == loader_bytes, "Owned loader changed during bootstrap")
    owned_loader.load_cli(ci_root)
    import skirmish_source_build
    host_output = output / "retained-host"
    run_receipt = skirmish_source_build.run(repo, patched_build_path, bundle_path, host_output,
                                           None, timeout, None, False)
    route_path = host_output / "s073-readonly-route-probe.json"
    need(route_path.is_file(), "Retained S073 host did not emit the route diagnostic")
    route = json.loads(route_path.read_text())
    need(route.get("kind") == "S073ReadOnlyRouteProbe" and
         route.get("scheduler_admitted") is False and route.get("movement_or_command_issued") is False and
         route.get("mission_ready") is False and len(route.get("routes", [])) == 24,
         "Complete unqualified read-only route report required")
    final = {
        "schema_version": 1,
        "kind": "S073ReadOnlyRouteProbeReceipt",
        "status": "Passed",
        "elapsed_seconds": time.monotonic() - started,
        "build_receipt": {"path": str(build_path), "sha256": sha(build_path)},
        "bundle": {"path": str(bundle_path), "sha256": sha(bundle_path)},
        "probe_executable": {"path": str(probe_exe), "sha256": sha(probe_exe)},
        "route_report": {"path": str(route_path), "sha256": sha(route_path), "route_count": 24,
                         "found_count": sum(1 for item in route["routes"] if item["route_found"])},
        "retained_host_status": run_receipt.get("status"),
        "scheduler_admitted": False,
        "movement_or_command_issued": False,
        "mission_ready": False,
        "qualification": "Source-only route diagnosis over actual retained S073 grid, committed blocker state, rebuilt occupancy, surface blob, spawned units, and live opposing buildings",
        "compile_log": {"path": str(output / "compile.log"), "sha256": sha(output / "compile.log")},
        "host_run_receipt": run_receipt,
    }
    (output / "receipt.json").write_text(json.dumps(final, indent=2) + "\n")
    print(json.dumps(final, separators=(",", ":")))
    return 0


if __name__ == "__main__":
    if len(sys.argv) not in (4, 5):
        raise SystemExit("usage: run_s073_readonly_route_probe.py <build.json> <S073-bundle.json> <fresh-output> [timeout-seconds]")
    try:
        raise SystemExit(run(sys.argv[1], sys.argv[2], sys.argv[3], float(sys.argv[4]) if len(sys.argv) == 5 else 1800))
    except Exception as error:
        output = Path(sys.argv[3]).absolute()
        if output.is_dir():
            files = {}
            for name in ["compile.log", "retained-host/process.log", "retained-host/result.json"]:
                path = output / name
                if path.is_file():
                    files[name] = {"path": str(path), "sha256": sha(path), "bytes": path.stat().st_size}
            failure = {"schema_version": 1, "kind": "S073ReadOnlyRouteProbeReceipt", "status": "Failed",
                       "stage": current_stage, "error": repr(error), "evidence_files": files,
                       "scheduler_admitted": False, "movement_or_command_issued": False, "mission_ready": False}
            (output / "failure.json").write_text(json.dumps(failure, indent=2) + "\n")
        print("S073 read-only route probe failed at " + current_stage + ": " + repr(error), file=sys.stderr)
        raise

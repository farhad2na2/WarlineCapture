#!/usr/bin/env python3
"""Compile and execute the same disposable-world owner fixture against two hosts."""
from __future__ import annotations

import hashlib
import argparse
import json
import os
from pathlib import Path
import statistics
import subprocess
import time

CONTROL = Path(__file__).with_name("StaticBlockerOwnerHistoryHostControl.cs")

def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()

def need(value: bool, message: str) -> None:
    if not value:
        raise ValueError(message)

def compile_and_run(build_path: Path, label: str, output: Path, repo: Path) -> dict:
    build = json.loads(build_path.read_text(encoding="utf-8"))
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("mission") == "S002" and build.get("seed") == 104731,
         "Passing S002 source-host compile receipt required for " + label)
    response = build["compiler_response"]
    response_path = Path(response["path"]).resolve(strict=True)
    need(sha(response_path) == response["sha256"], "Compiler response changed for " + label)
    args = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    main_indices = [i for i, value in enumerate(args) if value == "-main:SourceSimulationProgram"]
    output_indices = [i for i, value in enumerate(args) if value.startswith("-out:")]
    need(len(main_indices) == 1 and len(output_indices) == 1, "Expected unique host entry point/output")
    output.mkdir(parents=True, exist_ok=False)
    executable = output / ("StaticBlockerOwnerHistoryHostControl-" + label + ".exe")
    args[main_indices[0]] = "-main:StaticBlockerOwnerHistoryHostControl"
    args[output_indices[0]] = "-out:" + str(executable)
    args.append(str(CONTROL.resolve(strict=True)))
    rsp = output / "fixture.rsp"
    rsp.write_text("\n".join(json.dumps(value) for value in args) + "\n")
    pins = build["source_and_reference_pins"]
    dotnet = next((Path(p) for p in pins if Path(p).name == "dotnet"), None)
    compiler = next((Path(p) for p in pins if Path(p).name == "csc.dll"), None)
    need(dotnet is not None and compiler is not None, "Pinned Roslyn toolchain missing")
    compiled = subprocess.run([str(dotnet), str(compiler), "@" + str(rsp)], capture_output=True,
                              text=True, timeout=180)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    need(compiled.returncode == 0, "Compiled owner fixture failed for " + label)
    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    contents = runtime["contents"]
    native_helper = contents / "Frameworks/libMonoPosixHelper.dylib"
    mono_native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    need(native_helper.is_file() and mono_native.is_file(), "Pinned Mono runtime support missing")
    maps = '<dllmap dll="MonoPosixHelper" target="' + str(native_helper) + '" os="!windows"/>'
    maps += ''.join('<dllmap dll="' + name + '" target="' + str(mono_native) + '" os="!windows"/>'
                    for name in ("System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"))
    config = output / "mono-config.xml"
    config.write_text("<configuration>" + maps + "</configuration>\n")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
        str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)])
    started = time.monotonic()
    ran = subprocess.run([str(runtime["mono"]), "--config", str(config), str(executable)],
                         env=env, capture_output=True, text=True, timeout=120)
    (output / "run.log").write_text(ran.stdout + ran.stderr)
    need(ran.returncode == 0, "Actual owner fixture execution failed for " + label)
    marker = "[StaticBlockerOwnerHistoryHostControl] "
    lines = [line[len(marker):] for line in ran.stdout.splitlines() if line.startswith(marker)]
    need(len(lines) == 1, "Required actual-owner fixture result marker missing for " + label)
    result = json.loads(lines[0])
    need(result.get("status") == "Passed" and result.get("source_host_world_fixture") is True and
         result.get("mission_run") is False and result.get("case_count") == 5,
         "Actual-owner diagnostic fixture receipt mismatch for " + label)
    for path, expected in pins.items():
        need(sha(Path(path)) == expected, "Pinned build input changed during fixture: " + path)
    return {"label": label, "build_receipt": str(build_path), "build_receipt_sha256": sha(build_path),
            "compiler_response_sha256": response["sha256"], "fixture_sha256": sha(CONTROL),
            "fixture_executable_sha256": sha(executable), "compile_exit": compiled.returncode,
            "run_exit": ran.returncode, "elapsed_seconds": round(time.monotonic() - started, 3),
            "result": result}

def run(original_build: Path, candidate_build: Path, output: Path, repo: Path) -> dict:
    original = json.loads(original_build.read_text())
    candidate = json.loads(candidate_build.read_text())
    base_pins = original.get("source_and_reference_pins", {})
    candidate_pins = candidate.get("source_and_reference_pins", {})
    common = set(base_pins) & set(candidate_pins)
    mismatches = [p for p in common if base_pins[p] != candidate_pins[p]]
    need(not mismatches, "Non-experimental shared source/build pin drift: " + repr(mismatches[:12]))
    need(original.get("source_count") == candidate.get("source_count") == 708,
         "Same full source-host input count required")
    need(original.get("canonical_source_manifest", {}).get("sha256") ==
         candidate.get("canonical_source_manifest", {}).get("sha256"),
         "Canonical original source manifest differs between host builds")
    output.mkdir(parents=True, exist_ok=False)
    started = time.monotonic()
    runs = {"original": [], "candidate": []}
    timing_order = []
    reference_state = None
    repetitions = 5
    for repetition in range(repetitions):
        labels = ["original", "candidate"] if repetition % 2 == 0 else ["candidate", "original"]
        timing_order.extend(labels)
        for label in labels:
            build_path = original_build if label == "original" else candidate_build
            record = compile_and_run(build_path, label,
                                     output / ("repeat-" + str(repetition + 1) + "-" + label), repo)
            result = record["result"]
            state = {"cases": result["cases"], "steady_blocker_rows": result["steady_blocker_rows"],
                     "steady_cases": [{"kind": item["kind"], "before": item["before"], "after": item["after"],
                                       "full_update_measured": item["full_update_measured"],
                                       "setup_and_state_hashing_included": item["setup_and_state_hashing_included"]}
                                      for item in result["steady_cases"]]}
            if reference_state is None:
                reference_state = state
            need(state == reference_state,
                 "Actual owner post-state differed by candidate or repetition/order")
            runs[label].append(record)
    timing_summary = {}
    for label, records in runs.items():
        timing_summary[label] = {}
        for case_kind in ("warm_steady_state", "forced_rebuild"):
            samples = [next(item for item in record["result"]["steady_cases"]
                            if item["kind"] == case_kind)["on_update_elapsed_ms"]
                       for record in records]
            timing_summary[label][case_kind] = {"samples_ms": samples,
                "median_ms": statistics.median(samples), "min_ms": min(samples), "max_ms": max(samples)}
    receipt = {"schema_version": 1, "kind": "StaticBlockerOwnerHistoryCompiledDifferential",
        "status": "Passed", "same_host_source_count": 708, "shared_source_pins_compared": len(common),
        "shared_source_pin_mismatches": [], "mission_run": False, "unity_run": False,
        "source_provider_parity_qualified": False, "performance_claimed": False,
        "timing_scope": "Five alternating repetitions of full StaticGridBlockerUpdateSystem.OnUpdate in disposable source-shim worlds",
        "timing_order": timing_order, "timing_summary": timing_summary,
        "blocker_rows": 4972, "world_grid": {"width": 2048, "height": 1024},
        "elapsed_seconds": round(time.monotonic() - started, 3), "original_runs": runs["original"],
        "candidate_runs": runs["candidate"],
        "comparison": "Exact owner/storage/history post-state JSON summaries across original/candidate and all timing repetitions"}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt

def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--original-build", type=Path, required=True)
    parser.add_argument("--candidate-build", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--repo", type=Path, required=True)
    args = parser.parse_args()
    receipt = run(args.original_build.resolve(strict=True), args.candidate_build.resolve(strict=True),
                  args.output, args.repo.resolve(strict=True))
    print("[run_compiled_owner_differential] status=" + receipt["status"] +
          " source_host_cases=" + str(receipt["original"]["result"]["case_count"]))

if __name__ == "__main__":
    main()

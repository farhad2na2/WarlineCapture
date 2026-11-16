#!/usr/bin/env python3
"""Compile and execute the extracted original collector over a pinned pregame source projection."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[4]
GENERATOR = HERE / "generate_source_owner.py"
REFERENCE_BUILDER = HERE / "prepare_projection_reference.py"
RUNNER = Path(__file__).resolve()
SHIM = HERE / "SourceRenderBoundsShim.cs"
FIXTURE = HERE / "SourceStaticRenderBoundsProjectionReplayFixture.cs"
SOURCE_CAPTURE = REPO / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json"
PROJECTION = REPO / "Logs/SkirmishProduction/s073-clearance-projection-input-20261102-r1/static-clearance-from-capture.json"
REPORT = REPO / "Logs/SkirmishProduction/s073-static-clearance-bound-multiset-20261103-r2/report.json"
OWNER = REPO / "Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs"
CAPTURE_SOURCE = REPO / "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073StreamedWorld.cs"
REFERENCE_ALGORITHM = REPO / "Tools/CI/SkirmishSourceSimulation/Tests/compare_s073_static_clearance_multiset.py"
PINS = {
    SOURCE_CAPTURE: "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8",
    PROJECTION: "8b5087c8a96ad81113c93fb4fba1632f10c8920bd2ed16fcb168e58d22572065",
    REPORT: "c488ea5c8d70217e21833bf493add8380aaf64f1e2e86a5a8134993c31bb7275",
    OWNER: "82bf379f5bb3e5ae55705f608c3e4a26ed915afffeec7488c79b9448cecfb5a4",
    CAPTURE_SOURCE: "f278e4383dfb4ae3ad3f00d7b1da4cedce75625cbdceeaff7bdf60d9d9614346",
    REFERENCE_ALGORITHM: "b29bdd5457b6563706f2d288ebbef61c4d2ebdbb2578cc0ad75d74d9bfbbdf41",
}
def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(value: bool, message: str) -> None:
    if not value:
        raise ValueError(message)


def verify_pins() -> None:
    for path, expected in PINS.items():
        need(sha(path) == expected, "Pinned source/projection/collector input changed: " + str(path))
    need(sha(GENERATOR) == GENERATOR_SHA and sha(REFERENCE_BUILDER) == REFERENCE_BUILDER_SHA and
         sha(RUNNER) == RUNNER_SHA and
         sha(SHIM) == SHIM_SHA and sha(FIXTURE) == FIXTURE_SHA,
         "Replay recipe source changed during run")


GENERATOR_SHA = ""
REFERENCE_BUILDER_SHA = ""
RUNNER_SHA = ""
SHIM_SHA = ""
FIXTURE_SHA = ""


def run(build_path: Path, output: Path) -> dict:
    global GENERATOR_SHA, REFERENCE_BUILDER_SHA, RUNNER_SHA, SHIM_SHA, FIXTURE_SHA
    build_path = build_path.resolve(strict=True)
    output = output.absolute()
    need(not output.exists(), "Fresh isolated output directory required")
    output.mkdir(parents=True)
    GENERATOR_SHA = sha(GENERATOR)
    REFERENCE_BUILDER_SHA = sha(REFERENCE_BUILDER)
    RUNNER_SHA = sha(RUNNER)
    SHIM_SHA = sha(SHIM)
    FIXTURE_SHA = sha(FIXTURE)
    verify_pins()

    build_raw = build_path.read_bytes()
    build = json.loads(build_raw)
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("mission") == "S073", "Passing checked S073 source-host build receipt required")
    response_info = build["compiler_response"]
    response_path = Path(response_info["path"]).resolve(strict=True)
    need(sha(response_path) == response_info["sha256"], "Pinned compiler response changed")
    build_pins = build.get("source_and_reference_pins", {})
    resolved_build_pins = {Path(path).resolve(strict=True): expected for path, expected in build_pins.items()}
    pin_mismatches = {Path(path).resolve(strict=True): (expected, sha(Path(path).resolve(strict=True)))
                      for path, expected in build_pins.items()
                      if sha(Path(path).resolve(strict=True)) != expected}
    need(not pin_mismatches,
         "Source-host build pin drift: " + repr([str(path) for path in pin_mismatches]))
    source_arguments = {Path(value).resolve(strict=True) for value in
                        [json.loads(line) for line in response_path.read_text(encoding="utf-8").splitlines()
                         if line.strip()] if value.endswith(".cs")}
    need(all(path in resolved_build_pins for path in source_arguments),
         "Every host compiler source must have an exact retained source hash")
    for path, expected in build_pins.items():
        resolved = Path(path).resolve(strict=True)
        need(sha(resolved) == expected, "Source-host compiler input changed: " + path)

    generated_dir = output / "generated"
    extraction = subprocess.run([sys.executable, str(GENERATOR), "--source", str(OWNER), "--output",
                                 str(generated_dir)], capture_output=True, text=True, timeout=30)
    (output / "extraction.log").write_text(extraction.stdout + extraction.stderr, encoding="utf-8")
    need(extraction.returncode == 0, "Fail-closed original owner extraction failed")
    generated_owner = generated_dir / "ExtractedOriginalStaticRenderBoundsOwner.cs"
    reference_path = generated_dir / "source-reference.json"
    reference = subprocess.run([sys.executable, str(REFERENCE_BUILDER), "--repo", str(REPO),
                                "--output", str(reference_path)], capture_output=True, text=True, timeout=90)
    (output / "reference.log").write_text(reference.stdout + reference.stderr, encoding="utf-8")
    need(reference.returncode == 0, "Pinned independent source reference generation failed")

    args = [json.loads(line) for line in response_path.read_text(encoding="utf-8").splitlines() if line.strip()]
    mains = [i for i, value in enumerate(args) if value == "-main:SourceSimulationProgram"]
    outputs = [i for i, value in enumerate(args) if value.startswith("-out:")]
    need(len(mains) == 1 and len(outputs) == 1, "Expected unique source-host main/output arguments")
    args[mains[0]] = "-main:SourceStaticRenderBoundsProjectionReplayFixture"
    executable = output / "SourceStaticRenderBoundsProjectionReplayFixture.exe"
    args[outputs[0]] = "-out:" + str(executable)
    args.extend(str(path.resolve(strict=True)) for path in (SHIM, generated_owner, FIXTURE))
    fixture_response = output / "fixture.rsp"
    fixture_response.write_text("\n".join(json.dumps(value) for value in args) + "\n", encoding="utf-8")

    pins = build["source_and_reference_pins"]
    dotnet = next((Path(path) for path in pins if Path(path).name == "dotnet"), None)
    compiler = next((Path(path) for path in pins if Path(path).name == "csc.dll"), None)
    need(dotnet is not None and compiler is not None, "Pinned Roslyn toolchain missing")
    started = time.monotonic()
    compiled = subprocess.run([str(dotnet), str(compiler), "@" + str(fixture_response)], capture_output=True,
                              text=True, timeout=240)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr, encoding="utf-8")
    need(compiled.returncode == 0, "Actual owner projection replay fixture failed compilation")

    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    mono_helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    mono_native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    need(mono_helper.is_file() and mono_native.is_file(), "Pinned Mono support files missing")
    mappings = '<dllmap dll="MonoPosixHelper" target="' + str(mono_helper) + '" os="!windows"/>'
    mappings += ''.join('<dllmap dll="' + name + '" target="' + str(mono_native) + '" os="!windows"/>'
                        for name in ("System.Native", "System.Net.Security.Native",
                                     "System.Security.Cryptography.Native.Apple"))
    config = output / "mono-config.xml"
    config.write_text("<configuration>" + mappings + "</configuration>\n", encoding="utf-8")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([str(REPO / "Library/ScriptAssemblies"), str(runtime["engine"]),
        str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)])
    ran = subprocess.run([str(runtime["mono"]), "--config", str(config), str(executable),
                          str(PROJECTION), str(reference_path)], env=env,
                         capture_output=True, text=True, timeout=300)
    (output / "run.log").write_text(ran.stdout + ran.stderr, encoding="utf-8")
    marker = "[SourceStaticRenderBoundsProjectionReplayFixture] result=Passed source_projection_inputs=90865 selected=2400"
    need(ran.returncode == 0 and marker in ran.stdout, "Original collector source-projection replay failed")
    verify_pins()
    need(sha(response_path) == response_info["sha256"], "Compiler response changed during replay")
    for path, expected in build_pins.items():
        resolved = Path(path).resolve(strict=True)
        need(sha(resolved) == expected, "Source-host compiler input changed during replay: " + path)

    receipt = {
        "schema_version": 1,
        "kind": "S073CapturedPregameSourceProjectionOriginalCollectorReplay",
        "status": "Passed",
        "build_receipt": str(build_path),
        "build_receipt_sha256": hashlib.sha256(build_raw).hexdigest(),
        "compiler_response_sha256": response_info["sha256"],
        "host_source_input_pin_drift": [{"path": str(path), "retained_sha256": expected,
            "compiled_sha256": current} for path, (expected, current) in pin_mismatches.items()],
        "all_host_source_hashes_verified_before_after": True,
        "source_capture_path": str(SOURCE_CAPTURE),
        "source_capture_sha256": PINS[SOURCE_CAPTURE],
        "projection_path": str(PROJECTION),
        "projection_sha256": PINS[PROJECTION],
        "collector_path": str(OWNER),
        "collector_sha256": PINS[OWNER],
        "capture_producer_path": str(CAPTURE_SOURCE),
        "capture_producer_sha256": PINS[CAPTURE_SOURCE],
        "independent_reference_algorithm_path": str(REFERENCE_ALGORITHM),
        "independent_reference_algorithm_sha256": PINS[REFERENCE_ALGORITHM],
        "retained_query_report_sha256": PINS[REPORT],
        "generated_owner_sha256": sha(generated_owner),
        "source_reference_sha256": sha(reference_path),
        "compiled_executable_sha256": sha(executable),
        "case_count": 90865,
        "selected_bound_count": 2400,
        "duplicate_aware_multiset_match": True,
        "synthetic_source_entity_identity_count": 90865,
        "synthetic_exclusion_control_count": 3,
        "scope": "Replay of a retained pregame eligible source-bounds projection only; not a mission world",
        "native_state_imported": False,
        "live_native_domain_empty_claim": False,
        "current_s073_source_fit_claim": False,
        "mission_world_admission": False,
        "readiness_claim": False,
        "native_chunk_order_claim": False,
        "unity_run": False,
        "compile_exit": compiled.returncode,
        "run_exit": ran.returncode,
        "elapsed_seconds": round(time.monotonic() - started, 3),
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = run(args.build, args.output)
    print("[run_projection_replay] status=" + result["status"] +
          " source_bounds=" + str(result["case_count"]) +
          " selected=" + str(result["selected_bound_count"]) +
          " multiset=" + str(result["duplicate_aware_multiset_match"]).lower())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

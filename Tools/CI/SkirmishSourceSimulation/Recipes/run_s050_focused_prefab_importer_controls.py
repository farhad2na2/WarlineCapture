#!/usr/bin/env python3
"""Compile and run no-world S050 focused prefab reader/importer controls."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
CONTROL = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S050FocusedPrefabImporterControls.cs"
VIEW = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/419_GeneratedReferenceTemplateView.cs"
IMPORTER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.cs"
PASS = "[S050FocusedPrefabImporterControls] result=Passed positive=1 negative=7 worldAllocated=False missionReady=False"


def sha(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def need(condition, message):
    if not condition:
        raise ValueError(message)


def verify_pins(pins):
    need(isinstance(pins, dict) and pins, "Complete checked source-build pin map required")
    drift = []
    for raw_path, expected in pins.items():
        path = Path(raw_path)
        if not path.is_file() or path.is_symlink() or sha(path) != expected:
            drift.append(raw_path)
            if len(drift) >= 12:
                break
    need(not drift, "Current source build pin drift: " + ", ".join(drift))


def run(build_dir, descriptor_path, output):
    build_dir = build_dir.resolve(strict=True)
    descriptor_path = descriptor_path.resolve(strict=True)
    output = output.resolve()
    need(build_dir.is_relative_to(ROOT) and descriptor_path.is_relative_to(ROOT) and output.is_relative_to(ROOT),
         "Build, descriptor, and fresh output must remain inside the repository")
    need(not output.exists(), "Fresh S050 importer-control output required")
    output.mkdir(parents=True)
    receipt_path = output / "receipt.json"
    receipt = {"schema_version": 1, "kind": "S050FocusedPrefabImporterControlReceipt",
               "status": "Failed", "build_dir": str(build_dir), "descriptor": str(descriptor_path),
               "unity_launched": False, "world_allocated": False, "mission_ready": False}
    try:
        build_path = build_dir / "build.json"
        build_raw = build_path.read_bytes()
        build = json.loads(build_raw)
        need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
             build.get("compile_exit") == 0 and Path(build.get("repository_root", "")).resolve() == ROOT,
             "Passed current repository source-host build required")
        pins = build.get("source_and_reference_pins", {})
        verify_pins(pins)
        need(pins.get(str(VIEW)) == sha(VIEW) and pins.get(str(IMPORTER)) == sha(IMPORTER),
             "Checked build must pin exact current focused reader and compiled importer sources")
        need(pins.get(str(CONTROL)) is None, "Isolated S050 control source must stay outside production source closure")
        response_info = build.get("compiler_response", {})
        response = Path(response_info.get("path", "")).resolve(strict=True)
        exe_info = build.get("executable", {})
        source_exe = Path(exe_info.get("path", "")).resolve(strict=True)
        need(response.is_relative_to(build_dir) and source_exe.is_relative_to(build_dir) and
             sha(response) == response_info.get("sha256") and sha(source_exe) == exe_info.get("sha256"),
             "Exact source build response and executable required")
        runtime = build.get("runtime_selection", {})
        compiler = Path(next(p for p in pins if p.endswith("/Roslyn/bincore/csc.dll"))).resolve(strict=True)
        dotnet = Path(next(p for p in pins if Path(p).name == "dotnet")).resolve(strict=True)
        mono = Path(runtime.get("mono", "")).resolve(strict=True)
        engine = Path(runtime.get("engine", "")).resolve(strict=True)
        newtonsoft = Path(runtime.get("newtonsoft", "")).resolve(strict=True)
        need(compiler.is_file() and dotnet.is_file() and mono.is_file() and engine.is_dir() and newtonsoft.is_file(),
             "Pinned Roslyn, Mono, and source-host runtime dependencies required")

        arguments = [json.loads(line) for line in response.read_text(encoding="utf-8").splitlines() if line.strip()]
        output_exe = output / "S050FocusedPrefabImporterControls.exe"
        rewritten = []
        out_count = main_count = 0
        for argument in arguments:
            if argument.startswith("-out:"):
                rewritten.append("-out:" + str(output_exe)); out_count += 1
            elif argument == "-main:SourceSimulationProgram":
                rewritten.append("-main:Game.Runtime.S050FocusedPrefabImporterControls"); main_count += 1
            else:
                rewritten.append(argument)
        need(out_count == 1 and main_count == 1, "Checked response must expose one output and source-host entry point")
        rewritten.append(str(CONTROL))
        control_response = output / "control.rsp"
        control_response.write_text("".join(json.dumps(arg) + "\n" for arg in rewritten), encoding="utf-8")
        compiled = subprocess.run([str(dotnet), str(compiler), "@" + str(control_response)], cwd=ROOT,
                                  text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=180)
        (output / "compile.log").write_text(compiled.stdout, encoding="utf-8")
        receipt["compile_exit_code"] = compiled.returncode
        receipt["compile_log_sha256"] = sha(output / "compile.log")
        need(compiled.returncode == 0 and output_exe.is_file(), "Isolated reader/importer control compile failed")

        descriptor = json.loads(descriptor_path.read_bytes())
        proof = descriptor.get("source_only_capture_proof", {})
        need(Path(proof.get("path", "")).is_file() and sha(proof["path"]) == proof.get("sha256"),
             "Projection proof descriptor bytes changed")
        ref_dirs = {str(build_dir / "placement"), str(engine), str(newtonsoft.parent), str(ROOT / "Library/ScriptAssemblies"), str(output)}
        for argument in arguments:
            if argument.startswith("-r:") and Path(argument[3:]).is_file():
                ref_dirs.add(str(Path(argument[3:]).resolve().parent))
        env = {**os.environ, "MONO_PATH": os.pathsep.join(sorted(ref_dirs))}
        executed = subprocess.run([str(mono), str(output_exe), str(descriptor_path), str(ROOT)], cwd=ROOT,
                                  env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=240)
        (output / "run.log").write_text(executed.stdout, encoding="utf-8")
        receipt["run_exit_code"] = executed.returncode
        receipt["run_log_sha256"] = sha(output / "run.log")
        need(executed.returncode == 0 and executed.stdout.splitlines().count(PASS) == 1,
             "Reader/importer controls failed or exact pass marker missing")
        verify_pins(pins)
        need(build_path.read_bytes() == build_raw, "Checked build receipt changed during controls")
        receipt.update({"status": "Passed", "source_build_sha256": hashlib.sha256(build_raw).hexdigest(),
                        "source_pin_count": len(pins), "reader_sha256": sha(VIEW), "importer_sha256": sha(IMPORTER),
                        "control_source_sha256": sha(CONTROL), "descriptor_sha256": sha(descriptor_path),
                        "source_pins_rechecked_after": True, "positive_reader_and_compiled_importer_preflight": True,
                        "negative_count": 7, "raw_capture_failure_preserved": True,
                        "world_allocated": False, "mission_ready": False})
        print(executed.stdout, end="")
    except Exception as exc:
        receipt["failure"] = str(exc)
        receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        raise
    receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(receipt, indent=2, sort_keys=True))
    print(PASS)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-dir", type=Path, required=True)
    parser.add_argument("--descriptor", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    run(args.build_dir, args.descriptor, args.output)


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Compile and run isolated S074 focused-prefab reader/importer controls against a passed source build."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess

CONTROL = Path("Tools/CI/SkirmishSourceSimulation/Tests/S074FocusedPrefabImporterControls.cs")
VIEW = Path("Tools/CI/SkirmishSourceSimulation/Adapters/419_GeneratedReferenceTemplateView.cs")
PASS = "[S074FocusedPrefabImporterControls] result=Passed positive=1 negative=5 worldAllocated=False missionReady=False"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(condition, message):
    if not condition:
        raise ValueError(message)


def verify_pins(pins):
    need(isinstance(pins, dict) and pins, "Passed S074 build source pin map required")
    drift = []
    for raw_path, expected in pins.items():
        path = Path(raw_path)
        if not path.is_file() or sha(path) != expected:
            drift.append(raw_path)
            if len(drift) == 8:
                break
    need(not drift, "Source build pin drift: " + ", ".join(drift))


def run(build_dir, descriptor_path, output):
    repo = Path(__file__).resolve().parents[4]
    build_dir = build_dir.resolve(strict=True)
    descriptor_path = descriptor_path.resolve(strict=True)
    output = output.resolve()
    need(build_dir.is_relative_to(repo) and descriptor_path.is_relative_to(repo) and output.is_relative_to(repo),
         "Build, descriptor, and fresh output must remain inside the repository")
    need(not output.exists(), "Fresh isolated control output directory required")
    output.mkdir(parents=True)
    receipt_path = output / "receipt.json"
    receipt = {"schema_version": 1, "kind": "S074FocusedPrefabImporterControlReceipt", "status": "Failed",
               "source_build": str(build_dir / "build.json"), "descriptor_path": str(descriptor_path),
               "output": str(output), "unity_launched": False, "game_source_modified": False}
    build_path = build_dir / "build.json"
    build_raw = build_path.read_bytes()
    build = json.loads(build_raw.decode("utf-8"))
    try:
        need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
             build.get("compile_exit") == 0 and build.get("mission") == "S074", "Passed S074 source-host build required")
        host_root = Path(build.get("repository_root", "")).resolve(strict=True)
        need(host_root == repo, "Passed build must bind the current host checkout")
        pins = build.get("source_and_reference_pins", {})
        verify_pins(pins)
        view_path = repo / VIEW
        need(pins.get(str(view_path)) == sha(view_path), "Build must contain the exact current S074 reader/importer proof branch")
        response_info = build.get("compiler_response", {})
        response = Path(response_info.get("path", "")).resolve(strict=True)
        executable_info = build.get("executable", {})
        source_exe = Path(executable_info.get("path", "")).resolve(strict=True)
        need(response.is_relative_to(build_dir) and source_exe.is_relative_to(build_dir) and
             sha(response) == response_info.get("sha256") and sha(source_exe) == executable_info.get("sha256"),
             "Exact passed source-build response and executable required")
        need(pins.get(str((repo / CONTROL).resolve())) is None, "Isolated control source must not be in production source closure")

        runtime = build.get("runtime_selection", {})
        compiler = Path(next(p for p in pins if p.endswith("/Roslyn/bincore/csc.dll"))).resolve(strict=True)
        dotnet = Path(next(p for p in pins if Path(p).name == "dotnet")).resolve(strict=True)
        mono = Path(runtime.get("mono", "")).resolve(strict=True)
        engine = Path(runtime.get("engine", "")).resolve(strict=True)
        newtonsoft = Path(runtime.get("newtonsoft", "")).resolve(strict=True)
        need(compiler.is_file() and dotnet.is_file() and mono.is_file() and engine.is_dir() and newtonsoft.is_file(),
             "Pinned source compiler and runtime dependencies required")

        lines = response.read_text(encoding="utf-8").splitlines()
        out_exe = output / "S074FocusedPrefabImporterControls.exe"
        rewritten, out_seen, main_seen = [], False, False
        for line in lines:
            normalized = line.strip().strip('"')
            if normalized.startswith("-out:"):
                rewritten.append('"-out:' + str(out_exe) + '"'); out_seen = True
            elif normalized == "-main:SourceSimulationProgram":
                rewritten.append('"-main:Game.Runtime.S074FocusedPrefabImporterControls"'); main_seen = True
            else:
                rewritten.append(line)
        need(out_seen and main_seen, "Build response must have one output and source host entry point")
        rewritten.append('"' + str(repo / CONTROL) + '"')
        response_path = output / "control.rsp"
        response_path.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
        compile_result = subprocess.run([str(dotnet), str(compiler), "@" + str(response_path)], cwd=repo,
                                        text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=180)
        compile_log = output / "compile.log"
        compile_log.write_text(compile_result.stdout, encoding="utf-8")
        receipt["compile_exit_code"] = compile_result.returncode
        receipt["compile_log_sha256"] = sha(compile_log)
        need(compile_result.returncode == 0 and out_exe.is_file(), "Isolated C# control compile failed; full compile.log retained")

        descriptor = json.loads(descriptor_path.read_text(encoding="utf-8"))
        proof = descriptor["s074_capture_proof"]
        need(sha(proof["path"]) == proof["sha256"], "Exact S074 source-only capture proof required")
        run_result = subprocess.run([str(mono), str(out_exe), str(descriptor_path), str(host_root)], cwd=repo,
                                    env={**os.environ, "MONO_PATH": os.pathsep.join((str(build_dir / "placement"), str(engine),
                                                                                      str(newtonsoft.parent), str(repo / "Library/ScriptAssemblies"),
                                                                                      str(output)))},
                                    text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=240)
        run_log = output / "run.log"
        run_log.write_text(run_result.stdout, encoding="utf-8")
        receipt["run_exit_code"] = run_result.returncode
        receipt["run_log_sha256"] = sha(run_log)
        need(run_result.returncode == 0 and run_result.stdout.splitlines().count(PASS) == 1,
             "C# controls failed or exact pass marker missing; full run.log retained")
        verify_pins(pins)
        need(build_path.read_bytes() == build_raw, "Passed build receipt changed during controls")
        receipt.update({"status": "Passed", "source_build_sha256": hashlib.sha256(build_raw).hexdigest(),
                        "source_build_executable_sha256": sha(source_exe), "source_build_response_sha256": sha(response),
                        "source_pin_count": len(pins), "control_source_sha256": sha(repo / CONTROL),
                        "descriptor_sha256": sha(descriptor_path), "source_pins_rechecked_after": True,
                        "positive_actual_reader_and_importer_preflight": True, "negative_count": 5,
                        "replay_ready": False, "mission_ready": False, "world_allocated": False})
        print(run_result.stdout, end="")
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

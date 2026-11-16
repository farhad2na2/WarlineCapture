#!/usr/bin/env python3
"""Compile and run the isolated current-S002 C# view controls against a passed source build."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path

CONTROL = Path("Tools/CI/SkirmishSourceSimulation/Tests/CurrentS002TemplateViewProofControls.cs")
PASS = "[CurrentS002TemplateViewProofControls] result=Passed positive=1 negative={count} missionReady=False"


def sha_bytes(data):
    return hashlib.sha256(data).hexdigest()


def sha(path):
    return sha_bytes(Path(path).read_bytes())


def need(condition, message):
    if not condition:
        raise ValueError(message)


def verify_pins(pins):
    need(isinstance(pins, dict) and pins, "Passed S002 build source pin map required")
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
    receipt = {
        "schema_version": 1,
        "kind": "CurrentS002TemplateViewProofControlReceipt",
        "status": "Failed",
        "source_build": str(build_dir / "build.json"),
        "descriptor_path": str(descriptor_path),
        "output": str(output),
        "unity_launched": False,
        "game_source_modified": False,
    }
    descriptor_path_hash = sha(descriptor_path)
    descriptor = json.loads(descriptor_path.read_text(encoding="utf-8"))
    export_path = Path(descriptor["current_capture_proof"]["export_capture_path"])
    refreshed = export_path.is_relative_to(repo)
    pass_marker = PASS.format(count=15 if refreshed else 13)
    compile_log = output / "compile.log"
    run_log = output / "run.log"
    response_path = output / "control.rsp"
    try:
        build_path = build_dir / "build.json"
        build_raw = build_path.read_bytes()
        build = json.loads(build_raw.decode("utf-8"))
        need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
             build.get("compile_exit") == 0 and build.get("mission") == "S002",
             "Passed S002 source-host build required")
        host_source_root = Path(build.get("repository_root", "")).resolve(strict=True)
        need(host_source_root == repo and host_source_root.is_dir(),
             "Passed build must bind the current host checkout as source repository root")
        pins = build.get("source_and_reference_pins", {})
        verify_pins(pins)
        response_info = build.get("compiler_response", {})
        response = Path(response_info.get("path", "")).resolve(strict=True)
        exe_info = build.get("executable", {})
        source_exe = Path(exe_info.get("path", "")).resolve(strict=True)
        need(response.is_relative_to(build_dir) and source_exe.is_relative_to(build_dir) and
             sha(response) == response_info.get("sha256") and sha(source_exe) == exe_info.get("sha256"),
             "Exact passed source-build response and executable required")
        need(pins.get(str(CONTROL.resolve())) is None,
             "Control must remain isolated and outside the already-passed production source closure")
        view_path = repo / "Tools/CI/SkirmishSourceSimulation/Adapters/419_GeneratedReferenceTemplateView.cs"
        need(pins.get(str(view_path)) == sha(view_path),
             "Passed source build must contain the exact current template-view implementation")

        runtime = build.get("runtime_selection", {})
        compiler = Path(next(p for p in pins if p.endswith("/Roslyn/bincore/csc.dll"))).resolve(strict=True)
        dotnet = Path(next(p for p in pins if Path(p).name == "dotnet")).resolve(strict=True)
        mono = Path(runtime.get("mono", "")).resolve(strict=True)
        engine = Path(runtime.get("engine", "")).resolve(strict=True)
        newtonsoft = Path(runtime.get("newtonsoft", "")).resolve(strict=True)
        need(compiler.is_file() and dotnet.is_file() and mono.is_file() and engine.is_dir() and newtonsoft.is_file(),
             "Pinned S002 compiler and runtime dependencies required")

        source_lines = response.read_text(encoding="utf-8").splitlines()
        out_exe = output / "CurrentS002TemplateViewProofControls.exe"
        rewritten, out_seen, main_seen = [], False, False
        for line in source_lines:
            normalized = line.strip().strip('"')
            if normalized.startswith("-out:"):
                rewritten.append('"-out:' + str(out_exe) + '"')
                out_seen = True
            elif normalized == "-main:SourceSimulationProgram":
                rewritten.append('"-main:Game.Runtime.CurrentS002TemplateViewProofControls"')
                main_seen = True
            else:
                rewritten.append(line)
        need(out_seen and main_seen, "S002 response must have one output and the source host entry point")
        rewritten.append('"' + str(repo / CONTROL) + '"')
        response_path.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
        compiler_run = subprocess.run([str(dotnet), str(compiler), "@" + str(response_path)], cwd=repo,
                                      text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=180)
        compile_log.write_text(compiler_run.stdout, encoding="utf-8")
        receipt["compile_exit_code"] = compiler_run.returncode
        receipt["compile_log_sha256"] = sha(compile_log)
        need(compiler_run.returncode == 0 and out_exe.is_file(),
             "Isolated C# control compile failed; full compile.log retained")

        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join((str(build_dir / "placement"), str(engine), str(newtonsoft.parent),
                                             str(repo / "Library/ScriptAssemblies"), str(output)))
        execution = subprocess.run([str(mono), str(out_exe), str(descriptor_path), str(host_source_root)], cwd=repo, env=env,
                                   text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=180)
        run_log.write_text(execution.stdout, encoding="utf-8")
        receipt["run_exit_code"] = execution.returncode
        receipt["run_log_sha256"] = sha(run_log)
        need(execution.returncode == 0 and execution.stdout.splitlines().count(pass_marker) == 1,
             "C# proof controls failed or exact pass marker missing; full run.log retained")
        verify_pins(pins)
        need(build_path.read_bytes() == build_raw and sha(descriptor_path) == descriptor_path_hash,
             "Build receipt or descriptor changed during controls")
        receipt.update({
            "status": "Passed",
            "source_build_sha256": sha_bytes(build_raw),
            "source_build_executable_sha256": sha(source_exe),
            "source_build_response_sha256": sha(response),
            "source_pin_count": len(pins),
            "control_source_sha256": sha(repo / CONTROL),
            "host_source_repository_root": str(host_source_root),
            "descriptor_sha256": descriptor_path_hash,
            "source_pins_rechecked_after": True,
            "positive_actual_view_read": True,
            "negative_controls": ["wrong_s003_proof", "readiness_promotion", "changed_source_pin", "changed_external_key_pin", "wrong_root", "parent_symlink", "inside_root_key", "historical_export_locator_inside_root", "historical_export_locator_relative", "historical_export_locator_noncanonical", "relative_pin_parent_traversal", "additional_absolute_external_pin", "external_capture"] + (["changed_export_hash", "wrong_durable_capture"] if refreshed else []),
            "mission_ready": False,
            "replay_ready": False,
            "native_visual_readiness": False,
        })
    except Exception as exception:
        receipt["failure"] = str(exception)
        if compile_log.exists():
            receipt["compile_log_sha256"] = sha(compile_log)
        if run_log.exists():
            receipt["run_log_sha256"] = sha(run_log)
        receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        raise
    receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(receipt, indent=2, sort_keys=True))
    print(pass_marker)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-dir", type=Path, required=True)
    parser.add_argument("--descriptor", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    run(args.build_dir, args.descriptor, args.output)


if __name__ == "__main__":
    main()

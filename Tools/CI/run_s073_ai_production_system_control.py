#!/usr/bin/env python3
"""Compile and run the bounded synthetic original AI production owner control."""
from __future__ import annotations
import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "Tools/CI/SkirmishSourceSimulation/source_manifest.s073-ai-experimental.plan.json"
CONTROL_TYPE = "Game.Runtime.SourceS073AIProductionSystemControl"
PASS = "[S073AIProductionSystemControl] result=Passed"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(ok: bool, message: str) -> None:
    if not ok:
        raise RuntimeError(message)


def verify(pins: dict[str, str]) -> None:
    for name, expected in pins.items():
        path = Path(name)
        need(path.is_file() and digest(path) == expected, "Pinned build input changed: " + name)


def run(command: list[str], log_path: Path, timeout: int = 1800) -> int:
    with log_path.open("w") as log:
        proc = subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, timeout=timeout)
    return proc.returncode


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, help="Fresh retained evidence directory")
    args = parser.parse_args()
    out = Path(args.output).absolute()
    need(not out.exists(), "Fresh output directory required")
    out.mkdir(parents=True)
    script_path = Path(__file__).resolve()
    script_sha = digest(script_path)
    build_dir = out / "build"

    build_command = ["rtk", "proxy", "python3", "-B", "Tools/CI/run_skirmish_source_simulation.py",
                     "build", "--repo", str(ROOT), "--package-root", str(ROOT), "--manifest", str(MANIFEST),
                     "--mission", "S073", "--seed", "104729", "--output", str(build_dir)]
    build_exit = run(build_command, out / "build-command.log")
    build_path = build_dir / "build.json"
    if build_exit != 0 or not build_path.is_file():
        print("[S073AIProductionSystemControlRunner] result=BuildFailed evidence=" + str(out), file=sys.stderr)
        return 1

    receipt = json.loads(build_path.read_text())
    need(receipt.get("status") == "Passed" and receipt.get("mission") == "S073" and
         receipt.get("seed") == 104729 and receipt.get("native_or_player_launched") is False,
         "Exact opt-in S073 source build receipt required")
    pins = receipt["source_and_reference_pins"]
    verify(pins)
    response_path = Path(receipt["compiler_response"]["path"])
    response_args = [json.loads(line) for line in response_path.read_text().splitlines() if line]
    main_args = [x for x in response_args if x.startswith("-main:")]
    out_args = [x for x in response_args if x.startswith("-out:")]
    need(main_args == ["-main:SourceSimulationProgram"] and len(out_args) == 1,
         "Exact source compiler response entrypoint required")
    control_exe = out / "S073AIProductionSystemControl.exe"
    control_args = ["-main:" + CONTROL_TYPE if x.startswith("-main:") else
                    "-out:" + str(control_exe) if x.startswith("-out:") else x for x in response_args]
    control_rsp = out / "control-compile.rsp"
    control_rsp.write_text("\n".join(json.dumps(x) for x in control_args) + "\n")

    sdk_output = subprocess.check_output(["rtk", "proxy", "dotnet", "--list-sdks"], cwd=ROOT, text=True)
    sdks = []
    for line in sdk_output.splitlines():
        match = re.fullmatch(r"(\S+) \[(.+)\]", line)
        if match:
            sdks.append((tuple(int(x) for x in match.group(1).split(".")), Path(match.group(2)) / match.group(1)))
    need(bool(sdks), "Installed .NET SDK required")
    csc = max(sdks, key=lambda row: row[0])[1] / "Roslyn/bincore/csc.dll"
    need(csc.is_file(), "Installed Roslyn csc required")
    compile_exit = run(["rtk", "proxy", "dotnet", str(csc), "@" + str(control_rsp)], out / "control-compile.log", 180)
    if compile_exit != 0:
        print("[S073AIProductionSystemControlRunner] result=ControlCompileFailed evidence=" + str(out), file=sys.stderr)
        return 1

    runtime = {key: Path(value) for key, value in receipt["runtime_selection"].items()}
    unity_version = re.search(r"m_EditorVersion:\s*(\S+)", (ROOT / "ProjectSettings/ProjectVersion.txt").read_text()).group(1)
    contents = Path("/Applications/Unity/Hub/Editor") / unity_version / "Unity.app/Contents"
    mono = contents / "Resources/Scripting/MonoBleedingEdge/bin/mono"
    config_source = contents / "Resources/Scripting/MonoBleedingEdge/etc/mono/config"
    helper = contents / "Frameworks/libMonoPosixHelper.dylib"
    need(mono.is_file() and config_source.is_file() and helper.is_file(), "Qualified installed Mono runtime required")
    config_text = config_source.read_text()
    config_text = config_text.replace('target="libMonoPosixHelper.dylib"', 'target="' + str(helper) + '"')
    config_text = config_text.replace('target="MonoPosixHelper"', 'target="' + str(helper) + '"')
    config = out / "mono-config.xml"
    config.write_text(config_text)
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([str(ROOT / "Library/ScriptAssemblies"), str(runtime["engine"]),
                                         str(runtime["newtonsoft"].parent), str(Path(receipt["placement_build"]).parent)])
    verify(pins)
    with (out / "control-run.log").open("w") as log:
        proc = subprocess.run(["rtk", "proxy", str(mono), "--config", str(config), "--optimize=-float32",
                              str(control_exe)], cwd=ROOT, env=env, stdout=log, stderr=subprocess.STDOUT, timeout=120)
    output = (out / "control-run.log").read_text(errors="replace")
    passed = proc.returncode == 0 and PASS in output
    verify(pins)
    need(digest(script_path) == script_sha, "Control runner source changed during execution")
    evidence = {
        "schema_version": 1,
        "kind": "S073AIProductionSystemSyntheticOwnerControl",
        "status": "Passed" if passed else "Failed",
        "build_receipt": str(build_path),
        "build_receipt_sha256": digest(build_path),
        "build_source_count": receipt["source_count"],
        "control_response": str(control_rsp),
        "control_response_sha256": digest(control_rsp),
        "control_executable": str(control_exe),
        "control_executable_sha256": digest(control_exe),
        "control_compile_exit": compile_exit,
        "control_run_exit": proc.returncode,
        "pass_marker_observed": PASS in output,
        "source_inputs_stable": True,
        "source_host_bound": False,
        "source_world_modified": False,
        "production_owner_invocations": 2,
        "native_or_player_launched": False,
        "mission_readiness_claim": False,
        "scope": "Original AIProductionSystem OnCreate/OnUpdate on two independent explicitly synthetic managers; one affordable configured decision naturally enqueues one request and spends its configured resources, and one insufficient-funds decision emits no request and preserves resources.",
    }
    (out / "result.json").write_text(json.dumps(evidence, indent=2) + "\n")
    print(json.dumps({"status": evidence["status"], "evidence": str(out),
                      "source_count": receipt["source_count"], "owner_invocations": 2}))
    return 0 if passed else 1


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print("[S073AIProductionSystemControlRunner] result=Failed error=" + str(error), file=sys.stderr)
        raise SystemExit(1)

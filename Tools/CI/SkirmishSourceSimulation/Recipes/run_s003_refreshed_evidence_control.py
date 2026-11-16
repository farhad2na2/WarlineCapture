#!/usr/bin/env python3
"""Compile and execute the refreshed S003 evidence owner control."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S003RefreshedEvidenceControl.cs"
EVIDENCE = ROOT / "T Logs/SkirmishProduction/s003-refreshed-qualification-evidence-20261104-T5/source-only-qualification-evidence.json"
MARKER = "[S003RefreshedEvidenceControl] result=Passed positive=7 negative=8 world=0 readiness=0"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--evidence", type=Path, default=EVIDENCE)
    args = parser.parse_args()
    evidence = args.evidence.resolve(strict=True)
    evidence_sha = digest(evidence)
    output = Path(args.output).absolute()
    output.mkdir(parents=True, exist_ok=False)
    build_path = Path(args.build).resolve(strict=True)
    build = json.loads(build_path.read_bytes())
    if build.get("status") != "Passed" or build.get("mission") != "S003" or build.get("compile_exit") != 0:
        raise ValueError("Passed S003 source build required")
    pins = build["source_and_reference_pins"]
    for name, expected in pins.items():
        if digest(Path(name)) != expected:
            raise ValueError("Build source pin changed: " + name)
    csc = next(Path(name) for name in pins if name.endswith("/Roslyn/bincore/csc.dll"))
    dotnet = shutil.which("dotnet")
    if not dotnet or not any(Path(name).name == "dotnet" and Path(name).resolve() == Path(dotnet).resolve() for name in pins):
        raise ValueError("Pinned compiler host required")
    response = Path(build["compiler_response"]["path"])
    if digest(response) != build["compiler_response"]["sha256"]:
        raise ValueError("Compiler response changed")
    executable = output / "S003RefreshedEvidenceControl.exe"
    lines = []
    for line in response.read_text().splitlines():
        unquoted = line.strip().strip('"')
        if unquoted.startswith("-main:"):
            line = '"-main:S003RefreshedEvidenceControl"'
        elif unquoted.startswith("-out:"):
            line = '"-out:' + str(executable) + '"'
        lines.append(line)
    lines.append('"' + str(HARNESS) + '"')
    rsp = output / "compile.rsp"
    rsp.write_text("\n".join(lines) + "\n")
    compiled = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True, text=True, timeout=300)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode != 0:
        raise RuntimeError("Control compilation failed")
    runtime = build["runtime_selection"]
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(build_path.parent / "placement"), runtime["engine"],
                                       str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(output)))
    run = subprocess.run([runtime["mono"], str(executable), str(evidence)], cwd=ROOT, env=env,
                         capture_output=True, text=True, timeout=300)
    (output / "run.log").write_text(run.stdout + run.stderr)
    if run.returncode != 0 or MARKER not in run.stdout:
        raise RuntimeError("Control failed or marker missing")
    if digest(evidence) != evidence_sha:
        raise ValueError("Evidence changed during source control")
    receipt = {"status": "PassedSourceOnlyControl", "build_sha256": digest(build_path),
               "evidence_path": str(evidence), "evidence_sha256": evidence_sha, "harness_sha256": digest(HARNESS),
               "source_pins_verified": len(pins), "positive": 7, "negative": 8,
               "world_allocated": False, "runtime_admission": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()

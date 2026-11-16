#!/usr/bin/env python3
"""Compare current and indexed child projections on pinned Mono."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys


def sha(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def need(value, message):
    if not value:
        raise ValueError(message)


def main():
    need(len(sys.argv) == 3, "build receipt and fresh output directory required")
    build_path = Path(sys.argv[1]).resolve(strict=True)
    output = Path(sys.argv[2]).absolute(); output.mkdir(parents=True, exist_ok=False)
    build = json.loads(build_path.read_bytes())
    need(build.get("status") == "Passed" and build.get("mission") == "S003", "Passing S003 source build required")
    pins = build["source_and_reference_pins"]
    def verify():
        for path, digest in pins.items():
            need(sha(path) == digest, "Source build pin changed: " + path)
    verify()
    root = Path(build["repository_root"]).resolve(strict=True)
    source = root / "Tools/CI/SkirmishSourceSimulation/Tests/S003TransformChildrenIndexControl.cs"
    source_sha = sha(source)
    response = Path(build["compiler_response"]["path"])
    need(sha(response) == build["compiler_response"]["sha256"], "Compiler response changed")
    args = [json.loads(line) for line in response.read_text().splitlines() if line.strip()]
    mains = [i for i, value in enumerate(args) if value == "-main:SourceSimulationProgram"]
    outputs = [i for i, value in enumerate(args) if value.startswith("-out:")]
    need(len(mains) == len(outputs) == 1, "Unique source entry and output required")
    exe = output / "S003TransformChildrenIndexControl.exe"
    args[mains[0]] = "-main:S003TransformChildrenIndexControl"
    args[outputs[0]] = "-out:" + str(exe)
    args.append(str(source)); rsp = output / "compile.rsp"
    rsp.write_text("\n".join(json.dumps(value) for value in args) + "\n")
    compiler = next(Path(name) for name in pins if name.endswith("/Roslyn/bincore/csc.dll"))
    dotnet = shutil.which("dotnet")
    need(dotnet and any(Path(name).name == "dotnet" and Path(name).resolve() == Path(dotnet).resolve() for name in pins),
         "Pinned compiler host required")
    compiled = subprocess.run([dotnet, str(compiler), "@" + str(rsp)], capture_output=True, text=True, timeout=300)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    need(compiled.returncode == 0, "Source control compilation failed")
    runtime = build["runtime_selection"]; env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(build_path.parent / "placement"), runtime["engine"],
                                         str(Path(runtime["newtonsoft"]).parent), str(root / "Library/ScriptAssemblies"), str(output)))
    run = subprocess.run([runtime["mono"], "--optimize=-float32", str(exe)], cwd=root, env=env,
                         capture_output=True, text=True, timeout=300)
    (output / "run.log").write_text(run.stdout + run.stderr)
    marker = "[S003TransformChildrenIndexControl] result=Passed random=200 duplicate=Passed paired=12 sourceOnly=1"
    need(run.returncode == 0 and marker in run.stdout, "Source control or marker failed")
    verify(); need(sha(source) == source_sha, "Control source changed during execution")
    rows = dict(line.split("=", 1) for line in run.stdout.splitlines() if line.startswith(("original_ms=", "candidate_ms=")))
    times = {key: [float(x) for x in value.split(",")] for key, value in rows.items()}
    need(set(times) == {"original_ms", "candidate_ms"} and all(len(x) == 12 for x in times.values()), "Twelve measured pairs required")
    result = {"kind": "S003TransformChildrenIndexControl", "status": "PassedSourceOnlyFixture",
              "build_receipt_sha256": sha(build_path), "source_sha256": source_sha,
              "source_pins_verified": len(pins), "compile_exit": compiled.returncode, "run_exit": run.returncode,
              "times_ms": times, "gameplay_run": False, "native_parity_proven": False,
              "optimization_promoted": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result))


if __name__ == "__main__":
    main()

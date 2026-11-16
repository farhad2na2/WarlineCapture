#!/usr/bin/env python3
"""Compile and run focused behavioral cases against the actual declared-layout gate."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

RECIPE = Path(__file__).resolve().parent
HARNESS = RECIPE / "SourceDeclaredLayoutGateHarness.cs"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--build", type=Path, required=True, help="passed managed source-host build receipt")
    ap.add_argument("--graph", type=Path, required=True)
    ap.add_argument("--layout", type=Path, required=True)
    ap.add_argument("--projection", type=Path, required=True, help="current alias compiler owner projection")
    ap.add_argument("--direct-graph", type=Path, required=True, help="current S050 direct-source authored graph")
    ap.add_argument("--direct-layout", type=Path, required=True, help="S050 layout descriptor from its authored request")
    ap.add_argument("--direct-projection", type=Path, required=True, help="current S050 compiler owner projection")
    ap.add_argument("--output", type=Path, required=True, help="new output directory")
    opts = ap.parse_args()
    out = opts.output.absolute()
    out.mkdir(parents=True, exist_ok=False)
    build_path, graph_path, layout_path, projection_path, direct_graph_path, direct_layout_path, direct_projection_path = (
        p.resolve(strict=True) for p in (opts.build, opts.graph, opts.layout, opts.projection,
                                         opts.direct_graph, opts.direct_layout, opts.direct_projection))
    build = json.loads(build_path.read_text())
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("compile_exit") == 0, "Passed current source-host build required")
    pins = build.get("source_and_reference_pins")
    need(isinstance(pins, dict) and pins, "Build source/reference pins required")
    for path, digest in pins.items():
        need(Path(path).is_file() and sha(Path(path)) == digest, "Build input changed: " + path)
    response_info = build["compiler_response"]
    response = Path(response_info["path"]).resolve(strict=True)
    need(sha(response) == response_info["sha256"], "Compiler response pin mismatch")
    projection = json.loads(projection_path.read_text())
    need(projection.get("kind") == "SkirmishMissionAuthoringOwnerProjection" and
         projection.get("status") == "Partial_CompiledOwnerInputsOnly" and
         projection.get("target", {}).get("mission_id") == "S026" and
         projection.get("target", {}).get("operation_map_id") == "opmap.skirmish.city_crossroads",
         "Current S026 alias compiler owner projection required")
    need(projection["owner_descriptors"]["source_config_graph"]["sha256"] == sha(graph_path) and
         projection["owner_descriptors"]["layout"]["sha256"] == sha(layout_path),
         "Projection does not pin the supplied graph and layout")
    direct_projection = json.loads(direct_projection_path.read_text())
    need(direct_projection.get("kind") == "SkirmishMissionAuthoringOwnerProjection" and
         direct_projection.get("status") == "Partial_CompiledOwnerInputsOnly" and
         direct_projection.get("target", {}).get("mission_id") == "S050" and
         direct_projection.get("target", {}).get("source_operation_map_id") ==
         direct_projection.get("target", {}).get("operation_map_id") and
         direct_projection["owner_descriptors"]["source_config_graph"]["sha256"] == sha(direct_graph_path) and
         direct_projection["owner_descriptors"]["layout"]["sha256"] == sha(direct_layout_path),
         "Current pinned S050 direct-source graph/layout projection required")

    compiler_args = [json.loads(line) for line in response.read_text().splitlines() if line.strip()]
    need(any(a.startswith("-main:") for a in compiler_args) and any(a.startswith("-out:") for a in compiler_args),
         "Malformed compiler response")
    for i, arg in enumerate(compiler_args):
        if arg.startswith("-main:"):
            compiler_args[i] = "-main:SourceDeclaredLayoutGateHarness"
        elif arg.startswith("-out:"):
            compiler_args[i] = "-out:" + str(out / "SourceDeclaredLayoutGateHarness.exe")
    compiler_args.append(str(HARNESS))
    rsp = out / "compile.rsp"
    rsp.write_text("\n".join(json.dumps(a) for a in compiler_args) + "\n")
    dotnet = Path(next(path for path in pins if path.endswith("/dotnet")))
    csc_candidates = sorted(path for path in pins if re.search(r"/sdk/[0-9.]+/Roslyn/bincore/csc\.dll$", path))
    need(csc_candidates, "Pinned Roslyn compiler missing")
    compiled = subprocess.run([str(dotnet), csc_candidates[-1], "@" + str(rsp)], capture_output=True, text=True, timeout=300)
    (out / "compile.log").write_text(compiled.stdout + compiled.stderr)
    need(compiled.returncode == 0, "Focused gate harness compilation failed; see compile.log")

    runtime = build["runtime_selection"]
    refs = sorted({str(Path(arg[3:]).resolve().parent) for arg in compiler_args
                   if arg.startswith("-r:") and Path(arg[3:]).is_file()})
    mono_path = ":".join(refs + [str(Path(runtime["newtonsoft"]).resolve().parent)])
    import os
    env = os.environ.copy()
    env["MONO_PATH"] = mono_path
    run = subprocess.run([runtime["mono"], str(out / "SourceDeclaredLayoutGateHarness.exe"),
                          str(graph_path), str(layout_path), str(projection_path),
                          str(direct_graph_path), str(direct_layout_path), str(direct_projection_path)],
                        capture_output=True, text=True, timeout=120, env=env)
    (out / "run.log").write_text(run.stdout + run.stderr)
    need(run.returncode == 0 and '"status":"Passed"' in run.stdout,
         "SourceDeclaredMissionConfiguration gate harness failed; see run.log")
    result = json.loads(run.stdout.strip().splitlines()[-1])
    need(result.get("source_fit_certified") is False and result.get("mission_ready") is False,
         "Harness scope must remain source-only and non-ready")
    receipt = {"kind": "SourceDeclaredLayoutGateHarnessReceipt", "status": "Passed",
               "result": result,
               "build": {"path": str(build_path), "sha256": sha(build_path)},
               "source_graph": {"path": str(graph_path), "sha256": sha(graph_path)},
               "layout": {"path": str(layout_path), "sha256": sha(layout_path)},
               "alias_projection": {"path": str(projection_path), "sha256": sha(projection_path)},
               "direct_source_graph": {"path": str(direct_graph_path), "sha256": sha(direct_graph_path)},
               "direct_source_layout": {"path": str(direct_layout_path), "sha256": sha(direct_layout_path)},
               "direct_source_projection": {"path": str(direct_projection_path), "sha256": sha(direct_projection_path)},
               "compiler_response": {"path": str(response), "sha256": response_info["sha256"]},
               "executable": {"path": str(out / "SourceDeclaredLayoutGateHarness.exe"),
                              "sha256": sha(out / "SourceDeclaredLayoutGateHarness.exe")},
               "compile_log": str(out / "compile.log"), "run_log": str(out / "run.log"),
               "world_created": False, "placement_performed": False,
               "source_fit_certified": False, "mission_ready": False}
    (out / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt, separators=(",", ":")))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env python3
"""Build and run a source-only S004 compiler harness from a pinned source graph.

Requires a fresh, successful S002 source build response as its source host. It
does not invoke Unity, alter the shared manifest, initialize a world, or claim
physical source fit or mission readiness.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

HERE = Path(__file__).resolve().parent
BINDING = HERE / "S004OriginalCompilerSourceBinding.cs.txt"


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def verify_build_pins(build):
    pins = build.get("source_and_reference_pins")
    require(isinstance(pins, dict) and pins, "Complete source-build reference pins required")
    for path, sha in pins.items():
        require(Path(path).is_file() and digest(path) == sha, "S002 source-host pin changed or missing: " + path)


def version_key(path):
    match = re.search(r"/sdk/([0-9]+(?:\.[0-9]+)*)/Roslyn/bincore/csc\.dll$", path)
    return tuple(int(x) for x in match.group(1).split(".")) if match else ()


def run(build_receipt, graph_path, graph_sha, output, seed):
    build_path = Path(build_receipt).resolve()
    graph_path = Path(graph_path).resolve()
    out = Path(output).resolve()
    require(not out.exists(), "Fresh isolated output directory required")
    build = json.loads(build_path.read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("mission") == "S002" and
            build.get("status") == "Passed" and build.get("compile_exit") == 0 and build.get("source_count", 0) >= 700,
            "Successful S002 source build receipt required")
    verify_build_pins(build)
    graph = json.loads(graph_path.read_text())
    require(digest(graph_path) == graph_sha, "Explicit source graph hash mismatch")
    require(graph.get("authority") == "AuthoredConfigAssets" and graph.get("compiled_setup") is False and
            graph.get("world_initialized") is False and graph.get("native_world_state_used") is False and
            graph.get("definition_path") == "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishScenario_S004.asset" and
            graph.get("catalog_path") == "Assets/Game/Resources/SkirmishExpansionCatalog.asset" and
            isinstance(graph.get("setup_matrix"), dict), "Externally supplied pinned S004 graph required")
    for path, sha in graph.get("source_pins", {}).items():
        require(Path(path).is_file() and digest(path) == sha, "Pinned S004 source drift: " + path)
    rsp_desc = build.get("compiler_response")
    require(isinstance(rsp_desc, dict), "S002 compiler response descriptor missing")
    rsp = Path(rsp_desc["path"]).resolve()
    require(digest(rsp) == rsp_desc["sha256"], "S002 compiler response changed")
    args = [json.loads(line) for line in rsp.read_text().splitlines() if line.strip()]
    require(any(x.startswith("-main:") for x in args) and any(x.startswith("-out:") for x in args),
            "Expected compiler response structure missing")
    runtime = build["runtime_selection"]
    mono = Path(runtime["mono"])
    require(mono.is_file(), "Pinned source-host Mono runtime missing")
    source_pins = build["source_and_reference_pins"]
    dotnet_path = next((p for p in source_pins if p.endswith("/dotnet")), None)
    csc_paths = sorted((p for p in source_pins if re.search(r"/sdk/[0-9.]+/Roslyn/bincore/csc\.dll$", p)), key=version_key)
    require(dotnet_path and csc_paths, "Pinned source-build Roslyn toolchain missing")
    dotnet, csc = Path(dotnet_path), Path(csc_paths[-1])
    require(dotnet.is_file() and csc.is_file(), "Pinned Roslyn toolchain unavailable")
    out.mkdir(parents=True)
    newtonsoft = Path(runtime["newtonsoft"])
    require(newtonsoft.is_file(), "Pinned Newtonsoft runtime dependency missing")
    shutil.copy2(newtonsoft, out / "Newtonsoft.Json.dll")
    matrix = Path(graph["setup_matrix"]["path"]).resolve()
    require(matrix.is_file() and digest(matrix) == graph["setup_matrix"]["sha256"], "Pinned original setup matrix missing or changed")
    generated_rsp = list(args)
    for i, item in enumerate(generated_rsp):
        if item.startswith("-main:"): generated_rsp[i] = "-main:S004OriginalCompilerSourceBinding"
        if item.startswith("-out:"): generated_rsp[i] = "-out:" + str(out / "S004OriginalCompiler.exe")
    generated_rsp.append(str(BINDING.resolve()))
    response_path = out / "compile.rsp"
    response_path.write_text("\n".join(json.dumps(x) for x in generated_rsp) + "\n")
    compile_result = subprocess.run([str(dotnet), str(csc), "@" + str(response_path)], capture_output=True,
                                    text=True, timeout=120)
    (out / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    verify_build_pins(build)
    require(compile_result.returncode == 0, "S004 source harness compile failed; see " + str(out / "compile.log"))
    runtime_dirs = sorted({str(Path(item[3:]).resolve().parent) for item in generated_rsp if item.startswith("-r:") and Path(item[3:]).is_file()})
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join(runtime_dirs)
    invocation = subprocess.run([str(mono), str(out / "S004OriginalCompiler.exe"), str(graph_path), str(matrix), str(seed)],
                                capture_output=True, text=True, timeout=60, env=env)
    (out / "run.log").write_text(invocation.stdout + invocation.stderr)
    require(digest(graph_path) == graph_sha, "Source graph changed during compiler invocation")
    for path, sha in graph.get("source_pins", {}).items():
        require(Path(path).is_file() and digest(path) == sha, "Pinned S004 source changed during compiler invocation: " + path)
    verify_build_pins(build)
    require(invocation.returncode == 0 and '"kind":"S004OriginalCompilerSourceOnlyResult"' in invocation.stdout,
            "Original S004 compiler invocation failed; see " + str(out / "run.log"))
    result = json.loads(invocation.stdout.strip().splitlines()[-1])
    receipt = {"kind": "S004OriginalCompilerHarnessReceipt", "status": result["status"],
              "source_build": {"path": str(build_path), "sha256": digest(build_path)},
              "source_response": {"path": str(rsp), "sha256": digest(rsp)},
              "source_graph": {"path": str(graph_path), "sha256": graph_sha},
              "source_graph_source_pins": graph["source_pins"],
              "harness_source": {"path": str(BINDING), "sha256": digest(BINDING)},
              "runner_source": {"path": str(Path(__file__).resolve()), "sha256": digest(Path(__file__))},
              "compile_log_sha256": digest(out / "compile.log"), "run_log_sha256": digest(out / "run.log"),
              "runtime_search_paths": runtime_dirs,
              "matrix": {"path": str(matrix), "sha256": digest(matrix)}, "seed": seed,
              "result": result, "world_initialized": False, "physical_source_fit_verified": False,
              "mission_readiness": False}
    (out / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps({"status": receipt["status"], "setups": len(result["setups"]), "world_initialized": False,
                      "source_fit_verified": False, "mission_readiness": False}))
    return 0 if result["compiled"] else 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-receipt", required=True)
    parser.add_argument("--graph", required=True)
    parser.add_argument("--graph-sha256", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--seed", type=int, required=True)
    args = parser.parse_args()
    try:
        return run(args.build_receipt, args.graph, args.graph_sha256, args.output, args.seed)
    except (ValueError, OSError, KeyError, subprocess.SubprocessError, json.JSONDecodeError) as error:
        print("S004 source-only compiler harness failed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

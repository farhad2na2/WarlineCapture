#!/usr/bin/env python3
"""Export the original S004 Standard compiled setup from pinned authored sources.

This invokes the existing source-only compiler harness and a separate adapter.
It does not start Unity, initialize a world, or infer runtime production actions.
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
ORIGINAL_RUNNER = HERE / "run_s004_original_compiler.py"
ORIGINAL_BINDING = HERE / "S004OriginalCompilerSourceBinding.cs.txt"
ADAPTER = HERE / "S004StandardSetupExportSourceBinding.cs.txt"
JSON_ADAPTER = HERE / "S004StandardSetupJsonUtilityAdapter.cs.txt"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(ok, message):
    if not ok:
        raise ValueError(message)


def verify_pins(pins, label):
    require(isinstance(pins, dict) and pins, label + " pin inventory required")
    for path, expected in pins.items():
        require(Path(path).is_file() and sha(path) == expected, label + " pin changed or missing: " + path)


def run(build_receipt, graph_path, graph_sha, replay_receipt, native_receipt, output, seed):
    out = Path(output).resolve()
    require(not out.exists(), "Fresh isolated output directory required")
    build_path, graph_path = Path(build_receipt).resolve(), Path(graph_path).resolve()
    replay_path, native_path = Path(replay_receipt).resolve(), Path(native_receipt).resolve()
    require(sha(graph_path) == graph_sha, "Explicit current graph SHA-256 mismatch")
    graph = json.loads(graph_path.read_text())
    replay, native = json.loads(replay_path.read_text()), json.loads(native_path.read_text())
    build = json.loads(build_path.read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("mission") == "S002" and
            build.get("status") == "Passed" and build.get("compile_exit") == 0,
            "Passed S002 source-host build required")
    verify_pins(build.get("source_and_reference_pins"), "Source-host")
    verify_pins(graph.get("source_pins"), "S004 graph")
    require(replay.get("kind") == "S004AuthoredLayoutCaptureReplayReceipt" and replay.get("status") == "Passed" and
            replay.get("world_initialized") is False and replay.get("source_graph", {}).get("sha256") == graph_sha,
            "Passed source-bound capture replay for this exact graph required")
    require(native.get("schema") == "s004-native-json-capture-receipt-v1" and
            native.get("gameplay") == "NotRun" and native.get("world") == "NotCreated",
            "Current native capture receipt with explicit gameplay/world limits required")
    require(replay.get("native_receipt", {}).get("sha256") == sha(native_path),
            "Replay receipt is not bound to the supplied native capture receipt")
    replay_raw_path = replay_path.parent / "replayed-layout.json"
    require(replay_raw_path.is_file() and sha(replay_raw_path) == replay.get("replayed_raw_json_sha256") and
            replay.get("result", {}).get("rawJsonSha256") == replay.get("replayed_raw_json_sha256") and
            replay.get("result", {}).get("authoredLayoutTypedValuesMatched") is True and
            replay.get("result", {}).get("fieldSchemaAndOrderMatched") is True and
            replay.get("result", {}).get("float32BitwiseCompared") is True,
            "Shared hardened replay output must be present, pinned, and typed-value validated")
    replay_standard = next((x for x in replay["result"]["compiler"] if x.get("size") == "Standard"), None)
    native_standard = next((x for x in native["compilerRows"] if x.get("size") == "Standard"), None)
    require(replay_standard and replay_standard.get("matched") is True and
            native_standard and native_standard.get("outcome") == "accepted" and native_standard.get("measuredLayoutBound") is True and
            replay_standard.get("nativeSetupHash") == str(native_standard.get("setupHash")),
            "Independent native and source replay Standard hashes must agree")

    out.mkdir(parents=True)
    original_dir = out / "original-compiler"
    proc = subprocess.run([sys.executable, str(ORIGINAL_RUNNER), "--build-receipt", str(build_path),
                           "--graph", str(graph_path), "--graph-sha256", graph_sha,
                           "--output", str(original_dir), "--seed", str(seed)],
                          capture_output=True, text=True, timeout=180)
    (out / "original-runner.log").write_text(proc.stdout + proc.stderr)
    require(proc.returncode in (0, 1) and (original_dir / "receipt.json").is_file(),
            "Existing original compiler harness failed before preserving its result; inspect original-runner.log")
    original_harness = json.loads((original_dir / "receipt.json").read_text())
    original_result = original_harness["result"]
    require(original_result.get("kind") == "S004OriginalCompilerSourceOnlyResult" and
            original_result.get("world_initialized") is False, "Original compiler result missing or invalid")
    standard = next((x for x in original_result["s004_rows"] if x.get("size_id") == "Standard"), None)
    require(standard and (standard.get("compiled") is True or standard.get("source_host_boundary")),
            "Original compiler Standard result or explicit source-host boundary required")

    runtime = build["runtime_selection"]
    rsp_desc = build["compiler_response"]
    rsp = Path(rsp_desc["path"]).resolve()
    require(sha(rsp) == rsp_desc["sha256"], "Pinned source compiler response changed")
    args = [json.loads(line) for line in rsp.read_text().splitlines() if line.strip()]
    json_adapter_replaced = 0
    for i, item in enumerate(args):
        if item.startswith("-main:"): args[i] = "-main:S004StandardSetupExportSourceBinding"
        elif item.startswith("-out:"): args[i] = "-out:" + str(out / "S004StandardSetupExport.exe")
        elif item.endswith("/050_SourceConfigJson.cs"):
            args[i] = str(JSON_ADAPTER.resolve())
            json_adapter_replaced += 1
    require(json_adapter_replaced == 1, "Expected exactly one shared source JsonUtility adapter in build response")
    args.extend([str(ORIGINAL_BINDING.resolve()), str(ADAPTER.resolve())])
    adapter_rsp = out / "compile.rsp"
    adapter_rsp.write_text("\n".join(json.dumps(x) for x in args) + "\n")
    source_pins = build.get("source_and_reference_pins", {})
    for path, digest in source_pins.items():
        require(Path(path).is_file() and sha(path) == digest, "Pinned source-host input changed: " + path)
    dotnet = next((p for p in source_pins if p.endswith("/dotnet")), None)
    cscs = sorted((p for p in source_pins if re.search(r"/sdk/[0-9.]+/Roslyn/bincore/csc\.dll$", p)))
    require(dotnet and cscs, "Pinned Roslyn compiler missing from source build")
    compile_result = subprocess.run([dotnet, cscs[-1], "@" + str(adapter_rsp)],
                                    capture_output=True, text=True, timeout=120)
    (out / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    require(compile_result.returncode == 0, "Standard setup adapter compile failed; inspect compile.log")
    ref_dirs = sorted({str(Path(item[3:]).resolve().parent) for item in args
                       if item.startswith("-r:") and Path(item[3:]).is_file()})
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join(ref_dirs)
    original_result_path = out / "original-compiler-result.json"
    original_result_path.write_text(json.dumps(original_result, separators=(",", ":")) + "\n")
    invocation = subprocess.run([runtime["mono"], str(out / "S004StandardSetupExport.exe"),
                                 str(graph_path), graph["setup_matrix"]["path"], str(seed),
                                 str(original_result_path), str(replay_raw_path), str(native_standard["setupHash"])], capture_output=True, text=True,
                                timeout=60, env=env)
    (out / "run.log").write_text(invocation.stdout + invocation.stderr)
    require(invocation.returncode == 0 and invocation.stdout.strip(), "Standard setup adapter failed; inspect run.log")
    exported = json.loads(invocation.stdout.strip().splitlines()[-1])
    require(exported.get("kind") == "S004StandardCompiledSetupSourceExport" and exported.get("status") == "Passed" and
            exported.get("world_initialized") is False, "Source adapter result invalid")
    identity = exported["identity"]
    require(identity.get("size_id") == "Standard" and str(identity.get("setup_hash")) == str(native_standard["setupHash"]) and
            identity.get("setup_hash") == replay_standard["setupHash"] and identity.get("measured_layout_bound") is True,
            "Export identity differs from independent native/replay evidence")
    require(exported["authored_provenance"].get("source_pins") == graph.get("source_pins"),
            "Export provenance does not carry exact graph pins")
    full_compiler = exported.get("source_bound_compiler_result", {})
    require(full_compiler.get("compiled") is True and full_compiler.get("status") == "Compiled",
            "Source-bound original compiler result missing or failed")
    full_setups = {x.get("size_id"): x for x in full_compiler.get("setups", []) if isinstance(x, dict)}
    require(set(full_setups) == {"Standard", "War", "LargeWar"}, "Complete authored compiler matrix missing")
    native_by_size = {x["size"]: str(x["setupHash"]) for x in native["compilerRows"]}
    replay_by_size = {x["size"]: str(x["setupHash"]) for x in replay["result"]["compiler"]}
    require(all(str(full_setups[size].get("setup_hash")) == native_by_size[size] == replay_by_size[size]
                for size in ("Standard", "War", "LargeWar")),
            "Source-bound compiler hashes differ from native and hardened replay matrices")
    verify_pins(build.get("source_and_reference_pins"), "Source-host after export")
    verify_pins(graph.get("source_pins"), "S004 graph after export")
    require(sha(graph_path) == graph_sha and sha(replay_raw_path) == replay.get("replayed_raw_json_sha256") and
            sha(native_path) == replay.get("native_receipt", {}).get("sha256"),
            "Graph, validated replay bytes, or native receipt changed during export")
    export_path = out / "compiled-setup.json"
    export_path.write_text(json.dumps(exported, indent=2, ensure_ascii=False) + "\n")
    receipt = {"kind": "S004StandardSetupSourceExportReceipt", "status": "Passed",
        "source_build": {"path": str(build_path), "sha256": sha(build_path)},
        "source_graph": {"path": str(graph_path), "sha256": graph_sha},
        "capture_replay_receipt": {"path": str(replay_path), "sha256": sha(replay_path)},
        "native_capture_receipt": {"path": str(native_path), "sha256": sha(native_path)},
        "original_compiler_receipt": {"path": str(original_dir / "receipt.json"), "sha256": sha(original_dir / "receipt.json")},
        "adapter_source": {"path": str(ADAPTER), "sha256": sha(ADAPTER)},
        "diagnostic_json_adapter": {"path": str(JSON_ADAPTER), "sha256": sha(JSON_ADAPTER),
            "shared_replay_receipt": str(replay_path), "replayed_layout_sha256": sha(replay_raw_path)},
        "runner_source": {"path": str(Path(__file__).resolve()), "sha256": sha(__file__)},
        "compiled_setup": {"path": str(export_path), "sha256": sha(export_path)},
        "native_standard_setup_hash": native_standard["setupHash"],
        "source_replay_standard_setup_hash": replay_standard["setupHash"],
        "world_initialized": False, "physical_source_fit_verified": False, "mission_readiness": False}
    (out / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps({"status": "Passed", "setup_hash": identity["setup_hash"],
        "forces": len(exported["compiled_setup"]["Forces"]), "structures": len(exported["compiled_setup"]["Structures"]),
        "world_initialized": False, "physical_source_fit_verified": False, "mission_readiness": False}))
    return 0


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--build-receipt", required=True)
    ap.add_argument("--graph", required=True)
    ap.add_argument("--graph-sha256", required=True)
    ap.add_argument("--capture-replay-receipt", required=True)
    ap.add_argument("--native-capture-receipt", required=True)
    ap.add_argument("--output", required=True)
    ap.add_argument("--seed", type=int, required=True)
    args = ap.parse_args()
    try:
        return run(args.build_receipt, args.graph, args.graph_sha256, args.capture_replay_receipt,
                   args.native_capture_receipt, args.output, args.seed)
    except (ValueError, OSError, KeyError, subprocess.SubprocessError, json.JSONDecodeError) as error:
        print("S004 Standard setup export failed closed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env python3
"""Build/run the narrow S004 source-bound raw capture replay diagnostic."""
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
BINDING = HERE / "S004LayoutCaptureReplaySourceBinding.cs.txt"
ORIGINAL_BINDING = HERE / "S004OriginalCompilerSourceBinding.cs.txt"
DIAGNOSTIC_ADAPTER = HERE / "S004DiagnosticJsonUtilityAdapter.cs.txt"


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(ok, message):
    if not ok:
        raise ValueError(message)


def verify_build(build):
    pins = build.get("source_and_reference_pins")
    require(isinstance(pins, dict) and pins, "Complete source-host build pins required")
    for path, expected in pins.items():
        require(Path(path).is_file() and digest(path) == expected, "S002 source-host pin changed: " + path)


def version_key(path):
    match = re.search(r"/sdk/([0-9]+(?:\.[0-9]+)*)/Roslyn/bincore/csc\.dll$", path)
    return tuple(int(x) for x in match.group(1).split(".")) if match else ()


def validate_graph_source_pins(graph):
    pins = graph.get("source_pins")
    require(isinstance(pins, dict) and 0 < len(pins) <= 512,
            "Bounded nonempty authored graph source pin inventory required")
    for path, expected in pins.items():
        require(isinstance(path, str) and path and len(path) <= 1024 and
                isinstance(expected, str) and re.fullmatch(r"[0-9a-f]{64}", expected) is not None,
                "Malformed authored graph source pin")
        require(Path(path).is_file() and digest(path) == expected, "Authored graph source pin changed: " + path)


def validate_result(result):
    require(result.get("status") == "Passed", "Authored capture replay status is not Passed")
    require(result.get("world_initialized") is False and
            result.get("physical_source_fit_verified") is False and
            result.get("mission_readiness") is False,
            "Replay must explicitly leave world, source fit, and mission readiness unverified")
    rows = result.get("compiler")
    require(isinstance(rows, list) and len(rows) == 3, "Exactly three compiler matches required")
    by_size = {row.get("size"): row for row in rows if isinstance(row, dict)}
    require(len(by_size) == 3 and set(by_size) == {"Standard", "War", "LargeWar"} and
            all(row.get("matched") is True and isinstance(row.get("setupHash"), int) and
                row.get("nativeSetupHash") == str(row.get("setupHash")) for row in by_size.values()),
            "All three exact native compiler setup hashes must match")


def validate_capture(receipt_path, log_path):
    receipt = json.loads(Path(receipt_path).read_text())
    log = Path(log_path)
    raw = log.read_bytes()
    text = raw.decode("utf-8")
    require(receipt.get("schema") == "s004-native-json-capture-receipt-v1" and receipt.get("wrapperExit") == 0,
            "Verified successful native capture receipt required")
    authored = receipt.get("authoredS004", {})
    require(authored.get("asset") == "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishScenario_S004.asset" and
            authored.get("format") == "NativeJsonV1" and isinstance(authored.get("fingerprint"), str),
            "Native receipt does not identify exact authored S004 NativeJsonV1 capture")
    require(Path(receipt.get("captureLog", "")).resolve() == log.resolve() and
            receipt.get("captureLogSha256") == hashlib.sha256(raw).hexdigest(), "Receipt/log identity or hash mismatch")
    marker = "[S004OriginalLayoutNativeJson] result=Passed compilerCapture=Completed gameplay=NotRun world=NotCreated authoredInputsUnchanged=1 assemblyUnchanged=1"
    require(receipt.get("finalPassMarkerCount") == 1 and text.count(marker) == 1, "Unique native final pass marker required")
    authored_lines = [line for line in text.splitlines() if "[S004OriginalLayoutNativeJson] authored asset=" in line]
    require(len(authored_lines) == 1 and "asset=Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishScenario_S004.asset" in authored_lines[0],
            "Unique exact authored S004 raw capture line required")
    match = re.search(r" fingerprint=([0-9a-f]{64}) json=(\{.*\})$", authored_lines[0])
    require(match is not None and match.group(1) == authored.get("fingerprint") and
            hashlib.sha256(match.group(2).encode("utf-8")).hexdigest() == authored.get("fingerprint"),
            "Raw authored JSON fingerprint is not independently verified")
    need = receipt.get("sourcePinsVerified")
    require(isinstance(need, dict) and need, "Native source pin inventory required")
    for rel, expected in need.items():
        require(Path(rel).is_file() and digest(rel) == expected, "Native capture source pin changed: " + rel)
    rows = receipt.get("compilerRows")
    require(isinstance(rows, list) and len(rows) == 3, "Three native compiler rows required")
    by_size = {row.get("size"): row for row in rows}
    require(set(by_size) == {"Standard", "War", "LargeWar"} and
            all(by_size[size].get("outcome") == "accepted" and by_size[size].get("difficulty") == "Regular" and
                by_size[size].get("seed") == 104733 and isinstance(by_size[size].get("setupHash"), str)
                for size in by_size), "Exact native setup hash matrix required")
    return receipt, raw, text


def run(args):
    out = Path(args.output).resolve()
    require(not out.exists(), "Fresh isolated replay output required")
    build_path = Path(args.build_receipt).resolve()
    build = json.loads(build_path.read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("mission") == "S002" and
            build.get("status") == "Passed" and build.get("compile_exit") == 0 and build.get("source_count", 0) >= 700,
            "Successful S002 source build required")
    verify_build(build)
    graph_path = Path(args.graph).resolve()
    graph_sha = args.graph_sha256
    require(digest(graph_path) == graph_sha, "Explicit authored graph hash mismatch")
    graph = json.loads(graph_path.read_text())
    require(graph.get("authority") == "AuthoredConfigAssets" and graph.get("compiled_setup") is False and
            graph.get("world_initialized") is False and graph.get("native_world_state_used") is False and
            graph.get("definition_path") == "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishScenario_S004.asset",
            "Exact authored uncompiled S004 graph required")
    validate_graph_source_pins(graph)
    capture, native_raw, native_text = validate_capture(args.native_receipt, args.native_log)
    # The graph and native capture have separate source pin sets. Each set was
    # independently checked against current bytes; do not require unrelated
    # capture implementation sources to be graph dependencies.
    require("Batch mode:" in native_text and re.search(r"^Batch mode:\s+NO$", native_text, re.M),
            "Expected wrapper-driven GUI licensing native log")
    response_desc = build.get("compiler_response")
    rsp = Path(response_desc["path"]).resolve()
    require(digest(rsp) == response_desc["sha256"], "Pinned S002 compiler response changed")
    response = [json.loads(line) for line in rsp.read_text().splitlines() if line.strip()]
    require(any(item.startswith("-main:") for item in response) and any(item.startswith("-out:") for item in response),
            "S002 compiler response shape missing")
    runtime = build["runtime_selection"]
    source_pins = build["source_and_reference_pins"]
    dotnet = Path(next(path for path in source_pins if path.endswith("/dotnet")))
    cscs = sorted((path for path in source_pins if re.search(r"/sdk/[0-9.]+/Roslyn/bincore/csc\.dll$", path)), key=version_key)
    csc = Path(cscs[-1])
    mono = Path(runtime["mono"])
    require(dotnet.is_file() and csc.is_file() and mono.is_file(), "Pinned source-host toolchain missing")
    out.mkdir(parents=True)
    shutil.copy2(runtime["newtonsoft"], out / "Newtonsoft.Json.dll")
    matrix = Path(graph["setup_matrix"]["path"]).resolve()
    require(digest(matrix) == graph["setup_matrix"]["sha256"], "Original setup matrix hash mismatch")
    generated = list(response)
    adapter_replaced = 0
    for i, item in enumerate(generated):
        if item.startswith("-main:"): generated[i] = "-main:S004LayoutCaptureReplaySourceBinding"
        if item.startswith("-out:"): generated[i] = "-out:" + str(out / "S004LayoutCaptureReplay.exe")
        if item.endswith("/050_SourceConfigJson.cs"):
            generated[i] = str(DIAGNOSTIC_ADAPTER.resolve())
            adapter_replaced += 1
    require(adapter_replaced == 1, "Expected exactly one shared JsonUtility adapter in source response")
    generated.append(str(ORIGINAL_BINDING.resolve()))
    generated.append(str(BINDING.resolve()))
    rsp_path = out / "compile.rsp"
    rsp_path.write_text("\n".join(json.dumps(x) for x in generated) + "\n")
    compiled = subprocess.run([str(dotnet), str(csc), "@" + str(rsp_path)], capture_output=True, text=True, timeout=120)
    (out / "compile.log").write_text(compiled.stdout + compiled.stderr)
    verify_build(build)
    require(compiled.returncode == 0, "Capture replay compilation failed; see compile.log")
    output_raw = out / "replayed-layout.json"
    dirs = sorted({str(Path(item[3:]).resolve().parent) for item in generated if item.startswith("-r:") and Path(item[3:]).is_file()})
    env = os.environ.copy(); env["MONO_PATH"] = os.pathsep.join(dirs)
    invoked = subprocess.run([str(mono), str(out / "S004LayoutCaptureReplay.exe"), str(graph_path), str(matrix),
                              str(Path(args.native_log).resolve()), str(output_raw)], capture_output=True, text=True,
                             timeout=60, env=env)
    (out / "run.log").write_text(invoked.stdout + invoked.stderr)
    verify_build(build)
    require(digest(graph_path) == graph_sha, "Authored graph changed during replay")
    require(digest(args.native_log) == capture["captureLogSha256"], "Native capture log changed during replay")
    for path, expected in graph["source_pins"].items():
        require(Path(path).is_file() and digest(path) == expected, "Authored graph source changed during replay: " + path)
    require(invoked.returncode == 0 and '"kind":"S004AuthoredLayoutCaptureReplay"' in invoked.stdout,
            "Authored capture replay failed; see run.log")
    result = json.loads(invoked.stdout.strip().splitlines()[-1])
    # Preserve a clear failure classification if the host reports a boundary.
    # A zero process exit is not acceptance; only the checks below authorize the
    # final Passed receipt and zero runner exit.
    (out / "receipt.json").write_text(json.dumps({
        "kind": "S004AuthoredLayoutCaptureReplayReceipt", "status": result.get("status", "InvalidResult"),
        "mode": "SourceBoundRawCaptureReplay", "result": result,
        "world_initialized": result.get("world_initialized"),
        "physical_source_fit_verified": result.get("physical_source_fit_verified"),
        "mission_readiness": result.get("mission_readiness"),
    }, indent=2) + "\n")
    validate_result(result)
    require(result.get("rawReplayToJsonCalls", 0) > 0, "Diagnostic JsonUtility did not replay captured LayoutValues bytes")
    replay = output_raw.read_bytes()
    # Exact bytes are recovered from the log line, not the receipt's parsed JSON field.
    marker = "[S004OriginalLayoutNativeJson] authored asset="
    line = next(line for line in native_text.splitlines() if marker in line)
    native_json = line.split(" json=", 1)[1].encode("utf-8")
    require(replay == native_json, "Replayed bytes differ from the verified raw native authored JSON")
    receipt = {"kind": "S004AuthoredLayoutCaptureReplayReceipt", "status": result["status"], "mode": "SourceBoundRawCaptureReplay",
               "source_build": {"path": str(build_path), "sha256": digest(build_path)},
               "source_graph": {"path": str(graph_path), "sha256": graph_sha},
               "native_receipt": {"path": str(Path(args.native_receipt).resolve()), "sha256": digest(args.native_receipt)},
               "native_log": {"path": str(Path(args.native_log).resolve()), "sha256": capture["captureLogSha256"]},
               "replayed_raw_json_sha256": hashlib.sha256(replay).hexdigest(), "result": result,
               "harness_source": {"path": str(BINDING.resolve()), "sha256": digest(BINDING)},
               "runner_source": {"path": str(Path(__file__).resolve()), "sha256": digest(__file__)},
               "original_compiler_binding": {"path": str(ORIGINAL_BINDING.resolve()), "sha256": digest(ORIGINAL_BINDING)},
               "diagnostic_adapter": {"path": str(DIAGNOSTIC_ADAPTER.resolve()), "sha256": digest(DIAGNOSTIC_ADAPTER),
                                     "replaced_shared_adapter_only_in_generated_response": True},
               "compiler_response_sha256": digest(rsp_path), "compile_log_sha256": digest(out / "compile.log"),
               "run_log_sha256": digest(out / "run.log"), "world_initialized": False,
               "physical_source_fit_verified": False, "mission_readiness": False}
    (out / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps({"status": result["status"], "mode": "SourceBoundRawCaptureReplay",
                      "setup_hashes_matched": sum(1 for row in result["compiler"] if row.get("matched")),
                      "world_initialized": False, "mission_readiness": False}))
    return 0


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--build-receipt", required=True); p.add_argument("--graph", required=True)
    p.add_argument("--graph-sha256", required=True); p.add_argument("--native-receipt", required=True)
    p.add_argument("--native-log", required=True); p.add_argument("--output", required=True)
    args = p.parse_args()
    try: return run(args)
    except (ValueError, OSError, KeyError, TypeError, subprocess.SubprocessError, json.JSONDecodeError) as exc:
        print("S004 authored capture replay failed closed: " + str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

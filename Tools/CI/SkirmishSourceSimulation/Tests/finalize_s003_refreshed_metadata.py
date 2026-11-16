#!/usr/bin/env python3
"""Verify a fresh S003 asset-only metadata capture without admitting a mission."""
import argparse
import hashlib
import json
from pathlib import Path

MARKER = "[SkirmishS003MetadataCapture] result=Passed catalog=S003 seed=104732 scope=DataOnly native_context_qualified=false mission_ready=false"
METHOD = "Game.Editor.SkirmishBakedActorSnapshotExporter.RunS003MetadataCapture"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate field: " + key)
        result[key] = value
    return result


def qualify(root, capture, log, compiler, wrapper_exit):
    root = root.resolve(strict=True)
    raw, lograw, compilerraw = capture.read_bytes(), log.read_bytes(), compiler.read_bytes()
    value = json.loads(raw, object_pairs_hook=unique)
    build = json.loads(compilerraw, object_pairs_hook=unique)
    text = lograw.decode("utf-8", errors="strict")
    if wrapper_exit != 0 or sum(line.endswith(MARKER) for line in text.splitlines()) != 1 or METHOD not in text:
        raise ValueError("Checked wrapper exit, method and single pass marker required")
    if (value.get("kind") != "NativeS003OriginalCompilerFactoryMetadataCapture" or
        value.get("scope") != "CurrentPackagedS003CompilerAndOriginalMetadataFactories_DataOnly_NoMissionWorld"):
        raise ValueError("Expected asset-only native capture")
    for name in ("full_mission_initialized", "mission_world_created", "runtime_state_transferred",
                 "source_fit_certified", "native_context_qualified", "mission_ready"):
        if value.get(name) is not False:
            raise ValueError("Capture scope mismatch: " + name)
    if value.get("utility_world_created") is not True or value.get("utility_world_disposed") is not True:
        raise ValueError("Disposed utility world evidence required")
    context = value["context"]
    if (context.get("catalog_id"), context.get("seed"), context.get("setup_hash")) != ("S003", 104732, 2394754492):
        raise ValueError("S003 target mismatch")
    if context.get("wrong_context_hash_rejected") is not True or context.get("wrong_target_seed_rejected") is not True:
        raise ValueError("Native target negative controls required")
    graph = value["source_graph"]
    graphpath = Path(graph["path"])
    if not graphpath.is_file() or sha(graphpath.read_bytes()) != graph["sha256"]:
        raise ValueError("Graph bytes changed")
    if graph["sha256"] != "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb":
        raise ValueError("Expected regenerated S003 graph required")
    pins = value["source_pins"]
    if not isinstance(pins, dict) or len(pins) < 8000:
        raise ValueError("Complete capture pin set required")
    for name, expected in pins.items():
        path = Path(name)
        path = path if path.is_absolute() else root / path
        path = path.resolve(strict=True)
        if not path.is_relative_to(root) or not path.is_file() or sha(path.read_bytes()) != expected:
            raise ValueError("Current capture source changed: " + name)
    result = build.get("result", {})
    standard = [x for x in result.get("setups", []) if x.get("size_id") == "Standard"]
    if (build.get("kind") != "S003OriginalCompilerHarnessReceipt" or build.get("status") != "Compiled"
        or build.get("seed") != 104732 or len(standard) != 1 or standard[0].get("setup_hash") != context["setup_hash"]
        or build.get("world_initialized") is not False or build.get("mission_readiness") is not False):
        raise ValueError("Independent current source compiler agreement required")
    return {"status": "PassedDataOnlyCapture", "wrapper_exit_code": wrapper_exit, "required_marker_present": True,
            "capture": {"path": str(capture.resolve()), "sha256": sha(raw)},
            "editor_log": {"path": str(log.resolve()), "sha256": sha(lograw)},
            "source_compiler_receipt": {"path": str(compiler.resolve()), "sha256": sha(compilerraw)},
            "current_source_pins_verified": len(pins), "setup_hash": context["setup_hash"],
            "mission_world_created": False, "native_context_qualified": False,
            "source_fit_certified": False, "mission_ready": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("repo", "capture", "log", "compiler", "output"):
        parser.add_argument("--" + name, required=True)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    args = parser.parse_args()
    receipt = qualify(Path(args.repo), Path(args.capture), Path(args.log), Path(args.compiler), args.wrapper_exit)
    with Path(args.output).open("x") as stream:
        json.dump(receipt, stream, indent=2)
        stream.write("\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Audit current S003 source/compiler/factory evidence without runtime admission."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_metadata import qualify
from project_s003_current_factory_metadata import project_payload, read_factory_sources
from project_s003_refreshed_factory_metadata import CAPTURE_SHA


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def descriptor(path):
    return {"path": str(path.resolve(strict=True)), "sha256": sha(path.read_bytes())}


def verify_pins(root, pins):
    normalized = {}
    for name, digest in pins.items():
        path = Path(name)
        path = path if path.is_absolute() else root / path
        if path.is_symlink() or not path.is_file() or sha(path.read_bytes()) != digest:
            raise ValueError("Source pin missing or changed: " + name)
        key = str(path.resolve(strict=True))
        if key in normalized and normalized[key] != digest:
            raise ValueError("Conflicting normalized source pins: " + name)
        normalized[key] = digest
    return normalized


def audit(root, buildpath=None, compilerpath=None):
    root = root.resolve(strict=True)
    graphpath = root / "T Logs/SkirmishProduction/s003-runtime-graph-refresh-20261104-T1/source-config-graph.json"
    compilerpath = Path(compilerpath) if compilerpath is not None else root / "T Logs/SkirmishProduction/s003-runtime-graph-compiler-20261104-T4/receipt.json"
    buildpath = Path(buildpath) if buildpath is not None else root / "T Logs/SkirmishProduction/s002-refreshed-s003-profile-build-20261104-T3/build.json"
    for candidate in (compilerpath, buildpath):
        if not candidate.is_absolute() or candidate.is_symlink() or not candidate.resolve(strict=True).is_relative_to(root):
            raise ValueError("Canonical repository-owned compiler/build evidence required")
    capturepath = root / "Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-r3/data/s003-native-metadata.json"
    logpath = root / "T Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-T3/editor.log"
    capture_receipt = root / "T Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-T3/receipt.json"
    independent_compiler = root / "T Logs/SkirmishProduction/s003-current-original-compiler-20261104-T1/receipt.json"
    projection_dir = root / "T Logs/SkirmishProduction/s003-refreshed-factory-projection-20261104-T1"
    graph, compiler, build, capture = [json.loads(path.read_bytes()) for path in (graphpath, compilerpath, buildpath, capturepath)]
    q = qualify(root, capturepath, logpath, independent_compiler, 0)
    if q != json.loads(capture_receipt.read_bytes()) or q["capture"]["sha256"] != CAPTURE_SHA:
        raise ValueError("Fresh checked capture receipt required")
    if (compiler.get("status") != "Compiled" or compiler.get("seed") != 104732
        or compiler.get("source_graph") != descriptor(graphpath)
        or compiler.get("source_build") != descriptor(buildpath)
        or compiler.get("source_graph_source_pins") != graph.get("source_pins")
        or compiler.get("world_initialized") is not False or compiler.get("mission_readiness") is not False):
        raise ValueError("Current compiler must bind exact graph and build")
    if build.get("status") != "Passed" or build.get("compile_exit") != 0:
        raise ValueError("Passing source build required")
    setups = compiler.get("result", {}).get("setups", [])
    standard = [row for row in setups if row.get("size_id") == "Standard"]
    if len(standard) != 1 or standard[0].get("setup_hash") != capture["context"]["setup_hash"]:
        raise ValueError("Compiler/native setup mismatch")
    groups = {"compiler_graph": verify_pins(root, graph["source_pins"]),
              "capture": verify_pins(root, capture["source_pins"]),
              "source_build": verify_pins(root, build["source_and_reference_pins"])}
    union = {}
    for pins in groups.values():
        for name, digest in pins.items():
            if name in union and union[name] != digest:
                raise ValueError("Cross-evidence source mismatch: " + name)
            union[name] = digest
    projection = json.loads((projection_dir / "projection-receipt.json").read_bytes())
    expected = project_payload(capture, CAPTURE_SHA, capture["source_pins"], read_factory_sources(root), CAPTURE_SHA)
    expected["configured-metadata.json"]["reference"] = projection["artifacts"]["building-definitions.json"]
    for name, value in expected.items():
        path = projection_dir / name
        if projection["artifacts"].get(name) != descriptor(path) or json.loads(path.read_bytes()) != value:
            raise ValueError("Factory projection mismatch: " + name)
    if any(projection.get(flag) is not False for flag in ("native_context_qualified", "source_fit_certified", "runtime_admission", "mission_ready")):
        raise ValueError("Projection scope promotion")
    return {"kind": "S003RefreshedSourceEvidenceChainAudit", "status": "PassedSourceOnlyEvidence",
            "compiler": descriptor(compilerpath), "source_build": descriptor(buildpath),
            "runtime_graph": descriptor(graphpath), "native_capture": descriptor(capturepath),
            "capture_receipt": descriptor(capture_receipt), "projection_receipt": descriptor(projection_dir / "projection-receipt.json"),
            "pin_counts": {name: len(pins) for name, pins in groups.items()}, "unique_pins": len(union),
            "setup_hash": standard[0]["setup_hash"], "runtime_admission": False,
            "native_context_qualified": False, "source_fit_certified": False, "mission_ready": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--build")
    parser.add_argument("--compiler")
    args = parser.parse_args()
    if bool(args.build) != bool(args.compiler):
        parser.error("--build and --compiler must be supplied together")
    receipt = audit(Path(args.repo), args.build, args.compiler)
    with Path(args.output).open("x") as stream:
        json.dump(receipt, stream, indent=2)
        stream.write("\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()

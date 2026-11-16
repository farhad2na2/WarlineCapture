#!/usr/bin/env python3
"""Create a fresh S073 bundle revision bound to qualified native AI evidence."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

_FINALIZER_PATH = Path(__file__).with_name("finalize_s073_native_ai_snapshot.py")
_FINALIZER_SPEC = importlib.util.spec_from_file_location("s073_native_ai_snapshot_finalizer", _FINALIZER_PATH)
_FINALIZER = importlib.util.module_from_spec(_FINALIZER_SPEC)
_FINALIZER_SPEC.loader.exec_module(_FINALIZER)
qualify = _FINALIZER.qualify


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def descriptor(path):
    path = Path(path).resolve(strict=True)
    return {"path": str(path), "sha256": sha(path)}


def validate_graph(repo, path):
    graph = json.loads(path.read_text())
    if (graph.get("schema_version") != 1 or graph.get("authority") != "AuthoredConfigAssets" or
            graph.get("compiled_setup") is not False or graph.get("world_initialized") is not False or
            graph.get("native_world_state_used") is not False or
            graph.get("scope") != "Authored source readback only; no native or mission readiness" or
            not isinstance(graph.get("nodes"), list) or not graph["nodes"]):
        raise ValueError("Qualified authored S073 AI source graph required")
    pins = graph.get("source_pins")
    if not isinstance(pins, dict) or not pins:
        raise ValueError("AI source graph pins required")
    root = repo.resolve(strict=True)
    resolved_pins = {}
    for relative, expected in pins.items():
        pin_path = Path(relative)
        source = (pin_path if pin_path.is_absolute() else root / pin_path).resolve(strict=True)
        if not source.is_relative_to(root) or sha(source) != expected:
            raise ValueError("AI source graph pin changed: " + relative)
        resolved_pins[source] = expected
    for row in graph["nodes"]:
        source = row.get("source_path")
        if not isinstance(source, str) or (root / source).resolve(strict=True) not in resolved_pins:
            raise ValueError("AI graph node is not bound to its source pin")
        if row.get("source_sha256") != resolved_pins[(root / source).resolve(strict=True)]:
            raise ValueError("AI graph node is not bound to its source pin")
    return graph


def rebind(repo, source_bundle, snapshot_dir, graph_path, output):
    repo = Path(repo).resolve(strict=True)
    source_bundle = Path(source_bundle).resolve(strict=True)
    snapshot_dir = Path(snapshot_dir).resolve(strict=True)
    graph_path = Path(graph_path).resolve(strict=True)
    output = Path(output).absolute()
    if output.exists():
        raise ValueError("Fresh output bundle path required")
    if output.resolve().parent == source_bundle.parent and output.resolve() == source_bundle:
        raise ValueError("Output must not replace source bundle")

    bundle = json.loads(source_bundle.read_text())
    if (bundle.get("schema_version") != 1 or bundle.get("kind") != "S073LegacyStartupBundle" or
            bundle.get("mission") != "S073" or bundle.get("seed") != 104729):
        raise ValueError("Exact S073 startup bundle required")

    # Validate every retained descriptor before carrying it into the new bundle.
    # The two fields being rebound are intentionally excluded from old-pin checks.
    replacements = {"native_ai_snapshot_receipt", "native_ai_source_graph"}
    for name, value in bundle.items():
        if name in replacements:
            continue
        if isinstance(value, dict) and ("path" in value or "sha256" in value):
            if not isinstance(value.get("path"), str) or not isinstance(value.get("sha256"), str):
                raise ValueError("Malformed existing bundle descriptor: " + name)
            target = Path(value["path"]).resolve(strict=True)
            if sha(target) != value["sha256"]:
                raise ValueError("Existing bundle descriptor hash mismatch: " + name)

    expected_receipt = qualify(repo, snapshot_dir, 0)
    receipt_path = snapshot_dir / "snapshot-receipt.json"
    receipt = json.loads(receipt_path.read_text())
    if receipt != expected_receipt:
        raise ValueError("Snapshot receipt does not match current qualified snapshot and log")
    validate_graph(repo, graph_path)

    bundle["native_ai_snapshot_receipt"] = descriptor(receipt_path)
    bundle["native_ai_source_graph"] = descriptor(graph_path)
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x") as stream:
        json.dump(bundle, stream, indent=2)
        stream.write("\n")
    return {"status": "Passed", "bundle": str(output.resolve()), "bundle_sha256": sha(output),
            "receipt_sha256": sha(receipt_path), "graph_sha256": sha(graph_path),
            "retained_descriptor_count": sum(isinstance(v, dict) and "path" in v for k, v in bundle.items() if k not in replacements)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--source-bundle", required=True, type=Path)
    parser.add_argument("--snapshot-dir", required=True, type=Path)
    parser.add_argument("--ai-graph", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    print(json.dumps(rebind(args.repo, args.source_bundle, args.snapshot_dir, args.ai_graph, args.output)))


if __name__ == "__main__":
    main()

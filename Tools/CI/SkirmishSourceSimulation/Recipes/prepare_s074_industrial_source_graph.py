#!/usr/bin/env python3
"""Add the current Industrial Basin map to a pinned shared-config graph.

This provides source metadata only. It does not create an S074 definition,
measure a layout, initialize a World, or certify source fit or readiness.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[4]
GENERATOR = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/generate_s004_typed_config_graph.py"
MAP = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate field: " + key)
        result[key] = value
    return result


def read(path):
    return json.loads(path.read_bytes(), object_pairs_hook=unique,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError(value)))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", required=True, type=Path)
    parser.add_argument("--retained", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    base, retained = args.base.resolve(strict=True), args.retained.resolve(strict=True)
    if not base.is_relative_to(ROOT) or not retained.is_relative_to(ROOT):
        raise ValueError("Repository-owned graph inputs required")
    b, old = read(base), read(retained)
    if (b.get("authority") != "AuthoredConfigAssets" or
            any(b.get(k) is not False for k in ("compiled_setup", "world_initialized", "native_world_state_used"))):
        raise ValueError("Uncompiled shared source graph required")
    pins = b.get("source_pins")
    if not isinstance(pins, dict) or not pins:
        raise ValueError("Pinned graph required")
    for name, expected in pins.items():
        p = Path(name)
        if not p.is_absolute() or p.is_symlink() or not p.resolve(strict=True).is_relative_to(ROOT) or digest(p) != expected:
            raise ValueError("Shared source pin changed: " + name)
    rows = [n for n in old.get("nodes", []) if n.get("source_path") == MAP]
    if len(rows) != 1 or rows[0].get("source_type") != "OperationMapDefinition":
        raise ValueError("One retained Industrial Basin map projection required")
    node = rows[0]
    asset = ROOT / MAP
    if digest(asset) != node.get("source_sha256"):
        raise ValueError("Industrial Basin asset changed")
    spec = importlib.util.spec_from_file_location("source_graph_generator", GENERATOR)
    generator = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(generator)
    reader = ROOT / generator.READER_REL
    doc = generator.strict_read(reader, [asset])[str(asset)]
    mono = doc["MonoBehaviour"]
    fields = {k: v for k, v in mono.items() if not k.startswith("m_")}
    spellings = {k[len("MonoBehaviour."):]: v for k, v in doc["__source_scalar_spellings"].items()
                 if k.startswith("MonoBehaviour.")}
    if fields != node["fields"] or spellings != node["scalar_spellings"] or generator.refs(fields):
        raise ValueError("Current serialized map projection differs or has unresolved object references")
    if fields.get("operationMapId") != "opmap.skirmish.industrial_basin" or fields.get("sourceBinding", {}).get("sourceOperationMapId") != "opmap.skirmish.desert_base_01":
        raise ValueError("Current logical and physical map identities differ")
    meta = Path(str(asset) + ".meta")
    matches = re.findall(r"^guid: ([0-9a-f]{32})$", meta.read_text(), re.M)
    if matches != [node["asset_guid"]]:
        raise ValueError("Current map GUID differs")
    script_ref = mono.get("m_Script", {})
    hits = generator.guid_hits(ROOT / "Assets", script_ref.get("guid", ""))
    if len(hits) != 1:
        raise ValueError("Unique original map authoring script required")
    script_meta, script = generator.checked_meta_target(ROOT / "Assets", hits[0])
    if script.name != "OperationMapDefinition.cs" or script_ref.get("fileID") != 11500000:
        raise ValueError("Original map authoring script identity differs")
    result = copy.deepcopy(b)
    if any(n.get("asset_guid") == node["asset_guid"] or n.get("source_path") == MAP for n in result["nodes"]):
        raise ValueError("Map already present or GUID collision")
    result["nodes"].append(copy.deepcopy(node))
    added = [asset, meta, script, script_meta, reader, GENERATOR, Path(__file__).resolve()]
    for p in added:
        actual = digest(p)
        if str(p) in result["source_pins"] and result["source_pins"][str(p)] != actual:
            raise ValueError("Conflicting source pin: " + str(p))
        result["source_pins"][str(p)] = actual
    result["scope"] = ("Shared pinned config graph with actual Industrial Basin map metadata; "
                       "base definition remains S002. No S074 layout, compilation, World, or readiness.")
    for name, expected in result["source_pins"].items():
        if digest(Path(name)) != expected:
            raise ValueError("Source pin changed during graph construction: " + name)
    out = args.output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError("Repository-owned output required")
    out.mkdir(parents=True, exist_ok=False)
    graph = out / "source-config-graph.json"
    graph.write_text(json.dumps(result, indent=2) + "\n")
    receipt = {"kind": "S074IndustrialBasinSourceGraphProjection", "status": "PreparedSourceOnly",
               "base": {"path": str(base), "sha256": digest(base)},
               "retained": {"path": str(retained), "sha256": digest(retained)},
               "graph": {"path": str(graph), "sha256": digest(graph)},
               "operation_map_id": fields["operationMapId"], "campaign_source_id": fields["sourceBinding"]["sourceOperationMapId"],
               "s074_layout_supplied": False, "compiler_invoked": False, "world_initialized": False,
               "source_fit_certified": False, "mission_ready": False}
    (out / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps({"status": receipt["status"], "graph_sha256": receipt["graph"]["sha256"], "mission_ready": False}))


if __name__ == "__main__":
    main()

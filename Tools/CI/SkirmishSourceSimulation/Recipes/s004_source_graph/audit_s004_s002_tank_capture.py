#!/usr/bin/env python3
"""Audit whether the current S002 capture contains S004's tank source key."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
CAPTURE = ROOT / "T Logs/SkirmishProduction/s002-current-baked-templates-20261104-T1/prefab-reference-data.json"
RECEIPT = CAPTURE.parent / "capture-receipt.json"
LOG = CAPTURE.parent / "editor.log"
INVENTORY = ROOT / "T Logs/SkirmishProduction/s004-missing-four-focused-keys-20261103-T1/receipt.json"
GRAPH = ROOT / "T Logs/SkirmishProduction/s004-catalog-closure-refresh-20261104-T1/source-config-graph.json"
KEY = "Unit_Veh_Tank_USA"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    output = Path(args.output).absolute()
    require(not output.exists(), "Output must be new")
    paths = (CAPTURE, RECEIPT, LOG, INVENTORY, GRAPH)
    before = {str(path): digest(path) for path in paths}
    capture, receipt, inventory, graph = (json.loads(path.read_text()) for path in (CAPTURE, RECEIPT, INVENTORY, GRAPH))
    require(receipt.get("validation_status") == "Passed" and receipt.get("wrapper_exit_code") == 0 and receipt.get("required_marker_present") is True, "Checked scoped capture required")
    require(receipt.get("capture_sha256") == before[str(CAPTURE)] and receipt.get("editor_log_sha256") == before[str(LOG)], "Capture or log changed")
    pins = capture.get("source_pins")
    require(isinstance(pins, dict) and len(pins) == receipt.get("source_pin_count"), "Complete capture pins required")
    for name, expected in pins.items():
        path = ROOT / name
        require(path.is_file() and digest(path) == expected, "Capture source pin changed: " + name)
    graph_pins = graph.get("source_pins")
    require(graph.get("authority") == "AuthoredConfigAssets" and isinstance(graph_pins, dict) and graph_pins, "Authored graph required")
    for name, expected in graph_pins.items():
        path = ROOT / name
        require(path.is_file() and digest(path) == expected, "Graph source pin changed: " + name)
    rows = [r for r in inventory["source_assets"] if r.get("key") == KEY]
    require(len(rows) == 1, "Unique S004 tank inventory row required")
    row = rows[0]
    asset = Path(row["path"])
    require(asset.is_relative_to(ROOT) and asset.is_file() and digest(asset) == row["sha256"], "Tank asset changed")
    meta = Path(str(asset) + ".meta")
    require(meta.is_file() and digest(meta) == row["meta_sha256"] and "guid: " + row["guid"] in meta.read_text(), "Tank metadata changed")
    require(pins.get(asset.relative_to(ROOT).as_posix()) == row["sha256"], "Capture did not pin inventoried asset")
    roles = [r for n in graph["nodes"] if n.get("source_type") == "SkirmishRoleCatalogConfig" for r in n["fields"]["roles"] if r.get("RuntimePrefabKey") == KEY]
    require(len(roles) == 1 and (ROOT / roles[0]["SourceConfigPath"]).is_file(), "Unique current role key required")
    root_id = capture.get("opening_prefabs", {}).get(KEY)
    entities = [e for e in capture.get("entities", []) if e.get("id") == root_id]
    require(KEY in capture.get("requested_prefab_keys", []) and root_id and len(entities) == 1, "Captured tank root required")
    require({str(path): digest(path) for path in paths} == before, "Audit inputs changed")
    result = {"status": "PassedSourceKeyAndCurrentAssetPinAudit", "key": KEY, "capture_root_id": root_id,
              "source_pin_count": len(pins), "graph_source_pin_count": len(graph_pins), "source_asset": row,
              "role": roles[0], "input_sha256": before, "native_entity_identity_join_proven": False,
              "capture_template_imported": False, "other_missing_s004_keys_resolved": False,
              "mission_initialized": False, "source_fit_certified": False, "mission_ready": False}
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
    print(json.dumps({"status": result["status"], "key": KEY, "source_pins_checked": len(pins), "output": str(output)}))


if __name__ == "__main__":
    main()

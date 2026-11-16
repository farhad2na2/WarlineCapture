#!/usr/bin/env python3
"""Strict offline finalization for the S004 prepared visual mesh-bounds capture."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
import sys
from pathlib import Path
from typing import Any

ROOT_DEFAULT = Path(__file__).resolve().parents[2]
CAPTURE_DIR = "Logs/SkirmishProduction/s004-prepared-mesh-bounds-capture-20261103-r1"
INVENTORY = "T Logs/SkirmishProduction/s004-prepared-entity-visual-dependency-inventory-20261103-r1/visual-dependency-inventory.json"
METHOD = "Game.Editor.S004PreparedVisualMeshBoundsCapture.CaptureInventoryMeshBounds"
MARKER = "[S004PreparedVisualMeshBoundsCapture] result=Passed"
EXPORTER = "Assets/Game/Scripts/Editor/Skirmish/S004PreparedVisualMeshBoundsCapture.cs"


class FinalizeError(ValueError):
    pass


def need(ok: bool, message: str) -> None:
    if not ok:
        raise FinalizeError(message)


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_json(path: Path, label: str) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise FinalizeError(label + " is unreadable or invalid JSON") from exc


def project_file(root: Path, raw: str) -> Path:
    candidate = Path(raw)
    path = (candidate if candidate.is_absolute() else root / candidate).resolve()
    try:
        path.relative_to(root.resolve())
    except ValueError as exc:
        raise FinalizeError("source path escapes project root: " + raw) from exc
    return path


def expected_meshes(inventory: dict) -> dict[str, dict]:
    prefabs = inventory.get("prefab_inventory")
    need(isinstance(prefabs, dict) and bool(prefabs), "inventory prefab_inventory missing")
    expected: dict[str, dict] = {}
    for prefab_guid, prefab in prefabs.items():
        need(re.fullmatch(r"[0-9a-f]{32}", prefab_guid) is not None, "malformed prefab GUID")
        roots = prefab.get("source_local_roots")
        need(isinstance(roots, list), "prefab source_local_roots missing")
        for rootrow in roots:
            filters = rootrow.get("mesh_filters")
            need(isinstance(filters, list), "source root mesh_filters missing")
            for filterrow in filters:
                ref = filterrow.get("mesh")
                need(isinstance(ref, dict), "mesh reference missing")
                guid, local_id = ref.get("guid"), ref.get("file_id")
                need(isinstance(guid, str) and re.fullmatch(r"[0-9a-f]{32}", guid) is not None and
                     isinstance(local_id, str) and re.fullmatch(r"-?[0-9]+", local_id) is not None,
                     "malformed mesh GUID/local ID")
                key = guid + ":" + str(int(local_id))
                expected.setdefault(key, {"guid": guid, "local_file_id": int(local_id)})
    need(bool(expected), "inventory contains no meshes")
    return expected


def verify(log_path: Path, raw_path: Path, inventory_path: Path, root: Path, wrapper_exit: int) -> dict:
    need(wrapper_exit == 0, "Unity wrapper exit must be exactly zero")
    root = root.resolve()
    log = log_path.read_text(encoding="utf-8", errors="strict")
    markers = [line for line in log.splitlines() if line.startswith(MARKER)]
    need(len(markers) == 1 and markers[0].startswith(MARKER + " mesh_count="), "expected exactly one capture pass marker")
    need("-executeMethod\n" + METHOD in log and "-batchmode" not in log and str(root) in log,
         "capture log method/project/mode mismatch")
    raw = load_json(raw_path, "mesh-bounds export")
    inventory = load_json(inventory_path, "prepared visual inventory")
    need(inventory.get("kind") == "S004PreparedEntitiesVisualDependencyInventory" and
         inventory.get("status") == "PassedInventoryOnly", "unexpected inventory kind/status")
    need(inventory.get("source_pins_before_after") == "passed", "inventory before/after source pin check missing")
    need(raw.get("kind") == "S004PreparedVisualMeshBoundsCapture" and raw.get("schema_version") == 1 and
         raw.get("status") == "Passed" and raw.get("capture_mode") ==
         "AssetDatabaseExactGuidLocalId_NoSceneLoad_NoGameObjects_NoSimulation", "capture identity/scope mismatch")
    need(raw.get("input_inventory_sha256") == digest(inventory_path), "raw export is not bound to current inventory bytes")
    need(raw.get("input_inventory_path") == str(inventory_path.resolve()), "raw export input inventory path mismatch")
    expected = expected_meshes(inventory)
    rows = raw.get("meshes")
    need(isinstance(rows, list) and len(rows) == len(expected) and raw.get("unique_mesh_count") == len(expected),
         "mesh rows do not cover the exact unique inventory identities")
    observed = {}
    for row in rows:
        key = str(row.get("guid")) + ":" + str(row.get("local_file_id"))
        need(key in expected and key not in observed, "unexpected or duplicate mesh identity: " + key)
        need(row.get("guid") == expected[key]["guid"] and type(row.get("local_file_id")) is int and
             row["local_file_id"] == expected[key]["local_file_id"], "mesh identity type/value mismatch")
        asset = project_file(root, row.get("asset_path", ""))
        need(asset.is_file(), "mesh asset missing: " + str(asset))
        need(row.get("asset_dependency_hash"), "mesh asset dependency hash missing")
        center, size = row.get("bounds_center"), row.get("bounds_size")
        for label, vector in (("center", center), ("size", size)):
            need(isinstance(vector, list) and len(vector) == 3 and all(type(x) in (int, float) and math.isfinite(x) for x in vector),
                 "nonfinite or malformed bounds " + label + ": " + key)
        need(all(x >= 0 for x in size), "negative mesh bounds size: " + key)
        observed[key] = row
    need(set(observed) == set(expected), "missing inventory mesh identity")

    pins = inventory.get("asset_and_meta_pins")
    need(isinstance(pins, dict) and bool(pins), "inventory asset/meta pin set missing")
    checked = 0
    for pin_path, pin in pins.items():
        need(isinstance(pin, dict) and pin.get("status") == "pinned" and
             re.fullmatch(r"[0-9a-f]{64}", str(pin.get("sha256", ""))) is not None,
             "malformed inventory pin: " + pin_path)
        path = project_file(root, pin_path)
        need(path.is_file() and digest(path) == pin["sha256"], "inventory source pin differs from current file: " + pin_path)
        checked += 1
    source_hash = digest(project_file(root, EXPORTER))
    # These immutable inputs are pinned separately in the inventory pin map; require their exact inventory identities too.
    scene = inventory.get("input_scene", {})
    binding = inventory.get("source_map_binding_context", {})
    for path_key, hash_key in (("path", "sha256"),):
        raw_scene = scene.get(path_key)
        need(isinstance(raw_scene, str) and project_file(root, raw_scene).is_file(), "prepared scene input missing")
        need(digest(project_file(root, raw_scene)) == scene.get(hash_key), "prepared scene input hash changed")
    for path_key, hash_key in (("runtime_binding_scene", "runtime_binding_sha256"),
                                ("operation_map_asset", "operation_map_sha256")):
        raw_input = binding.get(path_key)
        need(isinstance(raw_input, str) and project_file(root, raw_input).is_file(), "map binding input missing: " + path_key)
        need(digest(project_file(root, raw_input)) == binding.get(hash_key), "map binding input hash changed: " + path_key)
    need(int(markers[0].split("mesh_count=", 1)[1].split()[0]) == len(expected), "pass marker mesh count mismatch")
    return {
        "schema": "s004-prepared-mesh-bounds-capture-receipt-v1",
        "evidenceClass": "OfflineFinalizationOfReadOnlyAssetMeshBoundsCapture",
        "wrapper": {"path": "Tools/CI/invoke_unity_macos.sh", "exitCode": wrapper_exit,
                    "mode": "GUI", "batchmode": False},
        "passMarker": markers[0], "captureMethod": METHOD,
        "captureLog": str(log_path.resolve()), "captureLogSha256": digest(log_path),
        "rawExport": str(raw_path.resolve()), "rawExportSha256": digest(raw_path),
        "inventory": str(inventory_path.resolve()), "inventorySha256": digest(inventory_path),
        "sourcePinsRehashedCurrent": checked, "sourcePinsBeforeAfter": "passed",
        "captureExporter": EXPORTER, "captureExporterSha256Current": source_hash,
        "preparedSceneSha256": scene["sha256"],
        "runtimeBindingSceneSha256": binding["runtime_binding_sha256"],
        "operationMapAssetSha256": binding["operation_map_sha256"],
        "uniqueMeshCount": len(expected), "meshRowsValidated": len(observed),
        "exactInventoryIdentityCoverage": True, "finiteBounds": True,
        "sceneLoaded": False, "gameObjectsCreated": False, "simulationRun": False,
        "missionReadiness": False, "visualReadiness": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT_DEFAULT)
    parser.add_argument("--log", type=Path)
    parser.add_argument("--raw", type=Path)
    parser.add_argument("--inventory", type=Path)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    parser.add_argument("--out-dir", type=Path)
    args = parser.parse_args()
    root = args.root.resolve()
    out = args.out_dir.resolve() if args.out_dir else root / CAPTURE_DIR
    log = args.log or out / "editor.log"
    raw = args.raw or out / "mesh-bounds.json"
    inventory = args.inventory or root / INVENTORY
    receipt_path = out / "capture-receipt.json"
    source_path = out / "capture-source.json"
    try:
        need(not receipt_path.exists() and not source_path.exists(), "fresh finalizer outputs required")
        receipt = verify(log, raw, inventory, root, args.wrapper_exit)
        out.mkdir(parents=True, exist_ok=True)
        receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        source_path.write_text(json.dumps({"schema": receipt["schema"], "rawExportSha256": receipt["rawExportSha256"],
                                           "inventorySha256": receipt["inventorySha256"],
                                           "captureExporterSha256Current": receipt["captureExporterSha256Current"]},
                                          indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print("[S004PreparedMeshBoundsFinalizer] result=Passed meshes=" + str(receipt["uniqueMeshCount"]) +
              " pinsRehashed=" + str(receipt["sourcePinsRehashedCurrent"]) + " sceneLoaded=0 visualReadiness=0 missionReadiness=0")
        return 0
    except (OSError, UnicodeError, json.JSONDecodeError, FinalizeError, TypeError, KeyError) as exc:
        print("[S004PreparedMeshBoundsFinalizer] result=Failed reason=" + str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

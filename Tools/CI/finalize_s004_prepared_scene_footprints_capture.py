#!/usr/bin/env python3
"""Strict offline finalization for the S004 PreparedEntities authored-footprint traversal."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
import sys
from pathlib import Path
from typing import Any

CAPTURE_DIR = "Logs/SkirmishProduction/s004-prepared-scene-footprints-capture-20261103-r3"
DATA_DIR = CAPTURE_DIR + "/data"
RAW_NAME = "authored-visual-footprints.json"
INVENTORY = "T Logs/SkirmishProduction/s004-prepared-entity-visual-dependency-inventory-20261103-r1/visual-dependency-inventory.json"
AUTHORINGS = "T Logs/SkirmishProduction/s004-prepared-entity-visual-dependency-inventory-20261103-r1/authorings.json"
MESH_DIR = "Logs/SkirmishProduction/s004-prepared-mesh-bounds-capture-20261103-r1"
EXPORTER = "Assets/Game/Scripts/Editor/Skirmish/S004PreparedSceneAuthoredVisualFootprints.cs"
METHOD = "Game.Editor.S004PreparedSceneAuthoredVisualFootprints.CapturePreparedAuthoredVisualFootprints"
MARKER = "[S004PreparedSceneAuthoredVisualFootprints] result=Passed buildings=344"
SCENE = "Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/PreparedEntities.unity"
MAP_ID = "opmap.skirmish.cityedgeairfield_prepared"
COUNT = 344


class FinalizeError(ValueError):
    pass


def need(ok: bool, message: str) -> None:
    if not ok:
        raise FinalizeError(message)


def sha(path: Path) -> str:
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
        raise FinalizeError("source pin escapes project root: " + raw) from exc
    return path


def vector(value: Any, label: str, integers: bool = False) -> tuple[float, ...]:
    need(isinstance(value, list) and len(value) == 2, "malformed " + label)
    if integers:
        need(all(type(v) is int for v in value), "noninteger " + label)
    else:
        need(all(type(v) in (int, float) and math.isfinite(v) for v in value), "nonfinite " + label)
    return tuple(value)


def bounds_vector(obj: dict, key: str, label: str) -> tuple[float, float, float]:
    value = obj.get(key)
    need(isinstance(value, list) and len(value) == 3 and
         all(type(x) in (int, float) and math.isfinite(x) for x in value), "nonfinite or malformed " + label)
    return tuple(float(x) for x in value)


def verify_mesh_capture(root: Path, mesh_dir: Path, inventory_path: Path) -> dict[str, dict]:
    raw_path, log_path, receipt_path = mesh_dir / "mesh-bounds.json", mesh_dir / "editor.log", mesh_dir / "capture-receipt.json"
    need(raw_path.is_file() and log_path.is_file() and receipt_path.is_file(), "qualified mesh-bounds capture evidence incomplete")
    log = log_path.read_text(encoding="utf-8", errors="strict")
    receipt = load_json(receipt_path, "mesh-bounds receipt")
    need(receipt.get("schema") == "s004-prepared-mesh-bounds-capture-receipt-v1" and
         receipt.get("rawExportSha256") == sha(raw_path) and receipt.get("captureLogSha256") == sha(log_path) and
         receipt.get("inventorySha256") == sha(inventory_path) and
         receipt.get("wrapper", {}).get("exitCode") == 0,
         "qualified mesh-bounds receipt is stale or has nonzero wrapper exit")
    raw = load_json(raw_path, "qualified mesh-bounds export")
    need("Batch mode:              NO" in log and "-batchmode" not in log and
         "-executeMethod\nGame.Editor.S004PreparedVisualMeshBoundsCapture.CaptureInventoryMeshBounds" in log and
         "[S004PreparedVisualMeshBoundsCapture] result=Passed mesh_count=" in log and
         "Exiting batchmode successfully now!" in log and "Batchmode quit successfully invoked" in log,
         "qualified mesh-bounds wrapper log evidence mismatch")
    need(raw.get("kind") == "S004PreparedVisualMeshBoundsCapture" and raw.get("status") == "Passed" and
         raw.get("input_inventory_sha256") == sha(inventory_path) and
         receipt.get("uniqueMeshCount") == raw.get("unique_mesh_count") and
         receipt.get("exactInventoryIdentityCoverage") is True and receipt.get("finiteBounds") is True,
         "qualified mesh-bounds export/receipt mismatch")
    return {row["guid"] + ":" + str(row["local_file_id"]): row for row in raw["meshes"]}


def validate_mesh_provenance(rows: list, qualified: dict[str, dict]) -> int:
    seen = set()
    for building in rows:
        local_rows = building.get("mesh_renderer_inputs")
        need(isinstance(local_rows, list) and bool(local_rows), "mesh renderer provenance missing: " + str(building.get("stable_id")))
        for mesh in local_rows:
            guid, local_id = mesh.get("mesh_guid"), mesh.get("mesh_local_file_id")
            key = str(guid) + ":" + str(local_id)
            need(key in qualified, "mesh identity absent from qualified mesh-bounds capture: " + key)
            reference = qualified[key]
            center = bounds_vector(mesh.get("mesh_local_bounds", {}), "center", "mesh local bounds center")
            size = bounds_vector(mesh.get("mesh_local_bounds", {}), "size", "mesh local bounds size")
            need(all(abs(center[i] - reference["bounds_center"][i]) <= 1e-5 for i in range(3)) and
                 all(abs(size[i] - reference["bounds_size"][i]) <= 1e-5 for i in range(3)),
                 "mesh local bounds differ from qualified exact asset capture: " + key)
            need(mesh.get("mesh_asset_path") == reference.get("asset_path"), "mesh asset path differs from qualified capture: " + key)
            seen.add(key)
    return len(seen)


def transform_bounds(center: tuple, size: tuple, matrix: Any) -> list[tuple[float, float, float]]:
    need(isinstance(matrix, list) and len(matrix) == 16 and all(type(x) in (int, float) and math.isfinite(x) for x in matrix),
         "malformed/nonfinite renderer transform")
    lo = [center[i] - size[i] * .5 for i in range(3)]
    hi = [center[i] + size[i] * .5 for i in range(3)]
    points = []
    for x in (lo[0], hi[0]):
        for y in (lo[1], hi[1]):
            for z in (lo[2], hi[2]):
                points.append((matrix[0]*x + matrix[4]*y + matrix[8]*z + matrix[12],
                               matrix[1]*x + matrix[5]*y + matrix[9]*z + matrix[13],
                               matrix[2]*x + matrix[6]*y + matrix[10]*z + matrix[14]))
    return points


def validate_world_bounds(building: dict) -> None:
    all_points = []
    for mesh in building["mesh_renderer_inputs"]:
        center = bounds_vector(mesh["mesh_local_bounds"], "center", "mesh bounds center")
        size = bounds_vector(mesh["mesh_local_bounds"], "size", "mesh bounds size")
        need(all(x >= 0 for x in size), "negative mesh local bounds size")
        all_points.extend(transform_bounds(center, size, mesh.get("renderer_local_to_world")))
    mins = tuple(min(p[i] for p in all_points) for i in range(3))
    maxs = tuple(max(p[i] for p in all_points) for i in range(3))
    bounds = building.get("world_visual_bounds")
    actual_min, actual_max = bounds_vector(bounds, "min", "world bounds min"), bounds_vector(bounds, "max", "world bounds max")
    need(all(abs(a-b) <= 2e-4 for a, b in zip(mins, actual_min)) and
         all(abs(a-b) <= 2e-4 for a, b in zip(maxs, actual_max)),
         "world visual bounds do not follow recorded mesh bounds and transforms")


def verify(log_path: Path, raw_path: Path, inventory_path: Path, authorings_path: Path,
           root: Path, wrapper_exit: int, mesh_dir: Path | None = None) -> dict:
    need(wrapper_exit == 0, "Unity wrapper exit must be exactly zero")
    root = root.resolve()
    log = log_path.read_text(encoding="utf-8", errors="strict")
    markers = [line for line in log.splitlines() if line.startswith(MARKER)]
    need(len(markers) == 1 and str(raw_path.resolve().parent) in markers[0], "expected one exact authored-footprint pass marker/output")
    need("Batch mode:              NO" in log and "-batchmode" not in log and
         "-executeMethod\n" + METHOD in log and str(root) in log and
         "Batchmode quit successfully invoked" in log and "Exiting batchmode successfully now!" in log and
         "cleanup=Failed" not in log,
         "wrapper log method/project/mode/shutdown/cleanup evidence mismatch")
    raw = load_json(raw_path, "authored footprint export")
    inventory = load_json(inventory_path, "visual dependency inventory")
    authors = load_json(authorings_path, "serialized scene authoring census")
    need(raw.get("kind") == "S004PreparedSceneAuthoredVisualFootprints" and raw.get("schema_version") == 1 and
         raw.get("status") == "PassedSourceTraversal" and
         raw.get("scope") == "EditModeAdditiveSceneReadOnly_ExactProjectAuthoredFootprintOwnerAndBakerTraversal",
         "capture kind/status/scope mismatch")
    need(raw.get("scene_path") == SCENE and raw.get("scene_loaded_additively") is True and
         raw.get("scene_mutated_or_saved") is False and raw.get("source_pin_rehash_before_after") is True,
         "scene scope or source-pin lifecycle claim mismatch")
    for key in ("gameplay_world_created", "baker_executed", "scene_fit_claim", "mission_readiness", "visual_readiness"):
        need(raw.get(key) is False, "capture contains forbidden claim: " + key)
    need(inventory.get("kind") == "S004PreparedEntitiesVisualDependencyInventory" and inventory.get("status") == "PassedInventoryOnly" and
         raw.get("inventory_sha256") == sha(inventory_path), "inventory binding mismatch")
    scene_path = project_file(root, SCENE)
    need(raw.get("scene_sha256") == sha(scene_path) == inventory.get("input_scene", {}).get("sha256"), "prepared scene hash differs from inventory/current bytes")
    need(Path(raw.get("inventory_path", "")).resolve() == inventory_path.resolve(), "raw inventory path mismatch")

    pins = raw.get("source_pins")
    need(isinstance(pins, dict) and len(pins) >= 8, "source pin set incomplete")
    for raw_pin, value in pins.items():
        need(re.fullmatch(r"[0-9a-f]{64}", str(value)) is not None, "malformed source pin: " + raw_pin)
        pin = project_file(root, raw_pin)
        need(pin.is_file() and sha(pin) == value, "source pin differs from current file: " + raw_pin)
    exporter_path = project_file(root, EXPORTER)
    exporter_sha = sha(exporter_path)
    source = exporter_path.read_text(encoding="utf-8")
    close_at = source.find("CloseAndRestore(ref ownedScene, setup);")
    write_at = source.find("WriteFreshDirectory(outputDir, report);")
    need(close_at >= 0 and write_at > close_at and "EditorSceneManager.RestoreSceneManagerSetup(setup)" in source and
         "VerifySetup(setup)" in source and ".isDirty" in source and
         "EditorSceneManager.CloseScene(ownedScene, true)" in source and "SaveScene(" not in source,
         "current capture source does not establish fail-closed close/restore before success output")

    rows = raw.get("buildings")
    need(isinstance(rows, list) and len(rows) == COUNT and raw.get("building_count") == COUNT and
         int(markers[0].split("buildings=", 1)[1].split()[0]) == COUNT,
         "exact 344 building count mismatch")
    need(isinstance(authors, list) and len(authors) == COUNT, "authoring inventory does not contain 344 rows")
    expected = {row.get("placement_index"): row for row in authors}
    need(set(expected) == set(range(COUNT)), "serialized authoring placement indices are incomplete/duplicate")
    by_id, seen_indices = {}, set()
    for row in rows:
        index = row.get("placement_index")
        need(type(index) is int and index in range(COUNT) and index not in seen_indices, "duplicate/invalid placement index")
        seen_indices.add(index)
        need(row.get("operation_map_id") == MAP_ID and isinstance(row.get("stable_id"), str) and bool(row["stable_id"]),
             "building identity/map ID invalid")
        need(row["stable_id"] not in by_id, "duplicate stable ID")
        by_id[row["stable_id"]] = row
        original = expected[index]
        need(row["stable_id"] == original.get("stable_id") and row["operation_map_id"] == original.get("operation_map_id") and
             row.get("source_global_object_id", "") == original.get("source_global_object_id", "") and
             row.get("faction_id") == original.get("faction_id") and row.get("blocker_policy") == int(original.get("blocker_policy")),
             "captured identity differs from serialized authoring census: " + str(index))
        serialized_origin = vector(row.get("serialized_origin_cell"), "serialized origin", True)
        serialized_footprint = vector(row.get("serialized_generated_footprint_cells"), "serialized footprint", True)
        expected_origin = (int(original["origin_cell"]["x"]), int(original["origin_cell"]["y"]))
        expected_footprint = (int(original["serialized_generated_footprint_cells"]["x"]), int(original["serialized_generated_footprint_cells"]["y"]))
        need(serialized_origin == expected_origin and serialized_footprint == expected_footprint,
             "serialized origin/footprint differs from prepared scene census: " + str(index))
        baker_input = vector(row.get("baker_input_footprint_cells"), "baker input footprint", True)
        calculated_origin = vector(row.get("calculated_origin_cell"), "calculated origin", True)
        calculated_footprint = vector(row.get("calculated_footprint_cells"), "calculated footprint", True)
        need(all(x > 0 for x in baker_input + calculated_footprint), "nonpositive footprint cells")
        bounds = row.get("world_visual_bounds")
        bmin, bmax = bounds_vector(bounds, "min", "visual bounds min"), bounds_vector(bounds, "max", "visual bounds max")
        need(all(bmin[i] <= bmax[i] for i in range(3)), "invalid visual bounds extent")
        validate_world_bounds(row)
        grid = raw.get("grid")
        origin3 = bounds_vector(grid, "origin", "grid origin")
        cell_size = grid.get("cell_size")
        need(type(cell_size) in (int, float) and math.isfinite(cell_size) and cell_size > 0, "invalid grid cell size")
        calc_x = math.floor((bmin[0] - origin3[0]) / cell_size + .0001)
        calc_z = math.floor((bmin[2] - origin3[2]) / cell_size + .0001)
        max_x = math.ceil((bmax[0] - origin3[0]) / cell_size - .0001)
        max_z = math.ceil((bmax[2] - origin3[2]) / cell_size - .0001)
        need(calculated_origin == (calc_x, calc_z) and
             calculated_footprint == (max(1, max_x-calc_x), max(1, max_z-calc_z)),
             "projected cells differ from exact owner projection formula: " + str(index))
        need(row.get("baker_executed") is False and row.get("visual_traversal") ==
             "EnabledActiveInHierarchyMeshRenderer+MeshFilter.sharedMesh.bounds.TransformPoint(8Corners)",
             "baker execution or visual traversal scope mismatch")
    need(seen_indices == set(range(COUNT)) and len(by_id) == COUNT, "stable ID or dense placement coverage incomplete")

    mesh_dir = mesh_dir or root / MESH_DIR
    qualified_meshes = verify_mesh_capture(root, mesh_dir, inventory_path)
    mesh_identity_count = validate_mesh_provenance(rows, qualified_meshes)
    return {
        "schema": "s004-prepared-scene-footprints-capture-receipt-v1",
        "evidenceClass": "OfflineFinalizationOfReadOnlyAuthoredVisualFootprintTraversal",
        "captureMethod": METHOD,
        "wrapper": {"path": "Tools/CI/invoke_unity_macos.sh", "exitCode": wrapper_exit, "editorMode": "GUI", "batchmode": False},
        "passMarker": markers[0], "captureLog": str(log_path.resolve()), "captureLogSha256": sha(log_path),
        "rawExport": str(raw_path.resolve()), "rawExportSha256": sha(raw_path),
        "inventorySha256": sha(inventory_path), "authoringCensusSha256": sha(authorings_path),
        "sourcePinCount": len(pins), "sourcePinsRehashedCurrent": True,
        "captureExporter": EXPORTER, "captureExporterSha256Current": exporter_sha,
        "sceneSetupCleanupEvidence": "SuccessMarkerEmittedOnlyAfterCloseAndRestore;CurrentSourceOrderVerified",
        "sceneSetupRestored": True, "ownedSceneClosed": True,
        "buildingCount": COUNT, "uniqueStableIdCount": len(by_id), "densePlacementCoverage": True,
        "meshIdentityCount": mesh_identity_count, "qualifiedMeshBoundsReceipt": str(mesh_dir / "capture-receipt.json"),
        "meshBoundsProvenanceVerified": True, "worldBoundsRecomputedFromLocalMeshBoundsAndMatrices": True,
        "projectedCellsRecomputed": True,
        "bakerExecuted": False, "sceneFitClaim": False, "missionReadiness": False, "visualReadiness": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--log", type=Path)
    parser.add_argument("--raw", type=Path)
    parser.add_argument("--inventory", type=Path)
    parser.add_argument("--authorings", type=Path)
    parser.add_argument("--mesh-dir", type=Path)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    parser.add_argument("--capture-dir", type=Path, help="capture directory containing editor.log and data/authored-visual-footprints.json")
    parser.add_argument("--out-dir", type=Path, help="fresh directory for finalized receipt/source files")
    args = parser.parse_args()
    root = args.root.resolve()
    capture = args.capture_dir.resolve() if args.capture_dir else root / CAPTURE_DIR
    out = args.out_dir.resolve() if args.out_dir else capture
    data = capture / "data"
    log = args.log or capture / "editor.log"
    raw = args.raw or data / RAW_NAME
    inventory = args.inventory or root / INVENTORY
    authors = args.authorings or root / AUTHORINGS
    mesh_dir = args.mesh_dir or root / MESH_DIR
    receipt_path, source_path = out / "capture-receipt.json", out / "capture-source.json"
    try:
        need(not receipt_path.exists() and not source_path.exists(), "fresh receipt outputs required")
        receipt = verify(log, raw, inventory, authors, root, args.wrapper_exit, mesh_dir)
        out.mkdir(parents=True, exist_ok=True)
        receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        source_path.write_text(json.dumps({"schema": receipt["schema"], "rawExportSha256": receipt["rawExportSha256"],
                                           "captureExporterSha256Current": receipt["captureExporterSha256Current"],
                                           "inventorySha256": receipt["inventorySha256"]}, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print("[S004PreparedSceneFootprintsFinalizer] result=Passed buildings=344 meshes=" +
              str(receipt["meshIdentityCount"]) + " sceneSetup=Restored baker=0 sceneFit=0 readiness=0")
        return 0
    except (OSError, UnicodeError, json.JSONDecodeError, FinalizeError, TypeError, KeyError, ValueError) as exc:
        print("[S004PreparedSceneFootprintsFinalizer] result=Failed reason=" + str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

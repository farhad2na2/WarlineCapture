"""Qualify source-bound S073 placement-time physical masks after checked Editor completion."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re


INITIAL_FORCES = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset"
CONSTRUCTION = "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset"
S073_PRESET = "Assets/Game/Resources/SkirmishIndustrialBasin.asset"
MASKED_BUILDINGS = {
    "building_barrack": "Building_Barrack",
    "building_ammunition_depot": "Building_Ammunition_Depot",
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def file_sha(path):
    return sha(Path(path).read_bytes())


def _need(condition, message):
    if not condition:
        raise ValueError(message)


def _expected_building_rectangles(legacy, native_catalog, census):
    _need(legacy.get("kind") == "S073LegacyInitialSpawnExternalData" and legacy.get("mission") == "S073",
          "Current S073 legacy config required")
    artifacts = legacy.get("source_artifacts", {})
    config_pins = artifacts.get("configuration_asset_sha256", {})
    census_pins = census.get("source_pins", {})
    initial_sha = config_pins.get(INITIAL_FORCES)
    _need(isinstance(initial_sha, str) and census_pins.get(INITIAL_FORCES) == initial_sha,
          "Same current InitialForces bytes must bind config and same-session census")

    factions = legacy.get("initial_spawn", {}).get("Factions")
    _need(isinstance(factions, list) and len(factions) == 2, "Exact two-faction S073 InitialForces readback required")
    faction_rows = [row for row in factions if row.get("FactionId") == 2]
    _need(len(faction_rows) == 1, "Unique faction-2 startup source required")
    faction = faction_rows[0]
    spawn_x, spawn_y = faction.get("SpawnX"), faction.get("SpawnY")
    _need(isinstance(spawn_x, int) and isinstance(spawn_y, int), "Exact authored faction spawn cell required")
    buildings = faction.get("Buildings")
    _need(isinstance(buildings, list), "Faction-2 authored startup buildings required")
    source_rows = {}
    for row in buildings:
        key = row.get("LookupKey")
        if key in MASKED_BUILDINGS:
            _need(key not in source_rows, "Duplicate source building for road mask observation: " + str(key))
            _need(isinstance(row.get("OriginOffsetX"), int) and isinstance(row.get("OriginOffsetY"), int),
                  "Exact authored startup origin offsets required")
            source_rows[key] = row
    _need(set(source_rows) == set(MASKED_BUILDINGS), "Both source-bound mask target buildings are required")

    _need(native_catalog.get("kind") == "S073ConfiguredCatalogMetadataExport" and
          native_catalog.get("mission") == "S073" and
          native_catalog.get("building_placement_source_path") == CONSTRUCTION and
          native_catalog.get("preset_asset_path") == S073_PRESET and
          native_catalog.get("footprint_authority") == "OriginalBuildingDefinitionMetadata.FootprintCells" and
          native_catalog.get("mission_world_loaded") is False and
          native_catalog.get("native_readiness") is False,
          "Fresh source-only S073 native Construction metadata capture required")
    catalog_rows = native_catalog.get("entries")
    expected_names = ["Building_Barrack", "Building_GuardTower", "Building_Ammunition_Depot",
                      "Building_OilPump", "Building_Refinery", "Building_Fuel_Bladder"]
    _need(isinstance(catalog_rows, list) and [row.get("prefab_name") for row in catalog_rows] == expected_names,
          "Exact ordered six-entry native S073 Construction catalog required")
    by_key = {}
    for row in catalog_rows:
        name = row["prefab_name"]
        footprint = row.get("effective_footprint_cells")
        _need(isinstance(footprint, dict) and type(footprint.get("x")) is int and
              type(footprint.get("y")) is int and footprint["x"] > 0 and footprint["y"] > 0,
              "Fresh native effective FootprintCells required: " + name)
        by_key[name] = (footprint["x"], footprint["y"])

    result = {}
    for lookup, definition_key in MASKED_BUILDINGS.items():
        footprint = by_key.get(definition_key)
        _need(footprint is not None,
              "Current authored footprint definition is missing: " + definition_key)
        row = source_rows[lookup]
        result[lookup] = (
            spawn_x + row["OriginOffsetX"], spawn_y + row["OriginOffsetY"],
            footprint[0], footprint[1],
        )
    return result


def validate(log, census, surface, sidewalk, exit_code, legacy=None, native_catalog=None,
             placement_audit=None, placement_audit_path=None, require_coordinated_capture=False):
    if exit_code != 0:
        raise ValueError("Checked Unity wrapper did not exit successfully")
    census_marker = "[S073PostStreamCensus] result=Passed ecsLoaded=1 ownedGrid=1 entities=4931"
    if log.splitlines().count(census_marker) != 1:
        raise ValueError("Exact required native census marker missing or duplicated")
    post = census.get("post_stream") or {}
    if (census.get("status") != "Passed" or census.get("mission_ready") is not False
            or post.get("phase") != "Playing" or post.get("startup_failure") != "None"
            or post.get("physical_source_validated") != 1 or post.get("entity_count") != 4931
            or post.get("source_identity_hash") != "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900"
            or post.get("source_content_hash") != "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c"):
        raise ValueError("Exact same-session S073 source and committed startup required")
    if require_coordinated_capture and (post.get("static_grid_committed") is not True or
                                        post.get("static_history_bound_count") != 4931):
        raise ValueError("Coordinated capture requires the exact committed same-session static grid")
    if len(surface) != 2048 * 1024 or len(sidewalk) != len(surface) or max(surface) > 1 or max(sidewalk) > 1:
        raise ValueError("Exact row-major bool masks required")

    if legacy is None or native_catalog is None:
        if require_coordinated_capture:
            raise ValueError("Strict coordinated validation requires current legacy config and native S073 catalog")
        expected = {
            "building_barrack": (828, 462, 28, 15),
            "building_ammunition_depot": (867, 462, 19, 16),
        }
    else:
        expected = _expected_building_rectangles(legacy, native_catalog, census)

    if require_coordinated_capture:
        if placement_audit is None or placement_audit_path is None:
            raise ValueError("Strict coordinated validation requires the placement audit artifact")
        lines = placement_audit.decode("utf-8", errors="strict").splitlines()
        if not lines or not lines[0].startswith("phase=Playing fail=None auditPassed="):
            raise ValueError("Placement audit must preserve the same Playing session without startup failure")
        audit_passed = lines[0] == "phase=Playing fail=None auditPassed=True"
        marker = ("[S073RoadCaptureCoordination] result=Passed postStream=1 masks=2 placementAudit=1 "
                  "placementAuditPath={path} placementAuditSha256={sha} missionReady=0").format(
                      path=placement_audit_path, sha=sha(placement_audit))
        if log.splitlines().count(marker) != 1:
            raise ValueError("Exact coordinated completion marker does not bind the supplied placement audit")
    else:
        audit_passed = None

    pattern = re.compile(
        r'^\[S073CurrentRoadMasks\] faction=2 building=(\S+) origin=int2\((\d+), (\d+)\) '
        r'rect=\(x:(\d+), y:(\d+), width:(\d+), height:(\d+)\) cells=(\d+) '
        r'surfaceCells=(\d+) sidewalkCells=(\d+) roadOverlap=(True|False) waterOverlap=(True|False) '
        r'surfaceSha=([0-9a-f]{64}) sidewalkSha=([0-9a-f]{64}) '
        r'scope=OriginalPreSpawnPhysicalInputs_NotReadiness$')
    rows = [pattern.fullmatch(line) for line in log.splitlines() if line.startswith("[S073CurrentRoadMasks]")]
    if len(rows) != 2 or any(row is None for row in rows):
        raise ValueError("Two exact authored startup mask observations required")
    remaining = dict(expected)
    surface_hash, sidewalk_hash = sha(surface), sha(sidewalk)
    for row in rows:
        name, x, y, rx, ry, width, height, count, road_cells, sidewalk_cells, road, water, rh, sh = row.groups()
        x, y, rx, ry, width, height, count, road_cells, sidewalk_cells = map(
            int, (x, y, rx, ry, width, height, count, road_cells, sidewalk_cells))
        actual = (x, y, width, height)
        if name not in remaining or remaining[name] != actual or (rx, ry) != (x, y):
            raise ValueError("Mask request origin/rectangle differs from the current source readbacks: " + name)
        del remaining[name]
        if count != len(surface) or rh != surface_hash or sh != sidewalk_hash:
            raise ValueError("Current placement mask identity or epoch mismatch")
        if road != "False" or water != "False":
            raise ValueError("Authored startup placement still intersects native road/water gate")
        if road_cells != sum(surface[cy * 2048 + cx] for cy in range(y, y + height) for cx in range(x, x + width)):
            raise ValueError("Surface mask differs from native rectangle observation")
        if sidewalk_cells != sum(sidewalk[cy * 2048 + cx] for cy in range(y, y + height) for cx in range(x, x + width)):
            raise ValueError("Sidewalk mask differs from native rectangle observation")
    if remaining:
        raise ValueError("One or more exact source building mask observations are missing")
    if require_coordinated_capture:
        post["placement_audit_passed"] = audit_passed
        post["placement_audit_sha256"] = sha(placement_audit)
    return post


def verify_repo_pins(document, repo, label, field="source_pins"):
    pins = document.get(field) or {}
    if not isinstance(pins, dict) or not pins:
        raise ValueError(label + " source pin map required")
    for raw, digest in pins.items():
        path = Path(raw)
        if not path.is_absolute():
            path = repo / path
        path = path.resolve(strict=True)
        if not path.is_relative_to(repo) or file_sha(path) != digest:
            raise ValueError(label + " source pin drift or escape: " + str(path))
    return pins


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", required=True, type=Path)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    parser.add_argument("--legacy-config", required=True, type=Path)
    parser.add_argument("--native-catalog", required=True, type=Path)
    parser.add_argument("--native-catalog-receipt", required=True, type=Path)
    parser.add_argument("--placement-audit", required=True, type=Path)
    args = parser.parse_args()
    root = args.directory.resolve(strict=True)
    repo = args.repo.resolve(strict=True)
    log_raw = (root / "editor.log").read_bytes()
    census_raw = (root / "census.json").read_bytes()
    census = json.loads(census_raw)
    surface = (root / "road.surface.bin").read_bytes()
    sidewalk = (root / "road.sidewalk.bin").read_bytes()
    legacy_path = args.legacy_config.resolve(strict=True)
    native_catalog_path = Path(os.path.abspath(args.native_catalog))
    native_catalog_receipt_path = Path(os.path.abspath(args.native_catalog_receipt))
    if not native_catalog_path.is_file() or not native_catalog_receipt_path.is_file():
        raise FileNotFoundError("Native catalog artifact and qualified receipt must exist")
    placement_path = args.placement_audit.absolute()
    if not placement_path.is_file():
        raise FileNotFoundError(placement_path)
    legacy = json.loads(legacy_path.read_text(encoding="utf-8"))
    native_catalog_raw = native_catalog_path.read_bytes()
    native_catalog = json.loads(native_catalog_raw)
    native_catalog_receipt = json.loads(native_catalog_receipt_path.read_text(encoding="utf-8"))
    placement_audit = placement_path.read_bytes()

    census_pins = verify_repo_pins(census, repo, "Same-session road census")
    native_pins = verify_repo_pins(native_catalog, repo, "Fresh native S073 Construction metadata",
                                    "source_dependencies_sha256")
    native_catalog_log_path = native_catalog_path.parent / "editor.log"
    expected_catalog_receipt_path = native_catalog_path.parent / "catalog-receipt.json"
    native_catalog_log = native_catalog_log_path.read_text(encoding="utf-8", errors="strict")
    native_catalog_marker = ("[S073CatalogMetadataExport] result=Passed entries=6 footprints=6 path=" +
                             str(native_catalog_path))
    native_catalog_method = "Game.Editor.S073ConfiguredCatalogMetadataExport.RunS073ConfiguredCatalogMetadataExport"
    if (native_catalog_log.splitlines().count(native_catalog_marker) != 1 or
            "COMMAND LINE ARGUMENTS:" not in native_catalog_log or native_catalog_method not in native_catalog_log):
        raise ValueError("Exact current S073 native catalog pass marker and Editor method required")
    if (native_catalog_receipt.get("kind") != "S073ConfiguredCatalogMetadataReceipt" or
            native_catalog_receipt.get("validation_status") != "Passed" or
            native_catalog_receipt.get("wrapper_exit_code") != 0 or
            native_catalog_receipt.get("required_marker_present") is not True or
            native_catalog_receipt.get("mission_ready") is not False or
            native_catalog_receipt.get("catalog_path") != str(native_catalog_path) or
            native_catalog_receipt.get("catalog_sha256") != file_sha(native_catalog_path) or
            native_catalog_receipt_path != expected_catalog_receipt_path or
            native_catalog_receipt.get("editor_log_path") != str(native_catalog_log_path) or
            native_catalog_receipt.get("editor_log_sha256") != file_sha(native_catalog_log_path) or
            native_catalog_receipt.get("footprint_entry_count") != 6 or
            native_catalog_receipt.get("source_pin_count") != len(native_pins) or
            native_catalog_receipt.get("construction_path") != CONSTRUCTION or
            native_catalog_receipt.get("footprint_authority") != "OriginalBuildingDefinitionMetadata.FootprintCells"):
        raise ValueError("Strict fresh S073 native catalog receipt required")
    initial_sha = legacy.get("source_artifacts", {}).get("configuration_asset_sha256", {}).get(INITIAL_FORCES)
    if not initial_sha or file_sha(repo / INITIAL_FORCES) != initial_sha or census_pins.get(INITIAL_FORCES) != initial_sha:
        raise ValueError("Current InitialForces source hash does not bind config and native same-session census")
    construction_sha = native_pins.get(CONSTRUCTION)
    if (native_catalog_receipt.get("construction_sha256") != construction_sha or not construction_sha or
            file_sha(repo / CONSTRUCTION) != construction_sha):
        raise ValueError("Current Construction source hash does not bind the qualified native S073 catalog")
    if native_pins.get(S073_PRESET) != file_sha(repo / S073_PRESET):
        raise ValueError("Current S073 preset source hash does not bind the native S073 catalog")

    post = validate(log_raw.decode("utf-8", errors="strict"), census, surface, sidewalk,
                    args.wrapper_exit, legacy, native_catalog, placement_audit, str(placement_path), True)
    trace = "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs"
    observer = "Assets/Game/Scripts/Editor/Skirmish/S073PrepareMapBoundaryObserver.cs"
    if trace not in census_pins or observer not in census_pins:
        raise ValueError("Capture-time road instrumentation and coordinator pins required")

    result = {
        "kind": "S073PhysicalRoadMaskCaptureReceipt", "validation_status": "Passed",
        "wrapper_exit_code": 0, "required_markers_present": True,
        "source_identity_hash": post["source_identity_hash"], "source_content_hash": post["source_content_hash"],
        "surface_mask_sha256": sha(surface), "sidewalk_mask_sha256": sha(sidewalk),
        "editor_log_sha256": sha(log_raw), "census_sha256": sha(census_raw),
        "legacy_config_path": str(legacy_path), "legacy_config_sha256": file_sha(legacy_path),
        "native_catalog_path": str(native_catalog_path), "native_catalog_sha256": sha(native_catalog_raw),
        "native_catalog_receipt_path": str(native_catalog_receipt_path),
        "native_catalog_receipt_sha256": file_sha(native_catalog_receipt_path),
        "initial_forces_sha256": initial_sha, "construction_sha256": construction_sha,
        "native_footprint_cells": {
            row["prefab_name"]: row["effective_footprint_cells"]
            for row in native_catalog["entries"] if row["prefab_name"] in set(MASKED_BUILDINGS.values())
        },
        "placement_audit_path": str(placement_path), "placement_audit_sha256": sha(placement_audit),
        "placement_audit_passed": post["placement_audit_passed"],
        "source_pins": census_pins, "campaign_gameplay_state_copied": False, "mission_ready": False,
        "scope": "Source-derived current faction-2 startup placement masks; not complete surface geometry, normal-input playthrough, or mission readiness",
    }
    receipt_path = root / "road-receipt.json"
    with receipt_path.open("x", encoding="utf-8") as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write("\n")
    print(json.dumps({key: result[key] for key in (
        "validation_status", "surface_mask_sha256", "sidewalk_mask_sha256", "placement_audit_passed")}, indent=2))


if __name__ == "__main__":
    main()

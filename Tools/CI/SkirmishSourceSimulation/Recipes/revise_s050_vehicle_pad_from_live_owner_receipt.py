#!/usr/bin/env python3
"""Prepare a symmetric S050 vehicle-pad candidate from current source masks and owner receipt.

The source-mask/actor-occupancy calculation is a bounded candidate census only.
It is not the live SpawnCellUtility result, source-fit evidence, or readiness.
"""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / "T Logs/SkirmishProduction"
AUTHOR_REQUEST = BASE / "s050-grid-aligned-deployment-pad-author-replay-20261116-r3/authoring-request.json"
AUTHOR_RESULT = BASE / "s050-grid-aligned-deployment-pad-author-replay-20261116-r3/authoring-result.json"
PHYSICAL = ROOT / "Logs/SkirmishProduction/s050-grid-aligned-physical-capture-20261116-r4/physical-reference.json"
UNIT_METADATA = ROOT / "Logs/SkirmishProduction/s050-grid-aligned-unit-capture-20261116-r3/unit-production-metadata.json"
BUILDING_METADATA = ROOT / "Logs/SkirmishProduction/s050-grid-aligned-building-capture-20261116-r3/building-definitions.json"
RUN_DIR = BASE / "s050-grid-aligned-one-tick-20261116-r1"
RUN_RECEIPT = RUN_DIR / "run-receipt.json"
RUN_REQUEST = RUN_DIR / "run-request.json"
RUN_RESULT = RUN_DIR / "result.json"
RUN_LOG = RUN_DIR / "process.log"
ACTOR_STATE = RUN_DIR / "failure-validator-state.json"
OUTPUT = BASE / "s050-grid-aligned-deployment-pad-layout-20261116-r9"
PRESET = ROOT / "Assets/Game/Resources/SkirmishBaseAssault.asset"
PRESET_SHA = "ff020b7c082ecbede2e0383731f0ca11443b715e8f625336174ebddf89c412fe"
ROLE_PREFABS = {
    "role.rifle": "Unit_Chr_Soldier_Male_02_Alt_04",
    "role.gunner": "Unit_Chr_Soldier_Male_01",
    "role.rocketeer": "Unit_Chr_Ghillie_Male_01",
    "role.car": "Unit_Veh_Light_Armored_Car",
    "role.apc_armored": "Unit_Veh_APC_Slow",
    "role.tank": "Unit_Veh_Tank_USA",
    "role.tanker": "Unit_Veh_Truck_Tanker",
    "role.logistics_truck": "Unit_Veh_Truck_Tray",
}


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def desc(path):
    path = Path(path).resolve(strict=True)
    if path.is_symlink() or not path.is_file() or not path.is_relative_to(ROOT):
        raise ValueError("Repository-owned regular evidence required: " + str(path))
    return {"path": str(path), "sha256": sha(path), "bytes": path.stat().st_size}


def read(path):
    return json.loads(Path(path).read_bytes())


def need(value, message):
    if not value:
        raise ValueError(message)


def read_footprint(fields):
    pair = fields.get("FootprintCells", {}).get("fields", {})
    return (int(pair["UnityEngine.Vector2Int::m_X"]["value"]),
            int(pair["UnityEngine.Vector2Int::m_Y"]["value"]))


def current_physical_masks(capture):
    import base64
    domain = capture.get("headlessPlacementInputs", {})
    masks = {}
    for name in ("gridWalkable", "authoredBuildings"):
        row = domain.get(name)
        need(isinstance(row, dict) and row.get("count") == 440 * 220,
             "Exact 440x220 physical mask required: " + name)
        raw = base64.b64decode(row.get("base64", ""), validate=True)
        need(len(raw) == 440 * 220 and hashlib.sha256(raw).hexdigest() == row.get("sha256"),
             "Current physical mask bytes/hash differ: " + name)
        masks[name] = raw
    need(domain.get("requiredRoadSidewalkMasksComplete") is True,
         "Complete road/sidewalk mask receipt required")
    nav = bytearray(440 * 220)
    for blocker in capture.get("fixedMapReadDomain", {}).get("authoredNavigationBlockers", []):
        components = blocker.get("components", {})
        cell = components.get("Game.Components.UnitGrid", {}).get("value", {}).get("fields", {}).get(
            "Game.Components.UnitGrid::Cell", {}).get("fields", {})
        size = components.get("Game.Components.GridBlockerSize", {}).get("value", {}).get("fields", {})
        x = int(cell["Unity.Mathematics.int2::x"]["value"])
        z = int(cell["Unity.Mathematics.int2::y"]["value"])
        w = int(size["Game.Components.GridBlockerSize::Size"]["fields"]["Unity.Mathematics.int2::x"]["value"])
        h = int(size["Game.Components.GridBlockerSize::Size"]["fields"]["Unity.Mathematics.int2::y"]["value"])
        need(w > 0 and h > 0, "Positive navigation blocker footprint required")
        for zz in range(max(0, z), min(220, z + h)):
            for xx in range(max(0, x), min(440, x + w)):
                nav[zz * 440 + xx] = 1
    masks["navigationBlockers"] = bytes(nav)
    return masks


def actor_occupancy(state, unit_metadata, building_metadata):
    units = {row["key"]: read_footprint(row["definition_metadata"]["fields"])
             for row in unit_metadata.get("units", [])}
    buildings = {row["key"]: read_footprint(row["fields"])
                 for row in building_metadata.get("definitions", [])}
    need(len(units) == len(unit_metadata.get("units", [])) and len(buildings) == 9,
         "Complete direct-owner footprint metadata required")
    occupancy = bytearray(440 * 220)
    actor_keys = {}
    actor_ids = set()
    faction_counts = {1: 0, 2: 0}
    for actor in state.get("actors", []):
        entity = actor.get("entity")
        components = actor.get("components", {})
        key = components.get("Game.Components.UnitSourcePrefabKey", {}).get("Value")
        cell = components.get("Game.Components.UnitGrid", {}).get("Cell")
        faction = actor.get("faction")
        need(isinstance(entity, str) and entity not in actor_ids and key and cell and faction in (1, 2),
             "Exact unique actual actor cell/faction census required")
        actor_ids.add(entity)
        faction_counts[faction] += 1
        footprint = buildings.get(key, units.get(key))
        need(footprint is not None, "Actual state actor lacks direct-owner footprint: " + str(key))
        x, z = int(cell["x"]), int(cell["y"])
        w, h = footprint
        need(0 <= x < 440 and 0 <= z < 220 and w > 0 and h > 0 and x + w <= 440 and z + h <= 220,
             "Actual actor footprint outside current grid")
        for zz in range(z, z + h):
            for xx in range(x, x + w):
                occupancy[zz * 440 + xx] = 1
        actor_keys[(faction, key)] = actor_keys.get((faction, key), 0) + 1
    need(len(actor_ids) == 71 and faction_counts == {1: 36, 2: 35},
         "Exact retained 71-actor pre-failure state required")
    need(actor_keys.get((2, "Unit_Veh_Truck_Tray")) == 1 and
         actor_keys.get((1, "Unit_Veh_Truck_Tray")) == 2,
         "Expected remaining enemy logistics truck after retained failure required")
    return bytes(occupancy), {"actor_count": len(actor_ids), "faction_counts": faction_counts,
                              "occupied_cell_count": sum(occupancy),
                              "actor_prefab_counts": {f"{faction}:{key}": count
                                  for (faction, key), count in sorted(actor_keys.items())}}


def candidate_origin_count(bounds, masks, occupancy, footprint=(3, 3)):
    x0, z0, width, depth = bounds
    walkable, buildings, nav = (masks["gridWalkable"], masks["authoredBuildings"],
                                masks["navigationBlockers"])
    fw, fh = footprint
    count = 0
    for z in range(z0, z0 + depth - fh + 1):
        for x in range(x0, x0 + width - fw + 1):
            cells = [(z + dz) * 440 + x + dx for dz in range(fh) for dx in range(fw)]
            if all(walkable[i] != 0 and buildings[i] == 0 and nav[i] == 0 and occupancy[i] == 0
                   for i in cells):
                count += 1
    return count


def main():
    need(not OUTPUT.exists(), "Fresh candidate output directory required")
    request, result = read(AUTHOR_REQUEST), read(AUTHOR_RESULT)
    physical, unit, building = read(PHYSICAL), read(UNIT_METADATA), read(BUILDING_METADATA)
    run_receipt, run_request = read(RUN_RECEIPT), read(RUN_REQUEST)
    run_result, state = read(RUN_RESULT), read(ACTOR_STATE)
    log = RUN_LOG.read_text(errors="replace")
    layout_pin = request.get("layout", {})
    layout_path = Path(layout_pin.get("path", "")).resolve(strict=True)
    layout = read(layout_path)
    need(request.get("catalog_id") == "S050" and request.get("seed") == 104779 and
         result.get("status") == "ConfigurationCompiled" and result.get("compiler_accepted") is True and
         result.get("resolved_setup", {}).get("setup_hash") == 44596873 and
         result.get("world_allocated") is False and result.get("source_fit_verified") is False and
         request.get("resource_preset") == {"path": str(PRESET), "sha256": PRESET_SHA} and
         sha(PRESET) == PRESET_SHA and result.get("input_pins", {}).get(str(PRESET), {}).get("sha256") == PRESET_SHA,
         "Current exact S050 author result and BaseAssault source selection required")
    need(unit.get("setup_hash") == 44596873 and building.get("setup_hash") == 44596873 and
         unit.get("catalog_id") == building.get("catalog_id") == "S050" and
         unit.get("full_mission_initialized") is False and building.get("full_mission_initialized") is False,
         "Current direct original-owner unit/building footprint outputs required")
    need(run_receipt.get("status") == "Failed" and run_receipt.get("process_exit") == 1 and
         run_receipt.get("timed_out") is False and run_receipt.get("source_result", {}).get("tick") == 2 and
         "role.logistics_truck.2.15.1" in run_receipt.get("source_result", {}).get("error", "") and
         run_result.get("status") == "Failed" and run_result.get("tick") == 2 and
         state.get("tick") == 2 and state.get("owned_count") == 71,
         "Exact retained current enemy logistics failure receipt/state required")
    rejected = ("[S050OpeningPlacement] actor=role.logistics_truck.2.15.1 stage=reject branch=spawn_cell_search_failed "
                "radius=12 census_scope=all_grid_candidate_origins census_out_of_bounds=1316 census_unwalkable=16107 "
                "census_blocked=4798 census_occupied=74522 census_reserved=57 census_available=0")
    need(rejected in log and "actor=role.logistics_truck.2.15.0 stage=placed" in log and
         run_receipt.get("data_bundle", {}).get("sha256") == sha(run_receipt["data_bundle"]["path"]),
         "Exact owner rejected-row census and current input bundle pin required")
    for name, expected in physical.get("readPins", {}).items():
        source = Path(name)
        need(source.is_file() and sha(source) == expected, "Physical capture source pin drift: " + name)
    masks = current_physical_masks(physical)
    occupancy, actor_census = actor_occupancy(state, unit, building)
    spawn_pads = {row.get("RoleId"): row for row in layout.get("pads", [])
                  if row.get("RoleId") in {"spawn.vehicle.player", "spawn.vehicle.enemy"}}
    need(set(spawn_pads) == {"spawn.vehicle.player", "spawn.vehicle.enemy"},
         "Exact symmetric current vehicle spawn pads required")
    source_sites = {}
    new_capacity = {}
    for role, faction in (("spawn.vehicle.player", 1), ("spawn.vehicle.enemy", 2)):
        pad = spawn_pads[role]
        need((pad.get("WidthMetres"), pad.get("DepthMetres")) == (9, 8),
             "Current owner-tested candidate must be exact 9x8 predecessor")
        center_x, center_z = pad["CenterX"], pad["CenterZ"]
        x_edge = center_x - 15 * 0.5
        z_edge = center_z - 16 * 0.5
        x0f, z0f = x_edge - layout["originX"], z_edge - layout["originZ"]
        need(x0f.is_integer() and z0f.is_integer(), "15x16 bounds must align to source grid edges")
        x0, z0 = int(x0f), int(z0f)
        route_cell = [x0 + (15 - 1) // 2, z0 + (16 - 1) // 2]
        prior_center = [center_x, center_z]
        pad["WidthMetres"], pad["DepthMetres"] = 15, 16
        source_sites[role] = {"faction": faction, "route_cell": route_cell,
                              "prior_center_world": prior_center,
                              "prior_width_depth_metres": [9, 8],
                              "new_center_world": [center_x, center_z],
                              "new_width_depth_metres": [15, 16],
                              "bounds_cell_origin": [x0, z0], "bounds_cell_dimensions": [15, 16]}
        old_x0 = round((center_x - 9 * 0.5 - layout["originX"]) / layout["cellSize"])
        old_z0 = round((center_z - 8 * 0.5 - layout["originZ"]) / layout["cellSize"])
        old_bounds = (old_x0, old_z0, 9, 8)
        new_bounds = (x0, z0, 15, 16)
        new_capacity[role] = {
            "previous_9x8_source_mask_and_actor_valid_3x3_origins": candidate_origin_count(old_bounds, masks, occupancy),
            "candidate_15x16_source_mask_and_actor_valid_3x3_origins": candidate_origin_count(new_bounds, masks, occupancy),
            "calculation_scope": "Captured walkability, authored-building mask, authored-navigation mask and reconstructed occupied footprints of 71 actual pre-failure actors; excludes live dynamic blocker/reservation/surface state and is not SpawnCellUtility evidence.",
        }
    need(layout.get("contentVersion") == 3, "Exact prior candidate layout version required")
    layout["contentVersion"] = 4
    layout["contentHash"] = "unqualified.s050.source-candidate.grid-aligned-v4"
    for role, counts in new_capacity.items():
        need(counts["candidate_15x16_source_mask_and_actor_valid_3x3_origins"] >= 48,
             "Symmetric pad candidate lacks the chosen 48-origin source/actor margin")
    OUTPUT.mkdir(parents=True)
    layout_path_out = OUTPUT / "layout.json"
    layout_path_out.write_text(json.dumps(layout, indent=2) + "\n")
    sources = {
        "authoring_request": desc(AUTHOR_REQUEST), "authoring_result": desc(AUTHOR_RESULT),
        "previous_layout": desc(layout_path), "physical_capture": desc(PHYSICAL),
        "unit_metadata": desc(UNIT_METADATA), "building_metadata": desc(BUILDING_METADATA),
        "run_receipt": desc(RUN_RECEIPT), "run_request": desc(RUN_REQUEST),
        "run_result": desc(RUN_RESULT), "process_log": desc(RUN_LOG),
        "failure_actor_state": desc(ACTOR_STATE), "resource_preset": desc(PRESET),
    }
    receipt = {
        "schema_version": 1, "kind": "S050LiveOwnerVehiclePadExpansionCandidate",
        "status": "PreparedSourceOnlyCandidate", "layout": desc(layout_path_out),
        "source_pins": sources, "operation_map_id": "opmap.ch05.citywide_alert",
        "setup_hash_predecessor": 44596873, "same_source_route_sites": source_sites,
        "old_new_vehicle_pad_candidate_capacity": new_capacity,
        "live_owner_failure": {"stable_id": "role.logistics_truck.2.15.1",
            "branch": "spawn_cell_search_failed", "search_radius": 12,
            "all_grid_census": {"out_of_bounds": 1316, "unwalkable": 16107, "blocked": 4798,
                               "occupied": 74522, "reserved": 57, "available": 0},
            "source_derived_map_bound_claim": False,
            "note": "The worker exception path omitted installed-map receipt serialization; map package was pinned and separate source preflight passed, so this is not evidence the map was absent or installed."},
        "layout_change": {"only_fields_changed": ["vehicle pad WidthMetres", "vehicle pad DepthMetres",
                                                   "contentVersion", "contentHash"],
            "old_dimensions_metres": [9, 8], "new_dimensions_metres": [15, 16],
            "center_world_preserved": True, "campaign_geometry_changed": False,
            "force_budget_changed": False, "resource_preset_sha256": PRESET_SHA},
        "approximation_limit": "The candidate-origin counts join captured static source masks with actual pre-failure actor footprints. They omit live dynamic blockers, allocator reservations and surface traversal; only a new actual source-owner run can establish placement.",
        "world_allocated": False, "runtime_admission": False,
        "source_fit_certified": False, "mission_ready": False,
    }
    (OUTPUT / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps({"status": receipt["status"], "layout": receipt["layout"],
                      "candidate_dimensions": [15, 16], "owner_failure": "role.logistics_truck.2.15.1",
                      "candidate_origins": {key: value["candidate_15x16_source_mask_and_actor_valid_3x3_origins"]
                                            for key, value in new_capacity.items()},
                      "source_fit_certified": False, "mission_ready": False}))


if __name__ == "__main__":
    main()

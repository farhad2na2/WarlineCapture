#!/usr/bin/env python3
"""Build an unqualified S050 layout from actual source-only site/path probes.

The output is an external Standard/Regular draft, not an authored Campaign
map, a live placement, a source-fit certificate, or runtime admission.
"""
import argparse
import hashlib
import importlib.util
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / "T Logs/SkirmishProduction/five-mission-pilot-20261106-T1"
INPUTS = BASE / "s050-citywide-source-search-inputs-r1"
SITES = BASE / "s050-citywide-greedy-source-candidate-r1"
PATHS = BASE / "s050-citywide-original-movement-mask-path-probe-r1"
CROSS = BASE / "s050-citywide-path-crosscheck-r1"
SPEEDS = BASE / "s074-provisional-source-path-layout-r2/receipt.json"
COMPRESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/extend_s074_candidate_with_source_paths.py"
S050_AUTHOR_RESULT = ROOT / "T Logs/SkirmishProduction/s050-grid-aligned-deployment-pad-author-replay-20261116-r2/authoring-result.json"
S050_AUTHOR_REQUEST = ROOT / "T Logs/SkirmishProduction/s050-grid-aligned-deployment-pad-author-replay-20261116-r2/authoring-request.json"
S050_UNIT_METADATA = ROOT / "T Logs/SkirmishProduction/s050-grid-aligned-audited-owner-projections-20261116-r1/unit-production-metadata.json"
S050_PHYSICAL_CAPTURE = ROOT / "Logs/SkirmishProduction/s050-grid-aligned-physical-capture-20261116-r3/physical-reference.json"
S050_PREVIOUS_OWNER_RUN = ROOT / "T Logs/SkirmishProduction/s050-grid-aligned-source-host-one-tick-20261116-r3/run-receipt.json"
S050_PREVIOUS_OWNER_LOG = ROOT / "T Logs/SkirmishProduction/s050-grid-aligned-source-host-one-tick-20261116-r3/process.log"
S050_PREVIOUS_BUNDLE = ROOT / "T Logs/SkirmishProduction/s050-grid-aligned-source-only-bundle-20261116-r4/data-bundle.json"
ROLE_CATEGORIES = {
    "role.rifle": ("infantry", "Unit_Chr_Soldier_Male_02_Alt_04"),
    "role.gunner": ("infantry", "Unit_Chr_Soldier_Male_01"),
    "role.rocketeer": ("infantry", "Unit_Chr_Ghillie_Male_01"),
    "role.car": ("vehicle", "Unit_Veh_Light_Armored_Car"),
    "role.apc_armored": ("vehicle", "Unit_Veh_APC_Slow"),
    "role.tank": ("vehicle", "Unit_Veh_Tank_USA"),
    "role.tanker": ("vehicle", "Unit_Veh_Truck_Tanker"),
    "role.logistics_truck": ("vehicle", "Unit_Veh_Truck_Tray"),
}
STRUCTURES = {
    "Building_Barrack": ("building.barracks", 1),
    "Building_Ammunition_Depot": ("building.fabrication_depot", 7),
    "Building_OilPump": ("building.oil_pump", 7),
    "Building_Refinery": ("building.refinery", 7),
    "Building_Fuel_Bladder": ("building.fuel_bladder", 7),
    "Building_GroundStaging": ("building.ground_staging", 2),
    "Building_Satelite_Dish": ("building.satellite_dish", 8),
    "Building_Helipad": ("building.helipad", 6),
}


def digest(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def desc(path):
    path = Path(path).resolve(strict=True)
    return {"path": str(path), "sha256": digest(path)}


def descriptor_matches(actual, expected):
    """Compare exact path and bytes hash, plus byte count when the receipt carries it."""
    if not isinstance(actual, dict) or not isinstance(expected, dict):
        return False
    try:
        path = Path(expected["path"]).resolve(strict=True)
        return (actual.get("path") == str(path) and actual.get("sha256") == digest(path) and
                expected.get("sha256") == digest(path) and
                ("bytes" not in actual or actual["bytes"] == path.stat().st_size) and
                ("bytes" not in expected or expected["bytes"] == path.stat().st_size))
    except (KeyError, OSError, TypeError):
        return False


def grid_aligned_pad_center(world_cell_origin, footprint):
    """Convert the source finder's lower-left cell origin to a center coordinate."""
    if (not isinstance(world_cell_origin, (list, tuple)) or len(world_cell_origin) != 2 or
            not isinstance(footprint, (list, tuple)) or len(footprint) != 2 or
            any(not isinstance(v, (int, float)) or not math.isfinite(v) for v in world_cell_origin) or
            any(not isinstance(v, int) or v <= 0 for v in footprint)):
        raise ValueError("Finite source lower-left origin and positive integer footprint required")
    x, z = world_cell_origin
    width, depth = footprint
    return [x + width * 0.5, z + depth * 0.5]


def grid_aligned_deployment_pad_center(route_cell, width, depth, positive_z_extra_cells=0):
    """Center a source-cell-window pad, allowing only an explicit +Z extension."""
    if (not isinstance(route_cell, (list, tuple)) or len(route_cell) != 2 or
            any(not isinstance(v, int) or v < 0 for v in route_cell) or
            not isinstance(width, int) or width <= 0 or not isinstance(depth, int) or depth <= 0 or
            not isinstance(positive_z_extra_cells, int) or positive_z_extra_cells < 0):
        raise ValueError("Integer route cell, positive pad dimensions, and nonnegative +Z extension required")
    x, z = route_cell
    x0, z0 = x - (width - 1) // 2, z - (depth - 1) // 2
    return [860.0 + x0 + width * 0.5,
            300.0 + z0 + (depth + positive_z_extra_cells) * 0.5]


def audit_previous_vehicle_placement_failure(author_request):
    receipt = json.loads(S050_PREVIOUS_OWNER_RUN.read_bytes())
    run_request = json.loads((S050_PREVIOUS_OWNER_RUN.parent / "run-request.json").read_bytes())
    log_text = S050_PREVIOUS_OWNER_LOG.read_text(errors="replace")
    data_bundle = desc(S050_PREVIOUS_BUNDLE)
    expected_placements = (
        "role.car.1.3.0 stage=placed cell=72,126",
        "role.apc_armored.1.4.0 stage=placed cell=69,127",
        "role.tank.1.5.0 stage=placed cell=75,124",
        "role.car.2.9.0 stage=placed cell=318,126",
        "role.apc_armored.2.10.0 stage=placed cell=321,124",
        "role.tank.2.11.0 stage=placed cell=321,128",
        "role.tanker.1.12.0 stage=placed cell=75,127",
        "role.logistics_truck.1.13.0 stage=placed cell=69,124",
    )
    rejected = ("role.logistics_truck.1.13.1 stage=reject branch=spawn_cell_search_failed radius=12 "
                "census_scope=all_grid_candidate_origins census_out_of_bounds=1316 census_unwalkable=16107 "
                "census_blocked=4798 census_occupied=74526 census_reserved=53 census_available=0")
    expected_layout = author_request.get("layout")
    if (not isinstance(expected_layout, dict) or desc(expected_layout.get("path", "")) != expected_layout or
            receipt.get("status") != "Failed" or receipt.get("process_exit") != 1 or
            receipt.get("timed_out") is not False or receipt.get("full_outcome_observed") is not False or
            receipt.get("source_fit_certified") is not False or receipt.get("mission_ready") is not False or
            not descriptor_matches(receipt.get("data_bundle"), data_bundle) or not descriptor_matches(run_request.get("initialization"),
                desc(ROOT / "T Logs/SkirmishProduction/s050-grid-aligned-source-only-bundle-20261116-r4/initialization.json")) or
            not descriptor_matches(run_request.get("source_derived_map"),
                desc(ROOT / "T Logs/SkirmishProduction/s050-grid-aligned-source-map-package-20261116-r2/map-package.json")) or
            receipt.get("source_result", {}).get("tick") != 2 or
            "Original opening placement failed role.logistics_truck.1.13.1" not in receipt.get("source_result", {}).get("error", "") or
            any(f"[S050OpeningPlacement] actor={row}" not in log_text for row in expected_placements) or
            f"[S050OpeningPlacement] actor={rejected}" not in log_text):
        raise ValueError("Exact retained S050 owner vehicle-pad failure evidence required")
    return {
        "run_receipt": desc(S050_PREVIOUS_OWNER_RUN), "process_log": desc(S050_PREVIOUS_OWNER_LOG),
        "prior_bundle": data_bundle, "prior_layout": expected_layout,
        "prior_layout_fingerprint": author_request.get("layout", {}).get("sha256"),
        "successful_3x3_vehicle_placements_before_failure": list(expected_placements),
        "failed_player_3x3_vehicle_placement": rejected,
        "failure_scope": "Actual source-owner spawn-cell search on 2026-11-16; startup abort at tick 2; no enemy support vehicles reached.",
        "source_fit_certified": False, "mission_ready": False,
    }


def read_current_deployment_requirements():
    """Read the current compiler-selected S050 force/prefab facts."""
    author = json.loads(S050_AUTHOR_RESULT.read_bytes())
    request = json.loads(S050_AUTHOR_REQUEST.read_bytes())
    metadata = json.loads(S050_UNIT_METADATA.read_bytes())
    if (author.get("catalog_id") != "S050" or author.get("seed") != 104779 or
            author.get("compiler_accepted") is not True or author.get("resolved_setup", {}).get("setup_hash") != 1275857659 or
            author.get("mission_ready") is not False or author.get("source_fit_verified") is not False or
            metadata.get("catalog_id") != "S050" or metadata.get("seed") != 104779 or
            metadata.get("setup_hash") != 1275857659 or metadata.get("native_S050_capture") is not False or
            metadata.get("native_visual_readiness") is not False or not isinstance(metadata.get("units"), list)):
        raise ValueError("Exact current S050/104779 author and direct-owner unit facts required")
    preset = request.get("resource_preset")
    preset_path = str(ROOT / "Assets/Game/Resources/SkirmishBaseAssault.asset")
    expected_preset_sha = "ff020b7c082ecbede2e0383731f0ca11443b715e8f625336174ebddf89c412fe"
    if (not isinstance(preset, dict) or preset.get("path") != preset_path or preset.get("sha256") != expected_preset_sha or
            digest(preset_path) != expected_preset_sha or author.get("input_pins", {}).get(preset_path, {}).get("sha256") != expected_preset_sha):
        raise ValueError("Exact hash-pinned BaseAssault resource preset is required")
    by_key = {x.get("key"): x for x in metadata["units"]}
    requirements = {k: {"per_faction_count": 0, "counts_by_faction": {1: 0, 2: 0}, "footprints": [], "max_role_quantity": 0}
                    for k in ("infantry", "vehicle", "air")}
    for force in author["resolved_setup"].get("forces", []):
        role, faction, quantity = force.get("role_id"), force.get("faction_id"), force.get("quantity")
        if role not in ROLE_CATEGORIES or faction not in (1, 2) or not isinstance(quantity, int) or quantity <= 0:
            raise ValueError("Unsupported or malformed current compiler force row")
        category, prefab_key = ROLE_CATEGORIES[role]
        fields = by_key.get(prefab_key, {}).get("definition_metadata", {}).get("fields", {})
        footprint = fields.get("FootprintCells", {}).get("fields", {})
        width = footprint.get("UnityEngine.Vector2Int::m_X", {}).get("value")
        depth = footprint.get("UnityEngine.Vector2Int::m_Y", {}).get("value")
        if width is None or depth is None:
            raise ValueError("Direct owner footprint absent for compiler-selected force prefab: " + prefab_key)
        req = requirements[category]
        req["counts_by_faction"][faction] += quantity
        req["per_faction_count"] = max(req["counts_by_faction"].values())
        req["footprints"].append([int(width), int(depth)])
        req["max_role_quantity"] = max(req["max_role_quantity"], quantity)
    for kind, count, footprint in (("infantry", 20, [1, 1]), ("vehicle", 6, [3, 3]), ("air", 0, None)):
        req = requirements[kind]
        if (req["counts_by_faction"] != {1: count, 2: count} or
                any(row != footprint for row in req["footprints"])):
            raise ValueError("Current S050 deployment count/footprints changed: " + kind)
    failure_evidence = audit_previous_vehicle_placement_failure(request)
    return requirements, {
        "authoring_request": desc(S050_AUTHOR_REQUEST), "authoring_result": desc(S050_AUTHOR_RESULT),
        "resource_preset": desc(preset_path), "unit_metadata": desc(S050_UNIT_METADATA),
        "unit_owner_authority": metadata.get("authority"), "setup_hash": author["resolved_setup"]["setup_hash"],
        "layout_fingerprint": author.get("layout_fingerprint"), "mission_ready": False,
        "source_fit_verified": False, "layout_binding_requires_recompile": True,
    }, failure_evidence


def decode_current_source_masks():
    import base64
    capture = json.loads(S050_PHYSICAL_CAPTURE.read_bytes())
    data = capture.get("headlessPlacementInputs", {})
    out = {}
    for name in ("gridWalkable", "authoredBuildings", "authoredRoadsIncludingSidewalks", "sidewalks"):
        row = data.get(name)
        if not isinstance(row, dict) or row.get("count") != 440 * 220:
            raise ValueError("Current source capture mask missing: " + name)
        raw = base64.b64decode(row.get("base64", ""), validate=True)
        if len(raw) != 440 * 220 or hashlib.sha256(raw).hexdigest() != row.get("sha256"):
            raise ValueError("Current source capture mask hash mismatch: " + name)
        out[name] = raw
    nav = bytearray(440 * 220)
    domain = capture.get("fixedMapReadDomain", {})
    if (domain.get("operationMapId") != "opmap.ch05.citywide_alert" or domain.get("missionReady") is not False or
            domain.get("sourceFitCertified") is not False):
        raise ValueError("Source-only fixed Campaign map domain required")
    for row in domain.get("authoredNavigationBlockers", []):
        components = row.get("components", {})
        cell = components.get("Game.Components.UnitGrid", {}).get("value", {}).get("fields", {}).get(
            "Game.Components.UnitGrid::Cell", {}).get("fields", {})
        size = components.get("Game.Components.GridBlockerSize", {}).get("value", {}).get("fields", {})
        try:
            x = int(cell["Unity.Mathematics.int2::x"]["value"])
            z = int(cell["Unity.Mathematics.int2::y"]["value"])
            sx = int(size["Game.Components.GridBlockerSize::Size"]["fields"]["Unity.Mathematics.int2::x"]["value"])
            sz = int(size["Game.Components.GridBlockerSize::Size"]["fields"]["Unity.Mathematics.int2::y"]["value"])
        except (KeyError, TypeError, ValueError):
            raise ValueError("Complete captured navigation blocker footprint required")
        if sx <= 0 or sz <= 0 or sx > 440 or sz > 220:
            raise ValueError("Captured navigation blocker footprint invalid")
        for yy in range(max(0, z), min(220, z + sz)):
            for xx in range(max(0, x), min(440, x + sx)): nav[yy * 440 + xx] = 1
    if data.get("requiredRoadSidewalkMasksComplete") is not True:
        raise ValueError("Complete captured authored-road and sidewalk masks required")
    return (out["gridWalkable"], out["authoredBuildings"], out["authoredRoadsIncludingSidewalks"],
            out["sidewalks"], bytes(nav), desc(S050_PHYSICAL_CAPTURE))


def pad_window_counts(center_cell, width, depth, walkable, buildings, roads_and_sidewalks, sidewalks, nav):
    cx, cz = center_cell
    x0, z0 = cx - (width - 1) // 2, cz - (depth - 1) // 2
    if x0 < 0 or z0 < 0 or x0 + width > 440 or z0 + depth > 220:
        return None
    indices = [(z0 + dz) * 440 + x0 + dx for dz in range(depth) for dx in range(width)]
    return {"area_cells": width * depth,
            "walkable_cells": sum(walkable[i] != 0 for i in indices),
            "authored_building_cells": sum(buildings[i] != 0 for i in indices),
            "authored_road_sidewalk_cells": sum(roads_and_sidewalks[i] != 0 for i in indices),
            "sidewalk_cells": sum(sidewalks[i] != 0 for i in indices),
            "navigation_blocker_cells": sum(nav[i] != 0 for i in indices),
            "available_1x1_source_cells": sum(walkable[i] != 0 and buildings[i] == 0 and nav[i] == 0 for i in indices)}


def choose_deployment_sites(routes, requirements, previous_pads, walkable, buildings, roads_and_sidewalks, sidewalks, nav):
    """Select only path cells from the actual path probe with a clear candidate pad window."""
    chosen, alignment = {}, []
    for kind, pad_kind, width, depth in (("infantry", 3, 15, 11), ("vehicle", 4, 9, 8)):
        route = routes[kind]
        cells = [tuple(row) for row in route["cells"]]
        if len(cells) < 2: raise ValueError("Original source route cells required: " + kind)
        selected = {}
        for faction, side, sequence in ((1, "player", cells), (2, "enemy", list(reversed(cells)))):
            choice = None
            for distance, cell in enumerate(sequence):
                counts = pad_window_counts(cell, width, depth, walkable, buildings, roads_and_sidewalks, sidewalks, nav)
                required_cells = requirements[kind]["per_faction_count"] * (1 if kind == "infantry" else 9)
                if (counts is None or counts["authored_building_cells"] != 0 or counts["navigation_blocker_cells"] != 0 or
                        counts["available_1x1_source_cells"] < required_cells):
                    continue
                if kind == "vehicle" and width // 3 * (depth // 3) < requirements[kind]["per_faction_count"]:
                    continue
                choice = (cell, distance, counts); break
            if choice is None: raise ValueError("No source path cell has clear candidate pad capacity: " + kind + "/" + side)
            selected[faction] = choice
            x, z = grid_aligned_deployment_pad_center(choice[0], width, depth)
            previous_pad = previous_pads.get(f"spawn.{kind}.{side}")
            if not isinstance(previous_pad, dict):
                raise ValueError("Exact previous owner-authoring spawn pad required: " + kind + "/" + side)
            previous_center = [previous_pad["CenterX"], previous_pad["CenterZ"]]
            alignment.append({"kind": "deployment", "faction": faction, "family": kind,
                "route_source_cell": list(choice[0]), "distance_along_original_source_path": choice[1],
                "old_center_world": previous_center, "new_center_world": [x, z],
                "old_width_depth_metres": [previous_pad["WidthMetres"], previous_pad["DepthMetres"]],
                "new_width_depth_metres": [width, depth], "required_entities_per_faction": requirements[kind]["per_faction_count"],
                "source_grid_extension_toward_positive_z_metres": 1 if kind == "vehicle" else 0,
                "minimum_owner_observed_capacity_rationale": ("The current 9x7m pad failed at the sixth player-side 3x3 vehicle request after five exact owner placements. This candidate extends the same source-selected region one 1m row toward +Z to expose a larger search region. It does not increase the simple non-overlapping 3x3 rectangular tiling count (six), and it is not measured fit." if kind == "vehicle" else "No vehicle-capacity change."),
                "required_unit_footprints_cells": sorted({tuple(fp) for fp in requirements[kind]["footprints"]}),
                "candidate_source_mask_counts": choice[2], "source_fit_certified": False, "mission_ready": False,
                "rationale": ("Pad bounds begin at the original selected route-cell window and extend one 1m grid row toward +Z; its new center shifts +0.5m while remaining bounded at that source-selected site. The larger search region is a diagnostic candidate only; its geometry does not by itself establish a seventh or any additional non-overlapping 3x3 slot, nor live fit." if kind == "vehicle" else "Pad center is an original path cell center; integer-aligned bounds avoid captured authored-building and navigation cells and offer candidate source-walkable area for the compiler roster. Not runtime fit.")})
        start_cell, goal_cell = selected[1][0], selected[2][0]
        start_index = cells.index(start_cell)
        goal_index = len(cells) - 1 - list(reversed(cells)).index(goal_cell)
        if start_index >= goal_index: raise ValueError("Chosen deployment sites reverse/collapse the original path")
        chosen[kind] = {"player": start_cell, "enemy": goal_cell, "route_cells": cells[start_index:goal_index + 1],
                        "pad_kind": pad_kind, "width": width, "depth": depth}
    return chosen, alignment


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    output = args.output.resolve()
    if not output.is_relative_to(ROOT):
        raise ValueError("Repository-owned output required")
    input_data = json.loads((INPUTS / "request.json").read_bytes())
    graph_path = Path(input_data["graph"]["path"])
    graph = json.loads(graph_path.read_bytes())
    site_receipt = json.loads((SITES / "receipt.json").read_bytes())
    sites = json.loads((SITES / "candidate-world-sites.json").read_bytes())
    path_receipt = json.loads((PATHS / "receipt.json").read_bytes())
    path_result = json.loads((PATHS / "result.json").read_bytes())
    cross = json.loads((CROSS / "receipt.json").read_bytes())
    speed_receipt = json.loads(SPEEDS.read_bytes())
    if (site_receipt.get("found") != 20 or site_receipt.get("mission_ready") is not False or
            sites.get("owner_receipt_sha256") != digest(SITES / "receipt.json") or
            sites.get("owner_result_sha256") != digest(SITES / "result.json") or
            path_receipt.get("result_sha256") != digest(PATHS / "result.json") or
            cross.get("vehicle_path_unchanged") is not True or cross.get("infantry_on_vehicle_path_valid") is not True or
            cross.get("mission_ready") is not False or speed_receipt.get("mission_ready") is not False):
        raise ValueError("Exact unqualified original-owner site and path evidence required")
    for key in ("source_capture", "map", "surface", "graph", "footprint_metadata"):
        if desc(input_data[key]["path"]) != input_data[key]:
            raise ValueError("Campaign input changed: " + key)
    for name, expected in graph["source_pins"].items():
        if digest(name) != expected:
            raise ValueError("Graph source changed: " + name)
    for evidence in (site_receipt, path_receipt, cross):
        for name, expected in evidence["input_pins"].items():
            if digest(name) != expected:
                raise ValueError("Original owner input changed: " + name)
    nodes = [x for x in graph["nodes"] if x.get("source_path") == "Assets/Game/GeneratedOperationMaps/CH05M01CitywideAlert/Definition.asset"]
    if len(nodes) != 1 or nodes[0]["fields"]["operationMapId"] != "opmap.ch05.citywide_alert":
        raise ValueError("Exact current Campaign operation map required")
    fields = nodes[0]["fields"]
    candidate_routes = {r["kind"]: r for r in path_result["routes"]}
    if set(candidate_routes) != {"infantry", "vehicle"} or any(x["status"] != "Found" for x in candidate_routes.values()):
        raise ValueError("Both actual source-only path results required")
    deployment_requirements, deployment_requirement_pins, owner_placement_failure_evidence = read_current_deployment_requirements()
    previous_author_request = json.loads(S050_AUTHOR_REQUEST.read_bytes())
    if desc(previous_author_request["layout"]["path"]) != previous_author_request["layout"]:
        raise ValueError("Previous author replay layout pin is stale")
    previous_layout = json.loads(Path(previous_author_request["layout"]["path"]).read_bytes())
    previous_spawn_pads = {row.get("RoleId"): row for row in previous_layout.get("pads", [])
                           if row.get("RoleId", "").startswith("spawn.")}
    walkable_mask, authored_building_mask, authored_road_sidewalk_mask, sidewalk_mask, navigation_blocker_mask, physical_capture_pin = decode_current_source_masks()
    deployment_sites, deployment_alignment = choose_deployment_sites(
        candidate_routes, deployment_requirements, previous_spawn_pads, walkable_mask, authored_building_mask,
        authored_road_sidewalk_mask, sidewalk_mask, navigation_blocker_mask)
    for kind, selected in deployment_sites.items():
        candidate_routes[kind]["start"] = list(selected["player"])
        candidate_routes[kind]["goal"] = list(selected["enemy"])
        candidate_routes[kind]["cells"] = [list(cell) for cell in selected["route_cells"]]
    spec = importlib.util.spec_from_file_location("source_path_compression", COMPRESS)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    speeds = speed_receipt["movement_speed_inputs"]
    infantry_speed, vehicle_speed = speeds["infantry"], speeds["historical_tank"]
    if any(not math.isfinite(v) or v <= 0 for v in (infantry_speed, vehicle_speed)):
        raise ValueError("Positive source-prefab speed estimates required")
    anchors, pads, bindings, gridAlignment = [], [], [], list(deployment_alignment)

    def anchor(identifier, role, x, z):
        if not (860 <= x <= 1300 and 300 <= z <= 520):
            raise ValueError("Provisional anchor outside Campaign source")
        anchors.append({"AnchorId": identifier, "RoleId": role, "NormalizedU": (x - 860) / 440.0,
                        "NormalizedV": (z - 300) / 220.0, "WorldX": x, "WorldZ": z})

    source_anchors = {x["anchorId"]: x for x in fields["anchors"]}
    deployments = {}
    for faction, side, source_id in ((1, "player", "anchor.ch05.m01.clinic"), (2, "enemy", "anchor.ch05.m01.utility")):
        position = source_anchors[source_id]["position"]
        deployments[faction] = [position["x"], position["z"]]
        anchor("s050.deployment." + str(faction), "base." + side, position["x"], position["z"])
    tower_seen = {1: 0, 2: 0}
    for site in sites["sites"]:
        faction, key, (x, z), (width, depth) = site["faction"], site["building_key"], site["world_origin"], site["footprint"]
        center_x, center_z = grid_aligned_pad_center((x, z), (width, depth))
        if key == "Building_GuardTower":
            tower_seen[faction] += 1
            sid, kind = ("building.watchtower" if tower_seen[faction] == 1 else "building.watchtower.approach"), 8
        else:
            sid, kind = STRUCTURES[key]
        side = "player" if faction == 1 else "enemy"
        stem = "s050.f" + str(faction) + "." + sid.removeprefix("building.")
        role = ({"building.ground_staging": "staging.", "building.oil_pump": "economy.",
                 "building.watchtower": "service.", "building.helipad": "air."}.get(sid) or "")
        role = role + side if role else stem
        anchor(stem + ".anchor", role, center_x, center_z)
        pads.append({"PadId": stem + ".pad", "AnchorId": stem + ".anchor", "RoleId": role, "Kind": kind,
                     "CenterX": center_x, "CenterZ": center_z, "WidthMetres": width, "DepthMetres": depth, "FactionId": faction})
        gridAlignment.append({"faction": faction, "building_key": key, "source_cell_origin_world": [x, z],
            "footprint_cells": [width, depth], "previous_center": [x, z],
            "grid_aligned_center": [center_x, center_z],
            "reason": "Center equals exact lower-left cell origin plus half footprint; exact integer cell rectangle is contained by equal-size pad."})
        bindings.append({"SizeId": 1, "DifficultyId": 2, "FactionId": faction, "StructureId": sid,
                         "InstanceOrdinal": 0, "AnchorId": stem + ".anchor", "PadId": stem + ".pad"})
    if len(bindings) != 20 or len({(x["FactionId"], x["StructureId"]) for x in bindings}) != 20:
        raise ValueError("Exact Standard structure domain required")
    for kind in ("infantry", "vehicle"):
        selected = deployment_sites[kind]
        for faction, side in ((1, "player"), (2, "enemy")):
            cell = selected[side]
            x, z = grid_aligned_deployment_pad_center(cell, selected["width"], selected["depth"])
            stem, role = "s050.provisional.spawn." + kind + "." + side, "spawn." + kind + "." + side
            anchor(stem + ".anchor", role, x, z)
            pads.append({"PadId": stem + ".pad", "AnchorId": stem + ".anchor", "RoleId": role, "Kind": selected["pad_kind"],
                         "CenterX": x, "CenterZ": z, "WidthMetres": selected["width"], "DepthMetres": selected["depth"], "FactionId": faction})
    for faction, side, cell in ((1, "player", candidate_routes["infantry"]["start"]), (2, "enemy", candidate_routes["infantry"]["goal"])):
        x, z = cell[0] + 860.5, cell[1] + 300.5
        stem = "s050.provisional.rally." + side
        anchor(stem + ".anchor", "rally." + side, x, z)
        pads.append({"PadId": stem + ".pad", "AnchorId": stem + ".anchor", "RoleId": "rally." + side,
                     "Kind": 5, "CenterX": x, "CenterZ": z, "WidthMetres": 1, "DepthMetres": 1, "FactionId": faction})
    routes, estimates = [], []
    for kind, route_kind in (("vehicle", 1), ("infantry", 2)):
        original = candidate_routes[kind]["cells"]
        if original[0] != candidate_routes[kind]["start"] or original[-1] != candidate_routes[kind]["goal"]:
            raise ValueError("Original source path endpoints differ")
        cells = module.remove_collinear_cells(original)
        ids = []
        for index, (x, z) in enumerate(cells):
            name = "s050.provisional.route." + kind + "." + str(index)
            anchor(name, name, x + 860.5, z + 300.5)
            ids.append(name)
        length = sum(math.hypot(q[0] - p[0], q[1] - p[1]) for p, q in zip(cells, cells[1:]))
        if length <= 0:
            raise ValueError("Positive original source path length required")
        routes.append({"RouteId": "s050.provisional.route." + kind, "WaypointAnchorIds": ids, "Kind": route_kind,
                       "LengthMetres": length, "InfantryTransitSeconds": length / infantry_speed,
                       "GroundTransitSeconds": length / vehicle_speed if kind == "vehicle" else 0.0,
                       "AirTransitSeconds": 0.0, "InfantryFirstContactSeconds": 0.0, "GroundFirstContactSeconds": 0.0,
                       "ProvisionalTimes": True, "WidthMetres": 3.0 if kind == "vehicle" else 1.0,
                       "AllowsVehicles": kind == "vehicle", "AllowsAircraft": False})
        estimates.append({"kind": kind, "length_metres": length, "source_cell_count": len(original),
                          "turn_point_count": len(cells), "first_contact_seconds": "unknown; zero placeholder"})
    layout = {"layoutId": "layout.s050.source-candidate.standard.regular", "operationMapId": fields["operationMapId"],
              "contentVersion": 3, "contentHash": "unqualified.s050.source-candidate.grid-aligned-v3", "worldWidthMetres": 440.0,
              "worldDepthMetres": 220.0, "originX": 860.0, "originZ": 300.0, "cellSize": 1.0,
              "anchors": anchors, "routes": routes, "pads": pads,
              "worldBinding": {"OriginX": 860.0, "OriginZ": 410.0, "ForwardX": 1.0, "ForwardZ": 0.0,
                               "AcrossX": 0.0, "AcrossZ": 1.0, "ForwardMetres": 440.0, "AcrossMetres": 220.0,
                               "PlayerAnchorId": "s050.deployment.1", "EnemyAnchorId": "s050.deployment.2",
                               "SourceHash": fields["contentHash"], "HasMeasuredDeployments": False, "BindAllConfigurations": False},
              "bindingValueFormat": 2, "declaredConfigurations": [{"SizeId": 1, "DifficultyId": 2,
                  "PlayerDeployment": {"x": deployments[1][0], "y": deployments[1][1]},
                  "EnemyDeployment": {"x": deployments[2][0], "y": deployments[2][1]}}],
              "structureInstancePads": bindings}
    for evidence in (site_receipt, path_receipt, cross):
        for name, expected in evidence["input_pins"].items():
            if digest(name) != expected:
                raise ValueError("Original owner input changed during construction: " + name)
    output.mkdir(parents=True, exist_ok=False)
    path = output / "layout.json"
    path.write_text(json.dumps(layout, indent=2) + "\n")
    receipt = {"kind": "S050CitywideProvisionalLayout", "status": "PreparedSourceOnlyCandidate", "layout": desc(path),
               "source_graph": desc(graph_path), "site_receipt": desc(SITES / "receipt.json"),
               "path_receipt": desc(PATHS / "receipt.json"), "crosscheck_receipt": desc(CROSS / "receipt.json"),
               "movement_speed_receipt": desc(SPEEDS), "builder": desc(Path(__file__)), "route_estimates": estimates,
               "starting_pad_grid_alignment": gridAlignment,
               "deployment_requirement_pins": deployment_requirement_pins,
               "owner_placement_failure_evidence": owner_placement_failure_evidence,
               "physical_source_capture": physical_capture_pin,
               "deployment_requirements": deployment_requirements,
               "deployment_scope": "Source-derived logical candidate only; latest author result has prior layout fingerprint, so setup must be recompiled before any current binding, capture, package, or run.",
               "timing_scope": "Distance divided by captured prefab speed; first contact unknown, zero placeholder; no timing observed",
               "source_limit": "Other static blockers, active water, native occupancy and complete actor placement remain unknown",
               "runtime_admission": False, "source_fit_certified": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps({"status": receipt["status"], "structures": len(bindings), "routes": len(routes), "mission_ready": False}))


if __name__ == "__main__":
    main()

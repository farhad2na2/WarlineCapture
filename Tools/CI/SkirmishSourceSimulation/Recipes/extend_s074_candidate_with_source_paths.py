#!/usr/bin/env python3
"""Add explicitly provisional source-path inputs to the S074 candidate.

The paths were checked on a conservative retained pregame grid, without live
actors or a published S074 world. Timings are estimates, not observations.
"""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / "T Logs/SkirmishProduction/five-mission-pilot-20261106-T1"
PATH_RUN = BASE / "s074-conservative-original-path-probe-r2"
CROSS = BASE / "s074-conservative-path-crosscheck-r1"
CAPTURE = ROOT / "Logs/SkirmishProduction/s003-resource-shared-prefab-capture-20261106-r1/data/prefab-reference-data.json"
TANK_CAPTURE = ROOT / "Logs/private-candidates/s026-physical-project-r1/Logs/SkirmishProduction/s026-prefab-reference-r3/prefab-reference-data.json"


def digest(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def desc(path):
    path = Path(path).resolve(strict=True)
    return {"path": str(path), "sha256": digest(path)}


def speed(capture, key):
    entity_id = capture["opening_prefabs"][key]
    matches = [x for x in capture["entities"] if x["id"] == entity_id]
    if len(matches) != 1:
        raise ValueError("Exact captured movement prefab required: " + key)
    field = matches[0]["components"]["Game.Components.UnitMove"]["value"]["fields"]["Game.Components.UnitMove::Speed"]
    if field.get("codec") != "f32-bits-v1":
        raise ValueError("Exact float movement value required")
    value = struct.unpack("!f", bytes.fromhex(field["bits"]))[0]
    if not math.isfinite(value) or value <= 0:
        raise ValueError("Positive finite movement speed required")
    return value


def remove_collinear_cells(cells):
    """Remove only interior points on the same forward line; never shortcut a turn."""
    points = []
    for cell in cells:
        if not isinstance(cell, list) or len(cell) != 2 or any(type(value) is not int for value in cell):
            raise ValueError("Exact integer source path cells required")
        if points and points[-1] == cell:
            raise ValueError("Duplicate original path point")
        points.append(cell)
        while len(points) >= 3:
            a, b, c = points[-3:]
            ux, uz = b[0] - a[0], b[1] - a[1]
            vx, vz = c[0] - b[0], c[1] - b[1]
            if ux * vz != uz * vx:
                break
            if ux * vx + uz * vz <= 0:
                raise ValueError("Duplicate or backtracking original path segment")
            points.pop(-2)
    if len(points) < 2 or len(points) > 64:
        raise ValueError("Original enemy-policy waypoint limit requires 2..64 exact turn points")
    return points


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--layout", required=True, type=Path,
                        help="Corrected source-origin candidate and its sibling receipt")
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    layout_path = args.layout.resolve(strict=True)
    if not layout_path.is_relative_to(ROOT):
        raise ValueError("Repository-owned input required")
    output = args.output.resolve()
    if not output.is_relative_to(ROOT):
        raise ValueError("Repository-owned output required")
    layout = copy.deepcopy(json.loads(layout_path.read_bytes()))
    if layout.get("contentHash") != "unqualified.s074.source-origin-centers.v1":
        raise ValueError("Stale S074 layout uses uncorrected site origins; regenerate layout and route evidence")
    layout_receipt = json.loads((layout_path.parent / "receipt.json").read_bytes())
    route_receipt = json.loads((PATH_RUN / "receipt.json").read_bytes())
    cross_receipt = json.loads((CROSS / "receipt.json").read_bytes())
    if (layout_receipt["layout"] != desc(layout_path) or route_receipt["result_sha256"] != digest(PATH_RUN / "result.json") or
            cross_receipt["result_sha256"] != digest(CROSS / "result.json") or
            cross_receipt.get("vehicle_path_unchanged") is not True or
            cross_receipt.get("infantry_on_vehicle_path_valid") is not True):
        raise ValueError("Exact original source path and infantry crosscheck required")
    for receipt in (route_receipt, cross_receipt):
        if receipt.get("mission_ready") is not False:
            raise ValueError("Diagnostic scope must remain unqualified")
        for path, expected in receipt["input_pins"].items():
            if digest(path) != expected:
                raise ValueError("Original path input changed: " + path)
    if route_receipt["input_pins"].get(str(CAPTURE)) != digest(CAPTURE) or route_receipt["input_pins"].get(str(TANK_CAPTURE)) != digest(TANK_CAPTURE):
        raise ValueError("Movement prefab evidence changed")
    current, historic = json.loads(CAPTURE.read_bytes()), json.loads(TANK_CAPTURE.read_bytes())
    infantry_speed = speed(current, "Unit_Chr_Soldier_Male_02_Alt_04")
    vehicle_speed = speed(historic, "Unit_Veh_Tank_USA")
    data = json.loads((PATH_RUN / "result.json").read_bytes())
    routes = {r["kind"]: r for r in data["routes"]}
    if set(routes) != {"infantry", "vehicle"} or any(r["status"] != "Found" for r in routes.values()):
        raise ValueError("Actual returned candidate paths required")
    if layout["routes"] or any(p["Kind"] in (3, 4, 5) for p in layout["pads"]):
        raise ValueError("Previously unfilled candidate route and force-pad domain required")

    def add_anchor(identifier, role, point):
        x, z = point
        if not (740 <= x < 990 and 420 <= z < 660):
            raise ValueError("Candidate point leaves authored playable rectangle")
        layout["anchors"].append({"AnchorId": identifier, "RoleId": role, "NormalizedU": x / 2048.0,
                                  "NormalizedV": z / 1024.0, "WorldX": x, "WorldZ": z})

    for kind, pad_kind in (("infantry", 3), ("vehicle", 4)):
        route = routes[kind]
        for faction, side, point in ((1, "player", route["start"]), (2, "enemy", route["goal"])):
            stem = "s074.provisional.spawn." + kind + "." + side
            role = "spawn." + kind + "." + side
            add_anchor(stem + ".anchor", role, point)
            width = 1 if kind == "infantry" else 3
            layout["pads"].append({"PadId": stem + ".pad", "AnchorId": stem + ".anchor", "RoleId": role,
                                   "Kind": pad_kind, "CenterX": point[0], "CenterZ": point[1],
                                   "WidthMetres": width, "DepthMetres": width, "FactionId": faction})
    for faction, side, point in ((1, "player", routes["infantry"]["start"]), (2, "enemy", routes["infantry"]["goal"])):
        stem = "s074.provisional.rally." + side
        add_anchor(stem + ".anchor", "rally." + side, point)
        layout["pads"].append({"PadId": stem + ".pad", "AnchorId": stem + ".anchor", "RoleId": "rally." + side,
                               "Kind": 5, "CenterX": point[0], "CenterZ": point[1],
                               "WidthMetres": 1, "DepthMetres": 1, "FactionId": faction})
    estimates = []
    for kind, route_kind in (("vehicle", 1), ("infantry", 2)):
        route = routes[kind]
        raw_cells = route["cells"]
        if len(raw_cells) < 2 or raw_cells[0] != route["start"] or raw_cells[-1] != route["goal"]:
            raise ValueError("Original path endpoints changed")
        cells = remove_collinear_cells(raw_cells)
        ids = []
        for i, point in enumerate(cells):
            stem = "s074.provisional.route." + kind + "." + str(i)
            add_anchor(stem, stem, point)
            ids.append(stem)
        length = sum(math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(cells, cells[1:]))
        if not math.isfinite(length) or length <= 0:
            raise ValueError("Positive source path length required")
        infantry_estimate = length / infantry_speed
        vehicle_estimate = length / vehicle_speed if kind == "vehicle" else 0.0
        layout["routes"].append({"RouteId": "s074.provisional.route." + kind, "WaypointAnchorIds": ids,
                                 "Kind": route_kind, "LengthMetres": length,
                                 "InfantryTransitSeconds": infantry_estimate, "GroundTransitSeconds": vehicle_estimate,
                                 "AirTransitSeconds": 0.0, "InfantryFirstContactSeconds": 0.0,
                                 "GroundFirstContactSeconds": 0.0, "ProvisionalTimes": True,
                                 "WidthMetres": 3.0 if kind == "vehicle" else 1.0,
                                 "AllowsVehicles": kind == "vehicle", "AllowsAircraft": False})
        estimates.append({"kind": kind, "original_cell_count": len(raw_cells), "turn_point_count": len(cells),
                          "length_metres": length, "infantry_seconds": infantry_estimate,
                          "vehicle_seconds": vehicle_estimate, "first_contact_seconds": "unknown; zero placeholder"})
    # Recheck all explicit proof inputs after preparing the candidate.
    for receipt in (route_receipt, cross_receipt):
        for path, expected in receipt["input_pins"].items():
            if digest(path) != expected:
                raise ValueError("Original path input changed: " + path)
    output.mkdir(parents=True, exist_ok=False)
    path = output / "layout.json"
    path.write_text(json.dumps(layout, indent=2) + "\n")
    receipt = {"kind": "S074ProvisionalPathLayout", "status": "PreparedSourceOnlyCandidate", "layout": desc(path),
               "base_layout": desc(layout_path), "path_receipt": desc(PATH_RUN / "receipt.json"),
               "crosscheck_receipt": desc(CROSS / "receipt.json"), "builder": desc(Path(__file__)),
               "movement_speed_inputs": {"infantry": infantry_speed, "historical_tank": vehicle_speed},
               "route_estimates": estimates, "unit_pad_scope": "Single footprint checks, not complete roster placement",
               "timing_scope": "Distance divided by captured prefab speed; first contact unknown, zero placeholder; no timing observed",
               "route_kind_scope": "Required typed enum labels; no physical highway or ruins claim",
               "site_change_scope": "Historical paths were not rechecked against corrected building positions",
               "runtime_admission": False, "source_fit_certified": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps({"status": receipt["status"], "routes": len(layout["routes"]), "new_pads": 6, "mission_ready": False}))


if __name__ == "__main__":
    main()

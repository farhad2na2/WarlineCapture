#!/usr/bin/env python3
"""Translate a pinned source-site search into an incomplete S074 logical layout.

The candidate contains no routes or unit-spawn pads. It is not a fit, runtime,
native, or player-readiness certificate and does not change Campaign geometry.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / "T Logs/SkirmishProduction/five-mission-pilot-20261106-T1"
SEARCH = BASE / "s074-standard-greedy-source-candidate-r1"
GRAPH = BASE / "s074-industrial-source-graph-r1/source-config-graph.json"
MAP = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"
EXISTING = {
    "building_barrack": ("building.barracks", 1),
    "building_ammunition_depot": ("building.fabrication_depot", 7),
    "building_oilpump": ("building.oil_pump", 7),
    "building_refinery": ("building.refinery", 7),
    "building_fuel_bladder": ("building.fuel_bladder", 7),
}
ADDITIONAL = {
    "ground_staging": ("building.ground_staging", 2),
    "satellite_dish": ("building.satellite_dish", 8),
    "watchtower": ("building.watchtower", 8),
    "watchtower_approach": ("building.watchtower.approach", 8),
    "helipad": ("building.helipad", 6),
}


def digest(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def descriptor(path):
    path = Path(path).resolve(strict=True)
    return {"path": str(path), "sha256": digest(path)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    output = args.output.resolve()
    if not output.is_relative_to(ROOT):
        raise ValueError("Repository-owned output required")
    receipt_path, result_path, request_path = (SEARCH / n for n in ("receipt.json", "result.json", "request.json"))
    receipt, result, request, graph = (json.loads(p.read_bytes()) for p in (receipt_path, result_path, request_path, GRAPH))
    if (receipt.get("status") != "SourceSearchReturned" or receipt.get("additional_found") != 10 or
            receipt.get("additional_requested") != 10 or receipt.get("result_sha256") != digest(result_path) or
            receipt.get("mission_ready") is not False or result.get("mission_ready") is not False):
        raise ValueError("Actual completed source-only site search required")
    for name, expected in receipt["input_pins"].items():
        if digest(name) != expected:
            raise ValueError("Candidate source changed: " + name)
    if receipt["input_pins"].get(str(request_path)) != digest(request_path):
        raise ValueError("Original search request changed")
    for name, expected in graph["source_pins"].items():
        if digest(name) != expected:
            raise ValueError("Graph source changed: " + name)
    nodes = [n for n in graph["nodes"] if n.get("source_path") == MAP]
    if len(nodes) != 1:
        raise ValueError("One actual Industrial Basin map required")
    fields = nodes[0]["fields"]
    actual = fields["anchors"]
    deployment = {x["factionId"]: x["position"] for x in actual if x["kind"] == 3 and x["factionId"] in (1, 2)}
    if len(deployment) != 2 or fields["operationMapId"] != "opmap.skirmish.industrial_basin":
        raise ValueError("Exact authored faction deployments required")
    existing, requested, searched = request["existing"], request["items"], result["rows"]
    if len(existing) != 10 or len(requested) != 10 or len(searched) != 10:
        raise ValueError("Exact Standard candidate cardinality required")
    structures = []
    for item in existing:
        sid, kind = EXISTING[item["id"]]
        structures.append((item["faction"], sid, kind, item["x"], item["z"], item["w"], item["h"]))
    for item, row in zip(requested, searched):
        if (row["found"] is not True or row["faction"] != item["faction"] or row["role"] != item["role"] or
                row["footprint"] != [item["w"], item["h"]] or
                row["blocks_cells"] != (item["role"] != "helipad")):
            raise ValueError("Source search and exact requested structure differ")
        sid, kind = ADDITIONAL[item["role"]]
        structures.append((item["faction"], sid, kind, row["origin"][0], row["origin"][1], item["w"], item["h"]))
    if len({(f, sid) for f, sid, *_ in structures}) != 20:
        raise ValueError("Exactly one of each canonical structure per faction required")
    anchor_rows, pads, bindings = [], [], []

    def anchor(identifier, role, x, z):
        anchor_rows.append({"AnchorId": identifier, "RoleId": role, "NormalizedU": x / 2048.0,
                            "NormalizedV": z / 1024.0, "WorldX": x, "WorldZ": z})

    for faction, role in ((1, "base.player"), (2, "base.enemy")):
        p = deployment[faction]
        anchor("s074.deployment." + str(faction), role, p["x"], p["z"])
    for faction, sid, kind, x, z, width, depth in structures:
        stem = "s074.f" + str(faction) + "." + sid.removeprefix("building.")
        # The source search validates RectInt origins. Typed pads and anchors
        # are centers; retain half-cell centers for odd footprints.
        center_x, center_z = x + width * 0.5, z + depth * 0.5
        if sid == "building.ground_staging":
            role = "staging.player" if faction == 1 else "staging.enemy"
        elif sid == "building.oil_pump":
            role = "economy.player" if faction == 1 else "economy.enemy"
        elif sid == "building.watchtower":
            role = "service.player" if faction == 1 else "service.enemy"
        elif sid == "building.helipad":
            role = "air.player" if faction == 1 else "air.enemy"
        else:
            role = stem
        anchor(stem + ".anchor", role, center_x, center_z)
        pads.append({"PadId": stem + ".pad", "AnchorId": stem + ".anchor", "RoleId": role,
                     "Kind": kind, "CenterX": center_x, "CenterZ": center_z, "WidthMetres": width,
                     "DepthMetres": depth, "FactionId": faction})
        bindings.append({"SizeId": 1, "DifficultyId": 2, "FactionId": faction, "StructureId": sid,
                         "InstanceOrdinal": 0, "AnchorId": stem + ".anchor", "PadId": stem + ".pad"})
    layout = {"layoutId": "layout.s074.source-candidate.standard.regular", "operationMapId": fields["operationMapId"],
              "contentVersion": 1, "contentHash": "unqualified.s074.source-origin-centers.v1",
              "worldWidthMetres": 2048.0, "worldDepthMetres": 1024.0, "originX": 0.0, "originZ": 0.0,
              "cellSize": 1.0, "anchors": anchor_rows, "routes": [], "pads": pads,
              "worldBinding": {"OriginX": 0.0, "OriginZ": 512.0, "ForwardX": 1.0, "ForwardZ": 0.0,
                               "AcrossX": 0.0, "AcrossZ": 1.0, "ForwardMetres": 2048.0, "AcrossMetres": 1024.0,
                               "PlayerAnchorId": "s074.deployment.1", "EnemyAnchorId": "s074.deployment.2",
                               "SourceHash": fields["contentHash"], "HasMeasuredDeployments": False,
                               "BindAllConfigurations": False},
              "bindingValueFormat": 2,
              "declaredConfigurations": [{"SizeId": 1, "DifficultyId": 2,
                                          "PlayerDeployment": {"x": deployment[1]["x"], "y": deployment[1]["z"]},
                                          "EnemyDeployment": {"x": deployment[2]["x"], "y": deployment[2]["z"]}}],
              "structureInstancePads": bindings}
    for name, expected in receipt["input_pins"].items():
        if digest(name) != expected:
            raise ValueError("Search evidence changed: " + name)
    output.mkdir(parents=True, exist_ok=False)
    path = output / "layout.json"
    path.write_text(json.dumps(layout, indent=2) + "\n")
    summary = {"kind": "S074ProvisionalTypedLayout", "status": "PreparedIncompleteSourceCandidate",
               "layout": descriptor(path), "search_receipt": descriptor(receipt_path), "source_graph": descriptor(GRAPH),
               "builder": descriptor(Path(__file__)), "structure_bindings": len(bindings), "route_count": 0,
               "unit_spawn_pads_supplied": False, "runtime_admission": False, "source_fit_certified": False,
               "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(summary, indent=2) + "\n")
    print(json.dumps({"status": summary["status"], "structure_bindings": len(bindings), "routes": 0, "mission_ready": False}))


if __name__ == "__main__":
    main()

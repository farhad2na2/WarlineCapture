#!/usr/bin/env python3
"""Build an isolated, unqualified S026 Standard/Regular route data draft.

The command requires a fresh output directory and refuses to overwrite files.
It validates both pinned input hashes and waypoint envelope bounds before writing.
No Unity/runtime execution or acceptance claim is made by this recipe.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
from typing import Any


PARTIAL_REL = Path(
    "Logs/SkirmishProduction/s026-standard-rally-pad-draft-20261116-r1/partial-layout-candidate.json"
)
WAYPOINT_REL = Path(
    "Logs/SkirmishProduction/s026-r41-owner-waypoint-draft-20261116-r1/candidates.json"
)
PARTIAL_SHA256 = "72f3eb39a6914f8b5cb41d45429e5a0279405cd4030df42d5fb425efb1a9cb4a"
WAYPOINT_SHA256 = "a83d57acfd2a97eb4c70f87480654edbf5cd5a5f0360f637e10f79819a422ac4"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"FAIL: {message}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output-dir",
        required=True,
        type=Path,
        help="new directory to create; existing directories are rejected",
    )
    parser.add_argument(
        "--project-root",
        type=Path,
        default=Path(__file__).resolve().parents[5],
        help="project root (defaults to the checked-in recipe's project)",
    )
    args = parser.parse_args()
    root = args.project_root.resolve()
    output_dir = args.output_dir.resolve()
    require(not output_dir.exists(), f"output directory already exists: {output_dir}")

    partial_path = root / PARTIAL_REL
    waypoint_path = root / WAYPOINT_REL
    require(partial_path.is_file(), f"missing pinned partial layout: {partial_path}")
    require(waypoint_path.is_file(), f"missing pinned waypoint draft: {waypoint_path}")
    partial_hash, waypoint_hash = sha256(partial_path), sha256(waypoint_path)
    require(partial_hash == PARTIAL_SHA256, f"partial layout hash mismatch: {partial_hash}")
    require(waypoint_hash == WAYPOINT_SHA256, f"waypoint draft hash mismatch: {waypoint_hash}")

    partial = json.loads(partial_path.read_text(encoding="utf-8"))
    waypoint_data = json.loads(waypoint_path.read_text(encoding="utf-8"))
    layout = partial["layoutCandidate"]["candidateLayout"]
    require(partial["identity"]["catalogId"] == "S026", "unexpected mission catalog ID")
    require(partial["identity"]["size"] == "Standard", "unexpected size")
    require(partial["identity"]["difficulty"] == "Regular", "unexpected difficulty")
    require(layout["operationMapId"] == "opmap.skirmish.city_crossroads", "unexpected alias map")
    require(layout["routes"] == [], "pinned partial candidate unexpectedly contains route rows")
    require(waypoint_data["kind"] == "S026R41OwnerWaypointCandidates", "unexpected waypoint draft kind")
    require(waypoint_data["status"] == "UnqualifiedDraft", "waypoint draft is not unqualified")

    binding = layout["worldBinding"]
    require(
        (layout["originX"], layout["originZ"], layout["worldWidthMetres"], layout["worldDepthMetres"])
        == (900.0, 260.0, 400.0, 615.0),
        "unexpected source envelope",
    )
    require(
        (
            binding["OriginX"], binding["OriginZ"], binding["ForwardX"], binding["ForwardZ"],
            binding["AcrossX"], binding["AcrossZ"], binding["ForwardMetres"], binding["AcrossMetres"],
        )
        == (900.0, 567.5, 1.0, 0.0, 0.0, 1.0, 400.0, 615.0),
        "unexpected source world binding",
    )

    chain_by_name = {
        (chain["candidate"], chain["roleId"]): chain for chain in waypoint_data["chains"]
    }
    chains = [chain_by_name[(name, "role.tank")] for name in ("TankPrimary", "RefinedChain")]
    for chain in chains:
        require(chain["footprintCells"] == [3, 3], f"unexpected tank fixture for {chain['candidate']}")
        for x, z in chain["worldCellCentersXZ"]:
            require(
                layout["originX"] <= x <= layout["originX"] + layout["worldWidthMetres"]
                and layout["originZ"] <= z <= layout["originZ"] + layout["worldDepthMetres"],
                f"waypoint outside pinned envelope: {(x, z)}",
            )

    output = json.loads(json.dumps(layout))
    output["layoutId"] = "layout.skirmish.s026.standard.regular.provisional-routes.unqualified"
    output["contentVersion"] = 1
    anchors: list[dict[str, Any]] = output["anchors"]
    shared = [anchor for anchor in anchors if anchor.get("RoleId") == "candidate.route.waypoint"]
    require(len(shared) == 3, "expected three retained shared waypoint roles")
    for index, anchor in enumerate(shared):
        anchor["RoleId"] = f"candidate.route.shared.waypoint.{index:02d}"
    for faction, role in ((1, "staging.player"), (2, "staging.enemy")):
        pads = [p for p in output["pads"] if p.get("FactionId") == faction and p.get("Kind") == 2]
        require(len(pads) == 1, f"expected one ground staging pad for faction {faction}")
        matches = [a for a in anchors if a.get("AnchorId") == pads[0]["AnchorId"]]
        require(len(matches) == 1 and not any(a.get("RoleId") == role for a in anchors),
                f"unique unused {role} binding required")
        matches[0]["RoleId"] = role
    # The original setup compiler resolves each shared facility category by
    # role before the instance bindings select the authored structure pads.
    # Bind those roles to the first existing typed pad for that category; do not
    # move or add any facility geometry.
    for faction, suffix in ((1, "player"), (2, "enemy")):
        for kind, category in ((7, "economy"), (8, "service"), (6, "air")):
            pads = [p for p in output["pads"] if p.get("FactionId") == faction and p.get("Kind") == kind]
            require(pads, f"missing {category} pad for faction {faction}")
            matches = [a for a in anchors if a.get("AnchorId") == pads[0]["AnchorId"]]
            role = f"{category}.{suffix}"
            require(len(matches) == 1 and not any(a.get("RoleId") == role for a in anchors),
                    f"unique unused {role} binding required")
            matches[0]["RoleId"] = role
    known_anchor_ids = {anchor["AnchorId"] for anchor in anchors}
    routes: list[dict[str, Any]] = []

    for index, chain in enumerate(chains):
        name = chain["candidate"]
        points = chain["worldCellCentersXZ"]
        anchor_ids: list[str] = []
        route_label = "primary" if index == 0 else "west_refined"
        for point_index, (x, z) in enumerate(points):
            anchor_id = f"anchor.s026.standard.regular.route.tank.{route_label}.{point_index:02d}"
            if anchor_id not in known_anchor_ids:
                anchors.append(
                    {
                        "AnchorId": anchor_id,
                        "RoleId": f"candidate.route.tank.{route_label}.waypoint.{point_index:02d}",
                        "NormalizedU": (x - layout["originX"]) / layout["worldWidthMetres"],
                        "NormalizedV": (z - layout["originZ"]) / layout["worldDepthMetres"],
                        "WorldX": x,
                        "WorldZ": z,
                        "HypothesisOnly": True,
                    }
                )
                known_anchor_ids.add(anchor_id)
            anchor_ids.append(anchor_id)

        length = sum(
            math.hypot(points[i + 1][0] - points[i][0], points[i + 1][1] - points[i][1])
            for i in range(len(points) - 1)
        )
        routes.append(
            {
                "RouteId": f"route.s026.standard.regular.tank.{route_label}",
                "WaypointAnchorIds": anchor_ids,
                # Schema enum: MainHighway=1; FlankNorthRuins=2. These are provisional labels.
                "Kind": 1 if index == 0 else 2,
                "LengthMetres": round(length, 3),
                "InfantryTransitSeconds": round(length / 4.0, 3),
                "GroundTransitSeconds": round(length / 8.0, 3),
                "AirTransitSeconds": round(length / 16.0, 3),
                "InfantryFirstContactSeconds": round(length / 2.0 / 4.0, 3),
                "GroundFirstContactSeconds": round(length / 2.0 / 8.0, 3),
                "ProvisionalTimes": True,
                "WidthMetres": 12.0 if index == 0 else 6.0,
                "AllowsVehicles": True,
                "AllowsAircraft": False,
            }
        )
    output["routes"] = routes

    output_dir.mkdir(parents=True, exist_ok=False)
    layout_path = output_dir / "layout.json"
    layout_path.write_text(json.dumps(output, indent=2, sort_keys=False) + "\n", encoding="utf-8")
    receipt = {
        "schema": 1,
        "kind": "S026StandardRegularProvisionalRouteDataDraft",
        "status": "UnqualifiedDraft",
        "output": "layout.json",
        "outputSha256": sha256(layout_path),
        "sourcePins": {
            PARTIAL_REL.as_posix(): partial_hash,
            WAYPOINT_REL.as_posix(): waypoint_hash,
        },
        "mapFrame": {
            "originX": layout["originX"],
            "originZ": layout["originZ"],
            "worldWidthMetres": layout["worldWidthMetres"],
            "worldDepthMetres": layout["worldDepthMetres"],
            "worldBinding": binding,
        },
        "routeAssumptions": {
            "fixtureRole": "role.tank",
            "fixtureFootprintCells": [3, 3],
            "speedMetresPerSecond": {"infantry": 4.0, "ground": 8.0, "air": 16.0},
            "provisionalTimes": True,
            "intendedWidthTargetsMetres": {"primary": 12.0, "westRefined": 6.0},
            "widthsAreMeasured": False,
            "firstContactAssumedAtHalfPathLength": True,
        },
        "routeSummary": [
            {
                "routeId": route["RouteId"],
                "kind": route["Kind"],
                "lengthMetres": route["LengthMetres"],
                "groundTransitSeconds": route["GroundTransitSeconds"],
                "widthTargetMetres": route["WidthMetres"],
                "waypointCount": len(route["WaypointAnchorIds"]),
            }
            for route in routes
        ],
        "claims": {
            "compilerAccepted": False,
            "routeProof": False,
            "reachability": False,
            "sourceFit": False,
            "normalMissionReady": False,
            "widthsMeasured": False,
            "runtimeExecuted": False,
        },
        "limitations": [
            "Route waypoint chains are unqualified R41 hypotheses; the chain carries probe-role metadata that is not an acceptance signal.",
            "Source frame copied from Rally partial layout; no runtime projection or occupancy validation performed.",
            "12 m and 6 m widths are intended design targets only, never measured.",
            "Transit times are arithmetic estimates using existing SkirmishMapLayoutBuilder assumptions (infantry 4 m/s, ground 8 m/s, air 16 m/s); first contact assumes half route length.",
            "The separate output preserves pinned partial layout values and adds route-only anchors and two route rows. No pinned source or gameplay asset is modified.",
            "No Unity, LFS, compiler, gameplay, or acceptance validation was run.",
        ],
    }
    (output_dir / "receipt.json").write_text(
        json.dumps(receipt, indent=2, sort_keys=False) + "\n", encoding="utf-8"
    )
    print("PASS pinned hashes, source frame, route inputs, and waypoint bounds")
    print("PASS wrote unqualified two-route draft; compiler/runtime/readiness claims remain false")
    print(f"OUTPUT {layout_path}")
    print(f"OUTPUT_SHA256 {receipt['outputSha256']}")


if __name__ == "__main__":
    main()

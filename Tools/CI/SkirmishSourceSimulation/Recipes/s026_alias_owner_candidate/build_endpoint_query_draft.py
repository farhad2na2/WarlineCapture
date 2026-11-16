#!/usr/bin/env python3
"""Build an unexecutable request draft from pinned S026 r41 candidate data.

This is data preparation only: it does not create runtime entities, run pathfinding,
or claim that any endpoint is reachable or that S026 is ready.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[5]
R41_REL = Path("Logs/SkirmishProduction/s026-citycrossroads-alias-source-diagnostic-20261116-r41/report.json")
R41_SHA256 = "9bc633854d9dbed1b8ab2247e635f48ab3bacf89ec38734f6187bd49e3a343f6"
RALLY_REL = Path("Logs/SkirmishProduction/s026-standard-rally-pad-draft-20261116-r1/partial-layout-candidate.json")
RALLY_SHA256 = "72f3eb39a6914f8b5cb41d45429e5a0279405cd4030df42d5fb425efb1a9cb4a"
RALLY_RECEIPT_REL = Path("Logs/SkirmishProduction/s026-standard-rally-pad-draft-20261116-r1/receipt.json")
RALLY_RECEIPT_SHA256 = "3fdf25226708c086a732bc949329477c80cb382f6121f8fcf32f8af578867beb"
OUTPUT_REL = Path("Logs/SkirmishProduction/s026-external-route-query-draft-20261116-r1/request-draft.json")

ROLE_COUNTS = {
    "role.rifle": 12, "role.gunner": 4, "role.rocketeer": 4,
    "role.car": 1, "role.apc_armored": 1, "role.tank": 1,
    "role.tanker": 1, "role.logistics_truck": 2,
}


def sha256(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def read_pinned(root: Path, rel: Path, expected: str) -> tuple[dict[str, Any], dict[str, Any]]:
    path = root / rel
    if path.is_symlink() or not path.is_file():
        raise ValueError(f"Expected regular source file: {rel}")
    raw = path.read_bytes()
    actual = sha256(raw)
    if actual != expected:
        raise ValueError(f"Pinned source hash mismatch for {rel}: {actual}")
    value = json.loads(raw)
    if not isinstance(value, dict):
        raise ValueError(f"Expected JSON object: {rel}")
    return value, {"path": rel.as_posix(), "sha256": actual, "bytes": len(raw)}


def build(root: Path) -> dict[str, Any]:
    report, r41_pin = read_pinned(root, R41_REL, R41_SHA256)
    rally, rally_pin = read_pinned(root, RALLY_REL, RALLY_SHA256)
    rally_receipt, receipt_pin = read_pinned(root, RALLY_RECEIPT_REL, RALLY_RECEIPT_SHA256)

    if (report.get("kind") != "S026CityCrossroadsAliasSourceOwnerDiagnostic" or
            report.get("result") != "CollectedUnqualified" or
            report.get("identity", {}).get("catalogId") != "S026" or
            report.get("identity", {}).get("size") != "Standard" or
            report.get("identity", {}).get("difficulty") != "Regular" or
            report.get("cleanupVerified") is not True or
            report.get("liveOccupancyIncluded") is not False or
            report.get("sourceFitCertified") is not False or
            report.get("normalMissionReady") is not False):
        raise ValueError("Expected cleanup-verified, unqualified r41 Standard/Regular candidate report")
    typed = report.get("sharedTankWaypointsWithCandidateFacilityBlockers", {}).get("typedPadHypotheses", {})
    variants = typed.get("variants")
    if typed.get("seed") != 104755 or typed.get("variantCountCap") != 2 or not isinstance(variants, list) or len(variants) != 2 or variants[0].get("variant") != 0:
        raise ValueError("Expected pinned r41 variant-0 actor candidate rows")
    variant = variants[0]
    if variant.get("requestedActors") != 52 or variant.get("foundActors") != 52 or variant.get("candidateOnly") is not True:
        raise ValueError("Expected exactly 52 candidate actors")

    if (rally.get("kind") != "S026StandardVariant0TypedSpawnAndRallyPadPartialData" or
            rally.get("status") != "PartialTypedSpawnAndRallyPadData_UnqualifiedHypotheses" or
            rally.get("claims", {}).get("normalMissionReady") is not False or
            rally_receipt.get("kind") != "S026StandardVariant0RallyPadDraftReceipt" or
            rally_receipt.get("claims", {}).get("readiness") is not False):
        raise ValueError("Expected separate unqualified variant-0 Rally draft")
    pads = rally.get("layoutCandidate", {}).get("candidateLayout", {}).get("pads", [])
    rally_by_faction = {p.get("FactionId"): p for p in pads if p.get("Kind") == 5 and p.get("HypothesisOnly") is True}
    if set(rally_by_faction) != {1, 2}:
        raise ValueError("Expected one hypothetical Rally pad per faction")

    grid = report.get("nativeSourceGrid", {})
    origin = grid.get("origin")
    cell_size = grid.get("cellSize")
    if not (isinstance(origin, list) and len(origin) == 2 and cell_size == 1.0):
        raise ValueError("Expected pinned 1 metre native source grid")

    rows: list[dict[str, Any]] = []
    seen: set[tuple[int, str, int]] = set()
    role_counts: dict[str, int] = {}
    faction_counts = {1: 0, 2: 0}
    for source_row in variant.get("packingRows", []):
        faction, role = source_row.get("faction"), source_row.get("roleId")
        if faction not in (1, 2) or role not in ROLE_COUNTS:
            raise ValueError("Unsupported r41 faction or role")
        cells = source_row.get("candidateCells")
        if not isinstance(cells, list) or len(cells) != source_row.get("requestedCount"):
            raise ValueError(f"Candidate-cell count mismatch for {role}/faction {faction}")
        footprint = {"x": source_row.get("footprintX"), "z": source_row.get("footprintZ")}
        movement = source_row.get("nativeMovementBehavior")
        if (not all(isinstance(footprint[k], int) and footprint[k] > 0 for k in footprint) or
                not isinstance(movement, dict) or set(movement) != {"AllowIdleWander", "UsesVehicleMotion"}):
            raise ValueError(f"Missing native footprint/movement for {role}/faction {faction}")
        rally_pad = rally_by_faction[faction]
        goal = {
            "x": math.floor((float(rally_pad["CenterX"]) - float(origin[0])) / cell_size),
            "z": math.floor((float(rally_pad["CenterZ"]) - float(origin[1])) / cell_size),
        }
        for cell in cells:
            member = cell.get("member")
            x, z = cell.get("x"), cell.get("z")
            if not isinstance(member, int) or not isinstance(x, int) or not isinstance(z, int):
                raise ValueError("Candidate start cell/member must be integer typed grid data")
            key = (faction, role, member)
            if key in seen:
                raise ValueError(f"Duplicate candidate actor key: {key}")
            seen.add(key)
            rows.append({
                "candidateActorId": f"r41.v0.f{faction}.{role}.m{member:02d}",
                "factionId": faction,
                "roleId": role,
                "memberIndex": member,
                "startCell": {"x": x, "z": z},
                "selectedFactionRally": {
                    "padId": rally_pad["PadId"],
                    "centerWorldMetres": {"x": rally_pad["CenterX"], "z": rally_pad["CenterZ"]},
                    "goalCell": goal,
                    "goalType": "Unity.Mathematics.int2",
                    "gridConversion": "floor((worldCentre - gridOrigin) / cellSize)",
                },
                "nativeFootprintCells": footprint,
                "nativeMovementBehavior": movement,
                "manualMove": False,
                "manualMoveSelection": "Provisional query choice; not an observed runtime order",
                "sourceSpawnPadId": source_row.get("padId"),
                "unexecutableReason": "Candidate coordinates only; no actor/entity exists and no pathfinding consumer is attached.",
            })
            role_counts[role] = role_counts.get(role, 0) + 1
            faction_counts[faction] += 1

    rows.sort(key=lambda r: (r["factionId"], r["roleId"], r["memberIndex"]))
    if len(rows) != 52 or role_counts != {k: v * 2 for k, v in ROLE_COUNTS.items()} or faction_counts != {1: 26, 2: 26}:
        raise ValueError(f"Unexpected candidate actor census: rows={len(rows)} roles={role_counts} factions={faction_counts}")

    return {
        "schema": 1,
        "kind": "S026ExternalTypedEndpointQueryRequestDraft",
        "status": "UnexecutableRequestDraft",
        "identity": {"catalogId": "S026", "size": "Standard", "difficulty": "Regular", "variant": 0, "seed": 104755},
        "requestSemantics": {
            "requestType": "Game.Components.UnitPathRequest",
            "goalField": "Unity.Mathematics.int2 Goal",
            "direction": "candidate spawn start cell to selected faction Rally goal cell",
            "manualMoveSelection": "False is a provisional query choice, not observed runtime behavior",
            "executorAttached": False,
            "widthSweepsIncluded": False,
            "pathResultsIncluded": False,
            "liveEntitiesCreated": False,
            "liveOccupancyIncluded": False,
        },
        "grid": {"origin": origin, "cellSizeMetres": cell_size, "width": grid.get("width"), "height": grid.get("height")},
        "inputs": [r41_pin, rally_pin, receipt_pin],
        "counts": {"requestRows": len(rows), "faction1": faction_counts[1], "faction2": faction_counts[2], "byRole": dict(sorted(role_counts.items()))},
        "rows": rows,
        "claims": {"sourceHashesVerified": True, "candidateRowsCounted": True, "pathfindingExecuted": False,
                   "reachability": False, "liveOccupancy": False, "widthResults": False,
                   "sourceFit": False, "missionReadiness": False},
        "limitations": [
            "The starts are hash-pinned r41 variant-0 candidate actor cells, not spawned or occupied actors.",
            "Rally pads and centers are unqualified hypotheses from a separate partial-data draft.",
            "Goal int2 conversion is a data convention only; no original owner or runtime consumer was invoked.",
            "No width sweep, route result, reachability, live occupancy, source fit, or readiness is established.",
        ],
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--output", type=Path, default=OUTPUT_REL)
    args = parser.parse_args()
    root = args.root.resolve()
    output = (args.output if args.output.is_absolute() else root / args.output).resolve()
    if not output.is_relative_to(root) or output.exists():
        raise ValueError("New output under the selected root required")
    output.parent.mkdir(parents=True, exist_ok=True)
    value = build(root)
    encoded = (json.dumps(value, sort_keys=True, indent=2, ensure_ascii=False) + "\n").encode()
    output.write_bytes(encoded)
    print(json.dumps({"status": value["status"], "rows": value["counts"]["requestRows"],
                      "sha256": sha256(encoded), "output": output.relative_to(root).as_posix(),
                      "executable": False, "pathfindingExecuted": False, "readiness": False}, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

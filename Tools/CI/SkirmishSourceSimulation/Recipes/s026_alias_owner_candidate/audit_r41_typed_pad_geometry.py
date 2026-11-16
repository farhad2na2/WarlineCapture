#!/usr/bin/env python3
"""Independently audit geometry in the unqualified S026 r41 pad hypotheses.

This reads one immutable Unity diagnostic report. It does not author pads,
run Unity, or qualify S026 layout/source fit.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[5]
REPORT_REL = Path("Logs/SkirmishProduction/s026-citycrossroads-alias-source-diagnostic-20261116-r41/report.json")
REPORT_SHA256 = "9bc633854d9dbed1b8ab2247e635f48ab3bacf89ec38734f6187bd49e3a343f6"
FOOTPRINT_PATH = "Assets/Game/Scripts/Components/UnitFootprintUtility.cs"
FOOTPRINT_SHA256 = "af2205647cd3bf6d99df509af5525840e60bda99659b3e0ffa17bc24e4303599"
OUTPUT_REL = Path("Logs/SkirmishProduction/s026-typed-pad-geometry-audit-20261116-r1/receipt.json")
EXPECTED_ROLE_COUNTS = {
    "role.rifle": 12,
    "role.gunner": 4,
    "role.rocketeer": 4,
    "role.car": 1,
    "role.apc_armored": 1,
    "role.tank": 1,
    "role.tanker": 1,
    "role.logistics_truck": 2,
}
INFANTRY_ROLES = {"role.rifle", "role.gunner", "role.rocketeer"}


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def require(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def reject_duplicate_keys(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        require(key not in result, f"Duplicate JSON property: {key}")
        result[key] = value
    return result


def read_json(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=reject_duplicate_keys)
    require(isinstance(value, dict), "Report must be a JSON object")
    return value


def rect_intersects(a: tuple[float, float, float, float], b: tuple[float, float, float, float]) -> bool:
    return a[0] < b[2] and b[0] < a[2] and a[1] < b[3] and b[1] < a[3]


def rect_contains(outer: tuple[float, float, float, float], inner: tuple[float, float, float, float], eps: float = 1e-6) -> bool:
    return (inner[0] >= outer[0] - eps and inner[1] >= outer[1] - eps and
            inner[2] <= outer[2] + eps and inner[3] <= outer[3] + eps)


def source_hashes(report: dict[str, Any], root: Path) -> list[dict[str, str]]:
    pins: dict[tuple[str, str], None] = {}

    def walk(value: Any) -> None:
        if isinstance(value, dict):
            path, digest = value.get("path"), value.get("sha256")
            if isinstance(path, str) and isinstance(digest, str):
                pins[(path, digest)] = None
            for child in value.values():
                walk(child)
        elif isinstance(value, list):
            for child in value:
                walk(child)

    walk(report)
    checked = []
    for rel, expected in sorted(pins):
        path = root / rel
        require(path.is_file(), f"Pinned source missing: {rel}")
        actual = sha256_bytes(path.read_bytes())
        require(actual == expected, f"Pinned source changed: {rel}")
        checked.append({"path": rel, "sha256": actual})
    return checked


def locate_exact_facility_rectangles(report: dict[str, Any]) -> list[tuple[str, tuple[float, float, float, float]]]:
    sizes = report["sourceStructurePlacementCandidates"]["constrainedFirstMiddleBandProbe"]["sizes"]
    standard = [row for row in sizes if row.get("size") == "Standard"]
    require(len(standard) == 1, "Expected one Standard facility-pair search")
    attempts = [row for row in standard[0]["pairAttempts"]
                if row.get("allPlannedStructuresRevalidatedInCanonicalOrder") is True and
                row.get("completeLogicalInstanceBindingProjection", {}).get("tryApplyPassed") is True and
                row.get("completeLogicalInstanceBindingProjection", {}).get("exactCoordinatesPreserved") is True and
                row.get("completeLogicalInstanceBindingProjection", {}).get("allOffsetsZero") is True]
    require(len(attempts) == 1, "Expected unique exact selected Standard facility attempt")
    plans = attempts[0].get("planByCompilerIndex")
    require(isinstance(plans, dict) and len(plans) == 20, "Expected exact 20 compiler facility plans")
    rects = []
    for index, plan in sorted(plans.items(), key=lambda item: int(item[0])):
        raw = plan.get("resolvedEffectiveRect")
        require(plan.get("foundByOriginalOwner") is True and isinstance(raw, dict),
                f"Facility {index} lacks exact original-owner rectangle")
        x, z, w, h = (raw.get(k) for k in ("x", "z", "width", "depth"))
        require(all(isinstance(v, int) and not isinstance(v, bool) for v in (x, z, w, h)) and w > 0 and h > 0,
                f"Facility {index} has invalid rectangle")
        rects.append((str(index), (float(x), float(z), float(x + w), float(z + h))))
    return rects


def variant_audit(variant: dict[str, Any], facilities: list[tuple[str, tuple[float, float, float, float]]],
                  grid_origin: tuple[float, float], cell_size: float,
                  bounds: tuple[float, float, float, float]) -> dict[str, Any]:
    pads = variant.get("candidatePads")
    require(isinstance(pads, list) and len(pads) == 4, "Each hypothesis must contain exactly four pads")
    pad_rects: dict[str, tuple[float, float, float, float]] = {}
    faction_kinds: set[tuple[int, int]] = set()
    for pad in pads:
        pid, faction, kind = pad.get("PadId"), pad.get("FactionId"), pad.get("Kind")
        require(isinstance(pid, str) and pid not in pad_rects, "Pad IDs must be unique")
        require(faction in (1, 2) and kind in (3, 4), "Pad has unsupported faction/kind")
        require((faction, kind) not in faction_kinds, "Duplicate faction/kind pad")
        faction_kinds.add((faction, kind))
        cx, cz, width, depth = (pad.get(k) for k in ("CenterX", "CenterZ", "WidthMetres", "DepthMetres"))
        require(all(isinstance(x, (int, float)) and not isinstance(x, bool) for x in (cx, cz, width, depth)) and
                width > 0 and depth > 0, f"Pad {pid} has invalid projected dimensions")
        rect = (float(cx) - float(width) / 2, float(cz) - float(depth) / 2,
                float(cx) + float(width) / 2, float(cz) + float(depth) / 2)
        require(rect_contains(bounds, rect), f"Pad outside source playable bounds: {pid}")
        pad_rects[pid] = rect
    require(faction_kinds == {(f, k) for f in (1, 2) for k in (3, 4)}, "Missing expected faction/category pad")

    pad_overlaps = []
    for i, (left_id, left) in enumerate(sorted(pad_rects.items())):
        for right_id, right in sorted(pad_rects.items())[i + 1:]:
            if rect_intersects(left, right):
                pad_overlaps.append([left_id, right_id])
    pad_facility_overlaps = [[pad_id, fid] for pad_id, rect in sorted(pad_rects.items())
                             for fid, facility in facilities if rect_intersects(rect, facility)]
    require(not pad_overlaps, "Candidate pads overlap each other")
    require(not pad_facility_overlaps, "Candidate pad intersects selected facility rectangle")

    rows = variant.get("packingRows")
    require(isinstance(rows, list) and len(rows) == 16, "Expected 16 exact faction/force rows")
    by_faction_role: dict[tuple[int, str], int] = {}
    actors: list[dict[str, Any]] = []
    for row in rows:
        faction, role = row.get("faction"), row.get("roleId")
        require(faction in (1, 2) and role in EXPECTED_ROLE_COUNTS, "Unexpected force row")
        require(row.get("requestedCount") == EXPECTED_ROLE_COUNTS[role], f"Wrong requested count for {role}")
        require(row.get("foundCount") == row.get("requestedCount") == row.get("insideTypedPadCount") == row.get("surfaceTraversableCount"),
                f"Incomplete or uncontained typed roster for {role}")
        require(row.get("typedSpawnConstraintApplied") is True, "Typed mask was not applied")
        pad_id = row.get("padId")
        require(pad_id in pad_rects, f"Force row refers to unknown pad: {pad_id}")
        pad = next(p for p in pads if p["PadId"] == pad_id)
        expected_kind = 3 if role in INFANTRY_ROLES else 4
        require(pad["FactionId"] == faction and pad["Kind"] == expected_kind, f"Wrong faction/category pad for {role}")
        by_faction_role[(faction, role)] = by_faction_role.get((faction, role), 0) + 1
        w, h = row.get("footprintX"), row.get("footprintZ")
        require(all(isinstance(n, int) and not isinstance(n, bool) and n > 0 for n in (w, h)),
                f"Invalid reported footprint for {role}")
        cells = row.get("candidateCells")
        require(isinstance(cells, list) and len(cells) == row["requestedCount"], f"Wrong actor count for {role}")
        require(sorted(c.get("member") for c in cells) == list(range(len(cells))), f"Duplicate/missing actor member for {role}")
        for cell in cells:
            require(cell.get("normalFallbackAccepted") is True and cell.get("surfaceTraversable") is True and
                    cell.get("insideActualTypedPad") is True, f"Actor flags failed for {role}")
            x, z = cell.get("x"), cell.get("z")
            require(all(isinstance(n, int) and not isinstance(n, bool) for n in (x, z)), "Actor cell must be integral")
            # Match UnitFootprintUtility.GetMinCell(center,size), then project the half-open footprint edges.
            min_x, min_z = x - ((w - 1) // 2), z - ((h - 1) // 2)
            world = (grid_origin[0] + min_x * cell_size, grid_origin[1] + min_z * cell_size,
                     grid_origin[0] + (min_x + w) * cell_size, grid_origin[1] + (min_z + h) * cell_size)
            require(rect_contains(pad_rects[pad_id], world), f"Reconstructed footprint outside pad: {role}/{cell['member']}")
            actors.append({"faction": faction, "roleId": role, "member": cell["member"],
                           "padId": pad_id, "cell": [x, z], "footprintCells": [min_x, min_z, w, h],
                           "worldRect": [round(v, 6) for v in world]})

    for faction in (1, 2):
        for role, quantity in EXPECTED_ROLE_COUNTS.items():
            require(by_faction_role.get((faction, role), 0) == 1, f"Missing or duplicate row for faction {faction} {role}")
    require(len(actors) == 52 and variant.get("requestedActors") == 52 and variant.get("foundActors") == 52 and
            variant.get("insideTypedPadActors") == 52 and variant.get("surfaceTraversableActors") == 52,
            "Variant must report exact 52/52 roster")

    actor_overlaps = []
    actor_facility_overlaps = []
    for i, actor in enumerate(actors):
        ar = tuple(actor["worldRect"])
        for previous in actors[:i]:
            if rect_intersects(ar, tuple(previous["worldRect"])):
                actor_overlaps.append([[previous["faction"], previous["roleId"], previous["member"]],
                                       [actor["faction"], actor["roleId"], actor["member"]]])
        for fid, fr in facilities:
            facility_world = (grid_origin[0] + fr[0] * cell_size, grid_origin[1] + fr[1] * cell_size,
                              grid_origin[0] + fr[2] * cell_size, grid_origin[1] + fr[3] * cell_size)
            if rect_intersects(ar, facility_world):
                actor_facility_overlaps.append([[actor["faction"], actor["roleId"], actor["member"]], fid])
    require(not actor_overlaps, "Actor footprints overlap")
    require(not actor_facility_overlaps, "Actor footprint intersects a facility rectangle")
    return {
        "variant": variant["variant"], "candidatePadCount": len(pads), "facilityRectCount": len(facilities),
        "requestedActors": len(actors), "padBounds": {k: list(v) for k, v in sorted(pad_rects.items())},
        "padPadOverlapPairs": pad_overlaps, "padFacilityOverlapPairs": pad_facility_overlaps,
        "actorFootprintOverlapPairs": actor_overlaps, "actorFacilityOverlapPairs": actor_facility_overlaps,
        "actorFootprints": actors,
        "reportClaimFacilityPadRectangleOverlapChecked": variant.get("facilityPadRectangleOverlapChecked"),
        "independentGeometryChecks": "Passed",
    }


def build_receipt(root: Path, report_rel: Path = REPORT_REL) -> dict[str, Any]:
    report_path = root / report_rel
    require(report_path.is_file(), f"Input report missing: {report_rel}")
    report_bytes = report_path.read_bytes()
    report_sha = sha256_bytes(report_bytes)
    require(report_sha == REPORT_SHA256, "r41 report hash mismatch")
    report = read_json(report_path)
    require(report.get("result") == "CollectedUnqualified", "Unexpected source diagnostic result")
    require(report.get("identity", {}).get("catalogId") == "S026" and
            report.get("identity", {}).get("size") == "Standard" and
            report.get("identity", {}).get("difficulty") == "Regular", "Unexpected mission tuple")
    require(report.get("sourceFitCertified") is False and report.get("normalMissionReady") is False and
            report.get("nativeMissionInitialized") is False and report.get("liveOccupancyIncluded") is False and
            report.get("fullMapOrAllSizeFitMeasured") is False, "Unqualified scope flags changed")
    require(report.get("cleanupVerified") is True and report.get("controlsPassed") == 6 and
            report.get("companionCleanupControlsPassed") == 6, "Cleanup/controls are not fully recorded")
    cleanup = report.get("cleanupDetails", {})
    require(cleanup.get("worldCensusSame") is True and cleanup.get("defaultWorldSame") is True and
            cleanup.get("sceneCountSame") is True and cleanup.get("previewCountSame") is True and
            cleanup.get("previewOwnershipAfter", {}).get("previewCensusComplete") is True,
            "Unity cleanup census differs")
    require(report.get("companionSceneOwnerCleanupOutcome") == "OwnerCallbackClosedOnlyTwoNewCompanionScenes_PostconditionPassed",
            "Unexpected companion scene cleanup outcome")
    source_pin_list = source_hashes(report, root)
    footprint_path = root / FOOTPRINT_PATH
    require(footprint_path.is_file() and sha256_bytes(footprint_path.read_bytes()) == FOOTPRINT_SHA256,
            "UnitFootprintUtility pin mismatch")
    pins = report.get("sourcePins", {})
    require(pins.get("diagnosticImplementationPath") == "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.S026AliasDiagnostic.cs" and
            pins.get("diagnosticImplementationSha256") == sha256_bytes((root / pins["diagnosticImplementationPath"]).read_bytes()),
            "Diagnostic implementation pin mismatch")

    relation = report.get("sharedTankWaypointsWithCandidateFacilityBlockers", {})
    typed = relation.get("typedPadHypotheses", {})
    require(relation.get("candidateFacilityCount") == 20 and typed.get("candidateOnly") is True and
            typed.get("sourceFitCertified") is False and typed.get("normalMissionInitialized") is False and
            typed.get("normalSpawnAcceptance") is False and typed.get("liveOccupancyIncluded") is False and
            typed.get("fullLayoutValidated") is False and typed.get("status") == "TransientTypedPadHypothesesCollected_Unqualified",
            "Typed hypothesis scope/qualification flags changed")
    require(typed.get("seed") == 104755 and typed.get("variantCountCap") == 2, "Unexpected typed probe seed/cap")
    grid = report.get("nativeSourceGrid", {})
    origin = grid.get("origin")
    require(isinstance(origin, list) and len(origin) == 2 and all(isinstance(x, (int, float)) for x in origin),
            "Grid origin unavailable")
    cell_size = grid.get("cellSize")
    require(isinstance(cell_size, (int, float)) and cell_size > 0, "Grid cell size invalid")
    bounds_obj = report.get("aliasPlayableBounds", {})
    bounds = (bounds_obj.get("minX"), bounds_obj.get("minZ"), bounds_obj.get("maxX"), bounds_obj.get("maxZ"))
    require(all(isinstance(x, (int, float)) for x in bounds) and bounds[0] < bounds[2] and bounds[1] < bounds[3],
            "Playable bounds invalid")
    facilities = locate_exact_facility_rectangles(report)
    variants = typed.get("variants")
    require(isinstance(variants, list) and len(variants) == 2, "Expected two typed pad hypotheses")
    audited = [variant_audit(v, facilities, (float(origin[0]), float(origin[1])), float(cell_size), tuple(map(float, bounds)))
               for v in variants]
    require([v["variant"] for v in audited] == [0, 1], "Unexpected hypothesis ordering")
    return {
        "schema": 1,
        "kind": "S026R41TypedPadGeometryAudit",
        "status": "Passed_UnqualifiedGeometryAudit",
        "inputs": [{"path": report_rel.as_posix(), "sha256": report_sha}],
        "separatelyPinnedOwner": {"path": FOOTPRINT_PATH, "sha256": FOOTPRINT_SHA256,
                                  "purpose": "UnitFootprintUtility.GetMinCell footprint reconstruction"},
        "verifiedReportOwnerPins": source_pin_list,
        "identity": {"catalogId": "S026", "definitionId": "skirmish.s026", "scenarioSetupId": "scenario.skirmish.s026",
                     "size": "Standard", "difficulty": "Regular", "seed": 104755},
        "facilitySelection": {"count": len(facilities), "source": "unique Standard pairAttempt, all 20 original-owner resolved rectangles",
                              "allOriginalOwnerRectanglesPresent": True},
        "variants": audited,
        "scope": {"candidateOnly": True, "authoredPads": False, "unityRunByRecipe": False,
                  "sourceFitCertified": False, "normalMissionInitialized": False,
                  "normalSpawnAcceptance": False, "liveOccupancyIncluded": False,
                  "fullLayoutValidated": False, "physicalWidthOrReadinessClaim": False,
                  "interpretation": "Independent geometry audit of reported temporary hypotheses only; does not qualify S026 fit."},
        "cleanup": {"verified": True, "controlsPassed": 6, "companionCleanupControlsPassed": 6,
                    "worldAndSceneCensusesRestored": True},
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--report", type=Path, default=REPORT_REL)
    parser.add_argument("--output", type=Path, default=OUTPUT_REL)
    args = parser.parse_args()
    root = args.root.resolve()
    output = args.output if args.output.is_absolute() else root / args.output
    receipt = build_receipt(root, args.report if args.report.is_absolute() else args.report)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(receipt, sort_keys=True, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"[s026-r41-typed-pad-geometry-audit] result=Passed_UnqualifiedGeometryAudit variants=2 actors_each=52 facilities=20 output={output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

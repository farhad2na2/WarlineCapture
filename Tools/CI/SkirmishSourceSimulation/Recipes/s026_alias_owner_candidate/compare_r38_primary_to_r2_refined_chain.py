#!/usr/bin/env python3
"""Compare two S026 static owner-path centerlines from pinned receipts.

This is a diagnostic overlap comparison only. It does not prove route
independence, physical width, source fit, gameplay acceptance, or readiness.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[5]
R38_REL = Path("Logs/SkirmishProduction/s026-citycrossroads-alias-source-diagnostic-20261116-r38/report.json")
R2_DIR_REL = Path("Logs/SkirmishProduction/s026-regular-owner-placement-candidate-20261116-r2")
R2_BUILD_REL = R2_DIR_REL / "build-receipt.json"
R2_CANDIDATE_REL = R2_DIR_REL / "candidate-evidence.json"

# Exact report and candidate snapshot used for this one bounded comparison.
R38_SHA256 = "cafa3958d61f972c58ad1cc2af4a2c021361005e6f97502c49929096ca918103"
R2_BUILD_SHA256 = "da36220caec2b72223d1d2be2be231d74b599f3c01e54642a3ba47bafc6f4237"
R2_CANDIDATE_SHA256 = "e93582eaa0f71ffcddd62d98d297831e6fa1ea08cae3a1b9253401127d811bfa"
BLOCKER_SCOPE = "ExactAcceptedStaticCandidateRectangles_NotLiveDynamicOccupancy"
ROLE_ORDER = ("role.rifle", "role.tank", "role.apc_armored")
SHARED_ENDPOINT_RADIUS_CELLS = 16
MAX_RASTERIZED_CELLS_PER_SEGMENT = 10001


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    return sha256_bytes(path.read_bytes())


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def read_json(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8") as stream:
        value = json.load(stream, object_pairs_hook=_reject_duplicate_keys)
    require(isinstance(value, dict), f"Expected JSON object: {path}")
    return value


def _reject_duplicate_keys(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        require(key not in result, f"Duplicate JSON property: {key}")
        result[key] = value
    return result


def sha256_path_from_pin(root: Path, pins: dict[str, Any], path_key: str, hash_key: str) -> str:
    rel = pins.get(path_key)
    expected = pins.get(hash_key)
    require(isinstance(rel, str) and isinstance(expected, str), f"Missing source pin: {path_key}/{hash_key}")
    path = root / rel
    require(path.is_file(), f"Pinned source file missing: {rel}")
    actual = sha256_file(path)
    require(actual == expected, f"Pinned source drift: {rel}")
    return actual


def resolve_json_pointer(root: Any, pointer: str) -> Any:
    require(pointer.startswith("/"), f"Expected absolute JSON pointer: {pointer}")
    value = root
    for raw in pointer[1:].split("/"):
        token = raw.replace("~1", "/").replace("~0", "~")
        if isinstance(value, list):
            require(token.isdigit(), f"Invalid array index in selector: {pointer}")
            value = value[int(token)]
        elif isinstance(value, dict):
            require(token in value, f"Receipt selector missing: {pointer}")
            value = value[token]
        else:
            raise ValueError(f"Receipt selector traverses scalar: {pointer}")
    return value


def int_cell(value: Any, label: str) -> tuple[int, int]:
    require(isinstance(value, list) and len(value) == 2, f"Invalid cell: {label}")
    require(all(isinstance(n, int) and not isinstance(n, bool) for n in value), f"Noninteger cell: {label}")
    return value[0], value[1]


def rasterize_csharp_bresenham(points: list[tuple[int, int]]) -> set[tuple[int, int]]:
    """Match RouteIndependence.cs RasterizePilotCenterline exactly."""
    require(len(points) >= 2, "At least two points required for a centerline")
    cells: set[tuple[int, int]] = set()
    for start, end in zip(points, points[1:]):
        x, z = start
        end_x, end_z = end
        dx = abs(end_x - x)
        dz = -abs(end_z - z)
        sx = 1 if x < end_x else -1
        sz = 1 if z < end_z else -1
        error = dx + dz
        require(dx - dz <= 10000, "Compressed path segment exceeds C# rasterization cap")
        segment_cells = 0
        while True:
            cells.add((x, z))
            segment_cells += 1
            require(segment_cells <= MAX_RASTERIZED_CELLS_PER_SEGMENT,
                    "Rasterized segment exceeds bounded cell cap")
            if x == end_x and z == end_z:
                break
            twice = error * 2
            if twice >= dz:
                error += dz
                x += sx
            if twice <= dx:
                error += dx
                z += sz
    return cells


def validate_path_rows(
    rows: list[dict[str, Any]],
    *,
    label: str,
    bounds: dict[str, Any],
    expected_footprint: tuple[int, int] | None = None,
) -> tuple[list[tuple[int, int]], tuple[int, int], tuple[int, int], tuple[int, int]]:
    require(rows, f"No segments: {label}")
    assembled_points: list[tuple[int, int]] = []
    all_compressed: list[tuple[int, int]] = []
    role_footprint: tuple[int, int] | None = expected_footprint
    for index, row in enumerate(rows):
        require(isinstance(row, dict), f"Malformed segment row: {label}/{index}")
        require(isinstance(row.get("segmentIndex"), int) and row.get("segmentIndex") >= 0,
                f"Missing owner segment index: {label}/{index}")
        require(row.get("status") == "NativeQueryFoundExactGoal" and row.get("failureCode") == 0,
                f"Owner segment did not pass exactly: {label}/{index}")
        require(row.get("exactOwnerSegmentPassed") is True and row.get("entirePathInsideAliasFrame") is True,
                f"Owner segment pass/alias flags missing: {label}/{index}")
        require(row.get("dynamicBlockersIncluded") is False and row.get("liveOccupancyIncluded") is False,
                f"Unexpected occupancy/blocker scope: {label}/{index}")
        require(row.get("plannedFacilityBlockerScope") == BLOCKER_SCOPE,
                f"Candidate facility blocker scope differs: {label}/{index}")
        require(row.get("nativeQueryFaction") == 1, f"Unexpected path query faction: {label}/{index}")
        footprint = (row.get("footprintX"), row.get("footprintZ"))
        require(all(isinstance(n, int) and n > 0 for n in footprint), f"Invalid role footprint: {label}/{index}")
        footprint = (int(footprint[0]), int(footprint[1]))
        if role_footprint is None:
            role_footprint = footprint
        require(footprint == role_footprint, f"Footprint changed within role chain: {label}/{index}")

        start = int_cell(row.get("startCell"), f"{label}/{index}/startCell")
        goal = int_cell(row.get("goalCell"), f"{label}/{index}/goalCell")
        cells_value = row.get("pathCells")
        require(isinstance(cells_value, list) and len(cells_value) >= 2, f"Missing returned cells: {label}/{index}")
        cells = [int_cell(point, f"{label}/{index}/pathCells") for point in cells_value]
        require(cells[0] == start and cells[-1] == goal, f"Returned path endpoints differ: {label}/{index}")
        if index == 0:
            assembled_points.append(start)
        else:
            require(assembled_points[-1] == start, f"Discontinuous owner segments: {label}/{index}")
        assembled_points.append(goal)
        if all_compressed:
            require(all_compressed[-1] == cells[0], f"Returned path cells do not join: {label}/{index}")
            all_compressed.extend(cells[1:])
        else:
            all_compressed.extend(cells)
        for x, z in cells:
            require(bounds["minX"] <= x <= bounds["maxX"] and bounds["minZ"] <= z <= bounds["maxZ"],
                    f"Returned cell outside alias bounds: {label}/{index}")

    require(role_footprint is not None, f"Role footprint unavailable: {label}")
    return all_compressed, assembled_points[0], assembled_points[-1], role_footprint


def endpoint_excluded(centerline: set[tuple[int, int]], start: tuple[int, int], goal: tuple[int, int]) -> set[tuple[int, int]]:
    radius_sq = SHARED_ENDPOINT_RADIUS_CELLS * SHARED_ENDPOINT_RADIUS_CELLS
    return {
        (x, z) for x, z in centerline
        if (x - start[0]) ** 2 + (z - start[1]) ** 2 > radius_sq
        and (x - goal[0]) ** 2 + (z - goal[1]) ** 2 > radius_sq
    }


def validate_r38_source_pins(root: Path, report: dict[str, Any]) -> dict[str, str]:
    pins = report.get("sourcePins")
    require(isinstance(pins, dict), "R38 sourcePins object missing")
    checked: dict[str, str] = {}
    expected_paths = {
        "aliasPath": "Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset",
        "campaignPath": "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset",
        "gridPath": "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset",
        "surfacePath": "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset",
        "diagnosticImplementationPath": "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.S026AliasDiagnostic.cs",
    }
    hash_keys = {
        "aliasPath": "aliasSha256", "campaignPath": "campaignSha256", "gridPath": "gridSha256",
        "surfacePath": "surfaceSha256", "diagnosticImplementationPath": "diagnosticImplementationSha256",
    }
    for key, expected_path in expected_paths.items():
        require(pins.get(key) == expected_path, f"Unexpected R38 pinned path: {key}")
        checked[key] = sha256_path_from_pin(root, pins, key, hash_keys[key])
    require(pins.get("aliasOperationMapId") == "opmap.skirmish.city_crossroads", "Unexpected alias map identity")
    require(pins.get("campaignOperationMapId") == "opmap.skirmish.desert_base_01", "Unexpected source map identity")
    require(pins.get("aliasSourceBinding", {}).get("sourceContentHash") == pins.get("campaignContentHash"),
            "Alias source content binding mismatch")
    return checked


def summarize(root: Path) -> dict[str, Any]:
    r38_path = root / R38_REL
    build_path = root / R2_BUILD_REL
    candidate_path = root / R2_CANDIDATE_REL
    require(sha256_file(r38_path) == R38_SHA256, "R38 report hash mismatch")
    require(sha256_file(build_path) == R2_BUILD_SHA256, "R2 build receipt hash mismatch")
    require(sha256_file(candidate_path) == R2_CANDIDATE_SHA256, "R2 candidate evidence hash mismatch")
    report = read_json(r38_path)
    build = read_json(build_path)
    candidate = read_json(candidate_path)
    require(build.get("candidatePath") == str(R2_CANDIDATE_REL), "R2 builder receipt candidate path mismatch")
    require(build.get("candidateSha256") == R2_CANDIDATE_SHA256, "R2 builder receipt candidate hash mismatch")
    builder_path = root / build.get("builderPath", "")
    require(builder_path.is_file() and sha256_file(builder_path) == build.get("builderSha256"),
            "R2 candidate builder source pin mismatch")

    require(report.get("result") == "CollectedUnqualified", "R38 diagnostic result is not unqualified-collected")
    pins_checked = validate_r38_source_pins(root, report)
    blocker_probe = report.get("sharedTankWaypointsWithCandidateFacilityBlockers")
    require(isinstance(blocker_probe, dict), "R38 facility-blocked waypoint section missing")
    for key in ("routeProof", "routeBindingExists", "sourceFitCertified", "liveOccupancyIncluded",
                "dynamicBlockersIncluded", "roadSemanticsUsed", "productionPathLimitsChanged"):
        require(blocker_probe.get(key) is False, f"R38 facility-blocked scope flag not false: {key}")
    require(blocker_probe.get("status") == "CandidateFacilityBlockedSegmentsCollected_Unqualified",
            "R38 facility-blocked section status mismatch")
    require(blocker_probe.get("candidateFacilityCount") == 20 and blocker_probe.get("blockedCellCount") == 3522,
            "R38 candidate facility blocker census mismatch")
    require(blocker_probe.get("crossRoleOwnerQueryCount") == 39 and blocker_probe.get("totalOwnerQueryCount") == 73,
            "R38 facility-blocked query census mismatch")

    chain = candidate.get("chainedOwnerSegmentCandidate")
    require(candidate.get("kind") == "S026RegularSourceOwnerPlacementCandidateEvidence" and
            candidate.get("status") == "PartialOwnerPlacementCandidate_Unqualified",
            "Unexpected R2 candidate evidence identity/status")
    # The candidate's top-level receipt records its original r10 source, while
    # the independently assembled refined chain was later refreshed against
    # r38. Pin both lineage edges; do not conflate them.
    original_receipt_rel = candidate.get("routeCandidateReceipt")
    original_receipt_hash = candidate.get("routeCandidateReceiptSha256")
    require(isinstance(original_receipt_rel, str) and isinstance(original_receipt_hash, str),
            "R2 candidate's original source receipt pin missing")
    original_receipt_path = root / original_receipt_rel
    require(original_receipt_path.is_file() and sha256_file(original_receipt_path) == original_receipt_hash,
            "R2 candidate's original source receipt hash mismatch")
    require(isinstance(chain, dict) and chain.get("receiptPath") == str(R38_REL) and
            chain.get("receiptSha256") == R38_SHA256, "R2 refined chain receipt pin mismatch")
    for key in ("routeBindingExists", "routeProof", "sourceFitCertified", "liveOccupancyIncluded",
                "dynamicBlockersIncluded", "sourceRoadSemanticsUsed", "normalMissionReady", "routeRowsAuthored"):
        require(chain.get(key) is False, f"R2 refined chain scope flag not false: {key}")
    require(chain.get("facilityBlockerCount") == 20 and chain.get("blockedCellCount") == 3522 and
            chain.get("cleanupVerified") is True and chain.get("sourceAssetsUnmodified") is True,
            "R2 refined chain facility/cleanup evidence mismatch")

    bounds = report.get("aliasPlayableBounds")
    require(isinstance(bounds, dict), "R38 alias bounds missing")
    primary_rows = blocker_probe.get("candidateRoles")
    require(isinstance(primary_rows, list), "R38 shared candidate roles missing")
    chain_roles = chain.get("roles")
    require(isinstance(chain_roles, list), "R2 chain roles missing")
    require({r.get("roleId") for r in chain_roles} == set(ROLE_ORDER), "R2 refined chain role set mismatch")

    comparisons: list[dict[str, Any]] = []
    for role_id in ROLE_ORDER:
        primary = [row for row in primary_rows if row.get("roleId") == role_id and row.get("candidate") == "TankPrimary"]
        refined = [row for row in chain_roles if row.get("roleId") == role_id]
        require(len(primary) == 1 and len(refined) == 1, f"Expected one primary/refined row for {role_id}")
        primary_row = primary[0]
        refined_role = refined[0]
        primary_segments = primary_row.get("ownerSegmentChecks")
        refined_summaries = refined_role.get("segments")
        require(isinstance(primary_segments, list) and primary_row.get("allSegmentsOwnerPassed") is True,
                f"R38 TankPrimary not fully owner-passed: {role_id}")
        require(isinstance(refined_summaries, list) and refined_role.get("ownerQueryPasses") is True and
                refined_role.get("endpointContinuityVerified") is True and refined_role.get("exactGoalReached") is True,
                f"R2 refined chain summary not complete: {role_id}")

        refined_rows: list[dict[str, Any]] = []
        for i, summary in enumerate(refined_summaries):
            selector = summary.get("receiptSelector")
            actual_row = resolve_json_pointer(report, selector)
            require(isinstance(actual_row, dict), f"R2 segment selector did not resolve to row: {role_id}/{i}")
            require(summary.get("returnedPathCellsSha256") == sha256_bytes(
                json.dumps(actual_row.get("pathCells"), separators=(",", ":")).encode("utf-8")),
                f"R2 segment path hash mismatch: {role_id}/{i}")
            require(summary.get("returnedPathCellCount") == len(actual_row.get("pathCells", [])),
                    f"R2 segment path count mismatch: {role_id}/{i}")
            require(summary.get("startCell") == actual_row.get("startCell") and
                    summary.get("exactGoalCell") == actual_row.get("goalCell"),
                    f"R2 segment endpoint summary mismatch: {role_id}/{i}")
            require(summary.get("footprint") == [actual_row.get("footprintX"), actual_row.get("footprintZ")],
                    f"R2 segment footprint summary mismatch: {role_id}/{i}")
            refined_rows.append(actual_row)

        primary_path, primary_start, primary_goal, primary_footprint = validate_path_rows(
            primary_segments, label=f"R38/{role_id}/TankPrimary", bounds=bounds)
        refined_path, refined_start, refined_goal, refined_footprint = validate_path_rows(
            refined_rows, label=f"R2/{role_id}/RefinedChain", bounds=bounds,
            expected_footprint=(primary_footprint if role_id == "role.rifle" else None))
        require(primary_start == refined_start and primary_goal == refined_goal,
                f"Role endpoints differ between compared candidates: {role_id}")
        require(primary_footprint == refined_footprint, f"Role footprint differs between candidates: {role_id}")
        primary_centerline = rasterize_csharp_bresenham(primary_path)
        refined_centerline = rasterize_csharp_bresenham(refined_path)
        primary_interior = endpoint_excluded(primary_centerline, primary_start, primary_goal)
        refined_interior = endpoint_excluded(refined_centerline, refined_start, refined_goal)
        intersection = primary_interior & refined_interior
        union = primary_interior | refined_interior
        comparisons.append({
            "roleId": role_id,
            "startCell": list(primary_start),
            "goalCell": list(primary_goal),
            "footprintCells": list(primary_footprint),
            "tankPrimarySegmentCount": len(primary_segments),
            "r2RefinedSegmentCount": len(refined_rows),
            "tankPrimaryCompressedPathCellCount": len(primary_path),
            "r2RefinedCompressedPathCellCount": len(refined_path),
            "tankPrimaryRasterizedCenterlineCellCount": len(primary_centerline),
            "r2RefinedRasterizedCenterlineCellCount": len(refined_centerline),
            "endpointExcludedInteriorCellCount": len(primary_interior),
            "r2EndpointExcludedInteriorCellCount": len(refined_interior),
            "sharedInteriorCellCount": len(intersection),
            "unionInteriorCellCount": len(union),
            "primaryOnlyInteriorCellCount": len(primary_interior - refined_interior),
            "refinedOnlyInteriorCellCount": len(refined_interior - primary_interior),
            "interiorCellJaccard": len(intersection) / len(union) if union else 1.0,
            "sharedEndpointRadiusCells": SHARED_ENDPOINT_RADIUS_CELLS,
        })

    return {
        "schema": 1,
        "kind": "S026StaticOwnerCenterlineOverlapComparison",
        "status": "ComparedOwnerCenterlines_Unqualified",
        "inputs": {
            "r38ReceiptPath": str(R38_REL),
            "r38ReceiptSha256": R38_SHA256,
            "r2BuildReceiptPath": str(R2_BUILD_REL),
            "r2BuildReceiptSha256": R2_BUILD_SHA256,
            "r2CandidateEvidencePath": str(R2_CANDIDATE_REL),
            "r2CandidateEvidenceSha256": R2_CANDIDATE_SHA256,
            "r2OriginalRouteReceiptPath": original_receipt_rel,
            "r2OriginalRouteReceiptSha256": original_receipt_hash,
            "r2RefinedChainRouteReceiptPath": str(R38_REL),
            "r2RefinedChainRouteReceiptSha256": R38_SHA256,
            "r2CandidateBuilderPath": build["builderPath"],
            "r2CandidateBuilderSha256": build["builderSha256"],
            "sourcePinsVerified": pins_checked,
        },
        "comparison": {
            "primaryCandidate": "R38 facility-blocked TankPrimary",
            "refinedCandidate": "R2 independently assembled refined owner-segment chain",
            "pathRasterization": "Exact integer Bresenham loop copied from SkirmishGroundSourceFitSurvey.RouteIndependence.RasterizePilotCenterline",
            "endpointExclusion": (
                "Radial endpoint exclusion only: remove rasterized centerline cells within Euclidean radius 16 grid cells of either shared role endpoint. "
                "Authored endpoint zones are unavailable; this is looser than ThreeStaticInteriorCorridors.v1 and is not route-independence proof."
            ),
            "facilityBlockers": "ExactAcceptedStaticCandidateRectangles_NotLiveDynamicOccupancy",
            "dynamicBlockersIncluded": False,
            "liveOccupancyIncluded": False,
            "roadSemanticsUsed": False,
            "routeBindingExists": False,
            "routeProof": False,
            "sourceFitCertified": False,
            "physicalWidthMeasured": False,
            "normalMissionReady": False,
            "interpretation": "Static rasterized owner-query centerline overlap/unique-cell counts only; no route, width, fit, readiness, or independence claim.",
        },
        "roles": comparisons,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, help="New absolute temporary JSON output path")
    args = parser.parse_args()
    output = Path(args.output)
    require(output.is_absolute(), "Output path must be absolute")
    require(not output.exists(), f"Refusing to overwrite output: {output}")
    require(output.parent.is_dir(), f"Output parent directory must already exist: {output.parent}")
    result = summarize(ROOT)
    output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps({"status": result["status"], "output": str(output), "roles": len(result["roles"])}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

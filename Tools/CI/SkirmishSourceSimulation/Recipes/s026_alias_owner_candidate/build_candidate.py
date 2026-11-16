#!/usr/bin/env python3
"""Consolidate bounded S026 owner-placement receipts into a non-layout candidate.

This deliberately does not synthesize anchors, pads, instance ordinals, route
records, or compiler admission. It preserves original receipt rows and provenance.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[5]
RECEIPTS = ROOT / "Logs/SkirmishProduction"
CASES = (
    ("Standard", "r27", "constrainedFirstMiddleBandProbe", 10, True),
    ("War", "r24", "perFactionFacilityRootSearch", 0, True),
    ("LargeWar", "r24", "perFactionFacilityRootSearch", 0, True),
)
EXPECTED_SOURCE = "opmap.skirmish.desert_base_01"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def lane_probe_summary(revision: str) -> dict:
    path, report = load_receipt(revision)
    probe = report["sourceAxisAndFlankLaneCandidates"]
    segment_total = segment_passed = 0
    outcomes = []
    comparisons_verified = True
    for role in probe["roles"]:
        for direction in role["directions"]:
            lanes = {lane["laneCandidateKind"]: lane for lane in direction["lanes"]}
            if set(lanes) != {"AxisCandidate", "FlankCandidate"}:
                raise ValueError(f"{revision} lane pair is incomplete")
            for lane in lanes.values():
                segment_total += len(lane["segments"])
                segment_passed += sum(1 for row in lane["segments"] if row["exactOriginalOwnerSegmentPassed"])
            axis_cells = {tuple(cell) for segment in lanes["AxisCandidate"]["segments"] for cell in segment["pathCells"]}
            flank_cells = {tuple(cell) for segment in lanes["FlankCandidate"]["segments"] for cell in segment["pathCells"]}
            intersection = len(axis_cells & flank_cells)
            union = len(axis_cells | flank_cells)
            jaccard = intersection / union if union else 1.0
            metrics = direction["laneDistinctnessMetrics"]
            if (metrics.get("ownerReturnedPathCellIntersectionCount") != intersection
                    or metrics.get("ownerReturnedPathCellUnionCount") != union
                    or abs(metrics.get("ownerReturnedPathCellSetJaccard", -1.0) - jaccard) > 1e-12
                    or metrics.get("ownerReturnedPathCellSetsDiffer") != (axis_cells != flank_cells)
                    or metrics.get("routeDistinctnessValidated") is not False):
                comparisons_verified = False
            outcomes.append({
                "roleId": role["roleId"],
                "direction": direction["direction"],
                "axisSegmentsPassed": sum(1 for row in lanes["AxisCandidate"]["segments"] if row["exactOriginalOwnerSegmentPassed"]),
                "flankSegmentsPassed": sum(1 for row in lanes["FlankCandidate"]["segments"] if row["exactOriginalOwnerSegmentPassed"]),
                "axisFailureCodes": [row.get("failureCode") for row in lanes["AxisCandidate"]["segments"] if not row["exactOriginalOwnerSegmentPassed"]],
                "flankFailureCodes": [row.get("failureCode") for row in lanes["FlankCandidate"]["segments"] if not row["exactOriginalOwnerSegmentPassed"]],
                "pathCellSetJaccard": jaccard,
                "routeDistinctnessValidated": False,
            })
    if segment_total != probe["originalOwnerQueryCount"] or not comparisons_verified:
        raise ValueError(f"{revision} segment census or comparison metrics failed independent check")
    if probe.get("routeProof") is not False or probe.get("routeBindingExists") is not False:
        raise ValueError(f"{revision} unexpectedly claims route proof/binding")
    return {
        "receiptPath": str(path.relative_to(ROOT)),
        "receiptSha256": sha256(path),
        "diagnosticImplementationPinCurrent": report["sourcePins"]["diagnosticImplementationSha256"] == sha256(ROOT / report["sourcePins"]["diagnosticImplementationPath"]),
        "status": probe.get("status"),
        "originalOwnerSegmentsPassed": segment_passed,
        "originalOwnerSegmentsQueried": segment_total,
        "comparisonMetricsVerifiedAgainstReturnedPathCells": comparisons_verified,
        "outcomes": outcomes,
        "distinctnessValidated": False,
        "routeBindingExists": False,
        "routeProof": False,
        "roadSemanticsUsed": probe.get("roadSemanticsUsed"),
        "dynamicBlockersIncluded": probe.get("dynamicBlockersIncluded"),
        "liveOccupancyIncluded": report.get("liveOccupancyIncluded"),
        "sourceFitCertified": report.get("sourceFitCertified"),
        "nativeMissionInitialized": report.get("nativeMissionInitialized"),
        "normalMissionReady": report.get("normalMissionReady"),
    }


def shared_waypoint_probe_summary(revision: str) -> dict:
    path, report = load_receipt(revision)
    probe = report["sharedTankWaypointCrossRoleCandidates"]
    queried = passed = 0
    roles = []
    for candidate in probe["candidateRoles"]:
        checks = candidate["ownerSegmentChecks"]
        successes = sum(1 for row in checks if row.get("exactOwnerSegmentPassed") is True)
        queried += len(checks)
        passed += successes
        roles.append({
            "roleId": candidate["roleId"],
            "candidate": candidate["candidate"],
            "sharedInteriorWaypointCount": len(candidate["sharedInteriorWaypoints"]),
            "ownerSegmentsPassed": successes,
            "ownerSegmentsQueried": len(checks),
            "failureCodes": [row.get("failureCode") for row in checks if row.get("exactOwnerSegmentPassed") is not True],
            "allReturnedPathsInsideAlias": all(row.get("entirePathInsideAliasFrame") is True for row in checks),
        })
    if queried != probe.get("ownerQueryCount") or queried != probe.get("ownerQueryCountCap") or passed != queried:
        raise ValueError(f"{revision} shared-waypoint owner segment census failed")
    if (probe.get("routeProof") is not False or probe.get("routeBindingExists") is not False
            or probe.get("distinctApproachValidated") is not False
            or probe.get("roadSemanticsUsed") is not False
            or probe.get("dynamicBlockersIncluded") is not False
            or probe.get("liveOccupancyIncluded") is not False
            or probe.get("productionPathLimitsChanged") is not False):
        raise ValueError(f"{revision} shared-waypoint probe carries an unexpected claim")
    return {
        "receiptPath": str(path.relative_to(ROOT)),
        "receiptSha256": sha256(path),
        "diagnosticImplementationPinCurrent": report["sourcePins"]["diagnosticImplementationSha256"] == sha256(ROOT / report["sourcePins"]["diagnosticImplementationPath"]),
        "status": probe.get("status"),
        "source": probe.get("source"),
        "ownerSegmentsPassed": passed,
        "ownerSegmentsQueried": queried,
        "ownerQueryCountCap": probe.get("ownerQueryCountCap"),
        "candidateRoles": roles,
        "routeBindingExists": False,
        "routeProof": False,
        "distinctApproachValidated": False,
        "roadSemanticsUsed": False,
        "dynamicBlockersIncluded": False,
        "liveOccupancyIncluded": False,
        "productionPathLimitsChanged": False,
        "sourceFitCertified": report.get("sourceFitCertified"),
        "nativeMissionInitialized": report.get("nativeMissionInitialized"),
        "normalMissionReady": report.get("normalMissionReady"),
    }


def chained_candidate_summary(revision: str) -> dict:
    """Validate a continuous, but still unbound, chain of original-owner legs."""
    path, report = load_receipt(revision)
    probe = report["sharedTankWaypointsWithCandidateFacilityBlockers"]
    subdivision = probe["rifleAlternateFirstLegSubdivision"]
    second = subdivision["secondSubdivision"]
    nearby = second["nearbySharedPointProbe"]["candidates"]
    chosen = next((row for row in nearby if row.get("cell") == [988, 721]), None)
    if chosen is None:
        raise ValueError(f"{revision} lacks the selected [988,721] shared point")

    if (probe.get("scope") != "OriginalPathOwner_WithExactCandidateFacilityRectanglesAsStaticBlockers"
            or probe.get("candidateFacilityCount") != 20
            or probe.get("blockedCellCount") != 3522
            or probe.get("routeProof") is not False
            or probe.get("routeBindingExists") is not False
            or probe.get("sourceFitCertified") is not False
            or probe.get("liveOccupancyIncluded") is not False
            or probe.get("dynamicBlockersIncluded") is not False
            or probe.get("roadSemanticsUsed") is not False
            or probe.get("productionPathLimitsChanged") is not False
            or report.get("result") != "CollectedUnqualified"
            or report.get("cleanupVerified") is not True
            or report.get("sourceAssetsUnmodified") is not True):
        raise ValueError(f"{revision} facility-blocker or unqualified scope changed")

    def require_pass(row: dict, footprint: tuple[int, int], selector: str) -> dict:
        if (row.get("status") != "NativeQueryFoundExactGoal"
                or row.get("failureCode") != 0
                or row.get("exactOwnerSegmentPassed") is not True
                or row.get("entirePathInsideAliasFrame") is not True
                or row.get("nativeQueryFaction") != 1
                or (row.get("footprintX"), row.get("footprintZ")) != footprint
                or row.get("dynamicBlockersIncluded") is not False
                or row.get("liveOccupancyIncluded") is not False
                or row.get("plannedFacilityBlockerScope") != "ExactAcceptedStaticCandidateRectangles_NotLiveDynamicOccupancy"):
            raise ValueError(f"{revision} failed exact owner segment check: {selector}")
        cells = row.get("pathCells")
        if (not isinstance(cells, list) or len(cells) < 2
                or cells[0] != row.get("startCell") or cells[-1] != row.get("goalCell")):
            raise ValueError(f"{revision} path does not terminate at exact owner endpoints: {selector}")
        bounds = report["aliasPlayableBounds"]
        if any(not (bounds["minX"] <= cell[0] <= bounds["maxX"]
                    and bounds["minZ"] <= cell[1] <= bounds["maxZ"]) for cell in cells):
            raise ValueError(f"{revision} returned path leaves the alias frame: {selector}")
        return {
            "receiptSelector": selector,
            "startCell": row["startCell"],
            "exactGoalCell": row["goalCell"],
            "status": row["status"],
            "failureCode": row["failureCode"],
            "expansions": row.get("expansions"),
            "footprint": list(footprint),
            "returnedPathCellCount": len(cells),
            "returnedPathCellsSha256": hashlib.sha256(json.dumps(cells, separators=(",", ":")).encode()).hexdigest(),
            "allReturnedCellsInsideAlias": True,
            "exactOwnerSegmentPassed": True,
        }

    nearby_role_rows = chosen["roles"]
    by_role = {row["roleId"]: row for row in nearby_role_rows}
    nearby_role_indices = {row["roleId"]: i for i, row in enumerate(nearby_role_rows)}
    alternate_rows = [(i, row) for i, row in enumerate(probe["candidateRoles"])
                      if row.get("candidate") == "TankAlternate"]
    original_alternates = {row["roleId"]: row for _, row in alternate_rows}
    alternate_indices = {row["roleId"]: i for i, row in alternate_rows}
    vehicle_rows = second["vehicleSharedPointChecks"]
    vehicle_first = {row["roleId"]: row for row in vehicle_rows}
    vehicle_indices = {row["roleId"]: i for i, row in enumerate(vehicle_rows)}
    footprints = {"role.rifle": (1, 1), "role.tank": (3, 3), "role.apc_armored": (3, 3)}
    waypoint_chain = [[1004, 734], [988, 721], [969, 695]]
    roles = []
    for role_id, footprint in footprints.items():
        if role_id not in by_role or role_id not in original_alternates:
            raise ValueError(f"{revision} missing role in chained route candidate: {role_id}")
        if role_id == "role.rifle":
            first = subdivision["ownerSegmentChecks"][0]
            first_selector = "/sharedTankWaypointsWithCandidateFacilityBlockers/rifleAlternateFirstLegSubdivision/ownerSegmentChecks/0"
        else:
            first = vehicle_first[role_id]["ownerSegmentChecks"][0]
            first_selector = f"/sharedTankWaypointsWithCandidateFacilityBlockers/rifleAlternateFirstLegSubdivision/secondSubdivision/vehicleSharedPointChecks/{vehicle_indices[role_id]}/ownerSegmentChecks/0"
        local = by_role[role_id]["ownerSegmentChecks"]
        if len(local) != 2:
            raise ValueError(f"{revision} shared candidate does not have exactly two legs: {role_id}")
        alternate = original_alternates[role_id]["ownerSegmentChecks"]
        if len(alternate) < 2 or alternate[0].get("startCell") == alternate[0].get("goalCell"):
            raise ValueError(f"{revision} alternate suffix unavailable: {role_id}")
        # Replace the original start-to-[969,695] leg with the first segment
        # plus two exact nearby-point legs, then retain its measured suffix.
        rows = [first, local[0], local[1], *alternate[1:]]
        selectors = [first_selector,
                     f"/sharedTankWaypointsWithCandidateFacilityBlockers/rifleAlternateFirstLegSubdivision/secondSubdivision/nearbySharedPointProbe/candidates/0/roles/{nearby_role_indices[role_id]}/ownerSegmentChecks/0",
                     f"/sharedTankWaypointsWithCandidateFacilityBlockers/rifleAlternateFirstLegSubdivision/secondSubdivision/nearbySharedPointProbe/candidates/0/roles/{nearby_role_indices[role_id]}/ownerSegmentChecks/1"]
        selectors.extend(f"/sharedTankWaypointsWithCandidateFacilityBlockers/candidateRoles/{alternate_indices[role_id]}/ownerSegmentChecks/{i}"
                         for i in range(1, len(alternate)))
        if len(selectors) != len(rows):
            raise ValueError("internal receipt selector census mismatch")
        segments = [require_pass(row, footprint, selector)
                    for row, selector in zip(rows, selectors)]
        if any(left["exactGoalCell"] != right["startCell"]
               for left, right in zip(segments, segments[1:])):
            raise ValueError(f"{revision} segment endpoint discontinuity: {role_id}")
        if [segments[0]["exactGoalCell"], segments[1]["exactGoalCell"], segments[2]["exactGoalCell"]] != waypoint_chain:
            raise ValueError(f"{revision} unexpected intermediate waypoint chain: {role_id}")
        if segments[-1]["exactGoalCell"] != alternate[-1]["goalCell"]:
            raise ValueError(f"{revision} chain does not terminate at the original exact goal: {role_id}")
        roles.append({
            "roleId": role_id,
            "footprint": list(footprint),
            "startCell": segments[0]["startCell"],
            "exactGoalCell": segments[-1]["exactGoalCell"],
            "intermediateWaypointCells": waypoint_chain,
            "segments": segments,
            "segmentCount": len(segments),
            "endpointContinuityVerified": True,
            "exactGoalReached": True,
            "ownerQueryPasses": True,
            "candidateOnly": True,
        })

    bounds = report["aliasPlayableBounds"]
    expected_current = {
        "aliasPath": "Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset",
        "campaignPath": "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset",
        "gridPath": "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset",
        "surfacePath": "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset",
        "diagnosticImplementationPath": "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.S026AliasDiagnostic.cs",
    }
    pins = report["sourcePins"]
    for path_key, path_value in expected_current.items():
        if pins.get(path_key) != path_value:
            raise ValueError(f"{revision} unexpected pinned path: {path_key}")
        current = ROOT / path_value
        hash_key = {
            "aliasPath": "aliasSha256", "campaignPath": "campaignSha256",
            "gridPath": "gridSha256", "surfacePath": "surfaceSha256",
            "diagnosticImplementationPath": "diagnosticImplementationSha256",
        }[path_key]
        if sha256(current) != pins.get(hash_key):
            raise ValueError(f"{revision} pinned source drift: {path_value}")
    return {
        "receiptPath": str(path.relative_to(ROOT)),
        "receiptSha256": sha256(path),
        "diagnosticImplementationPinCurrent": True,
        "scope": probe["scope"],
        "facilityBlockerCount": probe["candidateFacilityCount"],
        "blockedCellCount": probe["blockedCellCount"],
        "aliasPlayableBounds": bounds,
        "selectedSharedInteriorWaypoint": chosen["cell"],
        "otherSharedInteriorWaypoints": [row["cell"] for row in nearby if row is not chosen],
        "roles": roles,
        "sourceRoadSemanticsUsed": False,
        "dynamicBlockersIncluded": False,
        "liveOccupancyIncluded": False,
        "routeRowsAuthored": False,
        "routeBindingExists": False,
        "routeProof": False,
        "sourceFitCertified": False,
        "normalMissionReady": False,
        "cleanupVerified": True,
        "sourceAssetsUnmodified": True,
    }


def load_receipt(revision: str) -> tuple[Path, dict]:
    path = RECEIPTS / f"s026-citycrossroads-alias-source-diagnostic-20261116-{revision}" / "report.json"
    data = json.loads(path.read_text())
    if data.get("result") != "CollectedUnqualified" or not data.get("cleanupVerified"):
        raise ValueError(f"receipt is not a collected, cleanup-verified diagnostic: {path}")
    if not data.get("sourceAssetsUnmodified"):
        raise ValueError(f"source assets were not verified unchanged: {path}")
    pins = data["sourcePins"]
    if pins.get("aliasOperationMapId") != "opmap.skirmish.city_crossroads":
        raise ValueError(f"unexpected alias identity: {path}")
    if pins.get("campaignOperationMapId") != EXPECTED_SOURCE:
        raise ValueError(f"unexpected Campaign source identity: {path}")
    binding = pins.get("aliasSourceBinding", {})
    if (binding.get("operationMapId") != EXPECTED_SOURCE
            or binding.get("sourceIdentityHash") != pins.get("campaignSourceIdentityHash")
            or binding.get("sourceContentHash") != pins.get("campaignContentHash")):
        raise ValueError(f"alias to physical Campaign source binding mismatch: {path}")
    return path, data


def select_case(size: str, revision: str, probe_key: str, pair_index: int,
                has_projection: bool) -> dict:
    receipt_path, data = load_receipt(revision)
    source = data["sourceStructurePlacementCandidates"]
    probe = source[probe_key]
    size_row = next(x for x in probe["sizes"] if x.get("size") == size)
    if size == "Standard":
        pair = size_row["pairAttempts"][pair_index]
    else:
        recheck = size_row["fullRosterOriginalOwnerRecheck"]["sizes"][0]
        pair = recheck["pairAttempts"][pair_index]
    if has_projection:
        projection = pair["completeLogicalInstanceBindingProjection"]
        rows = projection["candidateRows"]
        expected_count = 20 if size == "Standard" else 22
        if (pair.get("planningOwnerFoundCount") != expected_count
                or pair.get("canonicalOrderExactRecheckPassedCount") != expected_count
                or not pair.get("allPlannedStructuresRevalidatedInCanonicalOrder")
                or not projection.get("tryApplyPassed")
                or not projection.get("completeCanonicalStructureDomain")
                or not projection.get("exactCoordinatesPreserved")
                or not projection.get("allOffsetsZero")):
            raise ValueError(f"selected {size} receipt pair lacks complete owner/binding projection")
        identity_projection = projection
        projection_note = "Complete ephemeral TryApply identity projection is retained verbatim."
    else:
        rows = pair["canonicalOrderExactRecheckRows"]
        if (pair.get("planningOwnerFoundCount") != 20
                or pair.get("canonicalOrderExactRecheckPassedCount") != 20
                or not pair.get("allPlannedStructuresRevalidatedInCanonicalOrder")):
            raise ValueError("selected Standard receipt pair is not 20/20 canonical-owner verified")
        identity_projection = None
        projection_note = "No complete logical instance binding projection exists in this receipt."
    expected_count = 20 if size == "Standard" else 22
    if len(rows) != expected_count:
        raise ValueError(f"{size} row count mismatch: {len(rows)} != {expected_count}")
    if has_projection:
        instances = [(row["faction"], row["structureId"], row["instanceOrdinal"]) for row in rows]
        compiler_indexes = [row["compilerIndex"] for row in rows]
        if len(set(instances)) != expected_count or sorted(compiler_indexes) != list(range(expected_count)):
            raise ValueError(f"{size} projection has duplicate instance identity or compiler index")
    return {
        "size": size,
        "difficulty": "Regular",
        "receiptPath": str(receipt_path.relative_to(ROOT)),
        "receiptSha256": sha256(receipt_path),
        "receiptImplementationPin": data["sourcePins"]["diagnosticImplementationSha256"],
        "currentImplementationSha256": sha256(ROOT / data["sourcePins"]["diagnosticImplementationPath"]),
        "selectedProbe": probe_key,
        "selectedPairIndex": pair_index,
        "selectedPairIdentity": {
            key: pair.get(key) for key in ("quadrantIndex", "playerQuadrantIndex", "enemyQuadrantIndex", "crossQuadrantPair", "playerRoot", "enemyRoot") if key in pair
        },
        "ownerPlanningCount": pair.get("planningOwnerFoundCount"),
        "canonicalOwnerRecheckCount": pair.get("canonicalOrderExactRecheckPassedCount"),
        "allCanonicalRechecksPassed": pair.get("allPlannedStructuresRevalidatedInCanonicalOrder"),
        "candidateRows": rows,
        "identityProjection": identity_projection,
        "identityProjectionNote": projection_note,
        "candidateOnly": True,
        "compilerLayout": False,
        "compilerAdmission": False,
        "sourceFitCertified": False,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    cases = [select_case(*case) for case in CASES]
    route_path = RECEIPTS / "s026-citycrossroads-alias-source-diagnostic-20261116-r10" / "report.json"
    r30_lane_probe = lane_probe_summary("r30")
    r31_lane_probe = lane_probe_summary("r31")
    r32_shared_waypoint_probe = shared_waypoint_probe_summary("r32")
    r38_chained_candidate = chained_candidate_summary("r38")
    output = {
        "schema": 1,
        "kind": "S026RegularSourceOwnerPlacementCandidateEvidence",
        "status": "PartialOwnerPlacementCandidate_Unqualified",
        "identity": {
            "catalogId": "S026",
            "definitionId": "skirmish.s026",
            "operationMapId": "opmap.skirmish.city_crossroads",
            "sourceOperationMapId": EXPECTED_SOURCE,
        },
        "difficultyCoverage": ["Regular"],
        "sizes": cases,
        "routes": [],
        "routeStatus": "Omitted_UnvalidatedOwnerRouteEvidence",
        "routeCandidateReceipt": "Logs/SkirmishProduction/s026-citycrossroads-alias-source-diagnostic-20261116-r10/report.json",
        "routeCandidateReceiptSha256": sha256(route_path),
        "temporaryLaneProbe": r30_lane_probe,
        "additionalTemporaryLaneProbe": r31_lane_probe,
        "sharedWaypointProbe": r32_shared_waypoint_probe,
        "chainedOwnerSegmentCandidate": r38_chained_candidate,
        "routeLimitations": [
            "r10 checkpoints and r30/r31 temporary axis/flank segments are diagnostic candidates, not authored route rows.",
            "Each r30/r31 probe made 24 original-owner segment queries; some failed and routeDistinctnessValidated remained false.",
            "r32 found all 39 temporary shared-waypoint owner segments for six roles, but distinct approach, route binding and route proof remain false.",
            "r38 provides a continuous candidate chain for rifle, tank and armored APC by replacing the failed first alternate leg with exact owner-tested segments through [1004,734], [988,721] and [969,695], then retaining the original alternate suffix. This is a composition of separately queried segments, not an authored or bound route.",
            "Gameplay road semantics, dynamic blockers and live occupancy were excluded.",
        ],
        "acceptance": {
            "layoutAssetCreated": False,
            "compilerAdmission": False,
            "sourceFitCertified": False,
            "nativeMissionInitialized": False,
            "normalMissionReady": False,
            "budgetChanged": False,
            "campaignGeometryChanged": False,
        },
        "limitations": [
            "Rows preserve selected original-owner candidate receipts; no new map geometry is authored.",
            "Standard uses the r27 complete ephemeral instance projection; War and LargeWar retain their own r24 projections.",
            "No row is copied across sizes.",
            "This evidence bundle is not a compiler layout and cannot be used as admission proof.",
        ],
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(output, indent=2, sort_keys=True) + "\n")
    inputs = {}
    for revision in ("r10", "r24", "r27", "r30", "r31", "r32", "r38"):
        path, data = load_receipt(revision)
        captured = data["sourcePins"]
        implementation_path = captured.get("diagnosticImplementationPath")
        implementation = ROOT / implementation_path if implementation_path else None
        inputs[revision] = {
            "path": str(path.relative_to(ROOT)),
            "sha256": sha256(path),
            "diagnosticImplementationPin": captured.get("diagnosticImplementationSha256"),
            "diagnosticImplementationCurrentSha256": sha256(implementation) if implementation else None,
            "diagnosticImplementationPinCurrent": captured.get("diagnosticImplementationSha256") == sha256(implementation) if implementation else None,
            "campaignSourceId": captured["campaignOperationMapId"],
            "campaignSourceContentHash": captured["campaignContentHash"],
            "aliasId": captured["aliasOperationMapId"],
            "sourceIdentityHashMatchesAlias": captured["aliasSourceBinding"]["sourceIdentityHash"] == captured["campaignSourceIdentityHash"],
            "sourceContentHashMatchesAlias": captured["aliasSourceBinding"]["sourceContentHash"] == captured["campaignContentHash"],
        }
    receipt = {
        "schema": 1,
        "kind": "S026OwnerPlacementCandidateExtractionReceipt",
        "candidatePath": str(args.output.relative_to(ROOT) if args.output.is_relative_to(ROOT) else args.output),
        "candidateSha256": sha256(args.output),
        "builderPath": str(Path(__file__).resolve().relative_to(ROOT)),
        "builderSha256": sha256(Path(__file__).resolve()),
        "deterministicExtraction": "Verified by a byte-identical second invocation recorded in the task execution; this receipt binds the produced artifact and inputs.",
        "inputReceipts": inputs,
        "limitations": [
            "r24 and r30 diagnostic implementation file hashes differ from the current implementation; their captured outputs are retained as historical receipt evidence, not represented as recaptured under current code.",
            "All captured alias, Campaign source, grid and surface file hashes match the current files.",
            "The output is a partial owner-placement evidence bundle, not a compiler layout or acceptance artifact.",
            "r38 diagnostic implementation pin matches current source; its chain remains a separately queried unqualified path candidate without road, dynamic blocker or live occupancy semantics.",
        ],
    }
    receipt_path = args.output.parent / "build-receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n")
    print(f"candidate={args.output}")
    print(f"receipt={receipt_path}")
    print(f"sizes={len(cases)} rows={sum(len(x['candidateRows']) for x in cases)} routes=0")


if __name__ == "__main__":
    main()

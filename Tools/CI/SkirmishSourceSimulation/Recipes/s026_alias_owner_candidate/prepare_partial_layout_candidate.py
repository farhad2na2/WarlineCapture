#!/usr/bin/env python3
"""Serialize a hash-pinned, incomplete S026 source-derived layout candidate.

The output preserves the original owner's ephemeral building projection and
the separately queried r38 waypoint chain. An r39 diagnostic replay is
recorded as separate candidate-only spawn-roster evidence; it does not replace
r38 route provenance or establish typed pad geometry. It intentionally emits
no route records, compiler qualification, fit result, or runtime/readiness claim.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path


ROOT = Path(__file__).resolve().parents[5]
OUTER = ROOT / "Logs/SkirmishProduction/s026-regular-owner-placement-candidate-20261116-r2"
CANDIDATE = OUTER / "candidate-evidence.json"
BUILD_RECEIPT = OUTER / "build-receipt.json"
R38 = ROOT / "Logs/SkirmishProduction/s026-citycrossroads-alias-source-diagnostic-20261116-r38/report.json"
R39 = ROOT / "Logs/SkirmishProduction/s026-citycrossroads-alias-source-diagnostic-20261116-r39/report.json"
EXPECTED_CANDIDATE_SHA = "e93582eaa0f71ffcddd62d98d297831e6fa1ea08cae3a1b9253401127d811bfa"
EXPECTED_BUILD_RECEIPT_SHA = "da36220caec2b72223d1d2be2be231d74b599f3c01e54642a3ba47bafc6f4237"
EXPECTED_R38_SHA = "cafa3958d61f972c58ad1cc2af4a2c021361005e6f97502c49929096ca918103"
EXPECTED_R38_IMPLEMENTATION_SHA = "62a8c27b14fd846014f9356022781016193446e823cfbbdd0c15abef405d6560"
EXPECTED_R39_SHA = "bb20f7d6ab45afdc854efd8757ea95127ab591bc0ac9bac4cb82dced77c6a6e9"
EXPECTED_R39_IMPLEMENTATION_SHA = "dcfc5d4888afdae229b2c920f2c5e05f652f9284a3be7dd8e593577d4ff045d7"
SIZE_IDS = {"Standard": 1, "War": 2, "LargeWar": 3}
DIFFICULTY_ID = 2  # Regular
PAD_KIND = {
    "building.ground_staging": 2,  # GroundStaging
    "building.helipad": 6,  # AirReturn
    "building.airport": 6,
    "building.oil_pump": 7,  # Supply
    "building.refinery": 7,
    "building.refinery.module": 7,
    "building.fuel_bladder": 7,
    "building.fabrication_depot": 7,
    "building.watchtower": 8,  # Service
    "building.watchtower.approach": 8,
    "building.satellite_dish": 8,
}
DEFAULT_PAD_KIND = 1  # PadKindForStructure's canonical fallback: BaseBarracks


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_json(path: Path) -> dict:
    return json.loads(path.read_bytes())


def descriptor(path: Path) -> dict:
    path = path.resolve(strict=True)
    return {"path": str(path.relative_to(ROOT)), "sha256": sha256(path), "bytes": path.stat().st_size}


def spawn_roster_summary(report: dict) -> list[dict]:
    rows = report.get("candidatePlacements")
    if not isinstance(rows, list) or not rows:
        raise ValueError("Candidate spawn roster rows are missing")
    fields = (
        "faction", "side", "roleId", "role", "requestedCount", "scope",
        "candidateOnly", "normalSpawnAcceptance", "liveOccupancyIncluded",
        "nativePrefabKey", "nativeFootprint", "foundCount",
        "sourceSurfaceTraversableCount", "status",
    )
    summary = [{key: row.get(key) for key in fields} for row in rows]
    summary.sort(key=lambda row: (row["faction"], row["roleId"]))
    if any(row["candidateOnly"] is not True
           or row["normalSpawnAcceptance"] is not False
           or row["liveOccupancyIncluded"] is not False
           or row["foundCount"] != row["requestedCount"]
           or row["sourceSurfaceTraversableCount"] != row["requestedCount"]
           for row in summary):
        raise ValueError("Spawn rows are not complete candidate-only source measurements")
    return summary


def load_inputs() -> tuple[dict, dict, dict, dict, list[dict]]:
    for path, expected in ((CANDIDATE, EXPECTED_CANDIDATE_SHA),
                           (BUILD_RECEIPT, EXPECTED_BUILD_RECEIPT_SHA),
                           (R38, EXPECTED_R38_SHA), (R39, EXPECTED_R39_SHA)):
        if sha256(path) != expected:
            raise ValueError(f"Pinned source evidence changed: {path}")
    evidence, build = read_json(CANDIDATE), read_json(BUILD_RECEIPT)
    route, roster = read_json(R38), read_json(R39)
    if (build.get("candidateSha256") != EXPECTED_CANDIDATE_SHA
            or build.get("candidatePath") != str(CANDIDATE.relative_to(ROOT))
            or build.get("inputReceipts", {}).get("r38", {}).get("sha256") != EXPECTED_R38_SHA
            or evidence.get("kind") != "S026RegularSourceOwnerPlacementCandidateEvidence"
            or evidence.get("status") != "PartialOwnerPlacementCandidate_Unqualified"
            or evidence.get("routes") != []
            or evidence.get("acceptance", {}).get("compilerAdmission") is not False
            or evidence.get("acceptance", {}).get("normalMissionReady") is not False):
        raise ValueError("Exact partial source-owner evidence bundle required")
    if (route.get("kind") != "S026CityCrossroadsAliasSourceOwnerDiagnostic"
            or route.get("result") != "CollectedUnqualified"
            or route.get("cleanupVerified") is not True
            or route.get("sourceAssetsUnmodified") is not True):
        raise ValueError("Exact collected and cleanup-verified r38 receipt required")
    historical_pins = route["sourcePins"]
    current_pins = roster.get("sourcePins", {})
    if (historical_pins.get("diagnosticImplementationPath")
            != current_pins.get("diagnosticImplementationPath")
            or historical_pins.get("diagnosticImplementationSha256")
            != EXPECTED_R38_IMPLEMENTATION_SHA
            or current_pins.get("diagnosticImplementationSha256")
            != EXPECTED_R39_IMPLEMENTATION_SHA):
        raise ValueError("Historical r38/current r39 diagnostic implementation provenance mismatch")
    for key, value in historical_pins.items():
        if key in ("diagnosticImplementationPath", "diagnosticImplementationSha256"):
            continue
        if current_pins.get(key) != value:
            raise ValueError(f"r38/r39 source input pin differs: {key}")
    stable_fields = ("aliasPlayableBounds", "nativeSourceGrid", "deploymentAnchors",
                     "standardBudget", "forceRequests", "sourceStructurePlacementCandidates")
    for key in stable_fields:
        if route.get(key) != roster.get(key):
            raise ValueError(f"r38/r39 source-derived input differs: {key}")
    if (roster.get("kind") != "S026CityCrossroadsAliasSourceOwnerDiagnostic"
            or roster.get("result") != "CollectedUnqualified"
            or roster.get("cleanupVerified") is not True
            or roster.get("sourceAssetsUnmodified") is not True
            or roster.get("scope") != "ExistingCampaignSource_AliasFrame_StandardOnly"
            or roster.get("sourceFitCertified") is not False
            or roster.get("allSizeCapacityProven") is not False
            or roster.get("nativeMissionInitialized") is not False
            or roster.get("liveOccupancyIncluded") is not False
            or roster.get("normalMissionReady") is not False):
        raise ValueError("Exact unqualified, cleanup-verified r39 spawn diagnostic required")
    r38_roster = spawn_roster_summary(route)
    r39_roster = spawn_roster_summary(roster)
    if r38_roster != r39_roster:
        raise ValueError("r38/r39 spawn roster inputs or candidate counts differ")
    identity = evidence.get("identity", {})
    alias_binding = current_pins.get("aliasSourceBinding", {})
    if (identity.get("catalogId") != "S026"
            or identity.get("definitionId") != "skirmish.s026"
            or identity.get("operationMapId") != current_pins.get("aliasOperationMapId")
            or identity.get("sourceOperationMapId") != alias_binding.get("operationMapId")
            or alias_binding.get("operationMapId") != current_pins.get("campaignOperationMapId")
            or alias_binding.get("sourceIdentityHash") != current_pins.get("campaignSourceIdentityHash")
            or alias_binding.get("sourceContentHash") != current_pins.get("campaignContentHash")):
        raise ValueError("Alias, Campaign source, and candidate identity closure differs")
    pin_paths = {
        "aliasPath": "aliasSha256", "campaignPath": "campaignSha256",
        "gridPath": "gridSha256", "surfacePath": "surfaceSha256",
    }
    for path_key, hash_key in pin_paths.items():
        path = ROOT / current_pins[path_key]
        if sha256(path) != current_pins[hash_key]:
            raise ValueError(f"Current source pin drift: {path}")
    implementation_path = ROOT / current_pins["diagnosticImplementationPath"]
    if sha256(implementation_path) != EXPECTED_R39_IMPLEMENTATION_SHA:
        raise ValueError(f"Current r39 diagnostic implementation pin drift: {implementation_path}")
    return evidence, build, route, roster, r39_roster


def anchor_row(anchor_id: str, role: str, world_x: float, world_z: float,
               bounds: dict, provenance: dict) -> tuple[dict, dict]:
    width = bounds["maxX"] - bounds["minX"]
    depth = bounds["maxZ"] - bounds["minZ"]
    if (not all(math.isfinite(v) for v in (world_x, world_z, width, depth))
            or width <= 0 or depth <= 0
            or not bounds["minX"] <= world_x <= bounds["maxX"]
            or not bounds["minZ"] <= world_z <= bounds["maxZ"]):
        raise ValueError(f"Candidate anchor leaves the pinned alias bounds: {anchor_id}")
    return ({"AnchorId": anchor_id, "RoleId": role,
             "NormalizedU": (world_x - bounds["minX"]) / width,
             "NormalizedV": (world_z - bounds["minZ"]) / depth,
             "WorldX": world_x, "WorldZ": world_z},
            {"anchorId": anchor_id, **provenance})


def layout_for_size(size: str, evidence: dict, route: dict) -> tuple[dict, dict]:
    case = next((row for row in evidence["sizes"] if row.get("size") == size), None)
    if case is None or case.get("difficulty") != "Regular":
        raise ValueError(f"Missing Regular candidate case: {size}")
    projection = case.get("identityProjection")
    rows = case.get("candidateRows")
    expected_count = 20 if size == "Standard" else 22
    if (not isinstance(projection, dict) or not isinstance(rows, list)
            or projection.get("tryApplyPassed") is not True
            or projection.get("completeCanonicalStructureDomain") is not True
            or projection.get("exactCoordinatesPreserved") is not True
            or projection.get("allOffsetsZero") is not True
            or projection.get("sourceFitCertified") is not False
            or projection.get("compilerAdmission") is not False
            or projection.get("candidateRows") != rows
            or len(rows) != expected_count):
        raise ValueError(f"{size} projection is incomplete or qualified unexpectedly")
    if sorted(row.get("compilerIndex") for row in rows) != list(range(expected_count)):
        raise ValueError(f"{size} compiler order is not complete")
    identities = [(row.get("faction"), row.get("structureId"), row.get("instanceOrdinal")) for row in rows]
    if len(set(identities)) != expected_count:
        raise ValueError(f"{size} structure instance identities are not unique")

    pins = route["sourcePins"]
    bounds = route["aliasPlayableBounds"]
    grid = route["nativeSourceGrid"]
    if (grid.get("cellSize") != 1.0 or grid.get("origin") != [0.0, 0.0]
            or grid.get("scope") != "ActualExistingCampaignBakedGridAndSurface_NoGameplaySystems"):
        raise ValueError("Pinned native grid frame changed")
    anchors, pads, bindings = [], [], []
    anchor_provenance = []
    deployments = sorted(route["deploymentAnchors"], key=lambda row: row["faction"])
    if len(deployments) != 2 or [row["faction"] for row in deployments] != [1, 2]:
        raise ValueError("Exact two Campaign-authored faction deployment anchors required")
    for row in deployments:
        anchor, provenance = anchor_row(
            row["anchorId"], "base.player" if row["faction"] == 1 else "base.enemy",
            row["worldX"], row["worldZ"], bounds,
            {"sourceKind": "CampaignAuthoredDeploymentAnchor", "sourceReceipt": EXPECTED_R38_SHA,
             "faction": row["faction"], "sourceAuthoredRadius": row["sourceAuthoredRadius"]})
        anchors.append(anchor)
        anchor_provenance.append(provenance)

    for row in rows:
        footprint = row.get("effectiveFootprintCells")
        center = row.get("centerWorldXZ")
        faction, structure = row.get("faction"), row.get("structureId")
        if (not isinstance(footprint, list) or len(footprint) != 2
                or any(type(value) is not int or value <= 0 for value in footprint)
                or not isinstance(center, list) or len(center) != 2
                or not all(isinstance(value, (int, float)) and math.isfinite(value) for value in center)
                or faction not in (1, 2) or row.get("instanceOrdinal") != 0
                or row.get("offsetsZeroedByExistingBinding") is not True):
            raise ValueError(f"Malformed owner-derived structure placement row: {size}/{row.get('compilerIndex')}")
        width_cells, depth_cells = footprint
        if (abs((center[0] - width_cells * .5) - round(center[0] - width_cells * .5)) > 1e-6
                or abs((center[1] - depth_cells * .5) - round(center[1] - depth_cells * .5)) > 1e-6):
            raise ValueError(f"Owner placement center is not aligned to source cell footprint: {size}/{structure}")
        anchor_id, pad_id = row["anchorId"], row["padId"]
        role_id = f"candidate.structure.{size.lower()}.{row['compilerIndex']}"
        anchor, provenance = anchor_row(
            anchor_id, role_id, float(center[0]), float(center[1]), bounds,
            {"sourceKind": "OriginalOwnerResolvedStructurePlacement",
             "sourceReceipt": descriptor(CANDIDATE), "size": size,
             "difficulty": "Regular", "compilerIndex": row["compilerIndex"],
             "faction": faction, "structureId": structure,
             "instanceOrdinal": row["instanceOrdinal"],
             "effectiveFootprintCells": footprint})
        anchors.append(anchor)
        anchor_provenance.append(provenance)
        pads.append({"PadId": pad_id, "AnchorId": anchor_id, "RoleId": role_id,
                     "Kind": PAD_KIND.get(structure, DEFAULT_PAD_KIND),
                     "CenterX": float(center[0]), "CenterZ": float(center[1]),
                     "WidthMetres": float(width_cells), "DepthMetres": float(depth_cells),
                     "FactionId": faction})
        bindings.append({"SizeId": SIZE_IDS[size], "DifficultyId": DIFFICULTY_ID,
                         "FactionId": faction, "StructureId": structure,
                         "InstanceOrdinal": row["instanceOrdinal"],
                         "AnchorId": anchor_id, "PadId": pad_id})

    chain = evidence.get("chainedOwnerSegmentCandidate")
    if (not isinstance(chain, dict) or chain.get("receiptSha256") != EXPECTED_R38_SHA
            or chain.get("routeRowsAuthored") is not False
            or chain.get("routeBindingExists") is not False
            or chain.get("routeProof") is not False):
        raise ValueError("Exact unqualified r38 chain summary required")
    expected_roles = {"role.rifle", "role.tank", "role.apc_armored"}
    if ({row.get("roleId") for row in chain.get("roles", [])} != expected_roles
            or any(row.get("ownerQueryPasses") is not True
                   or row.get("endpointContinuityVerified") is not True
                   or row.get("exactGoalReached") is not True
                   or row.get("candidateOnly") is not True for row in chain["roles"])):
        raise ValueError("r38 exact-role chain summary is incomplete or overclaimed")
    shared_cells = chain.get("roles", [])[0].get("intermediateWaypointCells", []) if chain.get("roles") else []
    if shared_cells != [[1004, 734], [988, 721], [969, 695]]:
        raise ValueError("r38 shared waypoint chain differs from validated receipt")
    for i, cell in enumerate(shared_cells):
        anchor_id = f"anchor.s026.candidate.route.shared.{i}"
        anchor, provenance = anchor_row(
            anchor_id, "candidate.route.waypoint", cell[0] + .5, cell[1] + .5, bounds,
            {"sourceKind": "OriginalOwnerSharedWaypoint", "sourceReceipt": EXPECTED_R38_SHA,
             "sourceCell": cell, "sharedWaypointOrdinal": i})
        anchors.append(anchor)
        anchor_provenance.append(provenance)
    role_chains = []
    for role in chain["roles"]:
        start_cell, goal_cell = role["startCell"], role["exactGoalCell"]
        role_id = role["roleId"].replace("role.", "")
        role_refs = []
        for label, cell in (("start", start_cell), ("goal", goal_cell)):
            anchor_id = f"anchor.s026.candidate.route.{role_id}.{label}"
            anchor, provenance = anchor_row(
                anchor_id, f"candidate.route.{role_id}.{label}", cell[0] + .5, cell[1] + .5, bounds,
                {"sourceKind": "OriginalOwnerRolePathEndpoint", "sourceReceipt": EXPECTED_R38_SHA,
                 "roleId": role["roleId"], "sourceCell": cell,
                 "endpointKind": label, "candidateOnly": True})
            anchors.append(anchor)
            anchor_provenance.append(provenance)
            role_refs.append(anchor_id)
        role_chains.append({"roleId": role["roleId"], "startAnchorId": role_refs[0],
                            "intermediateWaypointAnchorIds": [
                                f"anchor.s026.candidate.route.shared.{i}" for i in range(3)],
                            "goalAnchorId": role_refs[1], "sourceReceipt": EXPECTED_R38_SHA,
                            "ownerSegmentCount": role["segmentCount"],
                            "endpointContinuityVerified": role["endpointContinuityVerified"],
                            "exactGoalReached": role["exactGoalReached"],
                            "candidateOnly": True})

    player, enemy = deployments
    frame = {
        "OriginX": bounds["minX"],
        "OriginZ": bounds["minZ"] + (bounds["maxZ"] - bounds["minZ"]) * .5,
        "ForwardX": 1.0, "ForwardZ": 0.0,
        "AcrossX": 0.0, "AcrossZ": 1.0,
        "ForwardMetres": bounds["maxX"] - bounds["minX"],
        "AcrossMetres": bounds["maxZ"] - bounds["minZ"],
        "PlayerAnchorId": player["anchorId"], "EnemyAnchorId": enemy["anchorId"],
        "SourceHash": pins["aliasSourceBinding"]["sourceContentHash"],
        "HasMeasuredDeployments": False,
        "PlayerDeployment": {"x": player["worldX"], "y": player["worldZ"]},
        "EnemyDeployment": {"x": enemy["worldX"], "y": enemy["worldZ"]},
        "BindAllConfigurations": False,
    }
    layout = {
        "layoutId": f"layout.skirmish.s026.partial-source-candidate.{size.lower()}.regular",
        "operationMapId": pins["aliasOperationMapId"],
        "contentVersion": 1,
        "contentHash": "unqualified.s026.alias-source-owner-candidate.v1",
        "worldWidthMetres": bounds["maxX"] - bounds["minX"],
        "worldDepthMetres": bounds["maxZ"] - bounds["minZ"],
        "originX": bounds["minX"], "originZ": bounds["minZ"],
        "cellSize": grid["cellSize"],
        "anchors": anchors,
        "routes": [],
        "pads": pads,
        "worldBinding": frame,
        "bindingValueFormat": 2,
        "declaredConfigurations": [{
            "SizeId": SIZE_IDS[size], "DifficultyId": DIFFICULTY_ID,
            "PlayerDeployment": {"x": player["worldX"], "y": player["worldZ"]},
            "EnemyDeployment": {"x": enemy["worldX"], "y": enemy["worldZ"]},
        }],
        "structureInstancePads": bindings,
    }
    return layout, {"size": size, "difficulty": "Regular", "candidateLayout": layout,
                    "sourceDeploymentAnchors": [row["anchorId"] for row in [player, enemy]],
                    "waypointAnchorSources": anchor_provenance,
                    "candidateRoleChains": role_chains,
                    "candidateStructurePadCount": len(pads),
                    "candidateStructureBindingCount": len(bindings),
                    "routeCount": 0, "compilerLayout": False,
                    "compilerAdmission": False, "sourceFitCertified": False,
                    "nativeMissionInitialized": False, "normalMissionReady": False}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True, help="New repository-local output directory")
    args = parser.parse_args()
    output = args.output.resolve()
    if not output.is_relative_to(ROOT) or output.exists():
        raise ValueError("New repository-local output directory required")
    evidence, build, route, roster, roster_rows = load_inputs()
    layouts = [layout_for_size(size, evidence, route) for size in ("Standard", "War", "LargeWar")]
    artifact = {
        "schema": 1,
        "kind": "S026PartialSourceDerivedLayoutCandidate",
        "status": "PartialLayoutData_Unqualified",
        "identity": evidence["identity"],
        "difficultyCoverage": ["Regular"],
        "sourcePins": roster["sourcePins"],
        "historicalRouteSourcePins": route["sourcePins"],
        "inputReceipts": {
            "placementProjection": descriptor(CANDIDATE),
            "placementBuildReceipt": descriptor(BUILD_RECEIPT),
            "routeDiagnostic": descriptor(R38),
            "spawnRosterDiagnostic": descriptor(R39),
        },
        "layoutsBySize": [item[1] for item in layouts],
        "routeStatus": "NoRouteRows_WidthAndRouteIdentityUnspecified",
        "candidateRouteChains": evidence["chainedOwnerSegmentCandidate"],
        "spawnRosterCandidateEvidence": {
            "sourceReceipt": descriptor(R39),
            "currentDiagnosticImplementation": {
                "path": roster["sourcePins"]["diagnosticImplementationPath"],
                "sha256": roster["sourcePins"]["diagnosticImplementationSha256"],
            },
            "historicalRouteReceiptImplementation": {
                "path": route["sourcePins"]["diagnosticImplementationPath"],
                "sha256": route["sourcePins"]["diagnosticImplementationSha256"],
                "historicalOnly": True,
            },
            "scope": roster["scope"],
            "candidateOnly": True,
            "roleRows": roster_rows,
            "requestedActorCount": roster["spawnQueryTotals"]["requestedActors"],
            "foundCandidateActorCount": roster["spawnQueryTotals"]["foundActors"],
            "completeStandardRosterFound": roster["spawnQueryTotals"]["completeStandardRosterFound"],
            "normalSpawnAcceptance": False,
            "liveOccupancyIncluded": False,
            "completeRosterCapacityProven": False,
            "typedSpawnPadsDerived": False,
            "padRectanglesInferred": False,
            "padGeometrySource": None,
        },
        "claims": {
            "sourceDerivedStructurePads": True,
            "sourceDerivedWaypointAnchors": True,
            "compilerLayout": False,
            "compilerAccepted": False,
            "routeRowsAuthored": False,
            "routeBindingExists": False,
            "routeProof": False,
            "sourceFitCertified": False,
            "allSizeCapacityProven": False,
            "nativeMissionInitialized": False,
            "normalMissionReady": False,
            "budgetChanged": False,
            "campaignGeometryChanged": False,
        },
        "compilerBlockers": [
            "The original layout binder requires typed force pads (BaseBarracks, GroundStaging, VehicleSpawn and Rally) beyond these structure instance pads.",
            "The original route binder requires typed route rows with finite positive width and length, positive transit time for every enabled mode, at least two anchor waypoints, unique nonrepeating paths, at least two distinct approaches, and exactly one vehicle-enabled MainHighway.",
            "This receipt has only one direction and three role classes, has no measured route width or route type, and carries no dynamic blockers or live occupancy.",
            "The native declared-layout qualification must bind the exact candidate fingerprint and selected setup context; this artifact has no such qualification or compiler receipt.",
        ],
    }
    output.mkdir(parents=True, exist_ok=False)
    candidate_path = output / "partial-layout-candidate.json"
    candidate_path.write_text(json.dumps(artifact, indent=2, sort_keys=True) + "\n")
    receipt = {
        "schema": 1,
        "kind": "S026PartialLayoutCandidatePreparationReceipt",
        "status": "Prepared_Unqualified",
        "candidate": descriptor(candidate_path),
        "builder": descriptor(Path(__file__)),
        "inputReceipts": artifact["inputReceipts"],
        "inputReceiptHashes": {
            "placementProjection": EXPECTED_CANDIDATE_SHA,
            "placementBuildReceipt": EXPECTED_BUILD_RECEIPT_SHA,
            "routeDiagnostic": EXPECTED_R38_SHA,
            "spawnRosterDiagnostic": EXPECTED_R39_SHA,
        },
        "sizeCounts": {item["size"]: item["candidateStructureBindingCount"] for item in artifact["layoutsBySize"]},
        "routeRows": 0,
        "compilerAccepted": False,
        "sourceFitCertified": False,
        "normalMissionReady": False,
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n")
    print(json.dumps({"status": receipt["status"], "candidate": descriptor(candidate_path),
                      "sizeCounts": receipt["sizeCounts"], "routeRows": 0,
                      "compilerAccepted": False, "normalMissionReady": False}, sort_keys=True))


if __name__ == "__main__":
    main()

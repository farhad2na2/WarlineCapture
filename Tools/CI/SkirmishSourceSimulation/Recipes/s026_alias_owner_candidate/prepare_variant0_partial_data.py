#!/usr/bin/env python3
"""Prepare a bounded S026 Standard/Regular typed-pad data variant.

This copies the retained r5 Standard partial layout, adds the exact four
variant-0 typed spawn pads measured by r41, and gives each pad its own anchor
at the measured center. It emits no routes or Rally pad and is not a compiler
layout or fit/readiness result.
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
from pathlib import Path


ROOT = Path(__file__).resolve().parents[5]
R5_DIR = ROOT / "Logs/SkirmishProduction/s026-partial-layout-candidate-20261116-r5"
R5_CANDIDATE = R5_DIR / "partial-layout-candidate.json"
R5_RECEIPT = R5_DIR / "receipt.json"
R39 = ROOT / "Logs/SkirmishProduction/s026-citycrossroads-alias-source-diagnostic-20261116-r39/report.json"
R41 = ROOT / "Logs/SkirmishProduction/s026-citycrossroads-alias-source-diagnostic-20261116-r41/report.json"
R41_AUDIT = ROOT / "Logs/SkirmishProduction/s026-typed-pad-geometry-audit-20261116-r1/receipt.json"
R5_CANDIDATE_SHA = "21d065526412cc5b19a356d6dea6420a7be99c4c9fd427b5661bf2fe3c6957a8"
R5_RECEIPT_SHA = "ca90854e40c5a98375c3413ed1d8a22d7909735de4c6c079ce6bca7d7c7514a1"
R39_SHA = "bb20f7d6ab45afdc854efd8757ea95127ab591bc0ac9bac4cb82dced77c6a6e9"
R41_SHA = "9bc633854d9dbed1b8ab2247e635f48ab3bacf89ec38734f6187bd49e3a343f6"
R41_AUDIT_SHA = "97839adc296a5f4ca042b90c08338d814a1862bf5dab080365e1646f84ebf4fe"

CURRENT_ASSET_PIN_FIELDS = {
    "aliasPath": "aliasSha256",
    "campaignPath": "campaignSha256",
    "gridPath": "gridSha256",
    "surfacePath": "surfaceSha256",
}
EXPECTED_VARIANT0 = {
    (1, 3): ("s026.hypothesis.0.1.InfantrySpawn", 1012.0, 750.0, 12.0, 10.0),
    (1, 4): ("s026.hypothesis.0.1.VehicleSpawn", 1028.0, 750.0, 14.0, 12.0),
    (2, 3): ("s026.hypothesis.0.2.InfantrySpawn", 1092.0, 400.0, 12.0, 10.0),
    (2, 4): ("s026.hypothesis.0.2.VehicleSpawn", 1108.0, 400.0, 14.0, 12.0),
}


def sha_bytes(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def sha(path: Path) -> str:
    if path.is_symlink() or not path.is_file():
        raise ValueError(f"Regular non-symlink input required: {path}")
    return sha_bytes(path.read_bytes())


def descriptor(path: Path) -> dict:
    return {"path": path.relative_to(ROOT).as_posix(), "sha256": sha(path),
            "bytes": path.stat().st_size}


def read_json(path: Path) -> dict:
    return json.loads(path.read_bytes())


def rect_for(pad: dict) -> tuple[float, float, float, float]:
    half_w = float(pad["WidthMetres"]) / 2
    half_d = float(pad["DepthMetres"]) / 2
    return (float(pad["CenterX"]) - half_w, float(pad["CenterZ"]) - half_d,
            float(pad["CenterX"]) + half_w, float(pad["CenterZ"]) + half_d)


def intersects(a: tuple[float, float, float, float],
               b: tuple[float, float, float, float]) -> bool:
    return a[0] < b[2] and b[0] < a[2] and a[1] < b[3] and b[1] < a[3]


def load_inputs() -> tuple[dict, dict, dict, dict]:
    for path, expected in ((R5_CANDIDATE, R5_CANDIDATE_SHA),
                           (R5_RECEIPT, R5_RECEIPT_SHA),
                           (R39, R39_SHA), (R41, R41_SHA), (R41_AUDIT, R41_AUDIT_SHA)):
        if sha(path) != expected:
            raise ValueError(f"Pinned evidence changed: {path}")
    base = read_json(R5_CANDIDATE)
    r5_receipt = read_json(R5_RECEIPT)
    r39 = read_json(R39)
    report = read_json(R41)
    audit = read_json(R41_AUDIT)
    if (base.get("kind") != "S026PartialSourceDerivedLayoutCandidateWithTypedSpawnPadHypotheses"
            or base.get("status") != "PartialLayoutData_Unqualified"
            or base.get("identity", {}).get("catalogId") != "S026"
            or base.get("identity", {}).get("operationMapId") != "opmap.skirmish.city_crossroads"
            or r5_receipt.get("candidate", {}).get("sha256") != R5_CANDIDATE_SHA
            or r5_receipt.get("routeRows") != 0
            or r5_receipt.get("compilerAccepted") is not False
            or base.get("claims", {}).get("compilerAccepted") is not False):
        raise ValueError("Expected retained r5 partial candidate and receipt")
    if (report.get("kind") != "S026CityCrossroadsAliasSourceOwnerDiagnostic"
            or report.get("result") != "CollectedUnqualified"
            or report.get("identity", {}).get("catalogId") != "S026"
            or report.get("identity", {}).get("size") != "Standard"
            or report.get("identity", {}).get("difficulty") != "Regular"
            or report.get("cleanupVerified") is not True
            or report.get("sourceAssetsUnmodified") is not True
            or report.get("sourceFitCertified") is not False
            or report.get("nativeMissionInitialized") is not False
            or report.get("normalMissionReady") is not False):
        raise ValueError("Expected cleanup-verified unqualified r41 source diagnostic")
    if (audit.get("kind") != "S026R41TypedPadGeometryAudit"
            or audit.get("status") != "Passed_UnqualifiedGeometryAudit"
            or audit.get("scope", {}).get("authoredPads") is not False
            or audit.get("scope", {}).get("normalSpawnAcceptance") is not False
            or audit.get("scope", {}).get("sourceFitCertified") is not False):
        raise ValueError("Expected r41 independent unqualified geometry audit")

    r5_hyp = base.get("typedSpawnPadHypotheses", {})
    if (r5_hyp.get("sourceReport", {}).get("sha256") != R41_SHA
            or r5_hyp.get("independentGeometryAudit", {}).get("sha256") != R41_AUDIT_SHA
            or r5_hyp.get("selectedVariant") is not None
            or r5_hyp.get("variantCount") != 2):
        raise ValueError("r5 must preserve two unselected typed-pad hypotheses")
    if (base.get("inputReceipts", {}).get("spawnRosterDiagnostic", {}).get("sha256") != R39_SHA
            or base.get("sourcePins") != r39.get("sourcePins")
            or r39.get("result") != "CollectedUnqualified"
            or r39.get("cleanupVerified") is not True
            or r39.get("sourceAssetsUnmodified") is not True):
        raise ValueError("r5 historical pin snapshot must match its retained r39 input")
    typed = report.get("sharedTankWaypointsWithCandidateFacilityBlockers", {}).get("typedPadHypotheses", {})
    variants = typed.get("variants")
    if (typed.get("status") != "TransientTypedPadHypothesesCollected_Unqualified"
            or typed.get("candidateOnly") is not True
            or typed.get("seed") != 104755
            or typed.get("variantCountCap") != 2
            or not isinstance(variants, list) or len(variants) != 2
            or variants[0].get("variant") != 0 or variants[1].get("variant") != 1):
        raise ValueError("r41 exact two-variant owner hypothesis input required")
    pads = variants[0].get("candidatePads")
    if not isinstance(pads, list) or len(pads) != 4:
        raise ValueError("Variant 0 must contain four typed pads")
    observed = {}
    for pad in pads:
        key = (pad.get("FactionId"), pad.get("Kind"))
        row = (pad.get("PadId"), pad.get("CenterX"), pad.get("CenterZ"),
               pad.get("WidthMetres"), pad.get("DepthMetres"))
        if key in observed:
            raise ValueError("Duplicate variant-0 faction/category pad")
        observed[key] = row
    if observed != EXPECTED_VARIANT0:
        raise ValueError("Variant-0 dimensions/centers differ from exact tested geometry")

    historical_r5_pins = base.get("sourcePins", {})
    r41_pins = report.get("sourcePins", {})
    if set(historical_r5_pins) != set(r41_pins):
        raise ValueError("r5/r41 source pin domains differ")
    for key in set(r41_pins) - {"diagnosticImplementationSha256"}:
        if historical_r5_pins.get(key) != r41_pins.get(key):
            raise ValueError(f"r5/r41 historical source input differs: {key}")
    if (r41_pins.get("diagnosticImplementationPath") != historical_r5_pins.get("diagnosticImplementationPath")
            or r41_pins.get("diagnosticImplementationSha256") != r5_hyp.get("currentDiagnosticImplementation", {}).get("sha256")):
        raise ValueError("r41 variant source implementation is not the provenance pinned by r5 hypothesis")
    for path_key, digest_key in CURRENT_ASSET_PIN_FIELDS.items():
        path = ROOT / r41_pins[path_key]
        if sha(path) != r41_pins[digest_key]:
            raise ValueError(f"Current Campaign source asset pin drift: {path}")
    diagnostic_path = ROOT / r41_pins["diagnosticImplementationPath"]
    if sha(diagnostic_path) != r41_pins["diagnosticImplementationSha256"]:
        raise ValueError(f"Current r41 variant diagnostic implementation pin drift: {diagnostic_path}")
    return base, report, audit, r41_pins


def build_artifact() -> dict:
    base, report, audit, source_pins = load_inputs()
    size_rows = [row for row in base.get("layoutsBySize", []) if row.get("size") == "Standard"]
    if len(size_rows) != 1 or size_rows[0].get("difficulty") != "Regular":
        raise ValueError("One retained Standard/Regular partial layout required")
    partial_layout = copy.deepcopy(size_rows[0]["candidateLayout"])
    if (partial_layout.get("operationMapId") != base["identity"]["operationMapId"]
            or partial_layout.get("routes") != []
            or partial_layout.get("worldBinding", {}).get("HasMeasuredDeployments") is not False
            or partial_layout.get("worldBinding", {}).get("BindAllConfigurations") is not False
            or len(partial_layout.get("structureInstancePads", [])) != 20):
        raise ValueError("Retained Standard partial layout changed or became qualified")
    if any(pad.get("Kind") == 5 for pad in partial_layout.get("pads", [])):
        raise ValueError("Base candidate unexpectedly contains a Rally pad")

    bounds = report.get("aliasPlayableBounds", {})
    frame = partial_layout
    min_x, min_z = float(bounds["minX"]), float(bounds["minZ"])
    max_x, max_z = float(bounds["maxX"]), float(bounds["maxZ"])
    if (frame.get("originX") != min_x or frame.get("originZ") != min_z
            or frame.get("worldWidthMetres") != max_x - min_x
            or frame.get("worldDepthMetres") != max_z - min_z):
        raise ValueError("Partial layout frame differs from pinned playable alias bounds")
    # This is a distinct data draft; do not retain r5's placeholder content
    # identity after adding four rows.
    partial_layout["layoutId"] = "layout.skirmish.s026.partial-source-candidate.standard.regular.spawn-variant-0"
    partial_layout["contentHash"] = "unqualified.s026.standard.regular.spawn-variant-0.partial-data"

    variant0 = report["sharedTankWaypointsWithCandidateFacilityBlockers"]["typedPadHypotheses"]["variants"][0]
    base_anchor_ids = {row.get("AnchorId") for row in partial_layout.get("anchors", [])}
    base_pad_ids = {row.get("PadId") for row in partial_layout.get("pads", [])}
    if len(base_anchor_ids) != len(partial_layout["anchors"]) or len(base_pad_ids) != len(partial_layout["pads"]):
        raise ValueError("Retained candidate already has nonunique anchor/pad IDs")

    new_pads = []
    anchor_provenance = []
    roles = {3: "infantry", 4: "vehicle"}
    for source in sorted(variant0["candidatePads"], key=lambda p: (p["FactionId"], p["Kind"])):
        faction, kind = int(source["FactionId"]), int(source["Kind"])
        label = roles[kind]
        anchor_id = f"anchor.s026.standard.regular.spawn.variant0.faction_{faction}.{label}"
        role_id = f"candidate.spawn.variant0.faction_{faction}.{label}"
        pad_id = source["PadId"]
        if anchor_id in base_anchor_ids or pad_id in base_pad_ids:
            raise ValueError("Typed pad-specific anchor or pad ID collides with retained candidate")
        cx, cz = float(source["CenterX"]), float(source["CenterZ"])
        width, depth = float(source["WidthMetres"]), float(source["DepthMetres"])
        if not all(math.isfinite(value) for value in (cx, cz, width, depth)) or width <= 0 or depth <= 0:
            raise ValueError("Invalid source-tested typed pad geometry")
        anchor = {"AnchorId": anchor_id, "RoleId": role_id,
                  "NormalizedU": (cx - min_x) / (max_x - min_x),
                  "NormalizedV": (cz - min_z) / (max_z - min_z),
                  "WorldX": cx, "WorldZ": cz}
        pad = {"PadId": pad_id, "AnchorId": anchor_id, "RoleId": role_id,
               "Kind": kind, "CenterX": cx, "CenterZ": cz,
               "WidthMetres": width, "DepthMetres": depth, "FactionId": faction}
        partial_layout["anchors"].append(anchor)
        new_pads.append(pad)
        anchor_provenance.append({
            "anchorId": anchor_id,
            "sourceKind": "ExactR41Variant0TestedTypedPadCenter",
            "sourceReport": {"path": R41.relative_to(ROOT).as_posix(), "sha256": R41_SHA},
            "geometryAudit": {"path": R41_AUDIT.relative_to(ROOT).as_posix(), "sha256": R41_AUDIT_SHA},
            "variant": 0, "padId": pad_id, "factionId": faction,
            "padKind": kind, "worldCenter": [cx, cz],
        })
    partial_layout["pads"].extend(new_pads)

    # Independently check normalized anchors, alias bounds, and half-open rectangle overlap.
    typed_rects = []
    for pad in new_pads:
        rect = rect_for(pad)
        if rect[0] < min_x or rect[1] < min_z or rect[2] > max_x or rect[3] > max_z:
            raise ValueError(f"Typed pad rectangle leaves playable alias bounds: {pad['PadId']}")
        typed_rects.append((pad["PadId"], rect))
        anchor = next(a for a in partial_layout["anchors"] if a["AnchorId"] == pad["AnchorId"])
        if (anchor["WorldX"], anchor["WorldZ"]) != (pad["CenterX"], pad["CenterZ"]):
            raise ValueError("Typed pad anchor does not sit at tested center")
        if (not math.isclose(anchor["NormalizedU"], (pad["CenterX"] - min_x) / (max_x - min_x), abs_tol=1e-12)
                or not math.isclose(anchor["NormalizedV"], (pad["CenterZ"] - min_z) / (max_z - min_z), abs_tol=1e-12)):
            raise ValueError("Typed pad anchor normalized coordinates do not match frame")
    pad_overlap = []
    for index, (pad_id, rect) in enumerate(typed_rects):
        for other_id, other_rect in typed_rects[index + 1:]:
            if intersects(rect, other_rect):
                pad_overlap.append([pad_id, other_id])
    structure_rects = [(p["PadId"], rect_for(p)) for p in frame["pads"] if p["Kind"] != 3 and p["Kind"] != 4]
    facility_overlap = [[pid, fid] for pid, rect in typed_rects
                        for fid, facility in structure_rects if intersects(rect, facility)]
    if pad_overlap or facility_overlap:
        raise ValueError(f"Typed pad overlaps found: padPairs={pad_overlap}, facilities={facility_overlap}")

    # Match selected rectangles to the independent r41 owner geometry audit.
    audited_variant = next((v for v in audit.get("variants", []) if v.get("variant") == 0), None)
    if (audited_variant is None or audited_variant.get("independentGeometryChecks") != "Passed"
            or audited_variant.get("padPadOverlapPairs") != []
            or audited_variant.get("padFacilityOverlapPairs") != []
            or audited_variant.get("requestedActors") != 52
            or len(audited_variant.get("actorFootprints", [])) != 52):
        raise ValueError("Independent r41 geometry audit did not pass for variant 0")
    for pid, rect in typed_rects:
        expected_rect = audited_variant.get("padBounds", {}).get(pid)
        if expected_rect is None or any(not math.isclose(a, float(b), abs_tol=1e-9) for a, b in zip(rect, expected_rect)):
            raise ValueError(f"Pad rectangle differs from independent geometry audit: {pid}")
    # The r41 owner facility set and retained r5 structure pads are exactly the same 20 rectangles.
    exact_attempt = next(v for v in report["sourceStructurePlacementCandidates"]["constrainedFirstMiddleBandProbe"]["sizes"]
                        if v.get("size") == "Standard")
    attempts = [a for a in exact_attempt["pairAttempts"]
                if a.get("allPlannedStructuresRevalidatedInCanonicalOrder") is True
                and a.get("completeLogicalInstanceBindingProjection", {}).get("tryApplyPassed") is True
                and a.get("completeLogicalInstanceBindingProjection", {}).get("exactCoordinatesPreserved") is True]
    if len(attempts) != 1 or len(attempts[0].get("planByCompilerIndex", {})) != 20:
        raise ValueError("Unique exact r41 Standard structure owner rectangles required")
    for index, plan in attempts[0]["planByCompilerIndex"].items():
        raw = plan.get("resolvedEffectiveRect")
        old = frame["pads"][int(index)]
        if (raw is None or old["CenterX"] != raw["x"] + raw["width"] / 2
                or old["CenterZ"] != raw["z"] + raw["depth"] / 2
                or old["WidthMetres"] != raw["width"] or old["DepthMetres"] != raw["depth"]):
            raise ValueError("Retained r5 facility pads differ from r41 exact owner placement rectangles")

    pins = copy.deepcopy(source_pins)
    return {
        "schema": 1,
        "kind": "S026StandardVariant0TypedSpawnPadPartialData",
        "status": "PartialTypedSpawnPadData_Unqualified",
        "identity": {"catalogId": "S026", "definitionId": "skirmish.s026",
                     "operationMapId": pins["aliasOperationMapId"],
                     "sourceOperationMapId": pins["campaignOperationMapId"],
                     "size": "Standard", "difficulty": "Regular", "seed": 104755},
        "decisionProvenance": {
            "selectedHypothesis": 0,
            "selectionSource": "OwnerDirectionForThisArtifact",
            "variantLabel": "infantry west, vehicles east",
            "r5PriorSelectedVariant": None,
            "exactTestedCenters": True,
            "sourceOwnerProbeResult": "TransientTypedPadHypothesesCollected_Unqualified",
            "geometryAuditResult": "Passed_UnqualifiedGeometryAudit",
            "notes": [
                "This artifact selects variant 0 for a separate partial data draft; r5 remains unchanged with both variants unselected.",
                "Pad centers and dimensions are copied from r41 variant 0 without adjustment.",
                "Added typed pad anchors have unique IDs and are located at the exact tested pad centers.",
            ],
        },
        "sourcePins": pins,
        "inputs": {
            "r5Candidate": descriptor(R5_CANDIDATE),
            "r5Receipt": descriptor(R5_RECEIPT),
            "r39HistoricalRosterDiagnostic": descriptor(R39),
            "r41OwnerDiagnostic": descriptor(R41),
            "r41IndependentGeometryAudit": descriptor(R41_AUDIT),
        },
        "layoutCandidate": {
            "size": "Standard", "difficulty": "Regular",
            "candidateLayout": partial_layout,
            "typedSpawnAnchorProvenance": anchor_provenance,
            "existingStructurePadCount": 20,
            "addedTypedSpawnPadCount": len(new_pads),
            "addedTypedSpawnPads": new_pads,
            "validation": {
                "typedPadAliasBounds": "Passed",
                "typedPadPadOverlapPairs": pad_overlap,
                "typedPadStructureOverlapPairs": facility_overlap,
                "r41FacilityRectanglesMatchRetainedR5StructurePads": True,
                "allFourAnchorsUniqueAndAtExactCenters": True,
            },
        },
        "claims": {
            "typedSpawnPadDataAuthoredInPartialCandidate": True,
            "selectedVariantInR5": False,
            "routeRowsAuthored": False,
            "rallyPadPresent": False,
            "compilerLayout": False,
            "compilerAccepted": False,
            "fullLayoutValidated": False,
            "allSizeCapacityProven": False,
            "normalSpawnAcceptance": False,
            "liveOccupancyIncluded": False,
            "sourceFitCertified": False,
            "nativeMissionInitialized": False,
            "normalMissionReady": False,
            "budgetChanged": False,
            "campaignGeometryChanged": False,
        },
        "limitations": [
            "Only Standard/Regular variant-0 typed spawn pads are added; no other sizes or difficulties are covered.",
            "The layout has no Rally pad (Kind=5) and no authored route rows; original compiler admission remains blocked.",
            "R41 measurements are source-only transient spawn candidates without live occupancy or normal mission initialization.",
            "The r41 diagnostic implementation hash is retained as historical evidence provenance; this recipe does not claim that diagnostic code is still current.",
        ],
    }


def canonical_bytes(value: dict) -> bytes:
    return (json.dumps(value, indent=2, sort_keys=True, ensure_ascii=False,
                       allow_nan=False) + "\n").encode("utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True, help="New repository-local output directory")
    args = parser.parse_args()
    output = args.output if args.output.is_absolute() else ROOT / args.output
    output = output.resolve()
    if not output.is_relative_to(ROOT) or output.exists():
        raise ValueError("New repository-local output directory required")
    artifact = build_artifact()
    raw = canonical_bytes(artifact)
    # Repeat from the immutable input receipts before writing; any nondeterministic
    # projection or input drift fails closed.
    if raw != canonical_bytes(build_artifact()):
        raise ValueError("Partial typed-pad projection is not deterministic")
    candidate_path = output / "partial-layout-candidate.json"
    output.mkdir(parents=True, exist_ok=False)
    candidate_path.write_bytes(raw)
    receipt = {
        "schema": 1,
        "kind": "S026StandardVariant0TypedSpawnPadPreparationReceipt",
        "status": "Prepared_Unqualified",
        "candidate": descriptor(candidate_path),
        "builder": descriptor(Path(__file__)),
        "inputs": artifact["inputs"],
        "typedSpawnPadCount": 4,
        "typedSpawnAnchorCount": 4,
        "existingStructurePadCount": 20,
        "routeRows": 0,
        "rallyPadPresent": False,
        "compilerAccepted": False,
        "sourceFitCertified": False,
        "normalMissionReady": False,
    }
    (output / "receipt.json").write_bytes(canonical_bytes(receipt))
    print(json.dumps({"status": receipt["status"], "candidate": receipt["candidate"],
                      "typedSpawnPads": 4, "routeRows": 0, "compilerAccepted": False,
                      "normalMissionReady": False}, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env python3
"""Attach R41 typed-spawn-pad hypotheses to a separate partial S026 draft.

This preserves the existing partial layout and historical R38/R39 provenance.
R41's two temporary pad alternatives are copied as unselected evidence only;
they are not merged into compiler-bound layout rows or treated as accepted.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[5]
BASE = ROOT / "Logs/SkirmishProduction/s026-partial-layout-candidate-20261116-r3"
BASE_CANDIDATE = BASE / "partial-layout-candidate.json"
BASE_RECEIPT = BASE / "receipt.json"
R41 = ROOT / "Logs/SkirmishProduction/s026-citycrossroads-alias-source-diagnostic-20261116-r41/report.json"
R41_AUDIT = ROOT / "Logs/SkirmishProduction/s026-typed-pad-geometry-audit-20261116-r1/receipt.json"
R41_AUDITOR = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s026_alias_owner_candidate/audit_r41_typed_pad_geometry.py"
R41_TEST = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/test_s026_r41_typed_pad_geometry_audit.py"
R41_REPORT_SHA = "9bc633854d9dbed1b8ab2247e635f48ab3bacf89ec38734f6187bd49e3a343f6"
R41_AUDIT_SHA = "97839adc296a5f4ca042b90c08338d814a1862bf5dab080365e1646f84ebf4fe"
BASE_CANDIDATE_SHA = "03fe3395588f8fedea742405c468eea46290ab53ddb225dff3a8eb971cfd3fda"
BASE_RECEIPT_SHA = "7e8dea11af4e87fcfb5bdefa85fd68fb254af78f11def14fb202d00723b7e810"
R41_IMPL = "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.S026AliasDiagnostic.cs"
FOOTPRINT = "Assets/Game/Scripts/Components/UnitFootprintUtility.cs"
CURRENT_IMPL_SHA = "0a23a263492950e27f0d66f1f511735ce1063dc1321c625e1156b17179eb11e3"
FOOTPRINT_SHA = "af2205647cd3bf6d99df509af5525840e60bda99659b3e0ffa17bc24e4303599"
R41_AUDITOR_SHA = "80e0d914b169425e54dbb16d7eea3d9d0259c93858910879cbe6f8db3d9f4500"
R41_TEST_SHA = "56fff837f87b0f15bf351a7f8f2f1b74c46fa001416c0d9a1295cee5efdeed4f"
OUTPUT_DEFAULT = ROOT / "Logs/SkirmishProduction/s026-partial-layout-candidate-20261116-r4"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def no_duplicates(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        require(key not in result, f"Duplicate JSON property: {key}")
        result[key] = value
    return result


def read(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=no_duplicates)
    require(isinstance(value, dict), f"Expected JSON object: {path}")
    return value


def rel(path: Path) -> str:
    return path.resolve(strict=True).relative_to(ROOT).as_posix()


def descriptor(path: Path) -> dict[str, Any]:
    return {"path": rel(path), "sha256": sha(path), "bytes": path.stat().st_size}


def verify_owner_pins(pins: list[dict[str, Any]], label: str) -> list[dict[str, str]]:
    require(isinstance(pins, list) and pins, f"{label}: owner pin list missing")
    checked: list[dict[str, str]] = []
    seen: set[str] = set()
    for row in pins:
        path, expected = row.get("path"), row.get("sha256")
        require(isinstance(path, str) and path not in seen and isinstance(expected, str),
                f"{label}: malformed or duplicate owner pin")
        seen.add(path)
        actual_path = ROOT / path
        require(actual_path.is_file() and sha(actual_path) == expected,
                f"{label}: source pin is missing or stale: {path}")
        checked.append({"path": path, "sha256": expected})
    return sorted(checked, key=lambda row: row["path"])


def prepare() -> tuple[dict[str, Any], dict[str, Any]]:
    for path, expected in ((BASE_CANDIDATE, BASE_CANDIDATE_SHA),
                           (BASE_RECEIPT, BASE_RECEIPT_SHA),
                           (R41, R41_REPORT_SHA), (R41_AUDIT, R41_AUDIT_SHA),
                           (R41_AUDITOR, R41_AUDITOR_SHA), (R41_TEST, R41_TEST_SHA)):
        require(path.is_file() and sha(path) == expected, f"Pinned input changed: {path}")

    base = read(BASE_CANDIDATE)
    base_receipt = read(BASE_RECEIPT)
    report = read(R41)
    audit = read(R41_AUDIT)
    require(base.get("kind") == "S026PartialSourceDerivedLayoutCandidate"
            and base.get("status") == "PartialLayoutData_Unqualified"
            and base.get("routeStatus") == "NoRouteRows_WidthAndRouteIdentityUnspecified"
            and base.get("claims", {}).get("routeRowsAuthored") is False
            and base.get("claims", {}).get("compilerAccepted") is False
            and base_receipt.get("status") == "Prepared_Unqualified"
            and base_receipt.get("candidate", {}).get("sha256") == BASE_CANDIDATE_SHA,
            "Exact historical partial candidate required")
    require(all(row.get("candidateLayout", {}).get("routes") == [] for row in base.get("layoutsBySize", [])),
            "Historical partial candidate unexpectedly contains route rows")

    require(report.get("kind") == "S026CityCrossroadsAliasSourceOwnerDiagnostic"
            and report.get("result") == "CollectedUnqualified"
            and report.get("identity") == {
                "catalogId": "S026", "definitionId": "skirmish.s026",
                "scenarioSetupId": "scenario.skirmish.s026", "size": "Standard",
                "difficulty": "Regular", "tuple": {"map_id": "CC", "objective_id": "BA",
                    "army_profile": "G", "start_profile": "E"},
                "canonicalBudgetMatrixSha256": "b28cfe04f72ab0161dea86813fc12a9a61c5d46361fc9299839394ee91a2cf97",
                "canonicalBudgetAuthoritySha256": "b28cfe04f72ab0161dea86813fc12a9a61c5d46361fc9299839394ee91a2cf97"},
            "Unexpected r41 report identity or result")
    typed = report.get("sharedTankWaypointsWithCandidateFacilityBlockers", {}).get("typedPadHypotheses", {})
    require(typed.get("candidateOnly") is True and typed.get("seed") == 104755
            and typed.get("variantCountCap") == 2 and typed.get("status") == "TransientTypedPadHypothesesCollected_Unqualified"
            and typed.get("sourceFitCertified") is False and typed.get("normalMissionInitialized") is False
            and typed.get("normalSpawnAcceptance") is False and typed.get("liveOccupancyIncluded") is False
            and typed.get("fullLayoutValidated") is False,
            "R41 typed-pad scope or non-acceptance flags changed")
    require(report.get("cleanupVerified") is True and report.get("sourceAssetsUnmodified") is True
            and report.get("normalMissionReady") is False and report.get("fullMapOrAllSizeFitMeasured") is False,
            "R41 cleanup or unqualified status changed")
    require(report.get("sourcePins", {}).get("diagnosticImplementationPath") == R41_IMPL
            and report.get("sourcePins", {}).get("diagnosticImplementationSha256") == CURRENT_IMPL_SHA
            and sha(ROOT / R41_IMPL) == CURRENT_IMPL_SHA
            and sha(ROOT / FOOTPRINT) == FOOTPRINT_SHA,
            "Current R41 diagnostic or footprint owner pin changed")

    audit_input = audit.get("inputs")
    require(audit.get("kind") == "S026R41TypedPadGeometryAudit"
            and audit.get("status") == "Passed_UnqualifiedGeometryAudit"
            and audit_input == [{"path": rel(R41), "sha256": R41_REPORT_SHA}],
            "Independent geometry audit does not bind exact R41 report")
    scope = audit.get("scope", {})
    require(audit.get("identity", {}).get("seed") == 104755
            and audit.get("facilitySelection", {}).get("count") == 20
            and audit.get("facilitySelection", {}).get("allOriginalOwnerRectanglesPresent") is True
            and audit.get("cleanup", {}).get("verified") is True
            and audit.get("cleanup", {}).get("controlsPassed") == 6
            and audit.get("cleanup", {}).get("companionCleanupControlsPassed") == 6
            and scope.get("candidateOnly") is True
            and all(scope.get(field) is False for field in (
                "authoredPads", "fullLayoutValidated", "liveOccupancyIncluded",
                "normalMissionInitialized", "normalSpawnAcceptance", "physicalWidthOrReadinessClaim",
                "sourceFitCertified", "unityRunByRecipe")),
            "Independent audit scope, cleanup, or identity changed")
    report_owner_pins = audit.get("verifiedReportOwnerPins", [])
    checked_pins = verify_owner_pins(report_owner_pins, "R41 audit")
    require(len(checked_pins) == 25, "R41 audit owner-pin census changed")
    footprint = audit.get("separatelyPinnedOwner", {})
    require(footprint == {"path": FOOTPRINT, "sha256": FOOTPRINT_SHA,
                          "purpose": "UnitFootprintUtility.GetMinCell footprint reconstruction"},
            "Independent footprint owner pin changed")

    owner_pins = verify_owner_pins(typed.get("ownerSourcePins", []), "R41 typed pad hypotheses")
    report_pin_map = {row["path"]: row["sha256"] for row in checked_pins}
    require(all(report_pin_map.get(row["path"]) == row["sha256"] for row in owner_pins),
            "R41 typed owner pins are not covered by independent audit")
    variants = typed.get("variants")
    audited_variants = audit.get("variants")
    require(isinstance(variants, list) and len(variants) == 2
            and isinstance(audited_variants, list) and len(audited_variants) == 2,
            "Expected exactly two R41 hypotheses and audit rows")
    variant_rows = []
    for expected_index, (variant, audited) in enumerate(zip(variants, audited_variants)):
        require(variant.get("variant") == audited.get("variant") == expected_index
                and variant.get("candidateOnly") is True and variant.get("sourceFitCertified") is False
                and variant.get("status") == "TypedCandidatePackingCollected_Unqualified"
                and variant.get("requestedActors") == variant.get("foundActors") == 52
                and variant.get("insideTypedPadActors") == variant.get("surfaceTraversableActors") == 52
                and audited.get("requestedActors") == 52
                and audited.get("independentGeometryChecks") == "Passed"
                and not audited.get("padPadOverlapPairs") and not audited.get("padFacilityOverlapPairs")
                and not audited.get("actorFootprintOverlapPairs") and not audited.get("actorFacilityOverlapPairs"),
                f"R41 variant {expected_index} failed its exact candidate/audit contract")
        pads = variant.get("candidatePads")
        require(isinstance(pads, list) and len(pads) == 4, f"R41 variant {expected_index} pad count changed")
        variant_rows.append({
            "variant": expected_index,
            "status": "AlternativeCandidate_Unqualified",
            "candidateOnly": True,
            "candidatePads": pads,
            "requestedActors": 52,
            "foundCandidateActors": 52,
            "insideTypedPadActors": 52,
            "surfaceTraversableActors": 52,
            "geometryAudit": {
                "status": audited["independentGeometryChecks"],
                "facilityRectCount": audited["facilityRectCount"],
                "padBounds": audited["padBounds"],
                "padPadOverlapPairs": audited["padPadOverlapPairs"],
                "padFacilityOverlapPairs": audited["padFacilityOverlapPairs"],
                "actorFootprintOverlapPairs": audited["actorFootprintOverlapPairs"],
                "actorFacilityOverlapPairs": audited["actorFacilityOverlapPairs"],
            },
            "selection": False,
        })

    pins = base.get("sourcePins", {})
    r41_pins = report.get("sourcePins", {})
    for left, right in (("aliasPath", "aliasPath"), ("aliasSha256", "aliasSha256"),
                        ("aliasOperationMapId", "aliasOperationMapId"),
                        ("aliasContentHash", "aliasContentHash"), ("aliasSourceBinding", "aliasSourceBinding"),
                        ("campaignPath", "campaignPath"), ("campaignSha256", "campaignSha256"),
                        ("campaignOperationMapId", "campaignOperationMapId"),
                        ("campaignContentHash", "campaignContentHash"),
                        ("campaignSourceIdentityHash", "campaignSourceIdentityHash"),
                        ("gridPath", "gridPath"), ("gridSha256", "gridSha256"),
                        ("surfacePath", "surfacePath"), ("surfaceSha256", "surfaceSha256"),
                        ("sourceSceneGuid", "sourceSceneGuid"), ("navigationSubSceneGuid", "navigationSubSceneGuid")):
        require(pins.get(left) == r41_pins.get(right), f"Historical base and R41 source identity differ: {left}")
    for path_key, digest_key in (("aliasPath", "aliasSha256"), ("campaignPath", "campaignSha256"),
                                 ("gridPath", "gridSha256"), ("surfacePath", "surfaceSha256")):
        source_path = ROOT / r41_pins[path_key]
        require(source_path.is_file() and sha(source_path) == r41_pins[digest_key],
                f"Current Campaign/source asset pin changed: {r41_pins[path_key]}")

    artifact = {
        "schema": 1,
        "kind": "S026PartialSourceDerivedLayoutCandidateWithTypedSpawnPadHypotheses",
        "status": "PartialLayoutData_Unqualified",
        "basePartialCandidate": descriptor(BASE_CANDIDATE),
        "basePartialReceipt": descriptor(BASE_RECEIPT),
        "identity": base["identity"],
        "difficultyCoverage": base["difficultyCoverage"],
        "sourcePins": base["sourcePins"],
        "historicalRouteSourcePins": base["historicalRouteSourcePins"],
        "inputReceipts": {
            **base["inputReceipts"],
            "typedPadHypothesisReport": descriptor(R41),
            "typedPadGeometryAudit": descriptor(R41_AUDIT),
        },
        "layoutsBySize": base["layoutsBySize"],
        "routeStatus": base["routeStatus"],
        "candidateRouteChains": base["candidateRouteChains"],
        "spawnRosterCandidateEvidence": base["spawnRosterCandidateEvidence"],
        "typedSpawnPadHypotheses": {
            "sourceReport": descriptor(R41),
            "independentGeometryAudit": descriptor(R41_AUDIT),
            "currentDiagnosticImplementation": {"path": R41_IMPL, "sha256": CURRENT_IMPL_SHA},
            "historicalR38RouteImplementation": base["historicalRouteSourcePins"]["diagnosticImplementationSha256"],
            "ownerSourcePins": checked_pins,
            "identity": {"catalogId": "S026", "size": "Standard", "difficulty": "Regular", "seed": 104755},
            "scope": typed["scope"],
            "variantCount": 2,
            "selectedVariant": None,
            "variants": variant_rows,
            "candidateOnly": True,
            "normalSpawnAcceptance": False,
            "liveOccupancyIncluded": False,
            "sourceFitCertified": False,
            "fullLayoutValidated": False,
            "compilerAdmission": False,
            "normalMissionReady": False,
            "interpretation": "Two alternative transient owner-tested typed-mask hypotheses; neither is selected or accepted as S026 layout data.",
        },
        "claims": {**base["claims"], "typedSpawnPadHypothesesRecorded": True,
                    "typedSpawnPadsAuthored": False, "typedSpawnPadVariantSelected": False},
        "compilerBlockers": [*base["compilerBlockers"],
            "R41 contributes two unselected transient typed-pad hypotheses only; normal mission spawn acceptance, live occupancy, all-size fit, and compiler binding remain unverified."],
    }
    require(artifact["layoutsBySize"] == base["layoutsBySize"]
            and all(row["candidateLayout"]["routes"] == [] for row in artifact["layoutsBySize"]),
            "Typed pad evidence must not mutate layouts or add route rows")
    receipt = {
        "schema": 1,
        "kind": "S026PartialCandidateTypedPadHypothesesReceipt",
        "status": "Prepared_Unqualified",
        "candidate": None,
        "builder": {"path": rel(Path(__file__)), "sha256": sha(Path(__file__)), "bytes": Path(__file__).stat().st_size},
        "inputReceipts": artifact["inputReceipts"],
        "typedPadVariantCount": 2,
        "selectedVariant": None,
        "routeRows": 0,
        "compilerAccepted": False,
        "sourceFitCertified": False,
        "normalMissionReady": False,
        "verifiedCurrentOwnerPinCount": len(checked_pins),
    }
    return artifact, receipt


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=OUTPUT_DEFAULT,
                        help="Fresh repository-local output directory")
    args = parser.parse_args()
    output = args.output.resolve()
    require(output.is_relative_to(ROOT) and not output.exists(), "Fresh repository-local output directory required")
    artifact, receipt = prepare()
    output.mkdir(parents=True)
    candidate_path = output / "partial-layout-candidate.json"
    candidate_path.write_text(json.dumps(artifact, sort_keys=True, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    receipt["candidate"] = descriptor(candidate_path)
    receipt_path = output / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, sort_keys=True, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"[s026-partial-typed-pad-hypotheses] result=Prepared_Unqualified variants=2 selected=none route_rows=0 candidate_sha256={sha(candidate_path)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

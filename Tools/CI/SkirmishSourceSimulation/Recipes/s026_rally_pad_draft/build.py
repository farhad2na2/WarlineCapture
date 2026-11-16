#!/usr/bin/env python3
"""Build a separate unqualified S026 Standard/Regular Rally-pad draft."""
import copy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
SOURCE = ROOT / "Logs/SkirmishProduction/s026-standard-typed-pad-variant0-partial-20261116-r1/partial-layout-candidate.json"
EXPECTED_SHA256 = "b7c6688738598eecce764af5c55f167022bc440d5373c59ff5fe9762862ac113"
OUT = ROOT / "Logs/SkirmishProduction/s026-standard-rally-pad-draft-20261116-r1"

HYPOTHESES = [
    {"PadId": "s026.hypothesis.0.1.Rally", "AnchorId": "anchor.s026.hypothesis.0.1.rally", "RoleId": "candidate.rally.variant0.faction_1", "FactionId": 1, "Kind": 5, "CenterX": 1025.5, "CenterZ": 715.5, "WidthMetres": 3.0, "DepthMetres": 3.0},
    {"PadId": "s026.hypothesis.0.2.Rally", "AnchorId": "anchor.s026.hypothesis.0.2.rally", "RoleId": "candidate.rally.variant0.faction_2", "FactionId": 2, "Kind": 5, "CenterX": 1094.5, "CenterZ": 409.5, "WidthMetres": 3.0, "DepthMetres": 3.0},
]


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def overlap(a: dict, b: dict) -> bool:
    # Positive-area overlap; shared edges are non-overlapping.
    return (abs(a["CenterX"] - b["CenterX"]) * 2 < a["WidthMetres"] + b["WidthMetres"] and
            abs(a["CenterZ"] - b["CenterZ"]) * 2 < a["DepthMetres"] + b["DepthMetres"])


def main() -> None:
    raw = SOURCE.read_bytes()
    input_sha = sha256(raw)
    if input_sha != EXPECTED_SHA256:
        raise SystemExit(f"input hash mismatch: expected {EXPECTED_SHA256}, got {input_sha}")
    source = json.loads(raw)
    base = source["layoutCandidate"]["candidateLayout"]
    if len(base["pads"]) != 24:
        raise SystemExit(f"expected exactly 24 existing pads, got {len(base['pads'])}")
    layout = copy.deepcopy(source)
    target = layout["layoutCandidate"]["candidateLayout"]
    ids = {a["AnchorId"] for a in target["anchors"]}
    pad_ids = {p["PadId"] for p in target["pads"]}
    if any(p["AnchorId"] in ids or p["PadId"] in pad_ids for p in HYPOTHESES):
        raise SystemExit("hypothesis IDs collide with source")

    origin_x, origin_z = target["originX"], target["originZ"]
    width, depth = target["worldWidthMetres"], target["worldDepthMetres"]
    bounds = {"minX": origin_x, "maxX": origin_x + width,
              "minZ": origin_z, "maxZ": origin_z + depth}
    new_pads = []
    new_anchors = []
    for pad in HYPOTHESES:
        half_w, half_d = pad["WidthMetres"] / 2, pad["DepthMetres"] / 2
        if (pad["CenterX"] - half_w < bounds["minX"] or pad["CenterX"] + half_w > bounds["maxX"] or
                pad["CenterZ"] - half_d < bounds["minZ"] or pad["CenterZ"] + half_d > bounds["maxZ"]):
            raise SystemExit(f"pad outside alias logical bounds: {pad['PadId']}")
        new_pads.append({**pad, "HypothesisOnly": True})
        new_anchors.append({
            "AnchorId": pad["AnchorId"], "RoleId": pad["RoleId"],
            "WorldX": pad["CenterX"], "WorldZ": pad["CenterZ"],
            "NormalizedU": (pad["CenterX"] - origin_x) / width,
            "NormalizedV": (pad["CenterZ"] - origin_z) / depth,
            "HypothesisOnly": True,
        })

    all_pads = target["pads"] + new_pads
    all_pairs = [(a["PadId"], b["PadId"]) for i, a in enumerate(all_pads) for b in all_pads[i + 1:] if overlap(a, b)]
    existing_pairs = [(a["PadId"], b["PadId"]) for i, a in enumerate(target["pads"]) for b in target["pads"][i + 1:] if overlap(a, b)]
    new_existing_checks = len(new_pads) * len(target["pads"])
    if existing_pairs or all_pairs:
        raise SystemExit(f"positive-area pad overlap(s): {all_pairs}")
    if len(all_pads) != 26 or new_existing_checks != 48:
        raise SystemExit("unexpected pad census")

    target["pads"].extend(new_pads)
    target["anchors"].extend(new_anchors)
    layout["layoutCandidate"]["addedTypedRallyPadCount"] = 2
    layout["layoutCandidate"]["addedTypedRallyPads"] = new_pads
    layout["kind"] = "S026StandardVariant0TypedSpawnAndRallyPadPartialData"
    layout["status"] = "PartialTypedSpawnAndRallyPadData_UnqualifiedHypotheses"
    layout["limitations"] = [s for s in layout.get("limitations", []) if not s.startswith("The layout has no Rally pad") and not s.startswith("Only Standard/Regular variant-0 typed spawn pads")] + [
        "Only Standard/Regular variant 0 is included in this partial draft; other sizes and difficulties are not covered.",
        "The layout has no authored route rows; original compiler admission remains blocked.",
        "Two 3x3 Rally-pad dimensions and cell-center locations are unqualified hypotheses.",
        "No route, reachability, source fit, all-size capacity, compiler acceptance or normal mission readiness is claimed.",
    ]
    claims = layout.setdefault("claims", {})
    claims.update({"rallyPadPresent": False, "rallyPadHypothesesPresent": True, "rallyPadDimensionsQualified": False,
                   "rallyPadCentersQualified": False, "routeClaim": False, "reachabilityClaim": False,
                   "sourceFitCertified": False, "normalMissionReady": False})
    output_path = OUT / "partial-layout-candidate.json"
    receipt_path = OUT / "receipt.json"
    OUT.mkdir(parents=True, exist_ok=True)
    output_bytes = (json.dumps(layout, indent=2, ensure_ascii=False) + "\n").encode()
    output_path.write_bytes(output_bytes)
    receipt = {
        "schema": 1, "kind": "S026StandardVariant0RallyPadDraftReceipt",
        "status": "DraftBuilt_UnqualifiedHypotheses",
        "input": {"path": str(SOURCE.relative_to(ROOT)), "sha256": input_sha, "bytes": len(raw)},
        "output": {"path": str(output_path.relative_to(ROOT)), "sha256": sha256(output_bytes), "bytes": len(output_bytes)},
        "bounds": bounds,
        "existingPadCount": len(base["pads"]), "newRallyPadCount": 2, "finalPadCount": len(all_pads),
        "pairwiseOverlapChecks": {"allFinalPadPairs": len(all_pads) * (len(all_pads) - 1) // 2,
                                  "newRallyAgainstExistingPads": new_existing_checks,
                                  "rallyPair": 1, "positiveAreaOverlaps": 0},
        "newPads": new_pads,
        "dimensionsAndCentersQualified": False,
        "claims": {"route": False, "reachability": False, "sourceFit": False, "readiness": False},
        "sourceModified": False,
    }
    receipt_path.write_text(json.dumps(receipt, indent=2, ensure_ascii=False) + "\n")
    print(json.dumps({"status": receipt["status"], "inputSha256": input_sha,
                      "output": str(output_path.relative_to(ROOT)), "receipt": str(receipt_path.relative_to(ROOT)),
                      "existingPads": len(base["pads"]), "newExistingOverlapChecks": new_existing_checks,
                      "allFinalPadPairs": receipt["pairwiseOverlapChecks"]["allFinalPadPairs"],
                      "positiveAreaOverlaps": 0}, sort_keys=True))


if __name__ == "__main__":
    main()

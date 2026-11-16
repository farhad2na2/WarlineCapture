#!/usr/bin/env python3
"""Compare compiled S026 force rows with r41 candidate placement counts.

Source-only count comparison. Candidate placements are not accepted spawns and
the result makes no source-fit or readiness claim.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[5]


def require(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_json(path: Path) -> dict[str, Any]:
    def reject_duplicates(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
        obj: dict[str, Any] = {}
        for key, value in pairs:
            require(key not in obj, f"Duplicate JSON key: {key}")
            obj[key] = value
        return obj

    with path.open(encoding="utf-8") as stream:
        value = json.load(stream, object_pairs_hook=reject_duplicates)
    require(isinstance(value, dict), f"Expected JSON object: {path}")
    return value


def counts(rows: Any, faction_key: str, role_key: str, count_key: str, label: str) -> dict[tuple[int, str], int]:
    require(isinstance(rows, list) and rows, f"Missing {label} rows")
    result: dict[tuple[int, str], int] = {}
    for index, row in enumerate(rows):
        require(isinstance(row, dict), f"Malformed {label} row {index}")
        faction, role, amount = row.get(faction_key), row.get(role_key), row.get(count_key)
        require(isinstance(faction, int) and not isinstance(faction, bool) and faction > 0,
                f"Invalid faction in {label} row {index}")
        require(isinstance(role, str) and role, f"Invalid role in {label} row {index}")
        require(isinstance(amount, int) and not isinstance(amount, bool) and amount > 0,
                f"Invalid positive count in {label} row {index}")
        key = (faction, role)
        require(key not in result, f"Duplicate faction/role row in {label}: {key}")
        result[key] = amount
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--authoring", required=True, help="Compiled authoring result JSON")
    parser.add_argument("--authoring-sha256", required=True)
    parser.add_argument("--candidate-report", required=True, help="r41 report JSON")
    parser.add_argument("--candidate-sha256", required=True)
    parser.add_argument("--output", required=True, help="New absolute output JSON")
    args = parser.parse_args()

    paths = [Path(args.authoring).resolve(), Path(args.candidate_report).resolve()]
    expected_hashes = [args.authoring_sha256.lower(), args.candidate_sha256.lower()]
    require(all(len(h) == 64 and all(c in "0123456789abcdef" for c in h) for h in expected_hashes),
            "Input pins must be SHA-256 hex digests")
    for path, expected in zip(paths, expected_hashes):
        require(path.is_file(), f"Input missing: {path}")
        require(sha256(path) == expected, f"Input SHA-256 mismatch: {path}")

    authoring, candidate = (read_json(path) for path in paths)
    require(authoring.get("kind") == "SkirmishMissionAuthoringPreflightResult" and
            authoring.get("status") == "ConfigurationCompiled" and
            authoring.get("catalog_id") == "S026" and authoring.get("size") == "Standard" and
            authoring.get("difficulty") == "Regular", "Unexpected compiled authoring identity")
    require(authoring.get("world_allocated") is False, "Authoring result allocated a world")
    require(candidate.get("kind") == "S026CityCrossroadsAliasSourceOwnerDiagnostic" and
            candidate.get("scope") == "ExistingCampaignSource_AliasFrame_StandardOnly" and
            candidate.get("identity", {}).get("catalogId") == "S026" and
            candidate.get("identity", {}).get("size") == "Standard" and
            candidate.get("identity", {}).get("difficulty") == "Regular" and
            candidate.get("sourceFitCertified") is False and candidate.get("liveOccupancyIncluded") is False,
            "Unexpected r41 candidate report identity")
    compiled = counts(authoring.get("resolved_setup", {}).get("forces"),
                      "faction_id", "role_id", "quantity", "compiled forces")
    placements = counts(candidate.get("candidatePlacements"),
                        "faction", "roleId", "requestedCount", "candidate placements")
    keys = sorted(set(compiled) | set(placements))
    rows = [{"faction": faction, "roleId": role,
             "compiledQuantity": compiled.get((faction, role)),
             "candidateRequestedCount": placements.get((faction, role)),
             "equal": compiled.get((faction, role)) == placements.get((faction, role))}
            for faction, role in keys]
    compatible = all(row["equal"] for row in rows)

    output = Path(args.output).resolve()
    require(output.is_absolute() and not output.exists(), f"Output must be a fresh absolute path: {output}")
    require(output.parent.is_dir(), f"Output directory must already exist: {output.parent}")
    result = {
        "schema": 1,
        "kind": "S026CompiledForceToR41CandidateCountAudit",
        "status": "CountsEqual_Unqualified" if compatible else "IncompatibleForceRows",
        "inputs": {
            "authoringResultPath": str(paths[0]), "authoringResultSha256": expected_hashes[0],
            "candidateReportPath": str(paths[1]), "candidateReportSha256": expected_hashes[1],
        },
        "identity": {"catalogId": "S026", "size": "Standard", "difficulty": "Regular",
                     "authoringStatus": "ConfigurationCompiled", "worldAllocated": False},
        "comparison": {"compatible": compatible, "compiledUniqueRows": len(compiled),
                       "candidateUniqueRows": len(placements), "rows": rows},
        "scope": "Per-faction/role quantity comparison only. Candidate placements are not accepted spawns. No source-fit, gameplay, or readiness claim.",
    }
    for path, expected in zip(paths, expected_hashes):
        require(sha256(path) == expected, f"Input changed during audit: {path}")
    with output.open("x", encoding="utf-8") as stream:
        stream.write(json.dumps(result, indent=2, sort_keys=True) + "\n")
    print(json.dumps({"status": result["status"], "output": str(output), "rows": len(rows)}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

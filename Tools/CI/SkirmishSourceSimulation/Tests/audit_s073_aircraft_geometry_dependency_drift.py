#!/usr/bin/env python3
"""Read-only offline audit of the pinned S073 aircraft geometry drift evidence."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path
from typing import Any


REPORT_REL = Path("Logs/SkirmishProduction/s073-aircraft-geometry-drift-20261102-r1/report.json")
PROVENANCE_REL = Path("Logs/SkirmishProduction/AircraftRootProvenance/910f18719906473db6b2057696c181b0.json")
REPORT_SHA256 = "801eebd8adb5b36229afb9be89bf83b7fef2e213a92cb64adb5a229f2b57b6e2"
PROVENANCE_SHA256 = "7b2d7e900a97d6b690a2f2017ba0f7fd6ef6b02b74efa77f1473e0faf60c7a5f"
DECLARED_NATIVE_TEMPLATE_SHA256 = "5d7aa45d232a08b3a1336c0166d8f8834bf18758b92f1af9765acd218b1ea3bc"


class AuditError(RuntimeError):
    pass


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def report_mismatch_paths(report: dict[str, Any]) -> set[str]:
    paths: set[str] = set()
    for row in report.get("rotor_geometry", {}).get("rows", []):
        if row.get("dependency_hash_mismatch"):
            path = row.get("installed", {}).get("source_path")
            if path:
                paths.add(path)
    for role in report.get("canonical_geometry", {}).get("roles", []):
        for comparison in role.get("source_comparisons", []):
            if comparison.get("dependency_hash_mismatch"):
                path = comparison.get("installed", {}).get("path")
                if path:
                    paths.add(path)
    return paths


def audit(report_path: Path, provenance_path: Path, repo: Path) -> dict[str, Any]:
    report_hash = sha256_file(report_path)
    provenance_hash = sha256_file(provenance_path)
    if report_hash != REPORT_SHA256:
        raise AuditError(f"drift report SHA256 mismatch: {report_hash}")
    if provenance_hash != PROVENANCE_SHA256:
        raise AuditError(f"provenance report SHA256 mismatch: {provenance_hash}")

    report = json.loads(report_path.read_text(encoding="utf-8"))
    provenance = json.loads(provenance_path.read_text(encoding="utf-8"))
    mismatch_paths = report_mismatch_paths(report)
    graph_paths: set[str] = set()
    checked_records = 0
    missing_records: list[str] = []
    mismatched_records: list[dict[str, str]] = []
    for source in provenance.get("sources", []):
        for graph in source.get("importerDependencyGraphs", []):
            for dependency in graph.get("dependencies", []):
                asset_path = dependency.get("assetPath")
                if asset_path:
                    graph_paths.add(asset_path)
                for kind in ("source", "meta"):
                    record = dependency.get(kind, {})
                    if not record.get("exists") or not record.get("sha256"):
                        continue
                    physical = record.get("physicalPath")
                    if not physical:
                        missing_records.append(f"{asset_path}:{kind}:missing physicalPath")
                        continue
                    path = Path(physical)
                    if not path.is_absolute():
                        path = repo / path
                    if not path.is_file():
                        missing_records.append(f"{asset_path}:{kind}:{path}")
                        continue
                    actual = sha256_file(path)
                    checked_records += 1
                    if actual != record["sha256"]:
                        mismatched_records.append({
                            "asset_path": asset_path or "",
                            "kind": kind,
                            "physical_path": str(path),
                            "captured_sha256": record["sha256"],
                            "current_sha256": actual,
                        })

    uncovered = sorted(mismatch_paths - graph_paths)
    native_asset_path = repo / report.get("rotor_geometry", {}).get(
        "installed_asset_path", "Assets/Game/Resources/SkirmishAircraftRotorGeometry_V1.asset")
    native_asset_text = native_asset_path.read_text(encoding="utf-8")
    native_match = re.search(r"^\s*NativeTemplateProvenanceSha256:\s*([0-9a-f]{64})\s*$", native_asset_text, re.MULTILINE)
    declared_native_hash = native_match.group(1) if native_match else None
    authoring_link = False  # This artifact set contains no receipt binding provenance JSON to asset authoring.
    passed = not missing_records and not mismatched_records and not uncovered
    return {
        "kind": "S073AircraftGeometryDriftOfflineByteAudit",
        "schema_version": 1,
        "scope": "ReadOnlyPinnedEvidenceAndCurrentFilesystemByteComparison_NoUnity_NoAssetWrites",
        "status": "Passed_ByteClosureOnly" if passed else "Incomplete_ByteClosure",
        "pins": {
            "drift_report_path": str(report_path),
            "drift_report_sha256": report_hash,
            "provenance_report_path": str(provenance_path),
            "provenance_report_sha256": provenance_hash,
        },
        "counts": {
            "rotor_dependency_hash_mismatch_rows": sum(bool(r.get("dependency_hash_mismatch")) for r in report.get("rotor_geometry", {}).get("rows", [])),
            "canonical_dependency_hash_mismatch_sources": sum(bool(c.get("dependency_hash_mismatch")) for role in report.get("canonical_geometry", {}).get("roles", []) for c in role.get("source_comparisons", [])),
            "unique_mismatched_asset_paths": len(mismatch_paths),
            "unique_provenance_asset_paths": len(graph_paths),
            "mismatched_paths_covered": len(mismatch_paths & graph_paths),
            "raw_source_and_meta_hash_records_checked": checked_records,
            "raw_hash_mismatches": len(mismatched_records),
            "missing_raw_hash_records": len(missing_records),
            "mismatched_paths_uncovered": len(uncovered),
        },
        "mismatched_paths_uncovered": uncovered,
        "missing_raw_hash_records": missing_records,
        "raw_hash_mismatches": mismatched_records,
        "installed_native_template_asset_path": str(native_asset_path),
        "installed_native_template_provenance_sha256": declared_native_hash,
        "provenance_report_sha256_matches_installed_native_template_declaration": PROVENANCE_SHA256 == declared_native_hash,
        "authoring_receipt_binds_provenance_report_to_installed_asset": authoring_link,
        "limitation": "Current bytes match the captured source/meta hashes for the listed closure. The report does not explain the differing AssetDatabase dependency hashes. The provenance report SHA differs from the installed native-template declaration, and no authoring receipt binds this provenance report to the installed asset; cause remains undetermined.",
        "mission_ready": False,
        "player_readiness": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[4])
    parser.add_argument("--report", type=Path)
    parser.add_argument("--provenance", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    repo = args.repo.resolve()
    report_path = (args.report or repo / REPORT_REL).resolve()
    provenance_path = (args.provenance or repo / PROVENANCE_REL).resolve()
    try:
        result = audit(report_path, provenance_path, repo)
    except Exception as exc:
        result = {"kind": "S073AircraftGeometryDriftOfflineByteAudit", "schema_version": 1,
                  "scope": "ReadOnlyPinnedEvidenceAndCurrentFilesystemByteComparison_NoUnity_NoAssetWrites",
                  "status": "FailedClosed", "error": f"{type(exc).__name__}: {exc}",
                  "mission_ready": False, "player_readiness": False}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"[S073AircraftGeometryDriftOfflineByteAudit] result={result['status']} output={args.output}")
    return 0 if result["status"] == "Passed_ByteClosureOnly" else 1


if __name__ == "__main__":
    raise SystemExit(main())

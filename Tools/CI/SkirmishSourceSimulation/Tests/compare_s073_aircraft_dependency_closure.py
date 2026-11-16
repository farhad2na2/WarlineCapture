#!/usr/bin/env python3
"""Strict offline comparison of current aircraft dependency closure to retained provenance."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
from collections import defaultdict
from pathlib import Path
from typing import Any


CLOSURE_REL = Path("Logs/SkirmishProduction/s073-aircraft-dependency-closure-20261103-r2/closure.json")
PROVENANCE_REL = Path("Logs/SkirmishProduction/AircraftRootProvenance/910f18719906473db6b2057696c181b0.json")
DRIFT_REL = Path("Logs/SkirmishProduction/s073-aircraft-geometry-drift-20261102-r1/report.json")
CLOSURE_SHA256 = "79669719b755496bdc136e352589fd54ddd4b7efb4e5a08337d508ed1000e0e6"
PROVENANCE_SHA256 = "7b2d7e900a97d6b690a2f2017ba0f7fd6ef6b02b74efa77f1473e0faf60c7a5f"
DRIFT_SHA256 = "801eebd8adb5b36229afb9be89bf83b7fef2e213a92cb64adb5a229f2b57b6e2"
NATIVE_TEMPLATE_SHA256 = "5d7aa45d232a08b3a1336c0166d8f8834bf18758b92f1af9765acd218b1ea3bc"
HEX64 = re.compile(r"^[0-9a-f]{64}$")


class AuditError(RuntimeError):
    pass


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def report_roots(report: dict[str, Any]) -> set[str]:
    paths: set[str] = set()
    for row in report.get("rotor_geometry", {}).get("rows", []):
        path = row.get("installed", {}).get("source_path")
        if path:
            paths.add(path)
    for role in report.get("canonical_geometry", {}).get("roles", []):
        for comparison in role.get("source_comparisons", []):
            path = comparison.get("installed", {}).get("path")
            if path:
                paths.add(path)
    return paths


def _expected_dependencies(graphs: list[dict[str, Any]], root: str, errors: list[str]) -> dict[str, dict[str, str]]:
    usable = [g for g in graphs if g.get("rootAssetPath") == root]
    if not usable:
        errors.append(f"provenance root missing: {root}")
        return {}
    normalized_graphs: list[dict[str, dict[str, str]]] = []
    for graph in usable:
        if graph.get("status") != "Collected" or graph.get("captured") is not True:
            errors.append(f"provenance graph unavailable/unknown for {root}: status={graph.get('status')!r}")
        if graph.get("dependencyHashBefore") != graph.get("dependencyHashAfter"):
            errors.append(f"provenance dependency hash unstable during capture: {root}")
        records: dict[str, dict[str, str]] = {}
        for dep in graph.get("dependencies", []):
            path = dep.get("assetPath")
            if not isinstance(path, str) or not path:
                errors.append(f"provenance dependency path unknown for {root}")
                continue
            if path in records:
                errors.append(f"duplicate provenance dependency path for {root}: {path}")
                continue
            records[path] = {}
            for kind in ("source", "meta"):
                value = dep.get(kind)
                digest = value.get("sha256") if isinstance(value, dict) else None
                if not isinstance(value, dict) or value.get("exists") is not True or value.get("status") != "HashedRawBytes" or not isinstance(digest, str) or not HEX64.fullmatch(digest):
                    errors.append(f"provenance {kind} hash/status unknown for {root}: {path}")
                    continue
                records[path][kind] = digest
        normalized_graphs.append(records)
    if not normalized_graphs:
        return {}
    first = normalized_graphs[0]
    for other in normalized_graphs[1:]:
        if other != first:
            errors.append(f"conflicting duplicate provenance graphs for root: {root}")
    return first


def compare(closure: dict[str, Any], provenance: dict[str, Any], report: dict[str, Any]) -> dict[str, Any]:
    errors: list[str] = []
    root_rows = closure.get("sources")
    if closure.get("kind") != "S073AircraftDependencyClosureDiagnostic" or closure.get("schema_version") != 1:
        errors.append("closure kind/schema unknown")
    if closure.get("scope") != "PinnedDriftReportAircraftPrefabSources_AssetDatabaseDependenciesAndRawFileHashes_ReadOnly_NoPlayOrGameplayState":
        errors.append("closure scope unknown")
    if closure.get("status") != "Passed" or closure.get("native_readiness") is not False or closure.get("mission_world_loaded") is not False:
        errors.append("closure status/world/readiness fields not expected fail-closed values")
    if closure.get("asset_database_closure_recursive") is not True or not isinstance(root_rows, list):
        errors.append("recursive closure or source rows unavailable")
        root_rows = root_rows if isinstance(root_rows, list) else []
    if closure.get("source_count") != len(root_rows):
        errors.append("closure source_count does not match source rows")
    if provenance.get("scope") != "ReadOnlyAllSevenCanonicalAircraftRootBladeDrawBindings" or provenance.get("sourceRoleCount") != 7:
        errors.append("provenance scope or role count unknown")
    expected_roots = report_roots(report)
    actual_roots = [row.get("source_path") for row in root_rows]
    actual_root_set = {x for x in actual_roots if isinstance(x, str)}
    duplicate_roots = sorted(x for x in actual_root_set if actual_roots.count(x) > 1)
    missing_roots = sorted(expected_roots - actual_root_set)
    added_roots = sorted(actual_root_set - expected_roots)
    if duplicate_roots:
        errors.append("duplicate current roots: " + ", ".join(duplicate_roots))
    if missing_roots:
        errors.append("current roots missing from report: " + ", ".join(missing_roots))
    if added_roots:
        errors.append("current roots added beyond report: " + ", ".join(added_roots))

    graphs = [g for s in provenance.get("sources", []) for g in s.get("importerDependencyGraphs", [])]
    root_results: list[dict[str, Any]] = []
    path_count = byte_records_checked = 0
    for row in root_rows:
        root = row.get("source_path")
        if not isinstance(root, str) or not root:
            errors.append("current root path unknown")
            continue
        db_hash = row.get("asset_database_dependency_hash", {})
        if not isinstance(db_hash, dict) or db_hash.get("status") != "Available" or not isinstance(db_hash.get("value"), str) or not db_hash.get("value"):
            errors.append(f"current AssetDatabase dependency hash unavailable/unknown: {root}")
            current_db_hash = None
        else:
            current_db_hash = db_hash["value"]
        if row.get("closure_status") != "Available":
            errors.append(f"current closure unavailable/unknown: {root}")
        expected = _expected_dependencies(graphs, root, errors)
        deps = row.get("dependencies")
        if not isinstance(deps, list):
            errors.append(f"current dependency list unknown: {root}")
            deps = []
        current: dict[str, dict[str, str]] = {}
        for dep in deps:
            path = dep.get("asset_path")
            if not isinstance(path, str) or not path:
                errors.append(f"current dependency path unknown: {root}")
                continue
            if path in current:
                errors.append(f"duplicate current dependency path: {root}: {path}")
                continue
            current[path] = {}
            for key, kind in (("raw_bytes", "source"), ("meta_bytes", "meta")):
                value = dep.get(key)
                status = value.get("status") if isinstance(value, dict) else None
                digest = value.get("sha256") if isinstance(value, dict) else None
                if status != "Available" or not isinstance(digest, str) or not HEX64.fullmatch(digest):
                    errors.append(f"current {kind} bytes unavailable/unknown: {root}: {path}")
                    continue
                current[path][kind] = digest
                byte_records_checked += 1
        expected_paths, current_paths = set(expected), set(current)
        missing = sorted(expected_paths - current_paths)
        added = sorted(current_paths - expected_paths)
        changed = []
        for path in sorted(expected_paths & current_paths):
            for kind in ("source", "meta"):
                old, new = expected[path].get(kind), current[path].get(kind)
                if old is None or new is None:
                    continue
                if old != new:
                    changed.append({"asset_path": path, "kind": kind,
                                    "provenance_sha256": old, "current_sha256": new})
        if missing:
            errors.append(f"dependency paths missing for {root}: {', '.join(missing)}")
        if added:
            errors.append(f"dependency paths added for {root}: {', '.join(added)}")
        if changed:
            errors.append(f"dependency bytes differ for {root}: {len(changed)}")
        path_count += len(expected_paths & current_paths)
        matching_graphs = [g for g in graphs if g.get("rootAssetPath") == root]
        provenance_db = matching_graphs[0].get("dependencyHashAfter") if matching_graphs else None
        if any(g.get("dependencyHashAfter") != current_db_hash for g in matching_graphs):
            errors.append(f"AssetDatabase dependency hash differs from provenance capture: {root}")
        root_results.append({
            "source_path": root,
            "current_asset_database_dependency_hash": current_db_hash,
            "provenance_dependency_hash_after": provenance_db,
            "asset_database_dependency_hash_matches_provenance": bool(matching_graphs) and all(g.get("dependencyHashAfter") == current_db_hash for g in matching_graphs),
            "provenance_dependency_path_count": len(expected_paths),
            "current_dependency_path_count": len(current_paths),
            "missing_dependency_paths": missing,
            "added_dependency_paths": added,
            "raw_byte_hash_mismatches": changed,
        })

    native_path = "Assets/Game/Resources/SkirmishAircraftRotorGeometry_V1.asset"
    # The installed declaration is pinned as a known limitation from the asset source; no authoring receipt binds it to this provenance JSON.
    passed = not errors
    return {
        "kind": "S073AircraftDependencyClosureComparison",
        "schema_version": 1,
        "scope": "ReadOnlyPinnedCurrentClosureVersusRetainedProvenanceAndDriftReport_NoCausalInference_NoReadinessClaim",
        "status": "Passed_ExactClosureComparison" if passed else "IncompleteOrMismatch",
        "pins": {
            "closure_sha256": CLOSURE_SHA256,
            "provenance_sha256": PROVENANCE_SHA256,
            "drift_report_sha256": DRIFT_SHA256,
        },
        "counts": {
            "drift_report_expected_root_count": len(expected_roots),
            "current_root_count": len(actual_roots),
            "roots_compared": len(root_results),
            "roots_with_matching_asset_database_dependency_hash": sum(bool(x["asset_database_dependency_hash_matches_provenance"]) for x in root_results),
            "dependency_paths_compared": path_count,
            "source_and_meta_hash_records_compared": byte_records_checked,
            "root_errors": len(errors),
        },
        "root_comparisons": root_results,
        "errors": errors,
        "installed_native_template_asset_path": native_path,
        "installed_native_template_provenance_sha256": NATIVE_TEMPLATE_SHA256,
        "provenance_report_sha256_matches_installed_native_template_declaration": PROVENANCE_SHA256 == NATIVE_TEMPLATE_SHA256,
        "authoring_receipt_binds_provenance_report_to_installed_asset": False,
        "interpretation_limit": "Matching current dependency paths, raw bytes, and AssetDatabase hashes to this retained report does not prove that this report was the exact authoring input for the installed geometry asset. It does not explain why the geometry drift report's installed dependency hashes differ from the measured hashes, and establishes no readiness.",
        "unity_dependency_hash_cause_proven": False,
        "mission_ready": False,
        "player_readiness": False,
    }


def _load(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise AuditError(f"expected JSON object: {path}")
    return value


def run(closure_path: Path, provenance_path: Path, report_path: Path) -> dict[str, Any]:
    actual_hashes = (sha256_file(closure_path), sha256_file(provenance_path), sha256_file(report_path))
    expected_hashes = (CLOSURE_SHA256, PROVENANCE_SHA256, DRIFT_SHA256)
    labels = ("closure", "provenance", "drift report")
    for label, actual, expected in zip(labels, actual_hashes, expected_hashes):
        if actual != expected:
            raise AuditError(f"{label} SHA256 mismatch: expected={expected} actual={actual}")
    closure, provenance, report = (_load(p) for p in (closure_path, provenance_path, report_path))
    if closure.get("input_report_sha256") != DRIFT_SHA256 or closure.get("expected_input_report_sha256") != DRIFT_SHA256:
        raise AuditError("closure's pinned input report hashes do not match drift report pin")
    if closure.get("input_report") != DRIFT_REL.as_posix():
        raise AuditError("closure input report path does not match pinned drift report")
    return compare(closure, provenance, report)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[4])
    parser.add_argument("--closure", type=Path)
    parser.add_argument("--provenance", type=Path)
    parser.add_argument("--report", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    repo = args.repo.resolve()
    paths = (args.closure or repo / CLOSURE_REL, args.provenance or repo / PROVENANCE_REL, args.report or repo / DRIFT_REL)
    try:
        result = run(*(p.resolve() for p in paths))
    except Exception as exc:
        result = {"kind": "S073AircraftDependencyClosureComparison", "schema_version": 1,
                  "scope": "ReadOnlyPinnedCurrentClosureVersusRetainedProvenanceAndDriftReport_NoCausalInference_NoReadinessClaim",
                  "status": "FailedClosed", "error": f"{type(exc).__name__}: {exc}",
                  "unity_dependency_hash_cause_proven": False, "mission_ready": False, "player_readiness": False}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"[S073AircraftDependencyClosureComparison] result={result['status']} output={args.output}")
    return 0 if result["status"] == "Passed_ExactClosureComparison" else 1


if __name__ == "__main__":
    raise SystemExit(main())

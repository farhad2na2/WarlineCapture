#!/usr/bin/env python3
"""Run an explicitly test-issued S002 paid-production-to-move source diagnostic.

The worker is admitted only from a qualified S002 bundle after auditing every
reachable captured source_pins closure. This reports semantic source behavior
only; it does not claim ARIA/native UI/touch or mission readiness.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import subprocess
import sys
import tempfile
import time
from pathlib import Path


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


CURRENT_PACKAGE = Path("T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T12/package.json")
DIAGNOSTIC_SOURCE_MANIFEST = Path("T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T12/source_manifest.json")
CANONICAL_SOURCE_MANIFEST = Path("Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json")
BASE_VALIDATED_PACKAGE = Path("T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T2/package.json")
BASE_VALIDATED_PACKAGE_SHA = "de74b8fa217fdb515b3bbdbc36c26f8f18761f5b1ffae6156278823a03563628"
CURRENT_BUNDLE = Path("T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T3/data-bundle-r10-copied-initialization.json")
CURRENT_BUNDLE_SHA = "7d99d568efc0c4e8dd1774d79ba919fdf561e25b35c6167dab87a1003ddabf8f"
BUNDLE_DERIVATION_RECEIPT = Path("T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T3/bundle-derivation-receipt.json")
BUNDLE_DERIVATION_RECEIPT_SHA = "0fbee5110d02eb108aeaad56ced53ccd5c90e2e7851d3a45194c8216e0f0fe34"
COPIED_R10_INITIALIZATION = Path("T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T3/base-initialization-r10.json")
COPIED_R10_INITIALIZATION_SHA = "832fd44c042898b282c9c8c532a5d9904a2c4660554183710da1d4dd96d3dc63"
HISTORICAL_INPUTS = {
    "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json": "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8",
    "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-water-capture.json": "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5",
    "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-startup-observation.json": "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03",
    "Logs/SkirmishProduction/s073-captured-source-preservation-20261101-r1/receipt.json": "68b2a130fe08e3f952b1a8c1c0603b082ec38c7feb8b4581e1153cba7665e297",
}


def read_regular(path: Path, maximum: int = 256 * 1024 * 1024) -> bytes:
    if not path.is_absolute() or path.is_symlink() or not path.is_file():
        raise ValueError(f"Regular absolute source input required: {path}")
    if path.stat().st_size < 1 or path.stat().st_size > maximum:
        raise ValueError(f"Bounded source input required: {path}")
    return path.read_bytes()


def pin_runner_source(path: Path):
    path = path.resolve(strict=True)
    return {"path": str(path), "sha256": sha(read_regular(path))}


def runner_source_pin_matches(pin, path: Path) -> bool:
    try:
        return (isinstance(pin, dict) and pin.get("path") == str(path.resolve(strict=True)) and
                sha(read_regular(path)) == pin.get("sha256"))
    except (OSError, ValueError):
        return False


def diagnostic_attestation_build_mismatches(build: dict, build_path: Path,
                                            expected_attestation_sha: str, expected_manifest_sha: str,
                                            expected_manifest_bytes: int, repo: Path) -> list[dict]:
    """Require the exact diagnostic manifest and generated package attestation in the passed build."""
    failures = []
    canonical_desc = build.get("canonical_source_manifest")
    canonical_path = (repo / CANONICAL_SOURCE_MANIFEST).resolve()
    if not isinstance(canonical_desc, dict) or set(canonical_desc) != {"path", "sha256", "bytes"}:
        return [{"reason": "Canonical_default_manifest_descriptor_required"}]
    try:
        canonical_raw = read_regular(Path(canonical_desc["path"]), 16 * 1024 * 1024)
    except (OSError, ValueError) as exc:
        return [{"reason": "Canonical_default_manifest_unreadable", "error": str(exc)}]
    if (Path(canonical_desc["path"]).resolve() != canonical_path or
            sha(canonical_raw) != canonical_desc["sha256"] or len(canonical_raw) != canonical_desc["bytes"]):
        failures.append({"reason": "Canonical_default_manifest_changed_or_aliased"})
    manifest_desc = build.get("source_manifest")
    if not isinstance(manifest_desc, dict) or set(manifest_desc) != {"path", "sha256", "bytes"}:
        return failures + [{"reason": "Diagnostic_source_manifest_descriptor_required"}]
    expected_manifest_path = (repo / DIAGNOSTIC_SOURCE_MANIFEST).resolve()
    try:
        manifest_path = Path(manifest_desc["path"])
        manifest_raw = read_regular(manifest_path, 16 * 1024 * 1024)
        manifest_value = json.loads(manifest_raw)
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        return failures + [{"reason": "Diagnostic_manifest_unreadable", "error": str(exc)}]
    manifest_generator = "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/generate_s002_selected_map_graph_attestation.py"
    rows = [row for row in manifest_value.get("sources", [])
            if isinstance(row, dict) and row.get("id") == "S002CurrentSelectedMapGraphBuildAttestation.cs"]
    if (manifest_path.resolve() != expected_manifest_path or
            sha(manifest_raw) != manifest_desc["sha256"] or
            manifest_desc["sha256"] != expected_manifest_sha or
            len(manifest_raw) != expected_manifest_bytes or
            manifest_desc["bytes"] != expected_manifest_bytes or
            len(manifest_raw) != manifest_desc["bytes"] or
            "S002_CURRENT_PACKAGE_DIAGNOSTIC" not in manifest_value.get("compiler_defines", []) or
            len(rows) != 1 or rows[0].get("kind") != "current_original" or
            rows[0].get("generator") != manifest_generator or
                rows[0].get("output") != "S002CurrentSelectedMapGraphBuildAttestation.cs"):
        failures.append({"reason": "Diagnostic_manifest_attestation_source_mismatch"})
    expected_source = (build_path.parent / "source/generated/S002CurrentSelectedMapGraphBuildAttestation.cs").resolve()
    pins = build.get("source_and_reference_pins", {})
    matches = [(path, digest) for path, digest in pins.items()
               if Path(path).name == "S002CurrentSelectedMapGraphBuildAttestation.cs"]
    if (len(matches) != 1 or Path(matches[0][0]).resolve() != expected_source or
            matches[0][1] != expected_attestation_sha):
        failures.append({"reason": "Build_attestation_source_digest_mismatch", "matches": matches})
    return failures


def audit_current_package_inputs(bundle_path: Path, package_path: Path, package_sha: str, repo: Path):
    """Validate the explicit current-owner package and its copied r10 bundle without legacy capture promotion."""
    repo = repo.resolve(strict=True)
    expected_bundle = repo / CURRENT_BUNDLE
    expected_package = repo / CURRENT_PACKAGE
    if bundle_path.resolve(strict=True) != expected_bundle or package_path.resolve(strict=True) != expected_package:
        raise ValueError("Exact current S002 diagnostic bundle/package paths required")
    observed_package_raw = read_regular(package_path)
    if sha(observed_package_raw) != package_sha:
        raise ValueError("Current diagnostic package descriptor digest mismatch")
    sys.path.insert(0, str(repo / "Tools/CI"))
    import s002_current_source_diagnostic_package as package_reader
    graph_generator_path = repo / "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/generate_s002_selected_map_graph_attestation.py"
    import importlib.util
    spec = importlib.util.spec_from_file_location("s002_selected_map_attestation_runner", graph_generator_path)
    graph_generator = importlib.util.module_from_spec(spec)
    if spec is None or spec.loader is None:
        raise ValueError("Current selected-map graph attestation generator unavailable")
    spec.loader.exec_module(graph_generator)
    with tempfile.TemporaryDirectory(prefix="s002-selected-map-runner-replay-") as temp:
        generated = graph_generator.generate(repo, Path(temp) / "generation")
    base_package_path = repo / BASE_VALIDATED_PACKAGE
    base_package = package_reader.load_current_s002_package(
        base_package_path, BASE_VALIDATED_PACKAGE_SHA, repo)
    expected_value = dict(base_package["value"])
    expected_value["source_config_graph"] = {
        "path": str((repo / CURRENT_PACKAGE.parent / "source-config-graph.json").resolve()),
        "sha256": generated["enriched_graph_sha256"],
    }
    expected_package_raw = (json.dumps(expected_value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    expected_package_sha = sha(expected_package_raw)
    package_raw = observed_package_raw
    if (package_sha != expected_package_sha or sha(package_raw) != expected_package_sha or
            package_raw != expected_package_raw or generated["derived_package_path"] != str(expected_package) or
            generated["derived_package_sha256"] != expected_package_sha):
        raise ValueError("Current package bytes do not reproduce from strict owner evidence and selected-map graph")
    loaded = dict(base_package)
    loaded.update({"path": str(expected_package), "sha256": expected_package_sha,
                   "value": expected_value, "source_config_graph": expected_value["source_config_graph"]})
    loaded_pins = dict(base_package["source_pins"])
    for path, digest in generated["source_pins"].items():
        prior = loaded_pins.get(path)
        if prior is not None and prior != digest:
            raise ValueError("Selected-map graph producer conflicts with package source pin: " + path)
        loaded_pins[path] = digest
    for relative, digest in ((CURRENT_PACKAGE, expected_package_sha),
                             (CURRENT_PACKAGE.parent / "source-config-graph.json", generated["enriched_graph_sha256"]),
                             (CURRENT_PACKAGE.parent / "graph-preparation-proof.json", sha((repo / CURRENT_PACKAGE.parent / "graph-preparation-proof.json").read_bytes()))):
        loaded_pins[str((repo / relative).resolve())] = digest
    loaded["source_pins"] = loaded_pins
    manifest_preparer_path = repo / "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/prepare_s002_current_package_source_manifest.py"
    manifest_spec = importlib.util.spec_from_file_location("s002_current_package_manifest_replay", manifest_preparer_path)
    if manifest_spec is None or manifest_spec.loader is None:
        raise ValueError("Current diagnostic source-manifest preparer unavailable")
    manifest_preparer = importlib.util.module_from_spec(manifest_spec)
    manifest_spec.loader.exec_module(manifest_preparer)
    with tempfile.TemporaryDirectory(prefix="s002-current-package-manifest-replay-") as temp:
        replay_manifest = Path(temp) / "source_manifest.json"
        manifest_detail = manifest_preparer.prepare(repo, replay_manifest)
        expected_manifest_raw = read_regular(replay_manifest, 16 * 1024 * 1024)
        replay_proof_path = replay_manifest.with_name("source_manifest.proof.json")
        replay_proof = json.loads(read_regular(replay_proof_path, 16 * 1024 * 1024))
    durable_manifest_path = repo / DIAGNOSTIC_SOURCE_MANIFEST
    durable_proof_path = durable_manifest_path.with_name("source_manifest.proof.json")
    durable_manifest_raw = read_regular(durable_manifest_path, 16 * 1024 * 1024)
    durable_proof = json.loads(read_regular(durable_proof_path, 16 * 1024 * 1024))
    replay_proof["manifest"]["path"] = str(durable_manifest_path.resolve())
    expected_proof_raw = (json.dumps(replay_proof, indent=2, sort_keys=True, ensure_ascii=False) + "\n").encode("utf-8")
    if durable_manifest_raw != expected_manifest_raw or durable_proof != json.loads(expected_proof_raw) or durable_proof["manifest"] != {
            "path": str(durable_manifest_path.resolve()), "sha256": sha(durable_manifest_raw),
            "bytes": len(durable_manifest_raw)}:
        raise ValueError("Durable T12 diagnostic manifest/proof differs from independent preparation")
    manifest_sha = sha(durable_manifest_raw)
    proof_sha = sha(durable_proof_path.read_bytes())
    manifest_pins = durable_proof.get("source_pins")
    if not isinstance(manifest_pins, dict) or len(manifest_pins) != durable_proof.get("source_pin_count"):
        raise ValueError("Complete source-manifest generator/helper pin closure required")
    for path, digest in manifest_pins.items():
        if sha(read_regular(Path(path))) != digest:
            raise ValueError("Diagnostic manifest proof input changed: " + path)
        loaded_pins[path] = digest
    loaded_pins[str(durable_manifest_path.resolve())] = manifest_sha
    loaded_pins[str(durable_proof_path.resolve())] = proof_sha
    loaded_pins[str(manifest_preparer_path.resolve())] = sha(manifest_preparer_path.read_bytes())
    bundle_raw = read_regular(bundle_path)
    if sha(bundle_raw) != CURRENT_BUNDLE_SHA:
        raise ValueError("Derived copied-initialization bundle changed")
    bundle = package_reader._json(bundle_raw)
    expected_init = repo / COPIED_R10_INITIALIZATION
    if (bundle.get("mission"), bundle.get("seed"), bundle.get("runtime_backend")) != ("S002", 104731, "LegacyGroundS002V1") or bundle.get("initialization") != {"path": str(expected_init), "sha256": COPIED_R10_INITIALIZATION_SHA}:
        raise ValueError("Exact S002 r10 bundle/copy identity required")
    receipt_path = repo / BUNDLE_DERIVATION_RECEIPT
    receipt_raw = read_regular(receipt_path)
    if sha(receipt_raw) != BUNDLE_DERIVATION_RECEIPT_SHA:
        raise ValueError("Copied-initialization bundle derivation receipt changed")
    receipt = package_reader._json(receipt_raw)
    expected_source_bundle = "/private/tmp/s002-delivery-source-run-20261102-r10/data/data-bundle.json"
    expected_source_init = "/private/tmp/s002-delivery-source-run-20261102-r10/data/initialization.json"
    if (receipt.get("kind") != "S002R10BundleInitializationCopyDerivation" or
            receipt.get("source_bundle") != {"path": expected_source_bundle, "sha256": "0b17792d0dd956206215f6fb28108322748810f59af8704fff74eb2821ba1ee0"} or
            receipt.get("source_initialization") != {"path": expected_source_init, "sha256": COPIED_R10_INITIALIZATION_SHA, "external_locator_reopened": True} or
            receipt.get("copied_initialization") != {"path": str(expected_init), "sha256": COPIED_R10_INITIALIZATION_SHA, "byte_identical_to_source": True} or
            receipt.get("derived_bundle") != {"path": str(bundle_path), "sha256": CURRENT_BUNDLE_SHA} or
            receipt.get("changed_field") != "initialization.path" or
            any(receipt.get(key) is not False for key in ("native_context_qualified", "source_fit_certified", "mission_ready"))):
        raise ValueError("Exact path-only r10 bundle derivation receipt required")
    pins = dict(loaded["source_pins"])
    historical_binding = package_reader.load_s002_historical_placement_binding(repo)
    for path, digest in ((bundle_path, CURRENT_BUNDLE_SHA), (package_path, expected_package_sha),
                         (receipt_path, BUNDLE_DERIVATION_RECEIPT_SHA),
                         (expected_init, COPIED_R10_INITIALIZATION_SHA)):
        if sha(read_regular(path)) != digest:
            raise ValueError("Current package/bundle derivation input changed: " + str(path))
        pins[str(path.resolve())] = digest
    for relative, digest in HISTORICAL_INPUTS.items():
        path = repo / relative
        if sha(read_regular(path)) != digest:
            raise ValueError("Retained historical physical input changed: " + relative)
        pins[str(path.resolve())] = digest
    for path, digest in historical_binding["source_pins"].items():
        pins[path] = digest
    report = {
        "status": "Passed", "kind": "S002CurrentOwnerInputs_WithHistoricalPhysicalDiagnosticInputAudit",
        "scope": "Current owner artifacts and exact retained historical physical inputs only; no capture-closure, factory, admission, source-fit, or readiness promotion",
        "bundle_path": str(bundle_path), "bundle_sha256": CURRENT_BUNDLE_SHA,
        "package_path": str(package_path), "package_sha256": expected_package_sha,
        "enriched_graph_sha256": generated["enriched_graph_sha256"],
        "attestation_source_sha256": generated["attestation_source_sha256"],
        "producer_proof_sha256": generated["producer_proof_sha256"],
        "source_manifest_path": str(durable_manifest_path.resolve()),
        "source_manifest_sha256": manifest_sha,
        "source_manifest_bytes": len(durable_manifest_raw),
        "source_manifest_proof_path": str(durable_proof_path.resolve()),
        "source_manifest_proof_sha256": proof_sha,
        "source_manifest_rederived_sha256": manifest_detail["sha256"],
        "source_pin_count": len(pins), "worker_admitted": False,
        "historical_placement_descriptor_source": historical_binding["source_initialization"],
        "native_context_qualified": False, "source_fit_certified": False, "mission_ready": False,
    }
    return report, pins, loaded


def _object_after_key(raw: bytes, name: str):
    """Extract one JSON object-valued member without materializing a huge capture."""
    token = json.dumps(name).encode("ascii")
    cursor = 0
    while True:
        found = raw.find(token, cursor)
        if found < 0:
            return None
        colon = raw.find(b":", found + len(token))
        if colon < 0:
            return None
        start = colon + 1
        while start < len(raw) and raw[start] in b" \t\r\n":
            start += 1
        if start >= len(raw) or raw[start] != ord("{"):
            cursor = found + len(token)
            continue
        depth = 0
        quoted = False
        escaped = False
        for end in range(start, len(raw)):
            byte = raw[end]
            if quoted:
                if escaped:
                    escaped = False
                elif byte == ord("\\"):
                    escaped = True
                elif byte == ord('"'):
                    quoted = False
                continue
            if byte == ord('"'):
                quoted = True
            elif byte == ord("{"):
                depth += 1
            elif byte == ord("}"):
                depth -= 1
                if depth == 0:
                    try:
                        value = json.loads(raw[start:end + 1])
                    except (json.JSONDecodeError, UnicodeDecodeError):
                        cursor = found + len(token)
                        break
                    if isinstance(value, dict):
                        return value
                    cursor = found + len(token)
                    break
        else:
            return None


def _walk(value, visit):
    if isinstance(value, dict):
        visit(value)
        for child in value.values():
            _walk(child, visit)
    elif isinstance(value, list):
        for child in value:
            _walk(child, visit)


def audit_reachable_inputs(bundle_path: Path, qualification_path: Path, repo: Path):
    """Pin bundle inputs and every reachable JSON source_pins map; never repin."""
    bundle_raw = read_regular(bundle_path)
    bundle = json.loads(bundle_raw)
    qualification_raw = read_regular(qualification_path)
    qualification = json.loads(qualification_raw)
    if (bundle.get("mission"), bundle.get("seed"), bundle.get("runtime_backend")) != (
        "S002", 104731, "LegacyGroundS002V1"
    ):
        raise ValueError("Only the qualified S002/104731 ground bundle is admitted")
    if qualification.get("kind") != "S002QualifiedFocusedPrefabBundleRebind":
        raise ValueError("Exact S002 focused prefab rebind qualification receipt required")
    if Path(qualification.get("qualified_bundle_path", "")).resolve() != bundle_path.resolve():
        raise ValueError("Qualification receipt points at a different S002 bundle")
    if qualification.get("qualified_bundle_sha256") != sha(bundle_raw):
        raise ValueError("Qualified S002 bundle hash differs from its receipt")
    init_descriptor = bundle.get("initialization")
    if not isinstance(init_descriptor, dict) or Path(qualification.get("qualified_initialization_path", "")).resolve() != Path(init_descriptor.get("path", "")).resolve() or qualification.get("qualified_initialization_sha256") != init_descriptor.get("sha256"):
        raise ValueError("Qualified S002 initialization descriptor differs from its receipt")
    if qualification.get("mission_ready") is not False or qualification.get("native_visual_readiness") is not False:
        raise ValueError("Qualified source bundle must retain its non-readiness scope")

    roots = [bundle_path, qualification_path]
    seen: set[str] = set()
    source_pins: dict[str, str] = {}
    maps: dict[str, int] = {}
    map_entries: dict[str, list[tuple[str, str]]] = {}
    descriptors: dict[str, str] = {}
    conflicts = []
    queue: list[Path] = []

    def enqueue_descriptor(value):
        if not isinstance(value, dict) or not isinstance(value.get("path"), str) or not isinstance(value.get("sha256"), str):
            return
        candidate = Path(value["path"])
        if candidate.is_absolute() and len(value["sha256"]) == 64:
            queue.append(candidate)
            descriptors[str(candidate)] = value["sha256"]

    # The bundle's initialization points to the exact original authored graph,
    # prefab capture, unit metadata, and other data artifacts.
    for root in roots:
        _walk(bundle if root == bundle_path else qualification, enqueue_descriptor)
    while queue:
        path = queue.pop()
        key = str(path)
        if key in seen:
            continue
        seen.add(key)
        try:
            raw = read_regular(path)
        except (OSError, ValueError) as error:
            conflicts.append({"path": key, "expected": descriptors.get(key), "actual": "MISSING_OR_INVALID",
                              "kind": "descriptor_unavailable", "error": str(error)})
            continue
        expected = descriptors.get(key)
        actual = sha(raw)
        if expected is not None and expected != actual:
            conflicts.append({"path": key, "expected": expected, "actual": actual, "kind": "descriptor_hash"})
            continue
        if path.suffix.lower() != ".json":
            continue
        if len(raw) > 12 * 1024 * 1024:
            pin_map = _object_after_key(raw, "source_pins")
            document = None
        else:
            document = json.loads(raw)
            pin_map = None

        maps_found = []
        if pin_map is not None:
            maps_found.append(pin_map)
        if document is not None:
            def collect(value):
                candidate = value.get("source_pins")
                if isinstance(candidate, dict):
                    maps_found.append(candidate)
                enqueue_descriptor(value)
                # Legacy source reference descriptor is an adjacent path/hash pair.
                if isinstance(value.get("source_reference_path"), str) and isinstance(value.get("source_reference_sha256"), str):
                    enqueue_descriptor({"path": value["source_reference_path"], "sha256": value["source_reference_sha256"]})
            _walk(document, collect)
        else:
            # The large captured navigation artifact has no nested descriptor
            # contract; its source_pins closure is still audited in full.
            pass
        for index, pins in enumerate(maps_found):
            map_id = f"{key}#source_pins[{index}]"
            maps[map_id] = len(pins)
            map_entries[map_id] = []
            for pin_path, pin_sha in pins.items():
                if not isinstance(pin_path, str) or not isinstance(pin_sha, str) or len(pin_sha) != 64:
                    conflicts.append({"path": str(pin_path), "expected": pin_sha, "actual": "INVALID_PIN", "kind": "invalid_source_pin"})
                    continue
                declared_path = Path(pin_path)
                canonical_path = declared_path if declared_path.is_absolute() else repo / declared_path
                canonical_name = str(canonical_path.resolve(strict=False))
                map_entries[map_id].append((canonical_name, pin_sha))
                previous = source_pins.get(canonical_name)
                if previous is not None and previous != pin_sha:
                    conflicts.append({"path": canonical_name, "expected": previous, "actual": pin_sha, "kind": "conflicting_source_pins"})
                    continue
                source_pins[canonical_name] = pin_sha

    mismatches = []
    observed_hashes = {}
    for name, expected in source_pins.items():
        path = Path(name)
        try:
            raw = read_regular(path)
            actual = sha(raw)
        except (OSError, ValueError) as error:
            actual = "MISSING_OR_INVALID"
            observed_hashes[name] = actual
            mismatches.append({"path": name, "expected": expected, "actual": actual, "error": str(error)})
            continue
        observed_hashes[name] = actual
        if actual != expected:
            mismatches.append({"path": name, "expected": expected, "actual": actual})
    per_map = []
    for map_id, entries in map_entries.items():
        stale = [{"path": name, "expected": expected, "actual": observed_hashes.get(name, "NOT_AUDITED")}
                 for name, expected in entries if observed_hashes.get(name) != expected]
        per_map.append({"map": map_id, "source_pin_count": len(entries),
                        "current_hash_matches": len(entries) - len(stale),
                        "mismatch_count": len(stale),
                        "missing_or_invalid_count": sum(row["actual"] == "MISSING_OR_INVALID" for row in stale),
                        "mismatch_samples": stale[:5]})
    report = {
        "status": "Passed" if not conflicts and not mismatches else "Failed",
        "bundle_path": str(bundle_path.resolve()),
        "bundle_sha256": sha(bundle_raw),
        "qualification_receipt": str(qualification_path.resolve()),
        "reachable_json_artifacts": len(seen),
        "source_pin_maps": maps,
        "source_pin_map_audits": per_map,
        "unique_source_pins": len(source_pins),
        "conflict_count": len(conflicts),
        "conflicts": conflicts,
        "mismatch_count": len(mismatches),
        "missing_source_pin_count": sum(row["actual"] == "MISSING_OR_INVALID" for row in mismatches),
        "mismatches": mismatches,
        "worker_admitted": False,
    }
    return report, source_pins


def _classify(result):
    if not isinstance(result, dict) or result.get("fixture_only") is True or isinstance(result.get("transport_camera_fixture"), dict):
        return False
    test = result.get("semantic_command_control_test") if isinstance(result, dict) else None
    if not isinstance(test, dict):
        return False
    chain = test.get("request_and_receipts")
    if not isinstance(chain, dict):
        return False
    request = chain
    reservation = request.get("actual_paid_reservation")
    spawn = request.get("spawn")
    selection = request.get("selection")
    movement = request.get("movement")
    observed = test.get("position_observations")
    return (
        test.get("stage") == "ObservedPositionChange"
        and request.get("accepted") is True
        and isinstance(request.get("receipt_id"), int)
        and isinstance(reservation, dict)
        and reservation.get("reservation_id") == request.get("receipt_id")
        and isinstance(spawn, dict)
        and spawn.get("receipt_id") == request.get("receipt_id")
        and spawn.get("spawn_succeeded") is True
        and isinstance(spawn.get("entity_index"), int)
        and isinstance(selection, dict)
        and selection.get("source_return") is True
        and selection.get("spawned_entity_selected") is True
        and selection.get("all_live_group_members_from_request_receipt") is True
        and isinstance(movement, dict)
        and movement.get("accepted") is True
        and movement.get("spawned_entity_has_actual_move_intent") is True
        and movement.get("intent_active") is True
        and isinstance(observed, list)
        and any(isinstance(row, dict) and row.get("position_changed_from_pre_move") is True for row in observed)
    )


def _production_run_admits_fixture(run):
    """Presentation-free camera fixtures are source diagnostics, never this validator's input."""
    return isinstance(run, dict) and "presentation_free_delivery_fixture" not in run


def _valid_historical_classification(result):
    historical = result.get("historical_input_classification") if isinstance(result, dict) else None
    if not isinstance(historical, dict):
        return False
    false_fields = ("current_source_equivalence", "current_blocker_equivalence", "current_surface_equivalence",
                    "physical_source_fit", "water_equivalence", "scene_fit_certified", "mission_admitted",
                    "mission_ready", "native_world_created", "native_scene_loaded", "native_input_observed",
                    "player_order_observed", "validated_victory")
    exact = {
        "physical_sha256": "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8",
        "water_sha256": "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5",
        "startup_sha256": "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03",
        "preservation_sha256": "68b2a130fe08e3f952b1a8c1c0603b082ec38c7feb8b4581e1153cba7665e297",
    }
    return (historical.get("kind") == "HistoricalInputOnlyDiagnostic_Unqualified" and
            historical.get("historical_input_epoch") == "2026-11-01" and
            historical.get("historical_producer_digest_bytes_verified") is False and
            historical.get("historical_producer_digest_only_count") == 7 and
            historical.get("preserved_source_pin_count") == 6526 and
            historical.get("preservation_project_file_count") == 6522 and
            historical.get("preservation_external_pin_count") == 4 and
            historical.get("historical_navigation_row_count") == 4954 and
            historical.get("historical_navigation_rows_materialized") == 4954 and
            historical.get("historical_navigation_rows_materialization_complete") is True and
            all(historical.get(key) is False for key in false_fields) and
            all(historical.get(key) == value for key, value in exact.items()))


def _classify_source_aria(result, descriptor, worker, code, timed_out, pins_match, preflight_passed):
    return (pins_match and preflight_passed and not timed_out and code in (0, 2)
            and isinstance(result, dict) and "semantic_command_control_test" not in result
            and result.get("s002_current_source_diagnostic_package") == descriptor
            and _valid_historical_classification(result)
            and (worker / "aria-trace.jsonl").is_file() and (worker / "aria-trace.jsonl").stat().st_size > 0
            and result.get("status") in ("SourceTerminal", "BoundedPrefix", "SourceAriaStopped"))


def validate_prepared_historical_package_initialization(run: dict, prepared: dict, repo: Path):
    sys.path.insert(0, str(repo / "Tools/CI"))
    import s002_current_source_diagnostic_package as package_reader
    binding = package_reader.load_s002_historical_placement_binding(repo)
    if run.get("s002_historical_input_diagnostic") != binding["runtime_descriptor"]:
        raise ValueError("Prepared run historical descriptor differs from package derivation inputs")
    descriptor = run.get("initialization") if isinstance(run, dict) else None
    if not isinstance(descriptor, dict) or set(descriptor) != {"path", "sha256", "bytes"}:
        raise ValueError("Prepared run must reference its package-derived initialization")
    init_path = Path(descriptor["path"])
    raw = read_regular(init_path)
    if sha(raw) != descriptor["sha256"] or len(raw) != descriptor["bytes"]:
        raise ValueError("Prepared package initialization descriptor mismatch")
    init = package_reader._json(raw)
    base_path = repo / COPIED_R10_INITIALIZATION
    base = package_reader._json(read_regular(base_path))
    package_provenance = prepared.get("current_source_package_provenance") if isinstance(prepared, dict) else None
    derivation = package_provenance.get("historical_placement_derivation") if isinstance(package_provenance, dict) else None
    package_reader.require_s002_historical_placement_derivation(init, base, binding, derivation)
    return {"path": str(init_path.resolve()), "sha256": descriptor["sha256"],
            "historical_placement_source": binding["source_initialization"]}


def _write_json(path: Path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2) + "\n")


def validate_historical_clearance_projection(path: Path, repo: Path):
    """Admit only the complete source-derived inactive S002 clearance record."""
    import project_s002_static_clearance as clearance
    path = path.resolve(strict=True)
    raw = read_regular(path)
    projection = json.loads(raw, object_pairs_hook=clearance.no_duplicates)
    capture = repo / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json"
    clearance.validate_projection(projection, capture)
    return {"path": str(path), "sha256": sha(raw)}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--bundle", required=True)
    parser.add_argument("--qualification-receipt")
    parser.add_argument("--current-source-package")
    parser.add_argument("--current-source-package-sha256")
    parser.add_argument("--historical-clearance-projection")
    parser.add_argument("--source-aria-diagnostic", action="store_true",
                        help="Run the original ARIA policy in the unqualified current-package source world")
    parser.add_argument("--build-receipt", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--max-wait-ticks", type=int, default=3600)
    parser.add_argument("--max-ticks", type=int, default=3604)
    parser.add_argument("--timeout", type=float, default=1800)
    args = parser.parse_args(argv)
    if bool(args.current_source_package) != bool(args.current_source_package_sha256):
        raise ValueError("Current-source package path and SHA256 must be supplied together")
    current_package_mode = args.current_source_package is not None
    if args.source_aria_diagnostic and not current_package_mode:
        raise ValueError("Source ARIA diagnostic requires the current-package diagnostic lane")
    if args.historical_clearance_projection and not current_package_mode:
        raise ValueError("Historical clearance override requires the current-package diagnostic lane")
    if current_package_mode == (args.qualification_receipt is not None):
        raise ValueError("Select exactly one of qualified-capture or current-package diagnostic input modes")
    repo = Path(args.repo).resolve(strict=True)
    out = Path(args.output).absolute()
    if out.exists():
        raise ValueError("Fresh isolated output directory required")
    if args.max_wait_ticks < 1 or args.max_wait_ticks > 7200 or args.max_ticks < args.max_wait_ticks + 4:
        raise ValueError("Bounded run must provide the complete semantic command observation window")
    out.mkdir(parents=True)
    runner_path = Path(__file__).resolve(strict=True)
    runner_pin = pin_runner_source(runner_path)
    bundle_path = Path(args.bundle).resolve(strict=True)
    if current_package_mode:
        audit, source_pin_closure, loaded_package = audit_current_package_inputs(
            bundle_path, Path(args.current_source_package).resolve(strict=True), args.current_source_package_sha256, repo)
        current_package_descriptor = {"path": loaded_package["path"], "sha256": loaded_package["sha256"]}
        runtime_bundle_path = bundle_path
    else:
        audit, source_pin_closure = audit_reachable_inputs(bundle_path, Path(args.qualification_receipt).resolve(strict=True), repo)
        loaded_package = None
        current_package_descriptor = None
        runtime_bundle_path = bundle_path
    prior_runner_pin = source_pin_closure.get(runner_pin["path"])
    if prior_runner_pin is not None and prior_runner_pin != runner_pin["sha256"]:
        raise ValueError("Conflicting outer runner source pin")
    source_pin_closure[runner_pin["path"]] = runner_pin["sha256"]
    if args.historical_clearance_projection:
        projector = (repo / "Tools/CI/project_s002_static_clearance.py").resolve(strict=True)
        projector_pin = sha(read_regular(projector))
        previous = source_pin_closure.get(str(projector))
        if previous is not None and previous != projector_pin:
            raise ValueError("Conflicting clearance projector source pin")
        source_pin_closure[str(projector)] = projector_pin
    audit["outer_runner_source_pin"] = dict(runner_pin)
    _write_json(out / "transitive-source-pin-audit.json", audit)
    if audit["status"] != "Passed":
        receipt = {"status": "CaptureSourcePinsStale", "worker_launched": False, "source_pin_audit": audit,
                   "scope": "No source worker started; no capture pins were repinned."}
        _write_json(out / "semantic-production-move-receipt.json", receipt)
        print(json.dumps(receipt, separators=(",", ":")))
        return 2

    build_path = Path(args.build_receipt).resolve(strict=True)
    build = json.loads(read_regular(build_path))
    build_mismatches = []
    if build.get("status") != "Passed" or build.get("mission") != "S002" or build.get("seed") != 104731:
        build_mismatches.append({"reason": "S002_104731_passed_build_required"})
    for name, expected in build.get("source_and_reference_pins", {}).items():
        try:
            actual = sha(read_regular(Path(name)))
        except (ValueError, OSError):
            actual = "MISSING_OR_INVALID"
        if actual != expected:
            build_mismatches.append({"path": name, "expected": expected, "actual": actual})
    if build_mismatches:
        receipt = {"status": "BuildPinsStale", "worker_launched": False, "build_pin_mismatches": build_mismatches,
                   "source_pin_audit": audit}
        _write_json(out / "semantic-production-move-receipt.json", receipt)
        print(json.dumps(receipt, separators=(",", ":")))
        return 2
    if current_package_mode:
        build_mismatches.extend(diagnostic_attestation_build_mismatches(
            build, build_path, audit["attestation_source_sha256"],
            audit["source_manifest_sha256"], audit["source_manifest_bytes"], repo))
    if build_mismatches:
        receipt = {"status": "BuildPinsStale", "worker_launched": False, "build_pin_mismatches": build_mismatches,
                   "source_pin_audit": audit}
        _write_json(out / "semantic-production-move-receipt.json", receipt)
        print(json.dumps(receipt, separators=(",", ":")))
        return 2

    cli = repo / "Tools/CI/run_skirmish_source_simulation.py"
    prepared = out / "prepared"
    command = [sys.executable, str(cli), "run", "--repo", str(repo), "--package-root", str(repo),
               "--mission", "S002", "--seed", "104731", "--build-receipt", str(build_path),
               "--data-bundle", str(runtime_bundle_path), "--output", str(prepared),
               "--max-ticks", str(args.max_ticks), "--timeout", str(args.timeout), "--prepare-only"]
    if current_package_mode:
        command += ["--data-repo", str(repo), "--s002-historical-input-diagnostic",
                    "--s002-current-source-diagnostic-package", current_package_descriptor["path"],
                    "--s002-current-source-diagnostic-package-sha256", current_package_descriptor["sha256"]]
    if not build.get("canonical_source_manifest"):
        receipt = {"status": "BuildReceiptMissingCanonicalManifest", "worker_launched": False, "source_pin_audit": audit}
        _write_json(out / "semantic-production-move-receipt.json", receipt)
        return 2
    preparation = subprocess.run(command, cwd=repo, text=True, capture_output=True, timeout=args.timeout)
    _write_json(out / "prepare-cli.json", {"argv": command, "exit_code": preparation.returncode,
                                               "stdout": preparation.stdout, "stderr": preparation.stderr})
    if preparation.returncode != 0:
        receipt = {"status": "CliPreparationRejected", "worker_launched": False, "source_pin_audit": audit,
                   "prepare_exit": preparation.returncode}
        _write_json(out / "semantic-production-move-receipt.json", receipt)
        return 2

    prep = Path(prepared)
    run_path = prep / "run-request.json"
    run = json.loads(read_regular(run_path))
    prepared_path = prep / "prepared-run.json"
    prepared_receipt = json.loads(read_regular(prepared_path))
    if not _production_run_admits_fixture(run):
        receipt = {"status": "PresentationFreeFixtureNotProductionValidated", "worker_launched": False,
                   "fixture_only": True, "source_pin_audit": audit,
                   "scope": "A run carrying the presentation-free delivery fixture cannot enter semantic production classification."}
        _write_json(out / "semantic-production-move-receipt.json", receipt)
        return 2
    prepared_placement_proof = None
    if current_package_mode:
        if run.get("s002_current_source_diagnostic_package") != current_package_descriptor:
            raise ValueError("Prepared source run omitted or changed exact current package descriptor")
        prepared_placement_proof = validate_prepared_historical_package_initialization(run, prepared_receipt, repo)
        if args.historical_clearance_projection:
            clearance = validate_historical_clearance_projection(Path(args.historical_clearance_projection), repo)
            run["static_clearance"] = clearance
            prepared_receipt["historical_clearance_derivation"] = {
                "projection": clearance, "source_fit_certified": False, "mission_ready": False}
    worker = out / "worker"
    worker.mkdir()
    run["output_directory"] = str(worker)
    if not args.source_aria_diagnostic:
        run["semantic_command_control_test"] = {"kind": "PaidRifleToMove", "max_wait_ticks": args.max_wait_ticks}
    _write_json(run_path, run)
    pins_path = prep / "input-pins.json"
    pins = json.loads(read_regular(pins_path))
    if current_package_mode and args.historical_clearance_projection:
        pins[clearance["path"]] = clearance["sha256"]
    pins[str(run_path.resolve())] = sha(read_regular(run_path))
    for name, digest in source_pin_closure.items():
        if name in pins and pins[name] != digest:
            raise ValueError(f"Conflicting source/runtime input pins: {name}")
        pins[name] = digest
    _write_json(pins_path, pins)
    prepared_receipt["run_request"] = {"path": str(run_path.resolve()), "sha256": sha(read_regular(run_path)), "bytes": run_path.stat().st_size}
    prepared_receipt["input_pins"] = {"path": str(pins_path.resolve()), "sha256": sha(read_regular(pins_path)), "bytes": pins_path.stat().st_size}
    if args.source_aria_diagnostic:
        prepared_receipt["source_aria_diagnostic"] = {"original_policy_requested": True, "native_input_claimed": False}
    else:
        prepared_receipt["semantic_command_control_test"] = {"kind": "PaidRifleToMove", "max_wait_ticks": args.max_wait_ticks,
                                                              "commands_are_explicit_test_issues": True}
    _write_json(prepared_path, prepared_receipt)
    # Recheck every runtime/source input after request derivation, then run the
    # compiled host's own no-world preflight before any source world allocation.
    for name, expected in pins.items():
        actual = sha(read_regular(Path(name)))
        if actual != expected:
            raise ValueError(f"Pre-worker input pin changed: {name}")
    executable = build["executable"]["path"]
    tools = build["runtime_selection"]
    mono = tools["mono"]
    config = prep / "mono-config.xml"
    engine = Path(tools["engine"])
    newtonsoft = Path(tools["newtonsoft"])
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([str(repo / "Library/ScriptAssemblies"), str(engine),
                                          str(newtonsoft.parent), str(Path(build["placement_build"]).parent)])
    preflight = subprocess.run([mono, "--config", str(config), "--optimize=-float32", executable,
                                "--source-run-preflight", str(run_path)], cwd=repo, env=env,
                               text=True, capture_output=True, timeout=args.timeout)
    _write_json(out / "source-host-preflight.json", {"exit_code": preflight.returncode,
                                                       "stdout": preflight.stdout, "stderr": preflight.stderr,
                                                       "world_allocated": False})
    source_preflight_passed = preflight.returncode == 0 and "[SourceRunPreflight]" in preflight.stdout
    if not source_preflight_passed:
        receipt = {"status": "SourceHostPreflightRejected", "worker_launched": False, "source_pin_audit": audit,
                   "preflight_exit": preflight.returncode}
        _write_json(out / "semantic-production-move-receipt.json", receipt)
        return 2

    start = time.monotonic()
    timed_out = False
    with (worker / "process.log").open("w") as log:
        process = subprocess.Popen([mono, "--config", str(config), "--optimize=-float32", executable,
                                    str(run_path)], cwd=repo, env=env, stdout=log, stderr=subprocess.STDOUT)
        try:
            code = process.wait(timeout=args.timeout)
        except subprocess.TimeoutExpired:
            timed_out = True
            process.terminate()
            try:
                code = process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                process.kill()
                code = process.wait(timeout=5)
    for name, expected in pins.items():
        if name == runner_pin["path"]:
            continue
        actual = sha(read_regular(Path(name)))
        if actual != expected:
            raise ValueError(f"Post-worker input pin changed: {name}")
    runner_pin_after = runner_source_pin_matches(runner_pin, runner_path)
    result_path = worker / "result.json"
    result = json.loads(read_regular(result_path)) if result_path.is_file() else None
    observed = runner_pin_after and source_preflight_passed and not timed_out and code == 0 and _classify(result)
    if current_package_mode:
        observed = observed and isinstance(result, dict) and result.get("s002_current_source_diagnostic_package") == current_package_descriptor and _valid_historical_classification(result)
    if args.source_aria_diagnostic:
        aria_observed = _classify_source_aria(result, current_package_descriptor, worker, code, timed_out,
                                               runner_pin_after, source_preflight_passed)
        observed = False
    receipt = {"status": "SourceSemanticProductionMoveObserved" if observed else "SourceSemanticSequenceIncomplete",
               "worker_launched": True, "worker_exit": code, "timed_out": timed_out,
               "fixture_only": bool(isinstance(result, dict) and result.get("fixture_only") is True),
               "wall_seconds": time.monotonic() - start, "semantic_sequence_observed": observed,
               "source_result": result, "source_pin_audit": audit,
               "outer_runner_source_pin": {**runner_pin, "pre_worker_verified": True,
                                            "post_worker_matches": runner_pin_after},
               "scope": "Explicit source semantic test commands; no ARIA-generated action, native UI/touch, mission outcome, or mission readiness claim."}
    if current_package_mode:
        receipt.update(diagnostic_only=True, validation_passed=False, mission_victory=None,
                       native_acceptance=False, source_fit_certified=False, mission_ready=False,
                       camera_qualified=False, active_mission_water_qualified=False,
                       runtime_presentation_qualified=False,
                       source_diagnostic_classification="S002CurrentOwnerInputsHistoricalPhysicalDiagnostic_Unqualified",
                       current_source_diagnostic_package=current_package_descriptor,
                       prepared_historical_placement_derivation=prepared_placement_proof,
                       current_source_package_bound_by_source_preflight=(source_preflight_passed and isinstance(result, dict) and result.get("s002_current_source_diagnostic_package") == current_package_descriptor))
    if args.source_aria_diagnostic:
        receipt.update(status=("SourceAriaDiagnosticObserved" if aria_observed else "SourceAriaDiagnosticIncomplete"),
                       source_aria_diagnostic_observed=aria_observed,
                       scope="Original source ARIA policy on historical physical inputs; no native input, source fit, validated victory, or mission readiness claim.")
    _write_json(out / "semantic-production-move-receipt.json", receipt)
    print(json.dumps({"status": receipt["status"], "worker_exit": code, "timed_out": timed_out,
                      "semantic_sequence_observed": observed, "receipt": str(out / "semantic-production-move-receipt.json")}))
    return 0 if (aria_observed if args.source_aria_diagnostic else observed) else 1


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, KeyError, json.JSONDecodeError, subprocess.TimeoutExpired) as error:
        print(f"S002 semantic production/move admission failed before semantic classification: {error}", file=sys.stderr)
        raise SystemExit(2)

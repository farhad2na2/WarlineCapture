"""Strict input and output checks for S002 imported provider metadata readback."""
from __future__ import annotations

import hashlib
import json
from collections import Counter
from pathlib import Path
import re
from typing import Any, Optional

EXPECTED_KEYS = {
    "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Chr_Soldier_Male_01",
    "Unit_Chr_Ghillie_Male_01", "Unit_Veh_Light_Armored_Car",
    "Unit_Veh_APC_Slow", "Unit_Veh_Tank_USA", "Unit_Veh_Truck_Tanker",
    "Unit_Veh_Truck_Tray",
}
EXPECTED_T5_RECEIPT_SHA256 = "a6090ea7e79bfbf23dccf2b5a14559fdd8a1b6cfe12e013bd484f887480bcd01"
EXPECTED_T5_PROJECTION_SHA256 = "58098fab78cdadb9c11220b16c58866fad4d58bcb42abb555a782bf6d61e4b1f"
OWNER_SOURCES = (
    "Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs",
    "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
    "Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionCatalog.cs",
)
OBSERVER = "Assets/Game/Scripts/Editor/Skirmish/S002CurrentForceUnitMetadataObserver.cs"
OWNER_METADATA = {
    "production_duration_seconds", "production_transport_prefab",
    "is_air_unit", "production_transport_arrival_seconds",
    "production_transport_hold_for_next_ready_seconds",
    "production_transport_max_concurrent",
    "production_transport_requires_airport_runway",
    "production_transport_uses_runway_landing", "footprint_cells",
}


def sha_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(condition: bool, reason: str) -> None:
    if not condition:
        raise ValueError(reason)


def safe_repo_path(root: Path, raw: str) -> Path:
    p = Path(raw)
    if p.is_absolute():
        p = p.relative_to(root)
    result = (root / p).resolve()
    result.relative_to(root.resolve())
    return result


def load_t5(root: Path, receipt_path: Path, projection_path: Path) -> tuple[dict, dict]:
    receipt = json.loads(receipt_path.read_text())
    projection = json.loads(projection_path.read_text())
    need(sha_file(receipt_path) == EXPECTED_T5_RECEIPT_SHA256, "T5 receipt digest is not the reviewed input")
    need(sha_file(projection_path) == EXPECTED_T5_PROJECTION_SHA256,
         "T5 provider projection digest is not the reviewed input")
    need(receipt.get("kind") == "S002CurrentSetupProviderProjectionReceipt", "T5 receipt kind mismatch")
    need(receipt.get("status") == "JoinedWithUnqualifiedPrefabLocalIds", "T5 receipt status mismatch")
    need(receipt.get("setup", {}).get("catalog_id") == "S002" and receipt["setup"].get("seed") == 104731,
         "T5 receipt is not the pinned S002 setup")
    need(receipt.get("force_count") == 16 and receipt.get("structure_count") == 18,
         "T5 force/structure row counts changed")
    need(sha_file(projection_path) == receipt["projection"]["sha256"], "T5 projection hash differs from receipt")
    need(projection.get("kind") == "S002CurrentSetupProviderIdentityProjection", "T5 projection kind mismatch")
    need(projection.get("status") == "JoinedWithUnqualifiedPrefabLocalIds", "T5 projection status mismatch")
    need(len(projection.get("force_rows", [])) == 16, "T5 exact 16 force rows required")
    need(projection.get("world_allocated") is False and projection.get("metadata_factory_execution") is False,
         "T5 must remain a source-only provider projection")
    return receipt, projection


def _config_candidates(root: Path, prefab: dict) -> tuple[list[dict], list[dict]]:
    config = prefab.get("config")
    need(isinstance(config, dict), "provider config mapping missing")
    raw_paths = config.get("config_paths")
    bindings = config.get("owner_bindings")
    need(isinstance(raw_paths, list) and raw_paths and len(set(raw_paths)) == len(raw_paths),
         "unique T5 candidate config paths required")
    need(isinstance(bindings, list) and bindings, "T5 config owner bindings required")
    candidates = []
    for rel in raw_paths:
        need(isinstance(rel, str) and rel, "invalid T5 config path")
        asset = safe_repo_path(root, rel)
        meta = Path(str(asset) + ".meta")
        need(asset.is_file() and meta.is_file(), "T5 candidate config asset/meta missing")
        match = re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", meta.read_text())
        need(match is not None, "candidate config meta GUID missing")
        matching_bindings = [b for b in bindings if b.get("config_path") == rel]
        need(matching_bindings, "T5 candidate has no source owner binding")
        guids = {b.get("config_guid") for b in matching_bindings}
        ids = {str(b.get("config_file_id")) for b in matching_bindings}
        need(guids == {match.group(1)} and len(ids) == 1 and "None" not in ids,
             "T5 owner binding/config asset identity disagreement")
        for binding in matching_bindings:
            need(binding.get("config_sha256") == sha_file(asset), "T5 config source hash mismatch")
            need(binding.get("config_meta_sha256") == sha_file(meta), "T5 config meta hash mismatch")
        candidates.append({"path": rel, "guid": match.group(1), "local_id": next(iter(ids))})

    normalized_bindings = []
    for b in bindings:
        owner_path = b.get("owner_prefab_path")
        config_path = b.get("config_path")
        need(isinstance(owner_path, str) and isinstance(config_path, str), "T5 owner binding fields missing")
        owner = safe_repo_path(root, owner_path)
        owner_meta = Path(str(owner) + ".meta")
        need(owner.is_file() and owner_meta.is_file(), "T5 owner prefab/meta missing")
        owner_guid = re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", owner_meta.read_text())
        need(owner_guid is not None and owner_guid.group(1) == b.get("owner_component_guid"),
             "T5 owner prefab GUID differs from component binding GUID")
        normalized_bindings.append({
            "owner_path": owner_path,
            "owner_guid": owner_guid.group(1),
            "owner_local_id": str(b.get("owner_component_file_id")),
            "config_path": config_path,
            "config_guid": b.get("config_guid"),
            "config_local_id": str(b.get("config_file_id")),
        })
    need(len({(b["owner_path"], b["owner_local_id"], b["config_path"]) for b in normalized_bindings}) == len(normalized_bindings),
         "duplicate T5 owner binding")
    return candidates, normalized_bindings


def build_provider_specs(root: Path, projection: dict) -> list[dict]:
    grouped: dict[str, dict] = {}
    for row in projection["force_rows"]:
        prefab = row.get("provider_prefab")
        need(isinstance(prefab, dict), "T5 force row lacks provider prefab")
        key = prefab.get("key")
        need(isinstance(key, str) and key, "T5 provider key missing")
        if key in grouped:
            need(grouped[key] == prefab, "duplicate force rows disagree on provider source record")
        else:
            grouped[key] = prefab
    need(set(grouped) == EXPECTED_KEYS, "exact eight S002 provider keys required")
    specs = []
    for key in sorted(grouped):
        prefab = grouped[key]
        path, guid, file_id = prefab.get("path"), prefab.get("guid"), prefab.get("file_id")
        need(isinstance(path, str) and isinstance(guid, str) and isinstance(file_id, int),
             "T5 provider asset identity incomplete")
        asset = safe_repo_path(root, path)
        meta = Path(str(asset) + ".meta")
        need(asset.is_file() and meta.is_file(), "T5 provider prefab/meta missing")
        actual_guid = re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", meta.read_text())
        need(actual_guid is not None and actual_guid.group(1) == guid, "T5 provider prefab GUID mismatch")
        candidates, bindings = _config_candidates(root, prefab)
        specs.append({"key": key, "path": path, "guid": guid, "file_id": file_id,
                      "config_candidates": candidates, "owner_bindings": bindings})
    return specs


def validate_observation(observation: dict, specs: list[dict], expected_pins: Optional[dict[str, str]] = None) -> None:
    need(observation.get("kind") == "S002CurrentForceUnitMetadataObservation", "observer kind mismatch")
    need(observation.get("status") == "Passed", "imported provider metadata observer did not pass")
    need(observation.get("scope") == "ImportedProviderMetadataOnly_NoSceneLoad_NoWorldCreation_NoBake_NoProviderAdmission_NoReadiness",
         "observer scope mismatch")
    for key in ("observer_loaded_or_modified_scene", "observer_created_world", "observer_invoked_baker",
                "provider_admission_performed", "native_capture_used", "source_fit_verified", "mission_ready"):
        need(observation.get(key) is False, f"observer scope flag {key} must be false")
    before, after = observation.get("source_pins_before"), observation.get("source_pins_after")
    need(isinstance(before, dict) and before == after, "observer pre/post source pin maps differ or are missing")
    if expected_pins is not None:
        need(before == expected_pins, "observer source pin map differs from the executed manifest")
    expected = {spec["key"]: spec for spec in specs}
    rows = observation.get("providers")
    need(isinstance(rows, list) and len(rows) == 8, "exact eight provider observations required")
    by_key = {}
    for row in rows:
        key = row.get("key")
        need(key in expected and key not in by_key, "unknown or duplicate provider observation")
        by_key[key] = row
    need(set(by_key) == set(expected), "missing provider observation")
    for key, spec in expected.items():
        row = by_key[key]
        need(row.get("status") == "Passed", f"provider {key} failed")
        need(row.get("asset_path") == spec["path"], f"provider {key} path mismatch")
        ident = row.get("actual_asset_identity") or {}
        need(ident.get("guid") == spec["guid"] and ident.get("local_id") == str(spec["file_id"]),
             f"provider {key} imported root identity mismatch")
        config = row.get("effective_config_identity") or {}
        matches = [c for c in spec["config_candidates"] if c["path"] == config.get("path") and
                   c["guid"] == config.get("guid") and c["local_id"] == config.get("local_id")]
        need(len(matches) == 1 and row.get("effective_config_candidate_match") is True,
             f"provider {key} effective config is not one unique T5 candidate")
        override_issues = row.get("config_override_issues")
        need(override_issues == [], f"provider {key} imported config override issues present")
        actual_override_bindings = []
        for override in row.get("config_override_rows", []):
            need(override.get("binding_match") is True, f"provider {key} has unpinned config override")
            target = override.get("target_identity") or {}
            override_config = override.get("config_identity") or {}
            override_candidates = [candidate for candidate in spec["config_candidates"]
                                   if candidate["path"] == override_config.get("path") and
                                   candidate["guid"] == override_config.get("guid") and
                                   candidate["local_id"] == override_config.get("local_id")]
            need(len(override_candidates) == 1,
                 f"provider {key} override config is not one unique T5-pinned candidate")
            actual_override_bindings.append((target.get("path"), target.get("guid"), target.get("local_id"),
                                             override_config.get("path"), override_config.get("guid"),
                                             override_config.get("local_id")))
        expected_override_bindings = [(binding["owner_path"], binding["owner_guid"], binding["owner_local_id"],
                                       binding["config_path"], binding["config_guid"], binding["config_local_id"])
                                      for binding in spec["owner_bindings"]]
        need(Counter(actual_override_bindings) == Counter(expected_override_bindings),
             f"provider {key} config override rows do not cover each T5 binding exactly once")
        metadata = row.get("production_metadata")
        need(isinstance(metadata, dict) and OWNER_METADATA.issubset(metadata),
             f"provider {key} original metadata owner fields missing")
        need(row.get("original_production_metadata_owner") ==
             "Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper.TryGetMetadata",
             f"provider {key} did not report the original metadata owner")
        need(metadata.get("transport_reference_matches_config") is True,
             f"provider {key} transport reference differs from effective config")
        need(metadata.get("production_transport_prefab_is_null") is
             (metadata.get("production_transport_prefab") is None),
             f"provider {key} transport null flag disagrees with returned reference")
        footprint = metadata.get("footprint_cells")
        need(isinstance(footprint, dict) and {"x", "y"}.issubset(footprint),
             f"provider {key} original footprint values missing")
        need(isinstance(metadata.get("is_air_unit"), bool) and
             isinstance(metadata.get("production_transport_max_concurrent"), int) and
             metadata["production_transport_max_concurrent"] >= 1,
             f"provider {key} original air/concurrency metadata malformed")


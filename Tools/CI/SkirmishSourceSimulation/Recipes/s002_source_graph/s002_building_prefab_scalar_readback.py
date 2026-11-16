"""Pinned T1 source inputs and result checks for imported S002 building prefabs."""
from __future__ import annotations

from collections import Counter
import hashlib
import json
from pathlib import Path

T1_DIR = Path("T Logs/SkirmishProduction/s002-scalar-provider-projection-20261103-T1")
T1_RECEIPT = T1_DIR / "receipt.json"
T1_PROJECTION = T1_DIR / "provider-projection.json"
T1_INPUTS = T1_DIR / "building-metadata-scalar-inputs.json"
T1_RECEIPT_SHA256 = "a18c7d243df67e4e38eb6d88dd6288bff93843d9c4619562f0bddea2f50ef541"
T1_PROJECTION_SHA256 = "ed48e7026b727d106b47e66735c29af4b90f1e9c1772faf6e23ed9099769b41e"
T1_INPUTS_SHA256 = "c9735ec0bcaf312657e71d7f20f47ac2ad308f31200d93525c97852470dfa284"
EXPECTED_KEYS = {
    "Building_Ammunition_Depot", "Building_Barrack", "Building_Fuel_Bladder",
    "Building_GroundStaging", "Building_GuardTower", "Building_OilPump",
    "Building_Refinery", "Building_Satelite_Dish",
}
OWNER_PATHS = (
    "Assets/Game/Scripts/Components/BuildingDefinitionAuthoringScalarProjection.cs",
    "Assets/Game/Scripts/Authorings/BuildingDefinitionAuthoring.cs",
    "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
    "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
)
ASSEMBLIES = (
    "Library/ScriptAssemblies/Game.Components.dll", "Library/ScriptAssemblies/Game.Authoring.dll",
    "Library/ScriptAssemblies/Game.Configs.dll", "Library/ScriptAssemblies/Game.Composition.dll",
    "Library/ScriptAssemblies/Game.Runtime.dll", "Library/ScriptAssemblies/Game.Editor.dll",
)
SCALAR_FIELDS = ("max_health", "footprint_width", "footprint_depth", "price",
                 "materials_cost", "production_duration_seconds")
PASS_MARKER = "[S002BuildingPrefabScalarReadback] result=Passed rows=8 scope=ImportedPrefabScalarMetadata"


def sha_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(ok: bool, why: str) -> None:
    if not ok:
        raise ValueError(why)


def safe_path(root: Path, raw: str) -> Path:
    path = Path(raw)
    if path.is_absolute():
        path = path.relative_to(root)
    result = (root / path).resolve()
    result.relative_to(root.resolve())
    return result


def _read_json(path: Path) -> dict:
    return json.loads(path.read_text())


def load_t1(root: Path) -> tuple[dict, dict, dict]:
    receipt_path = root / T1_RECEIPT
    projection_path = root / T1_PROJECTION
    inputs_path = root / T1_INPUTS
    need(sha_file(receipt_path) == T1_RECEIPT_SHA256, "T1 receipt digest differs from reviewed input")
    need(sha_file(projection_path) == T1_PROJECTION_SHA256, "T1 provider projection digest differs from reviewed input")
    need(sha_file(inputs_path) == T1_INPUTS_SHA256, "T1 scalar input digest differs from reviewed input")
    receipt, projection, inputs = map(_read_json, (receipt_path, projection_path, inputs_path))
    need(receipt.get("kind") == "S002CurrentSetupProviderProjectionReceipt" and
         receipt.get("status") == "JoinedWithUnqualifiedPrefabLocalIds", "exact T1 provider projection receipt required")
    setup = receipt.get("setup", {})
    need(setup.get("catalog_id") == "S002" and setup.get("seed") == 104731 and
         setup.get("setup_hash") == 3657809016 and setup.get("size_id") == "Standard" and
         setup.get("difficulty_id") == "Regular", "T1 receipt must identify the exact S002 Standard/Regular setup")
    need(receipt.get("projection", {}).get("sha256") == T1_PROJECTION_SHA256 and
         receipt.get("scalar_source_inputs", {}).get("sha256") == T1_INPUTS_SHA256,
         "T1 receipt must bind the exact projection and scalar input files")
    need(projection.get("kind") == "S002CurrentSetupProviderIdentityProjection" and
         projection.get("status") == "JoinedWithUnqualifiedPrefabLocalIds", "T1 projection scope/status mismatch")
    need(inputs.get("kind") == "S002BuildingDefinitionScalarMetadataInputs" and
         inputs.get("source_scope") == "Current source YAML candidate fields only; prefab importer-effective scalar state is unverified.",
         "T1 scalar input scope mismatch")
    policy = projection.get("building_definition_scalar_metadata", {})
    need(policy.get("kind") == "S002BuildingDefinitionScalarMetadataSourceProjection" and
         policy.get("status") == "PassedSourceOnlyScalarPolicy" and policy.get("policy_controls_passed") is True and
         policy.get("unity_effective_prefab_state_verified") is False, "T1 source-only scalar policy result required")
    need(projection.get("world_allocated") is False and projection.get("metadata_factory_execution") is False and
         projection.get("source_fit_verified") is False and projection.get("mission_ready") is False,
         "T1 must retain source-only scope flags")
    need(len(inputs.get("rows", [])) == 8 and len(policy.get("rows", [])) == 8 and
         len(projection.get("structure_rows", [])) == 18, "T1 requires eight candidates, 18 compiled structure rows")
    need({row.get("key") for row in inputs["rows"]} == EXPECTED_KEYS and
         {row.get("key") for row in policy["rows"]} == EXPECTED_KEYS, "exact eight T1 candidate keys required")
    return receipt, projection, inputs


def provider_facts(projection: dict) -> dict[str, dict]:
    result: dict[str, dict] = {}
    for row in projection.get("structure_rows", []):
        key = row.get("visual_key")
        need(isinstance(key, str) and key, "T1 structure visual key required")
        if isinstance(row.get("provider_prefab"), dict):
            provider = row["provider_prefab"]
        else:
            special = row.get("special_source_binding")
            need(isinstance(special, dict) and special.get("key") == key,
                 "T1 special provider binding must be explicit")
            provider = {
                "key": key, "path": special.get("prefab_path"), "guid": special.get("prefab_guid"),
                "file_id": special.get("root_local_id"),
                "local_id_validation": special.get("local_id_validation"),
            }
        if key in result:
            prior = result[key]
            need((prior.get("key"), prior.get("path"), prior.get("guid"), prior.get("file_id"),
                  prior.get("local_id_validation"), prior.get("config")) ==
                 (provider.get("key"), provider.get("path"), provider.get("guid"), provider.get("file_id"),
                  provider.get("local_id_validation"), provider.get("config")),
                 "duplicate compiled S002 rows disagree on provider identity")
        else:
            result[key] = provider
    need(set(result) == EXPECTED_KEYS, "exact eight T1 compiled provider identities required")
    return result


def source_pins(root: Path, receipt: dict, projection: dict, inputs: dict,
                observer: str, observer_meta: str, runner: str, helper: str, test: str,
                wrapper: str) -> dict[str, str]:
    pins = projection.get("source_pins")
    need(isinstance(pins, dict) and len(pins) >= 380, "complete T1 390-source pin set required")
    result: dict[str, str] = {}
    for raw, expected in pins.items():
        path = safe_path(root, raw)
        need(path.is_file() and sha_file(path) == expected, "current source differs from T1 projection pin: " + raw)
        result[path.relative_to(root.resolve()).as_posix()] = expected
    extra = {
        T1_RECEIPT.as_posix(): T1_RECEIPT_SHA256,
        T1_PROJECTION.as_posix(): T1_PROJECTION_SHA256,
        T1_INPUTS.as_posix(): T1_INPUTS_SHA256,
        observer: sha_file(root / observer), observer_meta: sha_file(root / observer_meta),
        runner: sha_file(root / runner), helper: sha_file(root / helper),
        test: sha_file(root / test), wrapper: sha_file(root / wrapper),
    }
    # Pin referenced source build, graph, harness, and runner receipts as exact T1 context.
    for entry in (receipt.get("source_build", {}), receipt.get("source_graph", {}),
                  receipt.get("harness", {}), receipt.get("runner", {})):
        raw = entry.get("path")
        expected = entry.get("sha256")
        need(isinstance(raw, str) and isinstance(expected, str), "T1 upstream artifact reference missing")
        path = safe_path(root, raw)
        extra[path.relative_to(root.resolve()).as_posix()] = expected
    for rel in OWNER_PATHS:
        extra[rel] = sha_file(root / rel)
        extra[rel + ".meta"] = sha_file(root / (rel + ".meta"))
    for rel in (
        "Assets/Game/Scripts/Components/Game.Components.asmdef",
        "Assets/Game/Scripts/Authorings/Game.Authoring.asmdef",
        "Assets/Game/Scripts/Configs/Game.Configs.asmdef",
        "Assets/Game/Scripts/Composition/Game.Composition.asmdef",
        "Assets/Game/Scripts/Game.Runtime.asmdef",
        "Assets/Game/Scripts/Editor/Game.Editor.asmdef",
    ):
        extra[rel] = sha_file(root / rel)
        extra[rel + ".meta"] = sha_file(root / (rel + ".meta"))
    for rel, expected in extra.items():
        path = safe_path(root, rel)
        need(path.is_file() and sha_file(path) == expected, "T1 or observer pin mismatch: " + rel)
        prior = result.get(rel)
        need(prior is None or prior == expected, "conflicting source pin: " + rel)
        result[rel] = expected
    return dict(sorted(result.items()))


def build_candidates(root: Path, projection: dict, inputs: dict) -> list[dict]:
    providers = provider_facts(projection)
    input_by_key = {row["key"]: row for row in inputs["rows"]}
    policy_by_key = {row["key"]: row for row in projection["building_definition_scalar_metadata"]["rows"]}
    need(set(input_by_key) == EXPECTED_KEYS and set(policy_by_key) == EXPECTED_KEYS,
         "T1 input/policy key join must be one-to-one")
    result = []
    for key in sorted(EXPECTED_KEYS):
        row, policy, provider = input_by_key[key], policy_by_key[key], providers[key]
        need(row.get("source_prefab_path") == provider.get("path"), "T1 scalar candidate path differs from compiled provider")
        need(provider.get("key") == key and isinstance(provider.get("guid"), str), "T1 provider identity incomplete")
        special = key == "Building_GroundStaging"
        if special:
            binding = next(r["special_source_binding"] for r in projection["structure_rows"]
                           if r.get("visual_key") == key)
            config_path = binding.get("authoring_config_path")
            need(binding.get("prefab_path") == row.get("source_prefab_path") and
                 binding.get("prefab_guid") == provider.get("guid") and
                 binding.get("authoring_config_path") == row.get("source_config_path"),
                 "T1 special GroundStaging source binding identity mismatch")
            expected_config_guid = binding.get("authoring_config_guid")
        else:
            provider_row = next(r["provider_prefab"] for r in projection["structure_rows"]
                                if r.get("visual_key") == key and isinstance(r.get("provider_prefab"), dict))
            config = provider_row.get("config", {})
            bindings = config.get("owner_bindings", [])
            need(len(bindings) == 1, "T1 building provider must have one source owner binding")
            binding = bindings[0]
            config_path = config.get("config_path")
            need(config.get("config_paths") == [config_path] and
                 binding.get("config_path") == config_path and
                 binding.get("config_guid") == binding.get("config_asset_guid"),
                 "T1 building config binding must identify exactly one config path/GUID")
            expected_config_guid = binding.get("config_guid")
            need(binding.get("owner_prefab_path") == row.get("source_owner_prefab_path") and
                 binding.get("owner_component_file_id") == row.get("prefab_component_file_id"),
                 "T1 scalar source authoring component differs from compiled owner binding")
            owner_meta = Path(str(safe_path(root, row["source_owner_prefab_path"])) + ".meta")
            owner_guid = __import__("re").search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", owner_meta.read_text())
            need(owner_guid is not None and owner_guid.group(1) == binding.get("owner_component_guid"),
                 "T1 source authoring component GUID differs from its owner prefab GUID")
        need(config_path == row.get("source_config_path"), "T1 scalar config path differs from compiled provider binding")
        config_meta = Path(str(safe_path(root, config_path)) + ".meta")
        guid_match = __import__("re").search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", config_meta.read_text())
        need(guid_match is not None and guid_match.group(1) == expected_config_guid,
             "T1 config source GUID differs from its exact asset metadata")
        prefab_meta = Path(str(safe_path(root, provider["path"])) + ".meta")
        prefab_guid = __import__("re").search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", prefab_meta.read_text())
        need(prefab_guid is not None and prefab_guid.group(1) == provider.get("guid"),
             "T1 provider GUID differs from current prefab metadata")
        result.append({"key": key, "input": row, "expected": policy, "provider": provider,
                       "special_source_binding": special, "config_guid": guid_match.group(1),
                       "source_component_guid": (provider["guid"] if special else binding["owner_component_guid"])})
    return result


def make_manifest(root: Path, receipt: dict, projection: dict, inputs: dict,
                  observer: str, observer_meta: str, runner: str, helper: str, test: str,
                  wrapper: str) -> dict:
    candidates = build_candidates(root, projection, inputs)
    pins = source_pins(root, receipt, projection, inputs, observer, observer_meta, runner, helper, test, wrapper)
    return {
        "kind": "S002BuildingPrefabScalarReadbackPinManifest",
        "scope": "ImportedPrefabScalarMetadata",
        "t1_receipt_path": T1_RECEIPT.as_posix(), "t1_receipt_sha256": T1_RECEIPT_SHA256,
        "t1_projection_path": T1_PROJECTION.as_posix(), "t1_projection_sha256": T1_PROJECTION_SHA256,
        "t1_inputs_path": T1_INPUTS.as_posix(), "t1_inputs_sha256": T1_INPUTS_SHA256,
        "source_pins": pins,
        "candidate_keys": [candidate["key"] for candidate in candidates],
        "candidate_count": len(candidates),
        "provider_local_ids_are_unqualified": True,
        "claims": {"preview_prefab_contents_scene_used": True, "authored_or_mission_scene_loaded": False,
                   "world_created": False, "baker_invoked": False, "prefab_saved": False,
                   "source_fit_verified": False, "mission_ready": False},
    }


def validate_observation(observation: dict, candidates: list[dict], expected_pins: dict[str, str]) -> None:
    need(observation.get("kind") == "S002BuildingPrefabScalarMetadataObservation", "observation kind mismatch")
    need(observation.get("status") == "Passed", "imported scalar metadata observation failed")
    need(observation.get("t1_receipt_sha256") == T1_RECEIPT_SHA256 and
         observation.get("t1_projection_sha256") == T1_PROJECTION_SHA256 and
         observation.get("t1_inputs_sha256") == T1_INPUTS_SHA256,
         "observation does not bind the exact reviewed T1 inputs")
    need(observation.get("scope") ==
         "ImportedPrefabContentsPreviewScenes_NoAuthoredOrMissionScene_NoWorld_NoBaker_NoPrefabSave_NoFit_NoReadiness",
         "readback scope mismatch")
    need(observation.get("temporary_prefab_contents_preview_scene_used") is True and
         observation.get("authored_or_mission_scene_loaded") is False and observation.get("world_created") is False and
         observation.get("baker_invoked") is False and observation.get("prefab_asset_saved") is False and
         observation.get("source_fit_verified") is False and observation.get("mission_ready") is False,
         "readback scope flags invalid")
    need(observation.get("source_pins_before") == expected_pins and
         observation.get("source_pins_after") == expected_pins, "observer source pins differ from manifest")
    rows = observation.get("candidates")
    need(isinstance(rows, list) and len(rows) == 8, "exact eight imported candidates required")
    expected = {candidate["key"]: candidate for candidate in candidates}
    actual = {}
    for row in rows:
        key = row.get("key")
        need(key in expected and key not in actual, "unknown or duplicated imported building key")
        actual[key] = row
    need(set(actual) == EXPECTED_KEYS, "all exact T1 candidates required")
    for key, candidate in expected.items():
        row = actual[key]
        need(row.get("status") == "Passed", f"{key} imported metadata parity failed")
        need(row.get("asset_path") == candidate["input"]["source_prefab_path"], f"{key} prefab path mismatch")
        root_identity = row.get("actual_prefab_main_root_identity") or {}
        need(root_identity.get("path") == candidate["input"]["source_prefab_path"] and
             root_identity.get("guid") == candidate["provider"]["guid"] and
             root_identity.get("local_id") and
             row.get("actual_prefab_main_root_local_id_is_observed_not_inferred") == root_identity.get("local_id"),
             f"{key} imported main root identity missing or inferred")
        need(row.get("t1_provider_local_id") == candidate["provider"].get("file_id") and
             row.get("t1_provider_local_id_semantics") == candidate["provider"].get("local_id_validation"),
             f"{key} T1 unresolved provider fileID semantics changed")
        source_component = row.get("resolved_original_source_component_identity") or {}
        need(source_component.get("path") == candidate["input"]["source_owner_prefab_path"] and
             source_component.get("guid") == candidate["source_component_guid"] and
             source_component.get("local_id") == str(candidate["input"]["prefab_component_file_id"]) and
             source_component.get("identity_resolved") is True,
             f"{key} source authoring component identity differs from T1 owner component")
        config = row.get("effective_config_identity") or {}
        need(config.get("path") == candidate["input"]["source_config_path"] and
             config.get("guid") == candidate["config_guid"] and config.get("local_id") and
             config.get("identity_resolved") is True,
             f"{key} effective config identity differs from T1 source config")
        need(row.get("original_metadata_owner") ==
             "Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata",
             f"{key} original owner not reported")
        need(row.get("prefab_contents_unloaded_without_save") is True and
             row.get("imported_asset_dirty_before") is False and row.get("imported_asset_dirty_after_owner") is False,
             f"{key} imported asset state changed")
        expected_values = candidate["expected"]
        actual_values = row.get("original_metadata_owner_scalar_values") or {}
        for field in SCALAR_FIELDS:
            need(actual_values.get(field) == expected_values.get(field), f"{key} scalar field {field} differs")

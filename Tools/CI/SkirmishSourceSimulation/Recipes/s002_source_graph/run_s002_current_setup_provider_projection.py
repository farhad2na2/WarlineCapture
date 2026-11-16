#!/usr/bin/env python3
"""Compile current S002 setup and join it to serialized current source providers.

This is an identity/config-path projection only. It does not execute metadata
factories, initialize a World, or establish map fit/readiness.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import math
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
GRAPH_GENERATOR = HERE / "generate_s002_authored_config_graph.py"
BINDING = HERE / "S002CurrentSetupProviderSourceBinding.cs.txt"
GENERIC = HERE.parent / "s004_source_graph" / "generate_s004_typed_config_graph.py"
BUILDING_CONFIG = "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset"
UNIT_REGISTRY = "Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset"
BUILDING_OWNER = "Assets/Game/Scripts/Authorings/BuildingDefinitionAuthoring.cs"
UNIT_OWNER = "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs"
BUILDING_PROVIDER_SCRIPT = "Assets/Game/Scripts/Configs/Scene/BuildingPlacementSystemSceneConfigAsset.cs"
UNIT_PROVIDER_SCRIPT = "Assets/Game/Scripts/Configs/UnitPrefabRegistryAuthoringConfig.cs"
BUILDING_CONFIG_SCRIPT = "Assets/Game/Scripts/Configs/Prefabs/BuildingDefinitionAuthoringPrefabConfigAsset.cs"
UNIT_CONFIG_SCRIPT = "Assets/Game/Scripts/Configs/Prefabs/UnitGridAuthoringPrefabConfigAsset.cs"
GROUND_STAGING_KEY = "Building_GroundStaging"
GROUND_STAGING_PREFAB = "Assets/Game/Prefabs/Skirmish/Building_GroundStaging.prefab"
GROUND_STAGING_ACCESS = "Assets/Game/Scripts/Configs/Skirmish/SkirmishGroundStagingPrefabAccess.cs"
GROUND_STAGING_VISUAL_OWNER = "Assets/Game/Scripts/Runtime/Skirmish/SkirmishVisualSpawnService.cs"
GROUND_STAGING_ID_OWNER = "Assets/Game/Scripts/Skirmish/Contracts/SkirmishRosterContracts.cs"
GROUND_STAGING_IDENTITY_OWNER = "Assets/Game/Scripts/Configs/Skirmish/SkirmishGroundStagingIdentity.cs"
BUILDING_AUTHORING_OWNER = "Assets/Game/Scripts/Authorings/BuildingDefinitionAuthoring.cs"
BUILDING_CONFIG_OWNER = "Assets/Game/Scripts/Configs/Prefabs/BuildingDefinitionAuthoringPrefabConfigAsset.cs"
SCALAR_POLICY = "Assets/Game/Scripts/Components/BuildingDefinitionAuthoringScalarProjection.cs"
UNIT_SCALAR_POLICY = "Assets/Game/Scripts/Components/UnitProductionMetadataScalarPolicy.cs"
IMPORTER_READBACK_KIND = "S002PrefabImporterIdentityReadbackReceipt"
IMPORTER_READBACK_SCOPE = "ImporterObjectIdentityOnly_NoSceneLoad_NoWorldCreation_NoBake_NoProviderJoin_NoReadiness"
IMPORTER_MARKER = "[S002PrefabImporterIdentityObserver] result=Passed scope=ImporterObjectIdentityOnly"
IMPORTER_TARGET_GUID = "9ce6998c034284d98a89b5badc6e2f24"
IMPORTER_TARGET_FILE_ID = 391336776749024018
IMPORTER_CONFIG_GUID = "b0ffc9714da994462a1e48679b4863de"
IMPORTER_CONFIG_FILE_ID = 11400000
IMPORTER_TARGET_TYPE = "Game.Authoring.UnitGridAuthoring"
IMPORTER_PREFAB = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Light_Armored_Car.prefab"
IMPORTER_TARGET_PATH = "Assets/Game/Prefabs/Vehicles/Unit_Veh.prefab"
IMPORTER_CONFIG_PATH = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Light_Armored_Car_Config.asset"
IMPORTER_BASE_TARGET_GUID = "5680e7910ed3741beba6f07116b6b03c"
IMPORTER_BASE_TARGET_FILE_ID = 106347591433935587
IMPORTER_BASE_TARGET_PATH = "Assets/Game/Prefabs/Characters/Unit.prefab"
MAX_PREFAB = 16 * 1024 * 1024


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def need(ok: bool, why: str) -> None:
    if not ok:
        raise ValueError(why)


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


def config_guid(meta: Path) -> str:
    raw = meta.read_bytes()
    matches = re.findall(rb"^guid: ([0-9a-f]{32})\r?$", raw, re.M)
    need(len(matches) == 1, "exactly one lowercase config GUID required: " + str(meta))
    return matches[0].decode()


def script_guid(source: Path) -> str:
    return config_guid(Path(str(source) + ".meta"))


def load_provider_asset(repo: Path, helper, rel: str, field: str, expected_script: Path):
    path = repo / rel
    raw = path.read_bytes()
    need(0 < len(raw) <= helper.MAX_FILE and not path.is_symlink(), "bounded regular provider config required: " + rel)
    meta = Path(str(path) + ".meta")
    meta_guid = config_guid(meta)
    parsed = helper.strict_read(repo / helper.READER_REL, [path])[str(path)]
    mb = parsed.get("MonoBehaviour")
    need(isinstance(mb, dict), "provider config MonoBehaviour required: " + rel)
    script = mb.get("m_Script")
    need(isinstance(script, dict) and script.get("fileID") == 11500000 and
         script.get("guid") == script_guid(expected_script) and script.get("type") == 3,
         "provider asset must use its exact current original source type: " + rel)
    rows = mb.get(field)
    need(isinstance(rows, list) and rows, "ordered provider reference list missing: " + rel + ":" + field)
    result = []
    for i, row in enumerate(rows):
        need(isinstance(row, dict) and set(row) >= {"fileID", "guid", "type"} and
             isinstance(row["guid"], str) and re.fullmatch(r"[0-9a-f]{32}", row["guid"]),
             f"exact provider object reference required: {rel}:{field}[{i}]")
        need(row["type"] == 3 and isinstance(row["fileID"], int) and row["fileID"] != 0,
             f"non-null prefab provider reference required: {rel}:{field}[{i}]")
        result.append({"index": i, "file_id": row["fileID"], "guid": row["guid"], "type": row["type"]})
    return {"path": path, "meta": meta, "sha256": sha(raw), "meta_sha256": sha(meta.read_bytes()),
            "asset_guid": meta_guid, "script_path": expected_script, "script_sha256": sha(expected_script.read_bytes()),
            "rows": result}


def prefab_doc(raw: bytes, anchor: int) -> tuple[str, bytes] | None:
    docs = list(re.finditer(rb"(?m)^--- !u!(\d+) &(-?\d+)(?:[ \t]+stripped)?\s*$", raw))
    for i, match in enumerate(docs):
        if int(match.group(2)) == anchor:
            end = docs[i + 1].start() if i + 1 < len(docs) else len(raw)
            return match.group(1).decode(), raw[match.start():end]
    return None


def prefab_documents(raw: bytes):
    """Yield Unity YAML documents without allowing a field match across rows."""
    docs = list(re.finditer(rb"(?m)^--- !u!(\d+) &(-?\d+)(?:[ \t]+stripped)?\s*$", raw))
    for i, match in enumerate(docs):
        end = docs[i + 1].start() if i + 1 < len(docs) else len(raw)
        yield match.group(1).decode(), int(match.group(2)), raw[match.start():end]


def parse_unity_document(repo: Path, helper, raw: bytes, context: str) -> dict:
    """Parse one Unity document through the repository's strict duplicate-key/alias rejecting reader."""
    normalized = re.sub(rb"^--- !u!\d+ &-?\d+(?:[ \t]+stripped)?\s*$", b"---", raw, count=1, flags=re.M)
    with tempfile.TemporaryDirectory(prefix="s002-provider-yaml-") as temp:
        path = Path(temp) / "document.asset"
        path.write_bytes(b"%YAML 1.1\n" + normalized)
        try:
            return helper.strict_read(repo / helper.READER_REL, [path])[str(path)]
        except (ValueError, OSError, subprocess.SubprocessError, json.JSONDecodeError) as error:
            raise ValueError("strict Unity document rejected: " + context) from error


def exact_config_rows(repo: Path, helper, raw: bytes, context: str) -> tuple[list[dict], list[tuple[int, dict]]]:
    """Return exact variant config override rows and direct authoring components."""
    overrides, direct = [], []
    for kind, anchor, document in prefab_documents(raw):
        if kind == "1001":
            parsed = parse_unity_document(repo, helper, document, context + " PrefabInstance")
            prefab_instance = parsed.get("PrefabInstance")
            if not isinstance(prefab_instance, dict):
                continue
            modifications = (prefab_instance.get("m_Modification") or {}).get("m_Modifications")
            if modifications is None:
                continue
            need(isinstance(modifications, list), "PrefabInstance modifications must be an ordered list: " + context)
            for row in modifications:
                need(isinstance(row, dict), "PrefabInstance modification row must be a mapping: " + context)
                if row.get("propertyPath") == "config":
                    overrides.append(row)
        elif kind == "114":
            parsed = parse_unity_document(repo, helper, document, context + " MonoBehaviour")
            component = parsed.get("MonoBehaviour")
            if isinstance(component, dict):
                direct.append((anchor, component))
    return overrides, direct


def importer_witness_matches_target(witness: dict, owner_path: str, owner_sha256: str, owner_guid: str,
                                    owner_file_id: int, owner_script_guid: str) -> bool:
    """Match only the pinned shared base component identity, independent of child config data."""
    return (witness.get("owner_prefab_path") == IMPORTER_TARGET_PATH and owner_path == IMPORTER_TARGET_PATH and
            witness.get("owner_prefab_sha256") == owner_sha256 and
            witness.get("target_guid") == IMPORTER_TARGET_GUID == owner_guid and
            witness.get("target_file_id") == IMPORTER_TARGET_FILE_ID == owner_file_id and
            witness.get("target_path") == IMPORTER_TARGET_PATH and
            witness.get("target_type") == IMPORTER_TARGET_TYPE and
            witness.get("unit_owner_script_path") == str((ROOT / UNIT_OWNER).resolve()) and
            owner_script_guid == witness.get("unit_owner_script_guid"))


def validate_importer_observation_identity(observation: dict) -> dict:
    """Validate the two target/config identities returned by Unity, independently of receipt freshness."""
    need(observation.get("kind") == "S002PrefabImporterIdentityObservation" and observation.get("status") == "Passed" and
         observation.get("scope") == IMPORTER_READBACK_SCOPE and observation.get("prefab_path") == IMPORTER_PREFAB and
         observation.get("prefab_asset_type") == "Variant" and observation.get("identity_issues") == [] and
         observation.get("observer_loaded_or_modified_scene") is False and observation.get("observer_created_world") is False and
         observation.get("observer_invoked_baker") is False and observation.get("provider_join_complete") is False and
         observation.get("mission_ready") is False,
         "Passed importer observation with exact source-only scope required")
    rows = observation.get("config_overrides")
    need(isinstance(rows, list) and len(rows) == 2, "Exactly two observed source config overrides required")
    expected = [
        {"target_guid": IMPORTER_BASE_TARGET_GUID, "target_local_id": str(IMPORTER_BASE_TARGET_FILE_ID),
         "target_path": IMPORTER_BASE_TARGET_PATH, "target_type": IMPORTER_TARGET_TYPE,
         "target_name": "Unit", "config_guid": IMPORTER_CONFIG_GUID,
         "config_local_id": str(IMPORTER_CONFIG_FILE_ID), "config_type": "Game.Configs.UnitGridAuthoringPrefabConfigAsset",
         "config_path": IMPORTER_CONFIG_PATH, "property_path": "config"},
        {"target_guid": IMPORTER_TARGET_GUID, "target_local_id": str(IMPORTER_TARGET_FILE_ID),
         "target_path": IMPORTER_TARGET_PATH, "target_type": IMPORTER_TARGET_TYPE,
         "target_name": "Unit_Veh", "config_guid": IMPORTER_CONFIG_GUID,
         "config_local_id": str(IMPORTER_CONFIG_FILE_ID), "config_type": "Game.Configs.UnitGridAuthoringPrefabConfigAsset",
         "config_path": IMPORTER_CONFIG_PATH, "property_path": "config"},
    ]
    actual = [{key: row.get(key) for key in expected[0]} for row in rows]
    need(sorted(actual, key=lambda row: row["target_guid"]) == sorted(expected, key=lambda row: row["target_guid"]),
         "Importer-resolved targets/config references differ from the exact serialized identities")
    return next(row for row in rows if row.get("target_guid") == IMPORTER_TARGET_GUID and
                row.get("target_local_id") == str(IMPORTER_TARGET_FILE_ID))


def validate_partial_target_evidence(evidence: dict, repo: Path) -> dict:
    """Require both exact child-variant rows; the importer witness never substitutes for source rows."""
    need(evidence.get("kind") == "S002PrefabConfigTargetPartialSourceEvidence" and
         evidence.get("status") == "PartialUnresolved" and evidence.get("prefab_path") == IMPORTER_PREFAB and
         evidence.get("complete_provider_join") is False and evidence.get("unity_importer_semantics") is False and
         evidence.get("mission_ready") is False,
         "Exact unresolved source-only child prefab evidence required")
    expected = [
        {"property_path": "config", "target_guid": IMPORTER_BASE_TARGET_GUID,
         "target_file_id": IMPORTER_BASE_TARGET_FILE_ID, "target_type": 3,
         "config_guid": IMPORTER_CONFIG_GUID, "config_file_id": IMPORTER_CONFIG_FILE_ID,
         "target_prefab_path": IMPORTER_BASE_TARGET_PATH,
         "target_resolution": "serialized_expected_authoring_component"},
        {"property_path": "config", "target_guid": IMPORTER_TARGET_GUID,
         "target_file_id": IMPORTER_TARGET_FILE_ID, "target_type": 3,
         "config_guid": IMPORTER_CONFIG_GUID, "config_file_id": IMPORTER_CONFIG_FILE_ID,
         "target_prefab_path": IMPORTER_TARGET_PATH,
         "target_resolution": "unresolved_local_id_not_in_serialized_document"},
    ]
    rows = evidence.get("rows")
    need(isinstance(rows, list) and len(rows) == 2, "Exactly two serialized child variant config rows required")
    for actual, expected_row in zip(rows, expected):
        for key, value in expected_row.items():
            need(actual.get(key) == value, "Child prefab serialized config row mismatch: " + key)
        need(actual.get("target_guid_hit_count") == 1, "Current source target GUID must resolve uniquely")
    prefab_source = str((repo / IMPORTER_PREFAB).resolve(strict=True))
    need(isinstance(evidence.get("prefab_sha256"), str) and evidence.get("source_pins", {}).get(prefab_source) ==
         evidence.get("prefab_sha256"), "Child source prefab hash must match its evidence source pin")
    return rows[1]


def repo_artifact_path(repo: Path, raw: str, label: str) -> Path:
    repo = repo.resolve(strict=True)
    path = Path(raw)
    lexical = Path(os.path.abspath(path if path.is_absolute() else repo / path))
    need(lexical.is_relative_to(repo), label + " must stay inside this project")
    cursor = repo
    for part in lexical.relative_to(repo).parts:
        cursor = cursor / part
        need(not cursor.is_symlink(), label + " contains a symlink path component")
    path = lexical.resolve(strict=True)
    need(path.is_relative_to(repo) and path.is_file(),
         label + " must be a regular file within this project")
    return path


def validate_importer_identity_receipt(repo: Path, receipt_path: Path) -> dict:
    """Validate the capture chain and return only its single exact component identity witness."""
    repo = repo.resolve(strict=True)
    receipt_path = repo_artifact_path(repo, str(receipt_path), "Importer readback receipt")
    receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
    need(receipt.get("kind") == IMPORTER_READBACK_KIND and receipt.get("status") == "Passed" and
         receipt.get("scope") == IMPORTER_READBACK_SCOPE,
         "Passed importer-identity-only receipt required")
    need(receipt.get("provider_join_complete") is False and receipt.get("factory_execution") is False and
         receipt.get("observer_created_world") is False and receipt.get("observer_invoked_baker") is False and
         receipt.get("source_fit_verified") is False and receipt.get("mission_ready") is False,
         "Importer identity receipt must remain non-baking, non-world, and non-readiness evidence")
    before, after = receipt.get("source_pins_before"), receipt.get("source_pins_after")
    need(isinstance(before, dict) and before and before == after,
         "Importer readback source pins must be complete and unchanged")
    for raw, expected in before.items():
        path = repo_artifact_path(repo, raw, "Importer readback source pin")
        need(sha(path.read_bytes()) == expected, "Importer readback source pin is stale: " + raw)

    partial_desc, evidence_desc = receipt.get("partial_source_receipt"), receipt.get("partial_source_evidence")
    need(isinstance(partial_desc, dict) and isinstance(evidence_desc, dict),
         "Importer receipt must bind its partial source input")
    partial_path = repo_artifact_path(repo, partial_desc.get("path", ""), "Partial source receipt")
    evidence_path = repo_artifact_path(repo, evidence_desc.get("path", ""), "Partial source evidence")
    need(sha(partial_path.read_bytes()) == partial_desc.get("sha256") and
         sha(evidence_path.read_bytes()) == evidence_desc.get("sha256"),
         "Importer receipt partial source input hash mismatch")
    partial = json.loads(partial_path.read_text(encoding="utf-8"))
    evidence = json.loads(evidence_path.read_text(encoding="utf-8"))
    partial_inspector = repo / "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/inspect_s002_prefab_config_targets.py"
    projection_inspector = repo / "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/run_s002_current_setup_provider_projection.py"
    need(partial.get("runner") == {"path": str(partial_inspector), "sha256": sha(partial_inspector.read_bytes())} and
         partial.get("projection_runner") == {"path": str(projection_inspector), "sha256": sha(projection_inspector.read_bytes())},
         "Partial source receipt must bind the exact current inspectors")
    need(partial.get("kind") == "S002PrefabConfigTargetPartialSourceEvidenceReceipt" and
         partial.get("status") == "PartialUnresolved" and partial.get("prefab_path") == IMPORTER_PREFAB and
         partial.get("complete_provider_join") is False and partial.get("mission_ready") is False,
         "Importer witness must bind a partial unresolved source inspection")
    evidence_file_desc = partial.get("evidence")
    need(isinstance(evidence_file_desc, dict) and repo_artifact_path(repo, evidence_file_desc.get("path", ""),
         "Partial evidence file") == evidence_path and evidence_file_desc.get("sha256") == evidence_desc.get("sha256") and
         evidence.get("kind") == "S002PrefabConfigTargetPartialSourceEvidence" and
         evidence.get("source_pins") == partial.get("source_pins"),
         "Importer witness partial receipt/evidence linkage mismatch")
    for raw, expected in partial.get("source_pins", {}).items():
        source = repo_artifact_path(repo, raw, "Partial source pin")
        relative = source.relative_to(repo).as_posix()
        need(before.get(relative) == expected, "Importer readback does not pin the exact partial-source input: " + relative)
    validate_partial_target_evidence(evidence, repo)
    expected_asset_guids = {
        IMPORTER_PREFAB + ".meta": config_guid(repo / (IMPORTER_PREFAB + ".meta")),
        IMPORTER_TARGET_PATH + ".meta": IMPORTER_TARGET_GUID,
        IMPORTER_BASE_TARGET_PATH + ".meta": IMPORTER_BASE_TARGET_GUID,
        IMPORTER_CONFIG_PATH + ".meta": IMPORTER_CONFIG_GUID,
    }
    for relative, expected_guid in expected_asset_guids.items():
        need(relative in before and before[relative] == sha((repo / relative).read_bytes()) and
             config_guid(repo / relative) == expected_guid,
             "Importer readback does not pin the exact current GUID/meta identity: " + relative)

    manifest_desc = receipt.get("pin_manifest")
    need(isinstance(manifest_desc, dict) and manifest_desc.get("sha256_before") == manifest_desc.get("sha256_after"),
         "Importer pin manifest must remain unchanged")
    manifest_path = repo_artifact_path(repo, manifest_desc.get("path", ""), "Importer pin manifest")
    need(sha(manifest_path.read_bytes()) == manifest_desc.get("sha256_after"), "Importer pin manifest hash mismatch")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    need(manifest.get("kind") == "S002PrefabImporterIdentityPinManifest" and
         manifest.get("scope") == "ImporterObjectIdentityOnly" and manifest.get("source_pins") == before,
         "Importer pin manifest does not match the readback receipt")

    observation_desc = receipt.get("observation")
    need(isinstance(observation_desc, dict), "Importer observation descriptor required")
    observation_path = repo_artifact_path(repo, observation_desc.get("path", ""), "Importer observation")
    need(sha(observation_path.read_bytes()) == observation_desc.get("sha256"), "Importer observation hash mismatch")
    observation = json.loads(observation_path.read_text(encoding="utf-8"))
    observed = validate_importer_observation_identity(observation)
    project_version_path = repo / "ProjectSettings/ProjectVersion.txt"
    version_match = re.search(r"(?m)^m_EditorVersion:\s*([^\s]+)\s*$", project_version_path.read_text(encoding="utf-8"))
    need(version_match is not None and "ProjectSettings/ProjectVersion.txt" in before and
         before["ProjectSettings/ProjectVersion.txt"] == sha(project_version_path.read_bytes()) and
         observation.get("unity_version") == version_match.group(1),
         "Importer Unity version must match the current pinned project editor version")
    prefab_identity = observation.get("prefab_asset")
    need(isinstance(prefab_identity, dict) and prefab_identity.get("path") == IMPORTER_PREFAB and
         prefab_identity.get("guid") == expected_asset_guids[IMPORTER_PREFAB + ".meta"] and
         isinstance(prefab_identity.get("local_id"), str) and prefab_identity["local_id"].isdigit(),
         "Importer observation prefab asset identity differs from its pinned current meta GUID")
    need(observation.get("source_pins_before") == before and observation.get("source_pins_after") == after,
         "Importer observation source pins differ from receipt capture pins")

    wrapper = receipt.get("wrapper")
    need(isinstance(wrapper, dict) and wrapper.get("path") == "Tools/CI/invoke_unity_macos.sh" and
         wrapper.get("exit_code") == 0 and wrapper.get("required_marker") == IMPORTER_MARKER and
         wrapper.get("marker_present") is True,
         "Repository Unity wrapper must exit zero and report the exact importer pass marker")
    command = wrapper.get("command")
    wrapper_path = (repo / "Tools/CI/invoke_unity_macos.sh").resolve(strict=True)
    need(isinstance(command, list) and len(command) >= 10 and command[1:2] == ["proxy"] and
         Path(command[2]).resolve(strict=True) == wrapper_path and "-quit" in command and
         "-executeMethod" in command and command[command.index("-executeMethod") + 1] ==
         "Game.Tests.Editor.S002PrefabImporterIdentityObserver.Run",
         "Importer capture must use the recorded repository wrapper executeMethod command")
    for flag, expected in (("-s002-prefab-importer-output", observation_path),
                           ("-s002-prefab-importer-pins", manifest_path)):
        need(flag in command and Path(command[command.index(flag) + 1]).resolve(strict=True) == expected,
             "Importer wrapper command does not bind expected " + flag)
    wrapper_log_path = repo_artifact_path(repo, wrapper.get("log", ""), "Importer wrapper log")
    need(sha(wrapper_log_path.read_bytes()) == wrapper.get("log_sha256"), "Importer wrapper log hash mismatch")
    unity_log_desc = receipt.get("unity_log")
    need(isinstance(unity_log_desc, dict), "Importer Unity log descriptor required")
    unity_log_path = repo_artifact_path(repo, unity_log_desc.get("path", ""), "Importer Unity log")
    unity_log = unity_log_path.read_text(encoding="utf-8", errors="replace")
    need(sha(unity_log_path.read_bytes()) == unity_log_desc.get("sha256") and
         unity_log.count(IMPORTER_MARKER) == 1,
         "Importer Unity log must hash-match and contain exactly one required pass marker")
    rtk = receipt.get("rtk_invocation")
    need(isinstance(rtk, dict) and rtk.get("command_prefix") == [rtk.get("executable_path"), "proxy"] and
         rtk.get("binary_sha256_before") == rtk.get("binary_sha256_after"),
         "RTK wrapper invocation identity must remain stable")
    rtk_path = Path(rtk.get("executable_path", "")).resolve(strict=True)
    need(rtk_path.is_file() and not rtk_path.is_symlink() and
         sha(rtk_path.read_bytes()) == rtk.get("binary_sha256_after"), "RTK executable hash mismatch")
    need(command[0] == str(rtk_path) and command[:2] == rtk.get("command_prefix"),
         "Unity wrapper must be launched by the recorded RTK proxy")
    unity_log_arg = command.index("--log") + 1 if "--log" in command else -1
    need(unity_log_arg > 0 and Path(command[unity_log_arg]).resolve(strict=True) == unity_log_path,
         "Unity wrapper command log path differs from the pinned Unity log")

    owner_script = repo / UNIT_OWNER
    owner_source = owner_script.read_text(encoding="utf-8")
    need(re.search(r"\bnamespace\s+Game\.Authoring\b", owner_source) is not None and
         re.search(r"\bclass\s+UnitGridAuthoring\b", owner_source) is not None,
         "Pinned current source must declare Game.Authoring.UnitGridAuthoring")
    owner_script_guid = script_guid(owner_script)
    owner_prefab = repo / IMPORTER_TARGET_PATH
    owner_prefab_sha256 = sha(owner_prefab.read_bytes())
    need(before.get(IMPORTER_TARGET_PATH) == owner_prefab_sha256,
         "Importer receipt must pin the exact shared Unit_Veh base prefab bytes")
    witness = {"owner_prefab_path": IMPORTER_TARGET_PATH,
               "owner_prefab_sha256": owner_prefab_sha256,
               "target_guid": IMPORTER_TARGET_GUID, "target_file_id": IMPORTER_TARGET_FILE_ID,
               "target_path": IMPORTER_TARGET_PATH, "target_type": IMPORTER_TARGET_TYPE,
               "unit_owner_script_path": str(owner_script),
               "unit_owner_script_guid": owner_script_guid, "receipt_path": str(receipt_path),
               "receipt_sha256": sha(receipt_path.read_bytes()),
               "observation_path": str(observation_path), "observation_sha256": sha(observation_path.read_bytes()),
               "source_pins": {str(repo / relative): expected for relative, expected in before.items()},
               "evidence_artifacts": {str(path): sha(path.read_bytes()) for path in
                                      (receipt_path, observation_path, manifest_path, wrapper_log_path, unity_log_path)}}
    return witness


def inspect_prefab_config_targets(repo: Path, prefab: Path, owner_script: Path, helper) -> dict:
    """Emit partial source identity evidence without asserting unresolved Unity object semantics."""
    raw = prefab.read_bytes()
    need(0 < len(raw) <= MAX_PREFAB and not prefab.is_symlink(), "bounded regular prefab required: " + str(prefab))
    owner_guid = script_guid(owner_script)
    owner_meta = Path(str(owner_script) + ".meta")
    pins = {str(prefab): sha(raw), str(Path(str(prefab) + ".meta")): sha(Path(str(prefab) + ".meta").read_bytes()),
            str(owner_script): sha(owner_script.read_bytes()), str(owner_meta): sha(owner_meta.read_bytes())}
    rows, _ = exact_config_rows(repo, helper, raw, str(prefab))
    inspected = []
    for index, row in enumerate(rows):
        target, reference = row.get("target"), row.get("objectReference")
        need(isinstance(target, dict) and isinstance(reference, dict), "config row target/reference mappings required")
        target_guid, file_id = target.get("guid"), target.get("fileID")
        need(isinstance(target_guid, str) and re.fullmatch(r"[0-9a-f]{32}", target_guid) and
             isinstance(file_id, int) and file_id != 0 and target.get("type") == 3,
             "exact Unity component target identity required")
        need(row.get("propertyPath") == "config" and reference.get("type") == 2 and
             isinstance(reference.get("guid"), str) and re.fullmatch(r"[0-9a-f]{32}", reference["guid"]),
             "exact config object reference required")
        hits = guid_hits_cached(repo, helper, target_guid, {})
        item = {"row_index": index, "property_path": "config", "target_guid": target_guid,
                "target_file_id": file_id, "target_type": target.get("type"),
                "config_guid": reference["guid"], "config_file_id": reference.get("fileID"),
                "target_guid_hit_count": len(hits)}
        if len(hits) != 1:
            item["target_resolution"] = "unresolved_or_ambiguous_guid"
        else:
            meta, path = helper.checked_meta_target(repo / "Assets", hits[0])
            pin_target(repo, meta, path, pins)
            item["target_prefab_path"] = str(path.relative_to(repo))
            doc = prefab_doc(path.read_bytes(), file_id) if path.suffix == ".prefab" else None
            if doc is None:
                item["target_resolution"] = "unresolved_local_id_not_in_serialized_document"
            else:
                item["target_document_type"] = int(doc[0])
                if doc[0] != "114":
                    item["target_resolution"] = "serialized_non_monobehaviour_document"
                else:
                    parsed = parse_unity_document(repo, helper, doc[1], str(path) + " target component")
                    component = parsed.get("MonoBehaviour")
                    script = component.get("m_Script") if isinstance(component, dict) else None
                    if not isinstance(script, dict):
                        item["target_resolution"] = "serialized_monobehaviour_script_unresolved"
                    else:
                        item["target_script_guid"] = script.get("guid")
                        item["target_resolution"] = ("serialized_expected_authoring_component" if
                            script.get("fileID") == 11500000 and script.get("guid") == owner_guid and script.get("type") == 3
                            else "serialized_other_component")
        inspected.append(item)
    need(inspected, "at least one serialized config override row required")
    return {"kind": "S002PrefabConfigTargetPartialSourceEvidence",
            "status": "PartialUnresolved" if any(row["target_resolution"] != "serialized_expected_authoring_component" for row in inspected) else "AllTargetsTyped",
            "prefab_path": str(prefab.relative_to(repo)), "prefab_sha256": sha(raw),
            "rows": inspected, "source_pins": dict(sorted(pins.items())),
            "guid_resolution_scope": "Point-in-time current Assets GUID scan; uniqueness is not a replayable census.",
            "complete_provider_join": False, "factory_execution": False, "unity_importer_semantics": False,
            "source_fit_verified": False, "mission_ready": False,
            "scope": "Serialized propertyPath=config rows and source-file identity only. Missing local IDs remain unresolved; no Unity import semantics, factory execution, provider join, scene fit, or readiness claim."}


def guid_hits_cached(repo: Path, helper, guid: str, cache: dict[str, list[Path]]) -> list[Path]:
    if guid not in cache:
        cache[guid] = helper.guid_hits(repo / "Assets", guid)
    return cache[guid]


def config_assignments(repo: Path, prefab: Path, owner_script: str, config_script: Path, assets_helper,
                       pins: dict[str, str], guid_cache: dict[str, list[Path]], importer_witness=None):
    raw = prefab.read_bytes()
    need(0 < len(raw) <= MAX_PREFAB and not prefab.is_symlink(), "bounded regular prefab required: " + str(prefab))
    pins[str(prefab)] = sha(raw)
    assignments = []
    # Variants serialize config overrides through a target component and the
    # exact propertyPath row. Parse each document with the strict YAML reader;
    # never let whitespace matching cross into a neighboring modification.
    override_rows, direct_components = exact_config_rows(repo, assets_helper, raw, str(prefab))
    for row in override_rows:
        target = row.get("target")
        reference = row.get("objectReference")
        need(isinstance(target, dict) and isinstance(reference, dict),
             "config override row needs target and objectReference mappings: " + str(prefab))
        owner_file_id, owner_guid = target.get("fileID"), target.get("guid")
        config_file_id, config_asset_guid = reference.get("fileID"), reference.get("guid")
        need(isinstance(owner_file_id, int) and owner_file_id != 0 and
             isinstance(owner_guid, str) and re.fullmatch(r"[0-9a-f]{32}", owner_guid) and target.get("type") == 3,
             "config override target must be an exact source component reference: " + str(prefab))
        need(isinstance(config_file_id, int) and isinstance(config_asset_guid, str) and
             re.fullmatch(r"[0-9a-f]{32}", config_asset_guid) and reference.get("type") == 2,
             "config override objectReference must be an exact source config reference: " + str(prefab))
        owner_hits = guid_hits_cached(repo, assets_helper, owner_guid, guid_cache)
        need(len(owner_hits) == 1, "prefab component owner GUID must resolve uniquely: " + owner_guid)
        owner_meta, owner_path = assets_helper.checked_meta_target(repo / "Assets", owner_hits[0])
        need(owner_path.suffix == ".prefab", "config override owner must be a source prefab")
        owner_raw = owner_path.read_bytes()
        pin_target(repo, owner_meta, owner_path, pins)
        owner_doc = prefab_doc(owner_raw, owner_file_id)
        resolution_basis = "serialized_yaml_component"
        if owner_doc is None:
            need(importer_witness is not None and importer_witness_matches_target(
                     importer_witness, str(owner_path.relative_to(repo)), sha(owner_raw), owner_guid,
                     owner_file_id, owner_script),
                 "config override target is absent from YAML and lacks the exact current importer identity witness")
            resolution_basis = "exact_unity_importer_identity_witness"
        else:
            need(owner_doc[0] == "114", "config override target is not a serialized MonoBehaviour component")
            owner_parsed = parse_unity_document(repo, assets_helper, owner_doc[1], str(owner_path) + " target component")
            owner_component = owner_parsed.get("MonoBehaviour")
            script = owner_component.get("m_Script") if isinstance(owner_component, dict) else None
            need(isinstance(script, dict) and script.get("fileID") == 11500000 and script.get("guid") == owner_script and script.get("type") == 3,
                 "config override does not target the exact original authoring owner")
        need(config_file_id == 11400000, "authoring config must reference its main ScriptableObject")
        assignments.append({"config_guid": config_asset_guid, "config_file_id": config_file_id,
                            "owner_prefab_path": str(owner_path.relative_to(repo)),
                            "owner_component_file_id": owner_file_id, "owner_component_guid": owner_guid,
                            "owner_resolution_basis": resolution_basis})

    # Non-variant prefabs may serialize config directly on their own component.
    if not assignments:
        for anchor, component in direct_components:
            script, reference = component.get("m_Script"), component.get("config")
            if not (isinstance(script, dict) and script.get("fileID") == 11500000 and
                    script.get("guid") == owner_script and script.get("type") == 3 and isinstance(reference, dict)):
                continue
            need(reference.get("fileID") == 11400000 and reference.get("type") == 2 and
                 isinstance(reference.get("guid"), str) and re.fullmatch(r"[0-9a-f]{32}", reference["guid"]),
                 "authoring config reference must use exact main object fileID and GUID")
            assignments.append({"config_guid": reference["guid"], "config_file_id": 11400000,
                                "owner_prefab_path": str(prefab.relative_to(repo)),
                                "owner_component_file_id": anchor, "owner_component_guid": None,
                                "owner_resolution_basis": "serialized_yaml_component"})
    need(assignments, "prefab must have at least one expected authoring config binding: " + str(prefab))
    owner_identities = {(row["owner_prefab_path"], row["owner_component_file_id"], row["owner_component_guid"])
                        for row in assignments}
    need(len(owner_identities) == len(assignments),
         "prefab config override chain must resolve to distinct owner components: " + str(prefab))
    witness_used = any(row["owner_resolution_basis"] == "exact_unity_importer_identity_witness" for row in assignments)
    if witness_used:
        need(len(assignments) == 2 and
             assignments[0]["owner_prefab_path"] == IMPORTER_BASE_TARGET_PATH and
             assignments[0]["owner_component_guid"] == IMPORTER_BASE_TARGET_GUID and
             assignments[0]["owner_component_file_id"] == IMPORTER_BASE_TARGET_FILE_ID and
             assignments[0]["owner_resolution_basis"] == "serialized_yaml_component" and
             assignments[1]["owner_prefab_path"] == IMPORTER_TARGET_PATH and
             assignments[1]["owner_component_guid"] == IMPORTER_TARGET_GUID and
             assignments[1]["owner_component_file_id"] == IMPORTER_TARGET_FILE_ID and
             assignments[1]["owner_resolution_basis"] == "exact_unity_importer_identity_witness",
             "Variant referencing the witnessed base owner must retain the exact two-row inherited config owner chain")
    else:
        need(len(assignments) == 1,
             "Only a child referencing the exact witnessed base owner may use a two-owner config chain: " + str(prefab))
    resolved_bindings = []
    for assignment in assignments:
        config_hits = guid_hits_cached(repo, assets_helper, assignment["config_guid"], guid_cache)
        need(len(config_hits) == 1, "authoring config GUID must resolve uniquely: " + assignment["config_guid"])
        config_meta = repo / config_hits[0]
        config_path = Path(str(config_meta)[:-5])
        need(config_path.suffix == ".asset", "authoring config GUID must resolve to a ScriptableObject asset")
        config_raw = config_path.read_bytes()
        pin_target(repo, config_meta, config_path, pins)
        need(assets_helper.unity_anchor(config_path, assignment["config_file_id"]),
             "authoring config local fileID must match a serialized asset anchor")
        parsed = assets_helper.strict_read(repo / assets_helper.READER_REL, [config_path])[str(config_path)]
        mb = parsed.get("MonoBehaviour")
        need(isinstance(mb, dict) and (mb.get("m_Script") or {}).get("fileID") == 11500000,
             "exact authoring config MonoBehaviour required")
        config_script_hits = guid_hits_cached(repo, assets_helper, mb["m_Script"].get("guid", ""), guid_cache)
        need(len(config_script_hits) == 1, "authoring config MonoScript GUID must resolve uniquely")
        config_script_meta, config_script_target = assets_helper.checked_meta_target(repo / "Assets", config_script_hits[0])
        need(config_script_target == config_script and config_script_target.suffix == ".cs",
             "authoring config must use the exact original source config type: " + str(config_path))
        pin_target(repo, config_script_meta, config_script_target, pins)
        resolved_bindings.append({**assignment, "config_path": str(config_path.relative_to(repo)),
                                  "config_asset_guid": config_guid(config_meta), "config_sha256": sha(config_raw),
                                  "config_meta_sha256": sha(config_meta.read_bytes()),
                                  "config_kind_script_guid": mb["m_Script"]["guid"]})
    if witness_used:
        for raw_path, expected_hash in importer_witness["source_pins"].items():
            prior = pins.get(raw_path)
            need(prior is None or prior == expected_hash, "importer witness source pin conflicts with provider source pin: " + raw_path)
            pins[raw_path] = expected_hash
        for raw_path, expected_hash in importer_witness["evidence_artifacts"].items():
            prior = pins.get(raw_path)
            need(prior is None or prior == expected_hash, "importer witness artifact pin conflicts with provider source pin: " + raw_path)
            pins[raw_path] = expected_hash
    distinct_config_paths = sorted({row["config_path"] for row in resolved_bindings})
    distinct_config_guids = sorted({row["config_asset_guid"] for row in resolved_bindings})
    return {"config_path": distinct_config_paths[0] if len(distinct_config_paths) == 1 else None,
            "config_paths": distinct_config_paths,
            "config_asset_guid": distinct_config_guids[0] if len(distinct_config_guids) == 1 else None,
            "owner_bindings": resolved_bindings,
            "owner_resolution_basis": "exact_unity_importer_identity_witness" if witness_used else "serialized_yaml_component",
            "effective_override_selection_verified": False,
            "unity_effective_config_readback": False}


def pin_target(repo: Path, meta: Path, target: Path, pins: dict[str, str]) -> None:
    for path in (meta, target):
        raw = path.read_bytes()
        need(0 < len(raw) <= MAX_PREFAB and not path.is_symlink(), "regular bounded source identity required: " + str(path))
        pins[str(path)] = sha(raw)


def local_id_validation(helper, target: Path, file_id: int) -> str:
    # An anchor alone does not establish that an imported prefab reference is
    # its main/root GameObject. Preserve the number without upgrading semantics.
    return "anchor_present_unqualified" if helper.unity_anchor(target, file_id) else "unresolved"


def require_unique_key(rows: list[dict], key: str, owner: str) -> dict:
    matches = [row for row in rows if row["key"] == key]
    need(len(matches) == 1, owner + " key must resolve to exactly one provider object: " + key)
    return matches[0]


def require_force_role_binding(force: dict, role_by_id: dict) -> dict:
    role = role_by_id.get(force.get("role_id"))
    need(role is not None and role.get("SourceConfigPath") == force.get("source_config_path") and
         role.get("RuntimePrefabKey") == force.get("runtime_prefab_key"),
         "compiled force differs from current authored role catalog binding: " + str(force.get("role_id")))
    return role


def require_force_config_match(force: dict, provider_prefab: dict) -> dict:
    bindings = provider_prefab["config"].get("owner_bindings")
    need(isinstance(bindings, list) and bindings, "Serialized UnitGridAuthoring config owner rows are required")
    expected_path = force.get("source_config_path")
    witness_bindings = [row for row in bindings if row.get("owner_resolution_basis") ==
                        "exact_unity_importer_identity_witness"]
    if witness_bindings:
        matches = [row for row in witness_bindings if row.get("owner_prefab_path") == IMPORTER_TARGET_PATH and
                   row.get("owner_component_guid") == IMPORTER_TARGET_GUID and
                   row.get("owner_component_file_id") == IMPORTER_TARGET_FILE_ID and
                   row.get("config_path") == expected_path]
    else:
        matches = [row for row in bindings if row.get("config_path") == expected_path]
    need(len(matches) == 1,
         "compiled force source config must match exactly one serialized child config binding: " +
         str(force.get("runtime_prefab_key")))
    # This identifies the source row that matches the compiled role. It does
    # not claim Unity's effective nested-prefab override selection was run.
    result = dict(provider_prefab)
    config = dict(provider_prefab["config"])
    config["config_path"] = matches[0]["config_path"]
    config["selected_source_config_binding"] = matches[0]
    config["effective_override_selection_verified"] = False
    config["unity_effective_config_readback"] = False
    result["config"] = config
    return result


def csharp_method_body(source: str, method_pattern: str, context: str) -> str:
    match = re.search(method_pattern, source)
    need(match is not None, "exact source-owned method missing: " + context)
    opening = source.find("{", match.end())
    need(opening >= 0, "source-owned method body missing: " + context)
    depth = 0
    for index in range(opening, len(source)):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                return source[opening:index + 1]
    raise ValueError("unterminated source-owned method body: " + context)


def validate_ground_staging_source_contract(visual_source: str, access_source: str,
                                            ids_source: str) -> None:
    """Require the exact native source route for this special visual key."""
    ensure = csharp_method_body(
        visual_source, r"\bEnsureAuthoredGroundStaging\s*\(\s*SkirmishVisualPrefabCatalog\s+catalog\s*\)",
        "SkirmishVisualSpawnService.EnsureAuthoredGroundStaging")
    ordered = ("SkirmishStructureIds.VisualKey(SkirmishStructureIds.GroundStaging)",
               "catalog.Contains(key)", "SkirmishGroundStagingPrefabAccess.TryLoadAuthored(",
               "catalog.Bind(key, staging, owned)")
    positions = [ensure.find(token) for token in ordered]
    need(all(position >= 0 for position in positions) and positions == sorted(positions),
         "GroundStaging visual owner must preserve its exact authored access path and order")
    access = csharp_method_body(access_source, r"\bTryLoadAuthored\s*\(\s*out\s+GameObject\s+prefab\s*,\s*out\s+bool\s+owned\s*\)",
                                "SkirmishGroundStagingPrefabAccess.TryLoadAuthored")
    access_tokens = ("owned = false;", "prefab = null;", "#if UNITY_EDITOR",
                     "AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)",
                     "if (IsUsable(prefab))", "return true;", "#endif",
                     "prefab = Resources.Load<GameObject>(ResourcesKey);",
                     "if (IsUsable(prefab))", "return true;",
                     "SkirmishGroundStagingConfig config = LoadAuthoredConfig();",
                     "prefab = SkirmishGroundStagingBuilder.BuildHierarchy(config);",
                     "owned = prefab != null;", "return owned;")
    positions, cursor = [], 0
    for token in access_tokens:
        position = access.find(token, cursor)
        positions.append(position)
        if position >= 0:
            cursor = position + len(token)
    need(all(position >= 0 for position in positions) and positions == sorted(positions),
         "GroundStaging access owner must preserve exact guarded authored, Resources, and builder fallback order")
    editor_branch = access[positions[2]:positions[6] + len("#endif")]
    need("if (IsUsable(prefab))\n                return true;" in editor_branch,
         "GroundStaging editor authored load must return only when the loaded prefab is usable")
    resources_branch = access[positions[7]:positions[9] + len("return true;")]
    need("if (IsUsable(prefab))\n                return true;" in resources_branch,
         "GroundStaging Resources load must return only when the loaded prefab is usable")
    need(re.search(r'PrefabPath\s*=\s*"' + re.escape(GROUND_STAGING_PREFAB) + r'"', access_source) is not None,
         "GroundStaging source access must name the exact current prefab path")
    visual_key = csharp_method_body(ids_source, r"\bVisualKey\s*\(\s*string\s+structureId\s*\)",
                                    "SkirmishStructureIds.VisualKey")
    need(re.search(r"structureId\s*==\s*GroundStaging\s*\)\s*return\s*\"" +
                   re.escape(GROUND_STAGING_KEY) + r"\"\s*;", visual_key) is not None,
         "GroundStaging structure ID must retain its exact visual key")


def validate_ground_staging_absent_from_generic_provider(provider_rows: list[dict]) -> None:
    need(isinstance(provider_rows, list) and
         all(isinstance(row, dict) and isinstance(row.get("key"), str) for row in provider_rows),
         "Resolved current generic structure provider rows required")
    need(not any(row["key"] == GROUND_STAGING_KEY for row in provider_rows),
         "GroundStaging special source owner must remain absent from generic BuildingPlacement provider rows")


def resolve_ground_staging_special_owner(repo: Path, helper, pins: dict[str, str]) -> dict:
    """Bind only the special GroundStaging source owner, outside generic spawnables."""
    visual_path, access_path, ids_path = (repo / GROUND_STAGING_VISUAL_OWNER,
                                          repo / GROUND_STAGING_ACCESS, repo / GROUND_STAGING_ID_OWNER)
    visual_source = visual_path.read_text(encoding="utf-8")
    access_source = access_path.read_text(encoding="utf-8")
    ids_source = ids_path.read_text(encoding="utf-8")
    validate_ground_staging_source_contract(visual_source, access_source, ids_source)
    source_paths = (visual_path, access_path, ids_path)
    for source in source_paths:
        meta = Path(str(source) + ".meta")
        pin_target(repo, meta, source, pins)

    prefab = repo / GROUND_STAGING_PREFAB
    meta = Path(str(prefab) + ".meta")
    prefab_raw = prefab.read_bytes()
    prefab_guid = config_guid(meta)
    pin_target(repo, meta, prefab, pins)
    root_docs = [(anchor, document) for kind, anchor, document in prefab_documents(prefab_raw) if kind == "1"]
    matching_roots = []
    for anchor, document in root_docs:
        parsed = parse_unity_document(repo, helper, document, GROUND_STAGING_PREFAB + " GameObject")
        game_object = parsed.get("GameObject")
        if isinstance(game_object, dict) and game_object.get("m_Name") == GROUND_STAGING_KEY:
            matching_roots.append((anchor, game_object))
    need(len(matching_roots) == 1, "exact single authored GroundStaging root object required")
    root_anchor, root = matching_roots[0]
    components = root.get("m_Component")
    need(isinstance(components, list), "GroundStaging root component references required")
    component_ids = [row.get("component", {}).get("fileID") for row in components if isinstance(row, dict)]
    need(len(component_ids) == len(components) and len(set(component_ids)) == len(component_ids),
         "GroundStaging root component references must be unique exact local IDs")

    authoring_guid = script_guid(repo / BUILDING_AUTHORING_OWNER)
    identity_guid = script_guid(repo / GROUND_STAGING_IDENTITY_OWNER)
    identity_found = False
    authoring_component = None
    for kind, anchor, document in prefab_documents(prefab_raw):
        if kind != "114" or anchor not in component_ids:
            continue
        parsed = parse_unity_document(repo, helper, document, GROUND_STAGING_PREFAB + " component")
        component = parsed.get("MonoBehaviour")
        if not isinstance(component, dict) or component.get("m_GameObject", {}).get("fileID") != root_anchor:
            continue
        script = component.get("m_Script")
        if not isinstance(script, dict) or script.get("fileID") != 11500000 or script.get("type") != 3:
            continue
        if script.get("guid") == identity_guid:
            need(component.get("structureId") == "building.ground_staging",
                 "GroundStaging identity component must serialize the exact structure ID")
            identity_found = True
        if script.get("guid") == authoring_guid:
            authoring_component = component
    need(identity_found and isinstance(authoring_component, dict),
         "GroundStaging prefab must attach both current identity and BuildingDefinitionAuthoring components")
    reference = authoring_component.get("config")
    need(isinstance(reference, dict) and reference.get("fileID") == 11400000 and reference.get("type") == 2 and
         isinstance(reference.get("guid"), str) and re.fullmatch(r"[0-9a-f]{32}", reference["guid"]),
         "GroundStaging BuildingDefinitionAuthoring config reference must be exact")
    config_hits = guid_hits_cached(repo, helper, reference["guid"], {})
    need(len(config_hits) == 1, "GroundStaging config asset GUID must resolve uniquely")
    config_meta, config_path = helper.checked_meta_target(repo / "Assets", config_hits[0])
    need(config_path.suffix == ".asset", "GroundStaging config must resolve to a source asset")
    config_raw = config_path.read_bytes()
    pin_target(repo, config_meta, config_path, pins)
    config_yaml = helper.strict_read(repo / helper.READER_REL, [config_path])[str(config_path)]
    config_component = config_yaml.get("MonoBehaviour")
    need(isinstance(config_component, dict) and config_component.get("m_Script", {}).get("fileID") == 11500000,
         "GroundStaging authoring config must be a serialized scriptable-object component")
    config_script_hits = guid_hits_cached(repo, helper, config_component["m_Script"].get("guid", ""), {})
    need(len(config_script_hits) == 1, "GroundStaging config script GUID must resolve uniquely")
    config_script_meta, config_script_path = helper.checked_meta_target(repo / "Assets", config_script_hits[0])
    need(config_script_path == repo / BUILDING_CONFIG_OWNER,
         "GroundStaging config must use the exact original BuildingDefinitionAuthoring config source type")
    pin_target(repo, config_script_meta, config_script_path, pins)

    return {"key": GROUND_STAGING_KEY, "source_kind": "separate_skirmish_ground_staging_visual_owner",
            "source_binding_basis": "SkirmishStructureIds.VisualKey + EnsureAuthoredGroundStaging + TryLoadAuthored exact path",
            "prefab_path": GROUND_STAGING_PREFAB, "prefab_guid": prefab_guid,
            "prefab_sha256": sha(prefab_raw), "root_local_id": root_anchor,
            "root_local_id_semantics": "serialized GameObject document anchor only; not asserted as Unity imported main-object identity",
            "authoring_config_path": str(config_path.relative_to(repo)),
            "authoring_config_guid": reference["guid"], "authoring_config_sha256": sha(config_raw),
            "fallbacks_source_declared_but_not_executed": True,
            "unity_asset_load_or_import_execution": False,
            "generic_building_provider_membership": False,
            "factory_execution": False, "world_allocated": False,
            "source_fit_verified": False, "mission_ready": False,
            "local_id_validation": "not_applicable_special_source_binding"}


def scalar_integer(value, label: str) -> int:
    need(isinstance(value, int) and not isinstance(value, bool), "Serialized integer scalar required: " + label)
    return value


def scalar_float(value, label: str) -> float:
    need(isinstance(value, (int, float)) and not isinstance(value, bool),
         "Serialized float scalar required: " + label)
    result = float(value)
    need(math.isfinite(result), "Finite current scalar required for JSON source input: " + label)
    return result


def declared_numeric_field_default(source_text: str, field: str, scalar_kind: str):
    declaration = re.search(r"\bprivate\s+(?:int|float)\s+" + re.escape(field) + r"\s*(?:=\s*([^;]+))?;",
                             source_text)
    need(declaration is not None, "Exact current serialized component field declaration required: " + field)
    literal = declaration.group(1)
    if literal is None:
        return 0 if scalar_kind == "int" else 0.0
    token = literal.strip()
    if scalar_kind == "float":
        token = re.sub(r"[fFdDmM]$", "", token)
        return scalar_float(float(token), "BuildingDefinitionAuthoring default " + field)
    need(re.fullmatch(r"-?\d+", token) is not None,
         "Exact integer C# field initializer required: " + field)
    return int(token)


def serialized_numeric_field(component: dict, source_text: str, field: str, scalar_kind: str,
                             context: str):
    value = component.get(field)
    if value is None:
        value = declared_numeric_field_default(source_text, field, scalar_kind)
    return scalar_float(value, context) if scalar_kind == "float" else scalar_integer(value, context)


def source_authoring_component(repo: Path, helper, prefab_path: str, owner_component_file_id: int | None,
                               pins: dict[str, str]) -> tuple[dict, Path, int]:
    prefab = repo / prefab_path
    raw = prefab.read_bytes()
    meta = Path(str(prefab) + ".meta")
    pin_target(repo, meta, prefab, pins)
    owner_script = repo / BUILDING_OWNER
    owner_script_guid = script_guid(owner_script)
    owner_source = owner_script.read_text(encoding="utf-8")
    pins[str(owner_script)] = sha(owner_script.read_bytes())
    if owner_component_file_id is not None:
        document = prefab_doc(raw, owner_component_file_id)
        need(document is not None and document[0] == "114",
             "Exact serialized BuildingDefinitionAuthoring component document required: " + prefab_path)
        component = parse_unity_document(repo, helper, document[1], prefab_path + " authoring component").get("MonoBehaviour")
        anchor = owner_component_file_id
        need(isinstance(component, dict) and component.get("m_Script", {}).get("guid") == owner_script_guid,
             "Provider owner row must target current BuildingDefinitionAuthoring source")
    else:
        matches = []
        for kind, anchor, document in prefab_documents(raw):
            if kind != "114":
                continue
            parsed = parse_unity_document(repo, helper, document, prefab_path + " direct authoring component")
            component = parsed.get("MonoBehaviour")
            if isinstance(component, dict) and component.get("m_Script", {}).get("guid") == owner_script_guid:
                matches.append((anchor, component))
        need(len(matches) == 1, "Direct special prefab must have one current BuildingDefinitionAuthoring component")
        anchor, component = matches[0]

    root_id = component.get("m_GameObject", {}).get("fileID")
    need(isinstance(root_id, int), "Authoring component must reference its serialized GameObject")
    root_document = prefab_doc(raw, root_id)
    need(root_document is not None and root_document[0] == "1",
         "Authoring component GameObject must resolve to a serialized GameObject in its owner prefab")
    root = parse_unity_document(repo, helper, root_document[1], prefab_path + " authoring GameObject").get("GameObject")
    refs = root.get("m_Component") if isinstance(root, dict) else None
    component_ids = [row.get("component", {}).get("fileID") for row in refs if isinstance(row, dict)] if isinstance(refs, list) else []
    need(anchor in component_ids, "Authoring component must be attached to its serialized GameObject")
    footprint = component.get("footprintCells")
    if footprint is None:
        dimensions = re.search(r"\bprivate\s+Vector2Int\s+footprintCells\s*=\s*new\s*\(\s*(-?\d+)\s*,\s*(-?\d+)\s*\)",
                               owner_source)
        need(dimensions is not None, "Exact current footprint field initializer required")
        footprint = {"x": int(dimensions.group(1)), "y": int(dimensions.group(2))}
    need(isinstance(footprint, dict), "Serialized authoring footprint required: " + prefab_path)
    source = {"max_health": serialized_numeric_field(component, owner_source, "maxHealth", "int",
                                                     prefab_path + ".maxHealth"),
              "footprint_width": scalar_integer(footprint.get("x"), prefab_path + ".footprintCells.x"),
              "footprint_depth": scalar_integer(footprint.get("y"), prefab_path + ".footprintCells.y"),
              "price": serialized_numeric_field(component, owner_source, "price", "int", prefab_path + ".price"),
              "materials_cost": serialized_numeric_field(component, owner_source, "materialsCost", "int",
                                                         prefab_path + ".materialsCost"),
              "production_duration_seconds": serialized_numeric_field(
                  component, owner_source, "productionDurationSeconds", "float",
                  prefab_path + ".productionDurationSeconds")}
    return source, prefab, anchor


def source_variant_scalar_overrides(repo: Path, helper, prefab: Path, owner_guid: str, owner_file_id: int,
                                    source: dict, expected_config_guid: str, pins: dict[str, str]) -> dict:
    raw = prefab.read_bytes()
    meta = Path(str(prefab) + ".meta")
    pin_target(repo, meta, prefab, pins)
    override_paths = {"maxHealth": ("max_health", "int"), "footprintCells.x": ("footprint_width", "int"),
                      "footprintCells.y": ("footprint_depth", "int"), "price": ("price", "int"),
                      "materialsCost": ("materials_cost", "int"),
                      "productionDurationSeconds": ("production_duration_seconds", "float")}
    seen = set()
    config_refs = []
    for kind, _anchor, document in prefab_documents(raw):
        if kind != "1001":
            continue
        parsed = parse_unity_document(repo, helper, document, str(prefab) + " PrefabInstance")
        instance = parsed.get("PrefabInstance")
        if not isinstance(instance, dict):
            continue
        modifications = (instance.get("m_Modification") or {}).get("m_Modifications") or []
        need(isinstance(modifications, list), "PrefabInstance modification rows must be ordered")
        for modification in modifications:
            if not isinstance(modification, dict):
                raise ValueError("PrefabInstance modification row must be a mapping")
            target = modification.get("target")
            if not isinstance(target, dict) or target.get("guid") != owner_guid or target.get("fileID") != owner_file_id:
                continue
            need(target.get("type") == 3,
                 "Scalar prefab override target must retain exact MonoBehaviour reference type")
            path = modification.get("propertyPath")
            if path == "config":
                reference = modification.get("objectReference")
                need(isinstance(reference, dict) and reference.get("guid") == expected_config_guid and
                     reference.get("fileID") == 11400000 and reference.get("type") == 2,
                     "Serialized authoring config override must match the independently resolved config asset")
                config_refs.append(reference)
            elif path in override_paths:
                need(path not in seen, "Duplicate scalar prefab override path: " + path)
                seen.add(path)
                field, kind_name = override_paths[path]
                value = modification.get("value")
                source[field] = scalar_float(value, str(prefab) + "." + path) if kind_name == "float" else scalar_integer(
                    value, str(prefab) + "." + path)
    if prefab.suffix == ".prefab" and prefab.name != Path(GROUND_STAGING_PREFAB).name:
        need(len(config_refs) == 1,
             "Current provider variant must expose exactly one config override for its exact authoring target")
    return source


def build_building_scalar_inputs(repo: Path, helper, joined_structures: list[dict], pins: dict[str, str]) -> dict:
    unique = {}
    for joined in joined_structures:
        key = joined.get("visual_key")
        if key in unique:
            continue
        if "special_source_binding" in joined:
            special = joined["special_source_binding"]
            prefab_path = special["prefab_path"]
            owner_prefab_path = prefab_path
            config_path = special["authoring_config_path"]
            owner_component_file_id = None
            owner_component_guid = None
        else:
            provider = joined.get("provider_prefab", {})
            config_binding = provider.get("config", {})
            bindings = config_binding.get("owner_bindings")
            need(isinstance(bindings, list) and len(bindings) == 1,
                 "Each current S002 structure provider must have one exact metadata config row: " + str(key))
            binding = bindings[0]
            prefab_path = provider.get("path")
            owner_prefab_path = binding.get("owner_prefab_path")
            config_path = config_binding.get("config_path")
            need(isinstance(config_path, str) and config_path and
                 config_binding.get("config_paths") == [config_path],
                 "Current structure metadata config path must be uniquely source-resolved: " + str(key))
            owner_component_file_id = binding.get("owner_component_file_id")
            owner_component_guid = binding.get("owner_component_guid")

        prefab_source, prefab, component_id = source_authoring_component(
            repo, helper, owner_prefab_path, owner_component_file_id, pins)
        if owner_component_guid is not None:
            source_variant_scalar_overrides(repo, helper, repo / prefab_path, owner_component_guid,
                                             owner_component_file_id, prefab_source,
                                             next(row["config_guid"] for row in
                                                  joined["provider_prefab"]["config"]["owner_bindings"]), pins)
        config = repo / config_path
        config_meta = Path(str(config) + ".meta")
        config_raw = config.read_bytes()
        pin_target(repo, config_meta, config, pins)
        expected_config_guid = (joined["special_source_binding"].get("authoring_config_guid")
                                if "special_source_binding" in joined else
                                joined["provider_prefab"]["config"]["owner_bindings"][0]["config_asset_guid"])
        need(config_guid(config_meta) == expected_config_guid,
             "BuildingDefinitionAuthoring scalar config GUID differs from the selected source binding")
        config_yaml = helper.strict_read(repo / helper.READER_REL, [config])[str(config)]
        config_component = config_yaml.get("MonoBehaviour")
        need(isinstance(config_component, dict), "BuildingDefinitionAuthoring config YAML required: " + config_path)
        config_fields = {"max_health": scalar_integer(config_component.get("maxHealth"), config_path + ".maxHealth"),
                         "raw_price": scalar_integer(config_component.get("price"), config_path + ".price"),
                         "raw_materials_cost": scalar_integer(config_component.get("materialsCost"), config_path + ".materialsCost"),
                         "raw_production_duration_seconds": scalar_float(
                             config_component.get("productionDurationSeconds"), config_path + ".productionDurationSeconds")}
        policy_source = repo / SCALAR_POLICY
        need(policy_source.is_file(), "Shared scalar policy source must exist")
        pins[str(policy_source)] = sha(policy_source.read_bytes())
        policy_meta = Path(str(policy_source) + ".meta")
        pins[str(policy_meta)] = sha(policy_meta.read_bytes())
        unit_policy_source = repo / UNIT_SCALAR_POLICY
        need(unit_policy_source.is_file(), "Shared unit scalar policy source must exist")
        pins[str(unit_policy_source)] = sha(unit_policy_source.read_bytes())
        pins[str(Path(str(unit_policy_source) + ".meta"))] = sha(Path(str(unit_policy_source) + ".meta").read_bytes())
        authoring_source = repo / BUILDING_OWNER
        authoring_config_source = repo / "Assets/Game/Scripts/Configs/GameplayConfigModels.cs"
        for source_path in (authoring_source, authoring_config_source):
            pins[str(source_path)] = sha(source_path.read_bytes())
            source_meta = Path(str(source_path) + ".meta")
            if source_meta.is_file():
                pins[str(source_meta)] = sha(source_meta.read_bytes())
        unique[key] = {"key": key, "prefab_path": prefab_path, "owner_prefab_path": owner_prefab_path,
                       "prefab_component_file_id": component_id,
                       "source_component_values": prefab_source, "config_path": config_path,
                       "config_sha256": sha(config_raw), "source_config_values": config_fields,
                       "has_config": True}
    need(len(unique) == 8,
         "Current S002 Standard scalar metadata input must cover exactly eight unique compiled structure keys")
    rows = []
    for key in sorted(unique):
        entry = unique[key]
        prefab_values, config_values = entry["source_component_values"], entry["source_config_values"]
        rows.append({"key": key, "has_config": True,
                     "prefab_max_health": prefab_values["max_health"],
                     "prefab_footprint_width": prefab_values["footprint_width"],
                     "prefab_footprint_depth": prefab_values["footprint_depth"],
                     "prefab_price": prefab_values["price"],
                     "prefab_materials_cost": prefab_values["materials_cost"],
                     "prefab_production_duration_seconds": prefab_values["production_duration_seconds"],
                     "config_max_health": config_values["max_health"],
                     "config_raw_price": config_values["raw_price"],
                     "config_raw_materials_cost": config_values["raw_materials_cost"],
                     "config_raw_production_duration_seconds": config_values["raw_production_duration_seconds"],
                     "source_prefab_path": entry["prefab_path"], "source_owner_prefab_path": entry["owner_prefab_path"],
                     "source_config_path": entry["config_path"],
                     "prefab_component_file_id": entry["prefab_component_file_id"],
                     "unity_effective_prefab_state_verified": False})
    return {"kind": "S002BuildingDefinitionScalarMetadataInputs", "rows": rows,
            "world_allocated": False, "factory_execution": False, "mission_ready": False,
            "source_scope": "Current source YAML candidate fields only; prefab importer-effective scalar state is unverified."}


def resolve_provider_rows(repo: Path, helper, provider, expected_owner: Path, config_script: Path, key_set=None,
                          guid_cache=None, importer_witness=None):
    if guid_cache is None:
        guid_cache = {}
    owner_guid = script_guid(expected_owner)
    rows = []
    seen = set()
    for ref in provider["rows"]:
        hits = guid_hits_cached(repo, helper, ref["guid"], guid_cache)
        need(len(hits) == 1, "provider prefab GUID must resolve to exactly one current asset: " + ref["guid"])
        meta, target = helper.checked_meta_target(repo / "Assets", hits[0])
        need(target.suffix == ".prefab", "provider prefab GUID resolved to a non-prefab: " + str(target))
        pin_target(repo, meta, target, provider["pins"])
        key = target.stem
        identity = (ref["guid"], ref["file_id"])
        if identity in seen:
            # Match RebuildSpawnablesLookup's first-occurrence identity behavior.
            continue
        seen.add(identity)
        if key_set is not None and key in key_set:
            config = config_assignments(repo, target, owner_guid, config_script, helper, provider["pins"], guid_cache,
                                        importer_witness)
            rows.append({"index": ref["index"], "key": key, "path": str(target.relative_to(repo)),
                         "guid": ref["guid"], "file_id": ref["file_id"],
                         "local_id_validation": local_id_validation(helper, target, ref["file_id"]),
                         "config": config})
        else:
            rows.append({"index": ref["index"], "key": key, "path": str(target.relative_to(repo)),
                         "guid": ref["guid"], "file_id": ref["file_id"],
                         "local_id_validation": local_id_validation(helper, target, ref["file_id"])})
    return rows


def project_setup(repo: Path, graph_path: Path, graph_sha: str, setup_result: dict, pins: dict[str, str],
                  importer_witness=None) -> dict:
    helper = load_module(GENERIC, "s002_setup_join_yaml_helpers")
    graph = json.loads(graph_path.read_text(encoding="utf-8"))
    role_node = next((n for n in graph["nodes"] if n["source_type"] == "SkirmishRoleCatalogConfig"), None)
    need(role_node is not None, "current source graph role catalog required")
    role_rows = role_node["fields"].get("roles")
    need(isinstance(role_rows, list) and role_rows, "ordered current source role catalog rows required")
    role_by_id = {row["RoleId"]: row for row in role_rows}
    need(len(role_by_id) == len(role_rows), "current role catalog IDs must be unique")
    setup = setup_result["setup"]
    need(setup_result.get("kind") == "S002CurrentSetupProviderSourceResult" and setup_result.get("status") == "Compiled" and
         setup.get("catalog_id") == "S002" and setup.get("size_id") == "Standard" and
         setup.get("difficulty_id") == "Regular" and setup.get("seed", -1) >= 0 and
         setup_result.get("source_graph_sha256") == graph_sha,
         "exact current compiled S002 setup result required")
    need(setup_result.get("world_allocated") is False and setup_result.get("geometry_materialized") is False and
         setup_result.get("factory_execution_claimed") is False and setup_result.get("source_fit_verified") is False and
         setup_result.get("mission_ready") is False, "source-only setup scope flags must remain false")
    force_rows, structure_rows = setup_result.get("force_rows"), setup_result.get("structure_rows")
    need(isinstance(force_rows, list) and force_rows and isinstance(structure_rows, list) and structure_rows,
         "compiled ordered forces and structures required")
    for i, force in enumerate(force_rows):
        need(force.get("index") == i, "compiled force order must be contiguous")
        require_force_role_binding(force, role_by_id)
    structure_keys = {row.get("visual_key") for row in structure_rows}
    need(None not in structure_keys and "" not in structure_keys, "every compiled structure needs original VisualKey")
    build_script = repo / BUILDING_OWNER
    unit_script = repo / UNIT_OWNER
    providers = {
        "structures": load_provider_asset(repo, helper, BUILDING_CONFIG, "spawnables",
                                           repo / BUILDING_PROVIDER_SCRIPT),
        "forces": load_provider_asset(repo, helper, UNIT_REGISTRY, "unitSpawnPrefabs",
                                      repo / UNIT_PROVIDER_SCRIPT)
    }
    for provider in providers.values():
        provider["pins"] = {str(provider["path"]): provider["sha256"],
                            str(provider["meta"]): provider["meta_sha256"],
                            str(provider["script_path"]): provider["script_sha256"],
                            str(Path(str(provider["script_path"]) + ".meta")): sha(Path(str(provider["script_path"]) + ".meta").read_bytes())}
        for rel in (BUILDING_OWNER, UNIT_OWNER):
            p = repo / rel
            provider["pins"][str(p)] = sha(p.read_bytes())
            provider["pins"][str(Path(str(p) + ".meta"))] = sha(Path(str(p) + ".meta").read_bytes())
    current_structures = resolve_provider_rows(repo, helper, providers["structures"], build_script,
                                               repo / BUILDING_CONFIG_SCRIPT, structure_keys, {})
    if GROUND_STAGING_KEY in structure_keys:
        validate_ground_staging_absent_from_generic_provider(current_structures)
    current_forces = resolve_provider_rows(repo, helper, providers["forces"], unit_script,
                                           repo / UNIT_CONFIG_SCRIPT,
                                           {row["runtime_prefab_key"] for row in force_rows}, {}, importer_witness)
    ground_staging_binding = (resolve_ground_staging_special_owner(repo, helper, pins)
                              if GROUND_STAGING_KEY in structure_keys else None)
    by_structure = {}
    for row in current_structures:
        if row["key"] in structure_keys:
            by_structure[row["key"]] = row
    by_force = {}
    for row in current_forces:
        if row["key"] in {x["runtime_prefab_key"] for x in force_rows}:
            by_force[row["key"]] = row
    joined_structures = []
    for row in structure_rows:
        if row["visual_key"] == GROUND_STAGING_KEY:
            need(ground_staging_binding is not None,
                 "Compiled GroundStaging structure requires its separate source-owned binding")
            joined_structures.append({**row, "special_source_binding": ground_staging_binding})
        else:
            prefab = require_unique_key(current_structures, row["visual_key"], "BuildingPlacement provider")
            joined_structures.append({**row, "provider_prefab": prefab})
    joined_forces = []
    for row in force_rows:
        prefab = require_unique_key(current_forces, row["runtime_prefab_key"], "UnitPrefabRegistry provider")
        joined_forces.append({**row, "provider_prefab": require_force_config_match(row, prefab)})
    importer_witness_child_keys = [row["key"] for row in current_forces
                                   if row.get("config", {}).get("owner_resolution_basis") ==
                                   "exact_unity_importer_identity_witness"]
    need(importer_witness is None or importer_witness_child_keys,
         "Exact shared base identity witness must be consumed by a current S002 child config")
    all_pins = dict(pins)
    for provider in providers.values():
        all_pins.update(provider["pins"])
    if importer_witness is not None:
        for raw_path, expected_hash in importer_witness["source_pins"].items():
            prior = all_pins.get(raw_path)
            need(prior is None or prior == expected_hash,
                 "importer witness source pin conflicts with complete provider source set: " + raw_path)
            all_pins[raw_path] = expected_hash
        for raw_path, expected_hash in importer_witness["evidence_artifacts"].items():
            prior = all_pins.get(raw_path)
            need(prior is None or prior == expected_hash,
                 "importer witness artifact pin conflicts with complete provider source set: " + raw_path)
            all_pins[raw_path] = expected_hash
    scalar_inputs = build_building_scalar_inputs(repo, helper, joined_structures, all_pins)
    return {"kind": "S002CurrentSetupProviderIdentityProjection", "status": "JoinedWithUnqualifiedPrefabLocalIds"
            if any(row["provider_prefab"]["local_id_validation"] != "root_main_object_proven" for row in joined_structures + joined_forces)
            else "Joined",
            "setup": setup, "force_rows": joined_forces, "structure_rows": joined_structures,
            "provider_sources": {name: {"path": str(p["path"]), "sha256": p["sha256"],
                                         "asset_guid": p["asset_guid"], "ordered_count": len(p["rows"])}
                                 for name, p in providers.items()},
            "source_pins": dict(sorted(all_pins.items())),
            "guid_resolution_scope": "Point-in-time scan of current Assets metadata for each queried GUID; every returned hit is byte-checked, but no complete replayable .meta census is included, so uniqueness is not a durable proof.",
            "guid_uniqueness_replayable": False,
            "prefab_local_id_semantics": "Provider fileIDs are preserved verbatim. YAML anchor presence is reported without asserting root/main-object semantics.",
            "importer_identity_witness": None if importer_witness is None else {
                "used_for_shared_base_component_type_only": True,
                "consumer_prefab_keys": importer_witness_child_keys,
                "scope": "Exact pinned Unit_Veh base component GUID/localID/type only; each child config row and config asset is independently source-parsed/pinned. Does not identify provider prefab fileIDs as root/main objects.",
                "receipt_path": importer_witness["receipt_path"],
                "receipt_sha256": importer_witness["receipt_sha256"],
                "observation_path": importer_witness["observation_path"],
                "observation_sha256": importer_witness["observation_sha256"],
                "target_guid": IMPORTER_TARGET_GUID, "target_file_id": IMPORTER_TARGET_FILE_ID,
                "target_type": IMPORTER_TARGET_TYPE},
            "special_source_bindings": [] if ground_staging_binding is None else [ground_staging_binding],
            "building_metadata_scalar_inputs": scalar_inputs,
            "world_allocated": False, "geometry_materialized": False,
            "metadata_factory_execution": False, "full_initialization": False,
            "source_fit_verified": False, "native_readiness": False, "mission_ready": False,
            "scope": "Current S002 original-compiler roster joined to serialized scene provider prefab references and prefab-attached config assets, plus the separately source-owned GroundStaging visual access path. GroundStaging is not inserted into generic BuildingPlacement spawnables. GUID lookup is point-in-time only, not replayably unique; provider fileIDs are preserved verbatim and any YAML anchor match remains semantically unqualified. No metadata factory execution, runtime initialization, geometry materialization, Unity fallback execution, source-fit or readiness."}


def verify_build_pins(build: dict) -> None:
    pins = build.get("source_and_reference_pins")
    need(isinstance(pins, dict) and pins, "source build complete pin set required")
    for raw, expected in pins.items():
        path = Path(raw)
        need(path.is_file() and not path.is_symlink() and sha(path.read_bytes()) == expected,
             "source build pin changed/missing: " + raw)


def append_source_host_sources(args: list[str], repo: Path) -> list[str]:
    """Compile exact policy source files when their defining assembly is not referenced."""
    policy_sources = [repo / SCALAR_POLICY, repo / UNIT_SCALAR_POLICY]
    need(all(path.is_file() for path in policy_sources),
         "current shared building and unit scalar policy sources required for source-host compile")
    component_refs = []
    for arg in args:
        if not arg.startswith("-r:"):
            continue
        reference = arg[3:].split("=", 1)[-1]
        if Path(reference).name == "Game.Components.dll":
            component_refs.append(reference)
    need(not component_refs,
         "source-host compile must not source-define scalar policy types while referencing Game.Components.dll")
    # Source responses may already contain these shared policies, using either
    # repository-relative or absolute paths. Add only sources not already present.
    existing = {(repo / arg).resolve() for arg in args if not arg.startswith('-')}
    missing = []
    for path in policy_sources:
        matches = [p for p in existing if p.name == path.name or
                   re.fullmatch(r'[0-9]+_' + re.escape(path.name), p.name)]
        need(all(p.is_file() and sha(p.read_bytes()) == sha(path.read_bytes()) for p in matches),
             'source-host scalar policy copy differs from current source: ' + str(path))
        if not matches:
            missing.append(str(path.resolve()))
    return args + [str(BINDING.resolve())] + missing


def source_host_policy_pins(repo: Path) -> dict[str, str]:
    paths = (repo / SCALAR_POLICY, repo / UNIT_SCALAR_POLICY)
    need(all(path.is_file() and not path.is_symlink() for path in paths),
         "exact regular source-host scalar policy source files required")
    return {str(path.resolve()): sha(path.read_bytes()) for path in paths}


def run(build_receipt: Path, graph_path: Path, graph_sha: str, output: Path, seed: int,
        importer_identity_receipt: Path | None = None) -> dict:
    repo = ROOT.resolve(strict=True)
    build_receipt, graph_path = build_receipt.resolve(strict=True), graph_path.resolve(strict=True)
    output = output.absolute()
    need(not output.exists(), "fresh output directory required")
    build = json.loads(build_receipt.read_text())
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("mission") == "S002" and
         build.get("status") == "Passed" and build.get("compile_exit") == 0,
         "fresh passed S002 source-host build receipt required")
    verify_build_pins(build)
    graph = json.loads(graph_path.read_text())
    need(sha(graph_path.read_bytes()) == graph_sha and graph.get("authority") == "AuthoredConfigAssets" and
         graph.get("definition_path") == "Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishScenario_S002.asset" and
         graph.get("compiled_setup") is False and graph.get("world_initialized") is False,
         "exact current S002 authored graph required")
    for raw, expected in graph.get("source_pins", {}).items():
        path = Path(raw)
        need(path.is_file() and sha(path.read_bytes()) == expected, "S002 graph source pin changed: " + raw)
    response_desc = build.get("compiler_response")
    need(isinstance(response_desc, dict), "source compiler response descriptor required")
    response = Path(response_desc["path"]).resolve(strict=True)
    need(sha(response.read_bytes()) == response_desc["sha256"], "source compiler response changed")
    args = [json.loads(line) for line in response.read_text().splitlines() if line.strip()]
    mains = [i for i, arg in enumerate(args) if arg.startswith("-main:")]
    outputs = [i for i, arg in enumerate(args) if arg.startswith("-out:")]
    need(len(mains) == 1 and len(outputs) == 1, "exact source response main/output options required")
    runtime = build["runtime_selection"]
    pins = build["source_and_reference_pins"]
    source_policy_pins_before_compile = source_host_policy_pins(repo)
    dotnet = next((p for p in pins if p.endswith("/dotnet")), None)
    cscs = sorted((p for p in pins if re.search(r"/sdk/[0-9.]+/Roslyn/bincore/csc\.dll$", p)))
    need(dotnet and cscs and Path(dotnet).is_file() and Path(cscs[-1]).is_file(), "pinned Roslyn toolchain unavailable")
    output.mkdir(parents=True)
    args[mains[0]] = "-main:S002CurrentSetupProviderSourceBinding"
    args[outputs[0]] = "-out:" + str(output / "S002CurrentSetupProvider.exe")
    args = append_source_host_sources(args, repo)
    rsp = output / "compile.rsp"
    rsp.write_text("\n".join(json.dumps(arg) for arg in args) + "\n", encoding="utf-8")
    compiled = subprocess.run([dotnet, cscs[-1], "@" + str(rsp)], capture_output=True, text=True, timeout=120)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr, encoding="utf-8")
    verify_build_pins(build)
    need(source_host_policy_pins(repo) == source_policy_pins_before_compile,
         "source-host scalar policy source changed during compilation")
    need(compiled.returncode == 0, "source-only original setup projection compilation failed; inspect compile.log")
    ref_dirs = sorted({str(Path(arg[3:]).resolve().parent) for arg in args
                       if arg.startswith("-r:") and Path(arg[3:]).is_file()})
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join(ref_dirs)
    mono = Path(runtime["mono"])
    need(mono.is_file(), "pinned source-host runtime unavailable")
    invocation = subprocess.run([str(mono), str(output / "S002CurrentSetupProvider.exe"),
                                 str(graph_path), graph_sha, graph["setup_matrix"]["path"], str(seed)],
                                capture_output=True, text=True, timeout=60, env=env, cwd=repo)
    run_log = invocation.stdout + invocation.stderr
    (output / "run.log").write_text(run_log, encoding="utf-8")
    verify_build_pins(build)
    need(sha(graph_path.read_bytes()) == graph_sha, "S002 graph changed during setup compile")
    for raw, expected in graph["source_pins"].items():
        need(Path(raw).is_file() and sha(Path(raw).read_bytes()) == expected,
             "S002 graph pin changed during setup compile: " + raw)
    need(invocation.returncode == 0, "source-only original setup compile failed; inspect run.log")
    raw_lines = [line for line in invocation.stdout.splitlines() if line.strip()]
    need(raw_lines, "original setup compiler produced no result")
    setup_result = json.loads(raw_lines[-1])
    need(setup_result.get("setup", {}).get("seed") == seed,
         "original compiler result seed differs from requested current S002 target")
    pin_map = {raw: value for raw, value in graph["source_pins"].items()}
    importer_witness = (validate_importer_identity_receipt(repo, importer_identity_receipt)
                        if importer_identity_receipt is not None else None)
    projection = project_setup(repo, graph_path, graph_sha, setup_result, pin_map, importer_witness)
    need(all(projection["source_pins"].get(path) == expected
             for path, expected in source_policy_pins_before_compile.items()),
         "source-host compiled scalar policy differs from the later source projection pins")
    need(1 <= len(projection["force_rows"]) <= 128 and 1 <= len(projection["structure_rows"]) <= 128,
         "bounded actual current S002 compiled roster counts required")
    for raw, expected in projection["source_pins"].items():
        path = Path(raw)
        need(path.is_file() and sha(path.read_bytes()) == expected, "source pin changed before receipt: " + raw)

    scalar_inputs_path = output / "building-metadata-scalar-inputs.json"
    scalar_inputs_path.write_text(json.dumps(projection["building_metadata_scalar_inputs"],
                                             ensure_ascii=False, allow_nan=False), encoding="utf-8")
    scalar_invocation = subprocess.run(
        [str(mono), str(output / "S002CurrentSetupProvider.exe"),
         "--project-building-metadata-scalars", str(scalar_inputs_path)],
        capture_output=True, text=True, timeout=60, env=env, cwd=repo)
    scalar_log = scalar_invocation.stdout + scalar_invocation.stderr
    (output / "building-metadata-scalar-policy.log").write_text(scalar_log, encoding="utf-8")
    verify_build_pins(build)
    need(scalar_invocation.returncode == 0,
         "shared source-only building metadata scalar policy failed; inspect building-metadata-scalar-policy.log")
    scalar_lines = [line for line in scalar_invocation.stdout.splitlines() if line.strip()]
    need(len(scalar_lines) == 1, "shared scalar policy must produce exactly one JSON result")
    scalar_result = json.loads(scalar_lines[0])
    need(scalar_result.get("kind") == "S002BuildingDefinitionScalarMetadataSourceProjection" and
         scalar_result.get("status") == "PassedSourceOnlyScalarPolicy" and
         scalar_result.get("policy_controls_passed") is True and
         scalar_result.get("world_allocated") is False and scalar_result.get("factory_execution") is False and
         scalar_result.get("unity_effective_prefab_state_verified") is False and
         scalar_result.get("source_fit_verified") is False and scalar_result.get("mission_ready") is False,
         "shared scalar policy result must retain exact source-only scope")
    expected_keys = {row["key"] for row in projection["building_metadata_scalar_inputs"]["rows"]}
    scalar_rows = scalar_result.get("rows")
    need(isinstance(scalar_rows, list) and {row.get("key") for row in scalar_rows} == expected_keys and
         len(scalar_rows) == len(expected_keys),
         "shared scalar policy must return exactly one result for every source-joined structure key")
    projection["building_definition_scalar_metadata"] = scalar_result

    unit_policy_invocation = subprocess.run(
        [str(mono), str(output / "S002CurrentSetupProvider.exe"),
         "--unit-production-metadata-policy-controls"],
        capture_output=True, text=True, timeout=60, env=env, cwd=repo)
    unit_policy_log = unit_policy_invocation.stdout + unit_policy_invocation.stderr
    (output / "unit-production-metadata-scalar-policy.log").write_text(unit_policy_log, encoding="utf-8")
    verify_build_pins(build)
    unit_policy_marker = (
        "[S002UnitProductionMetadataScalarPolicy] result=Passed scope=SharedScalarPolicyControls "
        "noWorld=true noFactory=true sourceFitVerified=false missionReady=false")
    need(unit_policy_invocation.returncode == 0 and
         unit_policy_invocation.stdout.splitlines().count(unit_policy_marker) == 1,
         "shared unit production metadata scalar policy controls failed; inspect unit-production-metadata-scalar-policy.log")
    for raw, expected in projection["source_pins"].items():
        path = Path(raw)
        need(path.is_file() and sha(path.read_bytes()) == expected,
             "source pin changed before projection receipt: " + raw)
    result_path = output / "provider-projection.json"
    result_path.write_text(json.dumps(projection, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    receipt = {"kind": "S002CurrentSetupProviderProjectionReceipt", "status": projection["status"],
               "setup": projection["setup"], "source_build": {"path": str(build_receipt), "sha256": sha(build_receipt.read_bytes())},
               "source_graph": {"path": str(graph_path), "sha256": graph_sha},
               "harness": {"path": str(BINDING), "sha256": sha(BINDING.read_bytes())},
               "runner": {"path": str(Path(__file__).resolve()), "sha256": sha(Path(__file__).read_bytes())},
               "importer_identity_witness": None if importer_witness is None else {
                   "receipt_path": importer_witness["receipt_path"],
                   "receipt_sha256": importer_witness["receipt_sha256"],
                   "target_guid": IMPORTER_TARGET_GUID, "target_file_id": IMPORTER_TARGET_FILE_ID,
                   "scope": "Exact shared Unit_Veh base component type only; child config links independently parsed."},
               "projection": {"path": str(result_path), "sha256": sha(result_path.read_bytes())},
               "scalar_source_inputs": {"path": str(scalar_inputs_path), "sha256": sha(scalar_inputs_path.read_bytes())},
               "scalar_policy_log_sha256": sha((output / "building-metadata-scalar-policy.log").read_bytes()),
               "unit_scalar_policy_log_sha256": sha((output / "unit-production-metadata-scalar-policy.log").read_bytes()),
               "compile_log_sha256": sha((output / "compile.log").read_bytes()),
               "run_log_sha256": sha((output / "run.log").read_bytes()),
               "structure_count": len(projection["structure_rows"]), "force_count": len(projection["force_rows"]),
               "factory_execution": False, "full_initialization": False, "world_allocated": False,
               "source_fit_verified": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-receipt", type=Path, required=True)
    parser.add_argument("--graph", type=Path, required=True)
    parser.add_argument("--graph-sha256", required=True)
    parser.add_argument("--seed", type=int, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--importer-identity-receipt", type=Path)
    args = parser.parse_args()
    try:
        print(json.dumps(run(args.build_receipt, args.graph, args.graph_sha256, args.output, args.seed,
                             args.importer_identity_receipt), indent=2))
        return 0
    except (ValueError, OSError, KeyError, TypeError, subprocess.SubprocessError, json.JSONDecodeError) as error:
        print("S002 current setup provider projection failed closed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

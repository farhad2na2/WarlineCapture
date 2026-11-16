"""Strict opt-in binding for current S002 source-only diagnostic inputs.

This module does not qualify a mission or replace SourceMetadataCompatibility.
It binds the explicitly opted-in no-compatibility diagnostic inputs to the
current source owner artifacts and retains all underlying source pins.
"""
from __future__ import annotations

import hashlib
import importlib.util
import json
import os
import re
import stat
import tempfile
from pathlib import Path


KIND = "S002CurrentSourceDiagnosticInputPackage"
PACKAGE_PATH = "T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T2/package.json"
PACKAGE_SHA256 = "de74b8fa217fdb515b3bbdbc36c26f8f18761f5b1ffae6156278823a03563628"
SELECTED_MAP_PACKAGE_PATH = "T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T6/package.json"
SELECTED_MAP_GRAPH_PATH = "T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T6/source-config-graph.json"
SELECTED_MAP_GRAPH_PROOF_PATH = "T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T6/graph-preparation-proof.json"
GRID_EXTENDED_PACKAGE_PATH = "T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T12/package.json"
REFRESHED_BASE_PACKAGE_PATH = "T Logs/SkirmishProduction/s002-refreshed-source-diagnostic-input-20261105-T1/package.json"
REFRESHED_GRID_PACKAGE_PATH = "T Logs/SkirmishProduction/s002-refreshed-source-diagnostic-input-20261105-T3/package.json"
REFRESHED_GRID_GRAPH_PATH = "T Logs/SkirmishProduction/s002-refreshed-source-diagnostic-input-20261105-T3/source-config-graph.json"
REFRESHED_GRID_PROOF_PATH = "T Logs/SkirmishProduction/s002-refreshed-source-diagnostic-input-20261105-T3/graph-preparation-proof.json"
GRID_EXTENDED_GRAPH_PATH = "T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T12/source-config-graph.json"
GRID_EXTENDED_GRAPH_PROOF_PATH = "T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T12/graph-preparation-proof.json"
BUILDING_KEYS = [
    "Building_Barrack", "Building_GroundStaging", "Building_OilPump",
    "Building_Refinery", "Building_Fuel_Bladder", "Building_Ammunition_Depot",
    "Building_GuardTower", "Building_Satelite_Dish",
]
UNIT_KEYS = [
    "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Chr_Soldier_Male_01",
    "Unit_Chr_Soldier_Female_01", "Unit_Chr_Soldier_Female_02_Alt_02",
    "Unit_Chr_Ghillie_Male_01", "Unit_Veh_Light_Armored_Car",
    "Unit_Veh_APC_Fast", "Unit_Veh_APC_Slow", "Unit_Veh_Tank_USA",
    "Unit_Veh_Radar_Tank", "Unit_Veh_Truck_Tray", "Unit_Veh_Truck_Tanker",
]
EXPECTED = {
    "building": {
        "artifact": ("Logs/SkirmishProduction/s002-current-building-metadata-20261104-T1/building-definitions.json",
                     "087eec954782c0ee56e9dc3ef538e20a60fe01e8b69e379cc4c85dcfd0070f87"),
        "receipt": ("T Logs/SkirmishProduction/s002-current-building-metadata-20261104-T1/receipt.json",
                    "30e53a279df5ab48024f3a402cec3080628d2ed5b74ec5d4cbbdb0ff0b9b1ccb"),
        "log": ("T Logs/SkirmishProduction/s002-current-building-metadata-20261104-T1/editor.log",
                "4128e126cd752c75e1ba3a5b001befad1cc5d4adfafd1b51ebc27d506ab76418"),
        "marker": "[SkirmishAuthoredBuildingDefinitions] result=Passed definitions=8 unsupported=0 fullMission=0",
        "receipt_scope": "OriginalConfiguredPrefabMetadataFactory_NotMissionState",
        "kind": "AuthoredSkirmishBuildingDefinitions", "count": 8, "pins": 8746,
    },
    "unit": {
        "artifact": ("Logs/SkirmishProduction/s002-current-unit-metadata-20261104-T1/unit-production-metadata.json",
                     "666106d279517caed0360fb3773ee154df5d015431c69d1ed69c1f25a5226094"),
        "receipt": ("T Logs/SkirmishProduction/s002-current-unit-metadata-20261104-T1/receipt.json",
                    "29825b46301018c15888dfefc9df2c75fce1cc188d18db2fc959a8d305668404"),
        "log": ("T Logs/SkirmishProduction/s002-current-unit-metadata-20261104-T1/editor.log",
                "5496f00c865d01a76a881684446c1b57dff1c899416b8f3f9993878054615640"),
        "marker": "[SkirmishAuthoredUnitProductionMetadata] result=Passed units=12 transports=1 unsupported=0 fullMission=0",
        "receipt_scope": "OriginalConfiguredAuthoringFactories_NotMissionState",
        "kind": "AuthoredSkirmishUnitProductionMetadata", "count": 12, "pins": 8756,
    },
}
SUPPLEMENT = {
    "artifact": ("T Logs/SkirmishProduction/s002-current-building-request-supplement-20261104-T1/configured-building-request-metadata.json",
                 "317eb5fca01456c134e161cffe4afaf8f4dd69226f1f9ab214ba0563f1d4538f"),
    "receipt": ("T Logs/SkirmishProduction/s002-current-building-request-supplement-20261104-T1/receipt.json",
                "9faf28bf6b642cf1e6ae588c5cab9fd5e71b350b0136f197ba2d7b1b35d9c837"),
    "provider": ("Logs/SkirmishProduction/s002-current-provider-metadata-20261104-T1/two-seed-metadata.json",
                 "40e514571e1c5cd9585b7e874d3cfd20e06e7a87111bfd881b4538660c595b29"),
    "provider_receipt": ("T Logs/SkirmishProduction/s002-current-provider-metadata-20261104-T1/receipt.json",
                         "9a45e6879d28d7c92e4225ee1c491e21fac979f3db7c8d1f7df19c5dc5a34eb4"),
    "provider_log": ("T Logs/SkirmishProduction/s002-current-provider-metadata-20261104-T1/editor.log",
                     "96a5b8cf612878f8db61d0f9d0d742878bfd1d74454422d3517aa64b04bcb172"),
    "projector": ("Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/project_s002_current_building_request_supplement.py",
                  "209c3d5214173d92f7d33dc88c9253ad241f739340c51a8bee8e82c2f9bc6379"),
}
TEMPLATE_DESCRIPTOR = (
    "T Logs/SkirmishProduction/s002-current-baked-templates-20261104-T5/projection/reference-template-descriptor.json",
    "77ad77989b1f2996834d67dd9c54be400e60670cb760a0f74c23e443e03e0f2c",
)
TEMPLATE_CAPTURE = (
    "T Logs/SkirmishProduction/s002-current-baked-templates-20261104-T1/prefab-reference-data.json",
    "128853445e6ec37eff52fb3080281c8a23ec3a8355dd8ab01a02cc2b62d0ce74",
)
TEMPLATE_FINALIZER = (
    "T Logs/SkirmishProduction/s002-current-baked-templates-20261104-T5/finalization-receipt.json",
    "c70fbae9b38a0b95141b99191422f0d6a71c94d33ed7f67fbe776c699d13fdb5",
)
TEMPLATE_CAPTURE_LOG = (
    "T Logs/SkirmishProduction/s002-current-baked-templates-20261104-T1/editor.log",
    "923b0b5da29518313fd4aeffae8c509461538d06448ed8e25eccacb2fc583880",
)
TEMPLATE_KEY_DESCRIPTOR = (
    "/private/tmp/s002-focused-prefab-keys-20261102-r1.json",
    "f2d08edd3a4e495448d3f4df2ad6608d0180e6428be3d32f4844744dcbf23f09",
)
SOURCE_GRAPH = (
    "T Logs/SkirmishProduction/s002-source-registries-graph-20261104-T3/source-config-graph.json",
    "dcd777c16ccd7371efa49cc47c0b21a7eceafc08eab793ee5994faf93a2b0ffd",
)
SOURCE_GRAPH_PROOF = (
    "T Logs/SkirmishProduction/s002-source-registries-graph-20261104-T3/graph-proof.json",
    "a527c582ea7980ff3a5dd65ffe53f8e2034ed444b0dec79e55fca80ec86b2815",
)
ONE_PRODUCER = {
    "asset_path": "Assets/Game/Resources/OneProducerAuthoredReferencePackage.asset",
    "asset_sha256": "6080ed2770fc0579fadfb76feab9e3a39d9a5dc5f6363e7cbe8622b1dded6e92",
    "json_path": "Assets/Game/Resources/OneProducerAuthoredReferencePackage.json",
    "json_sha256": "f10567806dcdcfbfcfeb7504682e5bb89e81dae41e868ce25c42ee52d08ba0f9",
}
HISTORICAL_PLACEMENT_INITIALIZATION = (
    "Logs/SkirmishProduction/s002-derived-seed-20261103-r2/data/initialization.json",
    "c82539fe56368ce70fb3ecc669647ac75c7d2678e5392354b2d0e2c98cf48eba",
)
HISTORICAL_PLACEMENT_INPUTS = {
    "physical": ("Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json",
                 "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8"),
    "water": ("Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-water-capture.json",
              "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5"),
    "startup": ("Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-startup-observation.json",
                "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03"),
    "relocation": ("Logs/SkirmishProduction/s073-captured-source-preservation-20261101-r1/receipt.json",
                   "68b2a130fe08e3f952b1a8c1c0603b082ec38c7feb8b4581e1153cba7665e297"),
}


def _sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def _merge_pins(target: dict[str, str], new: dict[str, str]):
    for path, digest in new.items():
        prior = target.get(path)
        if prior is not None and prior != digest:
            raise ValueError("Conflicting hashes for consumed S002 input: " + path)
        target[path] = digest


def _pairs(rows):
    result = {}
    for key, value in rows:
        if key in result:
            raise ValueError(f"Duplicate JSON property: {key}")
        result[key] = value
    return result


def _json(raw: bytes):
    return json.loads(raw.decode("utf-8"), object_pairs_hook=_pairs,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Nonfinite JSON: " + value)))


def _canonical(root: Path, relative: str) -> Path:
    if type(relative) is not str or not relative or "\\" in relative:
        raise ValueError("Canonical package path required")
    supplied = Path(relative)
    if supplied.is_absolute():
        if not supplied.is_relative_to(root):
            raise ValueError("Absolute package input must remain inside the current repository")
        parts = supplied.relative_to(root).parts
    else:
        parts = tuple(relative.split("/"))
    if any(part in ("", ".", "..") for part in parts):
        raise ValueError("Canonical repository-relative package path required")
    path = root.joinpath(*parts)
    current = root
    for part in parts:
        current = current / part
        if current.is_symlink():
            raise ValueError("Symlink in package input path: " + str(current))
    resolved = path.resolve(strict=True)
    if not resolved.is_relative_to(root) or str(resolved) != str(path):
        raise ValueError("Package input escaped canonical repository root")
    mode = path.stat().st_mode
    if not stat.S_ISREG(mode):
        raise ValueError("Regular package input file required")
    return path


def _read(root: Path, relative: str, expected: str, limit: int = 64 * 1024 * 1024) -> bytes:
    if type(expected) is not str or not re.fullmatch(r"[0-9a-f]{64}", expected):
        raise ValueError("Lowercase SHA256 required")
    path = _canonical(root, relative)
    raw = path.read_bytes()
    if not 0 < len(raw) <= limit or _sha(raw) != expected:
        raise ValueError("Package input digest or size mismatch: " + relative)
    return raw


def _read_exact_external_pin(path: str, expected_path: str, expected_digest: str,
                             supplied_digest: str, limit: int = 16 * 1024 * 1024) -> bytes:
    if type(path) is not str or path != expected_path or type(expected_digest) is not str or supplied_digest != expected_digest:
        raise ValueError("Exact proof-bound external S002 pin required")
    external = Path(path)
    if not external.is_absolute() or str(external) != path:
        raise ValueError("Canonical absolute external pin path required")
    current = Path(external.anchor)
    for component in external.parts[1:]:
        current = current / component
        if current.is_symlink():
            raise ValueError("Symlink component in external package pin: " + str(current))
    resolved = external.resolve(strict=True)
    if str(resolved) != path:
        raise ValueError("External package pin path is not canonical")
    mode = external.stat().st_mode
    if not stat.S_ISREG(mode):
        raise ValueError("Regular external package pin required")
    raw = external.read_bytes()
    if not 0 < len(raw) <= limit or _sha(raw) != expected_digest:
        raise ValueError("External package pin hash or size mismatch")
    return raw


def _embedded_pins(root: Path, value: dict, expected_count: int) -> dict[str, str]:
    pins = value.get("source_pins")
    if type(pins) is not dict or len(pins) != expected_count:
        raise ValueError("Exact complete current source-pin set required")
    checked = {}
    for relative, digest in pins.items():
        if Path(relative).is_absolute() and relative == TEMPLATE_KEY_DESCRIPTOR[0]:
            _read_exact_external_pin(relative, TEMPLATE_KEY_DESCRIPTOR[0], TEMPLATE_KEY_DESCRIPTOR[1], digest)
            checked[relative] = digest
            continue
        path = _canonical(root, relative)
        raw = path.read_bytes()
        if not 0 < len(raw) <= 128 * 1024 * 1024 or _sha(raw) != digest:
            raise ValueError("Current embedded source pin mismatch: " + relative)
        checked[str(path)] = digest
    return checked


def _template_inputs(root: Path, descriptor: dict, pins: dict[str, str], template_profile=None):
    profile = ({"descriptor": TEMPLATE_DESCRIPTOR, "capture": TEMPLATE_CAPTURE,
                "finalizer": TEMPLATE_FINALIZER, "log": TEMPLATE_CAPTURE_LOG,
                "source_pin_count": 8051} if template_profile is None else template_profile)
    rel, digest = profile["descriptor"]
    if descriptor != {"path": str((root / rel).resolve()), "sha256": digest}:
        raise ValueError("Package template descriptor differs from current qualified S002 T5 descriptor")
    template = _json(_read(root, rel, digest, 4 * 1024 * 1024))
    proof = template.get("current_capture_proof")
    if template.get("capture_validation_status") != "PassedCurrentPinnedSourceOnly" or template.get("consumer_qualification_scope") != "Current S002 exact typed template closure; source pins and capture receipt verified; 14 presentation-only VFX omissions retained; no replay, source-fit, visual, or mission readiness" or any(template.get(key) is not False for key in ("mission_ready", "native_visual_readiness", "replay_ready", "source_fit_ready")):
        raise ValueError("Current S002 template source-only qualifications must remain false")
    if type(proof) is not dict or proof.get("kind") != "S002CurrentFocusedTemplateCaptureProof" or proof.get("source_pin_count") != profile["source_pin_count"] or proof.get("capture_sha256") != profile["capture"][1] or proof.get("finalization_receipt_sha256") != profile["finalizer"][1] or proof.get("capture_log_sha256") != profile["log"][1] or proof.get("key_descriptor_sha256") != TEMPLATE_KEY_DESCRIPTOR[1]:
        raise ValueError("Exact current S002 template proof linkage required")
    capture = _json(_read(root, profile["capture"][0], profile["capture"][1], 256 * 1024 * 1024))
    if capture.get("kind") != "AuthoredSkirmishPrefabReferenceData" or capture.get("full_mission_initialized") is not False or capture.get("native_visual_readiness") is not False or capture.get("requested_prefab_keys_sha256") != TEMPLATE_KEY_DESCRIPTOR[1]:
        raise ValueError("Current S002 focused capture scope/key binding mismatch")
    capture_pins = _embedded_pins(root, capture, profile["source_pin_count"])
    _merge_pins(pins, capture_pins)
    _merge_pins(pins, {str((root / rel).resolve()): digest,
                       str((root / profile["capture"][0]).resolve()): profile["capture"][1],
                       str((root / profile["finalizer"][0]).resolve()): profile["finalizer"][1],
                       str((root / profile["log"][0]).resolve()): profile["log"][1],
                       TEMPLATE_KEY_DESCRIPTOR[0]: TEMPLATE_KEY_DESCRIPTOR[1]})
    finalizer = _json(_read(root, profile["finalizer"][0], profile["finalizer"][1], 2 * 1024 * 1024))
    marker = "[SkirmishS002FocusedPrefabReferenceData] result=Passed prefabs=12 entities=352 unsupportedUnconsumedPresentation=14 replayReady=False fullMission=0"
    capture_log = _read(root, profile["log"][0], profile["log"][1], 64 * 1024 * 1024).decode("utf-8", errors="strict")
    if finalizer.get("validation_status") != "PassedCurrentPinnedSourceOnly" or finalizer.get("capture_sha256") != profile["capture"][1] or finalizer.get("source_pin_count") != profile["source_pin_count"] or finalizer.get("required_marker") != marker or finalizer.get("wrapper_exit_code") != 0 or capture_log.count(marker) != 1:
        raise ValueError("Current S002 focused template finalization/log mismatch")
    if template.get("projection_manifest_path") is None:
        raise ValueError("Current S002 template projection manifest required")
    manifest_path = Path(template["projection_manifest_path"])
    if not manifest_path.is_absolute() or not manifest_path.resolve(strict=True).is_relative_to(root):
        raise ValueError("Current S002 template projection manifest must be in repository")
    manifest_rel = manifest_path.resolve(strict=True).relative_to(root).as_posix()
    manifest_sha = template.get("projection_manifest_sha256")
    manifest = _json(_read(root, manifest_rel, manifest_sha, 4 * 1024 * 1024))
    if manifest.get("source_path") != template.get("source_reference_path") or manifest.get("source_sha256") != template.get("source_reference_sha256") or manifest.get("slice_path") != template.get("path") or manifest.get("slice_sha256") != template.get("sha256"):
        raise ValueError("Current S002 template projection manifest is not bound to descriptor")
    _merge_pins(pins, {str((root / manifest_rel).resolve()): manifest_sha})
    return template


def _source_graph_inputs(root: Path, descriptor: dict, pins: dict[str, str]):
    graph_rel, graph_sha = SOURCE_GRAPH
    if descriptor != {"path": str((root / graph_rel).resolve()), "sha256": graph_sha}:
        raise ValueError("Package source config graph differs from current S002 T3 graph")
    graph = _json(_read(root, graph_rel, graph_sha, 128 * 1024 * 1024))
    proof = _json(_read(root, SOURCE_GRAPH_PROOF[0], SOURCE_GRAPH_PROOF[1], 4 * 1024 * 1024))
    expected_matrix = graph.get("setup_matrix")
    if graph.get("schema_version") != 1 or graph.get("authority") != "AuthoredConfigAssets" or graph.get("scope") != "Current S002 authored config graph from checked-in Unity YAML, including pinned baseline preset and GUID-bound Match scene provider references. Exact serialized config fields and list order only; no setup compilation, geometry generation, runtime metadata, registry admission, World, or readiness." or graph.get("compiled_setup") is not False or graph.get("world_initialized") is not False or graph.get("native_world_state_used") is not False or ("mission_ready" in graph and graph.get("mission_ready") is not False):
        raise ValueError("Current S002 graph must remain descriptor-only and uncompiled")
    if proof.get("kind") != "S002CurrentAuthoredConfigGraph" or proof.get("status") != "Passed" or proof.get("graph") != descriptor or proof.get("source_pin_count") != 336 or proof.get("compiled_setup") is not False or proof.get("world_initialized") is not False or proof.get("mission_ready") is not False:
        raise ValueError("Current S002 graph proof/false-scope binding mismatch")
    _merge_pins(pins, _embedded_pins(root, graph, 336))
    _merge_pins(pins, {str((root / graph_rel).resolve()): graph_sha,
                       str((root / SOURCE_GRAPH_PROOF[0]).resolve()): SOURCE_GRAPH_PROOF[1]})
    matrix_path = Path(expected_matrix.get("path", "")) if type(expected_matrix) is dict else Path()
    matrix_sha = expected_matrix.get("sha256") if type(expected_matrix) is dict else None
    if not matrix_path.is_absolute() or not matrix_path.resolve(strict=True).is_relative_to(root) or type(matrix_sha) is not str:
        raise ValueError("Current graph-bound setup matrix descriptor required")
    relative = matrix_path.resolve(strict=True).relative_to(root).as_posix()
    _read(root, relative, matrix_sha, 4 * 1024 * 1024)
    _merge_pins(pins, {str((root / relative).resolve()): matrix_sha})
    return graph, {"path": str((root / relative).resolve()), "sha256": matrix_sha}


def _supplement(root: Path, supplement: dict, pins: dict[str, str], definitions: dict,
                expected_profiles=None, supplement_profile=None,
                provider_pin_count=8756, consumer_pin_count=8692):
    expected_profiles = EXPECTED if expected_profiles is None else expected_profiles
    supplement_profile = SUPPLEMENT if supplement_profile is None else supplement_profile
    art_rel, art_sha = supplement_profile["artifact"]
    receipt_rel, receipt_sha = supplement_profile["receipt"]
    expected_desc = {
        "path": ("artifact", supplement_profile["artifact"]), "receipt_path": ("receipt", supplement_profile["receipt"]),
        "provider_artifact_path": ("provider", supplement_profile["provider"]),
        "provider_receipt_path": ("provider_receipt", supplement_profile["provider_receipt"]),
        "provider_log_path": ("provider_log", supplement_profile["provider_log"]),
        "definitions_receipt_path": ("definitions_receipt", expected_profiles["building"]["receipt"]),
        "definitions_log_path": ("definitions_log", expected_profiles["building"]["log"]),
        "projector_path": ("projector", supplement_profile["projector"]),
    }
    if type(supplement) is not dict or set(supplement) != {"path", "sha256", "receipt_path", "receipt_sha256", "provider_artifact_path", "provider_artifact_sha256", "provider_receipt_path", "provider_receipt_sha256", "provider_log_path", "provider_log_sha256", "definitions_receipt_path", "definitions_receipt_sha256", "definitions_log_path", "definitions_log_sha256", "projector_path", "projector_sha256"}:
        raise ValueError("Exact current building-request supplement evidence descriptors required")
    for path_key, (name, (relative, digest)) in expected_desc.items():
        if supplement[path_key] != str((root / relative).resolve()) or supplement[path_key.replace("path", "sha256")] != digest:
            raise ValueError("Current building-request supplement descriptor mismatch: " + path_key)
    artifact = _json(_read(root, art_rel, art_sha, 64 * 1024 * 1024))
    receipt = _json(_read(root, receipt_rel, receipt_sha, 4 * 1024 * 1024))
    provider = _json(_read(root, supplement_profile["provider"][0], supplement_profile["provider"][1], 64 * 1024 * 1024))
    if artifact.get("kind") != "CurrentSourceBuildingRequestMetadataSupplement" or artifact.get("schema_version") != 1 or artifact.get("scope") != "CurrentOriginalS002ConfiguredProvidersAndRequestability_MetadataOnly" or artifact.get("reference") != {"path": str((root / expected_profiles["building"]["artifact"][0]).resolve()), "sha256": expected_profiles["building"]["artifact"][1]} or any(artifact.get(k) is not False for k in ("mission_ready", "full_mission_initialized", "native_visual_readiness", "runtime_state_transferred")):
        raise ValueError("Current building-request supplement scope/reference mismatch")
    provider_contexts = provider.get("contexts")
    if provider.get("kind") != "NativeTwoSeedMetadataQualification" or type(provider_contexts) is not list or len(provider_contexts) != 2:
        raise ValueError("Current original two-seed provider artifact required")
    current = provider_contexts[0]
    if (current.get("catalog_id"), current.get("difficulty"), current.get("size"), current.get("seed"), current.get("setup_hash")) != ("S002", "Regular", "Standard", 104731, 3657809016):
        raise ValueError("Current S002 provider context identity mismatch")
    provider_buildings = current.get("metadata", {}).get("buildings")
    if type(provider_buildings) is not list or len(provider_buildings) != 8:
        raise ValueError("Current source provider building rows required")
    receipt_fields = {
        "status": "Passed", "scope": "CurrentOriginalS002ConfiguredProvidersAndRequestability_MetadataOnly",
        "wrapper_exit_code": 0, "artifact_sha256": art_sha, "source_pins_checked": provider_pin_count + expected_profiles["building"]["pins"],
        "consumer_source_pins_emitted": consumer_pin_count, "mission_ready": False,
        "full_mission_initialized": False, "native_visual_readiness": False,
        "runtime_state_transferred": False,
        "provider_receipt_sha256": supplement_profile["provider_receipt"][1],
        "provider_artifact_sha256": supplement_profile["provider"][1],
        "provider_log_sha256": supplement_profile["provider_log"][1],
        "definitions_receipt_sha256": expected_profiles["building"]["receipt"][1],
        "definitions_artifact_sha256": expected_profiles["building"]["artifact"][1],
        "definitions_log_sha256": expected_profiles["building"]["log"][1],
        "projector_sha256": supplement_profile["projector"][1],
    }
    if any(receipt.get(key) != value for key, value in receipt_fields.items()):
        raise ValueError("Current building-request supplement receipt binding mismatch")
    if type(artifact.get("rows")) is not list or len(artifact["rows"]) != len(BUILDING_KEYS):
        raise ValueError("Exact current source building-request row set required")
    definition_rows = definitions["definitions"]
    seen = set()
    for row in artifact["rows"]:
        index = row.get("definition_index")
        key = row.get("key")
        if type(index) is not int or not 0 <= index < len(definition_rows) or type(key) is not str or key in seen:
            raise ValueError("Unique definition index/key join required")
        seen.add(key)
        source_row = next((item for item in definition_rows if item.get("key") == key), None)
        provider_row = next((item for item in provider_buildings if item.get("index") == index), None)
        if source_row is None or source_row.get("prefab") != row.get("prefab") or provider_row is None or provider_row.get("prefab") != row.get("prefab") or provider_row.get("index") != index or provider_row.get("configured_entry", {}).get("fields", {}).get("Prefab") != row.get("prefab"):
            raise ValueError("Building-request row must join exact definition key+prefab identity")
        provider_entry = provider_row["configured_entry"]["fields"]
        if (type(row.get("can_request")) is not bool or row.get("can_request") != provider_entry.get("CanRequest", {}).get("value") or
                row.get("display_name") != source_row.get("fields", {}).get("DisplayName", {}).get("value") or
                row.get("display_name") != provider_entry.get("DisplayName", {}).get("value") or
                row.get("construction_catalog_index") != (None if provider_row.get("construction_index") == -1 else provider_row.get("construction_index")) or
                row.get("scene_catalog_index") != (None if provider_row.get("scene_index") == -1 else provider_row.get("scene_index"))):
            raise ValueError("Building-request row scalar differs from its source definition")
    if seen != set(BUILDING_KEYS):
        raise ValueError("Complete S002 building-request identity set required")
    source_pins = artifact.get("source_pins")
    if type(source_pins) is not dict or len(source_pins) != consumer_pin_count:
        raise ValueError("Exact supplement consumer source pin set required")
    for path, digest in source_pins.items():
        _read(root, path, digest, 128 * 1024 * 1024)
    join = [{"key": row["key"], "guid": row["prefab"]["guid"], "local_id": row["prefab"]["local_id"]} for row in artifact["rows"]]
    join_sha = _sha(json.dumps(join, separators=(",", ":"), ensure_ascii=False).encode("utf-8"))
    if receipt.get("ordered_key_identity_join_kind") != "schema1_rows_order_key_plus_source_prefab_guid_local_id" or receipt.get("ordered_key_identity_join") != join or receipt.get("ordered_key_identity_join_sha256") != join_sha:
        raise ValueError("Supplement ordered key+source-prefab identity join mismatch")
    for name, (relative, digest) in (("artifact", supplement_profile["artifact"]), ("receipt", supplement_profile["receipt"]), ("provider", supplement_profile["provider"]), ("provider_receipt", supplement_profile["provider_receipt"]), ("provider_log", supplement_profile["provider_log"]), ("projector", supplement_profile["projector"])):
        _read(root, relative, digest, 256 * 1024 * 1024)
        _merge_pins(pins, {str((root / relative).resolve()): digest})
    _merge_pins(pins, _embedded_pins(root, provider, provider_pin_count))
    _merge_pins(pins, {str((root / path).resolve()): digest for path, digest in source_pins.items()})
    log = _read(root, supplement_profile["provider_log"][0], supplement_profile["provider_log"][1]).decode("utf-8", errors="strict")
    if log.count("[SkirmishTwoSeedMetadata] result=Passed contexts=2 negatives=8 scope=MetadataOnly") != 1:
        raise ValueError("Current S002 provider owner pass marker missing/duplicated")
    return artifact, {"path": str((root / art_rel).resolve()), "sha256": art_sha}


def _owner_artifact(root: Path, name: str, pins: dict[str, str], expected_profiles=None):
    expected = (EXPECTED if expected_profiles is None else expected_profiles)[name]
    artifact_rel, artifact_sha = expected["artifact"]
    receipt_rel, receipt_sha = expected["receipt"]
    log_rel, log_sha = expected["log"]
    artifact = _json(_read(root, artifact_rel, artifact_sha, 128 * 1024 * 1024))
    receipt = _json(_read(root, receipt_rel, receipt_sha, 2 * 1024 * 1024))
    log = _read(root, log_rel, log_sha, 128 * 1024 * 1024).decode("utf-8", errors="strict")
    if (artifact.get("kind") != expected["kind"] or artifact.get("schema_version") != 1 or
            artifact.get("catalog_id", "S002") != "S002" or artifact.get("seed") != 104731 or
            artifact.get("setup_hash") != 3657809016 or artifact.get("unsupported_values") != [] or
            artifact.get("full_mission_initialized") is not False or
            artifact.get("native_visual_readiness") is not False):
        raise ValueError("Current S002 owner artifact identity/scope mismatch: " + name)
    rows = artifact.get("definitions" if name == "building" else "units")
    row_keys = [row.get("key") for row in rows] if type(rows) is list else None
    expected_keys = BUILDING_KEYS if name == "building" else UNIT_KEYS
    if row_keys != expected_keys:
        raise ValueError("Current S002 owner row order/identity mismatch: " + name)
    if receipt.get("status") != "Passed" or receipt.get("wrapper_exit_code") != 0 or receipt.get("marker_count") != 1 or receipt.get("scope") != expected["receipt_scope"] or receipt.get("artifact_sha256") != artifact_sha or receipt.get("log_sha256") != log_sha or receipt.get("source_pins_checked") != expected["pins"] or receipt.get("mission_ready") is not False:
        raise ValueError("Current S002 owner receipt does not bind passed artifact/log: " + name)
    if log.count(expected["marker"]) != 1:
        raise ValueError("Current S002 owner pass marker missing/duplicated: " + name)
    _merge_pins(pins, _embedded_pins(root, artifact, expected["pins"]))
    _merge_pins(pins, {str((root / relative).resolve()): digest for relative, digest in
                       ((artifact_rel, artifact_sha), (receipt_rel, receipt_sha), (log_rel, log_sha))})
    return {"path": str((root / artifact_rel).resolve()), "sha256": artifact_sha}, artifact


def _one_producer(root: Path, pins: dict[str, str]) -> dict:
    for key, relative in (("asset_sha256", ONE_PRODUCER["asset_path"]), ("json_sha256", ONE_PRODUCER["json_path"])):
        current = _sha(_read(root, relative, ONE_PRODUCER[key], 1024 * 1024))
        if current != ONE_PRODUCER[key]:
            raise ValueError("Current generic one-producer package changed")
    data = _json(_read(root, ONE_PRODUCER["json_path"], ONE_PRODUCER["json_sha256"], 1024 * 1024))
    if data.get("kind") != "OneProducerAuthoredReferencePackage" or data.get("scope") != "GenericAuthoredReferenceOnly":
        raise ValueError("Exact generic one-producer package required")
    paths, hashes = data.get("sourcePinPaths"), data.get("sourcePinSha256")
    if type(paths) is not list or type(hashes) is not list or len(paths) != len(hashes) or len(set(paths)) != len(paths):
        raise ValueError("Complete generic one-producer package source pins required")
    for relative, digest in zip(paths, hashes):
        _read(root, relative, digest, 128 * 1024 * 1024)
        _merge_pins(pins, {str((root / relative).resolve()): digest})
    _merge_pins(pins, {str((root / ONE_PRODUCER["asset_path"]).resolve()): ONE_PRODUCER["asset_sha256"],
                       str((root / ONE_PRODUCER["json_path"]).resolve()): ONE_PRODUCER["json_sha256"]})
    return dict(ONE_PRODUCER)


def load_current_s002_package(path: str | Path, expected_sha256: str, repository_root: str | Path):
    root = Path(repository_root).resolve(strict=True)
    supplied_package_path = Path(path)
    if not supplied_package_path.is_absolute():
        raise ValueError("Absolute current S002 diagnostic package path required")
    if not supplied_package_path.is_relative_to(root):
        raise ValueError("Current S002 package must be a regular in-repository file")
    package_path = _canonical(root, supplied_package_path.relative_to(root).as_posix())
    relative_package = package_path.relative_to(root).as_posix()
    if relative_package == REFRESHED_BASE_PACKAGE_PATH:
        return _load_refreshed_base_package(package_path, expected_sha256, root)
    if relative_package == REFRESHED_GRID_PACKAGE_PATH:
        return _load_grid_extended_package(package_path, expected_sha256, root, refreshed=True)
    if relative_package == SELECTED_MAP_PACKAGE_PATH:
        return _load_selected_map_package(package_path, expected_sha256, root)
    if relative_package == GRID_EXTENDED_PACKAGE_PATH:
        return _load_grid_extended_package(package_path, expected_sha256, root)
    if package_path.relative_to(root).as_posix() != PACKAGE_PATH:
        raise ValueError("Only the exact current S002 diagnostic package path is admitted")
    raw = package_path.read_bytes()
    if expected_sha256 != PACKAGE_SHA256 or _sha(raw) != expected_sha256:
        raise ValueError("Current S002 package file digest mismatch")
    package = _json(raw)
    fields = {"schema_version", "kind", "scope", "mission", "seed", "setup_hash", "source_config_graph", "setup_matrix", "building_definitions", "unit_production_metadata", "reference_templates", "authored_reference_package", "building_request_metadata", "native_context_qualified", "source_fit_certified", "mission_ready"}
    if type(package) is not dict or set(package) != fields or package.get("schema_version") != 1 or package.get("kind") != KIND or package.get("scope") != "CurrentS002OwnerInputs_SourceHostDiagnosticOnly" or (package.get("mission"), package.get("seed"), package.get("setup_hash")) != ("S002", 104731, 3657809016) or any(package.get(k) is not False for k in ("native_context_qualified", "source_fit_certified", "mission_ready")):
        raise ValueError("Exact current S002 diagnostic package schema/identity/scope required")
    pins = {str(package_path): expected_sha256}
    building_desc, building = _owner_artifact(root, "building", pins)
    unit_desc, units = _owner_artifact(root, "unit", pins)
    if package["building_definitions"] != building_desc or package["unit_production_metadata"] != unit_desc:
        raise ValueError("Package owner artifact descriptors differ from current passed T1 outputs")
    _graph, setup_matrix = _source_graph_inputs(root, package["source_config_graph"], pins)
    if package["setup_matrix"] != setup_matrix:
        raise ValueError("Package setup matrix does not match current S002 graph")
    template_rel, template_sha = TEMPLATE_DESCRIPTOR
    template = _template_inputs(root, package["reference_templates"], pins)
    one = _one_producer(root, pins)
    if package["authored_reference_package"] != one:
        raise ValueError("Package must reference the current generic one-producer package")
    supplement = package["building_request_metadata"]
    expected_supplement = {"path": str((root / SUPPLEMENT["artifact"][0]).resolve()), "sha256": SUPPLEMENT["artifact"][1]}
    if type(supplement) is not dict or set(supplement) != {"path", "sha256", "receipt_path", "receipt_sha256", "provider_artifact_path", "provider_artifact_sha256", "provider_receipt_path", "provider_receipt_sha256", "provider_log_path", "provider_log_sha256", "definitions_receipt_path", "definitions_receipt_sha256", "definitions_log_path", "definitions_log_sha256", "projector_path", "projector_sha256"} or {"path": supplement.get("path"), "sha256": supplement.get("sha256")} != expected_supplement:
        raise ValueError("Exact current building-request supplement evidence descriptors required")
    expected_extra = {
        "receipt_path": ("receipt", "receipt"), "provider_artifact_path": ("provider", "artifact"),
        "provider_receipt_path": ("provider_receipt", "receipt"), "provider_log_path": ("provider_log", "log"),
        "definitions_receipt_path": ("building", "receipt"), "definitions_log_path": ("building", "log"),
        "projector_path": ("projector", "projector"),
    }
    for path_key, (group, member) in expected_extra.items():
        rel, digest = (EXPECTED[group][member] if group in EXPECTED else SUPPLEMENT[group])
        if supplement[path_key] != str((root / rel).resolve()) or supplement[path_key.replace("path", "sha256")] != digest:
            raise ValueError("Current supplement evidence descriptor mismatch: " + path_key)
    _supplement(root, supplement, pins, building)
    return {"path": str(package_path), "sha256": expected_sha256, "repository_root": str(root), "value": package,
            "building_definitions": building_desc, "unit_production_metadata": unit_desc,
            "source_config_graph": package["source_config_graph"], "setup_matrix": setup_matrix,
            "reference_templates": {"path": str((root / template_rel).resolve()), "sha256": template_sha},
            "building_request_metadata": supplement, "authored_reference_package": one,
            "source_pins": pins, "building_rows": building["definitions"], "unit_rows": units["units"]}


# Fresh captures are a separate diagnostic epoch. These are the exact artifacts
# produced by the checked 2026-11-05 wrappers and projector; the old profile
# and its historical evidence remain unchanged.
def _refreshed_s002_evidence(root: Path):
    building = dict(EXPECTED['building'],
        artifact=('Logs/SkirmishProduction/s002-building-metadata-refresh-20261105-T1/building-definitions.json', '34cba42316efc035342284bd63ff39656fa0ec14b64a0cb1684e17584bc6e9ec'),
        receipt=('T Logs/SkirmishProduction/s002-building-metadata-refresh-20261105-T1/projector-compatible-receipt.json', '9f109b02fe084fe2aa44044761d282b32e996dd545f76fd4db844202de1a3917'),
        log=('T Logs/SkirmishProduction/s002-building-metadata-refresh-20261105-T1/editor.log', '9ef23f1c89d8b53715c53a41df6be5ed82269f5ca97376602170197e3d135770'), pins=8772)
    unit = dict(EXPECTED['unit'],
        artifact=('Logs/SkirmishProduction/s002-unit-metadata-refresh-20261105-T1/unit-production-metadata.json', '314ed33d642c2dae7f1d6da6f3dd719962673e7e1c3998ffa86a945f07b8a5d6'),
        receipt=('T Logs/SkirmishProduction/s002-unit-metadata-refresh-20261105-T1/package-compatible-receipt.json', '3a68d753c48937438f6430e8ea68997802215dae25e8e57aa9af2daa77334211'),
        log=('T Logs/SkirmishProduction/s002-unit-metadata-refresh-20261105-T1/editor.log', '10136063fed49d392c287ba68d622655fc5e78bfff379b81718c28ba2b50f2e0'), pins=8782)
    profiles = {'building': building, 'unit': unit}
    supplement_profile = {
        'artifact': ('T Logs/SkirmishProduction/s002-building-request-supplement-refresh-20261105-T3/configured-building-request-metadata.json', 'f5f65211e87604a15b0c4de855b78575a62a303f550fbd66ef696260df550c8d'),
        'receipt': ('T Logs/SkirmishProduction/s002-building-request-supplement-refresh-20261105-T3/receipt.json', 'a74abca5d896fa3caf862f16bd4f6a5f96c8ace2cf06af6c2e47c25d12823bb6'),
        'provider': ('Logs/SkirmishProduction/s002-provider-metadata-refresh-20261105-T1/two-seed-metadata.json', '1351a5bebb34f319b862c815dada9e04a149a6719bd26dd14dcb7e5448c682f5'),
        'provider_receipt': ('T Logs/SkirmishProduction/s002-provider-metadata-refresh-20261105-T1/receipt.json', '0a3656d1a5cf355edf8ab8c57228ace51b474f88598c1d5b4865a75bd43b0ff3'),
        'provider_log': ('T Logs/SkirmishProduction/s002-provider-metadata-refresh-20261105-T1/editor.log', 'bd13e62e253e4c4ba258328580b98487406ca058a458e2791ca5b4315ea2ad72'),
        'projector': ('Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/project_s002_current_building_request_supplement.py', '7daa5b4d71a49d370303d349c20da0dc7e3aed302cb8e9db83294ba3fcbc9dec'),
    }
    template_profile = {
        'descriptor': ('T Logs/SkirmishProduction/s002-focused-templates-refresh-20261105-T1/projection-T2/reference-template-descriptor.json', 'f290b339dba278a1581299eac0180de77e916cdc29917ddd5f4e8d44c669f3be'),
        'capture': ('T Logs/SkirmishProduction/s002-focused-templates-refresh-20261105-T1/durable-prefab-reference-data.json', 'ac7ced091de35fdd937bab0f89d61fe8063017aeeaf275e19c8d0a08198b2aa9'),
        'finalizer': ('T Logs/SkirmishProduction/s002-focused-templates-refresh-20261105-T1/finalization-T4-receipt.json', 'ecaa4764ed94f3a25c833300312a725ebe5668ab9c25de1345a79f0d875c9684'),
        'log': ('T Logs/SkirmishProduction/s002-focused-templates-refresh-20261105-T1/editor.log', '83f893d818ab873af589f92ba5604903f930869cdbb306ec273f91c3cc1f35cc'),
        'source_pin_count': 8077,
    }
    pins = {}
    building_desc, building_rows = _owner_artifact(root, 'building', pins, profiles)
    unit_desc, unit_rows = _owner_artifact(root, 'unit', pins, profiles)
    base_raw = _read(root, PACKAGE_PATH, PACKAGE_SHA256, 4 * 1024 * 1024)
    base = _json(base_raw)
    if (base.get('kind') != KIND or base.get('scope') != 'CurrentS002OwnerInputs_SourceHostDiagnosticOnly' or
            (base.get('mission'), base.get('seed'), base.get('setup_hash')) != ('S002', 104731, 3657809016) or
            any(base.get(k) is not False for k in ('native_context_qualified', 'source_fit_certified', 'mission_ready'))):
        raise ValueError('Exact historical diagnostic package identity required')
    _graph, matrix = _source_graph_inputs(root, base['source_config_graph'], pins)
    if base['setup_matrix'] != matrix or base['authored_reference_package'] != _one_producer(root, pins):
        raise ValueError('Current source graph and authored one-producer identity required')
    template_desc = {'path': str((root / template_profile['descriptor'][0]).resolve()),
                     'sha256': template_profile['descriptor'][1]}
    _template_inputs(root, template_desc, pins, template_profile)
    supplemental = {}
    for key, source in (
        ('path', supplement_profile['artifact']), ('receipt_path', supplement_profile['receipt']),
        ('provider_artifact_path', supplement_profile['provider']), ('provider_receipt_path', supplement_profile['provider_receipt']),
        ('provider_log_path', supplement_profile['provider_log']), ('definitions_receipt_path', building['receipt']),
        ('definitions_log_path', building['log']), ('projector_path', supplement_profile['projector'])):
        supplemental[key] = str((root / source[0]).resolve())
        supplemental[key.replace('path', 'sha256')] = source[1]
    _supplement(root, supplemental, pins, building_rows, profiles, supplement_profile, 8782, 8718)
    value = dict(base)
    value.update(building_definitions=building_desc, unit_production_metadata=unit_desc,
                 reference_templates=template_desc, building_request_metadata=supplemental)
    _merge_pins(pins, {str((root / PACKAGE_PATH).resolve()): PACKAGE_SHA256})
    return {'value': value, 'source_pins': pins, 'building_rows': building_rows['definitions'],
            'unit_rows': unit_rows['units']}


def _load_refreshed_base_package(package_path: Path, expected_sha256: str, root: Path):
    evidence = _refreshed_s002_evidence(root)
    expected_raw = (json.dumps(evidence['value'], indent=2, ensure_ascii=False) + '\n').encode('utf-8')
    raw = package_path.read_bytes()
    actual = _sha(raw)
    if raw != expected_raw or expected_sha256 != actual:
        raise ValueError('Refreshed package differs from current validated evidence')
    pins = dict(evidence['source_pins'])
    _merge_pins(pins, {str(package_path): actual})
    value = evidence['value']
    return {'path': str(package_path), 'sha256': actual, 'repository_root': str(root), 'value': value,
            'building_definitions': value['building_definitions'], 'unit_production_metadata': value['unit_production_metadata'],
            'source_config_graph': value['source_config_graph'], 'setup_matrix': value['setup_matrix'],
            'reference_templates': value['reference_templates'], 'building_request_metadata': value['building_request_metadata'],
            'authored_reference_package': value['authored_reference_package'], 'source_pins': pins,
            'building_rows': evidence['building_rows'], 'unit_rows': evidence['unit_rows']}


def _load_selected_map_package(package_path: Path, expected_sha256: str, root: Path):
    """Reproduce the historical T6 selected-map graph without the later grid extension."""
    actual_candidate_sha = _sha(package_path.read_bytes())
    if expected_sha256 != actual_candidate_sha:
        raise ValueError("T6 package file digest mismatch")
    base_path = _canonical(root, PACKAGE_PATH)
    base = load_current_s002_package(base_path, PACKAGE_SHA256, root)
    generator_path = _canonical(
        root, "Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/prepare_inputs.py")
    generator_sha = _sha(generator_path.read_bytes())
    spec = importlib.util.spec_from_file_location("s002_historical_selected_map_graph_owner", generator_path)
    if spec is None or spec.loader is None:
        raise ValueError("Current selected-map source graph owner required")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    with tempfile.TemporaryDirectory(prefix="s002-selected-map-package-replay-") as temporary:
        output = Path(temporary) / "generated"
        preparation = module.generate(root, output, root,
                                      _canonical(root, "T Logs/SkirmishProduction/s002-source-registries-graph-20261104-T3/source-config-graph.json"), None)
        graph_raw = (output / "source-config-graph.json").read_bytes()
        extraction = _json((output / "source-extraction.json").read_bytes())
    extraction_outputs = extraction.get("outputs")
    if type(extraction_outputs) is not dict or not extraction_outputs:
        raise ValueError("Selected-map producer outputs required")
    normalized_outputs = {Path(name).name: digest for name, digest in extraction_outputs.items()}
    if len(normalized_outputs) != len(extraction_outputs):
        raise ValueError("Unique selected-map producer output names required")
    extraction["outputs"] = dict(sorted(normalized_outputs.items()))
    producer_proof_sha = _sha(json.dumps(extraction, sort_keys=True, separators=(",", ":")).encode("utf-8"))
    base_graph_path = _canonical(root, "T Logs/SkirmishProduction/s002-source-registries-graph-20261104-T3/source-config-graph.json")
    base_graph_proof_path = _canonical(root, "T Logs/SkirmishProduction/s002-source-registries-graph-20261104-T3/graph-proof.json")
    base_graph = _json(base_graph_path.read_bytes())
    graph_sha = _sha(graph_raw)
    expected_source_pins = dict(base_graph["source_pins"])
    _merge_pins(expected_source_pins, preparation["generator_inputs"])
    expected_proof = {
        "schema_version": 1,
        "kind": "S002CurrentSelectedMapGraphPreparationProof",
        "status": "Passed",
        "base_graph": {"path": str(base_graph_path), "sha256": _sha(base_graph_path.read_bytes())},
        "base_graph_proof": {"path": str(base_graph_proof_path), "sha256": _sha(base_graph_proof_path.read_bytes())},
        "graph": {"path": str(_canonical(root, SELECTED_MAP_GRAPH_PATH)), "sha256": graph_sha},
        "producer_proof_sha256": producer_proof_sha,
        "producer_outputs": dict(sorted(normalized_outputs.items())),
        "producer_output_location_policy": "Temporary producer destination omitted; exact output basenames and content hashes are retained; graph destination is separately fixed and byte-hashed.",
        "producer_path": "Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/prepare_inputs.py",
        "producer_sha256": generator_sha,
        "source_pins": dict(sorted(expected_source_pins.items())),
        "world_initialized": False,
        "mission_ready": False,
    }
    if _sha(generator_path.read_bytes()) != generator_sha:
        raise ValueError("Selected-map package attestation generator changed during reproduction")
    expected_package_path = _canonical(root, SELECTED_MAP_PACKAGE_PATH)
    expected_graph_path = _canonical(root, SELECTED_MAP_GRAPH_PATH)
    expected_proof_path = _canonical(root, SELECTED_MAP_GRAPH_PROOF_PATH)
    if package_path != expected_package_path:
        raise ValueError("Exact canonical T6 package output path required")
    actual_graph_raw = expected_graph_path.read_bytes()
    actual_proof_raw = expected_proof_path.read_bytes()
    actual_proof = _json(actual_proof_raw)
    if actual_graph_raw != graph_raw or actual_proof != expected_proof:
        raise ValueError("T6 selected-map graph/proof differs from the original current-source owner")
    value = dict(base["value"])
    value["source_config_graph"] = {
        "path": str(expected_graph_path), "sha256": graph_sha}
    expected_raw = (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    actual_raw = package_path.read_bytes()
    actual_sha = _sha(actual_raw)
    if expected_sha256 != actual_sha or actual_raw != expected_raw:
        raise ValueError("T6 package does not exactly reproduce from strict current evidence and selected-map owner")
    pins = dict(base["source_pins"])
    for path, digest in expected_proof["source_pins"].items():
        prior = pins.get(path)
        if prior is not None and prior != digest:
            raise ValueError("T6 selected-map source pin conflicts with strict T2 evidence: " + path)
        pins[path] = digest
    for path, digest in ((expected_package_path, actual_sha), (expected_graph_path, graph_sha),
                         (expected_proof_path, _sha(expected_proof_path.read_bytes()))):
        pins[str(path)] = digest
    return {**base, "path": str(expected_package_path), "sha256": actual_sha,
            "value": value, "source_config_graph": value["source_config_graph"],
            "source_pins": pins}


def _load_grid_extended_package(package_path: Path, expected_sha256: str, root: Path, refreshed: bool = False):
    """Reproduce the selected epoch from strict owner evidence and the grid/map graph producer."""
    actual_candidate_sha = _sha(package_path.read_bytes())
    if expected_sha256 != actual_candidate_sha:
        raise ValueError("T12 package file digest mismatch")
    base_path = _canonical(root, REFRESHED_BASE_PACKAGE_PATH if refreshed else PACKAGE_PATH)
    base_sha = _sha(base_path.read_bytes()) if refreshed else PACKAGE_SHA256
    base = load_current_s002_package(base_path, base_sha, root)
    generator_path = _canonical(
        root, "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/generate_s002_selected_map_graph_attestation.py")
    generator_sha = _sha(generator_path.read_bytes())
    spec = importlib.util.spec_from_file_location("s002_current_grid_extended_package_owner", generator_path)
    if spec is None or spec.loader is None:
        raise ValueError("Current grid-extended graph/package owner required")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    with tempfile.TemporaryDirectory(prefix="s002-grid-package-replay-") as temporary:
        generated = (module.generate(root, Path(temporary) / "generated", refreshed=True) if refreshed
                     else module.generate(root, Path(temporary) / "generated"))
    if _sha(generator_path.read_bytes()) != generator_sha:
        raise ValueError("Grid-extended package owner changed during reproduction")
    expected_package_path = _canonical(root, REFRESHED_GRID_PACKAGE_PATH if refreshed else GRID_EXTENDED_PACKAGE_PATH)
    expected_graph_path = _canonical(root, REFRESHED_GRID_GRAPH_PATH if refreshed else GRID_EXTENDED_GRAPH_PATH)
    expected_proof_path = _canonical(root, REFRESHED_GRID_PROOF_PATH if refreshed else GRID_EXTENDED_GRAPH_PROOF_PATH)
    if package_path != expected_package_path or generated["derived_package_path"] != str(expected_package_path):
        raise ValueError("Exact canonical T12 package output path required")
    value = dict(base["value"])
    value["source_config_graph"] = {"path": str(expected_graph_path),
                                    "sha256": generated["enriched_graph_sha256"]}
    expected_raw = (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    actual_raw = package_path.read_bytes()
    actual_sha = _sha(actual_raw)
    if (expected_sha256 != actual_sha or generated["derived_package_sha256"] != actual_sha or
            actual_raw != expected_raw or _sha(expected_graph_path.read_bytes()) != generated["enriched_graph_sha256"]):
        raise ValueError("T12 package/graph does not reproduce from strict current evidence and graph owner")
    proof_raw = expected_proof_path.read_bytes()
    if generated["source_pins"].get(str(expected_proof_path)) != _sha(proof_raw):
        raise ValueError("T12 graph preparation proof changed during reproduction")
    pins = dict(base["source_pins"])
    for path, digest in generated["source_pins"].items():
        prior = pins.get(path)
        if prior is not None and prior != digest:
            raise ValueError("T12 graph source pin conflicts with strict T2 evidence: " + path)
        pins[path] = digest
    for path, digest in ((expected_package_path, actual_sha),
                         (expected_graph_path, generated["enriched_graph_sha256"]),
                         (expected_proof_path, _sha(proof_raw))):
        pins[str(path)] = digest
    return {**base, "path": str(expected_package_path), "sha256": actual_sha,
            "value": value, "source_config_graph": value["source_config_graph"],
            "source_pins": pins}


def verify_current_s002_package_inputs(source_pins: dict[str, str]):
    for path, expected in source_pins.items():
        raw = Path(path).read_bytes()
        if _sha(raw) != expected:
            raise ValueError("Current S002 diagnostic input changed during run: " + path)


def load_s002_historical_placement_binding(repository_root: str | Path) -> dict:
    """Read the exact historical placement descriptor and bind every referenced input byte."""
    root = Path(repository_root).resolve(strict=True)
    init_relative, init_sha = HISTORICAL_PLACEMENT_INITIALIZATION
    init_raw = _read(root, init_relative, init_sha, 8 * 1024 * 1024)
    init = _json(init_raw)
    placement = init.get("placement_domain")
    descriptor = placement.get("s002_original_owner_diagnostic") if type(placement) is dict else None
    if type(descriptor) is not dict or set(descriptor) != set(HISTORICAL_PLACEMENT_INPUTS):
        raise ValueError("Exact historical original-owner placement descriptor required")
    source_pins = {str((root / init_relative).resolve()): init_sha}
    runtime_descriptor = {"kind": "S002HistoricalInputOnlyDiagnostic"}
    for name, (relative, digest) in HISTORICAL_PLACEMENT_INPUTS.items():
        path = _canonical(root, relative)
        if descriptor.get(name) != {"path": str(path.resolve()), "sha256": digest}:
            raise ValueError("Historical placement descriptor mismatch: " + name)
        raw = _read(root, relative, digest, 512 * 1024 * 1024)
        source_pins[str(path.resolve())] = _sha(raw)
        request_name = "preservation" if name == "relocation" else name
        runtime_descriptor[request_name] = {"path": str(path.resolve()), "sha256": digest}
    return {
        "source_initialization": {"path": str((root / init_relative).resolve()), "sha256": init_sha},
        "placement_descriptor": descriptor,
        "runtime_descriptor": runtime_descriptor,
        "source_pins": source_pins,
    }


def make_s002_historical_placement_derivation(binding: dict) -> dict:
    return {
        "kind": "S002VerifiedHistoricalPlacementDescriptorGraft",
        "source_initialization": binding["source_initialization"],
        "source_pins": binding["source_pins"],
        "preserved_base_placement_key": "s002_diagnostic_capture",
        "added_placement_key": "s002_original_owner_diagnostic",
        "native_context_qualified": False,
        "current_source_equivalence": False,
        "physical_source_fit": False,
        "mission_ready": False,
    }


def require_s002_historical_placement_derivation(initial: dict, base: dict, binding: dict,
                                                 derivation: dict):
    placement = initial.get("placement_domain") if type(initial) is dict else None
    base_placement = base.get("placement_domain") if type(base) is dict else None
    if type(placement) is not dict or type(base_placement) is not dict or "s002_diagnostic_capture" not in base_placement:
        raise ValueError("Original T3 diagnostic placement entry is required")
    if placement.get("s002_diagnostic_capture") != base_placement["s002_diagnostic_capture"]:
        raise ValueError("Historical placement graft changed the original T3 placement entry")
    if placement.get("s002_original_owner_diagnostic") != binding.get("placement_descriptor"):
        raise ValueError("Derived initialization omitted or mismatched the verified placement descriptor")
    expected_derivation = make_s002_historical_placement_derivation(binding)
    if derivation != expected_derivation:
        raise ValueError("Historical placement derivation provenance is absent or mismatched")
    return True


def derive_current_s002_initialization(base: dict, base_path: str, base_sha256: str,
                                        loaded: dict, historical_placement_binding: dict | None = None) -> tuple[dict, dict]:
    if type(base) is not dict or base.get("kind") != "GeneratedMissionInitialization" or base.get("mission_id") != "S002":
        raise ValueError("Exact retained S002 base initialization required")
    selection = base.get("selection")
    if type(selection) is not dict or (selection.get("difficulty_id"), selection.get("size_id")) != (2, 1):
        raise ValueError("S002 current-source package is bounded to Regular/Standard")
    base_path_obj = Path(base_path)
    expected_base = Path(loaded["repository_root"]) / "T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T3/base-initialization-r10.json"
    if (not base_path_obj.is_absolute() or str(base_path_obj) != str(expected_base) or
            base_sha256 != "832fd44c042898b282c9c8c532a5d9904a2c4660554183710da1d4dd96d3dc63" or
            not base_path_obj.is_file() or _sha(base_path_obj.read_bytes()) != base_sha256):
        raise ValueError("Exact retained S002 base initialization path/bytes required")
    package = loaded["value"]
    removed = [key for key in ("metadata_compatibility", "metadata_source_origin", "s002_derived_seed") if key in base]
    derived = json.loads(json.dumps(base))
    for key in removed:
        derived.pop(key, None)
    derived["selection"]["seed"] = package["seed"]
    derived["source_config_graph"] = package["source_config_graph"]
    derived["setup_matrix"] = package["setup_matrix"]
    derived["building_definitions"] = package["building_definitions"]
    derived["unit_production_metadata"] = package["unit_production_metadata"]
    derived["reference_templates"] = package["reference_templates"]
    derived["building_request_metadata"] = {
        "path": package["building_request_metadata"]["path"],
        "sha256": package["building_request_metadata"]["sha256"],
    }
    historical_derivation = None
    if historical_placement_binding is not None:
        actual_binding = load_s002_historical_placement_binding(loaded["repository_root"])
        if historical_placement_binding != actual_binding:
            raise ValueError("Caller historical placement binding differs from exact current source")
        placement_domain = derived.get("placement_domain")
        if type(placement_domain) is not dict or "s002_diagnostic_capture" not in placement_domain:
            raise ValueError("Exact T3 base placement capture entry must be preserved")
        existing = placement_domain.get("s002_original_owner_diagnostic")
        if existing is not None and existing != actual_binding["placement_descriptor"]:
            raise ValueError("Conflicting original-owner placement descriptor in base initialization")
        placement_domain["s002_original_owner_diagnostic"] = actual_binding["placement_descriptor"]
        derived["placement_domain"] = placement_domain
        historical_derivation = make_s002_historical_placement_derivation(actual_binding)
        require_s002_historical_placement_derivation(derived, base, actual_binding, historical_derivation)
    provenance = {
        "schema_version": 1,
        "kind": "S002CurrentSourceOwnerInputsDiagnosticDerivation",
        "scope": "CurrentOwnerInputs_WithHistoricalPhysicalDiagnosticOnly",
        "package": {"path": loaded["path"], "sha256": loaded["sha256"]},
        "base_initialization": {"path": str(Path(base_path).resolve(strict=True)), "sha256": base_sha256},
        "base_original_locator": {"path": "/private/tmp/s002-delivery-source-run-20261102-r10/data/initialization.json", "sha256": base_sha256, "reopened": False},
        "removed_metadata_fields": removed,
        "target": {"mission": "S002", "seed": package["seed"], "setup_hash": package["setup_hash"]},
        "native_context_qualified": False,
        "source_fit_certified": False,
        "mission_ready": False,
    }
    derived["s002_current_source_diagnostic"] = provenance
    graph_sha = package["source_config_graph"]["sha256"]
    epoch_payload = "S002-current-owner-source-diagnostic-v1|{}|{}|{}|{}|{}".format(
        loaded["sha256"], base_sha256, graph_sha, package["seed"], package["setup_hash"])
    derived["source_epoch"] = "S002-current-owner-source-diagnostic-v1:" + _sha(epoch_payload.encode("utf-8"))
    if "metadata_compatibility" in derived or "metadata_source_origin" in derived or "s002_derived_seed" in derived:
        raise ValueError("Current-source package derivation did not select the explicit no-compatibility lane")
    return derived, provenance


def package_value(repository_root: str | Path) -> dict:
    root = Path(repository_root).resolve(strict=True)
    pins: dict[str, str] = {}
    building_desc, building = _owner_artifact(root, "building", pins)
    unit_desc, _units = _owner_artifact(root, "unit", pins)
    graph_desc = {"path": str((root / SOURCE_GRAPH[0]).resolve()), "sha256": SOURCE_GRAPH[1]}
    _graph, setup_matrix = _source_graph_inputs(root, graph_desc, pins)
    template_desc = {"path": str((root / TEMPLATE_DESCRIPTOR[0]).resolve()), "sha256": TEMPLATE_DESCRIPTOR[1]}
    _template_inputs(root, template_desc, pins)
    authored = _one_producer(root, pins)
    supplement = {
        "path": str((root / SUPPLEMENT["artifact"][0]).resolve()), "sha256": SUPPLEMENT["artifact"][1],
        "receipt_path": str((root / SUPPLEMENT["receipt"][0]).resolve()), "receipt_sha256": SUPPLEMENT["receipt"][1],
        "provider_artifact_path": str((root / SUPPLEMENT["provider"][0]).resolve()), "provider_artifact_sha256": SUPPLEMENT["provider"][1],
        "provider_receipt_path": str((root / SUPPLEMENT["provider_receipt"][0]).resolve()), "provider_receipt_sha256": SUPPLEMENT["provider_receipt"][1],
        "provider_log_path": str((root / SUPPLEMENT["provider_log"][0]).resolve()), "provider_log_sha256": SUPPLEMENT["provider_log"][1],
        "definitions_receipt_path": str((root / EXPECTED["building"]["receipt"][0]).resolve()), "definitions_receipt_sha256": EXPECTED["building"]["receipt"][1],
        "definitions_log_path": str((root / EXPECTED["building"]["log"][0]).resolve()), "definitions_log_sha256": EXPECTED["building"]["log"][1],
        "projector_path": str((root / SUPPLEMENT["projector"][0]).resolve()), "projector_sha256": SUPPLEMENT["projector"][1],
    }
    _supplement(root, supplement, pins, building)
    return {
        "schema_version": 1, "kind": KIND,
        "scope": "CurrentS002OwnerInputs_SourceHostDiagnosticOnly",
        "mission": "S002", "seed": 104731, "setup_hash": 3657809016,
        "source_config_graph": graph_desc, "setup_matrix": setup_matrix,
        "building_definitions": building_desc,
        "unit_production_metadata": unit_desc,
        "reference_templates": template_desc,
        "authored_reference_package": authored,
        "building_request_metadata": supplement,
        "native_context_qualified": False, "source_fit_certified": False, "mission_ready": False,
    }


def write_package(repository_root: str | Path, output_path: str | Path) -> dict:
    root = Path(repository_root).resolve(strict=True)
    output = Path(output_path)
    if not output.is_absolute() or not output.is_relative_to(root):
        raise ValueError("Current S002 package output must be an absolute in-repository path")
    if output.exists() or output.is_symlink():
        raise ValueError("Fresh current S002 package output required")
    _canonical(root, output.relative_to(root).as_posix()) if output.parent.exists() else None
    value = package_value(root)
    output.parent.mkdir(parents=True, exist_ok=True)
    raw = (json.dumps(value, sort_keys=True, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    with output.open("xb") as stream:
        stream.write(raw)
    digest = _sha(raw)
    # Re-open through the same strict reader used by the launcher.
    load_current_s002_package(output, digest, root)
    return {"path": str(output), "sha256": digest, "bytes": len(raw), "kind": KIND,
            "scope": value["scope"], "mission_ready": False, "source_fit_certified": False}


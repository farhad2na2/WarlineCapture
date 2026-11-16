"""Validate and compare same-session S073 pre-spawn and Playing water captures."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import stat


IDENTITY = "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900"
CONTENT = "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c"
MAP_ID = "opmap.skirmish.industrial_basin"
PROVIDER = "Game.Rendering.RenderBoundsPresentation+Reader"
ENCODING = "uint8-bool01-row-major-xfast"
CELL_COUNT = 2048 * 1024
STARTUP_KIND = "S073NativeStartupWaterMaskObservation"
STARTUP_SCOPE = "OriginalPreparingPhasePlacementCallback_CachedRoadOwnerWaterMask_NotReadinessOrMissionAcceptance"
STARTUP_DIAGNOSTIC_KIND = "S073NativeStartupWaterMaskDiagnostic"
STARTUP_AUTHORITY = "S002PhysicalStartupObservation.OriginalPlacementFacts.Roads.GetWater"
PLAYING_KIND = "S073NativeWaterMaskObservation"
PLAYING_SCOPE = "SameSessionOriginalSkirmishWaterPlacementCreateMask_NotReadinessOrMissionAcceptance"
PLAYING_AUTHORITY = "OriginalSkirmishWaterPlacement.CreateMask_OriginalRenderBoundsGatewayProvider"

STARTUP_PINS = {
    "Assets/Game/Scripts/Editor/Skirmish/S073NativeStartupWaterMaskSnapshot.cs",
    "Assets/Game/Scripts/Systems/S002PhysicalStartupObservation.cs",
    "Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellCacheCompositionSystemHelper.RoadValidation.cs",
    "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs",
    "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredBuildingCache.cs",
    "Assets/Game/Scripts/Systems/SkirmishWaterPlacement.cs",
    "Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs",
    "Assets/Game/Scripts/Rendering/Contracts/IRenderBoundsPresentation.cs",
    "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
    "Assets/Game/Scenes/Match.unity",
    "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity",
}
PLAYING_PINS = {
    "Assets/Game/Scripts/Editor/Skirmish/S073NativeWaterMaskSnapshot.cs",
    "Assets/Game/Scripts/Components/GridComponents.cs",
    "Assets/Game/Scripts/Components/InitialUnitsSpawnComponents.cs",
    "Assets/Game/Scripts/Components/OperationMapBuildingComponents.cs",
    "Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs",
    "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs",
    "Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs",
    "Assets/Game/Scripts/Systems/GridOccupancyKernel.cs",
    "Assets/Game/Scripts/Systems/SkirmishWaterPlacement.cs",
    "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs",
    "Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs",
    "Assets/Game/Scripts/Rendering/Contracts/IRenderBoundsPresentation.cs",
    "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
    "Assets/Game/Scenes/Match.unity",
    "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity",
    "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity.meta",
    "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity",
    "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity.meta",
}
CAMPAIGN_SCENE_PATH = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity"
CAMPAIGN_SCENE_GUID = "c00140f2e94a04c3084c8dcb0c18cbd0"
COMMON_PINS = STARTUP_PINS & PLAYING_PINS
STARTUP_MARKER = re.compile(
    r"^\[S073NativeStartupWaterMask\] result=Recorded frame=(\d+) "
    r"cells=(\d+) waterCells=(\d+) path=(\S+)$"
)
STARTUP_DIAGNOSTIC_MARKER = re.compile(
    r"^\[S073NativeStartupWaterMask\] result=NotObserved reason=(\S+) path=(\S+)$"
)
PLAYING_MARKER = re.compile(
    r"^\[S073NativeWaterMask\] result=Recorded cells=(\d+) waterCells=(\d+) path=(\S+)$"
)
SOURCE_FIT_MARKER = re.compile(
    r"^\[S073LoadedCampaignSourceFit\] result=Captured session=([0-9a-f]{32}) "
    r"sceneGuid=([0-9a-f]{32}) spawnPlanAvailable=([01])$"
)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def _no_duplicate_keys(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property: " + key)
        result[key] = value
    return result


def _read_json(path):
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=_no_duplicate_keys)


def _regular_file(path, label):
    path = Path(path)
    try:
        mode = path.lstat().st_mode
    except FileNotFoundError as error:
        raise ValueError(label + " is missing") from error
    if not stat.S_ISREG(mode):
        raise ValueError(label + " must be an original regular file")
    return path.resolve(strict=True)


def _digest(value):
    return isinstance(value, str) and re.fullmatch(r"[0-9a-f]{64}", value) is not None


def _validate_grid(data, label):
    grid = data.get("grid")
    origin = grid.get("origin") if isinstance(grid, dict) else None
    if (not isinstance(grid, dict) or grid.get("width") != 2048 or grid.get("height") != 1024 or
            type(grid.get("cell_size")) not in (int, float) or isinstance(grid.get("cell_size"), bool) or
            grid.get("cell_size") != 1 or not isinstance(origin, list) or len(origin) != 3 or
            any(type(value) not in (int, float) or isinstance(value, bool) for value in origin) or
            origin != [0, 0, 0]):
        raise ValueError(label + " exact S073 2048x1024 one-meter origin-zero grid required")
    return grid


def _validate_pins(repo, pins, expected, label):
    if not isinstance(pins, dict) or set(pins) != expected:
        raise ValueError(label + " exact source pin set required")
    for relative, digest in pins.items():
        if not _digest(digest):
            raise ValueError(label + " malformed source pin: " + relative)
        path = (repo / relative).resolve(strict=True)
        if not path.is_relative_to(repo) or not path.is_file() or sha(path.read_bytes()) != digest:
            raise ValueError(label + " source pin changed: " + relative)
    return pins


def _validate_loaded_source_fit(repo, data, pins, receipt=None):
    fit = data.get("loaded_campaign_source_fit_observation")
    blockers = fit.get("blocker_contributors") if isinstance(fit, dict) else None
    buildings = fit.get("building_entities") if isinstance(fit, dict) else None
    completeness = fit.get("capture_completeness") if isinstance(fit, dict) else None
    scene = (repo / CAMPAIGN_SCENE_PATH).resolve(strict=True)
    meta = Path(str(scene) + ".meta")
    if (not isinstance(fit, dict) or fit.get("kind") != "S073SameSessionLoadedCampaignSourceFitObservation" or
            fit.get("scope") != "SamePlayingWorld_LoadedCampaignSceneOwnerAndSpawnInputs_DiagnosticOnly" or
            fit.get("session_id") != data.get("session_id") or fit.get("phase") != "Playing" or
            fit.get("world_name") != data.get("world_name") or fit.get("world_sequence") != data.get("world_sequence") or
            fit.get("logical_map_id") != MAP_ID or fit.get("campaign_source_map_id") != "opmap.skirmish.desert_base_01" or
            fit.get("source_identity_hash") != IDENTITY or fit.get("source_content_hash") != CONTENT or
            fit.get("source_scene_guid") != CAMPAIGN_SCENE_GUID or fit.get("source_scene_path") != CAMPAIGN_SCENE_PATH or
            fit.get("scene_loaded") is not True or fit.get("grid_owned_by_campaign_scene") is not True or
            fit.get("source_scene_sha256") != pins.get(CAMPAIGN_SCENE_PATH) or
            fit.get("source_scene_meta_sha256") != pins.get(CAMPAIGN_SCENE_PATH + ".meta") or
            fit.get("source_scene_sha256") != sha(scene.read_bytes()) or
            fit.get("source_scene_meta_sha256") != sha(meta.read_bytes()) or
            "guid: " + CAMPAIGN_SCENE_GUID not in meta.read_text(encoding="utf-8") or
            fit.get("s073_config_sha256") != pins.get("Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset") or
            fit.get("blocker_owner_source_sha256") != pins.get("Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs") or
            not isinstance(blockers, list) or fit.get("blocker_contributor_query_count") != len(blockers) or
            fit.get("blocker_contributors_owned_by_campaign_scene") != sum(row.get("bound_campaign_scene_owned") is True for row in blockers) or
            not isinstance(buildings, list) or fit.get("building_owner_query_count") != len(buildings) or
            fit.get("building_entities_owned_by_campaign_scene") != sum(row.get("bound_campaign_scene_owned") is True for row in buildings) or
            not isinstance(fit.get("original_initial_unit_spawn_cells"), list) or
            not isinstance(fit.get("original_initial_building_spawn_inputs"), list) or
            type(fit.get("original_initial_spawn_plan_available")) is not bool or
            not isinstance(completeness, dict) or completeness.get("loaded_campaign_scene") is not True or
            completeness.get("active_grid_scene_ownership") is not True or
            completeness.get("blocker_owner_inputs") is not (len(blockers) > 0) or
            completeness.get("building_owner_inputs") is not (len(buildings) > 0) or
            completeness.get("original_initial_spawn_plan") is not fit.get("original_initial_spawn_plan_available") or
            fit.get("source_fit_certified") is not False or fit.get("physical_source_admission_changed") is not False or
            fit.get("scheduler_admitted") is not False or fit.get("mission_ready") is not False or
            fit.get("player_readiness") is not False or
            (receipt is not None and receipt.get("loaded_campaign_source_fit_observation") != fit)):
        raise ValueError("Playing observation same actual world/session with exact loaded Campaign source-fit evidence and scene pins required")
    return fit


def _validate_capture(repo, data, mask, observation_path, mask_path, pins, kind, scope, authority, label):
    session = data.get("session_id")
    if (data.get("kind") != kind or data.get("schema_version") != 1 or
            data.get("scope") != scope or data.get("authority") != authority or
            data.get("unity_version") != "6000.5.8f1" or
            data.get("scenario_index") != 3 or data.get("seed") != 104729 or
            not isinstance(session, str) or re.fullmatch(r"[0-9a-f]{32}", session) is None or
            not isinstance(data.get("world_name"), str) or not data["world_name"] or
            not isinstance(data.get("world_sequence"), str) or not data["world_sequence"] or
            data.get("operation_map_id") != MAP_ID or data.get("source_identity_hash") != IDENTITY or
            data.get("source_content_hash") != CONTENT or data.get("physical_source_validated") is not True):
        raise ValueError(label + " exact actual S073 world/session/map identity required")
    if kind == PLAYING_KIND and data.get("phase") != "Playing":
        raise ValueError("Playing observation must be from the actual Playing phase")
    if kind == PLAYING_KIND:
        _validate_loaded_source_fit(repo, data, pins)
    if kind == STARTUP_KIND and data.get("phase") not in ("Preparing", "Playing"):
        raise ValueError("Pre-spawn observation must retain its actual startup phase")

    grid = _validate_grid(data, label)
    if data.get("mask_path") != str(mask_path) or data.get("mask_encoding") != ENCODING or \
            data.get("mask_cells") != CELL_COUNT or len(mask) != CELL_COUNT or any(value not in (0, 1) for value in mask):
        raise ValueError(label + " original complete bool01 mask path/encoding required")
    mask_hash = sha(mask)
    water_cells = sum(mask)
    if data.get("mask_sha256") != mask_hash or type(data.get("water_cells")) is not int or data["water_cells"] != water_cells:
        raise ValueError(label + " original mask hash or water-cell count mismatch")

    provider = data.get("provider_type")
    provider_path = data.get("provider_assembly_path")
    provider_hash = data.get("provider_assembly_sha256")
    if not isinstance(provider, str) or not provider.startswith(PROVIDER + ",") or \
            not isinstance(provider_path, str) or not Path(provider_path).is_absolute() or not _digest(provider_hash):
        raise ValueError(label + " original RenderBounds water provider identity required")
    assembly = _regular_file(provider_path, label + " provider assembly")
    if sha(assembly.read_bytes()) != provider_hash:
        raise ValueError(label + " provider assembly hash mismatch")

    false_fields = ("campaign_gameplay_state_copied", "mission_ready", "source_fit_certified",
                    "normal_input_playthrough", "player_readiness")
    if any(data.get(field) is not False for field in false_fields):
        raise ValueError(label + " explicit non-readiness limits required")
    return {
        "data": data,
        "grid": grid,
        "mask": mask,
        "mask_hash": mask_hash,
        "water_cells": water_cells,
        "observation_path": observation_path,
        "observation_sha256": sha(observation_path.read_bytes()),
        "mask_path": mask_path,
        "source_pins": pins,
        "provider_path": str(assembly),
        "provider_sha256": provider_hash,
    }


def validate(repo, startup_observation, startup_mask, playing_directory, wrapper_exit,
             editor_log=None, startup_receipt=None, playing_observation_path=None, playing_mask=None):
    """Validate both original captures and record exact parity or mismatch.

    A mismatch is a valid comparison result. It is never promoted to a capture
    validation failure or readiness/source-fit claim.
    """
    repo = Path(repo).resolve(strict=True)
    playing_directory = Path(playing_directory).resolve(strict=True)
    if type(wrapper_exit) is not int or wrapper_exit != 0:
        raise ValueError("Checked Unity wrapper did not exit successfully")

    startup_observation = _regular_file(startup_observation, "Startup observation")
    playing_observation = _regular_file(playing_observation_path or playing_directory / "snapshot.json", "Playing observation")
    playing_mask_path = _regular_file(playing_mask or playing_directory / "snapshot.water.bin", "Playing water mask")
    log_path = _regular_file(editor_log or playing_directory / "editor.log", "Shared Editor log")
    startup_data = _read_json(startup_observation)
    playing_data = _read_json(playing_observation)
    log_bytes = log_path.read_bytes()
    log = log_bytes.decode("utf-8", errors="strict")

    playing_markers = [PLAYING_MARKER.fullmatch(line) for line in log.splitlines()
                       if line.startswith("[S073NativeWaterMask]")]
    if len(playing_markers) != 1 or playing_markers[0] is None:
        raise ValueError("One exact successful S073 Playing water marker required")

    playing_cells, playing_water, playing_marker_path = playing_markers[0].groups()
    playing_pins = _validate_pins(repo, playing_data.get("source_pins_sha256"), PLAYING_PINS, "Playing")
    source_fit = _validate_loaded_source_fit(repo, playing_data, playing_pins)

    playing_receipt_path = _regular_file(playing_directory / "water-receipt.json", "Playing capture receipt")
    playing_receipt = _read_json(playing_receipt_path)
    if (playing_receipt.get("kind") != "S073NativeWaterMaskCaptureReceipt" or
            playing_receipt.get("validation_status") != "Passed" or
            playing_receipt.get("wrapper_exit_code") != 0 or
            playing_receipt.get("required_marker_present") is not True or
            (playing_receipt.get("observation_sha256") != sha(playing_observation.read_bytes()) and
             playing_receipt.get("original_observation_sha256") != sha(playing_observation.read_bytes())) or
            playing_receipt.get("mask_sha256") != sha(playing_mask_path.read_bytes()) or
            playing_receipt.get("editor_log_sha256") != sha(log_bytes) or
            playing_receipt.get("loaded_campaign_source_fit_observation") != source_fit):
        raise ValueError("Playing capture receipt does not bind the shared log and original capture paths/hashes")

    fit_markers = [SOURCE_FIT_MARKER.fullmatch(line) for line in log.splitlines()
                   if line.startswith("[S073LoadedCampaignSourceFit]")]
    if (len(fit_markers) != 1 or fit_markers[0] is None or
            fit_markers[0].group(1) != playing_data.get("session_id") or
            fit_markers[0].group(2) != source_fit.get("source_scene_guid") or
            fit_markers[0].group(3) != ("1" if source_fit.get("original_initial_spawn_plan_available") else "0")):
        raise ValueError("One exact same-session loaded Campaign source-fit marker required")

    original_playing_observation = _regular_file(
        playing_receipt.get("original_observation_path", playing_observation),
        "Original Playing observation")
    if Path(playing_marker_path).resolve(strict=True) != original_playing_observation:
        raise ValueError("Playing marker must identify the original Playing observation path")
    original_playing_data = _read_json(original_playing_observation)
    if playing_receipt.get("original_observation_sha256", sha(original_playing_observation.read_bytes())) != \
            sha(original_playing_observation.read_bytes()):
        raise ValueError("Playing receipt does not bind the original Editor observation hash")
    normalized_observation = _regular_file(
        playing_receipt.get("normalized_observation_path", playing_observation),
        "Normalized Playing observation")
    normalized_data = _read_json(normalized_observation)
    if playing_receipt.get("normalized_observation_sha256", sha(normalized_observation.read_bytes())) != \
            sha(normalized_observation.read_bytes()):
        raise ValueError("Playing receipt does not bind the normalized observation hash")
    normalized_projection = json.loads(json.dumps(original_playing_data))
    original_passive = normalized_projection.get("playing_passive_input_observation")
    normalized_passive = normalized_data.get("playing_passive_input_observation")
    original_mask_path = _regular_file(original_playing_data.get("mask_path", ""), "Original Playing mask")
    normalized_mask_path = _regular_file(normalized_data.get("mask_path", ""), "Normalized Playing mask")
    if (original_mask_path != Path(original_playing_observation).with_suffix(".water.bin").resolve(strict=True) or
            normalized_mask_path != (playing_directory / "snapshot.water.bin").resolve(strict=True) or
            sha(original_mask_path.read_bytes()) != playing_receipt.get("mask_sha256") or
            sha(normalized_mask_path.read_bytes()) != playing_receipt.get("mask_sha256")):
        raise ValueError("Playing mask relocation must preserve the exact captured bytes and paths")
    normalized_projection["mask_path"] = str(normalized_mask_path)
    if isinstance(original_passive, dict) and isinstance(normalized_passive, dict):
        original_raw = (original_passive.get("raw_blocker_occupancy_snapshot") or {}).get("raw_path")
        normalized_raw = (normalized_passive.get("raw_blocker_occupancy_snapshot") or {}).get("raw_path")
        if original_raw != normalized_raw:
            original_raw_path = _regular_file(original_raw, "Original Playing raw blocker snapshot")
            normalized_raw_path = _regular_file(normalized_raw, "Normalized Playing raw blocker snapshot")
            original_raw_receipt = original_passive["raw_blocker_occupancy_snapshot"]
            normalized_raw_receipt = normalized_passive["raw_blocker_occupancy_snapshot"]
            if (original_raw_path != Path(original_playing_observation).with_suffix(".blocker-occupancy.bin").resolve(strict=True) or
                    normalized_raw_path != (playing_directory / "blocker-occupancy.bin").resolve(strict=True) or
                    sha(original_raw_path.read_bytes()) != original_raw_receipt.get("raw_sha256") or
                    sha(normalized_raw_path.read_bytes()) != normalized_raw_receipt.get("raw_sha256") or
                    original_raw_receipt.get("raw_sha256") != normalized_raw_receipt.get("raw_sha256")):
                raise ValueError("Playing raw blocker relocation must preserve the exact captured bytes and paths")
            original_passive["raw_blocker_occupancy_snapshot"]["raw_path"] = normalized_raw
    elif original_passive is not None or normalized_passive is not None:
        raise ValueError("Playing raw blocker observation must be present in both original and normalized snapshots")
    if normalized_projection != normalized_data:
        raise ValueError("Finalized Playing snapshot differs from the original observation beyond raw-artifact relocation")
    playing_data = original_playing_data

    playing_capture = _validate_capture(repo, playing_data, original_mask_path.read_bytes(), original_playing_observation,
                                        original_mask_path, playing_pins, PLAYING_KIND, PLAYING_SCOPE,
                                        PLAYING_AUTHORITY, "Playing")

    startup_pins = _validate_pins(repo, startup_data.get("source_pins_sha256"), STARTUP_PINS, "Startup")
    for relative in COMMON_PINS:
        if startup_pins[relative] != playing_pins[relative]:
            raise ValueError("Startup/Playing source pin differs: " + relative)

    for field in ("world_name", "world_sequence", "session_id", "unity_version", "scenario_index", "seed",
                  "operation_map_id", "source_identity_hash", "source_content_hash"):
        if startup_data.get(field) != playing_data.get(field):
            raise ValueError("Startup and Playing observations are not the same actual world/session: " + field)

    if startup_data.get("kind") == STARTUP_DIAGNOSTIC_KIND:
        diagnostic_markers = [STARTUP_DIAGNOSTIC_MARKER.fullmatch(line) for line in log.splitlines()
                              if line.startswith("[S073NativeStartupWaterMask]")]
        if len(diagnostic_markers) != 1 or diagnostic_markers[0] is None:
            raise ValueError("One exact S073 startup no-callback diagnostic marker required")
        reason, marker_path = diagnostic_markers[0].groups()
        if Path(marker_path).resolve(strict=True) != startup_observation or \
                startup_data.get("status") != "NotObserved" or startup_data.get("diagnostic_reason") != reason or \
                reason != "NoPreparingPhaseOriginalPlacementCallbackBeforePlaying":
            raise ValueError("Startup no-callback diagnostic must bind its exact reason and original path")
        false_fields = ("campaign_gameplay_state_copied", "source_fit_certified", "mission_ready",
                        "normal_input_playthrough", "player_readiness")
        if any(startup_data.get(field) is not False for field in false_fields):
            raise ValueError("Startup diagnostic must retain explicit non-readiness limits")
        if startup_data.get("phase") != "Playing":
            raise ValueError("Startup absence diagnostic must identify the Playing boundary")
        input_hashes = {"startup_observation": sha(startup_observation.read_bytes()),
                        "playing_observation": playing_capture["observation_sha256"],
                        "playing_mask": playing_capture["mask_hash"], "editor_log": sha(log_bytes),
                        "playing_receipt": sha(playing_receipt_path.read_bytes())}
        for label, path in (("startup_observation", startup_observation), ("playing_observation", playing_observation),
                            ("playing_mask", playing_mask_path), ("editor_log", log_path),
                            ("playing_receipt", playing_receipt_path)):
            if sha(path.read_bytes()) != input_hashes[label]:
                raise ValueError("Joint water diagnostic input changed during validation: " + label)
        return {
            "kind": "S073JointNativeWaterMaskComparison", "schema_version": 1,
            "evidence_validation": "Passed", "parity_status": "NotComparable_StartupCaptureNotObserved",
            "masks_match": None, "differing_cells": None,
            "diagnostic_reason": reason,
            "comparison_scope": "SameActualSession_StartupCallbackAbsenceDiagnosticVsPlayingCapture_NotReadinessOrSourceFit",
            "wrapper_exit_code": wrapper_exit, "required_markers_present": True,
            "startup_capture": {"observation_path": str(startup_observation),
                                 "observation_sha256": input_hashes["startup_observation"],
                                 "mask_path": None, "mask_sha256": None, "request_id": None,
                                 "request_binding_available": False},
            "playing_capture": {"observation_path": str(playing_capture["observation_path"]),
                                "observation_sha256": playing_capture["observation_sha256"],
                                "mask_path": str(playing_capture["mask_path"]),
                                "mask_sha256": playing_capture["mask_hash"],
                                "water_cells": playing_capture["water_cells"],
                                "receipt_path": str(playing_receipt_path),
                                "receipt_sha256": input_hashes["playing_receipt"]},
            "editor_log_path": str(log_path), "editor_log_sha256": input_hashes["editor_log"],
            "session_id": startup_data["session_id"], "world_name": startup_data["world_name"],
            "world_sequence": startup_data["world_sequence"], "source_identity_hash": IDENTITY,
            "source_content_hash": CONTENT,
            "shared_source_pins_sha256": {relative: startup_pins[relative] for relative in sorted(COMMON_PINS)},
            "campaign_gameplay_state_copied": False, "source_fit_certified": False,
            "mission_ready": False, "normal_input_playthrough": False, "player_readiness": False,
        }

    if startup_data.get("kind") != STARTUP_KIND:
        raise ValueError("Qualified S073 startup observation or explicit no-callback diagnostic required")
    startup_markers = [STARTUP_MARKER.fullmatch(line) for line in log.splitlines()
                       if line.startswith("[S073NativeStartupWaterMask]")]
    if len(startup_markers) != 1 or startup_markers[0] is None:
        raise ValueError("One exact successful S073 Preparing-phase water marker required")
    startup_frame, startup_cells, startup_water, startup_marker_path = startup_markers[0].groups()
    if Path(startup_marker_path).resolve(strict=True) != startup_observation:
        raise ValueError("Startup marker must identify the original startup observation path")

    startup_mask = _regular_file(startup_mask, "Startup water mask")
    if startup_data.get("mask_path") != str(startup_mask) or playing_data.get("mask_path") != str(playing_mask_path):
        raise ValueError("Observation must bind the original capture mask paths")
    if (startup_data.get("request_id") is not None or startup_data.get("request_binding_available") is not False or
            startup_data.get("request_binding_reason") != "OriginalPlacementFacts does not expose BuildingRuntimeSpawnRequest identity" or
            type(startup_data.get("frame")) is not int or startup_data.get("frame") != int(startup_frame)):
        raise ValueError("Startup callback frame must bind without inventing an unavailable request identity")

    if startup_receipt is not None:
        startup_receipt_path = _regular_file(startup_receipt, "Startup capture receipt")
        startup_receipt_data = _read_json(startup_receipt_path)
        if (startup_receipt_data.get("validation_status") != "Passed" or
                startup_receipt_data.get("wrapper_exit_code") != 0 or
                startup_receipt_data.get("observation_sha256") != sha(startup_observation.read_bytes()) or
                startup_receipt_data.get("mask_sha256") != sha(startup_mask.read_bytes())):
            raise ValueError("Startup receipt does not bind the original capture paths and hashes")

    startup_capture = _validate_capture(repo, startup_data, startup_mask.read_bytes(), startup_observation,
                                        startup_mask, startup_pins, STARTUP_KIND, STARTUP_SCOPE,
                                        STARTUP_AUTHORITY, "Startup")
    if startup_capture["grid"] != playing_capture["grid"]:
        raise ValueError("Startup and Playing grid frames differ")
    if startup_capture["provider_path"] != playing_capture["provider_path"] or \
            startup_capture["provider_sha256"] != playing_capture["provider_sha256"]:
        raise ValueError("Startup and Playing water provider assembly differs")
    if int(startup_cells) != CELL_COUNT or int(playing_cells) != CELL_COUNT or \
            int(startup_water) != startup_capture["water_cells"] or int(playing_water) != playing_capture["water_cells"]:
        raise ValueError("Successful capture markers do not bind exact mask dimensions and populations")

    differing_cells = sum(left != right for left, right in zip(startup_capture["mask"], playing_capture["mask"]))
    masks_match = differing_cells == 0
    input_hashes = {"startup_observation": startup_capture["observation_sha256"],
                    "startup_mask": startup_capture["mask_hash"],
                    "playing_observation": playing_capture["observation_sha256"],
                    "playing_mask": playing_capture["mask_hash"], "editor_log": sha(log_bytes),
                    "playing_receipt": sha(playing_receipt_path.read_bytes())}
    for label, path in (("startup_observation", startup_observation), ("startup_mask", startup_mask),
                        ("playing_observation", original_playing_observation), ("playing_mask", original_mask_path),
                        ("editor_log", log_path), ("playing_receipt", playing_receipt_path)):
        if sha(path.read_bytes()) != input_hashes[label]:
            raise ValueError("Joint water comparison input changed during validation: " + label)

    return {
        "kind": "S073JointNativeWaterMaskComparison", "schema_version": 1,
        "evidence_validation": "Passed", "parity_status": "Match" if masks_match else "MismatchRecorded",
        "masks_match": masks_match, "differing_cells": differing_cells,
        "comparison_scope": "SameActualSession_OriginalPreparingCachedMaskVsPlayingOriginalCreateMask_NotReadinessOrSourceFit",
        "wrapper_exit_code": wrapper_exit, "required_markers_present": True,
        "startup_capture": {"observation_path": str(startup_capture["observation_path"]),
                             "observation_sha256": startup_capture["observation_sha256"],
                             "mask_path": str(startup_capture["mask_path"]),
                             "mask_sha256": startup_capture["mask_hash"], "request_id": None,
                             "request_binding_available": False, "frame": int(startup_frame),
                             "water_cells": startup_capture["water_cells"]},
        "playing_capture": {"observation_path": str(playing_capture["observation_path"]),
                            "observation_sha256": playing_capture["observation_sha256"],
                            "mask_path": str(playing_capture["mask_path"]),
                            "mask_sha256": playing_capture["mask_hash"],
                            "water_cells": playing_capture["water_cells"],
                            "receipt_path": str(playing_receipt_path),
                            "receipt_sha256": input_hashes["playing_receipt"]},
        "editor_log_path": str(log_path), "editor_log_sha256": input_hashes["editor_log"],
        "session_id": startup_data["session_id"], "world_name": startup_data["world_name"],
        "world_sequence": startup_data["world_sequence"], "source_identity_hash": IDENTITY,
        "source_content_hash": CONTENT, "grid": startup_capture["grid"],
        "shared_source_pins_sha256": {relative: startup_pins[relative] for relative in sorted(COMMON_PINS)},
        "campaign_gameplay_state_copied": False, "source_fit_certified": False,
        "mission_ready": False, "normal_input_playthrough": False, "player_readiness": False,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--startup-observation", required=True)
    parser.add_argument("--startup-mask", help="Original startup mask; omit when the observer emitted NotObserved diagnostic")
    parser.add_argument("--startup-receipt", help="Optional independently finalized startup capture receipt")
    parser.add_argument("--playing-directory", required=True,
                        help="Directory with snapshot.json, snapshot.water.bin, editor.log, and water-receipt.json")
    parser.add_argument("--playing-observation", help="Original Playing observation path if outside the receipt directory")
    parser.add_argument("--playing-mask", help="Original Playing mask path if outside the receipt directory")
    parser.add_argument("--editor-log", help="Shared log if the original startup observer wrote its own log path")
    parser.add_argument("--wrapper-exit", required=True, type=int)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    output = Path(args.output).resolve()
    if output.exists():
        raise ValueError("Fresh create-new joint comparison output required")
    result = validate(args.repo, args.startup_observation, args.startup_mask, args.playing_directory,
                      args.wrapper_exit, args.editor_log, args.startup_receipt, args.playing_observation, args.playing_mask)
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x", encoding="utf-8") as file:
        json.dump(result, file, indent=2)
        file.write("\n")
    print(json.dumps({"evidence_validation": result["evidence_validation"],
                      "parity_status": result["parity_status"],
                      "differing_cells": result["differing_cells"]}))


if __name__ == "__main__":
    main()

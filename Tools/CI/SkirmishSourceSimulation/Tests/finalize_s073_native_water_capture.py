"""Finalize an original-owner S073 water-mask observation; this is not readiness evidence."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct


IDENTITY = "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900"
CONTENT = "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c"
MAP_ID = "opmap.skirmish.industrial_basin"
PROVIDER = "Game.Rendering.RenderBoundsPresentation+Reader"
SCOPE = "SameSessionOriginalSkirmishWaterPlacementCreateMask_NotReadinessOrMissionAcceptance"
AUTHORITY = "OriginalSkirmishWaterPlacement.CreateMask_OriginalRenderBoundsGatewayProvider"
ENCODING = "uint8-bool01-row-major-xfast"
PIN_PATHS = {
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
MARKER = re.compile(
    r"^\[S073NativeWaterMask\] result=Recorded cells=(\d+) waterCells=(\d+) path=(\S+)$"
)
BLOCKER_MARKER = re.compile(
    r"^\[S073NativeBlockerOccupancy\] result=Recorded rawSha256=([0-9a-f]{64}) path=(\S+)$"
)
SOURCE_FIT_MARKER = re.compile(
    r"^\[S073LoadedCampaignSourceFit\] result=Captured session=([0-9a-f]{32}) "
    r"sceneGuid=([0-9a-f]{32}) spawnPlanAvailable=([01])$"
)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def _integer(value):
    return type(value) is int


def _hash(value):
    return isinstance(value, str) and re.fullmatch(r"[0-9a-f]{64}", value) is not None


def validate(repo, directory, wrapper_exit, observation_path=None, log_path=None, data_path=None):
    repo, directory = Path(repo).resolve(strict=True), Path(directory).resolve(strict=True)
    if wrapper_exit != 0:
        raise ValueError("Checked Unity wrapper did not exit successfully")
    observation_path = Path(observation_path).resolve(strict=True) if observation_path else directory / "snapshot.json"
    log_path = Path(log_path).resolve(strict=True) if log_path else directory / "editor.log"
    data_path = Path(data_path).resolve(strict=True) if data_path else directory / "snapshot.water.bin"
    raw, log = observation_path.read_bytes(), log_path.read_bytes()
    original_observation_sha256 = sha(raw)
    mask = data_path.read_bytes()
    data = json.loads(raw, object_pairs_hook=_no_duplicate_keys)
    original_observation_path = str(observation_path)
    original_observation_sha256 = sha(raw)
    normalized_observation_path = str(observation_path)
    normalized_observation_sha256 = original_observation_sha256
    text = log.decode("utf-8", errors="strict")

    markers = [MARKER.fullmatch(line) for line in text.splitlines()
               if line.startswith("[S073NativeWaterMask]")]
    if len(markers) != 1 or markers[0] is None:
        raise ValueError("One exact successful native water-mask marker required")
    marker_cells, marker_water, marker_path = markers[0].groups()
    if Path(marker_path).resolve(strict=True) != observation_path:
        raise ValueError("Native marker must identify this observation artifact")

    session = data.get("session_id")
    grid = data.get("grid")
    origin = grid.get("origin") if isinstance(grid, dict) else None
    if (data.get("kind") != "S073NativeWaterMaskObservation" or
            data.get("schema_version") != 1 or data.get("scope") != SCOPE or
            data.get("authority") != AUTHORITY or
            data.get("scenario_index") != 3 or data.get("seed") != 104729 or
            data.get("phase") != "Playing" or
            not isinstance(session, str) or re.fullmatch(r"[0-9a-f]{32}", session) is None or
            not isinstance(data.get("world_name"), str) or not data["world_name"] or
            not isinstance(data.get("world_sequence"), str) or not data["world_sequence"] or
            data.get("operation_map_id") != MAP_ID or
            data.get("source_identity_hash") != IDENTITY or
            data.get("source_content_hash") != CONTENT or
            data.get("physical_source_validated") is not True):
        raise ValueError("Exact same-session S073 map identity and validated source required")

    if (not isinstance(grid, dict) or grid.get("width") != 2048 or
            grid.get("height") != 1024 or type(grid.get("cell_size")) not in (int, float) or
            isinstance(grid.get("cell_size"), bool) or grid.get("cell_size") != 1 or
            not isinstance(origin, list) or len(origin) != 3 or
            any(type(v) not in (int, float) or isinstance(v, bool) for v in origin) or
            origin != [0, 0, 0]):
        raise ValueError("Exact S073 2048x1024 one-meter grid required")

    provider_type = data.get("provider_type")
    provider_path = data.get("provider_assembly_path")
    provider_hash = data.get("provider_assembly_sha256")
    if (not isinstance(provider_type, str) or not provider_type.startswith(PROVIDER + ",") or
            not isinstance(provider_path, str) or not provider_path or not _hash(provider_hash)):
        raise ValueError("Original RenderBoundsPresentation water provider required")
    assembly = Path(provider_path).resolve(strict=True)
    if assembly != (repo / "Library/ScriptAssemblies/Game.Rendering.dll").resolve(strict=True) or sha(assembly.read_bytes()) != provider_hash:
        raise ValueError("Captured water provider assembly changed")

    count = 2048 * 1024
    if (data.get("mask_path") != str(data_path) or
            data.get("mask_encoding") != ENCODING or data.get("mask_cells") != count or
            len(mask) != count or any(value not in (0, 1) for value in mask)):
        raise ValueError("Complete uint8 bool01 row-major water mask required")
    mask_hash = sha(mask)
    water_cells = sum(mask)
    if (data.get("mask_sha256") != mask_hash or data.get("water_cells") != water_cells or
            int(marker_cells) != count or int(marker_water) != water_cells):
        raise ValueError("Water mask count or hash does not match original capture")

    footprint_count = data.get("water_footprint_count")
    if not _integer(footprint_count) or not 0 <= footprint_count <= 100000:
        raise ValueError("Separate bounded water-footprint query count required")
    before, after = data.get("entity_order_version_before"), data.get("entity_order_version_after")
    if not _integer(before) or not _integer(after) or before != after:
        raise ValueError("ECS structural version changed during water observation")
    if data.get("provider_reference_unchanged") is not True:
        raise ValueError("Water provider reference changed during observation")

    pins = data.get("source_pins_sha256")
    if not isinstance(pins, dict) or set(pins) != PIN_PATHS:
        raise ValueError("Exact S073 water observation source pin set required")

    source_fit = data.get("loaded_campaign_source_fit_observation")
    completeness = source_fit.get("capture_completeness") if isinstance(source_fit, dict) else None
    scene_path = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity"
    source_blockers = source_fit.get("blocker_contributors") if isinstance(source_fit, dict) else None
    source_buildings = source_fit.get("building_entities") if isinstance(source_fit, dict) else None
    if (not isinstance(source_fit, dict) or
            source_fit.get("kind") != "S073SameSessionLoadedCampaignSourceFitObservation" or
            source_fit.get("scope") != "SamePlayingWorld_LoadedCampaignSceneOwnerAndSpawnInputs_DiagnosticOnly" or
            source_fit.get("session_id") != session or source_fit.get("phase") != "Playing" or
            source_fit.get("world_name") != data.get("world_name") or
            source_fit.get("world_sequence") != data.get("world_sequence") or
            source_fit.get("logical_map_id") != MAP_ID or
            source_fit.get("campaign_source_map_id") != "opmap.skirmish.desert_base_01" or
            source_fit.get("source_identity_hash") != IDENTITY or source_fit.get("source_content_hash") != CONTENT or
            source_fit.get("source_scene_guid") != "c00140f2e94a04c3084c8dcb0c18cbd0" or
            source_fit.get("source_scene_path") != scene_path or source_fit.get("scene_loaded") is not True or
            source_fit.get("grid_owned_by_campaign_scene") is not True or
            source_fit.get("source_scene_sha256") != pins.get(scene_path) or
            source_fit.get("source_scene_meta_sha256") != pins.get(scene_path + ".meta") or
            source_fit.get("s073_config_sha256") != pins.get("Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset") or
            source_fit.get("blocker_owner_source_sha256") != pins.get("Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs") or
            not isinstance(source_blockers, list) or
            source_fit.get("blocker_contributor_query_count") != len(source_blockers) or
            source_fit.get("blocker_contributors_owned_by_campaign_scene") != sum(row.get("bound_campaign_scene_owned") is True for row in source_blockers) or
            not isinstance(source_buildings, list) or
            source_fit.get("building_owner_query_count") != len(source_buildings) or
            source_fit.get("building_entities_owned_by_campaign_scene") != sum(row.get("bound_campaign_scene_owned") is True for row in source_buildings) or
            not isinstance(source_fit.get("original_initial_unit_spawn_cells"), list) or
            not isinstance(source_fit.get("original_initial_building_spawn_inputs"), list) or
            type(source_fit.get("original_initial_spawn_plan_available")) is not bool or
            not isinstance(completeness, dict) or
            completeness.get("loaded_campaign_scene") is not True or
            completeness.get("active_grid_scene_ownership") is not True or
            completeness.get("blocker_owner_inputs") is not (len(source_blockers) > 0) or
            completeness.get("building_owner_inputs") is not (len(source_buildings) > 0) or
            completeness.get("original_initial_spawn_plan") is not source_fit.get("original_initial_spawn_plan_available") or
            source_fit.get("source_fit_certified") is not False or
            source_fit.get("physical_source_admission_changed") is not False or
            source_fit.get("scheduler_admitted") is not False or source_fit.get("mission_ready") is not False or
            source_fit.get("player_readiness") is not False):
        raise ValueError("Exact same-session loaded Campaign source-fit diagnostic required; it cannot claim admission or readiness")
    scene_file = (repo / scene_path).resolve(strict=True)
    scene_meta_file = Path(str(scene_file) + ".meta")
    if (not scene_file.is_relative_to(repo) or not scene_meta_file.is_file() or
            sha(scene_file.read_bytes()) != source_fit.get("source_scene_sha256") or
            sha(scene_meta_file.read_bytes()) != source_fit.get("source_scene_meta_sha256") or
            "guid: " + source_fit["source_scene_guid"] not in scene_meta_file.read_text(encoding="utf-8")):
        raise ValueError("Loaded Campaign source scene or GUID meta changed after the same-session capture")
    source_fit_markers = [SOURCE_FIT_MARKER.fullmatch(line) for line in text.splitlines()
                          if line.startswith("[S073LoadedCampaignSourceFit]")]
    if (len(source_fit_markers) != 1 or source_fit_markers[0] is None or
            source_fit_markers[0].group(1) != session or
            source_fit_markers[0].group(2) != source_fit.get("source_scene_guid") or
            source_fit_markers[0].group(3) != ("1" if source_fit.get("original_initial_spawn_plan_available") else "0")):
        raise ValueError("One exact loaded Campaign source-fit marker bound to the captured session is required")

    blocker_capture = None
    mask_copy = directory / "snapshot.water.bin"
    if data_path != mask_copy:
        if mask_copy.exists():
            raise ValueError("Fresh persistent native water mask output required")
        with mask_copy.open("xb") as output:
            output.write(mask)
        data["mask_path"] = str(mask_copy)
    passive = data.get("playing_passive_input_observation")
    if passive is not None:
        if (not isinstance(passive, dict) or passive.get("observation_name") != "playing_passive_input_observation" or
                passive.get("session_id") != session or passive.get("phase") != "Playing" or
                passive.get("input_injected") is not False or passive.get("gameplay_state_mutated") is not False):
            raise ValueError("Same-session passive Playing blocker observation required")
        raw_receipt = passive.get("raw_blocker_occupancy_snapshot")
        if (not isinstance(raw_receipt, dict) or raw_receipt.get("kind") != "S073BlockerOccupancyRawSnapshot" or
                raw_receipt.get("producer") != "native" or raw_receipt.get("status") != "Captured" or
                raw_receipt.get("observation") != "playing_passive_input_observation" or
                raw_receipt.get("native_session_id") != session or raw_receipt.get("mission") != "S073" or
                raw_receipt.get("seed") != 104729 or raw_receipt.get("source_map_id") != MAP_ID or
                raw_receipt.get("source_campaign_map_id") != "opmap.skirmish.desert_base_01" or
                raw_receipt.get("source_identity_hash") != IDENTITY or raw_receipt.get("source_content_hash") != CONTENT or
                raw_receipt.get("native_source_pins_sha256") != pins or
                raw_receipt.get("source_host_state_transferred") is not False or
                raw_receipt.get("campaign_state_transferred") is not False or raw_receipt.get("readiness_claimed") is not False):
            raise ValueError("Exact native S073 passive blocker receipt and non-admission scope required")
        blocker_markers = [BLOCKER_MARKER.fullmatch(line) for line in text.splitlines()
                           if line.startswith("[S073NativeBlockerOccupancy]")]
        if len(blocker_markers) != 1 or blocker_markers[0] is None:
            raise ValueError("One exact native passive blocker capture marker required")
        original_raw = Path(raw_receipt.get("raw_path", "")).resolve(strict=True)
        raw_bytes = original_raw.read_bytes()
        if (sha(raw_bytes) != raw_receipt.get("raw_sha256") or len(raw_bytes) != raw_receipt.get("byte_length") or
                blocker_markers[0].group(1) != raw_receipt.get("raw_sha256") or
                Path(blocker_markers[0].group(2)).resolve(strict=True) != original_raw):
            raise ValueError("Native blocker raw payload hash/length mismatch")
        if len(raw_bytes) < 60 or raw_bytes[:8] != b"S073BOC1":
            raise ValueError("Native blocker raw payload magic/header required")
        width, height, cell_size, ox, oy, oz, epoch, applied_epoch, revision, cells = struct.unpack_from("<IIffffQQQI", raw_bytes, 8)
        bit_bytes = (cells + 7) // 8
        expected_length = 60 + bit_bytes + cells * 4 + cells + bit_bytes
        if (width != grid["width"] or height != grid["height"] or cells != width * height or
                cell_size != grid["cell_size"] or [ox, oy, oz] != origin or len(raw_bytes) != expected_length or
                raw_receipt.get("width") != width or raw_receipt.get("height") != height or
                raw_receipt.get("cell_size") != cell_size or raw_receipt.get("origin") != [ox, oy, oz] or
                raw_receipt.get("storage_epoch") != str(epoch) or
                raw_receipt.get("applied_storage_epoch") != str(applied_epoch) or
                raw_receipt.get("applied_revision") != str(revision)):
            raise ValueError("Native blocker raw schema/header differs from the Playing water grid")
        raw_copy = directory / "blocker-occupancy.bin"
        receipt_copy = directory / "blocker-occupancy.bin.json"
        if raw_copy.exists() or receipt_copy.exists():
            raise ValueError("Fresh persistent blocker raw capture outputs required")
        raw_copy.write_bytes(original_raw.read_bytes())
        copied_receipt = dict(raw_receipt)
        copied_receipt["raw_path"] = str(raw_copy)
        with receipt_copy.open("x", encoding="utf-8") as output:
            json.dump(copied_receipt, output, indent=2)
            output.write("\n")
        passive["raw_blocker_occupancy_snapshot"] = copied_receipt
        snapshot_copy = directory / "snapshot.json"
        if observation_path != snapshot_copy:
            if snapshot_copy.exists():
                raise ValueError("Fresh normalized native snapshot path required")
        with snapshot_copy.open("w" if observation_path == snapshot_copy else "x", encoding="utf-8") as output:
            json.dump(data, output, indent=2)
            output.write("\n")
        observation_path = snapshot_copy
        raw = observation_path.read_bytes()
        normalized_observation_path = str(snapshot_copy.resolve())
        normalized_observation_sha256 = sha(raw)
        blocker_capture = {
            "raw": {"path": str(raw_copy), "sha256": sha(raw_copy.read_bytes()), "bytes": raw_copy.stat().st_size},
            "receipt": {"path": str(receipt_copy), "sha256": sha(receipt_copy.read_bytes())},
            "observation": "playing_passive_input_observation",
        }

    for relative, digest in pins.items():
        if not _hash(digest):
            raise ValueError("Malformed source pin: " + relative)
        path = (repo / relative).resolve(strict=True)
        if not path.is_relative_to(repo) or not path.is_file() or sha(path.read_bytes()) != digest:
            raise ValueError("Capture source changed: " + relative)

    false_fields = ("campaign_gameplay_state_copied", "mission_ready", "source_fit_certified",
                    "normal_input_playthrough", "player_readiness")
    if any(data.get(field) is not False for field in false_fields):
        raise ValueError("Water observation must retain explicit non-readiness limits")
    if sha(observation_path.read_bytes()) != sha(raw) or sha(log_path.read_bytes()) != sha(log) or sha(data_path.read_bytes()) != mask_hash or sha(assembly.read_bytes()) != provider_hash:
        raise ValueError("Water capture inputs changed during validation")

    return {
        "kind": "S073NativeWaterMaskCaptureReceipt",
        "schema_version": 1,
        "validation_status": "Passed",
        "wrapper_exit_code": 0,
        "required_marker_present": True,
        "session_id": session,
        "world_name": data["world_name"],
        "world_sequence": data["world_sequence"],
        "operation_map_id": MAP_ID,
        "source_identity_hash": IDENTITY,
        "source_content_hash": CONTENT,
        "grid": grid,
        "provider_type": provider_type,
        "provider_assembly_path": provider_path,
        "provider_assembly_sha256": provider_hash,
        "water_footprint_count": footprint_count,
        "mask_encoding": ENCODING,
        "mask_cells": count,
        "water_cells": water_cells,
        "mask_sha256": mask_hash,
        "mask_path": str(mask_copy),
        "entity_order_version": before,
        "observation_sha256": original_observation_sha256,
        "original_observation_path": original_observation_path,
        "original_observation_sha256": original_observation_sha256,
        "normalized_observation_path": normalized_observation_path,
        "normalized_observation_sha256": normalized_observation_sha256,
        "original_observation_sha256": original_observation_sha256,
        "blocker_occupancy_raw_capture": blocker_capture,
        "loaded_campaign_source_fit_observation": source_fit,
        "editor_log_sha256": sha(log),
        "source_pins_sha256": pins,
        "campaign_gameplay_state_copied": False,
        "mission_ready": False,
        "source_fit_certified": False,
        "normal_input_playthrough": False,
        "player_readiness": False,
        "scope": "Same-session original water-mask observation; footprint query count is separate and this receipt is not readiness evidence",
    }


def _no_duplicate_keys(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property: " + key)
        result[key] = value
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--directory", required=True)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    parser.add_argument("--observation", help="Original observer output path if outside the receipt directory")
    parser.add_argument("--mask", help="Original observer mask path if outside the receipt directory")
    args = parser.parse_args()
    directory = Path(args.directory)
    receipt = validate(args.repo, directory, args.wrapper_exit, args.observation, None, args.mask)
    with (directory / "water-receipt.json").open("x", encoding="utf-8") as output:
        json.dump(receipt, output, indent=2)
        output.write("\n")
    print(json.dumps({key: receipt[key] for key in
                      ("validation_status", "water_footprint_count", "water_cells", "mask_sha256")}))


if __name__ == "__main__":
    main()

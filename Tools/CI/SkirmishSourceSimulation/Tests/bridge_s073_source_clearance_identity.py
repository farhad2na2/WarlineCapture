#!/usr/bin/env python3
"""Join retained Campaign clearance geometry to the exact current S073 source binding.

This validates source identity only. It does not establish provider parity,
delivery success, physical source fit, or mission readiness.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re


PHYSICAL_CAPTURE = "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json"
PHYSICAL_CAPTURE_SHA256 = "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8"
PHYSICAL_MAP = "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset"
PHYSICAL_MAP_SHA256 = "8a03d36fd1d4d59a180c65d82f1c708fd70e7c291f49d480c80629a80ec728c0"
S073_MAP = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"
S073_MAP_SHA256 = "b145eae26821fa3ee559de12473354813c83d0a4c1997159086c20654ea0112a"
S073_MAP_GUID = "20949d67846d14fc0862faf37b2ba0e9"
S073_MAP_META_SHA256 = "bc520f09b543cf15f55bcb1db0331980303bb26a228532b12eb1a046356382f3"
RUNTIME_SCENE = "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity"
RUNTIME_SCENE_SHA256 = "a820d20ac1d153d6c95880ed1da1f53bb522c9999345a88e23d213213a058ffa"
RUNTIME_SCENE_GUID = "dad0bd13fb20943dfb2f881cbe225f05"
SCENE = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity"
SCENE_SHA256 = "067c5d78592503cc22b9971db2d4abdeb852460b31f9caac12b37df3c5179975"
SCENE_GUID = "c00140f2e94a04c3084c8dcb0c18cbd0"
SCENE_META_SHA256 = "44f0ba1b5c8f8038d7e4dcda9669c7ea6be1e32d6165635f41c0b0e88036e40d"
SOURCE_MAP_ID = "opmap.skirmish.desert_base_01"
LOGICAL_MAP_ID = "opmap.skirmish.industrial_basin"
PROVIDER_AUTHORITY = "OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain"
WORLD_CAPTURE = "Logs/SkirmishProduction/s073-streamed-source-capture-20261101-r4/source-world.json"
WORLD_RECEIPT = "Logs/SkirmishProduction/s073-streamed-source-capture-20261101-r4/receipt.json"
PROVIDER_SOURCE = "Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs"
PROVIDER_SOURCE_SHA256 = "82bf379f5bb3e5ae55705f608c3e4a26ed915afffeec7488c79b9448cecfb5a4"
CAPTURE_SOURCE = "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073StreamedWorld.cs"
CAPTURE_SOURCE_SHA256 = "f278e4383dfb4ae3ad3f00d7b1da4cedce75625cbdceeaff7bdf60d9d9614346"


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha256(path):
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def read_json(path, max_bytes):
    require(path.is_file() and not path.is_symlink(), f"Regular source evidence required: {path}")
    require(path.stat().st_size <= max_bytes, f"Source evidence exceeds bound: {path}")
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=no_duplicates)


def no_duplicates(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, f"Duplicate JSON field: {key}")
        result[key] = value
    return result


def guid_from_meta(path):
    matches = re.findall(r"(?m)^guid: ([0-9a-f]{32})\s*$", path.read_text(encoding="utf-8"))
    require(len(matches) == 1, f"Unique source GUID required: {path}")
    return matches[0]


def validate_clearance_record(record):
    require(isinstance(record, dict) and
            record.get("authority") == PROVIDER_AUTHORITY and
            record.get("runtime_presentation_activation_sampled") is False and
            record.get("authored_transform_projection_count") == 124499 and
            record.get("grid_width") == 2048 and record.get("grid_height") == 1024 and
            record.get("grid_origin") == [0.0, 0.0, 0.0] and record.get("cell_size") == 1.0 and
            record.get("minimum_extent") == 0.25 and record.get("maximum_extent") == 20.0 and
            record.get("source_search_radius_cells") == 96 and record.get("source_padding_cells") == 2 and
            len(record.get("bounds", [])) == 90865,
            "Complete original source-frame drop-clearance projection required")
    rect = record.get("query_rect")
    require(rect == [-118.0, -118.0, 2284.0, 1260.0],
            "Exact complete-grid clearance query envelope required")
    return rect


def validate_s073_map_binding(selected_text, selected_guid, runtime_guid, runtime_scene_text):
    require("operationMapId: " + LOGICAL_MAP_ID in selected_text and
            "sourceOperationMapId: " + SOURCE_MAP_ID in selected_text,
            "S073 selected map no longer binds to the captured Campaign source map")
    require(selected_guid == S073_MAP_GUID, "S073 selected map GUID changed")
    require("m_AssetGUID: " + runtime_guid in selected_text and runtime_guid == RUNTIME_SCENE_GUID,
            "S073 map no longer references the pinned generated runtime source scene")
    require("guid: " + SCENE_GUID in runtime_scene_text,
            "Generated runtime scene no longer embeds the pinned Campaign source scene")


def build_receipt(repo, geometry_relocation=None):
    repo = Path(repo).resolve(strict=True)
    require(geometry_relocation is None,
            "Geometric relocation is rejected: S073 must use the exact shared Campaign source frame")

    physical_path = repo / PHYSICAL_CAPTURE
    require(sha256(physical_path) == PHYSICAL_CAPTURE_SHA256,
            "Exact retained physical source capture pin required")
    physical = read_json(physical_path, 160 * 1024 * 1024)
    require(physical.get("kind") == "PinnedCampaignPhysicalInputExport" and
            physical.get("result") == "Collected" and physical.get("worldDisposed") is True and
            physical.get("missionInitialized") is False and physical.get("missionReady") is False and
            physical.get("sourceFitCertified") is False and
            physical.get("operationMapId") == SOURCE_MAP_ID and
            physical.get("sourcePath") == PHYSICAL_MAP and
            physical.get("sourceSha256") == PHYSICAL_MAP_SHA256,
            "Pinned physical source identity/scope changed")
    physical_map_path = repo / PHYSICAL_MAP
    require(sha256(physical_map_path) == PHYSICAL_MAP_SHA256,
            "Current Campaign physical map differs from retained capture")
    record = physical.get("fixedMapReadDomain", {}).get("dropClearanceBounds")
    rect = validate_clearance_record(record)

    selected_path = repo / S073_MAP
    require(sha256(selected_path) == S073_MAP_SHA256,
            "Current S073 selected map source pin changed")
    selected_text = selected_path.read_text(encoding="utf-8")
    selected_meta = Path(str(selected_path) + ".meta")
    require(sha256(selected_meta) == S073_MAP_META_SHA256,
            "Current S073 selected map meta pin changed")
    selected_guid = guid_from_meta(selected_meta)
    runtime_scene_path = repo / RUNTIME_SCENE
    runtime_scene_meta = Path(str(runtime_scene_path) + ".meta")
    runtime_guid = guid_from_meta(runtime_scene_meta)
    runtime_scene_text = runtime_scene_path.read_text(encoding="utf-8")
    validate_s073_map_binding(selected_text, selected_guid, runtime_guid, runtime_scene_text)
    runtime_scene_sha = sha256(runtime_scene_path)
    require(runtime_scene_sha == RUNTIME_SCENE_SHA256,
            "Generated runtime source scene pin changed")

    scene_path = repo / SCENE
    scene_meta = Path(str(scene_path) + ".meta")
    require(sha256(scene_path) == SCENE_SHA256 and sha256(scene_meta) == SCENE_META_SHA256 and
            guid_from_meta(scene_meta) == SCENE_GUID,
            "Current S073 Campaign source scene identity changed")
    provider_path = repo / PROVIDER_SOURCE
    require(sha256(provider_path) == PROVIDER_SOURCE_SHA256,
            "Original static-bounds provider source pin changed")
    world_capture_path = repo / WORLD_CAPTURE
    world_receipt_path = repo / WORLD_RECEIPT
    world_capture = read_json(world_capture_path, 160 * 1024 * 1024)
    world_receipt = read_json(world_receipt_path, 1024 * 1024)
    world_capture_sha = sha256(world_capture_path)
    require(world_receipt.get("capture_source_sha256") == CAPTURE_SOURCE_SHA256 and
            sha256(repo / CAPTURE_SOURCE) == CAPTURE_SOURCE_SHA256,
            "S073 streamed capture implementation source pin changed")
    require(world_receipt.get("kind") == "S073StreamedSourceCaptureReceipt" and
            world_receipt.get("status") == "Captured" and world_receipt.get("wrapper_exit_code") == 0 and
            world_receipt.get("required_marker_present") is True and
            world_receipt.get("capture_sha256") == world_capture_sha and
            world_receipt.get("scene_sha256") == SCENE_SHA256 and
            world_receipt.get("mission_ready") is False and
            world_receipt.get("closure_complete") is False,
            "S073 streamed source capture receipt does not match its source capture")
    require(world_capture.get("kind") == "S073IsolatedStreamedSourceWorldCapture" and
            world_capture.get("scene_guid") == SCENE_GUID and world_capture.get("scene_path") == SCENE and
            world_capture.get("scene_sha256") == SCENE_SHA256 and
            world_capture.get("scope") == "IsolatedPreGameSource_NotLiveSessionOrReadiness" and
            world_capture.get("live_s073_session_observed") is False and
            world_capture.get("mission_ready") is False,
            "S073 source capture scene binding/scope differs")

    evidence = {
        "physical_capture": {"path": PHYSICAL_CAPTURE, "sha256": PHYSICAL_CAPTURE_SHA256},
        "physical_map": {"path": PHYSICAL_MAP, "sha256": PHYSICAL_MAP_SHA256},
        "s073_selected_map": {"path": S073_MAP, "sha256": S073_MAP_SHA256, "asset_guid": selected_guid,
                              "logical_map_id": LOGICAL_MAP_ID, "source_map_id": SOURCE_MAP_ID},
        "s073_source_scene": {"path": SCENE, "sha256": SCENE_SHA256, "asset_guid": SCENE_GUID},
        "s073_generated_runtime_scene": {"path": RUNTIME_SCENE, "sha256": runtime_scene_sha,
                                         "asset_guid": runtime_guid},
        "provider_implementation": {"path": PROVIDER_SOURCE, "sha256": PROVIDER_SOURCE_SHA256},
        "capture_implementation": {"path": CAPTURE_SOURCE, "sha256": CAPTURE_SOURCE_SHA256},
        "s073_streamed_capture": {"path": WORLD_CAPTURE, "sha256": world_capture_sha,
                                  "receipt_path": WORLD_RECEIPT, "receipt_sha256": sha256(world_receipt_path)},
    }
    return {
        "schema_version": 1,
        "kind": "S073SourceClearanceIdentityBridge",
        "status": "IdentityBound",
        "scope": "SharedCampaignSourceFrameIdentityOnly_NoProviderParity_NoDeliveryOrReadinessClaim",
        "source_geometry": {
            "authority": PROVIDER_AUTHORITY,
            "source_capture_sha256": PHYSICAL_CAPTURE_SHA256,
            "source_map_id": SOURCE_MAP_ID,
            "bounds_count": 90865,
            "authored_transform_projection_count": 124499,
            "grid": {"width": 2048, "height": 1024, "origin": [0.0, 0.0, 0.0], "cell_size": 1.0},
            "query_rect": rect,
            "minimum_extent": 0.25,
            "maximum_extent": 20.0,
            "geometric_relocation": "None_ExactSharedCampaignSourceFrameRequired",
        },
        "s073_source_binding": {
            "logical_map_id": LOGICAL_MAP_ID,
            "source_map_id": SOURCE_MAP_ID,
            "selected_map_guid": selected_guid,
            "scene_guid": SCENE_GUID,
            "streamed_capture_sha256": world_capture_sha,
            "mission_ready": False,
            "source_fit_certified": False,
        },
        "evidence": evidence,
        "provider_parity_established": False,
        "historical_capture_dependency_pins_revalidated": False,
        "source_capture_current_producer_equivalence": False,
        "capture_relocation_path_remapping_validated": False,
        "delivery_established": False,
        "mission_ready": False,
        "source_fit_certified": False,
        "limits": [
            "Historical dependency pins recorded inside the retained physical capture were not revalidated.",
            "Current source files were not compared with the historical capture producer binary/source environment.",
            "Capture relocation path remapping is outside this identity bridge; its false flag does not reject or validate a remapped capture path.",
            "The optional geometry-relocation rejection concerns coordinates only and is unrelated to capture path remapping.",
        ],
    }


def write_exclusive(path, receipt):
    path = Path(path).absolute()
    raw = (json.dumps(receipt, indent=2, ensure_ascii=False, allow_nan=False) + "\n").encode("utf-8")
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("xb") as stream:
        stream.write(raw)
    return path, hashlib.sha256(raw).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[4])
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--geometry-relocation", type=Path,
                        help="Rejected coordinate transform; unrelated to capture path remapping")
    args = parser.parse_args()
    receipt = build_receipt(args.repo, geometry_relocation=args.geometry_relocation)
    path, digest = write_exclusive(args.output, receipt)
    print(json.dumps({"result": "Passed", "output": str(path), "sha256": digest,
                      "scope": receipt["scope"], "mission_ready": False}, separators=(",", ":")))


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        raise SystemExit(f"[S073SourceClearanceIdentityBridge] result=Rejected reason={error}")

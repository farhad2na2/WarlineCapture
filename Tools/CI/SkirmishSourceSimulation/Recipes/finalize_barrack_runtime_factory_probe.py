#!/usr/bin/env python3
"""Fail-closed finalizer for the read-only direct Barrack factory probe."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[4]
MARKER = "[BarrackRuntimeFactorySourceProbe] result=Passed scope=direct_asset_factory_source_only"
PREFAB = "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab"
PREFAB_GUID = "683195b1e21c94df7add42ab26cd2a33"
PREFAB_LOCAL_ID = 6603657636351658259
REQUIRED_SOURCES = {
    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingModelBounds.cs",
    "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
    "Assets/Game/Scripts/Authorings/BuildingDefinitionAuthoring.cs",
    "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
    "Assets/Game/Scripts/RuntimeState/BuildingDefinition.cs",
    "Assets/Game/Scripts/Systems/BuildingDefinitionProductionRecipes.cs",
    "Assets/Game/Scripts/Systems/BuildingDefinitionUnitProductionResources.cs",
    "Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs",
    "Assets/Game/Scripts/Systems/BuildingRunwaySystem.PrefabGeometry.cs",
    "Assets/Game/Scripts/Components/BuildingProducerCoreProjection.cs",
    "Assets/Game/Scripts/Game.Runtime.asmdef",
    "Assets/Game/Scripts/Composition/Game.Composition.asmdef",
    "Assets/Game/Scripts/Editor/Game.Editor.asmdef",
    "ProjectSettings/ProjectVersion.txt",
    "Packages/manifest.json",
    "Packages/packages-lock.json",
    "Assets/Game/Scripts/Editor/Skirmish/BarrackRuntimeFactorySourceProbe.cs",
}
SHA_RE = re.compile(r"^[0-9a-f]{64}$")


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def read_pinned(root: Path, relative: str, expected: str) -> bytes:
    rel = Path(relative)
    need(not rel.is_absolute() and ".." not in rel.parts, "unsafe source pin path")
    path = root / rel
    cursor = root
    for part in rel.parts:
        cursor = cursor / part
        need(not cursor.is_symlink(), "symlinked source pin path rejected: " + relative)
    need(path.is_file(), "pinned source missing: " + relative)
    raw = path.read_bytes()
    need(sha(raw) == expected, "pinned source changed: " + relative)
    return raw


def validate(root: Path, capture: Path, log: Path, wrapper_exit: int) -> dict:
    root = root.resolve(strict=True)
    need(wrapper_exit == 0, "checked Unity wrapper exit must be zero")
    log_text = log.read_text(encoding="utf-8", errors="strict")
    need(log_text.count(MARKER) == 1,
         "exact direct asset factory pass marker must occur once")
    raw = capture.read_bytes()
    document = json.loads(raw.decode("utf-8", errors="strict"), object_pairs_hook=_no_duplicates)
    need(document.get("schema_version") == 1 and
         document.get("kind") == "BarrackRuntimeFactoryDirectAssetSourceProbe" and
         document.get("status") == "PassedDirectAssetFactorySourceProbe" and
         document.get("authority") == "Current Unity AssetDatabase source identity, evaluated prefab contents, and original CreateRuntimeBuildingDefinition output",
         "exact current direct factory probe required")
    need(document.get("unity_version") and isinstance(document["unity_version"], str),
         "Unity editor version required")
    prefab = document.get("prefab", {})
    need(prefab.get("path") == PREFAB and prefab.get("guid") == PREFAB_GUID and
         prefab.get("local_id") == PREFAB_LOCAL_ID and
         prefab.get("identity_basis") == "AssetDatabase.TryGetGUIDAndLocalFileIdentifier on exact loaded prefab root",
         "exact AssetDatabase Barrack root identity required")
    footprint = document.get("footprint", {})
    need(footprint.get("factory_method") == "BuildingDefinitionPrefabSystemHelper.CreateRuntimeBuildingDefinition" and
         footprint.get("bound_owner") == "BuildingModelBounds.TryMeasure/EnclosingCells" and
         footprint.get("factory_target") == "disposable PrefabUtility.LoadPrefabContents root; not AssetDatabase persistent object" and
         footprint.get("asset_contents_geometry_equivalence") is True and
         footprint.get("resolver_may_mutate_temporary_authoring_instance") is True and
         footprint.get("temporary_contents_unloaded_without_save") is True and
         footprint.get("authored_configured_cells_used_as_factory_authority") is False,
         "exact original factory/bounds owner facts required")
    def positive_pair(value):
        return isinstance(value, list) and len(value) == 2 and all(type(n) is int and n > 0 for n in value)
    need(positive_pair(footprint.get("asset_serialized_authoring_cells")) and
         positive_pair(footprint.get("contents_effective_authoring_cells")) and
         positive_pair(footprint.get("asset_visual_cells")) and
         positive_pair(footprint.get("contents_visual_cells")) and
         positive_pair(footprint.get("factory_cells")) and
         footprint["asset_visual_cells"] == footprint["contents_visual_cells"] == footprint["factory_cells"],
         "asset, disposable contents, and factory footprints must be separately captured and agree")
    for bounds_key in ("asset_visual_bounds", "contents_visual_bounds"):
        bounds = footprint.get(bounds_key)
        need(isinstance(bounds, dict) and all(k in bounds for k in ("center", "size", "min", "max")),
             "both asset and disposable contents visual bounds are required")
    scope = document.get("scope", {})
    need(scope == {
        "direct_prefab_asset_only": True,
        "mission_scene_opened": False,
        "temporary_prefab_contents_scene_used": True,
        "open_scene_setup_changed": False,
        "ecs_world_created_by_probe": False,
        "mission_setup_invoked": False,
        "asset_repair_invoked": False,
        "source_files_changed": False,
        "loaded_source_prefab_dirty_after_probe": False,
        "runtime_gameplay_admission": False,
        "mission_ready": False,
    }, "direct asset only scope required")

    pins = document.get("source_pins")
    need(isinstance(pins, dict) and pins, "source pin map required")
    for path, digest in pins.items():
        need(isinstance(path, str) and isinstance(digest, str) and SHA_RE.fullmatch(digest) is not None,
             "valid exact source hash required")
        read_pinned(root, path, digest)
    need(REQUIRED_SOURCES.issubset(pins), "original factory, bounds, resolver, authoring, projection, and probe sources must be pinned")
    need(pins.get(PREFAB) == prefab.get("asset_sha256") and
         pins.get(PREFAB + ".meta") == prefab.get("meta_sha256"),
         "prefab content and metadata hashes must match pinned sources")
    rows = document.get("dependency_paths")
    need(isinstance(rows, list) and rows, "transitive prefab dependency evidence required")
    dependency_paths = []
    for row in rows:
        need(isinstance(row, dict), "dependency row object required")
        path = row.get("path")
        need(isinstance(path, str) and path.startswith("Assets/") and path not in dependency_paths,
             "unique in-repository dependency path required")
        dependency_paths.append(path)
        need(pins.get(path) == row.get("sha256") and pins.get(path + ".meta") == row.get("meta_sha256"),
             "dependency row hashes must match source pins")
    need(PREFAB in dependency_paths, "Barrack prefab must appear in dependency closure")
    assemblies = document.get("loaded_assemblies")
    need(isinstance(assemblies, list) and
         len(assemblies) == 3 and
         {row.get("name") for row in assemblies if isinstance(row, dict)} ==
         {"Game.Runtime", "Game.Composition", "Game.Editor"},
         "exact loaded runtime, composition, and editor assemblies required")
    for row in assemblies:
        path = row.get("path")
        need(isinstance(path, str) and pins.get(path) == row.get("sha256"),
             "loaded assembly byte hash must match pinned source map")

    finalizer_path = Path(__file__).resolve()
    return {
        "schema_version": 1,
        "kind": "BarrackRuntimeFactoryDirectAssetProbeReceipt",
        "status": "PassedDirectAssetFactoryProbeOnly",
        "wrapper_exit_code": wrapper_exit,
        "required_marker_count": 1,
        "capture": {"path": str(capture.resolve()), "sha256": sha(raw)},
        "unity_version": document["unity_version"],
        "prefab_identity": {"path": PREFAB, "guid": PREFAB_GUID, "local_id": PREFAB_LOCAL_ID,
                            "basis": "current Unity AssetDatabase readback"},
        "serialized_authoring_footprint_cells": footprint["asset_serialized_authoring_cells"],
        "temporary_contents_effective_authoring_footprint_cells": footprint["contents_effective_authoring_cells"],
        "asset_visual_footprint_cells": footprint["asset_visual_cells"],
        "temporary_contents_visual_footprint_cells": footprint["contents_visual_cells"],
        "factory_footprint_cells": footprint["factory_cells"],
        "factory_target": footprint["factory_target"],
        "source_pin_count": len(pins),
        "dependency_count": len(rows),
        "loaded_assembly_count": len(assemblies),
        "finalizer_sha256": sha(finalizer_path.read_bytes()),
        "scope": {"mission_scene_opened": False, "temporary_prefab_contents_scene_used": True,
                  "world_created_by_probe": False, "repair_invoked": False,
                  "source_files_changed": False,
                  "runtime_admission": False, "mission_ready": False},
    }


def _no_duplicates(pairs):
    result = {}
    for key, value in pairs:
        need(key not in result, "duplicate JSON key rejected: " + key)
        result[key] = value
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--capture", type=Path, required=True)
    parser.add_argument("--log", type=Path, required=True)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    args = parser.parse_args()
    try:
        receipt = validate(args.repo, args.capture, args.log, args.wrapper_exit)
        output = args.receipt.absolute()
        need(not output.exists() and output.parent.is_dir(), "fresh receipt path in existing directory required")
        with output.open("xb") as stream:
            stream.write((json.dumps(receipt, indent=2) + "\n").encode("utf-8"))
        print(json.dumps({"status": receipt["status"], "source_pins": receipt["source_pin_count"],
                          "dependencies": receipt["dependency_count"], "mission_ready": False}))
        return 0
    except (OSError, UnicodeError, ValueError, TypeError, KeyError, json.JSONDecodeError) as error:
        print("Barrack runtime factory probe finalization failed closed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

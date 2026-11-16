"""Qualify the isolated S073 authoring-metadata export, not mission readiness."""
import argparse
import hashlib
import json
import os
from pathlib import Path


PRESET_PATH = "Assets/Game/Resources/SkirmishIndustrialBasin.asset"
CONSTRUCTION_PATH = "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset"
REQUIRED_NATIVE_METADATA_SOURCES = {
    "Assets/Game/Scripts/Editor/Skirmish/S073ConfiguredCatalogMetadataExport.cs",
    "Assets/Game/Scripts/Editor/Skirmish/S073ConfiguredCatalogMetadataExport.cs.meta",
    "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
    "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs.meta",
    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs.meta",
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def qualify(repo, directory, wrapper_exit):
    repo, directory = Path(repo).resolve(), Path(os.path.abspath(directory))
    log_path = directory / "editor.log"
    catalog_path = directory / "catalog.json"
    log = log_path.read_bytes()
    raw = catalog_path.read_bytes()
    catalog = json.loads(raw)
    marker = ("[S073CatalogMetadataExport] result=Passed entries=6 footprints=6 path=" + str(catalog_path))
    log_text = log.decode("utf-8", errors="strict")
    method = "Game.Editor.S073ConfiguredCatalogMetadataExport.RunS073ConfiguredCatalogMetadataExport"
    if (wrapper_exit != 0 or log_text.splitlines().count(marker) != 1 or
            "COMMAND LINE ARGUMENTS:" not in log_text or method not in log_text):
        raise ValueError("Successful checked wrapper and exporter marker required")
    if (catalog.get("kind") != "S073ConfiguredCatalogMetadataExport" or catalog.get("schema_version") != 1 or
            catalog.get("mission") != "S073" or catalog.get("mission_world_loaded") is not False or
            catalog.get("native_readiness") is not False or
            catalog.get("preset_asset_path") != PRESET_PATH or
            catalog.get("building_placement_source_path") != CONSTRUCTION_PATH or
            catalog.get("footprint_authority") != "OriginalBuildingDefinitionMetadata.FootprintCells"):
        raise ValueError("Exact source-only S073 export required")
    rows = catalog.get("entries")
    names = ["Building_Barrack", "Building_GuardTower", "Building_Ammunition_Depot",
             "Building_OilPump", "Building_Refinery", "Building_Fuel_Bladder"]
    if not isinstance(rows, list) or [row.get("prefab_name") for row in rows] != names:
        raise ValueError("Exact ordered S073 source catalog required")
    pins = catalog.get("source_dependencies_sha256")
    if not isinstance(pins, dict) or not REQUIRED_NATIVE_METADATA_SOURCES.issubset(pins) or \
            PRESET_PATH not in pins or PRESET_PATH + ".meta" not in pins or \
            CONSTRUCTION_PATH not in pins or CONSTRUCTION_PATH + ".meta" not in pins:
        raise ValueError("Capture source dependency pins required")
    for path, expected in pins.items():
        source = (repo / path).resolve(strict=True)
        if not source.is_relative_to(repo) or sha(source.read_bytes()) != expected:
            raise ValueError("Capture source changed: " + path)
    for row in rows:
        footprint = row.get("effective_footprint_cells")
        if (not isinstance(row.get("effective_can_request"), bool) or
                type(row.get("effective_price")) is not int or row["effective_price"] < 0 or
                type(row.get("effective_materials_cost")) is not int or row["effective_materials_cost"] < 0 or
                not isinstance(footprint, dict) or type(footprint.get("x")) is not int or
                type(footprint.get("y")) is not int or footprint["x"] <= 0 or footprint["y"] <= 0 or
                not isinstance(row.get("guid"), str) or len(row["guid"]) != 32 or
                type(row.get("local_file_id")) is not int or row["local_file_id"] == 0 or
                row.get("asset_path") not in pins or row.get("asset_path", "") + ".meta" not in pins):
            raise ValueError("Complete captured prefab identity and metadata required")
    return {"kind": "S073ConfiguredCatalogMetadataReceipt", "schema_version": 1,
            "validation_status": "Passed", "wrapper_exit_code": 0, "required_marker_present": True,
            "catalog_path": str(catalog_path), "catalog_sha256": sha(raw),
            "editor_log_path": str(log_path), "editor_log_sha256": sha(log),
            "construction_path": catalog["building_placement_source_path"],
            "construction_sha256": pins[catalog["building_placement_source_path"]],
            "footprint_authority": catalog["footprint_authority"],
            "footprint_entry_count": len(rows), "source_pin_count": len(pins),
            "mission_ready": False, "scope": "Isolated authored metadata, not a mission world or readiness"}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--repo", required=True)
    p.add_argument("--directory", required=True)
    p.add_argument("--wrapper-exit", type=int, required=True)
    args = p.parse_args()
    receipt = qualify(args.repo, args.directory, args.wrapper_exit)
    with (Path(os.path.abspath(args.directory)) / "catalog-receipt.json").open("x") as output:
        json.dump(receipt, output, indent=2)
        output.write("\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()

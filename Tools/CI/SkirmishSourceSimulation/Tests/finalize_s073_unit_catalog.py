"""Qualify the isolated S073 configured-unit export, not mission readiness."""
import argparse
import hashlib
import json
from pathlib import Path


def sha(data):
    return hashlib.sha256(data).hexdigest()


def qualify(repo, directory, wrapper_exit):
    repo, directory = Path(repo).resolve(), Path(directory).resolve()
    log = (directory / "editor.log").read_bytes()
    raw = (directory / "unit-catalog.json").read_bytes()
    data = json.loads(raw)
    if wrapper_exit != 0 or b"[S073UnitCatalogExport] result=Passed entries=3 " not in log:
        raise ValueError("Successful checked wrapper and unit export marker required")
    if (data.get("kind") != "S073ConfiguredUnitCatalogExport" or data.get("schema_version") != 1 or
            data.get("mission") != "S073" or data.get("mission_world_loaded") is not False or
            data.get("native_readiness") is not False or data.get("configured_building_count") != 6 or
            data.get("preset_asset_path") != "Assets/Game/Resources/SkirmishIndustrialBasin.asset" or
            data.get("building_placement_source_path") != "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset" or
            data.get("unit_registry_source_path") != "Assets/Game/Configs/Skirmish/UnitRegistry.asset"):
        raise ValueError("Exact source-only S073 unit export required")
    rows = data.get("entries")
    expected = ["Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_Truck_Tray", "Unit_Veh_Truck_Tanker"]
    if not isinstance(rows, list) or [row.get("prefab_name") for row in rows] != expected:
        raise ValueError("Exact configured unit order required")
    pins = data.get("source_dependencies_sha256")
    if not isinstance(pins, dict) or len(pins) < 6:
        raise ValueError("Unit capture source dependency pins required")
    for path, expected_hash in pins.items():
        source = (repo / path).resolve(strict=True)
        if not source.is_relative_to(repo) or sha(source.read_bytes()) != expected_hash:
            raise ValueError("Unit capture source changed: " + path)
    seen = set()
    for i, row in enumerate(rows):
        identity = (row.get("guid"), row.get("local_file_id"))
        if (row.get("registry_order") != i or not isinstance(identity[0], str) or len(identity[0]) != 32 or
                type(identity[1]) is not int or identity[1] == 0 or identity in seen or
                not isinstance(row.get("can_request"), bool) or not isinstance(row.get("is_vehicle"), bool) or
                not isinstance(row.get("display_name"), str) or
                any(type(row.get(key)) is not int or row[key] < 0 for key in ("displayed_price", "credits_cost", "materials_cost")) or
                row.get("asset_path") not in pins or row.get("asset_path", "") + ".meta" not in pins):
            raise ValueError("Complete unique configured unit identity and cost required")
        seen.add(identity)
    return {"kind": "S073ConfiguredUnitCatalogReceipt", "schema_version": 1,
            "validation_status": "Passed", "wrapper_exit_code": 0, "required_marker_present": True,
            "catalog_sha256": sha(raw), "editor_log_sha256": sha(log), "source_pin_count": len(pins),
            "mission_ready": False, "scope": "Isolated configured unit metadata, not a mission world or readiness"}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--repo", required=True)
    p.add_argument("--directory", required=True)
    p.add_argument("--wrapper-exit", type=int, required=True)
    args = p.parse_args()
    receipt = qualify(args.repo, args.directory, args.wrapper_exit)
    with (Path(args.directory) / "unit-catalog-receipt.json").open("x") as output:
        json.dump(receipt, output, indent=2)
        output.write("\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()

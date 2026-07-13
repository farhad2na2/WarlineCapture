#!/usr/bin/env python3
"""Build and optionally apply the approved Polygon portrait runtime mapping."""

from __future__ import annotations

import argparse
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
import re
import shutil
import subprocess
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
ASSETS_ROOT = PROJECT_ROOT / "Assets"
CONFIG_ROOT = ASSETS_ROOT / "Game/Configs/Prefabs"
CANDIDATE_ROOT = PROJECT_ROOT / "Design/VisualLockLayered/PortraitPolygonMigration/ProductionCandidates"
APPROVAL_ROOT = PROJECT_ROOT / "Design/VisualLockLayered/PortraitPolygonMigration/ApprovalBatch01"
REPORT_ROOT = PROJECT_ROOT / "Design/VisualLockLayered/PortraitPolygonMigration"

ROLE_FIELDS = {
    "Primary": "portraitSprite",
    "Card": "portraitCardSprite",
    "Action": "portraitActionSprite",
}

APPROVED_OVERRIDES = {
    "Prefab_UnitGrid_Chr_Soldier_Male_01_Config.asset": {
        "Primary": APPROVAL_ROOT / "PPA-01_HeavyGunner_Primary_R1.png",
        "Card": APPROVAL_ROOT / "PPA-02_HeavyGunner_Card_R1.png",
        "Action": APPROVAL_ROOT / "PPA-03_HeavyGunner_Action_R1.png",
    },
    "Prefab_UnitGrid_Veh_Jet_01_Config.asset": {
        "Primary": APPROVAL_ROOT / "PPA-04_StrikeJet_Primary_R4.png",
        "Card": APPROVAL_ROOT / "PPA-05_StrikeJet_Card_R4.png",
        "Action": APPROVAL_ROOT / "PPA-06_StrikeJet_Action_R4.png",
    },
    "Prefab_BuildingDefinition_Building_Barrack_Config.asset": {
        "Primary": APPROVAL_ROOT / "PPA-07_Barracks_Primary_R1.png",
        "Card": APPROVAL_ROOT / "PPA-08_Barracks_Card_R1.png",
        "Action": APPROVAL_ROOT / "PPA-09_Barracks_Action_R1.png",
    },
}

GROUP_MAPPINGS = (
    ("GenericSquad", "SelectionSummary_GenericSquad_Polygon.png", "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_selected_squad_group_portrait.png"),
    ("Soldiers", "SelectionSummary_Soldiers_Polygon.png", "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_rifle_portrait.png"),
    ("Vehicles", "SelectionSummary_Vehicles_Polygon.png", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_VehicleSquad_512.png"),
    ("Aircraft", "SelectionSummary_Aircraft_Polygon.png", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_Aircraft_Polygon_512.png"),
    ("Transports", "SelectionSummary_Transports_Polygon.png", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_Transports_Polygon_512.png"),
    ("Buildings", "SelectionSummary_Buildings_Polygon.png", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_Buildings_Polygon_512.png"),
    ("MixedForce", "SelectionSummary_MixedForce_Polygon.png", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_MixedForce_Polygon_512.png"),
    ("MixedSoldierVehicle", "SelectionSummary_MixedSoldierVehicle_Polygon.png", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_MixedSoldierVehicle_512.png"),
    ("MixedSoldierAircraft", "SelectionSummary_MixedSoldierAircraft_Polygon.png", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_MixedSoldierAircraft_512.png"),
    ("MixedVehicleAircraft", "SelectionSummary_MixedVehicleAircraft_Polygon.png", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_MixedVehicleAircraft_512.png"),
    ("MixedSoldierVehicleAircraft", "SelectionSummary_MixedSoldierVehicleAircraft_Polygon.png", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_MixedSoldierVehicleAircraft_512.png"),
    ("SquadTrayRifle", "SquadTray_RifleSquad_Polygon.png", "Assets/Game/Art/UI/Generated/MatchHUD/SquadTray/SquadTray_Card1_RifleSquad.png"),
    ("SquadTrayArmor", "SquadTray_Armor_Polygon.png", "Assets/Game/Art/UI/Generated/MatchHUD/SquadTray/SquadTray_Card2_CombatVehicles.png"),
    ("SquadTrayGunship", "SquadTray_Gunship_Polygon.png", "Assets/Game/Art/UI/Generated/MatchHUD/SquadTray/SquadTray_Card3_AttackHelicopter.png"),
    ("SquadTrayJetWing", "SquadTray_JetWing_Polygon.png", "Assets/Game/Art/UI/Generated/MatchHUD/SquadTray/SquadTray_Card4_FighterJet.png"),
    ("SquadTrayTransport", "SquadTray_Transport_Polygon.png", "Assets/Game/Art/UI/Generated/MatchHUD/SquadTray/SquadTray_Card5_Transport.png"),
)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def asset_path(path: Path) -> str:
    return path.relative_to(PROJECT_ROOT).as_posix()


def build_guid_index() -> dict[str, Path]:
    result: dict[str, Path] = {}
    guid_pattern = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.MULTILINE)
    for meta_path in ASSETS_ROOT.rglob("*.meta"):
        match = guid_pattern.search(meta_path.read_text(encoding="utf-8", errors="ignore"))
        if not match:
            continue
        guid = match.group(1)
        target = Path(str(meta_path)[:-5])
        if guid in result:
            raise RuntimeError(f"Duplicate GUID {guid}: {result[guid]} and {target}")
        result[guid] = target
    return result


def config_paths() -> list[Path]:
    paths = list(CONFIG_ROOT.glob("Prefab_UnitGrid_Chr_*_Config.asset"))
    paths.extend(CONFIG_ROOT.glob("Prefab_UnitGrid_Veh_*.asset"))
    paths.extend(CONFIG_ROOT.glob("Prefab_BuildingDefinition_*_Config.asset"))
    result = sorted(set(paths))
    if len(result) != 74:
        raise RuntimeError(f"Expected 74 portrait configs, found {len(result)}")
    return result


def building_key(config_name: str) -> str:
    key = config_name.removeprefix("Prefab_BuildingDefinition_").removesuffix("_Config.asset")
    key = key.rstrip("_")
    if key == "OilRefinery":
        return "Building_Refinery"
    if key == "OilRefinery_Big":
        return "Building_Refinery_Big"
    if key.startswith("Building_"):
        return key
    return f"Building_{key}"


def entity_key(config_path: Path) -> str:
    name = config_path.name
    if name.startswith("Prefab_BuildingDefinition_"):
        return building_key(name)
    return name.removeprefix("Prefab_UnitGrid_").removesuffix("_Config.asset").removesuffix(".asset")


def candidate_path(config_path: Path, role: str) -> Path:
    override = APPROVED_OVERRIDES.get(config_path.name)
    if override:
        return override[role]
    key = entity_key(config_path)
    category = "Buildings" if key.startswith("Building_") else "Characters" if key.startswith("Chr_") else "Vehicles"
    prefix = key if key.startswith("Building_") else f"Unit_{key}"
    return CANDIDATE_ROOT / category / f"{prefix}_{role}.png"


def new_entity_target(config_path: Path, role: str) -> Path:
    key = entity_key(config_path)
    if role == "Primary":
        return PROJECT_ROOT / "Assets/Game/Art/UI/Portraits/Generated" / f"Portrait_{key}_Polygon.png"
    return PROJECT_ROOT / "Assets/Game/Art/UI/Portraits/Secondary" / f"Portrait_{key}_{role}_512.png"


def read_role_guid(config_text: str, field: str) -> str | None:
    match = re.search(rf"^\s*{re.escape(field)}:\s*\{{[^}}]*guid:\s*([0-9a-f]{{32}})[^}}]*\}}\s*$", config_text, re.MULTILINE)
    return match.group(1) if match else None


def image_info(path: Path) -> tuple[int, int, str]:
    process = subprocess.run(
        ["magick", "identify", "-format", "%w %h %[channels]", str(path)],
        check=True,
        capture_output=True,
        text=True,
    )
    width, height, channels = process.stdout.strip().split(maxsplit=2)
    return int(width), int(height), channels


def validate_candidate(path: Path, role: str) -> dict[str, object]:
    if not path.is_file():
        raise RuntimeError(f"Missing candidate: {asset_path(path)}")
    width, height, channels = image_info(path)
    if width != height:
        raise RuntimeError(f"Candidate is not square: {asset_path(path)} ({width}x{height})")
    if role == "Primary" and "a" not in channels.lower():
        raise RuntimeError(f"Primary candidate has no alpha channel: {asset_path(path)} ({channels})")
    return {"width": width, "height": height, "channels": channels}


def build_mapping() -> tuple[list[dict[str, object]], list[dict[str, object]]]:
    guid_index = build_guid_index()
    entity_mappings: list[dict[str, object]] = []
    for config_path in config_paths():
        config_text = config_path.read_text(encoding="utf-8")
        for role, field in ROLE_FIELDS.items():
            source = candidate_path(config_path, role)
            source_info = validate_candidate(source, role)
            guid = read_role_guid(config_text, field)
            target = guid_index.get(guid) if guid else None
            if guid and target is None:
                raise RuntimeError(f"Unresolved {role} GUID {guid} in {asset_path(config_path)}")
            if target is None:
                target = new_entity_target(config_path, role)
            if target.suffix.lower() != ".png":
                raise RuntimeError(f"Portrait target is not PNG: {asset_path(target)}")
            meta = Path(f"{target}.meta")
            target_was_existing = guid is not None
            if target_was_existing and not target.is_file():
                raise RuntimeError(f"Assigned portrait target is missing: {asset_path(target)}")
            entity_mappings.append({
                "kind": "entity",
                "config": asset_path(config_path),
                "entityKey": entity_key(config_path),
                "role": role,
                "field": field,
                "source": asset_path(source),
                "sourceInfo": source_info,
                "target": asset_path(target),
                "existingGuid": guid,
                "targetExisted": target_was_existing,
                "metaExisted": meta.is_file(),
                "metaSha256Before": sha256(meta) if meta.is_file() else None,
            })

    group_mappings: list[dict[str, object]] = []
    for name, source_name, target_name in GROUP_MAPPINGS:
        source = CANDIDATE_ROOT / "MatchHUD" / source_name
        source_info = validate_candidate(source, "Group")
        target = PROJECT_ROOT / target_name
        meta = Path(f"{target}.meta")
        target_was_existing = meta.is_file()
        if target_was_existing and not target.is_file():
            raise RuntimeError(f"Assigned group portrait target is missing: {asset_path(target)}")
        group_mappings.append({
            "kind": "group",
            "name": name,
            "role": "Group",
            "source": asset_path(source),
            "sourceInfo": source_info,
            "target": asset_path(target),
            "targetExisted": target_was_existing,
            "metaExisted": meta.is_file(),
            "metaSha256Before": sha256(meta) if meta.is_file() else None,
        })

    if len(entity_mappings) != 222 or len(group_mappings) != 16:
        raise RuntimeError(f"Expected 222 entity and 16 group mappings, got {len(entity_mappings)} and {len(group_mappings)}")
    if sum(1 for item in entity_mappings if item["targetExisted"]) != 208:
        raise RuntimeError("Expected exactly 208 existing entity portrait targets")
    if sum(1 for item in entity_mappings if not item["targetExisted"]) != 14:
        raise RuntimeError("Expected exactly 14 new entity portrait targets")
    if sum(1 for item in group_mappings if item["targetExisted"]) != 12:
        raise RuntimeError("Expected exactly 12 existing group portrait targets")
    if sum(1 for item in group_mappings if not item["targetExisted"]) != 4:
        raise RuntimeError("Expected exactly 4 new group portrait targets")

    targets = [item["target"] for item in entity_mappings + group_mappings]
    if len(targets) != len(set(targets)):
        raise RuntimeError("Runtime mapping contains duplicate target paths")
    return entity_mappings, group_mappings


def write_reports(entity_mappings: list[dict[str, object]], group_mappings: list[dict[str, object]]) -> None:
    REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    all_mappings = entity_mappings + group_mappings
    report = {
        "schemaVersion": 1,
        "totals": {
            "entity": len(entity_mappings),
            "group": len(group_mappings),
            "all": len(all_mappings),
            "existingTargets": sum(1 for item in all_mappings if item["targetExisted"]),
            "newTargets": sum(1 for item in all_mappings if not item["targetExisted"]),
        },
        "mappings": all_mappings,
    }
    (REPORT_ROOT / "runtime_mapping_report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    meta_hashes = {
        item["target"] + ".meta": item["metaSha256Before"]
        for item in all_mappings
        if item["metaExisted"]
    }
    (REPORT_ROOT / "runtime_meta_sha256_before.json").write_text(json.dumps(meta_hashes, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def render_512(source: Path, target: Path, preserve_alpha: bool) -> None:
    target.parent.mkdir(parents=True, exist_ok=True)
    command = ["magick", str(source), "-resize", "512x512", "-depth", "8", "-strip"]
    if preserve_alpha:
        command.extend(["-alpha", "on", "-define", "png:color-type=6"])
    else:
        command.extend(["-alpha", "off", "-define", "png:color-type=2"])
    temporary = target.with_name(target.name + ".polygon-migration.tmp.png")
    command.append(str(temporary))
    subprocess.run(command, check=True)
    width, height, channels = image_info(temporary)
    if (width, height) != (512, 512):
        temporary.unlink(missing_ok=True)
        raise RuntimeError(f"Rendered image has wrong size: {temporary} ({width}x{height})")
    if preserve_alpha and "a" not in channels.lower():
        temporary.unlink(missing_ok=True)
        raise RuntimeError(f"Rendered Primary lost alpha: {temporary} ({channels})")
    temporary.replace(target)


def apply_mapping(entity_mappings: list[dict[str, object]], group_mappings: list[dict[str, object]]) -> None:
    if shutil.which("magick") is None:
        raise RuntimeError("ImageMagick 'magick' is required")

    def apply_item(item: dict[str, object]) -> None:
        source = PROJECT_ROOT / str(item["source"])
        target = PROJECT_ROOT / str(item["target"])
        render_512(source, target, item["role"] == "Primary")

    with ThreadPoolExecutor(max_workers=6) as executor:
        list(executor.map(apply_item, entity_mappings + group_mappings))


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true", help="Write the validated 512x512 images to runtime targets")
    args = parser.parse_args()

    entity_mappings, group_mappings = build_mapping()
    write_reports(entity_mappings, group_mappings)
    if args.apply:
        apply_mapping(entity_mappings, group_mappings)
    action = "applied" if args.apply else "validated"
    print(f"Polygon portrait mapping {action}: 222 entity + 16 group assets")


if __name__ == "__main__":
    main()

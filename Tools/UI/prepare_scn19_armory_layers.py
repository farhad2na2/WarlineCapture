#!/usr/bin/env python3
from __future__ import annotations

import json
import shutil
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageOps


ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "Design" / "VisualLockLayered" / "SCN-19_Armory"
SOURCE_DIR = PACK / "generated_one_go" / "source"
LAYER_DIR = PACK / "layers"
VALIDATION_DIR = PACK / "validation"

SOURCE_FILES = {
    "background": Path("/Users/farhad/.codex/generated_images/019e086f-4328-7861-9cbb-46d15339d257/ig_06ff61372c440d29016a1180ea0eac8191a0fdeb33dcdb2c0f.png"),
    "frames": Path("/Users/farhad/.codex/generated_images/019e086f-4328-7861-9cbb-46d15339d257/ig_06ff61372c440d29016a11813afb40819193a561184f4644d5.png"),
    "icons": Path("/Users/farhad/.codex/generated_images/019e086f-4328-7861-9cbb-46d15339d257/ig_06ff61372c440d29016a11819b27d88191b29175d80d24bc45.png"),
    "roster_art": Path("/Users/farhad/.codex/generated_images/019e086f-4328-7861-9cbb-46d15339d257/ig_06ff61372c440d29016a1181efb128819185f2ec7f8c742503.png"),
}

SOURCE_NAMES = {
    "background": "SCN-19_Background_21x9_NoUI.png",
    "frames": "SCN-19_Frames_Green.png",
    "icons": "SCN-19_Icons_Green.png",
    "roster_art": "SCN-19_RosterArt_RectTiles.png",
}

FRAME_BOXES = [
    ("scn19_header_logo_panel_bg", (28, 31, 548, 126), "Header/LogoPanel", "alpha_png"),
    ("scn19_header_resource_panel_bg", (570, 31, 1054, 126), "Header/ResourcePanel", "alpha_png"),
    ("scn19_header_right_actions_bg", (1088, 31, 1504, 126), "Header/RightActions", "alpha_png"),
    ("scn19_title_back_panel_frame", (35, 148, 414, 238), "Header/TitleBackPanel", "alpha_png"),
    ("scn19_category_button_selected_frame", (445, 154, 736, 232), "CategoryRail/SelectedButton", "alpha_png"),
    ("scn19_category_button_default_frame", (770, 154, 1077, 232), "CategoryRail/DefaultButton", "alpha_png"),
    ("scn19_dropdown_frame", (1155, 154, 1402, 232), "Roster/DropdownFrame", "alpha_png"),
    ("scn19_roster_card_selected_frame", (46, 272, 338, 686), "Roster/CardSelectedFrame", "alpha_png"),
    ("scn19_roster_card_default_frame", (382, 272, 670, 686), "Roster/CardDefaultFrame", "alpha_png"),
    ("scn19_roster_card_locked_frame", (712, 272, 998, 686), "Roster/CardLockedFrame", "alpha_png"),
    ("scn19_inspection_panel_frame", (1040, 256, 1488, 820), "Inspection/PanelFrame", "alpha_png"),
    ("scn19_bottom_tab_selected_frame", (43, 725, 222, 802), "BottomTabs/SelectedFrame", "alpha_png"),
    ("scn19_bottom_tab_default_frame", (247, 725, 423, 802), "BottomTabs/DefaultFrame", "alpha_png"),
    ("scn19_cta_primary_gold_frame", (454, 725, 626, 802), "Inspection/PrimaryCTAFrame", "alpha_png"),
    ("scn19_cta_secondary_dark_frame", (654, 725, 812, 802), "Inspection/SecondaryCTAFrame", "alpha_png"),
    ("scn19_cta_disabled_frame", (842, 725, 976, 802), "Inspection/DisabledCTAFrame", "alpha_png"),
    ("scn19_progress_meter_empty_frame", (49, 841, 388, 870), "Meters/EmptyFrame", "alpha_png"),
    ("scn19_small_status_chip_frame", (416, 836, 812, 878), "Common/StatusChipFrame", "alpha_png"),
    ("scn19_small_counter_chip_frame", (845, 836, 970, 878), "Common/CounterChipFrame", "alpha_png"),
    ("scn19_route_breadcrumb_strip_frame", (40, 908, 1010, 980), "Footer/BreadcrumbStrip", "alpha_png"),
    ("scn19_comms_status_panel_frame", (1044, 848, 1488, 963), "Footer/CommsPanel", "alpha_png"),
]

ICON_IDS = [
    "scn19_icon_back_arrow",
    "scn19_icon_armory_crossed_weapons",
    "scn19_icon_units_group",
    "scn19_icon_vehicle_truck",
    "scn19_icon_aircraft_helicopter",
    "scn19_icon_buildings",
    "scn19_icon_support_plus",
    "scn19_icon_upgrades_chevrons",
    "scn19_resource_credits_coin",
    "scn19_resource_supplies_crate",
    "scn19_resource_command_shield",
    "scn19_icon_inbox_envelope",
    "scn19_icon_settings_gear",
    "scn19_icon_dropdown_chevron",
    "scn19_badge_owned_checkmark",
    "scn19_badge_locked_padlock",
    "scn19_badge_upgrade_ready_chevrons",
    "scn19_icon_health_cross",
    "scn19_icon_damage_burst",
    "scn19_icon_range_reticle",
    "scn19_icon_speed_boot",
    "scn19_icon_move_runner",
    "scn19_icon_attack_reticle",
    "scn19_icon_hold_shield",
    "scn19_icon_patrol_chevrons",
    "scn19_icon_blueprint_parts",
    "scn19_icon_source_building",
    "scn19_selected_glow_strip",
    "scn19_progress_fill_gold_segment",
    "scn19_progress_fill_olive_segment",
    "scn19_icon_disabled_slash",
    "scn19_icon_comms_signal",
]

ART_IDS = [
    "scn19_art_rifleman_male_ii",
    "scn19_art_marksman_male_i",
    "scn19_art_assault_breacher_female_ii",
    "scn19_art_field_commander",
    "scn19_art_cargo_truck",
    "scn19_art_canopy_truck",
    "scn19_art_attack_helicopter",
    "scn19_art_transport_helicopter",
    "scn19_art_oil_pump",
    "scn19_art_oil_refinery",
    "scn19_art_guard_tower",
    "scn19_art_ammunition_depot",
]


def is_key_green(arr: np.ndarray) -> np.ndarray:
    r = arr[..., 0].astype(np.int16)
    g = arr[..., 1].astype(np.int16)
    b = arr[..., 2].astype(np.int16)
    return (g > 95) & (g - r > 35) & (g - b > 35) & (r < 155) & (b < 155)


def crop_green_layer(source: Image.Image, box: tuple[int, int, int, int], out_path: Path) -> dict:
    crop = source.crop(box).convert("RGBA")
    arr = np.array(crop)
    green = is_key_green(arr[..., :3])
    arr[..., 3] = np.where(green, 0, arr[..., 3])
    ys, xs = np.where(arr[..., 3] > 0)
    if len(xs) and len(ys):
        pad = 2
        x0 = max(int(xs.min()) - pad, 0)
        x1 = min(int(xs.max()) + 1 + pad, arr.shape[1])
        y0 = max(int(ys.min()) - pad, 0)
        y1 = min(int(ys.max()) + 1 + pad, arr.shape[0])
        arr = arr[y0:y1, x0:x1]
        box = (box[0] + x0, box[1] + y0, box[0] + x1, box[1] + y1)
    Image.fromarray(arr).save(out_path)
    return {"source_box": list(box), "size": {"width": int(arr.shape[1]), "height": int(arr.shape[0])}}


def connected_components(mask: np.ndarray, min_area: int = 300) -> list[tuple[int, int, int, int, int]]:
    h, w = mask.shape
    seen = np.zeros_like(mask, dtype=bool)
    comps: list[tuple[int, int, int, int, int]] = []
    for y in range(h):
        for x in range(w):
            if not mask[y, x] or seen[y, x]:
                continue
            q = deque([(x, y)])
            seen[y, x] = True
            x0 = x1 = x
            y0 = y1 = y
            area = 0
            while q:
                cx, cy = q.popleft()
                area += 1
                x0 = min(x0, cx)
                x1 = max(x1, cx)
                y0 = min(y0, cy)
                y1 = max(y1, cy)
                for nx in (cx - 1, cx, cx + 1):
                    for ny in (cy - 1, cy, cy + 1):
                        if nx < 0 or ny < 0 or nx >= w or ny >= h:
                            continue
                        if seen[ny, nx] or not mask[ny, nx]:
                            continue
                        seen[ny, nx] = True
                        q.append((nx, ny))
            if area >= min_area:
                comps.append((x0, y0, x1 + 1, y1 + 1, area))
    return comps


def extract_icons(source: Image.Image, layers: list[dict]) -> None:
    rgba = source.convert("RGBA")
    arr = np.array(rgba)
    foreground = ~is_key_green(arr[..., :3])
    dilated = Image.fromarray((foreground * 255).astype("uint8")).filter(ImageFilter.MaxFilter(29))
    comps = connected_components(np.array(dilated) > 0, min_area=1400)
    comps = sorted(comps, key=lambda b: (b[1] // 120, b[0]))
    if len(comps) < len(ICON_IDS):
        raise RuntimeError(f"Expected at least {len(ICON_IDS)} icon components, found {len(comps)}")
    comps = comps[: len(ICON_IDS)]
    for layer_id, (x0, y0, x1, y1, _) in zip(ICON_IDS, comps):
        pad = 8
        box = (
            max(x0 - pad, 0),
            max(y0 - pad, 0),
            min(x1 + pad, rgba.width),
            min(y1 + pad, rgba.height),
        )
        out_path = LAYER_DIR / f"{layer_id}.png"
        info = crop_green_layer(rgba, box, out_path)
        if layer_id == "scn19_selected_glow_strip":
            make_selected_glow(out_path)
            im = Image.open(out_path)
            info = {"source_box": list(box), "size": {"width": im.width, "height": im.height}}
        layers.append(make_layer(layer_id, out_path, SOURCE_DIR / SOURCE_NAMES["icons"], "Icons", True, **info))


def make_selected_glow(out_path: Path) -> None:
    w, h = 320, 40
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(glow)
    draw.rounded_rectangle((12, 14, w - 12, 25), radius=5, fill=(218, 163, 35, 210))
    glow = glow.filter(ImageFilter.GaussianBlur(7))
    im.alpha_composite(glow)
    draw = ImageDraw.Draw(im)
    draw.rounded_rectangle((18, 17, w - 18, 22), radius=3, fill=(235, 197, 62, 240))
    out_path.parent.mkdir(parents=True, exist_ok=True)
    im.save(out_path)


def extract_roster_art(source: Image.Image, layers: list[dict]) -> None:
    img = source.convert("RGB")
    boxes = []
    xs = [24, 376, 728, 1080]
    ys = [32, 382, 732]
    tile_w = 340
    tile_h = 338
    for y in ys:
        for x in xs:
            boxes.append((x, y, min(x + tile_w, img.width), min(y + tile_h, img.height)))
    for layer_id, box in zip(ART_IDS, boxes):
        out_path = LAYER_DIR / f"{layer_id}.png"
        img.crop(box).save(out_path)
        layers.append(make_layer(layer_id, out_path, SOURCE_DIR / SOURCE_NAMES["roster_art"], "RosterArt", False, source_box=list(box), size={"width": box[2] - box[0], "height": box[3] - box[1]}))


def make_layer(layer_id: str, out_path: Path, source: Path, role: str, key_green: bool, source_box=None, size=None, unity_destination=None, alpha_rule=None) -> dict:
    if size is None:
        im = Image.open(out_path)
        size = {"width": im.width, "height": im.height}
    return {
        "id": layer_id,
        "file": str(out_path),
        "source": str(source),
        "source_box": source_box,
        "size": size,
        "role": role,
        "unityDestination": unity_destination or role,
        "spriteImport": "Single",
        "alphaRule": alpha_rule or ("chroma_key_extracted" if key_green else "opaque_rect"),
        "liveText": "No baked dynamic text; labels and values are TMP/runtime-bound."
    }


def build_contact_sheet(layer_paths: list[Path]) -> None:
    thumbs = []
    for path in layer_paths:
        im = Image.open(path).convert("RGBA")
        bg = Image.new("RGBA", im.size, (0, 0, 0, 0))
        checker = Image.new("RGBA", im.size, (230, 230, 230, 255))
        draw = ImageDraw.Draw(checker)
        cell = 16
        for y in range(0, im.height, cell):
            for x in range(0, im.width, cell):
                if (x // cell + y // cell) % 2:
                    draw.rectangle([x, y, x + cell - 1, y + cell - 1], fill=(180, 180, 180, 255))
        bg.alpha_composite(checker)
        bg.alpha_composite(im)
        bg.thumbnail((220, 150), Image.Resampling.LANCZOS)
        tile = Image.new("RGB", (240, 190), (24, 26, 23))
        tile.paste(bg.convert("RGB"), ((240 - bg.width) // 2, 8))
        draw = ImageDraw.Draw(tile)
        name = path.stem.replace("scn19_", "")
        draw.text((10, 162), name[:34], fill=(220, 220, 200))
        thumbs.append(tile)
    cols = 5
    rows = (len(thumbs) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * 240, rows * 190), (12, 13, 12))
    for i, tile in enumerate(thumbs):
        sheet.paste(tile, ((i % cols) * 240, (i // cols) * 190))
    out = PACK / "generated_one_go" / "layers_contact_sheet.png"
    sheet.save(out)
    sheet.save(VALIDATION_DIR / "SCN-19_Armory_layers_contact_sheet.png")


def main() -> None:
    SOURCE_DIR.mkdir(parents=True, exist_ok=True)
    LAYER_DIR.mkdir(parents=True, exist_ok=True)
    VALIDATION_DIR.mkdir(parents=True, exist_ok=True)

    for key, src in SOURCE_FILES.items():
        shutil.copy2(src, SOURCE_DIR / SOURCE_NAMES[key])

    layers: list[dict] = []

    bg_src = SOURCE_DIR / SOURCE_NAMES["background"]
    bg_out = LAYER_DIR / "scn19_background_21x9_no_ui.png"
    shutil.copy2(bg_src, bg_out)
    layers.append(make_layer("scn19_background_21x9_no_ui", bg_out, bg_src, "Background", False))

    frame_src = Image.open(SOURCE_DIR / SOURCE_NAMES["frames"])
    for layer_id, box, role, alpha_rule in FRAME_BOXES:
        out_path = LAYER_DIR / f"{layer_id}.png"
        info = crop_green_layer(frame_src, box, out_path)
        layers.append(make_layer(layer_id, out_path, SOURCE_DIR / SOURCE_NAMES["frames"], role, True, alpha_rule=alpha_rule, **info))

    extract_icons(Image.open(SOURCE_DIR / SOURCE_NAMES["icons"]), layers)
    extract_roster_art(Image.open(SOURCE_DIR / SOURCE_NAMES["roster_art"]), layers)

    manifest = {
        "surface_id": "SCN-19_Armory",
        "workflow": "VisualLockLayered V15 3D Green-Source Extraction",
        "target_reference": str(PACK / "reference" / "SCN-19_Armory_Landscape_Target.png"),
        "rule": "Generated implementation sources only. The target-lock mockup was not sliced for layers.",
        "sources": [str(SOURCE_DIR / SOURCE_NAMES[key]) for key in ("background", "frames", "icons", "roster_art")],
        "layers": layers,
        "runtimeBindings": {
            "liveText": [
                "resource labels and values",
                "screen title and subtitle",
                "category names and counts",
                "filter and sort labels",
                "roster display names, roles, levels, owned/locked states",
                "inspection display name, description, stats, abilities, upgrade progress, source text",
                "CTA labels",
                "bottom tabs and breadcrumb"
            ],
            "runtimeData": [
                "PlayerInventory",
                "prefab displayName and description",
                "UnlockState",
                "CombatCatalog ids",
                "AbilityConfig",
                "UpgradeTrackConfig",
                "BlueprintParts",
                "GearModule inventory",
                "wallet Credits/Supplies/Command"
            ]
        }
    }
    (PACK / "layer_manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")

    build_contact_sheet([Path(layer["file"]) for layer in layers])

    # Backward-compatible copy mirroring older generated_v01 packs.
    generated_v01 = PACK / "generated_v01"
    if generated_v01.exists():
        shutil.rmtree(generated_v01)
    shutil.copytree(PACK / "generated_one_go", generated_v01)
    shutil.copytree(LAYER_DIR, generated_v01 / "layers")
    shutil.copy2(PACK / "layer_manifest.json", generated_v01 / "layer_manifest.json")
    (generated_v01 / "README.md").write_text(
        "# SCN-19 Armory Generated V01\n\n"
        "Generated implementation sources only. The target-lock reference was not sliced for layers.\n"
        "Use `layer_manifest.json` for Unity Canvas conversion.\n"
    )


if __name__ == "__main__":
    main()

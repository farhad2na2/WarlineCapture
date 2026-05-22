#!/usr/bin/env python3
"""Prepare SCN-08 RTS Battle HUD V01 VisualLockLayered sprites.

This extractor uses generated implementation source sheets only. It does not
slice or crop the target-lock mockup.
"""

from __future__ import annotations

import json
import shutil
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "Design" / "VisualLockLayered" / "SCN-08_RTSBattleHUD"
SOURCE = PACK / "generated_v01" / "source"
LAYERS = PACK / "generated_v01" / "layers"
VALIDATION = PACK / "generated_v01" / "validation"
ROOT_LAYERS = PACK / "layers"
ROOT_VALIDATION = PACK / "validation"


@dataclass(frozen=True)
class LayerSpec:
    name: str
    source: str
    box: tuple[int, int, int, int] | None = None
    key_green: bool = True
    trim: bool = True
    pad: int = 4


ART_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("scn08_battlefield_21x9_no_ui", "SCN-08_Battlefield_21x9_NoUI.png", None, False, False, 0),
    LayerSpec("scn08_minimap_content", "SCN-08_MinimapContent.png", None, False, False, 0),
)


FRAME_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("scn08_objective_panel_frame", "SCN-08_Frames_Green.png", (20, 42, 361, 260)),
    LayerSpec("scn08_title_banner_frame", "SCN-08_Frames_Green.png", (415, 44, 699, 112)),
    LayerSpec("scn08_resource_strip_frame", "SCN-08_Frames_Green.png", (750, 43, 1467, 113)),
    LayerSpec("scn08_top_icon_button_frame_a", "SCN-08_Frames_Green.png", (1506, 42, 1575, 104)),
    LayerSpec("scn08_top_icon_button_frame_b", "SCN-08_Frames_Green.png", (1587, 42, 1655, 104)),
    LayerSpec("scn08_command_mode_banner_frame", "SCN-08_Frames_Green.png", (750, 141, 1317, 211)),
    LayerSpec("scn08_jump_button_frame", "SCN-08_Frames_Green.png", (1342, 141, 1493, 204)),
    LayerSpec("scn08_selected_entity_portrait_frame", "SCN-08_Frames_Green.png", (433, 143, 650, 462)),
    LayerSpec("scn08_right_quick_panel_frame", "SCN-08_Frames_Green.png", (1507, 244, 1648, 886)),
    LayerSpec("scn08_threat_row_frame", "SCN-08_Frames_Green.png", (819, 245, 1140, 322)),
    LayerSpec("scn08_side_quick_button_frame", "SCN-08_Frames_Green.png", (1421, 277, 1491, 342)),
    LayerSpec("scn08_selected_entity_panel_frame", "SCN-08_Frames_Green.png", (18, 288, 277, 789)),
    LayerSpec("scn08_rule_toast_chip_frame", "SCN-08_Frames_Green.png", (786, 355, 1161, 421)),
    LayerSpec("scn08_invalid_command_toast_frame", "SCN-08_Frames_Green.png", (743, 446, 1225, 523)),
    LayerSpec("scn08_squad_tray_frame", "SCN-08_Frames_Green.png", (299, 495, 720, 571)),
    LayerSpec("scn08_squad_card_normal_frame", "SCN-08_Frames_Green.png", (293, 609, 429, 875)),
    LayerSpec("scn08_squad_card_selected_frame", "SCN-08_Frames_Green.png", (438, 608, 578, 875)),
    LayerSpec("scn08_command_bar_rail_frame", "SCN-08_Frames_Green.png", (594, 610, 1087, 698)),
    LayerSpec("scn08_minimap_panel_frame", "SCN-08_Frames_Green.png", (1103, 578, 1408, 889)),
    LayerSpec("scn08_minimap_zoom_button_frame", "SCN-08_Frames_Green.png", (1419, 692, 1494, 775)),
    LayerSpec("scn08_command_button_normal_frame", "SCN-08_Frames_Green.png", (593, 726, 672, 833)),
    LayerSpec("scn08_command_button_selected_frame", "SCN-08_Frames_Green.png", (675, 726, 756, 833)),
    LayerSpec("scn08_command_button_frame_alt_1", "SCN-08_Frames_Green.png", (758, 726, 838, 833)),
    LayerSpec("scn08_command_button_frame_alt_2", "SCN-08_Frames_Green.png", (841, 726, 921, 833)),
    LayerSpec("scn08_command_button_frame_alt_3", "SCN-08_Frames_Green.png", (924, 726, 1004, 833)),
    LayerSpec("scn08_command_button_frame_alt_4", "SCN-08_Frames_Green.png", (1006, 726, 1087, 833)),
    LayerSpec("scn08_ability_chip_frame", "SCN-08_Frames_Green.png", (28, 808, 100, 875)),
    LayerSpec("scn08_small_square_button_frame", "SCN-08_Frames_Green.png", (110, 808, 182, 875)),
)


ICON_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("scn08_icon_objective_list", "SCN-08_Icons_Green.png", (70, 60, 210, 180)),
    LayerSpec("scn08_icon_checkbox_empty", "SCN-08_Icons_Green.png", (250, 60, 390, 190)),
    LayerSpec("scn08_icon_checkbox_checked", "SCN-08_Icons_Green.png", (430, 60, 590, 190)),
    LayerSpec("scn08_icon_objective_star", "SCN-08_Icons_Green.png", (620, 50, 785, 190)),
    LayerSpec("scn08_icon_timer_clock", "SCN-08_Icons_Green.png", (805, 50, 975, 195)),
    LayerSpec("scn08_icon_threat_warning", "SCN-08_Icons_Green.png", (1015, 50, 1180, 195)),
    LayerSpec("scn08_icon_hostile_diamond", "SCN-08_Icons_Green.png", (1215, 30, 1400, 205)),
    LayerSpec("scn08_icon_jump_arrow", "SCN-08_Icons_Green.png", (1410, 45, 1630, 195)),
    LayerSpec("scn08_resource_credits_coin", "SCN-08_Icons_Green.png", (60, 220, 215, 385)),
    LayerSpec("scn08_resource_fuel_can", "SCN-08_Icons_Green.png", (250, 220, 395, 385)),
    LayerSpec("scn08_resource_supply_crate", "SCN-08_Icons_Green.png", (430, 220, 610, 390)),
    LayerSpec("scn08_icon_civilian_group", "SCN-08_Icons_Green.png", (620, 220, 790, 390)),
    LayerSpec("scn08_icon_pause", "SCN-08_Icons_Green.png", (820, 220, 980, 390)),
    LayerSpec("scn08_icon_settings_gear", "SCN-08_Icons_Green.png", (1015, 220, 1185, 390)),
    LayerSpec("scn08_icon_build_tools", "SCN-08_Icons_Green.png", (1200, 220, 1390, 390)),
    LayerSpec("scn08_icon_support_parachute", "SCN-08_Icons_Green.png", (1435, 220, 1615, 395)),
    LayerSpec("scn08_command_select_cursor", "SCN-08_Icons_Green.png", (50, 385, 210, 565)),
    LayerSpec("scn08_command_move_chevrons", "SCN-08_Icons_Green.png", (235, 385, 405, 565)),
    LayerSpec("scn08_command_attack_crosshair", "SCN-08_Icons_Green.png", (445, 385, 615, 565)),
    LayerSpec("scn08_command_hold_shield", "SCN-08_Icons_Green.png", (630, 385, 790, 565)),
    LayerSpec("scn08_command_stop_hand", "SCN-08_Icons_Green.png", (815, 385, 970, 565)),
    LayerSpec("scn08_command_scan_radar", "SCN-08_Icons_Green.png", (990, 385, 1165, 565)),
    LayerSpec("scn08_command_board_vehicle", "SCN-08_Icons_Green.png", (1180, 395, 1380, 565)),
    LayerSpec("scn08_health_bar_small_frame", "SCN-08_Icons_Green.png", (1390, 395, 1660, 560)),
    LayerSpec("scn08_health_bar_frame", "SCN-08_Icons_Green.png", (45, 570, 325, 690)),
    LayerSpec("scn08_status_segment_strip", "SCN-08_Icons_Green.png", (345, 570, 640, 690)),
    LayerSpec("scn08_icon_shield_rank_badge", "SCN-08_Icons_Green.png", (660, 550, 795, 725)),
    LayerSpec("scn08_squad_number_badge_frame", "SCN-08_Icons_Green.png", (835, 570, 975, 710)),
    LayerSpec("scn08_minimap_north_arrow", "SCN-08_Icons_Green.png", (1000, 570, 1160, 710)),
    LayerSpec("scn08_minimap_zoom_plus_icon", "SCN-08_Icons_Green.png", (1180, 570, 1305, 710)),
    LayerSpec("scn08_minimap_zoom_minus_icon", "SCN-08_Icons_Green.png", (1335, 570, 1460, 710)),
    LayerSpec("scn08_minimap_focus_target_icon", "SCN-08_Icons_Green.png", (1490, 570, 1615, 710)),
    LayerSpec("scn08_icon_menu_list", "SCN-08_Icons_Green.png", (55, 735, 200, 870)),
    LayerSpec("scn08_icon_invalid_warning", "SCN-08_Icons_Green.png", (240, 730, 385, 875)),
    LayerSpec("scn08_current_order_chevrons", "SCN-08_Icons_Green.png", (415, 735, 600, 865)),
)


MARKER_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("scn08_marker_selection_ring", "SCN-08_WorldMarkers_Green.png", (30, 30, 610, 230)),
    LayerSpec("scn08_marker_move_destination", "SCN-08_WorldMarkers_Green.png", (620, 30, 970, 230)),
    LayerSpec("scn08_marker_path_line", "SCN-08_WorldMarkers_Green.png", (990, 45, 1625, 230)),
    LayerSpec("scn08_marker_attack_target_ring", "SCN-08_WorldMarkers_Green.png", (55, 250, 345, 505)),
    LayerSpec("scn08_marker_hostile_diamond", "SCN-08_WorldMarkers_Green.png", (410, 250, 620, 520)),
    LayerSpec("scn08_marker_objective_star_pin", "SCN-08_WorldMarkers_Green.png", (745, 245, 915, 535)),
    LayerSpec("scn08_marker_civilian_risk_zone", "SCN-08_WorldMarkers_Green.png", (1030, 245, 1300, 535)),
    LayerSpec("scn08_marker_threat_warning_pin", "SCN-08_WorldMarkers_Green.png", (1400, 245, 1605, 515)),
    LayerSpec("scn08_marker_minimap_viewport_rect", "SCN-08_WorldMarkers_Green.png", (55, 530, 435, 720)),
    LayerSpec("scn08_marker_friendly_minimap_dot", "SCN-08_WorldMarkers_Green.png", (485, 540, 610, 665)),
    LayerSpec("scn08_marker_hostile_minimap_dot", "SCN-08_WorldMarkers_Green.png", (620, 540, 740, 665)),
    LayerSpec("scn08_marker_civilian_minimap_dot", "SCN-08_WorldMarkers_Green.png", (750, 540, 875, 665)),
    LayerSpec("scn08_marker_build_valid_footprint", "SCN-08_WorldMarkers_Green.png", (900, 520, 1305, 720)),
    LayerSpec("scn08_marker_invalid_command_x", "SCN-08_WorldMarkers_Green.png", (1340, 510, 1605, 735)),
    LayerSpec("scn08_marker_scan_ping_ring", "SCN-08_WorldMarkers_Green.png", (360, 690, 720, 910)),
    LayerSpec("scn08_marker_command_focus_brackets", "SCN-08_WorldMarkers_Green.png", (820, 705, 1290, 910)),
)


PORTRAIT_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("scn08_portrait_rifle_squad", "SCN-08_SquadPortraits_Green.png", (36, 219, 497, 603)),
    LayerSpec("scn08_portrait_fast_apc", "SCN-08_SquadPortraits_Green.png", (540, 218, 984, 604)),
    LayerSpec("scn08_portrait_recon_drone", "SCN-08_SquadPortraits_Green.png", (1028, 218, 1449, 604)),
    LayerSpec("scn08_portrait_bomb_suit", "SCN-08_SquadPortraits_Green.png", (1492, 219, 1875, 603)),
)


def key_green_to_alpha(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    pixels = image.load()
    width, height = image.size
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            is_key = g > 105 and g > r * 1.22 and g > b * 1.22
            if is_key:
                pixels[x, y] = (r, g, b, 0)
    return image


def trim_alpha(image: Image.Image, pad: int) -> Image.Image:
    bbox = image.getbbox()
    if bbox is None:
        return image
    return image.crop((
        max(0, bbox[0] - pad),
        max(0, bbox[1] - pad),
        min(image.width, bbox[2] + pad),
        min(image.height, bbox[3] + pad),
    ))


def process_layer(spec: LayerSpec) -> dict[str, object]:
    source_path = SOURCE / spec.source
    image = Image.open(source_path).convert("RGBA")
    if spec.box is not None:
        image = image.crop(spec.box)
    if spec.key_green:
        image = key_green_to_alpha(image)
    if spec.trim:
        image = trim_alpha(image, spec.pad)
    out_path = LAYERS / f"{spec.name}.png"
    image.save(out_path)
    return {
        "id": spec.name,
        "file": str(out_path),
        "source": str(source_path),
        "source_box": spec.box,
        "size": {"width": image.width, "height": image.height},
        "key_green": spec.key_green,
    }


def make_contact_sheet(layer_paths: Iterable[Path]) -> None:
    paths = list(layer_paths)
    tile_w, tile_h = 260, 210
    cols = 5
    rows = (len(paths) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * tile_w, rows * tile_h), (26, 28, 24))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    checker = Image.new("RGB", (tile_w, tile_h - 34), (52, 52, 52))
    cd = ImageDraw.Draw(checker)
    s = 16
    for y in range(0, checker.height, s):
        for x in range(0, checker.width, s):
            if (x // s + y // s) % 2 == 0:
                cd.rectangle((x, y, x + s - 1, y + s - 1), fill=(86, 86, 86))

    for index, path in enumerate(paths):
        col = index % cols
        row = index // cols
        x0 = col * tile_w
        y0 = row * tile_h
        sheet.paste(checker, (x0, y0))
        img = Image.open(path).convert("RGBA")
        img.thumbnail((tile_w - 24, tile_h - 58), Image.Resampling.LANCZOS)
        x = x0 + (tile_w - img.width) // 2
        y = y0 + (tile_h - 34 - img.height) // 2
        sheet.paste(img, (x, y), img)
        draw.rectangle((x0, y0 + tile_h - 34, x0 + tile_w, y0 + tile_h), fill=(10, 12, 10))
        draw.text((x0 + 8, y0 + tile_h - 28), path.stem[:34], fill=(220, 220, 196), font=font)
    sheet.save(VALIDATION / "SCN-08_RTSBattleHUD_layers_contact_sheet.png")


def promote_pack(manifest_path: Path) -> None:
    ROOT_LAYERS.mkdir(parents=True, exist_ok=True)
    ROOT_VALIDATION.mkdir(parents=True, exist_ok=True)

    for old_layer in ROOT_LAYERS.glob("*.png"):
        old_layer.unlink()

    for layer_path in sorted(LAYERS.glob("*.png")):
        shutil.copy2(layer_path, ROOT_LAYERS / layer_path.name)

    shutil.copy2(manifest_path, PACK / "layer_manifest.json")
    shutil.copy2(
        VALIDATION / "SCN-08_RTSBattleHUD_layers_contact_sheet.png",
        ROOT_VALIDATION / "SCN-08_RTSBattleHUD_layers_contact_sheet.png",
    )


def main() -> None:
    LAYERS.mkdir(parents=True, exist_ok=True)
    VALIDATION.mkdir(parents=True, exist_ok=True)
    specs = (*ART_LAYERS, *FRAME_LAYERS, *ICON_LAYERS, *MARKER_LAYERS, *PORTRAIT_LAYERS)
    layers = [process_layer(spec) for spec in specs]
    manifest = {
        "surface_id": "SCN-08_RTSBattleHUD",
        "workflow": "VisualLockLayered V01 3D Match HUD Green-Source Extraction",
        "target_reference": str(PACK / "reference" / "SCN-08_RTSBattleHUD_Landscape_Target.png"),
        "rule": "Generated implementation sources only. The target-lock mockup was not sliced for layers.",
        "sources": [str(path) for path in sorted(SOURCE.glob("*.png"))],
        "layers": layers,
    }
    manifest_path = PACK / "generated_v01" / "layer_manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    make_contact_sheet(sorted(LAYERS.glob("*.png")))
    promote_pack(manifest_path)
    print(f"Wrote {len(layers)} layers")
    print(manifest_path)
    print(VALIDATION / "SCN-08_RTSBattleHUD_layers_contact_sheet.png")
    print(PACK / "layer_manifest.json")


if __name__ == "__main__":
    main()

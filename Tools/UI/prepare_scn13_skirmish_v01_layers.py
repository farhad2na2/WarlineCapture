#!/usr/bin/env python3
"""Prepare SCN-13 Skirmish Setup V01 VisualLockLayered sprites.

This extractor uses only generated implementation source sheets. It does not
slice the target-lock mockup.
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "Design" / "VisualLockLayered" / "SCN-13_SkirmishSetup"
SOURCE = PACK / "generated_v01" / "source"
LAYERS = PACK / "generated_v01" / "layers"
VALIDATION = PACK / "generated_v01" / "validation"


@dataclass(frozen=True)
class LayerSpec:
    name: str
    source: str
    box: tuple[int, int, int, int] | None = None
    key_green: bool = True
    trim: bool = True
    pad: int = 4


FRAME_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("scn13_header_logo_panel_bg", "SCN-13_Frames_Green.png", (19, 23, 454, 133)),
    LayerSpec("scn13_header_resource_panel_bg", "SCN-13_Frames_Green.png", (527, 30, 1316, 120)),
    LayerSpec("scn13_header_right_actions_bg", "SCN-13_Frames_Green.png", (1367, 29, 1644, 125)),
    LayerSpec("scn13_title_back_panel_frame", "SCN-13_Frames_Green.png", (19, 169, 382, 255)),
    LayerSpec("scn13_preset_rail_panel_frame", "SCN-13_Frames_Green.png", (19, 274, 304, 752)),
    LayerSpec("scn13_preset_row_selected_frame", "SCN-13_Frames_Green.png", (321, 290, 570, 364)),
    LayerSpec("scn13_preset_row_locked_frame", "SCN-13_Frames_Green.png", (321, 380, 570, 456)),
    LayerSpec("scn13_operation_preview_frame", "SCN-13_Frames_Green.png", (591, 190, 1233, 713)),
    LayerSpec("scn13_rules_panel_frame", "SCN-13_Frames_Green.png", (1274, 153, 1631, 578)),
    LayerSpec("scn13_rule_row_frame", "SCN-13_Frames_Green.png", (1274, 588, 1631, 649)),
    LayerSpec("scn13_stepper_minus_frame", "SCN-13_Frames_Green.png", (1273, 658, 1349, 723)),
    LayerSpec("scn13_stepper_value_frame", "SCN-13_Frames_Green.png", (1359, 658, 1546, 723)),
    LayerSpec("scn13_stepper_plus_frame", "SCN-13_Frames_Green.png", (1555, 658, 1632, 723)),
    LayerSpec("scn13_locked_reason_chip_frame", "SCN-13_Frames_Green.png", (1258, 739, 1649, 802)),
    LayerSpec("scn13_info_panel_frame", "SCN-13_Frames_Green.png", (19, 789, 501, 920)),
    LayerSpec("scn13_secondary_action_button_frame", "SCN-13_Frames_Green.png", (547, 815, 1106, 908)),
    LayerSpec("scn13_launch_cta_frame", "SCN-13_Frames_Green.png", (1148, 809, 1654, 923)),
)


ICON_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("scn13_brand_logo_lockup", "SCN-13_Icons_Green.png", (45, 55, 670, 240)),
    LayerSpec("scn13_icon_back_arrow", "SCN-13_Icons_Green.png", (735, 70, 875, 210)),
    LayerSpec("scn13_icon_skirmish_blades", "SCN-13_Icons_Green.png", (920, 60, 1090, 220)),
    LayerSpec("scn13_icon_preset_target", "SCN-13_Icons_Green.png", (1110, 60, 1280, 220)),
    LayerSpec("scn13_icon_preset_convoy", "SCN-13_Icons_Green.png", (1290, 75, 1465, 220)),
    LayerSpec("scn13_icon_preset_airlift", "SCN-13_Icons_Green.png", (1500, 80, 1670, 210)),
    LayerSpec("scn13_icon_preset_breach", "SCN-13_Icons_Green.png", (40, 250, 200, 420)),
    LayerSpec("scn13_icon_preset_hidden_cell", "SCN-13_Icons_Green.png", (245, 250, 370, 405)),
    LayerSpec("scn13_icon_lock", "SCN-13_Icons_Green.png", (420, 250, 550, 410)),
    LayerSpec("scn13_selected_status_dot", "SCN-13_Icons_Green.png", (585, 260, 700, 395)),
    LayerSpec("scn13_resource_credits_coin", "SCN-13_Icons_Green.png", (760, 250, 930, 415)),
    LayerSpec("scn13_resource_supplies_crate", "SCN-13_Icons_Green.png", (950, 250, 1120, 415)),
    LayerSpec("scn13_resource_command_shield", "SCN-13_Icons_Green.png", (1130, 245, 1285, 420)),
    LayerSpec("scn13_icon_inbox_envelope", "SCN-13_Icons_Green.png", (1300, 270, 1480, 390)),
    LayerSpec("scn13_icon_settings_gear", "SCN-13_Icons_Green.png", (1510, 260, 1670, 405)),
    LayerSpec("scn13_icon_enemy_type", "SCN-13_Icons_Green.png", (55, 430, 180, 570)),
    LayerSpec("scn13_icon_enemy_count", "SCN-13_Icons_Green.png", (220, 430, 380, 570)),
    LayerSpec("scn13_icon_difficulty_bars", "SCN-13_Icons_Green.png", (440, 430, 570, 570)),
    LayerSpec("scn13_icon_starting_credits", "SCN-13_Icons_Green.png", (630, 430, 740, 570)),
    LayerSpec("scn13_icon_income_chart", "SCN-13_Icons_Green.png", (805, 430, 955, 570)),
    LayerSpec("scn13_icon_build_speed_gear", "SCN-13_Icons_Green.png", (990, 430, 1140, 570)),
    LayerSpec("scn13_icon_production_factory", "SCN-13_Icons_Green.png", (1180, 430, 1325, 570)),
    LayerSpec("scn13_icon_aggression_skull", "SCN-13_Icons_Green.png", (1355, 430, 1485, 570)),
    LayerSpec("scn13_icon_expansion_arrows", "SCN-13_Icons_Green.png", (1535, 430, 1665, 570)),
    LayerSpec("scn13_icon_win_condition_target", "SCN-13_Icons_Green.png", (75, 590, 220, 735)),
    LayerSpec("scn13_icon_fog_hidden_eye", "SCN-13_Icons_Green.png", (320, 590, 470, 735)),
    LayerSpec("scn13_icon_map_pin", "SCN-13_Icons_Green.png", (570, 590, 700, 745)),
    LayerSpec("scn13_icon_seed_dice", "SCN-13_Icons_Green.png", (760, 590, 900, 745)),
    LayerSpec("scn13_icon_intel_eye", "SCN-13_Icons_Green.png", (960, 590, 1130, 735)),
    LayerSpec("scn13_icon_civilian_group", "SCN-13_Icons_Green.png", (1170, 590, 1330, 740)),
    LayerSpec("scn13_icon_info_circle", "SCN-13_Icons_Green.png", (1380, 590, 1525, 735)),
    LayerSpec("scn13_icon_reset_arrow", "SCN-13_Icons_Green.png", (80, 735, 220, 875)),
    LayerSpec("scn13_icon_manage_list", "SCN-13_Icons_Green.png", (300, 740, 440, 870)),
    LayerSpec("scn13_launch_chevrons", "SCN-13_Icons_Green.png", (540, 750, 700, 870)),
    LayerSpec("scn13_dropdown_chevron", "SCN-13_Icons_Green.png", (780, 780, 900, 860)),
    LayerSpec("scn13_stepper_minus_icon", "SCN-13_Icons_Green.png", (990, 790, 1080, 850)),
    LayerSpec("scn13_stepper_plus_icon", "SCN-13_Icons_Green.png", (1165, 760, 1270, 870)),
)


MARKER_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("scn13_marker_hostile_intel_diamond", "SCN-13_MapMarkers_Green.png", (130, 80, 400, 350)),
    LayerSpec("scn13_marker_patrol_route_ring", "SCN-13_MapMarkers_Green.png", (500, 70, 780, 360)),
    LayerSpec("scn13_marker_path_segment", "SCN-13_MapMarkers_Green.png", (900, 60, 1140, 360)),
    LayerSpec("scn13_marker_deployment_zone_circle", "SCN-13_MapMarkers_Green.png", (1240, 80, 1520, 340)),
    LayerSpec("scn13_marker_deployment_flag", "SCN-13_MapMarkers_Green.png", (180, 390, 390, 640)),
    LayerSpec("scn13_marker_civilian_risk", "SCN-13_MapMarkers_Green.png", (570, 410, 730, 610)),
    LayerSpec("scn13_marker_objective_target", "SCN-13_MapMarkers_Green.png", (900, 390, 1150, 640)),
    LayerSpec("scn13_marker_scan_ping", "SCN-13_MapMarkers_Green.png", (1260, 430, 1520, 620)),
    LayerSpec("scn13_marker_warning_triangle", "SCN-13_MapMarkers_Green.png", (420, 630, 660, 850)),
    LayerSpec("scn13_marker_camera_brackets", "SCN-13_MapMarkers_Green.png", (750, 650, 980, 850)),
)


ART_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("scn13_background_21x9_no_ui", "SCN-13_Background_21x9_NoUI.png", None, False, False, 0),
    LayerSpec("scn13_operation_preview_art_wide", "SCN-13_OperationPreview_Wide.png", None, False, False, 0),
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
    left = max(0, bbox[0] - pad)
    top = max(0, bbox[1] - pad)
    right = min(image.width, bbox[2] + pad)
    bottom = min(image.height, bbox[3] + pad)
    return image.crop((left, top, right, bottom))


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

    sheet.save(VALIDATION / "SCN-13_SkirmishSetup_layers_contact_sheet.png")


def main() -> None:
    LAYERS.mkdir(parents=True, exist_ok=True)
    VALIDATION.mkdir(parents=True, exist_ok=True)

    specs = (*ART_LAYERS, *FRAME_LAYERS, *ICON_LAYERS, *MARKER_LAYERS)
    layers = [process_layer(spec) for spec in specs]

    manifest = {
        "surface_id": "SCN-13_SkirmishSetup",
        "workflow": "VisualLockLayered V01 Skirmish Setup Green-Source Extraction",
        "target_reference": str(PACK / "reference" / "SCN-13_SkirmishSetup_Landscape_Target.png"),
        "rule": "Generated implementation sources only. The target-lock mockup was not sliced for layers.",
        "sources": [str(path) for path in sorted(SOURCE.glob("*.png"))],
        "layers": layers,
    }
    manifest_path = PACK / "generated_v01" / "layer_manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    make_contact_sheet(sorted(LAYERS.glob("*.png")))
    print(f"Wrote {len(layers)} layers")
    print(manifest_path)
    print(VALIDATION / "SCN-13_SkirmishSetup_layers_contact_sheet.png")


if __name__ == "__main__":
    main()

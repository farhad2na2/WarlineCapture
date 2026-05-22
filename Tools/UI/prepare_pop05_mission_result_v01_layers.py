#!/usr/bin/env python3
"""Prepare POP-05 Mission Result V01 VisualLockLayered sprites.

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
PACK = ROOT / "Design" / "VisualLockLayered" / "POP-05_MissionResult"
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
    LayerSpec("pop05_background_21x9_no_ui", "POP-05_Background_21x9_NoUI.png", None, False, False, 0),
    LayerSpec("pop05_mission_snapshot_art", "POP-05_MissionSnapshot.png", None, False, False, 0),
)


FRAME_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("pop05_result_header_frame", "POP-05_Frames_Green.png", (165, 18, 1206, 115)),
    LayerSpec("pop05_rewards_panel_frame", "POP-05_Frames_Green.png", (1246, 21, 1578, 323)),
    LayerSpec("pop05_mission_metadata_strip_frame", "POP-05_Frames_Green.png", (385, 126, 1195, 172)),
    LayerSpec("pop05_mission_summary_panel_frame", "POP-05_Frames_Green.png", (81, 178, 284, 664)),
    LayerSpec("pop05_rating_objectives_panel_frame", "POP-05_Frames_Green.png", (631, 181, 1204, 401)),
    LayerSpec("pop05_mission_snapshot_frame", "POP-05_Frames_Green.png", (305, 188, 606, 410)),
    LayerSpec("pop05_consequences_panel_frame", "POP-05_Frames_Green.png", (1246, 337, 1578, 637)),
    LayerSpec("pop05_objective_row_frame", "POP-05_Frames_Green.png", (627, 407, 1201, 452)),
    LayerSpec("pop05_performance_stats_panel_frame", "POP-05_Frames_Green.png", (625, 457, 1203, 558)),
    LayerSpec("pop05_mission_description_panel_frame", "POP-05_Frames_Green.png", (298, 469, 611, 665)),
    LayerSpec("pop05_stat_tile_frame_1", "POP-05_Frames_Green.png", (629, 562, 765, 668)),
    LayerSpec("pop05_stat_tile_frame_2", "POP-05_Frames_Green.png", (772, 562, 903, 668)),
    LayerSpec("pop05_stat_tile_frame_3", "POP-05_Frames_Green.png", (909, 562, 1041, 668)),
    LayerSpec("pop05_stat_tile_frame_4", "POP-05_Frames_Green.png", (1047, 562, 1179, 668)),
    LayerSpec("pop05_compact_modal_border_frame", "POP-05_Frames_Green.png", (1426, 667, 1631, 883)),
    LayerSpec("pop05_bottom_action_bar_rail_frame", "POP-05_Frames_Green.png", (121, 679, 1372, 755)),
    LayerSpec("pop05_replay_button_frame", "POP-05_Frames_Green.png", (123, 777, 407, 877)),
    LayerSpec("pop05_continue_button_frame", "POP-05_Frames_Green.png", (1081, 778, 1379, 872)),
    LayerSpec("pop05_route_note_chip_frame", "POP-05_Frames_Green.png", (909, 788, 1052, 830)),
    LayerSpec("pop05_small_section_tab_frame", "POP-05_Frames_Green.png", (450, 797, 677, 859)),
    LayerSpec("pop05_xp_bar_frame", "POP-05_Frames_Green.png", (727, 813, 865, 852)),
)


ICON_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("pop05_commander_logo", "POP-05_Icons_Green.png", (45, 35, 240, 250)),
    LayerSpec("pop05_victory_wing_left", "POP-05_Icons_Green.png", (350, 65, 565, 190)),
    LayerSpec("pop05_victory_wing_right", "POP-05_Icons_Green.png", (835, 65, 1040, 190)),
    LayerSpec("pop05_star_full_gold", "POP-05_Icons_Green.png", (1150, 30, 1355, 220)),
    LayerSpec("pop05_star_empty_outline", "POP-05_Icons_Green.png", (1455, 30, 1665, 225)),
    LayerSpec("pop05_checkbox_checked", "POP-05_Icons_Green.png", (75, 250, 220, 390)),
    LayerSpec("pop05_checkbox_empty", "POP-05_Icons_Green.png", (340, 250, 480, 390)),
    LayerSpec("pop05_replay_arrow_icon", "POP-05_Icons_Green.png", (650, 245, 815, 400)),
    LayerSpec("pop05_continue_chevrons_icon", "POP-05_Icons_Green.png", (915, 240, 1100, 395)),
    LayerSpec("pop05_route_path_icon", "POP-05_Icons_Green.png", (1150, 245, 1320, 395)),
    LayerSpec("pop05_reward_commander_xp_shield", "POP-05_Icons_Green.png", (70, 405, 220, 575)),
    LayerSpec("pop05_reward_credits_coin", "POP-05_Icons_Green.png", (270, 415, 420, 575)),
    LayerSpec("pop05_reward_supplies_crate", "POP-05_Icons_Green.png", (480, 420, 650, 570)),
    LayerSpec("pop05_reward_intel_document", "POP-05_Icons_Green.png", (700, 420, 845, 570)),
    LayerSpec("pop05_consequence_civilian_group", "POP-05_Icons_Green.png", (900, 425, 1065, 570)),
    LayerSpec("pop05_consequence_district_trust_shield", "POP-05_Icons_Green.png", (1130, 410, 1280, 575)),
    LayerSpec("pop05_consequence_hostile_influence", "POP-05_Icons_Green.png", (1340, 415, 1490, 575)),
    LayerSpec("pop05_consequence_infrastructure", "POP-05_Icons_Green.png", (1550, 420, 1710, 570)),
    LayerSpec("pop05_stat_enemies_defeated_crosshair", "POP-05_Icons_Green.png", (60, 575, 220, 730)),
    LayerSpec("pop05_stat_units_lost_shield", "POP-05_Icons_Green.png", (290, 580, 420, 730)),
    LayerSpec("pop05_stat_timer_clock", "POP-05_Icons_Green.png", (485, 580, 635, 730)),
    LayerSpec("pop05_mission_summary_star_outline", "POP-05_Icons_Green.png", (690, 580, 830, 720)),
    LayerSpec("pop05_rewards_blades_icon", "POP-05_Icons_Green.png", (900, 580, 1030, 720)),
    LayerSpec("pop05_consequences_compass_icon", "POP-05_Icons_Green.png", (1080, 585, 1220, 725)),
    LayerSpec("pop05_value_plus_marker", "POP-05_Icons_Green.png", (1275, 600, 1390, 720)),
    LayerSpec("pop05_value_minus_marker", "POP-05_Icons_Green.png", (1440, 620, 1550, 690)),
    LayerSpec("pop05_value_stable_equal_marker", "POP-05_Icons_Green.png", (1590, 610, 1710, 705)),
    LayerSpec("pop05_progress_bar_frame", "POP-05_Icons_Green.png", (230, 760, 550, 850)),
    LayerSpec("pop05_progress_gold_fill_segment", "POP-05_Icons_Green.png", (700, 770, 900, 840)),
    LayerSpec("pop05_status_segment_strip", "POP-05_Icons_Green.png", (1010, 760, 1510, 850)),
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

    sheet.save(VALIDATION / "POP-05_MissionResult_layers_contact_sheet.png")


def promote_pack(manifest_path: Path) -> None:
    ROOT_LAYERS.mkdir(parents=True, exist_ok=True)
    ROOT_VALIDATION.mkdir(parents=True, exist_ok=True)

    for old_layer in ROOT_LAYERS.glob("*.png"):
        old_layer.unlink()
    for layer_path in sorted(LAYERS.glob("*.png")):
        shutil.copy2(layer_path, ROOT_LAYERS / layer_path.name)

    shutil.copy2(manifest_path, PACK / "layer_manifest.json")
    shutil.copy2(
        VALIDATION / "POP-05_MissionResult_layers_contact_sheet.png",
        ROOT_VALIDATION / "POP-05_MissionResult_layers_contact_sheet.png",
    )


def main() -> None:
    LAYERS.mkdir(parents=True, exist_ok=True)
    VALIDATION.mkdir(parents=True, exist_ok=True)

    specs = (*ART_LAYERS, *FRAME_LAYERS, *ICON_LAYERS)
    layers = [process_layer(spec) for spec in specs]

    manifest = {
        "surface_id": "POP-05_MissionResult",
        "workflow": "VisualLockLayered V01 3D Mission Result Green-Source Extraction",
        "target_reference": str(PACK / "reference" / "POP-05_MissionResult_Landscape_Target.png"),
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
    print(VALIDATION / "POP-05_MissionResult_layers_contact_sheet.png")
    print(PACK / "layer_manifest.json")


if __name__ == "__main__":
    main()

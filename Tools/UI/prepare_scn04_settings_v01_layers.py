#!/usr/bin/env python3
"""Prepare SCN-04 Settings / Accessibility V01 VisualLockLayered sprites.

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
PACK = ROOT / "Design" / "VisualLockLayered" / "SCN-04_SettingsAccessibility"
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
    LayerSpec("scn04_background_21x9_no_ui", "SCN-04_Background_21x9_NoUI.png", None, False, False, 0),
)


FRAME_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("scn04_header_logo_panel_bg", "SCN-04_Frames_Green.png", (18, 14, 435, 118)),
    LayerSpec("scn04_header_resource_credits_bg", "SCN-04_Frames_Green.png", (416, 24, 739, 108)),
    LayerSpec("scn04_header_resource_supplies_bg", "SCN-04_Frames_Green.png", (718, 24, 1040, 108)),
    LayerSpec("scn04_header_resource_command_bg", "SCN-04_Frames_Green.png", (1013, 24, 1336, 108)),
    LayerSpec("scn04_header_right_actions_bg", "SCN-04_Frames_Green.png", (1342, 20, 1652, 113)),
    LayerSpec("scn04_back_button_frame", "SCN-04_Frames_Green.png", (44, 131, 128, 207)),
    LayerSpec("scn04_title_strip_frame", "SCN-04_Frames_Green.png", (156, 136, 1212, 204)),
    LayerSpec("scn04_category_rail_frame", "SCN-04_Frames_Green.png", (26, 220, 184, 745)),
    LayerSpec("scn04_category_tab_normal_frame", "SCN-04_Frames_Green.png", (210, 240, 453, 319)),
    LayerSpec("scn04_category_tab_selected_frame", "SCN-04_Frames_Green.png", (210, 416, 453, 502)),
    LayerSpec("scn04_settings_content_panel_frame", "SCN-04_Frames_Green.png", (480, 220, 1250, 516)),
    LayerSpec("scn04_readability_preview_panel_frame", "SCN-04_Frames_Green.png", (1274, 168, 1632, 680)),
    LayerSpec("scn04_setting_row_frame", "SCN-04_Frames_Green.png", (219, 538, 615, 597)),
    LayerSpec("scn04_slider_track_frame", "SCN-04_Frames_Green.png", (652, 560, 934, 579)),
    LayerSpec("scn04_toggle_off_track_frame", "SCN-04_Frames_Green.png", (1009, 561, 1096, 606)),
    LayerSpec("scn04_toggle_on_track_frame", "SCN-04_Frames_Green.png", (1109, 561, 1196, 606)),
    LayerSpec("scn04_slider_handle_frame", "SCN-04_Frames_Green.png", (947, 591, 982, 627)),
    LayerSpec("scn04_slider_fill_bar", "SCN-04_Frames_Green.png", (652, 603, 934, 622)),
    LayerSpec("scn04_dropdown_field_frame", "SCN-04_Frames_Green.png", (219, 649, 578, 694)),
    LayerSpec("scn04_segment_option_normal_frame", "SCN-04_Frames_Green.png", (597, 649, 833, 694)),
    LayerSpec("scn04_segment_option_selected_frame", "SCN-04_Frames_Green.png", (857, 651, 1084, 694)),
    LayerSpec("scn04_status_chip_frame", "SCN-04_Frames_Green.png", (1115, 663, 1222, 690)),
    LayerSpec("scn04_preview_status_row_frame", "SCN-04_Frames_Green.png", (1354, 700, 1510, 755)),
    LayerSpec("scn04_dropdown_field_frame_wide", "SCN-04_Frames_Green.png", (597, 704, 833, 749)),
    LayerSpec("scn04_toggle_knob_frame", "SCN-04_Frames_Green.png", (1258, 704, 1307, 746)),
    LayerSpec("scn04_bottom_footer_rail_frame", "SCN-04_Frames_Green.png", (33, 763, 959, 822)),
    LayerSpec("scn04_modal_confirmation_frame", "SCN-04_Frames_Green.png", (1010, 773, 1333, 927)),
    LayerSpec("scn04_primary_apply_button_frame", "SCN-04_Frames_Green.png", (630, 834, 980, 925)),
    LayerSpec("scn04_secondary_button_frame", "SCN-04_Frames_Green.png", (26, 840, 300, 918)),
    LayerSpec("scn04_secondary_button_wide_frame", "SCN-04_Frames_Green.png", (319, 840, 599, 918)),
    LayerSpec("scn04_divider_line", "SCN-04_Frames_Green.png", (1357, 856, 1639, 862)),
)


ICON_LAYERS: tuple[LayerSpec, ...] = (
    LayerSpec("scn04_brand_logo_lockup", "SCN-04_Icons_Green.png", (45, 30, 760, 230)),
    LayerSpec("scn04_icon_back_arrow", "SCN-04_Icons_Green.png", (820, 45, 1010, 190)),
    LayerSpec("scn04_icon_gear", "SCN-04_Icons_Green.png", (1085, 40, 1245, 205)),
    LayerSpec("scn04_icon_inbox_envelope", "SCN-04_Icons_Green.png", (1340, 55, 1515, 190)),
    LayerSpec("scn04_icon_speaker", "SCN-04_Icons_Green.png", (60, 230, 230, 380)),
    LayerSpec("scn04_icon_music", "SCN-04_Icons_Green.png", (315, 230, 455, 380)),
    LayerSpec("scn04_icon_sound_wave", "SCN-04_Icons_Green.png", (560, 230, 700, 380)),
    LayerSpec("scn04_icon_voice_microphone", "SCN-04_Icons_Green.png", (795, 225, 930, 385)),
    LayerSpec("scn04_icon_display", "SCN-04_Icons_Green.png", (1025, 225, 1190, 385)),
    LayerSpec("scn04_icon_controls_crosshair", "SCN-04_Icons_Green.png", (1280, 220, 1450, 385)),
    LayerSpec("scn04_icon_notifications_bell", "SCN-04_Icons_Green.png", (1530, 225, 1670, 385)),
    LayerSpec("scn04_icon_accessibility_eye", "SCN-04_Icons_Green.png", (60, 390, 235, 540)),
    LayerSpec("scn04_icon_language_globe", "SCN-04_Icons_Green.png", (315, 385, 470, 540)),
    LayerSpec("scn04_icon_large_text", "SCN-04_Icons_Green.png", (570, 385, 730, 535)),
    LayerSpec("scn04_icon_high_contrast", "SCN-04_Icons_Green.png", (840, 385, 995, 540)),
    LayerSpec("scn04_icon_colorblind_dots", "SCN-04_Icons_Green.png", (1100, 385, 1240, 540)),
    LayerSpec("scn04_icon_reduced_motion", "SCN-04_Icons_Green.png", (1320, 385, 1480, 545)),
    LayerSpec("scn04_icon_dropdown_chevron", "SCN-04_Icons_Green.png", (70, 550, 220, 675)),
    LayerSpec("scn04_icon_checkmark_olive", "SCN-04_Icons_Green.png", (280, 535, 430, 675)),
    LayerSpec("scn04_icon_reset_arrow", "SCN-04_Icons_Green.png", (500, 535, 645, 680)),
    LayerSpec("scn04_icon_apply_checkmark", "SCN-04_Icons_Green.png", (720, 535, 875, 675)),
    LayerSpec("scn04_icon_commander_shield", "SCN-04_Icons_Green.png", (930, 525, 1075, 695)),
    LayerSpec("scn04_icon_credits_coin", "SCN-04_Icons_Green.png", (1115, 530, 1265, 690)),
    LayerSpec("scn04_icon_supplies_crate", "SCN-04_Icons_Green.png", (1300, 530, 1470, 695)),
    LayerSpec("scn04_icon_command_shield", "SCN-04_Icons_Green.png", (1500, 530, 1665, 695)),
    LayerSpec("scn04_icon_plus", "SCN-04_Icons_Green.png", (65, 700, 205, 840)),
    LayerSpec("scn04_icon_minus", "SCN-04_Icons_Green.png", (240, 720, 370, 805)),
    LayerSpec("scn04_icon_close_x", "SCN-04_Icons_Green.png", (420, 700, 545, 835)),
    LayerSpec("scn04_icon_info", "SCN-04_Icons_Green.png", (585, 700, 720, 840)),
    LayerSpec("scn04_icon_warning", "SCN-04_Icons_Green.png", (735, 695, 880, 840)),
    LayerSpec("scn04_icon_lock", "SCN-04_Icons_Green.png", (910, 690, 1030, 845)),
    LayerSpec("scn04_icon_disabled_slash", "SCN-04_Icons_Green.png", (1055, 700, 1195, 840)),
    LayerSpec("scn04_icon_gold_square_badge", "SCN-04_Icons_Green.png", (1215, 700, 1345, 840)),
    LayerSpec("scn04_toggle_part_on", "SCN-04_Icons_Green.png", (1350, 710, 1535, 830)),
    LayerSpec("scn04_toggle_knob_highlight", "SCN-04_Icons_Green.png", (1545, 715, 1665, 840)),
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

    sheet.save(VALIDATION / "SCN-04_SettingsAccessibility_layers_contact_sheet.png")


def promote_pack(manifest_path: Path) -> None:
    ROOT_LAYERS.mkdir(parents=True, exist_ok=True)
    ROOT_VALIDATION.mkdir(parents=True, exist_ok=True)

    for old_layer in ROOT_LAYERS.glob("*.png"):
        old_layer.unlink()
    for layer_path in sorted(LAYERS.glob("*.png")):
        shutil.copy2(layer_path, ROOT_LAYERS / layer_path.name)

    shutil.copy2(manifest_path, PACK / "layer_manifest.json")
    shutil.copy2(
        VALIDATION / "SCN-04_SettingsAccessibility_layers_contact_sheet.png",
        ROOT_VALIDATION / "SCN-04_SettingsAccessibility_layers_contact_sheet.png",
    )


def main() -> None:
    LAYERS.mkdir(parents=True, exist_ok=True)
    VALIDATION.mkdir(parents=True, exist_ok=True)

    specs = (*ART_LAYERS, *FRAME_LAYERS, *ICON_LAYERS)
    layers = [process_layer(spec) for spec in specs]

    manifest = {
        "surface_id": "SCN-04_SettingsAccessibility",
        "workflow": "VisualLockLayered V01 Settings Accessibility Green-Source Extraction",
        "target_reference": str(PACK / "reference" / "SCN-04_SettingsAccessibility_Landscape_Target.png"),
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
    print(VALIDATION / "SCN-04_SettingsAccessibility_layers_contact_sheet.png")
    print(PACK / "layer_manifest.json")


if __name__ == "__main__":
    main()

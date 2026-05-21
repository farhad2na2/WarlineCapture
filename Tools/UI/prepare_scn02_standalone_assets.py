#!/usr/bin/env python3
"""Prepare SCN-02 standalone imagegen sprites for Unity consumption."""

from __future__ import annotations

import json
import shutil
import subprocess
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


PROJECT = Path(__file__).resolve().parents[2]
GENERATED_CACHE = Path("/Users/farhad/.codex/generated_images/019e0794-b98b-7071-8cdd-00aa26983635")
OUT_ROOT = PROJECT / "Design/VisualLockLayered/SCN-02_MainMenu/imagegen_standalone_20260519"
RAW_ROOT = OUT_ROOT / "raw"
ASSET_ROOT = OUT_ROOT / "assets"
CONTACT = OUT_ROOT / "SCN02_standalone_assets_contact_sheet.png"
CHROMA_HELPER = Path.home() / ".codex/skills/.system/imagegen/scripts/remove_chroma_key.py"

ASSET_ORDER = [
    ("top_resource_bar_frame_full", True),
    ("settings_button_frame", True),
    ("brand_logo_panel_frame", True),
    ("screen_shell_frame", True),
    ("deploy_command_button_frame", True),
    ("left_nav_row_frame", True),
    ("commander_profile_panel_frame", True),
    ("commander_portrait_placeholder", False),
    ("mode_card_frame", True),
    ("operation_warning_row_frame", True),
    ("operation_footer_frame", True),
    ("circular_badge_frame", True),
    ("main_menu_background_tactical_map", False),
    ("mode_card_art_saga", False),
    ("mode_card_art_operation", False),
    ("mode_card_art_quick_custom", False),
    ("brand_logo_lockup", True),
    ("settings_gear_icon", True),
    ("icon_credits", True),
    ("icon_materials", True),
    ("icon_command_authority", True),
    ("operation_warning_icon", True),
    ("deploy_command_chevrons", True),
    ("left_nav_icon_inbox", True),
    ("left_nav_icon_store", True),
    ("left_nav_icon_events", True),
    ("left_nav_icon_ranking", True),
    ("left_nav_icon_command_feed", True),
    ("card_footer_icon_saga", True),
    ("card_footer_icon_operation", True),
    ("mode_card_header_emblem_quick_custom", True),
    ("mode_card_header_emblem_saga", True),
    ("mode_card_header_emblem_operation", True),
    ("mode_card_header_emblem_quick_swords", True),
    ("lock_badge_frame", True),
    ("lock_icon", True),
    ("disabled_status_pill_frame", True),
    ("operation_pressure_meter_segments", True),
    ("operation_risk_meter_segments", True),
    ("top_resource_separator_ticks", True),
    ("profile_data_status_strip", True),
]

ALIASES = {
    "commander_portrait_placeholder": ["commander_profile_portrait"],
    "disabled_status_pill_frame": ["designed_unavailable_badge"],
    "mode_card_header_emblem_quick_custom": ["card_footer_icon_quick_custom"],
}


def prepare_dirs() -> None:
    RAW_ROOT.mkdir(parents=True, exist_ok=True)
    ASSET_ROOT.mkdir(parents=True, exist_ok=True)


def trim_alpha(img: Image.Image, pad: int = 32) -> Image.Image:
    alpha = img.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        return img
    box = (
        max(0, bbox[0] - pad),
        max(0, bbox[1] - pad),
        min(img.width, bbox[2] + pad),
        min(img.height, bbox[3] + pad),
    )
    return img.crop(box)


def remove_chroma(raw_path: Path) -> Image.Image:
    tmp_path = raw_path.with_suffix(".alpha.tmp.png")
    subprocess.run(
        [
            "python3",
            str(CHROMA_HELPER),
            "--input",
            str(raw_path),
            "--out",
            str(tmp_path),
            "--auto-key",
            "border",
            "--soft-matte",
            "--transparent-threshold",
            "12",
            "--opaque-threshold",
            "220",
            "--despill",
        ],
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )
    image = trim_alpha(Image.open(tmp_path).convert("RGBA"))
    tmp_path.unlink()
    return image


def build_contact(records: list[dict[str, object]]) -> None:
    cols = 4
    cell_w, cell_h = 500, 340
    rows = (len(records) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cell_w, rows * cell_h), (24, 27, 30))
    draw = ImageDraw.Draw(sheet)

    try:
        font = ImageFont.truetype("Arial.ttf", 16)
        small = ImageFont.truetype("Arial.ttf", 13)
    except OSError:
        font = ImageFont.load_default()
        small = ImageFont.load_default()

    for i, record in enumerate(records):
        image = Image.open(PROJECT / str(record["asset"])).convert("RGBA")
        max_w, max_h = 450, 240
        scale = min(max_w / image.width, max_h / image.height, 1.0)
        preview = image.resize(
            (max(1, int(image.width * scale)), max(1, int(image.height * scale))),
            Image.Resampling.LANCZOS,
        )

        x = (i % cols) * cell_w
        y = (i // cols) * cell_h
        draw.rectangle((x + 8, y + 8, x + cell_w - 8, y + cell_h - 8), fill=(15, 17, 19), outline=(72, 82, 88))
        draw.text((x + 18, y + 16), str(record["name"]), fill=(235, 240, 240), font=font)
        draw.text((x + 18, y + 38), f"{image.width}x{image.height}", fill=(160, 170, 174), font=small)

        px = x + (cell_w - preview.width) // 2
        py = y + 68 + (240 - preview.height) // 2
        for yy in range(py, py + preview.height, 16):
            for xx in range(px, px + preview.width, 16):
                fill = (230, 230, 230) if ((xx // 16 + yy // 16) % 2 == 0) else (202, 202, 202)
                draw.rectangle((xx, yy, min(xx + 15, px + preview.width), min(yy + 15, py + preview.height)), fill=fill)
        sheet.paste(preview, (px, py), preview)

    sheet.save(CONTACT, optimize=True)


def main() -> None:
    prepare_dirs()
    files = sorted(GENERATED_CACHE.glob("*.png"), key=lambda p: p.stat().st_mtime)
    if len(files) != len(ASSET_ORDER):
        raise SystemExit(f"Expected {len(ASSET_ORDER)} generated pngs, found {len(files)} in {GENERATED_CACHE}")

    records: list[dict[str, object]] = []
    for index, ((name, transparent), source) in enumerate(zip(ASSET_ORDER, files), start=1):
        raw_path = RAW_ROOT / f"{index:02d}_{name}.png"
        shutil.copy2(source, raw_path)

        image = remove_chroma(raw_path) if transparent else Image.open(raw_path).convert("RGBA")
        asset_path = ASSET_ROOT / f"{name}.png"
        image.save(asset_path, optimize=True)
        record = {
            "name": name,
            "raw": str(raw_path.relative_to(PROJECT)),
            "asset": str(asset_path.relative_to(PROJECT)),
            "transparent": transparent,
            "size": [image.width, image.height],
        }
        records.append(record)

        for alias in ALIASES.get(name, []):
            alias_path = ASSET_ROOT / f"{alias}.png"
            shutil.copy2(asset_path, alias_path)
            records.append(
                {
                    "name": alias,
                    "raw": str(raw_path.relative_to(PROJECT)),
                    "asset": str(alias_path.relative_to(PROJECT)),
                    "transparent": transparent,
                    "size": [image.width, image.height],
                    "aliasOf": name,
                }
            )

    manifest = {
        "source": "standalone imagegen requests, one sprite per generated image; no labelled sheet extraction; no target mockup crops",
        "generatedCache": str(GENERATED_CACHE),
        "assetCount": len(records),
        "assets": records,
    }
    (OUT_ROOT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    build_contact(records)
    print(f"Prepared {len(records)} assets")
    print(CONTACT)


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Prepare SCN-02 3D V15 VisualLockLayered sprites from the green source sheet."""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "Design" / "VisualLockLayered" / "SCN-02_MainMenu"
SOURCE = PACK / "generated_one_go" / "source" / "SCN-02_MainMenu_LayerSourceSheet_Green_V15B.png"
TARGET = PACK / "reference" / "SCN-02_MainMenu_Landscape_Target.png"
BACKGROUND = PACK / "generated_one_go" / "source" / "SCN-02_MainMenu_BackgroundArt_21x9_NoUI.png"
LAYERS = PACK / "layers"
CONTACT = PACK / "generated_one_go" / "layers_contact_sheet.png"
HEADER_PREVIEW = PACK / "validation" / "header_split_preview.png"
MANIFEST = PACK / "layer_manifest.json"


Layer = dict[str, object]


LAYOUT: list[Layer] = [
    {
        "id": "scn02_header_logo_panel_bg",
        "box": [18, 21, 292, 104],
        "role": "Responsive top-left logo panel background",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_header_logo_panel_bg.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_brand_logo_lockup",
        "box": [304, 22, 560, 104],
        "role": "Separate Warline Capture logo lockup for top-left header panel",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_brand_logo_lockup.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_header_resource_panel_bg",
        "box": [577, 22, 1036, 104],
        "role": "Responsive top resource panel background, can stretch between logo and command panels",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_header_resource_panel_bg.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_header_command_panel_bg",
        "box": [1050, 22, 1238, 104],
        "role": "Responsive top command resource panel background",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_header_command_panel_bg.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_header_right_actions_bg",
        "box": [1256, 22, 1512, 105],
        "role": "Responsive top-right inbox/settings action panel background",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_header_right_actions_bg.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_nav_button_selected_frame",
        "box": [28, 186, 320, 264],
        "role": "Selected left navigation button frame",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_nav_button_selected_frame.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_nav_button_inactive_frame",
        "box": [29, 275, 320, 353],
        "role": "Inactive left navigation button frame",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_nav_button_inactive_frame.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_mode_card_frame",
        "box": [372, 126, 624, 465],
        "role": "Primary mode card frame with header/footer lanes",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_mode_card_frame.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_mode_card_thumbnail_mask_frame",
        "box": [652, 130, 951, 472],
        "role": "Mode card thumbnail frame/mask variant",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_mode_card_thumbnail_mask_frame.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_commander_panel_frame",
        "box": [987, 130, 1273, 583],
        "role": "Right commander profile panel frame",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_commander_panel_frame.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_commander_portrait_frame",
        "box": [1316, 130, 1462, 310],
        "role": "Standalone commander portrait frame",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_commander_portrait_frame.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_locked_row_frame",
        "box": [1292, 520, 1498, 577],
        "role": "Locked feature row frame",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_locked_row_frame.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_deploy_cta_frame",
        "box": [209, 478, 549, 576],
        "role": "Gold Deploy Operation CTA button frame, label is live TMP",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_deploy_cta_frame.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_comms_status_panel_frame",
        "box": [161, 584, 536, 709],
        "role": "Bottom-left comms/status panel frame",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_comms_status_panel_frame.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_mode_progress_meter_frame",
        "box": [571, 572, 873, 615],
        "role": "Progress/readiness meter frame",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_mode_progress_meter_frame.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_readiness_segments",
        "box": [572, 636, 857, 662],
        "role": "Readiness/progress fill segment strip",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_readiness_segments.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_resource_coin_badge",
        "box": [571, 504, 627, 561],
        "role": "Credits resource coin/star badge",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_resource_coin_badge.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_resource_supplies_crate",
        "box": [642, 504, 710, 563],
        "role": "Supplies crate icon",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_resource_supplies_crate.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_resource_command_shield",
        "box": [726, 500, 776, 566],
        "role": "Command resource shield badge",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_resource_command_shield.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_icon_campaign_crosshair",
        "box": [57, 370, 107, 420],
        "role": "Campaign route icon",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_icon_campaign_crosshair.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_icon_operations_pin",
        "box": [58, 435, 106, 493],
        "role": "Operations route icon",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_icon_operations_pin.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_icon_skirmish_blades",
        "box": [58, 512, 104, 557],
        "role": "Skirmish route icon",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_icon_skirmish_blades.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_icon_store_cart",
        "box": [58, 577, 102, 617],
        "role": "Store / Command Exchange route icon",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_icon_store_cart.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_icon_commander_bust",
        "box": [64, 633, 99, 669],
        "role": "Commander route icon",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_icon_commander_bust.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_icon_settings_gear",
        "box": [62, 681, 100, 719],
        "role": "Settings route icon",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_icon_settings_gear.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_icon_inbox_envelope",
        "box": [53, 131, 96, 161],
        "role": "Inbox / messages icon",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_icon_inbox_envelope.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_icon_lock",
        "box": [893, 606, 934, 660],
        "role": "Locked-state icon",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_icon_lock.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_notification_dot",
        "box": [228, 135, 249, 156],
        "role": "Top-right notification badge dot",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_notification_dot.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_trim_corner_brackets",
        "box": [954, 594, 1032, 670],
        "role": "Decorative corner brackets",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_trim_corner_brackets.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_trim_slashes_and_bolts",
        "box": [1050, 620, 1330, 670],
        "role": "Decorative slashes, bolts, and accent trims",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_trim_slashes_and_bolts.png",
        "alpha": "chroma_key_00ff00",
    },
    {
        "id": "scn02_commander_portrait_art",
        "box": [1302, 325, 1473, 506],
        "role": "Generated commander portrait silhouette art",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_commander_portrait_art.png",
        "alpha": "chroma_key_00ff00",
    },
]


OPAQUE_LAYOUT: list[Layer] = [
    {
        "id": "scn02_background_art",
        "source": "background",
        "box": "full",
        "role": "21:9-safe no-UI 3D command-base background art. Use cover/crop, never stretch.",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_background_art.png",
        "alpha": "opaque_rectangular_art",
    },
    {
        "id": "scn02_campaign_thumbnail_art",
        "source": "sheet",
        "box": [37, 728, 1430, 818],
        "role": "Wide Campaign mode card 3D thumbnail art with horizontal overscan for 20:9/21:9 reveal",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_campaign_thumbnail_art.png",
        "alpha": "opaque_rectangular_art",
    },
    {
        "id": "scn02_operations_thumbnail_art",
        "source": "sheet",
        "box": [36, 824, 1430, 908],
        "role": "Wide Operations mode card 3D thumbnail art with horizontal overscan for 20:9/21:9 reveal",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_operations_thumbnail_art.png",
        "alpha": "opaque_rectangular_art",
    },
    {
        "id": "scn02_skirmish_thumbnail_art",
        "source": "sheet",
        "box": [35, 915, 1430, 1002],
        "role": "Wide Skirmish mode card 3D thumbnail art with horizontal overscan for 20:9/21:9 reveal",
        "destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_skirmish_thumbnail_art.png",
        "alpha": "opaque_rectangular_art",
    },
]


LIVE_TEXT = [
    "WARLINE CAPTURE",
    "Credits",
    "187,540",
    "Supplies",
    "92,860",
    "Command",
    "2,715",
    "Campaign",
    "Operations",
    "Skirmish",
    "Store",
    "Commander",
    "Settings",
    "COMMANDER",
    "FIELD COMMANDER",
    "LEVEL 38",
    "READINESS",
    "SQUAD MANAGEMENT",
    "INTEL REPORT",
    "LOCKED",
    "COMMS ONLINE",
    "DEPLOY OPERATION",
]


def is_key_green(r: int, g: int, b: int) -> bool:
    return (
        (g > 145 and r < 115 and b < 115 and g > r + 55 and g > b + 55)
        or (g > 105 and r < 60 and b < 60 and g > r + 45 and g > b + 45)
    )


def is_edge_spill_green(r: int, g: int, b: int) -> bool:
    return g > 95 and r < 85 and b < 85 and g > r + 38 and g > b + 38


def has_transparent_neighbor(px, x: int, y: int, w: int, h: int) -> bool:
    for ny in range(max(0, y - 1), min(h, y + 2)):
        for nx in range(max(0, x - 1), min(w, x + 2)):
            if nx == x and ny == y:
                continue
            if px[nx, ny][3] == 0:
                return True
    return False


def remove_green(crop: Image.Image) -> Image.Image:
    rgba = crop.convert("RGBA")
    px = rgba.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            r, g, b, a = px[x, y]
            if is_key_green(r, g, b):
                px[x, y] = (r, g, b, 0)

    # Remove antialias spill left by the generated green-screen sheet without
    # deleting intentional olive/gold UI accents inside the sprite.
    for _ in range(2):
        to_clear: list[tuple[int, int]] = []
        for y in range(rgba.height):
            for x in range(rgba.width):
                r, g, b, a = px[x, y]
                is_outer_edge = x < 3 or y < 3 or x >= rgba.width - 3 or y >= rgba.height - 3
                if a > 0 and is_edge_spill_green(r, g, b) and (is_outer_edge or has_transparent_neighbor(px, x, y, rgba.width, rgba.height)):
                    to_clear.append((x, y))
        for x, y in to_clear:
            r, g, b, _ = px[x, y]
            px[x, y] = (r, g, b, 0)
    return rgba


def trim_alpha(img: Image.Image, pad: int = 2) -> Image.Image:
    alpha = img.getchannel("A")
    box = alpha.getbbox()
    if box is None:
        return img
    x0, y0, x1, y1 = box
    x0 = max(0, x0 - pad)
    y0 = max(0, y0 - pad)
    x1 = min(img.width, x1 + pad)
    y1 = min(img.height, y1 + pad)
    return img.crop((x0, y0, x1, y1))


def alpha_stats(img: Image.Image) -> dict[str, int]:
    alpha = img.getchannel("A")
    values = list(alpha.getdata())
    transparent = sum(1 for value in values if value == 0)
    partial = sum(1 for value in values if 0 < value < 255)
    opaque = sum(1 for value in values if value == 255)
    return {
        "width": img.width,
        "height": img.height,
        "transparent_pixels": transparent,
        "partial_alpha_pixels": partial,
        "opaque_pixels": opaque,
    }


def make_logo_lockup() -> Image.Image:
    img = trim_alpha(Image.open(BRAND_LOGO).convert("RGBA"), pad=4)
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            if b > 120 and g > 90 and r < 80:
                strength = min(1.0, max(0.0, (b + g - r) / 420.0))
                gold = (215, 154, 31)
                highlight = (255, 193, 74)
                nr = int(gold[0] * (1 - strength) + highlight[0] * strength)
                ng = int(gold[1] * (1 - strength) + highlight[1] * strength)
                nb = int(gold[2] * (1 - strength) + highlight[2] * strength)
                px[x, y] = (nr, ng, nb, a)
    return img


def checkerboard(size: tuple[int, int], cell: int = 12) -> Image.Image:
    w, h = size
    img = Image.new("RGBA", size, (228, 228, 228, 255))
    draw = ImageDraw.Draw(img)
    for y in range(0, h, cell):
        for x in range(0, w, cell):
            if ((x // cell) + (y // cell)) % 2:
                draw.rectangle([x, y, x + cell - 1, y + cell - 1], fill=(178, 178, 178, 255))
    return img


def make_contact_sheet(paths: list[Path]) -> None:
    thumb_w, thumb_h = 220, 150
    label_h = 46
    cols = 4
    rows = (len(paths) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * thumb_w, rows * (thumb_h + label_h)), (28, 31, 28, 255))
    draw = ImageDraw.Draw(sheet)
    for idx, path in enumerate(paths):
        src = Image.open(path).convert("RGBA")
        tile = checkerboard((thumb_w, thumb_h))
        src.thumbnail((thumb_w - 18, thumb_h - 18), Image.Resampling.LANCZOS)
        x = (idx % cols) * thumb_w
        y = (idx // cols) * (thumb_h + label_h)
        tile.alpha_composite(src, ((thumb_w - src.width) // 2, (thumb_h - src.height) // 2))
        sheet.alpha_composite(tile, (x, y))
        label = path.stem.replace("scn02_", "")
        draw.text((x + 8, y + thumb_h + 6), label[:28], fill=(235, 232, 214, 255))
    CONTACT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(CONTACT)


def make_header_preview() -> None:
    names = [
        ("logo bg", "scn02_header_logo_panel_bg.png"),
        ("resource bg", "scn02_header_resource_panel_bg.png"),
        ("command bg", "scn02_header_command_panel_bg.png"),
        ("right actions bg", "scn02_header_right_actions_bg.png"),
        ("logo overlay", "scn02_brand_logo_lockup.png"),
    ]
    widths = [Image.open(LAYERS / filename).width for _, filename in names]
    max_h = max(Image.open(LAYERS / filename).height for _, filename in names)
    gap = 36
    label_h = 32
    canvas = Image.new("RGBA", (sum(widths) + gap * (len(names) + 1), max_h + label_h + 36), (27, 30, 27, 255))
    draw = ImageDraw.Draw(canvas)
    x = gap
    for label, filename in names:
        img = Image.open(LAYERS / filename).convert("RGBA")
        bg = checkerboard((img.width, max_h))
        bg.alpha_composite(img, (0, (max_h - img.height) // 2))
        canvas.alpha_composite(bg, (x, 18))
        draw.text((x, max_h + 24), label, fill=(236, 230, 205, 255))
        x += img.width + gap
    HEADER_PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(HEADER_PREVIEW)


def main() -> None:
    if not SOURCE.exists():
        raise FileNotFoundError(SOURCE)
    if not TARGET.exists():
        raise FileNotFoundError(TARGET)
    if not BACKGROUND.exists():
        raise FileNotFoundError(BACKGROUND)
    if not MODE_THUMBNAILS.exists():
        raise FileNotFoundError(MODE_THUMBNAILS)
    if not BRAND_LOGO.exists():
        raise FileNotFoundError(BRAND_LOGO)

    source = Image.open(SOURCE).convert("RGBA")
    background = Image.open(BACKGROUND).convert("RGBA")
    mode_thumbnails = Image.open(MODE_THUMBNAILS).convert("RGBA")
    LAYERS.mkdir(parents=True, exist_ok=True)

    manifest_layers: list[dict[str, object]] = []
    written: list[Path] = []
    for spec in LAYOUT:
        layer_id = str(spec["id"])
        crop = source.crop(tuple(spec["box"]))  # type: ignore[arg-type]
        extracted = trim_alpha(remove_green(crop))
        out = LAYERS / f"{layer_id}.png"
        extracted.save(out)
        written.append(out)
        manifest_layers.append(
            {
                "id": layer_id,
                "file": str(out.relative_to(ROOT)),
                "source": str(SOURCE.relative_to(ROOT)),
                "source_box": spec["box"],
                "role": spec["role"],
                "unity_destination": spec["destination"],
                "sprite_import_type": "Sprite (2D and UI)",
                "slicing_border": [24, 24, 24, 24] if "frame" in layer_id or "button" in layer_id else [0, 0, 0, 0],
                "alpha_rule": spec["alpha"],
                "live_text_rule": "Contains no baked text. Text, numbers, unlock labels, and route labels must be TMP/runtime-bound.",
                "stats": alpha_stats(extracted),
            }
        )

    source_lookup = {
        "background": background,
        "mode_thumbnails": mode_thumbnails,
    }
    source_path_lookup = {
        "background": BACKGROUND,
        "mode_thumbnails": MODE_THUMBNAILS,
    }
    for spec in OPAQUE_LAYOUT:
        layer_id = str(spec["id"])
        source_name = str(spec["source"])
        src_img = source_lookup[source_name]
        src_path = source_path_lookup[source_name]
        if spec["box"] == "full":
            crop = src_img.copy()
            source_box: object = [0, 0, src_img.width, src_img.height]
        else:
            crop = src_img.crop(tuple(spec["box"]))  # type: ignore[arg-type]
            source_box = spec["box"]
        out = LAYERS / f"{layer_id}.png"
        crop.save(out)
        written.append(out)
        manifest_layers.append(
            {
                "id": layer_id,
                "file": str(out.relative_to(ROOT)),
                "source": str(src_path.relative_to(ROOT)),
                "source_box": source_box,
                "role": spec["role"],
                "unity_destination": spec["destination"],
                "sprite_import_type": "Sprite (2D and UI)",
                "slicing_border": [0, 0, 0, 0],
                "alpha_rule": spec["alpha"],
                "live_text_rule": "Opaque art only. Do not bake gameplay data or mode labels into this layer.",
                "stats": alpha_stats(crop),
            }
        )

    logo = make_logo_lockup()
    logo_out = LAYERS / "scn02_brand_logo_lockup.png"
    logo.save(logo_out)
    written.append(logo_out)
    manifest_layers.append(
        {
            "id": "scn02_brand_logo_lockup",
            "file": str(logo_out.relative_to(ROOT)),
            "source": str(BRAND_LOGO.relative_to(ROOT)),
            "source_box": "trimmed_alpha_full_image",
            "role": "Separate Warline Capture logo lockup for top-left header panel",
            "unity_destination": "Assets/Game/Art/UI/Generated/MainMenu/V15/scn02_brand_logo_lockup.png",
            "sprite_import_type": "Sprite (2D and UI)",
            "slicing_border": [0, 0, 0, 0],
            "alpha_rule": "native_alpha_from_source",
            "live_text_rule": "Logo art only. Do not use for route labels, resources, or dynamic text.",
            "stats": alpha_stats(logo),
        }
    )

    make_contact_sheet(written)
    make_header_preview()

    manifest = {
        "surface_id": "SCN-02_MainMenu",
        "surface_name": "Main Menu",
        "workflow": "VisualLockLayered V15 3D Green-Screen",
        "target_reference": "Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png",
        "source_sheet": str(SOURCE.relative_to(ROOT)),
        "background_art_source": str(BACKGROUND.relative_to(ROOT)),
        "mode_thumbnail_source": str(MODE_THUMBNAILS.relative_to(ROOT)),
        "brand_logo_source": str(BRAND_LOGO.relative_to(ROOT)),
        "contact_sheet": str(CONTACT.relative_to(ROOT)),
        "header_split_preview": str(HEADER_PREVIEW.relative_to(ROOT)),
        "regeneration_status": "Current layer sheet is not final. Header, logo, and right action panel require a fresh generated layer source; do not crop implementation assets from the target reference.",
        "game_direction": "Full 3D single-map mobile RTS command-base main menu, aligned with Demo/Demo2 gameplay references.",
        "live_text": LIVE_TEXT,
        "runtime_bound_values": [
            "credits_amount",
            "supplies_amount",
            "command_amount",
            "selected_mode",
            "mode_progress",
            "districts_controlled",
            "custom_setup_count",
            "commander_level",
            "readiness_meter",
            "feature_unlock_states",
            "comms_status",
        ],
        "layers": manifest_layers,
    }
    MANIFEST.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {len(written)} layers")
    print(CONTACT)
    print(MANIFEST)


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Prepare SCN-10 Support Popup VisualLockLayered reference and layer pack."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "Design" / "VisualLockLayered" / "SCN-10_SupportPopup"
REFERENCE = PACK / "reference"
LAYERS = PACK / "layers"
GENERATED = PACK / "generated_one_go"
SOURCE = GENERATED / "source"
VALIDATION = PACK / "validation"
BASE_HUD = ROOT / "Design" / "VisualLockLayered" / "SCN-08_RTSBattleHUD" / "reference" / "SCN-08_RTSBattleHUD_Landscape_Target.png"

W, H = 2400, 1080


COLORS = {
    "panel": (13, 17, 16, 232),
    "panel2": (20, 24, 22, 235),
    "line": (154, 116, 44, 220),
    "line_dim": (97, 78, 42, 180),
    "gold": (208, 148, 42, 255),
    "gold_dark": (99, 68, 18, 240),
    "green": (132, 169, 66, 255),
    "cyan": (79, 169, 185, 255),
    "red": (188, 61, 42, 255),
    "cream": (213, 205, 180, 255),
    "muted": (128, 124, 104, 255),
}


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf" if bold else "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/Library/Fonts/Arial Bold.ttf" if bold else "/Library/Fonts/Arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    ]
    for candidate in candidates:
        if Path(candidate).exists():
            return ImageFont.truetype(candidate, size)
    return ImageFont.load_default()


def ensure_dirs() -> None:
    for path in (REFERENCE, LAYERS, SOURCE, VALIDATION):
        path.mkdir(parents=True, exist_ok=True)


def transparent(size: tuple[int, int]) -> Image.Image:
    return Image.new("RGBA", size, (0, 0, 0, 0))


def rect_layer(size: tuple[int, int], radius: int = 16, fill=None, outline=None, width: int = 3) -> Image.Image:
    img = transparent(size)
    d = ImageDraw.Draw(img)
    box = (width // 2, width // 2, size[0] - width // 2 - 1, size[1] - width // 2 - 1)
    d.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)
    inset = 8
    if size[0] > inset * 2 + 8 and size[1] > inset * 2 + 8:
        d.rounded_rectangle((inset, inset, size[0] - inset - 1, size[1] - inset - 1), radius=max(radius - 6, 4), outline=COLORS["line_dim"], width=1)
    return img


def button_layer(size: tuple[int, int], selected: bool = False) -> Image.Image:
    fill = (34, 24, 8, 238) if selected else (17, 20, 18, 238)
    outline = COLORS["gold"] if selected else COLORS["line"]
    img = rect_layer(size, 12, fill, outline, 3)
    if selected:
        glow = Image.new("RGBA", size, (0, 0, 0, 0))
        gd = ImageDraw.Draw(glow)
        gd.rounded_rectangle((4, 4, size[0] - 5, size[1] - 5), radius=10, outline=(236, 184, 58, 90), width=8)
        img.alpha_composite(glow)
    return img


def draw_icon(kind: str, size: int = 128) -> Image.Image:
    img = transparent((size, size))
    d = ImageDraw.Draw(img)
    c = COLORS["gold"]
    g = COLORS["green"]
    r = COLORS["red"]
    w = max(4, size // 22)
    cx = cy = size // 2
    if kind == "support":
        d.arc((20, 22, size - 20, size - 12), 200, 340, fill=c, width=w)
        for x in (34, cx, size - 34):
            d.line((cx, 60, x, size - 30), fill=c, width=w // 2)
        d.rounded_rectangle((cx - 22, size - 34, cx + 22, size - 18), radius=4, fill=c)
    elif kind == "drone":
        d.ellipse((cx - 16, cy - 16, cx + 16, cy + 16), outline=c, width=w)
        for ox, oy in ((-36, -30), (36, -30), (-36, 30), (36, 30)):
            d.line((cx, cy, cx + ox, cy + oy), fill=c, width=w)
            d.ellipse((cx + ox - 15, cy + oy - 15, cx + ox + 15, cy + oy + 15), outline=g, width=w)
    elif kind == "airstrike":
        d.polygon([(24, 78), (size - 18, 40), (86, 86), (70, size - 24), (54, 92)], fill=c)
        d.line((28, size - 32, size - 26, size - 32), fill=r, width=w)
        d.line((46, size - 48, size - 44, size - 48), fill=r, width=w)
    elif kind == "smoke":
        for i, x in enumerate((34, 58, 82)):
            d.arc((x, 28 + i * 8, x + 48, 92 + i * 8), 90, 310, fill=(190, 190, 170, 230), width=w)
        d.rounded_rectangle((30, size - 38, size - 30, size - 20), radius=6, fill=c)
    elif kind == "supply":
        d.rectangle((36, 48, size - 36, size - 28), outline=c, width=w)
        d.line((36, 72, size - 36, 72), fill=c, width=w)
        d.line((cx, 48, cx, size - 28), fill=c, width=w)
        d.polygon([(36, 48), (52, 30), (size - 52, 30), (size - 36, 48)], outline=c)
    elif kind == "medevac":
        d.ellipse((30, 36, size - 30, size - 36), outline=c, width=w)
        arm = max(8, size // 13)
        top = int(size * 0.36)
        bottom = int(size * 0.64)
        left = int(size * 0.36)
        right = int(size * 0.64)
        d.rectangle((cx - arm // 2, top, cx + arm // 2, bottom), fill=g)
        d.rectangle((left, cy - arm // 2, right, cy + arm // 2), fill=g)
    elif kind == "reinforce":
        for x in (42, 64, 86):
            d.ellipse((x - 12, 30, x + 12, 54), fill=c)
            d.rounded_rectangle((x - 16, 58, x + 16, 98), radius=5, fill=c)
        d.line((38, size - 22, size - 38, size - 22), fill=g, width=w)
    elif kind == "artillery":
        d.arc((20, 22, size - 20, size - 8), 205, 330, fill=c, width=w)
        for x in (45, 64, 83):
            d.line((x, 40, x + 28, 96), fill=r, width=w)
        d.ellipse((cx - 20, size - 34, cx + 20, size - 14), outline=c, width=w)
    elif kind == "close":
        d.line((34, 34, size - 34, size - 34), fill=c, width=w + 2)
        d.line((size - 34, 34, 34, size - 34), fill=c, width=w + 2)
    elif kind == "command":
        d.polygon([(cx, 22), (size - 22, cy), (cx, size - 22), (22, cy)], outline=c)
        d.ellipse((cx - 15, cy - 15, cx + 15, cy + 15), fill=c)
    elif kind == "cooldown":
        d.ellipse((30, 30, size - 30, size - 30), outline=c, width=w)
        d.line((cx, cy, cx, 42), fill=c, width=w)
        d.line((cx, cy, size - 42, cy), fill=c, width=w)
    elif kind == "charge":
        d.polygon([(cx, 18), (size - 24, 52), (size - 48, size - 22), (24, 52)], outline=c)
        d.line((cx, 36, cx, size - 42), fill=g, width=w)
    elif kind == "target":
        d.ellipse((30, 30, size - 30, size - 30), outline=c, width=w)
        d.line((cx, 18, cx, 44), fill=c, width=w)
        d.line((cx, size - 18, cx, size - 44), fill=c, width=w)
        d.line((18, cy, 44, cy), fill=c, width=w)
        d.line((size - 18, cy, size - 44, cy), fill=c, width=w)
    elif kind == "lock":
        d.rounded_rectangle((34, 58, size - 34, size - 24), radius=6, outline=c, width=w)
        d.arc((42, 24, size - 42, 84), 180, 360, fill=c, width=w)
    elif kind == "warning":
        d.polygon([(cx, 20), (size - 20, size - 24), (20, size - 24)], outline=r, width=w)
        d.line((cx, 52, cx, 86), fill=r, width=w)
        d.ellipse((cx - 4, size - 44, cx + 4, size - 36), fill=r)
    return img


def card_thumb(kind: str, size: tuple[int, int] = (226, 128)) -> Image.Image:
    img = Image.new("RGBA", size, (32, 36, 34, 255))
    d = ImageDraw.Draw(img)
    for y in range(size[1]):
        shade = int(26 + y * 0.28)
        d.line((0, y, size[0], y), fill=(shade, shade + 4, shade + 2, 255))
    if kind in {"drone", "airstrike"}:
        d.rectangle((0, 76, size[0], size[1]), fill=(75, 68, 54, 255))
        d.polygon([(0, 76), (size[0], 56), (size[0], 90), (0, 105)], fill=(52, 61, 54, 255))
    else:
        d.rectangle((0, 70, size[0], size[1]), fill=(60, 55, 45, 255))
        d.rectangle((20, 48, 78, 82), fill=(88, 80, 64, 255))
        d.rectangle((118, 42, 190, 84), fill=(75, 70, 60, 255))
    icon = draw_icon(kind, 76)
    img.alpha_composite(icon, ((size[0] - 76) // 2, 22))
    d.rectangle((0, 0, size[0] - 1, size[1] - 1), outline=COLORS["line_dim"], width=2)
    return img


def save_layer(name: str, img: Image.Image, layers: list[dict], category: str, rule: str, rect=None) -> None:
    file = LAYERS / f"{name}.png"
    img.save(file)
    layers.append(
        {
            "id": name,
            "file": str(file.relative_to(PACK)),
            "category": category,
            "source": "programmatic_clean_layer",
            "sourceRect": rect,
            "unityImport": {
                "textureType": "Sprite",
                "alphaIsTransparency": True,
                "meshType": "FullRect",
            },
            "compositionRule": rule,
        }
    )


def make_layers() -> list[dict]:
    layers: list[dict] = []
    save_layer("chrome_01_support_popup_outer_frame", rect_layer((980, 670), 18, COLORS["panel"], COLORS["line"], 4), layers, "chrome", "Parent popup frame. Keep title, labels, values, cooldowns, and warnings live in Unity.")
    save_layer("chrome_02_support_detail_panel_frame", rect_layer((320, 444), 14, COLORS["panel2"], COLORS["line"], 3), layers, "chrome", "Right detail panel frame. Runtime binds selected support title, rules, costs, target type, disabled reason, and CTA.")
    save_layer("chrome_03_support_card_frame", rect_layer((280, 142), 12, (18, 22, 20, 236), COLORS["line"], 3), layers, "chrome", "Reusable support ability card frame. Art, icon, cost, cooldown, charges, selected and disabled overlays remain separate children.")
    save_layer("chrome_04_support_card_selected_highlight", rect_layer((280, 142), 12, (70, 44, 8, 65), COLORS["gold"], 5), layers, "chrome", "Selected card highlight overlay only.")
    save_layer("chrome_05_support_card_disabled_overlay", rect_layer((280, 142), 12, (0, 0, 0, 145), (70, 70, 60, 160), 2), layers, "chrome", "Disabled/locked overlay. Lock icon and reason text are separate.")
    save_layer("chrome_06_gold_execute_button_bg", button_layer((250, 64), True), layers, "chrome", "Execute/confirm button background. Button label is live TMP.")
    save_layer("chrome_07_secondary_button_bg", button_layer((250, 58), False), layers, "chrome", "Secondary cancel/info button background. Button label is live TMP.")
    save_layer("chrome_08_cooldown_bar_frame", rect_layer((148, 18), 5, (7, 9, 8, 210), COLORS["line_dim"], 2), layers, "chrome", "Cooldown/availability meter frame. Fill is separate.")
    save_layer("chrome_09_cooldown_bar_fill", rect_layer((104, 12), 4, COLORS["green"], None, 0), layers, "meter", "Runtime-scaled fill for cooldown/ready meters.")
    save_layer("chrome_10_charge_pip_ready", rect_layer((18, 18), 4, COLORS["green"], COLORS["line_dim"], 1), layers, "meter", "Single ready charge pip; duplicate at runtime.")
    save_layer("chrome_11_charge_pip_empty", rect_layer((18, 18), 4, (25, 27, 24, 210), COLORS["line_dim"], 1), layers, "meter", "Single empty charge pip; duplicate at runtime.")
    save_layer("chrome_12_warning_chip_bg", rect_layer((232, 44), 8, (55, 23, 14, 232), COLORS["red"], 2), layers, "chrome", "Warning chip background. Warning text and icon are separate.")
    save_layer("chrome_13_instruction_strip", rect_layer((910, 54), 10, (12, 15, 14, 235), COLORS["line_dim"], 2), layers, "chrome", "Bottom instruction strip. Text is live TMP.")
    save_layer("chrome_14_tab_selected_bg", button_layer((204, 54), True), layers, "chrome", "Selected support category tab background. Label is live TMP.")
    save_layer("chrome_15_tab_idle_bg", button_layer((204, 54), False), layers, "chrome", "Idle support category tab background. Label is live TMP.")
    for i, kind in enumerate(["support", "drone", "airstrike", "smoke", "supply", "medevac", "reinforce", "artillery", "close", "command", "cooldown", "charge", "target", "lock", "warning"], start=1):
        save_layer(f"icon_{i:02d}_{kind}", draw_icon(kind), layers, "icon", "Icon layer only. Do not bake labels or values into this asset.")
    for i, kind in enumerate(["drone", "airstrike", "smoke", "supply", "medevac", "reinforce", "artillery"], start=1):
        save_layer(f"thumb_{i:02d}_{kind}_support_thumb", card_thumb(kind), layers, "thumbnail", "Opaque support ability thumbnail. Card labels, costs, cooldowns, charges, and lock states are live/separate.")
    return layers


def text_center(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int], text: str, fnt, fill) -> None:
    bbox = draw.textbbox((0, 0), text, font=fnt)
    x = box[0] + (box[2] - box[0] - (bbox[2] - bbox[0])) // 2
    y = box[1] + (box[3] - box[1] - (bbox[3] - bbox[1])) // 2
    draw.text((x, y), text, font=fnt, fill=fill)


def compose_target() -> None:
    base = Image.open(BASE_HUD).convert("RGBA").resize((W, H))
    overlay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(overlay)
    d.rectangle((0, 0, W, H), fill=(0, 0, 0, 42))
    x, y = 760, 130
    popup = Image.open(LAYERS / "chrome_01_support_popup_outer_frame.png")
    overlay.alpha_composite(popup, (x, y))
    title_f = font(34, True)
    label_f = font(20, True)
    small_f = font(17)
    d.text((x + 76, y + 28), "SUPPORT", font=title_f, fill=COLORS["cream"])
    overlay.alpha_composite(Image.open(LAYERS / "icon_01_support.png").resize((52, 52)), (x + 24, y + 24))
    overlay.alpha_composite(Image.open(LAYERS / "icon_09_close.png").resize((46, 46)), (x + 908, y + 28))
    tab_y = y + 92
    for idx, label in enumerate(["TACTICAL", "LOGISTICS", "EVAC"]):
        tab = Image.open(LAYERS / ("chrome_14_tab_selected_bg.png" if idx == 0 else "chrome_15_tab_idle_bg.png"))
        tx = x + 28 + idx * 214
        overlay.alpha_composite(tab, (tx, tab_y))
        text_center(d, (tx, tab_y, tx + 204, tab_y + 54), label, label_f, COLORS["cream"])
    cards = [
        ("DRONE SCAN", "icon_02_drone.png", "thumb_01_drone_support_thumb.png", "Ready", "2 charges"),
        ("AIRSTRIKE", "icon_03_airstrike.png", "thumb_02_airstrike_support_thumb.png", "Command 4", "00:35"),
        ("SMOKE DROP", "icon_04_smoke.png", "thumb_03_smoke_support_thumb.png", "Ready", "3 charges"),
        ("SUPPLY DROP", "icon_05_supply.png", "thumb_04_supply_support_thumb.png", "Command 2", "Ready"),
        ("MEDEVAC", "icon_06_medevac.png", "thumb_05_medevac_support_thumb.png", "Wounded only", "Ready"),
        ("REINFORCE", "icon_07_reinforce.png", "thumb_06_reinforce_support_thumb.png", "Locked", "Need Lvl 4"),
    ]
    card_frame = Image.open(LAYERS / "chrome_03_support_card_frame.png")
    selected = Image.open(LAYERS / "chrome_04_support_card_selected_highlight.png")
    disabled = Image.open(LAYERS / "chrome_05_support_card_disabled_overlay.png")
    card_positions = []
    for row in range(3):
        for col in range(2):
            card_positions.append((x + 28 + col * 300, y + 164 + row * 154))
    for idx, (name, icon_file, thumb_file, cost, state) in enumerate(cards):
        cx, cy = card_positions[idx]
        overlay.alpha_composite(card_frame, (cx, cy))
        if idx == 1:
            overlay.alpha_composite(selected, (cx, cy))
        thumb = Image.open(LAYERS / thumb_file)
        thumb = thumb.resize((120, 68))
        overlay.alpha_composite(thumb, (cx + 142, cy + 42))
        overlay.alpha_composite(Image.open(LAYERS / icon_file).resize((38, 38)), (cx + 14, cy + 8))
        d.text((cx + 58, cy + 15), name, font=small_f, fill=COLORS["cream"])
        d.text((cx + 18, cy + 62), cost, font=small_f, fill=COLORS["gold"] if "Locked" not in cost else COLORS["red"])
        d.text((cx + 18, cy + 94), state, font=small_f, fill=COLORS["green"] if "Ready" in state or "charges" in state else COLORS["muted"])
        if "Locked" in cost:
            overlay.alpha_composite(disabled, (cx, cy))
            overlay.alpha_composite(Image.open(LAYERS / "icon_14_lock.png").resize((36, 36)), (cx + 18, cy + 92))
    detail_x, detail_y = x + 628, y + 164
    overlay.alpha_composite(Image.open(LAYERS / "chrome_02_support_detail_panel_frame.png"), (detail_x, detail_y))
    d.text((detail_x + 24, detail_y + 24), "AIRSTRIKE", font=label_f, fill=COLORS["cream"])
    overlay.alpha_composite(Image.open(LAYERS / "thumb_02_airstrike_support_thumb.png").resize((268, 150)), (detail_x + 24, detail_y + 64))
    for i, line in enumerate(["ROLE", "Precision strike", "TARGET", "Ground area, hostile only", "COST", "Command 4  |  Cooldown 00:35"]):
        d.text((detail_x + 24, detail_y + 232 + i * 28), line, font=small_f, fill=COLORS["gold"] if i % 2 == 0 else COLORS["cream"])
    overlay.alpha_composite(Image.open(LAYERS / "chrome_06_gold_execute_button_bg.png"), (detail_x + 34, detail_y + 360))
    text_center(d, (detail_x + 34, detail_y + 360, detail_x + 284, detail_y + 424), "SELECT TARGET", label_f, (20, 18, 12, 255))
    overlay.alpha_composite(Image.open(LAYERS / "chrome_13_instruction_strip.png"), (x + 35, y + 598))
    text_center(d, (x + 35, y + 598, x + 945, y + 652), "SELECT SUPPORT, THEN TAP VALID TARGET AREA.", small_f, COLORS["cream"])
    # Highlight the bottom command Support button region in the existing HUD.
    d.rounded_rectangle((1623, 860, 1738, 1048), radius=12, outline=(237, 185, 58, 230), width=6)
    d.rounded_rectangle((1629, 866, 1732, 1042), radius=10, outline=(237, 185, 58, 90), width=12)
    out = Image.alpha_composite(base, overlay)
    out.save(REFERENCE / "SCN-10_SupportPopup_OnExistingMatchHUD_TargetLock_V01.png")
    out.save(REFERENCE / "SCN-10_SupportPopup_Landscape_Target.png")


def make_contact_sheet(layer_paths: Iterable[Path]) -> None:
    items = list(layer_paths)
    thumb_w, thumb_h = 220, 160
    cols = 4
    rows = (len(items) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * thumb_w, rows * thumb_h), (36, 36, 36, 255))
    d = ImageDraw.Draw(sheet)
    f = font(13)
    for idx, path in enumerate(items):
        col = idx % cols
        row = idx // cols
        x = col * thumb_w
        y = row * thumb_h
        tile = Image.new("RGBA", (thumb_w, thumb_h), (44, 44, 44, 255))
        td = ImageDraw.Draw(tile)
        for yy in range(0, thumb_h, 16):
            for xx in range(0, thumb_w, 16):
                if (xx // 16 + yy // 16) % 2 == 0:
                    td.rectangle((xx, yy, xx + 15, yy + 15), fill=(58, 58, 58, 255))
        img = Image.open(path).convert("RGBA")
        img.thumbnail((thumb_w - 24, thumb_h - 46), Image.LANCZOS)
        tile.alpha_composite(img, ((thumb_w - img.width) // 2, 10 + (thumb_h - 46 - img.height) // 2))
        td.rectangle((0, 0, thumb_w - 1, thumb_h - 1), outline=(90, 90, 90, 255))
        label = path.stem[:28]
        td.text((8, thumb_h - 28), label, font=f, fill=(230, 230, 220, 255))
        sheet.alpha_composite(tile, (x, y))
    sheet.save(GENERATED / "layers_contact_sheet.png")
    sheet.save(VALIDATION / "SCN-10_SupportPopup_layers_contact_sheet.png")


def write_manifest(layers: list[dict]) -> None:
    manifest = {
        "scene": "SCN-10_SupportPopup",
        "reference": "reference/SCN-10_SupportPopup_Landscape_Target.png",
        "workflow": "VisualLockLayered V15 3D structured UI layer pack",
        "openedFrom": "SCN-08_RTSBattleHUD Support command button",
        "layout": {
            "canvasSize": {"width": W, "height": H},
        "popupRect": {"x": 760, "y": 130, "width": 980, "height": 670},
            "safeAreaBehavior": "Keep anchored above the command bar and left of the minimap at 16:9 and 20:9. Do not cover the bottom command bar Support button feedback.",
        },
        "notes": [
            "Support opens as an in-match popup/drawer, similar to SCN-09 BuildDrawer, not a full screen.",
            "Panel chrome, card frames, thumbnails, icons, cooldown bars, charge pips, warning chips, and instruction strips are separate layers.",
            "Live text, costs, cooldown numbers, charges, target prompts, disabled reasons, and ability availability must be rendered by Unity.",
            "Support execution enters Support Targeting Mode only after the player chooses an ability that requires a target.",
        ],
        "layers": layers,
    }
    (PACK / "layer_manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")


def write_readme() -> None:
    text = """# SCN-10 Support Popup VisualLockLayered

Status: Target-lock mockup and V01 implementation layer pack generated.
Date: 2026-06-08

## Active Target

- Reference target: `reference/SCN-10_SupportPopup_Landscape_Target.png`
- Existing-HUD target: `reference/SCN-10_SupportPopup_OnExistingMatchHUD_TargetLock_V01.png`
- Canonical layout context: `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png`
- Canonical size: `2400 x 1080`

This target is the Support popup opened from the SCN-08 match HUD. It follows the same in-match popup/drawer language as `SCN-09_BuildDrawer`, but it is for off-map and auxiliary support abilities rather than build/production.

## Layer Pack

- Manifest: `layer_manifest.json`
- Layers: `layers/`
- Contact sheet: `generated_one_go/layers_contact_sheet.png`
- Validation contact sheet: `validation/SCN-10_SupportPopup_layers_contact_sheet.png`

## Runtime Behavior

1. Player taps `SUPPORT` on the match HUD.
2. The Support popup opens and owns UI input. World taps behind it must not leak through.
3. Player selects a support ability such as Drone Scan, Airstrike, Smoke Drop, Supply Drop, Medevac, or Reinforcement.
4. If the ability needs a target, the popup closes or collapses and the HUD enters Support Targeting Mode.
5. Player taps a valid map target. Resources/charges/cooldown are spent only on accepted execution.
6. HUD returns to the previous selected-unit state unless the ability explicitly supports repeat targeting.

## Layer Rules Applied

- Do not cut the target-lock mockup into implementation sprites.
- Do not bake labels, cooldown values, charges, lock reasons, costs, or progress bars into reusable chrome.
- Keep popup frame, detail frame, card frames, selected/disabled overlays, icons, thumbnails, cooldown fills, charge pips, warning chips, and instruction strip as separate sprites.
- Keep support ability data live from mission/equipment/runtime support definitions.
- Keep style aligned with SCN-08 Battle HUD and SCN-09 Build Drawer.

## Design Source

- `Design/Match_HUD_And_Gameplay_Implementation_Spec.md`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`
- `Design/VisualLockLayered/SCN-09_BuildDrawer/README.md`
"""
    (PACK / "README.md").write_text(text, encoding="utf-8")


def main() -> None:
    ensure_dirs()
    layers = make_layers()
    compose_target()
    make_contact_sheet(sorted(LAYERS.glob("*.png")))
    write_manifest(layers)
    write_readme()
    print(REFERENCE / "SCN-10_SupportPopup_Landscape_Target.png")
    print(GENERATED / "layers_contact_sheet.png")
    print(PACK / "layer_manifest.json")


if __name__ == "__main__":
    main()

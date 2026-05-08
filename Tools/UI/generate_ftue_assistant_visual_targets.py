#!/usr/bin/env python3
from __future__ import annotations

import json
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[2]
VISUAL_LOCK = ROOT / "Design" / "VisualLock"
LAYERED = ROOT / "Design" / "VisualLockLayered"
FONT_DIR = ROOT / "Assets" / "Synty" / "InterfaceMilitaryCombatHUD" / "Fonts" / "Oxanium"
WIDTH = 1672
HEIGHT = 941

MATCH_BASE = "Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png"
COMMANDER_BASE = "Design/VisualLock/SCN-03_CommanderProfile/SCN-03_CommanderProfile_Landscape_Target.png"


COLORS = {
    "cyan": (74, 224, 245, 245),
    "cyan_soft": (45, 154, 178, 210),
    "orange": (248, 170, 44, 245),
    "amber": (255, 210, 76, 245),
    "green": (92, 220, 142, 240),
    "red": (246, 78, 70, 240),
    "white": (236, 245, 245, 255),
    "muted": (154, 178, 184, 245),
    "panel": (7, 15, 20, 238),
    "panel2": (12, 25, 33, 244),
    "panel3": (18, 36, 45, 246),
}


def font(name: str, size: int) -> ImageFont.FreeTypeFont:
    path = FONT_DIR / name
    if path.exists():
        return ImageFont.truetype(str(path), size=size)
    return ImageFont.load_default()


FONTS = {
    "light18": font("Oxanium-Light.ttf", 18),
    "light22": font("Oxanium-Light.ttf", 22),
    "light26": font("Oxanium-Light.ttf", 26),
    "bold18": font("Oxanium-Bold.ttf", 18),
    "bold22": font("Oxanium-Bold.ttf", 22),
    "bold24": font("Oxanium-Bold.ttf", 24),
    "bold26": font("Oxanium-Bold.ttf", 26),
    "bold30": font("Oxanium-Bold.ttf", 30),
    "bold32": font("Oxanium-Bold.ttf", 32),
    "bold40": font("Oxanium-Bold.ttf", 40),
    "bold52": font("Oxanium-Bold.ttf", 52),
}


def ensure(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)


def load_base(relative_path: str) -> Image.Image:
    img = Image.open(ROOT / relative_path).convert("RGBA")
    if img.size != (WIDTH, HEIGHT):
        img = img.resize((WIDTH, HEIGHT), Image.Resampling.LANCZOS)
    return img


def blurred_context(relative_path: str) -> Image.Image:
    base = load_base(relative_path)
    blur = base.filter(ImageFilter.GaussianBlur(7))
    scrim = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 118))
    blur.alpha_composite(scrim)
    vignette = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    vd = ImageDraw.Draw(vignette, "RGBA")
    for i in range(84):
        a = int(i * 1.8)
        vd.rectangle((i, i, WIDTH - i, HEIGHT - i), outline=(0, 0, 0, a))
    blur.alpha_composite(vignette)
    return blur


def cut_poly(rect: tuple[int, int, int, int], cut: int) -> list[tuple[int, int]]:
    x1, y1, x2, y2 = rect
    return [
        (x1 + cut, y1),
        (x2 - cut, y1),
        (x2, y1 + cut),
        (x2, y2 - cut),
        (x2 - cut, y2),
        (x1 + cut, y2),
        (x1, y2 - cut),
        (x1, y1 + cut),
    ]


def hud_text(draw: ImageDraw.ImageDraw, xy: tuple[int, int], value: str, font_key: str,
             fill="white", anchor=None, stroke: int = 2) -> None:
    color = COLORS[fill] if isinstance(fill, str) else fill
    draw.text(
        xy,
        value,
        font=FONTS[font_key],
        fill=color,
        anchor=anchor,
        stroke_width=stroke,
        stroke_fill=(0, 0, 0, 210),
    )


def text_width(draw: ImageDraw.ImageDraw, value: str, font_key: str) -> int:
    box = draw.textbbox((0, 0), value, font=FONTS[font_key])
    return box[2] - box[0]


def wrap_text(draw: ImageDraw.ImageDraw, xy: tuple[int, int], value: str, font_key: str,
              max_width: int, fill="muted", line_gap: int = 8) -> int:
    words = value.split()
    lines: list[str] = []
    current = ""
    for word in words:
        candidate = word if not current else f"{current} {word}"
        if text_width(draw, candidate, font_key) <= max_width:
            current = candidate
        else:
            if current:
                lines.append(current)
            current = word
    if current:
        lines.append(current)
    x, y = xy
    for i, line in enumerate(lines):
        hud_text(draw, (x, y + i * (FONTS[font_key].size + line_gap)), line, font_key, fill=fill)
    return y + len(lines) * (FONTS[font_key].size + line_gap)


def panel(img: Image.Image, rect: tuple[int, int, int, int], accent=COLORS["cyan"],
          fill=COLORS["panel"], cut: int = 22, border: int = 2, glow: int = 16) -> ImageDraw.ImageDraw:
    x1, y1, x2, y2 = rect
    shadow = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow, "RGBA")
    sd.polygon(cut_poly((x1 + 14, y1 + 18, x2 + 14, y2 + 18), cut), fill=(0, 0, 0, 170))
    sd.polygon(cut_poly((x1 - 2, y1 - 2, x2 + 2, y2 + 2), cut), outline=(accent[0], accent[1], accent[2], 120), fill=(0, 0, 0, 0))
    img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(glow)))

    d = ImageDraw.Draw(img, "RGBA")
    d.polygon(cut_poly(rect, cut), fill=fill, outline=accent)
    for i in range(1, border):
        d.line(cut_poly((x1 + i, y1 + i, x2 - i, y2 - i), max(1, cut - i)), fill=accent, width=1)
    d.line((x1 + cut + 10, y1 + 6, x1 + 220, y1 + 6), fill=(accent[0], accent[1], accent[2], 170), width=3)
    d.line((x2 - 220, y2 - 6, x2 - cut - 10, y2 - 6), fill=(accent[0], accent[1], accent[2], 130), width=3)
    return d


def button(draw: ImageDraw.ImageDraw, rect: tuple[int, int, int, int], label: str,
           selected: bool = False, danger: bool = False) -> None:
    accent = COLORS["red"] if danger else (COLORS["orange"] if selected else COLORS["cyan"])
    fill = (56, 16, 20, 240) if danger else ((58, 38, 10, 242) if selected else COLORS["panel2"])
    draw.polygon(cut_poly(rect, 12), fill=fill, outline=accent)
    hud_text(draw, ((rect[0] + rect[2]) // 2, rect[1] + 15), label, "bold22", fill="white", anchor="ma")


def meter(draw: ImageDraw.ImageDraw, rect: tuple[int, int, int, int], fraction: float, color=COLORS["cyan"]) -> None:
    draw.rectangle(rect, fill=(6, 14, 18, 220), outline=(80, 114, 120, 160))
    x1, y1, x2, y2 = rect
    fill_x = x1 + int((x2 - x1) * fraction)
    draw.rectangle((x1 + 3, y1 + 3, fill_x - 3, y2 - 3), fill=color)


def avatar(draw: ImageDraw.ImageDraw, center: tuple[int, int], radius: int, label: str,
           accent=COLORS["cyan"], fill=(19, 63, 76, 245)) -> None:
    cx, cy = center
    draw.ellipse((cx - radius, cy - radius, cx + radius, cy + radius), fill=fill, outline=accent, width=4)
    draw.ellipse((cx - radius + 18, cy - radius + 18, cx + radius - 18, cy + radius - 18), outline=(accent[0], accent[1], accent[2], 90), width=2)
    hud_text(draw, (cx, cy - 12), label, "bold22", fill=accent, anchor="ma")


def header(draw: ImageDraw.ImageDraw, x: int, y: int, title: str, subtitle: str | None = None,
           accent="cyan") -> None:
    hud_text(draw, (x, y), title, "bold40", fill=accent)
    if subtitle:
        hud_text(draw, (x, y + 48), subtitle, "light22", fill="muted")


def make_assistant_panel() -> Image.Image:
    img = blurred_context(MATCH_BASE)
    d = panel(img, (930, 92, 1588, 846), accent=COLORS["cyan"], fill=(6, 15, 20, 246), cut=28, glow=22)
    avatar(d, (1002, 184), 54, "ARIA")
    header(d, 1080, 128, "ARIA", "Adaptive Response Intelligence Assistant")
    for i, label in enumerate(["NEXT", "WHY", "PLAN", "GOALS"]):
        button(d, (982 + i * 136, 260, 1098 + i * 136, 306), label, selected=i == 0)
    hud_text(d, (982, 354), "RECOMMENDED NOW", "bold30", fill="orange")
    d.polygon(cut_poly((982, 394, 1538, 554), 16), fill=COLORS["panel3"], outline=COLORS["orange"])
    hud_text(d, (1014, 420), "Select Rifle Squad", "bold32")
    wrap_text(d, (1014, 466), "Move the squad to marked cover before the hostile patrol reaches the clinic road.", "light22", 452)
    for idx, (label, status) in enumerate([
        ("Select squad", "ready"),
        ("Move to cover", "recommended"),
        ("Avoid invalid target", "blocked until selected"),
    ]):
        y = 586 + idx * 54
        d.polygon(cut_poly((982, y, 1538, y + 40), 10), fill=(10, 25, 32, 228), outline=COLORS["cyan_soft"])
        hud_text(d, (1006, y + 8), label, "light22")
        hud_text(d, (1390, y + 8), status, "light18", fill="muted")
    button(d, (982, 770, 1132, 824), "SHOW ME")
    button(d, (1160, 770, 1308, 824), "DO IT", selected=True)
    button(d, (1336, 770, 1490, 824), "STOP", danger=True)
    return img


def make_tutorial_card() -> Image.Image:
    img = blurred_context(MATCH_BASE)
    d = panel(img, (124, 500, 880, 838), accent=COLORS["cyan"], fill=(5, 14, 19, 247), cut=28, glow=20)
    avatar(d, (208, 604), 58, "ARIA")
    header(d, 294, 548, "SELECT THE RIFLE SQUAD", "FTUE 01 / Basic command loop")
    wrap_text(d, (294, 648), "Orders start with selection. Tap the highlighted squad card or squad on the road, then choose MOVE and send them to the marked cover point.", "light22", 480)
    meter(d, (294, 740, 624, 758), 0.2, COLORS["cyan"])
    hud_text(d, (642, 732), "1 / 5", "bold22", fill="muted")
    button(d, (294, 782, 408, 828), "SKIP")
    button(d, (432, 782, 580, 828), "SHOW ME")
    button(d, (606, 782, 744, 828), "DO IT", selected=True)
    return img


def make_tutorial_highlight() -> Image.Image:
    img = blurred_context(MATCH_BASE)
    d = panel(img, (206, 146, 1466, 786), accent=COLORS["cyan"], fill=(5, 14, 19, 246), cut=30, glow=22)
    header(d, 264, 194, "TUTORIAL HIGHLIGHT SYSTEM", "Reusable focus rings, path previews, and blocked-action feedback")
    preview = (282, 306, 1390, 670)
    d.polygon(cut_poly(preview, 18), fill=(14, 30, 34, 235), outline=COLORS["cyan_soft"])
    d.line((382, 540, 590, 458, 820, 520, 1080, 390), fill=COLORS["orange"], width=7)
    for x, y in [(382, 540), (590, 458), (820, 520), (1080, 390)]:
        d.ellipse((x - 9, y - 9, x + 9, y + 9), fill=COLORS["orange"])
    d.ellipse((326, 472, 484, 604), outline=COLORS["green"], width=7)
    d.ellipse((1002, 338, 1164, 442), outline=COLORS["red"], width=7)
    d.ellipse((1168, 508, 1324, 612), outline=COLORS["cyan"], width=7)
    for label, x, y, color in [
        ("WORLD TARGET", 344, 612, "green"),
        ("PATH PREVIEW", 606, 538, "orange"),
        ("ATTACK TARGET", 998, 454, "red"),
        ("UI BUTTON", 1176, 622, "cyan"),
    ]:
        d.polygon(cut_poly((x, y, x + 220, y + 44), 10), fill=(4, 14, 18, 230), outline=COLORS[color])
        hud_text(d, (x + 110, y + 10), label, "bold18", fill=color, anchor="ma")
    d.polygon(cut_poly((680, 688, 1010, 738), 12), fill=(48, 13, 16, 238), outline=COLORS["red"])
    hud_text(d, (704, 702), "BLOCKED: Finish this step first", "bold22", fill="red")
    return img


def make_takeover() -> Image.Image:
    img = blurred_context(MATCH_BASE)
    d = panel(img, (284, 168, 1388, 740), accent=COLORS["orange"], fill=(8, 15, 18, 248), cut=30, glow=24)
    d.ellipse((342, 226, 462, 346), fill=(50, 35, 10, 235), outline=COLORS["orange"], width=4)
    hud_text(d, (402, 260), "ARIA", "bold22", fill="orange", anchor="ma")
    header(d, 504, 224, "ARIA CONTROLLING", "Tap anywhere to resume command", accent="orange")
    d.polygon(cut_poly((504, 348, 1324, 530), 18), fill=COLORS["panel3"], outline=COLORS["orange"])
    hud_text(d, (538, 374), "CURRENT INTENT", "bold26", fill="orange")
    hud_text(d, (538, 420), "Move Rifle Squad to cover", "bold32")
    wrap_text(d, (538, 466), "ARIA owns one bounded command, then returns control. No spending, no raid confirmation, no full mission autopilot.", "light22", 650)
    for i, label in enumerate(["1  Move to cover", "2  Hold position", "3  Scan patrol route"]):
        y = 560 + i * 44
        d.polygon(cut_poly((504, y, 988, y + 34), 8), fill=(10, 24, 30, 224), outline=COLORS["cyan_soft"])
        hud_text(d, (528, y + 6), label, "light22")
    button(d, (1030, 594, 1184, 646), "RESUME")
    button(d, (1208, 594, 1360, 646), "STOP ARIA", danger=True)
    return img


def make_assistant_button() -> Image.Image:
    img = blurred_context(MATCH_BASE)
    d = panel(img, (184, 178, 1488, 708), accent=COLORS["cyan"], fill=(5, 14, 19, 248), cut=30, glow=22)
    header(d, 244, 228, "PREFAB-04 ASSISTANT BUTTON STATES", "Persistent ARIA affordance for HUD and tutorial surfaces")
    states = [
        ("IDLE", "ARIA", COLORS["cyan"], "No active recommendation."),
        ("RECOMMEND", "NEXT", COLORS["green"], "Useful next action available."),
        ("CRITICAL", "WARN", COLORS["red"], "Failure risk needs attention."),
        ("TAKEOVER", "CTRL", COLORS["orange"], "ARIA owns one command."),
        ("MUTED", "OFF", (138, 154, 160, 230), "Proactive help disabled."),
    ]
    for i, (title, short, color, desc) in enumerate(states):
        cx = 330 + i * 250
        cy = 420
        d.ellipse((cx - 70, cy - 70, cx + 70, cy + 70), fill=(10, 30, 38, 240), outline=color, width=6)
        d.ellipse((cx - 88, cy - 88, cx + 88, cy + 88), outline=(color[0], color[1], color[2], 90), width=3)
        hud_text(d, (cx, cy - 14), short, "bold22", fill=color, anchor="ma")
        hud_text(d, (cx, cy + 108), title, "bold22", anchor="ma")
        d.polygon(cut_poly((cx - 104, cy + 146, cx + 104, cy + 218), 10), fill=(10, 24, 30, 224), outline=color)
        wrap_text(d, (cx - 82, cy + 162), desc, "light18", 164)
    return img


def make_commander_identity() -> Image.Image:
    img = blurred_context(COMMANDER_BASE)
    d = panel(img, (308, 104, 1368, 840), accent=COLORS["cyan"], fill=(5, 14, 19, 248), cut=30, glow=24)
    header(d, 370, 154, "COMMANDER IDENTITY", "Choose the icon and frame used in profile, rewards, and operation reports")
    d = panel(img, (370, 252, 694, 718), accent=COLORS["orange"], fill=COLORS["panel2"], cut=22, glow=8)
    hud_text(d, (404, 282), "CURRENT PROFILE", "bold24", fill="orange")
    d.rectangle((430, 334, 634, 538), fill=(13, 31, 38, 235), outline=COLORS["cyan"], width=3)
    avatar(d, (532, 430), 64, "C7X", accent=COLORS["cyan"])
    hud_text(d, (430, 568), "CALLSIGN", "bold18", fill="muted")
    hud_text(d, (430, 596), "COMMANDER_7X", "bold22")
    hud_text(d, (430, 636), "TITLE: FIELD COMMANDER", "bold22", fill="orange")

    d = panel(img, (742, 252, 1306, 718), accent=COLORS["cyan"], fill=COLORS["panel2"], cut=22, glow=8)
    hud_text(d, (776, 282), "CHOOSE COMMANDER ICON", "bold24", fill="cyan")
    for row in range(2):
        for col in range(3):
            x = 786 + col * 166
            y = 338 + row * 138
            accent = COLORS["orange"] if row == 0 and col == 0 else COLORS["cyan"]
            d.polygon(cut_poly((x, y, x + 124, y + 108), 12), fill=(10, 24, 30, 232), outline=accent)
            avatar(d, (x + 62, y + 48), 34, f"{row * 3 + col + 1}", accent=accent)
            hud_text(d, (x + 62, y + 82), f"ICON {row * 3 + col + 1}", "light18", anchor="ma")
    for i, label in enumerate(["DEFAULT", "IRON GUARD", "OP STABILIZER"]):
        button(d, (786 + i * 166, 624, 928 + i * 166, 670), label, selected=i == 0)
    button(d, (890, 752, 1052, 804), "CANCEL")
    button(d, (1080, 752, 1294, 804), "CONFIRM", selected=True)
    return img


SURFACES = {
    "POP-11_CommanderIdentity": {
        "image": make_commander_identity,
        "sourceBase": COMMANDER_BASE,
        "purpose": "Commander profile icon selection popup over blurred Commander Profile context.",
        "controls": ["CommanderNameInput", "CommanderIconGrid", "FrameGrid", "ConfirmButton", "CancelButton"],
    },
    "PREFAB-05_AssistantPanel": {
        "image": make_assistant_panel,
        "sourceBase": MATCH_BASE,
        "purpose": "High-quality ARIA recommendation panel over blurred Match Overlay context.",
        "controls": ["AssistantTabs", "RecommendationChips", "ShowMeButton", "DoItButton", "StopButton"],
    },
    "PREFAB-06_TutorialCard": {
        "image": make_tutorial_card,
        "sourceBase": MATCH_BASE,
        "purpose": "High-quality FTUE tutorial card over blurred Match Overlay context.",
        "controls": ["TutorialBody", "SkipButton", "ShowMeButton", "DoItButton", "WorldAnchor"],
    },
    "PREFAB-07_TutorialHighlight": {
        "image": make_tutorial_highlight,
        "sourceBase": MATCH_BASE,
        "purpose": "High-quality tutorial highlight component showcase over blurred Match Overlay context.",
        "controls": ["WorldTargetRing", "UiTargetRing", "PathPreview", "BlockedFeedback"],
    },
    "POP-10_AssistantTakeover": {
        "image": make_takeover,
        "sourceBase": MATCH_BASE,
        "purpose": "High-quality ARIA takeover popup over blurred Match Overlay context.",
        "controls": ["TakeoverBanner", "CurrentIntentCard", "StopNowButton", "ResumeButton"],
    },
    "PREFAB-04_AssistantButton": {
        "image": make_assistant_button,
        "sourceBase": MATCH_BASE,
        "purpose": "High-quality assistant button state board over blurred Match Overlay context.",
        "controls": ["IdleState", "RecommendationState", "CriticalState", "TakeoverState", "MutedState"],
    },
}


def write_visual_lock(surface: str, img: Image.Image, source_base: str, purpose: str, controls: list[str]) -> None:
    out = VISUAL_LOCK / surface
    ensure(out)
    target = out / f"{surface}_Landscape_Target.png"
    img.convert("RGB").save(target, quality=96)
    notes = f"""# {surface} Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/{surface}/{surface}_Landscape_Target.png`.
- Source background: `{source_base}` blurred and dimmed for panel/popup presentation.
- Source design: `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`.
- Purpose: {purpose}
- Direction: focused high-quality panel/popup target over WarlineCapture UI context. This is not a new gameplay HUD layout.
- Layering: flat mockup reference only; no new sliced/layered art pack is required for this visual target pass.

## Required Controls

{chr(10).join(f"- `{control}`" for control in controls)}

## Implementation Notes

- Match WarlineCapture's dark military RTS HUD language, Oxanium typography, cyan edge light, and restrained amber action accents.
- Keep labels readable in the target. Unity implementation should use live TMP text.
- ARIA and commander portrait/icon art shown here is placeholder visual-lock content until approved in `Design/WarlineCapture_Art_Asset_Requirements_Register.csv`.
"""
    (out / f"{surface}_CleanLandscape_Notes.md").write_text(notes, encoding="utf-8")
    manifest = {
        "surfaceId": surface,
        "target": str(target.relative_to(ROOT)),
        "sourceBackground": source_base,
        "source": "Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md",
        "purpose": purpose,
        "controls": controls,
        "canvas": {"width": WIDTH, "height": HEIGHT},
        "workflow": "flat-panel-popup-target-over-blurred-warlinecapture-context",
        "requiresSeparatedLayerPack": False,
    }
    (out / f"{surface}_Target_State_Manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")


def write_reference_pack(surface: str, source_base: str, purpose: str, controls: list[str]) -> None:
    root = LAYERED / surface
    ensure(root / "reference")
    ensure(root / "generated_one_go")
    ensure(root / "layers" / "Content")
    ensure(root / "prompts")
    src = VISUAL_LOCK / surface / f"{surface}_Landscape_Target.png"
    for dst in [
        root / "reference" / f"{surface}_Landscape_Target.png",
        root / "generated_one_go" / "layers_contact_sheet.png",
        root / "layers" / "Content" / "state_reference_plate.png",
    ]:
        shutil.copy2(src, dst)

    readme = f"""# {surface} Flat Visual Target Reference

This folder keeps a reference copy for the WarlineCapture visual-lock index.

- Reference target: `reference/{surface}_Landscape_Target.png`
- Source background: `{source_base}` blurred and dimmed for panel/popup context.
- Source design: `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- Purpose: {purpose}

This target is a flat high-quality panel/popup mockup. It does not require a new sliced layer pack for this design pass.
"""
    (root / "README.md").write_text(readme, encoding="utf-8")

    prompt = f"""Use case: ui-mockup
Asset type: flat WarlineCapture panel/popup visual target, 1672x941.

Primary request:
Use this target as a high-quality visual reference for `{surface}`.

Source background:
`{source_base}` blurred and dimmed behind the focused panel/popup.

Purpose:
{purpose}

Required visible controls:
{chr(10).join(f"- {control}" for control in controls)}

Rules:
- This is a flat mockup target, not a sliced layer-pack request.
- Keep the background as WarlineCapture UI context; do not invent a different game UI.
- Keep all labels readable in the reference target.
"""
    (root / "prompts" / "flat_panel_popup_target.md").write_text(prompt, encoding="utf-8")
    old_layer_prompt = root / "prompts" / "high_end_target_and_layers.md"
    if old_layer_prompt.exists():
        old_layer_prompt.unlink()

    manifest = {
        "surface": surface,
        "reference": f"reference/{surface}_Landscape_Target.png",
        "sourceBackground": source_base,
        "source": "Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md",
        "status": "flat-panel-popup-target",
        "requiresSeparatedLayerPack": False,
        "requiredControls": controls,
    }
    (root / "layer_manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")


def main() -> None:
    for surface, spec in SURFACES.items():
        img = spec["image"]()
        write_visual_lock(surface, img, spec["sourceBase"], spec["purpose"], spec["controls"])
        write_reference_pack(surface, spec["sourceBase"], spec["purpose"], spec["controls"])


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
VISUAL_LOCK = ROOT / "Design" / "VisualLock"
FONT_DIR = ROOT / "Assets" / "Synty" / "InterfaceMilitaryCombatHUD" / "Fonts" / "Oxanium"
WIDTH = 1672
HEIGHT = 941


COLORS = {
    "bg": (8, 13, 17),
    "panel": (15, 24, 30, 218),
    "panel2": (20, 32, 40, 232),
    "cyan": (80, 214, 232, 235),
    "cyan_dim": (42, 112, 128, 180),
    "orange": (242, 154, 52, 235),
    "orange_dim": (155, 89, 31, 190),
    "green": (98, 202, 123, 220),
    "red": (232, 72, 68, 230),
    "text": (229, 241, 242, 245),
    "muted": (147, 174, 180, 230),
    "dark": (3, 7, 10, 235),
}


def font(name: str, size: int) -> ImageFont.FreeTypeFont:
    path = FONT_DIR / name
    if path.exists():
        return ImageFont.truetype(str(path), size=size)
    return ImageFont.load_default()


FONT_LIGHT_18 = font("Oxanium-Light.ttf", 18)
FONT_LIGHT_22 = font("Oxanium-Light.ttf", 22)
FONT_LIGHT_28 = font("Oxanium-Light.ttf", 28)
FONT_BOLD_24 = font("Oxanium-Bold.ttf", 24)
FONT_BOLD_30 = font("Oxanium-Bold.ttf", 30)
FONT_BOLD_38 = font("Oxanium-Bold.ttf", 38)
FONT_BOLD_48 = font("Oxanium-Bold.ttf", 48)


def ensure_dir(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)


def load_base(relative: str) -> Image.Image:
    path = ROOT / relative
    return Image.open(path).convert("RGBA")


def save_target(surface: str, image: Image.Image, notes: str, manifest: dict) -> None:
    out_dir = VISUAL_LOCK / surface
    ensure_dir(out_dir)
    out_path = out_dir / f"{surface}_Landscape_Target.png"
    image.convert("RGB").save(out_path, quality=96)
    (out_dir / f"{surface}_CleanLandscape_Notes.md").write_text(notes, encoding="utf-8")
    (out_dir / f"{surface}_Target_State_Manifest.json").write_text(
        json.dumps(manifest, indent=2), encoding="utf-8"
    )


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


def panel(draw: ImageDraw.ImageDraw, rect: tuple[int, int, int, int], label: str | None = None,
          accent: tuple[int, int, int, int] = COLORS["cyan"], fill=COLORS["panel"],
          cut: int = 18, border: int = 2) -> None:
    poly = cut_poly(rect, cut)
    draw.polygon(poly, fill=fill, outline=accent)
    for i in range(1, border):
        x1, y1, x2, y2 = rect
        draw.line(cut_poly((x1 + i, y1 + i, x2 - i, y2 - i), max(1, cut - i)), fill=accent, width=1)
    if label:
        draw.text((rect[0] + 20, rect[1] + 14), label, font=FONT_BOLD_24, fill=COLORS["text"])


def button(draw: ImageDraw.ImageDraw, rect: tuple[int, int, int, int], label: str,
           selected: bool = False, icon: str | None = None) -> None:
    accent = COLORS["orange"] if selected else COLORS["cyan_dim"]
    fill = (33, 52, 60, 235) if selected else (14, 24, 31, 225)
    panel(draw, rect, None, accent=accent, fill=fill, cut=12, border=2)
    if icon:
        draw.text((rect[0] + 18, rect[1] + 12), icon, font=FONT_BOLD_30, fill=accent)
        text_x = rect[0] + 58
    else:
        text_x = rect[0] + 18
    draw.text((text_x, rect[1] + (rect[3] - rect[1] - 24) // 2), label, font=FONT_BOLD_24 if selected else FONT_LIGHT_22, fill=COLORS["text"])


def arrow(draw: ImageDraw.ImageDraw, xy: tuple[int, int], color=COLORS["cyan"]) -> None:
    x, y = xy
    draw.polygon([(x, y), (x + 18, y + 10), (x, y + 20), (x + 5, y + 10)], fill=color)


def ring(draw: ImageDraw.ImageDraw, cx: int, cy: int, rx: int, ry: int, color, width: int = 4) -> None:
    for i in range(width):
        draw.ellipse((cx - rx - i, cy - ry - i, cx + rx + i, cy + ry + i), outline=color)


def write_label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], title: str, subtitle: str | None = None) -> None:
    draw.text(xy, title, font=FONT_BOLD_24, fill=COLORS["text"])
    if subtitle:
        draw.text((xy[0], xy[1] + 30), subtitle, font=FONT_LIGHT_18, fill=COLORS["muted"])


def make_scn08_state() -> None:
    surface = "SCN-08_RTSBattleHUD_M01_TacticalFeedback"
    img = load_base("Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png")
    overlay = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    d = ImageDraw.Draw(overlay)

    # Tactical, not strategic, gameplay read: close-up road, units, runtime markers.
    d.rectangle((360, 142, 1296, 708), fill=(13, 22, 23, 170))
    d.polygon([(360, 435), (610, 330), (970, 405), (1296, 300), (1296, 430), (980, 515), (600, 455), (360, 555)], fill=(54, 62, 57, 205))
    d.line([(380, 506), (600, 415), (960, 470), (1270, 378)], fill=(112, 132, 122, 170), width=18)
    d.line([(380, 506), (600, 415), (960, 470), (1270, 378)], fill=(210, 189, 110, 130), width=2)

    # Runtime entities and markers.
    ring(d, 520, 480, 58, 30, COLORS["green"], 4)
    d.ellipse((492, 438, 548, 492), fill=(62, 132, 96, 240), outline=COLORS["green"], width=3)
    d.text((480, 502), "RIFLE SQUAD", font=FONT_LIGHT_18, fill=COLORS["text"])
    ring(d, 1068, 440, 60, 30, COLORS["red"], 4)
    d.ellipse((1038, 398, 1098, 452), fill=(132, 56, 52, 240), outline=COLORS["red"], width=3)
    d.text((1018, 462), "HOSTILE PATROL", font=FONT_LIGHT_18, fill=COLORS["text"])
    d.line((548, 468, 750, 455, 948, 436, 1058, 425), fill=COLORS["orange"], width=5)
    arrow(d, (748, 445), COLORS["orange"])
    ring(d, 748, 455, 28, 15, COLORS["cyan"], 3)
    d.text((718, 477), "MOVE", font=FONT_BOLD_24, fill=COLORS["cyan"])
    ring(d, 1068, 440, 78, 40, COLORS["orange"], 3)

    # New HUD state surfaces.
    panel(d, (1268, 150, 1588, 244), "COMMAND MODE", accent=COLORS["orange"], fill=COLORS["panel2"], cut=16)
    d.text((1290, 195), "ATTACK TARGETING", font=FONT_BOLD_30, fill=COLORS["orange"])
    panel(d, (498, 726, 1074, 846), "SELECTED ENTITY", accent=COLORS["cyan"], fill=COLORS["panel2"], cut=18)
    d.ellipse((528, 773, 594, 833), fill=(67, 122, 101, 255), outline=COLORS["cyan"], width=3)
    d.text((618, 766), "RIFLE SQUAD 01", font=FONT_BOLD_30, fill=COLORS["text"])
    d.text((618, 804), "Order: Attack  |  HP 100%  |  Commands: Move, Attack, Hold", font=FONT_LIGHT_22, fill=COLORS["muted"])
    panel(d, (596, 626, 1076, 684), None, accent=COLORS["red"], fill=(52, 20, 22, 232), cut=14)
    d.text((626, 643), "INVALID: Target outside mission area", font=FONT_BOLD_24, fill=COLORS["red"])
    panel(d, (1320, 618, 1578, 778), "MINIMAP", accent=COLORS["cyan"], fill=(8, 17, 21, 230), cut=18)
    d.rectangle((1350, 674, 1544, 742), outline=COLORS["orange"], width=3)
    d.line((1342, 724, 1548, 690), fill=COLORS["cyan_dim"], width=4)
    d.ellipse((1414, 704, 1432, 722), fill=COLORS["green"])
    d.ellipse((1492, 690, 1512, 710), fill=COLORS["red"])

    img.alpha_composite(overlay)
    notes = f"""# {surface}

State target generated from the accepted `SCN-08_RTSBattleHUD` target to reflect the 2026-05-07 strategic/tactical gameplay update.

This target is not a replacement for the base HUD chrome. It is the M01 tactical-feedback state target for `saga.ch01.m01.first_contact`.

Required new UI items shown here:

- `BattleHud.SelectedEntityPanel`
- `BattleHud.CommandModeBanner`
- `BattleHud.WorldCommandMarkerLayer`
- `BattleHud.InvalidCommandToast`
- `BattleHud.MinimapCameraBridge`
- selection, move, attack, invalid, objective/minimap feedback over a close-up tactical map, not a strategic preview

Canvas implementation must still use a `Design/VisualLockLayered/{surface}` layer pack before prefab work.
"""
    manifest = {
        "surfaceId": surface,
        "sourceBase": "Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png",
        "purpose": "M01 tactical command feedback state target",
        "newElements": [
            "BattleHud.SelectedEntityPanel",
            "BattleHud.CommandModeBanner",
            "BattleHud.WorldCommandMarkerLayer",
            "BattleHud.InvalidCommandToast",
            "BattleHud.MinimapCameraBridge",
        ],
    }
    save_target(surface, img, notes, manifest)


def make_state_from_base(surface: str, base: str, title: str, blocks: list[dict]) -> None:
    img = load_base(base)
    overlay = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    d = ImageDraw.Draw(overlay)
    panel(d, (74, 58, 728, 132), title, accent=COLORS["orange"], fill=(9, 16, 20, 226), cut=18)
    y = 164
    for block in blocks:
        rect = block["rect"]
        panel(d, rect, block["title"], accent=block.get("accent", COLORS["cyan"]), fill=block.get("fill", COLORS["panel2"]), cut=16)
        for line_idx, line in enumerate(block.get("lines", [])):
            d.text((rect[0] + 24, rect[1] + 52 + line_idx * 30), line, font=FONT_LIGHT_22, fill=block.get("textColor", COLORS["text"]))
        for btn_idx, btn_label in enumerate(block.get("buttons", [])):
            bx = rect[0] + 24 + btn_idx * 170
            by = rect[3] - 62
            button(d, (bx, by, bx + 148, by + 42), btn_label, selected=btn_idx == 0)
        y += 90
    img.alpha_composite(overlay)
    notes = f"""# {surface}

State target generated to align the existing target with the 2026-05-07 tactical/strategic gameplay update.

Base target:

`{base}`

This state target records missing or stale UI behavior from the updated design docs. Create the matching `Design/VisualLockLayered/{surface}` layer pack before Canvas implementation.
"""
    save_target(surface, img, notes, {"surfaceId": surface, "sourceBase": base, "blocks": blocks})


def make_support_targets() -> None:
    make_state_from_base(
        "SCN-09_BuildDrawer_M01DisabledState",
        "Design/VisualLock/SCN-09_BuildDrawerProduction/SCN-09_BuildDrawerProduction_Landscape_Target.png",
        "M01 BUILD DRAWER - DISABLED CONTRACT STATE",
        [
            {
                "rect": (250, 190, 874, 344),
                "title": "BUILD UNAVAILABLE",
                "accent": COLORS["orange"],
                "lines": ["Mission does not allow construction.", "Reason: MissionDoesNotAllowBuild"],
                "buttons": ["DETAIL", "CLOSE"],
            },
            {
                "rect": (934, 190, 1458, 344),
                "title": "ITEM AVAILABILITY REASONS",
                "lines": ["Locked, unaffordable, mission-banned,", "producer missing, or designed unavailable."],
            },
        ],
    )
    make_state_from_base(
        "SCN-10_UnitCommandWheel_TargetingState",
        "Design/VisualLock/SCN-10_UnitCommandWheel/SCN-10_UnitCommandWheel_Landscape_Target.png",
        "COMMAND WHEEL - MOVE / ATTACK TARGETING STATES",
        [
            {
                "rect": (1040, 178, 1548, 318),
                "title": "TARGET HINT",
                "accent": COLORS["orange"],
                "lines": ["Tap enemy patrol to attack.", "Invalid targets show a reason."],
            },
            {
                "rect": (1040, 352, 1548, 492),
                "title": "DISABLED REASON",
                "accent": COLORS["red"],
                "lines": ["SPECIAL unavailable: no ability", "equipped for Rifle Squad 01."],
            },
        ],
    )
    make_state_from_base(
        "POP-01_ThreatAlert_RoutePreviewState",
        "Design/VisualLock/POP-01_ThreatAlert/POP-01_ThreatAlert_Landscape_Target.png",
        "THREAT ALERT - ROUTE PREVIEW AND JUMP STATE",
        [
            {
                "rect": (760, 244, 1380, 512),
                "title": "ROUTE PREVIEW",
                "accent": COLORS["orange"],
                "lines": ["route.enemy_patrol_01", "ETA 00:45  |  Strength: Light"],
                "buttons": ["JUMP", "CLOSE"],
            },
        ],
    )
    make_state_from_base(
        "POP-03_BuildPlacement_MetadataValidityState",
        "Design/VisualLock/POP-03_BuildPlacement/POP-03_BuildPlacement_Landscape_Target.png",
        "BUILD PLACEMENT - METADATA VALIDITY STATE",
        [
            {
                "rect": (356, 228, 908, 508),
                "title": "FOOTPRINT OVERLAY",
                "accent": COLORS["green"],
                "lines": ["Valid cells use green outline.", "Blocked/socket mismatch uses red.", "Socket: pad.forward_command_01"],
            },
            {
                "rect": (958, 228, 1398, 508),
                "title": "CONFIRM STATE",
                "accent": COLORS["red"],
                "lines": ["Confirm disabled until footprint,", "cost, socket, and rotation are valid."],
                "buttons": ["ROTATE", "CANCEL", "CONFIRM"],
            },
        ],
    )
    make_state_from_base(
        "POP-05_MissionResult_M01ContractState",
        "Design/VisualLock/POP-05_MissionResult/POP-05_MissionResult_Landscape_Target.png",
        "MISSION RESULT - M01 SOURCE SUMMARY STATE",
        [
            {
                "rect": (236, 708, 1458, 842),
                "title": "TACTICAL SOURCE SUMMARY",
                "accent": COLORS["cyan"],
                "lines": [
                    "MissionId saga.ch01.m01.first_contact  |  Scenario scenario.ch01.m01.first_contact",
                    "Level level.ch01.district_edge_01  |  IsoMap iso.ch01.district_edge_01",
                ],
            },
        ],
    )


def make_new_ftue_targets() -> None:
    targets = [
        (
            "PREFAB-04_AssistantButton",
            "ARIA assistant persistent entry point with recommendation, critical alert, muted, and takeover states.",
            [("NORMAL", COLORS["cyan"]), ("RECOMMEND", COLORS["orange"]), ("CRITICAL", COLORS["red"])],
        ),
        (
            "PREFAB-05_AssistantPanel",
            "ARIA recommendations panel with Show Me, Do It, Stop, explanation, objective context, and city/tactical context.",
            [("SHOW ME", COLORS["cyan"]), ("DO IT", COLORS["orange"]), ("STOP", COLORS["red"])],
        ),
        (
            "PREFAB-06_TutorialCard",
            "Contextual tutorial card anchored to UI/world target with ARIA portrait, body, skip/show/do controls.",
            [("SKIP", COLORS["cyan_dim"]), ("SHOW ME", COLORS["cyan"]), ("DO IT", COLORS["orange"])],
        ),
        (
            "PREFAB-07_TutorialHighlight",
            "World/UI highlight layer with pulse ring, pointer, path preview, and blocked-action feedback.",
            [("UI RING", COLORS["cyan"]), ("PATH", COLORS["orange"]), ("BLOCKED", COLORS["red"])],
        ),
        (
            "POP-10_AssistantTakeover",
            "Blocking ownership banner for ARIA controlling state with tap-anywhere cancel/resume affordance.",
            [("ARIA CONTROLLING", COLORS["orange"]), ("TAP TO RESUME", COLORS["cyan"])],
        ),
        (
            "POP-11_CommanderIdentity",
            "Commander name, portrait grid, frame grid, title selector, confirm/cancel controls.",
            [("PORTRAITS", COLORS["cyan"]), ("FRAMES", COLORS["orange"]), ("CONFIRM", COLORS["green"])],
        ),
    ]
    for surface, purpose, controls in targets:
        img = Image.new("RGBA", (WIDTH, HEIGHT), COLORS["bg"])
        d = ImageDraw.Draw(img)
        d.rectangle((0, 0, WIDTH, HEIGHT), fill=(6, 12, 16, 255))
        d.line((0, 72, WIDTH, 72), fill=COLORS["cyan_dim"], width=2)
        d.line((0, HEIGHT - 72, WIDTH, HEIGHT - 72), fill=COLORS["cyan_dim"], width=2)
        panel(d, (88, 86, WIDTH - 88, HEIGHT - 86), surface.replace("_", " "), accent=COLORS["cyan"], fill=(10, 18, 24, 244), cut=30, border=2)
        d.text((132, 148), purpose, font=FONT_LIGHT_28, fill=COLORS["muted"])
        panel(d, (132, 228, 516, 548), "ARIA / COMMAND", accent=COLORS["orange"], fill=COLORS["panel2"], cut=20)
        d.ellipse((226, 308, 424, 506), fill=(32, 58, 72, 255), outline=COLORS["cyan"], width=3)
        d.text((274, 388), "ARIA", font=FONT_BOLD_38, fill=COLORS["cyan"])
        panel(d, (568, 228, 1514, 548), "STATE CONTENT", accent=COLORS["cyan"], fill=COLORS["panel2"], cut=20)
        y = 304
        for idx, line in enumerate([
            "Typed tactical targets only: UI element ids, runtime entities, tactical anchors.",
            "No raw screen-coordinate instructions. No strategic preview targeting after deploy.",
            "Uses close-up tactical map anchors for select, move, attack, build, objective, and minimap jumps.",
        ]):
            d.text((610, y + idx * 44), line, font=FONT_LIGHT_28, fill=COLORS["text"])
        x = 568
        for label, accent in controls:
            button(d, (x, 626, x + 260, 694), label, selected=accent == COLORS["orange"], icon=None)
            x += 294
        if surface == "POP-11_CommanderIdentity":
            for i in range(6):
                px = 612 + i * 96
                d.ellipse((px, 364, px + 68, 432), fill=(28, 52 + i * 10, 70, 255), outline=COLORS["cyan"], width=2)
            panel(d, (612, 454, 1186, 514), "TITLE: FIELD COMMANDER", accent=COLORS["orange"], fill=(20, 30, 36, 245), cut=12)
        if surface == "PREFAB-07_TutorialHighlight":
            ring(d, 1018, 388, 144, 72, COLORS["orange"], 5)
            d.line((736, 584, 1038, 430), fill=COLORS["orange"], width=5)
            arrow(d, (1028, 420), COLORS["orange"])
        notes = f"""# {surface}

New VisualLock target created for the 2026-05-07 FTUE / ARIA design update.

Purpose:

{purpose}

Implementation rule: create a matching `Design/VisualLockLayered/{surface}` layer pack before any Unity Canvas prefab work.
"""
        save_target(surface, img, notes, {"surfaceId": surface, "purpose": purpose, "controls": [c[0] for c in controls]})


def main() -> None:
    make_scn08_state()
    make_support_targets()
    make_new_ftue_targets()
    make_contact_sheet()


def make_contact_sheet() -> None:
    surfaces = [
        "SCN-08_RTSBattleHUD_M01_TacticalFeedback",
        "SCN-09_BuildDrawer_M01DisabledState",
        "SCN-10_UnitCommandWheel_TargetingState",
        "POP-01_ThreatAlert_RoutePreviewState",
        "POP-03_BuildPlacement_MetadataValidityState",
        "POP-05_MissionResult_M01ContractState",
        "PREFAB-04_AssistantButton",
        "PREFAB-05_AssistantPanel",
        "PREFAB-06_TutorialCard",
        "PREFAB-07_TutorialHighlight",
        "POP-10_AssistantTakeover",
        "POP-11_CommanderIdentity",
    ]
    thumb_w = 418
    thumb_h = 235
    cols = 3
    rows = 4
    label_h = 42
    sheet = Image.new("RGBA", (cols * thumb_w, rows * (thumb_h + label_h)), (6, 10, 14, 255))
    draw = ImageDraw.Draw(sheet)
    for idx, surface in enumerate(surfaces):
        src = VISUAL_LOCK / surface / f"{surface}_Landscape_Target.png"
        if not src.exists():
            continue
        im = Image.open(src).convert("RGBA").resize((thumb_w, thumb_h), Image.Resampling.LANCZOS)
        x = (idx % cols) * thumb_w
        y = (idx // cols) * (thumb_h + label_h)
        sheet.alpha_composite(im, (x, y))
        draw.rectangle((x, y + thumb_h, x + thumb_w, y + thumb_h + label_h), fill=(11, 20, 26, 255))
        draw.text((x + 12, y + thumb_h + 10), surface, font=FONT_LIGHT_18, fill=COLORS["text"])
    sheet.convert("RGB").save(VISUAL_LOCK / "TacticalStrategic_TargetRefresh_2026-05-07_ContactSheet.png", quality=94)


if __name__ == "__main__":
    main()

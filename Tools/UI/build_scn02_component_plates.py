#!/usr/bin/env python3
"""Build SCN-02 as frame-first layout data with independent child sprites."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from PIL import Image


PROJECT = Path(__file__).resolve().parents[2]
SOURCE = PROJECT / "Design/VisualLockLayered/SCN-02_MainMenu/imagegen_standalone_20260519/assets"
OUT = PROJECT / "Design/VisualLockLayered/SCN-02_MainMenu/component_plates_20260519/assets"
LAYOUT = PROJECT / "Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_menu_layout.json"
COMPONENT_SLOT_REPORT = PROJECT / "Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_slot_report.json"
CANVAS = (3840, 2160)
SHELL_SAFE_INSET = 72
MIN_CONTENT_GAP = 6


def load(name: str) -> Image.Image:
    return Image.open(SOURCE / name).convert("RGBA")


def trim_alpha(img: Image.Image, padding: int = 3) -> Image.Image:
    bbox = img.getbbox()
    if bbox is None:
        return img
    left, top, right, bottom = bbox
    return img.crop((
        max(0, left - padding),
        max(0, top - padding),
        min(img.width, right + padding),
        min(img.height, bottom + padding),
    ))


def save_trimmed(source_name: str, output_name: str, padding: int = 3) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    trim_alpha(load(source_name), padding).save(OUT / output_name, optimize=True)


def rect_edges(rect: list[int]) -> tuple[int, int, int, int]:
    x, y, w, h = rect
    return x, y, x + w, y + h


def inside(inner: list[int], outer: list[int], padding: int = 0) -> bool:
    ix1, iy1, ix2, iy2 = rect_edges(inner)
    ox1, oy1, ox2, oy2 = rect_edges(outer)
    return ix1 >= ox1 + padding and iy1 >= oy1 + padding and ix2 <= ox2 - padding and iy2 <= oy2 - padding


def intersects(a: list[int], b: list[int]) -> bool:
    ax1, ay1, ax2, ay2 = rect_edges(a)
    bx1, by1, bx2, by2 = rect_edges(b)
    return ax1 < bx2 and ax2 > bx1 and ay1 < by2 and ay2 > by1


def gap(a: list[int], b: list[int]) -> int:
    ax, ay, aw, ah = a
    bx, by, bw, bh = b
    horizontal = max(bx - (ax + aw), ax - (bx + bw), 0)
    vertical = max(by - (ay + ah), ay - (by + bh), 0)
    if horizontal == 0:
        return vertical
    if vertical == 0:
        return horizontal
    return min(horizontal, vertical)


def img(
    name: str,
    file: str,
    rect: list[int],
    z: int,
    *,
    role: str = "content",
    fit: str = "stretch",
    container: str | None = None,
    slot_kind: str | None = None,
    safe_rect: list[int] | None = None,
    tint: list[float] | None = None,
) -> dict[str, Any]:
    data: dict[str, Any] = {
        "name": name,
        "file": file,
        "rect": rect,
        "fit": fit,
        "z": z,
        "role": role,
    }
    if container:
        data["container"] = container
    if slot_kind:
        data["slotKind"] = slot_kind
    if safe_rect:
        data["safeRect"] = safe_rect
    if tint:
        data["tint"] = tint
    return data


def txt(
    name: str,
    value: str,
    rect: list[int],
    font_size: int,
    container: str,
    *,
    alignment: str = "left",
    weight: str = "light",
    color: list[float] | None = None,
) -> dict[str, Any]:
    return {
        "name": name,
        "text": value,
        "rect": rect,
        "fontSize": font_size,
        "alignment": alignment,
        "weight": weight,
        "color": color or [0.82, 0.84, 0.86, 1.0],
        "z": 700,
        "container": container,
        "slotKind": "text",
    }


def add_panel_specs(layout_images: list[dict[str, Any]], layout_texts: list[dict[str, Any]]) -> dict[str, dict[str, Any]]:
    panels = {entry["name"]: entry for entry in layout_images if entry.get("role") == "functional-panel"}
    children_by_panel: dict[str, list[dict[str, Any]]] = {name: [] for name in panels}

    for entry in layout_images:
        container = entry.get("container")
        if container in children_by_panel:
            children_by_panel[container].append(entry)

    for entry in layout_texts:
        container = entry.get("container")
        if container in children_by_panel:
            children_by_panel[container].append(entry)

    report: dict[str, dict[str, Any]] = {}
    for panel_name, panel in panels.items():
        safe_rect = panel.get("safeRect", panel["rect"])
        failures: list[str] = []
        active_children = []
        for child in children_by_panel[panel_name]:
            if not inside(child["rect"], safe_rect):
                failures.append(f"{child['name']} outside {panel_name} safe rect")
            if child.get("slotKind") not in {"frame", "art", "background"}:
                active_children.append(child)

        for index, left in enumerate(active_children):
            for right in active_children[index + 1:]:
                if intersects(left["rect"], right["rect"]):
                    failures.append(f"{left['name']} overlaps {right['name']}")
                elif gap(left["rect"], right["rect"]) < MIN_CONTENT_GAP:
                    failures.append(f"{left['name']} too close to {right['name']}; gap<{MIN_CONTENT_GAP}")

        report[panel_name] = {
            "rect": panel["rect"],
            "safeRect": safe_rect,
            "children": {child["name"]: child["rect"] for child in children_by_panel[panel_name]},
            "failures": failures,
        }
        if failures:
            raise ValueError(f"{panel_name} safe-zone validation failed: {failures}")

    return report


def build_trimmed_assets() -> None:
    for source_name in [
        "brand_logo_lockup.png",
        "settings_gear_icon.png",
        "icon_credits.png",
        "icon_materials.png",
        "icon_command_authority.png",
        "left_nav_icon_inbox.png",
        "left_nav_icon_store.png",
        "left_nav_icon_events.png",
        "left_nav_icon_ranking.png",
        "left_nav_icon_command_feed.png",
        "lock_icon.png",
        "mode_card_header_emblem_saga.png",
        "mode_card_header_emblem_operation.png",
        "mode_card_header_emblem_quick_custom.png",
        "card_footer_icon_saga.png",
        "card_footer_icon_operation.png",
        "card_footer_icon_quick_custom.png",
        "operation_warning_icon.png",
        "deploy_command_chevrons.png",
    ]:
        save_trimmed(source_name, f"ui_{source_name}")


def write_layout() -> None:
    images: list[dict[str, Any]] = [
        img("BackgroundTacticalMap", "main_menu_background_tactical_map.png", [0, 0, 3840, 2160], 0, role="background"),
    ]
    texts: list[dict[str, Any]] = []

    images += [
        img("LogoPanel", "brand_logo_panel_frame.png", [72, 72, 820, 226], 300, role="functional-panel", safe_rect=[112, 98, 700, 148]),
        img("LogoLockup", "ui_brand_logo_lockup.png", [156, 108, 540, 112], 700, fit="contain", container="LogoPanel", slot_kind="logo"),
        img("TopBar", "top_resource_bar_frame_full.png", [944, 72, 2770, 238], 300, role="functional-panel", safe_rect=[1000, 104, 2660, 170]),
        img("CreditsIcon", "ui_icon_credits.png", [1104, 114, 116, 116], 700, fit="contain", container="TopBar", slot_kind="icon"),
        img("MaterialsIcon", "ui_icon_materials.png", [1876, 114, 116, 116], 700, fit="contain", container="TopBar", slot_kind="icon"),
        img("AuthorityIcon", "ui_icon_command_authority.png", [2690, 108, 128, 128], 700, fit="contain", container="TopBar", slot_kind="icon"),
        img("SettingsGear", "ui_settings_gear_icon.png", [3540, 118, 104, 104], 700, fit="contain", container="TopBar", slot_kind="icon"),
    ]
    texts += [
        txt("CreditsLabel", "Credits", [1290, 112, 220, 42], 36, "TopBar", color=[0.78, 0.80, 0.82, 1.0]),
        txt("CreditsValue", "187,540", [1290, 164, 330, 72], 60, "TopBar", weight="bold", color=[0.96, 0.76, 0.48, 1.0]),
        txt("MaterialsLabel", "Materials", [2058, 112, 250, 42], 36, "TopBar", color=[0.78, 0.80, 0.82, 1.0]),
        txt("MaterialsValue", "92,860", [2058, 164, 330, 72], 60, "TopBar", weight="bold", color=[0.68, 0.84, 0.96, 1.0]),
        txt("AuthorityLabel", "Command Authority", [2876, 112, 420, 42], 36, "TopBar", color=[0.78, 0.80, 0.82, 1.0]),
        txt("AuthorityValue", "2,715", [2876, 164, 270, 72], 60, "TopBar", weight="bold", color=[0.96, 0.76, 0.48, 1.0]),
    ]

    images += [
        img("ProfilePanel", "commander_profile_panel_frame.png", [72, 326, 740, 640], 300, role="functional-panel", safe_rect=[120, 370, 644, 552]),
        img("ProfilePortrait", "commander_profile_portrait.png", [132, 442, 600, 350], 520, fit="cover", container="ProfilePanel", slot_kind="art"),
        img("ProfileStatusStrip", "profile_data_status_strip.png", [162, 822, 520, 58], 620, container="ProfilePanel", slot_kind="frame"),
    ]
    texts += [
        txt("CommanderProfileTitle", "Commander Profile", [132, 396, 420, 48], 38, "ProfilePanel", color=[0.18, 0.78, 0.95, 1.0]),
        txt("CommanderProfilePending", "Profile data pending", [176, 880, 490, 42], 31, "ProfilePanel", alignment="center", color=[0.72, 0.75, 0.78, 1.0]),
    ]

    nav_labels = [
        ("Inbox", "ui_left_nav_icon_inbox.png"),
        ("Store", "ui_left_nav_icon_store.png"),
        ("Events", "ui_left_nav_icon_events.png"),
        ("Ranking", "ui_left_nav_icon_ranking.png"),
        ("Command Feed", "ui_left_nav_icon_command_feed.png"),
    ]
    for index, (label, icon_file) in enumerate(nav_labels):
        y = 980 + index * 150
        panel_name = f"Nav{index}Panel"
        images += [
            img(panel_name, "left_nav_row_frame.png", [72, y, 740, 150], 300, role="functional-panel", safe_rect=[110, y + 24, 662, 102]),
            img(f"Nav{index}Icon", icon_file, [124, y + 30, 98, 90], 700, fit="contain", container=panel_name, slot_kind="icon"),
            img(f"Nav{index}DisabledFrame", "disabled_status_pill_frame.png", [500, y + 40, 138, 70], 650, container=panel_name, slot_kind="frame"),
            img(f"Nav{index}LockFrame", "lock_badge_frame.png", [680, y + 36, 72, 78], 650, fit="contain", container=panel_name, slot_kind="frame"),
            img(f"Nav{index}LockIcon", "ui_lock_icon.png", [700, y + 55, 32, 40], 700, fit="contain", container=panel_name, slot_kind="icon"),
        ]
        texts += [
            txt(f"Nav{index}Label", label, [252, y + 42, 226 if label != "Command Feed" else 246, 64], 42 if label != "Command Feed" else 32, panel_name, weight="bold"),
            txt(f"Nav{index}Unavailable", "Designed\nUnavailable", [510, y + 50, 116, 50], 18, panel_name, alignment="center", color=[0.70, 0.75, 0.78, 1.0]),
        ]

    cards = [
        ("Saga", 960, "mode_card_art_saga.png", "ui_mode_card_header_emblem_saga.png", "ui_card_footer_icon_saga.png", "Saga Campaign", "Play through the story arc\nand reclaim key districts."),
        ("Operation", 1850, "mode_card_art_operation.png", "ui_mode_card_header_emblem_operation.png", "ui_card_footer_icon_operation.png", "Persistent Operation", "Maintain control and manage\ndistrict and city operations."),
        ("QuickCustom", 2742, "mode_card_art_quick_custom.png", "ui_mode_card_header_emblem_quick_custom.png", "ui_card_footer_icon_quick_custom.png", "Quick Custom Game", "Set up a custom scenario\nand jump into battle."),
    ]
    for name, x, art_file, header_icon, footer_icon, title, description in cards:
        panel = f"{name}Card"
        images.append(img(panel, "mode_card_frame.png", [x, 374, 810, 1280], 300, role="functional-panel", safe_rect=[x + 50, 410, 710, 1188]))
        images.append(img(f"{name}Art", art_file, [x + 66, 560, 678, 760 if name != "Operation" else 500], 220, fit="cover", container=panel, slot_kind="art"))
        images.append(img(f"{name}HeaderIcon", header_icon, [x + 88, 436, 92, 92], 700, fit="contain", container=panel, slot_kind="icon"))
        images.append(img(f"{name}FooterBadgeFrame", "circular_badge_frame.png", [x + 76, 1450, 112, 112], 650, fit="contain", container=panel, slot_kind="frame"))
        images.append(img(f"{name}FooterIcon", footer_icon, [x + 94, 1468, 76, 76], 700, fit="contain", container=panel, slot_kind="icon"))
        texts.append(txt(f"{name}Title", title, [x + 220, 438, 540, 70], 46 if name != "Operation" else 44, panel, weight="bold", color=[0.82, 0.82, 0.80, 1.0]))
        texts.append(txt(f"{name}Description", description, [x + 224, 1464, 500, 110], 31, panel, color=[0.72, 0.76, 0.78, 1.0]))

    operation_panel = "OperationCard"
    warning_rows = [
        ("Pressure", 1110, "District pressure rising", "operation_pressure_meter_segments.png", "HIGH", [2390, 1190, 112, 38]),
        ("Risk", 1262, "City operation risk", "operation_risk_meter_segments.png", "ELEVATED", [2390, 1342, 154, 38]),
    ]
    for name, y, label, meter_file, status, status_rect in warning_rows:
        images += [
            img(f"OperationWarning{name}Frame", "operation_warning_row_frame.png", [1908, y, 700, 128], 620, container=operation_panel, slot_kind="frame"),
            img(f"OperationWarning{name}Icon", "ui_operation_warning_icon.png", [1938, y + 32, 62, 62], 700, fit="contain", container=operation_panel, slot_kind="icon"),
            img(f"Operation{name}Meter", meter_file, [2030, y + 80, 350, 32], 700, container=operation_panel, slot_kind="meter"),
        ]
        texts += [
            txt(f"Operation{name}Text", label, [2030, y + 28, 360, 38], 28, operation_panel, weight="bold", color=[1.0, 0.63, 0.10, 1.0]),
            txt(f"Operation{name}Status", status, status_rect, 25, operation_panel, alignment="right", weight="bold", color=[1.0, 0.63, 0.10, 1.0]),
        ]

    images += [
        img("DeployButton", "deploy_command_button_frame.png", [2630, 1788, 1086, 236], 300, role="functional-panel", safe_rect=[2690, 1838, 940, 136]),
        img("DeployChevrons", "ui_deploy_command_chevrons.png", [3470, 1858, 150, 72], 700, fit="contain", container="DeployButton", slot_kind="icon"),
    ]
    texts.append(txt("DeployCommandLabel", "DEPLOY COMMAND", [2800, 1852, 650, 88], 62, "DeployButton", alignment="center", weight="bold", color=[1.0, 0.67, 0.25, 1.0]))

    report = add_panel_specs(images, texts)
    COMPONENT_SLOT_REPORT.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

    data = {
        "schema": "warlinecapture.ui.frameFirstLayout.v2",
        "screen": "SCN-02_MainMenu",
        "canvas": list(CANVAS),
        "layoutPolicy": {
            "shellSafeInset": SHELL_SAFE_INSET,
            "minimumPanelGap": 0,
            "minimumTextPadding": 0,
            "minimumChildPadding": 0,
            "minimumContentGap": MIN_CONTENT_GAP,
            "fontSizeFloor": 18,
            "panelOverlapPolicy": "fail",
            "contentOverlapPolicy": "fail-except-frames-and-art",
            "safeAreaSource": "panel.safeRect",
        },
        "assetPolicy": {
            "approvedRoots": [
                "Design/VisualLockLayered/SCN-02_MainMenu/imagegen_standalone_20260519/assets",
                "Design/VisualLockLayered/SCN-02_MainMenu/component_plates_20260519/assets",
            ],
            "runtimeDestination": "Assets/Game/Art/UI/Generated/MainMenu/ComponentCanvas/Cleaned",
        },
        "images": images,
        "texts": texts,
    }
    LAYOUT.parent.mkdir(parents=True, exist_ok=True)
    LAYOUT.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    build_trimmed_assets()
    write_layout()
    print(f"frame-first assets: {OUT}")
    print(f"frame-first layout: {LAYOUT}")
    print(f"frame-first slot report: {COMPONENT_SLOT_REPORT}")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Validate the SCN-02 one-go layout manifest before Unity import."""

from __future__ import annotations

import json
import sys
from pathlib import Path

from PIL import Image


PROJECT = Path(__file__).resolve().parents[2]
LAYOUT = PROJECT / "Design/VisualLockLayered/SCN-02_MainMenu/scn02_main_menu_layout.json"
EXPECTED_ROOT = "Design/VisualLockLayered/SCN-02_MainMenu/imagegen_standalone_20260519/assets"
FORBIDDEN = (
    "Option3",
    "LayerCanvasTest",
    "SourceAssetsBatch01",
    "ImageGenFlat",
    "LayeredOneGo",
    "target_slice",
    "full_visual_lock_preview",
    "contact_sheet",
    "comparison",
    "screenshot",
    "imagegen_cleaned",
)


def fail(message: str) -> None:
    raise SystemExit(f"SCN02_LAYOUT_INVALID: {message}")


def rect(item: dict[str, object]) -> tuple[float, float, float, float]:
    values = item.get("rect")
    if not isinstance(values, list) or len(values) != 4:
        fail(f"{item.get('name')} has invalid rect")
    x, y, w, h = (float(v) for v in values)
    if w <= 0 or h <= 0:
        fail(f"{item.get('name')} has non-positive rect")
    return x, y, w, h


def intersects(a: dict[str, object], b: dict[str, object], pad: float = 0) -> bool:
    ax, ay, aw, ah = rect(a)
    bx, by, bw, bh = rect(b)
    return ax - pad < bx + bw and ax + aw + pad > bx and ay - pad < by + bh and ay + ah + pad > by


def contains(parent: dict[str, object], child: dict[str, object], pad: float = 0) -> bool:
    px, py, pw, ph = rect(parent)
    cx, cy, cw, ch = rect(child)
    return cx >= px + pad and cy >= py + pad and cx + cw <= px + pw - pad and cy + ch <= py + ph - pad


def center_delta(parent_rect: list[int], child: dict[str, object]) -> tuple[float, float]:
    px, py, pw, ph = (float(v) for v in parent_rect)
    cx, cy, cw, ch = rect(child)
    return (cx + cw * 0.5) - (px + pw * 0.5), (cy + ch * 0.5) - (py + ph * 0.5)


def find(items: list[dict[str, object]], name: str) -> dict[str, object]:
    for item in items:
        if item.get("name") == name:
            return item
    fail(f"Missing layout item {name}")


def main() -> None:
    data = json.loads(LAYOUT.read_text(encoding="utf-8"))
    roots = data.get("assetPolicy", {}).get("approvedRoots")
    if roots != [EXPECTED_ROOT]:
        fail(f"approvedRoots must be exactly [{EXPECTED_ROOT!r}], got {roots!r}")

    images = data.get("images")
    texts = data.get("texts")
    if not isinstance(images, list) or not isinstance(texts, list):
        fail("images/texts arrays are required")

    approved_root = PROJECT / EXPECTED_ROOT
    seen_names: set[str] = set()
    seen_files: set[str] = set()

    for item in images:
        name = str(item.get("name", ""))
        file_name = str(item.get("file", ""))
        fit = str(item.get("fit", ""))
        z = item.get("z")

        if not name or name in seen_names:
            fail(f"image name missing or duplicated: {name!r}")
        seen_names.add(name)

        if any(token in file_name for token in FORBIDDEN):
            fail(f"{name} uses forbidden source token in {file_name}")

        path = approved_root / file_name
        if not path.exists():
            fail(f"{name} missing asset {path}")

        if not isinstance(z, int):
            fail(f"{name} missing integer z")

        if fit not in {"stretch", "contain", "cover"}:
            fail(f"{name} has unsupported fit {fit!r}")

        if any(token in name for token in ("Icon", "Gear", "Chevrons")) and fit != "contain":
            fail(f"{name} must use contain fit to preserve aspect")

        if "Art_" in name or name == "CommanderProfilePortrait":
            if fit != "cover":
                fail(f"{name} must use cover fit so content crops inside its frame")

        with Image.open(path) as image:
            width, height = image.size
        if width <= 0 or height <= 0:
            fail(f"{name} asset has invalid size")

        seen_files.add(file_name)
        rect(item)

    for item in texts:
        name = str(item.get("name", ""))
        if not name:
            fail("text item missing name")
        rect(item)
        if not isinstance(item.get("z"), int):
            fail(f"{name} missing integer z")

    for card in ("Saga", "Operation", "QuickCustom"):
        frame = find(images, f"ModeCardFrame_{card}")
        art = find(images, f"ModeCardArt_{card}")
        if int(art["z"]) >= int(frame["z"]):
            fail(f"ModeCardArt_{card} must render below ModeCardFrame_{card}")
        if not intersects(frame, art):
            fail(f"ModeCardArt_{card} is not inside/under its frame")

    profile_frame = find(images, "CommanderProfilePanelFrame")
    portrait = find(images, "CommanderProfilePortrait")
    if int(portrait["z"]) >= int(profile_frame["z"]):
        fail("CommanderProfilePortrait must render below CommanderProfilePanelFrame")
    if not intersects(profile_frame, portrait):
        fail("CommanderProfilePortrait is not inside/under its frame")
    if not contains(profile_frame, portrait, pad=40):
        fail("CommanderProfilePortrait must stay inside the profile panel safe area")

    settings_slot = [3612, 49, 176, 174]
    settings_icon = find(images, "SettingsGearIcon")
    dx, dy = center_delta(settings_slot, settings_icon)
    if abs(dx) > 2 or abs(dy) > 2:
        fail(f"SettingsGearIcon is not centered in settings slot: delta=({dx:.1f},{dy:.1f})")

    deploy_text = find(texts, "DeployCommandLabel")
    deploy_text_slot = [2594, 1786, 1040, 260]
    dx, dy = center_delta(deploy_text_slot, deploy_text)
    if abs(dx) > 24 or abs(dy) > 10:
        fail(f"DeployCommandLabel is not centered in deploy frame: delta=({dx:.1f},{dy:.1f})")

    top_slots = {
        "CreditsIcon": [1050, 44, 220, 150],
        "MaterialsIcon": [1842, 44, 220, 150],
        "AuthorityIcon": [2660, 44, 240, 150],
    }
    for icon_name, slot in top_slots.items():
        icon = find(images, icon_name)
        _, _, _, h = rect(icon)
        if h > 112:
            fail(f"{icon_name} is too tall for top bar safe area")
        dx, dy = center_delta(slot, icon)
        if abs(dy) > 8:
            fail(f"{icon_name} is vertically off-center in top bar slot: delta={dy:.1f}")

    for card in ("Saga", "Operation", "QuickCustom"):
        frame = find(images, f"ModeCardFrame_{card}")
        art = find(images, f"ModeCardArt_{card}")
        if not contains(frame, art, pad=48):
            fail(f"ModeCardArt_{card} must stay inside the frame safe area")

    for row_name, meter_name in (
        ("OperationWarningRowPressure", "OperationPressureMeter"),
        ("OperationWarningRowRisk", "OperationRiskMeter"),
    ):
        row = find(images, row_name)
        meter = find(images, meter_name)
        if int(meter["z"]) <= int(row["z"]):
            fail(f"{meter_name} must render above {row_name}")

    nav_rows = [item for item in images if str(item.get("name", "")).startswith("LeftNavRow_")]
    nav_rows.sort(key=lambda item: rect(item)[1])
    for first, second in zip(nav_rows, nav_rows[1:]):
        _, y, _, h = rect(first)
        _, next_y, _, _ = rect(second)
        if next_y - (y + h) < 12:
            fail(f"{first['name']} and {second['name']} are too close")

    print(
        f"SCN02_LAYOUT_VALID images={len(images)} texts={len(texts)} uniqueFiles={len(seen_files)} root={EXPECTED_ROOT}"
    )


if __name__ == "__main__":
    try:
        main()
    except SystemExit:
        raise
    except Exception as exc:  # noqa: BLE001 - validation script should report any parse/runtime issue.
        fail(str(exc))

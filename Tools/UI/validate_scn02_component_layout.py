#!/usr/bin/env python3
"""Validate SCN-02 component layout constraints and emit a diagnostic overlay."""

from __future__ import annotations

import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw


PROJECT = Path(__file__).resolve().parents[2]
LAYOUT = PROJECT / "Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_menu_layout.json"
CAPTURE = PROJECT / "Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_3840x2160.png"
OVERLAY = PROJECT / "Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_Diagnostics.png"


def rect_edges(rect: list[int]) -> tuple[int, int, int, int]:
    x, y, w, h = rect
    return x, y, x + w, y + h


def intersects(a: list[int], b: list[int]) -> bool:
    ax1, ay1, ax2, ay2 = rect_edges(a)
    bx1, by1, bx2, by2 = rect_edges(b)
    return ax1 < bx2 and ax2 > bx1 and ay1 < by2 and ay2 > by1


def inside(inner: list[int], outer: list[int], padding: int = 0) -> bool:
    ix1, iy1, ix2, iy2 = rect_edges(inner)
    ox1, oy1, ox2, oy2 = rect_edges(outer)
    return ix1 >= ox1 + padding and iy1 >= oy1 + padding and ix2 <= ox2 - padding and iy2 <= oy2 - padding


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


def main() -> int:
    data = json.loads(LAYOUT.read_text(encoding="utf-8"))
    canvas_w, canvas_h = data.get("canvas", [3840, 2160])
    policy = data.get("layoutPolicy", {})
    shell_inset = int(policy.get("shellSafeInset", 72))
    text_padding = int(policy.get("minimumTextPadding", 18))
    child_padding = int(policy.get("minimumChildPadding", 0))
    minimum_content_gap = int(policy.get("minimumContentGap", 0))
    font_size_floor = int(policy.get("fontSizeFloor", 0))
    safe = [shell_inset, shell_inset, canvas_w - shell_inset * 2, canvas_h - shell_inset * 2]

    images = data.get("images", [])
    panels = [img for img in images if img.get("role") == "functional-panel"]
    panel_by_name = {img["name"]: img for img in panels}
    failures: list[str] = []

    for panel in panels:
        if not inside(panel["rect"], safe, 0):
            failures.append(f"shell-safe-area violation: {panel['name']} rect={panel['rect']} safe={safe}")

    for index, left in enumerate(panels):
        for right in panels[index + 1 :]:
            if intersects(left["rect"], right["rect"]):
                failures.append(f"panel overlap: {left['name']} {left['rect']} intersects {right['name']} {right['rect']}")

    def panel_safe_rect(panel: dict) -> list[int]:
        return panel.get("safeRect") or panel["rect"]

    for image in images:
        container_name = image.get("container")
        if not container_name:
            continue

        container = panel_by_name.get(container_name)
        if container is None:
            failures.append(f"image container missing: {image.get('name')} container={container_name}")
            continue

        if not inside(image["rect"], panel_safe_rect(container), child_padding):
            failures.append(
                f"image safe-rect violation: {image['name']} rect={image['rect']} "
                f"container={container_name} safe={panel_safe_rect(container)} padding={child_padding}"
            )

    for text in data.get("texts", []):
        container_name = text.get("container")
        if int(text.get("fontSize", 0)) < font_size_floor:
            failures.append(
                f"text font floor violation: {text.get('name')} fontSize={text.get('fontSize')} "
                f"floor={font_size_floor}"
            )
        if not container_name:
            failures.append(f"text missing container: {text.get('name')}")
            continue

        container = panel_by_name.get(container_name)
        if container is None:
            failures.append(f"text container missing: {text.get('name')} container={container_name}")
            continue

        if not inside(text["rect"], panel_safe_rect(container), text_padding):
            failures.append(
                f"text safe-rect violation: {text['name']} rect={text['rect']} "
                f"container={container_name} safe={panel_safe_rect(container)} padding={text_padding}"
            )

    content_by_container: dict[str, list[dict]] = {}
    for image in images:
        container = image.get("container")
        if container and image.get("slotKind") not in {"frame", "art", "background"}:
            content_by_container.setdefault(container, []).append(image)
    for text in data.get("texts", []):
        container = text.get("container")
        if container:
            content_by_container.setdefault(container, []).append(text)

    for container_name, children in content_by_container.items():
        for index, left in enumerate(children):
            for right in children[index + 1:]:
                if intersects(left["rect"], right["rect"]):
                    failures.append(
                        f"content overlap: {container_name} {left['name']} {left['rect']} "
                        f"intersects {right['name']} {right['rect']}"
                    )
                elif gap(left["rect"], right["rect"]) < minimum_content_gap:
                    failures.append(
                        f"content gap violation: {container_name} {left['name']} near {right['name']} "
                        f"gap<{minimum_content_gap}"
                    )

    if CAPTURE.exists():
        base = Image.open(CAPTURE).convert("RGBA")
    else:
        base = Image.new("RGBA", (canvas_w, canvas_h), (0, 0, 0, 255))

    overlay = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    sx1, sy1, sx2, sy2 = rect_edges(safe)
    draw.rectangle((sx1, sy1, sx2, sy2), outline=(255, 220, 0, 255), width=5)
    for panel in panels:
        x1, y1, x2, y2 = rect_edges(panel["rect"])
        draw.rectangle((x1, y1, x2, y2), outline=(0, 255, 180, 255), width=4)
        if panel.get("safeRect"):
            x1, y1, x2, y2 = rect_edges(panel["safeRect"])
            draw.rectangle((x1, y1, x2, y2), outline=(255, 180, 80, 230), width=3)
    for image in images:
        if image.get("container") and image.get("slotKind") not in {"frame", "art", "background"}:
            x1, y1, x2, y2 = rect_edges(image["rect"])
            draw.rectangle((x1, y1, x2, y2), outline=(180, 255, 180, 230), width=2)
    for text in data.get("texts", []):
        x1, y1, x2, y2 = rect_edges(text["rect"])
        draw.rectangle((x1, y1, x2, y2), outline=(120, 180, 255, 230), width=2)
    if failures:
        draw.rectangle((20, 20, canvas_w - 20, 90 + min(len(failures), 8) * 34), fill=(120, 0, 0, 210))
        for i, failure in enumerate(failures[:8]):
            draw.text((40, 42 + i * 34), failure[:190], fill=(255, 255, 255, 255))

    diagnostic = Image.alpha_composite(base, overlay)
    OVERLAY.parent.mkdir(parents=True, exist_ok=True)
    diagnostic.save(OVERLAY)

    if failures:
        print("SCN02_COMPONENT_LAYOUT_INVALID")
        for failure in failures:
            print(failure)
        print(f"diagnostic={OVERLAY}")
        return 1

    print(f"SCN02_COMPONENT_LAYOUT_VALID panels={len(panels)} texts={len(data.get('texts', []))} diagnostic={OVERLAY}")
    return 0


if __name__ == "__main__":
    sys.exit(main())

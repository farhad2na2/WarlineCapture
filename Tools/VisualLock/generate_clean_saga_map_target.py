#!/usr/bin/env python3
"""Generate the SCN-05 Saga Map visual-lock target.

This file intentionally does not redraw or reinterpret the Saga Map UI. The
source mockup already defines the exact silhouettes, borders, shadows, route
curves, icon styling, and positions. The output preserves those original
proportions and upscales the playable screen area into the visual-lock canvas.
"""

from __future__ import annotations

from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageEnhance, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
REF = ROOT / "Design" / "UIUX_Codex_Package" / "uiux_spec_assets" / "SCN-05_saga_map.jpg"
OUT_DIR = ROOT / "Design" / "VisualLock" / "SCN-05_SagaMap"
OUT_PATH = OUT_DIR / "SCN-05_SagaMap_Landscape_Target.png"
NOTES_PATH = OUT_DIR / "SCN-05_SagaMap_CleanLandscape_Notes.md"

CANVAS_W, CANVAS_H = 1672, 941
SOURCE_SCREEN = (0, 0, 1000, 624)


def upscale_screen() -> Image.Image:
    source = Image.open(REF).convert("RGB").crop(SOURCE_SCREEN)

    # Preserve the original aspect ratio. Stretching this source to 1672x941
    # changes button silhouettes, border angles, and route geometry.
    scale = min(CANVAS_W / source.width, CANVAS_H / source.height)
    target_w = round(source.width * scale)
    target_h = round(source.height * scale)

    cv_source = cv2.cvtColor(np.array(source), cv2.COLOR_RGB2BGR)
    cv_scaled = cv2.resize(cv_source, (target_w, target_h), interpolation=cv2.INTER_LANCZOS4)
    cv_scaled = cv2.fastNlMeansDenoisingColored(cv_scaled, None, 2, 2, 7, 21)
    scaled = Image.fromarray(cv2.cvtColor(cv_scaled, cv2.COLOR_BGR2RGB))

    # Mild sharpening only. This keeps the original icon and border shapes,
    # unlike vector redrawing which changed the design language.
    sharp = scaled.filter(ImageFilter.UnsharpMask(radius=1.3, percent=95, threshold=3))
    sharp = ImageEnhance.Contrast(sharp).enhance(1.04)

    canvas = Image.new("RGB", (CANVAS_W, CANVAS_H), (2, 9, 11))
    x = (CANVAS_W - target_w) // 2
    y = (CANVAS_H - target_h) // 2
    canvas.paste(sharp, (x, y))
    return canvas


def write_notes() -> None:
    NOTES_PATH.write_text(
        "\n".join(
            [
                "# SCN-05 Saga Map Visual Target",
                "",
                "- Canvas: 1672 x 941.",
                "- Source: `Design/UIUX_Codex_Package/uiux_spec_assets/SCN-05_saga_map.jpg`.",
                "- The playable screen area is upscaled with original aspect ratio preserved.",
                "- No UI components are redrawn or redesigned in this target.",
                "- Use this target for exact layout, silhouettes, shadows, borders, back icon, route curves, and spacing.",
                "- Background art can be replaced later, but UI geometry should match this reference.",
                "",
                f"Canonical target: `{OUT_PATH.relative_to(ROOT)}`",
            ]
        )
        + "\n",
        encoding="utf-8",
    )


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    upscale_screen().save(OUT_PATH)
    write_notes()
    print(OUT_PATH)
    print(NOTES_PATH)


if __name__ == "__main__":
    main()

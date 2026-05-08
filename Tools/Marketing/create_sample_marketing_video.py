#!/usr/bin/env python3
"""Build a local WarlineCapture marketing-video sample from approved design images.

The script intentionally uses only checked-in design references. External AI video
shots can replace the source paths later without changing the verification flow.
"""

from __future__ import annotations

import argparse
import json
import math
from dataclasses import dataclass
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "Design" / "Marketing" / "SampleVideo"
VIDEO_PATH = OUT_DIR / "WarlineCapture_Sample_Marketing_Video.mp4"
PREVIEW_PATH = OUT_DIR / "WarlineCapture_Sample_Marketing_Video_Preview.png"
MANIFEST_PATH = OUT_DIR / "WarlineCapture_Sample_Marketing_Video_Manifest.json"
QA_PATH = OUT_DIR / "WarlineCapture_Sample_Marketing_Video_QA.md"

WIDTH = 1920
HEIGHT = 1080
FPS = 24
BANNED_TERMS = ("Token", "Command Tokens", "Intel Keys", "loot box", "pay to win")


@dataclass(frozen=True)
class Scene:
    name: str
    source: str
    duration: float
    eyebrow: str
    title: str
    body: str
    cta: str | None = None


SCENES = (
    Scene(
        name="City Command",
        source="Design/VisualReferences/2DIsometricProduction/ISO-01_CityCommand_Target/ISO-01_CityCommand_ProductionTarget.png",
        duration=4.0,
        eyebrow="PROJECT CITY",
        title="Command the city under pressure",
        body="Build, deploy, and stabilize districts through tactical RTS decisions.",
    ),
    Scene(
        name="Battle HUD",
        source="Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png",
        duration=4.0,
        eyebrow="REAL-TIME COMMAND",
        title="Move squads. Hold lines. Control objectives.",
        body="Every button maps to battlefield intent: move, attack, build, stop, hold, special.",
    ),
    Scene(
        name="Operation Dashboard",
        source="Design/VisualLock/SCN-11_OperationDashboard/SCN-11_OperationDashboard_Landscape_Target.png",
        duration=4.0,
        eyebrow="PERSISTENT OPERATION",
        title="Recover districts between missions",
        body="Operation supplies become trust, security, intel, and infrastructure through authored actions.",
    ),
    Scene(
        name="Commander Store",
        source="generated:fair_store_panel",
        duration=4.0,
        eyebrow="FAIR MONETIZATION",
        title="Starter packs and cosmetics stay economy-safe",
        body="Purchases grant designed resources and unlocks; mission stars and district metrics are earned.",
    ),
    Scene(
        name="Mission Result",
        source="Design/VisualLock/POP-05_MissionResult/POP-05_MissionResult_Landscape_Target.png",
        duration=4.0,
        eyebrow="PLAYER SKILL FIRST",
        title="Win through objectives, not purchases",
        body="Rewards, progression, and store grants follow the economy lifecycle spec.",
        cta="Wishlist build sample",
    ),
)


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf" if bold else "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/SFNS.ttf",
    ]
    for candidate in candidates:
        path = Path(candidate)
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


FONT_EYEBROW = font(34, True)
FONT_TITLE = font(74, True)
FONT_BODY = font(36)
FONT_CTA = font(34, True)


def ease_in_out(t: float) -> float:
    return 0.5 - 0.5 * math.cos(math.pi * max(0.0, min(1.0, t)))


def cover_crop(image: Image.Image, width: int, height: int, zoom: float, pan_x: float, pan_y: float) -> Image.Image:
    src_w, src_h = image.size
    target_ratio = width / height
    src_ratio = src_w / src_h

    if src_ratio > target_ratio:
        crop_h = src_h / zoom
        crop_w = crop_h * target_ratio
    else:
        crop_w = src_w / zoom
        crop_h = crop_w / target_ratio

    max_x = max(0.0, src_w - crop_w)
    max_y = max(0.0, src_h - crop_h)
    left = max_x * pan_x
    top = max_y * pan_y
    box = (int(left), int(top), int(left + crop_w), int(top + crop_h))
    return image.crop(box).resize((width, height), Image.Resampling.LANCZOS)


def draw_overlay(frame: Image.Image, scene: Scene, local_t: float) -> Image.Image:
    overlay = Image.new("RGBA", frame.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)

    fade = int(205 * min(1.0, local_t / 0.7, (1.0 - local_t) / 0.45))
    panel = Image.new("RGBA", frame.size, (0, 0, 0, 0))
    panel_draw = ImageDraw.Draw(panel)
    panel_draw.rectangle((0, 0, WIDTH, HEIGHT), fill=(6, 10, 13, 70))
    panel_draw.rectangle((0, 0, 840, HEIGHT), fill=(7, 12, 15, fade))
    frame = Image.alpha_composite(frame.convert("RGBA"), panel)

    x = 96
    y = 150
    accent = (234, 169, 67, 255)
    text = (238, 242, 235, 255)
    muted = (195, 206, 198, 255)

    draw.rounded_rectangle((x, y, x + 320, y + 56), radius=4, fill=(21, 48, 47, 235), outline=accent, width=2)
    draw.text((x + 24, y + 12), scene.eyebrow, font=FONT_EYEBROW, fill=text)
    y += 96

    for line in wrap_text(draw, scene.title, FONT_TITLE, 700):
        draw.text((x, y), line, font=FONT_TITLE, fill=text)
        y += 82

    y += 24
    for line in wrap_text(draw, scene.body, FONT_BODY, 680):
        draw.text((x, y), line, font=FONT_BODY, fill=muted)
        y += 48

    if scene.cta:
        y = HEIGHT - 190
        draw.rounded_rectangle((x, y, x + 440, y + 72), radius=6, fill=(191, 75, 58, 245))
        draw.text((x + 28, y + 17), scene.cta, font=FONT_CTA, fill=(255, 251, 240, 255))

    frame = Image.alpha_composite(frame, overlay)
    return frame.convert("RGB")


def wrap_text(draw: ImageDraw.ImageDraw, text: str, text_font: ImageFont.ImageFont, max_width: int) -> list[str]:
    words = text.split()
    lines: list[str] = []
    current: list[str] = []
    for word in words:
        candidate = " ".join(current + [word])
        if draw.textbbox((0, 0), candidate, font=text_font)[2] <= max_width:
            current.append(word)
            continue
        if current:
            lines.append(" ".join(current))
        current = [word]
    if current:
        lines.append(" ".join(current))
    return lines


def make_fair_store_panel() -> Image.Image:
    image = Image.new("RGB", (WIDTH, HEIGHT), (8, 13, 15))
    draw = ImageDraw.Draw(image)
    grid = (20, 63, 68)
    for x in range(0, WIDTH, 96):
        draw.line((x, 0, x, HEIGHT), fill=grid, width=1)
    for y in range(0, HEIGHT, 96):
        draw.line((0, y, WIDTH, y), fill=grid, width=1)

    draw.rectangle((0, 0, WIDTH, 110), fill=(6, 12, 15))
    draw.text((78, 36), "COMMAND EXCHANGE", font=font(42, True), fill=(234, 242, 235))
    draw.text((1480, 38), "CREDITS 24.8K   MATERIALS 12.9K   FUEL 1,250   INTEL 88", font=font(24, True), fill=(184, 202, 195))

    def card(x: int, y: int, w: int, h: int, title: str, price: str, items: list[str], accent: tuple[int, int, int]) -> None:
        draw.rounded_rectangle((x, y, x + w, y + h), radius=8, fill=(14, 28, 31), outline=accent, width=3)
        draw.rectangle((x, y, x + w, y + 74), fill=(18, 43, 45))
        draw.text((x + 28, y + 20), title, font=font(30, True), fill=(240, 246, 238))
        draw.rounded_rectangle((x + w - 160, y + 18, x + w - 26, y + 56), radius=5, fill=(205, 150, 52))
        draw.text((x + w - 127, y + 25), price, font=font(21, True), fill=(7, 11, 12))
        draw.polygon(
            [
                (x + 64, y + 155),
                (x + 150, y + 108),
                (x + 236, y + 155),
                (x + 236, y + 252),
                (x + 150, y + 304),
                (x + 64, y + 252),
            ],
            fill=(51, 69, 60),
            outline=accent,
        )
        draw.line((x + 78, y + 166, x + 222, y + 248), fill=accent, width=8)
        draw.line((x + 222, y + 166, x + 78, y + 248), fill=accent, width=8)
        item_x = x + 315
        item_y = y + 116
        for item in items:
            draw.rounded_rectangle((item_x, item_y, x + w - 34, item_y + 54), radius=5, fill=(22, 44, 48), outline=(47, 134, 139), width=1)
            draw.ellipse((item_x + 18, item_y + 15, item_x + 42, item_y + 39), fill=accent)
            draw.text((item_x + 60, item_y + 13), item, font=font(25, True), fill=(230, 238, 232))
            item_y += 68

    card(
        96,
        176,
        780,
        430,
        "RECON STARTER PACK",
        "$4.99",
        ["2,500 Credits", "300 Materials", "120 Intel", "Ranger Unlock"],
        (68, 202, 216),
    )
    card(
        1000,
        176,
        780,
        430,
        "BASE BUILDER PACK",
        "$9.99",
        ["6,000 Credits", "900 Materials", "400 Fuel", "Builder Skin"],
        (222, 169, 65),
    )
    card(
        96,
        650,
        780,
        320,
        "NIGHT OPS COSMETIC SET",
        "$6.99",
        ["Unit Skin", "Banner Frame", "Profile Badge"],
        (90, 148, 229),
    )
    card(
        1000,
        650,
        780,
        320,
        "OPERATION SUPPLY DROP",
        "$3.99",
        ["Operation Supply", "Aid Convoy", "No direct district metrics"],
        (191, 75, 58),
    )
    return image


def frame_stats(frame: np.ndarray) -> dict[str, float]:
    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    return {
        "mean": float(np.mean(gray)),
        "stddev": float(np.std(gray)),
    }


def save_preview_contact_sheet(frames: list[tuple[str, Image.Image]]) -> None:
    tile_w = 384
    tile_h = 216
    label_h = 58
    sheet = Image.new("RGB", (tile_w * len(frames), tile_h + label_h), (10, 14, 16))
    draw = ImageDraw.Draw(sheet)
    label_font = font(22, True)
    for idx, (label, frame) in enumerate(frames):
        x = idx * tile_w
        thumb = frame.resize((tile_w, tile_h), Image.Resampling.LANCZOS)
        sheet.paste(thumb, (x, 0))
        draw.rectangle((x, tile_h, x + tile_w, tile_h + label_h), fill=(10, 14, 16))
        draw.text((x + 18, tile_h + 16), label, font=label_font, fill=(238, 242, 235))
    PREVIEW_PATH.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(PREVIEW_PATH)


def build_video() -> dict:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    source_images = {}
    missing = []
    for scene in SCENES:
        if scene.source == "generated:fair_store_panel":
            source_images[scene.name] = make_fair_store_panel()
            continue
        source_path = ROOT / scene.source
        if not source_path.exists():
            missing.append(scene.source)
            continue
        source_images[scene.name] = Image.open(source_path).convert("RGB")

    if missing:
        raise FileNotFoundError("Missing source images: " + ", ".join(missing))

    writer = cv2.VideoWriter(
        str(VIDEO_PATH),
        cv2.VideoWriter_fourcc(*"mp4v"),
        FPS,
        (WIDTH, HEIGHT),
    )
    if not writer.isOpened():
        raise RuntimeError("OpenCV could not open the MP4 writer.")

    stats = []
    preview_frames: list[tuple[str, Image.Image]] = []
    total_frames = 0
    last_arr: np.ndarray | None = None
    for scene_index, scene in enumerate(SCENES):
        frames = int(scene.duration * FPS)
        image = source_images[scene.name]
        for i in range(frames):
            local_t = i / max(1, frames - 1)
            motion = ease_in_out(local_t)
            zoom = 1.02 + 0.05 * motion
            pan_x = 0.18 + 0.10 * motion if scene_index % 2 == 0 else 0.28 - 0.10 * motion
            pan_y = 0.45
            frame = cover_crop(image, WIDTH, HEIGHT, zoom, pan_x, pan_y)
            frame = draw_overlay(frame, scene, local_t)

            arr = cv2.cvtColor(np.array(frame), cv2.COLOR_RGB2BGR)
            if i < FPS // 2 and scene_index > 0 and last_arr is not None:
                alpha = (i + 1) / ((FPS // 2) + 1)
                arr = cv2.addWeighted(last_arr, 1.0 - alpha, arr, alpha, 0)

            writer.write(arr)
            last_arr = arr
            total_frames += 1
            if i in (0, frames // 2, frames - 1):
                stats.append({"scene": scene.name, "frame": total_frames, **frame_stats(arr)})
            if i == frames // 2:
                preview = Image.fromarray(cv2.cvtColor(arr, cv2.COLOR_BGR2RGB))
                preview_frames.append((scene.name, preview))
    writer.release()
    save_preview_contact_sheet(preview_frames)

    manifest = {
        "video": str(VIDEO_PATH.relative_to(ROOT)),
        "preview": str(PREVIEW_PATH.relative_to(ROOT)),
        "resolution": {"width": WIDTH, "height": HEIGHT},
        "fps": FPS,
        "durationSeconds": round(total_frames / FPS, 2),
        "frameCount": total_frames,
        "sourceDocs": [
            "Design/WarlineCapture_Economy_Reward_Design.md",
            "Design/Monetization/WarlineCapture_Monetization_Strategy.md",
            "Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md",
        ],
        "sourceImages": [scene.source for scene in SCENES],
        "scenes": [scene.__dict__ for scene in SCENES],
        "verification": {
            "fileExists": VIDEO_PATH.exists(),
            "fileSizeBytes": VIDEO_PATH.stat().st_size if VIDEO_PATH.exists() else 0,
            "blockedEconomyTermCount": len(BANNED_TERMS),
            "blankFrameSamples": [
                sample for sample in stats if sample["mean"] < 8 or sample["stddev"] < 4
            ],
            "bannedTermsFound": find_banned_terms(),
        },
        "frameSamples": stats,
    }
    MANIFEST_PATH.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    QA_PATH.write_text(render_qa(manifest), encoding="utf-8")
    return manifest


def find_banned_terms() -> list[str]:
    text = "\n".join(
        [
            *[scene.eyebrow for scene in SCENES],
            *[scene.title for scene in SCENES],
            *[scene.body for scene in SCENES],
            *[scene.cta or "" for scene in SCENES],
            *[scene.source for scene in SCENES],
        ]
    )
    found = []
    for term in BANNED_TERMS:
        if term.lower() in text.lower():
            found.append(term)
    return found


def render_qa(manifest: dict) -> str:
    checks = [
        ("MP4 exists", manifest["verification"]["fileExists"]),
        ("File is non-empty", manifest["verification"]["fileSizeBytes"] > 100_000),
        ("Preview contact sheet exists", PREVIEW_PATH.exists()),
        ("Resolution is 1920x1080", manifest["resolution"] == {"width": WIDTH, "height": HEIGHT}),
        ("Duration is 20 seconds", manifest["durationSeconds"] == 20.0),
        ("No blank sampled frames", not manifest["verification"]["blankFrameSamples"]),
        ("No banned economy/monetization terms", not manifest["verification"]["bannedTermsFound"]),
    ]
    lines = [
        "# WarlineCapture Sample Marketing Video QA",
        "",
        f"- Video: `{manifest['video']}`",
        f"- Preview: `{manifest['preview']}`",
        f"- Manifest: `{MANIFEST_PATH.relative_to(ROOT)}`",
        f"- Runtime: {manifest['durationSeconds']}s at {manifest['fps']} fps",
        f"- Size: {manifest['resolution']['width']}x{manifest['resolution']['height']}",
        f"- File size: {manifest['verification']['fileSizeBytes']} bytes",
        "",
        "## Checks",
        "",
    ]
    for label, passed in checks:
        lines.append(f"- [{'x' if passed else ' '}] {label}")
    lines.extend(["", "## Scenes", ""])
    for scene in manifest["scenes"]:
        lines.append(f"- {scene['name']}: `{scene['source']}`")
    lines.extend(
        [
            "",
            "## Next AI Swap-In Points",
            "",
            "- Replace one or more `sourceImages` with generated Firefly/Sora/Luma shots.",
            "- Keep the same scene names, copy, banned-term checks, duration, and economy-safe claims.",
            "- Re-run this script before human validation.",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--print-manifest", action="store_true")
    args = parser.parse_args()
    manifest = build_video()
    if args.print_manifest:
        print(json.dumps(manifest, indent=2))
    else:
        print(f"Wrote {VIDEO_PATH}")
        print(f"Wrote {MANIFEST_PATH}")
        print(f"Wrote {QA_PATH}")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Export web-sized WebP images for the static site from in-repo game art.

Run from the repository root:
    python3 Website/export_assets.py
"""

from pathlib import Path
import shutil

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Website" / "assets"

COMICS = "Assets/Game/Resources/FutureMissionComics"
NARRATIVE = "Assets/Game/Art/Narrative"
FIRST_LAUNCH = "Design/NarrativeVision/FirstLaunch/ArtReview/FinalArt/Previews"
PORTRAITS = "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits"

# name: (source, crop box in source pixels or None, output widths)
IMAGES = {
    "hero-last-corridor": (f"{COMICS}/CH05M04_LastCorridor.png", None, (960, 1672)),
    "prologue-market": (f"{FIRST_LAUNCH}/FL-P01_16x9_R1.png", None, (960, 1600)),
    "prologue-console": (f"{FIRST_LAUNCH}/FL-P05_16x9_R1.png", None, (960, 1600)),
    "prologue-aria": (f"{COMICS}/Bookends/CH03_Open.png", None, (960, 1600)),
    "ch1-airlift": (f"{NARRATIVE}/M04Airlift/Final/M04-C01.png", None, (720, 1200)),
    "ch2-gridlock": (f"{NARRATIVE}/CH02M01Gridlock/Final/CH02-C01.png", None, (720, 1200)),
    "ch3-network-break": (f"{COMICS}/CH03M05_NetworkBreak.png", None, (720, 1200)),
    "ch4-air-corridor": (f"{COMICS}/CH04M01_AirCorridor.png", None, (720, 1200)),
    "ch5-command-node": (f"{COMICS}/CH05M05_CommandNode.png", None, (720, 1200)),
    "mode-campaign": (f"{COMICS}/CH04M05_ArmorBreak.png", None, (720, 1200)),
    "mode-operations": (f"{COMICS}/CH05M01_CitywideAlert_Opening.png", None, (720, 1200)),
    "mode-skirmish": (f"{COMICS}/CH04M03_SplitFront.png", None, (720, 1200)),
    "gameplay-hud": ("Design/AgentReports/SupportSystem/Evidence/SmokeJourney/20260928-174342-en-strike-player/strike-aircraft.png", None, (960, 1600)),
    "breach-evidence": (f"{NARRATIVE}/M05BreachAssault/Final/M05-C01.png", None, (960, 1600)),
    "epilogue": (f"{COMICS}/Bookends/Campaign_Epilogue.png", None, (960, 1672)),
    "aria": (f"{PORTRAITS}/portrait_aria_v3.png", None, (480, 820)),
    "cast-dalia": (f"{PORTRAITS}/portrait_dalia.png", (24, 24, 795, 795), (480,)),
    "cast-samira": (f"{PORTRAITS}/portrait_samira.png", (24, 24, 795, 795), (480,)),
    "cast-laila": (f"{NARRATIVE}/M04Airlift/Final/M04-C01.png", (820, 0, 1520, 700), (480,)),
    "cast-fadi": (f"{NARRATIVE}/CH02M01Gridlock/Final/CH02-C01.png", (330, 150, 750, 570), (480,)),
    "cast-qassem": ("Assets/Game/Art/UI/Portraits/Generated/Portrait_Qassem_CampaignComic.png", None, (480,)),
}


def export_image(name, source, crop, widths):
    image = Image.open(ROOT / source)
    if crop:
        image = image.crop(crop)
    image = image.convert("RGBA" if image.mode in ("RGBA", "LA", "P") else "RGB")
    for width in widths:
        width = min(width, image.width)
        height = round(image.height * width / image.width)
        resized = image.resize((width, height), Image.LANCZOS)
        target = OUT / f"{name}-{width}.webp"
        resized.save(target, "WEBP", quality=80, method=6)
        print(f"{target.relative_to(ROOT)} {width}x{height} {target.stat().st_size // 1024} KB")


def export_icons():
    logo = Image.open(ROOT / "Assets/Game/Textures/Logo.png").convert("RGBA")
    logo = logo.crop(logo.getbbox())
    side = max(logo.size)
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.paste(logo, ((side - logo.width) // 2, (side - logo.height) // 2))
    square.resize((256, 256), Image.LANCZOS).save(OUT / "logo-256.webp", "WEBP", quality=90)
    square.resize((64, 64), Image.LANCZOS).save(OUT / "favicon-64.png")
    touch = Image.new("RGB", (180, 180), (11, 17, 24))
    mark = square.resize((150, 150), Image.LANCZOS)
    touch.paste(mark, (15, 15), mark)
    touch.save(OUT / "apple-touch-icon.png")


def export_social_card():
    source = Image.open(ROOT / f"{COMICS}/CH05M04_LastCorridor.png").convert("RGB")
    target_ratio = 1200 / 630
    height = round(source.width / target_ratio)
    top = (source.height - height) // 2
    card = source.crop((0, top, source.width, top + height)).resize((1200, 630), Image.LANCZOS)
    card.save(OUT / "og-card.jpg", "JPEG", quality=82, optimize=True)


def copy_fonts():
    font_dir = ROOT / "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium"
    fonts_out = OUT / "fonts"
    fonts_out.mkdir(exist_ok=True)
    for name in ("Oxanium-Medium.ttf", "Oxanium-Bold.ttf", "OFL.txt"):
        shutil.copy2(font_dir / name, fonts_out / name)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, (source, crop, widths) in IMAGES.items():
        export_image(name, source, crop, widths)
    export_icons()
    export_social_card()
    copy_fonts()


if __name__ == "__main__":
    main()

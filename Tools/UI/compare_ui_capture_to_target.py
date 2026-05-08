#!/usr/bin/env python3
import argparse
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageStat


def open_rgb(path: Path) -> Image.Image:
    image = Image.open(path).convert("RGBA")
    background = Image.new("RGBA", image.size, (5, 8, 10, 255))
    background.alpha_composite(image)
    return background.convert("RGB")


def fit_to(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    if image.size == size:
        return image
    return image.resize(size, Image.Resampling.LANCZOS)


def mse(diff: Image.Image) -> float:
    stat = ImageStat.Stat(diff)
    values = stat.mean
    return sum(channel * channel for channel in values) / len(values)


def main() -> int:
    parser = argparse.ArgumentParser(description="Create a visual target/capture/diff montage.")
    parser.add_argument("--target", required=True)
    parser.add_argument("--capture", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--label", default="UI visual comparison")
    args = parser.parse_args()

    target_path = Path(args.target)
    capture_path = Path(args.capture)
    out_path = Path(args.out)

    target = open_rgb(target_path)
    capture = fit_to(open_rgb(capture_path), target.size)
    diff = ImageChops.difference(target, capture)
    amplified = ImageEnhance.Contrast(diff).enhance(4.0)
    score = mse(diff)

    width, height = target.size
    header = 58
    gutter = 16
    montage = Image.new("RGB", (width * 3 + gutter * 4, height + header + gutter * 2), (16, 20, 22))
    draw = ImageDraw.Draw(montage)
    draw.text((gutter, 18), f"{args.label} | mse={score:.2f} | target={target.size} capture={capture.size}", fill=(230, 238, 238))

    panels = [("TARGET", target), ("CAPTURE", capture), ("DIFF x4", amplified)]
    x = gutter
    for label, image in panels:
        draw.text((x, header - 22), label, fill=(150, 220, 235))
        montage.paste(image, (x, header + gutter))
        x += width + gutter

    out_path.parent.mkdir(parents=True, exist_ok=True)
    montage.save(out_path)
    print(f"{out_path} mse={score:.2f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

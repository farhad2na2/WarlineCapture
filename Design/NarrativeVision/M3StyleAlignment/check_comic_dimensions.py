#!/usr/bin/env python3
"""Fail when a campaign comic PNG does not use the approved panel canvas."""

from pathlib import Path
import struct
import sys


EXPECTED = (1672, 941)
REPO_ROOT = Path(__file__).resolve().parents[3]
ART_ROOT = REPO_ROOT / "Assets/Game/Art/Narrative"
UI_PARTS = ("FirstLaunch/Dialogue/", "FirstLaunch/Commander/")


def png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as source:
        header = source.read(24)
    if header[:8] != b"\x89PNG\r\n\x1a\n" or header[12:16] != b"IHDR":
        raise ValueError("missing PNG IHDR")
    return struct.unpack(">II", header[16:24])


def main() -> int:
    failures = []
    panels = 0
    for path in sorted(ART_ROOT.rglob("*.png")):
        relative = path.relative_to(ART_ROOT).as_posix()
        if any(relative.startswith(part) for part in UI_PARTS):
            continue
        panels += 1
        try:
            size = png_size(path)
        except (OSError, ValueError) as error:
            failures.append(f"{relative}: {error}")
            continue
        if size != EXPECTED:
            failures.append(f"{relative}: {size[0]}x{size[1]}")
    if failures:
        print(f"Comic canvas check failed: {len(failures)} of {panels} panels differ from {EXPECTED[0]}x{EXPECTED[1]}:")
        print("\n".join(failures))
        return 1
    print(f"Comic canvas check passed: {panels} panels at {EXPECTED[0]}x{EXPECTED[1]}")
    return 0


if __name__ == "__main__":
    sys.exit(main())

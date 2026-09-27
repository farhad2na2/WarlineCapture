#!/usr/bin/env python3
"""Validate the story-only mission catalog and its shipped PNG assets."""

import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
ASSETS = ROOT / "Assets/Game/Resources/FutureMissionComics"
EXPECTED = {(3, 5)} | {(chapter, number) for chapter in (4, 5) for number in range(1, 6)}
SIZE = (1672, 941)


def png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as image:
        header = image.read(24)
    assert header[:8] == b"\x89PNG\r\n\x1a\n", f"Invalid PNG: {path}"
    return struct.unpack(">II", header[16:24])


def main() -> None:
    source = json.loads((ASSETS / "catalog.json").read_text(encoding="utf-8"))
    missions = source["missions"]
    pairs = [(mission["chapter"], mission["number"]) for mission in missions]
    assert len(missions) == len(EXPECTED) == len(set(pairs)) == 11, pairs
    assert set(pairs) == EXPECTED, pairs
    assert len({mission["id"] for mission in missions}) == 11
    image_names = [name for mission in missions for name in
                   (mission["image"], mission["commsImage"], mission["debriefImage"])]
    assert len(image_names) == len(set(image_names)) == 33
    total_lines = 0
    for mission in missions:
        key = f"ch{mission['chapter']:02}.m{mission['number']:02}"
        assert key in mission["id"], mission["id"]
        assert all(mission[field].strip() for field in
                   ("titleEn", "titleFa", "summaryEn", "summaryFa",
                    "objectiveEn", "objectiveFa"))
        stages = [line["stage"] for line in mission["lines"]]
        assert stages.count("brief") >= 2 and "comms" in stages and "debrief" in stages
        assert stages == sorted(stages, key={"brief": 0, "comms": 1, "debrief": 2}.get)
        for line in mission["lines"]:
            assert line["speaker"].strip() and line["en"].strip() and line["fa"].strip()
            total_lines += 1
        for name in (mission["image"], mission["commsImage"], mission["debriefImage"]):
            image = ASSETS / f"{name}.png"
            assert image.exists(), image
            assert png_size(image) == SIZE, (image, png_size(image))
            assert image.with_suffix(".png.meta").exists(), image
    print(f"[FutureMissionComics] result=Passed missions={len(missions)} "
          f"lines={total_lines} images={len(image_names)} size={SIZE[0]}x{SIZE[1]}")


if __name__ == "__main__":
    main()

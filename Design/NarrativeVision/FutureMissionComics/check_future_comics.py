#!/usr/bin/env python3
"""Validate the story-only mission catalog and its shipped PNG assets."""

import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
ASSETS = ROOT / "Assets/Game/Resources/FutureMissionComics"
PLAYABLE_CONFIGS = ROOT / "Assets/Game/Configs/Narrative"
EXPECTED = {(3, 5)} | {(chapter, number) for chapter in (4, 5) for number in range(1, 6)}
BOOKENDS = {
    "seq.ch03.open.hidden_network",
    "seq.ch04.open.air_and_armor",
    "seq.ch05.open.citywide_command",
    *(f"seq.ch{chapter:02}.close.protocol_fragment_{chapter:02}" for chapter in range(1, 6)),
    "seq.campaign.epilogue.canonical",
    "seq.campaign.epilogue.trust_emphasis",
    "seq.campaign.epilogue.evidence_emphasis",
    "seq.campaign.epilogue.infrastructure_emphasis",
    "seq.campaign.postscript.recovery_watch",
}
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
    playable_debriefs = 0
    config_text = "\n".join(path.read_text(encoding="utf-8")
                            for path in PLAYABLE_CONFIGS.rglob("*.asset"))
    for chapter in (1, 2, 3):
        for number in range(1, 5 if chapter == 3 else 6):
            sequence_id = f"seq.ch{chapter:02}.m{number:02}.debrief"
            assert f"sequenceId: {sequence_id}\n" in config_text, sequence_id
            playable_debriefs += 1
    assert playable_debriefs + len(missions) == 25
    bookends = json.loads((ASSETS / "bookends.json").read_text(encoding="utf-8"))["sequences"]
    assert {sequence["id"] for sequence in bookends} == BOOKENDS
    assert len(bookends) == len(BOOKENDS) == 13
    bookend_images = set()
    bookend_lines = 0
    for sequence in bookends:
        assert sequence["pages"], sequence["id"]
        if ".epilogue." in sequence["id"] and sequence["id"] != "seq.campaign.epilogue.canonical":
            assert all(page.get("lowEn", "").strip() and page.get("lowFa", "").strip()
                       for page in sequence["pages"]), sequence["id"]
        for page in sequence["pages"]:
            assert all(page[field].strip() for field in ("speaker", "en", "fa", "image")), sequence["id"]
            image = ASSETS / f"{page['image']}.png"
            assert image.exists(), image
            assert png_size(image) == SIZE, (image, png_size(image))
            assert image.with_suffix(".png.meta").exists(), image
            bookend_images.add(page["image"])
            bookend_lines += 1
    print(f"[FutureMissionComics] result=Passed missions={len(missions)} "
          f"playableDebriefs={playable_debriefs} campaignDebriefs={playable_debriefs + len(missions)} "
          f"lines={total_lines} images={len(image_names)} "
          f"bookends={len(bookends)} bookendLines={bookend_lines} "
          f"bookendImages={len(bookend_images)} size={SIZE[0]}x{SIZE[1]}")


if __name__ == "__main__":
    main()

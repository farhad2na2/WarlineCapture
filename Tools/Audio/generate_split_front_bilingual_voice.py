#!/usr/bin/env python3
"""Generate CH04-M03's eight authored English and Persian story lines."""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
from pathlib import Path
import re

import generate_m02_bilingual_voice as audio
import persian_voice_profile

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "Assets/Game/Audio/Narrative/CH04M03SplitFront/split_front_voice_manifest.json"
REVIEW = ROOT / "Design/AgentReports/CH04M03SplitFront/voice_payload_review.json"
SOURCE = ROOT / "Assets/Game/Scripts/Configs/Narrative/CH04M03SplitFrontCopy.cs"
STRING = r'"((?:[^"\\]|\\.)*)"'


def canonical_lines():
    parsed = re.findall(
        r"new\(" + STRING + r",\s*NarrativeSpeakerId\.(\w+),\s*" + STRING + r",\s*" + STRING + r"\)",
        SOURCE.read_text(encoding="utf-8"), re.MULTILINE)
    if len(parsed) != 8:
        raise RuntimeError(f"Expected 8 canonical Split Front lines, got {len(parsed)}")
    def decode(value):
        return json.loads('"' + value + '"')
    return [("split_front-" + decode(identity), speaker.upper(), decode(english), decode(persian))
            for identity, speaker, english, persian in parsed]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--force", action="store_true")
    args = parser.parse_args()
    audio.VOICE_IDS["QASSEM"] = "JBFqnCBsd6RMkjVDRZzb"
    audio.VOICE_NAMES["QASSEM"] = "George - controlled, persuasive antagonist (Nadir Qassem)"
    entries = canonical_lines()
    REVIEW.parent.mkdir(parents=True, exist_ok=True)
    cast = sorted({entry[1] for entry in entries})
    REVIEW.write_text(json.dumps({
        "mission": "saga.ch04.m03.split_front", "provider": "ElevenLabs", "model": audio.MODEL,
        "cast": {speaker: audio.VOICE_IDS[speaker] for speaker in cast},
        "castDirection": {speaker: audio.VOICE_NAMES[speaker] for speaker in cast},
        "purpose": "Bilingual authored story clips; local runtime assets; no runtime network TTS",
        "entries": [list(entry) for entry in entries]
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    if args.dry_run:
        print(f"[SplitFrontVoiceCopy] result=Passed narrative={len(entries)} locales=2 characters={sum(len(e[2])+len(e[3]) for e in entries)}")
        return
    key = audio.read_api_key(ROOT / ".local/secrets/elevenlabs_api_key")
    subscription = audio.request_json(key, "/v1/user/subscription")
    if subscription.get("status") != "active" or subscription.get("tier") in {None, "free"}:
        raise RuntimeError("An existing active paid ElevenLabs subscription is required.")
    previous = json.loads(MANIFEST.read_text(encoding="utf-8")) if MANIFEST.exists() else {"clips": []}
    prior = {(clip["id"], clip["locale"]): clip for clip in previous["clips"]}
    records = []
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    for index, (identity, speaker, english, persian) in enumerate(entries):
        for language, locale, copy in (("en", "en-US", english), ("fa", "fa-IR", persian)):
            path = MANIFEST.parent / "Voice" / language / f"{identity}.wav"
            old = prior.get((identity, locale), {})
            if args.force or not persian_voice_profile.clip_matches(old, copy, path, language):
                audio.convert(audio.request_audio(key, audio.VOICE_IDS[speaker], copy, language,
                                                  9900 + index * 2 + (language == "fa")), path, speaker)
            clip = audio.record("narrative", identity, speaker, locale, copy, path)
            clip["captionSha256"] = hashlib.sha256(copy.encode("utf-8")).hexdigest()
            clip.update(persian_voice_profile.metadata(language))
            records.append(clip)
            print(f"[SplitFrontVoice] {identity} {locale} speaker={speaker} duration={clip['durationSeconds']:.2f}s", flush=True)
    MANIFEST.write_text(json.dumps({
        "schema": "WarlineCapture.SplitFrontBilingualVoice.v1",
        "missionId": "saga.ch04.m03.split_front",
        "generatedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "provider": "ElevenLabs", "license": audio.RIGHTS, "model": audio.MODEL,
        "runtimeNetworkTts": False,
        "subscription": {"tier": subscription.get("tier"), "status": subscription.get("status")},
        "processing": {"sampleRateHz": 44100, "channels": 1, "sourceEncoding": "PCM_S16LE", "loudnessLUFS": -18},
        "clips": records
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("[SplitFrontBilingualVoiceGeneration] result=Passed clips=16 narrative=8 locales=2 runtimeNetworkTts=0")


if __name__ == "__main__":
    main()

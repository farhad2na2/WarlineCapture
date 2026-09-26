#!/usr/bin/env python3
"""Generate final bilingual Market Lifeline story voices from canonical C# copy."""
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
MANIFEST = ROOT / "Assets/Game/Audio/Narrative/CH02M03MarketLifeline/market_lifeline_voice_manifest.json"
REVIEW = ROOT / "Design/AgentReports/CH02M03MarketLifeline/voice_payload_review.json"
STRING = r'"((?:[^"\\]|\\.)*)"'


def decode(value: str) -> str:
    return json.loads('"' + value + '"')


def canonical_lines() -> list[tuple[str, str, str, str]]:
    source = (ROOT / "Assets/Game/Scripts/Configs/Narrative/CH02M03MarketLifelineCopy.cs").read_text(encoding="utf-8")
    pattern = r'new\(' + STRING + r',\s*NarrativeSpeakerId\.(\w+),\s*' + STRING + r',\s*' + STRING + r'\)'
    parsed = re.findall(pattern, source, re.MULTILINE)
    if len(parsed) != 7:
        raise RuntimeError(f"Expected 7 canonical Market Lifeline lines, got {len(parsed)}")
    return [("market_lifeline-" + decode(identity), speaker.upper(), decode(english), decode(persian))
            for identity, speaker, english, persian in parsed]


def write_review(entries: list[tuple[str, str, str, str]]) -> None:
    REVIEW.parent.mkdir(parents=True, exist_ok=True)
    REVIEW.write_text(json.dumps({
        "mission": "saga.ch02.m03.market_lifeline",
        "provider": "ElevenLabs",
        "model": audio.MODEL,
        "cast": {
            "YASIN": "nPczCjzI2devNBz1zQrb",
            "SAMIRA": audio.VOICE_IDS["SAMIRA"],
            "ARIA": audio.VOICE_IDS["ARIA"],
        },
        "castDirection": {
            "YASIN": "Brian; calm, grounded, credible local market representative; distinct from Fadi",
            "SAMIRA": audio.VOICE_NAMES["SAMIRA"],
            "ARIA": audio.VOICE_NAMES["ARIA"],
        },
        "purpose": "Bilingual authored mission story clips; local runtime assets; no runtime network TTS",
        "entries": [list(entry) for entry in entries],
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--force", action="store_true")
    args = parser.parse_args()
    entries = canonical_lines()
    audio.VOICE_IDS["YASIN"] = "nPczCjzI2devNBz1zQrb"
    audio.VOICE_NAMES["YASIN"] = "Brian - Deep, Resonant and Comforting (Yasin)"
    write_review(entries)
    if args.dry_run:
        print(f"[MarketLifelineVoiceCopy] result=Passed narrative={len(entries)} locales=2 characters={sum(len(e[2])+len(e[3]) for e in entries)}")
        return
    key = audio.read_api_key(ROOT / ".local/secrets/elevenlabs_api_key")
    subscription = audio.request_json(key, "/v1/user/subscription")
    if subscription.get("status") != "active" or subscription.get("tier") in {None, "free"}:
        raise RuntimeError("An existing active paid ElevenLabs subscription is required.")
    previous = json.loads(MANIFEST.read_text(encoding="utf-8")) if MANIFEST.exists() else {"clips": []}
    prior = {(clip["id"], clip["locale"]): clip for clip in previous["clips"]}
    records: list[dict] = []
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    for index, (identity, speaker, english, persian) in enumerate(entries):
        for language, locale, text in (("en", "en-US", english), ("fa", "fa-IR", persian)):
            path = MANIFEST.parent / "Voice" / language / f"{identity}.wav"
            old = prior.get((identity, locale), {})
            if args.force or not persian_voice_profile.clip_matches(old, text, path, language):
                audio.convert(audio.request_audio(key, audio.VOICE_IDS[speaker], text, language, 7300 + index * 2 + (language == "fa")), path, speaker)
            clip = audio.record("narrative", identity, speaker, locale, text, path)
            clip["captionSha256"] = hashlib.sha256(text.encode("utf-8")).hexdigest()
            records.append(clip)
            print(f"[MarketLifelineVoice] {identity} {locale} speaker={speaker} duration={clip['durationSeconds']:.2f}s", flush=True)
    manifest = {
        "schema": "WarlineCapture.MarketLifelineBilingualVoice.v1",
        "missionId": "saga.ch02.m03.market_lifeline",
        "generatedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "provider": "ElevenLabs",
        "license": audio.RIGHTS,
        "model": audio.MODEL,
        "runtimeNetworkTts": False,
        "subscription": {"tier": subscription.get("tier"), "status": subscription.get("status")},
        "processing": {"sampleRateHz": 44100, "channels": 1, "sourceEncoding": "PCM_S16LE", "loudnessLUFS": -18},
        "clips": records,
    }
    MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("[MarketLifelineBilingualVoiceGeneration] result=Passed clips=14 narrative=7 locales=2 runtimeNetworkTts=0")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Prepare the exact missing comic payload; generate only after owner approval."""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
from pathlib import Path
import re

import generate_m02_bilingual_voice as audio
import generate_m04_bilingual_voice as airlift
import generate_split_front_bilingual_voice as split_front
import persian_voice_profile

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / "Design/AgentReports/MissingMissionVoices/20261001"
PAYLOAD = REPORT / "voice_payload_review.json"
SOURCE = ROOT / "Assets/Game/Scripts/Configs/Narrative/CH02M02SupplyLineCopy.cs"


def entries():
    audio.VOICE_IDS.update(LAILA="EXAVITQu4vr4xnSDxMaL", QASSEM="JBFqnCBsd6RMkjVDRZzb")
    audio.VOICE_NAMES.update(LAILA="Sarah - Mature, Reassuring, Confident (Captain Laila Nasser)",
                            QASSEM="George - controlled, persuasive antagonist (Nadir Qassem)")
    result = []
    for kind, identity, speaker, english, persian in airlift.lines():
        if kind == "narrative" and identity in {"m04-brief-01", "m04-brief-02"}:
            result.append(("M04Airlift", "m04_voice_manifest.json", identity, speaker, english, persian))
    string = split_front.STRING
    parsed = re.findall(r"new\(" + string + r",\s*NarrativeSpeakerId\.(\w+),\s*" + string + r",\s*" + string + r"\)", SOURCE.read_text())
    if len(parsed) != 7:
        raise RuntimeError(f"Expected seven Supply Line lines, got {len(parsed)}")
    for identity, speaker, english, persian in parsed:
        result.append(("CH02M02SupplyLine", "supply_line_voice_manifest.json", "supply_line-" + airlift.decode(identity),
                       speaker.upper(), airlift.decode(english), airlift.decode(persian)))
    for identity, speaker, english, persian in split_front.canonical_lines():
        result.append(("CH04M03SplitFront", "split_front_voice_manifest.json", identity, speaker, english, persian))
    if len(result) != 17:
        raise RuntimeError("Missing-voice payload must contain exactly seventeen bilingual pairs")
    return result


def payload_for(lines):
    clips = []
    for index, (folder, manifest, identity, speaker, english, persian) in enumerate(lines):
        for language, locale, text in (("en", "en-US", english), ("fa", "fa-IR", persian)):
            body = {"text": text, "model_id": audio.MODEL, "language_code": language,
                    "seed": 10100 + index * 2 + (language == "fa"),
                    "apply_text_normalization": "on"}
            persian_voice_profile.apply(body, language)
            clips.append({"id": identity, "speaker": speaker, "locale": locale,
                          "caption": text, "request": body,
                          "destination": f"{audio.API_ROOT}/v1/text-to-speech/{audio.VOICE_IDS[speaker]}",
                          "assetPath": f"Assets/Game/Audio/Narrative/{folder}/Voice/{language}/{identity}.wav"})
    return {"provider": "ElevenLabs", "model": audio.MODEL, "outputFormat": audio.OUTPUT_FORMAT,
            "scope": "Four replacement Airlift clips, fourteen Supply Line clips and sixteen Split Front clips",
            "runtimeNetworkTts": False, "paidSubscriptionRequired": True,
            "clipCount": len(clips), "captionCharacters": sum(len(c["caption"]) for c in clips), "clips": clips}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--generate-approved-payload", action="store_true",
                        help="Only use after owner approval of the prepared exact payload and destination")
    args = parser.parse_args()
    lines = entries()
    payload = payload_for(lines)
    if not args.generate_approved_payload:
        REPORT.mkdir(parents=True, exist_ok=True)
        PAYLOAD.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n")
        print(f"[MissingMissionVoicePayload] result=Passed pairs=17 clips=34 characters={payload['captionCharacters']} externalRequests=0")
        return
    if not PAYLOAD.exists() or json.loads(PAYLOAD.read_text()) != payload:
        raise RuntimeError("Canonical copy differs from the prepared exact approval payload; prepare and review it again")
    key = audio.read_api_key(ROOT / ".local/secrets/elevenlabs_api_key")
    subscription = audio.request_json(key, "/v1/user/subscription")
    if subscription.get("status") != "active" or subscription.get("tier") in {None, "free"}:
        raise RuntimeError("An existing active paid ElevenLabs subscription is required")
    manifests = {}
    completed = 0
    for index, (folder, filename, identity, speaker, english, persian) in enumerate(lines):
        manifest_path = ROOT / f"Assets/Game/Audio/Narrative/{folder}/{filename}"
        if manifest_path not in manifests:
            manifests[manifest_path] = json.loads(manifest_path.read_text()) if manifest_path.exists() else {
                "schema": "WarlineCapture.CampaignBilingualVoice.v1", "clips": []}
        manifest = manifests[manifest_path]
        for language, locale, text in (("en", "en-US", english), ("fa", "fa-IR", persian)):
            path = manifest_path.parent / "Voice" / language / f"{identity}.wav"
            old = next((c for c in manifest["clips"] if c["id"] == identity and c["locale"] == locale), {})
            if not persian_voice_profile.clip_matches(old, text, path, language):
                audio.convert(audio.request_audio(key, audio.VOICE_IDS[speaker], text, language,
                                                  10100 + index * 2 + (language == "fa")), path, speaker)
            record = audio.record("narrative", identity, speaker, locale, text, path)
            record["captionSha256"] = hashlib.sha256(text.encode()).hexdigest()
            manifest["clips"] = [c for c in manifest["clips"] if (c["id"], c["locale"]) != (identity, locale)] + [record]
            manifest.update(provider="ElevenLabs", license=audio.RIGHTS, model=audio.MODEL,
                            missionId={"M04Airlift": "saga.ch01.m04.airlift",
                                       "CH02M02SupplyLine": "saga.ch02.m02.supply_line",
                                       "CH04M03SplitFront": "saga.ch04.m03.split_front"}[folder],
                            generatedAtUtc=dt.datetime.now(dt.timezone.utc).isoformat(), runtimeNetworkTts=False,
                            subscription={k: subscription[k] for k in ("tier", "status")},
                            processing={"sampleRateHz": 44100, "channels": 1, "sourceEncoding": "PCM_S16LE", "loudnessLUFS": -18})
            manifest_path.parent.mkdir(parents=True, exist_ok=True)
            manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
            completed += 1
            print(f"[MissingMissionVoice] {identity} {locale} duration={record['durationSeconds']:.2f}s", flush=True)
    print(f"[MissingMissionVoiceGeneration] result=Passed clips={completed} runtimeNetworkTts=0")


if __name__ == "__main__":
    main()

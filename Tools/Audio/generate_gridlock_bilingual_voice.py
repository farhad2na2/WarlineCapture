#!/usr/bin/env python3
"""Generate Gridlock voice from the authoritative C# copy catalogs; no runtime TTS."""
from __future__ import annotations
import argparse
import persian_voice_profile
import copy
import datetime as dt
import hashlib
import json
from pathlib import Path
import re
import generate_m02_bilingual_voice as audio

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "Assets/Game/Audio/Narrative/CH02M01Gridlock/gridlock_voice_manifest.json"
STRING = r'"((?:[^"\\]|\\.)*)"'

def decode(value):
    return json.loads('"' + value + '"')

def lines():
    source = (ROOT / "Assets/Game/Scripts/Configs/Narrative/CH02M01GridlockNarrativeCopy.cs").read_text()
    pattern = r'new\(' + STRING + r',NarrativeSpeakerId\.(\w+),' + STRING + ',' + STRING + r'\)'
    narrative = re.findall(pattern, source)
    if len(narrative) != 12:
        raise RuntimeError(f"Expected 12 canonical narrative lines, got {len(narrative)}")
    for identity, speaker, english, persian in narrative:
        yield "narrative", "gridlock-" + decode(identity), speaker.upper(), decode(english), decode(persian)
    lessons = (ROOT / "Assets/Game/Scripts/Editor/CH02M01GridlockTutorialCopy.cs").read_text()
    tutorial = re.findall(r'\(' + ','.join([STRING] * 4) + r'\)', lessons)
    if len(tutorial) != 10:
        raise RuntimeError(f"Expected 10 canonical tutorial steps, got {len(tutorial)}")
    for i, row in enumerate(tutorial, 1):
        yield "tutorial", f"tutorial-gridlock-{i:02}", "ARIA", decode(row[2]), decode(row[3])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--force", action="store_true")
    args = parser.parse_args()
    entries = list(lines())
    audio.VOICE_IDS["FADI"] = "JBFqnCBsd6RMkjVDRZzb"
    audio.VOICE_NAMES["FADI"] = "George - Warm, Captivating Storyteller (Fadi)"
    review = json.loads((ROOT / "Design/AgentReports/CH02M01Gridlock/voice_payload_review.json").read_text())
    if [list(entry) for entry in entries] != review["entries"]:
        raise RuntimeError("Canonical Gridlock copy differs from the prepared approval payload.")
    if args.dry_run:
        print(f"[GridlockVoiceCopy] result=Passed narrative=12 tutorial=10 locales=2 characters={sum(len(e[3])+len(e[4]) for e in entries)}")
        return
    key = audio.read_api_key(ROOT / ".local/secrets/elevenlabs_api_key")
    subscription = audio.request_json(key, "/v1/user/subscription")
    if subscription.get("status") != "active" or subscription.get("tier") in {None, "free"}:
        raise RuntimeError("An existing active paid ElevenLabs subscription is required.")
    previous = json.loads(MANIFEST.read_text()) if MANIFEST.exists() else {"clips": []}
    prior = {(c["id"], c["locale"]): c for c in previous["clips"]}
    manifest = {"schema": "WarlineCapture.GridlockBilingualVoice.v1", "missionId": "saga.ch02.m01.gridlock",
        "generatedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(), "provider": "ElevenLabs",
        "license": audio.RIGHTS, "model": audio.MODEL, "runtimeNetworkTts": False,
        "subscription": {"tier": subscription.get("tier"), "status": subscription.get("status")},
        "processing": {"sampleRateHz": 44100, "channels": 1, "sourceEncoding": "PCM_S16LE", "loudnessLUFS": -18}, "clips": []}
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    for index, (kind, identity, speaker, english, persian) in enumerate(entries):
        for language, locale, text in (("en", "en-US", english), ("fa", "fa-IR", persian)):
            path = MANIFEST.parent / "Voice" / language / (identity + ".wav")
            old = prior.get((identity, locale), {})
            matching = persian_voice_profile.clip_matches(old, text, path, language)
            if args.force or not matching:
                audio.convert(audio.request_audio(key, audio.VOICE_IDS[speaker], text, language, 6200 + index*2 + (language=="fa")), path, speaker)
            clip = audio.record(kind, identity, speaker, locale, text, path)
            clip["captionSha256"] = hashlib.sha256(text.encode()).hexdigest()
            clip.update(persian_voice_profile.metadata(language))
            manifest["clips"].append(clip)
            MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
            print(f"[GridlockVoice] {kind} {identity} {locale} duration={clip['durationSeconds']:.2f}s", flush=True)
    register_events(manifest["clips"])
    print("[GridlockBilingualVoiceGeneration] result=Passed narrative=24 tutorial=20 locales=2")

def register_events(clips):
    path=ROOT / "Assets/Game/Audio/Config/audio_event_catalog_v0_1.json"
    catalog=json.loads(path.read_text())
    template=next(e for e in catalog["events"] if e["eventId"]=="VO.ARIA.Tutorial.M02.OpenBuild.En")
    catalog["events"]=[e for e in catalog["events"] if not e["eventId"].lower().startswith(("vo.aria.tutorial.gridlock.","vo.aria.gridlock.comms."))]
    for clip in clips:
        if clip["kind"]!="tutorial" and clip["id"]!="gridlock-comms-01":
            continue
        suffix="fa" if clip["locale"]=="fa-IR" else "en"
        identity="vo.aria.tutorial.gridlock."+clip["id"].rsplit("-",1)[1] if clip["kind"]=="tutorial" else "vo.aria.gridlock.comms.01"
        event=copy.deepcopy(template)
        event["eventId"]=identity+"."+suffix
        event["clips"]=[{"assetPath":clip["assetPath"],"status":"generated-elevenlabs","weight":1}]
        event.pop("localizedClips",None)
        catalog["events"].append(event)
    path.write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+"\n")

if __name__ == "__main__":
    main()

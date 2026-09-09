#!/usr/bin/env python3
"""Generate M03 voice from the authoritative C# copy catalogs; no runtime TTS."""
from __future__ import annotations
import argparse
import copy
import datetime as dt
import hashlib
import json
from pathlib import Path
import re
import generate_m02_bilingual_voice as audio

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "Assets/Game/Audio/Narrative/M03RadarWarning/m03_voice_manifest.json"
STRING = r'"((?:[^"\\]|\\.)*)"'

def decode(value):
    return json.loads('"' + value + '"')

def lines():
    source = (ROOT / "Assets/Game/Scripts/Configs/Narrative/M03RadarWarningCopyCatalog.cs").read_text()
    pattern = r'new\(' + STRING + r',NarrativeSpeakerId\.(\w+),\s*' + STRING + r',\s*' + STRING + r'\)'
    narrative = re.findall(pattern, source)
    if len(narrative) != 11:
        raise RuntimeError(f"Expected 11 canonical narrative lines including outcomes, got {len(narrative)}")
    for identity, speaker, english, persian in narrative:
        yield "narrative", "m03-" + decode(identity), speaker.upper(), decode(english), decode(persian)
    source = (ROOT / "Assets/Game/Scripts/Configs/Localization/M03RadarWarningTutorialCopyCatalog.cs").read_text()
    tutorial = re.findall(r'\(' + STRING + ',' + STRING + ',' + STRING + ',' + STRING + r'\)', source)
    if len(tutorial) != 12:
        raise RuntimeError(f"Expected 12 canonical tutorial steps, got {len(tutorial)}")
    for i, (_, english, _, persian) in enumerate(tutorial, 1):
        yield "tutorial", f"tutorial-m03-{i:02}", "ARIA", decode(english), decode(persian)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--force", action="store_true")
    args = parser.parse_args()
    entries = list(lines())
    if args.dry_run:
        print(f"[M03VoiceCopy] result=Passed narrative=11 tutorial=12 locales=2 characters={sum(len(e[3])+len(e[4]) for e in entries)}")
        return
    key = audio.read_api_key(ROOT / ".local/secrets/elevenlabs_api_key")
    subscription = audio.request_json(key, "/v1/user/subscription")
    if subscription.get("status") != "active" or subscription.get("tier") in {None, "free"}:
        raise RuntimeError("An existing active paid ElevenLabs subscription is required.")
    previous = json.loads(MANIFEST.read_text()) if MANIFEST.exists() else {"clips": []}
    prior = {(c["id"], c["locale"]): c for c in previous["clips"]}
    manifest = {"schema": "WarlineCapture.M03BilingualVoice.v1", "missionId": "saga.ch01.m03.radar_warning",
        "generatedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(), "provider": "ElevenLabs",
        "license": audio.RIGHTS, "model": audio.MODEL, "runtimeNetworkTts": False,
        "subscription": {"tier": subscription.get("tier"), "status": subscription.get("status")},
        "processing": {"sampleRateHz": 44100, "channels": 1, "sourceEncoding": "PCM_S16LE", "loudnessLUFS": -18}, "clips": []}
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    for index, (kind, identity, speaker, english, persian) in enumerate(entries):
        for language, locale, text in (("en", "en-US", english), ("fa", "fa-IR", persian)):
            path = MANIFEST.parent / "Voice" / language / (identity + ".wav")
            old = prior.get((identity, locale), {})
            matching = path.exists() and old.get("text") == text and old.get("sha256") == hashlib.sha256(path.read_bytes()).hexdigest()
            if args.force or not matching:
                audio.convert(audio.request_audio(key, audio.VOICE_IDS[speaker], text, language, 3300 + index*2 + (language=="fa")), path, speaker)
            clip = audio.record(kind, identity, speaker, locale, text, path)
            clip["captionSha256"] = hashlib.sha256(text.encode()).hexdigest()
            manifest["clips"].append(clip)
            MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
            print(f"[M03Voice] {kind} {identity} {locale} duration={clip['durationSeconds']:.2f}s", flush=True)
    register_events(manifest["clips"])
    print("[M03BilingualVoiceGeneration] result=Passed narrative=22 tutorial=24 locales=2")

def register_events(clips):
    path=ROOT / "Assets/Game/Audio/Config/audio_event_catalog_v0_1.json"
    catalog=json.loads(path.read_text())
    template=next(e for e in catalog["events"] if e["eventId"]=="VO.ARIA.Tutorial.M02.OpenBuild.En")
    catalog["events"]=[e for e in catalog["events"] if not e["eventId"].lower().startswith(("vo.aria.tutorial.m03.","vo.aria.m03.comms."))]
    for clip in clips:
        if clip["kind"]!="tutorial" and clip["id"]!="m03-comms-01":
            continue
        suffix="fa" if clip["locale"]=="fa-IR" else "en"
        identity="vo.aria.tutorial.m03."+clip["id"].rsplit("-",1)[1] if clip["kind"]=="tutorial" else "vo.aria.m03.comms.01"
        event=copy.deepcopy(template)
        event["eventId"]=identity+"."+suffix
        event["clips"]=[{"assetPath":clip["assetPath"],"status":"generated-elevenlabs","weight":1}]
        event.pop("localizedClips",None)
        catalog["events"].append(event)
    path.write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+"\n")

if __name__ == "__main__":
    main()

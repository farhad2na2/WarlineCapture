#!/usr/bin/env python3
"""Generate final bilingual Route Reopened story voices from canonical C# copy."""
from __future__ import annotations
import argparse
import datetime as dt
import hashlib
import json
from pathlib import Path
import re
import generate_m02_bilingual_voice as audio
import persian_voice_profile

ROOT=Path(__file__).resolve().parents[2]
MANIFEST=ROOT/"Assets/Game/Audio/Narrative/CH02M05RouteReopened/route_reopened_voice_manifest.json"
REVIEW=ROOT/"Design/AgentReports/CH02M05RouteReopened/voice_payload_review.json"
STRING=r'"((?:[^"\\]|\\.)*)"'
def decode(value):return json.loads('"'+value+'"')
def canonical_lines():
    source=(ROOT/"Assets/Game/Scripts/Configs/Narrative/CH02M05RouteReopenedCopy.cs").read_text(encoding="utf-8")
    parsed=re.findall(r'new\('+STRING+r',NarrativeSpeakerId\.(\w+),\s*'+STRING+r',\s*'+STRING+r'\)',source,re.MULTILINE)
    if len(parsed)!=7:raise RuntimeError(f"Expected 7 canonical Route Reopened lines, got {len(parsed)}")
    return [("route_reopened-"+decode(identity),speaker.upper(),decode(english),decode(persian)) for identity,speaker,english,persian in parsed]
def write_review(entries):
    REVIEW.parent.mkdir(parents=True,exist_ok=True)
    REVIEW.write_text(json.dumps({"mission":"saga.ch02.m05.route_reopened","provider":"ElevenLabs","model":audio.MODEL,"cast":{"DALIA":audio.VOICE_IDS["DALIA"],"SAMIRA":audio.VOICE_IDS["SAMIRA"],"ARIA":audio.VOICE_IDS["ARIA"],"QASSEM":audio.VOICE_IDS["QASSEM"]},"castDirection":{"DALIA":audio.VOICE_NAMES["DALIA"],"SAMIRA":audio.VOICE_NAMES["SAMIRA"],"ARIA":audio.VOICE_NAMES["ARIA"],"QASSEM":audio.VOICE_NAMES["QASSEM"]},"purpose":"Bilingual authored mission story clips; local runtime assets; no runtime network TTS","entries":[list(e) for e in entries]},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument("--dry-run",action="store_true");parser.add_argument("--force",action="store_true");args=parser.parse_args();entries=canonical_lines();audio.VOICE_IDS["QASSEM"]="JBFqnCBsd6RMkjVDRZzb";audio.VOICE_NAMES["QASSEM"]="George - controlled, persuasive antagonist (Nadir Qassem)";write_review(entries)
    if args.dry_run:print(f"[RouteReopenedVoiceCopy] result=Passed narrative={len(entries)} locales=2 characters={sum(len(e[2])+len(e[3]) for e in entries)}");return
    key=audio.read_api_key(ROOT/".local/secrets/elevenlabs_api_key");subscription=audio.request_json(key,"/v1/user/subscription")
    if subscription.get("status")!="active" or subscription.get("tier") in {None,"free"}:raise RuntimeError("An existing active paid ElevenLabs subscription is required.")
    previous=json.loads(MANIFEST.read_text(encoding="utf-8")) if MANIFEST.exists() else {"clips":[]};prior={(c["id"],c["locale"]):c for c in previous["clips"]};records=[];MANIFEST.parent.mkdir(parents=True,exist_ok=True)
    for index,(identity,speaker,english,persian) in enumerate(entries):
        for language,locale,text in (("en","en-US",english),("fa","fa-IR",persian)):
            path=MANIFEST.parent/"Voice"/language/f"{identity}.wav";old=prior.get((identity,locale),{})
            if args.force or not persian_voice_profile.clip_matches(old,text,path,language):audio.convert(audio.request_audio(key,audio.VOICE_IDS[speaker],text,language,8400+index*2+(language=="fa")),path,speaker)
            clip=audio.record("narrative",identity,speaker,locale,text,path);clip["captionSha256"]=hashlib.sha256(text.encode("utf-8")).hexdigest();clip.update(persian_voice_profile.metadata(language));records.append(clip);print(f"[RouteReopenedVoice] {identity} {locale} speaker={speaker} duration={clip['durationSeconds']:.2f}s",flush=True)
    manifest={"schema":"WarlineCapture.RouteReopenedBilingualVoice.v1","missionId":"saga.ch02.m05.route_reopened","generatedAtUtc":dt.datetime.now(dt.timezone.utc).isoformat(),"provider":"ElevenLabs","license":audio.RIGHTS,"model":audio.MODEL,"runtimeNetworkTts":False,"subscription":{"tier":subscription.get("tier"),"status":subscription.get("status")},"processing":{"sampleRateHz":44100,"channels":1,"sourceEncoding":"PCM_S16LE","loudnessLUFS":-18},"clips":records};MANIFEST.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n",encoding="utf-8");print("[RouteReopenedBilingualVoiceGeneration] result=Passed clips=14 narrative=7 locales=2 runtimeNetworkTts=0")
if __name__=="__main__":main()

#!/usr/bin/env python3
"""Generate final bilingual M02 narrative and ARIA tutorial voice assets."""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import wave


ROOT = Path(__file__).resolve().parents[2]
API_ROOT = "https://api.elevenlabs.io"
MODEL = "eleven_v3"
OUTPUT_FORMAT = "mp3_44100_192"
RIGHTS = "ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE"
VOICE_IDS = {
    "DALIA": "MK1Zvh93428YrgOQ8Obr",
    "SAMIRA": "7uxeJ73HfJL9gOH2mttA",
    "ARIA": "Fi9tPTnEcbh3of7hOHC8",
}
VOICE_NAMES = {
    "DALIA": "Warline - Major Dalia Rahim",
    "SAMIRA": "Warline - Engineer Samira Haddad",
    "ARIA": "Warline - ARIA Civic Relay",
}

NARRATIVE = (
    ("m02_brief_line_1", "DALIA", "This forward post is abandoned, but we need it. Restore it and prepare to defend the clinic road.",
     "این پاسگاه متروکه است، اما به آن نیاز داریم. آن را دوباره فعال کنید و برای دفاع از مسیر درمانگاه آماده شوید."),
    ("m02_brief_line_2", "ARIA", "Build a Barracks here, then train one rifle squad. That will make the post operational.",
     "اینجا یک سربازخانه بسازید، سپس یک گروه تفنگدار آموزش دهید. با این کار پاسگاه دوباره فعال می‌شود."),
    ("m02_brief_line_3", "SAMIRA", "The clinic and city crews use this road. Holding the post keeps their route open.",
     "درمانگاه و نیروهای خدمات شهری از این مسیر استفاده می‌کنند. حفظ پاسگاه، راه آن‌ها را باز نگه می‌دارد."),
    ("m02_comms_line_1", "DALIA", "Enemy patrol approaching from the west. Hold the post and keep them away from the clinic road.",
     "یک گشت دشمن از غرب نزدیک می‌شود. پاسگاه را حفظ کنید و نگذارید به مسیر درمانگاه برسند."),
    ("m02_comms_line_2", "ARIA", "We found a city access list on one attacker. It was copied before the first strike.",
     "یک فهرست دسترسی شهری همراه یکی از مهاجمان پیدا شد. این فهرست پیش از نخستین حمله کپی شده است."),
    ("m02_comms_line_3", "SAMIRA", "It marks power stations, service gates, and tunnels. Someone stole it before the attack.",
     "در آن، پست‌های برق، ورودی‌های خدماتی و تونل‌ها مشخص شده‌اند. کسی پیش از حمله آن را دزدیده است."),
    ("m02_debrief_line_1", "SAMIRA", "The post is active again. The clinic road and city response teams are connected.",
     "پاسگاه دوباره فعال است. مسیر درمانگاه و تیم‌های امداد شهری دوباره به هم متصل شده‌اند."),
    ("m02_debrief_line_2", "DALIA", "Commander, Dalia Rahim. I will lead the ground response from this post.",
     "فرمانده، دالیا رحیم هستم. از این پاسگاه، هدایت نیروهای زمینی را بر عهده می‌گیرم."),
    ("m02_debrief_line_3", "ARIA", "The warning network ahead has gone dark. Armored vehicles are moving toward the next sector.",
     "شبکه هشدار در مسیر پیش رو خاموش شده است. خودروهای زرهی به سمت منطقه بعدی حرکت می‌کنند."),
)

TUTORIAL = (
    (2, "open_build", "Open the Build menu.", "منوی ساخت را باز کنید."),
    (3, "select_barracks", "Select Barracks from the building list.", "سربازخانه را از فهرست ساختمان‌ها انتخاب کنید."),
    (4, "place_barracks", "Place the Barracks inside the green area, then confirm construction.", "سربازخانه را داخل محدوده سبز قرار دهید و ساخت را تأیید کنید."),
    (5, "check_cost", "Check the resource bar. The Barracks cost 40,000 Credits and 90 Materials.", "نوار منابع را بررسی کنید. سربازخانه ۴۰ هزار اعتبار و ۹۰ واحد مصالح هزینه دارد."),
    (6, "train_rifle_squad", "Open production and recruit one rifle squad.", "بخش تولید را باز کنید و یک گروه تفنگدار آموزش دهید."),
    (7, "incoming_patrol", "An enemy patrol is approaching from the west. Prepare your squad at the marked lane.", "یک گشت دشمن از غرب نزدیک می‌شود. گروه خود را در مسیر علامت‌گذاری‌شده آماده کنید."),
    (8, "defend_post", "Hold the marked lane and protect the forward post.", "مسیر علامت‌گذاری‌شده را حفظ کنید و از پاسگاه دفاع کنید."),
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--api-key-file",
        type=Path,
        default=ROOT / ".local/secrets/elevenlabs_api_key",
    )
    parser.add_argument("--force", action="store_true")
    return parser.parse_args()


def read_api_key(path: Path) -> str:
    value = os.environ.get("ELEVENLABS_API_KEY", "").strip()
    if value:
        return value
    if not path.exists():
        raise RuntimeError(f"Missing ElevenLabs API key file: {path}")
    value = path.read_text(encoding="utf-8").strip()
    if not value:
        raise RuntimeError(f"Empty ElevenLabs API key file: {path}")
    return value


def request_json(api_key: str, path: str) -> dict:
    request = urllib.request.Request(
        f"{API_ROOT}{path}",
        headers={"xi-api-key": api_key, "Accept": "application/json"},
    )
    try:
        with urllib.request.urlopen(request, timeout=120) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exception:
        detail = exception.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"ElevenLabs HTTP {exception.code}: {detail}") from exception


def request_audio(api_key: str, voice_id: str, text: str, language: str, seed: int) -> bytes:
    query = urllib.parse.urlencode({"output_format": OUTPUT_FORMAT})
    body = {
        "text": text,
        "model_id": MODEL,
        "language_code": language,
        "seed": seed,
        "apply_text_normalization": "on",
    }
    request = urllib.request.Request(
        f"{API_ROOT}/v1/text-to-speech/{voice_id}?{query}",
        data=json.dumps(body).encode("utf-8"),
        method="POST",
        headers={
            "xi-api-key": api_key,
            "Content-Type": "application/json",
            "Accept": "audio/mpeg",
        },
    )
    try:
        with urllib.request.urlopen(request, timeout=180) as response:
            return response.read()
    except urllib.error.HTTPError as exception:
        detail = exception.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"ElevenLabs HTTP {exception.code}: {detail}") from exception


def convert(mp3_bytes: bytes, destination: Path, speaker: str) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    highpass = "90" if speaker == "ARIA" else "150"
    lowpass = "14000" if speaker == "ARIA" else "6500"
    audio_filter = f"highpass=f={highpass},lowpass=f={lowpass},loudnorm=I=-18:LRA=7:TP=-2"
    with tempfile.TemporaryDirectory(prefix="warline-m02-voice-") as directory:
        source = Path(directory) / "source.mp3"
        source.write_bytes(mp3_bytes)
        subprocess.run(
            [
                "ffmpeg", "-y", "-hide_banner", "-loglevel", "error",
                "-i", str(source), "-af", audio_filter,
                "-ar", "44100", "-ac", "1", "-c:a", "pcm_s16le", str(destination),
            ],
            check=True,
        )


def record(kind: str, identity: str, speaker: str, locale: str, text: str, path: Path) -> dict:
    with wave.open(str(path), "rb") as audio:
        duration = audio.getnframes() / audio.getframerate()
    return {
        "kind": kind,
        "id": identity,
        "speaker": speaker,
        "locale": locale,
        "text": text,
        "voiceId": VOICE_IDS[speaker],
        "voiceName": VOICE_NAMES[speaker],
        "assetPath": path.relative_to(ROOT).as_posix(),
        "durationSeconds": round(duration, 6),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
    }


def write_manifest(path: Path, records: list[dict], subscription: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    manifest = {
        "schema": "WarlineCapture.M02BilingualVoice.v1",
        "missionId": "saga.ch01.m02.establish_base",
        "generatedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "provider": "ElevenLabs",
        "license": RIGHTS,
        "runtimeNetworkTts": False,
        "model": MODEL,
        "subscription": {"tier": subscription.get("tier"), "status": subscription.get("status")},
        "processing": {
            "sampleRateHz": 44100,
            "channels": 1,
            "sourceEncoding": "PCM_S16LE",
            "runtimeImportProfile": "Voice",
        },
        "clips": records,
    }
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    args = parse_args()
    api_key = read_api_key(args.api_key_file)
    subscription = request_json(api_key, "/v1/user/subscription")
    if subscription.get("status") != "active" or subscription.get("tier") in {None, "free"}:
        raise RuntimeError("An active paid ElevenLabs subscription is required.")

    narrative_records: list[dict] = []
    tutorial_records: list[dict] = []
    sequence = 3200
    for identity, speaker, english, persian in NARRATIVE:
        for locale, language, text, suffix in (
            ("en-US", "en", english, ""),
            ("fa-IR", "fa", persian, "_fa"),
        ):
            path = ROOT / f"Assets/Game/Audio/Narrative/M02EstablishBase/Voice/{'en' if language == 'en' else 'fa'}/{identity}{suffix}.wav"
            if args.force or not path.exists():
                convert(request_audio(api_key, VOICE_IDS[speaker], text, language, sequence), path, speaker)
            narrative_records.append(record("narrative", identity, speaker, locale, text, path))
            sequence += 1

    for step, identity, english, persian in TUTORIAL:
        for locale, language, text, suffix in (
            ("en-US", "en", english, ""),
            ("fa-IR", "fa", persian, "_fa"),
        ):
            path = ROOT / f"Assets/Game/Audio/Voice/Tutorial/{'en' if language == 'en' else 'fa'}/tutorial_m02_{identity}_aria{suffix}.wav"
            if args.force or not path.exists():
                convert(request_audio(api_key, VOICE_IDS["ARIA"], text, language, sequence), path, "ARIA")
            tutorial_records.append(record("tutorial", str(step), "ARIA", locale, text, path))
            sequence += 1

    write_manifest(
        ROOT / "Assets/Game/Audio/Narrative/M02EstablishBase/m02_narrative_voice_manifest.json",
        narrative_records,
        subscription,
    )
    write_manifest(
        ROOT / "Assets/Game/Audio/Voice/Tutorial/tutorial_m02_aria_voice_manifest.json",
        tutorial_records,
        subscription,
    )
    print("[M02BilingualVoiceGeneration] result=Passed narrative=18 tutorial=14 locales=2")


if __name__ == "__main__":
    main()

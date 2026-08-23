#!/usr/bin/env python3
"""Generate the exact bilingual ARIA cues used by Mission 1 tutorial actions."""

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
VOICE_ID = "Fi9tPTnEcbh3of7hOHC8"
VOICE_NAME = "Warline - ARIA Civic Relay"
OUTPUT_FORMAT = "mp3_44100_192"
MANIFEST_PATH = ROOT / "Assets/Game/Audio/Voice/Tutorial/tutorial_m01_aria_voice_manifest.json"

CUES = (
    (2, "command", "en-US", "en", "Tap MOVE to select the move command.",
     "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m01_move_to_cover_aria.wav", 2101),
    (3, "worldTarget", "en-US", "en", "Tap the highlighted destination to move your squad.",
     "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m01_move_destination_aria.wav", 2102),
    (4, "command", "en-US", "en", "Tap ATTACK to select the attack command.",
     "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m01_confirm_threat_aria.wav", 2103),
    (5, "worldTarget", "en-US", "en", "Tap the highlighted enemy to issue the attack.",
     "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m01_attack_target_aria.wav", 2104),
    (2, "command", "fa-IR", "fa", "برای انتخاب دستور حرکت، روی «حرکت» بزنید.",
     "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_move_to_cover_aria_fa.wav", 2201),
    (3, "worldTarget", "fa-IR", "fa", "برای حرکت گروه، روی مقصد علامت‌گذاری‌شده بزنید.",
     "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_move_destination_aria_fa.wav", 2202),
    (4, "command", "fa-IR", "fa", "برای انتخاب دستور حمله، روی «حمله» بزنید.",
     "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_confirm_threat_aria_fa.wav", 2203),
    (5, "worldTarget", "fa-IR", "fa", "برای صدور دستور حمله، روی دشمن علامت‌گذاری‌شده بزنید.",
     "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_attack_target_aria_fa.wav", 2204),
)

PRESERVED_CUES = (
    (1, "selection", "en-US", "Select the command squad to begin.",
     "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m01_find_squad_aria.wav"),
    (1, "selection", "fa-IR", "برای شروع، گروه فرماندهی را انتخاب کنید.",
     "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_find_squad_aria_fa.wav"),
    (5, "missionResolution", "en-US", "Check the objective and secure the civilian route.",
     "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m01_secure_corridor_aria.wav"),
    (5, "missionResolution", "fa-IR", "هدف را بررسی کنید و مسیر غیرنظامیان را امن کنید.",
     "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_secure_corridor_aria_fa.wav"),
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--api-key-file",
        type=Path,
        default=Path("/private/tmp/warlinecapture-secrets/elevenlabs_api_key"),
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


def request_audio(api_key: str, text: str, language: str, seed: int) -> bytes:
    query = urllib.parse.urlencode({"output_format": OUTPUT_FORMAT})
    body = {
        "text": text,
        "model_id": MODEL,
        "language_code": language,
        "seed": seed,
        "apply_text_normalization": "on",
    }
    request = urllib.request.Request(
        f"{API_ROOT}/v1/text-to-speech/{VOICE_ID}?{query}",
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


def convert_to_wav(mp3_bytes: bytes, destination: Path) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="warline-m01-voice-") as directory:
        source = Path(directory) / "source.mp3"
        source.write_bytes(mp3_bytes)
        subprocess.run(
            [
                "ffmpeg", "-y", "-hide_banner", "-loglevel", "error",
                "-i", str(source), "-ar", "44100", "-ac", "1",
                "-c:a", "pcm_s16le", str(destination),
            ],
            check=True,
        )


def clip_record(step: int, phase: str, locale: str, text: str, asset_path: str) -> dict:
    path = ROOT / asset_path
    with wave.open(str(path), "rb") as audio:
        duration = audio.getnframes() / audio.getframerate()
    return {
        "step": step,
        "phase": phase,
        "locale": locale,
        "text": text,
        "assetPath": asset_path,
        "durationSeconds": round(duration, 6),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
    }


def main() -> None:
    args = parse_args()
    api_key = read_api_key(args.api_key_file)
    subscription = request_json(api_key, "/v1/user/subscription")
    if subscription.get("status") != "active" or subscription.get("tier") in {None, "free"}:
        raise RuntimeError("An active paid ElevenLabs subscription is required.")

    records = [clip_record(*cue) for cue in PRESERVED_CUES]
    for step, phase, locale, language, text, asset_path, seed in CUES:
        destination = ROOT / asset_path
        if args.force or not destination.exists():
            convert_to_wav(request_audio(api_key, text, language, seed), destination)
        records.append(clip_record(step, phase, locale, text, asset_path))

    records.sort(key=lambda item: (item["locale"], item["step"], item["phase"]))
    manifest = {
        "schema": "WarlineCapture.TutorialNarrationVoice.v2",
        "missionId": "campaign.chapter01.mission01",
        "generatedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "provider": "ElevenLabs",
        "license": "ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE",
        "runtimeNetworkTts": False,
        "model": MODEL,
        "voice": {"id": VOICE_ID, "name": VOICE_NAME},
        "processing": {
            "sampleRateHz": 44100,
            "channels": 1,
            "sourceEncoding": "PCM_S16LE",
            "runtimeImportProfile": "Voice",
        },
        "subscription": {
            "tier": subscription.get("tier"),
            "status": subscription.get("status"),
        },
        "clips": records,
    }
    MANIFEST_PATH.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"[M01TutorialVoiceGeneration] result=Passed clips={len(records)} locales=2")


if __name__ == "__main__":
    main()

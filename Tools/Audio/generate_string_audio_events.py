#!/usr/bin/env python3
"""Generate ARIA voice clips for configured feedback strings.

This is a data/asset generation step only. It adds semantic audio event ids to
selected GameStrings entries, creates spoken WAV clips, and appends
matching events to the existing audio catalog. Runtime playback is intentionally
left to the audio request systems.
"""

from __future__ import annotations

import argparse
import concurrent.futures
import datetime as dt
import hashlib
import json
import os
import re
import shutil
import subprocess
import time
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import wave
from dataclasses import dataclass
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
STRINGS_PATH = ROOT / "Assets" / "Game" / "Configs" / "Scene" / "Game_GameStrings_Config.asset"
AUDIO_ROOT = ROOT / "Assets" / "Game" / "Audio"
VOICE_ROOT = AUDIO_ROOT / "Voice" / "ARIA"
CONFIG_ROOT = AUDIO_ROOT / "Config"
GENERATED_ROOT = AUDIO_ROOT / "GeneratedSource"
CATALOG_PATH = CONFIG_ROOT / "audio_event_catalog_v0_1.json"
MANIFEST_PATH = GENERATED_ROOT / "string_audio_event_manifest_v0_1.json"
EVENT_PREFIX = "VO.ARIA.Message."
DEFAULT_BACKEND = "edge"
DEFAULT_EDGE_TTS_PATH = "/private/tmp/warline-edge-tts"
DEFAULT_EDGE_VOICE = "en-US-AriaNeural"
DEFAULT_EDGE_RATE = "-6%"
DEFAULT_EDGE_VOLUME = "+0%"
DEFAULT_EDGE_PITCH = "-2Hz"
DEFAULT_ESPEAK_RATE = "155"
DEFAULT_SECRET_PATH = Path(os.environ.get("LOCALAPPDATA", Path.home())) / "WarlineCapture/Secrets/elevenlabs_api_key.txt"
ELEVENLABS_VOICE_MAP_PATH = ROOT / "Assets/Game/Data/Narrative/FirstLaunch/first_launch_elevenlabs_voice_map.json"
ELEVENLABS_API_ROOT = "https://api.elevenlabs.io"
ELEVENLABS_MODEL = "eleven_v3"
ELEVENLABS_OUTPUT_FORMAT = "mp3_44100_192"
ELEVENLABS_RIGHTS_STATUS = "ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE"
ELEVENLABS_ARIA_SPEAKER = "ARIA"
ELEVENLABS_SEED = 812047
ELEVENLABS_STAGING_ROOT = GENERATED_ROOT / "ARIAElevenLabsRaw"
ELEVENLABS_STAGING_MANIFEST = ELEVENLABS_STAGING_ROOT / "staging_manifest.json"


EXACT_KEYS = {
    "confirm_destroy",
    "not_enough_money",
    "create_first",
    "drag_building_to_final_position",
}


TARGET_PREFIXES = (
    "warning_",
    "tactical.command.reason.",
    "tactical.command.instruction.",
    "tactical.command.board.",
    "tactical.command.unavailable.",
    "tactical.feedback.",
    "tactical.airdrop.",
    "tactical.banner.",
    "build.drawer.ready.",
    "build.drawer.instruction.",
    "build.drawer.failure.",
    "build.drawer.success.",
    "build.drawer.empty.",
    "build.drawer.placement.",
    "build.drawer.action.",
    "build.feedback.",
    "build.placement.status.",
    "build.placement.title.",
    "build.placement.instruction.",
    "match.feedback.",
    "selection.feedback.",
)


@dataclass(frozen=True)
class StringAudioTarget:
    key: str
    text: str
    event_id: str
    clip_asset_path: str
    priority: str
    cooldown_ms: int
    volume_db: float


def unity_guid(path: Path) -> str:
    rel = path.relative_to(ROOT).as_posix()
    return hashlib.md5(f"warlinecapture-string-audio-v0.1:{rel}".encode("utf-8")).hexdigest()


def meta_path(path: Path) -> Path:
    return Path(f"{path}.meta")


def write_folder_meta(path: Path) -> None:
    meta = meta_path(path)
    if meta.exists():
        return
    meta.write_text(
        f"""fileFormatVersion: 2
guid: {unity_guid(path)}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: Generated ARIA message audio folder
  assetBundleName:
  assetBundleVariant:
"""
    )


def write_audio_meta(path: Path) -> None:
    meta_path(path).write_text(
        f"""fileFormatVersion: 2
guid: {unity_guid(path)}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    loadType: 0
    preloadAudioData: 1
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 1
    conversionMode: 0
  platformSettingOverrides: {{}}
  forceToMono: 1
  normalize: 1
  preloadAudioData: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 1
  userData: Generated ARIA message voice audio
  assetBundleName:
  assetBundleVariant:
"""
    )


def write_default_meta(path: Path, user_data: str) -> None:
    if meta_path(path).exists():
        return
    meta_path(path).write_text(
        f"""fileFormatVersion: 2
guid: {unity_guid(path)}
DefaultImporter:
  externalObjects: {{}}
  userData: {user_data}
  assetBundleName:
  assetBundleVariant:
"""
    )


def unquote_yaml_value(raw: str) -> str:
    value = raw.strip()
    if len(value) >= 2 and value[0] == "'" and value[-1] == "'":
        value = value[1:-1].replace("''", "'")
    return value


def should_generate_for_key(key: str) -> bool:
    return key in EXACT_KEYS or any(key.startswith(prefix) for prefix in TARGET_PREFIXES)


def to_pascal_key(key: str) -> str:
    parts = re.split(r"[^A-Za-z0-9]+", key)
    value = "".join(part[:1].upper() + part[1:] for part in parts if part)
    return value or "Message"


def to_clip_name(key: str) -> str:
    safe = re.sub(r"[^A-Za-z0-9]+", "_", key).strip("_").lower()
    return f"aria_message_{safe}_01.wav"


def normalize_spoken_text(text: str) -> str:
    replacements = {
        "{0}": "target",
        "{1}": "value",
        "{2}": "detail",
        " / ": " and ",
    }
    spoken = text
    for old, new in replacements.items():
        spoken = spoken.replace(old, new)
    spoken = spoken.replace(":", ".")
    spoken = re.sub(r"\s+", " ", spoken).strip()
    return spoken or "Message unavailable."


def priority_for_key(key: str) -> str:
    high_tokens = (
        "warning",
        "failure",
        "unavailable",
        "invalid",
        "blocked",
        "destroyed",
        "missile",
        "attack",
        "insufficient",
        "critical",
    )
    if any(token in key for token in high_tokens):
        return "High"
    return "Medium"


def cooldown_for_key(key: str) -> int:
    if key.startswith("warning_"):
        return 2500
    if ".failure." in key or ".reason." in key or ".unavailable." in key:
        return 900
    return 600


def volume_for_key(key: str) -> float:
    if key.startswith("warning_"):
        return -5.0
    if ".failure." in key or ".reason." in key:
        return -5.5
    return -6.0


def parse_string_targets() -> list[StringAudioTarget]:
    lines = STRINGS_PATH.read_text().splitlines()
    targets: list[StringAudioTarget] = []
    current_key = ""

    for line in lines:
        key_match = re.match(r"\s*- key: (.+)", line)
        if key_match:
            current_key = key_match.group(1).strip()
            continue

        value_match = re.match(r"\s+value: ?(.*)", line)
        if not value_match or not current_key or not should_generate_for_key(current_key):
            continue

        text = unquote_yaml_value(value_match.group(1))
        event_id = f"{EVENT_PREFIX}{to_pascal_key(current_key)}"
        clip_asset_path = f"Assets/Game/Audio/Voice/ARIA/{to_clip_name(current_key)}"
        targets.append(
            StringAudioTarget(
                key=current_key,
                text=text,
                event_id=event_id,
                clip_asset_path=clip_asset_path,
                priority=priority_for_key(current_key),
                cooldown_ms=cooldown_for_key(current_key),
                volume_db=volume_for_key(current_key),
            )
        )

    return targets


def update_string_config(targets: list[StringAudioTarget]) -> None:
    event_by_key = {target.key: target.event_id for target in targets}
    lines = STRINGS_PATH.read_text().splitlines()
    output: list[str] = []
    current_key = ""
    pending_audio_event_id = ""
    inserted_for_entry = False

    for line in lines:
        key_match = re.match(r"\s*- key: (.+)", line)
        if key_match:
            if pending_audio_event_id and not inserted_for_entry:
                output.append(f"    audioEventId: {pending_audio_event_id}")
            current_key = key_match.group(1).strip()
            pending_audio_event_id = event_by_key.get(current_key, "")
            inserted_for_entry = False
            output.append(line)
            continue

        audio_match = re.match(r"\s+audioEventId:.*", line)
        if audio_match and pending_audio_event_id:
            output.append(f"    audioEventId: {pending_audio_event_id}")
            inserted_for_entry = True
            continue

        output.append(line)

        if re.match(r"\s+value: ?.*", line) and pending_audio_event_id and not inserted_for_entry:
            output.append(f"    audioEventId: {pending_audio_event_id}")
            inserted_for_entry = True

    if pending_audio_event_id and not inserted_for_entry:
        output.append(f"    audioEventId: {pending_audio_event_id}")

    STRINGS_PATH.write_text("\n".join(output) + "\n")


def ensure_folders() -> None:
    for folder in (AUDIO_ROOT, AUDIO_ROOT / "Voice", VOICE_ROOT, CONFIG_ROOT, GENERATED_ROOT):
        folder.mkdir(parents=True, exist_ok=True)
        write_folder_meta(folder)


def generate_espeak_clip(target: StringAudioTarget, wav_path: Path) -> None:
    espeak_path = shutil.which("espeak")
    afconvert_path = shutil.which("afconvert")
    if not espeak_path or not afconvert_path:
        raise RuntimeError("Generating ARIA voice clips with eSpeak requires `espeak` and macOS `afconvert`.")

    with tempfile.TemporaryDirectory() as temp_dir:
        temp_wav = Path(temp_dir) / "voice_22050.wav"
        subprocess.run(
            [
                espeak_path,
                "-w",
                str(temp_wav),
                "-s",
                DEFAULT_ESPEAK_RATE,
                "-p",
                "45",
                "-a",
                "145",
                normalize_spoken_text(target.text),
            ],
            check=True,
        )
        subprocess.run(
            [afconvert_path, "-f", "WAVE", "-d", "LEI16@44100", str(temp_wav), str(wav_path)],
            check=True,
        )


def generate_edge_clip(
    target: StringAudioTarget,
    wav_path: Path,
    edge_tts_path: str,
    voice: str,
    rate: str,
    volume: str,
    pitch: str) -> None:
    ffmpeg_path = shutil.which("ffmpeg")
    if not ffmpeg_path:
        raise RuntimeError("Generating neural ARIA voice clips requires `ffmpeg` to convert MP3 to Unity WAV.")

    edge_package_path = Path(edge_tts_path)
    if not edge_package_path.exists():
        raise RuntimeError(
            f"edge-tts package path does not exist: {edge_package_path}. "
            "Install it with: python3 -m pip install --target /private/tmp/warline-edge-tts edge-tts")

    with tempfile.TemporaryDirectory() as temp_dir:
        temp_mp3 = Path(temp_dir) / "voice.mp3"
        env = dict(**__import__("os").environ)
        env["PYTHONPATH"] = str(edge_package_path)
        subprocess.run(
            [
                "python3",
                "-m",
                "edge_tts",
                "--voice",
                voice,
                f"--rate={rate}",
                f"--volume={volume}",
                f"--pitch={pitch}",
                "--text",
                normalize_spoken_text(target.text),
                "--write-media",
                str(temp_mp3),
            ],
            check=True,
            env=env,
        )
        subprocess.run(
            [
                ffmpeg_path,
                "-y",
                "-hide_banner",
                "-loglevel",
                "error",
                "-i",
                str(temp_mp3),
                "-ac",
                "1",
                "-ar",
                "44100",
                str(wav_path),
            ],
            check=True,
        )


def generate_voice_clip(
    target: StringAudioTarget,
    force: bool,
    backend: str,
    edge_tts_path: str,
    voice: str,
    rate: str,
    volume: str,
    pitch: str) -> dict[str, Any]:
    wav_path = ROOT / target.clip_asset_path
    if wav_path.exists() and not force:
        write_audio_meta(wav_path)
        return {"eventId": target.event_id, "assetPath": target.clip_asset_path, "status": "preserved", **read_wav_info(wav_path)}

    wav_path.parent.mkdir(parents=True, exist_ok=True)
    if backend == "edge":
        generate_edge_clip(target, wav_path, edge_tts_path, voice, rate, volume, pitch)
        status = "neural-tts"
    elif backend == "espeak":
        generate_espeak_clip(target, wav_path)
        status = "prototype-tts"
    else:
        raise ValueError(f"Unsupported voice backend: {backend}")

    write_audio_meta(wav_path)
    info = read_wav_info(wav_path)
    if info["durationSeconds"] <= 0:
        raise RuntimeError(f"Generated empty voice clip: {target.clip_asset_path}")
    return {"eventId": target.event_id, "assetPath": target.clip_asset_path, "status": status, **info}


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


def load_elevenlabs_aria_voice(path: Path) -> tuple[str, str]:
    voice_map = json.loads(path.read_text(encoding="utf-8"))
    for record in voice_map.get("voices", []):
        if record.get("speaker") == ELEVENLABS_ARIA_SPEAKER:
            voice_id = str(record.get("voiceId", "")).strip()
            if not voice_id:
                break
            return voice_id, str(record.get("name", "Warline - ARIA Civic Relay"))
    raise RuntimeError(f"ARIA voice is missing from ElevenLabs voice map: {path}")


def request_elevenlabs_audio(api_key: str, voice_id: str, text: str, seed: int) -> bytes:
    query = urllib.parse.urlencode({"output_format": ELEVENLABS_OUTPUT_FORMAT})
    body = {
        "text": text,
        "model_id": ELEVENLABS_MODEL,
        "language_code": "en",
        "seed": seed,
        "apply_text_normalization": "on",
    }
    request = urllib.request.Request(
        f"{ELEVENLABS_API_ROOT}/v1/text-to-speech/{voice_id}?{query}",
        data=json.dumps(body).encode("utf-8"),
        method="POST",
        headers={"xi-api-key": api_key, "Content-Type": "application/json", "Accept": "audio/mpeg"},
    )

    for attempt in range(5):
        try:
            with urllib.request.urlopen(request, timeout=180) as response:
                return response.read()
        except urllib.error.HTTPError as exc:
            detail = exc.read().decode("utf-8", errors="replace")
            if exc.code not in {408, 429, 500, 502, 503, 504} or attempt == 4:
                raise RuntimeError(f"ElevenLabs HTTP {exc.code} for voice {voice_id}: {detail}") from exc
            time.sleep(2 ** attempt)
    raise RuntimeError("ElevenLabs request retry loop exited unexpectedly.")


def generate_elevenlabs_clip(
    target: StringAudioTarget,
    force: bool,
    api_key: str,
    voice_id: str) -> dict[str, Any]:
    mp3_path = ELEVENLABS_STAGING_ROOT / Path(target.clip_asset_path).with_suffix(".mp3").name
    staging_asset_path = mp3_path.relative_to(ROOT).as_posix()
    if mp3_path.exists() and not force:
        return {
            "eventId": target.event_id,
            "assetPath": target.clip_asset_path,
            "stagingAssetPath": staging_asset_path,
            "status": "preserved",
            "voiceId": voice_id,
        }

    seed_offset = int(hashlib.sha256(target.key.encode("utf-8")).hexdigest()[:8], 16) % 100000
    raw_audio = request_elevenlabs_audio(
        api_key,
        voice_id,
        normalize_spoken_text(target.text),
        ELEVENLABS_SEED + seed_offset)
    temporary = mp3_path.with_suffix(".mp3.tmp")
    temporary.write_bytes(raw_audio)
    temporary.replace(mp3_path)
    return {
        "eventId": target.event_id,
        "assetPath": target.clip_asset_path,
        "stagingAssetPath": staging_asset_path,
        "status": "staged-elevenlabs-commercial",
        "voiceId": voice_id,
        "sourceBytes": len(raw_audio),
        "sourceSha256": hashlib.sha256(raw_audio).hexdigest(),
    }


def generate_elevenlabs_batch(
    targets: list[StringAudioTarget],
    force: bool,
    api_key: str,
    voice_id: str,
    jobs: int) -> list[dict[str, Any]]:
    def generate(index_and_target: tuple[int, StringAudioTarget]) -> tuple[int, dict[str, Any]]:
        index, target = index_and_target
        clip = generate_elevenlabs_clip(target, force, api_key, voice_id)
        print(f"[{index + 1:03d}/{len(targets):03d}] {target.key}", flush=True)
        return index, clip

    clips: list[dict[str, Any] | None] = [None] * len(targets)
    with concurrent.futures.ThreadPoolExecutor(max_workers=max(1, jobs)) as executor:
        for index, clip in executor.map(generate, enumerate(targets)):
            clips[index] = clip
    return [clip for clip in clips if clip is not None]


def write_elevenlabs_staging_manifest(
    targets: list[StringAudioTarget],
    staged_clips: list[dict[str, Any]],
    voice_id: str,
    voice_name: str) -> None:
    manifest = {
        "schema": "WarlineCapture.AriaMatchVoiceElevenLabsStaging.v0.1",
        "generatedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "voice": voice_name,
        "voiceId": voice_id,
        "model": ELEVENLABS_MODEL,
        "sourceFormat": ELEVENLABS_OUTPUT_FORMAT,
        "rightsStatus": ELEVENLABS_RIGHTS_STATUS,
        "targetCount": len(targets),
        "clips": staged_clips,
    }
    ELEVENLABS_STAGING_MANIFEST.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def finalize_elevenlabs_outputs(targets: list[StringAudioTarget], voice_id: str, voice_name: str) -> None:
    clips: list[dict[str, Any]] = []
    for target in targets:
        wav_path = ROOT / target.clip_asset_path
        if not wav_path.exists():
            raise RuntimeError(f"Missing Unity-converted ARIA voice clip: {target.clip_asset_path}")
        info = read_wav_info(wav_path)
        if info["channels"] != 1 or info["sampleRate"] != 44100:
            raise RuntimeError(f"Invalid Unity-converted ARIA voice clip: {target.clip_asset_path} -> {info}")
        clips.append(
            {
                "eventId": target.event_id,
                "assetPath": target.clip_asset_path,
                "status": "elevenlabs-commercial",
                "voiceId": voice_id,
                "sha256": hashlib.sha256(wav_path.read_bytes()).hexdigest(),
                **info,
            }
        )

    update_catalog(targets, "elevenlabs-commercial")
    update_string_config(targets)
    write_manifest(targets, clips, "elevenlabs", voice_name, "", "", "", voice_id)
    validate_outputs(targets)
    print(f"Finalized {len(clips)} licensed ARIA match-command voice clips.")


def read_wav_info(path: Path) -> dict[str, Any]:
    with wave.open(str(path), "rb") as wav:
        channels = wav.getnchannels()
        frames = wav.getnframes()
        rate = wav.getframerate()
    return {
        "channels": channels,
        "sampleRate": rate,
        "durationSeconds": round(frames / rate, 3),
    }


def ensure_voice_bus(catalog: dict[str, Any]) -> None:
    buses = catalog.setdefault("buses", [])
    if any(bus.get("busId") == "Voice" for bus in buses):
        return
    buses.append(
        {
            "busId": "Voice",
            "parentBusId": "Master",
            "defaultVolumeDb": -2.0,
            "ducks": ["Music", "Ambience"],
        }
    )


def build_catalog_event(target: StringAudioTarget, clip_status: str) -> dict[str, Any]:
    return {
        "eventId": target.event_id,
        "busId": "Voice",
        "priority": target.priority,
        "cooldownMs": target.cooldown_ms,
        "volumeDb": target.volume_db,
        "pitchVariance": {"min": 0.0, "max": 0.0},
        "playback": {
            "loop": False,
            "spatial": False,
            "maxInstances": 1,
            "allowRuntimeLoad": False,
        },
        "clips": [
            {
                "assetPath": target.clip_asset_path,
                "status": clip_status,
                "weight": 1,
            }
        ],
    }


def update_catalog(targets: list[StringAudioTarget], clip_status: str) -> None:
    catalog = json.loads(CATALOG_PATH.read_text())
    ensure_voice_bus(catalog)
    retained_events = [
        event for event in catalog.get("events", [])
        if not str(event.get("eventId", "")).startswith(EVENT_PREFIX)
    ]
    retained_events.extend(build_catalog_event(target, clip_status) for target in targets)
    event_ids = [event["eventId"] for event in retained_events]
    if len(event_ids) != len(set(event_ids)):
        raise RuntimeError("Duplicate event ids after adding ARIA string audio events.")
    catalog["events"] = retained_events
    CATALOG_PATH.write_text(json.dumps(catalog, indent=2) + "\n")
    write_default_meta(CATALOG_PATH, "WarlineCapture placeholder audio event catalog with ARIA message mappings")


def write_manifest(
    targets: list[StringAudioTarget],
    clips: list[dict[str, Any]],
    backend: str,
    voice: str,
    rate: str,
    volume: str,
    pitch: str,
    voice_id: str = "") -> None:
    manifest = {
        "schema": "WarlineCapture.StringAudioEventManifest.v0.1",
        "generatedBy": "Tools/Audio/generate_string_audio_events.py",
        "backend": backend,
        "voice": voice if backend in {"edge", "elevenlabs"} else "eSpeak English Prototype",
        "voiceId": voice_id,
        "model": ELEVENLABS_MODEL if backend == "elevenlabs" else "",
        "rightsStatus": ELEVENLABS_RIGHTS_STATUS if backend == "elevenlabs" else "",
        "generatedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "voiceRate": rate if backend == "edge" else "",
        "voiceVolume": volume if backend == "edge" else "",
        "voicePitch": pitch if backend == "edge" else "",
        "targetCount": len(targets),
        "catalogPath": CATALOG_PATH.relative_to(ROOT).as_posix(),
        "stringsPath": STRINGS_PATH.relative_to(ROOT).as_posix(),
        "note": "Licensed ARIA match-command voice clips generated with the persistent FirstLaunch ARIA voice." if backend == "elevenlabs" else "ARIA TTS clips for feedback/alert strings. Replace with final recorded voice before release if desired.",
        "targets": [
            {
                "key": target.key,
                "text": target.text,
                "spokenText": normalize_spoken_text(target.text),
                "eventId": target.event_id,
                "assetPath": target.clip_asset_path,
            }
            for target in targets
        ],
        "clips": clips,
    }
    MANIFEST_PATH.write_text(json.dumps(manifest, indent=2) + "\n")
    write_default_meta(MANIFEST_PATH, "Generated ARIA string audio event manifest")


def validate_outputs(targets: list[StringAudioTarget]) -> None:
    missing = []
    for target in targets:
        path = ROOT / target.clip_asset_path
        if not path.exists():
            missing.append(target.clip_asset_path)
        if not meta_path(path).exists():
            missing.append(f"{target.clip_asset_path}.meta")

    catalog = json.loads(CATALOG_PATH.read_text())
    catalog_event_ids = {event["eventId"] for event in catalog.get("events", [])}
    for target in targets:
        if target.event_id not in catalog_event_ids:
            missing.append(f"catalog event {target.event_id}")

    strings_text = STRINGS_PATH.read_text()
    for target in targets:
        if f"audioEventId: {target.event_id}" not in strings_text:
            missing.append(f"strings mapping {target.key} -> {target.event_id}")

    if missing:
        raise RuntimeError("Missing ARIA string audio outputs:\n" + "\n".join(missing))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--force", action="store_true", help="Regenerate existing message voice clips.")
    parser.add_argument("--dry-run", action="store_true", help="Print target count without writing files.")
    parser.add_argument("--backend", choices=["edge", "espeak", "elevenlabs"], default=DEFAULT_BACKEND)
    parser.add_argument("--edge-tts-path", default=DEFAULT_EDGE_TTS_PATH)
    parser.add_argument("--voice", default=DEFAULT_EDGE_VOICE)
    parser.add_argument("--rate", default=DEFAULT_EDGE_RATE)
    parser.add_argument("--volume", default=DEFAULT_EDGE_VOLUME)
    parser.add_argument("--pitch", default=DEFAULT_EDGE_PITCH)
    parser.add_argument("--api-key-file", type=Path, default=DEFAULT_SECRET_PATH)
    parser.add_argument("--voice-map", type=Path, default=ELEVENLABS_VOICE_MAP_PATH)
    parser.add_argument("--jobs", type=int, default=3)
    parser.add_argument("--finalize-elevenlabs", action="store_true")
    args = parser.parse_args()

    targets = parse_string_targets()
    if args.dry_run:
        for target in targets:
            print(f"{target.key} -> {target.event_id} -> {target.clip_asset_path}")
        print(f"{len(targets)} string audio targets")
        return

    if args.finalize_elevenlabs:
        voice_id, voice_name = load_elevenlabs_aria_voice(args.voice_map)
        finalize_elevenlabs_outputs(targets, voice_id, voice_name)
        return

    ensure_folders()
    voice_id = ""
    voice_name = args.voice
    if args.backend == "elevenlabs":
        api_key = read_api_key(args.api_key_file)
        voice_id, voice_name = load_elevenlabs_aria_voice(args.voice_map)
        ELEVENLABS_STAGING_ROOT.mkdir(parents=True, exist_ok=True)
        clips = generate_elevenlabs_batch(targets, args.force, api_key, voice_id, args.jobs)
        write_elevenlabs_staging_manifest(targets, clips, voice_id, voice_name)
        print(f"Staged {len(clips)} licensed ARIA match-command MP3 files for Unity conversion.")
        return
    else:
        clips = [
            generate_voice_clip(
                target,
                args.force,
                args.backend,
                args.edge_tts_path,
                args.voice,
                args.rate,
                args.volume,
                args.pitch)
            for target in targets
        ]
        clip_status = "neural-tts" if args.backend == "edge" else "prototype-tts"
    update_catalog(targets, clip_status)
    update_string_config(targets)
    write_manifest(targets, clips, args.backend, voice_name, args.rate, args.volume, args.pitch, voice_id)
    validate_outputs(targets)
    print(f"Generated {len(clips)} ARIA string audio mappings.")


if __name__ == "__main__":
    main()

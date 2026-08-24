#!/usr/bin/env python3
"""Generate the complete English/Persian ARIA match-command voice catalog."""

from __future__ import annotations

import argparse
import concurrent.futures
import datetime as dt
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import wave


ROOT = Path(__file__).resolve().parents[2]
SOURCE_GENERATOR = ROOT / "Tools/Audio/generate_string_audio_events.py"
PERSIAN_TEXT_CATALOG = (
    ROOT / "Assets/Game/Audio/GeneratedSource/aria_match_voice_fa_text_catalog_v0_1.json"
)
CATALOG_PATH = ROOT / "Assets/Game/Audio/Config/audio_event_catalog_v0_1.json"
MANIFEST_PATH = (
    ROOT / "Assets/Game/Audio/GeneratedSource/aria_match_voice_bilingual_manifest_v0_1.json"
)
PERSIAN_VOICE_ROOT = ROOT / "Assets/Game/Audio/Voice/ARIA/fa"
VOICE_MAP_PATH = (
    ROOT / "Assets/Game/Data/Narrative/FirstLaunch/first_launch_elevenlabs_voice_map.json"
)
DEFAULT_SECRET_PATH = Path("/private/tmp/warlinecapture-secrets/elevenlabs_api_key")
VOICE_ID = "Fi9tPTnEcbh3of7hOHC8"
VOICE_NAME = "Warline - ARIA Civic Relay"
MODEL = "eleven_v3"
OUTPUT_FORMAT = "mp3_44100_192"
API_ROOT = "https://api.elevenlabs.io"
RIGHTS_STATUS = "ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE"
CACHE_ROOT = Path("/private/tmp/warline-aria-match-voice-cache")
PERSIAN_LOCALE = "fa-IR"
PERSIAN_LANGUAGE = "fa"
ENGLISH_LOCALE = "en-US"
ENGLISH_LANGUAGE = "en"
BASE_SEED = 812047


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--api-key-file", type=Path, default=DEFAULT_SECRET_PATH)
    parser.add_argument("--jobs", type=int, default=3)
    parser.add_argument("--validate-only", action="store_true")
    return parser.parse_args()


def load_source_module():
    spec = importlib.util.spec_from_file_location("warline_string_audio_generator", SOURCE_GENERATOR)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load source generator: {SOURCE_GENERATOR}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def load_lines() -> list[dict]:
    module = load_source_module()
    english_targets = module.parse_string_targets()
    persian = json.loads(PERSIAN_TEXT_CATALOG.read_text(encoding="utf-8"))
    if persian.get("locale") != PERSIAN_LOCALE:
        raise RuntimeError(f"Persian text catalog locale must be {PERSIAN_LOCALE}.")
    if persian.get("voiceId") != VOICE_ID:
        raise RuntimeError("Persian text catalog does not use the canonical ARIA voice ID.")

    localized_by_key = {entry["key"]: entry for entry in persian.get("entries", [])}
    if len(localized_by_key) != len(persian.get("entries", [])):
        raise RuntimeError("Persian text catalog contains duplicate keys.")
    source_keys = {target.key for target in english_targets}
    if source_keys != set(localized_by_key):
        missing = sorted(source_keys - set(localized_by_key))
        extra = sorted(set(localized_by_key) - source_keys)
        raise RuntimeError(f"Persian text coverage mismatch. missing={missing} extra={extra}")

    lines: list[dict] = []
    for target in english_targets:
        localized = localized_by_key[target.key]
        if localized.get("eventId") != target.event_id:
            raise RuntimeError(f"Persian event ID mismatch for {target.key}.")
        if localized.get("englishText") != target.text:
            raise RuntimeError(f"Persian source text drifted for {target.key}.")
        persian_text = str(localized.get("text", "")).strip()
        if not persian_text:
            raise RuntimeError(f"Persian text is empty for {target.key}.")
        for placeholder in ("{0}", "{1}", "{2}"):
            if target.text.count(placeholder) != persian_text.count(placeholder):
                raise RuntimeError(f"Placeholder {placeholder} mismatch for {target.key}.")

        english_path = ROOT / target.clip_asset_path
        persian_stem = english_path.stem + "_fa.wav"
        persian_path = PERSIAN_VOICE_ROOT / persian_stem
        lines.append(
            {
                "key": target.key,
                "eventId": target.event_id,
                "englishText": target.text,
                "persianText": persian_text,
                "englishAssetPath": target.clip_asset_path,
                "persianAssetPath": persian_path.relative_to(ROOT).as_posix(),
            }
        )
    return lines


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


def validate_voice_map() -> None:
    voice_map = json.loads(VOICE_MAP_PATH.read_text(encoding="utf-8"))
    aria = next((voice for voice in voice_map.get("voices", []) if voice.get("speaker") == "ARIA"), None)
    if aria is None or aria.get("voiceId") != VOICE_ID or aria.get("name") != VOICE_NAME:
        raise RuntimeError("The permanent cast map no longer matches the canonical ARIA voice.")


def normalize_spoken_text(text: str, language: str) -> str:
    replacements = (
        {"{0}": "هدف", "{1}": "مقدار", "{2}": "جزئیات", " / ": " و "}
        if language == PERSIAN_LANGUAGE
        else {"{0}": "target", "{1}": "value", "{2}": "detail", " / ": " and "}
    )
    spoken = text
    for source, replacement in replacements.items():
        spoken = spoken.replace(source, replacement)
    return " ".join(spoken.replace(":", ".").split())


def cache_path(text: str, language: str) -> Path:
    digest = hashlib.sha256(
        f"{VOICE_ID}\n{MODEL}\n{language}\n{text}".encode("utf-8")
    ).hexdigest()
    return CACHE_ROOT / language / f"{digest}.mp3"


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


def ensure_cached_audio(api_key: str, line: dict, language: str, index: int) -> Path:
    text_key = "persianText" if language == PERSIAN_LANGUAGE else "englishText"
    spoken = normalize_spoken_text(line[text_key], language)
    destination = cache_path(spoken, language)
    if destination.exists() and destination.stat().st_size > 0:
        return destination
    destination.parent.mkdir(parents=True, exist_ok=True)
    seed_offset = int(hashlib.sha256(
        f"{language}:{line['key']}".encode("utf-8")).hexdigest()[:8], 16) % 100000
    payload = request_audio(api_key, spoken, language, BASE_SEED + seed_offset)
    temporary = destination.with_suffix(".mp3.tmp")
    temporary.write_bytes(payload)
    temporary.replace(destination)
    print(f"[AriaMatchVoice] cached {language} {index:03d} {line['key']}", flush=True)
    return destination


def convert_to_wav(source: Path, destination: Path) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="warline-aria-match-") as directory:
        output = Path(directory) / "output.wav"
        subprocess.run(
            [
                "ffmpeg", "-y", "-hide_banner", "-loglevel", "error",
                "-i", str(source), "-ar", "44100", "-ac", "1",
                "-c:a", "pcm_s16le", str(output),
            ],
            check=True,
        )
        temporary = destination.with_suffix(".wav.tmp")
        temporary.write_bytes(output.read_bytes())
        temporary.replace(destination)


def deterministic_guid(path: Path) -> str:
    relative = path.relative_to(ROOT).as_posix()
    return hashlib.md5(f"warline-aria-bilingual-v0.1:{relative}".encode("utf-8")).hexdigest()


def write_folder_meta(path: Path) -> None:
    meta = Path(f"{path}.meta")
    if meta.exists():
        return
    meta.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {deterministic_guid(path)}\n"
        "folderAsset: yes\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData: Canonical Persian ARIA match-command voice folder\n"
        "  assetBundleName:\n"
        "  assetBundleVariant:\n",
        encoding="utf-8",
    )


def write_audio_meta(path: Path) -> None:
    meta = Path(f"{path}.meta")
    if meta.exists():
        return
    meta.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {deterministic_guid(path)}\n"
        "AudioImporter:\n"
        "  externalObjects: {}\n"
        "  serializedVersion: 7\n"
        "  defaultSettings:\n"
        "    loadType: 0\n"
        "    preloadAudioData: 1\n"
        "    sampleRateSetting: 0\n"
        "    sampleRateOverride: 44100\n"
        "    compressionFormat: 1\n"
        "    quality: 1\n"
        "    conversionMode: 0\n"
        "  platformSettingOverrides: {}\n"
        "  forceToMono: 1\n"
        "  normalize: 1\n"
        "  preloadAudioData: 1\n"
        "  loadInBackground: 0\n"
        "  ambisonic: 0\n"
        "  3D: 1\n"
        "  userData: ElevenLabs canonical ARIA match-command voice\n"
        "  assetBundleName:\n"
        "  assetBundleVariant:\n",
        encoding="utf-8",
    )


def install_audio(lines: list[dict], cached: dict[tuple[str, str], Path]) -> None:
    PERSIAN_VOICE_ROOT.mkdir(parents=True, exist_ok=True)
    write_folder_meta(PERSIAN_VOICE_ROOT)
    for line in lines:
        english = ROOT / line["englishAssetPath"]
        persian = ROOT / line["persianAssetPath"]
        convert_to_wav(cached[(line["key"], ENGLISH_LANGUAGE)], english)
        convert_to_wav(cached[(line["key"], PERSIAN_LANGUAGE)], persian)
        write_audio_meta(english)
        write_audio_meta(persian)


def update_catalog(lines: list[dict]) -> None:
    by_event = {line["eventId"]: line for line in lines}
    catalog = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
    seen: set[str] = set()
    for event in catalog.get("events", []):
        event_id = event.get("eventId", "")
        if event_id not in by_event:
            continue
        line = by_event[event_id]
        event["clips"][0]["status"] = "elevenlabs-commercial"
        event["localizedClips"] = [
            {
                "localeCode": PERSIAN_LOCALE,
                "clips": [
                    {
                        "assetPath": line["persianAssetPath"],
                        "status": "elevenlabs-commercial",
                        "weight": 1,
                    }
                ],
            }
        ]
        seen.add(event_id)
    if seen != set(by_event):
        raise RuntimeError("Audio catalog does not contain the complete ARIA source event set.")
    catalog["status"] = "production-localized-voice"
    catalog["generatedBy"] = "Tools/Audio/generate_bilingual_aria_match_voice.py"
    catalog["implementationNote"] = (
        "ARIA match-command events contain deterministic English clips and complete fa-IR overrides."
    )
    CATALOG_PATH.write_text(
        json.dumps(catalog, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


def wave_record(line: dict, locale: str, text: str, asset_path: str) -> dict:
    path = ROOT / asset_path
    with wave.open(str(path), "rb") as audio:
        channels = audio.getnchannels()
        sample_rate = audio.getframerate()
        duration = audio.getnframes() / sample_rate
    if channels != 1 or sample_rate != 44100 or duration <= 0:
        raise RuntimeError(f"Invalid ARIA WAV: {asset_path}")
    return {
        "key": line["key"],
        "eventId": line["eventId"],
        "locale": locale,
        "text": text,
        "assetPath": asset_path,
        "durationSeconds": round(duration, 6),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
    }


def write_manifest(lines: list[dict]) -> None:
    clips: list[dict] = []
    for line in lines:
        clips.append(wave_record(line, ENGLISH_LOCALE, line["englishText"], line["englishAssetPath"]))
        clips.append(wave_record(line, PERSIAN_LOCALE, line["persianText"], line["persianAssetPath"]))
    manifest = {
        "schema": "WarlineCapture.AriaMatchVoiceBilingual.v0.1",
        "generatedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "provider": "ElevenLabs",
        "license": RIGHTS_STATUS,
        "runtimeNetworkTts": False,
        "model": MODEL,
        "voice": {"id": VOICE_ID, "name": VOICE_NAME},
        "eventCount": len(lines),
        "localeCount": 2,
        "clipCount": len(clips),
        "clips": clips,
    }
    MANIFEST_PATH.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


def validate_outputs(lines: list[dict]) -> None:
    catalog = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
    events = {event.get("eventId"): event for event in catalog.get("events", [])}
    for line in lines:
        event = events.get(line["eventId"])
        if event is None:
            raise RuntimeError(f"Missing catalog event: {line['eventId']}")
        localized = event.get("localizedClips", [])
        if len(localized) != 1 or localized[0].get("localeCode") != PERSIAN_LOCALE:
            raise RuntimeError(f"Missing Persian catalog override: {line['eventId']}")
        if localized[0]["clips"][0].get("assetPath") != line["persianAssetPath"]:
            raise RuntimeError(f"Persian clip path mismatch: {line['eventId']}")
        wave_record(line, ENGLISH_LOCALE, line["englishText"], line["englishAssetPath"])
        wave_record(line, PERSIAN_LOCALE, line["persianText"], line["persianAssetPath"])
    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    if manifest.get("voice", {}).get("id") != VOICE_ID:
        raise RuntimeError("Bilingual manifest does not use the canonical ARIA voice.")
    if manifest.get("eventCount") != len(lines) or manifest.get("clipCount") != len(lines) * 2:
        raise RuntimeError("Bilingual manifest coverage is incomplete.")
    records = {record.get("assetPath"): record for record in manifest.get("clips", [])}
    if len(records) != len(lines) * 2:
        raise RuntimeError("Bilingual manifest clip paths are incomplete or duplicated.")
    for line in lines:
        for asset_path, locale in (
            (line["englishAssetPath"], ENGLISH_LOCALE),
            (line["persianAssetPath"], PERSIAN_LOCALE),
        ):
            record = records.get(asset_path)
            if record is None or record.get("eventId") != line["eventId"] or record.get("locale") != locale:
                raise RuntimeError(f"Bilingual manifest record mismatch: {asset_path}")
            actual_hash = hashlib.sha256((ROOT / asset_path).read_bytes()).hexdigest()
            if record.get("sha256") != actual_hash:
                raise RuntimeError(f"Bilingual manifest hash mismatch: {asset_path}")


def main() -> None:
    args = parse_args()
    validate_voice_map()
    lines = load_lines()
    if args.validate_only:
        validate_outputs(lines)
        print(f"[AriaMatchVoice] result=Passed events={len(lines)} clips={len(lines) * 2} locales=2")
        return

    api_key = read_api_key(args.api_key_file)
    subscription = request_json(api_key, "/v1/user/subscription")
    if subscription.get("status") != "active" or subscription.get("tier") in {None, "free"}:
        raise RuntimeError("An active paid ElevenLabs subscription is required.")

    tasks = [
        (line, language, index)
        for index, line in enumerate(lines, 1)
        for language in (ENGLISH_LANGUAGE, PERSIAN_LANGUAGE)
    ]
    cached: dict[tuple[str, str], Path] = {}
    with concurrent.futures.ThreadPoolExecutor(max_workers=max(1, args.jobs)) as executor:
        futures = {
            executor.submit(ensure_cached_audio, api_key, line, language, index): (line, language)
            for line, language, index in tasks
        }
        for future in concurrent.futures.as_completed(futures):
            line, language = futures[future]
            cached[(line["key"], language)] = future.result()

    install_audio(lines, cached)
    update_catalog(lines)
    write_manifest(lines)
    validate_outputs(lines)
    print(f"[AriaMatchVoice] result=Passed events={len(lines)} clips={len(lines) * 2} locales=2")


if __name__ == "__main__":
    main()

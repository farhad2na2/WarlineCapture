#!/usr/bin/env python3
"""Create stable ElevenLabs voices and generate the FirstLaunch dialogue batch."""

from __future__ import annotations

import argparse
import base64
import datetime as dt
import hashlib
import json
import os
import shutil
import subprocess
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import wave
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
CATALOG_PATH = ROOT / "Assets/Game/Data/Narrative/FirstLaunch/first_launch_english_text_catalog.json"
VOICE_MAP_PATH = ROOT / "Assets/Game/Data/Narrative/FirstLaunch/first_launch_elevenlabs_voice_map.json"
VOICE_ROOT = ROOT / "Assets/Game/Audio/Narrative/FirstLaunch/Voice"
MANIFEST_PATH = ROOT / "Assets/Game/Audio/Narrative/FirstLaunch/first_launch_temp_voice_manifest.json"
DEFAULT_SECRET_PATH = Path(os.environ.get("LOCALAPPDATA", Path.home())) / "WarlineCapture/Secrets/elevenlabs_api_key.txt"

API_ROOT = "https://api.elevenlabs.io"
DESIGN_MODEL = "eleven_ttv_v3"
TTS_MODEL = "eleven_v3"
SOURCE_FORMAT = "mp3_44100_192"
RIGHTS_STATUS = "ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE"

MAX_DURATIONS = {
    "p02_radio": 7.45,
    "p03_radio": 8.45,
    "p04_dalia": 11.25,
    "p04_samira": 11.25,
    "p05_aria": 9.25,
    "p06_aria": 9.25,
    "p07_aria": 9.25,
    "p09_aria": 7.25,
    "p10_aria": 7.25,
    "p11_dalia": 8.25,
    "p12_samira": 8.25,
    "p13_aria": 7.25,
    "p14_commander": 7.25,
    "p15_dalia": 9.25,
    "p16_aria": 9.25,
    "p17_dalia": 7.25,
    "p18_aria": 9.25,
}

MAX_TIMING_COMPRESSION = {
    "p03_radio": 1.22,
}

VOICE_DESIGNS: dict[str, dict[str, Any]] = {
    "RADIO": {
        "name": "Warline - District Dispatch",
        "seed": 812003,
        "description": (
            "A middle-aged male emergency operations dispatcher speaking English with a subtle Levantine "
            "accent. Medium-low pitch, disciplined breath control, crisp consonants, brisk professional "
            "cadence, controlled urgency, credible military and civil-response radio operator, never theatrical."
        ),
        "preview": (
            "Joint Response Command, this is District Dispatch. Do you copy? The district relay is down, "
            "and the command channel has gone silent. Multiple incidents are being reported across Old Market."
        ),
        "selection": "shortest",
    },
    "DALIA": {
        "name": "Warline - Major Dalia Rahim",
        "seed": 812017,
        "description": (
            "A woman in her early forties speaking English with a natural, subtle Levantine accent. "
            "Grounded mezzo-alto voice, calm authority, concise military cadence, emotionally restrained "
            "under pressure, experienced field commander, warm enough to inspire trust without sounding soft."
        ),
        "preview": (
            "District Dispatch, this is Major Dalia Rahim with field command. We have located the convoy "
            "survivors and extraction is underway. Two squads remain operational and are standing by."
        ),
        "selection": "median",
    },
    "SAMIRA": {
        "name": "Warline - Engineer Samira Haddad",
        "seed": 812031,
        "description": (
            "A woman in her early thirties speaking English with a gentle Levantine accent. Clear intelligent "
            "delivery, medium pitch, practical civil engineer, compassionate but composed during emergencies, "
            "natural conversational rhythm, urgent when lives are at risk without melodrama."
        ),
        "preview": (
            "Field Command, this is Engineer Samira Haddad with Civil Infrastructure. Families and road crews "
            "are trapped beyond the clinic route. We need that corridor opened as soon as it is secure."
        ),
        "selection": "median",
    },
    "ARIA": {
        "name": "Warline - ARIA Civic Relay",
        "seed": 812047,
        "description": (
            "A distinctive feminine synthetic civic assistant speaking neutral international English. "
            "Human-adjacent and reassuring rather than robotic, precise diction, smooth medium pitch, measured "
            "tempo, calm tactical clarity, subtle intelligence and empathy, consistent enough for frequent RTS commands."
        ),
        "preview": (
            "Commander identity confirmed. I am Aria, the Civic Relay assistant. I will provide tactical "
            "support while you retain command authority. Select the rifle squad and move them into cover."
        ),
        "selection": "median",
    },
    "COMMANDER": {
        "name": "Warline - Commander",
        "seed": 812063,
        "description": (
            "A man in his early forties with a neutral international English accent. Grounded medium-low voice, "
            "quiet confidence, restrained authority, concise command cadence, thoughtful and humane, never a "
            "trailer announcer, credible as an experienced joint-response commander."
        ),
        "preview": (
            "Link the response teams. Secure the clinic corridor and confirm every target before engaging. "
            "Civilians remain behind the barriers, so keep the operation controlled and precise."
        ),
        "selection": "median",
    },
    "COMMANDER_FEMALE": {
        "name": "Warline - Commander Female",
        "seed": 812079,
        "description": (
            "A woman in her early forties with a neutral international English accent. Grounded mezzo-alto "
            "voice, quiet confidence, restrained authority, concise command cadence, thoughtful and humane, "
            "credible as an experienced joint-response commander, never theatrical or breathy."
        ),
        "preview": (
            "Link the response teams. Secure the clinic corridor and confirm every target before engaging. "
            "Civilians remain behind the barriers, so keep the operation controlled and precise."
        ),
        "selection": "median",
    },
    "COMMANDER_NEUTRAL": {
        "name": "Warline - Commander Neutral",
        "seed": 812093,
        "description": (
            "An adult androgynous command voice speaking neutral international English. Balanced middle pitch "
            "without strongly masculine or feminine markers, composed authority, concise tactical cadence, "
            "human and credible, suitable for a deliberately anonymous commander identity."
        ),
        "preview": (
            "Link the response teams. Secure the clinic corridor and confirm every target before engaging. "
            "Civilians remain behind the barriers, so keep the operation controlled and precise."
        ),
        "selection": "median",
    },
}

TRIM_START = "silenceremove=start_periods=1:start_duration=0.03:start_threshold=-55dB"
TRIM_END = "areverse,silenceremove=start_periods=1:start_duration=0.12:start_threshold=-55dB,areverse"

PROCESSING = {
    "dispatch-radio": (
        f"{TRIM_START},"
        "highpass=f=300,lowpass=f=3400,"
        "acompressor=threshold=0.12:ratio=3:attack=5:release=80:makeup=1.8,"
        f"loudnorm=I=-17:LRA=5:TP=-2,{TRIM_END}"
    ),
    "field-comms": (
        f"{TRIM_START},"
        "highpass=f=180,lowpass=f=5200,"
        "acompressor=threshold=0.15:ratio=2.2:attack=8:release=100:makeup=1.4,"
        f"loudnorm=I=-18:LRA=6:TP=-2,{TRIM_END}"
    ),
    "dalia-field-comms": (
        f"{TRIM_START},"
        "highpass=f=170,lowpass=f=5600,"
        "acompressor=threshold=0.18:ratio=1.6:attack=12:release=140:makeup=1.2,"
        f"loudnorm=I=-18:LRA=8:TP=-2,{TRIM_END}"
    ),
    "aria-clean": (
        f"{TRIM_START},"
        "highpass=f=90,lowpass=f=14000,"
        "acompressor=threshold=0.16:ratio=1.8:attack=10:release=120:makeup=1.2,"
        f"loudnorm=I=-18:LRA=6:TP=-2,{TRIM_END}"
    ),
    "commander-clean": (
        f"{TRIM_START},"
        f"highpass=f=75,lowpass=f=13500,loudnorm=I=-18:LRA=7:TP=-2,{TRIM_END}"
    ),
}

PERFORMANCE_DIRECTIONS = {
    "p04_dalia": {"tags": "[urgent] [speaking quickly]", "tempo": 1.04, "targetDuration": 7.4},
    "p11_dalia": {"tags": "[tense] [speaking quickly]", "tempo": 1.04, "targetDuration": 4.8},
    "p15_dalia": {"tags": "[alarmed] [controlled]", "tempo": 1.04, "targetDuration": 5.3},
    "p17_dalia": {"tags": "[strained] [resolute]", "tempo": 1.03, "targetDuration": 3.8},
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--api-key-file", type=Path, default=DEFAULT_SECRET_PATH)
    parser.add_argument("--catalog", type=Path, default=CATALOG_PATH)
    parser.add_argument("--voice-map", type=Path, default=VOICE_MAP_PATH)
    parser.add_argument("--voice-root", type=Path, default=VOICE_ROOT)
    parser.add_argument("--manifest", type=Path, default=MANIFEST_PATH)
    parser.add_argument("--language-code", default="")
    parser.add_argument("--ffmpeg", default="ffmpeg")
    parser.add_argument("--line-ids", nargs="*", default=[])
    parser.add_argument("--candidate-count", type=int, default=1)
    parser.add_argument("--commander-variants", action="store_true")
    parser.add_argument("--create-missing-voices", action="store_true")
    args = parser.parse_args()
    for attribute in ("api_key_file", "catalog", "voice_map", "voice_root", "manifest"):
        path = getattr(args, attribute)
        if not path.is_absolute():
            setattr(args, attribute, (ROOT / path).resolve())
    return args


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


def request_json(api_key: str, method: str, path: str, body: dict[str, Any] | None = None) -> dict[str, Any]:
    data = None if body is None else json.dumps(body).encode("utf-8")
    request = urllib.request.Request(
        f"{API_ROOT}{path}",
        data=data,
        method=method,
        headers={"xi-api-key": api_key, "Content-Type": "application/json", "Accept": "application/json"},
    )
    try:
        with urllib.request.urlopen(request, timeout=120) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"ElevenLabs HTTP {exc.code} for {path}: {detail}") from exc


def request_audio(
    api_key: str,
    voice_id: str,
    text: str,
    seed: int,
    language_code: str,
) -> tuple[bytes, dict[str, str]]:
    query = urllib.parse.urlencode({"output_format": SOURCE_FORMAT})
    body = {
        "text": text,
        "model_id": TTS_MODEL,
        "language_code": language_code,
        "seed": seed,
        "apply_text_normalization": "on",
    }
    request = urllib.request.Request(
        f"{API_ROOT}/v1/text-to-speech/{voice_id}?{query}",
        data=json.dumps(body).encode("utf-8"),
        method="POST",
        headers={"xi-api-key": api_key, "Content-Type": "application/json", "Accept": "audio/mpeg"},
    )
    try:
        with urllib.request.urlopen(request, timeout=180) as response:
            headers = {key.lower(): value for key, value in response.headers.items()}
            return response.read(), headers
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"ElevenLabs HTTP {exc.code} for voice {voice_id}: {detail}") from exc


def write_json_atomic(path: Path, value: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def subscription_snapshot(api_key: str) -> dict[str, Any]:
    data = request_json(api_key, "GET", "/v1/user/subscription")
    if data.get("status") != "active" or data.get("tier") in {None, "free"}:
        raise RuntimeError("An active paid ElevenLabs subscription is required for shipping dialogue generation.")
    return {
        "tier": data.get("tier"),
        "status": data.get("status"),
        "creditLimit": data.get("character_limit"),
        "creditsUsedBeforeGeneration": data.get("character_count"),
    }


def available_voices(api_key: str) -> list[dict[str, Any]]:
    data = request_json(api_key, "GET", "/v2/voices?page_size=100&include_total_count=true")
    return data.get("voices", [])


def load_voice_map(path: Path) -> dict[str, Any]:
    if path.exists():
        return json.loads(path.read_text(encoding="utf-8"))
    return {
        "schemaVersion": 1,
        "provider": "ElevenLabs",
        "designModel": DESIGN_MODEL,
        "createdAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "voices": [],
    }


def choose_preview(previews: list[dict[str, Any]], mode: str) -> dict[str, Any]:
    if not previews:
        raise RuntimeError("ElevenLabs Voice Design returned no previews.")
    ordered = sorted(previews, key=lambda preview: float(preview.get("duration_secs", 0.0)))
    return ordered[0] if mode == "shortest" else ordered[len(ordered) // 2]


def ensure_voice_map(api_key: str, path: Path, create_missing: bool) -> dict[str, Any]:
    voice_map = load_voice_map(path)
    records = {record["speaker"]: record for record in voice_map.get("voices", [])}
    owned_by_name = {voice.get("name"): voice for voice in available_voices(api_key) if voice.get("is_owner")}

    for speaker, design in VOICE_DESIGNS.items():
        if speaker in records:
            continue
        existing = owned_by_name.get(design["name"])
        if existing:
            created = existing
            selected_duration = None
        else:
            if not create_missing:
                raise RuntimeError(
                    f"Missing designed voice for {speaker}. Re-run with --create-missing-voices to create it."
                )
            preview_data = request_json(
                api_key,
                "POST",
                f"/v1/text-to-voice/design?{urllib.parse.urlencode({'output_format': SOURCE_FORMAT})}",
                {
                    "voice_description": design["description"],
                    "model_id": DESIGN_MODEL,
                    "text": design["preview"],
                    "loudness": 0.1,
                    "seed": design["seed"],
                    "guidance_scale": 4.0,
                },
            )
            selected = choose_preview(preview_data.get("previews", []), design["selection"])
            selected_duration = selected.get("duration_secs")
            created = request_json(
                api_key,
                "POST",
                "/v1/text-to-voice",
                {
                    "voice_name": design["name"],
                    "voice_description": design["description"],
                    "generated_voice_id": selected["generated_voice_id"],
                    "labels": {
                        "project": "WarlineCapture",
                        "speaker": speaker,
                        "language": "en",
                        "use_case": "video_game",
                    },
                },
            )
            print(f"[voice] created {speaker}: {created['name']} ({created['voice_id']})", flush=True)

        record = {
            "speaker": speaker,
            "voiceId": created["voice_id"],
            "name": created.get("name", design["name"]),
            "category": created.get("category", "generated"),
            "description": design["description"],
            "designSeed": design["seed"],
            "selectedPreviewDurationSeconds": selected_duration,
        }
        voice_map.setdefault("voices", []).append(record)
        records[speaker] = record
        write_json_atomic(path, voice_map)

    return voice_map


def processing_for(line_id: str, speaker: str) -> str:
    if line_id in {"p02_radio", "p03_radio"}:
        return "dispatch-radio"
    if speaker == "DALIA":
        return "dalia-field-comms"
    if speaker == "SAMIRA":
        return "field-comms"
    if speaker == "ARIA":
        return "aria-clean"
    return "commander-clean"


def spoken_text(text: str) -> str:
    return text.replace("JRC", "J. R. C.").replace("ARIA", "Aria")


def directed_text(line_id: str, text: str) -> str:
    direction = PERFORMANCE_DIRECTIONS.get(line_id)
    prefix = f"{direction['tags']} " if direction else ""
    return prefix + spoken_text(text)


def wav_duration(path: Path) -> float:
    with wave.open(str(path), "rb") as audio:
        if audio.getnchannels() != 1 or audio.getframerate() != 44100:
            raise RuntimeError(f"Unexpected WAV format for {path}: {audio.getnchannels()}ch {audio.getframerate()}Hz")
        return audio.getnframes() / audio.getframerate()


def convert_audio(ffmpeg: str, source: Path, destination: Path, filter_name: str, tempo: float = 1.0) -> None:
    audio_filter = PROCESSING[filter_name]
    if abs(tempo - 1.0) > 0.0001:
        audio_filter = f"{audio_filter},atempo={tempo:.6f}"
    subprocess.run(
        [
            ffmpeg,
            "-nostdin",
            "-hide_banner",
            "-loglevel",
            "error",
            "-y",
            "-i",
            str(source),
            "-af",
            audio_filter,
            "-ac",
            "1",
            "-ar",
            "44100",
            "-c:a",
            "pcm_s16le",
            str(destination),
        ],
        check=True,
    )


def generate_batch(args: argparse.Namespace, api_key: str, subscription: dict[str, Any], voice_map: dict[str, Any]) -> None:
    catalog = json.loads(args.catalog.read_text(encoding="utf-8"))
    language_code = args.language_code or catalog.get("elevenLabsLanguageCode") or "en"
    lines = catalog.get("lines", [])
    catalog_ids = {line["lineId"] for line in lines}
    if catalog_ids != set(MAX_DURATIONS):
        raise RuntimeError("FirstLaunch catalog line IDs do not match the timing contract.")
    if args.candidate_count < 1 or args.candidate_count > 5:
        raise RuntimeError("--candidate-count must be between 1 and 5.")

    selected_ids = set(args.line_ids) if args.line_ids else catalog_ids
    unknown_ids = selected_ids - catalog_ids
    if unknown_ids:
        raise RuntimeError(f"Unknown FirstLaunch line IDs: {', '.join(sorted(unknown_ids))}")
    selected_lines = [line for line in lines if line["lineId"] in selected_ids]

    voice_records = {record["speaker"]: record for record in voice_map["voices"]}
    ffmpeg = shutil.which(args.ffmpeg) or (str(Path(args.ffmpeg).resolve()) if Path(args.ffmpeg).exists() else None)
    if not ffmpeg:
        raise RuntimeError(f"Could not resolve ffmpeg executable: {args.ffmpeg}")

    existing_manifest: dict[str, Any] | None = None
    existing_clips: dict[str, dict[str, Any]] = {}
    if selected_ids != catalog_ids:
        if not args.manifest.exists():
            raise RuntimeError("Partial generation requires an existing complete voice manifest.")
        existing_manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
        existing_clips = {clip["clipId"]: clip for clip in existing_manifest.get("clips", [])}
        missing_existing = (catalog_ids - selected_ids) - set(existing_clips)
        if missing_existing:
            raise RuntimeError(f"Existing manifest is missing clips: {', '.join(sorted(missing_existing))}")

    generated_clips: dict[str, dict[str, Any]] = {}
    with tempfile.TemporaryDirectory(prefix="warline-first-launch-elevenlabs-") as temporary:
        work = Path(temporary)
        staged = work / "staged"
        staged.mkdir()
        for index, line in enumerate(selected_lines):
            line_id = line["lineId"]
            speaker = line["speaker"]
            speech_text = line.get("speechText", line["text"])
            voice = voice_records[speaker]
            processing = processing_for(line_id, speaker)
            maximum = MAX_DURATIONS[line_id]
            minimum = max(1.0, len(speech_text.split()) / 4.6)
            direction = PERFORMANCE_DIRECTIONS.get(line_id, {})
            base_tempo = float(direction.get("tempo", 1.0))
            candidates: list[dict[str, Any]] = []

            for candidate_index in range(1, args.candidate_count + 1):
                raw_mp3 = work / f"{line_id}_candidate_{candidate_index:02d}.mp3"
                output_wav = work / f"{line_id}_candidate_{candidate_index:02d}.wav"
                seed = 913000 + index * 10 + candidate_index
                generated, headers = request_audio(
                    api_key,
                    voice["voiceId"],
                    directed_text(line_id, speech_text),
                    seed,
                    language_code,
                )
                raw_mp3.write_bytes(generated)

                tempo = base_tempo
                convert_audio(ffmpeg, raw_mp3, output_wav, processing, tempo)
                duration = wav_duration(output_wav)
                if duration > maximum:
                    tempo *= duration / (maximum - 0.08)
                    if tempo > MAX_TIMING_COMPRESSION.get(line_id, 1.18):
                        raise RuntimeError(
                            f"{line_id} candidate {candidate_index} requires {tempo:.3f}x timing compression."
                        )
                    convert_audio(ffmpeg, raw_mp3, output_wav, processing, tempo)
                    duration = wav_duration(output_wav)
                if duration > maximum:
                    raise RuntimeError(
                        f"{line_id} candidate {candidate_index} is {duration:.3f}s, exceeding {maximum:.3f}s."
                    )
                if duration < minimum:
                    raise RuntimeError(
                        f"{line_id} candidate {candidate_index} is only {duration:.3f}s for "
                        f"{len(speech_text.split())} words; the line may be truncated."
                    )

                candidates.append(
                    {
                        "index": candidate_index,
                        "path": output_wav,
                        "duration": duration,
                        "tempo": tempo,
                        "seed": seed,
                        "requestId": headers.get("request-id") or headers.get("x-request-id"),
                        "characterCost": headers.get("character-cost"),
                    }
                )
                print(
                    f"[candidate] {line_id} #{candidate_index}: {duration:.3f}s "
                    f"{direction.get('tags', 'neutral')}",
                    flush=True,
                )

            target_duration = float(direction.get("targetDuration", candidates[0]["duration"]))
            selected = min(candidates, key=lambda candidate: abs(candidate["duration"] - target_duration))
            staged_wav = staged / f"{line_id}.wav"
            shutil.copy2(selected["path"], staged_wav)
            generated_at = dt.datetime.now(dt.timezone.utc).isoformat()
            generated_clips[line_id] = {
                    "clipId": line_id,
                    "speaker": speaker,
                    "voiceId": voice["voiceId"],
                    "voiceName": voice["name"],
                    "modelId": TTS_MODEL,
                    "sourceOutputFormat": SOURCE_FORMAT,
                    "processing": processing,
                    "performanceDirection": direction.get("tags"),
                    "durationSeconds": round(selected["duration"], 3),
                    "minimumDurationSeconds": round(minimum, 3),
                    "maximumDurationSeconds": maximum,
                    "targetDurationSeconds": target_duration,
                    "timingCompression": round(selected["tempo"], 4),
                    "selectedCandidate": selected["index"],
                    "candidateCount": len(candidates),
                    "candidateDurationsSeconds": [round(candidate["duration"], 3) for candidate in candidates],
                    "requestId": selected["requestId"],
                    "characterCost": selected["characterCost"],
                    "generationSeed": selected["seed"],
                    "generatedAtUtc": generated_at,
                    "sha256": hashlib.sha256(staged_wav.read_bytes()).hexdigest(),
                    "assetPath": (args.voice_root / f"{line_id}.wav").relative_to(ROOT).as_posix(),
                }
            print(
                f"[clip] selected {line_id} candidate {selected['index']}: "
                f"{selected['duration']:.3f}s {voice['name']} [{processing}]",
                flush=True,
            )

        args.voice_root.mkdir(parents=True, exist_ok=True)
        for line_id in selected_ids:
            shutil.copy2(staged / f"{line_id}.wav", args.voice_root / f"{line_id}.wav")

    all_clips = {**existing_clips, **generated_clips}
    ordered_clips = [all_clips[line["lineId"]] for line in lines]
    now = dt.datetime.now(dt.timezone.utc).isoformat()
    if existing_manifest is not None:
        manifest = existing_manifest
        manifest["lastUpdatedAtUtc"] = now
        manifest["subscriptionSnapshotLastUpdate"] = subscription
        manifest["radioTreatment"] = (
            "District Dispatch uses narrow-band command-radio processing; Dalia uses expressive field-comms "
            "processing; Samira uses lighter field-comms processing; ARIA and Commander remain clean."
        )
        manifest["clips"] = ordered_clips
    else:
        manifest = {
            "schemaVersion": 3,
            "generatedAtUtc": now,
            "rightsStatus": RIGHTS_STATUS,
            "provider": "ElevenLabs",
            "accountTierAtGeneration": subscription["tier"],
            "commercialUseEligible": True,
            "runtimeNetworkTts": False,
            "usage": "Imported local AudioClip assets for commercial game distribution",
            "sourceCatalog": args.catalog.relative_to(ROOT).as_posix(),
            "voiceMap": args.voice_map.relative_to(ROOT).as_posix(),
            "ttsModel": TTS_MODEL,
            "ttsLanguageCode": language_code,
            "sourceOutputFormat": SOURCE_FORMAT,
            "unityOutputFormat": "mono PCM s16le 44100 Hz",
            "radioTreatment": (
                "District Dispatch uses narrow-band command-radio processing; Dalia uses expressive field-comms "
                "processing; Samira uses lighter field-comms processing; ARIA and Commander remain clean."
            ),
            "subscriptionSnapshot": subscription,
            "clips": ordered_clips,
        }
    write_json_atomic(args.manifest, manifest)


def generate_commander_variants(
    args: argparse.Namespace,
    api_key: str,
    subscription: dict[str, Any],
    voice_map: dict[str, Any],
) -> None:
    if args.candidate_count < 1 or args.candidate_count > 5:
        raise RuntimeError("--candidate-count must be between 1 and 5.")
    catalog = json.loads(args.catalog.read_text(encoding="utf-8"))
    language_code = args.language_code or catalog.get("elevenLabsLanguageCode") or "en"
    source_line = next((line for line in catalog.get("lines", []) if line["lineId"] == "p14_commander"), None)
    if source_line is None:
        raise RuntimeError("The p14_commander source line is missing from the FirstLaunch catalog.")

    voice_records = {record["speaker"]: record for record in voice_map["voices"]}
    ffmpeg = shutil.which(args.ffmpeg) or (str(Path(args.ffmpeg).resolve()) if Path(args.ffmpeg).exists() else None)
    if not ffmpeg:
        raise RuntimeError(f"Could not resolve ffmpeg executable: {args.ffmpeg}")

    jobs = (
        ("p14_commander_female", "COMMANDER_FEMALE", "[urgent] [controlled]", 4.8),
        ("p14_commander_neutral", "COMMANDER_NEUTRAL", "[focused] [controlled]", 4.8),
    )
    maximum = MAX_DURATIONS["p14_commander"]
    minimum = max(1.0, len(source_line["text"].split()) / 4.6)
    variant_records: list[dict[str, Any]] = []

    with tempfile.TemporaryDirectory(prefix="warline-first-launch-commander-variants-") as temporary:
        work = Path(temporary)
        staged = work / "staged"
        staged.mkdir()
        for job_index, (clip_id, speaker, tags, target_duration) in enumerate(jobs):
            voice = voice_records[speaker]
            candidates: list[dict[str, Any]] = []
            for candidate_index in range(1, args.candidate_count + 1):
                raw_mp3 = work / f"{clip_id}_candidate_{candidate_index:02d}.mp3"
                output_wav = work / f"{clip_id}_candidate_{candidate_index:02d}.wav"
                seed = 923000 + job_index * 10 + candidate_index
                generated, headers = request_audio(
                    api_key,
                    voice["voiceId"],
                    f"{tags} {spoken_text(source_line['text'])}",
                    seed,
                    language_code,
                )
                raw_mp3.write_bytes(generated)
                tempo = 1.02
                convert_audio(ffmpeg, raw_mp3, output_wav, "commander-clean", tempo)
                duration = wav_duration(output_wav)
                if duration > maximum:
                    tempo *= duration / (maximum - 0.08)
                    if tempo > 1.18:
                        print(
                            f"[candidate-rejected] {clip_id} #{candidate_index}: "
                            f"requires excessive compression ({tempo:.3f}x)",
                            flush=True,
                        )
                        continue
                    convert_audio(ffmpeg, raw_mp3, output_wav, "commander-clean", tempo)
                    duration = wav_duration(output_wav)
                if duration < minimum or duration > maximum:
                    print(
                        f"[candidate-rejected] {clip_id} #{candidate_index}: duration {duration:.3f}s "
                        f"is outside {minimum:.3f}-{maximum:.3f}s",
                        flush=True,
                    )
                    continue
                candidates.append(
                    {
                        "index": candidate_index,
                        "path": output_wav,
                        "duration": duration,
                        "tempo": tempo,
                        "seed": seed,
                        "requestId": headers.get("request-id") or headers.get("x-request-id"),
                        "characterCost": headers.get("character-cost"),
                    }
                )
                print(f"[candidate] {clip_id} #{candidate_index}: {duration:.3f}s {tags}", flush=True)

            if not candidates:
                raise RuntimeError(f"No valid candidates were generated for {clip_id}.")

            selected = min(candidates, key=lambda candidate: abs(candidate["duration"] - target_duration))
            staged_wav = staged / f"{clip_id}.wav"
            shutil.copy2(selected["path"], staged_wav)
            variant_records.append(
                {
                    "clipId": clip_id,
                    "sourceLineId": "p14_commander",
                    "speaker": speaker,
                    "voiceId": voice["voiceId"],
                    "voiceName": voice["name"],
                    "modelId": TTS_MODEL,
                    "performanceDirection": tags,
                    "durationSeconds": round(selected["duration"], 3),
                    "minimumDurationSeconds": round(minimum, 3),
                    "maximumDurationSeconds": maximum,
                    "targetDurationSeconds": target_duration,
                    "selectedCandidate": selected["index"],
                    "candidateCount": len(candidates),
                    "candidateDurationsSeconds": [round(candidate["duration"], 3) for candidate in candidates],
                    "requestId": selected["requestId"],
                    "characterCost": selected["characterCost"],
                    "generationSeed": selected["seed"],
                    "sha256": hashlib.sha256(staged_wav.read_bytes()).hexdigest(),
                    "assetPath": (args.voice_root / f"{clip_id}.wav").relative_to(ROOT).as_posix(),
                }
            )
            print(
                f"[clip] selected {clip_id} candidate {selected['index']}: {selected['duration']:.3f}s",
                flush=True,
            )

        args.voice_root.mkdir(parents=True, exist_ok=True)
        for record in variant_records:
            shutil.copy2(staged / f"{record['clipId']}.wav", args.voice_root / f"{record['clipId']}.wav")

    manifest = json.loads(args.manifest.read_text(encoding="utf-8")) if args.manifest.exists() else {}
    manifest["lastUpdatedAtUtc"] = dt.datetime.now(dt.timezone.utc).isoformat()
    manifest["subscriptionSnapshotLastUpdate"] = subscription
    manifest["commanderVoiceSelection"] = {
        "femalePortraitIndices": [0, 2, 5],
        "malePortraitIndices": [1, 3, 4],
        "neutralPortraitIndices": [6],
    }
    manifest["voiceVariants"] = variant_records
    write_json_atomic(args.manifest, manifest)


def main() -> int:
    args = parse_args()
    api_key = read_api_key(args.api_key_file)
    subscription = subscription_snapshot(api_key)
    voice_map = ensure_voice_map(api_key, args.voice_map, args.create_missing_voices)
    if args.commander_variants:
        generate_commander_variants(args, api_key, subscription, voice_map)
        generated_count = 2
    else:
        generate_batch(args, api_key, subscription, voice_map)
        generated_count = len(args.line_ids) if args.line_ids else len(MAX_DURATIONS)
    print(f"[complete] generated {generated_count} licensed FirstLaunch voice clips", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

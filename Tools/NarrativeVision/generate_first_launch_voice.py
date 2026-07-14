"""Generate temporary FirstLaunch voice assets from the English text catalog."""

from __future__ import annotations

import argparse
import asyncio
import hashlib
import json
import shutil
import subprocess
import tempfile
import wave
from datetime import datetime, timezone
from pathlib import Path

try:
    import edge_tts
    import imageio_ffmpeg
except ImportError as exc:
    raise SystemExit(
        "Install edge-tts and imageio-ffmpeg, then expose them on PYTHONPATH."
    ) from exc


REPO_ROOT = Path(__file__).resolve().parents[2]
CATALOG_PATH = REPO_ROOT / "Assets/Game/Data/Narrative/FirstLaunch/first_launch_english_text_catalog.json"
VOICE_ROOT = REPO_ROOT / "Assets/Game/Audio/Narrative/FirstLaunch/Voice"
MANIFEST_PATH = REPO_ROOT / "Assets/Game/Audio/Narrative/FirstLaunch/first_launch_temp_voice_manifest.json"

VOICE_PROFILES = {
    "RADIO": {"voice": "en-US-DavisNeural", "rate": "+8%", "volume": "+0%", "pitch": "-2Hz"},
    "DALIA": {"voice": "en-US-MichelleNeural", "rate": "+2%", "volume": "+0%", "pitch": "-2Hz"},
    "SAMIRA": {"voice": "en-US-AvaNeural", "rate": "+0%", "volume": "+0%", "pitch": "+0Hz"},
    "ARIA": {"voice": "en-US-AriaNeural", "rate": "+0%", "volume": "+0%", "pitch": "+0Hz"},
    "COMMANDER": {"voice": "en-US-ChristopherNeural", "rate": "-2%", "volume": "+0%", "pitch": "-3Hz"},
}

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

DISPATCH_CLIPS = {"p02_radio", "p03_radio"}
FIELD_COMMS_SPEAKERS = {"DALIA", "SAMIRA"}
DISPATCH_FILTER = (
    "highpass=f=320,lowpass=f=3300,"
    "acompressor=threshold=0.12:ratio=3:attack=5:release=80:makeup=1.8,"
    "loudnorm=I=-17:LRA=5:TP=-2"
)
FIELD_COMMS_FILTER = (
    "highpass=f=220,lowpass=f=4300,"
    "acompressor=threshold=0.15:ratio=2.5:attack=8:release=100:makeup=1.5,"
    "loudnorm=I=-18:LRA=6:TP=-2"
)
CLEAN_FILTER = "loudnorm=I=-18:LRA=7:TP=-2"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--catalog", type=Path, default=CATALOG_PATH)
    parser.add_argument("--voice-root", type=Path, default=VOICE_ROOT)
    parser.add_argument("--manifest", type=Path, default=MANIFEST_PATH)
    return parser.parse_args()


def spoken_text(text: str) -> str:
    return text.replace("JRC", "J R C").replace("ARIA", "Aria")


def processing_for(line_id: str, speaker: str) -> tuple[str, str]:
    if line_id in DISPATCH_CLIPS:
        return DISPATCH_FILTER, "dispatch-radio"
    if speaker in FIELD_COMMS_SPEAKERS:
        return FIELD_COMMS_FILTER, "field-comms"
    return CLEAN_FILTER, "clean-dialogue"


def wav_duration(path: Path) -> float:
    with wave.open(str(path), "rb") as clip:
        return clip.getnframes() / clip.getframerate()


async def synthesize_line(line: dict, output: Path, ffmpeg: str, work: Path) -> dict:
    line_id = line["lineId"]
    speaker = line["speaker"]
    profile = VOICE_PROFILES[speaker]
    mp3_path = work / f"{line_id}.mp3"
    wav_path = work / f"{line_id}.wav"

    communicator = edge_tts.Communicate(
        spoken_text(line["text"]),
        profile["voice"],
        rate=profile["rate"],
        volume=profile["volume"],
        pitch=profile["pitch"],
    )
    await communicator.save(str(mp3_path))

    audio_filter, processing = processing_for(line_id, speaker)
    subprocess.run(
        [
            ffmpeg,
            "-nostdin",
            "-hide_banner",
            "-loglevel",
            "error",
            "-y",
            "-i",
            str(mp3_path),
            "-af",
            audio_filter,
            "-ac",
            "1",
            "-ar",
            "44100",
            "-c:a",
            "pcm_s16le",
            str(wav_path),
        ],
        check=True,
    )

    duration = wav_duration(wav_path)
    maximum = MAX_DURATIONS[line_id]
    if duration > maximum:
        raise RuntimeError(
            f"{line_id} is {duration:.2f}s, exceeding its {maximum:.2f}s dialogue window."
        )

    output.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(wav_path, output)
    return {
        "clipId": line_id,
        "speaker": speaker,
        "voice": profile["voice"],
        "rate": profile["rate"],
        "processing": processing,
        "durationSeconds": round(duration, 3),
        "sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
        "assetPath": output.relative_to(REPO_ROOT).as_posix(),
    }


async def generate(args: argparse.Namespace) -> None:
    catalog = json.loads(args.catalog.read_text(encoding="utf-8"))
    lines = catalog["lines"]
    actual_ids = {line["lineId"] for line in lines}
    if actual_ids != set(MAX_DURATIONS):
        raise RuntimeError("FirstLaunch catalog line IDs do not match the timing contract.")

    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    with tempfile.TemporaryDirectory(prefix="warline-first-launch-voice-") as temp:
        work = Path(temp)
        clips = []
        for line in lines:
            output = args.voice_root / f"{line['lineId']}.wav"
            clip = await synthesize_line(line, output, ffmpeg, work)
            clips.append(clip)
            print(
                f"{clip['clipId']}: {clip['durationSeconds']:.3f}s "
                f"{clip['voice']} [{clip['processing']}]"
            )

    manifest = {
        "schemaVersion": 2,
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "assetStatus": "TEMP_INTERNAL_ONLY_DISTRIBUTION_RIGHTS_UNVERIFIED",
        "provider": "Microsoft Edge neural voice",
        "usage": "Offline imported AudioClip assets for internal development and review only",
        "shippingApproved": False,
        "runtimeNetworkTts": False,
        "sourceCatalog": args.catalog.relative_to(REPO_ROOT).as_posix(),
        "radioTreatment": (
            "Davis dispatch voice with narrow-band command-radio processing; "
            "Dalia and Samira use lighter field-comms processing."
        ),
        "clips": clips,
    }
    args.manifest.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    asyncio.run(generate(parse_args()))

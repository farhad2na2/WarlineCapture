#!/usr/bin/env python3
"""Generate the licensed Eleven Music menu and match loops for Warline Capture."""

from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import asdict, dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
MUSIC_ROOT = ROOT / "Assets/Game/Audio/Music"
STAGING_ROOT = ROOT / "Assets/Game/Audio/GeneratedSource/ElevenLabsMusicRaw"
CATALOG_PATH = ROOT / "Assets/Game/Audio/Config/audio_event_catalog_v0_1.json"
MANIFEST_PATH = MUSIC_ROOT / "elevenlabs_menu_match_music_manifest.json"
DEFAULT_SECRET_PATH = Path(os.environ.get("LOCALAPPDATA", Path.home())) / "WarlineCapture/Secrets/elevenlabs_api_key.txt"
API_URL = "https://api.elevenlabs.io/v1/music"
MODEL_ID = "music_v2"
RIGHTS_STATUS = "ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE"


@dataclass(frozen=True)
class MusicSpec:
    event_id: str
    output_name: str
    title: str
    duration_ms: int
    target_rms_db: float
    prompt: str


SPECS = (
    MusicSpec(
        event_id="Music.Menu.Loop",
        output_name="music_menu_loop_01.wav",
        title="Quiet Authority",
        duration_ms=75_000,
        target_rms_db=-18.0,
        prompt=(
            "Instrumental seamless main menu underscore for a modern military strategy game set in a fictional "
            "Middle Eastern region. Quiet command-room authority, measured and intelligent rather than heroic. "
            "Warm low strings and cello, restrained oud motif, subtle ney breath texture, sparse frame drum detail, "
            "soft analog command-console atmosphere, 82 BPM, minor mode with restrained hopeful color. Evolving but "
            "unobtrusive, no obvious melody hook, no trailer impacts, no brass fanfare, no choir, no vocals, no chant. "
            "Leave generous space for UI sounds and radio dialogue. Constant loopable energy with no intro, no ending, "
            "no fade-in, and no fade-out; begin and end naturally in the middle of the arrangement."
        ),
    ),
    MusicSpec(
        event_id="Music.Match.CalmLoop",
        output_name="music_match_calm_loop_01.wav",
        title="Dustline Vigil",
        duration_ms=75_000,
        target_rms_db=-19.0,
        prompt=(
            "Instrumental seamless tactical battlefield underscore for a modern military RTS set in a fictional "
            "Middle Eastern region. Same sonic family as a restrained command-room menu theme, but more alert and "
            "spacious. Low strings, sparse oud phrases, muted irregular frame drum and hand percussion, distant bowed "
            "metal texture, subtle evolving tension, 94 BPM. Focused anticipation rather than constant combat; no "
            "trailer impacts, no pounding beat, no brass fanfare, no choir, no vocals, no chant. Preserve wide space "
            "for engines, weapons, alerts, and tactical radio. Constant loopable energy with no intro, no ending, no "
            "fade-in, and no fade-out; begin and end naturally in the middle of the arrangement."
        ),
    ),
)


def read_api_key(path: Path) -> str:
    if not path.exists():
        raise RuntimeError(f"ElevenLabs API key file not found: {path}")
    key = path.read_text(encoding="utf-8").strip()
    if not key:
        raise RuntimeError(f"ElevenLabs API key file is empty: {path}")
    return key


def request_music(api_key: str, spec: MusicSpec, destination: Path, timeout_seconds: int) -> str:
    payload = json.dumps(
        {
            "prompt": spec.prompt,
            "music_length_ms": spec.duration_ms,
            "model_id": MODEL_ID,
            "force_instrumental": True,
            "store_for_inpainting": True,
            "sign_with_c2pa": False,
        }
    ).encode("utf-8")
    url = f"{API_URL}?{urllib.parse.urlencode({'output_format': 'auto'})}"
    request = urllib.request.Request(
        url,
        data=payload,
        method="POST",
        headers={
            "Content-Type": "application/json",
            "Accept": "audio/mpeg",
            "xi-api-key": api_key,
        },
    )
    try:
        with urllib.request.urlopen(request, timeout=timeout_seconds) as response:
            destination.write_bytes(response.read())
            return response.headers.get("song-id", "")
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"Eleven Music HTTP {exc.code} for {spec.event_id}: {detail}") from exc
    except urllib.error.URLError as exc:
        raise RuntimeError(f"Eleven Music request failed for {spec.event_id}: {exc}") from exc


def update_catalog_status(event_ids: set[str]) -> None:
    catalog = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
    for event in catalog["events"]:
        if event.get("eventId") not in event_ids:
            continue
        for clip in event.get("clips", []):
            clip["status"] = "generated-elevenlabs-music"
    CATALOG_PATH.write_text(json.dumps(catalog, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--secret-path", type=Path, default=DEFAULT_SECRET_PATH)
    parser.add_argument("--timeout-seconds", type=int, default=600)
    parser.add_argument("--event-id", action="append", dest="event_ids")
    args = parser.parse_args()

    selected_ids = set(args.event_ids or (spec.event_id for spec in SPECS))
    selected = [spec for spec in SPECS if spec.event_id in selected_ids]
    missing = selected_ids - {spec.event_id for spec in selected}
    if missing:
        raise RuntimeError(f"Unknown music event id(s): {', '.join(sorted(missing))}")

    api_key = read_api_key(args.secret_path)
    STAGING_ROOT.mkdir(parents=True, exist_ok=True)
    generated = []
    for index, spec in enumerate(selected, start=1):
        destination = STAGING_ROOT / Path(spec.output_name).with_suffix(".mp3")
        print(f"[generate] {index}/{len(selected)} {spec.event_id}: {spec.title}", flush=True)
        song_id = request_music(api_key, spec, destination, args.timeout_seconds)
        generated.append(
            {
                "spec": asdict(spec),
                "stagedAssetPath": destination.relative_to(ROOT).as_posix(),
                "songId": song_id,
                "bytes": destination.stat().st_size,
            }
        )

    manifest = {
        "schemaVersion": "1.0",
        "generatedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "generator": "Tools/Audio/generate_elevenlabs_music.py",
        "provider": "ElevenLabs Eleven Music",
        "modelId": MODEL_ID,
        "rightsStatus": RIGHTS_STATUS,
        "commercialUseNote": "Generated while the project owner held a paid ElevenLabs Creator subscription.",
        "postProcessing": "Unity stereo decode, 3 second rotational crossfade, RMS normalization, PCM16 WAV.",
        "tracks": generated,
    }
    MANIFEST_PATH.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    update_catalog_status({spec.event_id for spec in selected})
    print(f"[done] Staged {len(selected)} track(s). Run Game/Audio/Convert ElevenLabs Menu And Match Music in Unity.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

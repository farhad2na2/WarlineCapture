#!/usr/bin/env python3
"""Arrange military menu and match music from GarageBand performances."""

from __future__ import annotations

import argparse
import json
import math
import shutil
import subprocess
import wave
from dataclasses import asdict, dataclass
from pathlib import Path

import numpy as np


ROOT = Path(__file__).resolve().parents[2]
MUSIC_ROOT = ROOT / "Assets" / "Game" / "Audio" / "Music"
MANIFEST_PATH = MUSIC_ROOT / "menu_match_music_generation_manifest.json"
APPLE_LOOPS_ROOT = Path("/Library/Audio/Apple Loops/Apple")
SAMPLE_RATE = 44_100


@dataclass(frozen=True)
class SourceSpec:
    source_id: str
    relative_path: str
    native_bars: int
    native_tempo: float
    role: str


@dataclass(frozen=True)
class LayerSpec:
    source_id: str
    start_bar: int
    bars: int
    gain_db: float
    pan: float = 0.0


@dataclass(frozen=True)
class TrackSpec:
    track_id: str
    output_name: str
    title: str
    tempo: float
    bars: int
    target_rms_db: float
    layers: tuple[LayerSpec, ...]


@dataclass(frozen=True)
class TrackMetrics:
    duration_seconds: float
    sample_rate: int
    channels: int
    rms_db: float
    peak_db: float
    crest_db: float
    silence_ratio: float
    window_rms_range_db: float
    max_window_rms_jump_db: float
    active_window_ratio: float
    seam_delta: float


SOURCES = {
    source.source_id: source
    for source in (
        SourceSpec(
            "persian_tar_100",
            "Apple Loops for GarageBand/Persian Market Tar 01.caf",
            2,
            100.0,
            "Persian melodic lead",
        ),
        SourceSpec(
            "persian_tar_85",
            "Apple Loops for GarageBand/Persian Market Tar 02.caf",
            2,
            85.0,
            "Persian melodic lead",
        ),
        SourceSpec(
            "oud_gold_a",
            "Apple Loops for GarageBand/Eastern Gold Oud 07.caf",
            2,
            85.0,
            "Oud answer phrase",
        ),
        SourceSpec(
            "oud_gold_b",
            "Apple Loops for GarageBand/Eastern Gold Oud 08.caf",
            2,
            85.0,
            "Oud answer phrase",
        ),
        SourceSpec(
            "oud_gold_turn",
            "Apple Loops for GarageBand/Eastern Gold Oud 12.caf",
            1,
            85.0,
            "Oud transition phrase",
        ),
        SourceSpec(
            "oud_storm_a",
            "Apple Loops for GarageBand/Eastern Storm Oud 01.caf",
            1,
            81.0,
            "Urgent oud motif",
        ),
        SourceSpec(
            "oud_storm_b",
            "Apple Loops for GarageBand/Eastern Storm Oud 02.caf",
            1,
            81.0,
            "Urgent oud motif",
        ),
        SourceSpec(
            "darbuka_a",
            "Apple Loops for GarageBand/Egyptian Nile Darbouka 01.caf",
            2,
            100.0,
            "Middle Eastern hand percussion",
        ),
        SourceSpec(
            "darbuka_b",
            "Apple Loops for GarageBand/Egyptian Nile Darbouka 02.caf",
            2,
            100.0,
            "Middle Eastern hand percussion",
        ),
        SourceSpec(
            "darbuka_c",
            "Apple Loops for GarageBand/Egyptian Nile Darbouka 03.caf",
            2,
            100.0,
            "Middle Eastern hand percussion",
        ),
        SourceSpec(
            "orchestra_strings",
            "Apple Loops for GarageBand/Orchestra Strings 27.caf",
            4,
            100.0,
            "Orchestral harmonic movement",
        ),
        SourceSpec(
            "command_brass",
            "01 Hip Hop/Rise Up Strings and Brass.caf",
            4,
            81.0,
            "Command-theme brass and strings",
        ),
        SourceSpec(
            "military_roll",
            "02 Electro House/Military Roll Topper.caf",
            2,
            128.0,
            "Restrained military snare pulse",
        ),
        SourceSpec(
            "marching_drum",
            "02 Electro House/Marching Drum Topper.caf",
            2,
            128.0,
            "Restrained marching pulse",
        ),
    )
}


def layer(source_id: str, start_bar: int, bars: int, gain_db: float, pan: float = 0.0) -> LayerSpec:
    return LayerSpec(source_id, start_bar, bars, gain_db, pan)


def repeated(source_id: str, starts: tuple[int, ...], bars: int, gain_db: float, pan: float = 0.0) -> tuple[LayerSpec, ...]:
    return tuple(layer(source_id, start, bars, gain_db, pan) for start in starts)


TRACKS = (
    TrackSpec(
        "Music.Menu.Loop",
        "music_menu_loop_01.wav",
        "Command at Dawn",
        81.0,
        24,
        -18.0,
        (
            *repeated("command_brass", (0, 4, 8, 12, 16, 20), 4, -10.5),
            *repeated("orchestra_strings", (0, 4, 8, 12, 16, 20), 4, -19.0),
            *repeated("military_roll", (4, 8, 12, 16, 20), 2, -15.0),
            *repeated("marching_drum", (6, 10, 14, 18, 22), 2, -17.0),
            *repeated("darbuka_a", (4, 8, 12, 16, 20), 2, -22.0),
            layer("persian_tar_85", 4, 2, -15.0, -0.12),
            layer("oud_gold_a", 10, 2, -16.0, 0.14),
            layer("persian_tar_85", 16, 2, -15.0, -0.10),
            layer("oud_gold_turn", 23, 1, -17.0, 0.10),
        ),
    ),
    TrackSpec(
        "Music.Match.CalmLoop",
        "music_match_calm_loop_01.wav",
        "Patrol Through Dust",
        95.0,
        24,
        -19.0,
        (
            *repeated("command_brass", (0, 4, 8, 12, 16, 20), 4, -11.0),
            *repeated("orchestra_strings", (0, 4, 8, 12, 16, 20), 4, -19.0),
            *repeated("military_roll", (0, 4, 8, 12, 16, 20), 2, -15.0),
            *repeated("marching_drum", (2, 6, 10, 14, 18, 22), 2, -16.0),
            *repeated("darbuka_a", (2, 6, 10, 14, 18, 22), 2, -20.0),
            layer("persian_tar_100", 4, 2, -15.0, -0.12),
            layer("oud_storm_b", 11, 1, -17.0, 0.15),
            layer("persian_tar_100", 16, 2, -15.0, -0.12),
        ),
    ),
    TrackSpec(
        "Music.Match.CombatLoop",
        "music_match_combat_loop_01.wav",
        "Armored Advance",
        100.0,
        24,
        -17.5,
        (
            *repeated("command_brass", (0, 4, 8, 12, 16, 20), 4, -8.5),
            *repeated("orchestra_strings", (0, 4, 8, 12, 16, 20), 4, -16.0),
            *repeated("military_roll", (0, 4, 8, 12, 16, 20), 2, -10.5),
            *repeated("marching_drum", (2, 6, 10, 14, 18, 22), 2, -11.5),
            *repeated("darbuka_a", (0, 6, 12, 18), 2, -15.0, -0.08),
            *repeated("darbuka_b", (2, 8, 14, 20), 2, -15.0, 0.08),
            *repeated("darbuka_c", (4, 10, 16, 22), 2, -14.5),
            *repeated("persian_tar_100", (4, 12, 20), 2, -14.0, -0.14),
            *repeated("oud_storm_b", (7, 15, 23), 1, -16.0, 0.14),
        ),
    ),
)


def db_to_amplitude(db: float) -> float:
    return 10.0 ** (db / 20.0)


def amplitude_db(value: float) -> float:
    return -120.0 if value <= 0.0 else 20.0 * math.log10(value)


def probe_duration(ffprobe: str, source: Path) -> float:
    result = subprocess.run(
        [ffprobe, "-v", "error", "-show_entries", "format=duration", "-of", "default=nw=1:nk=1", str(source)],
        check=True,
        capture_output=True,
        text=True,
    )
    return float(result.stdout.strip())


def decode_phrase(ffmpeg: str, ffprobe: str, source: Path, target_seconds: float) -> np.ndarray:
    source_seconds = probe_duration(ffprobe, source)
    tempo_factor = source_seconds / target_seconds
    if not 0.5 <= tempo_factor <= 2.0:
        raise ValueError(f"Unsupported tempo conversion {tempo_factor:.3f} for {source}")
    result = subprocess.run(
        [
            ffmpeg,
            "-v",
            "error",
            "-i",
            str(source),
            "-af",
            f"atempo={tempo_factor:.9f}",
            "-ar",
            str(SAMPLE_RATE),
            "-ac",
            "2",
            "-f",
            "f32le",
            "pipe:1",
        ],
        check=True,
        capture_output=True,
    )
    samples = np.frombuffer(result.stdout, dtype="<f4").astype(np.float64).reshape(-1, 2)
    target_frames = int(round(target_seconds * SAMPLE_RATE))
    if samples.shape[0] < target_frames:
        samples = np.pad(samples, ((0, target_frames - samples.shape[0]), (0, 0)))
    else:
        samples = samples[:target_frames]
    samples -= np.mean(samples, axis=0, keepdims=True)
    rms = math.sqrt(float(np.mean(samples * samples)))
    if rms > 1e-8:
        samples *= db_to_amplitude(-18.0) / rms
    return samples


def apply_pan(samples: np.ndarray, pan: float) -> np.ndarray:
    pan = max(-1.0, min(1.0, pan))
    result = samples.copy()
    result[:, 0] *= math.cos((pan + 1.0) * math.pi / 4.0) * math.sqrt(2.0)
    result[:, 1] *= math.sin((pan + 1.0) * math.pi / 4.0) * math.sqrt(2.0)
    return result


def render_track(spec: TrackSpec, ffmpeg: str, ffprobe: str) -> np.ndarray:
    bar_seconds = 240.0 / spec.tempo
    total_frames = int(round(spec.bars * bar_seconds * SAMPLE_RATE))
    mix = np.zeros((total_frames, 2), dtype=np.float64)
    cache: dict[tuple[str, int], np.ndarray] = {}

    for item in spec.layers:
        source = SOURCES[item.source_id]
        source_path = APPLE_LOOPS_ROOT / source.relative_path
        if not source_path.is_file():
            raise FileNotFoundError(f"Required GarageBand performance is missing: {source_path}")
        phrase_frames = int(round(item.bars * bar_seconds * SAMPLE_RATE))
        cache_key = (item.source_id, phrase_frames)
        if cache_key not in cache:
            cache[cache_key] = decode_phrase(ffmpeg, ffprobe, source_path, phrase_frames / SAMPLE_RATE)
        phrase = apply_pan(cache[cache_key], item.pan) * db_to_amplitude(item.gain_db)
        start = int(round(item.start_bar * bar_seconds * SAMPLE_RATE))
        end = min(total_frames, start + phrase.shape[0])
        mix[start:end] += phrase[: end - start]

    # Preserve transients while keeping music safely below full scale.
    mix -= np.mean(mix, axis=0, keepdims=True)
    mix = np.tanh(mix * 1.15) / math.tanh(1.15)
    rms = math.sqrt(float(np.mean(mix * mix)))
    mix *= db_to_amplitude(spec.target_rms_db) / max(rms, 1e-9)
    peak = float(np.max(np.abs(mix)))
    if peak > db_to_amplitude(-2.0):
        mix *= db_to_amplitude(-2.0) / peak

    # A short boundary fade prevents clicks without creating an audible pause.
    seam_frames = int(0.025 * SAMPLE_RATE)
    fade = np.sin(np.linspace(0.0, math.pi / 2.0, seam_frames)) ** 2
    mix[:seam_frames] *= fade[:, None]
    mix[-seam_frames:] *= fade[::-1, None]
    return mix


def write_wav(path: Path, samples: np.ndarray) -> None:
    pcm = np.clip(samples * 32767.0, -32768.0, 32767.0).astype("<i2")
    with wave.open(str(path), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(pcm.tobytes())


def analyze(samples: np.ndarray) -> TrackMetrics:
    absolute = np.abs(samples)
    rms = math.sqrt(float(np.mean(samples * samples)))
    peak = float(np.max(absolute))
    window_size = int(SAMPLE_RATE * 0.25)
    levels = []
    for start in range(0, samples.shape[0], window_size):
        window = samples[start : start + window_size]
        levels.append(amplitude_db(math.sqrt(float(np.mean(window * window)))))
    sorted_levels = sorted(levels)
    low = sorted_levels[int(len(sorted_levels) * 0.05)]
    high = sorted_levels[min(len(sorted_levels) - 1, int(len(sorted_levels) * 0.95))]
    jumps = [abs(levels[index] - levels[index - 1]) for index in range(1, len(levels))]
    active_threshold = amplitude_db(rms) - 12.0
    return TrackMetrics(
        duration_seconds=samples.shape[0] / SAMPLE_RATE,
        sample_rate=SAMPLE_RATE,
        channels=2,
        rms_db=amplitude_db(rms),
        peak_db=amplitude_db(peak),
        crest_db=amplitude_db(peak) - amplitude_db(rms),
        silence_ratio=float(np.mean(absolute < db_to_amplitude(-60.0))),
        window_rms_range_db=high - low,
        max_window_rms_jump_db=max(jumps, default=0.0),
        active_window_ratio=float(np.mean(np.asarray(levels) > active_threshold)),
        seam_delta=float(np.max(np.abs(samples[0] - samples[-1]))),
    )


def validate(metrics: TrackMetrics) -> list[str]:
    failures = []
    if metrics.channels != 2:
        failures.append("not_stereo")
    if metrics.duration_seconds < 45.0:
        failures.append("short_loop")
    if metrics.crest_db < 6.0 or metrics.crest_db > 20.0:
        failures.append(f"crest={metrics.crest_db:.2f}dB")
    if metrics.window_rms_range_db < 3.0:
        failures.append(f"insufficient_dynamics={metrics.window_rms_range_db:.2f}dB")
    if metrics.active_window_ratio < 0.75:
        failures.append(f"inactive_windows={metrics.active_window_ratio:.2%}")
    if metrics.seam_delta > 0.005:
        failures.append(f"seam_delta={metrics.seam_delta:.4f}")
    return failures


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-root", type=Path, default=MUSIC_ROOT)
    parser.add_argument("--manifest", type=Path, default=MANIFEST_PATH)
    args = parser.parse_args()
    args.output_root.mkdir(parents=True, exist_ok=True)

    ffmpeg = shutil.which("ffmpeg")
    ffprobe = shutil.which("ffprobe")
    if not ffmpeg or not ffprobe:
        raise RuntimeError("ffmpeg and ffprobe are required to render GarageBand performances")

    manifest = {
        "schema": "WarlineCapture.ArrangedBackgroundMusic.v2",
        "generator": "Tools/Audio/generate_background_music.py",
        "license": {
            "source": "GarageBand royalty-free Apple Loops",
            "terms": "https://support.apple.com/102034",
            "restriction": "Rendered original arrangements may ship; source loops must not be redistributed standalone.",
        },
        "intent": "Military tactical score led by marching cadence, brass, and orchestral movement, with restrained Middle Eastern regional color.",
        "sources": [asdict(source) for source in SOURCES.values()],
        "tracks": [],
    }
    has_failures = False
    for spec in TRACKS:
        samples = render_track(spec, ffmpeg, ffprobe)
        metrics = analyze(samples)
        failures = validate(metrics)
        destination = args.output_root / spec.output_name
        write_wav(destination, samples)
        try:
            asset_path = destination.relative_to(ROOT).as_posix()
        except ValueError:
            asset_path = str(destination)
        manifest["tracks"].append(
            {
                "spec": asdict(spec),
                "assetPath": asset_path,
                "metrics": asdict(metrics),
                "passed": not failures,
                "failures": failures,
            }
        )
        has_failures |= bool(failures)
        print(
            f"[{spec.track_id}] {spec.title}: duration={metrics.duration_seconds:.3f}s "
            f"rms={metrics.rms_db:.2f}dB peak={metrics.peak_db:.2f}dB "
            f"crest={metrics.crest_db:.2f}dB dynamics={metrics.window_rms_range_db:.2f}dB "
            f"active={metrics.active_window_ratio:.1%} failures={failures or 'none'}"
        )

    args.manifest.write_text(json.dumps(manifest, indent=2) + "\n")
    return 2 if has_failures else 0


if __name__ == "__main__":
    raise SystemExit(main())

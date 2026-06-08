#!/usr/bin/env python3
"""Generate WarlineCapture first-pass audio assets.

The output is intentionally procedural: real WAV files that can be imported by
Unity immediately, with deterministic synthesis so the pack can be regenerated.
These are vertical-slice/prototype assets matching the filenames in
Design/Audio_Design_Guidelines.md.
"""

from __future__ import annotations

import math
import random
import struct
import wave
import hashlib
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Game" / "Audio"
SR = 48000
TAU = math.tau


def clamp(v: float, lo: float = -1.0, hi: float = 1.0) -> float:
    return max(lo, min(hi, v))


def env(t: float, duration: float, attack: float = 0.01, release: float = 0.08) -> float:
    if duration <= 0:
        return 0.0
    if t < attack:
        return t / max(attack, 0.0001)
    if t > duration - release:
        return max(0.0, (duration - t) / max(release, 0.0001))
    return 1.0


def sine(freq: float, t: float) -> float:
    return math.sin(TAU * freq * t)


def square(freq: float, t: float) -> float:
    return 1.0 if sine(freq, t) >= 0 else -1.0


def saw(freq: float, t: float) -> float:
    return 2.0 * ((freq * t) % 1.0) - 1.0


def noise(rng: random.Random) -> float:
    return rng.uniform(-1.0, 1.0)


def write_wav(path: Path, samples: list[float], channels: int = 1) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    peak = max((abs(s) for s in samples), default=1.0)
    gain = 0.92 / peak if peak > 0.92 else 1.0
    with wave.open(str(path), "wb") as wf:
        wf.setnchannels(channels)
        wf.setsampwidth(2)
        wf.setframerate(SR)
        frames = bytearray()
        for s in samples:
            frames.extend(struct.pack("<h", int(clamp(s * gain) * 32767)))
        wf.writeframes(frames)


def render_mono(duration: float, fn) -> list[float]:
    n = int(duration * SR)
    rng = random.Random(1000 + n)
    return [clamp(fn(i / SR, rng)) for i in range(n)]


def render_stereo(duration: float, fn) -> list[float]:
    n = int(duration * SR)
    rng = random.Random(2000 + n)
    out: list[float] = []
    for i in range(n):
        l, r = fn(i / SR, rng)
        out.extend((clamp(l), clamp(r)))
    return out


def click(kind: str, duration: float = 0.14) -> list[float]:
    profiles = {
        "primary": (920, 1480, 0.16, 0.10),
        "secondary": (720, 1120, 0.12, 0.08),
        "negative": (360, 210, 0.14, 0.10),
        "disabled": (190, 120, 0.18, 0.12),
        "tab": (820, 1280, 0.10, 0.07),
        "toggle_on": (650, 1280, 0.12, 0.08),
        "toggle_off": (720, 320, 0.12, 0.08),
        "screen": (520, 980, 0.14, 0.12),
        "drawer": (280, 860, 0.16, 0.15),
    }
    f0, f1, tonal, noisy = profiles.get(kind, profiles["secondary"])

    def fn(t: float, rng: random.Random) -> float:
        p = min(1.0, t / duration)
        freq = f0 + (f1 - f0) * p
        impulse = math.exp(-t * 48.0) * noise(rng) * noisy
        tone = sine(freq, t) * math.exp(-t * 18.0) * tonal
        tick = sine(freq * 2.1, t) * math.exp(-t * 70.0) * 0.05
        return (tone + tick + impulse) * env(t, duration, 0.002, 0.03)

    return render_mono(duration, fn)


def sweep(name: str, duration: float, start: float, end: float, urgent: bool = False) -> list[float]:
    def fn(t: float, rng: random.Random) -> float:
        p = t / duration
        freq = start + (end - start) * p
        mod = 1.0 + 0.02 * sine(9, t)
        body = sine(freq * mod, t) * 0.26
        grit = noise(rng) * math.exp(-t * 8.0) * (0.08 if urgent else 0.035)
        pulse = square(6 if urgent else 3, t) * 0.04 if urgent else 0.0
        return (body + grit + pulse) * env(t, duration, 0.01, 0.09)

    return render_mono(duration, fn)


def impact(kind: str, duration: float = 0.65) -> list[float]:
    heavy = kind in {"large", "cannon", "breach", "destroyed"}

    def fn(t: float, rng: random.Random) -> float:
        low = sine(58 if heavy else 96, t) * math.exp(-t * (4.0 if heavy else 7.0)) * (0.55 if heavy else 0.22)
        mid = sine(160 + 80 * math.exp(-t * 6), t) * math.exp(-t * 9.0) * 0.18
        burst = noise(rng) * math.exp(-t * (7.0 if heavy else 14.0)) * (0.45 if heavy else 0.18)
        crack = sine(1800, t) * math.exp(-t * 70.0) * (0.15 if heavy else 0.08)
        return (low + mid + burst + crack) * env(t, duration, 0.001, 0.12)

    return render_mono(duration, fn)


def radio(kind: str, duration: float = 0.28) -> list[float]:
    base = {
        "move": (880, 1240),
        "attack": (520, 980),
        "invalid": (220, 150),
        "select": (760, 1140),
        "air": (1150, 1600),
        "vehicle": (420, 720),
        "infantry": (680, 980),
    }.get(kind, (700, 1000))

    def fn(t: float, rng: random.Random) -> float:
        p = t / duration
        chirp = sine(base[0] + (base[1] - base[0]) * p, t) * 0.22
        static = noise(rng) * 0.035
        gate = 1.0 if (t * 38) % 1.0 < 0.55 else 0.35
        return (chirp + static) * gate * env(t, duration, 0.006, 0.07)

    return render_mono(duration, fn)


def alert(kind: str, duration: float = 1.2) -> list[float]:
    freq = {
        "ground": 420,
        "air": 840,
        "base": 260,
        "timer": 620,
        "unit": 560,
        "destroyed": 300,
    }.get(kind, 520)

    def fn(t: float, rng: random.Random) -> float:
        pulse_rate = 5.0 if kind != "timer" else 7.0
        pulse = 0.55 + 0.45 * max(0.0, sine(pulse_rate, t))
        tone = (sine(freq, t) * 0.34 + sine(freq * 1.5, t) * 0.12) * pulse
        scan = sine(freq * (2.0 + 0.25 * sine(2, t)), t) * 0.06
        static = noise(rng) * 0.04
        return (tone + scan + static) * env(t, duration, 0.015, 0.18)

    return render_mono(duration, fn)


def short_positive(duration: float = 0.75) -> list[float]:
    def fn(t: float, rng: random.Random) -> float:
        notes = [523.25, 659.25, 783.99]
        s = 0.0
        for idx, f in enumerate(notes):
            local = t - idx * 0.13
            if local >= 0:
                s += sine(f, local) * math.exp(-local * 6.0) * 0.16
        s += noise(rng) * math.exp(-t * 9.0) * 0.025
        return s * env(t, duration, 0.01, 0.14)

    return render_mono(duration, fn)


def loop_music(mood: str, duration: float = 24.0) -> list[float]:
    root = {
        "menu": 110.0,
        "briefing": 98.0,
        "battle1": 82.0,
        "battle2": 92.0,
        "victory": 130.81,
        "defeat": 73.42,
    }.get(mood, 100.0)
    intensity = {"battle2": 1.0, "battle1": 0.65, "briefing": 0.45}.get(mood, 0.35)

    def fn(t: float, rng: random.Random) -> tuple[float, float]:
        loop_env = math.sin(math.pi * min(1.0, t / 1.0)) if t < 1.0 else 1.0
        if t > duration - 1.0:
            loop_env *= math.sin(math.pi * max(0.0, (duration - t) / 1.0))
        beat = 1.0 if (t * (1.5 + intensity)) % 1.0 < 0.08 else 0.0
        bass = sine(root, t) * 0.12 + sine(root / 2, t) * 0.10
        pulse = sine(root * 2, t) * (0.05 + 0.05 * intensity) * (0.7 + 0.3 * square(2, t))
        perc = noise(rng) * math.exp(-((t * (2 + intensity * 3)) % 1.0) * 24.0) * 0.11 * intensity
        pad = (sine(root * 1.5, t) + sine(root * 2.25, t + 0.2)) * 0.035
        if mood == "victory":
            pad += sine(root * 3, t) * 0.05
        if mood == "defeat":
            bass *= 1.15
            pulse *= 0.35
        s = (bass + pulse + perc + pad + beat * noise(rng) * 0.08) * loop_env
        pan = sine(0.07, t) * 0.12
        return s * (1 - pan), s * (1 + pan)

    return render_stereo(duration, fn)


def ambience(kind: str, duration: float = 24.0) -> list[float]:
    def fn(t: float, rng: random.Random) -> tuple[float, float]:
        wind = noise(rng) * 0.045
        hum = sine(55 if kind == "battle" else 70, t) * 0.035
        radio_bleep = 0.0
        if int(t * 10) % 97 == 0:
            radio_bleep = sine(1200, t) * 0.035
        distant = sine(23, t) * 0.025 if kind == "battle" else sine(31, t) * 0.018
        s = (wind + hum + radio_bleep + distant) * env(t, duration, 0.8, 0.8)
        pan = sine(0.05, t) * 0.2
        return s * (1 - pan), s * (1 + pan)

    return render_stereo(duration, fn)


def voice_placeholder(topic: str, duration: float = 2.2) -> list[float]:
    """Non-language tutorial placeholder: radio-like cadence for wiring tests."""

    def fn(t: float, rng: random.Random) -> float:
        syllable = 1.0 if (t * 5.5) % 1.0 < 0.45 else 0.25
        carrier = sine(185 + 18 * sine(3.1, t), t) * 0.12
        formant = sine(740 + 70 * sine(2.7, t), t) * 0.05
        static = noise(rng) * 0.018
        return (carrier + formant + static) * syllable * env(t, duration, 0.03, 0.16)

    return render_mono(duration, fn)


def write_meta(path: Path, category: str) -> None:
    guid = hashlib.md5(str(path.relative_to(ROOT)).encode("utf-8")).hexdigest()
    meta = f"""fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    loadType: 0
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
  userData: WarlineCapture generated {category} audio
  assetBundleName: 
  assetBundleVariant: 
"""
    path.with_suffix(path.suffix + ".meta").write_text(meta)


def save(rel: str, samples: list[float], channels: int = 1, category: str = "sfx") -> None:
    path = OUT / rel
    write_wav(path, samples, channels)
    write_meta(path, category)


def main() -> None:
    # UI
    ui = "UI"
    save(f"{ui}/ui_button_primary_click_01.wav", click("primary"), category="ui")
    save(f"{ui}/ui_button_secondary_click_01.wav", click("secondary"), category="ui")
    save(f"{ui}/ui_button_negative_click_01.wav", click("negative"), category="ui")
    save(f"{ui}/ui_button_disabled_tap_01.wav", click("disabled", 0.16), category="ui")
    save(f"{ui}/ui_toggle_on_01.wav", click("toggle_on", 0.13), category="ui")
    save(f"{ui}/ui_toggle_off_01.wav", click("toggle_off", 0.13), category="ui")
    save(f"{ui}/ui_slider_tick_01.wav", click("tab", 0.045), category="ui")
    save(f"{ui}/ui_tab_select_01.wav", click("tab", 0.12), category="ui")
    save(f"{ui}/ui_dropdown_open_01.wav", sweep("dropdown", 0.18, 420, 760), category="ui")
    save(f"{ui}/ui_dropdown_select_01.wav", click("secondary", 0.11), category="ui")
    save(f"{ui}/ui_card_select_01.wav", sweep("card", 0.20, 520, 980), category="ui")
    save(f"{ui}/ui_popup_open_01.wav", sweep("popup_open", 0.30, 240, 780), category="ui")
    save(f"{ui}/ui_popup_close_01.wav", sweep("popup_close", 0.24, 760, 260), category="ui")
    save(f"{ui}/ui_screen_forward_01.wav", sweep("screen_forward", 0.42, 360, 920), category="ui")
    save(f"{ui}/ui_screen_back_01.wav", sweep("screen_back", 0.36, 820, 300), category="ui")
    save(f"{ui}/ui_drawer_open_01.wav", click("drawer", 0.28), category="ui")
    save(f"{ui}/ui_drawer_close_01.wav", sweep("drawer_close", 0.22, 700, 240), category="ui")

    # Gameplay
    gp = "Gameplay"
    save(f"{gp}/game_unit_select_infantry_01.wav", radio("infantry", 0.26), category="gameplay")
    save(f"{gp}/game_unit_select_vehicle_01.wav", radio("vehicle", 0.30), category="gameplay")
    save(f"{gp}/game_unit_select_air_01.wav", radio("air", 0.32), category="gameplay")
    save(f"{gp}/game_command_move_confirm_01.wav", radio("move", 0.25), category="gameplay")
    save(f"{gp}/game_command_attack_confirm_01.wav", radio("attack", 0.28), category="gameplay")
    save(f"{gp}/game_command_invalid_01.wav", radio("invalid", 0.32), category="gameplay")
    save(f"{gp}/game_build_place_confirm_01.wav", impact("cannon", 0.55), category="gameplay")
    save(f"{gp}/game_build_invalid_placement_01.wav", sweep("invalid_build", 0.30, 260, 120, True), category="gameplay")
    save(f"{gp}/game_production_queue_unit_01.wav", click("primary", 0.22), category="gameplay")
    save(f"{gp}/game_production_complete_01.wav", short_positive(0.85), category="gameplay")
    save(f"{gp}/game_objective_update_01.wav", sweep("objective_update", 0.42, 520, 1040), category="gameplay")
    save(f"{gp}/game_objective_complete_01.wav", short_positive(1.05), category="gameplay")
    save(f"{gp}/game_resource_shortage_01.wav", sweep("shortage", 0.36, 300, 160, True), category="gameplay")

    # Alerts
    al = "Alerts"
    save(f"{al}/alert_threat_ground_detected_01.wav", alert("ground", 1.15), category="alert")
    save(f"{al}/alert_threat_air_detected_01.wav", alert("air", 1.15), category="alert")
    save(f"{al}/alert_unit_under_attack_01.wav", alert("unit", 0.95), category="alert")
    save(f"{al}/alert_base_breached_01.wav", alert("base", 1.55), category="alert")
    save(f"{al}/alert_building_destroyed_friendly_01.wav", alert("destroyed", 1.35), category="alert")
    save(f"{al}/alert_mission_timer_warning_01.wav", alert("timer", 0.90), category="alert")

    # Music and ambience
    save("Music/music_menu_loop_01.wav", loop_music("menu", 24.0), 2, "music")
    save("Music/music_briefing_loop_01.wav", loop_music("briefing", 24.0), 2, "music")
    save("Music/music_battle_intensity_01_loop.wav", loop_music("battle1", 24.0), 2, "music")
    save("Music/music_battle_intensity_02_loop.wav", loop_music("battle2", 24.0), 2, "music")
    save("Music/music_stinger_victory_01.wav", loop_music("victory", 5.0), 2, "music")
    save("Music/music_stinger_defeat_01.wav", loop_music("defeat", 5.0), 2, "music")
    save("Ambience/amb_city_strategic_loop_01.wav", ambience("city", 24.0), 2, "ambience")
    save("Ambience/amb_battlefield_loop_01.wav", ambience("battle", 24.0), 2, "ambience")

    # Tutorial
    tu = "Tutorial"
    save(f"{tu}/tutorial_step_open_01.wav", sweep("tutorial_open", 0.28, 440, 700), category="tutorial")
    save(f"{tu}/tutorial_step_complete_01.wav", short_positive(0.70), category="tutorial")
    save(f"{tu}/tutorial_step_blocked_01.wav", sweep("tutorial_blocked", 0.26, 280, 180, True), category="tutorial")
    for name in [
        "vo_tutorial_select_squad_01.wav",
        "vo_tutorial_move_units_01.wav",
        "vo_tutorial_attack_target_01.wav",
        "vo_tutorial_build_drawer_01.wav",
        "vo_tutorial_threat_alert_01.wav",
    ]:
        save(f"Voice/{name}", voice_placeholder(name, 2.4), category="voice")

    manifest = OUT / "AudioPackReadme.md"
    manifest.write_text(
        "# WarlineCapture Generated Audio Pack\n\n"
        "Generated by `Tools/generate_warlinecapture_audio.py`.\n\n"
        "This pack contains deterministic WAV assets for the first vertical slice "
        "listed in `Design/Audio_Design_Guidelines.md`. The clips are "
        "implementation-ready prototype assets: suitable for wiring, testing, and "
        "early play feel. Final AAA release audio should replace or sweeten these "
        "with dedicated sound-design masters while preserving filenames/event ids.\n"
    )
    print(f"Generated audio pack at {OUT}")


if __name__ == "__main__":
    main()

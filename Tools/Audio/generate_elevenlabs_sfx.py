#!/usr/bin/env python3
"""Generate, QA, and map ElevenLabs sound effects for gameplay audio events."""

from __future__ import annotations

import argparse
import datetime as dt
import json
import math
import os
import shutil
import struct
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import wave
from dataclasses import asdict, dataclass
from hashlib import md5
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
AUDIO_ROOT = ROOT / "Assets" / "Game" / "Audio"
CATALOG_PATH = AUDIO_ROOT / "Config" / "audio_event_catalog_v0_1.json"
DEFAULT_SECRET_PATH = Path("/private/tmp/warlinecapture-secrets/elevenlabs_api_key")
DEFAULT_WORK_ROOT = Path("/private/tmp/warlinecapture-elevenlabs-sfx")
API_URL = "https://api.elevenlabs.io/v1/sound-generation"
MODEL_ID = "eleven_text_to_sound_v2"
GENERATED_STATUS = "generated-elevenlabs"


@dataclass(frozen=True)
class SfxSpec:
    event_id: str
    asset_path: str
    duration_seconds: float
    prompt: str
    loop: bool = False
    prompt_influence: float = 0.48
    min_score: float = 72.0
    min_rms_db: float = -34.0
    max_rms_db: float = -7.0
    max_silence_ratio: float = 0.55
    min_crest_db: float = 3.0
    max_crest_db: float = 25.0
    transient_expected: bool = False


SPECS: tuple[SfxSpec, ...] = (
    SfxSpec(
        event_id="Gameplay.Unit.Engine.Vehicle.Move",
        asset_path="Assets/Game/Audio/Gameplay/game_unit_engine_vehicle_move_01.wav",
        duration_seconds=0.52,
        prompt=(
            "Modern armored military vehicle engine moving at close range, heavy diesel rumble, "
            "track and transmission vibration, compact dry battlefield game sound effect, no music, "
            "no voice, no siren, no alarm beep, no UI tone, clean mix with headroom."
        ),
        loop=False,
        prompt_influence=0.58,
        max_rms_db=-8.0,
    ),
    SfxSpec(
        event_id="Gameplay.Unit.Engine.Aircraft.Takeoff",
        asset_path="Assets/Game/Audio/Gameplay/game_unit_engine_aircraft_takeoff_01.wav",
        duration_seconds=1.05,
        prompt=(
            "Modern military fighter jet takeoff close pass, turbine spool-up into powerful "
            "afterburner roar, runway air pressure, compact one-second game sound effect, no music, "
            "no voice, no siren, no alarm beep, no UI tone, clean mix with headroom."
        ),
        loop=False,
        prompt_influence=0.62,
        max_rms_db=-6.0,
    ),
    SfxSpec(
        event_id="Gameplay.Unit.Engine.Aircraft.Flight",
        asset_path="Assets/Game/Audio/Gameplay/game_unit_engine_aircraft_flight_01.wav",
        duration_seconds=0.62,
        prompt=(
            "Modern combat jet engine flyby bed, sustained turbine roar and air rush for camera follow, "
            "short seamless-feeling game sound effect, no music, no voice, no siren, no alarm beep, "
            "no UI tone, clean mix with headroom."
        ),
        loop=False,
        prompt_influence=0.60,
        max_rms_db=-7.0,
        max_crest_db=18.0,
    ),
    SfxSpec(
        event_id="Gameplay.Unit.Engine.Helicopter.Flight",
        asset_path="Assets/Game/Audio/Gameplay/game_unit_engine_helicopter_flight_01.wav",
        duration_seconds=0.62,
        prompt=(
            "Military helicopter close flight engine, realistic rotor blade chop with turbine whine "
            "and air wash, short seamless-feeling camera-follow game sound effect, no music, no voice, "
            "no siren, no alarm beep, no UI tone, clean mix with headroom."
        ),
        loop=False,
        prompt_influence=0.62,
        max_rms_db=-7.0,
        max_crest_db=18.0,
    ),
    SfxSpec(
        event_id="Gameplay.Weapon.Fire.SmallArms",
        asset_path="Assets/Game/Audio/Gameplay/game_weapon_fire_small_arms_01.wav",
        duration_seconds=0.50,
        prompt=(
            "Distant enemy rifle and light machine-gun burst on open battlefield, sharp muzzle cracks "
            "with short tail, compact RTS game weapon-fire sound effect, no music, no voice, no siren, "
            "no alarm beep, no UI tone, clean mix with headroom."
        ),
        loop=False,
        prompt_influence=0.60,
        max_rms_db=-5.0,
        min_crest_db=5.0,
        transient_expected=True,
    ),
    SfxSpec(
        event_id="Gameplay.Weapon.Missile.Launch",
        asset_path="Assets/Game/Audio/Gameplay/game_weapon_missile_launch_01.wav",
        duration_seconds=0.75,
        prompt=(
            "Shoulder or aircraft missile launch, explosive ignition thump followed by hot rocket motor "
            "whoosh, compact battlefield game sound effect, no music, no voice, no siren, no alarm beep, "
            "no UI tone, clean mix with headroom."
        ),
        loop=False,
        prompt_influence=0.62,
        max_rms_db=-5.0,
        min_crest_db=5.0,
        transient_expected=True,
    ),
    SfxSpec(
        event_id="Gameplay.Weapon.Missile.Flight",
        asset_path="Assets/Game/Audio/Gameplay/game_weapon_missile_flight_01.wav",
        duration_seconds=0.50,
        prompt=(
            "Fast missile passing through air, bright rocket motor hiss and Doppler whoosh, short "
            "camera-tracked projectile game sound effect, no explosion, no music, no voice, no siren, "
            "no alarm beep, no UI tone, clean mix with headroom."
        ),
        loop=False,
        prompt_influence=0.60,
        max_rms_db=-7.0,
        max_crest_db=20.0,
    ),
    SfxSpec(
        event_id="Gameplay.Weapon.Missile.Impact",
        asset_path="Assets/Game/Audio/Gameplay/game_weapon_missile_impact_01.wav",
        duration_seconds=0.95,
        prompt=(
            "Missile impact explosion on a military base, hard blast transient, debris, pressure wave, "
            "short smoke tail, cinematic but dry RTS game sound effect, no music, no voice, no siren, "
            "no alarm beep, no UI tone, clean mix with headroom."
        ),
        loop=False,
        prompt_influence=0.62,
        max_rms_db=-4.0,
        min_crest_db=5.0,
        transient_expected=True,
    ),
    SfxSpec(
        "Gameplay.Unit.Select.Infantry",
        "Assets/Game/Audio/Gameplay/game_unit_select_infantry_01.wav",
        0.50,
        "Infantry squad selected, short tactical radio click with light gear rattle, professional RTS game UI-SFX, no voice, no music, no siren, no alarm.",
        prompt_influence=0.58,
        max_rms_db=-8.0,
    ),
    SfxSpec(
        "Gameplay.Unit.Select.Vehicle",
        "Assets/Game/Audio/Gameplay/game_unit_select_vehicle_01.wav",
        0.50,
        "Armored vehicle selected, short radio squelch with metallic vehicle servo clack, professional military RTS game sound, no voice, no music, no siren.",
        prompt_influence=0.58,
        max_rms_db=-8.0,
    ),
    SfxSpec(
        "Gameplay.Unit.Select.Air",
        "Assets/Game/Audio/Gameplay/game_unit_select_air_01.wav",
        0.50,
        "Aircraft selected, short avionics chirp with pilot radio squelch, clean military RTS game confirmation sound, no voice, no music, no siren.",
        prompt_influence=0.58,
        max_rms_db=-8.0,
    ),
    SfxSpec(
        "Gameplay.Command.Move.Accepted",
        "Assets/Game/Audio/Gameplay/game_command_move_accepted_01.wav",
        0.50,
        "Move command accepted, concise military radio command chirp and map marker tick, professional RTS interface sound, no spoken words, no music, no alarm.",
        prompt_influence=0.60,
    ),
    SfxSpec(
        "Gameplay.Command.Attack.Accepted",
        "Assets/Game/Audio/Gameplay/game_command_attack_accepted_01.wav",
        0.50,
        "Attack command accepted, target lock chirp with tactical radio squelch, aggressive military RTS confirmation sound, no spoken words, no music, no alarm.",
        prompt_influence=0.60,
        max_rms_db=-7.0,
    ),
    SfxSpec(
        "Gameplay.Command.Hold.Accepted",
        "Assets/Game/Audio/Gameplay/game_command_hold_accepted_01.wav",
        0.50,
        "Hold position command accepted, short defensive radio chirp with subtle mechanical click, professional military RTS sound, no voice, no music.",
        prompt_influence=0.58,
    ),
    SfxSpec(
        "Gameplay.Command.Stop.Returning",
        "Assets/Game/Audio/Gameplay/game_command_stop_returning_01.wav",
        0.55,
        "Stop or return-to-base command accepted, short tactical radio double-click with descending tone, clean RTS command sound, no voice, no music, no alarm.",
        prompt_influence=0.58,
    ),
    SfxSpec(
        "Gameplay.Command.Scan.Targeting",
        "Assets/Game/Audio/Gameplay/game_command_scan_targeting_01.wav",
        0.55,
        "Tactical scan targeting, short radar sweep and target acquisition tick, military reconnaissance interface sound, no voice, no music, no siren.",
        prompt_influence=0.62,
    ),
    SfxSpec(
        "Gameplay.Command.Scan.Accepted",
        "Assets/Game/Audio/Gameplay/game_command_scan_accepted_01.wav",
        0.55,
        "Scan command accepted, radar pulse confirms contact search, concise military RTS interface sound, no voice, no music, no alarm.",
        prompt_influence=0.62,
    ),
    SfxSpec(
        "Gameplay.Command.Rejected",
        "Assets/Game/Audio/Gameplay/game_command_rejected_01.wav",
        0.50,
        "Command rejected, short negative tactical UI buzz with dry radio click, no harsh alarm, no voice, no music.",
        prompt_influence=0.60,
    ),
    SfxSpec(
        "Gameplay.Build.Place.Valid",
        "Assets/Game/Audio/Gameplay/game_build_place_valid_01.wav",
        0.60,
        "Valid base building placement, compact construction clamp thud with positive tactical interface tick, military RTS game sound, no voice, no music.",
        prompt_influence=0.58,
        max_rms_db=-7.0,
    ),
    SfxSpec(
        "Gameplay.Build.Place.Invalid",
        "Assets/Game/Audio/Gameplay/game_build_place_invalid_01.wav",
        0.50,
        "Invalid building placement, short negative construction UI buzz and blocked metal tap, no harsh alarm, no voice, no music.",
        prompt_influence=0.60,
    ),
    SfxSpec(
        "Gameplay.Production.Queued",
        "Assets/Game/Audio/Gameplay/game_production_queued_01.wav",
        0.50,
        "Unit production queued, short factory console confirmation tick with subtle assembly servo, military base RTS sound, no voice, no music.",
        prompt_influence=0.58,
    ),
    SfxSpec(
        "Gameplay.Production.Complete",
        "Assets/Game/Audio/Gameplay/game_production_complete_01.wav",
        0.80,
        "Unit production complete, confident military factory completion sting, metal assembly lock and positive console pulse, no voice, no music.",
        prompt_influence=0.58,
        max_rms_db=-7.0,
    ),
    SfxSpec(
        "Gameplay.ResourceExchange.Accepted",
        "Assets/Game/Audio/Gameplay/game_resource_exchange_accepted_01.wav",
        0.50,
        "Resource exchange accepted, logistics terminal confirmation tick and soft crate clack, military base management sound, no voice, no music.",
        prompt_influence=0.58,
    ),
    SfxSpec(
        "Gameplay.ResourceExchange.Rejected",
        "Assets/Game/Audio/Gameplay/game_resource_exchange_rejected_01.wav",
        0.50,
        "Resource exchange rejected, short negative logistics terminal buzz, restrained and professional, no alarm, no voice, no music.",
        prompt_influence=0.58,
    ),
    SfxSpec(
        "Gameplay.ResourceExchange.QueueStarted",
        "Assets/Game/Audio/Gameplay/game_resource_exchange_queue_started_01.wav",
        0.55,
        "Resource exchange queue started, logistics conveyor click and terminal pulse, military supply base RTS sound, no voice, no music.",
        prompt_influence=0.56,
    ),
    SfxSpec(
        "Gameplay.ResourceExchange.Rushed",
        "Assets/Game/Audio/Gameplay/game_resource_exchange_rushed_01.wav",
        0.55,
        "Resource exchange rushed, fast logistics terminal pulse with accelerated mechanical ticks, professional RTS sound, no voice, no music, no alarm.",
        prompt_influence=0.58,
    ),
    SfxSpec(
        "Gameplay.ResourceExchange.Completed",
        "Assets/Game/Audio/Gameplay/game_resource_exchange_completed_01.wav",
        0.65,
        "Resource exchange completed, supply crate lock and positive command terminal chime, military logistics RTS sound, no voice, no music.",
        prompt_influence=0.58,
    ),
    SfxSpec(
        "Gameplay.ResourceExchange.Cancelled",
        "Assets/Game/Audio/Gameplay/game_resource_exchange_cancelled_01.wav",
        0.50,
        "Resource exchange cancelled, dry terminal cancel tick with muted mechanical stop, military logistics RTS sound, no voice, no music.",
        prompt_influence=0.58,
    ),
    SfxSpec(
        "Gameplay.Objective.Progress",
        "Assets/Game/Audio/Gameplay/game_objective_progress_01.wav",
        0.65,
        "Objective progress updated, tactical command interface pulse and restrained positive tick, military RTS mission sound, no voice, no music.",
        prompt_influence=0.58,
    ),
    SfxSpec(
        "Gameplay.Objective.Complete",
        "Assets/Game/Audio/Gameplay/game_objective_complete_01.wav",
        0.90,
        "Objective complete, short victorious military mission sting with command console pulse, professional RTS game sound, no voice, no full music.",
        prompt_influence=0.58,
        max_rms_db=-6.0,
    ),
    SfxSpec(
        "Gameplay.Objective.Failed",
        "Assets/Game/Audio/Gameplay/game_objective_failed_01.wav",
        0.90,
        "Objective failed, short serious military mission failure sting, low brass-like impact and terminal warning tone, no voice, no siren loop, no full music.",
        prompt_influence=0.58,
        max_rms_db=-6.0,
    ),
    SfxSpec(
        "Alert.Threat.Minor",
        "Assets/Game/Audio/Alerts/alert_threat_minor_01.wav",
        0.80,
        "Minor battlefield threat detected, short tactical warning ping with radar pulse, restrained military command alert, no voice, no looping siren, no music.",
        prompt_influence=0.62,
        max_rms_db=-6.0,
    ),
    SfxSpec(
        "Alert.Threat.Critical",
        "Assets/Game/Audio/Alerts/alert_threat_critical_01.wav",
        1.10,
        "Critical battlefield threat alert, urgent but short military command warning sting, radar lock pulse and low impact, no voice, no continuous siren, no music.",
        prompt_influence=0.62,
        max_rms_db=-5.0,
        transient_expected=True,
    ),
    SfxSpec(
        "Alert.Unit.UnderAttack",
        "Assets/Game/Audio/Alerts/alert_unit_under_attack_01.wav",
        0.85,
        "Friendly unit under attack alert, short tactical damage warning with two restrained beeps and radio static, no voice, no continuous alarm, no music.",
        prompt_influence=0.62,
        max_rms_db=-5.0,
    ),
    SfxSpec(
        "Alert.Base.Breached",
        "Assets/Game/Audio/Alerts/alert_base_breached_01.wav",
        1.20,
        "Base breached critical alert, short military base alarm sting with heavy impact and command console warning, no voice, no looping siren, no music.",
        prompt_influence=0.62,
        max_rms_db=-5.0,
        transient_expected=True,
    ),
    SfxSpec("UI.Button.Primary.Click", "Assets/Game/Audio/UI/ui_button_primary_click_01.wav", 0.50, "Premium sci-fi military UI primary button click, crisp tactile press, very short, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Button.Secondary.Click", "Assets/Game/Audio/UI/ui_button_secondary_click_01.wav", 0.50, "Premium sci-fi military UI secondary button click, softer tactile press, very short, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Button.Negative.Click", "Assets/Game/Audio/UI/ui_button_negative_click_01.wav", 0.50, "Premium sci-fi military UI negative button click, firm low tactile press, very short, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Button.Disabled.Tap", "Assets/Game/Audio/UI/ui_button_disabled_tap_01.wav", 0.50, "Disabled UI tap, clearly audible muted blocked plastic-metal click at the start with a short clean tail, professional, no alarm, no voice, no music.", prompt_influence=0.62, min_rms_db=-45.0, max_silence_ratio=0.92, max_crest_db=32.0),
    SfxSpec("UI.Tab.Select", "Assets/Game/Audio/UI/ui_tab_select_01.wav", 0.50, "Military command UI tab select, crisp digital tick and subtle panel switch, very short, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Card.Select", "Assets/Game/Audio/UI/ui_card_select_01.wav", 0.50, "Military command UI card select, clean tactical card lock-in tick, polished and short, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Card.Locked", "Assets/Game/Audio/UI/ui_card_locked_01.wav", 0.50, "Locked UI card feedback, muted access-denied click, restrained and short, no harsh alarm, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Popup.Open", "Assets/Game/Audio/UI/ui_popup_open_01.wav", 0.55, "Sci-fi military UI popup opens, quick panel slide and soft digital rise, professional short UI sound, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Popup.Close", "Assets/Game/Audio/UI/ui_popup_close_01.wav", 0.50, "Sci-fi military UI popup closes, quick panel slide and soft digital fall, professional short UI sound, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Screen.Forward", "Assets/Game/Audio/UI/ui_screen_forward_01.wav", 0.60, "Command interface screen transition forward, sleek digital whoosh and panel lock, short professional UI sound, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Screen.Back", "Assets/Game/Audio/UI/ui_screen_back_01.wav", 0.60, "Command interface screen transition back, sleek reverse digital whoosh and panel lock, short professional UI sound, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Drawer.Open", "Assets/Game/Audio/UI/ui_drawer_open_01.wav", 0.60, "Military UI drawer opens, compact mechanical slide with digital tick, short polished interface sound, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Drawer.Close", "Assets/Game/Audio/UI/ui_drawer_close_01.wav", 0.55, "Military UI drawer closes, compact mechanical slide shut with digital tick, short polished interface sound, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Slider.Tick", "Assets/Game/Audio/UI/ui_slider_tick_01.wav", 0.50, "Tiny UI slider tick, crisp subdued digital detent, very short and clean, no voice, no music.", prompt_influence=0.58, max_rms_db=-9.0),
    SfxSpec("UI.Toggle.On", "Assets/Game/Audio/UI/ui_toggle_on_01.wav", 0.50, "UI toggle on, crisp positive switch click with small digital rise, very short, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Toggle.Off", "Assets/Game/Audio/UI/ui_toggle_off_01.wav", 0.50, "UI toggle off, clearly audible crisp switch click with a small digital fall at the start and short clean tail, very short, no voice, no music.", prompt_influence=0.62, min_rms_db=-45.0, max_silence_ratio=0.92, max_crest_db=32.0),
    SfxSpec("UI.Feedback.Toast.Error", "Assets/Game/Audio/UI/ui_feedback_toast_error_01.wav", 0.65, "UI error toast, restrained negative digital pulse, professional military interface sound, no harsh alarm, no voice, no music.", prompt_influence=0.58),
    SfxSpec("UI.Feedback.Toast.Positive", "Assets/Game/Audio/UI/ui_feedback_toast_positive_01.wav", 0.65, "UI positive toast, restrained success digital pulse, professional military interface sound, no voice, no music.", prompt_influence=0.58),
    SfxSpec("Ambience.City.DayLoop", "Assets/Game/Audio/Ambience/amb_city_day_loop_01.wav", 8.0, "Loopable modern city daytime ambience near a military operation, distant traffic, wind, faint urban room tone, no music, no voice, seamless loop with clean headroom.", loop=True, prompt_influence=0.50, max_rms_db=-12.0, max_crest_db=22.0),
    SfxSpec("Ambience.Base.DistantLoop", "Assets/Game/Audio/Ambience/amb_base_distant_loop_01.wav", 8.0, "Loopable military base distant ambience, low generators, far vehicles, wind over concrete, subtle radio texture, no music, no voice, seamless loop with clean headroom.", loop=True, prompt_influence=0.52, max_rms_db=-12.0, max_crest_db=22.0),
    SfxSpec("Music.Splash.Intro", "Assets/Game/Audio/Music/music_splash_intro_01.wav", 5.0, "Short military strategy game splash intro sting, cinematic percussion and low brass texture, no voice, clean headroom.", prompt_influence=0.48, max_rms_db=-6.0),
    SfxSpec("Music.Menu.Loop", "Assets/Game/Audio/Music/music_menu_loop_01.wav", 10.0, "Loopable military strategy game main menu music bed, restrained cinematic pulse, subtle percussion, tactical tension, no voice, seamless loop.", loop=True, prompt_influence=0.46, max_rms_db=-8.0),
    SfxSpec("Music.Briefing.Loop", "Assets/Game/Audio/Music/music_briefing_loop_01.wav", 10.0, "Loopable mission briefing music bed, quiet military command room tension, low drones and subtle pulse, no voice, seamless loop.", loop=True, prompt_influence=0.46, max_rms_db=-9.0),
    SfxSpec("Music.Match.CalmLoop", "Assets/Game/Audio/Music/music_match_calm_loop_01.wav", 10.0, "Loopable calm RTS battlefield music bed, low tactical tension, restrained percussion and drones, no voice, seamless loop.", loop=True, prompt_influence=0.46, max_rms_db=-9.0),
    SfxSpec("Music.Match.CombatLoop", "Assets/Game/Audio/Music/music_match_combat_loop_01.wav", 10.0, "Loopable RTS combat music bed, military action pulse, cinematic percussion, tense low strings, no voice, seamless loop.", loop=True, prompt_influence=0.46, max_rms_db=-8.0),
    SfxSpec("Music.Result.Victory", "Assets/Game/Audio/Music/music_result_victory_01.wav", 4.0, "Short military strategy victory result sting, confident cinematic brass and percussion, no voice, clean ending.", prompt_influence=0.48, max_rms_db=-6.0),
    SfxSpec("Music.Result.Defeat", "Assets/Game/Audio/Music/music_result_defeat_01.wav", 4.0, "Short military strategy defeat result sting, somber low brass and percussion, no voice, clean ending.", prompt_influence=0.48, max_rms_db=-6.0),
)


@dataclass
class AudioMetrics:
    duration_seconds: float
    sample_rate: int
    channels: int
    rms_db: float
    peak_db: float
    crest_db: float
    clipped_ratio: float
    near_clip_ratio: float
    silence_ratio: float
    zero_crossing_rate: float


@dataclass
class CandidateResult:
    event_id: str
    index: int
    mp3_path: str
    wav_path: str
    score: float
    passed: bool
    reasons: list[str]
    metrics: AudioMetrics


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--api-key-file", type=Path, default=DEFAULT_SECRET_PATH)
    parser.add_argument("--work-root", type=Path, default=DEFAULT_WORK_ROOT)
    parser.add_argument("--events", nargs="*", default=[spec.event_id for spec in SPECS])
    parser.add_argument("--candidate-count", type=int, default=2)
    parser.add_argument("--max-candidates", type=int, default=4)
    parser.add_argument("--min-score", type=float, default=None)
    parser.add_argument("--output-format", default="mp3_44100_128")
    parser.add_argument("--timeout-seconds", type=float, default=120.0)
    parser.add_argument("--map", action="store_true", help="Copy selected passing WAVs to catalog asset paths.")
    parser.add_argument("--allow-failed-map", action="store_true", help="Map the best candidate even if QA did not pass.")
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def read_api_key(path: Path) -> str:
    key = os.environ.get("ELEVENLABS_API_KEY", "").strip()
    if key:
        return key
    if not path.exists():
        raise RuntimeError(f"Missing ElevenLabs API key file: {path}")
    key = path.read_text().strip()
    if not key:
        raise RuntimeError(f"Empty ElevenLabs API key file: {path}")
    return key


def slug(value: str) -> str:
    chars: list[str] = []
    for char in value.lower():
        if char.isalnum():
            chars.append(char)
        elif chars and chars[-1] != "_":
            chars.append("_")
    return "".join(chars).strip("_")


def run_dir(work_root: Path) -> Path:
    stamp = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    path = work_root / stamp
    path.mkdir(parents=True, exist_ok=False)
    return path


def request_sound(
    api_key: str,
    spec: SfxSpec,
    destination: Path,
    output_format: str,
    timeout_seconds: float,
    dry_run: bool,
) -> None:
    if dry_run:
        return

    query = urllib.parse.urlencode({"output_format": output_format})
    url = f"{API_URL}?{query}"
    body = {
        "text": spec.prompt,
        "duration_seconds": spec.duration_seconds,
        "loop": spec.loop,
        "prompt_influence": spec.prompt_influence,
        "model_id": MODEL_ID,
    }
    request = urllib.request.Request(
        url,
        data=json.dumps(body).encode("utf-8"),
        method="POST",
        headers={
            "Content-Type": "application/json",
            "Accept": "application/octet-stream",
            "xi-api-key": api_key,
        },
    )

    try:
        with urllib.request.urlopen(request, timeout=timeout_seconds) as response:
            destination.write_bytes(response.read())
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"ElevenLabs HTTP {exc.code} for {spec.event_id}: {detail}") from exc
    except urllib.error.URLError as exc:
        raise RuntimeError(f"ElevenLabs request failed for {spec.event_id}: {exc}") from exc


def convert_to_wav(source: Path, destination: Path) -> None:
    subprocess.run(
        [
            "ffmpeg",
            "-y",
            "-v",
            "error",
            "-i",
            str(source),
            "-ac",
            "1",
            "-ar",
            "44100",
            "-sample_fmt",
            "s16",
            "-filter:a",
            "volume=-1.5dB",
            str(destination),
        ],
        check=True,
    )


def amplitude_to_db(value: float) -> float:
    if value <= 0.0:
        return -120.0
    return 20.0 * math.log10(value)


def analyze_wav(path: Path) -> AudioMetrics:
    with wave.open(str(path), "rb") as wav:
        channels = wav.getnchannels()
        sample_width = wav.getsampwidth()
        sample_rate = wav.getframerate()
        frames = wav.getnframes()
        raw = wav.readframes(frames)

    if sample_width != 2:
        raise RuntimeError(f"{path} is not 16-bit PCM.")
    if frames <= 0:
        raise RuntimeError(f"{path} has no samples.")

    sample_count = len(raw) // 2
    samples = struct.unpack(f"<{sample_count}h", raw)
    abs_values = [abs(sample) / 32768.0 for sample in samples]
    peak = max(abs_values)
    square_sum = sum(value * value for value in abs_values)
    rms = math.sqrt(square_sum / max(1, len(abs_values)))
    silence_threshold = 10 ** (-60.0 / 20.0)
    silence_ratio = sum(1 for value in abs_values if value <= silence_threshold) / len(abs_values)
    clipped_ratio = sum(1 for sample in samples if abs(sample) >= 32767) / len(samples)
    near_clip_ratio = sum(1 for value in abs_values if value >= 0.98) / len(abs_values)

    zero_crossings = 0
    last = samples[0]
    for sample in samples[1:]:
        if (last < 0 <= sample) or (last >= 0 > sample):
            zero_crossings += 1
        last = sample

    rms_db = amplitude_to_db(rms)
    peak_db = amplitude_to_db(peak)
    return AudioMetrics(
        duration_seconds=frames / sample_rate,
        sample_rate=sample_rate,
        channels=channels,
        rms_db=rms_db,
        peak_db=peak_db,
        crest_db=peak_db - rms_db,
        clipped_ratio=clipped_ratio,
        near_clip_ratio=near_clip_ratio,
        silence_ratio=silence_ratio,
        zero_crossing_rate=zero_crossings / max(1, frames),
    )


def score_candidate(spec: SfxSpec, metrics: AudioMetrics, min_score_override: float | None) -> tuple[float, bool, list[str]]:
    score = 100.0
    reasons: list[str] = []

    duration_delta = abs(metrics.duration_seconds - spec.duration_seconds)
    if duration_delta > 0.12:
        penalty = min(30.0, duration_delta / max(0.1, spec.duration_seconds) * 40.0)
        score -= penalty
        reasons.append(f"duration_delta={duration_delta:.3f}s")

    if metrics.rms_db < spec.min_rms_db:
        score -= min(25.0, (spec.min_rms_db - metrics.rms_db) * 1.2)
        reasons.append(f"quiet_rms={metrics.rms_db:.1f}dB")
    if metrics.rms_db > spec.max_rms_db:
        score -= min(20.0, (metrics.rms_db - spec.max_rms_db) * 2.0)
        reasons.append(f"hot_rms={metrics.rms_db:.1f}dB")

    if metrics.clipped_ratio > 0.0:
        score -= min(60.0, 20.0 + metrics.clipped_ratio * 4000.0)
        reasons.append(f"clipped={metrics.clipped_ratio:.4%}")
    if metrics.near_clip_ratio > 0.01:
        score -= min(25.0, metrics.near_clip_ratio * 600.0)
        reasons.append(f"near_clip={metrics.near_clip_ratio:.3%}")
    if metrics.silence_ratio > spec.max_silence_ratio:
        score -= min(30.0, (metrics.silence_ratio - spec.max_silence_ratio) * 80.0)
        reasons.append(f"silence={metrics.silence_ratio:.1%}")
    if metrics.crest_db < spec.min_crest_db:
        score -= 12.0
        reasons.append(f"flat_crest={metrics.crest_db:.1f}dB")
    if metrics.crest_db > spec.max_crest_db:
        score -= 10.0
        reasons.append(f"spiky_crest={metrics.crest_db:.1f}dB")
    if spec.transient_expected and metrics.crest_db < 6.0:
        score -= 10.0
        reasons.append("weak_transient")
    if metrics.zero_crossing_rate <= 0.005:
        score -= 8.0
        reasons.append(f"low_high_frequency_content={metrics.zero_crossing_rate:.4f}")

    score = max(0.0, min(100.0, score))
    required_score = min_score_override if min_score_override is not None else spec.min_score
    passed = score >= required_score and metrics.clipped_ratio == 0.0
    if not passed and not reasons:
        reasons.append(f"score_below_threshold={score:.1f}")
    return score, passed, reasons


def unity_guid(path: Path) -> str:
    rel = path.relative_to(ROOT).as_posix()
    return md5(f"warlinecapture-audio-v0.1:{rel}".encode("utf-8")).hexdigest()


def write_gameplay_audio_meta(path: Path) -> None:
    meta = Path(f"{path}.meta")
    if meta.exists():
        return
    meta.write_text(
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
  userData: WarlineCapture generated ElevenLabs gameplay audio
  assetBundleName:
  assetBundleVariant:
"""
    )


def update_catalog_status(mapped_events: set[str]) -> None:
    catalog = json.loads(CATALOG_PATH.read_text())
    for event in catalog["events"]:
        if event.get("eventId") not in mapped_events:
            continue
        for clip in event.get("clips", []):
            clip["status"] = GENERATED_STATUS
    CATALOG_PATH.write_text(json.dumps(catalog, indent=2) + "\n")


def map_candidate(spec: SfxSpec, candidate: CandidateResult) -> None:
    destination = ROOT / spec.asset_path
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(candidate.wav_path, destination)
    write_gameplay_audio_meta(destination)


def find_specs(event_ids: list[str]) -> list[SfxSpec]:
    by_id = {spec.event_id: spec for spec in SPECS}
    missing = [event_id for event_id in event_ids if event_id not in by_id]
    if missing:
        raise RuntimeError(f"Unknown event id(s): {', '.join(missing)}")
    return [by_id[event_id] for event_id in event_ids]


def generate_for_spec(
    api_key: str,
    spec: SfxSpec,
    event_dir: Path,
    args: argparse.Namespace,
) -> tuple[CandidateResult, list[CandidateResult]]:
    event_dir.mkdir(parents=True, exist_ok=True)
    candidates: list[CandidateResult] = []
    max_candidates = max(args.candidate_count, args.max_candidates)

    for index in range(1, max_candidates + 1):
        mp3_path = event_dir / f"candidate_{index:02d}.mp3"
        wav_path = event_dir / f"candidate_{index:02d}.wav"
        print(f"[generate] {spec.event_id} candidate {index}/{max_candidates}", flush=True)
        request_sound(
            api_key,
            spec,
            mp3_path,
            output_format=args.output_format,
            timeout_seconds=args.timeout_seconds,
            dry_run=args.dry_run,
        )
        if args.dry_run:
            continue
        convert_to_wav(mp3_path, wav_path)
        metrics = analyze_wav(wav_path)
        score, passed, reasons = score_candidate(spec, metrics, args.min_score)
        result = CandidateResult(
            event_id=spec.event_id,
            index=index,
            mp3_path=str(mp3_path),
            wav_path=str(wav_path),
            score=round(score, 2),
            passed=passed,
            reasons=reasons,
            metrics=metrics,
        )
        candidates.append(result)
        status = "pass" if passed else "fail"
        print(
            f"[qa] {spec.event_id} candidate {index}: {status} score={score:.1f} "
            f"dur={metrics.duration_seconds:.3f}s rms={metrics.rms_db:.1f}dB "
            f"peak={metrics.peak_db:.1f}dB crest={metrics.crest_db:.1f}dB reasons={','.join(reasons) or 'none'}",
            flush=True,
        )
        if index >= args.candidate_count and any(candidate.passed for candidate in candidates):
            break
        time.sleep(0.25)

    if not candidates:
        raise RuntimeError(f"No candidates generated for {spec.event_id}")
    best = max(candidates, key=lambda candidate: candidate.score)
    return best, candidates


def main() -> int:
    args = parse_args()
    specs = find_specs(args.events)
    if args.dry_run:
        for spec in specs:
            print(
                f"[dry-run] {spec.event_id} duration={spec.duration_seconds:.2f}s "
                f"asset={spec.asset_path}",
                flush=True,
            )
        return 0

    api_key = read_api_key(args.api_key_file)
    run_path = run_dir(args.work_root)
    print(f"[run] writing candidates to {run_path}", flush=True)

    manifest: dict[str, Any] = {
        "schema": "WarlineCapture.ElevenLabsSfxGeneration.v0.1",
        "generatedAtUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "endpoint": API_URL,
        "modelId": MODEL_ID,
        "outputFormat": args.output_format,
        "mapped": args.map,
        "events": [],
    }
    mapped_events: set[str] = set()
    failures: list[str] = []

    for spec in specs:
        best, candidates = generate_for_spec(api_key, spec, run_path / slug(spec.event_id), args)
        can_map = best.passed or args.allow_failed_map
        if args.map and can_map and not args.dry_run:
            map_candidate(spec, best)
            mapped_events.add(spec.event_id)
            update_catalog_status({spec.event_id})
            print(f"[map] {spec.event_id} -> {spec.asset_path} candidate={best.index} score={best.score}", flush=True)
        elif args.map and not can_map:
            failures.append(f"{spec.event_id}: best score {best.score} failed QA")
            print(f"[skip] {spec.event_id} best candidate failed QA; not mapped", flush=True)

        manifest["events"].append(
            {
                "spec": asdict(spec),
                "bestCandidateIndex": best.index,
                "bestScore": best.score,
                "bestPassed": best.passed,
                "mapped": spec.event_id in mapped_events,
                "candidates": [
                    {
                        **asdict(candidate),
                        "metrics": asdict(candidate.metrics),
                    }
                    for candidate in candidates
                ],
            }
        )

    manifest_path = run_path / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n")
    print(f"[manifest] {manifest_path}", flush=True)

    if failures:
        for failure in failures:
            print(f"[failure] {failure}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

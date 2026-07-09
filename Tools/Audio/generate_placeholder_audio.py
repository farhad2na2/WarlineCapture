#!/usr/bin/env python3
"""Generate the current WarlineCapture placeholder audio catalog.

This is an asset/config generator only. It creates deterministic placeholder
WAV files for the current audio event contract and writes a data-only JSON
catalog that later runtime implementation can consume.
"""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import wave
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Callable


ROOT = Path(__file__).resolve().parents[2]
LEGACY_GENERATOR = ROOT / "Tools" / "generate_warlinecapture_audio.py"
AUDIO_ROOT = ROOT / "Assets" / "Game" / "Audio"
CONFIG_ROOT = AUDIO_ROOT / "Config"
GENERATED_ROOT = AUDIO_ROOT / "GeneratedSource"
CATALOG_PATH = CONFIG_ROOT / "audio_event_catalog_v0_1.json"
MANIFEST_PATH = GENERATED_ROOT / "audio_placeholder_manifest_v0_1.json"


def load_legacy_generator() -> Any:
    spec = importlib.util.spec_from_file_location("warlinecapture_legacy_audio", LEGACY_GENERATOR)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load audio synthesis helpers from {LEGACY_GENERATOR}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


LEGACY = load_legacy_generator()


@dataclass(frozen=True)
class EventSpec:
    event_id: str
    clip: str
    bus: str
    category: str
    priority: str
    cooldown_ms: int
    volume_db: float
    loop: bool = False
    spatial: bool = False
    max_instances: int = 4
    pitch_variance: tuple[float, float] = (-0.02, 0.02)


EVENTS: tuple[EventSpec, ...] = (
    EventSpec("UI.Button.Primary.Click", "UI/ui_button_primary_click_01.wav", "UI", "ui", "Medium", 35, -10.0),
    EventSpec("UI.Button.Secondary.Click", "UI/ui_button_secondary_click_01.wav", "UI", "ui", "Medium", 35, -11.0),
    EventSpec("UI.Button.Negative.Click", "UI/ui_button_negative_click_01.wav", "UI", "ui", "Medium", 35, -10.5),
    EventSpec("UI.Button.Disabled.Tap", "UI/ui_button_disabled_tap_01.wav", "UI", "ui", "Low", 120, -13.0),
    EventSpec("UI.Tab.Select", "UI/ui_tab_select_01.wav", "UI", "ui", "Medium", 45, -11.0),
    EventSpec("UI.Card.Select", "UI/ui_card_select_01.wav", "UI", "ui", "Medium", 45, -10.5),
    EventSpec("UI.Card.Locked", "UI/ui_card_locked_01.wav", "UI", "ui", "Low", 120, -13.0),
    EventSpec("UI.Popup.Open", "UI/ui_popup_open_01.wav", "UI", "ui", "Medium", 80, -12.0),
    EventSpec("UI.Popup.Close", "UI/ui_popup_close_01.wav", "UI", "ui", "Medium", 80, -12.0),
    EventSpec("UI.Slider.Tick", "UI/ui_slider_tick_01.wav", "UI", "ui", "Low", 35, -16.0, max_instances=2),
    EventSpec("UI.Toggle.On", "UI/ui_toggle_on_01.wav", "UI", "ui", "Medium", 50, -11.5),
    EventSpec("UI.Toggle.Off", "UI/ui_toggle_off_01.wav", "UI", "ui", "Medium", 50, -12.0),
    EventSpec("UI.Feedback.Toast.Error", "UI/ui_feedback_toast_error_01.wav", "UI", "ui", "High", 160, -9.5),
    EventSpec("UI.Feedback.Toast.Positive", "UI/ui_feedback_toast_positive_01.wav", "UI", "ui", "Medium", 120, -10.5),
    EventSpec("Gameplay.Unit.Select.Infantry", "Gameplay/game_unit_select_infantry_01.wav", "SFX", "gameplay", "Medium", 80, -10.0, spatial=True),
    EventSpec("Gameplay.Unit.Select.Vehicle", "Gameplay/game_unit_select_vehicle_01.wav", "SFX", "gameplay", "Medium", 80, -10.0, spatial=True),
    EventSpec("Gameplay.Unit.Select.Air", "Gameplay/game_unit_select_air_01.wav", "SFX", "gameplay", "Medium", 80, -10.0, spatial=True),
    EventSpec("Gameplay.Unit.Engine.Vehicle.Move", "Gameplay/game_unit_engine_vehicle_move_01.wav", "SFX", "gameplay", "Low", 0, -13.0, spatial=True, max_instances=8, pitch_variance=(-0.04, 0.03)),
    EventSpec("Gameplay.Unit.Engine.Aircraft.Takeoff", "Gameplay/game_unit_engine_aircraft_takeoff_01.wav", "SFX", "gameplay", "Medium", 0, -10.0, spatial=True, max_instances=4, pitch_variance=(-0.03, 0.03)),
    EventSpec("Gameplay.Unit.Engine.Aircraft.Flight", "Gameplay/game_unit_engine_aircraft_flight_01.wav", "SFX", "gameplay", "Low", 0, -16.0, spatial=True, max_instances=8, pitch_variance=(-0.015, 0.015)),
    EventSpec("Gameplay.Weapon.Fire.SmallArms", "Gameplay/game_weapon_fire_small_arms_01.wav", "SFX", "gameplay", "Medium", 40, -9.0, spatial=True, max_instances=12, pitch_variance=(-0.05, 0.04)),
    EventSpec("Gameplay.Weapon.Missile.Launch", "Gameplay/game_weapon_missile_launch_01.wav", "SFX", "gameplay", "High", 0, -7.0, spatial=True, max_instances=6, pitch_variance=(-0.03, 0.03)),
    EventSpec("Gameplay.Weapon.Missile.Flight", "Gameplay/game_weapon_missile_flight_01.wav", "SFX", "gameplay", "Medium", 0, -10.0, spatial=True, max_instances=8, pitch_variance=(-0.04, 0.04)),
    EventSpec("Gameplay.Weapon.Missile.Impact", "Gameplay/game_weapon_missile_impact_01.wav", "SFX", "gameplay", "High", 0, -6.5, spatial=True, max_instances=8, pitch_variance=(-0.03, 0.03)),
    EventSpec("Gameplay.Command.Move.Accepted", "Gameplay/game_command_move_accepted_01.wav", "SFX", "gameplay", "Medium", 90, -10.0, spatial=True),
    EventSpec("Gameplay.Command.Attack.Accepted", "Gameplay/game_command_attack_accepted_01.wav", "SFX", "gameplay", "High", 90, -9.0, spatial=True),
    EventSpec("Gameplay.Command.Hold.Accepted", "Gameplay/game_command_hold_accepted_01.wav", "SFX", "gameplay", "Medium", 90, -10.0, spatial=True),
    EventSpec("Gameplay.Command.Stop.Returning", "Gameplay/game_command_stop_returning_01.wav", "SFX", "gameplay", "High", 120, -9.0, spatial=True),
    EventSpec("Gameplay.Command.Scan.Targeting", "Gameplay/game_command_scan_targeting_01.wav", "SFX", "gameplay", "Medium", 120, -11.0, spatial=True),
    EventSpec("Gameplay.Command.Scan.Accepted", "Gameplay/game_command_scan_accepted_01.wav", "SFX", "gameplay", "Medium", 120, -10.0, spatial=True),
    EventSpec("Gameplay.Command.Rejected", "Gameplay/game_command_rejected_01.wav", "SFX", "gameplay", "High", 180, -9.5),
    EventSpec("Gameplay.Build.Place.Valid", "Gameplay/game_build_place_valid_01.wav", "SFX", "gameplay", "Medium", 120, -9.5, spatial=True),
    EventSpec("Gameplay.Build.Place.Invalid", "Gameplay/game_build_place_invalid_01.wav", "SFX", "gameplay", "High", 180, -9.5),
    EventSpec("Gameplay.Production.Queued", "Gameplay/game_production_queued_01.wav", "SFX", "gameplay", "Medium", 100, -10.0),
    EventSpec("Gameplay.Production.Complete", "Gameplay/game_production_complete_01.wav", "SFX", "gameplay", "High", 250, -9.0),
    EventSpec("Alert.Threat.Minor", "Alerts/alert_threat_minor_01.wav", "Alerts", "alert", "Medium", 1200, -8.5),
    EventSpec("Alert.Threat.Critical", "Alerts/alert_threat_critical_01.wav", "Alerts", "alert", "Critical", 2500, -7.0),
    EventSpec("Alert.Unit.UnderAttack", "Alerts/alert_unit_under_attack_01.wav", "Alerts", "alert", "High", 1800, -8.0),
    EventSpec("Alert.Base.Breached", "Alerts/alert_base_breached_01.wav", "Alerts", "alert", "Critical", 3500, -7.0),
    EventSpec("Gameplay.Objective.Progress", "Gameplay/game_objective_progress_01.wav", "SFX", "gameplay", "Medium", 500, -10.0),
    EventSpec("Gameplay.Objective.Complete", "Gameplay/game_objective_complete_01.wav", "SFX", "gameplay", "High", 800, -8.5),
    EventSpec("Gameplay.Objective.Failed", "Gameplay/game_objective_failed_01.wav", "SFX", "gameplay", "Critical", 1000, -7.5),
    EventSpec("Music.Splash.Intro", "Music/music_splash_intro_01.wav", "Music", "music", "Critical", 0, -6.0, loop=False, max_instances=1, pitch_variance=(0.0, 0.0)),
    EventSpec("Music.Menu.Loop", "Music/music_menu_loop_01.wav", "Music", "music", "Critical", 0, -8.0, loop=True, max_instances=1, pitch_variance=(0.0, 0.0)),
    EventSpec("Music.Briefing.Loop", "Music/music_briefing_loop_01.wav", "Music", "music", "Critical", 0, -9.0, loop=True, max_instances=1, pitch_variance=(0.0, 0.0)),
    EventSpec("Music.Match.CalmLoop", "Music/music_match_calm_loop_01.wav", "Music", "music", "Critical", 0, -10.0, loop=True, max_instances=1, pitch_variance=(0.0, 0.0)),
    EventSpec("Music.Match.CombatLoop", "Music/music_match_combat_loop_01.wav", "Music", "music", "Critical", 0, -8.5, loop=True, max_instances=1, pitch_variance=(0.0, 0.0)),
    EventSpec("Music.Result.Victory", "Music/music_result_victory_01.wav", "Music", "music", "Critical", 0, -7.0, loop=False, max_instances=1, pitch_variance=(0.0, 0.0)),
    EventSpec("Music.Result.Defeat", "Music/music_result_defeat_01.wav", "Music", "music", "Critical", 0, -7.0, loop=False, max_instances=1, pitch_variance=(0.0, 0.0)),
    EventSpec("Ambience.City.DayLoop", "Ambience/amb_city_day_loop_01.wav", "Ambience", "ambience", "Low", 0, -16.0, loop=True, max_instances=1, pitch_variance=(0.0, 0.0)),
    EventSpec("Ambience.Base.DistantLoop", "Ambience/amb_base_distant_loop_01.wav", "Ambience", "ambience", "Low", 0, -17.0, loop=True, max_instances=1, pitch_variance=(0.0, 0.0)),
)


def unity_guid(path: Path) -> str:
    rel = path.relative_to(ROOT).as_posix()
    return hashlib.md5(f"warlinecapture-audio-v0.1:{rel}".encode("utf-8")).hexdigest()


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
  userData: WarlineCapture generated audio folder
  assetBundleName:
  assetBundleVariant:
"""
    )


def write_default_meta(path: Path, user_data: str) -> None:
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


def write_audio_meta(path: Path, category: str, channels: int) -> None:
    force_to_mono = 1 if channels == 1 else 0
    preload = 0 if category in {"music", "ambience"} else 1
    load_in_background = 1 if category in {"music", "ambience"} else 0
    load_type = 2 if category in {"music", "ambience"} else 0
    meta_path(path).write_text(
        f"""fileFormatVersion: 2
guid: {unity_guid(path)}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    loadType: {load_type}
    preloadAudioData: {preload}
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 1
    conversionMode: 0
  platformSettingOverrides: {{}}
  forceToMono: {force_to_mono}
  normalize: 1
  preloadAudioData: {preload}
  loadInBackground: {load_in_background}
  ambisonic: 0
  3D: 1
  userData: WarlineCapture generated placeholder {category} audio
  assetBundleName:
  assetBundleVariant:
"""
    )


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


def render_clip(clip: str) -> tuple[list[float], int]:
    name = Path(clip).name

    def jet_engine_bed(duration: float = 2.8) -> list[float]:
        low = 0.0
        mid = 0.0
        slow = 0.0

        def fn(t: float, rng) -> float:
            nonlocal low, mid, slow
            white = LEGACY.noise(rng)
            slow += (white - slow) * 0.003
            low += (white - low) * 0.018
            mid += (white - mid) * 0.11
            hiss = white - mid
            wobble = 1.0 + 0.018 * LEGACY.sine(0.7, t) + 0.01 * LEGACY.sine(1.9, t)
            rumble = (
                LEGACY.sine(82 * wobble, t) * 0.11 +
                LEGACY.sine(164 * wobble, t) * 0.045)
            turbine = (
                LEGACY.sine(430 + 12 * LEGACY.sine(0.45, t), t) * 0.022 +
                LEGACY.sine(860 + 18 * LEGACY.sine(0.38, t), t) * 0.012)
            air = low * 0.18 + slow * 0.08 + hiss * 0.035
            return (rumble + turbine + air) * LEGACY.env(t, duration, 0.18, 0.22)

        return LEGACY.render_mono(duration, fn)

    recipes: dict[str, Callable[[], tuple[list[float], int]]] = {
        "ui_button_primary_click_01.wav": lambda: (LEGACY.click("primary"), 1),
        "ui_button_secondary_click_01.wav": lambda: (LEGACY.click("secondary"), 1),
        "ui_button_negative_click_01.wav": lambda: (LEGACY.click("negative"), 1),
        "ui_button_disabled_tap_01.wav": lambda: (LEGACY.click("disabled", 0.16), 1),
        "ui_tab_select_01.wav": lambda: (LEGACY.click("tab", 0.12), 1),
        "ui_card_select_01.wav": lambda: (LEGACY.sweep("card", 0.20, 520, 980), 1),
        "ui_card_locked_01.wav": lambda: (LEGACY.sweep("locked_card", 0.22, 240, 120, True), 1),
        "ui_popup_open_01.wav": lambda: (LEGACY.sweep("popup_open", 0.30, 240, 780), 1),
        "ui_popup_close_01.wav": lambda: (LEGACY.sweep("popup_close", 0.24, 760, 260), 1),
        "ui_slider_tick_01.wav": lambda: (LEGACY.click("tab", 0.045), 1),
        "ui_toggle_on_01.wav": lambda: (LEGACY.click("toggle_on", 0.13), 1),
        "ui_toggle_off_01.wav": lambda: (LEGACY.click("toggle_off", 0.13), 1),
        "ui_feedback_toast_error_01.wav": lambda: (LEGACY.sweep("toast_error", 0.34, 280, 140, True), 1),
        "ui_feedback_toast_positive_01.wav": lambda: (LEGACY.short_positive(0.52), 1),
        "game_unit_select_infantry_01.wav": lambda: (LEGACY.radio("infantry", 0.26), 1),
        "game_unit_select_vehicle_01.wav": lambda: (LEGACY.radio("vehicle", 0.30), 1),
        "game_unit_select_air_01.wav": lambda: (LEGACY.radio("air", 0.32), 1),
        "game_unit_engine_vehicle_move_01.wav": lambda: (LEGACY.sweep("vehicle_engine", 0.55, 90, 120), 1),
        "game_unit_engine_aircraft_takeoff_01.wav": lambda: (LEGACY.sweep("aircraft_takeoff", 1.05, 170, 680), 1),
        "game_unit_engine_aircraft_flight_01.wav": lambda: (jet_engine_bed(), 1),
        "game_weapon_fire_small_arms_01.wav": lambda: (LEGACY.impact("small_arms", 0.16), 1),
        "game_weapon_missile_launch_01.wav": lambda: (LEGACY.impact("cannon", 0.75), 1),
        "game_weapon_missile_flight_01.wav": lambda: (LEGACY.sweep("missile_flight", 0.55, 850, 1320), 1),
        "game_weapon_missile_impact_01.wav": lambda: (LEGACY.impact("large", 0.95), 1),
        "game_command_move_accepted_01.wav": lambda: (LEGACY.radio("move", 0.25), 1),
        "game_command_attack_accepted_01.wav": lambda: (LEGACY.radio("attack", 0.28), 1),
        "game_command_hold_accepted_01.wav": lambda: (LEGACY.sweep("hold", 0.28, 680, 420), 1),
        "game_command_stop_returning_01.wav": lambda: (LEGACY.sweep("stop_return", 0.44, 620, 260, True), 1),
        "game_command_scan_targeting_01.wav": lambda: (LEGACY.sweep("scan_targeting", 0.42, 740, 1180), 1),
        "game_command_scan_accepted_01.wav": lambda: (LEGACY.sweep("scan_accept", 0.48, 420, 1040), 1),
        "game_command_rejected_01.wav": lambda: (LEGACY.radio("invalid", 0.32), 1),
        "game_build_place_valid_01.wav": lambda: (LEGACY.impact("cannon", 0.55), 1),
        "game_build_place_invalid_01.wav": lambda: (LEGACY.sweep("invalid_build", 0.30, 260, 120, True), 1),
        "game_production_queued_01.wav": lambda: (LEGACY.click("primary", 0.22), 1),
        "game_production_complete_01.wav": lambda: (LEGACY.short_positive(0.85), 1),
        "alert_threat_minor_01.wav": lambda: (LEGACY.alert("ground", 0.9), 1),
        "alert_threat_critical_01.wav": lambda: (LEGACY.alert("base", 1.5), 1),
        "alert_unit_under_attack_01.wav": lambda: (LEGACY.alert("unit", 0.95), 1),
        "alert_base_breached_01.wav": lambda: (LEGACY.alert("base", 1.55), 1),
        "game_objective_progress_01.wav": lambda: (LEGACY.sweep("objective_update", 0.42, 520, 1040), 1),
        "game_objective_complete_01.wav": lambda: (LEGACY.short_positive(1.05), 1),
        "game_objective_failed_01.wav": lambda: (LEGACY.sweep("objective_failed", 1.0, 360, 110, True), 1),
        "music_splash_intro_01.wav": lambda: (LEGACY.loop_music("briefing", 4.0), 2),
        "music_menu_loop_01.wav": lambda: (LEGACY.loop_music("menu", 24.0), 2),
        "music_briefing_loop_01.wav": lambda: (LEGACY.loop_music("briefing", 24.0), 2),
        "music_match_calm_loop_01.wav": lambda: (LEGACY.loop_music("battle1", 24.0), 2),
        "music_match_combat_loop_01.wav": lambda: (LEGACY.loop_music("battle2", 24.0), 2),
        "music_result_victory_01.wav": lambda: (LEGACY.loop_music("victory", 5.0), 2),
        "music_result_defeat_01.wav": lambda: (LEGACY.loop_music("defeat", 5.0), 2),
        "amb_city_day_loop_01.wav": lambda: (LEGACY.ambience("city", 24.0), 2),
        "amb_base_distant_loop_01.wav": lambda: (LEGACY.ambience("battle", 24.0), 2),
    }

    try:
        return recipes[name]()
    except KeyError as exc:
        raise KeyError(f"No placeholder recipe is defined for {clip}") from exc


def ensure_folders() -> None:
    for folder in (
        AUDIO_ROOT,
        AUDIO_ROOT / "Mixers",
        AUDIO_ROOT / "Events",
        AUDIO_ROOT / "UI",
        AUDIO_ROOT / "Gameplay",
        AUDIO_ROOT / "Alerts",
        AUDIO_ROOT / "Music",
        AUDIO_ROOT / "Ambience",
        AUDIO_ROOT / "Voice",
        CONFIG_ROOT,
        GENERATED_ROOT,
    ):
        folder.mkdir(parents=True, exist_ok=True)
        write_folder_meta(folder)


def generate_assets(force: bool) -> dict[str, list[dict[str, Any]]]:
    generated: list[dict[str, Any]] = []
    preserved: list[dict[str, Any]] = []

    for event in EVENTS:
        path = AUDIO_ROOT / event.clip
        if force or not path.exists():
            samples, channels = render_clip(event.clip)
            path.parent.mkdir(parents=True, exist_ok=True)
            LEGACY.write_wav(path, samples, channels)
            write_audio_meta(path, event.category, channels)
            info = read_wav_info(path)
            generated.append({"eventId": event.event_id, "assetPath": unity_path(path), **info})
        else:
            info = read_wav_info(path)
            if not meta_path(path).exists():
                write_audio_meta(path, event.category, int(info["channels"]))
            preserved.append({"eventId": event.event_id, "assetPath": unity_path(path), **info})

    return {"generated": generated, "preserved": preserved}


def unity_path(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def build_catalog() -> dict[str, Any]:
    return {
        "schema": "WarlineCapture.AudioEventCatalog.Placeholder.v0.1",
        "status": "placeholder-data-only",
        "generatedBy": "Tools/Audio/generate_placeholder_audio.py",
        "implementationNote": (
            "This file assigns audio event ids to placeholder WAV assets. It does "
            "not implement playback, ECS systems, UI wiring, or prefab changes."
        ),
        "buses": [
            {"busId": "UI", "parentBusId": "Master", "defaultVolumeDb": 0.0},
            {"busId": "SFX", "parentBusId": "Master", "defaultVolumeDb": 0.0},
            {"busId": "Alerts", "parentBusId": "Master", "defaultVolumeDb": 0.0, "ducks": ["Music", "Ambience"]},
            {"busId": "Music", "parentBusId": "Master", "defaultVolumeDb": -4.0},
            {"busId": "Ambience", "parentBusId": "Master", "defaultVolumeDb": -8.0},
        ],
        "events": [
            {
                "eventId": event.event_id,
                "busId": event.bus,
                "priority": event.priority,
                "cooldownMs": event.cooldown_ms,
                "volumeDb": event.volume_db,
                "pitchVariance": {"min": event.pitch_variance[0], "max": event.pitch_variance[1]},
                "playback": {
                    "loop": event.loop,
                    "spatial": event.spatial,
                    "maxInstances": event.max_instances,
                    "allowRuntimeLoad": False,
                },
                "clips": [
                    {
                        "assetPath": f"Assets/Game/Audio/{event.clip}",
                        "status": "placeholder",
                        "weight": 1,
                    }
                ],
            }
            for event in EVENTS
        ],
    }


def write_catalog() -> None:
    catalog = build_catalog()
    CATALOG_PATH.parent.mkdir(parents=True, exist_ok=True)
    CATALOG_PATH.write_text(json.dumps(catalog, indent=2) + "\n")
    write_default_meta(CATALOG_PATH, "WarlineCapture placeholder audio event catalog")


def write_manifest(asset_result: dict[str, list[dict[str, Any]]]) -> None:
    clip_infos: list[dict[str, Any]] = []
    for event in EVENTS:
        path = AUDIO_ROOT / event.clip
        clip_infos.append(
            {
                "eventId": event.event_id,
                "assetPath": unity_path(path),
                "category": event.category,
                **read_wav_info(path),
            }
        )

    manifest = {
        "schema": "WarlineCapture.AudioPlaceholderManifest.v0.1",
        "generatedBy": "Tools/Audio/generate_placeholder_audio.py",
        "eventCount": len(EVENTS),
        "generatedClipCount": len(asset_result["generated"]),
        "preservedClipCount": len(asset_result["preserved"]),
        "catalogPath": unity_path(CATALOG_PATH),
        "note": "Prototype clips for wiring and UX timing only; replace with final mastered/licensed assets before release.",
        "generated": asset_result["generated"],
        "preserved": asset_result["preserved"],
        "clips": clip_infos,
    }

    MANIFEST_PATH.parent.mkdir(parents=True, exist_ok=True)
    MANIFEST_PATH.write_text(json.dumps(manifest, indent=2) + "\n")
    write_default_meta(MANIFEST_PATH, "WarlineCapture placeholder audio generation manifest")


def validate_outputs() -> None:
    missing = []
    for event in EVENTS:
        path = AUDIO_ROOT / event.clip
        if not path.exists():
            missing.append(unity_path(path))
            continue
        read_wav_info(path)
        if not meta_path(path).exists():
            missing.append(unity_path(meta_path(path)))

    for path in (CATALOG_PATH, MANIFEST_PATH):
        if not path.exists():
            missing.append(unity_path(path))
        if not meta_path(path).exists():
            missing.append(unity_path(meta_path(path)))

    if missing:
        raise RuntimeError("Missing generated audio outputs:\n" + "\n".join(missing))

    catalog = json.loads(CATALOG_PATH.read_text())
    event_ids = [entry["eventId"] for entry in catalog["events"]]
    duplicates = sorted({event_id for event_id in event_ids if event_ids.count(event_id) > 1})
    if duplicates:
        raise RuntimeError("Duplicate audio event ids in catalog: " + ", ".join(duplicates))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--force", action="store_true", help="Regenerate all catalog WAV files, including existing clips.")
    args = parser.parse_args()

    ensure_folders()
    asset_result = generate_assets(force=args.force)
    write_catalog()
    write_manifest(asset_result)
    validate_outputs()

    print(
        "Generated WarlineCapture placeholder audio catalog: "
        f"{len(asset_result['generated'])} clips created, "
        f"{len(asset_result['preserved'])} clips preserved, "
        f"{len(EVENTS)} events assigned."
    )


if __name__ == "__main__":
    main()

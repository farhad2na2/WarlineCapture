#!/usr/bin/env python3
"""Apply WarlineCapture audio import profile values to catalog WAV metas."""

from __future__ import annotations

import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
CATALOG_PATH = ROOT / "Assets" / "Game" / "Audio" / "Config" / "audio_event_catalog_v0_1.json"
PROFILE_PATH = ROOT / "Assets" / "Game" / "Audio" / "Config" / "audio_import_profiles_v0_1.json"


UNITY_LOAD_TYPES = {
    "DecompressOnLoad": 0,
    "CompressedInMemory": 1,
    "Streaming": 2,
}

UNITY_COMPRESSION_FORMATS = {
    "PCM": 0,
    "Vorbis": 1,
    "ADPCM": 2,
}


def load_catalog_clip_paths() -> list[Path]:
    catalog = json.loads(CATALOG_PATH.read_text())
    paths: list[Path] = []
    for event in catalog["events"]:
        for clip in event["clips"]:
            paths.append(ROOT / clip["assetPath"])
    return paths


def category_for(path: Path) -> str:
    rel = path.relative_to(ROOT).as_posix()
    parts = rel.split("/")
    if len(parts) < 5 or parts[:3] != ["Assets", "Game", "Audio"]:
        raise RuntimeError(f"Audio clip is outside Assets/Game/Audio: {rel}")
    return parts[3]


def replace_scalar(text: str, key: str, value: int | str) -> str:
    pattern = re.compile(rf"(^\s*{re.escape(key)}:\s*).*$", re.MULTILINE)
    replacement = rf"\g<1>{value}"
    text, count = pattern.subn(replacement, text)
    if count != 1:
        raise RuntimeError(f"Expected one '{key}' field in audio meta.")
    return text


def replace_exact_indent_scalar(text: str, key: str, value: int | str, spaces: int) -> str:
    indent = " " * spaces
    pattern = re.compile(rf"(^{indent}{re.escape(key)}:\s*).*$", re.MULTILINE)
    replacement = rf"\g<1>{value}"
    text, count = pattern.subn(replacement, text)
    if count != 1:
        raise RuntimeError(f"Expected one '{key}' field with {spaces} spaces in audio meta.")
    return text


def replace_or_insert_sample_scalar(text: str, key: str, value: int | str) -> str:
    pattern = re.compile(rf"(^    {re.escape(key)}:\s*).*$", re.MULTILINE)
    replacement = rf"\g<1>{value}"
    text, count = pattern.subn(replacement, text)
    if count == 1:
        return text
    if count > 1:
        raise RuntimeError(f"Expected at most one defaultSettings '{key}' field in audio meta.")

    load_type_pattern = re.compile(r"(^    loadType:\s*.*$)", re.MULTILINE)
    text, insert_count = load_type_pattern.subn(rf"\1\n    {key}: {value}", text, count=1)
    if insert_count != 1:
        raise RuntimeError(f"Unable to insert defaultSettings '{key}' field in audio meta.")
    return text


def apply_profile(path: Path, profile: dict[str, object]) -> None:
    meta_path = Path(f"{path}.meta")
    if not path.exists():
        raise RuntimeError(f"Missing audio clip: {path.relative_to(ROOT)}")
    if not meta_path.exists():
        raise RuntimeError(f"Missing audio meta: {meta_path.relative_to(ROOT)}")

    text = meta_path.read_text()
    text = replace_scalar(text, "loadType", UNITY_LOAD_TYPES[str(profile["loadType"])])
    text = replace_scalar(text, "sampleRateSetting", 0)
    text = replace_scalar(text, "sampleRateOverride", int(profile["sampleRateOverride"]))
    text = replace_scalar(text, "compressionFormat", UNITY_COMPRESSION_FORMATS[str(profile["compressionFormat"])])
    text = replace_scalar(text, "quality", 1)
    text = replace_scalar(text, "conversionMode", 0)
    text = replace_scalar(text, "forceToMono", 1 if profile["forceToMono"] else 0)
    text = replace_scalar(text, "normalize", 1)
    text = replace_exact_indent_scalar(text, "preloadAudioData", 1 if profile["preloadAudioData"] else 0, 2)
    text = replace_or_insert_sample_scalar(text, "preloadAudioData", 1 if profile["preloadAudioData"] else 0)
    text = replace_scalar(text, "loadInBackground", 1 if profile["loadInBackground"] else 0)
    text = replace_scalar(text, "ambisonic", 0)
    text = replace_scalar(text, "3D", 1)
    meta_path.write_text(re.sub(r"[ \t]+\n", "\n", text))


def main() -> None:
    profiles = json.loads(PROFILE_PATH.read_text())["profiles"]
    clip_paths = load_catalog_clip_paths()
    for clip_path in clip_paths:
        category = category_for(clip_path)
        if category not in profiles:
            raise RuntimeError(f"No import profile for category '{category}'.")
        apply_profile(clip_path, profiles[category])

    print(f"Applied audio import profiles to {len(clip_paths)} catalog clips.")


if __name__ == "__main__":
    main()

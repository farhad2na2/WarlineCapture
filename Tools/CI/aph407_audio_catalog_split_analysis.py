#!/usr/bin/env python3
"""Generate deterministic APH-407 audio catalog split evidence."""

from __future__ import annotations

import hashlib
import json
import re
from collections import defaultdict
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
INVENTORY_PATH = ROOT / "Design/AgentReports/architecture_performance_content_residency_baseline.json"
MENU_CAPTURE_PATH = ROOT / "Design/AgentReports/aph-401_audio-memory-playback-menu.json"
MATCH_CAPTURE_PATH = ROOT / "Design/AgentReports/aph-401_audio-memory-playback-match.json"
SOURCE_CATALOG_PATH = ROOT / "Assets/Game/Audio/Config/audio_event_catalog_v0_1.json"
IMPORT_PROFILES_PATH = ROOT / "Assets/Game/Audio/Config/audio_import_profiles_v0_1.json"
SERIALIZED_CATALOG_PATH = ROOT / "Assets/Game/Audio/Events/AudioEventCatalogConfig.asset"
MENU_SCENE_PATH = ROOT / "Assets/Game/Scenes/Menu.unity"
RUNTIME_VIEW_PATH = ROOT / "Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationRuntimeView.cs"
BRIDGE_PATH = ROOT / "Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.cs"
SCENE_LIFECYCLE_PATH = ROOT / "Assets/Game/Scripts/Systems/SceneLifecycleSceneSystemHelper.cs"
OUTPUT_JSON = ROOT / "Design/AgentReports/2026-07-11_aph-407_audio_catalog_split_analysis.json"
OUTPUT_MARKDOWN = ROOT / "Design/AgentReports/2026-07-11_aph-407_audio_catalog_split_analysis.md"


def load_json(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as handle:
        value = json.load(handle)
    if not isinstance(value, dict):
        raise ValueError(f"Expected JSON object: {path}")
    return value


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def audio_importer_state(meta_text: str) -> tuple[int, int, int]:
    default_match = re.search(
        r"  defaultSettings:\n(?P<body>(?:    .*\n)+?)  platformSettingOverrides:",
        meta_text,
    )
    background_match = re.search(r"^  loadInBackground: ([01])$", meta_text, re.MULTILINE)
    if default_match is None or background_match is None:
        raise ValueError("Audio importer metadata is missing required load fields")
    body = default_match.group("body")
    load_match = re.search(r"^    loadType: ([012])$", body, re.MULTILINE)
    preload_match = re.search(r"^    preloadAudioData: ([01])$", body, re.MULTILINE)
    if load_match is None or preload_match is None:
        raise ValueError("Audio importer default settings are incomplete")
    return int(load_match.group(1)), int(preload_match.group(1)), int(background_match.group(1))


def partition_for(bus_id: str, event_ids: list[str]) -> str:
    if bus_id == "Voice":
        return "Voice"
    if bus_id == "UI":
        return "Core/Menu"
    if bus_id == "Music" and all(not event_id.startswith("Music.Match.") for event_id in event_ids):
        return "Core/Menu"
    return "Match"


def summarize_inventory(clips: list[dict[str, Any]]) -> list[dict[str, Any]]:
    totals: dict[str, dict[str, Any]] = defaultdict(
        lambda: {
            "clipCount": 0,
            "durationSeconds": 0.0,
            "compressedBytes": 0,
            "estimatedDecodedBytes": 0,
            "buses": set(),
        }
    )
    for clip in clips:
        bus_ids = clip.get("busIds")
        event_ids = clip.get("eventIds")
        if not isinstance(bus_ids, list) or len(bus_ids) != 1 or not isinstance(event_ids, list):
            raise ValueError(f"Ambiguous catalog clip ownership: {clip.get('assetPath')}")
        partition = partition_for(bus_ids[0], event_ids)
        row = totals[partition]
        row["clipCount"] += 1
        row["durationSeconds"] += float(clip["durationSeconds"])
        row["compressedBytes"] += int(clip["compressedSizeBytes"])
        row["estimatedDecodedBytes"] += int(clip["estimatedDecodedSizeBytes"])
        row["buses"].add(bus_ids[0])

    result = []
    for partition in ("Core/Menu", "Match", "Voice"):
        row = totals[partition]
        result.append(
            {
                "partition": partition,
                "clipCount": row["clipCount"],
                "durationSeconds": round(row["durationSeconds"], 6),
                "compressedBytes": row["compressedBytes"],
                "estimatedDecodedBytes": row["estimatedDecodedBytes"],
                "buses": sorted(row["buses"]),
            }
        )
    return result


def summarize_snapshot(snapshot: dict[str, Any]) -> list[dict[str, Any]]:
    totals: dict[str, dict[str, int]] = defaultdict(lambda: {"clipCount": 0, "loadedClipCount": 0, "runtimeMemoryBytes": 0})
    clips = snapshot.get("catalogClips")
    if not isinstance(clips, list):
        raise ValueError(f"Snapshot has no catalogClips: {snapshot.get('phase')}")
    for clip in clips:
        bus_ids = clip.get("busIds")
        event_ids = clip.get("eventIds")
        if not isinstance(bus_ids, list) or len(bus_ids) != 1 or not isinstance(event_ids, list):
            raise ValueError(f"Ambiguous capture clip ownership: {clip.get('assetPath')}")
        partition = partition_for(bus_ids[0], event_ids)
        totals[partition]["clipCount"] += 1
        totals[partition]["loadedClipCount"] += 1 if clip.get("loadState") == "Loaded" else 0
        totals[partition]["runtimeMemoryBytes"] += int(clip["runtimeMemoryBytes"])
    return [{"partition": name, **totals[name]} for name in ("Core/Menu", "Match", "Voice")]


def require_capture(capture: dict[str, Any], target: str) -> None:
    if capture.get("captureResult") != "Succeeded" or capture.get("captureTarget") != target:
        raise ValueError(f"Invalid {target} capture result")
    snapshots = capture.get("snapshots")
    if not isinstance(snapshots, list) or not snapshots:
        raise ValueError(f"Missing {target} snapshots")
    if snapshots[0].get("catalogClipCount") != 234:
        raise ValueError(f"Unexpected {target} catalog clip count")


def build_report(root: Path = ROOT) -> dict[str, Any]:
    inventory = load_json(root / INVENTORY_PATH.relative_to(ROOT))
    menu_capture = load_json(root / MENU_CAPTURE_PATH.relative_to(ROOT))
    match_capture = load_json(root / MATCH_CAPTURE_PATH.relative_to(ROOT))
    source_catalog = load_json(root / SOURCE_CATALOG_PATH.relative_to(ROOT))
    profiles = load_json(root / IMPORT_PROFILES_PATH.relative_to(ROOT))

    clips = inventory.get("catalogAudioClips")
    source_events = source_catalog.get("events")
    if not isinstance(clips, list) or len(clips) != 234:
        raise ValueError("Content inventory must contain exactly 234 catalog clips")
    if not isinstance(source_events, list) or len(source_events) != 234:
        raise ValueError("Source catalog must contain exactly 234 events")
    require_capture(menu_capture, "Menu")
    require_capture(match_capture, "Match")

    menu_baseline = menu_capture["snapshots"][0]
    match_baseline = match_capture["snapshots"][0]
    menu_partitions = summarize_snapshot(menu_baseline)
    core_bytes = next(row["runtimeMemoryBytes"] for row in menu_partitions if row["partition"] == "Core/Menu")
    current_bytes = int(menu_baseline["catalogRuntimeMemoryBytes"])

    profile_map = profiles.get("profiles", {})
    overrides = profiles.get("overrides", [])
    validation_sets = profiles.get("validationSets", {})
    pilot_paths = sorted(validation_sets.get("APH405VoicePilot", []))
    voice_profile = profile_map.get("Voice", {})
    if overrides:
        raise ValueError("Accepted Voice rollout must not retain per-clip importer overrides")
    if len(pilot_paths) != 8:
        raise ValueError("Expected the frozen eight-clip APH-405 validation set")
    if (
        voice_profile.get("loadType") != "CompressedInMemory"
        or voice_profile.get("preloadAudioData") is not False
        or voice_profile.get("loadInBackground") is not True
    ):
        raise ValueError("Expected the accepted category-level Voice importer policy")
    clips_by_path = {clip["assetPath"]: clip for clip in clips}
    if any(path not in clips_by_path for path in pilot_paths):
        raise ValueError("VoicePilot contains a path absent from the catalog inventory")

    ownership_files = {
        "serializedCatalog": root / SERIALIZED_CATALOG_PATH.relative_to(ROOT),
        "menuScene": root / MENU_SCENE_PATH.relative_to(ROOT),
        "runtimeView": root / RUNTIME_VIEW_PATH.relative_to(ROOT),
        "bridge": root / BRIDGE_PATH.relative_to(ROOT),
        "sceneLifecycle": root / SCENE_LIFECYCLE_PATH.relative_to(ROOT),
    }
    ownership_text = {name: path.read_text(encoding="utf-8") for name, path in ownership_files.items()}
    assumptions = {
        "singleSerializedCatalog": ownership_text["serializedCatalog"].count("\n  - eventId:") == 234,
        "catalogOwnedByMenuScene": ownership_text["menuScene"].count("AudioPlaybackPresentationRuntimeView") == 1,
        "matchLoadsAdditively": "LoadSceneMode.Additive" in ownership_text["sceneLifecycle"],
        "bridgeCachesOneCatalog": "private AudioEventCatalogConfig _eventCatalog;" in ownership_text["bridge"],
        "runtimeViewHasOneCatalogField": ownership_text["runtimeView"].count("AudioEventCatalogConfig eventCatalog") == 1,
    }
    if not all(assumptions.values()):
        raise ValueError(f"Runtime ownership assumptions drifted: {assumptions}")

    pilot_clips = [clips_by_path[path] for path in pilot_paths]
    voice_paths = sorted(
        clip["assetPath"]
        for clip in clips
        if clip.get("busIds") == ["Voice"]
    )
    importer_states: dict[str, tuple[int, int, int]] = {}
    importer_digest = hashlib.sha256()
    for asset_path in voice_paths:
        meta_path = root / f"{asset_path}.meta"
        meta_bytes = meta_path.read_bytes()
        importer_digest.update(asset_path.encode("utf-8") + b"\0" + meta_bytes)
        importer_states[asset_path] = audio_importer_state(meta_bytes.decode("utf-8"))
    pilot_applied = sum(importer_states[path] == (1, 0, 1) for path in pilot_paths)
    full_policy_applied = sum(importer_states[path] == (1, 0, 1) for path in voice_paths)
    legacy_retained = sum(importer_states[path] == (0, 1, 0) for path in voice_paths)
    if pilot_applied != 8 or full_policy_applied != 163 or legacy_retained != 0:
        raise ValueError(
            "Unexpected Voice importer state: "
            f"pilot={pilot_applied}/8 fullPolicy={full_policy_applied}/163 legacy={legacy_retained}/0"
        )

    inventory_partitions = summarize_inventory(clips)
    menu_saved = current_bytes - core_bytes
    return {
        "schema": "WarlineCapture.APH407AudioCatalogSplitAnalysis.v2",
        "taskId": "APH-407",
        "recommendation": "DECLINE_OPENING_IMPLEMENTATION_NOW",
        "recommendationGate": "Re-evaluate only if full-policy Android residency evidence proves the accepted memory target is still missed.",
        "evidenceRevisions": {
            "contentInventoryCommit": inventory.get("baselineCommit"),
            "menuCaptureCommit": menu_capture.get("exactCommit"),
            "matchCaptureCommit": match_capture.get("exactCommit"),
            "capturesAreDirty": bool(menu_capture.get("dirty") or match_capture.get("dirty")),
        },
        "sourceHashes": {
            str(path.relative_to(root)): sha256(path) for path in sorted(ownership_files.values())
        } | {
            str((root / SOURCE_CATALOG_PATH.relative_to(ROOT)).relative_to(root)): sha256(root / SOURCE_CATALOG_PATH.relative_to(ROOT)),
            str((root / IMPORT_PROFILES_PATH.relative_to(ROOT)).relative_to(root)): sha256(root / IMPORT_PROFILES_PATH.relative_to(ROOT)),
            str((root / INVENTORY_PATH.relative_to(ROOT)).relative_to(root)): sha256(root / INVENTORY_PATH.relative_to(ROOT)),
            str((root / MENU_CAPTURE_PATH.relative_to(ROOT)).relative_to(root)): sha256(root / MENU_CAPTURE_PATH.relative_to(ROOT)),
            str((root / MATCH_CAPTURE_PATH.relative_to(ROOT)).relative_to(root)): sha256(root / MATCH_CAPTURE_PATH.relative_to(ROOT)),
            "voiceImporterMetaAggregate": importer_digest.hexdigest(),
        },
        "runtimeOwnership": assumptions,
        "catalogPartition": inventory_partitions,
        "measuredResidency": {
            "menuBeforePlayback": {
                "catalogRuntimeMemoryBytes": current_bytes,
                "loadedClipCount": int(menu_baseline["loadedCatalogClipCount"]),
                "partitions": menu_partitions,
            },
            "matchBeforePlayback": {
                "catalogRuntimeMemoryBytes": int(match_baseline["catalogRuntimeMemoryBytes"]),
                "loadedClipCount": int(match_baseline["loadedCatalogClipCount"]),
                "partitions": summarize_snapshot(match_baseline),
            },
        },
        "splitUpperBound": {
            "menuCoreOnlyRuntimeMemoryBytes": core_bytes,
            "menuRuntimeMemoryBytesAvoided": menu_saved,
            "menuRuntimeMemoryReductionPercent": round(menu_saved * 100.0 / current_bytes, 2),
            "boundary": "Measured editor clip runtime bytes reclassified by event semantics; excludes catalog object/dictionary overhead and does not prove unload behavior.",
        },
        "importerEvidence": {
            "pilotClipCount": len(pilot_clips),
            "pilotImporterAppliedCount": pilot_applied,
            "pilotCompressedBytes": sum(int(clip["compressedSizeBytes"]) for clip in pilot_clips),
            "pilotEstimatedDecodedBytes": sum(int(clip["estimatedDecodedSizeBytes"]) for clip in pilot_clips),
            "remainingVoiceDecompressPreloadCount": legacy_retained,
            "fullVoicePolicyAppliedCount": full_policy_applied,
            "pilotAndroidMeasurementAvailable": True,
            "fullVoicePolicyApplied": True,
            "capturePredatesPilot": True,
        },
        "risks": [
            "The persistent Menu scene owns the only runtime view while Match loads additively; scene-owned Match catalogs require explicit registration and teardown.",
            "The bridge caches exactly one catalog and one hash map; multiple catalogs need atomic precedence, duplicate-ID, and hash-collision contracts.",
            "Voice crosses Menu and Match features, so a Voice catalog has no natural single-scene lifetime.",
            "A serialized split alone does not prove AudioClip payload unload; active sources and Unity asset dependencies can retain clips.",
            "On-demand catalog loading can turn accepted ECS requests into missing-event races or first-play stalls.",
            "Catalog builders, parity checks, residency capture, and validation currently assume one 234-event catalog.",
        ],
        "decisionBasis": [
            "Voice owns the dominant measured persistent bytes, so the problem is clip import/load policy rather than lookup metadata.",
            "APH-405 accepted the eight-clip Android pilot and APH-406 promoted the policy to all 163 Voice clips.",
            "Opening a multi-catalog runtime slice before measuring full-policy residency would add lifecycle risk without proving incremental savings over importer policy.",
            "If full-policy Android residency still misses the target, reopen with explicit load/unload ownership and device proof; do not treat asset splitting alone as a memory fix.",
        ],
    }


def mib(value: int) -> str:
    return f"{value / 1_048_576:.2f} MiB"


def render_markdown(report: dict[str, Any]) -> str:
    measured = report["measuredResidency"]["menuBeforePlayback"]
    split = report["splitUpperBound"]
    lines = [
        "# APH-407 Persistent Audio Catalog Split Analysis",
        "",
        "## Recommendation",
        "",
        "**DECLINE opening a catalog-split implementation now.** APH-405 accepted the Android pilot and APH-406 promoted the policy to all Voice clips. Re-evaluate only if full-policy Android residency still misses the accepted memory target.",
        "",
        "## Evidence Boundary",
        "",
        f"- APH-401 capture revision: `{report['evidenceRevisions']['menuCaptureCommit']}`; captures are marked dirty and predate the APH-404 pilot.",
        f"- APH-400 inventory revision: `{report['evidenceRevisions']['contentInventoryCommit']}`; it supplies clip duration/import/size classifications, not post-pilot Android residency.",
        "- Runtime ownership was inspected at the current source revision and is hash-recorded in the companion JSON.",
        "- All 163 current Voice importer metas were inspected and match the accepted on-demand compressed policy; the original eight remain frozen as the APH-405 evidence set.",
        "- Measured residency values are Unity Editor runtime clip memory, not Android release-device memory.",
        "",
        "## Current Ownership",
        "",
        "- One 234-event serialized catalog exists.",
        "- The persistent Menu scene owns the only `AudioPlaybackPresentationRuntimeView` and its catalog reference.",
        "- Match loads additively beneath Menu; Match has no independent audio catalog owner.",
        "- The runtime view and bridge each accept/cache one catalog.",
        "",
        "## Quantified Residency",
        "",
        f"Before controlled Menu playback, {measured['loadedClipCount']} of 234 clips were loaded and catalog clips occupied **{mib(measured['catalogRuntimeMemoryBytes'])}**.",
        "",
        "| Proposed catalog | Clips | Compressed inventory | Estimated decoded inventory | Measured Menu baseline runtime | Loaded clips |",
        "|---|---:|---:|---:|---:|---:|",
    ]
    inventory_by_name = {row["partition"]: row for row in report["catalogPartition"]}
    measured_by_name = {row["partition"]: row for row in measured["partitions"]}
    for name in ("Core/Menu", "Match", "Voice"):
        inventory = inventory_by_name[name]
        runtime = measured_by_name[name]
        lines.append(
            f"| {name} | {inventory['clipCount']} | {mib(inventory['compressedBytes'])} | "
            f"{mib(inventory['estimatedDecodedBytes'])} | {mib(runtime['runtimeMemoryBytes'])} | {runtime['loadedClipCount']} |"
        )
    lines += [
        "",
        f"If only Core/Menu clip references were resident in Menu, the measured classification upper bound is **{mib(split['menuCoreOnlyRuntimeMemoryBytes'])}**, avoiding **{mib(split['menuRuntimeMemoryBytesAvoided'])} ({split['menuRuntimeMemoryReductionPercent']:.2f}%)**. This is an upper bound, not a proven implementation result: it excludes catalog/dictionary overhead and does not prove Unity unloads split dependencies.",
        "",
        "Voice accounts for 163 clips and the dominant measured baseline. The eight pilot clips represent "
        f"{mib(report['importerEvidence']['pilotCompressedBytes'])} compressed inventory versus "
        f"{mib(report['importerEvidence']['pilotEstimatedDecodedBytes'])} estimated decoded PCM. APH-405 recorded passing first-play, repeated-play, glitch-counter, and post-load residency evidence for that set; full-policy Android residency remains the next measurement.",
        "",
        "## Dependency And Lifecycle Risks",
        "",
    ]
    lines.extend(f"- {risk}" for risk in report["risks"])
    lines += [
        "",
        "## Decision Gate",
        "",
        "Do not open implementation from this analysis. Reopen APH-407 only if full-policy Android residency shows that the accepted Voice importer policy still misses the same-device memory target. A reopened slice must first specify catalog acquisition, request queuing while loading, duplicate-event precedence, Match teardown, Voice cross-scene ownership, active-source completion, unload proof, and Android first-play/audible regression gates.",
        "",
        "A split should be declined permanently if full Voice importer rollout meets the memory and latency targets, because Match-only non-Voice clips account for only a small persistent baseline relative to Voice and the split would add runtime ownership complexity for marginal incremental gain.",
        "",
        "## Reproduction",
        "",
        "```sh",
        "python3 Tools/CI/aph407_audio_catalog_split_analysis.py --check",
        "python3 -m unittest Tools.CI.tests.test_aph407_audio_catalog_split_analysis",
        "```",
        "",
    ]
    return "\n".join(lines)


def main() -> int:
    import argparse

    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", help="Fail if tracked reports differ from generated output")
    args = parser.parse_args()
    report = build_report()
    json_text = json.dumps(report, indent=2, sort_keys=True) + "\n"
    markdown_text = render_markdown(report)
    if args.check:
        if OUTPUT_JSON.read_text(encoding="utf-8") != json_text:
            raise SystemExit(f"Stale report: {OUTPUT_JSON.relative_to(ROOT)}")
        if OUTPUT_MARKDOWN.read_text(encoding="utf-8") != markdown_text:
            raise SystemExit(f"Stale report: {OUTPUT_MARKDOWN.relative_to(ROOT)}")
        return 0
    OUTPUT_JSON.write_text(json_text, encoding="utf-8")
    OUTPUT_MARKDOWN.write_text(markdown_text, encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

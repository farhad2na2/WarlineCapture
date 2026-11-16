#!/usr/bin/env python3
"""Create an S073 r32 successor from the exact r31-final2 lineage and new water captures.

This tool replaces only the four native water descriptors. The qualified capture
finalizers recheck their exact source-pin sets against the current checkout.
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import tempfile


ROOT = Path(__file__).resolve().parents[4]
PARENT_DIR = Path("/private/tmp/s073-final-bundle-r31-final2")
PARENT_BUNDLE = PARENT_DIR / "data-bundle-r31.json"
PARENT_RECEIPT = PARENT_DIR / "bundle-rebind-r31-receipt.json"
PARENT_BUNDLE_SHA256 = "e2d0b30ceb1885ac2fb6abcfec7c1fb62b9334a9cea4c2685559c3a90d1ee262"
PARENT_RECEIPT_SHA256 = "5b3fc978a64fe30f0eb44568c1cca6091b3c790c9c8f50f542abf37a2837b0a8"
EXPECTED_HISTORY = {
    "unresolved_count": 7,
    "qualification_effect": "Source-execution qualification remains unclaimed until historical binary content is verified.",
}
WATER_KEYS = {
    "native_water_capture_receipt",
    "native_water_capture_snapshot",
    "native_water_capture_mask",
    "native_water_startup_parity_receipt",
}
FALSE_READINESS_FIELDS = (
    "source_execution_qualified", "source_fit_certified", "mission_ready",
    "normal_input_playthrough", "player_readiness",
)


def sha(path: Path | bytes) -> str:
    data = path if isinstance(path, bytes) else Path(path).read_bytes()
    return hashlib.sha256(data).hexdigest()


def need(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def read(path: Path) -> dict:
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("duplicate JSON key: " + key)
            result[key] = value
        return result
    return json.loads(Path(path).read_text(encoding="utf-8"), object_pairs_hook=unique)


def descriptor(path: Path) -> dict:
    path = Path(path)
    need(not path.is_symlink(), "evidence artifact must not be a symlink: " + str(path))
    path = path.resolve(strict=True)
    need(path.is_file() and not path.is_symlink(), "regular evidence file required: " + str(path))
    return {"path": str(path), "sha256": sha(path)}


def verify_descriptor(value: dict, label: str, allowed_roots=(ROOT, Path("/private/tmp"))) -> dict:
    need(isinstance(value, dict) and set(value) == {"path", "sha256"}, "malformed descriptor: " + label)
    raw = Path(value["path"])
    need(not raw.is_symlink(), "descriptor artifact must not be a symlink: " + label)
    path = raw.resolve(strict=True)
    need(path.is_file() and not path.is_symlink(), "regular descriptor artifact required: " + label)
    roots = [Path(root).resolve(strict=True) for root in allowed_roots]
    need(any(path.is_relative_to(root) for root in roots), "descriptor escaped authorized roots: " + label)
    need(re.fullmatch(r"[0-9a-f]{64}", str(value["sha256"])) is not None and
         sha(path) == value["sha256"], "descriptor artifact hash mismatch: " + label)
    return {"path": str(path), "sha256": value["sha256"]}


def validate_parent(bundle_path=PARENT_BUNDLE, receipt_path=PARENT_RECEIPT):
    bundle_path, receipt_path = Path(bundle_path), Path(receipt_path)
    need(not bundle_path.is_symlink() and not receipt_path.is_symlink(), "r31 lineage inputs must not be symlinks")
    bundle_path, receipt_path = bundle_path.resolve(strict=True), receipt_path.resolve(strict=True)
    need(bundle_path == PARENT_BUNDLE.resolve(strict=True) and sha(bundle_path) == PARENT_BUNDLE_SHA256,
         "exact retained r31-final2 parent bundle required")
    need(receipt_path == PARENT_RECEIPT.resolve(strict=True) and sha(receipt_path) == PARENT_RECEIPT_SHA256,
         "exact retained r31-final2 lineage receipt required")
    bundle, receipt = read(bundle_path), read(receipt_path)
    need(bundle.get("schema_version") == 1 and bundle.get("kind") == "S073LegacyStartupBundle" and
         bundle.get("mission") == "S073" and bundle.get("seed") == 104729 and
         bundle.get("source_repository_root") == str(ROOT.resolve()), "S073 r31 parent identity mismatch")
    need(receipt.get("kind") == "S073FinalSourceBundleRebindReceipt" and receipt.get("status") == "Passed" and
         receipt.get("output_bundle") == descriptor(bundle_path) and
         receipt.get("historical_binary_limitation") == EXPECTED_HISTORY and
         receipt.get("source_execution_qualified") is False and receipt.get("mission_ready") is False,
         "passed r31 parent lineage and unresolved historical binary caveat required")
    all_descriptors = {}
    for key, value in bundle.items():
        if isinstance(value, dict) and "path" in value:
            all_descriptors[key] = verify_descriptor(value, key)
    need(len(all_descriptors) == 30 and WATER_KEYS <= set(all_descriptors),
         "unexpected r31 parent descriptor inventory")
    replaced = receipt.get("replaced_descriptors")
    preserved = receipt.get("preserved_descriptors")
    need(isinstance(replaced, dict) and len(replaced) == 15 and
         isinstance(preserved, dict) and len(preserved) == 15,
         "exact r31 replaced/preserved descriptor lineage required")
    need({key: all_descriptors[key] for key in replaced} == replaced and
         {key: all_descriptors[key] for key in preserved} == preserved and
         set(replaced).isdisjoint(preserved) and set(replaced) | set(preserved) == set(all_descriptors),
         "r31 receipt descriptor inventory does not match exact parent bundle")
    prior_bundle = verify_descriptor(receipt.get("source_bundle"), "r31 source bundle")
    prior_receipt = verify_descriptor(receipt.get("source_bundle_receipt"), "r31 source bundle receipt")
    need(prior_bundle["sha256"] == "94ef7bedc5b304ea2dd1c3e29717fded148cb1b7d39b7161c4e023e2d08b9a45" and
         prior_receipt["sha256"] == "bf2083c5ba5157e9e170a7084ba0c0b725f8dbbcbeddde4720e210174ce38c31",
         "r31 parent source lineage changed")
    return bundle, receipt, all_descriptors


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


def validate_new_water(repo: Path, water_directory: Path, startup_observation: Path,
                       startup_mask: Path, wrapper_exit: int) -> dict:
    repo = Path(repo).resolve(strict=True)
    water_directory = Path(water_directory).resolve(strict=True)
    startup_observation = Path(startup_observation).resolve(strict=True)
    startup_mask = Path(startup_mask).resolve(strict=True)
    need(type(wrapper_exit) is int and wrapper_exit == 0,
         "checked Unity wrapper must exit successfully before r32 rebind")
    snapshot_path = water_directory / "snapshot.json"
    mask_path = water_directory / "snapshot.water.bin"
    water_receipt_path = water_directory / "water-receipt.json"
    joint_receipt_path = water_directory / "joint-receipt.json"
    log_path = water_directory / "editor.log"
    for path, label in ((snapshot_path, "Playing snapshot"), (mask_path, "Playing mask"),
                         (water_receipt_path, "Playing water receipt"),
                         (joint_receipt_path, "joint water receipt"), (log_path, "shared Editor log"),
                         (startup_observation, "startup observation"), (startup_mask, "startup mask")):
        verify_descriptor(descriptor(path), label)

    snapshot = read(snapshot_path)
    startup_data = read(startup_observation)
    water_receipt = read(water_receipt_path)
    joint_receipt = read(joint_receipt_path)
    startup_pins = startup_data.get("source_pins_sha256")
    need(isinstance(startup_pins, dict) and startup_pins,
         "startup observation source pins are required")
    need(snapshot.get("kind") == "S073NativeWaterMaskObservation" and
         snapshot.get("phase") == "Playing" and snapshot.get("mission_ready") is False and
         snapshot.get("source_fit_certified") is False, "qualified Playing observation required")
    need(water_receipt.get("kind") == "S073NativeWaterMaskCaptureReceipt" and
         water_receipt.get("validation_status") == "Passed" and
         water_receipt.get("wrapper_exit_code") == 0 and
         water_receipt.get("required_marker_present") is True and
         water_receipt.get("mission_ready") is False, "qualified Playing water receipt required")
    need(water_receipt.get("session_id") == snapshot.get("session_id") and
         water_receipt.get("mask_sha256") == sha(mask_path) and
         water_receipt.get("editor_log_sha256") == sha(log_path) and
         water_receipt.get("source_pins_sha256") == snapshot.get("source_pins_sha256"),
         "Playing receipt does not bind exact current snapshot, mask, log and source pins")
    original_playing_observation = Path(water_receipt.get("original_observation_path", "")).resolve(strict=True)
    original_playing_mask = original_playing_observation.with_suffix(".water.bin")
    verify_descriptor(descriptor(original_playing_observation), "original Playing observation")
    verify_descriptor(descriptor(original_playing_mask), "original Playing mask")
    need(joint_receipt.get("playing_capture", {}).get("observation_path") == str(original_playing_observation) and
         joint_receipt.get("playing_capture", {}).get("mask_path") == str(original_playing_mask.resolve()),
         "joint receipt does not identify the original Playing observation and finalized mask")
    need(joint_receipt.get("kind") == "S073JointNativeWaterMaskComparison" and
         joint_receipt.get("evidence_validation") == "Passed" and
         joint_receipt.get("parity_status") == "Match" and joint_receipt.get("masks_match") is True and
         joint_receipt.get("differing_cells") == 0 and joint_receipt.get("wrapper_exit_code") == 0 and
         joint_receipt.get("mission_ready") is False and joint_receipt.get("source_fit_certified") is False,
         "qualified same-session startup/Playing water parity receipt required")
    need(joint_receipt.get("session_id") == snapshot.get("session_id") == water_receipt.get("session_id") and
         joint_receipt.get("editor_log_sha256") == sha(log_path) and
         joint_receipt.get("playing_capture", {}).get("receipt_sha256") == sha(water_receipt_path) and
         joint_receipt.get("playing_capture", {}).get("mask_sha256") == sha(mask_path) and
         joint_receipt.get("startup_capture", {}).get("observation_sha256") == sha(startup_observation.read_bytes()) and
         joint_receipt.get("startup_capture", {}).get("mask_sha256") == sha(startup_mask.read_bytes()),
         "joint receipt does not bind the exact new same-session captures")

    water_finalizer = load_module(Path(__file__).with_name("finalize_s073_native_water_capture.py"),
                                  "s073_water_finalizer_r32")
    joint_finalizer = load_module(Path(__file__).with_name("finalize_s073_joint_native_water_capture.py"),
                                  "s073_joint_water_finalizer_r32")
    with tempfile.TemporaryDirectory(prefix="s073-water-r32-validate-") as temporary:
        checked_water = water_finalizer.validate(repo, Path(temporary), wrapper_exit,
                                                 original_playing_observation, log_path, original_playing_mask)
    checked_joint = joint_finalizer.validate(repo, startup_observation, startup_mask, water_directory,
                                             wrapper_exit, log_path, None, snapshot_path, mask_path)
    need(checked_water.get("validation_status") == "Passed" and
         checked_water.get("provider_assembly_sha256") == water_receipt.get("provider_assembly_sha256") and
         checked_water.get("source_pins_sha256") == snapshot.get("source_pins_sha256"),
         "current Playing water capture source/provider validation failed")
    need(checked_joint.get("evidence_validation") == "Passed" and
         checked_joint.get("parity_status") == "Match" and checked_joint.get("differing_cells") == 0 and
         checked_joint.get("session_id") == joint_receipt.get("session_id") and
         checked_joint.get("shared_source_pins_sha256") == joint_receipt.get("shared_source_pins_sha256"),
         "current joint water source/session validation failed")
    return {
        "native_water_capture_receipt": descriptor(water_receipt_path),
        "native_water_capture_snapshot": descriptor(snapshot_path),
        "native_water_capture_mask": descriptor(mask_path),
        "native_water_startup_parity_receipt": descriptor(joint_receipt_path),
        "water_capture_validation": {
            "validation_status": checked_water["validation_status"],
            "provider_assembly_path": checked_water["provider_assembly_path"],
            "provider_assembly_sha256": checked_water["provider_assembly_sha256"],
            "observation_sha256": checked_water["observation_sha256"],
            "mask_sha256": checked_water["mask_sha256"],
            "source_pins_sha256": checked_water["source_pins_sha256"],
        },
        "joint_capture_validation": checked_joint,
        "current_source_pins": {
            "startup": startup_pins,
            "playing": checked_water["source_pins_sha256"],
            "shared": checked_joint["shared_source_pins_sha256"],
        },
    }


def apply_water_replacements(base: dict, replacements: dict) -> dict:
    need(set(replacements) == WATER_KEYS, "replacement descriptor set mismatch")
    rebound = copy.deepcopy(base)
    for key in WATER_KEYS:
        rebound[key] = replacements[key]
    need(all(rebound[key] == base[key] for key in base if key not in WATER_KEYS),
         "non-water r31 descriptor changed")
    return rebound


def rebind(water_directory: Path, startup_observation: Path, startup_mask: Path,
           output_directory: Path, wrapper_exit: int = 0) -> dict:
    output_directory = Path(output_directory).absolute()
    need(not output_directory.exists(), "fresh r32 output directory required")
    base, parent_receipt, parent_descriptors = validate_parent(PARENT_BUNDLE, PARENT_RECEIPT)
    inputs = validate_new_water(ROOT, water_directory, startup_observation, startup_mask, wrapper_exit)
    replacements = {key: inputs[key] for key in WATER_KEYS}
    bundle = apply_water_replacements(base, replacements)
    need(all(bundle[key] == base[key] for key in parent_descriptors if key not in WATER_KEYS),
         "non-water r31 descriptor changed")

    output_directory.mkdir(parents=True, exist_ok=False)
    bundle_path = output_directory / "data-bundle-r32.json"
    receipt_path = output_directory / "bundle-rebind-r32-receipt.json"
    with bundle_path.open("x", encoding="utf-8") as stream:
        json.dump(bundle, stream, indent=2)
        stream.write("\n")
    receipt = {
        "kind": "S073FinalSourceBundleRebindReceipt", "schema_version": 1, "status": "Passed",
        "source_bundle": descriptor(PARENT_BUNDLE), "source_bundle_receipt": descriptor(PARENT_RECEIPT),
        "source_bundle_sha256_required": PARENT_BUNDLE_SHA256,
        "output_bundle": descriptor(bundle_path), "replaced_descriptors": replacements,
        "replaced_descriptor_count": len(WATER_KEYS),
        "preserved_descriptor_count": len(parent_descriptors) - len(WATER_KEYS),
        "preserved_descriptors": {key: value for key, value in parent_descriptors.items() if key not in WATER_KEYS},
        "current_source_receipts": {
            "water_capture": descriptor(Path(water_directory) / "water-receipt.json"),
            "water_joint_parity": descriptor(Path(water_directory) / "joint-receipt.json"),
        },
        "water_capture_validation": inputs["water_capture_validation"],
        "joint_capture_validation": inputs["joint_capture_validation"],
        "current_source_pins": inputs["current_source_pins"],
        "historical_binary_limitation": copy.deepcopy(parent_receipt["historical_binary_limitation"]),
        "source_execution_qualified": False, "source_fit_certified": False,
        "mission_ready": False, "normal_input_playthrough": False, "player_readiness": False,
        "native_capture_rewritten": False,
    }
    with receipt_path.open("x", encoding="utf-8") as stream:
        json.dump(receipt, stream, indent=2, sort_keys=True)
        stream.write("\n")
    return {"status": "Passed", "bundle": str(bundle_path), "bundle_sha256": sha(bundle_path),
            "receipt": str(receipt_path), "replaced_descriptor_count": len(WATER_KEYS),
            "preserved_descriptor_count": len(parent_descriptors) - len(WATER_KEYS),
            "source_execution_qualified": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--water-directory", type=Path, required=True,
                        help="Finalized directory with snapshot, water receipt, mask, joint receipt and shared Editor log")
    parser.add_argument("--startup-observation", type=Path, required=True)
    parser.add_argument("--startup-mask", type=Path, required=True)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    parser.add_argument("--output-directory", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(rebind(args.water_directory, args.startup_observation, args.startup_mask,
                            args.output_directory, args.wrapper_exit), indent=2))


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Rebind only the qualified S073 native water descriptors to the current capture."""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
SOURCE_BUNDLE = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/data-bundle-native-ai-refreshed-r17.json"
SOURCE_BUNDLE_SHA256 = "ec3eed775c311949aa9d840dc1ea655a0cf44bcda91564afc566551a5485a1d7"
WATER_DIRECTORY = ROOT / "Logs/SkirmishProduction/s073-native-startup-playing-water-20261101-r5"
REPLACED = {
    "native_water_capture_receipt",
    "native_water_capture_snapshot",
    "native_water_capture_mask",
    "native_water_startup_parity_receipt",
}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def descriptor(path):
    path = Path(path).resolve(strict=True)
    return {"path": str(path), "sha256": sha(path)}


def load_module(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def rebind(repo, source_bundle, water_directory, output, receipt_output):
    repo = Path(repo).resolve(strict=True)
    source_bundle = Path(source_bundle).resolve(strict=True)
    water_directory = Path(water_directory).resolve(strict=True)
    output = Path(output).absolute()
    receipt_output = Path(receipt_output).absolute()
    if source_bundle != SOURCE_BUNDLE.resolve(strict=True) or sha(source_bundle) != SOURCE_BUNDLE_SHA256:
        raise ValueError("Exact retained r17 source bundle required")
    if water_directory != WATER_DIRECTORY.resolve(strict=True):
        raise ValueError("Only the exact current-source r5 native water capture is admitted")
    if output.exists() or receipt_output.exists():
        raise ValueError("Fresh output bundle and rebind receipt paths required")

    bundle = json.loads(source_bundle.read_text(encoding="utf-8"))
    if (bundle.get("schema_version") != 1 or bundle.get("kind") != "S073LegacyStartupBundle" or
            bundle.get("mission") != "S073" or bundle.get("seed") != 104729 or
            not REPLACED.issubset(bundle)):
        raise ValueError("Exact S073 r17 bundle with complete native water descriptors required")

    # Every non-water descriptor, including native AI and authored source graph,
    # remains byte-for-byte pinned to the retained input bundle.
    preserved = {}
    for name, value in bundle.items():
        if name in REPLACED or not isinstance(value, dict) or "path" not in value:
            continue
        path = Path(value["path"]).resolve(strict=True)
        if not path.is_file() or sha(path) != value.get("sha256"):
            raise ValueError("Retained independent S073 input changed: " + name)
        preserved[name] = value["sha256"]

    startup_observation = Path("/private/tmp/warline-s073-start-water-20261101-r5.json")
    startup_mask = Path("/private/tmp/warline-s073-start-water-20261101-r5.water.bin")
    playing_observation = Path("/private/tmp/warline-s073-play-water-20261101-r5.json")
    playing_mask = Path("/private/tmp/warline-s073-play-water-20261101-r5.water.bin")
    water_finalizer = load_module(Path(__file__).with_name("finalize_s073_native_water_capture.py"), "s073_water_finalizer")
    joint_finalizer = load_module(Path(__file__).with_name("finalize_s073_joint_native_water_capture.py"), "s073_joint_water_finalizer")
    water_result = water_finalizer.validate(repo, water_directory, 0, playing_observation,
                                            water_directory / "editor.log", playing_mask)
    joint_result = joint_finalizer.validate(repo, startup_observation, startup_mask,
                                            water_directory, 0, None, None,
                                            playing_observation, playing_mask)
    if (water_result.get("validation_status") != "Passed" or
            joint_result.get("evidence_validation") != "Passed" or
            joint_result.get("parity_status") != "Match" or joint_result.get("differing_cells") != 0):
        raise ValueError("Current-source native water and same-session parity finalizers must both pass")

    replacements = {
        "native_water_capture_receipt": descriptor(water_directory / "water-receipt.json"),
        "native_water_capture_snapshot": descriptor(water_directory / "snapshot.json"),
        "native_water_capture_mask": descriptor(water_directory / "snapshot.water.bin"),
        "native_water_startup_parity_receipt": descriptor(water_directory / "joint-receipt.json"),
    }
    rebound = copy.deepcopy(bundle)
    rebound.update(replacements)
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x", encoding="utf-8") as stream:
        json.dump(rebound, stream, indent=2)
        stream.write("\n")

    receipt = {
        "kind": "S073NativeWaterBundleRebindReceipt",
        "schema_version": 1,
        "status": "Passed",
        "source_bundle_path": str(source_bundle),
        "source_bundle_sha256": SOURCE_BUNDLE_SHA256,
        "output_bundle_path": str(output.resolve()),
        "output_bundle_sha256": sha(output),
        "preserved_descriptor_count": len(preserved),
        "preserved_descriptors_sha256": preserved,
        "replaced_water_descriptors": replacements,
        "water_capture_validation": "Passed",
        "same_session_startup_playing_water_parity": joint_result["parity_status"],
        "water_mask_sha256": water_result["mask_sha256"],
        "session_id": joint_result["session_id"],
        "mission_ready": False,
        "source_fit_certified": False,
        "normal_input_playthrough": False,
        "player_readiness": False,
    }
    receipt_output.parent.mkdir(parents=True, exist_ok=True)
    with receipt_output.open("x", encoding="utf-8") as stream:
        json.dump(receipt, stream, indent=2)
        stream.write("\n")
    return {"status": "Passed", "bundle": str(output.resolve()),
            "bundle_sha256": sha(output), "parity_status": joint_result["parity_status"],
            "preserved_descriptor_count": len(preserved)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--source-bundle", required=True, type=Path)
    parser.add_argument("--water-directory", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--receipt-output", required=True, type=Path)
    args = parser.parse_args()
    print(json.dumps(rebind(args.repo, args.source_bundle, args.water_directory,
                            args.output, args.receipt_output)))


if __name__ == "__main__":
    main()

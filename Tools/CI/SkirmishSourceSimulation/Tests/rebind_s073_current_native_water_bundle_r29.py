#!/usr/bin/env python3
"""Rebind only S073 native water inputs after strict passive blocker capture validation."""
from __future__ import annotations

import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile


ROOT = Path(__file__).resolve().parents[4]
SOURCE_BUNDLE = ROOT / "Logs/SkirmishProduction/s073-native-legacy-definitions-20261102-adapter-r3/data-bundle-r28.json"
SOURCE_BUNDLE_SHA256 = "74b80ce9e332219c0d1e2e7806b800e74f3ffea3d1728dae9cd6bdeafa14ed7b"
LINEAGE_RECEIPT = ROOT / "Logs/SkirmishProduction/s073-native-legacy-definitions-20261102-adapter-r3/bundle-rebind-r28-receipt.json"
WATER_DIRECTORY = ROOT / "Logs/SkirmishProduction/s073-native-playing-occupancy-20261102-r3-finalized-r2"
STARTUP_OBSERVATION = Path("/private/tmp/s073-native-playing-occupancy-20261102-r3-start.json")
STARTUP_MASK = Path("/private/tmp/s073-native-playing-occupancy-20261102-r3-start.water.bin")
PLAYING_OBSERVATION = Path("/private/tmp/s073-native-playing-occupancy-20261102-r3-playing.json")
PLAYING_MASK = Path("/private/tmp/s073-native-playing-occupancy-20261102-r3-playing.water.bin")
REPLACED = {
    "native_water_capture_receipt",
    "native_water_capture_snapshot",
    "native_water_capture_mask",
    "native_water_startup_parity_receipt",
}


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def descriptor(path: Path) -> dict:
    path = path.resolve(strict=True)
    return {"path": str(path), "sha256": sha(path)}


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


def rebind(repo: Path, output: Path, receipt_output: Path) -> dict:
    repo = repo.resolve(strict=True)
    output, receipt_output = output.absolute(), receipt_output.absolute()
    if SOURCE_BUNDLE.resolve(strict=True) != SOURCE_BUNDLE or sha(SOURCE_BUNDLE) != SOURCE_BUNDLE_SHA256:
        raise ValueError("Exact retained r28 S073 source bundle required")
    lineage = json.loads(LINEAGE_RECEIPT.read_text(encoding="utf-8"))
    if (lineage.get("status") != "Passed" or
            lineage.get("output_bundle", {}).get("path") != str(SOURCE_BUNDLE.resolve()) or
            lineage.get("output_bundle", {}).get("sha256") != SOURCE_BUNDLE_SHA256 or
            lineage.get("unresolved_historical_binary_count") != 7):
        raise ValueError("Exact r28 lineage with unresolved historical binary caveat required")
    if output.exists() or receipt_output.exists():
        raise ValueError("Fresh r29 bundle, parity receipt, and rebind receipt paths required")

    bundle = json.loads(SOURCE_BUNDLE.read_text(encoding="utf-8"))
    if (bundle.get("schema_version") != 1 or bundle.get("kind") != "S073LegacyStartupBundle" or
            bundle.get("mission") != "S073" or bundle.get("seed") != 104729 or
            not REPLACED.issubset(bundle)):
        raise ValueError("Exact S073 r28 bundle with all native water descriptors required")

    preserved = {}
    for name, value in bundle.items():
        if name in REPLACED or not isinstance(value, dict) or "path" not in value:
            continue
        path = Path(value["path"]).resolve(strict=True)
        if not path.is_file() or sha(path) != value.get("sha256"):
            raise ValueError("Retained independent S073 input changed: " + name)
        preserved[name] = {"path": str(path), "sha256": value["sha256"]}

    water_finalizer = load_module(Path(__file__).with_name("finalize_s073_native_water_capture.py"), "s073_water_finalizer_r29")
    joint_finalizer = load_module(Path(__file__).with_name("finalize_s073_joint_native_water_capture.py"), "s073_joint_water_finalizer_r29")
    with tempfile.TemporaryDirectory(prefix="s073-water-finalize-r29-") as temporary:
        water_result = water_finalizer.validate(repo, Path(temporary), 0, PLAYING_OBSERVATION,
                                                WATER_DIRECTORY / "editor.log", PLAYING_MASK)
    joint_result = joint_finalizer.validate(repo, STARTUP_OBSERVATION, STARTUP_MASK,
                                            WATER_DIRECTORY, 0, WATER_DIRECTORY / "editor.log", None,
                                            WATER_DIRECTORY / "snapshot.json", PLAYING_MASK)
    if (water_result.get("validation_status") != "Passed" or
            water_result.get("blocker_occupancy_raw_capture", {}).get("raw", {}).get("sha256") !=
            descriptor(WATER_DIRECTORY / "blocker-occupancy.bin")["sha256"] or
            joint_result.get("evidence_validation") != "Passed" or
            joint_result.get("parity_status") != "Match" or joint_result.get("differing_cells") != 0):
        raise ValueError("Current native water/raw capture and same-session parity must pass")

    parity_output = WATER_DIRECTORY / "joint-receipt.json"
    if not parity_output.is_file():
        raise ValueError("Qualified finalized-r2 joint water receipt required")
    persisted_joint = json.loads(parity_output.read_text(encoding="utf-8"))
    for field in ("evidence_validation", "parity_status", "differing_cells", "session_id", "playing_capture"):
        if persisted_joint.get(field) != joint_result.get(field):
            raise ValueError("Persisted finalized-r2 joint receipt differs from strict revalidation: " + field)

    replacements = {
        "native_water_capture_receipt": descriptor(WATER_DIRECTORY / "water-receipt.json"),
        "native_water_capture_snapshot": descriptor(WATER_DIRECTORY / "snapshot.json"),
        "native_water_capture_mask": descriptor(WATER_DIRECTORY / "snapshot.water.bin"),
        "native_water_startup_parity_receipt": descriptor(parity_output),
    }
    rebound = copy.deepcopy(bundle)
    rebound.update(replacements)
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x", encoding="utf-8") as stream:
        json.dump(rebound, stream, indent=2)
        stream.write("\n")

    receipt = {
        "kind": "S073NativeWaterBlockerBundleRebindReceipt",
        "schema_version": 1,
        "status": "Passed",
        "source_bundle": descriptor(SOURCE_BUNDLE),
        "r28_lineage_receipt": descriptor(LINEAGE_RECEIPT),
        "output_bundle": descriptor(output),
        "preserved_descriptor_count": len(preserved),
        "preserved_descriptors": preserved,
        "replaced_water_descriptors": replacements,
        "native_water_validation": "Passed",
        "native_blocker_occupancy_raw_validation": "Passed",
        "same_session_startup_playing_water_parity": joint_result["parity_status"],
        "water_mask_sha256": water_result["mask_sha256"],
        "blocker_occupancy_raw_sha256": water_result["blocker_occupancy_raw_capture"]["raw"]["sha256"],
        "session_id": joint_result["session_id"],
        "historical_binary_limitation": {
            "unresolved_count": 7,
            "qualification_effect": "Source-execution qualification remains unclaimed until historical binary content is verified.",
        },
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
            "blocker_occupancy_raw_sha256": receipt["blocker_occupancy_raw_sha256"],
            "preserved_descriptor_count": len(preserved), "unresolved_historical_binary_count": 7}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--receipt-output", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(rebind(args.repo, args.output, args.receipt_output)))


if __name__ == "__main__":
    main()

"""Rebind only water descriptors in the qualified S073 r24 bundle to current r1 evidence."""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
SOURCE_BUNDLE = ROOT / "Logs/SkirmishProduction/s073-native-legacy-definitions-20261102-adapter-r2/data-bundle-r24.json"
SOURCE_BUNDLE_SHA256 = "cc414d90a512b038effbd9db5c09d4754a7421df4e355f34857aadc6ed6999f8"
SOURCE_REBIND_RECEIPT = ROOT / "Logs/SkirmishProduction/s073-native-legacy-definitions-20261102-adapter-r2/rebind-receipt.json"
SOURCE_REBIND_RECEIPT_SHA256 = "fc8c531034a14771653672a36b36394ae446cb87a79d3b46a7af45f7f37328a3"
WATER_DIRECTORY = ROOT / "Logs/SkirmishProduction/s073-native-startup-playing-water-20261102-r1"
STARTUP_OBSERVATION = Path("/private/tmp/s073-water-current-20261102-r1-start.json")
STARTUP_MASK = Path("/private/tmp/s073-water-current-20261102-r1-start.water.bin")
PLAYING_OBSERVATION = Path("/private/tmp/s073-water-current-20261102-r1-playing.json")
PLAYING_MASK = Path("/private/tmp/s073-water-current-20261102-r1-playing.water.bin")
REPLACED = {
    "native_water_capture_receipt",
    "native_water_capture_snapshot",
    "native_water_capture_mask",
    "native_water_startup_parity_receipt",
}


def sha_bytes(data):
    return hashlib.sha256(data).hexdigest()


def sha(path):
    return sha_bytes(Path(path).read_bytes())


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
    output, receipt_output = Path(output).absolute(), Path(receipt_output).absolute()
    if source_bundle != SOURCE_BUNDLE.resolve(strict=True) or sha(source_bundle) != SOURCE_BUNDLE_SHA256:
        raise ValueError("Exact qualified r24 bundle required")
    if sha(SOURCE_REBIND_RECEIPT) != SOURCE_REBIND_RECEIPT_SHA256:
        raise ValueError("Qualified r24 adapter receipt changed")
    lineage = json.loads(SOURCE_REBIND_RECEIPT.read_text(encoding="utf-8"))
    relocation = lineage.get("campaign_relocation_audit", {})
    rebound_lineage = lineage.get("rebound_bundle", {})
    if (lineage.get("validation_status") != "Passed" or rebound_lineage.get("sha256") != SOURCE_BUNDLE_SHA256 or
            relocation.get("unresolved_historical_binary_count") != 7 or
            relocation.get("unresolved_non_historical_pin_count") != 0):
        raise ValueError("Exact r24 lineage and historical binary caveat required")
    if water_directory != WATER_DIRECTORY.resolve(strict=True):
        raise ValueError("Only the exact current-source r1 water capture is admitted")
    if output.exists() or receipt_output.exists():
        raise ValueError("Fresh create-new r25 bundle and receipt paths required")

    bundle = json.loads(source_bundle.read_text(encoding="utf-8"))
    if (bundle.get("schema_version") != 1 or bundle.get("kind") != "S073LegacyStartupBundle" or
            bundle.get("mission") != "S073" or bundle.get("seed") != 104729 or
            not REPLACED.issubset(bundle)):
        raise ValueError("Exact S073 r24 bundle with all native water descriptors required")

    preserved = {}
    for name, value in bundle.items():
        if name in REPLACED or not isinstance(value, dict) or "path" not in value:
            continue
        path = Path(value["path"]).resolve(strict=True)
        if not path.is_file() or sha(path) != value.get("sha256"):
            raise ValueError("Retained independent S073 input changed: " + name)
        preserved[name] = value["sha256"]

    water_finalizer = load_module(Path(__file__).with_name("finalize_s073_native_water_capture.py"), "s073_water_finalizer_r25")
    joint_finalizer = load_module(Path(__file__).with_name("finalize_s073_joint_native_water_capture.py"), "s073_joint_water_finalizer_r25")
    water_result = water_finalizer.validate(repo, water_directory, 0, PLAYING_OBSERVATION,
                                            water_directory / "editor.log", PLAYING_MASK)
    joint_result = joint_finalizer.validate(repo, STARTUP_OBSERVATION, STARTUP_MASK,
                                            water_directory, 0, water_directory / "editor.log", None,
                                            PLAYING_OBSERVATION, PLAYING_MASK)
    if (water_result.get("validation_status") != "Passed" or
            joint_result.get("evidence_validation") != "Passed" or
            joint_result.get("parity_status") != "Match" or joint_result.get("differing_cells") != 0):
        raise ValueError("Current-source water and same-session parity finalizers must both pass")

    replacements = {
        "native_water_capture_receipt": descriptor(water_directory / "water-receipt.json"),
        "native_water_capture_snapshot": descriptor(PLAYING_OBSERVATION),
        "native_water_capture_mask": descriptor(PLAYING_MASK),
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
        "source_rebind_receipt_path": str(SOURCE_REBIND_RECEIPT.resolve()),
        "source_rebind_receipt_sha256": SOURCE_REBIND_RECEIPT_SHA256,
        "output_bundle_path": str(output.resolve()),
        "output_bundle_sha256": sha(output),
        "preserved_descriptor_count": len(preserved),
        "preserved_descriptors_sha256": preserved,
        "replaced_water_descriptors": replacements,
        "water_capture_validation": "Passed",
        "same_session_startup_playing_water_parity": joint_result["parity_status"],
        "water_mask_sha256": water_result["mask_sha256"],
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
            "preserved_descriptor_count": len(preserved),
            "historical_binary_limitation_count": 7}


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

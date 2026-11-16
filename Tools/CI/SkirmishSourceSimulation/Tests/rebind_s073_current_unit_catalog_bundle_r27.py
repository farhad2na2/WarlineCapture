"""Rebind only the qualified current S073 unit-catalog receipt into r26."""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
SOURCE_BUNDLE = ROOT / "Logs/SkirmishProduction/s073-native-ai-rebind-20261102-r1/data-bundle-r26.json"
SOURCE_BUNDLE_SHA256 = "ada10c3ccd1f5b0a9444a681bcfb269eabc80f085c688026886aaf952cdd6b00"
SOURCE_WATER_RECEIPT = ROOT / "Logs/SkirmishProduction/s073-native-water-rebind-20261102-r25/rebind-receipt.json"
SOURCE_WATER_RECEIPT_SHA256 = "f6320288e80127ba014a8ffb84061b6d2d3b3659cfc1ecfa86e947e630254b0d"
LEGACY_ADAPTER_RECEIPT = ROOT / "Logs/SkirmishProduction/s073-native-legacy-definitions-20261102-adapter-r2/rebind-receipt.json"
LEGACY_ADAPTER_RECEIPT_SHA256 = "fc8c531034a14771653672a36b36394ae446cb87a79d3b46a7af45f7f37328a3"
UNIT_DIRECTORY = ROOT / "Logs/SkirmishProduction/s073-current-unit-catalog-20261102-r1"
REPLACED = "native_configured_unit_receipt"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def descriptor(path):
    path = Path(path).resolve(strict=True)
    return {"path": str(path), "sha256": sha(path)}


def load_finalizer():
    path = Path(__file__).with_name("finalize_s073_unit_catalog.py")
    spec = importlib.util.spec_from_file_location("s073_unit_catalog_finalizer_r27", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def rebind(repo, source_bundle, unit_directory, output, receipt_output):
    repo = Path(repo).resolve(strict=True)
    source_bundle = Path(source_bundle).resolve(strict=True)
    unit_directory = Path(unit_directory).resolve(strict=True)
    output, receipt_output = Path(output).absolute(), Path(receipt_output).absolute()
    if source_bundle != SOURCE_BUNDLE.resolve(strict=True) or sha(source_bundle) != SOURCE_BUNDLE_SHA256:
        raise ValueError("Exact qualified r26 input bundle required")
    if sha(SOURCE_WATER_RECEIPT) != SOURCE_WATER_RECEIPT_SHA256:
        raise ValueError("Qualified r25 water rebind receipt changed")
    if sha(LEGACY_ADAPTER_RECEIPT) != LEGACY_ADAPTER_RECEIPT_SHA256:
        raise ValueError("Qualified r24 legacy adapter receipt changed")
    water_lineage = json.loads(SOURCE_WATER_RECEIPT.read_text(encoding="utf-8"))
    legacy_lineage = json.loads(LEGACY_ADAPTER_RECEIPT.read_text(encoding="utf-8"))
    caveat = legacy_lineage.get("campaign_relocation_audit", {})
    if (water_lineage.get("status") != "Passed" or water_lineage.get("same_session_startup_playing_water_parity") != "Match" or
            legacy_lineage.get("validation_status") != "Passed" or
            caveat.get("unresolved_historical_binary_count") != 7 or
            caveat.get("unresolved_non_historical_pin_count") != 0):
        raise ValueError("Qualified water lineage and exact seven-binary caveat required")
    if unit_directory != UNIT_DIRECTORY.resolve(strict=True):
        raise ValueError("Only the exact current S073 native unit-catalog capture is admitted")
    if output.exists() or receipt_output.exists():
        raise ValueError("Fresh create-new r27 bundle and rebind receipt required")

    bundle = json.loads(source_bundle.read_text(encoding="utf-8"))
    if (bundle.get("schema_version") != 1 or bundle.get("kind") != "S073LegacyStartupBundle" or
            bundle.get("mission") != "S073" or bundle.get("seed") != 104729 or REPLACED not in bundle):
        raise ValueError("Exact S073 r26 bundle with configured-unit descriptor required")

    old_unit_descriptor = copy.deepcopy(bundle[REPLACED])
    preserved = {}
    for name, value in bundle.items():
        if name == REPLACED or not isinstance(value, dict) or "path" not in value:
            continue
        if set(value) != {"path", "sha256"}:
            raise ValueError("Malformed retained descriptor: " + name)
        path = Path(value["path"]).resolve(strict=True)
        if not path.is_file() or path.is_symlink() or sha(path) != value["sha256"]:
            raise ValueError("Retained independent S073 input changed: " + name)
        preserved[name] = value["sha256"]

    finalizer = load_finalizer()
    qualified_unit = finalizer.qualify(repo, unit_directory, 0)
    unit_receipt_path = unit_directory / "unit-catalog-receipt.json"
    if json.loads(unit_receipt_path.read_text(encoding="utf-8")) != qualified_unit:
        raise ValueError("Unit receipt does not equal strict finalizer output")

    rebound = copy.deepcopy(bundle)
    rebound[REPLACED] = descriptor(unit_receipt_path)
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x", encoding="utf-8") as stream:
        json.dump(rebound, stream, indent=2)
        stream.write("\n")

    receipt = {
        "kind": "S073NativeUnitCatalogBundleRebindReceipt",
        "schema_version": 1,
        "status": "Passed",
        "source_bundle_path": str(source_bundle),
        "source_bundle_sha256": SOURCE_BUNDLE_SHA256,
        "source_water_rebind_receipt_path": str(SOURCE_WATER_RECEIPT.resolve()),
        "source_water_rebind_receipt_sha256": SOURCE_WATER_RECEIPT_SHA256,
        "output_bundle_path": str(output.resolve()),
        "output_bundle_sha256": sha(output),
        "preserved_descriptor_count": len(preserved),
        "preserved_descriptors_sha256": preserved,
        "replaced_descriptor": {
            "name": REPLACED,
            "prior": old_unit_descriptor,
            "current": descriptor(unit_receipt_path),
            "catalog_path": str((unit_directory / "unit-catalog.json").resolve()),
            "catalog_sha256": sha(unit_directory / "unit-catalog.json"),
            "editor_log_path": str((unit_directory / "editor.log").resolve()),
            "editor_log_sha256": sha(unit_directory / "editor.log"),
        },
        "unit_catalog_validation": "Passed",
        "historical_binary_limitation": {
            "unresolved_count": 7,
            "qualification_effect": "Source-execution qualification remains unclaimed until historical binary content is verified.",
            "audit_receipt_path": str(LEGACY_ADAPTER_RECEIPT.resolve()),
            "audit_receipt_sha256": LEGACY_ADAPTER_RECEIPT_SHA256,
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
            "bundle_sha256": sha(output), "preserved_descriptor_count": len(preserved),
            "unit_catalog_source_pin_count": qualified_unit["source_pin_count"],
            "historical_binary_limitation_count": 7}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--source-bundle", required=True, type=Path)
    parser.add_argument("--unit-directory", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--receipt-output", required=True, type=Path)
    args = parser.parse_args()
    print(json.dumps(rebind(args.repo, args.source_bundle, args.unit_directory,
                            args.output, args.receipt_output)))


if __name__ == "__main__":
    main()

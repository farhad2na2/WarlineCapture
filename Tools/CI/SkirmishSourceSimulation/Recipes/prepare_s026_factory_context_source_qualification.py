#!/usr/bin/env python3
"""Emit a source-only qualification descriptor for the fresh S026 factory export."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
SOURCE_DIR = ROOT / "Logs/SkirmishProduction/s026-factory-context-export-20261116-r7"
CONTEXT = SOURCE_DIR / "s026/source-factory-context.json"
EXPORT_RECEIPT = SOURCE_DIR / "receipt.json"
BUILD_RECEIPT = SOURCE_DIR / "build-receipt.json"
TARGET = {"catalog_id": "S026", "definition_id": "skirmish.s026", "content_version": 2,
          "seed": 104755, "size": "Standard", "difficulty": "Regular", "setup_hash": 3582249640}
FALSE_GATES = ("world_allocated", "native_S050_capture", "source_fit_certified",
               "active_mission_water_qualified", "runtime_loader_admitted", "mission_ready")
TRUE_CHECKS = ("original_all_role_overlay_fields_raw_equal", "original_provider_selection_equal",
               "original_native_origin68_and_assets_exact", "current_factory67_package_exact",
               "factory_sections_unchanged")


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    out = args.output.resolve()
    logs = (ROOT / "Logs/SkirmishProduction").resolve()
    if not out.is_relative_to(logs) or out.exists():
        raise ValueError("Fresh repository Logs/SkirmishProduction output required")

    context = json.loads(CONTEXT.read_bytes())
    exported = json.loads(EXPORT_RECEIPT.read_bytes())
    build = json.loads(BUILD_RECEIPT.read_bytes())
    target = context.get("target_context")
    if context.get("kind") != "OriginalCompilerDerivedNativeFactoryContext" or any(target.get(k) != v for k, v in TARGET.items()):
        raise ValueError("Exact S026 seed 104755 setup 3582249640 factory context required")
    if exported.get("status") != "PassedOriginalOwnerExport" or exported.get("context_sha256") != sha(CONTEXT):
        raise ValueError("Export receipt does not hash the actual factory context")
    if exported.get("target_setup_hash") != TARGET["setup_hash"] or exported.get("source_only") is not True:
        raise ValueError("Exact source-only export receipt required")
    if build.get("kind") != "S026BoundAuthorFactoryContextExporterBuild" or build.get("status") != "Passed":
        raise ValueError("Successful matching factory exporter build receipt required")
    if any(context.get(k) is not False for k in FALSE_GATES):
        raise ValueError("Readiness and allocation gates must all remain false")
    checks = context.get("checks", {})
    if any(checks.get(k) is not True for k in TRUE_CHECKS):
        raise ValueError("Exact original factory checks are required")
    pins = context.get("consumed_input_pins", {})
    if not pins:
        raise ValueError("Factory context input pins are missing")
    stale = [p for p, digest in pins.items() if not Path(p).is_file() or sha(p) != digest]
    if stale:
        raise ValueError("Factory context input pins are stale or missing: " + ", ".join(stale[:5]))

    out.mkdir(parents=True)
    qualification = {
        "schema_version": 1,
        "kind": "ExperimentalGroundConfigurationQualification",
        "runtime_backend": "TypedGroundS026V1",
        "compiler_context": {"path": str(CONTEXT.resolve()), "sha256": sha(CONTEXT)},
        "scope": "OriginalCompilerAndQualifiedFactoryInputs_SourceOnly",
        "native_context_qualified": False,
        "mission_ready": False,
    }
    qpath = out / "qualification.json"
    qpath.write_text(json.dumps(qualification, indent=2, sort_keys=True) + "\n")
    receipt = {
        "kind": "S026FactoryContextSourceOnlyQualificationReceipt",
        "status": "PreparedSourceOnly",
        "qualification": {"path": str(qpath), "sha256": sha(qpath)},
        "source_factory_context": {"path": str(CONTEXT.resolve()), "sha256": sha(CONTEXT)},
        "factory_export_receipt": {"path": str(EXPORT_RECEIPT.resolve()), "sha256": sha(EXPORT_RECEIPT)},
        "factory_build_receipt": {"path": str(BUILD_RECEIPT.resolve()), "sha256": sha(BUILD_RECEIPT)},
        "target_context": target,
        "consumed_input_pin_count": len(pins),
        "all_consumed_input_pins_current": True,
        "world_allocated": False,
        "native_context_qualified": False,
        "source_fit_certified": False,
        "mission_ready": False,
    }
    (out / "receipt.json").write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()

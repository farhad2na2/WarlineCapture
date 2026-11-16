#!/usr/bin/env python3
"""Derive an S003 diagnostic bundle from checked refreshed evidence."""
import argparse
import hashlib
import json
import sys
import tempfile
from pathlib import Path

from build_s003_refreshed_qualification import construct

ROOT = Path(__file__).resolve().parents[4]


def pin(path):
    return {"path": str(path.resolve(strict=True)), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def write(path, value):
    raw = (json.dumps(value, indent=2) + "\n").encode()
    with path.open("xb") as stream:
        stream.write(raw)
    return {"path": str(path.resolve()), "sha256": hashlib.sha256(raw).hexdigest()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    parser.add_argument("--evidence")
    parser.add_argument("--audit")
    parser.add_argument("--movement", help="Checked current source-only rifle input")
    args = parser.parse_args()
    old_bundle = ROOT / "T Logs/SkirmishProduction/s003-current-data-bundle-rebind-20261103-T24/data-bundle.json"
    old_init = ROOT / "T Logs/SkirmishProduction/s003-current-data-bundle-rebind-20261103-T24/initialization.json"
    if bool(args.evidence) != bool(args.audit):
        parser.error("--evidence and --audit must be supplied together")
    evidence = Path(args.evidence) if args.evidence else ROOT / "T Logs/SkirmishProduction/s003-refreshed-qualification-evidence-20261104-T5/source-only-qualification-evidence.json"
    audit = Path(args.audit) if args.audit else ROOT / "T Logs/SkirmishProduction/s003-refreshed-source-evidence-audit-20261104-T4.json"
    if not evidence.is_absolute() or evidence.is_symlink() or not evidence.resolve(strict=True).is_relative_to(ROOT):
        raise ValueError("Canonical repository-owned qualification evidence required")
    if pin(old_bundle)["sha256"] != "46d0a5620dab0a9701aa6a085a9c0b4fb0d512e0dff373f72f9e94f9b0306d8c" or pin(old_init)["sha256"] != "04c1fe1c18b81e5d8922b4176cf293d5847c29d75948b31d204d311908523a35":
        raise ValueError("Exact retained source bundle and initialization required")
    bundle = json.loads(old_bundle.read_bytes())
    initial = json.loads(old_init.read_bytes())
    if bundle["initialization"] != pin(old_init):
        raise ValueError("Retained initialization binding changed")
    current = json.loads(evidence.read_bytes())
    if current != construct(ROOT, json.loads(audit.read_bytes()), audit):
        raise ValueError("Fresh evidence reconstruction required")
    template_path = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-projection-20261104-T1/reference-template-descriptor.json"
    template_receipt_path = template_path.parent / "receipt.json"
    template_receipt = json.loads(template_receipt_path.read_bytes())
    if template_receipt.get("status") != "PassedSourceOnlyProjection" or template_receipt.get("descriptor") != pin(template_path) or template_receipt.get("import_admitted") is not False or template_receipt.get("mission_ready") is not False:
        raise ValueError("Exact source-only template projection receipt required")
    template = json.loads(template_path.read_bytes())
    movement = Path(args.movement) if args.movement else ROOT / "T Logs/SkirmishProduction/s003-refreshed-rifle-input-20261104-T1/unit-movement-source-input.json"
    if not movement.is_absolute() or movement.is_symlink() or not movement.resolve(strict=True).is_relative_to(ROOT):
        raise ValueError("Canonical repository-owned rifle input required")
    movement_receipt = json.loads((movement.parent / "receipt.json").read_bytes())
    if movement_receipt.get("status") != "PassedSourceOnlyProjectionInput" or movement_receipt.get("input") != pin(movement) or movement_receipt.get("mission_ready") is not False:
        raise ValueError("Exact source-only rifle input receipt required")
    movement_data = json.loads(movement.read_bytes())
    source_pins = movement_data.get("source_pins")
    if not isinstance(source_pins, dict) or not source_pins or movement_receipt.get("source_pin_count") != len(source_pins):
        raise ValueError("Complete rifle source pins required")
    for relative, digest in source_pins.items():
        source = ROOT / relative
        if Path(relative).is_absolute() or not source.resolve(strict=True).is_relative_to(ROOT) or pin(source)["sha256"] != digest:
            raise ValueError("Rifle source pin changed: " + relative)
    if args.movement:
        sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
        sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/unit_move_projection"))
        from finalize_s003_refreshed_prefab_capture import qualify
        from run_unit_move_authoring_source_host import create_source_input
        capture = ROOT / "Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-r1/data/prefab-reference-data.json"
        log = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/editor.log"
        capture_receipt = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/receipt.json"
        if qualify(ROOT, capture, log, 0) != json.loads(capture_receipt.read_bytes()) or movement_receipt.get("capture_sha256") != pin(capture)["sha256"]:
            raise ValueError("Checked S003 prefab capture required for rifle input")
        with tempfile.TemporaryDirectory(prefix="s003-rifle-reconstruction-") as temporary:
            regenerated, _ = create_source_input(ROOT, Path(temporary), json.loads(capture.read_bytes()))
            if regenerated.read_bytes() != movement.read_bytes():
                raise ValueError("Rifle input differs from current source reconstruction")
    initial["configuration_qualification"] = pin(evidence)
    initial["metadata_compiler_context"] = pin(evidence)
    initial["source_config_graph"] = current["runtime_graph"]
    initial["metadata_compatibility"] = current["native_capture"]
    initial["reference_templates"] = template
    initial["unit_move_authoring_source"] = pin(movement)
    for field, artifact in (("building_definitions", "building-definitions.json"),
                            ("unit_production_metadata", "unit-production-metadata.json"),
                            ("building_request_metadata", "configured-metadata.json")):
        initial[field] = current["factory_artifacts"][artifact]
    output = Path(args.output).absolute()
    output.mkdir(parents=True, exist_ok=False)
    bundle["initialization"] = write(output / "initialization.json", initial)
    bundle["unit_metadata"] = current["factory_artifacts"]["unit-production-metadata.json"]
    result = write(output / "data-bundle.json", bundle)
    receipt = {"status": "PassedSourceOnlyBundleDerivation", "source_bundle": pin(old_bundle),
               "source_initialization": pin(old_init), "qualification_evidence": pin(evidence),
               "template_descriptor": pin(template_path), "rifle_input": pin(movement),
               "derived_bundle": result, "runtime_admission": False, "source_fit_certified": False,
               "native_context_qualified": False, "mission_ready": False}
    write(output / "receipt.json", receipt)
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()

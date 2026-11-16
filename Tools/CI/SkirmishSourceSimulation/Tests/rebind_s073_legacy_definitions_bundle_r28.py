#!/usr/bin/env python3
"""Rebind only the source-derived S073 legacy building definitions on r27."""
import argparse
import hashlib
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
EXPECTED_R27_SHA = "68432d505a91c433ee1f26231e90cc134a77d62bc7436d3c9c22a5a5795b1850"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def read(path):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("duplicate JSON key: " + key)
            result[key] = value
        return result
    return json.loads(Path(path).read_text(encoding="utf-8"), object_pairs_hook=unique)


def need(value, message):
    if not value:
        raise ValueError(message)


def pinned_descriptor(bundle, name):
    descriptor = bundle.get(name)
    need(isinstance(descriptor, dict) and set(descriptor) == {"path", "sha256"}, "invalid descriptor: " + name)
    path = Path(descriptor["path"]).resolve(strict=True)
    need(path.is_file() and sha(path) == descriptor["sha256"], "descriptor drift: " + name)
    return path


def rebind(args):
    repo = args.repo.resolve(strict=True)
    need(repo == ROOT.resolve(strict=True), "unexpected repository root")
    r27_path = args.bundle.resolve(strict=True)
    r27_sha = sha(r27_path)
    need(r27_sha == EXPECTED_R27_SHA, "exact pinned r27 bundle required")
    r27_receipt = read(args.bundle_receipt.resolve(strict=True))
    need(r27_receipt.get("kind") == "S073NativeUnitCatalogBundleRebindReceipt" and
         r27_receipt.get("status") == "Passed" and
         r27_receipt.get("output_bundle_path") == str(r27_path) and
         r27_receipt.get("output_bundle_sha256") == r27_sha and
         r27_receipt.get("historical_binary_limitation", {}).get("unresolved_count") == 7,
         "r27 receipt or seven-binary limitation mismatch")
    bundle = read(r27_path)
    for name, expected in r27_receipt["preserved_descriptors_sha256"].items():
        path = pinned_descriptor(bundle, name)
        need(sha(path) == expected, "r27 preserved descriptor changed: " + name)

    adapter_receipt_path = args.adapter_receipt.resolve(strict=True)
    adapter_receipt = read(adapter_receipt_path)
    doc = args.document.resolve(strict=True)
    need(adapter_receipt.get("kind") == "S073NativeLegacyDefinitionAdapterRebindReceipt" and
         adapter_receipt.get("validation_status") == "Passed" and
         adapter_receipt.get("adapted_document", {}).get("path") == str(doc) and
         adapter_receipt.get("adapted_document", {}).get("sha256") == sha(doc) and
         adapter_receipt.get("native_capture_rewritten") is False and
         adapter_receipt.get("campaign_relocation_audit", {}).get("unresolved_historical_binary_count") == 7,
         "strict native adapter receipt/document mismatch")
    adapted = read(doc)
    need(adapted.get("mission_world_allocated") is False and
         adapted.get("full_mission_initialized") is False and
         adapted.get("native_visual_readiness") is False and
         adapted.get("unsupported_values") == [], "consumer definition scope must remain non-ready")

    previous = pinned_descriptor(bundle, "building_definitions")
    need(sha(previous) == r27_receipt["preserved_descriptors_sha256"].get("building_definitions"),
         "unexpected previous building definition path")
    rebound = dict(bundle)
    rebound["building_definitions"] = {"path": str(doc), "sha256": sha(doc)}
    output = args.output.resolve()
    receipt_out = args.output_receipt.resolve()
    need(not output.exists() and not receipt_out.exists(), "r28 outputs must be new")
    output.parent.mkdir(parents=True, exist_ok=True)
    receipt_out.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x", encoding="utf-8") as stream:
        json.dump(rebound, stream, indent=2)
        stream.write("\n")
    out_sha = sha(output)
    preserved = {}
    for name, descriptor in bundle.items():
        if name == "building_definitions" or not isinstance(descriptor, dict) or "path" not in descriptor:
            continue
        path = pinned_descriptor(bundle, name)
        preserved[name] = {"path": str(path), "sha256": sha(path)}
    need(len(preserved) == 29, "r28 must preserve all 29 unaffected source descriptors")
    need(all(sha(row["path"]) == row["sha256"] for row in preserved.values()), "preserved descriptor drift")
    receipt = {
        "kind": "S073LegacyDefinitionBundleRebindReceipt", "schema_version": 1, "status": "Passed",
        "source_bundle": {"path": str(r27_path), "sha256": r27_sha},
        "source_bundle_receipt": {"path": str(args.bundle_receipt.resolve()), "sha256": sha(args.bundle_receipt)},
        "adapter_receipt": {"path": str(adapter_receipt_path), "sha256": sha(adapter_receipt_path)},
        "native_capture": adapter_receipt["native_capture"],
        "adapted_document": {"path": str(doc), "sha256": sha(doc)},
        "output_bundle": {"path": str(output), "sha256": out_sha},
        "replaced_descriptor": {"name": "building_definitions", "previous": {"path": str(previous), "sha256": sha(previous)},
                                "current": {"path": str(doc), "sha256": sha(doc)}},
        "preserved_descriptor_count": len(preserved), "preserved_descriptors": preserved,
        "unresolved_historical_binary_count": 7,
        "source_execution_qualification": False, "mission_ready": False, "source_fit_certified": False,
        "normal_input_playthrough": False, "player_readiness": False,
    }
    with receipt_out.open("x", encoding="utf-8") as stream:
        json.dump(receipt, stream, indent=2)
        stream.write("\n")
    return {"status": "Passed", "bundle": str(output), "bundle_sha256": out_sha,
            "receipt": str(receipt_out), "preserved_descriptors": len(preserved),
            "unresolved_historical_binaries": 7}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--bundle", type=Path, required=True)
    parser.add_argument("--bundle-receipt", type=Path, required=True)
    parser.add_argument("--adapter-receipt", type=Path, required=True)
    parser.add_argument("--document", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--output-receipt", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(rebind(args), indent=2))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as error:
        print("[S073LegacyDefinitionBundleRebind] result=Failed " + str(error), file=sys.stderr)
        sys.exit(1)

#!/usr/bin/env python3
"""Derive the exact ordered S003 unit-prefab keys from the finalized current capture."""
import argparse
import hashlib
import importlib.util
import json
import os
import re
from pathlib import Path


CAPTURE_KIND = "NativeS003OriginalCompilerFactoryMetadataCapture"
CAPTURE_RECEIPT_KIND = "S003OriginalCompilerFactoryMetadataCaptureReceipt"
DESCRIPTOR_KIND = "S003FocusedPrefabKeyDescriptorReceipt"
EXPECTED_SEED = 104732
EXPECTED_SETUP_HASH = 2394754492
EXPECTED_UNIT_COUNT = 19
HEX_32 = re.compile(r"[0-9a-f]{32}\Z")
HEX_64 = re.compile(r"[0-9a-f]{64}\Z")


def sha_bytes(data):
    return hashlib.sha256(data).hexdigest()


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property: " + key)
        result[key] = value
    return result


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique_object,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Invalid JSON constant: " + value)))


def require(condition, message):
    if not condition:
        raise ValueError(message)


def _check_no_symlink(path, root):
    path = path.absolute()
    root = root.absolute()
    relative = path.relative_to(root)
    current = root
    for part in relative.parts:
        current = current / part
        require(not current.is_symlink(), "Symlink prefab source path forbidden: " + str(current))


def derive_keys(repo, capture):
    context = capture.get("context")
    require(isinstance(context, dict) and context.get("catalog_id") == "S003" and
            context.get("size") == "Standard" and context.get("difficulty") == "Regular" and
            context.get("seed") == EXPECTED_SEED and context.get("setup_hash") == EXPECTED_SETUP_HASH,
            "Exact current S003 Standard/Regular compiler context required")
    require(context.get("wrong_context_hash_rejected") is True and
            context.get("wrong_target_seed_rejected") is True,
            "Current S003 compiler rejection controls required")
    metadata = context.get("metadata")
    require(isinstance(metadata, dict) and metadata.get("catalog_inputs", {}).get("catalog_id") == "S003",
            "Current packaged S003 factory metadata required")
    units = metadata.get("units")
    require(isinstance(units, list) and len(units) == EXPECTED_UNIT_COUNT,
            "Exact current S003 19-unit metadata domain required")
    source_pins = capture.get("source_pins")
    require(isinstance(source_pins, dict) and source_pins,
            "Current capture source-pin map required")

    keys, identities, folded = [], [], set()
    for index, row in enumerate(units):
        require(isinstance(row, dict) and row.get("index") == index and
                row.get("has_definition_metadata") is True and row.get("has_production_metadata") is True,
                "Ordered complete unit metadata row required at index " + str(index))
        prefab = row.get("prefab")
        require(isinstance(prefab, dict) and prefab.get("codec") == "source-asset-reference-v1" and
                prefab.get("type") == "UnityEngine.GameObject",
                "Exact source prefab identity record required at unit " + str(index))
        key, guid, local_id, asset_path = (prefab.get("key"), prefab.get("guid"), prefab.get("local_id"), prefab.get("path"))
        require(isinstance(key, str) and key and key == key.strip() and
                not any(ord(char) < 32 for char in key) and "/" not in key and "\\" not in key,
                "Canonical nonempty source prefab key required at unit " + str(index))
        require(key.casefold() not in folded, "Duplicate S003 unit prefab key")
        folded.add(key.casefold())
        require(isinstance(guid, str) and HEX_32.fullmatch(guid), "Canonical source prefab GUID required: " + key)
        require(type(local_id) is int and local_id != 0, "Nonzero source prefab local ID required: " + key)
        require(isinstance(asset_path, str) and asset_path.startswith("Assets/Game/Prefabs/") and
                asset_path.endswith(".prefab") and ".." not in Path(asset_path).parts,
                "Canonical project prefab path required: " + key)
        meta_path = asset_path + ".meta"
        prefab_sha, meta_sha = source_pins.get(asset_path), source_pins.get(meta_path)
        require(isinstance(prefab_sha, str) and HEX_64.fullmatch(prefab_sha) and
                isinstance(meta_sha, str) and HEX_64.fullmatch(meta_sha),
                "Prefab and .meta pins required: " + key)
        prefab_file, meta_file = repo / asset_path, repo / meta_path
        _check_no_symlink(prefab_file, repo)
        _check_no_symlink(meta_file, repo)
        require(sha_bytes(prefab_file.read_bytes()) == prefab_sha, "Current prefab pin changed: " + asset_path)
        meta_raw = meta_file.read_bytes()
        require(sha_bytes(meta_raw) == meta_sha, "Current prefab .meta pin changed: " + meta_path)
        guid_lines = re.findall(rb"(?m)^guid:\s*([0-9a-f]{32})\s*$", meta_raw)
        require(guid_lines == [guid.encode("ascii")], "Prefab .meta GUID differs from captured identity: " + key)
        keys.append(key)
        identities.append({"index": index, "key": key, "guid": guid, "local_id": local_id,
                           "path": asset_path, "prefab_sha256": prefab_sha, "meta_sha256": meta_sha})

    require(len(keys) == len(set(keys)) == EXPECTED_UNIT_COUNT,
            "Exact unique ordered S003 19-key set required")
    return keys, identities


def _qualified_receipt(repo, capture_path, capture_receipt_path, capture_raw, capture_receipt):
    require(capture_receipt.get("kind") == CAPTURE_RECEIPT_KIND and
            capture_receipt.get("validation_status") == "Passed" and
            capture_receipt.get("wrapper_exit_code") == 0 and
            capture_receipt.get("required_marker_count") == 1 and
            capture_receipt.get("capture_path") == str(capture_path.resolve()) and
            capture_receipt.get("capture_sha256") == sha_bytes(capture_raw),
            "Exact finalized S003 native metadata capture receipt required")
    require(capture_receipt.get("seed") == EXPECTED_SEED and
            capture_receipt.get("setup_hash") == EXPECTED_SETUP_HASH and
            capture_receipt.get("graph_sha256") == "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb" and
            capture_receipt.get("run6_receipt_sha256") == "3d518a67a576bf223d66fb88adf181fc893bfb890200d902948d4a1a36b13609" and
            capture_receipt.get("run6_post_run_audit_sha256") == "52b5e97a494dc4adf5e5159e65fbe5921f57ad90b730012c5d0698f820abeda9" and
            capture_receipt.get("run6_graph_sha256") == "150cb503bb95dcdb1371e2c52c54764308b9bf1ad5e6305ae7ba3a68bf39cc5b",
            "Exact current graph-r5 and run-6 compiler provenance required")
    require(capture_receipt.get("utility_world_created") is True and
            capture_receipt.get("utility_world_disposed") is True and
            capture_receipt.get("mission_world_created") is False and
            capture_receipt.get("native_context_qualified") is False and
            capture_receipt.get("source_fit_certified") is False and
            capture_receipt.get("mission_ready") is False,
            "Honest data-only capture and false readiness flags required")

    finalizer_path = (Path(__file__).resolve().parents[1] / "Tests" /
                      "finalize_s003_native_metadata_capture.py").resolve(strict=True)
    spec = importlib.util.spec_from_file_location("s003_capture_finalizer", finalizer_path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    checked = module.qualify(repo, capture_path.parent, capture_receipt["wrapper_exit_code"])
    require(checked == capture_receipt,
            "Stored finalizer receipt differs from independently revalidated current capture")
    return checked


def generate(repo, capture_path, capture_receipt_path, output):
    repo = Path(repo).resolve(strict=True)
    capture_path = Path(capture_path).resolve(strict=True)
    capture_receipt_path = Path(capture_receipt_path).resolve(strict=True)
    output = Path(os.path.abspath(output))
    require(not output.exists(), "Fresh descriptor output directory required")
    require(output.resolve().is_relative_to(repo), "Descriptor output must remain inside repository")
    capture_raw = capture_path.read_bytes()
    capture_receipt_raw = capture_receipt_path.read_bytes()
    capture = json.loads(capture_raw.decode("utf-8", errors="strict"), object_pairs_hook=unique_object)
    capture_receipt = json.loads(capture_receipt_raw.decode("utf-8", errors="strict"), object_pairs_hook=unique_object)
    qualified = _qualified_receipt(repo, capture_path, capture_receipt_path, capture_raw, capture_receipt)
    require(capture.get("schema_version") == 1 and capture.get("kind") == CAPTURE_KIND and
            capture.get("scope") == "CurrentPackagedS003CompilerAndOriginalMetadataFactories_DataOnly_NoMissionWorld" and
            capture.get("full_mission_initialized") is False and capture.get("utility_world_created") is True and
            capture.get("utility_world_disposed") is True and capture.get("mission_world_created") is False and
            capture.get("runtime_state_transferred") is False and capture.get("source_fit_certified") is False and
            capture.get("native_context_qualified") is False and capture.get("mission_ready") is False,
            "Exact S003 original factory capture with false readiness required")
    context = capture["context"]
    metadata = context["metadata"]
    metadata_digest = sha_bytes(json.dumps(metadata, separators=(",", ":"), ensure_ascii=False).encode("utf-8"))
    require(context.get("metadata_key_sha256") == metadata_digest and
            qualified.get("metadata_key_sha256") == metadata_digest and
            qualified.get("unit_count") == EXPECTED_UNIT_COUNT,
            "Current S003 metadata digest and unit count must match finalized receipt")
    keys, identities = derive_keys(repo, capture)
    key_bytes = (json.dumps(keys, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    key_sha = sha_bytes(key_bytes)
    descriptor = {
        "schema_version": 1,
        "kind": DESCRIPTOR_KIND,
        "status": "PassedSourceOnly",
        "authority": "CurrentFinalizedS003NativeMetadataCapture",
        "capture": {"path": str(capture_path), "sha256": sha_bytes(capture_raw),
                    "receipt_path": str(capture_receipt_path), "receipt_sha256": sha_bytes(capture_receipt_raw)},
        "compiler_target": {"catalog_id": "S003", "definition_id": "skirmish.s003", "size": "Standard",
                            "difficulty": "Regular", "seed": EXPECTED_SEED, "setup_hash": EXPECTED_SETUP_HASH,
                            "metadata_key_sha256": metadata_digest},
        "source_graph": {"path": qualified["graph_path"], "sha256": qualified["graph_sha256"]},
        "run6_compiler_provenance": {"receipt_sha256": qualified["run6_receipt_sha256"],
                                     "post_run_audit_sha256": qualified["run6_post_run_audit_sha256"],
                                     "source_graph_sha256": qualified["run6_graph_sha256"]},
        "descriptor": {"path": "prefab-keys.json", "sha256": key_sha, "key_count": len(keys), "keys_in_metadata_order": keys},
        "prefab_identities": identities,
        "source_pin_count": qualified["source_pin_count"],
        "utility_world_created": True,
        "utility_world_disposed": True,
        "mission_world_created": False,
        "native_context_qualified": False,
        "source_fit_certified": False,
        "mission_ready": False,
        "negative_controls": {"suite": "test_generate_s003_focused_prefab_key_descriptor.py",
                              "required_cases": ["duplicate-case-insensitive-key-rejected",
                                                 "wrong-target-setup-rejected",
                                                 "unmatched-prefab-pin-rejected",
                                                 "meta-guid-drift-rejected"],
                              "results_recorded_by": "separate focused test run; not inferred by generator"},
        "scope": "Ordered source prefab key names and identities only, derived from finalized S003 native metadata; no template bake, mission world, source-fit or readiness claim.",
    }
    output.mkdir(parents=True)
    descriptor_path = output / "prefab-keys.json"
    receipt_path = output / "receipt.json"
    descriptor_path.write_bytes(key_bytes)
    receipt_path.write_text(json.dumps(descriptor, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    require(descriptor_path.read_bytes() == key_bytes and sha_bytes(descriptor_path.read_bytes()) == key_sha,
            "Written key descriptor changed")
    require(capture_path.read_bytes() == capture_raw and capture_receipt_path.read_bytes() == capture_receipt_raw,
            "Capture evidence changed during key descriptor generation")
    for identity in identities:
        for path, digest in ((identity["path"], identity["prefab_sha256"]),
                             (identity["path"] + ".meta", identity["meta_sha256"])):
            require(sha_bytes((repo / path).read_bytes()) == digest, "Prefab source changed during key generation: " + path)
    return {"status": descriptor["status"], "descriptor_path": str(descriptor_path),
            "descriptor_sha256": key_sha, "receipt_path": str(receipt_path),
            "receipt_sha256": sha_bytes(receipt_path.read_bytes()), "key_count": len(keys)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--capture", required=True, type=Path)
    parser.add_argument("--capture-receipt", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    try:
        print(json.dumps(generate(args.repo, args.capture, args.capture_receipt, args.output), indent=2))
    except Exception as error:
        raise SystemExit("S003 focused prefab key descriptor generation failed closed: " + str(error))


if __name__ == "__main__":
    main()

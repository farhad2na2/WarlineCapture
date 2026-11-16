#!/usr/bin/env python3
"""Strictly finalize one current S002 focused capture without an epoch hash allowlist.

The only variable external source is the exact key descriptor path and SHA passed
by the caller. Capture contents, pins, method, log marker, and readiness limits are
validated by the existing strict S002 finalizer.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

FINALIZER = (Path(__file__).resolve().parents[1] / "Tests" /
             "finalize_s002_focused_prefab_capture.py")
SPEC = importlib.util.spec_from_file_location("strict_s002_finalizer", FINALIZER)
STRICT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(STRICT)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def finalize(capture_path, export_capture_path, log_path, wrapper_exit, repo_path, external_roots,
             key_path, key_sha):
    capture_path = Path(capture_path).resolve(strict=True)
    export_capture_path = Path(export_capture_path).resolve(strict=True)
    log_path = Path(log_path).resolve(strict=True)
    repo_path = Path(repo_path).resolve(strict=True)
    if not capture_path.is_relative_to(repo_path):
        raise ValueError("Durable capture artifact must be inside the current repository")
    # Captures may be written to the project's checked Logs directory when it
    # is on the requested output drive. The original output and durable copy
    # must still be distinct files; STRICT.validate binds the original output
    # directory to the checked Unity log and verifies its contents and pins.
    if (export_capture_path.samefile(capture_path) or
            (export_capture_path.is_relative_to(repo_path) and
             not export_capture_path.is_relative_to(repo_path / "Logs" / "SkirmishProduction"))):
        raise ValueError("Exporter output path must remain distinct from the durable repository copy")
    key_path = Path(key_path).resolve(strict=True)
    key_raw = key_path.read_bytes()
    if sha(key_raw) != key_sha:
        raise ValueError("Exact external key descriptor path/SHA mismatch")
    keys = json.loads(key_raw.decode("utf-8"), object_pairs_hook=STRICT.reject_duplicate_properties)
    if keys != STRICT.EXPECTED_KEYS:
        raise ValueError("External key descriptor must contain the exact ordered S002 key list")

    capture_raw = capture_path.read_bytes()
    export_raw = export_capture_path.read_bytes()
    if capture_raw != export_raw:
        raise ValueError("Durable capture copy differs from the exact exporter output bytes")
    capture = json.loads(capture_raw.decode("utf-8"), object_pairs_hook=STRICT.reject_duplicate_properties)
    pins = capture.get("source_pins")
    if not isinstance(pins, dict) or pins.get(str(key_path)) != key_sha:
        raise ValueError("Capture must pin the exact external key descriptor path and SHA")
    if capture.get("requested_prefab_keys_sha256") != key_sha:
        raise ValueError("Capture key descriptor SHA differs from the external key source")

    strict_receipt = STRICT.validate(export_capture_path, log_path, wrapper_exit,
                                     repo_path, external_roots)
    if strict_receipt["capture_sha256"] != sha(capture_raw):
        raise ValueError("Strict finalizer capture hash changed")
    log_raw = log_path.read_bytes()
    # Recheck all externally pinned files after finalization; the existing strict
    # finalizer performs this once, and this second pass binds the new proof to the
    # same exact source closure after the receipt was computed.
    pin_count = STRICT.verify_pins(capture, Path(repo_path).resolve(strict=True),
                                   [Path(p).resolve(strict=True) for p in external_roots])
    if capture_path.read_bytes() != capture_raw or export_capture_path.read_bytes() != export_raw or key_path.read_bytes() != key_raw or log_path.read_bytes() != log_raw:
        raise ValueError("Capture, key descriptor, or Unity log changed during finalization")
    return {
        "schema_version": 1,
        "kind": "S002CurrentFocusedTemplateCaptureFinalization",
        "validation_status": "PassedCurrentPinnedSourceOnly",
        "capture_path": str(capture_path),
        "capture_sha256": sha(capture_raw),
        "export_capture_path": str(export_capture_path),
        "export_capture_sha256": sha(export_raw),
        "capture_log_path": str(log_path),
        "capture_log_sha256": sha(log_raw),
        "key_descriptor_path": str(key_path),
        "key_descriptor_sha256": key_sha,
        "strict_finalizer": strict_receipt,
        "source_pin_count": pin_count,
        "required_marker": STRICT.MARKER,
        "required_method": STRICT.METHOD,
        "wrapper_exit_code": 0,
        "mission_ready": False,
        "replay_ready": False,
        "full_mission": False,
        "native_visual_readiness": False,
        "source_fit_ready": False,
    }


def reject_duplicate_properties(rows):
    result = {}
    for key, value in rows:
        if key in result:
            raise ValueError("Duplicate JSON property: " + key)
        result[key] = value
    return result


STRICT.reject_duplicate_properties = reject_duplicate_properties


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--capture", required=True, type=Path)
    parser.add_argument("--export-capture", required=True, type=Path)
    parser.add_argument("--capture-log", required=True, type=Path)
    parser.add_argument("--capture-wrapper-exit", required=True, type=int)
    parser.add_argument("--external-source-root", action="append", type=Path, default=[])
    parser.add_argument("--key-descriptor", required=True, type=Path)
    parser.add_argument("--key-descriptor-sha256", required=True)
    parser.add_argument("--receipt", required=True, type=Path)
    args = parser.parse_args()
    receipt_path = args.receipt.resolve()
    if receipt_path.exists():
        raise ValueError("Fresh receipt path required")
    receipt = finalize(args.capture, args.export_capture, args.capture_log, args.capture_wrapper_exit,
                      args.repo, args.external_source_root,
                      args.key_descriptor, args.key_descriptor_sha256)
    receipt_path.parent.mkdir(parents=True, exist_ok=True)
    receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(receipt, indent=2, sort_keys=True))


if __name__ == "__main__":
    main()

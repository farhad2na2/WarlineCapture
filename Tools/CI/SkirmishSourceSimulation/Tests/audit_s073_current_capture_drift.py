#!/usr/bin/env python3
"""Audit a preserved Campaign capture against current bytes; admit no runtime state."""
import argparse
import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
RECEIPT = ROOT / "Logs/SkirmishProduction/s073-captured-source-preservation-20261101-r1/receipt.json"


def digest(path):
    h = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def audit(root, receipt_path):
    receipt_raw = receipt_path.read_bytes()
    receipt = json.loads(receipt_raw)
    if receipt.get("status") != "Passed" or receipt.get("project_files") != 6522 or receipt.get("mission_ready") is not False:
        raise ValueError("Exact retained preservation inventory required")
    capture = Path(receipt["capture_path"])
    if digest(capture) != receipt["capture_sha256"]:
        raise ValueError("Retained Campaign capture changed")
    pins = json.loads(capture.read_bytes())["readPins"]
    preserved = Path(receipt["preserved_root"])
    original = receipt["original_root"].rstrip("/") + "/"
    rows = receipt["files"]
    if len(rows) != receipt["project_files"]:
        raise ValueError("Incomplete preserved source inventory")
    seen = set()
    mismatches = []
    missing = []
    for row in rows:
        relative = row["relative_path"]
        rel = Path(relative)
        if rel.is_absolute() or any(part in ("", ".", "..") for part in rel.parts) or relative in seen:
            raise ValueError("Duplicate or escaped preserved source path")
        seen.add(relative)
        expected = row["sha256"]
        if pins.get(relative, pins.get(original + relative)) != expected:
            raise ValueError("Preservation row differs from capture pin: " + relative)
        retained = preserved / rel
        if not retained.is_file() or retained.is_symlink() or digest(retained) != expected:
            raise ValueError("Preserved source changed: " + relative)
        current = root / rel
        if not current.is_file() or current.is_symlink():
            missing.append(relative)
            continue
        actual = digest(current)
        if actual != expected:
            mismatches.append({"path": relative, "captured_sha256": expected, "current_sha256": actual})
    return {"status": "CapturedSourceDriftAuditCompleted", "preservation_receipt_sha256": hashlib.sha256(receipt_raw).hexdigest(),
            "capture_sha256": receipt["capture_sha256"], "captured_files_verified": len(seen),
            "current_mismatches": mismatches, "current_missing_or_symlink": missing,
            "historical_binary_pins_verified": False, "current_source_equivalence": False,
            "all_current_bytes_match_at_read": not mismatches and not missing, "pin_epoch_stability_verified": False,
            "runtime_admitted": False, "source_fit_certified": False, "mission_ready": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    result = audit(ROOT, RECEIPT)
    output = Path(args.output).absolute()
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
    print(json.dumps({"status": result["status"], "captured_files_verified": result["captured_files_verified"],
                      "current_mismatches": len(result["current_mismatches"]),
                      "current_missing_or_symlink": len(result["current_missing_or_symlink"]), "mission_ready": False}))


if __name__ == "__main__":
    main()

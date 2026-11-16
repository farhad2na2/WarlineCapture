"""Persist the wrapper exit already proven by both strict S073 water receipts."""
import argparse
import hashlib
import json
from pathlib import Path


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def record(directory):
    directory = Path(directory).resolve(strict=True)
    water_path = directory / "water-receipt.json"
    joint_path = directory / "joint-receipt.json"
    log_path = directory / "editor.log"
    water = json.loads(water_path.read_text(encoding="utf-8"))
    joint = json.loads(joint_path.read_text(encoding="utf-8"))
    log_sha = digest(log_path)
    if (water.get("kind") != "S073NativeWaterMaskCaptureReceipt" or
            water.get("validation_status") != "Passed" or water.get("wrapper_exit_code") != 0 or
            water.get("required_marker_present") is not True or water.get("editor_log_sha256") != log_sha or
            joint.get("kind") != "S073JointNativeWaterMaskComparison" or
            joint.get("evidence_validation") != "Passed" or joint.get("parity_status") != "Match" or
            joint.get("wrapper_exit_code") != 0 or joint.get("required_markers_present") is not True or
            joint.get("editor_log_path") != str(log_path) or joint.get("editor_log_sha256") != log_sha or
            joint.get("playing_capture", {}).get("receipt_path") != str(water_path) or
            joint.get("playing_capture", {}).get("receipt_sha256") != digest(water_path)):
        raise ValueError("Both strict water receipts must bind wrapper exit 0, required markers, same log, and each other")

    wrapper_path = directory / "wrapper-exit.json"
    provenance_path = directory / "wrapper-exit-provenance.json"
    if wrapper_path.exists() or provenance_path.exists():
        raise ValueError("Create-new wrapper exit evidence required")
    wrapper_bytes = b'{"exit_code": 0}\n'
    provenance = {
        "kind": "S073WrapperExitMaterializationReceipt",
        "schema_version": 1,
        "status": "Passed",
        "source": "The wrapper exit code is materialized from the wrapper_exit_code=0 values already bound by both passing strict finalizer receipts; this file is not an independent raw wrapper log.",
        "wrapper_exit_path": str(wrapper_path),
        "wrapper_exit_sha256": hashlib.sha256(wrapper_bytes).hexdigest(),
        "wrapper_exit_code": 0,
        "water_receipt_path": str(water_path),
        "water_receipt_sha256": digest(water_path),
        "joint_receipt_path": str(joint_path),
        "joint_receipt_sha256": digest(joint_path),
        "editor_log_path": str(log_path),
        "editor_log_sha256": log_sha,
    }
    with wrapper_path.open("xb") as output:
        output.write(wrapper_bytes)
    with provenance_path.open("x", encoding="utf-8") as output:
        json.dump(provenance, output, indent=2)
        output.write("\n")
    return provenance


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", required=True, type=Path)
    args = parser.parse_args()
    print(json.dumps(record(args.directory)))


if __name__ == "__main__":
    main()

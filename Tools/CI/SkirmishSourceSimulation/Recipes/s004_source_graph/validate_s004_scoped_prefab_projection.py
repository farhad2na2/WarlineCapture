#!/usr/bin/env python3
"""Independently validate the passed-capture S004 source-only projection."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[5]
PROJECTOR = Path(__file__).with_name("project_s004_passed_scoped_prefab_capture.py")
SPEC = importlib.util.spec_from_file_location("s004_passed_scoped_prefab_projection", PROJECTOR)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--projection", type=Path, required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    args = parser.parse_args()
    projection_path, receipt_path = args.projection.resolve(), args.receipt.resolve()
    try:
        need(projection_path.is_file() and not receipt_path.exists(), "projection required and fresh receipt path required")
        projection = json.loads(projection_path.read_text(encoding="utf-8"))
        facts = MODULE.validate_projection(projection, ROOT)
        receipt = {"schema_version": 1, "kind": "S004PassedScopedPrefabProjectionProvenanceReceipt",
                   **facts, "projection": str(projection_path), "projection_sha256": sha(projection_path),
                   "capture_finalizer_sha256": sha(ROOT / projection["capture"]["finalizer_receipt"]),
                   "historical_t11_provenance": projection["historical_t11_provenance"],
                   "two_seed_metadata_drift_audit": projection["two_seed_metadata_drift_audit"]}
        receipt_path.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
        print(json.dumps({"status": receipt["status"], "roots": receipt["roots"],
                          "entities": receipt["entities"], "excluded_leaves": receipt["excluded_leaves"],
                          "live_pins": receipt["current_live_source_pins"],
                          "negative_controls": len(receipt["negative_controls"]),
                          "world_allocated": False, "mission_ready": False,
                          "receipt": str(receipt_path)}, indent=2))
        return 0
    except (ValueError, OSError, KeyError, TypeError, json.JSONDecodeError, MODULE.subprocess.SubprocessError) as exc:
        print("S004 scoped prefab projection validation failed closed: " + str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

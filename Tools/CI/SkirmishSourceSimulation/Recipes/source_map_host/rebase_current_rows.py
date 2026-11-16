#!/usr/bin/env python3
"""Append missing exact current owner rows to a derived source manifest."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]


def pin(path):
    path = Path(path).resolve(strict=True)
    if path.is_symlink() or not path.is_file():
        raise ValueError(f"Regular current owner source required: {path}")
    return {"path": str(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def merge_missing_rows(canonical, derived):
    if not isinstance(canonical, dict) or not isinstance(derived, dict):
        raise ValueError("Canonical and derived source manifest objects required")
    source_rows = canonical.get("sources")
    derived_rows = derived.get("sources")
    if not isinstance(source_rows, list) or not isinstance(derived_rows, list):
        raise ValueError("Canonical and derived source row lists required")
    if len(source_rows) > 2048 or len(derived_rows) > 2048:
        raise ValueError("Bounded source row lists required")

    def unique(rows, label):
        ids, paths = set(), set()
        for row in rows:
            if not isinstance(row, dict) or not isinstance(row.get("id"), str) or not row["id"]:
                raise ValueError(f"Exact {label} row identity required")
            path = row.get("output") or row.get("path")
            if not isinstance(path, str) or not path or Path(path).is_absolute() or "\\" in path or ".." in Path(path).parts:
                raise ValueError(f"Canonical relative {label} row path required: {row['id']}")
            if row["id"] in ids:
                raise ValueError(f"Duplicate {label} source row id: {row['id']}")
            if path in paths:
                raise ValueError(f"Conflicting {label} source row path: {path}")
            ids.add(row["id"])
            paths.add(path)
        return ids, paths

    canonical_ids, _ = unique(source_rows, "canonical")
    derived_ids, derived_paths = unique(derived_rows, "derived")
    appended, source_pins = [], {}
    for row in source_rows:
        if row["id"] in derived_ids:
            # Existing same-id rows may be deliberate derived overlays; preserve them byte-for-byte.
            continue
        if row["kind"] != "finite_adapter" or row.get("contains_original_gameplay") is not False:
            raise ValueError(f"Only bounded current finite-adapter rows may be appended: {row['id']}")
        if row.get("path") in derived_paths:
            raise ValueError(f"Canonical owner path conflicts with a derived row: {row['path']}")
        source = (ROOT / row["path"]).resolve(strict=True)
        if not source.is_relative_to(ROOT):
            raise ValueError(f"Canonical owner source escaped repository: {row['path']}")
        descriptor = pin(source)
        derived_rows.append(dict(row))
        appended.append(row["id"])
        source_pins[descriptor["path"]] = descriptor["sha256"]
        derived_ids.add(row["id"])
        derived_paths.add(row["path"])
    if derived.get("runtime_backend") != canonical.get("runtime_backend") or derived.get("profile") != canonical.get("profile"):
        raise ValueError("Derived and canonical source manifest profiles differ")
    return derived, {"appended_row_ids": appended, "source_pins": source_pins}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--canonical", type=Path, required=True)
    parser.add_argument("--derived", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    canonical_path = args.canonical.resolve(strict=True)
    derived_path = args.derived.resolve(strict=True)
    output = args.output.absolute()
    if not canonical_path.is_relative_to(ROOT) or not derived_path.is_relative_to(ROOT) or not output.is_relative_to(ROOT) or output.exists():
        raise ValueError("Repository-owned inputs and fresh repository-owned output required")
    canonical_bytes, derived_bytes = canonical_path.read_bytes(), derived_path.read_bytes()
    merged, evidence = merge_missing_rows(json.loads(canonical_bytes), json.loads(derived_bytes))
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(merged, indent=2) + "\n")
    receipt = {
        "schema_version": 1,
        "kind": "CurrentCanonicalOwnerRowsRebaseReceipt",
        "status": "GeneratedSourceOnly",
        "canonical_manifest": {"path": str(canonical_path), "sha256": hashlib.sha256(canonical_bytes).hexdigest()},
        "derived_manifest": {"path": str(derived_path), "sha256": hashlib.sha256(derived_bytes).hexdigest()},
        **evidence,
        "output_manifest": pin(output),
        "mission_run": False,
        "mission_ready": False,
        "native_acceptance": False,
    }
    receipt_path = output.with_name("rebase-receipt.json")
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt, sort_keys=True))


if __name__ == "__main__":
    main()

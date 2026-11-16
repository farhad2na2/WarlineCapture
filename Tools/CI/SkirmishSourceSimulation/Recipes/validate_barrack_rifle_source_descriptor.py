#!/usr/bin/env python3
"""Read-only Python preflight for the narrow Barrack/rifle authored descriptor.

This validates checked-in source facts only. It never admits mission or native
factory data and never constructs a producer core projection.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import importlib.util
from pathlib import Path
import re
import sys

_GENERATOR_PATH = Path(__file__).with_name("generate_barrack_rifle_source_descriptor.py")
_GENERATOR_SPEC = importlib.util.spec_from_file_location("barrack_rifle_source_descriptor", _GENERATOR_PATH)
source_descriptor = importlib.util.module_from_spec(_GENERATOR_SPEC)
_GENERATOR_SPEC.loader.exec_module(source_descriptor)

ROOT = Path(__file__).resolve().parents[4]
SHA_RE = re.compile(r"^[0-9a-f]{64}$")


def need(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def safe_regular(root: Path, path: Path) -> Path:
    root = root.resolve(strict=True)
    path = path.absolute()
    try:
        rel = path.relative_to(root)
    except ValueError as error:
        raise ValueError("descriptor path must be within repository") from error
    cursor = root
    for part in rel.parts:
        cursor = cursor / part
        need(not cursor.is_symlink(), "symlink/reparse path component rejected: " + str(cursor))
    need(path.is_file(), "regular descriptor file required")
    return path


def _pairs_no_duplicates(pairs):
    result = {}
    for key, value in pairs:
        need(key not in result, "duplicate JSON property rejected: " + key)
        result[key] = value
    return result


def _current_source_facts(root: Path) -> dict:
    facts = source_descriptor.build_descriptor(root)
    for rel, expected in facts["source_pins"].items():
        path = source_descriptor.safe_file(root, Path(rel))
        need(sha(path.read_bytes()) == expected, "current source pin changed during preflight: " + rel)
    for evidence in facts["guid_lookup_facts"]:
        _, current = source_descriptor.resolve_guid(root, evidence["guid"])
        need(current == evidence, "GUID lookup epoch changed during preflight: " + evidence["guid"])
    return facts


def validate_descriptor(root: Path, descriptor_path: Path, expected_sha256: str) -> dict:
    root = root.resolve(strict=True)
    need(SHA_RE.fullmatch(expected_sha256 or "") is not None, "exact lowercase descriptor SHA-256 required")
    path = safe_regular(root, descriptor_path)
    raw = path.read_bytes()
    actual = sha(raw)
    need(actual == expected_sha256, "descriptor SHA-256 mismatch")
    document = json.loads(raw.decode("utf-8", errors="strict"), object_pairs_hook=_pairs_no_duplicates)
    current = _current_source_facts(root)
    need(document == current, "descriptor authored identities, values, source pins, or GUID lookup evidence differ from current source")

    # Authored footprint and prefab GUID alone cannot establish the factory's
    # normalized runtime core identity. Do not substitute authored values.
    missing = []
    producer = document["facts"]["producer"]
    if producer["prefab"].get("local_id") is None:
        missing.append("Barrack prefab local ID from current factory/source identity")
    missing.append("current factory-derived normalized producer footprint authority")
    core_projection = {
        "status": "RejectedMissingAuthority",
        "projected": False,
        "missing_facts": missing,
        "authored_footprint_cells_observed": producer["footprint_cells"],
        "authored_footprint_used_as_factory_authority": False,
    }
    return {
        "schema_version": 1,
        "kind": "BarrackRifleSourceHostPreflightReceipt",
        "status": "PassedAuthoredSourcePreflight_CoreProjectionBlocked",
        "descriptor": {"path": str(path), "sha256": actual},
        "current_source_pin_count": len(current["source_pins"]),
        "current_guid_lookup_count": len(current["guid_lookup_facts"]),
        "recipe_count": len(current["facts"]["producer"]["recipes"]),
        "source_validation": "PassedAgainstCurrentAuthoredFiles",
        "producer_core_projection": core_projection,
        "scope": {
            "implementation": "Python source preflight only",
            "native_factory_invoked": False,
            "mission_overlay_compiled": False,
            "mission_snapshot_used": False,
            "runtime_admission": False,
            "native_context_qualified": False,
            "mission_ready": False,
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--descriptor", type=Path, required=True)
    parser.add_argument("--sha256", required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    args = parser.parse_args()
    try:
        root = args.repo.resolve(strict=True)
        receipt = validate_descriptor(root, args.descriptor, args.sha256)
        output = Path(args.receipt).absolute()
        need(not output.exists() and output.parent.is_dir(), "fresh receipt path in existing directory required")
        # Exclusive publication. No receipt is written until all source checks pass.
        with output.open("xb") as stream:
            stream.write((json.dumps(receipt, indent=2) + "\n").encode("utf-8"))
        print(json.dumps({"status": receipt["status"], "source_pins": receipt["current_source_pin_count"],
                          "core_projected": False, "mission_ready": False}))
        return 0
    except (OSError, UnicodeError, ValueError, KeyError, TypeError, json.JSONDecodeError) as error:
        print("Barrack/rifle source-host preflight failed closed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

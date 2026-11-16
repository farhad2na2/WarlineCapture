#!/usr/bin/env python3
"""Check whether a manifest declares the exact owner needed by the S073 control.

This cheap check does not resolve generators, compile, verify manifest pins, or
claim the control passes. Run the checked build and control after it succeeds.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

OWNER = "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs"
DECLARATION = "public partial struct InitialUnitsSpawnSystem"
LIMIT = 8 * 1024 * 1024


def inspect_manifest(path: Path) -> dict:
    path = path.resolve(strict=True)
    raw = path.read_bytes()
    if not raw or len(raw) > LIMIT:
        raise ValueError("Manifest must be nonempty and bounded")

    def unique_pairs(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("Duplicate manifest key: " + key)
            result[key] = value
        return result

    manifest = json.loads(raw, object_pairs_hook=unique_pairs)
    if not isinstance(manifest, dict):
        raise ValueError("Manifest object required")
    sources = manifest.get("sources")
    if not isinstance(sources, list):
        raise ValueError("Manifest source array required")
    candidates = [entry for entry in sources if isinstance(entry, dict) and entry.get("path") == OWNER]
    declarations = candidates[0].get("require_declarations") if len(candidates) == 1 else None
    admitted = (len(candidates) == 1 and candidates[0].get("kind") == "native_direct"
                and isinstance(declarations, list) and DECLARATION in declarations)
    return {
        "kind": "S073ReservationOwnerManifestPreflight",
        "status": "OwnerDeclared_CompileStillRequired" if admitted else "OwnerNotDeclared",
        "manifest_path": str(path),
        "manifest_sha256": hashlib.sha256(raw).hexdigest(),
        "owner_path": OWNER,
        "matching_entries": len(candidates),
        "owner_declared_native_direct": admitted,
        "source_resolution_verified": False,
        "compiler_verified": False,
        "control_passed": False,
        "mission_ready": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    args = parser.parse_args()
    try:
        result = inspect_manifest(args.manifest)
    except (OSError, ValueError, TypeError) as error:
        print(json.dumps({"kind": "S073ReservationOwnerManifestPreflight", "status": "InvalidManifest", "error": str(error)}))
        return 2
    print(json.dumps(result, sort_keys=True))
    return 0 if result["owner_declared_native_direct"] else 1


if __name__ == "__main__":
    raise SystemExit(main())

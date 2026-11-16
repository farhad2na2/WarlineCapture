#!/usr/bin/env python3
"""Derive a narrow S073 resource-census manifest from the current base plan."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json"
HELPER = "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073ResourceEconomyRuntimeCensus.cs"
DEFINE = "WARLINE_SOURCE_S073_RESOURCE_CENSUS"
ENTRY_ID = "292F8_SourceS073ResourceEconomyRuntimeCensus.cs"


def derive(base: dict) -> dict:
    if base.get("profile", {}).get("mission") != "S002":
        raise ValueError("Expected the current shared base manifest (S002 profile).")
    variants = base.get("source_profile_variants", [])
    if not any(row.get("mission") == "S073" and row.get("runtime_backend") == "LegacyS073OwnerV1"
               for row in variants):
        raise ValueError("Base manifest has no admitted LegacyS073OwnerV1 source profile.")
    result = json.loads(json.dumps(base))
    defines = result.setdefault("compiler_defines", [])
    if DEFINE in defines:
        raise ValueError("Base manifest already contains the dedicated census define.")
    defines.append(DEFINE)
    sources = result.setdefault("sources", [])
    if any(row.get("id") == ENTRY_ID or row.get("path") == HELPER for row in sources):
        raise ValueError("Base manifest already contains the census helper entry.")
    sources.append({
        "id": ENTRY_ID,
        "kind": "finite_adapter",
        "path": HELPER,
        "role": "host_glue",
        "contains_original_gameplay": False,
        "require_declarations": ["internal static class SourceS073ResourceEconomyRuntimeCensus"],
        "scope": "Opt-in read-only same-world S073 diagnostic census of observed faction economy, tactical materials, resource storage, production profiles and request inputs; no values or ownership are synthesized."
    })
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", type=Path, default=BASE, help="Current shared source manifest")
    parser.add_argument("--output", type=Path, required=True, help="New manifest output path")
    args = parser.parse_args()
    if args.output.exists():
        parser.error("Output must be a fresh path: " + str(args.output))
    base_bytes = args.base.read_bytes()
    plan = derive(json.loads(base_bytes))
    if not (ROOT / HELPER).is_file():
        parser.error("Census helper is missing: " + str(ROOT / HELPER))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    payload = (json.dumps(plan, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    args.output.write_bytes(payload)
    print(json.dumps({
        "status": "Derived",
        "base_manifest": str(args.base.resolve()),
        "base_sha256": hashlib.sha256(base_bytes).hexdigest(),
        "output_manifest": str(args.output.resolve()),
        "output_sha256": hashlib.sha256(payload).hexdigest(),
        "added_define": DEFINE,
        "added_source": HELPER,
        "mission_scope": "S073 LegacyS073OwnerV1 only; invoke with --manifest for the S073 census diagnostic"
    }, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

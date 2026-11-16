#!/usr/bin/env python3
"""Create an explicit alternate manifest for the blocker owner scan experiment."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

GENERATOR = "Tools/CI/SkirmishSourceSimulation/Recipes/static_blocker_owner_history/generate.py"
INPUTS = ["Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs"]
IDENTITY = "349_StaticGridBlockerUpdateSystem.cs"
OUTPUT = "StaticGridBlockerUpdateSystem.Candidate.cs"


def prepare(source_manifest: Path, output_manifest: Path) -> None:
    original = source_manifest.read_bytes()
    manifest = json.loads(original)
    seen = []
    for row in manifest.get("sources", []):
        if row.get("id") != IDENTITY:
            continue
        seen.append(row)
    if len(seen) != 1 or seen[0].get("kind") != "native_direct" or \
       seen[0].get("path") != INPUTS[0]:
        raise ValueError("Exact original current-owner manifest row required")
    row = seen[0]
    row["kind"] = "current_original"
    row.pop("path", None)
    row["generator"] = GENERATOR
    row["generator_inputs"] = INPUTS
    row["generator_output"] = OUTPUT
    row["output"] = OUTPUT
    row["scope"] = "Experimental-only source-host candidate; only combines preflight owner-history scans"
    for key in ("role", "contains_original_gameplay", "require_declarations"):
        row.pop(key, None)
    manifest["experimental_only"] = True
    manifest["experiment"] = "static_blocker_owner_history_combined_scan"
    output_manifest.parent.mkdir(parents=True, exist_ok=True)
    output_manifest.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    if source_manifest.read_bytes() != original:
        raise RuntimeError("Default source manifest changed during alternate manifest preparation")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    prepare(args.manifest.resolve(strict=True), args.output)
    print("Static blocker experimental manifest prepared: " + str(args.output.resolve()))


if __name__ == "__main__":
    main()

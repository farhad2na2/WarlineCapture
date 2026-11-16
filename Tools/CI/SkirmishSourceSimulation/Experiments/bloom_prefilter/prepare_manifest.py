#!/usr/bin/env python3
"""Prepare an explicit alternate manifest for the source-only Bloom experiment."""
import argparse
import json
from pathlib import Path

GENERATOR = "Tools/CI/SkirmishSourceSimulation/Experiments/bloom_prefilter/generate.py"
INPUTS = [
    "Tools/CI/SkirmishSourceSimulation/Adapters/122_OwnedSpatialMultiMap.cs",
    GENERATOR,
]

def prepare(source: Path, output: Path) -> None:
    manifest = json.loads(source.read_text(encoding="utf-8"))
    matched = []
    for row in manifest["sources"]:
        if row.get("id") != "122_OwnedSpatialMultiMap.cs":
            continue
        matched.append(row)
        row["kind"] = "current_original"
        row.pop("path", None)
        row["generator"] = GENERATOR
        row["generator_inputs"] = INPUTS
        row["generator_output"] = "BloomMap/Map.cs"
        row["output"] = "122_OwnedSpatialMultiMap.BloomExperimental.cs"
        row["scope"] = "Opt-in source-only Bloom prefilter experiment; generated copy only; no game source/default manifest mutation"
        row.pop("role", None)
        row.pop("contains_original_gameplay", None)
        row.pop("require_declarations", None)
    if len(matched) != 1:
        raise ValueError("Expected exactly one 122_OwnedSpatialMultiMap.cs manifest row")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")

def main() -> None:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--manifest", type=Path, required=True)
    p.add_argument("--output", type=Path, required=True)
    a = p.parse_args()
    prepare(a.manifest.resolve(strict=True), a.output)
    print("Bloom experimental manifest prepared: " + str(a.output.resolve()))

if __name__ == "__main__": main()

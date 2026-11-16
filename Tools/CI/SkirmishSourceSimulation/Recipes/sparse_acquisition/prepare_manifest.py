#!/usr/bin/env python3
"""Create an explicit alternate source manifest for the sparse acquisition experiment."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

GENERATOR = "Tools/CI/SkirmishSourceSimulation/Recipes/sparse_acquisition/generate.py"
INPUTS = [
    "Assets/Game/Scripts/Systems/TargetAcquisitionKernel.cs",
    "Assets/Game/Scripts/Systems/UnitEngagementSystem.cs",
    "Tools/CI/SkirmishSourceSimulation/Adapters/542_UnitEngagementSystem.TypedHostBridge.cs",
    "Tools/CI/SkirmishSourceSimulation/Adapters/122_OwnedSpatialMultiMap.cs",
    "Tools/CI/SkirmishSourceSimulation/Adapters/312_PaidSourceOwnerStep.retry.cs",
]
OUTPUTS = {
    "387_TargetAcquisitionKernel.cs": "SparseTargetAcquisitionKernel.cs",
    "399_UnitEngagementSystem.cs": "SparseUnitEngagementSystem.cs",
    "542_UnitEngagementSystem.TypedHostBridge.cs": "SparseUnitEngagementHostBridge.cs",
    "312_PaidSourceOwnerStep.retry.cs": "SparsePaidSourceOwnerStep.cs",
}


def prepare(source_manifest: Path, output_manifest: Path) -> None:
    manifest = json.loads(source_manifest.read_text(encoding="utf-8"))
    seen = set()
    for row in manifest["sources"]:
        output = OUTPUTS.get(row.get("id"))
        if output is None:
            continue
        seen.add(row["id"])
        row["kind"] = "current_original"
        row.pop("path", None)
        row["generator"] = GENERATOR
        row["generator_inputs"] = INPUTS
        row["generator_output"] = output
        row["output"] = output
        row["scope"] = "Opt-in source-only sparse acquisition experiment; no game source or default manifest mutation"
        row.pop("role", None)
        row.pop("contains_original_gameplay", None)
        row.pop("require_declarations", None)
    if seen != set(OUTPUTS):
        raise ValueError("Exact supported S002 manifest source identities required: " + repr(sorted(seen)))
    output_manifest.parent.mkdir(parents=True, exist_ok=True)
    output_manifest.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    prepare(args.manifest.resolve(strict=True), args.output)
    print("Sparse acquisition experimental manifest prepared: " + str(args.output.resolve()))


if __name__ == "__main__":
    main()

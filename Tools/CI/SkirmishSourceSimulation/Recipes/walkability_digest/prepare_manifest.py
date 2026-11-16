#!/usr/bin/env python3
"""Prepare a noncanonical S003 manifest for the source-only digest experiment."""
import argparse
import json
from pathlib import Path

GENERATOR = "Tools/CI/SkirmishSourceSimulation/Recipes/walkability_digest/generate.py"
INPUTS = ["Assets/Game/Scripts/Components/GridWalkableSourceWitness.cs",
          "Assets/Game/Scripts/Components/GridComponents.cs",
          "Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs"]
OUTPUT = "SourceExperimentalWalkabilityWitness.cs"


def prepare(source, output):
    manifest = json.loads(Path(source).read_bytes())
    rows = [row for row in manifest["sources"] if row.get("id") == "Shared_GridWalkableSourceWitness.cs"]
    if len(rows) != 1 or rows[0].get("kind") != "native_direct" or rows[0].get("path") != INPUTS[0]:
        raise ValueError("Exact single native witness source required")
    row = rows[0]; row.pop("path")
    row.update(kind="current_original", generator=GENERATOR, generator_inputs=INPUTS,
               generator_output=OUTPUT, output=OUTPUT,
               scope="Opt-in source-only digest experiment; original native source and default witness branch retained")
    output = Path(output); output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x") as stream:
        json.dump(manifest, stream, indent=2); stream.write("\n")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(); parser.add_argument("--manifest", required=True); parser.add_argument("--output", required=True)
    args = parser.parse_args(); prepare(args.manifest, args.output)
    print("[S003WalkabilityDigestManifest] result=Prepared experimental=1")

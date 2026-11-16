#!/usr/bin/env python3
"""Prepare the resume-receipt S026 host adapter for bounded phase observations."""
import argparse
import json
from pathlib import Path

SOURCE = "Tools/CI/SkirmishSourceSimulation/Adapters/S026/294-312_PaidSourceOwnerStep.retry.cs"
GENERATOR = "Tools/CI/SkirmishSourceSimulation/Recipes/s026_retry_drift_phase_trace/generate.py"
OUTPUT = "S026RetryDriftPhaseOverlay.cs"
ROW_ID = "S026_312_PaidSourceOwnerStep.retry.cs"

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_bytes())
    rows = [row for row in manifest["sources"] if row.get("id") == ROW_ID]
    if len(rows) != 1 or rows[0].get("path") != SOURCE or rows[0].get("kind") != "finite_adapter":
        raise ValueError("Exact S026 resume host adapter entry required")
    row = rows[0]
    row.update(kind="current_original", generator=GENERATOR, generator_inputs=[SOURCE],
               generator_output=OUTPUT, output=OUTPUT,
               scope="Opt-in bounded source-host path/movement phase snapshots for one S026 stable actor; observational only")
    for key in ("path", "role", "contains_original_gameplay"):
        row.pop(key, None)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("x") as stream:
        json.dump(manifest, stream, indent=2)
        stream.write("\n")

if __name__ == "__main__":
    main()

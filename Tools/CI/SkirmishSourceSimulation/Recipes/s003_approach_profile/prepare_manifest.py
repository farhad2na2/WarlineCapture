"""Prepare a noncanonical diagnostic manifest; do not alter the default."""
import argparse
import json
from pathlib import Path

GENERATOR = "Tools/CI/SkirmishSourceSimulation/Recipes/s003_approach_profile/generate.py"
SOURCE = "Assets/Game/Scripts/Runtime/Skirmish/SkirmishPublicSelectedApproachProjection.cs"

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text())
    rows = [r for r in manifest["sources"] if r.get("id") == "current_selected_approach_owner"]
    if len(rows) != 1 or rows[0].get("path") != SOURCE:
        raise ValueError("Exact original approach source row required")
    row = rows[0]
    row.update(kind="current_original", generator=GENERATOR, generator_inputs=[SOURCE],
               generator_output="S003ApproachProfile.cs", output="S003ApproachProfile.cs",
               scope="Opt-in source-only approach timing; no native or readiness claim")
    row.pop("path")
    row.pop("require_declarations", None)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("x") as out:
        json.dump(manifest, out, indent=2)
        out.write("\n")

if __name__ == "__main__":
    main()

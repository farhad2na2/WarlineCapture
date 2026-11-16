"""Create a noncanonical manifest for the source-only witness experiment."""
import argparse
import json
from pathlib import Path

def main():
    p=argparse.ArgumentParser();p.add_argument("--manifest",type=Path,required=True);p.add_argument("--output",type=Path,required=True);a=p.parse_args()
    d=json.loads(a.manifest.read_text()); generator="Tools/CI/SkirmishSourceSimulation/Recipes/s003_witness_cache/generate.py"
    inputs=["Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs","Assets/Game/Scripts/Runtime/Skirmish/SkirmishPublicSelectedApproachProjection.cs"]
    wanted={"S026_059_AutonomousEcs.cs":"TrackedS026Ecs.cs","current_selected_approach_owner":"CachedS003Approach.cs"}; seen=set()
    for row in d["sources"]:
        if row.get("id") not in wanted: continue
        name=wanted[row["id"]];seen.add(row["id"]);row.update(kind="current_original",generator=generator,generator_inputs=inputs,generator_output=name,output=name,scope="Experimental source-only witness cache")
        for key in ("path","require_declarations","role","contains_original_gameplay"): row.pop(key,None)
    if seen!=set(wanted): raise ValueError("Exact S003 source manifest required")
    a.output.parent.mkdir(parents=True,exist_ok=True)
    with a.output.open("x") as out: json.dump(d,out,indent=2);out.write("\n")

if __name__=="__main__": main()

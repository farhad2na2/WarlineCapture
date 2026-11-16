"""Add a source-only path trace window to an existing S003 manifest."""
import argparse,json
from pathlib import Path
def main():
 p=argparse.ArgumentParser();p.add_argument("--manifest",type=Path,required=True);p.add_argument("--output",type=Path,required=True);a=p.parse_args();d=json.loads(a.manifest.read_text());rows=[r for r in d["sources"] if r.get("id")=="117_OriginalGroundOwners.cs"]
 if len(rows)!=1 or rows[0].get("path")!="Tools/CI/SkirmishSourceSimulation/Adapters/117_OriginalGroundOwners.cs":raise ValueError("Exact source path trace adapter required")
 inputs=["Tools/CI/SkirmishSourceSimulation/Adapters/117_OriginalGroundOwners.cs","Tools/CI/SkirmishSourceSimulation/Adapters/S026/294-312_PaidSourceOwnerStep.retry.s003.cs","Assets/Game/Scripts/Systems/UnitPathfindingBudget.cs"]
 row=rows[0];row.update(kind="current_original",generator="Tools/CI/SkirmishSourceSimulation/Recipes/s003_path_window/generate.py",generator_inputs=inputs,generator_output="S003PathWindow.cs",output="S003PathWindow.cs",scope="Opt-in source-only pending-state trace; no native parity claim")
 for k in ("path","require_declarations","role","contains_original_gameplay"):row.pop(k,None)
 rows=[r for r in d["sources"] if r.get("id")=="S026_312_PaidSourceOwnerStep.retry.cs"]
 expected="Tools/CI/SkirmishSourceSimulation/Adapters/S026/294-312_PaidSourceOwnerStep.retry.s003.cs"
 if len(rows)!=1 or rows[0].get("path")!=expected:raise ValueError("Exact S003 trace output adapter required")
 row=rows[0];row.update(kind="current_original",generator="Tools/CI/SkirmishSourceSimulation/Recipes/s003_path_window/generate.py",generator_inputs=inputs,generator_output="S003PathTraceOutput.cs",output="S003PathTraceOutput.cs",scope="Opt-in S003 source trace output")
 for k in ("path","require_declarations","role","contains_original_gameplay"):row.pop(k,None)
 rows=[r for r in d["sources"] if r.get("id")=="203_UnitPathfindingBudget.cs"]
 if len(rows)!=1 or rows[0].get("path")!=inputs[2]:raise ValueError("Exact original path budget owner required")
 row=rows[0];row.update(kind="current_original",generator="Tools/CI/SkirmishSourceSimulation/Recipes/s003_path_window/generate.py",generator_inputs=inputs,generator_output="S003PathBudgetObservation.cs",output="S003PathBudgetObservation.cs",scope="Source-only direct completed budget observation")
 for k in ("path","require_declarations","role","contains_original_gameplay"):row.pop(k,None)
 a.output.parent.mkdir(parents=True,exist_ok=True)
 with a.output.open("x") as f:json.dump(d,f,indent=2);f.write("\n")
if __name__=="__main__":main()

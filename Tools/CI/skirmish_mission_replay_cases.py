"""Generate explicitly synthetic mission lifecycle probes from the existing 120-entry catalog."""
import argparse
import copy
import json
from pathlib import Path
import sys
sys.dont_write_bytecode = True
import skirmish_offline_sim_cases as probes
from skirmish_offline_sim import new_output
from build_skirmish_authoring_batches import rows

DERIVED = {"Playing", "Paused", "SimulationDeltaTicks", "Surrender", "ElapsedSeconds", "DeadlineSeconds"}


def convert(case, deadline):
    case = copy.deepcopy(case)
    # A surrendered mission receives an accepted surrender at the first boundary.
    surrender = any("surrender_playing" in row for row in case["steps"])
    source_rows = [row for row in case["steps"] if "facts" in row]
    rows = []
    for row in source_rows:
        f = row["facts"]
        dt = f.get("SimulationDeltaTicks", probes.T)/probes.T
        if case["family"] == "BA": dt = f.get("ElapsedSeconds", 0)
        rows.append(dict(delta_seconds=dt, paused=f["Paused"], surrender=False,
            world_facts={k:v for k,v in f.items() if k not in DERIVED}, repeat=row.get("repeat", 1)))
    if surrender:
        # Native non-BA surrender bypasses collection. BA projection runs first, so use living bases here.
        first = copy.deepcopy(rows[0]); first.update(delta_seconds=0, surrender=True, repeat=1)
        if case["family"] == "BA": first["world_facts"].update(PlayerDesignatedAlive=True, EnemyDesignatedAlive=True)
        rows.insert(0, first)
    outcome, reason = case["expect"]["Outcome"], case["expect"]["Reason"]
    return dict(id=case["id"], family=case["family"], size=case["size"], session_id="session."+case["id"],
        setup_hash="synthetic."+case["id"], initial_phase="Playing", legacy=False, deadline_seconds=deadline,
        area_ids=case["area_ids"], original_ids=case["original_ids"], steps=rows,
        expect=dict(Outcome=outcome, Reason=reason, Frozen=outcome!="None", FreezeCount=int(outcome!="None")))


def generate(project):
    suite, manifest = probes.generate(project)
    matrix = rows(Path(project)/"Design/Roadmap/Skirmish_Expansion/INITIAL_SETUP_MATRIX.csv")
    deadlines = {(r["catalog_id"],r["size_id"]):int(r["deadline_seconds"]) for r in matrix}
    cases = [convert(c, deadlines[(c["id"].split(".")[0],c["size"])]) for c in suite["cases"]]
    manifest["scope"] = "Synthetic mission lifecycle probes with supplied world facts; not autonomous battles or readiness evidence"
    manifest["world_facts_supplied"] = True
    return dict(schema_version=1,cases=cases),manifest


if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument("--project",type=Path,required=True); parser.add_argument("--output",type=Path,required=True)
    args=parser.parse_args(); output=new_output(args.project,args.output)
    suite,manifest=generate(args.project)
    (output/"cases.json").write_text(json.dumps(suite,indent=2)+"\n")
    (output/"manifest.json").write_text(json.dumps(manifest,indent=2)+"\n")
    print(json.dumps(manifest))

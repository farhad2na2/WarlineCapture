#!/usr/bin/env python3
"""Observe original S005 strategy and objective owners in the retained source world."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("ground",HERE/"prepare_s005_ground_owner_experiment.py")
ground=importlib.util.module_from_spec(spec);spec.loader.exec_module(ground)
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def prepare(out):
    ground.prepare(out)
    once=ground.host.start.roads.groups.epoch.roster.base.once
    helper=out/"helper.cs";text=helper.read_text()
    text=once(text,'            sourceHost=new JObject { ["ground_subset"]=groundSubset,',
        '            SourceS005CompiledSetupGuard.RequireExact(setup,Path.GetFullPath("T Logs/SkirmishProduction/s005-experimental-setup-export-20261105-T1/receipt.json"));\n'
        '            var strategySubset=retainedWorld.RunExpandedStrategyOwnerSubset(360);\n'
        '            sourceHost=new JObject { ["strategy_subset"]=strategySubset, ["ground_subset"]=groundSubset,')
    helper.write_text(text)
    root=HERE.parents[3]
    old=root/"T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/S004EnemyStrategyProjection.cs"
    original=root/"Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyStrategySystem.cs"
    generated=out/"SourceEnemyStrategySystem.cs"
    projection=old.read_text()
    old_guard='SourceS004CompiledSetupInput.RequireExact(setup,"/Volumes/T7/Projects/WarlineCapture/T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/compiled-setup.json","237062c97d534052418e00f9897c02f060034bf271509262c91c3dc510678e87"); if(setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault || setup.EnemyPolicy!=null) throw new System.NotSupportedException("Exact S004 null-policy BaseAssault required");'
    new_guard='SourceS005CompiledSetupGuard.RequireExact(setup,System.IO.Path.GetFullPath("T Logs/SkirmishProduction/s005-experimental-setup-export-20261105-T1/receipt.json")); if(setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault || setup.EnemyPolicy==null) throw new System.NotSupportedException("Exact S005 policy-backed BaseAssault required");'
    original_text=original.read_text()
    if 'if (setup.EnemyPolicy != null && setup.ObjectiveKind == SkirmishObjectiveKind.BaseAssault)\n                return SkirmishEnemyCommanderService.Evaluate(em, session, setup, army);' not in original_text:
        raise ValueError("Original policy-backed BaseAssault dispatch changed")
    generated.write_text(once(projection,old_guard,new_guard))
    setup_receipt=root/"T Logs/SkirmishProduction/s005-experimental-setup-export-20261105-T1/receipt.json"
    rsp=out/"compile.rsp";args=[json.loads(line) for line in rsp.read_text().splitlines() if line.strip()]
    if args.count(str(old))!=1 or str(generated) in args:raise ValueError("Exact enemy projection required")
    args[args.index(str(old))]=str(generated)
    rsp.write_text("".join(json.dumps(arg)+"\n" for arg in args))
    prep=out/"preparation.json";doc=json.loads(prep.read_text())
    for path in (helper,rsp,old,original,generated,setup_receipt,Path(__file__).resolve()):doc["source_pins"][str(path)]=sha(path)
    doc["enemy_source_substitution"]="Original policy-backed BaseAssault dispatch, in existing source projection with an S005 full-setup guard; other objective/policy branches reject"
    doc["strategy_scope"]="Original objective and enemy owners, bounded subset, no full scheduler claim"
    prep.write_text(json.dumps(doc,indent=2)+"\n")
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);prepare(p.parse_args().output.resolve())

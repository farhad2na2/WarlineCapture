#!/usr/bin/env python3
"""Bind retained S005 source startup to the shared recurring owner sequence."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
spec=importlib.util.spec_from_file_location("storage",HERE/"prepare_s005_source_storage_experiment.py")
storage=importlib.util.module_from_spec(spec);spec.loader.exec_module(storage)
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def prepare(out):
    storage.prepare(out)
    once=storage.strategy.ground.host.start.roads.groups.epoch.roster.base.once
    preset=ROOT/"Assets/Game/Resources/SkirmishBaseAssault.asset"
    factory=ROOT/"Logs/SkirmishProduction/s005-current-factory-capture-20261105-T2/full-factory-runway-definitions.json"
    captured=json.loads(factory.read_text())["source_pins"]["Assets/Game/Resources/SkirmishBaseAssault.asset"]
    if sha(preset)!=captured:raise ValueError("Captured selected source preset changed")
    helper=out/"helper.cs";text=helper.read_text()
    text=once(text,'var strategySubset=retainedWorld.RunExpandedStrategyOwnerSubset(360);',
        'var strategySubset=retainedWorld.RunExpandedRecurringOwnerDiagnostic(Path.GetFullPath("Assets/Game/Resources/SkirmishBaseAssault.asset"),"'+captured+'",60);')
    helper.write_text(text)
    oldStep=ROOT/"T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/S004AirFuelSafetyOwnerStep.cs"
    oldOrder=ROOT/"T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/S004AirFuelSafetySourceOwnerOrder.cs"
    step=ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/312_PaidSourceOwnerStep.retry.cs"
    order=ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/534_SourceOwnerOrderAdmission.cs"
    rsp=out/"compile.rsp";args=[json.loads(line) for line in rsp.read_text().splitlines() if line.strip()]
    for old,new in ((oldStep,step),(oldOrder,order)):
        if args.count(str(old))!=1 or str(new) in args:raise ValueError("Exact source owner selection required")
        args[args.index(str(old))]=str(new)
    rsp.write_text("".join(json.dumps(arg)+"\n" for arg in args))
    prep=out/"preparation.json";doc=json.loads(prep.read_text())
    for path in (helper,rsp,preset,factory,step,order,Path(__file__).resolve()):doc["source_pins"][str(path)]=sha(path)
    doc["recurring_scope"]="Shared current source owner phases, source-only, production/delivery availability reported separately"
    prep.write_text(json.dumps(doc,indent=2)+"\n")
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);prepare(p.parse_args().output.resolve())

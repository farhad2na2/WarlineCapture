#!/usr/bin/env python3
"""Bind a one-use source storage authority to S005's retained source world."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
spec=importlib.util.spec_from_file_location("strategy",HERE/"prepare_s005_strategy_owner_experiment.py")
strategy=importlib.util.module_from_spec(spec);spec.loader.exec_module(strategy)
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def prepare(out):
    strategy.prepare(out)
    once=strategy.ground.host.start.roads.groups.epoch.roster.base.once
    old=ROOT/"T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/S004CurrentEpochStorageAuthority.cs"
    generated=out/"SourceFreshStorageLifetimeAuthority.cs"
    generated.write_text(once(old.read_text(),"internal sealed class FreshSourceStorageLifetimeAuthority{",
                              "internal sealed partial class FreshSourceStorageLifetimeAuthority{"))
    extension=HERE/"S005FreshStorageAuthority.cs.txt"
    rsp=out/"compile.rsp";args=[json.loads(line) for line in rsp.read_text().splitlines() if line.strip()]
    if args.count(str(old))!=1 or str(extension) in args:raise ValueError("Exact storage source required")
    args[args.index(str(old))]=str(generated);args.append(str(extension))
    rsp.write_text("".join(json.dumps(arg)+"\n" for arg in args))
    receipt=ROOT/"T Logs/SkirmishProduction/s005-experimental-setup-export-20261105-T1/receipt.json"
    receiptHash=sha(receipt)
    helper=out/"helper.cs";text=helper.read_text()
    anchor='            var strategySubset=retainedWorld.RunExpandedStrategyOwnerSubset(360);'
    block='''            string setupReceipt=Path.GetFullPath("T Logs/SkirmishProduction/s005-experimental-setup-export-20261105-T1/receipt.json");
            bool wrongHashRejected=false;
            try { FreshSourceStorageLifetimeAuthority.FromS005RetainedSource(em,session,scenarioDefinition,setup,setupReceipt,new String('0',64)); }
            catch(InvalidDataException) { wrongHashRejected=true; }
            Need(wrongHashRejected,"Wrong storage authority receipt hash must reject");
            var storageAuthority=FreshSourceStorageLifetimeAuthority.FromS005RetainedSource(em,session,scenarioDefinition,setup,setupReceipt,"RECEIPT_HASH");
            var storageBinding=retainedWorld.BindFreshExpandedSourceStorage(storageAuthority,"RECEIPT_HASH");
            bool duplicateStorageRejected=false;
            try { retainedWorld.BindFreshExpandedSourceStorage(storageAuthority,"RECEIPT_HASH"); }
            catch(InvalidOperationException) { duplicateStorageRejected=true; }
            Need(duplicateStorageRejected,"Duplicate storage binding must reject");
'''.replace("RECEIPT_HASH",receiptHash)+anchor
    text=once(text,anchor,block)
    text=once(text,'sourceHost=new JObject { ["strategy_subset"]=strategySubset,',
              'sourceHost=new JObject { ["storage_binding"]=storageBinding,["wrong_hash_rejected"]=wrongHashRejected,["duplicate_storage_rejected"]=duplicateStorageRejected,["strategy_subset"]=strategySubset,')
    helper.write_text(text)
    prep=out/"preparation.json";doc=json.loads(prep.read_text())
    for path in (old,generated,extension,rsp,receipt,helper,Path(__file__).resolve()):doc["source_pins"][str(path)]=sha(path)
    doc["storage_authority_scope"]="One-use S005 full-source-setup receipt; actual same-world components; no native qualification"
    prep.write_text(json.dumps(doc,indent=2)+"\n")
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);prepare(p.parse_args().output.resolve())

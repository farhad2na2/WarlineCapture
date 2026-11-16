#!/usr/bin/env python3
"""Run a retained same-world original ground-owner subset for S005."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("host",HERE/"prepare_s005_retained_source_host.py")
host=importlib.util.module_from_spec(spec);spec.loader.exec_module(host)

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def prepare(out):
    host.prepare(out)
    once=host.start.roads.groups.epoch.roster.base.once
    helper=out/"helper.cs"
    text=helper.read_text()
    text=once(text,'            sourceHost=new JObject { ["attached"]=true,',
        '            var groundSubset=retainedWorld.InitializeAndRunExpandedGroundSubset();\n'
        '            sourceHost=new JObject { ["ground_subset"]=groundSubset, ["attached"]=true,')
    helper.write_text(text)
    prep=out/"preparation.json"; doc=json.loads(prep.read_text())
    for path in (helper,Path(__file__).resolve()):doc["source_pins"][str(path)]=sha(path)
    doc["ground_scope"]="One same-world original occupancy/path/movement subset, no native scheduler claim"
    prep.write_text(json.dumps(doc,indent=2)+"\n")

if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);prepare(p.parse_args().output.resolve())

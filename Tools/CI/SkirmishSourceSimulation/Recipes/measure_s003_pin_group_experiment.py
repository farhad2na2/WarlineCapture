#!/usr/bin/env python3
"""Bounded read-only comparison; not source-build or simulator performance."""
import argparse,hashlib,importlib.util,json,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
from audit_s003_refreshed_evidence_chain import verify_pins
from s003_pin_group_experiment import verify_groups
FILES={"compiler_graph":"T Logs/SkirmishProduction/s003-runtime-graph-refresh-20261104-T1/source-config-graph.json","capture":"Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-r3/data/s003-native-metadata.json","source_build":"T Logs/SkirmishProduction/s002-s003-qualification-refresh-build-20261104-T2/build.json"}
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument("--output",required=True);a=p.parse_args();out=Path(a.output).absolute();out.mkdir(parents=True,exist_ok=False)
 descriptors={k:ROOT/v for k,v in FILES.items()};raw={k:v.read_bytes() for k,v in descriptors.items()};groups={}
 for k,data in raw.items():
  doc=json.loads(data);pins=doc.get("source_pins",doc.get("source_and_reference_pins",{}));groups[k]={str((Path(n) if Path(n).is_absolute() else ROOT/n)):h for n,h in pins.items()}
 akeys,bkeys,ckeys=(set(groups[k]) for k in ("compiler_graph","capture","source_build"))
 def small(keys):return [x for x in sorted(keys) if Path(x).is_file() and not Path(x).is_symlink() and Path(x).stat().st_size<=65536]
 common=small(akeys&bkeys&ckeys)[:20];pair=small((bkeys&ckeys)-akeys)[:20];onlyb=small(bkeys-(akeys|ckeys))[:10];onlyc=small(ckeys-(akeys|bkeys))[:10]
 if min(len(common),len(pair))!=20 or min(len(onlyb),len(onlyc))!=10:raise ValueError("Bounded overlap fixture unavailable")
 choices={"compiler_graph":common,"capture":common+pair+onlyb,"source_build":common+pair+onlyc};subset={k:{n:groups[k][n] for n in v} for k,v in choices.items()}
 if any(len({groups[k][n] for k in groups if n in groups[k]})!=1 for n in common+pair):raise ValueError("Conflicting selected source pin")
 runs=[];expected=None
 for kind in ("baseline","experiment","experiment","baseline"):
  calls=[];start=time.perf_counter()
  if kind=="baseline":value={k:verify_pins(ROOT,v) for k,v in subset.items()};reads=sum(len(v) for v in subset.values())
  else:
   def read(path):calls.append(str(path));return path.read_bytes()
   value=verify_groups(ROOT,subset,read);reads=len(calls)
  elapsed=time.perf_counter()-start
  if expected is None:expected=value
  elif value!=expected:raise ValueError("Pin-group result mismatch")
  runs.append({"mode":kind,"seconds":elapsed,"file_reads":reads})
 if any(descriptors[k].read_bytes()!=raw[k] for k in raw):raise ValueError("Evidence descriptor changed during measurement")
 receipt={"status":"PassedBoundedPinGroupComparison","input_sha256":{str(descriptors[k]):hashlib.sha256(v).hexdigest() for k,v in raw.items()},"selected_group_counts":{k:len(v) for k,v in subset.items()},"unique_files":len(set().union(*(set(v) for v in subset.values()))),"runs":runs,"same_verified_group_results":True,"scope":"Small real-file subset; cache and concurrent I/O uncontrolled; not a build or simulator benchmark","production_integrated":False,"mission_ready":False}
 (out/"receipt.json").open("x").write(json.dumps(receipt,indent=2)+"\n");print(json.dumps(receipt))
if __name__=="__main__":main()

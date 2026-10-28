#!/usr/bin/env python3
"""Materialize a bounded historical subtree from a source-run pinned inventory."""
import argparse,hashlib,json,os,re,stat
from pathlib import Path
MAX=256*1024*1024

def checked(path):
 p=Path(path)
 if not p.is_absolute() or os.path.normpath(str(p))!=str(p):raise ValueError("Canonical absolute path required")
 for q in [*reversed(p.parents),p]:
  if stat.S_ISLNK(q.lstat().st_mode):raise ValueError("Symlink in path: "+str(q))
 st=p.stat()
 if not stat.S_ISREG(st.st_mode) or st.st_size>MAX:raise ValueError("Bounded regular file required: "+str(p))
 return p

def sha(p):
 h=hashlib.sha256()
 with checked(p).open("rb") as stream:
  for chunk in iter(lambda:stream.read(1024*1024),b""):h.update(chunk)
 return h.hexdigest()

def parse(raw):
 def pairs(rows):
  result={}
  for k,v in rows:
   if k in result:raise ValueError("Duplicate JSON key: "+k)
   result[k]=v
  return result
 def nonfinite(value):raise ValueError("Non-finite JSON value: "+value)
 return json.loads(raw,object_pairs_hook=pairs,parse_constant=nonfinite)

def main():
 ap=argparse.ArgumentParser();ap.add_argument("--inventory",required=True);ap.add_argument("--receipt-sha256",required=True);ap.add_argument("--historical-root",required=True);ap.add_argument("--output",required=True);ap.add_argument("--supplemental-descriptors",action="append",default=[]);a=ap.parse_args()
 inventory=Path(a.inventory);receipt=checked(inventory/"source-receipt.json");m=parse(checked(inventory/"inventory.json").read_bytes());r=parse(receipt.read_bytes());pins=r["input_and_runtime_pins"];entries=m["entries"]
 if not re.fullmatch("[0-9a-f]{64}",a.receipt_sha256) or sha(receipt)!=a.receipt_sha256:raise ValueError("Trusted receipt SHA mismatch")
 if r.get("kind")!="SkirmishSourceSimulationRunReceipt" or m.get("kind")!="PinnedSourceReceiptInventory" or m.get("schema_version")!=1 or sha(receipt)!=m.get("source_receipt_sha256") or set(entries)!=set(pins):raise ValueError("Original pinned receipt identity required")
 for flag in ("source_fit_certified","active_mission_water_qualified","camera_qualified","mission_ready","validation_passed","diagnostic_only"):
  if m.get(flag) is not r.get(flag):raise ValueError("Source qualification flag changed: "+flag)
 historical=Path(a.historical_root);out=Path(a.output)
 if not historical.is_absolute() or os.path.normpath(str(historical))!=str(historical) or historical==Path("/") or not out.is_absolute() or out.exists():raise ValueError("Canonical root and fresh output required")
 for parent in [out.parent,*out.parent.parents]:
  if parent.is_symlink():raise ValueError("Symlink output ancestor")
 selected=[]
 for name,e in entries.items():
  source=Path(name)
  if not source.is_absolute() or os.path.normpath(name)!=name:raise ValueError("Canonical historical identity required")
  if not source.is_relative_to(historical):continue
  rel=source.relative_to(historical)
  if not rel.parts or any(part in (".","..","") for part in rel.parts):raise ValueError("Invalid relative path")
  digest=pins[name]
  if not isinstance(digest,str) or not re.fullmatch("[0-9a-f]{64}",digest) or e!={"relative_path":"blobs/"+digest,"sha256":digest,"bytes":e.get("bytes")} or type(e["bytes"]) is not int or not 0<=e["bytes"]<=MAX:raise ValueError("Invalid exact inventory entry")
  blob=inventory/"blobs"/digest
  if checked(blob).stat().st_size!=e["bytes"] or sha(blob)!=digest:raise ValueError("Pinned blob changed: "+name)
  selected.append((rel,digest,blob,e["bytes"]))
 seen={str(rel):digest for rel,digest,_,_ in selected}
 def collect(value):
  if isinstance(value,list):
   for v in value:collect(v)
  elif isinstance(value,dict):
   if set(value)=={"path","sha256"} and isinstance(value["path"],str) and isinstance(value["sha256"],str):
    name=value["path"];digest=value["sha256"];source=Path(name)
    if not source.is_absolute() or os.path.normpath(name)!=name or not re.fullmatch("[0-9a-f]{64}",digest):raise ValueError("Invalid supplemental descriptor")
    if source.is_relative_to(historical):
     rel=source.relative_to(historical)
     if not rel.parts or any(part in (".","..","") for part in rel.parts):raise ValueError("Invalid supplemental relative path")
     if str(rel) in seen:
      if seen[str(rel)]!=digest:raise ValueError("Conflicting supplemental identity")
     else:
      file=checked(source);size=file.stat().st_size
      if sha(file)!=digest:raise ValueError("Supplemental source changed: "+name)
      selected.append((rel,digest,file,size));seen[str(rel)]=digest
   else:
    for v in value.values():collect(v)
 for name in a.supplemental_descriptors:
  digest=pins.get(name)
  if not isinstance(digest,str) or not re.fullmatch("[0-9a-f]{64}",digest):raise ValueError("Supplemental descriptor must be pinned by receipt")
  descriptor=inventory/"blobs"/digest
  if sha(descriptor)!=digest:raise ValueError("Pinned supplemental descriptor changed")
  collect(parse(checked(descriptor).read_bytes()))
 if not selected:raise ValueError("No pinned files under historical root")
 out.mkdir();copied={}
 for rel,digest,blob,size in selected:
  dst=out/rel;dst.parent.mkdir(parents=True,exist_ok=True)
  with blob.open("rb") as src,dst.open("xb") as target:
   for chunk in iter(lambda:src.read(1024*1024),b""):target.write(chunk)
  if dst.stat().st_size!=size or sha(dst)!=digest:raise ValueError("Materialized input changed: "+str(rel))
  copied[str(rel)]=digest
 manifest={"schema_version":1,"kind":"MaterializedPinnedSourceSubtree","historical_root":str(historical),"source_receipt_sha256":m["source_receipt_sha256"],"supplemental_descriptors":{name:pins[name] for name in a.supplemental_descriptors},"files":copied,"qualification":{key:r.get(key) for key in ("validation_passed","diagnostic_only","camera_qualified","active_mission_water_qualified","source_fit_certified","mission_ready")}}
 with (out/"materialization.json").open("x") as f:json.dump(manifest,f,indent=2);f.write("\n")
 print(json.dumps({"status":"MaterializedPinnedSubtree","historical_root":str(historical),"output":str(out),"files":len(copied),"receipt_sha256":m["source_receipt_sha256"]}))
if __name__=="__main__":main()

#!/usr/bin/env python3
"""Copy a source-run receipt’s pinned files unchanged into a verified portable inventory."""
import argparse, hashlib, json, os, re, stat
from pathlib import Path

MAX_BYTES=256*1024*1024
def digest(b): return hashlib.sha256(b).hexdigest()
def regular(path):
 p=Path(path)
 if not p.is_absolute() or any(x in (".","..") for x in p.parts): raise ValueError("Noncanonical absolute path: "+path)
 for component in [*reversed(p.parents),p]:
  st=component.lstat()
  if stat.S_ISLNK(st.st_mode): raise ValueError("Symlink in input path: "+str(component))
 st=p.stat()
 if not stat.S_ISREG(st.st_mode) or st.st_size>MAX_BYTES: raise ValueError("Expected bounded regular input: "+path)
 return p
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
 ap=argparse.ArgumentParser();ap.add_argument("--receipt",required=True);ap.add_argument("--output",required=True);ap.add_argument("--receipt-sha256",required=True);a=ap.parse_args()
 receipt=regular(a.receipt);original=receipt.read_bytes();record=parse(original);
 if not re.fullmatch("[0-9a-f]{64}",a.receipt_sha256) or digest(original)!=a.receipt_sha256:raise ValueError("Trusted receipt SHA mismatch")
 pins=record.get("input_and_runtime_pins")
 if record.get("kind")!="SkirmishSourceSimulationRunReceipt" or not isinstance(pins,dict) or not 0<len(pins)<=100000: raise ValueError("Expected bounded source run receipt pins")
 out=Path(a.output)
 if not out.is_absolute() or out.exists(): raise ValueError("Fresh absolute output required")
 for component in [out.parent,*out.parent.parents]:
  if component.is_symlink():raise ValueError("Symlink output ancestor")
 out.mkdir();blobs=out/"blobs";blobs.mkdir();entries={}
 for path,sha in sorted(pins.items()):
  if not isinstance(path,str) or not isinstance(sha,str) or not re.fullmatch("[0-9a-f]{64}",sha):raise ValueError("Malformed pinned input")
  src=regular(path);raw=src.read_bytes()
  if digest(raw)!=sha:raise ValueError("Input pin mismatch: "+path)
  target=blobs/sha
  if target.exists():
   if digest(target.read_bytes())!=sha:raise ValueError("Blob collision or mutation")
  else:
   with target.open("xb") as f:f.write(raw)
  entries[path]={"relative_path":"blobs/"+sha,"sha256":sha,"bytes":len(raw)}
 (out/"source-receipt.json").write_bytes(original)
 manifest={"schema_version":1,"kind":"PinnedSourceReceiptInventory","source_receipt_sha256":digest(original),"mission":record.get("mission"),"seed":record.get("seed"),"entries":entries,"source_fit_certified":record.get("source_fit_certified"),"active_mission_water_qualified":record.get("active_mission_water_qualified"),"camera_qualified":record.get("camera_qualified"),"mission_ready":record.get("mission_ready"),"validation_passed":record.get("validation_passed"),"diagnostic_only":record.get("diagnostic_only")}
 (out/"inventory.json").write_text(json.dumps(manifest,indent=2)+"\n");print(json.dumps({"status":"InventoryCreated","entries":len(entries),"distinct_blobs":len(list(blobs.iterdir())),"output":str(out)}))
if __name__=="__main__":main()

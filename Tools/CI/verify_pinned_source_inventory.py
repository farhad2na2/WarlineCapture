#!/usr/bin/env python3
"""Fail closed when a portable source-run inventory differs from its receipt."""
import argparse,hashlib,json,re,stat
from pathlib import Path
def read(p):
 for q in [*reversed(p.parents),p]:
  if stat.S_ISLNK(q.lstat().st_mode):raise ValueError("Symlink in inventory: "+str(q))
 st=p.stat()
 if not stat.S_ISREG(st.st_mode) or st.st_size>256*1024*1024:raise ValueError("Expected bounded regular inventory file")
 return p.read_bytes()
def sha(b):return hashlib.sha256(b).hexdigest()
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
 ap=argparse.ArgumentParser();ap.add_argument("root");ap.add_argument("--receipt-sha256",required=True);a=ap.parse_args();root=Path(a.root)
 if not root.is_absolute():raise ValueError("Absolute inventory root required")
 manifest=parse(read(root/"inventory.json"));receipt=read(root/"source-receipt.json");original=parse(receipt);entries=manifest["entries"];pins=original["input_and_runtime_pins"]
 if not re.fullmatch("[0-9a-f]{64}",a.receipt_sha256) or sha(receipt)!=a.receipt_sha256:raise ValueError("Trusted receipt SHA mismatch")
 if manifest["kind"]!="PinnedSourceReceiptInventory" or manifest["schema_version"]!=1 or sha(receipt)!=manifest["source_receipt_sha256"] or set(entries)!=set(pins):raise ValueError("Original receipt identity mismatch")
 for historical,entry in entries.items():
  digest=pins[historical]
  if not isinstance(digest,str) or not re.fullmatch("[0-9a-f]{64}",digest) or entry!=dict(relative_path="blobs/"+digest,sha256=digest,bytes=entry.get("bytes")):raise ValueError("Invalid mapping or digest")
  raw=read(root/"blobs"/digest)
  if len(raw)!=entry["bytes"] or sha(raw)!=digest:raise ValueError("Pinned bytes changed")
 for flag in ("source_fit_certified","active_mission_water_qualified","camera_qualified","mission_ready","validation_passed","diagnostic_only"):
  if manifest.get(flag) is not original.get(flag):raise ValueError("Source qualification flag changed: "+flag)
 print(json.dumps({"status":"InventoryVerified","entries":len(entries),"mission":original["mission"],"seed":original["seed"]}))
if __name__=="__main__":main()

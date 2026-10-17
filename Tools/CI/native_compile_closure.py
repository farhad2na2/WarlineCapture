"""Native compile closure observations; hooks only, never a Unity launcher."""
from pathlib import Path
import hashlib,json,ntpath,re
from skirmish_production_operational_inspector import need
CS=re.compile(r"(?m)^\[\d+/\d+[^\]\r\n]*\] Csc Library/Bee/artifacts/[^/\r\n]+/([^/\r\n]+)\.dll(?: \([^\r\n]*\))?\r?$")
PATH_PREFIXES=("-r:","/r:","-reference:","/reference:","-analyzer:","/analyzer:","-additionalfile:","/additionalfile:","-analyzerconfig:","/analyzerconfig:")
OUTPUT_PREFIXES=("-out:","/out:","-refout:","/refout:")
def key(root,path):
 p=Path(path);p=p if p.is_absolute()else Path(root)/p
 need(".."not in p.parts,"Declared compiler path traversal")
 return ntpath.normcase(ntpath.normpath(str(p)))
def ordinary(root,path,external=None,may_absent=False):
 root=Path(root).resolve();p=Path(path);p=p if p.is_absolute()else root/p
 need('..'not in p.parts,'Declared traversal')
 anchors=[root]+([Path(external).resolve()]if external is not None else [])
 anchor=next((a for a in anchors if p.is_relative_to(a)),None)
 need(anchor is not None,'Foreign declared compiler root')
 cursor=p
 while cursor!=anchor:
  need(not cursor.is_symlink()and not cursor.is_junction(),'Declared reparse path')
  cursor=cursor.parent
 if may_absent and not p.exists():return p
 need(p.resolve(strict=True).is_relative_to(anchor)and p.is_file(),'Ordinary declared compiler file')
 return p

def digest(path):
 h=hashlib.sha256()
 with Path(path).open("rb")as f:
  for b in iter(lambda:f.read(65536),b""):h.update(b)
 return h.hexdigest()
def declared(root,text):
 inputs=[];outputs=[];source=[]
 for line in text.splitlines():
  line=line.strip()
  if not line:continue
  low=line.lower()
  if line.startswith('"')and line.endswith('.cs"'):
   path=line.strip('"');inputs.append(path);source.append(path)
  elif low.startswith(PATH_PREFIXES):inputs.append(line.split(":",1)[1].strip('"'))
  elif low.startswith(OUTPUT_PREFIXES):outputs.append(line.split(":",1)[1].strip('"'))
  elif low.startswith(("-target:","/target:","-define:","/define:","-langversion:","/langversion:","-nowarn:","/nowarn:","/deterministic","-deterministic","/debug","-debug","/optimize","-optimize","/nologo","-nologo","/unsafe","-unsafe","/nostdlib","-nostdlib","/noconfig","-noconfig","/utf8output","-utf8output","/preferreduilang:","/runtimemetadataversion:","-runtimemetadataversion:","/warn:","-warn:","/nullable:","-nullable:")):pass
  else:raise ValueError("Unclassified native RSP option: "+line)
 need(source and len({key(root,p)for p in source})==len(source),"Complete unique owning source membership")
 return inputs,outputs,source

def capture(project,rsp_paths,sdk_refs):
 """Call once before compile and once after; no cache, source rewrite or subprocess."""
 root=Path(project).resolve();need(rsp_paths and len(rsp_paths)==len(set(rsp_paths)),"Exact nonempty owner map")
 need(set(sdk_refs)=={"dotnet","csc"},"Both checked SDK executables")
 sdk=Path(sdk_refs['dotnet']['path']);editor=sdk.parents[2]
 need(sdk==editor/'Data/DotNetSdk/dotnet.exe'and Path(sdk_refs['csc']['path']).is_relative_to(editor/'Data/DotNetSdk/sdk'),'Exact checked Unity SDK layout')
 inputs={};owners={};outputs={}
 for name,ref in sdk_refs.items():
  path=ordinary(root,ref["path"],editor);need(digest(path)==ref["sha256"],"Checked SDK input changed")
  inputs[key(root,path)]=ref["sha256"]
 for owner,rsp in rsp_paths.items():
  path=Path(rsp);path=path if path.is_absolute()else root/path
  path=ordinary(root,path);text=path.read_text(encoding="utf-8-sig");deps,outs,sources=declared(root,text)
  inputs[key(root,path)]=digest(path)
  rows={}
  for value in deps:
   q=Path(value);q=q if q.is_absolute()else root/q
   q=ordinary(root,q,editor)
   k=key(root,q);h=digest(q);need(k not in inputs or inputs[k]==h,"Conflicting declared alias");inputs[k]=h;rows[k]=h
  out={}
  for value in outs:
   q=Path(value);q=q if q.is_absolute()else root/q
   need(q.is_relative_to(root/'Library/Bee/artifacts')and q.name in (owner+'.dll',owner+'.ref.dll'),'Exact native owning output')
   q=ordinary(root,q,may_absent=True);out[key(root,q)]=digest(q)if q.is_file()else None
  owners[owner]={"rsp":key(root,path),"rspSha256":inputs[key(root,path)],"inputs":rows,"sources":[key(root,x)for x in sources],"outputs":out}
  outputs.update(out)
 return {"schema_version":1,"scope":"ObservedNativeDeclaredCompilerInputs-v1","inputs":inputs,"owners":owners,"outputs":outputs}

def validate_values(before,after,text,required,allowed):
 need(set(before)==set(after)=={"schema_version","scope","inputs","owners","outputs"},"Exact closure observation schema")
 need(type(before["schema_version"])is int and before["schema_version"]==after["schema_version"]==1 and before["scope"]==after["scope"]=="ObservedNativeDeclaredCompilerInputs-v1","Recorded native closure version")
 need(set(before["owners"])==set(after["owners"])and set(required)<=set(allowed)<=set(after["owners"]),"Actual owner scope")
 for observation in (before,after):
  combined={}
  for name,row in observation["owners"].items():
   need(isinstance(row["outputs"],dict)and row["outputs"],"Nonempty declared owner output map")
   for path,h in row["outputs"].items():
    need(path not in combined,"One unique owning output")
    combined[path]=h
  need(combined==observation["outputs"],"Exact complete global owning output union")
 fresh=set(CS.findall(text));need(set(required)<=fresh<=set(allowed),"Actual required/allowed numeric Csc owners")
 changes={p for p,h in after["outputs"].items()if before["outputs"].get(p)!=h}
 changedOwners={n for n,v in after["owners"].items()if changes.intersection(v["outputs"])}
 need(changedOwners<=set(allowed),"Output changes outside owning scope")
 need(all(isinstance(h,str)and re.fullmatch("[a-f0-9]{64}",h)for h in after["outputs"].values()),"Actual Csc outputs present")
 need(before["inputs"]==after["inputs"],"Declared input drift: Pending until stable whole-window or genuine per-consumer capture; allowed outputs never waive reference drift")
 for owner,row in after["owners"].items():
  need(row["rsp"]in after["inputs"]and after["inputs"][row["rsp"]]==row["rspSha256"],"Actual RSP input pin")
  need(all(after["inputs"].get(p)==h for p,h in row["inputs"].items())and set(row["sources"])<=set(row["inputs"]),"Every declared owner input recorded")
 return {"result":"MeasuredNativeDeclaredCompilerClosure","freshOwners":sorted(fresh),"changedOutputOwners":sorted(changedOwners),"owners":len(after["owners"]),"declaredInputs":len(after["inputs"]),"inputDriftAllowed":False}

def validate_source_hashes(root,before,after,source_before,source_after):
 for observation,snapshot in ((before,source_before),(after,source_after)):
  expected={key(root,r["path"]):r["sha256"]for r in snapshot["source_manifest"]}
  for owner,row in observation["owners"].items():
   for path in row["sources"]:
    need(path in expected and observation["inputs"].get(path)==row["inputs"].get(path)==expected[path],"Native declared source differs from actual source snapshot")

def validate_output_artifact_refs(root,outputs,artifacts):
 need(isinstance(artifacts,dict)and set(artifacts)==set(outputs),"Every actual native output artifact bound")
 seen=set()
 for output,ref in artifacts.items():
  need(isinstance(ref,dict)and set(ref)=={"path","sha256"}and ref["sha256"]==outputs[output],"Actual output artifact hash equals recorded after output")
  actual=key(root,ref["path"])
  need(actual==output and actual not in seen,"Artifact must be exact canonical declared native output path")
  seen.add(actual)

def validate_capture_origin(result,ref):
 need(isinstance(result,dict)and isinstance(ref,dict)and set(ref)=={"path","sha256"}and result.get("nativeCompileInputClosure")==ref,"Actual native result must seal same-window closure descriptor; no later backfill")

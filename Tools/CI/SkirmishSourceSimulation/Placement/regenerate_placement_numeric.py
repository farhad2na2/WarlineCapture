"""Finite current-original sidewalk SAT and water inclusion loop projection."""
from pathlib import Path
import hashlib,json,sys
def generate(repo,out):
 repo=Path(repo);out=Path(out);out.mkdir(parents=True,exist_ok=True);sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
 from skirmish_source_syntax import method_span
 root=Path(__file__).resolve().parent
 paths=[repo/"Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs",repo/"Assets/Game/Scripts/Systems/SkirmishWaterPlacement.cs",root/"SidewalkNumeric.template",root/"WaterNumeric.template"]
 raw={str(p):p.read_bytes()for p in paths};text={p:raw[str(p)].decode().replace("\r\n","\n")for p in paths}
 def extract(source,signature):
  if source.count(signature)!=1:raise ValueError("Unsupported original geometry shape: "+signature)
  a,b=method_span(source,signature);return source[a:b]
 side=extract(text[paths[0]],"private void AppendSidewalkCells(");body=side[side.index("{")+1:-1]
 body=body.replace("grid.Origin","gridOrigin").replace("grid.CellSize","gridCellSize").replace("grid.Width","gridWidth")
 axis=extract(text[paths[0]],"private static bool OverlapSidewalkAxis(")
 water=extract(text[paths[1]],"foreach (var footprint in footprints)");water=water[water.index("{")+1:-1]
 files={"SharedSidewalkGeometryKernel.cs":text[paths[2]].replace("/*SOURCE:sidewalk*/",body).replace("/*SOURCE:axis*/",axis),"SharedWaterGeometryKernel.cs":text[paths[3]].replace("/*SOURCE:water*/",water)}
 pins={p:hashlib.sha256(v).hexdigest()for p,v in raw.items()}
 if any(Path(p).read_bytes()!=v for p,v in raw.items()):raise ValueError("Geometry source changed before emission")
 for name,value in files.items():
  with(out/name).open("w",encoding="utf-8",newline="\n")as stream:stream.write(value)
 if any(Path(p).read_bytes()!=v for p,v in raw.items()):raise ValueError("Geometry source changed during emission")
 proof={"kind":"CurrentGeometryNumericProjection","source_inputs":pins,"outputs":{n:hashlib.sha256(v.encode()).hexdigest()for n,v in files.items()},"bindings":["Sidewalk instance grid.Width/CellSize/Origin -> explicit finite numeric arguments; method body otherwise current source.","Water original foreach body unchanged, with native RenderSurfaceFootprint/GridConfig parameter boundary."],"adapter_scope":"Finite numeric CLR footprint/data/admission facade only. Unused prototype-footprint builders omitted; not a second geometry policy."}
 (out/"source-extraction.json").write_text(json.dumps(proof,indent=2));return proof
if __name__=="__main__":
 import argparse
 p=argparse.ArgumentParser();p.add_argument("--repo",required=True);p.add_argument("--output",required=True);a=p.parse_args();generate(a.repo,a.output)

#!/usr/bin/env python3
"""Compile the exact extracted drop-buffer reservation owners against a pinned source-host build."""
from __future__ import annotations
import argparse, hashlib, json, subprocess, sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[4]
SOURCE = REPO / "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs"
GRID = REPO / "Assets/Game/Scripts/Components/GridComponents.cs"
PROVIDER = REPO / "Assets/Game/Scripts/Rendering/Contracts/IRenderBoundsPresentation.cs"
FIXTURE = HERE / "ReserveDropBufferCompileFixture.cs"
GENERATOR = HERE / "generate_reserve_probe.py"

def sha(path: Path) -> str: return hashlib.sha256(path.read_bytes()).hexdigest()
def need(ok: bool, msg: str) -> None:
    if not ok: raise RuntimeError(msg)

def main() -> int:
    p=argparse.ArgumentParser(description=__doc__); p.add_argument("--build",type=Path,required=True); p.add_argument("--output",type=Path,required=True); a=p.parse_args()
    build_path=a.build.resolve(strict=True); build=json.loads(build_path.read_text()); need(build.get("kind")=="SkirmishSourceSimulationBuild" and build.get("status")=="Passed","Passing source-host build required")
    pins=build.get("source_and_reference_pins",{}); response=build["compiler_response"]; response_path=Path(response["path"]).resolve(strict=True)
    need(sha(response_path)==response["sha256"],"Pinned compiler response changed")
    for path,digest in pins.items(): need(sha(Path(path).resolve(strict=True))==digest,"Pinned build input changed: "+path)
    before={str(path):sha(path) for path in (SOURCE,GRID,PROVIDER,FIXTURE,GENERATOR)}
    need(not a.output.exists(),"Fresh isolated output directory required"); a.output.mkdir(parents=True)
    gen=a.output/"generated"; result=subprocess.run([sys.executable,str(GENERATOR),"--source",str(SOURCE),"--contract",str(PROVIDER),"--output",str(gen)],capture_output=True,text=True,timeout=30)
    (a.output/"generation.log").write_text(result.stdout+result.stderr)
    need(result.returncode==0,"Extraction failed: "+result.stderr)
    args=[json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    mains=[i for i,v in enumerate(args) if v=="-main:SourceSimulationProgram"]; outs=[i for i,v in enumerate(args) if v.startswith("-out:")]
    need(len(mains)==1 and len(outs)==1,"Expected unique host main/output arguments")
    args[mains[0]]="-main:ReserveDropBufferCompileFixture"; output=a.output/"ReserveDropBufferCompileFixture.exe"; args[outs[0]]="-out:"+str(output)
    generated=gen/"ExtractedOriginalDropBufferReservation.cs"
    args.extend(str(x.resolve(strict=True)) for x in (gen/"ExtractedRenderBoundsContract.cs",generated,FIXTURE))
    rsp=a.output/"fixture.rsp"; rsp.write_text("\n".join(json.dumps(v) for v in args)+"\n")
    dotnet=next((Path(x) for x in pins if Path(x).name=="dotnet"),None); compiler=next((Path(x) for x in pins if Path(x).name=="csc.dll"),None)
    need(dotnet is not None and compiler is not None,"Pinned Roslyn compiler missing")
    compiled=subprocess.run([str(dotnet),str(compiler),"@"+str(rsp)],capture_output=True,text=True,timeout=180)
    log=compiled.stdout+compiled.stderr; (a.output/"compile.log").write_text(log)
    for path,expected in before.items(): need(sha(Path(path))==expected,"Source changed during extraction/compile: "+path)
    receipt={"kind":"SourceHostReserveDropBufferCompileProbe","status":"Passed" if compiled.returncode==0 else "Failed","build":str(build_path),"build_sha256":sha(build_path),"compiler_response_sha256":response["sha256"],"source_pins":before,"compile_exit":compiled.returncode,"source_bodies_copied_verbatim":True,"provider_contract_source":str(PROVIDER),"runtime_executed":False,"unity_run":False}
    (a.output/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n")
    print("[run_reserve_compile_probe] status="+receipt["status"]+" compile_exit="+str(compiled.returncode))
    if compiled.returncode: print(log)
    return 0 if compiled.returncode==0 else 1
if __name__=="__main__": raise SystemExit(main())

#!/usr/bin/env python3
"""Compile and run the actual source capture authority with pinned build inputs."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BUILD=ROOT/"T Logs/SkirmishProduction/source-unit-domain-s002-build-20261105-T4/build.json"
TEST=ROOT/"Tools/CI/SkirmishSourceSimulation/Tests/SourceAuthoredUnitMetadataAuthorityControl.cs"
SETUP=ROOT/"T Logs/SkirmishProduction/s005-experimental-setup-export-20261105-T1/receipt.json"
METADATA=ROOT/"Logs/SkirmishProduction/s005-unit-identity-capture-20261105-T4/unit-production-metadata.json"
RECEIPT=METADATA.parent/"receipt.json"
IDENTITY=METADATA.parent/"unit-prefab-identity-readback.json"
LOG=ROOT/"T Logs/SkirmishProduction/s005-unit-identity-capture-20261105-T4-editor.log"
CONFIG=ROOT/"T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1/mono-config.xml"
def sha(p):
    h=hashlib.sha256()
    with Path(p).open("rb") as f:
        for block in iter(lambda:f.read(1024*1024),b""):h.update(block)
    return h.hexdigest()
def run(out):
    out.mkdir(parents=True,exist_ok=False)
    build=json.loads(BUILD.read_text())
    if build["status"]!="Passed" or build["compile_exit"]!=0:raise ValueError("Passed source build required")
    pins=dict(build["source_and_reference_pins"])
    for p in (BUILD,TEST,SETUP,METADATA,RECEIPT,IDENTITY,LOG,CONFIG,Path(__file__).resolve()):pins[str(p)]=sha(p)
    def verify():
        for p,h in pins.items():
            if not Path(p).is_file() or sha(p)!=h:raise ValueError("Source drift: "+p)
    verify()
    args=[json.loads(x) for x in Path(build["compiler_response"]["path"]).read_text().splitlines() if x.strip()]
    args=["-main:SourceAuthoredUnitMetadataAuthorityControl" if x.startswith("-main:") else
          "-out:"+str(out/"control.exe") if x.startswith("-out:") else x for x in args]
    args.append(str(TEST));rsp=out/"compile.rsp";rsp.write_text("".join(json.dumps(x)+"\n" for x in args))
    compiler=["dotnet","/usr/local/share/dotnet/sdk/10.0.201/Roslyn/bincore/csc.dll","@"+str(rsp)]
    compiled=subprocess.run(compiler,capture_output=True,text=True,timeout=120)
    (out/"compile.log").write_text(compiled.stdout+compiled.stderr)
    if compiled.returncode:raise RuntimeError("Source control compile failed")
    env=os.environ.copy();env["MONO_PATH"]=os.pathsep.join(sorted({str(Path(x[3:]).resolve().parent) for x in args if x.startswith("-r:") and Path(x[3:]).is_file()}))
    command=[build["runtime_selection"]["mono"],"--config",str(CONFIG),"--optimize=-float32",str(out/"control.exe"),
             str(SETUP),str(METADATA),str(RECEIPT),str(LOG),str(ROOT),str(out),str(IDENTITY)]
    result=subprocess.run(command,capture_output=True,text=True,timeout=180,env=env)
    (out/"control.log").write_text(result.stdout+result.stderr)
    marker="[SourceAuthoredUnitMetadataAuthorityControl] result=Passed units=22 negatives=8 templates=0 scope=AssetOnly"
    if result.returncode or marker not in result.stdout:raise RuntimeError("Source authority control failed")
    verify()
    (out/"receipt.json").write_text(json.dumps({"status":"PassedAssetOnly","wrapper_exit":result.returncode,"marker":marker,
        "source_pin_count":len(pins),"source_pins_rechecked":True,"metadata_sha256":sha(METADATA),
        "native_mission":False,"mission_ready":False},indent=2)+"\n")
    print(marker)
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);run(p.parse_args().output.resolve())

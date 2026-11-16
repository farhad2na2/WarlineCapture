#!/usr/bin/env python3
"""Preflight and import an inspected source prefab graph in an isolated source world."""
import argparse
import hashlib
import importlib.util
import json
import os
import subprocess
from pathlib import Path
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
PROJECTION=ROOT/"T Logs/SkirmishProduction/s005-selected-prefab-projection-20261105-T1/projection.json"
RECEIPT=PROJECTION.with_name("receipt.json")
BASE=ROOT/"T Logs/SkirmishProduction/s005-retained-source-host-20261105-T2/compile.rsp"
IDENTITY=HERE/"S005IdentityImportExperiment.cs.txt"
CONFIG=ROOT/"T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1/mono-config.xml"
MONO=Path("/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Resources/Scripting/MonoBleedingEdge/bin/mono")
def sha(p):
    h=hashlib.sha256()
    with Path(p).open("rb") as f:
        for b in iter(lambda:f.read(1024*1024),b""):h.update(b)
    return h.hexdigest()
def once(s,a,b):
    if s.count(a)!=1:raise ValueError("Exact source control anchor required: "+a[:70])
    return s.replace(a,b,1)
def run(out,projection=PROJECTION,receipt=RECEIPT,expected_roots=22):
    evidence=json.loads(receipt.read_text());doc=json.loads(projection.read_text())
    if evidence["status"]!="ProjectedInspectionOnly" or sha(projection)!=evidence["projection_sha256"] or len(doc["requested_prefab_keys"])!=expected_roots or evidence["reference_count"]!=41:
        raise ValueError("Exact selected inspected source domain required")
    spec=importlib.util.spec_from_file_location("prepare_import",HERE/"prepare_s005_identity_import_experiment.py")
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    module.PROJECTION=projection;module.RECEIPT=receipt;module.prepare(out)
    source=IDENTITY.read_text()
    source=once(source,"refs.Count==29 && ledger.Count==29","refs.Count==ledger.Count && refs.Count>0 && refs.Count<=4096")
    source=once(source,"paths.Count==29","paths.Count==refs.Count")
    source=once(source,"result.Prefabs.Count==16","result.Prefabs.Count==keys.Length")
    source=once(source,'Need(muzzle==8 && impact==10 && air==1 && ground==1,"Captured component presence changed");',
        '''int Captured(string component) => ((JArray)doc["entities"]).OfType<JObject>().Count(row => ((JObject)row["components"]).Property(component)!=null);
            Need(muzzle==Captured("Game.Components.UnitMuzzleFlashVfxReference") && impact==Captured("Game.Components.UnitAttackImpactVfxReference") &&
                air==Captured("Game.Components.AirMissileLauncherVfxReferenceComponent") && ground==Captured("Game.Components.GroundMissileLauncherVfxReferenceComponent"),"Captured component presence changed");''')
    generated=out/"SourceIdentityHandles.cs";generated.write_text(source)
    args=[json.loads(x) for x in BASE.read_text().splitlines() if x.strip()]
    swaps={str(IDENTITY):str(generated),str(ROOT/"T Logs/SkirmishProduction/s005-identity-import-inputs-20261105-T1/importer.cs"):str(out/"importer.cs"),
        str(ROOT/"T Logs/SkirmishProduction/s005-identity-import-inputs-20261105-T1/codec.cs"):str(out/"codec.cs")}
    for old,new in swaps.items():
        if args.count(old)!=1:raise ValueError("Expected source closure missing: "+old)
        args[args.index(old)]=new
    args=["-main:S005IdentityImportExperiment" if x.startswith("-main:") else "-out:"+str(out/"control.exe") if x.startswith("-out:") else x for x in args]
    legacy_loader=str(ROOT/"T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/S004DiagnosticUnitMetadataMaterializer.cs")
    current_loader=str(ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/413_GeneratedUnitProductionMetadata.cs")
    if args.count(legacy_loader)!=1 or current_loader in args:raise ValueError("Exact source metadata loader closure required")
    args[args.index(legacy_loader)]=current_loader
    authority=str(ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/SourceAuthoredUnitMetadataAuthority.cs")
    if args.count(authority)>1:raise ValueError("Duplicate source metadata authority")
    if authority not in args:args.append(authority)
    rsp=out/"compile.rsp";rsp.write_text("".join(json.dumps(x)+"\n" for x in args))
    paths={str(projection),str(receipt),str(BASE),str(IDENTITY),str(CONFIG),str(MONO),str(Path(__file__).resolve()),str(HERE/"prepare_s005_identity_import_experiment.py")}
    paths.update(x[3:] if x.startswith("-r:") else x for x in args if x.startswith("-r:") or not x.startswith("-"))
    pins={p:sha(p) for p in paths if Path(p).is_file()}
    def verify():
        for p,h in pins.items():
            if not Path(p).is_file() or sha(p)!=h:raise ValueError("Input changed: "+p)
    compileResult=subprocess.run(["dotnet","/usr/local/share/dotnet/sdk/10.0.201/Roslyn/bincore/csc.dll","@"+str(rsp)],capture_output=True,text=True,timeout=120)
    (out/"compile.log").write_text(compileResult.stdout+compileResult.stderr)
    if compileResult.returncode:raise RuntimeError("Selected template control compile failed")
    env=os.environ.copy();env["MONO_PATH"]=os.pathsep.join(sorted({str(Path(x[3:]).resolve().parent) for x in args if x.startswith("-r:") and Path(x[3:]).is_file()}))
    result=subprocess.run([str(MONO),"--config",str(CONFIG),"--optimize=-float32",str(out/"control.exe"),str(projection)],capture_output=True,text=True,timeout=180,env=env)
    (out/"control.log").write_text(result.stdout+result.stderr)
    if result.returncode:raise RuntimeError("Selected template import control failed")
    lines=[x for x in result.stdout.splitlines() if x.startswith("{")];observed=json.loads(lines[-1])
    if observed["status"]!="ImportedDiagnosticOnly" or observed["roots"]!=expected_roots or observed["omitted_components"]!=0 or not observed["foreign_reference_rejected_before_allocation"] or observed["mission_ready"]:
        raise ValueError("Selected template import observation differs")
    verify()
    (out/"receipt.json").write_text(json.dumps({"status":"ImportedDiagnosticOnly","observation":observed,"source_pin_count":len(pins),
        "source_pins_rechecked":True,"scope":"Isolated source-world template import; diagnostic handles are not Unity instance IDs",
        "native_mission":False,"mission_ready":False},indent=2)+"\n")
    print(json.dumps(observed))
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);p.add_argument("--projection",type=Path,default=PROJECTION);p.add_argument("--projection-receipt",type=Path,default=RECEIPT);p.add_argument("--expected-roots",type=int,default=22)
    a=p.parse_args();run(a.output.resolve(),a.projection.resolve(),a.projection_receipt.resolve(),a.expected_roots)

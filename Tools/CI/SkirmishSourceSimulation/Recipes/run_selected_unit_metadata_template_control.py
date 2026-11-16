#!/usr/bin/env python3
"""Join selected source metadata to imported templates in an isolated source world."""
import argparse,hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE=ROOT/"T Logs/SkirmishProduction/s005-selected-unit-template-control-20261105-T1/compile.rsp"
PROJECTION=ROOT/"T Logs/SkirmishProduction/s005-combined-prefab-projection-20261105-T1/projection.json"
PROJECTION_RECEIPT=PROJECTION.with_name("receipt.json")
CAPTURE=ROOT/"Logs/SkirmishProduction/s005-combined-prefab-capture-20261105-T1/selected-unit-prefab-object-references.json"
CAPTURE_RECEIPT=CAPTURE.with_name("receipt.json")
DATA=ROOT/"Logs/SkirmishProduction/s005-unit-identity-capture-20261105-T4/unit-production-metadata.json"
RECEIPT=DATA.with_name("receipt.json");IDENTITY=DATA.with_name("unit-prefab-identity-readback.json")
LOG=ROOT/"T Logs/SkirmishProduction/s005-unit-identity-capture-20261105-T4-editor.log"
SETUP=ROOT/"T Logs/SkirmishProduction/s005-experimental-setup-export-20261105-T1/receipt.json"
CONFIG=ROOT/"T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1/mono-config.xml"
MONO=Path("/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Resources/Scripting/MonoBleedingEdge/bin/mono")
TEST=ROOT/"Tools/CI/SkirmishSourceSimulation/Tests/SelectedUnitMetadataTemplateControl.cs"
AUTH=ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/SourceAuthoredUnitMetadataAuthority.cs"
LOADER=ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/413_GeneratedUnitProductionMetadata.cs"
OLD=ROOT/"T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/S004DiagnosticUnitMetadataMaterializer.cs"
def sha(path):
    h=hashlib.sha256()
    with Path(path).open("rb") as f:
        for b in iter(lambda:f.read(1024*1024),b""):h.update(b)
    return h.hexdigest()
def run(out):
    out.mkdir(parents=True,exist_ok=False)
    pr=json.loads(PROJECTION_RECEIPT.read_text());cr=json.loads(CAPTURE_RECEIPT.read_text());capture=json.loads(CAPTURE.read_text());unit=json.loads(RECEIPT.read_text())
    if pr["status"]!="ProjectedInspectionOnly" or pr["projection_sha256"]!=sha(PROJECTION) or pr["capture_sha256"]!=sha(CAPTURE) or cr["wrapper_exit"]!=0 or cr["data_sha256"]!=sha(CAPTURE) or capture["selected_unit_metadata_sha256"]!=sha(DATA) or unit["metadata_sha256"]!=sha(DATA) or unit["identity_sha256"]!=sha(IDENTITY):
        raise ValueError("Exact selected capture chain required")
    pins=dict(capture["source_pins"])
    for row in ("capture_assembly","entities_assembly"):
        info=capture[row];pins[info["path"]]=info["sha256"]
    paths=[BASE,PROJECTION,PROJECTION_RECEIPT,CAPTURE,CAPTURE_RECEIPT,DATA,RECEIPT,IDENTITY,LOG,SETUP,CONFIG,MONO,TEST,AUTH,LOADER,Path(__file__).resolve()]
    args=[json.loads(line) for line in BASE.read_text().splitlines() if line.strip()]
    if args.count(str(OLD))!=1 or str(LOADER) in args or str(AUTH) in args:raise ValueError("Exact materializer replacement required")
    args[args.index(str(OLD))]=str(LOADER);args.extend([str(AUTH),str(TEST)])
    args=["-main:SelectedUnitMetadataTemplateControl" if x.startswith("-main:") else "-out:"+str(out/"control.exe") if x.startswith("-out:") else x for x in args]
    for x in args:
        path=x[3:] if x.startswith("-r:") else x
        if (x.startswith("-r:") or not x.startswith("-")) and Path(path).is_file():paths.append(Path(path))
    for path in paths:
        key=str(path);digest=sha(path)
        if key in pins and pins[key]!=digest:raise ValueError("Captured pin changed: "+key)
        pins[key]=digest
    def verify():
        for path,digest in pins.items():
            if not Path(path).is_file() or sha(path)!=digest:raise ValueError("Source changed: "+path)
    verify();rsp=out/"compile.rsp";rsp.write_text("".join(json.dumps(x)+"\n" for x in args))
    compiled=subprocess.run(["dotnet","/usr/local/share/dotnet/sdk/10.0.201/Roslyn/bincore/csc.dll","@"+str(rsp)],capture_output=True,text=True,timeout=120)
    (out/"compile.log").write_text(compiled.stdout+compiled.stderr)
    if compiled.returncode:raise RuntimeError("Metadata/template control compile failed")
    env=os.environ.copy();env["MONO_PATH"]=os.pathsep.join(sorted({str(Path(x[3:]).resolve().parent) for x in args if x.startswith("-r:") and Path(x[3:]).is_file()}))
    command=[str(MONO),"--config",str(CONFIG),"--optimize=-float32",str(out/"control.exe"),str(PROJECTION),str(DATA),str(RECEIPT),str(LOG),str(IDENTITY),str(SETUP),str(ROOT)]
    result=subprocess.run(command,capture_output=True,text=True,timeout=180,env=env);(out/"control.log").write_text(result.stdout+result.stderr)
    if result.returncode:raise RuntimeError("Metadata/template join control failed")
    rows=[line for line in result.stdout.splitlines() if line.startswith("{")];observed=json.loads(rows[-1])
    if observed["status"]!="JoinedDiagnosticOnly" or observed["unit_rows"]!=22 or observed["source_templates"]!=23 or not observed["missing_identity_rejected"] or not observed["wrong_template_rejected"] or observed["factory_domain_qualified"] or observed["mission_ready"]:
        raise ValueError("Metadata/template join result differs")
    verify();(out/"receipt.json").write_text(json.dumps({"status":"JoinedDiagnosticOnly","observation":observed,"source_pin_count":len(pins),
        "source_pins_rechecked":True,"setup_scope":"Serialized setup fixture with policy fingerprint, no live mission",
        "native_mission":False,"mission_ready":False},indent=2)+"\n");print(json.dumps(observed))
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);run(p.parse_args().output.resolve())

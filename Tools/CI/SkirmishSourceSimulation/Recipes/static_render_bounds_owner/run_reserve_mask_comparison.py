#!/usr/bin/env python3
"""Compile and run the original reservation owner against the S073 captured-source mask adapter."""
from __future__ import annotations
import argparse, hashlib, json, os, subprocess, sys, time
from pathlib import Path

HERE=Path(__file__).resolve().parent
REPO=HERE.parents[4]
RESERVE_SOURCE=REPO/"Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs"
COLLECTOR_SOURCE=REPO/"Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs"
CONTRACT_SOURCE=REPO/"Assets/Game/Scripts/Rendering/Contracts/IRenderBoundsPresentation.cs"
GRID_SOURCE=REPO/"Assets/Game/Scripts/Components/GridComponents.cs"
PROJECTION=REPO/"Logs/SkirmishProduction/s073-clearance-projection-input-20261102-r1/static-clearance-from-capture.json"
PROJECTION_SHA="8b5087c8a96ad81113c93fb4fba1632f10c8920bd2ed16fcb168e58d22572065"
ADAPTER_SOURCE=HERE.parents[1]/"Adapters/SourceS073CapturedClearanceProjection.cs"
CAPTURE_GENERATOR=HERE/"generate_source_owner.py"
RESERVE_GENERATOR=HERE/"generate_reserve_probe.py"
REFERENCE_PREP=HERE/"prepare_projection_reference.py"
SHIM=HERE/"SourceRenderBoundsShim.cs"
FIXTURE=HERE/"SourceStaticRenderReserveMaskComparisonFixture.cs"

def sha(path:Path)->str:return hashlib.sha256(path.read_bytes()).hexdigest()
def need(ok:bool,msg:str)->None:
    if not ok: raise RuntimeError(msg)
def verify_build_pins(pins:dict)->None:
    for p,digest in pins.items():
        path=Path(p).resolve(strict=True)
        need(sha(path)==digest,"Pinned host source/input changed: "+str(path))

def extract_s073_adapter(output:Path)->Path:
    generated=ADAPTER_SOURCE.read_text(encoding="utf-8")
    need("internal sealed class S073SourceClearanceProjectionAdapter" in generated,
         "Shared source-projection adapter declaration required")
    need("UNGENERATED_NATIVE" not in generated,"Complete extracted source-projection adapter required")
    target=output/"ExtractedS073SourceClearanceProjectionAdapter.cs"
    target.write_text(generated,encoding="utf-8")
    return target

def main()->int:
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--build",type=Path,required=True); ap.add_argument("--output",type=Path,required=True)
    a=ap.parse_args(); build_path=a.build.resolve(strict=True); out=a.output.absolute()
    need(out.is_relative_to(REPO/"Logs/SkirmishProduction"),"Output must be retained under Logs/SkirmishProduction on the project volume")
    need(not out.exists(),"Fresh isolated output directory required")
    build_raw=build_path.read_bytes(); build=json.loads(build_raw)
    need(build.get("kind")=="SkirmishSourceSimulationBuild" and build.get("status")=="Passed" and build.get("mission")=="S073","Passing S073 source-host build receipt required")
    pins=build["source_and_reference_pins"]; verify_build_pins(pins)
    local_sources=[RESERVE_SOURCE,COLLECTOR_SOURCE,CONTRACT_SOURCE,GRID_SOURCE,ADAPTER_SOURCE,
        CAPTURE_GENERATOR,RESERVE_GENERATOR,REFERENCE_PREP,SHIM,FIXTURE]
    local_hashes={str(p.resolve(strict=True)):sha(p) for p in local_sources}
    response=build["compiler_response"]; rsp_path=Path(response["path"]).resolve(strict=True)
    need(sha(rsp_path)==response["sha256"],"Pinned compiler response changed")
    source_args=[Path(json.loads(line)).resolve(strict=True) for line in rsp_path.read_text().splitlines()
                 if line.strip() and json.loads(line).endswith(".cs")]
    need(all(str(p) in pins for p in source_args),"Source-host response contains unpinned source inputs")
    adapter_path=ADAPTER_SOURCE.resolve(strict=True)
    adapter_pin=str(adapter_path)
    need(adapter_pin in pins and pins[adapter_pin]==sha(adapter_path),
         "Current S073 clearance adapter must be source-build pinned")
    # This adapter is retained but intentionally absent from this build's compiled roots.
    # Compile its exact source into the isolated fixture instead of copying its policy.
    need(sha(PROJECTION)==PROJECTION_SHA,"Captured 90,865-bounds projection pin changed")
    # This validates the complete capture/projection/reference chain and its fixed query record.
    out.mkdir(parents=True)
    prep=subprocess.run([sys.executable,str(REFERENCE_PREP),"--output",str(out/"source-reference.json")],capture_output=True,text=True,timeout=120)
    (out/"reference.log").write_text(prep.stdout+prep.stderr)
    need(prep.returncode==0,"Pinned capture/projection inputs rejected: "+prep.stderr)

    generated=out/"generated"; generated.mkdir()
    capture=subprocess.run([sys.executable,str(CAPTURE_GENERATOR),"--source",str(COLLECTOR_SOURCE),"--output",str(generated/"collector")],capture_output=True,text=True,timeout=30)
    reserve=subprocess.run([sys.executable,str(RESERVE_GENERATOR),"--source",str(RESERVE_SOURCE),"--contract",str(CONTRACT_SOURCE),"--output",str(generated/"reservation")],capture_output=True,text=True,timeout=30)
    (out/"generation.log").write_text(capture.stdout+capture.stderr+reserve.stdout+reserve.stderr)
    need(capture.returncode==0 and reserve.returncode==0,"Fail-closed original source extraction failed")
    generated_sources=[generated/"collector/ExtractedOriginalStaticRenderBoundsOwner.cs",
        generated/"reservation/ExtractedRenderBoundsContract.cs",generated/"reservation/ExtractedOriginalDropBufferReservation.cs"]
    adapter_generated=extract_s073_adapter(generated)
    args=[json.loads(line) for line in rsp_path.read_text().splitlines() if line.strip()]
    adapter_args=[value for value in args if value.endswith(".cs") and Path(value).resolve()==adapter_path]
    need(len(adapter_args)==1,
         "Expected exactly one build-compiled S073 clearance adapter before extracted fixture append")
    # The exact, build-pinned adapter is appended below as an extracted source file.
    # Remove only its original response entry so the fixture compiles it exactly once.
    args.remove(adapter_args[0])
    mains=[i for i,v in enumerate(args) if v=="-main:SourceSimulationProgram"]; outputs=[i for i,v in enumerate(args) if v.startswith("-out:")]
    need(len(mains)==1 and len(outputs)==1,"Expected unique source-host main/output arguments")
    args[mains[0]]="-main:SourceStaticRenderReserveMaskComparisonFixture"
    exe=out/"SourceStaticRenderReserveMaskComparisonFixture.exe"; args[outputs[0]]="-out:"+str(exe)
    args.extend(str(p.resolve(strict=True)) for p in [SHIM,*generated_sources,adapter_generated,FIXTURE])
    fixture_rsp=out/"fixture.rsp"; fixture_rsp.write_text("\n".join(json.dumps(v) for v in args)+"\n")
    dotnet=next((Path(p) for p in pins if Path(p).name=="dotnet"),None); csc=next((Path(p) for p in pins if Path(p).name=="csc.dll"),None)
    need(dotnet is not None and csc is not None,"Pinned Roslyn toolchain missing")
    start=time.monotonic()
    compiled=subprocess.run([str(dotnet),str(csc),"@"+str(fixture_rsp)],capture_output=True,text=True,timeout=240)
    compile_log=compiled.stdout+compiled.stderr; (out/"compile.log").write_text(compile_log)
    receipt={"schema_version":1,"kind":"SourceHostOriginalReserveMaskVsS073ProjectionAdapter","status":"CompileFailed" if compiled.returncode else "Compiled","build_receipt":str(build_path),"build_sha256":sha(build_path),"compiler_response_sha256":response["sha256"],"build_pins_verified_before_compile":True,"source_paths_pinned":[str(RESERVE_SOURCE),str(COLLECTOR_SOURCE),str(CONTRACT_SOURCE),str(GRID_SOURCE),str(ADAPTER_SOURCE)],"source_hashes":{"reserve_owner":sha(RESERVE_SOURCE),"collector_owner":sha(COLLECTOR_SOURCE),"provider_contract":sha(CONTRACT_SOURCE),"grid_utils":sha(GRID_SOURCE),"s073_projection_adapter":sha(ADAPTER_SOURCE)},"captured_projection_sha256":PROJECTION_SHA,"captured_bound_count":90865,"grid_mask_cells":2048*1024,"compile_exit":compiled.returncode,"runtime_executed":False,"unity_run":False,"scope":"Historical pregame captured bounds; no provider parity, mission admission or readiness claim"}
    if compiled.returncode:
        verify_build_pins(pins)
        need(all(sha(Path(p))==digest for p,digest in local_hashes.items()),"Source input changed during failed compile")
        receipt["all_build_pins_verified_after_compile"]=True; receipt["elapsed_seconds"]=round(time.monotonic()-start,3)
        (out/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n"); print("[run_reserve_mask_comparison] status=CompileFailed compile_exit="+str(compiled.returncode)); print(compile_log); return 1
    runtime={k:Path(v).resolve(strict=True) for k,v in build["runtime_selection"].items()}
    helper=runtime["contents"]/"Frameworks/libMonoPosixHelper.dylib"; native=runtime["mono"].parent.parent/"lib/libmono-native.dylib"
    need(helper.is_file() and native.is_file(),"Pinned Mono support libraries missing")
    maps='<dllmap dll="MonoPosixHelper" target="'+str(helper)+'" os="!windows"/>'
    maps+=''.join('<dllmap dll="'+n+'" target="'+str(native)+'" os="!windows"/>' for n in ("System.Native","System.Net.Security.Native","System.Security.Cryptography.Native.Apple"))
    config=out/"mono-config.xml"; config.write_text("<configuration>"+maps+"</configuration>\n")
    env=os.environ.copy(); env["MONO_PATH"]=os.pathsep.join([str(REPO/"Library/ScriptAssemblies"),str(runtime["engine"]),str(runtime["newtonsoft"].parent),str(Path(build["placement_build"]).parent)])
    ran=subprocess.run([str(runtime["mono"]),"--config",str(config),str(exe),str(REPO),str(PROJECTION)],env=env,capture_output=True,text=True,timeout=600)
    run_log=ran.stdout+ran.stderr; (out/"run.log").write_text(run_log)
    marker="[SourceStaticRenderReserveMaskComparisonFixture] result=Passed input_bounds=90865 preferred_count=7"
    receipt["status"]="Passed" if ran.returncode==0 and marker in ran.stdout else "RuntimeFailed"
    receipt["runtime_executed"]=True; receipt["run_exit"]=ran.returncode; receipt["all_build_pins_verified_after_run"]=True
    receipt["mask_comparison_pass_marker"]=marker in ran.stdout; receipt["elapsed_seconds"]=round(time.monotonic()-start,3)
    verify_build_pins(pins)
    need(all(sha(Path(p))==digest for p,digest in local_hashes.items()),"Source input changed during runtime comparison")
    need(sha(rsp_path)==response["sha256"],"Compiler response changed during run")
    (out/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n")
    print("[run_reserve_mask_comparison] status="+receipt["status"]+" compile_exit=0 run_exit="+str(ran.returncode))
    if ran.returncode or marker not in ran.stdout: print(run_log)
    return 0 if receipt["status"]=="Passed" else 1
if __name__=="__main__": raise SystemExit(main())

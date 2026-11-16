#!/usr/bin/env python3
"""Generate and compile an isolated generic-clearance overlay against S026 source build."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[5]
RECIPE = Path(__file__).resolve().parent
BASE = ROOT / "T Logs/SkirmishProduction/s026-staged-handoff-build-20261107-r1"
BUILD_JSON = BASE / "build.json"
ORIGINAL = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/527_GeneratedS002DropClearance.cs"
PASS = "[SourceMapClearanceHost] result=Passed generic_package=1 wrong_frame=1 insufficient_98_cell_envelope=1 small_cell_safe=1 small_cell_raw_rejected=1 reservation_loop=original world=0 mission=0"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(value: bool, message: str) -> None:
    if not value:
        raise ValueError(message)


def module():
    spec = importlib.util.spec_from_file_location("source_map_clearance_preflight", RECIPE / "preflight.py")
    need(spec is not None and spec.loader is not None, "package validator unavailable")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


BRANCH = r'''   if((string)root["kind"]=="SourceDerivedCampaignMapDropClearanceProjection"){
    if((int?)root["schema_version"]!=1||(bool?)root["live_occupancy_qualified"]!=false||(bool?)root["native_presentation_qualified"]!=false||(bool?)root["source_fit_certified"]!=false||(bool?)root["mission_ready"]!=false)throw new InvalidDataException("Unqualified generic source-map clearance projection required");
    var capturePin=root["package_capture"] as JObject;if(capturePin==null||capturePin.Count!=2)throw new InvalidDataException("Exact generic clearance capture pin required");
    var capturePath=(string)capturePin["path"];var captureSha=(string)capturePin["sha256"];var capture=JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(capturePath,captureSha,256*1024*1024)),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
    if((string)capture["result"]!="Collected"||(bool?)capture["worldDisposed"]!=true||(bool?)capture["missionInitialized"]!=false||(bool?)capture["missionReady"]!=false||(bool?)capture["sourceFitCertified"]!=false)throw new InvalidDataException("Disposed source-only capture required");
    var staticMap=root["static_map"] as JObject;var frame=root["frame"] as JObject;var collector=root["collector"] as JObject;var actual=capture["fixedMapReadDomain"]?["dropClearanceBounds"] as JObject;
    if(staticMap==null||frame==null||collector==null||actual==null)throw new InvalidDataException("Package clearance rows differ from pinned original capture");
    if((string)staticMap["operation_map_id"]!=(string)capture["operationMapId"]||(string)collector["authority"]!="OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain"||(string)actual["authority"]!="OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain"||(bool?)actual["runtime_presentation_activation_sampled"]!=false)throw new InvalidDataException("Generic original collector authority/map binding required");
    var mapDefinition=staticMap["definition"] as JObject;if(mapDefinition==null||mapDefinition.Count!=2)throw new InvalidDataException("Exact source map definition pin required");AutonomousWorld.PinnedFile((string)mapDefinition["path"],(string)mapDefinition["sha256"],8*1024*1024);
    sourceWidth=(int)frame["width"];sourceHeight=(int)frame["height"];sourceCell=(float)frame["cell_size"];var fo=frame["origin"] as JArray;if(fo==null||fo.Count!=3||sourceWidth<=0||sourceHeight<=0||!(sourceCell>0)||!math.isfinite(sourceCell))throw new InvalidDataException("Finite package frame required");sourceOrigin=new float3((float)fo[0],(float)fo[1],(float)fo[2]);
    var projectedOrigin=collector["grid_origin"] as JArray;if((int?)collector["grid_width"]!=sourceWidth||(int?)collector["grid_height"]!=sourceHeight||(float?)collector["cell_size"]!=sourceCell||projectedOrigin==null||projectedOrigin.Count!=3||(float)projectedOrigin[0]!=sourceOrigin.x||(float)projectedOrigin[1]!=sourceOrigin.y||(float)projectedOrigin[2]!=sourceOrigin.z)throw new InvalidDataException("Projection frame differs from captured collector frame");
    var capturedFrame=capture["fixedMapReadDomain"]?["gridFrame"];var co=capturedFrame?["origin"];if((int?)capturedFrame?["width"]!=sourceWidth||(int?)capturedFrame?["height"]!=sourceHeight||(float?)capturedFrame?["cellSize"]!=sourceCell||co==null||(float)co["x"]!=sourceOrigin.x||(float)co["y"]!=sourceOrigin.y||(float)co["z"]!=sourceOrigin.z)throw new InvalidDataException("Package frame differs from captured fixed-map frame");
    if(!math.all(math.isfinite(sourceOrigin))||(int?)actual["grid_width"]!=sourceWidth||(int?)actual["grid_height"]!=sourceHeight||(float?)actual["cell_size"]!=sourceCell||(float?)actual["minimum_extent"]!=.25f||(float?)actual["maximum_extent"]!=20f||(int?)actual["source_search_radius_cells"]!=96||(int?)actual["source_padding_cells"]!=2)throw new InvalidDataException("Generic collector frame/parameters differ");
    var ro=actual["grid_origin"] as JArray;if(ro==null||ro.Count!=3||(float)ro[0]!=sourceOrigin.x||(float)ro[1]!=sourceOrigin.y||(float)ro[2]!=sourceOrigin.z)throw new InvalidDataException("Generic collector origin mismatch");
    var qr=collector["query_rect"] as JArray;if(qr==null||qr.Count!=4)throw new InvalidDataException("Complete collector query envelope required");float qx=(float)qr[0],qz=(float)qr[1],qw=(float)qr[2],qh=(float)qr[3];float qxmax=qx+qw,qzmax=qz+qh;
    float safe=math.max(.01f,sourceCell);int padding=96+2;float requiredMinX=sourceOrigin.x+(0-padding)*safe,requiredMinZ=sourceOrigin.z+(0-padding)*safe;float rightLocalMin=sourceOrigin.x+((sourceWidth-1)-padding)*safe,bottomLocalMin=sourceOrigin.z+((sourceHeight-1)-padding)*safe;float requiredMaxX=rightLocalMin+(padding*2+1)*safe,requiredMaxZ=bottomLocalMin+(padding*2+1)*safe;if(!math.all(math.isfinite(new float4(qx,qz,qw,qh)))||qw<=0||qh<=0||qx>requiredMinX||qz>requiredMinZ||qxmax<requiredMaxX||qzmax<requiredMaxZ)throw new InvalidDataException("Collector query envelope omits exact Reserve-expanded domain");globalXMin=qx;globalYMin=qz;globalXMax=qxmax;globalYMax=qzmax;
    if(!JToken.DeepEquals(qr,actual["query_rect"]))throw new InvalidDataException("Package envelope differs from pinned original collector");var genericRows=actual["bounds"] as JArray;if(genericRows==null||genericRows.Count<1||genericRows.Count>1000000)throw new InvalidDataException("Complete bounded original source rows required");centers=new float3[genericRows.Count];extents=new float3[genericRows.Count];
    for(int i=0;i<genericRows.Count;i++){var c=genericRows[i]["center"] as JArray;var e=genericRows[i]["extents"] as JArray;if(c==null||c.Count!=3||e==null||e.Count!=3)throw new InvalidDataException("Original source bound shape required");centers[i]=new float3((float)c[0],(float)c[1],(float)c[2]);extents[i]=new float3((float)e[0],(float)e[1],(float)e[2]);if(!math.all(math.isfinite(centers[i]))||!math.all(math.isfinite(extents[i]))||math.any(extents[i]<0))throw new InvalidDataException("Invalid original source bound");}
    return;
   }
'''


HARNESS = r'''using System;
using System.IO;
using Game.Runtime;
internal static class SourceMapClearanceHostControls {
 static int Main(string[] args) {
  if(args.Length!=5)return 64;
  bool good=false,badFrame=false,badEnvelope=false,smallSafe=false,smallRaw=false;
  try{new GeneratedS002DropClearance(args[0],Hash(args[0]));good=true;}catch(Exception e){Console.Error.WriteLine("valid projection rejected: "+e);return 2;}
  try{new GeneratedS002DropClearance(args[1],Hash(args[1]));}catch(Exception){badFrame=true;}
  try{new GeneratedS002DropClearance(args[2],Hash(args[2]));}catch(Exception){badEnvelope=true;}
  try{new GeneratedS002DropClearance(args[3],Hash(args[3]));smallSafe=true;}catch(Exception e){Console.Error.WriteLine("safe small-cell projection rejected: "+e);return 4;}
  try{new GeneratedS002DropClearance(args[4],Hash(args[4]));}catch(Exception){smallRaw=true;}
  if(!good||!badFrame||!badEnvelope||!smallSafe||!smallRaw)return 3;
  Console.WriteLine("[SourceMapClearanceHost] result=Passed generic_package=1 wrong_frame=1 insufficient_98_cell_envelope=1 small_cell_safe=1 small_cell_raw_rejected=1 reservation_loop=original world=0 mission=0");
  return 0;
 }
 static string Hash(string p){using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
}
'''


def run(package: Path, output: Path) -> dict:
    output = output.resolve()
    need(output.is_relative_to(RECIPE.resolve()) and not output.exists(), "fresh output directory under recipe required")
    output.mkdir(parents=True)
    pre = module()
    package_obj = pre.obj(package)
    projection = pre.validate(package_obj)
    projection_path = output / "valid-projection.json"
    projection_path.write_text(json.dumps(projection, indent=2, sort_keys=True, allow_nan=False) + "\n", encoding="utf-8")
    wrong_frame = json.loads(json.dumps(projection)); wrong_frame["frame"]["width"] += 1
    wrong_frame_path = output / "wrong-frame.json"; wrong_frame_path.write_text(json.dumps(wrong_frame, indent=2) + "\n")
    narrow = json.loads(json.dumps(projection)); narrow["collector"]["query_rect"] = [narrow["frame"]["origin"][0], narrow["frame"]["origin"][2], narrow["frame"]["width"] * narrow["frame"]["cell_size"], narrow["frame"]["height"] * narrow["frame"]["cell_size"]]
    narrow_path = output / "insufficient-envelope.json"; narrow_path.write_text(json.dumps(narrow, indent=2) + "\n")
    from_small = module()
    small_safe_path = write_small_cell_control(projection, output, from_small, safe_margin=True)
    small_raw_path = write_small_cell_control(projection, output, from_small, safe_margin=False)

    original = ORIGINAL.read_text(encoding="utf-8")
    anchor = '   if((string)root["kind"]=="S026InactiveOriginalDropBoundsProjection"){'
    need(original.count(anchor) == 1, "exact original clearance branch anchor required")
    reserve_start = original.index("  internal void Reserve(")
    reserve_loop = original[reserve_start:]
    overlay_text = original.replace(anchor, BRANCH + anchor)
    need(overlay_text[overlay_text.index("  internal void Reserve("):] == reserve_loop,
         "original Reserve implementation must remain byte-identical")
    overlay = output / "GeneratedS002DropClearance.source-map-overlay.cs"
    overlay.write_text(overlay_text, encoding="utf-8")
    harness = output / "SourceMapClearanceHostControls.cs"; harness.write_text(HARNESS, encoding="utf-8")

    build = json.loads(BUILD_JSON.read_text(encoding="utf-8"))
    need(build.get("status") == "Passed" and build.get("mission") == "S026" and build.get("compile_exit") == 0,
         "passed S026 source build receipt required")
    rsp = Path(build["compiler_response"]["path"])
    need(sha(rsp) == build["compiler_response"]["sha256"], "pinned source-host response changed")
    args = rsp.read_text(encoding="utf-8").splitlines()
    rewritten = []; replaced = False; main = out = False
    for line in args:
        val = line.strip().strip('"')
        if val.endswith("/Adapters/DeclaredGround/527_GeneratedS002DropClearance.cs"):
            rewritten.append('"' + str(overlay) + '"'); replaced = True
        elif val.startswith("-main:"):
            rewritten.append('"-main:SourceMapClearanceHostControls"'); main = True
        elif val.startswith("-out:"):
            exe = output / "SourceMapClearanceHostControls.exe"
            rewritten.append('"-out:' + str(exe) + '"'); out = True
        else:
            rewritten.append(line)
    need(replaced and main and out, "source-host response did not contain exact replacement points")
    rewritten.append('"' + str(harness) + '"')
    rsp_out = output / "compile.rsp"; rsp_out.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
    pins = build["source_and_reference_pins"]
    csc = next(Path(x) for x in pins if x.endswith("/Roslyn/bincore/csc.dll") and Path(x).is_file() and sha(Path(x)) == pins[x])
    dotnet = next(Path(x) for x in pins if Path(x).name == "dotnet" and Path(x).is_file() and sha(Path(x)) == pins[x])
    compile_run = subprocess.run([str(dotnet), str(csc), "@" + str(rsp_out)], capture_output=True, text=True, timeout=300)
    (output / "compile.log").write_text(compile_run.stdout + compile_run.stderr, encoding="utf-8")
    need(compile_run.returncode == 0 and exe.is_file(), "targeted source-host compile failed")
    rt = build["runtime_selection"]
    env = dict(__import__("os").environ)
    env["MONO_PATH"] = __import__("os").pathsep.join((str(BASE / "placement"), rt["engine"], str(Path(rt["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(output)))
    execution = subprocess.run([rt["mono"], str(exe), str(projection_path), str(wrong_frame_path), str(narrow_path), str(small_safe_path), str(small_raw_path)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=120)
    (output / "run.log").write_text(execution.stdout + execution.stderr, encoding="utf-8")
    need(execution.returncode == 0 and execution.stdout.splitlines().count(PASS) == 1, "targeted C# source-host controls failed")
    manifest = {"schema_version": 1, "kind": "SourceMapClearanceExperimentalHostManifest", "status": "SourceOnlyOverlayBuilt", "base_build": {"path": str(BUILD_JSON), "sha256": sha(BUILD_JSON), "compile_response": {"path": str(rsp), "sha256": sha(rsp)}}, "clearance_source_replacement": {"original_path": str(ORIGINAL), "original_sha256": sha(ORIGINAL), "overlay_path": str(overlay), "overlay_sha256": sha(overlay)}, "host_compile": {"response_path": str(rsp_out), "response_sha256": sha(rsp_out), "entrypoint": "SourceMapClearanceHostControls", "executable_path": str(exe), "executable_sha256": sha(exe)}, "package_projection": {"path": str(projection_path), "sha256": sha(projection_path), "capture_path": projection["package_capture"]["path"], "capture_sha256": projection["package_capture"]["sha256"]}, "original_reserve_loop_preserved_byte_identically": True, "native_manifest_modified": False, "scope": "Experimental C# source-host clearance admission branch; no reservation call, world, mission, native parity, or readiness"}
    manifest_path = output / "source-host-manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    receipt = {"kind": "SourceMapClearanceOverlayHostReceipt", "status": "PassedSourceOnlyControls", "source_host_manifest": {"path": str(manifest_path), "sha256": sha(manifest_path)}, "source_build": {"path": str(BUILD_JSON), "sha256": sha(BUILD_JSON), "response_sha256": sha(rsp)}, "original_clearance_source": {"path": str(ORIGINAL), "sha256": sha(ORIGINAL)}, "overlay_source": {"path": str(overlay), "sha256": sha(overlay)}, "original_reserve_loop_preserved_byte_identically": True, "reservation_margin_model": "Original float32 Reserve operation order; safe=max(.01f,cellSize); local expanded rectangle is 197 cells wide from lo=preferred-98 to hi=preferred+99", "small_cell_safe_margin_accepted": True, "small_cell_raw_cell_margin_rejected": True, "package_capture_sha256": projection["package_capture"]["sha256"], "bounds_count": "Capture pinned; loaded from original record", "control_executable_sha256": sha(exe), "compile_response_sha256": sha(rsp_out), "compile_log_sha256": sha(output / "compile.log"), "run_log_sha256": sha(output / "run.log"), "scope": "C# constructor/preflight only; no reservation execution, world, mission, native parity, or readiness"}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(PASS)
    return receipt


def write_small_cell_control(template: dict, output: Path, preflight, safe_margin: bool) -> Path:
    """Write a compact internally pinned capture/projection for cellSize=.005f."""
    f32 = preflight.f32
    width, height, cell = 64, 32, .005
    frame = {"width": width, "height": height, "cell_size": cell, "origin": [0, 0, 0]}
    safe = .01 if safe_margin else cell
    padding, span = 98, 197
    min_edge = f32(f32(-padding) * f32(safe))
    x_low = f32(f32(width - 1 - padding) * f32(safe))
    z_low = f32(f32(height - 1 - padding) * f32(safe))
    x_max = f32(x_low + f32(span * f32(safe)))
    z_max = f32(z_low + f32(span * f32(safe)))
    rect = [min_edge, min_edge, f32(x_max - min_edge), f32(z_max - min_edge)]
    record = {"authority": "OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain",
              "runtime_presentation_activation_sampled": False, "grid_width": width, "grid_height": height,
              "cell_size": cell, "minimum_extent": .25, "maximum_extent": 20.0,
              "source_search_radius_cells": 96, "source_padding_cells": 2,
              "grid_origin": [0, 0, 0], "query_rect": rect,
              "bounds": [{"center": [0, 0, 0], "extents": [.5, 0, .5]}]}
    op_id = template["static_map"]["operation_map_id"]
    capture = {"result": "Collected", "worldDisposed": True, "missionInitialized": False,
               "missionReady": False, "sourceFitCertified": False, "operationMapId": op_id,
               "fixedMapReadDomain": {"gridFrame": {"width": width, "height": height, "cellSize": cell,
                 "origin": {"x": 0, "y": 0, "z": 0}}, "dropClearanceBounds": record}}
    capture_path = output / ("small-cell-safe-capture.json" if safe_margin else "small-cell-raw-capture.json")
    capture_path.write_text(json.dumps(capture, separators=(",", ":"), allow_nan=False) + "\n", encoding="utf-8")
    projection = json.loads(json.dumps(template))
    projection["package_capture"] = {"path": str(capture_path.resolve()), "sha256": sha(capture_path)}
    projection["frame"] = frame
    projection["collector"].update({"grid_width": width, "grid_height": height, "cell_size": cell,
        "grid_origin": [0, 0, 0], "query_rect": rect})
    path = output / ("small-cell-safe-projection.json" if safe_margin else "small-cell-raw-projection.json")
    path.write_text(json.dumps(projection, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return path


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--package", type=Path, default=RECIPE.parent / "source_map_package/s026-source-derived-map-package.json")
    ap.add_argument("--output", type=Path, required=True)
    args = ap.parse_args()
    try:
        run(args.package.resolve(strict=True), args.output)
        return 0
    except Exception as exc:
        print(str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

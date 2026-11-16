#!/usr/bin/env python3
"""Build an opt-in, source-only S074 pre-unit grid observation executable.

This clones the generated StartingWorld output from a pinned source build, inserts
one read-only observation immediately before the original opening-unit loop, and
compiles a separate executable. It does not change placement or mission inputs.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE_BUILD = ROOT / "T Logs/SkirmishProduction/s074-origin-source-build-20261113-r5"
DEFAULT_OUT = ROOT / "T Logs/SkirmishProduction/s074-origin-grid-observer-20261113-r2"
LAYOUT = ROOT / "T Logs/SkirmishProduction/s074-source-origin-centered-candidate-20261107-r1/layout.json"
MARKER = "while(opening.TryBeginMember(out var member))"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", type=Path, default=BASE_BUILD)
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT)
    args = parser.parse_args()
    build = args.build.resolve(strict=True)
    out = args.out.absolute()
    if out.exists():
        raise SystemExit(f"Refusing non-fresh output directory: {out}")
    receipt_path = build / "build.json"
    receipt = json.loads(receipt_path.read_text())
    if (receipt.get("kind") != "SkirmishSourceSimulationBuild" or receipt.get("status") != "Passed"
            or receipt.get("mission") != "S074" or receipt.get("seed") != 104803
            or receipt.get("compile_exit") != 0):
        raise SystemExit("Expected the passed pinned S074 seed-104803 source build")
    pins = receipt.get("source_and_reference_pins")
    if not isinstance(pins, dict) or not pins:
        raise SystemExit("Source build pins are missing")
    for name, expected in pins.items():
        path = Path(name)
        if not path.is_file() or sha(path) != expected:
            raise SystemExit(f"Source build pin changed: {name}")
    layout = json.loads(LAYOUT.read_text())
    pad_rows = [p for p in layout["pads"] if p["PadId"] in {
        "s074.provisional.spawn.infantry.player.pad",
        "s074.provisional.spawn.infantry.enemy.pad",
        "s074.provisional.spawn.vehicle.player.pad",
        "s074.provisional.spawn.vehicle.enemy.pad",
    }]
    if len(pad_rows) != 4:
        raise SystemExit("Expected exactly four retained origin spawn pads")

    out.mkdir(parents=True)
    generated = build / "source/generated/StartingWorld.cs"
    variant = out / "StartingWorld.S074OriginGridObserver.cs"
    source = generated.read_text()
    if source.count(MARKER) != 1:
        raise SystemExit("Expected one original first-unit loop insertion point")
    source = source.replace(MARKER,
        'S074OriginSpawnGridDiagnostic.Capture(Entities,GridEntity);' + MARKER)
    variant.write_text(source)
    observer = out / "S074OriginSpawnGridDiagnostic.cs"
    observer.write_text(observer_source(pad_rows))

    # Reuse the exact compiler/reference argv, replacing only generated source
    # and executable output paths. All original dependencies remain pinned.
    original_rsp = build / "compile.rsp"
    argv = [json.loads(line) for line in original_rsp.read_text().splitlines() if line]
    generated_args = [i for i, item in enumerate(argv) if item == str(generated)]
    output_args = [i for i, item in enumerate(argv) if isinstance(item, str) and item.startswith("-out:")]
    if len(generated_args) != 1 or len(output_args) != 1:
        raise SystemExit("Could not uniquely identify generated source and output in compiler response")
    argv[generated_args[0]] = str(variant)
    argv.insert(len(argv), str(observer))
    argv[output_args[0]] = "-out:" + str(out / "SourceSimulation.exe")
    rsp = out / "compile.rsp"
    rsp.write_text("\n".join(json.dumps(x) for x in argv) + "\n")

    # Reuse only compiler tools already present in the checked build pin set.
    dotnet = next((Path(p) for p in pins if Path(p).name == "dotnet"), None)
    csc = next((Path(p) for p in pins if Path(p).name == "csc.dll"), None)
    if dotnet is None or csc is None:
        raise SystemExit("Pinned dotnet/csc compiler tools are missing from source build")
    result = subprocess.run([str(dotnet), str(csc), "@" + str(rsp)],
                            capture_output=True, text=True, timeout=120)
    (out / "compile.log").write_text(result.stdout + result.stderr)
    if result.returncode != 0:
        raise SystemExit(f"Diagnostic compile failed ({result.returncode}); see {out / 'compile.log'}")

    variant_pin = {str(p): sha(p) for p in (variant, observer, rsp, out / "compile.log", LAYOUT)}
    build_variant = dict(receipt)
    build_variant["executable"] = {"path": str(out / "SourceSimulation.exe"),
                                   "sha256": sha(out / "SourceSimulation.exe"),
                                   "bytes": (out / "SourceSimulation.exe").stat().st_size}
    build_variant["source_and_reference_pins"] = dict(pins, **variant_pin)
    build_variant["diagnostic_variant"] = {
        "kind": "S074OriginPreUnitGridObservation",
        "scope": "Read-only bounded grid snapshot immediately before first original unit placement",
        "base_build": {"path": str(receipt_path), "sha256": sha(receipt_path)},
        "layout": {"path": str(LAYOUT), "sha256": sha(LAYOUT)},
        "radius_cells": 8,
        "placement_logic_changed": False,
        "mission_data_changed": False,
        "fit_or_readiness_claim": False,
    }
    (out / "build.json").write_text(json.dumps(build_variant, indent=2) + "\n")
    print(json.dumps({"status": "Passed", "build": str(out / "build.json"),
                      "executable": str(out / "SourceSimulation.exe"),
                      "compile_log": str(out / "compile.log"), "diagnostic_scope": build_variant["diagnostic_variant"]}, indent=2))
    return 0


def observer_source(pads: list[dict]) -> str:
    encoded = json.dumps([{"pad_id": p["PadId"], "x": p["CenterX"], "z": p["CenterZ"],
                           "width": p["WidthMetres"], "depth": p["DepthMetres"]} for p in pads], separators=(",", ":"))
    return r'''using System;using System.Collections.Generic;using Newtonsoft.Json.Linq;using Game.Components;using Game.Configs;using Unity.Entities;using Unity.Collections;using Unity.Mathematics;
namespace Game.Runtime{
internal static class S074OriginSpawnGridDiagnostic{
const int Radius=8;
static readonly string PadsJson=__PADS__;
internal static void Capture(EntityManager em,Entity gridEntity){
if(!em.Exists(gridEntity)||!em.HasComponent<GridConfig>(gridEntity)||!em.HasBuffer<GridWalkable>(gridEntity)||!em.HasComponent<DynamicBlockerComponent>(gridEntity)||!em.HasComponent<DynamicOccupancyComponent>(gridEntity))throw new InvalidOperationException("S074 origin grid diagnostic requires complete live allocator");
var g=em.GetComponentData<GridConfig>(gridEntity);var walk=em.GetBuffer<GridWalkable>(gridEntity);var blocked=em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;var occupied=em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
var pads=(JArray)JToken.Parse(PadsJson);var rows=new JArray();var reservations=new JArray();
using(var q=em.CreateEntityQuery(typeof(UnitGrid),typeof(UnitFootprint)))using(var entities=q.ToEntityArray(Allocator.Temp)){foreach(var e in entities){var cell=em.GetComponentData<UnitGrid>(e).Cell;var size=UnitFootprintUtility.ClampSize(em.GetComponentData<UnitFootprint>(e).Size);var min=UnitFootprintUtility.GetMinCell(cell,size);var near=false;foreach(var p in pads){var px=GridUtils.WorldToCell(g,new float3((float)p["x"],0,(float)p["z"]));if(math.abs(min.x-px.x)<=Radius+size.x&&math.abs(min.y-px.y)<=Radius+size.y){near=true;break;}}if(near)reservations.Add(new JObject{["entity_index"]=e.Index,["entity_version"]=e.Version,["min_cell"]=new JArray(min.x,min.y),["size_cells"]=new JArray(size.x,size.y)});}}
foreach(var p in pads){var center=GridUtils.WorldToCell(g,new float3((float)p["x"],0,(float)p["z"]));var cells=new JArray();for(int y=Math.Max(0,center.y-Radius);y<=Math.Min(g.Height-1,center.y+Radius);y++)for(int x=Math.Max(0,center.x-Radius);x<=Math.Min(g.Width-1,center.x+Radius);x++){int i=y*g.Width+x;cells.Add(new JObject{["x"]=x,["y"]=y,["walkable"]=walk[i].Value!=0,["blocked"]=blocked.IsSet(i),["occupied"]=occupied.IsSet(i)});}rows.Add(new JObject{["pad_id"]=(string)p["pad_id"],["center_cell"]=new JArray(center.x,center.y),["source_center_world"]=new JArray((float)p["x"],(float)p["z"]),["source_extent_metres"]=new JArray((float)p["width"],(float)p["depth"]),["radius_cells"]=Radius,["cells"]=cells});}
var document=new JObject{["kind"]="S074OriginPreUnitLiveGridObservation",["scope"]="Diagnostic observation only; no source-fit, spawnability or readiness determination",["tick_stage"]="Immediately before first original opening unit placement",["grid_width"]=g.Width,["grid_height"]=g.Height,["cell_size"]=g.CellSize,["grid_origin"]=new JArray(g.Origin.x,g.Origin.y,g.Origin.z),["pads"]=rows,["nearby_unit_footprint_reservations"]=reservations};
Console.WriteLine("[S074OriginPreUnitGrid] "+document.ToString(Newtonsoft.Json.Formatting.None));
}}
}'''.replace("__PADS__", json.dumps(encoded))


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env python3
"""Run an opt-in S004 opening-owner fixture with an explicitly synthetic grid."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[5]
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_prefab_capture import qualify
SETUP = ROOT / "T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/compiled-setup.json"
SETUP_RECEIPT = SETUP.parent / "receipt.json"
CAPTURE = ROOT / "Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-r1/data/prefab-reference-data.json"
CAPTURE_RECEIPT = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/receipt.json"
HELPER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004CompiledSetupInput.cs"
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S004AuthoredGridFullRosterFixture.cs"
GRID_DIR = ROOT / "T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1"
GRID_RECEIPT = GRID_DIR / "receipt.json"
GRID_REPORT = GRID_DIR / "source-projection.json"
GRID_MASK = GRID_DIR / "authored-grid-walkable-emulation.u8"
MARKER = "[S004AuthoredGridFullRosterFixture] result=Passed actualActors=54 sceneBlockers=0 sourceFit=0 mission=0 readiness=0"
OLD_GUARD = 'if(setup==null||!SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup)||typedPlacement==null)throw new NotSupportedException("Actual original launch-owned typed constraint required");SourceGroundMissionFamily.RequireCatalogSetup(setup);'


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True); parser.add_argument("--output", required=True)
    args = parser.parse_args(); output = Path(args.output).absolute(); output.mkdir(parents=True, exist_ok=False)
    build_path = Path(args.build).resolve(strict=True); build = json.loads(build_path.read_text())
    setup_receipt = json.loads(SETUP_RECEIPT.read_text()); capture_receipt = json.loads(CAPTURE_RECEIPT.read_text())
    if build.get("status") != "Passed" or build.get("compile_exit") != 0 or setup_receipt.get("compiled_setup", {}).get("sha256") != sha(SETUP): raise ValueError("Checked source build and S004 setup required")
    if qualify(ROOT, CAPTURE, Path(capture_receipt["editor_log"]["path"]), 0) != capture_receipt: raise ValueError("Checked source prefab capture required")
    pins = build["source_and_reference_pins"]
    def verify():
        for path, expected in pins.items():
            if sha(path) != expected: raise ValueError("Build pin changed: " + path)
    verify()
    grid_receipt = json.loads(GRID_RECEIPT.read_text())
    grid_report = json.loads(GRID_REPORT.read_text())
    if grid_receipt.get("status") != "Passed" or grid_receipt.get("world_initialized") is not False or grid_receipt.get("physical_source_fit_verified") is not False or grid_receipt["output"]["sha256"] != sha(GRID_REPORT) or grid_receipt["walkability_projection"]["sha256"] != sha(GRID_MASK) or grid_report["grid"]["dimensions"] != [2048, 1024] or grid_report["grid"]["authored_blocked_cell_count"] != 22840: raise ValueError("Exact current authored-grid source projection required")
    for pin in grid_receipt["inputs"]:
        if sha(pin["path"]) != pin["sha256"]: raise ValueError("Authored physical input changed")
    inputs = (build_path, SETUP, SETUP_RECEIPT, CAPTURE, CAPTURE_RECEIPT, HELPER, HARNESS, GRID_RECEIPT, GRID_REPORT, GRID_MASK)
    before = {str(path): sha(path) for path in inputs}
    response = Path(build["compiler_response"]["path"])
    if sha(response) != build["compiler_response"]["sha256"]: raise ValueError("Source response changed")
    arguments = [json.loads(line) for line in response.read_text().splitlines() if line.strip()]
    matches = [path for path in arguments if path.endswith("/typed-GeneratedTypedOpeningActorSource.cs")]
    if len(matches) != 1: raise ValueError("One pinned original typed actor projection required")
    original = Path(matches[0]); original_text = original.read_text()
    if original_text.count(OLD_GUARD) != 1: raise ValueError("Exact original profile guard required")
    guard = ('SourceS004CompiledSetupInput.RequireExact(setup,' + json.dumps(str(SETUP)) + ',' + json.dumps(sha(SETUP)) + ');'
             'if(typedPlacement!=null||forceIndex<0||forceIndex>=setup.Forces.Length||member<0||member>=force.Quantity||'
             '!Newtonsoft.Json.Linq.JToken.DeepEquals(Newtonsoft.Json.Linq.JObject.FromObject(force),Newtonsoft.Json.Linq.JObject.FromObject(setup.Forces[forceIndex]))||'
             'prefabKey!=force.RuntimePrefabKey||perMemberSupply!=force.SupplyCost/force.Quantity)throw new NotSupportedException("Exact S004 source force required");')
    derived = output / "S004OpeningActorSource.cs"; derived.write_text(original_text.replace(OLD_GUARD, guard))
    exe = output / "S004AuthoredGridFullRosterFixture.exe"
    for i, value in enumerate(arguments):
        if value == str(original): arguments[i] = str(derived)
        elif value.startswith("-main:"): arguments[i] = "-main:S004AuthoredGridFullRosterFixture"
        elif value.startswith("-out:"): arguments[i] = "-out:" + str(exe)
    arguments.extend((str(HELPER), str(HARNESS)))
    rsp = output / "compile.rsp"; rsp.write_text("\n".join(json.dumps(value) for value in arguments) + "\n")
    csc = next(Path(path) for path in pins if path.endswith("/Roslyn/bincore/csc.dll")); dotnet = shutil.which("dotnet")
    if not dotnet or not any(Path(path).name == "dotnet" and Path(path).resolve() == Path(dotnet).resolve() for path in pins): raise ValueError("Pinned dotnet required")
    compiled = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True, text=True, timeout=180)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode != 0: raise RuntimeError("S004 derived owner compile failed")
    runtime = build["runtime_selection"]; env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(Path(build["placement_build"]).parent), runtime["engine"], str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(output)))
    run = subprocess.run([runtime["mono"], str(exe), str(SETUP), sha(SETUP), str(CAPTURE), str(GRID_MASK), str(output / "actor-observations.json")], cwd=ROOT, env=env, capture_output=True, text=True, timeout=180)
    (output / "run.log").write_text(run.stdout + run.stderr)
    if run.returncode != 0 or run.stdout.splitlines().count(MARKER) != 1: raise RuntimeError("S004 opening-owner fixture failed")
    verify()
    if qualify(ROOT, CAPTURE, Path(capture_receipt["editor_log"]["path"]), 0) != capture_receipt or {str(path): sha(path) for path in inputs} != before: raise ValueError("Source evidence changed")
    result = {"status": "PassedDerivedSourceOwnerFixture", "input_sha256": before, "build_pins_rechecked": len(pins),
              "original_projection_sha256": sha(original), "derived_projection_sha256": sha(derived),
              "projection_change": "Only profile guard replaced; exact pinned S004 setup and source force required",
              "authored_grid_walkability_projection": True, "native_scene_blockers_included": False, "water_or_surface_admission": False, "source_fit_certified": False, "native_birth_authority": False,
              "created_and_grouped_actors": 54, "actor_observations_sha256": sha(output / "actor-observations.json"), "mission_world_created": False,
              "scheduler_executed": False, "runtime_admission": False, "mission_ready": False,
              "compile_log_sha256": sha(output / "compile.log"), "run_log_sha256": sha(output / "run.log")}
    (output / "receipt.json").write_text(json.dumps(result, indent=2) + "\n"); print(json.dumps(result))


if __name__ == "__main__": main()

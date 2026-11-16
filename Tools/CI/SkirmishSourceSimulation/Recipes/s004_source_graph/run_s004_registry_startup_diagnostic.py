#!/usr/bin/env python3
"""Run original prefab import over the checked four-root S004 capture."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[5]
CAPTURE_DIR = ROOT / "Logs/SkirmishProduction/s004-missing-four-current-capture-20261104-r3"
CAPTURE = CAPTURE_DIR / "data/prefab-reference-data.json"
CHECKED = ROOT / "T Logs/SkirmishProduction/s004-missing-four-current-capture-20261104-T3/receipt.json"
FINALIZER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/finalize_s004_missing_prefab_capture.py"
S3 = ROOT / "Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-r1/data/prefab-reference-data.json"
S3LOG = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/editor.log"
S3RECEIPT = S3LOG.parent / "receipt.json"
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_prefab_capture import qualify
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S004RegistryStartupDiagnostic.cs"
HELPER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004CompiledSetupInput.cs"
S004_LOADER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/GeneratedBuildingDefinitions.s004.cs"
SETUP = ROOT / "T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/compiled-setup.json"
SETUP_RECEIPT = SETUP.parent / "receipt.json"
FACTORY = ROOT / "Logs/SkirmishProduction/s004-current-full-factory-runway-20261104-r1/data/full-factory-runway-definitions.json"
PARITY = ROOT / "T Logs/SkirmishProduction/s004-current-full-factory-runway-parity-20261104-T1/receipt.json"
PARITY_CHECK = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/verify_s004_full_factory_runway_parity.py"
GRID_DIR = ROOT / "T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1"
GRID_RECEIPT, GRID_REPORT, GRID_MASK = GRID_DIR / "receipt.json", GRID_DIR / "source-projection.json", GRID_DIR / "authored-grid-walkable-emulation.u8"
BLOCK_DIR = ROOT / "Logs/SkirmishProduction/s004-static-blocker-source-20261104-r1"
BLOCK_CAPTURE = BLOCK_DIR / "data/static-blockers.json"
BLOCK_RECEIPT = ROOT / "T Logs/SkirmishProduction/s004-static-blocker-source-20261104-T1/receipt.json"
BLOCK_FINALIZER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/finalize_s004_static_blocker_capture.py"
READ_DIR = ROOT / "Logs/SkirmishProduction/s004-current-configured-readmodel-20261104-r1"
READ_RAW, READ_LOG = READ_DIR / "data/configured-spawnable-readmodel.json", READ_DIR / "editor.log"
READ_RECEIPT = ROOT / "T Logs/SkirmishProduction/s004-current-configured-readmodel-20261104-T1/capture-receipt.json"
READ_FINALIZER = ROOT / "Tools/CI/finalize_s004_configured_building_readmodel_capture.py"
SESSION_CONTROL = ROOT / "T Logs/SkirmishProduction/s004-session-initialization-control-20261104-T1/receipt.json"
SESSION_FRAGMENT = ROOT / "T Logs/SkirmishProduction/s004-session-owner-compile-20261104-T1/S004OriginalOwnerFragments.cs"

ROAD = ROOT / "T Logs/SkirmishProduction/s004-current-source-road-mask-20261104-T2/base-surface-roads.u8"
ROAD_RECEIPT = ROAD.parent / "receipt.json"
GRAPH = GRID_DIR / "source-config-graph-with-cityedge-physical-inputs.json"
MONO_CONFIG = GRID_DIR / "mono-config.xml"
PLACEMENT_CONFIG = ROOT / "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset"
MARKER = "[S004RegistryStartupDiagnostic] result=Observed "


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True); parser.add_argument("--output", required=True)
    args = parser.parse_args()
    output = Path(args.output).absolute(); output.mkdir(parents=True, exist_ok=False)
    build_path = Path(args.build).resolve(strict=True); build = json.loads(build_path.read_text())
    if build.get("kind") != "SkirmishSourceSimulationBuild" or build.get("status") != "Passed" or build.get("compile_exit") != 0:
        raise ValueError("Passed source build required")
    checked = json.loads(CHECKED.read_text())
    if checked.get("status") != "PassedScopedSourceOnlyCapture" or checked.get("wrapper_exit_code") != 0:
        raise ValueError("Checked native scoped capture required")
    setup_receipt = json.loads(SETUP_RECEIPT.read_text()); parity = json.loads(PARITY.read_text())
    if setup_receipt.get("status") != "Passed" or setup_receipt["compiled_setup"]["sha256"] != sha(SETUP) or parity.get("status") != "PassedExactSourceFactoryFieldParity" or parity["input_sha256"][str(FACTORY)] != sha(FACTORY): raise ValueError("Checked S004 setup and full factory parity required")
    grid_receipt, block_receipt, read_receipt, session_receipt = (json.loads(path.read_text()) for path in (GRID_RECEIPT, BLOCK_RECEIPT, READ_RECEIPT, SESSION_CONTROL))
    if grid_receipt.get("status") != "Passed" or grid_receipt["walkability_projection"]["sha256"] != sha(GRID_MASK) or grid_receipt["output"]["sha256"] != sha(GRID_REPORT): raise ValueError("Checked authored grid required")
    if block_receipt.get("status") != "PassedIsolatedBakedBlockerCapture" or block_receipt.get("capture_sha256") != sha(BLOCK_CAPTURE) or block_receipt.get("blocker_count") != 348: raise ValueError("Checked physical-only blockers required")
    if read_receipt.get("schema") != "s004-configured-spawnable-readmodel-receipt-v1" or read_receipt.get("rawExportSha256") != sha(READ_RAW): raise ValueError("Checked configured readmodel required")
    if session_receipt.get("status") != "PassedOriginalSessionOwnerFixture" or session_receipt["input_sha256"].get(str(SESSION_FRAGMENT)) != sha(SESSION_FRAGMENT): raise ValueError("Checked original session initializer required")
    road_receipt = json.loads(ROAD_RECEIPT.read_text())
    if road_receipt.get("status") != "PassedSourceOnlyBaseRoadProjection" or road_receipt.get("road_mask_sha256") != sha(ROAD) or road_receipt.get("road_cells") != 23500 or road_receipt["input_sha256"].get(str(GRAPH)) != sha(GRAPH) or road_receipt["input_sha256"].get(str(MONO_CONFIG)) != sha(MONO_CONFIG): raise ValueError("Checked source road projection required")
    import re
    config = PLACEMENT_CONFIG.read_text()
    if re.findall(r"^  buildPlaneY: ([^\r\n]+)$", config, re.M) != ["0"]: raise ValueError("Actual placement plane changed")
    grid_inputs = tuple(Path(row["path"]) for row in grid_receipt["inputs"])
    for row in grid_receipt["inputs"]:
        if sha(row["path"]) != row["sha256"]: raise ValueError("Authored grid dependency changed")
    pinned = (build_path, CAPTURE, CHECKED, FINALIZER, HARNESS, S3, S3LOG, S3RECEIPT, HELPER, S004_LOADER, SETUP, SETUP_RECEIPT, FACTORY, PARITY, PARITY_CHECK, GRID_RECEIPT, GRID_REPORT, GRID_MASK, BLOCK_CAPTURE, BLOCK_RECEIPT, BLOCK_FINALIZER, READ_RAW, READ_LOG, READ_RECEIPT, READ_FINALIZER, SESSION_CONTROL, SESSION_FRAGMENT, ROAD, ROAD_RECEIPT, GRAPH, MONO_CONFIG, PLACEMENT_CONFIG, *grid_inputs)
    input_sha = {str(path): sha(path) for path in pinned}
    pins = build["source_and_reference_pins"]
    def verify():
        for path, value in pins.items():
            if sha(path) != value: raise ValueError("Source build pin changed: " + path)
    def capture_check(name):
        result = subprocess.run(["python3", str(FINALIZER), "--directory", str(CAPTURE_DIR), "--wrapper-exit", "0", "--output", str(output / name)], cwd=ROOT, text=True, capture_output=True, timeout=180)
        (output / (name + ".log")).write_text(result.stdout + result.stderr)
        if result.returncode != 0 or json.loads((output / name).read_text()) != checked:
            raise ValueError("Capture receipt or current source pins changed")
    def parity_check(name):
        result = subprocess.run(["python3", str(PARITY_CHECK), "--wrapper-exit", "0", "--output", str(output / name)], cwd=ROOT, capture_output=True, text=True, timeout=180)
        (output / (name + ".log")).write_text(result.stdout + result.stderr)
        if result.returncode != 0 or json.loads((output / name).read_text()) != parity: raise ValueError("Full factory parity evidence changed")
    def blocker_check(name):
        result = subprocess.run(["python3", str(BLOCK_FINALIZER), "--directory", str(BLOCK_DIR), "--wrapper-exit", "0", "--output", str(output / name)], cwd=ROOT, capture_output=True, text=True, timeout=180)
        (output / (name + ".log")).write_text(result.stdout + result.stderr)
        if result.returncode != 0 or json.loads((output / name).read_text()) != block_receipt: raise ValueError("Blocker capture evidence changed")
    spec = importlib.util.spec_from_file_location("s004_readmodel_finalizer", READ_FINALIZER)
    read_finalizer = importlib.util.module_from_spec(spec); spec.loader.exec_module(read_finalizer)
    def readmodel_check():
        if read_finalizer.verify(READ_LOG, READ_RAW, SETUP, ROOT, 0) != read_receipt: raise ValueError("Configured readmodel evidence changed")
    verify(); capture_check("capture-before.json"); parity_check("parity-before.json"); blocker_check("blockers-before.json"); readmodel_check()
    if qualify(ROOT, S3, S3LOG, 0) != json.loads(S3RECEIPT.read_text()): raise ValueError("Current S003 source capture required")
    csc = next(Path(path) for path in pins if path.endswith("/Roslyn/bincore/csc.dll"))
    dotnet = shutil.which("dotnet")
    if not dotnet or not any(Path(path).name == "dotnet" and Path(path).resolve() == Path(dotnet).resolve() for path in pins): raise ValueError("Pinned dotnet required")
    response = Path(build["compiler_response"]["path"])
    if sha(response) != build["compiler_response"]["sha256"]: raise ValueError("Compiler response changed")
    arguments = [json.loads(line) for line in response.read_text().splitlines() if line.strip()]
    matches = [path for path in arguments if path.endswith("/typed-GeneratedTypedOpeningActorSource.cs")]
    if len(matches) != 1: raise ValueError("One original typed actor projection required")
    original = Path(matches[0]); original_text = original.read_text()
    old_guard = 'if(setup==null||!SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup)||typedPlacement==null)throw new NotSupportedException("Actual original launch-owned typed constraint required");SourceGroundMissionFamily.RequireCatalogSetup(setup);'
    if original_text.count(old_guard) != 1: raise ValueError("Exact original profile guard required")
    guard = ('SourceS004CompiledSetupInput.RequireExact(setup,' + json.dumps(str(SETUP)) + ',' + json.dumps(sha(SETUP)) + ');'
             'if(typedPlacement!=null||forceIndex<0||forceIndex>=setup.Forces.Length||member<0||member>=force.Quantity||'
             '!Newtonsoft.Json.Linq.JToken.DeepEquals(Newtonsoft.Json.Linq.JObject.FromObject(force),Newtonsoft.Json.Linq.JObject.FromObject(setup.Forces[forceIndex]))||'
             'prefabKey!=force.RuntimePrefabKey||perMemberSupply!=force.SupplyCost/force.Quantity)throw new NotSupportedException("Exact S004 source force required");')
    derived = output / "S004OpeningActorSource.cs"; derived.write_text(original_text.replace(old_guard, guard))
    executable = output / "S004RegistryStartupDiagnostic.exe"
    for i, value in enumerate(arguments):
        if value == str(original): arguments[i] = str(derived)
        elif value.startswith("-main:"): arguments[i] = "-main:S004RegistryStartupDiagnostic"
        elif value.startswith("-out:"): arguments[i] = "-out:" + str(executable)
    owner = ROOT / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishScenarioSpawnSystem.cs"
    if pins.get(str(owner)) != sha(owner): raise ValueError("Original ledger owner pin required")
    owner_text = owner.read_text()
    start_marker = "            visualPending = (byte)(startingSetBound ? 0 : 1);"
    end_marker = "            if (Application.isPlaying && !SkirmishStartingSupplyService.Initialize(em, session, setup))"
    if owner_text.count(start_marker) != 1 or owner_text.count(end_marker) != 1: raise ValueError("Original ledger source seam changed")
    tail = owner_text[owner_text.index(start_marker):owner_text.index(end_marker)]
    preamble = """using Game.Components; using Game.Configs; using Game.Runtime; using Game.Skirmish.Contracts; using Unity.Entities;
internal static class S004OriginalLedgerTail {
 internal static void Apply(EntityManager em, Entity session, SkirmishResolvedSetup setup, Game.Simulation.SkirmishMissionBootstrapKernel opening, out byte visualPending) {
 bool startingSetBound=opening.StartingSetBound;
 int infantryLive=opening.Player.Infantry, groundLive=opening.Player.Ground, airLive=opening.Player.Air, supplyLive=opening.Player.Supply;
 int enemyInfantryLive=opening.Enemy.Infantry, enemyGroundLive=opening.Enemy.Ground, enemyAirLive=opening.Enemy.Air, enemySupplyLive=opening.Enemy.Supply;
"""
    ledger = output / "S004OriginalLedgerTail.cs"; ledger.write_text(preamble + tail + "} }\n")
    probe_path = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/probe_s004_spawn_owners_compile.py"
    spec = importlib.util.spec_from_file_location("s004_owner_extract", probe_path); probe = importlib.util.module_from_spec(spec); spec.loader.exec_module(probe)
    lookup_method = probe.extract_method(owner_text, "BuildPrefabEntityLookup")
    lookup = output / "S004OriginalPrefabLookup.cs"
    lookup.write_text("using System.Collections.Generic; using Game.Components; using Unity.Collections; using Unity.Entities; namespace Game.Runtime { internal static class S004OriginalPrefabLookup {\n" + lookup_method + "\n} }\n")
    arguments.extend(str(path) for path in (HARNESS, HELPER, S004_LOADER, SESSION_FRAGMENT, ledger, lookup))
    rsp = output / "compile.rsp"; rsp.write_text("\n".join(json.dumps(value) for value in arguments) + "\n")
    compiled = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True, text=True, timeout=180)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode != 0: raise RuntimeError("Control compile failed")
    runtime = build["runtime_selection"]; env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(Path(build["placement_build"]).parent), runtime["engine"], str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(output)))
    run = subprocess.run([runtime["mono"], "--config", str(MONO_CONFIG), str(executable), str(SETUP), sha(SETUP), str(S3), str(GRID_MASK), str(output / "requests.json"), str(BLOCK_CAPTURE), str(CAPTURE), str(FACTORY), sha(FACTORY), str(READ_RAW), str(ROAD), str(GRAPH)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=180)
    (output / "run.log").write_text(run.stdout + run.stderr)
    if run.returncode != 0 or sum(line.startswith(MARKER) and line.endswith(" readiness=0") for line in run.stdout.splitlines()) != 1: raise RuntimeError("Import control failed")
    capture_check("capture-after.json"); parity_check("parity-after.json"); blocker_check("blockers-after.json"); readmodel_check(); verify()
    if qualify(ROOT, S3, S3LOG, 0) != json.loads(S3RECEIPT.read_text()): raise ValueError("S003 capture drift")
    if {str(path): sha(path) for path in pinned} != input_sha: raise ValueError("Control inputs changed")
    observed = json.loads((output / "requests.json").read_text())
    if observed.get("request_count") != 20 or sum(observed[k] for k in ("succeeded", "failed", "pending")) != 20 or observed.get("mission_ready") is not False: raise ValueError("Invalid observed request results")
    receipt = {"status": "ObservedDiagnosticSourcePlacement", "input_sha256": input_sha, "build_pins_rechecked": len(pins),
               "result": observed, "original_prefab_lookup_sha256": sha(lookup), "source_registry_is_native_capture": False, "original_ledger_owner_sha256": sha(owner), "extracted_ledger_tail_sha256": sha(ledger), "ledger_scope": "Original capacity and enemy stock/strategy tail; supply owner invoked separately, no phase transition", "original_actor_projection_sha256": sha(original), "derived_actor_projection_sha256": sha(derived), "actor_projection_change": "Only profile guard replaced with exact pinned S004 setup and force checks", "requests_sha256": sha(output / "requests.json"), "physical_source_validated": False,
               "runtime_admission": False, "native_visual_readiness": False, "mission_ready": False,
               "compile_log_sha256": sha(output / "compile.log"), "run_log_sha256": sha(output / "run.log")}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n"); print(json.dumps(receipt))


if __name__ == "__main__": main()

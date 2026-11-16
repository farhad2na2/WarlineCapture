#!/usr/bin/env python3
"""Compile and run the original S004 operation-map/surface owners in the S073 host."""
import hashlib
import json
import os
import shutil
import subprocess
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
BUILD = ROOT / "Logs/SkirmishProduction/s002-derived-two-epoch-build-20261103-r1"
R13 = ROOT / "T Logs/SkirmishProduction/s004-cityedge-physical-input-source-projection-20261103-r13"
OUT = ROOT / "T Logs/SkirmishProduction/s004-source-map-publication-diagnostic-20261103-r1/fixture-attempt-4"
FIXTURE = OUT.parent / "S004MapOwnerFixtureProgram.cs"
SHIMS = OUT.parent / "S004MapSurfaceOwnerCompileShims.cs"
SURFACE_OWNER = ROOT / "Assets/Game/Scripts/Composition/MapSurfaceRuntimeBootstrapSceneSystemHelper.cs"
ECS_HOST = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/059_AutonomousEcs.cs"
MATERIALIZER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/051_SourceConfigMaterializer.cs"
DECLARED_MATERIALIZER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/051_SourceConfigMaterializer.cs"
SESSION_BINDING = ROOT / "Logs/SkirmishProduction/s004-session-initialization-owner-diagnostic-20261103-T/S004NullRegistryLaunchDiagnosticBinding.cs"
MAP_OWNER = ROOT / "Assets/Game/Scripts/Composition/OperationMapRuntimeBootstrapSceneSystemHelper.cs"
GENERATED_MAP_OWNER = BUILD / "source/generated/SourceOperationMapRuntimeBootstrapSceneSystemHelper.cs"

def sha(path):
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

def require(ok, message):
    if not ok:
        raise RuntimeError(message)

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    build = json.loads((BUILD / "build.json").read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
            build.get("compile_exit") == 0, "Passed S002 generated source-host build required")
    sources = dict(build["source_and_reference_pins"])
    inputs = [BUILD / "build.json", BUILD / "compile.rsp", FIXTURE, SHIMS, SURFACE_OWNER, ECS_HOST, DECLARED_MATERIALIZER,
              SESSION_BINDING, MAP_OWNER, GENERATED_MAP_OWNER,
              R13 / "source-config-graph-with-cityedge-physical-inputs.json",
              R13 / "source-projection.json"]
    extra_pins = {str(path.resolve(strict=True)): sha(path) for path in inputs}
    pins = dict(sources)
    pins.update(extra_pins)
    for path, expected in pins.items():
        require(Path(path).is_file() and sha(Path(path)) == expected, "Input pin mismatch before compile: " + path)

    args = [json.loads(line) for line in (BUILD / "compile.rsp").read_text().splitlines()]
    assembly = OUT / "S004MapOwnerFixture.exe"
    host_shim = OUT / "AutonomousEcs.S004Fixture.cs"
    host_text = ECS_HOST.read_text()
    world_decl = "public sealed class World{public bool IsCreated{get;internal set;}=true;}"
    em_world = "public World World{get;}=new World();"
    require(host_text.count(world_decl) == 1 and host_text.count(em_world) == 1,
            "Exact source-host World/EntityManager declarations required for narrow Unity API bridge")
    host_text = host_text.replace(
        world_decl,
        "public sealed class World{public bool IsCreated{get;internal set;}=true; public EntityManager EntityManager{get;internal set;}}")
    host_text = host_text.replace(em_world, em_world + " public EntityManager(){World.EntityManager=this;}")
    host_shim.write_text(host_text)
    original_ecs = str(ECS_HOST.resolve(strict=True))
    replacement_ecs = str(host_shim.resolve())
    original_materializer = str(MATERIALIZER.resolve(strict=True))
    replacement_materializer = str(DECLARED_MATERIALIZER.resolve(strict=True))
    args = [
        "-out:" + str(assembly) if item.startswith("-out:") else
        "-main:S004MapOwnerFixtureProgram" if item.startswith("-main:") else item
        for item in args
    ]
    args = [replacement_ecs if item == original_ecs else
            replacement_materializer if item == original_materializer else item for item in args]
    args.extend([str(SURFACE_OWNER), str(SHIMS), str(FIXTURE)])
    response = OUT / "compile.rsp"
    response.write_text("\n".join(json.dumps(item) for item in args) + "\n")

    dotnet = Path(shutil.which("dotnet") or "").resolve(strict=True)
    compilers = sorted((dotnet.parent / "sdk").glob("*/Roslyn/bincore/csc.dll"))
    require(bool(compilers), "Roslyn csc not found beside dotnet")
    compiler = compilers[-1]
    start = time.monotonic()
    compile_run = subprocess.run([str(dotnet), str(compiler), "@" + str(response)],
                                 capture_output=True, text=True, timeout=120)
    compile_seconds = time.monotonic() - start
    compile_log = OUT / "compile.log"
    compile_log.write_text(compile_run.stdout + compile_run.stderr)
    for path, expected in pins.items():
        require(sha(Path(path)) == expected, "Input pin changed during compile: " + path)

    run = None
    mono_config_path = None
    if compile_run.returncode == 0:
        runtime_paths = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
        runtime = str(runtime_paths["mono"])
        require(Path(runtime).is_file(), "Pinned Mono runtime is unavailable")
        helper = runtime_paths["contents"] / "Frameworks/libMonoPosixHelper.dylib"
        mono_native = runtime_paths["mono"].parent.parent / "lib/libmono-native.dylib"
        require(helper.is_file() and mono_native.is_file(), "Pinned Mono native helpers are unavailable")
        dllmaps = '<dllmap dll="MonoPosixHelper" target="' + str(helper) + '" os="!windows"/>'
        dllmaps += ''.join('<dllmap dll="' + name + '" target="' + str(mono_native) + '" os="!windows"/>'
                           for name in ("System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"))
        mono_config = OUT / "mono-config.xml"
        mono_config.write_text("<configuration>" + dllmaps + "</configuration>\n")
        mono_config_path = str(mono_config)
        reference_dirs = sorted({str(Path(item[3:]).resolve().parent) for item in args
                                 if item.startswith("-r:") and Path(item[3:]).is_file()})
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join([
            str(ROOT / "Library/ScriptAssemblies"), str(runtime_paths["engine"]),
            str(runtime_paths["newtonsoft"].parent), str(Path(build["placement_build"]).parent), *reference_dirs
        ])
        run = subprocess.run([
            runtime, "--config", str(mono_config), "--optimize=-float32", str(assembly),
            str(R13 / "source-config-graph-with-cityedge-physical-inputs.json"),
            str(ROOT), str(OUT / "fixture-result.json")
        ], capture_output=True, text=True, env=env, timeout=60)
        (OUT / "run.log").write_text(run.stdout + run.stderr)

    fixture_result_path = OUT / "fixture-result.json"
    fixture_result = json.loads(fixture_result_path.read_text()) if fixture_result_path.is_file() else None
    result_status = fixture_result.get("status") if fixture_result else None
    receipt_status = ("CompileFailed" if compile_run.returncode else
                      result_status if run and result_status else
                      "FixtureExecutionFailed" if run else "CompilePassedNotRun")
    projection = json.loads((R13 / "source-projection.json").read_text())
    graph_path = R13 / "source-config-graph-with-cityedge-physical-inputs.json"
    projection_path = R13 / "source-projection.json"
    receipt = {
        "kind": "S004OriginalMapOwnerSourceHostFixtureReceipt",
        "status": receipt_status,
        "scope": "Original map helper source in fresh pinned source host; original surface helper invoked using same isolated EntityManager. The World.EntityManager bridge is the minimal Unity API contract absent from the host shim. No loaded scene, Baker, occupancy, readiness, or fit claim.",
        "base_build": str(BUILD / "build.json"), "base_build_sha256": sha(BUILD / "build.json"),
        "base_build_source_pin_count": len(sources),
        "base_build_source_pins_before_after": "passed",
        "s004_graph": str(R13 / "source-config-graph-with-cityedge-physical-inputs.json"),
        "s004_graph_sha256": sha(graph_path),
        "s004_projection": str(R13 / "source-projection.json"),
        "s004_projection_sha256": sha(projection_path),
        "pinned_assets": {path: digest for path, digest in [
            (projection["source_map"]["path"], projection["source_map"]["sha256"]),
            (projection["grid"]["path"], projection["grid"]["sha256"]),
            (projection["surface"]["path"], projection["surface"]["sha256"])]},
        "original_operation_map_owner": str(MAP_OWNER),
        "original_operation_map_owner_sha256": sha(MAP_OWNER),
        "generated_operation_map_owner": str(GENERATED_MAP_OWNER),
        "generated_operation_map_owner_sha256": sha(GENERATED_MAP_OWNER),
        "queued_fixture_allocator_reference": str(SESSION_BINDING),
        "queued_fixture_allocator_reference_sha256": sha(SESSION_BINDING),
        "rotation_reference": None,
        "rotation_reference_status": "Missing S004 source-qualified reference; S073 capture deliberately not used",
        "original_surface_helper": str(SURFACE_OWNER), "original_surface_helper_sha256": sha(SURFACE_OWNER),
        "compile_exit": compile_run.returncode, "compile_seconds": compile_seconds,
        "compile_log": str(compile_log), "compile_log_sha256": sha(compile_log),
        "runtime_invoked": run is not None,
        "runtime_exit": run.returncode if run else None,
        "runtime_log": str(OUT / "run.log") if run else None,
        "runtime_log_sha256": sha(OUT / "run.log") if run else None,
        "mono_config": mono_config_path,
        "mono_config_sha256": sha(Path(mono_config_path)) if mono_config_path else None,
        "fixture_result": str(fixture_result_path) if fixture_result_path.exists() else None,
        "fixture_result_sha256": sha(fixture_result_path) if fixture_result_path.exists() else None,
        "unity_or_baker_run": False, "native_scene_loaded": False,
        "native_grid_readback": False, "occupancy_written": False,
        "physical_fit_verified": False, "readiness_claimed": False,
        "source_pins_before_after": "passed"
    }
    receipt_path = OUT / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n")
    print("[S004OriginalMapOwnerSourceHostFixture] " +
          f"status={receipt['status']} compile_exit={compile_run.returncode} " +
          f"runtime_exit={receipt['runtime_exit']} native_scene=false grid_readback=false occupancy=false fit=false")
    return 0 if receipt["status"] in ("SurfaceOwnerPassedMapOwnerGated", "BlockedMissingS004RotationReference") else 1

if __name__ == "__main__":
    raise SystemExit(main())

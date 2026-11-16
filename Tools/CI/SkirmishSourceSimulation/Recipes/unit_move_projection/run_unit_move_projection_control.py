#!/usr/bin/env python3
import hashlib
import argparse
import json
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[5]
CONFIG = ROOT / "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset"
CONFIG_META = pathlib.Path(str(CONFIG) + ".meta")
PREFAB = ROOT / "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab"
AUTHORING = ROOT / "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs"
COMPONENT = ROOT / "Assets/Game/Scripts/Components/UnitMoveAuthoringProjection.cs"
COMPONENT_META = pathlib.Path(str(COMPONENT) + ".meta")
SURFACE_COMPONENT = ROOT / "Assets/Game/Scripts/Components/UnitSurfaceAuthoringProjection.cs"
SURFACE_CONTROL = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/UnitSurfaceAuthoringProjectionControl.cs"
GRID_COMPONENTS = ROOT / "Assets/Game/Scripts/Components/GridComponents.cs"
GRID_COORDINATE_KERNEL = ROOT / "Assets/Game/Scripts/Components/GridCoordinateKernel.cs"
UNITY_ENTITIES = ROOT / "Library/ScriptAssemblies/Unity.Entities.dll"
UNITY_COLLECTIONS = ROOT / "Library/ScriptAssemblies/Unity.Collections.dll"
CONTROL = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/UnitMoveAuthoringProjectionControl.cs"
MANIFESTS = [ROOT / "Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json",
             ROOT / "Tools/CI/SkirmishSourceSimulation/source_manifest.s003.plan.json"]


def need(value, message):
    if not value:
        raise RuntimeError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def authored_float(text, field):
    hits = re.findall(rf"(?m)^\s*{re.escape(field)}:\s*([-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?)\s*$", text)
    need(len(hits) == 1, f"expected exactly one serialized {field}")
    return float(hits[0])


def authored_bool(text, field):
    hits = re.findall(rf"(?m)^\s*{re.escape(field)}:\s*([01])\s*$", text)
    need(len(hits) == 1, f"expected exactly one serialized {field}")
    return hits[0] == "1"


def authored_int(text, parent, child):
    hits = re.findall(rf"(?m)^\s*{re.escape(parent)}:\s*\{{[^\r\n]*?\b{re.escape(child)}:\s*(-?\d+)[^\r\n]*\}}\s*$", text)
    need(len(hits) == 1, f"expected exactly one serialized {parent}.{child}")
    return int(hits[0])


def main():
    parser = argparse.ArgumentParser(description="Compile and run isolated unit authoring projection controls.")
    parser.add_argument("--output", required=True, type=pathlib.Path,
                        help="fresh evidence directory; the path must not already exist")
    args = parser.parse_args()
    out = args.output.resolve()
    need(not out.exists(), f"output path already exists; choose a fresh directory: {out}")
    out.mkdir(parents=True)
    config_text = CONFIG.read_text()
    meta_text = CONFIG_META.read_text()
    prefab_text = PREFAB.read_text()
    authoring_text = AUTHORING.read_text()
    config_guid = re.search(r"(?m)^guid: ([0-9a-f]{32})$", meta_text)
    need(config_guid, "config GUID missing")
    need(config_guid.group(1) in prefab_text, "rifle prefab does not reference this exact config GUID")
    need("speed = config.Speed;" in authoring_text and "walkSpeed = config.WalkSpeed;" in authoring_text and
         "roadSpeedMultiplier = config.RoadSpeedMultiplier;" in authoring_text and "arriveDistance = config.ArriveDistance;" in authoring_text,
         "ApplyConfigIfAvailable no longer copies all four fields directly")
    need("UnitMoveAuthoringProjection.FromAuthoredMovement(" in authoring_text,
         "UnitGridAuthoring baker is not using the shared movement projection")
    need("UnitSurfaceAuthoringProjection.FromAuthoredHeight(\n                    authoring.transform.position.y)" in authoring_text,
         "UnitGridAuthoring baker must pass its evaluated authored Y to the shared surface projection")
    values = [authored_float(config_text, k) for k in ("speed", "walkSpeed", "roadSpeedMultiplier", "arriveDistance")]
    movement_values = [authored_int(config_text, "footprintCells", "x"), authored_int(config_text, "footprintCells", "y"),
                       authored_bool(config_text, "autoCalculateFootprint"), authored_bool(config_text, "usesVehicleMotion"),
                       authored_bool(config_text, "allowIdleWander")]
    for path in MANIFESTS:
        manifest = json.loads(path.read_text())
        need(any(row.get("path") == str(COMPONENT.relative_to(ROOT)) for row in manifest["sources"]),
             f"shared helper absent from source manifest {path.name}")
        need(any(row.get("path") == str(SURFACE_COMPONENT.relative_to(ROOT)) for row in manifest["sources"]),
             f"surface helper absent from source manifest {path.name}")

    dotnet = pathlib.Path(subprocess.check_output(["which", "dotnet"], text=True).strip()).resolve()
    version_text = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text()
    editor_version = re.search(r"(?m)^m_EditorVersion: ([0-9.]+f[0-9]+)$", version_text)
    need(editor_version is not None, "exact project Editor version missing")
    unity_root = pathlib.Path("/Applications/Unity/Hub/Editor") / editor_version.group(1) / "Unity.app/Contents"
    math_candidates = sorted((unity_root / "Resources/PackageManager/ProjectTemplates/libcache").glob(
        "com.unity.template.3d-cross-platform-*/ScriptAssemblies/Unity.Mathematics.dll"))
    need(len(math_candidates) == 1, "one installed Unity Mathematics assembly required")
    mathematics = math_candidates[0].resolve(strict=True)
    managed = unity_root / "Resources/Scripting/Managed/UnityEngine"
    math_module = (managed / "UnityEngine.MathematicsModule.dll").resolve(strict=True)
    core_module = (managed / "UnityEngine.CoreModule.dll").resolve(strict=True)
    sdk = sorted((dotnet.parent / "sdk").glob("*/Roslyn/bincore/csc.dll"))
    need(bool(sdk), "installed Roslyn csc.dll missing")
    project = out / "control.csproj"
    for path in (GRID_COMPONENTS, GRID_COORDINATE_KERNEL, UNITY_ENTITIES, UNITY_COLLECTIONS):
        need(path.is_file(), f"Unity component reference missing: {path}")
    project.write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources></PropertyGroup><ItemGroup><Compile Include="{GRID_COMPONENTS}" Link="GridComponents.cs"/><Compile Include="{GRID_COORDINATE_KERNEL}" Link="GridCoordinateKernel.cs"/><Compile Include="{COMPONENT}" Link="UnitMoveAuthoringProjection.cs"/><Compile Include="{SURFACE_COMPONENT}" Link="UnitSurfaceAuthoringProjection.cs"/><Compile Include="{SURFACE_CONTROL}" Link="UnitSurfaceAuthoringProjectionControl.cs"/><Compile Include="{CONTROL}" Link="UnitMoveAuthoringProjectionControl.cs"/><Reference Include="Unity.Entities"><HintPath>{UNITY_ENTITIES}</HintPath><Private>true</Private></Reference><Reference Include="Unity.Collections"><HintPath>{UNITY_COLLECTIONS}</HintPath><Private>true</Private></Reference><Reference Include="Unity.Mathematics"><HintPath>{mathematics}</HintPath><Private>true</Private></Reference><Reference Include="UnityEngine.MathematicsModule"><HintPath>{math_module}</HintPath><Private>true</Private></Reference><Reference Include="UnityEngine.CoreModule"><HintPath>{core_module}</HintPath><Private>true</Private></Reference></ItemGroup></Project>\n''')
    build = subprocess.run([str(dotnet), "build", str(project), "--configuration", "Release", "--nologo",
                            "-p:NuGetAudit=false", "--ignore-failed-sources"],
                           cwd=ROOT, capture_output=True, text=True)
    (out / "compile.log").write_text(build.stdout + build.stderr)
    need(build.returncode == 0, "compiled control build failed; inspect compile.log")
    exe = out / "bin/Release/net8.0/control.dll"
    run_args = [*([format(v, ".17g") for v in values]), str(movement_values[0]), str(movement_values[1]),
                str(movement_values[2]).lower(), str(movement_values[3]).lower(), str(movement_values[4]).lower()]
    run = subprocess.run([str(dotnet), str(exe), *run_args],
                         cwd=ROOT, capture_output=True, text=True)
    (out / "run.log").write_text(run.stdout + run.stderr)
    need(run.returncode == 0 and "[UnitMoveAndSurfaceAuthoringProjectionControl] result=Passed" in run.stdout,
         "compiled control failed; inspect run.log")

    pins = [CONFIG, CONFIG_META, PREFAB, AUTHORING, GRID_COMPONENTS, GRID_COORDINATE_KERNEL, COMPONENT, COMPONENT_META, SURFACE_COMPONENT, SURFACE_CONTROL, CONTROL, UNITY_ENTITIES, UNITY_COLLECTIONS, *MANIFESTS]
    receipt = {
        "kind": "UnitMoveAndSurfaceAuthoringProjectionSourceControl",
        "status": "PassedCompiledSourceControl",
        "scope": "Compiled data-only movement projection controls plus UnitSurfaceAuthoringProjection values; no prefab import, transform-Y derivation, baking, spawn, mission admission, or Unity runtime claim",
        "rifle": {"key": "Unit_Chr_Soldier_Male_02_Alt_04", "config_guid": config_guid.group(1),
                  "speed": values[0], "walk_speed": values[1], "road_speed_multiplier": values[2], "arrive_distance": values[3]},
        "controls": ["authored rifle values", "five movement components projected from exact config inputs",
                      "negative speed/walk clamp", "subminimum speed preserves raw speed", "walk speed capped to speed",
                      "zero walk minimum", "road and arrive pass-through", "surface defaults, exact input-Y passthrough, normal, and flags"],
        "source_pins": {str(p.relative_to(ROOT)): sha(p) for p in pins},
        "compile_log_sha256": sha(out / "compile.log"), "run_log_sha256": sha(out / "run.log"),
        "compiler": str(sdk[-1]), "dotnet": str(dotnet),
        "unity_editor_version": editor_version.group(1),
        "unity_mathematics_assembly": {"path": str(mathematics), "sha256": sha(mathematics)},
        "unity_mathematics_module": {"path": str(math_module), "sha256": sha(math_module)},
        "unity_core_module": {"path": str(core_module), "sha256": sha(core_module)},
    }
    (out / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print("[UnitMoveAndSurfaceAuthoringProjectionControl] result=Passed")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"[UnitMoveAndSurfaceAuthoringProjectionControl] result=Failed {exc}", file=sys.stderr)
        raise

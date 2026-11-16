#!/usr/bin/env python3
"""Compile the shared UnitMove helper against Unity's installed Mathematics assembly."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[5]
OUT = ROOT / "T Logs/SkirmishProduction/unit-move-unity-math-control-20261103-T7"
HELPER = ROOT / "Assets/Game/Scripts/Components/UnitMoveAuthoringProjection.cs"
CONTROL = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/UnitMoveAuthoringProjectionUnityMathControl.cs"
PROJECT_VERSION = ROOT / "ProjectSettings/ProjectVersion.txt"
PACKAGE_LOCK = ROOT / "Packages/packages-lock.json"
GRID_COMPONENTS = ROOT / "Assets/Game/Scripts/Components/GridComponents.cs"
GRID_COORDINATE_KERNEL = ROOT / "Assets/Game/Scripts/Components/GridCoordinateKernel.cs"
UNITY_ENTITIES = ROOT / "Library/ScriptAssemblies/Unity.Entities.dll"
UNITY_COLLECTIONS = ROOT / "Library/ScriptAssemblies/Unity.Collections.dll"


def need(ok, reason):
    if not ok:
        raise RuntimeError(reason)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    need(not OUT.exists(), "fresh exclusive output directory required")
    version = re.search(r"(?m)^m_EditorVersion: ([0-9.]+f[0-9]+)$", PROJECT_VERSION.read_text())
    need(version is not None, "exact project Editor version missing")
    template_root = Path("/Applications/Unity/Hub/Editor") / version.group(1) / "Unity.app/Contents/Resources/PackageManager/ProjectTemplates/libcache"
    candidates = sorted(template_root.glob("com.unity.template.3d-cross-platform-*/ScriptAssemblies/Unity.Mathematics.dll"))
    need(len(candidates) == 1 and candidates[0].is_file(), "one installed Unity Mathematics assembly required")
    mathematics = candidates[0].resolve(strict=True)
    managed_unity = Path("/Applications/Unity/Hub/Editor") / version.group(1) / "Unity.app/Contents/Resources/Scripting/Managed/UnityEngine"
    mathematics_module = (managed_unity / "UnityEngine.MathematicsModule.dll").resolve(strict=True)
    core_module = (managed_unity / "UnityEngine.CoreModule.dll").resolve(strict=True)
    OUT.mkdir(parents=True, exist_ok=False)
    project = OUT / "control.csproj"
    for path in (GRID_COMPONENTS, GRID_COORDINATE_KERNEL, UNITY_ENTITIES, UNITY_COLLECTIONS):
        need(path.is_file(), "Unity component reference missing: " + str(path))
    project.write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources></PropertyGroup><ItemGroup><Compile Include="{GRID_COMPONENTS}" Link="GridComponents.cs"/><Compile Include="{GRID_COORDINATE_KERNEL}" Link="GridCoordinateKernel.cs"/><Compile Include="{HELPER}" Link="UnitMoveAuthoringProjection.cs"/><Compile Include="{CONTROL}" Link="UnitMoveAuthoringProjectionUnityMathControl.cs"/><Reference Include="Unity.Entities"><HintPath>{UNITY_ENTITIES}</HintPath><Private>true</Private></Reference><Reference Include="Unity.Collections"><HintPath>{UNITY_COLLECTIONS}</HintPath><Private>true</Private></Reference><Reference Include="Unity.Mathematics"><HintPath>{mathematics}</HintPath><Private>true</Private></Reference><Reference Include="UnityEngine.MathematicsModule"><HintPath>{mathematics_module}</HintPath><Private>true</Private></Reference><Reference Include="UnityEngine.CoreModule"><HintPath>{core_module}</HintPath><Private>true</Private></Reference></ItemGroup></Project>\n''', encoding="utf-8")
    dotnet = Path(subprocess.check_output(["which", "dotnet"], text=True).strip()).resolve()
    build = subprocess.run([str(dotnet), "build", str(project), "--configuration", "Release", "--nologo",
                            "-p:NuGetAudit=false", "--ignore-failed-sources"],
                           cwd=ROOT, capture_output=True, text=True)
    (OUT / "compile.log").write_text(build.stdout + build.stderr, encoding="utf-8")
    need(build.returncode == 0, "compiled Unity Mathematics comparison failed; inspect compile.log")
    config = ROOT / "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset"
    config_text = config.read_text(encoding="utf-8")
    values = []
    for field in ("speed", "walkSpeed", "roadSpeedMultiplier", "arriveDistance"):
        found = re.findall(rf"(?m)^\s*{field}: ([-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?)$", config_text)
        need(len(found) == 1, "exact serialized rifle value required: " + field)
        values.append(found[0])
    run = subprocess.run([str(dotnet), str(OUT / "bin/Release/net8.0/control.dll"), *values],
                         cwd=ROOT, capture_output=True, text=True)
    (OUT / "run.log").write_text(run.stdout + run.stderr, encoding="utf-8")
    need(run.returncode == 0 and "[UnitMoveAuthoringProjectionUnityMathControl] result=Passed pairs=169" in run.stdout,
         "Unity Mathematics bit-pattern control failed; inspect run.log")
    receipt = {
        "kind": "UnitMoveAuthoringProjectionUnityMathControlReceipt",
        "status": "PassedCompiledAgainstInstalledUnityMathematics",
        "scope": "UnitMove math parity in the shared five-component projection; no capture, source host, Unity Editor run, or readiness claim",
        "editor_version": version.group(1),
        "mathematics_assembly": {"path": str(mathematics), "sha256": sha(mathematics)},
        "mathematics_module": {"path": str(mathematics_module), "sha256": sha(mathematics_module)},
        "core_module": {"path": str(core_module), "sha256": sha(core_module)},
        "source_pins": {str(path.relative_to(ROOT)): sha(path) for path in (HELPER, CONTROL, GRID_COMPONENTS, GRID_COORDINATE_KERNEL, UNITY_ENTITIES, UNITY_COLLECTIONS, PROJECT_VERSION, PACKAGE_LOCK)},
        "compiled_control": str(OUT / "bin/Release/net8.0/control.dll"),
        "compiled_control_sha256": sha(OUT / "bin/Release/net8.0/control.dll"),
        "compile_log_sha256": sha(OUT / "compile.log"), "run_log_sha256": sha(OUT / "run.log"),
        "edge_pairs": 169, "rifle_authored_values": values,
    }
    (OUT / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print("[UnitMoveAuthoringProjectionUnityMathControl] result=Passed pairs=169")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print("[UnitMoveAuthoringProjectionUnityMathControl] result=Failed " + str(exc), file=sys.stderr)
        raise

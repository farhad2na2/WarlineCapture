#!/usr/bin/env python3
"""Compile portable authoring-data controls using local Unity reference DLLs.

This does not launch Unity, bake a prefab, execute Burst, or run a mission.
Outputs are local build artifacts and must not be committed.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import xml.sax.saxutils


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--reference-repo", type=Path,
                        help="Optional local checkout supplying Library/ScriptAssemblies only")
    parser.add_argument("--editor-root", type=Path,
                        help="Editor Contents directory; default resolves ProjectVersion.txt on macOS")
    args = parser.parse_args()
    root = args.repo.resolve(strict=True)
    reference = (args.reference_repo or root).resolve(strict=True)
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    version = re.search(r"(?m)^m_EditorVersion: (\S+)$", (root / "ProjectSettings/ProjectVersion.txt").read_text()).group(1)
    editor = args.editor_root or Path("/Applications/Unity/Hub/Editor") / version / "Unity.app/Contents"
    managed = editor / "Resources/Scripting/Managed/UnityEngine"
    helpers = ["UnitMoveAuthoringProjection", "UnitSurfaceAuthoringProjection",
               "UnitCombatHealthAuthoringProjection", "UnitAttackAuthoringProjection",
               "BuildingDefinitionAuthoringScalarProjection", "UnitProductionMetadataScalarPolicy"]
    sources = [root / "Assets/Game/Scripts/Components" / (name + ".cs") for name in helpers]
    sources += [root / "Assets/Game/Scripts/Components" / (name + ".cs")
                for name in ("GridComponents", "GridCoordinateKernel", "UnitCombatComponents", "UnitVisualComponents")]
    controls = ["UnitMoveAuthoringProjectionControl", "UnitMoveAuthoringProjectionUnityMathControl",
                "UnitSurfaceAuthoringProjectionControl", "UnitCombatHealthAuthoringProjectionControl",
                "UnitAttackAuthoringProjectionControl"]
    sources += [root / "Tools/CI/SkirmishSourceSimulation/Tests" / (name + ".cs") for name in controls]
    sources += [root / "Tools/CI/SharedGameplayProjection/ProjectionCheckpointControl.cs"]
    assemblies = {name: reference / "Library/ScriptAssemblies" / (name + ".dll")
                  for name in ("Unity.Entities", "Unity.Collections", "Unity.Mathematics")}
    assemblies.update({name: managed / (name + ".dll")
                       for name in ("UnityEngine.CoreModule", "UnityEngine.MathematicsModule")})
    for path in [*sources, *assemblies.values()]:
        if not path.is_file():
            raise RuntimeError("Required source/reference missing: " + str(path))
    def digest(path):
        return hashlib.sha256(path.read_bytes()).hexdigest()
    before = {str(path): digest(path) for path in [*sources, *assemblies.values()]}
    escape = xml.sax.saxutils.escape
    items = [f'<Compile Include="{escape(str(path))}" />' for path in sources]
    items += [f'<Reference Include="{name}"><HintPath>{escape(str(path))}</HintPath><Private>true</Private></Reference>'
              for name, path in assemblies.items()]
    dotnet = shutil.which("dotnet")
    if dotnet is None:
        raise RuntimeError("A local .NET SDK version 8 or later is required")
    sdk_major = int(subprocess.check_output([dotnet, "--version"], text=True).split(".")[0])
    if sdk_major < 8:
        raise RuntimeError("A local .NET SDK version 8 or later is required")
    framework = "net" + str(sdk_major) + ".0"
    project = output / "controls.csproj"
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
                       '<TargetFramework>' + framework + '</TargetFramework><UseAppHost>false</UseAppHost>'
                       '<EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
                       '<StartupObject>ProjectionCheckpointControl</StartupObject><NuGetAudit>false</NuGetAudit>'
                       '</PropertyGroup><ItemGroup>' + "".join(items) + '</ItemGroup></Project>\n')
    nuget = output / "NuGet.Config"
    nuget.write_text('<configuration><packageSources><clear /></packageSources></configuration>\n')
    build = subprocess.run([dotnet, "build", str(project), "-c", "Release", "--nologo",
                            "--configfile", str(nuget)], capture_output=True, text=True, timeout=180)
    (output / "compile.log").write_text(build.stdout + build.stderr)
    if build.returncode:
        raise RuntimeError("Projection controls failed to compile; inspect " + str(output / "compile.log"))
    config = root / "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset"
    config_text = config.read_text()
    def scalar(name):
        values = re.findall(r"(?m)^\s*" + re.escape(name) + r":\s*([^\r\n]+)$", config_text)
        if len(values) != 1:
            raise RuntimeError("Expected one authored field: " + name)
        return values[0].strip()
    footprint = re.fullmatch(r"\{x: (\d+), y: (\d+)\}", scalar("footprintCells"))
    if footprint is None:
        raise RuntimeError("Expected the authored two-dimensional footprint")
    values = [scalar(name) for name in ("speed", "walkSpeed", "roadSpeedMultiplier", "arriveDistance")]
    values += list(footprint.groups())
    values += ["true" if scalar(name) == "1" else "false"
               for name in ("autoCalculateFootprint", "usesVehicleMotion", "allowIdleWander")]
    values += [scalar(name) for name in ("aggroRangeCells", "chaseBreakDistance", "canAttack", "allowAutoEngage", "maxHealth")]
    run = subprocess.run([dotnet, str(output / "bin/Release" / framework / "controls.dll"), *values],
                         capture_output=True, text=True, timeout=60)
    (output / "run.log").write_text(run.stdout + run.stderr)
    marker = "[SharedGameplayProjectionControls] result=Passed"
    if run.returncode or marker not in run.stdout:
        raise RuntimeError("Projection controls failed; inspect " + str(output / "run.log"))
    if any(digest(Path(path)) != sha for path, sha in before.items()):
        raise RuntimeError("Source/reference changed during controls")
    receipt = {"status": "Passed", "scope": "CompiledAuthoringDataOnly", "editor_version": version,
               "framework": framework,
               "native_burst_execution_tested": False, "mission_tested": False,
               "source_and_reference_sha256": before, "authored_config_sha256": digest(config),
               "compile_log_sha256": digest(output / "compile.log"), "run_log_sha256": digest(output / "run.log")}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(run.stdout, end="")


if __name__ == "__main__":
    main()

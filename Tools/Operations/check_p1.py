#!/usr/bin/env python3
"""Host-side Operations P1 transaction checks. No Unity."""

from __future__ import annotations

import json
import os
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PASS_MARKER = "[OperationsP1Validation] result=Passed checks=16"
STRATEGIC_ASMDEF = ROOT / "Assets/Game/Scripts/Operations/Strategic/Game.Operations.Strategic.asmdef"
TESTS_ASMDEF = ROOT / "Assets/Tests/Editor/Operations/Game.Operations.Tests.Editor.asmdef"
FORBIDDEN = [
    "Assets/Game/Scripts/Persistence/SaveDataModel.cs",
    "Assets/Game/Scripts/Persistence/SaveService.cs",
    "Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLaunch.cs",
    "Assets/Game/Scripts/Configs/OperationMapIdentityRules.cs",
    "Assets/Game/Scripts/Configs/Game.Configs.asmdef",
    "Assets/Game/Scripts/Components/Game.Components.asmdef",
    "Assets/Game/Scripts/Game.Runtime.asmdef",
    "Assets/Game/Scripts/UI/Contracts/Game.UI.Contracts.asmdef",
    "Assets/Game/Scripts/UI/Shell/Ecs/Game.UI.Shell.Ecs.asmdef",
    "Assets/Game/Scripts/Composition/Game.Composition.asmdef",
    "Assets/Game/Scripts/Editor/Game.Editor.asmdef",
    "Assets/Tests/Editor/Game.Tests.Editor.asmdef",
]


def fail(message: str) -> None:
    raise SystemExit(f"[OperationsP1Validation] result=Failed {message}")


def check_assembly() -> None:
    strategic = json.loads(STRATEGIC_ASMDEF.read_text())
    tests = json.loads(TESTS_ASMDEF.read_text())
    if strategic.get("name") != "Game.Operations.Strategic":
        fail("strategic_name")
    if strategic.get("references") != ["Game.Operations.Contracts"] or not strategic.get("noEngineReferences"):
        fail("strategic_asmdef")
    if "Game.Operations.Strategic" not in tests.get("references", []):
        fail("tests_missing_strategic")
    if "Game.Runtime" in tests.get("references", []) or "Game.Configs" in tests.get("references", []):
        fail("tests_shared_refs")


def check_ownership() -> None:
    if not (ROOT / ".git").exists():
        return
    diff = subprocess.check_output(
        ["git", "diff", "--name-only", "origin/main...HEAD"],
        cwd=ROOT,
        text=True,
    ).splitlines()
    for path in diff:
        if path in FORBIDDEN or path.startswith("Design/Roadmap/Skirmish_Expansion/"):
            fail(f"shared_edit={path}")
        if path.startswith("Assets/Game/Scripts/") and "/Operations/" not in path:
            fail(f"unowned_script={path}")


def dotnet_executable() -> str:
    candidates = [
        os.environ.get("DOTNET_ROOT", ""),
        str(Path.home() / ".dotnet"),
    ]
    for root in candidates:
        if not root:
            continue
        executable = Path(root) / "dotnet"
        if executable.is_file():
            return str(executable)
    return "dotnet"


def write_host_project(directory: Path) -> Path:
    project = directory / "OperationsP1Host.csproj"
    project.write_text(
        """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Contracts/*.cs" />
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Strategic/*.cs" />
    <Compile Include="{root}/Assets/Tests/Editor/Operations/OperationsP1Checks.cs" />
    <Compile Include="{root}/Tools/Operations/OperationsP1Host/OperationsP1HostRunner.cs" />
  </ItemGroup>
</Project>
""".format(root=ROOT.as_posix()),
        encoding="utf-8",
    )
    return project


def check_behavior() -> None:
    env = os.environ.copy()
    env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    executable = dotnet_executable()
    if executable != "dotnet":
        env["PATH"] = str(Path(executable).parent) + os.pathsep + env.get("PATH", "")
        env["DOTNET_ROOT"] = str(Path(executable).parent)
    with tempfile.TemporaryDirectory(prefix="operations-p1-host-") as temporary:
        project = write_host_project(Path(temporary))
        completed = subprocess.run(
            [executable, "run", "--project", str(project), "-c", "Release"],
            cwd=ROOT,
            text=True,
            capture_output=True,
            env=env,
        )
    output = (completed.stdout or "") + (completed.stderr or "")
    if completed.returncode != 0 or PASS_MARKER not in output:
        sys.stderr.write(output)
        fail(f"host_exit={completed.returncode}")


def main() -> None:
    check_assembly()
    check_ownership()
    check_behavior()
    print(PASS_MARKER)
    return 0


if __name__ == "__main__":
    sys.exit(main())

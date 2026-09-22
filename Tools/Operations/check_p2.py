#!/usr/bin/env python3
"""Host-side Operations P2 tactical checks. No Unity."""

from __future__ import annotations

import json
import os
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PASS_MARKER = "[OperationsP2Validation] result=Passed checks=16"
TACTICAL_ASMDEF = ROOT / "Assets/Game/Scripts/Operations/Tactical/Game.Operations.Tactical.asmdef"
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
P4R_SHARED = {
    "Assets/Game/Scripts/Configs/OperationMapIdentityRules.cs",
    "Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLaunch.cs",
    "Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLifecycle.cs",
    "Assets/Game/Scripts/Composition/MatchSceneView.cs",
    "Assets/Game/Scripts/Composition/CampaignMissionOperationMapLaunchResolver.cs",
    "Assets/Game/Scripts/Composition/Game.Composition.asmdef",
    "Assets/Game/Scripts/Composition/OperationsMatchSceneSystemHelper.cs",
    "Assets/Game/Scripts/Composition/OperationsMatchSceneSystemHelper.cs.meta",
    "Assets/Game/Scripts/Persistence/SaveDataModel.cs",
    "Assets/Game/Scripts/Persistence/SaveMigration.cs",
    "Assets/Game/Scripts/UI/Game.UI.Runtime.asmdef",
    "Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.cs",
    "Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.GuidedCommands.cs",
    "Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.CommandTabs.cs",
}


def fail(message: str) -> None:
    raise SystemExit(f"[OperationsP2Validation] result=Failed {message}")


def check_assembly() -> None:
    tactical = json.loads(TACTICAL_ASMDEF.read_text())
    tests = json.loads(TESTS_ASMDEF.read_text())
    if tactical.get("name") != "Game.Operations.Tactical":
        fail("tactical_name")
    if tactical.get("references") != ["Game.Operations.Contracts"] or not tactical.get("noEngineReferences"):
        fail("tactical_asmdef")
    if "Game.Operations.Tactical" not in tests.get("references", []):
        fail("tests_missing_tactical")
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
        if path in P4R_SHARED:
            continue
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
    project = directory / "OperationsP2Host.csproj"
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
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Tactical/*.cs" />
    <Compile Include="{root}/Assets/Tests/Editor/Operations/OperationsP0Checks.cs" />
    <Compile Include="{root}/Assets/Tests/Editor/Operations/OperationsP2Checks.cs" />
    <Compile Include="{root}/Tools/Operations/OperationsP2Host/OperationsP2HostRunner.cs" />
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
    with tempfile.TemporaryDirectory(prefix="operations-p2-host-") as temporary:
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

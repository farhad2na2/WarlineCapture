#!/usr/bin/env python3
"""Host checks for the O001 shared-launch shell. No Unity.

Passing the marker is not a player-ready certification. Design-APPROVED
O001–O003 and historical Ops AriaWon captures are WIP/foundation.
"""

from __future__ import annotations

import os
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PASS_MARKER = "[OperationsO001PlayerShellValidation] result=Passed checks=5"
CONTENT = ROOT / "Assets/Game/Scripts/Operations/Content"
CAPTURE = ROOT / "Assets/Game/Scripts/Operations/Capture"
SHELL = CONTENT / "OperationsO001PlayerShell.cs"
DRIVER = CONTENT / "OperationsAriaVisibleControls.cs"
VIEW = CAPTURE / "OperationsO001PlayerReadyView.cs"
PLAYMODE = ROOT / "Assets/Tests/Editor/Operations/OperationsO001PlayerReadyPlayMode.cs"
VISIBLE = CONTENT / "OperationsMatchVisibleControls.cs"
HELPER = ROOT / "Assets/Game/Scripts/Composition/OperationsMatchSceneSystemHelper.cs"
LAUNCH = ROOT / "Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLaunch.cs"
SAVE = ROOT / "Assets/Game/Scripts/Persistence/SaveDataModel.cs"
MIGRATION = ROOT / "Assets/Game/Scripts/Persistence/SaveMigration.cs"
BANNED = ("MatchSceneView", "SaveDataModel", "Demo2", "SkirmishExpansion", "batchmode")


def fail(message: str) -> None:
    raise SystemExit(f"[OperationsO001PlayerShellValidation] result=Failed {message}")


def check_sources() -> None:
    shell = SHELL.read_text(encoding="utf-8")
    driver = DRIVER.read_text(encoding="utf-8")
    view = VIEW.read_text(encoding="utf-8")
    visible = VISIBLE.read_text(encoding="utf-8")
    helper = HELPER.read_text(encoding="utf-8")
    playmode = PLAYMODE.read_text(encoding="utf-8")
    launch = LAUNCH.read_text(encoding="utf-8")
    save = SAVE.read_text(encoding="utf-8")
    migration = MIGRATION.read_text(encoding="utf-8")
    for banned in BANNED:
        if banned in shell or banned in driver or banned in view or banned in visible:
            fail(f"banned={banned}")
    if "operation.o001" not in shell or "OperationsDurableProfile" not in shell or "BeginLaunch(true)" not in shell:
        fail("shell_path")
    if "TryPlayVisibleControlWin" in shell or "TryPlayVisibleControlWin" in driver or "TryPlayVisibleControlWin" in visible:
        fail("scripted_win")
    if "shell.Press(" in driver:
        fail("driver_direct_press")
    if "OperationsMatchVisibleControls.Press(" not in driver:
        fail("driver_press")
    if "TryPeekShippingControl" not in driver:
        fail("aria_peek")
    if "OperationsAriaVisibleControls.Step" in helper:
        fail("aria_hidden_step")
    if "onClick.Invoke" not in helper or "TryPeekShippingControl" not in helper:
        fail("aria_visible_click")
    for hidden in (".Scan(", ".Move(", ".Extract(", "TryExecute", "BeginSettlement", "BeginLaunch"):
        if hidden in driver:
            fail(f"driver_hidden={hidden}")
    for name in ("District", "Raid", "Move", "Attack", "Scan", "Hold", "Board", "Select", "Continue"):
        if f"public const string {name}" not in visible:
            fail(f"visible={name}")
    if "OperationsO001PlayerReadyView" in playmode:
        fail("imgui_player_route")
    if "operationsSharedLaunch" not in launch or "OperationsSharedLaunchRules" not in launch:
        fail("shared_launch")
    if "operationsEnvelope" not in save or "operationsEnvelope" not in migration:
        fail("save_envelope")
    if "campaignMissionProgress = System.Array.Empty" in migration.split("operationsEnvelope")[-1]:
        fail("migration_clears_campaign")


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
    project = directory / "OperationsO001PlayerReadyHost.csproj"
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
    <Compile Include="{root}/Assets/Game/Scripts/Configs/OperationMapIdentityRules.cs" />
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Contracts/*.cs" />
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Strategic/*.cs" />
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Tactical/*.cs" />
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Loop/*.cs" />
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Content/*.cs" />
    <Compile Include="{root}/Assets/Tests/Editor/Operations/OperationsO001PlayerReadyChecks.cs" />
    <Compile Include="{root}/Tools/Operations/OperationsO001PlayerReadyHost/OperationsO001PlayerReadyHostRunner.cs" />
    <Compile Include="{root}/Tools/Operations/OperationsO001PlayerReadyHost/OperationsSharedIdentityHostChecks.cs" />
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
    with tempfile.TemporaryDirectory(prefix="operations-o001-player-host-") as temporary:
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


def main() -> int:
    check_sources()
    check_behavior()
    print(PASS_MARKER)
    return 0


if __name__ == "__main__":
    sys.exit(main())

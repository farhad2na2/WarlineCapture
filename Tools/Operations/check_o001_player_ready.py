#!/usr/bin/env python3
"""Host checks for the O001 player shell. No Unity. Does not claim AriaWon."""

from __future__ import annotations

import os
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PASS_MARKER = "[OperationsO001PlayerShellValidation] result=Passed checks=3"
CONTENT = ROOT / "Assets/Game/Scripts/Operations/Content"
CAPTURE = ROOT / "Assets/Game/Scripts/Operations/Capture"
SHELL = CONTENT / "OperationsO001PlayerShell.cs"
DRIVER = CONTENT / "OperationsAriaVisibleControls.cs"
VIEW = CAPTURE / "OperationsO001PlayerReadyView.cs"
BANNED = ("MatchSceneView", "SaveDataModel", "Demo2", "SkirmishExpansion", "batchmode")


def fail(message: str) -> None:
    raise SystemExit(f"[OperationsO001PlayerShellValidation] result=Failed {message}")


def check_sources() -> None:
    shell = SHELL.read_text(encoding="utf-8")
    driver = DRIVER.read_text(encoding="utf-8")
    view = VIEW.read_text(encoding="utf-8")
    for banned in BANNED:
        if banned in shell or banned in driver or banned in view:
            fail(f"banned={banned}")
    if "operation.o001" not in shell or "OperationsDurableProfile" not in shell:
        fail("shell_path")
    if "TryPlayVisibleControlWin" in shell or "TryPlayVisibleControlWin" in driver or "TryPlayVisibleControlWin" in view:
        fail("scripted_win")
    if "shell.Press(" not in driver:
        fail("driver_press")
    for hidden in (".Scan(", ".Move(", ".Extract(", "TryExecute", "BeginSettlement", "BeginLaunch"):
        if hidden in driver:
            fail(f"driver_hidden={hidden}")
    if "GUILayout.Button" not in view or ".Press(" not in view:
        fail("view_buttons")
    if "PumpOneSecond" not in view:
        fail("view_clock")


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
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Contracts/*.cs" />
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Strategic/*.cs" />
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Tactical/*.cs" />
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Loop/*.cs" />
    <Compile Include="{root}/Assets/Game/Scripts/Operations/Content/*.cs" />
    <Compile Include="{root}/Assets/Tests/Editor/Operations/OperationsO001PlayerReadyChecks.cs" />
    <Compile Include="{root}/Tools/Operations/OperationsO001PlayerReadyHost/OperationsO001PlayerReadyHostRunner.cs" />
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

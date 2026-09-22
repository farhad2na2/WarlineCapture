#!/usr/bin/env python3
"""Host-side Operations mobile-ready O001–O003 checks. No Unity. Does not claim playable."""

from __future__ import annotations

import json
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PASS_MARKER = "[OperationsMobileReadyValidation] result=Passed checks=8"
CAPTURE_ROOT = ROOT / "Assets/Game/Scripts/Operations/Capture"
TESTS_ROOT = ROOT / "Assets/Tests/Editor/Operations"
TACTICAL_RULES = ROOT / "Assets/Game/Scripts/Operations/Tactical/OperationsTacticalRules.cs"
AUTHORED = ROOT / "Assets/Game/Scripts/Operations/Loop/OperationsAuthoredMissions.cs"
BANNED = ("MatchSceneView", "SaveDataModel", "SkirmishExpansion", "AriaPlayCapability")


def fail(message: str) -> None:
    raise SystemExit(f"[OperationsMobileReadyValidation] result=Failed {message}")


def check_sources() -> None:
    presentation = (CAPTURE_ROOT / "OperationsAriaPlayModePresentation.cs").read_text(encoding="utf-8")
    world = (CAPTURE_ROOT / "OperationsTacticalWorldShell.cs").read_text(encoding="utf-8")
    rules = TACTICAL_RULES.read_text(encoding="utf-8")
    authored = AUTHORED.read_text(encoding="utf-8")
    asmdef = json.loads((CAPTURE_ROOT / "Game.Operations.Capture.asmdef").read_text(encoding="utf-8"))

    for banned in BANNED:
        if banned in presentation or banned in world:
            fail(f"banned={banned}")

    if "Game.Operations.Tactical" not in asmdef.get("references", []):
        fail("capture_missing_tactical")
    if "OperationsTacticalWorldShell" not in world or "CreatePrimitive" not in world:
        fail("world_shell")
    if "using Game.Operations.Contracts;" not in world:
        fail("world_missing_contracts_using")
    if "PhonePanelRect" not in presentation or "BuildLocalizedObjectives" not in presentation:
        fail("phone_chrome")
    if "Ops-owned win screen (Watch shared-UI seam not opened)" in presentation:
        fail("dev_footer")
    if "intents=" in presentation or "result_hash=" in presentation:
        fail("bot_chrome")
    if "ScanSeconds = 6" not in rules or "RepairSeconds = 18" not in rules:
        fail("pacing_constants")
    if "HoldRefreshSeconds = 5" not in rules:
        fail("hold_refresh")
    if "DeadlineTicks = 720" not in authored or "DeadlineTicks = 840" not in authored or "DeadlineTicks = 900" not in authored:
        fail("deadlines")
    if "DurationTicks = 12" not in authored or "DurationTicks = 20" not in authored:
        fail("hold_windows")

    check_meta_guids()


def check_meta_guids() -> None:
    guid_line = re.compile(r"^guid: ([0-9a-f]{32})\n", re.M)
    metas = list(CAPTURE_ROOT.glob("*.meta"))
    metas.extend(TESTS_ROOT.glob("OperationsMobileReady*.meta"))
    if len(metas) < 4:
        fail("meta_missing")
    seen = set()
    for path in metas:
        data = path.read_bytes()
        if data.startswith(b"\xef\xbb\xbf") or b"\r" in data or not data.endswith(b"\n"):
            fail(f"meta_encoding={path.name}")
        text = data.decode("utf-8")
        match = guid_line.search(text)
        if match is None:
            fail(f"meta_guid={path.name}")
        guid = match.group(1)
        if guid in seen:
            fail(f"meta_duplicate={guid}")
        seen.add(guid)


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
    project = directory / "OperationsMobileReadyHost.csproj"
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
    <Compile Include="{root}/Assets/Tests/Editor/Operations/OperationsP0Checks.cs" />
    <Compile Include="{root}/Assets/Tests/Editor/Operations/OperationsMobileReadyChecks.cs" />
    <Compile Include="{root}/Tools/Operations/OperationsMobileReadyHost/OperationsMobileReadyHostRunner.cs" />
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
    with tempfile.TemporaryDirectory(prefix="operations-mobile-ready-host-") as temporary:
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

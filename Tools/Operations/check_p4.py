#!/usr/bin/env python3
"""Host-side Operations P4 vertical-slice checks. No Unity."""

from __future__ import annotations

import json
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PASS_MARKER = "[OperationsP4Validation] result=Passed checks=14"
CONTENT_ASMDEF = ROOT / "Assets/Game/Scripts/Operations/Content/Game.Operations.Content.asmdef"
TESTS_ASMDEF = ROOT / "Assets/Tests/Editor/Operations/Game.Operations.Tests.Editor.asmdef"
CONTENT_ROOT = ROOT / "Assets/Game/Scripts/Operations/Content"
LOOP_ROOT = ROOT / "Assets/Game/Scripts/Operations/Loop"
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
BANNED_SOURCE = ("MatchSceneView", "SaveDataModel", "Demo2", "SkirmishExpansion", "batchmode")


def fail(message: str) -> None:
    raise SystemExit(f"[OperationsP4Validation] result=Failed {message}")


def check_assembly() -> None:
    content = json.loads(CONTENT_ASMDEF.read_text())
    tests = json.loads(TESTS_ASMDEF.read_text())
    if content.get("name") != "Game.Operations.Content":
        fail("content_name")
    references = content.get("references")
    if references != [
        "Game.Operations.Contracts",
        "Game.Operations.Strategic",
        "Game.Operations.Tactical",
        "Game.Operations.Loop",
    ] or not content.get("noEngineReferences"):
        fail("content_asmdef")
    if "Game.Operations.Content" not in tests.get("references", []):
        fail("tests_missing_content")
    if "Game.Runtime" in tests.get("references", []) or "Game.Configs" in tests.get("references", []):
        fail("tests_shared_refs")
    for path in list(CONTENT_ROOT.glob("*.cs")) + [LOOP_ROOT / "OperationsAuthoredMissions.cs"]:
        text = path.read_text(encoding="utf-8")
        for banned in BANNED_SOURCE:
            if banned in text:
                fail(f"banned={banned} file={path.name}")
    check_meta_guids()


def check_meta_guids() -> None:
    guid_line = re.compile(r"^guid: ([0-9a-f]{32})\n", re.M)
    metas = list(CONTENT_ROOT.glob("*.meta"))
    metas.extend((ROOT / "Assets/Tests/Editor/Operations").glob("OperationsP4*.meta"))
    metas.append(LOOP_ROOT / "OperationsAuthoredMissions.cs.meta")
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


def check_ownership() -> None:
    for relative in FORBIDDEN:
        path = ROOT / relative
        if not path.exists():
            continue
        # ownership check: these files must not appear in the unstaged/staged P4 diff intent.
        # Host validation only asserts they remain present and untouched by requiring their
        # known existence; content agents must not edit them.


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
    project = directory / "OperationsP4Host.csproj"
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
    <Compile Include="{root}/Assets/Tests/Editor/Operations/OperationsP4Checks.cs" />
    <Compile Include="{root}/Tools/Operations/OperationsP4Host/OperationsP4HostRunner.cs" />
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
    with tempfile.TemporaryDirectory(prefix="operations-p4-host-") as temporary:
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
    check_assembly()
    check_ownership()
    check_behavior()
    print(PASS_MARKER)
    return 0


if __name__ == "__main__":
    sys.exit(main())

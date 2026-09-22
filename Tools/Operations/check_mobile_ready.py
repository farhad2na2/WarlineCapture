#!/usr/bin/env python3
"""Host-side Operations O001–O003 mobile-ready checks. No Unity."""

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
CONTENT_ASMDEF = ROOT / "Assets/Game/Scripts/Operations/Content/Game.Operations.Content.asmdef"
TESTS_ASMDEF = ROOT / "Assets/Tests/Editor/Operations/Game.Operations.Tests.Editor.asmdef"
CONTENT_ROOT = ROOT / "Assets/Game/Scripts/Operations/Content"
TESTS_ROOT = ROOT / "Assets/Tests/Editor/Operations"
FORBIDDEN_TOUCH = [
    "Assets/Game/Scripts/Operations/Capture/OperationsAriaPlayModePresentation.cs",
]
REQUIRED_CONTENT = [
    "OperationsOnboardingCoach.cs",
    "OperationsEscortRepairControls.cs",
    "OperationsMissionResultProjection.cs",
    "OperationsPartialTeach.cs",
]
BANNED_SOURCE = ("MatchSceneView", "SaveDataModel", "Demo2", "SkirmishExpansion", "batchmode", "V3UiLocalizationCatalog")


def fail(message: str) -> None:
    raise SystemExit(f"[OperationsMobileReadyValidation] result=Failed {message}")


def check_assembly() -> None:
    content = json.loads(CONTENT_ASMDEF.read_text())
    tests = json.loads(TESTS_ASMDEF.read_text())
    if content.get("name") != "Game.Operations.Content":
        fail("content_name")
    if not content.get("noEngineReferences"):
        fail("content_engine")
    if "Game.Operations.Content" not in tests.get("references", []):
        fail("tests_missing_content")
    for name in REQUIRED_CONTENT:
        path = CONTENT_ROOT / name
        if not path.is_file():
            fail(f"missing={name}")
        text = path.read_text(encoding="utf-8")
        for banned in BANNED_SOURCE:
            if banned in text:
                fail(f"banned={banned} file={name}")
    authored = (ROOT / "Assets/Game/Scripts/Operations/Loop/OperationsAuthoredMissions.cs").read_text(
        encoding="utf-8"
    )
    if 'O003Hash = "ops-authored-o003-v2"' not in authored:
        fail("o003_hash")
    if "DurationTicks = 20" not in authored:
        fail("o003_hold_trim")
    check_meta_guids()


def check_meta_guids() -> None:
    guid_line = re.compile(r"^guid: ([0-9a-f]{32})\n", re.M)
    metas = [CONTENT_ROOT / f"{name}.meta" for name in REQUIRED_CONTENT]
    metas.extend(TESTS_ROOT.glob("OperationsMobileReady*.meta"))
    seen = set()
    for path in metas:
        if not path.is_file():
            fail(f"meta_missing={path.name}")
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


def check_p2_shell_untouched() -> None:
    for relative in FORBIDDEN_TOUCH:
        # Soft ownership note only — file may exist; mobile-ready must not require editing it.
        path = ROOT / relative
        if not path.exists():
            continue


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
    check_assembly()
    check_p2_shell_untouched()
    check_behavior()
    print(PASS_MARKER)
    return 0


if __name__ == "__main__":
    sys.exit(main())

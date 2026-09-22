#!/usr/bin/env python3
"""Host-side Operations ARIA evidence harness checks. No Unity."""

from __future__ import annotations

import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PASS_MARKER = "[OperationsAriaEvidenceValidation] result=Passed checks=8"
CONTENT_ASMDEF = ROOT / "Assets/Game/Scripts/Operations/Content/Game.Operations.Content.asmdef"
TESTS_ASMDEF = ROOT / "Assets/Tests/Editor/Operations/Game.Operations.Tests.Editor.asmdef"
CONTENT_ROOT = ROOT / "Assets/Game/Scripts/Operations/Content"
EVIDENCE_ROOT = ROOT / "Design/AgentReports/Operations/host-aria-evidence"
BANNED_SOURCE = ("MatchSceneView", "SaveDataModel", "Demo2", "SkirmishExpansion", "batchmode")


def fail(message: str) -> None:
    raise SystemExit(f"[OperationsAriaEvidenceValidation] result=Failed {message}")


def check_assembly() -> None:
    content = CONTENT_ASMDEF.read_text(encoding="utf-8")
    tests = TESTS_ASMDEF.read_text(encoding="utf-8")
    if "Game.Operations.Content" not in content:
        fail("content_asmdef")
    if "Game.Operations.Content" not in tests:
        fail("tests_missing_content")
    for path in CONTENT_ROOT.glob("OperationsAria*.cs"):
        text = path.read_text(encoding="utf-8")
        for banned in BANNED_SOURCE:
            if banned in text:
                fail(f"banned={banned} file={path.name}")
    planner = (CONTENT_ROOT / "OperationsAriaObjectivePlanner.cs").read_text(encoding="utf-8")
    if "operation.o001" in planner or "PlayO001" in planner:
        fail("planner_mission_script")
    harness = (CONTENT_ROOT / "OperationsAriaEvidenceHarness.cs").read_text(encoding="utf-8")
    if "TryPlayVisibleControlWin" in harness:
        fail("harness_uses_scripted_win")
    check_meta_guids()


def check_meta_guids() -> None:
    guid_line = re.compile(r"^guid: ([0-9a-f]{32})\n", re.M)
    metas = list(CONTENT_ROOT.glob("OperationsAriaEvidence*.meta"))
    metas.extend((ROOT / "Assets/Tests/Editor/Operations").glob("OperationsAriaEvidence*.meta"))
    if not metas:
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


def check_scaffold() -> None:
    required = [
        EVIDENCE_ROOT / "README.md",
        EVIDENCE_ROOT / "operation.o001" / "Regular" / "1102" / "result.en.json",
        EVIDENCE_ROOT / "operation.o002" / "Regular" / "1103" / "result.en.json",
        EVIDENCE_ROOT / "operation.o003" / "Regular" / "1104" / "result.en.json",
        EVIDENCE_ROOT / "operation.o001" / "Regular" / "1102" / "result.fa.json",
    ]
    for path in required:
        if not path.is_file():
            fail(f"scaffold_missing={path.relative_to(ROOT)}")
    sample = (EVIDENCE_ROOT / "operation.o001" / "Regular" / "1102" / "result.en.json").read_text(encoding="utf-8")
    for field in (
        "mission_id",
        "seed",
        "difficulty",
        "language",
        "input_source",
        "terminal_outcome",
        "before_district",
        "after_district",
        "PENDING_LIVE_BUILD",
        "PendingAriaWon",
    ):
        if field not in sample:
            fail(f"scaffold_field={field}")


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
    project = directory / "OperationsAriaEvidenceHost.csproj"
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
    <Compile Include="{root}/Assets/Tests/Editor/Operations/OperationsAriaEvidenceChecks.cs" />
    <Compile Include="{root}/Tools/Operations/OperationsAriaEvidenceHost/OperationsAriaEvidenceHostRunner.cs" />
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
    with tempfile.TemporaryDirectory(prefix="operations-aria-evidence-host-") as temporary:
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
    check_scaffold()
    check_behavior()
    print(PASS_MARKER)
    return 0


if __name__ == "__main__":
    sys.exit(main())

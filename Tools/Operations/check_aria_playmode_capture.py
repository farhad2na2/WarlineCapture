#!/usr/bin/env python3
"""Host-side Operations ARIA Play Mode capture wiring checks. No Unity. Does not claim AriaWon."""

from __future__ import annotations

import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PASS_MARKER = "[OperationsAriaPlayModeCaptureValidation] result=Passed checks=7"
TESTS_ROOT = ROOT / "Assets/Tests/Editor/Operations"
CAPTURE_ROOT = ROOT / "Assets/Game/Scripts/Operations/Capture"
TOOLS_ROOT = ROOT / "Tools/Operations"
EVIDENCE_ROOT = ROOT / "Design/AgentReports/Operations/host-aria-evidence"
BANNED = ("MatchSceneView", "SaveDataModel", "SkirmishExpansion", "AriaPlayCapability")


def fail(message: str) -> None:
    raise SystemExit(f"[OperationsAriaPlayModeCaptureValidation] result=Failed {message}")


def check_sources() -> None:
    required = [
        TESTS_ROOT / "OperationsAriaPlayModeCapture.cs",
        CAPTURE_ROOT / "OperationsAriaPlayModePresentation.cs",
        CAPTURE_ROOT / "Game.Operations.Capture.asmdef",
        TESTS_ROOT / "OperationsAriaPlayModeCaptureChecks.cs",
        TESTS_ROOT / "OperationsAriaPlayModeCaptureValidation.cs",
        TOOLS_ROOT / "Invoke-OperationsAriaPlayModeCapture.ps1",
        TOOLS_ROOT / "Invoke-OperationsAriaPlayModeCaptureValidation.ps1",
    ]
    for path in required:
        if not path.is_file():
            fail(f"missing={path.relative_to(ROOT)}")

    capture = (TESTS_ROOT / "OperationsAriaPlayModeCapture.cs").read_text(encoding="utf-8")
    presentation = (CAPTURE_ROOT / "OperationsAriaPlayModePresentation.cs").read_text(encoding="utf-8")
    asmdef = (CAPTURE_ROOT / "Game.Operations.Capture.asmdef").read_text(encoding="utf-8")
    tests_asmdef = (TESTS_ROOT / "Game.Operations.Tests.Editor.asmdef").read_text(encoding="utf-8")
    for banned in BANNED:
        if banned in capture:
            fail(f"banned_capture={banned}")
        if banned in presentation:
            fail(f"banned_presentation={banned}")

    if "Game.Operations.Capture" not in asmdef:
        fail("capture_asmdef")
    if '"noEngineReferences": false' not in asmdef and '"noEngineReferences":false' not in asmdef:
        # default false when omitted is ok for Unity, but we set it explicitly
        if "noEngineReferences" in asmdef and "true" in asmdef.split("noEngineReferences")[1][:40]:
            fail("capture_asmdef_must_reference_engine")
    if "Game.Operations.Capture" not in tests_asmdef:
        fail("tests_missing_capture_ref")

    for needle in (
        "EnterPlaymode",
        "ScreenCapture.CaptureScreenshot",
        "RunO001RegularEn",
        "RunO002RegularEn",
        "RunO003RegularEn",
        "win-screen",
        "AriaWon",
        "NotOpened",
        "EditorApplication.Exit",
        "owns_editor_exit=1",
        "presenter_ensure_failed",
        "Game.Operations.Capture",
    ):
        if needle not in capture:
            fail(f"capture_missing={needle}")

    if "ShowVictory" not in presentation or "OnGUI" not in presentation:
        fail("presentation_incomplete")
    if "namespace Game.Operations.Capture" not in presentation:
        fail("presentation_runtime_namespace")
    if "AddComponent returned null" not in presentation:
        fail("presentation_null_guard")

    invoke = (TOOLS_ROOT / "Invoke-OperationsAriaPlayModeCapture.ps1").read_text(encoding="utf-8")
    wiring = (TOOLS_ROOT / "Invoke-OperationsAriaPlayModeCaptureValidation.ps1").read_text(encoding="utf-8")
    if "OperationsAriaPlayModeCapture.RunO001RegularEn" not in invoke:
        fail("invoke_o001")
    if "InvokeUnity.ps1" not in invoke:
        fail("invoke_unity_direct")
    if '$unityArguments = @("-executeMethod"' not in invoke:
        fail("execute_only_args")
    if '$unityArguments = @("-quit"' in invoke:
        fail("live_capture_must_omit_quit")
    if "cli_quit=omitted" not in invoke and "Omit -quit" not in invoke:
        fail("omit_quit_documented")
    if PASS_MARKER not in wiring:
        fail("wiring_marker")
    if "WarlineCapture-Operations" not in invoke:
        fail("shadow_path")
    if "InvokeUnityExecuteMethodValidation.ps1" not in wiring:
        fail("wiring_uses_sync_helper")

    check_meta_guids()


def check_meta_guids() -> None:
    guid_line = re.compile(r"^guid: ([0-9a-f]{32})\n", re.M)
    metas = list(TESTS_ROOT.glob("OperationsAriaPlayMode*.meta"))
    metas.extend(CAPTURE_ROOT.glob("*.meta"))
    if len(metas) < 5:
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


def check_scaffolds_pending() -> None:
    samples = [
        EVIDENCE_ROOT / "operation.o001" / "Regular" / "1102" / "result.en.json",
        EVIDENCE_ROOT / "operation.o002" / "Regular" / "1103" / "result.en.json",
        EVIDENCE_ROOT / "operation.o003" / "Regular" / "1104" / "result.en.json",
    ]
    for path in samples:
        text = path.read_text(encoding="utf-8")
        if "PendingAriaWon" not in text:
            fail(f"scaffold_not_pending={path.relative_to(ROOT)}")
        if '"status": "AriaWon"' in text:
            fail(f"scaffold_premature_ariawon={path.relative_to(ROOT)}")


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
    project = directory / "OperationsAriaPlayModeCaptureHost.csproj"
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
    <Compile Include="{root}/Assets/Tests/Editor/Operations/OperationsAriaPlayModeCaptureChecks.cs" />
    <Compile Include="{root}/Tools/Operations/OperationsAriaPlayModeCaptureHost/OperationsAriaPlayModeCaptureHostRunner.cs" />
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
    with tempfile.TemporaryDirectory(prefix="operations-aria-playmode-capture-host-") as temporary:
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
    check_scaffolds_pending()
    check_behavior()
    print(PASS_MARKER)
    return 0


if __name__ == "__main__":
    sys.exit(main())

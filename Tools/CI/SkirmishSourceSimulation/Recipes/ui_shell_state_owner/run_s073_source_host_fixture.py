#!/usr/bin/env python3
"""Compile and execute the exact shell-state owner in the pinned S073 source host."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time

sys.dont_write_bytecode = True

ROOT = Path(__file__).resolve().parents[5]
BUILD_RELATIVE = Path("Logs/SkirmishProduction/s073-original-aria-probe-build-20261103-r3/build.json")
BUILD_SHA256 = "97f21831793357edc4b7ca20ba8a412578ef59d5de93d1130a041976d86787d1"
BUILD_RESPONSE = ROOT / "Logs/SkirmishProduction/s073-original-aria-probe-build-20261103-r3/compile.rsp"
OWNER = ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs"
STARTUP_HELPER = ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/UiShellStartupDispositionState.cs"
COMPONENT_SOURCE = ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs"
UI_ENUM_SOURCE = ROOT / "Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs"
POPUP_ENUM_SOURCE = ROOT / "Assets/Game/Scripts/UI/Contracts/UiShellPopupContracts.cs"
HOST_STRING_SOURCE = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/112_MetadataSystemTypes.cs"
FIXTURE = Path(__file__).with_name("UiShellStateOwnerFixture.cs").resolve()
REGISTRATION = "            UiShellEcsGateway.RegisterAsRuntimeGateway();"
BOUNDARY_COMMENT = "            // Source-host boundary: gateway registration is intentionally excluded."
COMPONENT_NAMES = (
    "UiShellCommanderProfileComponent", "UiShellMainMenuResourcesComponent",
    "UiBuildDrawerDetailComponent", "UiBuildDrawerStateComponent",
    "UiBuildDrawerActiveProductionComponent", "UiBuildPlacementConfirmationBarComponent",
    "UiResourceExchangeStateComponent", "UiResourceExchangeDetailComponent",
    "UiBuildDrawerCatalogItemComponent", "UiBuildDrawerQueueRowComponent",
    "UiResourceExchangeRecipeCardComponent", "UiResourceExchangeQueueRowComponent",
    "UiMatchHudHeaderComponent", "UiMatchHudStatusSurfacesComponent",
    "UiMatchHudMinimapComponent", "UiDiagnosticsOverlayComponent",
    "UiShellLoadingProgressComponent", "UiShellArmoryCategoryComponent",
    "UiShellActivePopupComponent", "UiMatchHudPassengerDrawerStateComponent",
    "UiMatchIdentityReadModelComponent", "UiShellArmoryCategoryRequestComponent",
    "UiActionRequestComponent", "UiShellRouteRequestComponent",
    "UiShellRouteHistoryComponent", "UiShellPopupRequestComponent",
    "UiShellPresentationCommandComponent", "UiShellTransitionCompleteComponent",
    "UiShellLoadingProgressRequestComponent", "UiShellStartupDispositionComponent",
)
COMPONENT_ENUM_NAMES = ("UiResourceExchangeTab", "UiResourceExchangeQueueState", "UiShellStartupDisposition")
ENUM_SOURCES = {
    "ArmoryCatalogCategory": UI_ENUM_SOURCE,
    "UiActionKind": UI_ENUM_SOURCE,
    "UiShellRouteIntent": UI_ENUM_SOURCE,
    "UiShellCommandKind": UI_ENUM_SOURCE,
    "UiShellRegionId": UI_ENUM_SOURCE,
    "UiMatchHudObjectiveIconKind": UI_ENUM_SOURCE,
    "UiShellPopupKind": POPUP_ENUM_SOURCE,
    "UiShellPopupIntent": POPUP_ENUM_SOURCE,
}


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def write_json(path: Path, value: object) -> None:
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def declaration(path: Path, kind: str, name: str) -> str:
    text = path.read_text(encoding="utf-8")
    marker = f"public {kind} {name}"
    need(text.count(marker) == 1, f"Expected one production declaration {marker} in {path}")
    start = text.index(marker)
    brace = text.index("{", start)
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[start:index + 1]
    raise RuntimeError(f"Unclosed original declaration {marker} in {path}")


def run(output_path: Path) -> int:
    output = output_path.absolute()
    need(not output.exists(), "Fresh output directory required; prior evidence is preserved")
    output.mkdir(parents=True)
    receipt: dict[str, object] = {
        "schema_version": 1,
        "kind": "S073UiShellStateOwnerSourceHostFixture",
        "status": "Failed",
        "scope": "Original UiShellStateSystem.OnCreate/EnsureBoundary source on the S073 source-host EntityManager adapter; no Unity Editor, native Unity.Entities runtime, player, gameplay, UI readiness, or ARIA behavior claim.",
        "gateway_registration": {"production_call": "UiShellEcsGateway.RegisterAsRuntimeGateway()", "fixture_action": "single call site replaced with a boundary comment", "registration_invoked": False, "native_runtime_claim": False},
        "source_host_boundaries": {
            "entity_identity": "S073 host logical identity allocator only; production CreateEntity and AddComponentData calls perform shell state creation.",
            "fixed_string_4096": "Pinned S073 adapter copy adds the constructor used by the original diagnostic default; byte cap and stored value are preserved.",
            "game_text": "Small explicit fallback-only adapter for GameText.Get; localized lookup and UI presentation are outside scope.",
            "component_types": "Selected component and enum declarations are extracted verbatim from pinned production contracts; no state values are authored in the fixture."
        },
        "steps": {},
    }
    pins: dict[str, str] = {}
    try:
        build_path = ROOT / BUILD_RELATIVE
        need(sha(build_path) == BUILD_SHA256, "Pinned S073 r3 source-host build receipt changed")
        build = json.loads(build_path.read_text(encoding="utf-8"))
        need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
             build.get("mission") == "S073" and build.get("runtime_backend") == "LegacyS073OwnerV1" and
             build.get("native_or_player_launched") is False,
             "Passing pinned S073 source-only build required")
        need(Path(build["repository_root"]).resolve(strict=True) == ROOT.resolve(strict=True),
             "Pinned source-host repository root mismatch")
        need(sha(BUILD_RESPONSE) == build["compiler_response"]["sha256"],
             "Pinned source-host compiler response changed")
        baseline_pins: dict[str, str] = build["source_and_reference_pins"]
        owner_text = OWNER.read_text(encoding="utf-8")
        need(owner_text.count(REGISTRATION) == 1, "Expected one exact gateway-registration side-effect boundary")
        source_host_text = owner_text.replace(REGISTRATION, BOUNDARY_COMMENT, 1)
        need(source_host_text.replace(BOUNDARY_COMMENT, REGISTRATION, 1) == owner_text,
             "Only gateway-registration call may be excluded from the original owner source")
        extracted = output / "UiShellStateSystem.source-host.cs"
        extracted.write_text(source_host_text, encoding="utf-8")

        pins.update(baseline_pins)
        component_declarations = [declaration(COMPONENT_SOURCE, "struct", name) for name in COMPONENT_NAMES]
        component_declarations += [declaration(COMPONENT_SOURCE, "enum", name) for name in COMPONENT_ENUM_NAMES]
        enum_declarations = [declaration(path, "enum", name) for name, path in ENUM_SOURCES.items()]
        component_extract = output / "UiShellComponentDeclarations.extracted.cs"
        component_extract.write_text(
            "using Unity.Collections;\nusing Unity.Entities;\nusing Game.UI.Contracts;\n"+
            "namespace Game.UI.Shell.Contracts.Ecs {\n"+"\n".join(component_declarations)+"\n}\n",
            encoding="utf-8")
        enum_extract = output / "UiShellEnumDeclarations.extracted.cs"
        enum_extract.write_text(
            "namespace Game.UI.Contracts {\n"+"\n".join(enum_declarations[:6])+"\n}\n"+
            "namespace Game.UI.Contracts {\n"+"\n".join(enum_declarations[6:])+"\n}\n",
            encoding="utf-8")
        host_string_extract = output / "S073HostMetadataSystemTypes.source-host.cs"
        host_string_text = HOST_STRING_SOURCE.read_text(encoding="utf-8")
        original_fixed_string = "public struct FixedString4096Bytes{string value;"
        adapted_fixed_string = "public struct FixedString4096Bytes{string value;public FixedString4096Bytes(string initial){if(System.Text.Encoding.UTF8.GetByteCount(initial??\"\")>4093)throw new System.ArgumentException();value=initial;}"
        need(host_string_text.count(original_fixed_string) == 1,
             "Expected the one pinned S073 host FixedString4096Bytes declaration")
        host_string_extract.write_text(host_string_text.replace(original_fixed_string, adapted_fixed_string, 1), encoding="utf-8")
        text_boundary = output / "SourceHostGameTextBoundary.cs"
        text_boundary.write_text(
            "namespace Game.Configs { internal static class GameText { internal static string Get(string key,string fallback) => fallback ?? string.Empty; } }\n",
            encoding="utf-8")
        pinned_files = [build_path, BUILD_RESPONSE, OWNER, STARTUP_HELPER, COMPONENT_SOURCE,
                        UI_ENUM_SOURCE, POPUP_ENUM_SOURCE, HOST_STRING_SOURCE,
                        FIXTURE, Path(__file__).resolve(), extracted, component_extract, enum_extract, host_string_extract]
        pinned_files.append(text_boundary)
        pins.update({str(path.resolve(strict=True)): sha(path) for path in pinned_files})
        need(not [p for p, digest in pins.items() if not Path(p).is_file() or sha(Path(p)) != digest],
             "Source/build input pin failed before compilation")

        args = [json.loads(row) for row in BUILD_RESPONSE.read_text(encoding="utf-8").splitlines() if row.strip()]
        host_adapter_args = [i for i, value in enumerate(args) if isinstance(value, str) and value == str(HOST_STRING_SOURCE)]
        need(len(host_adapter_args) == 1, "Unique pinned S073 FixedString host adapter source required")
        args[host_adapter_args[0]] = str(host_string_extract)
        mains = [i for i, value in enumerate(args) if isinstance(value, str) and value.startswith("-main:")]
        outputs = [i for i, value in enumerate(args) if isinstance(value, str) and value.startswith("-out:")]
        need(len(mains) == 1 and len(outputs) == 1, "Unique source-host compiler main and output required")
        executable = output / "UiShellStateOwnerFixture.exe"
        args[mains[0]] = "-main:UiShellStateOwnerFixture"
        args[outputs[0]] = "-out:" + str(executable)
        args.extend([str(extracted), str(STARTUP_HELPER.resolve()), str(component_extract), str(enum_extract),
                     str(text_boundary), str(FIXTURE)])
        response = output / "compile.rsp"
        response.write_text("\n".join(json.dumps(value) for value in args) + "\n", encoding="utf-8")

        tool_paths = [Path(path) for path in pins if Path(path).name in ("dotnet", "csc.dll")]
        dots = [path for path in tool_paths if path.name == "dotnet"]
        compilers = [path for path in tool_paths if path.name == "csc.dll"]
        need(len(dots) == 1 and bool(compilers), "Build-pinned Roslyn and dotnet toolchain required")
        compilers.sort(key=lambda p: tuple(int(part) for part in p.parents[2].name.split(".")))
        start = time.monotonic()
        compile_result = subprocess.run([str(dots[0]), str(compilers[-1]), "@" + str(response)],
                                        capture_output=True, text=True, timeout=180)
        compile_seconds = time.monotonic() - start
        compile_log = output / "compile.log"
        compile_log.write_text(compile_result.stdout + compile_result.stderr, encoding="utf-8")
        receipt["steps"] = {"compile_exit": compile_result.returncode, "compile_seconds": compile_seconds,
                            "compile_log": str(compile_log), "compile_log_sha256": sha(compile_log)}
        need(not [p for p, digest in pins.items() if not Path(p).is_file() or sha(Path(p)) != digest],
             "Pinned inputs changed during compilation")
        need(compile_result.returncode == 0 and executable.is_file(), "Focused owner source-host compile failed")

        runtime = build["runtime_selection"]
        mono = Path(runtime["mono"]).resolve(strict=True)
        need(str(mono) in baseline_pins and sha(mono) == baseline_pins[str(mono)],
             "Build-pinned Mono runtime required")
        mono_config = mono.parent.parent / "etc/mono/config"
        need(mono_config.is_file(), "Build-matched Mono configuration required")
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join([
            str(ROOT / "Library/ScriptAssemblies"), str(Path(runtime["engine"])),
            str(Path(runtime["newtonsoft"]).parent), str(Path(build["placement_build"]).parent)
        ])
        start = time.monotonic()
        run_result = subprocess.run([str(mono), "--config", str(mono_config), "--optimize=-float32", str(executable)],
                                    capture_output=True, text=True, timeout=120, env=env)
        run_seconds = time.monotonic() - start
        run_log = output / "run.log"
        run_log.write_text(run_result.stdout + run_result.stderr, encoding="utf-8")
        pass_marker = "[UiShellStateOwnerFixture] result=Passed source_owner=OriginalOnCreate+EnsureBoundary entity_manager=S073Host gateway_registration=Excluded aria_state=Absent ui_events=Absent gameplay=0 player=0 readiness=0"
        receipt["steps"] = {"compile_exit": compile_result.returncode, "compile_seconds": compile_seconds,
                            "compile_log": str(compile_log), "compile_log_sha256": sha(compile_log),
                            "fixture_exit": run_result.returncode, "fixture_seconds": run_seconds,
                            "run_log": str(run_log), "run_log_sha256": sha(run_log),
                            "pass_marker": pass_marker, "pass_marker_present": pass_marker in run_result.stdout}
        need(not [p for p, digest in pins.items() if not Path(p).is_file() or sha(Path(p)) != digest],
             "Pinned inputs changed during fixture execution")
        need(run_result.returncode == 0 and pass_marker in run_result.stdout,
             "Original owner source-host fixture failed")
        receipt["status"] = "Passed"
    except Exception as error:  # Preserve a machine-readable failure alongside logs already written.
        receipt["failure"] = str(error)
    receipt["base_build"] = {"path": str(ROOT / BUILD_RELATIVE), "sha256": BUILD_SHA256}
    if (output / "UiShellStateSystem.source-host.cs").is_file():
        receipt["owner_source"] = {"path": str(OWNER), "sha256": sha(OWNER),
                                   "fixture_extract": str(output / "UiShellStateSystem.source-host.cs"),
                                   "fixture_extract_sha256": sha(output / "UiShellStateSystem.source-host.cs"),
                                   "only_transformation": "omit the one runtime gateway-registration call"}
    if (output / "compile.rsp").is_file():
        receipt["compiler_response"] = {"path": str(output / "compile.rsp"), "sha256": sha(output / "compile.rsp")}
    pins_file = output / "source-pins.json"
    if (output / "UiShellStateSystem.source-host.cs").is_file():
        pins[str((output / "UiShellStateSystem.source-host.cs").resolve())] = sha(output / "UiShellStateSystem.source-host.cs")
    if (output / "compile.rsp").is_file():
        pins[str((output / "compile.rsp").resolve())] = sha(output / "compile.rsp")
    if COMPONENT_SOURCE.is_file():
        for path in (COMPONENT_SOURCE, UI_ENUM_SOURCE, POPUP_ENUM_SOURCE, HOST_STRING_SOURCE):
            pins[str(path.resolve())] = sha(path)
    write_json(pins_file, pins)
    receipt["source_pins"] = {"path": str(pins_file), "sha256": sha(pins_file), "baseline_pin_count": len(build.get("source_and_reference_pins", {})) if "build" in locals() else 0}
    write_json(output / "receipt.json", receipt)
    print(json.dumps(receipt, separators=(",", ":")))
    return 0 if receipt["status"] == "Passed" else 1


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("output_dir", type=Path)
    parsed = parser.parse_args()
    try:
        raise SystemExit(run(parsed.output_dir))
    except Exception as exc:
        print("[S073UiShellStateOwnerFixture] result=Failed recipe_error=" + str(exc), file=sys.stderr)
        raise SystemExit(1)

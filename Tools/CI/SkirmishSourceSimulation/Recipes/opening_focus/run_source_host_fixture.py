#!/usr/bin/env python3
"""Compile and run the original Skirmish opening-focus owner in a bounded source-host fixture."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True

ROOT = Path(__file__).resolve().parents[5]
FOCUS_PATH = ROOT / "Assets/Game/Scripts/Composition/SkirmishPresentationSystem.cs"
OWNER_PATHS = {
    "environment": ROOT / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishS004Environment.cs",
    "lighting": ROOT / "Assets/Game/Scripts/Composition/SkirmishS004CampaignLighting.cs",
    "view_profile": ROOT / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishMissionViewProfileApplication.cs",
    "selection_config": ROOT / "Assets/Game/Scripts/Systems/SelectionRuntimeConfigStartupSystemHelper.cs",
    "launch_definition": ROOT / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishLaunchDefinitionRecord.cs",
}
FIXTURE_PATH = Path(__file__).with_name("FocusFixtureProgram.cs").resolve()


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def extract_member(path: Path, marker: str) -> str:
    text = path.read_text()
    require(text.count(marker) == 1, f"Expected one exact source member marker in {path}: {marker}")
    start = text.index(marker)
    paren = text.index("(", start)
    depth = 0
    close = -1
    for index in range(paren, len(text)):
        if text[index] == "(":
            depth += 1
        elif text[index] == ")":
            depth -= 1
            if depth == 0:
                close = index + 1
                break
    require(close >= 0, f"Unclosed parameter list in {path}: {marker}")
    arrow = text.find("=>", close)
    brace = text.find("{", close)
    if arrow >= 0 and (brace < 0 or arrow < brace):
        end = text.find(";", arrow)
        require(end >= 0, f"Unclosed expression member in {path}: {marker}")
        return text[start : end + 1]
    require(brace >= 0, f"Missing method body in {path}: {marker}")
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[start : index + 1]
    raise ValueError(f"Unclosed method body in {path}: {marker}")


def extract_class(path: Path, marker: str) -> str:
    text = path.read_text()
    require(text.count(marker) == 1, f"Expected one exact class marker in {path}: {marker}")
    start = text.index(marker)
    brace = text.index("{", start)
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[start : index + 1]
    raise ValueError(f"Unclosed class in {path}: {marker}")


def build_owner_extracts() -> tuple[str, str, str]:
    focus = extract_member(
        FOCUS_PATH,
        "private bool Focus(Entity entity,bool opening,int scenarioIndex,bool enemy,bool s002Session)",
    )
    focus_wrapper = """using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
namespace Game.Composition.SourceHostProbe {
internal sealed class SkirmishPresentationSystemFocusCompileProbe {
    internal EntityManager EntityManager { get; set; }
""" + focus + """
    internal bool InvokeFocus(Entity entity, bool opening, int scenarioIndex, bool enemy, bool s002Session) =>
        Focus(entity, opening, scenarioIndex, enemy, s002Session);
}
}
"""
    require(focus in focus_wrapper, "Focus extraction is not byte-exact.")

    environment = " ".join(
        extract_member(OWNER_PATHS["environment"], marker)
        for marker in (
            "public static bool IsActive(EntityManager em)",
            "public static OperationMapDefinition LoadMapForSession(EntityManager em)",
        )
    )
    lighting = extract_member(
        OWNER_PATHS["lighting"],
        "public static bool Supports(Game.Configs.OperationMapDefinition map)",
    )
    profile = " ".join(
        extract_member(OWNER_PATHS["view_profile"], marker)
        for marker in (
            "public static bool TryResolve(EntityManager em, out Game.Configs.SkirmishMissionViewProfile profile)",
            "internal static bool MatchesSource(EntityManager em,Game.Configs.SkirmishResolvedSetup setup,",
        )
    )
    selection_source = OWNER_PATHS["selection_config"].read_text()
    selection_constants = []
    for name in ("MobileMaxZoomHeightCap", "ForceMobileCameraLimitsEnv"):
        import re

        match = re.search(r"private const (?:float|string) " + name + r"\s*=.*?;", selection_source)
        require(match is not None, f"Missing original selection constant {name}")
        selection_constants.append(match.group(0))
    original_mobile_policy = extract_member(
        OWNER_PATHS["selection_config"], "private static bool ShouldApplyMobileCameraLimits()"
    )
    require(original_mobile_policy.count("Application.isMobilePlatform") == 1,
            "Expected one original Unity platform-property read.")
    mobile_policy = original_mobile_policy.replace(
        "Application.isMobilePlatform", "SourceHostFixturePlatform.IsMobile", 1
    )
    require(mobile_policy.replace("SourceHostFixturePlatform.IsMobile", "Application.isMobilePlatform", 1)
            == original_mobile_policy, "Only the explicit fixture platform boundary may vary.")
    selection = " ".join(selection_constants + [
        mobile_policy,
        extract_member(OWNER_PATHS["selection_config"], "public static float ClampAuthoredFocusHeight(float height)"),
    ])

    owner_methods = """using System;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;
using UnityEngine;
namespace Game.Runtime { public static class SkirmishS004Environment { """ + environment + " } }\n"
    owner_methods += "namespace Game.Composition { public static class SkirmishS004CampaignLighting { " + lighting + " } }\n"
    owner_methods += "namespace Game.Runtime { public static class SkirmishMissionViewProfileApplication { " + profile + " } }\n"
    owner_methods += "namespace Game.Runtime { internal static class SourceHostFixturePlatform { internal static bool IsMobile; } public sealed class SelectionRuntimeConfigStartupSystemHelper { " + selection + " } }\n"
    generic_source = "using Game.Components;\nusing Game.Configs;\nusing Unity.Entities;\nnamespace Game.Runtime { " + extract_class(
        OWNER_PATHS["launch_definition"], "public static class SkirmishGenericMissionSource"
    ) + " }\n"
    boundary = {
        "original_policy_sha256": hashlib.sha256(original_mobile_policy.encode()).hexdigest(),
        "fixture_policy_sha256": hashlib.sha256(mobile_policy.encode()).hexdigest(),
        "original_read_count": original_mobile_policy.count("Application.isMobilePlatform"),
        "fixture_read_count": mobile_policy.count("SourceHostFixturePlatform.IsMobile"),
    }
    return focus_wrapper, owner_methods, generic_source, boundary


def validate_pins(pins: dict[str, str]) -> list[str]:
    failed = []
    for name, expected in pins.items():
        path = Path(name)
        if not path.is_file() or sha(path) != expected:
            failed.append(name)
    return failed


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--build-dir", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    build_dir = args.build_dir.resolve(strict=True)
    output_dir = args.output_dir.resolve()
    require(not output_dir.exists(), "Output directory must be new; preserve prior evidence.")
    output_dir.mkdir(parents=True)
    build_json = build_dir / "build.json"
    build_rsp = build_dir / "compile.rsp"
    require(build_json.is_file() and build_rsp.is_file(), "Current source build receipt and response file required.")
    build = json.loads(build_json.read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
            build.get("compile_exit") == 0, "Passed current source build required.")
    pins: dict[str, str] = build["source_and_reference_pins"]
    extra_sources = [Path(__file__).resolve(), FIXTURE_PATH, FOCUS_PATH, *OWNER_PATHS.values(), build_json, build_rsp]
    extra_pins = {str(path.resolve(strict=True)): sha(path) for path in extra_sources}
    complete_pins = dict(pins)
    complete_pins.update(extra_pins)
    require(not validate_pins(complete_pins), "Current build and recipe source pin verification failed before extraction.")

    focus_text, owner_text, generic_text, boundary = build_owner_extracts()
    focus_file = output_dir / "SkirmishPresentationSystemFocus.extracted.cs"
    owner_file = output_dir / "OriginalFocusOwnerMethods.extracted.cs"
    generic_file = output_dir / "OriginalSkirmishGenericMissionSource.extracted.cs"
    focus_file.write_text(focus_text)
    owner_file.write_text(owner_text)
    generic_file.write_text(generic_text)
    fixture_hash = sha(FIXTURE_PATH)
    recipe_hash = sha(Path(__file__).resolve())

    arguments = [json.loads(line) for line in build_rsp.read_text().splitlines()]
    output = output_dir / "FocusFixture.exe"
    arguments = [
        ("-out:" + str(output)) if item.startswith("-out:") else
        "-main:FocusFixtureProgram" if item.startswith("-main:") else item
        for item in arguments
    ]
    arguments.extend([str(focus_file), str(owner_file), str(generic_file), str(FIXTURE_PATH)])
    response = output_dir / "compile.rsp"
    response.write_text("\n".join(json.dumps(item) for item in arguments) + "\n")

    dotnet = Path(subprocess.check_output(["which", "dotnet"], text=True).strip()).resolve(strict=True)
    compilers = list((dotnet.parent / "sdk").glob("*/Roslyn/bincore/csc.dll"))
    require(bool(compilers), "Installed Roslyn compiler required.")
    compiler = sorted(compilers)[-1]
    start = time.monotonic()
    compile_run = subprocess.run([str(dotnet), str(compiler), "@" + str(response)],
                                 capture_output=True, text=True, timeout=120)
    compile_elapsed = time.monotonic() - start
    compile_log = output_dir / "compile.log"
    compile_log.write_text(compile_run.stdout + compile_run.stderr)
    require(not validate_pins(complete_pins), "Current build and recipe source pins changed during compile.")

    run_log = output_dir / "fixture-run.log"
    run_code = None
    run_elapsed = None
    run_exception = None
    fixture_checks = {"missing_entity_guard": "not-run", "missing_request_guard": "not-run",
                      "duplicate_request_guard": "not-run", "missing_transform_guard": "not-run",
                      "positive_authored_focus_nonmobile": "not-run",
                      "positive_authored_focus_mobile": "not-run",
                      "source_census_base_nonmobile": "not-run",
                      "source_census_base_mobile": "not-run"}
    if compile_run.returncode == 0:
        runtime = build.get("runtime_selection", {}).get("mono")
        require(isinstance(runtime, str) and Path(runtime).is_file(), "Pinned installed Mono runtime path required.")
        start = time.monotonic()
        runtime_selection = build["runtime_selection"]
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join([
            str(ROOT / "Library/ScriptAssemblies"),
            runtime_selection["engine"],
            str(Path(runtime_selection["newtonsoft"]).parent),
            str(Path(build["placement_build"]).parent),
        ])
        run = subprocess.run([runtime, "--optimize=-float32", str(output)], env=env,
                             capture_output=True, text=True, timeout=60)
        run_elapsed = time.monotonic() - start
        run_code = run.returncode
        run_log.write_text(run.stdout + run.stderr)
        if run.returncode != 0:
            run_exception = (run.stdout + run.stderr).strip()
        passed_guards = re.search(r"guards(?:_passed)?=(\d+)", run.stdout + run.stderr)
        guard_count = int(passed_guards.group(1)) if passed_guards else 0
        guard_names = ["missing_entity_guard", "missing_request_guard", "duplicate_request_guard",
                       "missing_transform_guard"]
        for name in guard_names[:guard_count]:
            fixture_checks[name] = "passed"
        failed_stage = re.search(r"stage=([a-z-]+)", run.stdout + run.stderr)
        if run.returncode == 0:
            fixture_checks["positive_authored_focus_nonmobile"] = "passed"
            fixture_checks["positive_authored_focus_mobile"] = "passed"
            fixture_checks["source_census_base_nonmobile"] = "passed"
            fixture_checks["source_census_base_mobile"] = "passed"
        elif failed_stage and failed_stage.group(1) == "positive-authored-focus-nonmobile":
            fixture_checks["positive_authored_focus_nonmobile"] = "failed"
        elif failed_stage and failed_stage.group(1) == "positive-authored-focus-mobile":
            fixture_checks["positive_authored_focus_nonmobile"] = "passed"
            fixture_checks["positive_authored_focus_mobile"] = "failed"
        elif failed_stage and failed_stage.group(1) == "positive-source-census-base-nonmobile":
            fixture_checks["positive_authored_focus_nonmobile"] = "passed"
            fixture_checks["positive_authored_focus_mobile"] = "passed"
            fixture_checks["source_census_base_nonmobile"] = "failed"
        elif failed_stage and failed_stage.group(1) == "positive-source-census-base-mobile":
            fixture_checks["positive_authored_focus_nonmobile"] = "passed"
            fixture_checks["positive_authored_focus_mobile"] = "passed"
            fixture_checks["source_census_base_nonmobile"] = "passed"
            fixture_checks["source_census_base_mobile"] = "failed"
        elif failed_stage and failed_stage.group(1) == "missing-transform":
            fixture_checks["missing_transform_guard"] = "failed"
        require(not validate_pins(complete_pins), "Current build and recipe source pins changed during fixture run.")
    else:
        run_log.write_text("Not run: source-host compile failed.\n")

    all_pins = dict(complete_pins)
    for path in [focus_file, owner_file, generic_file, response, compile_log, run_log]:
        all_pins[str(path.resolve())] = sha(path)
    pin_file = output_dir / "source-pins.json"
    pin_file.write_text(json.dumps(all_pins, indent=2, sort_keys=True) + "\n")
    receipt = {
        "schema_version": 1,
        "kind": "S073OpeningFocusSourceHostFixture",
        "status": "Passed" if compile_run.returncode == 0 and run_code == 0 else "Failed",
        "scope": "Source-host compile and explicit fixture only; not same-world S073, native readiness, or player acceptance.",
        "engine_boundary": {
            "original_read": "UnityEngine.Application.isMobilePlatform",
            "fixture_read": "SourceHostFixturePlatform.IsMobile",
            "caller_supplied_values": [False, True],
            "only_owner_body_substitution": True,
            "native_platform_inferred": False,
            "original_policy_body_sha256": boundary["original_policy_sha256"],
            "fixture_policy_body_sha256": boundary["fixture_policy_sha256"],
            "original_read_count": boundary["original_read_count"],
            "fixture_read_count": boundary["fixture_read_count"],
        },
        "current_build": str(build_json),
        "current_build_sha256": sha(build_json),
        "baseline_pin_count": len(pins),
        "all_baseline_pins_match_before_and_after": True,
        "all_recipe_and_owner_source_pins_match_before_and_after": True,
        "source_pin_count": len(all_pins),
        "source_pins": str(pin_file),
        "source_pins_sha256": sha(pin_file),
        "fixture_source": str(FIXTURE_PATH),
        "fixture_source_sha256": fixture_hash,
        "recipe_source": str(Path(__file__).resolve()),
        "recipe_source_sha256": recipe_hash,
        "compile_exit": compile_run.returncode,
        "compile_seconds": compile_elapsed,
        "compile_log": str(compile_log),
        "compile_log_sha256": sha(compile_log),
        "fixture_exit": run_code,
        "fixture_seconds": run_elapsed,
        "fixture_log": str(run_log),
        "fixture_log_sha256": sha(run_log),
        "fixture_exception": run_exception,
        "fixture_checks": fixture_checks,
        "output": str(output),
        "focus_extract_sha256": sha(focus_file),
        "owner_methods_extract_sha256": sha(owner_file),
        "generic_mission_source_extract_sha256": sha(generic_file),
        "compiler_response_sha256": sha(response),
    }
    receipt_path = output_dir / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt, indent=2))
    return 0 if receipt["status"] == "Passed" else 1


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(f"[S073OpeningFocusFixture] result=Failed recipe_error={error}", file=sys.stderr)
        raise SystemExit(1)

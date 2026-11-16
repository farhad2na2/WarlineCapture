#!/usr/bin/env python3
"""Run the exact legacy squad tray projection over isolated Unity.Entities data."""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

sys.dont_write_bytecode = True

ROOT = Path(__file__).resolve().parents[5]
BUILD = ROOT / "Logs/SkirmishProduction/s073-original-aria-probe-build-20261103-r3/build.json"
BUILD_SHA256 = "97f21831793357edc4b7ca20ba8a412578ef59d5de93d1130a041976d86787d1"
OWNER = ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.CommandHeader.cs"
FIXTURE = Path(__file__).with_name("S073LegacyTrayProjectionFixture.cs").resolve()
PASS_MARKER = "[S073LegacyTrayProjectionFixture] result=Passed path=legacy_ecs_query mixedFactionSlot0Alive=2 deadSkipped=1 outOfRangeSkipped=1 emptyCardsVisible=1 cards=5 ui=stubbed_data_boundary=1 gameplay=0 native=0"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(ok: bool, message: str) -> None:
    if not ok:
        raise RuntimeError(message)


def extract_method(source: str, signature: str) -> str:
    start = source.index(signature)
    opening = source.index("{", start)
    depth = 0
    for i in range(opening, len(source)):
        if source[i] == "{":
            depth += 1
        elif source[i] == "}":
            depth -= 1
            if depth == 0:
                return source[start:i + 1]
    raise RuntimeError("Unbalanced original method: " + signature)


def run(output_path: Path) -> int:
    output = output_path.absolute()
    need(not output.exists(), "Fresh output path required; prior evidence is preserved")
    output.mkdir(parents=True)
    receipt: dict[str, object] = {
        "schema_version": 1,
        "kind": "S073LegacyTrayProjectionSourceHostFixture",
        "status": "Failed",
        "scope": "Exact original non-expanded TryReadMatchHudSquadTray and ExpandedCard method bodies over the pinned source host's Unity.Entities EntityManager/EntityQuery implementation; fixture-only boundary/session routing; no rendered UI, gameplay, native, or readiness claim.",
        "data_boundary_stubs": {"TryGetBoundary": True, "TrySkirmish": True, "expanded_session": "legacy IsLegacy=1; original expanded page probe returns false"},
    }
    try:
        need(BUILD.is_file() and sha(BUILD) == BUILD_SHA256, "Pinned passing S073 r3 source-host build required")
        build = json.loads(BUILD.read_text(encoding="utf-8"))
        need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
             build.get("mission") == "S073" and build.get("runtime_backend") == "LegacyS073OwnerV1" and
             build.get("native_or_player_launched") is False,
             "Passing source-only S073 r3 build receipt required")
        need(Path(build["repository_root"]).resolve(strict=True) == ROOT.resolve(strict=True),
             "Source-host repository root mismatch")
        baseline_pins: dict[str, str] = build["source_and_reference_pins"]
        stale = [path for path, digest in baseline_pins.items() if not Path(path).is_file() or sha(Path(path)) != digest]
        need(not stale, "Pinned source/build input changed: " + ", ".join(stale[:8]))

        owner_text = OWNER.read_text(encoding="utf-8")
        tray_method = extract_method(owner_text, "public static bool TryReadMatchHudSquadTray(")
        card_method = extract_method(owner_text, "private static UiMatchHudSquadTrayCardModel ExpandedCard(")
        extracted = output / "LegacyTrayProjectionHost.extracted.cs"
        host = """using System;\nusing Game.Components;\nusing Game.Configs;\nusing Game.Runtime;\nusing Game.UI.Contracts;\nusing Game.UI.Shell.Contracts.Ecs;\nusing Unity.Collections;\nusing Unity.Entities;\nnamespace Game.Configs { internal static class SourceHostTextStub { internal static string Get(string key,string fallback) => fallback; internal static string Format(string key,string fallback,params object[] args) => string.Format(fallback,args); } internal static class GameText { internal static string Get(string key,string fallback) => SourceHostTextStub.Get(key,fallback); internal static string Format(string key,string fallback,params object[] args) => SourceHostTextStub.Format(key,fallback,args); } internal static class GameLocalization { internal static bool IsRightToLeft => false; } internal static class SkirmishSetupBriefing { internal static string RoleLabel(Game.Skirmish.Contracts.SkirmishRoleKind role,bool rtl) => role.ToString(); } }\nnamespace Game.UI.Shell.Ecs {\ninternal static class LegacyTrayProjectionHost {\nstatic EntityManager em; static Entity boundary; static Entity session;\ninternal static void Bind(EntityManager manager, Entity uiBoundary, Entity skirmishSession) { em=manager; boundary=uiBoundary; session=skirmishSession; }\nprivate static bool TryGetBoundary(out EntityManager manager, out Entity entity) { manager=em; entity=boundary; return manager != null && manager.Exists(entity); }\nprivate static bool TrySkirmish(out EntityManager manager, out Entity entity, out SkirmishMatchState match) { manager=em; entity=session; match=default; return manager != null && manager.Exists(entity); }\n""" + tray_method + "\n" + card_method + "\n}\n}\n"
        extracted.write_text(host, encoding="utf-8")
        pins = {str(OWNER): sha(OWNER), str(FIXTURE): sha(FIXTURE), str(Path(__file__).resolve()): sha(Path(__file__).resolve()),
                str(BUILD): sha(BUILD), **baseline_pins}
        response_info = build["compiler_response"]
        response_path = Path(response_info["path"]).resolve(strict=True)
        need(sha(response_path) == response_info["sha256"], "Pinned source-host compiler response changed")
        args = [json.loads(row) for row in response_path.read_text(encoding="utf-8").splitlines() if row.strip()]
        mains = [i for i, value in enumerate(args) if isinstance(value, str) and value.startswith("-main:")]
        outs = [i for i, value in enumerate(args) if isinstance(value, str) and value.startswith("-out:")]
        need(len(mains) == 1 and len(outs) == 1, "Unique source-host entry/output required")
        executable = output / "S073LegacyTrayProjectionFixture.exe"
        args[mains[0]] = "-main:S073LegacyTrayProjectionFixture"
        args[outs[0]] = "-out:" + str(executable)
        args.extend([str(extracted), str(FIXTURE)])
        compile_response = output / "compile.rsp"
        compile_response.write_text("\n".join(json.dumps(value) for value in args) + "\n", encoding="utf-8")

        pin_rows = [Path(path) for path in baseline_pins]
        dots = [path for path in pin_rows if path.name == "dotnet"]
        compilers = [path for path in pin_rows if path.name == "csc.dll"]
        need(len(dots) == 1 and bool(compilers), "Pinned Roslyn and dotnet toolchain required")
        compilers.sort(key=lambda p: tuple(int(part) for part in p.parents[2].name.split(".")))
        compile_result = subprocess.run([str(dots[0]), str(compilers[-1]), "@" + str(compile_response)],
                                        capture_output=True, text=True, timeout=180)
        compile_log = output / "compile.log"
        compile_log.write_text(compile_result.stdout + compile_result.stderr, encoding="utf-8")
        receipt["compile"] = {"exit": compile_result.returncode, "log": str(compile_log), "log_sha256": sha(compile_log)}
        need(compile_result.returncode == 0 and executable.is_file(), "Exact legacy projection source-host compile failed; inspect compile.log")

        runtime = build["runtime_selection"]
        mono = Path(runtime["mono"]).resolve(strict=True)
        need(str(mono) in baseline_pins and sha(mono) == baseline_pins[str(mono)], "Build-pinned Mono required")
        mono_config = mono.parent.parent / "etc/mono/config"
        need(mono_config.is_file(), "Build-matched Mono config required")
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join([str(ROOT / "Library/ScriptAssemblies"), str(Path(runtime["engine"])),
                                             str(Path(runtime["newtonsoft"]).parent), str(Path(build["placement_build"]).parent)])
        result = subprocess.run([str(mono), "--config", str(mono_config), "--optimize=-float32", str(executable)],
                                capture_output=True, text=True, timeout=120, env=env)
        run_log = output / "run.log"
        run_log.write_text(result.stdout + result.stderr, encoding="utf-8")
        receipt["execution"] = {"exit": result.returncode, "log": str(run_log), "log_sha256": sha(run_log),
                                "pass_marker": PASS_MARKER, "pass_marker_present": PASS_MARKER in result.stdout}
        need(not [path for path, digest in pins.items() if not Path(path).is_file() or sha(Path(path)) != digest],
             "Pinned source/build input changed during fixture")
        need(result.returncode == 0 and PASS_MARKER in result.stdout, "Projection fixture failed; inspect run.log")
        receipt["status"] = "Passed"
        receipt["base_build"] = {"path": str(BUILD), "sha256": BUILD_SHA256}
        receipt["source_owner"] = {"path": str(OWNER), "sha256": sha(OWNER),
                                    "exact_extracted_members": ["TryReadMatchHudSquadTray", "ExpandedCard"],
                                    "extract": str(extracted), "extract_sha256": sha(extracted)}
        receipt["fixture"] = {"path": str(FIXTURE), "sha256": sha(FIXTURE), "checks": [
            "mixed faction entities aggregate in the same legacy slots", "zero-health entity skipped",
            "out-of-range slot skipped", "empty legacy cards remain visible", "five-card limit"]}
        receipt["recipe"] = {"path": str(Path(__file__).resolve()), "sha256": sha(Path(__file__).resolve())}
        receipt["compiler_response"] = {"path": str(compile_response), "sha256": sha(compile_response)}
        receipt["source_host_ecs_adapter"] = True
        receipt["faction_filter_claim"] = "none; exact original query contains no faction/session filter"
        receipt["gameplay_executed"] = False
        receipt["native_or_player_claim"] = False
        receipt["ui_visibility_claim"] = False
        receipt["mission_readiness_claim"] = False
        receipt["unity_launched"] = False
    except Exception as error:
        receipt["failure"] = str(error)
    receipt_path = output / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(receipt, separators=(",", ":")))
    return 0 if receipt["status"] == "Passed" else 1


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("usage: run_source_host_fixture.py <fresh-output-dir>")
    raise SystemExit(run(Path(sys.argv[1])))

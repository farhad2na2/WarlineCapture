#!/usr/bin/env python3
"""Compile current original ARIA plan owner partials against a pinned S073 source build."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time


ROOT = Path(__file__).resolve().parents[5]
SOURCES = [
    ROOT / "Assets/Game/Scripts/UI/Contracts/AriaSkirmishContracts.cs",
    ROOT / "Assets/Game/Scripts/UI/Contracts/AriaPlayContracts.cs",
    ROOT / "Assets/Game/Scripts/UI/Contracts/AriaObjectiveSupportContracts.cs",
    ROOT / "Assets/Game/Scripts/UI/Contracts/AriaPublicScanContracts.cs",
    ROOT / "Assets/Game/Scripts/UI/Contracts/AriaPublicBoardingContracts.cs",
    ROOT / "Assets/Game/Scripts/UI/Contracts/AriaAircraftServiceContracts.cs",
    ROOT / "Assets/Game/Scripts/UI/Contracts/AriaOffensiveAirContracts.cs",
    ROOT / "Assets/Game/Scripts/UI/Contracts/AriaObjectiveTouchContracts.cs",
    ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/Contracts/AriaSkirmishComponents.cs",
    ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/Contracts/AriaPlayComponents.cs",
    ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaS002NativeAdapter.cs",
    ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/aria_s073_original_step/S073OriginalAriaStepFixture.cs",
]

PLANNER_PARTS = [
    ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.cs",
    ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.Objective.cs",
    ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.ObjectiveSupport.cs",
    ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.PublicScan.cs",
    ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.PublicBoarding.cs",
    ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.AircraftService.cs",
    ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.OffensiveAir.cs",
]


def planner_members(path):
    text = path.read_text(encoding="utf-8")
    marker = "public partial struct AriaSkirmishPlanSystem"
    start = text.index(marker)
    opening = text.index("{", start)
    closing = text.rfind("\n    }\n}")
    require(closing > opening, "Original planner partial class boundary not found: " + str(path))
    members = text[opening + 1:closing]
    if path.name == "AriaSkirmishPlanSystem.cs":
        on_update = members.index("public void OnUpdate(")
        step = members.index("public static void Step(", on_update)
        members = members[step:]
    return members


def emit_data_only_planner(output):
    parts = [planner_members(path) for path in PLANNER_PARTS]
    source = """extern alias ActualCore;\nusing System;\nusing Game.Simulation;\nusing Game.UI.Contracts;\nusing Game.UI.Shell.Contracts.Ecs;\nusing Unity.Entities;\nnamespace Game.UI.Shell.Ecs {\n    public partial struct AriaSkirmishPlanSystem {\n""" + "\n".join(parts) + "\n    }\n}\n"
    source = source.replace("UnityEngine.", "ActualCore::UnityEngine.")
    path = output / "AriaSkirmishPlanSystem.DataOnly.extracted.cs"
    path.write_text(source)
    return path


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(ok, msg):
    if not ok:
        raise RuntimeError(msg)


def run(build_path, output_path, fixture=None, scenario="S073"):
    require(scenario in ("S073", "S004", "S003"), "Unsupported original planner fixture")
    sources = SOURCES[:-1] + [Path(fixture).resolve(strict=True) if fixture else SOURCES[-1]]
    fixture_name = scenario + "OriginalAriaStepFixture"
    build_path = Path(build_path).resolve(strict=True)
    output = Path(output_path).absolute()
    require(not output.exists(), "Fresh isolated output directory required")
    output.mkdir(parents=True)
    raw = build_path.read_bytes()
    build = json.loads(raw)
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed",
            "Passing checked S073 source build required")
    require(build.get("runtime_backend") in ("LegacyGroundS002V1", "LegacyS073OwnerV1") or
            build.get("profile", {}).get("mission") == "S002",
            "Expected the current source simulation build receipt")
    pins = build.get("source_and_reference_pins", {})
    changed = [path for path, expected in pins.items() if sha(path) != expected]
    if changed:
        receipt = {
            "schema_version": 1, "kind": "Aria" + scenario + "OriginalStepCompileProbe", "status": "Failed",
            "scope": "Compile not attempted because the source-build pin audit found stale inputs.",
            "base_build": str(build_path), "base_build_sha256": hashlib.sha256(raw).hexdigest(),
            "changed_build_pins": changed, "compile_attempted": False,
            "output": str(output), "gameplay_run": False,
        }
        (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
        print(json.dumps(receipt, indent=2))
        return 1
    response = Path(build["compiler_response"]["path"]).resolve(strict=True)
    require(sha(response) == build["compiler_response"]["sha256"], "Compiler response changed")
    args = [json.loads(line) for line in response.read_text().splitlines() if line.strip()]
    mains = [i for i, value in enumerate(args) if value == "-main:SourceSimulationProgram"]
    outs = [i for i, value in enumerate(args) if value.startswith("-out:")]
    require(len(mains) == 1 and len(outs) == 1, "Expected one source host entry/output")
    args[mains[0]] = "-main:" + fixture_name
    executable = output / ("Aria" + scenario + "OriginalStepFixture.exe")
    args[outs[0]] = "-out:" + str(executable)
    core = next((value.split("=", 1)[1] for value in args if value.startswith("-r:ActualCore=")), None)
    require(core is not None, "Pinned Unity CoreModule reference missing from source build")
    extracted = emit_data_only_planner(output)
    args.append(str(extracted))
    for source in sources:
        require(source.is_file(), "Required original owner source missing: " + str(source))
        original = source.read_text(encoding="utf-8")
        if "UnityEngine." in original:
            copied = output / (source.stem + ".extern-alias.extracted.cs")
            copied.write_text("extern alias ActualCore;\n" + original.replace("UnityEngine.", "ActualCore::UnityEngine."))
            args.append(str(copied))
        else:
            args.append(str(source))
    rsp = output / "compile.rsp"
    rsp.write_text("\n".join(json.dumps(value) for value in args) + "\n")
    pinned = {str(p): sha(p) for p in sources + PLANNER_PARTS}
    recipe_path = Path(__file__).resolve()
    (output / "source-pins.json").write_text(json.dumps({
        "kind": "Aria" + scenario + "OriginalStepCompileProbeSources", "source_pins": pinned,
        "recipe": str(recipe_path), "recipe_sha256": sha(recipe_path),
        "base_build": str(build_path), "base_build_sha256": hashlib.sha256(raw).hexdigest(),
        "extraction_seam": "Original static Step and actual planner partial helper member bodies; excludes only ECS OnUpdate/scheduler attribute shell.",
        "namespace_qualification": "UnityEngine references in extracted copies are qualified through the build's existing ActualCore extern alias; method bodies and policy decisions are otherwise original source text.",
        "compile_only": False, "source_only_fixture_run": True,
        "gameplay_run": False, "unity_launched": False,
    }, indent=2) + "\n")
    dotnet = next((p for p in pins if Path(p).name == "dotnet"), None)
    compiler = next((p for p in pins if Path(p).name == "csc.dll"), None)
    require(dotnet and compiler, "Pinned Roslyn toolchain missing")
    started = time.monotonic()
    result = subprocess.run([dotnet, compiler, "@" + str(rsp)], capture_output=True, text=True, timeout=180)
    (output / "compile.log").write_text(result.stdout + result.stderr)
    run_result = None
    if result.returncode == 0:
        runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
        mono_helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
        mono_native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
        require(mono_helper.is_file() and mono_native.is_file(), "Pinned Mono helper libraries required")
        mono_config = output / "mono-config.xml"
        dllmaps = "".join("<dllmap dll=\"" + name + "\" target=\"" + str(mono_helper) + "\" os=\"!windows\"/>"
                           for name in ["MonoPosixHelper"]) + "".join(
            "<dllmap dll=\"" + name + "\" target=\"" + str(mono_native) + "\" os=\"!windows\"/>"
            for name in ["System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"])
        mono_config.write_text("<configuration>" + dllmaps + "</configuration>\n")
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join([
            str(ROOT / "Library/ScriptAssemblies"), str(runtime["engine"]),
            str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)
        ])
        run_result = subprocess.run([str(runtime["mono"]), "--config", str(mono_config), str(executable)],
                                    env=env, capture_output=True, text=True, timeout=60)
        (output / "run.log").write_text(run_result.stdout + run_result.stderr)
    for path, expected in pins.items():
        require(sha(path) == expected, "Build pin changed during compile: " + path)
    for path, expected in pinned.items():
        require(sha(path) == expected, "Original ARIA source changed during compile: " + path)
    marker = "[" + fixture_name + "] result=Passed"
    run_log = "" if run_result is None else run_result.stdout + run_result.stderr
    status = "Passed" if result.returncode == 0 and run_result is not None and run_result.returncode == 0 and marker in run_log else "Failed"
    receipt = {
        "schema_version": 1, "kind": "Aria" + scenario + "OriginalStepCompileProbe", "status": status,
        "scope": "Compile and run source-only fixture over original data-only AriaSkirmishPlanSystem.Step and partial helpers plus exact AriaS002NativeAdapter.CanHandle/Step source; excludes ECS OnUpdate/scheduler shell; no Unity or gameplay execution.",
        "base_build": str(build_path), "base_build_sha256": hashlib.sha256(raw).hexdigest(),
        "recipe": str(recipe_path), "recipe_sha256": sha(recipe_path),
        "extraction_seam": "Original static Step and actual planner partial helper member bodies; excludes only ECS OnUpdate/scheduler attribute shell.",
        "namespace_qualification": "UnityEngine references in extracted copies are qualified through the build's existing ActualCore extern alias; method bodies and policy decisions are otherwise original source text.",
        "source_pins": pinned, "response": str(rsp), "response_sha256": sha(rsp),
        "compile_exit": result.returncode, "run_exit": None if run_result is None else run_result.returncode,
        "pass_marker_present": marker in run_log, "elapsed_seconds": time.monotonic() - started,
        "output": str(output), "gameplay_run": False, "unity_launched": False,
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt, indent=2))
    return 0 if status == "Passed" else 1


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: run_compile_probe.py <build.json> <fresh-output-dir>")
    raise SystemExit(run(sys.argv[1], sys.argv[2]))

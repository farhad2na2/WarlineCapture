#!/usr/bin/env python3
"""Compile and run focused UnitAttack projection parity controls against a passed source host."""
import argparse
import hashlib
import json
import os
import re
import subprocess
from pathlib import Path

REPO = Path("/Volumes/T7/Projects/WarlineCapture")
AUTHORING = Path("Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs")
CONFIG = Path("Assets/Game/Scripts/Configs/GameplayConfigModels.cs")
COMPONENTS = Path("Assets/Game/Scripts/Components/UnitCombatComponents.cs")
HELPER = Path("Assets/Game/Scripts/Components/UnitAttackAuthoringProjection.cs")
MANIFESTS = [Path("Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json"),
             Path("Tools/CI/SkirmishSourceSimulation/source_manifest.s003.plan.json")]
CONTROL = Path("Tools/CI/SkirmishSourceSimulation/Tests/UnitAttackAuthoringProjectionControl.cs")
PASS = "[UnitAttackAuthoringProjectionControl] result=Passed floatPatterns=14 nonfinite=NaN,+Inf,-Inf negativeControls=0 scope=UnitAttackValues"


def need(ok, message):
    if not ok:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def compact(path):
    return re.sub(r"\s+", "", Path(path).read_text(encoding="utf-8"))


def verify_current_sources(repo):
    bake = compact(repo / AUTHORING)
    expected_call = (
        "UnitAttackAuthoringProjection.FromConfiguredValues(authoring.ConfiguredAttackRange,"
        "authoring.ConfiguredAttackCooldownSeconds,authoring.ConfiguredAttackDamage,"
        "traceColor.r,traceColor.g,traceColor.b,traceColor.a,traceWidth,traceScrollSpeed,"
        "traceDashDensity,traceVisibleSeconds,authoring.config!=null?"
        "authoring.config.AttackTracerEveryNthShot:authoring.attackTracerEveryNthShot)"
    )
    need(bake.count(expected_call) == 1, "Baker projection call or getter evaluation order drifted")
    source = (repo / AUTHORING).read_text(encoding="utf-8")
    for expression in (
        "authoring.config != null ? authoring.config.AttackTraceColor : authoring.attackTraceColor",
        "authoring.config != null ? authoring.config.AttackTraceWidth : authoring.attackTraceWidth",
        "authoring.config != null ? authoring.config.AttackTraceScrollSpeed : authoring.attackTraceScrollSpeed",
        "authoring.config != null ? authoring.config.AttackTraceDashDensity : authoring.attackTraceDashDensity",
        "authoring.config != null ? authoring.config.AttackTraceVisibleSeconds : authoring.attackTraceVisibleSeconds",
    ):
        need(source.count(expression) == 1, "Baker config-or-fallback trace source changed: " + expression)
    config = (repo / CONFIG).read_text(encoding="utf-8")
    for expression in (
        "public float AttackTraceWidth => attackTraceWidth;",
        "public float AttackTraceScrollSpeed => attackTraceScrollSpeed;",
        "public float AttackTraceDashDensity => attackTraceDashDensity;",
        "public float AttackTraceVisibleSeconds => attackTraceVisibleSeconds;",
        "public Color AttackTraceColor => attackTraceColor;",
        "public int AttackTracerEveryNthShot => Mathf.Max(1, attackTracerEveryNthShot);",
    ):
        need(expression in config, "Current config getter changed: " + expression)
    for expression in (
        "public float ConfiguredAttackRange => Mathf.Max(0f, config != null ? config.AttackRange : attackRange);",
        "public float ConfiguredAttackCooldownSeconds => Mathf.Max(0.01f, config != null ? config.AttackCooldownSeconds : attackCooldownSeconds);",
        "public int ConfiguredAttackDamage => Mathf.Max(0, config != null ? config.AttackDamage : attackDamage);",
    ):
        need(expression in source, "Current configured combat getter changed: " + expression)
    helper = (repo / HELPER).read_text(encoding="utf-8")
    for field, floor in (("TraceWidth", "0.01f"), ("TraceScrollSpeed", "0.1f"),
                         ("TraceDashDensity", "1f"), ("TraceVisibleSeconds", "0.01f"),
                         ("TracerEveryNthShot", "1")):
        need(f"{field} = math.max({floor}," in helper,
             "Projection must retain actual Unity.Mathematics floor for " + field)
    need("using Unity.Mathematics;" in helper and "public UnitAttack" not in helper,
         "Projection must use Unity.Mathematics and return UnitAttack value data only")
    return {str(repo / item): sha(repo / item) for item in (AUTHORING, CONFIG, COMPONENTS, HELPER, CONTROL, *MANIFESTS)}


def run(build_dir, output):
    repo = REPO.resolve(strict=True)
    build_dir, output = build_dir.resolve(strict=True), output.resolve()
    need(build_dir.is_relative_to(repo) and output.is_relative_to(repo) and not output.exists(),
         "explicit passed build and fresh output must remain within repository")
    input_pins = verify_current_sources(repo)
    build_path = build_dir / "build.json"
    response = build_dir / "compile.rsp"
    build = json.loads(build_path.read_text(encoding="utf-8"))
    pins = build.get("source_and_reference_pins", {})
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("compile_exit") == 0 and pins, "passed fresh source-host build with exact pin map required")
    drift = [p for p, digest in pins.items() if not Path(p).is_file() or sha(p) != digest]
    need(not drift, "source build pin drift: " + ", ".join(drift[:4]))
    helper_path = repo / HELPER
    need(pins.get(str(helper_path)) == sha(helper_path) and sum(
        1 for line in response.read_text(encoding="utf-8").splitlines() if str(helper_path) in line) == 1,
        "passed build must compile and pin UnitAttack projection exactly once")
    for rel in (AUTHORING, COMPONENTS, HELPER):
        path = repo / rel
        need(pins.get(str(path)) == sha(path), "passed build pin missing/current source drift: " + str(rel))

    runtime = build.get("runtime_selection", {})
    compiler = Path(next(p for p in pins if p.endswith("/Roslyn/bincore/csc.dll")))
    dotnet = Path(next(p for p in pins if Path(p).name == "dotnet"))
    mono, engine, newtonsoft = Path(runtime.get("mono", "")), Path(runtime.get("engine", "")), Path(runtime.get("newtonsoft", ""))
    need(all(p.is_file() for p in (compiler, dotnet, mono, newtonsoft)) and engine.is_dir(),
         "pinned compiler/runtime dependencies missing")
    output.mkdir(parents=True)
    exe = output / "UnitAttackAuthoringProjectionControl.exe"
    rewritten, out_seen, main_seen = [], False, False
    for line in response.read_text(encoding="utf-8").splitlines():
        raw = line.strip().strip('"')
        if raw.startswith("-out:"):
            rewritten.append('"-out:' + str(exe) + '"'); out_seen = True
        elif raw == "-main:SourceSimulationProgram":
            rewritten.append('"-main:UnitAttackAuthoringProjectionControl"'); main_seen = True
        else:
            rewritten.append(line)
    need(out_seen and main_seen, "source response output/main switches missing")
    rewritten.append('"' + str(repo / CONTROL) + '"')
    control_response = output / "control.rsp"
    control_response.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
    compile_result = subprocess.run([str(dotnet), str(compiler), "@" + str(control_response)], cwd=repo,
                                    text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (output / "compile.log").write_text(compile_result.stdout, encoding="utf-8")
    need(compile_result.returncode == 0 and exe.is_file(), "focused .NET parity control compile failed; see compile.log")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(build_dir / "placement"), str(engine), str(newtonsoft.parent),
                                        str(repo / "Library/ScriptAssemblies"), str(output)))
    execution = subprocess.run([str(mono), str(exe)], cwd=repo, env=env,
                               text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (output / "run.log").write_text(execution.stdout, encoding="utf-8")
    need(execution.returncode == 0 and PASS in execution.stdout.splitlines(),
         "focused .NET parity control failed or pass marker missing; see run.log")
    post_drift = [p for p, digest in pins.items() if not Path(p).is_file() or sha(p) != digest]
    need(not post_drift, "source build pin drift after control: " + ", ".join(post_drift[:4]))
    receipt = {"kind": "UnitAttackAuthoringProjectionControlReceipt", "status": "Passed",
               "source_build": str(build_path), "source_build_sha256": sha(build_path),
               "source_build_executable_sha256": sha(build_dir / "SourceSimulation.exe"),
               "source_closure_count": len(pins), "source_inputs": input_pins,
               "scope": "Current Baker UnitAttack value projection with compiled Unity.Mathematics float edge parity; no VFX references or offsets.",
               "compile_exit": compile_result.returncode, "run_exit": execution.returncode, "pass_marker": PASS}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    return receipt


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--build-dir", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        print(json.dumps(run(args.build_dir, args.output), indent=2))
        return 0
    except Exception as exc:
        print(f"[UnitAttackAuthoringProjectionControl] result=Failed error={exc}")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env python3
"""Compile a scalar Alt04 component control over an exact passed source-build closure."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path

REPO = Path("/Volumes/T7/Projects/WarlineCapture")
PROBE = Path("Tools/CI/SkirmishSourceSimulation/Recipes/unit_move_projection/probe_alt04_core_components.py")
BUILD = Path("T Logs/SkirmishProduction/shared-gameplay-vertical-slice-20261103-T11/source-build-s002")
PASS = "[Alt04CoreComponentControl] result=Passed components=UnitCombat,UnitAttackScalar,UnitHealth negativeControls=3 faction=Unsupported surfaceHeight=Unsupported prefabClosure=Unsupported"


def require(ok, message):
    if not ok:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def control_source():
    return r'''using System;
using Game.Components;

internal static class Alt04CoreComponentControl
{
    private static int Main()
    {
        // Values are bound by the adjacent source-expression receipt to the current
        // Alt04 config GUID and Baker/getter source. This control constructs only
        // the public ECS data structs, never an entity or imported prefab.
        const int aggro = 12;
        const float chaseBreak = 110f;
        const byte canAttack = 1;
        const byte autoEngage = 1;
        const float range = 85f;
        const float cooldown = 0.82f;
        const int damage = 16;
        const int maxHealth = 125;

        UnitCombat combat = new UnitCombat
        {
            AggroRangeCells = Math.Max(0, aggro),
            ChaseBreakDistance = Math.Max(0f, chaseBreak),
            CanAttack = canAttack,
            AutoEngage = autoEngage
        };
        UnitAttack attack = new UnitAttack
        {
            Range = Math.Max(0f, range),
            CooldownSeconds = Math.Max(0.01f, cooldown),
            Damage = Math.Max(0, damage)
        };
        int hp = Math.Max(1, maxHealth);
        UnitHealth health = new UnitHealth { Current = hp, Max = hp };

        Require(combat.AggroRangeCells == 12 && combat.ChaseBreakDistance == 110f &&
                combat.CanAttack == 1 && combat.AutoEngage == 1, "UnitCombat fields");
        Require(attack.Range == 85f && attack.CooldownSeconds == 0.82f && attack.Damage == 16,
                "UnitAttack scalar fields");
        Require(health.Current == 125 && health.Max == 125, "UnitHealth fields");

        // Negative controls: each incorrect source input must be observable in the
        // actual ECS struct, and the assertion helper must reject that mismatch.
        RejectMismatch(combat.AggroRangeCells != 13, "aggro mismatch rejected");
        RejectMismatch(attack.Range != 86f, "attack range mismatch rejected");
        RejectMismatch(health.Max != 126, "max health mismatch rejected");
        Console.WriteLine("''' + PASS + r'''");
        return 0;
    }

    private static void RejectMismatch(bool mismatch, string label)
    {
        bool rejected = false;
        try { Require(!mismatch, label); }
        catch (InvalidOperationException) { rejected = true; }
        Require(rejected, label + " negative control");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("assertion failed: " + label);
    }
}
'''


def run(output):
    repo = REPO.resolve(strict=True)
    output = output.resolve()
    require(output.is_relative_to(repo) and not output.exists(), "fresh output directory inside repository required")
    build_dir = repo / BUILD
    build = json.loads((build_dir / "build.json").read_text(encoding="utf-8"))
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
            build.get("compile_exit") == 0, "passed source build T11 required")
    pins = build.get("source_and_reference_pins", {})
    require(len(pins) == 3896, "unexpected T11 source/reference closure size")
    drifts = [path for path, digest in pins.items() if not Path(path).is_file() or sha(Path(path)) != digest]
    require(not drifts, "T11 source closure has current drift: " + ", ".join(drifts[:5]))
    probe = repo / PROBE
    require(probe.is_file(), "source-expression probe is missing")
    probe_receipt = repo / "T Logs/SkirmishProduction/alt04-core-component-feasibility-20261103-T3/receipt.json"
    receipt = json.loads(probe_receipt.read_text(encoding="utf-8"))
    require(receipt.get("kind") == "Alt04CurrentCoreComponentSourceFeasibilityProbe" and
            receipt.get("status") == "PassedSourceExpressionPreflight" and
            receipt.get("negative_controls", {}).get("duplicate_authoring_getter_rejected") is True and
            receipt.get("negative_controls", {}).get("changed_aggro_clamp_rejected") is True,
            "current source-expression preflight receipt required")
    for path, digest in receipt.get("source_pins", {}).items():
        absolute = repo / path
        require(absolute.is_file() and sha(absolute) == digest,
                "source-expression receipt pin is stale: " + path)
        if path.startswith("Assets/Game/Scripts/"):
            require(pins.get(str(absolute)) == digest,
                    "T11 source closure and source-expression receipt must agree: " + path)

    rsp = build_dir / "compile.rsp"
    compiler_path = next((Path(p) for p in pins if p.endswith("/Roslyn/bincore/csc.dll")), None)
    dotnet_path = next((Path(p) for p in pins if Path(p).name == "dotnet" and Path(p).is_file()), None)
    require(compiler_path and dotnet_path and sha(compiler_path) == pins[str(compiler_path)] and
            sha(dotnet_path) == pins[str(dotnet_path)], "T11-pinned Roslyn and dotnet required")
    output.mkdir(parents=True)
    source_file = output / "Alt04CoreComponentControl.cs"
    exe = output / "Alt04CoreComponentControl.exe"
    source_file.write_text(control_source(), encoding="utf-8")
    rsp_lines = rsp.read_text(encoding="utf-8").splitlines()
    cleaned = [line.strip().strip('"') for line in rsp_lines if line.strip()]
    require(any(a.startswith("-out:") for a in cleaned) and any(a == "-main:SourceSimulationProgram" for a in cleaned),
            "unexpected T11 compiler response format")
    compiled_lines = []
    for line in rsp_lines:
        raw = line.strip().strip('"')
        if raw.startswith("-out:"):
            compiled_lines.append('"-out:' + str(exe) + '"')
        elif raw == "-main:SourceSimulationProgram":
            compiled_lines.append('"-main:Alt04CoreComponentControl"')
        else:
            compiled_lines.append(line)
    compiled_lines.append('"' + str(source_file) + '"')
    control_rsp = output / "control.rsp"
    control_rsp.write_text("\n".join(compiled_lines) + "\n", encoding="utf-8")
    compile_result = subprocess.run([str(dotnet_path), str(compiler_path), "@" + str(control_rsp)], cwd=repo,
                                    text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (output / "compile.log").write_text(compile_result.stdout, encoding="utf-8")
    require(compile_result.returncode == 0 and exe.is_file(),
            "compiled control failed; inspect retained compile.log")
    runtime = build.get("runtime_selection", {})
    mono = Path(runtime.get("mono", ""))
    engine = Path(runtime.get("engine", ""))
    newtonsoft = Path(runtime.get("newtonsoft", ""))
    require(mono.is_file() and engine.is_dir() and newtonsoft.is_file() and
            pins.get(str(mono)) == sha(mono), "T11-pinned Mono/Unity managed runtime required")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(build_dir / "placement"), str(engine), str(newtonsoft.parent),
                                        str(repo / "Library/ScriptAssemblies"), str(output)))
    execution = subprocess.run([str(mono), str(exe)], cwd=repo, env=env, text=True,
                               stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (output / "run.log").write_text(execution.stdout, encoding="utf-8")
    require(execution.returncode == 0 and PASS in execution.stdout,
            "compiled control failed or required pass marker absent; inspect retained run.log")
    result = {
        "kind": "Alt04CoreComponentCompiledControlReceipt",
        "status": "Passed",
        "source_build": str(build_dir / "build.json"),
        "source_build_sha256": sha(build_dir / "build.json"),
        "source_build_executable_sha256": sha(build_dir / "SourceSimulation.exe"),
        "compiler_response_sha256": sha(rsp),
        "source_closure_count": len(pins),
        "control_source_sha256": sha(source_file),
        "control_executable_sha256": sha(exe),
        "runtime_mono_sha256": sha(mono),
        "scope": "Compiled actual UnitCombat, UnitAttack scalar, UnitHealth ECS structs with current-source-bound Alt04 values; no entity/import/spawn claim.",
        "negative_controls": ["wrong aggro rejected", "wrong attack range rejected", "wrong max health rejected"],
        "unsupported": ["Faction model-child ownership branch", "evaluated UnitSurfaceComponent height", "attack trace data", "VFX references", "complete prefab closure"],
        "pass_marker": PASS,
        "compile_exit": compile_result.returncode,
        "run_exit": execution.returncode,
    }
    (output / "receipt.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    try:
        print(json.dumps(run(args.output), indent=2))
        return 0
    except Exception as exc:
        print(f"[Alt04CoreComponentControl] result=Failed error={exc}")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

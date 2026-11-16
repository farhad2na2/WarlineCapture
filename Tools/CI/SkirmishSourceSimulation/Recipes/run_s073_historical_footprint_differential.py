#!/usr/bin/env python3
"""Compare current footprint owner against original HEAD methods in a fixture."""
import argparse
import hashlib
import json
import os
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
OLD_PRIMARY = "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs"
OLD_PARTIAL = "Assets/Game/Scripts/Authorings/UnitGridAuthoring.BakerVisuals.cs"
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S073UnitFootprintHistoricalDifferentialControl.cs"
PRESERVED = ROOT / "Logs/SkirmishProduction/s073-captured-source-preservation-20261101-r1/source" / OLD_PRIMARY
MARKER = "[S073UnitFootprintHistoricalDifferentialControl] result=Passed cases=12800 fixtureOnly=1 sceneParity=0 readiness=0"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def git(*args):
    return subprocess.run(["rtk", "proxy", "git", *args], cwd=ROOT, check=True, capture_output=True).stdout


def method(source, signature):
    if source.count(signature) != 1:
        raise ValueError("Unique original method signature required")
    start = source.index(signature)
    opening = source.index("{", start)
    depth = 0
    for i in range(opening, len(source)):
        depth += (source[i] == "{") - (source[i] == "}")
        if depth == 0:
            return source[start:i + 1].replace("UnitGridAuthoring authoring", "LegacyInputs authoring").replace("Bounds modelBounds", "LegacyBounds modelBounds")
    raise ValueError("Unclosed original method")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    out = Path(args.output).absolute()
    out.mkdir(parents=True, exist_ok=False)
    build = Path(args.build).resolve(strict=True)
    data = json.loads(build.read_bytes())
    if data.get("status") != "Passed" or data.get("mission") != "S003" or data.get("compile_exit") != 0:
        raise ValueError("Passed S003 source build required")
    pins = data["source_and_reference_pins"]
    head = git("rev-parse", "HEAD").decode().strip()
    old_primary = git("show", head + ":" + OLD_PRIMARY)
    old_partial = git("show", head + ":" + OLD_PARTIAL)
    if old_primary != PRESERVED.read_bytes():
        raise ValueError("Historical primary source differs from preserved capture bytes")
    source = old_partial.decode("utf-8")
    footprint = method(source, "private static int2 ResolveFootprint(UnitGridAuthoring authoring, bool hasModelBounds, Bounds modelBounds)")
    vehicle = method(source, "private static UnitVehicleMovement ResolveVehicleMovement(UnitGridAuthoring authoring, int2 footprint, bool hasModelBounds, Bounds modelBounds)")
    generated = out / "HistoricalFootprintOwner.cs"
    generated.write_text("using Game.Components;using Unity.Mathematics;using UnityEngine;\n"
        "internal static class HistoricalFootprintOwner {\n"
        "internal sealed class LegacyInputs { internal Vector2Int footprintCells; internal bool UsesVehicleMotion; internal bool autoCalculateFootprint; }\n"
        "internal struct LegacyBounds { internal float3 size; }\n"
        + footprint + "\n" + vehicle + "\n"
        "internal static int2 Footprint(LegacyInputs a,bool b,LegacyBounds c)=>ResolveFootprint(a,b,c);\n"
        "internal static UnitVehicleMovement Vehicle(LegacyInputs a,int2 b,bool c,LegacyBounds d)=>ResolveVehicleMovement(a,b,c,d);\n}\n")
    generated_pin, harness_pin = sha(generated), sha(HARNESS)
    def verify():
        for path, expected in pins.items():
            if sha(path) != expected:
                raise ValueError("Source build pin changed: " + path)
        if sha(generated) != generated_pin or sha(HARNESS) != harness_pin or git("rev-parse", "HEAD").decode().strip() != head:
            raise ValueError("Fixture or source revision changed")
    verify()
    csc = next(Path(name) for name in pins if name.endswith("/Roslyn/bincore/csc.dll"))
    dotnet = shutil.which("dotnet")
    if not dotnet or not any(Path(name).name == "dotnet" and Path(name).resolve() == Path(dotnet).resolve() for name in pins):
        raise ValueError("Pinned dotnet required")
    response = Path(data["compiler_response"]["path"])
    if sha(response) != data["compiler_response"]["sha256"]:
        raise ValueError("Compiler response changed")
    exe = out / "S073UnitFootprintHistoricalDifferentialControl.exe"
    values = [json.loads(line) for line in response.read_text().splitlines() if line.strip()]
    values = ["-main:S073UnitFootprintHistoricalDifferentialControl" if x.startswith("-main:") else "-out:" + str(exe) if x.startswith("-out:") else x for x in values]
    values.extend((str(HARNESS), str(generated)))
    rsp = out / "compile.rsp"
    rsp.write_text("\n".join(json.dumps(x) for x in values) + "\n")
    compiled = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True, text=True, timeout=300)
    (out / "compile.log").write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode:
        raise RuntimeError("Differential source compile failed")
    runtime = data["runtime_selection"]
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(build.parent / "placement"), runtime["engine"], str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(out)))
    run = subprocess.run([runtime["mono"], str(exe)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (out / "run.log").write_text(run.stdout + run.stderr)
    if run.returncode or MARKER not in run.stdout:
        raise RuntimeError("Differential control failed or marker missing")
    verify()
    result = {"status": "PassedHistoricalHeadFootprintDifferentialFixture", "cases": 12800, "source_revision": head,
        "historical_primary_sha256": hashlib.sha256(old_primary).hexdigest(), "historical_partial_sha256": hashlib.sha256(old_partial).hexdigest(),
        "historical_primary_matches_capture": True, "historical_partial_capture_identity_proven": False,
        "bounds_size_is_fixture_input": True, "unity_bounds_construction_compared": False,
        "generated_sha256": generated_pin, "harness_sha256": harness_pin, "build_sha256": sha(build),
        "source_pins_before_after": True, "source_pin_count": len(pins), "fixture_only": True,
        "scene_or_native_parity": False, "source_fit_certified": False, "mission_ready": False}
    (out / "receipt.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result))


if __name__ == "__main__":
    main()

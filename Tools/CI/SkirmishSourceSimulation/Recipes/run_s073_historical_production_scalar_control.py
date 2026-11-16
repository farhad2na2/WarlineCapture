#!/usr/bin/env python3
"""Compile captured authoring getters against the current original scalar owner."""
import argparse
import hashlib
import json
import os
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
RELATIVE = "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs"
CAPTURED = ROOT / "Logs/SkirmishProduction/s073-captured-source-preservation-20261101-r1/source" / RELATIVE
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S073HistoricalProductionScalarControl.cs"
MARKER = "[S073HistoricalProductionScalarControl] result=Passed cases=64000 fixtureOnly=1 lifecycleParity=0 readiness=0"
GETTERS = ("ProductionDurationSeconds", "ProductionTransportArrivalSeconds", "ProductionTransportHoldForNextReadySeconds",
           "ProductionTransportMaxConcurrent", "ProductionTransportRequiresAirportRunway", "ProductionTransportUsesRunwayLanding",
           "IsAirUnit", "GetConfiguredFootprintCells")


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def getter_lines(source):
    lines = source.splitlines()
    result = []
    for name in GETTERS:
        rows = [line.strip() for line in lines if line.strip().startswith("public ") and
                (name + " =>" in line or name + "() =>" in line)]
        if len(rows) != 1:
            raise ValueError("Unique captured source getter required: " + name)
        result.append(rows[0])
    return result


def method(source, signature):
    if source.count(signature) != 1:
        raise ValueError("Unique current capture method required")
    start = source.index(signature)
    opening = source.index("{", start)
    depth = 0
    for index in range(opening, len(source)):
        depth += (source[index] == "{") - (source[index] == "}")
        if depth == 0:
            return source[start:index + 1]
    raise ValueError("Unclosed source capture method")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    out = Path(args.output).absolute()
    out.mkdir(parents=True, exist_ok=False)
    build = Path(args.build).resolve(strict=True)
    data = json.loads(build.read_bytes())
    if data.get("status") != "Passed" or data.get("mission") != "S073" or data.get("compile_exit") != 0:
        raise ValueError("Passed S073 source build required")
    pins = data["source_and_reference_pins"]
    current = ROOT / RELATIVE
    old_raw, current_raw = CAPTURED.read_bytes(), current.read_bytes()
    if hashlib.sha256(old_raw).hexdigest() != "cf5f9decee267a41f823e37c6dd0573debfadf2a4c5e140d84af475d1b998f4b":
        raise ValueError("Exact preserved authoring source required")
    if pins.get(str(current)) != hashlib.sha256(current_raw).hexdigest():
        raise ValueError("Current authoring source differs from source build pin")
    old, new = old_raw.decode("utf-8"), current_raw.decode("utf-8")
    getters = getter_lines(old)
    if getters != getter_lines(new):
        raise ValueError("Current getters differ from preserved captured getters")
    capture = method(new, "public UnitProductionMetadataScalarSource CaptureProductionMetadataScalarSource()")
    generated = out / "HistoricalProductionScalarOwner.cs"
    generated.write_text("using Game.Components;using UnityEngine;\ninternal sealed class HistoricalProductionScalarOwner {\n"
        "internal sealed class Config { internal bool IsAirUnit, ProductionTransportRequiresAirportRunway, ProductionTransportUsesRunwayLanding; }\n"
        "internal Config config; internal bool isAirUnit, productionTransportRequiresAirportRunway, productionTransportUsesRunwayLanding;\n"
        "internal float productionDurationSeconds, productionTransportArrivalSeconds, productionTransportHoldForNextReadySeconds;\n"
        "internal int productionTransportMaxConcurrent; internal Vector2Int footprintCells;\n" + "\n".join(getters) + "\n" + capture + "\n}\n")
    generated_pin, harness_pin, captured_pin = sha(generated), sha(HARNESS), sha(CAPTURED)
    def verify():
        for path, expected in pins.items():
            if sha(path) != expected:
                raise ValueError("Source build pin changed: " + path)
        if sha(generated) != generated_pin or sha(HARNESS) != harness_pin or sha(CAPTURED) != captured_pin:
            raise ValueError("Fixture source changed")
    verify()
    csc = next(Path(name) for name in pins if name.endswith("/Roslyn/bincore/csc.dll"))
    dotnet = shutil.which("dotnet")
    if not dotnet or not any(Path(name).name == "dotnet" and Path(name).resolve() == Path(dotnet).resolve() for name in pins):
        raise ValueError("Pinned dotnet required")
    response = Path(data["compiler_response"]["path"])
    if sha(response) != data["compiler_response"]["sha256"]:
        raise ValueError("Compiler response changed")
    exe = out / "S073HistoricalProductionScalarControl.exe"
    values = [json.loads(line) for line in response.read_text().splitlines() if line.strip()]
    values = ["-main:S073HistoricalProductionScalarControl" if x.startswith("-main:") else "-out:" + str(exe) if x.startswith("-out:") else x for x in values]
    values.extend((str(HARNESS), str(generated)))
    rsp = out / "compile.rsp"
    rsp.write_text("\n".join(json.dumps(x) for x in values) + "\n")
    compiled = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True, text=True, timeout=300)
    (out / "compile.log").write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode:
        raise RuntimeError("Production scalar source compile failed")
    runtime = data["runtime_selection"]
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(build.parent / "placement"), runtime["engine"], str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(out)))
    run = subprocess.run([runtime["mono"], str(exe)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (out / "run.log").write_text(run.stdout + run.stderr)
    if run.returncode or MARKER not in run.stdout:
        raise RuntimeError("Production scalar fixture failed or marker missing")
    verify()
    result = {"status": "PassedHistoricalProductionScalarFixture", "cases": 64000,
        "preserved_authoring_sha256": captured_pin, "current_authoring_sha256": sha(current),
        "getter_bodies_byte_equal": True, "capture_method_extracted_from_current_source": True,
        "generated_sha256": generated_pin, "harness_sha256": harness_pin, "build_sha256": sha(build),
        "source_pins_before_after": True, "source_pin_count": len(pins), "fixture_only": True,
        "prefab_lifecycle_proven": False, "transport_prefab_identity_compared": False,
        "recipe_observation_current": False, "runtime_admission": False, "mission_ready": False}
    (out / "receipt.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result))


if __name__ == "__main__":
    main()

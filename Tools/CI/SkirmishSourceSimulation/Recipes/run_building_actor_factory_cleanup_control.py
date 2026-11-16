#!/usr/bin/env python3
"""Fault-inject the exact extracted building actor factories in the source-host ECS."""
import argparse
import hashlib
import json
import os
import shutil
import subprocess
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[4]
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/BuildingActorFactoryCleanupControl.cs"
FACTORY = ROOT / "Assets/Game/Scripts/Systems/BuildingRuntimeEntityCompositionSystemHelper.cs"
RECIPE = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/regenerate_construction.py"
MARKER = "[BuildingActorFactoryCleanupControl] result=Passed blocker_stamp_failure=cleaned combat_component_failure=cleaned original_exceptions=preserved fixture_only=1 readiness=0"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--build", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    out = Path(args.output).absolute()
    out.mkdir(parents=True, exist_ok=False)
    build_path = Path(args.build).resolve(strict=True)
    build = json.loads(build_path.read_bytes())
    if build.get("status") != "Passed" or build.get("compile_exit") != 0:
        raise ValueError("Passed source-host build required")
    pins = build["source_and_reference_pins"]
    local_pins = {path: sha(path) for path in (HARNESS, FACTORY, RECIPE)}
    sys.path.insert(0, str(RECIPE.parent))
    import regenerate_construction

    def verify():
        for name, digest in pins.items():
            if sha(name) != digest:
                raise ValueError("Source build pin changed: " + name)
        if any(sha(path) != digest for path, digest in local_pins.items()):
            raise ValueError("Fault-control source changed during run")

    verify()
    csc = next(Path(name) for name in pins if name.endswith("/Roslyn/bincore/csc.dll"))
    dotnet = shutil.which("dotnet")
    if not dotnet or not any(Path(name).name == "dotnet" and Path(name).resolve() == Path(dotnet).resolve() for name in pins):
        raise ValueError("Pinned dotnet required")
    response = Path(build["compiler_response"]["path"])
    if sha(response) != build["compiler_response"]["sha256"]:
        raise ValueError("Compiler response pin changed")
    generated = out / "generated"
    generated.mkdir()
    regenerate_construction.generate(ROOT, generated)
    build_generated_factory = build_path.parent / "source/generated/323_BuildingRuntimeEntityFactory.source.cs"
    if build_generated_factory.read_bytes() != (generated / "BuildingRuntimeEntityFactory.source.cs").read_bytes():
        raise ValueError("Build-generated factory is not the current exact source extraction")
    exe = out / "BuildingActorFactoryCleanupControl.exe"
    argv = [json.loads(line) for line in response.read_text().splitlines() if line.strip()]
    argv = ["-main:BuildingActorFactoryCleanupControl" if value.startswith("-main:")
            else "-out:" + str(exe) if value.startswith("-out:") else value for value in argv]
    argv.append(str(HARNESS))
    rsp = out / "compile.rsp"
    rsp.write_text("\n".join(json.dumps(value) for value in argv) + "\n")
    compile_result = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True,
                                    text=True, timeout=300)
    (out / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    if compile_result.returncode:
        raise RuntimeError("Control compile failed")
    runtime = build["runtime_selection"]
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(build_path.parent / "placement"), str(build_path.parent), runtime["engine"],
        str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(out)))
    run_result = subprocess.run([runtime["mono"], str(exe)], cwd=ROOT, env=env,
                                capture_output=True, text=True, timeout=300)
    (out / "run.log").write_text(run_result.stdout + run_result.stderr)
    if run_result.returncode or MARKER not in run_result.stdout:
        raise RuntimeError("Control failed or pass marker missing")
    verify()
    print(json.dumps({"status": "PassedBuildingActorFactoryCleanupControl", "build_sha256": sha(build_path),
        "harness_sha256": local_pins[HARNESS], "factory_sha256": local_pins[FACTORY],
        "recipe_sha256": local_pins[RECIPE], "source_pins_before_after": True,
        "blocker_stamp_failure_cleaned": True, "combat_component_failure_cleaned": True,
        "original_exceptions_preserved": True, "fixture_only": True, "mission_ready": False}))


if __name__ == "__main__":
    main()

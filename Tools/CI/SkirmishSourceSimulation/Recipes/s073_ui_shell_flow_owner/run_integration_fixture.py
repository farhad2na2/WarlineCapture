#!/usr/bin/env python3
"""Compile and execute the S073 owner + original flow against a current pinned source host."""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[5]
HERE = Path(__file__).resolve().parent
BUILD = ROOT / "Logs/SkirmishProduction/s073-ui-shell-state-owner-integrated-build-20261103-r3/build.json"
BUILD_SHA256 = "16feafbbe2d54cf15b513cfdfabad708b69141cd9788b6dd79f6c1b3f25d8bc8"
FLOW_ADAPTER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073UiShellFlowBootstrap.cs"
OWNER_BOOTSTRAP = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073UiShellStateOwnerBootstrap.cs"
FLOW = ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs"
FLOW_REQUESTS = ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.Requests.cs"
FLOW_STARTUP = ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/UiShellStartupFlow.cs"
FLOW_AUDIO_CONTRACT = ROOT / "Assets/Game/Scripts/UI/Components/UIAudioEventGateway.cs"
FLOW_BOUNDARY = HERE / "SourceS073UiShellFlowBoundary.cs"
FLOW_STATE_HOST = HERE / "SourceS073FlowSystemStateHost.cs"
HOST_ECS_SOURCE = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/059_AutonomousEcs.cs"
FIXTURE = HERE / "SourceS073UiShellFlowIntegrationFixture.cs"
RUNNER = Path(__file__).resolve()


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def adapt_flow_source(source: str) -> str:
    seams = (
        ("            UiShellEcsGateway.RegisterAsRuntimeGateway();", "            SourceS073UiShellFlowBoundary.RegisterGateway();"),
        ("            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(entityManager);", "            Entity audioEntity = SourceS073UiShellFlowBoundary.EnsureAudioEntity(entityManager);"),
        ("            AudioEventRequestSystem.EnqueueMusicState(", "            SourceS073UiShellFlowBoundary.EnqueueMusicState("),
        ("                UiAudioEventBridgeSystem.TryEnqueue(world, request);", "                SourceS073UiShellFlowBoundary.TryEnqueue(world, request);"),
    )
    result = source
    for original, replacement in seams:
        if result.count(original) != 1:
            raise RuntimeError(f"Expected exactly one source-host effect seam: {original.strip()}")
        result = result.replace(original, replacement, 1)
    restored = result
    for original, replacement in seams:
        restored = restored.replace(replacement, original, 1)
    if restored != source:
        raise RuntimeError("Flow source adapter changed bytes outside the four captured effect seams")
    return result


def main() -> int:
    if len(sys.argv) != 2:
        raise SystemExit("usage: run_integration_fixture.py <new-output-directory>")
    output = Path(sys.argv[1]).absolute()
    if output.exists():
        raise FileExistsError(f"Fresh output directory required; preserving prior evidence: {output}")
    output.mkdir(parents=True)
    if sha(BUILD) != BUILD_SHA256:
        raise RuntimeError("Current S073 integrated build receipt pin mismatch")
    build = json.loads(BUILD.read_text(encoding="utf-8"))
    if build.get("status") != "Passed" or build.get("mission") != "S073" or build.get("native_or_player_launched") is not False:
        raise RuntimeError("Passing non-native S073 source-host build receipt required")
    response_path = Path(build["compiler_response"]["path"]).resolve(strict=True)
    if sha(response_path) != build["compiler_response"]["sha256"]:
        raise RuntimeError("Current S073 compiler response pin mismatch")
    pins = build["source_and_reference_pins"]
    changed = [(path, expected, sha(Path(path)) if Path(path).is_file() else None)
               for path, expected in pins.items()
               if not Path(path).is_file() or sha(Path(path)) != expected]
    if changed:
        raise RuntimeError(f"Current S073 source/reference pin failures: {changed[:5]}")

    args = [json.loads(line) for line in response_path.read_text(encoding="utf-8").splitlines() if line.strip()]
    main_indexes = [i for i, value in enumerate(args) if isinstance(value, str) and value.startswith("-main:")]
    out_indexes = [i for i, value in enumerate(args) if isinstance(value, str) and value.startswith("-out:")]
    ecs_indexes = [i for i, value in enumerate(args) if value == str(HOST_ECS_SOURCE)]
    if len(main_indexes) != 1 or len(out_indexes) != 1 or len(ecs_indexes) != 1:
        raise RuntimeError("Current compiler response must contain one main, output, and host ECS source")
    args[main_indexes[0]] = "-main:SourceS073UiShellFlowIntegrationFixture"
    executable = output / "S073UiShellFlowIntegrationFixture.exe"
    args[out_indexes[0]] = "-out:" + str(executable)

    original_world = "public sealed class World{public bool IsCreated{get;internal set;}=true;}"
    adapted_world = "public sealed class World{public bool IsCreated{get;internal set;}=true;public EntityManager EntityManager{get;set;}}"
    ecs_text = HOST_ECS_SOURCE.read_text(encoding="utf-8")
    if ecs_text.count(original_world) != 1:
        raise RuntimeError("Expected one exact source-host World declaration")
    adapted_ecs = output / "S073HostEcs.FlowWorldAdapter.cs"
    adapted_ecs.write_text(ecs_text.replace(original_world, adapted_world, 1), encoding="utf-8")
    if adapted_ecs.read_text(encoding="utf-8").replace(adapted_world, original_world, 1) != ecs_text:
        raise RuntimeError("World adapter changed bytes outside its EntityManager property")
    args[ecs_indexes[0]] = str(adapted_ecs)

    adapted_flow = output / "UiShellFlowSystem.source-host.cs"
    adapted_flow.write_text(adapt_flow_source(FLOW.read_text(encoding="utf-8")), encoding="utf-8")
    additions = (adapted_flow, FLOW_REQUESTS, FLOW_STARTUP, FLOW_AUDIO_CONTRACT,
                 FLOW_BOUNDARY, FLOW_STATE_HOST, FLOW_ADAPTER, FIXTURE)
    args.extend(str(path.resolve(strict=True)) for path in additions)
    compile_response = output / "compile.rsp"
    compile_response.write_text("\n".join(json.dumps(value) for value in args) + "\n", encoding="utf-8")

    dotnet = [Path(path) for path in pins if Path(path).name == "dotnet"]
    compilers = [Path(path) for path in pins if Path(path).name == "csc.dll"]
    if len(dotnet) != 1 or not compilers:
        raise RuntimeError("Current build receipt lacks pinned Roslyn/.NET compiler")
    compilers.sort(key=lambda path: tuple(int(part) for part in path.parents[2].name.split(".")))
    compiler = compilers[-1]
    start = time.monotonic()
    compile_result = subprocess.run([str(dotnet[0]), str(compiler), "@" + str(compile_response)],
                                    capture_output=True, text=True, timeout=180)
    compile_log = output / "compile.log"
    compile_log.write_text(compile_result.stdout + compile_result.stderr, encoding="utf-8")
    receipt = {
        "schema_version": 1,
        "kind": "S073UiShellOwnerFlowCurrentSourceHostFixture",
        "status": "Failed",
        "scope": "Current receipt-pinned S073 source host; original shell owner plus original UiShellFlowSystem OnCreate and one no-input OnUpdate. Four flow effect calls are captured at source-host boundaries; no native Unity/player/readiness claim.",
        "base_build": {"path": str(BUILD), "sha256": BUILD_SHA256},
        "base_source_pin_count": len(pins),
        "compiler_response": {"path": str(response_path), "sha256": sha(response_path)},
        "world_adapter": {
            "source": str(HOST_ECS_SOURCE),
            "source_sha256": sha(HOST_ECS_SOURCE),
            "generated_copy": str(adapted_ecs),
            "generated_copy_sha256": sha(adapted_ecs),
            "transformation": "Add one EntityManager property to the source-host World declaration; original ECS adapter remains untouched.",
        },
        "effect_boundaries": {
            "gateway_registration": "Original call replaced in derived flow copy with one captured counter; no runtime gateway registration.",
            "audio_entity_ensure": "Original call replaced with captured counter returning Entity.Null; no audio entity created.",
            "music_enqueue": "Original enqueue replaced with captured counter; no music queue mutation.",
            "ui_audio_enqueue": "Original bridge enqueue replaced with captured counter; no audio queue mutation.",
            "presentation": "Original flow presentation logic retained; command buffer observed empty before/after the one no-input update; no presentation effects omitted or injected.",
        },
        "source_pins": {str(path.resolve(strict=True)): sha(path) for path in (FLOW, *additions, HOST_ECS_SOURCE, adapted_ecs, compile_response)},
        "compile": {"exit_code": compile_result.returncode, "seconds": time.monotonic() - start,
                    "log": str(compile_log), "log_sha256": sha(compile_log)},
    }
    try:
        if compile_result.returncode != 0 or not executable.is_file():
            raise RuntimeError("Current S073 owner+flow source-host compile failed")
        runtime = build["runtime_selection"]
        mono = Path(runtime["mono"]).resolve(strict=True)
        config = mono.parent.parent / "etc/mono/config"
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join([
            str(ROOT / "Library/ScriptAssemblies"), str(Path(runtime["engine"])),
            str(Path(runtime["newtonsoft"]).parent), str(Path(build["placement_build"]).parent)
        ])
        start = time.monotonic()
        run_result = subprocess.run([str(mono), "--config", str(config), "--optimize=-float32", str(executable)],
                                    capture_output=True, text=True, timeout=120, env=env)
        run_log = output / "run.log"
        run_log.write_text(run_result.stdout + run_result.stderr, encoding="utf-8")
        marker = "[S073UiShellFlowIntegrationFixture] result=Passed owner=OriginalOnCreate flow=OriginalOnCreate+OneNoInputOnUpdate state=Splash/Pending requests=0 audio=0 music=0 gateway=captured native=0 readiness=0"
        receipt["run"] = {"exit_code": run_result.returncode, "seconds": time.monotonic() - start,
                          "log": str(run_log), "log_sha256": sha(run_log),
                          "required_pass_marker": marker, "pass_marker_present": marker in run_result.stdout}
        if run_result.returncode != 0 or marker not in run_result.stdout:
            raise RuntimeError("Current S073 owner+flow source-host fixture failed")
        receipt["status"] = "Passed"
    except Exception as error:
        receipt["failure"] = str(error)
    source_pin_ledger = output / "source-pins.json"
    integration_sources = (RUNNER, FLOW, FLOW_REQUESTS, FLOW_STARTUP, FLOW_AUDIO_CONTRACT,
                           FLOW_BOUNDARY, FLOW_STATE_HOST, OWNER_BOOTSTRAP, FLOW_ADAPTER, FIXTURE, HOST_ECS_SOURCE,
                           adapted_flow, adapted_ecs, compile_response)
    ledger = {
        "schema_version": 1,
        "base_build_receipt": {"path": str(BUILD), "sha256": BUILD_SHA256},
        "base_source_and_reference_pins": pins,
        "integration_source_pins": {str(path.resolve(strict=True)): sha(path) for path in integration_sources},
        "generated_boundaries": receipt["effect_boundaries"],
        "generated_world_adapter": receipt["world_adapter"],
    }
    source_pin_ledger.write_text(json.dumps(ledger, indent=2) + "\n", encoding="utf-8")
    receipt["source_pin_ledger"] = {"path": str(source_pin_ledger), "sha256": sha(source_pin_ledger),
                                    "base_source_pin_count": len(pins),
                                    "integration_source_pin_count": len(integration_sources)}
    receipt_path = output / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(receipt, separators=(",", ":")))
    return 0 if receipt["status"] == "Passed" else 1


if __name__ == "__main__":
    raise SystemExit(main())

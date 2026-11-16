#!/usr/bin/env python3
"""Compile/run a disposable current-source Rifle ARIA preflight fixture."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import time


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def extract_original_hud_command_helper(repo, pins, output):
    """Extract exact native DTO/context/entry bodies into the isolated source host."""
    helper_path = (repo / "Assets/Game/Scripts/Systems/BuildingUiCommandSystemHelper.cs").resolve(strict=True)
    context_path = (repo / "Assets/Game/Scripts/Systems/BuildingUiCommandContext.cs").resolve(strict=True)
    helper_sha = sha(helper_path)
    context_sha = sha(context_path)
    require(pins.get(str(helper_path)) == helper_sha and pins.get(str(context_path)) == context_sha,
            "Native HUD helper/context must match the checked source-build pins")

    extractor_path = (repo / "Tools/CI/SkirmishSourceSimulation/Recipes/availability/regenerate_availability.py").resolve(strict=True)
    extractor_sha = sha(extractor_path)
    require(pins.get(str(extractor_path)) == extractor_sha,
            "Source-owner extractor must match the checked source-build pin")
    sys.dont_write_bytecode = True
    spec = importlib.util.spec_from_file_location("fixture_source_owner_extractor", extractor_path)
    require(spec is not None and spec.loader is not None, "Current source-owner extractor unavailable")
    extractor = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extractor)
    helper_text = helper_path.read_text()
    context_text = context_path.read_text()

    def source_type(source, kind, name):
        return extractor.declaration(source, kind, name)

    def source_delegate(name):
        code = extractor.mask(helper_text)
        matches = list(__import__("re").finditer(
            r"(?m)^[ \t]*public\s+delegate\b[^;{}]*\b" + __import__("re").escape(name) + r"\s*\(", code))
        require(len(matches) == 1, "Unique original HUD delegate required: " + name)
        start = matches[0].start()
        opening = code.find("(", matches[0].start(), matches[0].end())
        depth = 1
        cursor = opening + 1
        while depth:
            require(cursor < len(code), "Unclosed original HUD delegate: " + name)
            if code[cursor] == "(": depth += 1
            elif code[cursor] == ")": depth -= 1
            cursor += 1
        end = code.find(";", cursor)
        require(end >= 0, "Original HUD delegate terminator missing: " + name)
        return helper_text[start:end + 1]

    def bind_source_token(source):
        return __import__("re").sub(r"\bGameObject\b", "SourceUnitPrefab", source)

    pieces = [
        source_type(helper_text, "readonly struct", "ConfiguredUnitEntry"),
        source_delegate("TryGetConfiguredSpawnableDelegate"),
        source_delegate("TryGetConfiguredUnitDelegate"),
        source_delegate("GetCampRequestFailureDelegate"),
        source_delegate("TryRequestCampItemDelegate"),
        source_delegate("CancelProductionDelegate"),
        extractor.member(helper_text, "TryRequestCampItem"),
        source_type(context_text, "readonly struct", "Context"),
    ]
    generated = (
        "using System;\nusing UnityEngine;\nnamespace Game.Runtime { public sealed partial class BuildingUiCommandSystemHelper {\n" +
        "\n".join(bind_source_token(piece) for piece in pieces) + "\n} }\n")
    generated_path = output / "OriginalHudCommandHelperSource.cs"
    generated_path.write_text(generated)
    return {
        "path": str(generated_path),
        "sha256": sha(generated_path),
        "source_pins": {str(helper_path): helper_sha, str(context_path): context_sha},
        "extractor_path": str(extractor_path),
        "extractor_sha256": extractor_sha,
        "members": ["ConfiguredUnitEntry", "five original delegate declarations", "TryRequestCampItem", "Context"],
        "binding": "exact source members with GameObject token aliased to SourceUnitPrefab",
    }


def run(build_path, descriptor_path, descriptor_sha, movement_path, movement_sha,
        graph_path, graph_sha, matrix_path, matrix_sha, output_path, connected_entry):
    require(connected_entry in ("aria-success", "hud-success"),
            "Explicit connected entry must be aria-success or hud-success")
    build_path = Path(build_path).resolve(strict=True)
    descriptor_path = Path(descriptor_path)
    movement_path = Path(movement_path)
    graph_path = Path(graph_path)
    matrix_path = Path(matrix_path)
    for path in (descriptor_path, movement_path, graph_path, matrix_path):
        require(path.is_absolute(), "Explicit absolute fixture source path required")
    descriptor_path = descriptor_path.resolve(strict=True)
    movement_path = movement_path.resolve(strict=True)
    graph_path = graph_path.resolve(strict=True)
    matrix_path = matrix_path.resolve(strict=True)
    for path, expected in ((descriptor_path, descriptor_sha), (movement_path, movement_sha),
                           (graph_path, graph_sha), (matrix_path, matrix_sha)):
        require(len(expected) == 64 and all(c in "0123456789abcdef" for c in expected),
                "Explicit lowercase SHA-256 required for " + str(path))
        require(sha(path) == expected, "Explicit fixture source SHA-256 mismatch: " + str(path))

    output = Path(output_path).absolute()
    require(not output.exists(), "Fresh isolated output directory required")
    output.mkdir(parents=True)
    build_raw = build_path.read_bytes()
    build = json.loads(build_raw)
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
            build.get("mission") == "S002", "Passing current checked S002 source build required")
    pins = build.get("source_and_reference_pins", {})

    def verify_pins():
        for path, expected in pins.items():
            require(Path(path).is_file() and sha(path) == expected, "Checked source/build pin changed: " + path)
        for path, expected in ((descriptor_path, descriptor_sha), (movement_path, movement_sha),
                               (graph_path, graph_sha), (matrix_path, matrix_sha)):
            require(sha(path) == expected, "Fixture input changed during run: " + str(path))

    verify_pins()
    repo = Path(build["repository_root"]).resolve(strict=True)
    source = repo / "Tools/CI/SkirmishSourceSimulation/Tests/SourceS002RifleAriaPreflightFixture.cs"
    source_hash = sha(source)
    hud_helper_extraction = extract_original_hud_command_helper(repo, pins, output)
    response_path = Path(build["compiler_response"]["path"]).resolve(strict=True)
    require(sha(response_path) == build["compiler_response"]["sha256"], "Checked compiler response changed")
    arguments = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    mains = [i for i, value in enumerate(arguments) if value == "-main:SourceSimulationProgram"]
    outputs = [i for i, value in enumerate(arguments) if value.startswith("-out:")]
    require(len(mains) == 1 and len(outputs) == 1, "Expected one source host entry and output")
    executable = output / "SourceS002RifleAriaPreflightFixture.exe"
    arguments[mains[0]] = "-main:SourceS002RifleAriaPreflightFixture"
    arguments[outputs[0]] = "-out:" + str(executable)
    arguments.append(str(source))
    arguments.append(hud_helper_extraction["path"])
    test_response = output / "fixture.rsp"
    test_response.write_text("\n".join(json.dumps(value) for value in arguments) + "\n")

    dotnet = next((path for path in pins if Path(path).name == "dotnet"), None)
    compiler = next((path for path in pins if Path(path).name == "csc.dll"), None)
    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    require(dotnet and compiler, "Pinned Roslyn toolchain required")
    mono_helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    mono_native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    require(mono_helper.is_file() and mono_native.is_file(), "Pinned Mono helper libraries required")
    mono_config = output / "mono-config.xml"
    dllmaps = "".join("<dllmap dll=\"" + name + "\" target=\"" + str(mono_helper) + "\" os=\"!windows\"/>"
                       for name in ["MonoPosixHelper"]) + "".join(
        "<dllmap dll=\"" + name + "\" target=\"" + str(mono_native) + "\" os=\"!windows\"/>"
        for name in ["System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"])
    mono_config.write_text("<configuration>" + dllmaps + "</configuration>\n")

    started = time.monotonic()
    compile_result = subprocess.run([dotnet, compiler, "@" + str(test_response)], capture_output=True,
                                    text=True, timeout=120)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    run_result = None
    if compile_result.returncode == 0:
        verify_pins()
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join([
            str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
            str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)
        ])
        run_result = subprocess.run([str(runtime["mono"]), "--config", str(mono_config), str(executable),
                                     str(descriptor_path), descriptor_sha, str(movement_path), movement_sha,
                                     str(graph_path), graph_sha, str(matrix_path), matrix_sha, connected_entry],
                                    env=env, capture_output=True, text=True, timeout=60)
        (output / "run.log").write_text(run_result.stdout + run_result.stderr)
        verify_pins()

    log = "" if run_result is None else run_result.stdout + run_result.stderr
    marker = "[SourceS002RifleAriaPreflightFixture] result=Passed scope=DisposableS002OneProducerFixture"
    connected_marker = "connected_entry=" + connected_entry + " connected_receipt="
    movement_match = __import__("re").search(
        r"movement_outcome=actual_manual_order_pathfinding_and_grid_movement movement_start=(.*?) movement_start_position=([^ ]+) movement_goal=(.*?) movement_end=(.*?) movement_end_position=([^ ]+) movement_ticks=([0-9]+) observed_path_cells=([0-9]+) path_cells=([^ ]*) path_follow_index_after_apply=(-?[0-9]+) path_follow_after_owner=([Tt]rue|[Ff]alse) path_range_after_owner=([Tt]rue|[Ff]alse) movement_order_completed=([Tt]rue|[Ff]alse) movement_stop_reason=([a-z_]+) owner_tick_observations=([0-9]+) owner_tick_observations_json=(\[.*?\]) shared_semantic_owner=",
        log)
    movement_observations = None if movement_match is None else json.loads(movement_match.group(15))
    result = {
        "schema_version": 1,
        "kind": "SourceS002RifleAriaPreflightFixture",
        "status": "Passed" if compile_result.returncode == 0 and run_result is not None and
                  run_result.returncode == 0 and marker in log and connected_marker in log and movement_match is not None else "Failed",
        "connected_entry": connected_entry,
        "scope": "Disposable one-producer fixture using current no-capture Rifle projection, current authored metadata, original S002 setup compiler and actual source bank/producer/availability/delivery owners. It preserves actual pre-payment missing-helicopter rejection controls, then performs exactly one paid request through the selected entry (SourceAriaPaidRecruitment or the extracted original BuildingUiCommandSystemHelper.TryRequestCampItem), using an explicit known-null presentation fixture scope. Session phase/completion state is fixture-scoped; its legacy economy-stock carrier is populated from exact compiled setup fields following the original session initializer. One of 18 compiled setup structures, an explicitly synthetic open 64x64 fixture grid with X/Z origin -40, and exact disposable-manager no-record census are also fixture-scoped; none claim S002 runtime state or source-map fit. The grid frame places the null-instance delivery fallback outside the source-authored Barrack footprint reservation. The fixture producer slot carries the exact descriptor-derived mixed-case Rifle source key required by native receipt delivery. Producer identity is generic authored witness only, with no Unity resolution or S002 membership. A fixture-created Prefab-tagged ECS entity carries only the source-bound movement/spawn/initializer component subset and is bound to the exact source GUID:fileID; it is not the native Baker prefab. UnitSelectionHitbox, UnitDisplayInfo, UnitAnimationOrder, UnitAnimationGroundOffset, model/visual/VFX references, and complete prefab closure are omitted; Faction's pre-initializer carrier value is unspecified, and source-host string-hash parity with native Unity is unclaimed. SourceMetadataComplete remains false; producer factory footprint is unverified. After actual receipt delivery/spawn, the fixture issues a manual order and invokes original pathfinding, occupancy, and grid-movement owners with explicit fixture-only activation and 1/60-second steps. This is source-owner fixture behavior only, not live S002, native scheduler parity, map-fit evidence, normal HUD composition or player readiness.",
        "build_receipt": str(build_path),
        "build_receipt_sha256": hashlib.sha256(build_raw).hexdigest(),
        "fixture_source_sha256": source_hash,
        "original_hud_command_helper_extraction": hud_helper_extraction,
        "compiler_response_sha256": sha(response_path),
        "authored_metadata_descriptor": str(descriptor_path),
        "authored_metadata_descriptor_sha256": descriptor_sha,
        "movement_projection_input": str(movement_path),
        "movement_projection_input_sha256": movement_sha,
        "connected_entry": connected_entry,
        "source_config_graph": str(graph_path),
        "source_config_graph_sha256": graph_sha,
        "setup_matrix": str(matrix_path),
        "setup_matrix_sha256": matrix_sha,
        "compile_exit": compile_result.returncode,
        "run_exit": None if run_result is None else run_result.returncode,
        "pass_marker_present": marker in log,
        "elapsed_seconds": time.monotonic() - started,
        "output": str(output),
        "native_or_player_launched": False,
        "fixture_partial_prefab_entity_materialized": True,
        "fixture_grid": {"kind": "SyntheticDisposableCoordinateFrame", "width": 64, "height": 64,
                         "cell_size": 1.0, "origin": [-40.0, 0.0, -40.0],
                         "source_map_fit_claimed": False},
        "native_baker_prefab_materialized": False,
        "partial_prefab_omissions": ["UnitSelectionHitbox", "UnitDisplayInfo", "UnitAnimationOrder", "UnitAnimationGroundOffset", "model/visual/VFX references", "complete prefab closure"],
        "faction_pre_initializer_value_claimed": False,
        "idle_hash_native_runtime_parity_claimed": False,
        "unit_spawned": connected_marker in log,
        "movement_issued": movement_match is not None,
        "movement_completed": movement_match is not None and movement_match.group(12).lower() == "true",
        "movement_stop_reason": None if movement_match is None else movement_match.group(13),
        "fixture_movement": None if movement_match is None else {
            "start_cell": movement_match.group(1), "start_position": movement_match.group(2),
            "goal_cell": movement_match.group(3), "end_cell": movement_match.group(4),
            "end_position": movement_match.group(5), "owner_updates": int(movement_match.group(6)),
            "observed_path_cells": int(movement_match.group(7)), "path_cells": movement_match.group(8),
            "path_follow_index_after_apply": int(movement_match.group(9)),
            "path_follow_after_owner": movement_match.group(10).lower() == "true",
            "path_range_after_owner": movement_match.group(11).lower() == "true",
            "order_completed": movement_match.group(12).lower() == "true",
            "stop_reason": movement_match.group(13),
            "owner_tick_count": int(movement_match.group(14)),
            "owner_tick_observations": movement_observations,
            "actual_manual_order_pathfinding_occupancy_and_grid_movement_owners": True,
            "fixture_activation_only": True, "native_scheduler_parity_claimed": False
        },
        "mission_admission": False,
        "mission_ready": False
    }
    (output / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, indent=2))
    return 0 if result["status"] == "Passed" else 1


if __name__ == "__main__":
    if len(sys.argv) != 12:
        raise SystemExit("usage: run_source_s002_rifle_aria_preflight_fixture.py <build.json> <descriptor.json> <descriptor-sha> <movement-input.json> <movement-sha> <graph.json> <graph-sha> <matrix.csv> <matrix-sha> <fresh-output-dir> <aria-success|hud-success>")
    raise SystemExit(run(*sys.argv[1:]))

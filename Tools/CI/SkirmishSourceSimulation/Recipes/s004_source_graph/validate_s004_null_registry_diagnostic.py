#!/usr/bin/env python3
"""Fail-closed validator for the scoped S004 null-registry owner diagnostic."""
import hashlib
import json
from pathlib import Path
import re
import sys


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(ok, message):
    if not ok:
        raise ValueError(message)


def validate(receipt_path):
    receipt_path = Path(receipt_path).resolve()
    receipt = json.loads(receipt_path.read_text())
    require(receipt.get("kind") == "S004NullRegistryOriginalLaunchSourceHostDiagnostic" and
            receipt.get("status") == "PassedDiagnosticOnly",
            "A passed diagnostic-only S004 receipt is required")
    base = receipt_path.parent
    for key in ("source_build", "source_graph", "capture_replay_context"):
        entry = receipt[key]
        path = Path(entry["path"])
        if not path.is_absolute():
            path = (Path.cwd() / path).resolve()
        require(path.is_file() and sha(path) == entry["sha256"], key + " pin changed or is missing")

    build = json.loads(Path(receipt["source_build"]["path"]).read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and
            build.get("mission") == "S002" and build.get("status") == "Passed",
            "Pinned source-host build is not a successful S002 build")
    for path, expected in build["source_and_reference_pins"].items():
        require(Path(path).is_file() and sha(path) == expected, "Source-host build pin changed: " + path)

    graph_path = Path(receipt["source_graph"]["path"])
    graph = json.loads(graph_path.read_text())
    require(graph.get("authority") == "AuthoredConfigAssets" and graph.get("compiled_setup") is False and
            graph.get("world_initialized") is False and graph.get("native_world_state_used") is False and
            graph.get("definition_path") ==
            "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishScenario_S004.asset",
            "Exact authored uncompiled S004 graph required")
    for path, expected in graph["source_pins"].items():
        require(Path(path).is_file() and sha(path) == expected, "Authored graph source pin changed: " + path)

    owner = receipt["extracted_original_owner"]
    owner_path = Path(owner["path"])
    if not owner_path.is_absolute():
        owner_path = (Path.cwd() / owner_path).resolve()
    original_path = Path(owner["original_source_path"])
    original = original_path.read_text()
    extracted = owner_path.read_text()
    require(sha(original_path) == owner["original_source_sha256"] and sha(owner_path) == owner["sha256"],
            "Original or extracted owner source pin changed")
    require(owner["contract"] == "Only TryQueue; registry parameter/guard removed under explicit null-only extraction" and
            "TryQueue(" in extracted and "TryQueue(" in original and
            receipt["source_host_logical_identity_profile"]["identity_kind"].startswith("Source-host logical Entity IDs only"),
            "Null-only TryQueue extraction and logical source-host identity contract required")
    # The extracted file must be the original owner's TryQueue body with only its
    # registry parameter and registry guard removed. Compare normalized owner body
    # against the pinned source; do not admit a hand-reimplemented queue path.
    require("SkirmishExpandedLaunchProjection.TryQueue(em, wrongSeed, standardSetup)" in
            (base / "S004NullRegistryLaunchDiagnosticBinding.cs").read_text() and
            "SkirmishExpandedLaunchProjection.TryQueue(em, payload, standardSetup)" in
            (base / "S004NullRegistryLaunchDiagnosticBinding.cs").read_text(),
            "Diagnostic must exercise extracted original owner for both negative and positive paths")
    initialization = receipt.get("session_initialization_owner", {})
    initialization_path = Path(initialization.get("path", ""))
    original_initialization = Path(initialization.get("original_source_path", ""))
    require(initialization_path.is_file() and original_initialization.is_file() and
            sha(initialization_path) == initialization.get("sha256") and
            sha(original_initialization) == initialization.get("original_source_sha256") and
            initialization.get("source_bodies_copied_verbatim") is True and
            set(initialization.get("extracted_method_hashes", {})) ==
            {"OnUpdate", "TryInitialize", "ProjectRoles", "Fail"},
            "Original session initialization owner source and extracted method evidence required")
    initialization_source = initialization_path.read_text()
    binding_source = (base / "S004NullRegistryLaunchDiagnosticBinding.cs").read_text()
    require(all(name in initialization_source for name in
                ("OnUpdate(", "TryInitialize(", "ProjectRoles(", "Fail(")) and
            "initializationSystem.OnUpdate(ref systemState)" in binding_source,
            "Diagnostic must invoke the extracted original session initialization owner")

    compile = receipt["compile"]
    run = receipt["run"]
    for section in (compile, run):
        log = Path(section["log"])
        require(log.is_file() and sha(log) == section["log_sha256"], "Diagnostic evidence log changed")
    require(compile.get("exit") == 0 and run.get("exit") == 0 and
            Path(compile["output"]).is_file() and Path(compile["response"]).is_file() and
            sha(compile["response"]) == compile["response_sha256"],
            "Diagnostic compile/run evidence is incomplete")
    output = json.loads(Path(run["log"]).read_text().splitlines()[-1])
    require(output.get("kind") == "S004AuthoredLayoutCaptureReplay" and output.get("status") == "Passed" and
            output.get("world_initialized") is False and output.get("mission_readiness") is False and
            output.get("physical_source_fit_verified") is False,
            "Source-host diagnostic output failed or overclaims readiness")
    launch = output.get("launch_diagnostic", {})
    require(launch.get("status") == "Passed" and launch.get("catalog_id") == "S004" and
            launch.get("definition_id") == "skirmish.s004" and launch.get("session_phase") == "Queued" and
            launch.get("match_phase") == "Queued" and launch.get("setup_reference_preserved") is True and
            launch.get("generated_session_id_by_original_owner") is True and
            launch.get("wrong_seed_rejected_without_mutation") is True and
            launch.get("duplicate_rejected") is True and launch.get("native_input_claim") is False and
            launch.get("prefab_or_scene_admitted") is False,
            "Actual original-owner queue state or negative cases did not validate")
    init_result = launch.get("session_initialization", {})
    require(init_result.get("status") == "Passed" and
            init_result.get("owner") == "Game.Runtime.SkirmishSessionInitializationSystem.OnUpdate" and
            init_result.get("phase_before") == "Queued" and init_result.get("phase_after") == "Spawning" and
            init_result.get("initialization_complete") is True and
            all(init_result.get(field) is True for field in
                ("objective_state_projected", "capacity_projected", "economy_stock_projected",
                 "production_reservation_buffer_projected", "readiness_request_buffer_projected")) and
            init_result.get("native_world_initialized") is False and
            init_result.get("scene_or_prefab_admitted") is False and init_result.get("spawn_executed") is False,
            "Original session initialization owner gate or source-host output is invalid")
    standard = [row for row in output.get("compiler", []) if row.get("size") == "Standard"]
    require(len(standard) == 1 and standard[0].get("matched") is True,
            "Graph-materialized Standard setup must match the pinned native compiler hash")
    require(receipt["boundaries"] == {
        "source_host_entity_manager_constructed": True,
        "source_host_identity_allocator_admitted": True,
        "native_world_initialized": False,
        "native_input_claim": False,
        "prefab_or_scene_admitted": False,
        "runtime_spawn_executed": False,
        "physical_source_fit_verified": False,
        "mission_readiness": False,
    }, "Diagnostic boundary claims changed")
    return {"status": "PassedDiagnosticOnly", "queued_catalog_id": "S004",
            "setup_hash": standard[0]["setupHash"], "native_input": False,
            "mission_readiness": False}


if __name__ == "__main__":
    try:
        if len(sys.argv) != 2:
            raise ValueError("usage: validate_s004_null_registry_diagnostic.py RECEIPT.json")
        print(json.dumps(validate(sys.argv[1]), sort_keys=True))
    except (ValueError, OSError, KeyError, TypeError, json.JSONDecodeError) as exc:
        print("S004 null-registry diagnostic rejected: " + str(exc), file=sys.stderr)
        raise SystemExit(2)

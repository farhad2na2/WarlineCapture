#!/usr/bin/env python3
"""Build a bounded source-only diagnostic joining an actual S003 compiled Rifle overlay to current authored role/prefab inputs.

This diagnostic establishes no Unity factory closure, prefab bake, runtime admission, mission world, or readiness.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
GRAPH = ROOT / "T Logs/SkirmishProduction/s003-source-graph-extensions-20261103-T26/grid-surface/source-config-graph-with-grid-surface-nodes.json"
GRAPH_SHA = "34194993cb3430293923140addf3d19d6222e86a675fb82a2fe06b83f84eaf9f"
CAPTURE_BINDING = ROOT / "T Logs/SkirmishProduction/s003-3419-graph-refresh-20261103-T1/original-compiler/S003OriginalCompilerSourceBinding.cs"
CAPTURE_BINDING_SHA = "ba6fca29fde4a4ceaa43a59f1d2af8f8451b1e7705a5e9c701ec9ec25aecbe04"
COMPILER_RESPONSE = ROOT / "T Logs/SkirmishProduction/s003-t26-qualification-build-20261103-T1/compile.rsp"
SOURCE_DESCRIPTOR = ROOT / "T Logs/SkirmishProduction/barrack-rifle-source-descriptor-20261103-T17/descriptor.json"
SOURCE_DESCRIPTOR_SHA = "5e025367f128dde6b862fab29e80ff8acb8e8245a451f97e1e066ef89a42a6af"
COMPILER_RECEIPT = ROOT / "T Logs/SkirmishProduction/s003-t26-original-compiler-20261103-T1/receipt.json"
BUILD_RECEIPT = ROOT / "T Logs/SkirmishProduction/s003-t26-qualification-build-20261103-T1/build.json"
COMPILER_RECEIPT_SHA = "9d23329b7565ed5cb6194f68bbe9257229b80b6f2d29d0f8f4a36d6c701bef06"
BUILD_RECEIPT_SHA = "f14ab8beb9723412f4a21d406f66dab65a88c5748495c51811aea86c7b7005b1"
EXPECTED_SETUP_HASHES = {"Standard": 2394754492, "War": 2291016159, "LargeWar": 539080277}
TRANSPORT_RESOLVER = "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs"


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def unique(items, key):
    found = [item for item in items if key(item)]
    need(len(found) == 1, "Expected exactly one matching authored role row")
    return found[0]


def compiled_setup_identity(setups):
    return [{key: row.get(key) for key in
             ("catalog_id", "definition_id", "content_version", "seed", "size_id", "difficulty_id", "setup_hash", "compiled")}
            for row in setups]


def validate_historical_setup_match(compiler: dict, receipt: dict) -> None:
    result = receipt.get("result", {})
    need(result.get("kind") == "S003OriginalCompilerSourceOnlyResult" and result.get("compiled") is True and
         result.get("status") == "Compiled" and result.get("setups") == compiled_setup_identity(compiler.get("setups", [])),
         "Fresh original compiler result differs from pinned T26 setup receipt")


def validate_build_binding(compiler_receipt: dict, build_path: Path, build_sha: str) -> None:
    need(compiler_receipt.get("source_build", {}).get("path") == str(build_path) and
         compiler_receipt.get("source_build", {}).get("sha256") == build_sha,
         "Original compiler receipt does not bind the exact source build receipt")


def validate_transport_resolver(source_text: str) -> None:
    """Check the actual pinned queue resolver flow that selects transport metadata and applies its floor."""
    start = source_text.find("public ProductionTransportSettings ResolveProductionTransportSettings(")
    end = source_text.find("\n        private ", start + 1)
    need(start >= 0 and end > start, "Original production transport resolver declaration required")
    method = source_text[start:end]
    required = [
        "bool hasProducedMetadata = TryGetUnitProductionMetadata(spawnUnitPrefab, out UnitProductionMetadata producedMetadata);",
        "transportPrefab = hasProducedMetadata ? producedMetadata.ProductionTransportPrefab : null;",
        "if (TryGetUnitProductionMetadata(transportPrefab, out UnitProductionMetadata transportMetadata))",
        "arrivalSeconds = transportMetadata.ProductionTransportArrivalSeconds;",
        "holdForNextReadySeconds = transportMetadata.ProductionTransportHoldForNextReadySeconds;",
        "maxConcurrent = transportMetadata.ProductionTransportMaxConcurrent;",
        'if (string.Equals(transportPrefab.name, "Unit_Veh_Helicopter_Transport", System.StringComparison.Ordinal))',
        "maxConcurrent = Mathf.Max(2, maxConcurrent);",
    ]
    positions = [method.find(token) for token in required]
    need(all(position >= 0 for position in positions) and positions == sorted(positions),
         "Original resolver no longer selects transport prefab metadata then enforces helicopter concurrency floor")


def validate_join(graph: dict, compiler: dict, source: dict) -> dict:
    need(graph.get("authority") == "AuthoredConfigAssets" and graph.get("compiled_setup") is False and
         graph.get("world_initialized") is False and graph.get("native_world_state_used") is False,
         "Current source-only T26 graph required")
    nodes = graph.get("nodes")
    catalog_node = unique(nodes, lambda n: n.get("source_path") ==
                          "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishRoleCatalog_V1.asset")
    role_rows = catalog_node.get("fields", {}).get("roles")
    role = unique(role_rows, lambda r: r.get("RoleId") == "role.rifle")
    need(role == {"RoleId": "role.rifle", "Kind": 1,
                  "SourceConfigPath": "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset",
                  "RuntimePrefabKey": "Unit_Chr_Soldier_Male_02_Alt_04", "Producer": 1,
                  "RequiredReadiness": 1, "Disposition": 1, "CapabilityCertified": 0},
         "Exact current S003 Rifle role-catalog row required")
    setups = compiler.get("setups")
    need(compiler.get("kind") == "S003OriginalCompilerSourceOnlyResult" and compiler.get("compiled") is True and
         compiler.get("status") == "Compiled" and compiler.get("catalog_id") == "S003" and
         compiler.get("definition_id") == "skirmish.s003" and compiler.get("world_initialized") is False and
         compiler.get("physical_source_fit_verified") is False and compiler.get("mission_readiness") is False,
         "Exact successful source-only original S003 compiler output required")
    need(isinstance(setups, list) and len(setups) == 3 and
         [s.get("size_id") for s in setups].count("Standard") == 1 and
         [s.get("size_id") for s in setups].count("War") == 1 and
         [s.get("size_id") for s in setups].count("LargeWar") == 1,
         "Exactly one compiled setup per S003 size required")
    for setup in setups:
        size = setup.get("size_id")
        need(setup.get("catalog_id") == "S003" and setup.get("definition_id") == "skirmish.s003" and
             setup.get("content_version") == 2 and setup.get("seed") == 104732 and
             setup.get("difficulty_id") == "Regular" and setup.get("compiled") is True and
             setup.get("setup_hash") == EXPECTED_SETUP_HASHES.get(size),
             "Exact current S003 compiled setup identity/hash required")
    matches = []
    for setup in setups:
        roles = setup.get("role_overlays")
        matches.append(unique(roles, lambda r: r.get("role_id") == "role.rifle"))
    need(all(r.get("role_kind") == "Rifle" and r.get("producer") == "Barracks" for r in matches),
         "Compiled S003 Rifle overlay must select Barracks")
    need(all(r == matches[0] for r in matches), "Compiled S003 Rifle overlay changed across size rows")
    need(source.get("kind") == "BarrackRifleCurrentAuthoredSourceFacts" and
         source.get("status") == "PassedSourceOnlyDescriptor" and
         source.get("scope", {}).get("generic_witness_establishes_s002_s003_membership") is False,
         "Exact source-only Barrack/Rifle descriptor with explicit non-membership scope required")
    facts = source.get("facts", {})
    producer, unit = facts.get("producer", {}), facts.get("unit", {})
    need(producer.get("key") == "Building_Barrack" and unit.get("key") == role["RuntimePrefabKey"],
         "Compiled overlay and authored role/source descriptor identities disagree")
    need(producer.get("prefab", {}).get("path") == "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab" and
         unit.get("prefab", {}).get("path") == "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab",
         "Exact source prefab identities required")
    need(unit.get("config", {}).get("path") == role["SourceConfigPath"] and
         producer.get("config", {}).get("path") == "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset",
         "Exact current role→unit-config and producer-config paths required")
    for path in (unit["config"]["path"], producer["config"]["path"],
                 unit["prefab"]["path"], producer["prefab"]["path"]):
        need(path in source.get("source_pins", {}), "Required S003 diagnostic source pin absent: " + path)
    recipe = unique(producer.get("recipes", []), lambda r: r.get("unit_key") == role["RuntimePrefabKey"])
    need(recipe.get("index") == 0 and recipe.get("quantity") == matches[0].get("squad_members") and
         recipe.get("prefab", {}).get("guid") == unit.get("prefab", {}).get("guid") and
         recipe.get("prefab", {}).get("local_id") == unit.get("prefab", {}).get("local_id"),
         "Current authored single Barrack Rifle recipe must agree with actual compiled overlay quantity/identity")
    transport = source.get("facts", {}).get("transport_unit")
    unit_transport = unit.get("transport")
    need(isinstance(transport, dict) and isinstance(unit_transport, dict) and
         transport.get("key") == "Unit_Veh_Helicopter_Transport" and
         transport.get("prefab", {}).get("path") == unit_transport.get("path") and
         transport.get("prefab", {}).get("guid") == unit_transport.get("guid") and
         transport.get("prefab", {}).get("local_id") == unit_transport.get("local_id") and
         transport.get("config", {}).get("path") == unit_transport.get("config_path") and
         transport.get("config", {}).get("guid") == unit_transport.get("config_guid"),
         "Exact product→transport source identity/config link required")
    need(unit.get("transport_max_concurrent") == 1 and
         transport.get("production_transport_max_concurrent") == 2 and
         unit.get("transport_arrival_seconds") == transport.get("production_transport_arrival_seconds") == 5.0 and
         unit.get("transport_hold_for_next_ready_seconds") == transport.get("production_transport_hold_for_next_ready_seconds") == 4.0,
         "Product authoring and referenced transport metadata values required")
    return {"role": role, "compiled_overlay": matches[0], "producer": producer, "unit": unit,
            "recipe": recipe, "transport": transport}


def add_role_overlay_output(binding: str) -> str:
    exact = '''foreach (var setup in setups.Where(x => x != null && x.CatalogId == "S003"))
            results.Add(new JObject { ["catalog_id"] = setup.CatalogId, ["definition_id"] = setup.DefinitionId,
                ["content_version"] = setup.ContentVersion, ["seed"] = setup.Seed,
                ["size_id"] = setup.SizeId.ToString(), ["difficulty_id"] = setup.DifficultyId.ToString(),
                ["setup_hash"] = setup.SetupHash, ["compiled"] = true });'''
    replacement = '''foreach (var setup in setups.Where(x => x != null && x.CatalogId == "S003"))
        {
            var roleOverlays = new JArray();
            foreach (var role in setup.RoleOverlays ?? Array.Empty<SkirmishRoleOverlay>())
                roleOverlays.Add(new JObject { ["role_id"] = role.RoleId, ["role_kind"] = role.RoleKind.ToString(),
                    ["producer"] = role.Producer.ToString(), ["squad_members"] = role.SquadMembers,
                    ["materials_cost"] = role.MaterialsCost, ["supply_cost"] = role.SupplyCost });
            results.Add(new JObject { ["catalog_id"] = setup.CatalogId, ["definition_id"] = setup.DefinitionId,
                ["content_version"] = setup.ContentVersion, ["seed"] = setup.Seed,
                ["size_id"] = setup.SizeId.ToString(), ["difficulty_id"] = setup.DifficultyId.ToString(),
                ["setup_hash"] = setup.SetupHash, ["compiled"] = true, ["role_overlays"] = roleOverlays });
        }'''
    need(binding.count(exact) == 1, "Current original compiler harness result shape changed")
    return binding.replace(exact, replacement)


def run(output: Path) -> dict:
    output = output.resolve()
    need(not output.exists(), "Fresh output directory required")
    need(sha(GRAPH.read_bytes()) == GRAPH_SHA, "Current T26 S003 graph hash changed")
    need(sha(SOURCE_DESCRIPTOR.read_bytes()) == SOURCE_DESCRIPTOR_SHA, "Current T17 source descriptor changed")
    compiler_receipt = json.loads(COMPILER_RECEIPT.read_text())
    build_receipt_bytes = BUILD_RECEIPT.read_bytes()
    need(sha(COMPILER_RECEIPT.read_bytes()) == COMPILER_RECEIPT_SHA and
         sha(build_receipt_bytes) == BUILD_RECEIPT_SHA,
         "Pinned T26 compiler/build receipts changed")
    need(compiler_receipt.get("kind") == "S003OriginalCompilerHarnessReceipt" and
         compiler_receipt.get("status") == "Compiled" and
         compiler_receipt.get("source_graph", {}).get("path") == str(GRAPH) and
         compiler_receipt.get("source_graph", {}).get("sha256") == GRAPH_SHA and
         compiler_receipt.get("harness_source", {}).get("path") == str(CAPTURE_BINDING) and
         compiler_receipt.get("harness_source", {}).get("sha256") == CAPTURE_BINDING_SHA and
         sha(CAPTURE_BINDING.read_bytes()) == CAPTURE_BINDING_SHA and
         compiler_receipt.get("source_response", {}).get("path") == str(COMPILER_RESPONSE) and
         compiler_receipt.get("seed") == 104732,
         "Exact passed T26 original compiler harness provenance required")
    validate_build_binding(compiler_receipt, BUILD_RECEIPT, sha(build_receipt_bytes))
    need(sha(COMPILER_RESPONSE.read_bytes()) == compiler_receipt["source_response"]["sha256"],
         "T26 compiler response changed")
    graph = json.loads(GRAPH.read_text())
    need(compiler_receipt.get("source_graph_source_pins") == graph.get("source_pins"),
         "T26 original compiler receipt source pin closure differs from graph")
    for path, digest in graph["source_pins"].items():
        need(Path(path).is_file() and sha(Path(path).read_bytes()) == digest, "T26 graph pin drift: " + path)
    source = json.loads(SOURCE_DESCRIPTOR.read_text())
    for relative, digest in source["source_pins"].items():
        path = ROOT / relative
        need(path.is_file() and sha(path.read_bytes()) == digest, "Barrack/Rifle source pin drift: " + relative)
    need(TRANSPORT_RESOLVER in source["source_pins"], "Pinned original transport resolver source absent")
    resolver_text = (ROOT / TRANSPORT_RESOLVER).read_text()
    validate_transport_resolver(resolver_text)
    binding = CAPTURE_BINDING.read_text()
    generated_binding = add_role_overlay_output(binding)
    output.mkdir(parents=True)
    patched = output / "S003OriginalCompilerSourceBinding.cs"
    patched.write_text(generated_binding)
    args = [json.loads(line) for line in COMPILER_RESPONSE.read_text().splitlines() if line.strip()]
    for i, arg in enumerate(args):
        if arg.startswith("-out:"):
            args[i] = "-out:" + str(output / "S003OriginalCompiler.exe")
        elif arg.startswith("-main:"):
            args[i] = "-main:S003OriginalCompilerSourceBinding"
    args.append(str(patched))
    rsp = output / "compile.rsp"
    rsp.write_text("\n".join(json.dumps(arg) for arg in args) + "\n")
    # Use the already-pinned toolchain and source host from the T26 compiler build receipt.
    build = json.loads(BUILD_RECEIPT.read_text())
    need(build.get("status") == "Passed" and build.get("mission") == "S002" and build.get("compile_exit") == 0,
         "Passed source-host build receipt required")
    for pinned_path, pinned_sha in build.get("source_and_reference_pins", {}).items():
        need(Path(pinned_path).is_file() and sha(Path(pinned_path).read_bytes()) == pinned_sha,
             "T26 source-host pin drift: " + pinned_path)
    build_compiler = build.get("compiler_response", {})
    need(build_compiler.get("path") == str(COMPILER_RESPONSE) and
         build_compiler.get("sha256") == sha(COMPILER_RESPONSE.read_bytes()),
         "Current T26 source build response must equal the harness source response")
    pins = build.get("source_and_reference_pins", {})
    csc = next((Path(p) for p in pins if re.search(r"/sdk/[0-9.]+/Roslyn/bincore/csc\.dll$", p)), None)
    dotnet = next((Path(p) for p in pins if p.endswith("/dotnet")), None)
    runtime = build.get("runtime_selection", {})
    mono, newtonsoft = Path(runtime["mono"]), Path(runtime["newtonsoft"])
    need(csc and csc.is_file() and dotnet and dotnet.is_file() and mono.is_file() and newtonsoft.is_file(),
         "Pinned T26 source compiler/runtime required")
    shutil.copy2(newtonsoft, output / "Newtonsoft.Json.dll")
    compile_result = subprocess.run([str(dotnet), str(csc), "@" + str(rsp)], capture_output=True,
                                    text=True, timeout=180)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    need(compile_result.returncode == 0, "Supplemental overlay compiler failed; see " + str(output / "compile.log"))
    matrix = graph["setup_matrix"]["path"]
    runtime_dirs = sorted({str(Path(arg[3:]).resolve().parent) for arg in args
                           if arg.startswith("-r:") and Path(arg[3:]).is_file()})
    import os
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join(runtime_dirs)
    invocation = subprocess.run([str(mono), str(output / "S003OriginalCompiler.exe"), str(GRAPH),
                                 matrix, "104732"], capture_output=True, text=True, timeout=120, env=env)
    (output / "run.log").write_text(invocation.stdout + invocation.stderr)
    need(invocation.returncode == 0, "Supplemental original compiler invocation failed; see " + str(output / "run.log"))
    compiler = json.loads(invocation.stdout.strip().splitlines()[-1])
    validate_historical_setup_match(compiler, compiler_receipt)
    # Controls mutate facts taken from the real current graph/compiler output and must fail closed.
    joined = validate_join(graph, compiler, source)
    bad_compiler = json.loads(json.dumps(compiler))
    bad_compiler["setups"][0]["role_overlays"][0]["producer"] = "GroundStaging"
    expect_reject(lambda: validate_join(graph, bad_compiler, source), "producer mutation")
    bad_compiler = json.loads(json.dumps(compiler))
    bad_compiler["setups"].append(json.loads(json.dumps(bad_compiler["setups"][0])))
    expect_reject(lambda: validate_join(graph, bad_compiler, source), "duplicate size row")
    bad_compiler = json.loads(json.dumps(compiler))
    bad_compiler["setups"][0]["seed"] = 104731
    expect_reject(lambda: validate_join(graph, bad_compiler, source), "compiled seed mutation")
    bad_compiler = json.loads(json.dumps(compiler))
    bad_compiler["setups"][0]["setup_hash"] ^= 1
    expect_reject(lambda: validate_join(graph, bad_compiler, source), "compiled setup hash mutation")
    bad_receipt = json.loads(json.dumps(compiler_receipt))
    bad_receipt["result"]["setups"][0]["setup_hash"] ^= 1
    expect_reject(lambda: validate_historical_setup_match(compiler, bad_receipt), "original receipt setup hash mismatch")
    bad_receipt = json.loads(json.dumps(compiler_receipt))
    bad_receipt["source_build"]["sha256"] = "0" * 64
    expect_reject(lambda: validate_build_binding(bad_receipt, BUILD_RECEIPT, sha(build_receipt_bytes)),
                   "original receipt build mismatch")
    bad_graph = json.loads(json.dumps(graph))
    role_node = next(n for n in bad_graph["nodes"] if n.get("source_path", "").endswith("SkirmishRoleCatalog_V1.asset"))
    role_node["fields"]["roles"].append(json.loads(json.dumps(joined["role"])))
    expect_reject(lambda: validate_join(bad_graph, compiler, source), "duplicate role")
    bad_source = json.loads(json.dumps(source))
    bad_source["facts"]["unit"]["prefab"]["path"] = "Assets/Game/Prefabs/Characters/Unit.prefab"
    expect_reject(lambda: validate_join(graph, compiler, bad_source), "prefab path mutation")
    bad_source = json.loads(json.dumps(source))
    bad_source["facts"]["unit"]["config"]["path"] = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Config.asset"
    expect_reject(lambda: validate_join(graph, compiler, bad_source), "role config mutation")
    bad_source = json.loads(json.dumps(source))
    bad_source["facts"]["transport_unit"]["production_transport_max_concurrent"] = 1
    expect_reject(lambda: validate_join(graph, compiler, bad_source), "transport concurrency mutation")
    bad_source = json.loads(json.dumps(source))
    bad_source["facts"]["unit"]["transport"]["guid"] = "00000000000000000000000000000000"
    expect_reject(lambda: validate_join(graph, compiler, bad_source), "transport reference mutation")
    bad_resolver = resolver_text.replace("maxConcurrent = Mathf.Max(2, maxConcurrent);",
                                        "maxConcurrent = Mathf.Max(1, maxConcurrent);", 1)
    expect_reject(lambda: validate_transport_resolver(bad_resolver), "helicopter concurrency floor mutation")
    bad_compiler = json.loads(json.dumps(compiler))
    bad_compiler["setups"][0]["role_overlays"].append(json.loads(json.dumps(joined["compiled_overlay"])))
    expect_reject(lambda: validate_join(graph, bad_compiler, source), "duplicate compiled overlay")
    # Recheck every external input after compile and execution so a mid-run change cannot
    # produce a receipt that appears bound to the inputs validated before invocation.
    need(sha(GRAPH.read_bytes()) == GRAPH_SHA and
         sha(SOURCE_DESCRIPTOR.read_bytes()) == SOURCE_DESCRIPTOR_SHA and
         sha(COMPILER_RECEIPT.read_bytes()) == COMPILER_RECEIPT_SHA and
         sha(BUILD_RECEIPT.read_bytes()) == BUILD_RECEIPT_SHA and
         sha(COMPILER_RESPONSE.read_bytes()) == compiler_receipt["source_response"]["sha256"] and
         sha(CAPTURE_BINDING.read_bytes()) == CAPTURE_BINDING_SHA,
         "Pinned diagnostic inputs changed during compile/run")
    for path, digest in graph["source_pins"].items():
        need(Path(path).is_file() and sha(Path(path).read_bytes()) == digest,
             "T26 graph pin drift after compile/run: " + path)
    for relative, digest in source["source_pins"].items():
        path = ROOT / relative
        need(path.is_file() and sha(path.read_bytes()) == digest,
             "Barrack/Rifle source pin drift after compile/run: " + relative)
    need((ROOT / TRANSPORT_RESOLVER).read_text() == resolver_text,
         "Pinned original transport resolver changed during compile/run")
    result = {"schema_version": 1, "kind": "S003SingleRifleSourceRoleRecipeDiagnostic",
              "status": "PassedSourceOnlyDiagnostic", "target": {"catalog_id": "S003", "seed": 104732,
              "compiled_sizes": [{"size_id": s["size_id"], "setup_hash": s["setup_hash"]} for s in compiler["setups"]]},
              "role": joined["role"], "compiled_role_overlay": joined["compiled_overlay"],
              "producer_source": joined["producer"], "unit_source": joined["unit"],
              "authored_recipe": joined["recipe"],
              "transport_resolution": {"rifle_authored_max_concurrent": joined["unit"]["transport_max_concurrent"],
                                        "referenced_transport_authored_max_concurrent": joined["transport"]["production_transport_max_concurrent"],
                                        "effective_max_concurrent": max(2, joined["transport"]["production_transport_max_concurrent"]),
                                        "arrival_seconds": joined["transport"]["production_transport_arrival_seconds"],
                                        "hold_seconds": joined["transport"]["production_transport_hold_for_next_ready_seconds"],
                                        "resolution_basis": "Pinned original queue resolver copies referenced transport UnitProductionMetadata then applies Mathf.Max(2, maxConcurrent) for Unit_Veh_Helicopter_Transport"},
              "source_pins": {**graph["source_pins"], **source["source_pins"]},
              "inputs": {"graph_sha256": GRAPH_SHA, "source_descriptor_sha256": sha(SOURCE_DESCRIPTOR.read_bytes()),
                         "compiler_binding_sha256": sha(binding.encode()), "generated_binding_sha256": sha(generated_binding.encode())},
              "negative_controls": ["compiled_overlay_producer_mutation_rejected", "duplicate_role_row_rejected",
                                    "source_prefab_identity_mutation_rejected", "role_config_path_mutation_rejected",
                                    "duplicate_compiled_overlay_rejected", "duplicate_setup_size_rejected",
                                    "compiled_seed_mutation_rejected", "compiled_setup_hash_mutation_rejected",
                                    "original_receipt_setup_hash_mismatch_rejected", "original_receipt_build_mismatch_rejected",
                                    "transport_concurrency_mutation_rejected", "transport_reference_mutation_rejected",
                                    "helicopter_resolver_floor_mutation_rejected"],
              "scope": {"source_only": True, "compiled_setup_role_overlay": True, "one_role": "role.rifle",
                        "factory_closure": False, "unity_factory_invoked": False, "prefab_bake_claimed": False,
                        "runtime_admission": False, "mission_world_created": False,
                        "physical_source_fit_verified": False, "mission_readiness": False}}
    (output / "diagnostic.json").write_text(json.dumps(result, indent=2) + "\n")
    receipt = {"kind": "S003SingleRifleSourceRoleRecipeDiagnosticReceipt", "status": "PassedSourceOnlyDiagnostic",
              "diagnostic": {"path": str(output / "diagnostic.json"), "sha256": sha((output / "diagnostic.json").read_bytes())},
              "runner": {"path": str(Path(__file__).resolve()), "sha256": sha(Path(__file__).read_bytes())},
              "source_harness": {"path": str(patched), "sha256": sha(patched.read_bytes())},
              "compiler_response": {"path": str(rsp), "sha256": sha(rsp.read_bytes())},
              "compile_log": {"path": str(output / "compile.log"), "sha256": sha((output / "compile.log").read_bytes())},
              "run_log": {"path": str(output / "run.log"), "sha256": sha((output / "run.log").read_bytes())},
              "t26_graph": {"path": str(GRAPH), "sha256": GRAPH_SHA},
              "source_descriptor": {"path": str(SOURCE_DESCRIPTOR), "sha256": SOURCE_DESCRIPTOR_SHA},
              "source_build": {"path": str(BUILD_RECEIPT), "sha256": sha(BUILD_RECEIPT.read_bytes())},
              "original_compiler_receipt": {"path": str(COMPILER_RECEIPT), "sha256": sha(COMPILER_RECEIPT.read_bytes())},
              "scope": result["scope"]}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    return result


def expect_reject(action, name):
    try:
        action()
    except ValueError:
        return
    raise ValueError("Negative control accepted: " + name)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    try:
        result = run(Path(args.output))
        print(json.dumps({"status": result["status"], "role": result["role"]["RoleId"],
                          "source_only": True, "factory_closure": False, "runtime_admission": False}))
        return 0
    except Exception as error:
        print("S003 single-Rifle source diagnostic failed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

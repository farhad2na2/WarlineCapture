#!/usr/bin/env python3
"""Extend the immutable S002 authored graph with its exact grid asset and decode physical inputs.

The control uses the existing S002 graph/build and the original MapSurfaceDataAsset decoder.
It does not load a Unity scene, run an Editor/Baker, reconstruct map meshes, process water,
initialize a World, or admit a mission.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[5]
GRAPH = ROOT / "T Logs/SkirmishProduction/s002-current-authored-graph-20261103-T1/source-config-graph.json"
GRAPH_SHA = "87f9aa0d7f38e7dd4446ab2739086e6905325195b790783394aceac72faf20bf"
PROOF = ROOT / "T Logs/SkirmishProduction/s002-current-authored-graph-20261103-T1/graph-proof.json"
PROOF_SHA = "d87ed025841435e3b5152c4fd45265c72f1c5e688d581c1366f3869f7922e2ab"
GRID_PATH = "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset"
GRID_GUID = "b201000000000000000000000000000b"
GRID_SHA = "8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb"
SURFACE_PATH = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset"
SURFACE_GUID = "12f517deb32ab49698acbfdaf7c3eac7"
SURFACE_SHA = "5447c779e2b68ba6b7c6f86a1146f6bf570d49b4455a676a2680ea595fa47d1d"
SURFACE_META_SHA = "16b2032b6083f05562d108232c9438e2dc2b62210bc732ea94b067842fa0b7cb"
MAP_PATH = "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset"
MAP_SHA = "8a03d36fd1d4d59a180c65d82f1c708fd70e7c291f49d480c80629a80ec728c0"
MATERIALIZER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/051_SourceConfigMaterializer.cs"
ADAPTER = Path(__file__).with_name("S002PhysicalInputSourceBinding.cs.txt")
MAX_ASSET_BYTES = 4 * 1024 * 1024
HELPER_PATH = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/generate_s004_typed_config_graph.py"
OWNER_PATHS = (
    "Assets/Game/Scripts/Authorings/GridAuthoring.Baker.cs",
    "Assets/Game/Scripts/Authorings/GridAuthoring.cs",
    "Assets/Game/Scripts/Configs/Scene/GridAuthoringSceneConfigAsset.cs",
    "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
    "Assets/Game/Scripts/Configs/MapSurfaceDataAsset.cs",
    "Assets/Game/Scripts/Components/MapSurfaceComponents.cs",
    "Assets/Game/Scripts/Components/MapSurfaceBlobAccess.cs",
)


def sha_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha_file(path: Path) -> str:
    return sha_bytes(path.read_bytes())


def hash128_source_path(repo: Path) -> Path:
    matches = sorted((repo / "Library/PackageCache").glob("com.unity.entities@*/Unity.Entities/Types/Hash128.cs"))
    need(len(matches) == 1, "one installed Unity.Entities Hash128 source required")
    return matches[0]


def need(ok: bool, why: str) -> None:
    if not ok:
        raise ValueError(why)


def load_json(path: Path) -> dict:
    def unique(pairs):
        result = {}
        for key, value in pairs:
            need(key not in result, "duplicate JSON property: " + key)
            result[key] = value
        return result
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique)


def helpers_module():
    spec = importlib.util.spec_from_file_location("s002_physical_graph_helpers", HELPER_PATH)
    module = importlib.util.module_from_spec(spec)
    need(spec is not None and spec.loader is not None, "strict source graph helper module unavailable")
    spec.loader.exec_module(module)
    return module


def verify_file(path: Path, digest: str, maximum: int) -> bytes:
    need(path.is_file() and not path.is_symlink(), "regular pinned source file required: " + str(path))
    raw = path.read_bytes()
    need(0 < len(raw) <= maximum, "source file outside byte bound: " + str(path))
    need(sha_bytes(raw) == digest, "pinned source changed: " + str(path))
    return raw


def add_grid_node(repo: Path, original: dict, proof: dict, *, base_graph_path: Path | None = None,
                  base_graph_sha256: str | None = None, parent_graph_sha256: str | None = None,
                  base_graph_proof_sha256: str | None = None) -> dict:
    """Return a separate graph extension; retain the legacy T1 defaults for its original control."""
    h = helpers_module()
    assets = repo / "Assets"
    target = h.checked_asset_path(assets, repo / GRID_PATH)
    meta, _ = h.checked_meta_target(assets, Path(str(target) + ".meta"))
    need(sha_file(target) == GRID_SHA, "exact T1 grid asset hash mismatch")
    need(sha_file(meta) == "7e654c06fb69f8ce3f8a2c4c06db1d6d54058ad13f5daac8f8baf8d0579959c2",
         "exact T1 grid asset meta hash mismatch")
    meta_guids = re.findall(rb"^guid: ([0-9a-f]{32})$", meta.read_bytes(), re.M)
    need(meta_guids == [GRID_GUID.encode()], "grid asset GUID does not match T1 proof")
    target_meta = proof["geometry_source_targets"]["gridMetadata"]
    need(target_meta == {"guid": GRID_GUID, "path": GRID_PATH, "sha256": GRID_SHA, "bytes": target.stat().st_size},
         "grid target differs from pinned T1 graph proof")
    surface_targets = proof["geometry_source_targets"]
    for key in ("surfaceMetadata", "mapSurfaceDataReference"):
        need(surface_targets.get(key) == {"guid": SURFACE_GUID, "path": SURFACE_PATH,
                                          "sha256": SURFACE_SHA, "bytes": (repo / SURFACE_PATH).stat().st_size},
             "surface target differs from pinned T1 graph proof: " + key)
    surface_meta = repo / (SURFACE_PATH + ".meta")
    verify_file(surface_meta, SURFACE_META_SHA, MAX_ASSET_BYTES)
    need(re.findall(rb"^guid: ([0-9a-f]{32})$", surface_meta.read_bytes(), re.M) == [SURFACE_GUID.encode()],
         "surface asset GUID does not match T1 proof")

    parsed = h.strict_read(repo / h.READER_REL, [target])[str(target)]
    mono = parsed.get("MonoBehaviour")
    need(isinstance(mono, dict), "grid config must parse as one MonoBehaviour")
    identifier = mono.get("m_EditorClassIdentifier", "")
    need(identifier is None,
         "exact T1 grid asset schema requires a blank m_EditorClassIdentifier resolved through m_Script")
    script = mono.get("m_Script")
    need(isinstance(script, dict) and script.get("fileID") == 11500000 and script.get("type") == 3,
         "exact grid MonoScript reference required")
    script_guid = script.get("guid")
    script_hits = h.guid_hits(assets, script_guid)
    need(len(script_hits) == 1, "grid MonoScript GUID must resolve uniquely")
    script_meta, script_path = h.checked_meta_target(assets, script_hits[0])
    need(script_path.relative_to(repo).as_posix() == "Assets/Game/Scripts/Configs/Scene/GridAuthoringSceneConfigAsset.cs",
         "grid MonoScript must resolve to original GridAuthoringSceneConfigAsset owner")
    need(script_path.stem == "GridAuthoringSceneConfigAsset", "grid script class/file mismatch")
    owner_source = script_path.read_text(encoding="utf-8")
    need("namespace Game.Configs" in owner_source and
         re.search(r"\bpublic\s+sealed\s+class\s+GridAuthoringSceneConfigAsset\s*:\s*GridAuthoringConfig\b", owner_source),
         "resolved MonoScript must declare the exact Game.Configs grid asset type")
    spellings = {key[len("MonoBehaviour."):]: value
                 for key, value in parsed.get("__source_scalar_spellings", {}).items()
                 if key.startswith("MonoBehaviour.")}
    need(spellings, "strict grid serialized scalar spellings required")
    fields = {key: value for key, value in mono.items() if not key.startswith("m_")}
    node = {"asset_guid": GRID_GUID, "source_type": "GridAuthoringSceneConfigAsset", "source_path": GRID_PATH,
            "source_sha256": GRID_SHA, "fields": fields, "scalar_spellings": spellings}

    ext = json.loads(json.dumps(original))
    nodes = ext.get("nodes")
    need(isinstance(nodes, list) and len(nodes) < 64, "bounded authored graph node capacity required")
    map_nodes = [row for row in nodes if row.get("source_path") == MAP_PATH]
    surface_nodes = [row for row in nodes if row.get("source_path") == SURFACE_PATH]
    need(len(map_nodes) == 1 and map_nodes[0].get("source_type") == "OperationMapDefinition" and
         map_nodes[0].get("source_sha256") == MAP_SHA, "exact pinned S002 map node required")
    need(len(surface_nodes) == 1 and surface_nodes[0].get("source_type") == "MapSurfaceDataAsset" and
         surface_nodes[0].get("asset_guid") == SURFACE_GUID and surface_nodes[0].get("source_sha256") == SURFACE_SHA,
         "exact pinned S002 surface node required")
    need(not any(row.get("asset_guid") == GRID_GUID or row.get("source_path") == GRID_PATH for row in nodes),
         "grid node already present or GUID collision")
    nodes.append(node)
    nodes.sort(key=lambda row: row["source_path"])
    pins = ext.get("source_pins")
    need(isinstance(pins, dict), "source pin map required")
    additions = {str(target): GRID_SHA, str(meta): sha_file(meta),
                 str(script_path): sha_file(script_path), str(script_meta): sha_file(script_meta)}
    for path, digest in additions.items():
        prior = pins.get(path)
        need(prior is None or prior == digest, "grid extension source pin conflicts: " + path)
        pins[path] = digest
    selected_base_path = Path(base_graph_path) if base_graph_path is not None else GRAPH
    selected_base_sha = base_graph_sha256 or GRAPH_SHA
    if base_graph_path is not None:
        need(proof.get("graph") == {"path": str(selected_base_path), "sha256": selected_base_sha},
             "explicit base graph path/hash must match its checked source proof")
        need(re.fullmatch(r"[0-9a-f]{64}", parent_graph_sha256 or "") is not None and
             re.fullmatch(r"[0-9a-f]{64}", base_graph_proof_sha256 or "") is not None,
             "selected-map parent graph and source proof digests required")
    extension = {
        "kind": "S002ExactGridAuthoringConfigNode",
        "base_graph_path": str(selected_base_path), "base_graph_sha256": selected_base_sha,
        "added_source_path": GRID_PATH, "added_asset_guid": GRID_GUID,
        "added_source_sha256": GRID_SHA,
        "scope": "Typed current GridAuthoringSceneConfigAsset materialization only; original graph remains immutable"
    }
    if base_graph_path is not None:
        extension.update({
            "scope": "Typed current GridAuthoringSceneConfigAsset materialization only; selected-map graph input remains immutable",
            "base_graph_proof_sha256": base_graph_proof_sha256,
            "parent_graph_sha256": parent_graph_sha256,
            "added_script_guid": script_guid,
            "added_meta_sha256": sha_file(meta),
            "added_script_path": str(script_path.relative_to(repo)),
            "added_script_sha256": sha_file(script_path),
            "added_script_meta_sha256": sha_file(script_meta),
        })
    ext["source_graph_extension"] = extension
    return ext


def descriptor_path_and_sha(value, fallback_sha=None, repo: Path | None = None) -> tuple[Path, str]:
    if isinstance(value, dict):
        path, digest = value.get("path"), value.get("sha256", fallback_sha)
    else:
        path, digest = value, fallback_sha
    need(isinstance(path, str) and path and isinstance(digest, str) and
         re.fullmatch(r"[0-9a-f]{64}", digest) is not None,
         "receipt must provide an exact path and SHA-256 binding")
    result = Path(path)
    if not result.is_absolute() and repo is not None:
        result = repo / result
    return result, digest


def validate_trust(repo: Path, build_receipt_arg: Path, setup_receipt_arg: Path) -> tuple[dict, dict, dict, Path, Path, Path, dict]:
    verify_file(GRAPH, GRAPH_SHA, 16 * 1024 * 1024)
    verify_file(PROOF, PROOF_SHA, 4 * 1024 * 1024)
    verify_file(build_receipt_arg, sha_file(build_receipt_arg), 16 * 1024 * 1024)
    verify_file(setup_receipt_arg, sha_file(setup_receipt_arg), 16 * 1024 * 1024)
    build_receipt_path = build_receipt_arg.resolve(strict=True)
    setup_receipt_path = setup_receipt_arg.resolve(strict=True)
    build_receipt_sha = sha_file(build_receipt_path)
    setup_receipt_sha = sha_file(setup_receipt_path)
    proof = load_json(PROOF)
    graph = load_json(GRAPH)
    compile_receipt = load_json(setup_receipt_path)
    build = load_json(build_receipt_path)
    need(proof.get("kind") == "S002CurrentAuthoredConfigGraph" and proof.get("status") == "Passed" and
         proof.get("graph", {}).get("path") == str(GRAPH) and proof.get("graph", {}).get("sha256") == GRAPH_SHA,
         "passed exact S002 T1 graph proof required")
    need(compile_receipt.get("compiler_accepted") is True,
         "current S002 setup compile receipt must explicitly record compiler acceptance")
    graph_path, graph_sha = descriptor_path_and_sha(
        compile_receipt.get("source_config_graph", compile_receipt.get("source_graph")),
        compile_receipt.get("source_config_graph_sha256", compile_receipt.get("source_graph_sha256")), repo)
    need(graph_path.resolve(strict=True) == GRAPH.resolve(strict=True) and graph_sha == GRAPH_SHA,
         "setup compile receipt must bind exact immutable S002 T1 graph")
    linked_build_path, linked_build_sha = descriptor_path_and_sha(
        compile_receipt.get("build_receipt", compile_receipt.get("source_build")),
        compile_receipt.get("build_receipt_sha256", compile_receipt.get("source_build_sha256")), repo)
    need(linked_build_path.resolve(strict=True) == build_receipt_path and linked_build_sha == build_receipt_sha,
         "setup compile receipt must bind the supplied current S002 source build by path and hash")
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("mission") == "S002" and build.get("compile_exit") == 0,
         "passed S002 source build required")
    if "build_pin_count" in compile_receipt:
        need(compile_receipt["build_pin_count"] == len(build.get("source_and_reference_pins", {})),
             "setup compile receipt build pin count differs from supplied build")
    build_pins = build.get("source_and_reference_pins", {})
    need(isinstance(build_pins, dict) and build_pins, "current source build must include source/reference pins")
    owner_pins = {}
    for rel in OWNER_PATHS:
        absolute = str(repo / rel)
        digest = build_pins.get(absolute)
        need(isinstance(digest, str) and re.fullmatch(r"[0-9a-f]{64}", digest) is not None,
             "current source build does not pin required original owner: " + rel)
        verify_file(repo / rel, digest, 8 * 1024 * 1024)
        owner_pins[rel] = digest
    verify_file(MATERIALIZER, sha_file(MATERIALIZER), 2 * 1024 * 1024)
    verify_file(HELPER_PATH, sha_file(HELPER_PATH), 8 * 1024 * 1024)
    need(graph.get("compiled_setup") is False and graph.get("world_initialized") is False and
         graph.get("native_world_state_used") is False and proof.get("compiled_setup") is False and
         proof.get("world_initialized") is False and proof.get("mission_ready") is False,
         "base graph and its proof must retain the data-only, no-world, not-ready scope")
    need(proof.get("geometry_source_targets", {}).get("gridMetadata", {}).get("path") == GRID_PATH and
         proof.get("geometry_source_targets", {}).get("surfaceMetadata", {}).get("path") == SURFACE_PATH,
         "exact S002 current grid and surface target identities required")
    for path, digest in build.get("source_and_reference_pins", {}).items():
        verify_file(Path(path), digest, 512 * 1024 * 1024)
    for path, digest in graph.get("source_pins", {}).items():
        verify_file(Path(path), digest, 64 * 1024 * 1024)
    rsp = Path(build["compiler_response"]["path"])
    verify_file(rsp, build["compiler_response"]["sha256"], 16 * 1024 * 1024)
    hash_source = hash128_source_path(repo)
    verify_file(hash_source, sha_file(hash_source), 4 * 1024 * 1024)
    return proof, graph, build, rsp, build_receipt_path, setup_receipt_path, {
        "owner_pins": owner_pins,
        "materializer_sha256": sha_file(MATERIALIZER),
        "helper_sha256": sha_file(HELPER_PATH),
        "hash128_source_path": hash_source,
        "hash128_source_sha256": sha_file(hash_source),
    }


def run_controls(repo: Path, graph_path: Path, exe: Path, output: Path, build: dict, env: dict) -> list[dict]:
    runtime = build["runtime_selection"]
    mono = runtime["mono"]
    cases = ["changed-grid-pin", "changed-surface-pin", "duplicate-grid-guid",
             "grid-guid-mismatch", "surface-guid-mismatch",
             "out-of-bounds-blocked-cell", "duplicate-blocked-cell", "grid-frame-mismatch",
             "surface-frame-mismatch", "runtime-hash-mismatch", "corrupt-surface-payload"]
    results = []
    for case in cases:
        proc = subprocess.run([mono, str(exe), str(graph_path), str(repo), str(output), case],
                              capture_output=True, text=True, timeout=120, env=env)
        (output / ("negative-" + case + ".log")).write_text(proc.stdout + proc.stderr, encoding="utf-8")
        need(proc.returncode == 0 and '"result":"RejectedAsExpected"' in proc.stdout,
             "negative control did not reject: " + case)
        results.append({"case": case, "result": "RejectedAsExpected"})
    proc = subprocess.run([mono, str(exe), str(graph_path), str(repo), str(output), "hash-text-parity"],
                          capture_output=True, text=True, timeout=120, env=env)
    (output / "control-hash-text-parity.log").write_text(proc.stdout + proc.stderr, encoding="utf-8")
    need(proc.returncode == 0 and '"result":"Passed"' in proc.stdout, "Hash128 text parity control failed")
    results.append({"case": "hash-text-parity", "result": "Passed", "vectors": 5})
    return results


def execute(repo: Path, output: Path, build_receipt_arg: Path, setup_receipt_arg: Path) -> dict:
    repo = repo.resolve(strict=True)
    output = output.absolute()
    need(not output.exists(), "fresh output directory required; refusing overwrite")
    proof, original, build, rsp_path, build_receipt_path, setup_receipt_path, input_pins = validate_trust(
        repo, build_receipt_arg, setup_receipt_arg)
    build_receipt_sha = sha_file(build_receipt_path)
    setup_receipt_sha = sha_file(setup_receipt_path)
    need(repo == ROOT.resolve(strict=True), "fixed S002 T1 source root required")
    runner_sha = sha_file(Path(__file__).resolve(strict=True))
    adapter_sha = sha_file(ADAPTER.resolve(strict=True))
    original_bytes = GRAPH.read_bytes()
    extended = add_grid_node(repo, original, proof)
    output.mkdir(parents=True)
    extended_path = output / "source-config-graph-with-grid.json"
    extended_path.write_text(json.dumps(extended, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    extended_sha = sha_file(extended_path)

    args = [json.loads(line) for line in rsp_path.read_text(encoding="utf-8").splitlines() if line.strip()]
    mains = [i for i, item in enumerate(args) if item.startswith("-main:")]
    outs = [i for i, item in enumerate(args) if item.startswith("-out:")]
    need(len(mains) == 1 and len(outs) == 1, "unique source compiler main/output switches required")
    args[mains[0]] = "-main:S002PhysicalInputSourceBinding"
    exe = output / "S002PhysicalInputSourceBinding.exe"
    args[outs[0]] = "-out:" + str(exe)
    materializer_slots = [i for i, item in enumerate(args)
                          if item.endswith("/Adapters/051_SourceConfigMaterializer.cs")]
    need(len(materializer_slots) == 1, "unique source materializer compile slot required")
    args[materializer_slots[0]] = str(MATERIALIZER.resolve(strict=True))
    args.append(str(ADAPTER.resolve(strict=True)))
    rsp_out = output / "compile.rsp"
    rsp_out.write_text("\n".join(json.dumps(item) for item in args) + "\n", encoding="utf-8")
    dotnet_candidates = [Path(p) for p in build["source_and_reference_pins"] if p.endswith("/dotnet")]
    csc_candidates = sorted(Path(p) for p in build["source_and_reference_pins"]
                            if re.search(r"/sdk/[0-9.]+/Roslyn/bincore/csc\.dll$", p))
    need(dotnet_candidates and csc_candidates, "pinned dotnet and Roslyn compiler required")
    compile_proc = subprocess.run([str(dotnet_candidates[0]), str(csc_candidates[-1]), "@" + str(rsp_out)],
                                  capture_output=True, text=True, timeout=180)
    (output / "compile.log").write_text(compile_proc.stdout + compile_proc.stderr, encoding="utf-8")
    need(compile_proc.returncode == 0, "S002 source physical-input adapter compile failed; compile.log preserved")

    ref_dirs = sorted({str(Path(item[3:]).resolve().parent) for item in args
                       if item.startswith("-r:") and Path(item[3:]).is_file()})
    runtime = build["runtime_selection"]
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([str(repo / "Library/ScriptAssemblies"), runtime["engine"],
                                        str(Path(runtime["newtonsoft"]).parent), *ref_dirs])
    mono = Path(runtime["mono"])
    need(mono.is_file(), "pinned Mono executable missing")
    runtime_paths = {k: Path(v).resolve(strict=True) for k, v in runtime.items()}
    helper = runtime_paths["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    mono_native = runtime_paths["mono"].parent.parent / "lib/libmono-native.dylib"
    need(helper.is_file() and mono_native.is_file(), "pinned Mono native helpers required")
    dllmaps = '<dllmap dll="MonoPosixHelper" target="' + str(helper) + '" os="!windows"/>'
    dllmaps += ''.join('<dllmap dll="' + name + '" target="' + str(mono_native) + '" os="!windows"/>'
                       for name in ("System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"))
    mono_config = output / "mono-config.xml"
    mono_config.write_text("<configuration>" + dllmaps + "</configuration>\n", encoding="utf-8")
    env["MONO_PATH"] = os.pathsep.join([str(repo / "Library/ScriptAssemblies"), runtime["engine"],
                                        str(Path(runtime["newtonsoft"]).parent), *ref_dirs])

    run_proc = subprocess.run([str(mono), "--config", str(mono_config), str(exe), str(extended_path), str(repo), str(output)],
                              capture_output=True, text=True, timeout=180, env=env)
    (output / "run.log").write_text(run_proc.stdout + run_proc.stderr, encoding="utf-8")
    need(run_proc.returncode == 0 and run_proc.stdout.strip(), "S002 physical-input projection failed; run.log preserved")
    projected = json.loads(run_proc.stdout.strip().splitlines()[-1])
    need(projected.get("kind") == "S002PinnedDesertBasePhysicalInputSourceProjection" and
         projected.get("status") == "Passed" and projected.get("mission_admission") is False,
         "S002 physical-input output kind/scope invalid")
    grid_result = projected.get("grid", {})
    surface_result = projected.get("surface", {}).get("decoded", {})
    expected_cells = 2048 * 1024
    need(grid_result.get("dimensions") == [2048, 1024] and
         grid_result.get("authored_blocked_cell_count") == 0 and
         grid_result.get("projected_walkable_cell_count") == expected_cells and
         grid_result.get("projection_byte_count") == expected_cells,
         "authored GridAuthoring blocked-cell projection differs from exact S002 inputs")
    need(surface_result.get("dimensions") == [2048, 1024] and
         surface_result.get("surface_count") == expected_cells and
         surface_result.get("connection_count") == 0 and
         surface_result.get("compact_sample_count") == expected_cells and
         surface_result.get("runtime_blob_hash") == "8810a336a366016217a73bf104b45a3a" and
         surface_result.get("runtime_blob_hash_metadata_match") is True and
         surface_result.get("compact_sample_record_bytes") == expected_cells * 16,
         "original surface decoder output differs from exact S002 payload/metadata")
    controls = run_controls(repo, extended_path, exe, output, build, env)

    # Recheck original immutable graph and all relevant source pins after execution.
    need(GRAPH.read_bytes() == original_bytes and sha_file(GRAPH) == GRAPH_SHA,
         "immutable T1 source graph changed during projection")
    verify_file(PROOF, PROOF_SHA, 4 * 1024 * 1024)
    verify_file(build_receipt_path, build_receipt_sha, 16 * 1024 * 1024)
    verify_file(setup_receipt_path, setup_receipt_sha, 16 * 1024 * 1024)
    verify_file(Path(__file__).resolve(strict=True), runner_sha, 2 * 1024 * 1024)
    verify_file(ADAPTER.resolve(strict=True), adapter_sha, 2 * 1024 * 1024)
    verify_file(MATERIALIZER, input_pins["materializer_sha256"], 2 * 1024 * 1024)
    verify_file(HELPER_PATH, input_pins["helper_sha256"], 8 * 1024 * 1024)
    verify_file(input_pins["hash128_source_path"], input_pins["hash128_source_sha256"], 4 * 1024 * 1024)
    for path, digest in build["source_and_reference_pins"].items():
        verify_file(Path(path), digest, 512 * 1024 * 1024)
    for path, digest in extended["source_pins"].items():
        verify_file(Path(path), digest, 64 * 1024 * 1024)

    result_path = output / "source-projection.json"
    need(result_path.is_file(), "source projection output missing")
    receipt = {
        "kind": "S002PinnedPhysicalInputSourceProjectionReceipt", "status": "Passed",
        "authority": "Exact immutable S002 T1 authored graph plus typed current GridAuthoringSceneConfigAsset extension and original MapSurfaceDataAsset decoder",
        "base_graph": {"path": str(GRAPH), "sha256": GRAPH_SHA, "unchanged_after_run": True},
        "graph_proof": {"path": str(PROOF), "sha256": PROOF_SHA},
        "setup_compile_receipt": {"path": str(setup_receipt_path), "sha256": setup_receipt_sha},
        "source_build": {"path": str(build_receipt_path), "sha256": build_receipt_sha,
                         "compiler_response_sha256": build["compiler_response"]["sha256"]},
        "extended_graph": {"path": str(extended_path), "sha256": extended_sha,
                           "added_grid_source_path": GRID_PATH, "added_grid_guid": GRID_GUID,
                           "added_grid_sha256": GRID_SHA},
        "adapter": {"path": str(ADAPTER), "sha256": sha_file(ADAPTER)},
        "runner": {"path": str(Path(__file__).resolve()), "sha256": runner_sha},
        "materializer": {"path": str(MATERIALIZER), "sha256": sha_file(MATERIALIZER)},
        "source_owner_pins": input_pins["owner_pins"],
        "projection": {"path": str(result_path), "sha256": sha_file(result_path)},
        "walkability": {"path": str(output / "authored-grid-walkable-emulation.u8"),
                        "sha256": sha_file(output / "authored-grid-walkable-emulation.u8")},
        "surface_samples": {"path": str(output / "authored-surface-samples.bin"),
                            "sha256": sha_file(output / "authored-surface-samples.bin")},
        "negative_controls": controls,
        "world_initialized": False, "unity_editor_or_baker_run": False,
        "native_scene_loaded": False, "scene_geometry_read": False, "water_processed": False,
        "physical_source_fit_verified": False, "mission_admission": False, "mission_readiness": False,
        "scope": "Authored grid walkability emulation and compact surface payload decode only; no scene/mesh reconstruction or end-to-end one-producer/one-rifle runtime claim"
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    receipt["receipt_path"] = str(output / "receipt.json")
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, type=Path, help="fresh evidence directory; existing paths are rejected")
    parser.add_argument("--build-receipt", required=True, type=Path,
                        help="current passed S002 source-build receipt")
    parser.add_argument("--setup-compile-receipt", required=True, type=Path,
                        help="current accepted S002 setup-compile receipt bound to the supplied build and T1 graph")
    args = parser.parse_args()
    try:
        result = execute(ROOT, args.output, args.build_receipt, args.setup_compile_receipt)
        print(json.dumps({"status": result["status"], "receipt": result["receipt_path"],
                          "world_initialized": False, "unity_editor_or_baker_run": False,
                          "mission_admission": False, "mission_readiness": False}, indent=2))
        return 0
    except Exception as error:
        print("S002 pinned physical-input projection failed closed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

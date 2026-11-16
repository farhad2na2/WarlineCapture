#!/usr/bin/env python3
"""Source-host decode/projection for S004's exact pinned CityEdge Grid/Surface inputs.

No Unity Editor, scene, world, Baker, physical-fit, or readiness claim is made.
The Grid walkability output is an emulation of GridAuthoring.Baker's authored
blocked-cell rule, not a captured Unity bake.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = Path(__file__).resolve().parents[5]
GENERATOR = HERE / "generate_s004_typed_config_graph.py"
ADAPTER = HERE / "S004PhysicalInputSourceBinding.cs.txt"
MATERIALIZER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/051_SourceConfigMaterializer.cs"
GRAPH_REL = "Logs/SkirmishProduction/s004-typed-config-graph-20261103-r3/source-config-graph.json"
EXPORT_RECEIPT_REL = "Logs/SkirmishProduction/s004-standard-setup-export-20261103-r1/receipt.json"
MAP_REL = "Assets/Game/Resources/SkirmishS004OperationMap.asset"
ASSET_REFS = {
    "grid_config": ("GridAuthoringSceneConfigAsset", "Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Grid.asset", "7c338e0bc78684837a39eed484be918e"),
    "surface_data": ("MapSurfaceDataAsset", "Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Surface.asset", "8f750533817554c5e8ec1d550cdd0554"),
}
MAX_ASSET_BYTES = 4 * 1024 * 1024
HASH128_SOURCE = ROOT / "Library/PackageCache/com.unity.entities@ef040875dea9/Unity.Entities/Types/Hash128.cs"
HASH128_SOURCE_SHA256 = "fc9989feabee6de0f1acdd54564b1ab523e520934e8c63b52c94f2871b62a2f5"


def sha_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def sha_file(path: Path) -> str:
    return sha_bytes(path.read_bytes())


def need(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def strict_parse(reader: Path, path: Path) -> dict:
    proc = subprocess.run(["ruby", str(reader), str(path)], capture_output=True, text=True, timeout=60)
    need(proc.returncode == 0, "strict YAML reader rejected " + str(path) + ": " + proc.stderr.strip())
    parsed = json.loads(proc.stdout)
    return parsed[str(path)]


def asset_node(repo: Path, graph: dict, reader: Path, type_name: str, rel: str, expected_guid: str) -> dict:
    target = repo / rel
    meta = Path(str(target) + ".meta")
    for path in (target, meta):
        need(path.is_file() and not path.is_symlink(), "regular exact source asset required: " + str(path))
        need(0 < path.stat().st_size <= MAX_ASSET_BYTES, "source asset exceeds bound: " + str(path))
    raw, meta_raw = target.read_bytes(), meta.read_bytes()
    digest = sha_bytes(raw)
    matches = re.findall(rb"^guid: ([0-9a-f]{32})$", meta_raw, re.M)
    need(matches == [expected_guid.encode()], "source asset GUID meta differs: " + str(meta))
    refs = graph["operation_map_resource"]["asset_reference_targets"]
    candidates = [v for v in refs.values() if v.get("guid") == expected_guid]
    need(len(candidates) >= 1, "operation map resource lacks expected serialized GUID: " + expected_guid)
    need(all(v.get("target_path") == rel and v.get("target_sha256") == digest for v in candidates),
         "resource target path/hash differs from current source: " + rel)
    pin_matches = [v for p, v in graph["source_pins"].items() if p.replace("\\", "/").endswith("/" + rel)]
    need(pin_matches == [digest], "base graph must uniquely pin exact source asset: " + rel)

    parsed = strict_parse(reader, target)
    mono = parsed.get("MonoBehaviour")
    need(isinstance(mono, dict), "strict YAML reader did not return one MonoBehaviour: " + rel)
    identifier = mono.get("m_EditorClassIdentifier", "")
    need(identifier.rsplit(".", 1)[-1] == type_name, "serialized Unity type mismatch: " + rel)
    script = mono.get("m_Script")
    need(script == {"fileID": 11500000, "guid": script.get("guid") if isinstance(script, dict) else None, "type": 3}
         and isinstance(script.get("guid"), str), "exact MonoScript reference required: " + rel)
    script_guid = script["guid"]
    find = subprocess.run(["rg", "-l", "--glob", "*.meta", "^guid: " + script_guid + "$", str(repo / "Assets")],
                          capture_output=True, text=True, timeout=60)
    need(find.returncode == 0, "MonoScript GUID resolution failed: " + rel)
    hits = [Path(s) for s in find.stdout.splitlines()]
    need(len(hits) == 1 and not hits[0].is_symlink(), "MonoScript GUID must resolve uniquely: " + rel)
    script_meta = hits[0]
    script_path = Path(str(script_meta)[:-5])
    need(script_path.is_file() and not script_path.is_symlink() and script_path.suffix == ".cs" and
         script_path.stem == type_name, "MonoScript class source mismatch: " + rel)

    fields = {k: v for k, v in mono.items() if not k.startswith("m_")}
    spelling_map = parsed.get("__source_scalar_spellings", {})
    spellings = {k[len("MonoBehaviour."):]: v for k, v in spelling_map.items()
                 if k.startswith("MonoBehaviour.")}
    need(spellings, "strict scalar spelling transcript required: " + rel)
    node = {"asset_guid": expected_guid, "source_type": type_name, "source_path": rel,
            "source_sha256": digest, "fields": fields, "scalar_spellings": spellings}
    graph["source_pins"][str(target)] = digest
    graph["source_pins"][str(meta)] = sha_bytes(meta_raw)
    graph["source_pins"][str(script_meta)] = sha_file(script_meta)
    graph["source_pins"][str(script_path)] = sha_file(script_path)
    return node


def validate_graph(repo: Path, graph_path: Path, expected_sha: str, receipt_path: Path) -> tuple[dict, dict, dict]:
    base_raw = graph_path.read_bytes()
    need(sha_bytes(base_raw) == expected_sha, "explicit graph r3 SHA-256 mismatch")
    graph = json.loads(base_raw)
    receipt = json.loads(receipt_path.read_text())
    need(receipt.get("kind") == "S004StandardSetupSourceExportReceipt" and receipt.get("status") == "Passed",
         "passed S004 Standard setup export receipt required")
    need(receipt.get("source_graph", {}).get("path") == str(graph_path.resolve()) and
         receipt.get("source_graph", {}).get("sha256") == expected_sha, "Standard export is not bound to exact graph r3")
    build_desc = receipt.get("source_build", {})
    build_path = Path(build_desc.get("path", ""))
    need(build_path.is_file() and sha_file(build_path) == build_desc.get("sha256"), "pinned source-host build receipt required")
    build = json.loads(build_path.read_text())
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("mission") == "S002" and build.get("compile_exit") == 0,
         "passed source-host compiler/reference build required")
    for path, digest in build.get("source_and_reference_pins", {}).items():
        p = Path(path)
        need(p.is_file() and sha_file(p) == digest, "source-host build pin changed: " + path)
    return graph, receipt, build


def extend_graph(repo: Path, graph: dict) -> dict:
    ext = json.loads(json.dumps(graph))
    reader = repo / "Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb"
    nodes = ext["nodes"]
    guids = {node["asset_guid"] for node in nodes}
    additions = []
    for key, (type_name, rel, guid) in ASSET_REFS.items():
        target = next((v for v in ext["operation_map_resource"]["asset_reference_targets"].values()
                       if v.get("guid") == guid and v.get("target_path") == rel), None)
        need(target is not None, "expected exact selected map target reference missing: " + key)
        need(target.get("fileID_validation", "").startswith("not_applicable"),
             "unexpected object fileID scope for GUID-string AssetReference: " + key)
        node = asset_node(repo, ext, reader, type_name, rel, guid)
        need(node["source_sha256"] == target["target_sha256"], "extension source hash differs from map resource pin")
        need(guid not in guids, "extension GUID already appears in base graph: " + guid)
        guids.add(guid)
        additions.append(node)
    nodes.extend(additions)
    ext["nodes"] = sorted(nodes, key=lambda n: n["source_path"])
    ext["source_graph_extension"] = {
        "kind": "ExactS004PinnedPhysicalInputNodes",
        "base_graph_sha256": sha_file(Path(ext["_base_graph_path"])),
        "added_source_paths": [node["source_path"] for node in additions],
        "scope": "Materialization support for exact Grid/Surface GUID-string references only; no geometry or scene capture",
    }
    ext.pop("_base_graph_path", None)
    return ext


def run(repo: Path, graph_path: Path, graph_sha: str, export_receipt_path: Path,
        output: Path, controls: bool) -> dict:
    hash128_source = repo / HASH128_SOURCE.relative_to(ROOT)
    need(hash128_source.is_file() and sha_file(hash128_source) == HASH128_SOURCE_SHA256,
         "pinned Unity.Entities.Hash128.ToString source changed or is missing")
    repo = repo.resolve(strict=True)
    graph_path, export_receipt_path, output = graph_path.resolve(strict=True), export_receipt_path.resolve(strict=True), output.absolute()
    need(not output.exists(), "fresh output directory required; refusing overwrite")
    graph, export_receipt, build = validate_graph(repo, graph_path, graph_sha, export_receipt_path)
    need(export_receipt["compiled_setup"]["path"] and
         sha_file(Path(export_receipt["compiled_setup"]["path"])) == export_receipt["compiled_setup"]["sha256"],
         "pinned Standard setup export required")
    base_graph_sha = sha_file(graph_path)
    graph["_base_graph_path"] = str(graph_path)
    extended = extend_graph(repo, graph)
    extended["source_graph_extension"]["base_graph_sha256"] = base_graph_sha
    output.mkdir(parents=True)
    extended_path = output / "source-config-graph-with-cityedge-physical-inputs.json"
    extended_path.write_text(json.dumps(extended, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    extended_sha = sha_file(extended_path)

    rsp_desc = build.get("compiler_response", {})
    rsp_path = Path(rsp_desc.get("path", ""))
    need(rsp_path.is_file() and sha_file(rsp_path) == rsp_desc.get("sha256"), "pinned source-host compiler response required")
    args = [json.loads(line) for line in rsp_path.read_text().splitlines() if line.strip()]
    mains = [i for i, item in enumerate(args) if item.startswith("-main:")]
    outputs = [i for i, item in enumerate(args) if item.startswith("-out:")]
    need(len(mains) == 1 and len(outputs) == 1, "unique source compiler main/output switches required")
    args[mains[0]] = "-main:S004PhysicalInputSourceBinding"
    exe = output / "S004PhysicalInputSourceBinding.exe"
    args[outputs[0]] = "-out:" + str(exe)
    materializer_slots = [i for i, item in enumerate(args)
                          if item.endswith("/Adapters/051_SourceConfigMaterializer.cs")]
    need(len(materializer_slots) == 1, "unique source materializer input required")
    args[materializer_slots[0]] = str(MATERIALIZER.resolve())
    args.append(str(ADAPTER.resolve()))
    adapter_rsp = output / "compile.rsp"
    adapter_rsp.write_text("\n".join(json.dumps(item) for item in args) + "\n", encoding="utf-8")
    csc_candidates = sorted(Path(path) for path in build["source_and_reference_pins"]
                           if re.search(r"/sdk/[0-9.]+/Roslyn/bincore/csc\.dll$", path))
    dotnet_candidates = [Path(path) for path in build["source_and_reference_pins"] if path.endswith("/dotnet")]
    need(csc_candidates and dotnet_candidates, "pinned Roslyn and dotnet required")
    compile_proc = subprocess.run([str(dotnet_candidates[0]), str(csc_candidates[-1]), "@" + str(adapter_rsp)],
                                  capture_output=True, text=True, timeout=180)
    (output / "compile.log").write_text(compile_proc.stdout + compile_proc.stderr, encoding="utf-8")
    need(compile_proc.returncode == 0, "source physical-input runner compile failed; compile.log preserved")
    ref_dirs = sorted({str(Path(item[3:]).resolve().parent) for item in args
                       if item.startswith("-r:") and Path(item[3:]).is_file()})
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join(ref_dirs)
    runtime = build.get("runtime_selection", {})
    mono = runtime.get("mono")
    need(mono and Path(mono).is_file(), "pinned Mono runtime required")
    runtime_paths = {key: Path(value).resolve(strict=True) for key, value in runtime.items()}
    helper = runtime_paths["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    mono_native = runtime_paths["mono"].parent.parent / "lib/libmono-native.dylib"
    need(helper.is_file() and mono_native.is_file(), "pinned Mono runtime native helpers required")
    dllmaps = '<dllmap dll="MonoPosixHelper" target="' + str(helper) + '" os="!windows"/>'
    dllmaps += ''.join('<dllmap dll="' + name + '" target="' + str(mono_native) + '" os="!windows"/>'
                       for name in ("System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"))
    mono_config = output / "mono-config.xml"
    mono_config.write_text("<configuration>" + dllmaps + "</configuration>\n", encoding="utf-8")
    env["MONO_PATH"] = os.pathsep.join([str(repo / "Library/ScriptAssemblies"), str(runtime_paths["engine"]),
                                        str(runtime_paths["newtonsoft"].parent), str(Path(build.get("placement_build", rsp_path.parent)).parent),
                                        *ref_dirs])
    run_proc = subprocess.run([mono, "--config", str(mono_config), str(exe), str(extended_path), str(repo), str(output)],
                              capture_output=True, text=True, timeout=180, env=env)
    (output / "run.log").write_text(run_proc.stdout + run_proc.stderr, encoding="utf-8")
    need(run_proc.returncode == 0 and run_proc.stdout.strip(), "source physical-input projection failed; run.log preserved")
    projected = json.loads(run_proc.stdout.strip().splitlines()[-1])
    need(projected.get("kind") == "S004PinnedCityEdgePhysicalInputSourceProjection" and
         projected.get("status") == "Passed" and projected.get("physical_source_fit_verified") is False,
         "projection output kind/status/scope invalid")

    control_results = []
    if controls:
        for case in ("changed-pin", "duplicate-guid", "out-of-bounds-blocked-cell", "corrupt-surface-gzip"):
            proc = subprocess.run([mono, str(exe), str(extended_path), str(repo), str(output), case],
                                 capture_output=True, text=True, timeout=90, env=env)
            (output / ("control-" + case + ".log")).write_text(proc.stdout + proc.stderr, encoding="utf-8")
            need(proc.returncode == 0 and "RejectedAsExpected" in proc.stdout,
                 "negative control failed: " + case)
            control_results.append({"case": case, "result": "RejectedAsExpected"})
        proc = subprocess.run([mono, str(exe), str(extended_path), str(repo), str(output), "hash-text-parity"],
                             capture_output=True, text=True, timeout=90, env=env)
        (output / "control-hash-text-parity.log").write_text(proc.stdout + proc.stderr, encoding="utf-8")
        need(proc.returncode == 0 and '"result":"Passed"' in proc.stdout,
             "HashText parity control failed against pinned Unity.Entities.Hash128.ToString source")
        control_results.append({"case": "hash-text-parity", "result": "Passed",
                                "vectors": 5, "oracle": "Exact transcription of pinned Hash128.cs ToString nibble loop"})

    input_descriptors = []
    for path, digest in build.get("source_and_reference_pins", {}).items():
        p = Path(path)
        need(p.is_file() and sha_file(p) == digest, "source-host build pin changed during projection: " + path)
    for path, digest in extended["source_pins"].items():
        p = Path(path)
        need(p.is_file() and sha_file(p) == digest, "pinned input changed during projection: " + path)
    need(sha_file(hash128_source) == HASH128_SOURCE_SHA256,
         "pinned Unity.Entities.Hash128.ToString source changed during projection")
    for rel in (MAP_REL, *(v[1] for v in ASSET_REFS.values())):
        p = repo / rel
        input_descriptors.append({"path": str(p), "sha256": sha_file(p)})
    receipt = {
        "kind": "S004PinnedCityEdgePhysicalInputSourceProjectionReceipt",
        "status": "Passed",
        "base_graph": {"path": str(graph_path), "sha256": base_graph_sha},
        "extended_graph": {"path": str(extended_path), "sha256": extended_sha},
        "standard_setup_export_receipt": {"path": str(export_receipt_path), "sha256": sha_file(export_receipt_path)},
        "source_build": {"path": build["compiler_response"]["path"], "sha256": build["compiler_response"]["sha256"]},
        "source_host_build_pins_verified_before_and_after": True,
        "adapter_source": {"path": str(ADAPTER.resolve()), "sha256": sha_file(ADAPTER)},
        "source_materializer": {"path": str(MATERIALIZER.resolve()), "sha256": sha_file(MATERIALIZER)},
        "hash128_format_source": {"path": str(hash128_source.resolve()),
                                  "sha256": HASH128_SOURCE_SHA256,
                                  "pinned_before_and_after": True,
                                  "method": "Unity.Entities.Hash128.ToString"},
        "inputs": input_descriptors,
        "output": {"path": str(output / "source-projection.json"), "sha256": sha_file(output / "source-projection.json")},
        "walkability_projection": {"path": str(output / "authored-grid-walkable-emulation.u8"),
                                   "sha256": sha_file(output / "authored-grid-walkable-emulation.u8"),
                                   "scope": "Authored GridAuthoring.Baker rule emulation; not Unity Baker/scene output"},
        "negative_controls": control_results,
        "world_initialized": False,
        "unity_run": False,
        "unity_baker_observed": False,
        "native_scene_grid_readback": False,
        "physical_source_fit_verified": False,
        "mission_readiness": False,
        "scope": "Original surface payload decode plus source-authored Grid blocked-cell projection; not scene/domain capture or fit proof",
    }
    receipt_path = output / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return receipt


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--repo", type=Path, default=ROOT)
    ap.add_argument("--graph", type=Path, required=True)
    ap.add_argument("--graph-sha256", required=True)
    ap.add_argument("--standard-export-receipt", type=Path, required=True)
    ap.add_argument("--output", type=Path, required=True)
    ap.add_argument("--negative-controls", action="store_true")
    args = ap.parse_args()
    try:
        result = run(args.repo, args.graph, args.graph_sha256, args.standard_export_receipt,
                     args.output, args.negative_controls)
        print(json.dumps({"status": result["status"], "receipt": result["output"]["path"],
                          "world_initialized": False, "unity_run": False,
                          "physical_source_fit_verified": False, "mission_readiness": False}, indent=2))
        return 0
    except Exception as error:
        print("S004 pinned physical-input projection failed closed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

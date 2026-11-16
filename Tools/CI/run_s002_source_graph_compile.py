#!/usr/bin/env python3
"""Compile current authored S002 setup data through the original compiler only.

No native metadata qualification, Unity World, geometry materialization, or
runtime admission is performed by this bounded diagnostic.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import re
import shutil
import subprocess
import time

CONTROL_SOURCE = Path(__file__).resolve().parent / "SkirmishSourceSimulation/Tests/S002SourceGraphCompilerControl.cs"
GRAPH_GENERATOR = Path(__file__).resolve().parent / "SkirmishSourceSimulation/Recipes/s002_source_graph/generate_s002_authored_config_graph.py"
REQUIRED_GEOMETRY_TARGETS = {"gridMetadata", "surfaceMetadata", "mapSurfaceDataReference", "sourceSceneReference"}
REQUIRED_DIAGNOSTIC_FALSE = {
    "world_allocated", "geometry_materialized", "source_fit_verified", "native_acceptance", "mission_ready"
}


def digest(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def read_json(path: Path):
    raw = path.read_bytes()
    value = json.loads(raw, object_pairs_hook=_unique_pairs,
                       parse_constant=lambda value: (_ for _ in ()).throw(ValueError("nonfinite JSON")))
    if type(value) is not dict:
        raise ValueError("JSON object required: " + str(path))
    return value, raw


def _unique_pairs(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("duplicate JSON field: " + key)
        result[key] = value
    return result


def verify_file(path: Path, expected: str, label: str, root: Path | None = None) -> None:
    if not path.is_absolute() or path.is_symlink() or not re.fullmatch(r"[0-9a-f]{64}", str(expected)):
        raise ValueError("absolute regular hash-bound " + label + " required: " + str(path))
    resolved = path.resolve(strict=True)
    if root is not None and not resolved.is_relative_to(root):
        raise ValueError(label + " escaped repository: " + str(path))
    if not resolved.is_file() or digest(resolved) != expected:
        raise ValueError(label + " pin changed: " + str(path))


def verify_build_receipt(receipt: dict, repo: Path) -> dict:
    if (receipt.get("status") != "Passed" or receipt.get("mission") != "S002" or
            receipt.get("seed") != 104731 or Path(receipt.get("repository_root", "")).resolve() != repo):
        raise ValueError("Passed current S002/104731 build receipt required")
    pins = receipt.get("source_and_reference_pins")
    if type(pins) is not dict or not pins:
        raise ValueError("nonempty build source pin map required")
    for path_text, expected in pins.items():
        verify_file(Path(path_text), expected, "build source")
    for label in ("executable", "compiler_response"):
        descriptor = receipt.get(label)
        if type(descriptor) is not dict or not {"path", "sha256"} <= set(descriptor):
            raise ValueError("pinned build " + label + " required")
        verify_file(Path(descriptor["path"]), descriptor["sha256"], "build " + label)
    canonical = receipt.get("canonical_source_manifest")
    if type(canonical) is not dict:
        raise ValueError("canonical source manifest pin required")
    verify_file(Path(canonical["path"]), canonical["sha256"], "canonical source manifest", root=repo)
    return pins


def verify_graph(graph_path: Path, proof_path: Path, repo: Path) -> tuple[dict, dict, str]:
    proof, proof_raw = read_json(proof_path)
    if proof.get("kind") != "S002CurrentAuthoredConfigGraph" or proof.get("status") != "Passed":
        raise ValueError("passed current S002 authored graph proof required")
    graph_descriptor = proof.get("graph")
    if type(graph_descriptor) is not dict or Path(graph_descriptor.get("path", "")).resolve() != graph_path.resolve():
        raise ValueError("graph path differs from graph proof")
    verify_file(graph_path.resolve(strict=True), graph_descriptor.get("sha256"), "authored graph")
    graph, graph_raw = read_json(graph_path)
    if (graph.get("authority") != "AuthoredConfigAssets" or graph.get("definition_path") !=
            "Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishScenario_S002.asset" or
            graph.get("catalog_path") != "Assets/Game/Resources/SkirmishExpansionCatalog.asset" or
            graph.get("compiled_setup") is not False or graph.get("world_initialized") is not False or
            graph.get("native_world_state_used") is not False):
        raise ValueError("current uncompiled S002 authored config graph required")
    pins = graph.get("source_pins")
    if type(pins) is not dict or not pins:
        raise ValueError("current authored graph source pins required")
    for path_text, expected in pins.items():
        verify_file(Path(path_text), expected, "graph source", root=repo)
    matrix = graph.get("setup_matrix")
    if type(matrix) is not dict or set(matrix) != {"path", "sha256"}:
        raise ValueError("exact graph-owned setup matrix descriptor required")
    verify_file(Path(matrix["path"]), matrix["sha256"], "setup matrix", root=repo)
    targets = graph.get("geometry_source_targets")
    if type(targets) is not dict or set(targets) != REQUIRED_GEOMETRY_TARGETS:
        raise ValueError("exact selected map source target set required")
    for name, target in targets.items():
        if type(target) is not dict or not {"guid", "path", "sha256", "bytes"} <= set(target):
            raise ValueError("complete current map source target descriptor required: " + name)
        path = (repo / target["path"]).resolve(strict=True)
        verify_file(path, target["sha256"], "map source target", root=repo)
        if path.stat().st_size != target["bytes"]:
            raise ValueError("map source target byte count changed: " + name)
    return graph, matrix, hashlib.sha256(graph_raw).hexdigest()


def validate_result(value: dict) -> bool:
    if value.get("kind") != "S002SourceGraphCompileResult" or value.get("mission") != "S002" or value.get("seed") != 104731:
        return False
    if value.get("status") not in {"ConfigurationCompiled", "CompilerRejected"}:
        return False
    if value.get("compiler_accepted") is not (value.get("status") == "ConfigurationCompiled"):
        return False
    if any(value.get(field) is not False for field in REQUIRED_DIAGNOSTIC_FALSE):
        return False
    if value.get("operation_map_id") != "opmap.skirmish.desert_base_01" or value.get("selected_map_source") != "opmap.skirmish.desert_base_01":
        return False
    if not re.fullmatch(r"[0-9a-f]{64}", str(value.get("source_graph_sha256", ""))):
        return False
    if not re.fullmatch(r"[0-9a-f]{64}", str(value.get("setup_matrix_sha256", ""))):
        return False
    if type(value.get("compiler_reasons")) is not list:
        return False
    if value["status"] == "ConfigurationCompiled" and (type(value.get("setup_hash")) is not int or value["setup_hash"] < 0):
        return False
    return True


def run(build_receipt_path: Path, graph_proof_path: Path, graph_path: Path, output_path: Path,
        repo: Path, timeout: float = 300.0) -> dict:
    repo = repo.resolve(strict=True)
    build_receipt_path = build_receipt_path.resolve(strict=True)
    graph_proof_path = graph_proof_path.resolve(strict=True)
    graph_path = graph_path.resolve(strict=True)
    output = output_path.absolute()
    if not math.isfinite(timeout) or timeout <= 0 or timeout > 1800:
        raise ValueError("bounded positive timeout required")
    if output.exists():
        raise ValueError("fresh isolated output directory required")
    build_receipt, build_raw = read_json(build_receipt_path)
    build_pins = verify_build_receipt(build_receipt, repo)
    graph, matrix, graph_sha = verify_graph(graph_path, graph_proof_path, repo)
    graph_proof_sha = digest(graph_proof_path)
    tool_pins = {str(CONTROL_SOURCE): digest(CONTROL_SOURCE), str(GRAPH_GENERATOR): digest(GRAPH_GENERATOR)}
    for path_text, expected in tool_pins.items():
        verify_file(Path(path_text), expected, "diagnostic tool source", root=repo)

    output.mkdir(parents=True)
    control_output = output / "control-output"
    control_output.mkdir()
    source_dir = output / "build"
    source_dir.mkdir()
    receipt = {
        "schema_version": 1, "kind": "S002SourceGraphCompileRequest", "mission": "S002",
        "seed": 104731, "difficulty_id": 2, "size_id": 1,
        "source_config_graph": {"path": str(graph_path), "sha256": graph_sha},
        "setup_matrix": matrix, "repository_root": str(repo), "output_directory": str(control_output)
    }
    request_path = output / "compile-request.json"
    request_path.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    request_sha = digest(request_path)

    compiler_response = build_receipt["compiler_response"]["path"]
    raw = Path(compiler_response).read_text(encoding="utf-8")
    old_main = '"-main:SourceSimulationProgram"'
    old_out = '"-out:' + str(Path(compiler_response).parent / "SourceSimulation.exe") + '"'
    if raw.count(old_main) != 1 or raw.count(old_out) != 1:
        raise ValueError("unsupported pinned original compiler response")
    executable = source_dir / "S002SourceGraphCompilerControl.exe"
    raw = raw.replace(old_main, '"-main:S002SourceGraphCompilerControl"').replace(old_out, '"-out:' + str(executable) + '"')
    raw += '\n"' + str(CONTROL_SOURCE) + '"\n'
    response_path = source_dir / "compile.rsp"
    response_path.write_text(raw, encoding="utf-8")

    csc_candidates = [Path(path) for path in build_pins if path.endswith("/Roslyn/bincore/csc.dll")]
    if not csc_candidates:
        raise ValueError("pinned Roslyn csc required")
    csc_candidates.sort(key=lambda path: tuple(int(part) for part in path.parts[-4].split(".") if part.isdigit()))
    dotnet = shutil.which("dotnet")
    if not dotnet or str(Path(dotnet).resolve(strict=True)) not in build_pins:
        raise ValueError("pinned dotnet host required")
    runtime = build_receipt["runtime_selection"]
    mono = Path(runtime["mono"])
    if str(mono) not in build_pins or str(Path(runtime["newtonsoft"])) not in build_pins:
        raise ValueError("pinned Mono/Newtonsoft runtime required")
    placement = Path(build_receipt["placement_build"]).parent
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([str(Path(runtime["newtonsoft"]).parent), runtime["engine"],
                                          str(repo / "Library/ScriptAssemblies"), str(placement)])

    start = time.monotonic()
    compile_run = subprocess.run([dotnet, str(csc_candidates[-1]), "@" + str(response_path)], cwd=repo,
                                  env=env, capture_output=True, text=True, timeout=timeout)
    (output / "compile.log").write_text(compile_run.stdout + compile_run.stderr, encoding="utf-8")
    if compile_run.returncode != 0:
        raise ValueError("focused compiler control compilation failed; see " + str(output / "compile.log"))
    command = [str(mono), "--optimize=-float32", str(executable), str(request_path)]
    execution = subprocess.run(command, cwd=repo, env=env, capture_output=True, text=True, timeout=timeout)
    (output / "control.log").write_text(execution.stdout + execution.stderr, encoding="utf-8")
    if execution.returncode != 0 or "[S002SourceGraphCompilerControl] result=Completed status=" not in execution.stdout:
        raise ValueError("focused S002 compiler control failed; see " + str(output / "control.log"))

    result_path = control_output / "compiler-result.json"
    result, result_raw = read_json(result_path)
    if not validate_result(result):
        raise ValueError("source-only compiler result failed strict classification")
    for path_text, expected in build_pins.items():
        # Build receipts can pin toolchain/runtime files outside the repository.
        verify_file(Path(path_text), expected, "post-control build source")
    for path_text, expected in graph["source_pins"].items():
        verify_file(Path(path_text), expected, "post-control graph source", root=repo)
    verify_file(Path(matrix["path"]), matrix["sha256"], "post-control matrix", root=repo)
    if digest(request_path) != request_sha or digest(build_receipt_path) != hashlib.sha256(build_raw).hexdigest():
        raise ValueError("compiler request or build receipt changed during diagnostic")
    if digest(graph_path) != graph_sha or digest(graph_proof_path) != graph_proof_sha or not validate_result(result):
        raise ValueError("graph or result changed during diagnostic")
    for path_text, expected in tool_pins.items():
        verify_file(Path(path_text), expected, "post-control diagnostic tool source", root=repo)

    report = {
        "schema_version": 1, "kind": "S002SourceGraphCompileReceipt", "status": "Completed",
        "compiler_status": result["status"], "compiler_accepted": result["compiler_accepted"],
        "mission": "S002", "seed": 104731, "build_receipt": str(build_receipt_path),
        "build_receipt_sha256": hashlib.sha256(build_raw).hexdigest(),
        "graph_proof": str(graph_proof_path), "source_config_graph": str(graph_path),
        "source_config_graph_sha256": graph_sha, "setup_matrix": matrix,
        "compiler_result": str(result_path), "compiler_result_sha256": hashlib.sha256(result_raw).hexdigest(),
        "build_pin_count": len(build_pins), "graph_pin_count": len(graph["source_pins"]),
        "tool_source_pins": tool_pins,
        "world_allocated": False, "geometry_materialized": False, "source_fit_verified": False,
        "native_acceptance": False, "mission_ready": False,
        "elapsed_seconds": time.monotonic() - start,
        "scope": "Original S002 setup compiler only. No native metadata qualification, World, geometry fit or runtime admission."
    }
    receipt_path = output / "compile-control-receipt.json"
    receipt_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": report["status"], "compiler_status": report["compiler_status"],
                      "compiler_accepted": report["compiler_accepted"], "receipt": str(receipt_path)}))
    return report


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-receipt", type=Path, required=True)
    parser.add_argument("--graph-proof", type=Path, required=True)
    parser.add_argument("--graph", type=Path, required=True)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--timeout", type=float, default=300)
    args = parser.parse_args(argv)
    try:
        run(args.build_receipt, args.graph_proof, args.graph, args.output, args.repo, args.timeout)
    except (OSError, ValueError, KeyError, TypeError, json.JSONDecodeError, subprocess.TimeoutExpired) as error:
        raise SystemExit("S002 source graph compile diagnostic failed: " + str(error))


if __name__ == "__main__":
    main()

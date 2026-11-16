#!/usr/bin/env python3
"""Compile and run the original operation-map contract validator over authored source data."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

RECIPE = Path(__file__).resolve().parent
HARNESS = RECIPE / "OperationMapContractHarness.cs"
YAML_READER = RECIPE.parent / "selected_operation_map/helpers/read_source_configs.rb"
CONTRACT = Path("Assets/Game/Scripts/Configs/OperationMapContractValidation.cs")
SCENARIO = Path("Assets/Game/Scripts/Configs/ScenarioSetupConfig.cs")
SCENARIO_DEPENDENCIES = [
    "ScenarioMissionRuntimeConfig.cs", "MissionDefenseDefinitionConfig.cs", "MissionExtractionDefinitionConfig.cs",
    "MissionBreachDefinitionConfig.cs", "MissionGridlockDefinitionConfig.cs", "MissionSupplyLineDefinitionConfig.cs",
    "MissionMarketLifelineDefinitionConfig.cs", "MissionPowerRelayDefinitionConfig.cs", "MissionRouteReopenedDefinitionConfig.cs",
    "ScenarioMissionRuntimeContractValidation.cs", "MissionDefenseDefinitionValidation.cs",
    "MissionExtractionDefinitionValidation.cs", "MissionBreachDefinitionValidation.cs", "MissionGuidanceStepConfig.cs",
]
CAMERA = Path("Assets/Game/Scripts/Configs/MissionCameraTourConfig.cs")
GUIDANCE = Path("Assets/Game/Scripts/Missions/Contracts/MissionGuidanceContracts.cs")
PASS = "OriginalOperationMapContractSourceOnlyResult"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        need(key not in result, "Duplicate JSON property: " + key)
        result[key] = value
    return result


def read_json(path: Path):
    return json.loads(path.read_text(), object_pairs_hook=unique_object)


def read_json_line(value: str):
    return json.loads(value, object_pairs_hook=unique_object)


def checked_asset(repo: Path, rel: str) -> Path:
    relative = Path(rel)
    need(not relative.is_absolute() and ".." not in relative.parts,
         "Asset path must be repo-relative without traversal: " + rel)
    base = (repo / "Assets").absolute()
    target = (repo / relative).absolute()
    need(target.is_relative_to(base), "Asset path escaped Assets: " + rel)
    cursor = base
    for part in target.relative_to(base).parts:
        cursor = cursor / part
        need(not cursor.is_symlink(), "Symlink in asset path: " + str(cursor))
    need(target.is_file(), "Asset is not a regular file: " + str(target))
    return target


def absolute_without_symlinks(path: Path) -> bool:
    if not path.is_absolute():
        return False
    cursor = Path(path.anchor)
    for part in path.parts[1:]:
        cursor = cursor / part
        if cursor.is_symlink():
            return False
    return True


def strict_yaml(repo: Path, paths: list[str]) -> dict:
    resolved = [repo / path for path in paths]
    proc = subprocess.run(["ruby", str(YAML_READER), *map(str, resolved)], capture_output=True, text=True, timeout=45)
    need(proc.returncode == 0, "Strict authored YAML reader rejected source: " + proc.stderr.strip())
    return json.loads(proc.stdout, object_pairs_hook=unique_object)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--repo", type=Path, required=True)
    ap.add_argument("--graph", type=Path, required=True)
    ap.add_argument("--graph-sha256", required=True)
    ap.add_argument("--build", type=Path, required=True)
    ap.add_argument("--build-sha256", required=True)
    ap.add_argument("--package", type=Path, required=True)
    ap.add_argument("--package-sha256", required=True)
    ap.add_argument("--package-preflight", type=Path, required=True,
                    help="full source-only package preflight report from the shared workflow")
    ap.add_argument("--package-preflight-sha256", required=True)
    ap.add_argument("--logical", required=True, help="logical operation-map asset path relative to repo")
    ap.add_argument("--physical", required=True, help="physical operation-map asset path relative to repo")
    ap.add_argument("--output", type=Path, required=True, help="new artifact directory, preferably under T Logs")
    opts = ap.parse_args()
    repo = opts.repo.resolve(strict=True)
    out = opts.output.absolute()
    out.mkdir(parents=True, exist_ok=False)

    try:
        configs = repo / "Assets/Game/Scripts/Configs"
        source_files = [repo / CAMERA, repo / GUIDANCE, repo / SCENARIO, repo / CONTRACT,
                        *(configs / name for name in SCENARIO_DEPENDENCIES), HARNESS, YAML_READER,
                        Path(__file__).resolve()]
        added_source_pins = {str(path.resolve(strict=True)): sha(path) for path in source_files}
        graph_path, build_path, package_path = (p.resolve(strict=True) for p in (opts.graph, opts.build, opts.package))
        package_preflight_input = opts.package_preflight.resolve(strict=True)
        need(sha(graph_path) == opts.graph_sha256, "Source graph SHA-256 does not match requested pin")
        need(sha(build_path) == opts.build_sha256, "Managed build receipt SHA-256 does not match requested pin")
        need(sha(package_path) == opts.package_sha256, "Source-derived package SHA-256 does not match requested pin")
        need(sha(package_preflight_input) == opts.package_preflight_sha256,
             "Package preflight report SHA-256 does not match requested pin")
        graph = read_json(graph_path)
        need(graph.get("schema_version") == 1 and graph.get("authority") == "AuthoredConfigAssets" and
             graph.get("compiled_setup") is False and graph.get("world_initialized") is False and
             graph.get("native_world_state_used") is False, "Uncompiled authored config graph required")
        pins = graph.get("source_pins")
        need(isinstance(pins, dict) and pins, "Graph source pins are required")
        for path, digest in pins.items():
            source = Path(path)
            need(source.is_absolute() and absolute_without_symlinks(source) and source.is_file() and sha(source) == digest,
                 "Authored graph source pin is non-absolute, symlinked, missing, or changed: " + path)

        build = read_json(build_path)
        need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
             build.get("compile_exit") == 0, "Passed managed source host build required")
        build_pins = build.get("source_and_reference_pins")
        need(isinstance(build_pins, dict) and build_pins, "Managed compiler input pins are required")
        for path, digest in build_pins.items():
            need(Path(path).is_file() and sha(Path(path)) == digest, "Managed compiler source/reference changed: " + path)
        runtime = build["runtime_selection"]
        for runtime_path in (runtime.get("mono"), runtime.get("newtonsoft")):
            need(runtime_path in build_pins, "Runtime dependency is not a verified build pin: " + str(runtime_path))

        preflight_report = read_json(package_preflight_input)
        need(preflight_report.get("kind") == "SourceDerivedCampaignMapPackage" and
             preflight_report.get("status") == "SourceOnlyPreflightPassed" and
             preflight_report.get("mission_ready") is False and
             preflight_report.get("source_fit_certified") is False and
             preflight_report.get("world_created") is False,
             "Pinned source-only package preflight report required")
        verified_inputs = preflight_report.get("verified_inputs")
        need(isinstance(verified_inputs, list) and any(
            row.get("path") == str(package_path) and row.get("sha256") == opts.package_sha256
            for row in verified_inputs if isinstance(row, dict)),
            "Package preflight report does not pin the supplied package bytes")
        package_preflight_path = out / "package-preflight.json"
        shutil.copyfile(package_preflight_input, package_preflight_path)

        package = read_json(package_path)
        static_map = package["static_map"]
        package_definition = static_map["definition"]
        logical_path, physical_path = opts.logical, opts.physical
        nodes = {}
        for source_path in (logical_path, physical_path):
            matches = [n for n in graph.get("nodes", []) if n.get("source_type") == "OperationMapDefinition" and
                       n.get("source_path") == source_path]
            need(len(matches) == 1, "Graph must contain exactly one requested operation-map node: " + source_path)
            nodes[source_path] = matches[0]
        need(logical_path != physical_path, "Logical and physical operation-map paths must differ")
        logical_file, physical_file = checked_asset(repo, logical_path), checked_asset(repo, physical_path)
        yaml = strict_yaml(repo, [logical_path, physical_path])
        logical_yaml, physical_yaml = (yaml[str(repo / p)]["MonoBehaviour"] for p in (logical_path, physical_path))
        for path, authored in ((logical_path, logical_yaml), (physical_path, physical_yaml)):
            node = nodes[path]
            asset_file = logical_file if path == logical_path else physical_file
            need(node.get("source_sha256") == sha(asset_file), "Operation-map graph source pin differs from authored bytes: " + path)
            fields = node.get("fields")
            authored_fields = {k: v for k, v in authored.items() if not k.startswith("m_")}
            need(isinstance(fields, dict) and authored_fields == fields,
                 "Strict YAML fields differ from graph node: " + path)

        binding = logical_yaml.get("sourceBinding")
        physical_id = physical_yaml.get("operationMapId")
        physical_hash = physical_yaml.get("contentHash")
        joined_source_fields = ("gridMetadata", "surfaceMetadata", "navigationMetadata",
                                "sourceSceneReference", "mapSurfaceDataReference")
        for field in joined_source_fields:
            need(logical_yaml.get(field) == physical_yaml.get(field),
                 "Logical and physical authored source field differs: " + field)
        grid = physical_yaml.get("gridMetadata")
        dimensions, origin = grid.get("dimensions", {}), grid.get("origin", {})
        frame = package.get("frame", {})
        need(frame.get("width") == dimensions.get("x") and frame.get("height") == dimensions.get("y") and
             frame.get("cell_size") == grid.get("cellSize") and
             frame.get("origin") == [origin.get("x"), origin.get("y"), origin.get("z")],
             "Source package frame differs from authored operation-map grid metadata")
        package_definition_path = Path(package_definition.get("path", ""))
        need(static_map.get("operation_map_id") == physical_id and package_definition_path.is_absolute() and
             package_definition_path.resolve(strict=True) == physical_file.resolve(strict=True) and
             package_definition.get("sha256") == sha(physical_file),
             "Source-derived package definition does not join the requested physical asset")
        need(binding.get("sourceOperationMapId") == physical_id and
             binding.get("sourceIdentityHash") == physical_yaml.get("sourceIdentityHash") and
             binding.get("sourceContentHash") == physical_hash and
             logical_yaml.get("sourceIdentityHash") == physical_yaml.get("sourceIdentityHash") and
             logical_yaml.get("sourceSceneReference", {}).get("m_AssetGUID") ==
             physical_yaml.get("sourceSceneReference", {}).get("m_AssetGUID"),
             "Logical authored binding does not join the package physical map")

        evidence_rows = []
        for path, authored in ((logical_path, logical_yaml), (physical_path, physical_yaml)):
            row = {key: authored[key] for key in ("operationMapId", "schemaVersion", "contentVersion",
                                                    "sourceIdentityHash", "contentHash", "generatedMetadataHash")}
            row["sourcePath"] = path
            row["authoredDefinitionSha256"] = sha(logical_file if path == logical_path else physical_file)
            evidence_rows.append(row)
        evidence_path = out / "evidence.json"
        evidence_path.write_text(json.dumps(evidence_rows, indent=2) + "\n")

        response_meta = build["compiler_response"]
        response = Path(response_meta["path"])
        need(sha(response) == response_meta["sha256"], "Pinned compiler response changed")
        compiler_args = [json.loads(line) for line in response.read_text().splitlines() if line.strip()]
        need(any(a.startswith("-main:") for a in compiler_args) and any(a.startswith("-out:") for a in compiler_args),
             "Compiler response shape is invalid")
        for i, arg in enumerate(compiler_args):
            if arg.startswith("-main:"):
                compiler_args[i] = "-main:OperationMapContractHarness"
            elif arg.startswith("-out:"):
                compiler_args[i] = "-out:" + str(out / "OperationMapContract.exe")
        dotnet = Path(next(p for p in build_pins if p.endswith("/dotnet")))
        csc_candidates = sorted(p for p in build_pins if re.search(r"/sdk/[0-9.]+/Roslyn/bincore/csc\.dll$", p))
        need(csc_candidates, "Pinned Roslyn compiler missing")
        csc = Path(csc_candidates[-1])
        # The source-host alias prevents the UnityEngine.CoreModule reference alias from colliding with Vector4.
        camera_source = (repo / CAMERA).read_text()
        camera_alias = out / "MissionCameraTourConfig.SourceHostAlias.cs"
        camera_alias.write_text("extern alias ActualCore; using Vector4=ActualCore::UnityEngine.Vector4;\n" + camera_source)
        compiler_args = [a for a in compiler_args if not a.endswith("/OperationMapContractHarness.cs")]
        compiler_args += [str(configs / name) for name in SCENARIO_DEPENDENCIES]
        compiler_args += [str(camera_alias), str(repo / GUIDANCE), str(repo / SCENARIO), str(repo / CONTRACT), str(HARNESS)]
        rsp_path = out / "compile.rsp"
        rsp_path.write_text("\n".join(json.dumps(a) for a in compiler_args) + "\n")
        compiled = subprocess.run([str(dotnet), str(csc), "@" + str(rsp_path)], capture_output=True, text=True, timeout=240)
        (out / "compile.log").write_text(compiled.stdout + compiled.stderr)
        need(compiled.returncode == 0, "Focused original-validator compilation failed; see compile.log")

        refs = sorted({str(Path(a[3:]).resolve().parent) for a in compiler_args
                       if a.startswith("-r:") and Path(a[3:]).is_file()})
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join(refs + [str(Path(runtime["newtonsoft"]).parent)])
        run = subprocess.run([runtime["mono"], str(out / "OperationMapContract.exe"), str(graph_path),
                              logical_path, physical_path, str(evidence_path)], cwd=repo,
                             capture_output=True, text=True, timeout=120, env=env)
        (out / "run.log").write_text(run.stdout + run.stderr)
        need(run.returncode == 0 and PASS in run.stdout, "Original public validator run failed; see run.log")

        for path, digest in pins.items():
            need(Path(path).is_file() and sha(Path(path)) == digest, "Authored graph input changed during compile/run: " + path)
        for path, digest in build_pins.items():
            need(Path(path).is_file() and sha(Path(path)) == digest, "Managed compiler input changed during compile/run: " + path)
        need(sha(graph_path) == opts.graph_sha256 and sha(build_path) == opts.build_sha256 and
             sha(package_path) == opts.package_sha256, "Pinned graph, build, or package changed during validation")
        for path, digest in added_source_pins.items():
            need(Path(path).is_file() and sha(Path(path)) == digest, "Validator/helper source changed during run: " + path)
        need(sha(response) == response_meta["sha256"], "Pinned compiler response changed during run")
        result = read_json_line(run.stdout.strip().splitlines()[-1])
        receipt = {"kind": "OriginalOperationMapContractSourceOnlyReceipt", "status": "Passed", "result": result,
                   "source_graph": {"path": str(graph_path), "sha256": opts.graph_sha256, "source_pin_count": len(pins)},
                   "source_build": {"path": str(build_path), "sha256": opts.build_sha256},
                   "source_package": {"path": str(package_path), "sha256": opts.package_sha256},
                   "package_preflight": {"path": str(package_preflight_path), "sha256": sha(package_preflight_path),
                                         "status": preflight_report["status"], "operation_map_id": preflight_report.get("operation_map_id")},
                   "logical_source": {"path": logical_path, "sha256": sha(logical_file)},
                   "physical_source": {"path": physical_path, "sha256": sha(physical_file)},
                   "joined_authored_source_fields": list(joined_source_fields),
                   "package_frame_join": {"frame": frame, "authored_grid_metadata": grid, "status": "Passed"},
                   "additional_source_pins": added_source_pins,
                   "generated_alias": {"path": str(camera_alias), "sha256": sha(camera_alias)},
                   "compiler_response": {"path": str(response), "sha256": response_meta["sha256"]},
                   "executable": {"path": str(out / "OperationMapContract.exe"), "sha256": sha(out / "OperationMapContract.exe")},
                   "compile_log_sha256": sha(out / "compile.log"), "run_log_sha256": sha(out / "run.log"),
                   "world_initialized": False, "mission_ready": False, "source_fit_certified": False}
        (out / "receipt.json").write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n")
        print("[OriginalOperationMapContract] result=Passed public_validator=true authored_yaml_verified=true package_join=true")
        return 0
    except Exception as exc:
        (out / "failure.txt").write_text(str(exc) + "\n")
        print("[OriginalOperationMapContract] result=Failed " + str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

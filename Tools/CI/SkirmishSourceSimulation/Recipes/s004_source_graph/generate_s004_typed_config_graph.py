#!/usr/bin/env python3
"""Generate an actual, bounded S004 AuthoredConfigAssets graph for SourceConfigMaterializer.

This emits serialized source data only. It does not create Unity objects, compile
a mission, or establish physical map fit or readiness.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile

HERE = Path(__file__).resolve().parent
READER_REL = "Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb"
GENERATOR_REL = "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/generate_s004_typed_config_graph.py"
ROOT_REL = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishScenario_S004.asset"
CATALOG_REL = "Assets/Game/Resources/SkirmishExpansionCatalog.asset"
MATRIX_REL = "Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv"
S004_MAP_RESOURCE_REL = "Assets/Game/Resources/SkirmishS004OperationMap.asset"
MAX_FILE = 4 * 1024 * 1024
MAX_NODES = 64
MAX_REFS = 512
DOCUMENT = re.compile(rb"^--- !u!\d+ &(-?\d+)\s*$", re.M)
ALLOWED = {
    "SkirmishScenarioDefinitionConfig", "SkirmishBattleCatalogConfig", "SkirmishObjectiveConfig",
    "SkirmishArmyProfileConfig", "SkirmishStartPackageConfig", "SkirmishEconomyConfig",
    "SkirmishUpgradeConfig", "SkirmishIntelConfig", "SkirmishRoleCatalogConfig",
    "SkirmishMapLayoutConfig", "SkirmishExpansionCatalogConfig", "SkirmishSizeConfig",
    "SkirmishDifficultyConfig", "SkirmishPublicationConfig", "SkirmishGroundStagingConfig",
    "SkirmishRoleOverlayCatalog", "OperationMapDefinition", "MapBuildingPlacementConfig",
    "MapSurfaceDataAsset", "GridAuthoringSceneConfigAsset", "SkirmishPresetConfig", "AIControllerConfig",
    "BuildingPlacementSystemConfig", "InitialUnitsSpawnerAuthoringConfig", "UnitPrefabRegistryAuthoringConfig",
    "UnitImpostorAtlasEntry", "AIPlanEntryStartupConfig", "ResourceExchangeRecipeConfigSet",
    "MapVehiclePlacementConfig", "MapVehiclePlacementConfigEntry",
}


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def need(ok: bool, msg: str) -> None:
    if not ok:
        raise ValueError(msg)


def strict_read(reader: Path, paths: list[Path]) -> dict:
    proc = subprocess.run(["ruby", str(reader), *map(str, paths)], capture_output=True,
                          text=True, timeout=30)
    need(proc.returncode == 0, "strict source YAML reader rejected input: " + proc.stderr.strip())
    return json.loads(proc.stdout)


def guid_hits(assets: Path, guid: str) -> list[Path]:
    proc = subprocess.run(["rg", "-l", "--glob", "*.meta", f"^guid: {guid}$", str(assets)],
                          capture_output=True, text=True, timeout=30)
    need(proc.returncode in (0, 1), "GUID search failed: " + proc.stderr.strip())
    hits = [Path(x) for x in proc.stdout.splitlines()]
    for hit in hits:
        raw = hit.read_bytes()
        need(0 < len(raw) <= MAX_FILE and re.findall(rb"^guid: ([0-9a-f]{32})$", raw, re.M) == [guid.encode()],
             "GUID search returned malformed meta: " + str(hit))
    return hits


def checked_asset_path(assets: Path, candidate: Path) -> Path:
    """Require a regular in-Assets path and reject symlinks before resolving it."""
    base = Path(os.path.abspath(assets))
    lexical = Path(os.path.abspath(candidate))
    need(lexical.is_relative_to(base), "asset path lexically escaped Assets: " + str(candidate))
    cursor = base
    need(not cursor.is_symlink(), "Assets root is a symlink: " + str(cursor))
    for part in lexical.relative_to(base).parts:
        cursor = cursor / part
        need(not cursor.is_symlink(), "symlink component in source path: " + str(cursor))
    resolved_base = base.resolve(strict=True)
    resolved = lexical.resolve(strict=True)
    need(resolved.is_relative_to(resolved_base), "resolved source path escaped Assets: " + str(candidate))
    need(resolved.is_file(), "resolved source is not a regular file: " + str(candidate))
    return resolved


def checked_meta_target(assets: Path, rel_meta: Path) -> tuple[Path, Path]:
    """Production GUID-target gate shared by graph construction and negative controls."""
    meta = checked_asset_path(assets, rel_meta)
    need(meta.name.endswith(".meta"), "GUID target is not a meta file: " + str(meta))
    target = checked_asset_path(assets, Path(str(meta)[:-5]))
    return meta, target


def unity_anchor(target: Path, file_id) -> bool:
    if not isinstance(file_id, (int, str)) or not re.fullmatch(r"-?\d+", str(file_id)):
        return False
    raw = target.read_bytes()
    return raw.startswith(b"%YAML") and int(file_id) in {int(x) for x in DOCUMENT.findall(raw)}


def refs(value, path="$", result=None):
    if result is None:
        result = []
    if isinstance(value, dict):
        if set(value) >= {"fileID", "guid", "type"} and isinstance(value.get("guid"), str):
            result.append((path, value))
        else:
            for key, child in value.items():
                refs(child, path + "." + str(key), result)
    elif isinstance(value, list):
        for i, child in enumerate(value):
            refs(child, f"{path}[{i}]", result)
    return result


def run(repo: Path, output: Path, controls: bool, include_operation_map_resource: bool = False) -> dict:
    repo = repo.resolve(strict=True)
    out = output.absolute()
    need(not out.exists(), "fresh output directory required; refusing overwrite")
    assets = repo / "Assets"
    reader = repo / READER_REL
    root = repo / ROOT_REL
    catalog = repo / CATALOG_REL
    matrix = repo / MATRIX_REL
    for p in (reader, repo / GENERATOR_REL, root, catalog, matrix):
        need(p.is_file() and not p.is_symlink(), "required regular source missing: " + str(p))
    parsed_cache = {}
    pins = {}
    nodes_by_guid = {}
    script_guids = {}
    queue = [root, catalog]
    asset_reference_targets = {}
    if include_operation_map_resource:
        operation_map = repo / S004_MAP_RESOURCE_REL
        need(operation_map.is_file() and not operation_map.is_symlink(),
             "actual S004 Resources operation-map asset missing")
        queue.append(operation_map)
    reference_count = 0

    def pin(path: Path) -> str:
        lexical = Path(os.path.abspath(path))
        if lexical.is_relative_to(Path(os.path.abspath(assets))):
            path = checked_asset_path(assets, lexical)
        raw = path.read_bytes()
        need(0 < len(raw) <= MAX_FILE, "empty/oversized source: " + str(path))
        pins[str(path)] = sha(raw)
        return pins[str(path)]

    pin(reader)
    pin(repo / GENERATOR_REL)
    pin(matrix)
    pin(Path(str(matrix) + ".meta"))
    # Publication and layout/setup assets are inventory roots, pinned as contextual source evidence.
    for rel in (
        "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishLayout_S004.asset",
        "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/ScenarioSetup_S004.asset",
        "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset",
    ):
        p = repo / rel
        pin(p)
        pin(Path(str(p) + ".meta"))

    while queue:
        path = checked_asset_path(assets, queue.pop(0))
        need(path.is_relative_to(assets.resolve()) and path.suffix == ".asset",
             "config dependency escaped checked-in Assets or is not an asset: " + str(path))
        rel = str(path.relative_to(repo))
        meta, _ = checked_meta_target(assets, Path(str(path) + ".meta"))
        meta_raw = meta.read_bytes()
        guids = re.findall(rb"^guid: ([0-9a-f]{32})$", meta_raw, re.M)
        need(len(guids) == 1, "asset requires exactly one serialized GUID: " + str(meta))
        guid = guids[0].decode()
        need(guid not in nodes_by_guid or nodes_by_guid[guid]["source_path"] == rel,
             "two source assets resolve to the same GUID: " + guid)
        if guid in nodes_by_guid:
            continue
        need(len(nodes_by_guid) < MAX_NODES, "typed graph node bound exceeded")
        pin(path)
        pin(meta)
        if str(path) not in parsed_cache:
            parsed_cache[str(path)] = strict_read(reader, [path])[str(path)]
        doc = parsed_cache[str(path)]
        mono = doc.get("MonoBehaviour")
        need(isinstance(mono, dict), "expected serialized MonoBehaviour source asset: " + rel)
        identifier = mono.get("m_EditorClassIdentifier", "")
        need(isinstance(identifier, str) and "::" in identifier, "typed editor class identifier missing: " + rel)
        source_type = identifier.rsplit(".", 1)[-1]
        need(source_type in ALLOWED, "source config type outside SourceConfigMaterializer profile: " + source_type)
        script_ref = mono.get("m_Script")
        need(isinstance(script_ref, dict) and script_ref.get("fileID") == 11500000 and
             isinstance(script_ref.get("guid"), str) and script_ref.get("type") == 3,
             "exact MonoScript reference required: " + rel)
        script_guid = script_ref["guid"]
        hits = guid_hits(assets, script_guid)
        need(len(hits) == 1, "MonoScript GUID must resolve uniquely: " + script_guid)
        script_meta, script_path = checked_meta_target(assets, hits[0])
        need(script_path.suffix == ".cs",
             "MonoScript GUID must target checked-in C# source: " + str(script_path))
        script_class = script_path.stem
        need(script_class == source_type, "serialized class/source script mismatch: " + rel)
        pin(script_meta)
        pin(script_path)
        script_guids[script_guid] = str(script_path.relative_to(repo))

        fields = {k: v for k, v in mono.items() if not k.startswith("m_")}
        spelling_map = doc.get("__source_scalar_spellings", {})
        spellings = {k[len("MonoBehaviour."):]: v for k, v in spelling_map.items()
                     if k.startswith("MonoBehaviour.")}
        need(spellings, "strict reader scalar spelling transcript missing: " + rel)
        row_refs = refs(fields, "fields")
        reference_count += len(row_refs)
        need(reference_count <= MAX_REFS, "typed graph reference bound exceeded")
        node = {"asset_guid": guid, "source_type": source_type, "source_path": rel,
                "source_sha256": pins[str(path)], "fields": fields, "scalar_spellings": spellings}
        nodes_by_guid[guid] = node
        if include_operation_map_resource and rel == S004_MAP_RESOURCE_REL:
            need(source_type == "OperationMapDefinition" and mono.get("m_Name") == "SkirmishS004OperationMap",
                 "S004 operation-map Resources identity changed")
            need(fields.get("operationMapId") == "opmap.skirmish.desert_base_01",
                 "S004 operation-map resource map identity changed")
            # These references are serialized AssetReference GUID strings or metadata identities,
            # not {fileID,guid,type} Unity object pointers. Resolve and pin actual targets without
            # claiming a local YAML fileID proof or materializing non-config external objects.
            referenced_guids = {}
            for field_name, field_value in fields.items():
                if isinstance(field_value, dict):
                    asset_guid = field_value.get("m_AssetGUID")
                    if isinstance(asset_guid, str) and asset_guid:
                        referenced_guids[f"{field_name}.m_AssetGUID"] = asset_guid
                    if field_name in {"gridMetadata", "surfaceMetadata"}:
                        asset_guid = field_value.get("assetGuid")
                        if isinstance(asset_guid, str) and asset_guid:
                            referenced_guids[f"{field_name}.assetGuid"] = asset_guid
            for field_name, asset_guid in sorted(referenced_guids.items()):
                ref_hits = guid_hits(assets, asset_guid)
                need(len(ref_hits) == 1, f"AssetReference/metadata GUID must resolve uniquely: {field_name}={asset_guid}")
                ref_meta, ref_target = checked_meta_target(assets, ref_hits[0])
                pin(ref_meta)
                pin(ref_target)
                asset_reference_targets[field_name] = {
                    "guid": asset_guid,
                    "target_path": str(ref_target.relative_to(repo)),
                    "target_sha256": pins[str(ref_target)],
                    "fileID_validation": "not_applicable; source serializes a GUID string without a local fileID",
                }
        for ref_path, reference in row_refs:
            dep_guid = reference["guid"]
            dep_hits = guid_hits(assets, dep_guid)
            need(len(dep_hits) == 1, f"reference GUID must resolve uniquely: {rel}:{ref_path}={dep_guid}")
            dep_meta, dep_target = checked_meta_target(assets, dep_hits[0])
            pin(dep_meta)
            pin(dep_target)
            if dep_target.suffix == ".cs":
                need(reference["fileID"] == 11500000, "MonoScript reference fileID mismatch: " + ref_path)
                continue
            need(dep_target.suffix == ".asset" and unity_anchor(dep_target, reference["fileID"]),
                 "asset reference fileID is not a declared Unity YAML anchor: " + rel + ":" + ref_path)
            # Runtime config materializer supports config assets; every object edge is included and typed.
            queue.append(dep_target)

    graph = {"schema_version": 1, "authority": "AuthoredConfigAssets",
             "source_pins": dict(sorted(pins.items())),
             "nodes": sorted(nodes_by_guid.values(), key=lambda n: n["source_path"]),
             "compiled_setup": False, "world_initialized": False, "native_world_state_used": False,
             "definition_path": ROOT_REL,
             "catalog_path": CATALOG_REL,
             "setup_matrix": {"path": str(matrix), "sha256": pins[str(matrix)]},
             "scope": "Actual S004 typed config source graph; no Unity, compilation, mission admission, physical-domain capture, source-fit or readiness."}
    if include_operation_map_resource:
        resource_guid = next(n["asset_guid"] for n in nodes_by_guid.values()
                             if n["source_path"] == S004_MAP_RESOURCE_REL)
        graph["operation_map_resource"] = {
            "path": S004_MAP_RESOURCE_REL,
            "asset_guid": resource_guid,
            "source_sha256": pins[str(repo / S004_MAP_RESOURCE_REL)],
            "resource_key": "SkirmishS004OperationMap",
            "asset_reference_targets": asset_reference_targets,
            "fileID_scope": "Unity object references in config fields are checked against declared YAML anchors; embedded AssetReference/metadata GUID strings have no local fileID and are only uniquely resolved and pinned.",
        }

    if controls:
        # Controls exercise the same strict parser, GUID resolver, and asset anchor gate with bounded fixtures.
        checked = []
        cases = [
            ("duplicate-key", b"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  repeated: 1\n  repeated: 2\n"),
            ("alias", b"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  value: &x 1\n  copy: *x\n"),
        ]
        with tempfile.TemporaryDirectory(prefix="s004-typed-negative-") as temp:
            for name, raw in cases:
                fixture = Path(temp) / (name + ".asset")
                fixture.write_bytes(raw)
                try:
                    strict_read(reader, [fixture])
                    rejected = False
                except (ValueError, json.JSONDecodeError):
                    rejected = True
                need(rejected, "strict YAML negative was accepted: " + name)
                checked.append({"case": name, "result": "RejectedAsExpected"})
            missing_guid = "a" * 32
            # Exercise the exact production meta-to-target gate against a private Assets root.
            private_assets = Path(temp) / "Assets"
            private_assets.mkdir()
            only_meta = private_assets / "missing.asset.meta"
            only_meta.write_text("guid: " + missing_guid + "\n", encoding="utf-8")
            hits = guid_hits(private_assets, missing_guid)
            need(len(hits) == 1, "missing target control setup failed")
            rejected = False
            try:
                checked_meta_target(private_assets, hits[0])
            except (OSError, ValueError):
                rejected = True
            need(rejected, "production meta-to-target gate accepted missing target")
            checked.append({"case": "missing-guid-target", "result": "RejectedAsExpected"})
            duplicate = private_assets / "duplicate.asset.meta"
            duplicate.write_text("guid: " + missing_guid + "\n", encoding="utf-8")
            need(len(guid_hits(private_assets, missing_guid)) == 2, "duplicate GUID control setup failed")
            checked.append({"case": "duplicate-guid", "result": "AmbiguousAsExpected"})
            symlink_target = private_assets / "symlinked.asset"
            real_target = private_assets / "real.asset"
            real_target.write_text("source\n", encoding="utf-8")
            symlink_target.symlink_to(real_target)
            symlink_meta = private_assets / "symlinked.asset.meta"
            symlink_meta.write_text("guid: " + "b" * 32 + "\n", encoding="utf-8")
            rejected = False
            try:
                checked_meta_target(private_assets, symlink_meta)
            except (OSError, ValueError):
                rejected = True
            need(rejected, "production meta-to-target gate accepted symlink target")
            checked.append({"case": "symlink-target", "result": "RejectedAsExpected"})
            anchor = Path(temp) / "anchor.asset"
            anchor.write_bytes(b"%YAML 1.1\n--- !u!114 &11400000\nMonoBehaviour: {}\n")
            need(unity_anchor(anchor, 11400000) and not unity_anchor(anchor, 12345), "fileID control failed")
            checked.extend([{"case": "matching-unity-anchor", "result": "ProvenAsExpected"},
                            {"case": "missing-unity-anchor", "result": "RejectedAsExpected"}])
        graph["negative_controls"] = checked

    # Recheck every source pin after traversal to fail closed on concurrent source drift.
    for path, digest in pins.items():
        need(sha(Path(path).read_bytes()) == digest, "pinned source changed during graph generation: " + path)
    out.mkdir(parents=True)
    target = out / "source-config-graph.json"
    target.write_text(json.dumps(graph, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return {"status": "Passed", "kind": "S004TypedConfigSourceGraph", "node_count": len(nodes_by_guid),
            "reference_count": reference_count, "script_count": len(script_guids),
            "graph": {"path": str(target), "sha256": sha(target.read_bytes())},
            "negative_controls": graph.get("negative_controls", []), "compiled_setup": False,
            "world_initialized": False, "mission_ready": False, "scope": graph["scope"]}


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--repo", type=Path, required=True)
    ap.add_argument("--output", type=Path, required=True)
    ap.add_argument("--negative-controls", action="store_true")
    ap.add_argument("--include-operation-map-resource", action="store_true",
                    help="add the actual Resources/SkirmishS004OperationMap asset and pin its serialized GUID-string targets")
    args = ap.parse_args()
    try:
        print(json.dumps(run(args.repo, args.output, args.negative_controls,
                             args.include_operation_map_resource), indent=2))
    except Exception as exc:
        raise SystemExit("S004 typed config graph failed closed: " + str(exc))


if __name__ == "__main__":
    main()

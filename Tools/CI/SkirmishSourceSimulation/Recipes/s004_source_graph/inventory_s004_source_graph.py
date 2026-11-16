#!/usr/bin/env python3
"""Bounded, read-only inventory of checked-in S004 Unity YAML references.

This records current source files and direct GUID/fileID references only. It does
not hydrate Unity objects, compile a mission, or qualify physical map fit/readiness.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import tempfile

HERE = Path(__file__).resolve().parent
READER = HERE.parent / "selected_operation_map" / "helpers" / "read_source_configs.rb"
ROOTS = [
    "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishScenario_S004.asset",
    "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishLayout_S004.asset",
    "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/ScenarioSetup_S004.asset",
    "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset",
]
MAX_FILE = 2 * 1024 * 1024
MAX_ROOTS = 8
MAX_REFS = 256
UNITY_DOCUMENT = re.compile(rb"^--- !u!\d+ &(-?\d+)\s*$", re.M)


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def require(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def parse(reader: Path, paths: list[Path]) -> dict:
    proc = subprocess.run(["ruby", str(reader), *map(str, paths)], capture_output=True,
                          text=True, timeout=20)
    require(proc.returncode == 0, "strict YAML reader rejected input: " + proc.stderr.strip())
    return json.loads(proc.stdout)


def find_guid(assets: Path, guid: str, cache: dict[str, list[str]]) -> list[str]:
    if guid not in cache:
        proc = subprocess.run(["rg", "-l", "--glob", "*.meta", f"^guid: {guid}$", str(assets)],
                              capture_output=True, text=True, timeout=20)
        require(proc.returncode in (0, 1), "GUID lookup failed: " + proc.stderr.strip())
        hits = sorted(Path(p) for p in proc.stdout.splitlines())
        found = []
        for meta in hits:
            raw = meta.read_bytes()
            require(len(raw) <= MAX_FILE, "meta exceeds bound: " + str(meta))
            matches = re.findall(rb"^guid: ([0-9a-f]{32})$", raw, re.M)
            require(matches == [guid.encode()], "GUID lookup returned a non-exact meta: " + str(meta))
            found.append(str(meta.relative_to(assets.parent)))
        cache[guid] = found
    return cache[guid]


def target_for_meta(assets: Path, rel_meta: str) -> Path:
    meta = assets.parent / rel_meta
    require(meta.parent.resolve().is_relative_to(assets.resolve()), "resolved meta escaped Assets")
    require(not meta.is_symlink(), "resolved meta is a symlink: " + str(meta))
    target = Path(str(meta)[:-5])
    require(target.parent.resolve().is_relative_to(assets.resolve()), "resolved target escaped Assets")
    require(not target.is_symlink() and target.is_file(),
            "resolved GUID target missing, non-file, or symlink: " + str(target))
    return target


def validate_file_id(target: Path, file_id) -> tuple[str, str]:
    """Prove a local fileID only when its serialized Unity document declares it."""
    if not isinstance(file_id, (int, str)) or not re.fullmatch(r"-?\d+", str(file_id)):
        return "unresolved", "fileID is not a serialized integer"
    if target.suffix not in {".asset", ".prefab"}:
        return "unresolved", "target format is not supported serialized Unity YAML"
    raw = target.read_bytes()
    if not raw.startswith(b"%YAML") or not UNITY_DOCUMENT.search(raw):
        return "unresolved", "target is not a recognized Unity YAML document"
    declared = {int(value) for value in UNITY_DOCUMENT.findall(raw)}
    if int(file_id) in declared:
        return "proven", "fileID matches a serialized Unity document anchor"
    return "unresolved", "fileID is not declared by a serialized Unity document anchor"


def refs(value, path="$", out=None):
    if out is None:
        out = []
    if isinstance(value, dict):
        if set(value) >= {"fileID", "guid", "type"} and isinstance(value.get("guid"), str):
            out.append({"field": path, "fileID": value["fileID"], "guid": value["guid"],
                        "type": value["type"]})
        else:
            for key, child in value.items():
                refs(child, path + "." + str(key), out)
    elif isinstance(value, list):
        for i, child in enumerate(value):
            refs(child, f"{path}[{i}]", out)
    return out


def run(repo: Path, output: Path, negative_controls: bool) -> dict:
    repo = repo.resolve()
    out = output.absolute()
    require(not out.exists(), "fresh output directory required; refusing overwrite")
    root_paths = [repo / item for item in ROOTS]
    require(len(root_paths) <= MAX_ROOTS, "root bound exceeded")
    pins = {}
    for path in root_paths + [READER]:
        raw = path.read_bytes()
        require(0 < len(raw) <= MAX_FILE, "missing/oversized bounded input: " + str(path))
        pins[str(path)] = sha(raw)
    parsed = parse(READER, root_paths)
    guid_cache: dict[str, list[str]] = {}
    rows = []
    unresolved = []
    all_refs = []
    for path in root_paths:
        key = str(path)
        doc = parsed[key]
        require(isinstance(doc, dict) and isinstance(doc.get("MonoBehaviour"), dict),
                "expected one Unity MonoBehaviour document: " + key)
        mb = doc["MonoBehaviour"]
        row_refs = refs(mb, "MonoBehaviour")
        all_refs.extend(row_refs)
        for ref in row_refs:
            hits = find_guid(repo / "Assets", ref["guid"], guid_cache)
            ref["resolved_meta"] = hits[0] if len(hits) == 1 else None
            ref["guid_resolution"] = "unique" if len(hits) == 1 else ("missing" if not hits else "ambiguous")
            ref["fileID_validation"] = "unresolved"
            ref["target_validation"] = "not_checked"
            if len(hits) != 1:
                unresolved.append({"source": key, **ref})
            for rel in hits:
                meta_path = repo / rel
                raw = meta_path.read_bytes()
                pins[str(meta_path)] = sha(raw)
                target = target_for_meta(repo / "Assets", rel)
                target_raw = target.read_bytes()
                require(0 < len(target_raw) <= MAX_FILE, "resolved target empty or oversized: " + str(target))
                pins[str(target)] = sha(target_raw)
                ref["resolved_target"] = str(target.relative_to(repo))
                ref["target_sha256"] = pins[str(target)]
                ref["target_validation"] = "present_regular_file_pinned"
                validation, reason = validate_file_id(target, ref["fileID"])
                ref["fileID_validation"] = validation
                ref["fileID_validation_reason"] = reason
                if validation != "proven":
                    unresolved.append({"source": key, **ref})
        rows.append({"path": str(path.relative_to(repo)), "sha256": pins[key],
                     "name": mb.get("m_Name"), "editor_class": mb.get("m_EditorClassIdentifier"),
                     "catalog_id": mb.get("catalogId"), "definition_id": mb.get("definitionId"),
                     "scenario_id": mb.get("scenarioId"), "operation_map_id": mb.get("operationMapId"),
                     "references": row_refs})
    require(len(all_refs) <= MAX_REFS, "direct reference bound exceeded")
    # Explicitly record expected key source files without converting their YAML
    # into runtime/compiler objects. The source graph is a direct-reference census.
    result = {"schema_version": 1, "kind": "S004CheckedInSourceGraphInventory",
              "status": "InventoryComplete" if not unresolved else "InventoryWithUnresolvedReferences",
              "scope": "Checked-in S004 scenario/layout/setup/publication YAML and direct GUID/fileID references; no Unity, hydration, mission compilation, physical-domain capture, source-fit, or readiness claim.",
              "roots": rows, "direct_reference_count": len(all_refs),
              "unique_guid_resolutions": sum(x["guid_resolution"] == "unique" for x in all_refs),
              "fileID_validation": "proven only when a direct reference fileID matches an anchor in a supported serialized .asset/.prefab Unity YAML target; other formats and missing anchors remain unresolved",
              "proven_fileID_references": sum(x["fileID_validation"] == "proven" for x in all_refs),
              "unresolved_fileID_references": sum(x["fileID_validation"] != "proven" for x in all_refs),
              "unresolved_references": unresolved,
              "source_pins": pins,
              "provenance": "Values parsed by the existing strict selected_operation_map YAML reader; references resolved against current checked-in Assets .meta GUIDs."}
    if negative_controls:
        controls = []
        cases = [
            ("duplicate-key", b"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  repeated: 1\n  repeated: 2\n"),
            ("alias", b"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  value: &x 1\n  copy: *x\n"),
        ]
        with tempfile.TemporaryDirectory(prefix="s004-yaml-negative-") as temp:
            for name, raw in cases:
                sample = Path(temp) / (name + ".asset")
                sample.write_bytes(raw)
                try:
                    parse(READER, [sample])
                    rejected = False
                except (ValueError, json.JSONDecodeError):
                    rejected = True
                require(rejected, "strict parser accepted negative control: " + name)
                controls.append({"case": name, "result": "RejectedAsExpected"})
        with tempfile.TemporaryDirectory(prefix="s004-guid-negative-") as temp:
            assets = Path(temp) / "Assets"
            assets.mkdir()
            guid = "a" * 32
            missing_meta = assets / "missing.asset.meta"
            missing_meta.write_text("guid: " + guid + "\n", encoding="utf-8")
            cache = {}
            missing_hits = find_guid(assets, guid, cache)
            require(len(missing_hits) == 1, "missing-target fixture GUID should resolve uniquely")
            rejected = False
            try:
                target_for_meta(assets, missing_hits[0])
            except ValueError:
                rejected = True
            require(rejected, "missing GUID target negative control was not rejected")
            controls.append({"case": "missing-guid-target", "result": "RejectedAsExpected"})

            duplicate = assets / "duplicate.asset.meta"
            duplicate.write_text("guid: " + guid + "\n", encoding="utf-8")
            hits = find_guid(assets, guid, {})
            require(len(hits) != 1, "duplicate GUID negative control was not ambiguous")
            controls.append({"case": "duplicate-guid", "result": "AmbiguousAsExpected"})
        with tempfile.TemporaryDirectory(prefix="s004-fileid-controls-") as temp:
            sample = Path(temp) / "sample.asset"
            sample.write_bytes(b"%YAML 1.1\n--- !u!114 &11400000\nMonoBehaviour: {}\n")
            require(validate_file_id(sample, 11400000)[0] == "proven",
                    "serialized anchor control was not proven")
            controls.append({"case": "matching-unity-anchor", "result": "ProvenAsExpected"})
            require(validate_file_id(sample, 12345)[0] == "unresolved",
                    "missing anchor control was not unresolved")
            controls.append({"case": "missing-unity-anchor", "result": "UnresolvedAsExpected"})
            script = Path(temp) / "source.cs"
            script.write_text("class Source {}\n", encoding="utf-8")
            require(validate_file_id(script, 11500000)[0] == "unresolved",
                    "unsupported script target was not unresolved")
            controls.append({"case": "unsupported-script-target", "result": "UnresolvedAsExpected"})
        result["negative_controls"] = controls
    # Inputs are checked again immediately before emission to catch concurrent drift.
    require(all(sha(Path(path).read_bytes()) == digest for path, digest in pins.items()),
            "source changed during inventory")
    out.mkdir(parents=True)
    target = out / "inventory.json"
    target.write_text(json.dumps(result, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    result["output"] = {"path": str(target), "sha256": sha(target.read_bytes())}
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--negative-controls", action="store_true")
    args = parser.parse_args()
    try:
        print(json.dumps(run(args.repo, args.output, args.negative_controls), indent=2))
    except Exception as exc:
        raise SystemExit("S004 source graph inventory failed closed: " + str(exc))


if __name__ == "__main__":
    main()

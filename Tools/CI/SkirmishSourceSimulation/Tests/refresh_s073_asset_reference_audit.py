"""Revalidate the retained S073 direct-reference inventory against current YAML and assets."""
import argparse
from collections import Counter
import copy
import hashlib
import json
from pathlib import Path
import re
import subprocess


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def refs(node, source, field=""):
    if isinstance(node, dict):
        if "guid" in node and "fileID" in node:
            yield (source, field, str(node["guid"]), int(node["fileID"]), int(node.get("type", 0)))
            return
        for key, value in node.items():
            if key != "__source_scalar_spellings":
                yield from refs(value, source, field + "." + key if field else key)
    elif isinstance(node, list):
        for index, value in enumerate(node): yield from refs(value, source, field + "[" + str(index) + "]")


def main():
    p = argparse.ArgumentParser(); p.add_argument("--repo", required=True); p.add_argument("--readback-audit", required=True); p.add_argument("--output-dir", required=True)
    a = p.parse_args(); repo = Path(a.repo).resolve(); out = Path(a.output_dir); out.mkdir(parents=True, exist_ok=False)
    baseline = repo / "Tools/CI/SkirmishSourceSimulation/Fixtures/S073/retained/source-asset-reference-audit.json"
    old = json.loads(baseline.read_text()); readback = json.loads(Path(a.readback_audit).read_text())
    if (old.get("kind") != "S073LegacySourceAssetReferenceAudit" or old.get("mission") != "S073"
            or readback.get("kind") != "S073LegacyConfigYamlReadback" or readback.get("mission") != "S073"
            or readback.get("retained_asset_audit_sha256") != sha(baseline)):
        raise ValueError("Verified retained reference audit and fresh S073 YAML readback required")
    roots = old["configuration_roots"]; paths = [r["path"] for r in roots]
    if len(paths) != 12 or len(set(paths)) != 12: raise ValueError("Exact 12 configuration roots required")
    initial = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset"
    if readback.get("source_artifacts", {}).get(initial) != sha(repo / initial):
        raise ValueError("Fresh YAML readback does not pin the current InitialForces asset")
    for root in roots:
        path = (repo / root["path"]).resolve()
        if not path.is_relative_to(repo): raise ValueError("Source root escaped repository")
        if root["path"] != initial and sha(path) != root["sha256"]: raise ValueError("Unchanged source root differs: " + root["path"])
    parser = repo / old["parser"]["path"]
    if sha(parser) != old["parser"]["sha256"]: raise ValueError("Source YAML parser changed")
    root_epoch = {path: sha(repo / path) for path in paths}
    result = subprocess.run(["ruby", str(parser)] + [str(repo / path) for path in paths], check=True, capture_output=True)
    parsed = json.loads(result.stdout)
    actual = Counter(item for path in paths for item in refs(parsed[str(repo / path)], path))
    expected = Counter((use["source_path"], use["field_path"], row["guid"], int(use["file_id"]), int(use["type"]))
                       for row in old["references"] for use in row["uses"])
    if actual != expected: raise ValueError("Current complete direct-reference inventory differs from retained audit")
    audit = copy.deepcopy(old); changed = []
    for row in audit["references"]:
        asset = (repo / row["asset_path"]).resolve(); meta = (repo / row["meta_path"]).resolve()
        if (not asset.is_relative_to(repo) or not meta.is_relative_to(repo)
                or not re.search(r"(?m)^guid: " + re.escape(row["guid"]) + r"\s*$", meta.read_text())):
            raise ValueError("Current referenced asset or GUID changed: " + row["asset_path"])
        current_asset, current_meta = sha(asset), sha(meta)
        if current_asset != row["asset_sha256"] or current_meta != row["meta_sha256"]:
            changed.append({"path":row["asset_path"], "previous_asset_sha256":row["asset_sha256"], "current_asset_sha256":current_asset,
                            "previous_meta_sha256":row["meta_sha256"], "current_meta_sha256":current_meta})
        row["asset_sha256"] = current_asset; row["meta_sha256"] = current_meta
    guid_paths = {}
    for meta in (repo / "Assets").rglob("*.meta"):
        match = re.search(r"(?m)^guid: ([0-9a-f]{32})\s*$", meta.read_text(errors="strict"))
        if match: guid_paths.setdefault(match.group(1), []).append(meta.resolve())
    for row in audit["references"]:
        if guid_paths.get(row["guid"]) != [(repo / row["meta_path"]).resolve()]:
            raise ValueError("Referenced GUID is missing, ambiguous, or moved: " + row["guid"])
    readback_path = out / "current-yaml-readback.json"; readback_path.write_bytes(result.stdout)
    audit["configuration_roots"] = [dict(row, sha256=root_epoch[row["path"]]) for row in roots]
    audit["readback_artifact_sha256"] = sha(readback_path)
    (out / "changed-reference-assets.json").write_text(json.dumps(changed, indent=2) + "\n")
    for row in audit["configuration_roots"]:
        if sha(repo / row["path"]) != row["sha256"]: raise ValueError("Configuration changed during audit")
    for row in audit["references"]:
        if sha(repo / row["asset_path"]) != row["asset_sha256"] or sha(repo / row["meta_path"]) != row["meta_sha256"]:
            raise ValueError("Referenced asset changed during audit")
    audit_path = out / "source-asset-reference-audit.json"; audit_path.write_text(json.dumps(audit, indent=2) + "\n")
    print(json.dumps({"status":"Passed", "roots":len(roots), "direct_reference_uses":sum(actual.values()),
                      "audit":str(audit_path), "audit_sha256":sha(audit_path), "readback_sha256":sha(readback_path),
                      "scope":"Current YAML direct-reference and asset identity revalidation; not a Unity world or readiness"}))


if __name__ == "__main__": main()

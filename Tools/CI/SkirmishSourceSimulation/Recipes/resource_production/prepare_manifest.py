"""Prepare an isolated alternate source-host manifest for owner compilation."""
import argparse
import hashlib
import json
from pathlib import Path

EXPECTED = {
    "284_BuildingResourceProductionEcsSystem.cs": ("native_direct", "Assets/Game/Scripts/Systems/BuildingResourceProductionEcsSystem.cs"),
    "285_BuildingResourceProductionSystemHelper.cs": ("native_direct", "Assets/Game/Scripts/Systems/BuildingResourceProductionSystemHelper.cs"),
    "407_FactionResourceCompositionSystemHelper.cs": ("native_direct", "Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs"),
}


def prepare(source, output):
    raw = source.read_bytes()
    manifest = json.loads(raw)
    rows = {row.get("id"): row for row in manifest.get("sources", [])}
    for key, (kind, path) in EXPECTED.items():
        row = rows.get(key)
        if row is None or row.get("kind") != kind or row.get("path") != path:
            raise ValueError("Exact original production owner manifest row required: " + key)
    output.parent.mkdir(parents=True, exist_ok=True)
    manifest["experimental_only"] = True
    manifest["experiment"] = "s073_original_resource_production_owner_compile"
    manifest["resource_production_owner_compile_scope"] = {
        "owner_rows": sorted(EXPECTED),
        "claim": "Compile-only current source-host closure; no Unity world, startup, mission run or readiness",
    }
    output.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    if source.read_bytes() != raw:
        raise RuntimeError("Default manifest changed during alternate preparation")
    return {"source_manifest_sha256": hashlib.sha256(raw).hexdigest(),
            "alternate_manifest_sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
            "owner_rows": sorted(EXPECTED)}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(prepare(args.manifest.resolve(strict=True), args.output), indent=2))

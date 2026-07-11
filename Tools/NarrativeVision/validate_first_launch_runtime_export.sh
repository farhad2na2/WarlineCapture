#!/usr/bin/env bash

set -euo pipefail

repo_root=$(git rev-parse --show-toplevel)
manifest="$repo_root/Assets/Game/Art/Narrative/FirstLaunch/approved_first_launch_art_manifest.json"
report="$repo_root/Design/NarrativeVision/FirstLaunch/ArtReview/FinalArt/Evidence/APPROVED_RUNTIME_EXPORT_VALIDATION.json"

python3 - "$repo_root" "$manifest" "$report" <<'PY'
import hashlib
import json
from pathlib import Path
import sys

repo_root = Path(sys.argv[1])
manifest_path = Path(sys.argv[2])
report_path = Path(sys.argv[3])
errors = []

if not manifest_path.is_file():
    raise SystemExit(f"Missing runtime manifest: {manifest_path}")

manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
panels = manifest.get("panels", [])

if manifest.get("status") != "Gate6UserApproved":
    errors.append("Manifest status is not Gate6UserApproved")
if manifest.get("panelCount") != 22 or len(panels) != 22:
    errors.append("Manifest must contain exactly 22 panels")
if manifest.get("dialogueAndInteractiveUiBakedIntoPanels") is not False:
    errors.append("Manifest must declare dialogue and interactive UI as separate runtime layers")

expected_ids = [f"FL-P{i:02d}" for i in range(1, 23)]
actual_ids = [panel.get("panelId") for panel in panels]
if actual_ids != expected_ids:
    errors.append("Panel IDs are missing, duplicated, or out of order")

def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()

verified = []
for panel in panels:
    panel_id = panel.get("panelId", "unknown")
    record = {"panelId": panel_id, "revision": panel.get("approvedRevision"), "valid": True}
    for path_key, hash_key in (
        ("sourceMaster", "sourceMasterSha256"),
        ("runtime16x9", "runtime16x9Sha256"),
        ("runtime20x9", "runtime20x9Sha256"),
    ):
        relative_path = panel.get(path_key, "")
        asset_path = repo_root / relative_path
        if not asset_path.is_file():
            errors.append(f"{panel_id}: missing {path_key} file {relative_path}")
            record["valid"] = False
            continue
        actual_hash = sha256(asset_path)
        if actual_hash != panel.get(hash_key):
            errors.append(f"{panel_id}: {path_key} hash mismatch")
            record["valid"] = False

        if path_key.startswith("runtime"):
            meta_path = Path(str(asset_path) + ".meta")
            if not meta_path.is_file():
                errors.append(f"{panel_id}: missing Unity meta for {relative_path}")
                record["valid"] = False
                continue
            meta = meta_path.read_text(encoding="utf-8")
            required_fragments = (
                "textureType: 8",
                "spriteMode: 1",
                "enableMipMap: 0",
                "isReadable: 0",
                "wrapU: 1",
                "buildTarget: Android",
                "buildTarget: iOS",
                "textureFormat: 50",
            )
            for fragment in required_fragments:
                if fragment not in meta:
                    errors.append(f"{panel_id}: importer metadata missing {fragment!r}")
                    record["valid"] = False
    verified.append(record)

runtime_pngs = sorted((repo_root / "Assets/Game/Art/Narrative/FirstLaunch/Panels").rglob("FL-P??.png"))
if len(runtime_pngs) != 44:
    errors.append(f"Expected 44 runtime PNG files, found {len(runtime_pngs)}")

payload = {
    "schemaVersion": 1,
    "status": "pass" if not errors else "fail",
    "gate": "Gate 6",
    "approvedOn": manifest.get("approvedOn"),
    "panelCount": len(panels),
    "runtimeTextureCount": len(runtime_pngs),
    "unityLayeredComposition": manifest.get("runtimeComposition") == "UnityLayered",
    "verifiedPanels": verified,
    "errors": errors,
}
report_path.parent.mkdir(parents=True, exist_ok=True)
report_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")

if errors:
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    raise SystemExit(1)

print("Approved FirstLaunch runtime export validation passed for 22 panels and 44 textures.")
print(f"Report: {report_path.relative_to(repo_root)}")
PY

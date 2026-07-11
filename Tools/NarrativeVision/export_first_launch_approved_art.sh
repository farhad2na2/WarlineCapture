#!/usr/bin/env bash

set -euo pipefail

repo_root=$(git rev-parse --show-toplevel)
final_art_root="$repo_root/Design/NarrativeVision/FirstLaunch/ArtReview/FinalArt"
source_dir="$final_art_root/SourceMasters"
preview_dir="$final_art_root/Previews"
ledger="$final_art_root/FINAL_ART_REVIEW_LEDGER.md"
runtime_root="$repo_root/Assets/Game/Art/Narrative/FirstLaunch"
runtime_16="$runtime_root/Panels/16x9"
runtime_20="$runtime_root/Panels/20x9"
manifest="$runtime_root/approved_first_launch_art_manifest.json"

for command_name in find shasum python3; do
    if ! command -v "$command_name" >/dev/null 2>&1; then
        echo "Missing required command: $command_name" >&2
        exit 69
    fi
done

bash "$repo_root/Tools/NarrativeVision/validate_first_launch_final_art.sh"

approved_count=$(grep -cE '^\| `FL-P[0-9]{2}` \| .* \| Approved \| User / 2026-07-11 \|' "$ledger" || true)
if [[ "$approved_count" -ne 22 ]]; then
    echo "Runtime export requires 22 exact user-approved ledger rows; found $approved_count." >&2
    exit 65
fi

mkdir -p "$runtime_16" "$runtime_20"
records="$runtime_root/.approved_first_launch_art_records.tsv"
: > "$records"

find_one() {
    local directory="$1"
    local pattern="$2"
    local matches=()
    while IFS= read -r path; do
        matches+=("$path")
    done < <(find "$directory" -maxdepth 1 -type f -name "$pattern" -print | LC_ALL=C sort)

    if [[ ${#matches[@]} -ne 1 ]]; then
        echo "Expected one file for $pattern in $directory; found ${#matches[@]}." >&2
        exit 65
    fi

    printf '%s\n' "${matches[0]}"
}

for panel_number in $(seq -w 1 22); do
    panel_id="FL-P${panel_number}"
    source=$(find_one "$source_dir" "${panel_id}_*Candidate_R*.png")
    preview_16=$(find_one "$preview_dir" "${panel_id}_16x9_R*.png")
    preview_20=$(find_one "$preview_dir" "${panel_id}_20x9_R*.png")
    source_name=$(basename "$source")

    if [[ ! "$source_name" =~ _R([1-9][0-9]*)\.png$ ]]; then
        echo "Cannot parse revision from $source_name" >&2
        exit 65
    fi
    revision="R${BASH_REMATCH[1]}"

    runtime_16_path="$runtime_16/${panel_id}.png"
    runtime_20_path="$runtime_20/${panel_id}.png"
    cp "$preview_16" "$runtime_16_path"
    cp "$preview_20" "$runtime_20_path"

    source_hash=$(shasum -a 256 "$source" | awk '{print $1}')
    hash_16=$(shasum -a 256 "$runtime_16_path" | awk '{print $1}')
    hash_20=$(shasum -a 256 "$runtime_20_path" | awk '{print $1}')

    printf '%s\t%s\t%s\t%s\t%s\t%s\n' \
        "$panel_id" "$revision" "$source_hash" "$hash_16" "$hash_20" \
        "${source#$repo_root/}" >> "$records"
done

python3 - "$repo_root" "$records" "$manifest" <<'PY'
import json
from pathlib import Path
import sys

repo_root = Path(sys.argv[1])
records_path = Path(sys.argv[2])
manifest_path = Path(sys.argv[3])

panels = []
for line in records_path.read_text(encoding="utf-8").splitlines():
    panel_id, revision, source_hash, hash_16, hash_20, source_path = line.split("\t")
    panels.append(
        {
            "panelId": panel_id,
            "approvedRevision": revision,
            "approvedOn": "2026-07-11",
            "sourceMaster": source_path,
            "sourceMasterSha256": source_hash,
            "runtime16x9": f"Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/{panel_id}.png",
            "runtime16x9Sha256": hash_16,
            "runtime20x9": f"Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/{panel_id}.png",
            "runtime20x9Sha256": hash_20,
        }
    )

if [panel["panelId"] for panel in panels] != [f"FL-P{i:02d}" for i in range(1, 23)]:
    raise SystemExit("Runtime export manifest panel order is invalid")

payload = {
    "schemaVersion": 1,
    "status": "Gate6UserApproved",
    "approvedOn": "2026-07-11",
    "panelCount": 22,
    "runtimeComposition": "UnityLayered",
    "cleanPanelArt": True,
    "dialogueAndInteractiveUiBakedIntoPanels": False,
    "panels": panels,
}
manifest_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
PY

rm -f "$records"

runtime_count=$(find "$runtime_16" "$runtime_20" -maxdepth 1 -type f -name 'FL-P??.png' | wc -l | tr -d ' ')
if [[ "$runtime_count" -ne 44 ]]; then
    echo "Expected 44 exported panel textures; found $runtime_count." >&2
    exit 65
fi

echo "Exported 22 approved FirstLaunch panels in 16:9 and 20:9 runtime formats."
echo "Manifest: ${manifest#$repo_root/}"

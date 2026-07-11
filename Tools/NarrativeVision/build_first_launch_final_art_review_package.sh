#!/usr/bin/env bash

set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/../.." && pwd)

final_art_root="$repo_root/Design/NarrativeVision/FirstLaunch/ArtReview/FinalArt"
source_dir="$final_art_root/SourceMasters"
preview_dir="$final_art_root/Previews"
storyboard_dir="$repo_root/Design/NarrativeVision/FirstLaunch/storyboard/frames"
evidence_dir="$final_art_root/Evidence"

font="/System/Library/Fonts/Helvetica.ttc"
bg="#141c26"
panel_count=22

contact_output="$evidence_dir/FINAL_ART_CONTACT_16x9.png"
safe16_output="$evidence_dir/FINAL_ART_SAFEAREA_CONTACT_16x9.png"
safe20_output="$evidence_dir/FINAL_ART_SAFEAREA_CONTACT_20x9.png"
comparison_output="$evidence_dir/FINAL_ART_STORYBOARD_COMPARISON.png"
summary_output="$evidence_dir/FINAL_ART_REFERENCE_SUMMARY.png"

reference_paths=(
    "$repo_root/Design/NarrativeVision/FirstLaunch/ArtReview/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-01_GeographyMaster_CandidateB.png"
    "$repo_root/Design/NarrativeVision/FirstLaunch/ArtReview/ContinuityCandidates/DirectionB_MatchAligned/CHAR-COMMANDER-01_PortraitChoices.png"
    "$repo_root/Design/NarrativeVision/FirstLaunch/ArtReview/ContinuityCandidates/DirectionB_MatchAligned/CHAR-SAMIRA-01_CandidateA.png"
    "$repo_root/Design/NarrativeVision/FirstLaunch/ArtReview/WorldCandidates/DirectionB_MatchAligned/WORLD-FX-01_ReusableEffectsSheet.png"
    "$repo_root/Design/NarrativeVision/FirstLaunch/ArtReview/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-03_AttackBlackout_CandidateB.png"
    "$repo_root/Design/NarrativeVision/FirstLaunch/ArtReview/WorldCandidates/DirectionB_MatchAligned/WORLD-RELAY-01_DamagedCommandPost.png"
)

for required_command in magick find sort awk stat; do
    if ! command -v "$required_command" >/dev/null 2>&1; then
        echo "Missing required command: $required_command" >&2
        exit 69
    fi
done

for required_dir in "$source_dir" "$preview_dir" "$storyboard_dir" "$evidence_dir"; do
    if [[ ! -d "$required_dir" ]]; then
        echo "Missing required directory: $required_dir" >&2
        exit 66
    fi
done

if [[ ! -f "$font" ]]; then
    echo "Missing review-package font: $font" >&2
    exit 66
fi

for reference_path in "${reference_paths[@]}"; do
    if [[ ! -f "$reference_path" ]]; then
        echo "Missing reference summary asset: $reference_path" >&2
        exit 66
    fi
done

work_dir=$(mktemp -d "${TMPDIR:-/tmp}/first-launch-final-art-review.XXXXXX")
trap 'rm -rf -- "$work_dir"' EXIT

declare -a panel_ids=()
declare -a source_paths=()
declare -a preview16_paths=()
declare -a preview20_paths=()
declare -a storyboard_paths=()

find_one_match() {
    local search_dir="$1"
    local pattern="$2"
    local label="$3"
    local -a matches=()

    while IFS= read -r matched_path; do
        matches+=("$matched_path")
    done < <(find "$search_dir" -maxdepth 1 -type f -name "$pattern" -print | LC_ALL=C sort)

    if [[ ${#matches[@]} -ne 1 ]]; then
        echo "Expected exactly one $label; found ${#matches[@]} for pattern $pattern in $search_dir" >&2
        exit 65
    fi

    printf '%s\n' "${matches[0]}"
}

extract_revision() {
    local path="$1"
    local basename
    basename=$(basename "$path")
    if [[ "$basename" =~ _R([1-9][0-9]*)\.png$ ]]; then
        printf 'R%s\n' "${BASH_REMATCH[1]}"
        return 0
    fi

    echo "Could not parse revision from $path" >&2
    exit 65
}

compose_tile() {
    local canvas="$1"
    local tile="$2"
    local x="$3"
    local y="$4"
    local output="$5"

    magick "$canvas" "$tile" -geometry "+${x}+${y}" -compose over -composite "$output"
}

for panel_number in $(seq 1 "$panel_count"); do
    panel_id=$(printf 'FL-P%02d' "$panel_number")
    source_path=$(find_one_match "$source_dir" "${panel_id}_*Candidate_R*.png" "$panel_id source master")
    preview16_path=$(find_one_match "$preview_dir" "${panel_id}_16x9_R*.png" "$panel_id 16x9 preview")
    preview20_path=$(find_one_match "$preview_dir" "${panel_id}_20x9_R*.png" "$panel_id 20x9 preview")
    storyboard_path="$storyboard_dir/${panel_id}.png"

    if [[ ! -f "$storyboard_path" ]]; then
        echo "Missing storyboard frame for $panel_id: $storyboard_path" >&2
        exit 66
    fi

    source_revision=$(extract_revision "$source_path")
    preview16_revision=$(extract_revision "$preview16_path")
    preview20_revision=$(extract_revision "$preview20_path")

    if [[ "$source_revision" != "$preview16_revision" || "$source_revision" != "$preview20_revision" ]]; then
        echo "Revision mismatch for $panel_id: source=$source_revision 16x9=$preview16_revision 20x9=$preview20_revision" >&2
        exit 65
    fi

    panel_ids+=("$panel_id")
    source_paths+=("$source_path")
    preview16_paths+=("$preview16_path")
    preview20_paths+=("$preview20_path")
    storyboard_paths+=("$storyboard_path")
done

contact_tile_dir="$work_dir/contact_tiles"
safe16_tile_dir="$work_dir/safe16_tiles"
safe20_tile_dir="$work_dir/safe20_tiles"
comparison_tile_dir="$work_dir/comparison_tiles"
mkdir -p "$contact_tile_dir" "$safe16_tile_dir" "$safe20_tile_dir" "$comparison_tile_dir"

for ((index = 0; index < panel_count; index++)); do
    panel_id="${panel_ids[$index]}"
    preview16_path="${preview16_paths[$index]}"
    preview20_path="${preview20_paths[$index]}"
    storyboard_path="${storyboard_paths[$index]}"

    magick "$preview16_path" \
        -resize '480x270^' \
        -gravity center \
        -extent 480x270 \
        -background "$bg" \
        -gravity north \
        -splice 0x34 \
        -font "$font" \
        -pointsize 21 \
        -fill white \
        -stroke none \
        -gravity north \
        -annotate +0+6 "$panel_id" \
        "$contact_tile_dir/${panel_id}.png"

    magick "$preview16_path" \
        -fill 'rgba(0,0,0,0.34)' \
        -stroke none \
        -draw 'rectangle 0,822 1920,1080' \
        -fill none \
        -stroke '#00e5ff' \
        -strokewidth 9 \
        -draw 'rectangle 192,54 1728,1026' \
        -stroke '#ffd54a' \
        -draw 'rectangle 1650,0 1920,150' \
        -resize '480x270^' \
        -gravity center \
        -extent 480x270 \
        -background "$bg" \
        -gravity north \
        -splice 0x34 \
        -font "$font" \
        -pointsize 21 \
        -fill white \
        -stroke none \
        -gravity north \
        -annotate +0+6 "$panel_id" \
        "$safe16_tile_dir/${panel_id}.png"

    magick "$preview20_path" \
        -fill 'rgba(0,0,0,0.34)' \
        -stroke none \
        -draw 'rectangle 240,822 2160,1080' \
        -fill none \
        -stroke '#00e5ff' \
        -strokewidth 9 \
        -draw 'rectangle 432,54 1968,1026' \
        -stroke '#ffd54a' \
        -draw 'rectangle 2070,0 2400,150' \
        -resize '600x270^' \
        -gravity center \
        -extent 600x270 \
        -background "$bg" \
        -gravity north \
        -splice 0x34 \
        -font "$font" \
        -pointsize 21 \
        -fill white \
        -stroke none \
        -gravity north \
        -annotate +0+6 "$panel_id" \
        "$safe20_tile_dir/${panel_id}.png"

    storyboard_tile="$work_dir/${panel_id}_storyboard.png"
    final_tile="$work_dir/${panel_id}_final.png"

    magick "$storyboard_path" \
        -resize '320x180^' -gravity center -extent 320x180 \
        -background "$bg" -gravity north -splice 0x40 \
        -font "$font" -pointsize 18 -fill white -stroke none \
        -gravity north -annotate +0+8 "${panel_id} STORYBOARD" \
        "$storyboard_tile"

    magick "$preview16_path" \
        -resize '320x180^' -gravity center -extent 320x180 \
        -background "$bg" -gravity north -splice 0x40 \
        -font "$font" -pointsize 18 -fill white -stroke none \
        -gravity north -annotate +0+8 "${panel_id} FINAL CANDIDATE" \
        "$final_tile"

    magick "$storyboard_tile" "$final_tile" +append \
        "$comparison_tile_dir/${panel_id}.png"
done

contact_canvas="$work_dir/contact_canvas.png"
safe16_canvas="$work_dir/safe16_canvas.png"
safe20_canvas="$work_dir/safe20_canvas.png"
comparison_canvas="$work_dir/comparison_canvas.png"
summary_canvas="$work_dir/summary_canvas.png"

magick -size 1984x1884 "xc:${bg}" "$contact_canvas"
magick -size 1984x1884 "xc:${bg}" "$safe16_canvas"
magick -size 2464x1884 "xc:${bg}" "$safe20_canvas"
magick -size 1312x2420 "xc:${bg}" "$comparison_canvas"
magick -size 1968x752 "xc:#d8d2ca" "$summary_canvas"

for ((index = 0; index < panel_count; index++)); do
    panel_id="${panel_ids[$index]}"
    row=$((index / 4))
    col=$((index % 4))
    x_contact=$((8 + col * 496))
    y_contact=$((row * 316))

    compose_tile "$contact_canvas" "$contact_tile_dir/${panel_id}.png" "$x_contact" "$y_contact" "$contact_canvas"
    compose_tile "$safe16_canvas" "$safe16_tile_dir/${panel_id}.png" "$x_contact" "$y_contact" "$safe16_canvas"

    x_safe20=$((8 + col * 616))
    compose_tile "$safe20_canvas" "$safe20_tile_dir/${panel_id}.png" "$x_safe20" "$y_contact" "$safe20_canvas"
done

for ((index = 0; index < panel_count; index++)); do
    panel_id="${panel_ids[$index]}"
    row=$((index / 2))
    col=$((index % 2))
    x_pair=$((8 + col * 648))
    y_pair=$((row * 220))

    compose_tile "$comparison_canvas" "$comparison_tile_dir/${panel_id}.png" "$x_pair" "$y_pair" "$comparison_canvas"
done

for ((index = 0; index < ${#reference_paths[@]}; index++)); do
    reference_path="${reference_paths[$index]}"
    tile_path="$work_dir/reference_$index.png"
    row=$((index / 3))
    col=$((index % 3))
    x_ref=$((col * 660))
    y_ref=$((row * 376))

    magick "$reference_path" \
        -resize '648x376^' \
        -gravity center \
        -extent 648x376 \
        "$tile_path"

    compose_tile "$summary_canvas" "$tile_path" "$x_ref" "$y_ref" "$summary_canvas"
done

mv -f -- "$contact_canvas" "$contact_output"
mv -f -- "$safe16_canvas" "$safe16_output"
mv -f -- "$safe20_canvas" "$safe20_output"
mv -f -- "$comparison_canvas" "$comparison_output"
mv -f -- "$summary_canvas" "$summary_output"

printf 'Rebuilt Gate 6 final-art evidence in %s\n' "$evidence_dir"
for output_path in \
    "$contact_output" \
    "$safe16_output" \
    "$safe20_output" \
    "$comparison_output" \
    "$summary_output"; do
    dimensions=$(magick identify -format '%wx%h' "$output_path")
    size_bytes=$(stat -f '%z' "$output_path" 2>/dev/null || stat -c '%s' "$output_path")
    printf '%s %s %s bytes\n' "$(basename "$output_path")" "$dimensions" "$size_bytes"
done

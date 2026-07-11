#!/usr/bin/env bash

set -euo pipefail

if [[ $# -ne 4 ]]; then
    echo "Usage: $0 <sheet-p01-p07> <sheet-p08-p16> <sheet-p17-p22> <output-dir>" >&2
    exit 64
fi

sheet_one="$1"
sheet_two="$2"
sheet_three="$3"
output_dir="$4"

for required_command in magick; do
    if ! command -v "$required_command" >/dev/null 2>&1; then
        echo "Missing required command: $required_command" >&2
        exit 69
    fi
done

for sheet in "$sheet_one" "$sheet_two" "$sheet_three"; do
    if [[ ! -f "$sheet" ]]; then
        echo "Missing storyboard sheet: $sheet" >&2
        exit 66
    fi

    is_wide=$(magick identify -format '%[fx:abs(w/h-16/9)<0.02]' "$sheet")
    if [[ "$is_wide" != "1" ]]; then
        echo "Storyboard sheet is not approximately 16:9: $sheet" >&2
        exit 65
    fi
done

font="/System/Library/Fonts/Helvetica.ttc"
if [[ ! -f "$font" ]]; then
    echo "Missing contact-sheet font: $font" >&2
    exit 66
fi

frames_dir="$output_dir/frames"
tmp_dir="$output_dir/.contact-sheet-tmp"
mkdir -p "$frames_dir" "$tmp_dir"

extract_sheet() {
    local sheet="$1"
    local start_panel="$2"
    local panel_count="$3"
    local prefix="$4"

    magick "$sheet" -crop 3x3@ +repage "$tmp_dir/${prefix}_cell_%02d.png"

    local cell_index
    for ((cell_index = 0; cell_index < panel_count; cell_index++)); do
        local panel_number=$((start_panel + cell_index))
        local panel_id
        panel_id=$(printf 'FL-P%02d' "$panel_number")
        local source_cell
        source_cell=$(printf '%s/%s_cell_%02d.png' "$tmp_dir" "$prefix" "$cell_index")

        magick "$source_cell" \
            -shave 6x6 \
            -resize '640x360^' \
            -gravity center \
            -extent 640x360 \
            "$frames_dir/$panel_id.png"
    done
}

extract_sheet "$sheet_one" 1 7 "sheet01"
extract_sheet "$sheet_two" 8 9 "sheet02"
extract_sheet "$sheet_three" 17 6 "sheet03"

contact_tiles=()
safe_16_tiles=()
safe_20_tiles=()

for panel_number in $(seq 1 22); do
    panel_id=$(printf 'FL-P%02d' "$panel_number")
    frame="$frames_dir/$panel_id.png"
    contact_tile="$tmp_dir/${panel_id}_contact.png"
    safe_16_tile="$tmp_dir/${panel_id}_safe16.png"
    safe_20_tile="$tmp_dir/${panel_id}_safe20.png"

    magick "$frame" \
        -resize 480x270 \
        -background '#202428' \
        -gravity south \
        -splice 0x34 \
        -font "$font" \
        -pointsize 21 \
        -fill white \
        -gravity south \
        -annotate +0+6 "$panel_id" \
        "$contact_tile"

    magick "$frame" \
        -fill 'rgba(0,0,0,0.34)' \
        -stroke none \
        -draw 'rectangle 0,274 640,360' \
        -fill none \
        -stroke '#00e5ff' \
        -strokewidth 3 \
        -draw 'rectangle 64,18 576,342' \
        -stroke '#ffd54a' \
        -draw 'rectangle 550,0 640,50' \
        -resize 480x270 \
        -background '#202428' \
        -gravity south \
        -splice 0x34 \
        -font "$font" \
        -pointsize 21 \
        -fill white \
        -stroke none \
        -gravity south \
        -annotate +0+6 "$panel_id" \
        "$safe_16_tile"

    magick "$frame" \
        -background '#111820' \
        -gravity center \
        -extent 800x360 \
        -fill 'rgba(0,0,0,0.34)' \
        -stroke none \
        -draw 'rectangle 80,274 720,360' \
        -fill none \
        -stroke '#00e5ff' \
        -strokewidth 3 \
        -draw 'rectangle 144,18 656,342' \
        -stroke '#ffd54a' \
        -draw 'rectangle 690,0 800,50' \
        -resize 600x270 \
        -background '#202428' \
        -gravity south \
        -splice 0x34 \
        -font "$font" \
        -pointsize 21 \
        -fill white \
        -stroke none \
        -gravity south \
        -annotate +0+6 "$panel_id" \
        "$safe_20_tile"

    contact_tiles+=("$contact_tile")
    safe_16_tiles+=("$safe_16_tile")
    safe_20_tiles+=("$safe_20_tile")
done

magick montage "${contact_tiles[@]}" \
    -font "$font" \
    -background '#202428' \
    -tile 6x4 \
    -geometry +10+10 \
    "$output_dir/first_launch_contact_sheet.png"

magick montage "${safe_16_tiles[@]}" \
    -font "$font" \
    -background '#202428' \
    -tile 6x4 \
    -geometry +10+10 \
    "$output_dir/first_launch_safe_area_contact_sheet_16x9.png"

magick montage "${safe_20_tiles[@]}" \
    -font "$font" \
    -background '#202428' \
    -tile 4x6 \
    -geometry +10+10 \
    "$output_dir/first_launch_safe_area_contact_sheet_20x9.png"

sheet_one_size=$(magick identify -format '%wx%h' "$sheet_one")
sheet_two_size=$(magick identify -format '%wx%h' "$sheet_two")
sheet_three_size=$(magick identify -format '%wx%h' "$sheet_three")
contact_size=$(magick identify -format '%wx%h' "$output_dir/first_launch_contact_sheet.png")

printf '{\n' > "$output_dir/first_launch_contact_sheet_validation.json"
printf '  "schemaVersion": 1,\n' >> "$output_dir/first_launch_contact_sheet_validation.json"
printf '  "panelCount": 22,\n' >> "$output_dir/first_launch_contact_sheet_validation.json"
printf '  "panelIdRange": "FL-P01..FL-P22",\n' >> "$output_dir/first_launch_contact_sheet_validation.json"
printf '  "sourceSheetSizes": ["%s", "%s", "%s"],\n' "$sheet_one_size" "$sheet_two_size" "$sheet_three_size" >> "$output_dir/first_launch_contact_sheet_validation.json"
printf '  "normalizedFrameSize": "640x360",\n' >> "$output_dir/first_launch_contact_sheet_validation.json"
printf '  "contactSheetSize": "%s",\n' "$contact_size" >> "$output_dir/first_launch_contact_sheet_validation.json"
printf '  "safeAreaLegend": {"cyan": "centered story-safe area", "yellow": "skip control reserve", "dark": "subtitle reserve"}\n' >> "$output_dir/first_launch_contact_sheet_validation.json"
printf '}\n' >> "$output_dir/first_launch_contact_sheet_validation.json"

rm -rf "$tmp_dir"

echo "Created 22 normalized frames and contact-sheet evidence in $output_dir"

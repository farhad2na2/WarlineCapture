#!/usr/bin/env bash

set -euo pipefail

if [[ $# -ne 4 ]]; then
    echo "Usage: $0 <source-sheet> <zero-based-cell-index> <replacement-image> <output-sheet>" >&2
    exit 64
fi

source_sheet="$1"
cell_index="$2"
replacement_image="$3"
output_sheet="$4"

if [[ ! -f "$source_sheet" ]]; then
    echo "Missing source sheet: $source_sheet" >&2
    exit 66
fi

if [[ ! -f "$replacement_image" ]]; then
    echo "Missing replacement image: $replacement_image" >&2
    exit 66
fi

if [[ ! "$cell_index" =~ ^[0-8]$ ]]; then
    echo "Cell index must be an integer from 0 through 8." >&2
    exit 64
fi

source_size=$(magick identify -format '%wx%h' "$source_sheet")
if [[ "$source_size" != "1672x941" ]]; then
    echo "Unsupported source sheet size: $source_size; expected 1672x941." >&2
    exit 65
fi

x_offsets=(8 562 1117 8 562 1117 8 562 1117)
y_offsets=(8 8 8 319 319 319 631 631 631)
cell_width=546
cell_height=303
x_offset="${x_offsets[$cell_index]}"
y_offset="${y_offsets[$cell_index]}"

output_parent=$(dirname "$output_sheet")
mkdir -p "$output_parent"

tmp_dir=$(mktemp -d "${TMPDIR:-/tmp}/storyboard-cell.XXXXXX")
trap 'rm -rf "$tmp_dir"' EXIT

normalized_replacement="$tmp_dir/replacement.png"
composited_output="$tmp_dir/output.png"

magick "$replacement_image" \
    -resize "${cell_width}x${cell_height}^" \
    -gravity center \
    -extent "${cell_width}x${cell_height}" \
    "$normalized_replacement"

magick "$source_sheet" "$normalized_replacement" \
    -geometry "+${x_offset}+${y_offset}" \
    -composite \
    "$composited_output"

result_size=$(magick identify -format '%wx%h' "$composited_output")
if [[ "$result_size" != "$source_size" ]]; then
    echo "Replacement changed sheet dimensions: $result_size" >&2
    exit 65
fi

mv "$composited_output" "$output_sheet"

echo "Replaced cell $cell_index at +${x_offset}+${y_offset} in $output_sheet"

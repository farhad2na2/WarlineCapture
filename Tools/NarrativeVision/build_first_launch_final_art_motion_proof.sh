#!/usr/bin/env bash

set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/../.." && pwd)
final_art_root="$repo_root/Design/NarrativeVision/FirstLaunch/ArtReview/FinalArt"
source_dir="$final_art_root/SourceMasters"
evidence_dir="$final_art_root/Evidence"
output="$evidence_dir/FINAL_ART_MOTION_PROOF.mp4"

width=1280
height=720
frame_rate=30
panel_seconds=2
panel_count=22
frames_per_panel=$((frame_rate * panel_seconds))
expected_frames=$((panel_count * frames_per_panel))
expected_duration=$((panel_count * panel_seconds))
font=/System/Library/Fonts/Helvetica.ttc

for command_name in ffmpeg ffprobe magick; do
    if ! command -v "$command_name" >/dev/null 2>&1; then
        echo "Missing required command: $command_name" >&2
        exit 69
    fi
done

if [[ ! -d "$source_dir" ]]; then
    echo "Missing SourceMasters directory: $source_dir" >&2
    exit 66
fi

if [[ ! -f "$font" ]]; then
    echo "Missing deterministic overlay font: $font" >&2
    exit 66
fi

source_paths=()
for ((panel_number = 1; panel_number <= panel_count; panel_number++)); do
    panel_id=$(printf 'FL-P%02d' "$panel_number")
    matches=()
    while IFS= read -r source_path; do
        matches+=("$source_path")
    done < <(find "$source_dir" -maxdepth 1 -type f -name "${panel_id}_*Candidate_R*.png" -print | LC_ALL=C sort)

    if [[ ${#matches[@]} -ne 1 ]]; then
        echo "Expected exactly one source master for $panel_id; found ${#matches[@]}." >&2
        exit 65
    fi

    expected_path="${matches[0]}"
    source_paths+=("$expected_path")

    dimensions=$(ffprobe -v error -select_streams v:0 \
        -show_entries stream=width,height -of csv=p=0 "$expected_path")
    if [[ "$dimensions" != "1672,941" ]]; then
        echo "Unexpected dimensions for $(basename "$expected_path"): $dimensions (expected 1672,941)." >&2
        exit 65
    fi
done

mkdir -p "$evidence_dir"
work_dir=$(mktemp -d "${TMPDIR:-/tmp}/first-launch-motion-proof.XXXXXX")
trap 'rm -rf -- "$work_dir"' EXIT
temporary_output="$work_dir/FINAL_ART_MOTION_PROOF.mp4"

ffmpeg_args=(-nostdin -hide_banner -loglevel error -y)
filter_parts=()
concat_inputs=

for ((index = 0; index < panel_count; index++)); do
    panel_number=$((index + 1))
    panel_id=$(printf 'FL-P%02d' "$panel_number")
    source_path="${source_paths[$index]}"
    label_path="$work_dir/${panel_id}.png"

    magick -quiet -size 132x38 "xc:#111820CC" \
        -font "$font" -fill '#FFFFFF' -pointsize 21 -gravity center \
        -annotate +0+0 "$panel_id" -bordercolor '#FFFFFF66' -border 1 \
        -strip "$label_path"

    source_input=$((index * 2))
    label_input=$((source_input + 1))
    ffmpeg_args+=(
        -loop 1 -framerate "$frame_rate" -i "$source_path"
        -loop 1 -framerate "$frame_rate" -i "$label_path"
    )

    if ((panel_number % 2 == 1)); then
        zoom="min(1.035,1+0.035*on/$((frames_per_panel - 1)))"
        x_position="iw/2-(iw/zoom/2)"
    elif ((panel_number % 4 == 2)); then
        zoom=1.025
        x_position="(iw-iw/zoom)*on/$((frames_per_panel - 1))"
    else
        zoom=1.025
        x_position="(iw-iw/zoom)*(1-on/$((frames_per_panel - 1)))"
    fi

    filter_parts+=(
        "[${source_input}:v]scale=${width}:${height}:force_original_aspect_ratio=increase:flags=lanczos,crop=${width}:${height},setsar=1,zoompan=z='${zoom}':x='${x_position}':y='ih/2-(ih/zoom/2)':d=1:s=${width}x${height}:fps=${frame_rate},trim=end_frame=${frames_per_panel},setpts=PTS-STARTPTS[base${index}]"
        "[${label_input}:v]format=rgba,trim=end_frame=${frames_per_panel},setpts=PTS-STARTPTS[id${index}]"
        "[base${index}][id${index}]overlay=24:24:format=auto:shortest=1,format=yuv420p[v${index}]"
    )
    concat_inputs+="[v${index}]"
done

filter_parts+=("${concat_inputs}concat=n=${panel_count}:v=1:a=0[outv]")
filter_complex=$(IFS=';'; printf '%s' "${filter_parts[*]}")

ffmpeg "${ffmpeg_args[@]}" \
    -filter_complex "$filter_complex" -map '[outv]' \
    -frames:v "$expected_frames" -r "$frame_rate" -fps_mode cfr -an \
    -c:v libx264 -preset medium -crf 20 -pix_fmt yuv420p \
    -profile:v high -level:v 3.1 -g "$frames_per_panel" -keyint_min "$frames_per_panel" \
    -sc_threshold 0 -bf 2 -threads 1 -fflags +bitexact -flags:v +bitexact \
    -map_metadata -1 -metadata creation_time=1970-01-01T00:00:00Z \
    -movflags +faststart "$temporary_output"

video_streams=$(ffprobe -v error -select_streams v \
    -show_entries stream=index -of csv=p=0 "$temporary_output" | wc -l | tr -d ' ')
audio_streams=$(ffprobe -v error -select_streams a \
    -show_entries stream=index -of csv=p=0 "$temporary_output" | wc -l | tr -d ' ')
codec=$(ffprobe -v error -select_streams v:0 \
    -show_entries stream=codec_name -of default=nw=1:nk=1 "$temporary_output")
dimensions=$(ffprobe -v error -select_streams v:0 \
    -show_entries stream=width,height -of csv=p=0 "$temporary_output")
average_rate=$(ffprobe -v error -select_streams v:0 \
    -show_entries stream=avg_frame_rate -of default=nw=1:nk=1 "$temporary_output")
real_rate=$(ffprobe -v error -select_streams v:0 \
    -show_entries stream=r_frame_rate -of default=nw=1:nk=1 "$temporary_output")
read_frames=$(ffprobe -v error -count_frames -select_streams v:0 \
    -show_entries stream=nb_read_frames -of default=nw=1:nk=1 "$temporary_output")
duration=$(ffprobe -v error -show_entries format=duration \
    -of default=nw=1:nk=1 "$temporary_output")

if [[ "$video_streams" != 1 || "$audio_streams" != 0 ]]; then
    echo "Invalid stream layout: video=$video_streams audio=$audio_streams (expected 1 video, 0 audio)." >&2
    exit 65
fi
if [[ "$codec" != h264 ]]; then
    echo "Invalid video codec: $codec (expected h264)." >&2
    exit 65
fi
if [[ "$dimensions" != "${width},${height}" ]]; then
    echo "Invalid output dimensions: $dimensions (expected ${width},${height})." >&2
    exit 65
fi
if [[ "$average_rate" != "${frame_rate}/1" || "$real_rate" != "${frame_rate}/1" ]]; then
    echo "Invalid frame rate: avg=$average_rate real=$real_rate (expected ${frame_rate}/1)." >&2
    exit 65
fi
if [[ "$read_frames" != "$expected_frames" ]]; then
    echo "Invalid frame count: $read_frames (expected $expected_frames for all $panel_count panels)." >&2
    exit 65
fi
if ! awk -v actual="$duration" -v expected="$expected_duration" \
    'BEGIN { exit !(actual > 0 && actual >= expected - 0.001 && actual <= expected + 0.001) }'; then
    echo "Invalid duration: $duration (expected ${expected_duration}s and nonzero)." >&2
    exit 65
fi

mv -f -- "$temporary_output" "$output"

size_bytes=$(stat -f '%z' "$output" 2>/dev/null || stat -c '%s' "$output")
echo "Final-art motion proof passed validation."
echo "Panels: $panel_count/22 in FL-P01..FL-P22 order"
echo "Media: h264, ${width}x${height}, ${frame_rate} fps, ${duration}s, ${read_frames} frames, no audio"
echo "Size: ${size_bytes} bytes"
echo "Output: $output"

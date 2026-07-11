#!/usr/bin/env bash

set -euo pipefail

repo_root=$(git rev-parse --show-toplevel)
animatic_dir="$repo_root/Design/NarrativeVision/FirstLaunch/animatic"
voice_script="${VOICE_SCRIPT:-$animatic_dir/first_launch_temp_voice.tsv}"
voice_dir="${VOICE_DIR:-$animatic_dir/audio/temp_voice}"
report="${VOICE_REPORT:-$animatic_dir/audio/first_launch_temp_voice_generation.tsv}"
expected_clip_count="${EXPECTED_CLIP_COUNT:-23}"
edge_tts_path="${EDGE_TTS_PATH:-/private/tmp/warline-edge-tts}"

for required_command in ffmpeg ffprobe python3; do
    if ! command -v "$required_command" >/dev/null 2>&1; then
        echo "Missing required command: $required_command" >&2
        exit 69
    fi
done

if [[ ! -f "$voice_script" ]]; then
    echo "Missing temporary voice script: $voice_script" >&2
    exit 66
fi

mkdir -p "$voice_dir"
work_dir=$(mktemp -d "${TMPDIR:-/tmp}/first-launch-voice.XXXXXX")
trap 'rm -rf "$work_dir"' EXIT
printf 'clip_id\tstart_seconds\tdeadline_seconds\tduration_seconds\tend_seconds\tspeaker\tbackend\tvoice\trate\tvolume\tpitch\n' > "$report"

clip_count=0
timing_failures=0
while IFS=$'\t' read -r clip_id start_seconds deadline_seconds speaker backend voice rate volume pitch text; do
    if [[ "$clip_id" == "clip_id" || -z "$clip_id" ]]; then
        continue
    fi

    output="$voice_dir/$clip_id.wav"
    case "$backend" in
        edge)
            if [[ ! -d "$edge_tts_path" ]]; then
                echo "Missing edge-tts package path: $edge_tts_path" >&2
                echo "Install it with: python3 -m pip install --target $edge_tts_path edge-tts" >&2
                exit 69
            fi
            mp3="$work_dir/$clip_id.mp3"
            PYTHONPATH="$edge_tts_path" python3 -m edge_tts \
                --voice "$voice" \
                --rate="$rate" \
                --volume="$volume" \
                --pitch="$pitch" \
                --text "$text" \
                --write-media "$mp3"
            ffmpeg -nostdin -hide_banner -loglevel error -y \
                -i "$mp3" -ac 1 -ar 44100 "$output"
            ;;
        macos)
            aiff="$work_dir/$clip_id.aiff"
            say -v "$voice" -r "$rate" -o "$aiff" "$text"
            ffmpeg -nostdin -hide_banner -loglevel error -y \
                -i "$aiff" -ac 1 -ar 44100 "$output"
            ;;
        *)
            echo "Unsupported temporary voice backend '$backend' for $clip_id" >&2
            exit 64
            ;;
    esac

    duration=$(ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "$output")
    if ! awk -v value="$duration" 'BEGIN { exit !(value > 0.05) }'; then
        echo "Speech generation produced an empty clip: $clip_id" >&2
        exit 65
    fi

    end_seconds=$(awk -v start="$start_seconds" -v value="$duration" 'BEGIN { printf "%.6f", start + value }')
    if ! awk -v end="$end_seconds" -v deadline="$deadline_seconds" 'BEGIN { exit !(end <= deadline + 0.001) }'; then
        echo "Timing-read clip crosses its deadline: $clip_id ends at $end_seconds, deadline $deadline_seconds" >&2
        timing_failures=$((timing_failures + 1))
    fi

    printf '%s\t%s\t%s\t%.6f\t%s\t%s\t%s\t%s\t%s\t%s\t%s\n' \
        "$clip_id" "$start_seconds" "$deadline_seconds" "$duration" "$end_seconds" \
        "$speaker" "$backend" "$voice" "$rate" "$volume" "$pitch" >> "$report"
    clip_count=$((clip_count + 1))
done < "$voice_script"

if [[ "$clip_count" -ne "$expected_clip_count" ]]; then
    echo "Expected $expected_clip_count timing-read clips, generated $clip_count." >&2
    exit 65
fi

if [[ "$timing_failures" -ne 0 ]]; then
    echo "$timing_failures timing-read clips crossed their deadlines; see $report." >&2
    exit 65
fi

speaker_count=$(awk -F '\t' 'NR > 1 { print $6 }' "$report" | sort -u | wc -l | tr -d ' ')
speaker_voice_count=$(awk -F '\t' 'NR > 1 { print $6 "\t" $8 }' "$report" | sort -u | wc -l | tr -d ' ')
unique_voice_count=$(awk -F '\t' 'NR > 1 { print $8 }' "$report" | sort -u | wc -l | tr -d ' ')
if [[ "$speaker_count" -ne 5 || "$speaker_voice_count" -ne 5 || "$unique_voice_count" -ne 5 ]]; then
    echo "Expected five speakers with five stable, unique voices; got speakers=$speaker_count mappings=$speaker_voice_count voices=$unique_voice_count." >&2
    exit 65
fi

echo "Generated and deadline-validated $clip_count timing reads across five unique Microsoft neural voices."

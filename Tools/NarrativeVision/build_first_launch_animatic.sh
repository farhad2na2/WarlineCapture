#!/usr/bin/env bash

set -euo pipefail

repo_root=$(git rev-parse --show-toplevel)
slice_root="$repo_root/Design/NarrativeVision/FirstLaunch"
animatic_dir="$slice_root/animatic"
timeline="$animatic_dir/first_launch_animatic_timeline.json"
subtitles="$animatic_dir/first_launch_temp_subtitles.srt"
voice_script="$animatic_dir/first_launch_temp_voice.tsv"
frames_root="$slice_root"
voice_dir="$animatic_dir/audio/temp_voice"
work_dir="$animatic_dir/.build"
normal_segments="$work_dir/normal_segments"
static_segments="$work_dir/static_segments"
subtitle_segments="$work_dir/subtitle_segments"
font="/System/Library/Fonts/Helvetica.ttc"

for required_command in ffmpeg ffprobe jq magick; do
    if ! command -v "$required_command" >/dev/null 2>&1; then
        echo "Missing required command: $required_command" >&2
        exit 69
    fi
done

for required_file in "$timeline" "$subtitles" "$voice_script" "$font"; do
    if [[ ! -f "$required_file" ]]; then
        echo "Missing animatic input: $required_file" >&2
        exit 66
    fi
done

mkdir -p "$voice_dir" "$normal_segments" "$static_segments" "$subtitle_segments" "$animatic_dir/auxiliary"

total_duration=$(jq -r '.totalDurationSeconds' "$timeline")
frame_rate=$(jq -r '.frameRate' "$timeline")
resolution=$(jq -r '.resolution' "$timeline")
width=${resolution%x*}
height=${resolution#*x}

validate_timeline() {
    if ! jq -e '
        (.states | length) == 25 and
        .states[0].startSeconds == 0 and
        .gameplayHandoffSeconds <= 90 and
        (.totalDurationSeconds == (.states[-1].startSeconds + .states[-1].durationSeconds)) and
        ([range(0; (.states | length) - 1) as $i |
            (.states[$i].startSeconds + .states[$i].durationSeconds) == .states[$i + 1].startSeconds
        ] | all)
    ' "$timeline" >/dev/null; then
        echo "Animatic timeline is discontinuous or violates the Gate 5 handoff contract." >&2
        exit 65
    fi
}

write_timing_report() {
    printf 'order\tstate_id\tstart_seconds\tduration_seconds\tend_seconds\tmotion\tskip_destination\n' \
        > "$animatic_dir/first_launch_timing_report.tsv"
    jq -r '.states[] |
        [.order, .id, .startSeconds, .durationSeconds, (.startSeconds + .durationSeconds), .motion, .skipDestination] |
        @tsv' "$timeline" >> "$animatic_dir/first_launch_timing_report.tsv"
}

make_auxiliary_frames() {
    magick -size "${width}x${height}" xc:'#111820' \
        -font "$font" -fill white -pointsize 58 -gravity center \
        -annotate +0-18 'WARLINE CAPTURE' \
        -fill '#7d8a91' -pointsize 24 -annotate +0+52 'FIRST RESPONSE' \
        "$animatic_dir/auxiliary/first_launch_logo.png"

    magick "$slice_root/storyboard/frames/FL-P08.png" \
        -resize "${width}x${height}^" -gravity center -extent "${width}x${height}" \
        -fill 'rgba(8,13,17,0.74)' -stroke '#4b5b63' -strokewidth 2 \
        -draw 'roundrectangle 350,150 930,570 8,8' \
        -font "$font" -fill white -stroke none -gravity north -pointsize 34 \
        -annotate +0+205 'COMMANDER IDENTITY' \
        -fill '#9dadb4' -pointsize 22 -annotate +0+275 'DEFAULT COMMANDER' \
        -fill '#65d8e8' -pointsize 24 -annotate +0+355 'CONTINUE' \
        "$animatic_dir/auxiliary/first_launch_identity_hold.png"

    magick "$slice_root/storyboard/frames/FL-P09.png" \
        -resize "${width}x${height}^" -gravity center -extent "${width}x${height}" \
        -fill 'rgba(8,13,17,0.78)' -stroke '#4b5b63' -strokewidth 2 \
        -draw 'roundrectangle 350,145 930,575 8,8' \
        -font "$font" -fill white -stroke none -gravity north -pointsize 34 \
        -annotate +0+195 'GUIDANCE' \
        -fill '#65d8e8' -pointsize 24 -annotate +0+270 'FULL GUIDANCE' \
        -fill '#9dadb4' -pointsize 22 -annotate +0+325 'TACTICAL HINTS' \
        -annotate +0+375 'VETERAN' \
        "$animatic_dir/auxiliary/first_launch_guidance_hold.png"

    magick -size "${width}x${height}" xc:'#111820' \
        -font "$font" -fill white -pointsize 40 -gravity center \
        -annotate +0-25 'M01 GAMEPLAY HANDOFF' \
        -fill '#9dadb4' -pointsize 22 -annotate +0+35 'REVIEW PLACEHOLDER' \
        "$animatic_dir/auxiliary/first_launch_gameplay_placeholder.png"
}

validate_voice_clips() {
    while IFS=$'\t' read -r clip_id start_seconds deadline_seconds speaker backend voice rate volume pitch text; do
        if [[ "$clip_id" == "clip_id" || -z "$clip_id" ]]; then
            continue
        fi

        clip="$voice_dir/$clip_id.wav"
        if [[ ! -f "$clip" ]]; then
            echo "Missing temporary voice clip: $clip" >&2
            echo "Run Tools/NarrativeVision/generate_first_launch_temp_voice.sh with Microsoft Edge TTS and macOS speech-service access." >&2
            exit 66
        fi

        duration=$(ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "$clip")
        if ! awk -v value="$duration" 'BEGIN { exit !(value > 0.05) }'; then
            echo "Temporary voice clip is empty: $clip" >&2
            echo "Run Tools/NarrativeVision/generate_first_launch_temp_voice.sh with Microsoft Edge TTS and macOS speech-service access." >&2
            exit 65
        fi
    done < "$voice_script"
}

mix_temporary_audio() {
    local ffmpeg_inputs=(-f lavfi -t "$total_duration" -i "anullsrc=r=48000:cl=stereo")
    local filter_parts=("[0:a]atrim=duration=${total_duration}[base]")
    local mix_labels="[base]"
    local input_index=1

    while IFS=$'\t' read -r clip_id start_seconds deadline_seconds speaker backend voice rate volume pitch text; do
        if [[ "$clip_id" == "clip_id" || -z "$clip_id" ]]; then
            continue
        fi

        ffmpeg_inputs+=(-i "$voice_dir/$clip_id.wav")
        delay_ms=$(awk -v seconds="$start_seconds" 'BEGIN { printf "%d", seconds * 1000 }')
        filter_parts+=("[${input_index}:a]aresample=48000,adelay=${delay_ms}|${delay_ms},volume=0.88[v${input_index}]")
        mix_labels+="[v${input_index}]"
        input_index=$((input_index + 1))
    done < "$voice_script"

    local joined_filters
    joined_filters=$(IFS=';'; echo "${filter_parts[*]}")
    local mix_count=$input_index
    local filter_complex="${joined_filters};${mix_labels}amix=inputs=${mix_count}:normalize=0:dropout_transition=0,alimiter=limit=0.92[voice]"

    ffmpeg -nostdin -hide_banner -loglevel error -y \
        "${ffmpeg_inputs[@]}" \
        -filter_complex "$filter_complex" \
        -map '[voice]' -t "$total_duration" -ar 48000 -ac 2 \
        "$animatic_dir/audio/first_launch_temp_voice_mix.wav"

    ffmpeg -nostdin -hide_banner -loglevel error -y \
        -f lavfi -i "anoisesrc=color=pink:duration=${total_duration}:amplitude=0.006:sample_rate=48000" \
        -f lavfi -i "sine=frequency=55:duration=0.45:sample_rate=48000" \
        -f lavfi -i "sine=frequency=48:duration=0.55:sample_rate=48000" \
        -f lavfi -i "anoisesrc=color=white:duration=4:amplitude=0.018:sample_rate=48000" \
        -f lavfi -i "sine=frequency=720:duration=0.55:sample_rate=48000" \
        -f lavfi -i "sine=frequency=920:duration=0.28:sample_rate=48000" \
        -f lavfi -i "sine=frequency=440:duration=0.45:sample_rate=48000" \
        -filter_complex \
        "[0:a]volume=0.65[amb];\
         [1:a]adelay=7800|7800,volume=0.35[blast1];\
         [2:a]adelay=9300|9300,volume=0.32[blast2];\
         [3:a]highpass=f=900,adelay=12000|12000,volume=0.35[radio];\
         [4:a]adelay=22000|22000,volume=0.18[boot];\
         [5:a]adelay=42000|42000,volume=0.16[auth];\
         [6:a]adelay=84500|84500,volume=0.14[handoff];\
         [amb][blast1][blast2][radio][boot][auth][handoff]amix=inputs=7:normalize=0:dropout_transition=0,alimiter=limit=0.75[sfx]" \
        -map '[sfx]' -t "$total_duration" -ar 48000 -ac 2 \
        "$animatic_dir/audio/first_launch_temp_sfx_mix.wav"

    ffmpeg -nostdin -hide_banner -loglevel error -y \
        -i "$animatic_dir/audio/first_launch_temp_voice_mix.wav" \
        -i "$animatic_dir/audio/first_launch_temp_sfx_mix.wav" \
        -filter_complex '[0:a][1:a]amix=inputs=2:normalize=0:dropout_transition=0,alimiter=limit=0.95[a]' \
        -map '[a]' -t "$total_duration" -ar 48000 -ac 2 \
        "$animatic_dir/audio/first_launch_temp_mix.wav"
}

render_segments() {
    : > "$work_dir/normal_concat.txt"
    : > "$work_dir/static_concat.txt"

    while IFS=$'\t' read -r order state_id relative_frame start_seconds duration_seconds motion; do
        frame="$frames_root/$relative_frame"
        if [[ ! -f "$frame" ]]; then
            echo "Missing timeline frame: $frame" >&2
            exit 66
        fi

        segment_name=$(printf '%02d_%s.mp4' "$order" "${state_id//./_}")
        normal_segment="$normal_segments/$segment_name"
        static_segment="$static_segments/$segment_name"
        frame_count=$(awk -v duration="$duration_seconds" -v fps="$frame_rate" 'BEGIN { printf "%d", duration * fps }')
        motion_increment=$(awk -v frames="$frame_count" 'BEGIN { printf "%.9f", 0.03 / frames }')

        if [[ "$motion" == Static* ]]; then
            normal_filter="scale=${width}:${height}:force_original_aspect_ratio=increase,crop=${width}:${height},format=yuv420p"
        elif [[ "$motion" == "DriftRight" ]]; then
            normal_filter="scale=${width}:${height}:force_original_aspect_ratio=increase,crop=${width}:${height},zoompan=z='1.03':x='(iw-iw/zoom)*(on/${frame_count})':y='ih/2-(ih/zoom/2)':d=${frame_count}:s=${width}x${height}:fps=${frame_rate},format=yuv420p"
        elif [[ "$motion" == "DriftLeft" ]]; then
            normal_filter="scale=${width}:${height}:force_original_aspect_ratio=increase,crop=${width}:${height},zoompan=z='1.03':x='(iw-iw/zoom)*(1-on/${frame_count})':y='ih/2-(ih/zoom/2)':d=${frame_count}:s=${width}x${height}:fps=${frame_rate},format=yuv420p"
        elif [[ "$motion" == "PullBack" ]]; then
            normal_filter="scale=${width}:${height}:force_original_aspect_ratio=increase,crop=${width}:${height},zoompan=z='max(1.0,1.03-on*${motion_increment})':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d=${frame_count}:s=${width}x${height}:fps=${frame_rate},format=yuv420p"
        else
            normal_filter="scale=${width}:${height}:force_original_aspect_ratio=increase,crop=${width}:${height},zoompan=z='min(1.03,1.0+on*${motion_increment})':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d=${frame_count}:s=${width}x${height}:fps=${frame_rate},format=yuv420p"
        fi

        ffmpeg -nostdin -hide_banner -loglevel error -y \
            -loop 1 -framerate "$frame_rate" -i "$frame" \
            -frames:v "$frame_count" -vf "$normal_filter" \
            -r "$frame_rate" -an -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p \
            "$normal_segment"

        ffmpeg -nostdin -hide_banner -loglevel error -y \
            -loop 1 -framerate "$frame_rate" -i "$frame" \
            -frames:v "$frame_count" \
            -vf "scale=${width}:${height}:force_original_aspect_ratio=increase,crop=${width}:${height},format=yuv420p" \
            -r "$frame_rate" -an -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p \
            "$static_segment"

        printf "file '%s'\n" "$normal_segment" >> "$work_dir/normal_concat.txt"
        printf "file '%s'\n" "$static_segment" >> "$work_dir/static_concat.txt"
    done < <(jq -r '.states[] | [.order, .id, .frame, .startSeconds, .durationSeconds, .motion] | @tsv' "$timeline")
}

timestamp_to_seconds() {
    local timestamp="$1"
    awk -F '[:,]' -v value="$timestamp" 'BEGIN {
        split(value, parts, /[:,]/)
        printf "%.3f", (parts[1] * 3600) + (parts[2] * 60) + parts[3] + (parts[4] / 1000)
    }'
}

render_subtitle_segment() {
    local frame="$1"
    local duration="$2"
    local output="$3"

    ffmpeg -nostdin -hide_banner -loglevel error -y \
        -loop 1 -framerate "$frame_rate" -i "$frame" \
        -t "$duration" -vf "scale=${width}:${height},format=rgb24" \
        -r "$frame_rate" -an -c:v ffv1 -pix_fmt rgb24 \
        "$output"
}

render_subtitle_overlay() {
    local cues_tsv="$work_dir/subtitle_cues.tsv"
    local concat_file="$work_dir/subtitle_concat.txt"
    local blank_frame="$work_dir/subtitle_blank.png"
    local cursor="0.000"
    local segment_index=0

    awk 'BEGIN { RS=""; FS="\n"; OFS="\t" }
        NF >= 3 {
            split($2, timing, " --> ")
            text=$3
            for (i=4; i<=NF; i++) text=text " " $i
            print timing[1], timing[2], text
        }' "$subtitles" > "$cues_tsv"

    magick -size "${width}x${height}" xc:black "$blank_frame"
    : > "$concat_file"

    while IFS=$'\t' read -r start_timestamp end_timestamp subtitle_text; do
        start_seconds=$(timestamp_to_seconds "$start_timestamp")
        end_seconds=$(timestamp_to_seconds "$end_timestamp")
        gap_duration=$(awk -v start="$start_seconds" -v cursor="$cursor" 'BEGIN { printf "%.3f", start - cursor }')

        if awk -v duration="$gap_duration" 'BEGIN { exit !(duration > 0.0005) }'; then
            gap_segment=$(printf '%s/%03d_gap.mkv' "$subtitle_segments" "$segment_index")
            render_subtitle_segment "$blank_frame" "$gap_duration" "$gap_segment"
            printf "file '%s'\n" "$gap_segment" >> "$concat_file"
            segment_index=$((segment_index + 1))
        fi

        subtitle_frame=$(printf '%s/subtitle_%03d.png' "$work_dir" "$segment_index")
        subtitle_segment=$(printf '%s/%03d_text.mkv' "$subtitle_segments" "$segment_index")
        subtitle_duration=$(awk -v end="$end_seconds" -v start="$start_seconds" 'BEGIN { printf "%.3f", end - start }')

        magick -size "${width}x${height}" xc:black \
            -fill '#263238' -stroke '#4b5b63' -strokewidth 1 \
            -draw 'roundrectangle 70,596 1210,690 5,5' \
            -font "$font" -fill white -stroke none -pointsize 27 -gravity south \
            -annotate +0+34 "$subtitle_text" \
            "$subtitle_frame"

        render_subtitle_segment "$subtitle_frame" "$subtitle_duration" "$subtitle_segment"
        printf "file '%s'\n" "$subtitle_segment" >> "$concat_file"
        segment_index=$((segment_index + 1))
        cursor="$end_seconds"
    done < "$cues_tsv"

    tail_duration=$(awk -v total="$total_duration" -v cursor="$cursor" 'BEGIN { printf "%.3f", total - cursor }')
    if awk -v duration="$tail_duration" 'BEGIN { exit !(duration > 0.0005) }'; then
        tail_segment=$(printf '%s/%03d_tail.mkv' "$subtitle_segments" "$segment_index")
        render_subtitle_segment "$blank_frame" "$tail_duration" "$tail_segment"
        printf "file '%s'\n" "$tail_segment" >> "$concat_file"
    fi

    ffmpeg -nostdin -hide_banner -loglevel error -y \
        -f concat -safe 0 -i "$concat_file" \
        -c copy "$work_dir/first_launch_subtitle_overlay.mkv"
}

finish_video() {
    local concat_file="$1"
    local base_video="$2"
    local output_video="$3"

    ffmpeg -nostdin -hide_banner -loglevel error -y \
        -f concat -safe 0 -i "$concat_file" \
        -c copy "$base_video"

    ffmpeg -nostdin -hide_banner -loglevel error -y \
        -i "$base_video" \
        -i "$work_dir/first_launch_subtitle_overlay.mkv" \
        -i "$animatic_dir/audio/first_launch_temp_mix.wav" \
        -filter_complex '[1:v]colorkey=0x000000:0.03:0.0[key];[0:v][key]overlay=0:0:format=auto[v]' \
        -map '[v]' -map 2:a \
        -t "$total_duration" \
        -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p \
        -c:a aac -b:a 160k -ar 48000 \
        "$output_video"
}

write_media_report() {
    normal_duration=$(ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "$animatic_dir/first_launch_animatic.mp4")
    reduced_duration=$(ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "$animatic_dir/first_launch_animatic_reduced_motion.mp4")

    jq -n \
        --arg normal "$normal_duration" \
        --arg reduced "$reduced_duration" \
        --argjson expected "$total_duration" \
        --argjson handoff "$(jq -r '.gameplayHandoffSeconds' "$timeline")" \
        --argjson stateCount "$(jq -r '.states | length' "$timeline")" \
        '{schemaVersion: 1, expectedDurationSeconds: $expected, normalVideoDurationSeconds: ($normal | tonumber), reducedMotionVideoDurationSeconds: ($reduced | tonumber), gameplayHandoffSeconds: $handoff, stateCount: $stateCount, normalVideo: "first_launch_animatic.mp4", reducedMotionVideo: "first_launch_animatic_reduced_motion.mp4"}' \
        > "$animatic_dir/first_launch_animatic_media_report.json"
}

validate_timeline
write_timing_report
make_auxiliary_frames
validate_voice_clips
mix_temporary_audio
render_segments
render_subtitle_overlay
finish_video "$work_dir/normal_concat.txt" "$work_dir/first_launch_normal_base.mp4" "$animatic_dir/first_launch_animatic.mp4"
finish_video "$work_dir/static_concat.txt" "$work_dir/first_launch_static_base.mp4" "$animatic_dir/first_launch_animatic_reduced_motion.mp4"
write_media_report

echo "Built first-launch normal and reduced-motion animatics in $animatic_dir"

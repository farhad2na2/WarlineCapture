#!/usr/bin/env bash

set -euo pipefail

repo_root=$(git rev-parse --show-toplevel)
slice_root="$repo_root/Design/NarrativeVision/FirstLaunch"
animatic_dir="$slice_root/animatic/revision_v2"
timeline="$animatic_dir/first_launch_opening_timeline.json"
voice_script="$animatic_dir/first_launch_opening_voice.tsv"
frames_root="$slice_root"
voice_dir="$animatic_dir/audio/temp_voice"
work_dir="$animatic_dir/.build"
normal_segments="$work_dir/normal_segments"
static_segments="$work_dir/static_segments"
dialogue_segments="$work_dir/dialogue_segments"
runtime_source="$slice_root/ArtReview/PresentationCandidates/RevisionB_UserFeedback/dialogue_runtime_source"
dialogue_frame="$runtime_source/dialogue_frame_9slice.png"
font="/System/Library/Fonts/Helvetica.ttc"

for required_command in ffmpeg ffprobe jq magick; do
    if ! command -v "$required_command" >/dev/null 2>&1; then
        echo "Missing required command: $required_command" >&2
        exit 69
    fi
done

for required_file in "$timeline" "$voice_script" "$dialogue_frame" "$font"; do
    if [[ ! -f "$required_file" ]]; then
        echo "Missing animatic input: $required_file" >&2
        exit 66
    fi
done

mkdir -p "$voice_dir" "$normal_segments" "$static_segments" "$dialogue_segments" "$animatic_dir/auxiliary" "$animatic_dir/audio"

total_duration=$(jq -r '.totalDurationSeconds' "$timeline")
frame_rate=$(jq -r '.frameRate' "$timeline")
resolution=$(jq -r '.resolution' "$timeline")
width=${resolution%x*}
height=${resolution#*x}

validate_timeline() {
    if ! jq -e '
        (.states | length) == 18 and
        .states[0].startSeconds == 0 and
        .gameplayHandoffSeconds >= 150 and
        .gameplayHandoffSeconds <= 180 and
        (.interactiveStatesExcludedFromVideo | index("first_launch.commander_identity")) != null and
        (.interactiveStatesExcludedFromVideo | index("first_launch.guidance_choice")) != null and
        (.totalDurationSeconds == (.states[-1].startSeconds + .states[-1].durationSeconds)) and
        ([range(0; (.states | length) - 1) as $i |
            (.states[$i].startSeconds + .states[$i].durationSeconds) == .states[$i + 1].startSeconds
        ] | all)
    ' "$timeline" >/dev/null; then
        echo "Opening timeline is discontinuous or violates the revised Gate 5 contract." >&2
        exit 65
    fi
}

write_timing_report() {
    printf 'order\tstate_id\tstart_seconds\tduration_seconds\tend_seconds\tmotion\n' \
        > "$animatic_dir/first_launch_opening_timing_report.tsv"
    jq -r '.states[] |
        [.order, .id, .startSeconds, .durationSeconds, (.startSeconds + .durationSeconds), .motion] |
        @tsv' "$timeline" >> "$animatic_dir/first_launch_opening_timing_report.tsv"
}

make_auxiliary_frames() {
    magick -size "${width}x${height}" xc:'#111820' \
        -font "$font" -fill white -pointsize 58 -gravity center \
        -annotate +0-18 'WARLINE CAPTURE' \
        -fill '#7d8a91' -pointsize 24 -annotate +0+52 'FIRST RESPONSE' \
        "$animatic_dir/auxiliary/first_launch_logo.png"

    magick -size 220x220 xc:'#1c2328' \
        -stroke '#c7bca7' -strokewidth 5 -fill none -draw 'rectangle 5,5 214,214' \
        -font "$font" -fill '#f0eadc' -stroke none -gravity center -pointsize 30 \
        -annotate +0+0 'RADIO' "$work_dir/plate_radio.png"

    magick -size 220x220 xc:'#1c2328' \
        -stroke '#c7bca7' -strokewidth 5 -fill none -draw 'rectangle 5,5 214,214' \
        -fill '#aeb7bb' -stroke none -draw 'circle 110,72 110,34' \
        -draw 'roundrectangle 55,120 165,208 22,22' \
        "$work_dir/plate_commander.png"
}

validate_voice_clips() {
    while IFS=$'\t' read -r clip_id start_seconds deadline_seconds speaker backend voice rate volume pitch text; do
        if [[ "$clip_id" == "clip_id" || -z "$clip_id" ]]; then
            continue
        fi

        clip="$voice_dir/$clip_id.wav"
        if [[ ! -f "$clip" ]]; then
            echo "Missing temporary voice clip: $clip" >&2
            echo "Run Tools/NarrativeVision/generate_first_launch_temp_voice.sh with the revision-v2 voice environment." >&2
            exit 66
        fi

        duration=$(ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "$clip")
        if ! awk -v value="$duration" 'BEGIN { exit !(value > 0.05) }'; then
            echo "Temporary voice clip is empty: $clip" >&2
            echo "Regenerate the revision-v2 Microsoft neural voice package." >&2
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
        "$animatic_dir/audio/first_launch_opening_voice_mix.wav"

    ffmpeg -nostdin -hide_banner -loglevel error -y \
        -stream_loop -1 -i "$repo_root/Assets/Game/Audio/Ambience/amb_city_strategic_loop_01.wav" \
        -stream_loop -1 -i "$repo_root/Assets/Game/Audio/Ambience/amb_city_day_loop_01.wav" \
        -stream_loop -1 -i "$repo_root/Assets/Game/Audio/Ambience/amb_base_distant_loop_01.wav" \
        -i "$repo_root/Assets/Game/Audio/Gameplay/game_unit_aircraft_flyby_01.wav" \
        -i "$repo_root/Assets/Game/Audio/Gameplay/game_explosion_large_01.wav" \
        -i "$repo_root/Assets/Game/Audio/Gameplay/game_explosion_small_01.wav" \
        -f lavfi -i "sine=frequency=760:duration=0.65:sample_rate=48000" \
        -filter_complex \
        "[0:a]atrim=duration=${total_duration},volume=0.65[city];\
         [1:a]atrim=duration=${total_duration},volume=3.0[market];\
         [2:a]atrim=duration=${total_duration},adelay=25500|25500,volume=0.32[base];\
         [3:a]adelay=9000|9000,volume=0.45[flyby];\
         [4:a]adelay=17800|17800,volume=0.22[blast1];\
         [5:a]adelay=19900|19900,volume=0.18[blast2];\
         [6:a]adelay=59000|59000,volume=0.12[boot];\
         [city][market][base][flyby][blast1][blast2][boot]amix=inputs=7:normalize=0:dropout_transition=0,alimiter=limit=0.86[ambience]" \
        -map '[ambience]' -t "$total_duration" -ar 48000 -ac 2 \
        "$animatic_dir/audio/first_launch_opening_ambience_mix.wav"

    ffmpeg -nostdin -hide_banner -loglevel error -y \
        -i "$animatic_dir/audio/first_launch_opening_voice_mix.wav" \
        -i "$animatic_dir/audio/first_launch_opening_ambience_mix.wav" \
        -filter_complex '[0:a]volume=1.0[voice];[1:a]volume=0.78[ambience];[voice][ambience]amix=inputs=2:normalize=0:dropout_transition=0,alimiter=limit=0.95[a]' \
        -map '[a]' -t "$total_duration" -ar 48000 -ac 2 \
        "$animatic_dir/audio/first_launch_opening_mix.wav"
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

render_dialogue_segment() {
    local frame="$1"
    local duration="$2"
    local output="$3"

    ffmpeg -nostdin -hide_banner -loglevel error -y \
        -loop 1 -framerate "$frame_rate" -i "$frame" \
        -t "$duration" -vf "scale=${width}:${height},format=rgb24" \
        -r "$frame_rate" -an -c:v ffv1 -pix_fmt rgb24 \
        "$output"
}

speaker_presentation() {
    local speaker="$1"

    case "$speaker" in
        RADIO)
            printf '%s\t%s\t%s\t%s\n' "$work_dir/plate_radio.png" "JRC EMERGENCY CHANNEL" "DISTRICT RADIO" "#6f8790"
            ;;
        DALIA)
            printf '%s\t%s\t%s\t%s\n' "$runtime_source/portrait_dalia.png" "MAJOR DALIA RAHIM" "JRC FIELD COMMAND" "#3f6979"
            ;;
        SAMIRA)
            printf '%s\t%s\t%s\t%s\n' "$runtime_source/portrait_samira.png" "ENGINEER SAMIRA HADDAD" "CIVIL INFRASTRUCTURE LIAISON" "#8a6a45"
            ;;
        ARIA)
            printf '%s\t%s\t%s\t%s\n' "$runtime_source/icon_aria_plate.png" "ARIA" "CIVIC RELAY ASSISTANT" "#00a9c7"
            ;;
        COMMANDER)
            printf '%s\t%s\t%s\t%s\n' "$work_dir/plate_commander.png" "COMMANDER" "EMERGENCY AUTHORITY" "#4f687a"
            ;;
        *)
            echo "Unknown dialogue speaker: $speaker" >&2
            exit 65
            ;;
    esac
}

make_dialogue_frame() {
    local speaker="$1"
    local dialogue_text="$2"
    local output="$3"
    local presentation
    local plate
    local speaker_name
    local speaker_role
    local accent
    local body_frame="$work_dir/dialogue_body.png"

    presentation=$(speaker_presentation "$speaker")
    IFS=$'\t' read -r plate speaker_name speaker_role accent <<< "$presentation"

    magick -background none -fill '#171a1c' -font "$font" -pointsize 27 \
        -size 840x126 -gravity northwest caption:"$dialogue_text" "$body_frame"

    magick -size "${width}x${height}" xc:'#00ff00' \
        \( "$dialogue_frame" -resize '1030x310!' \) -geometry +175+385 -composite \
        \( "$plate" -resize '174x174!' \) -geometry +48+452 -composite \
        -stroke "$accent" -strokewidth 6 -fill none -draw 'rectangle 48,452 222,626' \
        -font "$font" -fill "$accent" -stroke none -pointsize 25 -gravity northwest \
        -annotate +265+414 "$speaker_name" \
        -fill '#4f5659' -pointsize 16 -annotate +265+447 "$speaker_role" \
        "$body_frame" -geometry +265+486 -composite \
        "$output"
}

render_dialogue_overlay() {
    local concat_file="$work_dir/dialogue_concat.txt"
    local blank_frame="$work_dir/dialogue_blank.png"
    local cursor="0.000"
    local segment_index=0

    magick -size "${width}x${height}" xc:'#00ff00' "$blank_frame"
    : > "$concat_file"

    while IFS=$'\t' read -r clip_id start_seconds deadline_seconds speaker backend voice rate volume pitch dialogue_text; do
        if [[ "$clip_id" == "clip_id" || -z "$clip_id" ]]; then
            continue
        fi

        clip_duration=$(ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "$voice_dir/$clip_id.wav")
        end_seconds=$(awk -v start="$start_seconds" -v duration="$clip_duration" -v deadline="$deadline_seconds" \
            'BEGIN { end=start+duration+0.25; if (end>deadline) end=deadline; printf "%.3f", end }')
        gap_duration=$(awk -v start="$start_seconds" -v cursor="$cursor" 'BEGIN { printf "%.3f", start - cursor }')

        if awk -v duration="$gap_duration" 'BEGIN { exit !(duration > 0.0005) }'; then
            gap_segment=$(printf '%s/%03d_gap.mkv' "$dialogue_segments" "$segment_index")
            render_dialogue_segment "$blank_frame" "$gap_duration" "$gap_segment"
            printf "file '%s'\n" "$gap_segment" >> "$concat_file"
            segment_index=$((segment_index + 1))
        fi

        dialogue_image=$(printf '%s/dialogue_%03d.png' "$work_dir" "$segment_index")
        dialogue_segment=$(printf '%s/%03d_text.mkv' "$dialogue_segments" "$segment_index")
        dialogue_duration=$(awk -v end="$end_seconds" -v start="$start_seconds" 'BEGIN { printf "%.3f", end - start }')

        make_dialogue_frame "$speaker" "$dialogue_text" "$dialogue_image"
        render_dialogue_segment "$dialogue_image" "$dialogue_duration" "$dialogue_segment"
        printf "file '%s'\n" "$dialogue_segment" >> "$concat_file"
        segment_index=$((segment_index + 1))
        cursor="$end_seconds"
    done < "$voice_script"

    tail_duration=$(awk -v total="$total_duration" -v cursor="$cursor" 'BEGIN { printf "%.3f", total - cursor }')
    if awk -v duration="$tail_duration" 'BEGIN { exit !(duration > 0.0005) }'; then
        tail_segment=$(printf '%s/%03d_tail.mkv' "$dialogue_segments" "$segment_index")
        render_dialogue_segment "$blank_frame" "$tail_duration" "$tail_segment"
        printf "file '%s'\n" "$tail_segment" >> "$concat_file"
    fi

    ffmpeg -nostdin -hide_banner -loglevel error -y \
        -f concat -safe 0 -i "$concat_file" \
        -c copy "$work_dir/first_launch_dialogue_overlay.mkv"
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
        -i "$work_dir/first_launch_dialogue_overlay.mkv" \
        -i "$animatic_dir/audio/first_launch_opening_mix.wav" \
        -filter_complex '[1:v]colorkey=0x00ff00:0.08:0.0[key];[0:v][key]overlay=0:0:format=auto[v]' \
        -map '[v]' -map 2:a \
        -t "$total_duration" \
        -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p \
        -c:a aac -b:a 160k -ar 48000 \
        "$output_video"
}

write_media_report() {
    normal_duration=$(ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "$animatic_dir/first_launch_opening_reference_v2.mp4")
    reduced_duration=$(ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "$animatic_dir/first_launch_opening_reference_v2_reduced_motion.mp4")

    jq -n \
        --arg normal "$normal_duration" \
        --arg reduced "$reduced_duration" \
        --argjson expected "$total_duration" \
        --argjson handoff "$(jq -r '.gameplayHandoffSeconds' "$timeline")" \
        --argjson stateCount "$(jq -r '.states | length' "$timeline")" \
        '{schemaVersion: 2, purpose: "reference-only", expectedDurationSeconds: $expected, normalVideoDurationSeconds: ($normal | tonumber), reducedMotionVideoDurationSeconds: ($reduced | tonumber), gameplayHandoffSeconds: $handoff, stateCount: $stateCount, interactiveStatesExcludedFromVideo: true, normalVideo: "first_launch_opening_reference_v2.mp4", reducedMotionVideo: "first_launch_opening_reference_v2_reduced_motion.mp4"}' \
        > "$animatic_dir/first_launch_opening_media_report.json"
}

validate_timeline
write_timing_report
make_auxiliary_frames
validate_voice_clips
mix_temporary_audio
render_segments
render_dialogue_overlay
finish_video "$work_dir/normal_concat.txt" "$work_dir/first_launch_normal_base.mp4" "$animatic_dir/first_launch_opening_reference_v2.mp4"
finish_video "$work_dir/static_concat.txt" "$work_dir/first_launch_static_base.mp4" "$animatic_dir/first_launch_opening_reference_v2_reduced_motion.mp4"
write_media_report

echo "Built revised first-launch reference videos in $animatic_dir"

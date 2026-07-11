# First Launch Phase 10 Integrated QA Report

Status: Superseded as final acceptance by first user live-playback review; retained as technical baseline evidence only
Date: 2026-07-11
Unity: 6000.5.2f1

## Acceptance Supersession

The first user review found phone-readability, speech-frame proportion/artifact, shipping/reviewer control scale and style, runtime environmental-audio, city-establishment, and character-introduction deficiencies. The technical measurements below remain valid, but visual/audio Gate 9 and integrated Gate 10 acceptance are reopened under tracker Phase 10R. M01 must not begin until the revised live sequence passes a second user review.

## Visual And Device-Aspect Evidence

- `../phase9/first_launch_integrated_runtime_review_1920x1080.mp4`: production Unity prefab/config/player playback, 174 seconds, restrained motion, typed dialogue, portraits/ARIA icon, Microsoft temporary voices, and ambience.
- `../phase9/first_launch_integrated_runtime_contact_sheet.png`: nine-position full-playback inspection sheet.
- `normal_playback_2400x1080.png`: 20:9 source and runtime UI at the target wide landscape aspect.
- `tablet_landscape_1920x1200.png`: representative 16:10 tablet landscape framing.
- `../phase8/dialogue_max_expansion_2400x1080.png`: maximum English expansion with extra-large subtitles and opaque backing.
- `../phase9/reviewer_controls_reduced_motion_2400x1080.png`: reduced-motion and 20:9 reviewer evidence.

The panel layer now uses `AspectRatioFitter.AspectMode.EnvelopeParent`. It remains full-bleed without stretching at 16:9, 20:9, and 16:10. Dialogue, portraits, ARIA icon, pointer, and Skip remain inside the authored safe area with no overlap in retained captures.

## Runtime And Performance Evidence

See `../../performance/FIRST_LAUNCH_EDITOR_PERFORMANCE_REPORT.md`.

| Measure | Result |
|---|---:|
| Cold FL-P01 Addressables load | 130.052 ms |
| Warm transition average | 0.235 ms |
| Warm transition maximum | 1.052 ms |
| Stable playback sample | 1,800 ticks |
| Stable managed allocation after warmup | 0 bytes |
| Resident panel handles | 2 maximum: current + optional next |
| Current decoded panel texture estimate | 0.88 MiB |
| Temporary voice assets referenced | 17 / 0.66 MiB runtime memory |

These are deterministic development-Mac Editor measurements. They pass the architecture/residency/allocation baseline but do not replace physical Android GPU, thermal, frame pacing, and audio-start measurements.

## Failure, Offline, And Retail Checks

- Missing optional voice reaches readable-text auto-advance and does not stall.
- Narrative runtime sources contain no HTTP, cloud TTS, Microsoft endpoint, `edge-tts`, or `Resources.Load` path.
- The review MP4 is evidence only; retail playback remains layered Unity content.
- Development reviewer controls are alpha-zero, non-interactable, and non-raycasting by default.
- Reviewer mode remains available only through the explicit Editor request path and does not mutate production profile state.
- Consolidated Gate 8/9 suite passes 26 checks after authored timing and aspect-envelope changes.
- Live Menu PlayMode test passes Addressables boot, reduced motion, subtitle/safe-area controls, complete state traversal, debrief Skip, and command-base arrival.

## Cultural, Symbol, Equipment, And Generated-Art Review

- `../../visual/continuity/CONTINUITY_VALIDATION.md` passes Dalia, Samira, ARIA, Commander, JRC, named civilians, and Ash Line identity/equipment continuity.
- `../../visual/world/WORLD_CONTINUITY_VALIDATION.md` passes Old Market geography, persistent damage, Match-aligned low-poly language, text/sign removal, and cultural/symbol safety.
- `../../../ArtReview/FinalArt/FINAL_ART_REVIEW_LEDGER.md` records user approval of all 22 final revisions.
- `../../../ArtReview/FinalArt/FINAL_ART_PROVENANCE.md` records source, revision, hash, and runtime export evidence for every panel.

## Open Gate 10 Blockers

1. **Illustrated-to-3D handoff comparison:** no production M01 camera or final M01 Old Market gameplay composition exists. The approved FL-P18 revision is the binding future geography/camera authority. A valid comparison cannot be fabricated from the current Match scene.
2. **Physical Android profiling:** no Android device or ADB runtime is available in this workspace. Release lock still requires frame-time, GPU memory, thermal, and audio-start evidence on the target development device.
3. **Existing shell regression:** the broad `UIShellCurrentContentLoadTests` suite remains blocked by the pre-existing missing `statusChipSprite`; the focused shell route/audio suite passes.

Gate 10 remains open until the first two prerequisites exist and the unrelated shell regression is repaired or formally waived. No user art approval is required to resolve these engineering prerequisites.

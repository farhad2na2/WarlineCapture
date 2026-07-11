# FirstLaunch Phase 9 Menu Integration Report

Status: Gate 9 passed; reusable player, authored timing, restrained motion, reviewer mode, production Menu startup, full playback, and Skip acceptance implemented
Date: 2026-07-11
Gate 8 status: Passed with Addressables 3.1 current/next residency
Gate 9 status: Passed with complete Skip evidence and graphics-enabled integrated runtime playback

## Implemented

- Production `Menu.unity` contains one hidden, non-blocking `NarrativeLayer` as the final child of the UI canvas.
- `MenuBootstrapView` references the exact sequence, speaker, punctuation, and narrative view assets.
- Fresh profiles keep the ECS shell in `UiShellMode.None`; the Main Menu is not presented as the first experience.
- Returning and migrated legacy profiles enter the existing Main Menu path.
- `HandoffPending` profiles resume the Match handoff without replaying the narrative.
- Confirmed handoff enters Match through `UiShellRouteIntent.EnterMatch`, preserving loading presentation, additive scene loading, and Match start sequencing.
- Profile persistence records schema, status, last state, Commander callsign/name/portrait, guidance, watched/skipped state, and resumable handoff.
- Identity and guidance are separate live TMP/Button/InputField surfaces, not baked into panel art or video.
- Skip opens a live confirmation surface. Cancelling resumes playback. Confirming persists valid default or selected identity/guidance and emits one handoff.
- Runtime player supports static and voiced states, multiple lines, localization fallback resolution, tap complete/continue, auto-advance, pause/resume, restart, previous/next, cancellation, and typed handoff publication.
- Automatic playback honors the authored 174-second opening timeline, including panel holds, delayed dialogue entrances, FL-P04 speaker spacing, and an exact M01 handoff at 174.00 seconds.
- Runtime panel motion supports bounded PushIn, PullBack, DriftLeft, DriftRight, and StaticImpact presets. Reduced motion keeps the panel static while preserving state duration and story order.
- Editor review entry is available at `Game/Narrative/First Launch/Review In Play Mode` with Play/Pause, Restart, Previous/Next, timeline seek, Skip To Game, Jump To Debrief, reduced-motion preview, and capture request controls.
- Reviewer mode is development-only and leaves completed/fresh production profiles unchanged.
- Config assets are rebuilt in place, preserving GUID and Menu scene references across repeated same-process builds.
- The sequence stores Addressables `AssetReferenceSprite` values with no direct panel dependencies. Runtime retains current/next operation handles and releases superseded assets; the temporary Resources prototype and wrappers were removed.

## Visual Evidence

- `../phase8/live_identity_1920x1080.png`
- `../phase8/live_guidance_1920x1080.png`
- `../phase8/live_skip_confirmation_1920x1080.png`
- `../phase8/dialogue_standard_1920x1080.png`
- `../phase8/dialogue_max_expansion_2400x1080.png`
- `reviewer_controls_normal_1920x1080.png`
- `reviewer_controls_reduced_motion_2400x1080.png`
- `first_launch_integrated_runtime_contact_sheet.png`
- `first_launch_integrated_runtime_review_1920x1080.mp4`
- `first_launch_integrated_runtime_timing.tsv`

All retained interaction captures are live Unity UI over approved panel art. The integrated video is rendered from the production prefab, config, player, panel motion, dialogue reveal, portraits/icons, and Addressables-backed sequence. Interactive identity/guidance screens and development reviewer controls are intentionally excluded from the video because they remain selectable Unity UI. The temporary Microsoft voices and ambience are muxed after deterministic rendering. The Commander uses the existing shadowed neutral portrait; no unapproved fixed player face was introduced.

## Automated Evidence

- Consolidated Gate 8/9 validation covering Addressables residency, motion bounds/timing, authored panel/line scheduling, direct-dependency rejection, reviewer controls, mandatory debrief payloads, post-identity preservation, presentation prefab, reusable player, Menu wiring, profile-safe review navigation, and repeated config builds: 26 focused tests passed.
- Reusable player progression, interaction request, Skip, typed handoff, stepping, restart, and cancellation: 3 focused tests passed.
- Identity and guidance view defaults, edits, selection, accessibility labels, and commit debounce: 4 focused tests passed.
- Menu scene wiring, fresh profile confirmation, completed-profile bypass, and pending-handoff resume: 3 focused tests passed.
- Startup gate pending/FirstLaunch/menu/Match behavior: 4 focused tests passed.
- SaveService profile migration and persistence: 7 Unity Test Framework tests passed.
- Existing shell route/audio regression: 8 focused tests passed.
- Live Unity PlayMode Menu test passed: Addressables panel boot, live reviewer controls, reduced motion, complete state traversal, debrief Skip, and command-base arrival.
- Integrated playback media validation passed: H.264 `1920x1080` at 10 fps, AAC audio, exact `174.000` second duration, audio peak `-4.93 dB`, audio RMS `-24.51 dB`, and all 17 opening panel transitions matching the authored timeline.

## Skip Checkpoint Evidence

- `skip_early_fl-p02_1920x1080.png`
- `skip_middle_fl-p10_1920x1080.png`
- `skip_identity_fl-p08_1920x1080.png`
- `skip_final_opening_fl-p18_1920x1080.png`

Fresh-profile checkpoints retain the default-identity confirmation. A separate automated path verifies that committed Commander identity, portrait, and guidance bypass that modal, route directly, and remain unchanged.

## Remaining After Gate 9

- Capture 20:9 and representative tablet playback evidence.
- Measure load time, transition stutter, panel/audio residency, and stable-playback allocations.
- Run remaining offline/missing-audio, retail-control, cultural-art, continuity, and device acceptance checks for Gate 10.

## Known Unrelated Regression

`UIShellCurrentContentLoadTests.RunFocusedValidation` currently fails before FirstLaunch-specific assertions because the existing placement-bar prefab has no `statusChipSprite`. This report does not modify that unrelated prefab. The focused shell route/audio regression passes.

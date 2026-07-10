# APH-310 Integrated Graphics and Visual Review

## Result

The required settings, graphics, Match, and visual-capture workflows completed successfully on `main` using Unity `6000.5.2f1` with the Metal graphics device.

The day and dusk frames pass structural review. The night frame is valid and nonblank, but readability is red: units, buildings, roads, and terrain boundaries are difficult to identify without night-vision or an additional minimum lighting floor. This report does not change the approved lighting values.

## Automated Evidence

- Settings popup: `[SettingsPopupValidation] result=Passed tests=8`
- Android visual quality: `[AndroidVisualQualityValidation] result=Passed tests=12`
- Match settings/audio smoke: `[SettingsAudioRuntimeSmoke] result=Passed` at `MatchHudReady`
- Visual capture: `[MobileVisualQualityPlayModeCapture] result=Passed profile=current`
- Unity logs:
  - `/private/tmp/aph310-settings-popup.log`
  - `/private/tmp/aph310-android-visual.log`
  - `/private/tmp/aph310-match-smoke.log`
  - `/private/tmp/aph310-visual-capture-dusk-corrected.log`

## Capture Evidence

Artifact directory: `Design/AgentReports/Captures/ArchitecturePerformanceHardening/APH-310/`

| View | Time | Resolution | Mean luma | Review |
|---|---:|---:|---:|---|
| `current_gameplay_zoom.png` | 12:00 | 1920x1080 | 133.61 | Pass; readable normal gameplay framing |
| `current_max_zoom_out.png` | 12:00 | 1920x1080 | 140.66 | Pass; readable high tactical framing |
| `current_dusk_phase.png` | 21:00 | 1920x1080 | 108.16 | Pass; distinct transition state without geometry or fog reset |
| `current_night_phase.png` | 23:00 | 1920x1080 | 22.34 | Red; nonblank but too dark for reliable battlefield identification |

Mean luma uses Rec. 709 weights over the complete RGB frame. It is diagnostic evidence, not a standalone art acceptance threshold.

## Visual Checks

- No missing building shells, detached interiors, or blank camera frames were observed.
- No new floating/buried props, road alignment changes, terrain material loss, or persistent fog reset was observed across the captured states.
- The 21:00 dusk state is visibly distinct from 12:00. The initial 18:00 proof attempt was rejected because the configured dusk transition is 20:30-21:30.
- The 23:00 state preserves the authored Day/Night result, but its readability is insufficient for final AAA mobile acceptance without a deliberate design decision.

## Follow-up

Keep the current lighting behavior unchanged until the owner approves either a minimum night illumination floor or reliance on the existing night-vision treatment. Android APH-311 captures must record this readability issue separately from frame, GPU, memory, and thermal results.

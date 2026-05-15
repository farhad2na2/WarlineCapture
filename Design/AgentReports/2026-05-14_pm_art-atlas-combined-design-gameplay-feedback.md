# PM Routing: Combined Designer And Gameplay Feedback To Art/Atlas

Date: 2026-05-14
Lane: Art/Atlas
Topic: Correct M01 AAA layered sample for design alignment and implementation readiness

## Decision

Art/Atlas remains the active owner. The current two-frame sample is not approved yet.

Designer feedback:
`Design/AgentReports/2026-05-14_designer_m01-art-sample-alignment-review.md`

Gameplay feedback:
`Design/AgentReports/2026-05-14_gameplay_m01-mockup-asset-implementation-audit.md`

## Combined Required Fixes

Art/Atlas must revise only the two-frame approval sample:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-01_TacticalStart_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/SourceNotes.md`
- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`

Visual/design fixes:

- Use one shared tactical camera plate and one shared zoom/framing lock for M01-01 and M01-02.
- Keep player and enemy squad screen scale consistent between frames.
- Normalize player and enemy infantry scale on the same isometric ground plane. The current mockup makes enemies read smaller than player units; that is not acceptable unless a documented unit-class scale rule says they should be smaller.
- M01-01 must read as no selection with no command mode, no selected ring, no move/attack/objective markers, neutral or disabled command controls, and M01-only objective text.
- M01-02 must read as selected but not in Move/Attack/command mode.
- Build must be hidden or disabled for M01 with `MissionDoesNotAllowBuild` if shown.
- Enemy ring/health treatment must be declared as permanent affiliation or hidden if stateful markers.

Implementation asset-prep fixes:

- Provide a tactical camera source/plate with orthographic zoom, camera center, world bounds, and minimap viewport mapping.
- Provide player and enemy sprite sheet/frame keys, facing, formation offsets, pivots, feet anchors, and contact-shadow rules.
- Include player/enemy infantry scale comparison notes proving both use the same isometric projection scale.
- Provide selection ring source, placement, pivot, scale, z-order, and hidden/visible state rules.
- Prepare move/attack/objective/invalid marker sources and state rules for future frames, while hidden in M01-01/M01-02.
- Slice HUD chrome for objective/log/resource/squad tray/command/top controls/minimap, with 9-slice and transparent-corner rules.
- Keep all text/icons/counters/objective ticks/health values/button labels/reason codes separate from panel/button art.
- Update all LayerPack manifests and source notes with existing reusable assets, missing Art assets, import settings, z-order, anchors, and Unity ownership paths.

## Held Lanes

Gameplay runtime implementation and QA/HCI remain blocked.

## Next Gate

After Art/Atlas submits the corrected sample, Designer/PM/user review it again. If approved, Gameplay may implement only `M01-01_TacticalStart` exactly from the approved layered sample and LayerPack. The rest of the sequence remains blocked until the first implementation path is confirmed and PM/user approves continuing.

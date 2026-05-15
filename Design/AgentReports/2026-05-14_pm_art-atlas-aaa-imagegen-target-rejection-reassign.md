# PM Rejection And Reassignment: M01 Mockups Must Match AAA Imagegen Locked Targets

Date: 2026-05-14
Lane: Art/Atlas
Topic: M01 step-by-step gameplay mockups

## Decision

Rejected and reassigned to Art/Atlas. The latest output under `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/` is not approved and has been removed from the output folder.

## Reason

The produced frames do not match the previous high-quality AAA imagegen locked targets. They read as deterministic/composited placeholder frames with flat HUD panels, weak chrome, poor typography/spacing, visible truncation, and simplified result UI. The output is not implementation-ready target-lock art and must not be routed to Gameplay or QA/HCI.

Art also violated the PM gate by generating the full sequence instead of a 1-2 sequence approval sample, and did not deliver the required layered `LayerPack` implementation structure.

## Required Visual Authority

The corrected Art pass must align UI and gameplay with these locked targets:

- `Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png`
- `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
- `Design/VisualLock/POP-05_MissionResult/POP-05_MissionResult_Landscape_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_PlayerRifleSquad_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_EnemyPatrol_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Markers_SelectionMoveAttackEnemyObjectiveHover.png`

## Reassigned Task

Current owner:
Art/Atlas

Art/Atlas must produce only a tiny 1-2 sequence AAA layered sample first:

- `M01-01_TacticalStart_1920x1080.png`
- `M01-02_SquadSelected_1920x1080.png`
- one 1920x1080 sample contact sheet
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- per-frame layer breakdowns for the sample frames

Required output locations:

- Flattened review PNGs: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/`
- Sample contact sheet: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`
- Package manifest: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- Per-frame layer manifests: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/<FrameId>_layers.json`
- Source notes/prompt notes: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/SourceNotes.md`
- Handoff report: `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`

Do not place corrected results outside these paths. Do not write runtime/import assets into `Assets/` during this approval task.

Optional second proof only if PM/user requests combat/marker validation:

- `M01-05_AttackPreview_1920x1080.png`

## Hard Requirements

- The sample must be AAA imagegen target-lock quality, visually aligned with the locked targets listed above.
- The sample must be layered like existing lockups, not flattened-only PNGs.
- Flattened PNGs are visual QA references only.
- Include intended Unity object paths or prefab ownership, rects/anchors/resolution, z-order, alpha rules, and reusable/stateful/dynamic/reference-only status.
- Do not generate the remaining frames until the user approves the 1-2 sequence sample as good and aligned.
- Do not route Gameplay or QA/HCI until user-approved mockups exist.
- Do not restore the rejected ugly images.

## Held Lanes

Gameplay and QA/HCI remain blocked until the user approves the corrected layered AAA sample and then approves the full mockup set.

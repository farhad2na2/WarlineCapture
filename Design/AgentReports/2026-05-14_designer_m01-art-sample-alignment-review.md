# Designer Review: M01 Art Sample Alignment

Date: 2026-05-14
Lane: Designer
Status: needs Art/Atlas fixes
Owner of next action: Art/Atlas

## Sources Reviewed

- `Design/AgentTasks/designer_current.md`
- `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`
- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`
- `Design/AgentReports/2026-05-14_pm_designer-spec-review-art-routing.md`
- `Design/AgentReports/2026-05-14_pm_art-atlas-deterministic-mockups-rejected.md`
- `Design/AgentReports/2026-05-14_pm_art-atlas-aaa-imagegen-target-rejection-reassign.md`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-01_TacticalStart_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`
- `Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png`
- `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`

## Decision

Decision: needs fixes before approval

The sample is directionally strong enough to keep as the AAA quality target family, but it is not aligned enough to approve for full-frame production or Gameplay implementation. The user's zoom-level concern is valid: M01-01 and M01-02 do not read as the same tactical camera at the same zoom, even though both layer manifests claim `camera.default_start`.

Gameplay and QA/HCI remain held. Do not route implementation or project import from this sample.

## What Is Working

- Overall image quality is a major improvement over the rejected deterministic pass.
- The tactical map material density, damaged-city mood, lighting, and HUD chrome are close to the approved AAA VisualLock direction.
- The sample is correctly limited to a first 1-2 sequence approval set rather than a full unapproved frame run.
- The LayerPack structure is present and uses useful Unity ownership paths, rects, anchors, z-order, alpha rules, and dynamic/stateful/reusable status.
- M01-02 clearly communicates squad selection with cyan ground rings and readable unit grouping.

## Blocking Alignment Issues

1. Camera and zoom are inconsistent between M01-01 and M01-02.
   - M01-01 player unit rect: `[440, 570, 180, 150]`
   - M01-02 player unit rect: `[500, 600, 210, 170]`
   - M01-01 enemy unit rect: `[1420, 310, 190, 150]`
   - M01-02 enemy unit rect: `[1250, 225, 240, 170]`
   - Both frames are supposed to use `camera.default_start`; selection alone must not reframe, zoom in, or change unit scale. The player and enemy squads should stay at the same world projection size and tactical camera scale between M01-01 and M01-02.

2. M01-01 no-selection state is not clean enough.
   - Designer spec requires no unit selected, neutral/disabled command panel until selection, no command mode, and no visible move/attack/objective markers.
   - Current M01-01 shows a full squad tray and active-looking command bar. If the squad tray remains visible for SCN-08 HUD continuity, its state must clearly read as unselected/neutral, not selected or command-ready.
   - Build appears as an available command. For M01, Build must be hidden or clearly disabled; if visible in later runtime, the disabled reason is `MissionDoesNotAllowBuild`.

3. M01-02 selected state incorrectly implies an active command mode.
   - Designer spec requires squad selected, command controls enabled, and no command mode banner active.
   - Current M01-02 has Move visually highlighted as if Move mode is active. In this frame, selection has happened but Move has not been chosen; Move, Attack, Stop, and Hold should read available but inactive.

4. Objective content must stay scoped to M01.
   - M01 objective is `Destroy hostile patrol`.
   - Extra objectives such as `Secure the intersection` and `Hold the forward position` should not appear in the M01-01 approval sample unless PM explicitly expands M01. If star goals remain as HUD language, they must not conflict with the primary objective or imply extra M01 gameplay.

5. Enemy ring/health treatment needs a state rule.
   - M01-01 declares `worldMarkersVisible: false`, yet the flattened frame shows red rings/health readouts on enemy soldiers.
   - If these are permanent enemy affiliation/health decals, Art/Atlas must state that in the layer manifest and keep them restrained. If they are world markers, they must be hidden in M01-01 and M01-02 until attack/targeting states.

## Required Art/Atlas Fixes

- Rebuild M01-01 and M01-02 from one shared tactical camera plate and one shared zoom/framing lock.
- Keep player and enemy squad screen scale consistent between M01-01 and M01-02; selection can add rings and selected HUD states, but cannot resize the world.
- Preserve the same camera center unless the Designer spec step explicitly calls for camera movement. M01-02 does not.
- M01-01: show no selection, no selected ring, no command mode, no move/attack/objective markers, neutral/disabled command controls, and M01-only objective text.
- M01-02: show selection ring, selected squad panel/card state, enabled command controls, no Move/Attack mode highlight, no command mode banner, no issued movement.
- Clarify in both layer manifests whether enemy red rings/health are permanent unit affiliation layers or stateful world markers; hide them if they are markers.
- Keep the layered package and flattened PNG review target pattern. Do not generate remaining frames until this 1-2 sample is corrected and approved.

## Acceptance Criteria For Corrected Sample

- `M01-01_TacticalStart_1920x1080.png` and `M01-02_SquadSelected_1920x1080.png` use identical orthographic isometric camera zoom and world scale.
- Layer manifests for both frames show consistent camera, player squad, and enemy patrol scale/placement unless a state change explicitly requires only UI/selection layer changes.
- M01-01 reads as no-selection at a glance.
- M01-02 reads as selected but not in Move, Attack, or any other command mode.
- The objective panel is M01-specific and does not introduce unapproved objectives.
- Build is not presented as an available M01 command.
- The corrected contact sheet makes the transition from M01-01 to M01-02 look like a selection-state change, not a camera cut.
- No runtime files, Unity assets, imports, Gameplay routing, or QA/HCI routing are changed before user approval.

## Routing

Designer review result: sample not approved yet

Next lane: Art/Atlas

Art/Atlas action: revise the two-frame AAA layered sample and contact sheet using the fixes above

Gameplay and QA/HCI: held

User approval required before full mockup production, project import, or Gameplay implementation: yes

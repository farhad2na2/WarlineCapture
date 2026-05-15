# Designer Review: M01 Imagegen Sample Alignment

Date: 2026-05-14
Lane: Designer
Status: approved for PM/user visual approval
Owner of next action: PM/user

## Sources Reviewed

- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/designer_pm_message.md`
- `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`
- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`
- `Design/AgentReports/2026-05-14_pm_art-atlas-imagegen-only-marker-rejection.md`
- `Design/AgentReports/2026-05-14_pm_art-atlas-latest-reference-comparison-rejection.md`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/CameraLock_M01_DefaultStart.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/AssetPrep_M01_Sample.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-01_TacticalStart_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`
- `Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png`
- `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`

## Decision

Decision: approved for PM/user visual approval

This approval is for the two-frame imagegen visual sample only. It is not runtime implementation approval, not full-sequence approval, and not permission to route Gameplay or QA/HCI before the user approves the sample.

The latest imagegen-only pass resolves the prior Designer blockers well enough for PM/user review:

- M01-01 and M01-02 now read as the same intended tactical camera, zoom, framing, and unit scale.
- M01-01 reads as tactical start/no selection.
- M01-02 reads as selected squad/no active command mode.
- The HUD, markers, selected-squad world status, enemy readability, minimap, squad cards, objective panel, threat feed, top bar, and command bar are now close enough to the approved AAA VisualLock direction for user approval.

## Alignment Checks

Camera/zoom/framing: pass. The contact sheet shows M01-01 and M01-02 sharing the same battlefield composition rather than cutting to a new zoom. `CameraLock_M01_DefaultStart.json` and both frame manifests use the same `m01.default_start.shared_sample_camera`, player squad rect `[405, 570, 230, 190]`, enemy patrol rect `[1345, 235, 250, 190]`, and minimap viewport rect `[1612, 815, 105, 85]`.

M01-01 tactical start/no selection: pass. The flattened frame shows no player selection rings, no selected-squad shield/status bar, no command mode, no move/attack/objective/invalid world marker, and muted command controls. The objective panel is M01-scoped with `Destroy hostile patrol` and a Star Goals row.

M01-02 selected squad state: pass. The flattened frame shows four cyan under-foot selection rings, a blue/cyan shield plus segmented status bar above the selected squad, selected squad card emphasis, enabled command buttons, and no Move/Attack command mode banner. The SELECT button being emphasized reads as selected-state affordance rather than a command mode.

Command bar state/order: pass for visual approval. The latest sample uses `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD` and removes the prior `SPECIAL`/dominant Build mismatch. Build is not presented as a normal available M01 command in the visible five-button set; metadata still records `MissionDoesNotAllowBuild`.

HUD quality: pass for visual approval. The latest panels are closer to the original AAA reference: darker beveled glass/metal panels, more restrained cyan trim, stronger top bar, richer squad cards, improved command buttons, clearer minimap frame, and better objective/threat-feed density.

Selected markers and selected-squad status: pass. The cyan rings have appropriate ellipse/perspective, restrained glow, and per-soldier placement. The shield plus segmented blue bar above the squad now matches the intended selected-world-status language and avoids an extra squad-name label.

Enemy red readability: pass. Enemy red foot rings and segmented health bars are restrained enough and are documented as permanent affiliation/readability layers, not attack-target markers.

Minimap: pass for sample approval. The minimap now shares the same style family as the HUD, keeps a visible viewport, and supports the same camera relationship across M01-01 and M01-02.

Text/content: pass with runtime caveat. The visible generated text is acceptable as visual-reference text for a target-lock image, but runtime must rebuild all text with native TMP and exact data. The sample keeps the M01 objective focused on `Destroy hostile patrol` and restores Star Goals density without introducing extra tactical objectives.

LayerPack consistency: pass for visual approval. The manifests describe shared camera, state deltas, enemy affiliation, selected rings, selected status, command state, HUD groups, and missing runtime prep clearly enough for PM/user review and future implementation planning.

## Remaining Notes Before Gameplay

- Flattened PNGs are visual QA references only. They must not be sliced or imported as runtime source.
- Gameplay still needs clean source layers/assets before implementation: no-HUD tactical plate, native HUD chrome sprites, native TMP text, icons/counters, minimap texture/viewport transform, unit pivots, formation offsets, and import settings.
- The full M01 step-by-step frame set remains ungenerated and unapproved.
- Optional M01-05 attack-preview validation remains available later if PM/user wants combat and attack marker proof before full-frame production.
- Any user rejection of this two-frame sample should route back to Art/Atlas, not Gameplay.

## Routing

Designer review result: approved for PM/user visual approval

Next lane: PM/user

Art/Atlas after approval: produce the remaining M01 step-by-step mockup frames/contact sheets from the Designer spec and this approved visual direction

Gameplay and QA/HCI: held

User approval required before full mockup production, project import, or Gameplay implementation: yes

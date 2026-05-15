# PM Message To Art/Atlas

Date: 2026-05-14
Priority: P0
Status: complete

Art/Atlas delivered the latest imagegen-only two-frame approval sample:

- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`

Designer is now the active next lane for alignment review. Art/Atlas waits for Designer/PM/user review before making more changes.

Prior assignment context:

Designer delivered the Design-owned M01 step-by-step gameplay spec:

- `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`

PM rejection: the image pass under `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/` is not approved. It looks deterministic/assembled, is below target-lock quality, and does not align UI/gameplay with the previous high-quality AAA imagegen locked targets. That pass has been removed. Do not restore it, present it for approval, continue from it as art direction, or leave stale rejected frames beside corrected samples.

Designer review update: the latest two-frame AAA sample is also not approved yet. Read `Design/AgentReports/2026-05-14_designer_m01-art-sample-alignment-review.md` and revise only the first sample set before asking for approval again.

Gameplay audit update: Gameplay completed `Design/AgentReports/2026-05-14_gameplay_m01-mockup-asset-implementation-audit.md`. Use it as required implementation-prep feedback. The corrected sample must be visually aligned and also asset-prepared so Gameplay can later implement `M01-01_TacticalStart` pixel-perfect.

Latest Gameplay audit update: Gameplay completed `Design/AgentReports/2026-05-14_gameplay_m01-corrected-sample-asset-audit.md`. Decision: needs Art/Atlas fixes before Designer/PM/user implementation approval. The package is materially closer and the user approved the quality direction, but selected marker treatment is still not implementation-ready.

User scale feedback: enemies in the current mockup read smaller than player units. Fix this. In a true isometric mockup, player and enemy infantry on the same ground plane must use the same projection scale unless a documented unit-class scale rule says otherwise.

User quality approval/fix note: the user approves the new sample quality direction, but the selected marker blue circle under each soldier is missing in `M01-02_SquadSelected`. Fix this now. Do not rework the approved quality direction, camera, world composition, HUD style, or unit scale except where required to add the selected markers correctly.

Latest PM/user rejection: the new selected-marker pass is not approved. The blue circles added under the soldiers are ugly and do not match the previous clean blue VisualLock marker mockup. This is an Art quality failure, not a Gameplay audit blocker. Replace those circles; do not keep, polish, or present them again.

PM visual check against the original reference image: the current `M01-02_SquadSelected_1920x1080.png` still does not match the original selected-state treatment. The under-foot rings are too weak/plain and do not read like the original clean neon segmented blue rings. The selected squad world status/health treatment above the soldiers is missing: the original shows a blue shield icon plus segmented blue horizontal bar above the selected squad. Enemy soldiers in the original also use readable red above-head segmented health bars plus restrained red foot rings. Fix all of these as Art output before asking for review again.

PM HUD quality rejection: the current HUD is not acceptable compared with the original reference. The original HUD has premium beveled sci-fi panels, layered dark glass/metal depth, crisp cyan edge lighting, dense readable typography, integrated icons, polished command buttons, high-quality squad cards, and a detailed minimap frame. The current HUD reads flatter and less finished: panel bevels/inner shadows/edge glow are weaker, command buttons and Build state look crude, bottom squad cards lack the original visual richness, log/objective panels are simplified, and minimap framing/readability does not match the original AAA VisualLock finish. Rebuild the HUD quality to the original reference; do not only fix the marker overlays.

Latest PM comparison after Art marked complete: the new pass is closer on selected rings and enemy red bars, but it is still not approved. HUD quality and layout still do not match the original reference. The current HUD has bright cyan construction-line corner strokes and oversized outlines that read like debug/layout guides, not the original's polished beveled sci-fi frame. The command bar order/state is wrong versus the reference (`SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`); current output shows `STOP`, `HOLD`, `MOVE`, `ATTACK`, `SPECIAL` and an oversized crude Build block. The objective panel is simplified and missing the original Star Goals section. The selected squad world bar is too flat/rectangular, includes a visible label not present in the reference, and does not match the original segmented shield/bar polish. The minimap, squad cards, log panel, and command buttons remain below the original AAA HUD finish.

Latest PM marker rejection: the markers are still not 100% exactly matched to the original mockup. Stop deterministic/image-editing fixes for this visual pass. Do not use scripted compositing, manual shape overlays, pixel-patch editing, deterministic marker placement, or local image-editing workflows to patch the current PNG. Use the imagegen skill to generate a fresh AAA bitmap mockup from the original reference and locked VisualLock references. The deliverable must look like one cohesive imagegen target-lock frame, not a base screenshot with edited UI/marker patches.

Focused selected-marker correction required now:

- Replace the rejected marker pass in `M01-02_SquadSelected_1920x1080.png` with visible clean blue/cyan selected marker rings under each selected soldier.
- Use the established AAA target-lock visual style from the approved marker reference, not a flat deterministic overlay.
- Required marker source/style references:
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Markers/selection_ring.png`
  - `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Markers_SelectionMoveAttackEnemyObjectiveHover.png`
- Original selected-state reference provided by PM/user in thread: selected player squad has four clean blue/cyan segmented rings under feet, a blue shield icon, and a segmented blue horizontal status/health bar above the selected soldiers; enemy soldiers have readable red segmented health bars above heads and restrained red foot rings.
- Original HUD reference provided by PM/user in thread: premium RTS battle HUD with beveled dark glass/metal panels, cyan trim, readable objective/log/resource/top bar typography, polished squad cards, command buttons, Build treatment, minimap frame, and integrated icon language.
- The marker must read as a thin/segmented sci-fi blue/cyan ring with transparent center, soft controlled glow, correct isometric ellipse perspective, and terrain-integrated lighting.
- Do not draw crude filled blue circles, thick flat ellipses, high-saturation blobs, programmer debug rings, or quick paint-over marker shapes.
- Add four explicit per-soldier selected marker child layers or entries in `LayerPack/Frames/M01-02_SquadSelected_layers.json`.
- Each marker layer must include source asset, rect, foot anchor, pivot, scale, z-order, alpha rule, and visible state.
- Add a separate selected-squad world status/health treatment above the selected soldiers: blue shield icon plus segmented blue horizontal bar, anchored to the squad/leader area in world space.
- Add explicit separate LayerPack entries for that shield icon and segmented status/health bar, with source asset, rect, world anchor, pivot, scale, z-order, alpha rule, and visible state.
- Ensure enemy soldiers retain original-style readable red above-head segmented health bars and restrained red foot rings. Mark these as permanent affiliation/readability layers, not attack-target markers.
- Rebuild the HUD in both `M01-01_TacticalStart_1920x1080.png` and `M01-02_SquadSelected_1920x1080.png` so objective/log panels, top resource bar, squad cards, command buttons, Build disabled state, and minimap frame match the original reference quality, style, and layout density.
- Do not use flat placeholder HUD panels, crude generated text, weak bevels, mismatched button geometry, low-detail squad cards, simplified minimap chrome, or UI that looks lower quality than the original reference.
- Remove debug/construction-line-looking cyan corner strokes from HUD panels. Use the original reference's restrained beveled cyan edge trim instead.
- Restore command bar order and state to the original reference for selected/no-command mode: `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`. Do not replace `SELECT` with `SPECIAL` in this sample.
- Restore the original objective panel density, including Star Goals, unless Designer explicitly rejected that content.
- Rework the Build disabled treatment so it is intentional and secondary; do not let an oversized disabled Build block dominate the command rail.
- Rework the selected-squad world status treatment so it matches the original: shield icon plus segmented blue bar, no extra visible squad-name label unless the original reference includes it.
- Use imagegen for the visual mockup pass. Do not use deterministic image editing, scripted overlays, local compositing, manual marker drawing, or patched-on UI shapes for the flattened review PNGs.
- Markers must be regenerated as part of the cohesive imagegen scene and match the original mockup exactly in visual language: ring thickness, segmented gaps, glow strength, perspective ellipse, size, opacity, terrain integration, and placement under each soldier.
- Keep LayerPack metadata after the imagegen output is chosen, but the visual target itself must come from imagegen, not from deterministic image editing.
- Update `LayerPack/manifest.json`, `LayerPack/AssetPrep_M01_Sample.json`, and `LayerPack/SourceNotes.md`.
- Update `M01_StepByStepGameplay_SampleContactSheet_1920x1080.png` if the selected frame changes.
- The LayerPack source asset for the marker layers must point to the clean marker source or a new approved clean marker asset, not to a hand-painted flattened circle.
- Do not generate the rest of the sequence.
- Do not write runtime/import assets into `Assets/`.

Required fixes from Designer:

- Use one shared tactical camera plate and one shared zoom/framing lock for M01-01 and M01-02.
- Keep player and enemy squad screen scale consistent between M01-01 and M01-02.
- Normalize player and enemy infantry scale on the same isometric ground plane; do not shrink enemies because they are farther away in the composition.
- Preserve the same camera center; selection must not resize, reframe, or cut the world.
- M01-01 must read as no selection: no selected ring, no command mode, no move/attack/objective markers, neutral or disabled command controls, and M01-only objective text.
- M01-02 must read as selected but not in command mode: selection ring visible, selected squad state visible, command controls enabled, no Move/Attack/Stop/Hold active highlight.
- M01-02 must show the blue/cyan selected marker circle under each selected soldier, aligned to each soldier's feet on the isometric ground plane.
- Build must be hidden or clearly disabled for M01, using `MissionDoesNotAllowBuild` if a reason is shown.
- Clarify enemy red rings/health in both layer manifests as permanent affiliation layers or stateful markers; hide them if they are markers.
- Do not generate the rest of the sequence until this corrected sample is approved.

Required fixes from Gameplay audit:

- Provide one shared tactical camera plate/source with orthographic zoom, camera center, world bounds, and minimap viewport mapping.
- Provide player/enemy sprite sheet frame keys, facing, formation offsets, pivots, feet anchors, and contact-shadow rules.
- Include player/enemy infantry scale comparison notes proving they share the same isometric projection scale.
- Provide selection ring placement/pivot/scale/z-order and hidden/visible state rules.
- Update `M01-02_SquadSelected_layers.json` with per-soldier selected marker layers, source asset, pivots, z-order, alpha rule, and visible state.
- Prepare move/attack/objective/invalid marker sources and state rules for future frames, while keeping them hidden in M01-01/M01-02.
- Slice HUD chrome for objective/log/resource/squad tray/command/top controls/minimap, with transparent corners and 9-slice rules.
- Keep text, icons, counters, objective ticks, health values, button labels, and reason codes as separate runtime elements.
- Update `LayerPack/manifest.json`, per-frame layer manifests, and `LayerPack/SourceNotes.md` with existing reusable assets, missing Art assets, import settings, z-order, anchors, and Unity ownership paths.

Latest Gameplay audit says the rest of the sample is mostly acceptable for approval-sample review after this marker fix. Keep the correction tightly scoped.

Do not begin from the PM draft alone. Use the Designer report as source of truth for gameplay steps. Use the previous high-quality AAA imagegen locked targets as visual authority for UI and gameplay style:

- `Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png`
- `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
- `Design/VisualLock/POP-05_MissionResult/POP-05_MissionResult_Landscape_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_PlayerRifleSquad_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_EnemyPatrol_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Markers_SelectionMoveAttackEnemyObjectiveHover.png`

The package under `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/` remains reference material unless corrected by the Designer report and this PM rejection.

Create AAA target-lock review mockup images/contact sheets under:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/`

Create implementation layer metadata/source structure under:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/`

Put the result only in these structured project locations:

- Flattened review PNGs: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/`
- Sample contact sheet: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`
- Package manifest: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- Per-frame layer manifests: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/<FrameId>_layers.json`
- Source notes/prompt notes: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/SourceNotes.md`
- Handoff report: `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`

Do not place corrected results outside these paths. Do not write runtime/import assets into `Assets/` during this approval task.

Deliver:

- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`

The mockups must show every Design-approved step with the camera/framing, units, selection state, movement/attack/objective markers, HUD, minimap, ARIA, invalid-command recovery, and result state needed for user review.

Quality bar: match the approved AAA imagegen VisualLock targets above. Do not use schematic diagrams, deterministic renderer output, flat placeholder UI, programmer-facing layout art, debug labels, or contact-sheet-only substitutes as final target-lock imagery.

Layering bar: this must be layered like the existing VisualLock lockups, not just flattened PNGs. Use `Design/VisualLock/SCN-08_RTSBattleHUD/LayerPack/manifest.json` as the implementation pattern. For each sample frame, provide layer metadata for battlefield/camera plate, HUD chrome, panel fills, command buttons, unit sprites, selection rings, move/attack/objective markers, minimap content, ARIA panel, toasts/log rows, FX, and result popup as separate implementable layers. Include Unity object paths or intended prefab/object ownership, rects/anchors/resolution, z-order, alpha/transparent-corner rules, and reusable/stateful/dynamic/reference-only status.

Flattened PNGs are visual QA references only. They are not the implementation source.

First deliver only a tiny 1-2 sequence AAA sample set for approval. Do not produce the full mockup sequence yet.

Preferred first approval sample:

- `M01-01_TacticalStart_1920x1080.png`
- `M01-02_SquadSelected_1920x1080.png`
- one 1920x1080 contact sheet for those sample frames
- `LayerPack/manifest.json` plus per-frame layer breakdowns for those sample frames

Optional second approval sample only if PM/user needs combat/marker proof before approval:

- `M01-05_AttackPreview_1920x1080.png`
- one 1920x1080 contact sheet or sample board including the selected/tactical-start sample plus this attack-preview frame
- `LayerPack/manifest.json` plus per-frame layer breakdowns for the included frames

After PM/user says the 1-2 sequence sample is good, aligned, and approved, produce the remaining frames.

Before writing the corrected sample set, remove any rejected deterministic/placeholder images from `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/` so the folder contains only current approved-candidate Art output.

Do not make runtime implementation changes. Do not add the mockups to project runtime/assets as accepted visual lock until the user approves them.

# Art/Atlas Current Task

Date: 2026-05-14
Status: complete
Priority: P0 imagegen-only sample delivered; waiting for Designer/PM/user review

## Assignment

Art/Atlas delivered the latest imagegen-only two-frame approval sample and handoff report:

- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`

Designer is now the active next lane for alignment review. Art/Atlas waits for Designer/PM/user review before making more changes.

## Prior Assignment Context

Designer delivered the Design-owned M01 step-by-step gameplay spec:

`Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`

PM rejection: the Art output under `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/` is not approved. It reads as deterministic/assembled placeholder imagery rather than AAA imagegen target-lock mockups, and its UI/gameplay treatment does not match the previous high-quality locked targets. That pass has been removed and must not be restored, continued, polished, referenced, or presented for approval.

Designer review result: the corrected two-frame sample is directionally strong but not approved yet. Read `Design/AgentReports/2026-05-14_designer_m01-art-sample-alignment-review.md` and preserve the accepted direction.

Gameplay audit result: Gameplay completed an implementation-readiness audit in `Design/AgentReports/2026-05-14_gameplay_m01-mockup-asset-implementation-audit.md`. Apply its asset-preparation requirements together with Designer's visual alignment fixes. The next Art pass must make the sample implementable pixel-perfect later, not just visually acceptable.

Latest Gameplay audit result: Gameplay completed `Design/AgentReports/2026-05-14_gameplay_m01-corrected-sample-asset-audit.md`. The package is materially closer and the quality direction is approved by the user, but `M01-02_SquadSelected` still needs selected marker correction before Designer/PM/user implementation approval.

User scale feedback: the current mockup makes enemies look smaller than player units. In a true isometric mockup, units on the same ground plane must follow the same projection scale. Enemy infantry can differ by approved unit type/silhouette, but must not shrink because of inconsistent camera, zoom, distance, or composition.

User quality approval/fix note: the user approves the new sample quality direction, but the selected marker blue circle under each soldier is missing in `M01-02_SquadSelected`. Art/Atlas must fix the selected frame and its LayerPack metadata now. Do not change the approved quality direction, camera, world composition, HUD style, or scale except where required to add the selected markers correctly.

Latest PM/user rejection: the new marker pass is not approved. Art/Atlas added ugly blue circles under the soldiers that do not match the previous clean blue VisualLock mockup/marker style. This is a visual-quality rejection, not a Gameplay audit issue. Replace the ugly circles; do not keep, polish, or present that pass again.

PM visual check against the original reference image: the current `M01-02_SquadSelected_1920x1080.png` still does not match the original selected-state treatment. The under-foot rings are too weak/plain and do not read like the original clean neon segmented blue rings. The selected squad world status/health treatment above the soldiers is also missing: the original shows a blue shield icon plus segmented blue horizontal bar above the selected squad. Enemy soldiers in the original also use readable red above-head segmented health bars plus restrained red foot rings. Art/Atlas must restore these world-view combat readability elements in the same high-quality style.

PM HUD quality rejection: the current HUD is not acceptable compared with the original reference. The original HUD has premium beveled sci-fi panels, layered dark glass/metal depth, crisp cyan edge lighting, dense readable typography, integrated icons, polished command buttons, high-quality squad cards, and a detailed minimap frame. The current HUD reads flatter and less finished: panel bevels/inner shadows/edge glow are weaker, command buttons and Build state look crude, bottom squad cards lack the original visual richness, log/objective panels are simplified, and minimap framing/readability does not match the original AAA VisualLock finish. Art/Atlas must rebuild the HUD quality to the original reference, not only the marker overlays.

Latest PM comparison after Art marked complete: the new pass is closer on selected rings and enemy red bars, but it is still not approved. HUD quality and layout still do not match the original reference. The current HUD has bright cyan construction-line corner strokes and oversized outlines that read like debug/layout guides, not the original's polished beveled sci-fi frame. The command bar order/state is wrong versus the reference (`SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`); current output shows `STOP`, `HOLD`, `MOVE`, `ATTACK`, `SPECIAL` and an oversized crude Build block. The objective panel is simplified and missing the original Star Goals section. The selected squad world bar is too flat/rectangular, includes a visible label not present in the reference, and does not match the original segmented shield/bar polish. The minimap, squad cards, log panel, and command buttons remain below the original AAA HUD finish.

Latest PM marker rejection: the markers are still not 100% exactly matched to the original mockup. Stop deterministic/image-editing fixes for this visual pass. Do not use scripted compositing, manual shape overlays, pixel-patch editing, deterministic marker placement, or local image-editing workflows to patch the current PNG. Use the imagegen skill to generate a fresh AAA bitmap mockup from the original reference and locked VisualLock references. The deliverable must look like one cohesive imagegen target-lock frame, not a base screenshot with edited UI/marker patches.

Do not start from the PM draft alone. Use the Designer report as source of truth for gameplay steps. Use the previous high-quality AAA imagegen locked targets as the visual authority for UI and gameplay style.

Required visual authorities:

- `Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png`
- `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
- `Design/VisualLock/POP-05_MissionResult/POP-05_MissionResult_Landscape_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_PlayerRifleSquad_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_EnemyPatrol_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Markers_SelectionMoveAttackEnemyObjectiveHover.png`

Required marker source/style references:

- Clean runtime marker source: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Markers/selection_ring.png`
- Approved marker style sheet: `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Markers_SelectionMoveAttackEnemyObjectiveHover.png`
- Original selected-state reference provided by PM/user in thread: selected player squad has four clean blue/cyan segmented rings under feet, a blue shield icon, and a segmented blue horizontal status/health bar above the selected soldiers; enemy soldiers have readable red segmented health bars above heads and restrained red foot rings.
- Original HUD reference provided by PM/user in thread: premium RTS battle HUD with beveled dark glass/metal panels, cyan trim, readable objective/log/resource/top bar typography, polished squad cards, command buttons, Build treatment, minimap frame, and integrated icon language.

Create AAA target-lock review mockup images/contact sheets under:

`Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/`

Create the implementation layer package under:

`Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/`

## Required Output Locations

Keep the result structured in the existing project layout:

- Flattened review PNGs: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/`
- Layer manifests and source breakdowns: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/`
- Sample contact sheet: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`
- Package manifest: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- Per-frame layer manifests: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/<FrameId>_layers.json`
- Source notes or prompt notes: `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/SourceNotes.md`
- Art handoff report: `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`

Do not place corrected results outside these paths. Do not write implementation assets into `Assets/` until PM/user approval and a separate implementation/import task.

Required Art output:

- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`

## Required Designer Fixes

Revise only the first approval sample, with the current pass focused on replacing the rejected `M01-02_SquadSelected` selected markers:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-01_TacticalStart_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/SourceNotes.md`

Previously required fixes remain valid. For this pass, rework the selected marker/status treatment and HUD quality to match the original reference:

- Rebuild M01-01 and M01-02 from one shared tactical camera plate and one shared zoom/framing lock.
- Keep player and enemy squad screen scale consistent between M01-01 and M01-02; selection may add rings and selected HUD state, but must not resize or reframe the world.
- Normalize player and enemy infantry scale on the same isometric ground plane. Do not make enemies smaller than player infantry unless a documented unit-class scale rule says they should be smaller.
- Preserve the same camera center unless the Designer spec explicitly calls for camera movement. M01-02 does not.
- M01-01 must read as no selection: no selected ring, no command mode, no move/attack/objective markers, neutral or disabled command controls, and M01-only objective text.
- M01-02 must read as selected but not in any command mode: selection ring and selected squad state are visible, command controls are enabled, but Move/Attack/Stop/Hold are available and inactive.
- M01-02 must show the blue/cyan selected marker circle under each selected soldier, aligned to the feet on the isometric ground plane. Do not replace the per-soldier circles with only a group highlight or HUD selected-card state.
- The selected marker must match the clean approved VisualLock marker style: thin/segmented sci-fi blue/cyan ring, transparent center, soft controlled glow, correct isometric ellipse perspective, seated under feet, and integrated with terrain lighting.
- Do not draw crude filled blue circles, thick flat ellipses, high-saturation blobs, programmer debug rings, or deterministic placeholder overlays.
- Prefer compositing the existing clean `selection_ring.png` marker source under each soldier, transformed to the correct isometric perspective and opacity, or regenerate a visually matching marker from the approved marker reference. The result must read like the previous high-quality target-lock marker, not like a quick paint-over.
- M01-02 must include the selected-squad world status/health treatment from the original reference: blue shield icon plus segmented blue horizontal bar above the selected squad, anchored to the squad/leader area in world space. This must be a separate implementable layer, not baked into the unit body art.
- Enemy soldier readability must match the original reference: small red segmented above-head health bars and restrained red foot rings for enemies, handled as permanent unit-affiliation/readability layers in M01-01/M01-02, not attack-target command markers.
- Build must not appear as an available M01 command. Hide it or clearly disable it using the M01 reason `MissionDoesNotAllowBuild`.
- HUD quality must match the original reference and previous AAA VisualLock targets: beveled dark panels, layered glass/metal depth, crisp cyan trims, controlled glow, consistent icon language, readable typography, polished squad cards, premium command buttons, and a detailed readable minimap frame.
- Do not use flat placeholder HUD panels, crude generated text, weak bevels, mismatched button geometry, low-detail squad cards, simplified minimap chrome, or UI that looks lower quality than the original reference.
- Keep the original reference's HUD structure and density: objective panel top-left, resource/top bar, pause/settings top-right, log panel bottom-left, squad cards bottom-left, command bar bottom-center, and minimap bottom-right unless the Designer spec explicitly requires a state change.
- Remove debug/construction-line-looking cyan corner strokes from HUD panels. Use the original reference's restrained beveled cyan edge trim instead.
- Restore command bar order and state to the original reference for selected/no-command mode: `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`. Do not replace `SELECT` with `SPECIAL` in this sample.
- Restore the original objective panel density, including Star Goals, unless Designer explicitly rejected that content.
- Rework the Build disabled treatment so it is intentional and secondary; do not let an oversized disabled Build block dominate the command rail.
- Rework the selected-squad world status treatment so it matches the original: shield icon plus segmented blue bar, no extra visible squad-name label unless the original reference includes it.
- Use imagegen for the visual mockup pass. Do not use deterministic image editing, scripted overlays, local compositing, manual marker drawing, or patched-on UI shapes for the flattened review PNGs.
- Markers must be regenerated as part of the cohesive imagegen scene and match the original mockup exactly in visual language: ring thickness, segmented gaps, glow strength, perspective ellipse, size, opacity, terrain integration, and placement under each soldier.
- Clarify in both layer manifests whether enemy red rings/health are permanent unit affiliation layers or stateful world markers. If they are markers, hide them in M01-01 and M01-02.
- Do not generate remaining frames until this corrected sample is approved by Designer/PM/user.

## Required Gameplay Asset-Prep Feedback

Read `Design/AgentReports/2026-05-14_gameplay_m01-mockup-asset-implementation-audit.md` and update the sample package so Gameplay can later implement `M01-01_TacticalStart` pixel-perfect from approved assets. The corrected Art package must include:

- one approved tactical camera plate/source with orthographic zoom, camera center, world bounds, and minimap viewport mapping shared by M01-01 and M01-02
- player rifle squad sprite sheet/frame keys/facing/formation offsets/pivots/feet anchors/contact shadows matching the corrected sample
- enemy patrol sprite sheet/frame keys/facing/formation offsets/pivots/feet anchors/contact shadows matching the corrected sample
- player/enemy infantry scale comparison notes proving both read at the same isometric projection scale
- explicit decision whether enemy red rings/health are permanent affiliation layers or stateful markers
- selection ring source with group/per-soldier placement, pivot, scale, z-order, and hidden/visible state rules
- corrected per-soldier selected marker layer in `M01-02_SquadSelected_layers.json`, including source asset, rects/anchors, pivots, z-order, alpha rule, and visible state for each soldier
- move, attack, objective, and invalid marker prep kept hidden for M01-01/M01-02 but declared for future states
- sliced HUD chrome for objective panel, log/threat panel, resource bar, squad tray/cards, command bar/buttons, top controls, and minimap panel
- separate runtime text/icons/numbers/objective ticks/health values/button labels; do not bake them into panel or button art
- disabled/hidden Build state for M01 with canonical reason `MissionDoesNotAllowBuild` if a reason appears
- layer manifests that map each layer to real or intended Unity owner paths, rects/anchors/z-order, alpha/transparent-corner rules, reusable/stateful/dynamic/reference-only status, and import/slicing notes
- `LayerPack/SourceNotes.md` updated with existing source assets that can be reused and missing Art assets that must be generated before Gameplay implementation

Also read `Design/AgentReports/2026-05-14_gameplay_m01-corrected-sample-asset-audit.md`. For this focused pass, the required fix is:

- Replace the rejected ugly blue circles with visible clean blue/cyan selected marker rings under each selected soldier in `M01-02_SquadSelected_1920x1080.png`.
- Add the original-style blue shield icon and segmented blue selected-squad world status/health bar above the selected soldiers in `M01-02_SquadSelected_1920x1080.png`.
- Ensure enemy above-head red segmented health bars and restrained red foot rings remain readable and match the original reference.
- Rebuild the HUD in both `M01-01_TacticalStart_1920x1080.png` and `M01-02_SquadSelected_1920x1080.png` so it matches the original reference quality, style, and layout density.
- Ensure objective/log panels, top resource bar, squad cards, command buttons, Build disabled state, and minimap frame are all at the same AAA target-lock quality as the original.
- Correct the command bar contents/order to `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD` for this selected-state sample.
- Restore the original objective panel density with Star Goals.
- Replace the current bright cyan debug-like HUD corner marks with polished integrated bevel/trim.
- Regenerate the flattened review frames with imagegen using the original reference as the primary visual target. Do not patch the current PNG with deterministic tools.
- Keep LayerPack metadata after the imagegen output is chosen, but the visual target itself must come from imagegen, not from deterministic image editing.
- Replace the single group-only selected-marker representation with four explicit per-soldier selected marker child layers or entries in `LayerPack/Frames/M01-02_SquadSelected_layers.json`.
- Each selected-marker layer must include source asset, rect, foot anchor, pivot, scale, z-order, alpha rule, and visible state.
- Add explicit separate LayerPack entries for the selected-squad world shield icon and segmented blue status/health bar, including source asset, rect, world anchor, pivot, scale, z-order, alpha rule, and visible state.
- Add or correct explicit enemy affiliation/readability entries for red above-head health bars and red foot rings, including source asset, anchors, z-order, and the rule that these are permanent affiliation/readability elements, not attack markers.
- Add or correct LayerPack entries for all HUD chrome groups that changed: objective panel, log/threat panel, top resource bar, squad cards, command buttons, Build disabled state, and minimap frame. Each must include source asset/slice notes, rect, anchors, z-order, alpha/transparent-corner rule, state, and Unity owner path.
- Update `LayerPack/manifest.json`, `LayerPack/AssetPrep_M01_Sample.json`, and `LayerPack/SourceNotes.md` so Gameplay can implement selected markers without guessing.
- Update the sample contact sheet if the selected frame changes.
- The LayerPack source asset for these layers should point to the clean marker source or a new approved clean marker asset, not to a hand-painted flattened circle.
- Do not generate the rest of the sequence.
- Do not write runtime/import assets into `Assets/`.

The corrected mockup package must include:

- one image or contact-sheet cell for each Design-approved gameplay step
- clear camera/framing, units, selection state, movement/attack/objective markers, HUD, minimap, ARIA, invalid-command recovery, and result state
- frame ids matching the Designer spec
- AAA target-lock visual quality matching the approved M01 VisualLock targets, not schematic, wireframe, deterministic renderer, flat HUD diagram, placeholder composition, or programmer-facing layout art
- generated or painted bitmap mockups with cohesive lighting, material detail, unit integration, tactical readability, and polished UI presentation
- UI and gameplay composition aligned to the previous high-quality AAA imagegen locked targets listed above
- layered implementation-ready source structure like the existing VisualLock lockups, especially `Design/VisualLock/SCN-08_RTSBattleHUD/LayerPack/manifest.json`
- per-frame layer manifests that identify battlefield/camera plate, HUD chrome, panel fills, command buttons, unit sprites, selection rings, move/attack/objective markers, minimap content, ARIA panel, toasts/log rows, FX, and result popup as separate implementable layers
- explicit Unity object paths or intended prefab/object ownership for every UI/HUD layer, plus rects/anchors/resolution, z-order, alpha/transparent-corner rules, and whether the layer is reusable, stateful, dynamic, or visual-reference-only
- flattened PNGs only as visual QA references; do not treat the flattened mockup as the implementation asset source
- no visible frame labels or debug labels inside the cinematic/gameplay image unless the Designer spec requires that label as actual UI
- no runtime implementation changes
- no import into project runtime/assets until user approval
- source notes for any gaps or unresolved Design questions

Before producing the full frame set, produce only a tiny approval sample first: one or two gameplay sequences, not the whole mockup set.

Preferred first approval sample:

- `M01-01_TacticalStart_1920x1080.png`
- `M01-02_SquadSelected_1920x1080.png`
- one 1920x1080 contact sheet for those sample frames
- matching `LayerPack/manifest.json` entries and per-frame layer breakdowns for those sample frames

Optional second approval sample only if needed to prove combat/marker alignment:

- `M01-05_AttackPreview_1920x1080.png`
- one 1920x1080 contact sheet or sample board including the selected/tactical-start sample plus this attack-preview frame
- matching `LayerPack/manifest.json` entries and per-frame layer breakdowns for the included frames

These sample mockups are review artifacts for user approval first. Do not generate the remaining frames until PM/user confirms the sample is good, aligned, and approved.

Rejected-output rule:
If rejected deterministic/placeholder images already exist in `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/`, delete or replace them before writing the corrected sample set. Do not leave stale rejected frames beside the new layered AAA samples.

The current `M01-02_SquadSelected_1920x1080.png` marker pass with ugly blue circles is rejected. Replace it and the contact sheet with clean VisualLock-matched marker output before reporting ready for review.

## Current Source

Source lane:
Designer complete

Source report:
`Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`

Next approval:
User approval of the corrected 1-2 sequence AAA Art sample before full-frame production, Gameplay implementation, or project import.

Implementation readiness gate:
The sample is not ready for approval unless it includes both flattened AAA target images and a layered source/manifest package sufficient for Gameplay/UI to implement the target without guessing. Full sequence production starts only after user approval of this small sample.

Blocked lanes:
Gameplay and QA/HCI.

# Lane

Gameplay

# Task

Second implementation-readiness audit for the corrected M01 two-frame sample.

# Files changed

- Added `Design/AgentReports/2026-05-14_gameplay_m01-corrected-sample-asset-audit.md`.

# Contracts touched

- Report only. No source docs, task files, Unity assets, prefabs, runtime scripts, imports, or git operations were changed.
- Reviewed implementation ownership contracts:
  - `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
  - `Assets/Game/Scripts/Systems/MissionRuntimeTerrainSurfaceRendererSystem.cs`
  - `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
  - `Assets/Game/Scripts/TacticalMaps/TacticalMapRuntimeLoader.cs`
  - `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`

# User-visible behavior

None. This audit does not implement runtime behavior, import assets, or route QA/HCI.

# Handoff assessment

- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`: needs Art fixes before implementation approval. The corrected package improves camera lock, scale metadata, state metadata, Build state, and asset-prep metadata, but selected marker treatment is still not implementation-ready.
- `Design/AgentReports/2026-05-14_gameplay_m01-mockup-asset-implementation-audit.md`: accepted as first Gameplay asset-prep feedback.
- `Design/AgentReports/2026-05-14_pm_art-atlas-combined-design-gameplay-feedback.md`: accepted.
- `Design/AgentReports/2026-05-14_pm_art-atlas-unit-scale-feedback.md`: accepted; the corrected package now includes a same-scale infantry rule.
- `Design/AgentReports/2026-05-14_pm_gameplay-audit-not-blocker-correction.md`: accepted; missing selected markers are handled below as an audit finding, not as a blocker-only stop.

# Audit decision

Decision: needs Art/Atlas fixes before Designer/PM/user implementation approval.

The corrected package is materially closer to implementation readiness. Camera/zoom consistency, unit scale metadata, M01-01 no-selection state, M01-02 selected-but-no-command-mode state, Build disabled state, enemy affiliation rule, and asset-prep metadata are now mostly implementable as an approval sample. The remaining selected-marker issue is a required Art fix before Gameplay can later implement pixel-perfect selected-state behavior.

# Findings

1. Needs fix: M01-02 selected markers are not specified as separate per-soldier runtime layers.
   - User/PM specifically requires one blue/cyan selected marker circle under each selected soldier.
   - Current `M01-02_SquadSelected_layers.json` has a single `Screen_MatchOverlay/WorldMarkers/SelectionRing` layer with rect `[405,625,230,105]` and a text `placementRule` saying per-soldier rings.
   - That is not enough for implementation readiness. The LayerPack must include four explicit marker child layers or entries, each with source asset, rect, foot anchor, pivot, scale, z-order, alpha rule, and visible state.
   - Required Art fix: add per-soldier selected marker circles to `M01-02_SquadSelected_1920x1080.png` and represent them as explicit per-soldier layers in the LayerPack.

2. Accepted with implementation caveat: shared camera/zoom consistency is now described clearly.
   - `CameraLock_M01_DefaultStart.json` defines one lock for both M01-01 and M01-02.
   - Shared rects match between frames: player `[405,570,230,190]`, enemy `[1345,235,250,190]`, minimap panel `[1545,715,365,325]`, viewport `[1612,815,105,85]`.
   - Caveat: runtime still needs a clean no-HUD/no-unit camera plate or runtime terrain capture matching this lock.

3. Accepted with implementation caveat: player/enemy infantry scale is now normalized in metadata.
   - `AssetPrep_M01_Sample.json` states player and enemy infantry share the same isometric projection scale.
   - Both frame manifests repeat the same-scale rule and formation-spread explanation.
   - Caveat: final implementation still needs confirmed frame keys, pivots, formation offsets, and contact-shadow split against approved runtime sprites.

4. Accepted: M01-01 no-selection state is implementable as a state target.
   - M01-01 declares `selection:none`, `commandMode:none`, `worldMarkersVisible:false`, assistant closed, Build disabled, and no visible selection/move/attack/objective/invalid markers.
   - Squad tray is neutral/unselected and command controls are neutral-disabled.

5. Accepted except selected-marker issue: M01-02 selected-but-no-command-mode state is implementable.
   - M01-02 declares selected squad, `commandMode:none`, same camera, command controls enabled/readable, no active command highlight, and move/attack/objective/invalid markers hidden.
   - Remaining gap is selected marker implementation detail: it needs per-soldier visible layers rather than a single group layer.

6. Accepted: Build disabled/hidden state is clear enough for implementation.
   - Both frame manifests include `buildState: disabled` and `buildDisabledReason: MissionDoesNotAllowBuild`.
   - `AssetPrep_M01_Sample.json` repeats `disabled:MissionDoesNotAllowBuild` for M01-01 and M01-02.
   - Runtime owner is `Screen_MatchOverlay/BuildButton` plus existing HUD/command reason handling.

7. Accepted: enemy affiliation/health treatment is clear enough for this sample.
   - `manifest.json` declares enemy red/health treatment as a permanent restrained unit-affiliation layer, not a world marker.
   - Attack target marker remains hidden until later attack states.
   - Runtime implementation will still need the actual overlay split if it is not baked into the enemy unit sprite frames.

8. Needs future source preparation before runtime implementation: clean runtime source layers are still missing.
   - Art/Atlas correctly states flattened PNGs are visual references only.
   - `AssetPrep_M01_Sample.json` lists missing clean camera plate/runtime capture, native minimap texture and viewport transform, final sliced HUD chrome, separate TMP/icons/counters/labels/reason codes, and final unit frame data.
   - These are acceptable gaps for approval-sample review, but they must be closed before Gameplay implements `M01-01_TacticalStart` pixel-perfect.

# M01-01_TacticalStart readiness

M01-01 is ready for Designer/PM/user approval as a visual/state target, but not ready for runtime implementation.

Implementation-ready after approval requires:

- clean tactical camera plate or runtime terrain capture matching `CameraLock_M01_DefaultStart.json`
- native minimap source and viewport transform from the same world bounds
- final sliced HUD chrome for objective, threat/log, resource, squad tray, command bar, Build button, top controls, and minimap
- separate runtime text/icons/counters/objective ticks/health values/button labels/reason codes
- confirmed player/enemy sprite frame keys, feet pivots, formation offsets, contact-shadow split, and import settings

# M01-02_SquadSelected readiness

M01-02 is not ready for implementation approval because selected marker treatment is incomplete.

Required before approval:

- visible blue/cyan selected marker circle under each selected soldier in the flattened sample
- four explicit per-soldier selected marker layer entries in `M01-02_SquadSelected_layers.json`
- per-marker source asset, rect, feet anchor, pivot, scale, z-order, alpha rule, and visible state
- matching updates to `manifest.json`, `AssetPrep_M01_Sample.json`, and `SourceNotes.md`

# Remaining missing Art assets or metadata before Gameplay can implement M01-01_TacticalStart

- Clean no-HUD/no-unit tactical camera plate or runtime terrain capture matching the approved sample lock.
- Runtime minimap texture and viewport transform derived from the same camera/world bounds.
- Sliced HUD chrome and import settings for objective panel, threat/log panel, resource bar, squad tray/cards, command bar/buttons, Build button, top controls, and minimap.
- Separate runtime text/icons/counters/objective ticks/health values/button labels/reason codes.
- Final unit frame keys, pivots, formation offsets, feet anchors, contact-shadow split, and import settings for player and enemy infantry.
- If enemy affiliation/health is separate from sprite frames, actual overlay assets and z-order/anchor rules.
- For M01-02 selected state, explicit per-soldier selection marker layers and visible circles.

# Validation run

- Read `Design/AgentTasks/gameplay_heartbeat.md`.
- Read `Design/AgentTasks/gameplay_current.md`.
- Checked recent `Design/AgentReports/` handoffs.
- Read:
  - `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`
  - `Design/AgentReports/2026-05-14_gameplay_m01-mockup-asset-implementation-audit.md`
  - `Design/AgentReports/2026-05-14_pm_art-atlas-combined-design-gameplay-feedback.md`
  - `Design/AgentReports/2026-05-14_pm_art-atlas-unit-scale-feedback.md`
  - `Design/AgentReports/2026-05-14_pm_gameplay-audit-not-blocker-correction.md`
- Read LayerPack:
  - `manifest.json`
  - `CameraLock_M01_DefaultStart.json`
  - `AssetPrep_M01_Sample.json`
  - `Frames/M01-01_TacticalStart_layers.json`
  - `Frames/M01-02_SquadSelected_layers.json`
  - `SourceNotes.md`
- Verified sample PNG dimensions with `sips`: all three review images are `1920x1080`.
- Parsed all LayerPack JSON files with `python3 -m json.tool`.
- Rechecked existing runtime ownership paths and source asset dimensions for tactical plate, unit atlases, and selection marker.

# Validation result

Needs Art/Atlas fixes before implementation approval.

The package is strong enough to route back to Art/Atlas with a focused selected-marker correction and source-layer completion list. Runtime implementation and QA/HCI remain blocked.

# Known gaps

- No runtime implementation was performed.
- No Unity import validation was run because the task is audit-only and project import is not approved.
- Visual inspection of the missing per-soldier selected marker issue is based on user/PM feedback and the current LayerPack structure, which still exposes one selection ring layer instead of explicit per-soldier layers.

# Cross-lane impacts

- Art/Atlas owns the selected marker correction and any LayerPack/source-note updates.
- Designer/PM/user should not approve implementation until the selected-marker fix is present and reviewed.
- Gameplay can implement only `M01-01_TacticalStart` after Designer/PM/user approval and after clean runtime source layers are available.
- QA/HCI remains held until a runtime implementation exists.

# Next recommended task

Art/Atlas should revise only the selected marker treatment: restore per-soldier blue/cyan selected circles in `M01-02_SquadSelected`, add explicit per-soldier selection marker layers to the LayerPack, and update manifest/asset-prep/source notes. After that, Designer/PM/user can review the corrected sample for approval; Gameplay should not start runtime implementation before that approval.

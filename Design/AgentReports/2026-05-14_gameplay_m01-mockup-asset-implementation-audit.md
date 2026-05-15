# Lane

Gameplay

# Task

P0 implementation-readiness audit for the M01 step-by-step mockup assets before Art revision.

Gameplay assessed the current Art/Atlas sample, Designer review, PM routing, LayerPack, and existing Unity assets. This is an audit only. No runtime implementation is approved yet, and Gameplay remains blocked from runtime work until Art/Atlas submits a corrected sample and Designer/PM/user approve it.

# Handoff assessment

- `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`: accepted as the current gameplay/art behavior spec for M01 mockups.
- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`: needs fixes. The files exist and the LayerPack structure is useful, but Designer/PM rejected approval because M01-01 and M01-02 do not preserve one locked tactical camera/zoom.
- `Design/AgentReports/2026-05-14_designer_m01-art-sample-alignment-review.md`: accepted. Gameplay agrees implementation must remain held.
- `Design/AgentReports/2026-05-14_pm_designer-art-sample-review-routing.md`: accepted. Art/Atlas owns the next action.

# Files changed

- Added `Design/AgentReports/2026-05-14_gameplay_m01-mockup-asset-implementation-audit.md`.

# Contracts touched

- Report only. No source docs, task files, Unity assets, prefabs, runtime scripts, ECS systems, scenes, imports, or git operations were changed.
- Existing implementation contracts reviewed:
  - `Assets/Game/Scripts/TacticalMaps/Chapter01TacticalAtlasContract.cs`
  - `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
  - `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
  - `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`

# User-visible behavior

None. This audit changes no game behavior and imports no assets.

# Implementation gate

Runtime implementation is blocked. The current `M01-01_TacticalStart` and `M01-02_SquadSelected` samples are not implementation targets yet because camera, scale, command state, objective scope, Build availability, and enemy ring/health state rules need Art/Atlas correction and Designer/PM/user approval.

# Exact assets needed for M01-01_TacticalStart

M01-01 must be a no-selection tactical start at 1920x1080 reference, with `camera.default_start`, stable orthographic isometric scale, objective `Destroy hostile patrol`, no selection ring, no command mode, no move/attack/objective markers, assistant closed, and neutral/disabled command controls.

- Shared tactical camera plate/map source: one approved tactical ground source for `camera.default_start`, separate from units, markers, minimap viewport, and HUD. Existing source candidate is `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/TacticalMaps/m01_tactical_plate_a_pot_2048x1024.png` at 2048x1024, resolved by `Chapter01M01SpriteAssetResolver.M01ProductionTacticalPlateAAssetId`. Missing: corrected approved camera lock metadata proving the 1920x1080 mockup is rendered from the same map, orthographic size, camera center, and world bounds used in runtime.
- Player squad idle sprites: four-soldier `unit.player.rifle_squad_01` idle state, alive, facing into lane, no selection overlay. Existing source candidate is `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas_v2.png` at 4096x1792 plus per-frame V2 folders. Missing: explicit approved frame keys/facing/formation offsets matching the corrected mockup and feet/pivot anchors.
- Enemy patrol idle sprites: `unit.enemy.patrol_01` alive, distant, not targeted, not defeated. Existing source candidate is `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/enemy_patrol_animation_atlas_v2.png` at 4096x1792 plus per-frame V2 folders. Missing: Art state rule for restrained enemy affiliation/health, and explicit frame keys/facing/formation offsets matching the corrected mockup.
- HUD objective panel: top-left panel chrome, 9-slice background, separate icon/text children, text `Destroy hostile patrol` only. Existing owner is `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab` under `ObjectivePanel`. Missing: approved sliced panel art matching corrected frame and no baked text.
- Threat/log panel: no mission-breaking extra objectives, mission start/log row optional. Existing HUD owner is `Screen_MatchOverlay`; current LayerPack alternates `ThreatLogPanel` and `LogPanel`. Missing: one canonical object name and sliced background/row art.
- Resource bar and top-right controls: static/reusable SCN-08 HUD chrome with counters/icons as separate runtime children. Existing owner is `Screen_MatchOverlay`. Missing: corrected sliced chrome if the current sample shape is final.
- Squad tray/cards: if visible in no-selection, it must read neutral/unselected, not selected or command-ready. Existing owner is `Screen_MatchOverlay/SquadTray`. Missing: neutral card background, disabled/empty selected state rules, separate portraits/health/text.
- Command bar/buttons: neutral/disabled command bar until selection, Build hidden or disabled with `MissionDoesNotAllowBuild` if visible. Existing owner is `Screen_MatchOverlay/CommandBar`. Missing: disabled button art and explicit Build visibility rule.
- Minimap panel: panel chrome, clipped minimap content, start viewport rectangle. Existing owner is `Screen_MatchOverlay/MiniMapPanel`. Missing: minimap source texture derived from the same tactical map/world bounds and viewport mapping from camera.default_start.
- Marker reserve assets: selection/move/attack/objective/invalid markers are not visible in M01-01. Existing marker PNGs exist under `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Markers/` at 256x256. Missing: corrected approval that they are hidden for M01-01 and not baked into world or HUD.

# Exact assets needed for M01-02_SquadSelected

M01-02 is preparation only until the corrected sample is approved. It must reuse the exact M01-01 tactical camera plate, zoom, world positions, unit scale, HUD layout baseline, and minimap viewport. The only intended gameplay delta is selected friendly squad state with command controls enabled but no Move/Attack mode active.

- Shared M01-01 world/map/HUD base: all static/reusable M01-01 assets must carry forward unchanged unless Designer explicitly approves a UI state delta. Current sample violates this: player rect changes from `[440,570,180,150]` to `[500,600,210,170]`, enemy rect changes from `[1420,310,190,150]` to `[1250,225,240,170]`, and HUD panel rects also shift.
- Selection rings: cyan ground-plane rings attached to player squad feet. Existing source candidate is `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Markers/selection_ring.png` at 256x256 and contract sprite id `marker.selection.ring`. Missing: final corrected ring footprint, per-soldier/group placement rule, animation/pulse rule, pivot `(0.5, 0.5)` confirmation, and world scale matching the approved corrected frame.
- Selected squad card/tray state: selected card highlight, portraits/health/text separate runtime children. Existing owner is `Screen_MatchOverlay/SquadTray`. Missing: approved selected and unselected card art, exact selected card rect, and neutral inactive treatment for other squad cards.
- Command bar selected-ready state: Select, Move, Attack, Stop, Hold enabled/readable but none highlighted as active command mode. Existing owner is `Screen_MatchOverlay/CommandBar`. Missing: normal/hover/disabled/selected-ready button art and a clear rule that Move highlight is not active in M01-02.
- Enemy affiliation/health: enemy remains alive and not targeted. Missing: Art/Atlas must declare whether red rings/health are permanent unit affiliation overlays or stateful world markers. If stateful markers, they must be hidden in M01-01 and M01-02.
- Minimap viewport: unchanged from M01-01. Missing: locked mapping proving M01-02 uses the same camera viewport and no camera recenter.

# Layer family audit

| Layer family | Current source asset if exists | Missing asset or contract gap | Required Art/Atlas preparation | Target Unity owner | Rect/anchor/z-order notes | Alpha/slicing/import | Runtime status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Battlefield camera plate | `m01_tactical_plate_a_pot_2048x1024.png`, plus plate B/C variants | Approved corrected camera plate/zoom metadata and 1920 viewport mapping | Rebuild M01-01/M01-02 from one shared plate and one orthographic lock | ECS world renderer via `MissionRuntimeTerrainSurfaceRendererSystem`/`TacticalMapRuntimeLoader` and `Chapter01M01SpriteAssetResolver` | LayerPack rect `[0,0,1920,1080]`, z0, stretch; current rect is fine but projection proof is missing | Opaque world/map texture; no baked UI/units/markers | Dynamic world |
| Player rifle squad | `player_rifle_squad_animation_atlas_v2.png` and V2 per-frame folders | Corrected frame keys, facing, scale, formation offsets, feet pivots | Provide approved idle/run/aim/fire/damaged/death frame list for M01, with M01-01/M01-02 idle frame lock | ECS unit presentation through `MissionRuntimeAtlasQuadPresentationSystem`; LayerPack conceptual owner `Assets/Game/Prefabs/Units/Player/RifleSquad01.prefab` does not currently exist | Current LayerPack rects are rejected as implementation targets until fixed; z20 | Transparent sprite atlas, Sprite mode Multiple, pivot/feet anchor explicit | Dynamic |
| Enemy patrol | `enemy_patrol_animation_atlas_v2.png` and V2 per-frame folders | Corrected frame keys, facing, scale, formation offsets, enemy affiliation/health rule | Provide alive idle/aim/fire/damaged/death/destroyed sequence; death/destroyed only after M01-06C | ECS unit presentation; LayerPack conceptual owner `Assets/Game/Prefabs/Units/Enemy/Patrol01.prefab` does not currently exist | Current LayerPack rects are rejected until fixed; z20 | Transparent sprite atlas, Sprite mode Multiple, feet pivot explicit | Dynamic |
| Selection ring | `Markers/selection_ring.png` 256x256; contract id `marker.selection.ring` | Corrected M01-02 footprint and animation rule | Provide group/per-soldier ring placement with feet anchors; hidden in M01-01 | ECS marker rendering in `MissionRuntimeAtlasQuadPresentationSystem`; existing HUD prefab has `SelectionRing` | M01-02 current rect `[488,633,230,118]` rejected until camera scale fixed; z30 | Transparent outside ring strokes; no filled blob | Stateful |
| Move marker | `Markers/move_destination.png` 256x256; contract id `marker.move.destination` | Not needed visible for M01-01/02; future M01-03 prep only | Keep hidden in M01-01/02; provide ground-plane pulse states for later frames | ECS target marker rendering | Hidden rect `[0,0,0,0]`; z32 | Transparent amber/isometric marker | Dynamic later |
| Attack marker | `Markers/attack_target.png` 256x256; contract id `marker.attack.target` | Not needed visible for M01-01/02; future M01-05 prep only | Keep hidden in M01-01/02; provide restrained target pulse for alive enemy | ECS target marker rendering | Hidden rect `[0,0,0,0]`; z32 | Transparent red ring/stroke | Dynamic later |
| Objective/invalid markers | `objective_focus.png`, `invalid_blocked.png` 256x256 | Contract has objective id; invalid marker is present as art but not in atlas contract defaults | Keep hidden in M01-01/02; add explicit ids/rules before later frames | ECS/world marker layer plus HUD bridge | Hidden rect `[0,0,0,0]`; objective z31, invalid z35 | Transparent ground FX; no screen-space square | Dynamic/stateful later |
| Objective panel | `Screen_MatchOverlay.prefab` contains `ObjectivePanel` | Corrected M01-only content and final sliced chrome | Provide 9-slice panel background, separate icons/text, complete/incomplete row states | `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab` | Current rect changes `[10,12,330,188]` to `[10,12,410,220]`; revised sample must lock intended state deltas | Transparent beveled corners; TMP text separate | Reusable/stateful |
| Threat/log panel | `Screen_MatchOverlay.prefab`; LayerPack names `ThreatLogPanel`/`LogPanel` | Canonical object name and sliced row art | Provide start log and selected log states, no baked text | `Screen_MatchOverlay` | Current rect changes from left-middle to lower-left; revised sample must decide stable HUD layout | 9-slice panel, row highlights separate | Stateful |
| Resource/top controls | `Screen_MatchOverlay.prefab` | Final SCN-08 matching sliced chrome if sample is approved | Provide reusable frames/icons/counters separated | `Screen_MatchOverlay` | Top anchored; current resource x changes 940 to 950 | Transparent rails/buttons; icons/text separate | Reusable |
| Squad tray/cards | `Screen_MatchOverlay.prefab` contains `SquadTray` | Neutral no-selection state and selected card state | Provide card backgrounds, portraits, health bars, selected highlight as separate layers | `Screen_MatchOverlay/SquadTray` | Current rect changes `[12,790,730,260]` to `[12,835,680,230]`; corrected camera sample should not look like a layout jump unless approved | 9-slice tray/card, transparent corners, text separate | Stateful |
| Command bar/buttons | `Screen_MatchOverlay.prefab` contains `CommandBar` | Disabled/neutral vs selected-ready states; Build unavailable rule | Provide button states for Select/Move/Attack/Stop/Hold; Build hidden or disabled with `MissionDoesNotAllowBuild` | `Screen_MatchOverlay/CommandBar` and `BattleHudGameplayBridge` | Current rect changes `[760,880,690,180]` to `[780,875,700,190]`; M01-02 must not show active Move | Button sprites 9-slice where scalable; icons/text separate | Stateful |
| Minimap | `Screen_MatchOverlay.prefab` contains `MiniMapPanel`; source map is implicit | Minimap source and viewport mapping | Provide minimap texture/source split, viewport rectangle/tap ripple as separate layers | `Screen_MatchOverlay/MiniMapPanel` | Rect `[1510,700,390,350]`, z105; viewport must match camera.default_start | Clipped map content inside transparent frame | Dynamic |
| ARIA/result/combat FX | Assistant prefabs exist under UI components; `MissionResultPopup.prefab` exists under UI/Popups, LayerPack references `Screen_MissionResultPopup` | Not visible in M01-01/02; future frame mapping mismatch for result popup path | Keep hidden and do not route into this implementation slice | `Screen_MatchOverlay`, `Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab`, ECS/VFX owner later | Hidden z150/300/40 | Transparent panels/particle alpha; text separate | Later stateful/dynamic |

# Character sprite sheet requirements

- Player squad and enemy patrol must each provide approved atlas entries for idle, run, aim, fire, damaged/hit, death, and destroyed where applicable. Living M01-01/M01-02 may only use idle/ready; death/destroyed is forbidden before patrol defeat.
- Facing must cover at least NE, NW, SE, SW to match current V2 asset structure, with frame keys declared for the corrected M01 camera direction.
- Formation layout must be explicit: per-soldier local offsets, group footprint, and whether selection rings render per soldier or group-wide. Current ECS code uses four rifle-squad soldier offsets, but Art must approve the visual arrangement against the corrected frame.
- Scale must remain identical between M01-01 and M01-02. Selection rings and HUD selection state cannot resize either squad.
- Pivot/feet anchor must be declared per frame. Current atlas contract expects unit pivot `(0.5, 0.15)`; Art must confirm or provide corrected pivots for the V2 frames.
- Contact shadows must be either included consistently in the transparent unit frames or delivered as separate shadow sprites with owner, alpha, and sorting rules. Do not mix baked and runtime shadows inconsistently between player and enemy.
- Health/affiliation overlays must be separate from unit body sprites unless Art declares them permanent baked unit decals. Enemy red rings/health in M01-01 conflict with `worldMarkersVisible:false` unless they are permanent affiliation layers.

# Battlefield plate and map requirements

- The corrected sample needs one shared tactical camera plate and one shared camera lock for M01-01 and M01-02: orthographic size, camera center, world bounds, projection matrix assumptions, and minimap viewport transform.
- The tactical map source must be split from UI, markers, units, objective pulses, minimap viewport, and tutorial overlays. The flattened 1920x1080 PNG is a review target only and must not be imported as implementation source.
- The runtime map path should continue through the M01 production tactical map resolver unless Art/Atlas creates a new approved package. Target path for approved corrected imports should be under the M01 production/approved 2DISO Chapter01 asset family, not the VisualLock review folder.
- Minimap must be generated from the same map/world source as the camera plate or have documented source alignment. It needs a viewport rectangle for `camera.default_start` and later tap/focus states.
- World-to-screen anchors must be provided for player spawn, enemy patrol, selection rings, target markers, objective focus, and minimap viewport. Current LayerPack screen rects are audit evidence only, not approved runtime coordinates.

# Marker requirements

- Selection ring: visible only in M01-02, cyan/blue, ground-aligned, transparent strokes, no filled blob, anchored to squad feet. Provide idle/pulse animation if desired, but default selected state should be readable without an active command.
- Enemy affiliation/health: decide whether this is permanent unit affiliation or stateful marker UI. If stateful world marker, hide it in M01-01 and M01-02. If permanent, declare its layer family, owner, z-order, and health state assets.
- Move marker: hidden in M01-01/M01-02, ground-aligned for later M01-03/M01-04 only, with preview vs accepted states separated.
- Attack marker: hidden in M01-01/M01-02, restrained hostile target ring for later M01-05/M01-06 only, not covering the enemy silhouette.
- Objective marker: hidden in M01-01/M01-02, ground-plane pulse for objective focus/completion later.
- Invalid marker: hidden in M01-01/M01-02, brief warning pulse for canonical rejection states later.

# UI requirements

- Objective panel: M01-only objective `Destroy hostile patrol`; do not include `Secure the intersection`, `Hold the forward position`, or any other unapproved objective.
- Threat/log panel: optional mission start and selection rows only; text must be runtime TMP, not baked in art.
- Resource bar: separate frame, icons, counters, and labels.
- Squad tray/cards: provide no-selection neutral state and selected state. If visible in M01-01, it must not look selected.
- Command bar/buttons: M01-01 neutral/disabled until selection; M01-02 enabled/readable but no active Move/Attack command mode. Build must be hidden or disabled with canonical `MissionDoesNotAllowBuild`.
- Top controls: separate button chrome and icons.
- Minimap panel: separate frame, map, viewport, unit/enemy pips if any, and tap/focus FX.
- ARIA and result popup: not visible in M01-01/M01-02; leave as future state prep only.

# Slicing and layering requirements

- All HUD panels and scalable buttons must be delivered as 9-slice-capable sprites with transparent outside corners.
- Text, numbers, icons, objective ticks, health values, button labels, and reason codes must be separate runtime elements.
- Unit and marker sprites must use transparent alpha and correct Unity import pivots. Atlases/sheets should use Sprite mode Multiple; single panel/chrome art can use Single with borders configured for 9-slice.
- Avoid baking UI or marker shadows into the tactical map. Avoid baking health bars into unit sprites unless explicitly approved as permanent unit decals.
- Preserve z-order families from LayerPack: world z0, units z20, selection z30, objective z31, move/attack z32, invalid z35, combat z40, HUD z100-110, ARIA z150, result z300.

# Exact Art/Atlas feedback to combine with Designer feedback

1. Rebuild M01-01 and M01-02 from one shared tactical camera plate, one orthographic zoom, one camera center, and one minimap viewport mapping.
2. Keep player and enemy unit screen size/scale stable between M01-01 and M01-02; selection state may add rings and HUD changes only.
3. Do not treat current LayerPack unit rects as implementation targets until corrected: they currently prove a camera/scale mismatch.
4. Provide corrected LayerPack metadata with frame keys/facing/pivots for player and enemy V2 unit sprites, not just screen-space rectangles.
5. Declare enemy red ring/health as either permanent affiliation layers or stateful world markers; hide them in M01-01/M01-02 if they are markers.
6. Deliver sliced HUD chrome for objective, log, resource, squad tray/cards, command bar/buttons, top controls, and minimap, with all text/icons separated.
7. M01-01 command state must be neutral/disabled; M01-02 command state must be enabled/readable but no active Move/Attack highlight.
8. Hide or disable Build in M01, with `MissionDoesNotAllowBuild` as the only visible canonical reason if a disabled reason appears.
9. Objective content must be only `Destroy hostile patrol` unless PM expands M01.
10. Keep flattened PNGs as approval references only; provide runtime-suitable source assets under the approved asset family before Gameplay implementation.

# Validation run

- Read `Design/AgentTasks/gameplay_heartbeat.md`.
- Read `Design/AgentTasks/gameplay_current.md`.
- Checked recent `Design/AgentReports/` handoffs.
- Read Designer spec, Designer sample review, Art/Atlas handoff, and PM routing reports named in the current task.
- Read `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`.
- Read both per-frame LayerPack manifests.
- Verified sample PNG dimensions with `sips`.
- Scanned relevant existing assets under `Assets/Game/Art/`, `Assets/Game/Prefabs/`, and owner scripts under `Assets/Game/Scripts/`.
- Checked dimensions for key existing M01 production map, marker, and unit atlas assets.

# Validation result

Audit complete. Current Art/Atlas sample is not accepted for implementation. Gameplay handoff is ready for PM/Designer/Art/Atlas review as an asset-preparation report only.

# Known gaps

- No corrected approved M01-01/M01-02 sample exists yet.
- No runtime import or implementation was performed by design.
- Current LayerPack target prefab paths for `Assets/Game/Prefabs/Units/Player/RifleSquad01.prefab`, `Assets/Game/Prefabs/Units/Enemy/Patrol01.prefab`, and `Assets/Game/Prefabs/Markers/*.prefab` do not match confirmed existing prefab paths; current runtime ownership appears to be ECS presentation plus `Screen_MatchOverlay`, not those conceptual prefabs.
- Exact pixel-perfect rects must come from the corrected approved LayerPack, not the current rejected rects.
- Final UI slice names and atlas placement remain Art/UI ownership after the corrected mockup is approved.

# Cross-lane impacts

- Art/Atlas must revise the two-frame sample and LayerPack using Designer feedback plus this Gameplay asset-prep list.
- Designer/PM/user must approve the corrected sample before Gameplay starts runtime work.
- UI will need to validate sliced HUD assets and `Screen_MatchOverlay` object mapping once approved.
- QA/HCI remains held until there is an approved runtime implementation target.

# Next recommended task

Art/Atlas should produce a corrected `M01-01_TacticalStart` and `M01-02_SquadSelected` sample with one locked camera/zoom and the asset/source metadata listed above. After Designer/PM/user approval, Gameplay should implement only `M01-01_TacticalStart` from the approved LayerPack before expanding to the rest of the sequence.

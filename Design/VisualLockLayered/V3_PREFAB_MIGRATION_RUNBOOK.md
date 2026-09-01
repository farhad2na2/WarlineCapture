# V3 Prefab Migration Runbook

Status: active execution plan; shared foundation implemented; popup migration
completed through review-frozen Intel Reveal Iteration 2 as of 2026-09-01.

This runbook turns the accepted V3 target locks and the shared-art strategy into
the current Unity Canvas prefabs without creating screen-local duplicate art or
breaking runtime bindings.

Related sources:

- [`V3_SCREEN_INVENTORY.md`](V3_SCREEN_INVENTORY.md)
- [`V3_SHARED_LAYERED_ART_ATLAS_STRATEGY.md`](V3_SHARED_LAYERED_ART_ATLAS_STRATEGY.md)
- [`UI_Imagegen_Target_Mockup_To_Layered_Unity_Workflow.md`](../UI_Imagegen_Target_Mockup_To_Layered_Unity_Workflow.md)
- [`canvas_shared_chrome_asset_map.md`](_CanvasTargetLockVisualMatch/shared/canvas_shared_chrome_asset_map.md)

## Live Migration Ledger

- Settings: Iteration 19 is the current review candidate. It preserves the
  directional gradients and one 3 px border system, keeps the outer modal,
  tab rail, and active page as three non-overlapping frames, and scales the
  popup to 84% of the live canvas height with a 76% width cap. The isolated QA
  path now uses the same Expand aspect contract as the Menu scene. The 16:9
  and 20:9 captures are under `POP-06_Settings/iterations/iteration_19/`.
  Final post-fix Play Mode confirmation is pending only because the host Mac
  auto-locked; the candidate is not accepted until the user explicitly
  confirms it.
- Splash Loading: Iteration 4 is the current review candidate. Its single
  environment plate uses aspect-fill cover cropping, while a centered 1672x941
  chrome reference keeps the logo, status chips, and footer stable at 16:9 and
  20:9. The rank mark now uses the target's diamond motif instead of a
  five-point star. Both immutable captures are under
  `SCN-01_SplashLoading/iterations/iteration_04/`; the candidate is not accepted
  until the user explicitly confirms it.
- First Launch: Iteration 5 is the current review candidate for Language
  Choice, Comic Playback, Commander Identity, and ARIA Guidance. The real Menu
  scene was exercised in Play Mode at exact 1920x1080 and 4800x2160 capture
  sizes. That integration pass exposed a QA reviewer bar over three states; it
  was suppressed in the capture route and the invalid frames were not frozen.
  The language selector now has circular globe geometry, identity uses a
  procedural warning triangle plus shared V3 rank/support marks, every
  selection frame follows the 3 px border contract, and the aspect-preserved
  ARIA portrait has the target's cyan diagnostic surround. Live evidence and
  sharp deterministic comparison renders are under
  `SCN-00_FirstLaunch/iterations/iteration_05/`. It is not accepted until the
  user explicitly confirms it.
- Main Menu: Iteration 12 is the current review candidate. Each stable
  commander ID now selects one cohesive baked full-screen scene containing the
  commander, environment, tactical table, contact shadows, lighting, and
  occlusion. `field_commander_01` uses
  `SCN02_FieldCommander_01_Scene_V3.png`; the failed commander-free background
  plus transparent cutout experiment remains provenance only and is not
  referenced by the prefab. ARIA remains an independent assistant portrait.
  Campaign, Operations, and Skirmish each use one independent scene plate.
  Their five target-derived mode/footer icons are packed exactly once in
  `UI_V3_MainMenuIcons_01`; the three scene cards use
  `UI_V3_MainMenuScenes_01`, and ARIA uses `UI_V3_Assistants_01`. Live frames,
  equal-width borders, directional gradients, resource symbols, telemetry,
  labels, and buttons remain procedural/shared. The background now uses
  aspect-fill cover cropping instead of stretching; all five shell regions
  reassert their target-space position after the live shell finishes mounting;
  the FIELD COMMANDER rail is wider with explicit right insets; and the FPS
  diagnostic is opt-in. Actual Play Mode was checked with the Game view set to
  exact 1920x1080 and 4800x2160 presets; the immutable fitted-viewport evidence
  is under `SCN-02_MainMenuV3/iterations/iteration_12/`. It is not accepted
  until the user explicitly confirms it.
- Commander Profile: Iteration 2 is the current review candidate. The obsolete
  ornate TargetLockV01 structure has been replaced by the final V3 composition,
  using the canonical baked commander/environment scene with a tighter masked
  profile crop. All five tabs are readable, unsupported font glyphs were
  replaced by procedural vector marks, the header/footer and side panels use
  visible gradients with one 3 px border contract, and the 1920x1080 and
  4800x2160 Play Mode captures contain no panel overlap. Evidence is under
  `SCN-03_CommanderProfile/iterations/iteration_02/`. Its mockup silhouettes now
  replace the temporary icon substitutions and are packed once in
  `UI_V3_CommanderIcons_01`. It is not accepted until the user explicitly
  confirms it.
- Mission Briefing: Iteration 1 is the current review candidate. The legacy
  ornate SCN-06 composition has been replaced by the final V3 hierarchy with
  independent 3 px frames and procedural gradients. Its forward-post scene and
  enemy-officer portrait are standalone aspect-preserved art plates with no
  baked UI; all small symbols reuse the shared V3 atlases. The live M02 route
  keeps the target chapter and briefing copy, complete reward labels, and a
  non-overlapping Back / Loadout / Deploy footer. Actual Play Mode evidence at
  1920x1080 and 4800x2160 is under
  `SCN-06_MissionBriefing/iterations/iteration_01/`. It is not accepted until
  the user explicitly confirms it.
- Match HUD: Iteration 4 is the current review candidate. The real selection,
  squad tray, minimap, tactical feedback, settings/pause, and command bindings
  now mount inside a centered 1672x941 V3 composition. Board is in the selected
  unit action grid; Support and Build complete the eight-command footer rail;
  the expanded aspect-preserved ARIA panel owns the live minimap. All repeated
  chrome uses procedural gradients and one 3 px border contract. Match-only
  icons are packed once in `UI_V3_MatchIcons_01.spriteatlas`, while shared
  symbols remain in their existing atlases. Evidence is under
  `SCN-08_MatchHudV3/iterations/iteration_04/`. It is not accepted until the
  user explicitly confirms it.
- Match HUD — Transport Passengers: Iteration 5 is the current review candidate
  for the live transport drawer state. Opening the real passenger chip compacts
  the transport panel and reveals a target-aligned 449x618 drawer with ten live
  capacity slots, four pooled passenger rows, existing unit/Dalia portraits,
  severity-colored health bars, and functional Exit, Exit All, Close, Board,
  and Rope Drop actions. The transport portrait fills without stretching;
  ready feedback now uses the fresh cyan V3 info state rather than the static
  red error presentation. Deterministic and actual Menu-to-Match Play Mode
  evidence at 1920x1080 and 4800x2160 is under
  `SCN-08_MatchHudV3/iterations/iteration_05/`. The focused interaction suite
  passed four checks. It is review-frozen, not user-accepted.
- Match HUD — POP-03 Build Placement: Iteration 3 supersedes the earlier
  lower-right confirmation-bar candidate. The real placement footer now spans
  the target width; Rotate, Cancel, and Place Building remain live actions.
  Invalid placement disables the action and swaps ARIA for the reusable
  right-pinned footprint-validity/minimap panel; closing restores ARIA and the
  threat cue. Existing building/minimap art and shared V3 icons are reused, all
  directional gradient surfaces use the constant 3 px frame contract, and no
  screen-local art copies were added. Four focused state/restore checks and
  exact 1920x1080 / 4800x2160 Play Mode captures for valid and invalid states
  pass. Evidence is under
  `POP-03_BuildPlacement/iterations/iteration_03/`. It is review-frozen, not
  user-accepted.
- Match HUD — Tutorial Presentation: Iteration 3 is the current review
  candidate. POP-13 retains its narration, RTL, highlight, Show Me, Do It, and
  close semantics while replacing the old lower-left card with the final V3
  top-right presentation. It uses the new aspect-preserved ARIA portrait,
  procedural gradient buttons, constant 3 px borders, a non-blocking cyan guide
  connected to the live Rifle Squad and Move controls, and a reversible compact
  header variant that prevents Settings/Pause overlap. The 23-test behavior
  suite and three focused V3 checks pass; deterministic and Play Mode evidence
  at both required ratios is under
  `PREFAB-06_TutorialPresentation/iterations/iteration_03/`. It is review-frozen,
  not user-accepted.
- Match HUD — ARIA Command Assistant: Iteration 3 is the current review
  candidate. The runtime no longer forces the old 2460x1510 gold modal; the
  normal POP-13 state is a responsive 510x690 top-right V3 panel. It uses the
  shared non-stretched ARIA portrait and shared target/integrity/range icons,
  twelve procedural gradient surfaces, and one 3 px frame-border contract.
  Input is captured only inside the visible panel, the header compacts and
  restores cleanly, and the voice switch persists through `SettingsService`.
  Three focused V3 checks, the 23-test assistant behavior suite, the Tutorial
  regression suite, and exact 1920x1080 / 4800x2160 Play Mode captures pass.
  Evidence is under
  `POP-13_ARIACommandAssistant/iterations/iteration_03/`. It is review-frozen,
  not user-accepted.
- Match HUD — Assistant Takeover: Iteration 2 is the current review candidate.
  The `ARIA CONTROL` state now composes a centered responsive modal inside the
  shared POP-13 prefab while preserving the embedded right-side ARIA tutorial
  and minimap panel. The ARIA portrait is aspect-preserved, Current Intent uses
  live recommendation and goal bindings, Resume Command and Stop ARIA both
  invoke the stop-control action, and the target `STOP ARIA` label is retained.
  Procedural directional gradients and one 3 px border contract are used
  throughout. Three focused checks, the 23-test assistant behavior suite, the
  POP-13 and Tutorial regression suites, and exact 1920x1080 / 4800x2160 Play
  Mode captures pass. Evidence is under
  `POP-10_AssistantTakeover/iterations/iteration_02/`. It is review-frozen, not
  user-accepted.
- Match HUD — Threat Alert: Iteration 2 is the current review candidate for
  both incoming-alert and route-preview states. A single responsive prefab now
  replaces the obsolete stacked presentation and temporarily suppresses the
  legacy Match HUD threat banner while visible. The centered alert uses the
  existing aspect-preserved convoy art; route preview removes the scrim,
  compacts the summary, exposes the route overlay, and preserves the top-right
  ARIA/minimap panel plus full-width command footer. Shared V3 Match symbols,
  directional gradients, and one 3 px border contract are used throughout.
  Three focused checks, the existing threat behavior regression, and exact
  1920x1080 / 4800x2160 Play Mode captures for both states pass. Evidence is
  under `POP-01_ThreatAlert/iterations/iteration_02/`. It is review-frozen, not
  user-accepted.
- Confirm Raid: Iteration 2 is the current review candidate. The obsolete
  raster-frame structure and placeholder thumbnail were replaced by the final
  V3 1008x688 modal. The target panel now reuses the existing Sahrin district
  map with aspect-fill cropping, metrics and meters use shared V3/Operations
  symbols, and every chrome/action surface uses a directional procedural
  gradient with one 3 px border contract. The first implementation's footer
  overlap was rejected and corrected before evidence was frozen. Three focused
  layout/interaction checks and exact 1920x1080 / 4800x2160 Play Mode captures
  on the real Operations route pass. Evidence is under
  `POP-02_ConfirmRaid/iterations/iteration_02/`. It is review-frozen, not
  user-accepted.
- Intel Reveal: Iteration 2 is the current review candidate. The inherited
  top-cropped prefab and resource-placeholder thumbnails were replaced by the
  final V3 1100x756 modal with three evidence cards, a complete progress row,
  and non-overlapping Close / View Intel actions. Supply Ledger, Cargo
  Manifest, and Radio Intercept art are packed once in
  `POP08_EvidenceAtlas_V3.png` and displayed through separate aspect-preserved
  UV viewports; no individual duplicate card textures were added. Every framed
  surface uses the shared 3 px border contract and procedural directional
  gradients. Three focused layout/atlas/interaction checks and exact
  1920x1080 / 4800x2160 Play Mode captures on the Operations route pass.
  Evidence is under `POP-08_IntelReveal/iterations/iteration_02/`. It is
  review-frozen, not user-accepted.
- Build Drawer: Iteration 1 is the current review candidate. The full V3 popup
  retains its live catalog, selection, placement, production, queue, and input
  bindings while replacing the old structure with one centered 1672x941
  composition. All unit/building cards and detail views reuse existing catalog
  portraits; no four-card replacement art was created. Directional gradients,
  independent 3 px borders, shared-atlas symbols, ready/disabled states, and
  aspect-preserving card art are frozen under
  `SCN-09_BuildDrawer/iterations/iteration_01/`. The popup passed 25 focused
  behavior checks and actual Menu-scene Play Mode capture at 1920x1080 and
  4800x2160. The Menu validation route does not load the battlefield backdrop,
  so final in-match background confirmation and explicit user acceptance are
  still pending.
- Unit Command Wheel: Iteration 1 is the current review candidate for the base
  and Attack-targeting states. The selected-unit portrait opens the live wheel;
  Attack enters the targeting presentation; the six radial sectors use visible
  directional gradients and constant 3 px outer borders. A separate Black Hawk
  detail card reuses the existing helicopter portrait and fresh shared V3 stat
  icons. The targeting state moves that card down-left, moves the wheel right,
  and anchors a narrower targeting rail at the far edge without collision. Live
  and deterministic evidence at 1920x1080 and 4800x2160 is under
  `SCN-10_UnitCommandWheel/iterations/iteration_01/`. The focused interaction
  suite passed three checks. It is review-frozen, not user-accepted.
- Mission Result: Iteration 4 is the current review candidate for both Victory
  and Defeat. The popup uses one centered 1672x941 composition, aspect-preserved
  result art, procedural directional gradients, and one 3 px border contract.
  Its runtime-bound action changes per state instead of duplicating the popup.
  Evidence at both required ratios and both states is under
  `POP-05_MissionResult/iterations/iteration_04/`. It is not accepted until the
  user explicitly confirms it.
- End-of-Day Report: Iteration 3 is the current review candidate. The report
  retains the generic popup runtime frame while replacing the old static body
  with the V3 map, daily summary, progression track, and two live actions. The
  map and environment remain aspect-preserved content plates; graph and chrome
  are procedural, with one 3 px border contract. Evidence is under
  `POP-06_EndOfDayReport/iterations/iteration_03/`. It is not accepted until the
  user explicitly confirms it.
- Reward Unlock: Iteration 2 is the current review candidate. The newly made
  Ranger Squad illustration is an isolated squad-plus-blueprint content layer,
  while title, description, rewards, frame, gradients, borders, and Continue
  action remain live Unity UI. The square art plate preserves aspect ratio and
  all four reward cards use canonical shared icons without screen-local copies.
  Evidence is under `POP-04_RewardUnlock/iterations/iteration_02/`. It is not
  accepted until the user explicitly confirms it.

An iteration number names evidence, not progress. A new number is valid only
when the posted immutable capture visibly contains the claimed change.

## Current Implementation Facts

- The canonical reference set contains 46 final target states. They are not 46
  independent prefabs; several states compose one shared prefab with different
  data, visibility, tint, or overlays.
- Current UI prefabs are under `Assets/Game/Prefabs/UI`.
- Many current prefabs are generated or repaired by editor builders under
  `Assets/Game/Scripts/Editor`.
- Builders still hard-code V1/V2 generated sprite paths and inline colors.
- Rebuilding after a manual prefab-only restyle would overwrite those edits.
- `SCN02_MainMenuContent`, `SCN03_CommanderProfileContent`,
  `SCN08_MatchHudContent`, and `SCN19_ArmoryContent` already expose named shell
  sections through `UIShellContentSectionsView`.
- `MainMenuLeftNavButton.prefab` and `PopupFrameView.prefab` are existing shared
  component starting points.
- Some validators currently reject sprites outside a screen-local folder. They
  must accept the canonical V3 shared root before shared sprites are assigned.

## Execution Rule

Update the canonical art catalog, theme, shared component factory, and editor
builders first. Regenerate prefabs through Unity after the source changes
compile. Do not edit serialized prefab YAML by hand.

Shared art is not visual acceptance. Every migrated prefab must reproduce the
composition, hierarchy, proportions, spacing, state presentation, and color
hierarchy of its own approved V3 final target. Reusing an old prefab layout with
new shared sprites is a failed migration.

For every screen iteration:

1. Post the approved V3 target lock.
2. Copy the generated capture to a unique immutable evidence path such as
   `iterations/iteration_14/settings_match_16x9.png`. Never post a mutable
   `/private/tmp` capture path or reuse an earlier iteration filename.
3. Open and inspect the exact immutable file that will be posted.
4. Post that capture at the matching aspect and identify visible mismatches
   explicitly.
5. Correct the builder/prefab and capture again under a new iteration path.
6. Repeat until the implementation matches; only then mark the prefab passed.

Every player-facing screen must also pass a live Play Mode gate at both
1920x1080 and 4800x2160. A prefab-only or offscreen capture is comparison
evidence, not a substitute for the live gate. The Game view must be fit to the
window before recording evidence, and any offscreen QA canvas must use the same
CanvasScaler `Expand` policy as the runtime shell.

## Phase 1: Freeze The V3 Foundation

### 1.1 Create the active asset registry

Create `Design/VisualLockLayered/V3_SHARED_ASSET_REGISTRY.csv` with these fields:

`asset_id`, `role`, `silhouette`, `state_strategy`, `tintable`, `source_master`,
`runtime_png`, `unity_guid`, `atlas`, `max_display_px`, `slice_lbrt`, `used_by`,
`sha256`, `perceptual_hash`, `status`.

Register existing canonical resource icons and the first four calibration
assets. Do not register copies or screen aliases as separate assets.

### 1.2 Generate the four calibration masters

Generate and inspect these assets before requesting the full pack:

1. `ui_core_panel_9s`
2. `ui_core_button_9s`
3. `ui_core_focus_overlay_9s`
4. `ui_icon_attack`

Use the Main Menu, Settings, and Match HUD V3 targets as style references. The
assets must have transparent backgrounds, hard 90-degree corners, neutral
tintable construction, crisp antialiased edges, no text, and no baked state
color. Save selected masters under:

`Assets/Game/Art/UI/V3Shared/SourceMasters/Calibration/`

Save runtime-cleaned sprites under:

`Assets/Game/Art/UI/V3Shared/Sprites/Core/`

The source master is retained for provenance. The runtime sprite is clamped to
alpha bounds and is the only file referenced by prefabs.

### 1.3 Create the shared atlas foundation

Create:

- `Assets/Game/Art/UI/V3Shared/Atlases/UI_V3_CoreChrome_01.spriteatlas`
- `Assets/Game/Art/UI/V3Shared/Atlases/UI_V3_CoreIcons_01.spriteatlas`

Initial atlas policy:

- maximum page `1024x1024`
- padding 8
- rotation off
- tight packing off for sliced chrome
- mipmaps off
- alpha transparency enabled
- bilinear filtering
- uncompressed during calibration

Do not atlas full-screen backgrounds, comics, maps, or hero illustrations.

### 1.4 Create canonical Unity access points

Add:

- `V3UiArtCatalog`: one canonical sprite reference per asset ID
- `V3UiTheme`: the shared neutral and semantic palette
- `V3UiPrefabFactory`: editor-only helpers for panels, buttons, images, text,
  state transitions, and sliced-image setup

Builders load the catalog once and use semantic fields. They must not load V3
sprites through screen-specific filename strings.

The first palette exposes:

- Canvas
- Surface
- SurfaceRaised
- LinePrimary
- TextPrimary
- TextMuted
- Cyan
- Blue
- Green
- Amber
- OrangeRed
- Violet
- Disabled
- Dimmer

State handling uses the shared neutral sprite plus theme colors. Do not create
separate bitmap files for normal, hover, pressed, selected, or disabled when the
silhouette is unchanged.

## Phase 2: Shared Component Proof

Update or create these reusable component prefabs through builder code:

1. `MainMenuLeftNavButton.prefab`
2. `PopupFrameView.prefab`
3. `V3Panel.prefab`
4. `V3Button.prefab`
5. `V3Card.prefab`
6. `V3Chip.prefab`
7. `V3Progress.prefab`
8. `V3Toggle.prefab`
9. `V3Slider.prefab`
10. shared menu header and footer modules

The components own only presentation and stable component slots. Screen
prefabs continue to own their runtime view scripts and data bindings.

### Shared component acceptance gate

- All four calibration sprites import successfully.
- Sliced images preserve corner/stroke thickness at minimum, typical, and
  maximum sizes.
- Icons remain crisp at 32, 48, 64, and 96 display pixels.
- Button normal, hover, pressed, selected, and disabled states use the same
  canonical base and focus overlay.
- Prefabs contain no reference to copied calibration sprites.
- Atlas-packed and standalone visual crops match.

## Phase 3: Settings Pilot

Use `SCN_SettingsPopup.prefab` as the first complete V3 migration because it is
shared by menu and match and exercises most core components.

### Builder changes

Update `SettingsPopupPrefabBuilder` to:

- load `V3UiArtCatalog` and `V3UiTheme`
- remove the `MainMenuBrightCommand/Sprites` root dependency
- use shared panel, button, focus, chip, progress, toggle, and slider assets
- use live TMP text for every label and value
- use one base button sprite plus theme/state transitions
- preserve `SettingsPopupView` bindings and the existing menu/match wiring
- preserve lifecycle controls and accessibility behavior
- preserve the existing visual QA capture entry point

### Settings visual target

`POP-06_Settings/reference/POP-06_SettingsV3_Final_Target.png`

### Settings acceptance gate

- Rebuild completes with the required pass marker.
- Menu and match contexts open and close correctly.
- Audio sliders and toggles remain bound and interactive.
- Gameplay, Video, and Accessibility tabs still switch content.
- Apply and Reset remain functional.
- No missing serialized references.
- 16:9 menu, 20:9 menu, 16:9 match, and 20:9 match captures exist.
- Panel borders, tabs, slider tracks, toggles, close button, and footer actions
  remain sharp at capture resolution.
- Selected tabs, ON toggles, Reset, and Apply reproduce the directional
  gradients visible in the lock; flat semantic-color fills are rejected.
- Repeated panel/button borders have one consistent visible stroke weight.
  Duplicate nested frame layers are rejected, and gradient fills may not cover
  the outer modal chrome.
- The prefab uses only canonical V3 shared UI art plus approved content assets.

## Phase 4: Migration Order

Migrate in this order after the Settings pilot passes.

Active continuation priority requested on 2026-08-31:

1. First Launch and Comic screens
2. Remaining Match screens and overlays
3. End Match screens

Within each group, finish the target-lock comparison and both live aspect gates
before moving to the next screen.

### 4.1 Shared shell and menu routes

1. Splash/loading
2. Main menu shared header, navigation, cards, and footer
3. Commander profile
4. Campaign chapter and mission selection
5. Mission briefing
6. Operations dashboard
7. Skirmish setup
8. Armory

Preserve the named shell content sections and route lifecycle components.

### 4.2 Modal family

1. Pause
2. Threat alert and route preview
3. Confirm raid
4. Build placement and validity state
5. Reward unlock
6. Mission result victory and defeat
7. Intel reveal
8. Ability/upgrade detail
9. Resource exchange
10. End-of-day report
11. ARIA command assistant and takeover

Every state pair reuses the same chrome. Differences are data, tint, emblem, or
modular overlays unless a silhouette change is proven.

### 4.3 Match and runtime-bound tools

1. Match HUD shared chrome and command bar
2. Transport passenger drawer
3. Tactical feedback banner
4. Build drawer and disabled state
5. Placement confirmation bar
6. Full tactical map
7. Unit command wheel and targeting state
8. Tutorial presentation/highlight

This phase runs last because the Match HUD has the most serialized sprite and
runtime-view references. Preserve every protected child name and serialized
field before visual changes.

### 4.4 New or incomplete route surfaces

Build missing dedicated content surfaces only after the shared system is stable:

- loadout/squad prep
- district detail actions
- store
- inbox
- events
- ranking
- command feed

These surfaces must start from the shared prefabs and catalog rather than
creating another screen-local visual system.

## Builder And Validator Migration Rules

- Replace screen-local sprite roots with catalog lookups.
- Expand validators that currently whitelist only a screen folder so they allow
  `Assets/Game/Art/UI/V3Shared/` and approved canonical content roots.
- Preserve required runtime components, serialized fields, child names, and
  routing objects.
- Do not rebuild unrelated screens in the same execute method.
- Give each focused rebuild a unique pass marker.
- Capture the live prefab, not the flattened target PNG.
- Compare the target and implementation before marking a screen migrated.
- Update `used_by` in the registry whenever a prefab begins using an asset.

## Validation Contract

When this project is already open, use Unity CLI + Pipeline and do not launch a
second Editor:

```bash
unity status --project-path /Users/farhad/Projects/WarlineCapture
unity command recompile --project-path /Users/farhad/Projects/WarlineCapture
unity command menu --path "Game/UI/Rebuild Settings Popups" \
  --project-path /Users/farhad/Projects/WarlineCapture
unity command menu --path "Game/UI/Capture Settings Popup QA" \
  --project-path /Users/farhad/Projects/WarlineCapture
```

Poll `recompile_status` when the first command returns `compiling`, and read the
Pipeline console after each build for its pass marker and new errors.

When no Editor owns the project, macOS execute methods and validation runs use
only `Tools/CI/invoke_unity_macos.sh`. Never invoke the Unity executable directly
and never add `-batchmode`.

Focused rebuild example:

```bash
Tools/CI/invoke_unity_macos.sh --timeout 600 \
  --log /private/tmp/warline-v3-settings-build.log -- \
  -quit -executeMethod Game.Editor.SettingsPopupPrefabBuilder.Build
```

Focused capture example:

```bash
Tools/CI/invoke_unity_macos.sh --timeout 600 \
  --log /private/tmp/warline-v3-settings-capture.log -- \
  -quit -executeMethod Game.Editor.SettingsPopupPrefabBuilder.CaptureVisualQa
```

Every wrapper run requires:

- explicit timeout
- explicit log path
- zero exit code
- expected pass marker
- no project lock
- no compile errors or missing-script/missing-reference errors

Do not start a wrapper validation while another Unity process owns this project.
Do not terminate an Editor to make room. Prefer the connected live Editor
through Unity CLI when one is available.

## Settings Pilot Execution Record

The initial 2026-08-30 technical pilot produced:

- 4 imported calibration sprites with alpha, mipmaps disabled, and the recorded
  9-slice borders
- `UI_V3_CoreChrome_01.spriteatlas` with 3 packables
- `UI_V3_CoreIcons_01.spriteatlas` with the attack icon, canonical Settings icon,
  and 6 canonical resource icons
- `V3UiArtCatalog.asset` and `V3UiTheme.asset`
- a SHA-256 validation gate that rejects duplicate file content across every
  catalogued atlas input
- one color-driven button state path plus `V3UiSelectableFocusView`
- a first regenerated `SCN_SettingsPopup.prefab` containing 97 Images and 26
  Buttons but only 4 unique sprite asset paths
- successful menu and match captures at 1920x1080 and 2400x1080

That first prefab passed technical checks but failed the V3 target comparison:
it retained the legacy four-panel grid. It is not accepted as V3. The corrected
pilot must use the target's four-button vertical tab rail, one visible page at a
time, large controls, and attached amber/green footer actions. Structural
validation now rejects the legacy grid.

## One-Go Execution Checkpoints

This work may run in one interactive Codex task. No automation is required.
Use checkpoints so a later continuation can resume safely:

- [x] Runbook saved
- [x] Asset registry created
- [x] Four calibration masters generated and inspected
- [x] Runtime calibration sprites imported by Unity
- [x] Core chrome/icon atlases created
- [x] V3 art catalog created
- [x] V3 theme created
- [x] Shared prefab factory created
- [x] Shared selectable focus component integrated
- [ ] Shared component proof rebuilt
- [x] Settings builder migrated to target structure
- [x] Settings prefab regenerated; iteration 19 is the current visual-match candidate
- [x] Compile and focused validation passed after target-structure correction
- [x] Four corrected Settings QA captures compared to the target
- [ ] Settings iteration 19 explicitly accepted by the user
- [x] Registry and runbook updated with results
- [x] Commander Profile iteration 2 captured at 16:9 and 20:9 with the V3 mockup icon set; awaiting explicit acceptance
- [x] Campaign Chapter Select and Mission Select iteration 2 captured at 16:9 and 20:9 with ARIA aspect preservation enforced; awaiting explicit acceptance
- [x] Campaign icon sources packed once in `UI_V3_CampaignIcons_01.spriteatlas`
- [x] Mission Briefing iteration 1 captured at 16:9 and 20:9; awaiting explicit acceptance
- [x] Loadout / Squad Prep route, prefab, and runtime capture support implemented
- [x] Loadout / Squad Prep iteration 3 captured at 16:9 and 20:9; awaiting explicit acceptance
- [x] Loadout equipment art packed once in `UI_V3_EquipmentIcons_01.spriteatlas`
- [x] Operations Dashboard iteration 3 captured at true 16:9 and 20:9 Game View sizes; awaiting explicit acceptance
- [x] Operations Dashboard icons packed once in `UI_V3_OperationsIcons_01.spriteatlas`
- [x] District Detail Actions route and prefab implemented; five Operations district markers open it
- [x] District Detail Actions iteration 3 captured at true 16:9 and 20:9 Game View sizes; awaiting explicit acceptance
- [x] District Detail action and threat icons packed through the existing shared V3 atlases without screen-local duplicates
- [x] V3 route captures now select the matching Game View preset before rendering
- [x] First Launch language, commander identity, comic playback, and ARIA guidance iteration 4 captured at 16:9 and 20:9; awaiting explicit acceptance
- [x] First Launch backgrounds, commander portraits, comic art, and ARIA preserve aspect ratio at both validated sizes
- [x] First Launch controls use procedural gradients, constant 3px borders, and shared-atlas target-type icons
- [x] Match HUD iteration 4 captured at 16:9 and 20:9 with eight runtime-bound commands and five squads; awaiting explicit acceptance
- [x] Match HUD ARIA, selected portrait, minimap, and gameplay background preserve aspect ratio at both validated sizes
- [x] Match-only icons packed once in `UI_V3_MatchIcons_01.spriteatlas`; shared icons remain deduplicated
- [x] Build Drawer iteration 1 captured in ready, disabled, and actual Play Mode states at 16:9 and 20:9; awaiting explicit acceptance
- [x] Build Drawer reuses existing unit/building catalog portraits and shared V3 atlases; no screen-local four-card art was added
- [x] Build Drawer retains placement, production, queue cancel/clear, and popup input behavior; focused validation passes 25 tests
- [x] Mission Result iteration 4 captured for victory and defeat at 16:9 and 20:9; awaiting explicit acceptance
- [x] Mission Result uses one live data-driven prefab, one visible action per state, consistent 3px borders, procedural gradients, and aspect-preserved shared battlefield art
- [x] Victory filled stars reuse the shared Campaign atlas sprite while defeat outlines and the reward-header star are procedural; no screen-local duplicate star asset was added
- [x] End-of-Day Report iteration 3 captured at 16:9 and 20:9; awaiting explicit acceptance
- [x] End-of-Day map, district overlays, pressure chart, panels, and two footer actions match the V3 hierarchy with aspect-preserved shared art

## SCN-07 Loadout / Squad Prep Execution Record

The screen did not previously have a dedicated prefab or an installed shell
route. `SCN07_LoadoutSquadPrepContent.prefab` now supplies the complete V3
surface and the shell mounts it for `UIRoute.LoadoutSquadPrep`.

The screen uses a reusable equipment source sheet retained under
`V3Shared/SourceMasters/Loadout` and eight isolated runtime sprites under
`V3Shared/Sprites/Equipment`. The eight sprites are packed once in
`UI_V3_EquipmentIcons_01.spriteatlas`; screens must reference these shared paths
instead of copying the PNG files into local screen folders.

Iteration 3 is the current review candidate. Runtime captures and comparison
notes are frozen under
`SCN-07_LoadoutSquadPrep/iterations/iteration_03`. Validation passed at
1920x1080 and 4800x2160. The screen remains review-only until explicit user
acceptance.

## SCN-11 Operations Dashboard Execution Record

The legacy oversized-logo/satellite-map composition has been replaced by
`SCN11_OperationsDashboardContent.prefab`, authored against the 1672x941 V3
target lock. The screen now uses five procedural district fills over one
aspect-preserved map plate, an aspect-preserved ARIA briefing, the five-card
readiness rail, three warning rows, and the complete six-action command bar.

Dashboard icon sources are referenced from their canonical files and packed
once in `UI_V3_OperationsIcons_01.spriteatlas`. The runtime polygon component
requires its own `CanvasRenderer`, and validation rejects any dashboard missing
the five district overlays, procedural gradients, or aspect fitters.

Iteration 3 is the current review candidate. Runtime captures and comparison
notes are frozen under
`SCN-11_OperationsDashboard/iterations/iteration_03`. Validation passed with the
Game View explicitly set to 1920x1080 and 4800x2160 before each render. The
screen remains review-only until explicit user acceptance.

## SCN-12 District Detail Actions Execution Record

`UIRoute.DistrictDetail` and `SCN12_DistrictDetailActionsContent.prefab` now
provide the dedicated V3 district-action surface. All five district markers on
the Operations Dashboard route to it, while the header back interaction returns
to Operations.

The screen uses a centered 1672x941 design composition, independent 3px panel
borders, directional procedural gradients, an aspect-preserved shared district
map, an aspect-preserved ARIA portrait, seven action cards, and action-correct
icons. Newly required map-pin, tank, drone, aid, and clock sources are packed in
the existing `UI_V3_OperationsIcons_01.spriteatlas`; existing mission, campaign,
equipment, and core sprites remain referenced from their canonical shared atlas
paths without copies.

Iteration 3 is the current review candidate. Runtime captures and comparison
notes are frozen under
`SCN-12_DistrictDetailActions/iterations/iteration_03`. Validation passed with
the Game View explicitly set to 1920x1080 and 4800x2160 before each render. The
screen remains review-only until explicit user acceptance.

## SCN-00 First Launch Execution Record

The functional first-launch sequence now presents four V3-authored states from
the existing language and narrative prefabs: language choice, commander
identity, comic playback, and ARIA guidance. Runtime flow remains intact while
selection is explicit and Continue performs confirmation.

The screen family uses centered 1672x941 authored compositions, procedural
directional gradients, independent constant 3px borders, shared-atlas icons,
and aspect-preserved portrait/comic art. The language background lives outside
the fixed composition so it fills 20:9 without stretching or black gutters.
The new ARIA portrait is a reusable source sprite and is intentionally not
duplicated inside an atlas.

Iteration 4 is the current review candidate. Eight runtime captures and the
comparison record are frozen under `SCN-00_FirstLaunch/iterations/iteration_04`.
Validation passed at 1920x1080 and 4800x2160. The screen family remains
review-only until explicit user acceptance.

## SCN-08 Match HUD Execution Record

The existing `SCN08_MatchHudContent.prefab` keeps its protected runtime view
components and serialized controls while its four shell sections mount beneath
a centered 1672x941 V3 composition. The selection summary remains live; Board
is relocated into its four-action grid; Support and Build are relocated into
the eight-command footer rail; the live minimap is attached to the expanded
ARIA assistant panel; and the obsolete objectives, current-order pill, and
right quick rail are hidden.

Every repeated surface uses a procedural directional gradient with the same
3 px stroke. ARIA, squad portraits, the minimap, and the gameplay backdrop use
aspect-preserving fit or cover policies. The five squad labels are persistent
prefab content and their runtime initializer is idempotent. Match-only symbols
are packed once in `UI_V3_MatchIcons_01.spriteatlas`; Attack, Hold, Scan,
Board, Settings, civilian-risk, and warning symbols reuse their canonical
shared atlas entries.

Iteration 4 is the current review candidate. The 1920x1080 and 4800x2160
captures plus comparison notes are frozen under
`SCN-08_MatchHudV3/iterations/iteration_04/`. Validation passed with eight
functional command buttons, five squad cards, 32 gradients, attached live
minimap, and aspect-preserved art. The screen remains review-only until explicit
user acceptance.

Transport Passengers iteration 5 is frozen under
`SCN-08_MatchHudV3/iterations/iteration_05/`. It keeps ten live capacity slots,
four pooled passenger rows, per-row Exit, Rope Drop/Exit All, Close, existing
unit portraits, severity-aware feedback, and aspect-preserved transport art.

Tactical Feedback iteration 6 is frozen under
`SCN-08_MatchHudV3/iterations/iteration_06/`. It adds an exclusive procedural
V3 selected state for the real Attack command, the target range strip, dashed
route, friendly ring, hostile marker/health, target-sized error strip, and the
`TUTORIAL 1/5` ARIA state. The duplicate legacy current-order banner is
suppressed on the V3 prefab so it cannot collide with ARIA at 20:9. Actual
Menu-to-Match Play Mode captures passed after clicking `AttackCommand` at both
1920x1080 and 4800x2160, and the focused feedback suite passes 17 tests.

Iterations 4-6 remain review-only until explicit user acceptance.

## SCN-08 Full Map Execution Record

`SCN08_FullMapPopup.prefab` now uses the V3 target hierarchy while preserving the
existing `MatchHudFullMapPopupView` and `MatchHudMinimapView` runtime contracts.
The modal contains a distinct header, full legend, aspect-preserved tactical map,
map-information rail, five quick toggles, and footer instruction/Center on HQ
action. Close, drag/tap focus, zoom, Center on HQ, and toggle visuals remain real
Unity controls.

The map reuses the existing shared Sahrin plate and never stretches it. Live map
markers now use the fresh shared V3 friendly, hostile, and neutral symbols instead
of generated square placeholders. Repeated surfaces use directional procedural
gradients and independent constant 3 px borders; the saved prefab contains no
missing scripts or screen-local icon copies.

Iteration 1 is frozen under `SCN-08_FullMap/iterations/iteration_01/`. Deterministic
and live shell captures passed at 1920x1080 and 4800x2160, and the focused prefab
suite passes three tests. The screen remains review-only until explicit user
acceptance.

## POP-03 Build Placement Execution Record

`SCN08_BuildPlacementConfirmationBar.prefab` mounts as a full 1672x941
responsive Match section with a full-width 1664x310 footer. The old standalone
`BuildPlacementPanel.prefab` is no longer an obsolete full-screen popup: it is a
reusable top-right footprint-validity/minimap state instantiated by the real
confirmation bar. Valid placement keeps ARIA visible and Place Building active;
invalid placement replaces ARIA, displays obstruction metadata, and renders a
neutral disabled action. Closing restores every temporarily suppressed surface.

The building image reuses the existing refinery/action portrait with aspect
cropping; the minimap reuses the existing Match map with aspect-fill clipping;
resource, warning, check, and marker symbols reuse shared V3 art. All repeated
surfaces are procedural gradients with the same 3 px primary stroke.

Iteration 3 is frozen under `POP-03_BuildPlacement/iterations/iteration_03/`.
Four focused structure/state/restore checks and valid/invalid exact-size live
captures at 1920x1080 and 4800x2160 pass. The live route has no battlefield
world, so deterministic comparison renders carry the full gameplay plate. The
world placement grid/ghost remains runtime-owned rather than duplicated in UI.
The screen remains review-only until explicit user acceptance.

## PREFAB-06 Tutorial Presentation Execution Record

`POP13_ARIACommandAssistantPopup.prefab` retains the existing popup and tutorial
runtime contracts while its tutorial surface becomes the target-sized top-right
V3 panel. The new ARIA portrait is masked and aspect-preserved. Do It, Show Me,
and Skip remain real buttons; English/Persian step progression, narration, RTL,
and command handoff remain data-driven. The first-step cyan guide is visual only
and does not consume battlefield input.

While the wider tutorial panel is active, a reversible runtime variant compacts
the live resource strip and Settings/Pause controls and hides the normal embedded
ARIA panel. Closing the tutorial restores their authored Match HUD layout.
Iteration 3 is frozen under
`PREFAB-06_TutorialPresentation/iterations/iteration_03/`. The 23-test behavior
suite, three focused V3 checks, deterministic comparison renders, and exact-size
1920x1080 / 4800x2160 Play Mode captures pass. It remains review-only until
explicit user acceptance.

## SCN-09 Build Drawer Execution Record

`SCN09_BuildDrawerPopup.prefab` retains the existing `BuildDrawerView` and
`BuildDrawerCatalogRuntimeView` ownership while presenting the final V3
header, four category tabs, 2x2 catalog, selected-item detail, production
queue, instruction strip, and primary action. The ready and disabled states
are data-driven states of the same prefab.

Unit and building illustrations are not Build Drawer assets. Cards resolve the
existing catalog `portraitCardSprite`, the detail panel resolves
`portraitActionSprite`, and queue thumbnails use the same catalog projection.
Resource, category, lock, time, and instruction symbols reuse shared V3 atlas
entries or procedural geometry. All repeated panels use directional procedural
gradients and independent 3 px borders.

Iteration 1 is the current review candidate. Ready and disabled prefab renders,
actual Menu-scene Play Mode captures at 1920x1080 and 4800x2160, logs, and
comparison notes are frozen under
`SCN-09_BuildDrawer/iterations/iteration_01/`. The catalog/interaction suite
passes 25 tests. The Menu-scene Match route does not load a battlefield, so a
final in-match backdrop capture and explicit user acceptance remain pending.

## POP-05 Mission Result Execution Record

`MissionResultPopup.prefab` is now one shared V3 layout for victory and defeat.
The existing `MissionResultPopupView` and `CampaignMissionHudResultBinder`
remain authoritative; the model changes the title, mission identity, elapsed
time, stars, objective states, performance values, rewards, outcome palette,
summary, and the single available action without swapping or flattening the UI.

The background is the shared Forward Post V3 plate using
`AspectRatioFitter.EnvelopeParent`, while the centered 1672x941 composition
preserves the lock geometry at 1920x1080 and 4800x2160. Major header, middle,
and footer panels use constant 3 px borders and procedural gradients. Detailed
filled stars reuse the existing shared Campaign atlas sprite; empty stars and
the state-colored reward-header star are procedural.

Iteration 4 is the current review candidate. Four runtime captures and the
comparison record are frozen under
`POP-05_MissionResult/iterations/iteration_04/`. Validation passed with two
runtime states, three live stars, 17 procedural gradients, exactly one visible
action per outcome, and aspect-preserved art. The screen remains review-only
until explicit user acceptance.

## POP-06 End-of-Day Report Execution Record

`EndOfDayReportPopup.prefab` now uses the V3 target hierarchy while preserving
its existing `UIPopupFrameView` contract. The header contains the brand, day,
and canonical Credits/Command resources. The report body contains three status
cards, a procedural pressure chart, four procedural district overlays, an
operation summary, district states, and the civilian-protection total. Both
footer controls remain real buttons with directional gradients.

The screen uses one shared Sahrin map texture for both the centered report map
and its ultrawide backdrop. Both references use
`AspectRatioFitter.EnvelopeParent`, so neither 1920x1080 nor 4800x2160 stretches
the source. No report-local copy of the map or icons was created. Major panels
and actions use constant 3 px borders and 42 procedural gradient surfaces.

Iteration 3 is the current review candidate. Runtime captures and comparison
notes are frozen under `POP-06_EndOfDayReport/iterations/iteration_03/`. The
screen remains review-only until explicit user acceptance.

## POP-09 Ability / Upgrade Detail Work-In-Progress Record

The V3 prefab source, runtime view, tests, and exact-size capture entry points are
implemented. Focused validation passed and the 1920x1080 live image is a useful
review candidate, but the wrapper process did not exit cleanly and the
4800x2160 capture could not run while the macOS login session was locked.

Evidence remains under
`POP-09_AbilityUpgradeDetail/work_in_progress/iteration_02_pending_live/` and is
deliberately not frozen under `iterations/`. Rerun both exact live captures and
freeze only after the wrapper exits cleanly and both images pass comparison.

## POP-07 Pause Options Work-In-Progress Record

The rejected legacy capture uses menu-world art, ornate/gold chrome, flat or
inconsistent action styling, and omits the target's live status column. The
staged V3 rebuild uses the actual Match route as its backdrop, one clean modal
frame, five directional-gradient actions, and the target objective, squads,
civilian-risk, and autosave status surfaces. Adjacent status rows use single
3 px dividers so borders do not overlap.

Resume, Options, Exit, and close retain shell action dispatch. Restart now queues
a new attempt through the existing mission launch and cleanup system after a
confirmation; Help opens an interactive controls panel. The offline contracts,
runtime, ECS, builder, and test assembly audit passes. Exact Play Mode captures
at 1920x1080 and 4800x2160 remain pending while the macOS login session is
locked. Evidence and pending state are recorded in
`POP-07_PauseOptions/WORK_IN_PROGRESS.md`; no immutable iteration exists yet.

## SCN-19 Armory Work-In-Progress Record

The previous ornate/gold Armory iteration was reopened because it does not match
the standalone V3 target. The source rebuild replaces it with the shared V3
header, five category tabs, runtime-backed 2x4 catalog, wide inspection panel,
and footer routes. Unit and building portraits continue to resolve from the
existing registries; no screen-local copies are baked into cards. Shared icons,
procedural directional gradients, and independent 3 px borders are used.

The staged source and pending validation commands are recorded in
`SCN-19_Armory/WORK_IN_PROGRESS.md`. No Armory iteration is frozen yet: Unity
build, focused validation, and Play Mode comparison at 1920x1080 and 4800x2160
remain mandatory after the macOS login session is unlocked.

## SCN-13 Skirmish Setup Work-In-Progress Record

The canonical target and the legacy prefab were audited before implementation.
No legacy image is being relabeled as a new iteration. The staged 1672x941 V3
rebuild restores all five preset cards, the central Sahrin operation preview,
the target opposing-force and match-rule columns, and the complete three-action
footer. Existing quick-custom configuration, reset, randomize, back, and launch
contracts are preserved.

Chrome uses procedural directional gradients and independent 3 px borders.
The map and all preset art use masked aspect-fill crops rather than stretching.
The implementation reuses existing operation/unit art and shared V3 icon
sources; procedural lock, check, target, dice, and chevron marks add no duplicate
atlas assets. The center section expands at 20:9 while the right controls remain
pinned to the canvas edge.

Offline contract, UI runtime, builder, and focused-test compilation passes.
Exact Unity build/test plus Play Mode comparison at 1920x1080 and 4800x2160 are
still mandatory after the locked macOS login session is available. Evidence and
the wrapper-only command list are recorded in
`SCN-13_SkirmishSetup/WORK_IN_PROGRESS.md`; no immutable SCN-13 iteration exists
yet.

## SCN-14 Store / Command Exchange Work-In-Progress Record

The canonical target was audited before implementation. There was no dedicated
Store Canvas body or valid current runtime iteration, so no legacy screenshot is
being relabeled as progress. The staged 1672x941 rebuild adds the complete
target header, six categories, responsive 2x2 offer catalog, selected-offer
detail, eligibility panel, Back action, and Purchase action. The center catalog
expands at 20:9 while the detail and right-side actions remain pinned to the
canvas edge.

All visible surfaces use directional procedural gradients and the same 3 px
border width. Art is masked with aspect-fill cropping and reuses shared V3
resource/menu imagery plus existing Armory and commander assets; no Store-local
raster duplicates were introduced. Category and offer inspection are
interactive. Purchase remains visibly but intentionally disabled until the
required wallet, receipt, catalog, persistence, and reward-grant services exist.

Offline runtime, isolated-builder, and focused-test compilation passes. Exact
Unity build/test plus Play Mode comparison at 1920x1080 and 4800x2160 remain
mandatory after the locked macOS login session is available. Evidence and the
wrapper-only command list are recorded in
`SCN-14_StoreCommandExchange/WORK_IN_PROGRESS.md`; no immutable Store iteration
exists yet.

## SCN-15 Command Inbox Work-In-Progress Record

The canonical Inbox target was audited before implementation. The route value
existed, but the project had no Inbox prefab, runtime view, route mount, or live
iteration. The staged 1672x941 implementation adds the complete target header,
five categories, search and sorting, five selectable messages, live read and
favorite state, two attachments, selected-message detail, and View Intel
navigation.

Chrome uses directional procedural gradients and independent 3 px borders. The
detail plate is an aspect-fill crop. The message column and search field expand
on 20:9 while right-side controls and detail stay pinned, preventing empty side
gutters. Existing shared V3 environment, map, Ranger, ARIA, operation, resource,
and settings sources are reused; no Inbox-local raster duplicates were created.

Offline full-runtime, isolated-builder, and focused-test compilation passes.
Exact Unity build/test plus Play Mode comparison at 1920x1080 and 4800x2160
remain mandatory after the locked macOS login session is available. Evidence
and wrapper-only commands are recorded in
`SCN-15_Inbox/WORK_IN_PROGRESS.md`; no immutable Inbox iteration exists yet.

## Completion Boundary For The First Slice

The first slice is complete when the V3 foundation and Settings pilot pass all
acceptance gates. Migration of the remaining screen families is subsequent work
using the proven system; it must not be simulated with temporary screen-local
art or flattened target images.

# UI Toolkit Target Lock Visual Match Tracker

Purpose:
Iteratively restyle the completed UI Toolkit screens so their rendered output matches the saved Target Lock reference mockups while preserving the existing shell and screen panel structure. This is a visual parity loop, not a layout architecture rewrite and not a behavior migration.

Branch:
`codex/ui-toolkit-target-lock-visual-match`

Last updated:
2026-06-21

Progress snapshot:

- Checklist progress: `34 / 142 complete (23.9%)`.
- In progress: `0`.
- Remaining open: `108`.
- Current target: `Next remaining SCN surface after SCN-08 Match HUD squad-tray correction`.
- Iteration loop status: `SCN-02 approved and reused as shared chrome baseline; SCN-02 runtime scaling now uses UI Toolkit Scale With Screen Size against the 4800x2160 reference with Expand-style aspect handling so 1920x1080 does not render raw oversized text; SCN-08 was reopened because the squad tray had been incorrectly accepted from a weak readability crop, then slice 08 corrected the five squad cards with focused shadow UI Builder evidence, removed the non-reference top health/progress strips, and made selection/hover a reusable state on the shared card frame; SCN-19 slice 13 fixes the rejected right action-button scale and the right stats label/value collision; SCN-03 slice 02 fixes artifacting panel frames and readability`.
- Surfaces target-matched: `1 / 11`.
- User verification gate: `not required for every remaining screen, but do not leave the current screen while an obvious visual defect remains; continue only after latest UI Builder/static crops are visually satisfactory`.
- Pixel Per Unit audit status: `SCN-02 audit recorded in pixel_per_unit_audit.md; no PPU changes`.
- 9-slice audit status: `SCN-02 audit recorded in nine_slice_audit.md; slice-scale unit fix applied; no sprite border changes`.
- UI Builder/static comparison status: `SCN-02 UI Builder remains the 4800x2160 authoring/reference view; UI Builder does not apply runtime PanelSettings scale mode`.
- Game View capture comparison status: `SCN-02 main-scene Game View captured at 1920x1080 after Scale With Screen Size fix; text scales down instead of rendering oversized`.
- Shadow project validation status: `SCN-19 validated only in /Users/farhad/Projects/WarlineCapture-CodexUnity1 UI Builder/static preview; PlayMode/Game View not used`.
- Validation status: `git diff --check passed after SCN-19 shared chrome slice; shadow Editor log final import has no active SCN-19 invalid asset-path or slice-scale warnings`.

## Scope

Update UI Toolkit visual styling for the existing shell and screens:

| Surface | UI Toolkit path | Reference target |
| --- | --- | --- |
| Shell | `Assets/Game/UI Toolkit/UIShellAppCanvas/` | Shared Target Lock chrome contract |
| SCN-01 Loading | `Assets/Game/UI Toolkit/SCN01_LoadingContent/` | `Design/VisualLockLayered/SCN-01_SplashLoading/reference/SCN-01_SplashLoading_NewMainMenuArtDirection_TargetLock_V04.png` |
| SCN-02 Main Menu | `Assets/Game/UI Toolkit/SCN02_MainMenuContent/` | `Design/VisualLockLayered/SCN-02C_MainMenuBrightCommand/reference/scn02c_target_lock_warline_capture_bright.png` |
| SCN-03 Commander Profile | `Assets/Game/UI Toolkit/SCN03_CommanderProfileContent/` | `Design/VisualLockLayered/SCN-03_CommanderProfile/reference/SCN-03_CommanderProfile_NewMainMenuArtDirection_TargetLock_V01.png` |
| SCN-08 Match HUD | `Assets/Game/UI Toolkit/SCN08_MatchHudContent/` | `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_NewMainMenuArtDirection_TargetLock_V02.png` |
| SCN-08 Build Placement Bar | `Assets/Game/UI Toolkit/SCN08_BuildPlacementConfirmationBar/` | `Design/VisualLockLayered/SCN-08_BuildPlacementConfirmationBar/reference/SCN-08_BuildPlacementConfirmationBar_NewMainMenuArtDirection_TargetLock_V01.png` |
| SCN-09 Build Drawer Popup | `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/` | `Design/VisualLockLayered/SCN-09_BuildDrawer/reference/SCN-09_BuildDrawer_NewMainMenuArtDirection_TargetLock_V03.png` |
| SCN-19 Armory | `Assets/Game/UI Toolkit/SCN19_ArmoryContent/` | `Design/VisualLockLayered/SCN-19_Armory/reference/SCN-19_Armory_NewMainMenuArtDirection_TargetLock_V04.png` |
| POP-05 Mission Result | `Assets/Game/UI Toolkit/POP05_MissionResultPopup/` | `Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_NewMainMenuArtDirection_TargetLock_V01.png` |
| POP-06 Settings | `Assets/Game/UI Toolkit/POP06_SettingsPopup/` | Target required before visual-lock claim |
| POP-07 Inbox | `Assets/Game/UI Toolkit/POP07_InboxPopup/` | Target required before visual-lock claim |

## Non-Negotiable Structure Lock

The existing UI Toolkit route and panel structure must remain intact.

Shell regions that must remain:

- `HeaderRegion`
- `LeftRegion`
- `MiddleRegion`
- `RightRegion`
- `FooterRegion`
- `MenuBackgroundRegion`
- Existing screen slots under `MiddleRegion`
- Existing popup slot and modal overlay

Allowed changes:

- USS positioning, width, height, margin, padding, gap, alignment, opacity, color, tint, font, font size, font style, letter spacing, text alignment, background image, background scale mode, slice values, and slice scale.
- Sprite imports, including Texture Type, Sprite Mode, Mesh Type, Pixel Per Unit, borders, alpha handling, compression, mipmaps, and atlas assignment.
- Replacing a background, frame, icon, or chrome sprite with the correct Target Lock layer asset.
- Adding narrowly scoped USS classes for visual states when they do not change hierarchy or behavior.
- Adjusting PanelSettings scale mode or reference resolution only after documenting before/after captures and confirming every surface.

Allowed write paths:

- `Assets/Game/UI Toolkit/**/*.uxml`
- `Assets/Game/UI Toolkit/**/*.uss`
- `Assets/Game/Art/UI/**/*.png`
- `Assets/Game/Art/UI/**/*.png.meta`
- `Assets/Game/UI Toolkit/**/*.asset` only if a UI Toolkit asset import or PanelSettings visual scale issue is explicitly documented first.
- `Design/Architecture/ui_toolkit_target_lock_visual_match_tracker.md`
- `Design/VisualLockLayered/_UIToolkitVisualMatch/**`
- `Design/AgentReports/**`
- Narrowly scoped editor-only static preview/capture tooling when needed to support UI Builder/static comparison in the shadow project. This exception does not allow runtime behavior, gameplay, ECS, composition, scene, prefab, or PlayMode capture changes.

Forbidden changes:

- Do not remove, rename, or move shell regions.
- Do not collapse header, footer, left, right, or middle content into one flat image.
- Do not replace live UI panels with a screenshot of the mockup.
- Do not edit C# runtime or test files in this loop.
- Do not edit editor C# except for the explicit static preview/capture tooling exception in the allowed write paths.
- Do not edit `Assets/Game/Scripts/**/*.cs`, `Assets/Tests/**/*.cs`, `.asmdef`, scene, prefab, ECS, gameplay, or composition files.
- Do not change ECS read models, UI action requests, gameplay behavior, screen routing, or bindings.
- Do not add Canvas dependencies back into the UI Toolkit path.
- Do not add `Update`, `LateUpdate`, coroutines, polling loops, or gameplay timers to UI views.
- Do not "fix" visual mismatch by changing data values or hiding required runtime panels.
- Do not delete fallback Canvas assets in this visual-match loop.

## Shared Chrome Override Rules

These rules override mockup pixel matching when there is a conflict.

- Reuse the approved SCN-02 Main Menu header for Armory and the rest of the main-menu-adjacent screens. Do not create or convert separate target headers for those screens.
- Match HUD is the exception: it is allowed to keep or receive its own gameplay HUD header.
- Reuse the approved SCN-02 Main Menu left navigation style and background for Armory and other main-menu screens.
- For reused left navigation, only icons, labels, selected state, and route-specific active item should change.
- Do not create a visually different Armory left nav, even if the mockup shows one.
- If a right-side mockup panel is one large baked sprite with multiple sections, split it into separate UI Toolkit panels like the approved SCN-02 right commander panel.
- Baked sprites are acceptable only for decorative background art, not for multi-section live UI panels.
- Priority order is shared SCN-02 header/nav language, then live panel-by-panel composition, then clean Target Lock style, then strict mockup pixel matching.

## Implementation Loop Contract

Every surface must use the same loop until it is visually accepted:

1. Implement one small visual fix batch in allowed files only.
2. Reload or reopen the surface in UI Builder.
3. Before screenshot capture, enable the `Match Game View` check-mark toggle so UI Builder uses the intended aspect/resolution.
4. After the `Match Game View` toggle is enabled, click `Fit Viewport` so the preview is scaled correctly in the UI Builder viewport.
5. Compare against the saved Target Lock reference mockup.
6. Classify differences as `PPU`, `9-slice`, `position`, `padding`, `font`, `sprite`, `state`, `content`, `responsive`, or `artifact`.
7. Fix Pixel Per Unit and 9-slice issues before layout or font tweaks.
8. Repeat UI Builder comparison until no obvious UI Builder mismatch remains.
9. Sync the same allowed-file changes into the shadow project.
10. Reconfirm the `Match Game View` check-mark toggle is enabled, click `Fit Viewport`, then capture the surface in the shadow project at the required aspect/resolution without opening the main project.
11. Generate comparison contact sheets and focused crops.
12. Reclassify remaining differences and start the next fix batch.
13. Continue until the surface has no visible unapproved mismatch in latest UI Builder preview, latest required captures, and focused crops.
14. If any visible mismatch remains, keep the surface `In progress` or record a user-approved exception; do not mark it `Target matched`.

This is intentionally iterative. One implementation pass is never enough evidence for completion.

## Pixel Per Unit Rule

Pixel Per Unit is a first-class acceptance item, not a cleanup detail.

- If chrome is too thick, too heavy, or visually oversized, inspect and tune Pixel Per Unit before changing layout.
- If chrome is too thin or undersized, inspect and tune Pixel Per Unit before increasing panel size.
- In UI Toolkit, increasing Pixel Per Unit makes sprite chrome render visually smaller/thinner; lowering Pixel Per Unit makes it larger/heavier.
- Do not leave every sprite at default `100` when focused crops show wrong line thickness.
- Do not compensate for bad Pixel Per Unit with distorted width/height or bad slice scale.
- Record every non-default Pixel Per Unit value in this tracker or a per-surface implementation report.
- Recompare in UI Builder and Game View after every Pixel Per Unit or 9-slice change.

## Visual Match Acceptance

A surface is target-matched only when:

- Header, footer, left, right, and middle regions remain in their existing structural roles.
- Main-menu-adjacent screens reuse the approved SCN-02 header and left-nav style unless they are explicitly gameplay HUD surfaces.
- Multi-section right panels are decomposed into live panel-by-panel UI instead of kept as one baked sprite.
- Full-screen Game View capture matches the target composition at the canonical target resolution.
- 20:9 capture keeps intended edge anchoring and does not stretch content art.
- Focused crops match for every major panel, button, tab, card, resource chip, icon, portrait, divider, and footer/header chrome.
- Frame thickness, corner silhouette, 9-slice borders, fill opacity, and chrome scale match the reference.
- Fonts, font sizes, weights, text alignment, and line heights match focused target crops.
- All visible labels, counters, button captions, and state text remain correct.
- No text clips, wraps unexpectedly, overlaps, or uses autosize to hide wrong sizing.
- No opaque rectangular artifacts appear around sliced or transparent chrome.
- No obvious difference remains unlisted.

## Progress Update Rules

- Update `Last updated` after every implementation slice.
- Update the progress snapshot after every iteration.
- Update per-surface status as `Not started`, `In progress`, `Blocked`, `Needs target`, `Needs assets`, `Needs PPU pass`, `Needs 9-slice pass`, `Needs UI Builder pass`, `Needs Game View pass`, or `Target matched`.
- Add capture paths and comparison outputs for every completed iteration.
- If a checklist item is added or removed, update the denominator.
- A surface cannot be `Target matched` until the latest UI Builder and Game View comparison loops are both complete.

## Phase 0 - Baseline, Structure Lock, And Reference Inventory

Goal:
Freeze the existing UI Toolkit hierarchy, collect canonical targets, and define what is allowed to move visually.

- [x] Confirm `Design/Architecture/ui_toolkit_canvas_replacement_plan.md` remains behavior-complete before visual restyle starts.
- [x] Inventory every UXML file under `Assets/Game/UI Toolkit`.
- [x] Record shell regions from `UIShellAppCanvas.uxml`: `HeaderRegion`, `LeftRegion`, `MiddleRegion`, `RightRegion`, `FooterRegion`, and `MenuBackgroundRegion`.
- [ ] Record each screen's root visual element and top-level header/left/right/middle/footer content elements.
- [x] Create a structure-lock table with `UXML path`, `element name`, `region role`, `may move visually`, `may resize`, and `may not rename`.
- [x] Confirm all canonical reference PNGs listed in the scope table exist.
- [x] Mark POP-06 Settings as `Needs target` if no saved target exists.
- [x] Mark POP-07 Inbox as `Needs target` if no saved target exists.
- [ ] Capture current UI Toolkit Game View screenshots for every route at 16:9.
- [ ] Capture current UI Toolkit Game View screenshots for every route at 20:9.
- [ ] Capture current UI Builder screenshots or exported previews for every editable UXML surface.
- [ ] Store captures under `Design/VisualLockLayered/_UIToolkitVisualMatch/<SurfaceId>/baseline/`.
- [ ] Record capture commands, Unity version, resolution, branch, and commit hash.
- [ ] Run `git diff --check` before implementation edits.

Acceptance:

- Existing structure is documented.
- Current visual state is captured.
- Missing targets are known before any surface is claimed target-matched.

## Phase 1 - Target-To-Toolkit Mapping

Goal:
Map every important target element to an existing UI Toolkit element without changing screen structure.

- [x] Create `Design/VisualLockLayered/_UIToolkitVisualMatch/target_to_toolkit_mapping.md`.
- [ ] Add one table per surface.
- [ ] For each visible target element, record target bounds, current UXML element, USS class, sprite asset, font style, expected region, and QA status.
- [ ] Mark each element as `header`, `footer`, `left`, `right`, `middle`, `background`, `modal`, or `diagnostic`.
- [ ] Mark every element that is missing an asset.
- [ ] Mark every element that exists structurally but has wrong chrome, padding, font, or placement.
- [ ] Mark every element where the target differs from runtime data and decide whether target or runtime content is authoritative.
- [ ] Identify repeated templates: armory item, build catalog item, production item, passenger item, squad card, command button, popup row.
- [ ] Identify state variants: normal, selected, disabled, locked, pressed, warning, empty, active, ready, unavailable.
- [ ] Confirm no target element requires moving content between header/footer/left/right/middle regions.
- [ ] Add blockers for target features that cannot fit the existing structure without user approval.

Acceptance:

- Every target element has a Toolkit owner or a documented blocker.
- No implementation starts from visual guessing.

## Phase 2 - Asset Import, Pixel Per Unit, And 9-Slice Audit

Goal:
Fix the most common chrome failure modes before USS positioning work.

- [ ] Inventory every sprite referenced by the UI Toolkit USS files.
- [ ] Inventory every candidate Target Lock sprite under `Assets/Game/Art/UI/Generated`, `Assets/Game/Art/UI/Panels`, `Assets/Game/Art/UI/Icons`, and `Assets/Game/Art/UI/Final`.
- [ ] For each frame/panel/button/card sprite, inspect alpha corners and transparent gutters.
- [ ] For each frame/panel/button/card sprite, record current Pixel Per Unit.
- [ ] For each frame/panel/button/card sprite, record sprite border values.
- [ ] For each USS class using sliced chrome, record `-unity-slice-left`, `-unity-slice-right`, `-unity-slice-top`, `-unity-slice-bottom`, and `-unity-slice-scale`.
- [ ] Compare chrome thickness in focused crops before changing layout dimensions.
- [ ] Tune Pixel Per Unit for each mismatched chrome sprite.
- [ ] Tune sprite borders in the import metadata when corners or frame edges distort.
- [ ] Tune USS slice values only after source sprite borders and Pixel Per Unit are correct.
- [ ] Remove transparent gutters from source assets only through an explicit asset cleanup slice with preserved `.meta` files.
- [ ] Disable mipmaps for UI sprites when needed.
- [ ] Confirm texture compression does not blur thin Target Lock chrome.
- [ ] Confirm icons are standalone transparent sprites, not baked into button backgrounds.
- [ ] Confirm reusable backgrounds do not contain labels, icons, portraits, counters, or health bars.
- [ ] Record every non-default Pixel Per Unit in `Design/VisualLockLayered/_UIToolkitVisualMatch/pixel_per_unit_audit.md`.
- [ ] Record every 9-slice value in `Design/VisualLockLayered/_UIToolkitVisualMatch/nine_slice_audit.md`.

Acceptance:

- Chrome scale problems are solved at import/slice level before layout hacks.
- Pixel Per Unit decisions are traceable.

## Phase 3 - Shared Target Lock Style Pass

Goal:
Normalize shared visual language without changing panel structure.

- [ ] Audit shared colors for command green, dark olive backplates, gold accents, warning amber, disabled gray, and text cream.
- [ ] Audit shared font usage and confirm Oxanium variants are used consistently.
- [ ] Create or tune shared USS variables/classes only where local files already support the pattern.
- [ ] Normalize button reset rules so buttons do not inherit unwanted default borders or padding.
- [ ] Normalize text shadow/outline/readability treatment if current screens drift from target.
- [ ] Normalize resource chips, square icon buttons, rectangular command buttons, tabs, and card frames.
- [ ] Normalize selected/disabled/locked/active state classes across repeated templates.
- [ ] Confirm shared style changes do not move elements between shell regions.
- [ ] Capture before/after shared component crops.

Acceptance:

- Common chrome reads as one Target Lock system.
- Shared changes do not break individual screen composition.

## Phase 4 - Surface Visual Passes

Goal:
Apply visual updates one surface at a time, preserving structure and behavior.

- [ ] SCN-01 Loading: match loading panel chrome, logo scale, progress bar, status chips, spinner, background, and footer/header spacing.
- [ ] SCN-02 Main Menu: match header, navigation, resource strip, mode cards, commander panel, deploy CTA, background, and footer spacing.
- [x] SCN-03 Commander Profile: match profile panels, stat cards, portrait treatment, tabs, header, footer, and chrome hierarchy for the current shared-chrome/readability pass.
- [x] SCN-08 Match HUD: match top header, resource strip, objectives panel, selected panel, right quick rail, footer squad tray, command rail, minimap/quick panels if present for the current readability pass.
- [ ] SCN-08 Build Placement Bar: match confirmation rail, action buttons, instruction text, warning state, and edge anchoring.
- [ ] SCN-09 Build Drawer Popup: match popup frame, left catalog, right details/queue panel, tabs, cards, footer buttons, and close/header chrome.
- [x] SCN-19 Armory: match roster cards, inspection panel, bottom tabs, locked/selected states, filters, top chrome, and CTA buttons for the current shared-chrome/readability pass.
- [ ] POP-05 Mission Result: match modal frame, result title, rewards/stats, CTA buttons, close/back behavior, and backdrop.
- [ ] POP-06 Settings: do not claim target match until a saved reference target exists.
- [ ] POP-07 Inbox: do not claim target match until a saved reference target exists.
- [ ] Shell: verify `HeaderRegion`, `LeftRegion`, `MiddleRegion`, `RightRegion`, and `FooterRegion` still mount the same screen content.

Acceptance:

- Every surface has a focused implementation pass.
- Missing-reference popups are explicit blockers, not silent failures.

## Phase 5 - UI Builder Comparison Loop

Goal:
Use UI Builder as the first visual feedback loop for each surface before runtime capture.

For each surface:

- [ ] Open the UXML in UI Builder.
- [ ] Set the preview resolution to the canonical reference aspect when possible.
- [ ] Load the target mockup beside UI Builder for manual comparison.
- [ ] Compare root region placement: header, footer, left, right, middle, modal/background.
- [ ] Compare every major frame's width, height, border weight, corner silhouette, and fill opacity.
- [ ] Compare padding inside cards, buttons, tabs, chips, and panels.
- [ ] Compare icon size, center point, aspect ratio, and state sprite.
- [ ] Compare font family, font size, style, color, alignment, and line height.
- [ ] Compare labels and data values against target or runtime-authoritative notes.
- [ ] Identify first-order fixes in this order: Pixel Per Unit, sprite border, USS slice, background sprite, padding, position, font size.
- [ ] Apply one coherent fix batch.
- [ ] Reopen/reload UI Builder and compare again.
- [ ] Repeat until no obvious UI Builder-only mismatch remains.
- [ ] Save a UI Builder preview screenshot under the surface iteration folder.

Acceptance:

- UI Builder preview is visually close before runtime capture starts.
- Pixel Per Unit and 9-slice problems are not deferred to Game View.

## Phase 6 - Runtime Capture And Difference Loop

Goal:
Verify the actual runtime render in the shadow project, not just static UXML preview.

For each surface and iteration:

- [x] Confirm `/Users/farhad/Projects/WarlineCapture-CodexUnity1` exists and opens independently from the main project.
- [x] Sync only allowed changed files into `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- [ ] Launch Unity validation/capture path against `/Users/farhad/Projects/WarlineCapture-CodexUnity1`, not the main project.
- [ ] Capture 16:9 Game View from the shadow project at the canonical target size or nearest project-standard size.
- [ ] Capture 20:9 Game View from the shadow project.
- [ ] Confirm each capture is current, nonblank, and shows the intended route.
- [ ] Create a comparison contact sheet: target, UI Builder preview, Game View capture, amplified difference overlay.
- [ ] Create focused crops for header, footer, left panel, right panel, middle panel, cards/buttons, and reported problem areas.
- [ ] Review focused crops for frame thickness, corner silhouette, slice distortion, padding, icon centering, font size, text clipping, and alpha artifacts.
- [ ] Classify each difference as `PPU`, `9-slice`, `position`, `padding`, `font`, `sprite`, `state`, `content`, `responsive`, or `unknown`.
- [ ] Fix `PPU` and `9-slice` differences before spacing fixes.
- [ ] Fix spacing/position differences before typography micro-adjustments.
- [ ] Fix typography after container size and padding are stable.
- [ ] Rerun capture after the fix batch.
- [ ] Stop only when the latest contact sheet and focused crops meet acceptance or a blocker is recorded.

Acceptance:

- Shadow-project runtime Game View matches target at both required aspects.
- Latest comparison artifacts are saved and linked.

## Phase 7 - Responsive And Safe-Area Pass

Goal:
Ensure visual lock is not limited to one desktop capture.

- [ ] Validate 16:9 landscape.
- [ ] Validate 20:9 landscape.
- [ ] Validate a tablet-like aspect if already supported by project capture tooling.
- [ ] Confirm header remains attached to top region.
- [ ] Confirm footer remains attached to bottom region.
- [ ] Confirm left and right panels remain edge-anchored and do not overlap middle content.
- [ ] Confirm middle content remains readable and does not hide behind side panels.
- [ ] Confirm modal popups remain centered and inside safe area.
- [ ] Confirm font sizes do not scale with viewport width in a way that breaks target proportions.
- [ ] Confirm text does not clip in English strings used by the current runtime.
- [ ] Confirm repeated cards/rows do not resize parent panels unexpectedly.
- [ ] Confirm no target-matched chrome becomes blurry or distorted at 20:9.

Acceptance:

- Surface remains clean across supported landscape aspects.
- Structural regions stay intact.

## Phase 8 - Shadow Project Validation Path

Goal:
Validate and compare in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` without opening or mutating the main project.

- [x] Verify the shadow project exists: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- [x] Verify the shadow project Unity version and packages match the main project closely enough for UI Toolkit rendering comparison.
- [x] Define the sync command for allowed paths only: UI Toolkit UXML/USS, UI art PNG/meta, and comparison notes.
- [x] Request approval before syncing files to the shadow project when the current sandbox does not allow writes outside `/Users/farhad/Projects/WarlineCapture`.
- [x] Do not sync C# files into the shadow project for this loop.
- [x] Do not run Unity against `/Users/farhad/Projects/WarlineCapture` for visual comparison unless the user explicitly requests main-project validation.
- [x] Run batchmode/open validation on `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- [x] Save shadow logs under `/private/tmp/warline-ui-target-lock-<surface>-shadow.log`.
- [ ] Save shadow captures under `Design/VisualLockLayered/_UIToolkitVisualMatch/<SurfaceId>/iteration_##/`.
- [ ] Compare shadow captures to the saved target and UI Builder preview.
- [x] If shadow project is stale or missing required assets, refresh only the allowed UI Toolkit/art paths from main to shadow.
- [ ] If shadow project cannot open, record `Blocked - shadow project unavailable` and do not fall back to mutating the main project silently.

Suggested shadow validation command shape:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 \
  -logFile /private/tmp/warline-ui-target-lock-shadow.log
```

Acceptance:

- The loop can capture and compare in the shadow project without opening the main project.
- Any main-project validation is a separate explicit request, not the default.

## Phase 9 - Non-C# Guardrails

Goal:
Run guardrails without changing C# code.

- [ ] Run existing tests that assert UXML required region names still exist.
- [ ] Run existing tests that assert UI Toolkit runtime paths do not regain Canvas dependencies.
- [ ] Run text scans that assert required USS files do not reference old Canvas-era sprites.
- [ ] Run text scans that assert critical Target Lock sprite paths are present in USS.
- [ ] Run text scans that confirm this loop did not modify `Assets/Game/Scripts/**/*.cs`, `Assets/Tests/**/*.cs`, `.asmdef`, scenes, or prefabs.
- [ ] Run a Pixel Per Unit audit report for known Target Lock chrome sprites.
- [ ] Run a 9-slice audit report for known panel/button/card sprites.
- [ ] Update capture tooling documentation with exact commands and output paths when it is markdown-only.
- [ ] Run `git diff --check`.
- [ ] Run focused existing EditMode validation for UI Toolkit migration/structure tests against the shadow project when practical.
- [ ] If a new C# test or editor tool is needed, record it as a separate follow-up task outside this visual-match loop.

Acceptance:

- The most expensive visual regressions are mechanically guarded where practical.
- C# files remain untouched by this loop.
- Tests and scans do not replace manual visual comparison; they support it.

## Phase 10 - Per-Surface Completion Reports

Goal:
Make each completed screen auditable by future agents.

For each completed surface:

- [ ] Write a report under `Design/AgentReports/`.
- [ ] Include target reference path.
- [ ] Include changed UXML, USS, sprite, and `.meta` files.
- [ ] Include Pixel Per Unit changes.
- [ ] Include 9-slice/border changes.
- [ ] Include capture paths for baseline, iteration screenshots, final UI Builder preview, final 16:9 Game View, final 20:9 Game View, and focused crop sheets.
- [ ] Include known remaining differences or state `none observed`.
- [ ] Include validation commands and results.
- [ ] Include any blocked target or missing asset notes.
- [ ] Update this tracker surface status and progress snapshot.

Acceptance:

- A future agent can see exactly why a surface was accepted.
- Non-default import settings are not lost.

## Required Iteration Folder Shape

Use this folder shape for comparison artifacts:

```text
Design/VisualLockLayered/_UIToolkitVisualMatch/
  <SurfaceId>/
    baseline/
      current_16x9.png
      current_20x9.png
      ui_builder_preview.png
    iteration_01/
      ui_builder_preview.png
      game_16x9.png
      game_20x9.png
      comparison_contact_sheet.png
      crops_header.png
      crops_left.png
      crops_middle.png
      crops_right.png
      crops_footer.png
      notes.md
    final/
      ui_builder_preview.png
      game_16x9.png
      game_20x9.png
      comparison_contact_sheet.png
      focused_crops.png
      acceptance.md
```

## Difference Classification

Use these labels in iteration notes:

| Label | Meaning | First fix to try |
| --- | --- | --- |
| `PPU` | Chrome/icon line weight or sprite visual scale is wrong even when element bounds are right. | Tune Pixel Per Unit and reimport. |
| `9-slice` | Corners distort, borders stretch unevenly, center fill leaks, or edge thickness changes by panel size. | Tune sprite border and USS slice values. |
| `position` | Element belongs in same region but is shifted. | Adjust USS left/right/top/bottom/width/height. |
| `padding` | Children are too close/far inside a stable frame. | Adjust padding/gap/inset. |
| `font` | Text height, weight, alignment, or color differs. | Adjust font asset, size, style, color, line height. |
| `sprite` | Wrong chrome/icon/art asset. | Swap to correct Target Lock layer. |
| `state` | Selected/disabled/locked/active state differs. | Fix state class and state sprite. |
| `content` | Text/value differs from target or runtime source. | Decide target revision vs runtime-authoritative value. |
| `responsive` | Looks correct at one aspect but breaks at another. | Fix anchoring and percentage constraints. |
| `artifact` | Opaque box, bad alpha, compression blur, gutter, or background leak. | Fix source alpha/import/compression/gutters. |

## Per-Surface Status Table

| Surface | Status | Current iteration | Main blocker | Latest artifacts |
| --- | --- | ---: | --- | --- |
| Shell | Not started | 0 | None | None |
| SCN-01 Loading | Not started | 0 | None | None |
| SCN-02 Main Menu | Target matched | 1 | Approved by user; runtime scaling fixed by switching RuntimePanelSettings to UI Toolkit Scale With Screen Size (`m_ScaleMode: 2`) with 4800x2160 reference and Expand-style aspect handling (`m_ScreenMatchMode: 2`). Use SCN-02 lessons from `Design/Architecture/ui_toolkit_target_lock_mockup_conversion_playbook.md` for later screens. | `Assets/Game/Scripts/Editor/UiToolkitTargetLockStaticPreview.cs`; `Design/VisualLockLayered/_UIToolkitVisualMatch/target_to_toolkit_mapping.md`; `Design/VisualLockLayered/_UIToolkitVisualMatch/pixel_per_unit_audit.md`; `Design/VisualLockLayered/_UIToolkitVisualMatch/nine_slice_audit.md`; `Design/Architecture/ui_toolkit_target_lock_mockup_conversion_playbook.md`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/notes.md`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/shadow_ui_builder_static_scn02_typography_slice04.png`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/shadow_ui_builder_static_scn02_typography_slice04_canvas.png`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/target_vs_shadow_ui_builder_typography_slice04_contact.png`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_01/main_runtime_scale_with_screen_1920.png` |
| SCN-03 Commander Profile | Satisfied for current pass | 2 | Slice 02 removes broken stretched frame artifacts, applies SCN-02-style left rail chrome, and raises typography enough for focused crop readability. The content UXML does not duplicate the shared shell/header. | `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-03_CommanderProfile/iteration_01/notes.md`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-03_CommanderProfile/iteration_01/shadow_ui_builder_scn03_readability_slice02.png`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-03_CommanderProfile/iteration_01/shadow_ui_builder_scn03_left_nav_valid_slice02_crop.png`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-03_CommanderProfile/iteration_01/shadow_ui_builder_scn03_identity_valid_slice02_crop.png`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-03_CommanderProfile/iteration_01/shadow_ui_builder_scn03_right_valid_slice02_crop.png`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-03_CommanderProfile/iteration_01/shadow_ui_builder_scn03_footer_valid_slice02_crop.png` |
| SCN-08 Match HUD | Satisfied for current pass | 8 | Reopened after user review: squad tray cards were accepted from a weak readability crop but did not match the reference and the health/slider area was visually poor. Slice 08 removes the incorrect top health/progress strips, keeps health/status treatment at the bottom like the reference, and makes the selected/hover state a reusable class-driven highlight on the shared card frame. Existing runtime selection already moves `squad-card-selected` by `SelectedSlot`, so every squad card can receive the same selected visual state. Existing runtime bindings remain intact. | `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-08_MatchHud/iteration_01/notes.md`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-08_MatchHud/iteration_01/shadow_ui_builder_scn08_squad_tray_slice08_full.png`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-08_MatchHud/iteration_01/shadow_ui_builder_scn08_squad_tray_slice08_crop.png`; `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_NewMainMenuArtDirection_TargetLock_V02.png` |
| SCN-08 Build Placement Bar | Not started | 0 | None | None |
| SCN-09 Build Drawer Popup | Not started | 0 | None | None |
| SCN-19 Armory | Satisfied for current pass | 7 | Slice 13 fixes the right action buttons, stat label/value collision, and middle roster readability. Continue to SCN-03 unless a later precision pass is requested. | `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-19_Armory/iteration_01/notes.md`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-19_Armory/iteration_01/shadow_ui_builder_scn19_stats_columns_slice13_valid.png`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-19_Armory/iteration_01/shadow_ui_builder_scn19_right_panel_slice13_valid_crop.png`; `Design/VisualLockLayered/_UIToolkitVisualMatch/SCN-19_Armory/iteration_01/shadow_ui_builder_scn19_middle_cards_slice13_valid_crop.png`; `Design/VisualLockLayered/SCN-19_Armory/reference/SCN-19_Armory_NewMainMenuArtDirection_TargetLock_V04.png` |
| POP-05 Mission Result | Not started | 0 | None | None |
| POP-06 Settings | Needs target | 0 | Saved Target Lock reference missing | None |
| POP-07 Inbox | Needs target | 0 | Saved Target Lock reference missing | None |

## Suggested Work Order

Main-menu verification gate:

- [x] Complete SCN-02 Main Menu first.
- [x] Save final SCN-02 4800x2160 UI Builder/static preview, comparison contact sheet, focused crops, Pixel Per Unit notes, 9-slice notes, and validation result.
- [x] Mark SCN-02 as `Pending user verification`, not `Target matched`, until the user reviews it.
- [x] Stop implementation after the SCN-02 handoff and do not continue to SCN-19 or later surfaces until the user approves the main-menu result.
- [x] Document shared chrome override rules: reuse SCN-02 header/nav for menu screens, allow Match HUD its own header, and decompose baked multi-section right panels.
- [x] Document UI Builder screenshot validation rule: enable the `Match Game View` check-mark toggle, then click `Fit Viewport` before capture.

1. SCN-02 Main Menu, because it defines the main Target Lock chrome language for menu screens.
2. SCN-19 Armory, because it has dense cards, tabs, locked/selected states, and known chrome sensitivity.
3. SCN-03 Commander Profile, because it should share menu chrome after SCN-02/SCN-19.
4. SCN-08 Match HUD, because it is dense and should reuse the calibrated panel/button/icon settings.
5. SCN-09 Build Drawer Popup, because it depends on HUD/build chrome and has repeated catalog cards.
6. SCN-08 Build Placement Bar, because it is smaller and can reuse HUD/build button settings.
7. SCN-01 Loading, because it is visually isolated but still uses shared logo/progress chrome.
8. POP-05 Mission Result, because modal chrome should reuse the calibrated popup frame.
9. POP-06 and POP-07 only after saved targets exist.
10. Shell final pass after all screen-level region mounting is stable.

## Handoff Template

```markdown
# UI Toolkit Target Lock Visual Match Handoff - <SurfaceId> - YYYY-MM-DD

Surface:

Target reference:

Changed files:

Structure lock:
- Header/footer/left/right/middle preserved: yes/no
- UXML element rename/remove/move: none/list

Pixel Per Unit changes:
- Sprite:
- Old:
- New:
- Reason:

9-slice changes:
- Sprite/class:
- Values:
- Reason:

Iterations:
- Iteration 01:
  - UI Builder preview:
  - Game 16:9:
  - Game 20:9:
  - Comparison sheet:
  - Remaining differences:

Validation:
- `git diff --check`:
- Unity/EditMode:
- Manual visual QA:

Final status:
```

## Completion Criteria

- Every surface with a saved target is marked `Target matched`.
- POP-06 and POP-07 are either target-matched or explicitly blocked on missing saved targets.
- Header, footer, left, right, middle, modal, and shell slot structure is preserved.
- Pixel Per Unit and 9-slice audits are complete and linked.
- UI Builder and Game View comparison artifacts exist for every completed surface.
- `git diff --check` passes.
- Focused UI Toolkit validation passes.
- User-visible Target Lock chrome no longer shows obvious wrong scale, bad padding, distorted 9-slices, weak fonts, or bad panel alignment.

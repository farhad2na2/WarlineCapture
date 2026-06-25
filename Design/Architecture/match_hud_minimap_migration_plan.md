# Match HUD Local Minimap And Full-Screen Tactical Map Plan

Status: implementation in progress; first functional Canvas slice validated in Unity batchmode.
Updated: 2026-06-23

## Goal

Split the current Match HUD minimap into two separate map experiences:

- A compact HUD minimap that shows only the local area around the gameplay camera.
- A full-screen tactical map popup that reuses the current full-map behavior: full map coverage, camera viewport rectangle, map click/drag recentering, zoom controls, unit/building markers, and a close button.

The current minimap behaves like a full-screen tactical map compressed into the HUD. That makes the HUD minimap visually busy, expensive, and semantically wrong. The HUD slot should become a local situational minimap; the full-map controls should move into a modal popup.

## Progress Dashboard

Overall progress: 88% - 29 / 33 tracked items complete

Current phase: Phase 5 - Validation And Performance

Blocked: runtime smoke re-run is blocked while the main Unity editor has `/Users/farhad/Projects/WarlineCapture` open.

| Phase | Status | Progress | Done / Total | Evidence |
| --- | --- | ---: | ---: | --- |
| Phase 0 - Contract And Inventory | Complete | 100% | 5 / 5 | This document; current owners and split captured below. |
| Phase 1 - Data And Projection Split | In progress | 86% | 6 / 7 | `MatchHudMinimapView`, `MatchHudMinimapInputUiSystemHelper`; focused projection tests still pending. |
| Phase 2 - Compact HUD Minimap | In progress | 83% | 5 / 6 | HUD minimap uses camera-centered projection and hides zoom/viewport; performance smoke still pending. |
| Phase 3 - Full-Screen Tactical Map Popup | Complete | 100% | 7 / 7 | `SCN08_FullMapPopup.prefab`; larger build-popup-style chrome, close action, low-padding map well, and map fallback refresh. |
| Phase 4 - Runtime Binding And Input Flow | In progress | 75% | 3 / 4 | Shell and `MainMenuPlayUI` route open/close; gameplay command regression smoke still pending. |
| Phase 5 - Validation And Performance | In progress | 50% | 2 / 4 | Static warning/missing-script scans clean; Unity smoke re-run pending after the main editor releases the project lock. |

## Progress Update Rules

Every implementation update must update this document before reporting completion.

- Update `Updated`.
- Update `Overall progress`.
- Update the touched phase row.
- Mark phase status as `Not started`, `In progress`, `Blocked`, or `Complete`.
- Add evidence paths for completed work: code, prefab, test, validation log, or screenshot.
- A phase is complete only when every task and validation bullet in that phase is done.

## Architecture Contract Alignment

This feature must follow `Design/Architecture/gameplay_solid_ecs_contract.md`, `Design/Architecture/file_naming_architecture_contract.md`, and `Design/Architecture/performance_regression_contract.md`.

Rules for this feature:

- UI `*View` classes are passive serialized-reference holders. They may apply visual state and wire UI events to typed requests, but they must not own camera policy, marker policy, zoom policy, gameplay validation, or route/popup policy.
- No new runtime class or file names may contain or end with `Controller`, `Presenter`, `Bridge`, `Manager`, or gameplay-facing `Button`.
- Do not add new bare non-ECS `*System` helper classes. Bare `*System` remains reserved for Unity ECS `ISystem`/`SystemBase` types. Plain UI helpers must use an approved suffix such as `UiSystemHelper` or `PresentationSystemHelper`.
- Do not add runtime hierarchy path lookup, `GameObject.Find`, `FindObjectOfType`, `FindAnyObjectByType`, `Camera.main`, or broad scene scans. Runtime references must flow through serialized view fields, shell binding, ECS managed reference components, or existing injected boundaries.
- UI clicks must enqueue typed requests or call existing narrow command/UI boundaries. UI code must not mutate gameplay camera state directly.
- Pure coordinate/projection math should stay in static utility code or Burst-compatible ECS systems where practical.
- Managed Unity UI object mutation is allowed only at the UI edge. It must stay thin and mechanical.
- Prefer `ISystem` over `SystemBase` for any new ECS processing whenever the data is unmanaged and Burst-compatible. Use `SystemBase` only for the narrow managed UI/presentation edge that must touch Unity objects such as `GameObject`, `Image`, `Button`, `Sprite`, `RectTransform`, or popup/view references.
- Hot paths must not allocate per frame after warmup. Minimap marker and viewport updates need throttling, cached queries, and reusable buffers.
- Canvas is the active runtime target for this feature. UI Toolkit parity can be handled later, but new art or popup references must not pull old visual-lock assets into new UI Toolkit work.

## User Behavior Contract

### Compact HUD Minimap

- Shows a smaller camera-centered area, not the whole map.
- Shows local markers for visible/nearby relevant units, buildings, threats, and objective anchors.
- Does not show zoom buttons.
- Does not show a draggable camera viewport rectangle.
- Is clickable as one intentional surface.
- Clicking the compact minimap opens the full-screen tactical map popup.
- The click must be consumed by UI so no world selection/move/build action happens underneath.

### Full-Screen Tactical Map Popup

- Opens above the Match HUD and above other HUD content.
- Uses chrome/background style matching the Build Drawer popup.
- Has a close button matching the Build Drawer close button.
- Shows the full map.
- Shows the current camera viewport rectangle.
- Supports clicking/dragging the map or viewport to recenter the gameplay camera.
- Supports zoom controls using the existing camera zoom semantics.
- Shows the same runtime markers as the current full-map minimap, with readable scale.
- Closing returns to the Match HUD without changing command mode.

## Visual Direction

- Compact minimap remains inside the existing Match HUD right-side minimap area.
- Compact minimap chrome should stay lightweight so it does not look like a full tactical panel.
- Full-screen map popup should reuse Build Drawer modal language: dark brushed metal panel, gold chrome, clean close button, dimmed background, generous padding.
- Full-screen map must not be a single baked image; it uses the same runtime map capture/marker pipeline as the current minimap.
- If a new full-screen map frame or close-button state is missing, generate the required new-art-direction asset through the documented imagegen workflow and import it as a properly sliced sprite.

## Proposed Runtime Shape

Names are provisional and should be adjusted to existing local naming during implementation while preserving ownership.

### Views

- `MatchHudLocalMinimapView`
  - Passive Canvas view for the compact HUD minimap.
  - Serialized references: map image, marker root, click surface root.
  - No zoom button references.
  - No viewport-rect drag references.
  - Emits `OpenFullMapRequested`.

- `MatchHudFullMapPopupView`
  - Passive Canvas view for the full-screen map popup.
  - Serialized references: popup root, frame, map image, marker root, viewport rect, zoom controls, close action.
  - Emits close, focus, drag, and zoom-held events through injected callbacks/request boundaries.

- Existing `MatchHudMinimapView`
  - Treat as current full-map behavior source.
  - During implementation, either adapt it into the full-screen popup view or split it into the two views above.
  - Do not expand it into a mixed compact/fullscreen policy owner.

### Read Models And Requests

- Compact minimap read model:
  - camera-centered projection grid
  - local marker positions
  - static map capture state
  - click-enabled state

- Full-screen map read model:
  - full-map projection grid
  - camera viewport rect
  - full marker set
  - zoom enabled state
  - popup open/closed state

- Requests:
  - `OpenFullMap`
  - `CloseFullMap`
  - `FullMapFocus`
  - `FullMapZoomHeld`

These requests should flow through the existing UI shell popup/action boundary or a narrow Match HUD UI request buffer. Camera focus and zoom execution must reuse existing camera command boundaries, not direct UI camera mutation.

Read-model and request-processing systems should be `ISystem` by default. If any proposed map system needs `SystemBase`, document the exact managed dependency and keep that type out of simulation/hot-path policy.

### Projection Ownership

- Local minimap projection: camera-centered map window around the gameplay camera.
- Full-screen map projection: full grid/map extents.
- Coordinate conversion math should be shared, but mode selection should be data-driven and explicit.
- Marker filtering differs by mode:
  - local minimap filters to a local camera-centered area and clamps/fades edge markers if needed.
  - full-screen map shows the full marker set.

## Implementation Plan

### Phase 0 - Contract And Inventory

- [x] Inventory current `MatchHudMinimapView`, `MatchHudMinimapInputUiSystemHelper`, `MatchHudMinimapProjectionSystem`, marker read model, and shell binding.
- [x] Inventory Build Drawer popup chrome, close button, modal overlay, and popup motion ownership.
- [x] Identify which current minimap behavior moves to full-screen map and which remains in HUD.
- [x] Decide whether to split or rename the existing non-ECS `MatchHudMinimapInputUiSystemHelper` so no new bare non-ECS `*System` debt is added.
- [x] Write exact prefab hierarchy changes for `SCN08_MatchHudContent` and the new full-screen popup prefab.

Done criteria:

- Current owners and target owners are listed in this document or a linked AgentReport.
- No implementation starts until the split and naming decision is documented.

### Phase 1 - Data And Projection Split

- [x] Add explicit projection mode data for local camera-centered minimap vs full-map tactical map.
- [x] Reuse or extract common coordinate conversion math without putting gameplay policy in views.
- [x] Add local marker filtering/clamping rules for the compact minimap.
- [x] Preserve full marker projection for the full-screen map.
- [x] Ensure camera focus and zoom requests still route through existing camera/selection command boundaries.
- [x] Prefer `ISystem` for projection/read-model/request processing; document any unavoidable `SystemBase` exception before adding it.
- [ ] Add focused tests for local projection, full projection, marker filtering, and click-to-world conversion.

Done criteria:

- Projection tests pass.
- No new UI view owns camera math beyond forwarding pointer coordinates.

### Phase 2 - Compact HUD Minimap

- [x] Replace HUD minimap behavior with local camera-area projection.
- [x] Remove/hide zoom controls from the HUD minimap.
- [x] Remove/hide viewport rectangle from the HUD minimap.
- [x] Add a click surface on the conceptual minimap root, not on a hidden proxy child.
- [x] Ensure HUD minimap click opens the full-screen map request and blocks world click-through.
- [ ] Keep compact minimap update cadence cheap: no per-frame static map capture, no per-frame marker allocation.

Done criteria:

- HUD minimap shows local area only.
- No zoom buttons or camera rect are visible in the HUD minimap.
- Clicking it opens the full-screen map and does not select/move/build in the world.

### Phase 3 - Full-Screen Tactical Map Popup

- [x] Create a full-screen map popup prefab using Build Drawer-style chrome.
- [x] Add a close action styled like the Build Drawer close button.
- [x] Move full-map background capture and marker rendering into this popup.
- [x] Show the current camera viewport rectangle only in the full-screen popup.
- [x] Support click/drag recentering only in the full-screen popup.
- [x] Support zoom controls only in the full-screen popup.
- [x] Make popup layer render above Match HUD and other popups according to shell popup ordering.

Done criteria:

- Popup opens full-screen with full map coverage.
- Close button only closes the popup.
- Popup controls do not affect command mode except consuming pointer input.

### Phase 4 - Runtime Binding And Input Flow

- [x] Bind compact minimap and full-screen map popup through shell/runtime composition using serialized references.
- [x] Add typed open/close/focus/zoom request routing.
- [x] Ensure UI input blocking works over both the compact minimap and full-screen popup.
- [ ] Confirm Match HUD selection/move/attack/build flows continue working after closing the popup.

Done criteria:

- No runtime hierarchy search or global scene lookup is added.
- All click paths are deterministic and typed.

### Phase 5 - Validation And Performance

- [x] Unity compile: no errors, no new warnings.
- [x] Focused EditMode/execute-method tests for projection and prefab wiring.
- [ ] Runtime smoke: open Match, click compact minimap, full-screen map opens, pan/zoom/focus works, close returns to HUD.
- [ ] Performance validation: compare Match FPS before/after, and confirm minimap update path does not reintroduce recurring GC allocation or large Canvas rebuild spikes.

Done criteria:

- Match HUD remains usable.
- Full-screen map behavior matches current full-map minimap behavior.
- Compact minimap is cheaper and cleaner than the current full-map HUD minimap.

## Validation Commands

Exact commands should be filled in during implementation. Expected validation set:

- Focused projection tests for `MatchHudMinimapProjectionSystem` or the renamed projection owner.
- Focused prefab wiring tests for `SCN08_MatchHudContent` and the new full-screen map popup.
- Canvas Match smoke/FPS validation.
- Architecture guardrails:
  - assembly boundary validation
  - forbidden naming validation
  - Burst/hot-path architecture validation if any ECS hot path is touched

Completed validation:

- `git diff --check`
- `rg -n "class .*Controller|class .*Presenter|class .*Bridge|class .*Manager|class .*Button|GameObject\\.Find|FindObjectOfType|FindAnyObjectByType|Camera\\.main" Assets/Game/Scripts/UI/Screens/MatchHudFullMapPopupView.cs Assets/Game/Scripts/Editor/MatchHudFullMapPopupPrefabSetup.cs Assets/Game/Scripts/UI/Screens/MatchHudMinimapView.cs Assets/Game/Scripts/UI/Screens/MatchHudMinimapInputUiSystemHelper.cs Assets/Game/Scripts/UI/MainMenuPlayUI.cs Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod MatchHudFullMapPopupPrefabSetup.Validate -logFile /tmp/warlinecapture-fullmap-popup-validate.log`
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod MatchRuntimeShellSmokeValidation.Run -logFile /tmp/warlinecapture-fullmap-match-smoke.log`
- `rg -n "m_Script: \\{fileID: 0\\}|warning CS|error CS|The referenced script|Missing|missing|Exception" Assets/Game/Prefabs/UI/Shell/Popups/SCN08_FullMapPopup.prefab /Users/farhad/Library/Logs/Unity/Editor.log`

Validation result:

- Unity validation passed and logged `[MatchHudFullMapPopupPrefabSetup] Validation passed.`
- Menu-to-Match shell smoke passed and logged `[MatchRuntimeShellSmokeValidation] result=Passed mode=MatchHud route=Match phase=MatchHudReady ...`.
- No compiler errors or warnings were reported by the filtered validation log.
- The Match smoke log includes a Unity/editor shutdown `NullReferenceException` after the pass marker; the feature smoke result itself passed before shutdown.
- The full-map popup was updated so the close action is larger, the map frame fills more of the panel, initial map color is not black, full-map opening forces a fresh static map refresh, and overly dark live captures fall back to the generated terrain map image.
- Current static scan reports no missing script references in `SCN08_FullMapPopup.prefab` and no matching compiler/editor warnings in the active editor log.

Attempted validation not yet accepted:

- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod MatchHudMinimapProjectionSystemTests.RunFocusedValidation -logFile /tmp/warlinecapture-fullmap-minimap-projection.log`
- Result: failed existing `CameraProjectionHelpersDoNotAllocateAfterWarmup` allocation assertion in `Assets/Tests/Editor/MatchHudMinimapProjectionSystemTests.cs:179`. The failure is in the focused projection allocation validation and remains part of the Phase 1/5 validation gap.
- `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod MatchHudFullMapPopupPrefabSetup.Apply -logFile /tmp/warlinecapture-fullmap-popup-apply.log`
- Result: blocked because another Unity instance currently has `/Users/farhad/Projects/WarlineCapture` open. The popup prefab was patched directly to match the generator values; re-run the apply/validate/smoke once the editor releases the project lock.

## Open Decisions

- Whether the full-screen map popup should be a shell popup route or a Match HUD-owned overlay. Recommendation: shell popup route if it needs modal ordering above other popups; Match HUD-owned overlay only if it is strictly in-match and never coexists with other shell popups.
- Whether to rename/split current non-ECS `MatchHudMinimapInputUiSystemHelper` during this feature. Recommendation: split now because the feature changes its responsibility anyway.
- Whether compact minimap static capture should reuse the full-map texture cropped by projection, or render a local capture. Recommendation: reuse one cached full-map/static source when possible and crop/project locally, to avoid expensive capture churn.

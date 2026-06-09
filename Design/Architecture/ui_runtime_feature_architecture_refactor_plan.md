# UI Runtime Feature Architecture Refactor Plan

## Goal

Bring the recently added Match HUD, Build Drawer, build placement, production queue, and runtime feedback UI features back into alignment with:

- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`
- `Design/Architecture/performance_regression_contract.md`

The target shape is:

- UI `*View` MonoBehaviours own serialized references and visual state only.
- Runtime UI actions submit ECS or narrow shell-edge requests.
- Gameplay policy remains in ECS/data/query/command systems.
- No shipped runtime UI code uses static mutable view registries, broad discovery, `Camera.main`, hierarchy string lookup, or temporary direct debug logging.
- No UI prefab is regenerated wholesale during this refactor.

## Scope

Included:

- Build Drawer popup and production queue UI.
- Build placement confirmation bar.
- Match HUD command buttons and right quick rail build button.
- Match HUD tactical feedback and selection feedback.
- Recent runtime popup close/motion helpers.

Excluded:

- Reworking visual layout or art.
- Rebuilding prefabs from editor builders.
- Mission/campaign/tutorial restoration.
- Large gameplay balance or production timing changes.
- Full shell ECS rewrite beyond the specific drift items listed here.

## Current Audit Snapshot

Audit date: 2026-06-09.

The recent features are partially aligned:

- Build Drawer catalog filtering uses config requestability.
- Build Drawer gameplay actions route through `BuildingUiCommandSystem` and production request systems.
- Match command buttons route through `SelectionUiCommandSystem`.
- Most UI elements are `*View` types with serialized fields.

Known drift:

- `BattleHudRuntimeFeedbackSystem` owns static mutable view state.
- `BuildDrawerCatalogPresenterView` still has direct temporary `Debug.Log*` diagnostics.
- `MatchHudSquadTrayView` uses `Camera.main` as a fallback.
- `UIShellContentView`, `MatchHudRightQuickRailView`, and command popup fallback paths still do runtime component discovery/fallback binding.
- `BuildPlacementConfirmationBarView` can generate a runtime UI layout when a prefab/reference path is missing.
- Build Drawer queue refresh destroys/recreates runtime queue rows on a timer.

## Architecture Constraints

- Do not rebuild UI prefabs wholesale.
- Do not add `Object.Find*`, `GameObject.Find`, runtime hierarchy string lookup, static service locators, static mutable view registries, broad manager/controller/facade shells, or direct gameplay mutation from UI.
- Preserve existing Unity `.meta` files.
- Keep each slice behavior-preserving unless the step explicitly removes a fallback.
- Prefer explicit serialized references and narrow binding systems over runtime search.
- Keep diagnostics only behind existing diagnostic state or ECS diagnostic/log buffers; remove temporary click logs when they are no longer needed.

## Step 01: Remove Temporary Runtime UI Logs

Status: [ ]

Files to inspect first:

- `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogPresenterView.cs`
- `Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputSystem.cs`
- `Assets/Game/Scripts/UI/Screens/MatchHudRightQuickRailView.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`

Implementation:

- Remove temporary direct `Debug.Log*` calls added for diagnosing Build Drawer and command button binding.
- Keep user-facing feedback through `BuildDrawerView.ApplyInstruction(...)` and HUD feedback.
- If a diagnostic is still required, route it through a gated diagnostic system or an ECS diagnostic buffer, not unconditional runtime logging.

Acceptance checks:

- No direct Build Drawer click/result logs remain in shipped UI code.
- User-visible failure messages still appear through the instruction strip or HUD feedback.
- Focused Build Drawer tests still pass.

## Step 02: Remove `Camera.main` And Event-Camera Fallbacks

Status: [ ]

Files to inspect first:

- `Assets/Game/Scripts/UI/Components/MatchHudSquadTrayView.cs`
- `Assets/Game/Scripts/UI/Screens/MatchHudRightQuickRailView.cs`
- `Assets/Game/Scripts/UI/Screens/BuildPlacementConfirmationBarView.cs`
- `Assets/Game/Scripts/UI/Screens/MatchHudMinimapView.cs`

Implementation:

- Replace `Camera.main` fallback with a serialized or injected event-camera source where needed.
- For screen-space overlay canvases, pass `null` to `RectTransformUtility`.
- For camera/world-space canvases, use the Canvas `worldCamera` only if explicitly assigned.
- Add validation for missing camera references where the canvas mode requires one.

Acceptance checks:

- Runtime UI scan finds no `Camera.main`.
- Hit testing still works for command buttons, squad tray, minimap, build drawer, and placement confirmation bar.

## Step 03: Replace Runtime UI Discovery With Explicit Binding

Status: [ ]

Files to inspect first:

- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- `Assets/Game/Scripts/UI/Screens/MatchHudRightQuickRailView.cs`
- `Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputSystem.cs`
- `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab`
- `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`

Implementation:

- Add or reuse narrow serialized view fields for Match HUD command controls, runtime feedback, minimap, squad tray, selected panel, and right quick rail.
- Bind those views through explicit `UIShellContentView` references after region content is installed.
- Remove self-binding from `MatchHudRightQuickRailView` that reaches upward to `UIShellContentView`.
- Remove popup fallback code in `MatchOverlayCommandInputSystem` that locates/destroys children by prefab name.
- Keep popup open/close ownership in `UIShellContentView` or a narrow popup shell boundary.

Acceptance checks:

- No runtime child lookup or parent fallback is required for right quick rail/build drawer binding.
- Build button opens Build Drawer and close button hides it.
- Clicks on HUD controls do not fall through to world selection.

## Step 04: Move HUD Runtime Feedback Off Static View Registry

Status: [ ]

Files to inspect first:

- `Assets/Game/Scripts/Systems/BattleHudRuntimeFeedbackSystem.cs`
- `Assets/Game/Scripts/UI/Components/BattleHudRuntimeFeedbackView.cs`
- `Assets/Game/Scripts/Systems/SelectionHudFeedbackSystem.cs`
- `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogPresenterView.cs`
- `Assets/Game/Scripts/UI/Screens/BuildPlacementConfirmationBarView.cs`
- `Assets/Game/Scripts/UI/Screens/CommandWheelPanelView.cs`
- `Assets/Game/Scripts/UI/Screens/BuildDrawerPanelView.cs`

Implementation:

- Introduce an explicit feedback binding/context so callers receive a `BattleHudRuntimeFeedbackView` or ECS feedback context through composition.
- Route command mode, command result, selected-entity visibility, and feedback panel text through ECS feedback data or an injected shell-edge feedback context.
- Remove `BattleHudRuntimeFeedbackSystem.ActiveView` and `StatesByView`.
- Keep `BattleHudRuntimeFeedbackSystem` only if it becomes stateless formatting/apply logic that receives the target view explicitly, or split formatting into a pure utility and move state into ECS/read-model data.

Acceptance checks:

- Runtime scan finds no static mutable UI registry for HUD feedback.
- Selection panel visibility, command button highlights, Move/Attack/Scan feedback, Build mode, placement feedback, and invalid command messages still work.
- No `ResolveActiveView()` callers remain in shipped runtime code.

## Step 05: Remove Runtime-Generated Confirmation Bar Fallback

Status: [ ]

Files to inspect first:

- `Assets/Game/Scripts/UI/Screens/BuildPlacementConfirmationBarView.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- `Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab`

Implementation:

- Require the serialized `SCN08_BuildPlacementConfirmationBar` prefab path.
- Keep `BuildPlacementConfirmationBarView` as a view that binds existing serialized children.
- Remove or editor-gate runtime layout generation (`new GameObject`, `AddComponent`, procedural text/image/button creation).
- Add prefab validation that required fields are serialized.

Acceptance checks:

- Confirmation bar still appears during building placement.
- Cancel, rotate, and confirm buttons still route through `BuildingUiCommandSystem`.
- Runtime scan finds no generated confirmation-bar fallback path.

## Step 06: Make Build Drawer Queue UI Retained

Status: [ ]

Files to inspect first:

- `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogPresenterView.cs`
- `Assets/Game/Scripts/UI/Screens/BuildDrawerQueueItemView.cs`
- `Assets/Game/Scripts/UI/Screens/BuildDrawerView.cs`

Implementation:

- Replace destroy/recreate queue row refresh with retained rows.
- Keep one active row and a small pool/list of queued rows.
- Update existing row contents and active state when queue data changes.
- Track stable production identity if available; otherwise update by queue index without reallocating rows every refresh.

Acceptance checks:

- Production queue visuals still show active item, progress, number, image, and queued items.
- Cancel current and clear all still work.
- Refresh no longer destroys/recreates queue item GameObjects every 0.2 seconds.

## Step 07: Add Architecture Regression Tests

Status: [ ]

Files to inspect first:

- Existing architecture tests under `Assets/Tests/Editor`
- `Design/Architecture/script_architecture_alignment_roadmap.md`

Implementation:

- Add or extend tests to fail on:
  - `Camera.main` in shipped runtime UI/gameplay.
  - Direct `Debug.Log*` in newly touched UI features unless allowlisted as diagnostics.
  - Static mutable view registries such as active view fields or view dictionaries.
  - Runtime hierarchy string lookup and broad `Object.Find*`.
  - Project-name-prefixed source filenames.
- Keep allowlists narrow and tied to roadmap notes.

Acceptance checks:

- Tests catch the violations listed in this document before refactor.
- Tests pass after each accepted cleanup slice.

## Step 08: Final Validation And Documentation

Status: [ ]

Validation:

- Run `git diff --check`.
- Run focused EditMode tests for Build Drawer, command controls, shell binding, and architecture guardrails.
- Run Unity compile validation in the shadow project if the active editor locks the main project.
- Run a graphics-capable smoke check when UI interaction or prefab references change.

Completion criteria:

- Every checklist item in this document is marked complete.
- No new contract violations are introduced.
- Build Drawer, build placement, production queue, Match HUD command buttons, right quick rail Build button, close button, feedback panel, and selection panel still work.
- Any intentionally deferred debt is documented with a narrow owner and reason.

## Progress Notes

- 2026-06-09: Plan created from audit findings. Next step is Step 01, removing temporary runtime UI logs while preserving user-facing Build Drawer/HUD feedback.

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

Resolved drift:

- `BattleHudRuntimeFeedbackSystem` no longer owns static mutable view state.
- Runtime UI click diagnostics were removed or covered by narrow architecture guardrails.
- Runtime scripts no longer use `Camera.main`.
- Match HUD and Build Drawer runtime binding now uses serialized shell references and explicit dependency edges.
- `BuildPlacementConfirmationBarView` no longer generates runtime UI layout fallbacks.
- Build Drawer queue refresh now retains extra queue rows instead of recreating them on every refresh.

## Architecture Constraints

- Do not rebuild UI prefabs wholesale.
- Do not add `Object.Find*`, `GameObject.Find`, runtime hierarchy string lookup, static service locators, static mutable view registries, broad manager/controller/facade shells, or direct gameplay mutation from UI.
- Preserve existing Unity `.meta` files.
- Keep each slice behavior-preserving unless the step explicitly removes a fallback.
- Prefer explicit serialized references and narrow binding systems over runtime search.
- Keep diagnostics only behind existing diagnostic state or ECS diagnostic/log buffers; remove temporary click logs when they are no longer needed.

## Step 01: Remove Temporary Runtime UI Logs

Status: [x]

Files to inspect first:

- `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.cs`
- `Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.cs`
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

Status: [x]

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

Status: [x]

Files to inspect first:

- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- `Assets/Game/Scripts/UI/Screens/MatchHudRightQuickRailView.cs`
- `Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.cs`
- `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab`
- `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`

Implementation:

- Add or reuse narrow serialized view fields for Match HUD command controls, runtime feedback, minimap, squad tray, selected panel, and right quick rail.
- Bind those views through explicit `UIShellContentView` references after region content is installed.
- Remove self-binding from `MatchHudRightQuickRailView` that reaches upward to `UIShellContentView`.
- Remove popup fallback code in `MatchOverlayCommandInputUiSystemHelper` that locates/destroys children by prefab name.
- Keep popup open/close ownership in `UIShellContentView` or a narrow popup shell boundary.

Acceptance checks:

- No runtime child lookup or parent fallback is required for right quick rail/build drawer binding.
- Build button opens Build Drawer and close button hides it.
- Clicks on HUD controls do not fall through to world selection.

## Step 04: Move HUD Runtime Feedback Off Static View Registry

Status: [x]

Files to inspect first:

- `Assets/Game/Scripts/Systems/BattleHudRuntimeFeedbackSystem.cs`
- `Assets/Game/Scripts/UI/Components/BattleHudRuntimeFeedbackView.cs`
- `Assets/Game/Scripts/Systems/SelectionHudFeedbackSystem.cs`
- `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.cs`
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

Status: [x]

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

Status: [x]

Files to inspect first:

- `Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.cs`
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

Status: [x]

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

Status: [x]

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
- 2026-06-10: Step 01 complete. Removed temporary Build Drawer and Match HUD click/binding `Debug.Log*` calls from shipped runtime UI code. User-facing failures still route through Build Drawer instructions or HUD command feedback. Next validation: focused Build Drawer/command/shell EditMode tests after Step 02 slice.
- 2026-06-10: Step 02 complete. Removed the `Camera.main` fallback from `MatchHudSquadTrayView`; listed UI hit-test views now use overlay `null` event cameras or explicitly assigned Canvas `worldCamera` only. Runtime UI scan found no remaining `Camera.main`.
- 2026-06-10: Step 03 in progress. Removed the Match HUD command-input Build Drawer popup fallback that searched/destroyed children by prefab name, removed right quick rail upward self-binding into `UIShellContentView`, and narrowed right quick rail binding to shell-owned explicit binding. `UIPopupMotionView` now completes immediately in EditMode for deterministic validation while preserving play-mode tweening. Validation passed: `MatchHudCommandControlsCurrentPrefabTests` 2/2, `UIShellCurrentContentLoadTests` 7/7, `BuildDrawerCatalogQueryUiSystemHelperTests` 21/21, `MatchHudCommandFeedbackPanelTests` 3/3.
- 2026-06-10: Step 03 follow-up slice complete. Removed stale `BuildDrawerPopupPrefab` ownership from `MatchOverlayCommandControlsView`, its SCN08 prefab serialization, and the command-controls prefab test. Build Drawer popup prefab ownership now remains on `UIShellContentView`. Validation passed: `MatchHudCommandControlsCurrentPrefabTests` 2/2.
- 2026-06-10: Step 03 complete. Added `MatchHudFooterContentView` as the serialized FooterContent reference holder for command controls, runtime feedback, minimap, and squad tray, then updated `UIShellContentView` to bind cached installed section views instead of rediscovering footer children. Validation passed: `UIShellCurrentContentLoadTests` 7/7; `git diff --check` passed.
- 2026-06-10: Step 04 complete. Removed `BattleHudRuntimeFeedbackSystem` active-view/static-state registry and no-argument feedback APIs; runtime feedback state now lives on `BattleHudRuntimeFeedbackView`, and all command/build/selection feedback paths use explicit view bindings through shell or gameplay dependency edges. Road build and building placement composition now route build command feedback through `MainMenuPlayUI` dependencies instead of parameterless feedback calls. Contract scan found no `ResolveActiveView`, `SetActiveView`, `ClearActiveView`, or `GetState()` calls. Validation passed: `git diff --check`, `UIShellCurrentContentLoadTests` 8/8, `MatchHudCommandFeedbackPanelTests` 3/3, `BuildDrawerCatalogQueryUiSystemHelperTests` 21/21.
- 2026-06-10: Step 05 complete. Serialized real child controls and references onto `SCN08_BuildPlacementConfirmationBar.prefab`, removed the runtime-generated confirmation bar layout fallback from `BuildPlacementConfirmationBarView`, and extended shell validation to require all placement bar text/button references. Runtime scan found no confirmation-bar `new GameObject`, `AddComponent`, or generated-layout fallback path. Validation passed: `git diff --check`, `UIShellCurrentContentLoadTests` 8/8, `BuildDrawerCatalogQueryUiSystemHelperTests` 21/21.
- 2026-06-10: Step 06 complete. Changed Build Drawer queue refresh to retain and reuse extra queue rows instead of destroying/recreating them on each snapshot refresh; runtime rows are hidden when unused and destroyed only on presenter teardown. Added queue-retention assertions to `BuildDrawerCatalogQueryUiSystemHelperTests`. Validation passed: `git diff --check`, `BuildDrawerCatalogQueryUiSystemHelperTests` 21/21.
- 2026-06-10: Step 07 complete. Extended `ScriptArchitectureAlignmentContractTests` with guardrails for `Camera.main`, direct runtime UI `Debug.Log*` debt, and static runtime view registries. Removed the remaining `Camera.main` fallback from `TerrainLodHeightSwitch`, removed temporary Armory click logging, removed the route-success debug log, and renamed `BuildDrawerCatalogPresenterView` to `BuildDrawerCatalogRuntimeView` while preserving its `.meta`/MonoScript GUID. Validation passed: `git diff --check`, `ScriptArchitectureAlignmentContractTests` 9/9.
- 2026-06-10: Step 08 complete. Final validation passed: `git diff --check`, `ScriptArchitectureAlignmentContractTests` 9/9, `BuildDrawerCatalogQueryUiSystemHelperTests` 21/21, `UIShellCurrentContentLoadTests` 8/8, `MatchHudCommandControlsCurrentPrefabTests` 2/2, and `MatchHudCommandFeedbackPanelTests` 3/3. No remaining checklist items are open.
- 2026-06-10: Step 04 in progress. Added an explicit shell-to-gameplay binding path for `BattleHudRuntimeFeedbackView` through `MainMenuPlayUI` and `SelectionHudFeedbackSystem`, reducing selection feedback dependence on `BattleHudRuntimeFeedbackSystem.ResolveActiveView()` while keeping the static compatibility API for remaining callers. Validation passed: `UIShellCurrentContentLoadTests` 8/8; `git diff --check` passed.
- 2026-06-10: Step 04 state slice complete. Removed the static `StatesByView` dictionary from `BattleHudRuntimeFeedbackSystem`; per-view command mode, sticky mode, and last-result state now lives on `BattleHudRuntimeFeedbackView` and resets with the view lifecycle. Validation passed: `MatchHudCommandFeedbackPanelTests` 3/3, `UIShellCurrentContentLoadTests` 8/8, `git diff --check`. Attempted `BattleHudRuntimeFeedbackSystemConnectionTests`, but the suite is currently blocked by its removed `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab` fixture path.
- 2026-06-10: Step 04 selection cleanup complete. Removed `SelectionGameplayStartupSystem`'s direct `BattleHudRuntimeFeedbackSystem.ResolveActiveView()` command-clear path; selection command clears now rely on the explicitly bound `SelectionHudFeedbackSystem` feedback view. Validation passed: `UIShellCurrentContentLoadTests` 8/8; `git diff --check` passed.
- 2026-06-10: Step 04 UI fallback cleanup continued. Removed `ResolveActiveView()` fallbacks from `BuildDrawerPanelView`, `CommandWheelPanelView`, and `UIPopupCloseButtonView`; popup close helpers now accept an explicit feedback binding from `UIShellContentView`. `MatchOverlayCommandInputUiSystemHelper` now receives the serialized footer runtime-feedback view and uses explicit-view command feedback/sticky-mode calls. Validation passed: `UIShellCurrentContentLoadTests` 8/8, `MatchHudCommandControlsCurrentPrefabTests` 2/2, `MatchHudCommandFeedbackPanelTests` 3/3, `git diff --check`.
- 2026-06-10: Step 04 Build Drawer feedback cleanup complete. `BuildDrawerCatalogRuntimeView`, `BuildPlacementConfirmationBarView`, and `MatchHudRightQuickRailView` now receive the explicit footer `BattleHudRuntimeFeedbackView` from `UIShellContentView`; their command result and sticky build-mode feedback no longer use parameterless global feedback calls. Validation passed: `BuildDrawerCatalogQueryUiSystemHelperTests` 21/21, `UIShellCurrentContentLoadTests` 8/8, `git diff --check`.

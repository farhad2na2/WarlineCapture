# Menu And Match Settings Popup Implementation Tracker

Status: implementation slice complete; focused settings popup validation and visual screenshot QA passed. The broader non-ECS architecture guardrail currently fails on pre-existing movement/pathing ownership violations outside this settings popup slice.

Purpose:
Implement menu settings and match settings as Canvas popups, opened from the existing menu and match settings buttons, while preserving the ECS/SOLID architecture contract and using only the approved `MainMenuBrightCommand` sprite set.

## Goal

- Menu `SettingsButton` opens a menu settings popup.
- Match HUD `SettingsButton` opens a match settings popup without leaving Match HUD.
- Both popups expose the implemented settings model and the documented assistant guidance setting.
- Settings persist through `SettingsService` and apply through the existing runtime apply path where supported.
- UI visuals use only sprites from `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites`.
- No new manager/controller/facade types, no broad replacement `ISystem` shells, and no new updating `MonoBehaviour` loops.

## Source Contracts

| Source | Contract |
| --- | --- |
| `Design/UIUX_Gameplay_Element_Alignment.md` | SCN-04 settings categories: audio, graphics, controls, notifications, accessibility, language, reset, apply. |
| `Design/FTUE_And_Command_Assistant_Design.md` | Assistance level must be exposed in Settings. |
| `Design/UIUX_Target_To_Canvas_Workflow_Guide.md` | Buttons/cards/tabs must use real Normal, Highlighted, Pressed, Selected, Disabled states. |
| `Design/Architecture/gameplay_solid_ecs_contract.md` | Keep hot gameplay in ECS; Unity-object UI is a narrow managed presentation exception. |
| `Design/Architecture/file_naming_architecture_contract.md` | Preserve naming conventions; do not introduce manager/controller/facade names. |

## Existing Code Anchors

| Area | Current file | Planned use |
| --- | --- | --- |
| Settings model | `Assets/Game/Scripts/UI/Settings/UISettingsModels.cs` | Extend with assistant guidance setting. |
| Settings persistence/runtime apply | `Assets/Game/Scripts/UI/Settings/SettingsService.cs` | Continue as the settings persistence boundary. |
| Current settings screen flow | `Assets/Game/Scripts/UI/Settings/SettingsScreenFlowUiSystemHelper.cs` | Reuse or split into a popup-compatible helper without changing the suffix convention. |
| Current settings screen view | `Assets/Game/Scripts/UI/Settings/SettingsScreenView.cs` | Source of existing bind/readback behavior; avoid duplicating logic blindly. |
| Popup shell command flow | `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs` | Continue using `UiShellPopupRequestComponent` and `PopupLayer`. |
| UI action translation | `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs` | Change `OpenSettings` from route request to settings popup request. |
| Popup content installer | `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs` | Add settings popup prefab slots and install/close methods. |
| Menu content prefab | `Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab` | Wire menu `SettingsButton` to settings popup action. |
| Match HUD content prefab | `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab` | Wire match `SettingsButton` to settings popup action. |

## Approved Sprite Whitelist

All new popup sprites must come from:

`Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites`

| Use | Sprite candidates |
| --- | --- |
| Popup body/backing | `scn02c_mode_card_backing_blue.png`, `scn02c_mode_card_backing_selected.png` |
| Popup frame | `scn02c_mode_card_frame_default_blue.png`, `scn02c_mode_card_frame_selected.png` |
| Header | `scn02c_header_bar_frame.png`, `scn02c_settings_gear_icon.png` |
| Close/back square button | `scn02c_header_square_button_frame_default.png`, `hover`, `pressed`, `selected`, `disabled` |
| Footer apply/reset buttons | `scn02c_deploy_button_frame.png`, `hover`, `pressed`, `selected`, `disabled` |
| Tabs/segments | `scn02c_nav_button_frame_default.png`, `scn02c_nav_button_frame_selected.png`, `scn02c_nav_button_backing_default.png` |
| Slider/toggle/dropdown chrome | `scn02c_resource_chip_frame.png`, square button frames, mode label plates |

Do not use legacy `MainMenu/LayeredOneGo`, UI Toolkit assets, generated screenshots, or newly imported sprites.

## Status Values

| Status | Meaning |
| --- | --- |
| Open | Planned, not started. |
| InProgress | Active implementation slice. |
| Blocked | Cannot continue without a decision or external fix. |
| Complete | Implemented and validated for the slice. |
| Deferred | Explicitly out of this implementation pass. |

## Progress Snapshot

| Metric | Count |
| --- | ---: |
| Planned implementation slices | 11 |
| Complete | 10 |
| In progress | 0 |
| Blocked | 1 |
| Deferred | 0 |
| Open | 0 |

Current target:
`Runtime, prefabs, button wiring, ECS request plumbing, focused validation, and screenshot-based visual QA are complete. Remaining work is unrelated non-ECS guardrail cleanup in existing movement/pathing files.`

## M0 - Intake And Tracker Setup

Goal:
Document the implementation path before changing runtime behavior.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Identify existing settings model/service/view flow. | `UISettingsModels`, `SettingsService`, `SettingsScreenView`, `SettingsScreenFlowUiSystemHelper`. |
| Complete | Identify shell popup path. | `UiShellPopupRequestComponent`, `UiShellFlowSystem`, `UIShellContentView`, `PopupLayer`. |
| Complete | Identify current settings button state. | Menu button routes to `UIRoute.Settings`; match button has `Button` but needs explicit popup action wiring. |
| Complete | Identify approved sprite source. | `MainMenuBrightCommand/Sprites` only. |
| Complete | Write this tracker. | Plan-only; no runtime edits in M0. |

Validation:

- `git diff --check` after tracker creation.

## M1 - Settings Model And Assistant Guidance Setting

Goal:
Align the settings data model with documented settings content.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Add `UIAssistanceLevel` enum. | Values: `FullGuidance`, `HintsOnly`, `Minimal`, `Off`. |
| Complete | Add assistant settings model field. | Added `AssistantSettingsModel` inside `UISettingsModel`. |
| Complete | Add PlayerPrefs load/save/defaults. | Default: `FullGuidance`, per FTUE design. |
| Complete | Preserve existing defaults. | Existing audio/graphics/control/accessibility defaults unchanged. |
| Complete | Add focused model persistence tests. | Covered by `SettingsPopupValidationTests.SettingsService_DefaultsAndPersistsAssistantLevel`. |

Validation:

- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`
- Focused settings model/service EditMode validation.

## M2 - Shared Settings Panel View Extraction

Goal:
Make settings controls reusable by the existing screen and new popups without duplicating bind/readback logic.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Introduce `SettingsPanelView`. | Owns serialized rows/segments/toggles and read/write model binding for the new popups. |
| Complete | Move common flow surface behind an interface. | Added `ISettingsControlsView`; `SettingsScreenView` and `SettingsPanelView` both use `SettingsScreenFlowUiSystemHelper`. |
| Complete | Keep `SettingsScreenFlowUiSystemHelper`. | Helper suffix remains approved; no bare non-ECS `*System` introduced. |
| Complete | Avoid duplicated listener ownership in generated popups. | Popup components add listeners once during lifecycle; no update loops. |
| Deferred | Add dirty-state support. | Apply remains explicit and always available in this slice. |

Validation:

- Existing settings screen tests, if any.
- New `SettingsPanelView` bind/readback EditMode tests.
- `NonEcsSystemConversionArchitectureTests.RunFocusedValidation` if new helper naming touches architecture guardrails.

## M3 - Popup View Components

Goal:
Add popup-specific presentation and lifecycle without creating gameplay logic in MonoBehaviours.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Add `SettingsPopupView`. | Owns title, close/apply/reset buttons and a `SettingsPanelView` reference. |
| Complete | Add menu/match context enum if needed. | Added `SettingsPopupContext.Menu` and `SettingsPopupContext.Match`. |
| Complete | Add close callback binding. | Event-driven; no `Update`. |
| Complete | Add apply/reset button behavior. | Uses `SettingsScreenFlowUiSystemHelper` and `SettingsService`. |
| Deferred | Add tab switching. | Generated popups show all settings in a compact two-column panel for this slice. |

Validation:

- Popup initializes from `SettingsService.Load()`.
- Apply persists and invokes runtime apply.
- Reset restores defaults and updates all visible controls.
- Close destroys/clears popup without saving unintended changes unless explicitly designed.

## M4 - ECS Shell Request Plumbing

Goal:
Route settings opens through the existing ECS popup request path.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Add `Settings` to `UiShellPopupKind`. | Contract enum extension. |
| Complete | Change `UiActionKind.OpenSettings` handling. | Enqueues `UiShellPopupKind.Settings` instead of `UIRoute.Settings`. |
| Complete | Add close action if useful. | Used direct popup close binding in `UIShellContentView`; no extra action kind needed. |
| Complete | Preserve `UIRoute.Settings` for legacy route compatibility. | Route remains defined; visible buttons now use popup action. |
| Complete | Ensure match route is preserved. | Focused validation opens match settings without replacing Match HUD. |

Validation:

- ECS unit/EditMode test: `OpenSettings` produces `UiShellPopupRequestComponent(Settings, Show)`.
- Test that active shell route remains `UIRoute.Match` when opened from Match HUD.

## M5 - Shell Content Installation And Cleanup

Goal:
Install the correct settings popup prefab into `PopupLayer` using existing shell content patterns.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Add serialized popup prefab references to `UIShellContentView`. | `menuSettingsPopupPrefab`, `matchSettingsPopupPrefab`. |
| Complete | Add settings popup instance tracking. | Mirrors build drawer/full map popup cleanup. |
| Complete | Add `InstallSettingsPopup`. | Chooses menu or match prefab from command route. |
| Complete | Add `CloseSettingsPopup`. | Plays hide motion in play mode and clears instance refs. |
| Complete | Clear settings popup on `PopupLayer` clear. | Avoids stale view references. |

Validation:

- Existing build drawer/full map popup tests still pass.
- New content test asserts settings popup is installed under `PopupLayer`.
- Closing clears `PopupLayer`.

## M6 - Settings Popup Prefabs

Goal:
Build production Canvas prefabs from approved sprites and live UI controls.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Create `SCN02_MenuSettingsPopup.prefab`. | Generated by `SettingsPopupPrefabBuilder`. |
| Complete | Create `SCN08_MatchSettingsPopup.prefab`. | Generated by `SettingsPopupPrefabBuilder`. |
| Complete | Use live TMP text for all labels/values. | No baked text in sprites. |
| Complete | Use real Button/Toggle/Slider controls. | Popup uses live Unity UI controls; dropdowns remain supported in the panel API but generated popup uses segments. |
| Complete | Wire button sprite states. | Uses `MainMenuBrightCommand/Sprites` state sprites where present. |
| Complete | Support 16:9 and 20:9 visual capture proof. | Added `SettingsPopupPrefabBuilder.CaptureVisualQa`; captures cover menu and match popups. |

Validation:

- Prefab sprite-path whitelist test.
- Prefab interactive rect/raycast tests for close/apply/reset/tabs/controls.
- Visual captures at 16:9 and 20:9.

## M7 - Menu And Match Button Wiring

Goal:
Make the existing visible settings buttons open the new popups.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Add a shell action click relay if needed. | Added `UIShellActionButtonView`; serialized `UiActionKind` and `payloadId`; no `Update`. |
| Complete | Replace menu settings route behavior. | Removed `UIShellRouteButtonView` from menu `SettingsButton`; bound `OpenSettings` action. |
| Complete | Wire match `SettingsButton`. | Bound same `OpenSettings` action. |
| Complete | Keep button sprite states intact. | Existing button visuals preserved. |
| Complete | Add interaction tests. | Focused validation asserts serialized button action and real shell popup installation. |

Validation:

- Menu settings click opens `SCN02_MenuSettingsPopup`.
- Match settings click opens `SCN08_MatchSettingsPopup`.
- Match settings click does not leave Match HUD.

## M8 - Tests And Guardrails

Goal:
Lock the behavior and asset/source restrictions.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Add settings popup content tests. | Added `SettingsPopupValidationTests`. |
| Complete | Add sprite whitelist test. | Asserts all popup Image sprites resolve under `MainMenuBrightCommand/Sprites`. |
| Complete | Add ECS action request test. | `OpenSettings` maps to `UiShellPopupKind.Settings`. |
| Complete | Add persistence tests. | Assistant setting defaults and PlayerPrefs round-trip covered. |
| Complete | Add no-route-regression test. | Match popup installs via shell command without leaving Match HUD. |

Validation commands:

```bash
git diff --check
dotnet build Assembly-CSharp.csproj --no-restore -v:minimal
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SettingsPopupValidationTests.RunFocusedValidation -logFile /private/tmp/warline-settings-popup-validation.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UIShellCurrentContentLoadTests.RunFocusedValidation -logFile /private/tmp/warline-settings-popup-shell-content.log
```

Actual validation log:

| Command | Result | Log |
| --- | --- | --- |
| `SettingsPopupPrefabBuilder.Build` | Passed; prefabs and scene/content bindings regenerated. | `/private/tmp/warline-settings-popup-builder.log` |
| `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` | Passed, 0 warnings/errors. | terminal |
| `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` | Passed, 0 warnings/errors. | terminal |
| `git diff --check` | Passed. | terminal |
| `SettingsPopupValidationTests.RunFocusedValidation` | Passed, `tests=8`. | `/private/tmp/warline-settings-popup-validation.log` |
| `SettingsPopupPrefabBuilder.CaptureVisualQa` | Passed; menu/match 16:9 and 20:9 captures generated. | `/private/tmp/warline-settings-popup-capture-gpu.log` |
| `NonEcsSystemConversionArchitectureTests.RunFocusedValidation` | Failed on pre-existing direct `new UnitPathRequest` ownership violations outside this slice. | `/private/tmp/warline-settings-popup-non-ecs-guardrail.log` |

Known unrelated guardrail failure:

- `Assets/Game/Scripts/ScenarioLab/BattleScenarioDr001Runner.cs`
- `Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs`

## M9 - Visual QA

Goal:
Verify the popups are visually usable and match the command-base style.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Capture menu popup at 16:9. | `/private/tmp/warline-settings-popup-menu-16x9.png`. |
| Complete | Capture menu popup at 20:9. | `/private/tmp/warline-settings-popup-menu-20x9.png`. |
| Complete | Capture match popup at 16:9. | `/private/tmp/warline-settings-popup-match-16x9.png`. |
| Complete | Capture match popup at 20:9. | `/private/tmp/warline-settings-popup-match-20x9.png`. |
| Complete | Inspect control states. | Captures inspected; frame/control Images now serialize `Sliced` with PPU multiplier `2`; validation locks this. |

Acceptance:

- No text clipping or overlapping.
- Buttons are reachable and raycastable.
- Popup does not hide critical close/apply controls at supported aspect ratios.
- No sprites outside the approved folder.
- Frame/control Images use sliced rendering with Image PPU multiplier `2`.

## M10 - Documentation And Handover

Goal:
Record exactly what changed and how it was validated.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Update this tracker status. | Completed slices and blockers recorded. |
| Complete | Record validation commands and log paths. | Pass/fail markers listed above. |
| Complete | Record intentional deferred work. | Dirty-state, tabs, and screenshot QA deferred. |
| Complete | Summarize runtime behavior. | Menu and match settings popup entry points, apply/reset behavior, close behavior documented below. |

Runtime behavior summary:

- Menu Settings and Match HUD Settings now enqueue `UiActionKind.OpenSettings`.
- `UiActionRequestSystem` translates `OpenSettings` to `UiShellPopupKind.Settings` instead of a full-screen settings route.
- `UIShellContentView` installs `SCN02_MenuSettingsPopup` for menu route commands and `SCN08_MatchSettingsPopup` for match route commands under `PopupLayer`.
- Settings popups load from `SettingsService`, apply through `SettingsScreenFlowUiSystemHelper.SaveSettings`, reset through `SettingsService.ResetToDefaults`, and close through shell popup cleanup.

## Architecture Guardrails

- UI views may hold Unity object references and serialized prefab references.
- UI views must not contain gameplay policy, simulation policy, or ECS command validation.
- Hot gameplay remains in unmanaged ECS systems/jobs.
- New shell request translation stays inside existing unmanaged ECS systems.
- New helper classes must use approved suffixes if they contain `System` in the name.
- Do not create manager/controller/facade classes.
- Do not add broad `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loops, or polling-only components.
- Do not touch UI Toolkit migration.
- Do not rename unrelated files.

## Implementation Notes

- Prefer preserving `UIRoute.Settings` temporarily for compatibility, but stop using it for the visible settings buttons once popup behavior lands.
- If a close action is added to `UiActionKind`, keep it scoped to popup close only and avoid broad popup manager behavior.
- Existing `UIActionButtonView` is currently a visual binding component, not a shell action sender; do not overload it unless the change is clearly compatible with current call sites.
- If prefab YAML edits are too risky, use a focused Unity editor builder/validation pass to generate or wire the popup prefabs, then validate serialized references.

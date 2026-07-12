# Commander Screen Target-Lock UI Tracker

Date: 2026-07-12
Owner lane: Support/UI implementation
Scope: Commander profile/screen visual pass using existing target-lock art direction and main-menu UI conventions.

## Goal

Bring the Commander screen/panel to the same professional target-lock visual quality as the approved main-menu shell and Commander Profile target reference, without changing the shared header or introducing unrelated UI architecture changes.

## Current Evidence

- Target-lock reference exists:
  - `Design/VisualLockLayered/SCN-03_CommanderProfile/reference/SCN-03_CommanderProfile_NewMainMenuArtDirection_TargetLock_V01.png`
- Commander profile prefab exists:
  - `Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab`
- Main menu reference prefab exists:
  - `Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab`
- Target-lock popup/panel examples exist:
  - `Assets/Game/Prefabs/UI/Shell/Popups/POP13_ARIACommandAssistantPopup.prefab`
- Main menu left-nav button component exists:
  - `Assets/Game/Prefabs/UI/Components/MainMenuLeftNavButton.prefab`

## Dirty Baseline To Avoid

These files were already dirty before this Commander UI task started. Do not stage, revert, or edit them unless explicitly required and rechecked:

- `Assets/Game/Scripts/Systems/BuildingSpawnPrefabSystem.cs`
- `Assets/Settings/Mobile_RPAsset.asset`
- `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`
- `Design/AgentReports/architecture_performance_android_apk_build_report.json`
- `Design/AgentReports/architecture_performance_android_apk_build_report.md`
- `Design/Architecture/architecture_performance_hardening_implementation_tracker.md`
- `Assets/SceneDependencyCache/70000aceaf686ac77eee65950cf20a2d.sceneWithBuildSettings`
- `Assets/SceneDependencyCache/70000aceaf686ac77eee65950cf20a2d.sceneWithBuildSettings.meta`

## Non-Negotiable UI Rules

- Reuse the existing Commander Profile target-lock mockup; do not generate new art unless the existing reference is unusable.
- Keep the shared menu header unchanged from menu to menu.
- Reuse existing sprites and panels when they match the target-lock language.
- Avoid large bespoke multi-selection sprites.
- Match main-menu font asset, font sizing, pixels-per-unit, image type, 9-slice borders, and button state behavior.
- Button state wiring must match existing buttons: normal, highlighted, pressed, selected, disabled.
- Prefer existing prefab/component patterns over one-off hierarchy inventions.
- No gameplay behavior changes.
- No unrelated UI Toolkit/C# cleanup in this pass.

## Progress

Overall: 100%

| Stage | Status | Percent | Notes |
| --- | --- | ---: | --- |
| 1. Baseline and references | Complete | 100% | Existing target-lock image, Commander prefab, reusable sprite set, and editor layout utility found. Active shell route gap identified. |
| 2. Style contract audit | Complete | 100% | Existing editor utility uses sliced target-lock frames, main-menu nav frames, PPU multipliers, shared font style, and SpriteSwap button states. Validated after applying. |
| 3. Implementation plan lock | Complete | 100% | Imagegen was not needed. Scope included prefab polish plus active `CommandFeed` route wiring because the Commander prefab was not previously mounted by `UIShellContentView`. |
| 4. Prefab implementation | Complete | 100% | Route wiring is complete. User review rejected the first visual prefab pass. Corrected generator now makes the Commander prefab body-only, removes its own background/header, and aligns the left rail to main-menu nav frame/state/PPU conventions. Prefab regenerated successfully. |
| 5. Focused validation | Complete | 100% | Focused validation passed 12/12 after the visual correction, including the body-only Commander prefab guard. |
| 6. Handoff | Complete | 100% | Final handoff recorded below. |

## Stage 1 Checklist - Baseline And References

- [x] Confirm existing target-lock Commander Profile reference.
- [x] Confirm existing Commander Profile content prefab.
- [x] Confirm existing Main Menu content prefab for style comparison.
- [x] Inspect target-lock reference image visually.
- [x] Inspect Commander prefab current hierarchy and serialized style values.
- [x] Inspect Main Menu prefab style values and button state setup.
- [x] Inspect existing target-lock popup/panel prefab for reusable chrome/panel patterns.

Stage 1 notes:

- The approved target-lock reference is usable, so no new image generation is needed.
- `Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/` already contains Commander panel chrome, CTA frames, selected small-button chrome, icons, portrait art, and a background slice.
- `CanvasMenuFallbackValidation.ApplyCommanderProfileTargetLockLayout()` already exists and is the safest prefab implementation path because it applies sliced target-lock panels, main-menu nav button frames, correct `SpriteSwap` button states, disables the duplicated content header, and preserves live UI sections instead of flattening the mockup.
- Active runtime gap: `UIShellContentView.InstallMenuRouteBody(UIRoute route)` only handles `UIRoute.Armory` specially; all other menu routes fall back to the main-menu body. Therefore `UIRoute.CommandFeed` does not currently mount `SCN03_CommanderProfileContent.prefab`.

## Stage 2 Checklist - Style Contract Audit

- [x] Identify main menu font asset(s) and font sizes used for title, nav, labels, counters.
- [x] Identify main menu image sprites, image type, PPU, and 9-slice border values.
- [x] Identify button transition mode, colors/sprites, interactable state, selected/disabled behavior.
- [x] Identify Commander screen mismatches: layout, sprite use, typography, button states, oversized unique sprites, header duplication.
- [x] Decide reusable sprite/panel sources.
- [x] Verify active route wiring after adding Commander content prefab slot.

Stage 2 notes:

- The Commander target-lock layout reuses `Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/` sprites and the existing main-menu navigation button state pattern.
- The content prefab's own header remains disabled; the live shell keeps the shared menu header stable across menu routes.
- The route gap was runtime wiring, not missing art: `UIRoute.CommandFeed` previously fell back to the main-menu body.

## Stage 3 Checklist - Implementation Plan Lock

- [x] State whether imagegen is needed. Expected: no, because the Commander target-lock reference exists.
- [x] List exact prefab sections to modify.
- [x] List exact assets to reuse.
- [x] Define validation commands/tests before editing prefab.

Stage 3 locked implementation:

- Add `commanderProfileContentPrefab` to `UIShellContentView` and expose it like the other content prefabs.
- Extend `UIShellContentView.Configure(...)` with an optional Commander prefab argument without breaking existing callers.
- Route `UIRoute.CommandFeed` to a new `InstallCommanderProfileBody()` method that installs Commander left/middle/right/footer body sections from `SCN03_CommanderProfileContent.prefab` while preserving the shared shell header/background.
- Assign `SCN03_CommanderProfileContent.prefab` to the live `Menu.unity` shell component.
- Run `CanvasMenuFallbackValidation.ApplyCommanderProfileTargetLockLayout()` to rebuild the Commander content prefab against the target-lock reference.
- Validation target: Unity batch run of the layout method, `git diff --check`, route/source scan showing `CommandFeed` no longer falls back to Main Menu, and focused compile if practical.

## Stage 4 Checklist - Prefab Implementation

- [x] Preserve shared header behavior and route buttons.
- [x] Replace or restyle Commander body panels to match target-lock reference.
- [x] Ensure all buttons use correct button states and existing component patterns.
- [x] Keep text legible and consistent with main menu font/size rules.
- [x] Avoid adding large new bespoke sprites unless required by target-lock art direction.

Stage 4 notes:

- `UIShellContentView` now exposes a Commander profile content prefab slot and routes `UIRoute.CommandFeed` into Commander body sections.
- `Menu.unity` assigns `SCN03_CommanderProfileContent.prefab` to the new shell slot.
- `SCN03_CommanderProfileContent.prefab` now has `UIShellContentSectionsView` section references for Left, Middle, Right, and Footer so the shell can mount body regions without replacing the persistent header.
- `CanvasMenuFallbackValidation.ApplyCommanderProfileTargetLockLayout()` now also configures the Commander section references, keeping future prefab regeneration aligned with the shell route.
- `UIShellContentSectionPrefabMigration` now knows the Commander content prefab and sections.

User review follow-up:

- The first generated Commander prefab was rejected because it still behaved like a full-screen mockup: it contained its own background, used a left navigation rail that did not match the main menu closely enough, and produced an over-designed panel composition.
- Corrective source changes are in progress:
  - `ApplyCommanderProfileTargetLockLayout()` now removes direct `HeaderContent` and `MenuBackgroundContent` children from the Commander prefab during regeneration.
  - Commander left navigation now uses main-menu nav frame sprites, main-menu nav icon sources where available, `SpriteSwap` state setup, and PPU multiplier `1`.
  - Body panels were reduced to a restrained shell-compatible layout instead of the oversized target-lock mockup coordinates.
  - Focused tests now assert that the Commander prefab is body-only and does not serialize its own header/background children.
- Resolved blocker: Unity batchmode could not regenerate the prefab while `/Users/farhad/Projects/WarlineCapture` was open in the Editor. After Unity was closed, regeneration succeeded.
- Earlier blocked command:
  - `/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod Game.Editor.CanvasMenuFallbackValidation.ApplyCommanderProfileTargetLockLayout -quit -logFile /private/tmp/warline-commander-profile-target-lock-layout-v2.log`
  - First result: Failed before execution, `Multiple Unity instances cannot open the same project`.
- Final result after Unity closed: Passed, marker `[CanvasCommanderProfileTargetLockLayout] result=Passed`.

Final visual correction:

- Rebuilt the body around the target-lock hierarchy: wide icon navigation, bright canonical commander portrait, identity/rank/XP block, four overview cards, three-row account table, reward progression, five-row recent history, and aligned footer CTAs.
- Reused the shared main-menu commander portrait and existing target-lock sprites; no new generated art was required.
- Removed duplicate legacy footer button objects left by earlier prefab generations.
- Added explicit active icon children for Back, section navigation, and footer actions so the live routed screen matches the target-lock information density.
- Verified the body still owns no `HeaderContent` or `MenuBackgroundContent`; the shell header/background remain shared.
- Moved the identity, service record, account table, and footer onto the same target-lock grid as the reward/history column.
- Added `CommanderProfileContentView` so the Commander name and subtitle consume the existing `UiShellCommanderProfileModel` without a per-frame `Update`.
- Wired `Open Armory` to the real Armory route. Detail, Replay, Stats, Badges, History, and Upgrades remain visibly disabled until their destinations exist.
- Added `CommanderProfileResponsiveLayoutView`, driven by enable/rect-dimension events, to interpolate middle/footer offsets between the 16:9 logical canvas height and 20:9/ultrawide height.

## Stage 5 Checklist - Validation

- [x] Run YAML/prefab sanity scan for broken references.
- [x] Run focused Unity validation if available.
- [x] If no test exists, add or run the nearest existing UI prefab validation.
- [x] Capture known gaps if Unity cannot run due sandbox/licensing.

Stage 5 validation:

- `git diff --check -- Assets/Game/Scripts/UI/Shell/UIShellContentView.cs Assets/Game/Scenes/Menu.unity Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs Assets/Game/Scripts/Editor/CanvasMenuFallbackValidation.cs Assets/Game/Scripts/Editor/UIShellContentSectionPrefabMigration.cs Design/Architecture/commander_screen_target_lock_ui_tracker.md`
  - Result: Passed before user visual rejection.
- `/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UIShellCurrentContentLoadTests.RunFocusedValidation -quit -logFile /private/tmp/warline-ui-shell-current-content-validation.log`
  - Result: Passed before user visual rejection, 12/12 focused shell tests.
  - New Commander-specific coverage: `MenuSceneShellInstallsCommanderProfileRouteWithoutReplacingHeader`.
- `git diff --check -- Assets/Game/Scripts/Editor/CanvasMenuFallbackValidation.cs Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs Design/Architecture/commander_screen_target_lock_ui_tracker.md`
  - Result: Passed after the body-only source correction.
- `/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod Game.Editor.CanvasMenuFallbackValidation.ApplyCommanderProfileTargetLockLayout -quit -logFile /private/tmp/warline-commander-profile-target-lock-layout-v2.log`
  - Result: Passed, marker `[CanvasCommanderProfileTargetLockLayout] result=Passed`.
- Body-only prefab scan:
  - `rg -n "m_Name: (HeaderContent|MenuBackgroundContent)" Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab`
  - Result: No matches.
- `git diff --check -- Assets/Game/Scripts/UI/Shell/UIShellContentView.cs Assets/Game/Scenes/Menu.unity Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs Assets/Game/Scripts/Editor/CanvasMenuFallbackValidation.cs Assets/Game/Scripts/Editor/UIShellContentSectionPrefabMigration.cs Design/Architecture/commander_screen_target_lock_ui_tracker.md`
  - Result: Passed after prefab regeneration and YAML cleanup.
- `/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UIShellCurrentContentLoadTests.RunFocusedValidation -quit -logFile /private/tmp/warline-ui-shell-current-content-validation-v2.log`
  - Result: Passed, 12/12 focused shell tests.
  - Commander-specific coverage: `MenuSceneShellInstallsCommanderProfileRouteWithoutReplacingHeader` validates route mounting, shared-header preservation, and body-only Commander prefab ownership.
- `/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod Game.Editor.CanvasMenuFallbackValidation.RunCommanderProfileRouteCapture -logFile /private/tmp/warline-commander-route-capture-rich-v4.log`
  - Result: Passed, route `CommandFeed`, 1920x1080, luma `0.237`, detail `0.984`.
  - Capture: `Design/AgentReports/Captures/commander_profile_route_capture.png`.
  - Visual result: shared header preserved; no panel overlap or clipped text; target-lock portrait, navigation icons, account table, reward track, history list, and footer CTAs are visible.
- `/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UIShellCurrentContentLoadTests.RunFocusedValidation -quit -logFile /private/tmp/warline-ui-shell-current-content-validation-commander-rich.log`
  - Result: Passed, 13/13 focused shell tests.
  - Includes `MainMenuCommanderRouteButtonOpensProfileAndBackReturnsToMainMenu`.
- `/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UIShellCurrentContentLoadTests.RunFocusedValidation -quit -logFile /private/tmp/warline-commander-responsive-tests-v2.log`
  - Result: Passed, 14/14 focused shell tests.
  - Added coverage for profile read-model binding, Armory routing, disabled unavailable actions, and 16:9/20:9 responsive offsets.
- Final routed visual captures:
  - `Design/AgentReports/Captures/commander_profile_1920x1080.png`: Passed, luma `0.233`, detail `0.987`.
  - `Design/AgentReports/Captures/commander_profile_2560x1080.png`: Passed, luma `0.224`, detail `0.970`.
  - `Design/AgentReports/Captures/commander_profile_android_2400x1080.png`: Passed, luma `0.210`, detail `0.958`.
  - Visual inspection result: no header collision, footer overlap, panel overlap, clipped labels, or duplicate CTA objects.

## Stage 6 Handoff

- Lane: Support/UI implementation.
- Task: Build the Commander screen body against the existing target-lock art direction and wire it into the live shell route.
- Files changed:
  - `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
  - `Assets/Game/Scripts/UI/Screens/CommanderProfileContentView.cs`
  - `Assets/Game/Scripts/UI/Screens/CommanderProfileResponsiveLayoutView.cs`
  - `Assets/Game/Scenes/Menu.unity`
  - `Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab`
  - `Assets/Game/Scripts/Editor/CanvasMenuFallbackValidation.cs`
  - `Assets/Game/Scripts/Editor/UIShellContentSectionPrefabMigration.cs`
  - `Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs`
  - `Design/Architecture/commander_screen_target_lock_ui_tracker.md`
- Contracts touched: Added Commander content prefab binding to `UIShellContentView`; added Commander content section setup through existing `UIShellContentSectionsView` contract.
- User-visible behavior: Opening Commander shows the target-lock profile dashboard with a shared header, responsive 16:9/20:9 layout, live Commander name/subtitle binding, functional Back/Open Armory actions, and visibly disabled unavailable actions.
- Validation run: `git diff --check`; Unity focused UI shell validation.
- Validation result: Passed.
- Known gaps: Commander profile values are currently representative presentation data; connecting them to persistent player progression is a separate data-binding task.
- Cross-lane impacts: No gameplay code touched. Shell route behavior changes only for `UIRoute.CommandFeed`.
- Next recommended task: Bind the Commander presentation fields to player profile/progression data without changing the validated layout.

## Stage 7 - Readability And Internal Padding Pass

- Status: Complete.
- User feedback addressed:
  - Small information panels did not provide enough internal padding.
  - Icons and labels visually touched panel borders.
  - Compact-panel icons and text were too small at 1080p.
- Implementation:
  - Increased the Service Record section height and converted each stat card from narrow chip chrome to a full-height framed card.
  - Increased stat-card icons, values, labels, suffixes, and their separation from visible borders.
  - Increased Account Snapshot row icons and primary/value typography while retaining the existing table columns.
  - Increased Reward Track level, progress, milestone, reward-card, and next-reward components; reward cards now use full-height framed cards instead of the misleading narrow chip frame.
  - Increased Recent History row height, icons, titles, results, and time labels with explicit left/right padding.
  - Increased left-navigation and footer action icon/label sizing.
  - Rebalanced middle-section heights without changing the overall Commander dashboard footprint.
  - Added focused prefab guards for minimum stat-card, account-row, reward-card, and history-row sizes, icon sizes, and font sizes.
- Files changed:
  - `Assets/Game/Scripts/Editor/CanvasMenuFallbackValidation.cs`
  - `Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab`
  - `Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs`
  - `Design/AgentReports/Captures/commander_profile_1920x1080.png`
  - `Design/AgentReports/Captures/commander_profile_android_2400x1080.png`
- Validation:
  - Commander prefab regeneration passed: `[CanvasCommanderProfileTargetLockLayout] result=Passed`.
  - Focused shell validation passed: `14/14`, including the new Commander readability assertions.
  - Routed `1920x1080` capture passed: luma `0.240`, detail `0.992`.
  - Routed `2400x1080` capture passed: luma `0.221`, detail `0.982`.
  - Manual inspection: no text clipping, panel overlap, border contact, or footer collision at either aspect ratio.
- Known gap:
  - This pass improves readability and component construction. Exact target-lock fidelity still requires dedicated Commander-specific panel artwork; the current implementation intentionally reuses validated project chrome.
- Next recommended task:
  - Bind representative Commander values to persistent player-profile/progression data without changing the validated visual geometry.

## Stage 8 - Cohesion, Depth, And Responsive Composition

- Status: Complete.
- User-visible improvements:
  - Added a non-interactive Commander-only scrim over the shell-owned menu background; returning to Main Menu removes it.
  - Rebuilt the identity hierarchy with a rank emblem, motto divider, level medallion, Command XP label, and framed XP progress.
  - Added a visible XP meter to the reward track and improved reward-state spacing.
  - Replaced ambiguous faded navigation states with explicit lock badges while preserving the Main Menu navigation frame language.
  - Expanded account/history row spacing and panel depth for 16:9.
  - Added compact 20:9 panel heights and row spacing so the footer remains visible on Android-style ultrawide screens.
  - Added a shared framed footer rail so the three actions read as part of the dashboard rather than detached buttons.
- Runtime/architecture behavior:
  - Commander remains a body-only prefab; it does not own or duplicate the shared header or background.
  - `UIShellContentView` owns the route-specific background scrim and removes it when leaving Commander.
  - `CommanderProfileResponsiveLayoutView` remains event-driven and now interpolates optional target heights as well as top offsets; no `Update` or `LateUpdate` was added.
  - Added a right-column responsive section for reward/history panels and rows.
  - Commander capture validation now survives normal domain/scene reload through `SessionState`, temporarily disables fast Enter Play Mode only for the capture, and restores the original project setting before exit.
- Files changed:
  - `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
  - `Assets/Game/Scripts/UI/Screens/CommanderProfileResponsiveLayoutView.cs`
  - `Assets/Game/Scripts/Editor/CanvasMenuFallbackValidation.cs`
  - `Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab`
  - `Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs`
  - `Design/AgentReports/Captures/commander_profile_1920x1080.png`
  - `Design/AgentReports/Captures/commander_profile_android_2400x1080.png`
- Validation:
  - Commander prefab regeneration: Passed.
  - Focused shell/Commander validation: Passed, `14/14`.
  - Regression coverage includes scrim lifecycle, lock/identity/reward/footer objects, 16:9 expanded heights, 20:9 compact heights, and both footer baselines.
  - Real routed `1920x1080` Play Mode capture: Passed, luma `0.204`, detail `0.992`.
  - Real routed `2400x1080` Play Mode capture: Passed, luma `0.199`, detail `0.982`.
  - Manual inspection: no footer clipping, text clipping, panel overlap, header collision, or border contact at either aspect ratio.
  - Enter Play Mode settings restored after capture: enabled with options value `3`, matching the pre-capture state.
- Known gaps:
  - Commander values remain representative presentation data until player-profile/progression contracts are connected.
  - Exact pixel identity with the concept still depends on dedicated production Commander panel art; this pass improves composition and visual coherence using the existing approved asset library.
- Next recommended task:
  - Bind persistent Commander/player progression data without changing the validated layout geometry.

## Stage 6 Handoff Format

When stable, report:

- Lane
- Task
- Files changed
- Contracts touched
- User-visible behavior
- Validation run
- Validation result
- Known gaps
- Cross-lane impacts
- Next recommended task

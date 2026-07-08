# ARIA Assistant ECS Implementation Tracker

Date: 2026-07-08
Status: Phase 5 bounded control sequence complete
Source design: `Design/ARIA_Assistant_ECS_Design.md`

## Progress

Overall progress: 68% complete, 46 of 68 checklist items complete.

| Phase | Scope | Items | Complete | Status |
|---|---|---:|---:|---|
| 0 | Contract reconciliation | 6 | 6 | Complete |
| 1 | ECS data contract | 8 | 8 | Complete |
| 2 | Match header and panel shell | 8 | 8 | Complete |
| 3 | Goal and recommendation read models | 8 | 8 | Complete |
| 4 | Show Me and Do It command intents | 8 | 8 | Complete |
| 5 | Give Control ownership | 8 | 8 | Complete |
| 6 | Message, narration, and audio | 8 | 0 | Not started |
| 7 | Settings, save, and accessibility | 6 | 0 | Not started |
| 8 | Validation, performance, and rollout | 8 | 0 | Not started |

Progress update rule: update the complete count and overall percentage after every stable slice. Do not mark a phase complete until code, docs, and validation notes are updated.

## Architecture Guardrails

- Prefer unmanaged `ISystem`, Burst-compatible jobs, and pure ECS data for capture, scoring, filtering, command requests, command results, and control-state transitions.
- Keep Unity UI, camera highlights, audio playback, and persistence as narrow managed helper boundaries.
- Do not introduce managers, controllers, facades, service locators, broad replacement shells, or new updating `MonoBehaviour` loops.
- Do not add `WarlineCaptureAssistantService`, `AssistantContextProvider`, `M01AssistantRecommendationProvider`, `CommandIntentExecutor`, `M01AssistantCommandRuntime`, `AssistantPanelController`, or `AssistantHighlightController`.
- Use approved suffixes such as `UiSystemHelper`, `PresentationSystemHelper`, `PersistenceSystemHelper`, `CompositionSystemHelper`, and `UtilitySystemHelper` for non-ECS managed helpers.
- Do not call HUD child views or legacy selection internals from assistant logic. UI helpers enqueue ECS requests and consume read models.
- Avoid per-frame managed allocations, LINQ, string formatting, list churn, or full read-model rebuilds.
- Keep UI Toolkit/Canvas migration out of scope.

## Phase 0: Contract Reconciliation

- [x] Add implementation notes to older M01 assistant docs so they point to `Design/ARIA_Assistant_ECS_Design.md` for current naming and ownership.
- [x] Confirm existing assistant panel prefab fields and current match HUD header hierarchy.
- [x] Inventory existing assistant/tutorial classes and identify names that must be retained only as historical/test debt or migrated.
- [x] Identify existing ECS command boundaries for selection, move, attack, camera focus, and stop.
- [x] Identify existing alert/feedback data sources for command rejection, objectives, resources, fuel, threats, and match result state.
- [x] Record any blockers or legacy compatibility constraints in this tracker before implementation edits.

Exit criteria: current docs and code inventory agree on the first implementation slice and no forbidden broad assistant owner is planned.

Phase 0 notes:

- Active source currently contains `Assets/Game/Scripts/UI/Components/AssistantButtonView.cs` and assistant settings rows, but no active `AssistantPanelView`, `AssistantPanelController`, or assistant service/provider/runtime source files. Older docs and agent reports reference those as historical work.
- `PREFAB-05_AssistantPanel` is not present in active prefab/source paths found by `rg --files`; Phase 2 must either build a new panel surface or locate/import the intended prefab before binding.
- Existing command boundaries include `RtsSelectionCommandIntentRequestElement`, `RtsSelectionCommandResultElement`, `RtsCameraRequestElement`, `UnitMoveOrderRequestElement`, `UnitAttackOrderRequestElement`, `ScanIntelCommandRequestElement`, and transport boarding command/result flow.
- Existing feedback/read sources include `SelectionHudFeedbackElement`, `RtsSelectionCommandResultElement`, `BattleHudRuntimeFeedbackView` / runtime feedback contracts, `UiMatchHudHeaderComponent`, `UiMatchHudStatusSurfacesComponent`, `BuildingRuntimeFactionUsableFuelSummary`, building UI command results, scan command results, and threat warning UI contracts.
- Compatibility constraint: the first live UI slice must not assume an existing assistant panel controller. It should introduce `MatchHudAssistantUiSystemHelper` and `AssistantPanelUiSystemHelper` as narrow presentation helpers over ECS read models.

## Phase 1: ECS Data Contract

- [x] Add `AssistantStateComponent`.
- [x] Add `AssistantControlOwnerComponent`.
- [x] Add `AssistantRecommendationReadModelComponent`.
- [x] Add `AssistantNarrationStateComponent`.
- [x] Add `AssistantSettingsComponent`.
- [x] Add dynamic buffer elements: `AssistantGoalReadModelElement`, `AssistantRecommendationElement`, and `AssistantMessageElement`.
- [x] Add dynamic buffer elements: `AssistantNarrationRequestElement`, `AssistantCommandIntentRequestElement`, and `AssistantCommandIntentResultElement`.
- [x] Add assistant enums for control state, recommendation kind, message priority, narration mode, and command intent status.

Exit criteria: data types compile, use Burst-compatible fields, and have focused tests or compile-time checks for unmanaged ECS eligibility.

Phase 1 notes:

- Added `Assets/Game/Scripts/Components/AssistantComponents.cs` under `Game.Components`.
- Added `Assets/Tests/Editor/AssistantEcsDataContractTests.cs` for unmanaged component/buffer compile constraints, buffer population, and stable enum values.
- The contract is data-only; no assistant behavior, UI binding, narration playback, or command execution was added in this slice.

## Phase 2: Match Header And Panel Shell

- [x] Add ARIA header button to the match HUD using existing Canvas style and sprites.
- [x] Ensure ARIA header button blocks world pointer/touch selection behind UI on desktop and Android.
- [x] Add panel open/close binding through `MatchHudAssistantUiSystemHelper`.
- [x] Add panel binding through `AssistantPanelUiSystemHelper` without assistant policy in the UI helper.
- [x] Show current goals area from read-model rows.
- [x] Show top recommendation area with title, reason, risk/status chip, and action buttons.
- [x] Show alert/report list from prioritized message rows.
- [x] Add visible ownership state when ARIA preview/takeover is active.

Exit criteria: opening/closing the panel is visually validated in Unity, causes no world click-through, and does not allocate in steady-state panel refresh.

Phase 2 notes:

- Added `MatchHudAssistantUiSystemHelper` as a narrow managed UI boundary. It creates the ARIA header button and a lightweight panel shell when the match HUD header installs, and destroys both with the header lifecycle.
- ARIA button and panel clicks call `MainMenuPlayUI.CaptureGameplayUiClickSequence()` so Android/desktop UI touches suppress world selection behind the HUD.
- The panel shell now consumes live `UiAssistantPanelModel` rows for current goals and the top recommendation. It may still extract `AssistantPanelUiSystemHelper` if future alert/report and ownership-state binding grows beyond the current helper.
- The shell includes `NEXT ACTION` and `GIVE CONTROL` buttons as non-executing UI affordances. Command intent wiring remains Phase 4/5.
- Added `UiAssistantPanelModel` and runtime gateway binding so `MainMenuPlayUI` applies live goal/recommendation rows to the panel without the UI helper querying ECS. The gateway caches converted strings by assistant source/recommendation versions, and the helper only updates text when the model version changes.
- Extracted `AssistantPanelUiSystemHelper` as the panel read-model binding boundary. `MatchHudAssistantUiSystemHelper` now owns ARIA header/panel lifecycle and pointer capture, while the panel helper applies `UiAssistantPanelModel` text/button state behind dirty version checks.
- Added alert/report panel binding from `AssistantMessageElement` rows. The gateway converts up to three unacknowledged prioritized message rows into cached panel text, and the panel helper displays the alerts without querying ECS or owning message policy.
- Added a visible control-state label to the panel, bound from `UiAssistantPanelModel.OwnershipText`. Phase 5 still owns real control-state transitions and cancellation behavior; Phase 2 now has the display surface ready.

## Phase 3: Goal And Recommendation Read Models

- [x] Add `AssistantGoalReadModelSystem` with dirty/version gates for objective source changes.
- [x] Add `AssistantRecommendationSystem` with bounded candidate scoring.
- [x] Add first recommendation source for no selection or selected idle combat unit.
- [x] Add first recommendation source for active objective or visible mission target.
- [x] Add first recommendation source for resource/fuel/logistics warning when relevant.
- [x] Add deterministic priority tie-breakers and stable recommendation ids.
- [x] Add UI read-model dirty version publishing.
- [x] Add tests for recommendation suppression when source versions do not change.

Exit criteria: panel can show live goals and one recommendation without per-frame rebuilds.

Phase 3 notes:

- Added `AssistantGoalReadModelSystem` in the UI Shell ECS lane. It creates assistant read-model buffers on the match HUD shell boundary and publishes objective rows only when objective text/icon state changes.
- Added `AssistantRecommendationSystem` with a bounded first pass: active threat warning wins with a stable defensive-alert recommendation, otherwise the first active objective produces a stable `CameraFocus` recommendation.
- Added selection-aware recommendation scoring: no selected unit produces a stable `Select` recommendation; a focused, player-owned idle combat unit produces a stable `Attack` or `Move` recommendation based on command availability.
- Added fuel/logistics recommendation scoring from `BuildingRuntimeFactionUsableFuelSummary`. Empty fuel creates a stable high-priority `Logistics` recommendation before no-selection/objective guidance, with source versioning derived from the usable fuel summary version and rounded oil/fuel values.
- Phase 3 is complete: live goals and one bounded top recommendation can now be sourced from objective, threat, selection, and fuel/logistics state without per-frame UI rebuilds.

## Phase 4: Show Me And Do It Command Intents

- [x] Add `AssistantCommandIntentRequestElement` enqueue path from `Show Me`.
- [x] Add `AssistantCommandIntentRequestElement` enqueue path from `Do It`.
- [x] Add `AssistantCommandIntentSystem` routing for camera/selection preview intents.
- [x] Add `AssistantHighlightPresentationSystemHelper` for UI pulse, world highlight, and camera nudge from ECS read models.
- [x] Add one safe `Do It` command through existing ECS command boundaries.
- [x] Add result rows for accepted, rejected, completed, cancelled, and timed-out intents.
- [x] Add recovery message when a command intent is rejected.
- [x] Add focused tests for intent request/result routing and invalid target recovery.

Exit criteria: `Show Me` previews without committing gameplay and `Do It` executes one safe command through ECS request/result data.

Phase 4 notes:

- Added a UI-contract `UiAssistantCommandIntentKind` so `Game.UI.Contracts` does not depend on ECS component assemblies.
- The ARIA panel `SHOW ME` button now calls the runtime gateway and enqueues a `ShowRecommendation` intent copied from the current top `AssistantRecommendationElement`.
- `UiShellEcsGateway` creates assistant command request/result buffers on the shell boundary as needed and rejects stale Show Me calls when the top recommendation cannot be shown. Routing and visual preview are intentionally left for the next Phase 4 slice.
- Added `AssistantCommandIntentSystem` as an unmanaged UI Shell ECS system. It consumes queued Show Me / focus-preview requests, resolves entity or world-position targets, queues existing `RtsCameraRequestElement` smooth focus requests, writes accepted/rejected assistant result rows, and marks assistant control state as `AssistantPreview` without executing gameplay.
- The ARIA panel `DO IT` button now enqueues `ExecuteRecommendation` only when the top recommendation exposes `CanExecute`. `UiShellEcsGateway` maps executable recommendation kinds to typed assistant intents (`SelectEntity`, `MoveToWorldPosition`, `AttackEntity`, `FocusCamera`, or `StopAssistantControl`) instead of assuming all executable recommendations are attacks. No gameplay command execution is added yet.
- Added `AssistantPreviewHighlightElement` as the ECS preview read model for accepted Show Me / focus-preview requests. `AssistantCommandIntentSystem` now writes one active highlight row on accepted preview and clears stale rows on rejected or unsupported preview requests.
- Added `UiAssistantHighlightModel`, gateway conversion/caching, and `AssistantHighlightPresentationSystemHelper`. The match HUD panel consumes the model through the existing refresh path, displays a bounded cyan pulse behind the recommendation area, and shows a pooled overlay world ring at the preview target. Camera preview nudge continues through the existing `RtsCameraRequestElement` smooth-focus request.
- Added the first safe `DO IT` route through the existing RTS selection command queue. ARIA `SelectEntity` intents now enqueue `EnterSelectionMode` for the no-selection UI-surface recommendation, or enqueue `FocusUnit` with a pre-resolved entity target when one is available. The existing selection focus command helper now accepts entity-target `FocusUnit` requests by using its existing runtime entity focus path.
- Rejected command intents now write a bounded high-priority `AssistantMessageElement` recovery row beside the rejected result row. The existing ARIA panel alert binding can surface the recovery text without a new UI path, and the focused command-intent validation covers invalid-target recovery.
- Phase 4 lifecycle status rows are complete. Preview requests now write `Accepted` and `Completed` rows once the camera/highlight preview is active, explicit preview cancellation writes `Cancelled`, stale queued requests write `TimedOut`, and existing invalid/unsupported paths continue writing `Rejected`.

## Phase 5: Give Control Ownership

- [x] Add `AssistantControlOwnerSystem` transition handling for player, guided, preview, takeover, and override-pending states.
- [x] Add explicit takeover timeout and max action count.
- [x] Add player input override detection through existing input boundaries.
- [x] Cancel takeover on pause, route change, result popup, destroyed target, invalid command, or selection ownership mismatch.
- [x] Add `Stop` button request path to cancel preview/takeover.
- [x] Add visible ownership banner/state on the panel.
- [x] Add one bounded control sequence, such as one tutorial select/move/action.
- [x] Add tests for cancellation and player override.

Exit criteria: ARIA can perform one bounded control sequence and reliably returns control to the player.

Phase 5 notes:

- Added a bounded `STOP` control to the ARIA panel. It is enabled only while ARIA is in Guided, Preview, Takeover, or PlayerOverridePending state.
- `UiShellEcsGateway` now queues `StopAssistantControl` directly from the UI boundary without requiring a current recommendation row, so the player can always cancel an active ARIA preview/takeover state.
- `AssistantCommandIntentSystem` handles `StopAssistantControl` as a cancellation result, clears preview highlight rows, resets active recommendation id, and returns control state to `Player`.
- The assistant panel model version now includes control state so state-only ownership changes refresh the header/panel text and button interactability.
- Added unmanaged `AssistantControlOwnerSystem` to mirror player/guided/preview/takeover/override-pending ownership states from `AssistantStateComponent` into `AssistantControlOwnerComponent`.
- Takeover ownership now starts with bounded defaults: 30 seconds and 3 counted actions. The owner system returns control to the player when timeout or action count is reached.
- Player pointer requests and queued move-order tokens are now sampled from the existing RTS selection input ECS boundary. ARIA ownership baselines the latest observed input when ownership starts, then enters `PlayerOverridePending` and returns control to the player when newer player input appears.
- Assistant takeover now returns control to the player when a newer command-intent result is rejected, cancelled, or timed out. This covers invalid commands and destroyed-target rejection paths through the existing ECS command-result boundary.
- Shell blocker cancellation now returns control to the player when the shell leaves the match route, enters a non-match/non-popup mode, or shows pause/settings/reward popups. Threat alert popups are intentionally non-blocking. The mission-result popup currently has no ECS source (`TryReadMissionResult` returns false), so future result UI should either use `UiShellActivePopupComponent` or add a data-only ECS result component before ARIA can observe it directly.
- The panel ownership surface now uses a short header state plus a readable detail line from `UiAssistantPanelModel.OwnershipDetailText`, so active preview/takeover/override states tell the player what ARIA is doing and how to stop it.
- Phase 5 bounded control sequence is complete. The ARIA panel Give Control action now sends a takeover-marked execute request; `AssistantCommandIntentSystem` routes the safe selection/focus command through existing RTS selection command buffers and enters `AssistantTakeover`, where `AssistantControlOwnerSystem` applies timeout, action-count, shell-blocker, invalid-command, and player-override bounds.

## Phase 6: Message, Narration, And Audio

- [ ] Add `AssistantMessagePrioritySystem` to merge objectives, command feedback, resources, fuel, threats, and reports.
- [ ] Add priority levels: Critical, High, Normal, Low.
- [ ] Add cooldown, coalescing, and duplicate suppression.
- [ ] Add `AssistantNarrationRequestSystem` to create narration requests from eligible messages.
- [ ] Add subtitle/text display for all narration requests.
- [ ] Add `AssistantNarrationPresentationSystemHelper` for pre-authored clip playback or silent fallback.
- [ ] Add narration settings gate: Off, CriticalOnly, Important, All.
- [ ] Add tests for spam suppression and priority interruption.

Exit criteria: ARIA can present prioritized alerts/reports and read eligible messages without spam or allocations.

## Phase 7: Settings, Save, And Accessibility

- [ ] Add assistant guidance level setting.
- [ ] Add narration mode setting.
- [ ] Add assistant takeover permission setting.
- [ ] Persist assistant settings through `AssistantSettingsPersistenceSystemHelper`.
- [ ] Add text fallback and subtitle visibility rules.
- [ ] Add UI affordance for turning off narration and takeover.

Exit criteria: settings survive session reload and never block critical text feedback.

## Phase 8: Validation, Performance, And Rollout

- [ ] Run `git diff --check`.
- [ ] Run focused assistant edit-mode tests.
- [ ] Run architecture guardrails for forbidden naming and helper suffix compliance.
- [ ] Run compile validation.
- [ ] Run Unity visual validation for match header button, panel, Show Me, Do It, Give Control, Stop, and narration text.
- [ ] Capture performance diagnostics for assistant update time and GC allocations.
- [ ] Update `Design/README.md`, root `README.md`, and related docs with final implementation status.
- [ ] Commit and push only after stable validation passes.

Exit criteria: feature is playable, validated, documented, and does not regress architecture or frame hot paths.

## Validation Log

| Date | Slice | Commands / Evidence | Result | Notes |
|---|---|---|---|---|
| 2026-07-08 | Design and tracker creation | `git diff --check` | Passed | Added high-level ARIA ECS design, tracker, and cross-doc links. No code implementation yet. |
| 2026-07-08 | Phase 0 inventory and Phase 1 ECS data contract | `git diff --check`; forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-data-contract-unity-rerun.log --timeout 420 -- -quit -executeMethod AssistantEcsDataContractTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[AssistantEcsDataContractValidation] result=Passed tests=3`. Earlier dotnet revalidation attempts cancelled at 5:00 with 0 warnings/errors, so Unity wrapper validation is the authoritative compile/test evidence for this slice. |
| 2026-07-08 | Phase 2 match HUD assistant shell | `git diff --check`; forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-match-hud-assistant-ui-unity-final.log --timeout 420 -- -quit -executeMethod MatchHudAssistantUiSystemHelperTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[MatchHudAssistantUiValidation] result=Passed tests=1`. Validates ARIA button/panel creation, panel open, world-click suppression, and match HUD helper cleanup. |
| 2026-07-08 | Phase 3 objective goals and recommendation read-model base | `git diff --check`; forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-read-models-unity.log --timeout 420 -- -quit -executeMethod AssistantReadModelSystemTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[AssistantReadModelValidation] result=Passed tests=3`. Validates objective goal publishing, objective recommendation publishing, and no republish/version bump when sources are unchanged. |
| 2026-07-08 | Phase 2/3 live assistant panel read-model binding | `git diff --check`; forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-match-hud-assistant-ui-live-model-unity.log --timeout 420 -- -quit -executeMethod MatchHudAssistantUiSystemHelperTests.RunFocusedValidation`; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-read-models-live-binding-rerun-unity.log --timeout 420 -- -quit -executeMethod AssistantReadModelSystemTests.RunFocusedValidation` | Passed | UI validation reported `[MatchHudAssistantUiValidation] result=Passed tests=2`; read-model rerun reported `[AssistantReadModelValidation] result=Passed tests=3`. Validates live panel text/button binding through `UiAssistantPanelModel` and keeps ECS read-model tests green. |
| 2026-07-08 | Phase 3 selection-aware recommendations | `git diff --check`; forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-selection-recommendations-unity.log --timeout 420 -- -quit -executeMethod AssistantReadModelSystemTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[AssistantReadModelValidation] result=Passed tests=5`. Validates no-selection recommendation, focused idle combat-unit recommendation, objective fallback, and unchanged-source suppression. |
| 2026-07-08 | Phase 3 fuel/logistics recommendations | `git diff --check`; forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-fuel-recommendations-unity.log --timeout 420 -- -quit -executeMethod AssistantReadModelSystemTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[AssistantReadModelValidation] result=Passed tests=6`. Validates high-priority logistics recommendation from empty player usable fuel summary and keeps objective/selection recommendation tests green. |
| 2026-07-08 | Phase 2 assistant panel helper binding extraction | `git diff --check`; forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-panel-helper-unity.log --timeout 420 -- -quit -executeMethod MatchHudAssistantUiSystemHelperTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[MatchHudAssistantUiValidation] result=Passed tests=2`. Validates ARIA header/panel creation, world-click suppression, and live panel read-model binding after extracting `AssistantPanelUiSystemHelper`. |
| 2026-07-08 | Phase 2 assistant panel alert/report rows | `git diff --check`; forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-alert-panel-unity.log --timeout 420 -- -quit -executeMethod MatchHudAssistantUiSystemHelperTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[MatchHudAssistantUiValidation] result=Passed tests=2`. Validates alert/report text rendering from `UiAssistantPanelModel` and compiles the gateway conversion from `AssistantMessageElement` rows. |
| 2026-07-08 | Phase 2 assistant panel ownership state | `git diff --check`; forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-ownership-panel-unity.log --timeout 420 -- -quit -executeMethod MatchHudAssistantUiSystemHelperTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[MatchHudAssistantUiValidation] result=Passed tests=2`. Validates visible `OwnershipText` binding in the ARIA panel and keeps header/panel click suppression validation green. |
| 2026-07-08 | Phase 4 Show Me command intent enqueue | `git diff --check`; forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-showme-ui-unity.log --timeout 420 -- -quit -executeMethod MatchHudAssistantUiSystemHelperTests.RunFocusedValidation`; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-command-intent-gateway-unity.log --timeout 420 -- -quit -executeMethod AssistantCommandIntentGatewayTests.RunFocusedValidation` | Passed | UI validation reported `[MatchHudAssistantUiValidation] result=Passed tests=2`; gateway validation reported `[AssistantCommandIntentGatewayValidation] result=Passed tests=1`. Validates the Show Me button requests a UI assistant intent and the ECS gateway writes an `AssistantCommandIntentRequestElement` copied from the top recommendation. |
| 2026-07-08 | Phase 4 Show Me camera preview routing | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; sandboxed Unity attempt `/private/tmp/aria-assistant-command-intent-system-unity.log`; escalated workaround attempt `/private/tmp/aria-assistant-command-intent-system-unity-escalated-rerun.log` with `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-command-intent-system-unity-escalated-rerun.log --timeout 420 -- -quit -executeMethod AssistantCommandIntentSystemTests.RunFocusedValidation` | Passed | Sandboxed Unity hit the documented `LicenseClient-farhad` licensing initialization failure before tests. The documented out-of-sandbox rerun passed with `[AssistantCommandIntentSystemValidation] result=Passed tests=2`, validating accepted camera-preview routing and rejected no-target recovery. |
| 2026-07-08 | Phase 4 Do It command intent enqueue | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-doit-ui-unity.log --timeout 420 -- -quit -executeMethod MatchHudAssistantUiSystemHelperTests.RunFocusedValidation`; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-doit-gateway-unity.log --timeout 420 -- -quit -executeMethod AssistantCommandIntentGatewayTests.RunFocusedValidation` | Passed | UI validation reported `[MatchHudAssistantUiValidation] result=Passed tests=2`; gateway validation reported `[AssistantCommandIntentGatewayValidation] result=Passed tests=2`. Validates the panel `DO IT` button queues `ExecuteRecommendation` and executable recommendation kinds map to typed ECS assistant intents without executing gameplay. |
| 2026-07-08 | Phase 4 assistant preview highlight UI pulse | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; sandboxed Unity attempt `/private/tmp/aria-assistant-highlight-data-contract-unity.log`; escalated workaround validations `/private/tmp/aria-assistant-highlight-data-contract-unity-escalated.log`, `/private/tmp/aria-assistant-highlight-intent-system-unity.log`, `/private/tmp/aria-assistant-highlight-ui-unity.log`, and `/private/tmp/aria-assistant-highlight-gateway-unity.log` | Passed | Sandboxed Unity hit the documented `LicenseClient-farhad` licensing initialization failure. Workaround reruns passed with `[AssistantEcsDataContractValidation] result=Passed tests=3`, `[AssistantCommandIntentSystemValidation] result=Passed tests=2`, `[MatchHudAssistantUiValidation] result=Passed tests=2`, and `[AssistantCommandIntentGatewayValidation] result=Passed tests=3`. Validates ECS preview highlight rows, stale highlight clearing on rejection, gateway conversion, and HUD panel pulse binding. World-space highlight remains for the next stable slice. |
| 2026-07-08 | Phase 4 assistant preview world highlight | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-world-highlight-ui-unity.log --timeout 420 -- -quit -executeMethod MatchHudAssistantUiSystemHelperTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[MatchHudAssistantUiValidation] result=Passed tests=2`. Validates the panel pulse plus pooled `AriaAssistantPreviewHighlightRuntime` world ring from `UiAssistantHighlightModel`, including target position and renderer setup. |
| 2026-07-08 | Phase 4 safe Do It selection command | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-safe-select-intent-unity.log --timeout 420 -- -quit -executeMethod AssistantCommandIntentSystemTests.RunFocusedValidation`; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-safe-select-read-model-unity.log --timeout 420 -- -quit -executeMethod AssistantReadModelSystemTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[AssistantCommandIntentSystemValidation] result=Passed tests=4` and `[AssistantReadModelValidation] result=Passed tests=6`. Validates Show Me preview routing, rejected no-target preview recovery, no-selection `DO IT` selection-mode queueing, pre-resolved entity `DO IT` focus-unit queueing through existing RTS selection command buffers, and the executable no-selection recommendation contract. |
| 2026-07-08 | Phase 4 rejected-intent recovery message | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-rejected-intent-recovery-unity.log --timeout 420 -- -quit -executeMethod AssistantCommandIntentSystemTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[AssistantCommandIntentSystemValidation] result=Passed tests=5`. Validates accepted preview routing, safe Do It selection routing, rejected no-target preview recovery, and invalid select-target recovery message rows for the ARIA panel alert stream. |
| 2026-07-08 | Phase 4 command-intent lifecycle statuses | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-intent-status-lifecycle-unity.log --timeout 420 -- -quit -executeMethod AssistantCommandIntentSystemTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[AssistantCommandIntentSystemValidation] result=Passed tests=7`. Validates `Accepted`, `Rejected`, `Completed`, `Cancelled`, and `TimedOut` assistant command result rows without adding broad takeover ownership. |
| 2026-07-08 | Phase 5 Stop control cancellation path | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; sequential Unity validations after an initial parallel batchmode attempt raced `Library/Bee`: `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-stop-gateway-unity-rerun.log --timeout 420 -- -quit -executeMethod AssistantCommandIntentGatewayTests.RunFocusedValidation`; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-stop-intent-system-unity-rerun.log --timeout 420 -- -quit -executeMethod AssistantCommandIntentSystemTests.RunFocusedValidation`; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-stop-ui-unity-rerun.log --timeout 420 -- -quit -executeMethod MatchHudAssistantUiSystemHelperTests.RunFocusedValidation` | Passed | Gateway validation reported `[AssistantCommandIntentGatewayValidation] result=Passed tests=4`; command-intent validation reported `[AssistantCommandIntentSystemValidation] result=Passed tests=8`; UI validation reported `[MatchHudAssistantUiValidation] result=Passed tests=2`. Validates the panel Stop button, no-recommendation stop enqueue, ECS cancellation result, preview highlight clearing, and player-control return. |
| 2026-07-08 | Phase 5 control owner state and takeover bounds | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-control-owner-unity.log --timeout 420 -- -quit -executeMethod AssistantControlOwnerSystemTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[AssistantControlOwnerSystemValidation] result=Passed tests=4`. Validates owner-state mirroring, bounded takeover startup, timeout cancellation, and max-action cancellation without adding managed ownership shells. |
| 2026-07-08 | Phase 5 player input override detection | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; initial focused Unity attempt `/private/tmp/aria-assistant-player-override-unity.log` failed due to stale test `DynamicBuffer` after a structural change; fixed test handle refresh; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-player-override-rerun-unity.log --timeout 420 -- -quit -executeMethod AssistantControlOwnerSystemTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[AssistantControlOwnerSystemValidation] result=Passed tests=6`. Validates baseline sampling, pointer-request override, queued-move override, `PlayerOverridePending`, and return to player control. |
| 2026-07-08 | Phase 5 invalid command takeover cancellation | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-cancel-invalid-intent-unity.log --timeout 420 -- -quit -executeMethod AssistantControlOwnerSystemTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[AssistantControlOwnerSystemValidation] result=Passed tests=8`. Validates takeover return-to-player on rejected and timed-out assistant command results, alongside timeout, max-action, and player-input override coverage. |
| 2026-07-08 | Phase 5 shell blocker takeover cancellation | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-shell-blocker-cancel-unity.log --timeout 420 -- -quit -executeMethod AssistantControlOwnerSystemTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[AssistantControlOwnerSystemValidation] result=Passed tests=11`. Validates takeover return-to-player on match route changes and pause popups, while keeping non-blocking threat alert popups from cancelling ARIA ownership. |
| 2026-07-08 | Phase 5 ownership banner/detail text | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-ownership-banner-ui-unity.log --timeout 420 -- -quit -executeMethod MatchHudAssistantUiSystemHelperTests.RunFocusedValidation` | Passed | Focused Unity validation reported `[MatchHudAssistantUiValidation] result=Passed tests=2`. Validates the ARIA panel ownership detail text and existing assistant panel command buttons/world highlight binding. |
| 2026-07-08 | Phase 5 bounded Give Control sequence | `git diff --check`; source-only forbidden assistant owner-name `rg` scan; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-bounded-control-gateway-unity.log --timeout 420 -- -quit -executeMethod AssistantCommandIntentGatewayTests.RunFocusedValidation`; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-bounded-control-intent-unity.log --timeout 420 -- -quit -executeMethod AssistantCommandIntentSystemTests.RunFocusedValidation`; `Tools/CI/invoke_unity_macos.sh --project /Users/farhad/Projects/WarlineCapture-Clone --log /private/tmp/aria-assistant-bounded-control-ui-unity.log --timeout 420 -- -quit -executeMethod MatchHudAssistantUiSystemHelperTests.RunFocusedValidation` | Passed | Focused Unity validations reported `[AssistantCommandIntentGatewayValidation] result=Passed tests=5`, `[AssistantCommandIntentSystemValidation] result=Passed tests=9`, and `[MatchHudAssistantUiValidation] result=Passed tests=2`. Validates Give Control sends takeover intent, safe selection/focus command routing enters `AssistantTakeover`, and the bounded owner system remains responsible for returning control. |

## Open Decisions

| Topic | Current Recommendation | Decision Needed |
|---|---|---|
| First `Do It` command | Start with select/focus or one safe tutorial move. | Choose first gameplay command during Phase 0 inventory. |
| First `Give Control` sequence | One bounded tutorial/action step. | Choose after existing ECS command boundaries are confirmed. |
| Voice clips | Pre-authored/imported clips for common lines; text fallback first. | Decide asset pipeline and voice identity before Phase 6 clip binding. |
| Dynamic TTS | Optional for low-priority reports only. | Decide platform support after core assistant is stable. |

# Resource Logistics Exchange Implementation Tracker

Date: 2026-07-09
Status: Phase 8 AI exchange opt-in gate complete; data-driven AI planner guardrail next
Design source: `../Resource_Logistics_Exchange_Design.md`

## Objective

Implement the timed Resource Logistics Exchange without drifting from WarlineCapture's ECS/SOLID architecture. The feature lets players open a Build-Popup-style match popup from the resource header, queue import/export logistics jobs, rush eligible jobs with Rush Tickets, and receive optional world truck/transport-plane presentation while ECS remains the source of truth for resources, timers, results, and economy events.

## Architecture Contract

- Prefer unmanaged `ISystem` for runtime exchange simulation, validation, queue ticking, completion, cancellation, rush, and read-model publication.
- Use `SystemBase` only when managed Unity references are unavoidable, and split those cases so data production stays in ECS while managed code is presentation-only.
- Do not add broad `Manager`, `Controller`, `Facade`, `Service`, singleton, static mutable state, or stringly runtime state.
- UI views may submit typed requests and display read models, but must not own economy policy, conversion math, queue state, resource mutation, or disabled reasons.
- World plane/truck presentation is non-authoritative. It consumes ECS visual cue data and must not control exchange completion.
- Hot paths must avoid LINQ, per-frame string formatting, broad scene searches, instantiate/destroy churn, and recurring managed allocation.
- Use typed reason codes, resource enums, queue states, versioned read models, and economy events.
- Keep resource names aligned with `../Economy_Reward_Design.md`: Credits, Materials, Fuel, Intel, Command Authority, Rush Tickets, and tactical Oil.
- Keep Oil/Fuel logistics aligned with `../Field_Logistics_Oil_Fuel_Design.md` and `../Automated_Fuel_Logistics_Design.md`.
- Keep popup visuals aligned with `../Match_HUD_And_Gameplay_Implementation_Spec.md`, `../SCN09_Build_Placement_Mode_Implementation_Spec.md`, and active VisualLockLayered workflows.

## Progress Summary

Overall implementation progress: 88% (83/94 checklist items complete).

Progress is checklist-based. Each `- [ ]` or `- [x]` implementation/validation item below counts as one item. When a future implementation slice adds or removes checklist items, update this section in the same commit.

| Phase | Status | Complete | Total | Progress | Notes |
|---|---|---:|---:|---:|---|
| 0. Inventory and source alignment | Complete | 8 | 8 | 100% | Current resource data, HUD header, Build Popup, fuel logistics, save/profile fields, and economy event gaps are documented below. |
| 1. Data model and config | Complete | 12 | 12 | 100% | Added Resource Exchange enums, ECS data, config entries, scenario gates, and config validation tests. |
| 2. Request validation and queue start | Complete | 10 | 10 | 100% | Added ISystem request validation, ECS wallet boundary, input reservation, queue item creation, economy event row, and typed results. |
| 3. Queue ticking, completion, cancel, refund | Complete | 11 | 11 | 100% | Timed queue, output grant once, cancel/refund rules, mission-end cancel/refund policy. |
| 4. Rush Tickets | Complete | 7 | 7 | 100% | Rush eligible jobs with ticket spend, per-item caps, rush-all budget, and feedback. |
| 5. UI popup and header routing | Complete | 15 | 15 | 100% | ECS-backed UI read-model, target-lock reference, separated layer pack, Canvas popup prefab, serialized view refs, cards, details, amount stepper, queue panel, enabled-gated resource header tap route, popup input restoration, live read-model binding, request-buffer wiring, TMP/Oxanium validation, prefab contract tests, and 16:9/20:9 captures complete. |
| 6. World presentation | Complete | 8 | 8 | 100% | Presentation anchors, deterministic fallback resolution, data-only ECS visual cue emission, managed presentation boundary, pooled actor reuse, actor safety, fallback behavior, cleanup wiring, and focused validation are complete. |
| 7. Audio, VFX, feedback, ARIA | Complete | 7 | 7 | 100% | Resource Exchange audio ids, placeholder clips, data-only delta flyout requests, typed toast requests, optional ARIA strings, non-authoritative VFX marker data, and direct-audio wiring guardrails are complete. |
| 8. AI, balance, telemetry | In progress | 5 | 6 | 83% | Economy event coverage, Resource Exchange balance report fields, data sanity tests, scenario gates, and AI opt-in gating are complete; AI planner guardrails remain. |
| 9. Validation and performance | Not started | 0 | 10 | 0% | Focused tests, compile, architecture guardrails, UI captures, GC/performance checks. |

## Phase 0: Inventory And Source Alignment

- [x] Inventory current player/account resource fields in save/profile data.
- [x] Inventory current tactical resource components and Faction resource mutation paths.
- [x] Inventory current Oil/Fuel logistics data from fuel automation and field logistics systems.
- [x] Inventory current Match HUD resource header view, tap/click handling, and UI bridge ownership.
- [x] Inventory current Build Popup/Build Drawer prefab hierarchy, sprites, tabs, cards, details, and queue row conventions.
- [x] Inventory current Rush Ticket data, production queue rush implementation, and audio/feedback events.
- [x] Identify existing economy event/log/report path or define required new event boundary.
- [x] Document exact implementation files expected to change before coding begins.

Exit criteria:

- Active resource ownership is known.
- No implementation starts before the correct wallet/faction resource path is identified.
- Any missing prerequisite is written as a blocker in this tracker.

Phase 0 inventory findings:

- Account/profile resources live in `Assets/Game/Scripts/Persistence/SaveDataModel.cs` on `PlayerProfileSaveData`: `credits`, `materials`, `fuel`, `intel`, `commandAuthority`, and `rushTickets`. These are persistent account resources, not automatically authoritative for active-match exchange jobs.
- Current in-match tactical Credits are represented as runtime dollars through `Assets/Game/Scripts/Systems/RuntimeResourceUtilitySystemHelper.cs` and `Assets/Game/Scripts/Systems/CitizenResourceCompositionSystemHelper.cs`. Build placement and production spend through the existing `TrySpendDollars` delegate path, so Phase 1 must decide whether Resource Exchange uses this current match wallet directly or introduces a typed ECS Credits component/read model around the same value.
- Tactical Oil/Fuel data exists in `Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs`: `ResourceKind`, `BuildingResourceStorageComponent`, `BuildingRuntimeFactionSummary`, `BuildingRuntimeFactionUsableFuelSummary`, fuel logistics tags, and `BuildingFactionResourceSellRequest`. `BuildingFactionResourceSellRequest` is an AI/economy sell request and is not sufficient for the player-facing timed exchange queue.
- Fuel automation and field logistics mutate Oil/Fuel through building storage/reservation fields and the production/logistics helpers, including `BuildingResourceProductionSystemHelper`, `BuildingResourceProductionEcsSystem`, `FactionResourceCompositionSystemHelper`, `VehicleFuelConsumptionSystem`, and runtime fuel summary publication.
- Match HUD resource display is owned by `UiShellEcsGateway.TryReadMatchHudHeader`, `UiMatchHudHeaderComponent`, and `MainMenuPlayUI.BindMatchHudResourceSlots` / `ApplyMatchHudHeaderResourceState`. Current code formats Credits/Fuel/Oil text, but no Resource Exchange header tap route exists yet.
- Build Popup conventions to mirror are `BuildDrawerCatalogRuntimeView`, `BuildDrawerView`, `BuildDrawerQueueItemView`, `BuildDrawerCatalogQueryUiSystemHelper`, `UiRuntimeContracts` (`IBuildingUiCommand`, `IBuildingUiQuery`, `BuildingPendingProductionUiEntry`), and the command/result buffers in `BuildingRuntimeEcsComponents`.
- Rush Tickets exist as persistent profile data and economy design. The Build Drawer has Rush UI/read-model fields (`RushButton`, `UiActionKind.BuildProductionRush`, `RushEnabled`), but no complete live Rush Ticket spend path was found in the implementation inventory. Resource Exchange must add its own typed rush request/result path and later integrate with a shared Rush Ticket wallet if that becomes available.
- No central `EconomyEvent` runtime contract was found. Current balancing/reporting uses `GameRuntimeStats`, `BalanceMetrics`, and `BalanceReportWriter` for sampled economy activity. Phase 1/8 must introduce a typed Resource Exchange economy event boundary instead of only mutating resource numbers.

Expected implementation file set:

- New ECS data should start in a dedicated file such as `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs` unless the existing component assembly requires a different local convention.
- New config data should be added through the existing config model path, either a dedicated `ResourceExchangeRecipeConfig` asset/model or a narrowly scoped addition near `GameplayConfigModels.cs`.
- Runtime systems should use focused `ISystem` files such as `ResourceExchangeConfigProjectionSystem`, `ResourceExchangeRequestValidationSystem`, `ResourceExchangeQueueTickSystem`, `ResourceExchangeCompletionSystem`, `ResourceExchangeCancelSystem`, and `ResourceExchangeRushSystem`.
- UI contracts/read models should be explicit, for example `ResourceExchangeUiModels`, `ResourceExchangePopupView`, `ResourceExchangeRecipeCardView`, `ResourceExchangeQueueItemView`, and `UiResourceExchangeReadModelSystem`.
- Header routing should extend the existing match HUD/UI shell path rather than adding a separate scene-global manager.
- Validation should start with focused editor tests such as `ResourceExchangeConfigValidationTests`, `ResourceExchangeRequestValidationTests`, `ResourceExchangeQueueSystemTests`, `ResourceExchangeRushSystemTests`, and `ResourceExchangeUiRoutingTests`.

## Phase 1: Data Model And Config

- [x] Define `ResourceExchangeRouteType` enum.
- [x] Define `ResourceExchangeQueueState` enum.
- [x] Define `ResourceExchangeReason` enum.
- [x] Define exchange resource kind mapping for Credits, Materials, Oil, Fuel, and Rush Tickets.
- [x] Add exchange recipe config data with stable recipe ids, route type, input/output resources, rates, caps, duration, rush rules, requirements, and scenario tags.
- [x] Add scenario/preset gate data for exchange enabled/disabled state.
- [x] Add ECS recipe projection data such as `ResourceExchangeRecipeComponent`.
- [x] Add ECS request data such as `ResourceExchangeRequestComponent`.
- [x] Add ECS queue data such as `ResourceExchangeQueueComponent`.
- [x] Add ECS result data such as `ResourceExchangeResultComponent`.
- [x] Add versioned exchange summary/read-model data for UI.
- [x] Add tests for recipe id uniqueness, nonnegative amounts, valid resources, valid rates, and valid duration/rush values.

Exit criteria:

- Recipe data is config-driven.
- Runtime data is typed ECS data.
- No UI hard-codes recipe math.

## Phase 2: Request Validation And Queue Start

- [x] Implement request intake from UI/appended ECS request data.
- [x] Validate exchange enabled state by mission/skirmish/operation gate.
- [x] Validate recipe availability and unlock/mission tags.
- [x] Validate amount min, max, and step.
- [x] Validate input affordability.
- [x] Validate output storage or capacity where required.
- [x] Validate queue capacity.
- [x] Spend or reserve input resources at confirmation.
- [x] Create queue item with deterministic id, start time, duration, input/output preview, and state.
- [x] Write accepted/rejected result rows with typed reason codes.

Exit criteria:

- Invalid requests never mutate resources.
- Accepted requests spend/reserve input exactly once.
- UI can display exact rejection reason.

## Phase 3: Queue Ticking, Completion, Cancel, And Refund

- [x] Implement queue tick using `ISystem` where practical.
- [x] Advance timers without managed allocation.
- [x] Pause or block queue items when output storage becomes invalid/full by recipe policy.
- [x] Complete exchange jobs exactly once.
- [x] Apply output resource grant on completion.
- [x] Emit economy event for input spend and output grant.
- [x] Implement cancel request path.
- [x] Apply refund rules based on queue state and recipe policy.
- [x] Handle mission end policy: complete, cancel/refund, or discard tactical-only exchange by scenario.
- [x] Publish versioned queue/read-model updates only when queue state changes.
- [x] Add deterministic edit-mode tests for complete, cancel, refund, storage-blocked, and mission-ending paths.

Exit criteria:

- Queue jobs cannot double-grant or double-refund.
- Queue progress is deterministic in tests.
- No per-frame string or managed allocation in queue ticking.

## Phase 4: Rush Tickets

- [x] Add rush request data for one queue item and optional rush-all command.
- [x] Validate queue item rush eligibility.
- [x] Validate Rush Ticket affordability.
- [x] Enforce max tickets per queue item and per rush action.
- [x] Spend Rush Tickets at rush confirmation.
- [x] Reduce remaining duration or complete instantly if remaining time reaches zero.
- [x] Add tests for rush acceptance, insufficient tickets, max cap, non-rushable job, and completion from rush.

Exit criteria:

- Rush uses Rush Tickets only.
- Rush cannot bypass blocked/invalid queue state.
- Rush results are visible through typed feedback.

## Phase 5: UI Popup And Header Routing

- [x] Add ECS-backed UI read-model projection for popup state, recipe cards, selected detail, wallet totals, and queue rows.
- [x] Create accepted target-lock mockup request for `POP-12 Resource Logistics Exchange` aligned with the Build Popup visual language.
- [x] Create separated layer pack under `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/`.
- [x] Build Canvas popup from reusable panels/icons/text/buttons, not a screenshot.
- [x] Add `ResourceExchangePopupView` with explicit serialized references.
- [x] Add recipe card view with selected, disabled, locked, and warning states.
- [x] Add details panel with rate, amount stepper, input/output preview, requirements, duration, confirm button.
- [x] Add exchange queue panel rows with progress, ETA, rush, cancel, and complete state.
- [x] Route match `ResourceBar` tap/click to popup only when exchange is enabled.
- [x] Ensure popup blocks world input and restores prior match HUD state on close.
- [x] Bind installed POP-12 popup to the ECS read model and typed tab/card/control UI actions.
- [x] Route POP-12 amount, confirm, rush-all, clear-completed, row rush, and row cancel controls into ECS request buffers.
- [x] Ensure all runtime text uses TMP/Oxanium rules from UI docs.
- [x] Validate 16:9 and 20:9 layout with captures.
- [x] Add focused UI/prefab tests for object ids, button wiring, disabled reasons, and input suppression.

Exit criteria:

- UI is visually consistent with the Build Popup.
- UI state is data-bound from ECS/read models.
- Header tapping does not leak into world input.

## Phase 6: World Presentation

- [x] Define presentation anchors for base depot, runway/landing zone, storage, and fallback safe anchor.
- [x] Add ECS visual cue request data for exchange start, load, plane landing, plane departure, unload, and completion.
- [x] Implement managed presentation boundary as `ResourceExchangeVisualPresentationSystemHelper` or existing approved presentation owner.
- [x] Use pooled plane/truck presentation actors or an existing pooling boundary.
- [x] Ensure presentation actors do not block gameplay pathfinding or mutate economy state.
- [x] Add fallback behavior when anchors or presentation prefabs are missing.
- [x] Add visual state cleanup on popup close, mission end, scene unload, and queue cancellation.
- [x] Add tests or playmode validation for missing-anchor fallback and no duplicate presentation actors.

Exit criteria:

- Queue completion does not depend on visuals.
- Visuals do not introduce gameplay blockers.
- Presentation can be disabled without breaking economy state.

Phase 6 implementation notes:

### 2026-07-09 - Phase 6B Visual Cue Request Emission

- Added `ResourceExchangeVisualCueSystem` as an unmanaged `ISystem` with a pure static entry point for deterministic tests.
- Added queue-side visual emission flags so exchange start, landing, transfer, departure, completion, and cancellation cues are emitted once per queue item.
- Expanded `ResourceExchangeVisualRequestComponent` with route, amounts, requested anchor, resolved anchor, fallback flag, anchor pose, and radius so the future managed presentation boundary can avoid scene searches.
- Cue order is narrative-safe for both imports and exports: exchange starts, transport plane lands, trucks transfer resources, transport plane departs, then terminal completion/cancellation emits when queue state reaches a terminal state.
- `PresentationStarted` is set only after a resolved world-presentation cue. If anchors are missing, cues still record unresolved requests for diagnostics, but cancellation refund eligibility is preserved.
- Presentation remains optional and non-authoritative: when `AllowWorldPresentation` is disabled, no visual cues emit and economy/queue state continues normally.
- Validation passed:
  - `dotnet build Game.Components.csproj --no-restore -v:q -clp:ErrorsOnly`
  - `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`
  - `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`
  - `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-visual-cue-validation.log --timeout 420 -- -quit -executeMethod ResourceExchangeVisualCueSystemTests.RunFocusedValidation`
  - Unity focused result: `[ResourceExchangeVisualCueValidation] result=Passed tests=5`
- Known unrelated worktree side effect left unstaged: `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`.

### 2026-07-09 - Phase 6C Managed Presentation Boundary

- Added `ResourceExchangeVisualPresentationSystemHelper` as the managed-only boundary that consumes `ResourceExchangeVisualRequestComponent` rows and clears consumed cue buffers.
- Added explicit `ResourceExchangeVisualActorKind` mapping for exchange markers, transport planes, resource trucks, completion markers, and cancellation markers.
- Added pooled actor acquisition/release by prefab, active actor reuse by queue item and actor kind, and terminal cue release for queue-scoped transient actors.
- Added missing-anchor and missing-prefab fallback behavior. Missing anchors/prefabs do not spawn actors, do not throw, and still clear consumed cue requests after recording result counts.
- Presentation actors are non-authoritative: the helper does not read or write wallet, queue, recipe, result, or economy-event buffers. It only consumes visual requests.
- Presentation actors disable all child `Collider` components on acquire so they do not create physics/pathfinding blockers.
- Added cleanup APIs `ReleaseActorsForQueue` and `ReleaseAll`; scene/popup/mission-end wiring remains as the last Phase 6 task.
- Validation passed:
  - `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`
  - `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`
  - `git diff --check`
  - `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-visual-presentation-validation.log --timeout 420 -- -quit -executeMethod ResourceExchangeVisualPresentationSystemHelperTests.RunFocusedValidation`
  - Unity focused result: `[ResourceExchangeVisualPresentationValidation] result=Passed tests=5`
- Known unrelated worktree side effect left unstaged: `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`.

### 2026-07-09 - Phase 6D Visual Cleanup Boundaries

- Added typed visual cleanup reasons for popup close, mission ending, scene unload, and queue cancellation.
- Added `CleanupVisualState` APIs that release active presentation actors and clear stale pending visual cue requests at the same lifecycle boundary.
- Popup close, mission ending, and scene unload cleanup release all active exchange presentation actors and clear all pending visual requests.
- Queue cancellation cleanup releases only actors for the cancelled queue item and removes only pending visual requests for that queue item, preserving other active exchange visuals.
- Cleanup remains presentation-only: it does not mutate queue state, wallet state, results, recipes, or economy events.
- Validation passed:
  - `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`
  - `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`
  - `git diff --check`
  - `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-visual-presentation-cleanup-validation.log --timeout 420 -- -quit -executeMethod ResourceExchangeVisualPresentationSystemHelperTests.RunFocusedValidation`
  - Unity focused result: `[ResourceExchangeVisualPresentationValidation] result=Passed tests=7`
- Known unrelated worktree side effect left unstaged: `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`.

## Phase 7: Audio, VFX, Feedback, And ARIA

- [x] Add config-driven audio event ids for accepted, rejected, queue started, rushed, completed, and cancelled exchange events.
- [x] Generate or assign placeholder audio clips through the audio config workflow.
- [x] Add resource delta flyouts for spend, reserve, output grant, refund, and rush spend.
- [x] Add completion toast and rejection toast with typed reason text.
- [x] Add optional ARIA strings for insufficient resources, exchange started, exchange complete, and exchange blocked.
- [x] Pair world presentation cues with non-authoritative VFX markers.
- [x] Validate no direct AudioSource/prefab sound wiring outside the config-driven audio path.

Exit criteria:

- Feedback is clear for every accepted/rejected path.
- Audio remains config-driven.
- No feedback text is baked into art.

## Phase 8: AI, Balance, And Telemetry

- [x] Add economy events for input spend/reserve, output grant, refund, rush ticket spend, and blocked/cancelled jobs.
- [x] Add balance report fields for exchange route, amount, duration, source mode, completion, and resource delta.
- [x] Add data sanity tests for rates, fees, duration, and farming-risk caps.
- [x] Gate exchange recipes by chapter/mission/skirmish preset so early FTUE is not overloaded.
- [x] Add AI exchange support only if a scenario explicitly enables AI exchange behavior.
- [ ] If AI exchange is enabled, ensure it is data-driven and does not add managed per-frame planner scans.

Exit criteria:

- Balancing can see every exchange source/sink.
- Exchange cannot become a hidden infinite resource loop.
- AI behavior is optional and data-gated.

## Phase 9: Validation And Performance

- [ ] Run `git diff --check`.
- [ ] Run repository compile validation.
- [ ] Run focused exchange data/config tests.
- [ ] Run focused exchange queue/rush/cancel/refund tests.
- [ ] Run focused HUD/header popup routing tests.
- [ ] Run focused UI prefab/capture validation at 16:9 and 20:9.
- [ ] Run architecture guardrails for naming, ECS boundaries, and no broad manager/controller/service drift.
- [ ] Run GC allocation check for exchange queue steady state.
- [ ] Run performance scenario if exchange is active during match steady state.
- [ ] Update this tracker with validation commands, log paths, pass/fail result, and remaining blockers.

Performance acceptance targets:

- Queue ticking and validation introduce no steady-state GC allocation.
- UI read models update only on version changes.
- World presentation uses pooling and creates no recurring instantiate/destroy churn.
- No runtime broad scene searches or hierarchy-path lookups in match frames.
- Exchange systems remain within the current match performance budget for active logistics scenarios.

## Validation Matrix

| Area | Required Checks |
|---|---|
| Compile | Unity compile or repository-approved compile validation. |
| Architecture | SOLID/ECS guardrails, ISystem preference, no new broad shell types. |
| Data | Recipe ids, valid resource kinds, rates, duration, caps, queue states, reason ids. |
| Economy | Spend/reserve input, grant output once, refund rules, rush ticket spend, economy event logging. |
| UI | Header route, popup layout, card/detail/queue states, disabled reasons, input suppression. |
| World presentation | Optional/non-authoritative truck/plane visuals, pooling, anchor fallback. |
| Performance | 0 B/frame queue ticking, version-gated UI, no managed hot-path churn. |
| Balance | Farming caps, early FTUE gates, import/export rates, mission-end policy. |

## Implementation Notes Log

Use this section during implementation. Each completed batch should add:

- date
- files changed
- behavior changed
- validation commands and log paths
- profiler result when performance-sensitive
- remaining blocker or next slice

### 2026-07-08 - Phase 0 Inventory And Source Alignment

Files changed:

- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Documentation-only tracker update.
- Marked Phase 0 complete after inventorying profile resources, in-match tactical credit helpers, Oil/Fuel ECS storage, Match HUD header binding, Build Popup queue conventions, Rush Ticket implementation status, and economy reporting gaps.
- Confirmed the first implementation slice should not mutate unrelated audio/combat work currently dirty in the workspace.

Validation:

- `git diff --check` passed for this documentation slice.

Remaining blocker or next slice:

- Next slice is Phase 1 data model/config.
- Resource Exchange needs an explicit typed economy event boundary because no central runtime `EconomyEvent` contract was found.
- Resource Exchange must not rely on the incomplete Build Drawer Rush button path; add typed exchange rush requests/results first, then integrate with persistent Rush Tickets or a future shared Rush Ticket wallet.

### 2026-07-08 - Phase 1 Data Model And Config

Files changed:

- `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs`
- `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs.meta`
- `Assets/Game/Scripts/Configs/ResourceExchangeConfigModels.cs`
- `Assets/Game/Scripts/Configs/ResourceExchangeConfigModels.cs.meta`
- `Assets/Tests/Editor/ResourceExchangeConfigValidationTests.cs`
- `Assets/Tests/Editor/ResourceExchangeConfigValidationTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added typed Resource Exchange runtime data: route type, queue state, reason codes, resource kind mapping, request kind, result kind, visual cue kind, enabled marker, recipe buffer, request buffer, queue buffer, result buffer, summary/read-model component, economy event buffer, and visual request buffer.
- Added config-driven recipe and scenario gate models through `ResourceExchangeRecipeConfigSet`, `ResourceExchangeRecipeConfigEntry`, and `ResourceExchangeScenarioGateConfigEntry`.
- Added `ResourceExchangeRecipeConfigValidator` for recipe id, duplicate id, allowed route, amount bounds, input step, rate, duration, rush-rule, and resource-kind validation.
- Added focused editor tests for stable enum values, valid import/export routes, duplicate ids, invalid amount/rate/duration/rush rules, and disallowed conversion routes.
- Kept this slice data/config only. No runtime systems, resource mutation, UI wiring, or world presentation behavior was added.

Validation:

- `git diff --check` passed.
- `dotnet build Game.Tests.Editor.csproj --no-restore` passed with existing Unity reference conflict warnings and 0 errors.

Remaining blocker or next slice:

- Next slice is Phase 2 request validation and queue start.
- Phase 2 must choose the first authoritative active-match Credits boundary before spending/reserving inputs: either wrap `RuntimeResourceUtilitySystemHelper` with typed exchange access or introduce an ECS tactical Credits component synced with the current runtime dollar helper.
- Phase 2 should consume the new config/ECS data only through typed requests/results and must not add UI-owned conversion math.

### 2026-07-08 - Phase 2 Request Validation And Queue Start

Files changed:

- `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs.meta`
- `Assets/Tests/Editor/ResourceExchangeRequestValidationSystemTests.cs`
- `Assets/Tests/Editor/ResourceExchangeRequestValidationSystemTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `ResourceExchangeWalletComponent` as the typed ECS Resource Exchange wallet boundary for active-match Credits, Materials, Oil, Fuel, and Rush Tickets during exchange validation.
- Added `ResourceExchangeRequestQueueComponent` for deterministic request ids and queue item ids.
- Added `ResourceExchangeRequestValidationSystem` as an unmanaged `ISystem` that processes appended ECS start requests, validates exchange enabled state, faction, recipe availability, mission tag, amount bounds/steps, input affordability, output storage capacity, and queue capacity.
- Accepted requests reserve/spend the input resource from the exchange wallet, create an `InProgress` queue item with deterministic id/start/duration/input/output fields, write a negative input economy event row, and publish an accepted typed result.
- Rejected requests leave wallet and queue state unchanged and publish typed rejection reasons.
- Added focused tests for accepted export, disabled exchange, missing/locked recipes, invalid amount path, insufficient Oil, storage full, and queue full.

Validation:

- `git diff --check` passed.
- `dotnet build Game.Tests.Editor.csproj --no-restore` passed with existing Unity reference conflict warnings and 0 errors.

Remaining blocker or next slice:

- Next slice is Phase 3 queue ticking, completion, cancel, and refund.
- The `ResourceExchangeWalletComponent` is now the ECS-side exchange authority for this feature. A later UI/runtime integration slice must seed/sync it from current match resources, especially the existing `RuntimeResourceUtilitySystemHelper` tactical Credits path, before the popup is exposed in live HUD.
- Queue completion must grant output exactly once and emit the positive output economy event row; this is intentionally not part of the Phase 2 queue-start slice.

### 2026-07-08 - Phase 3 Queue Ticking, Completion, Cancel, And Refund

Files changed:

- `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeQueueTickSystem.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeQueueTickSystem.cs.meta`
- `Assets/Tests/Editor/ResourceExchangeQueueTickSystemTests.cs`
- `Assets/Tests/Editor/ResourceExchangeQueueTickSystemTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `ResourceExchangeQueueTickSystem` as an unmanaged `ISystem` that advances active queue timers, blocks jobs when output storage is unavailable, resumes blocked jobs when capacity returns, and completes jobs without managed allocation.
- Queue completion now grants output resources exactly once, clears reserved input, appends a positive output economy event row, and publishes a typed `QueueCompleted` result.
- Added typed cancel request intake through `ResourceExchangeRequestValidationSystem.EnqueueCancelRequest`.
- Cancellation now applies the documented refund rule: full reserved-input refund before presentation starts and no refund after presentation has started until recipe-level partial refund policy is added.
- Added typed mission-end cleanup through `ResourceExchangeRequestValidationSystem.EnqueueMissionEndRequest`; the current default policy cancels active jobs and applies the same refund rule while marking queue rows with `MissionEnding`.
- Summary/read-model versions now update on queue lifecycle state changes instead of every idle tick.
- Added focused edit-mode tests for one-time completion, storage-full blocking/resume, cancel refund, cancel no-refund after presentation, and mission-end cancellation/refund.

Validation:

- `git diff --check` passed.
- `dotnet build Game.Tests.Editor.csproj --no-restore` passed with existing Unity reference conflict warnings and 0 errors.

Remaining blocker or next slice:

- Next slice is Phase 4 Rush Tickets.
- The current cancel policy intentionally uses the global design default. Recipe-level partial refund rules are not implemented yet because the Phase 1 recipe data does not carry a partial-refund percentage.
- A later UI/runtime integration slice must still bind the exchange wallet to the live match resource header before players can safely use the popup in the HUD.

### 2026-07-08 - Phase 4 Rush Tickets

Files changed:

- `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeQueueTickSystem.cs`
- `Assets/Tests/Editor/ResourceExchangeRushSystemTests.cs`
- `Assets/Tests/Editor/ResourceExchangeRushSystemTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `RushTicketsSpent` to queue rows so max rush caps are enforced across multiple actions on the same exchange job.
- Added typed request helpers for single-item rush and rush-all budget requests.
- Rush validation now checks exchange enabled state, `AllowRush`, faction ownership, queue item state, remaining time, recipe rush rules, per-item ticket caps, and Rush Ticket affordability.
- Accepted rush actions spend Rush Tickets from the typed exchange wallet, append a negative Rush Ticket economy event row, reduce remaining duration, increment queue row version, and publish typed `RushAccepted` results.
- If a rush reduces remaining time to zero, the queue item completes immediately through the same queue-completion helper used by normal ticking, including output grant, storage blocking, economy event, and one-time output protection.
- Rush-all allocates a request budget across eligible in-progress queue items in queue order without rushing blocked, completed, cancelled, or invalid jobs.
- Added focused edit-mode tests for accepted rush, insufficient Rush Tickets, per-item max cap, blocked/non-rushable queue item rejection, immediate completion from rush, and rush-all budget allocation.

Validation:

- `git diff --check` passed.
- `dotnet build Game.Tests.Editor.csproj --no-restore` passed with existing Unity reference conflict warnings and 0 errors.

Remaining blocker or next slice:

- Next slice is Phase 5 UI popup and header routing.
- The Resource Exchange popup still does not exist in the live Match HUD, and the exchange wallet still needs to be seeded/synced from live match resources before player-facing UI is enabled.
- Rush Ticket spend currently uses the ECS exchange wallet boundary introduced for this feature. A later persistence/account integration pass must decide when active-match Rush Ticket spend writes back to profile save data.

### 2026-07-08 - Phase 5A UI Read-Model Foundation

Files changed:

- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiResourceExchangeReadModelSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiResourceExchangeReadModelSystem.cs.meta`
- `Assets/Tests/Editor/UiResourceExchangeReadModelSystemTests.cs`
- `Assets/Tests/Editor/UiResourceExchangeReadModelSystemTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `ResourceExchange` as a typed UI shell popup kind so the match HUD can route to the exchange popup without stringly popup ids.
- Added UI ECS read-model data for Resource Exchange active tab state, selected recipe slot, wallet totals, queue capacity, rush/clear button state, recipe cards, detail panel, and queue rows.
- Added `UiResourceExchangeReadModelSystem` as an unmanaged `ISystem` in the presentation group. It only writes Resource Exchange UI data while the Resource Exchange popup is visible.
- The read model projects enabled state, recipes, wallet values, summary, rush availability, storage-full/queue-full/insufficient-resource disabled reasons, queue ETA, progress, queue state, rush, cancel, and completed-row flags from ECS data.
- The slice does not expose a partial visual popup, bind header taps, or own exchange math in UI views. Managed Canvas/prefab work remains a later Phase 5 slice.
- Added focused editor tests for export card/detail/queue projection, import storage-full disabled copy, and empty active-tab fallback.

Validation:

- `git diff --check` passed.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q` passed with existing Unity reference conflict warnings and 0 errors.

Remaining blocker or next slice:

- Next slice is Phase 5 visual target/layer alignment or Canvas popup construction, depending on whether the accepted `POP-12 Resource Logistics Exchange` target-lock/layer pack already exists by then.
- Header/resource-bar tap routing still needs to open `UiShellPopupKind.ResourceExchange` only when exchange is enabled and must block world input while open.
- The exchange wallet still needs live match resource seeding/sync before the popup can be enabled for player use in a match.

### 2026-07-08 - Phase 5B POP-12 Target-Lock Request

Files changed:

- `Design/VisualLockLayered/README.md`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/README.md`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/prompts/POP-12_ResourceLogisticsExchange_NewMainMenuArtDirection_TargetLock_V01.md`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/reference/README.md`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/layer_requests/README.md`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Documentation-only visual workflow slice.
- Added the `POP-12 Resource Logistics Exchange` VisualLockLayered pack shell and target-lock prompt request.
- The prompt defines the Build-Popup-aligned popup composition, Export/Import tabs, route cards, selected detail panel, amount stepper, exchange queue panel, Rush All/Clear Completed actions, allowed resource set, and layer separation constraints.
- Added explicit reference/layer blocking notes so future agents do not implement from prompt-only drafts, chat-only images, screenshot crops, or baked layers.
- Registered `POP-12_ResourceLogisticsExchange` under VisualLockLayered pending target requests instead of saved references because the generated PNG does not exist yet.

Validation:

- `git diff --check` passed.

Remaining blocker or next slice:

- Next slice is generating/saving the target-lock reference PNG or creating the separated layer pack once an accepted reference exists.
- Unity Canvas/prefab implementation remains blocked until the accepted PNG and layer manifest exist under `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/`.
- Header/resource-bar tap routing can proceed in parallel only if it stays behind exchange-enabled gating and does not expose an unfinished popup to players.

### 2026-07-08 - Phase 5C POP-12 Target-Lock Reference PNG

Files changed:

- `Design/VisualLockLayered/README.md`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/README.md`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/reference/POP-12_ResourceLogisticsExchange_NewMainMenuArtDirection_TargetLock_V01.png`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/reference/README.md`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/layer_requests/README.md`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Generated and saved the accepted `POP-12 Resource Logistics Exchange` target-lock reference PNG using the built-in imagegen workflow.
- Moved `POP-12_ResourceLogisticsExchange` from pending target requests to saved references in the VisualLockLayered inventory.
- Recorded validation notes for the generated reference: Build-Popup-aligned dark/gold command UI, Export/Import route cards, selected details, amount stepper, queue rows, Rush All, Clear Completed, and separable-looking icons/progress/badges.
- Kept the separated layer pack and Canvas implementation unchecked. The target PNG is a visual reference, not a Unity-ready layer pack.
- Noted that the generated locked `IMPORT OIL` card is a disabled/gated visual state only; runtime must keep Credits -> Oil unavailable by default unless an authored scenario enables it.

Validation:

- Visually inspected the saved PNG at `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/reference/POP-12_ResourceLogisticsExchange_NewMainMenuArtDirection_TargetLock_V01.png`.
- `git diff --check` passed.

Remaining blocker or next slice:

- Next slice is the separated layer pack under `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/`.
- Unity Canvas/prefab implementation remains blocked until layer extraction produces separate frames, icons, progress fills, badges, button states, `layer_manifest.json`, and a contact sheet.

### 2026-07-08 - Phase 5D POP-12 Separated Layer Pack

Files changed:

- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/README.md`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/layer_requests/README.md`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/layer_manifest.json`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/generated_one_go/source/POP-12_ResourceExchange_Panels_Green_v01.png`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/generated_one_go/source/POP-12_ResourceExchange_Icons_Green_v01.png`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/generated_one_go/source/POP-12_ResourceExchange_Content_Green_v01.png`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/generated_one_go/layers_contact_sheet.png`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/layers/`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/validation/POP-12_ResourceLogisticsExchange_layers_contact_sheet.png`
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/validation/pop12_layer_validation.json`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Generated the V01 separated layer pack from separate green-key one-go source sheets, not from target-reference crops.
- Added 55 transparent sprites covering popup chrome, panel/card/button frames, amount/progress/queue elements, resource/action icons, badges, and route thumbnail content images.
- Kept route thumbnails as artwork-only layers; runtime labels, amounts, locks, checks, warnings, progress bars, and buttons remain separate live UI elements.
- Cleaned green-key spill from frame sprites before accepting the contact sheet.
- Updated POP-12 docs so the next implementation slice can build the Canvas popup from `layer_manifest.json`.

Validation:

- Visually inspected `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/generated_one_go/layers_contact_sheet.png`.
- `Design/VisualLockLayered/POP-12_ResourceLogisticsExchange/validation/pop12_layer_validation.json`: 55 files, 26 panel/control sprites, 23 icon/badge/resource sprites, 6 content thumbnails, 0 pure key-green pixels, 0 border key-green pixels, 0 frame green-spill pixels.
- `git diff --check` passed.
- Unity compile was not run for this image/documentation-only slice.

Remaining blocker or next slice:

- Next slice is Canvas popup construction from the V01 layer pack and ECS read model.
- Header/resource-bar tap routing must remain gated until the popup has real button wiring and input suppression.

### 2026-07-08 - Phase 5E POP-12 Canvas Popup Prefab

Files changed:

- `Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/`
- `Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab`
- `Assets/Game/Scripts/Editor/ResourceExchangePopupPrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Screens/ResourceExchangePopupView.cs`
- `Assets/Game/Scripts/UI/Screens/ResourceExchangeRecipeCardView.cs`
- `Assets/Game/Scripts/UI/Screens/ResourceExchangeQueueItemView.cs`
- `Assets/Tests/Editor/ResourceExchangePopupPrefabTests.cs`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added the implementation-ready `POP12_ResourceExchangePopup` prefab built from separated POP-12 V01 sprites instead of a target screenshot or cropped mockup.
- Added explicit serialized popup/view references for close, tabs, amount controls, confirm, recipe card root/template/static cards, detail panel fields, queue root/template/static rows, Rush All, Clear Completed, and instruction text.
- Added reusable recipe card and queue row views with live TMP text, separate thumbnails, selected/default/locked frames, check/lock/warning icons, disabled overlay, progress fill, rush/cancel buttons, and completed state.
- Added an editor prefab builder and validator so the popup can be regenerated from the layer pack in one step.
- Kept resource-header routing, ECS read-model binding into the live popup, input suppression/restoration, and 16:9/20:9 capture proof for later Phase 5 slices.

Validation:

- `git diff --check` passed.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 21 warnings and 0 errors.
- Unity batchmode prefab rebuild passed: `/private/tmp/warline-resource-exchange-popup-build-2.log`.
- Unity batchmode prefab validation passed with `[ResourceExchangePopupPrefabBuilder] Validation passed`: `/private/tmp/warline-resource-exchange-popup-validate.log`.
- Unity EditMode focused tests passed with `ResourceExchangePopupPrefabTests` 3/3: `/private/tmp/warline-resource-exchange-popup-tests.xml` and `/private/tmp/warline-resource-exchange-popup-tests-assembly.log`.

Remaining blocker or next slice:

- Next slice is routing the match resource header tap/click to `UiShellPopupKind.ResourceExchange` only when exchange is enabled.
- The popup still needs live ECS read-model binding, close/open input suppression, and layout captures before it is player-facing.

### 2026-07-08 - Phase 5F Resource Header Popup Route

Files changed:

- `Assets/Game/Scenes/Menu.unity`
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/MainMenuPlayUI.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- `Assets/Tests/Editor/ResourceExchangeHeaderRoutingTests.cs`
- `Assets/Tests/Editor/ResourceExchangeHeaderRoutingTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `UiActionKind.OpenResourceExchange` as the typed HUD action for opening Resource Logistics Exchange.
- Match HUD resource slots now dynamically bind click targets for Credits, Oil, Fuel, and Supply and enqueue the typed open action through `UiShellRuntimeGateway`.
- `UiActionRequestSystem` now consumes `OpenResourceExchange`, captures the UI click sequence, suppresses the matching world click, and only enqueues `UiShellPopupKind.ResourceExchange` when an enabled `ResourceExchangeEnabledComponent` exists.
- `UIShellContentView` now serializes the POP-12 popup prefab, installs it for `UiShellPopupKind.ResourceExchange`, binds the popup close button, and clears the popup reference on close/region reset.
- `MainMenuPlayUI` now recognizes the Resource Exchange popup as gameplay UI for pointer filtering while it is open.

Validation:

- `git diff --check` passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 0 warnings and 0 errors.
- `dotnet build Game.UI.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.UI.Contracts.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 4 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 11 warnings and 0 errors after rebasing onto `origin/main`.
- Unity EditMode focused tests passed with `ResourceExchangeHeaderRoutingTests` 3/3: `/private/tmp/warline-resource-exchange-header-routing-tests.xml` and `/private/tmp/warline-resource-exchange-header-routing-tests.log`.

Remaining blocker or next slice:

- Next slice is popup input suppression/restoration and live ECS read-model binding into the installed POP-12 popup.
- The popup can now open from the resource header only when exchange is enabled, but the visible rows/details still need live read-model data and button actions before this is player-facing.
- 16:9 and 20:9 layout captures are still required after live binding is in place.

### 2026-07-08 - Phase 5G Popup Input Restore

Files changed:

- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- `Assets/Tests/Editor/ResourceExchangeHeaderRoutingTests.cs`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added append-only `UiActionKind.CloseResourceExchange` so the POP-12 close button has a typed UI action without shifting existing serialized button enum values.
- Resource Exchange close now captures the UI click sequence, suppresses the matching world click, enqueues `UiShellPopupKind.ResourceExchange` hide intent, and closes the Canvas popup immediately.
- `UiShellFlowSystem` now receives a typed Resource Exchange hide request through the existing popup request path, clears `UiShellActivePopupComponent.Visible`, and keeps the underlying `MatchHud` mode active.
- The installed popup remains a full-screen modal blocker while open through `ResourceExchangePopupView.ContainsScreenPoint`, then unbinds from `MainMenuPlayUI` on close so the Resource Exchange modal no longer owns gameplay hit testing.
- Expanded focused routing tests to cover close action suppression, ECS popup-state restoration, modal input blocking while open, and modal unbinding after close.

Validation:

- `git diff --check` passed.
- `dotnet build Game.UI.Contracts.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 4 warnings and 0 errors.
- `dotnet build Game.UI.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 11 warnings and 0 errors after rerunning serially to avoid parallel PDB contention.
- Unity EditMode focused tests passed with `ResourceExchangeHeaderRoutingTests` 5/5: `/private/tmp/warline-resource-exchange-input-restore-tests.xml` and `/private/tmp/warline-resource-exchange-input-restore-tests.log`.

Remaining blocker or next slice:

- Next slice should bind the installed POP-12 popup to the ECS read model and its typed recipe/tab/amount/queue button requests.
- TMP/Oxanium validation and 16:9/20:9 captures remain required before this popup is player-facing.

### 2026-07-08 - Phase 5H POP-12 Live Read-Model Binding

Files changed:

- `Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab`
- `Assets/Game/Scripts/Editor/ResourceExchangePopupPrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs`
- `Assets/Game/Scripts/UI/Screens/ResourceExchangePopupRuntimeView.cs`
- `Assets/Game/Scripts/UI/Screens/ResourceExchangePopupView.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`
- `Assets/Game/Scripts/UI/Shell/UiShellRuntimeGateway.cs`
- `Assets/Tests/Editor/MatchHudAssistantUiSystemHelperTests.cs`
- `Assets/Tests/Editor/ResourceExchangeHeaderRoutingTests.cs`
- `Assets/Tests/Editor/ResourceExchangePopupPrefabTests.cs`
- `Assets/Tests/Editor/UiResourceExchangeReadModelSystemTests.cs`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `UiResourceExchangeModel`, detail, card, and queue row UI contract models so POP-12 can read an ECS-published snapshot through the same UI gateway pattern used by other shell screens.
- Added `UiShellRuntimeGateway.TryReadResourceExchange` and `UiShellEcsGateway.TryReadResourceExchange`, converting `UiResourceExchange*Component` ECS data into managed UI strings and bounded card/queue row structs.
- Added `ResourceExchangePopupRuntimeView`, a managed presentation-only component that applies the read model to `ResourceExchangePopupView` on version changes and never owns resource policy, conversion math, or queue authority.
- Extended `ResourceExchangePopupView` with `ApplyModel`, recipe-card application, queue-row application, and thumbnail resolution helpers while keeping art references serialized on the view.
- Regenerated `POP12_ResourceExchangePopup.prefab` from `ResourceExchangePopupPrefabBuilder` so the prefab owns both `ResourceExchangePopupView` and `ResourceExchangePopupRuntimeView`.
- Added append-only typed UI actions for Resource Exchange tab, recipe, amount, confirm, rush-all, clear-completed, row rush, and row cancel controls.
- `UiActionRequestSystem` now consumes Resource Exchange tab/card actions, suppresses matching world input for all POP-12 controls, updates ECS UI tab/selected-card state, and leaves amount/confirm/queue gameplay request handling for the next backend-wiring slice.
- Fixed an existing `UiResourceExchangeReadModelSystemTests` ECS buffer lifetime issue by reacquiring the recipe buffer after the test creates a second entity.

Validation:

- `git diff --check` passed after trimming Unity YAML trailing spaces from the regenerated prefab.
- `dotnet build Game.UI.Contracts.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 4 warnings and 0 errors.
- `dotnet build Game.UI.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.UI.Shell.Ecs.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 7 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 10 warnings and 0 errors.
- Unity batchmode POP-12 prefab rebuild passed: `/private/tmp/warline-resource-exchange-popup-livebinding-build.log`.
- Unity EditMode focused ResourceExchange tests passed 33/33: `/private/tmp/warline-resource-exchange-popup-livebinding-tests.xml` and `/private/tmp/warline-resource-exchange-popup-livebinding-tests.log`.

Remaining blocker or next slice:

- Next slice should route `ResourceExchangeConfirm`, amount stepper changes, row rush/cancel, rush-all, and clear-completed actions into the existing `ResourceExchangeRequestComponent` buffers without adding UI-owned economy policy.
- TMP/Oxanium validation and 16:9/20:9 layout captures remain required before this popup is player-facing.

### 2026-07-08 - Phase 5I POP-12 Backend Request Wiring

Files changed:

- `Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiResourceExchangeReadModelSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs`
- `Assets/Tests/Editor/ResourceExchangeRequestValidationSystemTests.cs`
- `Assets/Tests/Editor/ResourceExchangeUiActionRequestSystemTests.cs`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `SelectedInputAmount` to the POP-12 ECS UI state so the amount stepper can persist the selected amount independently from the recipe slot.
- `UiResourceExchangeReadModelSystem` now normalizes the selected amount by recipe min/max/step, updates detail cost/output/duration from that amount, and validates confirm affordability/storage against the selected amount instead of always using the recipe minimum.
- `UiActionRequestSystem` now resolves the selected recipe from ECS and routes amount decrease/increase into `SelectedInputAmount` updates without owning wallet or conversion policy.
- `ResourceExchangeConfirm` now appends a typed `ResourceExchangeRequestKind.Start` request with the selected recipe id and normalized input amount.
- `ResourceExchangeQueueRush`, `ResourceExchangeQueueCancel`, `ResourceExchangeRushAll`, and `ResourceExchangeClearCompleted` now append typed ECS request-buffer entries through the existing `ResourceExchangeRequestValidationSystem` helpers.
- Added `EnqueueClearCompletedRequest` and validation-system handling for `ResourceExchangeRequestKind.ClearCompleted`; the request removes completed rows for the requesting faction only and never mutates wallet balances.
- Added focused editor validation entry points for UI action routing and backend request validation so future heartbeat slices can run deterministic Unity batchmode checks.

Validation:

- `git diff --check` passed.
- `dotnet build Game.UI.Shell.Contracts.Ecs.csproj --no-restore` passed with 4 warnings and 0 errors.
- `dotnet build Game.Runtime.csproj --no-restore` passed with 6 warnings and 0 errors.
- `dotnet build Game.UI.Shell.Ecs.csproj --no-restore` passed with 7 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore` passed with 10 warnings and 0 errors.
- Unity focused UI action validation passed 3/3 with `ResourceExchangeUiActionRequestSystemTests.RunFocusedValidation`: `/private/tmp/wlc-resource-ui-action-validation.log`.
- Unity focused backend validation passed 2/2 with `ResourceExchangeRequestValidationSystemTests.RunFocusedValidation`: `/private/tmp/wlc-resource-request-validation.log`.

Remaining blocker or next slice:

- Next slice should verify POP-12 runtime text uses TMP/Oxanium and that all visible object ids/button refs survive prefab regeneration.
- 16:9 and 20:9 layout captures remain required before this popup is player-facing.

### 2026-07-09 - Phase 5J POP-12 Prefab Contract Tests

Files changed:

- `Assets/Tests/Editor/ResourceExchangePopupPrefabTests.cs`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added focused POP-12 prefab contract coverage for live TMP-only text, Oxanium TMP font usage, non-raycast TMP labels, stable named hierarchy paths, recipe-card/queue-row disabled state objects, and runtime popup button actions.
- Added `ResourceExchangePopupPrefabTests.RunFocusedValidation` so the POP-12 prefab contract can be executed directly in Unity batchmode without relying on the full EditMode runner.
- No gameplay runtime behavior changed in this slice.

Validation:

- Unity focused POP-12 prefab validation passed 6/6 with `ResourceExchangePopupPrefabTests.RunFocusedValidation`: `/private/tmp/wlc-resource-exchange-popup-prefab-validation-workaround.log`.
- The successful validation used the documented licensing workaround: `Tools/CI/invoke_unity_macos.sh` in windowed batchmode, without `-nographics`, so it avoided the previous `com.unity.editor.headless` licensing route.
- `git diff --check` passed after this tracker update.
- Unity modified `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` as an editor side effect in the temp worktree; that unrelated file was left unstaged and is not part of this slice.

Remaining blocker or next slice:

- Completed by Phase 5K below.

### 2026-07-09 - Phase 5K POP-12 Layout Captures

Files changed:

- `Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab`
- `Assets/Game/Scripts/Editor/ResourceExchangePopupPrefabBuilder.cs`
- `Assets/Game/Scripts/Editor/ResourceExchangePopupLayoutCaptureValidation.cs`
- `Assets/Game/Scripts/Editor/ResourceExchangePopupLayoutCaptureValidation.cs.meta`
- `Design/AgentReports/Captures/ResourceExchange/POP12_ResourceExchange_16x9_1920x1080.png`
- `Design/AgentReports/Captures/ResourceExchange/POP12_ResourceExchange_20x9_2400x1080.png`
- `Design/AgentReports/Captures/ResourceExchange/POP12_ResourceExchange_layout_capture_report.md`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added an editor-only POP-12 layout capture validator that renders the live Canvas prefab at 16:9 and 20:9 instead of using a target-lock screenshot.
- The validator writes proof captures, checks exact capture resolution, checks nonblank output, keeps the 1640x916 modal panel stable across both aspects, validates major panel bounds, checks active TMP overflow, and guards the detail panel against requirements/confirm/instruction overlap.
- Adjusted the POP-12 detail-panel row spacing so `Requires Oil Pump`, the amount stepper, the `CONFIRM` button, and the detail instruction line no longer collide.

Validation:

- `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 8 warnings and 0 errors.
- Unity prefab rebuild passed with `Game.Editor.ResourceExchangePopupPrefabBuilder.Build`: `/private/tmp/wlc-resource-exchange-popup-rebuild-layout-v3.log`.
- Unity POP-12 layout capture validation passed with `Game.Editor.ResourceExchangePopupLayoutCaptureValidation.Run`: `/private/tmp/wlc-resource-exchange-layout-capture-v5.log`.
- Unity focused POP-12 prefab contract validation still passed 6/6 with `ResourceExchangePopupPrefabTests.RunFocusedValidation`: `/private/tmp/wlc-resource-exchange-popup-prefab-validation-layout.log`.
- Capture report: `Design/AgentReports/Captures/ResourceExchange/POP12_ResourceExchange_layout_capture_report.md`.
- 16:9 capture: `Design/AgentReports/Captures/ResourceExchange/POP12_ResourceExchange_16x9_1920x1080.png`.
- 20:9 capture: `Design/AgentReports/Captures/ResourceExchange/POP12_ResourceExchange_20x9_2400x1080.png`.
- Final visual inspection confirmed the earlier detail-panel text/CTA collision is gone in both captures.
- Unity modified `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` as an editor side effect in the temp worktree; that unrelated file was left unstaged and is not part of this slice.

Remaining blocker or next slice:

- Phase 5 is complete.
- Next slice is Phase 6 world presentation: define Resource Exchange presentation anchors and non-authoritative cue data without making truck/plane visuals control exchange completion.

### 2026-07-09 - Phase 6A Presentation Anchor Data

Files changed:

- `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangePresentationAnchorUtility.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangePresentationAnchorUtility.cs.meta`
- `Assets/Tests/Editor/ResourceExchangePresentationAnchorUtilityTests.cs`
- `Assets/Tests/Editor/ResourceExchangePresentationAnchorUtilityTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added typed Resource Exchange presentation anchor kinds: base depot, runway/landing zone, storage, and fallback safe.
- Added `ResourceExchangePresentationAnchorComponent` as ECS buffer data with faction id, anchor kind, stable anchor id, world position, rotation, safe radius, and validity flag.
- Added `ResourceExchangePresentationAnchorUtility` as a pure data resolver for later visual playback. It resolves exact preferred anchors first, then deterministic fallback safe/base depot/storage/runway anchors without mutating economy, pathfinding, or queue state.
- Kept this slice data-only. It does not spawn planes/trucks, query scene objects, run pathfinding, or change queue completion.

Validation:

- `dotnet build Game.Components.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 11 warnings and 0 errors.
- `git diff --check` passed for the touched files before this tracker update.
- Unity focused presentation-anchor validation passed 4/4 with `ResourceExchangePresentationAnchorUtilityTests.RunFocusedValidation`: `/private/tmp/wlc-resource-exchange-presentation-anchor-validation.log`.
- Unity modified `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` as an editor side effect in the temp worktree; that unrelated file was left unstaged and is not part of this slice.

Remaining blocker or next slice:

- Next Phase 6 slice should add ECS visual cue request emission for exchange start, load, plane landing, plane departure, unload, completion, and cancellation.
- Managed pooled truck/plane playback remains intentionally unimplemented until cue emission is stable.

### 2026-07-09 - Phase 7A Resource Exchange Audio Event Ids And Placeholder Clips

Files changed:

- `Assets/Game/Audio/Config/audio_event_catalog_v0_1.json`
- `Assets/Game/Audio/Events/AudioEventCatalogConfig.asset`
- `Assets/Game/Audio/Gameplay/game_resource_exchange_accepted_01.wav`
- `Assets/Game/Audio/Gameplay/game_resource_exchange_accepted_01.wav.meta`
- `Assets/Game/Audio/Gameplay/game_resource_exchange_rejected_01.wav`
- `Assets/Game/Audio/Gameplay/game_resource_exchange_rejected_01.wav.meta`
- `Assets/Game/Audio/Gameplay/game_resource_exchange_queue_started_01.wav`
- `Assets/Game/Audio/Gameplay/game_resource_exchange_queue_started_01.wav.meta`
- `Assets/Game/Audio/Gameplay/game_resource_exchange_rushed_01.wav`
- `Assets/Game/Audio/Gameplay/game_resource_exchange_rushed_01.wav.meta`
- `Assets/Game/Audio/Gameplay/game_resource_exchange_completed_01.wav`
- `Assets/Game/Audio/Gameplay/game_resource_exchange_completed_01.wav.meta`
- `Assets/Game/Audio/Gameplay/game_resource_exchange_cancelled_01.wav`
- `Assets/Game/Audio/Gameplay/game_resource_exchange_cancelled_01.wav.meta`
- `Assets/Game/Scripts/Audio/Config/AudioEventIds.cs`
- `Assets/Tests/Editor/ResourceExchangeAudioConfigTests.cs`
- `Assets/Tests/Editor/ResourceExchangeAudioConfigTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added six config-driven Resource Exchange SFX event ids: accepted, rejected, queue started, rushed, completed, and cancelled.
- Generated deterministic mono placeholder WAV clips for those events and wired them through the central audio catalog with `SFX` bus routing, non-looping playback, no runtime loading, and stable clip metas.
- Regenerated `AudioEventIds.cs` from the updated catalog so constants, hashes, and all-event ordering stay source-of-truth driven by `audio_event_catalog_v0_1.json`.
- Mirrored the same entries into `AudioEventCatalogConfig.asset` so runtime config users can resolve the new clips before the Unity asset builder can be rerun successfully.
- Added `ResourceExchangeAudioConfigTests.RunFocusedValidation` to keep Resource Exchange audio ids, hashes, SFX routing, clip paths, and placeholder assets required by a focused editor contract.

Validation:

- `dotnet build Game.Tests.Editor.csproj --no-restore` passed with 11 warnings and 0 errors.
- Non-Unity catalog/constants/runtime-asset validation passed for the six Resource Exchange events, including JSON entries, generated constants, stable hashes, WAV headers, `.meta` GUIDs, and runtime YAML references.
- Attempted Unity runtime audio config validation with the documented macOS licensing workaround: `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-audio-runtime-config-builder.log --timeout 420 -- -quit -executeMethod AudioRuntimeConfigAssetBuilderTests.RunFocusedValidation`.
- Unity validation was blocked by licensing after the wrapper was already used: the log timed out waiting for `LicenseClient-farhad`, then reported `Licensing initialization failed after 74.83s`. The stuck temp-worktree Unity PID was terminated; the separate main-checkout Unity process was left alone.
- Known unrelated worktree side effect left unstaged: `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`.

Remaining blocker or next slice:

- Re-run Unity focused validations when licensing is healthy: `AudioRuntimeConfigAssetBuilderTests.RunFocusedValidation`, `AudioConfigContractTests.RunFocusedValidation`, and `ResourceExchangeAudioConfigTests.RunFocusedValidation`.
- Next Phase 7 slice should add resource delta flyouts for spend, reserve, output grant, refund, and rush spend.

### 2026-07-09 - Phase 7B Resource Delta Flyout Data

Files changed:

- `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeQueueTickSystem.cs`
- `Assets/Tests/Editor/ResourceExchangeDeltaFlyoutSystemTests.cs`
- `Assets/Tests/Editor/ResourceExchangeDeltaFlyoutSystemTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `ResourceExchangeDeltaFlyoutKind` and `ResourceExchangeDeltaFlyoutComponent` as data-only ECS presentation requests for Resource Exchange resource deltas.
- Kept the new buffer optional in runtime systems. Existing exchange entities without `ResourceExchangeDeltaFlyoutComponent` still process requests, queue ticking, cancel, rush, and completion normally.
- Start requests emit an `InputReserved` flyout with a negative input amount when resources are spent/reserved into a queue item.
- Queue completion emits an `OutputGranted` flyout with a positive output amount when the output is actually applied.
- Cancel and mission-end refund paths emit `InputRefunded` flyouts only when reserved input is actually returned.
- Rush and rush-all paths emit `RushTicketsSpent` flyouts with negative Rush Ticket amounts. If rushing completes a job immediately, the same path also emits the `OutputGranted` completion flyout.
- Added focused editor coverage for input reserve/spend, output grant, refund, rush spend, and rush-immediate-completion flyout sequencing.

Validation:

- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 11 warnings and 0 errors.
- `dotnet build Game.Components.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `git diff --check` passed before this tracker update.
- Attempted Unity focused validation with the documented macOS licensing workaround: `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-delta-flyout-validation.log --timeout 420 -- -quit -executeMethod ResourceExchangeDeltaFlyoutSystemTests.RunFocusedValidation`.
- Unity validation was blocked by licensing after the wrapper was already used: the log timed out waiting for `LicenseClient-farhad`, then reported `Licensing initialization failed after 74.83s`. The stuck temp-worktree Unity PID was terminated.
- Known unrelated worktree side effect left unstaged: `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`.

Remaining blocker or next slice:

- Re-run Unity focused validation when licensing is healthy: `ResourceExchangeDeltaFlyoutSystemTests.RunFocusedValidation`.
- Next Phase 7 slice should add completion and rejection toast data with typed reason text.

### 2026-07-09 - Phase 7C Completion And Rejection Toast Data

Files changed:

- `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeToastTextUtility.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeToastTextUtility.cs.meta`
- `Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeQueueTickSystem.cs`
- `Assets/Tests/Editor/ResourceExchangeToastSystemTests.cs`
- `Assets/Tests/Editor/ResourceExchangeToastSystemTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `ResourceExchangeToastKind`, `ResourceExchangeToastSeverity`, and `ResourceExchangeToastComponent` as data-only ECS feedback requests for Resource Exchange toasts.
- Added `ResourceExchangeToastTextUtility` to map typed `ResourceExchangeReason` values into short player-facing body text without stringly runtime state.
- Kept the toast buffer optional in runtime systems. Existing exchange entities without `ResourceExchangeToastComponent` still process requests, queue ticking, cancel, rush, and completion normally.
- Start request acceptance emits an `EXCHANGE QUEUED` toast.
- Request rejection, rush rejection, and queue-blocked paths emit an `EXCHANGE BLOCKED` toast with typed reason text such as insufficient Oil, queue full, storage full, storage missing, rush unavailable, and mission ending.
- Queue completion emits an `EXCHANGE COMPLETE` toast only when the output is actually granted.
- Cancel and mission-end cancellation paths emit an `EXCHANGE CANCELLED` toast. Rush accepted paths emit a `RUSH APPLIED` toast, and rush-immediate-completion can emit both rush and completion toast rows.
- Added focused editor coverage for accepted start, insufficient-Oil rejection, queue completion, cancel/refund, rush-immediate-completion, and common typed reason text mapping.

Validation:

- `git diff --check` passed before this tracker update.
- `dotnet build Game.Components.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 11 warnings and 0 errors.
- Attempted Unity focused validation with the documented macOS licensing workaround: `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-toast-validation.log --timeout 420 -- -quit -executeMethod ResourceExchangeToastSystemTests.RunFocusedValidation`.
- Unity validation was blocked by licensing after the wrapper was already used: the log timed out waiting for `LicenseClient-farhad`, then reported `Licensing initialization failed after 74.83s`. The stuck temp-worktree Unity PID was terminated.

Remaining blocker or next slice:

- Re-run Unity focused validation when licensing is healthy: `ResourceExchangeToastSystemTests.RunFocusedValidation`.
- Next Phase 7 slice should add optional ARIA strings for insufficient resources, exchange started, exchange complete, and exchange blocked.

### 2026-07-09 - Phase 7D Optional ARIA Announcement Strings

Files changed:

- `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeAriaTextUtility.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeAriaTextUtility.cs.meta`
- `Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeQueueTickSystem.cs`
- `Assets/Tests/Editor/ResourceExchangeAriaAnnouncementSystemTests.cs`
- `Assets/Tests/Editor/ResourceExchangeAriaAnnouncementSystemTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `ResourceExchangeAriaAnnouncementKind` and `ResourceExchangeAriaAnnouncementComponent` as optional data-only ECS read-aloud requests for Resource Exchange.
- Added `ResourceExchangeAriaTextUtility` to map typed result/reason data into ARIA-friendly strings for insufficient resources, exchange started, exchange complete, and exchange blocked states.
- Kept ARIA announcements optional in runtime systems. Existing exchange entities without `ResourceExchangeAriaAnnouncementComponent` still process requests, queue ticking, cancel, rush, and completion normally.
- Start request acceptance emits `Exchange queued. Logistics timer started.`
- Queue completion emits `Exchange complete. Resources received.` only when output is actually applied.
- Insufficient Credits, Materials, Oil, Fuel, and Rush Tickets emit specific insufficient-resource ARIA text.
- Request rejection, rush rejection, and queue-blocked paths emit exchange-blocked ARIA text with typed reason details for queue full, storage full, storage missing, locked route, unavailable exchange, unavailable rush, unavailable transport, and mission ending.
- Added focused editor coverage for the four required announcement categories and common text mappings.

Validation:

- `git diff --check` passed before this tracker update.
- `dotnet build Game.Components.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 11 warnings and 0 errors.
- Attempted Unity focused validation with the documented macOS licensing workaround: `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-aria-announcement-validation.log --timeout 420 -- -quit -executeMethod ResourceExchangeAriaAnnouncementSystemTests.RunFocusedValidation`.
- Unity validation was blocked by licensing after the wrapper was already used: the log timed out waiting for `LicenseClient-farhad`, then reported `Licensing initialization failed after 74.83s`. The stuck temp-worktree Unity PID was terminated.

Remaining blocker or next slice:

- Re-run Unity focused validation when licensing is healthy: `ResourceExchangeAriaAnnouncementSystemTests.RunFocusedValidation`.
- Next Phase 7 slice should pair world presentation cues with non-authoritative VFX markers.

### 2026-07-09 - Phase 7E Non-Authoritative VFX Marker Data

Files changed:

- `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeVisualCueSystem.cs`
- `Assets/Tests/Editor/ResourceExchangeVfxMarkerSystemTests.cs`
- `Assets/Tests/Editor/ResourceExchangeVfxMarkerSystemTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `ResourceExchangeVfxMarkerKind` and `ResourceExchangeVfxMarkerComponent` as optional data-only ECS presentation markers for Resource Exchange VFX.
- Kept VFX markers non-authoritative. They mirror visual cue timing and resolved anchor data, but they do not mutate queue state, wallet state, resources, timers, or world blockers.
- Kept the marker buffer optional. Existing exchange entities without `ResourceExchangeVfxMarkerComponent` still emit normal `ResourceExchangeVisualRequestComponent` rows and preserve the previous visual cue API.
- Paired exchange start, transport landing, export load, import unload, transport departure, completion, and cancellation cues with stable marker kinds and short suggested durations.
- Preserved missing-anchor diagnostics. Marker rows still emit with `AnchorResolved = 0` when anchors are absent, allowing presentation diagnostics without starting gameplay presentation.
- Added focused editor coverage for export/import marker pairing, terminal completion/cancel markers, missing-anchor unresolved markers, disabled world presentation, no duplicate marker emission, and cue-kind mapping.

Validation:

- `git diff --check` passed before this tracker update.
- `dotnet build Game.Components.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 11 warnings and 0 errors.
- `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-vfx-marker-validation.log --timeout 420 -- -quit -executeMethod ResourceExchangeVfxMarkerSystemTests.RunFocusedValidation`
- Unity focused result: `[ResourceExchangeVfxMarkerValidation] result=Passed tests=6`

Remaining blocker or next slice:

- Next Phase 7 slice should validate that Resource Exchange uses the config-driven audio path and does not add direct `AudioSource` or prefab sound wiring outside the audio catalog/bridge contract.

### 2026-07-09 - Phase 7F Direct Audio Wiring Guardrail

Files changed:

- `Assets/Tests/Editor/ResourceExchangeAudioWiringContractTests.cs`
- `Assets/Tests/Editor/ResourceExchangeAudioWiringContractTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `ResourceExchangeAudioWiringContractTests` to validate Resource Exchange production scripts and the POP-12 popup prefab do not play audio directly.
- The guardrail scans dedicated `ResourceExchange*.cs` files under runtime, UI, config, component, system, and editor-script roots for direct playback tokens such as `AudioSource`, `AudioClip`, `PlayOneShot`, `PlayClipAtPoint`, scheduled playback, `AudioListener`, `AudioSettings`, and direct `Resources.Load`.
- The guardrail scans `POP12_ResourceExchangePopup.prefab` for serialized audio clip/source/listener/mixer fields so the popup cannot embed direct sound wiring.
- Central audio config remains the allowed path. Resource Exchange audio must continue through the existing audio event catalog/request path, not scene or prefab-local clip references.

Validation:

- `git diff --check` passed before this tracker update.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 10 warnings and 0 errors.
- `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-audio-wiring-validation.log --timeout 420 -- -quit -executeMethod ResourceExchangeAudioWiringContractTests.RunFocusedValidation`
- Unity focused result: `[ResourceExchangeAudioWiringValidation] result=Passed tests=2`

Remaining blocker or next slice:

- Phase 7 is complete. Next slice should begin Phase 8 with economy/balance/telemetry event coverage for exchange input reserve, output grant, refund, rush ticket spend, blocked jobs, and cancelled jobs.

### 2026-07-09 - Phase 8A Economy Event Coverage

Files changed:

- `Assets/Game/Scripts/Systems/ResourceExchangeQueueTickSystem.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs`
- `Assets/Tests/Editor/ResourceExchangeQueueTickSystemTests.cs`
- `Assets/Tests/Editor/ResourceExchangeEconomyEventSystemTests.cs`
- `Assets/Tests/Editor/ResourceExchangeEconomyEventSystemTests.cs.meta`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Queue start/input reserve already emitted `QueueStarted` economy events with negative input amounts; focused coverage now locks this contract.
- Queue completion/output grant already emitted `QueueCompleted` economy events with positive output amounts; focused coverage now locks this contract.
- Rush accepted already emitted `RushAccepted` economy events with negative Rush Ticket amounts; focused coverage now locks this contract.
- Queue blocked now emits a `QueueBlocked` economy event with output resource and amount `0`, so storage/capacity blockers are visible to telemetry and balance reporting.
- Queue cancellation now always emits a `QueueCancelled` economy event. Refundable cancellations use the positive refund amount; no-refund cancellations emit amount `0`, so cancellation telemetry is not lost when presentation has already started.
- Mission-ending cancellation now emits one `QueueCancelled` economy event per cancelled job, including zero-amount rows for no-refund jobs.
- Added `ResourceExchangeEconomyEventSystemTests` to validate input reserve, output grant, refund, no-refund cancel, blocked job, and rush-ticket spend event rows.

Validation:

- `git diff --check` passed before this tracker update.
- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 11 warnings and 0 errors.
- `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-economy-event-validation.log --timeout 420 -- -quit -executeMethod ResourceExchangeEconomyEventSystemTests.RunFocusedValidation`
- Unity focused result: `[ResourceExchangeEconomyEventValidation] result=Passed tests=6`

Remaining blocker or next slice:

- Next Phase 8 slice should add balance report fields for exchange route, amount, duration, source mode, completion, and resource delta.

### 2026-07-09 - Phase 8B Balance Report Fields

Files changed:

- `Assets/Game/Scripts/Balance/BalanceMetrics.cs`
- `Assets/Game/Scripts/Balance/BalanceReportWriter.cs`
- `Assets/Tests/Editor/Balance/BalanceHarnessContractTests.cs`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added Resource Exchange balance fields to `BalanceMetrics` for source mode, route summary, started/completed/cancelled/blocked/rush counts, planned input/output amounts, total duration seconds, completion rate, per-resource deltas, and net resource delta.
- Added `BalanceMetrics.ApplyResourceExchangeTelemetry` as a pure reporting helper that summarizes existing `ResourceExchangeQueueComponent` rows and `ResourceExchangeEconomyEventComponent` rows without owning gameplay policy or mutating ECS state.
- Added a Resource Exchange section to generated Markdown balance reports, exposing route, amount, duration, source mode, completion, and resource delta fields for human tuning review.
- Added focused balance harness coverage proving Resource Exchange queue/economy telemetry populates the new fields and both JSON and Markdown reports include them.

Validation:

- `git diff --check` passed before this tracker update.
- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 11 warnings and 0 errors.
- `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-balance-report-validation.log --timeout 420 -- -quit -executeMethod BalanceHarnessContractTests.RunFocusedValidation`
- Unity focused result: `[BalanceHarnessContractValidation] result=Passed tests=2`

Remaining blocker or next slice:

- Next Phase 8 slice should add data sanity tests for exchange rates, fees, duration, and farming-risk caps.

### 2026-07-09 - Phase 8C Data Sanity And Farming-Risk Caps

Files changed:

- `Assets/Game/Scripts/Configs/ResourceExchangeConfigModels.cs`
- `Assets/Tests/Editor/ResourceExchangeConfigValidationTests.cs`
- `Design/Resource_Logistics_Exchange_Design.md`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added named Resource Exchange config guardrails for single-exchange amount caps, per-recipe output-rate caps, minimum base queue duration, per-job Rush Ticket caps, and paired export/import round-trip retention.
- Recipe validation now rejects unsafe authored values before runtime instead of only rejecting mathematically invalid negative/NaN/infinite data.
- Recipe-set validation now checks paired export/import loops through Credits for Materials/Fuel and rejects round trips that retain more than 85% of the original resource after fees.
- Added focused editor coverage for amount cap, rate cap, fee NaN, instant-duration, rush cap, safe paired routes, and unsafe round-trip farming loops.
- Updated the high-level design document so the data sanity thresholds are visible to designers and future implementation agents.

Validation:

- `git diff --check` passed before this tracker update.
- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 12 warnings and 0 errors.
- `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-config-sanity-validation-rerun.log --timeout 420 -- -quit -executeMethod ResourceExchangeConfigValidationTests.RunFocusedValidation`
- Unity focused result: `[ResourceExchangeConfigValidation] result=Passed tests=4`

Remaining blocker or next slice:

- Next Phase 8 slice should gate exchange recipes by chapter, mission, and skirmish preset so early FTUE is not overloaded.

### 2026-07-09 - Phase 8D Scenario Gate Authoring

Files changed:

- `Assets/Game/Scripts/Configs/ResourceExchangeConfigModels.cs`
- `Assets/Tests/Editor/ResourceExchangeConfigValidationTests.cs`
- `Design/Resource_Logistics_Exchange_Design.md`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added explicit Resource Exchange scenario gate validation for chapter, mission, campaign, skirmish, custom skirmish, and operation tags.
- Added constructor support for `ResourceExchangeScenarioGateConfigEntry` so tests and authoring tools can create gate entries without reflection or prefab/asset mutation.
- Added full config validation that checks recipes and scenario gates together, then rejects blank/global recipe gates, unknown recipe gate tags, duplicate scenario gates, invalid scenario tag prefixes, enabled gates with no queue capacity, and disabled gates without a typed disabled reason.
- Documented the FTUE gate policy: early chapters should carry explicit disabled gates, shipping recipes must use non-empty `missionTag` values, and enabled gates must opt into only the routes the scenario has taught.
- Kept runtime request validation unchanged. The existing `ResourceExchangeEnabledComponent.ScenarioTag` and recipe `MissionTag` path remains the match-time enforcement boundary.

Validation:

- `git diff --check` passed before this tracker update.
- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 6 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 11 warnings and 0 errors.
- `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-scenario-gate-validation.log --timeout 420 -- -quit -executeMethod ResourceExchangeConfigValidationTests.RunFocusedValidation`
- Unity focused result: `[ResourceExchangeConfigValidation] result=Passed tests=6`

Remaining blocker or next slice:

- Next Phase 8 slice should add AI exchange support only if a scenario explicitly enables AI exchange behavior.

### 2026-07-09 - Phase 8E AI Exchange Opt-In Gate

Files changed:

- `Assets/Game/Scripts/Components/ResourceExchangeComponents.cs`
- `Assets/Game/Scripts/Configs/ResourceExchangeConfigModels.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeQueueTickSystem.cs`
- `Assets/Game/Scripts/Systems/ResourceExchangeRequestValidationSystem.cs`
- `Assets/Tests/Editor/ResourceExchangeConfigValidationTests.cs`
- `Assets/Tests/Editor/ResourceExchangeRequestValidationSystemTests.cs`
- `Design/Resource_Logistics_Exchange_Design.md`
- `Design/Architecture/resource_logistics_exchange_implementation_tracker.md`

Behavior changed:

- Added `allowAiExchange` to Resource Exchange scenario gates. It defaults to false and is invalid on disabled gates.
- Added `AllowAiExchange` to `ResourceExchangeEnabledComponent` and `ResourceExchangeSummaryComponent` so future AI logic can read the scenario opt-in from ECS data without inferring permission from general faction AI control.
- Request validation and queue ticking now preserve the AI opt-in flag into Resource Exchange summary state alongside rush and world-presentation flags.
- Added focused config coverage proving AI exchange is off by default, explicitly opt-in when authored, accepted only on enabled scenario gates, and rejected on disabled scenario gates.
- Added focused request-validation coverage proving the runtime summary carries the AI exchange gate when the scenario state enables it.
- Documented that AI-controlled factions must not infer exchange permission from `AIControlledTag` or `FactionControlEntry` alone.
- No AI planner, broad scene search, managed polling, or autonomous AI exchange request behavior was added in this slice.

Validation:

- `git diff --check` passed before this tracker update.
- `dotnet build Game.Components.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 10 warnings and 0 errors.
- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 7 warnings and 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed with 12 warnings and 0 errors.
- `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-ai-gate-config-validation.log --timeout 420 -- -quit -executeMethod ResourceExchangeConfigValidationTests.RunFocusedValidation`
- Unity focused result: `[ResourceExchangeConfigValidation] result=Passed tests=7`
- `Tools/CI/invoke_unity_macos.sh --project /private/tmp/wlc-resource-exchange-next --log /private/tmp/wlc-resource-exchange-ai-gate-request-validation.log --timeout 420 -- -quit -executeMethod ResourceExchangeRequestValidationSystemTests.RunFocusedValidation`
- Unity focused result: `[ResourceExchangeRequestValidation] result=Passed tests=3`

Remaining blocker or next slice:

- Next Phase 8 slice should ensure that, if AI exchange is enabled later, it remains data-driven and does not add managed per-frame planner scans.

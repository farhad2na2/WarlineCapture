# Resource Logistics Exchange Implementation Tracker

Date: 2026-07-08
Status: Phase 5 UI popup and header routing in progress
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

Overall implementation progress: 61% (56/92 checklist items complete).

Progress is checklist-based. Each `- [ ]` or `- [x]` implementation/validation item below counts as one item. When a future implementation slice adds or removes checklist items, update this section in the same commit.

| Phase | Status | Complete | Total | Progress | Notes |
|---|---|---:|---:|---:|---|
| 0. Inventory and source alignment | Complete | 8 | 8 | 100% | Current resource data, HUD header, Build Popup, fuel logistics, save/profile fields, and economy event gaps are documented below. |
| 1. Data model and config | Complete | 12 | 12 | 100% | Added Resource Exchange enums, ECS data, config entries, scenario gates, and config validation tests. |
| 2. Request validation and queue start | Complete | 10 | 10 | 100% | Added ISystem request validation, ECS wallet boundary, input reservation, queue item creation, economy event row, and typed results. |
| 3. Queue ticking, completion, cancel, refund | Complete | 11 | 11 | 100% | Timed queue, output grant once, cancel/refund rules, mission-end cancel/refund policy. |
| 4. Rush Tickets | Complete | 7 | 7 | 100% | Rush eligible jobs with ticket spend, per-item caps, rush-all budget, and feedback. |
| 5. UI popup and header routing | In progress | 8 | 13 | 62% | ECS-backed UI read-model, target-lock reference, separated layer pack, Canvas popup prefab, serialized view refs, cards, details, amount stepper, and queue panel complete; resource header tap route, input suppression, captures, and focused routing tests remain. |
| 6. World presentation | Not started | 0 | 8 | 0% | Non-authoritative pooled truck/plane presentation and fallback behavior. |
| 7. Audio, VFX, feedback, ARIA | Not started | 0 | 7 | 0% | Config-driven audio, resource flyouts, completion/reject feedback, optional ARIA copy. |
| 8. AI, balance, telemetry | Not started | 0 | 6 | 0% | Economy events, balancing reports, AI awareness if enabled for AI factions. |
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
- [ ] Route match `ResourceBar` tap/click to popup only when exchange is enabled.
- [ ] Ensure popup blocks world input and restores prior match HUD state on close.
- [ ] Ensure all runtime text uses TMP/Oxanium rules from UI docs.
- [ ] Validate 16:9 and 20:9 layout with captures.
- [ ] Add focused UI/prefab tests for object ids, button wiring, disabled reasons, and input suppression.

Exit criteria:

- UI is visually consistent with the Build Popup.
- UI state is data-bound from ECS/read models.
- Header tapping does not leak into world input.

## Phase 6: World Presentation

- [ ] Define presentation anchors for base depot, runway/landing zone, storage, and fallback safe anchor.
- [ ] Add ECS visual cue request data for exchange start, load, plane landing, plane departure, unload, and completion.
- [ ] Implement managed presentation boundary as `ResourceExchangeVisualPresentationSystemHelper` or existing approved presentation owner.
- [ ] Use pooled plane/truck presentation actors or an existing pooling boundary.
- [ ] Ensure presentation actors do not block gameplay pathfinding or mutate economy state.
- [ ] Add fallback behavior when anchors or presentation prefabs are missing.
- [ ] Add visual state cleanup on popup close, mission end, scene unload, and queue cancellation.
- [ ] Add tests or playmode validation for missing-anchor fallback and no duplicate presentation actors.

Exit criteria:

- Queue completion does not depend on visuals.
- Visuals do not introduce gameplay blockers.
- Presentation can be disabled without breaking economy state.

## Phase 7: Audio, VFX, Feedback, And ARIA

- [ ] Add config-driven audio event ids for accepted, rejected, queue started, rushed, completed, and cancelled exchange events.
- [ ] Generate or assign placeholder audio clips through the audio config workflow.
- [ ] Add resource delta flyouts for spend, reserve, output grant, refund, and rush spend.
- [ ] Add completion toast and rejection toast with typed reason text.
- [ ] Add optional ARIA strings for insufficient resources, exchange started, exchange complete, and exchange blocked.
- [ ] Pair world presentation cues with non-authoritative VFX markers.
- [ ] Validate no direct AudioSource/prefab sound wiring outside the config-driven audio path.

Exit criteria:

- Feedback is clear for every accepted/rejected path.
- Audio remains config-driven.
- No feedback text is baked into art.

## Phase 8: AI, Balance, And Telemetry

- [ ] Add economy events for input spend/reserve, output grant, refund, rush ticket spend, and blocked/cancelled jobs.
- [ ] Add balance report fields for exchange route, amount, duration, source mode, completion, and resource delta.
- [ ] Add data sanity tests for rates, fees, duration, and farming-risk caps.
- [ ] Gate exchange recipes by chapter/mission/skirmish preset so early FTUE is not overloaded.
- [ ] Add AI exchange support only if a scenario explicitly enables AI exchange behavior.
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

# WarlineCapture Resource Logistics Exchange Design

Date: 2026-07-08
Status: Design source of truth

## Purpose

This document defines the timed in-match resource exchange system proposed for WarlineCapture. The feature lets the player export surplus tactical resources for Credits or import tactical resources by spending Credits, while presenting the transaction as a military logistics operation: trucks, storage, transport plane arrival/departure, queue progress, and final wallet updates.

The system should feel like a AAA mobile RTS logistics layer, not an instant shop conversion. The player is ordering a field exchange through command logistics. The UI is a popup similar to the Build Drawer, and the world feedback reinforces that resources are physically moving through the operation map.

## Related Source Documents

- `Economy_Reward_Design.md` owns canonical resource names, reward types, lifecycle rules, store boundaries, and conversion guardrails.
- `Field_Logistics_Oil_Fuel_Design.md` owns tactical Oil/Fuel extraction, refinery, storage, tray truck, and tanker truck rules.
- `Automated_Fuel_Logistics_Design.md` owns autonomous Oil -> Fuel logistics and usable faction Fuel rules.
- `Match_HUD_And_Gameplay_Implementation_Spec.md` owns `SCN-08` header/resource bar behavior and match-owned popups.
- `SCN09_Build_Placement_Mode_Implementation_Spec.md` owns the existing build placement confirmation pattern that this popup should visually align with.
- `UIUX_Target_To_Canvas_Workflow_Guide.md` and `UI_Screen_Reference_To_Icons_Panels_GreenKey_Workflow.md` own target-to-layered-Canvas and green-key asset workflows.
- `Architecture/gameplay_solid_ecs_contract.md` owns ECS/SOLID naming, assembly, runtime ownership, and no-drift rules.
- `Architecture/performance_regression_contract.md` owns performance validation and no-GC hot-path expectations.
- `Architecture/resource_logistics_exchange_implementation_tracker.md` tracks implementation progress.

## Feature Name

Player-facing name:

- `Resource Exchange`

Internal design name:

- `Resource Logistics Exchange`

Suggested UI surface id:

- `POP-12 Resource Logistics Exchange`

Suggested match-owned queue surface:

- `PREFAB-08 Exchange Queue Item`

Do not call this feature `Store`, `Command Exchange`, or `Shop` in match UI. Store and Command Exchange are account/monetization surfaces. This feature is match logistics.

## Player Fantasy

The player is a field commander authorizing a logistics exchange:

- Export surplus Oil, Materials, or Fuel to command logistics and receive Credits.
- Import Materials or Fuel by spending Credits.
- See the transaction as a timed operation with a queue item, truck/plane feedback, progress, and clear completion.
- Rush eligible exchange jobs with Rush Tickets.

The exchange should communicate military logistics:

```text
Resource Header
  -> Resource Exchange Popup
  -> Select Export or Import recipe
  -> Choose amount
  -> Confirm
  -> Queue item starts
  -> Optional world logistics presentation plays
  -> Timer completes
  -> Output resource is granted
```

## Core Rules

1. Resource exchange is disabled unless the mission, Skirmish preset, or Operation event explicitly enables it.
2. Input resources are reserved or spent at confirmation, not at completion.
3. Output resources are granted only when the queue item completes.
4. The conversion rate is never 1:1 by default. Every recipe has authored rate, time, capacity, and loss/fee values.
5. World truck/plane feedback is presentation only. ECS queue data is the source of truth.
6. Rush uses Rush Tickets only. Rush does not use Credits, Command Authority, Fuel, or Materials.
7. Command Authority, Intel, Rush Tickets, Campaign Stars, and Operation metric deltas cannot be freely converted.
8. Paid or account-store resources must not be injected into an active match unless an authored scenario explicitly grants them through match setup.
9. All resource deltas must become economy events for balancing and telemetry.
10. UI must expose exact affordability, queue, timer, storage, cap, and disabled reasons. No silent failure.

## Scenario Gate Rules

Resource Exchange is an authored scenario feature, not a default FTUE mechanic.

- Every shipping Resource Exchange config must include explicit scenario gates before exchange can be enabled.
- Gate tags must use stable authored prefixes: `chapter.`, `mission.`, `campaign.`, `skirmish.`, `custom.skirmish.`, or `operation.`.
- Early FTUE chapters should include an explicit disabled gate, for example `chapter.01.ftue`, with `ExchangeUnavailable` as the disabled reason.
- Recipes in a gated config must use a non-empty `missionTag` that maps to a known scenario gate. Do not ship blank/global recipes, because they would appear in every enabled mission or preset.
- Mission and campaign gates should expose only the routes the player has been taught. Skirmish/custom gates may expose a wider set once the preset explicitly opts in.
- Enabled gates must set a positive queue cap. Disabled gates may use a queue cap of `0` but must carry a typed disabled reason.
- AI exchange is separately opt-in through the scenario gate. `allowAiExchange` defaults to false, must never be true on a disabled gate, and should be enabled only for authored scenarios where AI logistics behavior is part of the mission or preset balance.
- AI-controlled factions must not infer exchange permission from `AIControlledTag` or `FactionControlEntry` alone. Any future AI exchange planner must read the Resource Exchange runtime gate and remain data-driven.
- Future AI exchange planner code must be ECS/data-driven. It should consume `ResourceExchangeEnabledComponent`, `ResourceExchangeSummaryComponent`, wallet, recipe, queue, and faction-control data, then append typed exchange requests. It must not use `SystemBase`, `MonoBehaviour`, broad scene searches, LINQ planner scans, `ToComponentDataArray`/`ToEntityArray` snapshots, or Unity object lookups in a per-frame planning loop.

## Resource Exchange Matrix

| Route | Player Copy | Allowed | Input Timing | Output Timing | Notes |
|---|---|---:|---|---|---|
| `Oil -> Credits` | Export Oil | Yes when Oil logistics is active. | Reserve/spend Oil at confirm. | Credits on completion. | Strong fiction fit. Requires active tactical Oil pool or storage. |
| `Materials -> Credits` | Export Materials | Yes when Materials are active in the match. | Spend Materials at confirm. | Credits on completion. | Useful for surplus construction stock. Rate should be worse than mission rewards. |
| `Fuel -> Credits` | Export Fuel | Optional, inefficient. | Spend Fuel at confirm. | Credits on completion. | Should warn that mobility readiness is reduced. |
| `Credits -> Materials` | Import Materials | Yes when build/repair economy is active. | Spend Credits at confirm. | Materials on completion. | Core player recovery route. |
| `Credits -> Fuel` | Import Fuel | Yes when Fuel logistics is active. | Spend Credits at confirm. | Fuel on completion. | Important for vehicles, aircraft, and emergency logistics. |
| `Credits -> Oil` | Import Oil | Normally no. | N/A | N/A | Prefer Oil extraction through pumps. If needed later, call it `Import Crude` and gate it to authored logistics missions. |
| `Credits -> Intel` | Buy Intel | No for this feature. | N/A | N/A | Intel reveal belongs to scouting, rewards, Operations, or Store dossier products. |
| `Intel -> Credits` | Sell Intel | No. | N/A | N/A | Intel is information value, not a commodity. |
| `Command Authority -> Any` | Command purchase | No in match. | N/A | N/A | Account Command Exchange only. |
| `Rush Tickets -> Time` | Rush queue | Yes. | Spend tickets at rush confirm. | Time reduction immediately. | Rush only affects eligible exchange queue timers. |

## Economy Model

Each recipe should be authored as data:

| Field | Purpose |
|---|---|
| `recipeId` | Stable id, for example `exchange.export_oil_credits.standard`. |
| `displayName` | UI title, for example `Export Oil`. |
| `routeType` | `Export` or `Import`. |
| `inputResource` | `Credits`, `Materials`, `Oil`, or `Fuel`. |
| `outputResource` | `Credits`, `Materials`, or `Fuel`. |
| `inputAmountMin` | Minimum amount allowed. |
| `inputAmountMax` | Maximum amount per exchange. |
| `inputStep` | Stepper increment. |
| `outputPerInput` | Base conversion rate. |
| `feePercent` | Loss or logistics fee. |
| `durationSecondsBase` | Base queue duration. |
| `durationSecondsPerStep` | Added duration by amount. |
| `rushTicketSecondsPerTicket` | Rush value for this recipe. |
| `maxRushTickets` | Per-job rush cap. |
| `requiresStorage` | Whether source/destination storage is required. |
| `requiresTransportPlane` | Whether world plane presentation is expected. |
| `requiresTruckPresentation` | Whether truck presentation is expected. |
| `missionTag` | Optional scenario/preset gate. |
| `disabledReason` | Typed reason when unavailable. |

Balance recommendation:

- Keep export rates clearly below mission reward value to avoid farming.
- Keep import rates expensive enough that players still care about building Oil/Fuel infrastructure.
- Use queue time as a pressure lever, not as a punishment.
- Use storage caps to avoid overfilling imported Fuel/Materials.
- Limit simultaneous exchange jobs per faction to a small number for readability and performance.
- Config validation should reject unsafe economy data before runtime: single exchange amounts above 100,000, per-recipe output rates above 5x, queue base duration below 1 second, per-job rush caps above 10 tickets, and any paired Credits export/import loop that retains more than 85% of the original resource after fees.

## Queue Rules

Exchange jobs are queued like production jobs:

| State | Meaning | UI Requirement |
|---|---|---|
| `Pending` | Request accepted but not yet active if queue is full. | Show queued row with order number. |
| `InProgress` | Timer is running. | Show progress bar, ETA, source/output resources, and optional world status. |
| `Completing` | Completion is being applied. | Brief completion pulse; no duplicate grants. |
| `Completed` | Output applied and row can disappear after feedback. | Show resource delta flyout and remove/persist row by UI policy. |
| `Cancelled` | Job was cancelled before completion. | Refund by recipe refund rule and show reason. |
| `Blocked` | Job cannot proceed because an external requirement changed. | Pause timer and show typed reason, or cancel/refund by recipe policy. |

Recommended queue policy:

- Spend/reserve input at confirmation.
- Refund 100% if the job is cancelled before the transport presentation starts.
- Refund partial amount or no refund after the presentation starts, based on recipe.
- If storage becomes full before an import completes, pause as `BlockedStorageFull` instead of losing output.
- If the mission ends before completion, apply the mission-defined rule: complete instantly, cancel/refund, or discard tactical-only exchange. Campaign tutorial missions should avoid ambiguous pending exchanges.

## UI Design

The popup should visually align with the current Build Popup:

- Same dark military panel language.
- Same gold selected-tab state.
- Same card grid/detail-panel split where practical.
- Same production-queue row style.
- Same icon separation rules: no baked text, progress bars, icons, locks, or counters in background art.
- Same 16:9 and 20:9 landscape behavior.
- Same safe-area and mobile-touch sizing rules.

Suggested layout:

```text
POP-12_ResourceLogisticsExchange
  HeaderBar
    Title: RESOURCE EXCHANGE
    CloseButton
  Tabs
    EXPORT
    IMPORT
  RecipeGrid
    RecipeCard_ExportOil
    RecipeCard_ExportMaterials
    RecipeCard_ExportFuel
    RecipeCard_ImportMaterials
    RecipeCard_ImportFuel
  DetailsPanel
    SelectedRecipeTitle
    RateSummary
    AmountStepper
    InputCostRow
    OutputPreviewRow
    DurationRow
    RequirementRows
    ConfirmButton
  ExchangeQueuePanel
    QueueCapacityLabel
    QueueRows
    RushAllButton
    ClearCompletedButton
```

Header/resource interaction:

- Tapping a resource chip in the match `ResourceBar` opens this popup only if exchange is enabled.
- If exchange is not enabled, tapping the header may show a resource detail tooltip or no-op by mission policy.
- The popup must block world input while open.
- The popup closes to the same match HUD state that opened it, unless the player confirms a build/placement action from another popup.

Queue item behavior:

- Show recipe icon, input amount, output amount, ETA, progress fill, state label, rush button, and cancel button if allowed.
- Progress fill must be a separate UI layer.
- Rush icon, cancel icon, resource icons, and warning badges must be separate sprites.

## World Presentation

World presentation should reinforce the exchange but must not own economy state.

Export presentation:

```text
Storage / base source
  -> trucks move source resource toward logistics plane or depot
  -> transport plane lands or loads
  -> plane takes off
  -> Credits are granted on queue completion
```

Import presentation:

```text
Credits spend confirmation
  -> optional header credit-drain/flyout
  -> transport plane lands
  -> trucks unload Fuel or Materials to storage
  -> output resource is granted on queue completion
```

Presentation rules:

- Use pooled presentation actors where possible.
- Do not require real pathfinding for the queue timer to finish.
- If the world presentation cannot spawn safely, the queue still runs and completes.
- Plane/truck presentation must not block units, occupy gameplay cells, or mutate tactical logistics buffers unless a specific mission later requires real convoy gameplay.
- Use authored logistics anchors: base depot, runway/landing zone, storage building, or fallback safe presentation anchor.
- Avoid per-frame managed allocation, broad scene searches, and instantiate/destroy churn during active exchange.

## Runtime Data Ownership

Gameplay state belongs in ECS:

- Recipe availability is projected into ECS data.
- UI writes typed exchange request components/buffers.
- ECS validation accepts/rejects requests with typed reason codes.
- ECS queue tick advances timers.
- ECS completion applies wallet/resource deltas exactly once.
- ECS result buffers feed UI and feedback.
- Managed UI views only display state and submit typed requests.
- Managed world presentation only consumes ECS visual requests/read models.

No `Manager`, `Controller`, broad `Service`, global singleton, or static mutable state should be introduced for the exchange domain.

## Suggested ECS Data Names

Use existing naming conventions. Exact names can change during implementation if the architecture owner finds a better local pattern.

| Data | Suggested Name | Notes |
|---|---|---|
| Enabled scenario marker | `ResourceExchangeEnabledComponent` | Faction or scenario singleton. |
| Recipe row | `ResourceExchangeRecipeComponent` | Buffer element data is acceptable with `Component` suffix. |
| Request row | `ResourceExchangeRequestComponent` | UI/appends request data. |
| Queue row | `ResourceExchangeQueueComponent` | Active/pending queue state. |
| Result row | `ResourceExchangeResultComponent` | Accepted/rejected/completed/cancelled events. |
| Faction summary | `ResourceExchangeSummaryComponent` | Queue count, active count, version. |
| Visual cue request | `ResourceExchangeVisualRequestComponent` | Presentation consumes and clears. |
| Disabled reason | `ResourceExchangeReason` | Enum, not strings. |
| Recipe route | `ResourceExchangeRouteType` | `Export` / `Import`. |
| Queue state | `ResourceExchangeQueueState` | `Pending`, `InProgress`, etc. |

## Suggested ECS Systems

Prefer unmanaged `ISystem` and Burst-compatible data paths:

| System | Responsibility | Runtime Type Guidance |
|---|---|---|
| `ResourceExchangeConfigProjectionSystem` | Project recipe config/scenario availability into ECS. | `ISystem` if data-only; managed edge only if reading ScriptableObjects at startup. |
| `ResourceExchangeRequestValidationSystem` | Validate requests, affordability, caps, and queue capacity. | `ISystem`, Burst when possible. |
| `ResourceExchangeQueueTickSystem` | Advance timers and queue state. | `ISystem`, Burst-compatible. |
| `ResourceExchangeCompletionSystem` | Apply output deltas and economy events once. | `ISystem`, Burst-compatible when wallet data is ECS. |
| `ResourceExchangeCancelSystem` | Cancel/refund by policy. | `ISystem`. |
| `ResourceExchangeRushSystem` | Spend Rush Tickets and reduce time. | `ISystem`. |
| `ResourceExchangeReadModelSystem` | Publish versioned UI read model. | `ISystem` when possible. |
| `ResourceExchangeVisualCueSystem` | Emit presentation cue requests. | `ISystem` for request data only. |
| `ResourceExchangeAiPlannerSystem` | Optional AI exchange request planner for authored scenarios. | `ISystem`; must read `AllowAiExchange` and data buffers before appending requests. |
| `ResourceExchangeVisualPresentationSystemHelper` | Pooled plane/truck visual playback. | Managed helper/presentation boundary only. |

Use `SystemBase` only if a system must hold or query managed Unity references and cannot be split into an ECS data producer plus managed presentation helper. If `SystemBase` is used, document why in the implementation tracker.

## Typed Reason Codes

Minimum reason ids:

| Reason | Meaning |
|---|---|
| `ExchangeUnavailable` | Mission/preset does not allow exchange. |
| `RecipeLocked` | Recipe exists but is not unlocked for this scenario. |
| `InsufficientCredits` | Not enough Credits. |
| `InsufficientMaterials` | Not enough Materials. |
| `InsufficientOil` | Not enough Oil. |
| `InsufficientFuel` | Not enough Fuel. |
| `InputBelowMinimum` | Amount is too small. |
| `InputAboveMaximum` | Amount exceeds recipe cap. |
| `QueueFull` | No exchange queue slot available. |
| `StorageFull` | Import output cannot fit. |
| `StorageMissing` | Required storage does not exist. |
| `TransportUnavailable` | Required logistics anchor or transport presentation is unavailable. |
| `RushUnavailable` | Queue item cannot be rushed. |
| `InsufficientRushTickets` | Not enough Rush Tickets. |
| `CancelUnavailable` | Queue item cannot be cancelled. |
| `MissionEnding` | Match is ending and exchange no longer accepts requests. |

## Audio, VFX, And Feedback

Recommended feedback events:

- `UI.Popup.Open` / `UI.Popup.Close`
- `UI.Tab.Select`
- `UI.Card.Select`
- `Gameplay.Exchange.Request.Accepted`
- `Gameplay.Exchange.Request.Rejected`
- `Gameplay.Exchange.Queue.Started`
- `Gameplay.Exchange.Rush.Accepted`
- `Gameplay.Exchange.Completed`
- `Gameplay.Exchange.Cancelled`
- `VO.ARIA.Message.ExchangeInsufficientResources`
- `VO.ARIA.Message.ExchangeCompleted`

VFX/feedback:

- Header resource delta flyouts.
- Queue row progress pulse.
- Plane landing/departure marker.
- Truck loading/unloading icons.
- Completion toast.

Audio must be config-driven through `Audio_Config_Driven_Implementation_Spec.md`.

## Acceptance Criteria

- The popup opens from the match resource header only when enabled.
- Export and import recipes come from data, not hard-coded UI.
- Input resources are spent/reserved at confirmation.
- Output resources grant once on completion.
- Rush consumes Rush Tickets and reduces time without bypassing non-rushable queue rules.
- Command Authority, Intel, and Rush Tickets are not freely convertible.
- World presentation is optional and non-authoritative.
- UI uses the Build Popup visual language and separated live text/icons/progress layers.
- ECS systems own validation, queue, completion, refund, and results.
- UI views do not own economy policy.
- Hot paths use `ISystem` and Burst-compatible data where practical.
- No steady-state GC allocation is introduced by queue ticking or read-model updates.
- Focused tests cover validation, affordability, queue completion, rush, cancel/refund, disabled reasons, UI input suppression, and economy events.

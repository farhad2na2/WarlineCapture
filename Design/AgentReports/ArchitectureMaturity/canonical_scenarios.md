# AM-003 Canonical Scenario Catalog

This document is a deterministic projection of `Design/AgentReports/ArchitectureMaturity/canonical_scenarios.json`. The JSON is authoritative.

## Artifact Identity

| Field | Value |
|---|---|
| Schema version | `1` |
| Artifact | `AM-003` / `architecture-maturity-canonical-scenario-catalog` |
| Branch | `codex/am-003-canonical-scenarios` |
| Commit | `7dd5b81f7471f4bc6c209e80937ec5cad6212973` |
| Tree | `9596b8d25dd9f911c17697666e9121b8dd63564c` |
| Git object format | `sha1` |
| Production behavior changed | `false` |
| Shared tracker changed | `false` |
| Write allowlist | `Design/AgentReports/ArchitectureMaturity/canonical_scenarios.json`, `Design/AgentReports/ArchitectureMaturity/canonical_scenarios.md` |

## Determinism

| Field | Contract |
|---|---|
| Encoding | UTF-8 |
| Line endings | LF |
| Canonicalization | jq walk sorts every string-array set, then jq -S emits default two-space indentation followed by one LF |
| Timestamp policy | No generated or reviewed timestamp is stored. |
| Markdown projection | deterministic renderer version 1; scenario and coverage rows preserve canonical JSON order |
| Unchanged input | Byte-identical JSON and Markdown |
| Coverage order | coverageId code-point ascending |
| Scenario order | id code-point ascending |
| Ordered steps | order integer ascending |
| Path/string sets | UTF-8 code-point ascending with duplicates rejected |

## Execution Policy

| Field | Contract |
|---|---|
| attemptsPerRequiredRunner | 1 |
| budgetAuthority | AM-004 |
| budgetRule | All AM-004 placeholders remain null until AM-004 freezes accepted scenario-specific budgets. |
| commitBinding | Runner checkout commit and tree must exactly equal baseline.commit and baseline.tree. |
| requiredRunnerOutcome | passed |
| retryPolicy | prohibited |
| skipPolicy | A skipped, ignored, missing, unsupported, not-run, or no-runner result for any required runner is failure. |
| unexpectedLogPolicy | Unhandled exception, assertion failure, fatal marker, timeout, or required diagnostic absence is failure. |

Retries are prohibited. Every required runner gets one attempt. A skipped, ignored, missing, unsupported, not-run, or no-runner outcome is a failure.

## Global Exclusions

| ID | State | Reason |
|---|---|---|
| `EXC-ANDROID-RELEASE` | `deferred` | Android release-device execution, thermal acceptance, and sustained release execution remain deferred to the inactive Release Certification Lane. |
| `EXC-FIRST-LAUNCH` | `excluded-separately-owned` | FirstLaunch is pre-completed for returning-user scenarios and remains separately owned; this catalog does not exercise or modify it. |
| `EXC-OPERATION-MAP-RND` | `excluded` | Operation-map research and development is outside AM-003. |

## Coverage Matrix

| Coverage ID | State | Executable | Scenario IDs | Evidence / reason |
|---|---|---|---|---|
| `CAT-AIRCRAFT` | `implemented` | `true` | `AM003-SCN-005-AIRCRAFT` |  |
| `CAT-CONSTRUCTION` | `implemented` | `true` | `AM003-SCN-003-CONSTRUCTION` |  |
| `CAT-IDLE-MATCH` | `implemented` | `true` | `AM003-SCN-001-IDLE-MATCH` |  |
| `CAT-LONG-SOAK` | `implemented` | `true` | `AM003-SCN-010-LONG-SOAK` |  |
| `CAT-MAJOR-MATCH-UI` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |
| `CAT-MAXIMUM-COMBAT` | `implemented` | `true` | `AM003-SCN-002-MAXIMUM-COMBAT` |  |
| `CAT-PROJECTILES` | `implemented` | `true` | `AM003-SCN-006-PROJECTILES` |  |
| `CAT-RETURNING-USER-MATCH-TO-MENU` | `implemented` | `true` | `AM003-SCN-009-RETURNING-MATCH-TO-MENU` |  |
| `CAT-RETURNING-USER-MENU-TO-MATCH` | `implemented` | `true` | `AM003-SCN-008-RETURNING-MENU-TO-MATCH` |  |
| `CAT-TRANSPORT` | `implemented` | `true` | `AM003-SCN-004-TRANSPORT` |  |
| `GAP-RESULT-ROUTE` | `unimplemented-coverage-gap` | `false` | None | Evidence: `Assets/Game/Scripts/UI/Contracts/UIRoute.cs`, `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs` |
| `SURFACE-ARIA-ASSISTANT` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |
| `SURFACE-BUILD-DRAWER` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |
| `SURFACE-CURRENT-ORDER-FEEDBACK` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |
| `SURFACE-FULL-MAP` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |
| `SURFACE-MATCH-SETTINGS` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |
| `SURFACE-MINIMAP` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |
| `SURFACE-OBJECTIVES` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |
| `SURFACE-PASSENGER-DRAWER` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |
| `SURFACE-PLACEMENT-CONFIRMATION` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |
| `SURFACE-RESOURCE-EXCHANGE` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |
| `SURFACE-SELECTION-COMMAND-RAIL` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |
| `SURFACE-SQUAD-TRAY` | `implemented` | `true` | `AM003-SCN-007-MAJOR-MATCH-UI` |  |

The Result route is an explicit non-executable gap: `UIRoute` has no Result member, and `TryReadMissionResult` returns false unconditionally.

## Caveats

- `CAVEAT-CONSTRUCTION-BURST`: The production fixtures prove one placement and one production queue path; no existing fixture defines a repeated construction-burst population, so burst scale remains a visible follow-up gap.
- `CAVEAT-HISTORICAL-SOAK`: Historical development or release soak output is diagnostic context only and is never acceptance evidence for this baseline.
- `CAVEAT-IDLE-FLOOR`: MatchPerformanceFixtureSeed creates a synthetic historical entity floor; it is not representative content distribution or proof of a naturally idle mission.
- `CAVEAT-LAB-AIRCRAFT`: Scenario Lab aircraft coverage exercises isolated air-defense definitions and visual playback, not full Match aircraft scheduling, formation scale, airport logistics, or release rendering performance.
- `CAVEAT-MAX-COMBAT-COUNT`: MatchGcAllocationCallstackCapture caps arming at 64 but currently succeeds with any positive armed count; this catalog raises the canonical acceptance signal to exactly 64 armed attackers and fails closed below that count.
- `CAVEAT-TRANSPORT-RUNWAY`: Transport fixtures cover boarding, disembark, rope, parachute, and cargo-drop states; they do not prove airport runway reservation, taxi, takeoff, landing, or full Match route contention.

## Scenario Index

| ID | Category | Purpose | Warmup | Measurement | Core state |
|---|---|---|---|---|---|
| `AM003-SCN-001-IDLE-MATCH` | `idle-match` | Establish the deterministic steady-state Match floor without player-driven activity. | 180 frames | 300 frames | `executable-procedure-defined` |
| `AM003-SCN-002-MAXIMUM-COMBAT` | `maximum-combat` | Exercise the largest combat workload defined by the existing battle capture while closing its positive-count weakness. | 180 frames | 300 frames | `executable-fail-closed-with-exact-count` |
| `AM003-SCN-003-CONSTRUCTION` | `construction` | Measure one complete production placement-to-queue flow through the existing production boundaries. | 60 frames | 300 frames | `executable-single-cycle-burst-gap-declared` |
| `AM003-SCN-004-TRANSPORT` | `transport` | Exercise deterministic boarding, disembark, rope, parachute, cargo, rejection, and cleanup behavior with the existing batch validator. | 16 scenario-executions | 64 scenario-executions | `executable-existing-logic-visual-and-performance-fixtures` |
| `AM003-SCN-005-AIRCRAFT` | `aircraft` | Execute the canonical isolated aircraft and air-defense workload while making Scenario Lab limitations explicit. | 11 scenario-executions | 11 scenario-executions | `executable-scenario-lab-with-limitations` |
| `AM003-SCN-006-PROJECTILES` | `projectiles` | Exercise production ground-rocket and air-interceptor launch-to-impact lifecycles. | 1 scenario-executions | 2 scenario-executions | `executable-existing-playmode-and-scenario-lab-fixtures` |
| `AM003-SCN-007-MAJOR-MATCH-UI` | `major-match-ui` | Exercise every major Match HUD surface and popup through production bindings in one fixed interaction sequence. | 60 frames | 300 frames | `executable-production-ui-sequence` |
| `AM003-SCN-008-RETURNING-MENU-TO-MATCH` | `returning-user-menu-to-match` | Measure returning-user Menu-to-Match lifecycle, UI binding, World preservation, and audio transition. | 1 round-trips | 5 transitions | `executable-five-sample-returning-user-transition` |
| `AM003-SCN-009-RETURNING-MATCH-TO-MENU` | `returning-user-match-to-menu` | Measure returning-user Match-to-Menu unload, runtime cleanup, World preservation, and audio restoration. | 1 round-trips | 5 transitions | `executable-five-sample-returning-user-transition` |
| `AM003-SCN-010-LONG-SOAK` | `long-soak` | Define a long core-lane steady-state Match soak without claiming deferred Android sustained acceptance. | 60 seconds | 1800 seconds | `executable-1800-second-editor-soak` |

## AM003-SCN-001-IDLE-MATCH

Establish the deterministic steady-state Match floor without player-driven activity.

| Field | Value |
|---|---|
| Category | `idle-match` |
| Fixed seed | `3001` (uint32; AM-003 scenario ordinal) |
| Warmup | `180` frames |
| Measurement | `300` frames |
| Core lane | `executable-procedure-defined` |
| Android release execution | `deferred` |
| Thermal execution | `deferred` |
| Sustained release execution | `deferred` |

### Surfaces

- `Match HUD steady state`
- `Match world`

### Prerequisites

- Exact baseline commit and tree are checked out with a clean tracked worktree.
- Menu scene production shell can enter Match and create the default ECS World.
- Profiler categories required by the existing capture fixture are available.
- Returning-user profile state is FirstLaunch completed.

### Existing Fixtures

- `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs`
- `Assets/Game/Scripts/Editor/MatchPerformanceFixtureSeed.cs`
- `Assets/Tests/Editor/MatchGcAllocationCallstackCaptureTests.cs`
- `Assets/Tests/Editor/MatchPerformanceFixtureSeedTests.cs`
- `Design/Architecture/performance_regression_contract.md`

### Setup

1. Reset shell route history and ensure Match is unloaded before loading Menu.
2. Apply fixed seed 3001 before any scenario-owned random choice.
3. Enter Match and wait for GridConfig, shell root, Match HUD, and runtime fixture boundaries.

### Actions

1. Enter Match through the production Menu-to-Match route.
2. Call MatchPerformanceFixtureSeed.Ensure once and assert a second call adds no entities.
3. Hold camera, selection, commands, popups, construction, transport, aircraft, and projectiles inactive for the full measurement window.

### Stabilization

- Complete tracked ECS jobs before warmup.
- Exclude scene load, import, and first-install frames before warmup begins.
- Require stable fixture counts for 30 consecutive frames.

### Required States

| Phase | World state | UI state |
|---|---|---|
| Before | Default ECS World created; Match scene unloaded | MainMenu ready; No popup visible |
| Measurement | Fixture floor stable; Match scene loaded; No active scenario-owned orders | Match HUD ready; No popup visible; No selection or sticky command mode |
| After | Default ECS World preserved; Match scene unloaded | MainMenu active; No Match HUD content installed |

### Acceptance Signals

- Match route is active and transition is not running.
- MatchPerformanceFixtureSeed final counts are at least 733 source entities, 628 buildings, 105 render visual states, and 59 culled units.
- No gameplay command, construction request, transport request, aircraft scenario, or projectile scenario is active during measurement.
- Required frame, GC, system, entity, and UI metrics are present; no required runner is skipped.

### Metrics

- `entityCounts`
- `frameTimeAverageMs`
- `frameTimeMaxMs`
- `frameTimeP95Ms`
- `frameTimeP99Ms`
- `gcAllocationPerFrameP95Bytes`
- `gcAllocationRecurringSites`
- `gcAllocationTotalBytes`
- `memoryGrowthBytes`
- `namedHotSystemP95P99MaxMs`
- `uiObjectCounts`

### AM-004 Budget Placeholders

| Budget ID | Metric | Threshold | Unit | Status |
|---|---|---|---|---|
| `AM004.AM003-SCN-001-IDLE-MATCH.frame-time` | `frameTimeMs` | `null` | `milliseconds` | `pending-am-004` |
| `AM004.AM003-SCN-001-IDLE-MATCH.gc-allocation` | `gcAllocationAfterWarmup` | `null` | `bytes` | `pending-am-004` |
| `AM004.AM003-SCN-001-IDLE-MATCH.memory-growth` | `memoryGrowth` | `null` | `bytes` | `pending-am-004` |

### Failure Diagnostics

- Capture frame-time percentiles, GC call stacks, top system samples, entity counts, UI object counts, and fatal/error markers.
- Capture route, transition phase, frame index, fixture initial/final counts, and second-Ensure deltas.
- Fail immediately on count deficit, mutation during measurement, missing metric, timeout, skipped runner, or retry request.

### Teardown And Reset

- Clear scenario-owned seed, metrics, entities, and temporary report state.
- Return to MainMenu through the production route.
- Stop profiler capture and restore profiler/logging state.
- Unload Match and assert one preserved lifecycle root with no Match runtime UI binding.

### Exclusions

- Android release, thermal, and sustained release acceptance
- Natural mission-content representativeness
- Operation-map research and development

## AM003-SCN-002-MAXIMUM-COMBAT

Exercise the largest combat workload defined by the existing battle capture while closing its positive-count weakness.

| Field | Value |
|---|---|
| Category | `maximum-combat` |
| Fixed seed | `3002` (uint32; AM-003 scenario ordinal) |
| Warmup | `180` frames |
| Measurement | `300` frames |
| Core lane | `executable-fail-closed-with-exact-count` |
| Android release execution | `deferred` |
| Thermal execution | `deferred` |
| Sustained release execution | `deferred` |

### Surfaces

- `Combat simulation`
- `Match HUD`
- `Projectile and impact presentation`

### Prerequisites

- Exact baseline commit and tree are checked out with a clean tracked worktree.
- Ground and air combat VFX references needed by the battle prewarm path resolve.
- Match contains at least 64 eligible attack-capable units after fixture setup.
- Returning-user profile state is FirstLaunch completed.

### Existing Fixtures

- `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs`
- `Assets/Game/Scripts/Editor/MatchPerformanceFixtureSeed.cs`
- `Assets/Tests/Editor/MatchGcAllocationCallstackCaptureTests.cs`
- `Assets/Tests/Editor/MatchPerformanceFixtureSeedTests.cs`
- `Design/Architecture/performance_regression_contract.md`

### Setup

1. Reset route and enter Match through the production shell.
2. Apply fixed seed 3002 and ensure the Match historical fixture floor.
3. Prewarm every collected unit-attack, ground-missile, and air-missile VFX prefab with the existing battle prewarm path.

### Actions

1. Run the battle-state preparation path and record candidates and armed count.
2. Assert armed count equals 64 before warmup; do not downgrade to the current positive-count success condition.
3. Maintain all 64 attackers in commanded combat against scenario-owned high-health targets through measurement.

### Stabilization

- Complete tracked ECS jobs before battle preparation.
- Exclude scene load, import, fixture seeding, target creation, and VFX prewarm frames from measurement.
- Require exactly 64 armed attackers and stable target/projectile counters before warmup.

### Required States

| Phase | World state | UI state |
|---|---|---|
| Before | Fixture floor stable; Match runtime ready | MainMenu ready |
| Measurement | Exactly 64 armed attackers active; Recurring combat and presentation active; Scenario-owned targets alive | Combat feedback may update; Match HUD ready; No modal popup obscures combat |
| After | Match scene unloaded; No scenario-owned targets or engage orders | MainMenu active; No Match HUD content installed |

### Acceptance Signals

- Battle VFX pools are prewarmed before warmup.
- Each armed attacker has a live high-health target in range and enters recurring attack activity.
- Exactly 64 attack-capable non-air-missile-launcher attackers are armed; fewer than 64 is failure.
- Required frame, GC, system, combat-count, projectile-count, presentation-count, and UI metrics are present; no required runner is skipped.

### Metrics

- `armedAttackerCount`
- `combatTargetCount`
- `entityCounts`
- `frameTimeAverageMs`
- `frameTimeMaxMs`
- `frameTimeP95Ms`
- `frameTimeP99Ms`
- `gcAllocationPerFrameP95Bytes`
- `gcAllocationRecurringSites`
- `gcAllocationTotalBytes`
- `memoryGrowthBytes`
- `namedCombatSystemP95P99MaxMs`
- `presentationObjectCounts`
- `projectileCounts`
- `uiObjectCounts`

### AM-004 Budget Placeholders

| Budget ID | Metric | Threshold | Unit | Status |
|---|---|---|---|---|
| `AM004.AM003-SCN-002-MAXIMUM-COMBAT.frame-time` | `frameTimeMs` | `null` | `milliseconds` | `pending-am-004` |
| `AM004.AM003-SCN-002-MAXIMUM-COMBAT.gc-allocation` | `gcAllocationAfterWarmup` | `null` | `bytes` | `pending-am-004` |
| `AM004.AM003-SCN-002-MAXIMUM-COMBAT.memory-growth` | `memoryGrowth` | `null` | `bytes` | `pending-am-004` |

### Failure Diagnostics

- Capture candidate count, armed count, rejected candidate component state, target count, target health, attack cooldown/trace state, projectile count, and VFX pool count.
- Capture frame-time percentiles, GC call stacks, top system samples, entity/presentation/UI counts, and fatal/error markers.
- Fail immediately on armed count below 64, combat decay, missing metric, timeout, skipped runner, or retry request.

### Teardown And Reset

- Clear seed, metrics, and VFX scenario state.
- Remove every scenario-owned target and combat order.
- Return to MainMenu, unload Match, and assert lifecycle/UI cleanup.
- Stop capture and restore profiler/logging state.

### Exclusions

- Air-missile launcher coverage, which belongs to the aircraft/projectile scenarios
- Android release, thermal, and sustained release acceptance
- Operation-map research and development

## AM003-SCN-003-CONSTRUCTION

Measure one complete production placement-to-queue flow through the existing production boundaries.

| Field | Value |
|---|---|
| Category | `construction` |
| Fixed seed | `3003` (uint32; AM-003 scenario ordinal) |
| Warmup | `60` frames |
| Measurement | `300` frames |
| Core lane | `executable-single-cycle-burst-gap-declared` |
| Android release execution | `deferred` |
| Thermal execution | `deferred` |
| Sustained release execution | `deferred` |

### Surfaces

- `Build Drawer`
- `Construction simulation`
- `Current-order and feedback`
- `Placement confirmation`
- `Selection and command rail`

### Prerequisites

- Exact baseline commit and tree are checked out with a clean tracked worktree.
- Match building-placement config and deterministic buildable catalog entry resolve.
- Player resources cover the fixture costs and the selected cell is valid.
- Returning-user profile state is FirstLaunch completed.

### Existing Fixtures

- `Assets/Tests/PlayMode/Aph806BuildingPlacementProductionPlayModeTests.cs`
- `Assets/Tests/PlayMode/BuildingPlacementProductionPlayModeTests.cs`
- `Design/Architecture/performance_regression_contract.md`

### Setup

1. Reset route, enter Match, and apply fixed seed 3003.
2. Select deterministic valid placement cell and building/unit catalog entries.
3. Snapshot resources, building count, pending production count, and UI state before warmup.

### Actions

1. Open Build Drawer, select one deterministic building entry, and start placement.
2. Move preview to the deterministic valid cell and confirm placement.
3. Select the completed producer and queue one deterministic unit production.
4. Hold the completed building and queued production state through the remaining measurement frames.

### Stabilization

- Exclude initial catalog binding and asset load frames from measurement.
- Require no pending placement or production command before warmup.
- Wait for Match loading gate, build catalog, resource projection, and placement query to stabilize.

### Required States

| Phase | World state | UI state |
|---|---|---|
| Before | No pending building placement; No pending production | Build Drawer closed; Match HUD ready; No placement active |
| Measurement | One building committed; One production queued | Build Drawer opens then closes; Current-order/feedback reflects accepted results; Placement confirmation visible only while placement is active; Selection/command rail reflects producer |
| After | Match scene unloaded; Scenario-owned placement and production state cleared | MainMenu active; No placement confirmation visible |

### Acceptance Signals

- A building placement request is accepted and committed through the production boundary.
- A production command is accepted and queues exactly one unit with the expected production index.
- Placement confirmation and runtime feedback transition through started, completed, and production-queued states.
- Required frame, GC, construction timing, request-count, entity-count, and UI metrics are present; no required runner is skipped.

### Metrics

- `buildingEntityDelta`
- `frameTimeAverageMs`
- `frameTimeMaxMs`
- `frameTimeP95Ms`
- `frameTimeP99Ms`
- `gcAllocationPerFrameP95Bytes`
- `gcAllocationRecurringSites`
- `gcAllocationTotalBytes`
- `memoryGrowthBytes`
- `namedConstructionSystemP95P99MaxMs`
- `placementRequestCountsByResult`
- `productionQueueCount`
- `uiObjectCounts`

### AM-004 Budget Placeholders

| Budget ID | Metric | Threshold | Unit | Status |
|---|---|---|---|---|
| `AM004.AM003-SCN-003-CONSTRUCTION.frame-time` | `frameTimeMs` | `null` | `milliseconds` | `pending-am-004` |
| `AM004.AM003-SCN-003-CONSTRUCTION.gc-allocation` | `gcAllocationAfterWarmup` | `null` | `bytes` | `pending-am-004` |
| `AM004.AM003-SCN-003-CONSTRUCTION.memory-growth` | `memoryGrowth` | `null` | `bytes` | `pending-am-004` |

### Failure Diagnostics

- Capture frame-time percentiles, GC sites, building-placement/production system samples, entity/UI counts, and feedback text/reason codes.
- Capture placement command/result codes, selected catalog item, cell validity, cost/resource deltas, building entity identity, producer identity, pending production count, and production index.
- Fail immediately on rejected command, unexpected count, missing metric, timeout, skipped runner, or retry request.

### Teardown And Reset

- Cancel any pending placement and clear placement preview state.
- Remove scenario-owned building/production entities or unload Match.
- Restore resources and selection state through fixture reset.
- Return to MainMenu and assert lifecycle/UI cleanup.

### Exclusions

- Android release, thermal, and sustained release acceptance
- Construction burst scale beyond one placement and one queued production
- Operation-map research and development

## AM003-SCN-004-TRANSPORT

Exercise deterministic boarding, disembark, rope, parachute, cargo, rejection, and cleanup behavior with the existing batch validator.

| Field | Value |
|---|---|
| Category | `transport` |
| Fixed seed | `3004` (uint32; AM-003 scenario ordinal) |
| Warmup | `16` scenario-executions |
| Measurement | `64` scenario-executions |
| Core lane | `executable-existing-logic-visual-and-performance-fixtures` |
| Android release execution | `deferred` |
| Thermal execution | `deferred` |
| Sustained release execution | `deferred` |

### Surfaces

- `Passenger drawer`
- `Transport boarding simulation`
- `Transport command feedback`
- `Transport visual playback`

### Prerequisites

- Exact baseline commit and tree are checked out with a clean tracked worktree.
- Required visual playback scenarios have registered runners.
- The performance fixture can create a 64 by 64 grid, one transport, and 8 selected passengers.
- Transport Scenario Lab definitions and production prefab registry resolve.

### Existing Fixtures

- `Assets/Game/Scripts/Editor/ScenarioLab/BattleScenarioLabValidationRunner.cs`
- `Assets/Game/Scripts/ScenarioLab/TransportBoardingScenarioCatalog.cs`
- `Assets/Tests/Editor/ScenarioLab/TransportBoardingScenarioLabTests.cs`
- `Assets/Tests/Editor/TransportBoardingPerformanceValidation.cs`
- `Assets/Tests/PlayMode/Aph807TransportBoardingFlowPlayModeTests.cs`
- `Assets/Tests/PlayMode/GameSceneTransportBoardingPlayModeTests.cs`

### Setup

1. Apply fixed seed 3004 and reset Scenario Lab/session state.
2. Validate the twelve-entry TransportBoardingScenarioCatalog and required definition paths.
3. Create clean world/grid/transport/passenger state per performance sample.

### Actions

1. Run the 16-scenario warmup defined by TransportBoardingPerformanceValidation.
2. Run 64 measured board-all/update/disembark-all samples with 8 passengers each.
3. Run required TB-001, TB-002, TB-003, TB-005, TB-006, TB-007, TB-008, and TB-009 production visual playback checks once each.
4. Run the cleanup/run-again validation once and reject residue.

### Stabilization

- Complete all tracked jobs before timing boundaries and result reads.
- Dispose and recreate the isolated World for each performance sample.
- Require no transport/passenger/drop/runtime-grid/command residue before each visual execution.

### Required States

| Phase | World state | UI state |
|---|---|---|
| Before | Eight selected passengers; Fresh grid and transport state | No passenger drawer open |
| Measurement | Eight passengers board; Eight passengers disembark; Required visual states are observed | Command feedback reflects accepted or production rejection reason; Passenger drawer reflects onboard/empty state where Match HUD is used |
| After | All isolated Worlds disposed; No transport scenario residue | No Scenario Lab overlay state retained; No passenger drawer bound |

### Acceptance Signals

- All 16 warmup scenarios and all 64 measured scenarios execute as distinct samples, not retries.
- Each measured sample boards and disembarks all 8 selected passengers.
- Required board/update/disembark timings, allocation, passenger, route, visual-state, entity, and UI metrics are present.
- Required catalog logic tests, production PlayMode transport tests, and performance validator pass without skip.

### Metrics

- `allocatedBytesCurrentThread`
- `boardCommandAverageP95P99MaxMs`
- `boardedPassengerCount`
- `boardingUpdateAverageP95P99MaxMs`
- `disembarkCommandAverageP95P99MaxMs`
- `disembarkedPassengerCount`
- `entityCounts`
- `pathRequestCounts`
- `residueCounts`
- `totalAverageP95P99MaxMs`
- `transportResultCountsByReason`
- `uiObjectCounts`
- `visualStateCounts`

### AM-004 Budget Placeholders

| Budget ID | Metric | Threshold | Unit | Status |
|---|---|---|---|---|
| `AM004.AM003-SCN-004-TRANSPORT.command-time` | `transportCommandTimeMs` | `null` | `milliseconds` | `pending-am-004` |
| `AM004.AM003-SCN-004-TRANSPORT.gc-allocation` | `gcAllocationMeasuredSamples` | `null` | `bytes` | `pending-am-004` |
| `AM004.AM003-SCN-004-TRANSPORT.memory-growth` | `memoryGrowth` | `null` | `bytes` | `pending-am-004` |

### Failure Diagnostics

- Capture board-command, boarding-update, disembark-command, total timing samples, current-thread allocation, and boarded/disembarked counts.
- Capture scenario ID, transport/passenger source keys, command result/reason code, passenger buffer length, disabled/passenger/drop components, pathfinding snapshot, and residue counts.
- Fail immediately on any skipped/no-runner result, passenger mismatch, residue, timeout, missing metric, or retry request.

### Teardown And Reset

- Clear Scenario Lab SessionState keys and report state.
- Dispose every sample World and native grid allocation.
- Run cleanup/run-again residue validation.
- Unload Scenario Lab or Match scene used by visual playback.

### Exclusions

- Airport/runway reservation, taxi, takeoff, landing, and full Match route contention
- Android release, thermal, and sustained release acceptance
- Operation-map research and development

## AM003-SCN-005-AIRCRAFT

Execute the canonical isolated aircraft and air-defense workload while making Scenario Lab limitations explicit.

| Field | Value |
|---|---|
| Category | `aircraft` |
| Fixed seed | `3005` (uint32; AM-003 scenario ordinal) |
| Warmup | `11` scenario-executions |
| Measurement | `11` scenario-executions |
| Core lane | `executable-scenario-lab-with-limitations` |
| Android release execution | `deferred` |
| Thermal execution | `deferred` |
| Sustained release execution | `deferred` |

### Surfaces

- `Air projectile and impact presentation`
- `Air-defense simulation`
- `Aircraft simulation`
- `Scenario Lab overlay`

### Prerequisites

- AD-001 through AD-011 definition assets and Scenario Lab prefab registry resolve.
- Every required AD definition has a registered runtime runner.
- Exact baseline commit and tree are checked out with a clean tracked worktree.
- Scenario Lab scene references and visual playback dependencies resolve.

### Existing Fixtures

- `Assets/Game/Scripts/Editor/ScenarioLab/BattleScenarioLabSuiteRunner.cs`
- `Assets/Game/Scripts/Editor/ScenarioLab/BattleScenarioLabValidationRunner.cs`
- `Assets/Tests/Editor/ScenarioLab/BattleScenarioAd002RunnerTests.cs`
- `Assets/Tests/Editor/ScenarioLab/BattleScenarioAd003RunnerTests.cs`
- `Assets/Tests/Editor/ScenarioLab/BattleScenarioAd008RunnerTests.cs`
- `Assets/Tests/Editor/ScenarioLab/BattleScenarioAd011RunnerTests.cs`

### Setup

1. Apply fixed seed 3005 and clear Scenario Lab suite/session state.
2. Discover AD definition assets and sort by scenario ID.
3. Assert the exact required ID set AD-001 through AD-011 and reject duplicate or missing IDs.

### Actions

1. Run one complete AD-001 through AD-011 suite as warmup and discard its performance samples.
2. Run AD-001 through AD-011 once each in scenario-ID order for measurement.
3. Require registered runners, passed variants, aircraft targets, air projectiles, impacts, and stable report identity for every required definition.

### Stabilization

- Complete tracked ECS jobs and require prefab registry/runtime grid readiness before each execution.
- Discard first complete suite performance samples as warmup.
- Reset Scenario Lab runtime state between definitions.

### Required States

| Phase | World state | UI state |
|---|---|---|
| Before | Prefab registry and runtime grid ready | Scenario Lab overlay ready |
| Measurement | Required aircraft target class active; Required projectile and impact states observed | Current scenario and variant IDs visible; Result reflects pass/failure without skip |
| After | No Scenario Lab aircraft residue | No Scenario Lab overlay state retained |

### Acceptance Signals

- AD-001 through AD-011 definitions are discovered in stable scenario-ID order and execute with registered runners.
- Aircraft target classes and air-missile projectile/impact evidence required by AD-002, AD-003, AD-008, and AD-011 are observed.
- All required variants pass and no suite entry is skipped, even though the current suite runner treats no-runner entries as skipped.
- Required scenario, variant, timing, allocation, entity, target-class, projectile, and visual-state metrics are present.

### Metrics

- `aircraftTargetCountsByClass`
- `entityCounts`
- `frameTimeAverageP95P99MaxMs`
- `gcAllocationPerScenarioBytes`
- `memoryGrowthBytes`
- `namedAircraftSystemP95P99MaxMs`
- `projectileAndImpactCounts`
- `scenarioDurationAverageP95P99MaxMs`
- `suiteResultCounts`
- `variantResultCounts`
- `visualStateCounts`

### AM-004 Budget Placeholders

| Budget ID | Metric | Threshold | Unit | Status |
|---|---|---|---|---|
| `AM004.AM003-SCN-005-AIRCRAFT.frame-time` | `frameTimeMs` | `null` | `milliseconds` | `pending-am-004` |
| `AM004.AM003-SCN-005-AIRCRAFT.gc-allocation` | `gcAllocationMeasuredScenarios` | `null` | `bytes` | `pending-am-004` |
| `AM004.AM003-SCN-005-AIRCRAFT.memory-growth` | `memoryGrowth` | `null` | `bytes` | `pending-am-004` |

### Failure Diagnostics

- Capture per-scenario frame/system timing, GC allocation, entity/projectile/visual counts, and cleanup residue.
- Capture suite scenario ID, asset path, runner registration, variant ID, support mode, target class/source key, projectile/impact state, failure reason, and report path.
- Fail immediately on skipped/no-runner entry, unsupported operation, missing aircraft target/projectile/impact, malformed report, timeout, or retry request.

### Teardown And Reset

- Assert no aircraft/projectile/impact residue before exit.
- Clear Scenario Lab entities, commands, SessionState keys, and report state.
- Unload Scenario Lab scene and dispose temporary Worlds.

### Exclusions

- Android release, thermal, and sustained release acceptance
- Full Match aircraft scheduling, formations, airport logistics, and rendering scale
- Operation-map research and development

## AM003-SCN-006-PROJECTILES

Exercise production ground-rocket and air-interceptor launch-to-impact lifecycles.

| Field | Value |
|---|---|
| Category | `projectiles` |
| Fixed seed | `3006` (uint32; AM-003 scenario ordinal) |
| Warmup | `1` scenario-executions |
| Measurement | `2` scenario-executions |
| Core lane | `executable-existing-playmode-and-scenario-lab-fixtures` |
| Android release execution | `deferred` |
| Thermal execution | `deferred` |
| Sustained release execution | `deferred` |

### Surfaces

- `Air projectile presentation`
- `Ground projectile presentation`
- `Impact feedback`
- `Projectile simulation`

### Prerequisites

- Exact baseline commit and tree are checked out with a clean tracked worktree.
- GM-001 and AD-001 definition assets and production prefab registry resolve.
- GM-001 and AD-001 have registered runners.
- Ground and air missile VFX references resolve.

### Existing Fixtures

- `Assets/Game/Scripts/Editor/ScenarioLab/BattleScenarioLabSuiteRunner.cs`
- `Assets/Game/Scripts/Editor/ScenarioLab/BattleScenarioLabValidationRunner.cs`
- `Assets/Tests/Editor/ScenarioLab/BattleScenarioAd001RunnerTests.cs`
- `Assets/Tests/Editor/ScenarioLab/BattleScenarioGm001RunnerTests.cs`
- `Assets/Tests/PlayMode/GroundMissileLauncherPlayModeTests.cs`

### Setup

1. Apply fixed seed 3006 and clear Scenario Lab/projectile state.
2. Prewarm ground and air missile VFX used by GM-001 and AD-001.
3. Snapshot launcher rack, target health, projectile counts, and visual counts.

### Actions

1. Run GM-001 once as warmup and discard its performance sample.
2. Run GM-001 once and measure visible ground-rocket launch, flight, impact, damage, and visual restoration.
3. Run AD-001 once and measure incoming ground projectile, air interceptor projectile, intercept event, and cleanup.

### Stabilization

- Complete tracked jobs between scenario setup, action, and result reads.
- Discard the first GM-001 execution as warmup.
- Require launcher, target, prefab registry, runtime grid, and VFX references before execution.

### Required States

| Phase | World state | UI state |
|---|---|---|
| Before | Launcher and target ready; No projectile active | Scenario Lab overlay ready |
| Measurement | Ground and air projectile lifecycles active; Impact and damage observed | Current scenario/variant visible; Impact/result feedback visible |
| After | Launcher rack visual restored; No scenario-owned projectile residue | No projectile scenario overlay state retained |

### Acceptance Signals

- AD-001 observes both ground and air projectile paths and an intercept event.
- GM-001 launches a visible ground rocket, applies damage on impact, and restores the rack visual.
- GroundMissileLauncherPlayModeTests and required Scenario Lab runners pass without skip.
- Required launch, flight, impact, damage, timing, allocation, entity, projectile, VFX, and cleanup metrics are present.

### Metrics

- `damageDelta`
- `entityCounts`
- `frameTimeAverageP95P99MaxMs`
- `gcAllocationPerScenarioBytes`
- `impactCounts`
- `memoryGrowthBytes`
- `namedProjectileSystemP95P99MaxMs`
- `projectileCountsByKind`
- `scenarioDurationMs`
- `vfxPoolAndVisualCounts`

### AM-004 Budget Placeholders

| Budget ID | Metric | Threshold | Unit | Status |
|---|---|---|---|---|
| `AM004.AM003-SCN-006-PROJECTILES.frame-time` | `frameTimeMs` | `null` | `milliseconds` | `pending-am-004` |
| `AM004.AM003-SCN-006-PROJECTILES.gc-allocation` | `gcAllocationMeasuredScenarios` | `null` | `bytes` | `pending-am-004` |
| `AM004.AM003-SCN-006-PROJECTILES.memory-growth` | `memoryGrowth` | `null` | `bytes` | `pending-am-004` |

### Failure Diagnostics

- Capture frame/system timing, GC allocation, projectile/VFX/entity counts, closest intercept distances, and timeout/failure reason.
- Capture scenario/variant IDs, launcher and target source keys, launch state, projectile entity/visual identity, altitude/distance observations, impact event, damage delta, visual restoration, and cleanup residue.
- Fail immediately on skipped/no-runner entry, missing visible rocket/projectile/impact/damage/restoration, timeout, missing metric, or retry request.

### Teardown And Reset

- Assert rack visual restoration and zero scenario-owned projectile residue.
- Clear projectile, impact, launcher, target, and VFX scenario state.
- Unload Scenario Lab scene and dispose temporary Worlds.

### Exclusions

- Android release, thermal, and sustained release acceptance
- Full Match projectile saturation beyond the maximum-combat scenario
- Operation-map research and development

## AM003-SCN-007-MAJOR-MATCH-UI

Exercise every major Match HUD surface and popup through production bindings in one fixed interaction sequence.

| Field | Value |
|---|---|
| Category | `major-match-ui` |
| Fixed seed | `3007` (uint32; AM-003 scenario ordinal) |
| Warmup | `60` frames |
| Measurement | `300` frames |
| Core lane | `executable-production-ui-sequence` |
| Android release execution | `deferred` |
| Thermal execution | `deferred` |
| Sustained release execution | `deferred` |

### Surfaces

- `ARIA Assistant`
- `Build Drawer`
- `Current-order and feedback`
- `Full Map`
- `Match Settings`
- `Minimap`
- `Objectives`
- `Passenger drawer`
- `Placement confirmation`
- `Resource Exchange`
- `Selection and command rail`
- `Squad tray`

### Prerequisites

- A deterministic selectable squad, loaded transport, valid building entry/cell, objectives, and minimap data are available.
- Exact baseline commit and tree are checked out with a clean tracked worktree.
- Match HUD prefab and all popup prefabs referenced by UIShellContentView resolve.
- Returning-user profile state is FirstLaunch completed.

### Existing Fixtures

- `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab`
- `Assets/Game/Scripts/UI/Contracts/UIRoute.cs`
- `Assets/Game/Scripts/UI/MainMenuPlayUI.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- `Assets/Tests/PlayMode/Aph806BuildingPlacementProductionPlayModeTests.cs`
- `Assets/Tests/PlayMode/Aph807TransportBoardingFlowPlayModeTests.cs`

### Setup

1. Reset route, enter Match, apply fixed seed 3007, and wait for Match HUD bindings.
2. Create deterministic squad, loaded transport, objective, minimap, and valid placement preconditions through existing fixture boundaries.
3. Snapshot UI object/pool counts, projection versions, popup state, selection state, and world counts.

### Actions

1. Observe objectives, minimap, squad tray, selection/command rail, and current-order/feedback in the base Match HUD.
2. Select a deterministic squad, issue one move command, and observe selection/command rail plus current-order/feedback.
3. Select the deterministic loaded transport and open/close the passenger drawer.
4. Open/close ARIA Assistant, then Build Drawer, then Full Map, then Resource Exchange; assert only one large tactical popup is active.
5. Open/close Match Settings and assert Match context.
6. Start deterministic building placement, observe placement confirmation, then cancel and verify feedback/current-order reset.
7. Use minimap focus once and select one squad from the squad tray.

### Stabilization

- Exclude initial prefab installation, catalog binding, and asset load frames before warmup.
- Require all base HUD views bound and no popup/placement active before warmup.
- Wait for MatchHudReady and stable UI read-model versions for 30 frames.

### Required States

| Phase | World state | UI state |
|---|---|---|
| Before | Deterministic squad/transport/objective/placement prerequisites ready | Match HUD ready; No placement active; No popup visible |
| Measurement | No duplicate shell root or runtime binding; World remains live while UI actions project current state | All twelve required surfaces observed; At most one large tactical popup active; Match Settings uses Match context; Selection, order, feedback, placement, passenger, objective, minimap, and squad states update |
| After | Match scene unloaded; Scenario-owned commands and placement cleared | MainMenu active; No Match HUD or Match popup installed |

### Acceptance Signals

- All twelve required Match UI surfaces are installed, interactable, and observed in the declared action sequence.
- Large tactical popup mutual exclusion is preserved across ARIA Assistant, Build Drawer, Full Map, and Resource Exchange.
- Match Settings opens in Match context and closes without route corruption.
- Required frame, GC, projection/version, interaction, object-count, popup-count, and world-state metrics are present; no required runner is skipped.

### Metrics

- `activePopupCountByKind`
- `entityCounts`
- `frameTimeAverageMs`
- `frameTimeMaxMs`
- `frameTimeP95Ms`
- `frameTimeP99Ms`
- `gcAllocationPerFrameP95Bytes`
- `gcAllocationRecurringSites`
- `gcAllocationTotalBytes`
- `interactionResultCounts`
- `managedProjectionRebuildCounts`
- `memoryGrowthBytes`
- `namedUiMarkerP95P99MaxMs`
- `uiObjectAndPoolCounts`

### AM-004 Budget Placeholders

| Budget ID | Metric | Threshold | Unit | Status |
|---|---|---|---|---|
| `AM004.AM003-SCN-007-MAJOR-MATCH-UI.frame-time` | `frameTimeMs` | `null` | `milliseconds` | `pending-am-004` |
| `AM004.AM003-SCN-007-MAJOR-MATCH-UI.gc-allocation` | `gcAllocationAfterWarmup` | `null` | `bytes` | `pending-am-004` |
| `AM004.AM003-SCN-007-MAJOR-MATCH-UI.memory-growth` | `memoryGrowth` | `null` | `bytes` | `pending-am-004` |

### Failure Diagnostics

- Capture active route/phase, popup kind/count, selected entity/squad, command mode/result/reason, placement state, passenger count, minimap input, objective/squad projection versions, and object identities.
- Capture frame-time percentiles, GC sites, UI marker samples, managed projection rebuild counts, object/pool counts, world entity counts, and fatal/error markers.
- Fail immediately on missing/duplicate surface, overlapping large popup, wrong Settings context, stale projection, missing metric, timeout, skipped runner, or retry request.

### Teardown And Reset

- Clear selection, sticky command mode, feedback, and passenger-drawer state.
- Close every popup and cancel placement through production commands.
- Compare final UI object/pool/world counts against reset expectations.
- Return to MainMenu, unload Match, and assert no Match HUD content remains.

### Exclusions

- Android release, thermal, and sustained release acceptance
- FirstLaunch UI
- Operation-map research and development
- Result route, which is explicitly unimplemented

## AM003-SCN-008-RETURNING-MENU-TO-MATCH

Measure returning-user Menu-to-Match lifecycle, UI binding, World preservation, and audio transition.

| Field | Value |
|---|---|
| Category | `returning-user-menu-to-match` |
| Fixed seed | `3008` (uint32; AM-003 scenario ordinal) |
| Warmup | `1` round-trips |
| Measurement | `5` transitions |
| Core lane | `executable-five-sample-returning-user-transition` |
| Android release execution | `deferred` |
| Thermal execution | `deferred` |
| Sustained release execution | `deferred` |

### Surfaces

- `Main Menu`
- `Match HUD`
- `Menu-to-Match loading transition`
- `Music transition`

### Prerequisites

- Exact baseline commit and tree are checked out with a clean tracked worktree.
- Menu and Match scenes and serialized lifecycle/UI/audio references resolve.
- Production UI shell gateway accepts EnterMatch.
- Profile state is explicitly FirstLaunch completed without editing separately owned FirstLaunch files.

### Existing Fixtures

- `Assets/Game/Scripts/UI/Contracts/UIRoute.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs`
- `Assets/Tests/PlayMode/Aph805MenuMatchMenuLifecyclePlayModeTests.cs`
- `Assets/Tests/PlayMode/MenuMatchMusicPlayModeTests.cs`

### Setup

1. Set fixture profile to FirstLaunch completed and apply fixed seed 3008.
2. Load Menu single, wait for MenuBootstrapView and default ECS World, and assert one lifecycle root.
3. Snapshot World identity, scene objects, entities, UI bindings, and active Menu music.

### Actions

1. Execute one complete Menu-to-Match-to-Menu round trip as warmup.
2. For each of five measured samples, enqueue EnterMatch with UIRoute.Match and PushHistory false.
3. Wait for Match scene, MatchSceneView, runtime UI dependencies, Match HUD binding, stable shell state, and Match music.
4. Record sample and return to Menu for reset; a failed sample terminates the scenario and is not retried.

### Stabilization

- Do not include warmup round-trip samples in measurement.
- Require Match fully unloaded and no Match HUD binding before each sample.
- Require MenuReady, transition-running false, one lifecycle root, and stable Menu music before each sample.

### Required States

| Phase | World state | UI state |
|---|---|---|
| Before | Default ECS World created; Match scene unloaded; One lifecycle root | FirstLaunch not visible; MainMenu ready; Transition not running |
| Measurement | Match scene loaded additively; One lifecycle root; Same default ECS World | Loading transition progresses; Match HUD ready and bound; Match music active |
| After | Default ECS World preserved; Match scene unloaded; One lifecycle root | MainMenu ready; Menu music active; No Match HUD installed |

### Acceptance Signals

- Each transition loads Match additively, preserves the default ECS World and one lifecycle root, binds runtime UI, and crossfades to Match music.
- Exactly five measured Menu-to-Match transitions complete; repetitions are samples, never retries.
- Required transition, loading, frame, GC, world/lifecycle, UI-binding, audio, entity, and object-count metrics are present; no required runner is skipped.
- Returning-user profile bypasses FirstLaunch and Menu is ready before measurement.

### Metrics

- `audioCrossfadeDurationMs`
- `entityCountDelta`
- `frameTimeAverageP95P99MaxMs`
- `gcAllocationPerTransitionBytes`
- `lifecycleRootCount`
- `loadingGateDurationMs`
- `memoryGrowthBytes`
- `sceneObjectCountDelta`
- `transitionDurationAverageP95P99MaxMs`
- `uiBindingCounts`
- `worldIdentityChanges`

### AM-004 Budget Placeholders

| Budget ID | Metric | Threshold | Unit | Status |
|---|---|---|---|---|
| `AM004.AM003-SCN-008-RETURNING-MENU-TO-MATCH.gc-allocation` | `transitionGcAllocation` | `null` | `bytes` | `pending-am-004` |
| `AM004.AM003-SCN-008-RETURNING-MENU-TO-MATCH.memory-growth` | `transitionMemoryGrowth` | `null` | `bytes` | `pending-am-004` |
| `AM004.AM003-SCN-008-RETURNING-MENU-TO-MATCH.transition-time` | `transitionDuration` | `null` | `milliseconds` | `pending-am-004` |

### Failure Diagnostics

- Capture frame-time, GC allocation, memory/object/entity deltas, loading progress, timeout, and fatal/error markers per sample.
- Capture route request, shell current mode/route/phase/sequence/running flag, scene load state, World identity, lifecycle-root count, UI dependency bindings, music clip/source state, and transition phase timings.
- Fail immediately on FirstLaunch appearance, scene/binding/music mismatch, duplicate root, World replacement, timeout, skipped runner, or retry request.

### Teardown And Reset

- After each measured sample, return through production Match-to-Menu route.
- After sample five, leave Menu ready and clear scenario metrics/seed state.
- Assert Match unload, runtime UI cleanup, one lifecycle root, and preserved World before the next sample.

### Exclusions

- Android release cold/warm startup acceptance
- FirstLaunch execution or ownership
- Operation-map research and development
- Result route

## AM003-SCN-009-RETURNING-MATCH-TO-MENU

Measure returning-user Match-to-Menu unload, runtime cleanup, World preservation, and audio restoration.

| Field | Value |
|---|---|
| Category | `returning-user-match-to-menu` |
| Fixed seed | `3009` (uint32; AM-003 scenario ordinal) |
| Warmup | `1` round-trips |
| Measurement | `5` transitions |
| Core lane | `executable-five-sample-returning-user-transition` |
| Android release execution | `deferred` |
| Thermal execution | `deferred` |
| Sustained release execution | `deferred` |

### Surfaces

- `Main Menu`
- `Match HUD`
- `Match-to-Menu unloading transition`
- `Music transition`

### Prerequisites

- Exact baseline commit and tree are checked out with a clean tracked worktree.
- Menu and Match scenes and serialized lifecycle/UI/audio references resolve.
- Production UI shell gateway accepts ReturnToMainMenu.
- Profile state is explicitly FirstLaunch completed without editing separately owned FirstLaunch files.

### Existing Fixtures

- `Assets/Game/Scripts/UI/Contracts/UIRoute.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs`
- `Assets/Tests/PlayMode/Aph805MenuMatchMenuLifecyclePlayModeTests.cs`
- `Assets/Tests/PlayMode/MenuMatchMusicPlayModeTests.cs`

### Setup

1. Set fixture profile to FirstLaunch completed and apply fixed seed 3009.
2. Load Menu, enter Match through production route, and wait for Match HUD/runtime bindings.
3. Snapshot World identity, lifecycle root, Match runtime references, scene objects, entities, HUD binding, and Match music.

### Actions

1. Execute one complete Menu-to-Match-to-Menu round trip as warmup.
2. For each of five measured samples, begin from stable Match and enqueue ReturnToMainMenu with UIRoute.MainMenu and PushHistory false.
3. Wait for Match unload, shell MainMenu readiness, runtime UI cleanup, one lifecycle root, preserved World, and Menu music.
4. Record sample and enter Match again for the next sample; a failed sample terminates the scenario and is not retried.

### Stabilization

- Do not include warmup round-trip samples in measurement.
- Require Match runtime UI dependencies bound before issuing return.
- Require MatchHudReady, transition-running false, one lifecycle root, and stable Match music before each sample.

### Required States

| Phase | World state | UI state |
|---|---|---|
| Before | Default ECS World active; Match scene loaded; One lifecycle root | Match HUD ready and bound; Match music active; Transition not running |
| Measurement | Match runtime state is removed during unload; Same default ECS World | MainMenu becomes ready; Match HUD is removed; Unloading transition progresses |
| After | Default ECS World preserved; Match scene unloaded; One lifecycle root | MainMenu ready; Menu music active; No Match HUD installed |

### Acceptance Signals

- Each transition unloads Match, preserves Menu and the default ECS World, retains exactly one lifecycle root, and removes Match runtime/UI references.
- Exactly five measured Match-to-Menu transitions complete; repetitions are samples, never retries.
- Menu music is restored and Match music is inactive after each transition.
- Required transition, unload, frame, GC, world/lifecycle, UI cleanup, audio, entity, and object-count metrics are present; no required runner is skipped.

### Metrics

- `audioCrossfadeDurationMs`
- `entityCountDelta`
- `frameTimeAverageP95P99MaxMs`
- `gcAllocationPerTransitionBytes`
- `lifecycleRootCount`
- `matchRuntimeReferenceCounts`
- `memoryGrowthBytes`
- `sceneObjectCountDelta`
- `transitionDurationAverageP95P99MaxMs`
- `uiCleanupCounts`
- `worldIdentityChanges`

### AM-004 Budget Placeholders

| Budget ID | Metric | Threshold | Unit | Status |
|---|---|---|---|---|
| `AM004.AM003-SCN-009-RETURNING-MATCH-TO-MENU.gc-allocation` | `transitionGcAllocation` | `null` | `bytes` | `pending-am-004` |
| `AM004.AM003-SCN-009-RETURNING-MATCH-TO-MENU.memory-growth` | `transitionMemoryGrowth` | `null` | `bytes` | `pending-am-004` |
| `AM004.AM003-SCN-009-RETURNING-MATCH-TO-MENU.transition-time` | `transitionDuration` | `null` | `milliseconds` | `pending-am-004` |

### Failure Diagnostics

- Capture frame-time, GC allocation, memory/object/entity deltas, unload phase timings, timeout, and fatal/error markers per sample.
- Capture route request, shell route/phase/sequence/running flag, scene unload state, World identity, lifecycle-root count, destroyed MatchSceneView state, retained runtime references, HUD content, and music source state.
- Fail immediately on retained Match scene/runtime/UI, duplicate root, World replacement, music mismatch, timeout, skipped runner, or retry request.

### Teardown And Reset

- After each measured sample, assert cleanup before re-entering Match.
- After sample five, leave Menu ready with Match unloaded and one lifecycle root.
- Clear scenario metrics and seed state.

### Exclusions

- Android release cold/warm startup acceptance
- FirstLaunch execution or ownership
- Operation-map research and development
- Result route

## AM003-SCN-010-LONG-SOAK

Define a long core-lane steady-state Match soak without claiming deferred Android sustained acceptance.

| Field | Value |
|---|---|
| Category | `long-soak` |
| Fixed seed | `3010` (uint32; AM-003 scenario ordinal) |
| Warmup | `60` seconds |
| Measurement | `1800` seconds |
| Core lane | `executable-1800-second-editor-soak` |
| Android release execution | `deferred` |
| Thermal execution | `deferred` |
| Sustained release execution | `deferred` |

### Surfaces

- `Long-duration Match simulation`
- `Match HUD steady state`
- `Structured performance recorder`

### Prerequisites

- Exact baseline commit and tree are checked out with a clean tracked worktree.
- Host can keep the Editor foreground and uninterrupted for 1860 seconds plus setup/teardown.
- Menu-to-Match route, default ECS World, Match HUD, fixture seeding, and structured interval recorder are available.
- Returning-user profile state is FirstLaunch completed.

### Existing Fixtures

- `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs`
- `Assets/Game/Scripts/Editor/MatchPerformanceFixtureSeed.cs`
- `Assets/Tests/Editor/MatchGcAllocationCallstackCaptureTests.cs`
- `Assets/Tests/Editor/MatchPerformanceFixtureSeedTests.cs`
- `Design/AgentReports/2026-07-12_aph-804_release_evidence_contract.md`
- `Design/Architecture/performance_regression_contract.md`

### Setup

1. Reset route, enter Match through production shell, and apply fixed seed 3010.
2. Ensure fixture floor and assert a second Ensure call is idempotent.
3. Snapshot route, World, lifecycle, fixture, HUD, entity/UI/object/pool, memory, and recorder state.

### Actions

1. Enter Match, ensure the deterministic idle fixture floor, and start the 60-second warmup.
2. Run 1800 uninterrupted seconds in foreground with fixed camera, no player commands, no popups, and one-second structured interval sampling.
3. Record start/end and interval state; terminate on any route, World, lifecycle, fixture, HUD, fatal, or collector discontinuity.

### Stabilization

- Complete the full 60-second warmup before any acceptance sample.
- Complete tracked jobs and require stable fixture/entity/UI/object/pool counts for 30 frames.
- Exclude scene load, import, seeding, first-install, and recorder-start frames.

### Required States

| Phase | World state | UI state |
|---|---|---|
| Before | Fixture floor stable; One lifecycle root | Match HUD ready; No popup visible |
| Measurement | Fixture floor remains stable; Same default ECS World; Same route and one lifecycle root | Match HUD remains bound; No popup, selection, or sticky command mode; UI object counts remain continuously sampled |
| After | Default ECS World preserved; Match scene unloaded | MainMenu ready; No Match HUD installed |

### Acceptance Signals

- Match route, World identity, lifecycle-root count, fixture floor, HUD binding, and seed remain stable for the full measurement.
- No retry, pause, backgrounding, skipped runner, fatal marker, timeout, or metrics gap is accepted.
- Required interval frame/GC/system/entity/UI/object/pool/memory-growth samples and start/end state snapshots are present.
- The core-lane soak completes 60 seconds of warmup and 1800 seconds of uninterrupted measured foreground Match time.

### Metrics

- `entityCountTimeSeries`
- `frameTimeAverageMs`
- `frameTimeMaxMs`
- `frameTimeP95Ms`
- `frameTimeP99Ms`
- `gcAllocationIntervalAndTotalBytes`
- `lifecycleRootCountTimeSeries`
- `managedAndNativeMemoryGrowthBytes`
- `namedHotSystemP95P99MaxMs`
- `poolAndPresentationObjectCountTimeSeries`
- `routeAndWorldIdentityChanges`
- `uiObjectCountTimeSeries`

### AM-004 Budget Placeholders

| Budget ID | Metric | Threshold | Unit | Status |
|---|---|---|---|---|
| `AM004.AM003-SCN-010-LONG-SOAK.frame-time` | `frameTimeMs` | `null` | `milliseconds` | `pending-am-004` |
| `AM004.AM003-SCN-010-LONG-SOAK.gc-allocation` | `gcAllocationAfterWarmup` | `null` | `bytes` | `pending-am-004` |
| `AM004.AM003-SCN-010-LONG-SOAK.memory-growth` | `memoryGrowth` | `null` | `bytes` | `pending-am-004` |

### Failure Diagnostics

- Capture one-second interval frame/GC/system/entity/UI/object/pool/memory samples plus route/phase, World identity, lifecycle-root count, fixture counts, HUD binding, process state, and fatal/error markers.
- Fail immediately on interruption, backgrounding, route/World/root/HUD/count drift, missing interval, timeout, skipped runner, or retry request.
- On failure retain the first divergent interval, preceding ten intervals, top GC call stacks, top system samples, object/entity deltas, and teardown status.

### Teardown And Reset

- Historical APH-804 output may be compared diagnostically but cannot satisfy this run.
- Restore profiler/logging state and clear seed/recorder/session state.
- Return to MainMenu, unload Match, and assert one lifecycle root with no Match runtime UI.
- Stop and flush structured capture without dropping the final interval.

### Exclusions

- Android release, thermal, battery, and sustained release acceptance
- Historical soak evidence as acceptance for this baseline
- Operation-map research and development

## Verified Authorities

- `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab`
- `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs`
- `Assets/Game/Scripts/Editor/MatchPerformanceFixtureSeed.cs`
- `Assets/Game/Scripts/Editor/ScenarioLab/BattleScenarioLabSuiteRunner.cs`
- `Assets/Game/Scripts/Editor/ScenarioLab/BattleScenarioLabValidationRunner.cs`
- `Assets/Game/Scripts/ScenarioLab/TransportBoardingScenarioCatalog.cs`
- `Assets/Game/Scripts/UI/Contracts/UIRoute.cs`
- `Assets/Game/Scripts/UI/MainMenuPlayUI.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- `Assets/Tests/Editor/MatchGcAllocationCallstackCaptureTests.cs`
- `Assets/Tests/Editor/MatchPerformanceFixtureSeedTests.cs`
- `Assets/Tests/Editor/ScenarioLab/TransportBoardingScenarioLabTests.cs`
- `Assets/Tests/Editor/TransportBoardingPerformanceValidation.cs`
- `Assets/Tests/PlayMode/Aph805MenuMatchMenuLifecyclePlayModeTests.cs`
- `Assets/Tests/PlayMode/Aph806BuildingPlacementProductionPlayModeTests.cs`
- `Assets/Tests/PlayMode/Aph807TransportBoardingFlowPlayModeTests.cs`
- `Assets/Tests/PlayMode/BuildingPlacementProductionPlayModeTests.cs`
- `Assets/Tests/PlayMode/GameSceneTransportBoardingPlayModeTests.cs`
- `Assets/Tests/PlayMode/GroundMissileLauncherPlayModeTests.cs`
- `Assets/Tests/PlayMode/MenuMatchMusicPlayModeTests.cs`
- `Design/AgentReports/2026-07-12_aph-804_release_evidence_contract.md`
- `Design/Architecture/performance_regression_contract.md`

All authority paths above exist at the bound commit. No operation-map, FirstLaunch, tracker, production-code, scene, prefab, package, or project-setting file is changed by AM-003.

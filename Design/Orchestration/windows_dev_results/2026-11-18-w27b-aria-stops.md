# Windows dev results — W27b ARIA self-stops S059 (114) and S075 (101 Inspect) (2026-11-19)

Branch `missions/win-w27b-aria-stops` (from integration `65b1808de`). The Windows dev lead did this task. Shared ARIA code, under the owner's standing OK given in this Windows session (2026-11-19): "Standing OK for tested shared code, tool and map work from the program lead's queue; no unit-stat or game-rule changes." Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at `ace5499e8`. Logs under `win-dev-unity\w27b-utf\` and `w27b-menu\`.

**Outcome:** S059 is fixed: Victory, no stop. S075 did not stop this time: a normal Defeat with no ARIA stop. Its Inspect block is unexplained and now instrumented.

## 1. Changes `ace5499e8`
- **S059 Blocked 114** (`[AriaBlocked] reason=114 kind=Unavailable intent=ReturnAircraft control=Squad4`, W27 log `w27-menu`:55463).
  - `StepExactAircraftService` reported `Unavailable` (through `Target()`) whenever a control it wanted was not shown: the bound aircraft's Return or action button, or a page or card control. `AriaPlayDecisionSystem` then stops ARIA with 114 after 15 s without progress.
  - Every service target now goes through `ServiceTarget`. It reports Waiting while the control is missing. After `MaxServiceTargetWaitSeconds = 5`, it ends the service with `EndExactAircraftService`, which sets the normal 15 s pause.
  - New field `AriaAircraftServicePlan.UnavailableSince`.
- **S075 Blocked 101 Inspect.** This is the assault map-inspection stage-4 block (`AriaSkirmishPlanSystem.cs`, "Never dismiss a foreign/unreachable popup"), and the log did not say why. As a diagnostic only, `[AriaBlockedControl]` now also logs `mapOpen`, `mapOwned`, `fullMap`, `mapId`, `inspectMapId`, `inspectStage`, `closeMap`, `drawer` and `placement`. No behaviour change.

## 2. EditMode tests (`w27b-utf`)
- Suites: AriaAircraftService, AriaObjectiveSkill, AriaObjectiveCoverage, AriaObjectiveSupport, AriaFindBaseRecovery, SkirmishExpandedAria, MatchHudAssistantUiSystemHelper and SkirmishConvoyServiceJourney.
- **Result: 164/164 Passed**, including the new `UnavailableServiceControlEndsTheServiceBeforeTheInputLayerStops`. That test asserts Waiting, not Unavailable, while the control is missing, and that the service ends after 5 s, well inside the 15 s limit.

## 3. Menu runs (NON-counted, Standard Regular, en; `w27b-menu\menu\unity.log`)

| Row | Seed | Result | match s | ARIA stop | Before (W27, `w27-menu`) |
|---|---|---|---|---|---|
| S059 MP FC | 104731 | **Victory** TicketsDepleted (return yes) | 705.5 | — | Abort, 114 ReturnAircraft at 291 s |
| S075 IB BA air field | 130365 | Defeat (BA) | 342.4 | — | Abort, 101 Inspect at 430 s |

- No `[AriaBlocked]` line in either run, so the S075 Inspect block did not recur and the new diagnostic had nothing to capture.
- **S075 loss:** Base Assault on Industrial Basin still loses (W27 cause 2 territory).
- Harness evidence moved to `w27b-menu\harness-evidence`, not committed.

## Open
- **S075 Inspect block:** unexplained; the diagnostic will name the map facts if it recurs.
- **W27 cause 2:** Industrial Basin assault progress.

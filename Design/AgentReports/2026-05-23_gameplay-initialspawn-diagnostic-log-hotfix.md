# WarlineCapture Handoff

## Lane
Gameplay

## Task
Investigate inconsistent Unity Editor FPS after reverting the startup initialization extraction, and remove avoidable diagnostic noise from the runtime startup path.

## Files changed
- `Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs`
- `Design/AgentReports/2026-05-23_gameplay-initialspawn-diagnostic-log-hotfix.md`

## Contracts touched
- No architecture contract change.
- Existing `EnableInitialSpawnDiagnostics` flag now gates initial-spawn and initial-base diagnostic warnings/count logs consistently.

## User-visible behavior
No intended gameplay behavior change. Initial unit/base spawning still runs the same logic, but repeated `[InitialBase]` and initial-spawn warning logs are suppressed unless diagnostics are explicitly enabled.

## Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests;InitialFactionBaseValidationTests`
- Editor log scan: `/Users/farhad/Library/Logs/Unity/Editor.log`

## Validation result
- `git diff --check`: passed.
- `GameplayArchitectureContractTests;InitialFactionBaseValidationTests`: passed 73/73. Results: `/private/tmp/warlinecapture-initialspawn-log-hotfix-editmode.xml`.
- Editor log showed 72 `[InitialBase]` entries, repeated `InitialUnitsSpawnSystem` stack traces, a `RuntimeCitySpawner=1138.2ms` startup hitch, and later steady frames around 19-20 FPS with roughly 9M triangles / 21M vertices and render/GPU waits.

## Known gaps
- This hotfix removes Console/log overhead from the startup retry path, but it does not solve the heavier render-side cost visible in the current Editor log.
- The current FPS measurement is not a clean steady-state baseline because the log also includes asset import/domain reload hitches and scene streaming.
- A controlled runtime capture is still needed after clearing Console and waiting for import/compilation to finish.

## Cross-lane impacts
- No Art/UI asset changes.
- No scene content changes.

## Next recommended task
Run a clean in-editor performance capture after Unity finishes importing: clear Console, enter Play once, wait until initial city/base spawn completes, then measure a 10-second stable window. If FPS remains below target, optimize the active scene/render cost first, because the current log points to very high triangle/vertex counts and render-thread/GPU waits rather than the reverted architecture extraction.

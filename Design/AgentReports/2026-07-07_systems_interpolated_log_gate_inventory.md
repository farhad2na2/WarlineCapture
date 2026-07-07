# Systems Interpolated Log Gate Inventory - 2026-07-07

## Scope
Inventory direct interpolated `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError` calls under `Assets/Game/Scripts/Systems`.

This report supports `Design/Architecture/perf_architecture_reaudit_followup_tracker.md` Phase 2.

## Summary

| Classification | Count |
|---|---:|
| Already gated before interpolation | 18 |
| Editor/development-only | 2 |
| Warning/error retained | 4 |
| Intentional one-shot startup marker | 2 |
| Needs guard hoist | 0 |

No code changes were needed. The recurring diagnostics found in this pass already check a feature flag, interval, or slow-frame threshold before constructing the interpolated log string.

## Direct Interpolated Debug Calls

### Already Gated
- `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:937` - `VerboseResourceHaulerLogs` guard before interpolation.
- `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:941` - `VerboseResourceHaulerLogs` guard before interpolation.
- `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:962` - `VerboseResourceHaulerLogs` guard before interpolation.
- `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:992` - `VerboseResourceHaulerLogs` guard before interpolation.
- `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:999` - `VerboseResourceHaulerLogs` guard before interpolation.
- `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:1008` - `VerboseResourceHaulerLogs` guard before interpolation.
- `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:1014` - `VerboseResourceHaulerLogs` guard before interpolation.
- `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:1027` - `VerboseResourceHaulerLogs` guard before interpolation.
- `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:1037` - `VerboseResourceHaulerLogs` guard before interpolation.
- `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:1046` - `VerboseResourceHaulerLogs` guard before warning interpolation.
- `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:1053` - `VerboseResourceHaulerLogs` guard before interpolation.
- `Assets/Game/Scripts/Systems/CitizenPopulationDiagnosticsSystemHelper.cs:159` - `EnableCitizenPopulationDiagnostics` and `FreezeLogThresholdSeconds` return before interpolation.
- `Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:417` - loading-gate waiting diagnostic is interval-gated before interpolation.
- `Assets/Game/Scripts/Systems/SelectionRuntimeDiagnosticsSystemHelper.cs:248` - `EnableSelectionClickDiagnostics` return before interpolation inside the helper.
- `Assets/Game/Scripts/Systems/SelectionRuntimeDiagnosticsSystemHelper.cs:263` - `EnableMoveCommandTrace` return before interpolation inside the helper.
- `Assets/Game/Scripts/Systems/SelectionRuntimeDiagnosticsSystemHelper.cs:271` - `EnableScanCommandTrace` return before interpolation inside the helper.
- `Assets/Game/Scripts/Systems/UnitAnimationIndexSystem.cs:94` - `EnableAnimationIndexFreezeLogs` and slow-frame threshold before interpolation.
- `Assets/Game/Scripts/Systems/UnitGridMovementSystem.cs:790` - `EnableGridMovementFreezeLogs` and slow-frame threshold before interpolation.

### Editor / Development Only
- `Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs:328` - inside `UNITY_EDITOR`.
- `Assets/Game/Scripts/Systems/PreGameEcsActivityDiagnosticsSystem.cs:83` - system disables itself outside editor/development builds and interval-gates before interpolation.

### Warning / Error Retained
- `Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:982` - invalid-capacity warning is deduplicated by entity and retained for failure diagnosis.
- `Assets/Game/Scripts/Systems/CustomGameStartupSystemHelper.cs:824` - startup warning retained for missing converted ECS unit prefabs.
- `Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:227` - loading-gate fail-open error retained.
- `Assets/Game/Scripts/Systems/UnitRespawnSystem.cs:65` - no-free-spawn-cell warning retained.

### Intentional One-shot Startup Markers
- `Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:216` - loading-gate ready marker.
- `Assets/Game/Scripts/Systems/RuntimeGridBootstrapStartupSystemHelper.cs:54` - runtime-grid bootstrap ready marker.

## Notes
- This pass did not broaden scope into every diagnostic helper call site. Sampled helper call sites already gate expensive interpolation with `SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace` or `EnableScanCommandTrace` before constructing the message.
- A broader helper-call-site audit can be a later slice if profiling shows string allocation from these disabled diagnostics.
- No steady-state GC gate was rerun for this report because Phase 2 made no runtime code changes.

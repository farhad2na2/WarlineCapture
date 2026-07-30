# VRP-054 Windows Remediation Evidence

Date: 2026-07-31
Scope: candidate-only remediation after the rejected VRP-053 Android capture

## Result

The Windows remediation substep passed. Android remeasurement and the VRP-054
expansion decision remain open.

- Runtime residency now derives its required and guard envelopes from the camera
  frustum projected across the baked operation-map presentation height range.
  It no longer uses the elevated camera X/Z position as the envelope center.
- `UnitMassRenderSettingsSystem` now tests operation-map identity and authored
  vehicle exclusions immediately after each parent traversal. Operation-map
  parents carrying `UnitGrid` and `Faction` can no longer be misclassified as
  incomplete units and retried every frame.
- Unit render children classified by the mass-render pass receive the faction
  tint target/default color contract in that same one-shot traversal.
  `UnitFactionTintTargetBackfillSystem` excludes entities already carrying
  `UnitMassRenderSettingsApplied`, so it cannot repeat the same hierarchy walk.

## Checked Windows Validation

- Camera/initial-view contract:
  `%TEMP%\warline-vrp054-camera-host-final.log`;
  `[OperationMapRenderVirtualizationInitializationFocusedValidation] result=Passed tests=9`.
- Mass-render classification:
  `%TEMP%\warline-vrp054-unit-render-host.log`;
  `[UnitRenderBudgetFocusedValidation] result=Passed tests=36`.
- Faction tint/backfill:
  `%TEMP%\warline-vrp054-faction-tint-host.log`;
  `[FactionTintFocusedValidation] result=Passed tests=4`.
- Dense-count stationary CPU proxy:
  `%TEMP%\warline-vrp054-windows-steady-state.log`;
  `[VRP054WindowsSteadyState] result=Passed renderChildren=70710 samples=300 p95Ms=0.0016 budgetMs=8.3333`.
- ECS/Burst architecture:
  `%TEMP%\warline-vrp054-ecs-architecture-host-rerun.log`;
  `[EcsBurstHotPathArchitectureValidation] result=Passed tests=11`.

The dense-count proxy creates exactly `70,710` operation-map render children,
requires the bounded classifier to drain them, then measures 300 stationary
updates of the two former dominant CPU owners. The 8.3333 ms threshold is a
120-Hz Editor CPU budget for these two systems only; it is not a whole-frame
rendering measurement and is not Android evidence.

## Rejected Attempts

- Sandboxed batch/GUI wrapper attempts failed before project import because the
  generic Hub licensing client rejects LocalIPC 1.18.1 and the sandbox denied
  the Editor's versioned-client hardware query.
- The first host-access compile rejected a blob array passed by value
  (`EA0009`); the corrected non-readonly `ref` contract passed.
- The first camera fixture used a hand-built view matrix and failed the
  fail-closed envelope gate. The accepted fixture publishes matrices from a
  temporary Unity `Camera`, matching the production camera snapshot boundary.
- The first architecture invocation named a nonexistent execute method and
  produced no pass marker. The corrected checked invocation passed 11/11.

No rejected attempt contributes to a pass claim.

## Open Gate

`VRP-054` remains unchecked. Expansion and Phase 6 remain forbidden until the
same Android route proves:

- materially improved CPU-main p95;
- active proxy cells/placements and nonzero enabled slots;
- visual/state parity;
- no replacement dominant main-thread owner.

Production remains `StaticSceneChunks + ResidentEntities`;
`productionCutover=0`. No accepted, frozen, production, Addressables, or Android
artifact changed.

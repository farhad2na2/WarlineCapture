# Phase 7 Agent D Handoff - P7-0090 BuildingPlacementRedirectSystem

Branch:
`codex/phase7-agent-d-building-production`

Rows completed:
- `P7-0090` - `BuildingPlacementRedirectSystem` - `Retired/Folded`

Responsibility split:
- Old: disabled `SystemBase` wrapper with empty lifecycle; runtime building side-effect composition called placement redirect methods directly.
- New: plain direct-owned placement redirect helper. Deferred side-effect depth, placed-footprint redirect queues, pending marker refresh, overlap/perimeter redirect goal search, and move-order request writes stayed unchanged.

Architecture notes:
- No new manager/controller/facade/broad replacement `ISystem`/runtime `MonoBehaviour` loop.
- Runtime creation and runtime tick composition still own when redirect side effects are invoked; this change only removes the empty ECS shell.
- Redirect logic still uses explicit context delegates for `EntityManager`, grid data, query preparation, and redirect-unit query access.
- Marker refresh remains a deferred callback consumer, not a gameplay owner.

Inventory impact:
- Total ECS system declarations: `298`.
- Production `SystemBase`/legacy declarations: `165`.
- Production `ISystem` declarations: `133`.
- Production non-UI rows: `291`.
- Agent D rows: `29`.
- `SplitThenConvert` rows: `88`.
- Open rows: `143`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `BuildingPlacementRuntimeTickSystemTests.RunFocusedValidation`: passed, log `/private/tmp/warline-phase7-agent-d-placement-redirect-helper-fold-placement-runtime.log`, marker `[BuildingPlacementRuntimeTickFocusedValidation] result=Passed tests=3`.
- `BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation`: passed, log `/private/tmp/warline-phase7-agent-d-placement-redirect-helper-fold-placement-command.log`, marker `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`.
- `BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation`: passed, log `/private/tmp/warline-phase7-agent-d-placement-redirect-helper-fold-smoke.log`, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `UnitMoveOrderSystemTests.RunFocusedValidation`: passed, log `/private/tmp/warline-phase7-agent-d-placement-redirect-helper-fold-move-order.log`, marker `[UnitMoveOrderFocusedValidation] result=Passed tests=15`.
- `NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation`: passed, log `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`: passed after tracker and handoff updates.

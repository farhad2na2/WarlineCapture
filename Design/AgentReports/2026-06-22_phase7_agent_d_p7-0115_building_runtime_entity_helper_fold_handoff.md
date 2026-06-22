# Phase 7 Agent D Handoff - P7-0115 BuildingRuntimeEntitySystem

Branch:
`codex/phase7-agent-d-building-production`

Rows completed:
- `P7-0115` - `BuildingRuntimeEntitySystem` - `Retired/Folded`

Responsibility split:
- Old: `BuildingRuntimeEntitySystem` was a disabled `SystemBase` wrapper with empty lifecycle methods. Building runtime composition called its public blocker, combat entity, delete, and destroyed-building helper methods directly.
- New: `BuildingRuntimeEntitySystem` is a plain direct-owned runtime entity helper. Blocker entity creation, combat entity creation, runtime building delete/destroy callbacks, and pathing-blocker policy stayed unchanged.

Architecture notes:
- No new manager, controller, facade, broad replacement `ISystem`, or runtime `MonoBehaviour` loop was introduced.
- The helper still uses `EntityManager` from explicit context delegates; no gameplay behavior was moved into a Unity-object presentation owner.
- The managed `GameObject` argument remains only on the existing destroyed-building callback boundary and was not moved into unmanaged `ISystem` code.

Inventory impact:
- Total ECS system declarations: `300`.
- Production `SystemBase`/legacy declarations: `167`.
- Production `ISystem` declarations: `133`.
- Production non-UI rows: `293`.
- Agent D rows: `31`.
- `SplitThenConvert` rows: `90`.
- Open rows: `145`.

Validation:
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation`: passed, log `/private/tmp/warline-phase7-agent-d-runtime-entity-helper-fold-smoke.log`, marker `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `RuntimeBuildingSystemTests.RunFocusedValidation`: passed, log `/private/tmp/warline-phase7-agent-d-runtime-entity-helper-fold-runtime-building.log`, marker `[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5`.
- `NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation`: passed, log `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- `git diff --check`: passed.

Risks:
- Remaining Agent D rows include broad runtime boundary, spawn, production, placement lifecycle, selection, UI query, and map placement owners. They require inspection for real split/conversion work before any additional shell fold.

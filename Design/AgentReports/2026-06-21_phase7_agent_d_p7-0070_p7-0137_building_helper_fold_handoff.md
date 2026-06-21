# Phase 7 Agent D Handoff - Building Helper Fold

Branch:
`codex/phase7-agent-d-building-production`

Rows completed:

- `P7-0070` - `BuildingGameplayGridDataSystem` - `Retired/Folded`
- `P7-0137` - `BuildingSurfacePlacementSystem` - `Retired/Folded`

Files changed:

- `Assets/Game/Scripts/Systems/BuildingGameplayGridDataSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingSurfacePlacementSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_d_building_production_tracker.md`
- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`

Responsibility split:

- Old:
  - Both rows were disabled `SystemBase` wrappers with empty `OnUpdate`.
  - Both were constructed directly with `new()` by building/runtime composition code and tests, not scheduled by ECS groups.
- New:
  - `BuildingGameplayGridDataSystem` is a plain direct-owned helper for grid data and screen-cell lookup.
  - `BuildingSurfacePlacementSystem` is a plain direct-owned helper for map-surface footprint sampling and `BuildingSurfaceComponent` projection.
  - No gameplay policy moved into a managed presentation/config exception and no broad replacement shell was introduced.

Counts:

- Converted to `ISystem`: `0`
- Split passive/managed boundaries: `0`
- Managed `SystemBase` exceptions: `0`
- Retired/folded: `2`

Validation:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with `0 Warning(s), 0 Error(s)`.
- `git diff --check`: passed.
- `/private/tmp/warline-phase7-agent-d-helper-fold-map-surface.log`: `[MapSurfaceLayeredGridFocusedValidation] result=Passed tests=15`.
- `/private/tmp/warline-phase7-agent-d-helper-fold-placement-command.log`: `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`.
- `/private/tmp/warline-phase7-agent-d-helper-fold-composition-smoke.log`: `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/private/tmp/warline-phase7-agent-a-architecture.log`: `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.

Coordination notes:

- Agent C selection command contracts were not changed.
- Agent F building visuals were not touched.
- Agent E road/city/citizen systems were not touched.

Rows returned to Agent A:

- None.

Risks:

- This slice only removes disabled ECS lifetime boilerplate from two direct-owned helpers. Broad building spawn, production, runtime query, and placement command owners remain open for later Agent D slices.

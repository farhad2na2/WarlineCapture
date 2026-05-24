# BuildingPlacementSystem Retirement Audit

Date: 2026-05-24
Lane: Gameplay

## Current Status

`BuildingPlacementSystem` is not deletable yet. It no longer owns the main managed runtime `Update()` entry point, and `GameBootstrap` / `ManagedGameplayStartupSystem` no longer carry it directly. It still exists as a temporary managed composition shell under `BuildingGameplayCompositionSystem`.

Current measured size:
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`: 2322 lines.
- Public/internal facade declarations: 135, excluding the class declaration.

## Allowed Production Facade References

These are the only production files allowed to reference the facade during the next retirement steps:

- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`

No other production file should construct, store, pass, or call `BuildingPlacementSystem`.

## Allowed Test Facade Construction

These editor validation harnesses still construct the facade directly and must migrate before final deletion:

- `Assets/Tests/Editor/AIProductionValidationTests.cs`
- `Assets/Tests/Editor/AIBuildPlannerValidationTests.cs`
- `Assets/Tests/Editor/AIEndToEndValidationTests.cs`
- `Assets/Tests/Editor/BaseBreachValidationTests.cs`
- `Assets/Tests/Editor/BuildingRuntimeBoundaryValidationTests.cs`
- `Assets/Tests/Editor/InitialFactionBaseValidationTests.cs`

`Assets/Tests/Editor/RuntimeGameplayStateTestHelper.cs` still accepts a `BuildingPlacementSystem` parameter for legacy boundary publication tests.

## Remaining Facade Responsibilities

The facade still owns or exposes these migration debts:

- Managed construction of building domain systems and context factory access.
- Runtime building registry access and runtime building dictionary exposure.
- Runtime tick source callbacks for placement pointer flow, click suppression, and diagnostics, adapted by managed composition.
- Runtime visual/resource animation ticks and destroyed-building/combat sync ticks are now wired from managed composition to `BuildingRuntimeVisualSystem` and `BuildingCombatSystem`.
- Marker-refresh and road barrier door ticks are now wired from managed composition to `BuildingPlacementRedirectSystem` and `BuildingBarrierSystem`.
- Runtime boundary context source callbacks for spawn, production request, runtime query, resource, and ECS boundary queries.
- Production/resource/hauler context source callbacks.
- Active placement UI/session command wrappers.
- Runtime/manual building and wall spawn wrappers.
- Selection, building query, combat/breach, resource, and UI command/query compatibility wrappers.
- Test-only runtime tick and runtime building validation hooks.

## Deletion Gates

1. Move remaining tick callbacks by domain: runtime visuals, destruction/combat sync, barrier door updates, marker refresh, pointer/selection click flow, and diagnostics.
2. Move runtime registry ownership and read access out of the facade.
3. Move definition/config initialization into managed composition and narrow systems.
4. Move runtime spawn/runtime creation/runtime ownership context factories out of the facade.
5. Move production request, production update, production transport, and hauler context factories out of the facade.
6. Move runtime resource and unit prefab context wiring out of the facade.
7. Move active placement lifecycle/session command wrappers out of the facade.
8. Move placement grid/input/preview/commit context wiring out of the facade.
9. Move UI command/query compatibility wrappers out of the facade.
10. Move selection and interaction compatibility wrappers out of the facade.
11. Replace `BuildingGameplayCompositionSystem` construction so it no longer calls `new BuildingPlacementSystem()`.
12. Migrate remaining editor validation tests to a narrow building gameplay harness.
13. Delete `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs` and its `.meta`.
14. Remove facade allowlists and update the architecture contract to require zero facade references.

## Drift Guard

Until deletion:
- `BuildingPlacementSystem.cs` must not grow beyond 2322 lines.
- Public/internal facade declarations must not exceed 135.
- Production facade references must remain inside the two allowed files above.
- Production construction must remain isolated to `BuildingGameplayCompositionSystem`.
- New building behavior must extend an owning `*System` slice, not the facade.

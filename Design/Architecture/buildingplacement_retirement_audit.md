# BuildingPlacementSystem Retirement Audit

Date: 2026-05-24
Lane: Gameplay

## Current Status

`BuildingPlacementSystem` is not deletable yet. It no longer owns the main managed runtime `Update()` entry point, and `GameBootstrap` / `ManagedGameplayStartupSystem` no longer carry it directly. It still exists as a temporary managed composition shell under `BuildingGameplayCompositionSystem`.

Current measured size:
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`: 1982 lines.
- Public/internal facade declarations: 120, excluding the class declaration.

## Allowed Production Facade References

These are the only production files allowed to reference the facade during the next retirement steps:

- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`

No other production file should construct, store, pass, or call `BuildingPlacementSystem`.

## Step 17 Blocker Inventory

Deletion is blocked by the exact references below. Do not add new items to this
inventory; remove them in order during Steps 18-25.

### Production Composition Blockers

`Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs` still owns the production facade dependency:

- `Result` stores `private readonly BuildingPlacementSystem PlacementFacade`.
- `Result` constructor accepts `BuildingPlacementSystem placementFacade`.
- `Initialize` constructs `new BuildingPlacementSystem()`.
- `Initialize` calls `placementFacade.Init(...)`.
- `Result.BindSelection`, `Result.CreateCitizenPopulation`, and `Result.BindCitizenPopulation` reach through `PlacementFacade`.
- `Initialize` builds narrow runtime/menu/feature outputs from facade properties and context factories.
- Runtime tick helpers still accept `BuildingPlacementSystem placement`:
  - `CreateRuntimeTickSource`
  - `CreateInputRuntimeTickContext`
  - `CreateProductionRuntimeTickContext`
  - `CreateRuntimeTickDiagnosticsContext`
  - `CreateRuntimeBoundaryPublishContext`

### Production Facade Surface Blockers

`Assets/Game/Scripts/UI/BuildingPlacementSystem.cs` still exposes production-facing systems, contexts, state, and wrappers:

- Composition/system accessors: `RuntimeCitySpawnSystem`, `RuntimeQuerySystem`, `RuntimeResourcePrefabContextSystem`, `BuildingUiCommandSystem`, `BuildingUiQuerySystem`, `BuildingPlacementInteractionSystem`, `RuntimeTickSystem`, `RuntimeTickDomains`, `RuntimeInputDomains`, `WorldCamera`, `ActivePlacement`, `PlayRequested`, `BuildModeActive`, `RuntimeBuildingRegistry`, `DayNightSystem`, `FactionResourceSystem`, `ProductionUpdateSystem`, `ProductionContextSystem`, `ResourceHaulerBridgeSystem`, `BuildingSpawnSystem`, `RuntimeBoundarySystem`, `DefinitionSystem`, `RuntimeSpawnSystem`, `RuntimeContextSystem`, `ProductionRequestSystem`, `RuntimeBoundaryQuery`, `OilBarrelsPerFuelBarrelRatio`, `BuildingSelectionClickSystem`, config preview properties.
- Runtime/read wrappers: runtime building ids by role/house, runtime building focus, destroyed/refugee state, combat info, base-breach target, approach-cell checks, spawn-unit prefab lookup, dollar spend/init.
- Placement/session/UI wrappers: soldier-base/tent/factory placement, confirm/cancel/exit, selected-building unit production, production-arm, selected-building delete/clear, placement UI pointer, selected-building labels/descriptions/status, active placement pointer context.
- Runtime spawn/test wrappers: initial roster, runtime building/wall spawn, wall/building footprint lookup, production spawn point/helipad lookup.
- Runtime maintenance/test wrappers: destroyed combat sync, runtime tick for tests, road barrier door tests, runtime building entity lookup, destroyed-state checks, road barrier gate rects.
- Context/source wrappers still exposed on or behind the facade:
  - `CreateBuildingProductionContextSource`
  - `CreateBuildingRuntimeContextSource`
  - `CreateRuntimeResourcePrefabContextSource`
  - `CreateBuildingUiCommandContext`
  - `CreateBuildingUiQueryContext`
  - `CreateBuildingPlacementInteractionContext`
  - `CreateBuildingRuntimeVisualContext`
  - `CreateBuildingPlacementRedirectContext`
  - `CreateBuildingCombatContext`
  - `CreateBuildingRuntimeQueryContext`
  - `CreateRuntimeBuildingQueryContext`
  - `CreateBuildingBarrierContext`
- Private context factories still used internally by facade methods:
  - `CreatePlacementContextSource`
  - `CreateRuntimeSpawnCommandContext`
  - `CreateBuildingSpawnContext`
  - `CreateBuildingRuntimeEntityContext`
  - `CreateBuildingUiContextSource`
  - `CreateBuildingPlacementInteractionContextSource`

### Config/Startup Blockers

The facade still exposes startup/config wiring that must move before deletion:

- `Init(...)`, `BindDependencies(...)`, and `Dispose()` remain facade methods until production construction moves out of `BuildingGameplayCompositionSystem`.
- Runtime dependency references for road build, menu UI, selection, grid blocker, city spawner, citizen population, faction visuals, and day/night.
- Runtime building instance/combat/blocker cleanup still runs from facade `Dispose()`.

The following startup/config ownership has moved out of the facade:

- Serialized config fields and runtime camera/build-plane/preview values now live in `BuildingPlacementStartupSystem`.
- Configured spawnable/unit lookup rebuild and configured definition startup selection now live in `BuildingPlacementStartupSystem`.
- `RuntimeBuildings` root creation now lives in `BuildingPlacementStartupSystem`.

### Editor Test Blockers

The following editor test harnesses still construct or type against `BuildingPlacementSystem` and must migrate to a narrow building gameplay harness:

- `Assets/Tests/Editor/AIProductionValidationTests.cs`
- `Assets/Tests/Editor/AIBuildPlannerValidationTests.cs`
- `Assets/Tests/Editor/AIEndToEndValidationTests.cs`
- `Assets/Tests/Editor/BaseBreachValidationTests.cs`
- `Assets/Tests/Editor/BuildingRuntimeBoundaryValidationTests.cs`
- `Assets/Tests/Editor/InitialFactionBaseValidationTests.cs`
- `Assets/Tests/Editor/RuntimeGameplayStateTestHelper.cs`

`Assets/Tests/Editor/GameplayArchitectureContractTests.cs` intentionally references the facade while enforcing the temporary debt rules; these references must be removed in Step 24 after the facade is deleted.

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
- Runtime building registry ownership, count, dictionary, id allocation, and selected/active ids live in `RuntimeBuildingSystem`; managed composition now consumes that registry instead of facade count/dictionary properties.
- Runtime tick diagnostics threshold, enablement, timing normalization, and log formatting now live in `BuildingPlacementRuntimeTickDiagnosticsSystem`.
- Placement pointer flow, click suppression, and selection click handling are now wired from managed composition to `BuildingPlacementInputRuntimeTickSystem`.
- Runtime visual/resource animation ticks and destroyed-building/combat sync ticks are now wired from managed composition to `BuildingRuntimeVisualSystem` and `BuildingCombatSystem`.
- Marker-refresh and road barrier door ticks are now wired from managed composition to `BuildingPlacementRedirectSystem` and `BuildingBarrierSystem`.
- Definition/configured spawnable/unit lookup state and authored spawnable/unit prefab lists now live in `BuildingDefinitionSystem`; the facade still forwards the top-level config object into that boundary.
- Runtime spawn, runtime creation, runtime ownership, runtime city-spawn, building spawn, runtime entity, runtime visual, redirect, combat, runtime query, and barrier context construction now live in `BuildingRuntimeContextSystem`; the facade still exposes temporary source bundles and wrapper methods.
- Runtime boundary context source callbacks for spawn, production request, runtime query, resource, and ECS boundary queries.
- Production request/update/transport/queue and resource-hauler bridge context construction now lives in `BuildingProductionContextSystem`; the facade still exposes a temporary production source bundle.
- Runtime resource, runtime unit-prefab, citizen resource, citizen prefab, and building spawn-prefab context construction now lives in `BuildingRuntimeResourcePrefabContextSystem`; the facade still exposes a temporary resource/prefab source bundle.
- Active placement begin/cancel/confirm/exit command flow and selection-preservation state now live in `BuildingPlacementSessionSystem`; the facade still exposes temporary compatibility methods and placement session context assembly.
- Runtime/manual building and wall spawn command translation now lives in `BuildingRuntimeSpawnCommandSystem`; the facade still exposes temporary compatibility methods for legacy tests and callers.
- Placement lifecycle, input, validation, and commit context construction now lives in `BuildingPlacementContextSystem`; the facade still exposes a temporary placement context source bundle.
- UI command/query context construction now lives in `BuildingUiContextSystem`; the facade still exposes temporary UI context source callbacks.
- Selection/interaction context construction now lives in `BuildingPlacementInteractionContextSystem`; the facade still exposes temporary interaction context source callbacks.
- Selection-click context construction now lives in `BuildingSelectionClickSystem`; selection context construction now lives in `BuildingSelectionSystem`; selected-building query context construction now lives in `BuildingPlacementQuerySystem`; the facade still exposes temporary wrapper methods while callers migrate.
- Placement config application, runtime building root creation, configured definition startup selection, build plane/camera/preview config state, and placement preview initialization now live in `BuildingPlacementStartupSystem`; the facade still exposes temporary `Init(...)`, `BindDependencies(...)`, and `Dispose()` compatibility methods.
- Selection, building query, combat/breach, resource, and UI command/query compatibility wrappers.
- Test-only runtime tick and runtime building validation hooks.

## Deletion Gates

17. Inventory final facade blockers and freeze this audit.
18. Extract remaining runtime context factories from the facade.
19. Extract selection and query facade wrappers.
20. Extract config/init ownership into managed composition or narrow startup/config systems.
21. Replace `BuildingGameplayCompositionSystem` construction so it no longer calls `new BuildingPlacementSystem()`.
22. Migrate remaining editor validation tests to a narrow building gameplay harness.
23. Delete `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs` and its `.meta`.
24. Remove facade allowlists and update the architecture contract to require zero facade references.
25. Run the validation gate: architecture tests, building runtime boundary tests, bootstrap/menu playmode smoke, and one focused runtime load validation.

## Drift Guard

Until deletion:
- `BuildingPlacementSystem.cs` must not grow beyond 1982 lines.
- Public/internal facade declarations must not exceed 120.
- Production facade references must remain inside the two allowed files above.
- Production construction must remain isolated to `BuildingGameplayCompositionSystem`.
- New building behavior must extend an owning `*System` slice, not the facade.

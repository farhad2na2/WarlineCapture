# W34: small airport building `building.airport_small` (no mission wired)

Branch `missions/win-w34-small-airport`, cut from integration 58a95037d. The owner approved the plan on 2026-11-19 and approved building W34 in this session on 2026-11-21 ("Yes, build W34"). It is new shared game code. There are no unit-stat changes and no changes to existing rules: with no small airport alive, every existing decision takes the same path as before.

Commits:
- `3c19aee29` contracts and mapping;
- `e121f7b2e` eligibility, enemy AI, gate model and runtime;
- `1b072fa04` builder;
- `950cab082` builder layout fix (run 1 failed, see below);
- `c764e630c` generated prefab, destroyed variant and config;
- `0995e183c` tests;
- `ca9e8b4ee` CS0104 fix in the tests.

## Code sites changed (the feasibility note's list, in order)
- **`SkirmishRosterContracts.cs`:**
  - Adds `SkirmishStructureIds.AirportSmall = "building.airport_small"`.
  - `VisualKey` maps it to `Building_Airport_Small`, and `FromVisualKey` maps it back.
  - `ProducerFor` maps it to the new `SkirmishProducerKind.SmallAirport`. The value 7 is appended, so values 0–6 are unchanged.
  - New `StructureFor(producer)`, the inverse mapping.
  - New `SkirmishAirportTypes` holds the one rule: `IsAirport`, `SmallAirportServes` (fighter and strike only), `Serves`, `Resolve` and `OfferedStructure`.
- **`SkirmishRoleCatalogConfig.cs:96-102`, `SkirmishRoleOverlayCatalog.cs:42-47`:**
  - The canonical bindings are unchanged: fighter, strike and transport_plane still name `Airport`.
  - New `SkirmishRoleOverlayCatalog.WithSmallAirport(overlays)` is the small-airport map form. It moves fighter and strike to `SmallAirport`, withholds transport_plane, and copies everything else unchanged.
  - Nothing calls it yet. That is the mission-wiring step.
- **`SkirmishProductionEligibility.cs:70`:**
  - The single `AirportPresent` check became `SkirmishAirportTypes.Resolve(overlay producer, role, AirportPresent, SmallAirportPresent)`.
  - It prefers the overlay's own airport type, then the other type if that type may serve the role.
  - `decision.Producer` is the resolved kind, so reservations, queue locks and AirPadReady follow the structure that actually exists.
  - Field `producer.airport_small` is the missing-producer field for a small-airport overlay.
  - `SkirmishProductionRequest` and `SkirmishPublicPerception` gain `SmallAirportPresent`. It defaults to false, so existing callers are unchanged.
- **Runtime callers:**
  - `SkirmishProductionService` sets `SmallAirportPresent` in `EvaluateQueue`. `AirPadReady` treats both airports as FullArsenal pads. It adds `HasLivingProducerFor(overlay)`.
  - `SkirmishNativeProduction.cs:37-38`: the small airport also requires FullArsenal to place. The missing-producer check uses `HasLivingProducerFor`.
  - `SkirmishResearchService`: aircraft-efficiency research uses a small airport only when neither the Helipad nor the full Airport exists.
  - `SkirmishStrategyScoring`, `SkirmishAriaPublicProjection` and `SkirmishEnemyStrategySystem.cs:117` set `SmallAirportPresent`. `SkirmishEnemyEconomicCoordinator` sets it in its all-present projection.
- **`SkirmishProductionCatalog.cs:127`:** `ProducerPrefabKey(SmallAirport)` is `Building_Airport_Small`.
- **Enemy AI:**
  - `SkirmishEnemyEconomicCoordinator.ConstructionStructureFor(overlay)` replaces the inline `FromVisualKey(ProducerPrefabKey(...))`.
  - The enemy builds the airport type its map's overlay offers: `building.airport` or `building.airport_small`.
  - A small-airport overlay for transport_plane offers nothing, so it is skipped and never built.
  - Every existing overlay gets the same construction target as before (tested).
  - `SkirmishEnemyConstructionAdapter.cs:388` and `SkirmishMapLayoutBuilder.PadKindForStructure` map the small airport to the `AirReturn` pad.
  - `SkirmishEnemyStrategySystem.cs:480` adds the id to the footprint-trace list.
- **`SkirmishS002AriaRunHarness.GateUnits.cs:78`:** the gate-unit model maps `SmallAirport` to `building.airport_small`.
- **Runway mechanics:**
  - `UnitAirMovementSystem.cs:16-22` is unchanged; it uses fractions of the runway.
  - `BuildingRunwaySystem.PrefabGeometry.cs:28-77` is unchanged. It reads the shortened `Runway`/`Runway_Start`/`Runway_End` children, and the runtime definition reports `HasRunway=1`.
  - `BuildingRunwayPathingSurface.cs:15` now also opens the strip of `Building_Airport_Small` for ground pathing, not only `Building_Airport`.

## transport_plane exclusion: new producer kind
- **Choice:** option (b) from the note, a new producer kind `SmallAirport`, with the rule in `SkirmishAirportTypes`.
- **Why not (a), a per-map army profile:**
  - It would keep the small airport on producer kind `Airport`.
  - Every `HasLivingProducer(Airport)` check would then accept a small airport for transport_plane on any map whose profile allows it.
  - The guarantee would depend on each map's profile being authored correctly, which does not satisfy "transport_plane can never come from the small airport".
- **What (b) guarantees:**
  - The restriction is structural. `Resolve` and `Serves` never return `SmallAirport` for transport_plane, whatever the overlay, presence flags or map.
  - Even a malformed overlay that names `SmallAirport` for transport_plane is rejected (tested over every presence combination).
  - The extra cost is small: one appended enum value and the airport branches listed above.
  - The small-airport map form also withholds transport_plane from the overlays.

## Delivery mode: the jet's own runway landing (unchanged), no transport plane
- The small airport produces only Jet_01 and Jet_02.
- **How jets arrive:**
  - Their unit configs have no `productionTransportPrefab`.
  - So `ResolveProductionTransportSettings` makes each jet its own transport in `ProductionTransportMode.Plane`: `usesRunwaySelfArrival`, `requiresAirportRunway`.
  - The jet lands on the nearest own runway and taxis.
  - `Unit_Veh_Plane_Transport` delivery is never used for the small airport's recipes.
- **Landing geometry scales with the strip** (`BuildingProductionTransportPresentationSystemHelper.ManagedDelivery.cs:45-77`):
  - touchdown at `min(8 m, 0.35·half)` from the runway start;
  - taxi from touchdown to the runway centre (about 44 m on the 103.5 m strip);
  - approach from `max(80, 5·half)` (about 259 m) at +28 m.
  - Nothing needs more strip than exists, so no self-arrival switch was needed and no delivery code was changed.
- **Pending:** a visual or play check of a jet landing on the short strip. No play-mode run was made.

## Prefab and builder
- **`Game.Editor.SmallAirportPrefabBuilder.Build`** (marker `[SmallAirportPrefabBuilder] result=Passed`) builds three assets:
  - `Assets/Game/Prefabs/Buildings/Building_Airport_Small.prefab`: a prefab variant of `Building_Airport.prefab`, with transform and authoring overrides only:
    - runway scale x 0.57 / z 0.72 (strip 119.50 × 29.90 m, from 209.34 × 41.63 m);
    - barrier length compensated;
    - tower at the west end beside the strip;
    - 10 parking spawns kept at their fractional place on the strip shoulders;
    - `footprintCells` 120×40;
    - config set to the new config.
  - `Assets/Game/Prefabs/Buildings/Destroyed/Building_Airport_Small_Destroyed.prefab`: a variant of `Building_Airport_Destroyed.prefab` with the destroyed tower moved to the new tower position.
  - `Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_AirportSmall_Config.asset`:
    - a copy of the Airport config with display name "Small Airport" (unique, because definitions also match by display name);
    - the destroyed variant;
    - productions `Unit_Veh_Jet_01` and `Unit_Veh_Jet_02` only;
    - health, price, materials and portraits as the Airport (no new values invented).
- **Checks the builder fails closed on:**
  - SHA-256 of `Building_Airport.prefab`, its destroyed visual and its config unchanged;
  - model bounds enclosing exactly 120×40;
  - runtime `BuildingDefinition` footprint 120×40 with `HasRunway`;
  - runway Start–End length between 80 and 120 m;
  - spawns inside the bounds with at least 1 m margin;
  - both assets are variants of the right base;
  - every dependency of the new assets already a dependency of the Airport assets (`newBinaryDependencies=0`: Airport meshes, materials and portraits reused).
- **Run 1 (`w34-builder-run1/unity.log:2429`): Failed.** The unscaled strip (41.63 m across) plus the tower (9.35 m) cannot fit the 40-cell depth. No asset was written. Fixed in `950cab082`.
- **Run 2 (`w34-builder-run2/unity.log:1736`): Passed.** `footprint=120x40 runwayLengthM=103.49 runwayScale=0.57x0.72 strip=119.50x29.90 modelBounds=119.50x39.50 runtimeFootprint=120x40 spawnsInside=10/10 productions=Unit_Veh_Jet_01,Unit_Veh_Jet_02 created=prefab:1,destroyed:1,config:1 baseUnchanged=1 newBinaryDependencies=0`
- **Idempotence:**
  - Run 3 (`w34-builder-run3-idempotence/unity.log:506`): Passed with `created=prefab:0,destroyed:0,config:0`. The SHA-256 of all six generated files matched run 2 (`sha256sum -c`: 6× OK).
  - Run 4 (`w34-builder-run4-committed/unity.log:504`) ran on the committed assets at `ca9e8b4ee`: Passed, and `git status --short` in the main checkout afterwards was **empty**.

## Byte identity
- `git diff --stat origin/missions/integration HEAD` on the protected paths gives **0 lines**:
  - `Building_Airport.prefab`;
  - `Building_Airport_Destroyed.prefab`;
  - `Prefab_BuildingDefinition_Airport_Config.asset`;
  - `Assets/Game/Resources`, `Assets/Game/Configs/Skirmish` and `Assets/Game/Configs/Scene`;
  - `Design/MapVariants`.
- The only non-code changes are the six added files above.
- No placement config or mission references them: `Construction.asset` and `Game_BuildingPlacement_Config.asset` do not list `Building_Airport_Small` (tested).
- **S004 keeps its exclusion:** it has no airport overlay and no Airport or small airport structure. `SkirmishS004NoAirportTests` passes 5/5.

## EditMode before/after
- **Filter, same for both runs:** `SkirmishSmallAirportTests|SkirmishGateUnitsModelTests|SkirmishS004NoAirportTests|SkirmishEnemyConstructionAdapterTests|SkirmishEnemyEconomicCoordinatorTests|SkirmishEnemyEconomyTests|SkirmishEnemyPaidProductionAdapterTests|SkirmishExpandedDefinitionTests|SkirmishGenericGroundRosterTests|SkirmishSharedProductionRecipeTests|SkirmishExpandedAriaTests|BuildingMaterialsCostConfigProjectionTests`
- **Baseline at 58a95037d** (`w34-base-58a95037d/unity.log:2539`, `results.xml`): **total 205, passed 205, failed 0.**
- **After at ca9e8b4ee** (`w34-after-ca9e8b4ee/unity.log:1086`, `results.xml`): **total 215, passed 215, failed 0.**
  - The 10 added tests are all new `SkirmishSmallAirportTests`.
  - Every other fixture has the same counts as the baseline: Aria 81, Definition 34, ConstructionAdapter 23, PaidProduction 17, Coordinator 11, Economy 11, GenericGroundRoster 8, SharedProductionRecipe 7, S004 5, BuildingMaterialsCost 5, GateUnits 3.
  - The failing set is identical (empty) apart from the new tests.
- **Failed attempt:** `w34-after-0995e183c` did not run any tests. It failed to compile with CS0104: `SkirmishMapLayoutBuilder` is ambiguous between Game.Configs and Game.Editor. Fixed in `ca9e8b4ee`.
- **New tests:**
  - id/prefab mapping, with the full Airport mapping unchanged;
  - fighter and strike from either airport, transport_plane only from the full Airport;
  - transport_plane never resolves to the small airport;
  - the small-airport map form;
  - the enemy airport-type choice;
  - live queue and perception on an ECS fixture;
  - the gate-unit model on S003 in small-airport form;
  - the prefab spec;
  - the full Airport assets and roles unchanged;
  - no mission offers the small airport, and S004 keeps its exclusion.
- **Prefab rewrites by tests:** none. The main checkout was clean after both test runs.

## LFS
`git diff --name-only origin/missions/integration HEAD | git check-attr --stdin filter`: every path is `filter: unspecified`. No LFS-tracked file is added.

## Not done / pending
- **No mission is wired.** No session or setup offers the small airport, and no placement config lists the prefab. Wiring needs all of the following:
  - `WithSmallAirport` on the map's overlays;
  - `Building_Airport_Small` in that session's spawnables;
  - legal pads per side, for example City Crossroads from the feasibility note.
- **Visual review pending:** the shortened runway, the parked jets on the narrowed shoulders, the destroyed tower position, and a jet landing on the 103.5 m strip.
- **Real-player/device acceptance is pending.**

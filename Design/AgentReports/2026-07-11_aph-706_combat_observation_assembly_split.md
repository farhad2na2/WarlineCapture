# APH-706 Combat Observation Assembly Split

Date: 2026-07-11
Status: Complete

## Result

The combat-damage observation seam now compiles in the dedicated `Game.Runtime.Combat` assembly. The source asset GUID is preserved, shared observation data remains in `Game.Components`, and the resulting dependency direction is:

```text
Game.Runtime -> Game.Runtime.Combat -> Game.Components
Game.Tests.Editor -> Game.Runtime.Combat
```

The regenerated dependency report contains no reverse `Game.Runtime.Combat -> Game.Runtime` edge and no first-party assembly cycle. The new assembly owns one source file and two declared types.

## Implementation

- Moved `CombatDamageObservationBootstrapSystem` and `CombatDamageObservationUtility` to `Systems/Combat/Observation` under namespace `Game.Runtime.Combat`.
- Added `Game.Runtime.Combat.asmdef` with only `Game.Components` as a first-party dependency.
- Added the combat assembly reference to runtime and editor-test assemblies.
- Updated direct-fire, building-defense, ground-missile, air-missile, telemetry, diagnostics, and architecture-gate call sites.
- Preserved the observation component contract in `Game.Components` and retained the unmanaged `ISystem` bootstrap.
- Regenerated the APH-700 dependency report: 19 assemblies, 82 first-party edges, 99 external references, 1,208 owned source files, 2,573 declared types, and 30,514 resolved cross-domain occurrences.

The fetched first-launch narrative slice exposed pre-existing violations of the frozen assembly and source-growth contracts during integrated validation. This slice repaired those violations by moving shared narrative enums into catalog contracts, removing UI/config reverse dependencies, extracting startup/loading partials, and renaming newly added production `*SystemHelper` files without changing narrative behavior. It also replaced direct structural `EntityManager` mutations in the disabled spatial-index shadow system with command-buffer/singleton access so the Burst gate remains fail-closed and green.

## Validation

| Gate | Result | Evidence |
|---|---|---|
| Combat observation telemetry | Passed `9/9` | `/private/tmp/warline-aph706-combat-observation-final.log` |
| Assembly boundaries | Passed `31/31` | `/private/tmp/warline-aph706-boundary-final.log` |
| ECS/Burst architecture | Passed `10/10` | `/private/tmp/warline-aph706-burst-final.log` |
| Production source growth | Passed `15/15` | `/private/tmp/warline-aph706-source-growth-r4.log` |
| Direct-fire unit combat | Passed `3/3` | `/private/tmp/warline-aph706-unit-combat.log` |
| Building defense | Passed `13/13`; measured allocation remained zero | `/private/tmp/warline-aph706-building-defense.log` |
| Ground missile | Passed `5/5` | `/private/tmp/warline-aph706-ground-missile.log` |
| Air missile | Passed | `/private/tmp/warline-aph706-air-missile.log` |
| Narrative menu integration | Passed `5/5` | `/private/tmp/warline-aph706-narrative-menu.log` |
| Narrative presentation | Passed `6/6` | `/private/tmp/warline-aph706-narrative-presentation.log` |
| Dependency report generation | Passed | `/private/tmp/warline-aph706-aph700-final.log` |
| Diff hygiene | Passed | `git diff --check` |

Unity imported and compiled the complete assembly graph with zero C# compiler errors during the final architecture runs. Standalone `dotnet build` attempts for the generated Unity projects did not complete and were terminated after hanging without reporting compiler errors; they are not claimed as passing evidence.

## Behavior And Risk

No gameplay, targeting, damage, queue retention, audio, VFX, UI, or visual behavior was intentionally changed. This is an assembly ownership split plus architecture-contract repair. The split is not expected to change Match FPS. Larger combat systems remain in `Game.Runtime` until their movement, audio, VFX, and building-runtime reverse dependencies are removed.

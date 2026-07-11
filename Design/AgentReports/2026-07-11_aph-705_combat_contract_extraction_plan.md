# APH-705 Combat Contract Extraction Plan

Date: 2026-07-11
Status: report-only architecture analysis; no production or assembly changes applied

## Decision

Create a future `Game.Runtime.Combat` assembly as a lower-level runtime domain that is referenced by `Game.Runtime`. Do not let it reference `Game.Runtime`.

The minimal first physical split for APH-706 should be the combat-damage observation seam:

- `CombatDamageObservationBootstrapSystem`
- `CombatDamageObservationUtility`

The seam is already closed over ECS data in `Game.Components`. Its runtime implementation has no dependency on another `Game.Runtime` type, while direct fire, building defense, and both missile paths consume the utility. Moving this seam therefore creates the intended one-way edge without a dependency cycle:

```text
Game.UI.Shell.Ecs ---------> Game.Components
Game.Runtime --------------> Game.Runtime.Combat
Game.Runtime.Combat -------> Game.Components
Game.Runtime.Pathfinding --> Game.Components
Game.Configs --------------> Game.Components
```

`Game.Runtime.Combat -> Game.Runtime` is prohibited. If that edge appears, the split must fail validation.

## Current Assembly Baseline

`Game.Runtime` currently owns 472 source files and 1,053 declared types in the latest APH-700 report. Its direct first-party dependencies are:

- `Game.Components`
- `Game.Configs`
- `Game.Runtime.Pathfinding`
- `Game.Rendering.Contracts`
- `Game.Tactical.Contracts`
- `Game.UI.Contracts`

There is no current asmdef cycle. Combat, building runtime, selection commands, audio request production, pathfinding coordination, VFX playback, and death/respawn behavior coexist inside `Game.Runtime`, so source-level bidirectional dependencies are hidden by the monolith.

The existing shared ECS data boundary is already structurally correct: `Game.Components` has no first-party assembly dependency and is consumed by runtime, UI shell ECS, configs, authoring, rendering, and tests. Combat data that crosses those domains must stay there rather than move upward into `Game.Runtime.Combat`.

## Combat Contracts And Data

### First-split shared data: retain in `Game.Components`

These types are the complete shared contract for the proposed observation split:

| Source | Types | Consumers |
|---|---|---|
| `Components/CombatDamageObservationComponents.cs` | `CombatDamageSourceKind`, `CombatDamageObservationQueueComponent`, `CombatDamageObservationElement` | combat producers, `AssistantThreatReadModelSystem`, editor validations |

They must remain in `Game.Components`. Moving them into `Game.Runtime.Combat` would force `Game.UI.Shell.Ecs` to depend on a gameplay implementation assembly and would reverse the current leaf-data architecture.

### Data required by later combat slices: retain and regroup only when useful

The following are valid shared combat data, but are not required to move for the first split:

| Source | Combat-owned types |
|---|---|
| `Components/UnitCombatComponents.cs` | `UnitCombat`, `ThreatDetectionKind`, `ThreatDetector`, `UnitHealth`, `UnitAttack`, attack trace/VFX request types, `UnitAttackCooldownComponent`, `BuildingDefenseWeapon`, `BuildingDefenseAttackSlot`, `EngageTarget`, `BaseBreachOrder`, `RecentAttacker`, `UnitTurretReference` |
| `Components/RuntimeBuildingCombatComponents.cs` | `RuntimeBuildingCombatTag`, `RuntimeBuildingCombatInfo` |
| `Components/UnitAttackOrderRequestComponents.cs` | `UnitAttackOrderRequestKind`, queue/request/result data |
| `Components/AirMissileComponents.cs` | launcher, target, support-provider, projectile, trail, visual, and impact data |
| `Components/GroundMissileComponents.cs` | launcher, projectile, interception, trail, visual, and impact data |

`UnitCombatComponents.cs` also contains display, fuel-hauling, logistics, and respawn data. A later file-only regroup may separate those concerns inside `Game.Components`, but APH-705 must not change namespaces or introduce a second data assembly merely to improve folder appearance.

### Managed building contracts that are not ready to move

`BuildingCombatUtilitySystemHelper` currently nests `RuntimeCombatState`, `IRuntimeBuilding`, `IRuntimeBuildingVisualState`, delegates, and `Context<TBuilding>`. These look like combat contracts but are coupled to:

- `RuntimeBuildingEntity`
- `RuntimeBuildingCollection`
- barrier mutation
- destroyed-building presentation
- object destruction and scene callbacks
- resource/building runtime composition helpers

Moving this helper or its nested contracts into `Game.Runtime.Combat` now would create either `Game.Runtime.Combat -> Game.Runtime` or a broad building-domain migration. Keep it in `Game.Runtime` until a separate building runtime contract boundary exists.

## Hidden Source Dependency Cycles

The monolithic assembly currently hides these cycles or reverse edges. They explain why the first split must stay narrow.

| Candidate | Current dependencies back into the monolith | Split consequence |
|---|---|---|
| `BuildingDefenseAttackSystem` | `UnitAttackSystem`, `UnitDeathSystem`, `AudioEventRequestSystem`, `GameplayAudioFeedbackSystemHelper` | Moving it alone requires `Combat -> Runtime`; runtime systems and tests also reference it, creating the reverse edge. |
| `UnitAttackSystem` | `UnitEngagedMovementSystem`, fixed-wing runway utility, audio requests, cinematic shot contract | Not a closed combat core. |
| `UnitEngagementSystem` | `DynamicOccupancyRebuildSystem`, `UnitPathfindingSystem` | Couples combat acquisition to monolithic movement/pathfinding ownership. |
| engage validation/sync | Update-order references to `UnitHealthBarSystem`, `UnitEngagementSystem`, and `UnitEngagedMovementSystem` | Attributes create compile-time type edges even when data access is clean. |
| missile systems | attack, engagement, VFX playback, audio, and missile-trail presentation types | Runtime and presentation responsibilities are interleaved. |
| attack VFX playback | `UnitAttackSystem`, missile impact systems, `UnitDeathSystem`, GameObject views | Bidirectional gameplay/presentation references. |
| building combat utility | runtime building entity/collection plus building presentation and barrier helpers | Requires a separate building boundary before combat extraction. |

These are not asmdef cycles today because both ends compile into `Game.Runtime`. They become real cycles if moved without first extracting leaf contracts or reversing dependencies.

## Minimal First Physical Split

### Assembly

Add `Assets/Game/Scripts/Systems/Combat/Game.Runtime.Combat.asmdef` with:

- first-party reference: `Game.Components`
- Unity references: `Unity.Entities`, `Unity.Mathematics`
- `autoReferenced: true`
- no reference to `Game.Runtime`, UI, composition, rendering, configs, authoring, or pathfinding

Keep `UnityEngine` available for the existing `Time.frameCount` observation field. Replacing frame count is a behavior change and is outside this split.

### Source move and namespace

Move `Systems/CombatDamageObservationSystem.cs` and its `.meta` into `Systems/Combat/Observation/`. Prefer namespace `Game.Runtime.Combat` so assembly ownership is explicit. The move must preserve the source asset GUID.

Move only:

- `CombatDamageObservationBootstrapSystem`
- `CombatDamageObservationUtility`

Do not move the observation ECS components out of `Game.Components`.

### Required dependency and call-site edits

| File | Required change |
|---|---|
| `Assets/Game/Scripts/Game.Runtime.asmdef` | Add `Game.Runtime.Combat`. |
| `Assets/Tests/Editor/Game.Tests.Editor.asmdef` | Add `Game.Runtime.Combat`. |
| `Systems/UnitAttackSystem.cs` | Import the combat namespace for `CombatDamageObservationUtility`. |
| `Systems/BuildingDefenseAttackSystem.cs` | Import the combat namespace. |
| `Systems/GroundMissileLauncherSystems.cs` | Import the combat namespace. |
| `Systems/AirMissileLauncherSystems.cs` | Import the combat namespace. |
| `Tests/Editor/CombatDamageObservationTelemetryTests.cs` | Import the combat namespace; retain `Game.Runtime` for producer systems. |
| `Tests/Editor/AssistantPerformanceDiagnosticsValidation.cs` | Import the combat namespace. |

No UI shell source edit is required: `AssistantThreatReadModelSystem` consumes only the `Game.Components` observation data.

### Resulting edge set

New edges:

- `Game.Runtime -> Game.Runtime.Combat`
- `Game.Runtime.Combat -> Game.Components`
- `Game.Tests.Editor -> Game.Runtime.Combat`

Forbidden edges:

- `Game.Runtime.Combat -> Game.Runtime`
- `Game.Runtime.Combat -> Game.Composition`
- `Game.Runtime.Combat -> Game.UI.*`
- `Game.Components -> Game.Runtime.Combat`

## Why This Is The First Split

The observation seam is small but architecturally meaningful:

- It is used by four distinct damage producers.
- It owns one bounded cross-domain event contract consumed by UI threat read models.
- Its implementation is independent of attack, targeting, movement, pathfinding, presentation, and building runtime objects.
- It establishes and validates the assembly direction needed before larger combat systems move.
- It avoids moving a hot combat loop merely to prove asmdef mechanics.

The split is not expected to improve runtime FPS. Its acceptance criteria are unchanged behavior, unchanged steady-state allocation, and a cleaner compile/dependency boundary.

## Follow-on Extraction Order

After the observation slice passes, use this order and stop whenever an edge would point back to `Game.Runtime`:

1. Extract pure combat policies from static methods only when their parameters are ECS values or explicit interfaces, not `EntityManager` access to unrelated runtime services.
2. Separate combat event production from audio and VFX presentation request creation.
3. Replace update-order references to monolithic systems with group-level ordering or lower-level marker systems where same-frame behavior is proven unchanged.
4. Move attack resolution and engagement only after movement/pathfinding dependencies are expressed as leaf contracts.
5. Move building defense only after direct-fire damage application, death checks, and feedback emission are injected or represented as ECS requests.
6. Keep `BuildingCombatUtilitySystemHelper` in the building domain until its runtime-object and presentation coupling is removed.

## Required Validation For APH-706

### Assembly and dependency gates

- Regenerate the APH-700 JSON/Markdown reports through the project generator.
- Assert `Game.Runtime.Combat` is present and owns only the moved observation source.
- Assert the three new first-party edges above exist.
- Assert no first-party cycle exists.
- Assert `Game.Runtime.Combat` has no dependency on `Game.Runtime`, composition, UI, rendering, authoring, configs, or pathfinding.
- Update assembly-count and edge-count expectations only from regenerated evidence, not hand-edited report values.
- Run the APH-700 focused validation and the APH-701/702 source-growth architecture validation.

### Compile matrix

Require zero errors for at least:

- `Game.Components`
- `Game.Runtime.Combat`
- `Game.Runtime`
- `Game.UI.Shell.Ecs`
- `Game.Composition`
- `Game.Editor`
- `Game.Tests.Editor`
- `Game.Tests.PlayMode`

### Focused behavior

- Run all nine `CombatDamageObservationTelemetryTests` cases.
- Run assistant threat/read-model tests that consume observation data.
- Run building-defense focused validation.
- Run direct-fire unit combat focused validation.
- Run ground- and air-missile focused validations.
- Prove queue singleton creation, capacity 64, newest-event retention, overflow fail-closed behavior, and no dependency of damage application on queue availability.

### Architecture and performance

- Run `EcsBurstHotPathArchitectureTests.RunFocusedValidation`.
- Confirm the bootstrap remains `ISystem`; do not introduce `SystemBase`.
- Confirm no per-frame managed allocation is added to any damage producer.
- Re-run the unchanged Match steady-state GC gate.
- Re-run the building-defense benchmark and reject regression against the accepted Phase 2 floor.
- A device APK is not required for the contract-only split unless integrated Match or visual validation changes unexpectedly.

### Lifecycle

- Menu -> Match -> Menu must create exactly one observation queue per Match world and dispose it with the world.
- Domain-reload-disabled re-entry must not retain a stale queue or static world reference.
- System discovery must include `CombatDamageObservationBootstrapSystem` in player builds.

## Risks And Controls

| Risk | Control |
|---|---|
| Namespace move causes silent missed references | Compile the full matrix and use source search for both utility and bootstrap symbols. |
| New assembly is omitted from player build | Keep it auto-referenced, retain a direct `Game.Runtime` reference, and validate system creation in Match lifecycle coverage. |
| Test assembly sees duplicate/ambiguous namespace imports | Use the explicit `Game.Runtime.Combat` namespace and add exactly one asmdef reference. |
| APH-700 baselines are manually patched | Regenerate reports and validate their fingerprint. |
| Larger combat files are pulled into the split opportunistically | Limit APH-706 to the observation source, asmdefs, imports, generated dependency evidence, tests, and tracker evidence. |

## Source Evidence Commands

Run from the repository root:

```bash
rg -n "APH-705|Phase 7|Combat/building defense" Design/Architecture/architecture_performance_hardening_implementation_tracker.md

find Assets -name '*.asmdef' -print | sort

sed -n '1,220p' Assets/Game/Scripts/Game.Runtime.asmdef
sed -n '1,220p' Assets/Game/Scripts/Components/Game.Components.asmdef
sed -n '1,220p' Assets/Game/Scripts/Composition/Game.Composition.asmdef
sed -n '1,220p' Assets/Tests/Editor/Game.Tests.Editor.asmdef

sed -n '1,240p' Assets/Game/Scripts/Systems/CombatDamageObservationSystem.cs
sed -n '1,180p' Assets/Game/Scripts/Components/CombatDamageObservationComponents.cs

rg -n "CombatDamageObservation(QueueComponent|Element|BootstrapSystem|Utility)|CombatDamageSourceKind" Assets/Game/Scripts Assets/Tests --glob '*.cs'

rg -n "BuildingDefenseAttackSystem|UnitAttackSystem|UnitEngagementSystem|EngageTargetValidateSystem|EngageTargetSyncSystem|GroundMissile|AirMissile|BuildingCombatUtilitySystemHelper" Assets/Game/Scripts Assets/Tests --glob '*.cs'

rg -n "RuntimeCombatState|IRuntimeBuilding|IRuntimeBuildingVisualState|Context<TBuilding>" Assets/Game/Scripts/Systems/BuildingCombatUtilitySystemHelper.cs

python3 -c 'import json; d=json.load(open("Design/AgentReports/2026-07-10_aph-700_first_party_assembly_dependencies.json")); print(d["summary"]); print([a for a in d["assemblies"] if a["name"] in ("Game.Runtime", "Game.Components")])'

git diff --check -- Design/AgentReports/2026-07-11_aph-705_combat_contract_extraction_plan.md
```

## Acceptance Recommendation

This report is sufficient to progress APH-705 as an architecture plan, but APH-705 should only be marked complete by the coordinator after independent review confirms the source evidence and the tracker records the decision. APH-706 must implement only the observation slice first; broader attack, missile, engagement, building-defense, VFX, audio, death, or building-runtime movement is explicitly out of scope for that first physical split.

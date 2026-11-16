# S073 tactical AI source closure experiment

This is an opt-in source-closure experiment for the current tactical producer
chain. It adds the original `AISquadSystem`, `AITargetingSystem`, and
`AICombatOrderSystem` sources to a copy of the ordinary source manifest. The
default `source_manifest.plan.json` remains unchanged.

Run a fresh isolated compile with:

```sh
rtk proxy python3 -B Tools/CI/run_skirmish_source_simulation.py build \
  --repo /Volumes/T7/Projects/WarlineCapture \
  --package-root /Volumes/T7/Projects/WarlineCapture \
  --manifest /Volumes/T7/Projects/WarlineCapture/Tools/CI/SkirmishSourceSimulation/source_manifest.s073-ai-experimental.plan.json \
  --mission S073 --seed 104729 \
  --output /private/tmp/s073-ai-closure-build-YYYYMMDD
```

Use a new output directory each time. Passing `--manifest` explicitly is the
only path to this experiment; ordinary mission/profile commands continue to
select the unchanged default manifest. The experimental manifest retains the
S073 profile variant and launch identity checks.

## Engine-boundary scope

`Adapters/S073/SourceS073TacticalAIEngineBoundary.cs` supplies only APIs needed
to compile and synchronously exercise selected original job bodies:

- `IJob` executes synchronously after completing the supplied dependency.
- `IJobChunk` is admitted only for the original nested
  `AITargetingSystem.AssignTargetsJob`, on the currently bound source manager,
  with at most 131,072 chunks. It invokes the original `Execute` body once per
  query chunk.
- `NativeReference<T>` uses a shared managed cell so writes from a copied job
  struct are observable by its caller.
- `FixedList128Bytes<T>` admits only the byte faction set used by the combat
  owner. Its managed backing aliases on struct copies, unlike Unity's value
  container; current production use only reads the copy. Other element types
  fail closed, and this approximation needs focused behavioral validation.
- Buffer and entity-storage lookups retain the bound managed `EntityManager`
  and resolve entities on each access; `Update(ref state)` only verifies the
  owner. Read-only lookup flags are not enforced, and buffer access does not
  reproduce Unity safety handles or native buffer aliasing rules.
- The `AIProductionSystem` type is a compile-time ordering marker only. It has
  no owner body and executes no production behavior.

This boundary does not reproduce Burst, parallel scheduling, Unity's native
World scheduler, enabled-mask semantics, or native container layout. It must
not be used to claim native scheduler parity or mission readiness. A future
fixture must call original owners in an explicitly declared order and report
this backend limitation.

## Current build checkpoint

The combined experimental build passed with 707 source files; its receipt and
compiler output are in `Logs/SkirmishProduction/s073-ai-closure-combined-20261101-r1/`.
It includes the original membership partial and four extracted breach-approach
methods. The earlier failed r4 build remains in
`Logs/SkirmishProduction/s073-ai-closure-20261101-r4/`. The default S073 source
manifest build also passed. No AI owner was invoked and no source runtime or
physical map state was changed by these builds. Compilation is not evidence of
autonomous play or native scheduler parity.

## S073 legacy economy/storage census

The same opt-in manifest also compiles
`Adapters/SourceS073LegacyEconomyStorageCensus.cs`. To emit its read-only census
from the retained legacy host, run with
`WARLINE_S073_LEGACY_ECONOMY_STORAGE_CENSUS=1`. It reports runtime startup
configuration readback separately from source-value expectations, all
faction economy/material rows, exact building-storage values, source identity
fields, and Campaign scenery exclusions. The diagnostic does not mutate the
source host or copy entities.

Entities without an exact matching `SkirmishAttemptOwnedComponent.SessionId`
remain candidates. Faction, prefab key, ownership faction, runtime building
ID, and origin do not alone qualify entities for disposable-copy import.
`disposable_copy_admissible` stays false unless exact session ownership and
one correctly factioned tactical-material bank per side are present. The
census is not a mission readiness result.

## Movement-owner follow-on gate

The optional `WARLINE_S073_TACTICAL_AI_MOVEMENT_OWNER_DIAGNOSTIC=1` lane also
requires `WARLINE_S073_TACTICAL_AI_RUN_OWNERS=1`,
`WARLINE_S073_TACTICAL_AI_RUN_RULES_OWNER=1`, and
`WARLINE_S073_TACTICAL_AI_TARGET_POLICY_DIAGNOSTIC=1`. It runs its preflight
around the existing 122-tick target-policy window and copies movement structs
only from the same observed source entities. It records source-derived
movement query populations and a bounded position sample. After those 122
ticks it invokes the original engage-target validation, dynamic occupancy,
acquisition, target synchronization, combat approach, pathfinding,
engaged-movement, grid-movement, and end-simulation playback owners through
the existing disposable-copy owner seam. It records unit position changes for
up to 180 updates at 1/60 second. This is an isolated post-target-policy phase:
Rules and AI owners do not continue during those 180 movement updates. The
fixture injects no targets, move orders, or admission state. Missing source
grid buffers, path state, ECB ownership, or other required inputs fail closed.
The fixture-local move-order queue is tracked for cleanup and outstanding path
work must drain within four additional owner updates. A source MapSurface blob
may be referenced by the disposable copy for the approach owner; this does not
claim physical map or surface parity. The evidence does not establish native
scheduler parity or mission readiness.

## Latest movement-owner run (2026-11-01 r4)

The latest run used the r9 experimental build and
`data-bundle-native-ai-refreshed-r17.json`. It preserved source identity and
state (`source_guard_before == source_guard_after`, `source_world_mutated=false`).
The 122-tick window called `AIFactionControlSystem` and `AISquadSystem` 122
times each; `AITargetingSystem` and `AICombatOrderSystem` each had 122 calls
with their AISquad required-query gate closed, so each executed zero updates.
After Rules, faction 2's source plan required 8 units. The post-AI census found
17 AI-controlled tags, but only 5 candidates in the exact AISquad unit query:
4 soldiers had become `SkirmishBaseDefender`, and 3 units were resource
haulers. No source squad, engage target, or path was produced. The earlier
8-unit eligibility observation from the incomplete movement-component copy
is superseded by this more complete source copy and census.

The isolated movement phase then called Dynamic Occupancy Rebuild, Unit
Engagement, Skirmish Combat Approach, Pathfinding, and end-simulation ECB
playback 180 times each at 1/60 second. Validate, target sync, engaged movement,
and grid movement each had 180 phase calls but zero actual updates because
their required queries were empty. All 24 source-derived unit positions stayed
unchanged; there were no targets or paths to move toward. No targets, orders,
or admission state were injected. The next source owner needed to change this
gate is AI production, which must produce enough eligible source units under
the existing rules and budget; the fixture does not lower the plan minimum or
force squad creation. `SkirmishSquadAssignment` creates player quick-select
membership and is not the missing AI squad producer.

The durable run artifact is
`Logs/SkirmishProduction/s073-ai-movement-owner-20261101-r4/source-run-r4/s073-tactical-ai-disposable-copy.json`;
the original temporary copy remains at
`/private/tmp/s073-ai-movement-run-20261101-r4/s073-tactical-ai-disposable-copy.json`.
The complete final experimental build, ordinary build, and run bundles are
retained under `Logs/SkirmishProduction/s073-ai-movement-owner-20261101-r4/`.
The outer run exits 2 with `PreparingPhysicalSourceRequired` because S073
physical-source validation and autonomous scheduler admission remain
incomplete. The ordinary S073 build passed with 703 sources at
`ordinary-build-r3/`; the final experimental build passed with 713 sources at
`experimental-build-r9/`. This is owner-call and
gate evidence only, not autonomous play, native scheduler parity, or mission
readiness.


## S073 original production/economy source closure (2026-11-01)

The explicit experimental manifest `source_manifest.s073-ai-experimental.plan.json`
now includes the original `AIProductionSystem` and
`AIProductionSystem.ProductionRequests` partial, original `AIEconomySystem`, the
read-only production-input census adapter, and a separate compiler-control
entrypoint. The default source manifest and normal S073 startup remain
unchanged. The host boundary provides only a type-only `AIBuildPlannerSystem`
needed by the original production ordering attribute. The actual production
owner completes the shared partial type; the former marker has no behavior.

The AI plan entry buffer path uses the existing same-manager, read-only chunk
snapshot adapter. It requires a complete entity-aligned chunk and an actual
buffer for each queried entity; writes to a snapshot do not write through.
A separate test manager checks populated and empty plan-entry buffers, alignment,
and snapshot isolation.

A standalone compiler control invokes the original `AIProductionSystem`
`OnCreate` and `OnUpdate` directly on two independent bounded synthetic
managers. Its affordable case naturally emits one pending request and spends
10 credits plus 5 tactical materials. Its no-funds case emits no request and
preserves both balances. No request is preloaded, and neither manager is bound
to or copied from the source host. The normal source simulation entrypoint is
not changed to select this control.

The production/economy closure compile passed with 717 sources at
`Logs/SkirmishProduction/s073-production-economy-closure-20261101-r6/`; earlier
failed compile attempts r1-r4 remain beside it. The standalone two-case owner
control passed with 718 sources at
`Logs/SkirmishProduction/s073-ai-production-owner-control-20261101-r2/`. Its
first run r1, which had an incorrect assertion about the material-spend tally,
is retained with failure output. These bounded synthetic checks do not establish
native scheduler parity, autonomous play, or mission readiness.

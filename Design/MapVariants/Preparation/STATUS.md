# Map preparation status

Updated: 2026-09-29. **Work remains in progress. No map foundation is qualified.**

## Scope and ownership

Starting commit: `5a1e5a15003cd18294d311b24e29aadf7b7e7a5a`.
Work ran sequentially in the existing checkout, with one owner of generation and Unity runs.
The initial resource-system/test/font changes and mission evidence were preserved. Other content-planning
documents changed concurrently and were left alone. Initial and subsequent dirty-state snapshots are in
`Evidence/starting-working-tree.txt` and `Evidence/current-working-tree.txt`.

Owned code: `MapVariantPreparationSchema.cs`, `MapVariantPreparationInventory.cs`,
`MapVariantRefineryPreparationSlice.cs`, the preparation tests/runner, and the small builder changes for
transient inventory geometry and explicit attachment provenance. Shared integration changes distinguish
resident presentation from explicitly requested virtualization in the authoring/baking contracts.
Owned generated output is limited to `GeneratedOperationMaps/Variants/RefineryDistrict/Slice`,
`Scenes/OperationMaps/Variants/RefineryDistrict/PreparationSlice.unity`, and this report directory.
LFS attributes were added for the generated ground mesh and large inventory exports.

No mission/scenario binding, production catalog, Addressables setting, progression, reward, UI or
dense-city physical source was changed by this preparation work. **469 protected files retain their
recorded bytes**, checked by `Evidence/protected-source-check.json`. Prototype scene GUIDs and art remain
the references. No scene/prefab/asset YAML was edited to perform conversion.

## Per-map gates

| Map | Status | Inventory / mapping | Prepared authoring | Destruction / surfaces | Packed runtime | Native visual / device acceptance |
|---|---|---|---|---|---|---|
| RefineryDistrict | Failed gate | Export/rebuild passed; 30 placements in 9 prefab classes lack mappings | 24-owner civilian fixture generated and replayed; full candidate pending | Baked fixture transitions passed; ridge bounds and real movement pending | Pending | Authoring comparisons inspected; native runtime and device acceptance pending |
| CityEdgeAirfield | Failed gate | Export/rebuild passed; 15 placements in 6 classes lack mappings | Pending | 3 declared zone footprints fail bounds; runway/helipad movement pending | Pending | Pending |
| AshLinePort | Failed gate | Export/rebuild passed; 29 placements in 8 classes lack mappings | Pending | 3 declared zone footprints fail bounds; crossing geometry/navigation pending | Pending | Pending |
| Frontier | In progress | Export/rebuild passed; 100 placements in 14 classes lack mappings | Full candidate deferred until medium-map pipeline is proven | Translation exported; real surfaces/routes pending | Pending | No performance measurements; no passing performance claim |

Inventory success is an export/consistency result. It does not pass the handoff's complete-classification,
navigation, native runtime, packed-content or device gates. Mission readiness remains **out of scope and
pending** for every map.

## Inventory artifacts

Each map has `inventory.json` and `REVIEW.md`. The JSON retains source GUID/local ID and dependency hash,
prefab hierarchy/branches, materials/shaders/physics, source transforms, map-scoped stable placement keys,
rotated footprint corners, rendering category, independent gameplay semantics, attachment recipes and
owner references, triangulated ground vertex heights, road cells, bridge deck data and zones.

| Map | Placements | Unique prefabs including composed children | Unresolved placements inside playable bounds | Outside full playable footprint | Explicit backdrop placements | Intentional ruins |
|---|---:|---:|---:|---:|---:|---:|
| RefineryDistrict | 11,066 | 129 | 24 | 8,587 | 7 | 72 |
| CityEdgeAirfield | 11,084 | 113 | 10 | 9,183 | 4 | 39 |
| AshLinePort | 9,492 | 116 | 28 | 7,169 | 4 | 0 |
| Frontier | 25,678 | 170 | 100 | 7,197 | 4 | 192 |

Outside counts include placements whose full footprint crosses the boundary; they are not certified
backdrop counts. The importer still needs an explicit policy for boundary-straddling obstacles.
Grass, pebbles, sand edges and low shrub dressing have nonblocking semantics independent of `Reserved`.
Large rocks, trees, cargo, fences, parked scenery vehicles and intentional wrecks have deliberate obstacle
semantics. Props such as pipeline tanks do not acquire production/resource roles.

Stable placement identity hashes map ID, base prefab GUID and source transform, with no global traversal
index. Canonical export order sorts by that identity. Every variant rebuilt twice with identical semantic
JSON and zero duplicate placement keys. Dressing does not renumber owners at an unchanged transform.

Semantic hashes:

- RefineryDistrict: `991921cbbaa27b890dbab216e8150881fb16f49da19fb740e3f5b902ce624141`
- CityEdgeAirfield: `e17e9d47fdb3e892d6b87ca6cf7ddcbf27bfe7ea7dd5a95c06f977533a0cff9e`
- AshLinePort: `0e00ab597ba89f6344e962122c83aeed5b692e1230cab858aa8e519e213fe78f`
- Frontier: `50ceeabc9425809dd1c73402f11abd2279db9932450ff285333a87936d8d181e`

### Outstanding mappings

Exact unresolved classes and instance counts are listed in each `REVIEW.md`. They include warehouses,
hangars, gas/oil towers, guard/control towers, barracks, sheds, gas station, water tower, Clock Tower and
Hall_02. The inventory did not find a validated direct alternative plus an explicit compatible definition
for these classes. None was converted to an active building owner or substituted with a house ruin.

A user question is pending: use deliberately non-destructible obstacles for unsupported industrial/
warehouse/tower classes initially, or require matching destruction assets. Continue unaffected work;
do not infer that either option has been approved from elapsed time.

Composed-child state policies are still unqualified. The exporter retains each complete recipe (including
offset/material overrides) and child GUIDs. Separately placed `PlaceOnTop` details now retain an explicit
support reference instead of relying on group numbers. The current four builds contain no separate
`PlaceOnTop` placements; most attachments are composed kit children.

## Civilian building fixture

Output manifest: `RefineryDistrict/Slice/output-manifest.json`.
Scene: `Assets/Game/Scenes/OperationMaps/Variants/RefineryDistrict/PreparationSlice.unity`.
Map identity: `opmap.skirmish.refinery_preparation_slice` (validation-only, no catalog registration).
Current artifact content hash: `9714f7dfcf6d0bcd1991052a8099287973c90b3cdde1d58d9bdd11001bbb725f`.

The 110 × 80 m source-coordinate crop contains 24 civilian buildings, including House_06's combined
roof/details and independent neighbors. It uses the existing dense-city record factory, presentation
realizer, normalization, physics stripping, definition library and `OperationMapBuildingAuthoring` baker.
Each owner is neutral, has the existing civilian house definition and has no production/resource output.
The exact source `Destroyed` branch is copied into an independent generated prefab, preserving meshes,
materials and local transforms. Normalization removes only the validated embedded sibling from the
generated intact instance. The exact original placement is removed by provenance.

The saved authoring scene retains both state roots at their full visible scale so the baker records the
correct destroyed-visible scale. The runtime destruction system initializes visibility. The `intact.png`
capture hides the alternate for authoring review; it is **not a native runtime capture**.

Automated baking exercised actual generated owners twice in fresh ECS worlds. Initial intact visibility,
one-owner damage transition, untouched neighbors, retained blockers, one resident implementation and
fresh-bake reset passed. These are automated state tests, not a normal-input mission or visual runtime
playthrough. The original artifact tested had hash
`1911c3744b7b8e3f6fdff4a80a95f10461c90fbe3d7f7f306bb87b79fd579f9c`.
Subsequent regeneration preserved all canonical owner IDs, transforms, prefab GUIDs, renderer counts and
state provenance; scene serialization changed the artifact byte hash. Final packed/native qualification
must test its exact final artifact.

The full six-case representative fixture remains incomplete: warehouse, industrial structure, composed
attachment policy, static prop and intentional ruin must be added and qualified before scaling.

### Shared baking fix

Generic building/presentation bakers emit markers in resident and virtualized modes. Previously the
virtualization baking pass required a database merely because those markers existed. The explicit
`OperationMapRenderSourceStrippingRequestBakingComponent` now identifies requested stripping before
database validation. Resident owners retain their presentation; requested stripping without a database,
or virtualized/incomplete owners without one, still reject. Baking versions were incremented. The existing
48-check virtualization suite passed after this change, along with missing-database rejection coverage.

## Bounds and surfaces

Source and runtime positions/footprints are separate fields. Medium-map translation is zero; Frontier's
translation is exactly `(-176, 0, -176)`. Consumers must not translate already-converted fields again.
The simulation remains 1 m / 2048 × 1024; these exports do not change it.

The surface helper composes ground, declared bridge decks and pad tops, rejects outside playable/grid
coordinates, and rejects water outside declared crossings. Roads alone cannot override a water error.
Bridge rendered-pivot height is exported as declared deck level + 0.08 m, matching builder code; port bridge
anchor centers consequently export y = 0.08 instead of -4. Actual bridge width, approach clearance and
agent traversal remain unverified. Runway/helipad/pier geometry is inventoried, with surface qualification
and a complete runtime surface/grid asset still pending.

All seven known zone exceptions remain explicit failed rows, retaining their original extents:
`SplitFront_Ridge`; `Airlift_Helipad`, `AirCorridor_Tower`, `Anchor_CityGate`;
`RouteReopened_Hub`, `SupplyYard_East`, `Anchor_EastCheckpoint`.
No location was silently clamped and no gameplay region was enlarged.

## Visual evidence and acceptance

Inspected `RefineryDistrict/Slice/before.png`, `intact.png` and `destroyed-state-reference.png` at matching
1920 × 1080, 51.6° pitch / 55° FOV coordinates. The before frame shows co-located destroyed alternatives;
the intact frame removes that overlap. The destroyed reference removes House_06's unsupported intact
roof/tower geometry while retaining the neighboring buildings. This isolated authoring crop omits roads,
dressing and refinery landmarks. It does not establish map identity/readability or player acceptance.

Native prepared-map top-down/battle-camera frames, moving units, real damage, bridges, packed
load/unload/reload/old-map switching, target-device budgets and measurements remain pending. No device
performance threshold was invented or weakened.

## Reproduction and validation evidence

Keep Unity Hub open and signed in. All runs used GUI licensing through the checked macOS wrapper;
no direct Editor invocation, batchmode, IPC reset or process termination was used.

```bash
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/map-preparation-new.log -- \
  -quit -executeMethod MapVariantPreparationValidation.Run
```

This entry point exports all inventories, builds/replays the civilian slice, then runs focused preparation
and shared regressions. Require all four per-map inventory pass markers, the aggregate inventory marker,
the slice marker and `[MapPreparationValidation] result=Passed`; a nonzero exit, timeout, missing marker,
baking error or failed sub-suite fails the run. Preserve each full log.

Actual commands, candidate hashes, marker checks and exit evidence are in `Evidence/runs.json`.
Source hashes and current tracked diff are in `Evidence/code-identity.json` and
`Evidence/preparation-tracked.diff`. Early runs did not capture a full source snapshot before execution;
their logs and dirty starting revision remain, and this limitation must not be presented as exact historical
diff identity. The later validation and replay have source-hash evidence.

Failed evidence retained: first slice used an invalid map namespace; first combined validation exposed
the resident database error and an exact-float assertion; next validation demonstrated that generic
render-row markers also occur in resident mode. The final focused validation passed 8 preparation checks,
2 record-factory checks, 5 attachment cases, 3 presentation-realizer checks, 4 destruction checks and
48 virtualization checks. Slice regeneration then passed deterministic owner replay.

## Next work and rollback

1. Resolve the pending unsupported-structure policy and exact mappings; leave unresolved classes inactive.
2. Complete the representative Refinery fixture's required cases, including composed attachments and
   explicit non-destructible or matching-destruction warehouse/industrial choices.
3. Resolve bounds intentionally, build real surface/grid restrictions, avoid generic/building double
   blockers, and validate routes with actual infantry/vehicle footprints and turning clearance.
4. Generate full Refinery authoring/entity-presentation derivatives, convert repeated dressing, then prove
   additive isolated packed loading, ownership, lighting, unloading and switching.
5. Complete Airfield and Port with the proven pipeline. Keep full Frontier deferred and performance gates
   visible. Publish qualified physical-source manifests before subsequent mission migration.

Rollback consists of reverting only the owned source changes and removing the listed new preparation
assets/reports if this work is abandoned. Do not reset/clean the checkout, touch unrelated content changes,
remove prototype/vendor assets or alter production bindings. No production rollback switch is needed
because the fixture was never registered in production.

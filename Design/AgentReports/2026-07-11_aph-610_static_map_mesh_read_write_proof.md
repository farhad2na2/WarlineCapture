# Static-Map Mesh Read/Write Proof

Date: 2026-07-11
Tracker item: `APH-610`
Result: proof complete; no additional importer is safe to change under the accepted runtime architecture.

## Decision

The static-map presentation manifest contains `17,564` renderer sources, `1,186` unique mesh sub-assets, and `830` FBX importers. Direct manifest/meta inspection found:

| Importer state | Count |
|---|---:|
| Read/Write already disabled | `758` |
| Read/Write enabled and retained | `72` |
| Unclassified manifest importers | `0` |

No importer was changed. The 758 unreadable importers already render through the accepted presentation path, as confirmed by the Android ownership run and ten-minute visual soak. The remaining 72 are a conservative hold set because at least one accepted runtime path can require CPU mesh data.

## Runtime Contracts

The valid Android presentation path does not read vertex or index buffers. It validates renderer state, mesh/material identity, and bounds before suppressing canonical renderers in `StaticMapPresentationOwnership`.

Read/Write remains required by these accepted paths:

1. A missing or stale presentation manifest deliberately enters legacy static-map batching. Eligibility checks `mesh.isReadable`, and batching calls `Mesh.CombineMeshes`.
2. Runtime roads cache imported road geometry and call `Mesh.CombineMeshes` while building road variants and chunks.
3. Runtime decorations use `CombinedMeshUtility`, which rejects unreadable source meshes before calling `Mesh.CombineMeshes`.

Relevant owners:

- `Assets/Game/Scripts/Rendering/StaticMapPresentationOwnership.cs`
- `Assets/Game/Scripts/Rendering/StaticMapChunkBatchingPolicy.cs`
- `Assets/Game/Scripts/Rendering/StaticMapChunkBatchingPresentationSystemHelper.cs`
- `Assets/Game/Scripts/Systems/RoadBuildStartupSystem.cs`
- `Assets/Game/Scripts/Systems/RoadVisualVariantSystem.cs`
- `Assets/Game/Scripts/Systems/RoadChunkVisualSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerPresentationSystemHelper.cs`
- `Assets/Game/Scripts/Utilities/CombinedMeshUtility.cs`

Editor-only mesh reads do not justify player Read/Write by themselves. `StaticMapMeshReadbackUtility` can copy an unreadable mesh through `MeshUtility.AcquireReadOnlyMeshData`, and its fidelity tests already cover that path.

## Retained Importers

The 72 readable manifest importers are retained as the explicit fail-closed set:

- Dirt roads: corner, exits 01-02, slope up/down, and straight.
- Flowers 01-05 and grass 01-04.
- Ground hills, flat hills, round/square ground, white ground, and grass circle/square variants.
- Mountains 01-07.
- Road, road edge, road lights, runway, sidewalk corners, and sidewalk straight.
- Rocks 01-06, rock archway, and flat rocks 01-02.
- Ruins 01-04.
- Sand dunes 01-03 and sand edges 01-06.
- Downpipe connectors, corner, end, and straight pieces.

Three of these are reachable from both Menu and Match in the accepted content-residency inventory; the other 69 are Match-rooted. Build-scene reachability alone is not sufficient to disable them because CPU consumers execute inside Match and the legacy fallback remains a production path.

## Acceptance Boundary

`APH-610` is complete because:

- valid Android manifests suppress all `17,564` canonical renderers and skip runtime static-map combination;
- invalid manifests still fail closed to visible canonical renderers and legacy batching;
- `758 / 830` map importers already avoid CPU copies;
- every remaining readable importer has a documented runtime or fallback reason to remain readable;
- no speculative importer change, stale-manifest interval, or road/decoration regression was introduced.

Further reduction requires first replacing or removing the legacy CPU-combine fallback and proving that road, decoration, collider, and editor workflows no longer consume the candidate meshes. That is not part of this accepted closeout.

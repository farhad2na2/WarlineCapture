# APH-603 Static Map Presentation Bake

Date: 2026-07-10
Baseline: `554ea2c5a`
Task: `APH-603`

## Result

The canonical Match scene now has an editor-only shared-mesh presentation baker. The baker creates additive scenes under `Assets/Game/GeneratedStaticMapPresentation/Scenes/` and records their ownership in `StaticMapPresentationManifest.asset`.

The generated scenes contain only `Transform`, `MeshFilter`, and `MeshRenderer` presentation objects. Meshes and materials remain references to existing project assets; no generated mesh or material asset is produced. Canonical `Match.unity`, map-surface authoring filters, colliders, scripts, and authored transforms are unchanged.

## Metrics

| Metric | Value |
|---|---:|
| Canonical renderers scanned | 21,226 |
| Included shared-renderer sources | 17,564 |
| Overlay-source entries retained in canonical scene | 17,564 |
| Additive chunk scenes | 525 |
| Chunk size | 32 m |
| Generated output size | 63 MB |
| Manifest size | 14 MB |
| Excluded authoring renderers | 2,312 |
| Excluded material-layout/render-queue renderers | 207 |
| Excluded inactive/other renderers | 1,143 |
| Generated mesh assets | 0 |
| Generated material assets | 0 |
| Duplicate source IDs | 0 |

Manifest content hash: `05da13c7d8593ee269ad730856904213`.

## Validation

- Unity compilation completed with zero C# errors.
- `[AndroidVisualQualityValidation] result=Passed tests=12`.
- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
- `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
- Every manifest chunk path resolves to one of 525 generated scenes.
- Generated scenes contain zero `MonoBehaviour`, collider, prefab-instance, embedded mesh, embedded material, or nonzero static-batching flag records.
- `Assets/Game/Scenes/Match.unity` remained clean.
- `git diff --check` passed.

## Remaining Work

`APH-604` owns deterministic rebake comparison and manifest-owned stale scene cleanup. `APH-605` owns full source/chunk structural equivalence and renderer-state validation. No generated scene is wired into Android builds or runtime loading in this slice.

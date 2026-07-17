# Static Map Presentation Namespaced Scene Files

Date: 2026-07-17
Result: Passed

## Scope

Generated chunk scene filenames now include the validated operation-map identity. The current map uses:

`StaticMapPresentation_opmap_skirmish_desert_base_01_chunk_<x>_<z>.unity`

`StaticMapPresentationOutputPathContract` derives the prefix from `operationMapId`; the baker no longer owns one global scene-name prefix.

## Migration

`StaticMapPresentationBaker.NamespaceCurrentSceneFiles` moved all `514` chunk scenes with `AssetDatabase.MoveAsset`, retaining each `.meta` GUID. The transaction journal included legacy source paths, namespaced targets, manifest, and integrity ledger. Foreign folders and malformed scene names remain rejected.

The manifest and integrity ledger were updated to namespaced paths. Chunk ids, source records, content hash, scene bytes, and meta bytes were unchanged.

## Validation

- Scene-prefix contract and compile: `12 / 12` passed.
- Structured migration: `514` chunks, `514` moves, result passed.
- Pre/post blob comparison: `1,028 / 1,028` chunk scene/meta files byte-identical.
- Canonical bake: `16,542` sources, `514` chunks, `0` scene writes, `0` stale deletes.
- Affected ownership/transaction/structural/Android/baseline suite: `95 / 95` passed.
- Ownership evidence, architecture gates, final diff checks, and repeat no-op bake are recorded in the tracker validation log.

No runtime loader, Addressables configuration, source map scene, authored placement, or additional physical map changed.

# Static Map Presentation Staged Bake Checkpoint

Date: 2026-07-18
Result: Passed, not published

## Scope

Added an explicit editor/batch entry point for baking the current static-map
presentation from the staged operation-map scene:

`StaticMapPresentationBaker.BakeCurrentStagedOperationMapPresentation`

The command uses the existing map-scoped input and transactional rollback owner.
It does not add a runtime loop or alter the compatibility bake command.

## Evidence

- Focused staged-input validation: `15 / 15` passed.
- Staged bake: `16,542` sources, `514` chunks, `514` scene writes,
  `0` stale deletes.
- Canonical scene:
  `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity`.
- Canonical scene GUID: `ca1f2d7f265d8495f8c815441d68fda0`.
- Staged content hash: `667418e5a560e053b1487960764ccdd1`.
- Deterministic second bake: `reusedScenes=1`, `scenesWritten=0`,
  `staleScenesDeleted=0`, same content hash.
- Exhaustive structural validation: `3 / 3` passed across all generated
  chunks and canonical renderer records.
- Staged scene/reference/placement parity after authoritative definition
  regeneration: `10 / 10` passed.
- Unity compilation reported zero C# compiler errors.

Logs are local validation artifacts under `/private/tmp/opmap-staged-*.log`.

## Publication Boundary

The staged manifest and chunk changes were deliberately restored after
validation. Publishing them before the Addressables source-scene loader and
thin-Match cutover would make the active compatibility runtime and Android
resolver consume staged GlobalObjectIds while `Match.unity` still owns the
canonical renderers.

The Phase 4 bake checkbox therefore remains open. At atomic cutover, rerun this
command, retain the staged outputs, validate the loader and stripped Match
shell, then publish the scene, manifest, definitions, and runtime binding
together.

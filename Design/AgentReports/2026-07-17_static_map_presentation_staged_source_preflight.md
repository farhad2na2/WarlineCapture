# Static Map Presentation Staged Source Preflight

Date: 2026-07-17

## Scope

The static presentation baker can now validate both supported source ownership routes for `opmap.skirmish.desert_base_01`:

- the live compatibility source through exactly one `MatchSceneView`;
- the extracted source through exactly one validated `OperationMapSceneView`.

The staged route binds its serialized map root and requires the accepted exact `Map/Buildings` and `Map/Vehicles` authoring roots before source collection. It rejects mixed, missing, duplicate, mismatched-map, or malformed scene views. Gameplay and composition policy still select maps by typed id rather than scene path.

This slice intentionally did not run a bake or modify the live manifest, integrity ledger, generated chunks, scenes, Addressables settings, or map assets. Publishing staged output remains an atomic-cutover task because replacing the compatibility manifest early would invalidate the current Match renderer-suppression and Android dependency contract.

## Validation

- `StaticMapPresentationBakeInputTests`: 15/15 passed against the real compatibility and staged scenes.
- `NonEcsSystemConversionArchitectureTests`: 9/9 passed.
- `dotnet build Game.Editor.csproj --no-restore`: zero errors.
- Unity 6000.5.2f1 compile: zero compiler errors.
- `git diff --check`: passed.
- NUnit XML: `/private/tmp/opmap-staged-bake-source-tests.xml`.
- Unity log: `/private/tmp/opmap-staged-bake-source-tests.log`.

The Phase 4 staged-bake checkbox remains open until candidate publication, source parity, and the shell cutover can be validated without breaking the live compatibility route.

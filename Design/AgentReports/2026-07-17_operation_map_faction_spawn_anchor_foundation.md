# Operation Map Faction Spawn Anchor Foundation

Date: 2026-07-17

## Scope

Implemented the loader-neutral runtime contract for faction deployment/spawn anchors without inventing transforms for the current compatibility map.

## Runtime Contract

- `OperationMapMetadataUtility.TryResolveActiveFactionAnchorCell` resolves only exact typed `Deployment` or `Spawn` records by faction and lane.
- The lookup validates active-map identity/generation, grid metadata, finite position, uniqueness, and grid bounds.
- `InitialFactionSpawnCellSystem` prefers a lane-neutral deployment anchor, then a lane-neutral spawn anchor.
- Missing typed anchors preserve the current baked/config compatibility fallback.
- Invalid, duplicate, or out-of-grid typed anchors fail startup rather than silently selecting a fallback.
- The lookup runs only during startup composition and introduces no update-loop owner or per-frame allocation path.

## Current-Map Completion

The current compatibility definition now contains deterministic typed deployment anchors authored from the exact canonical faction-volume transforms. The committed metadata blob resolves both faction initial-spawn cells, closing the remaining current-map dependency. See `2026-07-17_operation_map_current_faction_spawn_and_build_manifest.md`.

## Validation

- `InitialFactionSpawnCellSystemTests.RunFocusedValidation`: passed 5/5.
- `AIStartupSystemValidationTests.RunFocusedValidation`: passed 1/1.
- Camera/minimap ownership evidence regenerated twice byte-identically: SHA-256 `aa1a042817e23478acce598ed30218b01f446034b9461fe74edd153c026073dc`.
- `ProductionSourceGrowthArchitectureTests.RunFocusedValidation`: passed 17/17.
- `NonEcsSystemConversionArchitectureTests.RunFocusedValidation`: passed 9/9.
- `Aph805MenuMatchMenuLifecyclePlayModeTests`: passed 1/1; current map compatibility fallback remains functional.
- Unity script compilation: passed with no C# errors.
- `git diff --check`: passed.

Logs:

- `/private/tmp/opmap-faction-spawn-anchor-focused.log`
- `/private/tmp/opmap-faction-spawn-ai-startup.log`
- `/private/tmp/opmap-faction-camera-evidence.log`
- `/private/tmp/opmap-faction-camera-evidence-2.log`
- `/private/tmp/opmap-faction-source-growth.log`
- `/private/tmp/opmap-faction-naming.log`
- `/private/tmp/opmap-faction-menu-match-menu.log`

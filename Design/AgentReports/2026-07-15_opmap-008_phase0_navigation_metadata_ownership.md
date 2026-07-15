# Operation Map Phase 0 Navigation Metadata Ownership Evidence

Date: 2026-07-15
Task: `opmap-008`
Branch: `codex/opmap-008-phase0-navigation-metadata-ownership`
Baseline: `2873e4dd7b2f1f5a5727c1de81bd9c86f97dc60d`
Workflow: pull request; this evidence does not merge or claim tracker acceptance.

## Scope

`Game.Editor.OperationMapPhase0NavigationMetadataOwnershipProbe` performs a deterministic read-only inspection of exact GUID/local-file identities for the Match map surface authoring and data, MatchSubScene grid authoring/config, baked terrain/road/bridge metadata, static/dynamic blocker and occupancy authorities, airport runway metadata/endpoints, helipad definition/spawn metadata, and 13 runtime consumers. It does not save scenes, rebake data, invoke generators, or modify runtime/config content.

The exact task allowlist is the probe and test `.cs`/`.meta` pairs plus this `.json`/`.md` pair.

## Result

Status: `NeedsDecision`

The surface and grid agree on origin `(0,0,0)`, cell size `1`, and dimensions `2048x1024`. The payload contains 2,097,152 surfaces: 1,760,186 terrain samples, 126,986 road-flagged samples, 6,933 bridge-flagged samples, and 209,980 samples with no movement mask. The grid config has zero legacy blocked cells; runtime static blockers project through `StaticGridBlockerUpdateSystem`, dynamic blocker storage is owned by `DynamicBlockerInitSystem`, and moving-unit occupancy is owned by `DynamicOccupancyRebuildSystem`.

The airport runway, `Runway_Start`, and `Runway_End` are pinned by prefab GUID and transform local IDs. `BuildingRunwaySystem` owns runway geometry resolution. The helipad definition is pinned by asset GUID and exposes three exact production prefab identities; its `Spawn_01` transform is pinned by prefab GUID/local ID. Runtime spawn consumers use `BuildingFactionProductionSpawnPointReadModel` and typed runway/air state.

All 15 authority records remain `NeedsDecision`. The operation map architecture, navigation gameplay, and air operations owners must decide migration into the operation map definition, preserve the GUID/local-ID contracts, and prove runtime parity before removing Match compatibility ownership.

## Prior Evidence

The probe cross-references rather than rescans accepted evidence:

- `opmap-002` revision `996e460029730a69832bc8df81255a1892f1bca9`, SHA-256 `d4d4674850766c5cd95e1bb5fbb6f26893e0bb019dbaf266a0c9897a3befc807`.
- `opmap-004` revision `2069aa01f66040f34fa0fb48ea1d8fec41691bab`, SHA-256 `e1080bd90e88140d8151755b7ef6086c02d8683b7d277708004797893fc3c49b`.
- `opmap-006` revision `47c84afc5f873dbf2ea665ab4875d0825b51efd8`, SHA-256 `115270bdb5844b5df504f33b5796caa4c85c49e82f02d23ea05e5ce732d0f759`.

## Determinism And Guardrails

- Schema: `warline.operation-map.phase0-navigation-metadata-ownership`, version `1`; unknown fields are rejected at every object level.
- Pinned canonical identity payload SHA-256: `f330f6914b7f48563625a083b3925bf7cee2d3490e7589a56372058338105de3`.
- Committed JSON SHA-256: `9dbd050451a5295ca0f95356b2385e2c9e6b8204a87e61d90b0cf3c3c7a5a847`.
- Output is absolute/external-only and rejects project-relative, project-contained, or symlinked path components.
- Publication invalidates stale output, uses a same-directory unique `CreateNew` temporary file, flushes and revalidates bytes, rejects output races, atomically renames, and removes failure artifacts.
- Direct inputs are pinned and hashed before/after inspection. Records and references use ordinal total ordering and contain no timestamp, worktree path, Unity version, session ID, or instance ID.

## Validation

Validation uses Unity `6000.5.2f1`:

- Focused EditMode suite: passed `32/32`; `/private/tmp/opmap008-focused.xml`.
- Two independent probe runs and the committed report are byte-identical at `34,221` bytes, each with SHA-256 `9dbd050451a5295ca0f95356b2385e2c9e6b8204a87e61d90b0cf3c3c7a5a847`.
- `NonEcsSystemConversionArchitectureTests.RunFocusedValidation`: passed `9` checks; `/private/tmp/opmap-008-gate-non-ecs.log`.
- `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`: passed `31` checks; `/private/tmp/opmap-008-gate-assembly-boundary.log`.
- `ScriptArchitectureAlignmentContractTests.RunBroadShellValidation`: passed `1` check; `/private/tmp/opmap-008-gate-broad-shell.log`.

Unity emitted each success marker and completed its shutdown sequence. Where the batch process remained after shutdown, only the completed opmap-008 process was terminated; no validation was retried after a marker.

## Known Limits

This evidence does not migrate ownership, alter pathfinding, rebuild map-surface data, exercise Android/device runtime behavior, or close a Phase 0 tracker checkbox. No tracker, scene, prefab, config, generated output, manifest, project setting, or package file is modified.

# Operation Map Phase 0 Navigation Metadata Ownership Evidence

Date: 2026-07-15
Task: `opmap-008`
Integration: serial direct-main shared-foundation slice
Baseline: `75ed0c9d6922020d264a5ad77662c955c07fc30e`
Workflow: validated direct-main commit; this evidence does not claim tracker acceptance.

## Scope

`Game.Editor.OperationMapPhase0NavigationMetadataOwnershipProbe` performs a deterministic read-only inspection of exact GUID/local-file identities for the Match map surface authoring and data, MatchSubScene grid authoring/config, baked terrain/road/bridge metadata, static/dynamic blocker and occupancy authorities, airport runway metadata/endpoints, helipad definition/spawn metadata, and 15 exact compiled runtime consumer types/members. It does not save scenes, rebake data, invoke generators, or modify runtime/config content.

The evidence implementation is limited to the probe and test `.cs`/`.meta` pairs plus this `.json`/`.md` pair. Direct-main integration also updates the operation-map tracker with one validation-log row; it does not change tracker progress or close a checkbox.

## Result

Status: `NeedsDecision`

The surface and grid agree on origin `(0,0,0)`, cell size `1`, and dimensions `2048x1024`. The payload contains 2,097,152 surfaces: 1,760,186 terrain samples, 126,986 road-flagged samples, 6,933 independently counted `BridgeDeck` samples, 6,933 bridge-flagged samples, and 209,980 samples with no movement mask. The probe fails if bridge type and flag counts diverge. The grid config has zero legacy blocked cells; runtime static blockers project through `StaticGridBlockerUpdateSystem`, dynamic blocker storage is owned by `DynamicBlockerInitSystem`, and moving-unit occupancy is owned by `DynamicOccupancyRebuildSystem` and consumed by `UnitGridMovementSystem`.

The airport runway, `Runway_Start`, and `Runway_End` are pinned by prefab GUID and transform local IDs. `BuildingRunwaySystem` owns runway geometry resolution, and `FixedWingRunwayHomeInitializationSystem.OnUpdate` is recorded as an exact compiled consumer. The helipad definition is pinned by asset GUID and exposes three exact production prefab identities; its `Spawn_01` transform is pinned by prefab GUID/local ID. Runtime spawn consumers use `BuildingFactionProductionSpawnPointReadModel` and typed runway/air state.

The 15 authority records now have explicit typed classifications: `7 MapOwned`, `4 SharedConfig`, `3 Mixed`, and `1 Unresolved`. Map-owned and shared-config rows are accepted evidence with `MoveWithOperationMap` or `RemainSharedConfig` dispositions. Only the four Mixed/Unresolved rows use `DecisionRequired`, so the report truthfully remains `NeedsDecision` without treating every authority as undecided.

## Prior Evidence

The probe cross-references rather than rescans accepted evidence:

- `opmap-002` revision `996e460029730a69832bc8df81255a1892f1bca9`, SHA-256 `d4d4674850766c5cd95e1bb5fbb6f26893e0bb019dbaf266a0c9897a3befc807`.
- `opmap-004` revision `2069aa01f66040f34fa0fb48ea1d8fec41691bab`, SHA-256 `e1080bd90e88140d8151755b7ef6086c02d8683b7d277708004797893fc3c49b`.
- `opmap-006` revision `47c84afc5f873dbf2ea665ab4875d0825b51efd8`, SHA-256 `115270bdb5844b5df504f33b5796caa4c85c49e82f02d23ea05e5ce732d0f759`.

## Determinism And Guardrails

- Schema: `warline.operation-map.phase0-navigation-metadata-ownership`, version `2`; unknown fields are rejected at every object level.
- Pinned canonical identity payload SHA-256: `3a4ac48efcd1c8b46e958656ffed1dbcf70b4c11fa402cc1ed890b474a3b7acc`.
- Committed JSON SHA-256: `7eedc22455866caf6e3771e6165a6aa7b3d39f38602088f045529ad3014300f1`.
- Output is absolute/external-only and rejects project-relative, project-contained, or symlinked path components.
- Publication invalidates stale output, uses a same-directory unique `CreateNew` temporary file, flushes and revalidates bytes, rejects output races, atomically renames, and removes failure artifacts.
- Direct inputs are pinned and hashed before/after inspection. Records and references use ordinal total ordering and contain no timestamp, worktree path, Unity version, session ID, or instance ID.

## Validation

Validation uses Unity `6000.5.2f1`:

- Focused EditMode suite: passed `36/36`; `/private/tmp/opmap008-shared-v2-focused.xml`.
- Two independent schema-v2 probe runs and the committed report are byte-identical at `34,196` bytes, each with SHA-256 `7eedc22455866caf6e3771e6165a6aa7b3d39f38602088f045529ad3014300f1`.
- `NonEcsSystemConversionArchitectureTests.RunFocusedValidation`: passed `9` checks; `/private/tmp/opmap008-gate-non-ecs.log`.
- `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`: passed `31` checks; `/private/tmp/opmap008-gate-assembly.log`.
- `ScriptArchitectureAlignmentContractTests.RunBroadShellValidation`: passed `1` check; `/private/tmp/opmap008-gate-broad-shell.log`.

A broader diagnostic run of both architecture test classes reported seven unrelated pre-existing policy failures; it is retained at `/private/tmp/opmap008-shared-architecture.xml` and was not treated as acceptance evidence for this narrow editor probe.

Unity emitted each success marker and completed its shutdown sequence. Where the batch process remained after shutdown, only the completed opmap-008 process was terminated; no validation was retried after a marker.

## Known Limits

This evidence does not migrate ownership, alter pathfinding, rebuild map-surface data, exercise Android/device runtime behavior, or close a Phase 0 tracker checkbox. Apart from the tracker validation-log row, no scene, prefab, config, generated output, manifest, project setting, or package file is modified.

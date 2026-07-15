# Operation Map Phase 0 Placement Ownership Evidence

Date: 2026-07-15
Task: `opmap-006`
Branch: `codex/opmap-006-phase0-placement-ownership`
Baseline: `98cfe8cedb3c7d18a14819759bb0d5e51c202264`
Workflow: pull request; this evidence does not merge or claim tracker acceptance.
Rebased onto: `2a8940fa5b646a242460a965e3a91945e9a3fb34` (`origin/main` at validation).

## Scope

`Game.Editor.OperationMapPhase0PlacementOwnershipProbe` performs a read-only inspection of the current building and vehicle placement bindings in `Assets/Game/Scenes/Match.unity`. It verifies the bound config assets, authoring roots, every ordered placement identity, all seven runtime consumers, and 16 direct inputs without invoking a baker, saving a scene, or changing generated presentation assets.

The exact task allowlist is:

- `Assets/Game/Scripts/Editor/OperationMapPhase0PlacementOwnershipProbe.cs`
- `Assets/Game/Scripts/Editor/OperationMapPhase0PlacementOwnershipProbe.cs.meta`
- `Assets/Tests/Editor/OperationMapPhase0PlacementOwnershipProbeTests.cs`
- `Assets/Tests/Editor/OperationMapPhase0PlacementOwnershipProbeTests.cs.meta`
- `Design/AgentReports/2026-07-15_opmap-006_phase0_placement_ownership.json`
- `Design/AgentReports/2026-07-15_opmap-006_phase0_placement_ownership.md`

## Result

Status: `NeedsDecision`

The probe reports exactly 451 building placements and 29 vehicle placements. Building placements resolve through `Map[10]/Buildings[18]`; vehicle placements resolve through `Map[10]/Vehicles[20]`. The 480 ordered identities preserve config asset, prefab path/GUID/local ID/type, source-group reference and resolution, faction, occurrence, transform, yaw, and rotation metadata.

Source candidates are represented once per source-path group instead of repeated as entry-level claims. Buildings contain 348 groups, including 49 duplicate-path groups covering 152 entries. Vehicles contain 24 groups, including 5 duplicate-path groups covering 10 entries. Those 54 groups and 162 entries are `Unresolved`; entry ownership is `Mixed`, source hiding is `Unresolved`, and the decision owner is the operation map architecture owner and gameplay placement owner. Singleton groups resolve to one indexed hierarchy path.

Both placement configs are currently owned by the Match scene compatibility binding through `Game.Composition.MatchSceneView`. Their target owner is the operation map definition. The operation map architecture owner and gameplay placement owner must decide and own migration of each config together with its authoring hierarchy, preserving the config `.meta` GUID and proving placement identity parity before removing compatibility fields.

This result deliberately does not claim `Passed` and does not close a Phase 0 tracker checkbox.

## Determinism And Guardrails

- Output schema: `warline.operation-map.phase0-placement-ownership`, version `1`.
- Committed JSON SHA-256: `115270bdb5844b5df504f33b5796caa4c85c49e82f02d23ea05e5ce732d0f759`.
- Pinned canonical placement identity/payload SHA-256: `76859d5eaadb49b9a05d494b34d7232ff6bbe6c0a710620f42996b513fb4317a`.
- Building identity aggregate: `87a26e3d33214e942e0075e461d66a91a45e0735bfe51455bb140c695149f65b`; vehicle identity aggregate: `9d2ec4c8c563e7692efe51d3fd879bcf2d9ff2df015cf41730f11a6b75c4a065`.
- Two real Unity probe runs produced byte-identical 1,428,140-byte JSON, and both outputs are byte-identical to the committed JSON.
- Probe logs: `/private/tmp/opmap-006-revision-run1.log` and `/private/tmp/opmap-006-revision-run2.log`.
- Probe outputs: `/private/tmp/opmap006-revision-run1.json` and `/private/tmp/opmap006-revision-run2.json`.
- Both probe logs report `result=NeedsDecision buildings=451 vehicles=29 needsDecision=2`.
- Paths in JSON are repository-relative; no timestamp, worktree path, Unity version, output path, session ID, or instance ID is recorded.
- Existing report and temporary output are invalidated before validation; publication validates before and after the same-directory temporary write and fails closed.
- Direct inputs are hashed before and after inspection. Loaded saved-scene setup is restored in `finally`.

## Validation

Validation used Unity `6000.5.2f1` with logs and test results under `/private/tmp`.

- Focused EditMode tests: `54 / 54` passed, zero failed, skipped, or inconclusive; `/private/tmp/opmap-006-revision-focused.xml`, log `/private/tmp/opmap-006-revision-focused.log`.
- `NonEcsSystemConversionArchitectureTests.RunFocusedValidation`: passed (`9` checks); `/private/tmp/opmap-006-revision-gate-non-ecs.log`.
- `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`: emitted `result=Passed tests=31`, then Unity crashed during shutdown with exit `139`; `/private/tmp/opmap-006-revision-gate-assembly-boundary.log`. Per the no-loop crash rule, this command was not retried.
- `ScriptArchitectureAlignmentContractTests.RunBroadShellValidation`: passed (`1` check); `/private/tmp/opmap-006-revision-gate-broad-shell.log`.
- The two real probes were rerun after the rebase and implementation changes.
- `git diff --check` and exact six-file allowlist validation passed before commit.

## Known Limits

- The assembly-boundary assertions completed and emitted their passing marker, but a clean Unity process exit for that command is unproven because of the post-marker shutdown crash.
- This read-only editor evidence does not exercise Android builds, device runtime behavior, placement migration, operation-map switching, or removal of the current Match scene compatibility bindings.
- No scene, config, manifest, integrity ledger, generated chunk, project setting, package manifest, or runtime source is modified.

# Operation Map Phase 0 Placement Ownership Evidence

Date: 2026-07-15
Task: `opmap-006`
Branch: `codex/opmap-006-phase0-placement-ownership`
Baseline: `98cfe8cedb3c7d18a14819759bb0d5e51c202264`
Workflow: pull request; this evidence does not merge or claim tracker acceptance.

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

The probe reports 451 building placements and 29 vehicle placements. Building placements resolve through `Map[10]/Buildings[18]`; vehicle placements resolve through `Map[10]/Vehicles[20]`. The 480 ordered identities preserve config asset, prefab, source hierarchy, faction, occurrence, transform, yaw, and rotation metadata.

Both placement configs are currently owned by the Match scene compatibility binding through `Game.Composition.MatchSceneView`. Their target owner is the operation map definition. The operation map architecture owner and gameplay placement owner must decide and own migration of each config together with its authoring hierarchy, preserving the config `.meta` GUID and proving placement identity parity before removing compatibility fields.

This result deliberately does not claim `Passed` and does not close a Phase 0 tracker checkbox.

## Determinism And Guardrails

- Output schema: `warline.operation-map.phase0-placement-ownership`, version `1`.
- Committed JSON SHA-256: `ec98a594d77436297dda41e119dd1769c139ccf98500d7a9772b7098aba4af21`.
- Two real Unity probe runs produced byte-identical 1,079,793-byte JSON, and both outputs are byte-identical to the committed JSON.
- Probe logs: `/private/tmp/opmap-006-placement-run1.log` and `/private/tmp/opmap-006-placement-run2.log`.
- Probe outputs: `/private/tmp/opmap006-real-run1.json` and `/private/tmp/opmap006-real-run2.json`.
- Both probe logs report `result=NeedsDecision buildings=451 vehicles=29 needsDecision=2`.
- Paths in JSON are repository-relative; no timestamp, worktree path, Unity version, output path, session ID, or instance ID is recorded.
- Existing report and temporary output are invalidated before validation; publication validates before and after the same-directory temporary write and fails closed.
- Direct inputs are hashed before and after inspection. Loaded saved-scene setup is restored in `finally`.

## Validation

Validation used Unity `6000.5.2f1` with logs and test results under `/private/tmp`.

- Focused EditMode tests: `30 / 30` passed, zero failed, skipped, or inconclusive; `/private/tmp/opmap-006-final-focused.xml`, log `/private/tmp/opmap-006-final-focused.log`.
- `NonEcsSystemConversionArchitectureTests.RunFocusedValidation`: passed (`9` checks); `/private/tmp/opmap-006-final-gate-non-ecs.log`.
- `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`: emitted `result=Passed tests=31`, then Unity terminated during shutdown with `Trace/BPT trap` and wrapper exit `133`; `/private/tmp/opmap-006-final-gate-assembly-boundary.log`. Per the no-loop crash rule, this command was not retried.
- `ScriptArchitectureAlignmentContractTests.RunBroadShellValidation`: passed (`1` check); `/private/tmp/opmap-006-final-gate-broad-shell.log`.
- The existing real-run logs were sufficient for byte-identical proof, so the probe was not rerun.
- `git diff --check` and exact six-file allowlist validation passed before commit.

## Known Limits

- The assembly-boundary assertions completed and emitted their passing marker, but a clean Unity process exit for that command is unproven because of the post-marker shutdown crash.
- This read-only editor evidence does not exercise Android builds, device runtime behavior, placement migration, operation-map switching, or removal of the current Match scene compatibility bindings.
- No scene, config, manifest, integrity ledger, generated chunk, project setting, package manifest, or runtime source is modified.

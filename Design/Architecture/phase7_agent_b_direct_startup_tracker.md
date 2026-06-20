# Phase 7 Agent B Tracker - Direct Data Systems, Diagnostics, Startup, And Config

Purpose:
Convert the lowest-risk non-UI `SystemBase` targets that are pure ECS data/request/state processors, then handle one-shot startup/config projection systems after serialized or managed configuration has a safe ECS data boundary.

Branch:
`codex/phase7-agent-b-direct-startup`

Progress snapshot:

- Checklist progress: `0 / 44 complete (0.0%)`.
- In progress: `0`.
- Remaining open: `44`.
- Current target: `B0 - wait for Agent A inventory`.
- Direct conversions completed: `0`.
- Startup/config conversions completed: `0`.
- Retired/folded helpers: `0`.
- Managed diagnostic/config `SystemBase` exceptions created: `0`.
- Validation status: `not started`.

Ownership:

- Owns low-risk direct conversion targets after Agent A classifies them.
- Owns non-UI diagnostics/runtime state systems that do not touch UI, camera, prefab, scene, or Unity objects.
- Owns AI/faction/economy/runtime-grid startup projection only after config data is ECS-safe.

Do not touch:

- Selection command systems owned by Agent C.
- Building, placement, and production systems owned by Agent D.
- Road, runtime-city, and citizen systems owned by Agent E.
- Rendering/VFX/camera/presentation systems owned by Agent F.
- Main Phase 7 tracker except through handoff reports.
- Any `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loop, or manager-style MonoBehaviour ticker. MonoBehaviours are view/reference holders only.

Candidate examples to verify, not pre-approved:

- `MatchStartRequestSystem`
- `RuntimeGameplayStateSystem`
- `RuntimeDiagnosticsSystem`
- `SelectionRuntimeDiagnosticsSystem` only if Agent C agrees it is not selection command state.
- `AIStartupSystem`
- `AIPlanEntryStartupSystem`
- `AIFactionControlStartupSystem`
- `FactionEconomyStartupSystem`
- `RuntimeGridBootstrapSystem`
- `InitialFactionSpawnCellSystem` if inventory still contains it.

## B0 - Intake And Classification

- [ ] Wait for Agent A inventory and guardrail baseline.
- [ ] Pull only rows assigned to Agent B.
- [ ] For each assigned row, inspect the source file and call sites before editing.
- [ ] Confirm no target has UI, camera, prefab, `GameObject`, `UnityEngine.Object`, or serialized asset ownership.
- [ ] Split Agent B rows into `DirectConvert`, `StartupProjection`, `RetireFold`, and `NeedsOtherAgent`.
- [ ] Write an initial Agent B handoff with the target list before implementation starts.

Acceptance:

- Every Agent B target has a first slice and validation gate.
- No broad or managed-object target is accepted as direct conversion.

## B1 - Direct Conversion Batch

- [ ] Pick 3-5 smallest `DirectConvert` targets.
- [ ] For each target, inspect lifecycle methods and query dependencies.
- [ ] Convert `sealed partial class : SystemBase` to `partial struct : ISystem`.
- [ ] Replace `OnCreate()` with `OnCreate(ref SystemState state)`.
- [ ] Replace `OnUpdate()` with `OnUpdate(ref SystemState state)`.
- [ ] Replace `EntityManager` and `GetEntityQuery` calls with `state.EntityManager`, `state.GetEntityQuery`, or `SystemAPI`.
- [ ] Replace `Entities.ForEach` with `SystemAPI.Query`, `IJobEntity`, `IJobChunk`, or explicit query iteration.
- [ ] Cache `EntityQuery`, handles, and lookups in fields when repeated.
- [ ] Refresh handles/lookups each update.
- [ ] Use ECB for structural changes unless same-frame mutation is required and documented.
- [ ] Add `[BurstCompile]` only after unmanaged access is proven.
- [ ] Run focused validation after each file.
- [ ] Regenerate inventory if Agent A has provided the command.

Acceptance:

- Converted targets compile.
- No converted target references managed Unity object APIs.
- Domain behavior validation passes.

## B2 - Diagnostics And Runtime State

- [ ] Identify diagnostics systems that only publish ECS counters, flags, or logs.
- [ ] Split any Unity log subscription, file IO, or managed retained string buffer into a counted managed diagnostic/config `SystemBase` exception when it must tick, or a view/reference-only object when it does not.
- [ ] Convert pure ECS diagnostics publication to `ISystem`.
- [ ] Ensure logs are gated and do not allocate in hot paths.
- [ ] Replace managed list/string accumulation with fixed buffers, ring buffers, or counted managed diagnostic/config exception state.
- [ ] Add validation for diagnostic state publication.
- [ ] Run architecture guardrails.

Acceptance:

- Diagnostics ECS work is unmanaged where practical.
- Managed log/string work is outside recurring gameplay `SystemBase` and does not introduce updating MonoBehaviours.

## B3 - Startup And Config Projection

- [ ] Inventory startup systems assigned to Agent B.
- [ ] Separate authored/serialized config reads from ECS writes.
- [ ] If a system reads `ScriptableObject`, scene references, or serialized managed objects, create or reuse a managed projection `SystemBase` exception first when ticking is required.
- [ ] Do not move config projection ticking into MonoBehaviour `Update`, `LateUpdate`, `FixedUpdate`, or coroutine loops.
- [ ] Represent runtime startup data as unmanaged singleton components or dynamic buffers.
- [ ] Convert one-shot ECS writes to `ISystem`.
- [ ] Use `RequireForUpdate` and one-shot completion tags instead of managed enabled flags where appropriate.
- [ ] Avoid static mutable startup registries.
- [ ] Add focused validation for match-start request, AI plan startup, faction economy startup, and runtime-grid bootstrap.
- [ ] Run compile and architecture validation.

Acceptance:

- Startup behavior remains one-shot and deterministic.
- Serialized config does not get forced into unmanaged `ISystem`.

## B4 - Retire/Fold Helpers

- [ ] Identify assigned `SystemBase` classes with no independent update responsibility.
- [ ] Fold pure helper logic into plain static/value helpers or owning systems.
- [ ] Replace public managed helper APIs with ECS request/result data when needed.
- [ ] Delete empty managed ECS shells only after call sites are removed.
- [ ] Preserve `.meta` files when files are moved or deleted.
- [ ] Run grep to confirm old type names are gone or intentionally retained.

Acceptance:

- No broad helper shell remains just to satisfy composition.
- Call sites are explicit and test-covered.

## B5 - Agent B Completion

- [ ] Run `git diff --check`.
- [ ] Run Agent B focused validation set.
- [ ] Run architecture guardrails.
- [ ] Write `Design/AgentReports/YYYY-MM-DD_phase7_agent_b_direct_startup_handoff.md`.
- [ ] Include converted count, split count, retired count, validation logs, and remaining blockers.

Handoff format:

- Checklist progress.
- Target systems completed.
- Target systems deferred and why.
- New `ISystem` names.
- Removed `SystemBase` names.
- Managed diagnostic/config `SystemBase` exceptions created.
- Validation commands and logs.
- Merge notes for Agent A.

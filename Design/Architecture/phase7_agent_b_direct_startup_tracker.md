# Phase 7 Agent B Tracker - Direct ECS Data, Startup, Config, And Diagnostics

Purpose:
Convert the low-risk non-UI `SystemBase` systems whose behavior is already pure ECS data, one-shot startup projection, lightweight diagnostics, or request/result processing. Agent B is the "direct conversion first" lane, but only for rows classified by Agent A as safe. If a target owns Unity objects, prefab GameObjects, camera state, UI, visual effects, or managed presentation ticking, Agent B must not force it into an unmanaged `ISystem`.

Branch:
`codex/phase7-agent-b-direct-startup`

Execution order:

1. Wait for Agent A to finish the authoritative inventory, guardrail tests, and owner assignment.
2. Pull only rows assigned to `AgentB` from `Design/Architecture/systembase_to_isystem_inventory.md`.
3. Convert in small batches: one system, one tightly coupled pair, or one retired helper per slice.
4. After every slice, run the focused validation gate recorded by Agent A, update this tracker, and write a handoff under `Design/AgentReports/`.
5. Do not edit the main Phase 7 tracker directly; Agent A owns integration bookkeeping.

Progress snapshot:

- Checklist progress: `0 / 72 complete (0.0%)`.
- In progress: `0`.
- Remaining open: `72`.
- Current target: `B0 - wait for Agent A inventory assignment`.
- Converted to `ISystem`: `0`.
- Retired/folded helpers: `0`.
- Managed `SystemBase` exceptions created: `0`.
- Validation status: `not started`.

Owned files:

- `Design/Architecture/phase7_agent_b_direct_startup_tracker.md`
- Agent B implementation files assigned by `Design/Architecture/systembase_to_isystem_inventory.md`
- Agent B focused tests or validation runners when a converted row lacks coverage
- Agent B handoff reports under `Design/AgentReports/`

Do not touch:

- UI Toolkit/Canvas systems, UI views, or UI documents.
- Building placement, building production, road, citizen, city, rendering, VFX, camera, or selection systems unless Agent A explicitly assigns a row to Agent B.
- Shared trackers except this file.
- Scenes, prefabs, materials, quality assets, or ScriptableObject config assets unless the assigned row already has a documented passive-config projection slice.

Shared rules:

- Do not introduce `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loops, or manager-style MonoBehaviour tickers.
- MonoBehaviours are view/reference holders only. They may expose serialized references or one-shot event methods, but not ticking gameplay orchestration.
- Managed Unity-object ticking belongs in a counted managed `SystemBase` exception when unavoidable, not in an `ISystem` and not in a MonoBehaviour loop.
- Do not use `GameObject.Find`, `Object.Find*`, `Camera.main`, hierarchy path lookup, service locators, mutable static gameplay registries, or broad facade/controller shells.
- Do not keep public `Update()` methods on `SystemBase`. If the system remains managed, lifecycle must be explicit and warning-free.
- Do not replace one broad `SystemBase` with one broad `ISystem`. Split request, calculation, result, and presentation boundaries when the row has mixed responsibilities.
- Preserve Unity `.meta` files.

Reference documents:

- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/performance_regression_contract.md`
- `Design/Architecture/ecs_native_command_request_system_conversion_example.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`

Likely Agent B target families after Agent A review:

- Match lifecycle request and runtime state systems.
- AI/faction startup and one-shot bootstrap projection systems.
- Runtime grid bootstrap and initial spawn-cell systems that already operate on ECS components.
- Diagnostics systems that read ECS state and publish counters without owning Unity objects.
- Disabled or helper systems that can be retired, folded into a static domain function, or replaced with a small `ISystem` processor.

Likely not Agent B even if they look small:

- Systems with GameObject prefab fields or runtime prefab reverse lookup.
- Systems with camera, visual, renderer, particle, material, light, or transform ownership.
- Systems that expose a public managed API consumed by composition or UI.
- Systems whose public methods are really a managed view/config boundary.

## B0 - Intake From Agent A

Goal:
Start only from the authoritative inventory and avoid duplicate work with other agents.

- [ ] Read `Design/Architecture/systembase_to_isystem_inventory.md` after Agent A marks it ready.
- [ ] Filter rows assigned to `AgentB`.
- [ ] Confirm every selected row has one of these dispositions: `DirectConvert`, `RetireFold`, `SplitThenConvert`, or `ReviewRequired`.
- [ ] Reject or return any row with Unity object blockers unless Agent A explicitly marks a managed exception or split boundary.
- [ ] Copy the Agent B rows into this tracker or into an Agent B report with ids, type names, paths, disposition, blockers, and validation gates.
- [ ] Sort rows by risk: retired/folded helpers first, pure ECS direct conversions second, one-shot startup projection third, diagnostics last.
- [ ] Identify rows with public methods/properties and list every caller with `rg`.
- [ ] Identify update group/order attributes for each target.
- [ ] Identify singleton dependencies and required creation order for each target.
- [ ] Identify any jobs, native containers, entity queries, and cached lookups that need `OnDestroy` cleanup or type-handle refresh.
- [ ] Record rows that need Agent A reclassification before code changes.
- [ ] Update the progress snapshot with the Agent B checklist denominator and current target.

Acceptance:

- Agent B has a concrete row list from the authoritative inventory.
- No row overlaps with Agent C-F ownership.
- No runtime code changed before intake is complete.

## B1 - Direct Conversion Playbook

Goal:
Use one repeatable pattern for pure ECS data systems.

- [ ] Inspect the whole file before editing, including attributes, public API, nested types, and comments.
- [ ] Inspect all call sites with `rg "<TypeName>" Assets/Game/Scripts Assets/Tests`.
- [ ] Decide whether the public API must become ECS singleton data, a request component, a result component, or a static pure helper.
- [ ] Convert `public sealed partial class X : SystemBase` to `public partial struct X : ISystem` only when no managed fields remain.
- [ ] Replace `OnCreate()` with `OnCreate(ref SystemState state)`.
- [ ] Replace `OnUpdate()` with `OnUpdate(ref SystemState state)`.
- [ ] Replace `OnDestroy()` with `OnDestroy(ref SystemState state)` only when cleanup is required.
- [ ] Replace `RequireForUpdate<T>()` with `state.RequireForUpdate<T>()`.
- [ ] Replace `EntityManager` property usage with `state.EntityManager` or `SystemAPI` calls appropriate for unmanaged systems.
- [ ] Replace cached `EntityQuery` fields with local query builders, `SystemAPI.Query`, or `state.GetEntityQuery` when caching is safe in unmanaged fields.
- [ ] Replace cached `ComponentLookup<T>` and `BufferLookup<T>` with unmanaged fields only when they are refreshed every update.
- [ ] Replace `Entities.ForEach` with `SystemAPI.Query`, `IJobEntity`, or explicit query iteration.
- [ ] Keep `state.Dependency` handling explicit when jobs are scheduled.
- [ ] Use `EntityCommandBuffer` only from safe world-owned allocators or singleton ECB systems; dispose temporary ECBs.
- [ ] Add `[BurstCompile]` only after managed blockers are gone and the code is deterministic under Burst.
- [ ] Preserve existing update group/order attributes exactly unless a validation failure proves they need adjustment.
- [ ] Avoid broad cleanup while converting; do not rename gameplay concepts outside the target file unless required for compile.
- [ ] Run `git diff --check` after the slice.
- [ ] Run the target's focused test, architecture test, or Unity compile validation.
- [ ] Write a handoff note with the inventory row id, changed files, validation command, and residual risk.

Acceptance:

- Converted system compiles as `ISystem`.
- No managed fields, Unity object references, or runtime GameObject APIs remain in the converted type.
- Behavior path still uses the same ECS inputs and produces the same ECS outputs.

## B2 - Startup And Bootstrap Projection

Goal:
Convert one-shot startup systems without creating hidden managed state.

- [ ] Identify whether the target is one-shot bootstrap, per-match startup, per-world startup, or scene/runtime startup.
- [ ] Confirm startup trigger: singleton tag, request component, scene entity, subscene baked data, or composition call.
- [ ] Replace public `Initialize`/`Configure` style calls with ECS data written by the existing composition boundary when safe.
- [ ] Keep serialized config and ScriptableObject reads outside unmanaged `ISystem`; project them into ECS data through an existing managed boundary or a counted managed config exception.
- [ ] Ensure one-shot systems are gated by completion tags or disabled request entities so they do not repeat.
- [ ] Preserve deterministic startup order with update groups and `UpdateBefore`/`UpdateAfter` attributes.
- [ ] Do not create static mutable startup registries.
- [ ] Do not move scene reference ownership into the `ISystem`.
- [ ] Validate a fresh match start and a second match restart if the target participates in lifecycle setup.

Acceptance:

- Startup still runs exactly once per intended lifecycle.
- Restart/shutdown paths do not leave stale singleton state.
- No serialized Unity object ownership moved into unmanaged ECS code.

## B3 - Diagnostics Systems

Goal:
Keep diagnostics useful while removing broad managed gameplay systems.

- [ ] Identify whether diagnostics run in production, development builds, editor only, or behind a debug flag.
- [ ] Separate data sampling from presentation/log formatting when the target mixes both.
- [ ] Convert pure ECS sampling to `ISystem`.
- [ ] Keep managed sinks such as strings, file IO, UI text, or Unity logging in a counted managed diagnostics `SystemBase` only when they must tick.
- [ ] Gate all logs so hot paths do not allocate or spam every frame.
- [ ] Use fixed-size ECS buffers or singleton counters for diagnostic values when possible.
- [ ] Preserve existing debug toggles and disabled-by-default behavior.
- [ ] Validate that diagnostics can be enabled and disabled without changing gameplay.

Acceptance:

- Gameplay systems no longer own diagnostic formatting side effects.
- Converted diagnostic samplers are allocation-free in hot paths.
- Managed diagnostic exceptions are explicit and counted.

## B4 - Retire Or Fold Disabled Helpers

Goal:
Remove inactive wrappers instead of converting dead `SystemBase` code.

- [ ] Confirm the target is disabled, unused, test-only, editor-only, or a thin wrapper around pure domain logic.
- [ ] Search for type references, serialized references, reflection references, and asmdef references.
- [ ] If unused, delete the `.cs` file and preserve/delete `.meta` only according to Unity asset rules and project conventions.
- [ ] If logic is still needed, fold it into a static pure domain method or a nearby `ISystem` processor with clear ownership.
- [ ] Do not retire a type referenced by scenes, prefabs, asmdefs, or generated code unless Agent A approves the serialized-reference migration.
- [ ] Add or update tests when the retired helper had covered behavior.
- [ ] Record retired/folded count in the progress snapshot.

Acceptance:

- No references to the retired type remain.
- The project compiles without missing script warnings.
- The behavior either has an explicit replacement or was proven unused.

## B5 - Focused Validation Matrix

Goal:
Run validation that matches the risk of each converted row.

- [ ] Always run `git diff --check -- <changed files>`.
- [ ] Run existing architecture tests that enforce no new non-UI `SystemBase` and no forbidden managed blockers.
- [ ] For startup systems, run the focused EditMode/PlayMode test that starts a match or relevant mode.
- [ ] For diagnostics systems, run a compile validation plus any diagnostic toggle test.
- [ ] For faction/AI startup systems, validate initial faction state, spawn cells, resource setup, and plan-entry availability.
- [ ] For runtime state systems, validate shutdown/restart does not throw `InvalidOperationException` from destroyed `SystemBase` state.
- [ ] If Unity is locked, retry once, then use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` shadow validation when available.
- [ ] Save validation command, result, and log path in the handoff.

Suggested commands:

```bash
git diff --check -- <changed files>
```

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture \
  -runTests -testPlatform EditMode \
  -logFile /private/tmp/warline-phase7-agent-b-editmode.log
```

Acceptance:

- Validation result is recorded per slice.
- Any failed validation is either fixed in the same slice or explicitly handed back as a blocker.

## B6 - Handoff To Agent A

Goal:
Make integration cheap and auditable.

- [ ] Create a dated handoff report under `Design/AgentReports/`.
- [ ] Include inventory row ids, system names, files changed, and final disposition for each row.
- [ ] Include converted-to-`ISystem`, retired/folded, and managed-exception counts.
- [ ] Include validation commands and outcomes.
- [ ] Include any rows returned to Agent A for reclassification.
- [ ] Include any coordination required with Agent C-F.
- [ ] Confirm this tracker progress snapshot is current.
- [ ] Stop only when all Agent B rows are complete, blocked, or returned for reclassification.

Handoff template:

```markdown
# Phase 7 Agent B Handoff - YYYY-MM-DD

Branch:
`codex/phase7-agent-b-direct-startup`

Rows completed:
- `P7-####` - `TypeName` - `Converted` - validation: `passed`

Rows returned:
- `P7-####` - reason

Counts:
- Converted to ISystem:
- Retired/folded:
- Managed SystemBase exceptions:

Validation:
- `git diff --check`: passed/failed
- Unity validation: passed/failed/not run, log path

Risks:
- ...
```

Completion criteria:

- Every Agent B inventory row has final status.
- No Agent B conversion introduced Unity object ownership into `ISystem`.
- No Agent B slice introduced MonoBehaviour ticking.
- Agent A can merge without guessing what changed.

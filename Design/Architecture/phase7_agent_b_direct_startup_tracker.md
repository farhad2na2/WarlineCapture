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

- Checklist progress: `52 / 73 complete (71.2%)`.
- In progress: `0`.
- Remaining open: `20`.
- Current target: `P7-0003/P7-0019 remain held; 2026-06-23 re-audit confirmed both are world-scoped managed reference stores shared across MatchBootstrapSystem, MenuBootstrapSystem, and MatchStartSystem, so direct instance-field folding would change behavior and static/managed-component replacements are outside the current guardrails`.
- Converted to `ISystem`: `8`.
- Retired/folded helpers: `3`.
- Managed `SystemBase` exceptions created: `0`.
- Validation status: `P7-0001 GameplaySceneBindingSystem folded out of ECS into a plain direct-owned scene binding helper; it was a disabled SystemBase with empty OnUpdate and remains owned by MatchBootstrapSystem/GameplayFeatureStartupSystem for runtime grid blocker debug-view scene binding. Compile, inventory regeneration, git diff --check, and Phase 7 architecture guard passed. P7-0003/P7-0019 managed-reference boundary reclassification was tested and rejected by the current Phase 7 guard because those rows have no concrete Unity-object blockers; a 2026-06-23 direct-fold re-audit confirmed both rows are shared through World.GetOrCreateSystemManaged/GetExistingSystemManaged by multiple independently constructed helpers, so per-instance fields would break sharing, static mutable registries are disallowed, and managed ECS components would reintroduce managed component debt. Prior P7-0010 AIPlanEntryStartupSystem, P7-0015 FactionEconomyStartupSystem, P7-0008 AIFactionControlStartupSystem, P7-0016 InitialFactionSpawnCellSystem, P7-0021 RuntimeDiagnosticsSystem, P7-0022 RuntimeGameplayStateSystem, P7-0013 AIStartupSystem, and P7-0005 AICombatOrderSystem converted/cleaned and validated; P7-0020 PerformanceDiagnosticsSystem and P7-0002 MapSurfaceRuntimeBootstrapSystem retired/folded out of ECS and validated. Latest logs: /private/tmp/warline-phase7-agent-b-gameplay-scene-binding-helper-fold-bootstrap.log, /private/tmp/warline-phase7-agent-b-gameplay-scene-binding-helper-fold-assembly-boundary.log, and /private/tmp/warline-phase7-agent-a-architecture.log.`

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

- [x] Read `Design/Architecture/systembase_to_isystem_inventory.md` after Agent A marks it ready.
- [x] Filter rows assigned to `AgentB`.
- [x] Confirm every selected row has one of these dispositions: `DirectConvert`, `RetireFold`, `SplitThenConvert`, or `ReviewRequired`.
- [x] Reject or return any row with Unity object blockers unless Agent A explicitly marks a managed exception or split boundary.
- [x] Copy the Agent B rows into this tracker or into an Agent B report with ids, type names, paths, disposition, blockers, and validation gates.
- [x] Sort rows by risk: retired/folded helpers first, pure ECS direct conversions second, one-shot startup projection third, diagnostics last.
- [x] Identify rows with public methods/properties and list every caller with `rg`.
- [x] Identify update group/order attributes for each target.
- [x] Identify singleton dependencies and required creation order for each target.
- [x] Identify any jobs, native containers, entity queries, and cached lookups that need `OnDestroy` cleanup or type-handle refresh.
- [x] Record rows that need Agent A reclassification before code changes.
- [x] Update the progress snapshot with the Agent B checklist denominator and current target.

Intake command record:

```bash
python3 - <<'PY'
from pathlib import Path
p=Path('Design/Architecture/systembase_to_isystem_inventory.md')
for line in p.read_text().splitlines():
    if '| `P7-' in line and '| `AgentB` |' in line:
        print(line)
PY

rg -n "GameplaySceneBindingSystem|MapSurfaceRuntimeBootstrapSystem|MatchSceneReferenceBoundarySystem|AIFactionControlStartupSystem|AIPlanEntryStartupSystem|AIStartupSystem|FactionEconomyStartupSystem|InitialFactionSpawnCellSystem|PerformanceDiagnosticsReferenceBoundarySystem|PerformanceDiagnosticsSystem|RuntimeDiagnosticsSystem|RuntimeGameplayStateSystem" Assets/Game/Scripts Assets/Tests -g '*.cs'
```

Agent B row intake:

| Id | Type | Current | Disposition | Status | Risk order | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `P7-0003` | `MatchSceneReferenceBoundarySystem` | `SystemBase` | `RetireFold` | `ReturnedForReclassification` | Hold | Stores a managed `MatchSceneView` shared across menu/start/match composition; direct retire would require static mutable state or a managed component. Needs Agent A disposition review before code changes. |
| `P7-0019` | `PerformanceDiagnosticsReferenceBoundarySystem` | `SystemBase` | `RetireFold` | `ReturnedForReclassification` | Hold | Stores managed diagnostics presentation references across menu/match composition; direct retire has the same managed-reference boundary risk as `P7-0003`. Needs Agent A disposition review before code changes. |
| `P7-0020` | `PerformanceDiagnosticsSystem` | `PlainClass` | `RetiredFolded` | `Folded` | Done | Folded out of ECS inheritance; remains a manually owned diagnostics helper for menu/bootstrap composition and no longer counts in the ECS system inventory. |
| `P7-0001` | `GameplaySceneBindingSystem` | `PlainClass` | `RetiredFolded` | `Folded` | Done | Folded out of ECS inheritance as a direct-owned scene binding helper. It still reads `GridAuthoring.Instances` and `grid.gameObject.scene`, so it is intentionally not an unmanaged `ISystem`. |
| `P7-0008` | `AIFactionControlStartupSystem` | `ISystem` | `Converted` | `Converted` | Done | Converted in current Agent B slice; `AIControllerConfig` and `AISettingsRuntimeState` reads now stay outside the unmanaged startup system, which receives `AIFactionControlStartupEntry` values. |
| `P7-0010` | `AIPlanEntryStartupSystem` | `ISystem` | `Converted` | `Converted` | Done | Converted in current Agent B slice; public helpers now take plain fallback id lists instead of `AIPlanEntryStartupConfig`, so the unmanaged system does not reference a `ScriptableObject`. |
| `P7-0013` | `AIStartupSystem` | `ISystem` | `Converted` | `Converted` | Done | Converted in current Agent B slice; startup projection now has no managed fields and uses local startup-entry/projector values plus an `EntityManager` overload for validation. |
| `P7-0015` | `FactionEconomyStartupSystem` | `ISystem` | `Converted` | `Converted` | Done | Converted in current Agent B slice; `AIControllerConfig` ScriptableObject reads now stay in `AIStartupSystem`, which projects plain `FactionEconomyStartupEntry` values into the unmanaged system. |
| `P7-0016` | `InitialFactionSpawnCellSystem` | `ISystem` | `Converted` | `Converted` | Done | Converted in current Agent B slice; serialized fallback initial-units config is projected by `MatchBootstrapSystem` into `InitialFactionSpawnCellFallbackEntry` values before the unmanaged resolver runs. |
| `P7-0021` | `RuntimeDiagnosticsSystem` | `ISystem` | `Converted` | `Converted` | Done | Converted in current Agent B slice; the public diagnostics API now resolves the default world diagnostics singleton explicitly instead of relying on `SystemBase.EntityManager`. |
| `P7-0022` | `RuntimeGameplayStateSystem` | `ISystem` | `Converted` | `Converted` | Done | Converted in current Agent B slice; legacy mirror cache now lives in ECS data (`RuntimeGameplayLegacyMirrorComponent`) so the runtime state accessor has no managed system fields. |
| `P7-0002` | `MapSurfaceRuntimeBootstrapSystem` | `PlainClass` | `RetiredFolded` | `Folded` | Done | Folded out of ECS inheritance; remains an explicit composition helper because it must scan scene authoring `MeshFilter`/`Renderer` overlays while installing runtime map-surface ECS data. |
| `P7-0005` | `AICombatOrderSystem` | `ISystem` | `Converted` | `Converted` | Done | Already `ISystem`; false-positive blocker removed by renaming the `LocalTransform` record member away from `Transform` and deleting the stale manual inventory override. |
| `P7-0004` | `AIBuildPlannerSystem` | `ISystem` | `Converted` | `Converted` | Done | Keep clean; no Agent B code work needed unless validation flags regression. |
| `P7-0006` | `AIDiagnosticLogFlushSystem` | `ISystem` | `Converted` | `Converted` | Done | Keep clean. |
| `P7-0007` | `AIEconomySystem` | `ISystem` | `Converted` | `Converted` | Done | Keep clean. |
| `P7-0009` | `AIFactionControlSystem` | `ISystem` | `Converted` | `Converted` | Done | Keep clean. |
| `P7-0011` | `AIProductionSystem` | `ISystem` | `Converted` | `Converted` | Done | Keep clean. |
| `P7-0012` | `AISquadSystem` | `ISystem` | `Converted` | `Converted` | Done | Keep clean. |
| `P7-0014` | `AITargetingSystem` | `ISystem` | `Converted` | `Converted` | Done | Keep clean. |
| `P7-0017` | `InitialUnitsBlockerChurnSystem` | `ISystem` | `Converted` | `Converted` | Done | Keep clean. |
| `P7-0018` | `InitialUnitsSpawnSystem` | `ISystem` | `Converted` | `Converted` | Done | Keep clean. |

Rows requiring Agent A reclassification before code changes:

- `P7-0003` current `RetireFold` disposition is unsafe without an approved replacement design because the system holds a managed `MatchSceneView` reference shared across menu/start/match composition.
- `P7-0001` current `DirectConvert` disposition is unsafe because the system owns scene-authoring binding through `GridAuthoring.Instances` and `gameObject.scene`; this is a managed composition boundary, not ECS data work.
- `P7-0019` current `RetireFold` disposition has the same managed-reference boundary risk for performance diagnostics presentation state.
- `P7-0002` was folded out of ECS inheritance instead of converted because its managed scene-overlay extraction is a composition boundary; runtime blob/entity behavior remains method-scoped and validated.

Completed follow-up slices:

- `2026-06-22` - `P7-0001` `GameplaySceneBindingSystem`: folded disabled scene-binding `SystemBase` wrapper into a plain direct-owned helper. `MatchBootstrapSystem` still owns the helper directly and `GameplayFeatureStartupSystem` still invokes runtime grid blocker debug-view binding; no scene authoring lookup behavior changed.
- `P7-0005` is now clean in the regenerated inventory: `AICombatOrderSystem` remains `ISystem`, reports no managed blockers, and no longer needs a split/conversion slice.

Call-site and dependency notes:

- `MatchSceneReferenceBoundarySystem`, `MapSurfaceRuntimeBootstrapSystem`, `AIStartupSystem`, `InitialFactionSpawnCellSystem`, `GameplaySceneBindingSystem`, and `PerformanceDiagnosticsSystem` are called primarily by `Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs`.
- Startup projection systems have focused validation tests under `Assets/Tests/Editor/*StartupSystemValidationTests.cs` or dedicated spawn-cell tests.
- `RuntimeGameplayStateSystem` converted after call-site cleanup across road, selection, building placement, camera, runtime city, UI adapter, and focused tests; value-type contexts that write runtime state are intentionally mutable wrappers.
- Converted `ISystem` rows need no type-handle cleanup unless touched by validation; existing converted rows stay in monitoring status.

Completed slices:

| Id | Type | Result | Validation |
| --- | --- | --- | --- |
| `P7-0010` | `AIPlanEntryStartupSystem` | Converted from `SystemBase` to `ISystem`; public API no longer accepts `AIPlanEntryStartupConfig`; inventory generator key stabilized so the row id survived the inheritance change. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; AI plan-entry validation passed in `/private/tmp/warline-phase7-agent-b-ai-plan-entry-startup.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0015` | `FactionEconomyStartupSystem` | Converted from `SystemBase` to `ISystem`; added `FactionEconomyStartupEntry` value projection so unmanaged startup economy logic does not reference `AIControllerConfig` or UnityEngine APIs. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; faction economy startup validation passed in `/private/tmp/warline-phase7-agent-b-faction-economy-startup.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0008` | `AIFactionControlStartupSystem` | Converted from `SystemBase` to `ISystem`; added `AIFactionControlStartupEntry` value projection so unmanaged startup control logic does not reference `AIControllerConfig`, `AISettingsRuntimeState`, or UnityEngine APIs. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; faction-control startup validation passed in `/private/tmp/warline-phase7-agent-b-ai-faction-control-startup.log`; affected AI startup validation passed in `/private/tmp/warline-phase7-agent-b-ai-startup-after-faction-control.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0016` | `InitialFactionSpawnCellSystem` | Converted from `SystemBase` to `ISystem`; moved serialized fallback spawn-cell config ownership into `MatchBootstrapSystem` as plain `InitialFactionSpawnCellFallbackEntry` values. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; spawn-cell validation passed in `/private/tmp/warline-phase7-agent-b-initial-faction-spawn-cell.log`; affected AI startup validation passed in `/private/tmp/warline-phase7-agent-b-ai-startup-after-spawn-cell.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0021` | `RuntimeDiagnosticsSystem` | Converted from `SystemBase` to `ISystem`; removed `SystemBase.EntityManager` dependency and managed world/entity caches while preserving legacy-state mirroring into `RuntimeDiagnosticsStateComponent`. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; runtime diagnostics validation passed in `/private/tmp/warline-phase7-agent-b-runtime-diagnostics.log`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0022` | `RuntimeGameplayStateSystem` | Converted from `SystemBase` to `ISystem`; moved last-legacy mirror cache into ECS data, added focused batchmode validation, and updated runtime-state call sites/tests for value-type ownership. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; runtime gameplay state validation passed in `/private/tmp/warline-phase7-agent-b-runtime-gameplay-state.log`; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0013` | `AIStartupSystem` | Converted from `SystemBase` to `ISystem`; removed managed fields/lists, kept startup-only config projection in the composition/startup boundary, and added an `EntityManager` overload for deterministic validation. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; AI startup validation passed in `/private/tmp/warline-phase7-agent-b-ai-startup.log`; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0020` | `PerformanceDiagnosticsSystem` | Retired/folded from `SystemBase` into a plain diagnostics helper; removed disabled ECS lifecycle inheritance while preserving manual bootstrap/menu ownership and allocation behavior. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; performance diagnostics allocation validation passed in `/private/tmp/warline-phase7-agent-b-performance-diagnostics.log`; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0002` | `MapSurfaceRuntimeBootstrapSystem` | Retired/folded from `SystemBase` into a plain composition helper; removed disabled ECS lifecycle inheritance while preserving runtime blob install/disposal and managed scene-overlay extraction. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; map-surface runtime bootstrap validation passed in `/private/tmp/warline-phase7-agent-b-map-surface-runtime-bootstrap.log`; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |
| `P7-0005` | `AICombatOrderSystem` | Cleaned the stale false-positive inventory blocker; `AICombatOrderSystem` was already an `ISystem`, and the only blocker came from a `LocalTransform` record member named `Transform`. | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed; `git diff --check` passed; AI combat order focused validation passed in `/private/tmp/warline-phase7-agent-b-ai-combat-order.log`; inventory regenerated to `Design/Architecture/systembase_to_isystem_inventory.md`; architecture guard passed in `/private/tmp/warline-phase7-agent-a-architecture.log`. |

Acceptance:

- Agent B has a concrete row list from the authoritative inventory.
- No row overlaps with Agent C-F ownership.
- No runtime code changed before intake is complete.

## B1 - Direct Conversion Playbook

Goal:
Use one repeatable pattern for pure ECS data systems.

- [x] Inspect the whole file before editing, including attributes, public API, nested types, and comments.
- [x] Inspect all call sites with `rg "<TypeName>" Assets/Game/Scripts Assets/Tests`.
- [x] Decide whether the public API must become ECS singleton data, a request component, a result component, or a static pure helper.
- [x] Convert `public sealed partial class X : SystemBase` to `public partial struct X : ISystem` only when no managed fields remain.
- [x] Replace `OnCreate()` with `OnCreate(ref SystemState state)`.
- [x] Replace `OnUpdate()` with `OnUpdate(ref SystemState state)`.
- [x] Replace `OnDestroy()` with `OnDestroy(ref SystemState state)` only when cleanup is required.
- [x] Replace `RequireForUpdate<T>()` with `state.RequireForUpdate<T>()`.
- [x] Replace `EntityManager` property usage with `state.EntityManager` or `SystemAPI` calls appropriate for unmanaged systems.
- [x] Replace cached `EntityQuery` fields with local query builders, `SystemAPI.Query`, or `state.GetEntityQuery` when caching is safe in unmanaged fields.
- [x] Replace cached `ComponentLookup<T>` and `BufferLookup<T>` with unmanaged fields only when they are refreshed every update.
- [x] Replace `Entities.ForEach` with `SystemAPI.Query`, `IJobEntity`, or explicit query iteration.
- [x] Keep `state.Dependency` handling explicit when jobs are scheduled.
- [x] Use `EntityCommandBuffer` only from safe world-owned allocators or singleton ECB systems; dispose temporary ECBs.
- [x] Add `[BurstCompile]` only after managed blockers are gone and the code is deterministic under Burst.
- [x] Preserve existing update group/order attributes exactly unless a validation failure proves they need adjustment.
- [x] Avoid broad cleanup while converting; do not rename gameplay concepts outside the target file unless required for compile.
- [x] Run `git diff --check` after the slice.
- [x] Run the target's focused test, architecture test, or Unity compile validation.
- [x] Write a handoff note with the inventory row id, changed files, validation command, and residual risk.

Acceptance:

- Converted system compiles as `ISystem`.
- No managed fields, Unity object references, or runtime GameObject APIs remain in the converted type.
- Behavior path still uses the same ECS inputs and produces the same ECS outputs.

## B2 - Startup And Bootstrap Projection

Goal:
Convert one-shot startup systems without creating hidden managed state.

- [x] Identify whether the target is one-shot bootstrap, per-match startup, per-world startup, or scene/runtime startup.
- [x] Confirm startup trigger: singleton tag, request component, scene entity, subscene baked data, or composition call.
- [x] Replace public `Initialize`/`Configure` style calls with ECS data written by the existing composition boundary when safe.
- [x] Keep serialized config and ScriptableObject reads outside unmanaged `ISystem`; project them into ECS data through an existing managed boundary or a counted managed config exception.
- [x] Ensure one-shot systems are gated by completion tags or disabled request entities so they do not repeat.
- [x] Preserve deterministic startup order with update groups and `UpdateBefore`/`UpdateAfter` attributes.
- [x] Do not create static mutable startup registries.
- [x] Do not move scene reference ownership into the `ISystem`.
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

- [x] Always run `git diff --check -- <changed files>`.
- [x] Run existing architecture tests that enforce no new non-UI `SystemBase` and no forbidden managed blockers.
- [x] For startup systems, run the focused EditMode/PlayMode test that starts a match or relevant mode.
- [ ] For diagnostics systems, run a compile validation plus any diagnostic toggle test.
- [x] For faction/AI startup systems, validate initial faction state, spawn cells, resource setup, and plan-entry availability.
- [ ] For runtime state systems, validate shutdown/restart does not throw `InvalidOperationException` from destroyed `SystemBase` state.
- [x] If Unity is locked, retry once, then use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` shadow validation when available.
- [x] Save validation command, result, and log path in the handoff.

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

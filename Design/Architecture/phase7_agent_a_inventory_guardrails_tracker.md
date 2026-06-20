# Phase 7 Agent A Tracker - Inventory, Guardrails, And Integration

Purpose:
Own the authoritative Phase 7 denominator, migration guardrails, validation harness, and integration discipline for the non-UI gameplay `SystemBase` to `ISystem` migration. Agent A is the merge-captain lane. Agent A does not convert domain systems except tiny test fixtures required to prove guardrails.

Branch:
`codex/phase7-agent-a-inventory-guardrails`

Execution order:

1. Agent A completes A0-A4 before Agents B-F make implementation changes.
2. Agents B-F may do read-only domain prep while Agent A works, but they must not convert systems before the inventory and guardrails exist.
3. Agent A assigns every inventory row to exactly one owner lane before parallel implementation starts.
4. Agent A integrates domain branches one at a time and updates the main tracker only from the integration branch.

Progress snapshot:

- Checklist progress: `0 / 95 complete (0.0%)`.
- In progress: `0`.
- Remaining open: `95`.
- Current target: `A0 - authoritative inventory generator`.
- Runtime production baseline: `255 SystemBase`, `126 ISystem` under `Assets/Game/Scripts`.
- Non-UI gameplay production target after Phase 7: `0 SystemBase`.
- Allowed production `SystemBase` after Phase 7: `UiToolkitShellApplySystem` plus counted managed presentation/config/camera exceptions only; no editor/test counting in the production denominator.
- Managed presentation exception planning cap: `<= 30 non-UI SystemBase` until Agent A generates the authoritative inventory.
- Updating MonoBehaviour target after Phase 7: `0 newly introduced Update/LateUpdate/FixedUpdate/coroutine loops`.
- Planning projection: `350 ISystem / 31 SystemBase = 91.9% ISystem share` if the managed-exception cap is met and non-exception `SystemBase` targets convert one-to-one.
- Validation status: `not started`.

Owned files:

- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Assets/Tests/Editor/NonUiSystemBaseMigrationArchitectureTests.cs`
- Any new focused validation runner required for Phase 7 guardrails.
- Optional generator script under `Tools/Architecture/`.

Do not touch:

- Domain implementation files except tiny architecture-test fixtures.
- UI Toolkit/Canvas migration implementation.
- Agent B-F tracker progress except to record final reviewed status.
- Project settings, scenes, prefabs, or asmdefs unless a guardrail test genuinely requires an asmdef reference update.

Shared rules:

- Do not allow a new non-UI runtime `SystemBase` unless it has an explicit inventory row and managed presentation/config/camera exception.
- Do not allow converted `ISystem` files to reference `GameObject`, `Transform`, `Camera`, `UnityEngine.Object`, `ScriptableObject`, `Resources`, `Object.Instantiate`, `Object.Destroy`, `Find*`, `Camera.main`, hierarchy paths, managed component classes, `List<GameObject>`, `Dictionary<..., GameObject>`, or mutable static gameplay state.
- Do not mark a target complete if gameplay policy remains in a managed presentation/config/camera exception.
- Do not allow Phase 7 to introduce `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loops, or manager-style MonoBehaviour tickers. MonoBehaviours are view/reference holders only.
- Do not let domain agents edit shared trackers directly. They write handoffs under `Design/AgentReports/`.
- Do not classify a Unity-object visual system as `DirectConvert` just to improve the inheritance percentage. Preserve visuals and keep Unity-object ticking in counted managed `SystemBase` exceptions when required.
- Do not replace one broad `SystemBase` with one broad `ISystem`.

Reference documents:

- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/performance_regression_contract.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`
- `Design/Architecture/five_systembase_to_isystem_conversion_tracker.md`
- `Design/Architecture/ecs_native_command_request_system_conversion_example.md`

## A0 - Authoritative Inventory Generator

Goal:
Create a stable, repeatable inventory so all later work uses the same denominator and ownership rows.

- [ ] Inspect existing architecture tests, especially `EcsBurstHotPathArchitectureTests` and `NonEcsSystemConversionArchitectureTests`, before writing new parsing logic.
- [ ] Decide implementation form: C# architecture test preferred if it can parse all needed fields; generator script allowed if it produces a stable markdown artifact.
- [ ] Create `Tools/Architecture/generate_systembase_to_isystem_inventory.py` or an equivalent C# architecture generator/test.
- [ ] Enumerate every `SystemBase`, `ComponentSystemBase`, `ComponentSystem`, `JobComponentSystem`, and `ISystem` declaration under `Assets/Game/Scripts`.
- [ ] Use a parser robust enough for partial classes, multiple declarations per file, nested test helper classes, generic type declarations, multi-line inheritance lists, attributes, and comments.
- [ ] Exclude `Assets/Game/Scripts/UI` from the non-UI conversion denominator, but list UI systems separately.
- [ ] Exclude editor-only and test-only systems from the production denominator, but list them separately.
- [ ] Record file path, type name, kind, accessibility, namespace if present, assembly if discoverable, line number, and current inheritance.
- [ ] Record update group attributes, ordering attributes, and `[DisableAutoCreation]`.
- [ ] Record lifecycle methods: `OnCreate`, `OnStartRunning`, `OnUpdate`, `OnStopRunning`, `OnDestroy`, `Update`, `LateUpdate`, `FixedUpdate`, and coroutine methods.
- [ ] Record public/internal methods and properties that composition code may call.
- [ ] Record public interface implementations such as renderer, lookup, command, read-model, or boundary interfaces.
- [ ] Record managed field categories: Unity object, managed collection, public helper state, native container, query/lookup/cache, config asset, prefab reference, presentation view.
- [ ] Record ECS access shape: `Entities.ForEach`, `SystemAPI.Query`, `EntityQuery`, `EntityManager`, `GetComponentLookup`, `GetBufferLookup`, `ToEntityArray`, `ToComponentDataArray`, ECB, jobs, `.Run`, `.Schedule`, `.ScheduleParallel`.
- [ ] Record managed blocker tokens: `GameObject`, `Transform`, `Camera`, `UnityEngine.Object`, `ScriptableObject`, `Resources`, `Object.Instantiate`, `Object.Destroy`, `Find*`, `Camera.main`, `Material`, `Renderer`, `Light`, `ParticleSystem`, `LineRenderer`, `VisualEffect`, `MonoBehaviour`, `Coroutine`, `StartCoroutine`, `StopCoroutine`, `List<GameObject>`, `Dictionary<..., GameObject>`.
- [ ] Record likely owner lane from path/name prefix before manual review.
- [ ] Emit stable markdown to `Design/Architecture/systembase_to_isystem_inventory.md`.
- [ ] Emit a compact machine-readable sidecar if useful, for example `Library/Codex/systembase_to_isystem_inventory.json` or `/private/tmp/warline-phase7-systembase-inventory.json`; do not commit generated scratch under `Library`.
- [ ] Add generation timestamp, command used, source commit hash, and dirty-worktree note to the inventory.
- [ ] Run the generator twice and confirm stable output ordering.
- [ ] Record current counts in the Agent A tracker and main Phase 7 tracker.

Suggested generator command shape:

```bash
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py \
  --root Assets/Game/Scripts \
  --output Design/Architecture/systembase_to_isystem_inventory.md
```

Inventory table required columns:

| Column | Meaning |
| --- | --- |
| Id | Stable id such as `P7-0001`, not line-number dependent. |
| Type | C# type name. |
| Kind | `class` or `struct`. |
| Current base | `SystemBase`, `ISystem`, `ComponentSystemBase`, etc. |
| Path | Runtime source path. |
| Line | Declaration line. |
| UI/editor/test scope | `ProductionNonUI`, `ProductionUI`, `Editor`, `Test`. |
| Owner lane | `AgentB`, `AgentC`, `AgentD`, `AgentE`, `AgentF`, `Integration`. |
| Disposition | `DirectConvert`, `SplitThenConvert`, `RetireFold`, `ManagedPresentationSystemBaseException`, `ViewReferenceOnlyMonoBehaviour`, `UIOutOfScope`, `EditorOutOfScope`, `TestOutOfScope`, `ReviewRequired`. |
| Managed blockers | Concrete blocker tokens or `None`. |
| Gameplay policy risk | `None`, `Low`, `Medium`, `High`, with reason. |
| Public API/call sites | Summary of public helper surface and key callers. |
| First safe slice | Smallest recommended behavior-preserving edit. |
| Replacement target | New `ISystem`, split processors, folded helper, or exception type. |
| Validation gate | Focused test/runner/log expected before close. |
| Status | `Open`, `InProgress`, `Converted`, `Split`, `Retired`, `ManagedException`, `Deferred`. |

Acceptance:

- Inventory row count matches the declaration scan.
- Each non-UI production `SystemBase` appears exactly once.
- UI/test/editor exclusions are visible, not hidden.
- Rows are sorted deterministically by scope, owner lane, path, type name.
- Inventory can be regenerated after a domain branch merge without reshuffling unchanged row ids.

## A1 - Classification Rules And Owner Assignment

Goal:
Assign every row to the right lane before parallel implementation starts.

- [ ] Add disposition values: `DirectConvert`, `SplitThenConvert`, `RetireFold`, `ManagedPresentationSystemBaseException`, `ViewReferenceOnlyMonoBehaviour`, `UIOutOfScope`, `EditorOutOfScope`, `TestOutOfScope`, `ReviewRequired`.
- [ ] Add owner lane values: `AgentB`, `AgentC`, `AgentD`, `AgentE`, `AgentF`, `Integration`.
- [ ] Add blocker values from concrete token matches and manual review.
- [ ] Add first safe slice for every `ProductionNonUI` row.
- [ ] Add validation command for every `ProductionNonUI` row or mark as missing validation debt.
- [ ] Add converted/replacement type for any row already converted by earlier work.
- [ ] Mark all rows with no obvious owner as `ReviewRequired`, not guessed.
- [ ] Review every `ReviewRequired` row manually before Agents B-F start implementation.
- [ ] Export owner-lane filtered sections so each domain agent can quickly copy its assigned rows.

Classification decision table:

| Disposition | Use when | Must not contain |
| --- | --- | --- |
| `DirectConvert` | Pure ECS data/request/state work, no Unity object blockers, no broad public helper facade. | `GameObject`, `Camera`, prefab object refs, public managed facade API, gameplay-independent presentation. |
| `SplitThenConvert` | ECS gameplay is mixed with managed/presentation/config/camera work. | A plan to convert the whole broad owner into one large `ISystem`. |
| `RetireFold` | System only composes, forwards, caches dependencies, or exposes helper API with no independent update responsibility. | Hidden gameplay policy or recurring state mutation. |
| `ManagedPresentationSystemBaseException` | Must tick Unity objects like ParticleSystem, Renderer, Light, Camera, Transform, Material, pooled GameObject, scene refs, diagnostics output. | Gameplay decision, command validation, damage, spawn policy, pathing, economy, selection policy. |
| `ViewReferenceOnlyMonoBehaviour` | Holds serialized refs, prefab refs, or callable view methods, with no runtime loop. | `Update`, `LateUpdate`, `FixedUpdate`, coroutine loop, ECS mutation. |
| `ReviewRequired` | Blockers/call sites are unclear. | Silent assignment to a domain lane. |

Owner lane defaults:

| Lane | Default type/path patterns |
| --- | --- |
| Agent B | `RuntimeGameplayState*`, `RuntimeDiagnostics*`, `PerformanceDiagnostics*`, `AI*Startup*`, `AIPlanEntry*`, `AIFactionControlStartup*`, `FactionEconomyStartup*`, `RuntimeGridBootstrap*`, small data-only startup/config projection. |
| Agent C | `RtsSelection*`, `Selection*`, `FocusableUnit*`, `FocusedUnit*`, selected-state and command-result systems. |
| Agent D | `Building*`, `MapBuildingPlacement*`, building placement, production, runtime, spawn, combat, building UI query, five-SystemBase sub-track. |
| Agent E | `Road*`, `RuntimeCity*`, `RuntimeGridBlocker*`, `RuntimeDecoration*`, `DayNight*`, `Citizen*`. |
| Agent F | `Rendering/*`, `*Visual*`, `*Vfx*`, `*Trace*`, `*Impostor*`, `*Marker*`, `*Camera*`, `VisualQuality*`, ParticleSystem/Renderer/Light/Material/pooling ownership. |
| Integration | Shared contracts, architecture tests, generated inventory, main tracker, cross-lane conflicts. |

Acceptance:

- No row is left unclassified unless marked `ReviewRequired` with a reason.
- No row is marked as a managed exception without a concrete Unity-object ticking blocker.
- No row can introduce an updating MonoBehaviour bridge.
- Owner lanes do not overlap.
- Each domain agent can start from its filtered inventory section without reopening the whole project-wide classification.

## A2 - Guardrail Tests

Goal:
Make the migration ratchet enforceable before domain conversions.

- [ ] Add `Assets/Tests/Editor/NonUiSystemBaseMigrationArchitectureTests.cs`.
- [ ] Add `RunFocusedValidation()` entry point for batchmode execution.
- [ ] Load `Design/Architecture/systembase_to_isystem_inventory.md` and parse inventory ids, paths, type names, dispositions, owner lanes, and statuses.
- [ ] Add test that fails when a production non-UI `SystemBase` appears without an inventory row.
- [ ] Add test that fails when an inventory row points to a deleted or renamed file.
- [ ] Add test that fails when duplicate inventory rows point to the same type/path.
- [ ] Add test that converted targets cannot regain `SystemBase`.
- [ ] Add test that completed `ISystem` files avoid managed Unity object blockers.
- [ ] Add test that `ManagedPresentationSystemBaseException` rows do not contain gameplay request validation, command execution, simulation, damage, economy, pathing, selection policy, building placement policy, or gameplay ECS mutation policy.
- [ ] Add test that no Phase 7 change introduces new `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, or coroutine loops.
- [ ] Add test that all MonoBehaviour rows classified as `ViewReferenceOnlyMonoBehaviour` have no runtime loop methods.
- [ ] Add test that broad replacement `ISystem` types are flagged if source length, public helper count, query count, or responsibility markers exceed documented thresholds.
- [ ] Add test that new runtime non-UI ECS systems default to `ISystem` unless classified as managed presentation/config/camera exception.
- [ ] Add test that public helper APIs on converted systems are replaced by ECS request/result or plain helper functions.
- [ ] Add test that inventory owner lane names match existing agent tracker files.
- [ ] Add test that managed exception count does not exceed the planning cap unless the tracker has been updated with a new approved cap.
- [ ] Add test that the final share formula can be computed from inventory counts.

Initial broad-system thresholds:

- Public method/property count over `8`: manual review.
- `OnUpdate` source body over `180` nonblank lines: manual review.
- More than `5` independent query families: manual review.
- More than `2` domain prefixes in a type name or blockers: manual review.
- Any type containing `Manager`, `Controller`, `Facade`, `Service`, `Resolver`, `Context`, `Adapter`, or `Composer` in a new replacement name: fail unless explicitly approved by architecture contract.

Deliberate violation checks:

- [ ] Temporarily add a local untracked fixture or in-test source string for a new non-inventory `SystemBase`; confirm test fails, then remove it.
- [ ] Temporarily add a local untracked fixture or in-test source string for `MonoBehaviour.Update`; confirm test fails, then remove it.
- [ ] Temporarily mark a known gameplay system as `ManagedPresentationSystemBaseException`; confirm policy-token guard fails, then restore.

Acceptance:

- Guardrails pass on the current baseline before any domain conversion.
- Deliberate violations fail the expected guards.
- Guardrails are runnable without opening the Unity editor manually.
- Test failure messages include the file path, type name, inventory id, and required next action.

## A3 - Validation Matrix And Runner

Goal:
Give each domain lane a known validation set before implementation.

- [ ] Create a Phase 7 validation matrix in the inventory or as a section in this tracker.
- [ ] Map Agent B direct/startup systems to startup, diagnostics, and architecture validations.
- [ ] Map Agent C selection systems to selection input, command-result, hold/stop/scan, board/attack, and allocation validations.
- [ ] Map Agent D building systems to placement, production, build drawer, building selection, combat, and placement-to-production PlayMode validations.
- [ ] Map Agent E road/city/citizen systems to road build, runtime city, blocker, citizen, movement, and match smoke validations.
- [ ] Map Agent F rendering/VFX systems to render budget, vehicle visual, missile VFX, attached light, marker, visual-quality, and graphics smoke validations.
- [ ] Add one command or runner entry per validation gate where practical.
- [ ] Add fallback rule: if main Unity project is locked, retry once, then use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` if available.
- [ ] Add log path naming convention: `/private/tmp/warline-phase7-agent-<lane>-<target>-<validation>.log`.
- [ ] Add required validation status values: `NotRun`, `Passed`, `Failed`, `BlockedProjectLocked`, `BlockedMissingRunner`, `DeferredWithReason`.

Suggested architecture command:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture \
  -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation \
  -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Minimum validation matrix:

| Lane | Required validation gates |
| --- | --- |
| Agent B | Architecture guard, compile, `RuntimeDiagnosticsSystemTests`, `AIStartupSystemValidationTests`, `AIFactionControlStartupSystemValidationTests`, `AIPlanEntryStartupSystemValidationTests`, `FactionResourceSystemTests`, startup smoke where available. |
| Agent C | Architecture guard, compile, `RtsSelectionInputSystemTests`, `SelectionStateSystemTests`, `FocusableUnitLookupSystemTests`, `SelectedUnitOrderSnapshotSystemTests`, hold/stop/scan and board/attack command validations. |
| Agent D | Architecture guard, compile, building placement command/runtime tick, production, build drawer, building UI query, building combat, building selection marker/faction visual, placement-to-production PlayMode smoke. |
| Agent E | Architecture guard, compile, road build command, nearest road/build, runtime city generation, runtime grid blocker, citizen visible/population, movement, match runtime smoke. |
| Agent F | Architecture guard, compile, render budget, vehicle visual adornments, air missile VFX, ground missile VFX, attached light, marker, visual quality, graphics-capable smoke. |

Acceptance:

- Every inventory owner lane has at least one focused validation gate.
- Final Phase 7 cannot close without the matrix passing or explicit deferred validation owner.
- Each domain handoff can cite validation gates by name instead of inventing new ones.

## A4 - Integration Workflow

Goal:
Allow B-F to work in parallel without creating merge chaos.

- [ ] Define branch names in every agent tracker and verify they are unique.
- [ ] Create or name an integration branch, for example `codex/phase7-integration`.
- [ ] Require each domain agent to write `Design/AgentReports/YYYY-MM-DD_phase7_agent_<lane>_handoff.md`.
- [ ] Require each handoff to include files changed, inventory ids touched, systems converted, systems split, managed exceptions created/retained, validations run, validation logs, and blockers.
- [ ] Require each handoff to declare whether it touched shared components/contracts, asmdefs, tests, or generated inventory.
- [ ] Require each handoff to list expected conflicts before merge.
- [ ] Merge one domain branch at a time into the integration branch.
- [ ] After each merge, regenerate inventory and compare counts.
- [ ] After each merge, run `git diff --check`.
- [ ] After each merge, run Phase 7 architecture guardrails.
- [ ] After each merge, update only the relevant inventory rows and main tracker progress snapshot.
- [ ] If a domain branch conflicts with another branch, Agent A resolves using both handoffs and reruns the smaller affected validations before moving on.
- [ ] If a domain branch changes a shared contract, Agent A notifies affected lane docs in the integration handoff before merging the next branch.

Merge order recommendation:

1. Agent B direct/startup, because these should be smallest and prove the guardrails.
2. Agent C selection/commands, because many other domains read selection state.
3. Agent F rendering/VFX request contracts that do not depend on building/road internals.
4. Agent D building/production after request/result and visual exception rules are proven.
5. Agent E road/city/citizen after building and visual request contracts stabilize.
6. Agent F final visual/camera exceptions after D/E expose final request/result data.

Acceptance:

- No two agents edit the main Phase 7 tracker concurrently.
- No two agents edit the same inventory rows concurrently.
- Merge conflicts are handled by Agent A with the domain handoff open.
- Inventory counts are updated after every merge.

## A5 - Progress Accounting And Percentage Formula

Goal:
Keep user-facing progress and final ratio honest.

- [ ] Count production `SystemBase` under `Assets/Game/Scripts`, excluding editor and tests.
- [ ] Count production `ISystem` under `Assets/Game/Scripts`, excluding editor and tests.
- [ ] Count UI `SystemBase` separately.
- [ ] Count non-UI gameplay `SystemBase` separately.
- [ ] Count managed presentation/config/camera `SystemBase` exceptions separately.
- [ ] Count view/reference-only MonoBehaviours separately only when they replace old managed ownership.
- [ ] Count converted `ISystem` processors created by Phase 7.
- [ ] Count retired/folded systems.
- [ ] Count split managed exceptions retained.
- [ ] Count remaining `ReviewRequired` rows.
- [ ] Recalculate final `ISystem` share after every integration merge.

Formula:

```text
final_isystem_share =
  final_production_isystem_count /
  (final_production_isystem_count + final_production_systembase_count)
```

Planning examples from current baseline:

| Scenario | Remaining production `SystemBase` | Final production `ISystem` | `ISystem` share |
| --- | ---: | ---: | ---: |
| Aggressive managed-exception target | `21` (`1` UI + `20` non-UI managed exceptions) | `360` | `94.5%` |
| Planning cap | `31` (`1` UI + `30` non-UI managed exceptions) | `350` | `91.9%` |
| Conservative overrun | `41` (`1` UI + `40` non-UI managed exceptions) | `340` | `89.2%` |

Progress snapshot format:

```text
Phase 7 Agent A progress:
- Checklist: X / Y complete (Z%).
- Inventory rows: total N, production non-UI SystemBase N, UI SystemBase N, tests/editor N.
- Dispositions: DirectConvert N, SplitThenConvert N, RetireFold N, ManagedPresentationSystemBaseException N, ReviewRequired N.
- Owner lanes assigned: AgentB N, AgentC N, AgentD N, AgentE N, AgentF N, Integration N.
- Guardrails: NotRun/Passed/Failed.
- No-updating-MonoBehaviour guard: NotRun/Passed/Failed.
- Current projected final share: N ISystem / M SystemBase = P%.
```

Acceptance:

- Every user-facing status includes counts, percentage, current target, validation status, and blockers.
- Percentage is never reported from stale static seed counts after the inventory exists.

## A6 - Final Completion

Goal:
Close Agent A only when the project can safely start or finish parallel domain implementation.

- [ ] Regenerate inventory after all domain lanes are merged.
- [ ] Confirm non-UI gameplay production `SystemBase` count is `0`.
- [ ] Confirm production `SystemBase` count is `1 + counted managed presentation/config/camera exceptions`.
- [ ] Confirm managed exception count is at or below the approved cap, or update the cap with reasons.
- [ ] Confirm final `ISystem` and `SystemBase` percentage.
- [ ] Confirm no new updating MonoBehaviour was introduced by Phase 7.
- [ ] Confirm all `ReviewRequired` rows are resolved or explicitly deferred with owner and reason.
- [ ] Raise architecture guard floors for final counts.
- [ ] Run full Phase 7 validation matrix.
- [ ] Write final report under `Design/AgentReports`.
- [ ] Update the main tracker to close Phase 7.

Final Agent A handoff format:

- Completed checklist items over total.
- Remaining open items.
- Current production `SystemBase` count.
- Current non-UI gameplay `SystemBase` count.
- Current managed presentation/config/camera exception count.
- Current production `ISystem` count.
- Final `ISystem` share.
- Converted systems by lane.
- Retired/folded systems by lane.
- Managed exceptions retained by lane.
- Validation commands and logs.
- Known deferred work.

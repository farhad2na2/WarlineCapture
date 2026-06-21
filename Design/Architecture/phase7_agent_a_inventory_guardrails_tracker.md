# Phase 7 Agent A Tracker - Inventory, Guardrails, And Integration

Purpose:
Own the authoritative Phase 7 denominator, migration guardrails, validation harness, and integration discipline for the non-UI gameplay `SystemBase` to `ISystem` migration. Agent A is the merge-captain lane for parallel branches. In the current user-approved single-thread automation mode, this same thread may continue into Agents B-F after the Agent A baseline is complete, but it must follow each lane tracker, update that lane's progress, and preserve the same one-lane-at-a-time validation discipline.

Branch:
`codex/phase7-agent-a-inventory-guardrails`

Execution order:

1. Agent A completes the inventory, classification, guardrails, validation matrix, handoff contract, and progress accounting baseline before domain implementation changes.
2. In parallel-agent mode, Agents B-F write handoffs and Agent A integrates one branch at a time.
3. In current single-thread automation mode, this thread may continue into B-F lane work directly after Agent A baseline tasks are complete.
4. Single-thread domain work must still be lane-scoped: update the active lane tracker, touch only that lane's inventory rows/files unless a shared contract change is documented, run the lane validation gates, regenerate the authoritative inventory after each lane slice, and update the main tracker.
5. Agent A final completion remains open until all B-F lanes are done and the final validation matrix passes.

Progress snapshot:

- Checklist progress: `77 / 95 complete (81.1%)`.
- In progress: `0`.
- Remaining open: `18`.
- Current target: `Agent D building/production lane in progress; P7-0070/P7-0137/P7-0061/P7-0069/P7-0068/P7-0067 helper fold complete; continue next low-risk Agent D row before broad spawn/production owners`.
- Runtime production baseline: `220 SystemBase/legacy declarations`, `133 ISystem declarations` under `Assets/Game/Scripts`.
- Inventory rows: `353 total`, `346 ProductionNonUI`, `7 ProductionUI`.
- Owner lanes assigned: `AgentB 20`, `AgentC 12`, `AgentD 84`, `AgentE 97`, `AgentF 49`, `Integration 91`.
- Dispositions: `Converted 126`, `DirectConvert 54`, `ManagedPresentationSystemBaseException 22`, `RetireFold 29`, `ReviewRequired 0`, `SplitThenConvert 115`, `UIOutOfScope 7`.
- Non-UI gameplay production target after Phase 7: `0 non-exception SystemBase`.
- Allowed production `SystemBase` after Phase 7: `UiToolkitShellApplySystem` plus counted managed presentation/config/camera exceptions only; no editor/test counting in the production denominator.
- Managed presentation exception planning cap: `<= 30 non-UI SystemBase`; current inventory plans `22`.
- Updating MonoBehaviour target after Phase 7: `0 newly introduced Update/LateUpdate/FixedUpdate/coroutine loops`.
- Planning projection from current inventory: at least `331 ISystem / 22 managed-exception non-UI SystemBase = 93.8% non-UI ISystem share` if every open non-exception production row converts one-to-one and UI remains out of scope; `RetireFold` rows can improve the final denominator.
- Validation status: `Agent B P7-0010 AIPlanEntryStartupSystem, P7-0015 FactionEconomyStartupSystem, P7-0008 AIFactionControlStartupSystem, P7-0016 InitialFactionSpawnCellSystem, P7-0021 RuntimeDiagnosticsSystem, P7-0022 RuntimeGameplayStateSystem, P7-0013 AIStartupSystem, and P7-0005 AICombatOrderSystem conversions/cleanup passed focused validations; P7-0020 PerformanceDiagnosticsSystem and P7-0002 MapSurfaceRuntimeBootstrapSystem folded out of ECS and passed focused validations; Agent C has completed all open SystemBase rows; Agent F request-contract slice folded P7-0281 SelectionScreenMarkerSystem and P7-0259 BuildingMarkerVisualCompositionSystem out of ECS into plain request/visual helpers, split P7-0249 UnitModelSpawnSystem and P7-0251 UnitRenderBudgetSystem to consume RuntimeCameraSnapshotComponent instead of managed Camera, and moved P7-0283/P7-0284 combat VFX playback to reviewed managed presentation exceptions in UnitAttackVfxSystems.cs while marking P7-0338 UnitAttackSystem cleanly converted; Agent D folded P7-0070 BuildingGameplayGridDataSystem, P7-0137 BuildingSurfacePlacementSystem, P7-0061 BuildingGameplayBindingSystem, P7-0069 BuildingGameplayEcsQuerySystem, P7-0068 BuildingGameplayDisposalSystem, and P7-0067 BuildingGameplayDisposalCompositionSystem out of ECS into plain direct-owned helpers; Agent A architecture guard passed`; latest Agent D logs include `/private/tmp/warline-phase7-agent-d-helper-fold-map-surface.log`, `/private/tmp/warline-phase7-agent-d-helper-fold-placement-command.log`, `/private/tmp/warline-phase7-agent-d-helper-fold-composition-smoke.log`, `/private/tmp/warline-phase7-agent-d-binding-helper-fold-composition-smoke.log`, `/private/tmp/warline-phase7-agent-d-ecs-query-helper-fold-composition-smoke.log`, `/private/tmp/warline-phase7-agent-d-disposal-helper-fold-composition-smoke.log`, and `/private/tmp/warline-phase7-agent-a-architecture.log`; prior Agent F marker/camera/VFX logs remain recorded in the Agent F tracker; residual unrelated validation note: `/private/tmp/warline-phase7-agent-f-building-marker-composition-placement-runtime.log` failed an existing runtime-tick cadence assertion (`Expected: 2`, `But was: 1`); prior Agent B/C log paths remain recorded in the active lane trackers and handoff reports.

Current execution mode:

- Status: `SingleThreadDomainExecutionApproved`.
- User instruction: continue beyond Agent A into B-F boundaries after Agent A baseline; do not block on external handoffs.
- Parallel-agent handoff check command remains available: `find Design/AgentReports -maxdepth 1 -name '2026-*_phase7_agent_*_handoff.md' -print | sort`.
- Result at last check: no Agent B-F Phase 7 handoff reports are present yet, so the heartbeat should use single-thread lane execution rather than wait.
- Next automation action: enter Agent B tracker work first, then Agent C, Agent F request-contract slice, Agent D, Agent E, and Agent F final visual/camera exceptions. After each slice, update the active lane tracker, regenerate inventory, run `git diff --check`, run Phase 7 architecture guardrails, run affected focused validations, and update the main tracker progress.

Owned files:

- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_monobehaviour_loop_baseline.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Assets/Tests/Editor/NonUiSystemBaseMigrationArchitectureTests.cs`
- Any new focused validation runner required for Phase 7 guardrails.
- Optional generator script under `Tools/Architecture/`.

Do not touch unless the current single-thread automation lane explicitly owns it:

- UI Toolkit/Canvas migration implementation.
- Project settings, scenes, prefabs, or asmdefs unless a guardrail test or the active lane's validated implementation genuinely requires it.
- Files outside the active Agent B-F lane without documenting the shared-contract reason in the active lane tracker and main Phase 7 tracker.

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

- [x] Inspect existing architecture tests, especially `EcsBurstHotPathArchitectureTests` and `NonEcsSystemConversionArchitectureTests`, before writing new parsing logic.
- [x] Decide implementation form: C# architecture test preferred if it can parse all needed fields; generator script allowed if it produces a stable markdown artifact.
- [x] Create `Tools/Architecture/generate_systembase_to_isystem_inventory.py` or an equivalent C# architecture generator/test.
- [x] Enumerate every `SystemBase`, `ComponentSystemBase`, `ComponentSystem`, `JobComponentSystem`, and `ISystem` declaration under `Assets/Game/Scripts`.
- [x] Use a parser robust enough for partial classes, multiple declarations per file, nested test helper classes, generic type declarations, multi-line inheritance lists, attributes, and comments.
- [x] Exclude `Assets/Game/Scripts/UI` from the non-UI conversion denominator, but list UI systems separately.
- [x] Exclude editor-only and test-only systems from the production denominator, but list them separately.
- [x] Record file path, type name, kind, accessibility, namespace if present, assembly if discoverable, line number, and current inheritance.
- [x] Record update group attributes, ordering attributes, and `[DisableAutoCreation]`.
- [x] Record lifecycle methods: `OnCreate`, `OnStartRunning`, `OnUpdate`, `OnStopRunning`, `OnDestroy`, `Update`, `LateUpdate`, `FixedUpdate`, and coroutine methods.
- [x] Record public/internal methods and properties that composition code may call.
- [x] Record public interface implementations such as renderer, lookup, command, read-model, or boundary interfaces.
- [x] Record managed field categories: Unity object, managed collection, public helper state, native container, query/lookup/cache, config asset, prefab reference, presentation view.
- [x] Record ECS access shape: `Entities.ForEach`, `SystemAPI.Query`, `EntityQuery`, `EntityManager`, `GetComponentLookup`, `GetBufferLookup`, `ToEntityArray`, `ToComponentDataArray`, ECB, jobs, `.Run`, `.Schedule`, `.ScheduleParallel`.
- [x] Record managed blocker tokens: `GameObject`, `Transform`, `Camera`, `UnityEngine.Object`, `ScriptableObject`, `Resources`, `Object.Instantiate`, `Object.Destroy`, `Find*`, `Camera.main`, `Material`, `Renderer`, `Light`, `ParticleSystem`, `LineRenderer`, `VisualEffect`, `MonoBehaviour`, `Coroutine`, `StartCoroutine`, `StopCoroutine`, `List<GameObject>`, `Dictionary<..., GameObject>`.
- [x] Record likely owner lane from path/name prefix before manual review.
- [x] Emit stable markdown to `Design/Architecture/systembase_to_isystem_inventory.md`.
- [x] Emit a compact machine-readable sidecar if useful, for example `Library/Codex/systembase_to_isystem_inventory.json` or `/private/tmp/warline-phase7-systembase-inventory.json`; do not commit generated scratch under `Library`.
- [x] Add generation timestamp, command used, source commit hash, and dirty-worktree note to the inventory.
- [x] Run the generator twice and confirm stable output ordering.
- [x] Record current counts in the Agent A tracker and main Phase 7 tracker.

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

- [x] Add disposition values: `DirectConvert`, `SplitThenConvert`, `RetireFold`, `ManagedPresentationSystemBaseException`, `ViewReferenceOnlyMonoBehaviour`, `UIOutOfScope`, `EditorOutOfScope`, `TestOutOfScope`, `ReviewRequired`.
- [x] Add owner lane values: `AgentB`, `AgentC`, `AgentD`, `AgentE`, `AgentF`, `Integration`.
- [x] Add blocker values from concrete token matches and manual review.
- [x] Add first safe slice for every `ProductionNonUI` row.
- [x] Add validation command for every `ProductionNonUI` row or mark as missing validation debt.
- [x] Add converted/replacement type for any row already converted by earlier work.
- [x] Mark all rows with no obvious owner as `ReviewRequired`, not guessed.
- [x] Review every `ReviewRequired` row manually before Agents B-F start implementation.
- [x] Export owner-lane filtered sections so each domain agent can quickly copy its assigned rows.

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

- [x] Add `Assets/Tests/Editor/NonUiSystemBaseMigrationArchitectureTests.cs`.
- [x] Add `RunFocusedValidation()` entry point for batchmode execution.
- [x] Load `Design/Architecture/systembase_to_isystem_inventory.md` and parse inventory ids, paths, type names, dispositions, owner lanes, and statuses.
- [x] Add test that fails when a production non-UI `SystemBase` appears without an inventory row.
- [x] Add test that fails when an inventory row points to a deleted or renamed file.
- [x] Add test that fails when duplicate inventory rows point to the same type/path.
- [x] Add test that converted targets cannot regain `SystemBase`.
- [x] Add test that completed `ISystem` files avoid managed Unity object blockers.
- [x] Add test that `ManagedPresentationSystemBaseException` rows do not contain gameplay request validation, command execution, simulation, damage, economy, pathing, selection policy, building placement policy, or gameplay ECS mutation policy.
- [x] Add test that no Phase 7 change introduces new `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, or coroutine loops.
- [x] Add test that all MonoBehaviour rows classified as `ViewReferenceOnlyMonoBehaviour` have no runtime loop methods.
- [x] Add test that broad replacement `ISystem` types are flagged if source length, public helper count, query count, or responsibility markers exceed documented thresholds.
- [x] Add test that new runtime non-UI ECS systems default to `ISystem` unless classified as managed presentation/config/camera exception.
- [x] Add test that public helper APIs on converted systems are replaced by ECS request/result or plain helper functions.
- [x] Add test that inventory owner lane names match existing agent tracker files.
- [x] Add test that managed exception count does not exceed the planning cap unless the tracker has been updated with a new approved cap.
- [x] Add test that the final share formula can be computed from inventory counts.

Initial broad-system thresholds:

- Public method/property count over `8`: manual review.
- `OnUpdate` source body over `180` nonblank lines: manual review.
- More than `5` independent query families: manual review.
- More than `2` domain prefixes in a type name or blockers: manual review.
- Any type containing `Manager`, `Controller`, `Facade`, `Service`, `Resolver`, `Context`, `Adapter`, or `Composer` in a new replacement name: fail unless explicitly approved by architecture contract.

Deliberate violation checks:

- [x] Temporarily add a local untracked fixture or in-test source string for a new non-inventory `SystemBase`; confirm test fails, then remove it.
- [x] Temporarily add a local untracked fixture or in-test source string for `MonoBehaviour.Update`; confirm test fails, then remove it.
- [x] Temporarily mark a known gameplay system as `ManagedPresentationSystemBaseException`; confirm policy-token guard fails, then restore.

Acceptance:

- Guardrails pass on the current baseline before any domain conversion.
- Deliberate violations fail the expected guards.
- Guardrails are runnable without opening the Unity editor manually.
- Test failure messages include the file path, type name, inventory id, and required next action.

## A3 - Validation Matrix And Runner

Goal:
Give each domain lane a known validation set before implementation.

- [x] Create a Phase 7 validation matrix in the inventory or as a section in this tracker.
- [x] Map Agent B direct/startup systems to startup, diagnostics, and architecture validations.
- [x] Map Agent C selection systems to selection input, command-result, hold/stop/scan, board/attack, and allocation validations.
- [x] Map Agent D building systems to placement, production, build drawer, building selection, combat, and placement-to-production PlayMode validations.
- [x] Map Agent E road/city/citizen systems to road build, runtime city, blocker, citizen, movement, and match smoke validations.
- [x] Map Agent F rendering/VFX systems to render budget, vehicle visual, missile VFX, attached light, marker, visual-quality, and graphics smoke validations.
- [x] Add one command or runner entry per validation gate where practical.
- [x] Add fallback rule: if main Unity project is locked, retry once, then use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` if available.
- [x] Add log path naming convention: `/private/tmp/warline-phase7-agent-<lane>-<target>-<validation>.log`.
- [x] Add required validation status values: `NotRun`, `Passed`, `Failed`, `BlockedProjectLocked`, `BlockedMissingRunner`, `DeferredWithReason`.

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

Command template:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture-Clone \
  -executeMethod <RunnerType>.<RunnerMethod> \
  -logFile /private/tmp/warline-phase7-agent-<lane>-<target>-<validation>.log
```

Shared validation commands:

| Gate | Command | Log path/status |
| --- | --- | --- |
| Generator syntax | `python3 -B -c "import py_compile; py_compile.compile('Tools/Architecture/generate_systembase_to_isystem_inventory.py', cfile='/private/tmp/generate_systembase_to_isystem_inventory.pyc', doraise=True)"` | `/private/tmp/generate_systembase_to_isystem_inventory.pyc` |
| Inventory regeneration | `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json` | `/private/tmp/warline-phase7-systembase-inventory.json` |
| Editor compile | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` | domain handoff records console output or redirected log |
| Diff hygiene | `git diff --check` | domain handoff records pass/fail |
| Phase 7 architecture guard | Unity command with `NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-a-architecture.log` |

Focused runner matrix:

| Lane | Gate | Runner entry | Log path |
| --- | --- | --- | --- |
| Agent B | Runtime diagnostics | `RuntimeDiagnosticsSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-b-runtime-diagnostics.log` |
| Agent B | Performance diagnostics allocation | `PerformanceDiagnosticsSystemAllocationTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-b-performance-diagnostics.log` |
| Agent B | AI startup projection | `AIStartupSystemValidationTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-b-ai-startup.log` |
| Agent B | AI faction control startup | `AIFactionControlStartupSystemValidationTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-b-ai-faction-control-startup.log` |
| Agent B | AI plan entry startup | `AIPlanEntryStartupSystemValidationTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-b-ai-plan-entry-startup.log` |
| Agent B | Economy startup | `FactionEconomyStartupSystemValidationTests` tests through Unity Test Runner unless a batch runner is added by the slice | `BlockedMissingRunner` until runner exists |
| Agent C | Selection input | `RtsSelectionInputSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-c-selection-input.log` |
| Agent C | Selection state | `SelectionStateSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-c-selection-state.log` |
| Agent C | Focusable unit lookup | `FocusableUnitLookupSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-c-focusable-unit-lookup.log` |
| Agent C | Selected order snapshot | `SelectedUnitOrderSnapshotSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-c-selected-order-snapshot.log` |
| Agent C | Command request/result contract | `SelectionCommandRequestResultContractTests.RunBatchValidation` | `/private/tmp/warline-phase7-agent-c-command-contract.log` |
| Agent C | Selection/order markers | `SelectionOrderMarkerSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-c-selection-order-marker.log` |
| Agent C | Movement hold/stop/scan interaction | `UnitMovementBlockerValidationTests.RunHoldCommandFocusedValidation` and `UnitMovementBlockerValidationTests.RunBatchValidation` | `/private/tmp/warline-phase7-agent-c-hold-stop-scan.log` |
| Agent D | Building placement command | `BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation` | `/private/tmp/warline-phase7-agent-d-building-placement-command.log` |
| Agent D | Building placement runtime tick | `BuildingPlacementRuntimeTickSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-d-building-placement-runtime.log` |
| Agent D | Building runtime boundary | `BuildingRuntimeBoundaryValidationTests.RunBatchValidation` | `/private/tmp/warline-phase7-agent-d-building-runtime-boundary.log` |
| Agent D | Building production | `BuildingProductionSystemTests.RunProductionRequestValidation` | `/private/tmp/warline-phase7-agent-d-building-production.log` |
| Agent D | Building composition smoke | `BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation` | `/private/tmp/warline-phase7-agent-d-building-composition-smoke.log` |
| Agent D | Building UI query | `BuildingUiQuerySystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-d-building-ui-query.log` |
| Agent D | Building combat | `BuildingCombatSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-d-building-combat.log` |
| Agent D | Building selection marker | `BuildingSelectionMarkerSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-d-building-selection-marker.log` |
| Agent D | Building faction visual | `BuildingFactionVisualSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-d-building-faction-visual.log` |
| Agent E | Road build command | `RoadBuildCommandSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-e-road-build.log` |
| Agent E | Runtime city generation | `RuntimeCityGenerationFocusedTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-e-runtime-city-generation.log` |
| Agent E | Movement/pathing smoke | `UnitMovementBlockerValidationTests.RunBatchValidation` | `/private/tmp/warline-phase7-agent-e-movement.log` |
| Agent E | Citizen movement | `CitizenMovementCommandSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-e-citizen-movement.log` |
| Agent E | Map surface diagnostics | `MapSurfaceDiagnosticsSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-e-map-surface-diagnostics.log` |
| Agent F | Render budget | `UnitRenderBudgetSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-f-render-budget.log` |
| Agent F | Vehicle visual adornments | `VehicleVisualAdornmentsSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-f-vehicle-visual-adornments.log` |
| Agent F | Ground missile projectile dependencies | `GroundMissileLauncherRuntimeTests.RunProjectileDependencyValidation` | `/private/tmp/warline-phase7-agent-f-ground-missile-projectile.log` |
| Agent F | Ground missile visuals | `GroundMissileLauncherRuntimeTests.RunMissileVisualValidation` | `/private/tmp/warline-phase7-agent-f-ground-missile-visual.log` |
| Agent F | Ground missile attack | `GroundMissileLauncherRuntimeTests.RunAttackFocusedValidation` | `/private/tmp/warline-phase7-agent-f-ground-missile-attack.log` |
| Agent F | Radar/missile runtime | `MissileLauncherRadarAttackValidationTests.RunRuntimeFocusedValidation` | `/private/tmp/warline-phase7-agent-f-radar-missile-runtime.log` |
| Agent F | Building destroyed visual | `BuildingDestroyedVisualSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-f-building-destroyed-visual.log` |
| Agent F | Visual quality | Use Unity Test Runner or add a focused runner in the slice if no `RunFocusedValidation` exists for the touched visual-quality file | `BlockedMissingRunner` until runner exists |

Acceptance:

- Every inventory owner lane has at least one focused validation gate.
- Final Phase 7 cannot close without the matrix passing or explicit deferred validation owner.
- Each domain handoff can cite validation gates by name instead of inventing new ones.

## A4 - Integration Workflow

Goal:
Allow B-F to work in parallel without creating merge chaos.

Current integration mode:

- Status: `SingleThreadDomainExecutionApproved`.
- Checked command: `find Design/AgentReports -maxdepth 1 -name '2026-*_phase7_agent_*_handoff.md' -print | sort`.
- Result at last check: no Agent B-F Phase 7 handoff reports are present yet.
- Parallel branch merge checklist items stay open until actual external handoffs/branches exist.
- Current heartbeat should not block on missing handoffs. It should proceed to the next lane tracker in the approved sequence and treat each lane slice as the handoff source of truth.
- Required lane order for this single-thread automation: Agent B direct/startup, Agent C selection/commands, Agent F request-contract slice, Agent D building/production, Agent E road/city/citizen, then Agent F final visual/camera exceptions.

- [x] Define branch names in every agent tracker and verify they are unique.
- [x] Create or name an integration branch, for example `codex/phase7-integration`.
- [x] Require each domain agent to write `Design/AgentReports/YYYY-MM-DD_phase7_agent_<lane>_handoff.md`.
- [x] Require each handoff to include files changed, inventory ids touched, systems converted, systems split, managed exceptions created/retained, validations run, validation logs, and blockers.
- [x] Require each handoff to declare whether it touched shared components/contracts, asmdefs, tests, or generated inventory.
- [x] Require each handoff to list expected conflicts before merge.
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

Branch map:

| Lane | Branch | Verified source |
| --- | --- | --- |
| Agent A | `codex/phase7-agent-a-inventory-guardrails` | This tracker |
| Agent B | `codex/phase7-agent-b-direct-startup` | `Design/Architecture/phase7_agent_b_direct_startup_tracker.md` |
| Agent C | `codex/phase7-agent-c-selection-commands` | `Design/Architecture/phase7_agent_c_selection_commands_tracker.md` |
| Agent D | `codex/phase7-agent-d-building-production` | `Design/Architecture/phase7_agent_d_building_production_tracker.md` |
| Agent E | `codex/phase7-agent-e-road-city-citizen` | `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md` |
| Agent F | `codex/phase7-agent-f-rendering-vfx` | `Design/Architecture/phase7_agent_f_rendering_vfx_tracker.md` |
| Integration | `codex/phase7-integration` | Named by Agent A; create this branch only when first domain merge is ready |

Domain handoff contract:

- Required handoff path: `Design/AgentReports/YYYY-MM-DD_phase7_agent_<lane>_handoff.md`.
- Required template: `Design/AgentReports/phase7_domain_handoff_template.md`.
- Required contents: inventory ids touched, files changed, systems converted/split/retired, managed exceptions retained or created, shared components/contracts/asmdefs/tests/generated-inventory touches, validations run, log paths, blockers, deferred validations, and expected conflicts.
- Domain agents must not edit this tracker, the main Phase 7 tracker, or unrelated lane rows. They record progress in their own tracker and write the handoff for Agent A.
- Agent A merges one domain branch at a time into `codex/phase7-integration`, resolves conflicts with the handoff open, then updates the shared inventory and main tracker.

Single-thread automation contract:

- The heartbeat may continue domain work directly from this thread after Agent A baseline work.
- Before editing code for a lane, read that lane's tracker and the relevant inventory rows.
- Keep one active lane at a time. Do not mix B-F implementation slices in one validation batch.
- Update the active lane tracker progress as tasks complete or block.
- Regenerate `Design/Architecture/systembase_to_isystem_inventory.md` after each completed lane slice.
- Run `git diff --check`, the Agent A architecture guard, and the active lane focused validations before moving to the next slice.
- Update the main Phase 7 tracker only after validation passes or a blocker is explicitly documented.

Per-merge Agent A command sequence:

```bash
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py \
  --root Assets/Game/Scripts \
  --output Design/Architecture/systembase_to_isystem_inventory.md \
  --json-output /private/tmp/warline-phase7-systembase-inventory.json

git diff --check

dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal

/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture-Clone \
  -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation \
  -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Acceptance:

- No two agents edit the main Phase 7 tracker concurrently.
- No two agents edit the same inventory rows concurrently.
- Merge conflicts are handled by Agent A with the domain handoff open.
- Inventory counts are updated after every merge.

## A5 - Progress Accounting And Percentage Formula

Goal:
Keep user-facing progress and final ratio honest.

- [x] Count production `SystemBase` under `Assets/Game/Scripts`, excluding editor and tests.
- [x] Count production `ISystem` under `Assets/Game/Scripts`, excluding editor and tests.
- [x] Count UI `SystemBase` separately.
- [x] Count non-UI gameplay `SystemBase` separately.
- [x] Count managed presentation/config/camera `SystemBase` exceptions separately.
- [x] Count view/reference-only MonoBehaviours separately only when they replace old managed ownership.
- [x] Count converted `ISystem` processors created by Phase 7.
- [x] Count retired/folded systems.
- [x] Count split managed exceptions retained.
- [x] Count remaining `ReviewRequired` rows.
- [x] Recalculate final `ISystem` share after every integration merge.

## Current Validation Record

Commands run for the current Agent A slice:

```bash
python3 -B -c "import py_compile; py_compile.compile('Tools/Architecture/generate_systembase_to_isystem_inventory.py', cfile='/private/tmp/generate_systembase_to_isystem_inventory.pyc', doraise=True)"
python3 -B -c "import py_compile; py_compile.compile('Tools/Architecture/generate_phase7_monobehaviour_loop_baseline.py', cfile='/private/tmp/generate_phase7_monobehaviour_loop_baseline.pyc', doraise=True)"
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json
python3 Tools/Architecture/generate_phase7_monobehaviour_loop_baseline.py --root Assets/Game/Scripts --output Design/Architecture/phase7_monobehaviour_loop_baseline.md
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
git diff --check
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Results:

- Inventory generator syntax validation passed; bytecode target `/private/tmp/generate_systembase_to_isystem_inventory.pyc`.
- MonoBehaviour loop baseline generator syntax validation passed; bytecode target `/private/tmp/generate_phase7_monobehaviour_loop_baseline.pyc`.
- Inventory regenerated at `Design/Architecture/systembase_to_isystem_inventory.md`; JSON sidecar `/private/tmp/warline-phase7-systembase-inventory.json`.
- MonoBehaviour loop baseline generated at `Design/Architecture/phase7_monobehaviour_loop_baseline.md` with `40` existing loop keys.
- Manual review queue resolved to `0` rows; `42` reviewed rows are recorded under `Manual Review Decisions`.
- Phase 7-created converted `ISystem` processors currently total `8`; current `RetireFold` classification count is `32`; Agent C folded helper count is `18`; view/reference-only MonoBehaviour replacement count is `0`.
- Editor assembly compile passed with `0 Warning(s), 0 Error(s)`.
- `git diff --check` passed.
- Agent A focused architecture guard passed with `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- User approved single-thread domain execution; Agent B and Agent C baseline slices are complete/held where documented, P7-0281, P7-0259, P7-0249, and P7-0251 in the Agent F request-contract slice are completed/validated, and the heartbeat should continue with the next low-risk Agent F request-contract row rather than block on external handoffs.

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

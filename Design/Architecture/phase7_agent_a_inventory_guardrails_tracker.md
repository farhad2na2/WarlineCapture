# Phase 7 Agent A Tracker - Inventory, Guardrails, And Integration

Purpose:
Own the authoritative Phase 7 denominator, migration guardrails, validation harness, and integration discipline for the non-UI gameplay `SystemBase` to `ISystem` migration. This agent is the merge captain lane. It does not convert domain systems except tiny test fixtures required to prove guardrails.

Branch:
`codex/phase7-agent-a-inventory-guardrails`

Progress snapshot:

- Checklist progress: `0 / 34 complete (0.0%)`.
- In progress: `0`.
- Remaining open: `34`.
- Current target: `A0 - authoritative inventory generator`.
- Runtime production baseline: `255 SystemBase`, `126 ISystem` under `Assets/Game/Scripts`.
- Non-UI gameplay production target after Phase 7: `0 SystemBase`.
- Allowed production SystemBase after Phase 7: `UiToolkitShellApplySystem` plus counted managed presentation/config/camera exceptions only; no editor/test counting in the production denominator.
- Managed presentation exception planning cap: `<= 30 non-UI SystemBase` until Agent A generates the authoritative inventory.
- Updating MonoBehaviour target after Phase 7: `0 newly introduced Update/LateUpdate/FixedUpdate/coroutine loops`.
- Planning projection: `350 ISystem / 31 SystemBase = 91.9% ISystem share` if the managed-exception cap is met and non-exception SystemBase targets convert one-to-one.
- Validation status: `not started`.

Ownership:

- Owns `Design/Architecture/systembase_to_isystem_inventory.md`.
- Owns architecture tests and validation runners for Phase 7.
- Owns cross-agent merge sequencing and final progress snapshots.
- Owns main tracker updates in `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`.
- Owns shared rule changes only after reading all active agent handoffs.

Do not touch:

- Domain implementation files except when adding minimal fixtures for architecture tests.
- UI Toolkit/Canvas migration code.
- Agent B-F tracker progress except to merge final reviewed status.

Shared rules:

- Do not allow new non-UI runtime `SystemBase` unless it has an explicit inventory row and managed presentation/config/camera exception.
- Do not allow converted `ISystem` files to reference `GameObject`, `Transform`, `Camera`, `UnityEngine.Object`, `ScriptableObject`, `Resources`, `Object.Instantiate`, `Object.Destroy`, `Find*`, `Camera.main`, hierarchy paths, managed component classes, `List<GameObject>`, `Dictionary<..., GameObject>`, or mutable static gameplay state.
- Do not mark a target complete if gameplay policy remains in a managed presentation/config/camera exception.
- Do not allow Phase 7 to introduce `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loops, or manager-style MonoBehaviour tickers. MonoBehaviours are view/reference holders only.
- Do not let domain agents edit shared trackers directly; they should write handoffs under `Design/AgentReports/`.

## A0 - Authoritative Inventory Generator

- [ ] Create `Tools/Architecture/generate_systembase_to_isystem_inventory.py` or equivalent C# architecture test.
- [ ] Enumerate every declaration of `SystemBase` and `ISystem` under `Assets/Game/Scripts`.
- [ ] Exclude `Assets/Game/Scripts/UI` from the non-UI denominator, but list UI systems separately.
- [ ] Exclude `Assets/Tests` and editor-only paths from the production denominator, but list them separately.
- [ ] Record file path, type name, kind, accessibility, namespace, assembly, line number, and current inheritance.
- [ ] Record public/internal methods and properties that composition code may call.
- [ ] Record `OnCreate`, `OnStartRunning`, `OnUpdate`, `OnStopRunning`, and `OnDestroy` presence.
- [ ] Record managed field categories: Unity object, managed collection, public helper state, native container, query/lookup/cache.
- [ ] Emit a stable markdown inventory to `Design/Architecture/systembase_to_isystem_inventory.md`.
- [ ] Add generation timestamp, command used, and source commit hash to the inventory.
- [ ] Run the generator twice and confirm stable output ordering.

Acceptance:

- Inventory row count matches the declaration scan.
- Each non-UI production `SystemBase` appears exactly once.
- UI/test/editor exclusions are visible, not hidden.

## A1 - Classification Columns

- [ ] Add disposition column: `DirectConvert`, `SplitThenConvert`, `RetireFold`, `ManagedPresentationSystemBaseException`, `ViewReferenceOnlyMonoBehaviour`, `UIOutOfScope`, `EditorOutOfScope`, `TestOutOfScope`, `ReviewRequired`.
- [ ] Add owner lane column: `AgentB`, `AgentC`, `AgentD`, `AgentE`, `AgentF`, or `Integration`.
- [ ] Add managed blocker column with concrete blocker names.
- [ ] Add first safe slice column for each candidate.
- [ ] Add required validation command column.
- [ ] Add current status column: `Open`, `InProgress`, `Converted`, `Split`, `Retired`, `PassiveBoundary`, `Deferred`.
- [ ] Add converted/replacement type column for targets that move to `ISystem`.

Acceptance:

- No row is left unclassified unless marked `ReviewRequired` with a reason.
- No row is marked as a managed exception without a concrete Unity-object ticking blocker.
- No row can introduce an updating MonoBehaviour bridge.

## A2 - Guardrail Tests

- [ ] Add `NonUiSystemBaseMigrationArchitectureTests`.
- [ ] Add test that fails when a new non-UI runtime `SystemBase` appears without an inventory row.
- [ ] Add test that fails when an inventory row points to a deleted or renamed file.
- [ ] Add test that converted targets cannot regain `SystemBase`.
- [ ] Add test that completed `ISystem` files avoid managed Unity object blockers.
- [ ] Add test that managed presentation `SystemBase` exceptions cannot contain command execution, simulation, gameplay validation, or gameplay ECS mutation policy.
- [ ] Add test that no Phase 7 change introduces new `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, or coroutine loops.
- [ ] Add test that broad replacement `ISystem` classes are flagged for manual review if they exceed documented responsibility limits.
- [ ] Add test that new runtime non-UI ECS systems default to `ISystem`.
- [ ] Add test that public helper APIs on converted systems are replaced by ECS request/result or plain helper functions.

Acceptance:

- Guardrails pass on the current baseline before any domain conversion.
- A deliberate local violation fails the expected guard, then is reverted.
- A deliberate updating-MonoBehaviour violation fails the expected guard, then is reverted.

## A3 - Validation Matrix And Runner

- [ ] Create a Phase 7 validation matrix with compile, architecture, EditMode, PlayMode, and performance gates.
- [ ] Map Agent B direct/startup systems to startup, diagnostics, and architecture validations.
- [ ] Map Agent C selection systems to selection input, command-result, hold/stop/scan, board/attack, and allocation validations.
- [ ] Map Agent D building systems to placement, production, build drawer, building selection, combat, and placement-to-production PlayMode validations.
- [ ] Map Agent E road/city/citizen systems to road build, runtime city, blocker, citizen, movement, and match smoke validations.
- [ ] Map Agent F rendering/VFX systems to render budget, vehicle visual, missile VFX, attached light, marker, visual-quality, and graphics smoke validations.
- [ ] Add one command or runner entry per validation gate where practical.

Acceptance:

- Every inventory owner lane has at least one focused validation gate.
- Final Phase 7 cannot close without the matrix passing.

## A4 - Integration Workflow

- [ ] Define branch naming and merge order in this document.
- [ ] Require each domain agent to write `Design/AgentReports/YYYY-MM-DD_phase7_agent_<lane>_handoff.md`.
- [ ] Require each handoff to include files changed, systems converted, systems split, managed exceptions created/retained, validations run, and blockers.
- [ ] Merge one domain branch at a time into an integration branch.
- [ ] After each merge, regenerate inventory and compare counts.
- [ ] Update the main tracker only from the integration branch.
- [ ] Run `git diff --check` after every merge.
- [ ] Run architecture guardrails after every merge.

Acceptance:

- No two agents edit the main Phase 7 tracker concurrently.
- Merge conflicts are handled by Agent A with the domain handoff open.

## A5 - Final Completion

- [ ] Regenerate inventory after all domain lanes are merged.
- [ ] Confirm non-UI gameplay production `SystemBase` count is `0`.
- [ ] Confirm production `SystemBase` count is `1 + counted managed presentation/config/camera exceptions`.
- [ ] Confirm final `ISystem` and `SystemBase` percentage.
- [ ] Confirm no new updating MonoBehaviour was introduced by Phase 7.
- [ ] Raise architecture guard floors.
- [ ] Run full Phase 7 validation matrix.
- [ ] Write final report under `Design/AgentReports`.
- [ ] Update the main tracker to close Phase 7.

Final handoff format:

- Completed checklist items over total.
- Remaining open items.
- Current production `SystemBase` count.
- Current non-UI production `SystemBase` count.
- Current `ISystem` count.
- Converted systems.
- Split managed presentation/config/camera exceptions.
- Validation commands and logs.
- Known deferred work.

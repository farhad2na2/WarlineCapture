# Phase 7 Agent C Tracker - Selection, Focus, And Command Systems

Purpose:
Convert selection and command ECS policy to focused `ISystem` processors while keeping pointer, camera, UI, and presentation work in managed ECS boundaries or view/reference-only MonoBehaviours as required by the architecture contract.

Branch:
`codex/phase7-agent-c-selection-commands`

Progress snapshot:

- Checklist progress: `0 / 49 complete (0.0%)`.
- In progress: `0`.
- Remaining open: `49`.
- Current target: `C0 - selection inventory intake`.
- Direct conversions completed: `0`.
- Split command processors created: `0`.
- Passive input boundaries created: `0`.
- Validation status: `not started`.

Ownership:

- Owns `RtsSelection*`, `Selection*`, focused-unit, selected-state, command-result, and selection read-model systems assigned by Agent A.
- Owns command request/result ECS contracts only when they are selection-domain contracts.
- Coordinates with Agent D for building selection and with Agent F for selection/order marker visuals.

Do not touch:

- UI Toolkit and Canvas implementation.
- Building placement/production behavior except through agreed request/result contracts.
- Camera presentation code except passive input boundary wiring.
- Main Phase 7 tracker except through handoff reports.
- Any new `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loop, or manager-style MonoBehaviour ticker. MonoBehaviours are view/reference holders only.

Candidate examples to verify, not pre-approved:

- `RtsSelectionRuntimeInputSystem`
- `RtsSelectionInputStateSystem`
- `SelectionStateSystem`
- `FocusableUnitLookupSystem`
- `FocusedUnitLifecycleSystem`
- `FocusedUnitUiReadModelSystem`
- `RtsSelectionCommandResultFlushSystem`
- `RtsSelectionFocusCommandSystem`
- `SelectionBuildingInteractionSystem`
- `VisibleUnitSelectionSystem`
- `SelectionScreenMarkerSystem` only in coordination with Agent F.

## C0 - Selection Inventory Intake

- [ ] Wait for Agent A inventory and guardrails.
- [ ] Pull all Agent C rows and inspect call sites.
- [ ] Map every public helper API currently called by UI, camera, bootstrap, or building systems.
- [ ] Identify camera raycast, pointer, and UI-click suppression code that must remain in ECS managed boundaries or view/reference-only objects.
- [ ] Identify pure ECS command/state code that can become `ISystem`.
- [ ] Identify dependencies on building selection, markers, transport boarding, and attack target requests.
- [ ] Produce an initial dependency graph in the Agent C handoff.

Acceptance:

- Every selection target is classified as direct, split, managed presentation/config/camera exception, view/reference-only, or cross-agent blocked.
- No camera/UI/pointer dependency is planned for unmanaged `ISystem`.

## C1 - Request/Result Boundary First

- [ ] Define or reuse ECS request components/buffers for selection commands that currently enter through managed helper methods.
- [ ] Define or reuse result buffers for command feedback and UI read-model publication.
- [ ] Replace public managed command helper calls one call site at a time.
- [ ] Keep UI and input boundaries writing data only.
- [ ] Add validation that command request data is produced without direct gameplay mutation from UI/pointer code.
- [ ] Confirm command feedback still reaches the UI shell read-model.

Acceptance:

- Managed input boundaries enqueue requests only.
- ECS processors own command validation and mutation.

## C2 - Input State And Mode Systems

- [ ] Split pointer/camera raycast capture from ECS selection mode state.
- [ ] Convert selection mode command processors to `ISystem` where data-only.
- [ ] Convert attack/move/scan/cancel mode state processors to `ISystem` where data-only.
- [ ] Keep camera-dependent screen-space checks outside converted systems.
- [ ] Use explicit components for active command mode, target candidate, suppression state, and command result.
- [ ] Run selection input validation after each slice.

Acceptance:

- Converted systems do not touch `Camera`, `Transform`, `GameObject`, UI, or pointer APIs.
- Selection command modes behave the same as before.
- No selection bridge adds MonoBehaviour `Update`, `LateUpdate`, `FixedUpdate`, or coroutine loops.

## C3 - Selected State And Focus Lookup

- [ ] Inspect `SelectionStateSystem`, `FocusableUnitLookupSystem`, and focused-unit lifecycle/read-model systems.
- [ ] Split managed lookup caches or public helper APIs into ECS buffers/components.
- [ ] Convert selected-state mutation to `ISystem`.
- [ ] Convert focusable-unit lookup to `ISystem` only after camera/screen dependencies are pre-resolved.
- [ ] Convert focused-unit command readiness/read-model projection to `ISystem` when it only reads ECS data.
- [ ] Add validations for selected unit state, focus change, hold/stop/scan availability, and attack target selection.

Acceptance:

- Focus state is ECS-owned.
- UI display uses read-model data and does not own selection policy.

## C4 - Command Result Flush And Feedback

- [ ] Replace managed command result flush helpers with ECS result buffers.
- [ ] Convert command result publication to `ISystem` after UI read-model boundary exists.
- [ ] Preserve same-frame feedback requirements where documented.
- [ ] Confirm command result buffers are cleared deterministically.
- [ ] Run board, attack, hold, stop, scan, and move-target validations.

Acceptance:

- Feedback behavior matches current Canvas/UI Toolkit shell expectations.
- No stale command feedback remains after mode cancellation.

## C5 - Cross-Agent Coordination

- [ ] Coordinate with Agent D before changing building selection interactions.
- [ ] Coordinate with Agent F before touching selection/order marker visuals.
- [ ] Coordinate with Agent B if `SelectionRuntimeDiagnosticsSystem` is classified as diagnostics instead of selection policy.
- [ ] Record all shared contract changes in the handoff.

Acceptance:

- No shared contract is changed without naming the affected agent.

## C6 - Agent C Completion

- [ ] Run `git diff --check`.
- [ ] Run selection input validation.
- [ ] Run command/result validation.
- [ ] Run hold/stop/scan focused validation.
- [ ] Run board/attack focused validation.
- [ ] Run allocation/performance smoke for selection hot paths.
- [ ] Run architecture guardrails.
- [ ] Write `Design/AgentReports/YYYY-MM-DD_phase7_agent_c_selection_commands_handoff.md`.

Handoff format:

- Checklist progress.
- Converted systems.
- Split managed input/presentation boundaries.
- ECS request/result contracts added or changed.
- Cross-agent dependencies.
- Validation commands and logs.
- Remaining blockers.

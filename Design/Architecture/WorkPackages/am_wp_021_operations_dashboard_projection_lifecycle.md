# AM-WP-021 - Operations Dashboard Projection And Lifecycle

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-026` proves the authoritative Operations state/save/simulation owners and every dashboard caller, `AM-027` accepts the projection contract, `AM-028` accepts lifecycle ownership, and `AM-033` dispatches this package.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-026`.

## 1. Current Ownership And Risk

- `MenuOverlayRoutePresentation.InstallOperationsBody()` installs the serialized Operations prefab into `PopupLayer` and preserves the shared Main Menu header, but performs no data binding.
- `OperationsDashboardScreenView` exposes readiness cards, district/warning buttons, map, briefing, command actions, title, and day label as serialized references only. It owns no runtime projection, semantic version, or lifecycle binding.
- Current tests prove route/history behavior, assigned presentation references, production art, and intentionally disabled controls. They do not prove live Operations state or unchanged-state allocation behavior.
- Design documents require district metrics/actions, warnings, daily briefing, save checks, and End Day simulation. Some documents claim `OperationDashboardScreenController`, `WarlineCaptureOperationRuntime`, `OperationService`, and `OperationSimulationService` implementations, but no matching production C# classes were found in the current `Assets` tree.
- Black Market and Armory route buttons are active; district, warning, Intel Report, Command Log, and End Day controls are intentionally unavailable until their owners exist.

Risks are presenting placeholders as live state, enabling actions without authoritative validation/save ownership, implementing from stale documentation, duplicate or stale route bindings, recalculating strategic simulation in UI, and treating a missing projection as zero-allocation success.

## 2. Accepted Future Ownership

- One authoritative Operations domain owns persisted operation/day state, district metrics, warnings/events, readiness, action availability, pending missions, simulation, and save completion. UI never calculates district consequences or End Day outcomes.
- One immutable dashboard snapshot crosses the domain-to-UI boundary with stable district/warning row identities and semantic versions.
- One route-scoped presentation owner binds `OperationsDashboardScreenView`, performs keyed changed-only row applies, and releases every listener/reference on route/root replacement.
- District and warning actions carry exact item/state versions and are revalidated by the domain before routing. Disabled reasons come from the authoritative contract.
- Back remains disabled while an accepted save/end-day operation is unresolved. End Day remains disabled until the domain proves no blocking required action and publishes an executable request contract.
- Existing honest unavailable states remain until their exact destinations and data owners are executable. Static design copy is not upgraded to runtime authority.
- Gateway reads are pure; publication and action acknowledgement are explicit commands. No recurring UI poller is allowed.

No `SystemBase`, default-World lookup, view-local polling, UI-owned simulation/save authority, mutable global state, broad manager/controller/provider/service type, scene/prefab edit, or activation of unavailable controls without owner handoff is allowed.

## 3. Identity And Invalidation Contract

Minimum identity:

- World, Operations boundary, persisted profile/save slot, and shell root generations;
- operation/campaign, day, selected district, and dashboard snapshot versions;
- district collection structural version plus stable district ID and per-row semantic version;
- warning/event collection structural version plus stable warning ID and per-row semantic/actionability version;
- readiness, daily briefing, map state, resource, pending mission, save state, and End Day eligibility versions;
- route/binding, localization, formatting/theme, accessibility/layout, and explicit invalidation generations.

Rules:

- Equal composite identity produces zero managed-model rebuild, row churn, TMP/Image write, layout rebuild, or action rebind.
- Structural changes reconcile rows by stable ID; reordering does not recreate unchanged rows.
- A control submits the exact snapshot/item identity it displayed. Stale requests fail closed and trigger one version refresh, not local correction.
- Save/End Day pending state survives presentation replacement only through domain identity; the view cannot infer completion.
- Empty/missing collections clear previous rows and selected state once.
- Route exit, root/World/boundary replacement, profile change, and subsystem registration invalidate once and release listeners/references idempotently.
- Reads never initialize domain state, run simulation, save, or mutate action availability.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Screens/OperationsDashboardScreenView.cs`
- `Assets/Game/Scripts/UI/Shell/MenuOverlayRoutePresentation.cs` only for dashboard binding/lifecycle disposition
- exact shell content/root lifecycle files proven by `AM-026` and appended before dispatch
- one narrowly named Operations dashboard contract under `Assets/Game/Scripts/UI/Contracts/`
- one narrowly named Operations dashboard projection presentation under `Assets/Game/Scripts/UI/Screens/`
- exact Operations-domain read/action contracts only after written owner handoff

Test files allowed:

- `Assets/Tests/Editor/OperationsDashboardScreenTests.cs`
- `Assets/Tests/Editor/OperationsDashboardProjectionLifecycleTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/OperationsDashboardAllocationValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_021_operations_dashboard_projection_lifecycle_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_021_operations_dashboard_projection_lifecycle_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-026`, `AM-027`, `AM-028`, `AM-033`, and `AM-035` tracker records and progress snapshot

Read-only dependencies: Operations/gameplay design, save/profile ownership, resource/economy state, navigation, localization, visual-lock assets, scenes, and prefabs.

Hard exclusions: operation-map/static-map, FirstLaunch, audio, unrelated gameplay, scenes, prefabs, visual-lock art, packages, `ProjectSettings`, and any Operations simulation, persistence, economy, action, copy, or visual-policy change without explicit owner approval.

This package is serialized with other packages claiming shared shell route/lifecycle files.

## 5. Characterization Matrix

Required before edits:

1. fresh/default, loaded, partially progressed, and completed operation/day state for every supported profile/save identity;
2. zero/one/multiple districts and warnings, stable reordering, add/remove, selected item removal, and duplicate/missing IDs;
3. readiness/district metric changes, daily briefing changes, warning actionability changes, map changes, and resource changes;
4. Back, district, warning, Intel Report, Black Market, Armory, Command Log, and End Day in every approved enabled/disabled/pending state;
5. save success/failure/in-progress, End Day accepted/rejected/completed, repeated taps, stale actions, and pending mission routing;
6. route open/close/reopen, Main Menu history, popup overlap, root/World/boundary/profile replacement, pause/resume, and localization/theme/layout changes;
7. `100` route bind/unbind/replacement cycles with exact listener, row-instance, request, snapshot, and retained-reference counts.

Record source publications, identity reads, rebuilds, keyed row creates/removes/reuses, managed bytes, TMP/Image writes, layout rebuilds, listeners, action requests/results, saves, simulations, and CPU time.

## 6. Baseline And Acceptance Gates

Measure `180` warmup plus `300` unchanged open frames for representative empty and populated dashboard snapshots; changed rows, selection, action availability, localization/layout, route lifecycle, save, and End Day transitions are measured separately.

Acceptance:

- one authoritative Operations source, one route-scoped projection owner, and one domain action/save/simulation owner are proven;
- no placeholder or stale design claim is presented as live state, and unavailable controls remain honestly unavailable until executable;
- equal snapshots perform zero recurring production-owned managed allocation, model rebuild, row churn, TMP/Image write, layout rebuild, listener change, polling, or ECS structural work;
- one semantic source change applies at most once; keyed rows preserve unchanged instances and clear removed state;
- stale actions fail closed; Back/End Day/save behavior follows authoritative pending and eligibility state without UI simulation;
- `100` lifecycle cycles leave zero duplicate listeners, rows, requests, stale snapshots/views, or retained World/domain references;
- existing shared-header, route-history, honest-action-state, and production-art tests remain valid;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, authoritative owner/version contract, source hashes, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most four independently stable commits after authoritative Operations owner and caller characterization:

1. reconcile stale implementation claims and establish the exact immutable dashboard identity/version contract;
2. one route-scoped changed-only dashboard projection with keyed district/warning rows;
3. domain-validated actions, save/End Day pending behavior, and honest unavailable-state integration;
4. lifecycle and allocation acceptance.

Rollback if UI calculates Operations outcomes, save/simulation/action behavior regresses, unavailable controls become misleadingly active, stale state crosses profile/World identity, unchanged work remains, or the slice introduces `SystemBase`, polling, default-World discovery, mutable global authority, protected-file edits, or non-allowlisted overlap.

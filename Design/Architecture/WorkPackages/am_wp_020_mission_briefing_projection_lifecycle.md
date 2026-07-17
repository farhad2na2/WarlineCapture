# AM-WP-020 - Mission Briefing Projection And Lifecycle

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-026` proves authoritative mission/progression/reward-preview/scenario/launch owners and every briefing caller, `AM-027` accepts the projection contract, `AM-028` accepts lifecycle ownership, and `AM-033` dispatches this package.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-025`.

## 1. Current Ownership And Risk

- `MenuOverlayRoutePresentation.InstallMissionBriefingBody()` installs the serialized briefing prefab but performs no data binding.
- `MissionBriefingScreenView` exposes serialized title, objectives, stars, threat/intel, rewards, progress, navigation, and Deploy references only. It has no model, semantic version, binder, update loop, or lifecycle owner.
- `CampaignOperationsScreenView` launches a generic Mission Briefing route without carrying selected mission identity. The shell gateway has no briefing read or launch command contract.
- `ScenarioSetupConfig` owns only scenario ID, operation-map ID, and required anchors; it does not own briefing content, objectives, stars, intel, reward preview, planning camera, minimap, progression, or Deploy eligibility.
- The current prefab contains fixed M01 content and rewards, while Deploy is deliberately disabled. Existing tests prove route/history, hierarchy, art, and that honest unavailable state, not an executable projection.
- Design requires a broader briefing contract and claims several mission/session/result/reward runtime owners that are absent from current production scripts. Player-facing title and planning-camera/minimap identities also drift across documents and prefab copy.

Risks are launching the wrong mission, exposing placeholder rewards as authoritative, UI-calculated progression or eligibility, stale mission data across routes, conflicting map identities, duplicate listeners, and treating a missing projection as zero-allocation success.

## 2. Accepted Future Ownership

- Mission/campaign progression owns selected mission identity, availability, authored briefing/localization data, objectives, star goals, enemy-intel payload/confidence, and source route.
- Reward ownership publishes a canonical preview with first-clear/replay semantics; UI never calculates or grants rewards.
- Scenario/launch ownership validates scenario, operation-map, planning-camera, minimap, loadout, and launch payload identities and creates the active mission session. UI never fills missing identities from display copy.
- One immutable, semantically versioned briefing snapshot crosses into one route-scoped projection owner. Stable objective/reward row IDs support changed-only reconciliation.
- Deploy remains disabled until every required mission, progression, reward-preview, scenario, loadout, save, and launch validation succeeds. A submitted request carries the exact displayed snapshot identity and is revalidated by the launch owner.
- Back preserves shell history and source-route identity. Route/root/World/session replacement releases the projection and listeners idempotently.
- Gateway reads are pure and fail closed when no complete authoritative snapshot exists. No recurring polling is allowed.

No `SystemBase`, default-World lookup, view-local polling, UI-authored mission/reward/eligibility authority, mutable global session, broad manager/controller/provider/service type, scene/prefab edit, or activation of Deploy from placeholder data is allowed.

## 3. Identity And Invalidation Contract

Minimum identity:

- World, gameplay boundary, profile/save slot, shell root, and route/binding generations;
- mission ID, source route/mode, mission authored-data version, and campaign/progression version;
- objective/star-goal collection structural version plus stable row IDs and semantic versions;
- enemy-intel/confidence, reward-preview/first-clear, mission availability, and Deploy eligibility versions;
- scenario ID/config, operation-map ID/version, planning-camera ID/version, minimap ID/version, loadout, and launch-session generations;
- localization, formatting/theme, accessibility/layout, and explicit invalidation generations.

Rules:

- Equal composite identity produces zero model rebuild, string conversion, row churn, TMP/Image write, layout rebuild, or action rebind.
- Structural changes reconcile objective/reward rows by stable ID; empty/missing optional data clears previous content once.
- A briefing cannot display or submit mixed identities from different missions, sources, scenarios, maps, cameras, or reward previews.
- Deploy carries the exact snapshot/launch identity; stale or incomplete requests fail closed and trigger one version refresh.
- Reopening while awaiting a new snapshot shows a defined loading/unavailable state, never the previous mission.
- Back, route replacement, World/boundary/profile/session replacement, and subsystem registration invalidate once and release listeners/references idempotently.
- Reads never initialize mission state, grant rewards, mutate progression, or create a session.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Screens/MissionBriefingScreenView.cs`
- `Assets/Game/Scripts/UI/Screens/CampaignOperationsScreenView.cs` only for exact selected-mission route payload after owner handoff
- `Assets/Game/Scripts/UI/Shell/MenuOverlayRoutePresentation.cs` only for briefing binding/lifecycle disposition
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs` only for briefing binding/release points
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs` only for the approved briefing read/launch contract
- `Assets/Game/Scripts/UI/Shell/UiShellRuntimeGateway.cs` only for briefing contract/lifecycle disposition
- exact ECS gateway partials only after the immutable source contract is approved and appended before dispatch
- one narrowly named briefing model contract under `Assets/Game/Scripts/UI/Contracts/`
- one narrowly named briefing projection presentation under `Assets/Game/Scripts/UI/Screens/`
- exact mission/progression/reward/scenario/launch contracts only after written owner handoff

Test files allowed:

- `Assets/Tests/Editor/MissionBriefingScreenTests.cs`
- `Assets/Tests/Editor/MissionBriefingProjectionLifecycleTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/MissionBriefingAllocationValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_020_mission_briefing_projection_lifecycle_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_020_mission_briefing_projection_lifecycle_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-026`, `AM-027`, `AM-028`, `AM-033`, and `AM-035` tracker records and progress snapshot

Read-only dependencies: mission/campaign design, scenario/map/camera/minimap configs, progression/rewards, loadout/launch, save ownership, navigation, localization, visual-lock assets, scenes, and prefabs.

Hard exclusions: operation-map/static-map implementation, FirstLaunch, audio, unrelated gameplay, reward granting, campaign persistence, scenes, prefabs, visual-lock art, packages, `ProjectSettings`, and any mission identity/copy/reward/eligibility/launch-policy change without explicit owner approval.

This package is serialized with other packages claiming shared shell route/gateway/lifecycle files. Prefab binding requires a separate UI-prefab-owner handoff.

## 5. Characterization Matrix

Required before edits:

1. Campaign and Operations sources, first-clear and replay, locked/unlocked/completed mission, and every supported profile/save identity;
2. zero/one/multiple objectives, stars, intel rows, rewards, optional sections, and stable add/remove/reorder;
3. complete/incomplete/conflicting scenario, operation-map, planning-camera, minimap, loadout, reward-preview, and launch identities;
4. Deploy eligible/ineligible/pending/accepted/rejected, repeated taps, stale snapshot, and session creation success/failure;
5. Back, route open/close/reopen, source-route return, loading transition, popup overlap, pause/resume, and new mission/session;
6. World/boundary/root/profile replacement, localization/theme/layout changes, version rollover, and source invalidation;
7. `100` route bind/unbind/replacement cycles with exact listener, row-instance, request, snapshot, and retained-reference counts.

Record source publications, identity reads, rebuilds, keyed row creates/removes/reuses, managed bytes, TMP/Image writes, layout rebuilds, listeners, Deploy requests/results, session creations, and CPU time.

## 6. Baseline And Acceptance Gates

Measure `180` warmup plus `300` unchanged open frames for representative locked, eligible first-clear, and replay snapshots; changed rows/content, localization/layout, route lifecycle, validation, and Deploy transitions are measured separately. Route-open prefab instantiation cost is reported separately from steady state.

Acceptance:

- one authoritative owner is proven for mission/progression, reward preview, scenario/map identities, and launch/session creation; one route-scoped projection owner remains;
- no fixed prefab value or stale design claim is presented as live authority, and UI never calculates objectives, rewards, unlocks, or eligibility;
- Campaign and Operations launches preserve exact selected mission and source route; all scenario/map/camera/minimap identities are coherent;
- equal snapshots perform zero recurring production-owned managed allocation, model rebuild, row churn, conversion, TMP/Image write, layout rebuild, listener change, polling, or ECS structural work;
- one semantic source change applies at most once; keyed rows preserve unchanged instances and clear removed/optional state;
- Deploy stays disabled until all required authorities approve, stale requests fail closed, and accepted requests create exactly one correctly identified session;
- `100` lifecycle cycles leave zero duplicate listeners, rows, requests, stale snapshots/views, or retained World/session references;
- existing route-history, shared-header, art, and honest-disabled-state behavior remains valid;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, authoritative owner/version contract, source hashes, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most four independently stable commits after authoritative owner and identity-drift resolution:

1. reconcile stale implementation claims and establish exact mission/source/scenario/map/reward identity contracts;
2. one route-scoped changed-only briefing projection with keyed objective/reward rows;
3. domain-validated Deploy/session creation and source-route lifecycle;
4. lifecycle and allocation acceptance.

Rollback if a wrong mission launches, UI computes mission/reward/progression state, placeholder rewards become authoritative, stale data crosses route/session identity, Deploy validation regresses, unchanged work remains, or the slice introduces `SystemBase`, polling, default-World discovery, mutable global authority, protected-file edits, or non-allowlisted overlap.

# AM-WP-019 - Campaign Projection And Lifecycle

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-026` proves authoritative campaign/progression/catalog/save owners and every Campaign caller, `AM-027` accepts the projection contract, `AM-028` accepts lifecycle ownership, and `AM-033` dispatches this package.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-024`.

## 1. Current Ownership And Risk

- `MenuOverlayRoutePresentation.InstallCampaignBody()` only instantiates `CampaignContentPrefab`; `CampaignOperationsScreenView` is a passive holder of serialized chapter, node, progress, reward, navigation, and launch references.
- `UIShellRouteButtonView` and `UiShellFlowSystem` correctly own generic route listeners/history, but Campaign launch carries no selected mission, difficulty, scenario, map, or source-route identity.
- Current tests prove prefab assignment, static hierarchy/art, deliberately unavailable controls, route history, and opening the static Mission Briefing route. They do not prove live campaign projection, invalidation, or allocation behavior.
- The generated screen hard-codes five chapter cards, `0 / 5` progress, one active node, locked nodes, and mission copy that conflicts with the active Chapter 1 design.
- Design requires campaign/chapter progression, mission catalog, stars, lock reasons, difficulty, map identity, and chapter rewards. Claimed runtime owners for those domains are absent from current production scripts.
- Chapter cards and mission nodes are currently images/rect transforms, not typed data-bound controls. Prefab changes require a separate owner handoff.

Risks are UI-calculated progression/unlocks/rewards, launching the wrong mission, presenting placeholder copy as saved state, stale selection across profile/World changes, listener duplication, and treating a missing projection as zero-allocation success.

## 2. Accepted Future Ownership

- Campaign/progression ownership publishes the authoritative campaign/chapter catalog, unlock/completion/stars, selected mission/difficulty, chapter rewards, save/loading readiness, and lock reasons. UI never computes or persists them.
- Mission/scenario ownership publishes the exact mission-to-scenario/operation-map launch identity. Campaign hands that immutable identity to Mission Briefing; it does not infer it from labels or node position.
- One immutable, semantically versioned Campaign snapshot crosses into one route-scoped projection owner. Stable chapter and mission-node IDs support changed-only keyed reconciliation.
- One selected mission identity exists. Locked/missing/stale nodes cannot launch and show an authoritative designed-unavailable reason.
- Completed missions remain replayable only when the authoritative progression contract permits it. Chapter rewards remain preview-only until their owner confirms grant state.
- Back and Campaign-to-Briefing preserve shell history and exact source-route identity. Route/root/World/profile/save replacement releases the projection and listeners idempotently.
- Gateway reads are pure and fail closed when no complete snapshot exists. No recurring polling is allowed.

No `SystemBase`, default-World lookup, view-local polling, UI-owned progression/save/reward authority, mutable global selection, broad manager/controller/provider/service type, scene/prefab/editor-builder edit, or activation from placeholder data is allowed.

## 3. Identity And Invalidation Contract

Minimum identity:

- World, gameplay boundary, profile/save slot, shell root, and route/binding generations;
- campaign catalog/version, active campaign ID, chapter collection structural version, stable chapter ID/version, and selected chapter ID;
- mission-node collection structural version, stable mission ID/version, selected mission ID, progression/unlock/completion/stars version, and selected difficulty/version;
- chapter reward preview/grant version, lock-reason version, save/loading readiness, and launch availability version;
- scenario ID/config, operation-map ID/version, source-route, and briefing-handoff generations;
- localization, formatting/theme, accessibility/layout, and explicit invalidation generations.

Rules:

- Equal composite identity produces zero model rebuild, string conversion, row churn, TMP/Image write, layout rebuild, or action rebind.
- Structural changes reconcile chapters/nodes by stable ID; reorder does not recreate unchanged rows.
- Selection references exact catalog/progression identity. Removed, locked, or stale selections clear once and cannot launch.
- Briefing handoff carries exact mission, scenario, map, difficulty, progression, and source-route identities.
- Empty/missing catalogs clear previous state and show an approved unavailable/loading state, never stale campaign data.
- Route exit, save reload, root/World/boundary/profile replacement, and subsystem registration invalidate once and release listeners/references idempotently.
- Reads never initialize progression, save, grant rewards, or mutate unlocks.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Screens/CampaignOperationsScreenView.cs`
- `Assets/Game/Scripts/UI/Shell/MenuOverlayRoutePresentation.cs` only for Campaign binding/lifecycle disposition
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs` only for Campaign binding/release points
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs` only for the approved Campaign model/identity contract
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs` only for the approved Campaign read/handoff contract
- `Assets/Game/Scripts/UI/Shell/UiShellRuntimeGateway.cs` only for Campaign contract/lifecycle disposition
- exact ECS gateway/component partials only after the immutable source contract is approved and appended before dispatch
- one narrowly named Campaign projection presentation under `Assets/Game/Scripts/UI/Screens/`
- exact campaign/progression/catalog/save/mission-launch contracts only after written owner handoff

Test files allowed:

- `Assets/Tests/Editor/CampaignOperationsScreenTests.cs`
- `Assets/Tests/Editor/MissionBriefingScreenTests.cs` only for the exact Campaign-to-Briefing identity handoff
- `Assets/Tests/Editor/CampaignProjectionLifecycleTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/CampaignAllocationValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_019_campaign_projection_lifecycle_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_019_campaign_projection_lifecycle_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-026`, `AM-027`, `AM-028`, `AM-033`, and `AM-035` tracker records and progress snapshot

Read-only dependencies: campaign/chapter/mission design, progression/save/rewards, scenario/map configs, navigation, localization, visual-lock assets, scenes, prefabs, and editor builders.

Hard exclusions: operation-map/static-map, FirstLaunch, audio, unrelated gameplay, progression/save/reward implementation, scenes, prefabs, editor builders, visual-lock art, packages, `ProjectSettings`, and any campaign identity/copy/unlock/reward/difficulty/launch-policy change without explicit owner approval.

This package is serialized with other packages claiming shared shell route/gateway/lifecycle files. Typed prefab bindings require a separate UI-prefab-owner handoff.

## 5. Characterization Matrix

Required before edits:

1. fresh/default, loaded, partially completed, completed, and corrupted/missing campaign save for every supported profile;
2. zero/one/multiple chapters and mission nodes, add/remove/reorder, selected item removal, duplicate/missing IDs, locked/unlocked/completed/replayable states;
3. zero/partial/full stars and chapter progress, reward preview/ungranted/granted, difficulty changes, and lock-reason changes;
4. complete/incomplete/conflicting mission, scenario, operation-map, progression, and source-route identities;
5. locked/stale/valid Launch, repeated taps, Campaign-to-Briefing and Back history, save loading/reload, and source invalidation;
6. route open/close/reopen, root/World/boundary/profile replacement, pause/resume, localization/theme/layout changes, and version rollover;
7. `100` route bind/unbind/replacement cycles with exact listener, chapter/node instance, request, snapshot, and retained-reference counts.

Record source publications, identity reads, rebuilds, keyed row creates/removes/reuses, managed bytes, TMP/Image writes, layout rebuilds, listeners, selection/launch requests/results, and CPU time.

## 6. Baseline And Acceptance Gates

Measure `180` warmup plus `300` unchanged open frames for representative empty, locked, partially completed, and replayable snapshots; changed rows/progression/selection/localization/layout, save reload, and route lifecycle are measured separately. Route-open prefab instantiation cost is reported separately from steady state.

Acceptance:

- one authoritative campaign/progression/catalog/save source, one mission/scenario launch owner, and one route-scoped projection owner are proven;
- no static prefab value or stale design claim is presented as live authority, and UI never computes unlocks, rewards, stars, progression, or persistence;
- locked nodes never launch and display authoritative reasons; completed mission replay and chapter progress/rewards match source state;
- Campaign-to-Briefing carries exact mission, scenario, map, difficulty, progression, and source-route identities;
- equal snapshots perform zero recurring production-owned managed allocation, model rebuild, row churn, conversion, TMP/Image write, layout rebuild, listener change, polling, or ECS structural work;
- one semantic source change applies at most once; keyed rows preserve unchanged instances and clear removed/stale selection;
- `100` lifecycle cycles leave zero duplicate listeners, rows, requests, stale snapshots/views, or retained World/profile references;
- existing route-history, shared-header, art, and honest-unavailable behavior remains valid;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, authoritative owner/version contract, source hashes, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most four independently stable commits after authoritative owner and design/runtime identity reconciliation:

1. establish exact campaign/chapter/mission/progression/save identity contracts and resolve stale implementation claims;
2. one route-scoped changed-only projection with keyed chapter/mission-node rows;
3. domain-validated selection, difficulty, reward state, and Mission Briefing handoff;
4. lifecycle and allocation acceptance.

Rollback if UI computes campaign state, wrong/locked missions launch, placeholder content becomes authoritative, stale state crosses profile/World identity, route history regresses, unchanged work remains, or the slice introduces `SystemBase`, polling, default-World discovery, mutable global authority, protected-file edits, or non-allowlisted overlap.

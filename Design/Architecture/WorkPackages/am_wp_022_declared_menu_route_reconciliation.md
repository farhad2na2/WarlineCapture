# AM-WP-022 - Declared Menu Route Reconciliation

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-026` proves every caller and source owner for the five routes, `AM-028` accepts lifecycle ownership, and `AM-033` dispatches this package.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-027`.

## 1. Current Ownership And Risk

- `UIRoute` declares `CommandExchange`, `Inbox`, `Events`, `Ranking`, and `LoadoutSquadPrep`.
- `MenuOverlayRoutePresentation.Install()` forwards them to `CommanderProfileRouteLifecyclePresentation.InstallMenuRouteBody()`, whose fallback installs Main Menu content. The shell route identity changes while the visible content does not.
- No dedicated production view, prefab binding, projection, semantic version, or source contract was found for any of the five routes.
- Active design retains all five routes. Command Exchange and Loadout/Squad Prep require transactional/launch authorities; Inbox, Events, and Ranking require account/content/persistence authorities. Missing authorities make dedicated unavailable/empty shells safer than silent fallback.
- Some state documentation claims route shells that current production routing does not install; those claims are stale until executable evidence exists.

Risks are route/content identity mismatch, Back/history errors, accidental purchases or deployment, UI-authored rewards/eligibility/rankings, stale account data, hidden Main Menu work, and treating fallback content as implemented projection.

## 2. Accepted Future Ownership

- Every declared route installs either its dedicated content or an explicit route-specific designed-unavailable/empty state. Silent Main Menu fallback is prohibited.
- Command Exchange remains distinct from the in-match Resource Exchange. Wallet, catalog, platform product, receipt, reward, and profile owners validate every purchase; UI never grants products or mutates currency.
- Inbox uses message, operation-report, claim-queue, and read/claim persistence authorities. Events uses schedule/configuration, eligibility, modifier, reward-preview, and account-progress authorities.
- Ranking uses an explicit local/account/network source with season, category, scope, availability, and refresh semantics. Missing network authority produces an honest unavailable state.
- Loadout/Squad Prep uses exact selected mission, roster, squad/support/gear, restriction, deploy-cost, inventory, validation-reason, and launch-payload authorities. UI never computes power, ownership, cost, or eligibility.
- Each route has one route-scoped presentation owner, pure source reads, symmetric listeners, and idempotent route/root/World/account cleanup. No recurring polling is allowed.

No `SystemBase`, default-World lookup, silent fallback, view-local polling, UI-owned economy/reward/progression/launch authority, mutable global route data, broad manager/controller/provider/service type, scene/prefab edit, or activation without complete owner contracts is allowed.

## 3. Identity And Invalidation Contract

Common identity:

- World, shell boundary/root, route, route-request, content, binding, account/session/profile/save, localization, formatting/theme, accessibility/layout, and invalidation generations.

Route-specific identity:

- Command Exchange: wallet, catalog, platform availability/product, offer, receipt/purchase-pending, profile-commit, and entitlement versions;
- Inbox: message collection, stable message ID/version, read/claim state, reward transaction, and operation-report versions;
- Events: schedule/config, stable event ID/version, eligibility, modifier, reward preview, event progress, and time-window generations;
- Ranking: category, season, scope, leaderboard availability, local/account score, page/cursor, and row collection versions;
- Loadout/Squad Prep: mission, roster/inventory, selected squad/support/gear, restriction, deploy cost, validation, scenario/map, and launch generations.

Equal identity produces zero rebuild, conversion, row churn, TMP/Image write, layout rebuild, or listener change. Stable IDs reconcile rows. Stale purchase/claim/event/ranking/loadout actions fail closed. Route exit, account/session/profile/World/root replacement, and source invalidation clear state once.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Contracts/UIRoute.cs` only if route semantics require clarification
- `Assets/Game/Scripts/UI/Shell/MenuOverlayRoutePresentation.cs`
- `Assets/Game/Scripts/UI/Shell/CommanderProfileRouteLifecyclePresentation.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs` only for route installation/binding/release
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs` and `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs` only for approved route models/contracts
- exact ECS shell gateway/component partials appended after source contracts are approved
- narrowly named route views/projection presentations under the existing UI boundaries
- exact domain read/action contracts only after written owner handoff

Test files allowed: existing shell route/history tests whose contract changes, plus narrowly named route reconciliation, lifecycle, and allocation tests under `Assets/Tests/Editor/`.

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_022_declared_menu_route_reconciliation_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_022_declared_menu_route_reconciliation_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-026`, `AM-028`, `AM-033`, and `AM-035` tracker records and progress snapshot

Read-only dependencies: economy/store/platform purchase, messages/reports, events, ranking/network, roster/inventory/loadout/launch, persistence, localization, visual-lock assets, scenes, and prefabs.

Hard exclusions: operation-map/static-map, FirstLaunch, audio, gameplay/economy/persistence/platform authority, scenes, prefabs, visual-lock art, packages, `ProjectSettings`, and policy/content changes without explicit owner approval. Prefab binding requires a separate owner handoff.

This package is serialized with other packages claiming shared shell route/gateway/lifecycle files.

## 5. Characterization Matrix

Required before edits:

1. each route opened directly, from every current source, through Back/history, and across route/root/World/account replacement;
2. absent/unavailable/loading/empty/populated/error states for every source contract;
3. Command Exchange purchase unavailable, eligible, pending, success, failure, duplicate receipt, stale offer, and profile-commit failure;
4. Inbox unread/read/claimable/claimed/expired/stale messages and operation reports;
5. Events inactive/active/expired/ineligible/progress/reward-preview changes and time-window rollover;
6. Ranking local/account/network unavailable, category/season/scope/page changes, tie/reorder, and stale response;
7. Loadout locked/eligible/invalid/stale mission, inventory/roster change, cost change, repeated Deploy, and launch success/failure;
8. `100` open/close/replacement cycles with exact listeners, rows, requests, snapshots, and retained references.

Record identity reads, rebuilds, keyed row lifecycle, managed bytes, TMP/Image/layout writes, listeners, action requests/results, route commands, and CPU time.

## 6. Baseline And Acceptance Gates

Measure `180` warmup plus `300` unchanged open frames for each route's unavailable/empty state and one representative populated state when executable; changed data/actions and lifecycle are measured separately.

Acceptance:

- every route renders dedicated or explicit unavailable content and never Main Menu fallback;
- exact domain owners and one route-scoped presentation owner are proven per executable route;
- UI never computes purchases, rewards, claims, eligibility, rankings, event progress, loadout validity, cost, or launch rules;
- equal identity performs zero recurring production-owned allocation, rebuild, conversion, row churn, TMP/Image/layout write, listener change, polling, or ECS structural work;
- stale transactional/actions fail closed; purchasing and deployment remain disabled until complete authoritative contracts exist;
- `100` lifecycle cycles leave zero duplicate listeners, rows, requests, stale content, or retained World/account references;
- route identity, history, Back, loading, and shared shell behavior remain correct;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, caller/source classification, source hashes, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most five independently stable commits after caller/source characterization: one route-fallback correction with explicit unavailable shells, then at most one bounded executable integration slice per source-ready domain grouping, followed by shared lifecycle/allocation acceptance.

Rollback if any route silently falls back, transactional or launch actions bypass authority, route/history behavior regresses, stale account/profile data survives replacement, unchanged work remains, or the slice introduces `SystemBase`, polling, default-World discovery, mutable global authority, protected-file edits, or non-allowlisted overlap.

# AM-WP-023 - Declared Popup Reconciliation

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-026` proves every Threat Alert, Pause, and Reward Unlock caller/source, `AM-028` accepts popup lifecycle ownership, and `AM-033` dispatches this package.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-028`.

## 1. Current Ownership And Risk

- `UiShellPopupKind` declares `ThreatAlert`, `Pause`, and `RewardUnlock`, and generic shell flow can carry them, but `UIShellContentView.InstallPopup()` installs none of them.
- Threat detection/read models and a non-modal Match HUD threat panel/jump action are executable. The shell Threat Alert popup is not and would duplicate/block routine threat feedback.
- `UiActionKind.Pause` requests the Pause popup and Pause blocks assistant takeover, but no content installation or authoritative gameplay pause/resume owner was found. An existing Pause prefab appears orphaned.
- Reward Unlock is takeover-blocking but has no request producer, committed grant projection, binding, or installation. An existing prefab appears orphaned.

Risks are duplicate threat presentation, Canvas visibility becoming simulation authority, world input remaining active under Pause, uncommitted/duplicate rewards being shown, stale popups across World/session changes, and treating declared kinds as executable surfaces.

## 2. Accepted Future Ownership

- Threat Alert is classified as direct non-modal HUD presentation. Complete its exact threat/jump identity under the existing threat package and remove the shell popup declaration only after all producers/defaults are migrated and tests prove no caller remains.
- Pause remains a modal shell popup. UI requests pause; gameplay/simulation authority confirms paused state and owns clock/simulation policy. The popup owns Resume, nested Settings return, and Leave Match request presentation only.
- Reward Unlock remains a fail-closed modal contract for major first-time unlocks after inventory/profile commit. Ordinary currency/items use HUD feedback or Mission Result rows.
- Reward/grant authority owns immutable transaction/unlock identity, duplicate conversion, eligibility, inventory/profile commit, and acknowledgement persistence. UI never grants or commits rewards.
- One popup owner enforces modal/world-input/assistant takeover ordering, one active modal policy, pure reads, symmetric listeners, and idempotent match/root/World/account cleanup.

No `SystemBase`, default-World lookup, view-local polling, blocking Threat modal, Canvas-authored pause state, UI-owned reward authority, mutable global popup state, broad manager/controller/provider/service type, scene/prefab edit, or activation before owner handoff is allowed.

## 3. Identity And Invalidation Contract

Threat identity:

- World/boundary, match/session, stable threat ID/generation, warning semantic version, target/anchor generation, severity/ETA/expiry/acknowledgement, and jump-command generation.

Pause identity:

- World/boundary, match/session, pause request sequence, authoritative pause-state/eligibility/policy version, world-input/assistant ownership, nested-popup return state, and leave-match request generation.

Reward Unlock identity:

- World/account/session/profile, grant transaction ID/version, unlock ID/type/version, item collection structural version, inventory/profile commit version, first-time eligibility, duplicate conversion, acknowledgement, localization, and binding generations.

Equal identity produces zero rebuild, conversion, TMP/Image write, layout rebuild, or listener change. New threats supersede stale alerts; Pause show/resume is idempotent; Reward Unlock shows once only after successful commit and never replays after acknowledgement. Match termination and World/account/root replacement clear state once.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs` only for popup-kind/model disposition
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestDispatchSystemHelper.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs` only for approved Pause/Reward installation and lifecycle
- `Assets/Game/Scripts/UI/Shell/Ecs/AssistantControlOwnerSystem.cs` only for proven modal/takeover disposition
- existing threat HUD projection files proven by `AM-026` only for Threat popup retirement/identity completion
- narrowly named Pause and Reward Unlock contract/presentation files
- exact pause/simulation, world-input, reward/inventory/profile contracts only after written owner handoff

Test files allowed: existing threat/popup/shell tests whose contract changes, plus narrowly named popup reconciliation, Pause, Reward Unlock, lifecycle, and allocation tests under `Assets/Tests/Editor/`.

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_023_declared_popup_reconciliation_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_023_declared_popup_reconciliation_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-026`, `AM-028`, `AM-033`, and `AM-035` tracker records and progress snapshot

Read-only dependencies: threat/combat/camera, simulation/pause/input, reward/inventory/profile, navigation, localization, visual-lock assets, scenes, and prefabs.

Hard exclusions: operation-map/static-map, FirstLaunch, audio, camera/simulation/reward authority, scenes, prefabs, visual-lock art, packages, `ProjectSettings`, and gameplay/policy changes without explicit owner approval. Prefab wiring requires a separate UI-prefab-owner handoff.

This package is serialized with other packages claiming shared shell popup/flow/lifecycle files.

## 5. Characterization Matrix

Required before edits:

1. every code/serialized/default producer for all three popup kinds and every popup installation/takeover consumer;
2. Threat absent/new/equal/updated/expired/acknowledged/invalid target, burst/supersession, jump success/failure, match/root/World replacement;
3. Pause eligible/ineligible/requested/confirmed/resuming, repeated Pause/Resume, nested Settings and return, Leave Match confirmation, focus/pause events, match termination, and multiplayer policy if applicable;
4. Reward no producer/uncommitted/committed, first-time/duplicate, zero/one/multiple rows, acknowledgement success/failure, app restart, account/profile/World replacement, and stale transaction;
5. modal/world-input/assistant ownership ordering, generic popup overlap, route change, loading curtain, and interruption;
6. empty/long/localized content, accessibility/layout changes, version rollover, and missing optional data;
7. `100` show/hide/replacement cycles with exact listeners, requests, rows, acknowledgements, snapshots, and retained references.

Record source publications, identity reads, rebuilds, managed bytes, TMP/Image/layout writes, listeners, input/takeover state, pause/reward commands/results, and CPU time.

## 6. Baseline And Acceptance Gates

Measure `180` warmup plus `300` unchanged visible frames for Threat HUD, confirmed Pause, and committed Reward Unlock; hidden/unavailable state, changed data, opening, actions, and lifecycle are measured separately.

Acceptance:

- Threat uses one non-modal HUD owner and has no remaining shell-popup producer/default/declaration after safe migration;
- Pause has one authoritative simulation owner, one modal presentation owner, correct world-input/assistant blocking, idempotent Resume, and nested Settings return;
- Reward Unlock appears once only after committed authoritative grant, never grants in UI, and never replays after acknowledgement;
- equal identity performs zero recurring production-owned allocation, rebuild, conversion, TMP/Image/layout write, listener change, polling, or ECS structural work;
- stale threat/pause/reward commands fail closed and cannot cross match/account/World identity;
- `100` lifecycle cycles leave zero duplicate listeners, rows, commands, stale popups, retained views, or blocked input/takeover state;
- existing direct threat behavior and generic shell popup transitions remain correct;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, per-kind caller/source classification, source hashes, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most four independently stable commits after caller/source characterization:

1. complete direct Threat identity and retire the unused shell-popup contract;
2. authoritative Pause request/confirmation plus modal presentation/input lifecycle;
3. committed first-time Reward Unlock projection/acknowledgement;
4. shared lifecycle and allocation acceptance.

Rollback if threat feedback becomes blocking/duplicated, Canvas controls simulation state, Pause leaks world input or takeover, uncommitted/duplicate rewards display, stale popup state crosses identity, unchanged work remains, or the slice introduces `SystemBase`, polling, default-World discovery, mutable global authority, protected-file edits, or non-allowlisted overlap.

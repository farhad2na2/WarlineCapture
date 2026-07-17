# AM-WP-017 - Mission Result Projection And Lifecycle

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-026` proves the authoritative match-outcome owner and every result-route caller, `AM-027` accepts the projection contract, `AM-028` accepts lifecycle ownership, and `AM-033` dispatches this package.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-018`.

## 1. Current Ownership And Risk

- `UiMissionResultPopupModel` defines only `Victory` and `Loss`, with hard-coded default copy and replay enabled. `Design/Mission_Result_State_Spec.md` instead requires `VictoryComplete`, `PartialSuccess`, `DefeatFailed`, `Withdrawn`, and `SimulationResolved`, plus objectives, stars, statistics, rewards, consequences, narrative beats, reason codes, source mode, and source-specific routing.
- `UiShellReadModelAdapter.TryReadMissionResult()` and the fallback gateway both return `false`; no executable result projection or authoritative gameplay handoff was found.
- `UiShellPopupKind` and `UIRoute` have no Mission Result entry, while `UIShellContentView.InstallPopup()` has no result installation/binding/action path. Two result prefabs exist but appear orphaned from the runtime shell.
- The shell can represent generic route and popup state, but no production owner proves when a result becomes final, which match/run it belongs to, or whether it was consumed.
- No source/version identity prevents a late result from a previous match or replaced World from appearing in a new session.
- Static defaults can be mistaken for live gameplay truth if the dormant gateway is activated without an explicit outcome contract.

Risks are fabricated results, duplicate presentation, stale cross-match data, replaying an obsolete match, result UI racing match teardown, and measuring a missing implementation as zero-allocation success.

## 2. Accepted Future Ownership

- Gameplay owns the authoritative terminal match outcome, completion reason, objective/reward summary data, replay eligibility, and exact match/run identity. UI remains read-only and never derives victory or loss.
- One immutable, versioned result snapshot crosses the gameplay-to-UI boundary after the outcome is final. The shell consumes it at most once for that result identity.
- The result route/popup owner installs and clears the view; the projection owner applies changed semantic content. Neither owner polls gameplay every frame.
- The projection must support the five states required by `Mission_Result_State_Spec.md` through one reusable surface, but gameplay remains responsible for publishing the selected state and all internally consistent result data. Unspecified variants must not be invented here.
- Continue remains disabled until reward/save authority confirms completion. Continue, retry/replay, and return actions carry the exact result/source-mode identity, preserve the approved FirstLaunch/Campaign/Operations/Skirmish/Custom destinations, and are idempotent.
- World, boundary, shell-root, route, and match replacement clear pending/consumed state exactly once.
- Gateway reads are pure and fail closed when no authoritative snapshot exists.

No `SystemBase`, view-local poller, default-World lookup, UI-authored gameplay result, mutable static authority, broad manager/controller/provider/service type, scene/prefab edit, or placeholder-as-production behavior is allowed.

## 3. Identity And Invalidation Contract

Minimum identity:

- World and gameplay boundary generations;
- match/session/run identity and terminal-result sequence;
- authoritative outcome and completion-reason versions;
- objective, casualty, reward, and summary versions when those fields are approved;
- replay-eligibility version;
- shell root, route/popup, localization, formatting/theme, and binding generations;
- consumed/dismissed generation and explicit invalidation reason.

Rules:

- One terminal result identity is presented at most once unless an explicit restore/reopen contract is approved.
- Equal identity and versions produce zero managed-model rebuild, TMP write, layout rebuild, or shell command.
- A newer match invalidates every pending result from an older match before the new Match route becomes interactive.
- Result publication before shell readiness is retained once within a fixed capacity of one; overflow or conflicting terminal outcomes fail visibly in diagnostics without choosing a winner in UI.
- Closing, replaying, returning to menu, World disposal, and root replacement release references and listeners idempotently.
- Localization/theme changes may rebuild presentation without changing or re-consuming the gameplay result.
- Reads never create components, mutate gameplay, or acknowledge consumption; acknowledgement is an explicit command carrying the exact result identity.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs` only for the approved result model/identity contract
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs` only for the approved result read contract
- `Assets/Game/Scripts/UI/Shell/UiShellRuntimeGateway.cs` only for result contract/lifecycle disposition
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Contracts.cs` only for result contract disposition
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs` only for result cache/lifecycle disposition
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs` only for the approved immutable result snapshot/identity
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs` only for an approved result popup/action transition
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs` only for result installation/binding after prefab-owner handoff
- the existing Mission Result route/popup view and shell installation files proven by `AM-026`; exact paths must be appended before dispatch
- the exact gameplay-owned terminal-result contract file only after written owner handoff

Test files allowed:

- existing Mission Result/shell fake-gateway tests whose compile contract genuinely changes
- `Assets/Tests/Editor/MissionResultProjectionLifecycleTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/MissionResultAllocationValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_017_mission_result_projection_lifecycle_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_017_mission_result_projection_lifecycle_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-026`, `AM-027`, `AM-028`, `AM-033`, and `AM-035` tracker records and progress snapshot

Read-only dependencies: mission design, outcome/reward authority, navigation, localization, visual-lock assets, scenes, and prefabs.

Hard exclusions: operation-map/static-map, FirstLaunch, audio, unrelated gameplay, scenes, prefabs, visual-lock art, packages, `ProjectSettings`, and any outcome/copy/reward/replay-policy change without explicit owner approval.

This package is serialized with other packages claiming shared shell gateway/contracts.

## 5. Characterization Matrix

Required before edits:

1. `VictoryComplete`, `PartialSuccess`, `DefeatFailed`, `Withdrawn`, and `SimulationResolved` from the design contract;
2. result before/after shell readiness, duplicate publication, conflicting publication, and no result;
3. result while Match is active, during loading/teardown, after Main Menu entry, and across World/boundary/root replacement;
4. replay enabled/disabled, repeated Replay, repeated Main Menu, Back/system-back, and app pause/resume;
5. equal result, changed summary/reward/localization/theme, and version rollover;
6. zero/one/multiple objectives, stars, statistics, rewards, consequences, narrative beats, and reason codes; empty/long/localized copy; missing optional data; and source-mode routing;
7. `100` result-open/close and match-replacement cycles with exact listener, retained-snapshot, command, and presentation counts.

Record source publications, accepted/rejected identities, model rebuilds, managed bytes, TMP writes, layout rebuilds, shell commands, acknowledgements, retained references, and CPU time.

## 6. Baseline And Acceptance Gates

Measure `180` warmup plus `300` unchanged open frames for each approved outcome; changed-result, localization/theme, open/close, replay, return-to-menu, and World replacement are measured separately.

Acceptance:

- one authoritative result source and one presentation owner are proven;
- UI never computes or substitutes an outcome, and no placeholder is presented as live data;
- all five designed result states project correctly, with rewards/consequences/reason codes internally consistent and defeat/withdrawal never presenting ungranted clear rewards;
- each exact result identity is presented and acknowledged at most once;
- stale or conflicting results fail closed and cannot cross match/World identity;
- unchanged open result performs zero recurring production-owned managed allocation, model rebuild, TMP write, layout rebuild, polling, or ECS structural change;
- Continue remains blocked until reward/save completion; source-mode Continue/retry routing is correct; repeated actions and `100` lifecycle cycles leave zero duplicate listeners, commands, snapshots, stale views, or retained World references;
- all approved outcomes and replay/navigation behavior remain correct;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, source hashes, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most three independently stable commits after authoritative-source and caller characterization:

1. exact gameplay result identity/version contract and pure gateway read;
2. one version-gated retained projection and changed-only Mission Result apply path;
3. consumption, action idempotency, lifecycle, and allocation acceptance.

Rollback if any result is fabricated, duplicated, lost after accepted publication, leaked into another match, actions regress, unchanged work remains, or the slice introduces `SystemBase`, polling, default-World discovery, mutable static authority, protected-file edits, or non-allowlisted overlap.

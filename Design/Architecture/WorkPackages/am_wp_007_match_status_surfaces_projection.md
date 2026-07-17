# AM-WP-007 - Match Status Surfaces Projection

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, the mission/objective owner explicitly resolves the executable objective writer, `AM-027` accepts visible-semantic versions, and `AM-028` accepts World binding, invalidation, and projection/apply order.

Umbrella task: `AM-031`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-006`.

## 1. Current Authority And Gaps

The inventory row currently combines four behaviors that must not be forced into one update cadence:

- **Elapsed time:** `MatchHudObjectivesElapsedView` owns a view-local timer, advances it in `Update`, and allocates/formats a new string once per displayed second. It restarts on enable and is not proven to represent authoritative Match elapsed time.
- **Threat warnings:** `ThreatWarningPresentationState` reads World-scoped runtime warning state, formats a title once for a pending warning, and forwards a five-second lifetime to `MainMenuPlayUI`. `MainMenuPlayUI.Update` only checks expiry while the panel is visible.
- **Command/current-order feedback:** `BattleHudRuntimeFeedbackView` retains persistent/transient state and receives event-driven models. `MainMenuPlayUI.Update` ticks transient expiry. This behavior belongs to `AM-032` and is read-only in this package.
- **Combined ECS status adapter:** `TryReadMatchHudStatusSurfaces` converts objectives, elapsed, threat, and feedback fixed strings into one managed model and performs read-side default-state creation. No active production apply caller was found.

The executable writer for `MatchObjectiveRuntimeElement`/visible objective rows is unresolved in the current ownership evidence. This package records that dependency and must not invent a mission authority or modify operation-map work.

## 2. Accepted Future Ownership

- The accepted mission/runtime owner publishes objective structure/content/state and a visible-semantic objective version.
- Match elapsed time comes from one accepted Match/session clock source. The view never owns gameplay/session time. Display formatting is cached by whole displayed second.
- Threat runtime state remains ECS-owned; `ThreatWarningPresentationState` remains the bounded World-scoped consumption edge unless characterization selects an existing equivalent owner.
- Threat visibility expiry remains a narrow time-driven presentation lane and does not rebuild objective, elapsed, or feedback models.
- A World-bound `MatchHudStatusManagedProjectionCache` retains objective and threat managed strings by independent semantic identities.
- Command/current-order feedback remains event-driven under `AM-032`; this package may characterize its integration but cannot change its behavior or files.
- The dormant combined adapter is removed, split into canonical independent reads, or delegated to the accepted cache. Read-side structural mutation is removed.

No new `SystemBase`, view-local gameplay clock, broad polling loop, default-World lookup, or parallel mission/threat authority is allowed.

## 3. Required Semantic Identities

Minimum independent identities:

- bound World, Match session, shell boundary, and lifecycle generation;
- objective structure version, objective visible-content version, row count, and stable row identities;
- elapsed clock identity plus displayed whole-second value;
- threat request sequence, type, ETA bucket, count, title version, visibility state, and expiry identity;
- localization, settings/accessibility, icon/sprite catalog, and resolution/layout generations;
- explicit invalidation generation and reason.

Rules:

- An elapsed-second change updates only elapsed text.
- Objective progress/state changes rebuild only changed retained rows; unrelated assistant or operation-map counters do not rebuild visible objectives.
- Threat expiry changes only visibility. It does not rebuild threat title or objective/elapsed content.
- Equal warning requests and equal objective snapshots do not advance visible versions.
- Match pause/time-scale policy is defined by the accepted session clock, not inferred by the view.
- World/session replacement clears every cached entity, string, timer, and expiry identity before apply.

## 4. Exact File Allowlist

Production files allowed after every dispatch dependency is accepted:

- `Assets/Game/Scripts/UI/Components/MatchHudObjectivesElapsedView.cs`
- `Assets/Game/Scripts/Systems/ThreatWarningPresentationState.cs`
- `Assets/Game/Scripts/UI/MainMenuPlayUI.cs` only for the threat/elapsed presentation boundary; unrelated minimap, assistant, resource, zoom, and feedback code is excluded
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Assistant.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.DefaultState.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/MatchHudStatusManagedProjectionCache.cs` and its `.meta` if required
- the exact mission/objective source and writer selected by its active owner; amend this allowlist only after explicit handoff
- the exact accepted Match/session clock source; amend this allowlist after `AM-027` names it

Test files allowed:

- `Assets/Tests/Editor/ThreatWarningValidationTests.cs`
- `Assets/Tests/Editor/WorldScopedComponentQueryCachePerformanceValidation.cs`
- `Assets/Tests/Editor/AlertObjectiveAudioFeedbackTests.cs` as behavior-only regression coverage; audio production remains excluded
- `Assets/Tests/Editor/MatchHudStatusProjectionPerformanceValidation.cs` and its `.meta` if required
- `Assets/Tests/Editor/MatchHudStatusManagedProjectionCacheTests.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_007_match_status_surfaces_projection_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_007_match_status_surfaces_projection_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-031` tracker record and progress snapshot

Read-only dependencies:

- `Assets/Game/Scripts/Components/MatchObjectiveComponents.cs` and all operation-map ownership evidence until explicit handoff;
- `BattleHudRuntimeFeedbackView` and command/current-order helpers under `AM-032`;
- Match HUD prefabs, scenes, visual-lock assets, audio configs, and localization catalogs.

Hard exclusions:

- operation-map/static-map production, scenes, trackers, generators, and evidence;
- FirstLaunch, audio production, UI visual-lock art, prefabs, packages, and `ProjectSettings`;
- objective content/rules, mission progression, warning detection/audio behavior, elapsed-time design, pause policy, layout, art, or localization-copy changes;
- command/current-order feedback behavior and files;
- any production file outside this allowlist without a reviewed package amendment and active-owner handoff.

## 5. Characterization Matrix

Required before edits:

1. no objectives, one/three objectives, pending/active/complete/failed rows, row replacement, and objective count changes;
2. elapsed startup, second boundary, minute boundary, pause/resume, time-scale change, view disable/enable, Match restart, and return to Menu;
3. ground/air threat, ETA/no ETA, single/multiple threats, equal duplicate request, replacement before expiry, expiry, jump availability, and settings-disabled presentation;
4. simultaneous objective, elapsed, and threat changes proving independent apply lanes;
5. World/session/boundary replacement, missing/duplicate state, scene unload, view rebind, localization/settings/resolution invalidation;
6. current event-driven command/current-order feedback as read-only integration parity;
7. proof of active callers/writers for every retained path and proof that the combined gateway adapter is dormant before retirement.

Record source writes, versions, fixed-string conversions, formatting calls, managed string identities, row mutations, visibility mutations, expiry checks, model applies, and allocations.

## 6. Baseline And Acceptance Gates

Measure:

- `180` warmup plus `300` unchanged frames with objectives visible and no threat;
- `300` elapsed-second updates with objective/threat identities stable;
- `300` visible-threat lifetime frames with stable title;
- objective-only, threat-only, simultaneous, lifecycle, localization, settings, and resolution transitions separately.

Acceptance:

- exactly zero recurring production-owned managed bytes in unchanged, elapsed-only, and stable-threat windows after warmup;
- one elapsed formatting/apply per displayed second and no objective/threat rebuild from elapsed updates;
- one title conversion per semantic threat change and visibility-only expiry;
- one objective row apply per changed retained row and no rebuild from irrelevant source changes;
- no read-side structural mutation, default-World discovery, or stale World/session state;
- no view-local gameplay/session clock remains;
- focused behavior/performance tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds resolved owners, baseline/implementation commit and tree, source hashes, compressed logs, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most three independently stable commits after dispatch:

1. resolve and characterize objective/session-clock ownership with versions, without switching presentation;
2. managed objective/elapsed cache and retained apply;
3. threat cache/lifetime separation and dormant combined-adapter retirement.

Rollback if objective, elapsed, warning, jump, command-feedback integration, pause/restart, or lifecycle behavior changes; if elapsed/threat updates rebuild unrelated lanes; if recurring allocation remains; or if the slice introduces `SystemBase`, parallel authority, a service locator, protected map/audio/UI edits, or non-allowlisted overlap.

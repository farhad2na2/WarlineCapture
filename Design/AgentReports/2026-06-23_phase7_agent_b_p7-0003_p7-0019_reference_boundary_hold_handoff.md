# Phase 7 Agent B Handoff - P7-0003 / P7-0019 Managed Reference Boundary Hold

Date: 2026-06-23
Lane: Agent B
Rows: `P7-0003` `MatchSceneReferenceBoundarySystem`, `P7-0019` `PerformanceDiagnosticsReferenceBoundarySystem`
Disposition: `RetireFold`
Result: Held pending an explicit managed-reference boundary guardrail/model change.

## Summary

Both rows are disabled `SystemBase` wrappers with empty `OnUpdate`, but they are not safe direct instance-field folds under the current architecture contract. They provide world-scoped managed reference storage shared by multiple independently constructed helpers.

- `MatchSceneReferenceBoundarySystem` shares `MatchSceneView` across `MatchBootstrapSystem`, `MenuBootstrapSystem`, and `MatchStartSystem`.
- `PerformanceDiagnosticsReferenceBoundarySystem` shares `PerformanceDiagnosticsSystem` across `MatchBootstrapSystem` and `MenuBootstrapSystem`.

Direct per-instance fields would lose cross-helper sharing. Static mutable registries are disallowed by the Phase 7 guardrails. Managed ECS components would reintroduce managed component debt. Reclassifying these rows as managed exceptions was already tested and rejected by the current architecture guard because the rows have no concrete Unity-object ticking blocker.

## Files Reviewed

- `Assets/Game/Scripts/Composition/MatchSceneReferenceSystem.cs`
- `Assets/Game/Scripts/Systems/PerformanceDiagnosticsReferenceSystem.cs`
- `Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs`
- `Assets/Game/Scripts/Composition/MenuBootstrapSystem.cs`
- `Assets/Game/Scripts/Composition/MatchStartSystem.cs`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_agent_b_direct_startup_tracker.md`

## Current Inventory State

- Total ECS declarations: `165`.
- Production `SystemBase`/legacy declarations: `27`.
- Production `ISystem` declarations: `138`.
- Current production `ISystem` share: `83.6%`.
- Managed exceptions: `24`.
- Open rows: `2` (`P7-0003`, `P7-0019`).

## Required Decision

To finish these rows, Phase 7 needs one explicit model decision:

- allow a narrow world-scoped managed reference boundary despite no Unity-object ticking blocker,
- introduce an approved non-static world-scoped managed reference store pattern,
- or accept a behavior change that removes cross-helper sharing.

No code change was made for these two rows in this hold report.

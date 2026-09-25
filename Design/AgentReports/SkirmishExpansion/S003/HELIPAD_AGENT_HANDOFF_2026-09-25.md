# Mission 5 / S003 helipad repair handoff

## Assignment and current stop condition

Repair four player-observed issues: an invisible placed helipad, enemies ignoring hostile infrastructure in their territory, no demonstrated helicopter landing/use, and ARIA placing its first helipad beside the enemy Barracks. Work sequentially in small patches. This is implementation guidance, not proof that the defects are fixed.

Workspace: `/private/tmp/warline-s003-player-ready`, branch `codex/s003-player-ready`, base `09003236c`. The main checkout `/Users/farhad/Projects/WarlineCapture` contains unrelated work. Preserve ALL existing worktree changes; do not reset, stash, recreate the worktree, or apply its diff to main. Start with `rtk git status --short` and read local AGENTS.md plus the user's newer supplied rules. Use RTK, `rg --files` for path discovery, bounded reads, and no subagents unless explicitly authorized.

The user paused execution and then requested this handoff plan. **This plan does not resume Unity validation.** Source implementation can be assigned separately; keep validation paused until the user resumes it. No Editor is being launched by this handoff. Existing visual-direction approval remains valid; ordinary gameplay fixes need no new mockup approval. Do not create a task or change models unless requested.

Read first:
- `IMPLEMENTATION_PROGRESS_2026-09-24.md` (latest sections first).
- `INPUT_EVIDENCE_AUDIT_2026-09-25.md`.
- `FIX_PLAN_2026-09-24.md` for overall mission scope; do not attempt all remaining mission work as part of this bounded helipad repair.

## Baseline and evidence

Latest checked run: `/private/tmp/s003-broader-campaign-09.log`, copied to `RepairEvidence/s003-broader-campaign-09.log`. Wrapper exit 0, every broader constituent marker passed, aggregate `[SkirmishPlayerReadyRegressions] result=Passed`, Campaign `[M01FirstContactCampaignUiValidation] result=Passed tests=10 captures=3`.

This is automated evidence ONLY. Native run 14 ended in real Defeat at 187.033 seconds and Replay timed out. Later Replay, localization, defensive-opening, input-measurement and constructed-building ownership changes passed automated checks but have no new native proof. Old wins are diagnostic, not current-candidate acceptance. Keep every failure and original ledger row.

Latest construction adoption is already implemented in `SkirmishScenarioSpawnSystem.AdoptConstructedBuilding`, invoked by the normal placement registration callback in `BuildingPlacementCommandCompositionSystemHelper`. It assigns attempt/faction identity, roster policy and teardown ownership without turning rebuilt Barracks into designated objectives. Do not duplicate or undo it. It does not by itself establish that the GameObject renderer works.

## Facts versus hypotheses

Confirmed in source:
1. `UI/Shell/Ecs/AriaSkirmishPlanSystem.cs`, `StepExpandedBaseAssault`: generic `view.PlacementOpen` handling taps `PlacementConfirm` whenever available BEFORE deliberately choosing a site. The later fallback to `FocusPlayer` and `Site0..5` is bypassed by any initially valid preview. This can accept a camera-relative site beside the enemy. There is no explicit forward-base tactical decision here.
2. Same planner builds a pad when readiness is eligible, no pad exists, and it is affordable; then recruits air when offered. There is no explicit return/land/service/relaunch planner sequence. Existing shared aircraft systems do have return/landing behavior; do not claim landing is wholly unimplemented.
3. `Systems/SkirmishCombatPolicy.cs`, `TryAssignTargets`: nearby threat query requires UnitAttack, and candidates require Damage > 0. Unarmed pads are omitted. Confirm which targeting paths own S003 squad and individual behavior before patching; this finding alone is not proof of the exact observed runtime cause.
4. `Configs/Skirmish/SkirmishRoleOverlayCatalog.cs`: rifle target domains exclude Structure; rocket and other roles can target Structure. A rifle refusing to damage a building is existing design, not automatically a bug.

Unconfirmed: exact missing-model cause. Do NOT label renderer suppression, pooling, terrain height, source-scene unload, or ownership as the cause without evidence. No specific user-observed run ID was supplied.

All code paths below are relative to `Assets/Game/Scripts/` unless marked otherwise.

## Patch A — deliberate safe placement (do first)

Files:
- `UI/Shell/Ecs/AriaSkirmishPlanSystem.cs`.
- `UI/Contracts/AriaSkirmishContracts.cs` (locate actual plan/observation structs before extending).
- `UI/Screens/MatchHudAssistantUiSystemHelper.SkirmishWatch.cs`.
- `Assets/Tests/Editor/SkirmishExpansion/SkirmishExpandedAriaTests.cs`.

Implement an explicit bounded helipad placement sequence using existing normal touch actions:
1. Identify helipad placement intent separately from defensive tower placement; do not reuse stale tower counters/deadlines as proof of pad-site selection.
2. Focus the player's base through the public camera control before opening pad construction. Camera actions must never order troops.
3. Choose an observed home-area pad site, tap that world point, then wait for the completed touch and fresh placement feedback. Only then may Confirm be tapped. A valid default preview is never sufficient.
4. Candidate sites should use the public own-base position, valid footprint, visible threats and available access/landing space. Keep them on the home side, outside the Barracks/roads/other buildings; reject known hostile proximity. Prefer dedicated pad sites rather than tower-defense sites. Define named distance limits from actual footprint/map scale, document their values, and test them; do not scatter unexplained constants or use hidden enemy state.
5. If no safe legal site is visible, bounded retry/refocus then cancel with a clear waiting/recovery reason. Do not silently fall back to the current enemy-side camera position, teleport the preview, or mutate entities.
6. Reset this plan state on cancel, stop, Replay and attempt changes. Preserve the existing two-tower opening.

Required automated cases: initially valid enemy-side preview cannot be confirmed; home focus alone cannot confirm; correct site tap + completed input + legal feedback permits one confirm; stale input/preview rejected; blocked sites cancel within the bound; repeated observations cannot double-build; Stop/Replay clears state; camera focus creates no move/attack command.

## Patch B — visible real model

Inspect:
- `Systems/BuildingPlacementVisualPresentationSystemHelper.cs`: `CreateBuildingVisualInstanceCore`, pooling, `DisableSourceRenderersOutsideCombinedMesh` (currently trusts existence of a transform named CombinedMesh).
- `Systems/BuildingPlacementCommitCompositionSystemHelper.cs`: `CommitSinglePlacement` reuses the preview instance as the committed visual.
- `Systems/BuildingPlacementConstructionTransaction.cs`, `BuildingPlacementVisualUpdateCompositionSystemHelper.cs`, `BuildingVisualSystem.cs`.
- `Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Helipad_Config.asset`; discover actual prefab/config linkage with rg, do not assume this config directly stores the prefab.
- Existing `Assets/Tests/Editor/BuildingVisualSystemTests.cs` and placement/visual tests discovered with rg.

Once validation is resumed, inspect a normally placed pad read-only: runtime building identity, faction/health, visual root and children, active flags, enabled renderers, nonempty mesh/material, layer, world bounds/terrain height, scale, preview property blocks, pooled state and source-scene lifetime. Capture both unselected and selected views. Diagnose before modifying shared renderer code.

Fix the smallest owning layer. If CombinedMesh suppression is responsible, require a usable active replacement renderer/mesh before suppressing originals; do not blindly enable every prefab renderer (can double-render other buildings). If preview/pool state is responsible, restore proper committed state and verify fresh and pooled placement. No substitute cube/selection rectangle, special S003 renderer, or hand-edited scene serialization.

Add a regression for the confirmed cause, plus at least one unaffected shared building. Acceptance: actual helipad surface/markings visible before and after deselection, correct footprint and height, survives camera movement and normal scene lifecycle, disappears with its destroyed/replayed building.

## Patch C — hostile infrastructure response

Inspect:
- `Systems/SkirmishCombatPolicy.cs` and `Assets/Tests/Editor/SkirmishCombatPolicyTests.cs`.
- `Runtime/Skirmish/SkirmishEnemyStrategySystem.cs`, `SkirmishExpandedEngagementService.cs`.
- `Systems/AITargetingSystem.cs`, `UnitEngagementSystem.cs`, `CombatTargetPolicyUtility.cs`.
- `Configs/Skirmish/SkirmishRoleOverlayCatalog.cs`.

Trace both strategic squad orders and local engagement in S003. Add living hostile structures as valid infrastructure targets without requiring their own offensive weapon. Use existing faction, visibility/perception, range/navigation and attacker target-domain policy. Prefer immediate combat threats when appropriate; route structure-capable defenders to a nearby intrusion. Rifle-only squads must not get stuck repeatedly attacking an immune target; preserve authored weapon domains unless a separate design change is requested.

No omniscient map-wide selection, no attack through unreachable terrain, no scenery/neutral/friendly/dead targets, no direct HP damage, no second combat simulation owner. Preserve S001/S002 policies; if shared targeting is changed, test both legacy and expanded behavior.

Cases: enemy structure-capable defender acquires hostile unarmed pad in defended area; actual shared combat damages it; friendly/neutral/scenery/dead/out-of-range targets ignored; rifle-only incompatible target rejected without loop; nearby armed threat handled; normal base objective resumes after pad destruction.

## Patch D — useful aircraft lifecycle

Inspect:
- `Runtime/Skirmish/SkirmishNativeProduction.cs`, `SkirmishProductionService.cs` (AirPadReady), `SkirmishAriaPublicProjection.cs`.
- `Systems/UnitAirMovementSystem.cs`, `AircraftFuelSafetyReturnSystem.cs`, `OperationMapHelipadReadModelUtility.cs`.
- `UI/Shell/Ecs/UiActionRequestDispatchSystemHelper.cs`: ReturnSelection dispatches ReturnToBase. Locate its actual visible button and shared command owner using rg.
- `Assets/Tests/Editor/OperationMapCurrentAircraftRuntimeAcceptanceTests.cs`, `OperationMapHelipadReadModelUtilityTests.cs`.

Trace ordinary helicopter purchase -> real paid delivery -> owning/home pad binding -> takeoff -> attack -> return -> touchdown -> service -> second takeoff. Reuse shared owners. Check actual fuel semantics first: AircraftFuelSafetyReturnSystem currently examines usable faction storage; do not invent an onboard fuel mechanic or free refill.

Extend ARIA observations with only public player-readable aircraft/service state needed for decisions. Add bounded return/service/relaunch planning through visible selection and Return controls; do not continuously overwrite return orders with assault orders. Only buy/build when there is a credible affordable aircraft role and usable pad; do not spam pads or add a fake landing just to satisfy a test. A match ending before a landing is not itself a bug, but lifecycle acceptance needs a scenario where a sortie can return and relaunch naturally.

Handle occupied pad, destroyed pad, queued aircraft whose producer dies, unavailable fuel, lost aircraft, Stop and Replay using existing policies with clear bounded recovery. Do not reset resources or force a terminal outcome to make the test work.

Any newly used Return command must be included in `AriaCommandEvidence` and the input evidence audit BEFORE a zero-violation run can claim coverage. Current audit only covers previously supported planner controls; extend actual command boundaries, not just labels.

## Validation after the user resumes it

Read `/Users/farhad/.agents/skills/unity-cli/SKILL.md` before Unity CLI use. Keep Hub open/signed in. On macOS use ONLY the repository wrapper for launches/tests/captures; no direct Unity, no batchmode, no IPC reset, no termination of unrelated Editors. Respect active-project ownership. Source must remain unchanged during each native run.

First run focused new tests integrated into the existing broader entry, then:

```sh
rtk proxy Tools/CI/invoke_unity_macos.sh --project /private/tmp/warline-s003-player-ready --timeout 600 --log /private/tmp/s003-helipad-regressions-01.log -- -quit -executeMethod Game.Tests.Editor.SkirmishSharedMaterialsTests.RunBroaderAndCampaignValidation
```

Require exit 0, no compile/test failures, every constituent marker listed in the progress record, aggregate `[SkirmishPlayerReadyRegressions] result=Passed`, and Campaign `[M01FirstContactCampaignUiValidation] result=Passed tests=10 captures=3`. Exit 0 alone is insufficient. Preserve full logs and failed attempts. Wait for wrapper shutdown before the next same-project launch.

Existing normal ARIA entry (run only after resume):

```sh
WARLINE_REVIEW_WIDTH=2400 WARLINE_REVIEW_HEIGHT=1080 WARLINE_ARIA_BACKGROUND_VALIDATION=1 WARLINE_SKIRMISH_CATALOG=S003 WARLINE_S002_SEED=130366 WARLINE_S002_LOCALE=fa-IR WARLINE_TERMINAL_ACTION=REPLAY rtk proxy Tools/CI/invoke_unity_macos.sh --project /private/tmp/warline-s003-player-ready --timeout 1500 --log /private/tmp/s003-helipad-native-01-fa.log -- -executeMethod Game.Editor.SkirmishS002AriaRunHarness.RunFocusedAriaAndExit
```

Native harness has no -quit. Verify the entry signature from source before executing.

Normal input evidence must show: home-area construction with visible model; enemy response to a legally placed hostile pad in their area (can be a separate human-equivalent test); paid helicopter delivery; takeoff/attack/return/touchdown/service/second sortie; occupied/destroyed-pad recovery; terminal freeze and normal Replay/Main Menu return. EN 1280x720 and FA 2400x1080 visual review. Use existing production-probe touchdriver pattern if a bounded aircraft probe is needed; its current AA probe does not prove air lifecycle. Do not force outcomes, invoke button callbacks directly, grant resources, spawn units or change ARIA plan state in acceptance runs. Diagnostic/fixture injection must remain explicitly separate from native acceptance.

## Completion report / stop rules

For each patch report exact cause, owner changed, tests, native evidence and remaining uncertainty. Append progress and preserve original candidate hashes/rows. Mark this bounded repair complete only when its normal-input criteria pass. Automated fixtures, rendered mockups and older victories cannot establish player readiness. Full native checkpoints, remaining mission matrix and real player/device acceptance are separate outstanding gates; do not claim those are solved by this repair.

If blocked by an unknown runtime fact while validation is paused, record the exact read-only observation needed and continue independent source work; do not guess or start validation. If a shared change breaks a baseline test, fix the change rather than weaken the assertion. No commit/PR/publication or release-gate promotion is required by this planning request.

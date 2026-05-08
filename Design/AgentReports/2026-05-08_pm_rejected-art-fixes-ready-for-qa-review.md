# PM Review: Rejected-Art Fixes Ready For QA/HCI Rerun

Date: 2026-05-08
Status: accepted for QA/HCI rerun; not final Gate 4 acceptance

## Lane

PM

## Task

Review the Designer, Art/Atlas, and Gameplay handoffs created after the user rejected temporary Gate 4 art/runtime behavior, then route the next owner.

## Files changed

- `Design/AgentReports/2026-05-08_pm_rejected-art-fixes-ready-for-qa-review.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`

Accepted upstream handoffs:

- `Design/AgentReports/2026-05-08_designer_m01-metric-scale-readability-contract.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-rejected-temp-art-scale-readability.md`
- `Design/AgentReports/2026-05-08_gameplay_m01-ecs-scale-selection-motion-fix.md`
- `Design/AgentReports/2026-05-08_art-atlas_designer-metric-contract-watch.md`

## Contracts touched

- M01 Gate 4 remains blocked until QA/HCI reruns and PM/user reviews the new public evidence.
- Designer contract is accepted as the next QA checklist source for M01 metric scale/readability.
- Art/Atlas scale/readability package is accepted for next QA, but final art remains blocked.
- Gameplay runtime fix report is accepted for next QA, based on its reported focused PlayMode pass and code/test evidence.

## User-visible behavior

No new PM-authored runtime behavior. Expected next reviewed build/captures should show:

- public M01 unit visuals through ECS atlas quad presentation, not SpriteRenderer public unit visuals
- runtime root naming no longer exposing `M01RuntimeSpriteRenderers`
- infantry scale near `0.20`
- visible M01 building/decor scale direction near `0.80` where used as door/road-context readability anchor
- small grounded per-soldier selection markers
- slower realistic infantry movement
- visible move/run animation while moving

## Validation run

PM reviewed:

- `Design/AgentReports/2026-05-08_designer_m01-metric-scale-readability-contract.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-rejected-temp-art-scale-readability.md`
- `Design/AgentReports/2026-05-08_gameplay_m01-ecs-scale-selection-motion-fix.md`
- `Design/AgentReports/2026-05-08_art-atlas_designer-metric-contract-watch.md`

PM spot-checked local evidence with `rg` for:

- `M01RuntimeEcsAtlasQuads`
- `MissionRuntimeSpriteRendererRuntime`
- `SpriteRenderer`
- `0.20`
- `0.80`
- `0.42`
- `0.28`
- selection marker assertions

PM did not rerun Unity. Gameplay reports:

`/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-gameplay-ecs-scale-selection-motion-results.xml -logFile /private/tmp/warlinecapture-gameplay-ecs-scale-selection-motion.log`

with `Chapter01M01PlayModeValidationTests` passing `8/8`.

## Validation result

Accepted for QA/HCI rerun only.

The reports satisfy the immediate PM routing requirements:

- Designer provided a metric scale/readability contract.
- Art/Atlas aligned scale/readability and selection-art direction with the user rejection.
- Gameplay reports runtime fixes and focused test pass for ECS-only unit presentation, no public unit SpriteRenderer path, small per-soldier markers, calibrated movement speed, and move animation proof.

## Known gaps

- Not final Gate 4 acceptance.
- PM did not manually review the refreshed Unity route or captures.
- QA/HCI has not rerun from its validation workspace after all three handoffs landed.
- Final Art/Atlas gaps remain: final multi-frame run/walk loops, enemy final variant, final impact VFX, final destroyed/death VFX.
- User review is still required after QA/HCI provides the new public captures and step-by-step review instruction.

## Cross-lane impacts

- QA/HCI is now the active owner.
- Art/Atlas, Designer, and Gameplay should wait unless QA/HCI reports a concrete fix request.
- UI and Support/FTUE remain waiting unless QA/HCI finds a concrete HUD, assistant, or FTUE regression.
- PM owns final acceptance, user notification, commit/push, and Gate 4 routing.

## Next recommended task

QA/HCI should rerun focused Gate 4 validation and write:

`Design/AgentReports/2026-05-08_qa-hci_gate4-rejected-art-rerun.md`

Do not ask the user to review until QA/HCI proves the route and provides current public captures.

# PM Review: QA/HCI Rejected-Art Rerun

Date: 2026-05-08
Status: needs fixes before user review

## Lane

PM

## Task

Review QA/HCI's rejected-art Gate 4 rerun and decide whether the current evidence is ready for user approval.

## Files changed

- `Design/AgentReports/2026-05-08_pm_qa-hci-rejected-art-rerun-review.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`

## Contracts touched

- Gate 4 remains blocked.
- QA/HCI rerun is accepted as automated proof, but rejected as user-review readiness.
- The selected first-control public captures are not ready for PM/user approval because the squad readability and selected-state clarity still do not meet the user's stated review bar.

## User-visible behavior

PM reviewed:

- `Design/AgentReports/2026-05-08_qa-hci_gate4-rejected-art-rerun.md`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`

Automated validation passed, but the selected first-control captures still have visual issues:

1. The player squad reads as a crowded blob/duplicated cluster at gameplay scale, not clearly as four distinct individual soldiers.
2. The selected-state treatment is too subtle or unclear in the public capture. It does not clearly show a small grounded marker under each soldier.
3. The unit card/icon still visually reinforces a clustered multi-soldier sprite, which risks repeating the user's earlier objection that the art reads like a group sprite instead of separate soldiers.

## Validation run

PM did not rerun Unity. QA/HCI reported:

`/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-qa-gate4-rejected-art-rerun-results.xml -logFile /private/tmp/warlinecapture-qa-gate4-rejected-art-rerun.log`

QA/HCI result: `Chapter01M01PlayModeValidationTests` passed `8/8`.

PM visual check used the fresh selected first-control captures listed above.

## Validation result

Needs fixes before user review.

The automated pass is useful and should be preserved, but PM should not ask the user to approve these captures yet.

## Known gaps

- The public capture still does not clearly prove the user's requested "4 different soldiers" readability.
- The selected markers are not obvious enough in the selected first-control capture.
- The current capture does not visually prove final or acceptable temporary art. Final Art/Atlas gaps also remain.

## Cross-lane impacts

- Gameplay owns runtime layout/selection visibility and should adjust soldier spacing/marker visibility.
- Art/Atlas owns whether the current atlas source is causing each runtime soldier to read like a mini-squad/cluster instead of one soldier, and should provide a corrected individual-soldier frame/manifest recommendation if needed.
- QA/HCI waits for Gameplay and Art/Atlas follow-up before rerunning.
- UI and Support/FTUE have no action unless later QA finds concrete HUD/assistant regressions.
- User does not need to review this pass.

## Next recommended task

Gameplay and Art/Atlas should fix the selected first-control readability:

1. World squad must read as four distinct individual soldiers, not a crowded duplicated cluster.
2. Each selected soldier must have a visible but small grounded marker under/near the feet.
3. The selected first-control capture must be reviewable without asking the user to infer selection state.

Expected reports:

- `Design/AgentReports/2026-05-08_gameplay_m01-soldier-readability-selection-fix.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-individual-soldier-frame-review.md`

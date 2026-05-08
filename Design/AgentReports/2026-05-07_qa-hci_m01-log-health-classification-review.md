Lane:
QA/HCI

Task:
QA/HCI review of the Gameplay M01 log-health classification handoff.

Files changed:
- Design/AgentReports/2026-05-07_qa-hci_m01-log-health-classification-review.md

Contracts touched:
- Design/AgentTasks/qa-hci_current.md
- Design/AgentTasks/M01_CRITICAL_PATH.md
- Design/AgentReports/2026-05-07_gameplay_m01-log-health-classification.md
- Design/AgentReports/2026-05-07_qa-hci_m01-watcher-smoke-regression.md
- Design/AgentReports/2026-05-07_pm_design-audit-qa-report-filename-collision.md

User-visible behavior:
No runtime behavior changed in this QA pass. Gameplay's handoff claims M01 generic AI plan noise is now disabled for active fixed tactical M01, but QA could not produce fresh Unity pass/fail evidence because Unity licensing failed before Test Runner execution.

Validation run:
- Read Gameplay handoff: `Design/AgentReports/2026-05-07_gameplay_m01-log-health-classification.md`.
- Reviewed diff in `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs` and `Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs`.
- Static scan for M01 AI-plan guardrail symbols in touched files.
- Attempted Unity EditMode validation in QA workspace:
  - `Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform EditMode -testFilter Chapter01M01PlayableRuntimeTests -testResults /private/tmp/warlinecapture-qa-hci-log-health-runtime-results.xml -logFile /private/tmp/warlinecapture-qa-hci-log-health-runtime.log`

Validation result:
- Static review: the Gameplay change gates `AIBuildPlan`, `AIProductionPlan`, and `AISquadPlan` disabling behind `DisableGenericAIPlansForFixedTacticalMission(world, Chapter01M01PlayableRuntime.IsActiveMission())`; the focused test asserts plans stay enabled when inactive and are disabled when active.
- Unity validation: blocked. `/private/tmp/warlinecapture-qa-hci-log-health-runtime-results.xml` was not produced.
- Blocking log evidence: `/private/tmp/warlinecapture-qa-hci-log-health-runtime.log` repeatedly reports Unity Licensing Client failures, including timeout waiting for licensing initialization, lost licensing connection, unsuccessful reconnects, and `com.unity.editor.headless` not found.
- QA stopped the hung Unity validation process it started after licensing failed.

Known gaps:
- No fresh QA Unity pass/fail evidence exists for the Gameplay log-health change.
- No fresh PlayMode log exists to confirm `AIProduction`, `AIBuild`, and `AISquad` noise is gone.
- No player/device or non-headless classification exists for package-side `NullReferenceException`, headless render-target failures, preview-scene leak warning, or persistent allocation warnings.
- UI integrated 16:9/20:9 capture matrix handoff has not landed.
- Active balance QA remains blocked.

Cross-lane impacts:
- Gameplay's source-level fix is plausible, but Gate 4 should not accept it until Unity licensing is healthy enough to rerun focused EditMode and PlayMode validation.
- UI remains on the critical path for the locked integrated capture matrix.
- PM should resolve the QA report filename collision before the next full Gate 4 readiness report; this review used a unique filename to preserve the already-reviewed smoke-regression report.

Next recommended task:
Resolve Unity Licensing Client/headless entitlement failure, then rerun `Chapter01M01PlayableRuntimeTests` and `Chapter01M01PlayModeValidationTests` in a dedicated QA or Gameplay Unity workspace. If tests pass and the PlayMode log no longer contains generic AI plan noise, QA/HCI can downgrade that finding and continue toward the integrated 16:9/20:9 Gate 4 pass.

Severity:
- Blocker: Unity licensing failure prevents required QA validation for the Gameplay log-health handoff.
- Major: Gate 4 remains blocked by missing UI capture matrix and missing fresh log-health confirmation.
- Minor: none newly confirmed.
- Polish: none.

Reproduction steps:
1. Run the focused Unity command listed in `Validation run`.
2. Observe that no test-result XML is produced.
3. Inspect `/private/tmp/warlinecapture-qa-hci-log-health-runtime.log`.
4. Confirm repeated Unity Licensing Client reconnect failures and `com.unity.editor.headless` entitlement failure before Test Runner completion.

Expected vs actual:
- Expected: focused `Chapter01M01PlayableRuntimeTests` completes and proves the new M01 AI-plan guardrail.
- Actual: Unity never reaches Test Runner completion because licensing initialization/reconnect fails.
- Expected for Gate 4: Gameplay log-health and UI capture handoffs can be verified with fresh QA evidence.
- Actual for Gate 4: Gameplay handoff is source-plausible but not QA-accepted; UI capture handoff is still absent.

Affected lane:
gameplay / UI / QA-HCI

Whether this blocks the next milestone:
Yes. Gate 4 and active balance QA remain blocked until Unity validation can run and the UI capture matrix lands.

Recommended owner:
Gameplay or environment owner for Unity licensing/log-health rerun; UI for integrated capture matrix; QA/HCI for final Gate 4 validation once both handoffs are verifiable.

Lane:
QA/HCI

Task:
M01 watcher smoke regression after accepted gameplay sprite-renderer capture evidence and accepted UI/support assistant runtime handoffs.

Files changed:
- Design/AgentReports/2026-05-07_qa-hci_m01-watcher-smoke-regression.md

Contracts touched:
- Design/AgentTasks/M01_CRITICAL_PATH.md
- Design/AgentTasks/qa-hci_current.md
- Design/Agent_Coordination_Workflow.md
- Design/M01_FirstContact_Production_Contract.md
- Design/FTUE_And_Command_Assistant_Design.md
- Design/AssistantPanel_M01_Implementation_Contract.md
- Design/AssistantRuntime_M01_Wiring_Plan.md
- Design/AgentReports/2026-05-07_qa-hci_m01-validation-plan.md
- Design/AgentReports/2026-05-07_pm_qa-hci-validation-plan-review.md
- Design/AgentReports/2026-05-07_gameplay_m01-playmode-validation.md
- Design/AgentReports/2026-05-07_gameplay_m01-log-performance-fixed-roads.md
- Design/AgentReports/2026-05-07_pm_gameplay-m01-log-performance-fixed-roads-review.md
- Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer-capture-fix.md
- Design/AgentReports/2026-05-07_pm_gameplay-m01-sprite-capture-fix-review.md
- Design/AgentReports/2026-05-07_ui_assistant-runtime-binding-fix.md
- Design/AgentReports/2026-05-07_pm_ui-assistant-runtime-binding-fix-review.md
- Design/AgentReports/2026-05-07_support-ftue_command-intent-executor.md
- Design/AgentReports/2026-05-07_support-ftue_live-assistant-context-provider.md
- Design/AgentReports/2026-05-07_pm_design-audit-qa-capture-matrix.md

User-visible behavior:
No runtime behavior changed. This pass updates QA/HCI readiness status using focused smoke tests, log-health review, and current close tactical sprite evidence.

Validation run:
- QA Unity workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity2`
- Unity PlayMode: `Chapter01M01PlayModeValidationTests`
- Unity EditMode: `Chapter01M01PlayableRuntimeTests`
- Unity EditMode: `Chapter01TacticalRuntimeBindingTests`
- Unity EditMode: `Chapter01M01SpriteRendererTests`
- Unity EditMode: `WarlineCaptureUiAssistantRuntimeBindingTests`
- Unity EditMode: `M01AssistantRuntimeTests`
- Unity EditMode: `AssistantContextProviderTests`
- Unity EditMode: `CommandIntentExecutorTests`
- Log scan for `NullReferenceException`, `RenderTexture.Create failed`, preview-scene leaks, persistent allocation leaks, `FreezeDetect`, `PerfDiag`, `AIProduction`, `AIBuild`, and `AISquad`.
- Visual evidence review: `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png`.

Validation result:
- `Chapter01M01PlayModeValidationTests`: passed 3/3, `/private/tmp/warlinecapture-qa-hci-m01-watcher-playmode-results.xml`
- `Chapter01M01PlayableRuntimeTests`: passed 8/8, `/private/tmp/warlinecapture-qa-hci-m01-watcher-runtime-results.xml`
- `Chapter01TacticalRuntimeBindingTests`: passed 6/6, `/private/tmp/warlinecapture-qa-hci-m01-watcher-tactical-binding-results.xml`
- `Chapter01M01SpriteRendererTests`: passed 4/4, `/private/tmp/warlinecapture-qa-hci-m01-watcher-sprite-renderer-results.xml`
- `WarlineCaptureUiAssistantRuntimeBindingTests`: passed 7/7, `/private/tmp/warlinecapture-qa-hci-m01-watcher-ui-assistant-binding-results.xml`
- `M01AssistantRuntimeTests`: passed 9/9, `/private/tmp/warlinecapture-qa-hci-m01-watcher-assistant-runtime-results.xml`
- `AssistantContextProviderTests`: passed 7/7, `/private/tmp/warlinecapture-qa-hci-m01-watcher-assistant-context-results.xml`
- `CommandIntentExecutorTests`: passed 14/14, `/private/tmp/warlinecapture-qa-hci-m01-watcher-command-intent-results.xml`
- Old `RuntimeCitySpawner=1350.3ms`/`FreezeDetect` hitch did not reproduce in the QA PlayMode smoke log. No `FreezeDetect` or `PerfDiag` entries appeared in `/private/tmp/warlinecapture-qa-hci-m01-watcher-playmode.log`.
- Remaining log-health risks did reproduce in PlayMode: package-side `NullReferenceException` in `EntitiesGraphicsSystemUtility.RootsHandlerDelegate`, headless `RenderTexture.Create failed`, preview-scene leak warning, persistent allocation leak warning, and generic AI plan noise.
- Close capture review: `M01_SpriteRenderer_CloseCapture.png` is 1920x1080 RGBA and shows the command/decor proxy, player squad, and hostile patrol grounded on the M01 tactical map. This is acceptable as current review evidence only, not final art approval.

Known gaps:
- Integrated human M01 smoke was not completed in this pass.
- No new graphics capture matrix was run. The reviewed close capture is 16:9 only; 20:9 integrated gameplay capture remains pending.
- No Android/player-device log pass was run, so package-side NullReference and headless render-target errors remain classified as editor/headless risks rather than fully benign.
- Final art approval remains blocked on final atlas/config packaging, hostile non-color readability treatment, and `vfx.unit.destroyed.small`.
- World highlight rendering for `Show Me` remains outside the accepted UI runtime-binding pass and should stay behind typed preview/focus contracts.

Cross-lane impacts:
- Gameplay: technical M01 smoke remains green, old city-spawn hitch stays downgraded, but player/device log classification is still needed for package-side NullReference/render-target/leak warnings.
- UI: assistant runtime binding tests remain green; integrated smoke still must visually verify assistant ownership status, player-input release, result-flow Stop, HUD occlusion, and 16:9/20:9 captures.
- Support/FTUE: command intents, live context, and recommendation logic tests remain green; integrated smoke still must verify player-readable assistant guidance, not only DTO/service correctness.
- Art/design: current close capture supports grounding/scale review, but final asset approval and hostile non-color readability treatment remain open.

Next recommended task:
Run an integrated manual M01 smoke pass with the locked capture matrix once PM/user pins exact 16:9 and 20:9 resolutions/safe-area assumptions or confirms the current capture matrix. Include player/device or non-headless log collection to classify remaining Unity package/headless warnings before active balance QA.

Severity:
- Blocker: none found in automated smoke. Gate 4 is not fully passable yet because integrated manual smoke/captures are still missing.
- Major: remaining PlayMode log-health risks require device/non-headless classification before active balance QA can rely on timing and stability observations.
- Major: hostile non-color readability and final destroyed VFX remain unresolved for final art/integration readiness.
- Minor: generic AI plan noise (`AIProduction MissingProducerBuilding`, `AIBuild Blocked`, `AISquad Waiting`) still appears in M01 PlayMode logs and can obscure QA triage.
- Polish: ARIA status/copy readability still needs human capture review during integrated smoke.

Reproduction steps:
1. Open `/Users/farhad/Projects/WarlineCapture-CodexUnity2`.
2. Run the focused smoke filters listed in `Validation run`.
3. Scan `/private/tmp/warlinecapture-qa-hci-m01-watcher-playmode.log` for stability terms listed above.
4. Review `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png` at native 1920x1080.
5. For final Gate 4, run the integrated player route and capture match start, squad selected, move feedback, attack feedback, invalid recovery, assistant open, assistant takeover/Stop, and result popup at locked 16:9 and 20:9 resolutions.

Expected vs actual:
- Expected automated smoke: accepted gameplay, UI, and Support/FTUE handoff tests continue to pass in the QA workspace.
- Actual automated smoke: all focused filters passed.
- Expected log health before active balance QA: no recurring exceptions, no visible freeze/hitch output, no severe log spam that can hide real M01 failures, and device/non-headless classification for editor-only noise.
- Actual log health: no `FreezeDetect`/`PerfDiag` regression, but package-side NullReference, headless render-target failures, preview/persistent allocation leak warnings, and AI plan noise still appear in PlayMode logs.
- Expected visual evidence: current capture should prove grounding and scale without implying final art approval.
- Actual visual evidence: grounding/scale are reviewable at 1920x1080; hostile patrol is visible but final hostile non-color readability treatment remains open.

Affected lane:
gameplay / UI / support-FTUE / art-design

Whether this blocks the next milestone:
Yes for active balance QA and M02 expansion. Gate 4 still needs integrated manual smoke with no blocker findings and reproducible 16:9/20:9 capture evidence. The automated smoke baseline is green but not sufficient for balance conclusions.

Recommended owner:
QA/HCI owns the Gate 4 smoke pass and issue classification. Gameplay owns log/device classification and remaining VFX/art integration hooks. UI owns integrated capture readability, assistant ownership/status, player-input release, and result-flow Stop visibility. Support/FTUE owns assistant recommendation behavior and typed-intent correctness. Art/design owns final asset approval and hostile non-color readability.

## Current Balance-QA Gate Status

Active balance QA remains blocked. The old city-spawn hitch is downgraded and all focused tests passed, but the integrated human smoke pass, capture matrix, player/device log classification, and final readability checks are still missing.

## Performance / Freeze / Log-Health Findings

### QAHCI-M01-001: Remaining PlayMode log warnings need device/non-headless classification

- Severity: Major
- Affected lane: gameplay / support-FTUE
- Reproduction steps: run `Chapter01M01PlayModeValidationTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`, then scan `/private/tmp/warlinecapture-qa-hci-m01-watcher-playmode.log`.
- Expected: no recurring exceptions/leak warnings in the M01 smoke log, or a documented benign classification backed by player/device or non-headless evidence.
- Actual: PlayMode still logs `NullReferenceException` from Unity Entities Graphics resource-GC roots, headless `RenderTexture.Create failed`, preview-scene leak warning, and persistent allocation leak warning.
- Blocks next milestone: blocks active balance QA until classified as benign outside headless/editor conditions or explicitly waived by PM.
- Recommended owner: Gameplay, with QA/HCI verification.

### QAHCI-M01-002: Generic AI plan noise still appears in M01 PlayMode logs

- Severity: Minor
- Affected lane: gameplay / support-FTUE
- Reproduction steps: run the same PlayMode smoke and scan for `AIProduction`, `AIBuild`, and `AISquad`.
- Expected: M01 smoke logs should keep unrelated generic AI plan failures quiet or clearly classified so real command/assistant issues are easy to spot.
- Actual: repeated `AIProduction MissingProducerBuilding`, `AIBuild Blocked`, and `AISquad Waiting` entries appear.
- Blocks next milestone: no, unless it becomes player-visible or masks real failures during integrated smoke.
- Recommended owner: Gameplay.

### QAHCI-M01-003: Integrated capture/readability matrix remains incomplete

- Severity: Major
- Affected lane: UI / art-design / QA-HCI
- Reproduction steps: review the available close capture and compare against PM capture-matrix guidance.
- Expected: final Gate 4 smoke includes locked 16:9 and 20:9 captures for match start, selected squad, move, attack, invalid recovery, assistant open, assistant takeover/Stop, and result popup.
- Actual: current close capture is 1920x1080 only and useful for sprite grounding/scale. No new 20:9 integrated gameplay capture was produced in this watcher pass.
- Blocks next milestone: yes for final Gate 4 acceptance and active balance QA.
- Recommended owner: QA/HCI with UI and art/design support.

## New HCI Risks Introduced Or Carried Forward

- The accepted assistant runtime binding is strong at the service/test level, but still needs human-visible validation of ownership status, player-input release, and Stop behavior during the result flow.
- The close tactical capture makes the squads inspectable, but hostile identity still needs a non-color-only treatment before final art approval.
- Because `Show Me` world highlight rendering is outside the accepted UI pass, integrated smoke must ensure assistant guidance does not promise a visual highlight that is absent or ambiguous.
- The automated smoke suite validates command and assistant readiness, but balance QA would still be invalid if the first human route has input stalls, occluded HUD elements, or unclear enemy/friendly reads.

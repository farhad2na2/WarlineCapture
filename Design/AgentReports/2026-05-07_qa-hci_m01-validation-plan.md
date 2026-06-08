Lane:
QA/HCI

Task:
M01 QA/HCI validation plan for the QA Unity workspace before active gameplay balancing.

Files changed:
- Design/AgentReports/2026-05-07_qa-hci_m01-validation-plan.md

Contracts touched:
- Design/Agent_Coordination_Workflow.md
- Design/M01_FirstContact_Production_Contract.md
- Design/FTUE_And_Command_Assistant_Design.md
- Design/AssistantPanel_M01_Implementation_Contract.md
- Design/AssistantRuntime_M01_Wiring_Plan.md
- Design/AgentReports/2026-05-07_gameplay_m01-playable-runtime.md
- Design/AgentReports/2026-05-07_pm_ui-assistant-presentation-review.md
- Design/AgentReports/2026-05-07_pm_support-runtime-wiring-review.md

User-visible behavior:
This pass does not change runtime behavior. It defines the QA/HCI gate for M01 usability, tutorial clarity, command feedback, visual readability, failure recovery, assistant behavior, and the minimum integrated-playable threshold before active balance QA.

Validation run:
- QA Unity workspace used: `/Users/farhad/Projects/WarlineCapture-CodexUnity2`
- Unity PlayMode: `Chapter01M01PlayModeValidationTests`
- Unity EditMode: `Chapter01M01PlayableRuntimeTests`
- Unity EditMode: `Chapter01TacticalRuntimeBindingTests`
- Unity EditMode: `WarlineCaptureUiAssistantPanelControllerTests`
- Doc cross-check against the listed source contracts and reports.

Validation result:
- `Chapter01M01PlayModeValidationTests`: 3/3 passed, `/private/tmp/warlinecapture-codexunity2-m01-playmode-results.xml`
- `Chapter01M01PlayableRuntimeTests`: 7/7 passed, `/private/tmp/warlinecapture-codexunity2-m01-runtime-editmode-results.xml`
- `Chapter01TacticalRuntimeBindingTests`: 4/4 passed, `/private/tmp/warlinecapture-codexunity2-m01-tactical-binding-results.xml`
- `WarlineCaptureUiAssistantPanelControllerTests`: 4/4 passed, `/private/tmp/warlinecapture-codexunity2-assistant-panel-controller-results.xml`
- PlayMode log caveat: tests passed with suppressed runtime noise including `NullReferenceException`, `RenderTexture.Create failed`, a preview-scene leak warning, and performance hitches such as `RuntimeCitySpawner=2064.9ms`. Treat these as readiness risks for manual HCI/balance validation, not as test failures.

Known gaps:
- No manual player-operated run was completed in this pass.
- No Android/device validation was run.
- No capture review was produced for 16:9, 20:9, or mobile touch targets.
- M01 assistant runtime is not implemented yet: `WarlineCaptureAssistantService`, `M01AssistantRecommendationProvider`, typed command intent execution, highlight/takeover ownership, and tutorial persistence remain future work per the accepted runtime wiring plan.
- UI assistant shell exists as a controller surface but is not mounted into the match HUD/app shell yet.
- Active balance conclusions are blocked until the integrated M01 slice is playable end to end by a human through the real visible UI.

Cross-lane impacts:
- Gameplay owns clean playable-scene execution, typed command wrappers, log cleanup, camera/bounds behavior, and performance readiness before balance QA.
- UI owns match HUD mount, command feedback readability, assistant panel mount, world marker readability, minimap bridge, and capture validation.
- Support/FTUE owns assistant state production, typed Show Me / Do It / Stop intent rules, interruption/cancel behavior, and tutorial replay suppression.
- Art/design owns close-camera readability of the M01 ground, unit silhouettes, hostile/friendly distinction, and world marker visual priority.

Next recommended task:
Run a manual HCI playthrough only after the gameplay PlayMode scene is exposed through the real player route and UI/FTUE mount the required assistant surfaces. Use the checklist below as the pass/fail gate before starting active M01 balance testing.

Severity:
- Blocker: prevents M01 from being completed, failed, restarted, understood, or safely controlled in the integrated route; blocks the next milestone.
- Major: core command, tutorial, visual priority, feedback, or performance issue that can mislead players or invalidate balance data; blocks active balance QA until fixed or explicitly waived.
- Minor: localized usability/readability issue with a workaround; does not block balance QA if tracked.
- Polish: presentation, copy, timing, or comfort issue that should improve before content lock but does not block functional QA.

Reproduction steps:
1. Open the QA Unity workspace at `/Users/farhad/Projects/WarlineCapture-CodexUnity2`.
2. Run the focused automated smoke set listed in `Validation run`.
3. Launch M01 through the real player route when available.
4. Complete a no-assist pass: select squad, move to cover, attack patrol, reject Build, finish result, replay.
5. Complete a Full Guidance pass: ARIA objective intro, select, move, attack, invalid command recovery, result explanation, Stop/cancel behavior.
6. Repeat on desktop aspect 16:9 and mobile/tall target aspect 20:9; capture start, selected, move, attack, invalid command, result, and assistant states.

Expected vs actual:
- Expected now: a QA/HCI plan and smoke-test baseline, not balance approval.
- Actual now: automated M01 runtime/data/controller smoke tests pass in the QA Unity workspace, but the integrated human play route and assistant runtime are not ready for active balance conclusions.
- Expected before active balance QA: clean manual end-to-end completion through visible UI, clear command feedback, assistant Show Me / Do It / Stop behavior using typed ids, stable frame behavior, and reviewed captures.
- Actual blockers before balance QA: assistant runtime/mount remains incomplete, manual player-operated validation remains pending, and PlayMode logs show runtime noise/hitches that can distort HCI and balance observations.

Affected lane:
gameplay / UI / support-FTUE / art-design

Whether this blocks the next milestone:
Yes for active M01 balance QA. Automated tests are encouraging, but balance testing should not begin until the integrated playable route and HCI gates below pass.

Recommended owner:
QA/HCI owns the gate and report discipline. Gameplay owns runtime/log/performance blockers. UI owns HUD and assistant presentation in match. Support/FTUE owns assistant recommendation/service behavior. Art-design owns visual readability approval.

## Readiness Gate For Active Balance QA

Active M01 balance QA may start only when all of these are true:

- Integrated route: a human can enter M01 from the intended flow, not only from tests or editor-only prototype scenes.
- Select/move/attack: friendly squad selection, move order, attack order, and command mode feedback work through visible controls and direct-tap flows.
- Objective/result: enemy patrol destruction visibly updates the objective and opens the M01 result flow only while the command squad survives.
- Failure guard: command squad destruction blocks or fails completion with understandable feedback.
- Build rejection: Build never opens production in M01 and shows `Building unlocks in the next mission.`
- Assistant baseline: ARIA can show objectives, select, move, attack, invalid recovery, and result recommendations from typed ids, with no screen-coordinate automation.
- Control ownership: `Show Me`, `Do It`, and `Stop` are interruptible; player input cancels assistant preview/takeover at a clear boundary.
- Visual readability: squad, patrol, selection ring, move marker, attack marker, invalid feedback, objective marker, and minimap viewport are readable at close gameplay scale.
- Performance/log hygiene: no recurring runtime exceptions in normal play and no large first-interaction hitches that would invalidate command timing/balance observations.
- Capture coverage: 16:9 and 20:9 screenshots show no occlusion of squad, patrol, objective tracker, command feedback, assistant panel, result popup, or minimap.

## QA Unity Workspace Validation Checklist

Automated smoke tests:

- PlayMode `Chapter01M01PlayModeValidationTests`: confirms scene-spawned squad/patrol, anchors, camera start, selection, attack damage, result guard, command-squad survival guard, and Build rejection.
- EditMode `Chapter01M01PlayableRuntimeTests`: confirms M01 ids, metadata anchors, runtime entity creation/binding, objective completion, failure guard, and shared build-disabled reason.
- EditMode `Chapter01TacticalRuntimeBindingTests`: confirms M01 tactical metadata/binder contracts and camera/grid plane assumptions.
- EditMode `WarlineCaptureCampaignObjectiveTests`: run when objective/result rules change.
- EditMode `BattleHudGameplayBridgeConnectionTests`: run when command feedback or reason mappings change.
- EditMode `WarlineCaptureUiAssistantPanelControllerTests` and `WarlineCaptureUiAssistantPanelTests`: run when assistant panel binding, callbacks, or view fields change.
- Future assistant runtime tests from `AssistantRuntime_M01_Wiring_Plan.md`: recommendation provider, context provider, typed intents, invalid recovery, control ownership, and replay suppression.

Manual playthrough checks:

- Start M01 from the player route; verify first camera lands on `camera.default_start` and clamps to tactical bounds.
- Tap the friendly squad; verify selection ring, selected entity panel, and feedback copy appear without ambiguity.
- Tap valid ground and explicit Move; verify move marker/path feedback and command banner clear after acceptance.
- Tap enemy patrol and explicit Attack; verify hostile target highlight, attack marker, damage feedback, objective progress, and no confusion between friendly/enemy silhouettes.
- Tap invalid ground/out-of-bounds/blocked target; verify typed reason text and recovery target, not generic silence.
- Tap Build; verify the shared M01 disabled copy and no build mode side effects.
- Destroy patrol with squad alive; verify objective complete, result route/popup, result explanation, stars/rewards/city impact copy.
- Destroy command squad first; verify M01 cannot complete and failure/blocked-completion feedback is player-readable.
- Use ARIA Show Me / Do It / Stop for each M01 step; verify typed target ids, interruptibility, no raw-coordinate behavior, and no full autopilot.
- Replay M01; verify completed tutorial steps do not replay incorrectly while contextual recommendations remain available.

## Pass/Fail Criteria

- Select: Pass if the player can identify and select the rifle squad within 3 seconds of the prompt, and the selected state is visible in both world marker and HUD. Fail if the squad blends into the ground, selection is only visible in ECS/tests, or the HUD selected state is missing.
- Move: Pass if a valid move order produces immediate marker/path/order feedback and the squad visibly moves toward the intended anchor. Fail if valid taps feel ignored, marker position is ambiguous, or path feedback contradicts terrain.
- Attack: Pass if attack targeting is visually distinct from move targeting and damage/objective progress is understandable. Fail if hostile identity depends only on subtle tint or if the player cannot tell whether the attack was accepted.
- Invalid recovery: Pass if each blocked action returns a specific reason and the next valid target/action is obvious. Fail if feedback is silent, generic, off-screen, or hidden behind another panel.
- Result explanation: Pass if victory/failure routing and result meaning are clear without reading design docs. Fail if the result appears before objective completion, appears after squad death, or lacks stars/rewards/city-impact explanation.
- Build rejection: Pass if Build is visibly unavailable or rejects with `Building unlocks in the next mission.` and leaves command state intact. Fail if Build opens placement, silently does nothing, or uses inconsistent copy.
- Assistant behavior: Pass if ARIA recommends one next action at a time, executes only bounded typed intents with permission, and stops immediately on player override. Fail if ARIA uses screen coordinates, hides player agency, repeats completed steps, or has no visible Stop state.

## HCI Risk List

- Major: Automated tests can pass while tap targets, unit silhouettes, and marker contrast remain unreadable to a human at close camera scale.
- Major: The assistant panel controller can pass binding tests while the panel is not mounted into the match HUD, leaving M01 FTUE without an in-play surface.
- Major: Typed intent contracts exist on paper, but missing gameplay wrappers can force future UI/FTUE code into brittle child-path or coordinate automation.
- Major: Runtime hitches and suppressed exceptions in PlayMode can distort perceived command latency and make balance timing data unreliable.
- Major: Enemy/friendly distinction may rely too much on tint unless art-design validates silhouette, marker, health, and target feedback together.
- Minor: Result explanation can be mechanically correct but still fail if stars/rewards/city impact are not visually prioritized.
- Minor: Build rejection copy can pass automated string tests but still be missed if the toast location conflicts with command markers or assistant cards.
- Polish: ARIA copy should stay short, tactical, and player-agency preserving; long explanatory text will reduce command confidence during combat.

## Open Questions

- Which UI route will be the first approved player entry into integrated M01 for manual HCI validation?
- What exact capture matrix is required for mobile before active balance QA: device, resolution, safe-area, and orientation?
- Should PlayMode log noise be converted into a hard QA gate before balance QA, or tracked as a major risk until a reproducible player-visible symptom appears?
- Which owner will implement and expose the gameplay wrappers requested by `CommandIntentExecutor`: `TrySelectRuntimeEntity`, `TryIssueMoveToAnchor`, and `TryIssueAttackTarget`?

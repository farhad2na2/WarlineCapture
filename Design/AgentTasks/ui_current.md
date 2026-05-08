# UI Current Task

Date: 2026-05-08
Status: active
Priority: P1 public M01 launch path blocker

## Assignment

Support the public M01 launch-path fix from the UI side only: route the real menu/campaign/quick paths correctly, keep the WarlineCapture HUD/canvas active, preserve safe-area/HUD composition, and capture the full-screen player-facing view. Gameplay owns the tactical world/map/camera under the HUD. Do not start new UI mockups, broad polish, or unrelated UI work.

## PM Clarification

The route-driven capture and simulated safe-area profile matrix work is complete and accepted for UI evidence. The current UI/GamePlay blocker is public launch-path mismatch:

- Quick Custom / Launch and Test/Custom game mode still show the old 3D prototype.
- Main Menu -> Saga Map / campaign map -> First Contact path also shows the old 3D prototype.

Read:

- `Design/AgentReports/2026-05-08_pm_manual-test-quick-custom-launches-legacy-3d.md`
- `Design/AgentReports/2026-05-08_pm_workflow-public-launch-smoke-gate.md`
- `Design/AgentReports/2026-05-08_pm_manual-test-test-custom-still-legacy-scene.md`

Do not report idle or waiting on QA/HCI for this task. UI owns the UI shell/router/button/canvas/capture-composition side of the launch path and must either fix that side or report the exact Gameplay-owned blocker. UI does not own tactical terrain rendering, world-camera zoom, unit world scale, or map-loader output.

## Context

Read first:

- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentReports/2026-05-07_ui_prefab04-assistant-button-target-lock.md`
- `Design/AgentReports/2026-05-07_pm_ui-prefab04-assistant-button-target-lock-review.md`
- `Design/AgentReports/2026-05-07_ui_prefab04-assistant-button-production.md`
- `Design/AgentReports/2026-05-07_pm_ui-prefab04-assistant-button-production-review.md`
- `Design/AgentReports/2026-05-07_ui_prefab04-assistant-button-production-fix.md`
- `Design/AgentReports/2026-05-07_pm_ui-prefab04-assistant-button-production-fix-review.md`
- `Design/AgentReports/2026-05-07_support-ftue_command-intent-executor.md`
- `Design/AgentReports/2026-05-07_support-ftue_live-assistant-context-provider.md`
- `Design/AgentReports/2026-05-07_support-ftue_integration-support-watch.md`
- `Design/AgentReports/2026-05-07_ui_assistant-runtime-binding.md`
- `Design/AgentReports/2026-05-07_pm_ui-assistant-runtime-binding-review.md`
- `Design/AgentReports/2026-05-07_ui_assistant-runtime-binding-fix.md`
- `Design/AgentReports/2026-05-07_pm_ui-assistant-runtime-binding-fix-review.md`
- `Design/AgentReports/2026-05-07_qa-hci_m01-watcher-smoke-regression.md`
- `Design/AgentReports/2026-05-07_pm_qa-hci-m01-watcher-smoke-regression-review.md`
- `Design/AgentReports/2026-05-07_pm_design-audit-qa-capture-matrix.md`
- `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Landscape_Target.png`
- `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Style_ContactSheet.png`
- `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_CleanLandscape_Notes.md`
- `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Target_State_Manifest.json`
- `Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
- `Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md`
- `Design/WarlineCapture_Art_Asset_Requirements_Register.csv`

The `PREFAB-04_AssistantButton` production prefab is accepted for the M01 HUD visual gate. Support/FTUE has implemented the live `AssistantContextProvider` handoff. The UI assistant runtime-binding fix is accepted for live panel data, typed `Do It`, result-flow `Stop`, visible takeover/control status, and player-input release hook validation.

QA/HCI automated route, assistant runtime, and shell tests are green. The integrated visible route and simulated safe-area matrix have accepted evidence. Gate 4 remains blocked because the public player launch path still enters legacy 3D gameplay.

PM rejected the earlier route-only launch evidence for manual readiness. Keeping `WarlineCaptureRouter` on `WarlineCaptureRoute.Match` and disabling `UI_Canvas` is not sufficient if the player-visible rendered scene is still the old 3D prototype. The next handoff must prove the actual visible scene after launch.

PM also rejected the latest public-launch captures because they show WarlineCapture HUD chrome over a mostly flat brown world with tiny centered gameplay content. That is not an acceptable M01 production visible-scene proof. Hiding the old 3D prototype is only part of the blocker; the public launch must show a readable full gameplay composition aligned with the accepted gameplay camera/reference scale.

## Required Work

- Audit and fix UI-side public launch routing for M01:
  - Main Menu -> Saga Map -> Mission Briefing/Loadout -> Launch.
  - Test / Custom / Quick Custom launch paths.
  - Quick Custom -> Launch only if it is intended to be production, otherwise label it sandbox/legacy and provide a separate production test path.
- Keep UI work scoped to route wiring, canvas/HUD activation, safe-area composition, and full-screen player-facing capture composition.
- Do not try to solve the brown/blank/tiny-world issue by changing tactical world data, map rendering, gameplay camera zoom, terrain loading, unit world scale, or Gameplay runtime systems. If those are the cause, report the exact Gameplay-owned blocker and continue only with UI-owned validation/capture support.
- Coordinate with Gameplay on whether `WarlineCaptureGameLaunchUtility.StartExistingGameplayAndHideRouter`, Mission Briefing/Loadout buttons, route ids, or legacy `UI_Canvas` activation should change for `saga.ch01.m01.first_contact`.
- Coordinate with Gameplay on whether `GameBootstrap.BeginGameplay()` is still launching the old visible 3D scene and therefore cannot be accepted as the production M01 public entry by itself.
- Do not count router state alone as success. The visible rendered scene/camera after launch must be current M01 production direction.
- Do not count a brown/blank world with tiny M01 sprites as success. The public launch screen must show the authored M01 tactical map/terrain, readable unit scale, objective/HUD context, and gameplay camera framing aligned with:
  - `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png`
  - `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/M01_Integrated_1920x1080_01_MatchStart.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_1920x1080_01_MatchStart.png`
- If the UI capture pipeline is rendering only the world camera and missing the actual full-screen player composition, fix the capture pipeline or report that exact capture blocker. Do not hand off camera-only evidence as manual-ready visible gameplay.
- Preserve the accepted route-driven capture/safe-area evidence and assistant runtime binding.
- Do not ask the user for manual HCI/balance testing until public launch smoke proves the intended production slice is visible.
- Preserve the accepted UI assistant runtime binding and prefab visual composition.
- Do not mark asset-register rows complete until PM approves final integrated QA.
- Keep the accepted capture evidence available at `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/`, `Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/`, and `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/`.
- If public launch is blocked by Gameplay/runtime behavior, document the exact blocker and affected lane instead of broad waiting.
- If Gameplay/Input asks how to release assistant ownership, point them to `AssistantRuntimeBinding.NotifyPlayerInputOutsideAssistant()`; do not add gameplay execution logic to the UI prefab.
- Do not add runtime scene searches (`FindObjectOfType`, `FindObjectsOfType`, `FindFirstObjectByType`, `FindAnyObjectByType`, `GameObject.Find`, `Transform.Find` path traversal, `GetComponentInChildren` discovery, or name/tag lookup). Use serialized references, explicit controller references, or typed runtime services.

## Validation Required

- Public launch smoke must be reported:
  - Entry path used.
  - Expected mission id and visual direction.
  - Actual first visible gameplay state.
  - Whether legacy `UI_Canvas`, old 3D gameplay, wrong scene, or wrong mission appears.
  - Whether current M01 2D/isometric sprite-presenter/sprite-renderer visuals are actually visible to the player.
  - Screenshot/capture path when practical.
  - Confirmation that the visible playfield is not a flat brown/blank background and that terrain/camera/unit scale match the accepted gameplay reference captures.
  - Full-screen 16:9 evidence with HUD plus gameplay world, not only a camera render. Add 20:9 evidence if the same path is being claimed ready for QA/HCI.
- Explicit UI/GamePlay ownership split: list what UI changed, what Gameplay evidence/blocker remains for the world/map/camera, and whether the full-screen capture is real player composition or a temporary evidence composition.
- UI's assigned Unity workspace for focused validation/captures is `/Users/farhad/Projects/WarlineCapture-CodexUnity2`. Do not use the Gameplay workspace (`WarlineCapture-CodexUnity`) or QA/HCI workspace (`WarlineCapture-CodexUnity3`) unless PM explicitly reassigns a temporary workspace.
- If Unity batchmode hits `LicenseClient-farhad` reconnect/time-out loops before tests start, rerun the same required command with Codex escalation/out-of-sandbox execution in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`. QA/HCI confirmed this resolves the sandbox licensing issue. Do not switch to Gameplay/QA workspaces to work around licensing.
- Rerun affected focused UI/shell/runtime binding validation if UI launch code or prefabs change.
- Validate no new scene-search warnings or banned runtime lookup calls were introduced in touched UI runtime files.

## Completion Report

Write a report to:

`Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`.

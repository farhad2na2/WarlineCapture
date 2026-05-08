# M01 Critical Path

Date: 2026-05-07
Status: active
Goal: Finish M01 First Contact as the repeatable production pipeline before M02-M05 expansion.

## Rule

No agent should start M02-M05 implementation, broad polish, or optional legacy systems until this file is marked ready to expand by the PM assistant.

Agents may continue only work that closes one of the gates below or removes a named blocker.

## Current Gates

### Gate 1: Gameplay Stability And Direction

Owner: Gameplay
Status: accepted

Accepted evidence:

- `Design/AgentReports/2026-05-07_gameplay_m01-log-performance-fixed-roads.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-log-performance-fixed-roads-review.md`

Accepted result:

- M01 PlayMode log/performance risks are fixed or classified with evidence.
- M01 roads are proven to come from authored tactical metadata, not random/procedural road generation.
- Random city/road generation is disabled, bypassed, or isolated from M01 fixed tactical gameplay.
- Day/night and time-of-day visual mutation are disabled or isolated from M01.
- Legacy 3D `Model` and separate `Destroyed` child prefab dependencies are audited.
- Sprite-atlas migration plan exists for M01 production entities, including baked/contact shadows aligned to fixed tactical-map lighting.
- `Chapter01M01PlayModeValidationTests` is rerun and reported.

Accepted follow-up:

- `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-presenter.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer.md` is accepted only for focused code/test proof, not visual evidence.
- `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer-capture-fix.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-sprite-capture-fix-review.md`

Remaining follow-up:

- Gameplay renderer/capture follow-up is accepted for current review-art evidence. Final atlas packaging, final hostile readability treatment, and `vfx.unit.destroyed.small` remain art/integration follow-ups and must not be marked complete from the current capture alone.

### Gate 2: UI Visual Lock And Assistant Surface

Owner: UI
Status: accepted

Accepted evidence:

- `Design/AgentReports/2026-05-07_ui_prefab04-assistant-button-target-lock.md`
- `Design/AgentReports/2026-05-07_pm_ui-prefab04-assistant-button-target-lock-review.md`
- `Design/AgentReports/2026-05-07_ui_prefab04-assistant-button-production-fix.md`
- `Design/AgentReports/2026-05-07_pm_ui-prefab04-assistant-button-production-fix-review.md`

Accepted result:

- `PREFAB-04_AssistantButton` target lock is a real AAA WarlineCapture-aligned target, not a state board.
- Side-by-side/contact-sheet comparison proves it matches accepted nearby targets.
- Assistant HUD/panel mount remains visible and validated in 16:9 and 20:9.
- Reusable animated `PREFAB-04_AssistantButton` production prefab is accepted for the M01 HUD entry.
- Closed/open assistant captures are readable at 16:9 and 20:9.
- Five assistant button states remain represented with non-color-only cues.
- UI does not invent gameplay execution logic; assistant actions stay behind typed intents.

Remaining UI follow-up:

- Runtime assistant binding is accepted for live panel data, typed `Do It`, result-flow `Stop`, and visible takeover/release validation. Accepted evidence: `Design/AgentReports/2026-05-07_ui_assistant-runtime-binding-fix.md` and `Design/AgentReports/2026-05-07_pm_ui-assistant-runtime-binding-fix-review.md`.
- Asset-register rows should not be marked complete until final integrated QA confirms the route in playable M01.

### Gate 3: Support/FTUE Live Assistant Wiring

Owner: Support/FTUE
Status: accepted

Accepted evidence:

- Assistant `Do It` actions are specified through `CommandIntentExecutor`, not UI clicks or screen coordinates. Accepted evidence: `Design/AgentReports/2026-05-07_support-ftue_command-intent-executor.md`.
- Recommendation state binds to accepted M01 ids: `unit.player.rifle_squad_01`, `tutorial.move_target.cover_01`, and `unit.enemy.patrol_01`.
- Live `AssistantContextProvider` sources typed-command readiness, current selection, anchor availability, enemy visibility, and latest command results from runtime state. Accepted evidence: `Design/AgentReports/2026-05-07_support-ftue_live-assistant-context-provider.md`.
- UI panel binding to the assistant service/executor handoff is accepted. Evidence: `Design/AgentReports/2026-05-07_ui_assistant-runtime-binding-fix.md` and `Design/AgentReports/2026-05-07_pm_ui-assistant-runtime-binding-fix-review.md`.
- FTUE steps use typed ids and runtime anchors.
- Commander identity chooser gap remains tracked separately and must not block M01 tactical validation unless the user makes it required for first launch.

### Gate 4: QA/HCI M01 Smoke And Readability

Owner: QA/HCI
Status: needs fixes after automated QA/HCI smoke

Required before pass:

- At least one public player launch path reaches the intended M01 production slice, not the legacy 3D prototype or an editor-only route harness. Required paths to check before manual HCI/balance feedback:
  - `Main Menu -> Saga Map -> Mission Briefing/Loadout -> Launch`.
  - Any direct/quick/test launch path the team asks the user to use.
- The launch-path report must state mission id, expected visual direction, actual first visible gameplay state, whether legacy `UI_Canvas`/old 3D gameplay appeared, and screenshot/capture evidence when practical.
- M01 select, move, attack, objective, assistant recommendation, invalid-command recovery, and result flow are manually checked.
- Performance notes include frame drops, visible hitches, freezes, input stalls, log spam, and memory/leak warnings.
- Visual readability notes include unit grounding, sprite shadows, target markers, HUD occlusion, and WarlineCapture style alignment.
- Balance conclusions remain blocked until gameplay and UI are stable enough for meaningful runs.
- Use `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png` as current review-art evidence for grounding, scale, and readability checks. Do not treat it as final art approval.
- Automated QA/HCI smoke is green but not sufficient for Gate 4. Accepted review pending/follow-up: `Design/AgentReports/2026-05-07_qa-hci_m01-watcher-smoke-regression.md`, `Design/AgentReports/2026-05-07_pm_qa-hci-m01-watcher-smoke-regression-review.md`.
- Gameplay log-health classification is accepted for focused editor/non-headless evidence. Accepted evidence: `Design/AgentReports/2026-05-07_gameplay_m01-log-health-classification.md`, `Design/AgentReports/2026-05-07_pm_gameplay-m01-log-health-validation-review.md`.
- UI integrated capture matrix is accepted for QA/HCI evidence. Accepted evidence: `Design/AgentReports/2026-05-07_ui_m01-integrated-capture-matrix.md`, `Design/AgentReports/2026-05-07_pm_ui-m01-integrated-capture-matrix-review.md`, and `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/`.
- QA/HCI integrated readiness review is complete but needs fixes because player-route and safe-area/device evidence are still missing. Evidence: `Design/AgentReports/2026-05-07_qa-hci_m01-gate4-integrated-readiness.md`, `Design/AgentReports/2026-05-07_pm_qa-hci-m01-gate4-integrated-readiness-review.md`.
- QA/HCI player-route automation passed, but Gate 4 remains blocked because route-driven screenshots and safe-area/device evidence are missing. Evidence: `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-pass.md`, `Design/AgentReports/2026-05-08_pm_qa-hci-m01-player-route-safe-area-pass-review.md`.
- Remaining Gate 4 blockers are public M01 production launch-path wiring, UI route-driven safe-area profile closure, reason-code Unity validation, marker/VFX readiness, and QA/HCI rerun evidence proving select, move, attack, invalid recovery, assistant ownership/Stop, result flow, performance/freeze stability, and final log-health status.

## Ready To Expand Criteria

M01 can expand to M02 only when:

- Gate 1 is accepted by PM.
- Gate 2 is accepted by PM.
- Gate 3 is accepted or explicitly deferred by PM.
- Gate 4 has at least one QA/HCI smoke pass with no blocker findings.
- Project state dashboard is updated if the accepted gates materially change completion.

## PM Review Response

When a gate report lands, PM should respond with:

```text
Gate:
Status: accepted / needs fixes / blocked
Reason:
Validation accepted:
Validation still needed:
Cross-lane notices:
Next gate/task:
```

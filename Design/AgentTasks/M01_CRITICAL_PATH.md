# M01 Critical Path

Date: 2026-05-07
Status: active
Goal: Finish M01 First Contact as the repeatable production pipeline before M02-M05 expansion.

## Rule

No agent should start M02-M05 implementation, broad polish, or optional legacy systems until this file is marked ready to expand by the PM assistant.

Agents may continue only work that closes one of the gates below or removes a named blocker.

## M01 Product Scope Lock

M01 First Contact is an infantry-only first playable mission.

Allowed runtime combat entities:

- Player: one controllable infantry/rifle squad type, `unit.player.rifle_squad_01`.
- Enemy: one hostile infantry/patrol type, `unit.enemy.patrol_01`.
- Objective: destroy or neutralize the hostile patrol and reach the result flow.

Not allowed before M02:

- player-controlled vehicles
- vehicle production
- transport mechanics
- base/build mechanics
- additional player unit types
- broad combat variety or optional legacy systems

Decorative vehicles are allowed only if they are non-controllable, non-combat, do not confuse the first task, and do not block pathing/readability.

## Golden Playthrough Gate

Before Gate 4 can pass, one public M01 golden playthrough must be accepted by PM:

`Main Menu -> Saga Map -> M01 First Contact -> Mission Briefing/Loadout -> Deploy -> select rifle squad -> move to tutorial cover -> attack hostile patrol -> enemy destroyed/neutralized -> objective/result popup`.

This must be proven from the public player path, not an editor-only scene, route harness, static screenshot, or isolated test fixture.

Required pass criteria:

- player immediately understands which soldiers are theirs
- player rifle squad reads as four distinct soldiers under one controllable squad entity, not a single flat group sprite
- player can select the rifle squad or selection state is clearly presented
- selected state is visible in world and HUD after selection
- player can issue the first move order before lethal enemy fire
- movement uses tactical walkable/pathing metadata and rejects blocked/unreachable cells
- movement drives move animation and returns to idle after arrival
- attack command is readable and drives attack/combat animation or a clearly documented temporary visual state
- enemy projectile/impact VFX is tactical-scale and AAA-readable, not oversized arcade bullets
- enemy death/destroyed/neutralized state is visible and completes the objective
- no player-controlled vehicles or extra player unit types appear
- no legacy 3D combat units or design-target SpriteRenderer proxies appear in the public path
- UI/assistant supports the flow and does not block player control
- no blocker exceptions, freezes, severe input stalls, or unclassified log spam appear

Any report that only proves captures, safe-area profiles, route wiring, or isolated tests without this playable path is not sufficient for Gate 4.

## Current Gates

### Gate 1: Gameplay Stability And Direction

Owner: Gameplay
Status: needs fixes after user playtest

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

Current blockers:

- User playtest found that the playable M01 opens with hostile fire killing player units before the player can understand they have soldiers or issue the first move order.
- User also rejected visible SpriteRenderer-style unit presentation as not matching the goal. M01 units must be ECS runtime entities with animated sprite-atlas presentation, pathing-aware movement, and correct idle/move/attack/death visual states.
- User clarified the intended migration: existing unit/building prefabs and configs should remain the data/authoring source, and the useful ECS animation-state logic should be retained, but old visible child `Model` presentation and its per-model ECS animation output must be replaced with a new Gameplay-owned ECS sprite-atlas animator. Old child `Destroyed` dependencies must be deleted or removed from M01 runtime use because destroyed/death belongs in the same atlas visual-state machine.
- Earlier SpriteRenderer capture/renderer reports are retained as implementation evidence only. They are not final runtime presentation acceptance and must not be used to justify M02 expansion.
- Gameplay owns the active fix/proof in `Design/AgentTasks/gameplay_current.md`.
- Gameplay must prove the golden playthrough before PM can restore Gate 1 to accepted.

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
Status: blocked on Gameplay playable-loop fix

Required before pass:

- Golden playthrough gate above is accepted by PM.
- At least one public player launch path reaches the intended M01 production slice, not the legacy 3D prototype or an editor-only route harness. Required paths to check before manual HCI/balance feedback:
  - `Main Menu -> Saga Map -> Mission Briefing/Loadout -> Launch`.
  - Any direct/quick/test launch path the team asks the user to use.
- The launch-path report must state mission id, expected visual direction, actual first visible gameplay state, whether legacy `UI_Canvas`/old 3D gameplay appeared, and screenshot/capture evidence when practical.
- M01 select, move, attack, objective, assistant recommendation, invalid-command recovery, and result flow are manually checked.
- Performance notes include frame drops, visible hitches, freezes, input stalls, log spam, and memory/leak warnings.
- Visual readability notes include unit grounding, sprite shadows, target markers, HUD occlusion, and WarlineCapture style alignment.
- Balance conclusions remain blocked until gameplay and UI are stable enough for meaningful runs.
- `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png` may be used only as historical review-art reference for grounding/scale. It is not Gate 4 runtime readiness evidence.
- Automated QA/HCI smoke is green but not sufficient for Gate 4. Accepted review pending/follow-up: `Design/AgentReports/2026-05-07_qa-hci_m01-watcher-smoke-regression.md`, `Design/AgentReports/2026-05-07_pm_qa-hci-m01-watcher-smoke-regression-review.md`.
- Gameplay log-health classification is accepted for focused editor/non-headless evidence. Accepted evidence: `Design/AgentReports/2026-05-07_gameplay_m01-log-health-classification.md`, `Design/AgentReports/2026-05-07_pm_gameplay-m01-log-health-validation-review.md`.
- UI integrated capture matrix is accepted for QA/HCI evidence. Accepted evidence: `Design/AgentReports/2026-05-07_ui_m01-integrated-capture-matrix.md`, `Design/AgentReports/2026-05-07_pm_ui-m01-integrated-capture-matrix-review.md`, and `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/`.
- QA/HCI integrated readiness and earlier player-route automation reports are complete but not sufficient for Gate 4 after the public-launch fixes. Evidence: `Design/AgentReports/2026-05-07_qa-hci_m01-gate4-integrated-readiness.md`, `Design/AgentReports/2026-05-07_pm_qa-hci-m01-gate4-integrated-readiness-review.md`, `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-pass.md`, and `Design/AgentReports/2026-05-08_pm_qa-hci-m01-player-route-safe-area-pass-review.md`.
- Public M01 production launch-path wiring is accepted from assigned lane workspaces. Accepted evidence: `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`, `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`, and `Design/AgentReports/2026-05-08_pm_public-launch-handoff-workspace-review.md`.
- Current primary blocker is not more screenshot/capture proof. Current primary blocker is the playable M01 loop:
  - player survives long enough to see/select the squad
  - player squad reads as four distinct soldiers under one squad entity
  - selection state is clearly visible in world and HUD
  - player can order movement to `tutorial.move_target.cover_01`
  - movement follows tactical metadata/pathing
  - visible units are ECS runtime entities with animated atlas-backed state
  - visible M01 infantry presentation replaces the old prefab `Model` path with ECS-owned animated atlas rendering
  - M01 infantry animation is driven by a new ECS sprite-atlas animator, not by the old `MaterialAnimationIndex`/model visual-root animation output
  - M01 infantry does not use separate child `Destroyed` visuals; death/destroyed resolves through the atlas visual state
  - enemy patrol does not wipe the squad before the teaching window
  - attack/objective/result flow remains reachable
  - projectile/impact VFX scale and style are acceptable for AAA tactical readability
- infantry-only M01 scope is preserved: one player rifle squad type, one enemy patrol type, no player vehicles, no vehicle/build/transport mechanics
- QA/HCI should not produce final Gate 4 acceptance until Gameplay reports `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md` and PM accepts it.

## Ready To Expand Criteria

M01 can expand to M02 only when:

- Gate 1 is accepted by PM.
- Gate 2 is accepted by PM.
- Gate 3 is accepted or explicitly deferred by PM.
- Public M01 playable loop is accepted by PM: ECS animated units replacing visible old `Model` presentation and old per-model animation output, no separate `Destroyed` child dependency, first-control survival window, pathing-aware movement, attack/objective/result reachable.
- M01 visual feedback is accepted by PM/user or QA/HCI: four-soldier squad readability, selected state, move/attack markers, projectile/impact scale, and destroyed/death feedback.
- Infantry-only scope is preserved: one player rifle squad, one enemy patrol, no player vehicles or additional player unit types.
- Golden playthrough is accepted from public launch through result popup.
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

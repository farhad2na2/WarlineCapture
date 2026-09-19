# Watch ARIA play — visible touch-only demonstrations

Date: 2026-09-18. Status: **direction accepted for documentation; implementation planned, not started**. The owner requested this concept, its mockup and a detailed implementation plan covering all modes and matches. The [implementation plan](PLAN.md) is the entry point for delivery, architecture, coverage and acceptance. No gameplay code is changed by this planning package.

## Product recommendation

Add **Watch ARIA play** to the existing ARIA panel. After one clear confirmation, ARIA plays the current supported match through visible touch gestures, explains useful decisions, and returns control instantly on Stop or manual intervention. This is continuous, responsive match play rather than the former hidden one-step Do It action.

The same touch-based player can support developer gameplay QA and a player-facing teaching feature. Certification is by mission/ruleset and supported capabilities: do not promise an arbitrary new mission or every damaged starting position can be won. The agent should adapt to normal variations, loss and interruption instead of replaying a fixed sequence of coordinates.

## Player flow and control

1. **Idle:** full-width “Watch ARIA play” action in the ARIA panel, alongside the existing field help. Keep the mission objective readable.
2. **Confirm:** “Let ARIA play this match? Watch her touches and take over anytime. Your units and match resources are used normally.” Buttons: Cancel / Start. Scope is this current match, not an unlimited session or authority over the account.
3. **Active:** “ARIA is playing,” a short current intention, a useful next-action or wait status, and a large full-width **Stop ARIA** button. It stays reachable when the agent opens Build, selection commands or other gameplay panels. If the main panel collapses, retain a pinned stop control within the safe area.
4. **Stop:** immediate handback without another confirmation. No future synthetic touch is allowed. Already accepted movement/combat/production orders continue normally; stopping ARIA does not undo actions or stop all soldiers.
5. **Manual intervention:** the first genuine gameplay touch ends ARIA control and is consumed as a handover gesture, preventing an accidental world command. Clearly show “You are in control”; subsequent input works normally. Camera input also hands control back initially so two controllers never fight over the view.
6. **Terminal state:** win, loss, unsupported state, repeated lack of progress, backgrounding or scene exit ends automation, cancels contacts, and clears queued coaching audio. Never auto-retry, auto-restart or begin another match. A fresh start requires confirmation again.

Only the real player can authorize Start. ARIA must never click its own Start/Resume controls, approve payments or leave the match to change permanent progression. Operations support initially means the tactical match launched from Operations; district management is separate scope.

## Non-negotiable touch contract

All gameplay changes caused by ARIA go through the **same screen-touch input route as a player**: press, hold, move, release, cancel and supported multi-touch gestures. Pan through dragging/minimap interaction; zoom through the game's actual touch controls. The executor accepts screen positions and touch phases, not unit/entity commands.

Forbidden shortcuts include `Button.onClick.Invoke`, direct `ExecuteEvents` calls aimed at a button, selecting an entity by ID, enqueueing Move/Attack/build commands, direct camera focus, teleporting a preview, spawning units, changing money/health, or advancing tutorial/completion facts. A holographic animation around any such shortcut does not satisfy this feature.

The touch stream must produce the actual UI hit-test, gesture recognition, world picking and normal validation. When a target is occluded, disabled, offscreen or moving, ARIA has to resolve it through normal navigation or stop with a useful reason. No direct-command fallback for an inconvenient UI.

The input module may internally dispatch UI events as normal; the restriction is on the ARIA actuator bypassing the input route. Existing Editor probes can remain useful for other tests, but are not evidence of this strict contract.

## What ARIA may observe

Use a read-only **player-visible observation model**: currently displayed objective/instruction text, open menu controls and their screen rectangles, visible resource/queue/timer/status values, selected-unit details, currently observable battlefield objects and permitted minimap contacts. Include age/version and occlusion so decisions cannot reuse stale coordinates.

A semantic accessibility mirror of the rendered UI is recommended over continuously guessing every word through OCR. It may carry a language-neutral public objective identity alongside the exact displayed English/Farsi instruction. It must expose the same information a player can obtain at that moment, not hidden mission triggers, unrevealed enemies, secret spawn times, unreachable target entities or underlying completion flags. Unit health/resources may only be read at the precision exposed to the player.

Remember previously observed information with time and uncertainty. To read a closed panel, ARIA opens it with a touch. To inspect a distant location, she pans or touches a visible map/focus control. The holographic hand and its own coaching caption are excluded from perception. QA may compare hidden ground truth separately, but that diagnostic channel cannot feed decisions.

Campaign's public mission goals and Skirmish's rules use a shared goal vocabulary—select, move, recruit, build, defend, attack, board, deliver, hold an area—published consistently with the player instructions. Legacy recommendation buffers contain privileged targets and therefore require filtering; they are not automatically safe observation sources.

## Planner and explanation

Recommended initial architecture:

`Player-visible observation → goal interpretation → local tactical plan → gesture proposal → ownership/validity check → touch stream + hologram → observe outcome → replan`

A local goal-aware planner and reusable interaction skills are the first product implementation. This avoids depending on a remote model for every tap, and makes response timing, offline play and regression tests manageable. It is not unrestricted natural-language understanding: new goal mechanics require corresponding skills and certification. Optional future language/vision models can propose plans or richer explanations, but can only output bounded gesture proposals through the same observation and execution limits.

Skills are complete visible interactions: select a squad card, open Build, find an item, choose it, drag the footprint, rotate, confirm, inspect the result. They are not gameplay command APIs. Plans use current observations, public rules and validated strategies rather than hardcoded world coordinates or outcome manipulation.

After each meaningful gesture, verify a visible result: selection changed, menu opened, preview moved, queue increased, order accepted, or objective counter progressed. For an expected wait, watch the actual visible timer/condition and show a concise reason. Bound retries; a repeated failure yields control with “I couldn't reach that control” or the relevant reason instead of tapping endlessly.

Explanations describe tactical intent, not every mechanical click: “I'll keep the rifle squad on this road while the convoy approaches,” or “The tank needs anti-armor support.” Explain the intended decision before acting; distinguish acceptance from success afterward. If a plan fails, acknowledge it and adapt. Never claim success from a button press alone.

## Human pacing and holographic finger

Initial pacing proposals for playtesting: around 0.6–1.2 seconds between simple menu taps, 2–4 seconds to introduce a tactical decision, with shorter bounded reactions under immediate pressure. Gestures use real durations; no artificial random mistakes or shaking hand. The battle clock remains at the player's chosen visible speed. Do not secretly pause the world while ARIA thinks or narrates. Long explanations must not delay urgent actions; shorten or defer them.

Use a small translucent cyan hand with an extended index finger, faceted/wireframe detail matching ARIA's portrait, and a contact ring at the actual input coordinate. The fingertip remains exact while the palm rotates/repositions near screen edges. The overlay never captures input or covers essential text. Reduced-motion presentation may shorten travel and remove trails while retaining visible contact.

| Gesture | Visible treatment |
|---|---|
| Aim | Hand travels to the next target; no input press yet |
| Tap | Brief downward motion and one contact pulse at press/release |
| Hold | Finger remains pressed with a duration ring |
| Drag / selection | Pressed fingertip moves with a short trail; the real selection rectangle/preview responds |
| Pinch | Two contact indicators at actual touch points, with restrained hand presentation |
| Wait | Hand withdraws; ARIA shows what condition she is waiting for |
| Stop | Gesture cancels, hand withdraws and ownership changes visibly |

Drive this presentation from the **same timestamped gesture data** used for input. Never let an independent decorative finger drift away from where the engine receives the touch. Yellow guides remain manual-learning cues; cyan identifies ARIA's actual input. Avoid simultaneous competing guides and repeated flashing.

The image-generation board is a static visual proposal for these states, not a running animation or a new battlefield art direction. English copy is for initial review; implementation needs conversational Farsi, RTL layout and matching narration as well.

Review the [visual concept board](Mockups/aria-watch-play-concept-v01.png). The [generation prompts and provenance](Mockups/PROMPTS.md) preserve the exact built-in image-generation inputs and selected revision.

## Instant Stop engineering requirement

Stopping must not wait for the next planner decision, narration clip, animation or network result. A small local ownership/cancellation controller runs before ARIA event submission, handles genuine input first, invalidates the session generation and discards pending gesture work. Submit input incrementally, never a long future queue that continues after cancellation. Delayed planner responses from the old generation are ignored.

Cancel active touches with a real cancellation path that the existing UI/drag/build handlers respect. An ordinary release can accidentally confirm a tap or placement, so cancellation must be tested explicitly. Only clear ARIA contacts, not physical user contacts. Next input-processing update is the architectural stop target; measure visible handback latency on supported devices rather than claiming zero wall-clock latency during a main-thread stall.

Physical and synthetic input need distinct ownership without duplicate processing. Audit `Touchscreen.current` and primary-pointer assumptions before adding a virtual device: a synthetic device must not hide a physical Stop touch or misclassify its own touch as a user override. Overlay controls need independent real-user access even when the gameplay UI is modal.

## Existing project baseline

- `AssistantCommandIntentSystem.cs` directly routes selection, move/attack and camera intents. It is unsuitable as the actuator for this feature.
- `AssistantControlOwnerSystem.cs` provides ownership concepts, but the old takeover is bounded to three actions/30 seconds and detects overrides through shared selection input. It is not a whole-match touch player or a sufficient instant-stop interlock.
- `MatchHudAssistantUiSystemHelper.Commands.cs` contains direct button invocation and mission-specific Do It behavior. Reuse appropriate panel styling, not this execution path.
- `SkirmishGameplayProbe.cs` sends direct EventSystem events for button tests and direct tactical requests for some world commands. Those helpers must not be relabeled as touch-only QA.
- `MissionGuidanceStepConfig.cs` and mission projections offer useful authored goal vocabulary, but hidden facts and pre-resolved entity targets must remain outside player-visible observations.
- The installed Input System is 1.19.0. Its bundled `Events.md` documents `TouchState` press/move/release queueing. Public [Touchscreen documentation](https://docs.unity.cn/Packages/com.unity.inputsystem%401.13/api/UnityEngine.InputSystem.Touchscreen.html) describes the underlying event model; validate against the installed version and the actual game input adapter. [Unity's UI input documentation](https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/ui-input-module-reference.md) also identifies multiple-pointer handling as a configuration concern.

## Proposed delivery and acceptance

1. **Touch player shell:** confirm/start/stop/real-touch handover, visible tap/hold/drag/pinch, input ownership and action trace. Test Stop during every gesture phase, popups and layout changes before adding autonomous strategy.
2. **M1 end to end:** read the visible instruction, select and command through real controls, narrate decisions and finish or hand back honestly. Include different camera positions, screen aspects/locales and mid-mission starts.
3. **M2–M5 capabilities:** production/delivery, valid construction, defense/waits, scan, transport/boarding and breach. Use the same reusable skills and match goals. Each mission requires real gesture evidence and a complete normal-speed run.
4. **Existing small Skirmish:** independent economy, recruitment, defense/flank choices, counterplay and victory/loss handling. Use several seeds and starting states; publish observed success rates, failure reasons and touch pace rather than a guaranteed-win claim.
5. **Operations tactical matches and expanded Skirmish:** add supported goal/roster adapters as those modes become ready. Do not expose a working-looking button for unsupported mechanics. Generalization beyond the certified set is future work.

For every meaningful game action, record observation version, intended target, gesture, actual hit target, visible result and ownership token. No action without a traceable synthetic touch. Protect the actuator boundary through restricted dependencies and tests; trace generation is an audit tool, not permission for secret commands. Compare identical human/ARIA gestures through the same input handlers.

QA runs must disclose injected scenario setup, skipped stories or accelerated time separately. A broken/occluded UI is a defect to report and fix; ARIA cannot bypass it to manufacture a pass. This driver can strengthen QA substantially, but it does not replace visual/audio review, independent player judgment or phone performance testing.

Success criteria include one-start confirmation, no confirmation to stop, no action after cancellation, no accidental final release action, no hidden-information advantage, valid camera/gesture parity, readable hands without hit blocking, correct EN/FA voice lifecycle and recovery from changed layouts or rejected actions.

## Player access and future monetization

First prove that users learn by watching and can repeat the action themselves. Keep a basic teaching demonstration available; consider paid extended coaching, advanced strategy lessons, or replay analysis later. Build the on-device version without per-tap cloud dependency first.

Track an assisted-run flag for honest results and any future leaderboard/reward policy. Do not silently alter current campaign rewards; decide and disclose the policy before a paid or competitive launch. Avoid selling guaranteed wins or unattended reward farming. Price/entitlements/payment implementation are out of scope for this proposal; Stop and taking back control always remain immediate.

The owner accepted documenting this direction and requested an all-mode implementation plan. Follow [PLAN.md](PLAN.md): audit all routes and prove touch/cancellation first, use M1 as the first end-to-end validation, then complete M2–M5, current Skirmish and every playable Operations tactical match. M1 alone does not complete the feature. The [delivery checklist](DELIVERY.md) and [coverage ledger](COVERAGE.md) keep unfinished mode support visible; the larger Skirmish expansion adds its own new-mechanic coverage as it is implemented.

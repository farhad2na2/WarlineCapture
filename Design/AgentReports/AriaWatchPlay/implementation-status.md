## Current Skirmish baseline — 2026-09-20

Both selectable Base Assault scenarios now have autonomous Editor victories in English and Farsi: S1 EN 5:38 / FA 5:20; S2 EN 6:17 / FA 6:22. Default seed 104729, normal rules, isolated saves, approved API start and shipping touch-only gameplay. All four results saved and cancelled terminal input. The final Farsi S1 result was state-verified; the other three victory screens were also visually reviewed.

See [delivery and limitations](skirmish-two-scenario-delivery.md) and [per-run evidence](skirmish-two-scenario-results.json). Wider seed/device/player-study certification remains open; the development preview gate is retained. The older baseline JSON and historical notes below describe prior revisions.

# Watch ARIA Play — implementation checkpoint

Date: 2026-09-19. Status: **guided M1–M5 baseline wins verified in both languages; broader release certification remains open**.

## Implemented foundation

- Managed virtual touchscreen actuator; the actuator has no gameplay, entity, camera or Button references. It submits touch state events through Unity Input System.
- Press, hold and drag trajectories with accepted-contact presentation data; one gesture at a time.
- Cancellation removes the synthetic touchscreen to avoid the input module treating a cancelled touch as a click release. World selection and placement explicitly clear cancelled input.
- Player mouse, keyboard or physical touchscreen contact interrupts the demonstration. The handback press/release is consumed to prevent accidental orders.
- ECS decision/session state, stable-target aiming delay, verification delay, bounded retries, and bounded waits.
- Public observations from already displayed tutorial guidance and reachable UI controls; no hidden entity target supplied to the actuator.
- Initial English/Farsi Watch/confirmation/Stop controls, procedural cyan finger, separate Watch layout row, and persistent Stop overlay.
- Isolated Editor validation profile with restore on returning to Edit mode.

## Actual evidence

Connected normal WarlineCapture Editor, Unity 6000.5.2f1. No batchmode or direct Editor executable used.

`Game.Editor.AriaTouchInputValidation.Run()` returned:

```
[AriaTouchInputValidation] result=Passed cases=12
```

Checks cover one pointer click per tap; accepted contact position; cancellation during contact and before queued contact delivery; no delayed release; mouse and physical touchscreen interruption/consumption; one active gesture; accepted drag movement; cancelled drag does not click. This is an isolated Edit-mode fixture using the real InputSystemUIInputModule and a test raycast surface, not a rendered mission test. Navigation actions are disabled in the pointer-only fixture because their performed-this-frame state can survive multiple manual input updates within a single Editor frame. No equivalent navigation override was added to the game.

`Game.Editor.AriaPlayDecisionValidation.Run()` returned:

```
[AriaPlayDecisionValidation] result=Passed cases=10
```

Checks cover explicit start requirement, aiming delay, retargeting a moving control, stable-target execution, no overlapping gestures, bounded retries, waiting without inputs, wait timeout, and result termination.

### Live campaign runs after unlock

The initial M1 setup accidentally combined incomplete onboarding with direct test deployment. That evidence is discarded. The fixture now uses a separate profile marked onboarding-complete before deployment; native story controls then worked normally.

- **M1 English initial win:** after a runtime correction to inherited disabled CanvasGroups on the new Watch controls, ARIA won through six simulated touches after confirmation. Result displayed Victory, three stars, 03:15, zero squad losses, enemies 3/3. No manual gameplay input after Start. The control fix is now in source, but a fresh run is required; terminal cleanup was also corrected afterward.
- **M2 English autonomous completion:** fresh compiled controls; nine simulated touches opened Build, selected/placed Barracks, and recruited the required squad. No manual gameplay input after Start. The mission reached its ending comic. Session was Manual, pressed=0, gestureRequested=0, StopReason=5 (terminal outcome). This verified the fix that preserves the session when the same match rebuilds its HUD during Build navigation.
- **M3:** opening story completed and the initial briefing displayed. A manual Build open/close occurred before Start; no production/placement action. The Mac locked again at the Watch confirmation, so no autonomous M3 evidence is claimed. Editor restore to Edit mode was requested and acknowledged.

Additional source fixes: Show Me remains available to the observation adapter when a visible guide is obstructed; replacement controls at identical coordinates require a new approach delay. The tenth decision fixture case passed after the next unlock: `[AriaPlayDecisionValidation] result=Passed cases=10`.

A broad connected architecture reflection run timed out with no result file. It is not a pass. The new input SystemBase is explicitly classified as a managed virtual-device boundary in the architecture guardrail; the classification does not excuse gameplay policy or direct orders.

## Remaining release gates (updated after the run log below)

- Basic live handback/restart passed in M2; the full interruption matrix (including placement drag, pause/focus, every mission and device timing) remains open. M1–M5 have clean completion evidence in both languages.
- Broader varied-start, loss, tutorial-disabled and interrupted-match recovery coverage from the acceptance plan remains open. The current policy follows visible guided actions, not a general tactical planner.
- Validate final permanent-left minimap spacing across selection/transport, Build, aspect ratios and both languages.
- Full repository architecture gate still has unrelated existing failures; new ARIA boundary registrations are recorded precisely.
- Android touch/performance and unfamiliar-player teaching review remain separate, unpassed gates. The procedural hand is initial art, not mockup-fidelity approval.

No commit/push was requested for this implementation turn. No new audio generation or external AI service was used.

### M3 resumed run

Clean M3 English run after native intro skip, confirmed Watch then no manual gameplay input. ARIA used 11 simulated touches, placed the road barrier, selected/moved defenders, issued Hold, waited through both convoys and reached Victory: 3/3 stars, 01:44, rifles lost 0, hostiles stopped 7/7, civilians lost 0, forward post undamaged. Session stopped with terminal reason 5 and no pending/pressed gesture.

The completed broad architecture audit file became available after unlock. It contains failures, including existing Skirmish/production-growth debt and the new touch helper requiring a precise source-growth registration. No full architecture pass is claimed.

User permitted moving the minimap left when space is needed. Implemented temporary left docking while the Watch confirmation is open, restoring the normal dock afterward; visual validation pending in M4.

### M4 first run and correction

The first run stopped making progress at the command-wheel Board wedge. Its rectangular center lies in the wheel hub, so the safe hit-test correctly refused to press it. Added `V3RadialWedgeGraphic.GuidanceTouchPoint`, computed from the rendered sector's middle angle and radius, and used it in the observation adapter. The same event-system hit test remains required. A connected geometry check passed all four quadrant orientations. Fresh M4 run in progress.

M4 confirmation screenshot verified temporary left minimap docking: no overlap with Start or Cancel; normal right docking restored after Start.

### M4 second run and correction

All four specialists boarded the APC and then the helicopter using simulated touches. Step 10 displayed “Wait … no click is needed” but its guide was still a crosshair. The actuator retried the apparently actionable marker and safely handed back after three attempts. Changed the existing tutorial presentation to a defensive waiting-area ring when the aircraft is already at the landing area; no hidden mission state was added to ARIA's planner. A new full run is required and underway.

The floating Stop copy now appears only when the ordinary ARIA Stop button is occluded/inaccessible, preventing the duplicate from covering M4's mission counters. Physical intervention still cancels independently of either button.

### M4 English completion

Fresh full run with the wedge and wait-ring fixes completed the rescue in 25 simulated touches after Start, with no manual gameplay intervention. APC manifest reached 4/4, all four specialists transferred to the helicopter, the 20-second safety timer completed without synthetic taps, and ARIA selected/moved the helicopter to exit. Ending comic confirmed four passengers accounted for. Public campaign outcome was Victory (`Outcome=1`), phase terminal; session Manual, StopReason=5, pressed=0 and gestureRequested=0. M5 next.

### M5 English completion

Fresh full run: 12 simulated touches after confirmation, no manual gameplay input. Gate destroyed; rifles entered compound; radar destroyed; archive 20-second recovery completed without synthetic taps during the wait. Ending comic confirmed archive recovered. Session ended automatically with reason 5, pressed=0, gestureRequested=0. M1 clean rerun and Farsi coverage remain.

### M1 English clean rerun

Fresh source-only run, no runtime UI corrections. Six simulated touches after Start; Victory, 3/3 stars, 01:28, squad losses 0, enemies 3/3, civilians 0. Terminal stop reason 5, no pressed/pending gesture. All five English missions have now completed through the shipping touch driver. English confirmation copy was visibly truncated; enabled bounded wrapping on new Watch controls before Farsi validation.

Re-ran isolated input and decision fixtures after the mission fixes: 12 input cases and 10 decision cases passed. Four wedge-orientation checks also passed. This does not replace physical Android testing.

### Farsi runs and focused architecture review

M1 Farsi completed with six touches, three stars, 01:59, no squad/civilian losses and enemies 3/3. The fixture initially used unsupported locale `fa`; before Start it was corrected to the catalog code `fa-IR`. All gameplay after Start was Farsi and touch-only. M2 launched directly with `fa-IR`, completed with nine touches and reached the localized ending comic. Both sessions ended with reason 5, no pressed contact or pending gesture.

The focused architecture results are in `/private/tmp/aria-focused-architecture.txt`: exact helper/source-growth registrations no longer report the ARIA touch helper or its two modified cancellation owners. Existing unrelated growth/classification failures remain; the managed-boundary disjoint/concrete check passed. The connected call timed out while the synchronous audit ran; the completed output file is evidence of individual results, not a successful overall gate.

M3 Farsi completed both convoys with 11 simulated touches and a public Victory outcome. ARIA waited during defense, without further taps; terminal stop reason 5, no held/pending contact.

The owner then requested permanent left minimap docking. It now stays above the squad/placement controls on the left, and the ARIA rail uses the full right-side height down to the command dock. This supersedes the temporary confirmation-only docking described above.

M4 Farsi completed with 25 touches and Victory. Both manifests reached 4/4, the safety wait completed without input, and the helicopter departed. Terminal stop reason 5, no pending/pressed gesture. Permanent left map docking revealed selection-panel overlap; the map now shifts horizontally beside an expanded selection panel while staying on the left side, preserving the gap above squad/placement controls.

### M5 Farsi completion and baseline summary

M5 Farsi completed with 12 touches and Victory. The gate and radar were destroyed, the archive recovered after its normal wait, and localized debrief played. Session ended automatically with reason 5, no pending/pressed contact. No manual gameplay input after Start. The left minimap remained separate from the selected-unit panel after the overlap correction.

| Mission | English touches | Farsi touches | Observed outcome |
|---|---:|---:|---|
| M1 | 6 | 6 | Victory; three stars in both |
| M2 | 9 | 9 | Completed recruitment and ending comic |
| M3 | 11 | 11 | Both convoys stopped; Victory |
| M4 | 25 | 25 | Four specialists extracted; Victory |
| M5 | 12 | 12 | Archive recovered; Victory |

These are clean baseline gameplay runs after Watch confirmation, with normal simulation speed and a separate validation profile. Story skips and test mission deployment happened before Start. They do not establish all varied-start/device/player-review gates in the design acceptance plan. Input (12 cases) and decision (10 cases) fixtures passed again after final minimap changes.

### Test isolation correction

A later live handback setup found the Watch button nonresponsive after running the isolated input fixture. The fixture had manually enabled its sibling EventSystem through SendMessage but never explicitly disabled it on Edit-mode destruction, leaving a destroyed registered EventSystem. Replaced broadcast initialization with targeted lifecycle calls and balanced both module/EventSystem cleanup. Added an exact registration-restoration assertion; the corrected 12-case fixture passes. The earlier live campaign baseline runs used functioning EventSystems and reached their recorded outcomes; the dead-control handback setup is not counted as a gameplay pass.

### Live handback and wide-layout evidence

After the corrected fixture, Watch responded normally. At 2400×1080 (20:9), English confirmation wrapped completely and the map stayed on the left without covering the selected Barracks. The Editor Game view initially cropped its preview at the previous scale; expanding the Game view showed the full render, so this was not a game safe-area failure.

First physical Stop attempt arrived after ARIA had placed the Barracks (5 accepted gestures), before recruitment. Session became Manual, reason 6 (physical interruption), pending/pressed=0 and virtual-device count=0. The state remained stopped during inspection. The next manual Build click opened production correctly. Restart required a fresh confirmation; another physical intervention stopped after the Soldiers tab gesture (1 accepted gesture), leaving 30 materials and no production queued. This is handback/restart evidence, not an autonomous baseline run.

The Mac locked before a further distinct-target close-button handback check. Do not claim that final check passed.

After the Mac lock, QA was stopped through the connected Editor. Restored 1920×1080 and requested the original Edit-mode/Menu scene and profile root. The Game view remains expanded until native UI access returns. No unattended match was left running.


### Skirmish extension — 2026-09-19 (not certified)

Latest request: finish campaign ARIA and extend it to Skirmish. Guided campaign baseline matrix above remains ten normal-speed wins. An additional English M2 run accepted nine ARIA gestures before physical interruption; Manual, reason 6, zero pending/pressed. The intended distinct-control live handover was missed because gameplay advanced before the click; it is **not** counted as that live check.

Input fixture now explicitly tests a different button during a pending ARIA press: first physical press invokes neither button, second manual press invokes the new button once. **14 input cases + 10 campaign decision cases + 10 Skirmish planner cases passed** through the connected Editor. Fixture restores EventSystem/device registration.

Skirmish source added:
- Typed observations of displayed squad cards, selection, command mode, base health/population, live drawer controls and visible public enemy-base objective marker. No ECS entities or world positions enter the planner. The fixed Base Assault objective is already public through its base-focus button; the development marker renders its tap point for the player as well.
- Burst planner for recruitment, group selection, base focus, attack, bounded observation waits and no-progress handback. Real virtual touchscreen remains the sole actuator; no old direct gameplay probe is used.
- Conversational EN/FA intent explanations, same confirmation and instant takeover; exact supported campaign IDs; reject concurrent independent player-faction AI without changing the chosen rules.
- Numeric HUD mirrors avoid parsing localized labels every frame. Terminal Skirmish lifecycle cancels pending gestures immediately.
- Skirmish is deliberately available only in Editor/development builds as **Watch ARIA — preview** until the full coverage row passes. Do not remove that gate on the strength of planner unit tests.

First live Skirmish pilot: five accepted touches, then correctly bounded failure. Recruitment opened/closed without completing; squad selection visually selected four soldiers but generic squad readout returned None. Corrected observations to read the displayed card selection and added three seconds for drawer availability to settle before affordability backoff. A second run was launched but Mac locked before visible Start confirmation; restored Edit mode rather than leaving the match clock running. **No full Skirmish win is claimed.** These corrections still need a live rerun.

Next concrete work:
1. Unlock-dependent: rerun Skirmish EN, confirm Watch through visible UI; inspect recruitment control resolution if it still closes without recruitment. Verify visible target marker aligns with the actual selectable base and target acceptance, not only tap delivery.
2. Complete combat/defense/reinforcement strategy, construction/economy shortages and realistic mid-match recovery. The current base-attack loop is not yet a complete tactical planner.
3. Full EN/FA matches over pinned seeds; record actual outcomes, accepted orders, resources, no-progress behavior and terminal cancellation. Do not use direct SkirmishGameplayProbe actions as ARIA evidence.
4. Finish broader campaign interruption/varied-start checks and device gates separately from the ten baseline wins.

Normal Editor has been returned to Edit mode/Menu and the validation profile cleared after the Mac lock. No player save changes, commits or push in this extension turn.

### Continued Skirmish live audit — 2026-09-19

The corrected squad observation and recruitment settling logic worked in the next EN live run: paid recruitment delivered a third squad, and visible Attack gestures issued real orders. The run nevertheless lost: directing every squad at the base ignored attacking enemy units. This is a failed tactical acceptance test, not readiness evidence.

Added threat observations sourced only from contacts already rendered on the player's minimap, plus the normal tactical-map popup navigation. ARIA taps the minimap to open it, taps a presented hostile contact to focus, closes it, selects Attack and targets that battlefield position. No direct camera/order/health/resource mutation. The first navigation pilot bounded out because it did not understand the popup; a second exposed repeated tracking of a moving contact. Added explicit popup handling and one focus gesture before returning to the battlefield. Focus gestures on the small and large maps have distinct IDs. Recruitment unavailable backoff reduced from 45 to 12 seconds.

Skirmish decision fixture now has 15 passing cases including threat priority, popup focus and close. Full-match validation of this correction is still in progress. Release/development gate unchanged; no Skirmish win claimed.

Further live diagnosis found two independent stalls: recruitment retries could preempt an unfinished combat sequence, and the tactical-map close decision depended on the touch system still targeting the map. As soon as the hand aimed at Close, that condition became false and selected the map again. Recruitment now occurs between combat sequences, and completed map focus is latched until the popup closes. Added regression cases for both. Minimap threat inspection is inactive outside an active demonstration.

Final connected-Editor checks for this checkpoint: **18 Skirmish planner + 10 campaign decision + 14 real Input System fixture cases passed (42 total)**; `git diff --check` passed. The Mac locked before the latest corrected live run could be confirmed; that unstarted run was stopped and the Editor/profile restored. The latest map-close/recruitment corrections remain live-validation pending. Do not promote Skirmish from development preview or call it complete.

Resume with a fresh normal-speed EN Skirmish, visible Watch/Start, then no manual gameplay. Specifically verify: popup closes once after focus; actual Attack reaches visible hostile contact; recruitment does not preempt targeting; enemy losses and objective health progress; true terminal outcome. Follow with FA and adverse-state runs. If targeting moving contacts repeatedly delays aiming, diagnose and test tracking separately instead of relaxing all UI movement checks. Construction/economy and broader campaign recovery/device coverage remain open as above.

### Skirmish Start returning silently to preview

User reported Watch → Start returning to preview with no play. Fresh isolated EN startup did run (six gestures), so the exact user session was not reproduced. Source audit found a cross-mode terminal bug: AriaPlayInputSystem OR-ed any retained campaign result with the active Skirmish outcome. Skirmish now exclusively owns terminal detection while present. Campaign final-tutorial suppression also no longer overrides Skirmish observation. Start rejection keeps the confirmation open with EN/FA retry/cancel feedback rather than discarding the return value and silently returning to preview.

Added three terminal-scope regressions: prior campaign Victory plus playing Skirmish remains active; finished Skirmish stops; campaign Victory without Skirmish still stops. Connected Editor passed 13 decision + 18 Skirmish + 14 input checks (45 total). Post-change FA visible Watch/Start remained active through nine touch gestures, paid recruitment, squad selection and hostile-target attack, with zero stop reason. This verifies startup/action progress, not a full Skirmish victory. The broader readiness gate remains open.

## 2026-09-19 — visible holographic hand correction

- Root cause of the invisible pointer: the runtime custom Graphic had no CanvasRenderer. Added an explicit renderer and a required-component attribute so every construction path is safe.
- The hand now remains visible throughout an active demonstration, travels from its previous location to each aiming target, and follows the accepted contact exactly during a press/drag. Removed the old 24-pixel target-local approach approximation from the gateway.
- Added cyan outline/glow, wireframe palm, contact rings and a short expanding press ripple. Screen-relative sizing, complete mesh bounds, and fingertip-centered edge rotation keep it readable. The overlay is input-transparent except for its existing Stop control.
- Live English Skirmish through visible Watch/Start: observed the hand above Build, Soldiers, world targets and the tactical map. Runtime mesh had 688 vertices, was not culled, and did not receive raycasts. Read-only sampling recorded 143 distinct aiming positions in ten seconds. Hand disappeared on terminal result.
- The current planner navigates the camera through map taps; this change does not invent drag animations for tap-driven camera movement. The actual touch-driver drag is covered by the fixture, including hand/contact alignment.
- Validation: 13 decision + 18 Skirmish planning + 18 input/presentation checks passed. Four added regression assertions cover renderer presence, input transparency, press alignment and drag alignment. Editor restored to Edit mode; isolated save override cleared.
- This is a presentation fix, not Skirmish victory certification: the observed match ended in defeat. The existing development-only preview gate remains, and tactical reliability is still open.

## 2026-09-19 — fixed left minimap and compact selection card

- Removed selection-dependent horizontal minimap relocation. The map remains at the left margin above the squad tray / placement controls.
- Selection title and subtitle now overlay the portrait on a dark backing. The card measures the space above the minimap, adjusts portrait height, and caps its visible bounds with a 12-unit gap. Overflow details scroll within the card; the passenger drawer retains its separate floating rendering and input surface.
- Live isolated Skirmish checked with English squad selection, then Farsi squad and vehicle selection. The map stayed at the same left position. Corrected portrait sibling order during visual QA so the image remains visible under the title strip.
- Final code compiled; git diff whitespace check passed. Editor restored to Edit mode and validation save override cleared. Android device validation remains separate.

## 2026-09-19: passenger drawer exception and Skirmish behavior follow-up

Work in progress; **Skirmish autonomous victory is not certified**.

- Replaced C# null-coalescing component creation with Unity-aware null checks for the compact selection panel's ScrollRect and passenger drawer Canvas. Live selection created the Canvas without the reported exception. Added a prefab-level repeat-layout regression check.
- Removed tactical-map opening from the Skirmish planner. An already open map is closed before world orders; visible base focus controls provide navigation. Moving world contacts no longer perpetually restart the hand approach delay.
- Added visible Select + drag-box grouping through the existing touch actuator, camera-settle delay, and recruitment backoff. These are still under gameplay evaluation.
- Focused decision/input checks passed (14 decision, 23 Skirmish, 18 touch cases). Group cases cover camera settlement, selection mode, drag endpoints, and advancement after the gesture.
- Live tests removed the map loop but did not establish a victory. The grouping observation failed to produce a clear rectangle in the crowded HUD view and fell back to one squad. Troops were lost in the initial navigation-only run. Do not present these changes as a winning strategy yet.
- Live testing stopped because the Mac locked. The isolated run was stopped and the Editor restored to Edit mode. Resume with visible grouping/camera geometry diagnosis, then complete a hands-off normal Skirmish to a real terminal result. Preserve touch-only gameplay and the development-preview restriction.

### Selection-mode correction

The planner previously emitted Select before checking for a usable drag rectangle, then silently fell back to a squad card. It now requires both available corners and a non-degenerate rectangle before emitting Select. With a valid rectangle it emits the existing hold-and-drag gesture; without one it waits briefly and uses a squad card without pressing Select. Added regression cases for missing, incomplete, and zero-size rectangles; 26 planner and 18 touch checks passed. Ran an isolated live Skirmish smoke check and restored Edit mode. This does not certify a full Skirmish victory or resolve the remaining crowded-view rectangle detection issue above.

## 2026-09-19: full-match defeat investigation (latest status)

**Not ready; no autonomous victory verified.** The user's defeat report is reproduced. A completed normal Skirmish run ended in defeat at 3:08, with 29 units lost / 3 defeated and one building lost. Subsequent runs exposed additional planner sequencing faults; they are diagnostic runs, not passes.

Changes under evaluation:
- Try both selection-box diagonals to avoid crossing HUD panels.
- Distinguish a single-card fallback from grouped selection; rotate orders through remaining squad cards. A box is not assumed to include the entire army either.
- Recruit toward the displayed preset's 24-infantry capacity, back off eight seconds between orders, and assemble an opening force before advancing (bounded opening timeout).
- Choose threats from all currently presented map contacts, not only those already inside the world camera. Navigate with a bounded open/focus/close sequence. This supersedes the earlier blanket map-opening removal.
- Wait for full-map marker presentation; require completion of the specific -20004 focus gesture before closing. An increment caused by opening the map is not focus completion.
- Keep camera settlement inside the attack sequence, preventing recruitment from pulling the camera away before the world attack.

Focused planner validation: 30 cases passed. The latest full-match retest could not start because the Mac locked. Stopped the isolated run and restored the Editor. Latest changes remain uncommitted and must not be represented as a verified winning Skirmish implementation.

Temporary diagnostic evidence: /private/tmp/aria-combat-run.jsonl, /private/tmp/aria-opening-run.jsonl, /private/tmp/aria-navigation-run.jsonl, /private/tmp/aria-focus-run.jsonl. These are read-only observations; no resources, unit stats, damage, or win conditions were modified to help ARIA.

### Unlocked continuation: map gestures, startup, rally combat (2026-09-19)

**Still not ready: no autonomous Skirmish victory verified.**

- Reproduced an immediate Start cancellation (StopReason 4): input validation rejected stale observation data before the HUD published its first active observation. Starting now waits up to three seconds for a fresh observation; terminal, focus, route, and pause checks remain active. Subsequent live starts succeeded.
- Full-map taps inside the camera viewport intentionally do nothing. ARIA now performs a real viewport drag for these contacts. A live sequence centered the enemy, closed the map, and issued Attack followed by a world target touch.
- A completed subsequent run still ended in defeat at 3:08 (17 lost / 6 defeated). Opening and recruitment sequencing remain under evaluation. Do not equate the successful touch sequence with tactical success.
- Current defensive opening uses the starting squad cards and Hold while recruiting. Newly delivered troops retain their automatic rally; they are not repeatedly stopped at delivery. Recruitment no longer interrupts the short wait between orders to different squads. Latest focused planner suite passed 35 cases.
- Found a shared gameplay defect: automatic player reinforcement rally used the manual Move tag, suppressing engagement and retaliation while the path was active. The automatic rally now removes only the tag created by its own order. Existing player Move/Hold/Attack orders retain priority, and rally remains one-shot. The expanded PaidRecruitsRallyOnceAndNeverOverridePlayerOrders regression passed in the connected Editor.
- Additional live diagnostics: /private/tmp/aria-viewport2-run.jsonl, /private/tmp/aria-defense-run.jsonl, /private/tmp/aria-hold-run.jsonl, /private/tmp/aria-rally-run.jsonl. These exposed tactical losses; none is a victory certificate.
- The Mac locked again before the latest full-match retest could be started. Stopped the isolated run and restored Edit mode. Resume with the current defensive opening plus interruptible rally, complete a normal hands-off match, and inspect actual terminal outcome. Preserve the development-preview gate and touch-only gameplay. No stats, resources, enemy difficulty, or victory conditions were changed to help ARIA.

### Recruitment cycle regression and latest live run (2026-09-19)

**Not ready: no autonomous Skirmish victory verified.**

- An empty final squad slot prevented the attack cycle from completing, starving recruitment. Completion now skips empty trailing slots before advancing the cycle. Empty armies also return to recruitment rather than rectangle selection around buildings. Focused planner suite passed 37 cases before this live run.
- Normal Skirmish touch pacing uses a 0.65-second post-gesture pause; campaign pacing is unchanged. Recruitment retries after delivery use a three-second delay. No combat stats, resources, difficulty, or victory conditions were altered.
- Read-only click diagnostics confirmed real Attack world touches were accepted. Diagnostics are Editor-only and disabled by default. Map narration now describes navigation without claiming every gesture is a tap.
- Fresh EN Watch/Start run recorded 94 gestures, actual grouping, recruitment recovery, and two completed attack cycles. At the last active sample (338.6 seconds), the player base was still 1200/1200 but only one infantry remained; enemy base was 1200/1200. This is not a pass: casualties outpaced replenishment, and repeated navigation/regrouping consumed too much time.
- The Mac locked during the run. Subsequent terminal read showed defeat at 6:09, 41 units lost / 27 defeated. Because active ARIA observation stopped before that outcome, distinguish the verified tactical attrition from the unattended terminal result. Earlier completed run was also a defeat at 3:35, 29 lost / 8 defeated.
- Evidence: `/private/tmp/aria-cycle-run.jsonl`, `/private/tmp/aria-click-run.jsonl`. Restored Edit mode and cleared isolated save override after the lock. Next work: improve replenishment scheduling and reduce redundant navigation/regrouping, then complete uninterrupted EN/FA normal-speed matches. Keep the development-preview restriction.

### Replenishment and target continuity follow-up (2026-09-19)

**Still not ready; no autonomous victory.**

- Removed repeated regrouping after each recruitment and on transient loss of selection. Surviving squad cards continue the existing rotation; Select remains restricted to an actual prepared rectangle drag.
- Depleted forces can replenish after a completed attack gesture, without waiting for every squad slot. Replenishment is bounded to two orders before returning to combat; unavailable production preserves its longer backoff.
- Removed the blanket opening Hold orders. The engagement code restricts held units to weapon range, preventing them from closing on longer-range attackers. Normal automatic engagement remains active during opening recruitment. Full infantry capacity can end the opening early.
- Full EN run with the revised opening, before the latest two-order/visible-target changes, ended in **defeat at 7:49, 53 lost / 40 defeated**. Player base remained undamaged through much of the run, but attrition and navigation still prevented an offensive breakthrough. Evidence: `/private/tmp/aria-mobile-defence-run.jsonl`. Earlier partial diagnostic run stopped at 6:05 with 240 base health, 50 lost / 17 defeated (`/private/tmp/aria-replenish-run.jsonl`).
- Target observation now prefers an enemy already on screen and reachable through the HUD, rather than opening the map for a slightly nearer off-screen contact. It still uses only presented map contacts and screen geometry. No hidden orders or game-balance changes.
- Latest planner validation passed 42 cases. The Mac locked at Start confirmation for the combined retest; its start was not verified, and the Editor was restored to Edit mode. The latest target-continuity and two-order changes still need uninterrupted live EN/FA validation. Do not promote the preview to ready.

### macOS screen-saver interruption: diagnosed and mitigated (2026-09-19)

- Read-only Settings inspection: display sleep Never, automatic logout disabled, password delay eight hours. Those settings did not explain the repeated interruptions.
- macOS loginwindow logs confirmed the actual trigger: `targetUserIdle = 1200.0`, followed by `starting screen saver due to user idle` and `kLWLockFromScreenSaverIdleLaunch`. The same check reported `preventIdleDisplaySleep = 0`. Unity's assertions prevented system sleep only.
- With user authorization, started `/usr/bin/caffeinate -di -t 7200` for the QA session. `pmset -g assertions` verified `PreventUserIdleDisplaySleep = 1` and an owned caffeinate timeout. Password, screen saver preferences, and manual lock behavior were not changed.
- Before future attended gameplay QA, start a bounded display/system keep-awake assertion and verify it with `pmset -g assertions`. Release the QA-owned process after testing, or let its timeout expire. Do not treat this as an unlock mechanism; a manually locked Mac still requires the user to unlock it. The current guard expires around 22:12 local time on 2026-09-19.

### Attack Move and Skirmish continuation (2026-09-19)

- User selected ground **Attack Move**: engage enemies encountered along the route, then resume the destination. Direct hostile taps retain focus fire. Uses the existing Attack button and ordinary movement, formation, terrain, combat and visibility rules. Move, Hold and direct attack cancel the old advance; automatic firing approaches preserve it. Added destination feedback and selection status in English and Farsi.
- Connected Editor movement validation passed 20 cases, including retaining the destination during combat, resuming afterward and canceling on Move/Hold. Planner validation passed 54 cases, including visible ground gestures and recruitment drawer closure during affordability backoff.
- Manual live EN touch check: squad card → Attack → clear ground produced the attack marker, movement and “Advancing and engaging” selection status. This verifies the player-facing interaction, not an autonomous victory.
- Prior approach-fix run ended in defeat at 9:43 (81 lost / 49 defeated). Coordination run was stopped for diagnosis; tower-opening run built one tower through UI but later lost Unity focus. None passed the win gate. Evidence: `/private/tmp/aria-approach-fixed-run.jsonl`, `/private/tmp/aria-coordination-run.jsonl`, `/private/tmp/aria-tower-run.jsonl`.
- First Attack Move planner diagnostic exposed reliance on a fog-filtered base contact; changed observation to the public base objective already used by Enemy Base navigation. ARIA still issues all gameplay through the shipping touch driver, and no combat stats/resources/difficulty/victory conditions were changed.
- **Not ready: full autonomous EN/Farsi victories remain to be verified.**

#### Full-map follow-up

- Farsi Attack Move run ended in defeat at 5:47, 49 lost / 19 defeated; enemy base remained 1200. Recorded in `/private/tmp/aria-attack-move-fa.jsonl`.
- Added ordinary visible ground placement gestures for defensive towers, with bounded candidate sites and confirmation only when the actual placement button is enabled. Planner now passes 58 cases.
- Tower-backed EN diagnostic preserved full base health and reached 23 infantry, but exposed a long-distance pathfinding defect: stripping the manual-order tag also stripped commanded pathfinding behavior. Stopped that run for correction; it is not a victory. Evidence: `/private/tmp/aria-defense-advance-en.jsonl`.
- Attack Move now retains commanded pathfinding priority. Shared combat explicitly permits engagement/retaliation for an Attack Move order while preserving plain Move behavior. Added a real ECS combat regression verifying hostile acquisition, neutral exclusion, interruption of travel and retention of destination; passed along with 20 movement cases and 58 planner cases.

#### Frontline and building acquisition follow-up

- Commanded-path EN run confirmed units advancing across the map but was stopped at about 8:38 with four infantry remaining and both bases at 1200 health. Not a victory (`/private/tmp/aria-commanded-advance-en.jsonl`).
- Frontline target observation now uses four forward presented friendly contacts, preferring a nearby visible threat over another advance. Planner validation passed 59 cases. The next EN diagnostic built two towers using visible taps/confirmation and preserved the base, but suffered substantial infantry attrition without damaging the enemy base; stopped for investigation (`/private/tmp/aria-frontline-focus-en.jsonl`).
- That investigation exposed a shared Attack Move gap: normal automatic acquisition omits static buildings. Added a separate hostile-building candidate list for Attack Move only, after mobile-threat acquisition. Neutral, friendly, dead and campaign-suppressed buildings remain excluded. Plain Move/Hold/idle behavior is unchanged. Added a real ECS regression for this behavior; validation pending at the time of this entry.
- Autonomous victory acceptance remains open. Defensive survival and successful gestures are not sufficient to mark ARIA ready.

- Connected Editor validation passed after the building-target change: 20 movement cases, hostile-unit interruption, hostile-building acquisition with neutral/allied/dead/suppressed exclusions, existing Hold acquisition regression, and 59 planner cases.
- EN combined diagnostic (`/private/tmp/aria-building-advance-en.jsonl`) was started through visible Watch/Start controls. ARIA placed one valid tower, recruited and issued advances with its finger above the base HUD. At approximately six minutes, its base was still 1200 but infantry had dropped to seven and the enemy base remained 1200. Stopped for investigation; this is **not** a victory or readiness pass.
- Remaining acceptance work: coordinated Skirmish assault/replenishment, complete autonomous EN and Farsi victories, and aircraft-specific Attack Move resume/fuel-return interactions. Ground interaction and focused combat regressions pass; full-mode readiness remains unverified.

### Coordinated Skirmish assault work (2026-09-19 continuation)

- Replaced the early 90-second departure with assembly up to 24 infantry, waiting for a nearby threat to clear, with a bounded fallback when production is constrained. Added public-contact target continuity so squad orders concentrate on a moving enemy instead of switching targets every observation.
- The first assembly diagnostic still split its force; no victory (`/private/tmp/aria-assembled-force-en.jsonl`). The continuity run reduced hostile combatants from 21 to five, but then split into separated squad advances and suffered losses at the base approach. Stopped for correction, not counted as a completed acceptance run (`/private/tmp/aria-target-continuity-en.jsonl`, `/private/tmp/aria-continuity-combat.txt`).
- Read-only combat evidence verified an actual rectangle-selected group of 16. The planner had immediately replaced that group with individual squad-card selections after its first order. Changed it to preserve the group across fights. It reads the existing localized selection heading count, avoids opening production while a substantial selected force fights, and rebuilds a depleted assault rather than streaming replacements separately.
- New full-match verification is required. No stats, enemy difficulty, resources, or victory conditions were altered.

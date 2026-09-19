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

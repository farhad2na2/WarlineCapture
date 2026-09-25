# O001 player experience redesign — review candidate

Date: 2026-09-24. Source baseline: `5a62ae9ba90ea19d4bf3b0bd5e202ea9ca3673de`.

Status: **Visual direction approved by the user in this task on 2026-09-24. Native implementation and automated validation are complete. Player/device acceptance is pending. O001 is not accepted as player-ready.**

## Candidate at a glance

- Implemented: automatic paused briefing and camera tour; persistent objectives and ARIA guidance; ordinary tactical controls; separate camera guidance and green ARIA PLAY / red STOP ARIA controls.
- Proved in Editor: five Regular-seed ARIA full victories, a Persian full victory, 76 focused tests, withdrawal lifecycle, and save/resume across Editor processes.
- Normal mouse journey: all three scans and evidence recovery, then Partial Success at the deadline. A full human-controlled win is still required.
- Final English control and wide Persian layout reruns passed. No phone acceptance or release deployment is claimed.

## Request and authority

The user rejected the deployed O001 experience: no visible purpose or ARIA introduction, an unexplained ADVANCE: EXIT button, hidden objectives, ambiguous signal controls, poor mobile legibility and inconsistent visual styling. They approved planning and executing the recovery and asked to be consulted on new screens. This pass follows the agreed sequence: review concrete mockups, implement the first playable sequence, then complete and validate the mission. Historical plans and readiness reports are evidence, not new instructions or proof of current acceptance.

The screen review is requested because the agreed workflow calls for visual direction review before implementing another UI. It is not a permission requirement imposed by the ImageGen skill. Independent source analysis and planning proceeded while review was pending.

## Review screens

1. [Deployment briefing](mockups/01-deployment-briefing-v01.png): ARIA, mission purpose, the full required sequence, force and time limit, one Show the plan action.
2. [Gameplay checklist](mockups/02-gameplay-checklist-v01.png): persistent objectives, current next step, compact ARIA guidance, one Show objective camera action.
3. [Active scan](mockups/03-active-scan-v01.png): progress at the target and in the tracker, readable range boundary and interruption guidance, existing Scan control.

All three were generated using the built-in `image_gen` tool. Exact prompts are retained under [prompts](prompts/). References were the user's current O001 screenshot, the actual M01 English gameplay handoff, and the existing first-launch ARIA visual. The latter two live at:

- `Design/AgentReports/M01StoryReplay/Evidence/english-gameplay-handoff.png`
- `Design/VisualLockLayered/SCN-00_FirstLaunch/iterations/iteration_06/aria_guidance_v3_16x9.png`

The generated world, marker positions, troop arrangement and portrait pixels are illustrative. These images are review references, not runtime assets, gameplay captures or proof of a functioning mechanic. Implementation must reuse the actual authored map, portrait assets, UI sprites, font and live mission facts. In particular, the generated active-scan courtyard is not a request to rebuild the map or change the roster.

## Initial visual review

- All three concepts preserve the established colored command bar and illustrated squad cards, with large type and framed dark panels.
- The checklist and ARIA occupy the right edge without overlapping the bottom controls; the battlefield center is usable.
- The active scan correctly remains at 0/3 while its first site is at 8/15 seconds.
- Future objective rows need a contrast check in the native UI; dimmed must not mean unreadable.
- The gameplay panel currently uses about a quarter of screen width. User review should resolve whether this feels too large; a compact variant must retain the objective summary and immediate next step.
- The briefing's optional-mastery text needs more precise native copy: "Optional: finish all scans without losing recon infantry." Avoid suggesting an unverified monetary bonus or requiring recon survival after scanning.
- The world progress label is a status graphic, never a button. Decorative framing must not imply it issues orders.
- Native text, numbers and layout require EN/FA verification. ImageGen typography is not the implementation source.

## Initial audit of the rejected implementation (historical)

- `OperationsMissionScreenView.Update` disables `AriaTutorialBriefingView` and hides briefing/status/objectives unless `guideOpen` is true.
- `OperationsMissionPresentationSystem` starts at `Focus(definition.exitPosition)`; this is also the starting squad location, so the extraction marker dominates deployment.
- Signal A/B/C buttons focus the camera, adjacent scan buttons issue gameplay actions, and ADVANCE markers issue travel/combat intents. The guide mixes several unrelated functions.
- Failed advance writes a notice that is invisible when the guide is closed. This is a plausible explanation of the reported apparent no-op, not a reproduced diagnosis of that exact click.
- `MatchHudAssistantUiSystemHelper.Watch` depends on the guide's ScanButton, AdvanceButton, FocusButton and RecoverButton. Removing only the visuals would break the current ARIA path.
- The existing toolbar Scan handler has campaign radar handling and a generic scan-command path, but no O001-specific integration at that UI entry point.
- The objective system already awards full Victory automatically when its extraction predicate is met. Conclude exists for Partial. Preserve automatic Victory and expose Partial clearly in the pause flow instead of deleting the outcome.

## Player journey and interaction contract

### Deployment and camera introduction

Deploy through the normal Operations route. When map, squad and HUD are ready, show the briefing automatically. Pause simulation and all mission pressure during the briefing and tour, without spending gameplay time or overwriting the player's pause preference.

Purpose: find the active relay by scanning the three known courtyard search sites and return its evidence. Do not invent additional Operations campaign goals or rewards.

Required sequence: scan three distinct sites, recover relay evidence, bring its live carrier and at least two original eligible infantry into the safe extraction zone before the 12-minute deadline. Show optional recon mastery separately. Keep existing balance, finite forces, wave triggers, AP and persistence rules.

Show the plan begins a short skippable tour of public search courtyards, then extraction, then returns to the squad. ARIA supplies brief captions. Do not reveal undiscovered enemies or the evidence position before it is available. Respect reduced motion through static framing/cuts. Skip returns to the squad and starts player control reliably. Mid-mission resume restores progress with a short current-objective recap rather than replaying the full tour.

### Gameplay

Keep all three required stages visible with active/future/complete semantics. During scanning show 0/3 to 3/3, with distinct site status available without adding gameplay buttons. Sites are valid in any order; a recommended next site is guidance, not a forced sequence. Support grouped and split-scout play.

One guidance control, Show objective, frames the current public target and temporarily offers Return to squad in that same slot. It must never select units, move troops, scan or complete an objective. No involuntary mid-combat camera jumps. Offer the same control when the next stage unlocks.

Use normal selection and movement/attack controls. Integrate the existing Scan command with eligible selected infantry at a known site. When unavailable, show a visible reason immediately (select infantry, move within range, blocked approach, site already complete). Keep active scans idempotent so repeated taps cannot reset progress. Show scan progress and interruption feedback beside the active objective.

After all three scans, reveal the evidence marker and explain recovery. Use a single clearly labelled contextual Recover evidence interaction at the valid location, outside the guidance panel. Preserve the normal Scan function elsewhere; do not silently relabel an unrelated command or create a second control matrix. Show the carrier and recovery progress, and update guidance if the carrier dies and evidence is dropped.

Extraction uses normal movement. Explain remaining requirements in plain language (carrier missing, insufficient infantry, unsafe extraction). Preserve automatic Victory. Save & Exit, Withdraw and the eligible Partial outcome belong in the established pause flow; label Partial as an early extraction with partial success and explain its consequences. Keep the existing ARIA Watch/Play entry available through its established assistant affordance, separately from ARIA's guidance text; removing the prototype top button must not remove ARIA play capability.

### Results

Show completed objectives and the actual persisted result, then return to Operations. Preserve exactly-once AP, reward and district settlement. Never show a saved result before acknowledgment. Remove mission guidance and camera ownership cleanly on finish, withdrawal or return.

## Implementation sequence

| Gate | Work | Required evidence | Status |
|---|---|---|---|
| D1 | Source audit, three screens, interaction contract | Saved review concepts and user feedback | Approved |
| D2 | Native briefing, persistent tracker, tour, first Scan through toolbar | Native captures, target-in-viewport assertions, agent mouse deployment and first scan | Verified in Editor; no video recording |
| D3 | Remaining sites, evidence, carrier loss, extraction, pause/Partial, result | ARIA full wins; agent mouse run reached Partial; focused negative-case tests | Implemented; full human-controlled Victory pending |
| D4 | ARIA adapted to supported visible controls; EN/FA and resume | EN/FA full wins, Stop/Play and fresh-process checkpoint evidence | Verified: five Regular seeds, EN/FA, resume and final UI checks |
| D5 | Regression and player-facing acceptance | Candidate identity, logs, captures, usability findings and explicit limitations | Pending |

D2 must be inspected in motion before expanding D3. A good still does not close D2. Do not mark any gate passed using previous screenshots or previous ARIA victories.

## Source implementation map

- `Assets/Game/Scripts/UI/Screens/OperationsMissionScreenView.cs`: replace runtime prototype panels with the approved native composition, persistent read-only checklist, ARIA guidance, camera action and lifecycle states. Reuse existing UI art and typography; remove competing briefing suppression once ownership is explicit.
- `Assets/Game/Scripts/Composition/OperationsMissionPresentationSystem.cs`: project current stage, progress, public target, survivor/carrier status and refusal reasons; own presentation coordination without adding a second mission simulation.
- `Assets/Game/Scripts/Components/OperationsReconComponents.cs` and the existing Operations UI contracts: add only the state required for briefing/tour readiness and public guidance; keep checkpoint/session behavior explicit.
- Existing campaign camera tour contracts and `CampaignMissionPatrolOrderSystem.DefenseCamera.cs`: inspect for reusable camera behavior, pause handling and reduced motion. Do not attach a Campaign mission identity to Operations to obtain the effect.
- `Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.CommandTabs.cs`: route normal Scan through the correct active-mode adapter, preserving Campaign radar and generic Skirmish behavior.
- `Assets/Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.Watch.cs`: replace legacy guide-button targets with visible selection, command, camera and world-target observations. ARIA must use the same reachable controls as a human; hidden buttons or direct-order fallbacks are not acceptable.
- `Assets/Game/Scripts/Runtime/Missions/OperationsReconObjectiveSystem.cs`: preserve authority and existing outcome semantics; make only necessary interaction fixes backed by focused tests.
- `Assets/Game/Configs/Localization/OperationsO001UiStrings.json` and the existing catalog import path: add natural EN/FA briefing/guidance/state copy; check RTL, numbers and clipping in rendered UI.
- `Assets/Game/Scripts/Editor/OperationsReconLaunchSmokeValidation.cs`: replace checks that require the closed guide/prototype buttons with meaningful first-frame briefing, paused tour, readable tracker and normal-input journey checks.

## Validation contract

All Unity execution must use `rtk proxy Tools/CI/invoke_unity_macos.sh` with explicit timeout and full log, Hub open and signed in, no batchmode. Do not stop existing Editors or reset licensing. Any missing pass marker, timeout, project lock or nonzero exit is a failed validation, not an inferred licensing incident. Load the Unity CLI skill before interacting with a running Editor; use connected Pipeline commands for appropriate inspection, and repository wrappers for validation/capture.

Focused checks must cover:

- Briefing/tour freeze timer and combat; skip, reduced motion, pause, restart and resume hand off control once.
- Checklist visible at handoff; Show objective changes only camera state; unavailable commands give visible reasons.
- All three sites scan in any order; repeated taps do not reset an active channel or count a completed site twice; split scouts remain valid.
- Evidence stays hidden until unlocked, recovery channel works, carrier death leaves recoverable evidence, extraction reports missing conditions and awards Victory automatically.
- Partial, defeat, withdraw, Save & Exit, fresh-process resume and exactly-once result/return remain valid.
- EN/FA native layouts at 16:9 and wide mobile aspect ratios, safe-area simulation, readable text, no clipping or command overlap. Actual phone touch/performance acceptance is separate and cannot be claimed from Editor images.
- A complete normal-speed human-control journey and a complete ARIA visible-input journey. Preserve the existing Regular seed matrix (1102/2102/3102/4102/5102, at least 4/5 including 1102) and FA 9102 on the changed candidate for the corresponding acceptance claim; retain failures.
- Affected Campaign camera/Scan/ARIA/pause and Skirmish Scan/Watch regressions. Investigate new failures and disclose unrelated existing failures.

Player-facing review: a campaign-familiar player should identify the mission purpose, first destination, next action and win conditions without developer explanation, then finish through ordinary controls. If no real user/device review occurs, report that gate as pending. Do not claim an automated agent substitutes for unfamiliar-player testing.

## Continuation

The user approved this direction. Execute D2 and continue through the remaining gates. The user has authorized implementation after the agreed design review; do not repeatedly request permission for routine code, art reuse, testing or fixes within this scope. Ask again only if a new material design choice needs their judgment.

## Runtime progress — current implementation

The native briefing and HUD now reuse the live campaign font and ARIA portrait. The simulation pauses for the intro/tour; the persistent tracker exposes camera guidance and the user-requested visible ARIA Play / Stop control. SCAN routes through the existing command bar. The pause menu holds save/exit, a supplementary ARIA shortcut and conditional early extraction. Read-only world rings show scan range. Interaction feedback covers range, blocked approach, interrupted channels and carrier loss.

Historical early-run evidence (superseded by the later runs below):

- `evidence/launch-04/validation.log`: wrapper exit 0 and deployment smoke pass. Fresh briefing and gameplay screenshots are native captures. A later inspection found the objective-focus capture happened before camera settling; the harness has since been strengthened to check the actual world target viewport after settling. The early focus still is not camera-proof.
- `evidence/first-scan-01/validation.log`: failed, retained. ARIA made 6 actions but infantry stayed at extraction; the visible squad-card default did not establish an actual selection. The observer now checks the normal selection read model and selects the squad through the visible card before issuing commands. A rerun is underway.
- The cloned Editor emits a Unity Editor SearchDatabase indexing exception at startup. It is outside the mission stack and did not prevent the deployment smoke. This is recorded rather than silently described as an entirely clean Editor log.
- Full normal-input victory, save/resume, focused regression tests, RTL/wide-screen review and actual player/device acceptance remain open. These screenshots and smoke results do not establish player readiness.

### User correction: visible ARIA Play / Stop

The user explicitly requested the missing visible ARIA start/stop control during implementation. ARIA Play must remain on the gameplay HUD, alongside guidance, and switch to STOP ARIA while active. Stop returns control to the human; it does not withdraw from or restart the mission. Show objective remains a separate camera-only action. The pause-menu shortcut is supplementary, not the primary entry. This correction supersedes any interpretation that the tracker may contain only one button of any kind.

### Input findings after the visible ARIA correction

- First-scan runs 03–06 failed and their logs are retained. Run 04 demonstrated a real selected squad (16 selected) after matching the existing isolated input fixture's Editor settings. Background input requires both the opt-in flag and a temporary `InputSettings` clone; the harness restores the original settings, and shipping input is unchanged.
- Runs 05/06 traced the no-op to normal Attack target resolution: both the signal center and nearby ground resolve to a neutral `DenseCityBuilding`, so the attack path never issues a movement order. The new watch path uses the normal MOVE toolbar to reach the visible scan area. This is under validation; no direct move intents or hidden mission buttons are used.
- The missing scan ring was a missing CanvasRenderer requirement on the custom UI graphic, now fixed. Updated native captures include the actual ring and the visible ARIA start/stop button. Later source makes STOP ARIA red and expands guidance space to prevent longer feedback overlapping controls.

### Camera, first scan and combat verification

- `evidence/first-scan-10/validation.log`: wrapper exit 0, `journey=first-scan input=visible-touch scans=1`. The stronger tour assertion verifies that all three public sites, extraction and the squad enter the playable camera viewport. Briefing/tour hold the clock. Native active-scan capture includes the range ring, checklist, portrait, camera action and red Stop ARIA.
- Runs 08/09 failed before the successful tour proof. Pausing simulation also skipped the normal camera request update. The Operations introduction now drives the shared camera request owner while simulation is paused, using unscaled transition time. No troop orders or gameplay ticks run in that driver.
- `evidence/victory-01/validation.log`: full ARIA run failed at 166.5 mission seconds with two scans and one survivor. Moving through combat without engaging was not a viable default route. The candidate now uses normal Attack-ground advance; the shared picker excludes neutral city scenery from attack targets so its focus footprint cannot swallow a ground advance. Neutral buildings remain protected.
- `evidence/tests-01/results.xml`: **76/76 EditMode tests passed**, wrapper exit 0. Includes Operations world/checkpoint rules, briefing pause, shared camera, Scan, pause prefab, automatic faction targeting, and a regression reproducing the neutral-city focus footprint. These are rule/regression checks, not a full mission win.
- The full journey harness now checks visible Stop ARIA, no subsequent assistant actions, continued mission time, restored Play and restart. Its start/stop setup uses visible button events; tactical selection and mission actions use ARIA's synthetic touch path. This does not claim a physical-device interruption test.
- Withdrawal confirmation now keeps the existing Pause open until confirmation, rather than resuming combat beneath a decision modal. Native lifecycle validation is pending.
- Root `AGENTS.md` now records the approved mockup-before-implementation workflow, visible ARIA control requirement and distinct readiness evidence levels for future mission work.

### Full journey and localized presentation

- `evidence/victory-02/validation.log`: **Passed**, Regular seed 1102, English, visible-touch tactical input, full Victory and return, 272.6 mission seconds, 16 survivors, 12 infantry in extraction when Victory triggered, saved result and AP=2. Visible button-event Stop/Play check passed during this journey. No health/position/order or terminal-outcome injection was used. This run precedes the later text-spacing/tour-marker polish.
- `evidence/fa-01/validation.log`: failed because the test compared shaped Persian Stop text against an unshaped string. The visible native button existed; the harness now finds that same public button and checks its active/stopped state before clicking it.
- `evidence/fa-02/validation.log`: failed the new rendered text overflow check after increasing Persian readability. The extraction row was enlarged to retain its full copy. Arabic metric margins and bounded font sizes are scoped to this mission view; Campaign/Skirmish typography is unchanged.
- Signal range rings and target labels are now visible during their paused camera-tour segment. Channel labels have extra height for their two-line status. These polish changes require the later native layout captures, not the earlier victory screenshots, for visual evidence.

### Additional verified states

- `evidence/fa-03/validation.log`: **Passed**, Regular seed 9102, Persian, 273.2 mission seconds, 16 survivors, 13 extracted, saved result, return and AP=2. The native text overflow checks and visible ARIA stop/restart check passed. Later copy changes clarify the mixed-digit scan counter without changing mission behavior.
- `evidence/wide-fa-01/validation.log`: **Passed**, 4800×2160 (20:9) native Persian briefing, all camera-tour destinations, persistent tracker, objective focus and squad return. The final counter reads “0 out of 3” naturally rather than a reversed mixed-script fraction. This is Editor aspect-ratio evidence, not a phone test.
- `evidence/lifecycle-01/validation.log`: **Passed**, deploy → open withdrawal → cancel while time stays frozen → withdraw → saved result/return → redeploy with the expected AP and finite roster. The confirmation capture exposed obsolete copy saying the mission continued underneath the modal; the source/catalog now say only that withdrawal ends the attempt and applies district consequences.
- `evidence/checkpoint-save-01/validation.log`: failed in the harness after its own deliberate Stop ARIA. Saving now has a separate harness stage so an intentional stop is not classified as a gameplay failure. Fresh-process checkpoint validation remains pending until both save and restore runs pass.

At this point in the historical sequence, a new human-controlled win, the five-seed matrix, and player/device acceptance were still pending. The later matrix results below supersede the matrix status; human/device acceptance remains pending.


### Fresh-process checkpoint and manual input follow-up

- `evidence/checkpoint-save-02/validation.log` and `evidence/checkpoint-restore-01/validation.log`: both passed. A scan saved at 2.004 seconds resumed in a fresh Editor process with the same session and AP=2, through the native recap and normal handoff.
- A normal macOS mouse check found the deployment action extending below the DailyBriefing card and behind Active Warnings. Earlier setup used `Button.onClick` and therefore did not prove reachability. The menu now fits the existing 252-point area, hides the redundant Deploy action while Resume is available, and the formal button helper additionally checks screen bounds and the actual EventSystem hit at the button center.
- The corrected menu was deployed successfully with a normal mouse click in the original Editor on an isolated temporary profile. Briefing, Show the plan, squad selection, Show objective and Attack-ground movement into the visible ring were exercised through the UI. No position, order, health, timer or outcome injection was used. This is an agent-operated mouse check, not an unfamiliar human playtest.


### Mouse-run outcome and cleanup

`evidence/manual-mouse-01/notes.md` records the agent-operated normal mouse journey. All three scans and evidence recovery succeeded. The deadline arrived while returning to extraction: the native result was **Partial Success, 3/3 signals, 2 extracted**, then Continue returned to Operations. This does not satisfy the full human-controlled Victory gate. Inspection delays and computer-control timeouts consumed mission time; no timer or gameplay state was changed to force a success. The full original Editor log and the isolated profile evidence are retained. The temporary save-root override was verified cleared (`saveRoot=null`) and original Editor Play mode was verified stopped.

The missing completed-site checkmark is replaced by the already-localized DONE label. This and the deployment-card correction are presentation-only changes after the full-win matrix candidate; the later final UI runs validate their native rendering and click reachability.

## Remaining acceptance scope

- The owner has approved the mockup direction and explicitly required visible ARIA Play / Stop. The implemented native screens still need the owner's usability judgment.
- A campaign-familiar human's full Victory, unfamiliar-player comprehension, and an actual phone touch/performance run have not been observed. Editor aspect ratios and automated wins do not replace those checks.
- No full Campaign or Skirmish playthrough was rerun. The affected shared camera, scan, faction targeting, attack picking and pause fixtures passed 76 focused tests.
- Reduced-motion uses the shared explicit-camera cut path; safe-area anchors are implemented, but a physical notched-device run has not been performed.
- This work is local and uncommitted. No release build, deployment, merge or publication has been performed.


### Regular robustness matrix

All five wrapper runs passed full Victory, saved return, AP=2, and the visible-button-event Stop/Play interruption check. Tactical orders were issued through visible synthetic touch controls; deployment/start/stop/result setup used button events. Native-input isolation was enabled only in the disposable validation Editor.

| Seed | Mission seconds | Survivors | Extracted at Victory |
|---|---:|---:|---:|
| 1102 | 274.5 | 16 | 10 |
| 2102 | 274.9 | 16 | 9 |
| 3102 | 281.0 | 16 | 9 |
| 4102 | 293.0 | 16 | 9 |
| 5102 | 277.3 | 16 | 13 |

Full logs, wrapper exits and native images are under `evidence/regular-matrix/<seed>/`; the machine-readable summary is `evidence/regular-matrix/results.json`. The matrix uses the final gameplay implementation before the later deployment-card fit and DONE-label corrections. Those presentation changes are covered by the final UI checks.


### Final native control reachability checks

- `evidence/final-first-scan-03/validation.log`: wrapper exit 0, first scan passed, Deploy reachable through the EventSystem, and `ariaStopResume=Passed input=visible-button-events missionContinued=1`. The helper checks full button bounds and the first raycast hit before invoking each setup/control event. Fresh native captures show green ARIA PLAY after stopping, red STOP ARIA after restarting, and the completed-site DONE label.
- The retained `final-first-scan` and `final-first-scan-02` failures were validation coordinate bugs: a nested Canvas, then `Screen.width` queried in an Editor update, disagreed with the fixed Game view. The helper now checks the root Canvas and camera pixel rectangle (or Game view size for overlays). The bounds and raycast assertions remain enforced; the actual mouse Deploy check and native menu capture independently corroborate reachability.

- `evidence/final-wide-fa-03` exposed the remaining Editor-context raycast mismatch at the visible briefing button. `GraphicRaycaster` internally reads Screen dimensions, so the harness now ticks once per rendered game frame through `Canvas.willRenderCanvases`, with recursion protection. It retains full bounds and first-hit checks.
- **Final reruns passed:** `evidence/final-first-scan-04` (English, 1920×1080, first Scan, visible Play/Stop/Play and continued mission time) and `evidence/final-wide-fa-04` (Persian, 4800×2160, deployment, briefing, tour, objective focus and squad return). Both wrappers exited 0 and recorded reachable Deploy. Native dashboard, briefing, tracker and ARIA control captures were visually inspected.
- Final candidate identity is recorded in `candidate-files.sha256` (28 source/asset files against the baseline above). Non-generated source passes `git diff --check`; Unity's localization asset serialization retains wrapped-string trailing spaces. The candidate remains local and uncommitted.

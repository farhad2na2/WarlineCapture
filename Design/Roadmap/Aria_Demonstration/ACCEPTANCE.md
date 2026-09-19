# ARIA demonstration acceptance gates

Status: **partial implementation evidence available**, 2026-09-19. This remains the required release test plan; see [actual implementation status](IMPLEMENTATION_STATUS.md) for the ten guided campaign baseline wins and the still-open gates. Apply [PLAN.md](PLAN.md), [COVERAGE.md](COVERAGE.md) and the repository's Unity execution rules.

## Release criteria

- Every currently playable match/ruleset has an enumerated, versioned coverage record. Unknown routes and uncovered playable rows block an all-mode readiness claim.
- All meaningful ARIA-caused game actions have a corresponding input trace through the normal touch route. No direct mutation fallback exists.
- Every Stop/handover test prevents further synthetic gameplay input and accidental release actions; no interruption is allowed to depend on successful planning.
- Full normal-speed Campaign and free-match evidence demonstrates actual objectives/results, readable EN/FA presentation and correct audio lifecycle.
- Manual play remains correct with Watch disabled and after handback. Device and teaching evidence are reported separately from Editor logic passes.

## Input, lifecycle and perception matrix

| ID | Scenario | Required outcome |
|---|---|---|
| G01 | Start, Cancel and rapid repeated Start | Only genuine confirmation starts; one session; no pre-confirmation gesture or duplicate executor |
| G02 | Stop during aim, press, hold, drag, pinch, release boundary, wait and narration | Generation invalidated; no later synthetic action; active contacts cancelled without committing the pending action |
| G03 | Physical touch arrives with a synthetic sample; real Stop while a virtual touchscreen is current | Physical control wins; no self-override from synthetic events; first handover contact consumed, next touch manual |
| G04 | Stop under Build, production, command wheel, exchange and transport panels | One reachable control, correct hit test, no world click-through or covered confirmation |
| G05 | Result/loss/scene exit/background/pause while touching; old planner result returns | Contacts/audio cleared; delayed work rejected; no result-button tap, auto-replay or auto-resume |
| G06 | Open menus, UI scale/safe-area/aspect change, camera move and target motion after planning | Re-observe/re-resolve bounds; cancel unsafe trajectory; never use obsolete screen coordinates |
| G07 | Hidden/disabled/offscreen/occluded control or object; target dies mid-gesture | No secret interaction; inspect/navigate/replan or hand back with a reason |
| G08 | Fog/last-known enemy, minimap-only contact, hidden mission trigger | Only currently public information exposed; no live hidden coordinates or prediction from private spawn schedules |
| G09 | Synthetic attempt to press consent, Stop, account purchase, surrender or next match | Session policy rejects it; cannot expand its own authority |
| G10 | Physical versus synthetic identical taps/drags/pinches | Same hit tests, selection thresholds, camera/placement behavior and UI/world suppression |
| G11 | Rejected order, insufficient resources, stalled route, full transport, lost producer | Visible error understood; bounded correction; no spending twice, blind loop or phantom success |
| G12 | Known timer versus no visible timer | Honest progress/reason; no fake ETA, repeated instruction or unexplained indefinite wait |
| G13 | Forced direct-command dependency/use from planner/skill assemblies | Architecture validation fails; tests do not whitelist legacy mutation just to pass |
| G14 | Stop/restart repeatedly and load a saved match | No stale contacts/listeners/devices; fresh observation/confirmation; active ownership never restored |
| G15 | Legacy takeover or player-auto-AI is present in the setup | No second hidden faction controller; normal unit autonomy remains unchanged; explicit ownership/support disposition and honest action attribution |

Stop timing target: no new ARIA gameplay input **after the next input-processing opportunity that receives Stop**, with physical input processed first. At a stable supported frame rate, target p95 visible ownership handback below 100 ms. Measure actual timestamps; a main-thread stall is a separate performance failure, never evidence of instantaneous wall-clock cancellation. Contact cancellation is allowed and required after stop; clicks/orders are not. Decorative withdrawal and narration fade cannot delay input cancellation.

Verify hand contact position within 2 logical UI pixels of the submitted input position after coordinate conversion at test resolutions; it must remain inside the actual target hit region. Any duplicate command, accepted action after stop, hidden-information leak or direct actuation is a release blocker, regardless of statistical rates.

## Gameplay matrix and run counts

Initial certification sample is a minimum, not a statistical guarantee. Pin builds and seed/start conditions in every record. Failed runs remain in the report; do not silently discard seeds.

| Coverage | Minimum planned evidence |
|---|---|
| M1–M5 | One uninterrupted normal-speed full win per language per mission (10 baseline runs); two additional varied runs per mission covering changed camera/layout and a recoverable mid-match start; at least one loss and one handback/restart case per mission |
| Campaign variations | Both first-play and replay represented for every mission; exercise tutorial off where supported; include open panels, partially completed goals, underway orders and existing saved progress |
| S01 current Base Assault | At least 10 pinned seeds, each in EN and FA; every publicly supported setup option covered plus a documented pairwise interaction matrix; include win/loss/draw/time limit and mid-match recovery fixtures |
| Each playable Operations ruleset | At least five pinned scenario/start variants per language, full success plus failure/timeout paths where defined; verify real Operations launch and return/result consequences |
| Input failure fixtures | G01–G15 across representative UI/world gestures in both languages and supported layouts; expand when a new handler or modal is added |

Campaign baseline and recoverable authored variants must complete without test-side intervention. Loss fixtures must correctly acknowledge loss rather than modify the outcome. Initial Skirmish Normal strategy target is at least 80% wins over the pinned 10-seed sample per language, with no UI/automation deadlocks; report actual counts and classify defeats. This is a proposed product gate to tune from A0/A5 evidence, not a current success claim. If the target is missed, improve strategy or record the unmet gate; do not alter opponent difficulty, speed or rewards covertly. Other difficulties report separate rates with their supported teaching expectation.

Fixtures may create difficult states before control begins, but their setup must be disclosed and the planner receives only normal observations. At least the baseline runs launch through the public menu/story path using an isolated profile, with normal clock, economy, combat and delivery. Record start of automation separately from setup/story time. A trace of taps without world-result verification is insufficient.

## Specific regression obligations

| Flow | Required observation |
|---|---|
| M2 ending | Fourth recruitment/delivery completes; no repeated completed Build lesson before ending comic/result |
| M3 gate | Preview is on the road and visible; final gate retains confirmed transform; no sidewalk/mountain placement |
| M3 defense | Hold serves the objective; Scan has relevant visible feedback; convoy progress/waits clear; no removed Stop instruction or unexpected jet camera focus |
| M4 extraction | Four specialists selected together despite aircraft overlap; manifests correct; clear landing/security/departure progress |
| M5 breach | Gate between walls is attackable through normal picking; radar destruction removes live marker immediately; archive wait understandable |
| Skirmish | Buildings selectable, valid placement only, recruitment delivery visible without camera lock, exchange uses real balances |
| Guidance/layout | Show Me/yellow guide state recovers correctly on handback; no stale highlight, clipped ARIA text, flashing alternate panel or duplicate old buttons |
| Audio/story | Correct active language; no stale coach/unit/mission voice after result; no skipped/repeated story caused by queued touches |

## Visual, mobile and learning review

Review 16:9, 20:9 and tablet landscape layouts, safe-area variations, smallest supported phone and larger text settings supported by the game. Stop and confirmation remain reachable; the hand never hides the information needed to understand a gesture. Verify English and actual shaped Farsi text with correct glyphs/RTL; do not infer from translation keys. Record device identity and effective touch target dimensions.

Test tap, hold/rectangle, placement drag, camera movement and pinch with motion normal/reduced and audio on/off. Have at least three unfamiliar players watch representative selection, placement/recruitment and transport demonstrations, then perform those actions independently. Record completion, confusion, mistaken taps and readability feedback. All three must be able to find Stop immediately; any consistent confusion requires another iteration. This small review is a usability gate, not broad audience proof.

## Performance and platform gates

Use [performance authority](../../Architecture/performance_regression_contract.md) and its accepted baseline JSON. Compare the same scenario/build/device with Watch off/on. Measure frame p95/p99, input/Stop latency, observation/planning time, GC, memory, hand rendering and audio, including dense combat. No weakening existing budgets to make ARIA pass.

Initial engineering allocation for ARIA: combined observation/planning/input CPU work p95 at or below 1 ms per rendered frame on the baseline device, measured with amortized work and separate worst-frame reporting; no recurring managed allocation after warmup from new steady-state paths. This is a target to validate, not a measured result. Bind large-battle work to the actual certified Skirmish counts; hundreds-of-unit support remains pending until that expansion exists and is measured. Do not drop visible targets silently to meet a budget.

Editor checks prove logic and visible desktop behavior. Android development validates physical/synthetic touch coexistence and profiling. An exact Android release candidate validates player/device acceptance. If device testing is deferred, label the result internal Editor-ready and keep the release gate open. Offline operation is required for the initial local planner.

## Evidence format and sign-off

For each run keep: code/config/manifest hashes; platform/device/build kind; locale/layout; public launch route; coverage ID/seed/start state; normal versus fixture setup; start/stop/outcome timestamps; expected/actual visible results; gestures/retries/no-progress events; performance sample; video/screenshots/audio-review notes; failure classification; reviewer.

Store implementation evidence under `Design/AgentReports/AriaDemonstration/` when work begins. Use compact bounded traces; release players do not need permanent recording or data upload. No cloud telemetry or account audio is transmitted by this feature plan. Stop/result/safe handback and assisted metadata are local behaviors.

Sign-off requires updated coverage records and a completion report that separates implementation, Editor, device and player review. Keep known content dependencies and unsupported future modes visible. Historical direct-command probes, source inspection and this planning package cannot substitute for the new touch-only gameplay evidence.

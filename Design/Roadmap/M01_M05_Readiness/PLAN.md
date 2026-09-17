# M1–M5 reliability and mobile experience plan

Started 2026-09-16. Updated 2026-09-17. Status: internal gameplay/recovery verification complete; Android candidate built and verified; external device/player gates open. Parent: [roadmap](../README.md).

Execution checklist: [mission-by-mission acceptance cases](MISSION_CASES.md). Observations: [initial gameplay assessment](PLAYTEST_NOTES.md).

## Acceptance contract

1. Every required action is available, understandable and produces visible feedback. Guidance identifies the current action and follows state changes; it does not repeat an already completed click.
2. Every wait names what is happening, shows useful progress, and says whether the player should act or wait. Cooldown is not confused with mission countdown.
3. Preview, accepted command, simulation result, rendered result and next instruction agree. Inspect after placement, boarding, unloading, movement, attack and destruction.
4. A player can recover from wrong targets, incomplete selection, cancellation, interruption and retry without resetting the application or needing instructions from the developer.
5. Essential touch controls are legible, generously sized, spaced, within safe areas and consistent in enabled/disabled states. Prefer existing tap-based squad actions where supported; do not indicate a tap for a drag gesture.
6. Camera starts close enough to understand play, frames relevant units/targets, leaves player control intact, and keeps placement clear of HUD occlusion. No unexplained jumps or return-camera button.
7. English and Farsi have equivalent meaning and functionality. Farsi layout, wrapping, voice language and conversational tone must agree with current commands; no obsolete Stop instructions.
8. Results explain success/failure, settle the correct reward once, and offer working retry/continue routes. The next mission starts with clean state.

## Protect the existing experience

- Use current command semantics and HUD style. Do not add new gameplay buttons to solve mission-specific teaching problems.
- Keep enemy challenge, mission objectives, supported optional routes and player agency unless observed evidence requires a documented adjustment.
- Fix one cause at a time. Recheck the initiating action, visible result and next action; exercise cancellation and repetition when relevant.
- Record dirty-worktree baseline and validate in an isolated project/profile. Do not overwrite unrelated user changes.
- Do not equate hidden automation commands, direct state mutation, or a screenshot alone with playable acceptance.

## Work packages

| ID | Package | Concrete work | Done when |
|---|---|---|---|
| R0 | Baseline and QA credibility | Inventory current mission flows, existing probes and known regressions; distinguish current versus obsolete test paths; record source versions and setup | Replays use current player controls or are explicitly labeled lower-level checks |
| R1 | Shared interaction reliability | Validate selection, commands, placement, build close, guidance visibility, camera ownership and target death | Action → world result → displayed feedback checks pass across affected missions |
| R2 | M1: First Contact | Fresh/campaign entry; first usable instructions; select defenders, move, attack, objective and results; interruption/retry | A new player can finish with the displayed help; no unexplained action, empty ARIA or off-screen required target |
| R3 | M2: Establish Base | Open Build, category/card, placement, confirm, construction, select producer, recruitment, material lesson and results | Preview matches built object; production uses visible controls, drawer closes; waits and resource costs agree |
| R4 | M3: Radar Warning | Mission purpose and incoming route, optional defense, road gate, group move/Hold, warning/Scan feedback, two-wave defense and results | No mandatory redundant Stop/Scan sequence; gate remains across the road; warnings readable and timed states explained |
| R5 | M4: Airlift | Four specialists selected by intended group action, APC boarding, drive/unload, helicopter boarding, landing-zone hold, departure | Helicopter overlap cannot trap selection; each transfer completes; wait/clearance/recovery are understandable |
| R6 | M5: Breach Assault | Comic/gameplay coherence, force selection, attack gate, enter compound, destroy radar, secure archive, results | Marked targets attackable; dead targets lose cues immediately; securing progress and any interruption explain the required response |
| R7 | Localization and layout | Same flow EN/FA; narrow landscape and wide landscape; text lengths, stacked ARIA actions, guidance safe-area behavior, meaningful disabled states | No clipping, hidden required button, obsolete voice instruction or language fallback found in sampled journeys |
| R8 | Recovery and persistence | Pause/resume, guide close, wrong target, repeated taps, canceled construction, defeat/retry, victory/continue, cross-mission cleanup | No stale marker, selection, tutorial, pause state or duplicate reward; re-entry starts as intended |
| R9 | Device and independent play | Test on available representative phones; 3–5 unfamiliar players try opening missions without coaching | Document device performance/touch behavior and observed confusion; fix blocking findings before expansion |

## Mission-by-mission procedure

For each mission, start with a fresh isolated attempt and write down the objective from the visible instruction. Follow the offered control. At every step capture: displayed text, expected action, enabled control/indicator, command acceptance, actual world change, resulting UI and next instruction. Do not repair gameplay by changing facts in the test.

Exercise a wrong or repeated action at high-risk transitions: choose an invalid target, cancel and reopen Build, change selection, open/close the field guide, interrupt with pause. Verify helpful feedback and recovery. Finish through the result screen and leave through its actual primary action. Then replay affected paths in the other language.

Use normal speed for judging pacing/readability. Accelerated simulation can supplement mechanical combat coverage but cannot validate pacing or the time a player has to read. Skipping comics does not validate comic playback. A mission unlocked in an isolated QA profile does not validate fresh-profile campaign progression.

## Evidence and priority

P0: crash, lost progress, progression dead end, impossible required action.
P1: action differs from preview/instruction, incorrect target/selection/camera, unreadable or inaccessible required control, missing wait explanation.
P2: friction or inconsistency with a working recovery path.
P3: optional decorative improvements; defer unless cheap and clearly safe.

Store exact run commands, source hashes, pass/fail markers and observation notes. Use the [verification matrix](VERIFICATION.md) for coverage, not an undifferentiated “QA passed.” Fixes close only their evidenced scenarios. Preserve failed-run evidence explaining newly caught regressions.

## Exit gate

All P0/P1 findings closed with relevant regression evidence; current EN/FA M1–M5 journeys complete; important recovery routes exercised; UI reviewed at supported landscape sizes; target-device and external-player gates explicitly completed or reported as outstanding. Then prepare the candidate build and decide whether to proceed to skirmish. No new mission chapter, reward overhaul or unrelated mode is part of this active milestone.

## Completion status

R0–R8 have current internal evidence: both-language normal-speed routes, genuine first-clear save/unlock chains, deliberate cross-mission replay, recovery actions, focused regression and rendered/audio checks. The final focused set contains 269 distinct passing tests. M4 additionally passed clearance interruption/re-entry, actual combat defeat and clean Retry; M5 passed actual deadline defeat and clean Retry. Those accelerated failure checks validate mechanics only; normal route pacing was checked at time scale 1.

See VERIFICATION.md for exact scope and retained failed runs. Not every possible player action or device condition has been exhausted. The current runtime has no unresolved reproduced progression or required-action defect from this pass. The Android candidate built successfully and passed package, signature and archive-integrity checks.

R9 remains external: there is no connected Android device, and no unfamiliar-player sessions have been conducted. The next gate is to install the candidate on representative phones and check real touch input, safe areas, frame pacing, repeated-scene memory and thermal behavior. Then observe 3–5 new players without coaching, especially at M3's defense wait and M4's transport handoff.

Do not call the Editor pass a device-performance or universal “bug-free” guarantee. Occasional frame hitches in the concurrent Editor session are recorded as a reason to retain the phone-performance gate. No new mission, reward overhaul or unrelated mode was added to this milestone.

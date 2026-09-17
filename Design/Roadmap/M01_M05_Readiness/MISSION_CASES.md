# Mission acceptance cases

Status: pending unless linked evidence in [VERIFICATION.md](VERIFICATION.md) says otherwise. Run each normal route in English and Farsi. These cases preserve the current mission design; a design change needs a recorded gameplay reason.

## Shared checks on every route

At each instruction, answer from the screen: What is my objective? What do I do now? Where? What tells me it worked? If nothing needs clicking, what am I waiting for?

Record the current instruction, cue, accepted player action, world result and next instruction. A button-handler call alone cannot pass the case. Capture the rendered state before acting and after the result. Use normal simulation speed. Inspect both languages at the current landscape size; repeat high-risk layouts at the narrower supported size.

## M1 — First Contact

| Case | Player route and expected result | Recovery / alternate check |
|---|---|---|
| M1-01 | Enter from campaign; current interactive opening and camera settle; readable ARIA objective and usable first selection cue appear (legacy M1 comic assets are not enabled in this flow) | Completed profile with assistant Off; return after M4/M5; no blank intro or inherited transport logic |
| M1-02 | Tap the guided mission-group action; intended friendly group is selected and visibly acknowledged; cue advances to Move | Partial/manual selection, repeat tap; do not imply a world tap performs rectangle selection |
| M1-03 | Press Move, then the indicated destination; units travel to reachable cover; instruction describes travel and advances on arrival | Wrong/blocked destination; camera moved away; Show Me restores a useful target |
| M1-04 | Select the intended force, press Attack, tap a live marked hostile; damage and death are visible; dead-target cues disappear promptly | Friendly/ground target rejection, changed selection, already-dead target |
| M1-05 | Complete combat/finale, read result and use its primary action; campaign progression and rewards are correct | Pause/resume, failed attempt/retry, repeated result tap and replay reward isolation |

## M2 — Establish Base

| Case | Player route and expected result | Recovery / alternate check |
|---|---|---|
| M2-01 | Opening leads to Build; each cue points to the next required tab/card/action, skipping an already selected tab | Re-enter after another mission, assistant Off, manual choice before Show Me |
| M2-02 | Choose the required barracks and Place; preview is centered in unobscured gameplay space, model and footprint agree | Close/reopen drawer; drag while camera stays stable; rotate; cancel and resume |
| M2-03 | Confirm a valid footprint; actual building keeps preview origin/rotation; construction and resource change agree with the displayed cost | Road/overlap rejection; rapid Confirm taps cannot double-place or double-charge |
| M2-04 | Materials lesson has usable Continue; tap hides it and changes state; construction wait identifies what is happening | Tap twice; wait for construction before/after Continue; no stale Credits teaching |
| M2-05 | Follow visible producer/category/soldier/Build controls; recruit the intended group; drawer closes after acceptance; queue/spawn acknowledged | Insufficient resources, busy queue, cancellation where supported; never rely on hidden Do It |
| M2-06 | Recruitment completes the construction mission; results list the Barracks and trained squad, with no invented patrol victory | Camera motion around the helicopter must not cause material flashing; return/retry state clean |

## M3 — Radar Warning

| Case | Player route and expected result | Recovery / alternate check |
|---|---|---|
| M3-01 | Opening explains the defended location, incoming route, and what failure/success mean; readable warning has useful feedback | No unrelated jet focus or return-camera control; warning survives locale/layout changes |
| M3-02 | Optional defense choice is explained; choose a structure or skip the optional lesson; player is not forced to place every item | Cancel/reopen without losing instruction; optional production stays optional |
| M3-03 | Road gate preview spans the drivable road with appropriate rotation; Confirm creates the same pose; ordinary buildings remain off roads | Default preview and moved/rotated preview; reject sidewalks, median, walls and overlaps; compare completed model after the next lesson |
| M3-04 | Select defenders, Move to the defended route, then Hold; acknowledgments and travel/arrival states are explicit | Wrong selection/target; no redundant removed Stop action or stale Stop voice |
| M3-05 | Warning/Scan teaching explains the tactical purpose; using the existing Scan control visibly confirms information or explains unavailability | Cooldown/charge feedback; repeat tap; no extra radar command or silent click |
| M3-06 | Defend against both waves with a clear next action or named wait; contact timer and ability cooldown have distinct meanings | Move away from objective and recover with Show Me; change selection during countdown; optional reinforcements |
| M3-07 | Result reflects defended objective and actual losses; campaign return/retry resets targets, building state and tutorial | Replay after M4; no stale extraction selection, indicators or camera requests |

## M4 — Airlift

| Case | Player route and expected result | Recovery / alternate check |
|---|---|---|
| M4-01 | Opening explains rescuing all four specialists via APC then helicopter, deadline and optional escort objective | Continue is visible/clickable once; current Farsi comic voice and body copy agree |
| M4-02 | Select APC, approach pickup, select specialists with the dedicated group action; only intended passengers are selected | Partial selection; overlapping APC/guards; active Select cue must clear after selection |
| M4-03 | Use Commands → Board and tap APC; four passengers board with readable count; drive to landing area and unload | Wrong/full transport, repeated tap, pause during boarding; no legacy Board All / Cancel overlay |
| M4-04 | Select helicopter and land; reselect all specialists even while overlapping helicopter/APC; board helicopter | Exclude vehicles from passenger group; count tracks boarding and target becomes usable |
| M4-05 | Hold the loaded helicopter in the landing area; explain the 20-second clearance and why it pauses/resets | Enemy enters zone or helicopter leaves; Show Me only when useful; no duplicate guide action |
| M4-06 | After clearance, guide departure; complete extraction and results with all required people alive | Missed passenger, interrupted departure, defeat/retry and campaign return |

## M5 — Breach Assault

| Case | Player route and expected result | Recovery / alternate check |
|---|---|---|
| M5-01 | Comic and map show the same compound-breach premise; opening explains gate, radar and archive objectives | Re-entry after M4; no stale transport selection routing |
| M5-02 | Select assault force, Attack then tap the guided gate on screen; damage destroys the actual barrier and opens the route | Tap mesh/marker/edge; reject unrelated target with useful feedback; do not supply a target entity from test code |
| M5-03 | Enter compound and attack the radar; model footprint matches its size; destruction clears marker immediately and shows authored wreck where available | Target dies while selected/guided; no persistent red marker over empty space |
| M5-04 | Respond to defenders/reinforcements; instructions identify any remaining threat and useful movement/attack | Changed force, wrong target, offscreen objective, pause/resume |
| M5-05 | Enter archive zone and secure it; named progress shows remaining time and explains interruption/reinforcements | Empty/contested area and re-entry; no unexplained wait with only a field-guide button |
| M5-06 | Victory settles once, result action works, replay begins with fresh actors/objectives/tutorial | Full campaign return, replay reward isolation, defeat/retry |

## Final regression sequence

1. Normal first-clear M1 → M2 → M3 → M4 → M5, including result-to-next-mission routes.
2. Replay M4 → M1 → M3 → M5 → M2 to expose persistent mission-state contamination.
3. Repeat high-risk actions in Farsi; include long text and narrow landscape.
4. Repeat on available target phones, then observe 3–5 unfamiliar players without coaching. Log hesitation/wrong-action patterns as UX findings rather than assuming completion alone means clarity.

Device and unfamiliar-player checks remain explicit external evidence gates. Current Editor replays can close only the cases they actually exercise.

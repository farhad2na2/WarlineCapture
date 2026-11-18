# Owner packet — early prototype play (2026-11-17)

You asked to play the first prototype of each objective type early (owner sampling: recorded separately, never an
automated gate, and it does not replace gates 1–4). Each entry is added when that prototype starts from the real menu
and can be played to a result with normal input.

## How to open (Mac, `/Volumes/T7/Projects/WarlineCapture`; owner choice 2026-11-17)
The Mac checkout is on `missions/integration`. Agents keep using Unity on this project until you say you are ready to
test: then the program lead finishes or stops the current agent run cleanly, leaves the Editor compiled on the
candidate commit, marks the lane as held for you, and starts no agent Unity run until you say you are done.
1. Open Unity Hub and open the project `/Volumes/T7/Projects/WarlineCapture` (Unity 6000.5.8f1; existing Library, no rebuild).
2. Open the scene `Assets/Game/Scenes/Menu.unity` and press Play, then follow the menu path given per mission.
3. In the match, switch ARIA off (the ARIA Play/Stop control in the match HUD) so you play yourself.
4. When you are done, tell the program lead (agent runs resume after that).

## 1. Base Assault — S002 (Desert Base, Ground Maneuver, Established Base)
- **Commit:** the current `missions/integration` head on the Mac (contains `e8ad30c10`).
- **Menu path** (the one the automated menu journey used, lane14/qa:371735): Main menu → Skirmish hotspot → Battle Library
  (scroll to "Desert Base · Base Assault · Ground Maneuver · Established Base") → that mission card → Launch Mission
  (Standard size, Regular difficulty are the defaults). (On a fresh profile the Skirmish entry appears only after Campaign
  Chapter 1.)
- **What the automated runs show:** ARIA wins most runs in about 230 s of match time (about 21% of the 18-minute deadline),
  through the real menu (`program-logs/2026-11-17/lane14/qa`). It uses every unit type the mission offers.
- **Known unfinished:** one seed (155923) still loses at about 96 s when the enemy's lone tank slips past the army and
  shells the base; a guard tower was added at the player base today (owner decision) and the cause is being reproduced.
  Deadline unchanged (18 minutes). Not yet accepted.
- **Please judge:** is the mission's purpose clear from the start; are the objectives readable on screen; does the pacing
  feel right for you (a human is expected at roughly 1.5–2× ARIA's time); is it fair (e.g. can you stop the lone tank).

## 1b. Base Assault on a second map — S026 (City Crossroads, Ground Maneuver, Established Base)
- **Commit:** `51cd502bb` (made visible in the real catalog for internal acceptance only).
- **Menu path:** Main menu → Skirmish hotspot → Battle Library (scroll to the City Crossroads Base Assault card) → Launch Mission.
- **Known:** ARIA wins it in about 150 s of match time (14% of the deadline) — faster than the 20% target, so the enemy is
  probably too weak; some unit types (drone, heavy APC, transport helicopter, siege) are not yet used by ARIA.
- **Please judge:** same questions as S002; also whether City Crossroads feels too easy.

## 2. Frontline Control — S007 (Desert Base, Ground Maneuver, Field Base): starts from the menu (2026-11-18, lane 22)
- **Commit:** `missions/integration` from `68be965d4` (S007 re-authored from the measured draft r2; visible internally since `8ab8f5eeb`, not published; publication row stays InProgress).
- **What was fixed:** lane 20's launch aborted `BlockedSpawn` (draft copied from an old S006 example). The draft now uses the measured legal base, side and per-building pads; re-measured (`Design/AgentReports/SkirmishExpansion/S007/area-measurement-r2.json`), authored (168 measured instance pads, 0 derived), pad containment Passed.
- **Menu path** (automated menu journey, lane22/s007menu/unity.log:2324): Main menu → Skirmish hotspot → Back → Desert Base theater pin → Start Battle → **FC** objective tab → the S007 card → Start Battle (Standard / Regular). The card is a validation card (internal).
- **What the automated launch shows:** menu journey Passed, the match starts with no startup failure and no building-placement failure; both sides spawn (8 riflemen, 4 rocketeers, armoured APC, car, truck, tanker, watchtower each). ARIA then stops at 8.6 s (`[AriaBlocked] reason=101 intent=Recruit`): ARIA has only a Base Assault skill, so it cannot play this objective. **Nobody has played S007 to a result yet**; that is your play.
- **Known unfinished:** see section 5 (common) plus: zone/ticket HUD and enemy zone allocation are unobserved in a native match; deadline is the packaged 1200/1500/1800 s by size.

## 3. Breakthrough — S013 (Desert Base, Ground Maneuver, Field Base): starts from the menu (2026-11-18, lane 22)
- **Commit:** `d487e0bf0` (authored) + `b3285b36c` (visible internally, not published).
- **What was fixed:** lane 20's authoring failed on the DefenseApproach pad; the enemy defender towers now reuse the typed `defense.approach.a/b` pads (e4aa8e42b). Re-measured (`S013/area-measurement-r2.json`: both corridors and the exit reachable for infantry, both tower pads fit), authored (192 instance rows: 168 measured + 24 defender role pads), pad containment Passed.
- **Menu path** (lane22/s013menu/unity.log:2163): Main menu → Skirmish hotspot → **BT** objective tab → the S013 card → Start Battle.
- **What the automated launch shows:** menu journey Passed, clean start, both sides spawn (player 12 riflemen + APC, car, truck, tanker). ARIA stops at 8.4 s (same `reason=101 intent=Recruit`). The two defender towers at the approach pads are outside the start trace's 90 m radius, so their spawn is not shown in the log; check them in play. Not played to a result.
- **Known unfinished:** end reasons `BreakthroughEvacuated` / `BreakthroughImpossible` show the raw-name fallback; enemy defence policy unobserved; attacker advantage not tuned (packaged matrix row).

## 4. Convoy Escort — S019 (Desert Base, Ground Maneuver, Field Base): starts from the menu (2026-11-18, lane 22)
- **Commit:** `3e57302a9` (authored) + `38c0405fc` (visible internally, not published).
- **Done:** convoy logical assets verified (`created=0`), re-measured (`S019/area-measurement-r2.json`: destination reachable for trucks, the three bays fit the 3x3 truck), authored (192 instance rows), pad containment Passed.
- **Menu path** (lane22/s019menu/unity.log:2303): Main menu → Skirmish hotspot → **CE** objective tab → the S019 card → Start Battle.
- **What the automated launch shows:** menu journey Passed, clean start, the player has the three convoy trucks (`ConvoyLogic:3`) plus the usual starting army. ARIA stops at 8.5 s (same `reason=101`). Not played to a result.
- **Known unfinished:** the deadline is the fixed size deadline, not a route-time deadline; truck commands, repair and enemy interdiction are unobserved; route native path queries failed on the middle segments (flood fill reached them).

## 5. Common to S007 / S013 / S019 (what you will see that is unfinished)
- Result text falls back to raw end-reason names and the neutral headline `MATCH COMPLETE` (copy keys not in the catalog).
- No Save & Exit (the pause menu shows RESTART MATCH).
- Menu/briefing copy is catalog fallback text; no mission-specific purpose copy.
- ARIA cannot play these objectives (switch it off and play yourself); no automated result/return evidence exists for them.

Your notes: write them in this file or tell the program lead; they are recorded as owner sampling in `missions.csv`
(`owner_manual_win` and notes), never as a gate.

The [independent designer playtest and prioritized development plan](2026-11-17-s002-s026-designer-playtest.md) records the agent-operated Mac Editor sessions separately. The owner's personal playtest is still open.

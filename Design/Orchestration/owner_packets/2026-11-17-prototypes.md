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

## 2. Frontline Control — not ready to play (2026-11-18: areas measured, authoring blocked)
S007 area measurement Completed (lane18/s007/measure.log; all three areas inside the playable window, infantry reachable from both bases); draft passes check_draft/pad_preflight. `AuthorFromDraft` FAILED: `compile.Standard.Recruit MissingReference:missionLogicalBinding` (lane18/s007/author.log:679). The authoring entry forces a null logical binding but the setup compiler demands one for every non-Base-Assault objective, so FC/BT/CE cannot be authored without a tooling change. Nothing was made visible or launched.
## 3. Breakthrough — not ready to play (2026-11-18: areas measured, authoring blocked)
S013 area measurement Completed (lane18/s013/measure.log; both corridors and the exit inside the playable window and infantry-reachable; both tower pads fit the 3x4 m tower). Authoring not attempted: same objective-kind blocker as S007 (deterministic). Nothing visible or launched.
## 4. Convoy Escort — not ready (S019 candidate draft; same measurement step)

Your notes: write them in this file or tell the program lead; they are recorded as owner sampling in `missions.csv`
(`owner_manual_win` and notes), never as a gate.

The [independent designer playtest and prioritized development plan](2026-11-17-s002-s026-designer-playtest.md) records the agent-operated Mac Editor sessions separately. The owner's personal playtest is still open.

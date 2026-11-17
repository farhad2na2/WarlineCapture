# Owner packet — early prototype play (2026-11-17)

You asked to play the first prototype of each objective type early (owner sampling: recorded separately, never an
automated gate, and it does not replace gates 1–4). Each entry is added when that prototype starts from the real menu
and can be played to a result with normal input.

## How to open (Windows PC, `D:\Projects\WarlineCapture`)
1. Make sure no Unity Editor has the project open, then in a terminal in `D:\Projects\WarlineCapture`:
   `git fetch origin` and `git switch missions/integration` (or `git merge origin/missions/integration` if you stay on
   `missions/windows-lane`). Then `git pull`.
2. Open the project in Unity 6000.5.8f1 from Unity Hub (existing Library; no rebuild).
3. Open the start scene, press Play, then follow the menu path given per mission.
Tip: switch ARIA off (the ARIA Play/Stop control in the match HUD) so you play yourself.

## 1. Base Assault — S002 (Desert Base, Ground Maneuver, Established Base)
- **Commit:** `e8ad30c10` on `missions/integration` (or any later commit).
- **Menu path:** Main menu → Skirmish → Skirmish library → "Desert Base · Base Assault · Ground Maneuver · Established
  Base" → Standard size, Regular difficulty → Launch. (On a fresh profile the Skirmish entry appears only after Campaign
  Chapter 1.)
- **What the automated runs show:** ARIA wins most runs in about 230 s of match time (about 21% of the 18-minute deadline),
  through the real menu (`program-logs/2026-11-17/lane14/qa`). It uses every unit type the mission offers.
- **Known unfinished:** one seed (155923) still loses at about 96 s when the enemy's lone tank slips past the army and
  shells the base; a guard tower was added at the player base today (owner decision) and the cause is being reproduced.
  Deadline unchanged (18 minutes). Not yet accepted.
- **Please judge:** is the mission's purpose clear from the start; are the objectives readable on screen; does the pacing
  feel right for you (a human is expected at roughly 1.5–2× ARIA's time); is it fair (e.g. can you stop the lone tank).

## 2. Frontline Control — not ready (areas need a Unity measurement run; S007 candidate draft exists)
## 3. Breakthrough — not ready (S013 candidate draft; same measurement step)
## 4. Convoy Escort — not ready (S019 candidate draft; same measurement step)

Your notes: write them in this file or tell the program lead; they are recorded as owner sampling in `missions.csv`
(`owner_manual_win` and notes), never as a gate.

# Designer playtest fixes: S002 and S026 (2026-11-18)

This packet answers the request to implement the designer playtest recommendations. It covers the candidate commits, which fixes passed, what still needs a phone check, and how Farhad can replay the revised missions.

All work is on `missions/integration` in the Mac checkout. Nothing is on `main` and nothing is published to players.

Evidence is reported in four separate kinds:
- **Automated checks:** tests and harness lines.
- **Normal-input runs:** automated runs through the real menu with touch input.
- **Visual review:** captures that a person has to look at.
- **Phone acceptance:** a real player on a real device.

None of the phone items below have been done.

## 1. Candidate commits (merged into `missions/integration`)

| Area | Merge | What it contains |
|---|---|---|
| P0 runtime | `afde3ed3e` | The allocator fix: per-hit and render-budget state can now be switched on and off in place. It replaces the structural churn that filled the 16 MiB block. It builds on the earlier mitigation `0986acaea`. |
| HUD and base threat | `dceb918b5` | S002 base-under-attack row: it reads ENEMY AT BASE, then names the attacker, or SOURCE UNKNOWN if the attacker is hidden. It also highlights the nearest defender card and its page button. Enemy-base marker: last-known state instead of a bare label, and it is now shown in release builds too. S026: a first-order prompt for the first 45 s ("Advance your infantry along the main avenue…") and a RENDEZVOUS chip. Result panel: each star is judged by its own condition, and the panel says why a met criterion earned no star. This is the low-loss star with zero losses case. |
| Presentation | `ce3fea518` | A camera kick on first contact and on the first hit to the main base, and a marker pop on each accepted order. The kick does not issue troop orders. |
| ARIA base alert | `9c576e26a` | ARIA reacts to the same HUD warning a player sees. Hidden shooters are no longer named. |
| Camera | `3b3a3cf14` | Ground view: height 34 m (was 18/25), pitch 50° (was 65°), zoom-out to 47.5 m (40 m cap on mobile). Helicopter follow: the aircraft fills at most 30% of the view, at least 36 m away, with the ground below and 24 m ahead in frame, and the camera always above the aircraft. Openings: S026 looks down the avenue; S002 faces the enemy tank's approach road. |
| S026 rendezvous anchor | `6024790ab` | The chip moved from 115 m off the junction to 13 m off it. |
| Capture tooling | `a3e3953d9` | Capture names now use the real screen size. Adds the S026 helicopter capture through recruitment. |
| Long-session reason log | `6a945d2e2` | The long session now logs why each purchase was refused. Harness only, no gameplay change. |

The S002 enemy tank and its strength are unchanged. S026 strength is unchanged. Balance was not tuned on automated time alone, as the designer asked. The player guard tower stays off. Section 2 explains why.

## 2. Which fixes passed

**P0 runtime: PASSED twice.**
- The test is a real-menu S026 session at seed 104729. ARIA is off, and the harness buys infantry every 30 s through the normal touch UI and gives no orders. It plays to the 18:00 deadline.
- The pass line on the current candidate (lane21/longsession/unity.log) shows:
  - allocatorErrors=0, exceptions=0, errors=0;
  - result screen shown, menu return working, and the timer expiring as expected;
  - Draw at time limit, 1080.0 s;
  - 0 BlockAllocator exceptions; ECS query blocks 120 of 2048.
- The real-fix candidate also passed in lane19/longsession2, and the earlier mitigation passed in lane18/longsession2.
- Purchases stop after the 7th. The new reason log shows all 15 refusals are the army population cap (48 of 48 infantry). Money is not the cause. So the "army capacity reached" message is correct in this run.

**S002: auto-accepted** (gates 1–4, type-first: 3 English seeds + 1 Farsi). Every run went through the real menu, and ARIA used every unit type in each counted run.

| Seed | Locale | Result | Time | % of 18:00 |
|---|---|---|---|---|
| 130365 | English | Victory | 227.5 s | 21.1% |
| 104731 | English | Victory | 232.3 s | 21.5% |
| 155923 | English (3 runs) | Victory 3 of 3 | 227.8–321.7 s | 21–30% |
| 130365 | Farsi | Victory | 227.2 s | 21.0% |

- In one 155923 run the lone enemy tank reached the base. The alert came at 61.3 s and the first hit at 66.9 s. ARIA's defenders were ordered 1.2 s after the hit, and the base survived at 44 of 800.
- The guard tower stays off. The evidence is thin, and it is reopened if a player loses the base to the lone tank.
- The result screen showed and menu return worked. Checkpoint does not apply because S002 does not offer Save.
- Logs are in the S002 row of `Design/Orchestration/missions.csv`.

**Automated tests passed:**
- 164 EditMode tests after the merges, including two stale tests that were fixed.
- The camera framing focused tests, 7 of 7.
- The aircraft and opening-view tests, 49 of 49.
- Python 148 of 148.

**Camera "after" captures:** S026 and S002 ground views at 2400×1080 and 1920×1080 are committed. They are not reviewed yet (section 3).

**Not yet passed:**
- **Helicopter capture:** it failed twice with `helicopterSquadCardNotFound`. The purchase, return from recruitment and delivery all worked, but the capture code checks for the helicopter's squad card too early. A capture-code fix is being written now.
- **Farsi capture:** not taken. The capture tool had no locale switch, and that is being added in the same fix.
- **104729 interception retest:** ARIA's harness rejects that seed for S002, so only a player can retest it. It is the default seed of a normal menu launch.

## 3. What still needs a phone check (or Farhad on the Mac)

1. **Ground camera:**
   - Is the new height wide enough without making infantry hard to tap?
   - Check that zoom still works and that tapping a squad still selects it.
   - Compare `Design/AgentReports/SkirmishExpansion/CameraFraming/before/` with `after/` (same names, 2400×1080).
2. **Helicopter follow:**
   - Takeoff, turning, flight and landing: are the whole aircraft, its rotor, its heading and the landing spot readable together?
   - Switch back to infantry and return from recruitment; the camera must not lose the group.
   - No automated helicopter capture exists yet.
3. **Disorientation:** is the yawed camera confusing against the north-up minimap? S026 opens facing 180°, S002 opens facing 107.8°.
4. **S002 at seed 104729:**
   - Is the first base hit clear, with its direction and the highlighted defender?
   - Can the player intercept the tank?
   - With zero losses, does the result panel explain the missing low-loss star?
5. **S026:**
   - Is the RENDEZVOUS chip on the crossroads and visible from the opening?
   - Is the first-order prompt readable?
   - Are accepted-order pops, the enemy base's last-known marker, first contact and the decisive hit readable?
6. **S026 long session by hand:** play to 18:00 with ARIA off and recruit reinforcements. Confirm the result screen, the menu return, and that the "army capacity reached" message only appears when the army is really full.

## 4. How Farhad can replay the revised missions (Mac)

1. Tell the program lead you are ready to play. Agents are using Unity on this project now, and only one Editor may open it. The lane is returned to you after the current run.
2. Unity Hub → WarlineCapture → open `Assets/Game/Scenes/Menu.unity` → **Play**.
3. Main menu → **Skirmish** hotspot → **Battle Library**:
   - **S002:** scroll to **Desert Base · Base Assault · Ground Maneuver · Established Base** → **Launch Mission**.
   - **S026:** scroll to **City Crossroads Base Assault** → **Launch Mission**.
4. Keep the defaults (Standard size, Regular difficulty). Leave ARIA on **Stop** to play yourself. The menu's default seed is 104729, the same seed the designer used.
5. Each mission ends at the result screen. **MAIN MENU** returns to the Skirmish menu.

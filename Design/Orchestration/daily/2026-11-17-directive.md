# Directive to the program lead — 2026-11-17 (owner approved "send all", afternoon)

These replace the matching lines of the definition of done and the order of work. The lead edits
`MISSION_PLAN_2026-11-16.md`, `ORCHESTRATOR_PROMPT.md` and `PROGRAM_LEAD_PROMPT.md` to match, marks
the two entries in `decisions_pending.md` as decided with a pointer to this file, and continues.

## 1. Gates (decided)

- **Gate 2 time band is 20–70% of the deadline** (was 40–70%) for ARIA wins at Standard/Regular.
  Reason: ARIA executes faster than a human; a human is expected at roughly 1.5–2 times ARIA's
  time. No deadline, matrix or test change for Base Assault. A win below 20% still triggers the
  plan's early-win rule; a win above 70% still triggers easing.
- **Gate 4 checkpoint/recovery applies only to missions where the game offers Save.** For the
  others the row records "checkpoint: not offered" and gate 4 is result screen + return.
- ~~**Lone-tank base loss: add a guard tower at the player base in the Base Assault template**~~ **Replaced
  (orchestrator, owner-delegated, 2026-11-17 late):** threat legibility first (base vision through fog, "base under attack"
  warning EN/FA, camera jump without orders, ARIA gets nothing extra), then measure counterplay on 155923 and two
  other tank-reaches-base seeds (ARIA saves the base in 3 of 3 reruns of 155923 using only that information), and add
  the guard tower only if that measurement fails. Original line:
  (the lead's option 1). Starting forces only; no unit-stat change. No more ARIA intercept work
  for this. Re-run the Base Assault missions that start today after the change.
- Gate 3 is unchanged: a row needs the `[SkirmishGateUnits]` evidence from a finished run.

## 2. Unity lane priority

Until one authored mission starts and is played to a result (S026 first), the lane runs only:
(a) what S026 needs to start, (b) the S002/S004 runs needed to accept them under the gates above.
Airport probes, Industrial Basin, FC/BT/CE area measurement and other lane work wait behind that.
Work that needs no Unity continues in parallel.

## 3. Certify by family

- A **family** is the set of missions sharing one map source and one objective type.
- **First accepted mission of each objective type:** 3 seeds in English + 1 run in Farsi.
- **First mission of each further family:** 3 seeds in English.
- **Every other mission in a family:** 1 English win at Standard/Regular with the gate-3 trace and
  result/return in that same run, plus the offline draft check, containment and startup markers.
  A mission that loses its single run gets the full 3-seed set after the fix.
- Each row records which rule accepted it ("type-first", "family-first", "sibling") and the family
  mission it relies on. If a family-first mission is later reopened, its siblings are reopened too.

## 4. Operations

- The rule "Operations wait for all 120 Skirmish" is relaxed: **Operations production starts once
  the first authored Skirmish mission of each objective type is accepted.**
- Now: one Codex read-only survey of Operations readiness (what runtime, authoring, HUD, ARIA and
  catalog code exists, what is missing, what it reuses from Skirmish). Report only, no patches.

## Not decided

A second Unity lane on a Windows PC waits on the owner saying whether one is available.

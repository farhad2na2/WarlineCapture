# Lane worker 20 — 2026-11-18

Logs: /Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-18/lane20/. Candidate: integration with legibility build, guard tower off. Match clock = harness `match_seconds`.

## Step 1 — S002 counterplay measurement (all `--menu-journey`, MenuJourney Passed each)

| job | log | result | match clock | % of 1080 | gate-3 unused | base alert (match t) | first base damage (player base) | defenders / home_defense | player base HP at end |
|---|---|---|---|---|---|---|---|---|---|
| S004 104733 warm-up | q1 | Victory | 174.3 s | 16.14% | airport, aa, apc_fast, attack_heli, ... | onset 100.50 | none | n/a | 800 |
| S002 155923 (run 1) | q1 | Victory | 233.8 s | 21.65% | apc_armored, apc_fast, breacher, car, marksman, radar, tank | onset 221.96 | none | triggered 221.96, ordered 230.77 | 800 |
| S002 104729 | q1 | Abort (rejected) | - | - | - | - | - | - | - |
| S002 130365 | q1 | Victory | 227.5 s | 21.07% | none | onset 218.26 | none | triggered 218.26, ordered 219.42 | 800 |
| S002 104731 | q1 | Victory | 232.3 s | 21.51% | none | onset 223.06 | none | triggered 224.21, ordered 229.52 | 800 |
| S002 155923 (run 2) | q2 | Victory | 321.7 s | 29.78% | none | onset 61.34; damage-reason onset 67.08 | 66.92 (tank role.tank.2.11.0, 28/hit) | triggered 61.34, ordered 68.07, re-triggered 84.40/ordered 85.55, ordered 96.18, cleared 101.77; defenders arrived, tank stopped at base HP 44 of 800 (last tank hit 92.67) | 44 |
| S002 155923 (run 3) | q3 | Victory | 227.8 s | 21.09% | none | onset 222.19 | none | triggered 223.37 | 800 |

- 104729 rejection line: `[SkirmishAriaQueue] job=3/5 launchFailed=1 error=First-visit ARIA seed must be 104731, 130365 or 155923.` Not substituted.
- No `event=base_under_attack` string exists in the harness logs; the alert appears as `[AriaS002Trace] event=base_alert state=onset`.
- 155923 results over three runs: Victory x3 (233.8, 321.7, 227.8 s). In 2 of 3 the player base was never damaged because ARIA won first; in run 2 the tank reached the base at ~61 s, ARIA responded and the base survived with 44/800 HP. Not a clean proof of the counterplay: in runs 1/3 the alert came at ~222 s, almost at the win.

## Step 2 — authoring merge
Merged missions/authoring-logical-binding (bbf93fd92, 861218868). Compile clean in the Unity run. `SkirmishMissionAuthoringEntryLogicalBindingTests.RunFocusedValidation`: `result=Passed controls=2` (step2-binding.log). EditMode SkirmishMissionHudGuidanceTests 2/2 passed (step2-hud.xml).

## Step 3 — S007 / S013
- S007: preflight PASS, AuthorFromDraft Passed (2773d4970), containment clean, visible commit, menu-journey Passed but match startup Abort `expandedStartupFailure=BlockedSpawn` (s007/menu/unity.log:2925; first placement failure line 2478 Building_Barrack preferred=(1373,643)).
- S013: AuthorFromDraft FAILED `instancePads: Standard/Recruit: building.watchtower.defense_approach_a needs a DefenseApproach pad, which per-building authoring does not support.` (s013/author.log:686). Partial assets removed.

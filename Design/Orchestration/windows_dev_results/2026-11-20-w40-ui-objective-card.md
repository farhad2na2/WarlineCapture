# W40 — Match UI polish ui-b: Board 2 (compact objective card) and Board 4 (ownership colours and text fit)

- Windows UI lab checkout: `D:\Projects\WarlineCapture-uilab`
- Branch: `missions/win-w40-ui-objective-card`, cut from W39's branch `missions/win-w39-ui-side-panels` at 8a4f3bac9, not from `missions/integration`. This reuses W39's capture approach and expanded-Skirmish test gateway pattern. W40 adds its own fixture (`MatchHudObjectiveCardCapture`) and gateway (`W40ObjectiveCardGateway`).
- Editor: 6000.5.8f1, run through the checked wrappers with `-GuiLicensing`.
- Logs: `D:\Projects\WarlineCapture-program-logs\2026-11-20\win-uilab\w40-*`
- Approved boards: `Design/AgentReports/MatchUiPolish/review.md` ("Approve all six", 2026-11-20). No further visual review was requested.

**Status: automated W40 checks pass.**
- Implementation is done.
- EditMode matches the baseline, with no new failure.
- Native captures pass with 0 rule failures.
- ARIA run 2 on the final code passed every judged job.
- ARIA run 1, on the code before the last copy fix, had one S002 FindBase block. It has the same signature as the pre-existing loop diagnosed in W39 (§3).
- **Pending: real-player and device acceptance have not been done.** Everything below is automated evidence from the Windows lab: fixture captures, EditMode tests and ARIA journeys.

## Commits

| Commit | Content |
|---|---|
| 62bb6a168 | Native capture fixture `MatchHudObjectiveCardCapture` and `W40ObjectiveCardGateway` (Tests/Editor) |
| 5c81b25b6 | Board 4: ownership colours and glyphs, compact chip layout, in-world enemy base chip |
| 8080110da | Board 2: one compact top-centre objective card |
| 462756b13 | EditMode tests (`MatchHudObjectiveCardTests`, updated `MatchHudPolishTests`) |
| 5a5339cb6 | Native before/after captures and `compare.md` |
| 8813d0aac | Board 2: the next action uses the objective family's own sentence (found in live S031, ARIA run 1) |
| ec0dfcfd8 | After captures re-taken on the final code |
| (this commit) | Results note, `compare.md` live section, five live opening frames in `Implemented/W40/live/` |

## What changed

All values are canvas units on the 1920×1080 reference. The Skirmish canvas matches height, so one unit is 1 px at both 1920×1080 and 2400×1080.

**Board 2: the objective card** (`SkirmishMatchView.cs`, `.ObjectiveCard.cs` new, `.Clock.cs`, `.MissionGuidance.cs`, `.PublicObjectiveHud.cs`, `SkirmishObjectiveCardLayout.cs` new)
- One card, in the native threat-panel style:
  - charcoal fill and a 3 px `#E7562C` keyline;
  - 12 px under the live `ResourceStrip`, or 12 px under a visible threat banner;
  - 16 px padding;
  - centred where the old strip was centred.
- **Row 1 (purpose first):**
  - an amber objective glyph;
  - the purpose (the family name: BASE ASSAULT / FRONTLINE CONTROL / BREAKTHROUGH / CONVOY ESCORT, reusing the Battle Library EN/FA copy);
  - the next action (the mission's first order while it is shown, otherwise the objective sentence);
  - the timer.
- **Row 2:** the existing focus controls as chips, and a score cell (tickets, or completed and alive) where the clock used to be.
- **Height and width:**
  - 184 px tall;
  - English Base Assault is 560 px wide;
  - FC, BT and CE grow to fit their controls, up to the space between the side panels (826 px at 1920, 1032 px at 2400).
- **S026:** the stacked `FirstOrderPrompt` bar is removed. Its text is now the card's next action.
- **S041, S046, S011:** each chip is sized to its copy, and its lines auto-size instead of wrapping. Corridor names, "WITHIN 12 m · MATERIALS 900" and NEUTRAL now stay on one line, inside their frames.
- **Next action for FC/BT/CE:** these families use their family objective sentence (`skirmish.s007/s013/s019.objective`, existing EN/FA copy). The live S031 frame from ARIA run 1 showed that the session objective line for that mission is the Base Assault sentence.

**Board 4: ownership** (`HudOwnershipGlyphGraphic.cs` new, `HudIconButtonView.cs`, `.PublicObjectiveHud.cs`, `.Observation.cs`)

| Ownership | Colour | Glyph |
|---|---|---|
| Friendly | `#49BD62` | filled shield |
| Enemy | `#F45C3B` | diamond |
| Neutral | `#BBC7CD` | hollow ring |

- The glyphs are drawn procedurally and never take taps.
- **Where ownership now appears:**
  - YOUR BASE / ENEMY BASE chips;
  - FC zone chips, by owner;
  - the in-world enemy base chip: keyline in the enemy colour, strength by knowledge, diamond before the title;
  - in-world zone labels, tinted by owner.
- **Chip layout:**
  - The camera icon and title sit on the first line.
  - Ownership has its own full-width line.
  - Contested state and capture progress stay visible: an amber tag on the title line (CONTESTED / CAPTURE n % / NEUTRALIZE n %) plus a 4 px capture bar in the capturing side's colour.
- **Interaction unchanged:**
  - Button GameObject names, `PlayerFocusButton` / `EnemyFocusButton` / third-goal identity, handlers and raycast surfaces are unchanged.
  - Show Me / focus stays camera-only.
  - The ARIA frame plumbing (`PublishRenderedObjectiveFrame`, `ResolvePublicOrderGoal`, `AllInsidePublicGoal`, `GroundPublicId`, `AriaPresentedObjective*`) and `SkirmishMatchView.ConvoyService.cs` were not edited.
  - The O001 rail and missions.csv were not touched.
- **No new strings.** Every caption reuses existing EN/FA copy.

Intentional differences from the mockups are listed in `Design/AgentReports/MatchUiPolish/Implemented/W40/compare.md`:
- the card is 184 px tall rather than about 106: two 72 px rows, so 64 px targets and Persian fit;
- there is no separate gold SHOW ME, which would duplicate the ENEMY BASE focus target;
- the existing ownership words are kept;
- Persian next action stays on one line.

## Validation

### 1. Compile and EditMode
- **Compile: Passed, 0 `error CS`.**
  - `w40-capture-after-v4.log:269` compiles `Game.UI.Runtime.dll`.
  - `w40-capture-after-v4.log:3243` reads `[UnityInvoke] ExitCode: 0`.
  - `w40-editmode-3.log` also has 0 `error CS`.
- **Suites (touched areas):** `MatchHudObjectiveCardTests`, `MatchHudPolishTests`, `MatchHudAssistantUiSystemHelperTests`, `HudRightColumnLayoutValidation`, `M03PresentationTests`, `SkirmishPublicObjectiveHudTests`, `SkirmishS002HudRetestTests`, `SkirmishMissionHudGuidanceTests`, `SkirmishHudChipPlacementTests`, `SkirmishConvoyServiceJourneyTests`, `SkirmishNativePublicGoalPresentationTests`, `SkirmishPublicRepairHudTests`, `AriaPublicScanTests`, `AriaPublicBoardingTests`.
- **Baseline (unmodified UI code):** `w40-editmode-baseline-results.xml:2` reads `total="155" passed="143" failed="12"`.
  - These are W39's 7 known failures plus the 5 `SkirmishNativePublicGoalPresentationTests` failures (`MatrixMismatch:deadline_seconds:expected=1690 actual=1200; MissingReference:missionLogicalBinding`). Those 5 are pre-existing and not UI.
- **W40 (final code):** `w40-editmode-3-results.xml:2` reads `total="163" passed="152" failed="11"`.
  - The wrapper exits 1 because tests failed (`w40-editmode-3.log:763-764`).
  - All 11 failures are in the baseline set.
  - `MatchHudPolishTests.PublicObjectiveScoreDoesNotReplaceTheCountdown(True)` failed at baseline and now passes. The test was updated for the score cell.
  - New `MatchHudObjectiveCardTests`: 8/8 passed. They cover:
    - ownership colour, glyph and role mapping;
    - card size, padding, width and placement;
    - chip widths for both text lines;
    - EN/FA zone, corridor and service-pad copy fitting one line;
    - card hierarchy and order;
    - glyphs never raycasting;
    - the family next-action copy.
- Intermediate runs:
  - `w40-editmode.log`: 1 failure in my own new fit test, which was too strict past the maximum chip width. Fixed.
  - `w40-editmode-2.log`: 162/151/11.
- **Prefab churn:** every EditMode run rewrote `Assets/Game/Prefabs/UI/Popups/M03_FieldGuide.prefab`. The writer is `M03PresentationTests` (known). I restored it with `git checkout` each time; it is not committed. No other asset was rewritten.

### 2. Native captures
- **Before** (unmodified UI, fixture only): `w40-capture-before-v2.log:5957` reads `[W40ObjectiveCard] result=Passed phase=before captures=32 ruleFailures=235 enforced=False`.
  - The fixture reproduces the audits: the S026 stacked bar, S011 "NEUTRA / L", S041 corridor names crossing frames, and S046 Materials wrapping to 3 lines.
- **After (final code):** `w40-capture-after-v4.log:3162` reads `[W40ObjectiveCard] result=Passed phase=after captures=32 ruleFailures=0 enforced=True`.
- **States:** S002 BA, S026 BA first order, S031 FC, S011 FC all neutral, S037 BT, S041 BT exit open, S020 CE, S046 CE with service pad. Each is captured in EN and FA, at 1920×1080 and 2400×1080.
- **Paths:**
  - `D:\Projects\WarlineCapture-uilab\Design\AgentReports\MatchUiPolish\Implemented\W40\before\` (32 PNG + `geometry.md`);
  - `...\W40\after\` (32 PNG + `geometry.md`);
  - `...\W40\compare.md`;
  - live frames in `...\W40\live\`.
- These are fixture renders named after the missions, not live frames. Each PNG is the top 600 px band, full width.
- **Earlier after runs:**
  - `w40-capture-after.log`: 4 rule failures, the Persian title "خروج" at 64 px chips.
  - `w40-capture-after-v2.log`: still 4 failures, at 72 px chips; the dot of "خ" reached above the keyline.
  - `w40-capture-after-v3.log:1708`: 0 failures, after a 5 px top inset.
  - I fixed these failures; I did not relax the rules.

### 3. ARIA regression
Command, run from `D:\Projects\WarlineCapture-uilab`: `python -B …\Tools\Missions\run_aria_queue.py --launcher windows --jobs S031:130366:en,S002:130365:en,S020:130365:en,S031:130365:en,S037:130365:en,S043:130365:fa-IR --menu-journey --validation-library --no-table --timeout 30000 --log-dir <dir>`

**Scene-cache warm-up (uncounted):** `--jobs S031:130366:en` → `w40-aria-warmup\unity.log:3490` reads `result=Completed jobs=1 recorded=1 wins=0 sourceUnchanged=1 exitCode=0`.
- The job itself aborted while the scene dependency cache imported (333 s).

**Run 1** (`w40-aria\unity.log`), on code at 5a5339cb6, before the next-action copy fix. The queue completed: `:397129 result=Completed jobs=6 recorded=6 wins=5 sourceUnchanged=1 exitCode=0`.
- S031:130366 (warm-up slot): Victory, 40.40 %.
- S020: Victory, 50.20 %, PASS.
- S031: Victory, 32.13 %, PASS.
- S037: Victory, 36.81 %, PASS.
- S043 fa-IR: Victory, 51.20 %, PASS.
- **S002 failed:**
  - Abort `ariaStopped:Blocked`.
  - First failing line: `w40-aria\unity.log:360348 [AriaBlocked] reason=101 target=-257998 attempts=1 actions=122 kind=Waiting offered=0 goal=10000 intent=FindBase frame=5234`.
  - Gate 3: S002 FAIL (other=1).
- **S002 diagnosis (W39 method).** This has the same signature as W39 run 2. W40 controls are not involved.
  - The army reached the enemy base. ARIA tapped ENEMY BASE four times (`:213895–214049`) at screen (1119.55, 843.04) y-up, which is (1119.55, 236.96) top-down.
    - That is the exact centre of the W40 ENEMY BASE chip: x 986.3–1252.8, y 201–273 (`after/geometry.md`, s002-ba-en-1920).
    - The tap was live and correctly placed.
  - A perimeter base alert (`:214116 event=base_alert state=onset reason=perimeter`) sent ARIA to DefendBase (`:214170`). The army went home: distance 22.6 → 700.7 (`:214205`). These are the same numbers as W39 run 2.
  - FindBase then looped five times: open the full map on the minimap (199.63, 329.29), then tap the full map's Close button (1660.68, 967.52), with `mapOpen=True closeAvailable=True` (`:252645`, `:270824`, …). Then it blocked.
  - W40 does not touch the minimap or the full-map popup.
  - Verdict: the pre-existing ARIA FindBase behaviour after a base-alert detour, as diagnosed in W39. Not caused by W40. Nothing was tuned.

**Run 2** (`w40-aria-2\unity.log`), on the final code (8813d0aac / ec0dfcfd8). The queue completed: `:242329 result=Completed jobs=6 recorded=6 wins=5 sourceUnchanged=1 exitCode=0`. There are 0 `[AriaBlocked]` lines in the log.

| Job | Result | deadlinePct | Menu journey | Return | AriaBlocked | Gate 3 |
|---|---|---|---|---|---|---|
| S031:130366 en (warm-up, not judged) | Abort, `startupFailure=Timeout` (`:3170`) | — | Passed (`:3048`) | n/a | 0 | — |
| S002:130365 en | Victory, MainBaseDestroyed, 234.25 s (`:201874`) | 21.69 | Passed (`:4016`) | Passed MAIN MENU (`:201802`) | 0 | PASS |
| S020:130365 en | Victory, ConvoyDelivered (`:209209`) | 49.59 | Passed (`:202685`) | Passed MAIN MENU (`:209468`) | 0 | PASS |
| S031:130365 en | Victory, TicketsDepleted (`:218828`) | 32.13 | Passed (`:210305`) | Passed MAIN MENU (`:219102`) | 0 | PASS |
| S037:130365 en | Victory, BreakthroughEvacuated (`:235979`) | 54.56 | Passed (`:219983`) | Passed MAIN MENU (`:236237`) | 0 | PASS |
| S043:130365 fa-IR | Victory, ConvoyDelivered (`:241779`) | 36.17 | Passed (`:237096`) | Passed MAIN MENU (`:242047`) | 0 | PASS |

- Gate 3: `gate3_objective.py --summary` gives PASS for S002, S020, S031, S037 and S043 (`w40-aria-2-gate3.out`).
- The warm-up slot timed out at startup, the same infrastructure case W39 recorded in its run 1. It is uncounted, and no judged job was affected. I recorded it rather than retried it.
- S043 fa-IR was 36.17 % in this run and 51.20 % in run 1, inside 20–70. The known Windows CE divergence did not appear.
- **Evidence handling:**
  - I deleted the harness evidence the runs wrote under `SkirmishExpansion/**/_Evidence`: 106 files after run 1 and the warm-up, 91 after run 2.
  - I restored the tracked `S002/_Evidence/s002-aria-watch-controls.png` each time.
  - Nothing under `_Evidence` is committed.
  - Five run-2 opening frames were copied first to `Implemented/W40/live/`.

**Observation, not W40:** in the live frames the ARIA panel heading reads BASE ASSAULT / "حمله به پایگاه" in S031, S037, S020 and S043. That is ARIA panel copy, and it is pre-existing.

### 4. This note
Committed on the branch. No shared dated results file was edited.

## Incidents and deviations
- **No CPU-load timeouts on the Unity wrapper runs.** The only timeout was the warm-up slot's startup timeout in ARIA run 2 (uncounted).
- The main checkout was not touched. Every write used absolute `D:\Projects\WarlineCapture-uilab\` paths or `git -C`. Its running Editor and Library were left alone.
- Pending decisions for the lead:
  - whether ARIA run 1's S002 block (pre-existing FindBase loop) needs anything beyond run 2's clean S002 win;
  - the ARIA-panel family heading in non-BA missions.
- **Pending:** real-player and device acceptance.

# W42 — ARIA panel heading names the mission's objective

- Windows UI lab checkout: `D:\Projects\WarlineCapture-uilab`
- **Branch:** `missions/win-w42-aria-heading`.
  - Cut from W40's branch `missions/win-w40-ui-objective-card` at 9917ae900, so that W40's HUD is present.
  - Integration later merged W40 (13f2652ea). Before the push, `origin/missions/integration` was merged in with a normal merge, so the branch's diff against integration shows W42 only (see Commits).
- Editor: 6000.5.8f1, run through the checked wrappers with `-GuiLicensing`.
- Logs: `D:\Projects\WarlineCapture-program-logs\2026-11-21\win-uilab\w42-*`

**Status: automated W42 checks pass.**
- The ARIA panel heading now names the mission's own objective family, in EN and FA.
- EditMode matches the baseline: the same 11 pre-existing failures and no new failure. The new heading tests pass 11/11.
- Native after-captures pass with 0 rule failures.
- The ARIA regression won every judged job inside the band, with gate 3 PASS and 0 AriaBlocked.
- The live opening frames show FRONTLINE CONTROL, BREAKTHROUGH, CONVOY ESCORT and "اسکورت کاروان".
- **Pending: real-player and device acceptance have not been done.** Everything below is automated evidence from the Windows lab: fixture captures, EditMode tests and ARIA journeys.

## Why the heading said BASE ASSAULT

The Skirmish ARIA briefing heading is the model's `RecommendationTitle`. It is set in `UiShellEcsGateway.ReadModels.Assistant.cs`, in `TryReadMatchHudAssistantPanel`, Skirmish branch. That branch built the panel model with a hard-coded `GameText.Get("ui.skirmish.base_assault","BASE ASSAULT")` for every Skirmish session, whatever the objective. `AriaTutorialBriefingView.Apply` shows that title as-is, upper-cased in English.

The W40 capture fixture passed the family name straight into the view (`MatchHudObjectiveCardCapture.Family`). That is why the bug only appeared in live frames: S031, S037, S020, S043 in W40, and the S009 FC live capture.

## What changed (presentation only)

- **`UiShellEcsGateway.ReadModels.Assistant.cs`** (Skirmish branch only):
  - The objective kind comes from the session's `SkirmishResolvedSetupRecord.Setup.ObjectiveKind`. If that is missing or `None`, it comes from `SkirmishExpandedSessionComponent.ObjectiveKind`. Otherwise it is Base Assault.
  - The heading is `SkirmishObjectiveCardLayout.AriaHeading(kind, rtl, <ui.skirmish.base_assault>)`.
- **`SkirmishObjectiveCardLayout.AriaHeading`** (new, 4 lines):
  - FC, BT and CE return `Purpose(kind, persian)`. This is the Battle Library EN/FA family copy the W40 card already uses: FRONTLINE CONTROL / کنترل خط, BREAKTHROUGH / شکستن خط, CONVOY ESCORT / اسکورت کاروان.
  - Base Assault, and a session with no known family, keep the localized catalog title exactly as before.
- **No new strings.**
- **Not touched:**
  - the body text, actions and the ARIA Play / Stop control;
  - GameObject names, buttons and raycasts;
  - ARIA decision logic and objective data;
  - the camera;
  - the ARIA frame plumbing in `SkirmishMatchView.PublicObjectiveHud.cs`;
  - the O001 rail;
  - missions.csv.

## Commits

| Commit | Content |
|---|---|
| 344eea83f | Native capture fixture `MatchHudAriaHeadingCapture` and focused `MatchHudAriaHeadingTests` (Tests/Editor) |
| eaf0d0381 | Fixture: compare Persian after NFKC; check the read-model heading too; `.meta` files |
| 9015e8632 | The fix: the ARIA heading names the mission's objective family (`UiShellEcsGateway.ReadModels.Assistant.cs`, `SkirmishObjectiveCardLayout.AriaHeading`) |
| b95b3c28f | A resolved setup without a kind does not override the session's kind |
| 97985991e | Fixture: fold the WAW presentation forms that Mono's NFKC leaves |
| e460efb34 | Native before/after captures and `compare.md` |
| e2dee30b9 | Live ARIA opening frames and the `compare.md` live section |
| 9d10735f1 | Normal merge of `origin/missions/integration` (fd231f6e7, which includes W40 as 13f2652ea) |
| (this commit) | Results note |

- **The merge had no conflicts.** It brought only `Design/Orchestration/STATUS.md`, `missions.csv`, `windows_dev_queue.md` and a 5-line `Tools/Missions/run_aria_queue.py` change from integration. It touched no Assets.
- After the merge, `git diff --name-only origin/missions/integration HEAD` lists only W42 files.
- No file in that list has `filter: lfs`.
- The validation above ran on e460efb34, before the merge.

## Validation

### 1. Compile and EditMode
- **Compile: 0 `error CS`.**
  - `w42-capture-after-v1.log` compiles `Game.UI.Runtime.dll` (`:449`) and `Game.UI.Shell.Ecs.dll` (`:499`) on the fix, with 0 `error CS`.
  - `w42-capture-after.log` and `w42-editmode.log` also have 0 `error CS`.
- **Suites:** W40's 14 touched suites plus the new `MatchHudAriaHeadingTests`.
  - The suites are `MatchHudObjectiveCardTests`, `MatchHudPolishTests`, `MatchHudAssistantUiSystemHelperTests`, `HudRightColumnLayoutValidation`, `M03PresentationTests`, `SkirmishPublicObjectiveHudTests`, `SkirmishS002HudRetestTests`, `SkirmishMissionHudGuidanceTests`, `SkirmishHudChipPlacementTests`, `SkirmishConvoyServiceJourneyTests`, `SkirmishNativePublicGoalPresentationTests`, `SkirmishPublicRepairHudTests`, `AriaPublicScanTests` and `AriaPublicBoardingTests`.
- **Baseline (unmodified UI code at eaf0d0381, new tests included):** `w42-editmode-baseline-results.xml:2` reads `total="174" passed="155" failed="19"`.
  - The wrapper exits 1 because tests failed (`w42-editmode-baseline.log:751-752`).
  - 11 failures are W40's known pre-existing set:
    - 5 × `SkirmishNativePublicGoalPresentationTests`, `MatrixMismatch:deadline_seconds … missionLogicalBinding`;
    - `HudRightColumnLayoutValidation.MinimapDockAndContentHeightFollowActualControlsAndCopy`;
    - `M03PresentationTests.WarningDetailsAndOptionalActionsFitTheHudAtBothAspects`;
    - 4 × `MatchHudAssistantUiSystemHelperTests` tutorial cases.
  - The other 8 failures are the new W42 tests, which reproduce the bug on unmodified code: FC, BT and CE in EN and FA, plus the two session-fallback cases.
- **W42:** `w42-editmode-results.xml:2` reads `total="174" passed="163" failed="11"`.
  - The wrapper exits 1 because tests failed (`w42-editmode.log:753-754`).
  - The 11 failures are exactly the baseline's pre-existing 11.
  - `MatchHudAriaHeadingTests` passed 11/11. The tests drive the shipped `UiShellEcsGateway.TryReadMatchHudAssistantPanel` over an isolated playing session and check:
    - objective type → heading for BA, FC, BT and CE in EN and FA (8 cases);
    - BA equals the `ui.skirmish.base_assault` catalog title;
    - fallback to the expanded-session kind, and BA for a session with no kind (EN and FA);
    - body, action label and Show/Execute/Stop/TakeControl flags unchanged across families.
- **Prefab churn:** both EditMode runs rewrote `Assets/Game/Prefabs/UI/Popups/M03_FieldGuide.prefab`. The writer is `M03PresentationTests` (known). I restored it with `git checkout` each time; it is not committed. No other asset was rewritten.

### 2. Native captures (1920×1080, EN and FA)
Fixture: `MatchHudAriaHeadingCapture` (Tests/Editor), with `W40ObjectiveCardGateway` for the HUD shell state.
- **Source of the heading:**
  - For each mission, the fixture loads the real resolved setup with `SkirmishSetupBriefing.TryLoad(id, 130365)`.
  - It reads the panel from the shipped `UiShellEcsGateway.TryReadMatchHudAssistantPanel` over an isolated session.
  - The fixture never chooses the heading.
- **Missions:** S002 BA, S031 FC, S037 BT and S020 CE, in ARIA Play observing state.
- **Before (unmodified code, eaf0d0381):** `w42-capture-before-v2.log:1186` reads `result=Passed phase=before captures=8 ruleFailures=12 enforced=False`.
  - The 12 failures are 2 rules × FC/BT/CE × EN/FA. The read model and the panel both show BASE ASSAULT / حمله به پایگاه.
  - BA passes.
- **After (W42 code):** `w42-capture-after.log:964` reads `result=Passed phase=after captures=8 ruleFailures=0 enforced=True`.
  - Every heading is one line, not truncated, inside its 427 px frame and the panel.
  - FRONTLINE CONTROL is the longest, ending at x 1808.0 of 1886.8.
  - Persian is drawn RTL and right-aligned.
  - The panel and Stop ARIA (427 × 82.6 px) keep their rectangles, inside the safe area.
- **Paths:**
  - `D:\Projects\WarlineCapture-uilab\Design\AgentReports\MatchUiPolish\Implemented\W42\before\` (8 PNG + `geometry.md`);
  - `...\W42\after\` (8 PNG + `geometry.md`);
  - `...\W42\compare.md`.
- **Earlier capture attempts (logs kept):**
  - `w42-capture-before-attempt1.log`: my outer shell redirect collided with the wrapper's own `.stdout.log`. The Editor's `System.Console` handle was invalid, scripts did not compile (`executeMethod class … could not be found`), and the Editor hung. I verified its project, path, PID 19044, start time and tree, then terminated it. No evidence was lost. Later runs write the wrapper output to `*.wrapper.out`.
  - `w42-capture-before-attempt2.log:2448`: native `Crash!!!` in `ShaderVariantCollection` warmup at project start, right after the forced stop. This was infrastructure; I retried once.
  - `w42-capture-before-v1.log:931`: 7 failures. Persian is drawn in Arabic presentation forms, and the fixture compared it unnormalized. I fixed the fixture (NFKC) and re-ran it as v2.
  - `w42-capture-after-v1.log:2257`: 1 failure, CE FA only. Mono's NFKC leaves the WAW forms U+FEED/U+FEEE unfolded. The rendered text was correct (see `after/s020-ce-fa-IR-1920.png`). I fixed the fixture's comparison; I did not relax a rule.

### 3. ARIA regression
Run from `D:\Projects\WarlineCapture-uilab` on the W42 code (e460efb34). Command:

`python -B …\Tools\Missions\run_aria_queue.py --launcher windows --jobs S031:130366:en,S002:130365:en,S020:130365:en,S031:130365:en,S037:130365:en,S043:130365:fa-IR --menu-journey --validation-library --no-table --timeout 30000 --log-dir …\w42-aria`

**Scene-cache warm-up (uncounted):** `--jobs S031:130366:en`.
- `w42-aria-warmup\unity.log:3447` reads `result=Completed jobs=1 recorded=1 wins=0 sourceUnchanged=1 exitCode=0`.
- The job itself aborted after 371 s (`:3432`), as in W40.

**Counted run** (`w42-aria\unity.log`):
- The queue completed: `:257198 result=Completed jobs=6 recorded=6 wins=6 sourceUnchanged=1 exitCode=0`.
- The wrapper reports `[UnityInvoke] ExitCode: 0` (`w42-aria-queue.out:17`).
- The log has 0 `[AriaBlocked]` lines.

| Job | Result | deadlinePct | Menu journey | Return | AriaBlocked | Gate 3 |
|---|---|---|---|---|---|---|
| S031:130366 en (warm-up slot, not judged) | Victory, TicketsDepleted (`:10298`) | 34.64 | Passed (`:1257`) | Passed MAIN MENU (`:10552`) | 0 | PASS |
| S002:130365 en | Victory, MainBaseDestroyed, 232.9 s (`:223793`, queue `:257123`) | 21.56 | Passed (`:11427`) | Passed MAIN MENU (`:224032`) | 0 | PASS |
| S020:130365 en | Victory, ConvoyDelivered (`:231173`) | 50.08 | Passed (`:224854`) | Passed MAIN MENU (`:231447`) | 0 | PASS |
| S031:130365 en | Victory, TicketsDepleted (`:241364`) | 34.64 | Passed (`:232325`) | Passed MAIN MENU (`:241657`) | 0 | PASS |
| S037:130365 en | Victory, BreakthroughEvacuated (`:248393`) | 51.99 | Passed (`:242497`) | Passed MAIN MENU (`:248648`) | 0 | PASS |
| S043:130365 fa-IR | Victory, ConvoyDelivered (`:256645`) | 55.62 | Passed (`:249428`) | Passed MAIN MENU (`:256915`) | 0 | PASS |

- Gate 3: `gate3_objective.py --summary w42-aria\unity.log` gives PASS for S002, S020, S031 (2 jobs), S037 and S043 (`w42-aria-gate3.out`).
- S043 fa-IR was 55.62 %, inside 20–70. The known Windows CE divergence did not appear.
- S002 did not hit the W41 FindBase signature, so no S002 rerun was needed.
- **Live frames:**
  - Five opening frames (1920×1080) from this run were copied to `Design/AgentReports/MatchUiPolish/Implemented/W42/live/` before the evidence cleanup.
  - S031, S037 and S020 show FRONTLINE CONTROL, BREAKTHROUGH and CONVOY ESCORT in the ARIA heading.
  - S043 FA shows "اسکورت کاروان", right-to-left.
  - S002 still shows BASE ASSAULT.
- **Evidence handling:**
  - I deleted the harness evidence the warm-up and counted run wrote under `SkirmishExpansion/**/_Evidence`: S031 warm-up candidates, then 99 files after the counted run.
  - I restored the tracked `S002/_Evidence/s002-aria-watch-controls.png`.
  - Nothing under `_Evidence` is committed.

### 4. This note
Committed on the branch. No shared dated results file was edited.

## Observed, not changed (outside W42)
- **The ARIA panel's static body is still the Base Assault match help for FC, BT and CE** ("Destroy the original enemy Barracks and protect yours. …").
  - It comes from `SkirmishSetupBriefing.MatchHelp`, which ignores the objective kind.
  - It is visible in the fixture captures before and after. During live ARIA Play the body shows ARIA's narration instead (see the live frames).
  - W42's scope was the heading only, so the body is left for a lead decision.
- **`PauseOptionsV3PopupView.cs:399`** also shows `ui.skirmish.base_assault` as the mission name in the pause popup for every Skirmish. I did not change it.

## Incidents and deviations
- **Capture attempts 1 and 2** (see §2) were my redirect mistake and the crash that followed it. I terminated one hung Editor that I had started:
  - I verified it first: project `WarlineCapture-uilab`, path `D:\Program Files\Unity\6000.5.8f1\Editor\Unity.exe`, PID 19044, started 02:36:46, command line `-executeMethod MatchHudAriaHeadingCapture.RunBefore`, and its owned tree.
  - It was idle after the failed `executeMethod`, so nothing was lost.
- **What I left alone:** Unity Hub, the licensing client, the Hub helper (PID 21108) and the main checkout's Editor (PID 620). The main checkout was not touched; every write used absolute `D:\Projects\WarlineCapture-uilab\` paths or `git -C`.
- **Capture v1 Editor (`w42-capture-before-v1`):** it lingered about 2 minutes after `Cleanup mono` before it exited on its own. Its exit code was 0 (`w42-capture-before-v1.wrapper.out`).
- **No CPU-load startup timeouts** occurred.
- **The before captures come from fixture v2, before the WAW fold was added.** The fold only changes the Persian CE comparison. That comparison already failed before W42 on the read-model rule (BASE ASSAULT), so the before verdicts are unchanged.
- **The S037 live frame shows overlapping "DESIGNATED SOLDIER n 0 / 3 s" world labels** around the riflemen at the opening. This is pre-existing in-world text, not the ARIA panel, and is not part of W42.
- **Pending:** real-player and device acceptance.

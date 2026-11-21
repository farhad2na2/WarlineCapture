# W39 — Match UI polish ui-a: Board 1 (left panel spacing) and Board 3 (ARIA panel states)

- Windows UI lab checkout: `D:\Projects\WarlineCapture-uilab`
- Branch: `missions/win-w39-ui-side-panels`, cut from `missions/integration` a59d86c6e
- Editor: 6000.5.8f1, run through the checked wrappers with `-GuiLicensing`
- Logs: `D:\Projects\WarlineCapture-program-logs\2026-11-20\win-uilab\`
- Approved boards: `Design/AgentReports/MatchUiPolish/review.md` ("Approve all six", 2026-11-20)

**Status: automated W39 checks are done; the S002 decision is with the lead.**
- Implementation, EditMode and native captures pass.
- S002 was blocked in ARIA run 2. The diagnosis attributes this to a FindBase full-map loop after a base-alert detour, not to W39 layout (§3).
- The lead-approved counted S002 rerun passed: Victory 21.74%, menu journey Passed, return Passed MAIN MENU, 0 AriaBlocked, gate 3 PASS.
- S020, S031 and S037 passed in run 2. S043 passed except for the known Windows CE band divergence (93.41%).
- **Pending:** real-player and device acceptance has not been done. Everything below is automated evidence from the Windows lab: fixture captures, EditMode tests and ARIA journeys.

## Commits

| Commit | Content |
|---|---|
| 486c27cdd | Native capture fixture `MatchHudSidePanelsCapture` (Tests/Editor) |
| cdcf889c7 | Fixture: expanded-Skirmish roster state (test-only gateway) and exclusion of the rail root button |
| 5c1e89155 | Board 1: left column inset and gutters |
| 65f817d93 | Board 3: ARIA panel sized from content, insets and gaps, compact stopped state |
| befd036ee | EditMode tests for the layout rules (16 cases) |
| 9c1f29b20 | Native before/after captures and `compare.md` |
| (this commit) | Results note, plus three live ARIA opening frames in `Implemented/W39/live/` |

## What changed

All values are in pixels on the 1920×1080 reference. HUD units convert as px × 941/1080. Measured scale: 1 unit = 1.148 px at both 1920×1080 and 2400×1080.

**Board 1, left column** (`MatchHudSidePanelLayout.cs`, `MissionHudTouchLayoutView.RefreshLayout`, `MatchHudSelectionPanelView.ContentSize`/`.FieldRepair`, `MatchHudSquadTrayView.Pagination`)
- The selected-unit card, minimap and first category card share a 16 px left inset.
- The minimap sits 24 px above the cards. Previously it was fixed at 230/300 units, which left 27.5 px over category cards and 107.9 px over the expanded roster.
- The card stops 24 px above the minimap (was 13.8 px).
- Card inner padding is 16 px, with 12 px between sections. COMMANDS stays 83 px tall.
- Category cards are 8 px apart (was 5.7 px). Cards are 133 units wide with a 7-unit gap, so the 140-unit stride is unchanged: paging, swipe distances and the cards ARIA taps keep their positions.
- The tray viewport now fits exactly five cards. The sixth roster card no longer shows as a sliver beside the command strip, and the last card is 13.8 px from the strip.
- Cards sit 16.1 px above the bottom edge (was 14.9 px).
- The command strip is unchanged: its buttons are 19.5 px above the bottom, which meets the ≥ 16 px rule.

**Board 3, ARIA panel** (`AriaTutorialBriefingView.Layout`/`.Polish`/`.Watch`)
- 16 px side insets (was 23 px), 12 px gaps (was 13.8 px), and a 16 px bottom inset. The panel is sized from its content.
- Without guidance copy, the panel shows a compact header instead of the 230-unit empty portrait stage and telemetry: "ARIA", a status line ("Guidance paused" / "راهنمایی متوقف شده") and the portrait at the end. Below it sits the existing green Play control. Stopped panel height: 383 px → 221 px.
- Show Me already used the native camera glyph; unchanged.
- Button GameObject names, identities, hierarchy and raycasts are unchanged. The ARIA input layer needed no change.

Intentional deviations are listed in `Design/AgentReports/MatchUiPolish/Implemented/W39/compare.md`:
- Show Me is above Field Guide, because they live in different rows.
- The existing WATCH/ARIA PLAY/STOP captions are kept.
- The new status string is an inline EN/FA pair, following the `.Watch.cs` precedent.
- The command strip and the outer ARIA rail are not moved.

## Validation

### 1. Compile and EditMode
- **Compile: Passed.** 0 `error CS`.
  - `w39-capture-after-v2.log:471` compiles `Game.UI.Runtime.dll`.
  - `w39-capture-after-v2.log:2384` reads `[UnityInvoke] ExitCode: 0`.
  - The EditMode logs also contain no `error CS`.
- **EditMode (touched suites plus new tests):** `w39-editmode-2-results.xml:2` reads `total="105" passed="98" failed="7"`.
  - The wrapper exits 1 because tests failed (`w39-editmode-2.log:2172`).
  - New `MatchHudSidePanelLayoutTests`: 16/16 passed.
  - The 7 failures are pre-existing. The baseline on unmodified UI code, `w39-editmode-baseline-results.xml:2`, reads `total="89" passed="82" failed="7"` with the same 7 tests:
    - `HudRightColumnLayoutValidation.MinimapDockAndContentHeightFollowActualControlsAndCopy`: stale; it expects a minimap docked 8 units above Build.
    - `M03PresentationTests.WarningDetailsAndOptionalActionsFitTheHudAtBothAspects`: threat-panel contact text.
    - `MatchHudAssistantUiSystemHelperTests`: 4 tests on copy/localization strings and the NameStrip RectTransform.
    - `MatchHudPolishTests.PublicObjectiveScoreDoesNotReplaceTheCountdown(True)`.
  - W39 initially failed two `M03PresentationTests` lesson-layout cases. They asserted the old 12-unit gap and 20-unit inset. They now assert the approved 12 px / 16 px through `AriaTutorialBriefingView.Gap`/`Inset`, and pass.
- **Prefab churn:** each EditMode run rewrote `Assets/Game/Prefabs/UI/Popups/M03_FieldGuide.prefab`, reserializing it (~1750 changed lines). The writer is `M03PresentationTests`; record it as a known writer. It is not fixed in W39. I restored the prefab with `git checkout` after every run and did not commit it.

### 2. Native captures
- **Before** (unmodified UI, fixture only): `w39-capture-before-v2.log:5273` reads `[W39SidePanels] result=Passed phase=before captures=12 ruleFailures=156 enforced=False`.
- **After:** `w39-capture-after-v2.log:2305` reads `[W39SidePanels] result=Passed phase=after captures=12 ruleFailures=0 enforced=True`.
- Paths: `Design/AgentReports/MatchUiPolish/Implemented/W39/before/*.png`, `after/*.png`, each with `geometry.md`, plus `compare.md`.
  - EN and FA, 1920×1080 and 2400×1080.
  - Three states: two actions with a tall card and category cards; one action with the paginated expanded roster; stopped.
- This is a native prefab fixture with fixture models, not live mission frames.

### 3. ARIA regression
**Result: S002 failed in run 2; the lead-approved counted S002 rerun passed.**
- Run 2: S002 aborted on an `AriaBlocked`. The diagnosis below finds it pre-existing ARIA FindBase behaviour, not W39.
- Rerun (`aria-3`): S002 won (21.74%) with journey, return and gate 3 passing and 0 AriaBlocked.
- All other judged jobs passed in run 2. The exception is S043's CE deadlinePct, the known Windows divergence.
- Whether this is enough is the lead's decision.

Command (both runs): `python -B Tools/Missions/run_aria_queue.py --launcher windows --jobs S031:130366:en,S002:130365:en,S020:130365:en,S031:130365:en,S037:130365:en,S043:130365:fa-IR --menu-journey --validation-library --no-table --timeout 30000 --log-dir <dir>`. The code is the committed W39 code (9c1f29b20).

**Run 1** (`win-uilab\aria\unity.log`) — queue-level failure, infrastructure.
- First failing line: `aria\unity.log:255083 [SkirmishAriaQueue] result=Failed reason=candidateSourceChangedDuringQueue jobs=6 recorded=6 wins=0 sourceUnchanged=0 exitCode=2`.
- Cause: the candidate fingerprint was `c61d6483…` at the start and `197c7249…` at the end.
  - On this fresh checkout, the first matches generated two gitignored files: `Assets/SceneDependencyCache/{e4e00d5e…,c4f8c913…}.sceneWithBuildSettings.meta`, created at 21:17 local.
  - `SkirmishCandidateSourceHash` counts `.meta` files under `Assets`, so the fingerprint changed and every job was classified `Abort`.
- Matches as played:

  | Job | Outcome | deadlinePct |
  |---|---|---|
  | Warm-up S031:130366 | `startupFailure=Timeout` (`aria\unity.log:10005`) | — |
  | S002 | Victory, MainBaseDestroyed | 21.74% |
  | S020 | Victory | 50.09% |
  | S031 | Victory | 34.65% |
  | S037 | Victory | 55.42% |
  | S043 fa-IR | Victory | 85.75% |

- Every judged job had menu journey Passed, return Passed MAIN MENU, 0 AriaBlocked, and gate 3 PASS (`gate3_objective.py --summary`).
- These results are not counted, because the queue failed.

**Run 2** (`win-uilab\aria-2\unity.log`) — same queue, scene cache now warm.
- The fingerprint stayed stable: `aria-2\unity.log:351178 [SkirmishAriaQueue] result=Completed jobs=6 recorded=6 wins=5 sourceUnchanged=1 exitCode=0`.

| Job | Result | deadlinePct | Menu journey | Return | AriaBlocked | Gate 3 |
|---|---|---|---|---|---|---|
| S031:130366 en (warm-up, not judged) | Victory (`aria-2:351088`) | 32.15 | Passed (`:1250`) | Passed MAIN MENU (`:12456`) | 0 | — |
| S002:130365 en | **Abort, `ariaStopped:Blocked`** (`aria-2:312319`) | — (stopped at 362.8 s) | Passed (`:13289`) | n/a (aborted) | **1** | **FAIL** (other=1) |
| S020:130365 en | Victory, ConvoyDelivered (`:320061`) | 50.36 | Passed (`:313084`) | Passed (`:320302`) | 0 | PASS |
| S031:130365 en | Victory, TicketsDepleted (`:351133`) | 34.66 | Passed (`:321092`) | Passed (`:331801`) | 0 | PASS |
| S037:130365 en | Victory, BreakthroughEvacuated (`:351148`) | 52.65 | Passed (`:332597`) | Passed (`:338732`) | 0 | PASS |
| S043:130365 fa-IR | Victory, ConvoyDelivered (`:351163`) | **93.41** | Passed (`:339569`) | Passed (`:350896`) | 0 | PASS |

**S002 failure**
- First failing line: `aria-2\unity.log:310865 [AriaBlocked] reason=101 target=-258640 attempts=1 actions=122 kind=Waiting offered=0 goal=10000 intent=FindBase frame=4709`.
- ARIA looped in FindBase: 7 taps on the minimap (target -20010 at screen (199.6, 329.3), y-up), each followed by a tap at (1660.7, 967.5) on a fresh dynamic target (-168616 … -258640). Then it blocked.
- In run 1, the same mission and seed on the same W39 code made that pair of taps once, then won.
- The minimap taps landed (W39 moved the minimap). The block is on the follow-up control at (1660.7, 967.5): `control=unknown`, `mapOpen=False` (`aria-2:310898`).
- I did not tune around it, and I did not change ARIA logic.

**S002 diagnosis (lead-approved follow-up)**

Coordinates: ARIA logs Unity screen points with y up at 1920×1080 (`aria-2\unity.log:564` reads `gameView=1920x1080`). The rects below are top-down, from the native fixture geometry for the expanded-roster state.

1. **Does the minimap tap land inside the minimap?** The tap at (199.6, 329.3) y-up is (199.6, 750.7) top-down.
   - It lands inside the W39 minimap rect: x 16.0–383.3, y 567.1–819.6 (`Implemented/W39/after/geometry.md`, aria-one-action-en-1920).
   - It would fall below the pre-W39 rect: x 17.2–384.5, y 483.2–735.7 (`before/geometry.md`).
   - The run used W39 code, so landing in the new rect is correct. The tap's x (199.63) is the new rect's centre x; the old centre x was 200.85.
2. **Is the target computed live or from a cache?** It is computed live.
   - The open-map target `-20010` is computed every observation from the live `watchMap.MapRect` (`MatchHudAssistantUiSystemHelper.SkirmishWatch.cs:611-624`, `ObserveAdvanceMapFocus`): `RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center))`.
   - Its availability is a live `EventSystem.RaycastAll` that must hit the same `MatchHudMinimapView` (line 623).
   - There is no cached or old rect.
   - Each tap opened the full map: the next gesture logs `mapOpen=True` (e.g. `aria-2:200871`, `:253062`, `:307483`).
3. **What is the control at (1660.7, 967.5)?** That point is (1660.7, 112.5) top-down. It is the full-map popup's Close button.
   - It is observed as `view.CloseMap = ObserveWatchButton(watchFullMap.CloseAction)` (`SkirmishWatch.cs:791-795`).
   - Its id is the button's EntityId hash (`SkirmishWatch.cs:24-31`). The popup is a new instance on every open, so the id changes on each loop: -168616 … -258640, with map ids -238284 … -259020.
   - Every one of those taps logs `mapOpen=True … closeAvailable=True` (e.g. `aria-2:307483`).
   - The planner taps it through `Target(view.CloseMap, …)` (`AriaSkirmishPlanSystem.cs:661/674`).
   - `control=unknown` and `mapOpen=False` at the block (`aria-2:310898`) mean the popup had already closed, so the last close tap's target was no longer offered (`reason=101 Waiting offered=0`).
   - W39 does not touch the full-map popup or its Close button.
4. **Why did run 2 loop and run 1 not?** The game state diverged; the UI behaved the same in both runs.
   - Both runs made the same early map trip (open `-20010`, drag `-20011`, close), at run 1 `aria:20259-21648` and run 2 `aria-2:21820-22964`.
   - Both runs then reached the enemy base and tapped ENEMY BASE four times: run 1 at t=218–243 (`aria:170595-176437`), run 2 at t=256–282 (`aria-2:165099-165305`). The army was 22.6 m from the base in both.
   - In run 1 the base fell at 234.8 s (Victory).
   - In run 2 it did not. At t≈285 a perimeter base alert (`aria-2:165355-165372`, `base_alert … r=perimeter`, `home_defense`) sent ARIA to DefendBase, and the army moved back home: distance 22.6 → 700.7 (`aria-2:165451`).
   - After that, FindBase with the full map open never produced a `-20011` drag target. Each trip was open → close: `aria-2:192557, 200871, 210536, 218180, 244409, 253062, 262836, 270817, 299258, 307483`. The FindBase policy then blocked.
   - This is the ARIA FindBase/map-navigation response to a game-state divergence (base not destroyed before the home alert). It is not a layout fault. Every tap landed on the live, intended control.
5. **Verdict: pre-existing ARIA behaviour, not caused by W39. Nothing was fixed in W39.** The coordinator can run the integration comparison in main if needed.

**Counted S002 rerun (lead-approved)**
- **Warm-up first** (scene cache warm, uncounted): `aria-3-warmup\unity.log`, S031:130366 Victory 34.58%, queue exit 0.
- **Counted job:** `--jobs S002:130365:en --menu-journey --validation-library --no-table --timeout 30000 --log-dir …\aria-3`.

| Check | Result | Log |
|---|---|---|
| Outcome | Victory, MainBaseDestroyed, 234.834 s, deadlinePct 21.74% | `aria-3\unity.log:187453`, `:187573`; queue `aria-3-queue.out` |
| Menu journey | Passed | `aria-3:1290` |
| Return | Passed MAIN MENU | `aria-3:187381` |
| AriaBlocked | 0 | — |
| Gate 3 | PASS (`gate3_objective.py --summary`) | — |
| Queue | `result=Completed jobs=1 recorded=1 wins=1 sourceUnchanged=1 exitCode=0` | `aria-3:187588` |

- I removed the harness evidence from these runs: 35 files deleted, and the tracked `s002-aria-watch-controls.png` restored.

**S043 CE deadlinePct** is outside 20–70 in both runs (85.75%, 93.41%). This is the known Windows CE timing divergence. S043 still won, with return and gate 3 passing.

**Evidence handling:** I removed all harness evidence written under `SkirmishExpansion/**/_Evidence` by both runs (181 files) and restored `S002/_Evidence/s002-aria-watch-controls.png`. Three live run-2 opening frames were copied first to `Design/AgentReports/MatchUiPolish/Implemented/W39/live/` as native after-evidence:
- S002 EN;
- S031 EN;
- S043 FA, which shows the live one-action state (Field Guide + Stop).

### 4. This note
Committed on the branch. No shared dated results file was edited.

## Library copy diagnosis (for seeding future checkouts)

**Conclusion.** The copied Library was valid, but the import keys did not match. The main checkout's working tree holds CRLF bytes for about 20.9k text assets, and a fresh checkout writes LF. Unity hashes raw `.meta`/asset bytes, so every asset whose `.meta` differed was reimported. Changed source content, a different build target, settings, packages or Editor version were all ruled out.

**Evidence**

| Check | Result |
|---|---|
| Sampled reimported textures | Asset bytes are identical between checkouts. Every `.meta` differs. E.g. `scn08_title_banner_frame.png.meta`: main 3905 bytes with 156 CRLF, uilab 3749 bytes LF-only. |
| `git ls-files --eol -- Assets` | Main: 20,855 of 52,643 files are `w/crlf` (14,255 `.meta`, 3,221 `.prefab`, 1,727 `.asset`, 667 `.cs`, 427 `.mat`, 324 `.anim`, …). Uilab: 110. Both are `i/lf` in the index. |
| Line-ending policy | `.gitattributes` has `* text=auto eol=lf`; the system gitconfig has `core.autocrlf=true` (shared by both worktrees; no `core.eol`). The `eol=lf` rule landed in 12c8dfc1e on 2026-07-15, after the main working tree was checked out. Git does not rewrite unchanged files, and the content normalizes equal, so main's CRLF bytes never show as modified. |
| Build target | `Library/EditorUserBuildSettings.asset` is identical: same hash, 9,568 bytes. |
| ProjectSettings and Packages | `manifest.json`, `packages-lock.json`, `ProjectVersion.txt`, `EditorSettings.asset` and `ProjectSettings.asset` are byte-identical. No diff between the two HEADs under `ProjectSettings` or `Packages`. |
| Editor version | Only 6000.5.8f1 has opened the copy: `w39-capture-before.log` header "Version is '6000.5.8f1 (5cb7df797b7d)'". This matches `ProjectVersion.txt` and the main Editor. |
| Sparse checkout | 0 skip-worktree entries under `Assets`, `Packages` or `ProjectSettings`. All 18,955 skipped paths are under `Design/**` and other non-Unity folders. |
| Reimport size | The first open reimported 9,311 assets before I stopped it: 3,199 Texture, 2,601 FBX (of 2,619), 1,151 NativeFormat and 623 Default. The approved second open (`w39-reimport.log`) finished the remaining 3,475 and exited 0. |

**Decision (lead, 2026-11-20): option 1.** Accept one reimport per new checkout. Main is not renormalized.

Options that were considered:
1. Accept one reimport per new checkout. This is what happened here. **Chosen.**
2. Make the main working tree byte-identical to a fresh checkout once: re-checkout with LF, e.g. `git rm --cached -r . && git reset --hard` while no Editor is open. That costs one reimport in main, after which copies are reusable. It touches the main checkout, so it needs the owner's OK.
3. Seed new checkouts by copying main's working-tree files as well as its Library. Then the bytes match, but the checkout differs from git normalization.

A related trap appeared in the ARIA run (§3): a fresh checkout's first ARIA queue generates `Assets/SceneDependencyCache/*.sceneWithBuildSettings.meta` (gitignored). `SkirmishCandidateSourceHash` counts these files, so the candidate fingerprint changes mid-queue. Warm the scene cache before the first counted queue on a new checkout.

## Incidents
- **First open stopped.** I stopped the first open (my own Editor, PID 37076, uilab only) during the unexpected full reimport and reported it. The owner then approved "Allow lab reimport" for uilab only.
- **Two empty files created in the main checkout by mistake.** A PowerShell .NET `WriteAllText` with a relative path ran against the main checkout's working directory and created two empty files:
  - `D:\Projects\WarlineCapture\Assets\Tests\Editor\MatchHudSidePanelsCapture.cs`
  - `D:\Projects\WarlineCapture\Assets\Tests\Editor\MatchHudSidePanelLayoutTests.cs`

  I deleted them about 10 seconds later. No `.meta` was generated, and `git status` in main is clean for that folder. Nothing else in main was touched; its Library was never touched.

  **Rule from now on:** use absolute `D:\Projects\WarlineCapture-uilab\...` paths only. Set the working directory explicitly (`Set-Location` and `[Environment]::CurrentDirectory`, or `git -C`) for every .NET or PowerShell file write. Nothing may be written, created or deleted in `D:\Projects\WarlineCapture` or any other checkout. The S002 follow-up used only absolute uilab paths.
- **Main Library untouched.** It was never touched or copied into.

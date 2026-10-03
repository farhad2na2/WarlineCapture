# Handoff: Menu UI/UX fixes (Main Menu, Campaign, Skirmish, Operations, Commander)

Created 2026-10-03. Read `AGENTS.md` first. Its Unity execution contract and mission-UI rules apply to all of this work.

## Status

- **Done:** the audit, current-screen captures and one ImageGen fix mockup per screen.
- **Started (2026-10-03):** Main Menu Credits, Commander, ARIA, Store and Armory share a measured column width and left/right edges; Settings moved left and Commander is above ARIA. Native prefab rebuilt and six geometry/capture checks passed. Evidence: `After/main-menu-columns-20261003-175133/`.
- **Implemented (2026-10-03):** all five approved native menus and loading rebuilt in the T7 workspace. Native capture/build and English/Persian normal-touch menu walkthrough passed. Full report: [IMPLEMENTATION.md](IMPLEMENTATION.md).
- **Pending acceptance:** physical phone/Device Simulator safe-area and touch-size review, human/player acceptance and complete normal-input mission including ARIA/result/return. Menu navigation does not establish these gates.
- **Visual direction approved (2026-10-03):** the user approved the five mockups with these Main Menu revisions: Store and Armory match ARIA width; Credits matches Commander width; all five panels share the same left/right edges. Settings sits to the left of the Credits panel. Apply the known mockup corrections listed below. Do not ask again for routine implementation choices.

## Evidence

| What | Path |
| --- | --- |
| Captures: 16:9, 4:3 and Persian 20:9, plus English 20:9 (Main Menu, Skirmish, Operations and Commander are still covered by the loading overlay) | `Design/AgentReports/MenuUiUxAudit/Before/20261003-143108/` |
| English 20:9 phone captures taken after the loading overlay cleared | `Design/AgentReports/MenuUiUxAudit/Before/20261003-143826/` |
| Raw geometry findings per screen and size | `findings.md` in each folder above |
| Fix mockups | `Design/AgentReports/MenuUiUxAudit/Mockups/fix-{main-menu,campaign,skirmish,operations,commander}-v01.jpg` |
| Previous Main Menu English 20:9 capture (the new one is behind the loading overlay) | `Design/AgentReports/MenuHeaderMonetization/After/completion/home-full-en-2400.png` |

Capture tool: `Assets/Game/Scripts/Editor/MenuUiAuditCapture.cs`.

- `Run`: 2400×1080 en, 1920×1080 en, 2048×1536 en and 2400×1080 fa-IR.
- `RunPhone`: 2400×1080 en only.
- It prepares an isolated save with 12 campaign missions settled, then jumps straight to each route through `UiShellRuntimeGateway.TryEnqueueRouteRequest`. It never taps anything, so it is **not** normal-input evidence.
- The `findings.md` output has known noise. It counts active-but-hidden layers (`FirstLaunchLanguageLayer`, `NarrativeLayer/.../DevelopmentReviewerControls`) and scroll-list rows outside the viewport. Only trust a finding you can also see in a screenshot.
- `FindObjectsByType(FindObjectsSortMode)` gives CS0618 deprecation warnings. These are harmless.

Run it (macOS) through the wrapper only:

```bash
Tools/CI/invoke_unity_macos.sh --timeout 1500 --log /private/tmp/warline-menu-ui-audit.log -- \
  -executeMethod Game.Editor.MenuUiAuditCapture.Run
# pass marker: "[MenuUiAudit] result=Passed output=<folder>"
```

Known quirks:

- `TryReadLoadingProgress().IsComplete` never became true on the menu, so the script waits a fixed 15 s instead. Even after that wait, the English Main Menu was still behind the loading overlay at 100%.
- After the `RunPhone` pass, the Editor exited with code 133 (Trace/BPT trap). This happened after the pass marker was logged.

## Shared problems (fix these first; they cause most per-screen issues)

1. **The top bar is inconsistent.**
   - Main Menu, Campaign and Commander use logo + credits + settings.
   - Skirmish and Operations use a "BACK TO MAIN MENU" button + a title box, with a gap before settings.
   - Back is top-left on Skirmish and Operations, and bottom-left on Campaign and Commander.
   - The credits box moves from screen to screen.
   - The blocks in the bar are different heights.
2. **Margins are too thin.** Panels sit 4–8px from the screen edge, with no safe-area inset on 20:9 phones (notches and rounded corners). The Editor Game view doesn't simulate safe areas, so test on a device or the Device Simulator.
3. **Too many text sizes.**
   - Everything uses Oxanium (Bold/Medium SDF), but Campaign uses about 23 sizes from 5 to 31dp.
   - Lots of text is 6–10dp. Smallest examples: Campaign `Goal*/Copy` is 6dp, `Objective/Label` 7–8dp, `ChapterCard_*/Subtitle` 9dp, `MissionNode_*/MissionLabel` 9–10dp.
4. **No tablet layout.**
   - At 4:3, Main Menu uses only the top ~70% of the screen.
   - Campaign shows as a 16:9 block with empty bands above and below.
   - Commander floats mid-screen.
5. **Persian (fa-IR) is only partly right-to-left.**
   - Panels and arrows aren't mirrored, and section headings mix centred and right alignment.
   - On Commander, content is shifted and the bottom bar doesn't span the full width.
   - Some strings are still English: the Skirmish search placeholder, and the default commander name "Commander" on the Main Menu card.
   - Persian has 16–39 text-overflow flags per screen, against 2–4 in English.
   - Persian text mixes in Latin digits and codes (`CH03 · M03`, `M01`).

## Per-screen issues

**Main Menu**
- The logo, credits and settings blocks are different heights.
- The Campaign hero card has no border (every other card has one), and its text sits on busy art with a weak dark overlay.
- The purpose text overflows and leaves a single word on the last line.
- The ARIA card looks tappable but has no action.
- The Commander card repeats "Commander".
- The Store and Armory buttons use a different style from the other tiles.
- The loading overlay shows developer labels to players: "ANDROID BUILD", "SECURE LINK", "COMMAND SYSTEM", "Command shell ready". The source is `Assets/Game/Scripts/Editor/SplashLoadingV3PrefabBuilder.cs`, plus `Composition/MenuBootstrapLoadingUtilities.cs`, `Composition/MenuBootstrapCompositionSystemHelper.cs` and the localization catalog/seeder.

**Campaign**
- The text-size problems above.
- The "CAMPAIGN | MISSION SELECT" title floats over the map instead of sitting in the top bar.
- The "CHAPTERS" button repeats the chapter list already on screen, and it uses the same map icon as "STORY ARCHIVE".
- The mission cards on the map have no route line between them, and one sits near the frame edge.
- The briefing panel packs objectives into cramped green tiles.

**Skirmish** (`SkirmishSetupPrefabBuilder.cs`, `UI/Screens/QuickCustomScreenView.Scenarios.cs`)
- The top bar is three unrelated boxes with a big empty gap.
- Only three of 120 battle rows are visible.
- The map preview is squashed into a thin strip.
- Players see developer wording: "S001" IDs, "SHOWING 120 OF 120 BATTLES", "Search battles (S001, Desert, BA, ...)" and "RANDOMIZE SEED". "RESET" is unclear.
- The rules panel is a wall of text that runs to the panel border. `BaseAssaultRules/Objective` and `/Roster` are flagged as overflowing.
- The map tabs are at the 44dp minimum.

**Operations** (`OperationsDashboardV3PrefabBuilder.cs`, `UI/Screens/OperationsDashboardScreenView.cs`)
- The five city stats show "—" with empty bars, which reads as broken.
- District labels are white with no backing on saturated colour zones.
- A stray "—" appears after "NEW CITY".
- "DEPLOY TO BEGIN" is styled as a red warning even though it's an instruction.
- The bottom buttons are six different colours.
- "INTEL REPORT" runs into its icon.
- On 20:9 there's a gap between ARMORY and END DAY.

**Commander** (`CommanderProfileV3PrefabBuilder.cs`)
- **The data is hardcoded:** level 38, "15,680 / 24,000 XP", stats `{ "128", "246", "8,642", "312", "76%" }` and the history rows "HOSTILE PATROL / SUPPLY RUN / CONVOY ESCORT" (around lines 289, 302 and 399). Bind these to the real profile and save.
- The portrait (an older man) doesn't match the Main Menu commander (a young woman, portrait index 0).
- "FIELD COMMANDER" appears twice.
- On phones the bottom bar and settings button don't line up with the columns above.
- The reward-track numbers aren't aligned with their icon tiles.
- "OPEN ARMORY" is shown as a main action on a profile screen.

Other builders for these screens: `MainMenuV3PrefabBuilder.cs`, `CampaignOperationsV3PrefabBuilder.cs`, `UI/Screens/MainMenuV3SectionLayoutView.cs` and `UI/Screens/CampaignOperationsScreenView*.cs`. The prefabs are generated by these editor builders, so change the builders and rebuild rather than hand-editing prefabs.

## Proposed design rules (the mockups follow these)

- One shared 64px top bar on every screen: back + title on the left (logo only on Main Menu), credits pill + settings on the right. All blocks the same height.
- 24px outer margin inside the safe area, and 12px gaps between panels.
- Every column starts and ends on the same lines as its neighbours.
- One 72px bottom action bar with a single green main button.
- No more than three text sizes (title, label, body) and nothing under 12dp. Use Oxanium Bold for headings and buttons, Medium for body text, and NotoSansArabic for fa-IR.
- 2px borders and one corner radius everywhere. Use accent colour only for selected states and the main action.
- No developer wording visible to players. Real data only.

Errors in the mockups (don't copy them):
- The Commander reward track reads "3, 4, 4, 5, 7", and its example numbers are made up.
- The Campaign map mixes numbered pins with thumbnail cards. Pick one.
- The Skirmish top bar shows both the logo and a back button.

## Suggested order

1. Get the user's approval of the mockup direction, or their changes to it.
2. Build the shared top bar, safe-area margins and text-size rules once, and use them on all five screens.
3. Remove the developer wording (loading overlay, Skirmish) and bind Commander to real data, including the portrait.
4. Rework each screen's layout, then do a fa-IR right-to-left pass and a 4:3 tablet pass.
5. Re-run `MenuUiAuditCapture.Run`. Compare the before and after captures, and check that the tiny-text and overflow counts drop.

## Validation and reporting

Report each of these separately, and keep any failed evidence:

1. Visual review of the before/after captures.
2. Automated checks: the capture script plus any focused validation.
3. A normal-input walkthrough: tap through Main Menu → each screen → back, in en and fa-IR. Existing helpers to reuse: `MenuCompletionInputReview.cs`, and the touch tap helper in `HudButtonIconNativeValidation.cs`.
4. Real phone/device acceptance (still pending).

Unity: on macOS use only `Tools/CI/invoke_unity_macos.sh`. No `-batchmode`, no direct Unity binary, no IPC reset, and never kill Unity or Unity Hub.

# Skirmish library view model

Programmer 1 owns `SkirmishLibraryViewModel` on `feature/skirmish-library-p1` (PR #65). This branch is stacked on that branch. The screen follows `Assets/Game/Scripts/UI/Screens/SkirmishLibrary/ADAPTER_SWAP.md`.

`QuickCustomScreenView` owns one `SkirmishLibraryViewModel`. `Library` returns it. `UseLibraryViewModel` replaces it. Composition injects the instance through `SkirmishLibraryFactory`; until that bind, the screen uses `CreateUnbound`. Medals are read through `ISkirmishMedalReader`. The screen subscribes to `Changed` and rebuilds the theatre, battlefield, and briefing. It subscribes to `Interaction` and plays `UIAudioEventGateway.Raise(interaction.Sound)`. The view model does not call the gateway. `Game.UI.Runtime` does not reference `Game.Runtime`.

View methods `SelectScenarioId` and `SelectMapCollection` stay `void` and forward. The interface methods with the same names return `bool`.

## Live data the screen reads

- Theatre progress strip: `OverallWonCount` / `OverallPublishedCount`, plus `OverallMedalTotals` for Recruit, Regular, Veteran, and Commander. In the editor that denominator is `DevOverallPublishedCount` (120). In a release build it is the published Playable count. The strip sits on the bottom of the map, theatre only, and is not drawn inside the battlefield summary.
- Region pins and objective tabs: `WonCount` / `PublishedCount` for that map or objective. The map's 6 cards and the grid's 24 slots are layout lengths, not the win denominator.
- Medal icons: `SkirmishMedalMask` on the card and briefing, and `SkirmishMedalTotals` on the theatre pin.
- Recommended Next: `RecommendedScenarioId`, `IsRecommended`, and the briefing `TitleLocKey` for that scenario.
- Access and lock: `SkirmishCardAccess` and `LockedReasonLocKey`. Empty planned cells stay blank.
- Titles and descriptions: `TitleLocKey`, `SubtitleLocKey`, `DescriptionLocKey`, and the army and start loc keys, through `GameLocalization`. The view model does not bake English or Farsi strings. Theatre region labels, the screen heading, and the battlefield tabs use `SkirmishBattleLibraryArt.Copy` (Mountain Pass, Airfield Plains, Desert Base, City Crossroads, Industrial Basin), so they do not show the loc strings "ARMOR APPROACH" or "CITY AIRFIELD".

`Launch()` stores `SkirmishLibraryLaunchContext.Difficulty` and returns `briefing.CanLaunch`. When it returns true, `LaunchMatch` continues into `flowSystem.LaunchMatch`. The screen does not call `SkirmishExpandedLaunchResolver`.

Quick Battle keeps the view model's one outer click. The screen copies the selected scenario locally and does not call `SelectScenarioId` again.

## Fields the view model does not have

Pass these to Programmer 1 if the briefing tiles or the mockup ratios must be live:

- The briefing tiles are objective, army, and start, from the catalog entry.
- `DeadlineSeconds` and `RecommendedSize` are shown only when `RecommendedSize` is not `None`, which is how a compiled definition is distinguished from the legacy 900-second fallback. The fallback is not shown as TIME 15:00.
- `StartingStructureCount` is not shown. Programmer 1 records that the mockup SIZE integer has no source; Field 7 and Established 10 are structure counts, not a certified size.
- Map width and depth are shown in metres only when both are greater than zero.
- No force headcount on the catalog or the view model.
- No credits on the view model. The briefing credits tile is hidden. `Game.UI.Runtime` does not reference `Game.Runtime`, so the library does not load `PlayerProfileSaveData` and does not write `0` over the account header. The header keeps `MainMenuAccountHeaderView`, which reads `TryReadMainMenuResources`.
- No fixed `/24` or `/6` denominator. A header that must read `won/24`, or a tab that must read `won/6`, needs a field. The current contract says not to use the 24 grid slots or the 6 map cards as that denominator.
- `ui.skirmish.theater.mp.title` is still "ARMOR APPROACH" / "گذر زرهی", and `ui.skirmish.theater.ap.title` is "CITY AIRFIELD". The theatre labels do not use those strings. They use the art copy names above.

`StoreRouteProductId` is on the briefing. The shell route `UIRoute.CommandExchange` has no product argument, so the store button cannot pass it.

## Flow capture

The click-through entry point is `Game.Editor.SkirmishBattleLibraryFlowCapture.RunFocusedValidation`. It writes 14 PNGs under `Design/AgentReports/SkirmishBattleLibrary/2026-10-14/flow/` and logs `[SkirmishBattleLibraryFlow] result=Passed` only when every step's state assertion holds, including the launch request. Before the theatre shot it calls `FocusTheaterOnMap("DB")`, so Zoom In opens Desert Base and `BattleCard_S001` exists. The demo seeds medals through `SkirmishLibraryViewModel` and an in-memory prefs store. Farsi captures call `TMP_FontAsset.HasCharacters` on every active text and fail when a glyph is missing. Unity has not been run for this stack on this machine, so those PNGs are not in this commit.

Windows, through the checked wrapper. The wrapper adds `-batchmode`. Do not pass `-nographics`.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnity.ps1 `
  -UnityExe "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe" `
  -ProjectPath (Get-Location).Path `
  -LogFile "$env:TEMP\warline-skirmish-library-flow.log" `
  -TimeoutSeconds 900 `
  -quit -executeMethod Game.Editor.SkirmishBattleLibraryFlowCapture.RunFocusedValidation
```

macOS, through the repository wrapper. Do not pass `-batchmode` or `-nographics`.

```bash
Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/warline-skirmish-library-flow.log -- \
  -quit -executeMethod Game.Editor.SkirmishBattleLibraryFlowCapture.RunFocusedValidation
```

Static theatre, map, and grid capture:

```bash
Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/warline-skirmish-library-capture.log -- \
  -quit -executeMethod Game.Editor.SkirmishBattleLibraryCapture.RunFocusedValidation
```

Marker: `[SkirmishBattleLibraryCapture] result=Passed`.

These captures are Editor renders. They are not visual sign-off, a normal-input playthrough, or device acceptance.

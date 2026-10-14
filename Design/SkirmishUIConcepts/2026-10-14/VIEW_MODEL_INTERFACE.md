# Skirmish library view model

Programmer 1 owns `SkirmishLibraryViewModel` on `feature/skirmish-library-p1` (PR #65). This branch is stacked on that branch. The screen follows `Assets/Game/Scripts/UI/Screens/SkirmishLibrary/ADAPTER_SWAP.md`.

`QuickCustomScreenView` owns one `SkirmishLibraryViewModel`. `Library` returns it. `UseLibraryViewModel` replaces it. The screen subscribes to `Changed` and rebuilds the theatre, battlefield, and briefing. It subscribes to `Interaction` and plays `UIAudioEventGateway.Raise(interaction.Sound)`. The view model does not call the gateway.

View methods `SelectScenarioId` and `SelectMapCollection` stay `void` and forward. The interface methods with the same names return `bool`.

## Live data the screen reads

- Theatre header: `OverallWonCount` / `OverallPublishedCount`. In the editor that denominator is `DevOverallPublishedCount` (120). In a release build it is the published Playable count.
- Region pins and objective tabs: `WonCount` / `PublishedCount` for that map or objective. The map's 6 cards and the grid's 24 slots are layout lengths, not the win denominator.
- Medal icons: `SkirmishMedalMask` on the card and briefing, and `SkirmishMedalTotals` on the theatre pin.
- Recommended Next: `RecommendedScenarioId`, `IsRecommended`, and the briefing `TitleLocKey` for that scenario.
- Access and lock: `SkirmishCardAccess` and `LockedReasonLocKey`. Empty planned cells stay blank.
- Titles and descriptions: `TitleLocKey`, `SubtitleLocKey`, `DescriptionLocKey`, and the army and start loc keys, through `GameLocalization`. The view model does not bake English or Farsi strings.

`Launch()` stores `SkirmishLibraryLaunchContext.Difficulty` and returns `briefing.CanLaunch`. When it returns true, `LaunchMatch` continues into `flowSystem.LaunchMatch`. The screen does not call `SkirmishExpandedLaunchResolver`.

Quick Battle keeps the view model's one outer click. The screen copies the selected scenario locally and does not call `SelectScenarioId` again.

## Fields the view model does not have

Pass these to Programmer 1 if the briefing tiles or the mockup ratios must be live:

- No match duration or time limit on `SkirmishBriefingState`. The TIME tile shows the briefing medal count as `n/4`.
- No force count. The FORCES tile shows `ArmyLocKey`.
- No certified size. The START tile shows `StartLocKey`.
- No credits. The screen reads `PlayerProfileSaveData.credits` and shows `0` when the profile cannot be loaded.
- No fixed `/24` or `/6` denominator. A header that must read `won/24`, or a tab that must read `won/6`, needs a field. The current contract says not to use the 24 grid slots or the 6 map cards as that denominator.
- `ui.skirmish.theater.mp.title` is "ARMOR APPROACH" / "گذر زرهی". The screen shows that loc string. "Mountain Pass" is not a view-model field.

`StoreRouteProductId` is on the briefing. The shell route `UIRoute.CommandExchange` has no product argument, so the store button cannot pass it.

## Flow capture

The click-through entry point is `Game.Editor.SkirmishBattleLibraryFlowCapture.RunFocusedValidation`. It writes 14 PNGs under `Design/AgentReports/SkirmishBattleLibrary/2026-10-14/flow/` and logs `[SkirmishBattleLibraryFlow] result=Passed` only when every step's state assertion holds, including the launch request. The demo seeds medals through `SkirmishLibraryViewModel` and an in-memory prefs store. Unity has not been run for this stack on this machine, so those PNGs are not in this commit.

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

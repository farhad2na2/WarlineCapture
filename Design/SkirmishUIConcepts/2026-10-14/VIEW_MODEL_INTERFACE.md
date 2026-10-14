# Skirmish library view model

Programmer 1 owns the real `SkirmishLibraryViewModel`. This file is the contract the SCN-13 screen binds to now. The stub below keeps the screen compiling and capturable. It does not award medals, choose Recommended Next, or pick Quick Battle.

File: `Assets/Game/Scripts/UI/Screens/SkirmishLibrary/SkirmishLibraryViewModel.cs`
Namespace: `Game.UI.Skirmish`
Owner: `QuickCustomScreenView` (`LibraryViewModel`, replace with `UseLibraryViewModel`).

`SelectScenarioId` and `SelectMapCollection` on the view stay `void`. They forward to the interface, whose methods return `bool`.

## Types

`SkirmishLibraryLevel`: `Theater`, `Battlefield`.
`SkirmishBattlefieldLayout`: `Map`, `Grid`.
`SkirmishCardAccess`: `Playable`, `NotOwned`, `Unavailable`, `Planned` (development builds only).
`SkirmishMedalMask : byte`: `None = 0`, `Recruit = 1`, `Regular = 2`, `Veteran = 4`, `Commander = 8`.

`SkirmishMedalTotals`: `Recruit`, `Regular`, `Veteran`, `Commander`.
`SkirmishTheaterPinState`: `MapId`, `TitleLocKey`, `SubtitleLocKey`, `PlatePreviewKey`, `PublishedCount`, `WonCount`, `Medals`, `HasOwnedPlayable`, `IsRecommended`.
`SkirmishTheaterState`: `Pins` (release hides battlefields with no Playable scenario), `SelectedMapId`, `RecommendedScenarioId`.
`SkirmishObjectiveTabState`: `ObjectiveId`, `TitleLocKey`, `PublishedCount`, `WonCount`.
`SkirmishBattleCardState`: scenario, map, objective, army, start, loc keys, `PreviewSpriteKey`, `PinId` (`MapId.ObjectiveId.0-5`), `Medals`, `Access`, `LockedReasonLocKey` (null when Playable), `IsSelected`, `IsRecommended`.
`SkirmishBattlefieldState`: `MapId`, `Layout`, `Tabs`, `SelectedObjectiveId`, `MapCards` (6 for the tab), `GridCards` (24, row-major `BA, FC, BT, CE` × `G-F, G-E, A-F, A-E, C-F, C-E`). Null slots are empty cells.
`SkirmishBriefingState`: loc keys, `Medals`, `AvailableDifficulties`, `SelectedDifficulty`, `Access`, `LockedReasonLocKey`, `StoreRouteProductId` (NotOwned; the full briefing stays visible), `CanLaunch`.
`SkirmishLibraryFilter`: `SearchText`, `ObjectiveId`, `ArmyProfile`, `StartProfile`, `MedalMissing`.

`SkirmishDifficultyId` is the existing enum in `Game.Skirmish.Contracts`. It is not declared again.

## Interface

`ISkirmishLibraryViewModel`

- `event Action Changed`
- `Level`, `Theater`, `Battlefield` (null on the theatre), `Briefing` (null when nothing is selected), `Filter`
- `bool SelectMapCollection(string mapId)` — `""` returns to the theatre
- `bool SelectObjective(string objectiveId)`
- `void SetLayout(SkirmishBattlefieldLayout layout)`
- `bool SelectScenarioId(string scenarioId)` — opens that battlefield and tab
- `void SelectDifficulty(SkirmishDifficultyId difficulty)`
- `void ZoomIn()`
- `void BackToTheater()`
- `string QuickBattle()` — select and open the briefing, do not launch
- `void ApplyFilter(SkirmishLibraryFilter filter)`
- `bool Launch()` — the real view model queues through `SkirmishExpandedLaunchResolver`. False leaves the screen's existing `LaunchMatch` path in place.
- `void RestoreFromPrefs()`
- `void SaveToPrefs()`

## Stub

`SkirmishLibraryStubViewModel` reads the catalog for structure only.

- Medal masks and won counts stay empty.
- `RecommendedScenarioId` stays null. `IsRecommended` stays false.
- `QuickBattle()` returns null. The screen then opens the selected battlefield and does not launch.
- `Launch()` returns false. UI cannot reference `SkirmishExpandedLaunchResolver` (`Game.Composition`). When Programmer 1's view model returns true, `QuickCustomScreenView.LaunchMatch` does not launch a second time.
- `SkirmishBattleLibraryAccessHost` assigns `SkirmishLibraryStubViewModel.Access` from `ContentAccessRuntime.Evaluate("skirmish." + id)`.
- Pin coordinates stay in `SkirmishBattlefieldPinLayout.TryGet(pinId)`.

Session keys the stub reads: `Warline.Skirmish.LibraryLevel`, `LibraryMap`, `LibraryObjective`, `LibraryPlate`, `LibraryDifficulty`, plus the existing `Warline.Skirmish.LastScenarioId`.

## Result type

`SkirmishMedalAwardResult` (`ScenarioId`, `Difficulty`, `IsFirstAward`) belongs to Programmer 1's result screen. It is not declared in this file, and there is no ledger.

## Name notes

- View methods `SelectScenarioId` / `SelectMapCollection` are `void`. The interface methods with the same names return `bool`. They are different types, so this is not a compile clash.
- The stub is not named `SkirmishLibraryViewModel`, so Programmer 1 can add that class beside the interface.
- The previous `Game.UI.Runtime` projection types (`ISkirmishBattleLibraryProjection`, `ISkirmishMedalReadModel`, `ISkirmishBattleLibraryAdvisor`, `SkirmishLibraryLevel` in that namespace) are removed.

## Editor capture

Programmer 2 runs this on the Mac with the repository wrapper. Do not pass `-batchmode`.

```bash
Tools/CI/invoke_unity_macos.sh --timeout 600 --log /private/tmp/warline-skirmish-library-prefab.log -- \
  -quit -executeMethod Game.Editor.SkirmishSetupPrefabBuilder.Build
```

Markers: `[SkirmishSetupPrefabBuilder] result=Passed` and `[SkirmishBattleLibraryPlate] result=Passed`.

```bash
Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/warline-skirmish-library-capture.log -- \
  -quit -executeMethod Game.Editor.SkirmishBattleLibraryCapture.RunFocusedValidation
```

Marker: `[SkirmishBattleLibraryCapture] result=Passed`.

PNGs: `Design/AgentReports/SkirmishBattleLibrary/2026-10-14/{theater|map|grid}-{16x9|20x9|4x3}-{en|fa-IR}.png` (18 files).

These captures are Editor renders. They are not visual sign-off, a normal-input playthrough, or device acceptance.

The capture calls `QuickCustomScreenView.ApplyLibraryCaptureDemo()` so the PNGs show wins, medals and a recommendation. That fill stays off the runtime stub. A blank or uniform PNG fails the marker. A flat grey frame, including the `-nographics` clear colour, is a failed capture.

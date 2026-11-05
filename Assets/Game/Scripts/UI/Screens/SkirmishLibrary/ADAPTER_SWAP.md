# Replace the library stub with SkirmishLibraryViewModel

Programmer 2's screen should own one `SkirmishLibraryViewModel` and drop the stub adapter. Do not keep a second interface.

## Construction

`QuickCustomScreenView` owns the instance. Read it from `QuickCustomScreenView.Library` (`ISkirmishLibraryViewModel`). Composition builds it and injects it. `MenuBootstrapCompositionSystemHelper` passes `SkirmishLibraryComposition.Create` through `UIShellContentView.BindQuickCustomRuntimeDependencies` into `QuickCustomScreenView.BindRuntimeDependencies`. The first factory is kept; a later match rebind does not replace an in-progress library.

`SkirmishLibraryComposition.Create`:

- wraps `SkirmishMedalLedger` in `ISkirmishMedalReader` (`SkirmishMedalLedgerReader`). `Refresh` reloads profile medals
- evaluates access with `ContentAccessRuntime.Evaluate`
- writes the selected difficulty with `difficulty => SkirmishLibraryLaunchContext.Difficulty = difficulty`
- passes `ShowPlannedCards` (`UNITY_EDITOR` or `DEVELOPMENT_BUILD`)
- reads compiled briefing facts with `briefingSource: ReadBriefingSource` (`SkirmishExpansionCatalogConfig.TryGetDefinition`)

The screen calls `RestoreFromPrefs` after the factory returns. `Game.UI.Runtime` does not reference `Game.Runtime`. The view model reads medals only through `ISkirmishMedalReader`. The result card reads `MedalScenarioId`, `MedalDifficulty`, and `MedalIsFirstAward` from the skirmish gateway and does not reference `SkirmishMedalLedger`. Before composition binds, the screen uses an empty reader and treats access as allowed.

Tests construct it with a fake reader:

```csharp
var library = new SkirmishLibraryViewModel(
    catalog,
    medals,
    id => ContentAccessState.Allowed,
    showPlanned: SkirmishLibraryViewModel.ShowPlannedCards,
    setLaunchDifficulty: difficulty => SkirmishLibraryLaunchContext.Difficulty = difficulty);
```

`SelectMapCollection("")` is the theater. `SelectScenarioId` opens that scenario's battlefield and card. `Launch()` calls the injected difficulty callback and does not touch `SkirmishLibraryLaunchContext` itself. `SkirmishExpandedLaunchResolver` stays the access gate.

## Changed

Subscribe to `Changed` and rebuild Theater, Battlefield, and Briefing from the properties. The view model does not bake display strings. Bind `TitleLocKey` and `SubtitleLocKey` through `GameLocalization`.

`WonCount` / `PublishedCount` on each battlefield pin and objective tab are live. `PublishedCount` is the playable catalog rows for that map or objective. The map layout is always 6 cards and the grid is always 24 slots; those lengths are not the win denominator.

The theater header reads overall progress from `SkirmishTheaterState`:

- `OverallWonCount` — distinct catalog scenarios with at least one medal
- `OverallPublishedCount` — release: published Playable count; dev/editor (`showPlanned`): `SkirmishLibraryViewModel.DevOverallPublishedCount` (120)
- `OverallMedalsEarned` — Recruit + Regular + Veteran + Commander on those scenarios
- `OverallMedalsPossible` — `OverallPublishedCount * 4`
- `OverallMedalTotals` — the same four counts read through `ISkirmishMedalReader`

Briefing adds sourced facts only. `ObjectiveLocKey` comes from the catalog objective id. When a compiled definition exists, `DeadlineSeconds`, `RecommendedSize`, `StartingStructureCount`, `MapWidthMetres`, and `MapDepthMetres` come from that definition. `ShowTime` is true when `DeadlineSeconds > 0`. `ShowSize` is true only for a found definition whose `RecommendedSize` is Standard, War, or Large War, and `SizeLocKey` is `ui.skirmish.size.standard`, `ui.skirmish.size.war`, or `ui.skirmish.size.large_war`. A playable scenario with no definition uses `SkirmishPresetConfig.MatchDurationSeconds` (900), so `ShowTime` stays true, and leaves the size, structure count, and map extent at zero (`ShowSize` false). A planned scenario with no definition leaves the deadline at zero (`ShowTime` false). `StartingStructureCount` is not the SIZE label. There is no per-scenario enemy count; do not bind `QuickGameConfig.EnemyCount`.

`PreviewSpriteKey` is `Assets/Game/Art/SkirmishLibrary/Previews/skirmish.preview.{ScenarioId}.png` when that sprite exists, otherwise `skirmish.preview.{ScenarioId}` for a playable card and the plate key for a planned card. `PlatePreviewKey` is `Assets/Game/Art/SkirmishLibrary/Plates/skirmish.plate.{MapId}.png` when that sprite exists, otherwise `skirmish.plate.{MapId}`. Player builds keep the logical keys. `Game.Editor.SkirmishLibraryPreviewCapture.Run` writes card previews at 1024×576 and plates at 1920×1080. Those PNGs live under `Assets/Game/Art`, so `SkirmishLibraryPreviewAssets.Load` does not read the disk and does not call `AssetDatabase`. It resolves the asset path and the logical key through `SkirmishLibrarySpriteRegistry` at `Assets/Game/Resources/SkirmishLibrary/SkirmishLibrarySpriteRegistry.asset`. `SkirmishLibrarySpriteRegistryBuilder.Rebuild` registers every PNG in the Previews and Plates folders under both keys. The SCN-13 prefab builder, preview import, and an AssetPostprocessor on `Assets/Game/Art/SkirmishLibrary/**/*.png` all call Rebuild. The screen cover-crops the registered sprite, using that texture's pixel size, into the card, grid, briefing, and plate frames. A key missing from the registry shows a dark neutral frame sized to that image slot and drawn behind the card chrome. Theatre zones show the registered plate, tinted, with an edge and a card (name, won/total, progress ring). Capture and the flow check fail when a set `PreviewSpriteKey` or `PlatePreviewKey` resolves to null, and when any PNG in those two folders is missing from the registry under either key.

The TIME tile stays visible when `ShowTime` is true and shows `DeadlineSeconds`. A playable scenario with no compiled definition uses the Design-approved fallback `SkirmishPresetConfig.MatchDurationSeconds` (900), so the tile reads 15:00. The capture demo uses that fallback. Keep the tile. The SIZE tile is visible only when `ShowSize` is true and then shows `SizeLocKey`. The screen does not show `StartingStructureCount`, map metres, or a forces count. Credits use `SkirmishLibraryWallet.ReadCredits`, which composition binds to the profile wallet. When that reader is absent the credits chip stays hidden.

## Button sounds

Subscribe to `Interaction`. Each notification is a `SkirmishLibraryInteraction` with `Kind` (`Clicked` or `Selected`), `TargetId`, and `Sound` (`UIAudioEventKind`). Play it with `UIAudioEventGateway.Raise(interaction.Sound)`. The view model does not call the gateway, so EditMode tests stay silent. Nested actions such as Quick Battle raise one outer notification.

## Return from a match

A finished expanded match queues `SkirmishLibraryReturn`. `QuickCustomScreenView.ReturnLibraryToScenario` applies it with `RestoreFromPrefs` then `SelectScenarioId`. Call that when the Battlefield control on the result card is pressed. Leave stays Main Menu.

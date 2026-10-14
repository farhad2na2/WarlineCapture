# Replace the library stub with SkirmishLibraryViewModel

Programmer 2's screen should own one `SkirmishLibraryViewModel` and drop the stub adapter. Do not keep a second interface.

## Construction

`QuickCustomScreenView` already owns the instance. Read it from `QuickCustomScreenView.Library` (`ISkirmishLibraryViewModel`). The screen builds it with `SkirmishLibraryViewModel.CreateDefault(catalog)`:

- loads profile medals through `SkirmishMedalLedger`
- evaluates access with `ContentAccessRuntime.Evaluate`
- passes `ShowPlannedCards` (`UNITY_EDITOR` or `DEVELOPMENT_BUILD`)
- calls `RestoreFromPrefs`

Tests and tools can construct it directly:

```csharp
var library = new SkirmishLibraryViewModel(
    catalog,
    ledger,
    id => ContentAccessRuntime.Evaluate(id),
    showPlanned: SkirmishLibraryViewModel.ShowPlannedCards);
```

`SelectMapCollection("")` is the theater. `SelectScenarioId` opens that scenario's battlefield and card. `Launch()` only stores the selected difficulty on `SkirmishLibraryLaunchContext`. `SkirmishExpandedLaunchResolver` stays the access gate.

## Changed

Subscribe to `Changed` and rebuild Theater, Battlefield, and Briefing from the properties. The view model does not bake display strings. Bind `TitleLocKey` and `SubtitleLocKey` through `GameLocalization`.

`WonCount` / `PublishedCount` on each battlefield pin and objective tab are live. `PublishedCount` is the playable catalog rows for that map or objective. The map layout is always 6 cards and the grid is always 24 slots; those lengths are not the win denominator.

The theater header reads overall progress from `SkirmishTheaterState`:

- `OverallWonCount` — distinct catalog scenarios with at least one medal
- `OverallPublishedCount` — release: published Playable count; dev/editor (`showPlanned`): `SkirmishLibraryViewModel.DevOverallPublishedCount` (120)
- `OverallMedalsEarned` — Recruit + Regular + Veteran + Commander on those scenarios
- `OverallMedalsPossible` — `OverallPublishedCount * 4`
- `OverallMedalTotals` — the same four counts from `SkirmishMedalLedger`

## Button sounds

Subscribe to `Interaction`. Each notification is a `SkirmishLibraryInteraction` with `Kind` (`Clicked` or `Selected`), `TargetId`, and `Sound` (`UIAudioEventKind`). Play it with `UIAudioEventGateway.Raise(interaction.Sound)`. The view model does not call the gateway, so EditMode tests stay silent. Nested actions such as Quick Battle raise one outer notification.

## Return from a match

A finished expanded match queues `SkirmishLibraryReturn`. `QuickCustomScreenView.ReturnLibraryToScenario` applies it with `RestoreFromPrefs` then `SelectScenarioId`. Call that when the Battlefield control on the result card is pressed. Leave stays Main Menu.

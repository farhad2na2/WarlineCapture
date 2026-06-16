# SCN08 Match HUD Content UI Toolkit Conversion

This folder mirrors the Canvas prefab at `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab` as UI Toolkit assets.

- `SCN08_MatchHudContent.uxml` preserves the main HUD section names from the Canvas prefab: `HeaderContent`, `LeftContent`, `RightContent`, `FooterContent`, `SelectedSquadPanel`, `SquadTray`, `CommandRail`, `MinimapPanel`, and `FeedbackPanel`.
- `SCN08_PassengerItemView.uxml` is the reusable passenger row template equivalent to the Canvas `PassengerItemView` under `TransportPassengerDrawer / Scroll View / Viewport / Content`.
- The passenger drawer is represented as a `ScrollView` named `Scroll_View` with a named `Content` container for runtime item population.
- The styles use the generated SCN08 target-lock sprites from `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV01`, plus the cleaned panel/icon folders already used by the Canvas prefab.

# SCN08 Match HUD Content UI Toolkit Conversion

This folder mirrors the Canvas prefab at `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab` as UI Toolkit assets.

- `SCN08_MatchHudContent.uxml` preserves the main HUD section names from the Canvas prefab: `HeaderContent`, `LeftContent`, `RightContent`, `FooterContent`, `SelectedSquadPanel`, `SquadTray`, `CommandRail`, `MinimapPanel`, and `FeedbackPanel`.
- `SCN08_PassengerItemView.uxml` is the reusable passenger row template equivalent to the Canvas `PassengerItemView` under `TransportPassengerDrawer / Scroll View / Viewport / Content`.
- The passenger drawer is represented as a `ScrollView` named `Scroll_View` with a named `Content` container for runtime item population.
- The styles use `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02`, built from the accepted new main-menu/loading art direction.
- Long baked multipanel source sheets are explicitly not imported into Unity; every Toolkit panel uses a single-purpose frame/background sprite with live text and independently placed icons.
- The V02 reusable chrome frames used by this surface intentionally use `spritePixelsToUnits: 300` so UI Toolkit renders buttons, cards, chips, rails, and panel chrome slimmer, closer to the target mockup. This includes `scn08_v02_square_button_frame.png`, `scn08_v02_rect_button_frame.png`, `scn08_v02_squad_card_frame.png`, `scn08_v02_panel_frame_large.png`, `scn08_v02_selected_panel_frame.png`, `scn08_v02_objectives_panel_frame.png`, `scn08_v02_wide_rail_frame.png`, `scn08_v02_resource_chip_frame.png`, `scn08_v02_chip_frame.png`, and `scn08_v02_progress_track_frame.png`. Do not reset them to the generic `100` import default without rechecking the UI Builder/Game View crop.
- Match HUD live text should be sized by focused crop comparison against `SCN-08_RTSBattleHUD_NewMainMenuArtDirection_TargetLock_V02.png`; avoid tiny default labels when the target uses larger readable type.

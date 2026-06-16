# SCN02 Main Menu Content UI Toolkit Conversion

This folder mirrors the Canvas prefab at `Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab` as UI Toolkit assets.

- `SCN02_MainMenuContent.uxml` keeps the main Canvas section names and interaction element names for binding-friendly lookup.
- `SCN02_MainMenuContent.uss` uses the atomized `MainMenuBrightCommand` sprites from `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites`.
- Text remains live UI Toolkit text and uses the current project UI font, `Oxanium-Medium`, through the USS font definition.
- Runtime binding points include `Nav_*`, `Card_*`, `InboxButton`, `SettingsButton`, `CommanderPanel`, `DeployOperationButton`, and the resource value labels.
- Card art, card frames, label plates, badges, header chips, header actions, commander chrome, and deploy chrome are separate sprite layers rather than one baked mockup image.
- Chrome sprites are imported with Sprite Editor borders and rendered with UI Toolkit `-unity-slice-*` plus `-unity-slice-scale` so the frame edges stay thin instead of stretching into sharp oversized corners.
- Transparent frame interiors use exact-shape backing sprites, such as `scn02c_mode_card_backing_*` and `scn02c_nav_button_backing_default`, beneath the chrome frame. Do not use plain rectangular fills for chamfered panel chrome, because the background will leak through transparent or semi-transparent frame areas.
- The commander/right column is composed from separate reusable sections: title, portrait, identity, progress, and readiness. Avoid returning to a single tall multi-section frame because stretched separators make text and icon alignment brittle.

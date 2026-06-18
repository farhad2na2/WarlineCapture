# SCN19 Armory Content UI Toolkit Conversion

This folder contains the UI Toolkit Armory screen for the current bright premium command-table art direction.

- Active target reference: `Design/VisualLockLayered/SCN-19_Armory/reference/SCN-19_Armory_NewMainMenuArtDirection_TargetLock_V04.png`.
- Do not use the deleted/old `SCN-19_Armory_Landscape_Target.png` or older SCN19 target-lock folders for this screen.
- The top header is intentionally aligned with the new main-menu header language: Warline Capture logo, resource strip, and right menu action.
- `SCN19_ArmoryContent.uxml` preserves the main section names from the Canvas prefab for binding-friendly lookup: `LeftContent`, `MiddleContent`, `RightContent`, `FooterContent`, `Scroll_View`, `Content`, and the route/button names.
- `SCN19_ArmoryItemView.uxml` is the reusable catalog item template equivalent to the Canvas `ItemView` under the original `Scroll View/Viewport/Content`.
- `SCN19_ArmoryContent.uss` uses generated SCN19 sprites from `Assets/Game/Art/UI/Generated/Armory/LayeredOneGo` plus shared new-art header/background assets from `MainMenuBrightCommand` and `SplashLoading/TargetLockV04Imagegen`.
- Runtime binding should populate cloned item template instances under `Content` and update the right `InspectionPanel` fields from the selected catalog entry.

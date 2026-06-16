# SCN19 Armory Content UI Toolkit Conversion

This folder mirrors the Canvas prefab at `Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab` as UI Toolkit assets.

- `SCN19_ArmoryContent.uxml` preserves the main section names from the Canvas prefab for binding-friendly lookup: `LeftContent`, `MiddleContent`, `RightContent`, `FooterContent`, `Scroll_View`, `Content`, and the route/button names.
- `SCN19_ArmoryItemView.uxml` is the reusable catalog item template equivalent to the Canvas `ItemView` under the original `Scroll View/Viewport/Content`.
- `SCN19_ArmoryContent.uss` uses the generated SCN19 sprites from `Assets/Game/Art/UI/Generated/Armory/LayeredOneGo` and final cleaned panel frames from `Assets/Game/Art/UI/Final`.
- Runtime binding should populate cloned item template instances under `Content` and update the right `InspectionPanel` fields from the selected catalog entry.

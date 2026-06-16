# SCN09 Build Drawer Popup UI Toolkit Conversion

This folder mirrors the Canvas prefab at `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab` as UI Toolkit assets.

- `SCN09_BuildDrawerPopup.uxml` preserves popup binding names such as `BuildDrawerRoot`, `DrawerFrame`, `BuildPanel`, `ProductionPanel`, `BuildButton`, `RushButton`, `ClearButton`, and `CloseButton`.
- `SCN09_BuildCatalogItemView.uxml` is the reusable catalog card template used by the left catalog scroll content.
- `SCN09_ProductionQueueItemView.uxml` and `SCN09_ProductionActiveItemView.uxml` mirror the queued and active production row templates under the right production scroll content.
- The two Canvas `ScrollRect` areas are represented as `CatalogScrollView` and `ProductionScrollView`, each with a named `Content` container for runtime population.
- Styles use the cleaned SCN09 panel/icon assets from `Assets/Game/Art/UI/Panels`, `Assets/Game/Art/UI/Icons`, and generated BuildDrawer layered assets.
